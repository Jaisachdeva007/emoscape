using UnityEngine;

namespace EmoScape.Bootstrap
{
    /// <summary>
    /// Persistent cross-scene session state, mirrors the web app's
    /// currentSessionId / sessionStart / turnCount globals shared between pages.
    /// </summary>
    public class SessionManager : MonoBehaviour
    {
        public static SessionManager Instance { get; private set; }

        public int? CurrentSessionId { get; private set; }
        public int TurnCount { get; private set; }
        float sessionStartTime;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        public void BeginSession(int sessionId)
        {
            CurrentSessionId = sessionId;
            sessionStartTime = Time.realtimeSinceStartup;
            TurnCount = 0;
        }

        public void RegisterTurn() => TurnCount++;

        public void EndSession()
        {
            CurrentSessionId = null;
            TurnCount = 0;
        }

        public float ElapsedSeconds => CurrentSessionId.HasValue ? Time.realtimeSinceStartup - sessionStartTime : 0f;
    }
}
