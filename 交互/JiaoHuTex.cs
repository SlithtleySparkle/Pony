using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JiaoHuTex : MonoBehaviour
{
    public Shader JiaoHuShader;
    private Camera _camera;
    private RenderTexture _RT = null;
    private RenderTexture _LastRT = null;

    public Transform JiaoHuGrassFanWei;
    [Range(0.0f, 50.0f)]
    public float RTDaXiao = 3f;
    [Range(0.000f, 1.000f)]
    public float FanWei = 0.3f;
    [Range(0.0065f, 0.1000f)]
    public float ShuaiJianSpeed = 0.01f;

    public Shader JiaoHuGrassPostShader;
    private Material JiaoHuGrassPostMaterial;
    public Transform PlayerPos;

    private int GrassVPMatrixID;
    private int JiaoHuGrassShaderVPMatrixID;
    private int GrassJiaoHuTexID;
    private int JiaoHuGrassLastRTID;
    private int ShuaiJianSpeedID;
    private int lastPosID;
    private int JiaoHuGrassPostShaderVPMatrixID;
    private int JiaoHuGrassTexFanWeiID;

    void Start()
    {
        _camera = gameObject.GetComponent<Camera>();
        GrassVPMatrixID = Shader.PropertyToID("_GrassVPMatrix");
        JiaoHuGrassShaderVPMatrixID = Shader.PropertyToID("_JiaoHuGrassShaderVPMatrix");
        GrassJiaoHuTexID = Shader.PropertyToID("_GrassJiaoHuTex");
        JiaoHuGrassLastRTID = Shader.PropertyToID("_JiaoHuGrassLastRT");
        ShuaiJianSpeedID = Shader.PropertyToID("_ShuaiJianSpeed");
        lastPosID = Shader.PropertyToID("_JiaoHuGrasslastPos");
        JiaoHuGrassPostShaderVPMatrixID = Shader.PropertyToID("_JiaoHuGrassPostShaderVPMatrix");
        JiaoHuGrassTexFanWeiID = Shader.PropertyToID("_JiaoHuGrassTexFanWei");

        JiaoHuGrassPostMaterial = new Material(JiaoHuGrassPostShader);

        _RT = new RenderTexture(1024, 1024, 0);
        _RT.wrapMode = TextureWrapMode.Clamp;
        _RT.hideFlags = HideFlags.DontSave;

        _LastRT = new RenderTexture(1024, 1024, 0);
        _LastRT.wrapMode = TextureWrapMode.Clamp;
        _LastRT.hideFlags = HideFlags.DontSave;

        _camera.targetTexture = _RT;
        _camera.SetReplacementShader(JiaoHuShader, "RenderType");
    }
    void Update()
    {
        JiaoHuGrassFanWei.localScale = new Vector3(RTDaXiao, RTDaXiao, RTDaXiao);
        _camera.orthographicSize = RTDaXiao;
        if (JiaoHuShader != null)
        {
            //平台差异化处理
            Matrix4x4 GrassJiaoHuMatrix = GL.GetGPUProjectionMatrix(_camera.projectionMatrix, false) * _camera.worldToCameraMatrix;

            Shader.SetGlobalMatrix(GrassVPMatrixID, GrassJiaoHuMatrix);//要用Shader.SetGlobalxxx
            Shader.SetGlobalMatrix(JiaoHuGrassShaderVPMatrixID, GrassJiaoHuMatrix);
            Shader.SetGlobalMatrix(JiaoHuGrassPostShaderVPMatrixID, GrassJiaoHuMatrix);
            Shader.SetGlobalTexture(GrassJiaoHuTexID, _RT);
            Shader.SetGlobalTexture(JiaoHuGrassLastRTID, _RT);
            Shader.SetGlobalFloat(ShuaiJianSpeedID, ShuaiJianSpeed);
            Shader.SetGlobalFloat(JiaoHuGrassTexFanWeiID, FanWei);

            //debug
            Shader.SetGlobalMatrix("_JiaoHuGrassShaderawdwaVPMatrix", GrassJiaoHuMatrix);
            Shader.SetGlobalTexture("_MainTexasdwa", _RT);
        }
    }
    private void OnRenderImage(RenderTexture src, RenderTexture dest)
    {
        if(JiaoHuGrassPostMaterial != null)
        {

            Graphics.Blit(src, _LastRT, JiaoHuGrassPostMaterial);
            Graphics.Blit(_LastRT, dest);

            //debug
            Shader.SetGlobalTexture("_JiaoHuPostShaderDebug", _LastRT);
        }
        else
        {
            Graphics.Blit(src, dest);
        }
        Shader.SetGlobalVector(lastPosID, new Vector4(PlayerPos.position.x, PlayerPos.position.y, PlayerPos.position.z, 1));
    }
}
