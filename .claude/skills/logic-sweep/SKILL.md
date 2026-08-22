---
name: logic-sweep
description: Run the canonical Logic Sweep (docs/LOGIC.md, SS-A44) on one or more books — six-dimension logic/continuity audit, severity triage, minimal-splice fixes, verification. NO votes, NO panels. Usage /logic-sweep [slug ...]; no argument = every book with beats changed since its last sweep.
---

# /logic-sweep — the canonical QA pass (SS-A44)

The law lives in `docs/LOGIC.md`. This skill is the runbook. **Never launch votes, panels,
or reviews from this skill** — if the user wants a score, they will say so explicitly.

## Fixed facts
- **HARD, ABSOLUTE (2026-08-22): nothing reaches the database except through Prose.Hub — reads
  AND writes, no exceptions.** An earlier version of this section instructed running raw `sqlcmd`
  reads directly against `(localdb)\MSSQLLocalDB` for audit reading — that is retired and must
  never be resurrected. **No Hub-routed command currently exists for bulk-reading a book's beats
  (Number/Id/Text in reading order) for audit purposes.** Until one does, do not run this skill's
  AUDIT step by querying the database yourself in any form — stop and tell the user this tooling
  gap exists so a proper `prose --export-beats`-style command (or MCP equivalent) can be built.
  Do not substitute a raw SQL read "just this once" to keep a sweep moving.
- Live book = enabled beats in reading order: chapter children by `Nodes.SortKey`, beats within
  by `BeatNodes.SortKey` — NEVER order by `Beats.Number` (not chapter-local; see project memory
  on cross-book `Beat.Number` confusion). This describes the correct ordering ANY tool must use,
  not an invitation to hand-write the join yourself.
- Reports → `audit-outlines-<today>/logic/<CODE>.md`. Fix files → `.../fixes/<CODE>/`.
- Text pushes via built exe `v3\Prose.Cli\bin\Release\net10.0\Prose.Cli.exe
  --beat update --id <guid> --text -` with OS-level `<` redirection (PowerShell pipelines inject
  BOM; verify `UNICODE(SUBSTRING(Text,1,1)) != 65279` after every push). One CLI invocation at a
  time (port). `dotnet run` may silently drop args — prefer the exe.
- NEVER `DELETE` from Nodes/Beats/NodeBeats, ever, from any surface. Soft-disable goes through the
  CLI/MCP disable path, not a hand-written `UPDATE`.
- Doc edits → `powershell -File tools/codex.ps1 digest` then `doctor` — must PASS.

## Steps

1. **Resolve scope.** Args = those slugs. No args: find books whose beats changed since their
   last sweep (compare `Beats.UpdatedAt` / `NodeBeats` changes against the book's most recent
   `audit-outlines-*/logic/<CODE>.md` date; when in doubt, include it). Small edit → scoped sweep
   (touched beats + blast radius); structural change (merge/disable/reorder) → full-book sweep.
   **Blast radius now runs automatically on every beat save (2026-08-14)** — `NodeWorkbenchService.
   UpdateBeatTextAsync` fires a narrow `LogicSweepService.RunNarrowAsync` over
   `BlastRadiusService`'s beat set after every edit, filed under its own `beat:{id}:blast` scope.
   This step's manual scoping is about the FULL SWEEP's own coverage, not a substitute for that —
   the auto-check catches a fix's own regressions in the same turn; this step still decides how
   much of the book a human/agent-driven sweep round reads.

2. **AUDIT (report-only agents, one per 1–2 books, Sonnet).** Each agent reads the book
   end-to-end and reports on the six dimensions of `docs/LOGIC.md` §3:
   causality chain / knowledge states (incl. names-before-coining) / timeline (respect
   bible-marked deliberate impossibilities) / plant-payoff ledger incl. arithmetic walked
   end-to-end / orphan references (grep distinctive phrases of every disabled beat; check
   IsChapterStart stranded on disabled beats) / bible agreement.
   Findings: severity (BLOCKER/MODERATE/MINOR) + SortKeys + quoted text + minimal fix proposal.
   Instruct verbatim: "Do not invent problems — if the logic holds, say so."
   Bible agreement is checked ACROSS ALTITUDES (docs/LOGIC.md §8): `prose --altitude-audit
   --slug <slug>` automates the 10,000↔100 ft comparison (bible/blueprint vs chapter
   synopses; findings filed as OutlineDrift); agents may read the book's
   `story-synopsis.txt` (or `NodeChapterSummaries`) for cheap chapter-altitude scoping
   before deep beat reads.
   Cross-book canon (shared characters, series arcs): one additional cross-read agent over the
   affected set when a series book is in scope.

