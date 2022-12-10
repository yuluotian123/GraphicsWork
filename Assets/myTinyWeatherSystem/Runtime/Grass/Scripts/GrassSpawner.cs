using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Yu_Weather
{

    [ExecuteInEditMode]
    public class GrassSpawner : MonoBehaviour
    {
        public Terrain terrain;
        private float terrainX;
        private float terrainZ;
        private float terrainHeight;
        private Vector3 terrainStartPosition;

        public int MaxGrassRangeX = 2000;
        public int MaxGrassRangeZ = 2000;
        public Vector2 targetStartOffset = Vector2.zero;
        public int grassRowCount = 512;
        public int grassCountPerPatch = 10;

        public Material grassMat;

        //variableCount for GrassGeneration(to do)
        public bool variableCountPerPatch = false;
        public int MaxCount = 20;
        public int MinCount = 5;

        [Header("Height(为什么不直接改InspectorUI？因为我懒)")]
        public float seaLevel = -25;
        public float maxLevel =  55;
        [Header("Texture(为什么不直接改InspectorUI？因为我懒)")]
        //Mask图
        public bool UseGenerateMap;
        public Texture2D GenerateMap;
        public float MaxCountPerPatch = 50;

        //点云生草
        private List<Vector3> verts = new List<Vector3>();

        public void GenerateGrassArea()
        {
            List<int> indices = new List<int>();
            //Unity网格顶点上限65535
            for (int i = 0; i < 65000; i++)
            {
                indices.Add(i);
            }
            //设置循环起始位置
            //直接生成了整个地形的Mesh
            var startPosition = terrainStartPosition;
            var patchSize = new Vector3(terrainX / grassRowCount, 0, terrainZ / grassRowCount);

            for (int y = 0; y < grassRowCount; y++)
            {
                for (int x = 0; x < grassRowCount; x++)
                {
                    int grassCount;
                    if (UseGenerateMap && GenerateMap)
                    {
                        Vector2 coords = new Vector2(GenerateMap.texelSize.x * x / grassRowCount, GenerateMap.texelSize.y * y / grassRowCount);
                        grassCount = (int)(MaxCountPerPatch * GenerateMap.GetPixel((int)coords.x, (int)coords.y).r);
                    }
                    else if (variableCountPerPatch)
                    {
                        grassCount = Random.Range(MinCount, MaxCount);
                    }
                    else
                        grassCount = grassCountPerPatch;

                    GenerateGrass(startPosition, patchSize, grassCount);
                    startPosition.x += patchSize.x;
                }

                startPosition.x = terrainStartPosition.x;
                startPosition.z += patchSize.z;
            }


            Mesh mesh;
            GameObject grassLayer;
            MeshFilter meshFilter;
            MeshRenderer renderer;

            int a = 0;
            while (verts.Count > 65000)
            {
                mesh = new Mesh();
                mesh.vertices = verts.GetRange(0, 65000).ToArray();
                mesh.SetIndices(indices.ToArray(), MeshTopology.Points, 0);

                grassLayer = new GameObject("grassLayer " + a++);
                grassLayer.tag = "Grass";
                grassLayer.layer = LayerMask.NameToLayer("Grass");
                meshFilter = grassLayer.AddComponent<MeshFilter>();
                renderer = grassLayer.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = grassMat;
                meshFilter.mesh = mesh;
                verts.RemoveRange(0, 65000);
            }

            grassLayer = new GameObject("grassLayer" + a);
            grassLayer.tag = "Grass";
            grassLayer.layer = LayerMask.NameToLayer("Grass");
            mesh = new Mesh();
            mesh.vertices = verts.ToArray();
            mesh.SetIndices(indices.GetRange(0, verts.Count).ToArray(), MeshTopology.Points, 0);
            meshFilter = grassLayer.AddComponent<MeshFilter>();
            renderer = grassLayer.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = grassMat;
            meshFilter.mesh = mesh;

        }

        public void GenerateGrass(Vector3 pos, Vector3 patchSize, int grassCountPerPatch)
        {
            for (int i = 0; i < grassCountPerPatch; i++)
            {
                var randomX = Random.value * patchSize.x;
                var randomZ = Random.value * patchSize.z;

                int indexX = (int)(pos.x + randomX);
                int indexZ = (int)(pos.z + randomZ);

                if (indexX >= terrainX)
                {
                    indexX = (int)terrainX - 1;
                }

                if (indexZ >= terrainX)
                {
                    indexZ = (int)terrainZ - 1;
                }

                Vector3 curPos = new Vector3(indexX, 0, indexZ);
                float Height = terrain.SampleHeight(curPos) + terrainStartPosition.y;
                Vector3 currentPos = new Vector3(pos.x + randomX, Height, pos.z + randomZ);

                float min = seaLevel + Random.Range(-5.0f, 0.0f);
                float max = maxLevel + Random.Range(-5.0f, 10.0f);

                if (Height >= min && Height <= max)
                {
                    verts.Add(currentPos);
                }
            }
        }

        //to do(可能会改用Instance，然后做HiZ)
        public bool IsPointInFrustum(Vector3 point,Camera cam)
        {
            Plane[] planes = GetFrustumPlanes(cam);

            for (int i = 0, iMax = planes.Length; i < iMax; ++i)
            {
                //判断一个点是否在平面的正方向上
                if (!planes[i].GetSide(point))
                {
                    return false;
                }
            }
            return true;
        }
        private Plane[] GetFrustumPlanes(Camera cam)
        {
            return GeometryUtility.CalculateFrustumPlanes(cam);
        }

        public void Start()
        {
            GameObject[] grassLayer = GameObject.FindGameObjectsWithTag("Grass");
            foreach (var grass in grassLayer)
            {
                if (Application.isEditor)
                    DestroyImmediate(grass);
                else
                    Destroy(grass);
            }

            verts.Clear();

            if (terrain == null)
                terrain = GetComponentInChildren<Terrain>();

            terrainX = Mathf.Min(terrain.terrainData.size.x, MaxGrassRangeX) - targetStartOffset.x;
            terrainZ = Mathf.Min(terrain.terrainData.size.z, MaxGrassRangeZ) - targetStartOffset.y;
            terrainHeight = terrain.terrainData.size.y;

            terrainStartPosition = terrain.GetPosition();
            terrainStartPosition.x += targetStartOffset.x;
            terrainStartPosition.y += targetStartOffset.y;

            GenerateGrassArea();

        }
    }

}