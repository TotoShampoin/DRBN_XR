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

        public Vector3 Position => rigidbody.position;
        public Vector3 Velocity => rigidbody.linearVelocity;
        public void AddForce(Vector3 force, ForceMode mode = ForceMode.Force) => rigidbody.AddForce(force, mode);


        void Start()
        {
            mesh = GetComponent<MeshRenderer>();
            rigidbody = GetComponent<Rigidbody>();
        }

        void Update()
        {
            if (rigidbody.isKinematic)
                mesh.material.color = Color.red;
            else
                mesh.material.color = hovered ? Color.yellow : Color.white;
        }

        public void OnHovered() => hovered = true;
        public void OnUnhovered() => hovered = false;
    }
}
