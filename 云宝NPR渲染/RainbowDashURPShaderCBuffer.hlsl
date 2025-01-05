CBUFFER_START(UnityPerMaterial)
    float4 _CameraDepthTexture_TexelSize;
    float4 _BaseColor;
    float _NormalScale;
    float _ShadowTint;

    //��Ե��
    float _RimWidth1;
    float _RimWidth2;
    float4 _RimColor;
    float _RimTint;
    float _RimDepthScale;

    //�۾��Է���
    float4 _EyeEmissionColor;

    //����͸��
    float _TouSheScale;
    float _TouSheQiangDu;
    //C#
    float4x4 _LightSpaceRainbowDashHouDuMatrix;
    float _MipmapLod_RainbowDashHouDu;

    //�沿SDF
    float4 _MianForwardDir;
    float4 _MianRightDir;

    //���
    float _OutlineWidth;
    float4 _OutlineColor;
CBUFFER_END

TEXTURE2D(_BaseMap);
SAMPLER(sampler_BaseMap);
TEXTURE2D(_BaseNormalMap);
SAMPLER(sampler_BaseNormalMap);
TEXTURE2D(_OtherMap);
SAMPLER(sampler_OtherMap);
TEXTURE2D(_RampMap);
SAMPLER(sampler_RampMap);
TEXTURE2D(_TouSheLUT);
SAMPLER(sampler_TouSheLUT);
TEXTURE2D(_RainbowDashTouSheTex);
SAMPLER(sampler_RainbowDashTouSheTex);