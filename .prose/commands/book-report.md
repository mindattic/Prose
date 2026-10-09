# /book-report — read the backlog by hand, fix what's real, write it up

Usage: `/book-report <slug-or-code>`; no argument = ask the author which book.

This codifies the 2026-10-08 BLST session: read a book's whole open-findings backlog against its
*live* prose (not the backlog's own summary text), verify or dismiss each finding by hand, fix any
real defect with a minimal splice, teach the engine the diegetic patterns that keep getting
mis-flagged so the next run doesn't re-flag them, then emit a deterministic report of what
happened. **The judging is still a human-grade read, every time — nothing in this runbook replaces
step 2 with an automated score.** See `docs/LOGIC.md` SS-LAW-17 and `docs/FINDING-CODES.md`.

## Steps

1. **Resolve the book + pull its backlog.**
   `prose --findings list --node <slug-or-code> --status new --limit 2000` (and `--limit`
   higher if the count returned looks capped). Group by Category — this tells you where the
   session's actual attention should go: a handful of Contradiction/ComprehensionDefect/
   EntityDrift findings deserve individual reads; a hundred-plus CraftChecklist/ProseHealth rows
   almost always cluster around a small number of repeated sub-patterns (`--findings show <id>`
   a sample of each) worth checking once, not once-per-row.

2. **Read each finding against the live beat, by hand — not the finding's own summary text.**
   `read_beats`/`prose --read-beats` the beat plus one neighbor each side. A finding is real only
   when the live prose actually shows the defect; an automated checker's summary describing a
   defect is a CLAIM, not evidence (see `docs/LOGIC.md` SS-LOGIC-4a — verify quote grounding on
   anything quoted before trusting it). Expect a meaningful fraction to be false positives:
   deliberate withheld information that pays off later, a character's established voice that a
   generic tic-detector can't tell from an AI tic (RFC 0011 row 3 — this is a *documented*,
   *recurring* failure mode in this engine, not a one-off), or two speakers alternating in
   untagged dialogue the checker misread as one. "Do not invent problems — if the logic holds,
   say so" (docs/LOGIC.md §4) applies here exactly as it does in a logic sweep.

3. **Fix what's real.** Same minimal-splice discipline as `/logic-sweep` steps 4-6: archive the
   book first, prefer a data fix over a prose rewrite, push beat text through the CLI only
   (`--beat update --file <path>`, never raw stdin/argv — see the BOM/CRLF gotcha in project
   memory), re-read the seam with neighbors, grep the old phrase (0 hits).

4. **Close genuinely false-positive findings, don't just leave them New.**
   `prose --findings dismiss <id>` for a one-off false positive you hand-verified this pass.

5. **Teach the engine the pattern, so it stops re-flagging it.** When the SAME sub-pattern keeps
   firing on an established, intentional choice (a POV character's own voice, a book's genre-
   appropriate recurring device), don't hand-dismiss every instance one at a time forever —
   record an exception once:
   `prose --findings suppress --node <slug> --code <code> [--beat <guid>] --reason "..."`
   (`prose --findings codes` lists valid codes — `docs/FINDING-CODES.md` is the same table with
   more context). Omit `--beat` for a book-wide exception; pass it to scope to one beat only.
   This is a side-table, never inline prose/entity-tag markup — see `FindingSuppressionRow`'s doc
   comment for why (entity tags regenerate from scratch on every beat save; an unrecognized tag
   type isn't stripped by the export pipeline and would leak into the published book).

6. **Run the logic-sweep convergence check** — `prose --logic-sweep --slug <slug> --until-dry` —
   so the report in step 7 reflects the engine's own current verdict, not a stale one.

7. **Generate the report.**
   `prose --book-report --slug <slug-or-code> [--complete]` — pass `--complete` once this pass
   actually read the backlog through and fixed what was real (not just skimmed it); omit it for a
   first-look draft. Every run does two things:
   - writes the Downloads draft `Downloads\GLMZ_Book_Reports\drafts\{CODE}[_complete].md` (feeds
     `make_pdfs.py`; tell the author to run it in that folder when they want the PDF refreshed — this
     command does not invoke Python itself);
   - overwrites the book's single row in the **`BookReports`** table (BookCode, Title, IsComplete,
     word/beat/open-finding counts, Converged, the full Markdown, the export file path, CreatedAt,
     UpdatedAt). One row per book, enforced by a unique index; a report is moot once the prose pass
     it drove has landed, so no history is kept.
   The readable copy in the book's export directory is **not** written here — that is
   `/export-book-report <code>` (`.prose/commands/export-book-report.md`). The command runs inside
   the Hub, so a change to it needs a Hub redeploy before it takes effect.

8. **Log the decision.** `log_decision` (or `prose --log-decision`) summarizing what was verified,
   what was fixed, what was suppressed and why, and what's still open — this is what
   `BookReportService` reads back into the report's "Logged decisions" section, and what a future
   session with zero conversation memory uses to pick up where this one left off.
