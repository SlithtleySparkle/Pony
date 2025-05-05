using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;
using System.Collections.Generic;

public class MagicMetaballShaderGUI : ShaderGUIVariables
{
    private bool useOrignalGUI = true;

    private Material mat;
    private Shader shader;
    private bool useTip = true;
    private Color originalCol = GUI.backgroundColor;
    private string texSavedPath = "Asstes/";//纹理保存路径
    private string configSavedPath = "Asstes/";//设置保存路径
    private const string texExtension = "png";
    private const string texDefaultName = "defaultTex";
    private const string configDefaultName = "defaultConfig";
    private float lastUpdateTime = 0;
    //单个保存   一次操作一个
    private bool[] saveCol = new bool[] { false, false, false, false };//curves和gradients的总数
    private string singleCurveSaveName = "";
    private string singleGradSaveName = "";
    private int clickNum = 0;

    //自定义Shader标识
    private static string[] CustomShaderFlags = new string[]
    {
        "UseTex",
        "CurveRamp",
        "RampGrad"
    };
    private PropertyTex propertyTex = new();
    private PropertyCurve propertyCurve = new();

    //曲线
    //要按Shader中的顺序排列
    private AnimationCurve[] curves = new AnimationCurve[]
    {
        new(new Keyframe(0, 0), new Keyframe(1, 1)),
        new(new Keyframe(0, 0), new Keyframe(1, 1))
    };
    private Texture2D[] curvesTex = new Texture2D[]
    {
        null,
        null
    };
    private bool[] isCurveChanged = new bool[] { false, false };//不用数组会导致始终保存第一个曲线
    private int curveTexWidth = 256;
    private int curveIndex = 0;

    //多颜色渐变
    private bool[] useGrad = new bool[] { true, true };
    private List<Gradient>[] gradients = new List<Gradient>[]
    {
        new(),
        new()
    };
    private Texture2D[] gradsTex = new Texture2D[] { null, null };
    private bool[] isGradChanged = new bool[] { false, false };
    private int[] gradNum = new int[] { 1, 1 };
    private int perRampGradHeight = 32;
    private int rampGradTexWidth = 256;
    private int gradIndex = 0;

