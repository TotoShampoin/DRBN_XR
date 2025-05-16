using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using System.Diagnostics;
using TMPro;
using UnityEngine.XR;

namespace Assets.SpringSim.V2
{
    public struct SpringLink
    {
        public int a;
        public int b;
        public float length;
    };
    public struct Triangle
    {
        public int l1;
        public int l2;
        public int l3;
        public int p1;
        public int p2;
        public int p3;
    };

    class CachedMass
    {
        public Vector3 position;
        public Vector3 velocity;
        public Vector3 force;
        public float rigidity;
    };

    public enum MassReturns
    {
        None,
        Corners,
        Edges,
        All,
    }

    public enum GrabInfluenceFunction
    {
        Linear,
        InoutCubic,
        OutCubic,
    }

    public class SpringSimulator : MonoBehaviour
    {
        [Header("Properties")]
        public float stiffness = 1000f;
        public float viscosity = 2f;
        public float comebackStiffness = 1000f;

        public MassObject massPrefab;
        public LinkObject linkPrefab;
        public bool useBounds = true;
        public Bounds bounds = new(new(0, 0, 0), new(1, 1, 1));
        public float forcedRate = 240f;
        public bool useGravity = false;
        public MassReturns returnType = MassReturns.None;
        public float grabDistance = 0.5f;
        public float dampingForce = 1f;
        public float grabStregth = 3000f;
        public GrabInfluenceFunction influenceFunction = GrabInfluenceFunction.OutCubic;

        MassObject selected = null;
        readonly List<MassObject> surrounding = new();

        public TextMeshProUGUI profiler;

        private readonly List<MassObject> massObjects = new();
        private readonly List<SpringLink> links = new();
        private readonly List<LinkObject> linkObjects = new();
        private readonly List<Triangle> triangles = new();

        private readonly List<CachedMass> cache = new();

        // --- Object Pools ---
        private readonly Queue<MassObject> massPool = new();
        private readonly Queue<LinkObject> linkPool = new();
        // --------------------

        private Bounds usedBounds;

        private Stopwatch stopwatch;

        public Grabber grabber;

        public float Stiffness { get => stiffness; set => stiffness = value; }
        public float Viscosity { get => viscosity; set => viscosity = value; }
        public float Comeback { get => comebackStiffness; set => comebackStiffness = value; }
        public bool UseGravity { get => useGravity; set => useGravity = value; }
        public float GrabRadius { get => grabDistance; set => grabDistance = value; }
        public MassReturns Return { get => returnType; set => returnType = value; }
        public int ReturnAsInt { get => (int)returnType; set => returnType = (MassReturns)value; }
        public bool UseBounds { get => useBounds; set => useBounds = value; }
        public float DampingForce { get => dampingForce; set => dampingForce = value; }
        public float GrabStregth { get => grabStregth; set => grabStregth = value; }
        public GrabInfluenceFunction InfluenceFunction { get => influenceFunction; set => influenceFunction = value; }
        public int InfluenceFunctionAsInt { get => (int)influenceFunction; set => influenceFunction = (GrabInfluenceFunction)value; }

        public bool HasMasses => massObjects.Count > 0;

        void Start()
        {
            Time.fixedDeltaTime = 1f / forcedRate;
            XRSettings.eyeTextureResolutionScale = 1.5f; // to make the vr hd
            grabber.simulator = this;
        }

        void Update()
        {
            profiler.text = $"Spring tickrate: {Mathf.Round(1f / (float)stopwatch.Elapsed.TotalSeconds)} tps\nFramerate: {Mathf.Round(1f / (float)Time.deltaTime)} fps";
        }

