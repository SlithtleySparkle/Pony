using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class MagicMetaball : MonoBehaviour
{
    //每个元球都需要创建一个材质球
    [Tooltip("元球使用的Layer，想修改可融合的元球数量需要在Shader里修改")]
    public string metaballLayer;
    [Tooltip("可融合元球的最大距离, 最好 >= 在Shader中设置的“融合阈值”")]
    public float fusionsMaxDistance;

    private GameObject[] everyMetaballObj;
    private List<GameObject> metaballPos = new();
    //遍历每个元球，找到与它最近的（最多）5个元球
    private int fusionsMetaballCount = 5;

    static readonly int MetaballPositionWSID = Shader.PropertyToID("_MetaballPositionWS");

    public UniversalRendererData renderdata;
    private MagicMetaballRenderFeature outputObjNorWSRF;
    private int layerint;

    ////摄像机
    //public Camera metaballMainCam;
    //private RenderTexture _RT = null;
    //private Camera _camera = null;

    void Start()
    {
        layerint = LayerMask.NameToLayer(metaballLayer);
        outputObjNorWSRF = renderdata.rendererFeatures.OfType<MagicMetaballRenderFeature>().FirstOrDefault();

        //if (outputObjNorWSRF.Setting.metaballNorWSCam == "")
        //{
        //    _camera = new GameObject("nullCam").AddComponent<Camera>();
        //}
        //else
        //{
        //    _camera = new GameObject(outputObjNorWSRF.Setting.metaballNorWSCam).AddComponent<Camera>();
        //}
        //_camera.transform.SetParent(metaballMainCam.transform, true);
        //_camera.transform.localPosition = Vector3.zero;
        //_camera.transform.localRotation = Quaternion.Euler(new Vector3(0, 0, 0));
        //_camera.depth = 2;
        //_camera.clearFlags = CameraClearFlags.SolidColor;
        //_camera.backgroundColor = Color.black;
        //_camera.nearClipPlane = metaballMainCam.nearClipPlane;
        //_camera.farClipPlane = metaballMainCam.farClipPlane;
        //_camera.cullingMask = 1 << layerint;
        //_camera.GetUniversalAdditionalCameraData().renderShadows = false;
        //_RT = new(Screen.width, Screen.height, 0)
        //{
        //    wrapMode = TextureWrapMode.Clamp,
        //    useMipMap = true,
        //    autoGenerateMips = true,
        //    hideFlags = HideFlags.DontSave,
        //    filterMode = FilterMode.Bilinear
        //};
        //_camera.targetTexture = _RT;

        FindEveryMetaball();
    }
    void Update()
    {
        //_camera.transform.localPosition = Vector3.zero;
        //_camera.transform.localRotation = Quaternion.Euler(new Vector3(0, 0, 0));
        //_camera.nearClipPlane = metaballMainCam.nearClipPlane;
        //_camera.farClipPlane = metaballMainCam.farClipPlane;

        if (Time.frameCount % 300 == 0)
        {
            FindEveryMetaball();
        }
        FinnAndSetMetaballPos(true);
    }
    void OnDisable()
    {
        FinnAndSetMetaballPos(false);
        //_RT.Release();
    }

    public void FinnAndSetMetaballPos(bool UpdateOrDisable)
    {
        if (UpdateOrDisable == true)
        {
            for (int i = 0; i < metaballPos.Count; i++)
            {
                //寻找可融合的元球
                List<Vector4> perMetaballPos = new();
                Material metaballMat = metaballPos[i].GetComponent<Renderer>().sharedMaterial;
                for (int it = 0; it < metaballPos.Count; it++)
                {
                    if (it != i)
                    {
                        float metaballLength = Vector3.Distance(metaballPos[i].transform.position, metaballPos[it].transform.position);
                        if (metaballLength < fusionsMaxDistance && perMetaballPos.Count < fusionsMetaballCount)
                        {
                            perMetaballPos.Add(metaballPos[it].transform.position);
                        }
                    }
                }
                //传递元球位置
                if (perMetaballPos.Count != 0)
                {
                    if (perMetaballPos.Count < 5)
                    {
                        for (int itt = perMetaballPos.Count; itt < 5; itt++)
                        {
                            perMetaballPos.Add(new Vector4(99999, 99999, 99999, 99999));
                        }
                    }
                    Vector4[] everyFusionsMetaball = perMetaballPos.ToArray();
                    metaballMat.SetVectorArray(MetaballPositionWSID, everyFusionsMetaball);
                }
                else
                {
                    Vector4 noneVec = new Vector4(99999, 99999, 99999, 99999);
                    Vector4[] noneVecList = new Vector4[5] { noneVec, noneVec, noneVec, noneVec, noneVec };
                    metaballMat.SetVectorArray(MetaballPositionWSID, noneVecList);
                }
            }
        }
        else
        {
            for (int i = 0; i < metaballPos.Count; i++)
            {
                Vector4 noneVecDis = new Vector4(99999, 99999, 99999, 99999);
                Vector4[] noneVecDisList = new Vector4[5] { noneVecDis, noneVecDis, noneVecDis, noneVecDis, noneVecDis };
                metaballPos[i].GetComponent<Renderer>().sharedMaterial.SetVectorArray(MetaballPositionWSID, noneVecDisList);
            }
        }
    }
    public void FindEveryMetaball()
    {
        metaballPos.Clear();
        everyMetaballObj = FindObjectsByType(typeof(GameObject), FindObjectsInactive.Exclude, FindObjectsSortMode.None) as GameObject[];
        //添加所有 有元球标签的物体
        for (int i = 0; i < everyMetaballObj.Length; i++)
        {
            if (everyMetaballObj[i].layer == layerint && everyMetaballObj[i].activeInHierarchy)
            {
                metaballPos.Add(everyMetaballObj[i]);
            }
        }

        List<Renderer> rendererlist = new();
        for (int it = 0; it < metaballPos.Count; it++)
        {
            rendererlist.Add(metaballPos[it].GetComponent<Renderer>());
        }
        outputObjNorWSRF.Setting.renderers = rendererlist.ToArray();
    }
}