"""EmoScape FastAPI backend server."""
import time
import json
from datetime import datetime
from typing import List, Optional
from fastapi import FastAPI, WebSocket, WebSocketDisconnect, Depends, HTTPException
from fastapi.middleware.cors import CORSMiddleware
from fastapi.staticfiles import StaticFiles
from fastapi.responses import FileResponse
from pydantic import BaseModel
from sqlalchemy.orm import Session
import os

from database import init_db, get_db, ReflectionSession, Utterance
from pipeline import extract_session_features, get_valence
from agent import get_agent_response, generate_session_summary, generate_session_theme, generate_day_summary

app = FastAPI(title="EmoScape API", version="1.0.0")

app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],
    allow_methods=["*"],
    allow_headers=["*"],
)

# Serve frontend static files
FRONTEND_DIR = os.path.join(os.path.dirname(__file__), '..', 'frontend')
if os.path.exists(FRONTEND_DIR):
    app.mount("/static", StaticFiles(directory=FRONTEND_DIR), name="static")

@app.on_event("startup")
def startup():
    init_db()
    print("✅ EmoScape server started")
    print("🌐 Open http://localhost:8000 in browser or Quest headset")

# ====== MODELS ======
class ChatMessage(BaseModel):
    session_id: int
    text: str
    duration_hint: Optional[float] = 0.0

class EndSession(BaseModel):
    session_id: int
    duration_seconds: float
    turn_count: int

# ====== ROUTES ======

@app.get("/")
def root():
    index = os.path.join(FRONTEND_DIR, 'index.html')
    if os.path.exists(index):
        return FileResponse(index)
    return {"status": "EmoScape API running", "docs": "/docs"}

@app.post("/session/start")
def start_session(db: Session = Depends(get_db)):
    """Start a new reflection session."""
    session = ReflectionSession()
    db.add(session)
    db.commit()
    db.refresh(session)
    return {"session_id": session.id, "started_at": session.created_at.isoformat()}

@app.post("/session/chat")
def chat(msg: ChatMessage, db: Session = Depends(get_db)):
    """Send a user message and get agent response."""
    # Load session
    session = db.get(ReflectionSession, msg.session_id)
    if not session:
        raise HTTPException(404, "Session not found")

    # Load conversation history for this session
    utterances = db.query(Utterance).filter(
        Utterance.session_id == msg.session_id
    ).order_by(Utterance.created_at).all()

    history = [{"role": u.speaker if u.speaker == "user" else "assistant", "content": u.text}
               for u in utterances]

    # Load past session summaries for cross-session memory
    past = db.query(ReflectionSession).filter(
        ReflectionSession.id < msg.session_id,
        ReflectionSession.summary != ''
    ).order_by(ReflectionSession.created_at.desc()).limit(5).all()
    past_summaries = [s.summary for s in past if s.summary]

    # Get agent response
    agent_reply = get_agent_response(msg.text, history, past_summaries)

    # Store utterances
    user_utt = Utterance(
        session_id=msg.session_id,
        speaker='user',
        text=msg.text,
        valence=get_valence(msg.text)
    )
    agent_utt = Utterance(
        session_id=msg.session_id,
        speaker='agent',
        text=agent_reply,
        valence=get_valence(agent_reply)
    )
    db.add_all([user_utt, agent_utt])
    db.commit()

    return {"reply": agent_reply, "session_id": msg.session_id}

@app.post("/session/end")
def end_session(data: EndSession, db: Session = Depends(get_db)):
    """End session, extract features, generate summary."""
    session = db.get(ReflectionSession, data.session_id)
    if not session:
        raise HTTPException(404, "Session not found")

    # Build full transcript
    utterances = db.query(Utterance).filter(
        Utterance.session_id == data.session_id,
        Utterance.speaker == 'user'
    ).all()
    transcript = " ".join([u.text for u in utterances])

    # Extract features
    features = extract_session_features(transcript, data.duration_seconds, data.turn_count)

    # Generate summary and theme via LLM
    full_transcript = " ".join([u.text for u in db.query(Utterance).filter(
        Utterance.session_id == data.session_id
    ).all()])
    summary = generate_session_summary(full_transcript)
    theme = generate_session_theme(full_transcript)

    # Update session
    session.duration_seconds = data.duration_seconds
    session.turn_count = data.turn_count
    session.transcript = full_transcript
    session.valence = features['valence']
    session.arousal = features['arousal']
    session.intensity = features['intensity']
    session.word_count = features['word_count']
    session.summary = summary
    session.theme = theme
    db.commit()

    return {
        "session_id": data.session_id,
        "valence": features['valence'],
        "arousal": features['arousal'],
        "intensity": features['intensity'],
        "theme": theme,
        "summary": summary
    }

@app.get("/sessions")
def get_sessions(db: Session = Depends(get_db)):
    """Get all sessions for 3D visualization."""
    sessions = db.query(ReflectionSession).order_by(ReflectionSession.created_at).all()
    return [{
        "id": s.id,
        "created_at": s.created_at.isoformat(),
        "valence": s.valence,
        "arousal": s.arousal,
        "intensity": s.intensity,
        "theme": s.theme,
        "summary": s.summary,
        "word_count": s.word_count,
        "duration_seconds": s.duration_seconds
    } for s in sessions]

@app.get("/sessions/{session_id}/utterances")
def get_utterances(session_id: int, db: Session = Depends(get_db)):
    """Get full conversation for a session."""
    utterances = db.query(Utterance).filter(
        Utterance.session_id == session_id
    ).order_by(Utterance.created_at).all()
    return [{"speaker": u.speaker, "text": u.text, "valence": u.valence} for u in utterances]

@app.get("/day-summary")
def get_day_summary(date: str = None, db: Session = Depends(get_db)):
    """Get or generate a diary summary for a given date (YYYY-MM-DD). Defaults to today."""
    from datetime import date as date_type
    target = date or str(date_type.today())
    
    # Get all sessions for that day
    all_sessions = db.query(ReflectionSession).order_by(ReflectionSession.created_at).all()
    day_sessions = [
        s for s in all_sessions
        if s.created_at.strftime('%Y-%m-%d') == target
        or (target == str(date_type.today()) and s.created_at.strftime('%Y-%m-%d') in [target, str(date_type.today())])
    ]
    day_sessions = [s for s in day_sessions if s.transcript]
    
    if not day_sessions:
        return {"date": target, "summary": "", "session_count": 0}
    
    sessions_data = [{"theme": s.theme, "transcript": s.transcript} for s in day_sessions]
    summary = generate_day_summary(sessions_data)
    
    return {
        "date": target,
        "summary": summary,
        "session_count": len(day_sessions),
        "themes": [s.theme for s in day_sessions if s.theme]
    }

@app.get("/health")
def health():
    return {"status": "ok", "time": datetime.utcnow().isoformat()}

if __name__ == "__main__":
    import uvicorn
    uvicorn.run(app, host="0.0.0.0", port=8000, reload=False)
