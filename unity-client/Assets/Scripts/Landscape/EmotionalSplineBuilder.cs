using System.Collections.Generic;
using EmoScape.Networking;
using EmoScape.Shared;
using UnityEngine;

namespace EmoScape.Landscape
{
    /// <summary>Ports buildSpline() from frontend/index.html: per-segment tubes+glow, nodes+halos+rings, hover pulse.</summary>
    public class EmotionalSplineBuilder : MonoBehaviour
    {
        struct NodeEntry { public Transform transform; public float baseScale; public int index; }

        readonly List<NodeEntry> nodeEntries = new();
        readonly List<Transform> rings = new();
        Camera targetCamera;

        public void SetCamera(Camera cam) => targetCamera = cam;
        public int NodeCount => nodeEntries.Count;

        public void BuildSpline(List<SessionDto> data, float xSpan = 30f, float xOffset = -15f, float yScale = 7f)
        {
            foreach (Transform child in transform) Destroy(child.gameObject);
            nodeEntries.Clear();
            rings.Clear();
            if (data == null || data.Count == 0) return;

            int n = data.Count;
            var pts = new Vector3[n];
            for (int i = 0; i < n; i++)
                pts[i] = new Vector3((i / (float)Mathf.Max(n - 1, 1)) * xSpan + xOffset, data[i].valence * yScale, 0f);

            if (n >= 2)
            {
                for (int i = 0; i < n - 1; i++)
                {
                    float tStart = i / (float)(n - 1);
                    float tEnd = (i + 1) / (float)(n - 1);
                    var segPts = CatmullRomSpline3D.SampleRange(pts, tStart, tEnd, 24);

                    float thick = 0.09f + data[i].intensity * 0.26f;
                    Color col = SessionColorUtil.GetColor(data[i].valence, data[i].arousal);

                    BuildTubeMesh($"Tube_{i}", segPts, thick, col, 1f, 1.7f, glow: false);
                    BuildTubeMesh($"TubeGlow_{i}", segPts, thick + 0.22f, col, 0.3f, 0f, glow: true);
                }
            }

            for (int i = 0; i < n; i++)
                BuildNode(i, data[i], pts[i]);
        }

        void BuildTubeMesh(string name, Vector3[] segPts, float radius, Color col, float alpha, float emissive, bool glow)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var mf = go.AddComponent<MeshFilter>();
            mf.mesh = SplineMeshGenerator.BuildTube(segPts, radius, 8);
            var mr = go.AddComponent<MeshRenderer>();
            mr.material = glow ? URPMaterialFactory.CreateUnlitTransparent() : URPMaterialFactory.CreateEmissiveTransparentLit();
            URPMaterialFactory.ApplyColor(mr, col, alpha, emissive);
        }

        void BuildNode(int i, SessionDto s, Vector3 pos)
        {
            Color col = SessionColorUtil.GetColor(s.valence, s.arousal);
            float size = 0.22f + s.intensity * 0.18f;

            // Core node — kept raycast-able (its default SphereCollider) for hover tooltips
            var node = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            node.name = $"Node_{i}";
            node.transform.SetParent(transform, false);
            node.transform.position = pos;
            node.transform.localScale = Vector3.one * size * 2f; // Unity's primitive sphere has radius 0.5
            var nr = node.GetComponent<MeshRenderer>();
            nr.material = URPMaterialFactory.CreateEmissiveTransparentLit();
            URPMaterialFactory.ApplyColor(nr, col, 1f, 2.4f);
            node.AddComponent<SessionNodeRef>().Session = s;
            nodeEntries.Add(new NodeEntry { transform = node.transform, baseScale = size * 2f, index = i });

            // Glow halo — no collider, back-facing (mirrors THREE.BackSide)
            var halo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            halo.name = $"Halo_{i}";
            halo.transform.SetParent(transform, false);
            halo.transform.position = pos;
            halo.transform.localScale = Vector3.one * (size + 0.25f) * 2f;
            Destroy(halo.GetComponent<Collider>());
            var hr = halo.GetComponent<MeshRenderer>();
            hr.material = URPMaterialFactory.CreateUnlitTransparent(doubleSided: false);
            hr.material.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Front);
            URPMaterialFactory.ApplyColor(hr, col, 0.28f);

            // Outer ring — billboards toward camera each frame
            var ring = new GameObject($"Ring_{i}");
            ring.transform.SetParent(transform, false);
            ring.transform.position = pos;
            var rmf = ring.AddComponent<MeshFilter>();
            rmf.mesh = ProceduralMeshUtil.CreateRing(size + 0.3f, size + 0.4f, 32);
            var rmr = ring.AddComponent<MeshRenderer>();
            rmr.material = URPMaterialFactory.CreateUnlitTransparent(doubleSided: true);
            URPMaterialFactory.ApplyColor(rmr, col, 0.45f);
            rings.Add(ring.transform);
        }

        void Update()
        {
            float t = Time.time;
            if (targetCamera != null)
                foreach (var ring in rings)
                    ring.LookAt(targetCamera.transform.position);

            foreach (var entry in nodeEntries)
            {
                float pulse = 1f + Mathf.Sin(t * 1.6f + entry.index * 0.85f) * 0.07f;
                entry.transform.localScale = Vector3.one * entry.baseScale * pulse;
            }
        }
    }
}
