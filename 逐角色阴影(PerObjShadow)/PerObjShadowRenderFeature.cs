using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[Tooltip("渲染结果：R：和官方相同的深度，G：顶点色r（透明度）， B：无透明物体的官方深度, A：仅角色的官方深度。\nR用于自身PCSS，G用于透明阴影，B用于自身阴影，A用于普通的PCSS")]
public class PerObjShadowRenderFeature : ScriptableRendererFeature
{
	public RenderPassEvent PassEvent = RenderPassEvent.AfterRenderingTransparents;
	//解码用DecodeLogarithmicDepthGeneralized(float d, float4(1, 1, 0, 0))
    public Material[] materials;
	[Tooltip("用于渲染的LightMode。已包含部分默认，只需添加自定义的LightMode，对需特殊渲染的物体无效")]
	public string[] LightModes;
	[Tooltip("R：输出深度（包含半透明物体），G：输出透明度")]
	public string[] m_ShadowMapNames;
    [Tooltip("逐角色刘海阴影ScriptableObject")]
    public PerHairShadowMatSO PerHairShadowSO;

    public PerObjShadowLightCameraPro perObjShadowLightCameraPro;
    [System.Serializable]
	public class PerObjShadowLightCameraPro
	{
		[Range(-3.0f, 3.0f)]
		public float Size = 0;
        [Range(-5.0f, 5.0f)]
        public float farPlane = 0;
        [Range(-5.0f, 5.0f)]
        public float position = 0;
    }
	[Tooltip("是否接受投影")]
	public bool UseTouYing = true;

    [Tooltip("包围盒大小")]
	[Range(0.1f, 5.0f)]
    public float BoundsSize = 1;

    [HideInInspector]
	public List<Renderer[]> renderers = new();
    [HideInInspector]
    public List<Renderer[]> hairRends = new();
    [HideInInspector]
	public int mbRenderingLayerMask;
    [HideInInspector]
    public Vector3 lightForwardDir;
    [HideInInspector]
    public bool onlyHairMask;

    private static readonly int PerObjShadowWtoPMatrID = Shader.PropertyToID("_PerObjShadowWtoPMatr");
    private static readonly int PerObjShadowWtoPMatr_hairID = Shader.PropertyToID("_PerObjShadowWtoPMatr_hair");
	private static readonly int UsePerObjShadowWtoPMatrID = Shader.PropertyToID("_UsePerObjShadowWtoPMatr");
	private static readonly int UsePerObjShadowWtoPMatr_hairID = Shader.PropertyToID("_UsePerObjShadowWtoPMatr_hair");
	private static readonly int PerObjNumberID = Shader.PropertyToID("_PerObjNumber");
	private static readonly int PerObjNumSqrtID = Shader.PropertyToID("_PerObjNumSqrt");
	private static readonly int PCSSInteNumID = Shader.PropertyToID("_PCSSInteNum");//PBR

