using System.Collections.Generic;
using UnityEngine;

namespace Particles3D
{
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
        [SerializeField] Bounds bounds = new(new(0, 0, 0), new(10, 10, 10));
        [SerializeField] Transform attractionCenter;
        [SerializeField] float attractionScale = 1f;
        [SerializeField] float densityRadius = 0.5f;
        [SerializeField] float targetDensity = 6f;
        [SerializeField] float pressureScale = 15f;
        [SerializeField] float nearbyPressureScale = 2f;
        [SerializeField] float viscosityRadius = 0.5f;
        [SerializeField] float viscosityStrength = 3f;

        [Header("Debug")]
        [SerializeField] bool resetOnNextFrame = false;
        readonly bool predictPositions = true;
        readonly bool calculateDensities = true;
        readonly bool calculatePressures = true;
        readonly bool calculateViscosity = true;
        readonly bool moveParticles = true;


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

        public ComputeBuffer testBuffer;

        public Bounds Bounds => bounds;
        public Vector3 MinBounds => bounds.center - bounds.size / 2;
        public Vector3 MaxBounds => bounds.center + bounds.size / 2;
        public float DensityRadius => densityRadius;
        public int ParticleCount => currentParticleCount;
        public int PartitionResolution => partitionResolution;
        public int PartitionCount =>
            partitionResolution * partitionResolution * partitionResolution;

        void OnEnable()
        {
            ResetParticles();
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

            positionsBuffer = null;
            velocitiesBuffer = null;
            predictedPositionsBuffer = null;
            densityBuffer = null;
            nearbyDensityBuffer = null;
            particlesIndices = null;
            subgridIndices = null;
            subgridStarts = null;
            subgridCounts = null;
            testBuffer = null;
        }

        void Update()
        {
            if (resetOnNextFrame)
            {
                ResetParticles();
                resetOnNextFrame = false;
            }
            UpdateParticles();
            // PartitionParticles();
        }

        public void ResetParticles()
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
            particleComputeShader.SetFloat("ViscosityRadius", viscosityRadius);
            particleComputeShader.SetFloat("ViscosityStrength", viscosityStrength);

            particleComputeShader.SetVector("CenterBound", bounds.center);
            particleComputeShader.SetVector("SizeBound", bounds.size);

            if (predictPositions)
            {
                var predict = particleComputeShader.FindKernel("PredictPositions");
                particleComputeShader.SetBuffer(predict, "Positions", positionsBuffer);
                particleComputeShader.SetBuffer(predict, "Velocities", velocitiesBuffer);
                particleComputeShader.SetBuffer(predict, "PredictedPositions", predictedPositionsBuffer);
                particleComputeShader.Dispatch(predict, threadsCount, 1, 1);
            }

            if (calculateDensities)
            {
                var density = particleComputeShader.FindKernel("CalculateDensities");
                particleComputeShader.SetBuffer(density, "Positions", positionsBuffer);
                particleComputeShader.SetBuffer(density, "Velocities", velocitiesBuffer);
                particleComputeShader.SetBuffer(density, "PredictedPositions", predictedPositionsBuffer);
                particleComputeShader.SetBuffer(density, "Densities", densityBuffer);
                particleComputeShader.SetBuffer(density, "NearbyDensities", nearbyDensityBuffer);
                particleComputeShader.SetBuffer(density, "ParticlesIndices", particlesIndices);
                particleComputeShader.SetBuffer(density, "SubgridStarts", subgridStarts);
                particleComputeShader.Dispatch(density, threadsCount, 1, 1);
            }

            if (calculatePressures)
            {
                var pressure = particleComputeShader.FindKernel("CalculatePressures");
                particleComputeShader.SetBuffer(pressure, "Positions", positionsBuffer);
                particleComputeShader.SetBuffer(pressure, "Velocities", velocitiesBuffer);
                particleComputeShader.SetBuffer(pressure, "PredictedPositions", predictedPositionsBuffer);
                particleComputeShader.SetBuffer(pressure, "Densities", densityBuffer);
                particleComputeShader.SetBuffer(pressure, "NearbyDensities", nearbyDensityBuffer);
                particleComputeShader.SetBuffer(pressure, "ParticlesIndices", particlesIndices);
                particleComputeShader.SetBuffer(pressure, "SubgridStarts", subgridStarts);
                particleComputeShader.Dispatch(pressure, threadsCount, 1, 1);
            }

            if (calculateViscosity)
            {
                var viscosity = particleComputeShader.FindKernel("CalculateViscosity");
                particleComputeShader.SetBuffer(viscosity, "Positions", positionsBuffer);
                particleComputeShader.SetBuffer(viscosity, "Velocities", velocitiesBuffer);
                particleComputeShader.SetBuffer(viscosity, "PredictedPositions", predictedPositionsBuffer);
                particleComputeShader.SetBuffer(viscosity, "Densities", densityBuffer);
                particleComputeShader.SetBuffer(viscosity, "NearbyDensities", nearbyDensityBuffer);
                particleComputeShader.SetBuffer(viscosity, "ParticlesIndices", particlesIndices);
                particleComputeShader.SetBuffer(viscosity, "SubgridStarts", subgridStarts);
                particleComputeShader.Dispatch(viscosity, threadsCount, 1, 1);
            }

            if (moveParticles)
            {
                var movement = particleComputeShader.FindKernel("MoveParticles");
                particleComputeShader.SetBuffer(movement, "Positions", positionsBuffer);
                particleComputeShader.SetBuffer(movement, "Velocities", velocitiesBuffer);
                particleComputeShader.Dispatch(movement, threadsCount, 1, 1);
            }
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
            partitioningComputeShader.SetVector("CenterBound", bounds.center);
            partitioningComputeShader.SetVector("SizeBound", bounds.size);

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
    }

}