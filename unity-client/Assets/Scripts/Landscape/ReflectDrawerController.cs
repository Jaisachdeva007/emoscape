using System;
using System.Threading.Tasks;
using EmoScape.Bootstrap;
using EmoScape.Networking;
using UnityEngine;
using UnityEngine.UIElements;

namespace EmoScape.Landscape
{
    /// <summary>Ports the right-side "Reflect" chat drawer (startSession/sendMessage/endSession) from frontend/index.html.</summary>
    public class ReflectDrawerController
    {
        readonly VisualElement panel;
        readonly ScrollView chatArea;
        readonly TextField textInput;
        readonly Action onSessionEnded;

        public ReflectDrawerController(VisualElement root, Action onSessionEnded)
        {
            this.onSessionEnded = onSessionEnded;
            panel = root.Q<VisualElement>("reflect-panel");
            chatArea = root.Q<ScrollView>("chat-area");
            textInput = root.Q<TextField>("text-input");

            root.Q<Button>("close-panel").clicked += Close;
            root.Q<Button>("btn-send").clicked += () => _ = SendMessageAsync();
            root.Q<Button>("btn-end").clicked += () => _ = EndSessionAsync();

            textInput.RegisterCallback<KeyDownEvent>(evt =>
            {
                if (evt.keyCode == KeyCode.Return && !evt.shiftKey)
                {
                    evt.StopPropagation();
                    _ = SendMessageAsync();
                }
            });
        }

        public void Open()
        {
            panel.AddToClassList("reflect-panel--open");
            if (SessionManager.Instance.CurrentSessionId == null)
                _ = StartSessionAsync();
        }

        public void Close() => panel.RemoveFromClassList("reflect-panel--open");

        async Task StartSessionAsync()
        {
            try
            {
                var resp = await ApiClient.Instance.StartSessionAsync();
                SessionManager.Instance.BeginSession(resp.session_id);
            }
            catch (Exception e) { Debug.LogWarning($"StartSession failed: {e.Message}"); }
        }

        async Task SendMessageAsync()
        {
            string text = textInput.value?.Trim();
            var sm = SessionManager.Instance;
            if (string.IsNullOrEmpty(text) || sm.CurrentSessionId == null) return;

            AddMessage("user", text);
            textInput.value = "";
            sm.RegisterTurn();
            var thinking = AddMessage("agent", "…", thinking: true);

            try
            {
                var resp = await ApiClient.Instance.PostChatAsync(sm.CurrentSessionId.Value, text);
                chatArea.Remove(thinking);
                AddMessage("agent", resp.reply);
            }
            catch (Exception e)
            {
                chatArea.Remove(thinking);
                AddMessage("agent", "Still here. Try again.");
                Debug.LogWarning($"Chat failed: {e.Message}");
            }
        }

        async Task EndSessionAsync()
        {
            var sm = SessionManager.Instance;
            if (sm.CurrentSessionId == null) return;
            try
            {
                await ApiClient.Instance.EndSessionAsync(sm.CurrentSessionId.Value, sm.ElapsedSeconds, sm.TurnCount);
                sm.EndSession();
                AddMessage("agent", "Session saved. Your landscape has been updated. See you next time.");
                onSessionEnded?.Invoke();
            }
            catch (Exception e) { Debug.LogWarning($"EndSession failed: {e.Message}"); }
        }

        VisualElement AddMessage(string speaker, string text, bool thinking = false)
        {
            var msg = new Label(text);
            msg.AddToClassList("msg");
            msg.AddToClassList(speaker == "user" ? "msg--user" : "msg--agent");
            if (thinking) msg.AddToClassList("msg--thinking");
            chatArea.Add(msg);
            chatArea.scrollOffset = new Vector2(0, float.MaxValue);
            return msg;
        }
    }
}
