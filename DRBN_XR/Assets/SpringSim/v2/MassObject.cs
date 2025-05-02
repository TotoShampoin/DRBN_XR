using UnityEngine;

namespace Assets.SpringSim.V2
{
    [RequireComponent(typeof(MeshRenderer), typeof(Rigidbody))]
    public class MassObject : MonoBehaviour
    {
        new Rigidbody rigidbody;
        MeshRenderer mesh;
        SpringSimulator parentSimulator;

        public Vector3 Position { get => rigidbody.position; set => rigidbody.position = value; }
        public Vector3 Velocity { get => rigidbody.linearVelocity; set => rigidbody.linearVelocity = value; }
        public bool UseGravity { get => rigidbody.useGravity; set => rigidbody.useGravity = value; }
        public void AddForce(Vector3 force, ForceMode mode = ForceMode.Force) => rigidbody.AddForce(force, mode);
        public float Damping { get => rigidbody.linearDamping; set => rigidbody.linearDamping = value; }

        public Vector3 Initial { get; set; }
        public bool ReturnToOrigin { get; set; }
        public float ComebackStiffness { get; set; }
        public bool Hovered { get; set; } = false;
        public bool Grabbed { get; set; } = false;
        public bool PartiallyGrabbed { get; set; } = false;
        public float Rigidity { get; set; } = 1f;

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

            var sim = transform.parent.gameObject.GetComponent<SpringSimulator>();
            if (sim) parentSimulator = sim;
        }

        void FixedUpdate()
        {
            if (ReturnToOrigin)
            {
                var k = ComebackStiffness;
                var force = k * (Initial - Position);
                AddForce(force);
            }
        }

        Color transparent = new(0, 0, 0, 0);
        void Update()
        {
            // THIS ASSUMES A SPECIFIC SHADER!
            if (Grabbed)
                mesh.material.color = Color.red;
            else if (PartiallyGrabbed)
                mesh.material.color = Color.magenta;
            else if (Hovered)
                mesh.material.color = Color.yellow;
            else
                mesh.material.color = transparent;
        }

        public void OnHovered() => Hovered = true;
        public void OnUnhovered() => Hovered = false;
        public void OnGrabbed()
        {
            Grabbed = true;
            parentSimulator?.OnMassGrabbed(this);
        }
        public void OnUngrabbed()
        {
            Grabbed = false;
            parentSimulator?.OnMassUngrabbed(this);
        }

        public void PartialGrab()
        {
            PartiallyGrabbed = true;
        }
        public void PartialUngrab()
        {
            PartiallyGrabbed = false;
        }
    }
}
