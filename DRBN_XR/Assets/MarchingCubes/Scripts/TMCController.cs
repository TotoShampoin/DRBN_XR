using UnityEngine;
using UnityEngine.InputSystem;

public class TMCController : MonoBehaviour
{
    public MeshGenerator MeshGenerator;
    public NoiseGenerator NoiseGenerator;
    public MeshBrush3D Brush;

    public InputActionReference PaintAction;
    public InputActionReference EraserAction;

    public Transform PaintAnchor;

    void Start()
    {
        MeshGenerator.Recreate(NoiseGenerator.GetNoise(GridMetrics.LastLod));

        PaintAction.action.performed += _ => Brush.IsPainting = true;
        PaintAction.action.canceled += _ => Brush.IsPainting = false;

        EraserAction.action.performed += _ => Brush.PaintOrErase = false;
        EraserAction.action.canceled += _ => Brush.PaintOrErase = true;
    }

    void FixedUpdate()
    {
        Brush.transform.position = PaintAnchor.position;
        if (Brush.IsPainting)
        {
            MeshGenerator.EditWeights(Brush.transform.position,
                Brush.BrushSize, Brush.PaintOrErase);
        }
    }
}
