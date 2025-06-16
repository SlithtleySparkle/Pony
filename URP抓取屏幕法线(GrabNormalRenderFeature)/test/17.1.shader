Shader "Unlit/17.1"
{
	Properties
	{
		_BumpMap ("Normalmap", 2D) = "bump" {}
	}
	SubShader
	{
		Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        Pass
		{
			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

			TEXTURE2D(_BumpMap);
            SAMPLER(sampler_BumpMap);

			struct a2v
            {
                float4 positionOS : POSITION;
                float4 tangentOS : TANGENT;
                float3 normalOS : NORMAL;
                float2 texcoord : TEXCOORD0;
            };
            struct v2f
            {
                float4 positionCS : SV_POSITION;
                float3 uv : TEXCOORD0;

                float3 TtoW0 : TEXCOORD1;
                float3 TtoW1 : TEXCOORD2;
                float3 TtoW2 : TEXCOORD3;
            };

			v2f vert (a2v v)
            {
                v2f o;
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.uv = float3(v.texcoord, o.positionCS.w);

                v.tangentOS.w *= unity_WorldTransformParams.w;
                float3 normalWS = TransformObjectToWorldNormal(v.normalOS, true);
                float3 tangentWS = TransformObjectToWorldDir(v.tangentOS.xyz, true);
                float3 binormalWS = cross(normalWS, tangentWS) * v.tangentOS.w;

                o.TtoW0 = float3(tangentWS.x, binormalWS.x, normalWS.x);
                o.TtoW1 = float3(tangentWS.y, binormalWS.y, normalWS.y);
                o.TtoW2 = float3(tangentWS.z, binormalWS.z, normalWS.z);

                return o;
            }
			float4 frag (v2f i) : SV_Target
            {
                Light Mainlightdata = GetMainLight();

                float2 uv = i.uv.xy;
                float4 sampleNormal = SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, uv);
                float3 normalTS = UnpackNormalScale(sampleNormal, 1);
                float3 normalWS = float3(dot(i.TtoW0, normalTS), dot(i.TtoW1, normalTS), dot(i.TtoW2, normalTS));

                float diffuse = dot(Mainlightdata.direction, normalWS);

                return diffuse;
            }
			ENDHLSL
		}
		Pass
		{
            Tags { "LightMode" = "GrabNormalPassTag" }
			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

			TEXTURE2D(_BumpMap);
            SAMPLER(sampler_BumpMap);

			struct a2v
            {
                float4 positionOS : POSITION;
                float4 tangentOS : TANGENT;
                float3 normalOS : NORMAL;
                float2 texcoord : TEXCOORD0;
            };
            struct v2f
            {
                float4 positionCS : SV_POSITION;
                float3 uv : TEXCOORD0;

                float3 TtoW0 : TEXCOORD1;
                float3 TtoW1 : TEXCOORD2;
                float3 TtoW2 : TEXCOORD3;
            };

			v2f vert (a2v v)
            {
                v2f o;
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.uv = float3(v.texcoord, o.positionCS.w);

                v.tangentOS.w *= unity_WorldTransformParams.w;
                float3 normalWS = TransformObjectToWorldNormal(v.normalOS, true);
                float3 tangentWS = TransformObjectToWorldDir(v.tangentOS.xyz, true);
                float3 binormalWS = cross(normalWS, tangentWS) * v.tangentOS.w;

                o.TtoW0 = float3(tangentWS.x, binormalWS.x, normalWS.x);
                o.TtoW1 = float3(tangentWS.y, binormalWS.y, normalWS.y);
                o.TtoW2 = float3(tangentWS.z, binormalWS.z, normalWS.z);

                return o;
            }
			float4 frag (v2f i) : SV_Target
            {
                float2 uv = i.uv.xy;
                float4 sampleNormal = SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, uv);
                float3 normalTS = UnpackNormalScale(sampleNormal, 1);
                float3 normalWS = float3(dot(i.TtoW0, normalTS), dot(i.TtoW1, normalTS), dot(i.TtoW2, normalTS));
                return float4(PackNormalOctQuadEncode(normalWS), 0, 1);
            }
			ENDHLSL
		}
	} 
}