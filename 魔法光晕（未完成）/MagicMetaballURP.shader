Shader "Unlit/MagicMetaballURP"
{
    Properties
    {
        [Header(SDF Properties)]
        [Space(10)]
        _SDF_StepNum ("步进次数", Float) = 100
        [Toggle] _isMagicMBHeBing ("内部元球是否开启融合", Float) = 0
        _SDF_RongHeTint ("内部元球融合强度", Range(0, 1)) = 0.5
        [Toggle] _isMultiColor ("是否开启多颜色", Float) = 0
        [Space(10)]

        [HDR] _BaseColor("颜色",Color) = (1,1,1,1)
        _AllAlpha ("整体透明度", Range(0, 1)) = 1
        _MagicRimLightTint ("魔法边缘光强度", Float) = 1
        _MagicRimLightWidth ("魔法边缘光宽度", Float) = 0
        //相互融合的元球之间，最好有相同的阈值
        _RongHeThreshold ("融合阈值", Float) = 1
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" }
        LOD 100
        Pass
        {
            // Tags { "LightMode" = "MagicMetaball" }
            Cull Off
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma shader_feature_local_fragment _ _ISMAGICMBHEBING_ON
            #pragma shader_feature_local_fragment _ _ISMULTICOLOR_ON
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "MagicMetaballURPInc.hlsl"

            CBUFFER_START(UnityPerMaterial)
                //SDF属性
                float _SDF_StepNum;
                float _SDF_RongHeTint;
                //SDF属性
                float4 _BaseColor;
                float _AllAlpha;
                float _MagicRimLightTint;
                float _MagicRimLightWidth;
                float _RongHeThreshold;
            CBUFFER_END
            //————————————————————————SDF属性设置
            //内部元球数量
            float _SDF_PerNum;
            //自身内部的元球的位置
            float4 _InsideMetaballPosWS[13];
            //自身内部的元球的旋转矩阵
            float4x4 _InsideMBRotMatrix[13];
            //胶囊体属性
            float4 _RoundConePro[13];
            //椭圆属性
            float4 _EllipsoidPro[13];
            //————————————————————————SDF属性设置
            //————————————————————————魔法光晕控制点属性
            float4 _ColorControlPosWS[2];
            float4 _ColorControlColor[2];
            float _ColorControlSmooth;
            //————————————————————————魔法光晕控制点属性
            // //可融合元球的最大数量
            // float4 _MetaballPositionWS[5];
            TEXTURE2D(_MetaballNormalWSTex);
            SAMPLER(sampler_MetaballNormalWSTex);

            struct a2v
            {
                float4 positionOS : POSITION;
            };
            struct v2f
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 positionOS : TEXCOORD1;
            };

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
            float GetRongHeSDF(float sdfNum_O, float3 p2, float4 RCP2, float4 EP2, float shape2)
            {
                float sdfNum_T = sdRoundConeAndEllipsoid(p2, RCP2, EP2, shape2);
                return opSmoothUnion(sdfNum_O, sdfNum_T, _SDF_RongHeTint);
            }
            void GetRHFinalMetaball(float3 centerPos, float3 sdfPos, float stepPerJuli, inout float sdfNum)
            {
                float4 POSandShape_O = _InsideMetaballPosWS[0];
                POSandShape_O.xyz -= centerPos;
                POSandShape_O.w = saturate(POSandShape_O.w);
                //旋转1
                float3x3 finRot_O = (float3x3)_InsideMBRotMatrix[0];
                //加上位移和旋转1
                float3 currentSDFPos_O = sdfPos - POSandShape_O.xyz;
                currentSDFPos_O = mul(finRot_O, currentSDFPos_O);
                sdfNum = sdRoundConeAndEllipsoid(currentSDFPos_O, _RoundConePro[0], _EllipsoidPro[0], POSandShape_O.w);

                [loop] for (int it = 1; it < _SDF_PerNum; it++)
                {
                    float4 POSandShape_T = _InsideMetaballPosWS[it];
                    POSandShape_T.xyz -= centerPos;
                    POSandShape_T.w = saturate(POSandShape_T.w);
                    //旋转2
                    float3x3 finRot_T = (float3x3)_InsideMBRotMatrix[it];
                    //加上位移和旋转2
                    float3 currentSDFPos_T = sdfPos - POSandShape_T.xyz;
                    currentSDFPos_T = mul(finRot_T, currentSDFPos_T);
                    //计算SDF
                    float sdfNum_Y = sdfNum;
                    sdfNum = GetRongHeSDF(sdfNum, currentSDFPos_T, _RoundConePro[it], _EllipsoidPro[it], POSandShape_T.w);
                }
            }
            float3 GetRongHeNor(float3 centerPos, float3 sdfPos, float stepPerJuli)
            {
                float2 offset = float2(JIXIAODIAN, 0);

                float sdfNum1 = 0;
                GetRHFinalMetaball(centerPos, sdfPos + offset.xyy, stepPerJuli, sdfNum1);
                float sdfNum2 = 0;
                GetRHFinalMetaball(centerPos, sdfPos - offset.xyy, stepPerJuli, sdfNum2);
                float sdfNum3 = 0;
                GetRHFinalMetaball(centerPos, sdfPos + offset.yxy, stepPerJuli, sdfNum3);
                float sdfNum4 = 0;
                GetRHFinalMetaball(centerPos, sdfPos - offset.yxy, stepPerJuli, sdfNum4);
                float sdfNum5 = 0;
                GetRHFinalMetaball(centerPos, sdfPos + offset.yyx, stepPerJuli, sdfNum5);
                float sdfNum6 = 0;
                GetRHFinalMetaball(centerPos, sdfPos - offset.yyx, stepPerJuli, sdfNum6);

                return normalize(float3(sdfNum1 - sdfNum2, sdfNum3 - sdfNum4, sdfNum5 - sdfNum6));
            }

            v2f vert(a2v v)
            {
                v2f o;
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.positionWS = TransformObjectToWorld(v.positionOS.xyz);
                o.positionOS = v.positionOS.xyz;
                return o;
            }
            float4 frag (v2f i) : SV_Target
            {
                float3 rayDir = normalize(i.positionWS - _WorldSpaceCameraPos.xyz);

                float3 centerPos = float3(unity_ObjectToWorld[0].w, unity_ObjectToWorld[1].w, unity_ObjectToWorld[2].w);
                float3 scale = float3(unity_ObjectToWorld[0].x, unity_ObjectToWorld[1].y, unity_ObjectToWorld[2].z);

                float3 boundsMin = centerPos - scale * 0.5;
                float3 boundsMax = centerPos + scale * 0.5;
                float2 rayToBox = rayBox(boundsMin, boundsMax, _WorldSpaceCameraPos.xyz, rayDir);
                if (rayToBox.x < 0) discard;

                float3 starPos = rayDir * rayToBox.x + _WorldSpaceCameraPos.xyz;
                float stepPerJuli = rayToBox.y / _SDF_StepNum;

                //形状遮罩
                float sdfSum = 0;
                float sdfNum = 0;
                //法线
                float3 sdfNormalWS = float3(0, 0, 0);

                [loop] for (float stepjuli = 0; stepjuli < _SDF_StepNum; stepjuli++)
                {
                    float3 stepjl = stepjuli * rayDir * stepPerJuli;
                    float3 currentPos = starPos + stepjl;
                    float3 sdfPos = currentPos - centerPos + noisePos3(i.positionWS) / 200;

                    #if defined(_ISMAGICMBHEBING_ON)
                        GetRHFinalMetaball(centerPos, sdfPos, stepPerJuli, sdfNum);
                        sdfNormalWS = GetRongHeNor(centerPos, sdfPos, stepPerJuli);
                        //放里面会导致输出背面的法线
                        if (sdfNum < 0.0001 && sdfNum > -stepPerJuli)
                        {
                            sdfSum = 1;
                            break;
                        }
                    #else
                        [loop] for (int it = 0; it < _SDF_PerNum; it++)
                        {
                            //位置
                            float4 POSandShape = _InsideMetaballPosWS[it];
                            POSandShape.xyz -= centerPos;
                            POSandShape.w = saturate(POSandShape.w);
                            //旋转
                            float3x3 finRot = (float3x3)_InsideMBRotMatrix[it];

                            float3 currentSDFPos = sdfPos - POSandShape.xyz;
                            currentSDFPos = mul(finRot, currentSDFPos);

                            sdfNum = sdRoundConeAndEllipsoid(currentSDFPos, _RoundConePro[it], _EllipsoidPro[it], POSandShape.w);
                            if (sdfNum < 0.0001)
                            {
                                if (sdfNum > -stepPerJuli)
                                {
                                    sdfNormalWS = GetRoundConeAndEllipsoidNormal(currentSDFPos, _RoundConePro[it], _EllipsoidPro[it], POSandShape.w);
                                    break;
                                }
                            }
                        }
                        if (sdfNum < 0.0001 && sdfNum > -stepPerJuli)
                        {
                            sdfSum = 1;
                            break;
                        }
                    #endif
                }

                float nDv = dot(-rayDir, sdfNormalWS);

                //边缘光
                //外层：噪声加上后，再乘个更高的数外扩
                float powNdV = saturate(1 - abs(nDv) + _MagicRimLightWidth);
                float rimLight = powNdV * _MagicRimLightTint;
                rimLight = lerp(0.25, 1, rimLight);

                #if defined(_ISMULTICOLOR_ON)
                    float colConSmoothT = _ColorControlSmooth * 2;

                    float3 conColOTDirWS = normalize(_ColorControlPosWS[0].xyz - _ColorControlPosWS[1].xyz);

                    float cdotDn1 = 1 - saturate(dot(conColOTDirWS, sdfNormalWS) + 1);
                    float mask_O = step(0.0001, cdotDn1);
                    cdotDn1 = lerp(_ColorControlSmooth, 1, saturate(cdotDn1 + colConSmoothT)) * mask_O;

                    float cdotDn2 = 1 - saturate(dot(-conColOTDirWS, sdfNormalWS) + 1);
                    float mask_T = step(0.0001, cdotDn2);
                    cdotDn2 = lerp(_ColorControlSmooth, 1, saturate(cdotDn2 + colConSmoothT)) * mask_T;

                    float cdotDn = (cdotDn1 - cdotDn2) * 0.5 + 0.5;
                    float3 finalConColor = lerp(_ColorControlColor[1].rgb, _ColorControlColor[0].rgb, cdotDn);

                    return float4(_BaseColor.rgb * rimLight * finalConColor, _AllAlpha * saturate(rimLight) * sdfSum * max(cdotDn1, cdotDn2));
                #endif

                return float4(_BaseColor.rgb * rimLight, _AllAlpha * saturate(rimLight) * sdfSum);
            }
            ENDHLSL
        }
    }
}