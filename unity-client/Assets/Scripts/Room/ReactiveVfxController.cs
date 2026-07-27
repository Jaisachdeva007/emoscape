using System.Collections.Generic;
using EmoScape.Shared;
using UnityEngine;

namespace EmoScape.Room
{
    /// <summary>
    /// Builds and animates all ambient/reactive decoration from frontend/room.html:
    /// floor, starfield, dust, aurora, ground rings, light shaft, background orbs,
    /// the speaking aura ring, orbiting companion orbs, the voice wave arc, the
    /// reactive uplight, and speaking ripples. Reads State.IsSpeaking/IsListening
    /// (set by RoomAudioPlayback/MicRecorder) to drive the same reactive look.
    /// </summary>
    public class ReactiveVfxController : MonoBehaviour
    {
        public RoomState State;
        public Light Key, Fill, Rim;

        MeshRenderer glowRenderer;
        readonly Color glowColor = SessionColorUtil.HexColor(0x7c3aed);

        Light upLight;
        MeshRenderer auraRingRenderer, auraGlowRenderer;
        Transform auraRingT, auraGlowT;

        readonly List<(Transform t, MeshRenderer mr, Color color, float phase)> orbitOrbs = new();
        LineRenderer waveLine1, waveLine2;
        const int WaveN = 52;

        readonly List<(MeshRenderer mr, Color color, float baseOpacity, int index)> groundRings = new();
        readonly List<(MeshRenderer mr, Color color)> auroraPlanes = new();
        static readonly float[] AuroraBaseOpacity = { 0.012f, 0.008f, 0.005f };

        MeshRenderer shaftRenderer;
        readonly Color shaftColor = SessionColorUtil.HexColor(0x7c3aed);

        readonly List<(Transform t, MeshRenderer mr, Color color, float offset)> bgOrbs = new();
        readonly List<(Transform t, MeshRenderer mr, float scale, float opacity)> ripples = new();
        float rippleTimer;
        float t;

        public void Build()
        {
            BuildFloor();
            BuildStarfieldAndDust();
            BuildAurora();
            BuildGroundRings();
            BuildShaft();
            BuildBgOrbs();
            BuildAura();
            BuildOrbitOrbs();
            BuildWaveArc();
            BuildUpLight();
        }

        void BuildFloor()
        {
            var floorGo = new GameObject("Floor");
            floorGo.transform.SetParent(transform, false);
            var mf = floorGo.AddComponent<MeshFilter>();
            mf.mesh = ProceduralMeshUtil.CreatePlane(40, 40);
            var mr = floorGo.AddComponent<MeshRenderer>();
            mr.material = URPMaterialFactory.CreateUnlitTransparent(doubleSided: false);
            URPMaterialFactory.ApplyColor(mr, SessionColorUtil.HexColor(0x020210), 1f);

            var gridGo = new GameObject("Grid");
            gridGo.transform.SetParent(transform, false);
            gridGo.transform.position = new Vector3(0, 0.002f, 0);
            var gmf = gridGo.AddComponent<MeshFilter>();
            gmf.mesh = BuildGridMesh(40f, 40);
            var gmr = gridGo.AddComponent<MeshRenderer>();
            gmr.material = URPMaterialFactory.CreateUnlitTransparent(doubleSided: true);
            URPMaterialFactory.ApplyColor(gmr, SessionColorUtil.HexColor(0x1a0f3d), 0.6f);

            var glowGo = new GameObject("GroundGlow");
            glowGo.transform.SetParent(transform, false);
            glowGo.transform.position = new Vector3(0, 0.005f, 0);
            var glmf = glowGo.AddComponent<MeshFilter>();
            glmf.mesh = ProceduralMeshUtil.CreateRing(0f, 1.1f, 64);
            glowRenderer = glowGo.AddComponent<MeshRenderer>();
            glowRenderer.material = URPMaterialFactory.CreateUnlitTransparent(doubleSided: true);
            URPMaterialFactory.ApplyColor(glowRenderer, glowColor, 0.22f);
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

        void BuildStarfieldAndDust()
        {
            BuildParticleField("Starfield", 2400, i =>
            {
                float th = Random.value * Mathf.PI * 2f;
                float ph = Mathf.Acos(2f * Random.value - 1f);
                float r = 7f + Random.value * 8f;
                return (new Vector3(
                    r * Mathf.Sin(ph) * Mathf.Cos(th),
                    Mathf.Abs(r * Mathf.Sin(ph) * Mathf.Sin(th)) * 0.6f + 0.5f,
                    r * Mathf.Cos(ph)), SessionColorUtil.HexColor(0xccbbff, 0.9f), 0.05f);
            });

            BuildParticleField("Dust", 2400, i =>
            {
                var pos = new Vector3((Random.value - 0.5f) * 20f, Random.value * 8f, (Random.value - 0.5f) * 20f);
                var color = ColorUtil.HSL(0.67f + Random.value * 0.16f, 0.9f, 0.5f + Random.value * 0.25f, 0.7f);
                return (pos, color, 0.028f);
            });
        }

        void BuildParticleField(string name, int count, System.Func<int, (Vector3 pos, Color col, float size)> gen)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.maxParticles = count;
            main.startLifetime = Mathf.Infinity;
            main.loop = false;
            main.playOnAwake = false;
            var emission = ps.emission;
            emission.enabled = false;

            var particles = new ParticleSystem.Particle[count];
            for (int i = 0; i < count; i++)
            {
                var (pos, col, size) = gen(i);
                particles[i].position = pos;
                particles[i].startColor = col;
                particles[i].startSize = size;
                particles[i].remainingLifetime = Mathf.Infinity;
                particles[i].startLifetime = Mathf.Infinity;
            }
            ps.SetParticles(particles, count);
            ps.Stop();
            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.material = URPMaterialFactory.CreateUnlitParticleMaterial();
        }

