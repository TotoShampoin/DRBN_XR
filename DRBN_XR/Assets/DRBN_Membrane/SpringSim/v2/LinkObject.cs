using System;
using UnityEngine;

namespace SpringSim.V2
{

    [RequireComponent(typeof(LineRenderer))]
    public class LinkObject : MonoBehaviour
    {
        [NonSerialized] public MassObject a, b;
        [NonSerialized] public float length;
        LineRenderer line;

        void Awake()
        {
            line = GetComponent<LineRenderer>();
        }

        void OnEnable() => Place();

        void Update() => Place();

        void Place()
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
