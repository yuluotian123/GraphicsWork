using UnityEngine;
using UnityEngine.Rendering;

namespace Yu_Weather
{
    [GenerateHLSL(PackingRules.Exact, needAccessors = false, generateCBuffer = true, constantRegister = 2)]
    public unsafe struct ShaderVariableVolumetricCloud
    {
        //Copy From Unity HDRP
        public float _MaxRayMarchingDistance;//in meters
        public float _HighestCloudAltitude;//in meters
        public float _LowestCloudAltitude;//in meters
        public float _EarthRadius;//should be the same as that in SkyAtmosphere but unit is meters

        // Stores (_HighestCloudAltitude + _EarthRadius)^2 and (_LowestCloudAltitude + _EarthRadius)^2
        // 说实话，我真的很不喜欢这种 但是。。。不存白不存
        public Vector2 _CloudRangeSquared;
        // Maximal primary steps that a ray can do
        public int _NumPrimarySteps;
        // Maximal number of light steps a ray can do
        public int _NumLightSteps;

        // Controls the tiling of the cloud map
        public Vector4 _CloudMapTiling;

        //3D x,y,z
        public Vector4 _ShapeNoiseOffset;

        // Controls the amount of low frenquency noise
        public float _ShapeFactor;
        // Controls the forward eccentricity of the clouds
        public float _ErosionFactor;
        // Multiplier to shape tiling
        public float _ShapeScale;
        // Multiplier to erosion tiling
        public float _ErosionScale;

        // Direction of the wind
        public Vector2 _WindDirection;
        // Displacement vector of the wind
        public Vector2 _WindVector;

    }
}
