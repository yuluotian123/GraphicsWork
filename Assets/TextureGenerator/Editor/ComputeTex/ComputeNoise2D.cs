using UnityEditor;
using UnityEngine;

public class ComputeNoise2D : ComputeTexture2D
{
    //噪音采用哪种噪音算法
    public enum Noise2DType
    {
        PerlinNoise,
        ValueNoise,
        SimplexNoise,
        WorleyNoise,
        PerlinWorleyNoise,
        Turbulence,
        Ridged
    }
    public Noise2DType type = Noise2DType.PerlinNoise;

    //噪音使用FBM的情况
    public bool IsFBM = false;
    public int FBMtimes = 2;

    public float Contrast = 1.0f;
    public float Brightness = 1.0f;
        
    public bool IsTile = false;
    public int Freq = 2;

    //噪声调色
    public override void OnPreGenerateGUI()
    {
        base.OnPreGenerateGUI();

        type = (Noise2DType)EditorGUILayout.EnumPopup("选择噪音类型", type);

        if (TextureGenerator == null)
            TextureGenerator = AssetDatabase.LoadAssetAtPath<ComputeShader>("Assets/TextureGenerator/Shaders/NoiseGenerator/ComputeNoise2D.compute");

        switch (type)
        {
            case Noise2DType.PerlinNoise:
            case Noise2DType.WorleyNoise:
            case Noise2DType.ValueNoise:
            case Noise2DType.SimplexNoise:
                KernelName = "ComputeNoise";
                break;
            case Noise2DType.PerlinWorleyNoise:
                KernelName = "PerlinWorleyNoise";
                break;
            case Noise2DType.Turbulence:
                KernelName = "Turbulence";
                break;
            case Noise2DType.Ridged:
                KernelName = "Ridged";
                break;
        }

        EditorGUILayout.BeginHorizontal();
        Contrast = EditorGUILayout.Slider("对比度",Contrast, 0.1f, 8.0f);
        Brightness = EditorGUILayout.Slider("亮度", Brightness, 0.1f, 2.0f);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.ObjectField("计算着色器(ReadOnly)", TextureGenerator, typeof(ComputeShader), false);
        EditorGUILayout.TextField("核函数(ReadOnly)", KernelName);
        EditorGUILayout.EndHorizontal();

        if (type == Noise2DType.Turbulence || type == Noise2DType.Ridged)
        {
            FBMtimes = EditorGUILayout.IntSlider("进行迭代的次数", FBMtimes, 2, 16);
        }
        else if(type == Noise2DType.PerlinWorleyNoise)
        {
            Freq = EditorGUILayout.IntSlider("频率 ", Freq, 2, 16);
            FBMtimes = EditorGUILayout.IntSlider("进行迭代的次数", FBMtimes, 2, 16);
        }
        else
        {
            if(type == Noise2DType.WorleyNoise || type == Noise2DType.PerlinNoise)
            {
                IsTile = EditorGUILayout.Toggle("使用TileNoise(简单版本) ", IsTile);
                if (IsTile)
                    Freq = EditorGUILayout.IntSlider("频率 ", Freq, 2, 16);
            }
            IsFBM = EditorGUILayout.Toggle("使用FBM ", IsFBM);
            if (IsFBM)
                FBMtimes = EditorGUILayout.IntSlider("进行迭代的次数", FBMtimes, 2, 16);
        }
    }
    private class PreviousSettings
    {
        public TextureSettings p_settings = new TextureSettings();

        public Noise2DType p_type = Noise2DType.PerlinNoise;

        public bool p_IsFBM = false;
        public int p_FBMtimes = 2;

        public bool p_IsTile = false;
        public int p_Freq = 2;

        public float p_Contrast = 1;
        public float p_Bright = 1;
    }
    private PreviousSettings prevSettings = new PreviousSettings();
    private bool HasPreviousSettingsChanged()
    {
        if (prevSettings.p_type == type &&
            prevSettings.p_settings==settings&&
            prevSettings.p_IsFBM == IsFBM &&
            prevSettings.p_FBMtimes == FBMtimes&&
            prevSettings.p_Contrast == Contrast&&
            prevSettings.p_Bright == Brightness&&
            prevSettings.p_IsTile == IsTile&&
            prevSettings.p_Freq == Freq
            ) 
        { 
            return false;
        }
        else
        {
            prevSettings.p_settings.UpdateSettings(settings);

            prevSettings.p_type = type;

            prevSettings.p_IsFBM = IsFBM;
            prevSettings.p_FBMtimes = FBMtimes;

            prevSettings.p_Contrast = Contrast;
            prevSettings.p_Bright = Brightness;

            prevSettings.p_IsTile = IsTile;
            prevSettings.p_Freq = Freq;

            return true;
        }
    }
    public override bool ShouldGenerate()
    {
        if (HasPreviousSettingsChanged() || rwTexture == null)
            return true;

        return false;
    }
    public override void OnGenerate()
    {
        Debug.Log("Generating...");
        int kernel = TextureGenerator.FindKernel(KernelName);

        if (type != Noise2DType.Turbulence &&type != Noise2DType.Ridged && type != Noise2DType.PerlinWorleyNoise)
        {
            switch (type)
            {
                case Noise2DType.PerlinNoise:
                    Shader.EnableKeyword("_PERLIN_NOISE");
                    break;
                case Noise2DType.ValueNoise:
                    Shader.EnableKeyword("_VALUE_NOISE");
                    break;
                case Noise2DType.SimplexNoise:
                    Shader.EnableKeyword("_SIMPLEX_NOISE");
                    break;
                case Noise2DType.WorleyNoise:
                    Shader.EnableKeyword("_WORLEY_NOISE");
                    break;
                default:
                    return;
            }

            if (IsTile && (type == Noise2DType.PerlinNoise || type == Noise2DType.WorleyNoise)) Shader.EnableKeyword("_ISTILE");
            if (IsFBM) Shader.EnableKeyword("_ISFBM");
        }

        TextureGenerator.SetFloat("_Counts", FBMtimes);
        TextureGenerator.SetFloat("_Freq", Freq);
        TextureGenerator.SetFloat("_Contrast", Contrast);
        TextureGenerator.SetFloat("_Bright", Brightness);

        TextureGenerator.Dispatch(kernel, settings.width / 16, settings.height / 16, 1);

        Shader.DisableKeyword("_PERLIN_NOISE");
        Shader.DisableKeyword("_VALUE_NOISE");
        Shader.DisableKeyword("_SIMPLEX_NOISE");
        Shader.DisableKeyword("_WORLEY_NOISE");
        Shader.DisableKeyword("_ISTILE");
        Shader.DisableKeyword("_ISFBM");
    }
}
