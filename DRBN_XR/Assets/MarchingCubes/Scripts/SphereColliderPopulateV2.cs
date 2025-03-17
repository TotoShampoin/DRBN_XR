using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;

public class SphereColliderPopulateV2 : MonoBehaviour
{
    public GameObject SpherePrefab;

    private Vector3[] VertList;
    private Vector3[] NormList;
    private bool isUpdated = false;

    private readonly List<GameObject> objectPool = new();

    // Start is called before the first frame update
    // void Start()
    // {
    //     EnsurePoolSize(10000);
    // }

    // Update is called once per frame
    void Update()
    {
        if (isUpdated)
        {
            Populate(VertList, NormList);
            isUpdated = false;
        }
    }

    public void ExtractAll(MeshFilter Populate, Transform withTransform = null)
    {
        VertList = new Vector3[Populate.mesh.vertexCount];
        NormList = new Vector3[Populate.mesh.vertexCount];
        for (int i = 0; i < VertList.Length; i++)
        {
            var t = withTransform ? withTransform : transform;
            var vertex = Populate.mesh.vertices[i];
            var normal = Populate.mesh.normals[i];
            VertList[i] = t.TransformPoint(vertex);
            NormList[i] = t.TransformDirection(normal);
        }
        isUpdated = true;
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

    void Populate(Vector3[] VertList, Vector3[] NormList)
    {
        EnsurePoolSize(VertList.Length);
        for (int i = 0; i < VertList.Length; i++)
        {
            objectPool[i].SetActive(true);
            objectPool[i].transform.SetPositionAndRotation(
                VertList[i], Quaternion.LookRotation(NormList[i]));
        }
        for (int i = VertList.Length; i < objectPool.Count; i++)
        {
            objectPool[i].SetActive(false);
        }
    }
}
