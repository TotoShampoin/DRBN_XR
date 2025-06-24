using System.Threading.Tasks;
using Unity.Profiling;
using UnityEngine;

namespace SpringSim.V3
{
    public class SpringsRenderer : MonoBehaviour
    {
        public Mesh massMesh;
        public Mesh linkMesh;
        public Material material;
        public Material highlightMaterial;
        public float thickness = 0.05f;

        static readonly ProfilerMarker rendering = new("Membrane.SpringSim.Rendering");

        Matrix4x4[] massBuffer;
        Matrix4x4[] linkBuffer;
        public void Render(SpringSimulatorState state, Matrix4x4 localToWorld = default, bool highlight = false)
        {
            if (state == null || state.masses.Count == 0)
                return;
            rendering.Begin();
            if (massBuffer == null || massBuffer.Length != state.masses.Count)
                massBuffer = new Matrix4x4[state.masses.Count];
            if (linkBuffer == null || linkBuffer.Length != state.links.Count)
                linkBuffer = new Matrix4x4[state.links.Count];
            Parallel.For(0, massBuffer.Length, i =>
            {
                var m = state.masses[i];
                massBuffer[i] =
                    localToWorld *
                    Matrix4x4.TRS(
                        m.position,
                        m.normal == Vector3.zero
                            ? Quaternion.identity
                            : Quaternion.LookRotation(m.normal),
                        thickness * Vector3.one
                    );
            });
            Parallel.For(0, linkBuffer.Length, i =>
            {
                var l = state.links[i];
                Vector3 start = state.masses[l.a].position;
                Vector3 end = state.masses[l.b].position;
                var delta = end - start;
                Vector3 middle = (start + end) / 2;
                float length = Vector3.Distance(start, end);
                linkBuffer[i] =
                    localToWorld *
                    Matrix4x4.TRS(
                        middle,
                        delta == Vector3.zero
                            ? Quaternion.identity
                            : Quaternion.LookRotation(delta),
                        new Vector3(thickness / 4f, thickness / 4f, length) / 2f
                    );
            });
            if (highlight)
            {
                Graphics.DrawMeshInstanced(massMesh, 0, highlightMaterial, massBuffer);
                Graphics.DrawMeshInstanced(linkMesh, 0, highlightMaterial, linkBuffer);
            }
            else
            {
                Graphics.DrawMeshInstanced(massMesh, 0, material, massBuffer);
                Graphics.DrawMeshInstanced(linkMesh, 0, material, linkBuffer);
            }
            rendering.End();
        }

    }
}
