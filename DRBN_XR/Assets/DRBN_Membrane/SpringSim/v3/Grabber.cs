using UnityEngine;
using UnityEngine.InputSystem;
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

        public InputActionReference click;
        public InputActionReference joystick;

        MeshRenderer mr;
        XRGrabInteractable xrgi;

        public SpringSimulator simulator;

        void Start()
        {
            mr = GetComponent<MeshRenderer>();
            xrgi = GetComponent<XRGrabInteractable>();

            click.action.Enable();
            joystick.action.Enable();

            click.action.canceled += (_) => OnClickRelease();
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

            if (joystick != null && joystick.action.enabled && IsGrabbed)
            {
                float yValue = joystick.action.ReadValue<Vector2>().y;
                if (Mathf.Abs(yValue) > 0.01f)
                {
                    OnChange(yValue);
                }
            }
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

        public void OnClickRelease()
        {
            if (simulator && isHovered) simulator.Impact();
        }

        public void OnHovered() => isHovered = true;
        public void OnUnhovered() => isHovered = IsGrabbed;
        public void OnChange(float yValue) => simulator.ChangeRigidity(yValue * Time.deltaTime);
    }
}
