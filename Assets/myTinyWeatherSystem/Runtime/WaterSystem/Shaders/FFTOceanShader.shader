Shader "Yu_Weather/FFTOceanShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }

HLSLINCLUDE
#pragma multi_compile _ USE_REFLECTION_PROBE USE_REFLECTION_PLANAR
#pragma multi_compile _ RENDER_AP
#pragma multi_compile _ IS_POSTPROCESSING

 // Lightweight Pipeline keywords
#pragma multi_compile _ _MAIN_LIGHT_SHADOWS
#pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
#pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
#pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS
#pragma multi_compile _ _SHADOWS_SOFT

#include "Assets/myTinyWeatherSystem/Runtime/SkySystem/ShaderLibrary/SkyAtmosphereCommon.hlsl"
#include "Assets/myTinyWeatherSystem/Runtime/WaterSystem/ShaderLibrary/WaterCommon.hlsl"

struct TessellationControlPoint 
{
	   float3 vertex : INTERNALTESSPOS;
	   float2 texcoord : TEXCOORD0;
};
struct appdata_base 
{
	  float4 vertex : POSITION;
	  float2 texcoord : TEXCOORD0;
	  //UNITY_VERTEX_INPUT_INSTANCE_ID
};
struct TessellationFactors
{
	float edge[3] : SV_TessFactor;
	float inside : SV_InsideTessFactor;
};

struct v2f
{
	float4 pos : SV_POSITION;
	float4 uv: TEXCOORD0;
    float3 worldPos: TEXCOORD1;
	float4 fogColor: TEXCOORD2;
	float4 screenPos: TEXCOORD3;
};

#ifdef USE_REFLECTION_PLANAR
TEXTURE2D(_ReflectionTexture);          SAMPLER(sampler_ReflectionTexture);
#elif USE_REFLECTION_PROBE
TEXTURECUBE(_ReflectionTexture);        SAMPLER(sampler_ReflectionTexture);
#endif

TEXTURE2D(_DisplaceTexture);            SAMPLER(sampler_DisplaceTexture_linear_repeat);
TEXTURE2D(_NormalTexture);              SAMPLER(sampler_NormalTexture_linear_repeat);
TEXTURE2D(_BubblesTexture);             SAMPLER(sampler_BubblesTexture_linear_repeat);
TEXTURE2D(_BubblesSSSTexture);          SAMPLER(sampler_BubblesSSSTexture_linear_repeat);

TEXTURE2D(_MainTex);                    SAMPLER(sampler_MainTex);
TEXTURE2D(_CameraOpaqueTexture);        SAMPLER(sampler_CameraOpaqueTexture_linear_clamp);
TEXTURE2D_X_FLOAT(_CameraDepthTexture); SAMPLER(sampler_CameraDepthTexture_linear_clamp);

TessellationControlPoint TessellationVertex(appdata_base v)
{
       TessellationControlPoint p;
       float3 worldSpaceVertex = mul(unity_ObjectToWorld, half4(v.vertex.xyz, 1)).xyz;
       p.vertex = worldSpaceVertex;
	   p.texcoord = v.texcoord;
	   return p;
}

TessellationFactors HullConstant(
	InputPatch<TessellationControlPoint, 3> patch
)
{
	float3 p0 = (patch[0].vertex.xyz);
	float3 p1 = (patch[1].vertex.xyz);
	float3 p2 = (patch[2].vertex.xyz);

	TessellationFactors f;
	f.edge[0] = LODFactor(p1, p2);
	f.edge[1] = LODFactor(p2, p0);
	f.edge[2] = LODFactor(p0, p1);
	f.inside = (f.edge[0] +
		f.edge[1] +
		f.edge[2]) / 3.0;
	return f;
}

[maxtessfactor(MAX_TESSELLATION_FACTORS)]
[domain("tri")]						// Processing triangle face
[partitioning("fractional_odd")]	// The parameter type of the subdivided factor, can be "integer" which is used to represent the integer, or can be a floating point number "fractional_odd"
[outputtopology("triangle_cw")]		// Clockwise vertex arranged as the front of the triangle
[patchconstantfunc("HullConstant")] // The function that calculates the factor of the triangle facet is not a constant. Different triangle faces can have different values. A constant can be understood as a uniform value for the three vertices inside a triangle face.
[outputcontrolpoints(3)]			// Explicitly point out that each patch handles three vertex data
TessellationControlPoint Hull(
	InputPatch<TessellationControlPoint, 3> patch,
	uint id : SV_OutputControlPointID
)
{
	return patch[id];
}

float _AerialPerspectiveStartDepthKm;

