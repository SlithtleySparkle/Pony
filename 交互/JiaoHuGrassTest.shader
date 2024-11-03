Shader "Roystan/JiaoHuGrassTest"
{
    Properties
    {
        _TopColor ("Top Color", Color) = (1,1,1,1)
		_BottomColor ("Bottom Color", Color) = (1,1,1,1)
		_BendRotationRandom ("Bend Rotation Random", Range(0, 1)) = 0.2

		_BladeWidth ("Blade Width", Float) = 0.05
		_BladeWidthRandom ("Blade Width Random", Float) = 0.02
		_BladeHeight ("Blade Height", Float) = 0.5
		_BladeHeightRandom ("Blade Height Random", Float) = 0.3

		_TessellationUniform("Tessellation Uniform", Range(1, 64)) = 1

		_WindDistortionMap("Wind Distortion Map", 2D) = "white" {}
		_WindFrequency("Wind Frequency", Vector) = (0.05, 0.05, 0, 0)
		_WindStrength("Wind Strength", Float) = 1

		_BladeForward("Blade Forward Amount", Float) = 0.38
		_BladeCurve("Blade Curvature Amount", Range(1, 4)) = 2

		_JiaoHuQiangDu ("交互强度", Range(0.0, 50.0)) = 1
		_JiaoHuGrassHeightQiangDu ("交互高度强度", Range(0.0, 3.0)) = 1.5
    }
    SubShader
    {
		Tags{ "RenderType" = "Opaque" "LightMode" = "ForwardBase" }
        Pass
        {
			Cull Off
            CGPROGRAM

            #pragma vertex vert

			#pragma hull hull
			#pragma domain domain

			#pragma geometry geom
            #pragma fragment frag
			#pragma target 4.6
			#include "UnityCG.cginc"
			#include "Autolight.cginc"
			#include "Lighting.cginc"
			#include "CustomTessellationTest.cginc"

			fixed4 _TopColor;
			fixed4 _BottomColor;
			fixed _BendRotationRandom;

			float _BladeHeight;
			float _BladeHeightRandom;	
			float _BladeWidth;
			float _BladeWidthRandom;

			sampler2D _WindDistortionMap;
			float4 _WindDistortionMap_ST;
			float2 _WindFrequency;
			float _WindStrength;

			float _BladeForward;
			float _BladeCurve;

			//交互
			float _JiaoHuQiangDu;
			sampler2D _GrassJiaoHuTex;
			float4x4 _GrassVPMatrix;
			float _JiaoHuGrassHeightQiangDu;
			//交互

			float rand(float3 co)//噪声
			{
				return frac(sin(dot(co.xyz, float3(12.9898, 78.233, 53.539))) * 43758.5453);
			}
			float3x3 AngleAxis3x3(float angle, float3 axis)//旋转矩阵
			{
				float c, s;
				sincos(angle, s, c);

				float t = 1 - c;
				float x = axis.x;
				float y = axis.y;
				float z = axis.z;

				return float3x3(
					t * x * x + c, t * x * y - s * z, t * x * z + s * y,
					t * x * y + s * z, t * y * y + c, t * y * z - s * x,
					t * x * z - s * y, t * y * z + s * x, t * z * z + c
					);
			}

			struct a2v
			{
				float4 vertex : POSITION;
				float3 normal : NORMAL;
				float4 tangent : TANGENT;
				float2 texcoord : TEXCOORD0;
			};
			struct v2g
			{
				float4 posG : POSITION;
				float3 normalG : NORMAL;
				float4 tangentG : TANGENT;
				float2 uvG : TEXCOORD0;
			};
			struct g2f
			{
				float4 pos : SV_POSITION;
				float2 uv : TEXCOORD0;
			};

			void GenerateGrassVertex (float3 offset, float3 vertexPos, float width, float height, float forward, float2 uv, float3x3 transformMatrix, g2f g, inout TriangleStream<g2f> triStream)
            {
				float3 tangentPos = float3(width, forward, height);

				float4 vertexG = float4(vertexPos + mul(transformMatrix, tangentPos), 1);
				vertexG.xz += offset.xy * height * _JiaoHuQiangDu;
				vertexG.y = saturate(vertexG.y - offset.z * height * _JiaoHuGrassHeightQiangDu);
                g.pos = UnityObjectToClipPos(vertexG);
                g.uv = uv;
                //g.uv = offset.xy;
                triStream.Append(g);
            }

			v2g vert(a2v v)
			{
				v2g o;
				o.posG = v.vertex;
				o.normalG = v.normal;
				o.tangentG = v.tangent;
				o.uvG = v.texcoord;
				return o;
			}
			[maxvertexcount(7)]
			void geom(triangle v2g IN[3],inout TriangleStream<g2f> triStream)
			{
				g2f g;
				float3 pos = IN[0].posG;

				//
				float4 Guv_jiaohu = mul(mul(_GrassVPMatrix, unity_ObjectToWorld), IN[0].posG);//不能在顶点着色器里计算
				Guv_jiaohu.xy /= Guv_jiaohu.w;
				Guv_jiaohu.xy = Guv_jiaohu.xy * 0.5 + float2(0.5, 0.5);
				float4 offset = float4(Guv_jiaohu.xy, 0, 1);

				float4 colOffset = tex2Dlod(_GrassJiaoHuTex, offset);
				float3 finalOffset = float3((colOffset.rg * 2 - float2(1, 1)) * colOffset.a, colOffset.a);
				//

				float height = (rand(pos.zyx) * 2 - 1) * _BladeHeightRandom + _BladeHeight;
				float width = (rand(pos.xzy) * 2 - 1) * _BladeWidthRandom + _BladeWidth;
				float forward = rand(pos.yyz) * _BladeForward;

				float2 uv = pos.xz * _WindDistortionMap_ST.xy + _WindDistortionMap_ST.zw + _WindFrequency * _Time.y;
				float2 windSample = (tex2Dlod(_WindDistortionMap, float4(uv, 0, 0)).xy * 2 - 1) * _WindStrength;
				float3 wind = normalize(float3(windSample.x, windSample.y, 0));
				float3x3 windRotation = AngleAxis3x3(3.14 * windSample, wind);

				float3 normal = IN[0].normalG;
				float4 tangent = IN[0].tangentG;
				float3 binormal = cross(normal, tangent.xyz) * tangent.w;
				float3x3 TtoL = float3x3(
					tangent.x, binormal.x, normal.x,
					tangent.y, binormal.y, normal.y,
					tangent.z, binormal.z, normal.z);

				float3x3 facingRotation = AngleAxis3x3(rand(pos.xyz) * 6.28, float3(0, 0, 1));
				float3x3 bendRotation = AngleAxis3x3(rand(pos.zzx) * _BendRotationRandom * 3.14 * 0.5, float3(-1, 0, 0));
				float3x3 transformation = mul(mul(mul(TtoL, windRotation), facingRotation), bendRotation);

				float3x3 transformationFacing = mul(TtoL, facingRotation);

				for (int i = 0; i < 3; i++)
				{
					float t = i / 3;

					float segmentHeight = height * t;
					float segmentWidth = width * (1 - t);
					float segmentForward = pow(t, _BladeCurve) * forward;

					float3x3 transformMatrix = i == 0 ? transformationFacing : transformation;

					GenerateGrassVertex(finalOffset, pos.xyz, segmentWidth, segmentHeight, segmentForward, float2(0, t), transformMatrix, g, triStream);
					GenerateGrassVertex(finalOffset, pos.xyz, -segmentWidth, segmentHeight, segmentForward, float2(1, t), transformMatrix, g, triStream);
				}
				GenerateGrassVertex(finalOffset, pos.xyz, 0, height, forward, float2(0.5, 1), transformation, g, triStream);
				triStream.RestartStrip();
			}
			float4 frag(g2f i, fixed facing : VFACE) : SV_Target
            {
				return lerp(_BottomColor, _TopColor, i.uv.y);

				//float4 col = lerp(float4(i.uv, 0, 1), float4(0.5, 0, 0.5, 1), i.uv.x + 0.05);
				//return col;

				//return float4(i.uv, 0, 1);
            }
            ENDCG
        }
    }
}