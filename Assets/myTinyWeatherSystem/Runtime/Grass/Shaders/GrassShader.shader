Shader "Yu_Weather/GrassShader"
{
    Properties
    {
		_MainTex ("GrassColorTexture", 2D) = "white" {}
		_AlphaTex ("GrassAlphaTexture", 2D) = "white" {}
		_SpecIntensity ("Specular Intensity", float) = 0
		_GrassTopColor ("Grass Top Color",Color)=(0,0,0,0)
		_GrassBottomColor ("Grass Bottom Color",Color)=(1,1,1,1)
		_ColorLevel ("Color Level",float)=1

		[Space(20)]
		_Height ("Grass Height", float) = 5
        _HeightRan ("Grass Height Random Range", float) = 0
        _Width ("Grass Width", float) = 0.3
        _WidthRan ("Grass Width Random Range", float) = 0

		[Space(20)]
        _Radius ("Interactive Radius", float) = 0.1
        _Strength ("Interactive Strength", float) = 0

		[Space(20)]
		 _WindEff ("Swing Effect", float) = 0
		_WindDirection("WindDirection(XYZ)", vector) = (1,0,0,0)
        _WindStrength("WindStrength",float) = 1

		[Space(20)]
        _LodDis1 ("LOD Distance 1", float) = 10
        _LodDis2 ("LOD Distance 2", float) = 500
		[Toggle] _NormalUp ("Normal Default Up", Float) = 0
		[Toggle] _ShadowCast ("CAST SHADOW", Float) = 0
    }

    SubShader
    {
        Tags { "RenderPipeline"="UniversalRenderPipeline" "RenderType" ="TransparentCutout" "Queue"="Transparent" "Layer"="Grass"}
        LOD 100
        Cull Off
        HLSLINCLUDE
		#pragma shader_feature_local _NORMALUP_ON 
		#pragma shader_feature_local _SHADOWCAST_ON 

        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

		TEXTURE2D(_MainTex);    SAMPLER(sampler_MainTex);
		TEXTURE2D(_AlphaTex);   SAMPLER(sampler_AlphaTex);
		float3 _PositionMoving; 

		CBUFFER_START(UnityPerMaterial)
		float _SpecIntensity;
		float4 _GrassTopColor;
		float4 _GrassBottomColor;
		float _ColorLevel;
		float _Radius;
        float _Strength;
		float _Height;
        float _Width;
        float _HeightRan;
        float _WidthRan;
		float _WindEff;
		float4 _WindDirection;
		float _WindStrength;
		float _LodDis1;
        float _LodDis2;
		CBUFFER_END

        struct appdata
        {
            float4 vertex : POSITION;
            float3 normal :NORMAL;
            float4 tangent : TANGENT;
            float2 uv : TEXCOORD0;
        };
        struct v2g
        {
            float2 uv : TEXCOORD0;
            float4 vertex : SV_POSITION; 
            float3 normal : NORMAL;
            float4 tangent : TANGENT;
            float3 viewPos : TEXCOORD2;
        };
		struct g2f
        {
        	float4 vertex : SV_POSITION;
        	float2 uv : TEXCOORD0;
        	float3 nDirWS : TEXCOORD1;
        	float3 posWS : TEXCOORD2;    	
        };

        v2g vert (appdata v)
        {
                v2g o;
                o.uv = v.uv;
                o.normal = v.normal;
                o.vertex = float4(TransformObjectToWorld(v.vertex.xyz),1);
                o.tangent = v.tangent;
                o.viewPos = TransformWorldToView(TransformObjectToWorld(v.vertex.xyz));
                return o;
         }

		 //Random Angle of Grass
		float3x3 AngleAxis3x3(float angle, float3 axis)
		{
				float s, c;
				sincos(angle, s, c);
				float x = axis.x;
				float y = axis.y;
				float z = axis.z;
				return float3x3(
					x*x + (y*y+z*z)*c, x*y*(1-c)-z*s, x*z*(1-c)-y*s,
					x*y*(1-c)+z*s, y*y+(x*x+z*z)*c, y*z*(1-c)-x*s,
					x*z*(1-c)-y*s, y*z*(1-c) + x*s, z*z+(x*x+y*y)*c
					);
	    }

		//Between [0,1]
		float rand(float3 seed)
		{
			float f = sin(dot(seed, float3(4.258, 178.31, 63.59)));
			f = frac(f * 43785.5453123);
			return f;
		}

        ENDHLSL

        Pass
        {
		    Tags {"LightMode"="UniversalForward"}
            HLSLPROGRAM
			#pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile _ _SHADOWS_SOFT
            #pragma vertex vert
            #pragma fragment frag
            #pragma geometry geom
            g2f geomOut(float3 normal, float3 pos, float2 uv)
            {
            	g2f o;
            	o.posWS = pos;
            	o.vertex = TransformWorldToHClip(pos);
            	o.uv = uv;
            	o.nDirWS = normal;
            	return o;
            }

            [maxvertexcount(30)]
			void geom(point v2g IN[1] : SV_POSITION, inout TriangleStream<g2f> triStream)
			{
				float3 pos = IN[0].vertex.xyz;
				#if _NORMALUP_ON
					float3 vNormal = float3(0,1,0);
				#else
					float3 vNormal = IN[0].normal;
				#endif

				float4 vTangent = IN[0].tangent;
				float3 vBinormal = cross(vNormal,vTangent.xyz)*vTangent.w;
				float3x3 TBN = float3x3(
				vTangent.x, vBinormal.x, vNormal.x,
				vTangent.y, vBinormal.y, vNormal.y,
				vTangent.z, vBinormal.z, vNormal.z);
				TBN = mul( TBN, AngleAxis3x3(rand(pos)*2*PI, float3(0,0,1)));
				
				//草的不同长度和宽度
				_Width += (rand(pos)*2-1)*_WidthRan;
				_Height += (rand(pos)*2-1)*_HeightRan;
				
				//Wind
				float3 WindBias = float3(_WindDirection.x,0,_WindDirection.z)*_WindStrength * min(abs(2*frac(_Time.x*3)-1)-0.5,0)*2;

				//LOD
				int segValue = 12;
				if(-IN[0].viewPos.z>_LodDis2)
					segValue = 3;
				else if(-IN[0].viewPos.z>_LodDis1)
					segValue = 6;

				//INTERACTIVE
				float3 dis = distance(_PositionMoving, pos);
				float3 radius = 1-saturate(dis/_Radius);	//dis>_Radius,radius=0
				float3 sphereDisp = float3(pos.x-_PositionMoving.x,0,pos.z-_PositionMoving.z);
				sphereDisp *= radius*_Strength;
				sphereDisp = clamp(sphereDisp.xyz, -10, 10);

				////CALCULATE NORMAL
				float t = 1/(float)segValue;
				float segmentHeight = _Height*(1-t);
				float segmentWidth = _Width*t;
				float3 pos1 = pos+WindBias+float3(_WindEff*sin(_Time.y),0,0)+sphereDisp+float3(mul(TBN,float3(0,0,_Height)));	//TOP POINT WORLD POSITION
				float3 pos2 = pos+WindBias*(1-t)*(1-t)+float3(_WindEff*sin(_Time.y)*(1-t)*(1-t),0,0)+sphereDisp*(1-t)*(1-t)+float3(mul(TBN,float3(segmentWidth,0,segmentHeight)));	//TWO VERTICES UNDER THE TOP VERTICE
				float3 pos3 = pos+WindBias*(1-t)*(1-t)+float3(_WindEff*sin(_Time.y)*(1-t)*(1-t),0,0)+sphereDisp*(1-t)*(1-t)+float3(mul(TBN,float3(-segmentWidth,0,segmentHeight)));//USE THEM TO CALCULATE NORMAL OF THE TOP VERTICE
				float3 TANGENT1 = normalize(pos2-pos3);
				float3 TANGENT2 = normalize(pos1-pos3);
				float3 nDir = normalize(cross(TANGENT1,TANGENT2));

				//TOP
				triStream.Append(geomOut(nDir, pos1, float2(0.5,1)));

				//MIDDLE	
				for(int i=1;i<segValue;i++)
				{
					t = i/(float)segValue;
					segmentHeight = _Height*(1-t);
					segmentWidth = _Width*t;
					pos2 = pos+WindBias*(1-t)*(1-t)+float3(_WindEff*sin(_Time.y)*(1-t)*(1-t),0,0)+sphereDisp*(1-t)*(1-t)+float3(mul(TBN,float3(segmentWidth,0,segmentHeight)));
					pos3 = pos+WindBias*(1-t)*(1-t)+float3(_WindEff*sin(_Time.y)*(1-t)*(1-t),0,0)+sphereDisp*(1-t)*(1-t)+float3(mul(TBN,float3(-segmentWidth,0,segmentHeight)));
					TANGENT1 = normalize(pos2-pos3);
					TANGENT2 = normalize(pos1-pos3);
					nDir = normalize(cross(TANGENT1,TANGENT2));
					triStream.Append(geomOut(nDir, pos2, float2(1-0.5*t,1-t)));
					triStream.Append(geomOut(nDir, pos3, float2(0.5*t,1-t)));
					pos1=pos2;
				}
				//BOTTOM
				triStream.Append(geomOut(nDir, pos+float3(mul(TBN,float3(_Width,0,0))), float2(0,0)));
				triStream.Append(geomOut(nDir, pos+float3(mul(TBN,float3(-_Width,0,0))), float2(1,0)));	
			}

			float3 SHIndirectDiffuse(float3 nDirWS)			//INDIRECT LIGHTING
			{
			    float4 SHCoefficients[7];
			    SHCoefficients[0] = unity_SHAr;
			    SHCoefficients[1] = unity_SHAg;
			    SHCoefficients[2] = unity_SHAb;
			    SHCoefficients[3] = unity_SHBr;
			    SHCoefficients[4] = unity_SHBg;
			    SHCoefficients[5] = unity_SHBb;
			    SHCoefficients[6] = unity_SHC;
			    return max(0, float3(SampleSH9(SHCoefficients, nDirWS)));
			}

			 float4 frag (g2f i,float facing:VFACE) : SV_Target
			 {
			    Light light = GetMainLight(TransformWorldToShadowCoord(i.posWS));
				
				float4 lightColor = float4(light.color,1)*light.shadowAttenuation;
				float3 lightDir = normalize(light.direction);
                float3 viewDir = normalize(_WorldSpaceCameraPos.xyz-i.posWS);
                float3 normalDir = facing>0?-normalize(i.nDirWS):normalize(i.nDirWS);

				float lambert = (0.6+0.4*dot(normalDir,lightDir));
				float spec = pow(max(dot(normalDir,normalize(lightDir+viewDir)),0), 50)*_SpecIntensity;
				float3 AmbientColor = SHIndirectDiffuse(normalDir);//获取环境光（天光颜色：DistantSkyLight）
				float3 texColor = SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,i.uv).rgb;
				float alpha = SAMPLE_TEXTURE2D(_AlphaTex,sampler_AlphaTex,i.uv).r;

				float3 lerpColor = lerp(_GrassBottomColor.rgb,_GrassTopColor.rgb,pow(max(i.uv.y,0),_ColorLevel));

				float4 finalColor = lightColor*(lambert+spec)*float4(texColor*lerpColor,1)+float4(AmbientColor,0);

				return float4(finalColor.rgb,finalColor.w * alpha);
			 }
           
            ENDHLSL
        }

		//USE CUSTOM PASS TO CAST SHADOW
        Pass
        {
        	Tags {"LightMode"="ShadowCaster"}
			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma geometry geom
			
			float3 _LightDirection;
			g2f geomOut(float3 normal, float3 pos, float2 uv)
            {
            	g2f o;
            	o.posWS = pos;
            	o.nDirWS = normal;
            	o.vertex = TransformWorldToHClip( ApplyShadowBias(o.posWS,o.nDirWS,_LightDirection) );
            	o.uv = uv;
            	return o;
            }
            
			[maxvertexcount(30)]
			void geom(point v2g IN[1] : SV_POSITION, inout TriangleStream<g2f> triStream)
			{
				#ifndef _SHADOWCAST_ON
					return;
				#endif

				float3 pos = IN[0].vertex.xyz;

				#if _NORMALUP_ON
					float3 vNormal = float3(0,1,0);
				#else
					float3 vNormal = IN[0].normal;
				#endif

				float4 vTangent = IN[0].tangent;
				float3 vBinormal = cross(vNormal,vTangent.xyz)*vTangent.w;
				float3x3 TBN = float3x3(
				vTangent.x, vBinormal.x, vNormal.x,
				vTangent.y, vBinormal.y, vNormal.y,
				vTangent.z, vBinormal.z, vNormal.z	);
				TBN = mul( TBN, AngleAxis3x3(rand(pos)*2*PI, float3(0,0,1)) );
				
				_Width += (rand(pos)*2-1)*_WidthRan;
				_Height += (rand(pos)*2-1)*_HeightRan;
				
				//WIND
				float3 WindBias = float3(_WindDirection.x,0,_WindDirection.z)*_WindStrength*min(abs(2*frac(_Time.x*3)-1)-0.5,0)*2;
				
				//LOD
				int segValue = 3;

				//INTERACTIVE
				float3 dis = distance(_PositionMoving, pos);
				float3 radius = 1-saturate(dis/_Radius);	//dis>_Radius,radius=0
				float3 sphereDisp = float3(pos.x-_PositionMoving.x,0,pos.z-_PositionMoving.z);
				sphereDisp *= radius*_Strength;
				sphereDisp = clamp(sphereDisp.xyz, -10, 10);

				////CALCULATE NORMAL
				float t = 1/(float)segValue;
				float segmentHeight = _Height*(1-t);
				float segmentWidth = _Width*t;
				float3 pos1 = pos+WindBias+float3(_WindEff*sin(_Time.y),0,0)+sphereDisp+float3(mul(TBN,float3(0,0,_Height)));	
				float3 pos2 = pos+WindBias*(1-t)*(1-t)+float3(_WindEff*sin(_Time.y)*(1-t)*(1-t),0,0)+sphereDisp*(1-t)*(1-t)+float3(mul(TBN,float3(segmentWidth,0,segmentHeight)));	
				float3 pos3 = pos+WindBias*(1-t)*(1-t)+float3(_WindEff*sin(_Time.y)*(1-t)*(1-t),0,0)+sphereDisp*(1-t)*(1-t)+float3(mul(TBN,float3(-segmentWidth,0,segmentHeight)));
				float3 TANGENT1 = normalize(pos2-pos3);
				float3 TANGENT2 = normalize(pos1-pos3);
				float3 nDir = normalize(cross(TANGENT1,TANGENT2));
				//TOP
				triStream.Append(geomOut(nDir, pos1, float2(0.5,1)));
				//MIDDLE	
				for(int i=1;i<segValue;i++)
				{
					t = i/(float)segValue;
					segmentHeight = _Height*(1-t);
					segmentWidth = _Width*t;
					pos2 = pos+WindBias*(1-t)*(1-t)+float3(_WindEff*sin(_Time.y)*(1-t)*(1-t),0,0)+sphereDisp*(1-t)*(1-t)+float3(mul(TBN,float3(segmentWidth,0,segmentHeight)));
					pos3 = pos+WindBias*(1-t)*(1-t)+float3(_WindEff*sin(_Time.y)*(1-t)*(1-t),0,0)+sphereDisp*(1-t)*(1-t)+float3(mul(TBN,float3(-segmentWidth,0,segmentHeight)));
					TANGENT1 = normalize(pos2-pos3);
					TANGENT2 = normalize(pos1-pos3);
					nDir = normalize(cross(TANGENT1,TANGENT2));
					triStream.Append(geomOut(nDir, pos2, float2(1-0.5*t,1-t)));
					triStream.Append(geomOut(nDir, pos3, float2(0.5*t,1-t)));
					pos1=pos2;
				}
				//BOTTOM
				triStream.Append(geomOut(nDir, pos+float3(mul(TBN,float3(_Width,0,0))), float2(0,0)));
				triStream.Append(geomOut(nDir, pos+float3(mul(TBN,float3(-_Width,0,0))), float2(1,0)));	
			}

            float4 frag (g2f i) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }
    }
}
