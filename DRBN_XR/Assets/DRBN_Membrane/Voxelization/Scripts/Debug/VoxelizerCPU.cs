using UnityEngine;

class VoxelizerCPU
{
    public Mesh mesh;
    public Bounds voxelBound;
    public Bounds meshBound;

    public ((Vector3, Vector3, Vector3) vertices, (Vector3, Vector3, Vector3) normals) GetTriangle(int idx)
    {
        var vertices = mesh.vertices;
        var normals = mesh.normals;
        return ((
            vertices[mesh.triangles[idx * 3 + 0]],
            vertices[mesh.triangles[idx * 3 + 1]],
            vertices[mesh.triangles[idx * 3 + 2]]
        ), (
            normals[mesh.triangles[idx * 3 + 0]],
            normals[mesh.triangles[idx * 3 + 1]],
            normals[mesh.triangles[idx * 3 + 2]]
        ));
    }

    public float Dot2(Vector3 v) => Vector3.Dot(v, v);

    public float TriangleDistance(Vector3 position, (Vector3, Vector3, Vector3) triangle)
    {
        Vector3 a = triangle.Item1;
        Vector3 b = triangle.Item2;
        Vector3 c = triangle.Item3;

        Vector3 ba = b - a;
        Vector3 pa = position - a;
        Vector3 cb = c - b;
        Vector3 pb = position - b;
        Vector3 ac = a - c;
        Vector3 pc = position - c;
        Vector3 nor = Vector3.Cross(ba, ac);

        float s1 = Mathf.Sign(Vector3.Dot(Vector3.Cross(ba, nor), pa));
        float s2 = Mathf.Sign(Vector3.Dot(Vector3.Cross(cb, nor), pb));
        float s3 = Mathf.Sign(Vector3.Dot(Vector3.Cross(ac, nor), pc));

        float d;
        if (s1 + s2 + s3 < 2.0f)
        {
            float d1 = Dot2(ba * Mathf.Clamp(Vector3.Dot(ba, pa) / Dot2(ba), 0.0f, 1.0f) - pa);
            float d2 = Dot2(cb * Mathf.Clamp(Vector3.Dot(cb, pb) / Dot2(cb), 0.0f, 1.0f) - pb);
            float d3 = Dot2(ac * Mathf.Clamp(Vector3.Dot(ac, pc) / Dot2(ac), 0.0f, 1.0f) - pc);
            d = Mathf.Min(Mathf.Min(d1, d2), d3);
        }
        else
        {
            float nDotPa = Vector3.Dot(nor, pa);
            d = (nDotPa * nDotPa) / (Dot2(nor) + Mathf.Epsilon);
        }

        return Mathf.Sqrt(d);
    }

    public Vector3 TriangleNormal(
        (Vector3, Vector3, Vector3) positions,
        (Vector3, Vector3, Vector3) normals,
        Vector3 mVertex)
    {
        Vector3 v0 = positions.Item2 - positions.Item1;
        Vector3 v1 = positions.Item3 - positions.Item1;
        Vector3 v2 = mVertex - positions.Item1;

        float d00 = Vector3.Dot(v0, v0);
        float d01 = Vector3.Dot(v0, v1);
        float d11 = Vector3.Dot(v1, v1);
        float d20 = Vector3.Dot(v2, v0);
        float d21 = Vector3.Dot(v2, v1);

        float denom = d00 * d11 - d01 * d01;
        if (denom == 0f)
            return (normals.Item1 + normals.Item2 + normals.Item3).normalized;

        float v = (d11 * d20 - d01 * d21) / denom;
        float w = (d00 * d21 - d01 * d20) / denom;
        float u = 1.0f - v - w;

        Vector3 normal = (u * normals.Item1 + v * normals.Item2 + w * normals.Item3).normalized;
        return normal;
    }

    public Vector3 ProjectionOnTriangle((Vector3, Vector3, Vector3) vertices, Vector3 position)
    {
        Vector3 v0 = vertices.Item1;
        Vector3 v1 = vertices.Item2;
        Vector3 v2 = vertices.Item3;
        Vector3 triNormal = Vector3.Normalize(Vector3.Cross(v1 - v0, v2 - v0));
        float d = Vector3.Dot(triNormal, v0);
        float t = (d - Vector3.Dot(triNormal, position)) / Vector3.Dot(triNormal, triNormal);
        Vector3 proj = position + triNormal * t;
        return proj;
    }

