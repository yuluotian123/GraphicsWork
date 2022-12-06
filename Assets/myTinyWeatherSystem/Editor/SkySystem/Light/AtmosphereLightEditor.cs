using UnityEditor;
using UnityEngine;

namespace Yu_Weather
{
    public class AtmosphereLightCreater
    {

        [MenuItem("GameObject/Light/Atmosphere Light", false, -1)]
        static void CreateAtmosphereLight(MenuCommand command)
        {
            if (GameObject.FindObjectsOfType<AtmosphereLight>().Length>=2)
            {
                Debug.LogError("场景中已经有两个大气光源了。");
                return;
            }

            GameObject go = new GameObject("Atmosphere Light");
            go.AddComponent<AtmosphereLight>();

            GameObjectUtility.SetParentAndAlign(go, command.context as GameObject);
            Undo.RegisterCreatedObjectUndo(go, "Create " + go.name);
            Selection.activeObject = go;
        }
    }

    [CustomEditor(typeof(AtmosphereLight))]
    public class AtmosphereLightEditor :Editor
    {
        //to do
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
        }

        protected static readonly Color kGizmoLight = new Color(254f / 255f, 253f / 255f, 8f / 15f, 128f / 255f);
        protected static readonly Color kGizmoDisabledLight = new Color(0.5294118f, 116f / 255f, 10f / 51f, 128f / 255f);

        static readonly Vector3[] directionalLightHandlesRayPositions =
{
            new Vector3(1, 0, 0),
            new Vector3(-1, 0, 0),
            new Vector3(0, 1, 0),
            new Vector3(0, -1, 0),
            new Vector3(1, 1, 0).normalized,
            new Vector3(1, -1, 0).normalized,
            new Vector3(-1, 1, 0).normalized,
            new Vector3(-1, -1, 0).normalized
        };
        private  void OnSceneGUI()
        {
            if (this.targets == null)
                return;

            AtmosphereLight light = (AtmosphereLight)target;

            if (light.enabled)
            {
                Handles.color = kGizmoLight;
            }
            else
            {
                Handles.color = kGizmoDisabledLight;
            }

            Vector3 position = light.transform.position;
            float handleSize;
            using (new Handles.DrawingScope(Matrix4x4.identity))
            {
                handleSize = HandleUtility.GetHandleSize(position);
            }

            float num = handleSize * 0.2f;
            using (new Handles.DrawingScope(Matrix4x4.TRS(position, light.transform.rotation, Vector3.one)))
            {
                Handles.DrawWireDisc(Vector3.zero, Vector3.forward, num);
                Vector3[] array = directionalLightHandlesRayPositions;
                foreach (Vector3 vector in array)
                {
                    Vector3 vector2 = vector * num;
                    Handles.DrawLine(vector2, vector2 + new Vector3(0f, 0f, handleSize));
                }
            }

        }
    }



}
