using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RendererUtils;
using UnityEngine.Rendering.Universal;

namespace Yu_Weather
{
    public class FFTOceanPass : ScriptableRenderPass
    {
        Material fftOceanMaterial;
        Material BlurMaterial;
        MaterialPropertyBlock BlurMaterialBlock;

        ComputeShader fftOceanCS;
        int Spectrumkernel;
        int FFTkernel;
        int Displacekernel;
        int NormalBubbleskernel;

        public RenderTexture TempTexture;

        public FFTOceanPass(ComputeShader _fftCS, Shader _fftPS)
        {
            fftOceanCS = _fftCS;
            fftOceanMaterial = CoreUtils.CreateEngineMaterial(_fftPS);
            BlurMaterial = CoreUtils.CreateEngineMaterial(Shader.Find("Yu_Weather/DualKawaseBlur"));

            Spectrumkernel = fftOceanCS.FindKernel("GenerateSpectrum");
            FFTkernel = fftOceanCS.FindKernel("ComputeFFT");
            Displacekernel = fftOceanCS.FindKernel("GenerateDisplace");
            NormalBubbleskernel = fftOceanCS.FindKernel("GenerateNormalAndBubbles");

        }

        public void UpdateWaterSurface(CommandBuffer cmd,ScriptableRenderContext context, ref WaterSurfaceDatas surfaceData,ref WaterRenderingDatas renderData)
        {
            int FFTSize = (int)surfaceData.surfaceParameters._FFTSize;
            int FFTPow = surfaceData.FFTPow;

            RenderTextureDescriptor spetrumDes = new RenderTextureDescriptor(FFTSize, FFTSize);
            spetrumDes.dimension = TextureDimension.Tex2D;
            spetrumDes.colorFormat = RenderTextureFormat.ARGBHalf;
            spetrumDes.enableRandomWrite = true;

            RenderTextureDescriptor textureDes = new RenderTextureDescriptor(FFTSize, FFTSize);
            textureDes.dimension = TextureDimension.Tex2D;
            textureDes.colorFormat = RenderTextureFormat.ARGBFloat;
            textureDes.enableRandomWrite = true;

            TempTexture = RenderTexture.GetTemporary(spetrumDes);

            //生成高度和偏移频谱
            {
                surfaceData.HeightSpectrumTexture = RenderTexture.GetTemporary(spetrumDes);
                surfaceData.DisplaceSpectrumTexture = RenderTexture.GetTemporary(spetrumDes);

                fftOceanCS.SetTexture(Spectrumkernel, surfaceData._GaussianTexture, surfaceData.GaussianTexture);
                fftOceanCS.SetTexture(Spectrumkernel, surfaceData._HeightSpectrumTexture, surfaceData.HeightSpectrumTexture);
                fftOceanCS.SetTexture(Spectrumkernel, surfaceData._DisplaceSpectrumTexture, surfaceData.DisplaceSpectrumTexture);
                fftOceanCS.Dispatch(Spectrumkernel, FFTSize / 8, FFTSize / 8, 1);
            }

            //进行FFT
            {   
                //进行横向FFT
                for (int m = 1; m <= FFTPow; m++)
                {
                    int ns = (int)Mathf.Pow(2, m - 1);
                    fftOceanCS.SetInt(Ns, ns);
                    if (m != FFTPow)
                    {
                        ComputeFFT(FFTSize,ref surfaceData.HeightSpectrumTexture, false, false, true);
                        ComputeFFT(FFTSize, ref surfaceData.DisplaceSpectrumTexture, false, true, true);
                    }
                    else
                    {
                        ComputeFFT(FFTSize, ref surfaceData.HeightSpectrumTexture, true, false, true);
                        ComputeFFT(FFTSize, ref surfaceData.DisplaceSpectrumTexture, true, true, true);
                    }
                }

                //进行纵向FFT
                for (int m = 1; m <= FFTPow; m++)
                {
                    int ns = (int)Mathf.Pow(2, m - 1);
                    fftOceanCS.SetInt(Ns, ns);
                    if (m != FFTPow)
                    {
                        ComputeFFT(FFTSize, ref surfaceData.HeightSpectrumTexture, false, false, false);
                        ComputeFFT(FFTSize, ref surfaceData.DisplaceSpectrumTexture, false, true, false);
                    }
                    else
                    {
                        ComputeFFT(FFTSize, ref surfaceData.HeightSpectrumTexture, true, false, false);
                        ComputeFFT(FFTSize, ref surfaceData.DisplaceSpectrumTexture, true, true, false);
                    }
                }
            }

            //计算偏移纹理
            {
                surfaceData.DisplaceTexture = RenderTexture.GetTemporary(textureDes);
                surfaceData.DisplaceTexture.wrapMode = TextureWrapMode.Repeat;

                fftOceanCS.SetTexture(Displacekernel, surfaceData._HeightSpectrumTexture, surfaceData.HeightSpectrumTexture);
                fftOceanCS.SetTexture(Displacekernel, surfaceData._DisplaceSpectrumTexture, surfaceData.DisplaceSpectrumTexture);
                fftOceanCS.SetTexture(Displacekernel, surfaceData._DisplaceTexture, surfaceData.DisplaceTexture);
                fftOceanCS.Dispatch(Displacekernel, FFTSize / 8, FFTSize / 8, 1);
            }

            //计算法线和泡沫
            {
                surfaceData.NormalTexture = RenderTexture.GetTemporary(textureDes);
                surfaceData.BubblesTexture = RenderTexture.GetTemporary(textureDes);
                surfaceData.NormalTexture.wrapMode = TextureWrapMode.Repeat;
                surfaceData.BubblesTexture.wrapMode = TextureWrapMode.Repeat;

                fftOceanCS.SetTexture(NormalBubbleskernel, surfaceData._DisplaceTexture, surfaceData.DisplaceTexture);
                fftOceanCS.SetTexture(NormalBubbleskernel, surfaceData._NormalTexture, surfaceData.NormalTexture);
                fftOceanCS.SetTexture(NormalBubbleskernel, surfaceData._BubblesTexture, surfaceData.BubblesTexture);
                fftOceanCS.Dispatch(NormalBubbleskernel, FFTSize / 8, FFTSize / 8, 1);
            }

            //泡沫纹理模糊
            {
                renderData.BubblesSSSTexture = RenderTexture.GetTemporary(textureDes);
                renderData.BubblesSSSTexture.wrapMode = TextureWrapMode.Repeat;

                cmd.Blit(surfaceData.BubblesTexture, renderData.BubblesSSSTexture);

                DualBoxBlur(cmd, context, textureDes, ref surfaceData.BubblesTexture, 3, 0.1f);
                DualBoxBlur(cmd, context,textureDes, ref renderData.BubblesSSSTexture, 4, 0.8f);
            }

            RenderTexture.ReleaseTemporary(TempTexture);
        }

