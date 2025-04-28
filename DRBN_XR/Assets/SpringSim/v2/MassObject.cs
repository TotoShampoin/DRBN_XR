using System;
using UnityEngine;

namespace Assets.SpringSim.V2
{
    [RequireComponent(typeof(MeshRenderer), typeof(Rigidbody))]
    public class MassObject : MonoBehaviour
    {
        new Rigidbody rigidbody;
        MeshRenderer mesh;

        bool hovered = false;
        bool grabbed = false;
        // public Vector3 grabOrigin;
        // public Vector3 grabInfluenceTarget;

        public Vector3 initial;
        public bool returnToOrigin;

        public Vector3 Position
        {
            get => rigidbody.position;
            set => rigidbody.position = value;
        }
        public Vector3 Velocity
        {
            get => rigidbody.linearVelocity;
            set => rigidbody.linearVelocity = value;
        }
        public void AddForce(Vector3 force, ForceMode mode = ForceMode.Force) => rigidbody.AddForce(force, mode);
        public bool UseGravity
        {
            get => rigidbody.useGravity;
            set => rigidbody.useGravity = value;
        }
        public float comebackStiffness;
        public bool partiallyGrabbed = false;

        public SpringSimulator parentSimulator;

        void Start()
        {
            mesh = GetComponent<MeshRenderer>();
            rigidbody = GetComponent<Rigidbody>();
            initial = Position;

            var sim = transform.parent.gameObject.GetComponent<SpringSimulator>();
            if (sim) parentSimulator = sim;
        }

        void FixedUpdate()
        {
            if (returnToOrigin)
            {
                var k = comebackStiffness;
                var force = k * (initial - Position);
                AddForce(force);
            }
            // if (partiallyGrabbed)
            // {
            //     var k = comebackStiffness;
            //     var force = k * (grabInfluenceTarget - Position);
            //     AddForce(force);
            // }
        }

        Color transparent = new(0, 0, 0, 0);
        void Update()
        {
            // THIS ASSUMES A SPECIFIC SHADER!
            if (grabbed)
                mesh.material.color = Color.red;
            else if (partiallyGrabbed)
                mesh.material.color = Color.magenta;
            else if (hovered)
                mesh.material.color = Color.yellow;
            else
                mesh.material.color = transparent;
        }

        public void OnHovered() => hovered = true;
        public void OnUnhovered() => hovered = false;
        public void OnGrabbed()
        {
            grabbed = true;
            parentSimulator.OnMassGrabbed(this);
            // grabOrigin = Position;
        }
        public void OnUngrabbed()
        {
            grabbed = false;
            parentSimulator.OnMassUngrabbed(this);
        }

        public void PartialGrab()
        {
            partiallyGrabbed = true;
            // grabOrigin = Position;
        }
        public void PartialUngrab()
        {
            partiallyGrabbed = false;
        }
    }
}
