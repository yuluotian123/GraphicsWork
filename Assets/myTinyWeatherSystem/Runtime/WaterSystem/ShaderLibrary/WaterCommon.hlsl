#ifndef YU_WATER_COMMON_INCLUDE
#define YU_WATER_COMMON_INCLUDE
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
#include "shaderVariableWater.cs.hlsl"
#define G 9.81f
#define MAX_TESSELLATION_FACTORS 32.0 
#define MAX_LIT_VALUE 48.0

TEXTURE2D_X_FLOAT(_WaterDepthTexture);   SAMPLER(sampler_WaterDepthTexture_linear_clamp);
TEXTURE2D(_AbsorptionScatteringRamp);    SAMPLER(sampler_AbsorptionScatteringRamp);
TEXTURE2D_X_FLOAT(_FoamMap);             SAMPLER(sampler_FoamMap_linear_repeat);
TEXTURE2D(_CausticsTexture);             SAMPLER(sampler_CausticsTexture);

float4 _WaterDepthParams;//xz:camPosition y:transformY(水面Height + HeightExtra)  w:camSize
float4x4 _WaterCameraWorldToViewMatrix;

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

//Mesh
//WaterSurface
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

//PostProcessingMesh
float3 getWorldPos(float depth, float3 ray)
{
    return (_WorldSpaceCameraPos + LinearEyeDepth(depth,_ZBufferParams) * ray.xyz);
}



//WaterRendering
//计算衰减
float Fade(float3 d)
{
	//on some gpus need float precision
    float _f = length(d) * _Fade;
    return saturate(1 / exp2(_f));
}

//计算WaterDepth
float WaterDepth(float3 posWS)
{
    float2 uv = (posWS.xz - _WaterDepthParams.xz) / (_WaterDepthParams.w);
    uv =  uv * .5f + .5f;
    
    float rawDepth;
    
    //对于超出UV范围的顶点直接认为Depth为最大值
    if (uv.x <=0.0f||uv.x>=1.0f||uv.y<=0.0f||uv.y>=1.0f)
        rawDepth = 0;
    else 
        rawDepth = SAMPLE_TEXTURE2D_LOD(_WaterDepthTexture, sampler_WaterDepthTexture_linear_clamp, uv, 0).r;
    
    float Depth = (1 - rawDepth) * (_HeightExtra + _MaxDepth) - _WaterDepthParams.y;
    return Depth + posWS.y;
}

//计算AmbientColor
float3 SHIndirectDiffuse(float3 nDirWS)			//INDIRECT LIGHTING
{
    float4 SHCoefficients[7];
    SHCoefficients[0] = unity_SHAr;
    SHCoefficients[1] = unity_SHAg;
    SHCoefficients[2] = unity_SHAb;
    SHCoefficients[3] = unity_SHBr;
    SHCoefficients[4] = unity_SHBg;
    SHCoefficients[5] = unity_SHBb;
    SHCoefficients[6] = unity_SHC;
    return max(0, float3(SampleSH9(SHCoefficients, nDirWS)));
}

//计算吸收和反射
half3 Scattering(half depth)
{
    return SAMPLE_TEXTURE2D(_AbsorptionScatteringRamp, sampler_AbsorptionScatteringRamp, half2(depth, 0.375h)).rgb;
}
half3 Absorption(half depth)
{
    return SAMPLE_TEXTURE2D(_AbsorptionScatteringRamp, sampler_AbsorptionScatteringRamp, half2(depth, 0.0h)).rgb;
}

//计算SSSColor
float4 SSSColor(float3 lightDir, float3 viewDir, float3 normal, float waveHeight, float SSSMask)
{
    float3 H = normalize(lightDir + normal*_SSSDistortion);
    float I = pow(saturate(dot(viewDir, -H)), _SSSPow) * _SSSscale * waveHeight * SSSMask;

    return _SSSColor * I;
}

//计算foamMap
float3 getFoamMap(float2 iuv)
{
    float2 uv = iuv*3 + _MTime.xx * 0.01h;
    return SAMPLE_TEXTURE2D(_FoamMap, sampler_FoamMap_linear_repeat, uv).rgb;

}

//distortUV
half2 DistortionUVs(half depth, float3 normalWS)
{
    half3 viewNormal = mul((float3x3) GetWorldToHClipMatrix(), -normalWS).xyz;

    return viewNormal.xz * saturate((depth) * 0.005);
}


//计算SpecTerm
float GGXTerm(float NdotH, float roughness)
{
    float a = roughness * roughness;
    float ta = a;
    a *= a;
	//on some gpus need float precision
    float d = NdotH * NdotH * (a - 1.f) + 1.f;
    return ta / max(PI * (d), 1e-7f);
}
float GGXSpecularDir(float3 V, float3 N, float3 Dir)
{
    float3 h = normalize(V - Dir);
    float nh = 1 - dot(N, h);

    return clamp(GGXTerm(nh, _Shininess) * saturate(dot(N, -Dir)), 0, MAX_LIT_VALUE);
}


#endif