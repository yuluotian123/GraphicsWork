using UnityEngine;
using UnityEngine.Rendering;

namespace Yu_Weather
{
    [ExecuteInEditMode]
    public class VolumetricCloudController : MonoBehaviour
    {
        [Foldout("位置")]
        public float CloudRange;
        public float LowestCloudAltitude;

        [Foldout("形状控制")]
        public Texture2D cloudMap;
        public Vector4 CloudMapTiling;

        public Texture2D cloudLut;

        public Texture3D baseNoiseTex;
        public Texture3D detailNoiseTex;
        public float ShapeFactor;
        public float ErosionFactor;
        public float ShapeScale;
        public float ErosionScale;

        [Foldout("风")]
        public Vector2 WindDirection;
        public Vector2 WindVector;

        [Foldout("性能")]
        public int NumPrimarySteps;
        public int NumLightSteps;
        public float MaxRayMarchingDistance;


        void Start()
        {

        }

        // UpdateByDefault is called once per frame
        void Update()
        {

        }
    }
}
