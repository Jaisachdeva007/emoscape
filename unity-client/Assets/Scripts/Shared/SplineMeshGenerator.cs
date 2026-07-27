using System.Collections.Generic;
using UnityEngine;

namespace EmoScape.Shared
{
    /// <summary>
    /// Ports THREE.TubeGeometry: sweeps a circular cross-section along a path using a
    /// rotation-minimizing (parallel-transport) frame so the tube doesn't twist at
    /// curvature inflections, matching three.js's computeFrenetFrames behavior.
    /// </summary>
    public static class SplineMeshGenerator
    {
        public static Mesh BuildTube(Vector3[] pathPoints, float radius, int radialSegments)
        {
            int n = pathPoints.Length;
            var mesh = new Mesh();
            if (n < 2) return mesh;

            var tangents = new Vector3[n];
            for (int i = 0; i < n; i++)
            {
                if (i == 0) tangents[i] = (pathPoints[1] - pathPoints[0]).normalized;
                else if (i == n - 1) tangents[i] = (pathPoints[n - 1] - pathPoints[n - 2]).normalized;
                else tangents[i] = (pathPoints[i + 1] - pathPoints[i - 1]).normalized;
            }

            var normals = new Vector3[n];
            var binormals = new Vector3[n];

            Vector3 t0 = tangents[0];
            Vector3 arbitrary =
                Mathf.Abs(t0.x) <= Mathf.Abs(t0.y) && Mathf.Abs(t0.x) <= Mathf.Abs(t0.z) ? Vector3.right :
                Mathf.Abs(t0.y) <= Mathf.Abs(t0.z) ? Vector3.up : Vector3.forward;
            Vector3 normal0 = Vector3.Cross(t0, arbitrary);
            if (normal0.sqrMagnitude < 1e-6f) normal0 = Vector3.Cross(t0, Vector3.up);
            normal0.Normalize();
            normals[0] = normal0;
            binormals[0] = Vector3.Cross(tangents[0], normals[0]).normalized;

            for (int i = 1; i < n; i++)
            {
                Vector3 prevN = normals[i - 1];
                Vector3 prevT = tangents[i - 1];
                Vector3 curT = tangents[i];

                Vector3 axis = Vector3.Cross(prevT, curT);
                Vector3 n2;
                if (axis.sqrMagnitude < 1e-9f)
                {
                    n2 = prevN;
                }
                else
                {
                    axis.Normalize();
                    float angle = Mathf.Acos(Mathf.Clamp(Vector3.Dot(prevT, curT), -1f, 1f));
                    n2 = Quaternion.AngleAxis(angle * Mathf.Rad2Deg, axis) * prevN;
                }
                // re-orthogonalize against the actual tangent to prevent drift accumulating
                n2 = (n2 - curT * Vector3.Dot(n2, curT));
                if (n2.sqrMagnitude < 1e-9f) n2 = prevN;
                n2.Normalize();

                normals[i] = n2;
                binormals[i] = Vector3.Cross(curT, n2).normalized;
            }

            var verts = new List<Vector3>(n * radialSegments);
            var norms = new List<Vector3>(n * radialSegments);
            var uvs = new List<Vector2>(n * radialSegments);
            var tris = new List<int>((n - 1) * radialSegments * 6);

            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < radialSegments; j++)
                {
                    float angle = j / (float)radialSegments * Mathf.PI * 2f;
                    Vector3 dir = normals[i] * Mathf.Cos(angle) + binormals[i] * Mathf.Sin(angle);
                    verts.Add(pathPoints[i] + dir * radius);
                    norms.Add(dir);
                    uvs.Add(new Vector2(i / (float)(n - 1), j / (float)radialSegments));
                }
            }

            for (int i = 0; i < n - 1; i++)
            {
                for (int j = 0; j < radialSegments; j++)
                {
                    int a = i * radialSegments + j;
                    int b = i * radialSegments + (j + 1) % radialSegments;
                    int c = (i + 1) * radialSegments + (j + 1) % radialSegments;
                    int d = (i + 1) * radialSegments + j;
                    tris.Add(a); tris.Add(d); tris.Add(c);
                    tris.Add(a); tris.Add(c); tris.Add(b);
                }
            }

            mesh.SetVertices(verts);
            mesh.SetNormals(norms);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
