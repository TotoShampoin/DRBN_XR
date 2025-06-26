using TMPro;
using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class ChunkGridShorthands : MonoBehaviour
{
    public ChunkGrid grid;
    public TMP_Dropdown renderModeUi;
    public Toggle forceUpdateUi;
    public Slider cycleRateUi;

    public int RenderModeAsInt { get => (int)grid.renderMode; set => grid.renderMode = (ChunkGrid.RenderMode)value; }
    public bool ForceUpdate { get => grid.forceUpdate; set => grid.forceUpdate = value; }
    public float CycleRate { get => grid.cycleRate; set => grid.cycleRate = value; }

    public void Update()
    {
        renderModeUi.value = RenderModeAsInt;
        forceUpdateUi.isOn = ForceUpdate;
        cycleRateUi.value = CycleRate;
    }

    public void Regenerate()
    {
        grid.ForEachPos(pos =>
        {
            grid.RegenerateChunk(pos);
        });
    }
    public void Voxelize()
    {
        grid.ForEachChunk((pos, chunk) =>
        {
            grid.VoxelizeChunk(pos);
            chunk.isDirtyForMesh = true;
        });
    }
    public void VoxelizeSprings()
    {
        grid.ForEachChunk((pos, chunk) =>
        {
            grid.VoxelizeChunk(pos, true);
            chunk.isDirtyForMesh = true;
        });
    }
    public void GenSprings()
    {
        grid.ForEachPos(pos =>
        {
            grid.GenerateSpringsForChunk(pos, true);
        });
        grid.updateSprings = true;
        grid.renderMode = ChunkGrid.RenderMode.SpringsAsMesh;
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(ChunkGridShorthands))]
public class ChunkGridShorthandsEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        ChunkGridShorthands grid = (ChunkGridShorthands)target;

        if (!Application.isPlaying) return;
        EditorGUILayout.Space(9);
        EditorGUILayout.LabelField("Function Calls", EditorStyles.boldLabel);
        if (GUILayout.Button("Regenerate"))
        {
            grid.Regenerate();
        }
        if (GUILayout.Button("Voxelize mass"))
        {
            grid.Voxelize();
        }
        if (GUILayout.Button("Voxelize springs"))
        {
            grid.VoxelizeSprings();
        }
        if (GUILayout.Button("Generate Springs"))
        {
            grid.GenSprings();
        }
    }
}
#endif
