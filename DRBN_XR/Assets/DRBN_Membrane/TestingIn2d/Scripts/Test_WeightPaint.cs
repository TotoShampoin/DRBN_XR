using UnityEngine;
using UnityEngine.InputSystem;
using WeightPainting;

public class Test_WeightPaint : MonoBehaviour
{
    public WeightPainter weightPainter;
    public Transform input;
    public Material textureRenderer;

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
