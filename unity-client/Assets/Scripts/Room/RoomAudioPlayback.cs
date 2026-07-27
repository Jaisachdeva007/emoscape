using System.Threading.Tasks;
using EmoScape.Networking;
using UnityEngine;

namespace EmoScape.Room
{
    /// <summary>
    /// Ports agentSpeak()'s TTS playback from frontend/room.html. Unlike the browser
    /// (which falls back to window.speechSynthesis if edge-tts fails), Unity has no
    /// built-in cross-platform TTS API — if the /tts fetch fails there is no fallback
    /// voice, the reply still appears in the transcript via RoomSceneController.
    /// </summary>
    public class RoomAudioPlayback : MonoBehaviour
    {
        AudioSource audioSource;

        void Awake() => audioSource = gameObject.AddComponent<AudioSource>();

        public async Task PlayAsync(string text)
        {
            AudioClip clip = null;
            try
            {
                clip = await ApiClient.Instance.GetTtsClipAsync(text);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"TTS fetch failed: {e.Message}");
                return;
            }
            if (clip == null) return;

            audioSource.clip = clip;
            audioSource.Play();
            while (audioSource.isPlaying) await Task.Yield();
        }
    }
}
