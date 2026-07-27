using System.Collections.Generic;
using EmoScape.Networking;
using EmoScape.Shared;
using UnityEngine;

namespace EmoScape.Room
{
    /// <summary>Ports loadBackgroundSpline() from frontend/room.html: a small floating copy of the emotional spline behind the avatar.</summary>
    public class MiniBackgroundSpline : MonoBehaviour
    {
        struct FadeEntry { public MeshRenderer renderer; public Color color; public float baseOpacity; }
        readonly List<FadeEntry> fading = new();
        float t;

        public void Build(List<SessionDto> sessions)
        {
            foreach (Transform child in transform) Destroy(child.gameObject);
            fading.Clear();
            if (sessions == null || sessions.Count < 2) return;

            int n = sessions.Count;
            var pts = new Vector3[n];
            for (int i = 0; i < n; i++)
                pts[i] = new Vector3((i / (float)Mathf.Max(n - 1, 1)) * 10f - 5f, sessions[i].valence * 2.2f, 0f);

            for (int i = 0; i < n - 1; i++)
            {
                float tStart = i / (float)(n - 1);
                float tEnd = (i + 1) / (float)(n - 1);
                var segPts = CatmullRomSpline3D.SampleRange(pts, tStart, tEnd, 20);

                Color col = SessionColorUtil.GetColor(sessions[i].valence, sessions[i].arousal);
                float thick = 0.05f + sessions[i].intensity * 0.1f;

                AddFadingTube(segPts, thick, col, 0.8f);
                AddFadingTube(segPts, thick + 0.18f, col, 0.12f);
            }

            for (int i = 0; i < n; i++)
            {
                Color col = SessionColorUtil.GetColor(sessions[i].valence, sessions[i].arousal);

                var node = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                node.transform.SetParent(transform, false);
                node.transform.position = pts[i];
                node.transform.localScale = Vector3.one * 0.13f * 2f;
                Destroy(node.GetComponent<Collider>());
                var nr = node.GetComponent<MeshRenderer>();
                nr.material = URPMaterialFactory.CreateUnlitTransparent(doubleSided: false);
                URPMaterialFactory.ApplyColor(nr, col, 0.95f);
                fading.Add(new FadeEntry { renderer = nr, color = col, baseOpacity = 0.95f });

                var halo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                halo.transform.SetParent(transform, false);
                halo.transform.position = pts[i];
                halo.transform.localScale = Vector3.one * 0.26f * 2f;
                Destroy(halo.GetComponent<Collider>());
                var hr = halo.GetComponent<MeshRenderer>();
                hr.material = URPMaterialFactory.CreateUnlitTransparent(doubleSided: false);
                hr.material.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Front);
                URPMaterialFactory.ApplyColor(hr, col, 0.14f);
                fading.Add(new FadeEntry { renderer = hr, color = col, baseOpacity = 0.14f });
            }
        }

        void AddFadingTube(Vector3[] segPts, float radius, Color col, float baseOpacity)
        {
            var go = new GameObject("MiniTube");
            go.transform.SetParent(transform, false);
            var mf = go.AddComponent<MeshFilter>();
            mf.mesh = SplineMeshGenerator.BuildTube(segPts, radius, 8);
            var mr = go.AddComponent<MeshRenderer>();
            mr.material = URPMaterialFactory.CreateUnlitTransparent();
            URPMaterialFactory.ApplyColor(mr, col, baseOpacity);
            fading.Add(new FadeEntry { renderer = mr, color = col, baseOpacity = baseOpacity });
        }

        void Update()
        {
            if (fading.Count == 0) return;
            t += Time.deltaTime;

            transform.localPosition = new Vector3(0, 1.35f + Mathf.Sin(t * 0.28f) * 0.06f, -2.6f);
            transform.localRotation = Quaternion.Euler(0, Mathf.Sin(t * 0.12f) * 0.1f * Mathf.Rad2Deg, 0);
            transform.localScale = Vector3.one * 0.52f;

            float factor = 0.75f + Mathf.Sin(t * 0.5f) * 0.25f;
            foreach (var e in fading)
                URPMaterialFactory.ApplyColor(e.renderer, e.color, e.baseOpacity * factor);
        }
    }
}