        void FixedUpdate()
        {
            stopwatch = Stopwatch.StartNew();
            for (int i = 0; i < massObjects.Count; i++)
            {
                cache[i].position = massObjects[i].Position;
                cache[i].velocity = massObjects[i].Velocity;
                cache[i].rigidity = massObjects[i].Rigidity;
                cache[i].force = Vector3.zero;
            }
            Parallel.For(0, links.Count, i =>
            {
                var link = links[i];
                var force = SpringForce(link);
                // var force = EllasticForce(link);
                lock (cache)
                {
                    cache[link.a].force += force;
                    cache[link.b].force -= force;
                }
            });

            Vector3? partialDelta = null;
            if (selected)
            {
                // partialDelta = selected.Position - selected.GrabOrigin;
                partialDelta = grabber.Position - grabber.origin;
            }
            for (int i = 0; i < massObjects.Count; i++)
            {
                massObjects[i].AddForce(cache[i].force);
                massObjects[i].UseGravity = useGravity;
                massObjects[i].ComebackStiffness = comebackStiffness;
                massObjects[i].Damping = dampingForce;
                massObjects[i].PartialDelta = partialDelta ?? Vector3.zero;
                massObjects[i].PartialStrength = grabStregth;
            }
            stopwatch.Stop();
        }

        void OnDrawGizmos()
        {
            Gizmos.color = Color.white;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(bounds.center, bounds.size);
        }

        public Vector3 SpringForce(SpringLink link)
        {
            var p1 = cache[link.a].position;
            var p2 = cache[link.b].position;
            var v1 = cache[link.a].velocity;
            var v2 = cache[link.b].velocity;
            var r1 = cache[link.a].rigidity;
            var r2 = cache[link.b].rigidity;

            var r = (r1 + r2) / 2f;

            var l0 = link.length;
            var k = stiffness * Mathf.Exp(r);
            var d = Vector3.Distance(p1, p2);

            if (d == 0) return Vector3.zero;
            var dir = (p2 - p1).normalized;
            var springForce = k * (d - l0) * dir;
            var dampingForce = viscosity * (v2 - v1);
            return springForce + dampingForce;
        }

        public Vector3 EllasticForce(SpringLink link)
        {
            var p1 = cache[link.a].position;
            var p2 = cache[link.b].position;
            var v1 = cache[link.a].velocity;
            var v2 = cache[link.b].velocity;
            var r1 = cache[link.a].rigidity;
            var r2 = cache[link.b].rigidity;

            var r = (r1 + r2) / 2f;

            var k = stiffness * Mathf.Exp(r);
            var springForce = k * (p2 - p1);
            var dampingForce = viscosity * (v2 - v1);
            return springForce + dampingForce;
        }

        public IEnumerable<MassObject> GetSurroundingMasses(Vector3 position, float distance)
        {
            return massObjects.Where(o => Vector3.Distance(o.Position, position) <= distance);
        }

        public void Grab()
        {
            MassObject closest = null;
            float distance = 0f;
            massObjects.ForEach(m =>
            {
                var d = Vector3.Distance(m.Position, grabber.Position);
                if (closest == null || d < distance)
                {
                    closest = m;
                    distance = d;
                }
            });
            OnMassGrabbed(closest);
        }
        public void Ungrab()
        {
            OnMassUngrabbed(selected);
        }
        public void ResetGrabbed()
        {
            selected = null;
            surrounding.ForEach(o => o.PartialUngrab());
            surrounding.Clear();
        }
        public void OnMassHovered(MassObject hovered)
        {
            grabber.Position = hovered.Position;
        }
        public void OnMassGrabbed(MassObject grabbed)
        {
            if (selected) return;

            float Influence(MassObject o)
            {
                var d = Vector3.Distance(grabbed.Position, o.Position);
                return influenceFunction switch
                {
                    GrabInfluenceFunction.Linear => d,
                    GrabInfluenceFunction.InoutCubic => Mathf.SmoothStep(0, grabDistance, d),
                    GrabInfluenceFunction.OutCubic => 1 - Mathf.Pow(1 - d, 3),
                    _ => 0,
                };

            }

            selected = grabbed;
            surrounding.AddRange(GetSurroundingMasses(grabbed.Position, grabDistance));
            surrounding.ForEach(o => o.PartialGrab(Influence(o)));
            selected.PartialGrab(1);
        }
        public void OnMassUngrabbed(MassObject ungrabbed)
        {
            if (selected != ungrabbed) return;
            ResetGrabbed();
        }

