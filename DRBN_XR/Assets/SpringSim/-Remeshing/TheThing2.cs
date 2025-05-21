using System.Threading.Tasks;
using Assets.SpringSim.V2;
using UnityEditor;
using UnityEngine;

public class TheThing2 : MonoBehaviour
{
    public Mesh test0;
    public Mesh test1;

    public Material material0;
    public Material material1;
    public Textbox textbox;

    [Range(0, 1)] public float tolerance = 0.2f;

    void Awake()
    {
        Remesh();
    }

    void Update()
    {
        Graphics.DrawMesh(test0, Matrix4x4.Translate(new(0, 0, 0)), material0, 0);
        Graphics.DrawMesh(test1, Matrix4x4.Translate(new(0, 0, 0)), material1, 0);
    }

    public void Remesh()
    {
        foreach (Transform child in transform) Destroy(child.gameObject);
        var distances = MeshMod.DistanceOfVertices(test0.vertices, test1.vertices);
        var max = Mathf.Max(distances);
        for (int i = 0; i < distances.Length; i++)
        {
            var pos = test0.vertices[i];
            var tb = Instantiate(textbox, pos + Vector3.up * 0.1f, Quaternion.identity, transform);
            tb.Text = $"{Mathf.Floor(distances[i] * 100) / 100}";
            tb.Color = new(0, 0, 0, distances[i] / max);
            tb.gameObject.SetActive(true);
            tb.transform.localScale = new(0.0025f, 0.0025f, 0.0025f);
        }
    }
}
