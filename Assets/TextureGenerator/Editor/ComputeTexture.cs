using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public abstract class ComputeTexture
{
    public enum TextureSize
    {
        S1 = 1,
        S2 = 2,
        S4 = 4,
        S8 = 8,
        S16 = 16,
        S32 = 32,
        S64 = 64,
        S128 = 128,
        S256 = 256,
        S512 = 512,
        S1024 = 1024,
        S2048 = 2048
    }
    private TextureSize Width = TextureSize.S256;
    private TextureSize Height = TextureSize.S256;
    private TextureSize Depth = TextureSize.S256;

    /// <summary>
    /// 基础的Texture参数设置(其实其他相关的设置，比如format，enableRandomRW等放到这里也行，但我懒)
    /// </summary>
    public class TextureSettings
    {
        public string rwTexName = "Result";
        public int width = 256;
        public int height = 256;
        public int depth = 256;
        public float scale = 1;
        public Vector3 offset = Vector3.zero;

        public static bool operator ==(TextureSettings st1, TextureSettings st2)
        {
            return st1.Equals(st2);
        }
        public static bool operator !=(TextureSettings st1, TextureSettings st2)
        {
            return !st1.Equals(st2);
        }
        public override bool Equals(object st1)
        {
            TextureSettings st2 = st1 as TextureSettings;

            return
            rwTexName == st2.rwTexName &&
            width == st2.width &&
            height == st2.height &&
            depth == st2.depth &&
            scale == st2.scale &&
            offset == st2.offset;
        }
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
        public void UpdateSettings(TextureSettings st2)
        {
            rwTexName = st2.rwTexName;
            width = st2.width;
            height = st2.height;
            depth = st2.depth;
            scale = st2.scale;
            offset = st2.offset;
        }
    }
    public TextureSettings settings = new TextureSettings();
    public RenderTexture rwTexture;

    public string KernelName = "";
    public ComputeShader TextureGenerator;
    public Vector3Int ThreadCounts;//to do

    public string dirPath;
    public string textureName;

    /// <summary>
    /// 定义在发生ComputeTexture切换的时候进行的行为（或者可以认为是初始化的时候进行的行为）
    /// </summary>
    public virtual void OnInit() { }
    /// <summary>
    /// 定义重新生成纹理的条件
    /// </summary>
    public virtual bool ShouldGenerate() { return false; }
    /// <summary>
    /// 设定生成的RW纹理的格式
    /// </summary>
    public virtual RenderTexture CreateRenderTexture() { return null; }
    /// <summary>
    ///定义在纹理生成之前所进行的行为
    /// </summary>
    public virtual void OnPreGenerateGUI()
    {
        OnTextureSettingsGUI();
    }

    /// <summary>
    /// 定义在满足重新生成纹理时所进行的行为
    /// </summary>
    public abstract void OnGenerate();
    /// <summary>
    /// 定义纹理生成之后所进行的行为，update代表是否进行了重新生成
    /// </summary>
    /// <param name="update"></param>
    public abstract void OnAfterGenerateGUI(bool update);

    /// <summary>
    /// 如果成功创建了RWTexture的话，就将纹理参数传递到ComputeShader
    /// </summary>
    public void SetRWTextureParameters()
    {
        TextureGenerator.SetFloat("_Width", settings.width);
        TextureGenerator.SetFloat("_Height", settings.height);
        TextureGenerator.SetFloat("_Depth", settings.depth);
        TextureGenerator.SetFloat("_Scale", settings.scale);
        TextureGenerator.SetVector("_Offset", settings.offset);
        int kernel = TextureGenerator.FindKernel(KernelName);
        TextureGenerator.SetTexture(kernel, settings.rwTexName, rwTexture);
    }
    /// <summary>
    /// 通过UI写入纹理的尺寸，缩放和偏移和其他属性
    /// </summary>
    /// <param name="update"></param>
    private void OnTextureSettingsGUI()
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label("渲染纹理 ");
        settings.rwTexName = GUILayout.TextField(settings.rwTexName);
        GUILayout.Label("纹理宽度 ");
        Width = (TextureSize)EditorGUILayout.EnumPopup(Width);
        settings.width = (int)Width;
        GUILayout.Label("纹理高度 ");
        Height = (TextureSize)EditorGUILayout.EnumPopup(Height);
        settings.height = (int)Height;
        GUILayout.Label("纹理深度（仅限3DTexture） ");
        Depth = (TextureSize)EditorGUILayout.EnumPopup(Depth);
        settings.depth = (int)Depth;
        GUILayout.Label("纹理缩放 ");
        settings.scale = EditorGUILayout.Slider(settings.scale, 0.1f, 100.0f);
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        settings.offset = EditorGUILayout.Vector3Field("纹理位移(请输入正数) ", settings.offset);
        GUILayout.EndHorizontal();

        EditorGUILayout.HelpBox("以上是ComputeTexture中自带的GUI", MessageType.Info);
    }
    public void Show(string DirPath,string TextureName) 
    {
        dirPath = DirPath;
        textureName = TextureName;

        bool update;

        if (GUILayout.Button("手动重新生成", GUILayout.Width(200))) update = true;
        else update = false;

        OnPreGenerateGUI();

        update |= ShouldGenerate();

        if (update)
        {
            if (TextureGenerator != null && KernelName != "")
            {
                rwTexture = CreateRenderTexture();

                if (rwTexture.IsCreated())
                {
                    SetRWTextureParameters();
                }

                OnGenerate();
            }
        }

        OnAfterGenerateGUI(update);
    }
}