float4 ComputeAP(float2 UvBuffer,float3 TranslatedWorldPos){
    return GetAerialPerspectiveLuminanceTransmittance(
		float2(SKYCONFIG_AERIAL_PERSPECTIVE_LUTWIDTH,SKYCONFIG_AERIAL_PERSPECTIVE_LUTWIDTH),
		UvBuffer, TranslatedWorldPos * M_TO_SKY_UNIT, GetCameraTranslatedWorldPos()*M_TO_SKY_UNIT,
		SKYCONFIG_AERIAL_PERSPECTIVE_LUTDEPTH_RESOLUTION,
		_AerialPerspectiveStartDepthKm,
		SKYCONFIG_AERIAL_PERSPECTIVE_LUTDEPTH/SKYCONFIG_AERIAL_PERSPECTIVE_LUTDEPTH_RESOLUTION);
}

v2f vert_Raw(TessellationControlPoint vert)
{
       v2f o = (v2f)0;

	   float3 worldSpaceVertex = vert.vertex.xyz;
	   float2 UV =  worldSpaceVertex .xz/_MeshLength * _MeshScale.xy;
	   float4 displace = SAMPLE_TEXTURE2D_LOD(_DisplaceTexture,sampler_DisplaceTexture_linear_repeat,UV,0);
	   worldSpaceVertex += displace.xyz;

	   o.pos = mul(UNITY_MATRIX_VP, float4(worldSpaceVertex, 1.0));
	   float4 ScreenUV = ComputeScreenPos(o.pos);
	   ScreenUV.xyz /= ScreenUV.w;

	   o.uv.xy = UV;
       o.worldPos = worldSpaceVertex;
	   o.screenPos = ComputeScreenPos(o.pos);
	   o.fogColor = float4(0,0,0,0);
#ifdef RENDER_AP
       o.fogColor += ComputeAP(ScreenUV.xy,worldSpaceVertex.xyz -_WorldSpaceCameraPos.xyz);
#endif
	   return o;
}

[domain("tri")]
v2f Domain(
	TessellationFactors factors,
	OutputPatch<TessellationControlPoint, 3> patch,
	float3 barycentricCoordinates : SV_DomainLocation
) {
		TessellationControlPoint data;

#define MY_DOMAIN_PROGRAM_INTERPOLATE(fieldName) data.fieldName = \
		patch[0].fieldName * barycentricCoordinates.x + \
		patch[1].fieldName * barycentricCoordinates.y + \
		patch[2].fieldName * barycentricCoordinates.z;

		MY_DOMAIN_PROGRAM_INTERPOLATE(vertex)
		MY_DOMAIN_PROGRAM_INTERPOLATE(texcoord)

		return vert_Raw(data);
}

v2f PostProcessVertex(appdata_base vert)
{
     v2f o = (v2f)0;

	 o.pos = TransformObjectToHClip(vert.vertex.xyz);
	 float Depth = UNITY_REVERSED_Z ? 1.0f : 0.0f;
	 float4 p = float4(o.pos.x, o.pos.y, Depth, 1);
	 p = p * _ProjectionParams.y;
	 o.worldPos = mul(UNITY_MATRIX_I_VP,p).xyz;

	 o.uv = float4(o.worldPos - _WorldSpaceCameraPos,0);
	 o.uv = normalize(o.uv) * (length(o.uv.xyz) / _ProjectionParams.y);

	 o.screenPos =  ComputeScreenPos(o.pos);
	 o.fogColor = float4(0,0,0,0);
	 return o;
}

float4 frag(v2f i, float facing : VFACE) : SV_Target{
       float2 ior = (i.screenPos.xy) / i.screenPos.w;

	   //sample DepthMap
	   float rawCamDepth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, sampler_CameraDepthTexture_linear_clamp, ior).r;

//postPocess（需要根据屏幕坐标反推回WorldPos，但是是在进行贴图转换之后的坐标,但是没有想到什么好办法，所以禁用）
#if 0
		float depth01 = Linear01Depth( rawCamDepth,_ZBufferParams);
		float3 worldPos = getWorldPos(rawCamDepth, i.uv.xyz);
		float3 ray = worldPos -  _WorldSpaceCameraPos;

		//偷懒，得到的就是Transform的y值
		float height = _WaterDepthParams.y - _HeightExtra;
		float t = (height -  _WorldSpaceCameraPos.y) / ray.y;

		float3 rawWaterPos =  _WorldSpaceCameraPos + ray * t;
		i.uv = float4(rawWaterPos.xz/_MeshLength * _MeshScale,0,0);
		//错误结果
		i.worldPos = rawWaterPos.xyz + SAMPLE_TEXTURE2D_LOD(_DisplaceTexture,sampler_DisplaceTexture_linear_repeat,i.uv.xy,0).xyz;

		t = (i.worldPos.y -  _WorldSpaceCameraPos.y) / ray.y;

		 if(!(0 < t && t < 1 || depth01 >= 1 && t > 0))
		 {
		    float3 color = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex,ior).rgb;
            return float4(color, 1);
		 }
