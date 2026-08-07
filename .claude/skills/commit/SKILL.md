---
name: commit
description: Stage, commit, push, and print the commit hash. Use /commit for auto-message or /commit "Message" for custom.
---

When invoked:

0. **3B sync (Beat ↔ Bible ↔ Blueprint):** run `dotnet run --project v3/Prose.Cli -- --close-all-sessions` via the Bash tool. This flushes any open edit sessions: extracts canon facts from edited beats and appends them to the relevant node bible, confirms or flags blueprint tags, then closes the sessions. If there are no open sessions it exits instantly. This step draws the coordination boundary so every commit is fully aligned. **Skip this step** only if the changes are purely code/infrastructure (no `docs/nodes/*.md` or beat-related files in the diff).
1. Run `git status` and `git diff --stat` to see what changed
2. Stage changed tracked files (use specific filenames, not `git add -A`)
3. **If arguments were provided** (`$ARGUMENTS`), use that as the commit message
4. **If no arguments**, auto-generate a descriptive commit message summarizing the "why" not the "what"
5. Always append to the commit message: `Co-Authored-By: Claude Opus 4.6 <noreply@anthropic.com>`
6. Commit and push to remote
7. Print the commit hash and message
8. Always end with: `To revert: /revert <hash>`
