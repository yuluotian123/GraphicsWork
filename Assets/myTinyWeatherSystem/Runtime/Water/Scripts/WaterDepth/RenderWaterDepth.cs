using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SocialPlatforms;
using UnityEngine.UIElements;

namespace Yu_Weather
{
    [ExecuteInEditMode]
    public class RenderWaterDepth : MonoBehaviour
    {
        public bool perCamDepth = false;
        public float maxDepth = 40.0f;
        public float CamSize = 128;
        public float depthExtra = 40.0f;
        public float meshLength = 512.0f;

        //ComputeDepthPerCam
        public Camera depthCam;
        public RenderTexture depthTexture;
        public bool bRenderedSceneDepth = false;
        public bool bRenderedCameraDepth = false;


        private void OnEnable()
        {
            RenderPipelineManager.beginCameraRendering += onCaptureDepthMap;
        }

        // Start is called before the first frame update
        private void Start()
        {
        }

        private void OnDestroy()
        {
            Cleanup();
            Debug.Log("Destroy");
        }

        private void OnDisable()
        {
            Cleanup();
            Debug.Log("Disable");
        }

        private void LateUpdate()
        {
            if (!perCamDepth && bRenderedCameraDepth)
            {
                bRenderedSceneDepth = false;
                bRenderedCameraDepth = false;
            }
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
                depthTexture.Release();
                SafeDestroy(depthTexture);
            }

        }

        private void onCaptureDepthMap(ScriptableRenderContext context, Camera camera)
        {
            if (!perCamDepth&& bRenderedSceneDepth) return;

            if (camera.cameraType == CameraType.Reflection || camera.cameraType == CameraType.Preview)
                return;
            if (!perCamDepth)
            {
                RenderDepthForCam(context);
                bRenderedSceneDepth = true;
            }
            else
            {
                RenderDepthForCam(context, camera);
                bRenderedCameraDepth = true;
            }
        }

        private void RenderDepthForCam(ScriptableRenderContext context, Camera cam = null)
        {
            GenerateDepthCam(cam);

            //Generate RT
            GenerateDepthRT(cam);

            UniversalRenderPipeline.RenderSingleCamera(context, depthCam);

            depthCam.enabled = false;
            depthCam.targetTexture = null;

            SetDepthCamShaderVariables();
        }

        private void GenerateDepthCam(Camera cam = null)
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

            depthCam.orthographic = true;
            depthCam.nearClipPlane = 0.01f;
            depthCam.farClipPlane = maxDepth + depthExtra;
            depthCam.allowHDR = false;
            depthCam.allowMSAA = false;
            //只渲染指定层
            depthCam.cullingMask = (1 << LayerMask.NameToLayer("Objects"));

            if (cam != null)
            {
                depthCam.orthographicSize = CamSize;
                depthCam.transform.position = new Vector3(cam.transform.position.x, transform.position.y + depthExtra, cam.transform.position.z);
                depthCam.transform.up = Vector3.forward;
                depthCam.cameraType = cam.cameraType;
            }
            else
            {
                depthCam.orthographicSize = meshLength / 2;
                var p = transform.position;
                p.y += depthExtra;
                depthCam.transform.position = p;
                depthCam.transform.up = Vector3.forward;
            }
        }
        private void GenerateDepthRT(Camera cam = null)
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
        private void SetDepthCamShaderVariables()
        {
            Shader.SetGlobalTexture(_WaterDepthTexture, depthTexture);
            var Params = new Vector4(depthCam.transform.position.x, depthCam.transform.position.y, depthCam.transform.position.z, depthCam.orthographicSize);
            Shader.SetGlobalVector(_WaterDepthParams, Params);
        }

        private static void SafeDestroy(Object o)
        {
            if (Application.isPlaying)
                Destroy(o);
            else
                DestroyImmediate(o);
        }

        static readonly int _WaterDepthTexture = Shader.PropertyToID("_WaterDepthTexture");
        static readonly int _WaterDepthParams = Shader.PropertyToID("_WaterDepthParams");
    }
}
