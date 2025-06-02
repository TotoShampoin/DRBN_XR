using System.Collections.Generic;
using System.Threading.Tasks;
using Assets.Voxelization;
using UnityEngine;
using UnityEngine.InputSystem;

public class Test_Voxelizer : MonoBehaviour
{
    public Voxelizer voxelizer;
    public RenderTexture texture;
    public RenderTexture normalTexture;
    public InputActionReference nextLineEvent;
    public InputActionReference newStripEvent;
    public InputActionReference resetEvent;
    public bool showNormalMap;

    readonly List<(Vector2 a, Vector2 b)> lines = new();

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
        meshFilter.mesh = ToMesh();
        voxelizer.Voxelize(meshFilter.mesh, texture, normalTexture);
    }

    void OnDraw(InputAction.CallbackContext evt, bool isRight)
    {
        if (!isRight && lastClick.HasValue)
            lines.Add((lastClick.Value, mousePos));
        lastClick = mousePos;
    }
    void OnReset()
    {
        lines.Clear();
        lastClick = null;
    }

    Mesh ToMesh()
    {
        // Lines span in a square of size screen-height centered at screen-center.
        // Voxelizer.Voxelize() expects a mesh in bounds from -0.5 to 0.5 centered at 0.
        // This function should convert the screen space lines into a world space mesh, where 
        //  each line maps to a quad aligned to look like a line on the XY plane, and has a Z-width of 1.
        // There shall be no concept of thickness, as this is meant to be a line to surface mesh conversion.

        // Key properties:
        //  1. Screen space is in canvas pixels, as the line is to be drawn by the user by clicking on screen
        //  2. Proportion must be retained (which is a given, as we're spanning from a square to a square)
        //  3. If some points are outside of the screen square, they shall be outside of the world square. *Do not make edge cases out of them*
        //  4. Screen-space size *is* screen height. It is written like that.

        if (lines.Count == 0)
            return new();

        // Get screen height in pixels
        float screenHeight = Screen.height;
        float screenWidth = Screen.width;
        float squareSize = screenHeight;

        // Center of the screen in pixel coordinates
        Vector2 screenCenter = new(screenWidth / 2f, screenHeight / 2f);

        // Prepare mesh data
        List<Vector3> vertices = new();
        List<int> indices = new();

        foreach (var (a, b) in lines)
        {
            // Convert screen space to normalized [-0.5, 0.5] world space
            Vector2 aNorm = (a - screenCenter) / (squareSize * 2);
            Vector2 bNorm = (b - screenCenter) / (squareSize * 2);

            // Each line becomes a quad (two triangles) with Z from -0.5 to 0.5
            // Four vertices per line
            Vector3 v0 = new(aNorm.x, aNorm.y, -0.5f);
            Vector3 v1 = new(aNorm.x, aNorm.y, 0.5f);
            Vector3 v2 = new(bNorm.x, bNorm.y, 0.5f);
            Vector3 v3 = new(bNorm.x, bNorm.y, -0.5f);

            int baseIndex = vertices.Count;
            vertices.Add(v0);
            vertices.Add(v1);
            vertices.Add(v2);
            vertices.Add(v3);
            // Two triangles per quad
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
        // mesh = MeshMod.DeduplicateVertices(mesh);
        return mesh;
    }
}
