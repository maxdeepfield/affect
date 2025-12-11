using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class ProceduralTerrainGeneratorTests
{
    private GameObject _go;
    private ProceduralTerrainGenerator _generator;
    private Material _grassMaterial;

    [SetUp]
    public void SetUp()
    {
        _go = new GameObject("ProceduralTerrainGenerator_Test");
        _generator = _go.AddComponent<ProceduralTerrainGenerator>();

        _grassMaterial = CreateGrassMaterial();

        SetField("generateOnStart", false);
        SetField("useRandomSeed", false);
        SetField("seed", 1234);
        SetField("resolution", new Vector2Int(4, 4));
        SetField("size", new Vector2(10f, 8f));
        SetField("height", 5f);
        SetField("addMeshCollider", true);
        SetField("spawnGrass", true);
        SetField("grassInstances", 16);
        SetField("grassMaterial", _grassMaterial);
    }

    [TearDown]
    public void TearDown()
    {
        if (_grassMaterial != null)
        {
            Object.DestroyImmediate(_grassMaterial);
        }
        if (_go != null)
        {
            Object.DestroyImmediate(_go);
        }
    }

    [Test]
    public void Generate_BuildsMeshAndCollider()
    {
        _generator.Generate();

        MeshFilter mf = _go.GetComponent<MeshFilter>();
        MeshCollider mc = _go.GetComponent<MeshCollider>();

        Assert.NotNull(mf);
        Assert.NotNull(mc);
        Assert.NotNull(mf.sharedMesh);
        Assert.AreSame(mf.sharedMesh, mc.sharedMesh);

        Mesh mesh = mf.sharedMesh;
        Assert.AreEqual(25, mesh.vertexCount); // (4+1)^2
        Assert.AreEqual(96, mesh.triangles.Length); // 4*4*6

        float minY = float.MaxValue;
        float maxY = float.MinValue;
        foreach (var v in mesh.vertices)
        {
            minY = Mathf.Min(minY, v.y);
            maxY = Mathf.Max(maxY, v.y);
        }

        Assert.That(minY, Is.GreaterThanOrEqualTo(-0.001f));
        Assert.That(maxY, Is.LessThanOrEqualTo(5f + 0.001f));
    }

    [Test]
    public void Generate_SetsGrassInstancingOnMaterial()
    {
        _generator.Generate();
        Assert.IsTrue(_grassMaterial.enableInstancing);
    }

    private void SetField(string fieldName, object value)
    {
        FieldInfo field = typeof(ProceduralTerrainGenerator).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(field, $"Field '{fieldName}' not found on ProceduralTerrainGenerator");
        field.SetValue(_generator, value);
    }

    private Material CreateGrassMaterial()
    {
        Shader shader = Shader.Find("AFFECT/Foliage/ProceduralGrass");
        if (shader == null)
            shader = Shader.Find("Universal Render Pipeline/Lit");

        Assert.NotNull(shader, "Unable to find a shader for grass material.");
        return new Material(shader);
    }
}
