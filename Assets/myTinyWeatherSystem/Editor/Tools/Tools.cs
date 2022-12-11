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
            int escape = 0;
            foreach (var temp in fileName)
            {
                if (temp.Contains("Assets\\3rd"))
                {
                    escape++;
                    continue;
                }

                int nowLine = 0; //当前文件累计行数
                StreamReader sr = new StreamReader(temp);
                while (sr.ReadLine() != null)
                {
                    nowLine++;
                }
                totalLine += nowLine;
            }
            Debug.Log(string.Format("文件后缀{0}:代码总行数: {1} -> 代码文件数:{2}",fileExtension,totalLine, fileName.Length - escape));

            Line += totalLine;
            Length += fileName.Length - escape;
        }
    }
}
