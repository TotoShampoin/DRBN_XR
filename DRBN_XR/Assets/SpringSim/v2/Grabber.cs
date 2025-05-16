using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace Assets.SpringSim.V2
{
    [RequireComponent(typeof(XRGrabInteractable), typeof(MeshRenderer))]
    public class Grabber : MonoBehaviour
    {
        public bool IsGrabbed => xrgi.isSelected;

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

        public void OnGrab()
        {
            simulator?.Grab();
            origin = Position;
        }
        public void OnUngrab()
        {
            simulator?.Ungrab();
        }

        public void OnHovered()
        {
            Debug.Log("hover");
            mr.enabled = true;
        }
        public void OnUnhovered()
        {
            Debug.Log("unhover");
            mr.enabled = IsGrabbed;
        }
    }
}
