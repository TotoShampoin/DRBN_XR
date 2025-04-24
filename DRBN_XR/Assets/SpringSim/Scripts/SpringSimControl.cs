using TMPro;
using UnityEngine;

namespace Assets.SpringSim
{

    public class SpringSimControl : MonoBehaviour
    {
        [SerializeField] RenderTexture renderTexture;
        [SerializeField] SpringSim springSim;
        [SerializeField] MarchingCubes marchingCubes;
        [SerializeField] WeightGenerator weightGenerator;
        [SerializeField] WeightPainter weightPainter;

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
            springSim.Clear();
            springSim.ExtractMesh(mesh);
            weightPainter.enabled = false;
        }

        public void ReturnToMarchingCubes()
        {
            springSim.Clear();
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
    [UnityEditor.CustomEditor(typeof(SpringSimControl))]
    public class SpringSimControlEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            SpringSimControl control = (SpringSimControl)target;

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
