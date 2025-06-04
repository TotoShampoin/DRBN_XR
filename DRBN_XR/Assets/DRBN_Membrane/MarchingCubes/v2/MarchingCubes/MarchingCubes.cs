using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace MarchingCubing.V2
{
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
        public bool smooth = true;
        public Bounds bounds = new(Vector3.zero, Vector3.one);

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
            Gizmos.DrawWireCube(bounds.center, bounds.size);
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
            marchingCubesShader.SetVector("_Min", bounds.min);
            marchingCubesShader.SetVector("_Max", bounds.max);

            triangleBuffer.SetCounterValue(0);

            marchingCubesShader.Dispatch(
                kernel,
                Mathf.CeilToInt((float)resolution / 8),
                Mathf.CeilToInt((float)resolution / 8),
                Mathf.CeilToInt((float)resolution / 8));

            Triangle[] triangles = new Triangle[ReadTriangleCount()];
            triangleBuffer.GetData(triangles); // [MARKER] Bottleneck

            return smooth
                ? SmoothMeshFromTriangles(triangles)
                : SharpMeshFromTriangles(triangles);
        }

        Mesh SharpMeshFromTriangles(Triangle[] triangles)
        {
            Vector3[] verts = new Vector3[triangles.Length * 3];
            int[] tris = new int[triangles.Length * 3];
            Parallel.For(0, triangles.Length, i =>
            {
                int vi = i * 3;
                verts[vi] = triangles[i].a;
                verts[vi + 1] = triangles[i].b;
                verts[vi + 2] = triangles[i].c;
                tris[vi] = vi;
                tris[vi + 1] = vi + 1;
                tris[vi + 2] = vi + 2;
            });

            Mesh mesh = new()
            {
                vertices = verts,
                triangles = tris
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
}