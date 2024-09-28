using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class TuoWei : MonoBehaviour
{
    public Texture MainTEX;             //主纹理
    public Shader TuoWeiShader;         //主Shader
    private Material TuoWeiMaterial;
    public GameObject XiangZaiPos;      //跟随角色

    private Mesh mesh;
    private MeshFilter filter;

    private List<Vector3> Vector3s = new();
    private List<int> ints = new();
    private List<Vector2> uvs = new();

    private int V = 3;
    private int I = 5;
    private float U = 1;
    private Vector3 YuanLaiPos;
    private Vector3 lastChangeVec1;
    private Vector3 lastChangeVec2;
    //private Vector3 GuaiJiao1;
    //private Vector3 GuaiJiao2;

    private Vector3 Dir;
    private Quaternion jiaodu;
    //private Vector3 lastDir = new(0, 0, 0);
    //private float dotDir;

    [Range(0, 100)]
    public int PingHuaChengDu = 5;                          //拖尾平滑程度
    [Range(0, 64)]
    public int WangGeXiFeng = 2;                            //网格细分程度
    [ColorUsage(false, true)]
    public Color TuoWeiColor = Color.white;                 //拖尾颜色
    public float JianGe = 0.1f;                             //间隔多远更新网格
    public float GaoDu = 1;                                 //网格高度
    public bool BianTouMing = true;                         //是否透明，是：会透明。否：不透明
    [Range(0.0f, 1.0f)]
    public float StartLife = 1;                             //从第几秒开始变透明
    public float TouMingCengDu = 0;                         //透明的程度
    public float ChangDu = 5;                               //拖尾长度
    public float Speed = 6;                                 //透明速度
    public float ChuShiTouMingDu = 0.85f;                   //拖尾初始透明度，影响粒子

    public Texture LiZiTex;                                 //粒子纹理
    [ColorUsage(false, true)]
    public Color LiZiColor = Color.white;                   //粒子颜色，a通道没用
    public float ZhongZi = 1;                               //种子，用于生成随机数
    [Range(0, 5)]
    public int MiDu = 3;                                    //粒子数量
    [Range(0.0f, 2.0f)]
    public float DaXiao = 0.04f;                            //粒子大小
    public Vector4 PianYiChengDu = new Vector4(0.25f, 0.25f, 0.25f, 0.25f); //粒子偏移程度
    public float CenterPosPianYi = 1;                                       //粒子中心偏移
    [Range(0.0f, 10.0f)]
    public float LiZiAlpha = 1;                                             //粒子整体透明度
    [Range(0.0f, 1.0f)]
    public float LiZiAlphaLiSan = 0.5f;                                     //透明度离散程度
    public Vector4 XYZpos = new Vector4(0, 0, 0, 0);
    [Range(0.0f, 100.0f)]
    public float LiZiYunDong = 1;                                           //粒子运动速度

    private static int WangGeXiFengID;
    private static int MainTexID;
    private static int LiZiTexID;
    private static int TuoWeiColID;
    private static int LiZiColID;
    private static int ZhongZiID;
    private static int MiDuID;
    private static int DaXiaoID;
    private static int PianYiChengDuID;
    private static int CenterPosPianYiID;
    private static int LiZiAlphaID;
    private static int LiZiAlphaLiSanID;
    private static int XYZposID;
    private static int LiZiYunDongID;
    private static int ChuShiTouMingDuID;
    private static int ChengDuID;
    private static int ChangDuID;

    void Start()
    {
        mesh = new Mesh();
        filter = GetComponent<MeshFilter>();
        filter.mesh = mesh;
        TuoWeiMaterial = new Material(TuoWeiShader);
        gameObject.GetComponent<MeshRenderer>().material = TuoWeiMaterial;
        YuanLaiPos = transform.position;

        WangGeXiFengID = Shader.PropertyToID("_WangGeXiFeng");
        MainTexID = Shader.PropertyToID("_MainTex");
        LiZiTexID = Shader.PropertyToID("_LiZiTex");
        TuoWeiColID = Shader.PropertyToID("_TuoWeiCol");
        LiZiColID = Shader.PropertyToID("_LiZiCol");
        ZhongZiID = Shader.PropertyToID("_ZhongZi");
        MiDuID = Shader.PropertyToID("_MiDu");
        DaXiaoID = Shader.PropertyToID("_DaXiao");
        PianYiChengDuID = Shader.PropertyToID("_PianYiChengDu");
        CenterPosPianYiID = Shader.PropertyToID("_CenterPosPianYi");
        LiZiAlphaID = Shader.PropertyToID("_LiZiAlpha");
        LiZiAlphaLiSanID = Shader.PropertyToID("_LiZiAlphaLiSan");
        XYZposID = Shader.PropertyToID("_XYZpos");
        LiZiYunDongID = Shader.PropertyToID("_LiZiYunDong");
        ChuShiTouMingDuID = Shader.PropertyToID("_ChuShiTouMingDu");
        ChengDuID = Shader.PropertyToID("_ChengDu");
        ChangDuID = Shader.PropertyToID("_ChangDu");
    }

    void Update()
    {
        TuoWeiMaterial.SetTexture(MainTexID, MainTEX);
        TuoWeiMaterial.SetTexture(LiZiTexID, LiZiTex);
        TuoWeiMaterial.SetFloat(ChuShiTouMingDuID, ChuShiTouMingDu);
        TuoWeiMaterial.SetFloat(MiDuID, Mathf.Max(0, MiDu));
        TuoWeiMaterial.SetFloat(DaXiaoID, Mathf.Max(0, DaXiao));
        TuoWeiMaterial.SetFloat(LiZiAlphaID, LiZiAlpha);
        TuoWeiMaterial.SetFloat(LiZiAlphaLiSanID, LiZiAlphaLiSan);
        TuoWeiMaterial.SetVector(PianYiChengDuID, PianYiChengDu);
        TuoWeiMaterial.SetColor(TuoWeiColID, TuoWeiColor);
        TuoWeiMaterial.SetColor(LiZiColID, LiZiColor);
        TuoWeiMaterial.SetFloat(CenterPosPianYiID, CenterPosPianYi);
        TuoWeiMaterial.SetVector(XYZposID, XYZpos);
        TuoWeiMaterial.SetFloat(ZhongZiID, Mathf.Abs(ZhongZi));
        TuoWeiMaterial.SetFloat(WangGeXiFengID, WangGeXiFeng);
        TuoWeiMaterial.SetFloat(LiZiYunDongID, LiZiYunDong);

        if (Vector3.Distance(XiangZaiPos.transform.position, YuanLaiPos) >= JianGe)
        {
            BianLiangDingDian();
            //lastDir = Dir;
            mesh.vertices = Vector3s.ToArray();
            mesh.triangles = ints.ToArray();
            mesh.uv = uvs.ToArray();
        }

        if (BianTouMing == true)
        {
            if (StartLife > 0)
            {
                StartLife -= Time.deltaTime;
            }
            else
            {
                TouMingCengDu += Time.deltaTime * Speed;
                TuoWeiMaterial.SetFloat(ChengDuID, TouMingCengDu);
                TuoWeiMaterial.SetFloat(ChangDuID, ChangDu);
            }
        }
    }
    private void BianLiangDingDian()
    {
        JiSuanDir();
        Vector3 LingShi = new(XiangZaiPos.transform.position.x - YuanLaiPos.x, XiangZaiPos.transform.position.y - YuanLaiPos.y, XiangZaiPos.transform.position.z - YuanLaiPos.z);
        YuanLaiPos = XiangZaiPos.transform.position;
        if (Vector3s.Count == 0)
        {
            Vector3s.Add(jiaodu * new Vector3(0, -GaoDu, 0));

            lastChangeVec1 = new Vector3(JianGe, -GaoDu, 0);
            lastChangeVec2 = new Vector3(JianGe, GaoDu, 0);
            RotationDian(LingShi);

            Vector3s.Add(jiaodu * new Vector3(0, GaoDu, 0));
        }
        else
        {
            RotationDian(LingShi);
            V = 2;
        }
        BianLiangSangJiao();
        BianLiangUV();
    }
    private void RotationDian(Vector3 lingshi)
    {
        Vector3 BeforeRotate1 = lastChangeVec1 + lingshi;
        Vector3 zhongxin = new Vector3(BeforeRotate1.x, BeforeRotate1.y + GaoDu, BeforeRotate1.z);
        Vector3 final1 = jiaodu * (BeforeRotate1 - zhongxin);
        final1 += zhongxin;

        Vector3 BeforeRotate2 = lastChangeVec2 + lingshi;
        Vector3 final2 = jiaodu * (BeforeRotate2 - zhongxin);
        final2 += zhongxin;

        float phcd = (float)PingHuaChengDu / 100;
        if (Vector3s.Count == 1)
        {
            Vector3s.Add(Vector3.Lerp(Vector3s[Vector3s.Count - 1], final1, phcd));
            Vector3s.Add(Vector3.Lerp(jiaodu * new Vector3(0, GaoDu, 0), final2, phcd));
            lastChangeVec1 = BeforeRotate1;
            lastChangeVec2 = BeforeRotate2;
        }
        else
        {
            Vector3 first = Vector3.Lerp(Vector3s[Vector3s.Count - V], final1, phcd);
            lastChangeVec1 = BeforeRotate1;
            Vector3s.Add(first);
            Vector3 second = Vector3.Lerp(Vector3s[Vector3s.Count - V], final2, phcd);
            lastChangeVec2 = BeforeRotate2;
            Vector3s.Add(second);
            //if (Mathf.Abs(dotDir) > 0.35f)
            //{
            //    Vector3 first = Vector3.Lerp(Vector3s[Vector3s.Count - V], final1, phcd);
            //    lastChangeVec1 = BeforeRotate1;
            //    Vector3s.Add(first);

            //    Vector3 second = Vector3.Lerp(Vector3s[Vector3s.Count - V], final2, phcd);
            //    lastChangeVec2 = BeforeRotate2;
            //    Vector3s.Add(second);
            //}
            //else
            //{
            //    Vector3 dir = Vector3.Normalize(Vector3.ProjectOnPlane(Dir, Vector3.up));
            //    Vector3 lastdir = Vector3.Normalize(Vector3.ProjectOnPlane(lastDir, Vector3.up));
            //    float dotV = Mathf.Abs(Vector3.Dot(dir, lastdir));

            //    float jd = Mathf.Acos(dotDir) * Mathf.Rad2Deg / 2;
            //    float changdu = Mathf.Abs(GaoDu / Mathf.Sin(jd));
            //    Vector3 LingShiDir = Vector3.Normalize(Dir - lastDir);

            //    Vector3 pianyiVec = changdu * LingShiDir;
            //    if (LingShiDir.x < 0 || LingShiDir.y > 0)
            //    {
            //        pianyiVec *= -1;
            //    }
            //    Vector3 GuaiJiao4 = GuaiJiao2 + pianyiVec;
            //    Vector3 GuaiJiao3 = Vector3.Lerp(GuaiJiao1, GuaiJiao4, dotV);
            //    Vector3s.Add(GuaiJiao3);

            //    Vector3s.Add(GuaiJiao2);

            //    lastChangeVec1 = BeforeRotate1;
            //    lastChangeVec2 = BeforeRotate2;

            //    var cu3 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            //    cu3.transform.GetComponent<Renderer>().material.color = Color.blue;
            //    cu3.transform.position = GuaiJiao3;
            //    cu3.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
            //    var cu4 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            //    cu4.transform.GetComponent<Renderer>().material.color = Color.white;
            //    cu4.transform.position = GuaiJiao2;
            //    cu4.transform.localScale = new Vector3(0.4f, 0.4f, 0.4f);
            //}
            //GuaiJiao1 = final1;
            //GuaiJiao2 = final2;
        }
    }
    private void BianLiangSangJiao()
    {
        if (ints.Count - I < 0)
        {
            ints.Add(0);
            ints.Add(2);
            ints.Add(1);
            ints.Add(0);
            ints.Add(3);
            ints.Add(2);
        }
        else
        {
            ints.Add(Vector3s.Count - I);
            ints.Add(Vector3s.Count - 1);
            ints.Add(Vector3s.Count - 2);
            ints.Add(Vector3s.Count - I);
            ints.Add(Vector3s.Count - I + 1);
            ints.Add(Vector3s.Count - 1);
            I = 4;
        }
    }
    private void BianLiangUV()
    {
        if (uvs.Count - U < 0)
        {
            uvs.Add(new Vector2(0, 0));
            uvs.Add(new Vector2(JianGe, 0));
            uvs.Add(new Vector2(JianGe, 1));
            uvs.Add(new Vector2(0, 1));
        }
        else
        {
            U++;
            uvs.Add(new Vector2(U * JianGe, 0));
            uvs.Add(new Vector2(U * JianGe, 1));
        }
    }
    private void JiSuanDir()
    {
        Dir = Vector3.Normalize(XiangZaiPos.transform.position - YuanLaiPos);
        //dotDir = Vector3.Dot(Dir, lastDir);
        Vector3 rightDir = Vector3.Cross(new Vector3(Dir.x / 10, 1, Dir.y / 10), Dir);
        Vector3 updir = Vector3.Cross(Dir, rightDir);
        jiaodu = Quaternion.LookRotation(Dir, updir);
    }
}