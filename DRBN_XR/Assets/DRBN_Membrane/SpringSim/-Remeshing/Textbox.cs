using TMPro;
using UnityEngine;

[RequireComponent(typeof(TextMeshPro))]
public class Textbox : MonoBehaviour
{
    public Vector3 Position { get => transform.position; set => transform.position = value; }
    public string Text
    {
        get => GetComponent<TextMeshPro>().text;
        set => GetComponent<TextMeshPro>().text = value;
    }
    public Color Color
    {
        get => GetComponent<TextMeshPro>().color;
        set => GetComponent<TextMeshPro>().color = value;
    }

    // void Update()
    // {
    //     transform.LookAt(Camera.main.transform);
    //     transform.rotation *= Quaternion.Euler(0f, 180f, 0f);
    // }
}
