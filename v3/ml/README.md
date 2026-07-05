# StreetSamurai ML Prose Engine

A self-adapting machine-learning layer that mines 21,985+ reader-persona reviews to improve
beat-level prose quality — retroactively auditing existing beats and preventively injecting
guidance into the ProseWriterRouter generation pipeline.

---

## How It Works (30-second version)

1. **Extract** — pull per-beat scores and gripe texts from the DB into Parquet files.
2. **Train** — LightGBM learns which prose features predict high/low reader scores; BERTopic
   clusters 21k+ gripe texts into recurring complaint categories; SetFit classifies beat mode
   from synopsis text.
3. **Audit** — score every current non-draft beat; any beat predicted below 3.5/5.0 gets a
   `Findings` row (`ML-PROSE-SCORE: Beat #N predicted 2.7/5 — weak on sentence_length_variance`).
4. **Inject** — `MlProseGuidanceService` reads those findings and surfaces the top 5 into
   `BeatContext.MlProseGuidanceContext`, which `ProseWriterRouter` injects into the system
   prompt before every beat generation.

No new tables. The existing `Findings` integration bus carries everything.

---

## Directory Layout

```
v3/ml/
├── config.py                  All thresholds, DB connection, artifact paths
├── db.py                      pyodbc connection factory (Windows Auth, LocalDB)
├── requirements.txt           All Python dependencies
│
├── extract/
│   ├── pull_reviews.py        → artifacts/reviews.parquet   (per-beat scores)
│   └── pull_beat_texts.py     → artifacts/beat_texts.parquet (temporal beat text reconstruction)
│
├── features/
│   └── text_features.py       ~45 numeric features per beat text (prose surface + semantic PCA-8)
│
├── models/
│   ├── beat_quality_model.py  LightGBM score regressor (target: MeanBeatScore 1–5)
│   ├── topic_model.py         BERTopic gripe miner (10–20 recurring complaint clusters)
│   ├── persona_preference_model.py  OCEAN→preference delta model (one per BeatMode)
│   └── beatmode_classifier.py SetFit beat-mode classifier (replaces keyword scan)
│
├── audit/
│   ├── beat_auditor.py        Scores all current beats → writes ML-PROSE-SCORE findings
│   └── findings_writer.py     SQL upsert contract mirroring C# FindingsService.Upsert
│
└── orchestrate/
    ├── nightly_run.py         Master orchestrator; argparse; MLflow tracking
    └── run_nightly.bat        Windows Task Scheduler wrapper (runs at 02:00 daily)
```

---

## Prerequisites

| Requirement | Check |
|---|---|
| Python 3.11+ | `python --version` |
| ODBC Driver 17 for SQL Server | `odbcad32` → check "Drivers" tab |
| .NET SDK (for `ss` CLI) | `dotnet --version` |
| LocalDB running | `sqllocaldb info MSSQLLocalDB` |

ODBC Driver 17 installer: https://aka.ms/odbc17  
*(If you have Driver 18, edit `config.py` and change the `Driver=` string to `{ODBC Driver 18 for SQL Server}`)*

---

## First-Time Setup

**Run once from `v3/ml/`:**

```powershell
cd D:\Projects\MindAttic\StreetSamurai\v3\ml

# 1. Create the virtual environment
python -m venv .venv

# 2. Install dependencies (~3–5 min, downloads ~2 GB of ML libs)
.venv\Scripts\pip install -r requirements.txt

# 3. Create the artifacts directory
mkdir -Force artifacts
```

---

## Night 1: First Training Run

These steps assume setup is complete. Total wall-clock: 2–4 hours.

```powershell
cd D:\Projects\MindAttic\StreetSamurai\v3\ml

# Export persona OCEAN profiles for the persona preference model
# (only needed once; re-export if PersonaLibrary is updated)
cd ..\StreetSamurai.Cli
dotnet run --project . -- --export-personas-json
cd ..\..\..\ml

# Run Phase 1 only: extract data + train the beat quality model + audit all beats
# This is the highest-value phase — do this first.
.venv\Scripts\python orchestrate\nightly_run.py --phases extract train_quality audit
```

