using UnityEngine;

namespace WeightGeneration
{
    /// <summary>
    /// f(x,y,z) = a r^2 + b r + c - y  ,  with r = sqrt(x^2 + z^2)
    /// </summary>
    class QuadraticCurve3D : WeightGenerator
    {
        public override bool ConstantlyRegenerate => false;
        public float threshold = 0.0f;
        public override float Threshold
        {
            get => threshold;
            set => threshold = value;
        }
        [SerializeField] ComputeShader quadraticCurve3DShader;
        [SerializeField] float a = 1f;
        [SerializeField] float b = 0f;
        [SerializeField] float c = 0f;

        [SerializeField] Vector3 minBounds = new(-1, -1, -1);
        [SerializeField] Vector3 maxBounds = new(1, 1, 1);

        public override void Generate(RenderTexture renderTexture)
        {
            var kernel = quadraticCurve3DShader.FindKernel("QuadraticCurve3D");

            quadraticCurve3DShader.SetTexture(kernel, "_Output", renderTexture);

            quadraticCurve3DShader.SetFloat("_A", a);
            quadraticCurve3DShader.SetFloat("_B", b);
            quadraticCurve3DShader.SetFloat("_C", c);
            quadraticCurve3DShader.SetVector("_MinBounds", minBounds + offset);
            quadraticCurve3DShader.SetVector("_MaxBounds", maxBounds + offset);

            quadraticCurve3DShader.Dispatch(
                kernel,
                Mathf.CeilToInt((float)renderTexture.width / 8),
                Mathf.CeilToInt((float)renderTexture.height / 8),
                Mathf.CeilToInt((float)renderTexture.volumeDepth / 8));
        }
    }
}