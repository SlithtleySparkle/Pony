using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering.Universal;

[Tooltip("此脚本挂在灯光上")]
public class PerObjShadowCS : MonoBehaviour
{
	[Tooltip("使用的RenderingLayerMask，从第几开始")]
	public int mbRenderingLayerMask;

	[Tooltip("不超过九个")]
	public List<GameObject> renderers = new List<GameObject>();
	private List<Renderer[]> listRenderers = new List<Renderer[]>();
	public UniversalRendererData renderdata;
	private PerObjShadowRenderFeature perObjShadowRenderFeature;

	[Tooltip("是否使用逐角色刘海阴影")]
	public bool perHairShadow;
    private List<Renderer[]> listHairRenderers = new List<Renderer[]>();
    private LiuHaiShadowRenderFeature liuHaiShadowRenderFeature;

	private Camera _camera = null;

	private static readonly int LightSpaceRainbowDashHouDuMatrixID = Shader.PropertyToID("_LightSpaceRainbowDashHouDuMatrix");
	private static readonly int PCSSLightSpaceShadowMatrixID = Shader.PropertyToID("_PCSSLightSpaceShadowMatrix");
    void Start()
	{
        perObjShadowRenderFeature = renderdata.rendererFeatures.OfType<PerObjShadowRenderFeature>().FirstOrDefault();
        liuHaiShadowRenderFeature = renderdata.rendererFeatures.OfType<LiuHaiShadowRenderFeature>().FirstOrDefault();
	}
	private void Update()
	{
        if (_camera == null)
        {
            _camera = gameObject.GetComponentInChildren<Camera>();
        }
        if (perObjShadowRenderFeature == null) Debug.LogError("RendererData为空！");
		if (liuHaiShadowRenderFeature == null) Debug.LogError("RendererData为空！");
        perObjShadowRenderFeature.onlyHairMask = perHairShadow;
		liuHaiShadowRenderFeature.Setting.onlyHairMask = perHairShadow;

        if (renderers.Count > 9)
		{
			Debug.Log("角色数量超过九个");
			for (int i = listRenderers.Count - 1; i > 9; i--)
			{
				listRenderers.RemoveAt(i);
			}
            for (int i = listHairRenderers.Count - 1; i > 9; i--)
            {
                listHairRenderers.RemoveAt(i);
            }
        }
		if (renderers != null && renderers.Count <= 9)
		{
            FindNeedRenderers(ref listRenderers, false);
            FindNeedRenderers(ref listHairRenderers, true);

            perObjShadowRenderFeature.renderers = listRenderers;
            perObjShadowRenderFeature.hairRends = listHairRenderers;
            liuHaiShadowRenderFeature.Setting.renderers = listHairRenderers;
		}
		perObjShadowRenderFeature.mbRenderingLayerMask = mbRenderingLayerMask;
		perObjShadowRenderFeature.lightForwardDir = gameObject.transform.forward;

        Matrix4x4 LightSpaceSkinHouDuMatrix = GL.GetGPUProjectionMatrix(_camera.projectionMatrix, false) * _camera.worldToCameraMatrix;
        Shader.SetGlobalMatrix(LightSpaceRainbowDashHouDuMatrixID, LightSpaceSkinHouDuMatrix);
        //PCSS阴影，给PBR Shader设置
        Shader.SetGlobalMatrix(PCSSLightSpaceShadowMatrixID, LightSpaceSkinHouDuMatrix);
	}
	private void OnDisable()
	{
		listRenderers?.Clear();
		listRenderers?.TrimExcess();
        listHairRenderers?.Clear();
        listHairRenderers?.TrimExcess();
    }

	private void FindNeedRenderers(ref List<Renderer[]> ListRenderers, bool perhairshadow)
	{
        if (ListRenderers.Count != renderers.Count)
        {
            ListRenderers?.Clear();
            ListRenderers?.TrimExcess();

			if (perhairshadow)
			{
                for (int i = 0; i < renderers.Count; i++)
                {
                    Renderer[] everyMetaballObj = renderers[i].GetComponentsInChildren<Renderer>(true);
					GameObject go = null;
                    foreach (var rend in everyMetaballObj)
                    {
						if (rend.name.IndexOf("_刘海") != -1)
						{
							go = rend.gameObject;
                            break;
                        }
                    }
					if (go == null)
					{
                        ListRenderers.Add(null);
                        Debug.LogError(renderers[i].name + "的刘海需重命名（在最后加上“_刘海”）");
					}
					else
					{
                        ListRenderers.Add(go.GetComponentsInChildren<Renderer>(true));
                    }
                }
            }
			else
			{
                //如果删除物体，需手动设置renderingLayerMask为Default
                for (int i = 0; i < renderers.Count; i++)
                {
                    Renderer[] everyMetaballObj = renderers[i].GetComponentsInChildren<Renderer>(true);
                    foreach (var rend in everyMetaballObj)
                    {
                        rend.renderingLayerMask = (uint)1 << mbRenderingLayerMask + i;
                    }
                    ListRenderers.Add(everyMetaballObj);
                }
            }
        }
        else
        {
			if (!perhairshadow)
			{
                //比较角色的renderer数量是否相同
                for (int i = 0; i < renderers.Count; i++)
                {
                    Renderer[] everyMetaballObj = renderers[i].GetComponentsInChildren<Renderer>(true);

                    bool isSame = new HashSet<Renderer>(ListRenderers[i]).SetEquals(everyMetaballObj);
                    if (!isSame)
                    {
                        foreach (var rend in everyMetaballObj)
                        {
                            rend.renderingLayerMask = (uint)1 << mbRenderingLayerMask + i;
                        }
                        ListRenderers[i] = everyMetaballObj;
                    }
                }
            }
        }
    }
}