using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace MarchingCubing.V1
{
    public class SphereColliderPopulateV2 : MonoBehaviour
    {
        public GameObject SpherePrefab;

        private readonly List<GameObject> objectPool = new();

        public void ExtractAndPopulate(
            MeshFilter Populate, Transform withTransform = null)
        {
            // ExtractAndPopulatePerVertex(Populate, withTransform);
            ExtractAndPopulatePerTriangle(Populate, withTransform);
        }

        public void ExtractAndPopulatePerVertex(
            MeshFilter Populate, Transform withTransform = null)
        {
            var vertices = Populate.mesh.vertices;
            var normals = Populate.mesh.normals;
            var vertexCount = Populate.mesh.vertexCount;
            var t = withTransform ? withTransform : transform;
            EnsurePoolSize(vertexCount);
            for (int i = 0; i < vertexCount; i++)
            {
                objectPool[i].SetActive(true);
                objectPool[i].transform.SetPositionAndRotation(
                    t.TransformPoint(vertices[i]),
                    Quaternion.LookRotation(t.TransformDirection(normals[i])));
            }
            for (int i = vertexCount; i < objectPool.Count; i++)
            {
                objectPool[i].SetActive(false);
            }
        }

        public void ExtractAndPopulatePerTriangle(
            MeshFilter Populate, Transform withTransform = null)
        {
            var meshVertices = Populate.mesh.vertices;
            var meshNormals = Populate.mesh.normals;
            var meshTriangles = Populate.mesh.triangles;
            var triangleCount = meshTriangles.Length / 3;
            Vector3[] positions = new Vector3[triangleCount];
            Vector3[] normals = new Vector3[triangleCount];
            EnsurePoolSize(triangleCount);
            Parallel.For(0, triangleCount, i =>
            {
                int index = i * 3;
                positions[i] = (meshVertices[meshTriangles[index]] +
                               meshVertices[meshTriangles[index + 1]] +
                               meshVertices[meshTriangles[index + 2]]) / 3f;
                normals[i] = (meshNormals[meshTriangles[index]] +
                              meshNormals[meshTriangles[index + 1]] +
                              meshNormals[meshTriangles[index + 2]]) / 3f;
            });
            var t = withTransform ? withTransform : transform;
            for (int i = 0; i < triangleCount; i++)
            {
                objectPool[i].SetActive(true);
                objectPool[i].transform.SetPositionAndRotation(
                    t.TransformPoint(positions[i]),
                    Quaternion.LookRotation(t.TransformDirection(normals[i])));
            }
            for (int i = triangleCount; i < objectPool.Count; i++)
            {
                objectPool[i].SetActive(false);
            }
        }

        void EnsurePoolSize(int requiredSize)
        {
            while (objectPool.Count < requiredSize)
            {
                GameObject obj = Instantiate(SpherePrefab, transform);
                obj.SetActive(true);
                objectPool.Add(obj);
            }
        }

    }
}