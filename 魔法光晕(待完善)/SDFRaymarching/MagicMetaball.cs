using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
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

    //遍历子物体，所有元球都要放在此游戏对象下
    private Transform[] everyMetaballObj;
    private List<GameObject> metaballPos = new();
    [HideInInspector]
    public bool isFindMBObj = false;

    //遍历每个元球，找到与它最近的（最多）5个元球
    private int fusionsMetaballCount = 5;
    static readonly int MetaballPositionWSID = Shader.PropertyToID("_MetaballPositionWS");
    private int layerint;

    //摄像机
    public Camera metaballMainCam;
    [HideInInspector]
    private RenderTexture _RT = null;
    private float minLenMB = 999.0f;
    [Tooltip("原屏幕分辨率的几分之一")]
    [Range(1, 10)]
    public int mbScreenPexel = 1;
    private int currentmbCeng = 0;

    void Start()
    {
        layerint = LayerMask.NameToLayer(metaballLayer);
        FindEveryMetaball();
        SetCameraAndRT();
    }
    void Update()
    {
        metaballMainCam.nearClipPlane = Camera.main.nearClipPlane;
        metaballMainCam.farClipPlane = Camera.main.farClipPlane;

        if (isFindMBObj == true)
        {
            FindEveryMetaball();
            isFindMBObj = false;
        }
        FindAndSetMetaballPos(true);
        SetRT();
    }
    void OnDisable()
    {
        FindAndSetMetaballPos(false);
        _RT.Release();
    }

    public void FindAndSetMetaballPos(bool UpdateOrDisable)
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

                //计算距离摄像机最近的元球距离
                float currentDis = Vector3.Distance(Camera.main.transform.position, metaballPos[i].transform.position);
                if (i == 0)
                {
                    minLenMB = currentDis;
                }
                else if (minLenMB > currentDis)
                {
                    minLenMB = currentDis;
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
        metaballPos?.Clear();
        everyMetaballObj = null;

        everyMetaballObj = gameObject.GetComponentsInChildren<Transform>(false);
        //添加所有 有元球标签的物体
        foreach (Transform obj in everyMetaballObj)
        {
            GameObject OBJ = obj.gameObject;
            if (OBJ.layer == layerint)
            {
                metaballPos.Add(OBJ);
            }
        }
    }

    public void SetCameraAndRT()
    {
        metaballMainCam.transform.SetParent(Camera.main.transform, true);
        metaballMainCam.transform.localPosition = Vector3.zero;
        metaballMainCam.transform.localRotation = Quaternion.Euler(new Vector3(0, 0, 0));
        metaballMainCam.depth = 1;
        metaballMainCam.clearFlags = CameraClearFlags.SolidColor;
        metaballMainCam.backgroundColor = new(0, 0, 0, 0);
        metaballMainCam.nearClipPlane = Camera.main.nearClipPlane;
        metaballMainCam.farClipPlane = Camera.main.farClipPlane;
        metaballMainCam.cullingMask = 1 << layerint;
        metaballMainCam.GetUniversalAdditionalCameraData().renderShadows = false;
        SetRT();
    }
    public void SetRT()
    {
        if (minLenMB > Camera.main.farClipPlane)
        {
            minLenMB = Camera.main.farClipPlane;
        }
        float camCeng = Camera.main.farClipPlane / mbScreenPexel;
        int nowmbPX = Mathf.CeilToInt(minLenMB / camCeng);
        if (currentmbCeng != nowmbPX)
        {
            currentmbCeng = nowmbPX;
            int wid = Mathf.FloorToInt(Camera.main.pixelWidth / mbScreenPexel * currentmbCeng);
            int hei = Mathf.FloorToInt(Camera.main.pixelHeight / mbScreenPexel * currentmbCeng);

            _RT = new(wid, hei, 0)
            {
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.DontSave,
                filterMode = FilterMode.Bilinear
            };
            metaballMainCam.targetTexture = _RT;
        }
    }
}
//public UniversalRendererData renderdata;
//private MagicMetaballRenderFeature outputObjNorWSRF;
//outputObjNorWSRF = renderdata.rendererFeatures.OfType<MagicMetaballRenderFeature>().FirstOrDefault();