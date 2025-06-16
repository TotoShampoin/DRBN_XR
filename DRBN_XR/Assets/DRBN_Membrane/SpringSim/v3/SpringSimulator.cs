using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.Profiling;
using UnityEngine;

namespace SpringSim.V3
{
    /// <summary>
    /// Standalone mass-spring physics simulation, with interop with Unity meshes. Also supports the ability for the user to drag the particles around, by means of the Grabber.
    /// </summary>
    public class SpringSimulator : MonoBehaviour
    {
        [Header("Parameters")]
        public float particleMass = 2f;
        public float selectionForce = 1500f;
        public float selectionRadius = 0.15f;
        public float stiffness = 1500f;
        public float viscosity = 2f;
        public float thickness = 0.05f;
        public float comeback = 1500f;
        public float drag = 0f;
        public float rigidityBase = 2f;
        public float maxRigidity = 5f;
        public bool useAchors = false;
        public float fps = 120f;

        [Header("Interaction")]
        public Grabber grabber;
        public Transform vrController;

        [Header("Rendering")]
        public Mesh massMesh;
        public Mesh linkMesh;
        public Material material;
        public Material triangleMaterial;

        [Header("Debug")]
        public bool showMesh;

        readonly List<Mass> masses = new();
        readonly List<SpringLink> links = new();
        readonly List<(int p1, int p2, int p3)> triangles = new();
        readonly List<(int i, Vector3 o)> anchors = new();
        readonly List<(int i, float w, Vector3 o)> selected = new();
        readonly List<(Vector3 f, Vector3 o, float r)> externalForces = new();
        int closestSelectedIdx = -1;
        Mesh cachedMesh;

        public float ParticleMass { get => particleMass; set => particleMass = value; }
        public float SelectionForce { get => selectionForce; set => selectionForce = value; }
        public float SelectionRadius { get => selectionRadius; set => selectionRadius = value; }
        public float Stiffness { get => stiffness; set => stiffness = value; }
        public float Viscosity { get => viscosity; set => viscosity = value; }
        public float Thickness { get => thickness; set => thickness = value; }
        public float Comeback { get => comeback; set => comeback = value; }
        public float Drag { get => drag; set => drag = value; }
        public float RigidityBase { get => rigidityBase; set => rigidityBase = value; }
        public float MaxRigidity { get => maxRigidity; set => maxRigidity = value; }
        public float FPS
        {
            get => fps;
            set
            {
                fps = value;
                // Time.fixedDeltaTime = 1 / value;
            }
        }
        public bool ShowMesh { get => showMesh; set => showMesh = value; }
        public bool UseAchors { get => useAchors; set => useAchors = value; }
        public bool HasMasses => masses.Count > 0;
        public bool NeedsRecalcNormals { get; set; } = true;

        static readonly ProfilerMarker normalMarker = new("Membrane.SpringSimulator.NormalCalculation");
        static readonly ProfilerMarker useMeshMarker = new("Membrane.SpringSimulator.UseMesh");
        static readonly ProfilerMarker linkFillMarker = new("Membrane.SpringSimulator.UseMesh.FillLinks");
        static readonly ProfilerMarker triangleFillMarker = new("Membrane.SpringSimulator.UseMesh.FillTriangles");
        static readonly ProfilerMarker useMeshVeloMarker = new("Membrane.SpringSimulator.UseMeshRetainVelocities");
        static readonly ProfilerMarker meshCreationMarker = new("Membrane.SpringSimulator.ToMesh");
        static readonly ProfilerMarker umvSearchNeighborsMarker = new("Membrane.SpringSimulator.UseMeshRetainVelocities.SearchNeighbors");
        static readonly ProfilerMarker umvCalcVeloMarker = new("Membrane.SpringSimulator.UseMeshRetainVelocities.CalculateVelocities");

        void Start()
        {
            grabber.simulator = this;
        }
        void Update()
        {
            Render();
            Time.fixedDeltaTime = 1 / fps;
        }

