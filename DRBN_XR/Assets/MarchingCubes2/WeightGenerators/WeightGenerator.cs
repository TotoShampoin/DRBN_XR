using UnityEngine;

public abstract class WeightGenerator : MonoBehaviour
{
    public abstract void Generate(RenderTexture renderTexture);
}
