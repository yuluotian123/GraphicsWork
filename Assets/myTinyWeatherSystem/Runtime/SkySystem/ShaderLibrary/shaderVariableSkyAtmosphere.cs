using UnityEngine;
using UnityEngine.Rendering;

namespace Yu_Weather
{
    [GenerateHLSL]
    enum SkyConfig
    {
        TransmittanceLutWidth = 256,
        TransmittanceLutHeight = 64,
        TransmittanceLUTSampleCount = 10,

        MultiScatteredLuminanceLutWidth = 32,
        MultiScatteredLuminanceLutHeight = 32,
        MultiScatteringLUTSampleCount = 15,

        DistantSkyLightLUTAltitude = 6,

        FastSkyLUTWidth = 192,
        FastSkyLUTHeight = 104,
        SampleCountMin = 2,
        SampleCountMax = 32,
        DistanceToSampleCountMax = 150,//Km
        FastSkyLUTSampleCountMin = 4,
        FastSkyLUTSampleCountMax = 32,
        FastSkyLUTDistanceToSampleCountMax = 150,

        AerialPerspectiveLUTDepthResolution = 16,
        AerialPerspectiveLUTDepth = 96,
        AerialPerspectiveLUTSampleCountMaxPerSlice = 2,
        AerialPerspectiveLUTWidth = 32,

    }

    [GenerateHLSL(PackingRules.Exact,needAccessors = false, generateCBuffer = true,constantRegister = 1)]
    public unsafe struct ShaderVariableSkyAtmosphere
    {
        public float _BottomRadiusKm;
        public float _TopRadiusKm;           
        public float _MultiScatteringFactor;
        public float _RayleighDensityExpScale;

        public Vector4 _RayleighScattering;// Unit is 1/km
       
        public Vector4 _MieScattering;     // Unit is 1/km
        public Vector4 _MieExtinction;     // idem
        public Vector4 _MieAbsorption;     // idem

        public float _MieDensityExpScale;
        public float _MiePhaseG;
        public float _AbsorptionDensity0LayerWidth;
        public float _AbsorptionDensity0ConstantTerm;
        public float _AbsorptionDensity0LinearTerm;
        public float _AbsorptionDensity1ConstantTerm;
        public float _AbsorptionDensity1LinearTerm;
        public float _UnUsed;

        public Vector4 _AbsorptionExtinction;

        public Vector4 _GroundAlbedo;
        public Vector4 _SkyLuminanceFactor;

    }
}