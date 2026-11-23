---
codex: 1
project: Prose
layer: planning
code: RA
title: "Read-along: scheduler job spec"
updated: 2026-11-23
---

# Read-along scheduler job (RA-T4.5.1.3)

The Hub reads along by itself (`ReadAlongBackgroundService`, every `ReadAlong:IntervalMinutes`, default 5).
This nightly job is the **backstop**: it catches anything the resident job missed while the Hub was down,
paused for the Azure SQL resume, or switched off with `ReadAlong:Enabled=false`. It lives in
**MindAttic.Scheduler** (registered by its `install.ps1`, headless and non-interactive like every job there),
not in this repository. Add it there to close this ticket.

| Field | Value |
| --- | --- |
| Name | `Prose.ReadAlong.Sweep` |
| When | nightly, 02:41 local time (off the hour, so it does not pile onto the :00 jobs) |
| Command | `prose --read-along --all` |
| Working directory | the Prose repo root, or anywhere `prose.cmd` is on the PATH |
| Needs | a running Hub (the CLI refuses to start without one). Start it first with `run.ps1 -Apps Hub -Start Hub` when the Hub is not resident. |
| Cost | none: deterministic, no LLM, no tokens |
| Output | one line, `Read-along sweep: read N chapter(s).`, plus a command-ledger row `read-along.sweep` (source `job`) when it read anything. The `/app` dashboard's Read-along tile shows the latest. |
| Failure | a non-zero exit (Hub unreachable). Nothing is written on failure; the next run reads what is still due. |
| Respects | each book's **Follow this book** switch, the quiet period does not apply (`--all` reads every due chapter), and the reading allowance (`prose --read-allowance`) |
