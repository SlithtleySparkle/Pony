Shader "Unlit/JiaoHuTexShader"
{
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            float4x4 _JiaoHuGrassShaderVPMatrix;
            float _JiaoHuGrassTexFanWei;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 texcoord : TEXCOORD0;
            };
            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 pos_screen : TEXCOORD1;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.texcoord;
                o.pos_screen = mul(mul(_JiaoHuGrassShaderVPMatrix, unity_ObjectToWorld), v.vertex);
                return o;
            }
            half4 frag (v2f i) : SV_Target
            {
                i.uv = (i.uv - float2(0.5, 0.5)) / _JiaoHuGrassTexFanWei;

                float2 uv = i.pos_screen.xy / i.pos_screen.w;
                uv = uv * 0.5 + 0.5;
                float juli = saturate(length(i.uv));

                float shuaijian = 1 - saturate(juli * 2);
                shuaijian *= shuaijian;

                juli = step(0.01, 1 - juli);
                uv *= juli;

                return half4(uv, 0, saturate(shuaijian));
            }
            ENDCG
        }
    }
}