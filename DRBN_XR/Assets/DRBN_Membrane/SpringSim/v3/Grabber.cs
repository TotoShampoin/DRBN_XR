using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace SpringSim.V3
{
    [RequireComponent(typeof(XRGrabInteractable), typeof(MeshRenderer))]
    public class Grabber : MonoBehaviour
    {
        bool isHovered;

        public bool IsGrabbed => xrgi.isSelected;
        public bool IsHovered => isHovered;

        public Vector3 origin;
        public Vector3 Position { get => transform.position; set => transform.position = value; }
        public Vector3 Delta => Position - origin;

        MeshRenderer mr;
        XRGrabInteractable xrgi;

        public SpringSimulator simulator;

        void Start()
        {
            mr = GetComponent<MeshRenderer>();
            xrgi = GetComponent<XRGrabInteractable>();
        }

        Color transparent = new(0, 0, 0, 0);
        void Update()
        {
            if (isHovered)
                mr.material.color = Color.yellow;
            else if (IsGrabbed)
                mr.material.color = Color.red;
            else
                mr.material.color = transparent;
        }

        public void ResetOrigin() => origin = Position;

        public void OnGrab()
        {
            if (simulator) simulator.Grab();
            ResetOrigin();
        }
        public void OnUngrab()
        {
            if (simulator) simulator.Ungrab();
        }

        public void OnHovered() => isHovered = true;
        public void OnUnhovered() => isHovered = IsGrabbed;
    }
}
