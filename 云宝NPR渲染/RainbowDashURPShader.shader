Shader "RainbowDash/RainbowDashURPShader"
{
    Properties
    {
        [KeywordEnum(Front, Back, Off)] _Cut ("渲染正面或背面", Float) = 0
        [KeywordEnum(Ti, Mian, Fa ,Mei)] _BuWei ("部位", Float) = 0

        // [Header(Stencil)]
        // [IntRange] _StencilRef ("Ref", Range(0, 255)) = 0
        // [Enum(UnityEngine.Rendering.CompareFunction)] _StencilComp ("Comp", Float) = 0
        // [Enum(UnityEngine.Rendering.StencilOp)] _StencilPass ("Pass", Float) = 0

        [Space(10)]
        [Header(Blend)]
        [Enum(UnityEngine.Rendering.BlendMode)] _BlendSrc ("源", Float) = 1
        [Enum(UnityEngine.Rendering.BlendMode)] _BlendDst ("目标", Float) = 0
        [Enum(UnityEngine.Rendering.BlendOp)] _Blendop ("方式", Float) = 0
        [Enum(UnityEngine.Rendering.CompareFunction)] _ZTest ("深度测试", Float) = 4
        [Enum(Off, 0, On, 1)] _Zwrite ("深度写入", Float) = 1

        [Space(10)]
        [Header(RimLight)]
        [Toggle(_RimLight)] _BianYuanGuang ("是否开启边缘光", Float) = 0
        _RimWidth1 ("边缘光屏幕UV偏移", Range(0, 10)) = 1
        _RimWidth2 ("边缘光宽度", Range(-1, 1)) = 0.75
        _RimColor ("边缘光颜色", Color) = (1, 1, 1, 1)
        _RimTint ("边缘光强度", Float) = 1
        _RimDepthScale ("边缘光深度差缩放", Float) = 10

        [Space(10)]
        [Header(EyeEmission)]
        [Toggle(_EyeEmission)] _EyeEmission ("是否开启眼睛自发光", Float) = 0
        [HDR] _EyeEmissionColor ("自发光颜色", Color) = (1, 1, 1, 1)

        [Space(10)]
        [Header(ErDuoTouShe)]
        [Toggle(_ErDuoTouShe)] _ErDuoTouShe ("是否开启耳朵透射", Float) = 0
        _TouSheLUT ("透射LUT", 2D) = "white" {}
        _TouSheScale ("厚度缩放", Range(0.0, 5.0)) = 1.0
        _TouSheQiangDu ("透射强度", Range(0, 10)) = 1

        [Space(10)]
        [Header(Outline)]
        _OutlineWidth ("描边宽度", Range(0, 10)) = 3
        _OutlineColor ("描边颜色(*)", Color) = (1, 1, 1, 1)

        [Space(10)]
        [Header(Other)]
        [MainTexture][NoScaleOffset] _BaseMap ("主贴图", 2D) = "white" {}
        [NoScaleOffset] _BaseNormalMap ("法线贴图", 2D) = "bump" {}
        _NormalScale ("法线强度", Range(0, 10)) = 1
        [NoScaleOffset] _OtherMap ("R：头发高光G：眼睛自发光B：耳朵透射A：面部阴影", 2D) = "white" {}
        [NoScaleOffset] _RampMap ("渐变图", 2D) = "white" {}
        [MainColor] _BaseColor ("主贴图颜色(*)", Color) = (1,1,1,1)
        _ShadowTint ("阴影深浅", Range(0, 1)) = 0

        [HideInInspector] _MianForwardDir ("_MianForwardDir", Color) = (1, 1, 1, 1)
        [HideInInspector] _MianRightDir ("_MianRightDir", Color) = (1, 1, 1, 1)
    }
    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
        }
        LOD 100

        Pass
        {
            Tags{ "LightMode" = "RainbowDashURP" }
            Cull [_Cut]
            ZTest [_ZTest]
            ZWrite [_Zwrite]
            Blend [_BlendSrc] [_BlendDst]
            BlendOp [_Blendop]
            // Stencil
            // {
            //     Ref [_StencilRef]
            //     Comp [_StencilComp]
            //     Pass [_StencilPass]
            // }

            HLSLPROGRAM
            #pragma vertex MainVert
            #pragma fragment MainFrag
            #pragma shader_feature _RimLight
            #pragma shader_feature _EyeEmission
            #pragma shader_feature _ErDuoTouShe
            #pragma multi_compile _BUWEI_TI _BUWEI_MIAN _BUWEI_FA

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/SpaceTransforms.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            #include "RainbowDashURPShaderCBuffer.hlsl"


            struct a2v
            {
                float4 positionOS : POSITION;
                float4 tangentOS : TANGENT;
                float3 normalOS : NORMAL;
                float2 texcoord : TEXCOORD0;
            };
            struct v2f
            {
                float4 positionCS : SV_POSITION;
                float3 positionVS : TEXCOORD0;
                float4 positionLS : TEXCOORD1;
                float2 uv : TEXCOORD2;
                float3 srceenUV : TEXCOORD3;
                float3 lightDirVS : TEXCOORD4;
                float4 TtoW0 : TEXCOORD5;
                float4 TtoW1 : TEXCOORD6;
                float4 TtoW2 : TEXCOORD7;
            };

            v2f MainVert (a2v v)
            {
                v2f o;
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                float4 posWS = mul(unity_ObjectToWorld, v.positionOS);
                o.positionVS = TransformWorldToView(posWS.xyz);
                o.positionLS = mul(_LightSpaceRainbowDashHouDuMatrix, posWS);
                o.uv = v.texcoord;

                o.srceenUV.xy = o.positionCS.xy * 0.5 + float2(0.5, 0.5) * o.positionCS.w;
                o.srceenUV.z = o.positionCS.w;

                o.lightDirVS = TransformWorldToViewDir(_MainLightPosition.xyz, true);

                float3 normalWS = TransformObjectToWorldNormal(v.normalOS, true);
                float3 tangentWS = TransformObjectToWorldDir(v.tangentOS.xyz, true);
                float3 binormalWS = cross(normalWS, tangentWS) * v.tangentOS.w;

                o.TtoW0 = float4(tangentWS.x, binormalWS.x, normalWS.x, posWS.x);
                o.TtoW1 = float4(tangentWS.y, binormalWS.y, normalWS.y, posWS.y);
                o.TtoW2 = float4(tangentWS.z, binormalWS.z, normalWS.z, posWS.z);

                return o;
            }
            float4 MainFrag (v2f i) : SV_Target
            {
                float4 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv) * _BaseColor;
                float4 otherMapCol = SAMPLE_TEXTURE2D(_OtherMap, sampler_OtherMap, i.uv);
                float HairSpecular = otherMapCol.r;
                float eyeEmission = otherMapCol.g;
                float erduoTousheMask = otherMapCol.b;
                float MainSDF = otherMapCol.a;

                float3 normalTS = UnpackNormalScale(SAMPLE_TEXTURE2D(_BaseNormalMap, sampler_BaseNormalMap, i.uv), _NormalScale);
                float3 normalWS = float3(dot(i.TtoW0.xyz, normalTS), dot(i.TtoW1.xyz, normalTS), dot(i.TtoW2.xyz, normalTS));
                float3 positionWS = float3(i.TtoW0.w, i.TtoW1.w, i.TtoW2.w);
                float3 lightDirWS = _MainLightPosition.xyz;
                float3 viewDirWS = normalize(_WorldSpaceCameraPos.xyz - positionWS);

                float ndl = dot(lightDirWS, normalWS);
                float ndv = dot(viewDirWS, normalWS);
                float vdl2 = dot(float3(0, 0, 1), normalize(i.lightDirVS));

                float rampShadow = saturate(SAMPLE_TEXTURE2D(_RampMap, sampler_RampMap, float2(ndl * 0.5 + 0.5, 0.5)).r + _ShadowTint);

                //多光源
                float lightCountIndex = GetAdditionalLightsCount();
                float3 addLightCol = float3(1, 1, 1);
                for(int LCIi = 0; LCIi < lightCountIndex; LCIi++)
                {
                    Light AddLight = GetAdditionalLight(LCIi, positionWS, half4(1, 1, 1, 1));
                    float ndlAdd = dot(AddLight.direction, normalWS) * 0.5 + 0.5;
                    float rampShadowAdd = SAMPLE_TEXTURE2D(_RampMap, sampler_RampMap, float2(ndlAdd, 0.5)).r;
                    addLightCol += AddLight.color * AddLight.distanceAttenuation * rampShadowAdd;
                }

                //边缘光   耳朵透射                                   边缘光(深度判断和光源有关)
                float3 RimLightCol = float3(0, 0, 0);
                #if _RimLight || _ErDuoTouShe
                    float2 srceenuv = i.srceenUV.xy / i.srceenUV.z;
                    #ifdef UNITY_UV_STARTS_AT_TOP
                        srceenuv.y = 1 - srceenuv.y;
                    #endif
                    float Modeldepth = LinearEyeDepth(-i.positionVS.z, _ZBufferParams);

                    #if _RimLight
                        float2 normalVS = TransformWorldToViewNormal(normalWS, true).xy;
                        srceenuv += normalVS * _RimWidth1 / 100;
                        float Texdepth = Linear01Depth(SampleSceneDepth(srceenuv), _ZBufferParams);

                        RimLightCol = saturate(saturate(Texdepth - Modeldepth) / _RimDepthScale * _RimColor.rgb * _RimTint * saturate(0.5 - abs(ndl) + _RimWidth2) * saturate(1 - ndv + _RimWidth2) * smoothstep(0.75, 1, -vdl2));
                    #endif
                    #if _ErDuoTouShe
                        i.positionLS.xyz = i.positionLS.xyz / i.positionLS.w * 0.5 + float3(0.5, 0.5, 0.5);
                        #ifdef UNITY_REVERSED_Z
                            i.positionLS.z = 1 - i.positionLS.z;
                        #endif
                        float TousheHouduTex = dot(_RainbowDashTouSheTex.SampleLevel(sampler_RainbowDashTouSheTex, i.positionLS.xy, _MipmapLod_RainbowDashHouDu), float4(1.0, 1/255.0, 1/65025.0, 1/16581375.0));
                        float houdu = saturate(saturate(i.positionLS.z - TousheHouduTex - 0.0025) * _TouSheScale + 0.2);
                        float3 tousheLUTCol = SAMPLE_TEXTURE2D(_TouSheLUT, sampler_TouSheLUT, float2(houdu, 0.5)).rgb * erduoTousheMask;
                        float tousheE = saturate(dot(-normalWS, lightDirWS));//+0.3
                        float3 TouSheCol = saturate(tousheLUTCol * tousheE * _TouSheQiangDu);
                        return saturate(albedo * rampShadow + float4(RimLightCol * (1 - erduoTousheMask) + TouSheCol, 0));
                    #endif
                #endif

                float4 finalColor = saturate(albedo * rampShadow + float4(RimLightCol, 0));

                #if _BUWEI_MIAN
                    float MianRdL = dot(_MianRightDir.xyz, lightDirWS);
                    MainSDF = MianRdL > 0 ? MainSDF : SAMPLE_TEXTURE2D(_OtherMap, sampler_OtherMap, float2(1 - i.uv.x, i.uv.y)).a;

                    float MianFdL = dot(_MianForwardDir.xyz, -lightDirWS) * 0.5 + 0.5;
                    float MianSDFShadow = MainSDF > MianFdL ? 1 : 0.5 + _ShadowTint;
                    return saturate(albedo * MianSDFShadow + float4(RimLightCol, 0));
                #elif _BUWEI_FA
                    rampShadow *= rampShadow;
                    float hairSpec = clamp(HairSpecular, 0, 0.4) * smoothstep(0.5, 1, ndv) * saturate(ndl);
                    return saturate(albedo + float4(hairSpec, hairSpec, hairSpec, 0) + float4(RimLightCol, 0));
                #endif
                #if _EyeEmission//时间相关
                    float3 YanJinEmission = eyeEmission * _EyeEmissionColor.rgb * _EyeEmissionColor.a;
                    return albedo + float4(YanJinEmission, 0);
                #endif
                return finalColor;
            }
            ENDHLSL
        }
        //描边
        Pass
        {
            Tags{ "LightMode" = "Outline" }
            Cull Front
            Stencil
            {
                Ref [_StencilRef]
                Comp [_StencilComp]
                Pass [_StencilPass]
            }
            HLSLPROGRAM
            #pragma vertex OutlineVert
            #pragma fragment OutlineFrag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/SpaceTransforms.hlsl"
            #include "RainbowDashURPShaderCBuffer.hlsl"

            struct a2v
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 texcoord : TEXCOORD0;
            };
            struct v2f
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            v2f OutlineVert (a2v v)
            {
                v2f o;
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                float2 normalCS = TransformWorldToHClipDir(TransformObjectToWorldNormal(v.normalOS, true), true).xy;
                float2 OutlineOffset = normalCS * _OutlineWidth * o.positionCS.w / 1000;
                o.positionCS.xy += OutlineOffset;
                o.uv = v.texcoord;
                return o;
            }
            float4 OutlineFrag (v2f i) : SV_Target
            {
                return SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv) * _BaseColor * _OutlineColor;
            }
	        ENDHLSL
        }
        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
    }
}