#ifndef YU_SKYATMOSPHERE_COMMON_INCLUDE
#define YU_SKYATMOSPHERE_COMMON_INCLUDE
#include "TranslationWorld.hlsl"
#include "shaderVariableAtmosphereLights.hlsl"
#include "shaderVariableSkyAtmosphere.cs.hlsl"
//基本都是照搬的UE5的实现，只是减少了一些个人觉得用不到的功能

//如果是TransmittanceLut等就不需要处理View相关的内容
#define VIEWDATA_AVAILABLE (!TRANSMITTANCE_PASS&& !MULTISCATTERING_PASS&& !SKYLIGHT_PASS)

//Unity的单位为m
#define M_TO_SKY_UNIT 0.001f
#define SKY_UNIT_TO_M (1.0f/M_TO_SKY_UNIT)

#define PLANET_RADIUS_OFFSET 0.001f

#define NearDepthValue (UNITY_REVERSED_Z ? 1.0f : 0.0f)
#define FarDepthValue  (UNITY_REVERSED_Z ? 0.0f : 1.0f)

#define M_PI 3.1415926535897932f

TEXTURE2D_X_FLOAT(_SkyViewTexture);
TEXTURE3D_FLOAT(_AerialPerspectiveTexture);
TEXTURE2D_X_FLOAT(_TransmittanceTexture);
TEXTURE2D_X_FLOAT(_MultiScatteredLuminanceTexture);
SAMPLER(sampler_linear_clamp);


float2 FromUnitToSubUvs(float2 uv, float2 Size)
{
    return (uv + 0.5f * float2(1 / Size.x, 1 / Size.y)) * (Size.xy / (Size.xy + 1.0f));
}
float2 FromSubUvsToUnit(float2 uv, float2 Size)
{
    return (uv - 0.5f * float2(1/Size.x,1/Size.y)) * (Size.xy / (Size.xy - 1.0f));
}

//SVPositon: X:[0...ScreenWidth] Y:[0...ScreenHeight] Z:[nearVal, farVal]（usually near=0，far=1 ReverseZ 1,0）W:ViewSpace Z
float4 GetScreenTranslatedWorldPos(float4 SVPos, float DeviceZ)
{
    return float4(SVPositionToTranslatedWorld(float4(SVPos.xy, DeviceZ, 1.0f)), 1.0f);
}
float3 GetScreenWorldDir(in float4 SVPosition)
{
    return ScreenToWorldDir(SVPosition);
}

float3x3 GetSkyViewLutReferential()
{
    return (float3x3) _SkyViewLutReferential;
}
float3 GetCameraTranslatedWorldPos()
{
    return (float3)_SkyCameraTranslatedWorldOrigin;
}
float3 GetTranslatedCameraPlanetPos()
{
    return (GetCameraTranslatedWorldPos() - _SkyPlanetTranslatedWorldCenterAndViewHeight.xyz) * M_TO_SKY_UNIT;
}

#define DEFAULT_SAMPLE_OFFSET 0.3f
TEXTURE2D(_BlueNoise);
SAMPLER(sampler_BlueNoise);
float SkyAtmosphereNoise(float2 UV)
{
#if defined(VIEWDATA_AVAILABLE) && defined(PER_PIXEL_NOISE)
    return SAMPLE_TEXTURE2D(_BlueNoise, sampler_BlueNoise,UV).r;
#else
    return DEFAULT_SAMPLE_OFFSET;
#endif
}
/**
 * Returns near intersection in x, far intersection in y, or both -1 if no intersection.
 * RayDirection does not need to be unit length.
 */
