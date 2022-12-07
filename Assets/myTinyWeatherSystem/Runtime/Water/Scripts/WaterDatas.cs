using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace Yu_Weather
{
    public class WaterSurfaceDatas
    {
        public bool Infinite = false;
        public Mesh targetMesh = null;
        public Matrix4x4 meshLocalToWorld;
        public RenderTexture GaussianTexture;

        public shaderVariableWaterSurface surfaceParameters;
        public int FFTPow;

        public RenderTexture HeightSpectrumTexture;
        public RenderTexture DisplaceSpectrumTexture;
        public RenderTexture DisplaceTexture;
        public RenderTexture NormalTexture;
        public RenderTexture BubblesTexture;

        public void Update(FFTOcean fftOcean,ComputeShader GaussianCS)
        {
            if (fftOcean == null) return;

            UpdateSurfaceParameters(fftOcean);

            int PreFFTPow = FFTPow;
            FFTPow = fftOcean.fftPow;

            Infinite = fftOcean.Infinite;
            if (!Infinite)
            {
                if (fftOcean.mesh == null)
                {
                    fftOcean.mesh = GenerateGridMesh(fftOcean.meshSize, fftOcean.meshLength);
                }

                targetMesh = fftOcean.mesh;
                meshLocalToWorld = fftOcean.transform.localToWorldMatrix;
            }
            else
            {
                //Use for Update?
                fftOcean.mesh = null;
                targetMesh = null;
            }

            //一旦设置发生变化就重新生成
            if (!fftOcean.hasGeneratedGaussianRT||!GaussianTexture|| FFTPow != PreFFTPow)
            {
                fftOcean.hasGeneratedGaussianRT = true;
                GenerateGaussianTexture(surfaceParameters._FFTSize, GaussianCS);
            }
        }

        public void UpdateSurfaceParameters(FFTOcean fftOcean)
        {
            surfaceParameters._MeshSize = fftOcean.meshSize;
            surfaceParameters._MeshLength = fftOcean.meshLength;
            surfaceParameters._FFTSize = Mathf.Pow(2, fftOcean.fftPow);
            surfaceParameters._Lambda = fftOcean.lambda;

            surfaceParameters._Amplitude = fftOcean.amplitude;
            surfaceParameters._HeightScale = fftOcean.heightScale;
            surfaceParameters._BubblesScale = fftOcean.bubblesScale;
            surfaceParameters._BubblesThreshold = fftOcean.bubblesThreshold;

            surfaceParameters._WindAndSeed.z = Random.Range(1, 10f);
            surfaceParameters._WindAndSeed.w = Random.Range(1, 10f);
            Vector2 wind =  fftOcean.windDirection.normalized * fftOcean.windScale;
            surfaceParameters._WindAndSeed = new Vector4(wind.x, wind.y, surfaceParameters._WindAndSeed.z, surfaceParameters._WindAndSeed.w);

            surfaceParameters._MTime = fftOcean.time;

            surfaceParameters._Tess = fftOcean.tess; 
            surfaceParameters._MinDist = fftOcean.minDist;
            surfaceParameters._MaxDist = fftOcean.maxDist;
        }

        public Mesh GenerateGridMesh(int MeshSize, float MeshLength)
        {
            Mesh mesh = new Mesh();
            int []vertIndexs = new int[(MeshSize - 1) * (MeshSize - 1) * 6];
            Vector3 []positions = new Vector3[MeshSize * MeshSize];
            Vector2 []uvs = new Vector2[MeshSize * MeshSize];

            int inx = 0;
            for (int i = 0; i < MeshSize; i++)
            {
                for (int j = 0; j < MeshSize; j++)
                {
                    int index = i * MeshSize + j;
                    positions[index] = new Vector3((j - MeshSize / 2.0f) * MeshLength / MeshSize, 0, (i - MeshSize / 2.0f) * MeshLength / MeshSize);
                    uvs[index] = new Vector2(j / (MeshSize - 1.0f), i / (MeshSize - 1.0f));

                    if (i != MeshSize - 1 && j != MeshSize - 1)
                    {
                        vertIndexs[inx++] = index;
                        vertIndexs[inx++] = index + MeshSize;
                        vertIndexs[inx++] = index + MeshSize + 1;

                        vertIndexs[inx++] = index;
                        vertIndexs[inx++] = index + MeshSize + 1;
                        vertIndexs[inx++] = index + 1;
                    }
                }
            }
            mesh.vertices = positions;
            mesh.SetIndices(vertIndexs, MeshTopology.Triangles, 0);
            mesh.uv = uvs;

            return mesh;
        }

        public void GenerateGaussianTexture(float FFTSize, ComputeShader GaussianCS)
        {
            if(GaussianTexture)
                GaussianTexture.Release();

            GaussianTexture = new RenderTexture((int)FFTSize, (int)FFTSize, 0, RenderTextureFormat.ARGBFloat);
            GaussianTexture.enableRandomWrite = true;
            GaussianTexture.Create();

            int kernel = GaussianCS.FindKernel("CSMain");
            //设置ComputerShader数据
            GaussianCS.SetInt("_FFTSize", (int)FFTSize);

            GaussianCS.SetTexture(kernel, _GaussianRT, GaussianTexture);
            GaussianCS.Dispatch(kernel, (int)FFTSize / 8, (int)FFTSize / 8, 1);

        }

        public void Release()
        {
            if (GaussianTexture)
                GaussianTexture.Release();

            ReleasePerFrame();

            targetMesh = null;
        }

        public void ReleasePerFrame()
        {
            if (HeightSpectrumTexture)
                RenderTexture.ReleaseTemporary(HeightSpectrumTexture);
            if (DisplaceSpectrumTexture)
                RenderTexture.ReleaseTemporary(DisplaceSpectrumTexture);
            if (DisplaceTexture)
                RenderTexture.ReleaseTemporary(DisplaceTexture);
            if (NormalTexture)
                RenderTexture.ReleaseTemporary(NormalTexture);
            if (BubblesTexture)
                RenderTexture.ReleaseTemporary(BubblesTexture);
        }

        public readonly int surfaceParametersID = Shader.PropertyToID("shaderVariableWaterSurface");
        private readonly int _GaussianRT = Shader.PropertyToID("_GaussianRT");

        public readonly int _GaussianTexture = Shader.PropertyToID("_GaussianTexture");
        public readonly int _HeightSpectrumTexture = Shader.PropertyToID("_HeightSpectrumTexture");
        public readonly int _DisplaceSpectrumTexture = Shader.PropertyToID("_DisplaceSpectrumTexture");
        public readonly int _DisplaceTexture = Shader.PropertyToID("_DisplaceTexture");
        public readonly int _NormalTexture = Shader.PropertyToID("_NormalTexture");
        public readonly int _BubblesTexture = Shader.PropertyToID("_BubblesTexture");
    }

    public class WaterRenderingDatas
    {
        public shaderVariableWaterRendering renderParameters;

        public Texture2D RampTexture;
        public Texture2D CausticsTexture;

        public RenderTexture ReflectionTexture;
        public ReflectionMode reflectionMode;

        public RenderTexture BubblesSSSTexture;

        public ViewSkyInfo viewSkyInfo;
        public bool bRenderAerialPerspective = false;

        public void UpdatePerFrame(FFTOcean fftOcean)
        {
            if (fftOcean == null)
                return;

            UpdateShaderVariable(fftOcean);

            reflectionMode = fftOcean.reflectionMode;

            //to do
            if (fftOcean.CausticsTexture == null)
            {

            }
            else
                CausticsTexture = fftOcean.CausticsTexture;

            if (RampTexture == null)
            {
                RampTexture = new Texture2D(128, 4, GraphicsFormat.R8G8B8A8_SRGB, TextureCreationFlags.None);

                RampTexture.wrapMode = TextureWrapMode.Clamp;

                var defaultFoamRamp = fftOcean.defaultRampTex;

                var cols = new Color[512];
                for (var i = 0; i < 128; i++)
                {
                    cols[i] = fftOcean.absorptionRamp.Evaluate(i / 128f);
                }
                for (var i = 0; i < 128; i++)
                {
                    cols[i + 128] = fftOcean.scatterRamp.Evaluate(i / 128f);
                }
                for (var i = 0; i < 128; i++)
                {

                    cols[i + 256] = defaultFoamRamp.GetPixelBilinear(i / 128f, 0.5f);
                }
                RampTexture.SetPixels(cols);
                RampTexture.Apply();
            }
        }

        public void UpdateShaderVariable(FFTOcean fftOcean)
        {
            renderParameters._HeightExtra = fftOcean.depthExtra;
            renderParameters._MaxDepth = fftOcean.maxDepth;
            renderParameters._Shininess = fftOcean.shinness;
            renderParameters._Fade = fftOcean.fade;

            renderParameters._Fresnel = fftOcean.fresnel;
            renderParameters._Reflect = fftOcean.reflect;
            renderParameters._Refract = fftOcean.refract;
            renderParameters._NormalPower = fftOcean.normalStrength;

            renderParameters._NormalBias = fftOcean.normalBias;
            renderParameters._Shadow = fftOcean.shadow;
            renderParameters._SSSPow = fftOcean.sssPow;
            renderParameters._SSSscale = fftOcean.sssScale;

            renderParameters._SSSColor = fftOcean.SSSColor;
    }

        public void UpdatePerCamera(ref ViewSkyInfo _viewSkyInfo,bool _bRenderAerialPerspective,FFTOcean fftOcean)
        {
            viewSkyInfo = _viewSkyInfo;
            bRenderAerialPerspective = _bRenderAerialPerspective;

            if (fftOcean == null) return;

            if (fftOcean.reflectionProbe && reflectionMode == ReflectionMode.REFLECTPROBE)
            {
                ReflectionTexture = fftOcean.reflectionProbe.realtimeTexture;
            }
            else if (reflectionMode == ReflectionMode.PLANAR)
            {
                ReflectionTexture = fftOcean.planarReflection.ReflectionTexture;
            }
        }

        public void Release()
        {
            ReleasePerCamera();
            ReleasePerFrame();
        }

        public void ReleasePerCamera()
        {
        }
        public void ReleasePerFrame()
        {
            if (BubblesSSSTexture)
                RenderTexture.ReleaseTemporary(BubblesSSSTexture);
        }

        public readonly int renderParametersID = Shader.PropertyToID("shaderVariableWaterRendering");
        public readonly int _ReflectionTexture = Shader.PropertyToID("_ReflectionTexture");
        public readonly int _BubblesSSSTexture = Shader.PropertyToID("_BubblesSSSTexture");
        public readonly int _AbsorptionScatteringRamp = Shader.PropertyToID("_AbsorptionScatteringRamp");
        public readonly int _CausticsTexture = Shader.PropertyToID("_CausticsTexture");
    }
}
