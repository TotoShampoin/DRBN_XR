using UnityEngine;

/// <summary>
/// Class used for the UI User to follow the player
/// </summary>
public class UIAnchor : MonoBehaviour
{
    [Tooltip("XR camera")]
    public Transform target;

    [Tooltip("Distance between the player and the UI")]
    public float distance = 1.0f;

    [Tooltip("Speed of the movement")]
    public float moveSpeed = 10f;

    private float lastYaw = 0f;     // Store the last yaw angle
    private Vector3 targetPosition;
    private Quaternion targetRotation;
    private bool isMoving = false;

    void Start()
    {
        if (target != null)
        {
            UpdateTargetPositionAndRotation();
        }
    }

    void LateUpdate()
    {
        if (target != null)
        {
            float currentYaw = target.eulerAngles.y;

            currentYaw = currentYaw % 360;
            if (currentYaw < 0) currentYaw += 360;

            if (Mathf.Abs(currentYaw - lastYaw) >= 30f)
            {
                float roundedYaw = Mathf.Round(currentYaw / 30f) * 30f;

                lastYaw = roundedYaw + 1;

                UpdateTargetPositionAndRotation(roundedYaw + 1);

                isMoving = true;
            }
        }
    }

    void Update()
    {
        if (isMoving)
        {
            transform.position = Vector3.Lerp(transform.position, targetPosition, Time.unscaledDeltaTime * moveSpeed);

            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.unscaledDeltaTime * moveSpeed);

            if (Vector3.Distance(transform.position, targetPosition) < 0.1f && Quaternion.Angle(transform.rotation, targetRotation) < 1f)
            {
                isMoving = false;
            }
        }
    }

    private void UpdateTargetPositionAndRotation(float yaw = 0f)
    {
        Vector3 flatForward = new Vector3(target.forward.x, 0, target.forward.z).normalized;

        targetPosition = target.position + flatForward * distance;

        targetRotation = Quaternion.Euler(0, yaw, 0);
    }
}