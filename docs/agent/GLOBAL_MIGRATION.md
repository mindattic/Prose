# Global command and policy migration

The global Claude root was inspected alongside the Prose project root. Its portable content is:

| Global source | Portable destination |
|---|---|
| Global bare `do` continuation rule | `tools/prose-agent.ps1 do` and `PROMPT_COMMANDS.md` |
| Global `/commit` | Host-neutral repository workflow; the command remains an explicit human-approved git action |
| Versioning, cost, and git rules | `PROSE_PROTOCOL.md` plus repository policy documents |
| Global quicksave/quickload protocol | `.prose/commands/quicksave.md`, `quickload.md`, and `tools/prose-agent.ps1` |
| Global prompt logger hook | Optional host telemetry integration; it is never required for Prose correctness |
| Context gauge/statusline | Host UI concern; portable agents use explicit handoff commands and bootstrap state |

`tools/export-agent-catalog.ps1` scans both `.claude/` in the project and the user's global Claude
root and writes [`command-catalog.json`](command-catalog.json). The catalog preserves source scope,
kind, name, description, and origin so project and global commands cannot be confused or silently
dropped. Legacy files remain inventory sources only; new behavior belongs in `.prose/commands` or
the Hub-routed protocol.
