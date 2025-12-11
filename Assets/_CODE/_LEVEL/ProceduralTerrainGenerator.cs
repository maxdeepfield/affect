using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[ExecuteAlways]
[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class ProceduralTerrainGenerator : MonoBehaviour
{
    [Header("Terrain")]
    [SerializeField] private Vector2 size = new Vector2(100f, 100f);
    [SerializeField] private float height = 25f;
    [SerializeField] private float noiseScale = 0.1f;
    [SerializeField] private Material terrainMaterial;
    [SerializeField] private Vector2 uvTiling = new Vector2(10f, 10f);

    [Header("Grass")]
    [SerializeField] private bool enableGrass = true;
    [SerializeField] private int grassCount = 100000;
    [SerializeField] private float grassHeight = 0.5f;
    [SerializeField] private float grassWidth = 0.08f;
    [SerializeField] private Material grassMaterial;
    [SerializeField] private Color grassColorA = new Color(0.3f, 0.5f, 0.25f);
    [SerializeField] private Color grassColorB = new Color(0.45f, 0.7f, 0.35f);

    private MeshFilter meshFilter;
    private MeshCollider meshCollider;
    private Mesh terrainMesh;
    private float[,] heightMap;
    private List<GrassBatch> grassBatches = new List<GrassBatch>();
    private Mesh grassMesh;

    private class GrassBatch
    {
        public Matrix4x4[] matrices;
        public MaterialPropertyBlock props;
        public int count;
    }

    void OnEnable()
    {
        Generate();
    }

    void OnDisable()
    {
        grassBatches.Clear();
    }

    void LateUpdate()
    {
        DrawGrass();
    }

    [ContextMenu("Generate")]
    public void Generate()
    {
        meshFilter = GetComponent<MeshFilter>();
        meshCollider = GetComponent<MeshCollider>();
        
        GenerateTerrain();
        GenerateGrass();
    }

    void GenerateTerrain()
    {
        int res = 64;
        heightMap = new float[res + 1, res + 1];
        
        float offsetX = UnityEngine.Random.Range(0f, 1000f);
        float offsetZ = UnityEngine.Random.Range(0f, 1000f);

        for (int z = 0; z <= res; z++)
        {
            for (int x = 0; x <= res; x++)
            {
                float nx = (float)x / res;
                float nz = (float)z / res;
                float h = Mathf.PerlinNoise(nx * noiseScale * 10f + offsetX, nz * noiseScale * 10f + offsetZ);
                h += Mathf.PerlinNoise(nx * noiseScale * 25f + offsetX, nz * noiseScale * 25f + offsetZ) * 0.5f;
                heightMap[x, z] = h * height;
            }
        }

        // Build mesh
        int vertCount = (res + 1) * (res + 1);
        Vector3[] verts = new Vector3[vertCount];
        Vector2[] uvs = new Vector2[vertCount];
        int[] tris = new int[res * res * 6];

        int ti = 0;
        for (int z = 0; z <= res; z++)
        {
            for (int x = 0; x <= res; x++)
            {
                int i = z * (res + 1) + x;
                float nx = (float)x / res;
                float nz = (float)z / res;
                verts[i] = new Vector3((nx - 0.5f) * size.x, heightMap[x, z], (nz - 0.5f) * size.y);
                uvs[i] = new Vector2(nx * uvTiling.x, nz * uvTiling.y);

                if (x < res && z < res)
                {
                    int next = i + res + 1;
                    tris[ti++] = i;
                    tris[ti++] = next;
                    tris[ti++] = next + 1;
                    tris[ti++] = i;
                    tris[ti++] = next + 1;
                    tris[ti++] = i + 1;
                }
            }
        }

        if (terrainMesh == null)
            terrainMesh = new Mesh { name = "Terrain" };
        else
            terrainMesh.Clear();

        terrainMesh.indexFormat = vertCount > 65000 ? IndexFormat.UInt32 : IndexFormat.UInt16;
        terrainMesh.vertices = verts;
        terrainMesh.uv = uvs;
        terrainMesh.triangles = tris;
        terrainMesh.RecalculateNormals();
        terrainMesh.RecalculateBounds();

        meshFilter.sharedMesh = terrainMesh;
        
        if (terrainMaterial != null)
            GetComponent<MeshRenderer>().sharedMaterial = terrainMaterial;

        if (meshCollider == null)
            meshCollider = gameObject.AddComponent<MeshCollider>();
        meshCollider.sharedMesh = terrainMesh;
    }

    void GenerateGrass()
    {
        grassBatches.Clear();

        if (!enableGrass || grassCount <= 0 || grassMaterial == null)
            return;

        // Always recreate mesh to pick up height/width changes
        grassMesh = CreateGrassBlade();

        const int batchSize = 1023;
        List<Matrix4x4> matrices = new List<Matrix4x4>(batchSize);
        List<Vector4> colors = new List<Vector4>(batchSize);
        List<float> swayOffsets = new List<float>(batchSize);

        int res = heightMap.GetLength(0) - 1;

        for (int i = 0; i < grassCount; i++)
        {
            float nx = UnityEngine.Random.value;
            float nz = UnityEngine.Random.value;

            // Sample height
            float fx = nx * res;
            float fz = nz * res;
            int x0 = Mathf.FloorToInt(fx);
            int z0 = Mathf.FloorToInt(fz);
            int x1 = Mathf.Min(x0 + 1, res);
            int z1 = Mathf.Min(z0 + 1, res);
            float tx = fx - x0;
            float tz = fz - z0;
            float h = Mathf.Lerp(
                Mathf.Lerp(heightMap[x0, z0], heightMap[x1, z0], tx),
                Mathf.Lerp(heightMap[x0, z1], heightMap[x1, z1], tx),
                tz);

            Vector3 pos = transform.TransformPoint(new Vector3(
                (nx - 0.5f) * size.x,
                h + 0.02f,
                (nz - 0.5f) * size.y));

            float scale = UnityEngine.Random.Range(0.8f, 1.4f);
            Quaternion rot = Quaternion.Euler(0, UnityEngine.Random.Range(0f, 360f), 0);

            matrices.Add(Matrix4x4.TRS(pos, rot, Vector3.one * scale));
            colors.Add(Color.Lerp(grassColorA, grassColorB, UnityEngine.Random.value));
            swayOffsets.Add(UnityEngine.Random.value * Mathf.PI * 2f);

            if (matrices.Count >= batchSize)
            {
                CreateBatch(matrices, colors, swayOffsets);
                matrices.Clear();
                colors.Clear();
                swayOffsets.Clear();
            }
        }

        if (matrices.Count > 0)
            CreateBatch(matrices, colors, swayOffsets);
    }

    void CreateBatch(List<Matrix4x4> matrices, List<Vector4> colors, List<float> swayOffsets)
    {
        var batch = new GrassBatch
        {
            matrices = matrices.ToArray(),
            count = matrices.Count,
            props = new MaterialPropertyBlock()
        };
        batch.props.SetVectorArray("_InstanceColor", colors);
        batch.props.SetFloatArray("_SwayOffset", swayOffsets);
        grassBatches.Add(batch);
    }

    void DrawGrass()
    {
        if (!enableGrass || grassMaterial == null || grassMesh == null)
            return;

        foreach (var batch in grassBatches)
        {
            Graphics.DrawMeshInstanced(
                grassMesh, 0, grassMaterial,
                batch.matrices, batch.count,
                batch.props,
                ShadowCastingMode.Off, true,
                gameObject.layer);
        }
    }

    Mesh CreateGrassBlade()
    {
        Mesh m = new Mesh { name = "GrassBlade" };
        float w = grassWidth;
        float h = grassHeight;
        
        // Triangular blade - pointed at top
        m.vertices = new Vector3[]
        {
            new Vector3(-w, 0, 0),      // bottom left
            new Vector3(w, 0, 0),       // bottom right
            new Vector3(0, h, 0)        // top point
        };
        m.uv = new Vector2[]
        {
            new Vector2(0, 0),
            new Vector2(1, 0),
            new Vector2(0.5f, 1)
        };
        m.triangles = new int[] { 0, 2, 1 };
        m.RecalculateNormals();
        return m;
    }
}
