using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Yu_Weather
{
    public class FFTOceanCreater
    {
        [MenuItem("GameObject/Water/FFTOcean", false, -1)]
        static void CreateAtmosphereLight(MenuCommand command)
        {
            if (GameObject.FindObjectsOfType<FFTOcean>().Length > 0)
            {
                Debug.LogError("目前还不支持多水体，Sorry。");
                return;
            }

            GameObject go = new GameObject("FFT Ocean");
            go.layer = LayerMask.NameToLayer("Water");
            go.AddComponent<FFTOcean>();
            go.AddComponent<PlanarReflection>();
            go.GetComponent<PlanarReflection>().hideFlags = HideFlags.HideInInspector;

            GameObjectUtility.SetParentAndAlign(go, command.context as GameObject);
            Undo.RegisterCreatedObjectUndo(go, "Create " + go.name);
            Selection.activeObject = go;
        }
    }
}
