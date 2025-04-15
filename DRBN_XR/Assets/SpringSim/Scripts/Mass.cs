using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace Assets.SpringSim
{

    public class Mass : MonoBehaviour
    {
        [NonSerialized] public Vector3 position;
        [NonSerialized] public float size;
        [NonSerialized] public float mass;
        [NonSerialized] public bool isSelected = false;
        Rigidbody rb;
        Material material;
        Transform originalParent; // Necessary because XRGrab actually changes the object's parent, which disrupts the simulation

        void OnEnable()
        {
            rb = GetComponent<Rigidbody>();
            material = GetComponent<MeshRenderer>().material;
            originalParent = transform.parent;

            material.color = Color.white;
        }

        void Update()
        {
            if (isSelected)
            {
                position = originalParent.InverseTransformPoint(transform.position);
            }
            else
            {
                transform.position = originalParent.TransformPoint(position);
            }

            rb.mass = mass;
            transform.localScale = size * Vector3.one;
        }

        public void Select()
        {
            isSelected = true;
            material.color = Color.red;
        }
        public void Deselect()
        {
            isSelected = false;
            material.color = Color.white;
        }
        public void Hover()
        {
            material.color = Color.yellow;
        }
        public void DeHover()
        {
            if (!isSelected)
                material.color = Color.white;
        }
    }
}
