using UnityEditor.EditorTools;
using UnityEngine;

/// <summary>
/// DashedLine class to create a dashed line between two points in 3D space.
/// Can be a little bit heavy on performance if the line is long and need to be updated frequently. (if the object attached to is moving)
/// Also can be buggy on some strange circumstances. Maybe need another approach to draw the line.
/// </summary>

public class DashedLine : MonoBehaviour
{
    [Tooltip("Start point of the dashed line")]
    public Transform startPoint;

    [Tooltip("End point of the dashed line")]
    public Transform endPoint;

    [Tooltip("Length of each dash")]
    public float dashLength = 0.1f;

    [Tooltip("Length of the gap between dashes")]
    public float gapLength = 0.05f;

    [Tooltip("Material to use for the dashes")]
    public Material dashMaterial;

    [Tooltip("Width of the dashes")]
    public float lineWidth = 0.01f;

    [Header("Scroll Environment")]

    [Tooltip("If true, the line will be displayed only if the minY and maxY are in the bounds of the scroll")]
    public bool isInScrollEnvironment = false;

    [Tooltip("Target object to check the Y position (used in scrolling when attached to a UI to disable the line when out of bounds of the scroll). If not in a scroll environment, targetObject and minY/maxY will be ignored")]
    public GameObject targetObject;

    [Tooltip("Minimum Y position to display the line (used in scrolling when attached to a UI to disable the line when out of bounds of the scroll). If not in a scroll environment this will be ignored")]
    public float minY = 0f;

    [Tooltip("Maximum Y position to display the line (used in scrolling when attached to a UI to disable the line when out of bounds of the scroll). If not in a scroll environment this will be ignored")]
    public float maxY = 115f;

    private Vector3 lastStart;
    private Vector3 lastEnd;

    void Update()
    {
        float targetY = targetObject.transform.parent.InverseTransformPoint(endPoint.transform.position).y;

        // Verify if targetY is in the interval [minY, maxY] or if not in a scroll environment
        if ((targetY >= minY && targetY <= maxY) || !isInScrollEnvironment)
        {
            // Redraw the line only if the start and end points have changed
            if (startPoint.position != lastStart || endPoint.position != lastEnd)
            {
                lastStart = startPoint.position;
                lastEnd = endPoint.position;
                Redraw();
            }
        }
        else
        {
            // Else destroy the line
            if (transform.childCount > 0)
            {
                foreach (Transform child in transform)
                {
                    Destroy(child.gameObject);
                }
            }
        }
    }   

    private void Redraw()
    {
        
        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }

        Vector3 direction = endPoint.position - startPoint.position;
        float distance = direction.magnitude;
        direction.Normalize();

        float currentLength = 0f;
        int index = 0;

        while (currentLength < distance)
        {
            float segmentLength = Mathf.Min(dashLength, distance - currentLength);

            Vector3 segmentStart = startPoint.position + direction * currentLength;
            Vector3 segmentEnd = segmentStart + direction * segmentLength;

            GameObject dash = new GameObject($"Dash_{index++}");
            dash.transform.parent = transform;

            LineRenderer lr = dash.AddComponent<LineRenderer>();
            lr.material = dashMaterial;
            lr.positionCount = 2;
            lr.SetPosition(0, segmentStart);
            lr.SetPosition(1, segmentEnd);
            lr.startWidth = lineWidth;
            lr.endWidth = lineWidth;
            lr.useWorldSpace = true;
            lr.alignment = LineAlignment.TransformZ;

            currentLength += dashLength + gapLength;
        }
    }
}
