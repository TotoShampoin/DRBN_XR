using UnityEngine;

[ExecuteInEditMode]
public class ParticleDensityFunction : MonoBehaviour
{
    // [SerializeField] Vector3 minBounds = new(-10, -10, -10);
    // [SerializeField] Vector3 maxBounds = new(10, 10, 10);
    [SerializeField, Range(0, 1)] float slice = 0.5f;
    // [SerializeField, Range(0, 20)] float radius = 0.5f;

    [SerializeField] ParticleSimulator particleSimulator;
    [SerializeField] ComputeShader pdfSliceShader;
    [SerializeField] RenderTexture pdfSliceTexture;

    [SerializeField] RenderTexture particleDensityBuffer;

    Transform sliceObject;

    void Start()
    {
        sliceObject = transform.GetChild(0);
    }

    void Update()
    {
        var minBounds = particleSimulator.MinBounds;
        var maxBounds = particleSimulator.MaxBounds;

        var position = Mathf.Lerp(minBounds.y, maxBounds.y, slice);
        sliceObject.localPosition = new Vector3(0, position, 0);
        sliceObject.localScale = new Vector3(
            maxBounds.x - minBounds.x,
            maxBounds.z - minBounds.z,
            1f
        );
        sliceObject.localRotation = Quaternion.LookRotation(Vector3.down);
        if (!Application.isPlaying) return;

        RenderDensity();
        sliceObject.GetComponent<MeshRenderer>()
            .material.SetFloat("_Slice", slice);
    }

    void RenderDensity()
    {
        var minBounds = particleSimulator.MinBounds;
        var maxBounds = particleSimulator.MaxBounds;
        var radius = particleSimulator.DensityRadius;

        var resolution = new Vector3Int(
            particleDensityBuffer.width,
            particleDensityBuffer.height,
            particleDensityBuffer.volumeDepth
        );

        pdfSliceShader.SetBuffer(0, "Positions",
            particleSimulator.positionsBuffer);
        pdfSliceShader.SetTexture(0, "PDF", particleDensityBuffer);

        pdfSliceShader.SetVector("MinBound", minBounds);
        pdfSliceShader.SetVector("MaxBound", maxBounds);
        pdfSliceShader.SetInt("ParticleCount",
            particleSimulator.positionsBuffer.count);

        pdfSliceShader.SetFloat("Radius", radius);

        pdfSliceShader.Dispatch(0,
            resolution.x / 4, resolution.y / 4, resolution.z / 4);
    }
}
