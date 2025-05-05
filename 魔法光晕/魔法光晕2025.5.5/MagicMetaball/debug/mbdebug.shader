Shader "Unlit/mbdebug"
{
    SubShader
    {
        // Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" }
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        LOD 100
        Pass
        {
            // Tags { "LightMode" = "MagicMetaball" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MetaballNormalWSTex);
            SAMPLER(sampler_MetaballNormalWSTex);
            // TEXTURE2D(_MetaballNormalWSTex_debug);
            // SAMPLER(sampler_MetaballNormalWSTex_debug);

            TEXTURE2D(_GrabOtherDepthTex);
            SAMPLER(sampler_GrabOtherDepthTex);

            // float DecodeFloatRGBA(float4 enc)
            // {
            //     float4 kDecodeDot = float4(1.0, 1/255.0, 1/65025.0, 1/16581375.0);
            //     return dot( enc, kDecodeDot );
            // }

            struct a2v
            {
                float4 positionOS : POSITION;
                float2 texcoord : TEXCOORD0;
                float3 normalOS : NORMAL;
            };
            struct v2f
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
            };


            v2f vert(a2v v)
            {
                v2f o;
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.uv = v.texcoord;
                o.normalWS = TransformObjectToWorldNormal(v.normalOS, true);
                return o;
            }

            float4 frag (v2f i) : SV_Target
            {
                float4 normalWST = SAMPLE_TEXTURE2D(_MetaballNormalWSTex, sampler_MetaballNormalWSTex, i.uv);
                // float4 depth = SAMPLE_TEXTURE2D(_GrabOtherDepthTex, sampler_GrabOtherDepthTex, i.uv);
                // float mask = DecodeFloatRGBA(SAMPLE_TEXTURE2D(_MetaballNormalWSTex_debug, sampler_MetaballNormalWSTex_debug, i.uv));
                return float4(i.normalWS, 1);
                // return float4(mask, normalWST.x, 0, 1);
            }
            ENDHLSL
        }
    }
}