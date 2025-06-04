using System.Collections.Generic;
using UnityEngine;
using MarchingCubeSystem.V2;
using WeightPainting;
using WeightGeneration;

public class ImpalaMeshController2 : MonoBehaviour
{
    [SerializeField] MarchingCubes marchingCubes;
    [SerializeField] SphereColliderPopulateV3 populate;

    [SerializeField] List<WeightGenerator> generators;
    [SerializeField] RenderTexture renderTexture;
    [SerializeField] WeightPainter weightPainter;

    public bool usePerVertex = true;

    int currentGeneratorIndex = 0;
    string meshName = "GeneratedMesh";

    void OnEnable()
    {
        Regenerate();
    }

    void FixedUpdate()
    {
        if (weightPainter.needsRegenerate)
        {
            ApplyMesh();
            weightPainter.needsRegenerate = false;
        }
        if (generators[currentGeneratorIndex].ConstantlyRegenerate)
            Regenerate();
    }

    void MakeSpheres()
    {
        populate.ExtractAndPopulate(
            marchingCubes.GetComponent<MeshFilter>(),
            marchingCubes.transform, usePerVertex);
    }

    public void Regenerate()
    {
        if (!renderTexture) return;
        generators[currentGeneratorIndex].Generate(renderTexture);
        ApplyMesh();
    }
    public void ApplyMesh()
    {
        if (!renderTexture) return;
        marchingCubes.GenerateAndApplyMesh(renderTexture,
            generators[currentGeneratorIndex].Threshold);
        MakeSpheres();
    }

    public void SelectGenerator(int index)
    {
        if (index < 0 || index >= generators.Count) return;
        currentGeneratorIndex = index;
        Regenerate();
    }
    public void SelectPopulateType(int index)
    {
        if (index < 0 || index >= 2) return;
        usePerVertex = index == 0;
        Regenerate();
    }
    public void SetMeshName(string name)
    {
        meshName = name;
    }
    public void SaveCube()
    {
        MeshLoader.SaveMesh(marchingCubes.GetComponent<MeshFilter>().mesh,
            $"Assets/DRBN_STEAMVR/Resources/{meshName}.asset");
    }
}
