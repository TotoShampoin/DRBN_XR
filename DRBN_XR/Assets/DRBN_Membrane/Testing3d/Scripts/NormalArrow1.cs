using TMPro;
using UnityEditor;
using UnityEngine;

public class NormalArrow1 : MonoBehaviour
{
    public LineRenderer lineRenderer;
    public TextMeshPro textMeshPro;

    public Vector3 normal;

    public Vector3 Origin
    {
        get => transform.position;
        set => transform.position = value;
    }
    public Vector3 Direction
    {
        get => normal;
        set => normal = value;
    }
    public float Distance
    {
        set => textMeshPro.text = $"{value:F2}";
    }

    void Update()
    {
        lineRenderer.SetPosition(0, transform.position);
        lineRenderer.SetPosition(1, transform.position + normal * 0.25f);
        Camera camera =
            SceneView.currentDrawingSceneView?.camera ??
            Camera.main ??
            null;
        if (camera != null)
        {
            textMeshPro.transform.position = transform.position + Vector3.up * (float)(0.25 * 0.25);
            textMeshPro.transform.LookAt(camera.transform);
            textMeshPro.transform.Rotate(0, 180f, 0);
        }
    }
}
