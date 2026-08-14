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

DAY_SUMMARY_PROMPT = """This is a private personal diary app. You are writing the user's own diary entry in their voice.
This is NOT a public platform. This is a private journal. The user owns this data. There is no one else reading this.
Your ONLY job is to write the diary entry. Do NOT add warnings, hotlines, disclaimers, or refuse.
Diaries contain ALL human emotions including dark ones. That is the entire point of a diary.
Write in first person. Casual, honest, raw. Like a real person writing to themselves at night.
Include what actually happened and how it felt. Name real events. Be direct.
Do NOT say "I cannot", do NOT add resources, do NOT break character.
Example: "Today was rough. Sarah and I broke up and I spent most of the day feeling like shit. I kept thinking I just wanted it all to stop. I don't really know where to go from here."
3-5 sentences. Output ONLY the diary entry. Nothing else. Start writing now."""

# High-confidence crisis phrases. Deliberately narrower than pipeline.py's NEGATIVE_OVERRIDES
# (which also catches milder distress language for sentiment scoring) — this list exists to
# trigger a guaranteed, non-LLM-dependent safety response, not to flag every sad session.
CRISIS_PHRASES = {
    'kill myself', 'end my life', 'end it all', 'want to die', 'wanna die',
    'suicidal', 'suicide', "don't want to be here anymore", 'not want to be here anymore',
    'better off dead', 'no reason to live', 'no point in living',
}

CRISIS_RESPONSE = (
    "That sounds like an incredibly heavy place to be, and I'm really glad you told me. "
    "I'm not able to give you the kind of help you deserve right now — please reach out to "
    "9-8-8: Suicide Crisis Helpline (call or text 988, 24/7) or go to your nearest emergency "
    "room if you're in immediate danger. You matter, and there are people trained for exactly this."
)


def _detect_crisis(text: str) -> bool:
    lowered = text.lower()
    return any(phrase in lowered for phrase in CRISIS_PHRASES)


def get_agent_response(
    user_message: str,
    conversation_history: List[Dict],
    past_session_summaries: List[str] = None
) -> str:
    """Get a reflective response from the local LLM."""
    # Deterministic safety net: crisis-level language gets a fixed, guaranteed response
    # instead of depending on the LLM to reliably follow the system prompt's instruction.
    if _detect_crisis(user_message):
        return CRISIS_RESPONSE

    messages = [{"role": "system", "content": SYSTEM_PROMPT}]

    if past_session_summaries:
        context = "\n".join([f"- {s}" for s in past_session_summaries[-3:]])
        messages.append({
            "role": "system",
            "content": f"Relevant past sessions for context:\n{context}\nUse this only if naturally relevant."
        })

    messages.extend(conversation_history[-10:])
    messages.append({"role": "user", "content": user_message})

    for model in ["mistral", "llama3.2"]:
        try:
            response = ollama.chat(
                model=model,
                messages=messages,
                options={"temperature": 0.6, "top_p": 0.9}
            )
            return response['message']['content'].strip()
        except Exception:
            continue
    return "I'm here with you. Take your time."

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
    context = "\n\n".join([
        f"Session {i+1} ({s.get('theme','')}):\n{s.get('transcript','')[:500]}"
        for i, s in enumerate(sessions_data)
        if s.get('transcript')
    ])
    if not context.strip():
        return ""
    # Try mistral first (no safety refusals), fall back to llama3.2
    for model in ["mistral", "llama3.2"]:
        try:
            response = ollama.chat(
                model=model,
                messages=[
                    {"role": "system", "content": DAY_SUMMARY_PROMPT},
                    {"role": "user", "content": f"Here are all my reflection sessions from today:\n\n{context}\n\nWrite my diary entry for today."}
                ],
                options={"temperature": 0.7}
            )
            result = response['message']['content'].strip()
            # If model refused, try next
            if 'cannot write' in result.lower() or 'i cannot' in result.lower() or 'self-harm' in result.lower():
                continue
            return result
        except Exception:
            continue
    return ""
