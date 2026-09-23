# /commit — stage, commit, push

Usage: `/commit` for an auto-generated message, or `/commit "Message"` for a custom one.

When invoked:

0. **Close edit sessions:** run `dotnet run --project src/Prose.Cli -- --close-all-sessions`. This closes any open edit sessions so the commit draws a clean session boundary; if there are none it exits instantly. (The Beat ↔ Bible ↔ Blueprint sync that used to run here is gone: the outline and blueprint were removed 2026-09-22, and a book is only its beats plus the canon entities they draw on.) **Skip this step** if the changes are purely code/infrastructure with no beat edits.
1. Run `git status` and `git diff --stat` to see what changed.
2. Stage changed tracked files (use specific filenames, not `git add -A`).
3. **If arguments were provided**, use that as the commit message.
4. **If no arguments**, auto-generate a descriptive commit message summarizing the "why" not the "what".
5. Append whatever attribution footer the current client's own instructions specify (do not hardcode a vendor/model name here — that hardcoding is exactly the kind of client-specific content this catalog exists to avoid).
6. Commit and push to remote.
7. Print the commit hash and message.
8. Always end with: `To revert: /revert <hash>`.
