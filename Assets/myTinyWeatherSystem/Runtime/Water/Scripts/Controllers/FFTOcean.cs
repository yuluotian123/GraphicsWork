using Mono.Cecil;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Yu_Weather
{
    public enum ReflectionMode
    {
        PLANAR,
        REFLECTPROBE,
        SSR_TO_DO
    }
    /// <summary>
    /// 平面反射和Depth都是写在FFTOcean里的，而没有写在RenderFeature里，主要是为了方便（×）
    /// 是因为平面反射和顶视角深度相机具有复用性，但又不值得我为他们单独开个RenderFeature
    /// </summary>
    [ExecuteInEditMode]
    public class FFTOcean : MonoBehaviour
    {
        [Foldout("网格设定")]
        [Tooltip("还没做，可能是用后处理做")]
        public bool Infinite = false;
        public Mesh mesh = null;
        public int fftPow = 9;         //生成海洋纹理大小 2的次幂，例 为10时，纹理大小为1024*1024
        public int meshSize = 150;      //网格长宽数量
        public float meshLength = 512;   //网格长度


        [Foldout("曲面细分设定")]
        public float tess = 7;
        public float minDist = 0;
        public float maxDist = 60;

        [Foldout("水面模拟")]
        public float lambda = 70;  //偏移量
        public float amplitude = 10;          //phillips谱参数，影响波浪高度    
        public float heightScale = 28.8f;   //高度影响
        public float bubblesScale = 1f;  //泡沫强度
        public float bubblesThreshold = 0.86f;//泡沫阈值
        public Vector2 windDirection = new Vector2(1f,3f);//风的方向
        public float windScale = 30f;
        public float timeScale = 2f;

        [Foldout("水面反射和折射")]
        public ReflectionMode reflectionMode = ReflectionMode.REFLECTPROBE;

        [Header("反射指针")]
        public ReflectionProbe reflectionProbe = null;
        public Transform target = null;

        [Header("平面反射")]
        public float PlaneOffset = 0.0f;
        public bool PlanarShadow = false;
        public bool PlanarPostProcessing = false;

        [Header("反射和折射")]
        public float fade = 0.002f;
        public float reflect = 10f;
        public float refract = 0.05f;
        public float fresnel = 0.04f;
        public float normalStrength = 4f;
        public float normalBias = 3f;

        [Foldout("水面颜色")]
        [Header("基础颜色")]
        public Texture2D defaultRampTex;
        public Gradient absorptionRamp;
        public Gradient scatterRamp;
        [Header("深度设置")]
        public bool perCamDepth = false;
        public float maxDepth = 40.0f;
        public float CamSize = 128;
        public float depthExtra = 40.0f;
        [Header("SSS")]
        public Color SSSColor = new Color(1f, 1f, 1f, 1f);
        public float sssScale = 1f;
        [Header("焦散")]
        public Texture2D CausticsTexture;

        [Foldout("水面光照")]
        public float sssPow = 0.5f;
        public float shadow = 0.35f;
        public float shinness = 32f;

        [Foldout("近岸海浪")]
        public bool nearShore = false;

        [HideInInspector]
        public float time = 0;
        [HideInInspector]
        public bool hasGeneratedGaussianRT = false;

        [HideInInspector]
        //Planar Reflection
        public PlanarReflection planarReflection;
        //ComputeDepthPerCam
        [HideInInspector]
        public RenderWaterDepth renderWaterDepth;


        private void Awake()
        {
            //让WaterData去生成！至于为什么，小编也很疑惑
            mesh = null;
            hasGeneratedGaussianRT = false;

            if (defaultRampTex == null)
                defaultRampTex = Resources.Load("Textures/Water/DefaultFoamRamp") as Texture2D;
            if(CausticsTexture == null)
                CausticsTexture = Resources.Load("Textures/Water/CausticsTexture") as Texture2D;
        }

        // Start is called before the first frame update
        private void Start()
        {
            planarReflection = GetComponent<PlanarReflection>();
            renderWaterDepth = GetComponent<RenderWaterDepth>();
            SetupRenderWaterDepth();
        }

        private void Update()
        {
            time += Time.deltaTime * timeScale;

            UpdateReflection();
            UpdateRenderWaterDepth();
        }

        private void UpdateReflection()
        {
            if (planarReflection == null)
                planarReflection = GetComponent<PlanarReflection>();

            if (reflectionMode != ReflectionMode.PLANAR)
                planarReflection.enabled = false;
            else
            {
                if (!planarReflection)
                {
                    planarReflection.enabled = false;
                    reflectionMode = ReflectionMode.REFLECTPROBE;
                }
                else
                {
                    if (reflectionProbe)
                        reflectionProbe.enabled = false;

                    if (!planarReflection.enabled)
                        planarReflection.enabled = true;

                    SetupPlanarReflection();
                }

            }

            if (reflectionProbe && target && reflectionMode == ReflectionMode.REFLECTPROBE)
            {
                if (!reflectionProbe.gameObject.activeSelf)
                    reflectionProbe.gameObject.SetActive(true);
                if (!reflectionProbe.enabled)
                    reflectionProbe.enabled = true;

                float Height = transform.position.y;
                var pos = target.position;
                pos.y = Height - (target.position.y - Height);
                reflectionProbe.transform.position = pos;
            }
        }
        private void UpdateRenderWaterDepth()
        {
            SetupRenderWaterDepth();
        }

        private void SetupPlanarReflection()
        {
            planarReflection.m_settings.m_Shadows = PlanarShadow;
            planarReflection.m_settings.m_postProcess = PlanarPostProcessing;
            planarReflection.m_planeOffset = PlaneOffset;
            planarReflection.m_settings.m_ClipPlaneOffset = 0.01f;
            planarReflection.targetPlane = this.gameObject;
            planarReflection.m_settings.m_ReflectLayers = ~(1 << LayerMask.NameToLayer("UI"));
            planarReflection.m_settings.m_ReflectLayers &= ~(1 << LayerMask.NameToLayer("Water"));
            //planarReflection.m_settings.m_ReflectLayers &= ~(1 << LayerMask.NameToLayer("Grass"));
        }
        private void SetupRenderWaterDepth()
        {
            renderWaterDepth.CamSize = CamSize;
            renderWaterDepth.depthExtra = depthExtra;
            renderWaterDepth.maxDepth = maxDepth;
            renderWaterDepth.meshLength = meshLength;
            renderWaterDepth.perCamDepth = perCamDepth;
        }
    }

}
