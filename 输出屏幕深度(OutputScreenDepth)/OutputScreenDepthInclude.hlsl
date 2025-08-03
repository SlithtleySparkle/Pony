#ifndef DELAUNAY_OUTPUT_SCREEN_DEPTH_INCLUDE_INCLUDED
#define DELAUNAY_OUTPUT_SCREEN_DEPTH_INCLUDE_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

struct a2v
{
	float4 positionOS : POSITION;
	float2 texcoord : TEXCOORD0;
};
struct v2f
{
	float4 positionCS : SV_POSITION;
	float3 positionVS : TEXCOORD0;
};

float frag(v2f i) : SV_Target
{
	//包含在Common.hlsl
	//解码用DecodeLogarithmicDepthGeneralized(float d, float4(1, 1, 0, 0))
#if defined(_SAME_AS_UNITY_RF)
	i.positionVS.x /= i.positionVS.y;
	#if defined(UNITY_REVERSED_Z)
		i.positionVS.x = 1 - i.positionVS.x;
	#endif
	//float depth = EncodeLogarithmicDepthGeneralized(1 - i.positionVS.x, float4(0, 1, 0, 0));
	float depth = 1 - i.positionVS.x;
#else
    //float depth = EncodeLogarithmicDepthGeneralized(-i.positionVS.z, float4(0, 1, 0, 0));
    float depth = -i.positionVS.z;
#endif
	return depth;
}

#endif