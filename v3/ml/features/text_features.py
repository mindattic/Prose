"""
Extract ~45 numeric features from beat prose text + positional metadata.

Features are organized in three groups:
  1. Positional / arc structure  (11 features)
  2. Prose surface statistics    (18 features)
  3. Beat-mode soft signals      (7 features)
  4. Semantic embedding PCA      (8 features, optional — requires model load)

The same extractor must be used for both training and inference so that
the feature order and None-handling are identical.
"""
import re
import math
from dataclasses import dataclass, field
from typing import Optional

import numpy as np

# Mode keywords matching BeatModeDetector.cs keyword arrays (lower-case).
_MODE_KEYWORDS: dict[str, list[str]] = {
    "combat":    ["fight", "shot", "blade", "blood", "weapon", "fire", "strike",
                  "attack", "gun", "fist", "wound", "kick", "stab", "shoot", "blast",
                  "explosion", "combat", "battle", "ambush", "assault"],
    "emotional": ["feel", "grief", "tears", "heart", "pain", "love", "fear",
                  "anguish", "regret", "shame", "guilt", "joy", "sorrow", "emotion",
                  "cry", "sob", "wept", "hollow", "numb", "rage", "despair"],
    "dialogue":  ["said", "asked", "replied", "answered", "spoke", "whispered",
                  "shouted", "muttered", "demanded", "admitted", "told", "explained",
                  "conversation", "voice", "words", "listening", "heard"],
    "transition":["walked", "moved", "traveled", "arrived", "left", "entered",
                  "crossed", "drove", "boarded", "climbed", "descended", "approached"],
    "revelation":["realized", "discovered", "knew", "understood", "truth", "revealed",
                  "secret", "learned", "recognized", "remembered", "noticed", "saw"],
    "narrative": ["meanwhile", "later", "before", "after", "morning", "night",
                  "week", "hour", "time", "day", "always", "never", "often"],
}

_ACTION_VERBS = {
    "grabbed", "threw", "slammed", "slashed", "ducked", "rolled", "fired",
    "sprinted", "dove", "charged", "deflected", "blocked", "punched", "kicked",
    "stabbed", "shot", "detonated", "shattered", "spun", "lunged",
}
_INTERIOR_MARKERS = {
    "realized", "thought", "noticed", "felt", "knew", "sensed", "wondered",
    "remembered", "imagined", "feared", "hoped", "decided", "believed",
    "understood", "recognized", "considered",
}
_PUNCT_RE   = re.compile(r"[!?—…]")
_ITALIC_RE  = re.compile(r"[_*][^_*\n]{1,200}[_*]")
_QUOTE_LINE = re.compile(r'^\s*"', re.MULTILINE)
_WORD_RE    = re.compile(r"\b[a-zA-Z]+\b")
_SENT_RE    = re.compile(r"(?<=[.!?])\s+")


@dataclass
class TextFeatures:
    # Positional (11)
    beat_position_ratio:  float = 0.0
    is_opening:           float = 0.0
    is_closing:           float = 0.0
    is_midpoint:          float = 0.0
    act_1:                float = 0.0
    act_2:                float = 0.0
    act_3:                float = 0.0
    act_4:                float = 0.0
    act_5:                float = 0.0
    beats_from_start:     float = 0.0
    beats_from_end:       float = 0.0

    # Prose surface (18)
    word_count:               float = 0.0
    sentence_count:           float = 0.0
    avg_sentence_length:      float = 0.0
    max_sentence_length:      float = 0.0
    sentence_length_variance: float = 0.0
    dialogue_line_ratio:      float = 0.0
    italics_count:            float = 0.0
    paragraph_count:          float = 0.0
    punct_density:            float = 0.0
    avg_word_length:          float = 0.0
    type_token_ratio:         float = 0.0
    has_action_verbs:         float = 0.0
    has_interior_markers:     float = 0.0
    capitalization_ratio:     float = 0.0
    number_count:             float = 0.0
    char_count:               float = 0.0
    unique_words:             float = 0.0
    quote_char_ratio:         float = 0.0

    # Mode soft signals (7)
    mode_combat:       float = 0.0
    mode_emotional:    float = 0.0
    mode_dialogue:     float = 0.0
    mode_transition:   float = 0.0
    mode_revelation:   float = 0.0
    mode_narrative:    float = 0.0
    mode_certainty:    float = 0.0

    # Semantic PCA-8 (filled by TextFeatureExtractor.extract_with_embeddings)
    sem_0: float = 0.0
    sem_1: float = 0.0
    sem_2: float = 0.0
    sem_3: float = 0.0
    sem_4: float = 0.0
    sem_5: float = 0.0
    sem_6: float = 0.0
    sem_7: float = 0.0

    def to_list(self) -> list[float]:
        return list(self.__dict__.values())

    @classmethod
    def columns(cls) -> list[str]:
        return list(cls.__dataclass_fields__.keys())


