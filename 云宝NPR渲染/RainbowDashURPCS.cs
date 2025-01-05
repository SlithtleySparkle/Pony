using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

//[ExecuteAlways]
public class RainbowDashURPCS : MonoBehaviour
{
    public Transform Head;
    public Material MianMaterial;

    public Shader ErDuoHouDuShader;
    public GameObject Light;
    [Range(0f, 10f)]
    public float XiangJiJuLi = 5;
    [Range(-5f, 5f)]
    public float CameraPosOffsetY = 5;
    private Camera _camera = null;
    private RenderTexture _RT = null;
    [Range(0.0f, 10.0f)]
    public float CameraSize = 1.5f;
    [Range(0.0f, 5.0f)]
    public float HouDuMoHuChengDu;
    [Range(0.01f, 10.0f)]
    public float nearClipPane = 0;
    [Range(0.1f, 50.0f)]
    public float farClipPane = 5;

    private int MianForwardDirID;
    private int MianRightDirID;
    private int RainbowDashTouSheHouDuCam_nearCliPplaneID;
    private int RainbowDashHouDuCam_farCliPplaneID;
    private int LightSpaceRainbowDashHouDuMatrixID;
    private int MipmapLod_RainbowDashHouDuID;
    private int RainbowDashTouSheTexID;

    private void Start()
    {
        MianForwardDirID = Shader.PropertyToID("_MianForwardDir");
        MianRightDirID = Shader.PropertyToID("_MianRightDir");
        RainbowDashTouSheHouDuCam_nearCliPplaneID = Shader.PropertyToID("RainbowDashTouSheHouDuCam_nearCliPplane");
        RainbowDashHouDuCam_farCliPplaneID = Shader.PropertyToID("RainbowDashHouDuCam_farCliPplane");
        LightSpaceRainbowDashHouDuMatrixID = Shader.PropertyToID("_LightSpaceRainbowDashHouDuMatrix");
        MipmapLod_RainbowDashHouDuID = Shader.PropertyToID("_MipmapLod_RainbowDashHouDu");
        RainbowDashTouSheTexID = Shader.PropertyToID("_RainbowDashTouSheTex");

        _camera = new GameObject("LightCamera_RainbowDashURPErDuoTouShe").AddComponent<Camera>();
        _camera.depth = 2;
        _camera.clearFlags = CameraClearFlags.SolidColor;
        _camera.backgroundColor = Color.white;
        _camera.aspect = 1;
        _camera.orthographic = true;
        _camera.orthographicSize = CameraSize;
        _camera.nearClipPlane = nearClipPane;
        _camera.farClipPlane = farClipPane;
        _camera.cullingMask = 1 << 10;

        _RT = new RenderTexture(1024, 1024, 0);
        _RT.wrapMode = TextureWrapMode.Clamp;
        _RT.useMipMap = true;
        _RT.autoGenerateMips = true;
        _RT.hideFlags = HideFlags.DontSave;

        _camera.targetTexture = _RT;
        _camera.SetReplacementShader(ErDuoHouDuShader, "RenderType");
    }
    void Update()
    {
        Vector3 offsetcamerapos = new Vector3(0, CameraPosOffsetY, 0);

        _camera.nearClipPlane = nearClipPane;
        _camera.farClipPlane = farClipPane;
        _camera.orthographicSize = CameraSize;
        _camera.transform.position = Head.position - Light.transform.forward * XiangJiJuLi + offsetcamerapos;
        _camera.transform.LookAt(Head.position + offsetcamerapos);

        MianMaterial.SetVector(MianForwardDirID, Head.up);
        MianMaterial.SetVector(MianRightDirID, Head.forward);

        Shader.SetGlobalFloat(RainbowDashTouSheHouDuCam_nearCliPplaneID, nearClipPane);
        Shader.SetGlobalFloat(RainbowDashHouDuCam_farCliPplaneID, farClipPane);

        if (ErDuoHouDuShader != null)
        {
            //平台差异化处理
            Matrix4x4 LightSpaceSkinHouDuMatrix = GL.GetGPUProjectionMatrix(_camera.projectionMatrix, false) * _camera.worldToCameraMatrix;
            Shader.SetGlobalMatrix(LightSpaceRainbowDashHouDuMatrixID, LightSpaceSkinHouDuMatrix);

            Shader.SetGlobalFloat(MipmapLod_RainbowDashHouDuID, HouDuMoHuChengDu);
            Shader.SetGlobalTexture(RainbowDashTouSheTexID, _RT);
        }
    }
}