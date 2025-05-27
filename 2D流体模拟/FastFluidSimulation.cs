using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public struct DataStride
{
    public static int cellData = 52;
    public static int staggeredData = 12;
    public static int floatData = 4;
    public static int float2Data = 8;
}
public class FastFluidSimulation : MonoBehaviour
{
    public bool isInit = false;

    public ComputeShader FluidSimulationCS;
    [HideInInspector]
    public RenderTexture FluidTex;

    //[Tooltip("雅可比迭代次数")]
    //[Range(1, 100)]
    //public int JacobiIt = 50;
    [Tooltip("更新速度迭代次数")]
    [Range(1, 128)]
    public int UpdateVeloIt = 64;
    [Tooltip("平流迭代次数")]
    [Range(1, 128)]
    public int AdvectIt = 32;
    [Tooltip("涡度约束迭代次数")]
    [Range(1, 128)]
    public int VorticityConfinementIt = 16;

    [Range(0, 3)]
    public float TimeStep = 1;
    [Range(0, 10)]
    public float MouseTint = 1;
    [Range(0, 10)]
    public float MaxVelocity = 3;
    [Range(0, 1)]
    public float CurlTint = 0.35f;

    [Tooltip("纹理大小，外部纹理也需一致，最好是32的整数倍（纹理大小会影响漩涡）")]
    [Range(2, 4096)]
    public int texSizeW = 256;
    [Tooltip("纹理大小，外部纹理也需一致，最好是32的整数倍（纹理大小会影响漩涡）")]
    [Range(2, 4096)]
    public int texSizeH = 256;
    private int2 texsize_dis;
    private float2 inverseTexSize;

    private Vector2 lastCurPos;

    public bool useForceTex = false;
    [Tooltip("RG通道存速度，B通道存染料量，A通道如果是1，则缩放RG到[-1, 1]。该力持续添加")]
    public Texture2D ForceTex = null;
    private Texture2D ForceTexOld = null;
    public bool useObstaclesTex = false;
    [Tooltip("0无障碍，1有障碍，0-1半障碍")]
    public Texture2D ObstaclesTex = null;
    private Texture2D ObstaclesTexOld = null;

    [Range(0.9f, 1)]
    public float velocityDissipation = 0.95f;
    [Range(0.99f, 1f)]
    public float densityDissipation = 0.999f;
    //[Range(0, 2)]
    //public float gradientScale = 1;
    [Range(0.001f, 0.01f)]
    public float ForceSize = 0.005f;
    public Color ObstaclesCol = Color.white;

    private ComputeBuffer cellBuffer;
    private ComputeBuffer UstaggeredBuffer;
    private ComputeBuffer VstaggeredBuffer;
    private ComputeBuffer UTempVeloBuffer;
    private ComputeBuffer VTempVeloBuffer;
    private ComputeBuffer CurlBuffer;

    private bool isuse = false;

    private int kernelInitDataID;
    private int kernelAddForceID;
    private int kernelAdvectID;
    private int kernelVorticityConfinementID;
    private int kernelVelocityTempID;
    private int kernelUpdateVelocityTempID;
    private int kernelUpdateVelocityStagID;
    private int kernelUpdateVelocityID;
    //private int kernelDivergenceID;
    //private int kernelComputePressureID;
    //private int kernelSubtractPressureGradientID;
    private int kernelUseForceTexID;
    private int kernelUseObstaclesTexID;

