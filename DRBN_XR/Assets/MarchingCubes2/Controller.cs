using UnityEngine;
using UnityEngine.InputSystem;

public class Controller : MonoBehaviour
{
    [SerializeField] WeightGenerator weightGenerator;
    [SerializeField] RenderTexture renderTexture;
    MarchingCubes marchingCubes;
    WeightPainter weightPainter;

    [SerializeField] Transform brushTransform;
    [SerializeField] InputActionReference paintTrigger;
    [SerializeField] InputActionReference eraseTrigger;

    bool isUsingBrush = false;
    bool isErasing = false;

    public bool regenerate = false;
    public bool constantlyRegenerate = false;

    void OnEnable()
    {
        marchingCubes = GetComponent<MarchingCubes>();
        paintTrigger.action.performed += _ => isUsingBrush = true;
        paintTrigger.action.canceled += _ => isUsingBrush = false;
        eraseTrigger.action.performed += _ => isErasing = true;
        eraseTrigger.action.canceled += _ => isErasing = false;

        weightPainter = GetComponent<WeightPainter>();
        weightPainter.Clear(renderTexture);

        Regenerate();
    }

    void Update()
    {
        if (isUsingBrush)
        {
            weightPainter.paint.position = marchingCubes.transform
                .InverseTransformPoint(brushTransform.position);
            weightPainter.paint.mode = isErasing
                ? WeightPainter.ActionMode.Subtract
                : WeightPainter.ActionMode.Add;
            weightPainter.Paint(renderTexture);
            marchingCubes.GenerateAndApplyMesh(renderTexture);
        }
        else
        {
            weightPainter.paint.mode = WeightPainter.ActionMode.None;
        }

        if (regenerate || constantlyRegenerate)
        {
            Regenerate();
            regenerate = false;
        }
    }

    void Regenerate()
    {
        if (!renderTexture) return;
        weightGenerator.Generate(renderTexture);
        marchingCubes.GenerateAndApplyMesh(renderTexture);
    }
}
