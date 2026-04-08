# Truth Discovery Pipeline

Uses ML tools to read 9,600+ entity files, extract every factual claim as a Subject-Predicate-Object triple, group semantically equivalent claims, and determine what's true based on source consensus — then flag or correct inconsistencies.

## Setup

```bash
cd v3/truth-discovery
pip install -r requirements.txt
cp .env.example .env
# Edit .env with your Anthropic API key
```

## Pipeline Phases

| Phase | Script | What it does |
|-------|--------|-------------|
| 1. Extract | `extract.py` | Sends each entity to Claude API, extracts SPO triples |
| 2. Embed | `embed.py` | Converts triples to vector embeddings (all-MiniLM-L6-v2) |
| 3. Cluster | `cluster.py` | Groups equivalent claims via HDBSCAN |
| 4. Score | `score.py` | Determines ground truth by consensus vote |
| 5. Query | `query.py` | CLI tool to search claims and view flags |
| 6. Repair | `repair.py` | Fixes inconsistencies in source JSON files |

## Quick Start

```bash
# Test with 50 files first
python run_pipeline.py --limit 50

# Full pipeline (takes hours with 9,600 files)
python run_pipeline.py

# Query results
python query.py "Arcturus Defense Solutions"
python query.py --stats
python query.py --flagged

# Preview repairs
python repair.py --dry-run --min-confidence 0.8

# Apply repairs
python repair.py --min-confidence 0.9
```

## How It Works

1. **Extract**: Claude API parses each entity's description into atomic claims like `{subject: "KS-4 Knitter", predicate: "manufactured_by", object: "Arcturus Defense Solutions"}`
2. **Embed**: Each claim sentence is converted to a 384-dimensional vector using sentence-transformers. "manufactured by Arcturus" and "made by Arcturus" become nearly identical vectors.
3. **Cluster**: HDBSCAN groups claims that mean the same thing regardless of wording.
4. **Score**: Within each cluster, the most-agreed-upon value wins. Confidence = agreeing sources / total sources.
5. **Flag**: Any claim that disagrees with the consensus is flagged with the correct value.
6. **Repair**: Optionally update source files with consensus values.

## C# Integration

The pipeline writes results to `truth.db` (SQLite). The C# Blazor app can read this database to display truth scores, flags, and repair suggestions in the UI. No Python dependency needed at runtime — just run the pipeline periodically and the C# app reads the results.