    private static readonly int TexSizeID = Shader.PropertyToID("TexSize");
    private static readonly int inverseTexSizeID = Shader.PropertyToID("inverseTexSize");
    private static readonly int TimeStepID = Shader.PropertyToID("TimeStep");
    private static readonly int CurlTintID = Shader.PropertyToID("CurlTint");
    private static readonly int MaxVelocityID = Shader.PropertyToID("MaxVelocity");
    private static readonly int ForceSizeID = Shader.PropertyToID("ForceSize");
    private static readonly int MousePos0ID = Shader.PropertyToID("MousePos0");
    private static readonly int MousePos1ID = Shader.PropertyToID("MousePos1");
    private static readonly int MouseTintID = Shader.PropertyToID("MouseTint");
    private static readonly int ForceDirID = Shader.PropertyToID("ForceDir");
    ///////////////////////////////////////////////////////////////////////////////////
    private static readonly int velocityDissipationID = Shader.PropertyToID("velocityDissipation");
    private static readonly int densityDissipationID = Shader.PropertyToID("densityDissipation");
    //private static readonly int gradientScaleID = Shader.PropertyToID("gradientScale");
    private static readonly int obstaclesColID = Shader.PropertyToID("obstaclesCol");
    ///////////////////////////////////////////////////////////////////////////////////
    private static readonly int cellDataID = Shader.PropertyToID("cellData");
    private static readonly int UstaggeredDataID = Shader.PropertyToID("UstaggeredData");
    private static readonly int VstaggeredDataID = Shader.PropertyToID("VstaggeredData");
    private static readonly int UTempVeloDataID = Shader.PropertyToID("UTempVeloData");
    private static readonly int VTempVeloDataID = Shader.PropertyToID("VTempVeloData");
    private static readonly int CurlDataID = Shader.PropertyToID("CurlData");
    ///////////////////////////////////////////////////////////////////////////////////
    private static readonly int FluidTexID = Shader.PropertyToID("FluidTex");
    private static readonly int ForceTexID = Shader.PropertyToID("ForceTex");
    private static readonly int ObstaclesTexID = Shader.PropertyToID("ObstaclesTex");

