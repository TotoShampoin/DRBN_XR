using UnityEngine;
using UnityEngine.XR;
using UnityEngine.EventSystems;

/// <summary>
/// HapticOnHover class to manage haptic feedback on hover events.
/// This class must be attached to the GameObject that will trigger the haptic feedback when hovered over.
/// </summary>

public class HapticOnHover : MonoBehaviour, IPointerEnterHandler
{
    [Tooltip("Input device characteristics to filter devices. (by default, it will be the right controller)")]
    public InputDeviceCharacteristics characteristics = InputDeviceCharacteristics.Right | InputDeviceCharacteristics.Controller;

    [Tooltip("Amplitude of the haptic feedback.")]
    public float amplitude = 0.5f;

    [Tooltip("Duration of the haptic feedback in seconds.")]
    public float duration = 0.1f;

    public void OnPointerEnter(PointerEventData eventData)
    {
        var devices = new System.Collections.Generic.List<InputDevice>();
        InputDevices.GetDevicesWithCharacteristics(characteristics, devices);

        foreach (var device in devices)
        {
            if (device.TryGetHapticCapabilities(out var capabilities) && capabilities.supportsImpulse)
            {
                device.SendHapticImpulse(0u, amplitude, duration);
            }
        }
    }
}
