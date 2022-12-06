using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Yu_Weather
{
    class ComputeLutsPass : ScriptableRenderPass
    {
        /// <summary>
        /// 使用了通过RenderDoc扒到的数据
        /// </summary>
        struct SphereVetor
        {
            public float x;
            public float y;
            public float z;
            public float w;
        }
        static double[] testData =  {
0.47778,  0.3282,  0.81487,  0.00
,  0.86046,  0.04313,  0.50768,  0.00
,  0.86619,  0.32325,  0.38107,  0.00
,  0.92988,  0.36688,  0.02682,  0.00
,  0.98682,  0.13566, -0.08825,  0.00
,  0.92624,  0.05843, -0.37237,  0.00
,  0.77975,  0.14644, -0.60872,  0.00
,  0.29193,  0.04189, -0.95552,  0.00
,  0.03684,  0.1977,  0.97957,  0.00
,  0.16067,  0.65769,  0.73595,  0.00
,  0.45193,  0.84597,  0.28302,  0.00
,  0.67033,  0.7061,  0.22819,  0.00
,  0.44871,  0.87189, -0.19612,  0.00
,  0.22475,  0.89621, -0.38249,  0.00
,  0.22558,  0.76872, -0.59848,  0.00
,  0.06579,  0.14871, -0.98669,  0.00
, -0.3813,  0.45931,  0.80228,  0.00
, -0.45008,  0.50893,  0.73377,  0.00
, -0.54338,  0.67604,  0.4977,  0.00
, -0.09091,  0.99159,  0.0921,  0.00
, -0.45068,  0.88589, -0.10997,  0.00
, -0.50254,  0.78582, -0.36048,  0.00
, -0.2647,  0.8014, -0.53637,  0.00
, -0.02099,  0.45625, -0.8896,  0.00
, -0.30121,  0.27401,  0.91334,  0.00
, -0.80374,  0.27383,  0.52822,  0.00
, -0.84571,  0.19738,  0.4958,  0.00
, -0.9325,  0.35225,  0.07974,  0.00
, -0.96532,  0.2481, -0.08129,  0.00
, -0.72387,  0.50171, -0.47361,  0.00
, -0.69586,  0.11719, -0.70855,  0.00
, -0.57862,  0.26181, -0.77244,  0.00
, -0.40183, -0.16196,  0.90128,  0.00
, -0.61404, -0.49068,  0.61822,  0.00
, -0.81284, -0.4591,  0.3585,  0.00
, -0.99121, -0.12964,  0.02643,  0.00
, -0.98515, -0.14046, -0.09871,  0.00
, -0.66522, -0.65269, -0.36261,  0.00
, -0.64749, -0.19774, -0.73597,  0.00
, -0.50887, -0.27864, -0.8145,  0.00
, -0.24011, -0.38839,  0.88966,  0.00
, -0.44003, -0.57226,  0.69202,  0.00
, -0.37811, -0.84161,  0.38565,  0.00
, -0.60524, -0.78355,  0.14046,  0.00
, -0.19452, -0.94887, -0.24861,  0.00
, -0.56046, -0.74505, -0.36166,  0.00
, -0.03089, -0.75608, -0.65375,  0.00
, -0.20537, -0.21415, -0.95497,  0.00
,  0.19314, -0.55179,  0.81131,  0.00
,  0.39453, -0.63194,  0.66708,  0.00
,  0.11647, -0.90857,  0.40118,  0.00
,  0.67573, -0.73341,  0.07415,  0.00
,  0.53887, -0.83254, -0.12847,  0.00
,  0.46548, -0.79147, -0.39611,  0.00
,  0.28962, -0.78804, -0.54324,  0.00
,  0.10689, -0.64189, -0.75931,  0.00
,  0.50264, -0.32378,  0.80157,  0.00
,  0.58862, -0.35983,  0.72391,  0.00
,  0.66333, -0.6141,  0.42764,  0.00
,  0.85308, -0.46384,  0.23895,  0.00
,  0.94241, -0.3033, -0.14099,  0.00
,  0.88032, -0.27571, -0.38602,  0.00
,  0.82202, -0.16523, -0.54496,  0.00
,  0.60714, -0.06858, -0.79163,  0.00 };

        ComputeShader lookUpTableCS;
        private int transmittanceKernel;
        private int multiScatteredLuminanceKernel;
        private int distantSkyLightKernel;
        private int skyViewLutKernel;
        private int aerialPerspectiveLutKernel;

        ComputeBuffer sphereSamplesBuffer;
        public const int BufferSize = 8;

        Texture2D skyColor;

        public ComputeLutsPass(ComputeShader shader)
        {
            //找到compute shader
            lookUpTableCS = shader;

            //球面采样Buffer
            SphereVetor[] bufferDatas = new SphereVetor[BufferSize * BufferSize];
            Random.InitState(1999+11+24);
            for (int i = 0;i< BufferSize;i++)
            {
                for (int j = 0; j < BufferSize; ++j)
                {
                    int idx = j * BufferSize + i;
                    //float u0 = ((float)i+ Random.Range(0f,1f)) * 1/ (float)BufferSize;
                    //float u1 = ((float)j + Random.Range(0f, 1f)) * 1 / (float)BufferSize;
                    //float a = 1.0f - 2.0f * u0;
                    //float b = Mathf.Sqrt(1.0f - a * a);
                    //float phi = 2 * Mathf.PI * u1;

                    //bufferDatas[idx].x = b * Mathf.Cos(phi);
                    //bufferDatas[idx].y = a;
                    //bufferDatas[idx].z = -b * Mathf.Sin(phi);
                    //bufferDatas[idx].w = 0;

                    //for test,the same as UE4
                    int length = idx * 4;
                    bufferDatas[idx].x = (float)testData[length];
                    bufferDatas[idx].y = (float)testData[length + 2];
                    bufferDatas[idx].z = -(float)testData[length + 1];
                    bufferDatas[idx].w = (float)testData[length + 3];
                }
            }
            sphereSamplesBuffer = new ComputeBuffer(BufferSize * BufferSize, 16);
            sphereSamplesBuffer.SetData(bufferDatas);

            //获取环境光颜色（需要借助一张纹理）
            skyColor = new Texture2D(1, 1);

            //获取Kernel
            transmittanceKernel = lookUpTableCS.FindKernel("TransmittanceLut");
            multiScatteredLuminanceKernel = lookUpTableCS.FindKernel("MultiScatteringLut");
            distantSkyLightKernel = lookUpTableCS.FindKernel("DistantSkyLightLut");
            skyViewLutKernel = lookUpTableCS.FindKernel("SkyViewLut");
            aerialPerspectiveLutKernel = lookUpTableCS.FindKernel("AerialPerspectiveLut");
        }

        /// <summary>
        /// 每帧渲染Luts 接收传入的数据并对其进行处理
        /// </summary>
        /// <param name="cmd"></param>
        /// <param name="context"></param>
        /// <param name="sceneSkyInfo"></param>
        public void RenderPerFrameLuts(CommandBuffer cmd, ScriptableRenderContext context, ref SceneSkyInfo sceneSkyInfo)
        {
            //Transmittance Lut(cost 0.05ms on RenderDoc)
            {
                RenderTextureDescriptor transmittanceDescriptor =
                    new RenderTextureDescriptor((int)SkyConfig.TransmittanceLutWidth, (int)SkyConfig.TransmittanceLutHeight);
                transmittanceDescriptor.dimension = TextureDimension.Tex2D;
                transmittanceDescriptor.colorFormat = RenderTextureFormat.ARGBFloat;
                transmittanceDescriptor.enableRandomWrite = true;

                sceneSkyInfo.transmittanceLut = RenderTexture.GetTemporary(transmittanceDescriptor);
                lookUpTableCS.SetTexture(transmittanceKernel, _TransmittanceLut, sceneSkyInfo.transmittanceLut);
              
                lookUpTableCS.Dispatch(transmittanceKernel, (int)SkyConfig.TransmittanceLutWidth / 8, (int)SkyConfig.TransmittanceLutHeight / 8, 1);
            }

            //MultiScatteringLut(0.12ms,精度好像不太够，最后出来的数值范围好像有点问题)
            {
                RenderTextureDescriptor multiScatteringDescriptor =
                new RenderTextureDescriptor((int)SkyConfig.MultiScatteredLuminanceLutWidth, (int)SkyConfig.MultiScatteredLuminanceLutHeight);
                multiScatteringDescriptor.dimension = TextureDimension.Tex2D;
                multiScatteringDescriptor.colorFormat = RenderTextureFormat.RGB111110Float;
                multiScatteringDescriptor.enableRandomWrite = true;

                sceneSkyInfo.multiScatteredLuminanceLut = RenderTexture.GetTemporary(multiScatteringDescriptor);
                lookUpTableCS.SetTexture(multiScatteredLuminanceKernel, _MultiScatteredLuminanceLut, sceneSkyInfo.multiScatteredLuminanceLut);
                lookUpTableCS.SetTexture(multiScatteredLuminanceKernel, sceneSkyInfo._TransmittanceTexture, sceneSkyInfo.transmittanceLut);
                lookUpTableCS.SetBuffer(multiScatteredLuminanceKernel, _UniformSphereSamplesBuffer, sphereSamplesBuffer);

                lookUpTableCS.Dispatch(multiScatteredLuminanceKernel, (int)SkyConfig.MultiScatteredLuminanceLutWidth / 8, (int)SkyConfig.MultiScatteredLuminanceLutHeight / 8, 1);
            }

            //distantLightLut(大气环境光)
            {
                RenderTextureDescriptor distantSkyLightDescriptor =
               new RenderTextureDescriptor(1, 1);
                distantSkyLightDescriptor.dimension = TextureDimension.Tex2D;
                distantSkyLightDescriptor.colorFormat = RenderTextureFormat.RGB111110Float;
                distantSkyLightDescriptor.enableRandomWrite = true;

                sceneSkyInfo.distantSkyLightLut = RenderTexture.GetTemporary(distantSkyLightDescriptor);
                lookUpTableCS.SetTexture(distantSkyLightKernel, __DistantSkyLightLut, sceneSkyInfo.distantSkyLightLut);

                lookUpTableCS.SetTexture(distantSkyLightKernel, sceneSkyInfo._TransmittanceTexture, sceneSkyInfo.transmittanceLut);
                lookUpTableCS.SetTexture(distantSkyLightKernel, sceneSkyInfo._MultiScatteredLuminanceTexture, sceneSkyInfo.multiScatteredLuminanceLut);
                lookUpTableCS.SetBuffer(distantSkyLightKernel, _UniformSphereSamplesBuffer, sphereSamplesBuffer);
                lookUpTableCS.SetInt(_BufferSize, BufferSize);

                CoreUtils.SetKeyword(lookUpTableCS, "SECOND_ATMOSPHERE_LIGHT_ENABLED", sceneSkyInfo.bSecondLightEnabled);

                lookUpTableCS.Dispatch(distantSkyLightKernel, 1, 1, 1);

                CoreUtils.SetKeyword(lookUpTableCS, "SECOND_ATMOSPHERE_LIGHT_ENABLED", false);

                //设置环境光颜色
                if(skyColor == null) 
                    skyColor = new Texture2D(1, 1);
                RenderTexture.active = sceneSkyInfo.distantSkyLightLut;
                skyColor.ReadPixels(new Rect(0, 0, 1, 1), 0, 0);
                skyColor.Apply();
                RenderSettings.ambientSkyColor = skyColor.GetPixel(0, 0)* sceneSkyInfo.ambientSkyIntensity;
            }

            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
        }
        /// <summary>
        /// 环境光CubeMap,每帧渲染，还没做
        /// </summary>
        /// <param name="cmd"></param>
        /// <param name="context"></param>
        /// <param name="sceneSkyInfo"></param>
        /// <param name="viewSkyInfo"></param>
        public void RenderEnvironmentCubeMap(CommandBuffer cmd, ScriptableRenderContext context, ref SceneSkyInfo sceneSkyInfo,ref ViewSkyInfo viewSkyInfo)
        {
        }

        /// <summary>
        /// 最好将Render PerFrame 和 Render PerCam区分开，因为两者采用了不同的数据传递方式，而且之间的关系也不太直观
        /// </summary>
        private ViewSkyInfo m_viewSkyInfo;
        private SceneSkyInfo m_sceneSkyInfo;
        public void Setup(ref ViewSkyInfo viewSkyInfo,ref SceneSkyInfo sceneSkyInfo)
        {
            m_viewSkyInfo = viewSkyInfo;
            m_sceneSkyInfo = sceneSkyInfo;
        }
        /// <summary>
        /// Call Per Camera（在BeforeSkyBox）
        /// </summary>
        /// <param name="context"></param>
        /// <param name="renderingData"></param>
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer cmd = CommandBufferPool.Get("RenderPerCameraLuts");

            if (m_sceneSkyInfo.bSecondLightEnabled) Shader.EnableKeyword("SECOND_ATMOSPHERE_LIGHT_ENABLED");

            //SkyViewLut
            {
                RenderTextureDescriptor skyViewDescriptor =
                  new RenderTextureDescriptor((int)SkyConfig.FastSkyLUTWidth, (int)SkyConfig.FastSkyLUTHeight);
                skyViewDescriptor.dimension = TextureDimension.Tex2D;
                skyViewDescriptor.colorFormat = RenderTextureFormat.RGB111110Float;
                skyViewDescriptor.enableRandomWrite = true;

                m_viewSkyInfo.skyViewLut = RenderTexture.GetTemporary(skyViewDescriptor);
                lookUpTableCS.SetTexture(skyViewLutKernel, _SkyViewLut, m_viewSkyInfo.skyViewLut);
                lookUpTableCS.SetTexture(skyViewLutKernel, m_sceneSkyInfo._TransmittanceTexture, m_sceneSkyInfo.transmittanceLut);
                lookUpTableCS.SetTexture(skyViewLutKernel, m_sceneSkyInfo._MultiScatteredLuminanceTexture, m_sceneSkyInfo.multiScatteredLuminanceLut);

                lookUpTableCS.Dispatch(skyViewLutKernel, (int)SkyConfig.FastSkyLUTWidth / 8, (int)SkyConfig.FastSkyLUTHeight / 8, 1);
            }

            //CameraAtmosphereVolume
            {
                RenderTextureDescriptor avDescriptor =
                  new RenderTextureDescriptor((int)SkyConfig.AerialPerspectiveLUTWidth, (int)SkyConfig.AerialPerspectiveLUTWidth);
                avDescriptor.dimension = TextureDimension.Tex3D;
                avDescriptor.volumeDepth = (int)SkyConfig.AerialPerspectiveLUTDepthResolution;
                avDescriptor.colorFormat = RenderTextureFormat.ARGBHalf;
                avDescriptor.enableRandomWrite = true;

                m_viewSkyInfo.aerialPerspectiveLut = RenderTexture.GetTemporary(avDescriptor);
                lookUpTableCS.SetTexture(aerialPerspectiveLutKernel, _AerialPerspectiveLut, m_viewSkyInfo.aerialPerspectiveLut);
                lookUpTableCS.SetTexture(aerialPerspectiveLutKernel, m_sceneSkyInfo._TransmittanceTexture, m_sceneSkyInfo.transmittanceLut);
                lookUpTableCS.SetTexture(aerialPerspectiveLutKernel, m_sceneSkyInfo._MultiScatteredLuminanceTexture, m_sceneSkyInfo.multiScatteredLuminanceLut);
                lookUpTableCS.SetFloat(m_viewSkyInfo._APStartDepthKm, m_viewSkyInfo.AerialPerspectiveStartDepthKm); 
                lookUpTableCS.SetFloat(m_viewSkyInfo._AerialPespectiveViewDistanceScale, m_viewSkyInfo.AerialPespectiveViewDistanceScale);

                lookUpTableCS.Dispatch(aerialPerspectiveLutKernel, (int)SkyConfig.AerialPerspectiveLUTWidth / 4, (int)SkyConfig.AerialPerspectiveLUTWidth / 4, (int)SkyConfig.AerialPerspectiveLUTDepthResolution / 4);
            }

            Shader.DisableKeyword("SECOND_ATMOSPHERE_LIGHT_ENABLED");
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
        public void Dispose()
        {
            lookUpTableCS = null;
            skyColor = null;

            sphereSamplesBuffer.Release();
        }

        public readonly int _UniformSphereSamplesBuffer = Shader.PropertyToID("_UniformSphereSamplesBuffer");
        public readonly int _BufferSize = Shader.PropertyToID("_BufferSize");

        public readonly int _TransmittanceLut = Shader.PropertyToID("_TransmittanceLutRW");
        public readonly int _MultiScatteredLuminanceLut = Shader.PropertyToID("_MultiScatteredLuminanceLutRW");
        public readonly int __DistantSkyLightLut = Shader.PropertyToID("_DistantSkyLightLutRW");
        public readonly int _SkyViewLut = Shader.PropertyToID("_SkyAtmosphereViewLutRW");
        public readonly int _AerialPerspectiveLut = Shader.PropertyToID("_AerialPerspectiveLutRW");
    }

    class SkyAtmospherePass : ScriptableRenderPass
    {
        const float KM_TO_M = 1000.0f;
        const float M_TO_KM = 1 / KM_TO_M;

        Material skyMaterial;
        public SkyAtmospherePass(Shader skyAtmospherePS)
        {
            skyMaterial = CoreUtils.CreateEngineMaterial(skyAtmospherePS);
        }

        private SceneSkyInfo m_sceneSkyInfo;
        private ViewSkyInfo m_viewSkyInfo;
        public void Setup(ref SceneSkyInfo sceneSkyInfo,ref ViewSkyInfo viewSkyInfo)
        {
            m_sceneSkyInfo = sceneSkyInfo;
            m_viewSkyInfo = viewSkyInfo;
        }

        private RenderTargetHandle colorTemp;
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {

            CommandBuffer cmd = CommandBufferPool.Get("RenderSky");

            Vector3 ViewOrigin = m_viewSkyInfo.ViewOrigin;
            Vector3 PlanetCenter = m_viewSkyInfo.PlanetCenterKm * KM_TO_M;
            float TopOfAtmosphere = m_sceneSkyInfo.skyParameters._TopRadiusKm * KM_TO_M;
            const float PlanetRadiusTraceSafeEdgeCm = 1000.0f;
            bool ForceRayMarching = m_sceneSkyInfo.bRayMarching || ((Vector3.Distance(ViewOrigin, PlanetCenter) - TopOfAtmosphere - PlanetRadiusTraceSafeEdgeCm) > 0.0f);

            //SetKeyword
            {
                CoreUtils.SetKeyword(skyMaterial, "SOURCE_DISK_ENABLED", m_sceneSkyInfo.bLightDiskEnabled);
                CoreUtils.SetKeyword(skyMaterial, "FASTSKY_ENABLED", m_sceneSkyInfo.bFastSky && !ForceRayMarching);
                CoreUtils.SetKeyword(skyMaterial, "FASTAERIALPERSPECTIVE_ENABLED", m_sceneSkyInfo.bFastAerialPespective && !ForceRayMarching);
                CoreUtils.SetKeyword(skyMaterial, "SECOND_ATMOSPHERE_LIGHT_ENABLED", m_sceneSkyInfo.bSecondLightEnabled);
            }

            skyMaterial.SetTexture(m_sceneSkyInfo._TransmittanceTexture, m_sceneSkyInfo.transmittanceLut);
            skyMaterial.SetTexture(m_sceneSkyInfo._MultiScatteredLuminanceTexture, m_sceneSkyInfo.multiScatteredLuminanceLut);
            skyMaterial.SetTexture(m_viewSkyInfo._SkyViewTexture, m_viewSkyInfo.skyViewLut);
            skyMaterial.SetTexture(m_viewSkyInfo._AerialPerspectiveTexture, m_viewSkyInfo.aerialPerspectiveLut);
            skyMaterial.SetFloat(m_viewSkyInfo._APStartDepthKm, m_viewSkyInfo.AerialPerspectiveStartDepthKm);
            skyMaterial.SetFloat(m_viewSkyInfo._AerialPespectiveViewDistanceScale, m_viewSkyInfo.AerialPespectiveViewDistanceScale);


            RenderTextureDescriptor tempDescriptor = renderingData.cameraData.cameraTargetDescriptor;
            //设置深度缓冲区，0表示不需要深度缓冲区
            tempDescriptor.depthBufferBits = 0;
            cmd.GetTemporaryRT(colorTemp.id, tempDescriptor);

            //将源图像放入材质中计算，然后存储到临时缓冲区中
            cmd.Blit(m_viewSkyInfo.cameraColorTarget, colorTemp.Identifier(), skyMaterial);
            //将临时缓冲区的结果存回源图像中
            cmd.Blit(colorTemp.Identifier(), m_viewSkyInfo.cameraColorTarget);


            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
            cmd.ReleaseTemporaryRT(colorTemp.id);
        }

        public void Dispose()
        {
            CoreUtils.Destroy(skyMaterial);
        }
    }
}
