"""
LightGBM beat quality regressor.

Predicts per-beat score (1.0–5.0) from text features + positional metadata.
Uses leave-one-strand-out cross-validation for realistic generalization estimates.
"""
import sys
import os
sys.path.insert(0, os.path.dirname(os.path.dirname(__file__)))

import pickle
import numpy as np
import pandas as pd
import lightgbm as lgb
import mlflow
from rich.console import Console
from sklearn.metrics import mean_squared_error, mean_absolute_error, r2_score
from features.text_features import TextFeatureExtractor, TextFeatures
from config import LGBM_PARAMS, BEAT_QUALITY_MODEL_PATH, EMBED_MODEL, PCA_DIM

console = Console()


class BeatQualityModel:
    """LightGBM regressor predicting per-beat score (1.0–5.0)."""

    def __init__(self):
        self.model: lgb.Booster | None = None
        self.extractor = TextFeatureExtractor()
        self.feature_names: list[str] = []
        self._pca_fitted = False

    def fit_extractor(self, texts: list[str]):
        """Load embedder and fit PCA on the training corpus."""
        self.extractor.load_embedder(EMBED_MODEL, pca_components=PCA_DIM, pca_fit_texts=texts)
        self._pca_fitted = True

    def _featurize(self, df: pd.DataFrame) -> np.ndarray:
        rows = []
        for _, row in df.iterrows():
            f = self.extractor.extract_with_embeddings(
                text=row["BeatText"],
                beat_number=int(row["BeatNumber"]),
                total_beats=int(row["ExpectedBeatCount"]),
            )
            rows.append(f.to_list())
        return np.array(rows, dtype=np.float32)

    def train(self, df: pd.DataFrame, mlflow_run: bool = True) -> dict:
        """
        Train on df with columns: BeatText, BeatNumber, ExpectedBeatCount,
        StrandSlug, MeanBeatScore.

        Performs leave-one-strand-out cross-validation; trains final model on all data.
        Returns metrics dict.
        """
        slugs = df["StrandSlug"].unique()
        console.print(f"[cyan]Training on {len(df):,} rows, {len(slugs)} strands[/cyan]")

        if not self._pca_fitted:
            self.fit_extractor(df["BeatText"].tolist())

        # ── Cross-validation ─────────────────────────────────────────────────
        fold_rmses, fold_r2s = [], []
        for slug in slugs:
            train_df = df[df["StrandSlug"] != slug]
            test_df  = df[df["StrandSlug"] == slug]
            if len(test_df) < 3:
                continue

            X_train = self._featurize(train_df)
            y_train = train_df["MeanBeatScore"].values.astype(np.float32)
            X_test  = self._featurize(test_df)
            y_test  = test_df["MeanBeatScore"].values.astype(np.float32)

            params = {**LGBM_PARAMS}
            params.pop("early_stopping_rounds", None)
            params["n_estimators"] = 200

            model = lgb.LGBMRegressor(**params)
            model.fit(X_train, y_train)
            preds = model.predict(X_test)
            rmse = float(np.sqrt(mean_squared_error(y_test, preds)))
            r2   = float(r2_score(y_test, preds))
            fold_rmses.append(rmse)
            fold_r2s.append(r2)

        cv_rmse = float(np.mean(fold_rmses)) if fold_rmses else 0.0
        cv_r2   = float(np.mean(fold_r2s)) if fold_r2s else 0.0
        console.print(f"  CV RMSE: {cv_rmse:.3f}  CV R²: {cv_r2:.3f}")

        # ── Final model on all data ──────────────────────────────────────────
        X_all = self._featurize(df)
        y_all = df["MeanBeatScore"].values.astype(np.float32)
        split = int(len(X_all) * 0.85)
        X_tr, X_val = X_all[:split], X_all[split:]
        y_tr, y_val = y_all[:split], y_all[split:]

        final = lgb.LGBMRegressor(**LGBM_PARAMS)
        final.fit(
            X_tr, y_tr,
            eval_set=[(X_val, y_val)],
            callbacks=[lgb.early_stopping(LGBM_PARAMS["early_stopping_rounds"], verbose=False)],
        )
        val_preds = final.predict(X_val)
        holdout_rmse = float(np.sqrt(mean_squared_error(y_val, val_preds)))
        holdout_mae  = float(mean_absolute_error(y_val, val_preds))
        holdout_r2   = float(r2_score(y_val, val_preds))

        self.model = final.booster_
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

    def predict(self, texts: list[str], beat_numbers: list[int], total_beats: list[int]) -> np.ndarray:
        if self.model is None:
            raise RuntimeError("Model not trained. Call train() or load().")
        rows = []
        for text, bn, tb in zip(texts, beat_numbers, total_beats):
            f = self.extractor.extract_with_embeddings(text, bn, tb)
            rows.append(f.to_list())
        X = np.array(rows, dtype=np.float32)
        return self.model.predict(X)

    def shap_values(self, texts: list[str], beat_numbers: list[int], total_beats: list[int]) -> np.ndarray:
        """Returns SHAP values: shape (n_samples, n_features)."""
        import shap
        rows = []
        for text, bn, tb in zip(texts, beat_numbers, total_beats):
            f = self.extractor.extract_with_embeddings(text, bn, tb)
            rows.append(f.to_list())
        X = np.ndarray(rows, dtype=np.float32)
        explainer = shap.TreeExplainer(self.model)
        return explainer.shap_values(X)

    def top_negative_features(self, text: str, beat_number: int, total_beats: int, top_n: int = 3) -> list[tuple[str, float]]:
        """Return top N features driving the score DOWN for a single beat."""
        import shap
        f = self.extractor.extract_with_embeddings(text, beat_number, total_beats)
        X = np.array([f.to_list()], dtype=np.float32)
        explainer = shap.TreeExplainer(self.model)
        shap_vals = explainer.shap_values(X)[0]
        pairs = sorted(zip(self.feature_names, shap_vals), key=lambda x: x[1])
        return [(name, float(val)) for name, val in pairs[:top_n]]

    def save(self, path=None):
        path = path or BEAT_QUALITY_MODEL_PATH
        with open(path, "wb") as f:
            pickle.dump({
                "booster":     self.model,
                "feature_names": self.feature_names,
                "pca":         self.extractor._pca,
                "pca_fitted":  self._pca_fitted,
            }, f)
        console.print(f"[green]Model saved to {path}[/green]")
        if mlflow.active_run():
            mlflow.log_artifact(str(path))

    def load(self, path=None):
        path = path or BEAT_QUALITY_MODEL_PATH
        with open(path, "rb") as f:
            state = pickle.load(f)
        self.model = state["booster"]
        self.feature_names = state["feature_names"]
        if state.get("pca") is not None:
            self.extractor.load_embedder(EMBED_MODEL)
            self.extractor._pca = state["pca"]
            self._pca_fitted = True
        console.print(f"[green]Model loaded from {path}[/green]")
