using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.Profiling;
using UnityEngine;

namespace SpringSim.V3
{
    class SpringMeshConversion
    {
        readonly static ProfilerMarker meshToSpring = new("Membrane.SpringMeshConversion.MeshToSprings");
        readonly static ProfilerMarker springToMesh = new("Membrane.SpringMeshConversion.SpringsToMesh");

        public static SpringSimulatorState MeshToSprings(
            Mesh mesh, float extractionEpsilon = 0.005f,
            SpringSimulatorState simulator = null
        )
        {
            simulator ??= new();
            var masses = simulator.masses;
            var links = simulator.links;
            var triangles = simulator.triangles;

            meshToSpring.Begin();
            masses.Clear();
            links.Clear();
            triangles.Clear();
            Mesh dmesh = MeshMod.DeduplicateVertices(mesh, extractionEpsilon);
            var dvertices = dmesh.vertices.Zip(dmesh.normals, (position, normal) => (position, normal));
            var dtriangles = dmesh.triangles;
            var bounds = dmesh.bounds;
            MeshMod.PreventFlatBounds(ref bounds, extractionEpsilon * 2);

            // Fill meshes
            masses.AddRange(dvertices
                .AsParallel()
                .AsOrdered()
                .Select(v => new Mass() { position = v.position, normal = v.normal }));

            // Fill links
            var edgeSet = new HashSet<(int, int)>();
            for (int i = 0; i < dtriangles.Length; i += 3)
            {
                int i0 = dtriangles[i + 0];
                int i1 = dtriangles[i + 1];
                int i2 = dtriangles[i + 2];

                // Always store edges as (min, max) to avoid duplicates
                edgeSet.Add((Mathf.Min(i0, i1), Mathf.Max(i0, i1)));
                edgeSet.Add((Mathf.Min(i1, i2), Mathf.Max(i1, i2)));
                edgeSet.Add((Mathf.Min(i2, i0), Mathf.Max(i2, i0)));
            }

            links.Capacity = edgeSet.Count;
            foreach (var (a, b) in edgeSet)
            {
                links.Add(new SpringLink
                {
                    a = a,
                    b = b,
                    length = Vector3.Distance(masses[a].position, masses[b].position)
                });
            }

            // Fill triangles
            var newTriangles = new (int, int, int)[dtriangles.Length / 3];
            var indexToLink = new Dictionary<(int, int), int>();
            int linkIdx = 0;

            foreach (var link in links)
            {
                int a = link.a;
                int b = link.b;
                if (a > b) (a, b) = (b, a);
                indexToLink[(a, b)] = linkIdx++;
            }

            // Precompute link indices for all triangle edges
            int triCount = dtriangles.Length / 3;
            var edgeLinkIndices = new int[triCount, 3];
            for (int i = 0; i < triCount; i++)
            {
                int i0 = dtriangles[i * 3 + 0];
                int i1 = dtriangles[i * 3 + 1];
                int i2 = dtriangles[i * 3 + 2];
                edgeLinkIndices[i, 0] = indexToLink[(Mathf.Min(i0, i1), Mathf.Max(i0, i1))];
                edgeLinkIndices[i, 1] = indexToLink[(Mathf.Min(i1, i2), Mathf.Max(i1, i2))];
                edgeLinkIndices[i, 2] = indexToLink[(Mathf.Min(i2, i0), Mathf.Max(i2, i0))];
            }

            Parallel.For(0, dtriangles.Length / 3, i =>
            {
                var linkA = links[edgeLinkIndices[i, 0]];
                var linkB = links[edgeLinkIndices[i, 1]];
                var linkC = links[edgeLinkIndices[i, 2]];

                int[] endpoints = { linkA.a, linkA.b, linkB.a, linkB.b, linkC.a, linkC.b };
                int v0 = linkA.a, v1 = linkA.b, v2 = -1;

                for (int j = 0; j < 6; j++)
                {
                    int candidate = endpoints[j];
                    if (candidate != v0 && candidate != v1)
                    {
                        v2 = candidate;
                        break;
                    }
                }
                if (v2 == -1) return;

                Vector3 p0 = masses[v0].position;
                Vector3 p1 = masses[v1].position;
                Vector3 p2 = masses[v2].position;
                Vector3 n0 = masses[v0].normal;
                Vector3 n1 = masses[v1].normal;
                Vector3 n2 = masses[v2].normal;
                Vector3 massNormal = (n0 + n1 + n2).normalized;
                Vector3 vertNormal = Vector3.Cross(p1 - p0, p2 - p0);
                // if (Vector3.Dot(normal, Vector3.up) < 0) (v1, v2) = (v2, v1);
                if (Vector3.Dot(vertNormal, massNormal) < 0) (v1, v2) = (v2, v1);

                newTriangles[i] = (v0, v1, v2);
            });
            triangles.AddRange(newTriangles.Where(t => t != default));
            // simulator.GenerateLUT();
            meshToSpring.End();
            return simulator;
        }

        public static Mesh SpringsToMesh(
            SpringSimulatorState springSimulator,
            Mesh mesh = null
        )
        {
            mesh = mesh != null ? mesh : new Mesh();
            springToMesh.Begin();
            mesh.Clear();
            mesh.SetVertices(springSimulator.masses.Select(m => m.position).ToArray());
            mesh.SetTriangles(springSimulator.triangles
                // .Where(tri => tri.p1 != tri.p2 && tri.p2 != tri.p3 && tri.p3 != tri.p1)
                .SelectMany(tri => new[] { tri.p1, tri.p2, tri.p3 })
                .ToArray(), 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            var normals = mesh.normals;
            Parallel.For(0, normals.Length, i =>
            {
                normals[i] = normals[i] * Mathf.Sign(Vector3.Dot(springSimulator.masses[i].normal, normals[i]));
            });
            mesh.SetNormals(normals);
            springToMesh.End();
            return mesh;
        }
    }
}
