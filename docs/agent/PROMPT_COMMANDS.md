# Portable prompt commands

`/do`, `/quicksave`, `/quickload`, and `/progress` are conversational aliases, not engine
features. Their portable implementation is the `.prose/commands` registry and
`tools/prose-agent.ps1`; a host may bind any UI syntax to those operations.

| Alias | Portable operation | Behavior |
|---|---|---|
| `/do` | `do` | Resume the saved task and print the next action |
| `/quicksave` | `save` | Save task, decisions, state, and next action as local handoff |
| `/quickload` | `load` | Print and consume the handoff (`-Keep` preserves it) |
| `/progress` | `cli --progress` | Show the Hub-routed strand dashboard |
| `/show <tuple>` | `cli --show` | Resolve and display an entity or book |
| `/dcm` | `mcp doc_context_status` | Inspect active Dynamic Context Memory |
| `/reader-qa` | `cli --reader-qa` | Run findings-based reader QA; no score panels |
| `/logic-sweep` | `cli --logic-sweep` | Run the continuity audit and convergence workflow |
| `/commit` | `git review-and-commit` | Review, stage named files, create a new commit, verify status, and push when authorized |

A client integration should translate its slash command into one runner invocation or MCP call.
It must not copy the old `.claude/commands` bodies into another vendor folder.

The catalog exporter also records the global Claude command root (`%USERPROFILE%/.claude`) and the
project root in [`command-catalog.json`](command-catalog.json), including skills and hooks. This
keeps global `/commit` and global prompt behavior visible to every provider without granting a
non-Claude client an implicit dependency on Claude's home directory.
