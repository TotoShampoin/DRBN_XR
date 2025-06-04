using UnityEngine;

namespace WeightGeneration
{
    class Noise : WeightGenerator
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

        public override void Generate(RenderTexture renderTexture)
        {
            var kernel = noiseShader.FindKernel("Noise");

            noiseShader.SetTexture(kernel, "_Output", renderTexture);

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