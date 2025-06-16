using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public enum RenderQueue
{
    不透明物体 = 0,
    透明物体 = 1
}
public class GrabNormalRenderFeature : ScriptableRendererFeature
{
    //解码使用UnpackNormalOctQuadEncode(float2)
    public RenderPassEvent PassEvent = RenderPassEvent.AfterRenderingTransparents;
    //[Tooltip("是否只用于主相机")]
    //public bool isOnlyMainCamera;
    [Tooltip("是否渲染透明物体法线，当成不透明物体渲染")]
    public bool isRenderTransparent;
    [System.Serializable]
    public class GrabNormal_Setting
    {
        public Material material;
        [Tooltip("是否只渲染自定义LightMode（即下面的）的物体法线，对使用法线贴图的无效")]
        public bool isRenderSpeObj;
        [Tooltip("用于渲染的LightMode。已包含部分默认，只需添加自定义的LightMode，对使用法线贴图的无效")]
        public string[] LightModes;
        [Tooltip("用于渲染的LayerMask，对使用法线贴图的无效")]
        public LayerMask ObjLayerMask;
        [Tooltip("需要使用法线贴图的物体，如果有子物体，需要使用相同的材质球（如果使用，可以在前面的设置中去除该物体）")]
        public ObjUseNormalTex[] objUseNormalTex;
    }
    [System.Serializable]
    public class ObjUseNormalTex
    {
        public string ObjName;
        public RenderQueue renderQueue;
        public Material material;
        [Tooltip("使用第几个Pass")]
        public int passIndex;
    }
    public GrabNormal_Setting Setting = new();
    GrabNormalPass myPass;

