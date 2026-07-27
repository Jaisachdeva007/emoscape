using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace EmoScape.UI.Room
{
    /// <summary>
    /// Ports room.html's pre-session "Enter VR / Use in Browser" gate overlay. Unity
    /// doesn't need the browser-autoplay workaround the original used this for, but
    /// mic recording still needs an explicit user gesture before requesting the
    /// RECORD_AUDIO permission on Android, so the gate serves that purpose here.
    /// </summary>
    public class StartGateController : MonoBehaviour
    {
        public void Build(Action onBegin)
        {
            if (EventSystem.current == null)
                new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

            var canvasGo = new GameObject("StartGateCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);
            canvasGo.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;

            var bgGo = new GameObject("Background", typeof(RectTransform), typeof(Image));
            bgGo.transform.SetParent(canvasGo.transform, false);
            var bgRect = bgGo.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero; bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero; bgRect.offsetMax = Vector2.zero;
            bgGo.GetComponent<Image>().color = new Color(0.016f, 0.016f, 0.055f, 0.95f);

            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            AddLabel(canvasGo.transform, font, "EmoScape", 0.6f, 36, new Color(0.88f, 0.83f, 1f));
            AddLabel(canvasGo.transform, font, "Your reflective companion is ready.", 0.52f, 18, new Color(0.44f, 0.44f, 0.66f));

            var btnGo = new GameObject("BeginButton", typeof(RectTransform), typeof(Image), typeof(Button));
            btnGo.transform.SetParent(canvasGo.transform, false);
            var btnRect = btnGo.GetComponent<RectTransform>();
            btnRect.anchorMin = new Vector2(0.5f, 0.4f); btnRect.anchorMax = new Vector2(0.5f, 0.4f);
            btnRect.sizeDelta = new Vector2(240, 64);
            btnGo.GetComponent<Image>().color = new Color(0.45f, 0.36f, 0.96f);
            var btn = btnGo.GetComponent<Button>();

            var btnTextGo = new GameObject("Label", typeof(RectTransform), typeof(Text));
            btnTextGo.transform.SetParent(btnGo.transform, false);
            var btnTextRect = btnTextGo.GetComponent<RectTransform>();
            btnTextRect.anchorMin = Vector2.zero; btnTextRect.anchorMax = Vector2.one;
            btnTextRect.offsetMin = Vector2.zero; btnTextRect.offsetMax = Vector2.zero;
            var btnText = btnTextGo.GetComponent<Text>();
            btnText.font = font; btnText.fontSize = 22; btnText.alignment = TextAnchor.MiddleCenter;
            btnText.color = Color.white; btnText.text = "Begin";

            btn.onClick.AddListener(() =>
            {
                Destroy(canvasGo);
                onBegin?.Invoke();
            });
        }

        static void AddLabel(Transform parent, Font font, string text, float anchorY, int fontSize, Color color)
        {
            var go = new GameObject("Label", typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, anchorY); rect.anchorMax = new Vector2(0.5f, anchorY);
            rect.sizeDelta = new Vector2(700, 60);
            var label = go.GetComponent<Text>();
            label.font = font; label.fontSize = fontSize; label.alignment = TextAnchor.MiddleCenter;
            label.color = color; label.text = text;
        }
    }
}
