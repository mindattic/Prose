"""
Shared constants for all StreetSamurai Python pipelines.

Import from here instead of duplicating env reads and magic strings.
Usage: from constants import DATA_DIR, MODEL, CONCURRENCY, ...
"""

import os
import json
from pathlib import Path
from dotenv import load_dotenv

load_dotenv()  # honour an explicit ANTHROPIC_API_KEY override / CI secret


def _vault_claude_key() -> str:
    """Resolve the Claude API key from the canonical MindAttic Vault store.

    No secret lives in this repo. Resolution mirrors the C# apps:
      1. ANTHROPIC_API_KEY environment variable (CI / explicit override)
      2. %APPDATA%\\MindAttic\\LLM\\providers.json -> ["claude"]["apiKey"]
         (honours MINDATTIC_VAULT_ROAMING_ROOT, like MindAttic.Vault)
    """
    env = os.getenv("ANTHROPIC_API_KEY")
    if env:
        return env
    root = os.getenv("MINDATTIC_VAULT_ROAMING_ROOT") or os.path.join(
        os.getenv("APPDATA") or str(Path.home() / ".config"), "MindAttic")
    providers = Path(root) / "LLM" / "providers.json"
    try:
        data = json.loads(providers.read_text(encoding="utf-8-sig"))
    except (OSError, ValueError):
        return ""
    return ((data.get("claude") or {}).get("apiKey") or "").strip()

# ── Environment / Config ──────────────────────────────────────────────────
ANTHROPIC_API_KEY = _vault_claude_key()
DATA_DIR = os.getenv("DATA_DIR", "../../engine/data")
DB_PATH = os.getenv("DB_PATH", "lore-triples.db")
CONCURRENCY = int(os.getenv("CONCURRENCY", "20"))
MODEL = os.getenv("MODEL", "claude-haiku-4-5-20251001")

# ── Entity repos (all subdirectories under engine/data) ───────────────────
REPOS = [
    "people", "synthetics", "corponations", "places", "factions", "weaponry",
    "equipment", "technology", "cyberware", "ammunition", "apparel",
    "archetypes", "automata", "entertainment", "genemods", "materials",
    "news", "pharmaceuticals", "consumer_goods", "quotes", "subsidiaries",
    "transportation", "vocabulary", "documents", "contracts",
]

# ── Protected characters (skip during surname/description regeneration) ───
SKIP_CHARACTERS = {"Kyle Ellen Corbin-Vasik"}

# ── Non-human species (skip genetic ancestry assignment) ──────────────────
NON_HUMAN_SPECIES = {
    "android", "robot", "rogue_ai", "corporate_ai", "emergent_ai",
    "distributed_ai", "cyborg_ai", "artificial intelligence",
}

# ── Reference fields to convert from names to GUIDs ──────────────────────
REFERENCE_FIELDS = {
    "related_entities": "list",
    "known_users": "list",
    "parent_corponation": "string",
    "manufacturer": "string",
    "primary_weapon": "string",
    "secondary_weapon": "string",
    "armor": "string",
    "vehicle": "string",
    "favorite_drink": "string",
    "favorite_food": "string",
    "stimulant": "string",
    "comm_device": "string",
    "signature_gear": "list",
    "pharmaceuticals": "list",
}
