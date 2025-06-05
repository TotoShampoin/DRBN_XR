using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Class used to slide a panel like an animation
/// </summary>
public class MovePanelOnButtonPress : MonoBehaviour
{
    [Tooltip("Panel to move")]
    public List<RectTransform> panel;

    [Tooltip("Distance on axis X")]
    public float moveDistance = -17.7f;

    [Tooltip("Speed movement")]
    public float moveSpeed = 10f;

    private Vector3 originalPosition;
    private Vector3 targetPosition;
    private bool isMoving = false;

    void Start()
    {
        if (panel != null)
        {

            originalPosition = panel[0].localPosition;
            targetPosition = originalPosition;
        }
    }

    void Update()
    {
        if (isMoving)
        {
            foreach (var p in panel)
            {
                p.localPosition = Vector3.Lerp(p.localPosition, targetPosition, Time.unscaledDeltaTime * moveSpeed);

                if (Vector3.Distance(p.localPosition, targetPosition) < 0.1f)
                {
                    isMoving = false;
                }
            }
        }
    }

    public void OnButtonPress()
    {
        isMoving = true;
        targetPosition = originalPosition + new Vector3(moveDistance, 0, 0);
    }

    public void ResetPanelPosition()
    {
        isMoving = true;
        targetPosition = originalPosition;
    }
}