public abstract class ComputeTexture2D : ComputeTexture
{
    public Texture2D resultTexture;
    public enum OutPutType
    {
        PNG,
        JPG,
        TGA,
        EXR,
        RAW
    }
    private OutPutType outPutType = OutPutType.PNG;
    private string fileExtension = ".png";

    private void SaveAssets()
    {
        if (!Directory.Exists(dirPath))
        {
            Directory.CreateDirectory(dirPath);//没有路径则生成
        }

        string rawPath = dirPath + textureName;
        string Path = rawPath;

        for (uint i = 1; File.Exists(Path + fileExtension); i++) Path = rawPath + "(" + i + ")";

        byte[] bytes = { };
        switch (outPutType)
        {
            case OutPutType.PNG:
                bytes = resultTexture.EncodeToPNG();
                break;
            case OutPutType.JPG:
                bytes = resultTexture.EncodeToJPG();
                break;
            case OutPutType.TGA:
                bytes = resultTexture.EncodeToTGA();
                break;
            case OutPutType.EXR:
                bytes = resultTexture.EncodeToEXR();
                break;
            case OutPutType.RAW:
                bytes = resultTexture.GetRawTextureData();
                break;
        }

        File.WriteAllBytes(Path + fileExtension, bytes);
    }
    private Texture2D GetRWTextureOutPut()
    {
        Texture2D texture = new Texture2D(settings.width, settings.height);
        if (rwTexture == null) return null;

        RenderTexture.active = rwTexture;
        texture.ReadPixels(new Rect(0, 0, settings.width, settings.height), 0, 0);
        texture.Apply();
        return texture;
    }
    public override RenderTexture CreateRenderTexture()
    {
        RenderTexture rt = new RenderTexture(settings.width, settings.height, 24,RenderTextureFormat.ARGB32);
        rt.enableRandomWrite = true;

        if (rt.Create()) 
            return rt;
        else
            return null;
    }

    private Vector2 scrollPos;
    public override void OnAfterGenerateGUI(bool update)
    {
        if(update) resultTexture = GetRWTextureOutPut();

        EditorGUILayout.HelpBox("以下是ComputeTexture2D中自带的GUI", MessageType.Info);
        EditorGUILayout.BeginHorizontal();
        outPutType = (OutPutType)EditorGUILayout.EnumPopup("选择保存类型", outPutType);
        switch (outPutType)
        {
            case OutPutType.PNG:
                fileExtension = ".png";
                break;
            case OutPutType.JPG:
                fileExtension = ".jpg";
                break;
            case OutPutType.TGA:
                fileExtension = ".tga";
                break;
            case OutPutType.EXR:
                fileExtension = ".exr";
                break;
            case OutPutType.RAW:
                fileExtension = ".raw";
                break;
        }

        string path = dirPath + textureName;

        if (GUILayout.Button("保存到"+ path + fileExtension)&& resultTexture != null)
        {
            if (path != "") SaveAssets();
        }
        EditorGUILayout.EndHorizontal();

        GUILayout.Label("纹理浏览：");
        if (resultTexture != null)
        {
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos, true, true);
            EditorGUI.DrawPreviewTexture(new Rect(0,0, resultTexture.width,resultTexture.height), resultTexture,null);//纹理浏览(不好用，考虑自己写一个代替)
            EditorGUILayout.EndScrollView();
        }
    }
}

