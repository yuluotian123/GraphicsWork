using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

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
        public int fftPow = 10;         //生成海洋纹理大小 2的次幂，例 为10时，纹理大小为1024*1024
        public int meshSize = 250;      //网格长宽数量
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
        public ReflectionProbe reflectionProbe = null;
        public Transform target = null;
        public float fade = 0.002f;
        public float reflect = 10f;
        public float refract = 0.05f;
        public float fresnel = 0.04f;
        public float normalStrength = 4f;
        public float normalBias = 3f;

        [Foldout("水面颜色")]
        public Color BaseColor = new Color(.54f, .95f, .99f, 1);
        public Color shallowColor = new Color(.10f, .4f, .43f, 1);
        public float shallowDepth = 5.0f;
        public float depth = 1f;

        [Foldout("水面光照")]
        public Color SSSColor = new Color(1f,1f,1f,1f);
        public float transparency = 0.5f;
        public float shadow = 0.35f;
        public float shinness = 32f;
        public float sssScale = 1f;


        [Foldout("近岸海浪（to do）")]
        public bool nearShore = false;

        [HideInInspector]
        public float time = 0;
        [HideInInspector]
        public bool hasGeneratedGaussianRT = false;

        //Planar Reflection
        public PlanarReflection planarReflection;

        //Debug
        private MeshCollider m_collider;

        // Start is called before the first frame update
        void Start()
        {
            //让WaterData去生成！至于为什么，小编也很疑惑
            mesh = null;
            hasGeneratedGaussianRT = false;

            planarReflection = GetComponent<PlanarReflection>();
            m_collider = GetComponent<MeshCollider>();
        }

        // Update is called once per frame
        void Update()
        {
            time += Time.deltaTime * timeScale;

            UpdateReflection();

            if (m_collider)
                m_collider.sharedMesh = mesh;
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

        private void SetupPlanarReflection()
        {
            planarReflection.m_settings.m_Shadows = false;
            planarReflection.m_planeOffset = 1;
            planarReflection.targetPlane = this.gameObject;
            planarReflection.m_settings.m_ClipPlaneOffset = -0.1f;
            planarReflection.m_settings.m_ReflectLayers = ~(1 << LayerMask.NameToLayer("UI"));
            planarReflection.m_settings.m_ReflectLayers &= ~(1 << LayerMask.NameToLayer("Water"));
            planarReflection.m_settings.m_ReflectLayers &= ~(1 << LayerMask.NameToLayer("Grass"));
        }
    }


}
