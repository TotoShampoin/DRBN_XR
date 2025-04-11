using UnityEngine;

public class DebugSphere : MonoBehaviour
{
    void OnDrawGizmos()
    {
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawWireSphere(Vector3.zero, 0.5f);
    }
}
