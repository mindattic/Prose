"""Central configuration for the StreetSamurai ML package."""
import os
from pathlib import Path

ML_ROOT = Path(__file__).parent
_appdata = os.environ.get("APPDATA") or str(Path.home() / "AppData" / "Roaming")
ARTIFACTS = Path(_appdata) / "MindAttic" / "ML"
ARTIFACTS.mkdir(parents=True, exist_ok=True)

# SQL Server LocalDB — Windows Auth, no credentials needed.
DB_CONN_STR = (
    "Driver={ODBC Driver 17 for SQL Server};"
    "Server=(localdb)\\MSSQLLocalDB;"
    "Database=StreetSamurai;"
    "Trusted_Connection=yes;"
)

# Artifact paths (all under %APPDATA%\MindAttic\ML)
TOPIC_MODEL_PATH    = ARTIFACTS / "topic_model"
REGISTER_MODEL_PATH = ARTIFACTS / "register_classifier.pkl"
GRIPES_CACHE_PATH   = ARTIFACTS / "gripes.parquet"
BEATS_CACHE_PATH    = ARTIFACTS / "beats.parquet"

# Embedding model (cached locally by sentence-transformers)
EMBED_MODEL = "sentence-transformers/all-MiniLM-L6-v2"

# Gripe topic: file a Finding when a topic appears in >= this % of a strand's gripes
GRIPE_TOPIC_MIN_PERCENT = 15.0

# Register: flag bleed when confidence that a beat belongs to a *different* strand exceeds this
REGISTER_BLEED_MIN_CONF = 0.75

# Minimum beats a strand must have to participate in register training
REGISTER_MIN_BEATS_TO_TRAIN = 10
