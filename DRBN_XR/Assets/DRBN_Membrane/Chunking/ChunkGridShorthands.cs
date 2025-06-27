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
    public Toggle springsModeUi;

    public int RenderModeAsInt { get => (int)grid.renderMode; set => grid.renderMode = (ChunkGrid.RenderMode)value; }
    public bool ForceUpdate { get => grid.forceUpdate; set => grid.forceUpdate = value; }
    public float CycleRate { get => grid.cycleRate; set => grid.cycleRate = value; }
    public bool SpringsMode { get => grid.updateSprings; set => grid.updateSprings = value; }

    public void Update()
    {
        renderModeUi.value = RenderModeAsInt;
        forceUpdateUi.isOn = ForceUpdate;
        cycleRateUi.value = CycleRate;
        springsModeUi.isOn = SpringsMode;
    }

    public void Regenerate()
    {
        grid.ForEach(pos => grid.GenerateVolume(pos));
        grid.ForEach(pos => grid.VolumeToMesh(pos));
    }
    public void Voxelize()
    {
        grid.ForEach((pos, chunk) =>
        {
            grid.MeshToVolume(pos);
        });
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
        if (GUILayout.Button("Voxelize"))
        {
            grid.Voxelize();
        }
    }
}
#endif
