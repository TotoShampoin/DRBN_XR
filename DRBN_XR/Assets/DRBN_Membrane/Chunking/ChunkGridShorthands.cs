using UnityEngine;

public class ChunkGridShorthands : MonoBehaviour
{
    public ChunkGrid grid;

    public void Regenerate()
    {
        grid.ForEachChunk(grid.RegenerateChunk);
    }
    public void Voxelize()
    {
        grid.ForEachChunk(grid.VoxelizeChunk);
    }
    public void GenSprings()
    {
        grid.ForEachChunk(grid.GenerateSpringsForChunk);
        grid.updateSprings = true;
        grid.renderMode = ChunkGrid.RenderMode.SpringsAsMesh;
    }
}