You can leave this running overnight. Output appends to `ml_nightly.log` in the repo root.

**What to check the next morning:**

```powershell
# How many ML findings were written?
sqlcmd -S "(localdb)\MSSQLLocalDB" -d StreetSamurai -Q "SELECT COUNT(*), Severity FROM Findings WHERE Summary LIKE 'ML-PROSE-SCORE%' GROUP BY Severity"

# What did the model learn? (RMSE < 0.6 = good; < 0.5 = excellent)
# Open in browser: http://127.0.0.1:5000
.venv\Scripts\mlflow ui --backend-store-uri sqlite:///D:/Projects/MindAttic/StreetSamurai/v3/ml/artifacts/mlflow.db
```

---

## Night 2: Full Pipeline

After Night 1 results look good, run all phases:

```powershell
.venv\Scripts\python orchestrate\nightly_run.py --phases all
```

Phases in order:
| Phase | What it does | Approx time |
|---|---|---|
| `extract` | Pull scores + reconstruct beat texts from temporal DB | ~15 min |
| `train_quality` | Train LightGBM beat scorer (leave-one-strand-out CV) | ~30–60 min |
| `train_topics` | BERTopic on 21k+ gripe texts | ~20 min |
| `train_persona` | OCEAN preference delta models (one per BeatMode) | ~45 min |
| `audit` | Score all current beats; write/update Findings | ~60 min |
| `train_beatmode` | SetFit beat-mode classifier from BeatModeLog | ~20 min |

---

## Nightly Automation (Windows Task Scheduler)

After Night 2, register the nightly run to fire at 02:00 every night automatically:

```powershell
# Run from repo root (D:\Projects\MindAttic\StreetSamurai)
schtasks /Create /TN "StreetSamurai ML Nightly" /TR "D:\Projects\MindAttic\StreetSamurai\v3\ml\orchestrate\run_nightly.bat" /SC DAILY /ST 02:00 /RU "%USERNAME%" /RL HIGHEST /F

# Verify it was created
schtasks /Query /TN "StreetSamurai ML Nightly"

# Manually trigger (to test before sleeping)
schtasks /Run /TN "StreetSamurai ML Nightly"
```

To remove: `schtasks /Delete /TN "StreetSamurai ML Nightly" /F`

---

## Checking Results in the Blazor UI

ML findings appear in the same Findings panel as other findings (Category = "Other",
Summary prefix = "ML-PROSE-SCORE"). The Blazor findings view at `/findings` shows them with
severity chips (High/Medium/Low).

**High severity** (predicted < 2.5): fix before publishing — these are the worst-scoring beats
by reader consensus.  
**Medium** (2.5–3.0): recommended improvement.  
**Low** (3.0–3.5): advisory only.

Each finding's `SuggestedFix` field shows the top SHAP drivers: which prose features are pulling
the score down and what to do about them.

---

## Running a Manual Audit

To audit a single strand without waiting for the nightly run:

```powershell
# Via ss CLI
ss --ml-audit --slug ATTE

# Or directly via Python
cd v3/ml
.venv\Scripts\python audit\beat_auditor.py --slug ATTE

# All strands
.venv\Scripts\python audit\beat_auditor.py --all
```

Exit codes: `0` = clean, `1` = advisory (Low findings only), `2` = blocking (High finding present).

---

## How ML Guidance Enters Generation

Once findings exist in the DB, every new beat generation automatically picks them up through
`MlProseGuidanceService` → `ProseWriterRouter`:

1. `ProseWriterRouter.WriteAsync()` calls `MlProseGuidanceService.BuildGuidanceAsync(strandId)`.
2. The service queries `Findings WHERE Summary LIKE 'ML-PROSE-SCORE%' AND Status='New'`, takes
   the top 5 by severity.
3. The guidance block is injected into `BeatContext.MlProseGuidanceContext`.
4. `BeatGeneratorService` appends it to the system prompt after the emotional guidance block.

This means **after the first nightly run**, subsequent beat generation is already informed by
ML findings with no manual intervention.

---

## LLM Rewrite Feature (Off by Default)

