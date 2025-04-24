using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.SpringSim.V2
{
    public struct SpringLink
    {
        public int a;
        public int b;
        public float length;
    };

    public class SpringSimulator : MonoBehaviour
    {
        [Header("Properties")]
        public float stiffness = 1000f;
        public float viscosity = 2f;

        public Rigidbody massPrefab;
        public Bounds bounds;
        public float extractionEpsilon = 0.005f;
        public float forcedRate = 240f;

        private readonly List<Rigidbody> massObjects = new();
        private readonly List<SpringLink> links = new();

        private readonly List<Vector3> positionCache = new();
        private readonly List<Vector3> veloctiyCache = new();
        private readonly List<Vector3> forces = new();

        public void SetStiffness(float s) => stiffness = s;
        public void SetViscosity(float v) => viscosity = v;

        void Start()
        {
            Time.fixedDeltaTime = 1f / forcedRate;
        }

        void FixedUpdate()
        {
            for (int i = 0; i < massObjects.Count; i++)
            {
                positionCache[i] = massObjects[i].position;
                veloctiyCache[i] = massObjects[i].linearVelocity;
            }
            Parallel.For(0, links.Count, i =>
            {
                var link = links[i];
                forces[i] = Vector3.zero;

                var p1 = positionCache[link.a];
                var p2 = positionCache[link.b];
                var v1 = veloctiyCache[link.a];
                var v2 = veloctiyCache[link.b];

                var l0 = link.length;
                var k = stiffness;
                var d = Vector3.Distance(p1, p2);

                if (d == 0) return;

                forces[i] += k * (1 - l0 / d) * (p2 - p1);
                forces[i] += viscosity * (v2 - v1);

            });
            for (int i = 0; i < massObjects.Count; i++)
            {
                massObjects[i].AddForce(forces[i]);
            }
        }

        void OnDrawGizmos()
        {
            Gizmos.color = Color.white;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(bounds.center, bounds.size);
        }

        public void ForEach(Action<int, Rigidbody> massCallback, Action<int, SpringLink> linkCallback)
        {
            for (int i = 0; i < Mathf.Max(massObjects.Count, links.Count); i++)
            {
                if (i < massObjects.Count) massCallback(i, massObjects[i]);
                if (i < links.Count) linkCallback(i, links[i]);
            }
        }

        public void Clear()
        {
            massObjects.ForEach(rb => Destroy(rb.gameObject));
            massObjects.Clear();
            links.Clear();
            positionCache.Clear();
            veloctiyCache.Clear();
            forces.Clear();
        }
        public void UseMesh(Mesh mesh)
        {
            Mesh dmesh = MeshMod.DeduplicateVertices(mesh, extractionEpsilon);
            var positions = dmesh.vertices;
            MeshMod.RescaleToBounds(ref positions, dmesh.bounds, bounds);
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
                    return mass;
                }));
            this.links.AddRange(links.Values);
            positionCache.AddRange(positions);
            veloctiyCache.AddRange(positions.Select(_ => Vector3.zero));
            forces.AddRange(links.Select(_ => Vector3.zero));
        }
    }
}
