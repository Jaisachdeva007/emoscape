"""Database models and setup for EmoScape."""
from sqlalchemy import create_engine, Column, Integer, Float, String, Text, DateTime
from sqlalchemy.orm import DeclarativeBase, Session
from datetime import datetime
import os

DB_PATH = os.path.join(os.path.dirname(__file__), '..', 'data', 'emoscape.db')
os.makedirs(os.path.dirname(DB_PATH), exist_ok=True)

engine = create_engine(f'sqlite:///{DB_PATH}', echo=False)

class Base(DeclarativeBase):
    pass

class ReflectionSession(Base):
    __tablename__ = 'sessions'
    id = Column(Integer, primary_key=True, autoincrement=True)
    created_at = Column(DateTime, default=datetime.utcnow)
    duration_seconds = Column(Float, default=0)
    valence = Column(Float, default=0.0)       # -1 to 1
    arousal = Column(Float, default=0.5)       # 0 to 1 (articulation rate normalized)
    intensity = Column(Float, default=0.5)     # 0 to 1 (word count + duration proxy)
    theme = Column(String(200), default='')
    summary = Column(Text, default='')
    transcript = Column(Text, default='')
    word_count = Column(Integer, default=0)
    turn_count = Column(Integer, default=0)

class Utterance(Base):
    __tablename__ = 'utterances'
    id = Column(Integer, primary_key=True, autoincrement=True)
    session_id = Column(Integer)
    created_at = Column(DateTime, default=datetime.utcnow)
    speaker = Column(String(10))  # 'user' or 'agent'
    text = Column(Text)
    valence = Column(Float, default=0.0)

def init_db():
    Base.metadata.create_all(engine)

def get_db():
    with Session(engine) as session:
        yield session
