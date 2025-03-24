using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//如果需要增加
//Shader：fragment里的第二个for（待优化）
//C#：本脚本
public enum MetaballShape
{
    [Tooltip("胶囊体")]
    RoundCone,
    [Tooltip("椭球")]
    Ellipsoid
}
public class MagicMetaballSpaceObj : MonoBehaviour
{
    //内部元球最大数量
    private int maxMBNum = 0;
    private static readonly int SDF_PerNumID = Shader.PropertyToID("_SDF_PerNum");

    [System.Serializable]
    public class PerMetaballSetting
    {
        [Tooltip("最多13个，不够需要在代码里修改。")]
        //若要修改
        //Shader：Properties里的_SDF_PerNum，SDF属性设置里的属性
        //C#：ClaerListProperties的for条件
        public PerMetaballSpaceObjShape[] perMBSpaceObjShape = new PerMetaballSpaceObjShape[0];
    }
    [System.Serializable]
    public class PerMetaballSpaceObjShape
    {
        [Tooltip("内部空对象")]
        public Transform PerMetaballSpaceObj;
        [Tooltip("形状，据此选择下面的属性")]
        public MetaballShape MBShapeEnum;

        [Tooltip("胶囊体属性")]
        public Vector4 RoundConeSetting;
        [Tooltip("椭圆属性")]
        public Vector4 EllipsoidSetting;
    }
    public PerMetaballSetting perMetaballSetting = new();

    private Material myMat;
    private List<Vector4> inpos = new();
    private static readonly int InsideMetaballPosWSID = Shader.PropertyToID("_InsideMetaballPosWS");
    private List<Matrix4x4> inrotMatr = new();
    private static readonly int InsideMBRotMatrixID = Shader.PropertyToID("_InsideMBRotMatrix");
    private List<Vector4> roundConePro = new();
    private static readonly int RoundConeProID = Shader.PropertyToID("_RoundConePro");
    private List<Vector4> ellipsoidPro = new();
    private static readonly int EllipsoidProID = Shader.PropertyToID("_EllipsoidPro");

    private int startLength = 0;
    private List<Vector4> Perrot = new();

    [Tooltip("不同颜色之间的过渡强度")]
    [Range(0.0f, 0.5f)]
    public float ColorControlSmooth = 0.25f;
    private static readonly int ColorControlSmoothID = Shader.PropertyToID("_ColorControlSmooth");
    [Tooltip("至少两个， 最多两个")]
    public ColorControlPoint[] colorControlPoint = new ColorControlPoint[2];
    [System.Serializable]
    public class ColorControlPoint
    {
        [Tooltip("控制点位置")]
        public Transform colConPoint;
        [Tooltip("相应颜色")]
        public Color colConColor = Color.white;
    }
    private List<Vector4> ColorControlPosWS = new();
    private static readonly int ColorControlPosWSID = Shader.PropertyToID("_ColorControlPosWS");
    private List<Vector4> ColorControlColor = new();
    private static readonly int ColorControlColorID = Shader.PropertyToID("_ColorControlColor");

