Shader "Unlit/RenderMagicMetaballShader"
{
    Properties
    {
        //流动
        [NoScaleOffset] _MetaballRaoDongNoise ("魔法光晕噪声", 2D) = "white" {}
        _MetaballRDNoiScale ("噪声缩放（除法）", Float) = 1
        _MetaballRaoDongTint ("噪声强度", Range(-0.1, 0.1)) = 0
        _MetaballRDHeiTint ("噪声强度-高度", Range(0, 1)) = 0
        _MetaballFluSpeed ("流动速度", Float) = 1
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" }
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            // Cull Off
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
                //流动
                float _MetaballRDNoiScale;
                float _MetaballRaoDongTint;
                float _MetaballRDHeiTint;
                float _MetaballFluSpeed;
            CBUFFER_END
            UNITY_INSTANCING_BUFFER_START(RToMMBInstanceBuffer)
                // UNITY_DEFINE_INSTANCED_PROP(float4, _RenderToMBCol[9])
            UNITY_INSTANCING_BUFFER_END(RToMMBInstanceBuffer)

            float _RenderToMBSize[9];
            float4x4 _MBPerObjWtoPMatrix[9];
            float4 _RenderToMBRotOS[9];

            TEXTURE2D(_MetaballCmaeraTex);
            SAMPLER(sampler_MetaballCmaeraTex);
            TEXTURE2D(_MetaballRaoDongNoise);
            SAMPLER(sampler_MetaballRaoDongNoise);

            //Dither噪声
            //看看需不需要
            float3 noisePos3(float3 p)
            {
	            p = float3(dot(p, float3(73.1, 94.7, 21.7)),
			               dot(p, float3(57.5, 73.3, 57.1)),
			               dot(p, float3(35.5, 67.9, 82.6)));
	            return frac(sin(p) * 437.513);
            }
            float3x3 RotationAboutQuaternion(float4 q)
            {
                return float3x3(1 - 2 * (q.y * q.y + q.z * q.z), 2 * (q.x * q.y + q.w * q.z), 2 * (q.x * q.z - q.w * q.y),
                                2 * (q.x * q.y - q.w * q.z), 1 - 2 * (q.x * q.x + q.z * q.z), 2 * (q.y * q.z + q.w * q.x),
                                2 * (q.x * q.z + q.w * q.y), 2 * (q.y * q.z - q.w * q.x), 1 - 2 * (q.x * q.x + q.y * q.y));
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
                float3 screenUV_addCam : TEXCOORD0;
                float3 posRotOS : TEXCOORD3;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            v2f vert (a2v v, uint instanceID: SV_InstanceID)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);

                v.positionOS.xyz += _RenderToMBSize[instanceID] * v.normalOS;

                float3 viewPosOS = TransformWorldToObject(_WorldSpaceCameraPos.xyz);
                viewPosOS.z *= -1;
                float3 ForwardDirOS = viewPosOS - float3(0, 0, 0);
                ForwardDirOS.y = 0;
                ForwardDirOS = normalize(ForwardDirOS);
                float3 UpDirOS = float3(0, 1, 0);
                float3 RightDirOS = normalize(cross(UpDirOS, ForwardDirOS));
                UpDirOS = normalize(cross(ForwardDirOS, RightDirOS));
                float3 billboardPosOS = v.positionOS.x * RightDirOS + v.positionOS.y * UpDirOS + v.positionOS.z * ForwardDirOS;

                float3 positionWS = TransformObjectToWorld(v.positionOS.xyz);
                o.screenUV_addCam = mul(_MBPerObjWtoPMatrix[instanceID], float4(positionWS, 1)).xyw;

                //噪声上移方向，假设模型空间Y轴朝上
                float fluDir = frac(_Time.x * _MetaballFluSpeed);
                float3x3 RotMatrOS = RotationAboutQuaternion(_RenderToMBRotOS[instanceID]);
                o.posRotOS.xy = mul(RotMatrOS, billboardPosOS).xy / _MetaballRDNoiScale;
                o.posRotOS.z = fluDir;

                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                return o;
            }
            float4 frag(v2f i, uint instanceID: SV_InstanceID) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);

                float4 finalCol = float4(0, 0, 0, 0);
                float row = instanceID % 3 + 1;
                float column = 3 - floor(instanceID / 3);

                i.screenUV_addCam.xy = i.screenUV_addCam.xy / i.screenUV_addCam.z * 0.5 + float2(0.5, 0.5);
                #if defined(UNITY_UV_STARTS_AT_TOP)
                    i.screenUV_addCam.y = 1 - i.screenUV_addCam.y;
                #endif

                float xMin = (row - 1) * ONECTHREE;
                float xMax = row * ONECTHREE;

                if (i.screenUV_addCam.x + MINVAL < xMax && i.screenUV_addCam.x - MINVAL > xMin && i.screenUV_addCam.y - MINVAL > (column - 1) * ONECTHREE && i.screenUV_addCam.y + MINVAL < column * ONECTHREE)
                {
                    float fluNoise = 0;
                    float2 positionVS_N = frac(i.posRotOS.xy);

                    //随机，根据高度
                    float2 newUV = i.screenUV_addCam.xy;
                    fluNoise = (SAMPLE_TEXTURE2D(_MetaballRaoDongNoise, sampler_MetaballRaoDongNoise, float2(0.25, positionVS_N.y - i.posRotOS.z)).r - 0.5) * 2;
                    float NoiTint = 1 - smoothstep(_MetaballRDHeiTint, 1, abs(positionVS_N.y - 0.5) * 2);
                    fluNoise = fluNoise * _MetaballRaoDongTint * NoiTint + 1;
                    float xCen = (xMax - xMin) / 2 + xMin;
                    if (i.posRotOS.x > 0)
                    {
                        newUV.x = max(xCen, newUV.x * fluNoise);
                    }
                    else
                    {
                        newUV.x = min(xCen, newUV.x / fluNoise);
                    }

                    finalCol = SAMPLE_TEXTURE2D(_MetaballCmaeraTex, sampler_MetaballCmaeraTex, newUV);
                    // finalCol = SAMPLE_TEXTURE2D(_MetaballCmaeraTex, sampler_MetaballCmaeraTex, i.screenUV_addCam.xy);
                }
                return saturate(finalCol);

                // return UNITY_ACCESS_INSTANCED_PROP(RToMMBInstanceBuffer, _RenderToMBCol);
            }
            ENDHLSL
        }
    }
}