Shader "RainbowDash/ShenChenTouSheHouDu"
{
    Properties
    {
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" }
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment fragShadowTex
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/SpaceTransforms.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float RainbowDashTouSheHouDuCam_nearCliPplane;
                float RainbowDashHouDuCam_farCliPplane;
            CBUFFER_END

            struct a2v
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 texcoord : TEXCOORD0;
            };
            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 depth : TEXCOORD0;
            };

            float4 EncodeFloatRGBA( float v )
            {
                float4 kEncodeMul = float4(1.0, 255.0, 65025.0, 16581375.0);
                float kEncodeBit = 1.0/255.0;
                float4 enc = kEncodeMul * v;
                enc = frac (enc);
                enc -= enc.yzww * kEncodeBit;
                return enc;
            }

            v2f vert (a2v v)
            {
                v2f o;
                v.vertex.xyz += v.normal * 0.005;
                o.pos = TransformObjectToHClip(v.vertex.xyz);
                o.depth = mul(UNITY_MATRIX_MV, v.vertex).zz;
                return o;
            }
            half4 fragShadowTex (v2f i) : SV_Target
            {
                float depth = -i.depth.x / RainbowDashHouDuCam_farCliPplane - RainbowDashTouSheHouDuCam_nearCliPplane;
                #ifdef UNITY_REVERSED_Z
                    depth = 1 - depth;
                #endif
                half4 col = EncodeFloatRGBA(depth);
                return col;
            }
            ENDHLSL
        }
    }
}