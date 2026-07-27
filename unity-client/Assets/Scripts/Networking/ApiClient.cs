using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

namespace EmoScape.Networking
{
    /// <summary>
    /// Persistent singleton (created by AppRoot, DontDestroyOnLoad) wrapping every backend call.
    /// Mirrors the web app's `API = window.location.origin` constant — Unity has no page-origin
    /// equivalent, so BaseUrl is a fixed field. Change it in SETUP.md's step 3 for device testing.
    /// </summary>
    public class ApiClient : MonoBehaviour
    {
        public static ApiClient Instance { get; private set; }
        public string BaseUrl = "http://localhost:8000";

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        public async Task<StartSessionResponse> StartSessionAsync()
        {
            using var req = new UnityWebRequest($"{BaseUrl}/session/start", "POST") { downloadHandler = new DownloadHandlerBuffer() };
            await req.SendWebRequest();
            ThrowIfError(req);
            return JsonConvert.DeserializeObject<StartSessionResponse>(req.downloadHandler.text);
        }

        public async Task<ChatResponse> PostChatAsync(int sessionId, string text)
        {
            var body = JsonConvert.SerializeObject(new ChatRequest { session_id = sessionId, text = text });
            using var req = new UnityWebRequest($"{BaseUrl}/session/chat", "POST")
            {
                uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(body)),
                downloadHandler = new DownloadHandlerBuffer()
            };
            req.SetRequestHeader("Content-Type", "application/json");
            await req.SendWebRequest();
            ThrowIfError(req);
            return JsonConvert.DeserializeObject<ChatResponse>(req.downloadHandler.text);
        }

        public async Task<EndSessionResponse> EndSessionAsync(int sessionId, float durationSeconds, int turnCount)
        {
            var body = JsonConvert.SerializeObject(new EndSessionRequest { session_id = sessionId, duration_seconds = durationSeconds, turn_count = turnCount });
            using var req = new UnityWebRequest($"{BaseUrl}/session/end", "POST")
            {
                uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(body)),
                downloadHandler = new DownloadHandlerBuffer()
            };
            req.SetRequestHeader("Content-Type", "application/json");
            await req.SendWebRequest();
            ThrowIfError(req);
            return JsonConvert.DeserializeObject<EndSessionResponse>(req.downloadHandler.text);
        }

        public async Task<List<SessionDto>> GetSessionsAsync()
        {
            using var req = UnityWebRequest.Get($"{BaseUrl}/sessions");
            await req.SendWebRequest();
            ThrowIfError(req);
            return JsonConvert.DeserializeObject<List<SessionDto>>(req.downloadHandler.text);
        }

        public async Task<DaySummaryResponse> GetDaySummaryAsync(string dateKey)
        {
            using var req = UnityWebRequest.Get($"{BaseUrl}/day-summary?date={UnityWebRequest.EscapeURL(dateKey)}");
            await req.SendWebRequest();
            ThrowIfError(req);
            return JsonConvert.DeserializeObject<DaySummaryResponse>(req.downloadHandler.text);
        }

        public async Task<AudioClip> GetTtsClipAsync(string text, string voice = "en-US-JennyNeural")
        {
            string url = $"{BaseUrl}/tts?voice={UnityWebRequest.EscapeURL(voice)}&text={UnityWebRequest.EscapeURL(text)}";
            using var req = UnityWebRequestMultimedia.GetAudioClip(url, AudioType.MPEG);
            await req.SendWebRequest();
            ThrowIfError(req);
            return DownloadHandlerAudioClip.GetContent(req);
        }

        public async Task<string> PostSttAsync(byte[] wavBytes)
        {
            var form = new List<IMultipartFormSection>
            {
                new MultipartFormFileSection("audio", wavBytes, "recording.wav", "audio/wav")
            };
            using var req = UnityWebRequest.Post($"{BaseUrl}/stt", form);
            await req.SendWebRequest();
            ThrowIfError(req);
            var resp = JsonConvert.DeserializeObject<SttResponse>(req.downloadHandler.text);
            return resp?.text ?? "";
        }

        static void ThrowIfError(UnityWebRequest req)
        {
            if (req.result != UnityWebRequest.Result.Success)
                throw new Exception($"{req.method} {req.url} failed: {req.error}");
        }
    }
}