The system can optionally generate Haiku rewrites for weak beats. This is **disabled by default**
to control cost. To enable, edit `config.py`:

```python
ML_REWRITE_ENABLED   = True          # enable Haiku rewrites
ML_REWRITE_MODEL     = "haiku"       # or "sonnet", "deepseek-chat"
ML_REWRITE_MAX_BEATS = 10            # max rewrites per nightly run
```

When enabled, a second Finding row is written per weak beat with prefix `ML-PROSE-REWRITE`,
containing the rewritten prose in `SuggestedFix`. Hint and rewrite rows are independently
dismissable in the Blazor findings panel.

---

## Feature Engineering (What the Model Sees)

~45 numeric features per beat text, in three groups:

**Positional (11):** `beat_position_ratio`, arc phase flags (act_1–5), is_opening, is_closing,
is_midpoint, beats_from_start, beats_from_end.

**Prose surface (18):** word_count, sentence_count, avg/max sentence length,
sentence_length_variance, dialogue_line_ratio, italics_count, paragraph_count,
punct_density, avg_word_length, type_token_ratio, has_action_verbs,
has_interior_markers, capitalization_ratio, quote_char_ratio.

**Mode signals (7):** soft-scored keyword hits for Combat/Emotional/Dialogue/Transition/
Revelation/Narrative, plus mode_certainty.

**Semantic PCA-8 (8):** `all-MiniLM-L6-v2` sentence embedding → PCA dimensionality reduction to
8 floats, capturing semantic similarity to high-scoring beats.

---

## Model Quality Targets

| Metric | Target | Meaning |
|---|---|---|
| Holdout RMSE | < 0.60 | Predicts reader scores within 0.6 points on unseen strands |
| Holdout R² | > 0.35 | Model explains > 35% of score variance |
| BERTopic coherence | > 0.40 | Topic labels are semantically coherent |
| SetFit F1 | > 0.82 | Beat-mode classification accuracy |

Check these in MLflow after each nightly run:
```powershell
.venv\Scripts\mlflow ui --backend-store-uri sqlite:///D:/Projects/MindAttic/StreetSamurai/v3/ml/artifacts/mlflow.db
```

---

## Selective Phase Reruns

To retrain only the beat quality model (e.g., after adding new reviews):

```powershell
.venv\Scripts\python orchestrate\nightly_run.py --phases train_quality audit
```

To audit a single strand without retraining:

```powershell
.venv\Scripts\python orchestrate\nightly_run.py --phases audit --strand ATTE --skip-retrain
```

To skip gripe mining (faster audit-only run):

```powershell
ss --ml-audit --slug BCODA --skip-gripes
```

---

## Troubleshooting

**`pyodbc.Error: Data source name not found`**  
ODBC Driver 17 is not installed. Download from https://aka.ms/odbc17 and install, then rerun.
If you have Driver 18, change the driver string in `config.py`.

**`ModuleNotFoundError: No module named 'lightgbm'`**  
The venv was not activated. Use `.venv\Scripts\python` explicitly (as shown above), or activate
first: `.venv\Scripts\Activate.ps1`.

**`[ml-audit] Python venv not found`** (from `ss --ml-audit`)  
The venv does not exist at `v3/ml/.venv`. Run the setup steps above.

**Nightly log shows `temporal reconstruction returned 0 rows`**  
The `Beats_History` temporal table may not have rows for that (StrandId, ContentHash) combination.
This is normal for strands with no review history — they are skipped gracefully.

**ML findings are not appearing in generation system prompts**  
Check that `MlProseGuidanceService` is registered (it is, in `ServiceCollectionExtensions.cs`)
and that `ProseWriterRouter` was injected with it. Verify via:
```powershell
ss --workflow-status --slug ATTE
```
The coverage matrix should show `MlProseGuidance` as an active service.

**High RMSE (> 0.8) after training**  
The model has too little data or the strand diversity is low. This improves automatically as more
reviews accumulate. Check that `pull_reviews.py` pulled > 5,000 per-beat rows; if fewer, the
extractor may have a join issue — check `ml_nightly.log`.
