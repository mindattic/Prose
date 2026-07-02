"""
Protagonist voice register classifier.

Trains a TF-IDF + LogisticRegression classifier on beat texts labeled by strand slug.
At inference, flags when a beat "sounds like" a different strand than expected —
catching register bleed (Kyle's arithmetic/parliament leaking into Sasha, etc.)
before it accumulates.

The model learns vocabulary signatures per strand; a mismatch between predicted
and expected slug at high confidence is a register bleed signal.

Usage:
    python register_classifier.py --train
    python register_classifier.py --infer --text "<beat text>" --strand-slug sasha_v
    python register_classifier.py --top-words sasha_v
Output (--infer):
    {"expected_slug": "sasha_v", "predicted_slug": "bushido_coda", "confidence": 0.88, "bleed": true}
"""
import sys
import os
sys.path.insert(0, os.path.dirname(os.path.dirname(__file__)))

import json
import pickle
import argparse
import pandas as pd
from pathlib import Path
from rich.console import Console
from sklearn.linear_model import LogisticRegression
from sklearn.feature_extraction.text import TfidfVectorizer
from sklearn.pipeline import Pipeline
from sklearn.model_selection import cross_val_score
from config import (
    REGISTER_MODEL_PATH, BEATS_CACHE_PATH,
    REGISTER_BLEED_MIN_CONF, REGISTER_MIN_BEATS_TO_TRAIN,
)

console = Console()


class RegisterClassifier:
    def __init__(self, model_path: Path | None = None):
        self.model_path    = Path(model_path or REGISTER_MODEL_PATH)
        self.pipeline: Pipeline | None = None
        self.trained_slugs: list[str]  = []

    def train(self, df: pd.DataFrame) -> dict:
        """
        df must have columns: StrandSlug, BeatText.
        Strands with fewer than REGISTER_MIN_BEATS_TO_TRAIN beats are excluded.
        """
        counts   = df.groupby("StrandSlug").size()
        eligible = counts[counts >= REGISTER_MIN_BEATS_TO_TRAIN].index.tolist()
        df       = df[df["StrandSlug"].isin(eligible)].copy()

        if len(eligible) < 2:
            console.print("[yellow]Need >= 2 eligible strands to train register classifier.[/yellow]")
            return {}

        console.print(f"[cyan]Training register classifier: {len(df):,} beats, {len(eligible)} strands[/cyan]")

        X = df["BeatText"].tolist()
        y = df["StrandSlug"].tolist()
        self.trained_slugs = sorted(set(y))

        self.pipeline = Pipeline([
            ("tfidf", TfidfVectorizer(
                ngram_range=(1, 2),
                max_features=20_000,
                sublinear_tf=True,
                min_df=3,
            )),
            ("lr", LogisticRegression(
                C=1.0, max_iter=1000, class_weight="balanced",
                solver="lbfgs", multi_class="multinomial",
            )),
        ])
        self.pipeline.fit(X, y)

        cv_folds = min(5, len(eligible))
        scores   = cross_val_score(self.pipeline, X, y, cv=cv_folds, scoring="accuracy")
        metrics  = {
            "cv_accuracy_mean": float(scores.mean()),
            "cv_accuracy_std":  float(scores.std()),
            "n_strands":        len(eligible),
            "n_beats":          len(df),
        }
        console.print(f"[green]CV accuracy: {scores.mean():.2%} ± {scores.std():.2%}[/green]")

        with open(self.model_path, "wb") as f:
            pickle.dump({"pipeline": self.pipeline, "trained_slugs": self.trained_slugs}, f)
        console.print(f"[green]Register classifier saved → {self.model_path}[/green]")
        return metrics

    def load(self) -> None:
        with open(self.model_path, "rb") as f:
            state = pickle.load(f)
        self.pipeline      = state["pipeline"]
        self.trained_slugs = state["trained_slugs"]

    def predict(self, text: str) -> tuple[str, float]:
        """Returns (predicted_slug, confidence)."""
        if self.pipeline is None:
            raise RuntimeError("Call train() or load() first.")
        probs = self.pipeline.predict_proba([text])[0]
        idx   = int(probs.argmax())
        return str(self.pipeline.classes_[idx]), float(probs[idx])

    def check_bleed(self, text: str, expected_slug: str) -> dict:
        predicted, confidence = self.predict(text)
        bleed = predicted != expected_slug and confidence >= REGISTER_BLEED_MIN_CONF
        return {
            "expected_slug":  expected_slug,
            "predicted_slug": predicted,
            "confidence":     round(confidence, 4),
            "bleed":          bleed,
        }

    def top_discriminating_words(self, slug: str, top_n: int = 10) -> list[tuple[str, float]]:
        """Words most distinctive of this strand's vocabulary register."""
        if self.pipeline is None:
            raise RuntimeError("Call train() or load() first.")
        tfidf  = self.pipeline.named_steps["tfidf"]
        lr     = self.pipeline.named_steps["lr"]
        idx    = list(self.pipeline.classes_).index(slug)
        coefs  = lr.coef_[idx]
        top    = coefs.argsort()[-top_n:][::-1]
        vocab  = {v: k for k, v in tfidf.vocabulary_.items()}
        return [(vocab[i], float(coefs[i])) for i in top]


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--train",       action="store_true")
    parser.add_argument("--infer",       action="store_true")
    parser.add_argument("--text",        type=str, default="")
    parser.add_argument("--strand-slug", type=str, default="")
    parser.add_argument("--top-words",   type=str, default="", metavar="SLUG",
                        help="Print top discriminating words for a strand slug")
    args = parser.parse_args()

    clf = RegisterClassifier()

    if args.train:
        if BEATS_CACHE_PATH.exists():
            df = pd.read_parquet(BEATS_CACHE_PATH)
        else:
            from extract.pull_beats import run as pull_beats
            df = pull_beats()
        clf.train(df)

    if args.top_words:
        clf.load()
        words = clf.top_discriminating_words(args.top_words)
        print(json.dumps([{"word": w, "coef": round(c, 4)} for w, c in words], indent=2))

    if args.infer:
        clf.load()
        if not args.text:
            print(json.dumps({"error": "no --text provided"}))
            return
        if args.strand_slug:
            result = clf.check_bleed(args.text, args.strand_slug)
        else:
            predicted, confidence = clf.predict(args.text)
            result = {"predicted_slug": predicted, "confidence": round(confidence, 4)}
        print(json.dumps(result))


if __name__ == "__main__":
    main()
