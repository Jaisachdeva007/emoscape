using System;
using System.Threading.Tasks;
using EmoScape.Bootstrap;
using EmoScape.Networking;
using EmoScape.Shared;
using EmoScape.UI.Room;
using UnityEngine;

namespace EmoScape.Room
{
    /// <summary>
    /// Root of Room.unity. Builds the whole scene procedurally (camera, lights, avatar,
    /// ambient/reactive VFX, HUD) and runs the mic/chat/TTS session flow — direct port
    /// of frontend/room.html's top-level script.
    /// </summary>
    public class RoomSceneController : MonoBehaviour
    {
        Camera cam;
        Light key, fill, rim;
        readonly RoomState state = new();

        AvatarLoader avatarLoader;
        HeadTrackingController headTracking;
        ReactiveVfxController vfx;
        MiniBackgroundSpline miniSpline;
        MicRecorder mic;
        RoomAudioPlayback audioPlayback;
        RoomHudBuilder hud;

        int turnCount;
        float sessionStartTime;

        async void Start()
        {
            BuildCamera();
            BuildLighting();

            var vfxGo = new GameObject("ReactiveVfx");
            vfxGo.transform.SetParent(transform, false);
            vfx = vfxGo.AddComponent<ReactiveVfxController>();
            vfx.State = state;
            vfx.Key = key; vfx.Fill = fill; vfx.Rim = rim;
            vfx.Build();

            var avatarGo = new GameObject("AvatarRoot");
            avatarGo.transform.SetParent(transform, false);
            avatarLoader = avatarGo.AddComponent<AvatarLoader>();
            headTracking = avatarGo.AddComponent<HeadTrackingController>();
            headTracking.CameraTransform = cam.transform;
            _ = avatarLoader.LoadAsync().ContinueWith(_ => headTracking.HeadBone = avatarLoader.HeadBone, TaskScheduler.FromCurrentSynchronizationContext());

            var miniSplineGo = new GameObject("MiniBackgroundSpline");
            miniSplineGo.transform.SetParent(transform, false);
            miniSpline = miniSplineGo.AddComponent<MiniBackgroundSpline>();
            _ = LoadMiniSplineAsync();

            mic = gameObject.AddComponent<MicRecorder>();
            audioPlayback = gameObject.AddComponent<RoomAudioPlayback>();

            var hudGo = new GameObject("RoomHud");
            hudGo.transform.SetParent(transform, false);
            hud = hudGo.AddComponent<RoomHudBuilder>();
            hud.Build(cam.transform);
            WireHud();
            hud.SetStatus("Loading avatar…");

            var gateGo = new GameObject("StartGate");
            gateGo.transform.SetParent(transform, false);
            gateGo.AddComponent<StartGateController>().Build(BeginSession);
        }

        void BuildCamera()
        {
            var camGo = new GameObject("Main Camera");
            cam = camGo.AddComponent<Camera>();
            cam.tag = "MainCamera";
            camGo.AddComponent<AudioListener>();
            cam.fieldOfView = 42f;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 50f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = SessionColorUtil.HexColor(0x06060f);
            camGo.transform.position = new Vector3(0, 1.5f, 3.0f);
            camGo.transform.LookAt(new Vector3(0, 0.9f, 0));

            PostProcessingBootstrapper.EnablePostProcessingOn(cam);
            FogSetup.Apply(SessionColorUtil.HexColor(0x04040c), 0.025f);
        }

        void BuildLighting()
        {
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = SessionColorUtil.HexColor(0x6655aa, 0.35f);

            key = CreateDirectionalLight("KeyLight", 0xfff5e4, 3.5f, new Vector3(2, 4, 3), shadows: true);
            fill = CreateDirectionalLight("FillLight", 0x8888ff, 1.5f, new Vector3(-3, 2, 2), shadows: false);
            rim = CreatePointLight("RimLight", 0xa78bfa, 3f, 6f, new Vector3(0, 3.5f, -1.5f));
            CreatePointLight("FrontLight", 0xffffff, 2f, 5f, new Vector3(0, 1.5f, 3.5f));
        }

        Light CreateDirectionalLight(string name, int hexColor, float intensity, Vector3 pos, bool shadows)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            go.transform.position = pos;
            go.transform.LookAt(Vector3.zero);
            var light = go.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = SessionColorUtil.HexColor(hexColor);
            light.intensity = intensity;
            light.shadows = shadows ? LightShadows.Soft : LightShadows.None;
            return light;
        }

