using System.Collections.Generic;
using UnityEngine;
using MarchingCubing.V2;
using WeightGeneration;
using WeightPainting;

public class ChunkGrid : MonoBehaviour
{
    public WeightGenerator weightGenerator;
    public WeightPainterNobehaviour weightPainter;
    public MarchingCubesRef marchingCubes;
    public Material material;
    public RenderTexture baseVolume;

    public int marchResolution = 8;
    float offsetFactor;

    public struct ChunkData
    {
        public Mesh mesh;
        public RenderTexture volume;
    };
    readonly Dictionary<Vector3Int, ChunkData> grid = new();

    void Start()
    {
        int N = 1;
        for (int x = -N; x <= N; x++)
            for (int y = -N; y <= N; y++)
                for (int z = -N; z <= N; z++)
                    CreateChunk(new Vector3Int(x, y, z));
    }

    void Update()
    {
        offsetFactor = 2f - (float)4 / marchResolution;
        marchingCubes.resolution = marchResolution;
        marchingCubes.bounds.size = new Vector3(offsetFactor, offsetFactor, offsetFactor);
        RenderParams rp = new(material);
        foreach (var (pos, chunk) in grid)
        {
            Vector3 posf = pos;
            RegenerateChunk(pos);
            MarchChunk(pos);
            Graphics.RenderMesh(
                rp, chunk.mesh, 0,
                transform.localToWorldMatrix * Matrix4x4.TRS(posf * offsetFactor, Quaternion.identity, Vector3.one)
            );
        }
    }

    public void CreateChunk(Vector3Int at)
    {
        grid.TryAdd(at, new ChunkData()
        {
            mesh = new Mesh(),
            volume = new RenderTexture(baseVolume),
        });
    }

    public void RegenerateChunk(Vector3Int at)
    {
        Vector3 atf = at;
        weightGenerator.offset = atf * offsetFactor;
        weightGenerator.Generate(grid[at].volume);
    }
    public void MarchChunk(Vector3Int at)
    {
        marchingCubes.GenerateMesh(grid[at].volume, 0, grid[at].mesh);
    }

    // void OnPreRender()
    // {
    //     RenderParams rp = new(material);
    //     foreach (var (pos, chunk) in grid)
    //     {
    //         Graphics.RenderMesh(
    //             rp, chunk.mesh, 0,
    //             transform.localToWorldMatrix * Matrix4x4.TRS(pos, Quaternion.identity, Vector3.one)
    //         );
    //     }
    // }

    void OnDrawGizmosSelected()
    {
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawWireCube(Vector3.zero, Vector3.one);
    }
}
