//
// This file was automatically generated. Please don't edit by hand. Execute Editor command [ Edit > Rendering > Generate Shader Includes ] instead
//

#ifndef SHADERVARIABLESKYATMOSPHERE_CS_HLSL
#define SHADERVARIABLESKYATMOSPHERE_CS_HLSL
//
// Yu_Weather.SkyConfig:  static fields
//
#define SKYCONFIG_TRANSMITTANCE_LUT_WIDTH (256)
#define SKYCONFIG_TRANSMITTANCE_LUT_HEIGHT (64)
#define SKYCONFIG_TRANSMITTANCE_LUTSAMPLE_COUNT (10)
#define SKYCONFIG_MULTI_SCATTERED_LUMINANCE_LUT_WIDTH (32)
#define SKYCONFIG_MULTI_SCATTERED_LUMINANCE_LUT_HEIGHT (32)
#define SKYCONFIG_MULTI_SCATTERING_LUTSAMPLE_COUNT (15)
#define SKYCONFIG_DISTANT_SKY_LIGHT_LUTALTITUDE (6)
#define SKYCONFIG_FAST_SKY_LUTWIDTH (192)
#define SKYCONFIG_FAST_SKY_LUTHEIGHT (104)
#define SKYCONFIG_SAMPLE_COUNT_MIN (2)
#define SKYCONFIG_SAMPLE_COUNT_MAX (32)
#define SKYCONFIG_DISTANCE_TO_SAMPLE_COUNT_MAX (150)
#define SKYCONFIG_FAST_SKY_LUTSAMPLE_COUNT_MIN (4)
#define SKYCONFIG_FAST_SKY_LUTSAMPLE_COUNT_MAX (32)
#define SKYCONFIG_FAST_SKY_LUTDISTANCE_TO_SAMPLE_COUNT_MAX (150)
#define SKYCONFIG_AERIAL_PERSPECTIVE_LUTDEPTH_RESOLUTION (16)
#define SKYCONFIG_AERIAL_PERSPECTIVE_LUTDEPTH (96)
#define SKYCONFIG_AERIAL_PERSPECTIVE_LUTSAMPLE_COUNT_MAX_PER_SLICE (2)
#define SKYCONFIG_AERIAL_PERSPECTIVE_LUTWIDTH (32)

// Generated from Yu_Weather.ShaderVariableSkyAtmosphere
// PackingRules = Exact
GLOBAL_CBUFFER_START(ShaderVariableSkyAtmosphere, b1)
    float _BottomRadiusKm;
    float _TopRadiusKm;
    float _MultiScatteringFactor;
    float _RayleighDensityExpScale;
    float4 _RayleighScattering;
    float4 _MieScattering;
    float4 _MieExtinction;
    float4 _MieAbsorption;
    float _MieDensityExpScale;
    float _MiePhaseG;
    float _AbsorptionDensity0LayerWidth;
    float _AbsorptionDensity0ConstantTerm;
    float _AbsorptionDensity0LinearTerm;
    float _AbsorptionDensity1ConstantTerm;
    float _AbsorptionDensity1LinearTerm;
    float _UnUsed;
    float4 _AbsorptionExtinction;
    float4 _GroundAlbedo;
    float4 _SkyLuminanceFactor;
CBUFFER_END


#endif
