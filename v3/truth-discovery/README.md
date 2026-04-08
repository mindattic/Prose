# Fact Discovery Pipeline

A Python-based machine learning pipeline that reads 10,000+ worldbuilding entity files, extracts every factual claim as a Subject-Predicate-Object triple, groups semantically equivalent claims using vector embeddings, and determines consensus based on source agreement — then flags and optionally repairs inconsistencies.

**Three technologies in one system:**
- **LLM** (Claude API) — extracts structured claims from unstructured text
- **ML** (sentence-transformers + HDBSCAN) — embeds and clusters semantically similar claims
- **Statistical consensus** — determines what's "true" by source agreement, flags disagreements

## Quick Start

```bash
# Navigate to the pipeline
cd D:\Projects\MindAttic\StreetSamurai\v3\truth-discovery

# Run everything — extract, embed, cluster, score, and auto-repair
python run_pipeline.py

# When done, query the results
python query.py --stats                          # Dashboard of numbers
python query.py --flagged                        # All inconsistencies
python query.py "Arcturus Defense Solutions"      # Query a specific subject

# Refresh the app — Tools > Fact Discovery shows results
```

**One command does everything:** extraction, embedding, clustering, scoring, and auto-repair of 90%+ confidence fixes.

**Resume-safe:** Stop it anytime (Ctrl+C). Run `python run_pipeline.py` again — it picks up where it left off.

**Takes hours** for 10,000+ files (mostly API time for extraction). Prints progress every 100 files.

## Setup

```bash
cd v3/truth-discovery
pip install -r requirements.txt
cp .env.example .env
# Edit .env with your Anthropic API key
```

### Dependencies

| Package | Purpose |
|---------|---------|
| `anthropic` | Claude API for SPO triple extraction |
| `sentence-transformers` | all-MiniLM-L6-v2 model for embedding claims as vectors |
| `hdbscan` | Density-based clustering to group equivalent claims |
| `scikit-learn` | Supporting ML utilities |
| `numpy` | Vector math for embeddings |
| `python-dotenv` | Environment variable management |
| `rich` | Terminal UI (progress bars, tables, colored output) |

### Configuration (.env)

```
ANTHROPIC_API_KEY=sk-ant-...     # Your Claude API key
DATA_DIR=../../engine/data        # Path to entity JSON files
DB_PATH=facts.db                  # SQLite database for results
BATCH_SIZE=10                     # Files between rate-limit pauses
SIMILARITY_THRESHOLD=0.87         # Cosine similarity for claim matching
```

## Pipeline Phases

### Phase 1: Extraction (`extract.py`)

Sends each entity's JSON to the Claude API with a structured prompt that extracts atomic factual claims as Subject-Predicate-Object triples.

**Example:** Given a weapon entity with description "A reliable mid-range sidearm manufactured by Hearthstone Firearms, popular among Circuit workers", it extracts:
```json
[
  {"subject": "Hearthstone HM-7", "predicate": "is_a", "object": "pistol", "sentence": "Hearthstone HM-7 is a pistol"},
  {"subject": "Hearthstone HM-7", "predicate": "manufactured_by", "object": "Hearthstone Firearms", "sentence": "Hearthstone HM-7 is manufactured by Hearthstone Firearms"},
  {"subject": "Hearthstone HM-7", "predicate": "popular_among", "object": "Circuit workers", "sentence": "Hearthstone HM-7 is popular among Circuit workers"}
]
```

**Resume-safe:** Tracks which files have been processed. Restart anytime — it picks up where it left off. Checkpoints every 50 files.

```bash
python extract.py                    # Process all files
python extract.py --limit 50         # Test with 50 files
python extract.py --repo documents   # Only process one repo
python extract.py --dry-run          # Preview without API calls
```

### Phase 2: Embedding (`embed.py`)

Converts each triple's natural language sentence into a 384-dimensional vector using the `all-MiniLM-L6-v2` model from sentence-transformers. This enables semantic comparison — "manufactured by Arcturus" and "made by Arcturus" become nearly identical vectors even though the words differ.

**Resume-safe:** Only embeds triples that don't already have embeddings.

```bash
python embed.py
```

### Phase 3: Clustering (`cluster.py`)

