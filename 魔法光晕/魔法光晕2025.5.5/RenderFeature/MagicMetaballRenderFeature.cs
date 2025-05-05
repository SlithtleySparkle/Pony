using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using static UnityEngine.Rendering.DebugUI.Table;

public class MagicMetaballRenderFeature : ScriptableRendererFeature
{
    [System.Serializable]
    public class MagicMetaballRenderFeature_Setting
    {
        public bool debugmaincam;
        [Range(0, 2)]
        public int OWV = 0;

        public RenderPassEvent PassEvent = RenderPassEvent.AfterRenderingTransparents;
        public List<Material> mats = new();

        [Tooltip("魔法光晕大小")]
        [Range(0f, 100f)]
        public float MBWidth = 0.0f;
        [Tooltip("魔法光晕包围盒大小")]
        [Range(0.1f, 10.0f)]
        public float BoundSize = 1.0f;
        [Tooltip("X：控制距离多少开始缩放，Y、Z：中间过渡，W：停止缩放的距离")]
        public Vector4 MMBDisASca = new(5, 10, 15, 50);
        [Tooltip("最大放大值")]
        [Range(0.0f, 5.0f)]
        public float MMBMaxSca = 2.2f;
        [Tooltip("最小缩小值")]
        [Range(0.0f, 0.1f)]
        public float MMBMinSca = 0.1f;

        [Tooltip("模糊相关")]
        public MagicMetaballBlurSetting magicMetaballBlurSetting;

        [Tooltip("Unity内置球体Mesh")]
        public Mesh SphereMesh;
        public Material RenderMetaballMat;
        [Tooltip("魔法光晕层数")]
        [Range(0, 5)]
        public int MagicMBLayer;

        //RenderMagicMetaballShader
        [Tooltip("魔法光晕整体大小")]
        [Range(0.0f, 1.0f)]
        public float RenderToMBZTSize = 0.5f;

        [HideInInspector]
        public string MetaballCam;
        [HideInInspector]
        public List<Renderer> mbGO = new();
        [HideInInspector]
        public int mbGOCount;
        [HideInInspector]
        public bool isReComputeBounds = false;
        [HideInInspector]
        public int renderingLayerMask = 0;
        [HideInInspector]
        public bool isSignObj = false;//////////////////////////////////////////////////
        [HideInInspector]
        public Vector3[] zdyRotv3;
        [HideInInspector]
        public Vector3[] MBPos_ke;
    }
    //模糊
    [System.Serializable]
    public class MagicMetaballBlurSetting
    {
        [Tooltip("升降采样次数")]
        [Range(1, 8)]
        public int UpDownSampleNum = 6;
        [Tooltip("模糊范围")]
        [Range(0.0f, 10.0f)]
        public float BlurSize = 1;
        [Tooltip("强度")]
        [Range(-50.0f, 50.0f)]
        public float LumTint = 0;
    }
    private static readonly int ObjBloomBlurVTexID = Shader.PropertyToID("_ObjBloomBlurVTex");
    private static readonly int ObjBloomBlurHTexID = Shader.PropertyToID("_ObjBloomBlurHTex");
    private static readonly int ObjBloomBlurSizeID = Shader.PropertyToID("_ObjBloomBlurSize");
    private static readonly int ObjBloomLumTintID = Shader.PropertyToID("_ObjBloomLumTint");
    private static readonly int UpSampleBloomTexID = Shader.PropertyToID("_UpSampleBloomTex");
    private static readonly int UpSampleOldBloomTexID = Shader.PropertyToID("_UpSampleOldBloomTex");

    public MagicMetaballRenderFeature_Setting Setting = new();
    MagicMetaballRenderPass myPass;

    //MagicMetaballBaseShader
    private static readonly int MetaballWidthTintID = Shader.PropertyToID("_MetaballWidthTint");
    private static readonly int MBPerObjWtoPMatrixID = Shader.PropertyToID("_MBPerObjWtoPMatrix");
    private static readonly int MagicMetaballColorID = Shader.PropertyToID("_MagicMetaballColor");
    //Debug
    private static readonly int MetaballCmaeraTexID = Shader.PropertyToID("_MetaballCmaeraTex");

