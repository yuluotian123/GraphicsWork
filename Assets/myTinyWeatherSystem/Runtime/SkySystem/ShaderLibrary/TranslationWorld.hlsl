#ifndef YU_TRANSLATION_WORLD_INCLUDE
#define YU_TRANSLATION_WORLD_INCLUDE
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
float4x4 _Translated_V_Matrix;
float4x4 _Inv_Translated_V_Matrix;
//GL.GetGPUProjectionMatrix:将NDC空间转换成平台对应
float4x4 _Translated_VP_Matrix;
float4x4 _Inv_Translated_VP_Matrix;

float4x4 _SkyViewLutReferential;
float4 _TranslatedWorldCameraOrigin;
float4 _SkyCameraTranslatedWorldOrigin;
float4 _SkyPlanetTranslatedWorldCenterAndViewHeight;

float4 _ScreenRect;

float4 GetPositionNDC(float4 SVPosition)
{
    float2 UV = SVPosition.xy / _ScreenRect.xy; //[0...1]
    float Depth = SVPosition.z;//[1...0 or 0...1]
    
#if UNITY_UV_STARTS_AT_TOP
    //NDC 空间 （-1->1，-1->1，1->0）in Directx
    float4 positionNDC = float4((UV -0.5)*float2(2,-2), Depth, 1.0);
#else
    //NDC 空间 （-1->1，-1->1，-1->1）in OpenGL
    float4 positionNDC = float4((UV - 0.5f) * float2(2.0f, 2.0f), Depth * 2.0f - 1.0f, 1.0f);
#endif
    
    return positionNDC;
}

float3 SVPositionToTranslatedWorld(float4 SVPosition)
{   
    float4 positionNDC = GetPositionNDC(SVPosition);
    
    //使用GPUProjectionMatrix,可以忽略平台的NDC差异
    float4 positionWS = mul(_Inv_Translated_VP_Matrix, positionNDC);

    return float3(positionWS.xyz / positionWS.w);
}

float3 ScreenToWorldDir(float4 SVPosition)
{
    float2 UV = GetPositionNDC(SVPosition).xy;
    
    float Depth = UNITY_REVERSED_Z ? 0.0f : 1.0f;
    float4 WorldPos = mul(_Inv_Translated_VP_Matrix, float4(UV, Depth, 1));
    WorldPos /= WorldPos.w;
    
    return normalize(float3(WorldPos.xyz - _TranslatedWorldCameraOrigin.xyz));
}
#endif