    public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
    {
        useOrignalGUI = EditorGUILayout.BeginToggleGroup("使用原材质面板", useOrignalGUI);
        EditorGUILayout.EndToggleGroup();
        if (!useOrignalGUI)
        {
            useOrignalGUI = false;
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            useTip = EditorGUILayout.BeginToggleGroup("提示", useTip);
            EditorGUILayout.EndToggleGroup();
            if (useTip)
            {
                GUIStyle gUIStyle = GUI.skin.label;
                useTip = true;
                gUIStyle.alignment = TextAnchor.MiddleCenter;
                EditorGUILayout.LabelField("设置完一定要保存！！！", gUIStyle);
                EditorGUILayout.LabelField("保存的曲线纹理要把sRGB去掉！！！", gUIStyle);
                EditorGUILayout.LabelField("设置保存路径的时候不要修改默认名！！！", gUIStyle);
                EditorGUILayout.LabelField("当不再设置时，可以选择使用原材质面板", gUIStyle);
                EditorGUILayout.IntField("曲线纹理宽度", curveTexWidth);
                EditorGUILayout.IntField("单RampGrad纹理高度", perRampGradHeight);
                EditorGUILayout.IntField("RampGrad纹理宽度", rampGradTexWidth);
                SetSavePath();
            }
            else
            {
                useTip = false;
            }
            EditorGUILayout.EndVertical();

            mat = materialEditor.target as Material;
            shader = mat.shader;
            curveIndex = 0;
            gradIndex = 0;
            UpdateProperty(properties);

            if (texSavedPath != "Asstes/" && configSavedPath != "Asstes/")
            //if (1 == 1)
            {
                for (int i = 0; i < properties.Length; i++)
                {
                    //返回数组，是[]内的标识，部分不返回 https://docs.unity3d.com/cn/current/ScriptReference/Rendering.ShaderPropertyFlags.html
                    var attributes = shader.GetPropertyAttributes(i);
                    if ((properties[i].flags & (MaterialProperty.PropFlags.HideInInspector | MaterialProperty.PropFlags.PerRendererData)) == MaterialProperty.PropFlags.None)
                    {
                        bool isCustom = false;
                        foreach (var attr in attributes)
                        {
                            if (CustomShaderFlags.Contains(attr))
                            {
                                isCustom = true;
                                DrawCustomShaderGUI(materialEditor, properties[i], attr);
                            }
                        }
                        if (!isCustom)
                        {
                            //默认绘制
                            materialEditor.ShaderProperty(properties[i], properties[i].displayName);
                        }
                    }
                }

                EditorGUILayout.Space(5);
                EditorGUILayout.BeginHorizontal();
                //https://zhuanlan.zhihu.com/p/537683267
                if (GUILayout.Button("保存全部"))
                {
                    var asset_save = ScriptableObject.CreateInstance<ShaderGUI_Config>();
                    asset_save.Curves = curves;

                    List<Gradient> temp = new();
                    int[] num = new int[gradients.Length];
                    for (int i = 0; i < gradients.Length; i++)
                    {
                        for (int it = 0; it < gradients[i].Count; it++)
                        {
                            temp.Add(gradients[i][it]);
                            num[i]++;
                        }
                    }
                    asset_save.Gradients = temp.ToArray();
                    asset_save.num = num;

                    string shaderName = Path.GetFileName(shader.name);
                    AssetDatabase.CreateAsset(asset_save, configSavedPath.Replace(configDefaultName + ".", shaderName + "Config_all."));
                }
                if (GUILayout.Button("读取全部设置"))
                {
                    string readConfig = EditorUtility.OpenFilePanel("读取", Application.dataPath, "asset").Substring(Application.dataPath.Length);
                    if (!string.IsNullOrEmpty(readConfig))
                    {
                        //绝对路径
                        var asset_read = AssetDatabase.LoadAssetAtPath("Assets" + readConfig, typeof(ShaderGUI_Config)) as ShaderGUI_Config;
                        curves = asset_read.Curves;
                        gradNum = asset_read.num;

                        int j = 0;
                        for (int i = 0; i < gradNum.Length; i++)
                        {
                            gradients[i]?.Clear();
                            for (int it = 0; it < gradNum[i]; it++)
                            {
                                gradients[i].Add(asset_read.Gradients[j++]);
                            }
                        }
                    }
                }
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.Space(10);
            if (SupportedRenderingFeatures.active.editableMaterialRenderQueue)
            {
                materialEditor.RenderQueueField();
            }
            materialEditor.EnableInstancingField();
            materialEditor.DoubleSidedGIField();
        }
        else
        {
            useOrignalGUI = true;
            base.OnGUI(materialEditor, properties);
        }
    }
    public override void OnClosed(Material material)
    {

    }

    //绘制自定义属性   使用自定义标记
    public void DrawCustomShaderGUI(MaterialEditor materialEditor, MaterialProperty prop, string shaderFlag)
    {
        //绘制
        //materialEditor.RangeProperty(propertyFloat.Alpha, "透明度");
        //materialEditor.FloatProperty(propertyFloat.NoistTint, "噪声强度");

        //贴图
        if (shaderFlag == CustomShaderFlags[0])
        {
            //属性框   开始和结尾成对出现
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            string keywordName = prop.name.ToUpper() + "TOG_ON";
            bool isuseTex = mat.IsKeywordEnabled(keywordName);
            isuseTex = EditorGUILayout.BeginToggleGroup("使用" + prop.displayName, isuseTex);
            EditorGUILayout.EndToggleGroup();

            //必须要有判断，否则无法修改Bool
            if (isuseTex)
            {
                mat.EnableKeyword(keywordName);
                EditorGUIUtility.fieldWidth = 64;
                materialEditor.ShaderProperty(prop, "");
            }
            else
            {
                mat.DisableKeyword(keywordName);
            }

            EditorGUILayout.EndVertical();
        }

        //曲线
        if (shaderFlag == CustomShaderFlags[1])
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginChangeCheck();

            string curve_savepath = texSavedPath.Replace(texDefaultName + ".", "Curve" + prop.name + ".");
            curves[curveIndex] = EditorGUILayout.CurveField(prop.displayName, curves[curveIndex], Color.green, new(0, 0, 1, 1));
            if (curvesTex[curveIndex] == null)
            {
                curvesTex[curveIndex] = BakeCurveToTexture(curveTexWidth, 1, TextureFormat.RFloat, false, curves[curveIndex]);
            }

            if (EditorGUI.EndChangeCheck())
            {
                lastUpdateTime = Time.realtimeSinceStartup;
                isCurveChanged[curveIndex] = true;

                curvesTex[curveIndex] = BakeCurveToTexture(curveTexWidth, 1, TextureFormat.RFloat, false, curves[curveIndex]);
                mat.SetTexture(prop.name, curvesTex[curveIndex]);
                materialEditor.PropertiesChanged();
            }

            SingleSaveAndRead(singleCurveSaveName, true, curveIndex);

            //Event.current.type == EventType.MouseUp  检测不到曲线编辑框
            if (Time.realtimeSinceStartup - lastUpdateTime > 0.5f && isCurveChanged[curveIndex])
            {
                isCurveChanged[curveIndex] = false;
                //保存
                byte[] curve_png = curvesTex[curveIndex].EncodeToPNG();
                File.WriteAllBytes(curve_savepath, curve_png);
                AssetDatabase.Refresh();
                //使用
                Texture2D savedTex = AssetDatabase.LoadAssetAtPath<Texture2D>(curve_savepath);
                mat.SetTexture(prop.name, savedTex);
            }
            curveIndex++;
        }

        //彩色渐变Ramp
        if (shaderFlag == CustomShaderFlags[2])
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            string grad_savepath = texSavedPath.Replace(texDefaultName + ".", "Gradient" + prop.name + ".");

            EditorGUILayout.BeginHorizontal();
            useGrad[gradIndex] = EditorGUILayout.BeginToggleGroup("彩色渐变", useGrad[gradIndex]);
            EditorGUILayout.EndToggleGroup();

            SingleSaveAndRead(singleGradSaveName, false, gradIndex);

            if (useGrad[gradIndex])
            {
                useGrad[gradIndex] = true;
                gradNum[gradIndex] = EditorGUILayout.IntSlider(gradNum[gradIndex], 0, 8);
                //检查数量
                if (gradNum[gradIndex] != gradients[gradIndex].Count)
                {
                    while (gradients[gradIndex].Count > gradNum[gradIndex])
                    {
                        gradients[gradIndex].RemoveAt(gradients[gradIndex].Count - 1);
                    }
                    while (gradients[gradIndex].Count < gradNum[gradIndex])
                    {
                        gradients[gradIndex].Add(new());
                    }
                }

                EditorGUI.BeginChangeCheck();
                for (int i = 0; i < gradients[gradIndex].Count; i++)
                {
                    gradients[gradIndex][i] = EditorGUILayout.GradientField(gradients[gradIndex][i]);
                }
                if (gradsTex[gradIndex] == null)
                {
                    gradsTex[gradIndex] = BakeCurveToTexture(rampGradTexWidth, perRampGradHeight, TextureFormat.RGBA32, true, null, gradients[gradIndex]);
                }

                if (EditorGUI.EndChangeCheck())
                {
                    lastUpdateTime = Time.realtimeSinceStartup;
                    isGradChanged[gradIndex] = true;

                    gradsTex[gradIndex] = BakeCurveToTexture(rampGradTexWidth, perRampGradHeight, TextureFormat.RGBA32, true, null, gradients[gradIndex]);
                    mat.SetTexture(prop.name, gradsTex[gradIndex]);
                    materialEditor.PropertiesChanged();
                }

                if (Time.realtimeSinceStartup - lastUpdateTime > 1 && isGradChanged[gradIndex])
                {
                    isGradChanged[gradIndex] = false;
                    //保存
                    byte[] grad_png = gradsTex[gradIndex].EncodeToPNG();
                    File.WriteAllBytes(grad_savepath, grad_png);
                    AssetDatabase.Refresh();
                    //使用
                    Texture2D savedTex = AssetDatabase.LoadAssetAtPath<Texture2D>(grad_savepath);
                    mat.SetTexture(prop.name, savedTex);
                }
            }
            else
            {
                useGrad[gradIndex] = false;
            }
            EditorGUILayout.EndVertical();
            gradIndex++;
        }
    }
    //设置属性
    public void UpdateProperty(MaterialProperty[] properties)
    {
        propertyTex.BaseTex = FindProperty("_BaseTex", properties, true);
        propertyTex.NormalTex = FindProperty("_NormalTex", properties, true);
        propertyTex.NoiseTex = FindProperty("_NoiseTex", properties, true);

        propertyCurve.Curve1 = FindProperty("_CurveTex", properties, true);
        propertyCurve.Curve2 = FindProperty("_Curvex2", properties, true);
        propertyCurve.RampGrad1 = FindProperty("_RampGradTex", properties, true);
        propertyCurve.RampGrad2 = FindProperty("_RampGradTex2", properties, true);
    }
    //烘焙到纹理
    public Texture2D BakeCurveToTexture(int width, int height, TextureFormat format, bool isRGBA, AnimationCurve curve, List<Gradient> gradients = null)
    {
        Texture2D tex;
        if (isRGBA)
        {
            int hei = gradients.Count * height;
            int CountMinusOne = gradients.Count - 1;
            tex = new(width, hei, format, false)
            {
                filterMode = FilterMode.Bilinear,
                ignoreMipmapLimit = true,
            };

            for (int num = CountMinusOne; num >= 0; num--)
            {
                for (int i = 0; i < width; i++)
                {
                    float x = i / (float)(width - 1);
                    Color col = gradients[num].Evaluate(x);
                    //for (int it = 0; Mathf.FloorToInt((float)it / height) == CountMinusOne - num; it++)
                    int a = (CountMinusOne - num) * height;
                    for (int it = a; it < a + height; it++)
                    {
                        tex.SetPixel(i, it, col);
                    }
                }
            }
        }
        else
        {
            tex = new(width, height, format, false)
            {
                filterMode = FilterMode.Bilinear,
                ignoreMipmapLimit = true
            };
            for (int i = 0; i < width; i++)
            {
                float x = i / (float)(width - 1);
                float y = curve.Evaluate(x);
                tex.SetPixel(i, 0, new(y, 0, 0, 1));
            }
        }
        tex.Apply();
        return tex;
    }
    //设置保存路径
    public void SetSavePath()
    {
        //纹理
        EditorGUILayout.BeginHorizontal();
        if (texSavedPath == "Asstes/")
        {
            GUI.backgroundColor = Color.red;
        }
        if (GUILayout.Button("Ramp图保存路径"))
        {
            texSavedPath = EditorUtility.SaveFilePanelInProject("保存到", texDefaultName, texExtension, "", texSavedPath);
        }
        GUI.backgroundColor = originalCol;

        //Config
        if (configSavedPath == "Asstes/")
        {
            GUI.backgroundColor = Color.red;
        }
        if (GUILayout.Button("Config保存路径"))
        {
            configSavedPath = EditorUtility.SaveFilePanelInProject("保存到", configDefaultName, "asset", "", configSavedPath);
        }
        GUI.backgroundColor = originalCol;
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space(10);
    }
    //单一读取保存
    public void SingleSaveAndRead(string saveName, bool CurOrGrad, int i)
    {
        if (!CurOrGrad)
        {
            i += curves.Length;
        }
        if (saveCol[i])
        {
            GUI.backgroundColor = Color.green;
        }
        //点击一次→输入文件名，点击两次→保存
        if (GUILayout.Button("保存"))
        {
            clickNum++;
            bool isnullemp = string.IsNullOrEmpty(saveName);
            if (isnullemp && clickNum == 2)
            {
                clickNum = 0;
                saveCol[i] = false;
            }
            if (GUI.backgroundColor != Color.green)
            {
                saveCol[i] = true;
            }
            if (!isnullemp)
            {
                saveCol[i] = false;
                if (CurOrGrad)
                {
                    var asset_save = ScriptableObject.CreateInstance<ShaderGUIConfig_SingleCurve>();
                    asset_save.SignleCurve = curves[i];
                    AssetDatabase.CreateAsset(asset_save, configSavedPath.Replace(configDefaultName + ".", saveName + "."));
                }
                else
                {
                    var asset_save = ScriptableObject.CreateInstance<ShaderGUIConfig_SingleGrad>();
                    asset_save.SingleGradient = gradients[i - 2].ToArray();
                    asset_save.num = gradients[i - 2].Count;
                    AssetDatabase.CreateAsset(asset_save, configSavedPath.Replace(configDefaultName + ".", saveName + "."));
                }
            }
        }
        if (saveCol[i])
        {
            GUI.backgroundColor = originalCol;
        }

        if (GUILayout.Button("读取"))
        {
            string readConfig = EditorUtility.OpenFilePanel("读取", Application.dataPath, "asset").Substring(Application.dataPath.Length);
            if (!string.IsNullOrEmpty(readConfig))
            {
                if (CurOrGrad)
                {
                    var asset_read = AssetDatabase.LoadAssetAtPath("Assets" + readConfig, typeof(ShaderGUIConfig_SingleCurve)) as ShaderGUIConfig_SingleCurve;
                    curves[i] = asset_read.SignleCurve;
                }
                else
                {
                    var asset_read = AssetDatabase.LoadAssetAtPath("Assets" + readConfig, typeof(ShaderGUIConfig_SingleGrad)) as ShaderGUIConfig_SingleGrad;
                    gradNum[gradIndex] = asset_read.num;
                    gradients[i - 2] = new(asset_read.SingleGradient.ToList());
                }
            }
        }
        EditorGUILayout.EndHorizontal();
        if (saveCol[i] && clickNum == 1)
        {
            EditorGUILayout.BeginVertical();
            if (CurOrGrad)
            {
                singleCurveSaveName = EditorGUILayout.TextField(singleCurveSaveName);
            }
            else
            {
                singleGradSaveName = EditorGUILayout.TextField(singleGradSaveName);
            }
            GUILayout.EndVertical();
        }
    }
}