3. **VERIFY QUOTE GROUNDING (mechanical, before triage — docs/LOGIC.md §SS-LOGIC-4a).**
   Every finding that quotes beat text gets checked against the DB before it's trusted, not
   after: build the claim list (BeatId + quoted text) from all audit reports and run
   `prose --verify-quotes-batch --json-file <path>` (array of `{"beatId":"<guid>","quote":"<text>"}`)
   — or `prose --verify-quote --id <beatId> --quote "<text>" --claimed-by "<agent>"` one at a time.
   MCP: `VerifyQuoteGroundingBatch` / `VerifyQuoteGrounding`. Any Fail = that finding is
   misattributed or fabricated — drop it from triage and, if it seems worth chasing, re-read
   the actual beat yourself before deciding. This is cheap and mechanical; run it on every
   quoted finding, not just the ones that feel off.

4. **TRIAGE.** Fix all BLOCKERs; MODERATEs almost always; MINORs when one word/clause. If a
   finding can't be named concretely, drop it. Editor-taste items go to the deferred ledger,
   not the fix pass.

5. **FIX (separate agents, the audit report as input).** Minimal-splice discipline per
   `docs/LOGIC.md` §4: prefer data fix → clause → passage → rewrite; reassign to established
   cast, reconcile counts to the load-bearing version; bible wins on facts unless the finding
   proves the bible stale — then fix the bible in the same change (+ digest/doctor).

6. **VERIFY (inside each fix pass).** Changed passages re-read with neighbors; old-defect
   greps = 0; disabled-content greps = 0; repaired arithmetic walked and printed; BOM checks;
   doctor PASS. Honest reporting: deviations and judgment calls are stated, never silently
   forced to satisfy a checklist.

7. **CLOSE.** Write/refresh `audit-outlines-<today>/logic/<CODE>.md` verdicts and, for
   multi-book sweeps, a `CORPUS-REPORT.md`. Summarize per book: verdict, findings by
   severity, fixes applied, anything deferred.

8. **CONVERGENCE (docs/LOGIC.md §9) — replaces "run it again."** Do not decide "is this book
   done" by running a fixed number of rounds — that was never a real stopping criterion (five
   rounds run, a sixth still finds something new is the exact failure mode this step exists to
   fix). After step 6/7 completes for a round, call
   `prose --logic-sweep --slug <slug> --until-dry` (one round of the persisted convergence
   campaign; MCP: `logic_sweep_until_dry`). It reports one of three things:
   - **skipped** — already converged, nothing changed since the last dry round. Nothing to do.
   - **not yet converged** — run another AUDIT→TRIAGE→FIX→VERIFY cycle (steps 2–6) on what it
     found, then call `--until-dry` again. A round that finds nothing resets the count toward
     convergence; a round that finds something (including a NEW regression from the last fix
     pass) resets the streak back to zero — that's intentional, not a bug.
   - **hit_safety_cap** — 8 rounds without ever reaching 2 consecutive clean rounds. Filed as its
     own finding (`LOGICSWEEP-CONVERGENCE [not-converging]`). Stop patching individual findings
     and read them for a common thread — this book needs a structural rewrite of the offending
     section, not another one-clause splice.
   A book is publish-ready only when this step reports **converged** AND the fact ledger
   (`prose --continuity extract --node <slug>` once, then check for open `CONTRADICTED` claims)
   has no open contradictions AND Reader-Proxy QA has zero open High/BLOCKER findings — the full
   five-point gate in docs/LOGIC.md §9, not this step alone.