float2 RayIntersectSphere(float3 RayOrigin, float3 RayDirection, float4 Sphere)
{
    float3 LocalPosition = RayOrigin - Sphere.xyz;
    float LocalPositionSqr = dot(LocalPosition, LocalPosition);

    float3 QuadraticCoef;
    QuadraticCoef.x = dot(RayDirection, RayDirection);
    QuadraticCoef.y = 2 * dot(RayDirection, LocalPosition);
    QuadraticCoef.z = LocalPositionSqr - Sphere.w * Sphere.w;

    float Discriminant = QuadraticCoef.y * QuadraticCoef.y - 4 * QuadraticCoef.x * QuadraticCoef.z;

    float2 Intersections = -1;

	// Only continue if the ray intersects the sphere
    [flatten]
    if (Discriminant >= 0)
    {
        float SqrtDiscriminant = sqrt(Discriminant);
        Intersections = (-QuadraticCoef.y + float2(-1, 1) * SqrtDiscriminant) / (2 * QuadraticCoef.x);
    }

    return Intersections;
}
// - RayOrigin: ray origin
// - RayDir: normalized ray direction
// - SphereCenter: sphere center
// - SphereRadius: sphere radius
// - Returns distance from RayOrigin to closest intersecion with sphere,
//   or -1.0 if no intersection.
float RaySphereIntersectNearest(float3 RayOrigin, float3 RayDir, float3 SphereCenter, float SphereRadius)
{
    float2 Sol = RayIntersectSphere(RayOrigin, RayDir, float4(SphereCenter, SphereRadius));
    float Sol0 = Sol.x;
    float Sol1 = Sol.y;
    if (Sol0 < 0.0f && Sol1 < 0.0f)
    {
        return -1.0f;
    }
    if (Sol0 < 0.0f)
    {
        return max(0.0f, Sol1);
    }
    else if (Sol1 < 0.0f)
    {
        return max(0.0f, Sol0);
    }
    return max(0.0f, min(Sol0, Sol1));
}

// uv in [0,1]
// ViewZenithCosAngle in [-1,1]
// ViewHeight in [bottomRAdius, topRadius]
void UVToTransmittanceParams(out float ViewHeight, out float ViewZenithCosAngle, in float BottomRadiusKm, in float TopRadiusKm,in
float2 UV)
{
    float Xmu = UV.x;
    float Xr = UV.y;

    float H = sqrt(TopRadiusKm * TopRadiusKm - BottomRadiusKm * BottomRadiusKm);
    float Rho = H * Xr;
    ViewHeight = sqrt(Rho * Rho + BottomRadiusKm * BottomRadiusKm);

    float Dmin = TopRadiusKm - ViewHeight;
    float Dmax = Rho + H;
    float D = Dmin + Xmu * (Dmax - Dmin);
    ViewZenithCosAngle = D == 0.0f ? 1.0f : (H * H - Rho * Rho - D * D) / (2.0f * ViewHeight * D);
    ViewZenithCosAngle = clamp(ViewZenithCosAngle, -1.0f, 1.0f);
}
void LutTransmittanceParamsToUv(in float viewHeight, in float viewZenithCosAngle, in float BottomRadiusKm,in float TopRadiusKm,out
float2 UV)
{
    float H = sqrt(max(0.0f, TopRadiusKm * TopRadiusKm - BottomRadiusKm * BottomRadiusKm));
    float Rho = sqrt(max(0.0f, viewHeight * viewHeight - BottomRadiusKm * BottomRadiusKm));

    float Discriminant = viewHeight * viewHeight * (viewZenithCosAngle * viewZenithCosAngle - 1.0f) + TopRadiusKm * TopRadiusKm;
    float D = max(0.0f, (-viewHeight * viewZenithCosAngle + sqrt(Discriminant))); 

    float Dmin = TopRadiusKm - viewHeight;
    float Dmax = Rho + H;
    float Xmu = (D - Dmin) / (Dmax - Dmin);
    float Xr = Rho / H;

    UV = float2(Xmu, Xr);
}

