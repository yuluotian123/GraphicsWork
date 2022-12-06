using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Yu_Weather
{
    public struct AtmosphereLightData
    {
        public int[] bRenderSunDisk;
        public int[] bCastShadow;

        public Vector4[] outSpaceIlluminance;
        public Vector4[] lightDiskOutSpaceLuminance;
        public float[] cosHalfApexAngle;
        public Vector4[] direction;

        public Texture[] lightDiskTexture;

        public float[] resolution;
        public float[] shadowCloudStrength;

        public AtmosphereLightData(int maxCounts)
        {
            bRenderSunDisk = new int[maxCounts];    
            bCastShadow = new int[maxCounts];
            outSpaceIlluminance = new Vector4[maxCounts];
            lightDiskOutSpaceLuminance = new Vector4[maxCounts];
            cosHalfApexAngle = new float[maxCounts];    
            direction = new Vector4[maxCounts];
            lightDiskTexture = new Texture[maxCounts];
            resolution = new float[maxCounts];
            shadowCloudStrength = new float[maxCounts];
        }
    }

    public class SceneSkyInfo
    {
        public static int MAX_LIGHT_COUNTS = 2;
        AtmosphereLightData lightDatas = new AtmosphereLightData(MAX_LIGHT_COUNTS);

        public int frame = 0;

        //skyAtmosphere
        public ShaderVariableSkyAtmosphere skyParameters;

        public RenderTexture transmittanceLut;
        public RenderTexture multiScatteredLuminanceLut;
        public RenderTexture distantSkyLightLut;
        public float ambientSkyIntensity;

        public bool bFastSky;
        public bool bFastAerialPespective;
        public bool bRayMarching;
        public bool bSecondLightEnabled;
        public bool bLightDiskEnabled;

        //volumetricCloud
        public ShaderVariableVolumetricCloud cloudParameters;

        Texture2D cloudMap;
        Texture2D cloudLut;
        Texture3D baseNoiseTex;
        Texture3D detailNoiseTex;

        RenderTexture cloudShadowMap0;
        RenderTexture cloudShadowMap1;

        public void Update(SkyAtmosphereController skyAC, VolumetricCloudController skyVC)
        {
            if (skyAC == null)
            {
                UpdateSkyParametersByDefault();
                bFastSky = false;
                bFastAerialPespective = false;
                bRayMarching = false;
                bLightDiskEnabled = true;
            }
            else
            {
                UpdateSkyParameters(skyAC);
                ambientSkyIntensity = skyAC.AmbientSkyIntensity;
                bFastSky = skyAC.UseFastSky;
                bFastAerialPespective = skyAC.UseFastAerialPespective;
                bRayMarching = skyAC.ForceRayMarching;
                bLightDiskEnabled = skyAC.RenderLightDisk;
            }

            if (skyVC == null)
            {
                UpdateCloudParametersByDefault();
                cloudMap = null;
                cloudLut = null;
                baseNoiseTex = null;
                detailNoiseTex = null;
            }
            else
            {
                UpdateCloudParameters(skyVC);

                cloudMap = skyVC.cloudMap;
                cloudLut = skyVC.cloudLut;
                baseNoiseTex = skyVC.baseNoiseTex;
                detailNoiseTex = skyVC.detailNoiseTex;
            }

            UpdateAtmosphereLightDatas();
        }

        //UpdateSkyAtmosphereCBuffer
        private void UpdateSkyParameters(SkyAtmosphereController skyAC)
        {
            skyParameters._BottomRadiusKm = skyAC.BottomRadiusKm;
            skyParameters._TopRadiusKm = skyAC.BottomRadiusKm + skyAC.AtmosphereHeightKm;
            skyParameters._MultiScatteringFactor = skyAC.MultiScatteringFactor;
            skyParameters._RayleighDensityExpScale = -1.0f / skyAC.RayleighExponentialDistribution;

            skyParameters._RayleighScattering = skyAC.RayleighScattering * skyAC.RayleighScatteringScale;
            skyParameters._MieScattering = skyAC.MieScattering * skyAC.MieScatteringScale;
            skyParameters._MieAbsorption = skyAC.MieAbsorption * skyAC.MieAbsorptionScale;
            skyParameters._MieExtinction = skyParameters._MieScattering + skyParameters._MieAbsorption;

            skyParameters._MieDensityExpScale = -1.0f / skyAC.MieExponentialDistribution;
            skyParameters._MiePhaseG = skyAC.MieAnisotropy;

            skyParameters._AbsorptionExtinction = skyAC.OtherAbsorption * skyAC.OtherAbsorptionScale;

            if (skyAC.OtherTentDistribution_Width > 0.0f && skyAC.OtherTentDistribution_TipValue > 0.0f)
            {
                float px = skyAC.OtherTentDistribution_TipAltitude;
                float py = skyAC.OtherTentDistribution_TipValue;
                float slope = skyAC.OtherTentDistribution_TipValue / skyAC.OtherTentDistribution_Width;
                skyParameters._AbsorptionDensity0LayerWidth = px;
                skyParameters._AbsorptionDensity0LinearTerm = slope;
                skyParameters._AbsorptionDensity1LinearTerm = -slope;
                skyParameters._AbsorptionDensity0ConstantTerm = py - px * skyParameters._AbsorptionDensity0LinearTerm;
                skyParameters._AbsorptionDensity1ConstantTerm = py - px * skyParameters._AbsorptionDensity1LinearTerm;
            }
            else
            {
                skyParameters._AbsorptionDensity0LayerWidth = 0;
                skyParameters._AbsorptionDensity0LinearTerm = 0;
                skyParameters._AbsorptionDensity1LinearTerm = -0;
                skyParameters._AbsorptionDensity0ConstantTerm = 0;
                skyParameters._AbsorptionDensity1ConstantTerm = 0;
            }

            skyParameters._GroundAlbedo = skyAC.GroundAlbedo;
            skyParameters._SkyLuminanceFactor = skyAC.SkyLuminanceFactor;
            skyParameters._UnUsed = 0.0f;
        }
        private void UpdateSkyParametersByDefault()
        {
            skyParameters._BottomRadiusKm = 6360.0f;
            skyParameters._TopRadiusKm = 6420.0f;
            skyParameters._MultiScatteringFactor = 1.0f;
            skyParameters._RayleighDensityExpScale = -1.0f / 8.0f;

            skyParameters._RayleighScattering = new Vector4(0.005802f, 0.013558f, 0.033100f, 0.0f);
            skyParameters._MieScattering = new Vector4(0.003996f, 0.003996f, 0.003996f, 0.0f);
            skyParameters._MieAbsorption = new Vector4(0.000444f, 0.000444f, 0.000444f, 0.0f);
            skyParameters._MieExtinction = skyParameters._MieScattering + skyParameters._MieAbsorption;

            skyParameters._MieDensityExpScale = -1.0f / 1.2f;
            skyParameters._MiePhaseG = 0.8f;

            skyParameters._AbsorptionExtinction = new Vector4(0.000650f, 0.001881f, 0.000085f, 0);

            float TipAltitude = 25.0f;
            float TipValue = 1.0f;
            float Width = 15.0f;
            if (Width > 0.0f && TipValue > 0.0f)
            {
                float px = TipAltitude;
                float py = TipValue;
                float slope = TipValue / Width;
                skyParameters._AbsorptionDensity0LayerWidth = px;
                skyParameters._AbsorptionDensity0LinearTerm = slope;
                skyParameters._AbsorptionDensity1LinearTerm = -slope;
                skyParameters._AbsorptionDensity0ConstantTerm = py - px * skyParameters._AbsorptionDensity0LinearTerm;
                skyParameters._AbsorptionDensity1ConstantTerm = py - px * skyParameters._AbsorptionDensity1LinearTerm;
            }
            else
            {
                skyParameters._AbsorptionDensity0LayerWidth = 0;
                skyParameters._AbsorptionDensity0LinearTerm = 0;
                skyParameters._AbsorptionDensity1LinearTerm = -0;
                skyParameters._AbsorptionDensity0ConstantTerm = 0;
                skyParameters._AbsorptionDensity1ConstantTerm = 0;
            }


            skyParameters._GroundAlbedo = new Vector4(0.4f, 0.4f, 0.4f, 1.0f);
            skyParameters._SkyLuminanceFactor = new Vector4(1.0f, 1.0f, 1.0f, 1.0f);
            skyParameters._UnUsed = 0.0f;
        }

        //UpdateCloudCBuffer
        private void UpdateCloudParameters(VolumetricCloudController skyVC)
        {

        }
        private void UpdateCloudParametersByDefault()
        {

        }

        //UpdateLights
        private void UpdateAtmosphereLightDatas()
        {
            //低性能的 但是我也没想到什么好办法 难道要我全部手动？
            List<AtmosphereLight> Lights = new List<AtmosphereLight>(Object.FindObjectsOfType<AtmosphereLight>());

            bSecondLightEnabled = Lights.Count == MAX_LIGHT_COUNTS;

            for (int i = 0; i < MAX_LIGHT_COUNTS; i++)
            {
                if (i >= Lights.Count)
                {
                    lightDatas.bRenderSunDisk[i] = 0;
                    lightDatas.outSpaceIlluminance[i] = Vector4.zero;
                    lightDatas.lightDiskOutSpaceLuminance[i] = Vector4.zero;
                    lightDatas.direction[i] = new Vector4(0,1,0,1);
                    lightDatas.lightDiskTexture[i] = null;
                    lightDatas.cosHalfApexAngle[i] = 0;
                    lightDatas.bCastShadow[i] = 0;
                    lightDatas.resolution[i] = 1;
                    lightDatas.shadowCloudStrength[i] = 0;
                    continue;
                }

                Lights[i].lightIlluminanceOnGround =
                    Lights[i].outSpaceColor * Lights[i].lightTransTransmittance * UpdateAtmosphereLightOnGroundTransmittance(Lights[i]);

                lightDatas.bRenderSunDisk[i] = Lights[i].renderDisk?1:0;
                lightDatas.outSpaceIlluminance[i] = Lights[i].GetLightOutSpaceIlluminance();
                lightDatas.lightDiskOutSpaceLuminance[i] = Lights[i].GetLightDiskOutSpaceLuminance();
                lightDatas.direction[i] = -Lights[i].GetLightDirection();
                lightDatas.lightDiskTexture[i] = Lights[i].diskTexture;
                lightDatas.cosHalfApexAngle[i] = Mathf.Cos(Lights[i].GetLightHalfApexAngleRadian());
                lightDatas.bCastShadow[i] = Lights[i].useCloudShadow?1:0;
                lightDatas.resolution[i] = Lights[i].shadowResolution;
                lightDatas.shadowCloudStrength[i] = Lights[i].shadowCloudStrength;
            }

            UpdateLightVariables();
        }
        private void UpdateLightVariables()
        {

            Shader.SetGlobalVectorArray(_LightDirection, lightDatas.direction);
            Shader.SetGlobalVectorArray(_LightIlluminanceOuterSpace, lightDatas.outSpaceIlluminance);
            Shader.SetGlobalVectorArray(_LightDiskLuminanceOuterSpace, lightDatas.lightDiskOutSpaceLuminance);
            Shader.SetGlobalFloatArray(_LightDiskCosHalfApexAngle, lightDatas.cosHalfApexAngle);
            Shader.SetGlobalFloatArray(_ShadowCloudStrength, lightDatas.shadowCloudStrength);
            Shader.SetGlobalFloatArray(_ShadowCloudResolution, lightDatas.resolution);

            Shader.SetGlobalInteger(_LightDiskEnable0, lightDatas.bRenderSunDisk[0]);
            Shader.SetGlobalInteger(_LightDiskEnable1, lightDatas.bRenderSunDisk[1]);
            Shader.SetGlobalTexture(_LightDiskTexture0, lightDatas.lightDiskTexture[0]);
            Shader.SetGlobalTexture(_LightDiskTexture0, lightDatas.lightDiskTexture[1]);
        }
        public Color UpdateAtmosphereLightOnGroundTransmittance(AtmosphereLight light)
        {
            Vector3 WorldPos = new Vector3(0.0f, skyParameters._BottomRadiusKm + 0.5f, 0.0f);
            float Elevation = Mathf.Max(light.transform.eulerAngles.x, -90.0f);
            Elevation = Mathf.Deg2Rad * Elevation;
            Vector3 WorldDir = new Vector3(-Mathf.Cos(Elevation), Mathf.Sin(Elevation), 0);
            Vector4 OpticalDepthRGB = OpticalDepth(WorldPos, WorldDir);
            return new Color(Mathf.Exp(-OpticalDepthRGB.x), Mathf.Exp(-OpticalDepthRGB.y), Mathf.Exp(-OpticalDepthRGB.z));
        }
        private Vector2 RayIntersectSphere(Vector3 RayOrigin, Vector3 RayDirection, Vector3 SphereOrigin, float SphereRadius)
        {
            Vector3 LocalPosition = RayOrigin - SphereOrigin;
            float LocalPositionSqr = Vector3.Dot(LocalPosition, LocalPosition);

            Vector3 QuadraticCoef;
            QuadraticCoef.x = Vector3.Dot(RayDirection, RayDirection);
            QuadraticCoef.y = 2.0f * Vector3.Dot(RayDirection, LocalPosition);
            QuadraticCoef.z = LocalPositionSqr - SphereRadius * SphereRadius;

            float Discriminant = QuadraticCoef.y * QuadraticCoef.y - 4.0f * QuadraticCoef.x * QuadraticCoef.z;

            // Only continue if the ray intersects the sphere
            Vector2 Intersections = new Vector2(-1.0f, -1.0f);
            if (Discriminant >= 0)
            {
                float SqrtDiscriminant = Mathf.Sqrt(Discriminant);
                Intersections.x = (-QuadraticCoef.y - 1.0f * SqrtDiscriminant) / (2 * QuadraticCoef.x);
                Intersections.y = (-QuadraticCoef.y + 1.0f * SqrtDiscriminant) / (2 * QuadraticCoef.x);
            }
            return Intersections;

        }
        private float raySphereIntersectNearest(Vector3 RayOrigin, Vector3 RayDirection, Vector3 SphereOrigin, float SphereRadius)
        {
            Vector2 sol = RayIntersectSphere(RayOrigin, RayDirection, SphereOrigin, SphereRadius);
            float sol0 = sol.x;
            float sol1 = sol.y;
            if (sol0 < 0.0f && sol1 < 0.0f)
            {
                return -1.0f;
            }
            if (sol0 < 0.0f)
            {
                return Mathf.Max(0.0f, sol1);
            }
            else if (sol1 < 0.0f)
            {
                return Mathf.Max(0.0f, sol0);
            }
            return Mathf.Max(0.0f, Mathf.Min(sol0, sol1));
        }
        private Vector4 OpticalDepth(Vector3 RayOrigin, Vector3 RayDirection)
        {
            float TMax = raySphereIntersectNearest(RayOrigin, RayDirection, new Vector3(0.0f, 0.0f, 0.0f), skyParameters._TopRadiusKm);


            //Debug.Log(RayDirection);

            Vector4 OpticalDepthRGB = new Vector4(0, 0, 0, 0);
            Vector3 VectorZero = new Vector3(0, 0, 0);
            if (TMax > 0.0f)
            {
                const float SampleCount = 15.0f;
                const float SampleStep = 1.0f / SampleCount;
                float SampleLength = SampleStep * TMax;
                for (float SampleT = 0.0f; SampleT < 1.0f; SampleT += SampleStep)
                {
                    Vector3 Pos = RayOrigin + RayDirection * (TMax * SampleT);
                    float viewHeight = Vector3.Distance(Pos, VectorZero) - skyParameters._BottomRadiusKm;

                    float densityMie = Mathf.Max(0.0f, Mathf.Exp(skyParameters._MieDensityExpScale * viewHeight));
                    float densityRay = Mathf.Max(0.0f, Mathf.Exp(skyParameters._RayleighDensityExpScale * viewHeight));
                    float densityOzo = Mathf.Clamp(viewHeight < skyParameters._AbsorptionDensity0LayerWidth ?
                        skyParameters._AbsorptionDensity0LinearTerm * viewHeight + skyParameters._AbsorptionDensity0ConstantTerm :
                        skyParameters._AbsorptionDensity1LinearTerm * viewHeight + skyParameters._AbsorptionDensity1ConstantTerm,
                    0.0f, 1.0f);

                    Vector4 SampleExtinction = densityMie * skyParameters._MieExtinction + densityRay * skyParameters._RayleighScattering + densityOzo * skyParameters._AbsorptionExtinction;
                    OpticalDepthRGB += SampleLength * SampleExtinction;
                }
            }

            return OpticalDepthRGB;
        }


        public void Release()
        {
            if (transmittanceLut)
                RenderTexture.ReleaseTemporary(transmittanceLut);
            if (multiScatteredLuminanceLut)
                RenderTexture.ReleaseTemporary(multiScatteredLuminanceLut);
            if (distantSkyLightLut)
                RenderTexture.ReleaseTemporary(distantSkyLightLut);
        }

        //PropertyToID
        public readonly int skyParametersID = Shader.PropertyToID("ShaderVariableSkyAtmosphere");
        public readonly int cloudParametersID = Shader.PropertyToID("ShaderVariableVolumetricCloud");

        public readonly int _TransmittanceTexture = Shader.PropertyToID("_TransmittanceTexture");
        public readonly int _MultiScatteredLuminanceTexture = Shader.PropertyToID("_MultiScatteredLuminanceTexture");
        public readonly int _DistanceSkyLightTexture = Shader.PropertyToID("_DistantSkyLightTexture");

        public readonly int _CloudMapTexture = Shader.PropertyToID("_CloudMapTexture");
        public readonly int _CloudLutTexture = Shader.PropertyToID("_CloudLutTexture");
        public readonly int _BaseNoise = Shader.PropertyToID("_BaseNoise");
        public readonly int _ErosionNoise = Shader.PropertyToID("_ErosionNoise");

        public readonly int _LightDirection = Shader.PropertyToID("_AtmosphereLightDirection");
        public readonly int _LightIlluminanceOuterSpace = Shader.PropertyToID("_AtmosphereLightIlluminanceOuterSpace");
        public readonly int _LightDiskLuminanceOuterSpace = Shader.PropertyToID("_AtmosphereLightDiskLuminance");
        public readonly int _LightDiskCosHalfApexAngle =Shader.PropertyToID("_AtmosphereLightDiskCosHalfApexAngle");
        public readonly int _ShadowCloudStrength = Shader.PropertyToID("_ShadowCloudStrength");
        public readonly int _ShadowCloudResolution = Shader.PropertyToID("_ShadowCloudResolution");

        public readonly int _LightDiskEnable0 =Shader.PropertyToID("_AtmosphereLightDiskEnable0");
        public readonly int _LightDiskEnable1 = Shader.PropertyToID("_AtmosphereLightDiskEnable1");

        public readonly int _LightDiskTexture0 =Shader.PropertyToID("_AtmosphereLightDiskTexture0") ;
        public readonly int _LightDiskTexture1 = Shader.PropertyToID("_AtmosphereLightDiskTexture1");
    }
}
