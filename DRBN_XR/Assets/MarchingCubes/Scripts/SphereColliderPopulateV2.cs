using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;

public class SphereColliderPopulateV2 : MonoBehaviour
{
    // public GameObject PopulateGO;

    public GameObject SpherePrefab;

    private Vector3[] VertList;
    private Vector3[] NormList;
    private SphereCollider[] Population;
    private GameObject[] Debugsphere;
    private bool isUpdated = false;

    private List<GameObject> objectPool = new();

    // Start is called before the first frame update
    void Start()
    {
        // ExtractAndPopulate();
        // EnsurePoolSize(10000);
    }

    // Update is called once per frame
    void Update()
    {
        if (isUpdated)
        {
            // ClearPopulate();
            Populate(VertList, NormList);
            isUpdated = false;
        }
    }

    public void ExtractAll(MeshFilter Populate)
    {
        VertList = ExtractVert(Populate);
        NormList = ExtractNorm(Populate);
        // Debug.Log(VertList.Length + " Length");
        isUpdated = true;
    }

    Vector3[] ExtractVert(MeshFilter Populate)
    {
        Mesh PopulateMesh = Populate.mesh;
        Debug.Log(PopulateMesh);
        VertList = PopulateMesh.vertices;

        Vector3[] vworld = new Vector3[VertList.Length];
        for (int i = 0; i < VertList.Length; i++)
        {
            vworld[i] = transform.TransformPoint(VertList[i]);
        }

        return vworld;
    }

    Vector3[] ExtractNorm(MeshFilter Populate)
    {
        Mesh PopulateMesh = Populate.mesh;
        NormList = PopulateMesh.normals;

        return NormList;
    }

    public void ActivateObj(GameObject obj, bool active)
    {
        obj.SetActive(active);
    }

    public void ClearPopulate()
    {
        foreach (GameObject obj in objectPool)
        {
            ActivateObj(obj, false);
        }
    }

    void EnsurePoolSize(int requiredSize)
    {
        Debug.Log($"EnsurePoolSize: {requiredSize}");
        // Add objects if pool is too small
        while (objectPool.Count < requiredSize)
        {
            GameObject obj = Instantiate(SpherePrefab, Vector3.zero, Quaternion.identity, transform);
            obj.SetActive(true);
            ActivateObj(obj, true);
            objectPool.Add(obj);
        }
    }

    bool CheckNearbyParallel(Vector3[] collection, Vector3 point, float threshold)
    {
        return collection.Any(vec =>
            Vector3.Distance(vec, point) < threshold);
    }

    void Populate(Vector3[] VertList, Vector3[] NormList)
    {
        EnsurePoolSize(VertList.Length);
        for (int i = 0; i < VertList.Length; i++)
        {
            ActivateObj(objectPool[i], true);
            objectPool[i].transform.SetPositionAndRotation(
                VertList[i], Quaternion.LookRotation(NormList[i]));
        }
        for (int i = VertList.Length; i < objectPool.Count; i++)
        {
            ActivateObj(objectPool[i], false);
        }
    }
}
