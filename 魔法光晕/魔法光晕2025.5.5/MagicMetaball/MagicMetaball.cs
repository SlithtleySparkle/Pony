using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public enum PerMetaballRTSize
{
    _240 = 240,
    _480 = 480,
    _960 = 960,
    _1920 = 1920
}
public class MagicMetaball : MonoBehaviour
{
    //支持渲染最多九个魔法光晕（不是九个添加了相应Layer的物体）
    //需要在一开始就确定哪些可能会被用于魔法光晕的渲染（添加相关标签），后续更改无效
    [Tooltip("魔法光晕使用的Layer")]
    public string metaballLayer;
    [Tooltip("魔法光晕使用的RenderingLayerMask从第几开始")]
    public int mbRenderingLayerMask;
    [Tooltip("魔法光晕质量")]
    public PerMetaballRTSize perMetaballRenderTextureSize = PerMetaballRTSize._480;

    private List<GameObject> metaballPos = new();
    private List<Renderer> minLengMB = new();
    private Dictionary<Renderer, float> minlenmb_D = new();
    private List<Renderer> lingshirenders = new();

    //摄像机
    [Tooltip("摄像机名字，默认MetaballAddCam")]
    public string CameraName = "MetaballAddCam";
    private Camera metaballMainCam;
    private RenderTexture _RT = null;

    public UniversalRendererData renderdata;
    private MagicMetaballRenderFeature MagicMBRenderFeature;

    void Start()
    {
        MagicMBRenderFeature = renderdata.rendererFeatures.OfType<MagicMetaballRenderFeature>().FirstOrDefault();
        MagicMBRenderFeature.Setting.renderingLayerMask = mbRenderingLayerMask;

        int layerint = LayerMask.NameToLayer(metaballLayer);
        SetCameraAndRT(layerint);
        FindEveryMetaball(layerint);
    }
    void Update()
    {
        if ((int)perMetaballRenderTextureSize != _RT.width)
        {
            SetRT();
        }

        if (Time.frameCount % 5 == 0)
        {
            lingshirenders?.Clear();
            lingshirenders?.TrimExcess();
            FindGoodMetaballAndMatrixVP(ref lingshirenders);
            MagicMBRenderFeature.Setting.mbGO = minLengMB;
            MagicMBRenderFeature.Setting.mbGOCount = minLengMB.Count;
        }

        Vector3[] minLengMB_rotOS = new Vector3[lingshirenders.Count];
        Vector3[] minLengMB_PosOff = new Vector3[lingshirenders.Count];
        for (int i = 0; i < lingshirenders.Count; i++)
        {
            minLengMB_rotOS[i] = lingshirenders[i].GetComponent<MagicMetaballSpaceObj>().MBRot_ke.transform.localRotation.eulerAngles;
            minLengMB_PosOff[i] = lingshirenders[i].GetComponent<MagicMetaballSpaceObj>().MBPos_ke;
        }
        MagicMBRenderFeature.Setting.zdyRotv3 = minLengMB_rotOS;
        MagicMBRenderFeature.Setting.MBPos_ke = minLengMB_PosOff;
    }
    void OnDisable()
    {
        _RT?.Release();
        minlenmb_D?.Clear();
        minlenmb_D?.TrimExcess();
        minLengMB?.Clear();
        minLengMB?.TrimExcess();
        metaballPos?.Clear();
        metaballPos?.TrimExcess();
        lingshirenders?.Clear();
        lingshirenders?.TrimExcess();
    }

    public void FindGoodMetaballAndMatrixVP(ref List<Renderer> lingshirenders)
    {
        minlenmb_D?.Clear();
        minlenmb_D?.TrimExcess();

        for (int i = 0; i < metaballPos.Count; i++)
        {
            //视锥剔除
            bool inCamera = false;
            Bounds bound = metaballPos[i].GetComponent<Renderer>().bounds;
            inCamera = IsVisibleByCamera(bound.center, bound.extents, Camera.main);
            if (inCamera && metaballPos[i].activeInHierarchy)
            {
                //计算距离摄像机最近的元球距离
                float currentDis = Vector3.Distance(Camera.main.transform.position, metaballPos[i].transform.position);
                if (currentDis < Camera.main.farClipPlane + 5)
                {
                    Renderer rend = metaballPos[i].GetComponent<Renderer>();
                    minlenmb_D.Add(rend, currentDis);
                    lingshirenders.Add(rend);
                }
            }
        }
        //按距离排序
        lingshirenders = lingshirenders.OrderBy(dis => minlenmb_D[dis]).ToList();

        //顺序无关，检查元素、数量是否一样
        //bool isSame = new HashSet<Renderer>(minLengMB).SetEquals(lingshirenders);
        //顺序有关，检查元素、数量是否一样
        bool isSame = minLengMB.SequenceEqual(lingshirenders);
        if (!isSame)
        {
            minLengMB?.Clear();
            minLengMB?.TrimExcess();
            for (int i = 0; i < lingshirenders.Count; i++)
            {
                minLengMB.Add(lingshirenders[i]);
            }
            MagicMBRenderFeature.Setting.isReComputeBounds = true;
        }
        else
        {
            MagicMBRenderFeature.Setting.isReComputeBounds = false;
        }
    }
    public void FindEveryMetaball(int layerint)
    {
        metaballPos?.Clear();
        metaballPos?.TrimExcess();
        GameObject[] everyMetaballObj = FindObjectsByType(typeof(GameObject), FindObjectsInactive.Include, FindObjectsSortMode.None) as GameObject[];
        //添加所有 有元球标签的物体
        foreach (GameObject obj in everyMetaballObj)
        {
            if (obj.layer == layerint)
            {
                metaballPos.Add(obj);
            }
        }
    }

