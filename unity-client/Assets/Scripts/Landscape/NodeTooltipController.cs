using System;
using EmoScape.Networking;
using UnityEngine;
using UnityEngine.UIElements;

namespace EmoScape.Landscape
{
    /// <summary>Ports the raycast hover tooltip from frontend/index.html.</summary>
    public class NodeTooltipController
    {
        readonly VisualElement tooltip;
        readonly Label themeLabel, dateLabel, summaryLabel, statsLabel;
        readonly Camera cam;

        public NodeTooltipController(VisualElement root, Camera cam)
        {
            this.cam = cam;
            tooltip = root.Q<VisualElement>("tooltip");
            themeLabel = root.Q<Label>("t-theme");
            dateLabel = root.Q<Label>("t-date");
            summaryLabel = root.Q<Label>("t-summary");
            statsLabel = root.Q<Label>("t-stats");
        }

        public void Tick(bool suppressed)
        {
            if (suppressed) { Hide(); return; }

            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out var hit, 500f))
            {
                var nodeRef = hit.collider.GetComponent<SessionNodeRef>();
                if (nodeRef != null) { Show(nodeRef.Session); return; }
            }
            Hide();
        }

        void Show(SessionDto s)
        {
            tooltip.style.display = DisplayStyle.Flex;
            Vector2 mouse = Input.mousePosition;
            float panelY = Screen.height - mouse.y; // UI Toolkit panel space is top-down, Input.mousePosition is bottom-up
            tooltip.style.left = mouse.x + 18;
            tooltip.style.top = panelY - 70;

            themeLabel.text = string.IsNullOrEmpty(s.theme) ? "Reflection" : s.theme;
            dateLabel.text = DateTime.TryParse(s.created_at, null, System.Globalization.DateTimeStyles.RoundtripKind, out var dt)
                ? dt.ToString("dddd, MMM d, h:mm tt") : "";
            summaryLabel.text = s.summary ?? "";
            statsLabel.text = $"valence {(s.valence > 0 ? "+" : "")}{s.valence:F2}  ·  arousal {s.arousal * 100f:F0}%  ·  {s.word_count} words";
        }

        void Hide() => tooltip.style.display = DisplayStyle.None;
    }
}
