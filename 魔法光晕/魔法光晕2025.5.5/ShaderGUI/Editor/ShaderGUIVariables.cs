using UnityEditor;

public class ShaderGUIVariables : ShaderGUI
{
    //纹理 纹理偏移缩放
    public class PropertyTex
    {
        public MaterialProperty BaseTex;//public MaterialProperty BaseTex_ST;//自动获取
        public MaterialProperty NormalTex;
        public MaterialProperty NoiseTex;
    }
    //曲线、渐变
    public class PropertyCurve
    {
        public MaterialProperty Curve1;
        public MaterialProperty Curve2;
        public MaterialProperty Curve3;
        public MaterialProperty RampGrad1;
        public MaterialProperty RampGrad2;
        public MaterialProperty RampGrad3;
    }
}