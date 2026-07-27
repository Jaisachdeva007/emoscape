using UnityEngine;

namespace EmoScape.Shared
{
    public static class ColorUtil
    {
        /// <summary>Matches THREE.Color.setHSL(h,s,l) exactly (standard HSL, not Unity's HSV-based Color.HSVToRGB).</summary>
        public static Color HSL(float h, float s, float l, float alpha = 1f)
        {
            h = Mathf.Repeat(h, 1f);
            s = Mathf.Clamp01(s);
            l = Mathf.Clamp01(l);
            if (s <= 0f) return new Color(l, l, l, alpha);

            float q = l < 0.5f ? l * (1f + s) : l + s - l * s;
            float p = 2f * l - q;
            float r = HueToRgb(p, q, h + 1f / 3f);
            float g = HueToRgb(p, q, h);
            float b = HueToRgb(p, q, h - 1f / 3f);
            return new Color(r, g, b, alpha);
        }

        static float HueToRgb(float p, float q, float t)
        {
            if (t < 0f) t += 1f;
            if (t > 1f) t -= 1f;
            if (t < 1f / 6f) return p + (q - p) * 6f * t;
            if (t < 1f / 2f) return q;
            if (t < 2f / 3f) return p + (q - p) * (2f / 3f - t) * 6f;
            return p;
        }
    }
}