        void BuildAurora()
        {
            var layers = new (int col, float y, float opacity, float rotIndex)[]
            {
                (0x4f46e5, 0.9f, 0.08f, 0f),
                (0x6d28d9, 2.8f, 0.055f, 1f),
                (0x1d4ed8, 5.2f, 0.035f, 2f),
            };
            foreach (var (col, y, opacity, idx) in layers)
            {
                var go = new GameObject("Aurora");
                go.transform.SetParent(transform, false);
                go.transform.position = new Vector3(0, y, 0);
                go.transform.rotation = Quaternion.Euler(0, 0, idx * 0.75f * Mathf.Rad2Deg);
                var mf = go.AddComponent<MeshFilter>();
                mf.mesh = ProceduralMeshUtil.CreatePlane(30, 30);
                var mr = go.AddComponent<MeshRenderer>();
                mr.material = URPMaterialFactory.CreateUnlitTransparent(doubleSided: true);
                var color = SessionColorUtil.HexColor(col);
                URPMaterialFactory.ApplyColor(mr, color, opacity);
                auroraPlanes.Add((mr, color));
            }
        }

        void BuildGroundRings()
        {
            float[] radii = { 0.55f, 1.1f, 1.85f, 2.85f, 4.2f };
            for (int i = 0; i < radii.Length; i++)
            {
                var go = new GameObject($"GroundRing_{i}");
                go.transform.SetParent(transform, false);
                go.transform.position = new Vector3(0, 0.004f + i * 0.001f, 0);
                var mf = go.AddComponent<MeshFilter>();
                mf.mesh = ProceduralMeshUtil.CreateRing(radii[i] - 0.02f, radii[i] + 0.02f, 64);
                var mr = go.AddComponent<MeshRenderer>();
                mr.material = URPMaterialFactory.CreateUnlitTransparent(doubleSided: true);
                var color = SessionColorUtil.HexColor(i < 2 ? 0x9b72ff : 0x5b4aee);
                float baseOpacity = Mathf.Max(0.06f, 0.32f - i * 0.05f);
                URPMaterialFactory.ApplyColor(mr, color, baseOpacity);
                groundRings.Add((mr, color, baseOpacity, i));
            }
        }

        void BuildShaft()
        {
            var go = new GameObject("LightShaft");
            go.transform.SetParent(transform, false);
            go.transform.position = new Vector3(0, 5, 0);
            var mf = go.AddComponent<MeshFilter>();
            mf.mesh = ProceduralMeshUtil.CreateConeShell(0.22f, 1.2f, 10f, 32);
            shaftRenderer = go.AddComponent<MeshRenderer>();
            shaftRenderer.material = URPMaterialFactory.CreateUnlitTransparent(doubleSided: true);
            URPMaterialFactory.ApplyColor(shaftRenderer, shaftColor, 0.032f);
        }

