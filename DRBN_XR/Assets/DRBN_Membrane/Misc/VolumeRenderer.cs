using UnityEngine;

public class VolumeRenderer : MonoBehaviour
{
    public RenderTexture texture;
    public RenderTexture normals;
    public Mesh voxelMesh;
    public Material voxelMaterial;
    public Bounds bounds = new(Vector3.zero, Vector3.one);

    ComputeBuffer argsBuffer;
    readonly uint[] args = new uint[5] { 0, 0, 0, 0, 0 };
    int meshCountTrack = -1;

    void OnEnable()
    {
        voxelMaterial = new(voxelMaterial);
        Initialize();
    }
    void OnDisable() { argsBuffer?.Release(); argsBuffer = null; }

    void Update()
    {
        if (texture == null)
            return;
        if (meshCountTrack != texture.width * texture.height * texture.volumeDepth)
            Initialize();

        DrawVolume();
    }

    void Initialize()
    {
        argsBuffer ??= new(1, sizeof(uint) * 5, ComputeBufferType.IndirectArguments);
        args[0] = voxelMesh.GetIndexCount(0);
        args[1] = (uint)(texture.width * texture.height * texture.volumeDepth);
        args[2] = voxelMesh.GetIndexStart(0);
        args[3] = voxelMesh.GetBaseVertex(0);
        argsBuffer.SetData(args);
        meshCountTrack = texture.width * texture.height * texture.volumeDepth;

        Debug.Log($"Using {meshCountTrack} voxels");
    }
    void DrawVolume()
    {
        if (voxelMesh == null || voxelMaterial == null) return;

        voxelMaterial.SetFloat("_Width", texture.width);
        voxelMaterial.SetFloat("_Height", texture.height);
        voxelMaterial.SetFloat("_Depth", texture.volumeDepth);
        voxelMaterial.SetTexture("_Texture", texture);
        voxelMaterial.SetTexture("_Normals", normals);
        voxelMaterial.SetVector("_BoundsMin", bounds.min);
        voxelMaterial.SetVector("_BoundsMax", bounds.max);
        voxelMaterial.SetMatrix("_LocalToWorld", transform.localToWorldMatrix);

        Graphics.DrawMeshInstancedIndirect(
            voxelMesh, 0,
            voxelMaterial,
            bounds,
            argsBuffer
        );
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawWireCube(
            bounds.center,
            bounds.size
        );
    }
#endif
}
