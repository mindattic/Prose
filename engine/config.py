"""Engine configuration — paths, model settings, and constants."""

from pathlib import Path

# Project root
ROOT = Path(__file__).resolve().parent.parent

# Canon source directories (READ-ONLY to generators)
WORLDBUILDING_DIR = ROOT / "worldbuilding"
CHARACTERS_DIR = ROOT / "characters"
ESSENCES_DIR = ROOT / "essences"
NARRATIVE_BIBLE = ROOT / "narrative_bible.md"

# Engine data (built from canon, can be rebuilt at any time)
ENGINE_DATA_DIR = ROOT / "engine_data"
CHROMA_DIR = ENGINE_DATA_DIR / "chromadb"
GRAPH_PATH = ENGINE_DATA_DIR / "knowledge_graph.json"
ENTITY_REGISTRY_DIR = ENGINE_DATA_DIR / "registry"

# Story output (NEVER treated as canon)
STORIES_DIR = ROOT / "stories"
CANON_QUEUE_DIR = ROOT / "canon_queue"

# Embedding settings
CHUNK_SIZE = 500          # tokens per chunk (approximate via chars)
CHUNK_OVERLAP = 50        # overlap between chunks
CHARS_PER_TOKEN = 4       # rough approximation
CHUNK_SIZE_CHARS = CHUNK_SIZE * CHARS_PER_TOKEN
CHUNK_OVERLAP_CHARS = CHUNK_OVERLAP * CHARS_PER_TOKEN

# Collection name in ChromaDB
CANON_COLLECTION = "street_samurai_canon"

# LLM settings for validation
VALIDATOR_MODEL = "claude-sonnet-4-6"
VALIDATOR_TEMPERATURE = 0.1
VALIDATOR_MAX_TOKENS = 2048
