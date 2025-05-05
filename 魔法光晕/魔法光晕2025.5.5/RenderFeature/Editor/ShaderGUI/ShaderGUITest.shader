Shader "Unlit/ShaderGUITest"
{
    Properties
    {
        [Enum(Front, 0, Back, 1, Off, 2)] _Cull ("Cull", Float) = 0
        _Alpha ("透明度", Range(0, 1)) = 1

        // [Toggle] _BaseTexTog ("使用主贴图", Float) = 1//不需要
        [UseTex][NoScaleOffset] _BaseTex ("主纹理", 2D) = "white" {}

        [UseTex] _NormalTex ("法线", 2D) = "white" {}

        [UseTex] _NoiseTex ("噪声", 2D) = "white" {}
        _NoistTint ("噪声强度", Float) = 1

        [CurveRamp] _CurveTex ("曲线纹理", 2D) = "white" {}
        [CurveRamp] _Curvex2 ("曲纹理2", 2D) = "white" {}

        [RampGrad] _RampGradTex ("多颜色渐变纹理", 2D) = "white" {}
        [RampGrad] _RampGradTex2 ("多颜色2", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Cull [_Cull]
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            //是否使用贴图的开关命名：贴图名 + TOG_ON
            #pragma shader_feature_local_fragment _ _BASETEXTOG_ON
            #pragma shader_feature_local_fragment _ _NORMALTEXTOG_ON
            #pragma shader_feature_local_fragment _ _NOISETEXTOG_ON
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float _Alpha;
                float _NoistTint;
            CBUFFER_END

            TEXTURE2D(_BaseTex);
            SAMPLER(sampler_BaseTex);
            TEXTURE2D(_NormalTex);
            SAMPLER(sampler_NormalTex);
            TEXTURE2D(_NoiseTex);
            SAMPLER(sampler_NoiseTex);
            TEXTURE2D(_CurveTex);
            SAMPLER(sampler_CurveTex);
            TEXTURE2D(_Curvex2);
            SAMPLER(sampler_Curvex2);
            TEXTURE2D(_RampGradTex);
            SAMPLER(sampler_RampGradTex);
            TEXTURE2D(_RampGradTex2);
            SAMPLER(sampler_RampGradTex2);

            struct a2v
            {
                float4 positionOS : POSITION;
                float2 texcoord : TEXCOORD0;
            };
            struct v2f
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };


            v2f vert (a2v v)
            {
                v2f o;
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.uv = v.texcoord;
                return o;
            }

            float4 frag (v2f i) : SV_Target
            {
                float3 col = float3(0, 0, 0);
                #if defined(_BASETEXTOG_ON)
                    col = SAMPLE_TEXTURE2D(_BaseTex, sampler_BaseTex, i.uv).rgb;
                #else
                    col.x = _Alpha;
                #endif
                float curveAlpha = SAMPLE_TEXTURE2D(_CurveTex, sampler_CurveTex, float2(i.uv.x, 0)).r;

                float3 ramp2 = SAMPLE_TEXTURE2D(_RampGradTex, sampler_RampGradTex, i.uv).rgb;
                return float4(ramp2, curveAlpha);
            }
            ENDHLSL
        }
    }
    CustomEditor "MagicMetaballShaderGUI"
    //为特定渲染管线指定额外的CustomEditor
    //CustomEditorForRenderPipeline "OtherExampleRenderPipelineShaderGUI" "OtherExampleRenderPipelineAsset"
}