---
description: Resume from the factory — the live next action and the last session's summary. Replaces the old paper-transcript quickload.
allowed-tools: PowerShell, Bash
---

# Quickload — resume from the factory (RFC 0015)

There is nothing to load from disk. The SessionStart hook already injected the factory block:
the NEXT ACTION (computed by the Hub from the book's state), the books on the line, the last
session's summary, and the laws. Resume from that.

If the block is missing (the hook did not run, or the Hub was down):

1. Make sure the Hub is up: `powershell -NoProfile -File D:\Projects\MindAttic\Prose\src\tools\deploy-apps.ps1 -Start Hub`.
2. Run `prose --factory next` (or MCP `factory_next`) and `prose --order list`.
3. Do exactly the next action it names. Do not work from memory or from an old transcript.

The old `.prose/quicksave.md*` transcripts are archived in `.prose/archive/` for history only.
