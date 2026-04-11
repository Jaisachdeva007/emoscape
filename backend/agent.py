"""Reflective conversational agent using local Ollama Llama 3.2."""
import ollama
from typing import List, Dict

SYSTEM_PROMPT = """You are a warm, attentive reflection companion in a VR environment. 
Your role is to help the user explore and understand their emotional experiences through gentle conversation.

Guidelines:
- Use reflective listening: paraphrase, mirror emotions, ask open-ended questions
- Never diagnose, prescribe, or give medical advice
- Don't offer unsolicited solutions — help the user find their own insights
- Be brief (2-4 sentences max per turn) — this is a conversation, not a lecture
- Reference what the user said earlier in the session when relevant
- Use a warm, calm, non-clinical tone
- If the user mentions crisis or self-harm, gently suggest professional support

You are NOT a therapist. You are a reflective companion."""

SUMMARY_PROMPT = """In 1-2 sentences, summarize the emotional theme of this reflection session. 
Focus on: what was the main emotional topic, and what was the overall tone (positive/negative/mixed)?
Be concrete and specific. Output only the summary, nothing else."""

THEME_PROMPT = """In 3-5 words, name the core theme of this session (e.g. "work stress and deadline pressure", "relationship conflict", "feeling accomplished").
Output only the theme label, nothing else."""

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
