using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class TMCController : MonoBehaviour
{
    public MeshGenerator MeshGenerator;
    public MeshBrush3D Brush;
    public SphereColliderPopulateV2 SphereColliderPopulateV2;

    public InputActionReference PaintAction;
    public InputActionReference EraserAction;

    public Transform PaintAnchor;

    public int GeneratorIndex = 0;
    public List<Generator> Generators;

    public bool RegenerateNextFrame = false;

    void Start()
    {
        MeshGenerator.Recreate(Generators[GeneratorIndex].Generate());
        SphereColliderPopulateV2.ExtractAll(MeshGenerator.GetComponent<MeshFilter>());

        PaintAction.action.performed += _ => Brush.IsPainting = true;
        PaintAction.action.canceled += _ => Brush.IsPainting = false;

        EraserAction.action.performed += _ => Brush.PaintOrErase = false;
        EraserAction.action.canceled += _ => Brush.PaintOrErase = true;
    }

    void FixedUpdate()
    {
        bool isUpdated = false;
        if (RegenerateNextFrame)
        {
            MeshGenerator.Recreate(Generators[GeneratorIndex].Generate());
            RegenerateNextFrame = false;
            isUpdated = true;
        }

        Brush.transform.position = PaintAnchor.position;
        if (Brush.IsPainting)
        {
            MeshGenerator.EditWeights(Brush.transform.position,
                Brush.BrushSize, Brush.PaintOrErase);
            isUpdated = true;
        }

        if (isUpdated)
        {
            SphereColliderPopulateV2.ExtractAll(MeshGenerator.GetComponent<MeshFilter>());
        }
    }

    public void Regenerate()
    {
        RegenerateNextFrame = true;
    }

}
