"""
LightGBM beat quality regressor.

Predicts per-beat score (1.0–5.0) from text features + positional metadata.
Uses leave-one-strand-out cross-validation for realistic generalization estimates.

Embedding cache
---------------
Raw sentence embeddings (pre-PCA, float32, ~3.8 GB for 2.5 M rows) are saved to
EMBED_CACHE_PATH keyed by a hash of the beat texts.  On a retrain the cache is
loaded from disk instead of re-encoding (~7 hours → seconds).  PCA is refit from
the raw embeddings (seconds) and the fitted object is persisted in the model pickle.
Delete EMBED_CACHE_PATH to force a full re-encode (e.g. after switching embed model).
"""
import sys
import os
sys.path.insert(0, os.path.dirname(os.path.dirname(__file__)))

import hashlib
import pickle
import numpy as np
import pandas as pd
import lightgbm as lgb
import mlflow
from pathlib import Path
from rich.console import Console
from sklearn.decomposition import PCA
from sklearn.metrics import mean_squared_error, mean_absolute_error, r2_score
from features.text_features import TextFeatureExtractor, TextFeatures
from config import LGBM_PARAMS, BEAT_QUALITY_MODEL_PATH, EMBED_MODEL, PCA_DIM, EMBED_CACHE_PATH

console = Console()


def _texts_hash(texts: list[str]) -> str:
    h = hashlib.sha256()
    for t in texts:
        h.update(t.encode("utf-8", errors="replace"))
    return h.hexdigest()[:16]


