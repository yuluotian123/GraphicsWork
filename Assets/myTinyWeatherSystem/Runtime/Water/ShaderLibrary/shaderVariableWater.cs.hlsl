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
CBUFFER_END

// Generated from Yu_Weather.shaderVariableWaterRendering
// PackingRules = Exact
GLOBAL_CBUFFER_START(shaderVariableWaterRendering, b4)
    float4 _BaseColor;
    float4 _ShallowColor;
    float _Transparency;
    float _ShallowDepth;
    float _Fade;
    float _Fresnel;
    float _Depth;
    float _Reflect;
    float _Refract;
    float _NormalPower;
    float _NormalBias;
    float _Shadow;
    float _Shininess;
    float _SSSscale;
CBUFFER_END


#endif
