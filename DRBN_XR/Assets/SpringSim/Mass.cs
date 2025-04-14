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
        [NonSerialized] public bool isSelected;
        Rigidbody rb;
        XRGrabInteractable grab;

        void OnEnable()
        {
            rb = GetComponent<Rigidbody>();
            grab = GetComponent<XRGrabInteractable>();
        }

        void Update()
        {
            // isSelected = Selection.activeGameObject == gameObject;
            if (rb)
            {
                rb.linearVelocity = new();
                rb.angularVelocity = new();
            }
            if (isSelected)
            {
                position = transform.localPosition;
            }
            else
            {
                transform.localPosition = position;
                if (rb) rb.mass = mass;
            }

            transform.localScale = size * Vector3.one;
        }

        void OnDrawGizmos()
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireSphere(Vector3.zero, 0.5f);
        }

        public void Select()
        {
            isSelected = true;
        }
        public void Deselect()
        {
            isSelected = false;
        }
    }
}
