using System;
using System.Collections.Generic;
using UnityEngine;

namespace SpringSim.V1
{

    [RequireComponent(
        typeof(Rigidbody),
        typeof(MeshRenderer))]
    public class Mass : MonoBehaviour
    {
        [NonSerialized] public Vector3 position;
        [NonSerialized] public Vector3 initial;
        [NonSerialized] public Vector3 velocity = Vector3.zero;
        [NonSerialized] public Vector3 tmpVelocity = Vector3.zero;
        [NonSerialized] public bool isSelected = false;
        [NonSerialized] public bool useInitial = true;
        [NonSerialized] public bool mark = true;
        Rigidbody rb;
        Material material;
        Transform originalParent; // Necessary because XRGrab actually changes the object's parent, which disrupts the simulation
        Color color;

        [NonSerialized] public static float size;
        [NonSerialized] public static float mass;
        [NonSerialized] public static float comebackForce;
        [NonSerialized] public static float dragForce;
        [NonSerialized] public static float avoidForce;
        [NonSerialized] public static float avoidRadius;

        readonly List<Collider> triggerList = new();

        void OnEnable()
        {
            rb = GetComponent<Rigidbody>();
            material = GetComponent<MeshRenderer>().material;
            originalParent = transform.parent;
            color = Color.white;
        }

        void Update()
        {
            if (isSelected)
            {
                position = originalParent.InverseTransformPoint(rb.position);
            }
            else
            {
                rb.position = originalParent.TransformPoint(position);
            }

            rb.mass = mass;
            transform.localScale = size * Vector3.one;

            if (mark) material.color = Color.magenta;
            else material.color = color;
        }

        public Vector3 ComebackForce()
        {
            if (!useInitial) return Vector3.zero;
            return (position - initial) * -comebackForce;
        }
        public Vector3 DragForce()
        {
            return -dragForce * mass * tmpVelocity;
        }

        public Vector3 AvoidForce(SpatialHash<Mass> masses)
        {
            var p = position;
            Vector3 F = Vector3.zero;
            foreach (var _m in masses.GetSurrounding(position, avoidForce))
            {
                var _p = _m.position;
                float factor = Mathf.SmoothStep(1, 0, Mathf.InverseLerp(0f, avoidRadius, Vector3.Distance(p, _p)));
                var direction = _p == p ? Vector3.zero : Vector3.Normalize(_p - p);
                F -= avoidForce * factor * direction;
            }
            return F;
        }

        public void Select()
        {
            isSelected = true;
            color = Color.red;
        }
        public void Deselect()
        {
            isSelected = false;
            color = Color.white;
        }
        public void Hover()
        {
            color = Color.yellow;
        }
        public void DeHover()
        {
            if (!isSelected)
                color = Color.white;
        }
    }
}