        void BuildBgOrbs()
        {
            for (int i = 0; i < 16; i++)
            {
                var orb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                orb.name = $"BgOrb_{i}";
                orb.transform.SetParent(transform, false);
                Destroy(orb.GetComponent<Collider>());
                float r = 0.022f + Random.value * 0.05f;
                orb.transform.localScale = Vector3.one * r * 2f;
                orb.transform.position = new Vector3((Random.value - 0.5f) * 13f, 0.3f + Random.value * 5f, -0.5f + (Random.value - 0.5f) * 10f);

                var color = ColorUtil.HSL(0.69f + Random.value * 0.16f, 1f, 0.65f);
                var mr = orb.GetComponent<MeshRenderer>();
                mr.material = URPMaterialFactory.CreateUnlitTransparent(doubleSided: false);
                float opacity = 0.45f + Random.value * 0.35f;
                URPMaterialFactory.ApplyColor(mr, color, opacity);
                bgOrbs.Add((orb.transform, mr, color, Random.value * Mathf.PI * 2f));
            }
        }

        void BuildAura()
        {
            var ringGo = new GameObject("AuraRing");
            ringGo.transform.SetParent(transform, false);
            ringGo.transform.position = new Vector3(0, 0.01f, 0);
            var rmf = ringGo.AddComponent<MeshFilter>();
            rmf.mesh = ProceduralMeshUtil.CreateRing(0.52f, 0.6f, 64);
            auraRingRenderer = ringGo.AddComponent<MeshRenderer>();
            auraRingRenderer.material = URPMaterialFactory.CreateUnlitTransparent(doubleSided: true);
            URPMaterialFactory.ApplyColor(auraRingRenderer, SessionColorUtil.HexColor(0x8b5cf6), 0f);
            auraRingT = ringGo.transform;

            var glowGo = new GameObject("AuraGlow");
            glowGo.transform.SetParent(transform, false);
            glowGo.transform.position = new Vector3(0, 0.008f, 0);
            var gmf = glowGo.AddComponent<MeshFilter>();
            gmf.mesh = ProceduralMeshUtil.CreateRing(0.38f, 0.88f, 64);
            auraGlowRenderer = glowGo.AddComponent<MeshRenderer>();
            auraGlowRenderer.material = URPMaterialFactory.CreateUnlitTransparent(doubleSided: true);
            URPMaterialFactory.ApplyColor(auraGlowRenderer, SessionColorUtil.HexColor(0x6d28d9), 0f);
            auraGlowT = glowGo.transform;
        }

        void BuildOrbitOrbs()
        {
            int[] colors = { 0xbb44ff, 0x00eeff, 0xff44cc };
            for (int i = 0; i < colors.Length; i++)
            {
                var orb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                orb.name = $"OrbitOrb_{i}";
                orb.transform.SetParent(transform, false);
                Destroy(orb.GetComponent<Collider>());
                orb.transform.localScale = Vector3.one * 0.075f * 2f;
                var color = SessionColorUtil.HexColor(colors[i]);
                var mr = orb.GetComponent<MeshRenderer>();
                mr.material = URPMaterialFactory.CreateUnlitTransparent(doubleSided: false);
                URPMaterialFactory.ApplyColor(mr, color, 0.95f);

                var halo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                halo.transform.SetParent(orb.transform, false);
                Destroy(halo.GetComponent<Collider>());
                halo.transform.localScale = Vector3.one * (0.16f / 0.075f); // scales with parent orb automatically
                var hr = halo.GetComponent<MeshRenderer>();
                hr.material = URPMaterialFactory.CreateUnlitTransparent(doubleSided: false);
                hr.material.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Front);
                URPMaterialFactory.ApplyColor(hr, color, 0.2f);

                orbitOrbs.Add((orb.transform, mr, color, (i / 3f) * Mathf.PI * 2f));
            }
        }

        void BuildWaveArc()
        {
            waveLine1 = CreateWaveLine("WaveArc1");
            waveLine2 = CreateWaveLine("WaveArc2");
        }

