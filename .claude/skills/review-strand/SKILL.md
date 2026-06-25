---
name: review-strand
description: Economical strand evaluation in one call — a sampled panel casts cheap score-ballots (overall + per-beat + one gripe), clusters them into a Pareto/contested/seam study, upgrades the most informative few to full prose, and prints one report with the strand score + 95% CI. Usage /review-strand [slug-or-id]; defaults to the most-recently-edited strand.
---

One call that grades a strand and tells you what to fix — cheaply. The default is a **sampled two-tier** run (NOT a 1024-persona census): a stratified panel casts score-only ballots, those ballots double as the segment study, and only the most informative handful get upgraded to readable prose. The user invokes it as `/review-strand <slug>` (slug optional).

## Fixed facts for this engine
- DB: `Server=(localdb)\MSSQLLocalDB;Database=StreetSamurai;Trusted_Connection=True;TrustServerCertificate=True;`
- CLI host project: `D:\Projects\MindAttic\StreetSamurai\v3\StreetSamurai.Blazor`
- **Invoke the CLI with `dotnet run --project <proj> -- <args>` directly. Do NOT use `ss.cmd`** — the shim mis-parses its own `rem` lines when spawned from a non-interactive shell and exits 255. Use `--no-build` if a VS/host instance holds the build lock (stop the host with `Get-Process StreetSamurai.Blazor | Stop-Process -Force` if you need a fresh build).
- **Providers: all trusted-4** (Claude, OpenAI, DeepSeek, Gemini), round-robined for model + temperament diversity. DeepSeek runs harsh and Gemini generous; the spread is expected — read the pooled mean, not any single provider. (Single chokepoint to narrow if ever needed: `StrandReviewService.ReviewProviderIds()`.)
- Reviews are **psychometric-grounded**: each persona reviews through its OCEAN/HEXACO/MBTI/Enneagram/DISC profile (delivered by the Legion package, injected via `BuildWhoBlock`).
- Reviews fingerprint the exact text via `ContentHash`. Pool/score ALWAYS by the ContentHash the run produced.
- **LATEST-RUN-ONLY for display/scoring**: dedupe to the most recent review per persona; never average stale opinions from the same persona across runs.

## Why sampled, not census
A score needs precision, not volume: with population SD ≈ 8, a sample of ~120 gives a 95% CI of ±~1.5 — tight enough to catch the ±2–3 pt moves edits produce. A 1024-census buys ±0.5 at ~8× the API calls (the per-call strand-input tokens dominate cost). And you only ever read a dozen reviews. So the default casts cheap **score-ballots** (just numbers + one gripe) wide, then writes **full prose** for only the most informative few.

## Steps when invoked

1. **Resolve the strand.** If an arg slug/id was given, use it. Otherwise default to the most-recently-edited:
   `SELECT TOP 1 Id, Slug, Title FROM Strands ORDER BY UpdatedAt DESC`. Echo which strand you're evaluating.

2. **GOTCHA — never edit beats during a run.** The run exports the strand text at its start; editing mid-run splits the panel across versions. If beats were just edited, confirm the edit is fully applied BEFORE starting.

