using UnityEngine;

public class WeightPainter : MonoBehaviour
{
    [SerializeField] ComputeShader weightPainterShader;

    public enum ActionMode
    {
        None = 0,
        Add = 1,
        Subtract = -1,
    }
    [System.Serializable]
    public struct PaintParameters
    {
        public Vector3 position;
        public float radius;
        public float weight;
        public ActionMode mode;
    };

    public PaintParameters paint = new()
    {
        position = new Vector3(0, 0, 0),
        radius = 1.0f,
        weight = 1.0f,
        mode = ActionMode.None,
    };

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

    public void Paint(RenderTexture renderTexture)
    {
        var kernel = weightPainterShader.FindKernel("WeightPainter");
        weightPainterShader.SetTexture(kernel, "_Output", renderTexture);

        weightPainterShader.SetVector("_Position", paint.position);
        weightPainterShader.SetFloat("_Radius", paint.radius);
        weightPainterShader.SetFloat("_Weight", paint.weight);
        weightPainterShader.SetInt("_Mode", (int)paint.mode);
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
}
