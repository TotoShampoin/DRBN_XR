using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TurnAfterDelay : MonoBehaviour
{
    public Transform target;
    


    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 directionToFace = target.position - transform.position;
        Debug.DrawRay(transform.position, directionToFace, Color.green);
        
        //access current rotation
        Quaternion targetRotation = Quaternion.LookRotation(directionToFace);

        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime);
    }
}
