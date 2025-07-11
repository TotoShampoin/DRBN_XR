using UnityEngine;
using UnityEditor;

[RequireComponent(typeof(LineRenderer))]
public class NormalArrow2 : MonoBehaviour
{
    public float delta = 0.0001f;
    public MeshFilter input;

    LineRenderer lineRenderer;

    public float Distance => DF(input.sharedMesh, transform.position);

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        lineRenderer = GetComponent<LineRenderer>();
    }

    // Update is called once per frame
    void Update()
    {
        var mesh = input.sharedMesh;
        float dist = DF(mesh, transform.position);
        float x0 = DF(mesh, transform.position + Vector3.right * delta);
        float y0 = DF(mesh, transform.position + Vector3.up * delta);
        float z0 = DF(mesh, transform.position + Vector3.forward * delta);
        float x1 = DF(mesh, transform.position - Vector3.right * delta);
        float y1 = DF(mesh, transform.position - Vector3.up * delta);
        float z1 = DF(mesh, transform.position - Vector3.forward * delta);

        lineRenderer.SetPosition(0, transform.position);
        lineRenderer.SetPosition(1, transform.position + new Vector3((x0 - x1) / 2f, (y0 - y1) / 2f, (z0 - z1) / 2f).normalized);

        if (dist < 0)
            lineRenderer.material.color = new Color(-dist, -dist * 0.5f, 0f);
        else
            lineRenderer.material.color = new Color(0f, dist * 0.5f, dist);
    }

    // float DF(Mesh mesh, Vector3 point) => DistanceToMesh.UnsignedDistanceFunction(mesh, point);
    static float DF(Mesh mesh, Vector3 point) => DistanceToMesh.SignedDistanceFunction(mesh, point);
}

[CustomEditor(typeof(NormalArrow2))]
public class NormalArrow2Editor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        NormalArrow2 normalArrow = (NormalArrow2)target;

        if (!Application.isPlaying) return;
        EditorGUILayout.Space(9);
        EditorGUILayout.LabelField($"Distance: {normalArrow.Distance}");
    }
}
