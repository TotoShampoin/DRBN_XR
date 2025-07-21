using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Voxelization;

public class Test_Voxelizer : MonoBehaviour
{
    public Voxelizer voxelizer;
    public RenderTexture texture;
    public RenderTexture normalTexture;
    public InputActionReference nextLineEvent;
    public InputActionReference newStripEvent;
    public InputActionReference resetEvent;
    public bool showNormalMap;
    public float arrowSize;
    public bool modified;
    public bool mergeVertices = true;

    readonly List<(Vector2 a, Vector2 b)> lines = new();
    VoxelizerCPU voxelizerDebug = new();

    public LineRenderer cursorLine;
    public LineRenderer normalLine;

    Vector2? lastClick;
    Vector2 mousePos;

    MeshFilter meshFilter;

    void Start()
    {
        nextLineEvent.action.Enable();
        newStripEvent.action.Enable();
        resetEvent.action.Enable();

        nextLineEvent.action.canceled += evt => OnDraw(evt, false);
        newStripEvent.action.canceled += evt => OnDraw(evt, true);
        resetEvent.action.canceled += evt => OnReset();

        meshFilter = GetComponent<MeshFilter>();
        meshFilter.mesh = new();
    }

    void Update()
    {
        mousePos = Mouse.current.position.ReadValue();
        if (modified)
        {
            meshFilter.mesh = ToMesh();
            voxelizer.Voxelize(meshFilter.mesh, texture, normalTexture);
            modified = false;
        }
        DebugVoxelizer();
    }

    void OnDraw(InputAction.CallbackContext evt, bool isRight)
    {
        if (!isRight && lastClick.HasValue)
            lines.Add((lastClick.Value, mousePos));
        lastClick = mousePos;
        modified = true;
    }
    void OnReset()
    {
        lines.Clear();
        lastClick = null;
        modified = true;
    }

    Mesh ToMesh()
    {
        if (lines.Count == 0)
            return new();

        float screenHeight = Screen.height;
        float screenWidth = Screen.width;
        float squareSize = screenHeight;

        Vector2 screenCenter = new(screenWidth / 2f, screenHeight / 2f);

        List<Vector3> vertices = new();
        List<int> indices = new();
        foreach (var (a, b) in lines)
        {
            Vector2 aNorm = (a - screenCenter) / (squareSize * 2);
            Vector2 bNorm = (b - screenCenter) / (squareSize * 2);

            Vector3 v0 = new(aNorm.x, aNorm.y, -0.5f);
            Vector3 v1 = new(aNorm.x, aNorm.y, 0.5f);
            Vector3 v2 = new(bNorm.x, bNorm.y, 0.5f);
            Vector3 v3 = new(bNorm.x, bNorm.y, -0.5f);

            int baseIndex = vertices.Count;
            vertices.Add(v0);
            vertices.Add(v1);
            vertices.Add(v2);
            vertices.Add(v3);
            indices.Add(baseIndex + 0);
            indices.Add(baseIndex + 1);
            indices.Add(baseIndex + 2);
            indices.Add(baseIndex + 2);
            indices.Add(baseIndex + 3);
            indices.Add(baseIndex + 0);
        }


        Mesh mesh = new();
        mesh.SetVertices(vertices);
        mesh.SetTriangles(indices, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        if(mergeVertices)
            mesh = MeshMod.DeduplicateVertices(mesh);
        return mesh;
    }

    void DebugVoxelizer()
    {
        voxelizerDebug.mesh = meshFilter.mesh;
        voxelizerDebug.voxelBound = voxelizer.voxelBounds;
        voxelizerDebug.meshBound = new(Vector3.zero, 2 * Vector3.one);

        var mouse = (mousePos - new Vector2(Screen.width / 2f, Screen.height / 2f)) / (Screen.height * 2);
        var mouseForMesh = new Vector3(mouse.x, mouse.y, 0.5f);

        var debugData = voxelizerDebug.VoxelizeAtPositionDebug(mouseForMesh);

        var point = mouseForMesh;
        var direction = (debugData.projectedPoint - mouseForMesh).normalized;

        cursorLine.startWidth = arrowSize;
        cursorLine.endWidth = arrowSize;
        normalLine.startWidth = arrowSize;
        normalLine.endWidth = arrowSize;

        cursorLine.SetPosition(0, point);
        cursorLine.SetPosition(1, point + arrowSize * direction);
        normalLine.SetPosition(0, debugData.projectedPoint);
        normalLine.SetPosition(1, debugData.projectedPoint + arrowSize * debugData.projectedNormal);
    }
}
