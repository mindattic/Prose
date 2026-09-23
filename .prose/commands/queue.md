---
description: Record a future task or feature as an author work order without stopping the current one. Usage /queue <text>; no argument lists open author orders.
argument-hint: "<task or feature to do later>"
allowed-tools: PowerShell
---

# Queue — record it in the factory, keep going (RFC 0015)

The queue is the factory's work-order tree, not a JSON file. Queued items are author orders under
the approved "Author requests" root, so they survive every session and show up in `prose --order list`.

## Do this now

- **With `$ARGUMENTS`:** find the "Author requests" root id (`prose --order list`), then

  ```powershell
  prose --order add --kind author --title "<$ARGUMENTS, trimmed to one line>" --parent <author-requests root id> --detail "<the full text, plus one sentence: what, where, done-when>"
  ```

  Reply with the one line it prints and **go straight back to what you were doing**. Do not start,
  plan or discuss the queued work.
- **With no argument:** `prose --order list --kind author` and show the open items.

The old `.prose/agent-queue.json*` files are archived in `.prose/archive/`.
