using UnityEngine;

namespace EmoScape.Shared
{
    /// <summary>Direct port of THREE.FogExp2(color, density) used in both scenes.</summary>
    public static class FogSetup
    {
        public static void Apply(Color color, float density)
        {
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = color;
            RenderSettings.fogDensity = density;
        }
    }
}
