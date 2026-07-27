using UnityEngine;

namespace EmoScape.Shared
{
    public struct EmotionInfo
    {
        public string Label;
        public Color Background;
        public Color Border;
        public Color Text;
    }

    /// <summary>Direct port of sessionColor()/emotionInfo() from frontend/index.html.</summary>
    public static class SessionColorUtil
    {
        static readonly Color NegHigh = HexColor(0xef4444); // red - stressed/dark
        static readonly Color NegLow = HexColor(0x4338ca);  // indigo - depleted
        static readonly Color PosHigh = HexColor(0xf59e0b); // amber - energized
        static readonly Color PosLow = HexColor(0x06b6d4);  // cyan - peaceful

        public static Color GetColor(float valence, float arousal)
        {
            float v = (valence + 1f) / 2f; // 0=negative, 1=positive
            float negativePull = Mathf.Max(0f, 1f - v * 2f);
            float a = Mathf.Min(1f, arousal + negativePull * 0.7f);

            Color negBlend = Color.Lerp(NegLow, NegHigh, a);
            Color posBlend = Color.Lerp(PosLow, PosHigh, a);
            return Color.Lerp(negBlend, posBlend, v);
        }

        public static EmotionInfo GetEmotionInfo(float v, float a)
        {
            if (v < -0.3f && a > 0.5f)
                return new EmotionInfo { Label = "Stressed", Background = HexColor(0xef4444, 0.15f), Border = HexColor(0xef4444, 0.3f), Text = HexColor(0xfca5a5) };
            if (v < -0.3f && a <= 0.5f)
                return new EmotionInfo { Label = "Depleted", Background = HexColor(0x4338ca, 0.15f), Border = HexColor(0x4338ca, 0.3f), Text = HexColor(0xa5b4fc) };
            if (v >= -0.3f && v <= 0.3f)
                return new EmotionInfo { Label = "Neutral", Background = HexColor(0x8b5cf6, 0.12f), Border = HexColor(0x8b5cf6, 0.25f), Text = HexColor(0xc4b5fd) };
            if (v > 0.3f && a > 0.5f)
                return new EmotionInfo { Label = "Energized", Background = HexColor(0xf59e0b, 0.12f), Border = HexColor(0xf59e0b, 0.3f), Text = HexColor(0xfcd34d) };
            return new EmotionInfo { Label = "Peaceful", Background = HexColor(0x06b6d4, 0.12f), Border = HexColor(0x06b6d4, 0.3f), Text = HexColor(0x67e8f9) };
        }

        public static Color HexColor(int hex, float alpha = 1f)
        {
            float r = ((hex >> 16) & 0xFF) / 255f;
            float g = ((hex >> 8) & 0xFF) / 255f;
            float b = (hex & 0xFF) / 255f;
            return new Color(r, g, b, alpha);
        }
    }
}
