using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace SpringSim.V3
{
    /// <summary>
    /// Mass-spring physics simulation, with interop with Unity meshes. Also supports the ability for the user to drag the particles around, by means of the Grabber.
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
        public bool useAchors = false;

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
        int closestSelectedIdx = -1;

        public float ParticleMass { get => particleMass; set => particleMass = value; }
        public float SelectionForce { get => selectionForce; set => selectionForce = value; }
        public float SelectionRadius { get => selectionRadius; set => selectionRadius = value; }
        public float Stiffness { get => stiffness; set => stiffness = value; }
        public float Viscosity { get => viscosity; set => viscosity = value; }
        public float Thickness { get => thickness; set => thickness = value; }
        public float Comeback { get => comeback; set => comeback = value; }
        public bool ShowMesh { get => showMesh; set => showMesh = value; }
        public bool UseAchors { get => useAchors; set => useAchors = value; }
        public bool HasMasses => masses.Count > 0;

        void Start()
        {
            grabber.simulator = this;
            Time.fixedDeltaTime = 0.01f;
        }
        void Update()
        {
            Render();
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
                    lock (sel) { sel.AddForce(selectionForce * weight * delta); }
                    // var selDelta = sel.position - origin;
                    // lock (sel) { sel.AddForce(selectionForce * weight * (delta - selDelta)); }
                });
            }
            Parallel.ForEach(anchors, a => masses[a.i].AddForce(comeback * (a.o - masses[a.i].position)));
            Parallel.ForEach(masses, m => m.ApplyForce(deltaTime));
        }

        public void Render()
        {
            Matrix4x4 localToWorld = transform.localToWorldMatrix;
            if (showMesh)
                Graphics.DrawMesh(ToMesh(), localToWorld, triangleMaterial, 0);
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
                                m.position, Quaternion.LookRotation(m.normal), thickness * Vector3.one
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
            var k = stiffness * Mathf.Exp(r);
            var d = Vector3.Distance(p1, p2);

            if (d == 0) return Vector3.zero;
            var dir = (p2 - p1).normalized;
            var springForce = k * (d - l0) * dir;
            var dampingForce = viscosity * (v2 - v1);
            return springForce + dampingForce;
        }

        public void Clear()
        {
            masses.Clear();
            links.Clear();
            triangles.Clear();
            anchors.Clear();
        }

        /// <summary>
        /// Converts a Unity3D mesh into a structure of mass-spring particles, with vertex merging.
        /// </summary>
        /// <param name="mesh"></param>
        /// <param name="extractionEpsilon">The minimum distance between two masses, below which they will be merged</param>
        public void UseMesh(Mesh mesh, float extractionEpsilon = 0.005f)
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
            var newLinks = new ConcurrentDictionary<uint, SpringLink>();
            static uint HashKey(int a, int b)
            {
                if (a > b) { (a, b) = (b, a); }
                return (uint)((a + b) * (a + b + 1) / 2 + b);
            }
            void TryAddToLinks(int i0, int i1) => newLinks.TryAdd(HashKey(i0, i1), new() { a = i0, b = i1, length = Vector3.Distance(masses[i0].position, masses[i1].position) });
            Parallel.For(0, dtriangles.Length / 3, i =>
            {
                var i0 = dtriangles[i * 3 + 0];
                var i1 = dtriangles[i * 3 + 1];
                var i2 = dtriangles[i * 3 + 2];
                TryAddToLinks(i0, i1);
                TryAddToLinks(i1, i2);
                TryAddToLinks(i2, i0);
            });
            links.AddRange(newLinks.Values);

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
            Parallel.For(0, dtriangles.Length / 3, i =>
            {
                int i0 = dtriangles[i * 3 + 0];
                int i1 = dtriangles[i * 3 + 1];
                int i2 = dtriangles[i * 3 + 2];
                var linkA = links[indexToLink[(Mathf.Min(i0, i1), Mathf.Max(i0, i1))]];
                var linkB = links[indexToLink[(Mathf.Min(i1, i2), Mathf.Max(i1, i2))]];
                var linkC = links[indexToLink[(Mathf.Min(i2, i0), Mathf.Max(i2, i0))]];

                int[] endpoints = { linkA.a, linkA.b, linkB.a, linkB.b, linkC.a, linkC.b };
                var counts = endpoints.GroupBy(x => x).ToDictionary(g => g.Key, g => g.Count());
                var verts = counts.Keys.ToArray();

                if (verts.Length != 3) return;

                int v0 = linkA.a;
                int v1 = linkA.b;
                int v2 = verts.First(x => x != v0 && x != v1);

                Vector3 p0 = masses[v0].position;
                Vector3 p1 = masses[v1].position;
                Vector3 p2 = masses[v2].position;
                Vector3 normal = Vector3.Cross(p1 - p0, p2 - p0);
                if (Vector3.Dot(normal, Vector3.up) < 0) (v1, v2) = (v2, v1);

                newTriangles[i] = (v0, v1, v2);
            });
            triangles.AddRange(newTriangles.Where(t => t != default));

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
                    if (nbCommon >= 2)
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
        /// <summary>
        /// Converts the current state of the simulation into a mesh
        /// </summary>
        /// <returns></returns>
        public Mesh ToMesh()
        {
            return new Mesh()
            {
                vertices = masses.Select(m => m.position).ToArray(),
                normals = masses.Select(m => m.normal).ToArray(),
                triangles = triangles
                    .Where(tri => tri.p1 != tri.p2 && tri.p2 != tri.p3 && tri.p3 != tri.p1)
                    .SelectMany(tri => new[] { tri.p1, tri.p2, tri.p3 })
                    .ToArray(),
            };
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

        public void AddForce(Vector3 force) => this.force += force;
        public void ApplyForce(float deltaTime)
        {
            velocity += force / mass * deltaTime;
            position += velocity * deltaTime;
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
