using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
 
public class ShowNoise2D : ComputeNoise2D
{
    public bool foldout = false;

    public void UpdateSettings(Noise2DType type,TextureSettings settings,int freq,int fbmTimes,float Contrast,float Brightness)
    {
        this.type = type;
        this.settings = settings;
        this.Freq = freq;
        this.FBMtimes = fbmTimes;
        this.Contrast = Contrast;
        this.Brightness = Brightness;
    }
    public override void OnPreGenerateGUI()
    {
        if (TextureGenerator == null)
            TextureGenerator = AssetDatabase.LoadAssetAtPath<ComputeShader>("Assets/TextureGenerator/Shaders/NoiseGenerator/ComputeNoise2D.compute");

        switch (type)
        {
            case Noise2DType.PerlinWorleyNoise:
                KernelName = "PerlinWorleyNoise";
                break;
            default:
                KernelName = "ComputeNoise";
                break;
        }

        IsTile = true;
        IsFBM = true;
    }
}

public sealed class ComputeNoise3D : ComputeTexture3D
{
    public enum Noise3DType
    {
        Noise3D_1,
        Noise3D_2,
        Custom
    }
    private Noise3DType type = Noise3DType.Noise3D_1;

    ShowNoise2D noiseR = new ShowNoise2D();
    ShowNoise2D noiseG = new ShowNoise2D();
    ShowNoise2D noiseB = new ShowNoise2D();
    ShowNoise2D noiseA = new ShowNoise2D();

    public override void OnPreGenerateGUI()
    {
         base.OnPreGenerateGUI();

        type = (Noise3DType)EditorGUILayout.EnumPopup("噪声类型", type);

        if(type == Noise3DType.Custom)
        {
            if (AssetDatabase.GetAssetPath(TextureGenerator) == "Assets/TextureGenerator/Shaders/NoiseGenerator/ComputeNoise3D.compute")
            {
                TextureGenerator = null;
                KernelName = "";
            }

            EditorGUILayout.BeginHorizontal();
            TextureGenerator = EditorGUILayout.ObjectField("计算着色器", TextureGenerator, typeof(ComputeShader), false) as ComputeShader;
            KernelName = EditorGUILayout.TextField("核函数", KernelName);
            EditorGUILayout.EndHorizontal();
        }
        else
        {
            if (TextureGenerator == null || AssetDatabase.GetAssetPath(TextureGenerator) != "Assets/TextureGenerator/Shaders/NoiseGenerator/ComputeNoise3D.compute")
                TextureGenerator = AssetDatabase.LoadAssetAtPath<ComputeShader>("Assets/TextureGenerator/Shaders/NoiseGenerator/ComputeNoise3D.compute");

            KernelName = "ComputeNoise";



            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.ObjectField("计算着色器(ReadOnly)", TextureGenerator, typeof(ComputeShader), false);
            EditorGUILayout.TextField("核函数(ReadOnly)", KernelName);
            EditorGUILayout.EndHorizontal();
            
            switch (type)
            {
                case Noise3DType.Noise3D_1:
                    noiseR.UpdateSettings(ComputeNoise2D.Noise2DType.PerlinWorleyNoise,settings,4,7,1.0f,1.1f);
                    noiseG.UpdateSettings(ComputeNoise2D.Noise2DType.WorleyNoise, settings, 4, 3, 0.9f, 1.0f);
                    noiseB.UpdateSettings(ComputeNoise2D.Noise2DType.WorleyNoise, settings, 8, 3, 0.9f, 1.0f);
                    noiseA.UpdateSettings(ComputeNoise2D.Noise2DType.WorleyNoise, settings, 16, 3, 0.9f, 1.0f);
                    break;
                case Noise3DType.Noise3D_2:
                    noiseR.UpdateSettings(ComputeNoise2D.Noise2DType.WorleyNoise, settings, 4, 3, 0.9f, 1.0f);
                    noiseG.UpdateSettings(ComputeNoise2D.Noise2DType.WorleyNoise, settings, 8, 3, 0.9f, 1.0f);
                    noiseB.UpdateSettings(ComputeNoise2D.Noise2DType.WorleyNoise, settings, 16, 3, 0.9f, 1.0f);
                    noiseA.UpdateSettings(ComputeNoise2D.Noise2DType.WorleyNoise, settings, 4, 3, 0.9f, 1.0f);
                    break;

            }
        }
    }

    public override void OnGenerate()
    {
        Debug.Log("Generating...");

        if (type != Noise3DType.Custom)
        {
            switch (type)
            {
                case Noise3DType.Noise3D_1:
                    Shader.EnableKeyword("NOISE_1");
                    break;
                case Noise3DType.Noise3D_2:
                    Shader.EnableKeyword("NOISE_2");
                    break;
            }


        }

        int kernel = TextureGenerator.FindKernel(KernelName);
        TextureGenerator.Dispatch(kernel, settings.width / 8, settings.height / 8, settings.depth / 8);

        Shader.DisableKeyword("NOISE_1");
        Shader.DisableKeyword("NOISE_2");
    }

    public override void OnAfterGenerateGUI(bool update)
    {
        base.OnAfterGenerateGUI(update);

        noiseR.foldout = EditorGUILayout.BeginFoldoutHeaderGroup(noiseR.foldout, "NoiseR");
        if (noiseR.foldout)
        {
            noiseR.Show(dirPath,textureName);
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
        noiseG.foldout = EditorGUILayout.BeginFoldoutHeaderGroup(noiseG.foldout, "NoiseG");
        if (noiseG.foldout)
        {
            noiseG.Show(dirPath, textureName);
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
        noiseB.foldout = EditorGUILayout.BeginFoldoutHeaderGroup(noiseB.foldout, "NoiseB");
        if (noiseB.foldout)
        {
            noiseB.Show(dirPath, textureName);
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
        noiseA.foldout = EditorGUILayout.BeginFoldoutHeaderGroup(noiseA.foldout, "NoiseA");
        if (noiseA.foldout)
        {
            noiseA.Show(dirPath, textureName);
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
    }
}