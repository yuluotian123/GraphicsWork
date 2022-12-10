using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Yu_Weather
{
    /// <summary>
    /// AtmosphereLight����������Ϊ������Դ�ĵ���
    /// ������ģ�����壬����̫�������������ڴ����ͳ�����Ӱ�죨��ѡ��
    /// ���֧������������Դ����Ϊ����Ҳ����Ⱦ��
    /// ��Ϊ�ҵĳ����ǲ�����URP�����е�Դ��
    /// </summary>
    [ExecuteInEditMode]
    public class AtmosphereLight : MonoBehaviour
    {
        [Tooltip("����ͨ����һ��Directional Light�����������Դ��ʹAtmosphere LightӰ�쳡��")]
        public bool affectScene = true;
        public Light dirLight = null;

        [Tooltip("lightIndex��С�Ĵ�����ԴΪ��������Դ,Ĭ��ֻ��Ⱦindex��С�Ĺ�Դ")]
        public uint lightIndex = 1;

        [Header("����")]
        [Tooltip("�Ƿ���Ⱦ����")]
        public bool renderDisk = true;
        [Tooltip("�������")]
        public Texture2D diskTexture = null;
        [Tooltip("������ɫ")]
        public Color diskColor = Color.white;
        [Tooltip("��������")]
        public float diskIntensity = 1.0f;
        [Tooltip("���弫��")]
        public float lightSourceAngle = 0.54f;

        [Header("����")]
        [Tooltip("ƽ�й���ɫ")]
        public Color outSpaceColor = Color.white;
        [Tooltip("ƽ�й�����")]
        public float lightIntensity = 1.0f;
        [Tooltip("���ߴ���˥��")]
        public Color lightTransTransmittance = Color.white;

        [Header("�������Ӱ��ֻ������Դ��Ͷ�������Ӱ����һ����ֻ��Ӱ�������to do����")]
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
