#ifndef YU_WATER_COMMON_INCLUDE
#define YU_WATER_COMMON_INCLUDE
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "shaderVariableWater.cs.hlsl"
#define G 9.81f
#define MAX_TESSELLATION_FACTORS 32.0 
#define HeightExtra 4.0f

TEXTURE2D_X_FLOAT(_WaterDepthTexture);   SAMPLER(sampler_WaterDepthTexture_linear_clamp);
float4 _WaterDepthParams;//xyz:camPosition w:camSize

float Fade(float3 d)
{
	//on some gpus need float precision
    float _f = length(d) * _Fade;
    return saturate(1 / exp2(_f));
}
float FadeTess(float3 d)
{
	//on some gpus need float precision
    float l = length(d);
    return clamp(1.0 - (l - _MinDist) / (_MaxDist - _MinDist), 0.01, 1.0) * _Tess;
}
float LODFactor(float3 p0, float3 p1)
{
    float3 edgeCenter = (p0 + p1) * 0.5;
    float factor = FadeTess(edgeCenter - _WorldSpaceCameraPos);

    return min(factor, MAX_TESSELLATION_FACTORS);
}
//复数相乘
float2 complexMultiply(float2 c1, float2 c2)
{
    return float2(c1.x * c2.x - c1.y * c2.y,
    c1.x * c2.y + c1.y * c2.x);
}
//计算弥散
float dispersion(float2 k)
{
    return sqrt(G * length(k));
}
//计算phillips谱
float phillips(float2 k)
{
    float kLength = length(k);
    kLength = max(0.001f, kLength);
    // kLength = 1;
    float kLength2 = kLength * kLength;
    float kLength4 = kLength2 * kLength2;

    float windLength = length(_WindAndSeed.xy);
    float l = windLength * windLength / G;
    float l2 = l * l;

    float damping = 0.001f;
    float L2 = l2 * damping * damping;

    //phillips谱
    return _Amplitude * exp(-1.0f / (kLength2 * l2)) / kLength4 * exp(-kLength2 * L2);
}
//Donelan-Banner方向拓展
float DonelanBannerDirectionalSpreading(float2 k)
{
    float betaS;
    float omegap = 0.855f * G / length(_WindAndSeed.xy);
    float ratio = dispersion(k) / omegap;

    if (ratio < 0.95f)
    {
        betaS = 2.61f * pow(ratio, 1.3f);
    }
    if (ratio >= 0.95f && ratio < 1.6f)
    {
        betaS = 2.28f * pow(ratio, -1.3f);
    }
    if (ratio > 1.6f)
    {
        float epsilon = -0.4f + 0.8393f * exp(-0.567f * log(ratio * ratio));
        betaS = pow(10, epsilon);
    }
    float theta = atan2(k.y, k.x) - atan2(_WindAndSeed.y, _WindAndSeed.x);

    return betaS / max(1e-7f, 2.0f * tanh(betaS * PI) * pow(cosh(betaS * theta), 2));
}
//计算Depth
float WaterDepth(float3 posWS)
{
    float2 uv = (posWS.xz - _WaterDepthParams.xz) / (_WaterDepthParams.w);
    uv = uv * .5 + .5;
    float Depth = (1 - SAMPLE_TEXTURE2D_LOD(_WaterDepthTexture, sampler_WaterDepthTexture_linear_clamp, uv, 0).r) * (HeightExtra + _MaxDepth) - _WaterDepthParams.y;
    return Depth + posWS.y;
}

#endif