public abstract class ComputeTexture3D : ComputeTexture
{
    public ComputeShader texture3DSlicer;

    public Texture3D resultTex;

    public override RenderTexture CreateRenderTexture()
    {
        RenderTexture rt = new RenderTexture(settings.width, settings.height, 0, RenderTextureFormat.ARGB32);
        rt.enableRandomWrite = true;
        rt.dimension = TextureDimension.Tex3D;
        rt.volumeDepth = settings.depth;
        rt.Create();

        return rt;
    }
    private RenderTexture Copy3DSlicerToRT(int layer)
    {
        RenderTexture rt = new RenderTexture(settings.width, settings.height, 0, RenderTextureFormat.ARGB32);
        rt.dimension = TextureDimension.Tex2D;
        rt.enableRandomWrite = true;
        rt.wrapMode = TextureWrapMode.Clamp;
        if (!rt.Create()) return null;

        if (texture3DSlicer == null)
            texture3DSlicer = AssetDatabase.LoadAssetAtPath<ComputeShader>("Assets/TextureGenerator/Shaders/NoiseGenerator/Tex3DSlicer.compute");

        int kernelIndex = texture3DSlicer.FindKernel("CSMain");

        texture3DSlicer.SetTexture(kernelIndex, "noise", rwTexture);
        texture3DSlicer.SetInt("layer", layer);
        texture3DSlicer.SetTexture(kernelIndex, "Result", rt);
        texture3DSlicer.Dispatch(kernelIndex, settings.width/32, settings.height/32, 1);

        return rt;
    }
    private Texture2D ConvertFromRenderTexture(RenderTexture rt)
    {
        Texture2D output = new Texture2D(settings.width, settings.height);
        RenderTexture.active = rt;
        output.ReadPixels(new Rect(0, 0, settings.width, settings.height), 0, 0);
        output.Apply();
        return output;
    }
    private Texture3D GetRWTextureOutPut()
    {
        if (rwTexture == null) return null;

        //Slice 3D RenderTexture to layers
        RenderTexture[] layers = new RenderTexture[settings.depth];

        for (int i = 0; i < settings.depth; i++)
            layers[i] = Copy3DSlicerToRT(i);

        Texture2D[] slices = new Texture2D[settings.depth];

        for(int i = 0; i < settings.depth; i++)
            slices[i] = ConvertFromRenderTexture(layers[i]);

        Texture3D output = new Texture3D(settings.width, settings.height, settings.depth, TextureFormat.ARGB32, true);
        output.filterMode = FilterMode.Trilinear;
        Color[] outputPixels = output.GetPixels();

        for (int k = 0; k < settings.depth; k++)
        {
            Color[] layerPixels = slices[k].GetPixels();
            for (int i = 0; i < settings.width; i++)
            {
                for (int j = 0; j < settings.height; j++)
                {
                    outputPixels[i + j * settings.width + k * settings.width * settings.height] = layerPixels[i + j * settings.width];
                }
            }
        }

        output.SetPixels(outputPixels);
        output.Apply();

        return output;
    }

    Vector2 scrollPos;
    public override void OnAfterGenerateGUI(bool update)
    {
        EditorGUILayout.HelpBox("以下是ComputeTexture3D中自带的GUI,就一个保存按钮，剩下的懒得做了", MessageType.Info);

        string path = dirPath + textureName + ".asset";

        if (update)
        {
            EditorUtility.DisplayProgressBar("进度条", "正在重新生成", 0);
            resultTex = GetRWTextureOutPut();
            EditorUtility.ClearProgressBar();
        }

        if (GUILayout.Button("保存到" + path) && resultTex != null)
        {
            if (path != "")
            {
                if (!Directory.Exists(dirPath))
                {
                    Directory.CreateDirectory(dirPath);
                }

                for (uint i = 1; File.Exists(path); i++)
                    path = dirPath + textureName + "(" + i + ")" + ".asset";

                path = path.Substring(path.IndexOf("Assets"));

                AssetDatabase.CreateAsset(resultTex , path);
            }
        }
    }
}