        public void Clear()
        {
            // Pool and deactivate mass objects
            foreach (var rb in massObjects)
            {
                rb.gameObject.SetActive(false);
                massPool.Enqueue(rb);
            }
            massObjects.Clear();
            // Pool and deactivate link objects
            foreach (var lk in linkObjects)
            {
                lk.gameObject.SetActive(false);
                linkPool.Enqueue(lk);
            }
            linkObjects.Clear();
            triangles.Clear();
            links.Clear();
            cache.Clear();
        }
        public void UseMesh(Mesh mesh, float extractionEpsilon = 0.005f)
        {
            Mesh dmesh = MeshMod.DeduplicateVertices(mesh, extractionEpsilon);
            var positions = dmesh.vertices;
            if (useBounds)
            {
                MeshMod.RescaleToBounds(ref positions, dmesh.bounds, bounds);
                usedBounds = bounds;
            }
            else
            {
                usedBounds = dmesh.bounds;
                // If any axis of the bounds size is 0, extend it to 2 * extractionEpsilon
                Vector3 size = usedBounds.size;
                Vector3 min = usedBounds.min;
                Vector3 max = usedBounds.max;
                if (size.x < extractionEpsilon)
                {
                    min.x -= 0.5f;
                    max.x += 0.5f;
                }
                if (size.y < extractionEpsilon)
                {
                    min.y -= 0.5f;
                    max.y += 0.5f;
                }
                if (size.z < extractionEpsilon)
                {
                    min.z -= 0.5f;
                    max.z += 0.5f;
                }
                usedBounds.SetMinMax(min, max);
            }
            ConcurrentDictionary<uint, SpringLink> links = new();
            static uint HashKey(int a, int b)
            {
                if (a > b) { (a, b) = (b, a); }
                return (uint)((a + b) * (a + b + 1) / 2 + b);
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
            Clear();
            UnityEngine.Debug.Log($"bounds: {usedBounds.min} - {usedBounds.max}");
            massObjects.AddRange(
                positions.Select(p =>
                {
                    MassObject mass;
                    if (massPool.Count > 0)
                    {
                        mass = massPool.Dequeue();
                        // mass.transform.SetParent(transform);
                        // mass.transform.position = transform.TransformPoint(p);
                        // mass.transform.rotation = Quaternion.identity;
                        mass.ResetStates(transform.TransformPoint(p), Quaternion.identity, transform);
                    }
                    else
                    {
                        mass = Instantiate(massPrefab, transform.TransformPoint(p), Quaternion.identity, transform);
                    }
                    mass.gameObject.SetActive(true);
                    switch (returnType)
                    {
                        case MassReturns.None: default: break;
                        case MassReturns.Corners:
                            {
                                int axesOnBounds = 0;
                                Vector3 min = usedBounds.min;
                                Vector3 max = usedBounds.max;
                                if (Mathf.Abs(p.x - min.x) < extractionEpsilon || Mathf.Abs(p.x - max.x) < extractionEpsilon) axesOnBounds++;
                                if (Mathf.Abs(p.y - min.y) < extractionEpsilon || Mathf.Abs(p.y - max.y) < extractionEpsilon) axesOnBounds++;
                                if (Mathf.Abs(p.z - min.z) < extractionEpsilon || Mathf.Abs(p.z - max.z) < extractionEpsilon) axesOnBounds++;
                                if (axesOnBounds >= 2)
                                {
                                    mass.ReturnToOrigin = true;
                                }
                            }
                            break;
                        case MassReturns.Edges:
                            {
                                Vector3 min = usedBounds.min;
                                Vector3 max = usedBounds.max;
                                if (
                                    Mathf.Abs(p.x - min.x) < extractionEpsilon || Mathf.Abs(p.x - max.x) < extractionEpsilon ||
                                    Mathf.Abs(p.y - min.y) < extractionEpsilon || Mathf.Abs(p.y - max.y) < extractionEpsilon ||
                                    Mathf.Abs(p.z - min.z) < extractionEpsilon || Mathf.Abs(p.z - max.z) < extractionEpsilon
                                )
                                {
                                    mass.ReturnToOrigin = true;
                                }
                            }
                            break;
                        case MassReturns.All:
                            {
                                mass.ReturnToOrigin = true;
                            }
                            break;
                    }
                    return mass;
                }));
            this.links.AddRange(links.Values);

            // Build triangles using link indices
            var indexToLink = new Dictionary<(int, int), int>();
            int linkIdx = 0;
            foreach (var link in links.Values)
            {
                int a = link.a;
                int b = link.b;
                if (a > b) (a, b) = (b, a);
                indexToLink[(a, b)] = linkIdx++;
            }

            for (int i = 0; i < dmesh.triangles.Length; i += 3)
            {
                int i0 = dmesh.triangles[i + 0];
                int i1 = dmesh.triangles[i + 1];
                int i2 = dmesh.triangles[i + 2];

                int l1 = indexToLink[(Mathf.Min(i0, i1), Mathf.Max(i0, i1))];
                int l2 = indexToLink[(Mathf.Min(i1, i2), Mathf.Max(i1, i2))];
                int l3 = indexToLink[(Mathf.Min(i2, i0), Mathf.Max(i2, i0))];

                triangles.Add(new Triangle
                {
                    l1 = l1,
                    l2 = l2,
                    l3 = l3,
                });
            }

            // Update triangles to store p1, p2, p3 as the three unique vertex indices for each triangle
            for (int i = 0; i < triangles.Count; i++)
            {
                var tri = triangles[i];

                var linkA = this.links[tri.l1];
                var linkB = this.links[tri.l2];
                var linkC = this.links[tri.l3];

                int[] endpoints = { linkA.a, linkA.b, linkB.a, linkB.b, linkC.a, linkC.b };
                var counts = endpoints.GroupBy(x => x).ToDictionary(g => g.Key, g => g.Count());
                var verts = counts.Keys.ToArray();

                if (verts.Length != 3)
                    continue; // skip degenerate triangles

                int v0 = linkA.a;
                int v1 = linkA.b;
                int v2 = verts.First(x => x != v0 && x != v1);

                // Check winding order
                Vector3 p0 = positions[v0];
                Vector3 p1 = positions[v1];
                Vector3 p2 = positions[v2];
                Vector3 normal = Vector3.Cross(p1 - p0, p2 - p0);
                if (Vector3.Dot(normal, Vector3.up) < 0)
                {
                    // Flip winding
                    (v1, v2) = (v2, v1);
                }

                triangles[i] = new Triangle
                {
                    l1 = tri.l1,
                    l2 = tri.l2,
                    l3 = tri.l3,
                    p1 = v0,
                    p2 = v1,
                    p3 = v2
                };
            }

            linkObjects.AddRange(
                links.Values.Select(lk =>
                {
                    LinkObject link;
                    if (linkPool.Count > 0)
                    {
                        link = linkPool.Dequeue();
                        link.transform.SetParent(transform);
                    }
                    else
                    {
                        link = Instantiate(linkPrefab, transform);
                    }
                    link.a = massObjects[lk.a].gameObject;
                    link.b = massObjects[lk.b].gameObject;
                    link.length = lk.length;
                    link.gameObject.SetActive(true);
                    return link;
                })
            );
            cache.AddRange(positions.Select(p => new CachedMass { position = p }));
        }

        public Mesh ToMesh()
        {
            Mesh mesh = new()
            {
                vertices = massObjects.Select(m => transform.InverseTransformPoint(m.Position)).ToArray(),
                triangles = this.triangles
                    .Where(tri => tri.p1 != tri.p2 && tri.p2 != tri.p3 && tri.p3 != tri.p1)
                    .SelectMany(tri => new[] { tri.p1, tri.p2, tri.p3 })
                    .ToArray(),
            };

            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
