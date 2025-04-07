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

    public RenderTexture renderTexture;
    public int resolution = 32;
    public float threshold = 0.0f;

    void Update()
    {
        if (!renderTexture) return;
        meshFilter.sharedMesh = GenerateMesh();
    }

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

    public Mesh GenerateMesh()
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

        // return MeshFromTriangles(triangles);
        return SmoothMeshFromTriangles(triangles);
    }

    Mesh MeshFromTriangles(Triangle[] triangles)
    {
        Vector3[] verts = new Vector3[triangles.Length * 3];
        int[] tris = new int[triangles.Length * 3];

        Parallel.For(0, triangles.Length, i =>
        {
            int startIndex = i * 3;
            verts[startIndex] = triangles[i].a;
            verts[startIndex + 1] = triangles[i].b;
            verts[startIndex + 2] = triangles[i].c;
            tris[startIndex] = startIndex;
            tris[startIndex + 1] = startIndex + 1;
            tris[startIndex + 2] = startIndex + 2;
        });

        Mesh mesh = new() { vertices = verts, triangles = tris };
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }
    Vector3Int ToVector3Int(Vector3 v)
    {
        return new(
            (int)(v.x * resolution),
            (int)(v.y * resolution),
            (int)(v.z * resolution));
    }
    Mesh SmoothMeshFromTriangles(Triangle[] triangles)
    {
        // The strategy: Remove all duplicate vertices, and have the triangles point to the unique vertices.
        int[] tris = new int[triangles.Length * 3];
        Vector3[] verts = new Vector3[triangles.Length * 3];
        Dictionary<Vector3Int, int> vertDict = new(triangles.Length * 3);

        int vertCount = 0;
        for (int i = 0; i < triangles.Length; i++)
        {
            int startIndex = i * 3;
            Vector3 a = triangles[i].a;
            Vector3 b = triangles[i].b;
            Vector3 c = triangles[i].c;
            Vector3Int ai = ToVector3Int(a);
            Vector3Int bi = ToVector3Int(b);
            Vector3Int ci = ToVector3Int(c);

            if (!vertDict.TryGetValue(ai, out int indexA))
            {
                indexA = vertCount;
                verts[vertCount] = a;
                vertDict[ai] = vertCount++;
            }
            if (!vertDict.TryGetValue(bi, out int indexB))
            {
                indexB = vertCount;
                verts[vertCount] = b;
                vertDict[bi] = vertCount++;
            }
            if (!vertDict.TryGetValue(ci, out int indexC))
            {
                indexC = vertCount;
                verts[vertCount] = c;
                vertDict[ci] = vertCount++;
            }

            tris[startIndex] = indexA;
            tris[startIndex + 1] = indexB;
            tris[startIndex + 2] = indexC;
        }
        // Resize the verts array to the number of unique vertices.
        Vector3[] uniqueVerts = new Vector3[vertCount];
        for (int i = 0; i < vertCount; i++)
        {
            uniqueVerts[i] = verts[i];
        }
        // Create the mesh with the unique vertices and triangles.
        Mesh mesh = new() { vertices = uniqueVerts, triangles = tris };
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
