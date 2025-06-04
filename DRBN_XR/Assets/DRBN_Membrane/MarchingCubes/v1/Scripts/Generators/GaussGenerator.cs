using UnityEngine;

public class GaussGenerator : Generator
{
    ComputeBuffer _weightsBuffer;
    public ComputeShader Shader;

    [SerializeField, Range(0, 1)] float groundLevel = 0.0f;
    [SerializeField, Range(0, 4)] float sigma = 1.0f;
    [SerializeField, Range(0, 4)] float amplitude = 1.0f;

    public override float[] Generate()
    {
        return GetGauss(GridMetrics.LastLod);
    }

    public float[] GetGauss(int lod)
    {
        CreateBuffers(lod);
        float[] values =
            new float[
                GridMetrics.PointsPerChunk(lod) *
                GridMetrics.PointsPerChunk(lod) *
                GridMetrics.PointsPerChunk(lod)];

        Shader.SetBuffer(0, "_Weights", _weightsBuffer);

        Shader.SetInt("_ChunkSize", GridMetrics.PointsPerChunk(lod));
        Shader.SetInt("_Scale", GridMetrics.Scale);

        Shader.SetFloat("_Min", -1.0f);
        Shader.SetFloat("_Max", 1.0f);
        Shader.SetFloat("_GroundLevel", groundLevel);

        Shader.SetFloat("_Sigma", sigma);
        Shader.SetFloat("_Amplitude", amplitude);

        Shader.Dispatch(0,
                GridMetrics.ThreadGroups(lod),
                GridMetrics.ThreadGroups(lod),
                GridMetrics.ThreadGroups(lod)
            );

        _weightsBuffer.GetData(values);

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
}