        void FixedUpdate()
        {
            float deltaTime = Time.fixedDeltaTime;
            Mass.mass = particleMass;
            Parallel.For(0, links.Count, i =>
            {
                var link = links[i];
                var force = SpringForce(link);
                lock (masses[link.a]) { masses[link.a].AddForce(force); }
                lock (masses[link.b]) { masses[link.b].AddForce(-force); }
            });
            if (selected.Count == 0)
            {
                GrabberOnMesh(new Ray(vrController.position, vrController.forward));
            }
            else
            {
                var closestMass = masses[closestSelectedIdx];
                var grabberPosLocal = transform.InverseTransformPoint(grabber.Position);
                var delta = grabberPosLocal - closestMass.position;
                // var delta = transform.InverseTransformDirection(grabber.Delta);
                Parallel.ForEach(selected, s =>
                {
                    var (idx, weight, origin) = s;
                    var sel = masses[idx];
                    // var delta = grabberPosLocal - sel.position;
                    lock (sel)
                    {
                        sel.AddForce(selectionForce * weight * delta);

                        // hack: selected masses shouldn't have drag force
                        if (sel.velocity == Vector3.zero) return;
                        var dir = Vector3.Normalize(sel.velocity);
                        var mag = Vector3.Magnitude(sel.velocity);
                        sel.AddForce(mag * mag * drag * weight * dir);
                    }
                    // var selDelta = sel.position - origin;
                    // lock (sel) { sel.AddForce(selectionForce * weight * (delta - selDelta)); }
                });
            }
            Parallel.ForEach(anchors, a => masses[a.i].AddForce(comeback * (a.o - masses[a.i].position)));
            Parallel.ForEach(externalForces, ef =>
            {
                var (force, origin, radius) = ef;
                var nearby = NearbyMasses(origin, radius).ToArray();
                Parallel.ForEach(nearby, n =>
                {
                    var (m, d, i) = n;
                    m.AddForce(force * (1 - d / radius));
                });
            });
            externalForces.Clear();
            Parallel.ForEach(masses, m =>
            {
                if (m.velocity == Vector3.zero) return;
                var dir = Vector3.Normalize(m.velocity);
                var mag = Vector3.Magnitude(m.velocity);
                m.AddForce(-mag * mag * drag * dir);
            });
            Parallel.ForEach(masses, m => m.ApplyForce(deltaTime));
            if (NeedsRecalcNormals)
            {
                using (normalMarker.Auto())
                {
                    UpdateCachedMesh();
                    RecalculateNormals();
                }
            }
        }

        public void Render()
        {
            Matrix4x4 localToWorld = transform.localToWorldMatrix;
            if (showMesh)
                Graphics.DrawMesh(cachedMesh, localToWorld, triangleMaterial, 0);
            else
            {
                Graphics.DrawMeshInstanced(
                    massMesh, 0,
                    material,
                    masses
                        .AsParallel()
                        .Select(m =>
                            localToWorld *
                            Matrix4x4.TRS(
                                m.position, m.normal == Vector3.zero ? Quaternion.identity : Quaternion.LookRotation(m.normal), thickness * Vector3.one
                            ))
                        .ToArray()
                );
                Graphics.DrawMeshInstanced(
                    linkMesh, 0,
                    material,
                    links
                        .AsParallel()
                        .Select(link =>
                        {
                            Vector3 start = masses[link.a].position;
                            Vector3 end = masses[link.b].position;
                            var delta = end - start;
                            Vector3 mid = (start + end) / 2;
                            float length = Vector3.Distance(start, end);
                            return localToWorld *
                                Matrix4x4.TRS(
                                    mid,
                                    delta == Vector3.zero ? Quaternion.identity : Quaternion.LookRotation(delta),
                                    new Vector3(thickness / 4f, thickness / 4f, length) / 2f
                                );
                        })
                        .ToArray()
                );
            }
        }

        public Vector3 SpringForce(SpringLink link)
        {
            var p1 = masses[link.a].position;
            var p2 = masses[link.b].position;
            var v1 = masses[link.a].velocity;
            var v2 = masses[link.b].velocity;
            var r1 = masses[link.a].rigidity;
            var r2 = masses[link.b].rigidity;

            var r = (r1 + r2) / 2f;

            var l0 = link.length;
            var k = stiffness * Mathf.Pow(rigidityBase, r);
            var d = Vector3.Distance(p1, p2);

            if (d == 0) return Vector3.zero;
            var dir = (p2 - p1).normalized;
            var springForce = k * (d - l0) * dir;
            var dampingForce = viscosity * (v2 - v1);
            return springForce + dampingForce;
        }