#ifdef RENDER_AP
        i.fogColor += ComputeAP(ior,i.worldPos.xyz -_WorldSpaceCameraPos.xyz);
#endif
#endif

       //获取Mesh UV
       float2 uv = i.uv.xy;

	   //view Direction Datas
//postPocess（需要根据屏幕坐标反推回WorldPos，但是是在进行贴图转换之后的坐标,但是没有想到什么好办法，所以禁用）
#if 0 
	   float3 viewPos = mul(_WaterCameraWorldToViewMatrix,float4(rawWaterPos.xyz,1)).xyz;
#else
	   float3 viewPos = TransformWorldToView(i.worldPos.xyz);
#endif
	   float3 viewVector = _WorldSpaceCameraPos - i.worldPos.xyz;
	   float viewDirUnit = length(viewPos / viewPos.z); // distance to surface unit(不知道该怎么称呼)
	   float viewLength = length(viewVector);
	   float fade = Fade(viewVector);
	   viewVector = normalize(viewVector);

	   //compute WorldNormal
	   float3 worldNormal = TransformObjectToWorldNormal(SAMPLE_TEXTURE2D(_NormalTexture,sampler_NormalTexture_linear_repeat,uv).xyz);
	   float3 worldNormal2 = worldNormal;
	   worldNormal.y *= _NormalPower;
	   worldNormal2.y *= _NormalBias;
	   float3 worldUp = float3(0,1,0);
	   worldNormal = normalize(lerp(worldUp, worldNormal, fade));//（根据距离衰减！！）for fresnel?not that good
	   worldNormal2 = normalize(worldNormal2);//for Spec 和 Light

	   //depth
	   float viewDepth = max(0, LinearEyeDepth(rawCamDepth,_ZBufferParams) - _ProjectionParams.y);
	   viewDepth = viewDepth * viewDirUnit - viewLength;//获取从水面到地面的距离（从相机的角度）
	   //rawCamDepth = (rawCamDepth * -_ProjectionParams.x) + (1-UNITY_REVERSED_Z);
	   half depthMulti = 1 / _MaxDepth;
	   float waterDepth = WaterDepth(i.worldPos);
	   
	   //fersnel
	   half dotNV = saturate(dot(viewVector, worldNormal2));//use worldNormal2 instead of worldNormal1 因为worldNormal1的远距离表现很烂
	   half fresnelPow = 8;
	   float fresnelFac = pow(1 - dotNV, fresnelPow);
	   //float fresnelFac = saturate(_Fresnel + (1 - _Fresnel) * fresnel);

	   //reflect
	   float4 rtReflections = float4(0,0,0,0);
#ifdef USE_REFLECTION_PLANAR
	   float2 ruv = ior + worldNormal.xz * _Reflect;
       rtReflections += SAMPLE_TEXTURE2D(_ReflectionTexture,sampler_ReflectionTexture,ruv);
#elif USE_REFLECTION_PROBE
       float3 rnormal = worldNormal;
	   rnormal.xz = lerp(0, rnormal.xz * _Reflect, fade);
       float3 reflectDir = reflect(-viewVector, rnormal);
	   rtReflections += SAMPLE_TEXTURECUBE(_ReflectionTexture,sampler_ReflectionTexture,reflectDir);
