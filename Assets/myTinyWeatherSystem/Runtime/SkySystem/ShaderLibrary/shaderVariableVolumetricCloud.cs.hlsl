//
// This file was automatically generated. Please don't edit by hand. Execute Editor command [ Edit > Rendering > Generate Shader Includes ] instead
//

#ifndef SHADERVARIABLEVOLUMETRICCLOUD_CS_HLSL
#define SHADERVARIABLEVOLUMETRICCLOUD_CS_HLSL
// Generated from Yu_Weather.ShaderVariableVolumetricCloud
// PackingRules = Exact
GLOBAL_CBUFFER_START(ShaderVariableVolumetricCloud, b2)
    float _MaxRayMarchingDistance;
    float _HighestCloudAltitude;
    float _LowestCloudAltitude;
    float _EarthRadius;
    float2 _CloudRangeSquared;
    int _NumPrimarySteps;
    int _NumLightSteps;
    float4 _CloudMapTiling;
    float4 _ShapeNoiseOffset;
    float _ShapeFactor;
    float _ErosionFactor;
    float _ShapeScale;
    float _ErosionScale;
    float2 _WindDirection;
    float2 _WindVector;
CBUFFER_END


#endif