        /// <summary>
        /// Adds a localised force that will be applied to the particles on the next physics update. The coordinate space is the global world space.
        /// </summary>
        /// <param name="force">Direction and magnitude of the force</param>
        /// <param name="origin">Where the force comes from</param>
        /// <param name="radius">The radius of influence from the origin</param>
        public void ApplyForce(Vector3 force, Vector3 origin, float? radius = null)
        {
            var F = transform.InverseTransformDirection(force);
            var O = transform.InverseTransformPoint(origin);
            externalForces.Add((F, O, radius ?? selectionRadius));
        }

        public void Clear()
        {
            masses.Clear();
            links.Clear();
            triangles.Clear();
            anchors.Clear();
        }

        /// <summary>
        /// Uses the converted mesh's normals for the masses' normals
        /// </summary>
        void RecalculateNormals()
        {
            var normals = cachedMesh.normals;
            Parallel.For(0, normals.Length, i =>
            {
                masses[i].normal = normals[i];
            });
        }

        /// <summary>
        /// Converts a Unity3D mesh into a structure of mass-spring particles, with vertex merging.
        /// </summary>
        /// <param name="mesh"></param>
        /// <param name="extractionEpsilon">The minimum distance between two masses, below which they will be merged</param>
        public void UseMesh(Mesh mesh, float extractionEpsilon = 0.005f)
        {
            using (useMeshMarker.Auto())
            {
                Vector3? selectedPosition = selected.Count > 0 ? masses[closestSelectedIdx].position : null;

                Clear();
                Mesh dmesh = MeshMod.DeduplicateVertices(mesh, extractionEpsilon);
                var dvertices = dmesh.vertices.Zip(dmesh.normals, (position, normal) => (position, normal));
                var dtriangles = dmesh.triangles;
                var bounds = dmesh.bounds;
                MeshMod.PreventFlatBounds(ref bounds, extractionEpsilon * 2);

                // Fill meshes
                masses.AddRange(dvertices
                    .AsParallel()
                    .AsOrdered()
                    .Select(v => new Mass() { position = v.position, normal = v.normal })
                    .ToArray());

                // Fill links
                using (linkFillMarker.Auto())
                {
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
                }

                // Fill triangles
                var newTriangles = new (int, int, int)[dtriangles.Length / 3];
                var indexToLink = new Dictionary<(int, int), int>();
                int linkIdx = 0;
                using (triangleFillMarker.Auto())
                {
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
                        Vector3 normal = Vector3.Cross(p1 - p0, p2 - p0);
                        if (Vector3.Dot(normal, Vector3.up) < 0) (v1, v2) = (v2, v1);

                        newTriangles[i] = (v0, v1, v2);
                    });
                    triangles.AddRange(newTriangles.Where(t => t != default));
                }

                if (useAchors)
                {
                    var bmin = bounds.min;
                    var bmax = bounds.max;
                    for (int i = 0; i < masses.Count; i++)
                    {
                        var mass = masses[i];
                        var p = mass.position;
                        int nbCommon = 0;
                        if (Mathf.Abs(p.x - bmin.x) < extractionEpsilon || Mathf.Abs(p.x - bmax.x) < extractionEpsilon) nbCommon++;
                        if (Mathf.Abs(p.y - bmin.y) < extractionEpsilon || Mathf.Abs(p.y - bmax.y) < extractionEpsilon) nbCommon++;
                        if (Mathf.Abs(p.z - bmin.z) < extractionEpsilon || Mathf.Abs(p.z - bmax.z) < extractionEpsilon) nbCommon++;
                        if (nbCommon >= 1)
                            anchors.Add((i, p));
                    }
                }

