using Unity.XR.CoreUtils;
using UnityEditor;
using UnityEngine;

public class PlayerGuardian : MonoBehaviour
{
    public XROrigin player;
    public float yLimit = -10f;

    Vector3 origin;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        origin = player.transform.position;
    }

    // Update is called once per frame
    void Update()
    {
        if (player.transform.position.y < yLimit)
        {
            player.transform.position = origin;
        }
    }

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        if (!Selection.Contains(gameObject)) return;
        Vector3 p = Vector3.zero;
        float s = 0;
        float c = yLimit;
        if (player)
        {
            p = player.transform.position;
            s = p.y > yLimit ? (player.transform.position.y - yLimit) / 2 : 0;
            c = p.y > yLimit ? (player.transform.position.y + yLimit) / 2 : p.y;
        }
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(new(0, c, 0), new(100, 2 * s, 100));
        Gizmos.DrawLineList(new Vector3[] {
            new(p.x, p.y, -50), new(p.x, p.y, 50),
            new(-50, p.y, p.z), new(50, p.y, p.z),
            new(p.x, yLimit, -50), new(p.x, yLimit, 50),
            new(-50, yLimit, p.z), new(50, yLimit, p.z)
        });
    }
#endif
}
