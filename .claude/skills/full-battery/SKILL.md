---
name: full-battery
description: Run the Full Battery — every QA/audit tool in the engine (census, coverage, plant-audit, timeline, verify, coordinate, examine-emotion, book-audit, diagnose, fidelity, logic-sweep, craft-checklist, check-canon, altitude-audit, reader-qa, storyscope, swain, chekhov) against a book via `prose --audit-book --full`, then fix, re-verify, validate, and re-export any book that was actually modified. Usage /full-battery [slug ...]; no argument = every book with live prose corpus-wide.
---

# /full-battery — the complete diagnostic + repair pass

There is no single existing command that runs every audit AND fixes/re-verifies/re-exports —
this skill is that command. It is a thin orchestration layer on top of tools that already
exist; it does not reinvent any of them. **Read-only diagnostics still come from
`prose --audit-book --full`** (`AuditNodeCli.cs` / `BookHealthService`, the "Player Piano") —
this skill adds the fix → re-verify → validate → re-export loop around it.

Never launch votes/panels/Legion review from this skill (SS-A44) — the battery below is scans
and findings, not opinion.

## What "Full Battery" means (the tiers, cumulative)

Driven by `prose --audit-book --full --slug <slug> --universe <u> --json --out <path>`:

- **FREE** (always): census (text/desc/struct/tone/score/EQ/mention coverage), coverage
  (per-service activation), plant-payoff audit, prose-pattern check, noun-consistency
  (`validate_nouns`), timeline-check, beat-verification (`BeatVerification`: BannedPattern /
  EventType / SubplotCarrier / EscalationFloor / DeclaredPurpose), outline-coordination.
- **DEEP** (`--deep`, implied by `--full`): examine-emotion, book-audit (Gateway/Sequel
  commandments), diagnose-node, check-fidelity (Semantic Fidelity Gap), Logic Sweep
  (six-dimension causality/knowledge/timeline/plant/orphan/bible), craft-checklist,
  check-canon, altitude-audit (10,000↔100 ft drift), reader-qa comprehension.
- **FULL** (`--full`): + storyscope-audit, swain-audit (Scene/Sequel doctrine), chekhov-audit.

Output: one **Structural Integrity Index (SII)** scorecard per book (deterministic Findings
rollup, never a vote) plus every underlying check's pass/fail detail.

## Steps

1. **Resolve scope.** Args = those slugs (with `--universe`). No args: every book with live
   prose corpus-wide —
   ```sql
   WITH Descendants AS (
     SELECT Id AS BookId, Id AS DescId FROM Nodes WHERE NodeType = 'book'
     UNION ALL SELECT d.BookId, n.Id FROM Nodes n JOIN Descendants d ON n.ParentNodeId = d.DescId)
   SELECT n.NodeCode, n.Slug, u.Slug AS Universe, COUNT(bn.BeatId) AS LiveBeats
   FROM Nodes n JOIN Universe u ON u.Id = n.UniverseId
   JOIN Descendants d ON d.BookId = n.Id
   JOIN BeatNodes bn ON bn.NodeId = d.DescId AND bn.IsEnabled = 1
   WHERE n.NodeType = 'book' GROUP BY n.NodeCode, n.Slug, u.Slug HAVING COUNT(bn.BeatId) > 0;
   ```
   One agent per book for FULL-tier runs (storyscope/swain/chekhov are multi-LLM-call and cost
   scales with book length — do not pair large books to save agent count; pairing only makes
   sense for the smallest books, e.g. <20 beats).

2. **AUDIT.** For each book:
   ```
   prose --audit-book --full --slug <slug> --universe <u> --json --out audit-outlines-<date>/battery/<CODE>.json
   ```
   Exit code: `0` clean, `1` blocking (High-severity) findings open, `2` SII < 60. Read the
   JSON `FindingsDeduction`/`RateAdjustments`/`Checks` — this is the authoritative "what's
   wrong" list; don't re-derive it by eyeballing prose.

