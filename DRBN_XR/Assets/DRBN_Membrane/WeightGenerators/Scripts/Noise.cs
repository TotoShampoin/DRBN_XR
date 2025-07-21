using UnityEngine;

namespace WeightGeneration
{
    /// <summary>
    /// Fast Improved 3D Perlin Noise, from https://www.shadertoy.com/view/slB3z3
    /// </summary>
    public class Noise : WeightGenerator
    {
        public override bool ConstantlyRegenerate => false;
        public float threshold = 0.5f;
        public override float Threshold
        {
            get => threshold;
            set => threshold = value;
        }
        [SerializeField] ComputeShader noiseShader;

        [SerializeField] Vector3 minBounds = new(-1, -1, -1);
        [SerializeField] Vector3 maxBounds = new(1, 1, 1);
        public Vector3 noiseOffset = Vector3.zero;

        public override void Generate(RenderTexture renderTexture)
        {
            var kernel = noiseShader.FindKernel("Noise");

            noiseShader.SetTexture(kernel, "_Output", renderTexture);

            noiseShader.SetVector("_Offset", offset);
            noiseShader.SetVector("_NoiseOffset", noiseOffset);
            noiseShader.SetVector("_MinBounds", minBounds);
            noiseShader.SetVector("_MaxBounds", maxBounds);

            noiseShader.Dispatch(
                kernel,
                Mathf.CeilToInt((float)renderTexture.width / 8),
                Mathf.CeilToInt((float)renderTexture.height / 8),
                Mathf.CeilToInt((float)renderTexture.volumeDepth / 8));
        }
    }
}