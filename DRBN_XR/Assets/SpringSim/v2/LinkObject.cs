using System;
using UnityEngine;

namespace Assets.SpringSim.V2
{

    [RequireComponent(typeof(LineRenderer))]
    public class LinkObject : MonoBehaviour
    {
        // public GameObject a, b;
        public MassObject a, b;
        [NonSerialized] public float length;
        LineRenderer line;

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
    }
}