    void Start()
    {
        isuse = FluidSimulationCS != null;
        if (isuse)
        {
            texsize_dis.x = Mathf.CeilToInt((float)texSizeW / 32);
            texsize_dis.y = Mathf.CeilToInt((float)texSizeH / 32);
            inverseTexSize.x = 1.0f / (texSizeW - 1);
            inverseTexSize.y = 1.0f / (texSizeH - 1);

            if (useForceTex)
            {
                ForceTexOld = ForceTex;
            }
            if (useObstaclesTex)
            {
                ObstaclesTexOld = ObstaclesTex;
            }

            FluidTex = new(texSizeW, texSizeH, 0, RenderTextureFormat.ARGB32)
            {
                useMipMap = false,
                autoGenerateMips = false,
                enableRandomWrite = true,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            FluidTex.Create();

            kernelInitDataID = FluidSimulationCS.FindKernel("InitData");
            kernelAddForceID = FluidSimulationCS.FindKernel("AddForce");
            kernelAdvectID = FluidSimulationCS.FindKernel("Advect");
            kernelVorticityConfinementID = FluidSimulationCS.FindKernel("VorticityConfinement");
            kernelVelocityTempID = FluidSimulationCS.FindKernel("VelocityTemp");
            kernelUpdateVelocityTempID = FluidSimulationCS.FindKernel("UpdateVelocityTemp");
            kernelUpdateVelocityStagID = FluidSimulationCS.FindKernel("UpdateVelocityStag");
            kernelUpdateVelocityID = FluidSimulationCS.FindKernel("UpdateVelocity");
            //kernelDivergenceID = FluidSimulationCS.FindKernel("Divergence");
            //kernelComputePressureID = FluidSimulationCS.FindKernel("ComputePressure");
            //kernelSubtractPressureGradientID = FluidSimulationCS.FindKernel("SubtractPressureGradient");
            kernelUseForceTexID = FluidSimulationCS.FindKernel("UseForceTex");
            kernelUseObstaclesTexID = FluidSimulationCS.FindKernel("UseObstaclesTex");

            lastCurPos = new(0.5f, 0.5f);
            SetInitNormal();

            int count = texSizeW * texSizeH;
            cellBuffer = new(count, DataStride.cellData);
            UstaggeredBuffer = new((texSizeW + 1) * texSizeH, DataStride.staggeredData);
            VstaggeredBuffer = new(texSizeW * (texSizeH + 1), DataStride.staggeredData);
            UTempVeloBuffer = new(UstaggeredBuffer.count * 2, DataStride.floatData);
            VTempVeloBuffer = new(VstaggeredBuffer.count * 2, DataStride.floatData);
            CurlBuffer = new(count, DataStride.float2Data);

            FluidSimulationCS.SetFloats(MousePos1ID, new float[] { 0.25f, 0.25f });

            FluidSimulationCS.SetFloat(ForceSizeID, ForceSize);
            ////////////////////////////////////////////////////
            FluidSimulationCS.SetBuffer(kernelInitDataID, cellDataID, cellBuffer);
            FluidSimulationCS.SetBuffer(kernelInitDataID, UstaggeredDataID, UstaggeredBuffer);
            FluidSimulationCS.SetBuffer(kernelInitDataID, VstaggeredDataID, VstaggeredBuffer);
            FluidSimulationCS.SetBuffer(kernelInitDataID, UTempVeloDataID, UTempVeloBuffer);
            FluidSimulationCS.SetBuffer(kernelInitDataID, VTempVeloDataID, VTempVeloBuffer);
            ////////////////////////////////////////////////////
            FluidSimulationCS.SetBuffer(kernelAddForceID, cellDataID, cellBuffer);
            ////////////////////////////////////////////////////
            FluidSimulationCS.SetBuffer(kernelAdvectID, cellDataID, cellBuffer);
            FluidSimulationCS.SetBuffer(kernelAdvectID, UstaggeredDataID, UstaggeredBuffer);
            FluidSimulationCS.SetBuffer(kernelAdvectID, VstaggeredDataID, VstaggeredBuffer);
            ////////////////////////////////////////////////////
            FluidSimulationCS.SetBuffer(kernelVorticityConfinementID, cellDataID, cellBuffer);
            FluidSimulationCS.SetBuffer(kernelVorticityConfinementID, CurlDataID, CurlBuffer);
            ////////////////////////////////////////////////////
            FluidSimulationCS.SetBuffer(kernelVelocityTempID, cellDataID, cellBuffer);
            FluidSimulationCS.SetBuffer(kernelVelocityTempID, CurlDataID, CurlBuffer);
            FluidSimulationCS.SetBuffer(kernelVelocityTempID, UstaggeredDataID, UstaggeredBuffer);
            FluidSimulationCS.SetBuffer(kernelVelocityTempID, VstaggeredDataID, VstaggeredBuffer);
            ////////////////////////////////////////////////////
            FluidSimulationCS.SetBuffer(kernelUpdateVelocityTempID, cellDataID, cellBuffer);
            FluidSimulationCS.SetBuffer(kernelUpdateVelocityTempID, UstaggeredDataID, UstaggeredBuffer);
            FluidSimulationCS.SetBuffer(kernelUpdateVelocityTempID, VstaggeredDataID, VstaggeredBuffer);
            FluidSimulationCS.SetBuffer(kernelUpdateVelocityTempID, UTempVeloDataID, UTempVeloBuffer);
            FluidSimulationCS.SetBuffer(kernelUpdateVelocityTempID, VTempVeloDataID, VTempVeloBuffer);
            ////////////////////////////////////////////////////
            FluidSimulationCS.SetBuffer(kernelUpdateVelocityStagID, cellDataID, cellBuffer);
            FluidSimulationCS.SetBuffer(kernelUpdateVelocityStagID, UstaggeredDataID, UstaggeredBuffer);
            FluidSimulationCS.SetBuffer(kernelUpdateVelocityStagID, VstaggeredDataID, VstaggeredBuffer);
            FluidSimulationCS.SetBuffer(kernelUpdateVelocityStagID, UTempVeloDataID, UTempVeloBuffer);
            FluidSimulationCS.SetBuffer(kernelUpdateVelocityStagID, VTempVeloDataID, VTempVeloBuffer);
            ////////////////////////////////////////////////////
            FluidSimulationCS.SetBuffer(kernelUpdateVelocityID, cellDataID, cellBuffer);
            FluidSimulationCS.SetBuffer(kernelUpdateVelocityID, UstaggeredDataID, UstaggeredBuffer);
            FluidSimulationCS.SetBuffer(kernelUpdateVelocityID, VstaggeredDataID, VstaggeredBuffer);
            FluidSimulationCS.SetBuffer(kernelUpdateVelocityID, UTempVeloDataID, UTempVeloBuffer);
            FluidSimulationCS.SetBuffer(kernelUpdateVelocityID, VTempVeloDataID, VTempVeloBuffer);
            ////////////////////////////////////////////////////
            //FluidSimulationCS.SetBuffer(kernelDivergenceID, cellDataID, cellBuffer);
            //FluidSimulationCS.SetBuffer(kernelDivergenceID, UstaggeredDataID, UstaggeredBuffer);
            //FluidSimulationCS.SetBuffer(kernelDivergenceID, VstaggeredDataID, VstaggeredBuffer);
            ////////////////////////////////////////////////////
            //FluidSimulationCS.SetBuffer(kernelComputePressureID, cellDataID, cellBuffer);
            ////////////////////////////////////////////////////
            //FluidSimulationCS.SetBuffer(kernelSubtractPressureGradientID, cellDataID, cellBuffer);
            ////////////////////////////////////////////////////
            FluidSimulationCS.SetBuffer(kernelUseForceTexID, cellDataID, cellBuffer);
            ////////////////////////////////////////////////////
            FluidSimulationCS.SetBuffer(kernelUseObstaclesTexID, cellDataID, cellBuffer);

            FluidSimulationCS.SetTexture(kernelAdvectID, FluidTexID, FluidTex);
            FluidSimulationCS.SetTexture(kernelUseForceTexID, ForceTexID, ForceTex);

            //debug
            //FluidSimulationCS.SetTexture(kernelUseForceTexID, FluidTexID, FluidTex);

            //初始化
            FluidSimulationCS.Dispatch(kernelInitDataID, texsize_dis.x, texsize_dis.y, 1);
            if (useObstaclesTex)
            {
                FluidSimulationCS.SetTexture(kernelUseObstaclesTexID, ObstaclesTexID, ObstaclesTex);
                FluidSimulationCS.Dispatch(kernelUseObstaclesTexID, texsize_dis.x, texsize_dis.y, 1);
            }
        }
    }
    void OnDisable()
    {
        cellBuffer?.Release();
        UstaggeredBuffer?.Release();
        VstaggeredBuffer?.Release();
        UTempVeloBuffer?.Release();
        VTempVeloBuffer?.Release();
        CurlBuffer?.Release();

        FluidTex?.Release();
        ForceTexOld = null;
        ObstaclesTexOld = null;
    }
    private void OnValidate()
    {
        if (isuse)
        {
            if (useForceTex && ForceTexOld != ForceTex)
            {
                ForceTexOld = ForceTex;
                FluidSimulationCS.SetTexture(kernelUseForceTexID, ForceTexID, ForceTex);
            }
            if (useObstaclesTex && ObstaclesTexOld != ObstaclesTex)
            {
                ObstaclesTexOld = ObstaclesTex;
                FluidSimulationCS.SetTexture(kernelUseObstaclesTexID, ObstaclesTexID, ObstaclesTex);
                FluidSimulationCS.Dispatch(kernelUseObstaclesTexID, texsize_dis.x, texsize_dis.y, 1);
            }
        }
    }
    void Update()
    {
        isuse = FluidSimulationCS != null;
        if (isuse)
        {
            if (FluidTex != null && (FluidTex.width != texSizeW || FluidTex.height != texSizeH))
            {
                FluidTex?.Release();
                FluidTex = new(texSizeW, texSizeH, 0, RenderTextureFormat.ARGB32)
                {
                    useMipMap = false,
                    autoGenerateMips = false,
                    enableRandomWrite = true,
                    filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Clamp
                };
                FluidTex.Create();
            }
            if (isInit)
            {
                FluidSimulationCS.Dispatch(kernelInitDataID, texsize_dis.x, texsize_dis.y, 1);
                isInit = false;
            }
            if (!useObstaclesTex && ObstaclesTexOld != null)
            {
                ObstaclesTexOld = null;
            }
            if (!useForceTex && ForceTexOld != null)
            {
                ForceTexOld = null;
            }

            texsize_dis.x = Mathf.CeilToInt((float)texSizeW / 32);
            texsize_dis.y = Mathf.CeilToInt((float)texSizeH / 32);
            inverseTexSize.x = 1.0f / (texSizeW - 1);
            inverseTexSize.y = 1.0f / (texSizeH - 1);
            SetInitNormal();

            //用户输入
            if (Input.GetMouseButton(0))
            {
                Vector3 posmouse = Camera.main.ScreenToViewportPoint(Input.mousePosition);
                Vector2 nowCurPos = new(posmouse.x, posmouse.y);
                Vector2 dir = nowCurPos - lastCurPos;
                dir = dir.normalized;
                FluidSimulationCS.SetFloat(ForceSizeID, ForceSize);
                FluidSimulationCS.SetFloats(MousePos0ID, new float[] { nowCurPos.x, nowCurPos.y });
                FluidSimulationCS.SetFloats(ForceDirID, new float[] { dir.x, dir.y });
                FluidSimulationCS.SetFloat(MouseTintID, MouseTint);
                FluidSimulationCS.Dispatch(kernelAddForceID, texsize_dis.x, texsize_dis.y, 1);
                lastCurPos = nowCurPos;
            }
            if (useForceTex)
            {
                FluidSimulationCS.Dispatch(kernelUseForceTexID, texsize_dis.x, texsize_dis.y, 1);
            }
            //平流
            for (int i = 0; i < AdvectIt; i++)
            {
                FluidSimulationCS.Dispatch(kernelAdvectID, texsize_dis.x, texsize_dis.y, 1);
            }
            //涡度约束
            for (int i = 0; i < VorticityConfinementIt; i++)
            {
                FluidSimulationCS.Dispatch(kernelVorticityConfinementID, texsize_dis.x, texsize_dis.y, 1);
            }
            //更新速度
            FluidSimulationCS.Dispatch(kernelVelocityTempID, texsize_dis.x, texsize_dis.y, 1);
            for (int i = 0; i < UpdateVeloIt; i++)
            {
                FluidSimulationCS.Dispatch(kernelUpdateVelocityTempID, texsize_dis.x, texsize_dis.y, 1);
                FluidSimulationCS.Dispatch(kernelUpdateVelocityStagID, texsize_dis.x, texsize_dis.y, 1);
            }
            FluidSimulationCS.Dispatch(kernelUpdateVelocityID, texsize_dis.x, texsize_dis.y, 1);
            //散度
            //FluidSimulationCS.Dispatch(kernelDivergenceID, texsize_dis.x, texsize_dis.y, 1);
            //计算压力   雅可比迭代
            //for (int i = 0; i < JacobiIt; i++)
            //{
            //    FluidSimulationCS.Dispatch(kernelComputePressureID, texsize_dis.x, texsize_dis.y, 1);
            //}
            //减去梯度
            //FluidSimulationCS.Dispatch(kernelSubtractPressureGradientID, texsize_dis.x, texsize_dis.y, 1);
        }
    }
    private void OnGUI()
    {
        Vector2 size = new(texSizeW, texSizeH);
        Vector2 pos = new Vector2(Screen.width / 2, Screen.height / 2) - size * 0.5f;
        Rect m_rect = new(pos, size);
        GUI.DrawTexture(m_rect, FluidTex);
    }

    public void SetInitNormal()
    {
        FluidSimulationCS.SetInts(TexSizeID, new int[] { texSizeW - 1, texSizeH - 1 });
        FluidSimulationCS.SetFloats(inverseTexSizeID, new float[] { inverseTexSize.x, inverseTexSize.y });
        FluidSimulationCS.SetFloat(TimeStepID, TimeStep);
        FluidSimulationCS.SetFloat(CurlTintID, CurlTint);
        FluidSimulationCS.SetFloat(MaxVelocityID, MaxVelocity);

        FluidSimulationCS.SetFloat(velocityDissipationID, velocityDissipation);
        FluidSimulationCS.SetFloat(densityDissipationID, densityDissipation);
        //FluidSimulationCS.SetFloat(gradientScaleID, gradientScale);
        FluidSimulationCS.SetVector(obstaclesColID, ObstaclesCol);
    }
}