                if (selectedPosition.HasValue)
                {
                    grabber.ResetOrigin();
                    GrabAt(selectedPosition.Value);
                    masses[closestSelectedIdx].position = selectedPosition.Value;
                }
            }
        }

        private void UpdateCachedMesh()
        {
            cachedMesh = ToMesh();
        }

        public void UseMeshRetainVelocities(Mesh mesh, float searchRadius = 0.5f, float extractionEpsilon = 0.005f)
        {
            using (useMeshVeloMarker.Auto())
            {
                var oldMasses = masses.Select(m => new { m.position, m.velocity }).ToArray();
                UseMesh(mesh, extractionEpsilon);
                for (int i = 0; i < masses.Count; i++)
                {
                    umvSearchNeighborsMarker.Begin();
                    var newPos = masses[i].position;
                    var neighbors = oldMasses
                        .Select(m => new { m.velocity, dist = Vector3.Distance(m.position, newPos) })
                        .Where(x => x.dist < searchRadius)
                        .ToArray();
                    umvSearchNeighborsMarker.End();

                    if (neighbors.Length == 0)
                        return;

                    umvCalcVeloMarker.Begin();
                    Vector3 interpolatedVelocity = Vector3.zero;
                    float totalWeight = 0f;
                    foreach (var n in neighbors)
                    {
                        float w = n.dist / searchRadius;
                        interpolatedVelocity += n.velocity * w;
                        totalWeight += w;
                    }
                    masses[i].velocity = interpolatedVelocity / totalWeight;
                    umvCalcVeloMarker.End();
                }
            }
        }

        /// <summary>
        /// Converts the current state of the simulation into a mesh
        /// </summary>
        /// <returns></returns>
        public Mesh ToMesh()
        {
            using (meshCreationMarker.Auto())
            {
                var mesh = new Mesh() // [MARKER] Bottleneck, not really, mostly the GC; TODO: Get rid of LINQ?
                {
                    vertices = masses.Select(m => m.position).ToArray(),
                    colors = masses
                        .Select(m =>
                        {
                            if (m.rigidity > 0)
                                return Color.Lerp(Color.white, new Color(0f, 0.5f, 1f), 2f * Mathf.Clamp01(m.rigidity / maxRigidity));
                            else if (m.rigidity < 0)
                                return Color.Lerp(Color.white, new Color(1f, 0.5f, 0f), 2f * Mathf.Clamp01(-m.rigidity / maxRigidity));
                            else
                                return Color.white;
                            // var sel = selected.FirstOrDefault(s => s.i == masses.IndexOf(m));
                            // if (sel != default)
                            //     return Color.Lerp(Color.white, new Color(1f, 0f, 1f), Mathf.Clamp01(sel.w));
                            // return Color.white;
                        })
                        .ToArray(),
                    triangles = triangles
                        .Where(tri => tri.p1 != tri.p2 && tri.p2 != tri.p3 && tri.p3 != tri.p1)
                        .SelectMany(tri => new[] { tri.p1, tri.p2, tri.p3 })
                        .ToArray(),
                };
                mesh.RecalculateNormals();
                mesh.RecalculateBounds();
                var normals = mesh.normals;
                Parallel.For(0, normals.Length, i =>
                {
                    normals[i] = normals[i] * Mathf.Sign(Vector3.Dot(masses[i].normal, normals[i]));
                });
                mesh.normals = normals;
                return mesh;
            }
        }

        public IEnumerable<(Mass m, float d, int i)> NearbyMasses(Vector3 position, float distance)
        {
            for (int i = 0; i < masses.Count; i++)
            {
                float d = Vector3.Distance(masses[i].position, position);
                if (d <= distance)
                    yield return (masses[i], d, i);
            }
        }

        public void Grab()
        {
            var grabberPosition = transform.InverseTransformPoint(grabber.Position);
            var closestMass = masses.OrderBy(m => Vector3.Distance(m.position, grabberPosition)).First();
            GrabAt(closestMass.position);
        }
        public void GrabAt(Vector3 localGrabberPos)
        {
            selected.Clear();
            var nearby = NearbyMasses(localGrabberPos, selectionRadius).ToArray();
            selected.AddRange(nearby.Select(m => (m.i, 1f - m.d / selectionRadius, m.m.position)));

            if (selected.Count > 0)
            {
                closestSelectedIdx = selected
                    .OrderBy(s => Vector3.Distance(masses[s.i].position, localGrabberPos))
                    .First().i;
            }
            else
            {
                closestSelectedIdx = -1;
            }

            // if (closestSelectedIdx != -1)
            // {
            //     for (int i = 0; i < selected.Count; i++)
            //     {
            //         if (selected[i].i == closestSelectedIdx)
            //         {
            //             selected[i] = (selected[i].i, 1f, selected[i].o);
            //             break;
            //         }
            //     }
            // }
        }
        public void Ungrab()
        {
            selected.Clear();
        }

        public void Impact()
        {
            var grabberPosition = grabber.Position;
            var closestPoint = ClosestToMesh(grabberPosition);
            if (closestPoint.HasValue)
            {
                Vector3 direction = closestPoint.Value.position - grabberPosition;
                if (direction != Vector3.zero)
                {
                    ApplyForce(selectionForce * -closestPoint.Value.normal, grabberPosition, selectionRadius);
                }
            }
        }

        public void ChangeRigidity(float value)
        {
            Parallel.ForEach(selected, sel => masses[sel.i].rigidity = Mathf.Min(masses[sel.i].rigidity + value * sel.w, maxRigidity));
        }

        public (Vector3 position, Vector3 normal)? ClosestToMesh(Vector3 at)
        {
            try
            {
                return triangles
                    .AsParallel()
                    .AsOrdered()
                    .Select(t =>
                    {
                        var (p1, p2, p3) = t;
                        Vector3 v0 = masses[p1].position;
                        Vector3 v1 = masses[p2].position;
                        Vector3 v2 = masses[p3].position;
                        Vector3 n0 = masses[p1].normal;
                        Vector3 n1 = masses[p2].normal;
                        Vector3 n2 = masses[p3].normal;
                        var vertices = (v0, v1, v2);
                        var normals = (n0, n1, n2);

                        var position = Voxelization.VoxelizerCPU.ClosestOnTriangle(vertices, at);
                        var normal = Voxelization.VoxelizerCPU.TriangleNormal(vertices, normals, position);

                        return (position, normal);
                    })
                    .AsSequential()
                    .OrderBy(v => Vector3.Distance(v.position, at))
                    .First();
            }
            catch
            {
                return null;
            }
        }
        public void GrabberOnMesh(Ray ray)
        {
            var rayOrigin = ray.origin;
            var rayDirection = ray.direction;
            float closestDist = float.MaxValue;
            Vector3 hitPoint = Vector3.zero;
            bool hit = false;

            Matrix4x4 localToWorld = transform.localToWorldMatrix;
            for (int i = 0; i < triangles.Count; i++)
            {
                var (p1, p2, p3) = triangles[i];
                Vector3 v0 = localToWorld.MultiplyPoint(masses[p1].position);
                Vector3 v1 = localToWorld.MultiplyPoint(masses[p2].position);
                Vector3 v2 = localToWorld.MultiplyPoint(masses[p3].position);

                if (MeshMod.RayTriangleIntersection(rayOrigin, rayDirection, (v0, v1, v2), out Vector3 tempHit, out float dist))
                {
                    if (dist < closestDist)
                    {
                        closestDist = dist;
                        hitPoint = tempHit;
                        hit = true;
                    }
                }
            }

            if (hit)
            {
                grabber.Position = hitPoint;
            }
        }

    }

    public class Mass
    {
        public Vector3 position;
        public Vector3 normal;
        public Vector3 velocity = Vector3.zero;
        public Vector3 force = Vector3.zero;
        public float rigidity = 0;

        static public float mass;
        static readonly ProfilerMarker addForceMarker = new("Membrane.SpringSimulator.AddForce");

        public void AddForce(Vector3 force)
        {
            using (addForceMarker.Auto()) { this.force += force; }
        }
        public void ApplyForce(float deltaTime)
        {
            velocity += force / mass * deltaTime;
            if (float.IsNaN(velocity.x) || float.IsNaN(velocity.y) || float.IsNaN(velocity.z))
            {
                velocity = Vector3.zero;
            }
            position += velocity * deltaTime;
            if (float.IsNaN(position.x) || float.IsNaN(position.y) || float.IsNaN(position.z))
            {
                position = Vector3.zero;
            }
            force = Vector3.zero;
        }
    };

    public struct SpringLink
    {
        public int a;
        public int b;
        public float length;
    };

}
