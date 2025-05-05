using UnityEngine;

namespace Assets.Voxelization
{

    public class VoxelizationTestControl : MonoBehaviour
    {
        public RenderTexture weightTexture;
        public RenderTexture sdfTexture;
        public MarchingCubes marchingCubes1;
        public MarchingCubes marchingCubes2;
        public Voxelizer voxelizer;
        public WeightGenerator weightGenerator;
        public WeightPainter weightPainter;
        public float meshExtractionEpsilon = 0.005f;

        Mesh mesh;

        void Start()
        {
            GenerateMarchingCubes();
            weightPainter.enabled = true;
        }

        void Update()
        {
            if (weightPainter.enabled && weightPainter.needsRegenerate)
            {
                RefreshMarchingCubes();
                weightPainter.needsRegenerate = false;
            }
        }

        public void RefreshMarchingCubes()
        {
            mesh = marchingCubes1.GenerateMesh(weightTexture,
                weightGenerator.Threshold);
            marchingCubes1.ApplyMesh(mesh);
            voxelizer.Voxelize(mesh, sdfTexture);
            marchingCubes2.GenerateAndApplyMesh(sdfTexture, 0);
        }

        public void GenerateMarchingCubes()
        {
            weightGenerator.Generate(weightTexture);
            RefreshMarchingCubes();
        }
    }

}
