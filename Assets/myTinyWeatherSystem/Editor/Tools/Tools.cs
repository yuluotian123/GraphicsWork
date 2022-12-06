using System;
using UnityEngine;
using UnityEditor;
using System.IO;
using UnityEditor.Callbacks;

namespace Yu_Weather
{
    public class CalcCodeLine
    {
        static string calcPath = "Assets"; //代码统计路径【可自定义】

        static string[] fileExtension = { "*.cs", "*.shader", "*.compute", "*.hlsl" };

        [MenuItem("Tools/统计代码行数")]
        static void CalcCode()
        {
            if (!Directory.Exists(calcPath))
            {
                Debug.LogError(string.Format("Path Not Exist  : \"{0}\" ", calcPath));
                return;
            }

            int fileKinds = fileExtension.Length;
            int totalLine = 0;
            int totalFileLength = 0;

            for(int i = 0; i < fileKinds; i++)
            {
                string[] fileName = Directory.GetFiles(calcPath, fileExtension[i], SearchOption.AllDirectories);

                GetCalLog(fileName, fileExtension[i], ref totalLine, ref totalFileLength);
            }

            Debug.Log(string.Format("代码总行数: {0} -> 代码文件数:{1}",totalLine,totalFileLength));
        }

        static private void GetCalLog(in string[] fileName,in string fileExtension,ref int Line,ref int Length)
        {
            int totalLine = 0; //代码总行数
            foreach (var temp in fileName)
            {
                int nowLine = 0; //当前文件累计行数
                StreamReader sr = new StreamReader(temp);
                while (sr.ReadLine() != null)
                {
                    nowLine++;
                }
                totalLine += nowLine;
            }
            Debug.Log(string.Format("文件后缀{0}:代码总行数: {1} -> 代码文件数:{2}",fileExtension,totalLine, fileName.Length));

            Line += totalLine;
            Length += fileName.Length;
        }
    }

    public class ShaderVS
    {
        [MenuItem("Tools/启用VSCode编辑Shader文件")]
        public static void OpenVSCode()
        {
            UnityEditor.Compilation.CompilationPipeline.RequestScriptCompilation();
        }
    }

    public class ShaderEditor
    {
        [OnOpenAssetAttribute(1)]
        public static bool step1(int instanceID, int line)
        {
            string path = AssetDatabase.GetAssetPath(EditorUtility.InstanceIDToObject(instanceID));
            string name = Application.dataPath + "/" + path.Replace("Assets/", "");
            if (name.EndsWith(".shader"))    //文件扩展名类型
            {
                System.Diagnostics.Process process = new System.Diagnostics.Process();
                System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
                startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
                startInfo.FileName = "D:/vscode/Code.exe";   //VSCODE程序
                startInfo.Arguments = name;
                process.StartInfo = startInfo;
                process.Start();
                return true;
            }

            return false;
        }
    }
}
