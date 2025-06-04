using UnityEngine;

namespace MarchingCubeSystem.V2
{
    public abstract class WeightGenerator : MonoBehaviour
    {
        public abstract bool ConstantlyRegenerate { get; }
        public abstract float Threshold { get; set; }
        public abstract void Generate(RenderTexture renderTexture);
    }
}