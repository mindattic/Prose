# Prose commands — the one shared home

Every command/runbook definition for this project lives here, in `.prose/commands/`, regardless
of which client (Claude Code, Codex, Copilot, or anything else) is running it. No client gets its
own duplicated copy of the workflow — a client-specific folder (`.claude/`, `.github/`, `.copilot/`,
etc.) may contain only the thin, mechanically-generated pointer files that client's own slash-command
or skill discovery requires, each one just saying "read `.prose/commands/<name>.md` and follow it."
If you find real content duplicated into a client-specific folder, that's a bug — move the content
here and reduce the client-specific file back to a pointer.

`quicksave` / `quickload` / `do` / `queue` in this project run through the **Novel Factory** (RFC 0015): a session is a `FactorySessions` row in the Hub, `/quicksave` ends it (`prose --session end`), the SessionStart hook shows the factory's live next action on resume, and `/queue` adds an author work order. There are no transcript files any more (the old ones are in `.prose/archive/`). `progress` and `show` here are project-specific, richer implementations (`prose --progress` / `prose --show`) rather than the generic stubs the cross-project installer scaffolds elsewhere.

Run the CLI-backed ones from the repository root, e.g.:

```powershell
dotnet run --project src/Prose.Cli -- --progress
```
