Shader "Unlit/JiaoHuGrassPostShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
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

            sampler2D _MainTex;
            sampler2D _JiaoHuGrassLastRT;
            float _ShuaiJianSpeed;
            float4x4 _JiaoHuGrassPostShaderVPMatrix;
            float4 _JiaoHuGrasslastPos;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 texcoord : TEXCOORD0;
            };
            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 pos_Last : TEXCOORD1;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.texcoord;
                o.pos_Last = mul(mul(_JiaoHuGrassPostShaderVPMatrix, unity_ObjectToWorld), _JiaoHuGrasslastPos);
                return o;
            }
            half4 frag (v2f i) : SV_Target
            {
                float2 uv_offset = i.pos_Last.xy / i.pos_Last.w;
                uv_offset = uv_offset * 0.5 + 0.5 - float2(0.5, 0.5);

                half4 nowCol = tex2D(_MainTex, i.uv);
                half4 LastCol = tex2D(_JiaoHuGrassLastRT, i.uv - uv_offset);
                half4 col = max(nowCol, LastCol);

                return saturate(col - half4(_ShuaiJianSpeed, _ShuaiJianSpeed, 0, _ShuaiJianSpeed));
            }
            ENDCG
        }
    }
}