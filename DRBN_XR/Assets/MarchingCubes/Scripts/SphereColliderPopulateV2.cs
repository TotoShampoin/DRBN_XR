using System.Collections.Generic;
using System.Threading.Tasks;
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
        EnsurePoolSize(10000);
    }

    // Update is called once per frame
    void Update()
    {
        if (isUpdated)
        {
            ClearPopulate();
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
        // if (active)
        // {
        //     obj.GetComponent<MeshRenderer>().enabled = true;
        //     obj.GetComponent<Collider>().enabled = true;
        // }
        // else
        // {
        //     obj.GetComponent<MeshRenderer>().enabled = false;
        //     obj.GetComponent<Collider>().enabled = false;
        // }
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

        // for (int i = 0; i < VertList.Length; i++)
        // {
        //     GameObject ColliderOrientation = new GameObject();
        //     ColliderOrientation.transform.parent = transform;
        //     SphereCollider Sphere = ColliderOrientation.AddComponent<SphereCollider>();
        //     Sphere.radius = 0.03f;

        //     GameObject TriggerOrientation = new GameObject("trigger");
        //     TriggerOrientation.transform.parent = transform;

        //     SphereCollider Sphere_Trig = TriggerOrientation.AddComponent<SphereCollider>();
        //     Sphere_Trig.radius = 0.03f;
        //     Sphere_Trig.isTrigger = true;


        //     ColliderOrientation.transform.position = VertList[i];
        //     ColliderOrientation.transform.localRotation = Quaternion.LookRotation(NormList[i]);

        //     TriggerOrientation.transform.position = VertList[i];
        //     TriggerOrientation.transform.localRotation = Quaternion.LookRotation(NormList[i]);

        //     TriggerOrientation.AddComponent<ImpalaGeneralized>();

        //     GameObject DSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        //     Destroy(GetComponent<Collider>());
        //     DSphere.transform.parent = transform;
        //     DSphere.transform.position = VertList[i];
        //     DSphere.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
        //     DSphere.transform.localRotation = Quaternion.LookRotation(NormList[i]);

        //     Debug.Log(PopulateGO.GetComponent<MeshRenderer>().materials[0].name);
        //     Shader Green = Shader.Find("DRBN_STEAMVR/Material/Transparent Green");
        //     GetComponent<MeshRenderer>().material.shader = Green;
        // }
    }
}
