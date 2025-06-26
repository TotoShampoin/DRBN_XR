using System.Collections.Generic;
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
        static readonly ProfilerMarker iterationLinks = new("Membrane.SpringSim.Iteration.SpringForces");
        static readonly ProfilerMarker iterationExternals = new("Membrane.SpringSim.Iteration.ExternalForces");
        static readonly ProfilerMarker iterationDrag = new("Membrane.SpringSim.Iteration.Drag");
        static readonly ProfilerMarker iterationMasses = new("Membrane.SpringSim.Iteration.ApplyForces");

        public void Iterate(SpringSimulatorState state, float deltaTime)
        {
            Mass.mass = particleMass;
            using (iteration.Auto())
            {
                var masses = state.masses;
                var links = state.links;
                var externalForces = state.externalForces;
                using (iterationLinks.Auto())
                {
                    Parallel.For(0, links.Count, i =>
                    {
                        var link = links[i];
                        var force = SpringForce(state, link);
                        lock (masses[link.a]) { masses[link.a].AddForce(force); }
                        lock (masses[link.b]) { masses[link.b].AddForce(-force); }
                    });
                }
                using (iterationExternals.Auto())
                {
                    Parallel.ForEach(externalForces, ef =>
                    {
                        var (force, origin, radius) = ef;
                        var nearby = state.GetSurrounding(origin, radius);
                        foreach (var mass in nearby)
                        {
                            var d = Vector3.Distance(mass.position, origin);
                            mass.AddForce(force * (1 - d / radius));
                        }
                    });
                    externalForces.Clear();
                }
                using (iterationDrag.Auto())
                {
                    Parallel.ForEach(masses, m =>
                    {
                        if (m.velocity == Vector3.zero) return;
                        var dir = Vector3.Normalize(m.velocity);
                        var mag = Vector3.Magnitude(m.velocity);
                        m.AddForce(-mag * mag * drag * dir);
                    });
                }
                using (iterationMasses.Auto()) { Parallel.ForEach(masses, m => m.ApplyForce(deltaTime)); }
            }
            state.UpdateLUT();
        }

        public Vector3 SpringForce(SpringSimulatorState state, SpringLink link)
        {
            var p1 = state.masses[link.a].position;
            var p2 = state.masses[link.b].position;
            var v1 = state.masses[link.a].velocity;
            var v2 = state.masses[link.b].velocity;
            var springForce = SpringPull(p1, p2, link.length, stiffness);
            var dampingForce = viscosity * (v2 - v1);
            return springForce + dampingForce;
        }
        static public Vector3 SpringPull(Vector3 p1, Vector3 p2, float length, float stiffness = 1500f)
        {
            var distance = Vector3.Distance(p1, p2);
            if (distance == 0) return Vector3.zero;
            var direction = (p2 - p1).normalized;
            return stiffness * (distance - length) * direction;
        }
    }


    public class SpringSimulatorState
    {
        public Vector3 origin = Vector3.zero;
        public float offsetFactor = 2f;
        public List<Mass> masses = new();
        public List<SpringLink> links = new();
        public List<(int p1, int p2, int p3)> triangles = new();
        public List<(Vector3 f, Vector3 o, float r)> externalForces = new();

        public List<Vector3> oldPositions = new();
        public SpatialHash<Mass> lutMasses;

        readonly static ProfilerMarker generateLut = new("Membrane.SpringSim.GenerateLUT");
        readonly static ProfilerMarker updateLut = new("Membrane.SpringSim.UpdateLUT");
        readonly static ProfilerMarker surroundingFetching = new("Membrane.SpringSim.GetSurrounding");

        public Vector3 LocalToGlobalPosition(Vector3 position) => (position + origin) / offsetFactor;
        public Vector3 GlobalToLocalPosition(Vector3 position) => position * offsetFactor - origin;

        public List<(Mass self, Mass other)> Join(SpringSimulatorState with, float tolerance = 0.0001f)
        {
            List<(Mass, Mass)> result = new();
            masses.ForEach(mA =>
            {
                var mAp = LocalToGlobalPosition(mA.position);
                var mB = with.ClosestMassGlobal(mAp);
                if (mB == null) return;
                var mBp = with.LocalToGlobalPosition(mB.position);
                if (Vector3.Distance(mAp, mBp) < tolerance) result.Add((mA, mB));
            });
            return result;
        }

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

        public void AddExternalForce(Vector3 force, Vector3 from, float radius)
        {
            externalForces.Add((force, from, radius));
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
        // public List<Mass> GetSurrounding(Vector3 position, float distance) => lutMasses.GetSurrounding(position, distance); // This doesn't work :(
        public List<Mass> GetSurrounding(Vector3 position, float distance)
        {
            using (surroundingFetching.Auto())
            {
                var result = new List<Mass>();
                foreach (var mass in masses)
                {
                    if (Vector3.Distance(mass.position, position) <= distance)
                        result.Add(mass);
                }
                return result;
            }
        }

        public Mass ClosestMassLocal(Vector3 position)
        {
            float minDist = float.MaxValue;
            int minIdx = -1;
            for (int i = 0; i < masses.Count; i++)
            {
                var dist = Vector3.Distance(masses[i].position, position);
                if (dist < minDist)
                {
                    minDist = dist;
                    minIdx = i;
                }
            }
            return minIdx >= 0 ? masses[minIdx] : null;
        }
        public Mass ClosestMassGlobal(Vector3 position)
        {
            return ClosestMassLocal(GlobalToLocalPosition(position));
        }
    }

}
