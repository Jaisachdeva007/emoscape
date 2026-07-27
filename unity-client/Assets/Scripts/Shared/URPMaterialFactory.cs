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

        public static Material CreateUnlitParticleMaterial()
        {
            var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit")
                         ?? Shader.Find("Particles/Standard Unlit");
            var mat = new Material(shader);
            mat.SetFloat(SurfaceId, 1f);
            mat.SetFloat(BlendId, 0f);
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            return mat;
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
