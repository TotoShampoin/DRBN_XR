using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Profiling;
using UnityEngine;

namespace MarchingCubing.V2
{
    /// <summary>
    /// Transforms a 3D volume into a 3D mesh, which's surface is at a given threshold. The same as MarchingCubes, except it does zero memory reallocation.
    /// </summary>
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
        public bool smooth = true;
        public Bounds bounds = new(Vector3.zero, Vector3.one);

        readonly List<Vector3> cachedVerts = new();
        readonly List<int> cachedTris = new();
        readonly Dictionary<Vector3, int> cachedVertDict = new();
        readonly List<Triangle> cachedTriangles = new();

        static readonly ProfilerMarker prepareMarker = new("Membrane.MarchingCube.Prepare");
        static readonly ProfilerMarker marchMarker = new("Membrane.MarchingCube.March");
        static readonly ProfilerMarker readbackMarker = new("Membrane.MarchingCube.GpuRead");
        static readonly ProfilerMarker parseMarker = new("Membrane.MarchingCube.MeshConversion");

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
            GenerateMesh(renderTexture, threshold, meshFilter.mesh);
        }

        public void GenerateMesh(RenderTexture renderTexture, float threshold, Mesh mesh)
        {
            using (prepareMarker.Auto())
            {
                PrepareBuffer();
            }

            using (marchMarker.Auto())
            {
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
            }

            int triCount = ReadTriangleCount();
            using (readbackMarker.Auto())
            {
                if (cachedTriangles.Capacity < triCount)
                    cachedTriangles.Capacity = triCount;
                cachedTriangles.Clear();
                if (triCount > 0)
                {
                    Triangle[] tempTriangles = new Triangle[triCount];
                    triangleBuffer.GetData(tempTriangles);
                    cachedTriangles.AddRange(tempTriangles);
                }
            }

            using (parseMarker.Auto())
            {
                if (smooth)
                    SmoothMeshFromTriangles(cachedTriangles, mesh);
                else
                    SharpMeshFromTriangles(cachedTriangles, mesh);
            }
        }

        void SharpMeshFromTriangles(IList<Triangle> triangles, Mesh mesh)
        {
            cachedVerts.Clear();
            cachedTris.Clear();
            int triCount = triangles.Count;
            cachedVerts.AddRange(new Vector3[triCount * 3]);
            cachedTris.AddRange(new int[triCount * 3]);
            Parallel.For(0, triCount, i =>
            {
                var tri = triangles[i];
                int vIdx = i * 3;
                cachedVerts[vIdx] = tri.a;
                cachedVerts[vIdx + 1] = tri.b;
                cachedVerts[vIdx + 2] = tri.c;
                cachedTris[vIdx] = vIdx;
                cachedTris[vIdx + 1] = vIdx + 1;
                cachedTris[vIdx + 2] = vIdx + 2;
            });
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
}