class BeatQualityModel:
    """LightGBM regressor predicting per-beat score (1.0–5.0)."""

    def __init__(self):
        self.model: lgb.Booster | None = None
        self.extractor = TextFeatureExtractor()
        self.feature_names: list[str] = []
        self._pca_fitted = False
        self._shap_explainer = None  # cached across top_negative_features calls

    # ── Embedder helpers ─────────────────────────────────────────────────────

    def _ensure_embedder(self):
        if self.extractor._embedder is None:
            from sentence_transformers import SentenceTransformer
            self.extractor._embedder = SentenceTransformer(EMBED_MODEL, local_files_only=True)

    def _get_raw_embeddings(self, texts: list[str]) -> np.ndarray:
        """
        Returns raw (pre-PCA) sentence embeddings, shape (n, EMBED_DIM).

        Cached to EMBED_CACHE_PATH keyed by a hash of the texts.
        Cache hit  → load from disk (seconds).
        Cache miss → encode via SentenceTransformer (~7 h on 2.5 M rows) + cache.
        """
        cache_path = Path(EMBED_CACHE_PATH)
        hash_path  = cache_path.with_suffix(".hash")
        text_hash  = _texts_hash(texts)

        if cache_path.exists() and hash_path.exists():
            if hash_path.read_text().strip() == text_hash:
                console.print("[cyan]Embedding cache hit — loading from disk…[/cyan]")
                return np.load(str(cache_path))

        console.print(f"[cyan]Encoding {len(texts):,} texts (cache miss)…[/cyan]")
        self._ensure_embedder()
        raw_embs = self.extractor._embedder.encode(
            texts, batch_size=64, show_progress_bar=True
        )
        np.save(str(cache_path), raw_embs)
        hash_path.write_text(text_hash)
        console.print(f"[green]Embeddings cached → {cache_path}[/green]")
        return raw_embs

    # ── Featurization ────────────────────────────────────────────────────────

    def _featurize(self, df: pd.DataFrame, embeddings: np.ndarray) -> np.ndarray:
        """Build feature matrix from df rows + pre-computed PCA embeddings."""
        rows = []
        for i, (_, row) in enumerate(df.iterrows()):
            f = self.extractor.extract(
                text=row["BeatText"],
                beat_number=int(row["BeatNumber"]),
                total_beats=int(row["ExpectedBeatCount"]),
            )
            emb = embeddings[i]
            f.sem_0, f.sem_1, f.sem_2, f.sem_3 = (float(v) for v in emb[:4])
            f.sem_4, f.sem_5, f.sem_6, f.sem_7 = (float(v) for v in emb[4:8])
            rows.append(f.to_list())
        return np.array(rows, dtype=np.float32)

    def _encode_batch(self, texts: list[str]) -> np.ndarray:
        """Encode + PCA-transform a small batch (predict / audit paths)."""
        self._ensure_embedder()
        raw = self.extractor._embedder.encode(texts, batch_size=64, show_progress_bar=False)
        if self.extractor._pca is None:
            raise RuntimeError(
                "PCA not loaded — model pickle is missing PCA state. Retrain or reload the model."
            )
        return self.extractor._pca.transform(raw)

    # ── Training ─────────────────────────────────────────────────────────────

    def train(self, df: pd.DataFrame, mlflow_run: bool = True) -> dict:
        """
        Train on df with columns: BeatText, BeatNumber, ExpectedBeatCount,
        StrandSlug, MeanBeatScore.

        Performs leave-one-strand-out CV; trains final model on all data.
        Returns metrics dict.
        """
        slugs = df["StrandSlug"].unique()
        console.print(f"[cyan]Training on {len(df):,} rows, {len(slugs)} strands[/cyan]")

        df = df.reset_index(drop=True)

        # One encode pass (or cache load) — raw pre-PCA embeddings
        raw_embs = self._get_raw_embeddings(df["BeatText"].tolist())

        # ── Cross-validation ─────────────────────────────────────────────────
        # PCA is fit on the TRAIN fold only to avoid leaking test-set distribution.
        fold_rmses, fold_r2s = [], []
        for slug in slugs:
            train_mask = (df["StrandSlug"] != slug).values
            test_mask  = ~train_mask
            train_df   = df[train_mask]
            test_df    = df[test_mask]
            if len(test_df) < 3:
                continue

            fold_pca = PCA(n_components=PCA_DIM, random_state=42)
            fold_pca.fit(raw_embs[train_mask])
            train_embs = fold_pca.transform(raw_embs[train_mask])
            test_embs  = fold_pca.transform(raw_embs[test_mask])

            X_train = self._featurize(train_df, embeddings=train_embs)
            y_train = train_df["MeanBeatScore"].values.astype(np.float32)
            X_test  = self._featurize(test_df,  embeddings=test_embs)
            y_test  = test_df["MeanBeatScore"].values.astype(np.float32)

            params = {**LGBM_PARAMS}
            params.pop("early_stopping_rounds", None)
            params["n_estimators"] = 200

            fold_model = lgb.LGBMRegressor(**params)
            fold_model.fit(X_train, y_train)
            preds = fold_model.predict(X_test)
            fold_rmses.append(float(np.sqrt(mean_squared_error(y_test, preds))))
            fold_r2s.append(float(r2_score(y_test, preds)))

        cv_rmse = float(np.mean(fold_rmses)) if fold_rmses else 0.0
        cv_r2   = float(np.mean(fold_r2s))   if fold_r2s   else 0.0
        console.print(f"  CV RMSE: {cv_rmse:.3f}  CV R²: {cv_r2:.3f}")

        # ── Final model on all data ──────────────────────────────────────────
        # PCA for the final model is fit on the full corpus.
        if not self._pca_fitted:
            console.print("[cyan]Fitting PCA on full dataset…[/cyan]")
            self.extractor._pca = PCA(n_components=PCA_DIM, random_state=42)
            self.extractor._pca.fit(raw_embs)
            self._pca_fitted = True
        all_embs = self.extractor._pca.transform(raw_embs)

        X_all = self._featurize(df, embeddings=all_embs)
        y_all = df["MeanBeatScore"].values.astype(np.float32)

        # Shuffle before splitting to prevent strand-sorted holdout bias
        rng = np.random.default_rng(42)
        idx = rng.permutation(len(X_all))
        X_all, y_all = X_all[idx], y_all[idx]

        split = int(len(X_all) * 0.85)
        X_tr, X_val = X_all[:split], X_all[split:]
        y_tr, y_val = y_all[:split], y_all[split:]

        final = lgb.LGBMRegressor(**LGBM_PARAMS)
        final.fit(
            X_tr, y_tr,
            eval_set=[(X_val, y_val)],
            callbacks=[lgb.early_stopping(LGBM_PARAMS["early_stopping_rounds"], verbose=False)],
        )
        val_preds    = final.predict(X_val)
        holdout_rmse = float(np.sqrt(mean_squared_error(y_val, val_preds)))
        holdout_mae  = float(mean_absolute_error(y_val, val_preds))
        holdout_r2   = float(r2_score(y_val, val_preds))

        self.model         = final.booster_
        self.feature_names = TextFeatures.columns()

        metrics = {
            "cv_rmse": cv_rmse, "cv_r2": cv_r2,
            "holdout_rmse": holdout_rmse, "holdout_mae": holdout_mae,
            "holdout_r2": holdout_r2,
            "n_training_rows": len(df), "n_strands": len(slugs),
        }

        if mlflow_run:
            mlflow.log_params({
                "n_training_rows": len(df),
                "n_strands":       len(slugs),
                **{f"lgbm_{k}": v for k, v in LGBM_PARAMS.items()
                   if k not in ("early_stopping_rounds",)},
            })
            mlflow.log_metrics(metrics)

        console.print(f"[green]Final holdout RMSE: {holdout_rmse:.3f}  R²: {holdout_r2:.3f}[/green]")
        return metrics

    # ── Inference ────────────────────────────────────────────────────────────

    def predict(self, texts: list[str], beat_numbers: list[int], total_beats: list[int]) -> np.ndarray:
        if self.model is None:
            raise RuntimeError("Model not trained. Call train() or load().")
        embs = self._encode_batch(texts)
        rows = []
        for i, (text, bn, tb) in enumerate(zip(texts, beat_numbers, total_beats)):
            f = self.extractor.extract(text, bn, tb)
            emb = embs[i]
            f.sem_0, f.sem_1, f.sem_2, f.sem_3 = (float(v) for v in emb[:4])
            f.sem_4, f.sem_5, f.sem_6, f.sem_7 = (float(v) for v in emb[4:8])
            rows.append(f.to_list())
        return self.model.predict(np.array(rows, dtype=np.float32))

    def shap_values(self, texts: list[str], beat_numbers: list[int], total_beats: list[int]) -> np.ndarray:
        """Returns SHAP values: shape (n_samples, n_features)."""
        import shap
        embs = self._encode_batch(texts)
        rows = []
        for i, (text, bn, tb) in enumerate(zip(texts, beat_numbers, total_beats)):
            f = self.extractor.extract(text, bn, tb)
            emb = embs[i]
            f.sem_0, f.sem_1, f.sem_2, f.sem_3 = (float(v) for v in emb[:4])
            f.sem_4, f.sem_5, f.sem_6, f.sem_7 = (float(v) for v in emb[4:8])
            rows.append(f.to_list())
        X = np.array(rows, dtype=np.float32)
        if self._shap_explainer is None:
            self._shap_explainer = shap.TreeExplainer(self.model)
        return self._shap_explainer.shap_values(X)

    def top_negative_features(self, text: str, beat_number: int, total_beats: int, top_n: int = 3) -> list[tuple[str, float]]:
        """Return top N features driving the score DOWN for a single beat."""
        import shap
        emb = self._encode_batch([text])[0]
        f = self.extractor.extract(text, beat_number, total_beats)
        f.sem_0, f.sem_1, f.sem_2, f.sem_3 = (float(v) for v in emb[:4])
        f.sem_4, f.sem_5, f.sem_6, f.sem_7 = (float(v) for v in emb[4:8])
        X = np.array([f.to_list()], dtype=np.float32)
        if self._shap_explainer is None:
            self._shap_explainer = shap.TreeExplainer(self.model)
        shap_vals = self._shap_explainer.shap_values(X)[0]
        pairs = sorted(zip(self.feature_names, shap_vals), key=lambda x: x[1])
        return [(name, float(val)) for name, val in pairs[:top_n]]

    # ── Persistence ──────────────────────────────────────────────────────────

    def save(self, path=None):
        path = path or BEAT_QUALITY_MODEL_PATH
        with open(path, "wb") as f:
            pickle.dump({
                "booster":       self.model,
                "feature_names": self.feature_names,
                "pca":           self.extractor._pca,
                "pca_fitted":    self._pca_fitted,
            }, f)
        console.print(f"[green]Model saved to {path}[/green]")
        if mlflow.active_run():
            mlflow.log_artifact(str(path))

    def load(self, path=None):
        path = path or BEAT_QUALITY_MODEL_PATH
        with open(path, "rb") as f:
            state = pickle.load(f)
        self.model         = state["booster"]
        self.feature_names = state["feature_names"]
        if state.get("pca") is not None:
            self.extractor._pca = state["pca"]
            self._pca_fitted    = True
        console.print(f"[green]Model loaded from {path}[/green]")
