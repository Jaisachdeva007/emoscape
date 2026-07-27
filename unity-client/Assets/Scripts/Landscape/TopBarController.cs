using System;
using UnityEngine.UIElements;

namespace EmoScape.Landscape
{
    /// <summary>Ports the top pill nav bar (Landscape/Reflect/Diary tabs + Talk link) from frontend/index.html.</summary>
    public class TopBarController
    {
        readonly Button btnLandscape, btnReflect, btnDiary, btnTalk;
        readonly Label sessionCountLabel;

        public TopBarController(VisualElement root, Action onLandscape, Action onReflect, Action onDiary, Action onTalk)
        {
            btnLandscape = root.Q<Button>("btn-landscape");
            btnReflect = root.Q<Button>("btn-reflect");
            btnDiary = root.Q<Button>("btn-diary");
            btnTalk = root.Q<Button>("btn-talk");
            sessionCountLabel = root.Q<Label>("session-count");

            btnLandscape.clicked += () => { SetActive(btnLandscape); onLandscape(); };
            btnReflect.clicked += () => { SetActive(btnReflect); onReflect(); };
            btnDiary.clicked += () => { SetActive(btnDiary); onDiary(); };
            btnTalk.clicked += () => onTalk();
        }

        public void SetSessionCount(int count) => sessionCountLabel.text = $"{count} session{(count != 1 ? "s" : "")}";

        void SetActive(Button active)
        {
            foreach (var b in new[] { btnLandscape, btnReflect, btnDiary })
                b.RemoveFromClassList("tab-btn--active");
            active.AddToClassList("tab-btn--active");
        }
    }
}
