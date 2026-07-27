using UnityEngine;
using UnityEngine.Rendering;

namespace EmoScape.Shared
{
    /// <summary>
    /// Creates a small set of SHARED transparent/emissive URP materials, and varies
    /// per-object color/emission via MaterialPropertyBlock so no material instances
    /// leak per session-node/tube/etc (mirrors reusing one THREE.MeshPhongMaterial
    /// per archetype in the original code, just varying `.color`/`.emissive` per mesh).
    /// </summary>
    public static class URPMaterialFactory
    {
        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
        static readonly int SurfaceId = Shader.PropertyToID("_Surface");
        static readonly int BlendId = Shader.PropertyToID("_Blend");
        static readonly int SrcBlendId = Shader.PropertyToID("_SrcBlend");
        static readonly int DstBlendId = Shader.PropertyToID("_DstBlend");
        static readonly int ZWriteId = Shader.PropertyToID("_ZWrite");
        static readonly int CullId = Shader.PropertyToID("_Cull");

        public static Material CreateEmissiveTransparentLit(bool doubleSided = false)
        {
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            ConfigureTransparent(mat, doubleSided);
            mat.EnableKeyword("_EMISSION");
            return mat;
        }

        public static Material CreateUnlitTransparent(bool doubleSided = true)
        {
            var mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            ConfigureTransparent(mat, doubleSided);
            return mat;
        }

        static Texture2D softDotTexture;

        public static Material CreateUnlitParticleMaterial()
        {
            var legacyShader = Shader.Find("Particles/Alpha Blended");
            var tex = GetSoftDotTexture();

            if (legacyShader != null)
            {
                var legacyMat = new Material(legacyShader);
                legacyMat.mainTexture = tex;
                if (legacyMat.HasProperty("_TintColor")) legacyMat.SetColor("_TintColor", Color.white);
                return legacyMat;
            }

            var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit")
                         ?? Shader.Find("Particles/Standard Unlit");
            var mat = new Material(shader);
            mat.SetFloat(SurfaceId, 1f);
            mat.SetFloat(BlendId, 0f);
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.EnableKeyword("_ALPHAPREMULTIPLY_ON");
            mat.mainTexture = tex;
            if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", tex);
            if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", tex);
            return mat;
        }

        /// <summary>
        /// A soft radial-falloff dot, generated in code (no imported texture asset) so
        /// particles render as glowing round points instead of Unity's default flat
        /// square billboard.
        /// </summary>
        static Texture2D GetSoftDotTexture()
        {
            if (softDotTexture != null) return softDotTexture;

            const int size = 64;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var center = new Vector2(size / 2f, size / 2f);
            var pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center) / (size / 2f);
                    float alpha = Mathf.Clamp01(1f - dist);
                    alpha = Mathf.Pow(alpha, 1.8f); // soft falloff, bright core
                    pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
                }
            }
            tex.SetPixels(pixels);
            tex.Apply();
            softDotTexture = tex;
            return tex;
        }

        static void ConfigureTransparent(Material mat, bool doubleSided)
        {
            mat.SetFloat(SurfaceId, 1f); // 0=Opaque, 1=Transparent
            mat.SetFloat(BlendId, 0f);   // Alpha blend
            mat.SetOverrideTag("RenderType", "Transparent");
            mat.SetInt(SrcBlendId, (int)BlendMode.SrcAlpha);
            mat.SetInt(DstBlendId, (int)BlendMode.OneMinusSrcAlpha);
            mat.SetInt(ZWriteId, 0);
            if (doubleSided) mat.SetInt(CullId, (int)CullMode.Off);
            mat.renderQueue = (int)RenderQueue.Transparent;
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        }

        /// <summary>Sets per-renderer base color (with alpha) and optional emission via MaterialPropertyBlock.</summary>
        public static void ApplyColor(Renderer renderer, Color baseColor, float alpha, float emissiveIntensity = 0f)
        {
            var mpb = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(mpb);
            var c = baseColor;
            c.a = alpha;
            mpb.SetColor(BaseColorId, c);
            if (emissiveIntensity > 0f)
                mpb.SetColor(EmissionColorId, baseColor * emissiveIntensity);
            renderer.SetPropertyBlock(mpb);
        }
    }
}
