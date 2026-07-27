using UnityEngine;

namespace EmoScape.Shared
{
    /// <summary>Small procedural mesh builders for shapes THREE.js gets for free (PlaneGeometry, RingGeometry, CylinderGeometry).</summary>
    public static class ProceduralMeshUtil
    {
        public static Mesh CreatePlane(float width, float height)
        {
            float hw = width / 2f, hh = height / 2f;
            var mesh = new Mesh
            {
                vertices = new[]
                {
                    new Vector3(-hw, 0, -hh), new Vector3(hw, 0, -hh),
                    new Vector3(hw, 0, hh), new Vector3(-hw, 0, hh)
                },
                uv = new[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1) },
                triangles = new[] { 0, 2, 1, 0, 3, 2, 0, 1, 2, 0, 2, 3 } // both faces, matches THREE.DoubleSide planes
            };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        public static Mesh CreateRing(float innerRadius, float outerRadius, int segments)
        {
            var verts = new Vector3[(segments + 1) * 2];
            var uvs = new Vector2[verts.Length];
            var tris = new int[segments * 12]; // both faces

            for (int i = 0; i <= segments; i++)
            {
                float angle = i / (float)segments * Mathf.PI * 2f;
                float cos = Mathf.Cos(angle), sin = Mathf.Sin(angle);
                verts[i * 2] = new Vector3(cos * innerRadius, 0, sin * innerRadius);
                verts[i * 2 + 1] = new Vector3(cos * outerRadius, 0, sin * outerRadius);
                uvs[i * 2] = new Vector2(i / (float)segments, 0);
                uvs[i * 2 + 1] = new Vector2(i / (float)segments, 1);
            }

            int t = 0;
            for (int i = 0; i < segments; i++)
            {
                int a = i * 2, b = i * 2 + 1, c = (i + 1) * 2, d = (i + 1) * 2 + 1;
                tris[t++] = a; tris[t++] = c; tris[t++] = b;
                tris[t++] = b; tris[t++] = c; tris[t++] = d;
                tris[t++] = a; tris[t++] = b; tris[t++] = c; // back face
                tris[t++] = b; tris[t++] = d; tris[t++] = c;
            }

            var mesh = new Mesh { vertices = verts, uv = uvs, triangles = tris };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>Open-ended cone/cylinder shell (used for the Room scene's volumetric light shaft).</summary>
        public static Mesh CreateConeShell(float radiusTop, float radiusBottom, float height, int segments)
        {
            var verts = new Vector3[(segments + 1) * 2];
            for (int i = 0; i <= segments; i++)
            {
                float angle = i / (float)segments * Mathf.PI * 2f;
                float cos = Mathf.Cos(angle), sin = Mathf.Sin(angle);
                verts[i] = new Vector3(cos * radiusTop, height / 2f, sin * radiusTop);
                verts[i + segments + 1] = new Vector3(cos * radiusBottom, -height / 2f, sin * radiusBottom);
            }

            var tris = new int[segments * 6];
            int t = 0;
            for (int i = 0; i < segments; i++)
            {
                int a = i, b = i + 1, c = i + segments + 1, d = i + segments + 2;
                tris[t++] = a; tris[t++] = c; tris[t++] = b;
                tris[t++] = b; tris[t++] = c; tris[t++] = d;
            }

            var mesh = new Mesh { vertices = verts, triangles = tris };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
