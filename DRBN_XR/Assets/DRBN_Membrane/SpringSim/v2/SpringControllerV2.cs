using UnityEngine;
using MarchingCubing.V2;
using WeightPainting;
using WeightGeneration;

namespace SpringSim.V2
{

    public class SpringController : MonoBehaviour
    {
        public SpringSimulator simulator;
        public RenderTexture renderTexture;
        public MarchingCubes marchingCubes;
        public WeightGenerator weightGenerator;
        public WeightPainter weightPainter;
        public float meshExtractionEpsilon = 0.005f;

        public float MeshExtractionEpsilon
        {
            get => meshExtractionEpsilon;
            set => meshExtractionEpsilon = value;
        }
        public float MarchingCubeResolution // if this is not a float, Unity's slider won't accept it -_-
        {
            get => marchingCubes.resolution;
            set => marchingCubes.resolution = (int)value;
        }

        void Start()
        {
            GenerateMarchingCubes();
            weightPainter.enabled = true;
        }

        void Update()
        {
            if (weightPainter.enabled && weightPainter.needsRegenerate)
            {
                RefreshMarchingCubes();
                weightPainter.needsRegenerate = false;
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
        }

        public void ReturnToMarchingCubes()
        {
            simulator.Clear();
            weightPainter.enabled = true;
            RefreshMarchingCubes();
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
    public class SpringSimControlEditor : UnityEditor.Editor
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
            }
        }
    }
#endif
}
