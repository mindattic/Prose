---
name: completeness
description: Build the all-universe Story Completeness dashboard as an HTML artifact — per-story coverage (bible↔blueprint↔beat), chapter count, blueprint granularity, open gaps, node score, standard deviation, and ballot count, across every universe. Usage /completeness [--fresh]. --fresh re-runs coordination first (no LLM); default reads the latest reports/coordination/*.json.
---

# /completeness — Story Completeness Dashboard

Produce ONE visual dashboard covering **every story in every universe**: how complete each
story is across the three coordinates (MEANING = bible, CONSTRUCTION = blueprint, PROSE = beat),
plus its review score, spread, and structure. Read-only. NO votes, NO panels, NO LLM.

## Fixed facts
- DB (read-only): `sqlcmd -S "(localdb)\MSSQLLocalDB" -d StreetSamurai -Q "<query>"` (Windows Auth).
- Story nodes = `Nodes.NodeType='story'` (TPH discriminator; in EF, `db.Nodes.OfType<StoryNode>()`).
- Coordination is `ss --coordinate --slug <slug>` — read-only, no LLM. It writes
  `reports/coordination/<CODE>.coordination.json` and stamps `## Beat Coordination Index` into
  the node bible. Run against the **built** DLL for speed:
  `DLL=$(ls v3/StreetSamurai.Cli/bin/Debug/net*/StreetSamurai.Cli.dll | head -1)` then
  `dotnet "$DLL" --coordinate --slug <slug>` with `export Logging__LogLevel__Default=Warning
  Logging__LogLevel__Microsoft=Warning` to mute EF chatter. Build once if missing:
  `dotnet build v3/StreetSamurai.Cli -c Debug -v q --nologo`.
- Score lives on `Nodes.Score` (trimmed panel figure). SD + ballot count are NOT in
  `NodeScoreHistory` (that table is empty) — compute them from the individual persona ballots in
  `NodeReviews`, EXCLUDING failed 0-score ballots (draft contamination inflates SD to 30-40).
- Coordination flags: `MISSING_MEANING` (Beat.Description empty), `NO_CONSTRUCTION` (beat not
  mapped to a blueprint slice), `NO_PROSE`, `STUB_PROSE`, `UNSCORED`. **`UNSCORED` is by design** —
  scoring is at the node level (panels), never per beat, so it appears on every story. Report it
  under the Score columns, not as a coverage gap.
- Invariant (enforced): **every story has ≥1 chapter**; a single-chapter story prints no
  "Chapter 1". Show the chapter count so a regression (a flat story) is visible at a glance. Fix a
  flat story with `ss --ensure-chapter --slug <slug>` (or `--all`).

## Steps

1. **Resolve mode.** `--fresh` → re-run `ss --coordinate` for every story first (cheap, no LLM),
   so coverage reflects the current DB. Default → read the existing
   `reports/coordination/<CODE>.coordination.json` files (fast). If a story has no report yet,
   coordinate it regardless.

2. **List stories + structure + score.** One query gives universe, code, chapter count, direct-beat
   count (should be 0 — nonzero = flat story regression), and Node.Score:
   ```sql
   SELECT u.Slug AS uni, n.NodeCode AS code, n.Title,
     (SELECT COUNT(*) FROM Nodes c WHERE c.ParentNodeId=n.Id AND c.NodeType='chapter') AS chapters,
     (SELECT COUNT(*) FROM BeatNodes bn WHERE bn.NodeId=n.Id AND bn.IsEnabled=1) AS directBeats,
     CAST(n.Score AS decimal(6,2)) AS score
   FROM Nodes n JOIN Universe u ON n.UniverseId=u.Id
   WHERE n.NodeType='story' ORDER BY u.Slug, n.NodeCode;
   ```

3. **SD + ballots (clean).** Compute mean/SD/ballot count from valid ballots only:
   ```sql
   SELECT n.NodeCode,
     COUNT(CASE WHEN r.Score>0 THEN 1 END) AS ballots,
     COUNT(CASE WHEN r.Score=0 THEN 1 END) AS failed,
     CAST(AVG(CASE WHEN r.Score>0 THEN CAST(r.Score AS float) END) AS decimal(6,2)) AS mean,
     CAST(STDEV(CASE WHEN r.Score>0 THEN CAST(r.Score AS float) END) AS decimal(6,2)) AS sd
   FROM Nodes n LEFT JOIN NodeReviews r ON r.NodeId=n.Id
   WHERE n.NodeType='story' GROUP BY n.NodeCode;
   ```

4. **Coverage + gaps per story.** From each `reports/coordination/<CODE>.coordination.json`:
   `totalBeats`, `covered`, `storyScope.HasBlueprint`, `storyScope.Granularity`, and `flagCounts`
   (surface `MISSING_MEANING`, `NO_CONSTRUCTION`, `NO_PROSE`, `STUB_PROSE`; fold `UNSCORED` into the
   score side). Coverage % = `covered / totalBeats`.

5. **Render ONE artifact.** Load the `artifact-design` skill FIRST, then write an HTML file to the
   scratchpad dir and publish it with the Artifact tool. Requirements:
   - Grouped by universe; one row per story, sorted by score descending within each universe.
   - Columns: Story (code + title) · Chapters · Beats · Coverage % · Blueprint (granularity) ·
     Open gaps (the non-UNSCORED flags, "—" when none) · Score · SD · Ballots · State.
   - State badge: ✅ aligned (100% covered, blueprint present); ⚠️ gap (coverage <100% or a
     structural flag); ❌ missing blueprint or flat story (directBeats>0 or chapters=0).
   - Color-code coverage and SD; make the flat-story / missing-blueprint cases impossible to miss.
   - Theme-aware (light + dark), responsive, self-contained (no external assets). Favicon 📊.
   - A one-line legend: "Coverage = beats where MEANING+CONSTRUCTION+PROSE align. UNSCORED is
     by-design (node-level panels). Every story must have ≥1 chapter."

6. **Summarize in chat** (below the artifact link): count of aligned vs gapped stories, any flat
   stories or missing blueprints, and the highest-variance stories (largest SD).

## Notes
- Blueprint granularity drives beat↔construction indexing: `beat` = beat-parallel arrays, `chapter`
  = chapter-parallel. Coordination handles both; you only report what it emits.
- To close gaps (not this skill's job — it only reports): `ss --backfill-meaning` for
  MISSING_MEANING, `ss --generate-blueprint --retrofit` for NO_CONSTRUCTION, `ss --ensure-chapter`
  for a flat story.
