using System.Collections.Generic;
using EmoScape.Shared;
using UnityEngine;

namespace EmoScape.Landscape
{
    /// <summary>Ports the nebula glow plane, depth layers, grid, and floating decorative orbs from frontend/index.html.</summary>
    public class NebulaBackgroundBuilder : MonoBehaviour
    {
        MeshRenderer mainNebulaRenderer;
        Color mainNebulaColor;
        readonly List<(Transform t, float offset, Color color)> orbs = new();

        static readonly (int col, float y, float opacity, float rotZ)[] Layers =
        {
            (0x1a0f3d, -5f, 0.055f, 0.35f),
            (0x3d1070, -11f, 0.042f, 0.8f),
            (0x0d1a42, -3f, 0.038f, 1.4f),
            (0x1e0838, -14f, 0.028f, 0.6f),
        };

        public void Build()
        {
            mainNebulaColor = SessionColorUtil.HexColor(0x2d1654);
            var mainGo = CreateLayer("NebulaMain", 200f, mainNebulaColor, 0.08f, -8f, 0f);
            mainNebulaRenderer = mainGo.GetComponent<MeshRenderer>();

            foreach (var (col, y, opacity, rotZ) in Layers)
                CreateLayer("NebulaLayer", 280f, SessionColorUtil.HexColor(col), opacity, y, rotZ);

            BuildGrid();
            BuildFloatingOrbs();
        }

        GameObject CreateLayer(string name, float size, Color color, float opacity, float y, float rotZRad)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            go.transform.position = new Vector3(0, y, 0);
            go.transform.rotation = Quaternion.Euler(0, 0, rotZRad * Mathf.Rad2Deg);

            var mf = go.AddComponent<MeshFilter>();
            mf.mesh = ProceduralMeshUtil.CreatePlane(size, size);
            var mr = go.AddComponent<MeshRenderer>();
            mr.material = URPMaterialFactory.CreateUnlitTransparent(doubleSided: true);
            URPMaterialFactory.ApplyColor(mr, color, opacity);
            return go;
        }

        void BuildGrid()
        {
            var go = new GameObject("Grid");
            go.transform.SetParent(transform, false);
            go.transform.position = new Vector3(0, -8f, 0);

            var mf = go.AddComponent<MeshFilter>();
            mf.mesh = BuildGridMesh(120f, 50);
            var mr = go.AddComponent<MeshRenderer>();
            mr.material = URPMaterialFactory.CreateUnlitTransparent(doubleSided: true);
            URPMaterialFactory.ApplyColor(mr, SessionColorUtil.HexColor(0x1a0f3d), 0.5f);
        }

        static Mesh BuildGridMesh(float size, int divisions)
        {
            float half = size / 2f;
            float step = size / divisions;
            var verts = new List<Vector3>();
            var indices = new List<int>();

            for (int i = 0; i <= divisions; i++)
            {
                float pos = -half + i * step;
                verts.Add(new Vector3(-half, 0, pos)); verts.Add(new Vector3(half, 0, pos));
                indices.Add(verts.Count - 2); indices.Add(verts.Count - 1);
                verts.Add(new Vector3(pos, 0, -half)); verts.Add(new Vector3(pos, 0, half));
                indices.Add(verts.Count - 2); indices.Add(verts.Count - 1);
            }

            var mesh = new Mesh();
            mesh.SetVertices(verts);
            mesh.SetIndices(indices.ToArray(), MeshTopology.Lines, 0);
            mesh.RecalculateBounds();
            return mesh;
        }

        void BuildFloatingOrbs()
        {
            var group = new GameObject("FloatingOrbs");
            group.transform.SetParent(transform, false);
            for (int i = 0; i < 12; i++)
            {
                float r = 0.08f + Random.value * 0.15f;
                var orb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                orb.name = $"Orb_{i}";
                orb.transform.SetParent(group.transform, false);
                Object.Destroy(orb.GetComponent<Collider>());
                orb.transform.localScale = Vector3.one * r * 2f;
                orb.transform.position = new Vector3((Random.value - 0.5f) * 60f, (Random.value - 0.5f) * 20f, (Random.value - 0.5f) * 40f);

                var color = ColorUtil.HSL(0.7f + Random.value * 0.15f, 0.8f, 0.6f);
                var mr = orb.GetComponent<MeshRenderer>();
                mr.material = URPMaterialFactory.CreateUnlitTransparent(doubleSided: false);
                URPMaterialFactory.ApplyColor(mr, color, 0.4f);

                orbs.Add((orb.transform, Random.value * Mathf.PI * 2f, color));
            }
        }

        void Update()
        {
            float t = Time.time;
            if (mainNebulaRenderer != null)
            {
                float opacity = 0.06f + Mathf.Sin(t * 0.3f) * 0.03f;
                URPMaterialFactory.ApplyColor(mainNebulaRenderer, mainNebulaColor, opacity);
            }

            foreach (var (orbTransform, offset, color) in orbs)
            {
                var pos = orbTransform.position;
                pos.y += Mathf.Sin(t * 0.8f + offset) * 0.005f;
                orbTransform.position = pos;

                var mr = orbTransform.GetComponent<MeshRenderer>();
                float opacity = 0.3f + Mathf.Sin(t * 1.2f + offset) * 0.15f;
                URPMaterialFactory.ApplyColor(mr, color, opacity);
            }
        }
    }
}
