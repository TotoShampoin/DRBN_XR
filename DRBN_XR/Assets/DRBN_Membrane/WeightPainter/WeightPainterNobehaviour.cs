using UnityEngine;

namespace WeightPainting
{
    /// <summary>
    /// Same as the WeightPainter component, but with no automatic behaviour.
    /// </summary>
    public class WeightPainterNobehaviour : MonoBehaviour
    {
        [SerializeField] ComputeShader weightPainterShader;

        /// <summary>
        /// Empties the volume.
        /// </summary>
        /// <param name="renderTexture"></param>
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

        /// <summary>
        /// Applies the weight painting.
        /// </summary>
        public void Paint(
            RenderTexture renderTexture, Vector3 position, Bounds bounds,
            float radius, float weight, ActionMode mode)
        {
            var kernel = weightPainterShader.FindKernel("WeightPainter");
            weightPainterShader.SetTexture(kernel, "_Output", renderTexture);

            weightPainterShader.SetVector("_Position", position);
            weightPainterShader.SetFloat("_Radius", radius);
            weightPainterShader.SetFloat("_Weight", weight);
            weightPainterShader.SetInt("_Mode", (int)mode);
            weightPainterShader.SetVector("_MinBounds", bounds.min);
            weightPainterShader.SetVector("_MaxBounds", bounds.max);

            weightPainterShader.SetFloat("_MinClamp", -1.0f);
            weightPainterShader.SetFloat("_MaxClamp", 1.0f);
            weightPainterShader.SetFloat("_ResetValue", -1.0f);

            weightPainterShader.Dispatch(
                kernel,
                Mathf.CeilToInt((float)renderTexture.width / 8),
                Mathf.CeilToInt((float)renderTexture.height / 8),
                Mathf.CeilToInt((float)renderTexture.volumeDepth / 8));
        }

        public enum ActionMode
        {
            None = 0,
            Add = 1,
            Subtract = -1,
        }
    }
}
