#ifndef DELAUNAY_Magic_Metaball_URP_Inc_INCLUDED
#define DELAUNAY_Magic_Metaball_URP_Inc_INCLUDED

#define JIXIAODIAN 0.001

//https://iquilezles.org/articles/distfunctions/

//加法
float opSmoothUnion(float d1, float d2, float k)
{
    float h = clamp(0.5 + 0.5 * (d2 - d1) / k, 0, 1);
    return lerp(d2, d1, h) - k * h * (1 - h);
}
//减法
float opSmoothSubtraction(float d1, float d2, float k)
{
    float h = clamp(0.5 - 0.5 * (d2 + d1) / k, 0.0, 1.0);
    return lerp(d2, -d1, h) + k * h * (1.0 - h);
}
//交集
float opSmoothIntersection(float d1, float d2, float k)
{
    float h = clamp(0.5 - 0.5 * (d2 - d1) / k, 0.0, 1.0);
    return lerp(d2, d1, h) + k * h * (1.0 - h);
}

//球
float sdSphere(float3 p, float2 setting)
{
    setting /= 100;
    float scale = setting.x;
    float s = setting.y;
    s *= scale;
    return length(p) - s;
}
//上下半径不一致的胶囊体
float sdRoundCone(float3 p, float4 setting)
{
    setting /= 100;
    float scale = setting.x;
    float r1 = setting.y;
    float r2 = setting.z;
    float h = max(setting.w, 0.001);
    r1 *= scale;
    r2 *= scale;
    h *= scale;
    // sampling independent computations (only depend on shape)
    float b = (r1 - r2) / h;
    float a = sqrt(1 - b * b);
    // sampling dependant computations
    float2 q = float2(length(p.xz), p.y);
    float k = dot(q, float2(-b, a));
    
    //if (k < 0.0)
    //    return length(q) - r1;
    //if (k > a * h)
    //    return length(q - float2(0.0, h)) - r2;
    //return dot(q, float2(a, b)) - r1;
    float final1 = lerp(length(q - float2(0.0, h)) - r2, length(q) - r1, step(k, 0));
    float final = lerp(final1, dot(q, float2(a, b)) - r1, step(k, a * h) * step(0, k));
    return final;
}
//椭球
float sdEllipsoid(float3 p, float4 setting)
{
    setting /= 100;
    float scale = setting.x;
    float3 r = setting.yzw;
    r *= scale;
    float k0 = length(p / r);
    float k1 = length(p / (r * r));
    return k0 * (k0 - 1.0) / k1;
}
//上下半径不一致的胶囊体 与 椭球 的切换(1或0)
float sdRoundConeAndEllipsoid(float3 p, float4 RCSetting, float4 EPSetting, float lerp0to1)
{
    float RoundCone = sdRoundCone(p, RCSetting);
    float Ellipsoid = sdEllipsoid(p, EPSetting);
    return lerp0to1 == 1 ? RoundCone : Ellipsoid; //用lerp有问题;
}

//法线
float3 GetRoundConeNormal(float3 p, float4 setting)
{
    float2 offset = float2(JIXIAODIAN, 0);
    return normalize(float3(sdRoundCone(p + offset.xyy, setting) - sdRoundCone(p - offset.xyy, setting),
                            sdRoundCone(p + offset.yxy, setting) - sdRoundCone(p - offset.yxy, setting),
                            sdRoundCone(p + offset.yyx, setting) - sdRoundCone(p - offset.yyx, setting)));
}
float3 GetSphereNormal(float3 p, float2 setting)
{
    float2 offset = float2(JIXIAODIAN, 0);
    return normalize(float3(sdSphere(p + offset.xyy, setting) - sdSphere(p - offset.xyy, setting),
                            sdSphere(p + offset.yxy, setting) - sdSphere(p - offset.yxy, setting),
                            sdSphere(p + offset.yyx, setting) - sdSphere(p - offset.yyx, setting)));
}
float3 GetEllipsoidNormal(float3 p, float4 setting)
{
    float2 offset = float2(JIXIAODIAN, 0);
    return normalize(float3(sdEllipsoid(p + offset.xyy, setting) - sdEllipsoid(p - offset.xyy, setting),
                            sdEllipsoid(p + offset.yxy, setting) - sdEllipsoid(p - offset.yxy, setting),
                            sdEllipsoid(p + offset.yyx, setting) - sdEllipsoid(p - offset.yyx, setting)));
}
float3 GetRoundConeAndEllipsoidNormal(float3 p, float4 RCsetting, float4 EPsetting, float lerp0to1)
{
    float2 offset = float2(JIXIAODIAN, 0);
    return normalize(float3(sdRoundConeAndEllipsoid(p + offset.xyy, RCsetting, EPsetting, lerp0to1) - sdRoundConeAndEllipsoid(p - offset.xyy, RCsetting, EPsetting, lerp0to1),
                            sdRoundConeAndEllipsoid(p + offset.yxy, RCsetting, EPsetting, lerp0to1) - sdRoundConeAndEllipsoid(p - offset.yxy, RCsetting, EPsetting, lerp0to1),
                            sdRoundConeAndEllipsoid(p + offset.yyx, RCsetting, EPsetting, lerp0to1) - sdRoundConeAndEllipsoid(p - offset.yyx, RCsetting, EPsetting, lerp0to1)));
}

#endif