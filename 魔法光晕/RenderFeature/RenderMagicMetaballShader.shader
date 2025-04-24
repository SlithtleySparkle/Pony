Shader "Unlit/RenderMagicMetaballShader"
{
    Properties
    {
        _RenderToMBZTSize ("整体大小", Float) = 25
        //流动
        _MetaballRaoDongNoise ("魔法光晕噪声", 2D) = "white" {}
        _MetaballRaoDongTint ("魔法光晕噪声强度", Range(0, 1)) = 0.5
        _MetaballFluSpeed ("魔法光晕流动速度", Float) = 1
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" }
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            Cull Off
            ZWrite Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            //多控制点的情况下，默认第一个是主颜色（采样获得颜色）
            // #pragma shader_feature_local_fragment _ _ISMULTICOLOR_ON
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            #define ONECTHREE 0.333333
            #define MINVAL 0.005

            CBUFFER_START(UnityPerMaterial)
                float _RenderToMBZTSize;
                //流动
                float _MetaballRaoDongTint;
                float _MetaballFluSpeed;
            CBUFFER_END
            UNITY_INSTANCING_BUFFER_START(RToMMBInstanceBuffer)
                // UNITY_DEFINE_INSTANCED_PROP(float4, _RenderToMBCol[9])
            UNITY_INSTANCING_BUFFER_END(RToMMBInstanceBuffer)

            float _RenderToMBSize[9];

            float4x4 _MBPerObjWtoPMatrix[9];
            TEXTURE2D(_MetaballCmaeraTex);
            SAMPLER(sampler_MetaballCmaeraTex);
            TEXTURE2D(_MetaballRaoDongNoise);
            SAMPLER(sampler_MetaballRaoDongNoise);

            //Dither噪声
            float3 noisePos3(float3 p)
            {
	            p = float3(dot(p, float3(73.1, 94.7, 21.7)),
			               dot(p, float3(57.5, 73.3, 57.1)),
			               dot(p, float3(35.5, 67.9, 82.6)));
	            return frac(sin(p) * 437.513);
            }
            float2 rayBox(float3 boundsMin, float3 boundsMax, float3 camPos, float3 rayDir)//x最近y经过
            {
                float3 invRayDir = 1 / rayDir;

                float3 CtoB1 = (boundsMin - camPos) * invRayDir;
                float3 CtoB2 = (boundsMax - camPos) * invRayDir;

                float3 maxJuli = max(CtoB1, CtoB2);
                float3 minJuli = min(CtoB1, CtoB2);

                float Julimin = max(minJuli.x, max(minJuli.y, minJuli.z));
                float Julimax = min(maxJuli.x, min(maxJuli.y, maxJuli.z));

                float CtoBMin = max(0, Julimin);
                float CrossJuli = max(0, Julimax - CtoBMin);
                return float2(CtoBMin, CrossJuli);
			}

            struct a2v
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            struct v2f
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 screenUV_addCam : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            v2f vert (a2v v, uint instanceID: SV_InstanceID)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);

                v.positionOS.xyz += _RenderToMBSize[instanceID] * v.normalOS;
                v.positionOS.xyz *= _RenderToMBZTSize;

                o.positionWS = TransformObjectToWorld(v.positionOS.xyz);

                o.screenUV_addCam = mul(_MBPerObjWtoPMatrix[instanceID], float4(o.positionWS, 1)).xyw;
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                return o;
            }
            float4 frag(v2f i, uint instanceID: SV_InstanceID) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);

                i.screenUV_addCam.xy = i.screenUV_addCam.xy / i.screenUV_addCam.z * 0.5 + float2(0.5, 0.5);
                #if defined(UNITY_UV_STARTS_AT_TOP)
                    i.screenUV_addCam.y = 1 - i.screenUV_addCam.y;
                #endif

                float4 finalCol = float4(0, 0, 0, 0);
                float row = instanceID % 3 + 1;
                float column = 3 - floor(instanceID / 3);

                if (i.screenUV_addCam.x + MINVAL < row * ONECTHREE && i.screenUV_addCam.x - MINVAL > (row - 1) * ONECTHREE && i.screenUV_addCam.y - MINVAL > (column - 1) * ONECTHREE && i.screenUV_addCam.y + MINVAL < column * ONECTHREE)
                {
                    // float fluNoise = (SAMPLE_TEXTURE2D(_MetaballRaoDongNoise, sampler_MetaballRaoDongNoise, i.screenUV_addCam.xy - float2(0, _Time.x * _MetaballFluSpeed)).r - 0.3) * _MetaballRaoDongTint;

                    // float2 newUV = i.screenUV_addCam;
                    // if (i.screenUV_addCam.x < row * ONECTHREE * 0.5)
                    // {
                    //     newUV.x -= fluNoise;
                    // }
                    // else
                    // {
                    //     newUV.x += fluNoise;
                    // }

                    // finalCol = SAMPLE_TEXTURE2D(_MetaballCmaeraTex, sampler_MetaballCmaeraTex, newUV);
                    finalCol = SAMPLE_TEXTURE2D(_MetaballCmaeraTex, sampler_MetaballCmaeraTex, i.screenUV_addCam);
                }
                return saturate(finalCol);

                // return UNITY_ACCESS_INSTANCED_PROP(RToMMBInstanceBuffer, _RenderToMBCol);
            }
            ENDHLSL
        }
    }
}