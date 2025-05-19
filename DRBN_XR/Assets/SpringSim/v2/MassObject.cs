using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Assets.SpringSim.V2
{
    [RequireComponent(typeof(MeshRenderer), typeof(Rigidbody))]
    public class MassObject : MonoBehaviour
    {
        new Rigidbody rigidbody;
        MeshRenderer mesh;
        SpringSimulator parentSimulator;
        TextMeshPro debug;

        public Vector3 Position { get => rigidbody ? rigidbody.position : Vector3.zero; set { if (rigidbody) rigidbody.position = value; } }
        public Vector3 Velocity { get => rigidbody.linearVelocity; set => rigidbody.linearVelocity = value; }
        public bool UseGravity { get => rigidbody.useGravity; set => rigidbody.useGravity = value; }
        public void AddForce(Vector3 force, ForceMode mode = ForceMode.Force) => rigidbody.AddForce(force, mode);
        public float Damping { get => rigidbody.linearDamping; set => rigidbody.linearDamping = value; }

        public Vector3 Initial { get; set; }
        public bool ReturnToOrigin { get; set; }
        public float ComebackStiffness { get; set; }
        public float Rigidity { get; set; } = 0f;
        public float RigidityGradient { get; set; } = 0f;

        public bool Hovered { get; set; } = false;
        public bool Grabbed { get; set; } = false;
        public Vector3 GrabOrigin { get; set; }

        public bool PartiallyGrabbed { get; set; } = false;
        public Vector3 PartialDelta { get; set; }
        public float PartialInfluence { get; set; }
        public float PartialStrength { get; set; }

        // public InputActionReference rigidityController;

        const double k = 1.380649e-23; // J K−1

        // p[i].r = a + b
        // a = dT * sum(j => p[j].v * diffusion(p[i], p[j]))
        // b = dT / (k * T°) * sum(j => p[j].f * diffusion(p[i], p[j]))
        // c = noise(i, dT)

        // noise(i, dT): gaussian process (what's that?)

        // now, how does that translate to a Mass-Spring simulation :D

        void Start()
        {
            mesh = GetComponent<MeshRenderer>();
            rigidbody = GetComponent<Rigidbody>();
            Initial = Position;
            debug = GetComponentInChildren<TextMeshPro>();

            mesh.material.color = transparent;

            var sim = transform.parent.gameObject.GetComponent<SpringSimulator>();
            if (sim) parentSimulator = sim;

            // if (rigidityController != null)
            // {
            //     rigidityController.action.performed += ctx =>
            //     {
            //         Debug.Log($"{ctx.ReadValue<Vector2>()}");
            //         Vector2 axis = ctx.ReadValue<Vector2>();
            //         AxisControlsRigidity(axis);
            //     };
            //     rigidityController.action.canceled += ctx =>
            //     {
            //         Vector2 axis = ctx.ReadValue<Vector2>();
            //         AxisControlsRigidity(axis);
            //     };
            //     rigidityController.action.Enable();
            // }
        }

        void FixedUpdate()
        {
            if (ReturnToOrigin)
            {
                AddForce(ComebackStiffness * (Initial - Position));
            }
            if (PartiallyGrabbed)
            {
                var target = GrabOrigin + PartialDelta;
                AddForce(PartialStrength * PartialInfluence * (target - Position));
            }
        }

        Color transparent = new(0, 0, 0, 0);
        void Update()
        {
            // THIS ASSUMES A SPECIFIC SHADER!
            // if (Grabbed)
            //     mesh.material.color = Color.red;
            // else if (PartiallyGrabbed)
            //     mesh.material.color = Color.magenta;
            // else if (Hovered)
            //     mesh.material.color = Color.yellow;
            // else
            //     mesh.material.color = transparent;

            debug.transform.LookAt(Camera.main.transform);
            debug.transform.rotation *= Quaternion.Euler(0f, 180f, 0f);
            // if (Grabbed)
            //     debug.text = $"{Mathf.Round(Rigidity * 100) / 100}";
            // else
            //     debug.text = "";
            debug.text = "";

            Rigidity += RigidityGradient * Time.deltaTime;
        }

        public void ResetStates(Vector3 position, Quaternion rotation, Transform parent)
        {
            transform.SetParent(parent);
            transform.SetPositionAndRotation(position, rotation);
            if (rigidbody != null)
            {
                rigidbody.linearVelocity = Vector3.zero;
                rigidbody.angularVelocity = Vector3.zero;
                rigidbody.position = position;
                rigidbody.rotation = rotation;
            }

            Initial = position;
            ReturnToOrigin = false;
            ComebackStiffness = 0f;
            Rigidity = 0f;
            RigidityGradient = 0f;

            Hovered = false;
            Grabbed = false;
            GrabOrigin = Vector3.zero;

            PartiallyGrabbed = false;
            PartialDelta = Vector3.zero;
            PartialInfluence = 0f;
            PartialStrength = 0f;

            if (debug != null)
                debug.text = "";
        }

        public void AxisControlsRigidity(Vector2 axis)
        {
            if (Grabbed)
                RigidityGradient = axis.y;
        }

        public void OnHovered()
        {
            if (parentSimulator) parentSimulator.OnMassHovered(this);
            Hovered = true;
        }
        public void OnUnhovered()
        {
            Hovered = false;
        }
        public void OnGrabbed()
        {
            Grabbed = true;
            GrabOrigin = Position;
            if (parentSimulator) parentSimulator.OnMassGrabbed(this);
        }
        public void OnUngrabbed()
        {
            Grabbed = false;
            if (parentSimulator) parentSimulator.OnMassUngrabbed(this);
        }

        public void PartialGrab(float influence)
        {
            PartiallyGrabbed = true;
            GrabOrigin = Position;
            PartialInfluence = influence;
        }
        public void PartialUngrab()
        {
            PartiallyGrabbed = false;
        }
    }
}
