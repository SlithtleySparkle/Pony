Shader "Unlit/PerObjShadowShader"
{
	SubShader
	{
		//Pass1：包含透明物体，官方深度w(1-)、顶点色y。Pass2：无透明物体官方深度z。Pass3：包含透明物体，仅角色官方深度x。Pass4：渲染刘海遮罩。
		//A用于自身PCSS，G用于透明阴影，               B用于自身阴影，             R用于普通的PCSS
		//        白到黑                               黑到白                      白到黑
		Tags { "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" }
		Pass
		{
			ColorMask GA
			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma shader_feature_local_fragment _ _PEROBJSHADOW_SIGN
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

			float _PerObjNumber;
			float _PerObjNumSqrt;

			float4x4 _PerObjShadowWtoPMatr;

			struct a2v
			{
				float4 positionOS : POSITION;
				float3 vertexCol : COLOR0;
			};
			struct v2f
			{
				float4 positionCS : SV_POSITION;
				float4 screenUV : TEXCOORD0;
				float3 vertCol : TEXCOORD1;
			};

			v2f vert (a2v v)
			{
				v2f o;
				float3 positionWS = TransformObjectToWorld(v.positionOS.xyz);
				o.positionCS = mul(_PerObjShadowWtoPMatr, float4(positionWS, 1.0));
				o.positionCS.z *= _PerObjShadowWtoPMatr[3][3];

				o.screenUV = o.positionCS;
				o.vertCol = v.vertexCol;
				return o;
			}
			float4 frag (v2f i) : SV_Target
			{
				i.screenUV.xyz = i.screenUV.xyz / i.screenUV.w;
				i.screenUV.xy = i.screenUV.xy * 0.5 + float2(0.5, 0.5);
                #if defined(UNITY_UV_STARTS_AT_TOP)
                    i.screenUV.y = 1 - i.screenUV.y;
                #endif
				#if defined(UNITY_REVERSED_Z)
					i.screenUV.z = 1 - i.screenUV.z;
				#endif

				float2 finalCol = float2(0, 0);

				#if defined(_PEROBJSHADOW_SIGN)
					float depthU = 1 - i.screenUV.z;
					finalCol = float2(depthU, i.vertCol.x);
                #else
					float ID = _PerObjNumber + 0.0001;
					int row = floor(ID % _PerObjNumSqrt) + 1;
					int column = _PerObjNumSqrt - floor(ID / _PerObjNumSqrt);

                    float OneCSqrt = 1.0 / _PerObjNumSqrt;
                    if (i.screenUV.x < row * OneCSqrt && i.screenUV.x > (row - 1) * OneCSqrt && i.screenUV.y > (column - 1) * OneCSqrt && i.screenUV.y < column * OneCSqrt)
                    {
						float depthU = 1 - i.screenUV.z;
						finalCol = float2(depthU, i.vertCol.x);
                    }
					else
					{
						discard;
					}
                #endif

				return float4(0, finalCol.y, 0, finalCol.x);
			}
			ENDHLSL
		}
		Pass
		{
			ColorMask B
			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma shader_feature_local_fragment _ _PEROBJSHADOW_SIGN
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

			float _PerObjNumber;
			float _PerObjNumSqrt;

			float4x4 _PerObjShadowWtoPMatr;

			struct a2v
			{
				float4 positionOS : POSITION;
			};
			struct v2f
			{
				float4 positionCS : SV_POSITION;
				float3 screenUV : TEXCOORD0;
				float2 result : TEXCOORD1;
			};

			v2f vert (a2v v)
			{
				v2f o;
				float3 positionWS = TransformObjectToWorld(v.positionOS.xyz);
				o.positionCS = mul(_PerObjShadowWtoPMatr, float4(positionWS, 1.0));
				o.positionCS.z *= _PerObjShadowWtoPMatr[3][3];

				o.screenUV = o.positionCS.xyw;
				o.result.xy = TransformWorldToHClip(positionWS).zw;
				return o;
			}
			float4 frag (v2f i) : SV_Target
			{
				i.screenUV.xy = i.screenUV.xy / i.screenUV.z * 0.5 + float2(0.5, 0.5);
				i.result.x /= i.result.y;
                #if defined(UNITY_UV_STARTS_AT_TOP)
                    i.screenUV.y = 1 - i.screenUV.y;
                #endif
				#if defined(UNITY_REVERSED_Z)
					i.result.x = 1 - i.result.x;
				#endif

				float depth = 0;

				#if defined(_PEROBJSHADOW_SIGN)
					depth = i.result.x;
                #else
					float ID = _PerObjNumber + 0.0001;
					int row = floor(ID % _PerObjNumSqrt) + 1;
					int column = _PerObjNumSqrt - floor(ID / _PerObjNumSqrt);

                    float OneCSqrt = 1.0 / _PerObjNumSqrt;
                    if (i.screenUV.x < row * OneCSqrt && i.screenUV.x > (row - 1) * OneCSqrt && i.screenUV.y > (column - 1) * OneCSqrt && i.screenUV.y < column * OneCSqrt)
                    {
						depth = i.result.x;
                    }
					else
					{
						discard;
					}
                #endif

				return float4(0, 0, depth, 0);
			}
			ENDHLSL
		}
		Pass
		{
			ColorMask R
			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma shader_feature_local_fragment _ _PEROBJSHADOW_SIGN
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

			float _PerObjNumber;
			float _PerObjNumSqrt;

			float4x4 _PerObjShadowWtoPMatr;

			struct a2v
			{
				float4 positionOS : POSITION;
			};
			struct v2f
			{
				float4 positionCS : SV_POSITION;
				float3 screenUV : TEXCOORD0;
				float2 result : TEXCOORD1;
			};

			v2f vert (a2v v)
			{
				v2f o;
				float3 positionWS = TransformObjectToWorld(v.positionOS.xyz);
				o.positionCS = mul(_PerObjShadowWtoPMatr, float4(positionWS, 1.0));
				o.positionCS.z *= _PerObjShadowWtoPMatr[3][3];

				o.screenUV = o.positionCS.xyw;
				o.result.xy = TransformWorldToHClip(positionWS).zw;
				return o;
			}
			float4 frag (v2f i) : SV_Target
			{
				i.screenUV.xy = i.screenUV.xy / i.screenUV.z * 0.5 + float2(0.5, 0.5);
				i.result.x /= i.result.y;
                #if defined(UNITY_UV_STARTS_AT_TOP)
                    i.screenUV.y = 1 - i.screenUV.y;
                #endif
				#if defined(UNITY_REVERSED_Z)
					i.result.x = 1 - i.result.x;
				#endif

				float depth = 0;

				#if defined(_PEROBJSHADOW_SIGN)
					depth = 1 - i.result.x;
                #else
					float ID = _PerObjNumber + 0.0001;
					int row = floor(ID % _PerObjNumSqrt) + 1;
					int column = _PerObjNumSqrt - floor(ID / _PerObjNumSqrt);

                    float OneCSqrt = 1.0 / _PerObjNumSqrt;
                    if (i.screenUV.x < row * OneCSqrt && i.screenUV.x > (row - 1) * OneCSqrt && i.screenUV.y > (column - 1) * OneCSqrt && i.screenUV.y < column * OneCSqrt)
                    {
						depth = 1 - i.result.x;
                    }
					else
					{
						discard;
					}
                #endif

				return float4(depth, 0, 0, 0);
			}
			ENDHLSL
		}

		//刘海遮罩
		Pass
		{
			Cull Off
			ColorMask R
			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma shader_feature_local_fragment _ _PEROBJSHADOW_SIGN
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

			float _PerObjNumber;
			float _PerObjNumSqrt;

			float4x4 _PerObjShadowWtoPMatr_hair;

			struct a2v
			{
				float4 positionOS : POSITION;
			};
			struct v2f
			{
				float4 positionCS : SV_POSITION;
				float3 screenUV : TEXCOORD0;
				float2 result : TEXCOORD1;
			};

			v2f vert (a2v v)
			{
				v2f o;
				float3 positionWS = TransformObjectToWorld(v.positionOS.xyz);
				o.positionCS = mul(_PerObjShadowWtoPMatr_hair, float4(positionWS, 1.0));
				o.positionCS.z *= _PerObjShadowWtoPMatr_hair[3][3];

				o.screenUV = o.positionCS.xyw;

				o.result.xy = TransformWorldToHClip(positionWS).zw;
				return o;
			}
			float frag (v2f i) : SV_Target
			{
				i.screenUV.xy = i.screenUV.xy / i.screenUV.z * 0.5 + float2(0.5, 0.5);
				i.result.x /= i.result.y;
                #if defined(UNITY_UV_STARTS_AT_TOP)
                    i.screenUV.y = 1 - i.screenUV.y;
                #endif
				#if defined(UNITY_REVERSED_Z)
					i.result.x = 1 - i.result.x;
				#endif

				float mask = 0;

				#if defined(_PEROBJSHADOW_SIGN)
					mask = i.result.x;
                #else
					float ID = _PerObjNumber + 0.0001;
					int row = floor(ID % _PerObjNumSqrt) + 1;
					int column = _PerObjNumSqrt - floor(ID / _PerObjNumSqrt);

                    float OneCSqrt = 1.0 / _PerObjNumSqrt;
                    if (i.screenUV.x < row * OneCSqrt && i.screenUV.x > (row - 1) * OneCSqrt && i.screenUV.y > (column - 1) * OneCSqrt && i.screenUV.y < column * OneCSqrt)
                    {
						mask = i.result.x;
                    }
					else
					{
						discard;
					}
                #endif

				return mask;
			}
			ENDHLSL
		}
	}
}