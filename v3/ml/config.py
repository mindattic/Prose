"""Central configuration for the StreetSamurai ML package."""
import os
from pathlib import Path

ML_ROOT = Path(__file__).parent
ARTIFACTS = ML_ROOT / "artifacts"
ARTIFACTS.mkdir(exist_ok=True)

# SQL Server LocalDB — Windows Auth, no credentials needed.
DB_CONN_STR = (
    "Driver={ODBC Driver 17 for SQL Server};"
    "Server=(localdb)\\MSSQLLocalDB;"
    "Database=StreetSamurai;"
    "Trusted_Connection=yes;"
)

# Model artifact paths
BEAT_QUALITY_MODEL_PATH  = ARTIFACTS / "beat_quality_model.pkl"
TOPIC_MODEL_PATH         = ARTIFACTS / "topic_model"
BEATMODE_MODEL_PATH      = ARTIFACTS / "beatmode_classifier"
PERSONA_MODEL_PATH       = ARTIFACTS / "persona_models"
PERSONAS_JSON_PATH       = ARTIFACTS / "personas.json"

# Parquet cache paths (rebuilt each extraction run)
REVIEWS_PARQUET    = ARTIFACTS / "reviews.parquet"
BEAT_TEXTS_PARQUET = ARTIFACTS / "beat_texts.parquet"
TRAINING_PARQUET   = ARTIFACTS / "training_dataset.parquet"

# Scoring thresholds for Findings severity
BEAT_SCORE_HIGH_THRESHOLD   = 2.5   # predicted < 2.5  → High severity
BEAT_SCORE_MEDIUM_THRESHOLD = 3.0   # predicted 2.5–3.0 → Medium
BEAT_SCORE_LOW_THRESHOLD    = 3.5   # predicted 3.0–3.5 → Low  (below = flagged)

# Gripe topic: file a Finding when the topic appears in ≥ this % of a strand's gripes
GRIPE_TOPIC_MIN_PERCENT = 15.0

# LLM-rewrite feature (off by default; flip in local .env or here)
ML_REWRITE_ENABLED  = os.getenv("ML_REWRITE_ENABLED", "false").lower() == "true"
ML_REWRITE_MODEL    = os.getenv("ML_REWRITE_MODEL", "haiku")   # haiku | sonnet | deepseek-chat
ML_REWRITE_MAX_BEATS = int(os.getenv("ML_REWRITE_MAX_BEATS", "10"))

# Embeddings
EMBED_MODEL = "sentence-transformers/all-MiniLM-L6-v2"
EMBED_DIM   = 384
PCA_DIM     = 8    # PCA-reduced semantic features injected into LightGBM

# MLflow — SQLite backend (file-store deprecated in MLflow 2.x+)
MLFLOW_TRACKING_URI = f"sqlite:///{ARTIFACTS / 'mlflow.db'}"

# LightGBM defaults
LGBM_PARAMS = {
    "objective":             "regression",
    "metric":                "rmse",
    "num_leaves":            63,
    "learning_rate":         0.05,
    "feature_fraction":      0.8,
    "bagging_fraction":      0.8,
    "bagging_freq":          5,
    "min_child_samples":     20,
    "n_estimators":          500,
    "early_stopping_rounds": 50,
    "seed":                  42,
    "verbose":               -1,
}