        public void ComputeFFT(int FFTSize,ref RenderTexture input,bool isEnd,bool isDisplace,bool isHorizontal)
        {
            CoreUtils.SetKeyword(fftOceanCS, "IS_END", isEnd);
            CoreUtils.SetKeyword(fftOceanCS, "IS_DISPLACE", isDisplace);
            CoreUtils.SetKeyword(fftOceanCS, "IS_HORIZONTAL", isHorizontal);

            fftOceanCS.SetTexture(FFTkernel, _InputRT, input);
            fftOceanCS.SetTexture(FFTkernel, _OutputRT, TempTexture);
            fftOceanCS.Dispatch(FFTkernel, FFTSize / 8, FFTSize / 8, 1);

            RenderTexture rt = input;
            input = TempTexture;
            TempTexture = rt;
        }

        //BlurforBubblesSSS
        public class DualKawaseBlurSettings
        {
            //升/降采样Pass次数
            public int blurPasses = 4;
            //blur filter
            public float blurRadius = 1.5f;
        }
        public DualKawaseBlurSettings settings = new DualKawaseBlurSettings();

        RenderTexture[] downSampleRT;
        RenderTexture[] upSampleRT;

        public void DualBoxBlur(CommandBuffer cmd, ScriptableRenderContext context,RenderTextureDescriptor desc,ref RenderTexture input,int passCount = 4,float blurRadius = 1.5f,int Index = 0)
        {
            settings.blurPasses = passCount;
            settings.blurRadius = blurRadius;
            
            Shader.SetGlobalFloat("_Offset", settings.blurRadius);

            desc.depthBufferBits = 0;
            int tw = desc.width;
            int th = desc.height;

            downSampleRT = new RenderTexture[settings.blurPasses];
            upSampleRT = new RenderTexture[settings.blurPasses];

            RenderTexture tmpRT = input;
            //downSample
            for (int i = 0; i < settings.blurPasses; i++)
            {
                downSampleRT[i] = RenderTexture.GetTemporary(tw, th, 0,desc.colorFormat);
                downSampleRT[i].filterMode = FilterMode.Trilinear;
                upSampleRT[i] = RenderTexture.GetTemporary(tw, th, 0, desc.colorFormat);
                upSampleRT[i].filterMode = FilterMode.Trilinear;

                tw = Mathf.Max(tw / 2, 1);
                th = Mathf.Max(th / 2, 1);
                cmd.Blit(tmpRT, downSampleRT[i], BlurMaterial, 0);
                tmpRT = downSampleRT[i];
            }

            for (int i = settings.blurPasses - 2; i >= 0; i--)
            {
                cmd.Blit(tmpRT, upSampleRT[i], BlurMaterial, 1);
                tmpRT = upSampleRT[i];
            }

            //final pass
            cmd.Blit(tmpRT, input);

            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
            //必要
            context.Submit();

            for (int i = 0; i < settings.blurPasses; i++)
            {
                RenderTexture.ReleaseTemporary(downSampleRT[i]);
                RenderTexture.ReleaseTemporary(upSampleRT[i]);
            }

        }