class TextFeatureExtractor:
    """
    Extract numeric features from beat text + position.
    Call `load_embedder()` once to enable semantic PCA features.
    """

    def __init__(self):
        self._embedder = None
        self._pca = None

    def load_embedder(self, embed_model: str, pca_components: int = 8, pca_fit_texts: list[str] | None = None):
        """Load sentence-transformer + fit PCA on training corpus if provided."""
        from sentence_transformers import SentenceTransformer
        from sklearn.decomposition import PCA

        self._embedder = SentenceTransformer(embed_model)
        self._pca = PCA(n_components=pca_components, random_state=42)
        if pca_fit_texts:
            embeddings = self._embedder.encode(pca_fit_texts, batch_size=64, show_progress_bar=True)
            self._pca.fit(embeddings)

    def fit_pca(self, texts: list[str]):
        if self._embedder is None:
            raise RuntimeError("Call load_embedder() first.")
        embeddings = self._embedder.encode(texts, batch_size=64, show_progress_bar=True)
        self._pca.fit(embeddings)

    def extract(self, text: str, beat_number: int, total_beats: int) -> TextFeatures:
        f = TextFeatures()
        if not text or total_beats == 0:
            return f

        # ── Positional ──────────────────────────────────────────────────────
        ratio = (beat_number - 1) / max(total_beats - 1, 1)
        f.beat_position_ratio = ratio
        f.is_opening          = 1.0 if beat_number <= 2 else 0.0
        f.is_closing          = 1.0 if beat_number >= total_beats - 1 else 0.0
        f.is_midpoint         = 1.0 if abs(ratio - 0.5) < 0.06 else 0.0
        f.beats_from_start    = (beat_number - 1) / total_beats
        f.beats_from_end      = (total_beats - beat_number) / total_beats
        f.act_1 = 1.0 if ratio < 0.20 else 0.0
        f.act_2 = 1.0 if 0.20 <= ratio < 0.40 else 0.0
        f.act_3 = 1.0 if 0.40 <= ratio < 0.60 else 0.0
        f.act_4 = 1.0 if 0.60 <= ratio < 0.80 else 0.0
        f.act_5 = 1.0 if ratio >= 0.80 else 0.0

        # ── Prose surface ────────────────────────────────────────────────────
        lower  = text.lower()
        words  = _WORD_RE.findall(lower)
        sents  = [s.strip() for s in _SENT_RE.split(text.strip()) if s.strip()]
        paras  = [p.strip() for p in text.split("\n\n") if p.strip()]
        tokens = set(words)

        f.word_count      = len(words)
        f.sentence_count  = len(sents)
        f.paragraph_count = len(paras)
        f.char_count      = len(text)
        f.unique_words    = len(tokens)

        sent_lens = [len(_WORD_RE.findall(s)) for s in sents]
        f.avg_sentence_length  = (sum(sent_lens) / len(sent_lens)) if sent_lens else 0.0
        f.max_sentence_length  = max(sent_lens) if sent_lens else 0.0
        f.sentence_length_variance = (
            float(np.var(sent_lens)) if len(sent_lens) > 1 else 0.0
        )

        f.dialogue_line_ratio = len(_QUOTE_LINE.findall(text)) / max(len(sents), 1)
        f.quote_char_ratio    = text.count('"') / max(len(text), 1)
        f.italics_count       = len(_ITALIC_RE.findall(text))
        f.punct_density       = len(_PUNCT_RE.findall(text)) / max(f.word_count, 1) * 100

        f.avg_word_length = (
            sum(len(w) for w in words) / len(words) if words else 0.0
        )
        # Type-token ratio on first 100 words (Yule's K would be better but expensive)
        f.type_token_ratio = len(set(words[:100])) / min(len(words), 100) if words else 0.0

        f.has_action_verbs    = float(bool(_ACTION_VERBS.intersection(tokens)))
        f.has_interior_markers = float(bool(_INTERIOR_MARKERS.intersection(tokens)))

        cap_words = [w for w in words if w[0].isupper()] if words else []
        f.capitalization_ratio = len(cap_words) / max(len(words), 1)
        f.number_count = len(re.findall(r"\b\d+\b", text))

        # ── Mode soft signals ────────────────────────────────────────────────
        mode_scores: dict[str, int] = {}
        for mode, keywords in _MODE_KEYWORDS.items():
            mode_scores[mode] = sum(lower.count(kw) for kw in keywords)

        total_hits = max(sum(mode_scores.values()), 1)
        f.mode_combat      = mode_scores["combat"]      / total_hits
        f.mode_emotional   = mode_scores["emotional"]   / total_hits
        f.mode_dialogue    = mode_scores["dialogue"]    / total_hits
        f.mode_transition  = mode_scores["transition"]  / total_hits
        f.mode_revelation  = mode_scores["revelation"]  / total_hits
        f.mode_narrative   = mode_scores["narrative"]   / total_hits

        top = max(mode_scores.values())
        second = sorted(mode_scores.values(), reverse=True)[1] if len(mode_scores) > 1 else 0
        f.mode_certainty = (top - second) / max(total_hits, 1)

        return f

    def extract_with_embeddings(self, text: str, beat_number: int, total_beats: int) -> TextFeatures:
        f = self.extract(text, beat_number, total_beats)
        if self._embedder is not None and self._pca is not None:
            emb = self._embedder.encode([text])[0]
            pca_vals = self._pca.transform(emb.reshape(1, -1))[0]
            f.sem_0, f.sem_1, f.sem_2, f.sem_3 = (float(v) for v in pca_vals[:4])
            f.sem_4, f.sem_5, f.sem_6, f.sem_7 = (float(v) for v in pca_vals[4:8])
        return f
