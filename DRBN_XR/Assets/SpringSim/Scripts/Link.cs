using System;
using UnityEngine;

namespace Assets.SpringSim
{

    [RequireComponent(typeof(LineRenderer))]
    public class Link : MonoBehaviour
    {
        public Mass a, b;
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
