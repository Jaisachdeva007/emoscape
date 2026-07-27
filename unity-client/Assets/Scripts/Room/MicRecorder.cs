using System.Threading.Tasks;
using EmoScape.Networking;
using UnityEngine;
#if UNITY_ANDROID
using UnityEngine.Android;
#endif

namespace EmoScape.Room
{
    /// <summary>
    /// Replaces the browser's Web Speech API (frontend/room.html's setupRecognition()) —
    /// Unity has no equivalent, so this records raw mic audio and posts it to the new
    /// backend /stt endpoint (Whisper) added specifically for this port.
    /// </summary>
    public class MicRecorder : MonoBehaviour
    {
        const int SampleRate = 16000;
        const int MaxSeconds = 30;
        AudioClip clip;
        public bool IsRecording { get; private set; }

        public bool HasMicPermission()
        {
#if UNITY_ANDROID
            return Permission.HasUserAuthorizedPermission(Permission.Microphone);
#else
            return true;
#endif
        }

        public void RequestMicPermission()
        {
#if UNITY_ANDROID
            if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
                Permission.RequestUserPermission(Permission.Microphone);
#endif
        }

        public void StartRecording()
        {
            if (Microphone.devices.Length == 0)
            {
                Debug.LogWarning("No microphone device found.");
                return;
            }
            clip = Microphone.Start(null, false, MaxSeconds, SampleRate);
            IsRecording = true;
        }

        public async Task<string> StopAndTranscribeAsync()
        {
            if (!IsRecording) return "";
            int position = Microphone.GetPosition(null);
            Microphone.End(null);
            IsRecording = false;

            if (clip == null || position <= 0) return "";

            var samples = new float[position * clip.channels];
            clip.GetData(samples, 0);
            byte[] wav = WavUtility.FromSamples(samples, clip.channels, clip.frequency);

            try
            {
                return await ApiClient.Instance.PostSttAsync(wav);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"STT request failed: {e.Message}");
                return "";
            }
        }
    }
}
