using Unity.Profiling;
using UnityEngine;

public class DistanceOfVolumes : MonoBehaviour
{
    [SerializeField] ComputeShader shader;
    [Range(1, 100), SerializeField] float attenuation = 1f;
    [Range(0, 300), SerializeField] float distanceSensitivity = 3f;

    static readonly ProfilerMarker distanceMarker = new("Membrane.DistanceOfVolumes");
    static readonly ProfilerMarker computeMarker = new("Membrane.DistanceOfVolumes.Distance");
    static readonly ProfilerMarker valueMarker = new("Membrane.DistanceOfVolumes.MaxValue");

    public float Distance(RenderTexture a, RenderTexture b, RenderTexture output = null)
    {
        using (distanceMarker.Auto())
        {
            var result = new RenderTexture(a) { enableRandomWrite = true };
            result.Create();

            ComputeDistances(a, b, result);

            if (output) Graphics.CopyTexture(result, output);

            float score = ComputeMaxValue(result);

            result.Release();
            return score;
        }
    }

    void ComputeDistances(RenderTexture a, RenderTexture b, RenderTexture result)
    {
        using (computeMarker.Auto())
        {
            int kernel = shader.FindKernel("Distance");

            shader.SetTexture(kernel, "TextureA", a);
            shader.SetTexture(kernel, "TextureB", b);
            shader.SetTexture(kernel, "Result", result);
            shader.SetFloat("A", attenuation);
            shader.SetFloat("D", distanceSensitivity);

            shader.GetKernelThreadGroupSizes(kernel, out uint tgx, out uint tgy, out uint tgz);

            int groupsX = Mathf.CeilToInt((float)a.width / tgx);
            int groupsY = Mathf.CeilToInt((float)a.height / tgy);
            int groupsZ = Mathf.CeilToInt((float)a.volumeDepth / tgz);

            shader.Dispatch(kernel, groupsX, groupsY, groupsZ);
        }
    }

    float ComputeMaxValue(RenderTexture result)
    {
        using (valueMarker.Auto())
        {
            int kernel = shader.FindKernel("MaxReduce");
            shader.GetKernelThreadGroupSizes(kernel, out uint tgx, out uint tgy, out uint tgz);

            ComputeBuffer maxBuffer = new(1, sizeof(float));

            shader.SetBuffer(kernel, "OutputBuffer", maxBuffer);
            shader.SetTexture(kernel, "Result", result);

            int dim = Mathf.RoundToInt(
                Mathf.Max(new float[3] {
                result.width, result.height, result.volumeDepth
                })
            );
            int N = 1;
            while (N < dim)
                N <<= 1;

            N >>= 1;
            for (; N >= 1; N >>= 1)
            {
                shader.SetInt("N", N);
                shader.Dispatch(kernel,
                    Mathf.CeilToInt((float)N / tgx),
                    Mathf.CeilToInt((float)N / tgy),
                    Mathf.CeilToInt((float)N / tgz)
                );
            }

            float[] output = new float[1];
            maxBuffer.GetData(output);  // called 375, 165.85ms
            maxBuffer.Release();
            return output[0];
        }
    }
}
