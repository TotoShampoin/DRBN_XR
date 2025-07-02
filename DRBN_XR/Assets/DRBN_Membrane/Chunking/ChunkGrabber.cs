using UnityEngine;
using SpringSim.V3;

#if UNITY_EDITOR
using UnityEditor;
#endif

class ChunkGrabber : MonoBehaviour
{
    public ChunkGrid grid;
    public Vector3Int? selectedChunkIndex;
    public ChunkGrid.ChunkData selectedChunk;
    public Mass selectedMass;
    public float radius = 0.25f;
    public float force = 1500f;

    public LineRenderer forceArrowDebug;
    Vector3 F;

    Mesh sphere;
    Material mat;
    void Start()
    {
        var sphereGO = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere = sphereGO.GetComponent<MeshFilter>().sharedMesh;
        mat = new Material(Shader.Find("Standard"));
        Destroy(sphereGO);
    }

    void Update()
    {
        if (selectedMass != null)
        {
            var selectedSprings = selectedChunk.springs;
            var pos = grid.GridToWorldPosition(
                selectedSprings
                    .LocalToGlobalPosition(selectedMass.position));

            if (forceArrowDebug != null && forceArrowDebug.enabled)
            {
                forceArrowDebug.SetPosition(0, pos);
                forceArrowDebug.SetPosition(1, pos + F / 1500f * 0.25f);
            }

            Graphics.RenderMesh(
                new(mat), sphere, 0, Matrix4x4.TRS(
                    pos, Quaternion.identity, Vector3.one * 0.05f
                )
            );

            grid.SurroundingChunks(selectedChunkIndex ?? throw new System.Exception("Index unset"))
                .ForEach(c =>
                {
                    c.Item2.highlight = true;
                });
        }
    }

    void FixedUpdate()
    {
        if (selectedMass == null || selectedChunk == null) return;
        var selectedSprings = selectedChunk.springs;

        var massPos = grid.GridToWorldPosition(
            selectedSprings.LocalToGlobalPosition(selectedMass.position));
        F = SpringSimulatorNoBehaviour
            .SpringPull(massPos, transform.position, 0f, force);
        grid.ApplyForceToSprings(selectedChunkIndex.Value, F, massPos, radius, true);
    }

    public void SetPosition(Vector3 position)
    {
        if (selectedMass != null) return;
        transform.position = position;
    }
    public void StartGrab()
    {
        var globalPosition = grid.WorldToGridPosition(transform.position);
        var (chunkPos, chunk) = grid.GetChunk(transform.position) ?? (Vector3Int.zero, null);
        if (chunk == null || chunk.springs == null) return;
        selectedChunk = chunk;
        selectedChunkIndex = chunkPos;
        selectedMass = selectedChunk.springs.ClosestMassGlobal(globalPosition);
    }
    public void EndGrab()
    {
        selectedMass = null;
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(ChunkGrabber))]
public class ChunkGrabberEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        ChunkGrabber grabber = (ChunkGrabber)target;

        if (!Application.isPlaying) return;
        EditorGUILayout.Space(9);
        EditorGUILayout.LabelField("Function Calls", EditorStyles.boldLabel);
        if (GUILayout.Button("Start grab"))
        {
            grabber.StartGrab();
        }
        if (GUILayout.Button("End grab"))
        {
            grabber.EndGrab();
        }
    }
}
#endif