    public Vector3 ClosestOnTriangle((Vector3, Vector3, Vector3) triangle, Vector3 position)
    {
        Vector3 a = triangle.Item1;
        Vector3 b = triangle.Item2;
        Vector3 c = triangle.Item3;

        // Compute vectors
        Vector3 ab = b - a;
        Vector3 ac = c - a;
        Vector3 ap = position - a;

        float d1 = Vector3.Dot(ab, ap);
        float d2 = Vector3.Dot(ac, ap);

        if (d1 <= 0f && d2 <= 0f)
            return a; // barycentric coordinates (1,0,0)

        // Check if P in vertex region outside B
        Vector3 bp = position - b;
        float d3 = Vector3.Dot(ab, bp);
        float d4 = Vector3.Dot(ac, bp);
        if (d3 >= 0f && d4 <= d3)
            return b; // barycentric coordinates (0,1,0)

        // Check if P in edge region of AB, if so return projection of P onto AB
        float vc = d1 * d4 - d3 * d2;
        if (vc <= 0f && d1 >= 0f && d3 <= 0f)
        {
            float v = d1 / (d1 - d3);
            return a + v * ab; // barycentric coordinates (1-v, v, 0)
        }

        // Check if P in vertex region outside C
        Vector3 cp = position - c;
        float d5 = Vector3.Dot(ab, cp);
        float d6 = Vector3.Dot(ac, cp);
        if (d6 >= 0f && d5 <= d6)
            return c; // barycentric coordinates (0,0,1)

        // Check if P in edge region of AC, if so return projection of P onto AC
        float vb = d5 * d2 - d1 * d6;
        if (vb <= 0f && d2 >= 0f && d6 <= 0f)
        {
            float w = d2 / (d2 - d6);
            return a + w * ac; // barycentric coordinates (1-w, 0, w)
        }

        // Check if P in edge region of BC, if so return projection of P onto BC
        float va = d3 * d6 - d5 * d4;
        if (va <= 0f && (d4 - d3) >= 0f && (d5 - d6) >= 0f)
        {
            float w = (d4 - d3) / ((d4 - d3) + (d5 - d6));
            return b + w * (c - b); // barycentric coordinates (0, 1-w, w)
        }

        // P inside face region. Compute Q through its barycentric coordinates (u,v,w)
        float denom = 1f / (va + vb + vc);
        float vFinal = vb * denom;
        float wFinal = vc * denom;
        return a + ab * vFinal + ac * wFinal;
    }

    // Returns (signed distance, normal) for the given position
    public (float, Vector3) VoxelizeAtPosition(Vector3 position)
    {
        var debug = VoxelizeAtPositionDebug(position);
        return (debug.signedDistance, debug.normal);
    }

    public struct VoxelizeDebugResult
    {
        public float signedDistance;
        public Vector3 normal;
        public (Vector3, Vector3, Vector3) vertices;
        public (Vector3, Vector3, Vector3) normals;
        public Vector3 projectedPoint;
        public Vector3 projectedNormal;
    }

    // Debug version: returns detailed info in a struct
    public VoxelizeDebugResult VoxelizeAtPositionDebug(Vector3 position)
    {
        float minUDist = float.MaxValue;
        float minSDist = 0.0f;
        Vector3 usedNormal = Vector3.forward;
        (Vector3, Vector3, Vector3) closestVertices = (Vector3.zero, Vector3.zero, Vector3.zero);
        (Vector3, Vector3, Vector3) closestNormals = (Vector3.zero, Vector3.zero, Vector3.zero);
        Vector3 projectedPoint = Vector3.zero;
        Vector3 projectedNormal = Vector3.zero;

        int triangleCount = mesh.triangles.Length / 3;
        for (int i = 0; i < triangleCount; i++)
        {
            var (vertices, normals) = GetTriangle(i);
            float uDist = TriangleDistance(position, vertices);

            Vector3 barycenter = (vertices.Item1 + vertices.Item2 + vertices.Item3) / 3f;
            float baryDist = (position - barycenter).sqrMagnitude;

            if (uDist < minUDist)
            {
                minUDist = uDist;
                Vector3 mVertex = ClosestOnTriangle(vertices, position);
                Vector3 mNormal = TriangleNormal(vertices, normals, mVertex);
                float signVal = Mathf.Sign(Vector3.Dot(mNormal, position - mVertex));
                if (signVal == 0f) signVal = 1f;
                minSDist = uDist * -signVal;
                usedNormal = mNormal;
                closestVertices = vertices;
                closestNormals = normals;
                projectedPoint = mVertex;
                projectedNormal = mNormal;
            }
        }

        return new VoxelizeDebugResult
        {
            signedDistance = minSDist,
            normal = usedNormal,
            vertices = closestVertices,
            normals = closestNormals,
            projectedPoint = projectedPoint,
            projectedNormal = projectedNormal,
        };
    }
};
