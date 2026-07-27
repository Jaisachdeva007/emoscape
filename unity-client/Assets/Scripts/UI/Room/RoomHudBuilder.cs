using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;

namespace EmoScape.UI.Room
{
    /// <summary>
    /// Ports the bottom HUD (mic button, transcript, type input, Send/End) from
    /// frontend/room.html as a world-space uGUI Canvas built entirely in code — XR
    /// Interaction Toolkit's VR pointer raycaster (TrackedDeviceGraphicRaycaster)
    /// targets uGUI, not UI Toolkit world-space panels, which is why this scene uses
    /// uGUI instead of the UXML/USS approach used for the Landscape scene's flat UI.
    /// No TextMeshPro: uses the built-in font to avoid an Editor-only import step (see SETUP.md).
    /// </summary>
    public class RoomHudBuilder : MonoBehaviour
    {
        public Button MicButton { get; private set; }
        public Text TranscriptText { get; private set; }
        public InputField TypeInput { get; private set; }
        public Button SendButton { get; private set; }
        public Button EndButton { get; private set; }
        public Text StatusText { get; private set; }

        const int MaxTranscriptLines = 8;
        readonly List<string> lines = new();

        public void Build(Transform anchor)
        {
            EnsureEventSystem();

            var canvasGo = new GameObject("RoomHud", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            // Compute world position/rotation directly from the anchor's actual forward
            // vector rather than a local-space offset — parenting with a local offset
            // compounds through the camera's own tilt (it looks down at the avatar),
            // which pushed this out of frame when computed as a naive local Vector3.
            Vector3 worldPos = anchor.position + anchor.forward * 1.1f + anchor.up * -0.15f;
            canvasGo.transform.SetPositionAndRotation(worldPos, anchor.rotation);
            canvasGo.transform.SetParent(anchor, true);
            canvasGo.transform.localScale = Vector3.one * 0.001f;

            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            var rect = canvasGo.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(900, 260);

            try { canvasGo.AddComponent<TrackedDeviceGraphicRaycaster>(); }
            catch (Exception e) { Debug.LogWarning($"TrackedDeviceGraphicRaycaster unavailable: {e.Message}"); }

            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            TranscriptText = CreateText(canvasGo.transform, font, new Vector2(0, 70), new Vector2(860, 140), 20, TextAnchor.LowerLeft);
            StatusText = CreateText(canvasGo.transform, font, new Vector2(0, -95), new Vector2(860, 24), 16, TextAnchor.MiddleCenter);

            MicButton = CreateButton(canvasGo.transform, font, "MicButton", "Mic", new Vector2(-370, -40), new Vector2(90, 90), new Color(0.39f, 0.4f, 0.95f));
            SendButton = CreateButton(canvasGo.transform, font, "SendButton", "Send", new Vector2(320, -40), new Vector2(120, 60), new Color(0.55f, 0.36f, 0.97f, 0.6f));
            EndButton = CreateButton(canvasGo.transform, font, "EndButton", "End", new Vector2(320, -110), new Vector2(120, 50), new Color(1f, 1f, 1f, 0.06f));

            TypeInput = CreateInputField(canvasGo.transform, font, new Vector2(0, -40), new Vector2(560, 60));
        }

        static void EnsureEventSystem()
        {
            if (EventSystem.current != null) return;
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }

        static Text CreateText(Transform parent, Font font, Vector2 pos, Vector2 size, int fontSize, TextAnchor anchor)
        {
            var go = new GameObject("Text", typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchoredPosition = pos;
            rect.sizeDelta = size;
            var text = go.GetComponent<Text>();
            text.font = font;
            text.fontSize = fontSize;
            text.alignment = anchor;
            text.color = new Color(0.89f, 0.89f, 0.94f);
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            return text;
        }

        static Button CreateButton(Transform parent, Font font, string name, string label, Vector2 pos, Vector2 size, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchoredPosition = pos;
            rect.sizeDelta = size;
            go.GetComponent<Image>().color = color;

            var textGo = new GameObject("Label", typeof(RectTransform), typeof(Text));
            textGo.transform.SetParent(go.transform, false);
            var textRect = textGo.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero; textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero; textRect.offsetMax = Vector2.zero;
            var text = textGo.GetComponent<Text>();
            text.font = font;
            text.text = label;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.fontSize = 22;

            return go.GetComponent<Button>();
        }

        static InputField CreateInputField(Transform parent, Font font, Vector2 pos, Vector2 size)
        {
            var go = new GameObject("TypeInput", typeof(RectTransform), typeof(Image), typeof(InputField));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchoredPosition = pos;
            rect.sizeDelta = size;
            go.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.07f);

            var textGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
            textGo.transform.SetParent(go.transform, false);
            var textRect = textGo.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero; textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(12, 4); textRect.offsetMax = new Vector2(-12, -4);
            var text = textGo.GetComponent<Text>();
            text.font = font;
            text.color = Color.white;
            text.fontSize = 20;
            text.supportRichText = false;

            var placeholderGo = new GameObject("Placeholder", typeof(RectTransform), typeof(Text));
            placeholderGo.transform.SetParent(go.transform, false);
            var pRect = placeholderGo.GetComponent<RectTransform>();
            pRect.anchorMin = Vector2.zero; pRect.anchorMax = Vector2.one;
            pRect.offsetMin = new Vector2(12, 4); pRect.offsetMax = new Vector2(-12, -4);
            var placeholder = placeholderGo.GetComponent<Text>();
            placeholder.font = font;
            placeholder.text = "Or type here and press Enter…";
            placeholder.color = new Color(1f, 1f, 1f, 0.35f);
            placeholder.fontSize = 20;

            var input = go.GetComponent<InputField>();
            input.textComponent = text;
            input.placeholder = placeholder;
            return input;
        }

        public void AddTranscriptLine(string speaker, string text)
        {
            lines.Add($"{(speaker == "agent" ? "✦" : "you ·")} {text}");
            while (lines.Count > MaxTranscriptLines) lines.RemoveAt(0);
            TranscriptText.text = string.Join("\n", lines);
        }

        public void SetStatus(string text) => StatusText.text = text;

        public void SetMicState(bool listening, bool speaking)
        {
            MicButton.GetComponent<Image>().color =
                speaking ? new Color(0.04f, 0.6f, 0.42f) :
                listening ? new Color(0.85f, 0.27f, 0.27f) :
                new Color(0.39f, 0.4f, 0.95f);
        }
    }
}
