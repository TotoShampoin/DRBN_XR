using UnityEngine;

class ParticleDensity : WeightGenerator
{
    [SerializeField] ParticleSimulator particleSimulator;
    [SerializeField] ComputeShader particleDensityShader;

    public override void Generate(RenderTexture renderTexture)
    {
        var kernel = particleDensityShader.FindKernel("ParticleDensity");

        particleDensityShader
            .SetBuffer(kernel,
                "_Positions", particleSimulator.positionsBuffer);
        particleDensityShader
            .SetTexture(kernel, "_Output", renderTexture);

        particleDensityShader
            .SetFloat("_DensityRadius", particleSimulator.DensityRadius);
        particleDensityShader
            .SetVector("_MinBounds", particleSimulator.MinBounds);
        particleDensityShader
            .SetVector("_MaxBounds", particleSimulator.MaxBounds);

        particleDensityShader.Dispatch(
            kernel,
            Mathf.CeilToInt((float)renderTexture.width / 8),
            Mathf.CeilToInt((float)renderTexture.height / 8),
            Mathf.CeilToInt((float)renderTexture.volumeDepth / 8));
    }
}
