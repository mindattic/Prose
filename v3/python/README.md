# v3/python — SPO triple pipeline

A Python-based consistency pipeline for the StreetSamurai canon. Reads entity JSON files from `engine_data/`, extracts every factual claim as a Subject-Predicate-Object triple via the Claude API, clusters semantically equivalent claims using sentence-transformers + HDBSCAN, and flags inconsistencies by consensus vote. Auto-repair applies high-confidence fixes back to the source files.

**Three technologies in one system:**
- **LLM** (Claude API) — extracts structured claims from unstructured text
- **ML** (sentence-transformers + HDBSCAN) — embeds and clusters semantically similar claims
- **Statistical consensus** — determines what is "true" by source agreement, flags disagreements

The pipeline is resume-safe: stop it at any point and re-run — it picks up where it left off.

## Setup

```bash
cd v3/python
pip install -r requirements.txt
cp .env.example .env
# Edit .env with your Anthropic API key
```

### Dependencies (`requirements.txt`)

| Package | Purpose |
| --- | --- |
| `anthropic` | Claude API for SPO extraction |
| `sentence-transformers` | `all-MiniLM-L6-v2` for 384-dim embeddings |
| `hdbscan` | Density-based clustering |
| `scikit-learn` | Supporting ML utilities |
| `numpy` | Vector math |
| `python-dotenv` | `.env` loading |
| `rich` | Terminal progress bars and tables |
| `httpx` | HTTP client |

### Configuration (`.env`)

```
ANTHROPIC_API_KEY=sk-ant-...
DATA_DIR=../../engine_data
DB_PATH=lore-triples.db
BATCH_SIZE=10
SIMILARITY_THRESHOLD=0.87
```

## Quick start

```bash
# Full pipeline — extract, embed, cluster, score, auto-repair
python run_pipeline.py

# Query results
python query.py --stats          # dashboard
python query.py --flagged        # all inconsistencies
python query.py "Entity Name"    # all claims about one entity
```

**Takes hours** for 10,000+ files (API time dominates). Prints progress every 100 files.

## Pipeline phases

| Phase | Script | What it does |
| --- | --- | --- |
| 1 | `extract.py` | Claude API → SPO triples per entity file |
| 2 | `embed.py` | sentence-transformers → 384-dim vectors |
| 3 | `cluster.py` | HDBSCAN → semantic groupings |
| 4+5 | `score.py` | consensus vote, confidence scoring, flag disagreements |
| 6 | `query.py` | CLI search over results |
| 7 | `repair.py` | Apply high-confidence fixes to source JSON files |

```bash
python run_pipeline.py --limit 50         # test with 50 files
python run_pipeline.py --skip-extract     # re-run from embedding onward
python run_pipeline.py --phase score      # single phase
python run_pipeline.py --dry-run

python repair.py --dry-run                # always preview before applying
python repair.py --min-confidence 0.9     # apply high-confidence repairs
```

## Database (`lore-triples.db`)

SQLite. Tables: `triples`, `clusters`, `fact_scores`, `flagged_triples`, `processing_log`.

## C# integration

`LoreTripleService` in the Blazor app reads `lore-triples.db` in read-only mode at runtime. No Python dependency at runtime. The `/lore-triples` Tools page shows the dashboard, subject search, and flagged inconsistencies panel.

Run the pipeline periodically after content generation, then reload the app.

## Other scripts

| Script | Purpose |
| --- | --- |
| `generate_character_images.py` | Midjourney-style image prompts for character entities |
| `generate_descriptions.py` | Regenerate entity descriptions via Claude |
| `consistency_check.py` | Ad-hoc consistency sweep |
| `tools.bat` | Convenience launcher for common commands |
