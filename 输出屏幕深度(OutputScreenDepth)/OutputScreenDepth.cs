using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[Tooltip("输出深度，需指定相机，将渲染的结果输出到一个或多个纹理，需添加相关C#脚本。输出的深度图与官方的相同")]
public class OutputScreenDepth : ScriptableRendererFeature
{
    public enum isRenderTrans
    {
        不渲染透明物体 = 0,
        渲染透明物体 = 1,
        只渲染透明物体 = 2
    }

    public Material material;
    [Tooltip("输出的结果是否要与官方抓取的深度相同(_ZBufferParams)。为否：输出视角空间下的-z值。需加上#pragma shader_feature_local_fragment _ _SAME_AS_UNITY_RF")]
    //和官方相同：采样解码后，与LinearEyeDepth(视角空间-z或裁剪空间w, _ZBufferParams)比较
    //      不同：采样解码后，直接与视角空间-z或裁剪空间w比较
    public bool isSameAsUnityRF;
    [Tooltip("要用的摄像机和贴图，如果既要用于主相机又要用于其他相机，在这里加上主相机即可（但不建议用主相机，输出需要使用RenderTexture）。可重复添加相同名字的相机，但必须连续")]
    public ListCameraAndTexture[] cameraAndTexture;
    [System.Serializable]
	public class ListCameraAndTexture
	{
		public RenderPassEvent PassEvent = RenderPassEvent.AfterRenderingTransparents;
		public string m_CameraNames;
        [Tooltip("是否渲染透明物体，当成不透明物体渲染")]
        public isRenderTrans isRenderTransparent;
        [Tooltip("用于渲染的LightMode。已包含部分默认，只需添加自定义的LightMode，对需特殊渲染的物体无效")]
        public string[] LightModes;
        [Tooltip("用于渲染的LayerMask，对需特殊渲染的物体无效")]
        public LayerMask ObjLayerMask;
        [Tooltip("输出贴图的名字。如果为空，则默认是“_GrabDepthTex”，会与GrabDepthRenderFeature冲突")]
		public string[] m_ShadowMapNames;
        [Tooltip("是否在渲染特定物体前清除之前的渲染结果")]
        public bool isClearForObj;
        [Tooltip("需要特定Shader渲染的物体，所有物体需要使用相同的材质球")]
        public ObjCustomRenderDepth[] objCustomRenderDepths;
    }
	[System.Serializable]
	public class ObjCustomRenderDepth
	{
		public RenderQueue renderQueue;
		public Material material;
		[Tooltip("使用第几个Pass，相关Pass需添加Tag“OutputScreenDepthTag”")]
		public int passIndex;
	}
    OutputScreenDepthPass myPass;
	private int currentCameraNameID;
    private static readonly int GrabDepthTexID = Shader.PropertyToID("_GrabDepthTex");

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
		if (cameraAndTexture.Length == 0)
		{
			Debug.Log("'ListCameraAndTexture'不能为空");
			return false;
		}
		else
		{
			for (int i = 0; i < cameraAndTexture.Length; i++)
			{
				if (i > 0)
				{
					if (cameraAndTexture[i].m_CameraNames == cameraAndTexture[i - 1].m_CameraNames)
					{
                        continue;
                    }
				}
				//摄像机
				if (cameraAndTexture[i].m_CameraNames == "")
				{
					Debug.Log("摄像机名字不能为空");
					continue;
				}
				if (data.cameraData.camera.name != cameraAndTexture[i].m_CameraNames)
				{
					continue;
				}
				currentCameraNameID = i;
				return true;
			}
			return false;
		}
	}
	public override void Create()
	{
		myPass = new();
	}
	public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
	{
		//if (!SceneOrGame(renderingData)) return;
		if (!CameraName(renderingData)) return;
		renderer.EnqueuePass(myPass);
	}
	public override void SetupRenderPasses(ScriptableRenderer renderer, in RenderingData renderingData)
	{
		//if (!SceneOrGame(renderingData)) return;
		if (!CameraName(renderingData)) return;
        myPass.renderPassEvent = cameraAndTexture[currentCameraNameID].PassEvent;
        myPass.Setup(material, renderer.cameraDepthTargetHandle, isSameAsUnityRF, cameraAndTexture, currentCameraNameID);
	}
	protected override void Dispose(bool disposing)
    {
        myPass.PassDispose();
    }

    class OutputScreenDepthPass : ScriptableRenderPass
	{
		private Material PassMaterial;
        private RTHandle[] PassSource;
        private RTHandle CameraDepthRT;
		private const string passTag = "OutputScreenDepthPass";
        private static List<ShaderTagId> shadertagidList = new()
        {
            new("UniversalForward"),
            new("UniversalGBuffer"),
            new("UniversalForwardOnly"),
            new("SRPDefaultUnlit"),
        };

        private bool isSameAsUnityRF;
        private ListCameraAndTexture[] m_CameraAndTextures;
		private int currentCameraNameID;

		public void Setup(Material material, RTHandle deprt, bool issameAsUnityRF, ListCameraAndTexture[] cameraandtexture, int itt)
		{
			if (material == null)
			{
				Debug.LogError("没有材质球");
				return;
			}
			PassMaterial = material;
			CameraDepthRT = deprt;
            isSameAsUnityRF = issameAsUnityRF;
            m_CameraAndTextures = cameraandtexture;
			currentCameraNameID = itt;
        }
		public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
		{
            PassSource = new RTHandle[m_CameraAndTextures.Length];
            RenderTextureDescriptor desc = renderingData.cameraData.cameraTargetDescriptor;
            desc.depthBufferBits = 0;
            desc.colorFormat = RenderTextureFormat.RFloat;
            for (int i = 0; i < PassSource.Length; i++)
            {
                RenderingUtils.ReAllocateIfNeeded(ref PassSource[i], desc);
                PassSource[i].rt.wrapMode = TextureWrapMode.Clamp;
            }

            ConfigureTarget(PassSource[0], CameraDepthRT);
        }
		public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
		{
            CommandBuffer cmd = CommandBufferPool.Get(passTag);
            cmd.Clear();

            Color clearCol;
            if (isSameAsUnityRF)
            {
                PassMaterial?.EnableKeyword("_SAME_AS_UNITY_RF");
                clearCol = new(0, 0, 0, 0);
            }
            else
            {
                PassMaterial?.DisableKeyword("_SAME_AS_UNITY_RF");
                clearCol = new(float.MaxValue, float.MaxValue, float.MaxValue, float.MaxValue);
            }
            for (int i = currentCameraNameID; i < m_CameraAndTextures.Length; i++)
            {
                if (m_CameraAndTextures[i].m_CameraNames != m_CameraAndTextures[currentCameraNameID].m_CameraNames) break;
                if (i != currentCameraNameID)
                {
                    cmd.SetRenderTarget(PassSource[i - currentCameraNameID], CameraDepthRT);
                }
                cmd.ClearRenderTarget(RTClearFlags.All, clearCol);

                DrawRenderList(context, ref renderingData, cmd, i);
                //设置贴图
                if (m_CameraAndTextures[i].m_ShadowMapNames.Length == 0)
                {
                    Shader.SetGlobalTexture(GrabDepthTexID, PassSource[i - currentCameraNameID]);
                }
                else
                {
                    for (int it = 0; it < m_CameraAndTextures[i].m_ShadowMapNames.Length; it++)
                    {
                        Shader.SetGlobalTexture(m_CameraAndTextures[i].m_ShadowMapNames[it], PassSource[i - currentCameraNameID]);
                    }
                }
            }

            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
            CommandBufferPool.Release(cmd);
        }
        public override void OnCameraCleanup(CommandBuffer cmd)
        {
            PassDispose();//会导致RTHandle设置失效，在FrameDebugger查看
        }
        public void PassDispose()
		{
            if (PassSource != null)
            {
                for (int i = 0; i < PassSource.Length; i++)
                {
                    PassSource[i]?.Release();
                }
            }
        }
		public void DrawRenderList(ScriptableRenderContext context, ref RenderingData renderingData, CommandBuffer cmd, int o)
		{
            SortingCriteria criteria;
            RenderQueueRange renderQueueRange;
            FilteringSettings filteringSettings;
            DrawingSettings drawingSettings;
            RendererListParams parmas;
            RendererList rendererList;
            List<ShaderTagId> shaderTagIds = new();
            if (!m_CameraAndTextures[o].isClearForObj)
            {
                for (int i = 0; i < shadertagidList.Count; i++)
                {
                    ShaderTagId shaderTagId = shadertagidList[i];
                    shaderTagIds.Add(shaderTagId);
                }
                for (int i = 0; i < m_CameraAndTextures[o].LightModes.Length; i++)
                {
                    ShaderTagId shaderTagId = new(m_CameraAndTextures[o].LightModes[i]);
                    shaderTagIds.Add(shaderTagId);
                }
                //不透明物体
                if ((int)m_CameraAndTextures[o].isRenderTransparent != 2)
                {
                    criteria = SortingCriteria.CommonOpaque;
                    renderQueueRange = RenderQueueRange.opaque;
                    filteringSettings = new(renderQueueRange)
                    {
                        layerMask = m_CameraAndTextures[o].ObjLayerMask
                    };

                    drawingSettings = RenderingUtils.CreateDrawingSettings(shaderTagIds, ref renderingData, criteria);
                    drawingSettings.overrideMaterial = PassMaterial;
                    drawingSettings.overrideMaterialPassIndex = 0;

                    parmas = new(renderingData.cullResults, drawingSettings, filteringSettings);
                    rendererList = context.CreateRendererList(ref parmas);
                    cmd.DrawRendererList(rendererList);
                }
                //透明物体
                if ((int)m_CameraAndTextures[o].isRenderTransparent != 0)
                {
                    criteria = SortingCriteria.CommonTransparent;
                    renderQueueRange = RenderQueueRange.transparent;
                    filteringSettings = new(renderQueueRange)
                    {
                        layerMask = m_CameraAndTextures[o].ObjLayerMask
                    };

                    drawingSettings = RenderingUtils.CreateDrawingSettings(shaderTagIds, ref renderingData, criteria);
                    drawingSettings.overrideMaterial = PassMaterial;
                    drawingSettings.overrideMaterialPassIndex = 0;

                    parmas = new(renderingData.cullResults, drawingSettings, filteringSettings);
                    rendererList = context.CreateRendererList(ref parmas);
                    cmd.DrawRendererList(rendererList);
                }
            }
            //需特殊渲染的物体
            if (m_CameraAndTextures[o].objCustomRenderDepths != null && m_CameraAndTextures[o].objCustomRenderDepths.Length > 0)
            {
                for (int i = 0; i < m_CameraAndTextures[o].objCustomRenderDepths.Length; i++)
                {
                    if (m_CameraAndTextures[o].objCustomRenderDepths[i].renderQueue == 0)
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
                    ShaderTagId shaderTagId = new("OutputScreenDepthTag");

                    drawingSettings = RenderingUtils.CreateDrawingSettings(shaderTagId, ref renderingData, criteria);
                    drawingSettings.overrideMaterial = m_CameraAndTextures[o].objCustomRenderDepths[i].material;
                    drawingSettings.overrideMaterialPassIndex = m_CameraAndTextures[o].objCustomRenderDepths[i].passIndex;

                    parmas = new(renderingData.cullResults, drawingSettings, filteringSettings);
                    rendererList = context.CreateRendererList(ref parmas);
                    cmd.DrawRendererList(rendererList);
                }
            }
        }
	}
}