        LineRenderer CreateWaveLine(string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var lr = go.AddComponent<LineRenderer>();
            lr.positionCount = WaveN;
            lr.useWorldSpace = false;
            lr.startWidth = lr.endWidth = 0.012f;
            lr.material = URPMaterialFactory.CreateUnlitTransparent();
            lr.startColor = lr.endColor = new Color(0, 0, 0, 0);
            for (int i = 0; i < WaveN; i++)
                lr.SetPosition(i, new Vector3((i / (float)(WaveN - 1) - 0.5f) * 1.5f, 2.22f, 0f));
            return lr;
        }

        void BuildUpLight()
        {
            var go = new GameObject("UpLight");
            go.transform.SetParent(transform, false);
            go.transform.position = new Vector3(0, 0.2f, 0.6f);
            upLight = go.AddComponent<Light>();
            upLight.type = LightType.Point;
            upLight.color = SessionColorUtil.HexColor(0x8b5cf6);
            upLight.intensity = 0f;
            upLight.range = 4f;
        }

        void Update()
        {
            t += Time.deltaTime;
            bool speaking = State != null && State.IsSpeaking;
            bool listening = State != null && State.IsListening;

            if (glowRenderer != null)
            {
                float op = 0.05f + Mathf.Sin(t * 1.4f) * 0.025f + (speaking ? 0.1f : 0f) + (listening ? 0.04f : 0f);
                URPMaterialFactory.ApplyColor(glowRenderer, glowColor, op);
            }

            if (Rim != null) Rim.intensity = 3f + Mathf.Sin(t * 0.9f) * 0.5f + (speaking ? Mathf.Sin(t * 7f) * 1.6f : 0f);
            if (Key != null) Key.intensity = 3.5f + Mathf.Sin(t * 0.5f) * 0.35f;
            if (Fill != null) Fill.intensity = 1.5f + Mathf.Sin(t * 0.38f) * 0.4f;

            if (upLight != null)
            {
                float target = speaking ? 2.2f + Mathf.Sin(t * 6f) * 0.9f : listening ? 0.6f : 0f;
                upLight.intensity += (target - upLight.intensity) * 0.1f;
                upLight.color = ColorUtil.HSL(listening ? 0.55f : 0.75f, 1f, 0.5f);
            }

            UpdateAura(speaking, listening);
            UpdateOrbitOrbs(speaking);
            float waveOpTarget = speaking ? 0.8f : (listening ? 0.35f : 0f);
            UpdateWave(waveLine1, waveOpTarget, speaking, mirror: false);
            UpdateWave(waveLine2, waveOpTarget * 0.55f, speaking, mirror: true);
            UpdateAurora();
            UpdateGroundRings(speaking);

            if (shaftRenderer != null)
            {
                float op = 0.026f + Mathf.Sin(t * 0.52f) * 0.01f + (speaking ? 0.03f : 0f);
                URPMaterialFactory.ApplyColor(shaftRenderer, shaftColor, op);
            }

            UpdateBgOrbs();

            if (speaking)
            {
                rippleTimer += Time.deltaTime;
                if (rippleTimer > 0.52f) { rippleTimer = 0f; SpawnRipple(); }
            }
            UpdateRipples();
        }

        void UpdateAura(bool speaking, bool listening)
        {
            if (auraRingRenderer == null) return;
            var mpb = new MaterialPropertyBlock();
            auraRingRenderer.GetPropertyBlock(mpb);
            float curOpacity = mpb.GetColor("_BaseColor").a;
            float auraTarget = speaking ? 0.42f + Mathf.Sin(t * 9f) * 0.18f : (listening ? 0.1f : 0f);
            float newOpacity = curOpacity + (auraTarget - curOpacity) * 0.09f;
            URPMaterialFactory.ApplyColor(auraRingRenderer, SessionColorUtil.HexColor(0x8b5cf6), newOpacity);
            URPMaterialFactory.ApplyColor(auraGlowRenderer, SessionColorUtil.HexColor(0x6d28d9), newOpacity * 0.42f);
            float scale = 1f + (speaking ? Mathf.Sin(t * 5f) * 0.07f : 0f);
            auraRingT.localScale = Vector3.one * scale;
            auraGlowT.localScale = Vector3.one * scale;
        }

