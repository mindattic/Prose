# Prose commands — the one shared home

Every command/runbook definition for this project lives here, in `.prose/commands/`, regardless
of which client (Claude Code, Codex, Copilot, or anything else) is running it. No client gets its
own duplicated copy of the workflow — a client-specific folder (`.claude/`, `.github/`, `.copilot/`,
etc.) may contain only the thin, mechanically-generated pointer files that client's own slash-command
or skill discovery requires, each one just saying "read `.prose/commands/<name>.md` and follow it."
If you find real content duplicated into a client-specific folder, that's a bug — move the content
here and reduce the client-specific file back to a pointer.

`quicksave` / `quickload` / `do` in this project use a richer **paper-transcript** convention (a
full markdown handoff written to `.prose/quicksave.md`) rather than the shared
`mindattic-agent-standard/prose-agent.ps1` JSON-state runner described in the standard's own
README — that's a deliberate per-project override, not a fork of the standard. `progress` and
`show` here are also project-specific, richer implementations (`prose --progress` / `prose --show`)
rather than the generic stubs the cross-project installer scaffolds elsewhere.

Run the CLI-backed ones from the repository root, e.g.:

```powershell
dotnet run --project v3/Prose.Cli -- --progress
```
