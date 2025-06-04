using UnityEngine;
using WeightGeneration;

public class Test_Generator : MonoBehaviour
{
    public WeightGenerator generator;
    public RenderTexture texture;
    public Material textureRenderer;

    void Update()
    {
        generator.Generate(texture);
        textureRenderer.SetFloat("_Threshold", generator.Threshold);
    }
}