        void UpdateOrbitOrbs(bool speaking)
        {
            float orbSpeed = speaking ? 2.4f : 0.7f;
            foreach (var (orbT, mr, color, phase) in orbitOrbs)
            {
                float ang = t * orbSpeed + phase;
                orbT.localPosition = new Vector3(Mathf.Cos(ang) * 0.75f, 1.2f + Mathf.Sin(t * 0.8f + phase) * 0.18f, Mathf.Sin(ang) * 0.75f);
                float opacity = speaking ? 0.9f + Mathf.Sin(t * 5f + phase) * 0.1f : 0.85f;
                URPMaterialFactory.ApplyColor(mr, color, opacity);
                float s = speaking ? 1.3f + Mathf.Sin(t * 7f + phase) * 0.25f : 1f;
                orbT.localScale = Vector3.one * 0.075f * 2f * s;
            }
        }

        void UpdateWave(LineRenderer lr, float targetOpacity, bool speaking, bool mirror)
        {
            if (lr == null) return;
            float curOpacity = lr.startColor.a;
            float newOpacity = curOpacity + (targetOpacity - curOpacity) * 0.1f;
            var c = lr.startColor; c.a = newOpacity;
            lr.startColor = lr.endColor = c;

            if (newOpacity > 0.02f)
            {
                float amp = speaking ? 0.09f : 0.04f;
                float spd = speaking ? 8f : 4f;
                for (int i = 0; i < WaveN; i++)
                {
                    float x = (i / (float)(WaveN - 1) - 0.5f) * 1.5f;
                    float y = Mathf.Sin(t * spd + x * 13f) * amp + Mathf.Sin(t * spd * 0.6f + x * 8f) * amp * 0.5f;
                    lr.SetPosition(i, new Vector3(x, 2.22f + (mirror ? -y : y), 0f));
                }
            }
        }

        void UpdateAurora()
        {
            for (int i = 0; i < auroraPlanes.Count; i++)
            {
                var (mr, color) = auroraPlanes[i];
                float op = AuroraBaseOpacity[i] + Mathf.Sin(t * 0.42f + i * 1.4f) * 0.003f;
                URPMaterialFactory.ApplyColor(mr, color, op);
                mr.transform.Rotate(0, 0, 0.0132f * Time.deltaTime * Mathf.Rad2Deg, Space.Self);
            }
        }

        void UpdateGroundRings(bool speaking)
        {
            foreach (var (mr, color, baseOpacity, idx) in groundRings)
            {
                float op = baseOpacity + Mathf.Sin(t * 1.15f + idx * 0.95f) * 0.06f + (speaking ? 0.1f : 0f);
                URPMaterialFactory.ApplyColor(mr, color, op);
            }
        }

        void UpdateBgOrbs()
        {
            foreach (var (orbT, mr, color, offset) in bgOrbs)
            {
                var pos = orbT.position;
                pos.y += Mathf.Sin(t * 0.62f + offset) * 0.0022f;
                orbT.position = pos;
                float opacity = 0.3f + Mathf.Sin(t * 1.05f + offset) * 0.22f;
                URPMaterialFactory.ApplyColor(mr, color, opacity);
            }
        }

        void SpawnRipple()
        {
            var go = new GameObject("Ripple");
            go.transform.SetParent(transform, false);
            go.transform.position = new Vector3(0, 0.007f, 0);
            var mf = go.AddComponent<MeshFilter>();
            mf.mesh = ProceduralMeshUtil.CreateRing(0.01f, 0.06f, 48);
            var mr = go.AddComponent<MeshRenderer>();
            mr.material = URPMaterialFactory.CreateUnlitTransparent(doubleSided: true);
            URPMaterialFactory.ApplyColor(mr, SessionColorUtil.HexColor(0x8b5cf6), 0.75f);
            ripples.Add((go.transform, mr, 1f, 0.75f));
        }

        void UpdateRipples()
        {
            for (int i = ripples.Count - 1; i >= 0; i--)
            {
                var (rt, mr, scale, opacity) = ripples[i];
                scale += 0.048f;
                opacity -= 0.024f;
                rt.localScale = Vector3.one * scale;
                URPMaterialFactory.ApplyColor(mr, SessionColorUtil.HexColor(0x8b5cf6), Mathf.Max(0f, opacity));
                if (opacity <= 0f)
                {
                    Destroy(rt.gameObject);
                    ripples.RemoveAt(i);
                }
                else
                {
                    ripples[i] = (rt, mr, scale, opacity);
                }
            }
        }
    }
}
