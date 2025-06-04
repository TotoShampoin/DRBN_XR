using UnityEngine;

namespace WeightGeneration
{
    public abstract class WeightGenerator : MonoBehaviour
    {
        public abstract bool ConstantlyRegenerate { get; }
        public abstract float Threshold { get; set; }
        public abstract void Generate(RenderTexture renderTexture);
    }
}