using System;
using System.Collections.Generic;

namespace EmoScape.Networking
{
    // DTOs mirror backend/main.py's JSON shapes exactly (see ChatMessage/EndSession
    // pydantic models and the /sessions, /day-summary, /stt route return values).

    [Serializable]
    public class SessionDto
    {
        public int id;
        public string created_at;
        public float valence;
        public float arousal;
        public float intensity;
        public string theme;
        public string summary;
        public int word_count;
        public float duration_seconds;
    }

    [Serializable]
    public class StartSessionResponse
    {
        public int session_id;
        public string started_at;
    }

    [Serializable]
    public class ChatRequest
    {
        public int session_id;
        public string text;
    }

    [Serializable]
    public class ChatResponse
    {
        public string reply;
        public int session_id;
    }

    [Serializable]
    public class EndSessionRequest
    {
        public int session_id;
        public float duration_seconds;
        public int turn_count;
    }

    [Serializable]
    public class EndSessionResponse
    {
        public int session_id;
        public float valence;
        public float arousal;
        public float intensity;
        public string theme;
        public string summary;
    }

    [Serializable]
    public class DaySummaryResponse
    {
        public string date;
        public string summary;
        public int session_count;
        public List<string> themes;
    }

    [Serializable]
    public class SttResponse
    {
        public string text;
    }
}