3. **Run the default sampled pass** (one command — ballots + study + prose, all in one). Launch with `run_in_background: true`; wait for the completion notification (don't poll):
   ```
   dotnet run --project <proj> --no-build -- --review-strand --slug <slug>
   ```
   Knobs: `--ballots N` (default 120) sets the sample size; `--prose N` (default 10) sets how many full reviews to write. The command prints the strand mean + SD + 95% CI, cluster count, and the full Pareto/contested/seam report verbatim.

4. **Pool + verify** (PowerShell + System.Data.SqlClient — Unicode-safe; newest ContentHash first). The CLI already prints the headline number, but confirm/expand:
   - Newest ContentHash + ballot count (expect ≈ `--ballots`).
   - **Pooled mean, SD, 95% CI** (`±1.96*SD/sqrt(n)`).
   - **Provider split** (claude / openai / deepseek / gemini means) — the temperament gap (~9–12 pts; deepseek harshest, gemini most generous) usually dwarfs any panel gap. Read the pooled mean.
   - Compare vs the strand's prior-version means (other ContentHashes): did the change clear the ±CI band, or is it inside the noise?

5. **Per-beat + complaints.** The per-beat % is already computed onto each beat (from the ballots' 1–5 micro-scores). For "what's gone awry," cluster the ballots' one-line **weakness** tags (stored in `Improvements`) by LIKE-count prevalence, and read the ~`--prose` full reviews (the harshest / median / most-generous spectrum) for the *why*.

6. **Unified report.** Lead with the verdict (mean + CI vs prior versions: real move or noise?), then the study's fix-list (Pareto beats first — fix-for-everyone; then contested forks; then seams), then the clustered weakness tags, then the handful of full reviews. Be honest — if it regressed, name the beats/lines driving it so the author can override the panel where voice warrants.

## Modes / escape hatches
- **Default (bare)** → sampled two-tier. Best for routine "did my edit move the needle?" Cheap, a few minutes.
- `--ballots 60` → quicker pulse (CI ±~2.1) for fast iteration; `--ballots 200` for a milestone read.
- `--local` → run the ballots (and the synopsis) on a **local LLM** instead of the cloud trusted-4 — free, no API tokens. See "Local LLM mode" below. Applies to the default sampled path and `--by-act` only; `--census/--study/--group/--same-personas` always run cloud.
- `--census` → full-population pass: every enriched persona writes a full review (~1024). The old behavior — use only when you want the absolute-tightest number; expensive.
- `--group "Group X"` → focus-group tracking on a fixed roster (full reviews) across versions.
- `--same-personas` → re-run the exact personas from the strand's last batch (before/after focus group).
- `--study` → standalone segment study only (the default already folds this in).

## Local LLM mode (`--local`)
Routes review ballots to a local Ollama model instead of the paid cloud panel. The whole
review machinery (1024 personas, psychometric profiles, ballots, clustering, scoring,
synopsis) is unchanged — only the final HTTP hop changes. Cloud and local transports are
separate classes (`CloudReviewLlm` / `LocalReviewLlm` in `StreetSamurai.Core/Services/Local/`);
the cloud path and MindAttic.Legion itself are untouched.

**One-time setup (RTX 3080 Ti, 12 GB):**
```
winget install Ollama.Ollama          # or download from ollama.com
ollama pull qwen2.5:14b-instruct      # ~9 GB, q4 — best JSON + judgment for 12 GB
# Ollama defaults num_ctx to 2048, which TRUNCATES a strand. Bake a real context window:
#   Modelfile:
#     FROM qwen2.5:14b-instruct
#     PARAMETER num_ctx 16384
ollama create qwen2.5-14b-rev -f Modelfile
ollama serve                          # leave running
```
Defaults live in settings: `LocalReviewBaseUrl` (`http://localhost:11434/v1/chat/completions`),
`LocalReviewModel` (`qwen2.5-14b-rev`), `LocalReviewMaxConcurrency` (`2`). Override the model
for one run with `--local-model <tag>`.

**Run:**
```
dotnet run --project <proj> --no-build -- --review-strand --slug <slug> --local
```

**Caveats — read these before trusting the number:**
- **No model/temperament diversity** — one local model = one temperament. Diversity is
  persona + psychometric only. Absolute calibration differs from the cloud trusted-4 mean.
- **Separate baseline.** Local ballots are stamped `ProviderId = 'local'`. Do NOT compare a
  local strand mean against historical cloud means — start a fresh local baseline.
- **Speed.** Prefill (whole strand in) dominates; at concurrency 2 a 20-ballot pass is a few
  minutes. Free, not instant. Big strands still auto-route to the segmented path.
- If Ollama isn't running the run **fails fast** with an actionable error — it never silently
  falls back to the cloud.

## Reference: pooled-stats PowerShell skeleton
```powershell
$cs='Server=(localdb)\MSSQLLocalDB;Database=StreetSamurai;Trusted_Connection=True;TrustServerCertificate=True;'
Add-Type -AssemblyName System.Data
# newest hash:  SELECT TOP 1 ContentHash FROM StrandReviews WHERE StrandId=@sid ORDER BY ReviewedAt DESC
# pooled:       latest review per persona for that hash -> AVG/STDEV/COUNT, CI = 1.96*SD/sqrt(n)
# provider:     GROUP BY ProviderId  (claude vs openai)
# weakness tags: SELECT Improvements ... WHERE ContentHash=@h AND Improvements IS NOT NULL  (cluster by LIKE)
# full reviews:  WHERE ContentHash=@h AND LEN(ReviewText) > 0   (the ~10 prose upgrades)
```

## Notes
- The ballots ARE the study sample (they carry per-beat 1–5 scores), so the default already produces the clustering / Pareto / contested / seam analysis — no separate `--study` needed.
- Strand headline score = mean of the ballots (tagged `Group Sample <hash>` so `RecomputeScoresAsync` counts them); per-beat % = mean of the ballots' micro-scores mapped positional→SortKey order.
- If the score regressed and the weakness tags / prose blame specific lines, surface them by name — author voice can legitimately override the panel.
