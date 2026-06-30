"""
Persona preference model.

Trains one LightGBM model per BeatMode, predicting a persona's score DELTA
from the panel mean: (persona_score - beat_panel_mean).

Features: OCEAN Big Five, age, archetype (one-hot), worldview (one-hot), provider (one-hot).

Output: segment profiles telling us which persona archetypes consistently
over- or under-score specific beat modes — injected as generation guidance.
"""
import sys
import os
sys.path.insert(0, os.path.dirname(os.path.dirname(__file__)))

import json
import pickle
from pathlib import Path
import numpy as np
import pandas as pd
import lightgbm as lgb
import mlflow
from rich.console import Console
from sklearn.preprocessing import LabelEncoder
from config import PERSONA_MODEL_PATH, PERSONAS_JSON_PATH

console = Console()

BEAT_MODES = ["Narrative", "Combat", "EmotionalClimax", "Dialogue", "Transition", "Revelation"]

PERSONA_FEATURES_SQL = """
SELECT
    sr.PersonaId,
    sr.PersonaBlurb,
    sr.Score           AS StrandScore,
    sr.FlowScore,
    srbs.BeatNumber,
    srbs.Score         AS BeatScore,
    s.Slug             AS StrandSlug,
    sr.ContentHash,
    -- Provider as a category feature
    sr.ProviderId
FROM StrandReviewBeatScores srbs
JOIN StrandReviews sr ON sr.Id = srbs.ReviewId
JOIN Strands s ON s.Id = sr.StrandId
WHERE s.IsDraft = 0
"""


class PersonaPreferenceModel:
    def __init__(self):
        self.models: dict[str, lgb.LGBMRegressor] = {}
        self.encoders: dict[str, LabelEncoder] = {}
        self.personas: dict[str, dict] = {}
        self.model_path = Path(PERSONA_MODEL_PATH)

    def _load_personas(self) -> dict[str, dict]:
        if not PERSONAS_JSON_PATH.exists():
            console.print(f"[yellow]personas.json not found at {PERSONAS_JSON_PATH}. "
                          "Run: ss --export-personas-json[/yellow]")
            return {}
        with open(PERSONAS_JSON_PATH) as f:
            data = json.load(f)
        return {p["Id"]: p for p in data}

    def _build_features(self, df: pd.DataFrame) -> pd.DataFrame:
        """Merge persona metadata + encode categoricals."""
        if not self.personas:
            self.personas = self._load_personas()

        rows = []
        for _, row in df.iterrows():
            pid = row["PersonaId"]
            p   = self.personas.get(pid, {})
            ocean = p.get("Ocean", {})
            rows.append({
                "PersonaId":  pid,
                "BeatNumber": row["BeatNumber"],
                "StrandSlug": row["StrandSlug"],
                "ContentHash": row["ContentHash"],
                "BeatScore":  row["BeatScore"],
                "ProviderId": row.get("ProviderId", "unknown"),
                "Openness":          ocean.get("Openness", 50.0),
                "Conscientiousness": ocean.get("Conscientiousness", 50.0),
                "Extraversion":      ocean.get("Extraversion", 50.0),
                "Agreeableness":     ocean.get("Agreeableness", 50.0),
                "Neuroticism":       ocean.get("Neuroticism", 50.0),
                "Age":               float(p.get("Age", 40)),
                "Archetype":         p.get("Archetype", "unknown"),
                "Worldview":         p.get("Worldview", "unknown"),
            })
        return pd.DataFrame(rows)

    def train(self, conn, panel_scores: pd.DataFrame) -> None:
        """
        panel_scores: DataFrame with columns StrandSlug, BeatNumber, ContentHash, MeanBeatScore
        (from pull_reviews). We join that with raw persona scores from DB.
        """
        from db import fetchdf
        console.print("[cyan]Loading raw persona-beat scores...[/cyan]")
        raw = fetchdf(conn, PERSONA_FEATURES_SQL)

        # Build persona features
        feat_df = self._build_features(raw)

        # Merge with panel mean to compute delta
        merged = feat_df.merge(
            panel_scores[["StrandSlug", "BeatNumber", "ContentHash", "MeanBeatScore"]],
            on=["StrandSlug", "BeatNumber", "ContentHash"],
            how="inner",
        )
        merged["ScoreDelta"] = merged["BeatScore"] - merged["MeanBeatScore"]

        # For each beat mode, filter by detected mode and train
        # (we use a simple mode approximation from position ratio as placeholder;
        # the full pipeline injects BeatMode via BeatModeLog join — added in future iteration)
        self.model_path.mkdir(parents=True, exist_ok=True)

        # Train a global model (no per-mode split yet — requires BeatModeLog join)
        feat_cols = ["Openness", "Conscientiousness", "Extraversion", "Agreeableness",
                     "Neuroticism", "Age"]
        cat_cols  = ["Archetype", "Worldview", "ProviderId"]

        for col in cat_cols:
            enc = LabelEncoder()
            merged[col + "_enc"] = enc.fit_transform(merged[col].fillna("unknown"))
            self.encoders[col] = enc

        X = merged[feat_cols + [c + "_enc" for c in cat_cols]].values.astype(np.float32)
        y = merged["ScoreDelta"].values.astype(np.float32)

        split = int(len(X) * 0.85)
        model = lgb.LGBMRegressor(
            objective="regression", metric="rmse",
            num_leaves=31, learning_rate=0.05, n_estimators=300,
            min_child_samples=50, seed=42, verbose=-1,
        )
        model.fit(
            X[:split], y[:split],
            eval_set=[(X[split:], y[split:])],
            callbacks=[lgb.early_stopping(30, verbose=False)],
        )
        self.models["global"] = model

        if mlflow.active_run():
            mlflow.log_metric("persona_model_rows", len(merged))
            mlflow.log_metric("persona_model_unique_personas", merged["PersonaId"].nunique())

        self.save()
        console.print("[green]Persona preference model trained.[/green]")

    def get_segment_profiles(self) -> list[dict]:
        """
        Return top-level insight: which archetypes/worldviews are outlier predictors of score delta.
        Simple group-mean analysis — no model needed.
        """
        # Placeholder: full implementation requires the raw data at inference time
        return []

    def save(self):
        with open(self.model_path / "persona_models.pkl", "wb") as f:
            pickle.dump({"models": self.models, "encoders": self.encoders}, f)

    def load(self):
        path = self.model_path / "persona_models.pkl"
        with open(path, "rb") as f:
            state = pickle.load(f)
        self.models   = state["models"]
        self.encoders = state["encoders"]
        self.personas = self._load_personas()
