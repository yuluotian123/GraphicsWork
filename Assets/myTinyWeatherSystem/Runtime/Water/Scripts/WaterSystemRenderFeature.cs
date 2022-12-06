using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Yu_Weather
{
    public class WaterSystemRenderFeature : ScriptableRendererFeature
    {
        //from Scene
        FFTOcean fftOceanComponent;
        [HideInInspector]
        public bool bRenderWater = false;

        //Datas
        public WaterSurfaceDatas surfaceData = new WaterSurfaceDatas();
        public WaterRenderingDatas renderData = new WaterRenderingDatas();

        //fftoceanPass
        private FFTOceanPass fftoceanPass;

        public ComputeShader fftOceanCS;
        public ComputeShader GaussianCS;
        public Shader fftOceanPS;

        //水面的大气透视
        private ViewSkyInfo viewSkyInfo = null;
        [HideInInspector]
        public bool bRenderAerialPerspective = false;

        //override the parents so that it won't call Create() method
        public void OnEnable()
        {
            
        }
        public void OnValidate()
        {
            
        }

        public override void Create()
        {
            //beginContextRendering中加入action
            RenderPipelineManager.beginContextRendering += onPerFrameUpdateWaterSurface;

            fftoceanPass = new FFTOceanPass(fftOceanCS, fftOceanPS);
            fftoceanPass.renderPassEvent = RenderPassEvent.BeforeRenderingTransparents;
        }

        public void onPerFrameUpdateWaterSurface(ScriptableRenderContext context, List<Camera> cameras)
        {
            CommandBuffer cmd = CommandBufferPool.Get("PerFrameWater");
            bRenderWater = ShouldRenderWater(out fftOceanComponent);

            //Release上一帧的数据
            surfaceData.ReleasePerFrame();
            renderData.ReleasePerFrame();

            //更新Surface数据和CBuffer
            surfaceData.Update(fftOceanComponent,GaussianCS);
            renderData.UpdatePerFrame(fftOceanComponent);

            ConstantBuffer.PushGlobal(cmd, surfaceData.surfaceParameters, surfaceData.surfaceParametersID);
            ConstantBuffer.PushGlobal(cmd,renderData.renderParameters,renderData.renderParametersID);

            if (bRenderWater)
            {
                fftoceanPass.UpdateWaterSurface(cmd, context,ref surfaceData,ref renderData);
            }

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            bRenderAerialPerspective = TryGetCurrentSkyViewInfoAndShouldRenderSkyAerialPerspective();
            renderData.ReleasePerCamera();

            if (renderingData.cameraData.targetTexture != null && renderingData.cameraData.targetTexture.format == RenderTextureFormat.Depth) return;

            renderData.UpdatePerCamera(ref viewSkyInfo, bRenderAerialPerspective, fftOceanComponent);

            if (bRenderWater)
            {
                fftoceanPass.Setup(ref renderData, ref surfaceData);
                renderer.EnqueuePass(fftoceanPass);
            }
        }

        public bool TryGetCurrentSkyViewInfoAndShouldRenderSkyAerialPerspective()
        {
            SkySystemRendererFeature skyRF = null;
            //诡异的一笔，但是很方便
            UniversalRenderPipelineAsset URPAsset = (UniversalRenderPipelineAsset)QualitySettings.renderPipeline;
            FieldInfo propertyInfo = URPAsset.GetType().GetField("m_RendererDataList", BindingFlags.Instance | BindingFlags.NonPublic);
            UniversalRendererData URPData = (UniversalRendererData)(((ScriptableRendererData[])propertyInfo?.GetValue(URPAsset))?[0]);
            foreach (var rf in URPData.rendererFeatures)
            {
                if (rf.name == "SkySystemRendererFeature")
                {
                    skyRF = (SkySystemRendererFeature)rf;
                    viewSkyInfo = skyRF.viewSkyInfo;
                    break;
                }
            }

            if (skyRF == null) return false;
            else if (viewSkyInfo == null || !skyRF.bRenderAtmosphre) return false;

            return true;
        }

        public bool ShouldRenderWater(out FFTOcean fftOcean)
        {
            fftOcean = FindObjectOfType<FFTOcean>();

            return fftOceanComponent != null && fftOceanCS && fftOceanPS;
        }

        protected override void Dispose(bool disposing)
        {
            RenderPipelineManager.beginContextRendering -= onPerFrameUpdateWaterSurface;

            ConstantBuffer.ReleaseAll();

            surfaceData.Release();
            renderData.Release();

            fftoceanPass.Dispose();

            Debug.Log("DisposeWaterSystemRenderFeature");
        }
    }
}