    void Start()
    {
        myMat = gameObject.GetComponent<Renderer>().sharedMaterial;

        ClaerListProperties();
        startLength = perMetaballSetting.perMBSpaceObjShape.Length;
        //控制点
        ClaerConListProperties();
    }
    void Update()
    {
        //如果数量改变
        if (perMetaballSetting.perMBSpaceObjShape.Length != startLength)
        {
            ClaerListProperties();
            startLength = perMetaballSetting.perMBSpaceObjShape.Length;
        }
        for (int i = 0; i < perMetaballSetting.perMBSpaceObjShape.Length; i++)
        {
            Vector4 posls = new(perMetaballSetting.perMBSpaceObjShape[i].PerMetaballSpaceObj.position.x, perMetaballSetting.perMBSpaceObjShape[i].PerMetaballSpaceObj.position.y, perMetaballSetting.perMBSpaceObjShape[i].PerMetaballSpaceObj.position.z, 0.0f);
            //w分量用于在Shader里判断形状
            if (perMetaballSetting.perMBSpaceObjShape[i].MBShapeEnum.ToString() == "RoundCone")
            {
                posls.w = 1.0f;
                roundConePro[i] = perMetaballSetting.perMBSpaceObjShape[i].RoundConeSetting;
            }
            else
            {
                posls.w = 0.0f;
                ellipsoidPro[i] = perMetaballSetting.perMBSpaceObjShape[i].EllipsoidSetting;
            }

            inpos[i] = posls;
            Vector4 rotls = new(perMetaballSetting.perMBSpaceObjShape[i].PerMetaballSpaceObj.localRotation.x, perMetaballSetting.perMBSpaceObjShape[i].PerMetaballSpaceObj.localRotation.y, perMetaballSetting.perMBSpaceObjShape[i].PerMetaballSpaceObj.localRotation.z, perMetaballSetting.perMBSpaceObjShape[i].PerMetaballSpaceObj.localRotation.w);
            if (Perrot[i] != rotls)
            {
                RotMatrix(i, rotls);
                Perrot[i] = rotls;
            }
        }
        //控制点
        for (int i = 0; i < colorControlPoint.Length; i++)
        {
            ColorControlPosWS[i] = new(colorControlPoint[i].colConPoint.position.x, colorControlPoint[i].colConPoint.position.y, colorControlPoint[i].colConPoint.position.z, 0.0f);
            ColorControlColor[i] = new(colorControlPoint[i].colConColor.r, colorControlPoint[i].colConColor.g, colorControlPoint[i].colConColor.b, colorControlPoint[i].colConColor.a);
        }

        myMat.SetVectorArray(InsideMetaballPosWSID, inpos);
        myMat.SetMatrixArray(InsideMBRotMatrixID, inrotMatr);
        myMat.SetVectorArray(RoundConeProID, roundConePro);
        myMat.SetVectorArray(EllipsoidProID, ellipsoidPro);
        //控制点
        myMat.SetFloat(ColorControlSmoothID, ColorControlSmooth);
        myMat.SetVectorArray(ColorControlPosWSID, ColorControlPosWS);
        myMat.SetVectorArray(ColorControlColorID, ColorControlColor);
    }
    private void OnDisable()
    {
        PointClear();
    }

    private void PointClear()
    {
        inpos?.Clear();
        inrotMatr?.Clear();
        roundConePro?.Clear();
        ellipsoidPro?.Clear();
        Perrot?.Clear();
    }
    private void ClaerListProperties()
    {
        PointClear();
        maxMBNum = perMetaballSetting.perMBSpaceObjShape.Length;
        myMat.SetFloat(SDF_PerNumID, maxMBNum);
        for (int i = 0; i < 13; i++)
        {
            inpos.Add(new(0, 0, 0, -1));

            Matrix4x4 rotMatr = Matrix4x4.identity;
            rotMatr.SetRow(0, new(1, 0, 0, 0));
            rotMatr.SetRow(1, new(0, 1, 0, 0));
            rotMatr.SetRow(2, new(0, 0, 1, 0));
            rotMatr.SetRow(3, new(0, 0, 0, 0));
            inrotMatr.Add(rotMatr);

            roundConePro.Add(new(0, 0, 0, -1));
            ellipsoidPro.Add(new(0, 0, 0, -1));
            Perrot.Add(new(0, 0, 0, 0));
        }
    }
    private void RotMatrix(int i, Vector4 q)
    {
        Matrix4x4 rotMatr = Matrix4x4.identity;
        Vector4 row1 = new(1 - 2 * (q.y * q.y + q.z * q.z), 2 * (q.x * q.y + q.w * q.z), 2 * (q.x * q.z - q.w * q.y), 0);
        Vector4 row2 = new(2 * (q.x * q.y - q.w * q.z), 1 - 2 * (q.x * q.x + q.z * q.z), 2 * (q.y * q.z + q.w * q.x), 0);
        Vector4 row3 = new(2 * (q.x * q.z + q.w * q.y), 2 * (q.y * q.z - q.w * q.x), 1 - 2 * (q.x * q.x + q.y * q.y), 0);

        rotMatr.SetRow(0, row1);
        rotMatr.SetRow(1, row2);
        rotMatr.SetRow(2, row3);
        rotMatr.SetRow(3, new(0, 0, 0, 0));

        inrotMatr[i] = rotMatr;
    }
    //控制点
    private void ConPointClear()
    {
        ColorControlPosWS?.Clear();
        ColorControlColor?.Clear();
    }
    private void ClaerConListProperties()
    {
        ConPointClear();
        for (int i = 0; i < 2; i++)
        {
            ColorControlPosWS.Add(new(0, 0, 0, 0));
            ColorControlColor.Add(new(0, 0, 0, 0));
        }
    }
}