#endif

	   //Lighting
	   Light mainLight = GetMainLight(TransformWorldToShadowCoord(i.worldPos.xyz));
	   float3 lightDir =  normalize(mainLight.direction);
	   float3 lightColor = mainLight.color*_LightIntensityScale;
       float shadow = MainLightRealtimeShadow(TransformWorldToShadowCoord(i.worldPos.xyz));
	   shadow = lerp(1 - _Shadow, 1, shadow);
	   float3 ambientCol = SHIndirectDiffuse(worldNormal);

	   //SSS
	   float SSSMask = saturate(SAMPLE_TEXTURE2D(_BubblesSSSTexture,sampler_BubblesSSSTexture_linear_repeat,uv).r);
	   float WaveHeight = saturate(i.worldPos.y/_SSSMaxWaveHeight);//change into [0,1]
	   //不要scale，不然太小
	   float3 sssColor =  SSSColor(lightDir, viewVector, worldNormal2, WaveHeight, SSSMask).rgb*mainLight.color;
	   float3 lightDiffuse = dot(lightDir,worldNormal2) * lightColor;
	   float3 sss =  sssColor+lightDiffuse;
	   sss = sss * shadow + ambientCol;//HDR(>1)

	   // Foam
	   float3 foamMap = getFoamMap(uv);
	   //计算海浪Foam
	   float wavefoam = SAMPLE_TEXTURE2D_LOD(_BubblesTexture,sampler_BubblesTexture_linear_repeat,uv,0).r *3;
	   //计算物体边缘Foam
	   half depthEdge = saturate( viewDepth * _FoamEdge);
	   half depthAdd = saturate(1 - viewDepth * _FoamAdd) * .5;
	   half edgefoam = saturate((1 - min(waterDepth,viewDepth) * _FoamRange - 0.25) + depthAdd) * depthEdge;
	   //将Wavefoam和edgefoam结合得到Mask
	   half foamBlendMask = max(edgefoam,wavefoam)*_FoamScale;
	   half3 foamBlend = SAMPLE_TEXTURE2D(_AbsorptionScatteringRamp, sampler_AbsorptionScatteringRamp, half2(foamBlendMask, 0.66)).rgb;
	   half foamMask = saturate(length(foamMap * foamBlend) * 1.5 - 0.1);
	   float3 foam = foamMask.xxx * (mainLight.shadowAttenuation * lightColor + ambientCol)*_FoamColor.rgb;


	   // Distortion
	   half2 distortion = DistortionUVs(viewDepth , worldNormal);
	   distortion = ior.xy + distortion * _Refract;
	   float d = viewDepth;
	   float rd = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, sampler_CameraDepthTexture_linear_clamp, distortion).r;
	   viewDepth =  max(0, LinearEyeDepth(rawCamDepth,_ZBufferParams) - _ProjectionParams.y);
	   viewDepth = viewDepth * viewDirUnit - viewLength;
	   distortion = viewDepth < 0 ? ior : distortion;
	   viewDepth =  viewDepth < 0 ? d :  viewDepth;

	   //spec(better)
	   	   //不要scale，不然太小
	    float3 spec =  GGXSpecularDir(viewVector, worldNormal2, -lightDir)* shadow * mainLight.color*_SpecColor.rgb;
	   //BRDFData brdfData;
    //   half alpha = 1;
    //   InitializeBRDFData(half3(0, 0, 0), 0, half3(1, 1, 1), 0.95, alpha, brdfData);
	   //float3 spec = DirectBDRF(brdfData, worldNormal2, mainLight.direction, viewVector).rgb*lightColor*_SpecColor;
#ifdef _ADDITIONAL_LIGHTS
       uint pixelLightCount = 0;//cant use GetAdditionalLightsCount() 因为一些 神秘 的 剔除原因（）
       for (uint lightIndex = 0u; lightIndex < pixelLightCount; ++lightIndex)
       {
        Light light = GetAdditionalLight(lightIndex, i.worldPos.xyz);
        spec += GGXSpecularDir(viewVector, worldNormal2, i.worldPos - light.direction)*light.distanceAttenuation * light.shadowAttenuation*light.color;
        sss += light.distanceAttenuation * light.color;
       }
#endif
	   //scattering
	   sss *= Scattering(viewDepth * depthMulti);

	   //refract
	   half3 rtRefraction = SAMPLE_TEXTURE2D_LOD(_CameraOpaqueTexture, sampler_CameraOpaqueTexture_linear_clamp, distortion, viewDepth * 0.25).rgb;
	    rtRefraction *= Absorption((viewDepth) * depthMulti);

	   // Do compositing
	   float3 comp = lerp(lerp(rtRefraction.rgb,rtReflections.rgb,fresnelFac)+ sss.rgb +spec.rgb,foam,foamMask);

	   comp *= _SeaColor.rgb;

	   comp = float3(comp.rgb *i.fogColor.a + i.fogColor.rgb);

	   return float4(comp,1);
       //return final;
}


ENDHLSL

    SubShader
    {

    Tags {  "RenderPipeline"="UniversalPipeline" "RenderType" = "Transparent" }
	Pass
	{
	Tags{"LightMode" = "UniversalForward"}
	Blend One Zero
	ZTest LEqual
	ZWrite on
	Cull Off
	HLSLPROGRAM
	#pragma vertex TessellationVertex
	#pragma hull Hull
	#pragma domain Domain
	#pragma fragment frag

	ENDHLSL
	}


	//后处理pass 禁用
	pass
	{
	 Cull Off ZWrite Off ZTest Always
	HLSLPROGRAM
	#pragma vertex PostProcessVertex
	#pragma fragment frag
	ENDHLSL
	}
    }
}
