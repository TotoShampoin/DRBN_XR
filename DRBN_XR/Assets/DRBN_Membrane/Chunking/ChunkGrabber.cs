using UnityEngine;
using SpringSim.V3;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using System.Collections.Generic;
using UnityEngine.XR.Interaction.Toolkit.Interactors;




#if UNITY_EDITOR
using UnityEditor;
#endif

class ChunkGrabber : MonoBehaviour
{
    public ChunkGrid grid;
    public Vector3Int selectedChunkIndex;
    public ChunkGrid.ChunkData selectedChunk;
    public SpringSimulatorState selectedSpring;
    public XRGrabInteractable xrGrabInteractable;
    public Mass selectedMass;
    public float radius = 0.25f;
    public float force = 1500f;
    public bool previewClosestMass = true;
    public bool useInEditor = false;
    public bool regrabNext = false;

    public LineRenderer forceArrowDebug;
    Vector3 F;
    bool isGrabbing;

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
        if (previewClosestMass)
        {
            var (_, chunk, closest) = grid.GetClosestMass(transform.position);
            if (chunk != null)
            {
                var closestGlobal = grid.GridToWorldPosition(
                    chunk.springs.LocalToGlobalPosition(closest.position));
                Graphics.RenderMesh(
                    new(mat), sphere, 0,
                    Matrix4x4.TRS(
                        closestGlobal, Quaternion.identity,
                        Vector3.one * 0.025f)
                );
            }
        }

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
        }

        if (regrabNext)
        {
            StartGrab();
            regrabNext = false;
        }
    }

    void FixedUpdate()
    {
        if (!isGrabbing) return;

        if (selectedChunk.dirtySpringsM)
        {
            EndGrab();
            regrabNext = true;
            return;
        }

        var selectedSprings = selectedChunk.springs;

        var massPos = grid.GridToWorldPosition(
            selectedSprings.LocalToGlobalPosition(selectedMass.position));
        F = SpringSimulatorNoBehaviour
            .SpringPull(massPos, transform.position, 0f, force);
        grid.ApplyForceToSprings(selectedChunkIndex, F, massPos, radius, true);
    }

    public void SetPosition(Vector3 position)
    {
        if (useInEditor || selectedMass != null) return;
        transform.position = position;
    }
    public void StartGrab()
    {
        var (idx, chunk, mass) = grid.GetClosestMass(transform.position);
        if (chunk == null || mass == null) return;
        selectedMass = mass;
        selectedChunk = chunk;
        selectedChunkIndex = idx;
        selectedSpring = chunk.springs;
        isGrabbing = true;
    }
    public void EndGrab()
    {
        selectedMass = null;
        selectedChunk = null;
        selectedChunkIndex = Vector3Int.zero;
        selectedSpring = null;
        isGrabbing = false;
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

