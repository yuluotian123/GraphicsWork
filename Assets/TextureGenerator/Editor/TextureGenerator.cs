using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class TextureGenerator : EditorWindow
{
    private static TextureGenerator Generator;

    public enum TextureType
    {
        NOISE_2D = 0,
        NOISE_3D = 1,
        CUSTOM
    }
    private TextureType dimension = TextureType.NOISE_2D;

    public static List<Type> ComputeTexTypeList;
    public static string[] TypeNameList;

    public static string AssetsPath = "TextureGenerator/SavedTextures";
    public static string TextureName = "Image";

    private ComputeTexture ComputeTex;
    private int index;

   [MenuItem("Tools/TextureGenerator")]
    public static void openGenerator()
    {
        Generator = GetWindow<TextureGenerator>(false, "TextureGenerator", true);

        ComputeTexTypeList = GetAllComputeTextureType<ComputeTexture>();

        TypeNameList = new string[ComputeTexTypeList.Count];
        for(int i=0;i<ComputeTexTypeList.Count;i++) 
            TypeNameList[i] = ComputeTexTypeList[i].Name;

        Generator.Show();
    }

    private void OnGUI()
    {
        var dirPath = Application.dataPath + "/" + AssetsPath + "/";
        GUILayout.Label("文件夹路径:" + dirPath);
        TextureName = EditorGUILayout.TextField(TextureName);
        dimension = (TextureType)EditorGUILayout.EnumPopup("选择生成的纹理类型", dimension);

        switch (dimension)
        {
            case TextureType.NOISE_2D:
                UpdateComputeTexture(typeof(ComputeNoise2D));
                break;
            case TextureType.NOISE_3D:
                UpdateComputeTexture(typeof(ComputeNoise3D));
                break;
            case TextureType.CUSTOM:
                index = EditorGUILayout.Popup("可选ComputeTex",index,TypeNameList);
                UpdateComputeTexture(ComputeTexTypeList[index]);
                break;
        }

        if(ComputeTex!=null) 
            ComputeTex.Show(dirPath,TextureName);
    }

    private static List<Type> GetAllComputeTextureType<T>() where T : ComputeTexture
    {
        return GetAllComputeTextureType(typeof(T));
    }
    private static List<Type> GetAllComputeTextureType(Type type)
    {
        List<Type> typeList = new List<Type>();

        var AllTypes = type.Assembly.GetTypes();

        foreach (var t in AllTypes)
        {
            if (!t.IsAbstract && !t.IsInterface && t.IsSubclassOf(type))
            {
               
                typeList.Add(t);
            }
        }

        return typeList;
    }

    private void UpdateComputeTexture(Type type)
    {
        if (ComputeTex == null || ComputeTex.GetType() != type)
        {
            ComputeTex = (ComputeTexture)type.Assembly.CreateInstance(type.Name);
            ComputeTex.OnInit();
        }
    }
}