        Light CreatePointLight(string name, int hexColor, float intensity, float range, Vector3 pos)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            go.transform.position = pos;
            var light = go.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = SessionColorUtil.HexColor(hexColor);
            light.intensity = intensity;
            light.range = range;
            return light;
        }

        async Task LoadMiniSplineAsync()
        {
            try
            {
                var sessions = await ApiClient.Instance.GetSessionsAsync();
                miniSpline.Build(sessions);
            }
            catch (Exception e) { Debug.Log($"No background spline: {e.Message}"); }
        }

        void WireHud()
        {
            hud.MicButton.onClick.AddListener(ToggleMic);
            hud.SendButton.onClick.AddListener(() => _ = SendTypedAsync());
            hud.EndButton.onClick.AddListener(() => _ = EndSessionAsync());
            hud.TypeInput.onEndEdit.AddListener(value =>
            {
                if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
                    _ = SendTypedAsync();
            });
        }

        async void BeginSession()
        {
            mic.RequestMicPermission();
            await StartSessionAsync();
        }

        async Task StartSessionAsync()
        {
            try
            {
                var resp = await ApiClient.Instance.StartSessionAsync();
                SessionManager.Instance.BeginSession(resp.session_id);
                sessionStartTime = Time.realtimeSinceStartup;
                turnCount = 0;
                await AgentSpeakAsync("Hey, good to see you. How's your day been going?");
            }
            catch (Exception e)
            {
                hud.SetStatus($"Server error — is the backend running? ({e.Message})");
            }
        }

        void ToggleMic()
        {
            if (state.IsSpeaking) return;
            if (state.IsListening) _ = StopListeningAsync();
            else StartListening();
        }

        void StartListening()
        {
            if (!mic.HasMicPermission())
            {
                mic.RequestMicPermission();
                hud.SetStatus("Waiting for microphone permission…");
                return;
            }
            state.IsListening = true;
            hud.SetMicState(true, false);
            hud.SetStatus("Listening… speak now");
            mic.StartRecording();
        }

        async Task StopListeningAsync()
        {
            state.IsListening = false;
            hud.SetMicState(false, false);
            hud.SetStatus("Transcribing…");
            string text = await mic.StopAndTranscribeAsync();
            if (!string.IsNullOrWhiteSpace(text))
                await SendMessageAsync(text);
            else
                hud.SetStatus("Didn't catch that — try again or type below");
        }

        async Task SendTypedAsync()
        {
            string text = hud.TypeInput.text?.Trim();
            if (string.IsNullOrEmpty(text)) return;
            hud.TypeInput.text = "";
            await SendMessageAsync(text);
        }

        async Task SendMessageAsync(string text)
        {
            var sm = SessionManager.Instance;
            if (sm.CurrentSessionId == null) { hud.SetStatus("Session not ready — restarting…"); await StartSessionAsync(); return; }

            hud.AddTranscriptLine("user", text);
            turnCount++;
            hud.SetStatus("Thinking…");
            try
            {
                var resp = await ApiClient.Instance.PostChatAsync(sm.CurrentSessionId.Value, text);
                await AgentSpeakAsync(resp.reply);
            }
            catch (Exception e)
            {
                hud.SetStatus($"Error: {e.Message} — is server running?");
            }
        }

        async Task AgentSpeakAsync(string text)
        {
            hud.AddTranscriptLine("agent", text);
            state.IsSpeaking = true;
            hud.SetMicState(false, true);
            hud.SetStatus("Speaking…");

            await audioPlayback.PlayAsync(text);

            state.IsSpeaking = false;
            hud.SetMicState(false, false);
            hud.SetStatus("Click mic to speak");
        }

        async Task EndSessionAsync()
        {
            var sm = SessionManager.Instance;
            if (sm.CurrentSessionId == null) { AppRoot.GoToLandscape(); return; }

            await AgentSpeakAsync("It was good talking with you. Take care of yourself.");
            float duration = Time.realtimeSinceStartup - sessionStartTime;
            try
            {
                await ApiClient.Instance.EndSessionAsync(sm.CurrentSessionId.Value, duration, turnCount);
            }
            catch (Exception e) { Debug.LogWarning($"EndSession failed: {e.Message}"); }
            sm.EndSession();
            AppRoot.GoToLandscape();
        }
    }
}
