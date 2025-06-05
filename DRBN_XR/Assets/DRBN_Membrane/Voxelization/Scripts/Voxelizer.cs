using UnityEngine;

namespace Voxelization
{
    /// <summary>
    /// Transforms a 3D mesh into a Signed Distance Function
    /// </summary>
    public class Voxelizer : MonoBehaviour
    {
        public ComputeShader voxelizer;
        [Range(0, 10)] public float multiplier = 1;

        public Bounds voxelBounds = new(Vector3.zero, Vector3.one);

        ComputeBuffer verticesBuffer;
        ComputeBuffer normalsBuffer;
        ComputeBuffer trianglesBuffer;

        static Vector3Int threadGroups = new(8, 8, 8);
        public void Voxelize(Mesh mesh, RenderTexture output, RenderTexture normals = null)
        {
            bool useNormals = normals != null;
            if (useNormals)
            {
                if (output.width != normals.width || output.height != normals.height || output.volumeDepth != normals.volumeDepth)
                    throw new System.ArgumentException("Output and normals RenderTextures must have the same dimensions.");
            }
            else
            {
                normals = new RenderTexture(output.width, output.height, 0, output.format)
                {
                    dimension = output.dimension,
                    volumeDepth = output.volumeDepth,
                    enableRandomWrite = true
                };
                normals.Create();
            }

            if (mesh.vertexCount == 0 || mesh.triangles.Length == 0)
            {
                var clearKernel = voxelizer.FindKernel("Clear");
                voxelizer.SetVector("_OutputSize", new(output.width, output.height, output.volumeDepth));
                voxelizer.SetTexture(clearKernel, "_Output", output);
                voxelizer.SetTexture(clearKernel, "_DebugNormals", normals);
                voxelizer.Dispatch(clearKernel,
                    Mathf.CeilToInt((float)output.width / threadGroups.x),
                    Mathf.CeilToInt((float)output.height / threadGroups.y),
                    Mathf.CeilToInt((float)output.volumeDepth / threadGroups.z)
                );
                return;
            }

            AllocateBuffers(mesh);
            var kernel = voxelizer.FindKernel("Voxelize");

            voxelizer.SetBuffer(kernel, "_Vertices", verticesBuffer);
            voxelizer.SetBuffer(kernel, "_Normals", normalsBuffer);
            voxelizer.SetBuffer(kernel, "_Triangles", trianglesBuffer);
            voxelizer.SetInt("_TriangleCount", mesh.triangles.Length);
            voxelizer.SetVector("_OutputSize", new(output.width, output.height, output.volumeDepth));
            voxelizer.SetTexture(kernel, "_Output", output);
            voxelizer.SetTexture(kernel, "_DebugNormals", normals);
            voxelizer.SetVector("_VoxelMinBound", voxelBounds.min);
            voxelizer.SetVector("_VoxelMaxBound", voxelBounds.max);
            voxelizer.SetVector("_MeshMinBound", -Vector3.one);
            voxelizer.SetVector("_MeshMaxBound", Vector3.one);
            voxelizer.SetFloat("_Multiplier", multiplier);

            voxelizer.Dispatch(kernel,
                Mathf.CeilToInt((float)output.width / threadGroups.x),
                Mathf.CeilToInt((float)output.height / threadGroups.y),
                Mathf.CeilToInt((float)output.volumeDepth / threadGroups.z)
            );
            if (!useNormals)
            {
                normals.Release();
            }
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