// uv in [0,1]
void UvToSkyViewLutParams(out float3 ViewDir, in float ViewHeight, in float2 UV)
{
	// Constrain uvs to valid sub texel range (avoid zenith derivative issue making LUT usage visible)
    UV = FromSubUvsToUnit(UV, float2(SKYCONFIG_FAST_SKY_LUTWIDTH, SKYCONFIG_FAST_SKY_LUTHEIGHT));

    float Vhorizon = sqrt(ViewHeight * ViewHeight - _BottomRadiusKm * _BottomRadiusKm);
    float CosBeta = Vhorizon / ViewHeight; // cos of zenith angle from horizon to zeniht
    float Beta = acos(CosBeta);
    float ZenithHorizonAngle = M_PI - Beta;
    
    float ViewZenithAngle;
    if (UV.y < 0.5f)
    {
        float Coord = 2.0f * UV.y;
        Coord = 1.0f - Coord;
        Coord *= Coord;
        Coord = 1.0f - Coord;
        ViewZenithAngle = ZenithHorizonAngle * Coord;
    }
    else
    {
        float Coord = UV.y * 2.0f - 1.0f;
        Coord *= Coord;
        ViewZenithAngle = ZenithHorizonAngle + Beta * Coord;
    }
    float CosViewZenithAngle = cos(ViewZenithAngle);
    float SinViewZenithAngle = sqrt(1.0 - CosViewZenithAngle * CosViewZenithAngle) * (ViewZenithAngle > 0.0f ? 1.0f : -1.0f); // Equivalent to sin(ViewZenithAngle)

    float LongitudeViewCosAngle = UV.x * 2.0f * M_PI;

	// Make sure those values are in range as it could disrupt other math done later such as sqrt(1.0-c*c)
    float CosLongitudeViewCosAngle = cos(LongitudeViewCosAngle);
    float SinLongitudeViewCosAngle = sqrt(1.0 - CosLongitudeViewCosAngle * CosLongitudeViewCosAngle) * (LongitudeViewCosAngle <= M_PI ? 1.0f : -1.0f); // Equivalent to sin(LongitudeViewCosAngle)
    ViewDir = float3(
		SinViewZenithAngle * CosLongitudeViewCosAngle,
		CosViewZenithAngle,
		-SinViewZenithAngle * SinLongitudeViewCosAngle
		);
}
void SkyViewLutParamsToUv(
	in bool IntersectGround, in float ViewZenithCosAngle, in float3 ViewDir, in float ViewHeight, in float BottomRadius, in float2 SkyViewLutSize,
	out float2 UV)
{
    float Vhorizon = sqrt(ViewHeight * ViewHeight - BottomRadius * BottomRadius);
    float CosBeta = Vhorizon / ViewHeight; // GroundToHorizonCos
    float Beta = acos(CosBeta);
    float ZenithHorizonAngle = M_PI - Beta;
    float ViewZenithAngle = acos(ViewZenithCosAngle);

    if (!IntersectGround)
    {
        float Coord = ViewZenithAngle / ZenithHorizonAngle;
        Coord = 1.0f - Coord;
        Coord = sqrt(Coord);
        Coord = 1.0f - Coord;
        UV.y = Coord * 0.5f;
    }
    else
    {
        float Coord = (ViewZenithAngle - ZenithHorizonAngle) / Beta;
        Coord = sqrt(Coord);
        UV.y = Coord * 0.5f + 0.5f;
    }

	{
        UV.x = (atan2(ViewDir.z, -ViewDir.x) + M_PI) / (2.0f * M_PI);
    }

	// Constrain uvs to valid sub texel range (avoid zenith derivative issue making LUT usage visible)
    UV = FromUnitToSubUvs(UV, SkyViewLutSize.xy);
}

bool MoveToTopAtmosphere(inout float3 WorldPos, in float3 WorldDir, in float AtmosphereTopRadius)
{
    float ViewHeight = length(WorldPos);
    if (ViewHeight > AtmosphereTopRadius)
    {
        float TTop = RaySphereIntersectNearest(WorldPos, WorldDir, float3(0.0f, 0.0f, 0.0f), AtmosphereTopRadius);
        if (TTop >= 0.0f)
        {
            float3 UpVector = WorldPos / ViewHeight;
            float3 UpOffset = UpVector * -PLANET_RADIUS_OFFSET;
            WorldPos = WorldPos + WorldDir * TTop + UpOffset;
        }
        else
        {
			// Ray is not intersecting the atmosphere
            return false;
        }
    }
    return true; // ok to start tracing
}

