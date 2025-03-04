using UnityEngine;

public class MeshBrush3D : MonoBehaviour
{
    [Range(0, 5)] public float BrushSize = 1.0f;
    public bool IsPainting = false;
    public bool PaintOrErase = true;

    public void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawWireSphere(Vector3.zero, BrushSize * 0.125f);
    }
}
