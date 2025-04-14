using UnityEngine;

namespace Assets.SpringSim.v1
{

    public class SpringSimControl : MonoBehaviour
    {
        [SerializeField] RenderTexture renderTexture;
        [SerializeField] SpringSim springSim;
        [SerializeField] MarchingCubes marchingCubes;
        [SerializeField] WeightGenerator weightGenerator;

        void Start()
        {
            GenerateSprings();
        }

        public void GenerateSprings()
        {
            weightGenerator.Generate(renderTexture);
            var mesh = marchingCubes.GenerateMesh(renderTexture, 0.0f);
            springSim.ExtractMesh(mesh);
            springSim.EntryPoint = mesh;
        }
    }

}
