using System.Linq;
using UnityEngine;

public class Gaussian : Smoothen
{
    ComputeBuffer _inBuffer;
    ComputeBuffer _outBuffer;
    public ComputeShader Shader;

    public float sigma = 1.0f;
    public int kernelSize = 2;

    public override float[] Smooth(float[] weights)
    {
        var xOut = Compute(GridMetrics.LastLod, weights, "GaussX");
        var yOut = Compute(GridMetrics.LastLod, xOut, "GaussY");
        var zOut = Compute(GridMetrics.LastLod, yOut, "GaussZ");
        return zOut;
    }

    public float[] Compute(int lod, float[] weights, string kernelName)
    {
        int kernel = Shader.FindKernel(kernelName);

        CreateBuffers(lod);
        float[] values =
            new float[
                GridMetrics.PointsPerChunk(lod) *
                GridMetrics.PointsPerChunk(lod) *
                GridMetrics.PointsPerChunk(lod)];

        _inBuffer.SetData(weights);
        Shader.SetBuffer(kernel, "_Input", _inBuffer);
        Shader.SetBuffer(kernel, "_Output", _outBuffer);

        Shader.SetInt("_ChunkSize", GridMetrics.PointsPerChunk(lod));
        Shader.SetInt("_Scale", GridMetrics.Scale);

        Shader.SetFloat("_Min", -1.0f);
        Shader.SetFloat("_Max", 1.0f);

        Shader.SetFloat("_Sigma", sigma);
        Shader.SetInt("_KernelSize", kernelSize);

        Shader.Dispatch(kernel,
                GridMetrics.ThreadGroups(lod),
                GridMetrics.ThreadGroups(lod),
                GridMetrics.ThreadGroups(lod)
            );

        _outBuffer.GetData(values);

        if (values.All(v => v == 0.0f) && !weights.All(v => v == 0.0f))
        {
            Debug.Log("All values are zero");
        }

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
