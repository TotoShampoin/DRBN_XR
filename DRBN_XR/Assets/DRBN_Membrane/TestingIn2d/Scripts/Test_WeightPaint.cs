using UnityEngine;
using UnityEngine.InputSystem;
using MarchingCubeSystem.V2;

public class Test_WeightPaint : MonoBehaviour
{
    public WeightPainter weightPainter;
    public Transform input;
    public Material textureRenderer;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        Vector2 mousePos = Mouse.current.position.ReadValue();
        float aspect = (float)Screen.width / Screen.height;
        float x = (mousePos.x / Screen.width) * 2f - 1f;
        float y = (mousePos.y / Screen.height) * 2f - 1f;
        x *= aspect;
        Vector3 normalizedPos = new Vector3(x, y, 0.0f);
        input.position = normalizedPos;
        textureRenderer.SetFloat("_Threshold", 0);
    }
}
