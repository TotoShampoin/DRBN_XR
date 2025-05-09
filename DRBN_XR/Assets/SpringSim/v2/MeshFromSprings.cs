using Assets.Voxelization;
using UnityEngine;

namespace Assets.SpringSim.V2
{

    public class MeshFromSprings : MonoBehaviour
    {
        MeshFilter meshFilter;
        Voxelizer voxelizer;
        MarchingCubes marchingCubes;

        public RenderTexture renderTexture;

        public int Resolution { get => marchingCubes.resolution; set => marchingCubes.resolution = value; }

        void Start()
        {
            meshFilter = GetComponent<MeshFilter>();
            voxelizer = GetComponent<Voxelizer>();
            marchingCubes = GetComponent<MarchingCubes>();
        }

        public void SetMesh(Mesh mesh)
        {
            // meshFilter.mesh = mesh;
            var vertices = mesh.vertices;
            for (int i = 0; i < vertices.Length; i++)
            {
                vertices[i] *= 0.5f;
            }
            mesh.vertices = vertices;
            mesh.RecalculateBounds();
            voxelizer.Voxelize(mesh, renderTexture);
            meshFilter.mesh = marchingCubes.GenerateMesh(renderTexture, 0);
        }
    }

}
