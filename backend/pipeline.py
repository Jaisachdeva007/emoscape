"""Affective feature extraction pipeline."""
from vaderSentiment.vaderSentiment import SentimentIntensityAnalyzer
import re

analyzer = SentimentIntensityAnalyzer()

# Words VADER gets wrong in emotional/journaling context
NEGATIVE_OVERRIDES = {
    'die', 'dying', 'dead', 'death', 'suicide', 'suicidal', 'kill', 'killing',
    'hurt', 'pain', 'numb', 'empty', 'hopeless', 'worthless', 'hate', 'hated',
    'wanna die', 'want to die', 'end it', 'give up', 'cant go on', 'no point'
}

def get_valence(text: str) -> float:
    """Extract semantic valence using VADER with override corrections. Returns -1 to 1."""
    if not text.strip():
        return 0.0
    score = analyzer.polarity_scores(text)
    compound = score['compound']

    # Apply override: if text contains strong negative words, pull score negative
    text_lower = text.lower()
    override_hits = sum(1 for w in NEGATIVE_OVERRIDES if w in text_lower)
    if override_hits > 0:
        penalty = min(override_hits * 0.35, 0.9)
        compound = max(compound - penalty, -1.0)

    return round(compound, 4)

def estimate_articulation_rate(text: str, duration_seconds: float) -> float:
    """
    Estimate arousal from text features: exclamation marks, question intensity,
    word length variance, and syllable density. More expressive = higher arousal.
    """
    if not text.strip():
        return 0.5

    words = text.split()
    word_count = len(words)
    if word_count == 0:
        return 0.5

    # Exclamation and question marks suggest activation
    exclaim = text.count('!') * 0.15
    questions = text.count('?') * 0.08

    # Caps words suggest emphasis
    caps_ratio = sum(1 for w in words if w.isupper() and len(w) > 1) / max(word_count, 1)

    # High emotion words
    high_arousal_words = {'stressed','stress','anxious','anxiety','excited','overwhelmed','angry',
                          'frustrated','terrified','amazing','incredible','exhausted','panicking',
                          'urgent','cant','cannot','never','always','everything','nothing',
                          'worst','best','negative','horrible','awful','terrible','depressed',
                          'miserable','broken','shattered','destroyed','lost','hopeless','empty',
                          'hate','dying','dead','pain','hurt','scared','afraid','lonely'}
    arousal_hits = sum(1 for w in words if w.lower().strip('!?,. ') in high_arousal_words)
    word_arousal = min(arousal_hits / max(word_count, 1) * 5, 1.0)

    # Sentence length variance (choppy = more activated)
    sentences = [s.strip() for s in re.split(r'[.!?]', text) if s.strip()]
    if len(sentences) > 1:
        lengths = [len(s.split()) for s in sentences]
        avg = sum(lengths)/len(lengths)
        variance = sum((l-avg)**2 for l in lengths)/len(lengths)
        choppiness = min(variance / 50, 1.0)
    else:
        choppiness = 0.3

    score = (exclaim + questions + caps_ratio * 0.5 + word_arousal * 0.4 + choppiness * 0.2)
    return float(max(0.05, min(0.95, score)))

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
    word_score = max(0, min(1, word_count / 500))
    duration_score = max(0, min(1, duration_seconds / 900))
    turn_score = max(0, min(1, turn_count / 20))
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
