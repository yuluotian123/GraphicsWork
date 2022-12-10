Shader "Yu_Weather/SkyAtmosphere"
{
    Properties{ _MainTex ("Texture", 2D) = "white" { } }

    HLSLINCLUDE
    #pragma vertex vert
    #define PER_PIXEL_NOISE
    #define MULTISCATTERING_APPROX_SAMPLING_ENABLED 
    #define PLANET_RADIUS_SAFE_TRACE_EDGE 0.01f

    #pragma multi_compile __ SECOND_ATMOSPHERE_LIGHT_ENABLED
    #pragma multi_compile __ FASTSKY_ENABLED 
    #pragma multi_compile __ FASTAERIALPERSPECTIVE_ENABLED
    #pragma multi_compile __ SOURCE_DISK_ENABLED
    #include "Assets/myTinyWeatherSystem/Runtime/SkySystem/ShaderLibrary/SkyAtmosphereCommon.hlsl"

    struct appdata
    {
      float4 positionOS : POSITION;
      float4 texcoord : TEXCOORD0;
     };

    struct v2f
    {
      float4 positionCS : SV_POSITION;
      float2 uv: TEXCOORD0;
     };

     v2f vert (appdata v)
     {
       v2f o;
       o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
       o.uv = v.texcoord.xy;
       return o;
      }

TEXTURE2D_X_FLOAT(_CameraDepthTexture); SAMPLER(sampler_CameraDepthTexture);
TEXTURE2D(_MainTex);                    SAMPLER(sampler_MainTex);

float _AerialPerspectiveStartDepthKm;
float _AerialPespectiveViewDistanceScale;
     
     float4 PrepareOutput(float4 texColor ,float3 PreExposedLuminance, float3 Transmittance = float3(1.0f, 1.0f, 1.0f))
     {
	     const float GreyScaleTransmittance = dot(Transmittance, float3(1.0f / 3.0f, 1.0f / 3.0f, 1.0f / 3.0f));

	     return float4(PreExposedLuminance + GreyScaleTransmittance * texColor.rgb , GreyScaleTransmittance);
     }

     float4 frag (v2f i) : SV_Target
     {
       float4 OutLuminance = 0;
       float2 PixPos = i.positionCS.xy;
       float2 UvBuffer = i.positionCS.xy/_ScreenParams.xy;
       float4 texColor =  SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, UvBuffer);

       float3 WorldPos = GetTranslatedCameraPlanetPos();
	   float3 WorldDir = GetScreenWorldDir(i.positionCS);
       

       //并没有Exposured Light，因为没有基于物理的光照系统
       float3 PreExposedL = 0;
	   float3 LuminanceScale = 1.0f;

       float DeviceZ = SAMPLE_TEXTURE2D_X(_CameraDepthTexture, sampler_CameraDepthTexture, UvBuffer).r;

       //RenderDisk(有问题！！！)
       if (DeviceZ == FarDepthValue)
	   {		 
		LuminanceScale = _SkyLuminanceFactor.rgb;
#if defined(SOURCE_DISK_ENABLED)
        if(_AtmosphereLightDiskEnable0 > 0 ){
            float t = RaySphereIntersectNearest(WorldPos, WorldDir, float3(0.0f, 0.0f, 0.0f), _BottomRadiusKm);
            if (t < 0.0f) { 
		        // GetLightDiskLuminance contains a tiny soft edge effect
                float3 LightDiskLuminance = GetLightDiskLuminance(WorldPos, WorldDir, _BottomRadiusKm, _TopRadiusKm ,
                _AtmosphereLightDirection[0].xyz,_AtmosphereLightDiskCosHalfApexAngle[0].x,_AtmosphereLightDiskLuminance[0].rgb);

                //To Do:Texture 
		        //float3 ExposedLightLuminance = LightDiskLuminance * OutputPreExposure;
		        PreExposedL += LightDiskLuminance;
	        }
        }
    #if defined(SECOND_ATMOSPHERE_LIGHT_ENABLED)
        if(_AtmosphereLightDiskEnable1 > 0 ){
            float t = RaySphereIntersectNearest(WorldPos, WorldDir, float3(0.0f, 0.0f, 0.0f), _BottomRadiusKm);
            if (t < 0.0f) { 
		        // GetLightDiskLuminance contains a tiny soft edge effect
                float3 LightDiskLuminance = GetLightDiskLuminance(WorldPos, WorldDir, _BottomRadiusKm, _TopRadiusKm ,
                _AtmosphereLightDirection[1].xyz,_AtmosphereLightDiskCosHalfApexAngle[1].x,_AtmosphereLightDiskLuminance[1].rgb);

                //To Do:Texture 
		        //float3 ExposedLightLuminance = LightDiskLuminance * OutputPreExposure;
		        PreExposedL += LightDiskLuminance;
	        }
        }
    #endif  
#endif
      }

       float ViewHeight = length(WorldPos);
#if FASTSKY_ENABLED
       if (ViewHeight < (_TopRadiusKm + PLANET_RADIUS_SAFE_TRACE_EDGE)&& DeviceZ == FarDepthValue)
	  {
		float2 UV;
		// The referencial used to build the Sky View lut
		float3x3 LocalReferencial = GetSkyViewLutReferential();

		// Input vectors expressed in this referencial: Up is always Y. Also note that ViewHeight is unchanged in this referencial.
		float3 WorldPosLocal = float3(0.0, ViewHeight,0.0);
		float3 UpVectorLocal = float3(0.0, 1.0,0.0);
		float3 WorldDirLocal = mul(LocalReferencial, WorldDir);
		float ViewZenithCosAngle = dot(WorldDirLocal, UpVectorLocal);

		// Now evaluate inputs in the referential
		bool IntersectGround = RaySphereIntersectNearest(WorldPosLocal, WorldDirLocal, float3(0, 0, 0), _BottomRadiusKm) >= 0.0f;

		SkyViewLutParamsToUv(IntersectGround, ViewZenithCosAngle, WorldDirLocal, ViewHeight, _BottomRadiusKm, float2(SKYCONFIG_FAST_SKY_LUTWIDTH,SKYCONFIG_FAST_SKY_LUTHEIGHT), UV);
		float3 SkyLuminance = SAMPLE_TEXTURE2D_X_LOD(_SkyViewTexture, sampler_linear_clamp, UV, 0).rgb;

		PreExposedL += SkyLuminance * LuminanceScale; //*(ViewOneOverPreExposure * OutputPreExposure);
		OutLuminance = PrepareOutput(texColor,PreExposedL);
		return OutLuminance;
	    }
#endif
#if FASTAERIALPERSPECTIVE_ENABLED
       float3 DepthBufferTranslatedWorldPos = GetScreenTranslatedWorldPos(i.positionCS, DeviceZ).xyz;

	   float4 AP = GetAerialPerspectiveLuminanceTransmittance(
		float2(SKYCONFIG_AERIAL_PERSPECTIVE_LUTWIDTH,SKYCONFIG_AERIAL_PERSPECTIVE_LUTWIDTH),
		UvBuffer, DepthBufferTranslatedWorldPos * M_TO_SKY_UNIT, GetCameraTranslatedWorldPos()*M_TO_SKY_UNIT,
		SKYCONFIG_AERIAL_PERSPECTIVE_LUTDEPTH_RESOLUTION,
		_AerialPerspectiveStartDepthKm,
		SKYCONFIG_AERIAL_PERSPECTIVE_LUTDEPTH/SKYCONFIG_AERIAL_PERSPECTIVE_LUTDEPTH_RESOLUTION);

	    PreExposedL += AP.rgb * LuminanceScale;

	    float Transmittance = AP.a;

	    OutLuminance = PrepareOutput(texColor,PreExposedL, float3(Transmittance, Transmittance, Transmittance));
	    return  OutLuminance;
#else
        if (!MoveToTopAtmosphere(WorldPos, WorldDir, _TopRadiusKm))
	    {
		   // Ray is not intersecting the atmosphere
		   OutLuminance = PrepareOutput(texColor,PreExposedL);
		   return OutLuminance;
	    }

        WorldPos += WorldDir * _AerialPerspectiveStartDepthKm;

        SamplingSetup Sampling = (SamplingSetup)0;
	    {
		   Sampling.VariableSampleCount = true;
		   Sampling.MinSampleCount = SKYCONFIG_SAMPLE_COUNT_MIN;
		   Sampling.MaxSampleCount = SKYCONFIG_SAMPLE_COUNT_MAX;
		   Sampling.DistanceToSampleCountMaxInv = 1/(float)SKYCONFIG_DISTANCE_TO_SAMPLE_COUNT_MAX;
	    }

        const bool Ground = false;
	    const bool MieRayPhase = true;
	    const float AerialPespectiveViewDistanceScale = DeviceZ == FarDepthValue ? 1.0f : _AerialPespectiveViewDistanceScale;
	    SingleScatteringResult ss = IntegrateSingleScatteredLuminance(
		i.positionCS, WorldPos, WorldDir,
		Ground, Sampling, DeviceZ, MieRayPhase,
		_AtmosphereLightDirection[0].xyz, _AtmosphereLightDirection[1].xyz, 
		_AtmosphereLightIlluminanceOuterSpace[0].rgb, _AtmosphereLightIlluminanceOuterSpace[1].rgb,
		AerialPespectiveViewDistanceScale);

	    PreExposedL += ss.L * LuminanceScale;
        OutLuminance = PrepareOutput(texColor,PreExposedL, ss.Transmittance);
        return OutLuminance;
#endif
     }
    ENDHLSL
    SubShader
    {
        Tags {  "RenderPipeline"="UniversalPipeline" }

        Pass
        {
            ZWrite Off
            ZTest Always
            Blend Off
            Cull Off

            HLSLPROGRAM
            #pragma fragment frag

            ENDHLSL
        }
    }
}
