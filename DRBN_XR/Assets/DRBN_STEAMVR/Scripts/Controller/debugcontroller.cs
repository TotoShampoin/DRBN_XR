using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// DebugController class to manage the activation and positioning of game objects in the scene.
/// This class is used for toggle on and placing the controller in front the the camera when not used in a VR environment.
/// </summary>
public class DebugController : MonoBehaviour
{
    [System.Serializable]
    public class ObjectTransform
    {
        public GameObject gameObject;
        public Vector3 localPosition;
        public Vector3 localEulerAngles;
    }

    public List<GameObject> objectsToEnable = new List<GameObject>();
    public List<ObjectTransform> objectsToPosition = new List<ObjectTransform>();

    void Start()
    {
        foreach (var obj in objectsToPosition)
        {
            if (obj.gameObject != null)
            {
                obj.gameObject.transform.localPosition = obj.localPosition;
                obj.gameObject.transform.localEulerAngles = obj.localEulerAngles;
            }
        }
    }

    void Update()
    {
        foreach (var obj in objectsToEnable)
        {
            if (obj != null && !obj.activeSelf)
            {
                obj.SetActive(true);
            }
        }
    }
}