float3 GetTransmittance(in float LightZenithCosAngle, in float PHeight)
{
    float2 UV;
    LutTransmittanceParamsToUv(PHeight, LightZenithCosAngle, _BottomRadiusKm,_TopRadiusKm,UV);
#ifdef TRANSMITTANCE_PASS
	float3 TransmittanceToLight = float3(1.0f,1.0f,1.0f); 
#else
    float3 TransmittanceToLight = SAMPLE_TEXTURE2D_X_LOD(_TransmittanceTexture, sampler_linear_clamp, UV, 0).rgb;
#endif
    return TransmittanceToLight;
}
float3 GetAtmosphereTransmittance(float3 PlanetCenterToWorldPos, float3 WorldDir, float BottomRadius, float TopRadius)
{
	// For each view height entry, transmittance is only stored from zenith to horizon. Earth shadow is not accounted for.
	// It does not contain earth shadow in order to avoid texel linear interpolation artefact when LUT is low resolution.
	// As such, at the most shadowed point of the LUT when close to horizon, pure black with earth shadow is never hit.
	// That is why we analytically compute the virtual planet shadow here.
    const float2 Sol = RayIntersectSphere(PlanetCenterToWorldPos, WorldDir, float4(float3(0.0f, 0.0f, 0.0f), BottomRadius));
    if (Sol.x > 0.0f || Sol.y > 0.0f)
    {
        return 0.0f;
    }

    const float PHeight = length(PlanetCenterToWorldPos);
    const float3 UpVector = PlanetCenterToWorldPos / PHeight;
    const float LightZenithCosAngle = dot(WorldDir, UpVector);
    float2 TransmittanceLutUv;
    LutTransmittanceParamsToUv(PHeight, LightZenithCosAngle, BottomRadius, TopRadius, TransmittanceLutUv);
    const float3 TransmittanceToLight = SAMPLE_TEXTURE2D_X_LOD(_TransmittanceTexture, sampler_linear_clamp, TransmittanceLutUv, 0).rgb;
    return TransmittanceToLight;
}

float3 GetMultipleScattering(float3 WorlPos, float ViewZenithCosAngle)
{
    float2 UV = saturate(float2(ViewZenithCosAngle * 0.5f + 0.5f, (length(WorlPos) - _BottomRadiusKm) / (_TopRadiusKm - _BottomRadiusKm)));
	// We do no apply UV transform to sub range here as it has minimal impact.
    float3 MultiScatteredLuminance = SAMPLE_TEXTURE2D_LOD(_MultiScatteredLuminanceTexture, sampler_linear_clamp, UV, 0).rgb;
    return MultiScatteredLuminance;
}

float4 GetAerialPerspectiveLuminanceTransmittance(float2 CameraAerialPerspectiveVolumeSize,float2 ScreenUV, float3 SampledWorldPos, float3 CameraWorldPos,
float AerialPerspectiveVolumeDepthResolution, float AerialPerspectiveVolumeStartDepth, float AerialPerspectiveVolumeDepthSliceLengthKm)
{ 
    float tDepth = max(0.0f, length(SampledWorldPos - CameraWorldPos) - AerialPerspectiveVolumeStartDepth);
    
    float LinearSlice = tDepth / AerialPerspectiveVolumeDepthSliceLengthKm;
    float LinearW = LinearSlice / AerialPerspectiveVolumeDepthResolution; // Depth slice coordinate in [0,1]
    float NonLinW = sqrt(LinearW); // Squared distribution
    float NonLinSlice = NonLinW * AerialPerspectiveVolumeDepthResolution;
    
    const float HalfSliceDepth = 0.70710678118654752440084436210485f; // sqrt(0.5f)
    float Weight = 1.0f;
    if (NonLinSlice < HalfSliceDepth)
    {
		// We multiply by weight to fade to 0 at depth 0. It works for luminance and opacity.
        Weight = saturate(NonLinSlice * NonLinSlice * 2.0f); // Square to have a linear falloff from the change of distribution above
    }
    
    float4 AP = SAMPLE_TEXTURE3D_LOD(_AerialPerspectiveTexture, sampler_linear_clamp, float3(ScreenUV, NonLinW),0);
    
    AP.rgb *= Weight;
    AP.a = 1.0 - (Weight * (1.0f - AP.a));
    
    return AP;
}

