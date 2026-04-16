"""
Shared constants for all StreetSamurai Python pipelines.

Import from here instead of duplicating env reads and magic strings.
Usage: from constants import DATA_DIR, MODEL, CONCURRENCY, ...
"""

import os
from dotenv import load_dotenv

load_dotenv()

# ── Environment / Config ──────────────────────────────────────────────────
ANTHROPIC_API_KEY = os.getenv("ANTHROPIC_API_KEY", "")
DATA_DIR = os.getenv("DATA_DIR", "../../engine/data")
DB_PATH = os.getenv("DB_PATH", "facts.db")
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

# ── Route mapping: repo folder → Blazor route prefix ─────────────────────
REPO_ROUTES = {
    "people":         "/characters",
    "synthetics":     "/synthetics",
    "corponations":   "/corps",
    "places":         "/places",
    "factions":       "/factions",
    "weaponry":       "/weaponry",
    "equipment":      "/equipment",
    "technology":     "/technology",
    "cyberware":      "/cyberware",
    "apparel":        "/apparel",
    "genemods":       "/genemods",
    "pharmaceuticals":"/pharmaceuticals",
    "materials":      "/materials",
    "transportation": "/transportation",
    "automata":       "/automata",
    "documents":      "/documents",
    "ammunition":     "/ammunition",
    "consumer_goods": "/consumer-goods",
    "archetypes":     "/archetypes",
    "entertainment":  "/entertainment",
    "news":           "/documents",
    "subsidiaries":   "/factions",
    "contracts":      "/documents",
    "vocabulary":     "/vocabulary",
    "quotes":         "/documents",
}

# ── Text fields to scan for wiki links (used by wiki_scan.py) ────────────
WIKI_SCAN_FIELDS = [
    "description", "body", "background", "personality", "ideology",
    "founding_story", "key_detail", "cultural_context", "lore",
    "notes", "history", "functionality", "common_usage",
    "effect", "side_effects", "flavor_text",
]

# ── Minimum name length for wiki link auto-insertion ─────────────────────
WIKI_MIN_NAME_LENGTH = 4

# ── Repos excluded from wiki link index (too generic to auto-link) ────────
# materials → "Copper", "Gold", "Silver" are common nouns in narrative text
# archetypes → "Face", "Ghost", "Scholar" are role labels, not proper nouns
WIKI_SCAN_SKIP_REPOS: set[str] = {"materials", "archetypes"}

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
