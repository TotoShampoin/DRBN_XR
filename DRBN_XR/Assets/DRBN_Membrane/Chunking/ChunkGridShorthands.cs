using TMPro;
using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// A separate class to handle the membrane with UI
/// </summary>
public class ChunkGridShorthands : MonoBehaviour
{
    public ChunkGrid grid;
    public TMP_Dropdown renderModeUi;
    public Slider cycleRateUi;
    public Toggle springsModeUi;
    public Slider mergeDistanceUi;
    public Slider joinDistanceUi;
    public Slider volumeThresholdUi;

    public int RenderModeAsInt { get => (int)grid.renderMode; set => grid.renderMode = (ChunkGrid.RenderMode)value; }
    public bool ForceUpdate { get => grid.forceUpdate; set => grid.forceUpdate = value; }
    public float CycleRate { get => grid.cycleRate; set => grid.cycleRate = value; }
    public bool SpringsMode { get => grid.updateSprings; set => grid.updateSprings = value; }
    public float MergeDistance { get => grid.mergeDistance; set => grid.mergeDistance = value; }
    public float JoinDistance { get => grid.joinDistance; set => grid.joinDistance = value; }
    public float VolumeThreshold { get => grid.volumeThreshold; set => grid.volumeThreshold = value; }

    public void Update()
    {
        renderModeUi.value = RenderModeAsInt;
        cycleRateUi.value = CycleRate;
        springsModeUi.isOn = SpringsMode;
        mergeDistanceUi.value = MergeDistance;
        joinDistanceUi.value = JoinDistance;
        volumeThresholdUi.value = VolumeThreshold;
    }

    public void Regenerate()
    {
        grid.ForEach(pos => grid.GenerateVolume(pos));
        grid.ForEach(pos => grid.VolumeToMesh(pos));
    }
    public void ForceVolumeUpdate()
    {
        grid.forceUpdate = true;
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
            grid.Regenerate();
        if (GUILayout.Button("Force Volume Update"))
            grid.ForceVolumeUpdate();
    }
}
#endif
