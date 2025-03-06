using UnityEngine;

public class Blur : Smoothen
{
    ComputeBuffer _inBuffer;
    ComputeBuffer _outBuffer;
    public ComputeShader Shader;

    public override float[] Smooth(float[] weights)
    {
        return Compute(GridMetrics.LastLod, weights);
    }

    public float[] Compute(int lod, float[] weights)
    {
        CreateBuffers(lod);
        float[] values =
            new float[
                GridMetrics.PointsPerChunk(lod) *
                GridMetrics.PointsPerChunk(lod) *
                GridMetrics.PointsPerChunk(lod)];

        _inBuffer.SetData(weights);
        Shader.SetBuffer(0, "_Input", _inBuffer);
        Shader.SetBuffer(0, "_Output", _outBuffer);

        Shader.SetInt("_ChunkSize", GridMetrics.PointsPerChunk(lod));
        Shader.SetInt("_Scale", GridMetrics.Scale);

        Shader.SetFloat("_Min", -1.0f);
        Shader.SetFloat("_Max", 1.0f);

        Shader.Dispatch(0,
                GridMetrics.ThreadGroups(lod),
                GridMetrics.ThreadGroups(lod),
                GridMetrics.ThreadGroups(lod)
            );

        _outBuffer.GetData(values);

        ReleaseBuffers();
        return values;
    }

    void CreateBuffers(int lod)
    {
        _inBuffer = new ComputeBuffer(
            GridMetrics.PointsPerChunk(lod) *
            GridMetrics.PointsPerChunk(lod) *
            GridMetrics.PointsPerChunk(lod),
            sizeof(float)
        );
        _outBuffer = new ComputeBuffer(
            GridMetrics.PointsPerChunk(lod) *
            GridMetrics.PointsPerChunk(lod) *
            GridMetrics.PointsPerChunk(lod),
            sizeof(float)
        );
    }

    void ReleaseBuffers()
    {
        _inBuffer.Release();
        _outBuffer.Release();
    }
}