float3 GetLightDiskLuminance(
	float3 PlanetCenterToWorldPos, float3 WorldDir, float BottomRadius, float TopRadius,
    float3 AtmosphereLightDirection, float AtmosphereLightDiscCosHalfApexAngle, float3 AtmosphereLightDiscLuminance)
{
    const float ViewDotLight = dot(WorldDir, AtmosphereLightDirection);
    const float CosHalfApex = AtmosphereLightDiscCosHalfApexAngle;
    if (ViewDotLight > CosHalfApex)
    {
        const float3 TransmittanceToLight = GetAtmosphereTransmittance(PlanetCenterToWorldPos, WorldDir, BottomRadius, TopRadius);

		// Soften out the sun disk to avoid bloom flickering at edge. The soften is applied on the outer part of the disk.
        const float SoftEdge = saturate(2.0f * (ViewDotLight - CosHalfApex) / (1.0f - CosHalfApex));

        return TransmittanceToLight * AtmosphereLightDiscLuminance * SoftEdge;
    }
    return 0.0f;
}

//Phase
float RayleighPhase(float CosTheta)
{
    float Factor = 3.0f / (16.0f * M_PI);
    return Factor * (1.0f + CosTheta * CosTheta);
}

float HgPhase(float G, float CosTheta)
{
	// Reference implementation (i.e. not schlick approximation). 
	// See http://www.pbr-book.org/3ed-2018/Volume_Scattering/Phase_Functions.html
    float Numer = 1.0f - G * G;
    float Denom = 1.0f + G * G + 2.0f * G * CosTheta;
    return Numer / (4.0f * M_PI * Denom * sqrt(Denom));
}

//MediumSample
struct MediumSampleRGB
{
    float3 Scattering;
    float3 Absorption;
    float3 Extinction;

    float3 ScatteringMie;
    float3 AbsorptionMie;
    float3 ExtinctionMie;

    float3 ScatteringRay;
    float3 AbsorptionRay;
    float3 ExtinctionRay;

    float3 ScatteringOzo;
    float3 AbsorptionOzo;
    float3 ExtinctionOzo;

    float3 Albedo;
};
float3 GetAlbedo(float3 Scattering, float3 Extinction)
{
    return Scattering / max(0.001f, Extinction);
}
//计算瑞利散射，米氏散射，臭氧散射的各种相关值
MediumSampleRGB SampleMediumRGB(in float3 WorldPos)
{
    const float SampleHeight = max(0.0, (length(WorldPos) - _BottomRadiusKm));

    const float DensityMie = exp(_MieDensityExpScale * SampleHeight);

    const float DensityRay = exp(_RayleighDensityExpScale * SampleHeight);

    const float DensityOzo = SampleHeight < _AbsorptionDensity0LayerWidth ?
		saturate(_AbsorptionDensity0LinearTerm * SampleHeight + _AbsorptionDensity0ConstantTerm) : // We use saturate to allow the user to create plateau, and it is free on GCN.
		saturate(_AbsorptionDensity1LinearTerm * SampleHeight + _AbsorptionDensity1ConstantTerm);

    MediumSampleRGB s;

    s.ScatteringMie = DensityMie * _MieScattering.rgb;
    s.AbsorptionMie = DensityMie * _MieAbsorption.rgb;
    s.ExtinctionMie = DensityMie * _MieExtinction.rgb;

    s.ScatteringRay = DensityRay * _RayleighScattering.rgb;
    s.AbsorptionRay = 0.0f;
    s.ExtinctionRay = s.ScatteringRay + s.AbsorptionRay;

    s.ScatteringOzo = 0.0f;
    s.AbsorptionOzo = DensityOzo * _AbsorptionExtinction.rgb;
    s.ExtinctionOzo = s.ScatteringOzo + s.AbsorptionOzo;

    s.Scattering = s.ScatteringMie + s.ScatteringRay + s.ScatteringOzo;
    s.Absorption = s.AbsorptionMie + s.AbsorptionRay + s.AbsorptionOzo;
    s.Extinction = s.ExtinctionMie + s.ExtinctionRay + s.ExtinctionOzo;
    s.Albedo = GetAlbedo(s.Scattering, s.Extinction);

    return s;
}