	PerObjShadowRenderPass myPass;

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
		if (data.cameraData.camera.name == "MainLightCamera")
		{
			return true;
		}
		if (GameObject.Find("MainLightCamera") == null)
		{
			Debug.Log("检查灯光是否挂载了“PerObjShadow”脚本");
			return false;
		}
		return false;
	}

	public override void Create()
	{
		myPass = new();
		myPass.renderPassEvent = PassEvent;
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
		myPass.Setup(materials, renderer.cameraDepthTargetHandle, renderers, hairRends, LightModes, m_ShadowMapNames, mbRenderingLayerMask, BoundsSize, lightForwardDir, perObjShadowLightCameraPro, UseTouYing, PerHairShadowSO, onlyHairMask);
	}
	protected override void Dispose(bool disposing)
	{
		myPass.PassDispose();
        PerHairShadowSO.PassMats = null;
    }

	class PerObjShadowRenderPass : ScriptableRenderPass
	{
		private Material[] PassMats;

        private bool onlyHairMask;
        private PerHairShadowMatSO PerHairShadowSO;

        private RTHandle PassSource;
		private RTHandle CameraDepthRT;
		private const string passTag = "PerObjShadowRenderPass";
		private static List<ShaderTagId> shadertagidList = new()
		{
			new("UniversalForward"),
			new("UniversalForwardOnly"),
			new("SRPDefaultUnlit"),
		};
		private List<Renderer[]> renderers = new();
		private List<Renderer[]> hairRends = new();

		private string[] LightModes;
		private string[] m_ShadowMapNames;
		private PerObjShadowLightCameraPro perObjShadowLightCameraPro;
		public bool UseTouYing;

        private int perObjNumSqrt;
		private Matrix4x4[] VP = new Matrix4x4[9];
		private Matrix4x4[] VP_hair = new Matrix4x4[9];

        private float BoundsSize;

        private int mbRenderingLayerMask;
        private Vector3 lightForwardDir;

		public void Setup(Material[] mats, RTHandle deprt, List<Renderer[]> rends, List<Renderer[]> rends2, string[] lightModes, string[] m_shadowMapNames, int mbrenderingLayerMask, float boundsSize, Vector3 lightforwardDir, PerObjShadowLightCameraPro perobjShadowLightCameraPro, bool useTouYing, PerHairShadowMatSO perHairShadowSO, bool onlyhairMask)
		{
			if (mats == null)
			{
				Debug.LogError("没有材质球");
				return;
			}
			if (rends.Count == 0) return;
            PerHairShadowSO = perHairShadowSO;
            onlyHairMask = onlyhairMask;
            PassMats = mats;

            CameraDepthRT = deprt;
			renderers = rends;
            hairRends = rends2;

            LightModes = lightModes;
			m_ShadowMapNames = m_shadowMapNames;
			perObjShadowLightCameraPro = perobjShadowLightCameraPro;
			UseTouYing = useTouYing;

            perObjNumSqrt = Mathf.CeilToInt(Mathf.Sqrt(renderers.Count));
			if (perObjNumSqrt == 1)
			{
				Shader.EnableKeyword("_PEROBJSHADOW_SIGN");
            }
			else
			{
                Shader.DisableKeyword("_PEROBJSHADOW_SIGN");
            }
			Shader.SetGlobalInt(PerObjNumSqrtID, perObjNumSqrt);

			BoundsSize = boundsSize;

            mbRenderingLayerMask = mbrenderingLayerMask;
			lightForwardDir = lightforwardDir;
        }
		public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
		{
            RenderTextureDescriptor desc = renderingData.cameraData.cameraTargetDescriptor;
			desc.depthBufferBits = 0;
			desc.colorFormat = RenderTextureFormat.ARGBFloat;
			RenderingUtils.ReAllocateIfNeeded(ref PassSource, desc);
            PassSource.rt.wrapMode = TextureWrapMode.Clamp;
            ConfigureTarget(PassSource, CameraDepthRT);
            ConfigureClear(ClearFlag.All, new(0, 1, float.MaxValue, 0));

            ComputeMatrix(ref renderingData);
            if (PerHairShadowSO != null)
            {
                PerHairShadowSO.PassMats = PassMats;
            }
            else if (onlyHairMask)
            {
                Debug.Log("PerHairShadowSO为空");
            }
        }
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
		{
			CommandBuffer cmd = CommandBufferPool.Get(passTag);
			cmd.Clear();

			for (int i = 0; i < renderers.Count; i++)
			{
				DrawRendererList(context, ref renderingData, cmd, i);
			}
			//设置贴图
			for (int i = 0; i < m_ShadowMapNames.Length; i++)
			{
				Shader.SetGlobalTexture(m_ShadowMapNames[i], PassSource);
            }

            context.ExecuteCommandBuffer(cmd);
			cmd.Clear();
			CommandBufferPool.Release(cmd);
		}
		public void PassDispose()
		{
			PassSource?.Release();
        }

		public void DrawRendererList(ScriptableRenderContext context, ref RenderingData renderingData, CommandBuffer cmd, int o)
		{
            List<ShaderTagId> shaderTagIdsOpaque = new();
            List<ShaderTagId> shaderTagIdsTransparent = new();
            for (int i = 0; i < LightModes.Length; i++)
            {
                ShaderTagId shaderTagId = new(LightModes[i]);
                shaderTagIdsOpaque.Add(shaderTagId);
                shaderTagIdsTransparent.Add(shaderTagId);
            }
            for (int i = 0; i < shadertagidList.Count; i++)
            {
                ShaderTagId shaderTagId = shadertagidList[i];
                shaderTagIdsOpaque.Add(shaderTagId);
            }

            RenderQueueRange renderQueueRangeO = RenderQueueRange.opaque;
            RenderQueueRange renderQueueRangeT = RenderQueueRange.transparent;
			FilteringSettings filteringSettingsOpaque;
			FilteringSettings filteringSettingsTransparent;
			FilteringSettings filteringSettingsPerO;
			FilteringSettings filteringSettingsPerT;
            if (UseTouYing)
			{
                filteringSettingsOpaque = new(renderQueueRangeO);
                filteringSettingsTransparent = new(renderQueueRangeT);
            }
			else
			{
                filteringSettingsOpaque = new(renderQueueRangeO)
				{
					renderingLayerMask = (uint)1 << mbRenderingLayerMask + o
				};
                filteringSettingsTransparent = new(renderQueueRangeT)
				{
                    renderingLayerMask = (uint)1 << mbRenderingLayerMask + o
                };
            }
            filteringSettingsPerO = new(renderQueueRangeO)
            {
                renderingLayerMask = (uint)1 << mbRenderingLayerMask + o
            };
            filteringSettingsPerT = new(renderQueueRangeT)
            {
                renderingLayerMask = (uint)1 << mbRenderingLayerMask + o
            };

            //A
            SortingCriteria criteria = SortingCriteria.CommonOpaque;
            DrawingSettings drawingSettings = RenderingUtils.CreateDrawingSettings(shaderTagIdsTransparent, ref renderingData, criteria);
            drawingSettings.overrideMaterial = PassMats[o];
            drawingSettings.overrideMaterialPassIndex = 2;
            RendererListParams parmas = new(renderingData.cullResults, drawingSettings, filteringSettingsPerO);
            RendererList rendererList = context.CreateRendererList(ref parmas);
            cmd.DrawRendererList(rendererList);

            criteria = SortingCriteria.CommonTransparent;
            drawingSettings = RenderingUtils.CreateDrawingSettings(shaderTagIdsTransparent, ref renderingData, criteria);
            drawingSettings.overrideMaterial = PassMats[o];
            drawingSettings.overrideMaterialPassIndex = 2;
            parmas = new(renderingData.cullResults, drawingSettings, filteringSettingsPerT);
            rendererList = context.CreateRendererList(ref parmas);
            cmd.DrawRendererList(rendererList);

            cmd.ClearRenderTarget(true, false, new(0, 1, float.MaxValue, 0));

            //B
            criteria = SortingCriteria.CommonOpaque;
            drawingSettings = RenderingUtils.CreateDrawingSettings(shaderTagIdsOpaque, ref renderingData, criteria);
            drawingSettings.overrideMaterial = PassMats[o];
            drawingSettings.overrideMaterialPassIndex = 1;
            parmas = new(renderingData.cullResults, drawingSettings, filteringSettingsOpaque);
            rendererList = context.CreateRendererList(ref parmas);
            cmd.DrawRendererList(rendererList);

            cmd.ClearRenderTarget(true, false, new(0, 1, float.MaxValue, 0));

            //RG
            drawingSettings = RenderingUtils.CreateDrawingSettings(shaderTagIdsOpaque, ref renderingData, criteria);
            drawingSettings.overrideMaterial = PassMats[o];
            drawingSettings.overrideMaterialPassIndex = 0;
            parmas = new(renderingData.cullResults, drawingSettings, filteringSettingsOpaque);
            rendererList = context.CreateRendererList(ref parmas);
            cmd.DrawRendererList(rendererList);

            criteria = SortingCriteria.CommonTransparent;
            drawingSettings = RenderingUtils.CreateDrawingSettings(shaderTagIdsTransparent, ref renderingData, criteria);
            drawingSettings.overrideMaterial = PassMats[o];
            drawingSettings.overrideMaterialPassIndex = 0;
            parmas = new(renderingData.cullResults, drawingSettings, filteringSettingsTransparent);
            rendererList = context.CreateRendererList(ref parmas);
            cmd.DrawRendererList(rendererList);
        }
        public void ComputeMatrix(ref RenderingData renderingData)
		{
            float[] maxPerBoundSize = new float[renderers.Count];
            Vector3 upZ_Rf = Vector3.zero, upZ_Rb = Vector3.zero, upZ_Lf = Vector3.zero, upZ_Lb = Vector3.zero, downZ_Rf = Vector3.zero, downZ_Rb = Vector3.zero, downZ_Lf = Vector3.zero, downZ_Lb = Vector3.zero;
			List<Matrix4x4> offsetMatr = new();
            Vector3[] boundcenterCS = new Vector3[renderers.Count];

            ComputeMatrix_OtherMatr(renderers, ref renderingData, ref maxPerBoundSize, ref boundcenterCS, ref upZ_Rf, ref upZ_Rb, ref upZ_Lf, ref upZ_Lb, ref downZ_Rf, ref downZ_Rb, ref downZ_Lf, ref downZ_Lb);
            //DrawDebug(upZ_Rf, upZ_Rb, upZ_Lf, upZ_Lb, downZ_Rf, downZ_Rb, downZ_Lf, downZ_Lb);
            //包围盒六个角
            Matrix4x4 WtLMatr = renderingData.cameraData.camera.transform.worldToLocalMatrix;
            float left = 0, right = 0, up = 0, down = 0, forward = 0, back = 0;
            ComputeMatrix_SixCorners(true, WtLMatr, upZ_Rf, upZ_Rb, upZ_Lf, upZ_Lb, downZ_Rf, downZ_Rb, downZ_Lf, downZ_Lb, ref left, ref right, ref up, ref down, ref forward, ref back);

            //修改灯光相机属性
            float xCam = right - left;
			float yCam = up - down;
			float zCam = forward - back;
            renderingData.cameraData.camera.transform.position = (upZ_Rf + downZ_Lb) / 2 - lightForwardDir * (zCam / 2 + perObjShadowLightCameraPro.position);
            float camSize = Mathf.Max(xCam, yCam) / 2 + perObjShadowLightCameraPro.Size;
            renderingData.cameraData.camera.orthographicSize = camSize;
			float farclipplane = Mathf.Max(renderingData.cameraData.camera.nearClipPlane + 0.01f, zCam + perObjShadowLightCameraPro.farPlane + perObjShadowLightCameraPro.position);
			renderingData.cameraData.camera.farClipPlane = farclipplane;

            //计算偏移矩阵
            ComputeMatrix_OffsetMatr(renderers, ref offsetMatr, boundcenterCS, maxPerBoundSize, camSize);

            //刘海部分
            float[] maxPerBoundSize_hair = new float[hairRends.Count];
            Vector3 upZ_Rf_hair = Vector3.zero, upZ_Rb_hair = Vector3.zero, upZ_Lf_hair = Vector3.zero, upZ_Lb_hair = Vector3.zero, downZ_Rf_hair = Vector3.zero, downZ_Rb_hair = Vector3.zero, downZ_Lf_hair = Vector3.zero, downZ_Lb_hair = Vector3.zero;
            List<Matrix4x4> offsetMatr_hair = new();
            Vector3[] boundcenterCS_hair = new Vector3[hairRends.Count];

            ComputeMatrix_OtherMatr(hairRends, ref renderingData, ref maxPerBoundSize_hair, ref boundcenterCS_hair, ref upZ_Rf_hair, ref upZ_Rb_hair, ref upZ_Lf_hair, ref upZ_Lb_hair, ref downZ_Rf_hair, ref downZ_Rb_hair, ref downZ_Lf_hair, ref downZ_Lb_hair);
            ComputeMatrix_OffsetMatr(hairRends, ref offsetMatr_hair, boundcenterCS_hair, maxPerBoundSize_hair, camSize);

            //最后
            //Matrix4x4 P_hair = GL.GetGPUProjectionMatrix(Matrix4x4.Ortho(left, right, down, up, forward, back), false);
            Matrix4x4 P = GL.GetGPUProjectionMatrix(renderingData.cameraData.camera.projectionMatrix, false);
            P *= renderingData.cameraData.camera.worldToCameraMatrix;
            for (int i = 0; i < renderers.Count; i++)
            {
                if (hairRends[i] == null)
                {
                    VP_hair[i] = P;
                }
                else
                {
                    VP_hair[i] = offsetMatr_hair[i] * P;
                }
                VP[i] = offsetMatr[i] * P;

                PassMats[i].SetFloat(PerObjNumberID, i);
                PassMats[i].SetMatrix(PerObjShadowWtoPMatrID, VP[i]);
                PassMats[i].SetMatrix(PerObjShadowWtoPMatr_hairID, VP_hair[i]);
            }
            Shader.SetGlobalMatrixArray(UsePerObjShadowWtoPMatrID, VP);
            Shader.SetGlobalMatrixArray(UsePerObjShadowWtoPMatr_hairID, VP_hair);
            Shader.SetGlobalFloat(PCSSInteNumID, renderers.Count);
        }
        public void ComputeMatrix_OtherMatr(List<Renderer[]> Renderers, ref RenderingData renderingData, ref float[] maxPerBoundSize, ref Vector3[] boundcenterCS, ref Vector3 upZ_Rf, ref Vector3 upZ_Rb, ref Vector3 upZ_Lf, ref Vector3 upZ_Lb, ref Vector3 downZ_Rf, ref Vector3 downZ_Rb, ref Vector3 downZ_Lf, ref Vector3 downZ_Lb)
        {
            for (int i = 0; i < Renderers.Count; i++)
            {
                if (Renderers[i] == null) continue;
                Vector3 up_Rf, up_Rb, up_Lf, up_Lb, down_Rf, down_Rb, down_Lf, down_Lb;
                Bounds nowbound = Renderers[i][0].bounds;

                foreach (Renderer rend in Renderers[i])
                {
                    nowbound.Encapsulate(rend.bounds);
                }

                float x = nowbound.extents.x * BoundsSize;
                float y = nowbound.extents.y * BoundsSize;
                float z = nowbound.extents.z * BoundsSize;
                Vector3 boundcenter_x = nowbound.center;

                //包围盒对角线
                up_Rf = new Vector3(x, y, z) + boundcenter_x;
                up_Rb = new Vector3(x, y, -z) + boundcenter_x;
                up_Lf = new Vector3(-x, y, z) + boundcenter_x;
                up_Lb = new Vector3(-x, y, -z) + boundcenter_x;
                /////////////////////////////////
                down_Rf = new Vector3(x, -y, z) + boundcenter_x;
                down_Rb = new Vector3(x, -y, -z) + boundcenter_x;
                down_Lf = new Vector3(-x, -y, z) + boundcenter_x;
                down_Lb = new Vector3(-x, -y, -z) + boundcenter_x;

                float left = 0, right = 0, up = 0, down = 0, forward = 0, back = 0;
                ComputeMatrix_SixCorners(false, renderingData.cameraData.camera.transform.worldToLocalMatrix, up_Rf, up_Rb, up_Lf, up_Lb, down_Rf, down_Rb, down_Lf, down_Lb, ref left, ref right, ref up, ref down, ref forward, ref back);
                float xCam = right - left;
                float yCam = up - down;
                maxPerBoundSize[i] = Mathf.Max(xCam, yCam) / 2;

                if (i == 0)
                {
                    upZ_Rf = up_Rf;
                    upZ_Rb = up_Rb;
                    upZ_Lf = up_Lf;
                    upZ_Lb = up_Lb;
                    /////////////////////////////////
                    downZ_Rf = down_Rf;
                    downZ_Rb = down_Rb;
                    downZ_Lf = down_Lf;
                    downZ_Lb = down_Lb;
                }
                else
                {
                    upZ_Rf = new(Mathf.Max(upZ_Rf.x, up_Rf.x), Mathf.Max(upZ_Rf.y, up_Rf.y), Mathf.Max(upZ_Rf.z, up_Rf.z));
                    upZ_Rb = new(Mathf.Max(upZ_Rb.x, up_Rb.x), Mathf.Max(upZ_Rb.y, up_Rb.y), Mathf.Min(upZ_Rb.z, up_Rb.z));
                    upZ_Lf = new(Mathf.Min(upZ_Lf.x, up_Lf.x), Mathf.Max(upZ_Lf.y, up_Lf.y), Mathf.Max(upZ_Lf.z, up_Lf.z));
                    upZ_Lb = new(Mathf.Min(upZ_Lb.x, up_Lb.x), Mathf.Max(upZ_Lb.y, up_Lb.y), Mathf.Min(upZ_Lb.z, up_Lb.z));
                    /////////////////////////////////
                    downZ_Rf = new(Mathf.Max(downZ_Rf.x, down_Rf.x), Mathf.Min(downZ_Rf.y, down_Rf.y), Mathf.Max(downZ_Rf.z, down_Rf.z));
                    downZ_Rb = new(Mathf.Max(downZ_Rb.x, down_Rb.x), Mathf.Min(downZ_Rb.y, down_Rb.y), Mathf.Min(downZ_Rb.z, down_Rb.z));
                    downZ_Lf = new(Mathf.Min(downZ_Lf.x, down_Lf.x), Mathf.Min(downZ_Lf.y, down_Lf.y), Mathf.Max(downZ_Lf.z, down_Lf.z));
                    downZ_Lb = new(Mathf.Min(downZ_Lb.x, down_Lb.x), Mathf.Min(downZ_Lb.y, down_Lb.y), Mathf.Min(downZ_Lb.z, down_Lb.z));
                }
                //DrawDebug(up_Rf, up_Rb, up_Lf, up_Lb, down_Rf, down_Rb, down_Lf, down_Lb);

                Vector3 boundcenterVS = renderingData.cameraData.camera.worldToCameraMatrix.MultiplyPoint3x4(boundcenter_x);
                boundcenterCS[i] = renderingData.cameraData.camera.projectionMatrix.MultiplyPoint(boundcenterVS);
            }
        }
        public void ComputeMatrix_SixCorners(bool needFB, Matrix4x4 WtLMatr, Vector3 upZ_Rf, Vector3 upZ_Rb, Vector3 upZ_Lf, Vector3 upZ_Lb, Vector3 downZ_Rf, Vector3 downZ_Rb, Vector3 downZ_Lf, Vector3 downZ_Lb, ref float left, ref float right, ref float up, ref float down, ref float forward, ref float back)
        {
            Vector3 UP_Rf = WtLMatr.MultiplyPoint3x4(upZ_Rf);
            Vector3 UP_Rb = WtLMatr.MultiplyPoint3x4(upZ_Rb);
            Vector3 UP_Lf = WtLMatr.MultiplyPoint3x4(upZ_Lf);
            Vector3 UP_Lb = WtLMatr.MultiplyPoint3x4(upZ_Lb);
            Vector3 DOWN_Rf = WtLMatr.MultiplyPoint3x4(downZ_Rf);
            Vector3 DOWN_Rb = WtLMatr.MultiplyPoint3x4(downZ_Rb);
            Vector3 DOWN_Lf = WtLMatr.MultiplyPoint3x4(downZ_Lf);
            Vector3 DOWN_Lb = WtLMatr.MultiplyPoint3x4(downZ_Lb);
            left = Mathf.Min(Mathf.Min(Mathf.Min(Mathf.Min(Mathf.Min(Mathf.Min(Mathf.Min(UP_Rf.x, UP_Rb.x), UP_Lf.x), UP_Lb.x), DOWN_Rf.x), DOWN_Rb.x), DOWN_Lf.x), DOWN_Lb.x);
            right = Mathf.Max(Mathf.Max(Mathf.Max(Mathf.Max(Mathf.Max(Mathf.Max(Mathf.Max(UP_Rf.x, UP_Rb.x), UP_Lf.x), UP_Lb.x), DOWN_Rf.x), DOWN_Rb.x), DOWN_Lf.x), DOWN_Lb.x);
            up = Mathf.Max(Mathf.Max(Mathf.Max(Mathf.Max(Mathf.Max(Mathf.Max(Mathf.Max(UP_Rf.y, UP_Rb.y), UP_Lf.y), UP_Lb.y), DOWN_Rf.y), DOWN_Rb.y), DOWN_Lf.y), DOWN_Lb.y);
            down = Mathf.Min(Mathf.Min(Mathf.Min(Mathf.Min(Mathf.Min(Mathf.Min(Mathf.Min(UP_Rf.y, UP_Rb.y), UP_Lf.y), UP_Lb.y), DOWN_Rf.y), DOWN_Rb.y), DOWN_Lf.y), DOWN_Lb.y);
            if (needFB)
            {
                forward = Mathf.Max(Mathf.Max(Mathf.Max(Mathf.Max(Mathf.Max(Mathf.Max(Mathf.Max(UP_Rf.z, UP_Rb.z), UP_Lf.z), UP_Lb.z), DOWN_Rf.z), DOWN_Rb.z), DOWN_Lf.z), DOWN_Lb.z);
                back = Mathf.Min(Mathf.Min(Mathf.Min(Mathf.Min(Mathf.Min(Mathf.Min(Mathf.Min(UP_Rf.z, UP_Rb.z), UP_Lf.z), UP_Lb.z), DOWN_Rf.z), DOWN_Rb.z), DOWN_Lf.z), DOWN_Lb.z);
            }
        }
        public void ComputeMatrix_OffsetMatr(List<Renderer[]> Renderers, ref List<Matrix4x4> offsetMatr, Vector3[] boundcenterCS, float[] maxPerBoundSize, float camSize)
        {
            for (int i = 0; i < Renderers.Count; i++)
            {
                if (Renderers[i] == null) continue;
                //放大倍数
                float scale = 1.0f / (camSize / maxPerBoundSize[i]);
                //移动到相机屏幕中心
                Matrix4x4 uvOffsetMatr = Matrix4x4.identity;
                uvOffsetMatr.SetRow(0, new Vector4(1, 0, 0, -boundcenterCS[i].x));
                uvOffsetMatr.SetRow(1, new Vector4(0, 1, 0, -boundcenterCS[i].y));
                uvOffsetMatr.SetRow(3, new Vector4(0, 0, 0, scale));
                //按九宫格摆放
                float minus = (perObjNumSqrt - 1) * 0.5f;
                float row = i % perObjNumSqrt - minus;
                float column = Mathf.FloorToInt(i / perObjNumSqrt) - minus;

                Matrix4x4 linshiMatr = Matrix4x4.identity;
                linshiMatr.SetRow(0, new Vector4(1, 0, 0, row * 2));
                linshiMatr.SetRow(1, new Vector4(0, -1, 0, column * 2));//xyz乘-1
                linshiMatr.SetRow(3, new Vector4(0, 0, 0, perObjNumSqrt));//缩放，和行列的格子数有关，齐次除法

                offsetMatr.Add(linshiMatr * uvOffsetMatr);
            }
        }

        private void DrawDebug(Vector3 up_Rf, Vector3 up_Rb, Vector3 up_Lf, Vector3 up_Lb, Vector3 down_Rf, Vector3 down_Rb, Vector3 down_Lf, Vector3 down_Lb)
        {
            Debug.DrawLine(up_Rf, up_Rb, Color.red);
            Debug.DrawLine(up_Rf, up_Lf, Color.red);
            Debug.DrawLine(up_Rf, down_Rf, Color.red);

            Debug.DrawLine(up_Lb, up_Lf, Color.red);
            Debug.DrawLine(up_Lb, up_Rb, Color.red);
            Debug.DrawLine(up_Lb, down_Lb, Color.red);

            Debug.DrawLine(down_Lf, down_Lb, Color.red);
            Debug.DrawLine(down_Lf, down_Rf, Color.red);
            Debug.DrawLine(down_Lf, up_Lf, Color.red);

            Debug.DrawLine(down_Rb, down_Rf, Color.red);
            Debug.DrawLine(down_Rb, down_Lb, Color.red);
            Debug.DrawLine(down_Rb, up_Rb, Color.red);
        }
    }
}