using System.Collections;
using System.Collections.Generic;
using UnityEditor.Rendering.LookDev;
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
        [HideInInspector]
        //Planar Reflection
        public PlanarReflection planarReflection;

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
        public Color BaseColor = new Color(.54f, .95f, .99f, 1);
        public Color shallowColor = new Color(.10f, .4f, .43f, 1);
        public bool perCamDepth = false;
        public float maxDepth = 40.0f;
        public float CamSize = 128;
        public float depth = 1f;
        [Header("SSS")]
        public Color SSSColor = new Color(1f, 1f, 1f, 1f);
        public float sssScale = 1f;

        [Foldout("水面光照")]
        public float transparency = 0.5f;
        public float shadow = 0.35f;
        public float shinness = 32f;

        [Foldout("近岸海浪")]
        public bool nearShore = false;

        [HideInInspector]
        public float time = 0;
        [HideInInspector]
        public bool hasGeneratedGaussianRT = false;

        //ComputeDepthPerCam
        private Camera depthCam;
        public RenderTexture depthTexture;
        private bool PreUseCamDepth = false;

        private void OnEnable()
        {
            RenderPipelineManager.beginCameraRendering += onCaptureDepthMap;
        }

        // Start is called before the first frame update
        private void Start()
        {
            //让WaterData去生成！至于为什么，小编也很疑惑
            mesh = null;
            hasGeneratedGaussianRT = false;

            planarReflection = GetComponent<PlanarReflection>();

            if (!perCamDepth)
                Invoke(nameof(RenderDepth), 1.0f);
        }

        private void OnDestroy()
        {
            Cleanup();
        }

        private void OnDisable()
        {
            Cleanup();
        }

        private void Cleanup()
        {
            RenderPipelineManager.beginCameraRendering -= onCaptureDepthMap;

            if (depthCam)
            {
                // 释放相机
                depthCam.targetTexture = null;
                SafeDestroy(depthCam.gameObject);
            }
            if (depthTexture)
            {
                // 释放纹理
                SafeDestroy(depthTexture);
            }

        }

        private void onCaptureDepthMap(ScriptableRenderContext context, Camera camera)
        {
            if (!perCamDepth) return;

            if (camera.cameraType == CameraType.Reflection || camera.cameraType == CameraType.Preview)
                return;

            RenderDepthForCam(context,camera);

            PreUseCamDepth = true;
        }

        public void RenderDepth()
        {
            GenerateDepthCam(); 

            GenerateDepthRT();

            depthCam.Render();

            depthCam.enabled = false;
            depthCam.targetTexture = null;

            Shader.SetGlobalTexture(_WaterDepthTexture, depthTexture);
            var Params = new Vector4(depthCam.transform.position.x, depthCam.transform.position.y, depthCam.transform.position.z, meshLength / 2);
            Shader.SetGlobalVector(_WaterDepthParams, Params);
        }
        public void RenderDepthForCam(ScriptableRenderContext context, Camera cam)
        {
            GenerateDepthCam(cam);

            //Generate RT
            GenerateDepthRT();

            UniversalRenderPipeline.RenderSingleCamera(context, depthCam);

            depthCam.enabled = false;
            depthCam.targetTexture = null;

            Shader.SetGlobalTexture(_WaterDepthTexture, depthTexture);
            var Params = new Vector4(depthCam.transform.position.x, depthCam.transform.position.y, depthCam.transform.position.z,CamSize);
            Shader.SetGlobalVector(_WaterDepthParams,Params);
        }

        public void GenerateDepthCam(Camera cam = null)
        {
            if (depthCam == null)
            {
                var go =
                    new GameObject("depthCamera") { hideFlags = HideFlags.HideAndDontSave }; //create the cameraObject
                depthCam = go.AddComponent<Camera>();
            }

            var additionalCamData = depthCam.GetUniversalAdditionalCameraData();
            additionalCamData.renderShadows = false;
            additionalCamData.renderPostProcessing = false;
            additionalCamData.requiresColorOption = CameraOverrideOption.Off;
            additionalCamData.requiresDepthOption = CameraOverrideOption.Off;

            var depthExtra = 4.0f;
            depthCam.orthographic = true;
            depthCam.nearClipPlane = 0.01f;
            depthCam.farClipPlane = maxDepth + depthExtra;
            depthCam.allowHDR = false;
            depthCam.allowMSAA = false;
            //只渲染指定层
            depthCam.cullingMask = (1 << LayerMask.NameToLayer("Objects"));

            if(cam != null)
            {
                depthCam.orthographicSize = CamSize;
                depthCam.transform.position = new Vector3(cam.transform.position.x, transform.position.y + depthExtra, cam.transform.position.z);
                depthCam.transform.up = Vector3.forward;
            }
            else
            {
                depthCam.orthographicSize = meshLength / 2;
                depthCam.transform.position = Vector3.up * (transform.position.y + depthExtra);
                depthCam.transform.up = Vector3.forward;
            }
        }

        public void GenerateDepthRT()
        {
            //Generate RT
            if (!depthTexture)
                depthTexture = new RenderTexture(1024, 1024, 24, RenderTextureFormat.Depth, RenderTextureReadWrite.Linear);
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.OpenGLES2 || SystemInfo.graphicsDeviceType == GraphicsDeviceType.OpenGLES3)
            {
                depthTexture.filterMode = FilterMode.Point;
            }
          
            depthTexture.wrapMode = TextureWrapMode.Clamp;
            depthTexture.name = "WaterDepthMap";

            depthCam.targetTexture = depthTexture;
        }

        private static void SafeDestroy(Object o)
        {
            if (Application.isPlaying)
                Destroy(o);
            else
                DestroyImmediate(o);
        }

        // Update is called once per frame
        private void Update()
        {
            time += Time.deltaTime * timeScale;

            UpdateReflection();
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

        private void ReUpdateWaterSceneDepth()
        {
            if (!perCamDepth && PreUseCamDepth)
                RenderDepth();

            PreUseCamDepth = false;
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

        static readonly int _WaterDepthTexture = Shader.PropertyToID("_WaterDepthTexture");
        static readonly int _WaterDepthParams = Shader.PropertyToID("_WaterDepthParams");
    }

}
