using Unity.Profiling;
using UnityEngine;

namespace SpringSim.V3
{
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
            if (!(float.IsFinite(velocity.x) && float.IsFinite(velocity.y) && float.IsFinite(velocity.z)))
            {
                velocity = Vector3.zero;
            }
            position += velocity * deltaTime;
            if (!(float.IsFinite(position.x) && float.IsFinite(position.y) && float.IsFinite(position.z)))
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