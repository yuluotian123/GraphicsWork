using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Yu_Weather
{
    /// <summary>
    /// AtmosphereLight就是用来作为大气光源的登西
    /// 他用来模拟天体，比如太阳或者月亮对于大气和场景的影响（可选）
    /// 最多支持两个大气光源（因为多了也不渲染）
    /// 因为我的初衷是不更改URP管线中的源码
    /// </summary>
    [ExecuteInEditMode]
    public class AtmosphereLight : MonoBehaviour
    {
        [Tooltip("可以通过将一个Directional Light赋予给大气光源来使Atmosphere Light影响场景")]
        public bool affectScene = true;
        public Light dirLight = null;

        [Tooltip("lightIndex最小的大气光源为大气主光源,默认只渲染index最小的光源")]
        public uint lightIndex = 1;

        [Header("天体")]
        [Tooltip("是否渲染天体")]
        public bool renderDisk = true;
        [Tooltip("天体材质")]
        public Texture2D diskTexture = null;
        [Tooltip("天体颜色")]
        public Color diskColor = Color.white;
        [Tooltip("天体亮度")]
        public float diskIntensity = 1.0f;
        [Tooltip("天体极角")]
        public float lightSourceAngle = 0.54f;

        [Header("光照")]
        [Tooltip("平行光颜色")]
        public Color outSpaceColor = Color.white;
        [Tooltip("平行光亮度")]
        public float lightIntensity = 1.0f;
        [Tooltip("光线大气衰减")]
        public Color lightTransTransmittance = Color.white;

        [Header("体积云阴影（只有主光源能投射地面阴影，另一个的只会影响大气（to do））")]
        public bool useCloudShadow = false;
        public int shadowResolution = 512;
        public float shadowCloudStrength = 1.0f;    

        [HideInInspector]
        public Color lightIlluminanceOnGround;

        public Color GetLightOutSpaceIlluminance()
        {
            return outSpaceColor * lightIntensity;
        }
        public Color GetLightDiskColorScale()
        {
            return diskColor * diskIntensity;
        }

        public float GetLightHalfApexAngleRadian()
        {
            return 0.5f * lightSourceAngle * Mathf.Deg2Rad; 
        }

        public Color GetLightDiskOutSpaceLuminance()
        {
            float SunSoildAngle = 2.0f*Mathf.PI*(1.0f-Mathf.Cos(GetLightHalfApexAngleRadian()));
            return GetLightDiskColorScale() * GetLightOutSpaceIlluminance() / SunSoildAngle;
        }

        public Matrix4x4 GetLocalToWorldMatrix()
        {
            return transform.localToWorldMatrix;
        }
        public Vector4 GetLightDirection() 
        {
            return GetLocalToWorldMatrix().GetColumn(2);
        }
        public Vector4 GetLightRight()
        {
            return GetLocalToWorldMatrix().GetColumn(0);
        }

        public Vector4 GetLightUp()
        {
            return GetLocalToWorldMatrix().GetColumn(1);
        }

        private void Awake()
        {
            
        }

        private void Update()
        {
            if (affectScene && dirLight && dirLight.type==LightType.Directional&&lightIndex==0)
            {
                dirLight.color = lightIlluminanceOnGround;
                dirLight.transform.rotation = transform.rotation;
            }
        }

    }

}
