using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//�����Ҫ����
//Shader��fragment��ĵڶ���for�����Ż���
//C#�����ű�
public enum MetaballShape
{
    [Tooltip("������")]
    RoundCone,
    [Tooltip("����")]
    Ellipsoid
}
public class MagicMetaballSpaceObj : MonoBehaviour
{
    //�ڲ�Ԫ���������
    private int maxMBNum = 0;
    private static readonly int SDF_PerNumID = Shader.PropertyToID("_SDF_PerNum");

    [System.Serializable]
    public class PerMetaballSetting
    {
        [Tooltip("���13����������Ҫ�ڴ������޸ġ�")]
        //��Ҫ�޸�
        //Shader��Properties���_SDF_PerNum��SDF���������������
        //C#��ClaerListProperties��for����
        public PerMetaballSpaceObjShape[] perMBSpaceObjShape = new PerMetaballSpaceObjShape[0];
    }
    [System.Serializable]
    public class PerMetaballSpaceObjShape
    {
        [Tooltip("�ڲ��ն���")]
        public Transform PerMetaballSpaceObj;
        [Tooltip("��״���ݴ�ѡ�����������")]
        public MetaballShape MBShapeEnum;

        [Tooltip("����������")]
        public Vector4 RoundConeSetting;
        [Tooltip("��Բ����")]
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

    [Tooltip("��ͬ��ɫ֮��Ĺ���ǿ��")]
    [Range(0.0f, 0.5f)]
    public float ColorControlSmooth = 0.25f;
    private static readonly int ColorControlSmoothID = Shader.PropertyToID("_ColorControlSmooth");
    [Tooltip("���������� �������")]
    public ColorControlPoint[] colorControlPoint = new ColorControlPoint[2];
    [System.Serializable]
    public class ColorControlPoint
    {
        [Tooltip("���Ƶ�λ��")]
        public Transform colConPoint;
        [Tooltip("��Ӧ��ɫ")]
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
        //���Ƶ�
        ClaerConListProperties();
    }
    void Update()
    {
        //��������ı�
        if (perMetaballSetting.perMBSpaceObjShape.Length != startLength)
        {
            ClaerListProperties();
            startLength = perMetaballSetting.perMBSpaceObjShape.Length;
        }
        for (int i = 0; i < perMetaballSetting.perMBSpaceObjShape.Length; i++)
        {
            Vector4 posls = new(perMetaballSetting.perMBSpaceObjShape[i].PerMetaballSpaceObj.position.x, perMetaballSetting.perMBSpaceObjShape[i].PerMetaballSpaceObj.position.y, perMetaballSetting.perMBSpaceObjShape[i].PerMetaballSpaceObj.position.z, 0.0f);
            //w����������Shader���ж���״
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
        //���Ƶ�
        for (int i = 0; i < colorControlPoint.Length; i++)
        {
            ColorControlPosWS[i] = new(colorControlPoint[i].colConPoint.position.x, colorControlPoint[i].colConPoint.position.y, colorControlPoint[i].colConPoint.position.z, 0.0f);
            ColorControlColor[i] = new(colorControlPoint[i].colConColor.r, colorControlPoint[i].colConColor.g, colorControlPoint[i].colConColor.b, colorControlPoint[i].colConColor.a);
        }

        myMat.SetVectorArray(InsideMetaballPosWSID, inpos);
        myMat.SetMatrixArray(InsideMBRotMatrixID, inrotMatr);
        myMat.SetVectorArray(RoundConeProID, roundConePro);
        myMat.SetVectorArray(EllipsoidProID, ellipsoidPro);
        //���Ƶ�
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
    //���Ƶ�
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