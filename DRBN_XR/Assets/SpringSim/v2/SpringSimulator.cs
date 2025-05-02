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
        Cubic,
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
        public GrabInfluenceFunction influenceFunction = GrabInfluenceFunction.Cubic;

        MassObject selected = null;
        readonly List<MassObject> surrounding = new();

        public TextMeshProUGUI profiler;

        private readonly List<MassObject> massObjects = new();
        private readonly List<SpringLink> links = new();
        private readonly List<LinkObject> linkObjects = new();

        private readonly List<CachedMass> cache = new();

        private Bounds usedBounds;

        private Stopwatch stopwatch;

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

        void Start()
        {
            Time.fixedDeltaTime = 1f / forcedRate;
            XRSettings.eyeTextureResolutionScale = 1.5f; // to make the vr hd
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
                var p1 = cache[link.a].position;
                var p2 = cache[link.b].position;
                var v1 = cache[link.a].velocity;
                var v2 = cache[link.b].velocity;
                var r1 = cache[link.a].rigidity;
                var r2 = cache[link.b].rigidity;

                var r = (r1 + r2) / 2f;

                var l0 = link.length;
                var k = stiffness * r;
                var d = Vector3.Distance(p1, p2);

                if (d == 0) return;
                var dir = (p2 - p1).normalized;
                var springForce = k * (d - l0) * dir;
                var dampingForce = viscosity * (v2 - v1);
                lock (cache)
                {
                    cache[link.a].force += springForce + dampingForce;
                    cache[link.b].force -= springForce + dampingForce;
                }
            });

            Vector3? partialDelta = null;
            if (selected)
                partialDelta = selected.Position - selected.GrabOrigin;
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

        public IEnumerable<MassObject> GetSurroundingMasses(Vector3 position, float distance)
        {
            return massObjects.Where(o => Vector3.Distance(o.Position, position) <= distance);
        }

        public void ResetGrabbed()
        {
            selected = null;
            surrounding.ForEach(o => o.PartialUngrab());
            surrounding.Clear();
        }
        public void OnMassGrabbed(MassObject grabbed)
        {
            if (selected) return;

            float Influence(MassObject o)
            {
                return influenceFunction switch
                {
                    GrabInfluenceFunction.Cubic => Mathf.SmoothStep(
                        0, grabDistance, Vector3.Distance(grabbed.Position, o.Position)),
                    GrabInfluenceFunction.Linear => Vector3.Distance(grabbed.Position, o.Position),
                    _ => 0,
                };

            }

            selected = grabbed;
            surrounding.AddRange(GetSurroundingMasses(grabbed.Position, grabDistance));
            surrounding.ForEach(o => o.PartialGrab(Influence(o)));
        }
        public void OnMassUngrabbed(MassObject ungrabbed)
        {
            if (selected != ungrabbed) return;
            ResetGrabbed();
        }

        public void Clear()
        {
            massObjects.ForEach(rb => Destroy(rb.gameObject));
            massObjects.Clear();
            linkObjects.ForEach(lk => Destroy(lk.gameObject));
            linkObjects.Clear();
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
            massObjects.AddRange(
                positions.Select(p =>
                {
                    var mass = Instantiate(massPrefab, transform.TransformPoint(p), Quaternion.identity, transform);
                    mass.gameObject.SetActive(true);
                    switch (returnType)
                    {
                        case MassReturns.None: default: break;
                        case MassReturns.Corners:
                            {
                                // Check if p is at least at 2 of the 3 axes of the bounds (i.e., on a corner)
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
                                // Check if p is at least on one of the axes of the bounds (i.e., on an edge)
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
            linkObjects.AddRange(
                links.Values.Select(lk =>
                {
                    var link = Instantiate(linkPrefab, transform);
                    link.a = massObjects[lk.a].gameObject;
                    link.b = massObjects[lk.b].gameObject;
                    link.length = lk.length;
                    link.gameObject.SetActive(true);
                    return link;
                })
            );
            cache.AddRange(positions.Select(p => new CachedMass { position = p }));
        }
    }
}
