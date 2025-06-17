using System;
using UnityEngine;

namespace WeightGeneration
{
    /// <summary>
    /// Interface class for filling a volume (a 1-channel 3D texture) using a function in R^3 -> R
    /// </summary>
    public abstract class WeightGenerator : MonoBehaviour
    {
        /// <summary>
        /// Whether to call Generate on each update or not
        /// </summary>
        [Obsolete("This does nothing, and you should handle it yourself")]
        public abstract bool ConstantlyRegenerate { get; }

        /// <summary>
        /// The value at which the MarchingCube should place its vertices. This value is purely informative, and doesn't do anything.
        /// </summary>
        public abstract float Threshold { get; set; }

        /// <summary>
        /// Fills the volume with the function tied to the class
        /// </summary>
        /// <param name="renderTexture"></param>
        public abstract void Generate(RenderTexture renderTexture);

        [NonSerialized] public Vector3 offset = Vector3.zero;
    }
}