using UnityEngine;

public abstract class Generator : MonoBehaviour
{
    public abstract float[] Generate();
}

public abstract class Smoothen : MonoBehaviour
{
    public abstract float[] Smooth(float[] weights);
}
