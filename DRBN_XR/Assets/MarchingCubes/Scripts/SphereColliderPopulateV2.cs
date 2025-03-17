using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class SphereColliderPopulateV2 : MonoBehaviour
{
    public GameObject SpherePrefab;

    private readonly List<GameObject> objectPool = new();

    // Start is called before the first frame update
    // void Start()
    // {
    //     EnsurePoolSize(10000);
    // }

    public void ExtractAndPopulate(
        MeshFilter Populate, Transform withTransform = null)
    {
        var vertices = Populate.mesh.vertices;
        var normals = Populate.mesh.normals;
        var vertexCount = Populate.mesh.vertexCount;
        var t = withTransform ? withTransform : transform;
        EnsurePoolSize(vertexCount);
        for (int i = 0; i < vertexCount; i++)
        {
            objectPool[i].SetActive(true);
            objectPool[i].transform.SetPositionAndRotation(
                t.TransformPoint(vertices[i]),
                Quaternion.LookRotation(t.TransformDirection(normals[i])));
        }
        for (int i = vertexCount; i < objectPool.Count; i++)
        {
            objectPool[i].SetActive(false);
        }
    }


    void EnsurePoolSize(int requiredSize)
    {
        while (objectPool.Count < requiredSize)
        {
            GameObject obj = Instantiate(SpherePrefab, transform);
            obj.SetActive(true);
            objectPool.Add(obj);
        }
    }

}
