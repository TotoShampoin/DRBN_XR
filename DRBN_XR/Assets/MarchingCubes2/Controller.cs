using UnityEngine;

public class Controller : MonoBehaviour
{
    [SerializeField] WeightGenerator weightGenerator;

    MarchingCubes marchingCubes;

    void OnEnable()
    {
        marchingCubes = GetComponent<MarchingCubes>();
    }

    void Update()
    {
        if (!marchingCubes.renderTexture) return;
        weightGenerator.Generate(marchingCubes.renderTexture);
    }
}
