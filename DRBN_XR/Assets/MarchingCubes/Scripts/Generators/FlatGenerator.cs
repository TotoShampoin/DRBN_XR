using UnityEngine;

public class FlatGenerator : Generator
{
    ComputeBuffer _weightsBuffer;
    public ComputeShader FlatShader;

    [SerializeField, Range(0, 1)] float groundLevel = 0.0f;

    public override float[] Generate()
    {
        return GetFlat(GridMetrics.LastLod);
    }

    public float[] GetFlat(int lod)
    {
        CreateBuffers(lod);
        float[] values =
            new float[
                GridMetrics.PointsPerChunk(lod) *
                GridMetrics.PointsPerChunk(lod) *
                GridMetrics.PointsPerChunk(lod)];

        FlatShader.SetBuffer(0, "_Weights", _weightsBuffer);

        FlatShader.SetInt("_ChunkSize", GridMetrics.PointsPerChunk(lod));
        FlatShader.SetInt("_Scale", GridMetrics.Scale);

        FlatShader.SetFloat("_Min", -1.0f);
        FlatShader.SetFloat("_Max", 1.0f);
        FlatShader.SetFloat("_GroundLevel", groundLevel);

        FlatShader.Dispatch(0,
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
