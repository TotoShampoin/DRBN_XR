using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Assets.Voxelization;
using UnityEngine;
using UnityEngine.Rendering;

namespace Assets.SpringSim.V2
{

    public class MeshFromSprings : MonoBehaviour
    {
        MeshFilter meshFilter;
        Voxelizer voxelizer;
        MarchingCubes marchingCubes;

        public RenderTexture renderTexture;
        public RenderTexture normalTexture;

        public int Resolution { get => marchingCubes.resolution; set => marchingCubes.resolution = value; }

        void Start()
        {
            meshFilter = GetComponent<MeshFilter>();
            voxelizer = GetComponent<Voxelizer>();
            marchingCubes = GetComponent<MarchingCubes>();
        }

        public void SetMesh(Mesh mesh)
        {
            meshFilter.mesh = FetchMesh(mesh);
        }
        public Mesh FetchMesh(Mesh mesh)
        {
            var vertices = mesh.vertices;
            Parallel.For(0, vertices.Length, (i) => vertices[i] *= 0.5f);
            mesh.vertices = vertices;
            mesh.RecalculateBounds();
            voxelizer.Voxelize(mesh, renderTexture, normalTexture);
            return marchingCubes.GenerateMesh(renderTexture, 0);
        }

        static public void CleanupMesh(Mesh toCleanUp, Mesh oldMesh, float tolerance = 0.2f)
        {
            // Why does this delete half of my vertices

            var verts = toCleanUp.vertices;
            var norms = toCleanUp.normals;
            var trigs = toCleanUp.triangles;

            var distances = MeshMod.DistanceOfVertices(verts, oldMesh.vertices);
            var toKeep = verts.Where((_, i) => distances[i] < tolerance).Select((_, i) => i).ToHashSet();

            List<(Vector3 v, Vector3 n, int i)> vertexNormals = new(toCleanUp.vertexCount);
            List<int> triangles = new(trigs.Length);
            for (int i = 0; i < trigs.Length / 3; i++)
            {
                var i0 = trigs[i * 3 + 0];
                var i1 = trigs[i * 3 + 1];
                var i2 = trigs[i * 3 + 2];
                var vn0 = (verts[i0], norms[i0], i0);
                var vn1 = (verts[i1], norms[i1], i1);
                var vn2 = (verts[i2], norms[i2], i2);
                if (toKeep.Contains(i0) && toKeep.Contains(i1) && toKeep.Contains(i2))
                {
                    int idx0 = vertexNormals.FindIndex(vn => vn.i == i0);
                    if (idx0 == -1)
                    {
                        idx0 = vertexNormals.Count;
                        vertexNormals.Add(vn0);
                    }
                    int idx1 = vertexNormals.FindIndex(vn => vn.i == i1);
                    if (idx1 == -1)
                    {
                        idx1 = vertexNormals.Count;
                        vertexNormals.Add(vn1);
                    }
                    int idx2 = vertexNormals.FindIndex(vn => vn.i == i2);
                    if (idx2 == -1)
                    {
                        idx2 = vertexNormals.Count;
                        vertexNormals.Add(vn2);
                    }
                    triangles.Add(idx0);
                    triangles.Add(idx1);
                    triangles.Add(idx2);
                }
            }

            toCleanUp.Clear();
            toCleanUp.vertices = vertexNormals.Select(vn => vn.v).ToArray();
            toCleanUp.normals = vertexNormals.Select(vn => vn.n).ToArray();
            toCleanUp.triangles = triangles.ToArray();
        }
    }

}
