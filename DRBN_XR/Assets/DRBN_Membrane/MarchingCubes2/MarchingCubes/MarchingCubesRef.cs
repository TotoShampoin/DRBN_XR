using System.Collections.Generic;
using UnityEngine;

public class MarchingCubesRef : MonoBehaviour
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

    readonly List<Vector3> cachedVerts = new();
    readonly List<int> cachedTris = new();
    readonly Dictionary<Vector3, int> cachedVertDict = new();
    readonly List<Triangle> cachedTriangles = new();

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
        GenerateMesh(renderTexture, threshold, meshFilter.mesh);
    }

    public void GenerateMesh(RenderTexture renderTexture, float threshold, Mesh mesh)
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

        int triCount = ReadTriangleCount();
        if (cachedTriangles.Capacity < triCount)
            cachedTriangles.Capacity = triCount;
        cachedTriangles.Clear();
        if (triCount > 0)
        {
            Triangle[] tempTriangles = new Triangle[triCount];
            triangleBuffer.GetData(tempTriangles);
            cachedTriangles.AddRange(tempTriangles);
        }

        if (smooth)
            SmoothMeshFromTriangles(cachedTriangles, mesh);
        else
            SharpMeshFromTriangles(cachedTriangles, mesh);
    }

    void SharpMeshFromTriangles(IList<Triangle> triangles, Mesh mesh)
    {
        cachedVerts.Clear();
        cachedTris.Clear();
        for (int i = 0; i < triangles.Count; i++)
        {
            cachedVerts.Add(triangles[i].a);
            cachedVerts.Add(triangles[i].b);
            cachedVerts.Add(triangles[i].c);
            cachedTris.Add(i * 3);
            cachedTris.Add(i * 3 + 1);
            cachedTris.Add(i * 3 + 2);
        }
        mesh.Clear();
        mesh.SetVertices(cachedVerts);
        mesh.SetTriangles(cachedTris, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
    }

    void SmoothMeshFromTriangles(IList<Triangle> triangles, Mesh mesh)
    {
        cachedVerts.Clear();
        cachedTris.Clear();
        cachedVertDict.Clear();
        for (int i = 0; i < triangles.Count; i++)
        {
            Vector3 a = triangles[i].a;
            Vector3 b = triangles[i].b;
            Vector3 c = triangles[i].c;
            Vector3 ai = a;
            Vector3 bi = b;
            Vector3 ci = c;

            if (!cachedVertDict.TryGetValue(ai, out int indexA))
            {
                indexA = cachedVerts.Count;
                cachedVerts.Add(a);
                cachedVertDict.Add(ai, indexA);
            }
            if (!cachedVertDict.TryGetValue(bi, out int indexB))
            {
                indexB = cachedVerts.Count;
                cachedVerts.Add(b);
                cachedVertDict.Add(bi, indexB);
            }
            if (!cachedVertDict.TryGetValue(ci, out int indexC))
            {
                indexC = cachedVerts.Count;
                cachedVerts.Add(c);
                cachedVertDict.Add(ci, indexC);
            }

            cachedTris.Add(indexA);
            cachedTris.Add(indexB);
            cachedTris.Add(indexC);
        }
        mesh.Clear();
        mesh.SetVertices(cachedVerts);
        mesh.SetTriangles(cachedTris, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
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
