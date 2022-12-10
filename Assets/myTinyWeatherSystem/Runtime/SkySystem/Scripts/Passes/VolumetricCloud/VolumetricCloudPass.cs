using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Yu_Weather
{
	class VolumetricCloudPass : ScriptableRenderPass
	{
		private ComputeShader volumetricCloudCS;

        private int shadowMapKernel;
		private int shadowMapFilterKernel;

		public VolumetricCloudPass(ComputeShader volumetricShader)
		{
			if (!volumetricCloudCS)
				volumetricCloudCS = volumetricShader;
        }

		//to do
		public void RenderVolumetricCloudShadowMap(CommandBuffer cmd,ScriptableRenderContext context,ref SceneSkyInfo sceneSkyInfo)
		{

		}

		private ViewSkyInfo m_viewSkyInfo;
        private SceneSkyInfo m_sceneSkyInfo;
        public void Setup(ref ViewSkyInfo viewSkyInfo,ref SceneSkyInfo sceneSkyInfo)
		{
			m_viewSkyInfo = viewSkyInfo;
			m_sceneSkyInfo = sceneSkyInfo;
        }
		public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
		{
			
		}

		public void Dispose()
		{

		}
	}
}