using UnityEngine;
using MarchingCubeSystem.V2;

public class Test_Generator : MonoBehaviour
{
    public WeightGenerator generator;
    public RenderTexture texture;
    public Material textureRenderer;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        generator.Generate(texture);
        textureRenderer.SetFloat("_Threshold", generator.Threshold);
    }
}
