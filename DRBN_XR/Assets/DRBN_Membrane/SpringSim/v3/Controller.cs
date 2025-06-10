using UnityEngine;
using MarchingCubing.V2;
using WeightPainting;
using WeightGeneration;
using System;

namespace SpringSim.V3
{
    public class SpringController : MonoBehaviour
    {
        public SpringSimulator simulator;
        public RenderTexture renderTexture;
        public MarchingCubes marchingCubes;
        public WeightGenerator weightGenerator;
        public WeightPainter weightPainter;
        public float meshExtractionEpsilon = 0.005f;
        public float meshExtractionDistance = 0.2f;
        public float meshExtractionVelocityInfluence = 0.5f;
        public V2.MeshFromSprings meshFromSprings;
        public float voxeliseRate = 15f;
        public bool constantRebuild = false;

        float voxeliseInterval;
        float voxeliseTimer = 0f;

        public float MeshExtractionEpsilon
        {
            get => meshExtractionEpsilon;
            set => meshExtractionEpsilon = value;
        }
        public float MeshExtractionDistance
        {
            get => meshExtractionDistance;
            set => meshExtractionDistance = value;
        }
        public float MeshExtractionVelocityInfluence
        {
            get => meshExtractionVelocityInfluence;
            set => meshExtractionVelocityInfluence = value;
        }
        public float MarchingCubeResolution // if this is not a float, Unity's slider won't accept it -_-
        {
            get => marchingCubes.resolution;
            set => marchingCubes.resolution = (int)value;
        }
        public float VoxeliseRate
        {
            get => 1f / voxeliseInterval;
            set => voxeliseInterval = 1f / value;
        }
        public bool ConstantRebuild
        {
            get => constantRebuild;
            set => constantRebuild = value;
        }

        void Start()
        {
            GenerateMarchingCubes();
            weightPainter.enabled = true;
            voxeliseInterval = 1f / voxeliseRate;
        }

        void Update()
        {
            if (weightPainter.enabled && weightPainter.needsRegenerate)
            {
                RefreshMarchingCubes();
                weightPainter.needsRegenerate = false;
            }
            Voxelize();

            if (constantRebuild && simulator.HasMasses && (voxeliseTimer += Time.deltaTime) >= voxeliseInterval)
            {
                GenerateSpringsWithVoxelizer();
                voxeliseTimer = 0f;
            }
        }

        public void RefreshMarchingCubes()
        {
            marchingCubes.GenerateAndApplyMesh(renderTexture,
                    weightGenerator.Threshold);
        }

        public void GenerateMarchingCubes()
        {
            weightGenerator.Generate(renderTexture);
            RefreshMarchingCubes();
        }

        public void GenerateSprings()
        {
            marchingCubes.ClearMesh();
            var mesh = marchingCubes.GenerateMesh(renderTexture,
                weightGenerator.Threshold);
            simulator.UseMesh(mesh, meshExtractionEpsilon);
            weightPainter.enabled = false;
            voxeliseTimer = 0f;
        }

        public void ReturnToMarchingCubes()
        {
            simulator.Clear();
            weightPainter.enabled = true;
            RefreshMarchingCubes();
        }

        public void Voxelize()
        {
            if (simulator.HasMasses)
            {
                meshFromSprings.Resolution = marchingCubes.resolution;
                meshFromSprings.SetMesh(simulator.ToMesh());
            }
        }

        public void GenerateSpringsWithVoxelizer()
        {
            if (simulator.HasMasses)
            {
                meshFromSprings.Resolution = marchingCubes.resolution;
                var oldMesh = simulator.ToMesh();
                var newMesh = meshFromSprings.FetchMesh(oldMesh);
                newMesh = V2.MeshFromSprings.CleanupMesh(newMesh, oldMesh, meshExtractionDistance);
                // simulator.UseMesh(newMesh, meshExtractionEpsilon);
                simulator.UseMeshRetainVelocities(newMesh, meshExtractionVelocityInfluence, meshExtractionEpsilon);
                weightPainter.enabled = false;
                voxeliseTimer = 0f;
            }
        }

        public void SaveMesh()
        {
            Mesh mesh;
            if (simulator.HasMasses)
            {
                mesh = simulator.ToMesh();
            }
            else
            {
                marchingCubes.ClearMesh();
                mesh = marchingCubes.GenerateMesh(renderTexture,
                    weightGenerator.Threshold);
            }
            var datetime = DateTime.Now.ToString().Replace("/", "-").Replace(":", "-").Replace(" ", "_");
            MeshLoader.SaveMesh(mesh, $"Assets/DRBN_Membrane/_Save/{datetime}.asset");
        }

        public void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
            Application.Quit();
        }
    }

#if UNITY_EDITOR
    [UnityEditor.CustomEditor(typeof(SpringController))]
    public class SpringSimWithMeshGenControlEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            SpringController control = (SpringController)target;

            DrawDefaultInspector();

            UnityEditor.EditorGUILayout.Space();

            if (Application.isPlaying)
            {
                if (GUILayout.Button("Generate Springs"))
                {
                    control.GenerateSprings();
                }
                if (GUILayout.Button("Return to Marching cubes"))
                {
                    control.ReturnToMarchingCubes();
                }
                if (GUILayout.Button("Reset Marching cubes"))
                {
                    control.GenerateMarchingCubes();
                }
                if (GUILayout.Button("External force test"))
                {
                    control.simulator.ApplyForce(Vector3.down * 500f, Vector3.up * 0.01f, 0.5f);
                }
            }
        }
    }
#endif
}
