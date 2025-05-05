using UnityEngine;

namespace Assets.Voxelization
{

    public class VoxelizationTestControl : MonoBehaviour
    {
        public RenderTexture weightTexture;
        public RenderTexture sdfTexture;
        public MarchingCubes marchingCubes;
        public WeightGenerator weightGenerator;
        public WeightPainter weightPainter;
        public float meshExtractionEpsilon = 0.005f;

        public float MeshExtractionEpsilon
        {
            get => meshExtractionEpsilon;
            set => meshExtractionEpsilon = value;
        }
        public float MarchingCubeResolution
        {
            get => marchingCubes.resolution;
            set => marchingCubes.resolution = (int)value;
        }

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
            marchingCubes.GenerateAndApplyMesh(weightTexture,
                    weightGenerator.Threshold);
        }

        public void GenerateMarchingCubes()
        {
            weightGenerator.Generate(weightTexture);
            RefreshMarchingCubes();
        }
    }

}
