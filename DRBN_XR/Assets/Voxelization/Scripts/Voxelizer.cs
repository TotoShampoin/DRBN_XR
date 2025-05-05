using UnityEngine;

namespace Assets.Voxelization
{

    public class Voxelizer : MonoBehaviour
    {
        public ComputeShader voxelizer;
        public Vector3Int threadGroups = new(8, 8, 8);
        [Range(0, 10)] public float multiplier = 1;

        ComputeBuffer verticesBuffer;
        ComputeBuffer normalsBuffer;
        ComputeBuffer trianglesBuffer;

        public void Voxelize(Mesh mesh, RenderTexture output)
        {
            AllocateBuffers(mesh);
            var kernel = voxelizer.FindKernel("Voxelize");

            voxelizer.SetBuffer(kernel, "_Vertices", verticesBuffer);
            voxelizer.SetBuffer(kernel, "_Normals", normalsBuffer);
            voxelizer.SetBuffer(kernel, "_Triangles", trianglesBuffer);
            voxelizer.SetInt("_TriangleCount", mesh.triangles.Length);
            voxelizer.SetTexture(kernel, "_Output", output);
            voxelizer.SetVector("_OutputSize", new(output.width, output.height, output.volumeDepth));
            voxelizer.SetVector("_MinBound", new(-1, -1, -1));
            voxelizer.SetVector("_MaxBound", new(1, 1, 1));
            voxelizer.SetFloat("_Multiplier", multiplier);

            voxelizer.Dispatch(kernel,
                Mathf.CeilToInt(output.width / threadGroups.x),
                Mathf.CeilToInt(output.height / threadGroups.y),
                Mathf.CeilToInt(output.volumeDepth / threadGroups.z)
            );
        }

        void OnDisable()
        {
            verticesBuffer?.Release();
            normalsBuffer?.Release();
            trianglesBuffer?.Release();

            verticesBuffer = null;
            normalsBuffer = null;
            trianglesBuffer = null;
        }

        void AllocateBuffers(Mesh mesh)
        {
            verticesBuffer?.Release();
            normalsBuffer?.Release();
            trianglesBuffer?.Release();
            verticesBuffer = new(mesh.vertexCount * 3, sizeof(float) * 3);
            normalsBuffer = new(mesh.vertexCount * 3, sizeof(float) * 3);
            trianglesBuffer = new(mesh.triangles.Length / 3, sizeof(int) * 3);

            verticesBuffer.SetData(mesh.vertices);
            normalsBuffer.SetData(mesh.normals);
            trianglesBuffer.SetData(mesh.triangles);
        }
    }

}
