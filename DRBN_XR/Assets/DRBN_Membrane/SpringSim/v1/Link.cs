using System;
using UnityEngine;

namespace SpringSim.V1
{

    [RequireComponent(typeof(LineRenderer))]
    public class Link : MonoBehaviour
    {
        public Mass a, b;
        [NonSerialized] public float length;
        LineRenderer line;

        [NonSerialized] public static float stiffness;
        [NonSerialized] public static float viscosity;

        void Awake()
        {
            line = GetComponent<LineRenderer>();
        }

        void Update()
        {
            if (a == null || b == null)
            {
                line.SetPositions(new Vector3[0]);
                return;
            }
            line.SetPositions(new[]{
                a.transform.position,
                b.transform.position,
            });
        }

        public Vector3 GetForce()
        {
            var F = Vector3.zero;

            var p1 = a.position;
            var p2 = b.position;
            var v1 = a.velocity;
            var v2 = b.velocity;

            var l0 = length;
            var k = stiffness;
            var d = Vector3.Distance(p1, p2);

            if (d == 0) return F;

            F += k * (1 - l0 / d) * (p2 - p1);
            F += viscosity * (v2 - v1);

            return F;
        }

        public float DistanceBetweenMasses()
        {
            return Vector3.Distance(a.position, b.position);
        }
    }
}
