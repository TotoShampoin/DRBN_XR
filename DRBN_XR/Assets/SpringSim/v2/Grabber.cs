using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace Assets.SpringSim.V2
{
    [RequireComponent(typeof(XRGrabInteractable), typeof(MeshRenderer))]
    public class Grabber : MonoBehaviour
    {
        public bool IsGrabbed => xrgi.isSelected;

        MeshRenderer mr;
        XRGrabInteractable xrgi;

        void Start()
        {
            mr = GetComponent<MeshRenderer>();
            xrgi = GetComponent<XRGrabInteractable>();
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
