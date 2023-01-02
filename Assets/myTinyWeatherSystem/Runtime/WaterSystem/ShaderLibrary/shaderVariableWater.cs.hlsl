//
// This file was automatically generated. Please don't edit by hand. Execute Editor command [ Edit > Rendering > Generate Shader Includes ] instead
//

#ifndef SHADERVARIABLEWATER_CS_HLSL
#define SHADERVARIABLEWATER_CS_HLSL
// Generated from Yu_Weather.shaderVariableWaterSurface
// PackingRules = Exact
GLOBAL_CBUFFER_START(shaderVariableWaterSurface, b3)
    float _MeshSize;
    float _MeshLength;
    float _FFTSize;
    float _Lambda;
    float _Amplitude;
    float _HeightScale;
    float _BubblesScale;
    float _BubblesThreshold;
    float4 _WindAndSeed;
    float _MTime;
    float _Tess;
    float _MinDist;
    float _MaxDist;
    float4 _MeshScale;
CBUFFER_END

// Generated from Yu_Weather.shaderVariableWaterRendering
// PackingRules = Exact
GLOBAL_CBUFFER_START(shaderVariableWaterRendering, b4)
    float _MaxDepth;
    float _HeightExtra;
    float _Fade;
    float _Fresnel;
    float _Reflect;
    float _Refract;
    float _NormalBias;
    float _NormalPower;
    float _SSSDistortion;
    float _SSSPow;
    float _SSSscale;
    float _SSSMaxWaveHeight;
    float4 _SSSColor;
    float4 _SpecColor;
    float _LightIntensityScale;
    float _Shininess;
    float _Shadow;
    float _UnUsed0;
    float _FoamEdge;
    float _FoamAdd;
    float _FoamScale;
    float _FoamRange;
    float4 _FoamColor;
    float4 _SeaColor;
CBUFFER_END


#endif
