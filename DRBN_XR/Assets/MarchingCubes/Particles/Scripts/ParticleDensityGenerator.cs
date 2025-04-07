using UnityEngine;

public class ParticleDensityGenerator : Generator
{
    [SerializeField] ParticleSimulator particleSimulator;
    [SerializeField] ComputeShader Shader;

    ComputeBuffer _weightsBuffer;

    public override float[] Generate()
    {
        return GetParticleDensity(GridMetrics.LastLod);
    }

    public float[] GetParticleDensity(int lod)
    {
        CreateBuffers(lod);
        float[] values =
            new float[
                GridMetrics.PointsPerChunk(lod) *
                GridMetrics.PointsPerChunk(lod) *
                GridMetrics.PointsPerChunk(lod)];

        Shader.SetBuffer(0, "_Weights", _weightsBuffer);
        Shader.SetBuffer(0, "_Positions", particleSimulator.positionsBuffer);

        Shader.SetInt("_ChunkSize", GridMetrics.PointsPerChunk(lod));
        Shader.SetInt("_Scale", GridMetrics.Scale);

        Shader.SetVector("_MinBounds", particleSimulator.MinBounds);
        Shader.SetVector("_MaxBounds", particleSimulator.MaxBounds);
        Shader.SetFloat("_Radius", particleSimulator.DensityRadius);
        Shader.SetInt("_ParticleCount",
            particleSimulator.positionsBuffer.count);

        Shader.Dispatch(0,
                GridMetrics.ThreadGroups(lod),
                GridMetrics.ThreadGroups(lod),
                GridMetrics.ThreadGroups(lod)
            );

        ReleaseBuffers();
        return values;
    }

    void CreateBuffers(int lod)
    {
        _weightsBuffer = new ComputeBuffer(
            GridMetrics.PointsPerChunk(lod) *
            GridMetrics.PointsPerChunk(lod) *
            GridMetrics.PointsPerChunk(lod),
            sizeof(float)
        );
    }

    void ReleaseBuffers()
    {
        _weightsBuffer.Release();
    }
};