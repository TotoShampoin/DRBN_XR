using UnityEngine;

namespace Assets.SpringSim.v1
{

    public class DebugSphere : MonoBehaviour
    {
        void OnDrawGizmos()
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireSphere(Vector3.zero, 0.5f);
        }
    }

}
