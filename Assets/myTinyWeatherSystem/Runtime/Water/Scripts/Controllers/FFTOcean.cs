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
    /// ƽ�淴���Depth����д��FFTOcean��ģ���û��д��RenderFeature���Ҫ��Ϊ�˷��㣨����
    /// ����Ϊƽ�淴��Ͷ��ӽ����������и����ԣ����ֲ�ֵ����Ϊ���ǵ�������RenderFeature
    /// </summary>
    [ExecuteInEditMode]
    public class FFTOcean : MonoBehaviour
    {
        [Foldout("�����趨")]
        [Tooltip("��û�����������ú�����")]
        public bool Infinite = false;
        public Mesh mesh = null;
        public int fftPow = 9;         //���ɺ��������С 2�Ĵ��ݣ��� Ϊ10ʱ�������СΪ1024*1024
        public int meshSize = 150;      //���񳤿�����
        public float meshLength = 512;   //���񳤶�


        [Foldout("����ϸ���趨")]
        public float tess = 7;
        public float minDist = 0;
        public float maxDist = 60;

        [Foldout("ˮ��ģ��")]
        public float lambda = 70;  //ƫ����
        public float amplitude = 10;          //phillips�ײ�����Ӱ�첨�˸߶�    
        public float heightScale = 28.8f;   //�߶�Ӱ��
        public float bubblesScale = 1f;  //��ĭǿ��
        public float bubblesThreshold = 0.86f;//��ĭ��ֵ
        public Vector2 windDirection = new Vector2(1f,3f);//��ķ���
        public float windScale = 30f;
        public float timeScale = 2f;

        [Foldout("ˮ�淴�������")]
        public ReflectionMode reflectionMode = ReflectionMode.REFLECTPROBE;

        [Header("����ָ��")]
        public ReflectionProbe reflectionProbe = null;
        public Transform target = null;

        [Header("ƽ�淴��")]
        public float PlaneOffset = 0.0f;
        public bool PlanarShadow = false;
        public bool PlanarPostProcessing = false;

        [Header("���������")]
        public float fade = 0.002f;
        public float reflect = 10f;
        public float refract = 0.05f;
        public float fresnel = 0.04f;
        public float normalStrength = 4f;
        public float normalBias = 3f;

        [Foldout("ˮ����ɫ")]
        [Header("������ɫ")]
        public Texture2D defaultRampTex;
        public Gradient absorptionRamp;
        public Gradient scatterRamp;
        [Header("�������")]
        public bool perCamDepth = false;
        public float maxDepth = 40.0f;
        public float CamSize = 128;
        public float depthExtra = 40.0f;
        [Header("SSS")]
        public Color SSSColor = new Color(1f, 1f, 1f, 1f);
        public float sssScale = 1f;
        [Header("��ɢ")]
        public Texture2D CausticsTexture;

        [Foldout("ˮ�����")]
        public float sssPow = 0.5f;
        public float shadow = 0.35f;
        public float shinness = 32f;

        [Foldout("��������")]
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
            //��WaterDataȥ���ɣ�����Ϊʲô��С��Ҳ���ɻ�
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
