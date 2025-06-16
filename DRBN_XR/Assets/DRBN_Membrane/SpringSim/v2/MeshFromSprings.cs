using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Voxelization;
using UnityEngine;
using MarchingCubing.V2;
using Unity.Profiling;

namespace SpringSim.V2
{

    public class MeshFromSprings : MonoBehaviour
    {
        MeshFilter meshFilter;
        Voxelizer voxelizer;
        MarchingCubes marchingCubes;

        public RenderTexture renderTexture;
        public RenderTexture normalTexture;

        public int Resolution { get => marchingCubes.resolution; set => marchingCubes.resolution = value; }

        static readonly ProfilerMarker fetchMarker = new("Membrane.MeshFromSprings.FetchMesh");
        static readonly ProfilerMarker cleanupMarker = new("Membrane.MeshFromSprings.CleanupMesh");

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
            using (fetchMarker.Auto())
            {
                var vertices = mesh.vertices;
                Parallel.For(0, vertices.Length, (i) => vertices[i] *= 0.5f);
                mesh.vertices = vertices;
                mesh.RecalculateBounds();
                // voxelizer.Voxelize(mesh, renderTexture, normalTexture);
                voxelizer.Voxelize(mesh, renderTexture);
                return marchingCubes.GenerateMesh(renderTexture, 0);
            }
        }

        static public Mesh CleanupMesh(Mesh toCleanUp, Mesh oldMesh, float tolerance = 0.2f)
        {
            using (cleanupMarker.Auto())
            {
                // return CleanupMeshByVertex(toCleanUp, oldMesh, tolerance);
                return CleanupMeshByCluster(toCleanUp, oldMesh, tolerance);
            }
        }

        static public Mesh CleanupMeshByVertex(Mesh toCleanUp, Mesh oldMesh, float tolerance)
        {
            var verts = toCleanUp.vertices;
            var norms = toCleanUp.normals;
            var trigs = toCleanUp.triangles;

            var distances = MeshMod.DistanceOfVertices(verts, oldMesh.vertices);
            var toKeep = verts
                .Select((v, i) => (v, i))
                .Where(pair => distances[pair.i] < tolerance)
                .Select(pair => pair.i)
                .ToHashSet();

            // Fast remapping: old index -> new index
            var indexMap = new Dictionary<int, int>();
            var vertexNormals = new List<(Vector3 v, Vector3 n)>(toKeep.Count);
            int newIdx = 0;
            foreach (var idx in toKeep)
            {
                indexMap[idx] = newIdx++;
                vertexNormals.Add((verts[idx], norms[idx]));
            }

            var triangles = new List<int>(trigs.Length);
            for (int i = 0; i < trigs.Length; i += 3)
            {
                var i0 = trigs[i];
                var i1 = trigs[i + 1];
                var i2 = trigs[i + 2];
                if (toKeep.Contains(i0) && toKeep.Contains(i1) && toKeep.Contains(i2))
                {
                    triangles.Add(indexMap[i0]);
                    triangles.Add(indexMap[i1]);
                    triangles.Add(indexMap[i2]);
                }
            }

            return new Mesh
            {
                vertices = vertexNormals.Select(vn => vn.v).ToArray(),
                normals = vertexNormals.Select(vn => vn.n).ToArray(),
                triangles = triangles.ToArray(),
            };
        }

        static public Mesh CleanupMeshByCluster(Mesh toCleanUp, Mesh oldMesh, float tolerance)
        {
            var toCleanupGroup = MeshMod.GroupVertices(toCleanUp);
            var oldMeshGroup = MeshMod.GroupVertices(oldMesh);
            var distances = MeshMod.DistanceOfGroups(toCleanupGroup, oldMeshGroup);

            var verts = toCleanUp.vertices;
            var norms = toCleanUp.normals;
            var trigs = toCleanUp.triangles;

            var toKeepGroups = distances
                .Select((dist, idx) => (dist, idx))
                .Where(pair => pair.dist < tolerance)
                .Select(pair => pair.idx)
                .ToHashSet();

            var toKeepIndices = new HashSet<int>();
            foreach (var groupIdx in toKeepGroups)
                foreach (var idx in toCleanupGroup.groups[groupIdx])
                    toKeepIndices.Add(idx);

            // Fast remapping: old index -> new index
            var indexMap = new Dictionary<int, int>();
            var vertexNormals = new List<(Vector3 v, Vector3 n)>(toKeepIndices.Count);
            int newIdx = 0;
            foreach (var idx in toKeepIndices)
            {
                indexMap[idx] = newIdx++;
                vertexNormals.Add((verts[idx], norms[idx]));
            }

            var triangles = new List<int>(trigs.Length);
            for (int i = 0; i < trigs.Length; i += 3)
            {
                var i0 = trigs[i];
                var i1 = trigs[i + 1];
                var i2 = trigs[i + 2];
                if (toKeepIndices.Contains(i0) && toKeepIndices.Contains(i1) && toKeepIndices.Contains(i2))
                {
                    triangles.Add(indexMap[i0]);
                    triangles.Add(indexMap[i1]);
                    triangles.Add(indexMap[i2]);
                }
            }

            return new Mesh
            {
                vertices = vertexNormals.Select(vn => vn.v).ToArray(),
                normals = vertexNormals.Select(vn => vn.n).ToArray(),
                triangles = triangles.ToArray(),
            };
        }
    }

}
