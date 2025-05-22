using UnityEngine;

namespace Assets.SpringSim.V2
{

    public class SpringControllerWithMeshGen : MonoBehaviour
    {
        public SpringSimulator simulator;
        public RenderTexture renderTexture;
        public MarchingCubes marchingCubes;
        public WeightGenerator weightGenerator;
        public WeightPainter weightPainter;
        public float meshExtractionEpsilon = 0.005f;
        public float meshExtractionDistance = 0.2f;
        public MeshFromSprings meshFromSprings;
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
        public float MarchingCubeResolution // if this is not a float, Unity's slider won't accept it -_-
        {
            get => marchingCubes.resolution;
            set => marchingCubes.resolution = (int)value;
        }
        public float VoxeliseInterval // if this is not a float, Unity's slider won't accept it -_-
        {
            get => voxeliseInterval;
            set => voxeliseInterval = value;
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
                newMesh = MeshFromSprings.CleanupMesh(newMesh, oldMesh, meshExtractionDistance);
                simulator.UseMesh(newMesh, meshExtractionEpsilon);
                if (simulator.HasMarks) ConstantRebuild = false;
                weightPainter.enabled = false;
                voxeliseTimer = 0f;
            }
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
    [UnityEditor.CustomEditor(typeof(SpringControllerWithMeshGen))]
    public class SpringSimWithMeshGenControlEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            SpringControllerWithMeshGen control = (SpringControllerWithMeshGen)target;

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
            }
        }
    }
#endif
}
