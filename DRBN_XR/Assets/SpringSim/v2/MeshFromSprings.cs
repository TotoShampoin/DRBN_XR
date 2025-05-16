using System.Threading.Tasks;
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
            meshFilter.mesh = FetchMesh(mesh);
        }
        public Mesh FetchMesh(Mesh mesh)
        {
            var vertices = mesh.vertices;
            Parallel.For(0, vertices.Length, (i) => vertices[i] *= 0.5f);
            mesh.vertices = vertices;
            mesh.RecalculateBounds();
            voxelizer.Voxelize(mesh, renderTexture);
            return marchingCubes.GenerateMesh(renderTexture, 0);
        }
    }

}
