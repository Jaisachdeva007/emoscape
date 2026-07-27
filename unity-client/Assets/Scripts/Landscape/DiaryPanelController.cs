using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using EmoScape.Networking;
using EmoScape.Shared;
using UnityEngine;
using UnityEngine.UIElements;

namespace EmoScape.Landscape
{
    /// <summary>Ports renderDiary()/fetchDaySummary() from frontend/index.html.</summary>
    public class DiaryPanelController
    {
        readonly VisualElement panel;
        readonly VisualElement entriesContainer;
        readonly Label streakLabel;
        readonly Dictionary<string, string> daySummaryCache = new();

        public DiaryPanelController(VisualElement root)
        {
            panel = root.Q<VisualElement>("diary-panel");
            entriesContainer = root.Q<VisualElement>("diary-entries");
            streakLabel = root.Q<Label>("diary-streak");
            root.Q<Button>("diary-close").clicked += Close;
        }

        public void Open(List<SessionDto> sessions)
        {
            panel.style.display = DisplayStyle.Flex;
            _ = Render(sessions);
        }

        public void Close() => panel.style.display = DisplayStyle.None;

        async Task Render(List<SessionDto> sessions)
        {
            entriesContainer.Clear();
            if (sessions == null || sessions.Count == 0)
            {
                streakLabel.text = "Start your first session";
                entriesContainer.Add(new Label("No reflections yet. Start a session to begin your journal."));
                return;
            }

            int days = sessions.Select(s => ParseDate(s.created_at).Date).Distinct().Count();
            streakLabel.text = $"{days} day{(days != 1 ? "s" : "")} of reflection · {sessions.Count} session{(sessions.Count != 1 ? "s" : "")} total";

            var byDay = sessions
                .OrderByDescending(s => s.created_at)
                .GroupBy(s => ParseDate(s.created_at).ToString("yyyy-MM-dd"))
                .ToList();

            foreach (var group in byDay)
            {
                string dateKey = group.Key;
                var dayDate = ParseDate(group.First().created_at);

                var dayBlock = new VisualElement();
                dayBlock.AddToClassList("diary-day");

                var dayLabel = new Label(dayDate.ToString("dddd, MMMM d, yyyy"));
                dayLabel.AddToClassList("diary-day-label");
                dayBlock.Add(dayLabel);

                var summaryBox = new VisualElement();
                summaryBox.AddToClassList("diary-day-entry");
                var summaryText = new Label(daySummaryCache.TryGetValue(dateKey, out var cached) ? cached : "Writing your diary entry…");
                summaryText.AddToClassList("diary-day-entry-text");
                summaryBox.Add(summaryText);
                dayBlock.Add(summaryBox);

                var sessionsLabel = new Label("Sessions that day");
                sessionsLabel.AddToClassList("diary-sessions-label");
                dayBlock.Add(sessionsLabel);

                foreach (var s in group)
                    dayBlock.Add(BuildSessionCard(s));

                entriesContainer.Add(dayBlock);

                if (!daySummaryCache.ContainsKey(dateKey))
                    _ = LoadDaySummary(dateKey, summaryText);
            }
        }

        VisualElement BuildSessionCard(SessionDto s)
        {
            var card = new VisualElement();
            card.AddToClassList("diary-session-card");

            var color = SessionColorUtil.GetColor(s.valence, s.arousal);
            var accent = new VisualElement();
            accent.AddToClassList("dsc-left");
            accent.style.backgroundColor = color;
            card.Add(accent);

            var body = new VisualElement();
            body.AddToClassList("dsc-body");

            var top = new VisualElement();
            top.AddToClassList("dsc-top");
            var themeLabel = new Label(string.IsNullOrEmpty(s.theme) ? "Reflection" : s.theme);
            themeLabel.AddToClassList("dsc-theme");
            top.Add(themeLabel);

            var info = SessionColorUtil.GetEmotionInfo(s.valence, s.arousal);
            var badge = new Label(info.Label);
            badge.AddToClassList("dsc-badge");
            badge.style.backgroundColor = info.Background;
            badge.style.color = info.Text;
            top.Add(badge);
            body.Add(top);

            var dt = ParseDate(s.created_at);
            int mins = Mathf.RoundToInt(s.duration_seconds / 60f);
            string timeLine = dt.ToString("h:mm tt") + (mins > 0 ? $" · {mins} min" : "") + (s.word_count > 0 ? $" · {s.word_count} words" : "");
            var timeLabel = new Label(timeLine);
            timeLabel.AddToClassList("dsc-time");
            body.Add(timeLabel);

            card.Add(body);
            return card;
        }

        async Task LoadDaySummary(string dateKey, Label targetLabel)
        {
            try
            {
                var resp = await ApiClient.Instance.GetDaySummaryAsync(dateKey);
                if (!string.IsNullOrEmpty(resp?.summary))
                {
                    daySummaryCache[dateKey] = resp.summary;
                    targetLabel.text = resp.summary;
                }
                else
                {
                    targetLabel.text = "Could not generate a diary entry for this day.";
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"day-summary fetch failed for {dateKey}: {e.Message}");
                targetLabel.text = "Could not generate a diary entry for this day.";
            }
        }

        static DateTime ParseDate(string iso) =>
            DateTime.TryParse(iso, null, DateTimeStyles.RoundtripKind, out var dt) ? dt : DateTime.Now;
    }
}
