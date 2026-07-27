using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace EmoScape.Shared
{
    /// <summary>
    /// Builds the global Bloom/Vignette post-processing volume entirely at runtime as an
    /// in-memory VolumeProfile ScriptableObject. Deliberately NOT a hand-authored .asset —
    /// see unity-client/SETUP.md for why (fragile cross-file GUID links with no Editor to verify them).
    /// </summary>
    public class PostProcessingBootstrapper : MonoBehaviour
    {
        void Awake()
        {
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();

            var bloom = profile.Add<Bloom>(true);
            bloom.threshold.Override(0.6f);
            bloom.intensity.Override(1.2f);
            bloom.scatter.Override(0.7f);

            var vignette = profile.Add<Vignette>(true);
            vignette.intensity.Override(0.35f);
            vignette.smoothness.Override(0.4f);

            var volume = gameObject.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 0;
            volume.profile = profile;
        }

        /// <summary>Call once per created camera — URP post-processing is a per-camera opt-in flag.</summary>
        public static void EnablePostProcessingOn(Camera cam)
        {
            var data = cam.GetUniversalAdditionalCameraData();
            if (data != null) data.renderPostProcessing = true;
        }
    }
}
