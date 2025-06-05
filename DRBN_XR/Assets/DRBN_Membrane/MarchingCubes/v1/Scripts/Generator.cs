using UnityEngine;

namespace MarchingCubing.V1
{
    public abstract class Generator : MonoBehaviour
    {
        public abstract float[] Generate();
    }

    public abstract class Smoothen : MonoBehaviour
    {
        public abstract float[] Smooth(float[] weights);
    }
}