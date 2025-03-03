using UnityEngine;

public class TMCController : MonoBehaviour
{
    public MeshGenerator MeshGenerator;
    public NoiseGenerator NoiseGenerator;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        MeshGenerator.Recreate(NoiseGenerator.GetNoise(GridMetrics.LastLod));
    }
}
