using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

public struct Particle
{
    public Vector3 position;
    public Vector3 velocity;

    public static int Size => sizeof(float) * 6;
}

public class ParticleSimulator : MonoBehaviour
{
    [SerializeField] GameObject particlePrefab;
    [SerializeField] int particleCount = 1000;
    [SerializeField] ComputeShader particleComputeShader;

    [SerializeField] float densityRadius = 3f;
    [SerializeField] float targetDensity = 1f;
    [SerializeField] float pressureScale = 1f;
    [SerializeField] float nearbyPressureScale = 1f;

    [SerializeField] Vector3 centerBounds = new(0, 0, 0);
    [SerializeField] Vector3 sizeBounds = new(10, 10, 10);

    [SerializeField] float particleSize = 0.1f;

    [SerializeField] bool resetOnNextFrame = false;

    public ComputeBuffer positionsBuffer;
    public ComputeBuffer velocitiesBuffer;
    public ComputeBuffer predictedPositionsBuffer;
    public ComputeBuffer densityBuffer;
    public ComputeBuffer nearbyDensityBuffer;

    public Vector3 CenterBounds => centerBounds;
    public Vector3 SizeBounds => sizeBounds;
    public Vector3 MinBounds => centerBounds - sizeBounds / 2;
    public Vector3 MaxBounds => centerBounds + sizeBounds / 2;
    public float DensityRadius => densityRadius;

    void Start()
    {
        ResetParticles();
    }

    void Update()
    {
        if (resetOnNextFrame)
        {
            ResetParticles();
            resetOnNextFrame = false;
        }
        UpdateParticles();
        DrawParticles();
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.white;
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawWireCube(
            centerBounds,
            sizeBounds
        );
    }

    void ResetParticles()
    {
        List<Vector3> positions = new(particleCount);
        List<Vector3> velocities = new(particleCount);
        List<float> densities = new(particleCount);

        for (int i = 0; i < particleCount; i++)
        {
            positions.Add(new(
                Random.Range(MinBounds.x, MaxBounds.x),
                Random.Range(MinBounds.y, MaxBounds.y),
                Random.Range(MinBounds.z, MaxBounds.z)
            ));
            velocities.Add(new(0, 0, 0));
            densities.Add(0f);
        }
        positionsBuffer = new ComputeBuffer(particleCount, Particle.Size);
        velocitiesBuffer = new ComputeBuffer(particleCount, Particle.Size);
        densityBuffer = new ComputeBuffer(particleCount, sizeof(float));
        nearbyDensityBuffer = new ComputeBuffer(particleCount, sizeof(float) * 2);
        predictedPositionsBuffer = new ComputeBuffer(particleCount, Particle.Size);
        positionsBuffer.SetData(positions);
        velocitiesBuffer.SetData(velocities);
        predictedPositionsBuffer.SetData(positions);
        densityBuffer.SetData(densities);
        nearbyDensityBuffer.SetData(densities);
    }

    void UpdateParticles()
    {
        var movement = particleComputeShader.FindKernel("MoveParticles");
        var density = particleComputeShader.FindKernel("CalculateDensities");
        var pressure = particleComputeShader.FindKernel("CalculatePressures");
        var predict = particleComputeShader.FindKernel("PredictPositions");

        particleComputeShader.SetFloat("DeltaTime", Time.deltaTime);
        particleComputeShader.SetInt("ParticleCount", positionsBuffer.count);
        particleComputeShader.SetFloat("DensityRadius", densityRadius);
        particleComputeShader.SetFloat("TargetDensity", targetDensity);
        particleComputeShader.SetFloat("PressureScale", pressureScale);
        particleComputeShader.SetFloat("NearbyPressureScale", nearbyPressureScale);
        particleComputeShader.SetVector("CenterBound", centerBounds);
        particleComputeShader.SetVector("SizeBound", sizeBounds);

        particleComputeShader.SetBuffer(predict, "Positions", positionsBuffer);
        particleComputeShader.SetBuffer(predict, "Velocities", velocitiesBuffer);
        particleComputeShader.SetBuffer(predict, "PredictedPositions", predictedPositionsBuffer);
        particleComputeShader.Dispatch(predict, positionsBuffer.count / 64, 1, 1);

        particleComputeShader.SetBuffer(density, "Positions", positionsBuffer);
        particleComputeShader.SetBuffer(density, "Velocities", velocitiesBuffer);
        particleComputeShader.SetBuffer(density, "PredictedPositions", predictedPositionsBuffer);
        particleComputeShader.SetBuffer(density, "Densities", densityBuffer);
        particleComputeShader.SetBuffer(density, "NearbyDensities", nearbyDensityBuffer);
        particleComputeShader.Dispatch(density, positionsBuffer.count / 64, 1, 1);

        particleComputeShader.SetBuffer(pressure, "Positions", positionsBuffer);
        particleComputeShader.SetBuffer(pressure, "Velocities", velocitiesBuffer);
        particleComputeShader.SetBuffer(pressure, "PredictedPositions", predictedPositionsBuffer);
        particleComputeShader.SetBuffer(pressure, "Densities", densityBuffer);
        particleComputeShader.SetBuffer(pressure, "NearbyDensities", nearbyDensityBuffer);
        particleComputeShader.Dispatch(pressure, positionsBuffer.count / 64, 1, 1);

        particleComputeShader.SetBuffer(movement, "Positions", positionsBuffer);
        particleComputeShader.SetBuffer(movement, "Velocities", velocitiesBuffer);
        particleComputeShader.Dispatch(movement, positionsBuffer.count / 64, 1, 1);
    }

    void DrawParticles()
    {
        Vector3[] particles = new Vector3[positionsBuffer.count];
        positionsBuffer.GetData(particles);
        for (int p = 0; p < particles.Length; p += 1023)
        {
            int count = Mathf.Min(1023, particles.Length - p);
            Graphics.DrawMeshInstanced(
                particlePrefab.GetComponent<MeshFilter>().sharedMesh, 0,
                particlePrefab.GetComponent<MeshRenderer>().sharedMaterial,
                particles
                    .Skip(p).Take(count)
                    .Select(position =>
                        transform.localToWorldMatrix *
                        Matrix4x4.TRS(
                            position,
                            Quaternion.Inverse(transform.rotation),
                            Vector3.one * particleSize))
                    .ToArray(),
                count
            );
        }
    }
}