        //Data per Camera
        private WaterSurfaceDatas m_surfaceData;
        private WaterRenderingDatas m_renderingData;

        public void Setup(ref WaterRenderingDatas _renderingData,ref WaterSurfaceDatas _surfaceData)
        {
            m_surfaceData = _surfaceData;
            m_renderingData = _renderingData;
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (IsCullWaterLayerMask(renderingData.cameraData.camera.cullingMask)) return;

            CommandBuffer cmd = CommandBufferPool.Get("RenderWaterTransparent");

            //SetKeyWords
            {
                if (m_renderingData.reflectionMode == ReflectionMode.REFLECTPROBE)
                {
                    CoreUtils.SetKeyword(fftOceanMaterial, "USE_REFLECTION_PROBE", true);
                    CoreUtils.SetKeyword(fftOceanMaterial, "USE_REFLECTION_PLANAR", false);
                }
                else if (m_renderingData.reflectionMode == ReflectionMode.PLANAR)
                {
                    CoreUtils.SetKeyword(fftOceanMaterial, "USE_REFLECTION_PROBE", false);
                    CoreUtils.SetKeyword(fftOceanMaterial, "USE_REFLECTION_PLANAR", true);
                }
                //set AP
                CoreUtils.SetKeyword(fftOceanMaterial, "RENDER_AP", m_renderingData.bRenderAerialPerspective);
            }

            fftOceanMaterial.SetTexture(m_surfaceData._DisplaceTexture, m_surfaceData.DisplaceTexture);
            fftOceanMaterial.SetTexture(m_surfaceData._NormalTexture, m_surfaceData.NormalTexture);
            fftOceanMaterial.SetTexture(m_surfaceData._BubblesTexture, m_surfaceData.BubblesTexture);

            fftOceanMaterial.SetTexture(m_renderingData._BubblesSSSTexture, m_renderingData.BubblesSSSTexture);
            fftOceanMaterial.SetTexture(m_renderingData._ReflectionTexture, m_renderingData.ReflectionTexture);

            if (!m_surfaceData.Infinite)
            {
                if (m_renderingData.bRenderAerialPerspective)
                {
                    ViewSkyInfo skyInfo = m_renderingData.viewSkyInfo;
                    fftOceanMaterial.SetTexture(skyInfo._AerialPerspectiveTexture, skyInfo.aerialPerspectiveLut);
                    fftOceanMaterial.SetFloat(skyInfo._APStartDepthKm,skyInfo.AerialPerspectiveStartDepthKm);
                }

                cmd.DrawMesh(m_surfaceData.targetMesh, m_surfaceData.meshLocalToWorld, fftOceanMaterial, 0, 0);
            }
            else
            {

            }

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        public bool IsCullWaterLayerMask(LayerMask layerMask)
        {
            // 根据Layer数值进行移位获得用于运算的Mask值
            int objLayerMask = 1 << LayerMask.NameToLayer("Water");
            return !((layerMask.value & objLayerMask) > 0);
        }

        public void Dispose()
        {
            fftOceanCS = null;

            if (TempTexture)
                RenderTexture.ReleaseTemporary(TempTexture);

            CoreUtils.Destroy(fftOceanMaterial);
        }

        public readonly int Ns = Shader.PropertyToID("Ns");
        public readonly int _InputRT = Shader.PropertyToID("_InputRT");
        public readonly int _OutputRT = Shader.PropertyToID("_OutputRT");
    }
}
