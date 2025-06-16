Shader "Unlit/GrabNormalShader"
{
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct a2v
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };
            struct v2f
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
            };

            v2f vert (a2v v)
            {
                v2f o;
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.normalWS = TransformObjectToWorldNormal(v.normalOS, true);
                return o;
            }
            float4 frag (v2f i) : SV_Target
            {
                //解码使用UnpackNormalOctQuadEncode(float2)
                return float4(PackNormalOctQuadEncode(i.normalWS), 0, 1);
            }
            ENDHLSL
        }
    }
}