---
name: review-strand
description: Full strand evaluation in one call — runs the A/B/C reader panels, computes the pooled score + CI, runs the segment study (Pareto/contested/seam diagnostic), pulls the bottom-decile complaints, and prints one unified report. Usage /review-strand [slug-or-id]; defaults to the most-recently-edited strand.
---

One call that grades a strand and tells you what to fix. Runs the whole evaluation pipeline in the correct order and returns a single report. The user invokes it as `/review-strand <slug>` (slug optional).

## Fixed facts for this engine
- DB: `Server=(localdb)\MSSQLLocalDB;Database=StreetSamurai;Trusted_Connection=True;TrustServerCertificate=True;`
- CLI host project: `D:\Projects\MindAttic\StreetSamurai\v3\StreetSamurai.Blazor`
- **Invoke the CLI with `dotnet run --project <proj> -- <args>` directly. Do NOT use `ss.cmd`** — the shim mis-parses its own `rem` lines when spawned from a non-interactive shell and exits 255.
- A/B/C are three FIXED, DISJOINT 128-persona focus groups ("Group A"/"Group B"/"Group C"). They already exist. If a `--make-group` is ever needed: `dotnet run --project <proj> -- --make-group --name "Group X" --size 128`.
- Reviews fingerprint the exact text via `ContentHash`. Pool/score ALWAYS by the ContentHash the run produced, so a re-run on edited text never mixes versions.

## Steps when invoked

1. **Resolve the strand.** If an arg slug/id was given, use it. Otherwise default to the most-recently-edited strand:
   `SELECT TOP 1 Id, Slug, Title FROM Strands ORDER BY UpdatedAt DESC`. Echo which strand you're evaluating.

2. **GOTCHA — never edit beats during a run.** Each panel exports the strand text at its own start; editing mid-run splits panels across versions. If beats were just edited, confirm the edit is fully applied BEFORE starting. Run the three panels **sequentially, in the background** (sequential avoids tripling concurrent API load):
   ```
   foreach ($g in 'Group A','Group B','Group C') {
     dotnet run --project <proj> -- --review-strand --slug <slug> --group $g
   }
   ```
   Launch with `run_in_background: true`; wait for the completion notification (don't poll).

3. **Pool + score** (PowerShell + System.Data.SqlClient — Unicode-safe; identify the newest ContentHash first, then stat it). Report:
   - Newest ContentHash for the strand and its review count (expect ~384).
   - **Pooled mean, SD, and 95% CI** (`±1.96*SD/sqrt(n)`).
   - **Per-panel** A/B/C mean/SD/min/max and the cross-panel spread (agreement check).
   - **Provider split** (deepseek/openai/claude/gemini means) — the temperament gap (~12 pts) usually dwarfs the panel gap; deepseek is the toughest critic, gemini the most generous.
   - Compare the pooled mean vs the strand's prior-version means (other ContentHashes), and state whether the change cleared the ±CI band or was inside the noise.

4. **Segment study** (the diagnostic — market-segmentation + social-choice + welfare + multi-objective):
   `dotnet run --project <proj> -- --review-strand --slug <slug> --study`
   Print its report verbatim: emergent audience clusters, **Pareto-improving** beats (fix-for-everyone, no tradeoff — do these first), **contested** beats (real forks, who-gains/who-loses), **seams** (transition/tissue), and the flow-vs-enjoyment guard.

5. **Bottom-decile complaints** — pull the lowest ~10% of reviews for the newest ContentHash with provider + improvement notes, and cluster the recurring complaints (theme prevalence across ALL notes via LIKE counts is a good second cut). This is the "what's gone awry" view.

6. **Unified report.** Lead with the verdict (pooled mean + CI vs prior versions: real move or noise?), then per-panel + provider, then the study's fix-list, then the clustered bottom-decile complaints. Be honest — if it regressed, say so and name the lines/beats driving it.

## Reference: pooled-stats PowerShell skeleton
```powershell
$cs='Server=(localdb)\MSSQLLocalDB;Database=StreetSamurai;Trusted_Connection=True;TrustServerCertificate=True;'
Add-Type -AssemblyName System.Data
$conn=New-Object System.Data.SqlClient.SqlConnection $cs; $conn.Open()
# newest hash for the strand:
#   SELECT TOP 1 ContentHash FROM StrandReviews WHERE StrandId=@sid ORDER BY ReviewedAt DESC
# per-panel:  GROUP BY FocusGroupName  -> AVG(CAST(Score AS FLOAT)), STDEV(...), COUNT(*)
# provider:   GROUP BY ProviderId
# bottom decile: SELECT TOP (n/10) Score, ProviderId, Improvements ... ORDER BY Score ASC
$conn.Close()
```

## Notes
- The plain A/B/C panels are pure measurement (score + CI). The `--study` pass is the only mode that runs the clustering / Pareto / contested / seam analysis.
- If the score regressed and the bottom-decile blames specific lines, surface them by name so the author can decide line-by-line — author voice can legitimately override the panel.
