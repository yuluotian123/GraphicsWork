using System.Drawing;
using UnityEngine;
using UnityEngine.Rendering;

namespace Yu_Weather
{
    [GenerateHLSL(PackingRules.Exact, needAccessors = false, generateCBuffer = true, constantRegister = 3)]
    public unsafe struct shaderVariableWaterSurface
    {
        public float _MeshSize;
        public float _MeshLength;
        public float _FFTSize;
        public float _Lambda; //用来控制偏移大小

        public float _Amplitude;          //phillips谱参数，影响波浪高度    
        public float _HeightScale;   //高度影响
        public float _BubblesScale;  //泡沫强度
        public float _BubblesThreshold;//泡沫阈值

        public Vector4 _WindAndSeed;//风向和随机种子 xy为风, zw为两个随机种子

        public float _MTime;//时间
        public float _Tess;//曲面细分
        public float _MinDist;
        public float _MaxDist;

        public Vector4 _MeshScale;
    }

    [GenerateHLSL(PackingRules.Exact, needAccessors = false, generateCBuffer = true, constantRegister = 4)]
    public unsafe struct shaderVariableWaterRendering
    {
        public float _MaxDepth;
        public float _HeightExtra;
        public float _Fade;
        public float _Fresnel;

        public float _Reflect;
        public float _Refract;
        public float _NormalBias;
        public float _NormalPower;

        public float _SSSDistortion;
        public float _SSSPow;
        public float _SSSscale;
        public float _SSSMaxWaveHeight;

        public Vector4 _SSSColor;
        public Vector4 _SpecColor;

        public float _LightIntensityScale;
        public float _Shininess;
        public float _Shadow;
        public float _UnUsed0;

        public float _FoamEdge;
        public float _FoamAdd;
        public float _FoamScale;
        public float _FoamRange;

        public Vector4 _FoamColor;

        public Vector4 _SeaColor;
    }

}