    bool SceneOrGame(in RenderingData data)
    {
        if (data.cameraData.cameraType != CameraType.Game)
        {
            return false;
        }
        return true;
    }
    bool IsMainCmaera(in RenderingData data)
    {
        if (data.cameraData.camera.name != "MainCameraGrabNormal")
        {
            return false;
        }
        return true;
    }
    public override void Create()
    {
        myPass = new()
        {
            renderPassEvent = PassEvent,
            isRenderTransparent = isRenderTransparent,
            PassMaterial = Setting.material,
            LightModes = Setting.LightModes,
            isRenderSpeObj = Setting.isRenderSpeObj,
            ObjLayerMask = Setting.ObjLayerMask,
            ObjUseNormalTex = Setting.objUseNormalTex
        };
    }
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (!SceneOrGame(renderingData)) return;
        //if (isOnlyMainCamera && !IsMainCmaera(renderingData)) return;
        if (!IsMainCmaera(renderingData)) return;
        renderer.EnqueuePass(myPass);
    }
    public override void SetupRenderPasses(ScriptableRenderer renderer, in RenderingData renderingData)
    {
        if (!SceneOrGame(renderingData)) return;
        //if (isOnlyMainCamera && !IsMainCmaera(renderingData)) return;
        if (!IsMainCmaera(renderingData)) return;
        myPass.Setup(renderer.cameraDepthTargetHandle);
    }
    protected override void Dispose(bool disposing)
    {
        myPass.DisposeTex();
    }

    class GrabNormalPass : ScriptableRenderPass
    {
        RTHandle GrabNormalTex;
        RTHandle CameraDepthRT;
        public Material PassMaterial;
        public bool isRenderTransparent;
        private const string passTag = "GrabNormalPass";
        private static List<ShaderTagId> shadertagidList = new()
        {
            new("UniversalForward"),
            new("UniversalGBuffer"),
            new("UniversalForwardOnly"),
            new("SRPDefaultUnlit"),
        };
        public string[] LightModes;
        public bool isRenderSpeObj;
        public LayerMask ObjLayerMask;
        public ObjUseNormalTex[] ObjUseNormalTex;

        public void Setup(RTHandle depthRT)
        {
            CameraDepthRT = depthRT;
        }
        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            RenderTextureDescriptor desc = renderingData.cameraData.cameraTargetDescriptor;
            desc.depthBufferBits = 0;
            desc.colorFormat = RenderTextureFormat.ARGB32;
            RenderingUtils.ReAllocateIfNeeded(ref GrabNormalTex, desc);

            ConfigureTarget(GrabNormalTex, CameraDepthRT);
            ConfigureClear(ClearFlag.All, new(0, 0, 0, 0));
        }
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer cmd = CommandBufferPool.Get(passTag);
            cmd.Clear();

            //不透明物体
            SortingCriteria criteria = SortingCriteria.CommonOpaque;
            RenderQueueRange renderQueueRange = RenderQueueRange.opaque;
            FilteringSettings filteringSettings = new(renderQueueRange)
            {
                layerMask = ObjLayerMask.value
            };

            List<ShaderTagId> shaderTagIds = new();
            if (!isRenderSpeObj)
            {
                for (int i = 0; i < shadertagidList.Count; i++)
                {
                    ShaderTagId shaderTagId = shadertagidList[i];
                    shaderTagIds.Add(shaderTagId);
                }
            }
            for (int i = 0; i < LightModes.Length; i++)
            {
                ShaderTagId shaderTagId = new(LightModes[i]);
                shaderTagIds.Add(shaderTagId);
            }

            DrawingSettings drawingSettings = RenderingUtils.CreateDrawingSettings(shaderTagIds, ref renderingData, criteria);
            drawingSettings.overrideMaterial = PassMaterial;
            drawingSettings.overrideMaterialPassIndex = 0;

            RendererListParams parmas = new(renderingData.cullResults, drawingSettings, filteringSettings);
            RendererList rendererList = context.CreateRendererList(ref parmas);
            cmd.DrawRendererList(rendererList);
            //透明物体
            if (isRenderTransparent)
            {
                criteria = SortingCriteria.CommonTransparent;
                renderQueueRange = RenderQueueRange.transparent;
                filteringSettings = new(renderQueueRange)
                {
                    layerMask = ObjLayerMask.value
                };

                drawingSettings = RenderingUtils.CreateDrawingSettings(shaderTagIds, ref renderingData, criteria);
                drawingSettings.overrideMaterial = PassMaterial;
                drawingSettings.overrideMaterialPassIndex = 0;

                parmas = new(renderingData.cullResults, drawingSettings, filteringSettings);
                rendererList = context.CreateRendererList(ref parmas);
                cmd.DrawRendererList(rendererList);
            }
            //使用法线的物体
            if (ObjUseNormalTex != null && ObjUseNormalTex.Length > 0)
            {
                for (int i = 0; i < ObjUseNormalTex.Length; i++)
                {
                    if (ObjUseNormalTex[i].ObjName != "")
                    {
                        if (ObjUseNormalTex[i].renderQueue == 0)
                        {
                            criteria = SortingCriteria.CommonOpaque;
                            renderQueueRange = RenderQueueRange.opaque;
                        }
                        else
                        {
                            criteria = SortingCriteria.CommonTransparent;
                            renderQueueRange = RenderQueueRange.transparent;
                        }
                        filteringSettings = new(renderQueueRange);
                        ShaderTagId shaderTagId = new("GrabNormalPassTag");

                        drawingSettings = RenderingUtils.CreateDrawingSettings(shaderTagId, ref renderingData, criteria);
                        drawingSettings.overrideMaterial = ObjUseNormalTex[i].material;
                        drawingSettings.overrideMaterialPassIndex = ObjUseNormalTex[i].passIndex;

                        parmas = new(renderingData.cullResults, drawingSettings, filteringSettings);
                        rendererList = context.CreateRendererList(ref parmas);
                        cmd.DrawRendererList(rendererList);
                    }
                }
            }

            cmd.SetGlobalTexture("_GrabNormalTex", GrabNormalTex);

            //cmd.ClearRenderTarget(true, true, new(0, 0, 0, 0));
            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
            CommandBufferPool.Release(cmd);
        }
        public override void OnCameraCleanup(CommandBuffer cmd)
        {
            GrabNormalTex?.Release();
        }
        public void DisposeTex()
        {
            GrabNormalTex?.Release();
        }
    }
}