    //https://zhuanlan.zhihu.com/p/19979217705
    public bool IsVisibleByCamera(float3 boundCen, float3 extents, Camera mainCam)
    {
        Matrix4x4 VP = GL.GetGPUProjectionMatrix(mainCam.projectionMatrix, false) * mainCam.worldToCameraMatrix;

        float3 fa = VP.m30 - new float3(VP.m00, VP.m10, VP.m20);
        float3 fb = VP.m31 - new float3(VP.m01, VP.m11, VP.m21);
        float3 fc = VP.m32 - new float3(VP.m02, VP.m12, VP.m22);
        float3 fd = VP.m33 - new float3(VP.m03, VP.m13, VP.m23);

        float3 fe = VP.m30 + new float3(VP.m00, VP.m10, VP.m20);
        float3 ff = VP.m31 + new float3(VP.m01, VP.m11, VP.m21);
        float3 fg = VP.m32 + new float3(VP.m02, VP.m12, VP.m22);
        float3 fh = VP.m33 + new float3(VP.m03, VP.m13, VP.m23);

        float3 d012 = fa * boundCen.x + fb * boundCen.y + fc * boundCen.z + fd;
        float3 d345 = fe * boundCen.x + ff * boundCen.y + fg * boundCen.z + fh;

        extents *= 0.85f;
        float3 r012 = math.abs(fa) * extents.x + math.abs(fb) * extents.y + math.abs(fc) * extents.z;
        float3 r345 = math.abs(fe) * extents.x + math.abs(ff) * extents.y + math.abs(fg) * extents.z;

        float3 f012 = d012 + r012;
        float3 f345 = d345 + r345;

        return math.all(f012 > 0) && math.all(f345 > 0);
    }

    public void SetCameraAndRT(int layerint)
    {
        GameObject mbCam = new GameObject(CameraName);
        MagicMBRenderFeature.Setting.MetaballCam = CameraName;

        metaballMainCam = mbCam.AddComponent<Camera>();
        metaballMainCam.transform.SetParent(Camera.main.transform, true);
        metaballMainCam.transform.localPosition = Vector3.zero;
        metaballMainCam.transform.localRotation = Quaternion.Euler(new Vector3(0, 0, 0));
        metaballMainCam.depth = -10;
        metaballMainCam.clearFlags = CameraClearFlags.SolidColor;
        metaballMainCam.backgroundColor = new(0, 0, 0, 0);
        metaballMainCam.depthTextureMode = DepthTextureMode.None;
        metaballMainCam.useOcclusionCulling = false;
        metaballMainCam.cullingMask = 1 << layerint;
        metaballMainCam.orthographic = false;
        metaballMainCam.aspect = Camera.main.aspect;
        metaballMainCam.fieldOfView = Camera.main.fieldOfView;
        metaballMainCam.nearClipPlane = Camera.main.nearClipPlane;
        metaballMainCam.farClipPlane = Camera.main.farClipPlane;
        metaballMainCam.GetUniversalAdditionalCameraData().renderShadows = false;
        metaballMainCam.GetUniversalAdditionalCameraData().renderType = CameraRenderType.Base;
        metaballMainCam.GetUniversalAdditionalCameraData().renderPostProcessing = false;

        SetRT();
    }
    public void SetRT()
    {
        _RT?.Release();

        int rtWSize = (int)perMetaballRenderTextureSize;
        int rtHSize = Mathf.CeilToInt((float)perMetaballRenderTextureSize / Camera.main.aspect);
        rtWSize *= 3;
        rtHSize *= 3;
        _RT = new(rtWSize, rtHSize, 0)
        {
            wrapMode = TextureWrapMode.Clamp,
            useMipMap = false,
            autoGenerateMips = false,
            hideFlags = HideFlags.DontSave,
            filterMode = FilterMode.Bilinear
        };
        metaballMainCam.targetTexture = _RT;
    }
}