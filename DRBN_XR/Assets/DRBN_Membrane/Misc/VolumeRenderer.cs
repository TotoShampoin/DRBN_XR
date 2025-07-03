using UnityEngine;

public class VolumeRenderer : MonoBehaviour
{
    public RenderTexture texture;
    public RenderTexture normals;
    public Mesh voxelMesh;
    public Material voxelMaterial;
    public Bounds bounds = new(Vector3.zero, Vector3.one);
    public bool renderOnUpdate = true;

    GraphicsBuffer argsBuffer;
    readonly uint[] args = new uint[5] { 0, 0, 0, 0, 0 };
    int meshCountTrack = -1;

    MaterialPropertyBlock materialProperties;

    public void DrawVolume(RenderTexture texture, Bounds bounds, Matrix4x4 localToWorld)
    {
        if (voxelMesh == null || voxelMaterial == null)
            return;

        if (meshCountTrack != texture.width * texture.height * texture.volumeDepth)
            Initialize();

        materialProperties ??= new MaterialPropertyBlock();
        materialProperties.SetFloat("_Width", texture.width);
        materialProperties.SetFloat("_Height", texture.height);
        materialProperties.SetFloat("_Depth", texture.volumeDepth);
        materialProperties.SetTexture("_Texture", texture);
        materialProperties.SetTexture("_Normals", normals);
        materialProperties.SetVector("_BoundsMin", bounds.min);
        materialProperties.SetVector("_BoundsMax", bounds.max);
        materialProperties.SetMatrix("_LocalToWorld", localToWorld);

        var renderParams = new RenderParams(voxelMaterial)
        {
            worldBounds = bounds,
            shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off,
            receiveShadows = false,
            lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off,
            matProps = materialProperties
        };

        Graphics.RenderMeshIndirect(renderParams, voxelMesh, argsBuffer);
    }

    void Initialize()
    {
        argsBuffer?.Release();
        argsBuffer = new GraphicsBuffer(GraphicsBuffer.Target.IndirectArguments, 1, sizeof(uint) * 5);

        args[0] = voxelMesh.GetIndexCount(0);
        args[1] = (uint)(texture.width * texture.height * texture.volumeDepth);
        args[2] = voxelMesh.GetIndexStart(0);
        args[3] = voxelMesh.GetBaseVertex(0);
        args[4] = 0;

        argsBuffer.SetData(args);
        meshCountTrack = texture.width * texture.height * texture.volumeDepth;

        Debug.Log($"Using {meshCountTrack} voxels");
    }

    void OnEnable()
    {
        voxelMaterial = new(voxelMaterial);
        Initialize();
    }
    void OnDisable()
    {
        argsBuffer?.Release();
        argsBuffer = null;
    }

    void Update()
    {
        if (texture == null)
            return;
        if (renderOnUpdate)
            DrawVolume(texture, bounds, transform.localToWorldMatrix);
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
