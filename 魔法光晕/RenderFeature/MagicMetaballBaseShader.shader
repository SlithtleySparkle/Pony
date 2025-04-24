Shader "Unlit/MagicMetaballBaseShader"
{
    Properties
    {
        [IntRange] _MBRenderLayerMaskID ("RenderLayerMaskID", Range(0, 8)) = 0
        _MagicMetaballColor ("魔法光晕颜色，脚本控制", Color) = (1, 1, 1, 1)
        _MagicMetaballOutline ("魔法光晕外圈宽度", Range(0, 2)) = 0.125
        _MagicMBOutlineAlpha ("魔法光晕外圈透明度", Range(0, 1)) = 0.15
        _MagicMetaballInline ("魔法光晕内圈宽度", Range(0, 1)) = 0.4
        _MagicMBInlineAlpha ("魔法光晕外圈透明度", Range(0, 1)) = 0.75
    }
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }
        //分块渲染角色
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZTest Off
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            #define ONECTHREE 0.333333

            CBUFFER_START(UnityPerMaterial)
                float _MBRenderLayerMaskID;
                float4 _MagicMetaballColor;
            CBUFFER_END

            //魔法光晕大小
            float _MetaballWidthTint;

            float4x4 _MBPerObjWtoPMatrix[9];

            struct a2v
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };
            struct v2f
            {
                float4 positionCS : SV_POSITION;
                float3 screenUV : TEXCOORD0;
            };
            v2f vert (a2v v)
            {
                v2f o;
                v.positionOS.xyz += max(0, _MetaballWidthTint) / 10 * v.normalOS;

                float3 positionWS = TransformObjectToWorld(v.positionOS.xyz);

                o.positionCS = mul(_MBPerObjWtoPMatrix[_MBRenderLayerMaskID], float4(positionWS, 1.0));
                o.screenUV = o.positionCS.xyw;
                return o;
            }
            float4 frag(v2f i) : SV_Target
            {
                float4 finalCol = float4(0, 0, 0, 0);

                float row = _MBRenderLayerMaskID % 3 + 1;
                float column = 3 - floor(_MBRenderLayerMaskID / 3);
                i.screenUV.xy = i.screenUV.xy / i.screenUV.z * 0.5 + float2(0.5, 0.5);
                #if defined(UNITY_UV_STARTS_AT_TOP)
                    i.screenUV.y = 1 - i.screenUV.y;
                #endif
                if (i.screenUV.x < row * ONECTHREE && i.screenUV.x > (row - 1) * ONECTHREE && i.screenUV.y > (column - 1) * ONECTHREE && i.screenUV.y < column * ONECTHREE)
                {
                    finalCol = float4(_MagicMetaballColor.rgb, 1);
                }

                return finalCol;
            }
            ENDHLSL
        }

        //模糊
        UsePass "Unlit/ObjectBloomShader/ObjBloomShaderVBlurPass"
        UsePass "Unlit/ObjectBloomShader/ObjBloomShaderHBlurPass"
        UsePass "Unlit/ObjectBloomShader/ObjBloomShaderUpSamplePass"
        //限制在范围内
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float _MagicMetaballOutline;
                float _MagicMBOutlineAlpha;
                float _MagicMetaballInline;
                float _MagicMBInlineAlpha;
            CBUFFER_END

            TEXTURE2D(_UpSampleBloomTex);
            SAMPLER(sampler_UpSampleBloomTex);

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
                float4 finalCol = saturate(SAMPLE_TEXTURE2D(_UpSampleBloomTex, sampler_UpSampleBloomTex, i.uv));
                float alpha = finalCol.a;
                float alphaMask = step(_MagicMetaballOutline / 10, alpha);

                float outAlpha = step(alpha, 0.2) * alphaMask;
                float inAlpha = (1 - outAlpha) * alphaMask * smoothstep(_MagicMetaballInline, 1, 1 - alpha) * _MagicMBInlineAlpha;
                outAlpha *= _MagicMBOutlineAlpha;

                finalCol.rgb *= inAlpha + outAlpha;

                return saturate(float4(finalCol.rgb, inAlpha + outAlpha));
            }
            ENDHLSL
        }
        //主相机Debug
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

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

                // return finalColor;
                // return mainCol;
                return SAMPLE_TEXTURE2D(_MetaballCmaeraTex, sampler_MetaballCmaeraTex, i.uv);
            }
            ENDHLSL
        }
    }
}