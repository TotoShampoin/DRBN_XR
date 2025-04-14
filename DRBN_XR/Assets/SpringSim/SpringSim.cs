using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Assets.SpringSim
{

    public struct SpringLink
    {
        public int a;
        public int b;
        public float length;
    };

    public class SpringSim : MonoBehaviour
    {
        [Header("Properties")]
        [SerializeField] float linkStiffness = 1000f;
        [SerializeField] float particleMass = 0.5f;
        [SerializeField] float viscosity = 2f;
        [SerializeField] float avoidRadius = 0.5f;
        [SerializeField] float avoidForce = 1.0f;

        [Header("Simulation")]
        [SerializeField] float rate = 50f;
        [SerializeField] float hashCellSize = 0.1f;

        [Header("Rendering")]
        [SerializeField] Mesh displayMesh;
        [SerializeField] Material displayMaterial;
        [SerializeField] float displaySize = 0.1f;

        [Header("Init")]
        [SerializeField] Mesh entryPoint;
        [SerializeField] bool rescaleToBounds = true;
        [SerializeField] Vector3 boundSize = new(1, 1, 1);

        [Header("Debug")]
        [SerializeField] Mass massPrefab;
        [SerializeField] Link linkPrefab;
        [SerializeField] bool useDebugBodies = false;
        [SerializeField] bool reset = false;

        private List<Vector3> positions;
        private List<Vector3> velocities;
        private List<Vector3> tmpVelocities;
        private List<SpringLink> links;

        private readonly List<Mass> massBodies = new();
        private readonly List<Link> linkObjects = new();
        bool isFirstFrame = true;

        public Mesh EntryPoint
        {
            get => entryPoint;
            set => entryPoint = value;
        }

        void Start()
        {
            Time.fixedDeltaTime = 1.0f / rate;
            if (entryPoint) ExtractMesh(entryPoint);
        }

        void Update()
        {
            Graphics.DrawMeshInstanced(
                displayMesh, 0,
                displayMaterial,
                positions
                    .Select(pos => transform.localToWorldMatrix
                        * Matrix4x4.TRS(pos, Quaternion.identity, displaySize * Vector3.one))
                    .ToArray()
            );
        }

        void FixedUpdate()
        {
            if (reset)
            {
                ExtractMesh(entryPoint);
                isFirstFrame = true;
                reset = false;
            }
            if (!isFirstFrame)
                Parallel.For(0, positions.Count, (i) =>
                {
                    positions[i] = massBodies[i].position;
                });

            var delta = Time.deltaTime;
            Parallel.For(0, Mathf.Max(links.Count, positions.Count), (i) =>
            {
                if (i < links.Count)
                {
                    var link = links[i];
                    var p1 = positions[link.a];
                    var p2 = positions[link.b];
                    var v1 = velocities[link.a];
                    var v2 = velocities[link.b];

                    var l0 = link.length;
                    var k = linkStiffness;
                    var d = Vector3.Distance(p1, p2);

                    if (d == 0) return;

                    var F = Vector3.zero;
                    F += k * (1 - l0 / d) * (p2 - p1);
                    F += viscosity * (v2 - v1);

                    tmpVelocities[link.a] += F / particleMass * delta;
                    tmpVelocities[link.b] -= F / particleMass * delta;
                }
                // if (i < positions.Count)
                // {
                //     var p = positions[i];
                //     Vector3 influence = Vector3.zero;
                //     foreach (var _p in positions)
                //     {
                //         float factor = Mathf.SmoothStep(1, 0, Mathf.InverseLerp(0f, avoidRadius, Vector3.Distance(p, _p)));
                //         var direction = _p == p ? Vector3.zero : Vector3.Normalize(_p - p);
                //         influence -= avoidForce * factor * direction;
                //     }
                //     tmpVelocities[i] += influence / particleMass * delta;
                // }
            });
            Parallel.For(0, Mathf.Max(positions.Count, links.Count), (i) =>
            {
                if (i < positions.Count)
                {
                    if (massBodies[i].isSelected)
                        tmpVelocities[i] = new(0, 0, 0);
                    velocities[i] = tmpVelocities[i];
                    positions[i] += velocities[i] * delta;

                    massBodies[i].position = massBodies[i].position;
                    massBodies[i].size = displaySize;
                    massBodies[i].mass = particleMass;
                }
                if (i < links.Count)
                {
                    linkObjects[i].a = massBodies[links[i].a];
                    linkObjects[i].b = massBodies[links[i].b];
                }
            });
            isFirstFrame = false;
        }

        void FillRigidBodies()
        {
            for (int i = massBodies.Count; i < positions.Count; i++)
            {
                massBodies.Add(Instantiate(massPrefab, transform));
                massBodies[i].gameObject.SetActive(true);
            }
            for (int i = linkObjects.Count; i < links.Count; i++)
            {
                linkObjects.Add(Instantiate(linkPrefab, transform));
                linkObjects[i].gameObject.SetActive(true);
            }
        }

        public void ExtractMesh(Mesh mesh)
        {
            Mesh dmesh = DeduplicateVertices(mesh);

            positions = dmesh.vertices.ToList();
            velocities = dmesh.vertices.Select(_ => Vector3.zero).ToList();
            tmpVelocities = dmesh.vertices.Select(_ => Vector3.zero).ToList();
            ConcurrentDictionary<uint, SpringLink> links = new();

            // Unordered cantor pairing function
            static uint HashKey(int a, int b)
            {
                if (a > b) { (a, b) = (b, a); }
                return (uint)((a + b) * (a + b + 1) / 2 + b);
            }

            Vector3 minBoundInput = dmesh.bounds.center - dmesh.bounds.size * 0.5f;
            Vector3 maxBoundInput = dmesh.bounds.center + dmesh.bounds.size * 0.5f;
            Vector3 minBoundOutput = -boundSize * 0.5f;
            Vector3 maxBoundOutput = boundSize * 0.5f;
            // Calculate the scale factor to maintain aspect ratio
            Vector3 inputSize = maxBoundInput - minBoundInput;
            Vector3 outputSize = maxBoundOutput - minBoundOutput;
            // Find the dimension with the largest ratio (most constrained)
            float scaleX = outputSize.x / inputSize.x;
            float scaleY = outputSize.y / inputSize.y;
            float scaleZ = outputSize.z / inputSize.z;
            float uniformScale = Mathf.Min(scaleX, scaleY, scaleZ);
            // Calculate centered scaling with preserved aspect ratio
            Vector3 scaledSize = inputSize * uniformScale;
            Vector3 outputCenter = (minBoundOutput + maxBoundOutput) * 0.5f;
            Vector3 scaledMinBound = outputCenter - scaledSize * 0.5f;
            Vector3 scaledMaxBound = outputCenter + scaledSize * 0.5f;
            for (int i = 0; i < positions.Count; i++)
            {
                if (!rescaleToBounds) return;
                // Remap each position from input bounds to output bounds
                Vector3 pos = positions[i];
                Vector3 normalizedPos = new(
                    Mathf.InverseLerp(minBoundInput.x, maxBoundInput.x, pos.x),
                    Mathf.InverseLerp(minBoundInput.y, maxBoundInput.y, pos.y),
                    Mathf.InverseLerp(minBoundInput.z, maxBoundInput.z, pos.z)
                );
                positions[i] = new(
                    Mathf.Lerp(scaledMinBound.x, scaledMaxBound.x, normalizedPos.x),
                    Mathf.Lerp(scaledMinBound.y, scaledMaxBound.y, normalizedPos.y),
                    Mathf.Lerp(scaledMinBound.z, scaledMaxBound.z, normalizedPos.z)
                );
                // MoveMassInMap(i, positions[i]);
            }
            for (int i = 0; i < dmesh.triangles.Length; i += 3)
            {
                var i0 = dmesh.triangles[i + 0];
                var i1 = dmesh.triangles[i + 1];
                var i2 = dmesh.triangles[i + 2];
                links.TryAdd(HashKey(i0, i1), new()
                {
                    a = i0,
                    b = i1,
                    length = Vector3.Distance(positions[i0], positions[i1])
                });
                links.TryAdd(HashKey(i1, i2), new()
                {
                    a = i1,
                    b = i2,
                    length = Vector3.Distance(positions[i1], positions[i2])
                });
                links.TryAdd(HashKey(i2, i0), new()
                {
                    a = i2,
                    b = i0,
                    length = Vector3.Distance(positions[i2], positions[i0])
                });
            }
            this.links = links.Select(kvp => kvp.Value).ToList();
            FillRigidBodies();
        }

        public Mesh DeduplicateVertices(Mesh mesh)
        {
            Vector3[] vertices = mesh.vertices;
            int[] triangles = mesh.triangles;

            List<Vector3> newVertices = new();
            List<int> newTriangles = new();

            for (int i = 0; i < triangles.Length; i += 3)
            {
                // Get vertices for this triangle
                Vector3 v1 = vertices[triangles[i]];
                Vector3 v2 = vertices[triangles[i + 1]];
                Vector3 v3 = vertices[triangles[i + 2]];

                // Define an epsilon constant for vertex comparison
                const float epsilon = 0.005f;

                // Helper function to find or add a vertex
                static int FindVertexIndex(List<Vector3> verts, Vector3 v)
                {
                    for (int i = 0; i < verts.Count; i++)
                    {
                        if (Vector3.SqrMagnitude(verts[i] - v) < epsilon)
                            return i;
                    }
                    verts.Add(v);
                    return verts.Count - 1;
                }

                // Find or add vertices to our deduplicated list
                int index1 = FindVertexIndex(newVertices, v1);
                int index2 = FindVertexIndex(newVertices, v2);
                int index3 = FindVertexIndex(newVertices, v3);

                // Add triangle indices
                newTriangles.Add(index1);
                newTriangles.Add(index2);
                newTriangles.Add(index3);
            }

            // Create new mesh with deduplicated vertices
            Mesh result = new()
            {
                vertices = newVertices.ToArray(),
                triangles = newTriangles.ToArray()
            };
            result.RecalculateNormals();
            result.RecalculateBounds();

            return result;
        }

        private int FindOrAddVertex(List<Vector3> vertices, Vector3 vertex)
        {
            // Use a small epsilon for floating-point comparison
            const float epsilon = 0.0001f;

            for (int i = 0; i < vertices.Count; i++)
            {
                if (Vector3.SqrMagnitude(vertices[i] - vertex) < epsilon)
                    return i;
            }

            // If vertex not found, add it
            vertices.Add(vertex);
            return vertices.Count - 1;
        }
    }

}
