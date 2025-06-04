using UnityEngine;
using WeightPainting;
using WeightGeneration;

namespace MarchingCubeSystem.V2
{
    public class Controller : MonoBehaviour
    {
        [SerializeField] WeightGenerator weightGenerator;
        [SerializeField] RenderTexture renderTexture;
        [SerializeField] MarchingCubes marchingCubes;
        [SerializeField] WeightPainter weightPainter;

        public bool regenerate = false;
        public bool constantlyRegenerate = false;

        void OnEnable()
        {
            Regenerate();
        }

        void Update()
        {
            if (weightPainter.needsRegenerate)
            {
                marchingCubes.GenerateAndApplyMesh(renderTexture,
                    weightGenerator.Threshold);
                weightPainter.needsRegenerate = false;
            }
            if (regenerate || constantlyRegenerate)
            {
                Regenerate();
                regenerate = false;
            }
        }

        void Regenerate()
        {
            if (!renderTexture) return;
            weightGenerator.Generate(renderTexture);
            marchingCubes.GenerateAndApplyMesh(renderTexture,
                    weightGenerator.Threshold);
        }
    }
}