using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.Profiling;
using UnityEngine;

namespace SpringSim.V3
{
    public class SpringSimulatorNoBehaviour : MonoBehaviour
    {
        [Header("Parameters")]
        [SerializeField] float particleMass = 2f;
        [SerializeField] float stiffness = 1500f;
        [SerializeField] float viscosity = 2f;
        [SerializeField] float comeback = 1500f;
        [SerializeField] float drag = 0f;

        public float ParticleMass { get => particleMass; set => particleMass = value; }
        public float Stiffness { get => stiffness; set => stiffness = value; }
        public float Viscosity { get => viscosity; set => viscosity = value; }
        public float Comeback { get => comeback; set => comeback = value; }
        public float Drag { get => drag; set => drag = value; }

        static readonly ProfilerMarker iteration = new("Membrane.SpringSim.Iteration");

        public void Iterate(SpringSimulatorState state, float deltaTime)
        {
            iteration.Begin();
            var masses = state.masses;
            var links = state.links;
            var externalForces = state.externalForces;
            Parallel.For(0, links.Count, i =>
            {
                var link = links[i];
                var force = SpringForce(state, link);
                lock (masses[link.a]) { masses[link.a].AddForce(force); }
                lock (masses[link.b]) { masses[link.b].AddForce(-force); }
            });
            Parallel.ForEach(externalForces, ef =>
            {
                var (force, origin, radius) = ef;
                var nearby = state.GetSurrounding(origin, radius);
                Parallel.ForEach(nearby, mass =>
                {
                    var d = Vector3.Distance(mass.position, origin);
                    mass.AddForce(force * (1 - d / radius));
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
            iteration.End();
            state.UpdateLUT();
        }

        public Vector3 SpringForce(SpringSimulatorState state, SpringLink link)
        {
            var p1 = state.masses[link.a].position;
            var p2 = state.masses[link.b].position;
            var v1 = state.masses[link.a].velocity;
            var v2 = state.masses[link.b].velocity;

            var l0 = link.length;
            var k = stiffness;
            var d = Vector3.Distance(p1, p2);

            if (d == 0) return Vector3.zero;
            var dir = (p2 - p1).normalized;
            var springForce = k * (d - l0) * dir;
            var dampingForce = viscosity * (v2 - v1);
            return springForce + dampingForce;
        }
    }


    public class SpringSimulatorState
    {
        public Vector3 origin = Vector3.zero;
        public List<Mass> masses = new();
        public List<SpringLink> links = new();
        public List<(int p1, int p2, int p3)> triangles = new();
        public List<(Vector3 f, Vector3 o, float r)> externalForces = new();

        public List<Vector3> oldPositions = new();
        public SpatialHash<Mass> lutMasses;

        readonly static ProfilerMarker generateLut = new("Membrane.SpringSim.GenerateLUT");
        readonly static ProfilerMarker updateLut = new("Membrane.SpringSim.UpdateLUT");

        public void UpdateLUT()
        {
            if (lutMasses == null)
            {
                GenerateLUT();
                return;
            }
            updateLut.Begin();
            for (int i = 0; i < masses.Count; i++)
            {
                lutMasses.Move(oldPositions[i], masses[i].position, masses[i]);
            }
            updateLut.End();
        }
        public void GenerateLUT(float cellSize = 0.125f)
        {
            generateLut.Begin();
            lutMasses = new(cellSize);
            masses.ForEach(mass =>
            {
                lutMasses.AddAt(mass.position, mass);
                oldPositions.Add(mass.position);
            });
            generateLut.End();
        }

        public void ApplyForce(Vector3 force, Vector3 origin, float radius)
        {
            externalForces.Add((force, origin, radius));
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
        public List<Mass> GetSurrounding(Vector3 position, float distance) => lutMasses.GetSurrounding(position, distance);

        public Mass ClosestMass(Vector3 position)
        {
            float[] distance = masses.Select(m => Vector3.Distance(m.position, position)).ToArray();
            float minDist = float.MaxValue;
            int minIdx = -1;
            Parallel.For(
                0, distance.Length,
                () => (dist: float.MaxValue, idx: -1),
                (i, state, local) =>
                {
                    if (distance[i] < local.idx)
                        local = (distance[i], i);
                    return local;
                },
                local =>
                {
                    lock (distance)
                    {
                        if (local.dist < minDist)
                        {
                            minDist = local.dist;
                            minIdx = local.idx;
                        }
                    }
                }
            );
            return minIdx >= 0 ? masses[minIdx] : null;
        }
    }

}
