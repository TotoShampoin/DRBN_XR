using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ChunkGridShorthands : MonoBehaviour
{
    public ChunkGrid grid;
    public TMP_Dropdown renderModeUi;
    public Toggle forceUpdateUi;

    public int RenderModeAsInt { get => (int)grid.renderMode; set => grid.renderMode = (ChunkGrid.RenderMode)value; }
    public bool ForceUpdate { get => grid.forceUpdate; set => grid.forceUpdate = value; }

    public void Update()
    {
        renderModeUi.value = RenderModeAsInt;
        forceUpdateUi.isOn = ForceUpdate;
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
        grid.ForEachPos(pos => grid.GenerateSpringsForChunk(pos, true));
        grid.updateSprings = true;
        grid.renderMode = ChunkGrid.RenderMode.SpringsAsMesh;
    }
}
