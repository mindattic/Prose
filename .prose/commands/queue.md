---
description: Bookmark a future task or feature without stopping the current one. Queued items are dumped into the next /quicksave. Usage /queue <text>; no argument lists what is queued.
argument-hint: "<task or feature to do later>"
allowed-tools: PowerShell
---

# Queue — bookmark it, keep going

Capture something to do later without derailing the task in hand. The queue is durable state in
`.prose\agent-queue.json` (JSON, repo-local), not conversation memory, so it survives `/clear` and
context compaction until the next `/quicksave` consumes it.

## Do this now

- **With `$ARGUMENTS`:** add it, verbatim, as one item:

  ```powershell
  & .\tools\prose-agent.ps1 queue -Item "<$ARGUMENTS>"
  ```

  Reply with the one line the runner prints (`Queued [N] … (K pending)`) and **go straight back to
  what you were doing**. Do not start the queued work, plan it, or ask about it — that is the point.
  If the item needs a fuller spec than the user gave, write one sentence of spec into the item
  text yourself (what, where, done-when); nothing more.

- **Without arguments:** list the queue and stop:

  ```powershell
  & .\tools\prose-agent.ps1 queue -List
  ```

## Lifecycle

- `/quicksave` reads the queue, writes every item into the transcript's **Queued** section, then
  drains it (`queue -Drain` archives to `agent-queue.json.NNN` — never deleted).
- The portable `save` operation drains it too, into `agent-session.json`'s `queued` field.
- Items come back after `/clear` through the quicksave transcript, where the next session sees
  them under **Queued** and treats them as pending work, not as the current task.
