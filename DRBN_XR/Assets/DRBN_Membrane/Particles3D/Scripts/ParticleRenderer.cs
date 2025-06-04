using UnityEditor;
using UnityEngine;

namespace Particles3D
{
    [RequireComponent(typeof(ParticleSimulator))]
    public class ParticleRenderer : MonoBehaviour
    {
        [SerializeField] Mesh mesh;
        [SerializeField] Material material;
        [SerializeField] float particleSize = 0.1f;

        ParticleSimulator simulator;
        ComputeBuffer argsBuffer;
        readonly uint[] args = new uint[5] { 0, 0, 0, 0, 0 };
        int particleCountTrack = -1;

        void Start()
        {
            Initialize();
        }
        void OnEnable()
        {
            Initialize();
        }
        void OnDisable()
        {
            argsBuffer?.Release();
            argsBuffer = null;
        }

        void Initialize()
        {
            simulator = GetComponent<ParticleSimulator>();
            argsBuffer ??= new(1, sizeof(uint) * 5, ComputeBufferType.IndirectArguments);
            args[0] = mesh.GetIndexCount(0);
            args[1] = (uint)simulator.ParticleCount;
            args[2] = mesh.GetIndexStart(0);
            args[3] = mesh.GetBaseVertex(0);
            argsBuffer.SetData(args);
            particleCountTrack = simulator.ParticleCount;
        }

        void Update()
        {
            if (particleCountTrack != simulator.ParticleCount)
                Initialize();

            DrawParticles();
        }

        void OnDrawGizmos()
        {
            if (!EditorApplication.isPlaying)
            {
                simulator = GetComponent<ParticleSimulator>();
            }
            Gizmos.color = Color.white;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(
                simulator.Bounds.center,
                simulator.Bounds.size
            );
        }

        void DrawParticles()
        {
            if (mesh == null || material == null) return;

            material.SetBuffer("_Positions", simulator.positionsBuffer);
            material.SetBuffer("_Velocities", simulator.velocitiesBuffer);
            material.SetBuffer("_Indices", simulator.particlesIndices);
            material.SetBuffer("_SubgridIndices", simulator.subgridIndices);
            material.SetFloat("_ParticleSize", particleSize);
            material.SetInt("_ParticleCount", simulator.ParticleCount);
            material.SetInt("_PartitionResolution", simulator.PartitionResolution);
            material.SetMatrix("_LocalToWorld", transform.localToWorldMatrix);

            // Draw all particles in a single draw call
            Graphics.DrawMeshInstancedIndirect(
                mesh, 0,
                material,
                simulator.Bounds,
                argsBuffer
            );
        }
    }
}