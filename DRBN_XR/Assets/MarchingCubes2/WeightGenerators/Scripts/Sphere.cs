using UnityEngine;

class Sphere : WeightGenerator
{
    public override bool ConstantlyRegenerate => false;
    public float threshold = 0.0f;
    public override float Threshold
    {
        get => threshold;
        set => threshold = value;
    }
    [SerializeField] ComputeShader sphereShader;
    [SerializeField] float radius = 1f;
    [SerializeField] float n = 2f;

    [SerializeField] Vector3 minBounds = new(-1, -1, -1);
    [SerializeField] Vector3 maxBounds = new(1, 1, 1);

    public override void Generate(RenderTexture renderTexture)
    {
        var kernel = sphereShader.FindKernel("QuadraticCurve3D");

        sphereShader.SetTexture(kernel, "_Output", renderTexture);

        sphereShader.SetFloat("_Radius", radius);
        sphereShader.SetFloat("_N", n);
        sphereShader.SetVector("_MinBounds", minBounds);
        sphereShader.SetVector("_MaxBounds", maxBounds);

        sphereShader.Dispatch(
            kernel,
            Mathf.CeilToInt((float)renderTexture.width / 8),
            Mathf.CeilToInt((float)renderTexture.height / 8),
            Mathf.CeilToInt((float)renderTexture.volumeDepth / 8));
    }
}
