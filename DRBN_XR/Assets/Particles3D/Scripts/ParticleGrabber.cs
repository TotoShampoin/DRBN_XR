using UnityEngine;
using UnityEngine.InputSystem;

class ParticleGrabber : MonoBehaviour
{
    [SerializeField] ParticleSimulator simulator;
    [SerializeField] Transform display;
    [SerializeField] Transform cursor;
    [SerializeField] ParticleDisplayType displayType = ParticleDisplayType.NoTransform;
    [SerializeField] InputActionReference grabTrigger;

    [SerializeField] ComputeShader attractToCursor;
    [SerializeField] float attractForce = 1f;
    [SerializeField] float attractRadius = 0.5f;

    bool isGrabbing = false;

    void Start()
    {
        grabTrigger.action.performed += _ => isGrabbing = true;
        grabTrigger.action.canceled += _ => isGrabbing = false;
    }

    void Update()
    {
        if (isGrabbing) Run();
    }

    void Run()
    {
        int threadsCount = Mathf.CeilToInt((float)simulator.ParticleCount / 256);
        var kernel = attractToCursor.FindKernel("Attract");

        attractToCursor.SetFloat("deltaTime", Time.deltaTime);
        attractToCursor.SetInt("particleCount", simulator.ParticleCount);
        attractToCursor.SetVector("attractTo", CursorPosition());
        attractToCursor.SetFloat("attractForce", attractForce);
        attractToCursor.SetFloat("attractRadius", attractRadius);

        attractToCursor.SetBuffer(kernel, "Positions", simulator.positionsBuffer);
        attractToCursor.SetBuffer(kernel, "Velocities", simulator.velocitiesBuffer);
        attractToCursor.Dispatch(kernel, threadsCount, 1, 1);
    }

    Vector3 CursorPosition()
    {
        switch (displayType)
        {
            default:
            case ParticleDisplayType.NoTransform:
                {
                    return cursor.position;
                }
            case ParticleDisplayType.UseTransform:
                {
                    return display.InverseTransformPoint(cursor.position);
                }

        }
    }
};


enum ParticleDisplayType
{
    NoTransform,
    UseTransform,
    // Remap,
}
