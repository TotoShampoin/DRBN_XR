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
    public ChunkGrabber grabber;

    //

    public TMP_Dropdown renderModeUi;
    public TMP_Dropdown springsModeDdUi;
    public Slider cycleRateUi;
    public Slider mergeDistanceUi;
    public Slider joinDistanceUi;
    public Slider volumeThresholdUi;

    public bool ForceUpdate { get => grid.forceUpdate; set => grid.forceUpdate = value; }
    public int RenderModeAsInt { get => (int)grid.renderMode; set => grid.renderMode = (ChunkGrid.RenderMode)value; }
    public int SpringsModeAsInt { get => grid.updateSprings ? 1 : 0; set => grid.updateSprings = value != 0; }
    public float CycleRate { get => grid.cycleRate; set => grid.cycleRate = value; }
    public float MergeDistance { get => grid.mergeDistance; set => grid.mergeDistance = value; }
    public float JoinDistance { get => grid.joinDistance; set => grid.joinDistance = value; }
    public float VolumeThreshold { get => grid.volumeThreshold; set => grid.volumeThreshold = value; }

    //

    public Slider paintRadiusUi;
    public Slider paintWeightUi;
    public Toggle smoothTrianglesUi;

    public float PaintRadius { get => grid.paintRadius; set => grid.paintRadius = value; }
    public float PaintWeight { get => grid.paintWeight; set => grid.paintWeight = value; }
    public bool SmoothTriangles { get => grid.marchingCubes.smooth; set => grid.marchingCubes.smooth = value; }

    //

    public Slider distanceAttenuationUi;
    public Slider distanceSensitivityUi;

    public float DistanceAttenuation { get => grid.distanceOfVolumes.attenuation; set => grid.distanceOfVolumes.attenuation = value; }
    public float DistanceSensitivity { get => grid.distanceOfVolumes.distanceSensitivity; set => grid.distanceOfVolumes.distanceSensitivity = value; }

    //

    public Slider particleMassUi;
    public Slider stiffnessUi;
    public Slider viscosityUi;
    public Slider dragUi;

    public float ParticleMass { get => grid.springSimulator.ParticleMass; set => grid.springSimulator.ParticleMass = value; }
    public float Stiffness { get => grid.springSimulator.Stiffness; set => grid.springSimulator.Stiffness = value; }
    public float Viscosity { get => grid.springSimulator.Viscosity; set => grid.springSimulator.Viscosity = value; }
    public float Drag { get => grid.springSimulator.Drag; set => grid.springSimulator.Drag = value; }

    //

    public void Update()
    {
        renderModeUi.value = RenderModeAsInt;
        springsModeDdUi.value = SpringsModeAsInt;
        cycleRateUi.value = CycleRate;
        mergeDistanceUi.value = MergeDistance;
        joinDistanceUi.value = JoinDistance;
        volumeThresholdUi.value = VolumeThreshold;

        paintRadiusUi.value = PaintRadius;
        paintWeightUi.value = PaintWeight;
        smoothTrianglesUi.isOn = SmoothTriangles;

        distanceAttenuationUi.value = DistanceAttenuation;
        distanceSensitivityUi.value = DistanceSensitivity;

        particleMassUi.value = ParticleMass;
        stiffnessUi.value = Stiffness;
        viscosityUi.value = Viscosity;
        dragUi.value = Drag;

        grabber.gameObject.SetActive(SpringsModeAsInt == 1);
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
