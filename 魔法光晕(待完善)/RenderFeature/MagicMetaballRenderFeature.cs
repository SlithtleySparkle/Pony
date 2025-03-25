using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RendererUtils;
using UnityEngine.Rendering.Universal;

public class MagicMetaballRenderFeature : ScriptableRendererFeature
{
    [System.Serializable]
    public class MagicMetaballRenderFeature_Setting
    {
        public RenderPassEvent PassEvent = RenderPassEvent.AfterRenderingTransparents;
        public Material material;
        [Tooltip("用于渲染魔法光晕的摄像机")]
        public string MetaballCam;
        [Tooltip("魔法光晕流动速度")]
        public float FluSpeed;
        [Tooltip("魔法光晕扰动强度")]
        [Range(0.0f, 1.0f)]
        public float RaoDongTint;
    }
    public MagicMetaballRenderFeature_Setting Setting = new();
    MagicMetaballRenderPass myPass;
    private static readonly int MetaballCmaeraTexID = Shader.PropertyToID("_MetaballCmaeraTex");
    private static readonly int MetaballFluSpeedID = Shader.PropertyToID("_MetaballFluSpeed");
    private static readonly int MetaballRaoDongTintID = Shader.PropertyToID("_MetaballRaoDongTint");

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
        myPass = new MagicMetaballRenderPass();
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
        myPass.ConfigureInput(ScriptableRenderPassInput.Color);
        myPass.Setup(Setting.material, renderer.cameraColorTargetHandle, Setting.MetaballCam, Setting.FluSpeed, Setting.RaoDongTint);
    }
    protected override void Dispose(bool disposing)
    {
        myPass.OnDispose();
    }

    class MagicMetaballRenderPass : ScriptableRenderPass
    {
        private Material PassMaterial;
        private RTHandle tempPassSource;
        private const string passTag = "MagicMetaballRenderPass";
        private string MBCamName;
        private float fluSpeed;
        private float rdTint;

        public void Setup(Material material, RTHandle col, string mbcamname, float fluspeed, float rdtint)
        {
            if (material == null)
            {
                Debug.LogError("没有材质球");
                return;
            }
            PassMaterial = material;
            tempPassSource = col;
            MBCamName = mbcamname;
            fluSpeed = fluspeed;
            rdTint = rdtint;
        }
        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            ConfigureTarget(tempPassSource);
        }
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer cmd = CommandBufferPool.Get(passTag);
            cmd.Clear();
            if (renderingData.cameraData.camera.name == MBCamName)
            {
                PassMaterial.SetFloat(MetaballFluSpeedID, fluSpeed);
                PassMaterial.SetFloat(MetaballRaoDongTintID, rdTint);
                cmd.Blit(tempPassSource, tempPassSource, PassMaterial, 0);
                PassMaterial.SetTexture(MetaballCmaeraTexID, tempPassSource);
            }
            else if (renderingData.cameraData.camera.CompareTag("MainCamera"))
            {
                cmd.Blit(tempPassSource, tempPassSource, PassMaterial, 1);
            }

            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
            CommandBufferPool.Release(cmd);
        }
        public override void OnCameraCleanup(CommandBuffer cmd)
        {
            tempPassSource?.Release();
        }
        public void OnDispose()
        {
            tempPassSource?.Release();
        }
    }
}