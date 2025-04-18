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
        [SerializeField] float dragForce = 5f;

        [Header("Simulation")]
        [SerializeField] float rate = 480f;
        [SerializeField] bool divideWhenTooLong = true;

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
        [SerializeField] float extractionEpsilon = 0.005f;

        private readonly List<Mass> massBodies = new();
        private readonly List<Link> linkObjects = new();
        private SpatialHash<Mass> massHash;
        private int massCount = 0;
        private int linkCount = 0;

        public Mesh EntryPoint
        {
            get => entryPoint;
            set => entryPoint = value;
        }

        public void Reset() => reset = true;
        public void SetStiffness(float stiffness) => linkStiffness = stiffness;
        public void SetComeback(float comeback) => comebackForce = comeback;
        public void SetDamping(float damping) => this.dragForce = damping;
        public void SetDivide(bool divide) => divideWhenTooLong = divide;
        public void SetAvoidForce(float force) => avoidForce = force;
        public void Clear()
        {
            massBodies.ForEach(mb => Destroy(mb.gameObject));
            massBodies.Clear();
            linkObjects.ForEach(lb => Destroy(lb.gameObject));
            linkObjects.Clear();
            massHash.Clear();
        }

        void Start()
        {
            Time.fixedDeltaTime = 1.0f / rate;
            if (entryPoint) ExtractMesh(entryPoint);
            massHash = new(avoidRadius / 2f);
        }

        void FixedUpdate()
        {
            if (reset)
            {
                ExtractMesh(entryPoint);
                reset = false;
            }

            var delta = Time.deltaTime;
            Parallel.ForEach(linkObjects, (link) =>
            {
                var F = link.GetForce() / particleMass * delta;
                link.a.tmpVelocity += F;
                link.b.tmpVelocity -= F;

                link.a.mark = false;
            });
            Parallel.ForEach(massBodies, (mass) =>
            {
                mass.tmpVelocity += (
                    // mass.AvoidForce(massHash) +
                    mass.ComebackForce() +
                    mass.DragForce()
                ) / particleMass * delta;

                if (mass.isSelected)
                {
                    mass.tmpVelocity = new(0, 0, 0);
                    massHash
                        .GetSurrounding(mass.position, avoidRadius)
                        .ForEach(m => m.mark = true);
                }
                mass.velocity = mass.tmpVelocity;

                var oldPosition = mass.position;
                mass.position += mass.velocity * delta;
                massHash.Move(oldPosition, mass.position, mass);
            });
            Mass.size = displaySize;
            Mass.mass = particleMass;
            Mass.comebackForce = comebackForce;
            Mass.dragForce = dragForce;
            Mass.avoidForce = avoidForce;
            Mass.avoidRadius = avoidRadius;
            Link.stiffness = linkStiffness;

            if (divideWhenTooLong)
            {
                var lc = linkCount;
                for (int i = 0; i < lc; i++)
                {
                    var link = linkObjects[i];
                    if (link.DistanceBetweenMasses() > link.length * 2f)
                    {
                        SplitLink(link);
                    }
                }
            }
        }

        public void ToMass(Mass mass, Vector3 p, bool useInitial = true)
        {
            mass.position = p;
            mass.initial = p;
            mass.velocity = Vector3.zero;
            mass.tmpVelocity = Vector3.zero;
            mass.useInitial = useInitial;
        }
        public void ToLink(Link link, Mass a, Mass b, float length)
        {
            link.a = a;
            link.b = b;
            link.length = length;
        }
        public void ToLink(Link link, SpringLink springLink) =>
            ToLink(
                link,
                massBodies[springLink.a],
                massBodies[springLink.b],
                springLink.length);
        public Mass AddMass(Vector3 p, bool useInital = true)
        {
            var mass = massBodies[massCount - 1];
            if (mass.gameObject.activeInHierarchy)
            {
                mass = Instantiate(massPrefab, transform);
                massBodies.Add(mass);
            }
            mass.gameObject.SetActive(true);
            ToMass(mass, p, useInital);
            massHash.AddAt(p, mass);
            return mass;
        }
        public Link AddLink(Mass a, Mass b, float length)
        {
            var link = linkObjects[linkCount - 1];
            if (link.gameObject.activeInHierarchy)
            {
                link = Instantiate(linkPrefab, transform);
                linkObjects.Add(link);
            }
            link.gameObject.SetActive(true);
            ToLink(link, a, b, length);
            return link;
        }
        public Link AddLink(SpringLink l) =>
            AddLink(massBodies[l.a], massBodies[l.b], l.length);
        public void SplitLink(Link linkA)
        {
            var a = linkA.a;
            var b = linkA.b;

            var m = AddMass((a.position + b.position) / 2f, false);
            var linkB = AddLink(m, b, 0);
            linkB.length = linkB.DistanceBetweenMasses();

            linkA.a = a;
            linkA.b = m;
            linkA.length = linkA.DistanceBetweenMasses();

            a.useInitial = false;
            b.useInitial = false;
        }

        void PreparePool(int massCount, int linkCount)
        {
            for (int i = massBodies.Count; i < massCount; i++)
            {
                massBodies.Add(Instantiate(massPrefab, transform));
                massBodies[i].gameObject.SetActive(true);
                this.massCount = massCount;
            }
            for (int i = linkObjects.Count; i < linkCount; i++)
            {
                linkObjects.Add(Instantiate(linkPrefab, transform));
                linkObjects[i].gameObject.SetActive(true);
                this.linkCount = linkCount;
            }
        }

        void Fill(IEnumerable<Vector3> positions, IEnumerable<SpringLink> links)
        {
            PreparePool(positions.Count(), links.Count());
            for (int i = 0; i < massBodies.Count; i++)
            {
                if (i < positions.Count())
                {
                    var position = positions.ElementAt(i);
                    massBodies[i].gameObject.SetActive(true);
                    ToMass(massBodies[i], position);
                    massHash.AddAt(position, massBodies[i]);
                }
                else
                {
                    massBodies[i].gameObject.SetActive(false);
                }
            }
            for (int i = 0; i < linkObjects.Count; i++)
            {
                if (i < links.Count())
                {
                    var link = links.ElementAt(i);
                    linkObjects[i].gameObject.SetActive(true);
                    ToLink(linkObjects[i], link);
                }
                else
                {
                    linkObjects[i].gameObject.SetActive(false);
                }
            }
        }

        public void ExtractMesh(Mesh mesh)
        {
            Mesh dmesh = MeshMod.DeduplicateVertices(mesh, extractionEpsilon);
            var positions = dmesh.vertices;
            if (rescaleToBounds)
                MeshMod.RescaleToBounds(ref positions, dmesh.bounds, boundSize);

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
            Fill(positions, links.Values);
        }
    }
}
