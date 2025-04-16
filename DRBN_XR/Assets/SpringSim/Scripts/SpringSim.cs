using System;
using System.Collections;
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
    public class SpringMass
    {
        public Vector3 position;
        public Vector3 initial;
        public Vector3 velocity = Vector3.zero;
        public Vector3 tmpVelocity = Vector3.zero;
    }

    public class SpringSim : MonoBehaviour
    {
        [Header("Properties")]
        [SerializeField] float linkStiffness = 1000f;
        [SerializeField] float particleMass = 0.5f;
        [SerializeField] float viscosity = 2f;
        [SerializeField] float avoidRadius = 0.5f;
        [SerializeField] float avoidForce = 1.0f;
        [SerializeField] float comebackForce = 100f;
        [SerializeField, Range(0, 1)] float damping = 0.01f;

        [Header("Simulation")]
        [SerializeField] float rate = 50f;

        [Header("Rendering")]
        [SerializeField] Mesh displayMesh;
        [SerializeField] Material displayMaterial;
        [SerializeField] float displaySize = 0.1f;

        [Header("Interaction")]
        [SerializeField] Mass massPrefab;
        [SerializeField] Link linkPrefab;
        [SerializeField] bool reset = false;

        [Header("Init")]
        [SerializeField] Mesh entryPoint;
        [SerializeField] bool rescaleToBounds = true;
        [SerializeField] Vector3 boundSize = new(1, 1, 1);

        private readonly List<Mass> massBodies = new();
        private readonly List<Link> linkObjects = new();

        public Mesh EntryPoint
        {
            get => entryPoint;
            set => entryPoint = value;
        }

        public void Reset() => reset = true;
        public void SetStiffness(float stiffness) => linkStiffness = stiffness;
        public void SetComeback(float comeback) => comebackForce = comeback;
        public void SetDamping(float damping) => this.damping = damping;
        public void Clear()
        {
            massBodies.ForEach(mb => Destroy(mb.gameObject));
            massBodies.Clear();
            linkObjects.ForEach(lb => Destroy(lb.gameObject));
            linkObjects.Clear();
        }

        void Start()
        {
            Time.fixedDeltaTime = 1.0f / rate;
            if (entryPoint) ExtractMesh(entryPoint);
        }

        void FixedUpdate()
        {
            if (reset)
            {
                ExtractMesh(entryPoint);
                reset = false;
            }

            var delta = Time.deltaTime;
            Parallel.For(0, Mathf.Max(linkObjects.Count, massBodies.Count), (i) =>
            {
                if (i < linkObjects.Count)
                {
                    var link = linkObjects[i];
                    var p1 = link.a.position;
                    var p2 = link.b.position;
                    var v1 = link.a.velocity;
                    var v2 = link.b.velocity;

                    var l0 = link.length;
                    var k = linkStiffness;
                    var d = Vector3.Distance(p1, p2);

                    if (d == 0) return;

                    var F = Vector3.zero;
                    F += k * (1 - l0 / d) * (p2 - p1);
                    F += viscosity * (v2 - v1);

                    link.a.tmpVelocity += F / particleMass * delta;
                    link.b.tmpVelocity -= F / particleMass * delta;
                }
                // if (i < massBodies.Count)
                // {
                //     var p = massBodies[i].position;
                //     Vector3 influence = Vector3.zero;
                //     foreach (var _m in massBodies)
                //     {
                //         var _p = _m.position;
                //         float factor = Mathf.SmoothStep(1, 0, Mathf.InverseLerp(0f, avoidRadius, Vector3.Distance(p, _p)));
                //         var direction = _p == p ? Vector3.zero : Vector3.Normalize(_p - p);
                //         influence -= avoidForce * factor * direction;
                //     }
                //     massBodies[i].tmpVelocity += influence / particleMass * delta;
                // }

                if (i < massBodies.Count)
                {
                    var diff = massBodies[i].position - massBodies[i].initial;
                    massBodies[i].tmpVelocity -= diff * comebackForce / particleMass * delta;

                    massBodies[i].tmpVelocity *= 1f - damping;
                }
            });
            Parallel.For(0, massBodies.Count, (i) =>
            {
                if (massBodies[i].isSelected)
                    massBodies[i].tmpVelocity = new(0, 0, 0);
                massBodies[i].velocity = massBodies[i].tmpVelocity;

                massBodies[i].position += massBodies[i].velocity * delta;

                massBodies[i].size = displaySize;
                massBodies[i].mass = particleMass;
            });
        }

        void PreparePool(int massCount, int linkCount)
        {
            for (int i = massBodies.Count; i < massCount; i++)
            {
                massBodies.Add(Instantiate(massPrefab, transform));
                massBodies[i].gameObject.SetActive(true);
            }
            for (int i = linkObjects.Count; i < linkCount; i++)
            {
                linkObjects.Add(Instantiate(linkPrefab, transform));
                linkObjects[i].gameObject.SetActive(true);
            }
        }

        void ToMass(Vector3 p, Mass mass)
        {
            mass.gameObject.SetActive(true);
            mass.position = p;
            mass.initial = p;
            mass.mass = particleMass;
            mass.size = displaySize;
        }
        void ToLink(SpringLink springLink, Link link)
        {
            link = link != null ? link : Instantiate(linkPrefab, transform);
            link.gameObject.SetActive(true);
            link.a = massBodies[springLink.a];
            link.b = massBodies[springLink.b];
            link.length = springLink.length;
        }

        void Fill(Vector3[] positions, SpringLink[] links)
        {
            PreparePool(positions.Length, links.Length);
            for (int i = 0; i < positions.Length; i++)
            {
                ToMass(positions[i], massBodies[i]);
            }
            for (int i = positions.Length; i < massBodies.Count; i++)
            {
                massBodies[i].gameObject.SetActive(false);
            }
            for (int i = 0; i < links.Length; i++)
            {
                ToLink(links[i], linkObjects[i]);
            }
            for (int i = links.Length; i < linkObjects.Count; i++)
            {
                linkObjects[i].gameObject.SetActive(false);
            }
        }

        public void ExtractMesh(Mesh mesh)
        {
            Mesh dmesh = DeduplicateVertices(mesh);

            var positions = dmesh.vertices.Clone() as Vector3[];
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
            for (int i = 0; i < positions.Length; i++)
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
            Fill(positions, links.Select(kvp => kvp.Value).ToArray());
        }

        public Mesh DeduplicateVertices(Mesh mesh, float epsilon = 0.005f)
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

                // Find or add vertices to our deduplicated list
                int index1 = FindOrAddVertex(newVertices, v1, epsilon);
                int index2 = FindOrAddVertex(newVertices, v2, epsilon);
                int index3 = FindOrAddVertex(newVertices, v3, epsilon);

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

        private int FindOrAddVertex(List<Vector3> vertices, Vector3 vertex, float epsilon = 0.0001f)
        {
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
