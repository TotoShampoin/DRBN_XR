using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Collections;
using UnityEngine;

public class MarchingCubes : MonoBehaviour
{
    struct Triangle
    {
        public Vector3 a;
        public Vector3 b;
        public Vector3 c;
        public static int Size => sizeof(float) * 3 * 3;
    }

    [SerializeField] ComputeShader marchingCubesShader;

    ComputeBuffer triangleBuffer;
    ComputeBuffer triangleCountBuffer;
    MeshFilter meshFilter;

    public int resolution = 32;
    // public float threshold = 0.0f;
    public bool smooth = true;

    void OnEnable()
    {
        meshFilter = meshFilter != null
            ? meshFilter : GetComponent<MeshFilter>();
    }

    void OnDisable()
    {
        triangleBuffer?.Release();
        triangleCountBuffer?.Release();
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.white;
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawWireCube(Vector3.zero, new Vector3(1, 1, 1));
    }

    public void ClearMesh()
    {
        meshFilter.mesh = new();
    }

    public void GenerateAndApplyMesh(RenderTexture renderTexture, float threshold)
    {
        ApplyMesh(GenerateMesh(renderTexture, threshold));
    }
    public void ApplyMesh(Mesh mesh)
    {
        meshFilter.mesh = mesh;
    }

    public Mesh GenerateMesh(RenderTexture renderTexture, float threshold)
    {
        PrepareBuffer();

        var kernel = marchingCubesShader.FindKernel("MarchingCubes");

        marchingCubesShader.SetBuffer(kernel, "_Triangles", triangleBuffer);
        marchingCubesShader.SetTexture(kernel, "_Input", renderTexture);

        marchingCubesShader.SetInt("_Resolution", resolution);
        marchingCubesShader.SetFloat("_Threshold", threshold);

        triangleBuffer.SetCounterValue(0);

        marchingCubesShader.Dispatch(
            kernel,
            Mathf.CeilToInt((float)resolution / 8),
            Mathf.CeilToInt((float)resolution / 8),
            Mathf.CeilToInt((float)resolution / 8));

        Triangle[] triangles = new Triangle[ReadTriangleCount()];
        triangleBuffer.GetData(triangles);

        return smooth
            ? SmoothMeshFromTriangles(triangles)
            : SharpMeshFromTriangles(triangles);
    }

    Mesh SharpMeshFromTriangles(Triangle[] triangles)
    {
        List<Vector3> verts = new(triangles.Length * 3);
        List<int> tris = new(triangles.Length * 3);
        Parallel.For(0, triangles.Length, i =>
        {
            verts.Add(triangles[i].a);
            verts.Add(triangles[i].b);
            verts.Add(triangles[i].c);
            tris.Add(i * 3);
            tris.Add(i * 3 + 1);
            tris.Add(i * 3 + 2);
        });

        Mesh mesh = new()
        {
            vertices = verts.ToArray(),
            triangles = tris.ToArray()
        };
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }
    Mesh SmoothMeshFromTriangles(Triangle[] triangles)
    {
        List<Vector3> verts = new(triangles.Length * 3);
        List<int> tris = new(triangles.Length * 3);
        Dictionary<Vector3, int> vertDict = new(triangles.Length * 3);

        for (int i = 0; i < triangles.Length; i++)
        {
            Vector3 a = triangles[i].a;
            Vector3 b = triangles[i].b;
            Vector3 c = triangles[i].c;
            Vector3 ai = a;
            Vector3 bi = b;
            Vector3 ci = c;

            if (!vertDict.TryGetValue(ai, out int indexA))
            {
                indexA = verts.Count;
                verts.Add(a);
                vertDict.Add(ai, indexA);
            }
            if (!vertDict.TryGetValue(bi, out int indexB))
            {
                indexB = verts.Count;
                verts.Add(b);
                vertDict.Add(bi, indexB);
            }
            if (!vertDict.TryGetValue(ci, out int indexC))
            {
                indexC = verts.Count;
                verts.Add(c);
                vertDict.Add(ci, indexC);
            }

            tris.Add(indexA);
            tris.Add(indexB);
            tris.Add(indexC);
        }

        Mesh mesh = new()
        {
            vertices = verts.ToArray(),
            triangles = tris.ToArray()
        };
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    void PrepareBuffer()
    {
        var size = 5 * resolution * resolution * resolution;
        if (triangleBuffer == null || triangleBuffer.count != size)
        {
            triangleBuffer?.Release();
            triangleBuffer = new(size, Triangle.Size, ComputeBufferType.Append);
        }
    }
    int ReadTriangleCount()
    {
        int[] triCount = { 0 };
        triangleCountBuffer ??= new(1, sizeof(int), ComputeBufferType.Raw);
        ComputeBuffer.CopyCount(triangleBuffer, triangleCountBuffer, 0);
        triangleCountBuffer.GetData(triCount);
        return triCount[0];
    }
}
