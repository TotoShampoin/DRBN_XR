using UnityEngine;
using UnityEngine.InputSystem;

public class WeightPainter : MonoBehaviour
{
    [SerializeField] ComputeShader weightPainterShader;
    [SerializeField] RenderTexture renderTexture;
    [SerializeField] Transform marchingCubes;

    [SerializeField] Transform brushTransform;
    [SerializeField] InputActionReference paintTrigger;
    [SerializeField] InputActionReference eraseTrigger;

    [SerializeField] float radius = 1.0f;
    [SerializeField] float weight = 1.0f;
    ActionMode mode = ActionMode.None;

    bool isUsingBrush = false;
    bool isErasing = false;
    public bool needsRegenerate = false;
    public bool clearAtStart = true;

    void Start()
    {
        paintTrigger.action.performed += _ => isUsingBrush = true;
        paintTrigger.action.canceled += _ => isUsingBrush = false;
        eraseTrigger.action.performed += _ => isErasing = true;
        eraseTrigger.action.canceled += _ => isErasing = false;

        if (clearAtStart)
        {
            Clear(renderTexture);
            needsRegenerate = true;
        }
    }

    void Update()
    {
        if (isUsingBrush)
        {
            Vector3 position = marchingCubes.transform
                .InverseTransformPoint(brushTransform.position);
            mode = isErasing
                ? ActionMode.Subtract
                : ActionMode.Add;
            Paint(renderTexture, position);
            needsRegenerate = true;
        }
        else
        {
            mode = ActionMode.None;
        }
    }

    public void Clear(RenderTexture renderTexture)
    {
        var kernel = weightPainterShader.FindKernel("ClearWeightMap");
        weightPainterShader.SetTexture(kernel, "_Output", renderTexture);
        weightPainterShader.Dispatch(
            kernel,
            Mathf.CeilToInt((float)renderTexture.width / 8),
            Mathf.CeilToInt((float)renderTexture.height / 8),
            Mathf.CeilToInt((float)renderTexture.volumeDepth / 8));
    }

    public void Paint(RenderTexture renderTexture, Vector3 position)
    {
        var kernel = weightPainterShader.FindKernel("WeightPainter");
        weightPainterShader.SetTexture(kernel, "_Output", renderTexture);

        weightPainterShader.SetVector("_Position", position);
        weightPainterShader.SetFloat("_Radius", radius);
        weightPainterShader.SetFloat("_Weight", weight);
        weightPainterShader.SetInt("_Mode", (int)mode);
        weightPainterShader.SetVector("_MinBounds", new(-0.5f, -0.5f, -0.5f));
        weightPainterShader.SetVector("_MaxBounds", new(0.5f, 0.5f, 0.5f));

        weightPainterShader.SetFloat("_MinClamp", -1.0f);
        weightPainterShader.SetFloat("_MaxClamp", 1.0f);
        weightPainterShader.SetFloat("_ResetValue", -1.0f);

        weightPainterShader.Dispatch(
            kernel,
            Mathf.CeilToInt((float)renderTexture.width / 8),
            Mathf.CeilToInt((float)renderTexture.height / 8),
            Mathf.CeilToInt((float)renderTexture.volumeDepth / 8));
    }

    public void SetRadius(float radius)
    {
        this.radius = radius;
    }

    public enum ActionMode
    {
        None = 0,
        Add = 1,
        Subtract = -1,
    }

}
