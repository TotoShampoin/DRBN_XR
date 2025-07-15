using UnityEngine;

[RequireComponent(typeof(MeshRenderer))]
public class TestParticle : MonoBehaviour
{
    MeshRenderer mr;
    TestSHM parent;

    public Color Color { get => mr.material.color; set => mr.material.color = value; }
    Vector3 oldPos;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        mr = GetComponent<MeshRenderer>();
        oldPos = transform.position;
        parent = transform.parent.GetComponent<TestSHM>();
    }

    // Update is called once per frame
    void Update()
    {
        if (oldPos != transform.position)
            parent.TriggerMove(oldPos, transform.position, this);
        oldPos = transform.position;
    }
}
