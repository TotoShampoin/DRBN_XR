using System;
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
    [Header("Shaders")]
    [SerializeField] ComputeShader particleComputeShader;
    [SerializeField] ComputeShader partitioningComputeShader;
    [SerializeField] int particleCount = 4096;
    [SerializeField] int partitionResolution = 8;

    [Header("Parameters")]
    [SerializeField] Vector3 centerBounds = new(0, 0, 0);
    [SerializeField] Vector3 sizeBounds = new(10, 10, 10);
    [SerializeField] Transform attractionCenter;
    [SerializeField] float attractionScale = 1f;
    [SerializeField] float densityRadius = 0.5f;
    [SerializeField] float targetDensity = 6f;
    [SerializeField] float pressureScale = 15f;
    [SerializeField] float nearbyPressureScale = 2f;

    [Header("Debug")]
    [SerializeField] GameObject particlePrefab;
    [SerializeField] float particleSize = 0.1f;
    [SerializeField] bool resetOnNextFrame = false;
    [SerializeField] bool drawParticles = true;

    int currentParticleCount = 0;

    public ComputeBuffer positionsBuffer;
    public ComputeBuffer velocitiesBuffer;
    public ComputeBuffer predictedPositionsBuffer;
    public ComputeBuffer densityBuffer;
    public ComputeBuffer nearbyDensityBuffer;
    public ComputeBuffer particlesIndices;
    public ComputeBuffer subgridIndices;
    public ComputeBuffer subgridStarts;
    public ComputeBuffer subgridCounts;
    private ComputeBuffer argsBuffer;

    public ComputeBuffer testBuffer;

    public Vector3 CenterBounds => centerBounds;
    public Vector3 SizeBounds => sizeBounds;
    public Vector3 MinBounds => centerBounds - sizeBounds / 2;
    public Vector3 MaxBounds => centerBounds + sizeBounds / 2;
    public float DensityRadius => densityRadius;
    public int ParticleCount => currentParticleCount;
    public int PartitionCount =>
        partitionResolution * partitionResolution * partitionResolution;

    private Mesh particleMesh;
    private Material particleMaterial;

    void OnEnable()
    {
        ResetParticles();
        particleMesh = particlePrefab.GetComponent<MeshFilter>().sharedMesh;
        particleMaterial = particlePrefab.GetComponent<MeshRenderer>().material;
    }

    void OnDisable()
    {
        positionsBuffer?.Release();
        velocitiesBuffer?.Release();
        predictedPositionsBuffer?.Release();
        densityBuffer?.Release();
        nearbyDensityBuffer?.Release();

        particlesIndices?.Release();
        subgridIndices?.Release();
        subgridStarts?.Release();
        subgridCounts?.Release();

        testBuffer?.Release();

        argsBuffer?.Release();
    }

    void Update()
    {
        if (resetOnNextFrame)
        {
            ResetParticles();
            resetOnNextFrame = false;
        }
        UpdateParticles();
        PartitionParticles();
        if (drawParticles)
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
                UnityEngine.Random.Range(MinBounds.x, MaxBounds.x),
                UnityEngine.Random.Range(MinBounds.y, MaxBounds.y),
                UnityEngine.Random.Range(MinBounds.z, MaxBounds.z)
            ));
            velocities.Add(new(0, 0, 0));
            densities.Add(0f);
        }
        positionsBuffer = new(particleCount, sizeof(float) * 3);
        velocitiesBuffer = new(particleCount, sizeof(float) * 3);
        predictedPositionsBuffer = new(particleCount, sizeof(float) * 3);
        densityBuffer = new(particleCount, sizeof(float));
        nearbyDensityBuffer = new(particleCount, sizeof(float));

        particlesIndices = new(particleCount, sizeof(uint));
        subgridIndices = new(particleCount, sizeof(uint));
        subgridStarts = new(PartitionCount, sizeof(uint));
        subgridCounts = new(PartitionCount, sizeof(uint));

        testBuffer = new(PartitionCount, sizeof(int) * 3 * 2);

        positionsBuffer.SetData(positions);
        velocitiesBuffer.SetData(velocities);
        predictedPositionsBuffer.SetData(positions);
        densityBuffer.SetData(densities);
        nearbyDensityBuffer.SetData(densities);

        currentParticleCount = particleCount;
    }

    void UpdateParticles()
    {
        int threadsCount = Mathf.CeilToInt((float)currentParticleCount / 256);

        particleComputeShader.SetFloat("DeltaTime", Time.deltaTime);
        particleComputeShader.SetInt("ParticleCount", currentParticleCount);
        particleComputeShader.SetFloat("DensityRadius", densityRadius);
        particleComputeShader.SetFloat("TargetDensity", targetDensity);
        particleComputeShader.SetFloat("PressureScale", pressureScale);
        particleComputeShader.SetFloat("NearbyPressureScale", nearbyPressureScale);
        particleComputeShader.SetVector("AttractionCenter", attractionCenter != null ? attractionCenter.position : Vector3.zero);
        particleComputeShader.SetFloat("AttractionStrength", attractionScale);
        particleComputeShader.SetFloat("PartitionResolution", partitionResolution);

        particleComputeShader.SetVector("CenterBound", centerBounds);
        particleComputeShader.SetVector("SizeBound", sizeBounds);

        var predict = particleComputeShader.FindKernel("PredictPositions");
        particleComputeShader.SetBuffer(predict, "Positions", positionsBuffer);
        particleComputeShader.SetBuffer(predict, "Velocities", velocitiesBuffer);
        particleComputeShader.SetBuffer(predict, "PredictedPositions", predictedPositionsBuffer);
        particleComputeShader.Dispatch(predict, threadsCount, 1, 1);

        var density = particleComputeShader.FindKernel("CalculateDensities");
        particleComputeShader.SetBuffer(density, "Positions", positionsBuffer);
        particleComputeShader.SetBuffer(density, "Velocities", velocitiesBuffer);
        particleComputeShader.SetBuffer(density, "PredictedPositions", predictedPositionsBuffer);
        particleComputeShader.SetBuffer(density, "Densities", densityBuffer);
        particleComputeShader.SetBuffer(density, "NearbyDensities", nearbyDensityBuffer);
        particleComputeShader.SetBuffer(density, "ParticlesIndices", particlesIndices);
        particleComputeShader.SetBuffer(density, "SubgridStarts", subgridStarts);
        particleComputeShader.Dispatch(density, threadsCount, 1, 1);

        var pressure = particleComputeShader.FindKernel("CalculatePressures");
        particleComputeShader.SetBuffer(pressure, "Positions", positionsBuffer);
        particleComputeShader.SetBuffer(pressure, "Velocities", velocitiesBuffer);
        particleComputeShader.SetBuffer(pressure, "PredictedPositions", predictedPositionsBuffer);
        particleComputeShader.SetBuffer(pressure, "Densities", densityBuffer);
        particleComputeShader.SetBuffer(pressure, "NearbyDensities", nearbyDensityBuffer);
        particleComputeShader.SetBuffer(pressure, "ParticlesIndices", particlesIndices);
        particleComputeShader.SetBuffer(pressure, "SubgridStarts", subgridStarts);
        particleComputeShader.Dispatch(pressure, threadsCount, 1, 1);

        var movement = particleComputeShader.FindKernel("MoveParticles");
        particleComputeShader.SetBuffer(movement, "Positions", positionsBuffer);
        particleComputeShader.SetBuffer(movement, "Velocities", velocitiesBuffer);
        particleComputeShader.Dispatch(movement, threadsCount, 1, 1);
    }

    void PartitionParticles()
    {
        var clear = partitioningComputeShader.FindKernel("Clear");
        var fetchSubgrids = partitioningComputeShader.FindKernel("FetchSubgrids");
        var fetchStarts = partitioningComputeShader.FindKernel("FetchStarts");
        var sortSubgridsGlobal = partitioningComputeShader.FindKernel("SortSubgridsGlobal");

        int threadsCountSubgrid = Mathf.CeilToInt((float)PartitionCount / 256);
        int threadsCountParticle = Mathf.CeilToInt((float)ParticleCount / 256);

        var power = Mathf.CeilToInt(Mathf.Log(PartitionCount, 2));
        var powerOf2Size = (uint)1 << power;

        partitioningComputeShader.SetInt("PartitionResolution", partitionResolution);
        partitioningComputeShader.SetVector("CenterBound", centerBounds);
        partitioningComputeShader.SetVector("SizeBound", sizeBounds);

        partitioningComputeShader.SetBuffer(clear, "SubgridCounts", subgridCounts);
        partitioningComputeShader.Dispatch(clear, threadsCountSubgrid, 1, 1);

        partitioningComputeShader.SetBuffer(fetchSubgrids, "Positions", positionsBuffer);
        partitioningComputeShader.SetBuffer(fetchSubgrids, "ParticlesIndices", particlesIndices);
        partitioningComputeShader.SetBuffer(fetchSubgrids, "SubgridIndices", subgridIndices);
        partitioningComputeShader.SetBuffer(fetchSubgrids, "SubgridCounts", subgridCounts);
        partitioningComputeShader.Dispatch(fetchSubgrids, threadsCountParticle, 1, 1);

        partitioningComputeShader.SetBuffer(fetchStarts, "SubgridCounts", subgridCounts);
        partitioningComputeShader.SetBuffer(fetchStarts, "SubgridStarts", subgridStarts);
        partitioningComputeShader.Dispatch(fetchStarts, threadsCountSubgrid, 1, 1);

        partitioningComputeShader.SetBuffer(sortSubgridsGlobal, "ParticlesIndices", particlesIndices);
        partitioningComputeShader.SetBuffer(sortSubgridsGlobal, "SubgridIndices", subgridIndices);
        for (uint k = 2; k <= powerOf2Size; k <<= 1)
        {
            partitioningComputeShader.SetInt("_k", (int)k);
            for (uint j = k >> 1; j > 0; j >>= 1)
            {
                partitioningComputeShader.SetInt("_j", (int)j);
                partitioningComputeShader.Dispatch(sortSubgridsGlobal, threadsCountParticle, 1, 1);
            }
        }
    }

    void DrawParticles()
    {
        if (particleMesh == null || particleMaterial == null) return;

        particleMaterial.SetBuffer("_Positions", positionsBuffer);
        particleMaterial.SetBuffer("_Indices", particlesIndices);
        particleMaterial.SetBuffer("_SubgridIndices", subgridIndices);
        particleMaterial.SetFloat("_ParticleSize", particleSize);
        particleMaterial.SetInt("_ParticleCount", currentParticleCount);
        particleMaterial.SetInt("_PartitionResolution", partitionResolution);
        particleMaterial.SetMatrix("_LocalToWorld", transform.localToWorldMatrix);

        // Set up the arguments buffer for indirect drawing
        argsBuffer ??=
            new(1, sizeof(uint) * 5, ComputeBufferType.IndirectArguments);

        uint[] args = new uint[5] { 0, 0, 0, 0, 0 };
        args[0] = particleMesh.GetIndexCount(0);
        args[1] = (uint)currentParticleCount;
        args[2] = particleMesh.GetIndexStart(0);
        args[3] = particleMesh.GetBaseVertex(0);
        argsBuffer.SetData(args);

        // Draw all particles in a single draw call
        Graphics.DrawMeshInstancedIndirect(
            particleMesh, 0,
            particleMaterial,
            new Bounds(centerBounds, sizeBounds * 2),
            argsBuffer
        );
    }
}
