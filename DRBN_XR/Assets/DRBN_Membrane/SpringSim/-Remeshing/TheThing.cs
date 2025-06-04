using SpringSim.V2;
using UnityEditor;
using UnityEngine;

public class TheThing : MonoBehaviour
{
    public Mesh test0;
    public Mesh test1;
    public Mesh test2;
    Mesh test01;
    Mesh test02;
    Mesh test12;
    Mesh test012;
    Mesh test021;
    Mesh test122;

    public Material material;
    public Textbox textbox;

    [Range(0, 1)] public float tolerance = 0.2f;

    void Awake()
    {
        Remesh();
        var text0 = Instantiate(textbox, new(3, 1, 3), Quaternion.identity, transform);
        text0.Text = "test0";
        text0.gameObject.SetActive(true);
        var text1 = Instantiate(textbox, new(3, 1, 0), Quaternion.identity, transform);
        text1.Text = "test1";
        text1.gameObject.SetActive(true);
        var text2 = Instantiate(textbox, new(3, 1, -3), Quaternion.identity, transform);
        text2.Text = "test2";
        text2.gameObject.SetActive(true);
        var text01 = Instantiate(textbox, new(0, 1, 0), Quaternion.identity, transform);
        text01.Text = "test01";
        text01.gameObject.SetActive(true);
        var text02 = Instantiate(textbox, new(0, 1, 3), Quaternion.identity, transform);
        text02.Text = "test02";
        text02.gameObject.SetActive(true);
        var text12 = Instantiate(textbox, new(-3, 1, 3), Quaternion.identity, transform);
        text12.Text = "test12";
        text12.gameObject.SetActive(true);
        var text012 = Instantiate(textbox, new(-6, 1, 3), Quaternion.identity, transform);
        text012.Text = "test012";
        text012.gameObject.SetActive(true);
        var text021 = Instantiate(textbox, new(-6, 1, 0), Quaternion.identity, transform);
        text021.Text = "test021";
        text021.gameObject.SetActive(true);
        var text122 = Instantiate(textbox, new(-6, 1, -3), Quaternion.identity, transform);
        text122.Text = "test122";
        text122.gameObject.SetActive(true);
    }

    void Update()
    {
        Graphics.DrawMesh(test0, Matrix4x4.Translate(new(3, 0, 3)), material, 0);
        Graphics.DrawMesh(test1, Matrix4x4.Translate(new(3, 0, 0)), material, 0);
        Graphics.DrawMesh(test2, Matrix4x4.Translate(new(3, 0, -3)), material, 0);
        Graphics.DrawMesh(test01, Matrix4x4.Translate(new(0, 0, 0)), material, 0);
        Graphics.DrawMesh(test02, Matrix4x4.Translate(new(0, 0, 3)), material, 0);
        Graphics.DrawMesh(test12, Matrix4x4.Translate(new(-3, 0, 3)), material, 0);
        Graphics.DrawMesh(test012, Matrix4x4.Translate(new(-6, 0, 3)), material, 0);
        Graphics.DrawMesh(test021, Matrix4x4.Translate(new(-6, 0, 0)), material, 0);
        Graphics.DrawMesh(test122, Matrix4x4.Translate(new(-6, 0, -3)), material, 0);
    }

    public void Remesh()
    {
        test01 = MeshFromSprings.CleanupMesh(test1, test0, tolerance);
        test02 = MeshFromSprings.CleanupMesh(test2, test0, tolerance);
        test12 = MeshFromSprings.CleanupMesh(test2, test1, tolerance);
        test012 = MeshFromSprings.CleanupMesh(test2, test01, tolerance);
        test021 = MeshFromSprings.CleanupMesh(test1, test02, tolerance);
        test122 = MeshFromSprings.CleanupMesh(test2, test12, tolerance);
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(TheThing))]
public class TheThingEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        TheThing theThing = (TheThing)target;
        if (GUILayout.Button("Remesh"))
        {
            theThing.Remesh();
        }
    }
}
#endif
