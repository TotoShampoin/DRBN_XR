using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class TMCController : MonoBehaviour
{
    public MeshGenerator MeshGenerator;
    public MeshBrush3D Brush;

    public InputActionReference PaintAction;
    public InputActionReference EraserAction;

    public Transform PaintAnchor;

    public int GeneratorIndex = 0;
    public List<Generator> Generators;

    public bool Regenerate = false;

    void Start()
    {
        MeshGenerator.Recreate(Generators[GeneratorIndex].Generate());

        PaintAction.action.performed += _ => Brush.IsPainting = true;
        PaintAction.action.canceled += _ => Brush.IsPainting = false;

        EraserAction.action.performed += _ => Brush.PaintOrErase = false;
        EraserAction.action.canceled += _ => Brush.PaintOrErase = true;
    }

    void FixedUpdate()
    {
        if (Regenerate)
        {
            MeshGenerator.Recreate(Generators[GeneratorIndex].Generate());
            Regenerate = false;
        }

        Brush.transform.position = PaintAnchor.position;
        if (Brush.IsPainting)
        {
            MeshGenerator.EditWeights(Brush.transform.position,
                Brush.BrushSize, Brush.PaintOrErase);
        }
    }
}