struct SamplingSetup
{
    bool VariableSampleCount;
    float SampleCountIni; // Used when VariableSampleCount is false
    float MinSampleCount;
    float MaxSampleCount;
    float DistanceToSampleCountMaxInv;
};
struct SingleScatteringResult
{
    float3 L; // Scattered light (luminance)
    float3 OpticalDepth; // Optical depth (1/m)
    float3 Transmittance; // Transmittance in [0,1] (unitless)
    float3 MultiScatAs1;
};
//照搬！！！
SingleScatteringResult IntegrateSingleScatteredLuminance(
	in float4 SVPos, in float3 WorldPos, in float3 WorldDir,
	in bool Ground, in SamplingSetup Sampling, in float DeviceZ, in bool MieRayPhase,
	in float3 Light0Dir, in float3 Light1Dir, in float3 Light0Illuminance, in float3 Light1Illuminance,
	in float AerialPespectiveViewDistanceScale,
	in float tMaxMax = 9000000.0f)
{
    SingleScatteringResult Result;
    Result.L = 0;
    Result.OpticalDepth = 0;
    Result.Transmittance = 1.0f;
    Result.MultiScatAs1 = 0;
    
    if (dot(WorldPos, WorldPos) <= _BottomRadiusKm * _BottomRadiusKm)
    {
        return Result;
    }
    
    float2 PixPos = SVPos.xy;
    
    float3 PlanetO = float3(0.0f, 0.0f, 0.0f);
    float tBottom = RaySphereIntersectNearest(WorldPos, WorldDir, PlanetO, _BottomRadiusKm);
    float tTop = RaySphereIntersectNearest(WorldPos, WorldDir, PlanetO, _TopRadiusKm);
    float tMax = 0.0f;
    if (tBottom < 0.0f)
    {
        if (tTop < 0.0f)
        {
            tMax = 0.0f;
            return Result;
        }
        else
        {
            tMax = tTop;
        }
    }
    else
    {
        if (tTop > 0.0f)
        {
            tMax = min(tTop, tBottom);
        }
    }
    
#if VIEWDATA_AVAILABLE 
    if (DeviceZ != FarDepthValue)
    {
        const float3 DepthBufferTranslatedWorldPosKm = GetScreenTranslatedWorldPos(SVPos, DeviceZ).xyz * M_TO_SKY_UNIT;
        const float3 TraceStartTranslatedWorldPosKm = WorldPos + _SkyPlanetTranslatedWorldCenterAndViewHeight.xyz * M_TO_SKY_UNIT; // apply planet offset to go back to world from planet local referencial.
        const float3 TraceStartToSurfaceWorldKm = DepthBufferTranslatedWorldPosKm - TraceStartTranslatedWorldPosKm;
        float tDepth = length(TraceStartToSurfaceWorldKm);
        if (tDepth < tMax)
        {
            tMax = tDepth;
        }
		//if the ray intersects with the atmosphere boundary, make sure we do not apply atmosphere on surfaces are front of it. 
        if (dot(WorldDir, TraceStartToSurfaceWorldKm) < 0.0)
        {
            return Result;
        }
    }
#endif 
    tMax = min(tMax, tMaxMax);
    
    //决定RayMarching sample相关的系数
    float SampleCount = Sampling.SampleCountIni;
    float SampleCountFloor = Sampling.SampleCountIni;
    float tMaxFloor = tMax;
    if (Sampling.VariableSampleCount)
    {
        SampleCount = lerp(Sampling.MinSampleCount, Sampling.MaxSampleCount, saturate(tMax * Sampling.DistanceToSampleCountMaxInv));
        SampleCountFloor = floor(SampleCount);
        tMaxFloor = tMax * SampleCountFloor / SampleCount; 
    }
    float dt = tMax / SampleCount;
    
    //相函数相关
    const float uniformPhase = 1.0f / (4.0f * M_PI);
    const float3 wi = Light0Dir;
    const float3 wo = WorldDir;
    float cosTheta = dot(wi, wo);
    float MiePhaseValueLight0 = HgPhase(_MiePhaseG, -cosTheta); 
    float RayleighPhaseValueLight0 = RayleighPhase(cosTheta);
#if SECOND_ATMOSPHERE_LIGHT_ENABLED
	cosTheta = dot(Light1Dir, wo);
	float MiePhaseValueLight1 = HgPhase(_MiePhaseG, -cosTheta);	
	float RayleighPhaseValueLight1 = RayleighPhase(cosTheta);
#endif
    
    // Ray march the atmosphere to integrate optical depth
    float3 L = 0.0f;
    float3 Throughput = 1.0f;
    float3 OpticalDepth = 0.0f;
    float t = 0.0f;
    float tPrev = 0.0f;
    
    //multiple with Exposure(但是我这里没有基于物理的光照系统，所以不做) 
    
#if SKYVIEWLUT_PASS
	float3x3 LocalReferencial = (float3x3)_SkyViewLutReferential;
#endif
    
    float PixelNoise = SkyAtmosphereNoise(PixPos.xy);
    for (float SampleI = 0.0f; SampleI < SampleCount; SampleI += 1.0f)
    {
        if (Sampling.VariableSampleCount)
        {
            // More expenssive but artefact free
            float t0 = (SampleI) / SampleCountFloor;
            float t1 = (SampleI + 1.0f) / SampleCountFloor;;
			// Non linear distribution of samples within the range.
            t0 = t0 * t0;
            t1 = t1 * t1;
			// Make t0 and t1 world space distances.
            t0 = tMaxFloor * t0;
            if (t1 > 1.0f)
            {
                t1 = tMax;
            }
            else
            {
                t1 = tMaxFloor * t1;
            }
            t = t0 + (t1 - t0) * PixelNoise;
            dt = t1 - t0;
        }
        else
        {
            t = tMax * (SampleI + PixelNoise) / SampleCount;
        }
        float3 P = WorldPos + t * WorldDir;
        float PHeight = length(P);
        
        // Sample the medium
        MediumSampleRGB Medium = SampleMediumRGB(P);      
        const float3 SampleOpticalDepth = Medium.Extinction * dt * AerialPespectiveViewDistanceScale;
        const float3 SampleTransmittance = exp(-SampleOpticalDepth);
        OpticalDepth += SampleOpticalDepth;
        
        // Phase and transmittance for light 0
        const float3 UpVector = P / PHeight;
        float Light0ZenithCosAngle = dot(Light0Dir, UpVector);
        float3 TransmittanceToLight0 = GetTransmittance(Light0ZenithCosAngle, PHeight);
        float3 PhaseTimesScattering0;
        if (MieRayPhase)
        {
            PhaseTimesScattering0 = Medium.ScatteringMie * MiePhaseValueLight0 + Medium.ScatteringRay * RayleighPhaseValueLight0;
        }
        else
        {
            PhaseTimesScattering0 = Medium.Scattering * uniformPhase;
        }
#if SECOND_ATMOSPHERE_LIGHT_ENABLED
		// Phase and transmittance for light 1
		float Light1ZenithCosAngle = dot(Light1Dir, UpVector);
		float3 TransmittanceToLight1 = GetTransmittance(Light1ZenithCosAngle, PHeight);
		float3 PhaseTimesScattering1;
		if (MieRayPhase)
		{
			PhaseTimesScattering1 = Medium.ScatteringMie * MiePhaseValueLight1 + Medium.ScatteringRay * RayleighPhaseValueLight1;
		}
		else
		{
			PhaseTimesScattering1 = Medium.Scattering * uniformPhase;
		}
#endif
        
        // Multiple scattering approximation
        float3 MultiScatteredLuminance0 = 0.0f;
#if defined(MULTISCATTERING_APPROX_SAMPLING_ENABLED)
		MultiScatteredLuminance0 = GetMultipleScattering(P, Light0ZenithCosAngle);
#endif    

        float tPlanet0 = RaySphereIntersectNearest(P, Light0Dir, PlanetO + PLANET_RADIUS_OFFSET * UpVector, _BottomRadiusKm);
        float PlanetShadow0 = tPlanet0 >= 0.0f ? 0.0f : 1.0f;
        
//#if SKYVIEWLUT_PASS
//用来做物体投射到大气上的阴影的，没有这个需求，所以不做
//		ShadowP0 = GetTranslatedCameraPlanetPos() + t * mul(LocalReferencial, WorldDir); // Inverse of the local SkyViewLUT referencial transform
//#endif
        
#if SAMPLE_CLOUD_SHADOW
        
#endif
        float3 S = Light0Illuminance * (PlanetShadow0 * TransmittanceToLight0 * PhaseTimesScattering0 + MultiScatteredLuminance0 * Medium.Scattering);
        
#if SECOND_ATMOSPHERE_LIGHT_ENABLED
        float tPlanet1 = RaySphereIntersectNearest(P, Light1Dir, PlanetO + PLANET_RADIUS_OFFSET * UpVector, _BottomRadiusKm);
		float PlanetShadow1 = tPlanet1 >= 0.0f ? 0.0f : 1.0f;
        
//#if SKYVIEWLUT_PASS
//用来做物体投射到大气上的阴影的，没有这个需求，所以不做
//		ShadowP1 = GetTranslatedCameraPlanetPos() + t * mul(LocalReferencial, WorldDir); // Inverse of the local SkyViewLUT referencial transform
//#endif
 
//次光源不需要云阴影？不，需要！！！。。。需要吗？很纠结 
#if SAMPLE_CLOUD_SHADOW
        
#endif
        S += Light1Illuminance * PlanetShadow1 * TransmittanceToLight1 * PhaseTimesScattering1;
#endif
        // 1 is the integration of luminance over the 4pi of a sphere, and assuming an isotropic phase function of 1.0/(4*PI) 
        Result.MultiScatAs1 += Throughput * Medium.Scattering * 1.0f * dt;
        
        float3 Sint = (S - S * SampleTransmittance) / Medium.Extinction; // integrate along the current step segment 
        L += Throughput * Sint; // accumulate and also take into account the transmittance from previous steps
        Throughput *= SampleTransmittance;
        
        tPrev = t;
    }
    
    if (Ground && tMax == tBottom)
    {
		// Account for bounced light off the planet
        float3 P = WorldPos + tBottom * WorldDir;
        float PHeight = length(P);

        const float3 UpVector = P / PHeight;
        float Light0ZenithCosAngle = dot(Light0Dir, UpVector);
        float3 TransmittanceToLight0 = GetTransmittance(Light0ZenithCosAngle, PHeight);

        const float NdotL0 = saturate(dot(UpVector, Light0Dir));
        L += Light0Illuminance * TransmittanceToLight0 * Throughput * NdotL0 * _GroundAlbedo.rgb / M_PI;
#if SECOND_ATMOSPHERE_LIGHT_ENABLED
		{
			const float NdotL1 = saturate(dot(UpVector, Light1Dir));
			float Light1ZenithCosAngle = dot(UpVector, Light1Dir);
			float3 TransmittanceToLight1 = GetTransmittance(Light1ZenithCosAngle, PHeight);
			L += Light1Illuminance * TransmittanceToLight1 * Throughput * NdotL1 * _GroundAlbedo.rgb / M_PI;
		}
#endif
    }
    
    Result.L = L;
    Result.OpticalDepth = OpticalDepth;
    Result.Transmittance = Throughput;
    
    return Result;
}
#endif