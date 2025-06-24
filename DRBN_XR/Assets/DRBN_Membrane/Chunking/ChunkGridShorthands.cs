using TMPro;
using UnityEngine;

public class ChunkGridShorthands : MonoBehaviour
{
    public ChunkGrid grid;
    public TMP_Dropdown renderModeUi;

    public int RenderModeAsInt { get => (int)grid.renderMode; set => grid.renderMode = (ChunkGrid.RenderMode)value; }

    public void Update()
    {
        renderModeUi.value = RenderModeAsInt;
    }

    public void Regenerate()
    {
        grid.ForEachPos(grid.RegenerateChunk);
    }
    public void Voxelize()
    {
        grid.ForEachPos(grid.VoxelizeChunk);
    }
    public void GenSprings()
    {
        grid.ForEachPos(grid.GenerateSpringsForChunk);
        grid.updateSprings = true;
        grid.renderMode = ChunkGrid.RenderMode.SpringsAsMesh;
    }
}
