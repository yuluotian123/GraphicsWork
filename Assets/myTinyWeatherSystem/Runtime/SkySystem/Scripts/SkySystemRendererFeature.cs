using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Yu_Weather
{
    public class SkySystemRendererFeature : ScriptableRendererFeature
    {
        /// <summary>
        /// sky,cloud PerFrame������ݣ���ÿ֡�����������Release
        /// </summary>
        public SceneSkyInfo sceneSkyInfo = new SceneSkyInfo();
        /// <summary>
        /// sky,cloud PerCamera������� �������ݴ���ǳ���ֱ�ۣ��д��Ľ�
        /// </summary>
        public ViewSkyInfo viewSkyInfo = new ViewSkyInfo();

        SkyAtmosphereController skyAC;
        VolumetricCloudController skyVC;

        public bool bRenderAtmosphre = false;
        [HideInInspector]
        public bool bRenderVolumetricCloud = false;

        /// <summary>
        /// ����������Luts
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
        /// Create�ᱻ��������,�ֱ���ScriptRenderer���ɵ�ʱ�����һ�Σ�OnEnable����һ�Σ�OnValidate����һ��
        /// ������ζ�������Create��newһ����������new3��
        /// ���� ���� ��дEnable��Validate���������������ٴε���Create()
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
            //new ����pass
            skylutPass = new ComputeLutsPass(lutCS);
            skylutPass.renderPassEvent = RenderPassEvent.BeforeRenderingSkybox;

            skyPass = new SkyAtmospherePass(skyPS);
            skyPass.renderPassEvent = RenderPassEvent.AfterRenderingSkybox;

            //To Do
            cloudPass = new VolumetricCloudPass(vcCS);
            cloudPass.renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
        }

        /// <summary>
        /// ÿ֡�Ŀ�ͷִ�У�������Camera֮ǰ
        /// </summary>
        /// <param name="context"></param>
        /// <param name="cameras"></param>
        public void onPerFrameLutRendering(ScriptableRenderContext context, List<Camera> cameras)
        {
            CommandBuffer cmd = CommandBufferPool.Get("PerFrameSky");

            //����Ƿ���Ҫ��Ⱦ(ÿ֡���һ�Σ�����ÿ������һ�Σ�)
            bRenderAtmosphre = ShouldRenderSkyAtmosphere(out skyAC);
            bRenderVolumetricCloud = ShouldRenderVolumetricCloud(out skyVC);

            //release��һ֡������
            sceneSkyInfo.Release();

            //��������
            sceneSkyInfo.Update(skyAC, skyVC);

            //����BlueNoise
            if (!blueNoise)
                blueNoise = Texture2D.whiteTexture;
            Shader.SetGlobalTexture(Shader.PropertyToID("_BlueNoise"), blueNoise);

            //����CBuffer(��պ�����Ƶ�CBuffer�����������)
            ConstantBuffer.PushGlobal(cmd, sceneSkyInfo.skyParameters, sceneSkyInfo.skyParametersID);

            if (bRenderAtmosphre)
            {
                //����ÿ֡����Ĵ������lut
                skylutPass.RenderPerFrameLuts(cmd, context, ref sceneSkyInfo);
                //����ÿ֡����Ļ���CubeMap(���ڻ�����ReflectionProbe��costly)
                ViewSkyInfo captureSkyInfo = new ViewSkyInfo();
                skylutPass.RenderEnvironmentCubeMap(cmd, context, ref sceneSkyInfo, ref captureSkyInfo);
            }

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        /// <summary>
        /// ��ÿ֡��ÿ��Camera�Ŀ�ͷ����
        /// �������ݴ���ķ�ʽ�ܶ��ģ������ҹ����Ǹ���ִ��˳��д�ģ���װ��������AddRenderPasses�ﰴ˳��ִ��
        /// </summary>
        /// <param name="renderer"></param>
        /// <param name="renderingData"></param>
        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            //release��һ�����������
            viewSkyInfo.Release();

            if (renderingData.cameraData.targetTexture != null && renderingData.cameraData.targetTexture.format == RenderTextureFormat.Depth) return;

            //���������ص�����
            viewSkyInfo.Update(ref renderingData.cameraData, skyAC);

            if (bRenderAtmosphre)
            {
                //����skylutPass����Ҫ������
                skylutPass.Setup(ref viewSkyInfo, ref sceneSkyInfo);
                renderer.EnqueuePass(skylutPass);

                //����skyPass����Ҫ������
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
        /// ÿ֡�����û�ж�Ӧ��component�ڳ����У������ƺ����˷ѣ�Ҳ����Բ���ÿ֡����
        /// Ҳ����Ҫÿ֡����
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

            return skyVC && bRenderAtmosphre && vcCS;//û�д����Ͳ���Ⱦ����ƣ���Ϊ�����ܳ�
        }

        /// <summary>
        /// Dispose all the passes
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing)
        {
            //beginContextRendering��ȥ��action
            RenderPipelineManager.beginContextRendering -= onPerFrameLutRendering;

            //�������CBuffer
            ConstantBuffer.ReleaseAll();

            //����������ɵ���ʱ����
            sceneSkyInfo.Release();
            viewSkyInfo.Release();

            //����pass��dispose
            
            skylutPass.Dispose();
            skyPass.Dispose();
            cloudPass.Dispose();

            Debug.Log("DisposeSkySystemFeature");
        }
    }
}