    //RenderMagicMetaballShader
    private static readonly int RenderToMBSizeID = Shader.PropertyToID("_RenderToMBSize");
    private static readonly int RenderToMBRotOSID = Shader.PropertyToID("_RenderToMBRotOS");
    //Properties

    bool SceneOrGame(in RenderingData data)
    {
        if (data.cameraData.cameraType != CameraType.Game)
        {
            return false;
        }
        return true;
    }
    bool CameraName(in RenderingData data)
    {
        if (Setting.mbGO.Count == 0 && data.cameraData.camera.name == Setting.MetaballCam || Setting.mbGOCount == 0 && data.cameraData.camera.name == Camera.main.name)
        {
            return false;
        }
        if (Setting.MetaballCam == "")
        {
            Debug.Log("摄像机名字不能为空");
            return false;
        }
        if (data.cameraData.camera.name == Setting.MetaballCam || data.cameraData.camera.CompareTag("MainCamera"))
        {
            return true;
        }
        return false;
    }

    public override void Create()
    {
        myPass = new MagicMetaballRenderPass()
        {
            debugmaincam = Setting.debugmaincam,
            OWV = Setting.OWV
        };
        myPass.renderPassEvent = Setting.PassEvent;
    }
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (!SceneOrGame(renderingData)) return;
        if (!CameraName(renderingData)) return;
        renderer.EnqueuePass(myPass);
    }
    public override void SetupRenderPasses(ScriptableRenderer renderer, in RenderingData renderingData)
    {
        if (!SceneOrGame(renderingData)) return;
        if (!CameraName(renderingData)) return;

        Shader.SetGlobalFloat(MetaballWidthTintID, Setting.MBWidth);
        Vector4 mMBDisASca;
        if (!(Setting.MMBDisASca.x < Setting.MMBDisASca.y && Setting.MMBDisASca.y < Setting.MMBDisASca.z && Setting.MMBDisASca.z < Setting.MMBDisASca.w))
        {
            mMBDisASca = new(5, 10, 15, 50);
        }
        else
        {
            mMBDisASca = Setting.MMBDisASca;
        }

        myPass.ConfigureInput(ScriptableRenderPassInput.Color);
        myPass.Setup(Setting.mats, renderer.cameraColorTargetHandle, Setting.isReComputeBounds, Setting.mbGO, Setting.MetaballCam, Setting.renderingLayerMask, Setting.BoundSize, Setting.magicMetaballBlurSetting.UpDownSampleNum, Setting.magicMetaballBlurSetting.BlurSize, Setting.magicMetaballBlurSetting.LumTint, Setting.SphereMesh, Setting.RenderMetaballMat, Setting.MagicMBLayer, Setting.RenderToMBZTSize, mMBDisASca, Setting.MMBMaxSca, Setting.MMBMinSca, Setting.isSignObj, Setting.zdyRotv3, Setting.MBPos_ke);
    }
    protected override void Dispose(bool disposing)
    {
        myPass.OnDispose();
        Setting.mbGO?.Clear();
        Setting.mbGO?.TrimExcess();
    }

    class MagicMetaballRenderPass : ScriptableRenderPass
    {
        public bool debugmaincam;
        public int OWV;

        private List<Material> Mats;
        private RTHandle SourceRT;
        private const string passTag = "MagicMetaballRenderPass";
        private ProfilingSampler m_profil = new(nameof(GrabDepthPass));
        private string MBCamName;

        private float BoundsSize;
        private Vector4 MMBDisASca;
        private float MMBMaxSca;
        private float MMBMinSca;

        private List<Renderer> renderers = new();
        private List<Renderer>[] realRenders = new List<Renderer>[9] {new(), new(), new(), new(), new(), new(), new(), new(), new()};
        private int mbRenderingLayerMask;
        private bool isrecomputebounds;
        private bool isSignObj;

        //如果魔法光晕对象用了自定义LightMode，需添加
        private static List<ShaderTagId> shadertagidList = new()
        {
            new("UniversalForward"),
            new("UniversalGBuffer"),
            new("UniversalForwardOnly"),
            new("SRPDefaultUnlit"),
            new("RainbowDashURP")
        };

        private Matrix4x4[] VP = new Matrix4x4[9];
        private Matrix4x4[] MatrOffset = new Matrix4x4[9];
        private Vector3[] zdyRotv3;
        private Vector3[] MBPos_ke;

        private List<Matrix4x4> BoundLtWMatrix = new();
        private Mesh SphereMesh;
        private Material RenderMetaballMat;
        private int MagicMBLayer;

        private MaterialPropertyBlock MMBMatProBlock;
        private float[] RenderToMBSize = new float[9];
        private float RenderToMBZTSize;
        private Vector4[] RenderToMBRotVec = new Vector4[9];

        private int InstancedNum = 0;

        //模糊
        private int UpDownSampleNum;
        private float BlurSize;
        private float LumTint;
        private RTHandle[] UpSampleRTH;
        private RTHandle[] DownSampleRTH;

        public void Setup(List<Material> mats, RTHandle col, bool isrecompute, List<Renderer> rends, string mbcamname, int rlmask, float boundsize, int updownsamplenum, float blursize, float lumtint, Mesh sphere, Material rendermetaballmat, int magicmmblayer, float renderToMBZTSize, Vector4 mmbDisASca, float mmbMaxSca, float mmbMinSca, bool issignobj, Vector3[] zdyrotv3, Vector3[] mbPos_ke)
        {
            if (mats.Count < 1)
            {
                Debug.LogError("至少要有一个材质球");
                return;
            }
            Mats = mats;
            SourceRT = col;
            MBCamName = mbcamname;

            BoundsSize = boundsize;
            MMBDisASca = mmbDisASca;
            MMBMaxSca = mmbMaxSca;
            MMBMinSca = mmbMinSca;

            renderers = rends;
            mbRenderingLayerMask = rlmask;
            isrecomputebounds = isrecompute;
            isSignObj = issignobj;

            zdyRotv3 = zdyrotv3;
            MBPos_ke = mbPos_ke;

            SphereMesh = sphere;
            RenderMetaballMat = rendermetaballmat;
            MagicMBLayer = magicmmblayer;

            RenderToMBZTSize = renderToMBZTSize;

            //模糊
            UpDownSampleNum = updownsamplenum;
            BlurSize = blursize;
            LumTint = lumtint;
        }
        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            ConfigureTarget(SourceRT);
            if (renderingData.cameraData.camera.name == MBCamName)
            {
                //变量改变时，可能会报错，除非在改变变量的时候允许执行
                //在renderers没有变的情况下（包括距离），如果两个不相交的物体现在相交了，需要执行
                //if (isrecomputebounds || Time.frameCount % 5 < 2)//后面是Debug，删
                //if (isrecomputebounds)//后面是Debug，删
                //{
                //    InstancedNum = 0;
                //    //计算矩阵
                //    ComputeMatrix(renderingData.cameraData.camera);
                //}
                InstancedNum = 0;
                ComputeMatrix();

                for (int i = 0; i < realRenders.Length; i++)
                {
                    if (realRenders[i] == null)
                    {
                        VP[i] = Matrix4x4.identity;
                        continue;
                    }
                    VP[i] = MatrOffset[i] * GL.GetGPUProjectionMatrix(renderingData.cameraData.camera.projectionMatrix, false) * renderingData.cameraData.camera.worldToCameraMatrix;
                }

                for (int i = VP.Length; i < 9; i++)
                {
                    Matrix4x4 matrix4X4 = Matrix4x4.identity;
                    VP[i] = matrix4X4;
                }
                for (int i = 0; i < Mats.Count; i++)
                {
                    Mats[i].SetMatrixArray(MBPerObjWtoPMatrixID, VP);
                }
                RenderMetaballMat.SetMatrixArray(MBPerObjWtoPMatrixID, VP);
                RenderMetaballMat.SetVectorArray(RenderToMBRotOSID, RenderToMBRotVec);

                //模糊
                UpSampleRTH = new RTHandle[UpDownSampleNum];
                DownSampleRTH = new RTHandle[UpDownSampleNum];
                for (int i = 0; i < UpDownSampleNum; i++)
                {
                    int up = Shader.PropertyToID("MMBUpSampleRTH" + i);
                    UpSampleRTH[i] = RTHandles.Alloc(up, name: "MMBUpSampleRTH" + i);
                    int down = Shader.PropertyToID("MMBDownSampleRTH" + i);
                    DownSampleRTH[i] = RTHandles.Alloc(down, name: "MMBDownSampleRTH" + i);
                }
            }
        }
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer cmd = CommandBufferPool.Get(passTag);
            cmd.Clear();
            using (new ProfilingScope(cmd, m_profil))
            {
                if (renderingData.cameraData.camera.name == MBCamName)
                {
                    cmd.SetRenderTarget(SourceRT);
                    cmd.ClearRenderTarget(RTClearFlags.All, new Color(0, 0, 0, 0));

                    //DrawRendererList
                    for (int i = 0; i < realRenders.Length; i++)
                    {
                        if (realRenders[i] == null) continue;

                        SortingCriteria criteria = SortingCriteria.CommonTransparent;
                        RenderQueueRange renderQueueRange = RenderQueueRange.all;
                        FilteringSettings filteringSettings = new(renderQueueRange)
                        {
                            renderingLayerMask = (uint)1 << mbRenderingLayerMask + i
                        };

                        DrawingSettings drawingSettings = RenderingUtils.CreateDrawingSettings(shadertagidList, ref renderingData, criteria);
                        drawingSettings.overrideMaterial = Mats[i];
                        drawingSettings.overrideMaterialPassIndex = 0;

                        RendererListParams parmas = new(renderingData.cullResults, drawingSettings, filteringSettings);
                        RendererList rendererList = context.CreateRendererList(ref parmas);
                        cmd.DrawRendererList(rendererList);
                    }
                    //模糊
                    for (int i = 0; i < UpDownSampleNum; i++)
                    {
                        int pow = (int)Mathf.Pow(2, i + 1);
                        int wid = renderingData.cameraData.camera.pixelWidth / pow;
                        int hei = renderingData.cameraData.camera.pixelHeight / pow;
                        RenderTextureDescriptor updownRTD = new(wid, hei, RenderTextureFormat.ARGBFloat, 0, 0, RenderTextureReadWrite.Linear);

                        RenderingUtils.ReAllocateIfNeeded(ref UpSampleRTH[i], updownRTD, FilterMode.Bilinear, TextureWrapMode.Clamp, name: "MMBUpSampleRTH" + i);
                        UpSampleRTH[i].rt.filterMode = FilterMode.Bilinear;
                        UpSampleRTH[i].rt.wrapMode = TextureWrapMode.Clamp;
                        RenderingUtils.ReAllocateIfNeeded(ref DownSampleRTH[i], updownRTD, FilterMode.Bilinear, TextureWrapMode.Clamp, name: "MMBDownSampleRTH" + i);
                        DownSampleRTH[i].rt.filterMode = FilterMode.Bilinear;
                        DownSampleRTH[i].rt.wrapMode = TextureWrapMode.Clamp;
                    }
                    Mats[0].SetFloat(ObjBloomBlurSizeID, BlurSize);
                    Mats[0].SetFloat(ObjBloomLumTintID, LumTint / 150 + 1);
                    cmd.SetRenderTarget(DownSampleRTH[0]);
                    cmd.ClearRenderTarget(RTClearFlags.All, new Color(0, 0, 0, 0));
                    BlurMMBTex(cmd, UpSampleRTH, DownSampleRTH, SourceRT);

                    cmd.SetRenderTarget(SourceRT);
                    cmd.SetGlobalTexture(UpSampleBloomTexID, UpSampleRTH[0]);
                    cmd.Blit(UpSampleRTH[0], SourceRT, Mats[0], 4);

                    Mats[0].SetTexture(MetaballCmaeraTexID, SourceRT);
                    RenderMetaballMat.SetTexture(MetaballCmaeraTexID, SourceRT);

                    if (RenderMetaballMat != null && BoundLtWMatrix.Count != 0)
                    {
                        MMBMatProBlock = new();
                        MMBMatProBlock?.Clear();

                        for (int i = 0; i < MagicMBLayer; i++)
                        {
                            MMBMatProBlock.SetFloatArray(RenderToMBSizeID, RenderToMBSize);

                            //离得近有问题，会被剔除
                            Graphics.DrawMeshInstanced(SphereMesh, 0, RenderMetaballMat, BoundLtWMatrix.ToArray(), InstancedNum, MMBMatProBlock, ShadowCastingMode.Off, false, 0, Camera.main);
                        }
                    }
                }
                else
                {
                    if (debugmaincam)
                    {
                        cmd.Blit(SourceRT, SourceRT, Mats[0], 5);
                    }
                }
            }
            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
            CommandBufferPool.Release(cmd);
        }
        public override void OnCameraCleanup(CommandBuffer cmd)
        {
            if (UpSampleRTH != null)
            {
                for (int i = 0; i < UpSampleRTH.Length; i++)
                {
                    UpSampleRTH[i]?.Release();
                    DownSampleRTH[i]?.Release();
                }
            }
        }
        public void OnDispose()
        {
            MMBMatProBlock?.Clear();
            BoundLtWMatrix?.Clear();
            BoundLtWMatrix?.TrimExcess();
        }

        public void ComputeMatrix()
        {
            BoundLtWMatrix?.Clear();
            BoundLtWMatrix?.TrimExcess();
            //用不到的Renders
            List<Renderer> noUseRenders = new();

            //包围盒求交
            //isSignObj
            //多个物体放一起可能仍会抖动
            //MBPos_ke
            List<Renderer>[] lingshiRealRend = new List<Renderer>[9]
            {
                new(), new(), new(),new(),new(),new(),new(),new(),new()
            };
            List<Vector3>[] lingshizdyRotv3 = new List<Vector3>[9]
            {
                new(), new(), new(),new(),new(),new(),new(),new(),new()
            };
            List<Vector3>[] lingshiMBPos_ke = new List<Vector3>[9]
            {
                new(), new(), new(),new(),new(),new(),new(),new(),new()
            };
            for (int i = 0; i < renderers.Count; i++)
            {
                List<Renderer> currRend = new() { renderers[i] };
                List<Vector3> currRot = new() { zdyRotv3[i] };
                List<Vector3> currPos = new() { MBPos_ke[i] };
                bool bxjNum = false;
                if (i == renderers.Count - 1)
                {
                    BoundsIntersectsCreatArrayList(ref lingshiRealRend, ref lingshizdyRotv3, ref lingshiMBPos_ke, currRend, currRot, currPos, i, false, -1, ref noUseRenders);
                }
                for (int it = i + 1; it < renderers.Count; it++)
                {
                    if (renderers[i].bounds.Intersects(renderers[it].bounds))
                    {
                        currRend.Add(renderers[it]);
                        currRot.Add(zdyRotv3[it]);
                        currPos.Add(zdyRotv3[it]);
                        BoundsIntersectsCreatArrayList(ref lingshiRealRend, ref lingshizdyRotv3, ref lingshiMBPos_ke, currRend, currRot, currPos, i, true, it, ref noUseRenders);
                    }
                    else if(!bxjNum)
                    {
                        bxjNum = true;
                        BoundsIntersectsCreatArrayList(ref lingshiRealRend, ref lingshizdyRotv3, ref lingshiMBPos_ke, currRend, currRot, currPos, i, false, it, ref noUseRenders);
                    }
                }
            }
            for (int i = 0; i < lingshiRealRend.Length; i++)
            {
                if (lingshiRealRend[i].Count == 0)
                {
                    lingshiRealRend[i] = null;
                }
            }
            realRenders = lingshiRealRend;

            //设置用不到的renderingLayerMask
            for (int i = 0; i < noUseRenders.Count; i++)
            {
                noUseRenders[i].renderingLayerMask = (uint)1 << 0;
            }

            for (int i = 0; i < realRenders.Length; i++)
            {
                if (realRenders[i] == null)
                {
                    RenderToMBSize[i] = 0;
                    MatrOffset[i] = Matrix4x4.identity;
                    RenderToMBRotVec[i] = new(0, 0, 0, 0);
                    continue;
                }
                InstancedNum++;

                Vector3 up_Rf = new(0, 0, 0), down_Lb = new(0, 0, 0);
                Vector3 boundcenter = new(0, 0, 0);
                Vector3 scaleOS = new(0, 0, 0);
                Vector3 mbobjRot = new(0, 0, 0);
                Vector3 mbobjPos = new(0, 0, 0);

                //计算包围盒中心与对角线
                for (int it = 0; it < realRenders[i].Count; it++)
                {
                    Vector3 lsrot = lingshizdyRotv3[i][it] / lingshizdyRotv3[i].Count;
                    mbobjRot += lsrot;
                    Vector3 lspos = lingshiMBPos_ke[i][it] / lingshiMBPos_ke[i].Count;
                    mbobjPos += lspos;

                    //Bounds nowbound = new();nowbound.size = Vector3.zero;有问题
                    Bounds nowbound = realRenders[i][it].bounds;

                    //获取当前物体下的所有Render，扩大包围盒以确保能够包含所有Render
                    Renderer[] crenderer = realRenders[i][it].GetComponentsInChildren<Renderer>();
                    foreach (Renderer rend in crenderer)
                    {
                        nowbound.Encapsulate(rend.bounds);
                    }
                    //设置renderingLayerMask
                    Renderer[] everyMetaballObj = realRenders[i][it].GetComponentsInChildren<Renderer>(true);
                    foreach (var rend in everyMetaballObj)
                    {
                        rend.renderingLayerMask = (uint)1 << mbRenderingLayerMask + i;
                    }

                    float x = nowbound.extents.x * BoundsSize;
                    float y = nowbound.extents.y * BoundsSize;
                    float z = nowbound.extents.z * BoundsSize;

                    Vector3 boundcenter_x = new(nowbound.center.x, nowbound.center.y, nowbound.center.z);
                    boundcenter += boundcenter_x;

                    //包围盒对角线
                    if (it == 0)
                    {
                        up_Rf = new Vector3(x, y, z) + boundcenter_x;
                        down_Lb = new Vector3(-x, -y, -z) + boundcenter_x;
                    }
                    else
                    {
                        Vector3 up_Rfx = new Vector3(x, y, z) + boundcenter_x;
                        up_Rf = new(Mathf.Max(up_Rf.x, up_Rfx.x), Mathf.Max(up_Rf.y, up_Rfx.y), Mathf.Max(up_Rf.z, up_Rfx.z));
                        Vector3 down_Lbx = new Vector3(-x, -y, -z) + boundcenter_x;
                        down_Lb = new(Mathf.Min(down_Lb.x, down_Lbx.x), Mathf.Min(down_Lb.y, down_Lbx.y), Mathf.Min(down_Lb.z, down_Lbx.z));
                    }

                    scaleOS = new(Mathf.Max(realRenders[i][it].transform.lossyScale.x, scaleOS.x), Mathf.Max(realRenders[i][it].transform.lossyScale.y, scaleOS.y), Mathf.Max(realRenders[i][it].transform.lossyScale.z, scaleOS.z));
                }

                boundcenter /= realRenders[i].Count;
                //壳的大小
                float duiJiaoXian = Vector3.Distance(up_Rf, down_Lb);
                RenderToMBSize[i] = duiJiaoXian;

                //用于渲染魔法光晕的壳的矩阵
                Vector4 pianyi_Y = realRenders[i][0].localToWorldMatrix.GetColumn(3);
                Vector3 offset = boundcenter - realRenders[i][0].transform.position;
                mbobjPos /= 10;
                Vector4 pingyi = new Vector4(offset.x + mbobjPos.x, offset.y + mbobjPos.y, offset.z + mbobjPos.z, 0) + pianyi_Y;

                Matrix4x4 keMtarix = Matrix4x4.identity;
                keMtarix.SetColumn(3, pingyi);

                float lingshiscale = RenderToMBZTSize * duiJiaoXian / 103.4544f;
                keMtarix.m00 = lingshiscale / scaleOS.x;
                keMtarix.m11 = lingshiscale / scaleOS.y;
                keMtarix.m22 = lingshiscale / scaleOS.z;
                BoundLtWMatrix.Add(keMtarix);

                Vector3 boundcenterVS = Camera.main.worldToCameraMatrix.MultiplyPoint(boundcenter);
                Vector3 boundcenterCS = Camera.main.projectionMatrix.MultiplyPoint(boundcenterVS);

                //构建矩阵修正噪声上移方向(local)    万向节死锁
                Quaternion mbobjRot_Q = Quaternion.Euler(mbobjRot);
                RenderToMBRotVec[i] = new(mbobjRot_Q.x, mbobjRot_Q.y, mbobjRot_Q.z, mbobjRot_Q.w);

                //距离控制缩放，自定义
                float lenScale = 1.0f;
                float lenW = Vector3.Distance(boundcenter, Camera.main.transform.position);
                Matrix4x4 uvOffsetMatr = Matrix4x4.identity;
                if (lenW < MMBDisASca.x)
                {
                    lenScale = MMBMaxSca;
                }
                else if (lenW < MMBDisASca.z)
                {
                    float linshibl = MMBMaxSca - (lenW - MMBDisASca.x) * 0.06f;
                    if (lenW <= MMBDisASca.y)
                    {
                        lenScale = linshibl;
                    }
                    else
                    {
                        lenScale = Mathf.Lerp(linshibl, 0.6f, (lenW - MMBDisASca.y) / Mathf.Max(0.001f, MMBDisASca.z - MMBDisASca.y));
                    }
                }
                else if (lenW <= Mathf.Min(Camera.main.farClipPlane, MMBDisASca.w))
                {
                    lenScale = Mathf.Lerp(0.6f, 0.001f, lenW / Mathf.Max(0.001f, MMBDisASca.w));
                }
                float sca = Mathf.Max(MMBMinSca, lenScale);

                //偏移矩阵
                int row = i % 3 - 1;
                int column = Mathf.FloorToInt(i / 3) - 1;
                //按九宫格摆放
                Matrix4x4 linshiMatr = Matrix4x4.identity;
                linshiMatr.SetRow(0, new Vector4(1, 0, 0, row * 2));
                linshiMatr.SetRow(1, new Vector4(0, -1, 0, column * 2));//xyz乘-1
                linshiMatr.SetRow(3, new Vector4(0, 0, 0, 3));//九宫格→3，和行列的格子数有关，齐次除法
                //移动到相机屏幕中心
                uvOffsetMatr.SetRow(0, new Vector4(1, 0, 0, -boundcenterCS.x));
                uvOffsetMatr.SetRow(1, new Vector4(0, 1, 0, -boundcenterCS.y));
                uvOffsetMatr.SetRow(3, new Vector4(0, 0, 0, sca));
                //合并
                MatrOffset[i] = linshiMatr * uvOffsetMatr;

                Mats[i].SetColor(MagicMetaballColorID, realRenders[i][0].gameObject.GetComponent<MagicMetaballSpaceObj>().normalColor);
            }
        }
        public void BoundsIntersectsCreatArrayList(ref List<Renderer>[] lingshiRealRend, ref List<Vector3>[] lingshizdyRotv3, ref List<Vector3>[] lingshiMBPos_ke, List<Renderer> currRend, List<Vector3> currRot, List<Vector3> currPos, int i, bool isIntersects, int it, ref List<Renderer> noUseRenders)
        {
            bool isadd = false;
            bool isisadd = false;
            //空位索引
            int[] ind = new int[9] { 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            //空位数量
            int ind_ind = 0;

            //先检查自身
            for (int j = 0; j < lingshiRealRend.Length; j++)
            {
                if (lingshiRealRend[j].Count != 0 && lingshiRealRend[j].Exists(obj => obj == renderers[i]))
                {
                    isadd = true;
                    isisadd = true;
                    if (isIntersects)
                    {
                        if (!lingshiRealRend[j].Exists(obj => obj == renderers[it]))
                        {
                            lingshiRealRend[j].Add(renderers[it]);
                            lingshizdyRotv3[j].Add(zdyRotv3[it]);
                            lingshiMBPos_ke[j].Add(MBPos_ke[it]);
                        }
                    }
                    break;
                }
                //保存空位的索引与数量
                if (lingshiRealRend[j].Count == 0)
                {
                    ind[ind_ind++] = j;
                }
            }
            //检查上一次是否存在当前Render
            if (!isadd)
            {
                for (int j = 0; j < realRenders.Length; j++)
                {
                    if (realRenders[j] != null && realRenders[j].Exists(obj => obj == renderers[i]))
                    {
                        isisadd = true;
                        if (lingshiRealRend[j].Count != 0)
                        {
                            int rendoldMinInd = int.MaxValue;
                            //找到当前位置已经存在的List<Render>中最靠前的Render的索引
                            for (int k = 0; k < lingshiRealRend[j].Count; k++)
                            {
                                rendoldMinInd = Mathf.Min(renderers.IndexOf(lingshiRealRend[j][k]), rendoldMinInd);
                            }
                            //如果当前renderers的索引更大，则不替换
                            if (rendoldMinInd < i)
                            {
                                break;
                            }
                        }
                        else
                        {
                            lingshiRealRend[j] = currRend;
                            lingshizdyRotv3[j] = currRot;
                            lingshiMBPos_ke[j] = currPos;
                            break;
                        }
                    }
                }
            }
            //寻找空位插入
            if (!isisadd)
            {
                int oldMinInd = -1;
                int ind_ind_ind = 0;
                bool nonull = false;
                //针对所有空位
                for (int j = 0; j < ind_ind; j++)
                {
                    //有空位直接赋值
                    if (realRenders[ind[j]] == null)
                    {
                        lingshiRealRend[ind[j]] = currRend;
                        lingshizdyRotv3[ind[j]] = currRot;
                        lingshiMBPos_ke[ind[j]] = currPos;
                        nonull = true;
                        break;
                    }
                    //临时索引
                    int rendoldMinInd = int.MaxValue;
                    //找到当前位置已经存在的List<Render>中最靠前的Render的索引
                    for (int k = 0; k < realRenders[ind[j]].Count; k++)
                    {
                        rendoldMinInd = Mathf.Min(renderers.IndexOf(realRenders[ind[j]][k]), rendoldMinInd);
                    }
                    //如果当前位置已经存在的List<Render>中最靠前的Render的索引更大，就使用当前索引用于之后的比较
                    if (rendoldMinInd > oldMinInd)
                    {
                        ind_ind_ind = j;
                    }
                    //找到位于最后一位的最靠前索引
                    oldMinInd = Mathf.Max(rendoldMinInd, oldMinInd);
                }
                //为True说明Render距离摄像机更近
                if (oldMinInd > i && !nonull)
                {
                    lingshiRealRend[ind[ind_ind_ind]] = currRend;
                    lingshizdyRotv3[ind[ind_ind_ind]] = currRot;
                    lingshiMBPos_ke[ind[ind_ind_ind]] = currPos;
                }
                else
                {
                    for (int itt = 0; itt < currRend.Count; itt++)
                    {
                        noUseRenders.Add(currRend[itt]);
                        noUseRenders.Add(currRend[itt]);
                    }
                }
            }
        }
        public void BlurMMBTex(CommandBuffer cmd, RTHandle[] UpSampleRTH, RTHandle[] DownSampleRTH, RTHandle SourceRT)
        {
            //下采样
            for (int i = 0; i < UpSampleRTH.Length; i++)
            {
                if (i == 0)
                {
                    cmd.SetGlobalTexture(ObjBloomBlurVTexID, SourceRT);
                    cmd.Blit(DownSampleRTH[0], UpSampleRTH[0], Mats[0], 1);
                }
                else
                {
                    cmd.SetGlobalTexture(ObjBloomBlurVTexID, DownSampleRTH[i - 1]);
                    cmd.Blit(DownSampleRTH[i - 1], UpSampleRTH[i], Mats[0], 1);
                }
                cmd.SetGlobalTexture(ObjBloomBlurHTexID, UpSampleRTH[i]);
                cmd.Blit(UpSampleRTH[i], DownSampleRTH[i], Mats[0], 2);

                cmd.Blit(DownSampleRTH[i], UpSampleRTH[i]);
            }
            //上采样
            for (int i = UpSampleRTH.Length - 2; i >= 0; i--)
            {
                if (i == UpSampleRTH.Length - 2)
                {
                    cmd.SetGlobalTexture(UpSampleOldBloomTexID, DownSampleRTH[i + 1]);
                    cmd.SetGlobalTexture(UpSampleBloomTexID, UpSampleRTH[i]);
                    cmd.Blit(DownSampleRTH[i + 1], UpSampleRTH[i], Mats[0], 3);
                }
                else
                {
                    cmd.SetGlobalTexture(UpSampleOldBloomTexID, UpSampleRTH[i + 1]);
                    cmd.SetGlobalTexture(UpSampleBloomTexID, UpSampleRTH[i]);
                    cmd.Blit(UpSampleRTH[i + 1], UpSampleRTH[i], Mats[0], 3);
                }
            }
        }
    }
}