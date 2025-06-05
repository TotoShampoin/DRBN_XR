using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using System;
using System.Linq;

public class Langevin_dial : MonoBehaviour
{

    //public Rigidbody[] GOS;
    private GameObject[] GOS;
    public List<Rigidbody> RBS;
    static float thrust = 100;

    /*Langevin variables*/
    public static double temp = 000.0f;
    //public double temp { get; set; }
    public static double Temp
    {
        get
        {
            return temp;
        }
        set
        {
            temp = value;
        }
    }
    public static double kB = 1.38f * Math.Pow(10.0f, 23.0f);
    public static double viscosity = 6.6e-3;
    public static double Ma = (13e6f * 1.7f * Math.Pow(10.0f, -1f)) / 2.0f;
    public static double friction = 6f * Math.PI * viscosity * 20f * Math.Pow(10f, -9f);
    public static double dt = Ma / friction;
    //public static double sigma = Math.Sqrt(6.0f * friction * kB * temp / dt);
    //public static float sigmaf = (float)sigma;
    public static float frictionf = (float)friction;

    List<Rigidbody> CountObjects()
    {
        GOS = GameObject.FindGameObjectsWithTag("molecule") as GameObject[];

        for (int i = 0; i <= GOS.Length - 1; i++)
        {
            Rigidbody[] RB = (GOS[i].GetComponentsInChildren<Rigidbody>());

            List<Rigidbody> RBList = RB.ToList<Rigidbody>();
            RBS.AddRange(RBList);
            //Debug.Log("RBS Count " + RBS.Count);
        }
        return RBS;
    }

    //List<Rigidbody> CountObjects()
    //{
    //    GOS = GameObject.FindGameObjectsWithTag("molecule") as GameObject[];

    //    for (int i = 0; i <= GOS.Length - 1; i++)
    //    {
    //        Rigidbody[] RB = (GOS[i].GetComponentsInChildren<Rigidbody>());

    //        List<Rigidbody> RBList = RB.ToList<Rigidbody>();
    //        RBS.AddRange(RBList);
    //        //Debug.Log("RBS Count " + RBS.Count);
    //    }
    //    return RBS;
    //}

    public void CountMOFOS()
    {
        CountObjects();
        Debug.Log("mofos");
    }

    Vector3 langevin_tr(Rigidbody arg1, float arg2, float arg3)
    {
        Vector3 argvb = arg1.linearVelocity;
        Vector3 randvec = UnityEngine.Random.insideUnitSphere;

        float rx = randvec[0];
        float ry = randvec[1];
        float rz = randvec[2];
        float argfx = 2f * arg2 * rx - arg3 * argvb[0];
        float argfy = 2f * arg2 * ry - arg3 * argvb[1];
        float argfz = 2f * arg2 * rz - arg3 * argvb[2];
        Vector3 addF = new Vector3(argfx, argfy, argfz);
        return addF;
    }



    // Use this for initialization

    void OnGUI()
    {
        GUI.Label(new Rect(0, 0, 1000, 100), "temp " + temp.ToString());
        //GUI.Label(new Rect(0, 10, 1000, 100), "sigma " + sigmaf.ToString("E3"));
        GUI.Label(new Rect(0, 20, 1000, 100), "friction " + frictionf.ToString("E3"));
    }

    void RndF()
    {
        foreach (Rigidbody RB in RBS)
        {
            //if (RB.transform.root.name != "char_shadow")
            //{
            double sigma = new double();
            float sigmaf = new float();
            sigma = Math.Sqrt(6.0f * friction * kB * temp / dt);
            sigmaf = (float)sigma;
            Vector3 addF = langevin_tr(RB, sigmaf, frictionf);
            //Debug.Log("sigma " + sigmaf);
            //Debug.Log("RB " + RB.name);
            RB.AddForce(addF);
            //}
        }
    }


    // Use this for initialization
    void Awake()
    {
        CountObjects();
    }

    void Start()
    {
        ////in a pinch, this works now
        //GOS = GameObject.FindGameObjectsWithTag("molecule") as GameObject[];

        //for (int i=0; i<=GOS.Length; i++)
        //{
        //    //Debug.Log("int i " + i + " GOS.Length " + GOS.Length);
        //    //Debug.Log("GOS length " + GOS[i].name );
        //    Rigidbody[] RB = (GOS[i].GetComponentsInChildren<Rigidbody>());
        //    //Debug.Log("RB Count " + RB.Count());
        //    List<Rigidbody> RBList = RB.ToList<Rigidbody>();
        //    RBS.AddRange(RBList);
        //    Debug.Log("RBS Count " + RBS.Count);
        //}
    }

    // Update is called once per frame
    void Update()
    {
        //CountObjects(); // works, but try calling CountObjects upon instantiating to save resources. 
        //Debug.Log("oy!");
        RndF();
    }

    public void Refresh(InputAction.CallbackContext context)
    {
        Debug.Log("balibalo");
        CountObjects();
    }
}

