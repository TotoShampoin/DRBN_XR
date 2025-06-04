using UnityEngine;


namespace Particles3D
{
    public class MarchingCubesParticlesControl : MonoBehaviour
    {
        [SerializeField] WeightGenerator weightGenerator;
        [SerializeField] RenderTexture renderTexture;
        [SerializeField] MarchingCubesRef marchingCubes;

        void OnEnable()
        {
            Regenerate();
        }

        void Update()
        {
            if (!renderTexture) return;
            weightGenerator.Generate(renderTexture);
            marchingCubes
                .GenerateAndApplyMesh(renderTexture, weightGenerator.Threshold);
        }

        void Regenerate()
        {
        }
    }
}