Shader "Yu_Weather/FFTOceanShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }

HLSLINCLUDE
#pragma multi_compile _ USE_REFLECTION_PROBE USE_REFLECTION_PLANAR
#pragma multi_compile _ RENDER_AP

#include "Assets/myTinyWeatherSystem/Runtime/SkySystem/ShaderLibrary/SkyAtmosphereCommon.hlsl"
#include "WaterCommon.hlsl"

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
	float2 uv: TEXCOORD0;
    float3 worldPos: TEXCOORD1;
	float4 fogColor: TEXCOORD2;
	float4 screenPos: TEXCOORD3;
};

#ifdef USE_REFLECTION_PLANAR
TEXTURE2D(_ReflectionTexture);          SAMPLER(sampler_ReflectionTexture);
#elif USE_REFLECTION_PROBE
TEXTURECUBE(_ReflectionTexture);        SAMPLER(sampler_ReflectionTexture);
#endif

TEXTURE2D(_DisplaceTexture);            SAMPLER(sampler_DisplaceTexture);
TEXTURE2D(_NormalTexture);              SAMPLER(sampler_NormalTexture);
TEXTURE2D(_BubblesTexture);             SAMPLER(sampler_BubblesTexture);
TEXTURE2D(_BubblesSSSTexture);          SAMPLER(sampler_BubblesSSSTexture);\
TEXTURE2D_X_FLOAT(_CameraDepthTexture); SAMPLER(sampler_CameraDepthTexture);
     
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
	   float2 UV = vert.texcoord;
	   float4 displace = SAMPLE_TEXTURE2D_LOD(_DisplaceTexture,sampler_DisplaceTexture,UV,0);
	   worldSpaceVertex += displace.xyz;

	   o.pos = mul(UNITY_MATRIX_VP, float4(worldSpaceVertex, 1.0));
	   float4 ScreenUV = ComputeScreenPos(o.pos);
	   ScreenUV.xyz /= ScreenUV.w;

	   o.uv = UV;
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

float4 frag(v2f i, float facing : VFACE) : SV_Target{

       float2 ior = (i.screenPos.xy) / i.screenPos.w;

	   float3 worldNormal = TransformObjectToWorldNormal(SAMPLE_TEXTURE2D(_NormalTexture,sampler_NormalTexture,i.uv).xyz);
	   float3 worldNormal2 = worldNormal;
	   worldNormal.y *= _NormalPower;
	   worldNormal2 *= _NormalBias;
	   float3 viewVector = (_WorldSpaceCameraPos - i.worldPos.xyz);
	   float fade = Fade(viewVector);
	   viewVector = normalize(viewVector);

	   //compute WorldNormal
	   float3 worldUp = float3(0,1,0);
	   worldNormal = normalize(lerp(worldUp, worldNormal, fade));//for fresnel
	   worldNormal2 = normalize(worldNormal2);

	   //fersnel
	   half dotNV = saturate(dot(viewVector, worldNormal));
	   half fresnelPow = 5;
	   float fresnel = pow(1 - dotNV, fresnelPow);
	   half fresnelFac = saturate(_Fresnel + (1 - _Fresnel) * fresnel);

	   //reflect
	   float4 rtReflections = float4(0,0,0,0);
#ifdef USE_REFLECTION_PLANAR
	   float2 ruv = ior + lerp(0, worldNormal.xz * _Reflect, fade);
       rtReflections += SAMPLE_TEXTURE2D(_ReflectionTexture,sampler_ReflectionTexture,ruv);
#elif USE_REFLECTION_PROBE
       float3 rnormal = worldNormal;
	   rnormal.xz = lerp(0, rnormal.xz * _Reflect, fade);
       float3 reflectDir = reflect(-viewVector, rnormal);
	   rtReflections += SAMPLE_TEXTURECUBE(_ReflectionTexture,sampler_ReflectionTexture,reflectDir);
#endif
	   
	   //float bubbles = SAMPLE_TEXTURE2D(_BubblesTexture,sampler_BubblesTexture,i.uv).r;
	   //float SSSMask = SAMPLE_TEXTURE2D(_BubblesSSSTexture,sampler_BubblesSSSTexture,i.uv).r;

	   return float4(rtReflections);
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
	ZWrite On
	Cull Off
	HLSLPROGRAM
	#pragma vertex TessellationVertex
	#pragma hull Hull
	#pragma domain Domain
	#pragma fragment frag

	ENDHLSL
	}

    }
}
