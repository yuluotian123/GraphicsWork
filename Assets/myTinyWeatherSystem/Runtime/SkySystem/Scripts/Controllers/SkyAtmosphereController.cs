using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Yu_Weather
{
    [ExecuteInEditMode]
    public class SkyAtmosphereController : MonoBehaviour
    {
        /// <summary>
        /// datas for SkyAtmosphere
        /// </summary>
        [Foldout("地球设置（单位为KM）")]
        public float BottomRadiusKm = 6360.0f;
        public float AtmosphereHeightKm = 60.0f;

        [Foldout("Rayleigh散射")]
        public Color RayleighScattering = new Color(0.005802f / 0.033100f, 0.013558f / 0.033100f, 1f);
        public float RayleighScatteringScale = 0.033100f;
        public float RayleighExponentialDistribution = 8.0f;

        [Foldout("Mie散射")]
        public Color MieScattering = Color.white;
        public float MieScatteringScale = 0.003996f;
        public Color MieAbsorption = Color.white;
        public float MieAbsorptionScale = 0.000444f;
        public float MieAnisotropy = 0.8f;
        public float MieExponentialDistribution = 1.2f;

        [Foldout("其他吸收")]
        public Color OtherAbsorption = new Color(0.000650f / 0.001881f, 1f, 0.000085f / 0.001881f);
        public float OtherAbsorptionScale = 0.001881f;
        public float OtherTentDistribution_TipAltitude = 25.0f;
        public float OtherTentDistribution_TipValue = 1.0f;
        public float OtherTentDistribution_Width = 15.0f;

        [Foldout("空中透视")]
        public float AerialPespectiveViewDistanceScale = 1.0f;
        public float AerialPerspectiveStartDepth = 0.1f;

        [Foldout("其他")]
        public float MultiScatteringFactor = 1.0f;
        public Color SkyLuminanceFactor = Color.white;
        public Color GroundAlbedo = new Color(0.4f, 0.4f, 0.4f);

        public float AmbientSkyIntensity = 1.0f;

        public bool UseFastSky = true;
        public bool UseFastAerialPespective = true;
        public bool ForceRayMarching = false;
        public bool RenderLightDisk = true;

        // Start is called before the first frame update
        void Start()
        {
           
        }

        // UpdateByDefault is called once per frame
        void Update()
        {

        }
    }

}
