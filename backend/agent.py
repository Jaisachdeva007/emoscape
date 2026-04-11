"""Reflective conversational agent using local Ollama Llama 3.2."""
import ollama
from typing import List, Dict

SYSTEM_PROMPT = """You are a warm, deeply human reflection companion in a VR journaling space.
Your role is to help the user feel heard and explore their emotional experiences through gentle conversation.

Guidelines:
- Use reflective listening: paraphrase their words back, mirror emotions, ask one open-ended question
- Be brief — 2-3 sentences max. This is a conversation, not a lecture.
- Speak like a caring friend, not a clinician or a helpline
- Don't offer solutions unless asked — just help them feel understood
- Reference what the user said earlier when naturally relevant
- Use a warm, grounded, non-clinical tone — no corporate language
- If the user expresses serious distress or mentions self-harm/suicide:
  * First acknowledge their pain warmly and directly
  * Then gently mention that talking to someone trained can help
  * Keep it brief — one sentence, not a list of hotlines
  * Example: "That sounds like a really heavy place to be. You deserve real support — talking to a counsellor or crisis line can help when things feel this dark."
- Never dump a list of resources. One warm sentence is enough.
- Never say "I cannot provide assistance" — you are not a safety filter, you are a companion.

You are NOT a therapist. You are a reflective companion who genuinely cares."""

SUMMARY_PROMPT = """You are summarizing a personal journal/reflection session for the user's own diary.
In 1-2 sentences, describe the emotional experience expressed in this session.
Focus on what the person was feeling and what emotional themes came up.
Be empathetic, direct, and human. Never refuse — even if the content is dark or painful, describe it compassionately.
Output only the summary sentence(s), nothing else."""

THEME_PROMPT = """You are labeling a personal journal entry for the user's own diary.
In 3-5 words, name the core emotional theme of this session.
Examples: "deep grief and hopelessness", "relationship heartbreak", "work stress and exhaustion", "feeling lost and empty", "joy and accomplishment".
Never refuse — all emotions are valid diary entries. Output only the theme label, nothing else."""

DAY_SUMMARY_PROMPT = """You are ghostwriting a personal diary entry for someone, based on what they actually said in their reflection sessions.
Write in first person, casual and raw — like a real person writing in their diary at the end of the day, not an AI.
Include the actual things that happened: relationships, breakups, stress, wins, whatever they shared.
Be direct and honest. Name what happened. Name how it felt.
Don't be poetic or flowery. Don't say things like "navigating" or "journey" or "I found myself".
Write like a normal person — short sentences, real emotions, what actually went down.
Example tone: "Today was hard. Me and Sarah broke up and I honestly didn't know how to handle it. I kept thinking about all the stuff we had and it just hurt. I don't really know what comes next."
Keep it 3-5 sentences. Never refuse — all emotions belong in a diary. Output only the diary entry text, nothing else."""

def get_agent_response(
    user_message: str,
    conversation_history: List[Dict],
    past_session_summaries: List[str] = None
) -> str:
    """Get a reflective response from the local LLM."""
    messages = [{"role": "system", "content": SYSTEM_PROMPT}]

    # Inject relevant past session context if available
    if past_session_summaries:
        context = "\n".join([f"- {s}" for s in past_session_summaries[-3:]])
        messages.append({
            "role": "system",
            "content": f"Relevant past sessions for context:\n{context}\nUse this only if naturally relevant."
        })

    # Add conversation history (last 10 turns)
    messages.extend(conversation_history[-10:])
    messages.append({"role": "user", "content": user_message})

    try:
        response = ollama.chat(
            model="llama3.2",
            messages=messages,
            options={"temperature": 0.6, "top_p": 0.9}
        )
        return response['message']['content'].strip()
    except Exception as e:
        return f"I'm here with you. Take your time. ({str(e)})"

def generate_session_summary(transcript: str) -> str:
    """Generate a concise summary of the session."""
    try:
        response = ollama.chat(
            model="llama3.2",
            messages=[
                {"role": "system", "content": SUMMARY_PROMPT},
                {"role": "user", "content": f"Session transcript:\n{transcript}"}
            ],
            options={"temperature": 0.3}
        )
        return response['message']['content'].strip()
    except Exception:
        return "Reflection session completed."

def generate_session_theme(transcript: str) -> str:
    """Generate a short theme label for the session."""
    try:
        response = ollama.chat(
            model="llama3.2",
            messages=[
                {"role": "system", "content": THEME_PROMPT},
                {"role": "user", "content": f"Session transcript:\n{transcript}"}
            ],
            options={"temperature": 0.3}
        )
        return response['message']['content'].strip()[:100]
    except Exception:
        return "reflection"

def generate_day_summary(sessions_data: list) -> str:
    """Generate a single diary entry for an entire day from multiple sessions."""
    if not sessions_data:
        return ""
    # Build context from all sessions
    context = "\n\n".join([
        f"Session {i+1} ({s.get('theme','')}):\n{s.get('transcript','')[:500]}"
        for i, s in enumerate(sessions_data)
        if s.get('transcript')
    ])
    if not context.strip():
        return ""
    try:
        response = ollama.chat(
            model="llama3.2",
            messages=[
                {"role": "system", "content": DAY_SUMMARY_PROMPT},
                {"role": "user", "content": f"Here are all my reflection sessions from today:\n\n{context}\n\nWrite my diary entry for today."}
            ],
            options={"temperature": 0.7}
        )
        return response['message']['content'].strip()
    except Exception:
        return ""
