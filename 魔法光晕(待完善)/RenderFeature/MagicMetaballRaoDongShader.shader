Shader "Unlit/MagicMetaballRaoDongShader"
{
    Properties
    {
        _MetaballRaoDongNoise ("魔法光晕噪声", 2D) = "white" {}
        _MetaballRaoDongTint ("魔法光晕噪声强度", Range(0, 1)) = 0.5
        _MetaballFluSpeed ("魔法光晕流动速度", Float) = 1
    }
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }
        //魔法光晕相机
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float _MetaballRaoDongTint;
                float _MetaballFluSpeed;
            CBUFFER_END

            TEXTURE2D(_GrabOtherColorTex);
            SAMPLER(sampler_GrabOtherColorTex);
            TEXTURE2D(_MetaballRaoDongNoise);
            SAMPLER(sampler_MetaballRaoDongNoise);

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
            float4 frag(v2f i) : SV_Target
            {
                float2 noiseuv = float2(i.uv.x, i.uv.y + _Time.x / 10 * _MetaballFluSpeed);
                float noise = SAMPLE_TEXTURE2D(_MetaballRaoDongNoise, sampler_MetaballRaoDongNoise, noiseuv) * _MetaballRaoDongTint / 10;

                #if defined(UNITY_UV_STARTS_AT_TOP)
                    i.uv.x -= noise;
                #endif
                float4 finalCol = SAMPLE_TEXTURE2D(_GrabOtherColorTex, sampler_GrabOtherColorTex, i.uv);
                return SAMPLE_TEXTURE2D(_GrabOtherColorTex, sampler_GrabOtherColorTex, i.uv);
            }
            ENDHLSL
        }
        //主相机
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"

            TEXTURE2D(_GrabColorTex);
            SAMPLER(sampler_GrabColorTex);
            TEXTURE2D(_MetaballCmaeraTex);
            SAMPLER(sampler_MetaballCmaeraTex);
            TEXTURE2D(_GrabDepthTex);
            SAMPLER(sampler_GrabDepthTex);
            TEXTURE2D(_MetaballDepthTex);
            SAMPLER(sampler_MetaballDepthTex);

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
                float mainCamDep = SAMPLE_TEXTURE2D(_GrabDepthTex, sampler_GrabDepthTex, i.uv).r;
                float mbCamDep = SAMPLE_TEXTURE2D(_MetaballDepthTex, sampler_MetaballDepthTex, i.uv).r;

                float ismb = step(mainCamDep, mbCamDep);
                float4 mbColor = SAMPLE_TEXTURE2D(_MetaballCmaeraTex, sampler_MetaballCmaeraTex, i.uv) * ismb;

                float4 mainCol = SAMPLE_TEXTURE2D(_GrabColorTex, sampler_GrabColorTex, i.uv);
                float4 finalColor = float4(mainCol.rgb * (1 - mbColor.a) + mbColor.rgb * mbColor.a, mainCol.a);

                return finalColor;
            }
            ENDHLSL
        }
    }
}