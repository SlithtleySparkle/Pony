Shader "Pony/TuoWei"
{
    Properties
    {
        [IntRange] _WangGeXiFeng ("网格细分程度", Range(0, 64)) = 1
        [Header(WenLi And Color)]
        [Space(10)]
        _MainTex ("拖尾纹理", 2D) = "white" {}
        _LiZiTex ("粒子纹理", 2D) = "white" {}
        [HDR]_TuoWeiCol ("拖尾颜色", Color) = (1, 1, 1, 1)
        [HDR]_LiZiCol ("粒子颜色", Color) = (1, 1, 1, 1)//a通道没用
        [Space(20)]
        [Header(LiZiControl)]
        [Space(10)]
        _ZhongZi ("种子", Float) = 13
        _MiDu ("粒子数量", Range(0.0, 5.0)) = 3
        _DaXiao ("粒子大小", Float) = 0.2
        _PianYiChengDu ("粒子位置随机偏移程度", Vector) = (0, 0, 0, 0)
        _CenterPosPianYi ("粒子位置偏移", Float) = 1
        _LiZiAlpha ("粒子整体透明度", Range(0.0, 100.0)) = 1
        _LiZiAlphaLiSan ("粒子透明度随机数", Float) = 0.5
        _XYZpos ("粒子的XYZ轴偏移量", Vector) = (0, 0, 0, 0)
        _LiZiYunDong ("粒子运动速度", Float) = 1
        [Space(20)]
        _ChuShiTouMingDu ("初始透明度", Range(0.0, 1.0)) = 0.85
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue" = "Transparent" "IgnoreProjector" = "Ture" "ForceNoShadowCasting" = "True" }
        LOD 100
        // Pass
        // {
        //     Cull Off
        //     ZWrite On
        //     ColorMask 0
        // }
        Pass
        {
            Cull Off
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha
            CGPROGRAM

            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;
            half4 _TuoWeiCol;
            half _ChengDu;
            half _ChangDu;
            half _ChuShiTouMingDu;

            struct a2v
            {
                float4 vertex : POSITION;
                float2 texcoord : TEXCOORD0;
            };
            struct v2f
            {
                float4 pos : POSITION;
                float2 uv : TEXCOORD0;
            };

            v2f vert(a2v v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);
                return o;
            }
            half4 frag(v2f i) : SV_Target
            {
                half4 col = tex2D(_MainTex, i.uv);
                col.rgb *= _TuoWeiCol.xyz;

                // half bloy = 1 + step(0.001, abs(i.uv.y - 0.5) - 0.496) * 10;
                // half blox = step(0.001, 1 - saturate(i.uv.x) - 0.99) * 10;
                // half blo = blox + bloy;

                float life;
                if (i.uv.x <= _ChengDu)
                {
                    life = i.uv.x - i.uv.x / _ChangDu * (_ChengDu - 10 - i.uv.x);
                    life = saturate(life / i.uv.x);
                }
                half finalLife = saturate((life - 1 + _ChuShiTouMingDu) * col.a);
                //return half4(col.rgb * blo, finalLife);
                return half4(col.rgb, finalLife);
            }
            ENDCG
        }
        Pass
        {
            ZWrite Off
            Blend SrcAlpha One
            CGPROGRAM

            #pragma vertex vert
            #pragma hull hull
			#pragma domain domain
            #pragma geometry geom
            #pragma fragment frag
            #pragma target 4.6
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;
            half _ChengDu;
            half _ChangDu;
            half _ChuShiTouMingDu;

            sampler2D _LiZiTex;
            half4 _LiZiCol;
            float _ZhongZi;
            half _MiDu;
            float _DaXiao;
            float4 _PianYiChengDu;
            float _CenterPosPianYi;
            half _LiZiAlpha;
            half _LiZiAlphaLiSan;
            float4 _XYZpos;
            float _LiZiYunDong;

            half _WangGeXiFeng;

            struct a2v
            {
                float4 vertex : POSITION;
                float2 texcoord : TEXCOORD0;
            };
            struct v2h
            {
                float4 posH : POSITION;
                float2 uvH : TEXCOORD0;
            };
            struct d2g
            {
                float4 posD :POSITION;
                float2 uvD : TEXCOORD0;
            };
            struct g2f
            {
                float4 pos : SV_POSITION;
                float4 uvG : TEXCOORD0;
                half2 color : TEXCOORD1;
            };
            struct TF
            {
                float edge[3] : SV_TessFactor;
                float inside : SV_InsideTessFactor;
            };

            half Random (float2 pos)
            {
                half ccp = frac(pos.y / _ZhongZi) * 4.617;
                half zxo = frac(pos.x / 4.617) * _ZhongZi;
                half cz = sin(ccp) + cos(zxo);
                half ti = frac((sin(_Time.x / 5000 * _LiZiYunDong) + 1) / 2);
                half dong = frac(cz + ti) * 3.14;
                return sin(dong);
            }
            half Noise (float2 pos)
            {
                float2 i = floor(pos);
                half2 f = frac(pos);

                half XZ = Random(i);
                half XY = Random(i + half2(1, 0));
                half SZ = Random(i + half2(0, 1));
                half SY = Random(i + half2(1, 1));

                half2 u = f * f * (3 - 2 * f);
                half X = lerp(XZ, XY, u.x);
                half S = lerp(SZ, SY, u.x);

                return lerp(X, S, u.y);
            }
            half TorF (float2 pos)
            {
                half a = Random(pos);
                a = a * 2 - 1;
                return a;
            }
            void addDian (float3 CenPos, half3 rDir, half3 uDir, float SJSDX, half4 XYZW, float2 uvy, g2f g, inout TriangleStream<g2f> triStream)
            {
                g.pos = UnityObjectToClipPos(float4(CenPos + rDir * SJSDX * XYZW.x + uDir * SJSDX * XYZW.y, 1));
                g.uvG = float4(XYZW.xy, uvy);
                g.color = XYZW.zw;
                triStream.Append(g);
            }

            v2h vert(a2v v)
            {
                v2h h;
                h.posH = v.vertex;
                h.uvH = TRANSFORM_TEX(v.texcoord, _MainTex);
                return h;
            }
            TF patchCF(InputPatch<a2v, 3> patch, uint patchID : SV_PrimitiveID)
            {
                TF f;
                f.edge[0] = 1;
                f.edge[1] = 1;
                f.edge[2] = _WangGeXiFeng;
                f.inside = _WangGeXiFeng;
                return f;
            }

            [domain("tri")]
			[outputcontrolpoints(3)]
			[outputtopology("triangle_cw")]
			[partitioning("integer")]
			[patchconstantfunc("patchCF")]
			[maxtessfactor(64.0f)]
            v2h hull(InputPatch<a2v, 3> patch, uint Hid : SV_OutputControlPointID, uint patchHID : SV_PrimitiveID)
            {
                v2h output;
                output.posH = patch[Hid].vertex;
                output.uvH = patch[Hid].texcoord;
                return output;
            }
            [domain("tri")]
            d2g domain(TF factors, float3 bary : SV_DomainLocation, const OutputPatch<v2h, 3> patch)
            {
                d2g d;
                d.posD = patch[0].posH * bary.x + patch[1].posH * bary.y + patch[2].posH * bary.z;
                d.uvD = patch[0].uvH * bary.x + patch[1].uvH * bary.y + patch[2].uvH * bary.z;
                return d;
            }

            [maxvertexcount(30)]//10的倍数，_MiDu * 10
            void geom(triangle d2g IN[3], inout TriangleStream<g2f> triStream)
            {
                g2f g;

                half SuiJiShuSL = Noise(IN[0].uvD * 3);
                SuiJiShuSL = clamp(floor(SuiJiShuSL * _MiDu), 0, 5);

                float3 CenterPos = (IN[0].posD + IN[1].posD + IN[2].posD).xyz / 3;
                CenterPos *= _CenterPosPianYi;
                CenterPos += _XYZpos.xyz;
                g.uvG.zw = (IN[0].uvD + IN[1].uvD + IN[2].uvD) / 3;

                for(int itt = 0; itt <= SuiJiShuSL; itt++)
                {
                    half n = itt % 3;
                    half SuiJiShuDX = Random(IN[n].uvD) * itt * 3 / 10;
                    half SuiJiShuA = Random(IN[n].uvD * _LiZiAlphaLiSan) * _LiZiAlpha;

                    float2 COS1 = float2(SuiJiShuDX * 165, SuiJiShuA * 461);
                    float2 COS2 = float2(SuiJiShuA * 165, SuiJiShuDX * 461);
                    float2 COS3 = float2(SuiJiShuDX * 461, SuiJiShuA * 165);
                    float3 offset = float3(TorF(COS1) * _PianYiChengDu.x, TorF(COS2) * _PianYiChengDu.y, TorF(COS3) * _PianYiChengDu.z);

                    CenterPos += offset;
                    g.uvG.zw += offset.xy;

                    float3 viewPos = mul(unity_WorldToObject, float4(_WorldSpaceCameraPos, 1));
                    half3 forwardDir = normalize(CenterPos - viewPos);
                    half3 upDir = half3(0, 1, 0);
                    half3 rightDir = normalize(cross(upDir, forwardDir));
                    upDir = normalize(cross(forwardDir, rightDir));


                    half blo = 1 - step(2, n);

                    half4 Xyzw11 = half4(0, 1, SuiJiShuA, blo);
                    half4 Xyzw12 = half4(1, 1, SuiJiShuA, blo);
                    half4 Xyzw13 = half4(0, 0, SuiJiShuA, blo);
                    half4 Xyzw14 = half4(1, 0, SuiJiShuA, blo);

                    addDian(CenterPos, rightDir, upDir, SuiJiShuDX * _DaXiao, Xyzw11, g.uvG.zw, g, triStream);
                    addDian(CenterPos, rightDir, upDir, SuiJiShuDX * _DaXiao, Xyzw12, g.uvG.zw, g, triStream);
                    addDian(CenterPos, rightDir, upDir, SuiJiShuDX * _DaXiao, Xyzw13, g.uvG.zw, g, triStream);
                    addDian(CenterPos, rightDir, upDir, SuiJiShuDX * _DaXiao, Xyzw14, g.uvG.zw, g, triStream);
                    triStream.RestartStrip();
                }
            }
            half4 frag (g2f i) : SV_Target
            {
                half4 col = half4(1, 1, 1, 1);
                col.a = tex2D(_LiZiTex, i.uvG.xy).a * i.color.x;
                col.rgb *= tex2D(_MainTex, i.uvG.zw).rgb * (i.color.y * 2 + 1.25);

                float life;
                if (i.uvG.z <= _ChengDu)
                {
                    life = i.uvG.z - i.uvG.z / _ChangDu * (_ChengDu - 10 - i.uvG.z);
                    life = saturate(life / i.uvG.z);
                }
                half finalLife = saturate((life - 1 + _ChuShiTouMingDu) * col.a);

                return half4(col.rgb * _LiZiCol.xyz, finalLife);
            }
            ENDCG
        }
    }
    Fallback Off
}