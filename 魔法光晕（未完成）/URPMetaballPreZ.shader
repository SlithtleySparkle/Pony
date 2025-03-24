Shader "Unlit/URPMetaballPreZ"
{
    Properties
    {
        // _MagicGuangYun ("魔法光晕大小", Float) = 1
        // _MagicFluSpeed ("魔法光晕流动速度", Float) = 1
        // [NoScaleOffset] _MagicMetaballNoise ("噪声", 3D) = "white" {}
        // _MagicMBNoiScale ("噪声缩放", Vector) = (1, 1, 1, 1)
        // _MagicMetaballNoiTint ("噪声强度", Float) = 1
    }
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }
        Pass
        {
			Cull Off
            ZWrite On
            ColorMask 0
            HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            // #include "MagicMetaballURPInc.hlsl"

			struct a2v
            {
                float4 positionOS : POSITION;
				// float4 tangentOS : TANGENT;
    //             float4 vertCol : COLOR;
    //             float3 normalOS : NORMAL;
            };
			struct v2f 
			{
				float4 positionCS : SV_POSITION;
			};
			v2f vert(a2v v)
			{
				v2f o;
                // GetMetaballFluPosWS(v.positionOS.xyz, v.tangentOS, v.normalOS, v.vertCol.xyz);
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
				return o;
			}
            float4 frag(v2f i) : SV_Target
			{
                return 0;
			}
			ENDHLSL
        }
    }
}