Uses HDBSCAN (Hierarchical Density-Based Spatial Clustering) to group embedded triples that represent the same claim stated different ways. Each cluster = one "disputed fact" that multiple sources may agree or disagree on.

```bash
python cluster.py                          # Default settings
python cluster.py --min-cluster-size 3     # Adjust sensitivity
python cluster.py --min-samples 2
```

### Phase 4+5: Scoring & Flagging (`score.py`)

For each cluster of claims, counts how many unique sources assert each variant. The most-agreed-upon value becomes the **consensus**. Confidence = agreeing sources / total sources. Any triple that disagrees with consensus is flagged.

```bash
python score.py                        # Score with default threshold
python score.py --min-confidence 0.6   # Show contested claims below threshold
```

### Phase 6: Query (`query.py`)

CLI tool to search the results database. Query any subject, view flagged inconsistencies, or check pipeline statistics.

```bash
python query.py "Arcturus Defense Solutions"   # All claims about Arcturus
python query.py "Meridian 88"                  # All claims about the city
python query.py --flagged                      # Show all inconsistencies
python query.py --flagged --limit 20           # Top 20 inconsistencies
python query.py --stats                        # Pipeline statistics
```

### Phase 7: Repair (`repair.py`)

Reads flagged inconsistencies and optionally updates the source JSON files with consensus values. **Always preview with --dry-run first.**

```bash
python repair.py --dry-run                     # Preview all repairs
python repair.py --dry-run --min-confidence 0.9  # Preview high-confidence only
python repair.py --min-confidence 0.9          # Apply high-confidence repairs
python repair.py --min-confidence 0.9 --limit 50  # Apply first 50 repairs
```

### Master Runner (`run_pipeline.py`)

Executes all phases in sequence. Resume-safe — restart at any point.

```bash
python run_pipeline.py                    # Full pipeline (hours for 10k files)
python run_pipeline.py --limit 50         # Test with 50 files
python run_pipeline.py --repo documents   # Only one repo
python run_pipeline.py --skip-extract     # Re-run from embedding onward
python run_pipeline.py --phase score      # Run a single phase
python run_pipeline.py --dry-run          # Preview extraction only
```

## Database Schema (`db_schema.py`)

SQLite database (`facts.db`) with these tables:

| Table | Purpose |
|-------|---------|
| `triples` | Every extracted SPO claim with source file, embedding, and cluster assignment |
| `clusters` | Cluster metadata (representative sentence, member count, source count) |
| `fact_scores` | Consensus value per cluster with confidence score |
| `flagged_triples` | Claims that disagree with consensus, with correct value |
| `processing_log` | Audit trail of pipeline runs |

## C# Integration

The Blazor app includes a `FactDiscoveryService` that reads `facts.db` in read-only mode. No Python dependency at runtime. The Fact Discovery page (`/facts` under Tools) shows:

- Dashboard with stats (triples, sources, clusters, consensus claims, flags, confidence)
- Subject search with confidence scores
- Flagged inconsistencies panel

Run the Python pipeline periodically (after content generation), then refresh the app to see updated results.

## What It Catches

- **Contradictions:** File A says "manufactured by Arcturus", File B says "manufactured by TESSERA" for the same weapon
- **Date disagreements:** Three news articles date the same event differently
- **Orphaned references:** "Bore Rats" mentioned by 22 characters but no faction entry exists
- **Naming inconsistencies:** "Quanta" vs "Phi" for the same currency
- **Location mismatches:** Character's description says "Circuit" but their district field says "Laceworks"
- **Attribute drift:** An entity's properties described differently across documents

## Architecture

```
10,001 JSON entity files (engine/data/*/*.json)
    |
    v
[extract.py] -- Claude API -> SPO triples
    |
    v
[embed.py] -- sentence-transformers (all-MiniLM-L6-v2) -> 384-dim vectors
    |
    v
[cluster.py] -- HDBSCAN -> semantic grouping
    |
    v
[score.py] -- consensus vote + flagging
    |
    v
facts.db (SQLite)
    |
    v
[query.py] -- CLI search    [repair.py] -- fix source files
    |
    v
FactDiscoveryService (C#) -- Blazor UI dashboard
```
