using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Yu_Weather
{
    public class SkySystemRendererFeature : ScriptableRendererFeature
    {
        /// <summary>
        /// sky,cloud PerFrame相关数据，在每帧的最初产生和Release
        /// </summary>
        public SceneSkyInfo sceneSkyInfo = new SceneSkyInfo();
        /// <summary>
        /// sky,cloud PerCamera相关数据 这块的数据传输非常不直观，有待改进
        /// </summary>
        public ViewSkyInfo viewSkyInfo = new ViewSkyInfo();

        SkyAtmosphereController skyAC;
        VolumetricCloudController skyVC;

        public bool bRenderAtmosphre = false;
        [HideInInspector]
        public bool bRenderVolumetricCloud = false;

        /// <summary>
        /// 计算大气相关Luts
        /// </summary>
        ComputeLutsPass skylutPass;
        SkyAtmospherePass skyPass;
        VolumetricCloudPass cloudPass;

        /// <summary>
        /// Shader Resources
        /// </summary>
        public Texture2D blueNoise;
        public ComputeShader lutCS;
        public Shader skyPS;
        public ComputeShader vcCS;

        /// <summary>
        /// Create会被调用三次,分别是ScriptRenderer生成的时候调用一次，OnEnable调用一次，OnValidate调用一次
        /// 所以意味着如果在Create中new一个登西，会new3次
        /// 所以 我们 重写Enable和Validate方法，让他不会再次调用Create()
        /// </summary>
        public void OnEnable()
        {
            //RenderPipelineManager.beginContextRendering += onPerFrameLutRendering;
        }
        public void OnValidate()
        {
            
        }

        public override void Create()
        {
            RenderPipelineManager.beginContextRendering += onPerFrameLutRendering;
            //new 各个pass
            skylutPass = new ComputeLutsPass(lutCS);
            skylutPass.renderPassEvent = RenderPassEvent.BeforeRenderingSkybox;

            skyPass = new SkyAtmospherePass(skyPS);
            skyPass.renderPassEvent = RenderPassEvent.AfterRenderingSkybox;

            //To Do
            cloudPass = new VolumetricCloudPass(vcCS);
            cloudPass.renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
        }

        /// <summary>
        /// 每帧的开头执行，在所有Camera之前
        /// </summary>
        /// <param name="context"></param>
        /// <param name="cameras"></param>
        public void onPerFrameLutRendering(ScriptableRenderContext context, List<Camera> cameras)
        {
            CommandBuffer cmd = CommandBufferPool.Get("PerFrameSky");

            //检测是否需要渲染(每帧检测一次？还是每相机检测一次？)
            bRenderAtmosphre = ShouldRenderSkyAtmosphere(out skyAC);
            bRenderVolumetricCloud = ShouldRenderVolumetricCloud(out skyVC);

            //release上一帧的数据
            sceneSkyInfo.Release();

            //更新数据
            sceneSkyInfo.Update(skyAC, skyVC);

            //设置BlueNoise
            if (!blueNoise)
                blueNoise = Texture2D.whiteTexture;
            Shader.SetGlobalTexture(Shader.PropertyToID("_BlueNoise"), blueNoise);

            //更新CBuffer(天空和体积云的CBuffer都在这里更新)
            ConstantBuffer.PushGlobal(cmd, sceneSkyInfo.skyParameters, sceneSkyInfo.skyParametersID);

            if (bRenderAtmosphre)
            {
                //生成每帧所需的大气相关lut
                skylutPass.RenderPerFrameLuts(cmd, context, ref sceneSkyInfo);
                //生成每帧所需的环境CubeMap(现在还在用ReflectionProbe，costly)
                ViewSkyInfo captureSkyInfo = new ViewSkyInfo();
                skylutPass.RenderEnvironmentCubeMap(cmd, context, ref sceneSkyInfo, ref captureSkyInfo);
            }

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        /// <summary>
        /// 在每帧的每个Camera的开头进行
        /// 由于数据传输的方式很恶心，所以我姑且是根据执行顺序写的，假装他们是在AddRenderPasses里按顺序执行
        /// </summary>
        /// <param name="renderer"></param>
        /// <param name="renderingData"></param>
        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            //release上一个相机的数据
            viewSkyInfo.Release();

            if (renderingData.cameraData.targetTexture != null && renderingData.cameraData.targetTexture.format == RenderTextureFormat.Depth) return;

            //更新相机相关的数据
            viewSkyInfo.Update(ref renderingData.cameraData, skyAC);

            if (bRenderAtmosphre)
            {
                //传入skylutPass所需要的数据
                skylutPass.Setup(ref viewSkyInfo, ref sceneSkyInfo);
                renderer.EnqueuePass(skylutPass);

                //传入skyPass所需要的数据
                skyPass.Setup(ref sceneSkyInfo, ref viewSkyInfo);
                renderer.EnqueuePass(skyPass);
            }
            //To Do
            if (bRenderVolumetricCloud)
            {
                cloudPass.Setup(ref viewSkyInfo, ref sceneSkyInfo);
                renderer.EnqueuePass(cloudPass);
            }

        }

        /// <summary>
        /// 每帧检查有没有对应的component在场景中，但是似乎很浪费，也许可以不必每帧计算
        /// 也许不需要每帧计算
        /// </summary>
        /// <param name="skyAC"></param>
        /// <returns></returns>
        public bool ShouldRenderSkyAtmosphere(out SkyAtmosphereController skyAC)
        {
            skyAC = FindObjectOfType<SkyAtmosphereController>();

            return skyAC != null && lutCS && skyPS;
        }
        public bool ShouldRenderVolumetricCloud(out VolumetricCloudController skyVC)
        {
            skyVC = FindObjectOfType<VolumetricCloudController>();

            return skyVC && bRenderAtmosphre && vcCS;//没有大气就不渲染体积云，因为那样很丑！
        }

        /// <summary>
        /// Dispose all the passes
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing)
        {
            //beginContextRendering中去掉action
            RenderPipelineManager.beginContextRendering -= onPerFrameLutRendering;

            //清除所有CBuffer
            ConstantBuffer.ReleaseAll();

            //清除所有生成的临时数据
            sceneSkyInfo.Release();
            viewSkyInfo.Release();

            //各个pass的dispose
            
            skylutPass.Dispose();
            skyPass.Dispose();
            cloudPass.Dispose();

            Debug.Log("DisposeSkySystemFeature");
        }
    }
}