3. **PULL THE FINDINGS LIST** scoped to this book. **HARD, ABSOLUTE (2026-08-22): nothing reaches
   the database except through Prose.Hub — reads AND writes, no exceptions.** The MCP
   `list_findings` inbox has no node filter, and no Hub-routed command currently scopes `Findings`
   (or `BeatVerifications`) to a book's leaf-descendant chapters. Until one exists, stop and tell
   the user this tooling gap exists rather than querying `Findings`/`BeatVerifications` directly —
   do not substitute a raw SQL read "just for triage." The join shape below (book → leaf-descendant
   ChapterIds → matching findings) is preserved as reference for building the real command.
   ```sql
   WITH Descendants AS (
     SELECT Id AS BookId, Id AS DescId FROM Nodes WHERE NodeCode = '<CODE>'
     UNION ALL SELECT d.BookId, n.Id FROM Nodes n JOIN Descendants d ON n.ParentNodeId = d.DescId)
   SELECT f.Id, f.Severity, f.Category, f.Status, f.Summary, f.SuggestedFix, f.ChapterId
   FROM Findings f JOIN Descendants d ON d.DescId = f.ChapterId
   WHERE f.Status IN ('New','Triaged') ORDER BY f.Severity DESC;
   ```

4. **TRIAGE.** BLOCKER/High: always fix. MODERATE: fix unless the call is a genuine editorial
   judgment (record it as deferred, don't force it). MINOR: fix only if it's a one-word/clause
   change; otherwise defer. If a finding can't be named concretely, drop it — never invent a
   fix for a vague complaint (`docs/LOGIC.md` §4, `feedback_storycraft_target_not_score`).

5. **FIX**, cheapest mechanism first:
   - Has a `SuggestedFix`/snippet the tool can locate → `apply_finding(id)` (writes the fix +
     backs up to `engine/data/archives/findings/` + marks Applied automatically).
   - Swain BLOCKER (Scene/Sequel doctrine failure) → `swain_repair(nodeIdOrSlug)` (splices the
     missing element only; set `useOpus:true` on a beat that resists a Sonnet pass).
   - Everything else (Logic Sweep findings, StoryScope anti-tells, craft-checklist, canon,
     comprehension defects) → hand-splice via `update_beat_text`, minimal-splice discipline:
     prefer data fix → clause → passage → full rewrite (`docs/LOGIC.md` §4). Bible wins on
     facts unless the finding proves the bible stale — fix the bible in the same change, then
     `generate_node_doc` + `codex.ps1 digest`/`doctor`.
   - No concrete fix possible → `set_finding_status(id, "Dismissed")` with a stated reason, or
     leave a `BeatVerification` row as-is (it will resurface next run if still real).

6. **RE-VERIFY** every beat you touched:
   - `validate_beat(beatId, checkBehavior:true)` — prose pattern guard + gear carry + behavior
     invariant, all in one call.
   - `validate_canon_text(text)` on the new passage — world-rule violations.
   - Any finding whose triage quoted beat text gets checked against the DB before being
     trusted (`verify_quote_grounding` / `verify_quote_grounding_batch`) — same mechanical
     gate the Logic Sweep uses (`docs/LOGIC.md` §SS-LOGIC-4a).

7. **VALIDATE** the whole book once fixes land: `validate_nouns(nodeIdOrSlug)` (deprecated/stale
   name sweep) + a fresh `prose --audit-book --full --slug <slug> --universe <u>` pass. Compare
   SII before/after — it must not regress on any dimension you didn't touch. If it does, you
   introduced a new defect; find and fix it before closing the book out.

8. **RE-EXPORT — only books that actually changed.** Track whether step 5 wrote any
   `update_beat_text`/`apply_finding`/`swain_repair` call for this book. If yes:
   ```
   prose --export-node --slug <slug> --universe <u>
   ```
   (This itself re-checks mojibake + BLOCKER `BeatVerification` gate before writing
   docx/epub/pdf/txt/description/synopsis/keywords/cover/dcm-viz — if it fails, step 6/7 missed
   something; go back.) Books with zero fixes applied are already current — do not re-export
   them just because the battery ran.

9. **CLOSE.** Write `audit-outlines-<date>/battery/CORPUS-REPORT.md`: per book — SII before →
   after, findings by severity (fixed / deferred / dismissed), whether it was re-exported.
   Flag any book where SII regressed or a fix couldn't be verified clean — those need a human
   decision, not a forced pass.

## Cost note

FULL tier is the heaviest audit tier in the engine — `storyscope_audit`/`swain_audit`/
`chekhov_audit` each make multiple LLM calls scaling with book length. Corpus-wide (every book,
`--full`) is a large, multi-hour, real-money undertaking, not a free mechanical check. Only run
corpus-wide when the user has explicitly asked for the whole corpus (as opposed to touched/
flagged books only, which is what `/logic-sweep` with no args already scopes cheaply).
