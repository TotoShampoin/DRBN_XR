using UnityEngine;

public class script_test_camera : MonoBehaviour
{
    void LateUpdate()
    {
        // Ne garder que la rotation autour de l'axe Y
        Vector3 currentEuler = transform.rotation.eulerAngles;
        transform.rotation = Quaternion.Euler(0f, currentEuler.y, 0f);
    }
}
