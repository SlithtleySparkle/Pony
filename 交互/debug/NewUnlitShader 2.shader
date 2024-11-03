Shader "Unlit/NewUnlitShader 2"
{
    Properties
    {
        //_MainTexasdwa ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float4 col : TEXCOORD1;
            };

            sampler2D _MainTexasdwa;
            float4x4 _JiaoHuGrassShaderawdwaVPMatrix;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                float4 pos_screen = mul(mul(_JiaoHuGrassShaderawdwaVPMatrix, unity_ObjectToWorld), v.vertex);

                float4 uv = float4(pos_screen.xy / pos_screen.w, 0, 1);
                uv.xy = uv.xy * 0.5 + float2(0.5, 0.5);

                o.col = tex2Dlod(_MainTexasdwa, uv);

                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                return i.col;
            }
            ENDCG
        }
    }
}