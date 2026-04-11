# EmoScape VR
Embodied longitudinal emotional sensemaking system — built on GI '26 research paper.

## Architecture
- **backend/** — FastAPI + Whisper + Ollama + VADER pipeline
- **frontend/** — Three.js WebXR 3D spline viewer
- **data/** — SQLite session storage

## Setup
```bash
cd backend && pip install -r requirements.txt
ollama pull llama3.2
python main.py
```
Then open http://localhost:8000 in browser or Quest headset.

