using EmoScape.Shared;
using UnityEngine;

namespace EmoScape.Landscape
{
    /// <summary>Ports the ambient particle field + close-star field (THREE.Points) from frontend/index.html.</summary>
    public class ParticleFieldBuilder : MonoBehaviour
    {
        ParticleSystem ambientField;

        public void BuildAmbientField(int count, Vector3 boxSize, float hueBase, float hueRange, float sat, float lightBase, float lightRange, float sizeMin, float sizeRange)
        {
            var go = new GameObject("AmbientParticles");
            go.transform.SetParent(transform, false);
            var ps = go.AddComponent<ParticleSystem>();
            ConfigureStatic(ps, count);

            var particles = new ParticleSystem.Particle[count];
            for (int i = 0; i < count; i++)
            {
                particles[i].position = RandomInBox(boxSize);
                particles[i].startSize = sizeMin + Random.value * sizeRange;
                particles[i].startColor = ColorUtil.HSL(hueBase + Random.value * hueRange, sat, lightBase + Random.value * lightRange);
                particles[i].remainingLifetime = Mathf.Infinity;
                particles[i].startLifetime = Mathf.Infinity;
            }
            ps.SetParticles(particles, count);
            FinishParticleSystem(ps);
            ambientField = ps;
        }

        public void BuildStarField(int count, Vector3 boxSize, Color color, float size, float opacity)
        {
            var go = new GameObject("StarField");
            go.transform.SetParent(transform, false);
            var ps = go.AddComponent<ParticleSystem>();
            ConfigureStatic(ps, count);

            var c = color; c.a = opacity;
            var particles = new ParticleSystem.Particle[count];
            for (int i = 0; i < count; i++)
            {
                particles[i].position = RandomInBox(boxSize);
                particles[i].startSize = size;
                particles[i].startColor = c;
                particles[i].remainingLifetime = Mathf.Infinity;
                particles[i].startLifetime = Mathf.Infinity;
            }
            ps.SetParticles(particles, count);
            FinishParticleSystem(ps);
        }

        static Vector3 RandomInBox(Vector3 boxSize) => new Vector3(
            (Random.value - 0.5f) * boxSize.x,
            (Random.value - 0.5f) * boxSize.y,
            (Random.value - 0.5f) * boxSize.z);

        static void ConfigureStatic(ParticleSystem ps, int count)
        {
            var main = ps.main;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.maxParticles = count;
            main.startLifetime = Mathf.Infinity;
            main.loop = false;
            main.playOnAwake = false;
            var emission = ps.emission;
            emission.enabled = false;
        }

        static void FinishParticleSystem(ParticleSystem ps)
        {
            ps.Stop();
            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.material = URPMaterialFactory.CreateUnlitParticleMaterial();
        }

        void Update()
        {
            if (ambientField == null) return;
            float t = Time.time;
            // Direct port of particles.rotation.y = t*0.04; particles.rotation.x = sin(t*0.02)*0.05
            ambientField.transform.localRotation = Quaternion.Euler(Mathf.Sin(t * 0.02f) * 0.05f * Mathf.Rad2Deg, t * 0.04f * Mathf.Rad2Deg, 0f);
        }
    }
}
