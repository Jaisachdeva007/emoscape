"""Affective feature extraction pipeline."""
import numpy as np
from vaderSentiment.vaderSentiment import SentimentIntensityAnalyzer
import re

analyzer = SentimentIntensityAnalyzer()

def get_valence(text: str) -> float:
    """Extract semantic valence using VADER. Returns -1 to 1."""
    if not text.strip():
        return 0.0
    score = analyzer.polarity_scores(text)
    return round(score['compound'], 4)

def estimate_articulation_rate(text: str, duration_seconds: float) -> float:
    """
    Estimate arousal via articulation rate (syllables / phonation time).
    Normalized to 0-1 range based on typical speech rates (2-6 syllables/sec).
    """
    if duration_seconds <= 0:
        return 0.5
    syllable_count = count_syllables(text)
    rate = syllable_count / duration_seconds
    # Normal speech: 2-6 syllables/sec → normalize to 0-1
    normalized = (rate - 2.0) / 4.0
    return float(np.clip(normalized, 0.0, 1.0))

def count_syllables(text: str) -> int:
    """Estimate syllable count using vowel nuclei detection."""
    text = text.lower()
    text = re.sub(r'[^a-z\s]', '', text)
    words = text.split()
    count = 0
    for word in words:
        vowels = len(re.findall(r'[aeiou]+', word))
        # Adjust for silent e
        if word.endswith('e') and len(word) > 2:
            vowels -= 1
        count += max(1, vowels)
    return count

def estimate_intensity(word_count: int, duration_seconds: float, turn_count: int) -> float:
    """
    Estimate interaction intensity (heuristic — not a psychological depth measure).
    Based on word count, duration, and turn count.
    Normalized to 0-1.
    """
    # Typical session: 100-500 words, 2-15 min, 4-20 turns
    word_score = np.clip(word_count / 500, 0, 1)
    duration_score = np.clip(duration_seconds / 900, 0, 1)
    turn_score = np.clip(turn_count / 20, 0, 1)
    intensity = (word_score * 0.4 + duration_score * 0.4 + turn_score * 0.2)
    return float(round(intensity, 4))

def extract_session_features(
    transcript: str,
    duration_seconds: float,
    turn_count: int
) -> dict:
    """Extract all affective features from a completed session."""
    word_count = len(transcript.split())
    valence = get_valence(transcript)
    arousal = estimate_articulation_rate(transcript, duration_seconds)
    intensity = estimate_intensity(word_count, duration_seconds, turn_count)
    return {
        'valence': valence,
        'arousal': arousal,
        'intensity': intensity,
        'word_count': word_count,
    }
