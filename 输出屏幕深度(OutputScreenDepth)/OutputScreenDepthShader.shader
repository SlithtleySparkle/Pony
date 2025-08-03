Shader "Unlit/OutputScreenDepthShader"
{
    SubShader
    {
        Tags{ "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        Pass
        {
            ColorMask R
            Cull Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma shader_feature_local_fragment _ _SAME_AS_UNITY_RF
            #include "OutputScreenDepthInclude.hlsl"

            v2f vert (a2v v)
            {
                v2f o;
                float3 positionWS = TransformObjectToWorld(v.positionOS.xyz);
                o.positionCS = TransformWorldToHClip(positionWS);
                o.positionVS.z = TransformWorldToView(positionWS).z;
                o.positionVS.xy = o.positionCS.zw;
                return o;
            }
            ENDHLSL
        }
    }
}