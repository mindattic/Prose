---
name: logic-sweep
description: Run the canonical Logic Sweep (docs/LOGIC.md, SS-A44) on one or more stories — six-dimension logic/continuity audit, severity triage, minimal-splice fixes, verification. NO votes, NO panels. Usage /logic-sweep [slug ...]; no argument = every story with beats changed since its last sweep.
---

# /logic-sweep — the canonical QA pass (SS-A44)

The law lives in `docs/LOGIC.md`. This skill is the runbook. **Never launch votes, panels,
or reviews from this skill** — if the user wants a score, they will say so explicitly.

## Fixed facts
- DB: `sqlcmd -S "(localdb)\MSSQLLocalDB" -d StreetSamurai` (Windows Auth, read-only for audits).
- Live story = enabled beats in reading order: `JOIN NodeBeats nb ... WHERE nb.IsEnabled = 1
  ORDER BY nb.SortKey`. Books: chapter children by `Nodes.SortKey`, beats within by `nb.SortKey`.
  NEVER order by `Beats.Number`.
- Reports → `audit-outlines-<today>/logic/<CODE>.md`. Fix files → `.../fixes/<CODE>/`.
- Text pushes via built exe `v3\StreetSamurai.Cli\bin\Release\net10.0\StreetSamurai.Cli.exe
  --beat update --id <guid> --text -` with OS-level `<` redirection (PowerShell pipelines inject
  BOM; verify `UNICODE(SUBSTRING(Text,1,1)) != 65279` after every push). One CLI invocation at a
  time (port). `dotnet run` may silently drop args — prefer the exe.
- NEVER `DELETE` from Nodes/Beats/NodeBeats. Soft-disable = `UPDATE NodeBeats SET IsEnabled=0`.
  Snapshot SortKeys to a backup file before re-seating.
- Doc edits → `powershell -File tools/codex.ps1 digest` then `doctor` — must PASS.

## Steps

1. **Resolve scope.** Args = those slugs. No args: find stories whose beats changed since their
   last sweep (compare `Beats.UpdatedAt` / `NodeBeats` changes against the story's most recent
   `audit-outlines-*/logic/<CODE>.md` date; when in doubt, include it). Small edit → scoped sweep
   (touched beats + blast radius); structural change (merge/disable/reorder) → full-story sweep.

2. **AUDIT (report-only agents, one per 1–2 stories, Sonnet).** Each agent reads the story
   end-to-end and reports on the six dimensions of `docs/LOGIC.md` §3:
   causality chain / knowledge states (incl. names-before-coining) / timeline (respect
   bible-marked deliberate impossibilities) / plant-payoff ledger incl. arithmetic walked
   end-to-end / orphan references (grep distinctive phrases of every disabled beat; check
   IsChapterStart stranded on disabled beats) / bible agreement.
   Findings: severity (BLOCKER/MODERATE/MINOR) + SortKeys + quoted text + minimal fix proposal.
   Instruct verbatim: "Do not invent problems — if the logic holds, say so."
   Cross-story canon (shared characters, series arcs): one additional cross-read agent over the
   affected set when a series story is in scope.

3. **TRIAGE.** Fix all BLOCKERs; MODERATEs almost always; MINORs when one word/clause. If a
   finding can't be named concretely, drop it. Editor-taste items go to the deferred ledger,
   not the fix pass.

4. **FIX (separate agents, the audit report as input).** Minimal-splice discipline per
   `docs/LOGIC.md` §4: prefer data fix → clause → passage → rewrite; reassign to established
   cast, reconcile counts to the load-bearing version; bible wins on facts unless the finding
   proves the bible stale — then fix the bible in the same change (+ digest/doctor).

5. **VERIFY (inside each fix pass).** Changed passages re-read with neighbors; old-defect
   greps = 0; disabled-content greps = 0; repaired arithmetic walked and printed; BOM checks;
   doctor PASS. Honest reporting: deviations and judgment calls are stated, never silently
   forced to satisfy a checklist.

6. **CLOSE.** Write/refresh `audit-outlines-<today>/logic/<CODE>.md` verdicts and, for
   multi-story sweeps, a `CORPUS-REPORT.md`. Summarize per story: verdict, findings by
   severity, fixes applied, anything deferred.
