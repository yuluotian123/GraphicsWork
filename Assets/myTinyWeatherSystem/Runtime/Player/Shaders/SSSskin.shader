Shader "Yu_Weather/SSSskin"
{
    Properties
    {
       _MainTex ("DSLut", 2D) = "white" {}
       _SpecTex("SSLut",2D) = "white" {}
       _Albedo("Albedo",Color) = (1,1,1,1)
       _Rough ("Rough", float) = 0.5
       _Metal("Metal",float) = 0.5
       _Shadow("MainLightShadow",float)= 1
       _ScatterScale("ScatterScale",float) = 1
    }
    SubShader
    {
        Tags {"RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline"}
        ZTest LEqual
        ZWrite on
        Cull Back

        Pass
        {
            Name "SSSskin"
            Tags{"LightMode" = "UniversalForward"}
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile _ _SHADOWS_SOFT
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
            TEXTURE2D(_MainTex);    SAMPLER(sampler_MainTex);
		    TEXTURE2D(_SpecTex);   SAMPLER(sampler_SpecTex);

            CBUFFER_START(UnityPerMaterial)
            float4 _Albedo;
		    float _Metal;
		    float _Rough;
            float _ScatterScale;
            float _Shadow;
		    CBUFFER_END

            struct appdata
            {
              float4 positionOS : POSITION;
              float4 texcoord : TEXCOORD0;
              float3 normalOS: NORMAL;
               float4 tangentOS: TANGENT;
              UNITY_VERTEX_INPUT_INSTANCE_ID
             };

            struct v2f
            {
              float4 positionCS : SV_POSITION;
              float2 uv: TEXCOORD0;
              float3 normalWS: TEXCOORD1;
              float3 worldPos: TEXCOORD2;
              float3 viewVector: TEXCOORD3;
              UNITY_VERTEX_INPUT_INSTANCE_ID
              UNITY_VERTEX_OUTPUT_STEREO
            };

            v2f vert (appdata v)
           {
             v2f o;
             UNITY_SETUP_INSTANCE_ID(v);
             UNITY_TRANSFER_INSTANCE_ID(v, o);
             UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

             o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
             o.uv = v.texcoord.xy;

             VertexNormalInputs normalInput = GetVertexNormalInputs(v.normalOS, v.tangentOS);
             o.normalWS = normalInput.normalWS;

             o.worldPos = TransformObjectToWorld(v.positionOS.xyz);
             o.viewVector = normalize(_WorldSpaceCameraPos - o.worldPos);

             return o;
           }

            float3 fresnelSchlick(float cosTheta, float3 F0)
            {
               return F0 + (1.0 - F0) * pow(clamp(1.0 - cosTheta, 0.0, 1.0), 5.0);
            }

            float4 frag (v2f o) : SV_Target
            {
                float3 F0 = float3(0.04f,0.04f,0.04f); 
                F0 = lerp(F0, _Albedo.rgb, _Metal);

                float3 viewDir = o.viewVector;
                float3 normal = o.normalWS;
                float3 worldPos = o.worldPos;

                float cuv =clamp(length(fwidth(normal)) / length(fwidth(worldPos)), 0.0, 1.0);

                                
                Light mainLight = GetMainLight(TransformWorldToShadowCoord(worldPos));
                float3 lightDir = normalize(mainLight.direction);
                float3 h = normalize(viewDir + lightDir);
                float NdotL = max(dot(normal,lightDir), 0.0);  
                float NdotH = max(dot(normal,h),0.0);

                float3 SSS = SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,float2(NdotL*0.5+0.5,cuv)).rgb;

                float p = SAMPLE_TEXTURE2D(_SpecTex,sampler_SpecTex,float2( clamp(NdotH,0,1), _Rough)).r;
                float PH = pow(abs(2.0 * p), 10.0);
                float3 Fr = fresnelSchlick(max(dot(h, viewDir), 0.0), float3(0.028f,0.028f,0.028f));
                float3 specular = max( PH * Fr / dot( h, h ), float3(0.f,0.f,0.f)) ;

                float3 kS = Fr;
                float3 kD = float3(1.0,1.0,1.0) - kS;
                kD *= 1.0 - _Metal;

                 float shadow =  lerp(1-_Shadow,1, mainLight.shadowAttenuation);

                float3 color = (kD*_Albedo.rgb/PI+specular)*mainLight.color.rgb* shadow * SSS *_ScatterScale;
#ifdef _ADDITIONAL_LIGHTS
                uint pixelLightCount = GetAdditionalLightsCount();
                for (uint lightIndex = 0u; lightIndex < pixelLightCount; ++lightIndex)
                {
                Light light = GetAdditionalLight(lightIndex, worldPos);
                lightDir = normalize(worldPos - light.direction);
                h = normalize(viewDir + lightDir);
                NdotL = max(dot(normal,lightDir), 0.0);  
                NdotH = max(dot(normal,h),0.0);

                SSS = SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,float2(NdotL*0.5+0.5,cuv)).rgb;

                p = SAMPLE_TEXTURE2D(_SpecTex,sampler_SpecTex,float2( clamp(NdotH,0,1), _Rough)).r;
                PH = pow(abs(2.0 * p), 10.0);
                Fr = fresnelSchlick(max(dot(h, viewDir), 0.0), float3(0.028f,0.028f,0.028f));
                specular = max( PH * Fr / dot( h, h ), float3(0.f,0.f,0.f)) ;

                kS = Fr;
                kD = float3(1.0,1.0,1.0) - kS;
                kD *= 1.0 - _Metal;

                color += (kD*_Albedo.rgb/PI+specular)*light.color.rgb*light.distanceAttenuation* SSS *_ScatterScale;
                }
#endif


                return float4(color,1);
            }

            ENDHLSL
        }

        Pass
        {
        	Tags {"LightMode"="ShadowCaster"}
			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            float3 _LightDirection;
                       struct appdata
            {
              float4 positionOS : POSITION;
              float4 texcoord : TEXCOORD0;
              float3 normalOS: NORMAL;
               float4 tangentOS: TANGENT;

             };

            struct v2f
            {
              float4 positionCS : SV_POSITION;
              float2 uv: TEXCOORD0;
              float3 normalWS: TEXCOORD1;
              float3 worldPos: TEXCOORD2;
            };

            v2f vert (appdata v)
           {
             v2f o;
             o.uv = v.texcoord.xy;

             VertexNormalInputs normalInput = GetVertexNormalInputs(v.normalOS, v.tangentOS);
             o.normalWS = normalInput.normalWS;

             o.worldPos = TransformObjectToWorld(v.positionOS.xyz);

             o.positionCS = TransformWorldToHClip(ApplyShadowBias( o.worldPos, o.normalWS, _LightDirection));


             return o;
           }

            float4 frag (v2f i) : SV_Target
            {
                return 0;
            }
        
        
            ENDHLSL
        }

        pass
        {
         Name "DepthOnly"
         Tags{"LightMode" = "DepthOnly"}

         ZWrite On
         ColorMask 0
           HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
         struct Attributes
{
    float4 position     : POSITION;
    float2 texcoord     : TEXCOORD0;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
    float2 uv           : TEXCOORD0;
    float4 positionCS   : SV_POSITION;
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

Varyings vert(Attributes input)
{
    Varyings output = (Varyings)0;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

    output.uv = input.texcoord;
    output.positionCS = TransformObjectToHClip(input.position.xyz);
    return output;
}

half4 frag(Varyings input) : SV_TARGET
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
    return 0;
}
        ENDHLSL
        
        }
    }
}
