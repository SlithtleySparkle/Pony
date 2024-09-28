Shader "Unlit/Bloom"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _SampleFanWei ("SampleFanWei", Float) = 1
        _RainbowChaJu ("RainbowChaJu", Float) = 5
    }
    SubShader
    {
        CGINCLUDE
        #include "UnityCG.cginc"

        sampler2D _MainTex;
        half4 _MainTex_TexelSize;
        sampler2D _Bloom;
        float _SampleFanWei;
        float _RainbowChaJu;

        //提取亮部
        half TiQuLiangBu(half3 color)
        {
            return 0.2125 * color.r + 0.7154 * color.g + 0.0721 * color.b;
        }
        struct v2fLiang
        {
            float4 pos : SV_POSITION;
            half2 uv : TEXCOORD0;
        };
        v2fLiang VertLiang(appdata_img v)
        {
            v2fLiang o;
            o.pos = UnityObjectToClipPos(v.vertex);
            o.uv = v.texcoord;
            return o;
        }
        half4 FragLiang(v2fLiang i) : SV_Target
        {
            half4 col = tex2D(_MainTex, i.uv);
            half liang = max(0, TiQuLiangBu(col.xyz) - 1.5);
            return col * liang;
            //return liang;
        }

        //提取彩虹
        half TiQuRainbow(half3 col)
        {
            half hong = step(abs((col.r + col.g + col.b) * 255 - 200 - 61 - 61) / 3, _RainbowChaJu);
            half cheng = step(abs((col.r + col.g + col.b) * 255 - 200 - 95 - 54) / 3, _RainbowChaJu);
            half huang = step(abs((col.r + col.g + col.b) * 255 - 200 - 200 - 136) / 3, _RainbowChaJu);
            half lv = step(abs((col.r + col.g + col.b) * 255 - 82 - 200 - 82) / 3, _RainbowChaJu);
            half lan = step(abs((col.r + col.g + col.b) * 255 - 37 - 122 - 200) / 3, _RainbowChaJu);
            half zi = step(abs((col.r + col.g + col.b) * 255 - 92 - 44 - 115) / 3, _RainbowChaJu);
            return saturate(hong + cheng + huang + lv + lan + zi);
        }
        struct v2fRainbow
        {
            float4 pos : SV_POSITION;
            half2 uv : TEXCOORD0;
        };
        v2fRainbow VertRainbow(appdata_img v)
        {
            v2fRainbow o;
            o.pos = UnityObjectToClipPos(v.vertex);
            o.uv = v.texcoord;
            return o;
        }
        half4 FragRainbow(v2fRainbow i) : SV_Target
        {
            half4 col = tex2D(_MainTex, i.uv);
            half rain = TiQuRainbow(col.xyz);
            half liang = max(0, TiQuLiangBu(col.xyz) - 1.5);
            return col * saturate(rain + liang);
            //return col * rain;
            //return liang;
        }

        //模糊
        struct v2fBlur
        {
            float4 pos : SV_POSITION;
            //half2 uv[7] : TEXCOORD0;

            half2 uv[13] : TEXCOORD0;
        };
        v2fBlur VertBlurV(appdata_img v)
        {
            // v2fBlur o;
            // o.pos = UnityObjectToClipPos(v.vertex);
            // o.uv[0] = v.texcoord;
            // o.uv[1] = v.texcoord + float2(0, _MainTex_TexelSize.y) * _SampleFanWei;
            // o.uv[2] = v.texcoord - float2(0, _MainTex_TexelSize.y) * _SampleFanWei;
            // o.uv[3] = v.texcoord + float2(0, _MainTex_TexelSize.y * 2) * _SampleFanWei;
            // o.uv[4] = v.texcoord - float2(0, _MainTex_TexelSize.y * 2) * _SampleFanWei;
            // o.uv[5] = v.texcoord + float2(0, _MainTex_TexelSize.y * 3) * _SampleFanWei;
            // o.uv[6] = v.texcoord - float2(0, _MainTex_TexelSize.y * 3) * _SampleFanWei;
            // return o;

            v2fBlur o;
            o.pos = UnityObjectToClipPos(v.vertex);
            o.uv[0] = v.texcoord;
            o.uv[1] = v.texcoord + float2(0, _MainTex_TexelSize.y) * _SampleFanWei / 2;
            o.uv[2] = v.texcoord - float2(0, _MainTex_TexelSize.y) * _SampleFanWei / 2;
            o.uv[3] = v.texcoord + float2(0, _MainTex_TexelSize.y * 2) * _SampleFanWei / 2;
            o.uv[4] = v.texcoord - float2(0, _MainTex_TexelSize.y * 2) * _SampleFanWei / 2;
            o.uv[5] = v.texcoord + float2(0, _MainTex_TexelSize.y * 3) * _SampleFanWei / 2;
            o.uv[6] = v.texcoord - float2(0, _MainTex_TexelSize.y * 3) * _SampleFanWei / 2;
            o.uv[7] = v.texcoord + float2(0, _MainTex_TexelSize.y * 4) * _SampleFanWei / 2;
            o.uv[8] = v.texcoord - float2(0, _MainTex_TexelSize.y * 4) * _SampleFanWei / 2;
            o.uv[9] = v.texcoord + float2(0, _MainTex_TexelSize.y * 5) * _SampleFanWei / 2;
            o.uv[10] = v.texcoord - float2(0, _MainTex_TexelSize.y * 5) * _SampleFanWei / 2;
            o.uv[11] = v.texcoord + float2(0, _MainTex_TexelSize.y * 6) * _SampleFanWei / 2;
            o.uv[12] = v.texcoord - float2(0, _MainTex_TexelSize.y * 6) * _SampleFanWei / 2;
            return o;
        }
        v2fBlur VertBlurU(appdata_img v)
        {
            // v2fBlur o;
            // o.pos = UnityObjectToClipPos(v.vertex);
            // o.uv[0] = v.texcoord;
            // o.uv[1] = v.texcoord + float2(_MainTex_TexelSize.x, 0) * _SampleFanWei;
            // o.uv[2] = v.texcoord - float2(_MainTex_TexelSize.x, 0) * _SampleFanWei;
            // o.uv[3] = v.texcoord + float2(_MainTex_TexelSize.x * 2, 0) * _SampleFanWei;
            // o.uv[4] = v.texcoord - float2(_MainTex_TexelSize.x * 2, 0) * _SampleFanWei;
            // o.uv[5] = v.texcoord + float2(_MainTex_TexelSize.x * 3, 0) * _SampleFanWei;
            // o.uv[6] = v.texcoord - float2(_MainTex_TexelSize.x * 3, 0) * _SampleFanWei;
            // return o;

            v2fBlur o;
            o.pos = UnityObjectToClipPos(v.vertex);
            o.uv[0] = v.texcoord;
            o.uv[1] = v.texcoord + float2(_MainTex_TexelSize.x, 0) * _SampleFanWei / 2;
            o.uv[2] = v.texcoord - float2(_MainTex_TexelSize.x, 0) * _SampleFanWei / 2;
            o.uv[3] = v.texcoord + float2(_MainTex_TexelSize.x * 2, 0) * _SampleFanWei / 2;
            o.uv[4] = v.texcoord - float2(_MainTex_TexelSize.x * 2, 0) * _SampleFanWei / 2;
            o.uv[5] = v.texcoord + float2(_MainTex_TexelSize.x * 3, 0) * _SampleFanWei / 2;
            o.uv[6] = v.texcoord - float2(_MainTex_TexelSize.x * 3, 0) * _SampleFanWei / 2;
            o.uv[7] = v.texcoord + float2(_MainTex_TexelSize.x * 4, 0) * _SampleFanWei / 2;
            o.uv[8] = v.texcoord - float2(_MainTex_TexelSize.x * 4, 0) * _SampleFanWei / 2;
            o.uv[9] = v.texcoord + float2(_MainTex_TexelSize.x * 5, 0) * _SampleFanWei / 2;
            o.uv[10] = v.texcoord - float2(_MainTex_TexelSize.x * 5, 0) * _SampleFanWei / 2;
            o.uv[11] = v.texcoord + float2(_MainTex_TexelSize.x * 6, 0) * _SampleFanWei / 2;
            o.uv[12] = v.texcoord - float2(_MainTex_TexelSize.x * 6, 0) * _SampleFanWei / 2;
            return o;
        }
        half4 FragBlur(v2fBlur i) : SV_Target
        {
            // //float GaoSiHe[3] = {0.6, 0.175, 0.15};
            // float GaoSiHe[3] = {0.425, 0.2, 0.075};
            // half3 finalCol = half3(0, 0, 0);
            // for(int it = 1; it < 4; it++)
            // {
            //     finalCol += tex2D(_MainTex, i.uv[it * 2 - 1]).rgb * GaoSiHe[it - 1];
            //     finalCol += tex2D(_MainTex, i.uv[it * 2]).rgb * GaoSiHe[it - 1];
            // }
            // return half4(saturate(finalCol), 1);

            //float GaoSiHe[3] = {0.6, 0.175, 0.15};
            float GaoSiHe[6] = {0.36, 0.15, 0.1, 0.05, 0.01, 0.005};
            half3 finalCol = half3(0, 0, 0);
            for(int it = 1; it < 7; it++)
            {
                finalCol += tex2D(_MainTex, i.uv[it * 2 - 1]).rgb * GaoSiHe[it - 1];
                finalCol += tex2D(_MainTex, i.uv[it * 2]).rgb * GaoSiHe[it - 1];
            }
            return half4(saturate(finalCol), 1);
            //return tex2D(_MainTex, i.uv[0]);
        }

        //Bloom
        struct v2fBloom
        {
            float4 pos : SV_POSITION;
            half4 uv : TEXCOORD0;
        };
        v2fBloom VertBloom(appdata_img v)
        {
            v2fBloom o;
            o.pos = UnityObjectToClipPos(v.vertex);
            o.uv.xy = v.texcoord;
            o.uv.zw = v.texcoord;

            #if UNITY_UV_STARTS_AT_TOP
            if (_MainTex_TexelSize.y < 0.0)
                o.uv.w = 1.0 - o.uv.w;
            #endif
            return o;
        }
        half4 FragBloom(v2fBloom i) : SV_Target
        {
            half4 col = tex2D(_MainTex, i.uv.xy);
            half4 bloomCol = tex2D(_Bloom, i.uv.zw);

            half qiangdu = max(1, max(max(col.r, col.g), col.b));

            half liang = 1 - step(0.1, saturate(TiQuLiangBu(col.xyz) - 1.5));
            col.rgb /= qiangdu;
            return half4(col.rgb + bloomCol.rgb * liang, col.a + bloomCol.a);
        }
        half4 FragBloomRain(v2fBloom i) : SV_Target
        {
            half4 col = tex2D(_MainTex, i.uv.xy);
            half4 bloomCol = tex2D(_Bloom, i.uv.zw);
            half qiangdu = max(1, max(max(col.r, col.g), col.b));
            half yuzhi = step(0.1, saturate(TiQuLiangBu(col.xyz)));
            half liang = (1 - yuzhi) * (1 - TiQuRainbow(col.xyz));
            col.rgb /= qiangdu;

            return half4(col.rgb + bloomCol.rgb * liang, col.a + bloomCol.a);
        }
        ENDCG
        //亮度
        Pass
        {
            CGPROGRAM
            #pragma vertex VertLiang
            #pragma fragment FragLiang
            ENDCG
        }
        //彩虹
        Pass
        {
            CGPROGRAM
            #pragma vertex VertRainbow
            #pragma fragment FragRainbow
            ENDCG
        }
        Pass
        {
            CGPROGRAM
            #pragma vertex VertBlurV
            #pragma fragment FragBlur
            ENDCG
        }
        Pass
        {
            CGPROGRAM
            #pragma vertex VertBlurU
            #pragma fragment FragBlur
            ENDCG
        }
        Pass
        {
            CGPROGRAM
            #pragma vertex VertBloom
            #pragma fragment FragBloom
            ENDCG
        }
        Pass
        {
            CGPROGRAM
            #pragma vertex VertBloom
            #pragma fragment FragBloomRain
            ENDCG
        }
    }
}