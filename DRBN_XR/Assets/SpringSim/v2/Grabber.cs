using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace Assets.SpringSim.V2
{
    [RequireComponent(typeof(XRGrabInteractable), typeof(MeshRenderer))]
    public class Grabber : MonoBehaviour
    {
        bool isHovered;

        public bool IsGrabbed => xrgi.isSelected;
        public bool IsHovered => isHovered;

        public Vector3 origin;
        public Vector3 Position { get => transform.position; set => transform.position = value; }

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
                mr.material.color = transparent;
            else if (IsGrabbed)
                mr.material.color = Color.yellow;
            else
                mr.material.color = Color.red;
        }

        public void OnGrab()
        {
            if (simulator) simulator.Grab();
            origin = Position;
        }
        public void OnUngrab()
        {
            if (simulator) simulator.Ungrab();
        }

        public void OnHovered()
        {
            Debug.Log("hover");
            isHovered = true;
        }
        public void OnUnhovered()
        {
            Debug.Log("unhover");
            isHovered = IsGrabbed;
        }
    }
}
