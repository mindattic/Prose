# Portable prompt commands

`/do`, `/quicksave`, `/quickload`, and `/progress` are conversational aliases, not engine
features. Their portable implementation is the `.prose/commands` registry and the factory's
CLI/MCP twins (RFC 0015); a host may bind any UI syntax to those operations.

| Alias | Portable operation | Behavior |
|---|---|---|
| `/do` | `cli --factory next` | Do the factory's next action (computed by the Hub from the book's state) |
| `/quicksave` | `cli --session end --file <summary.json>` | End the factory session; every decision must reference a ruling or work order |
| `/quickload` | `cli --factory next` | Resume from the factory's live next action and the last session's summary |
| `/queue <text>` | `cli --order add --kind author` | Record a future task as an author work order under "Author requests"; no argument lists them |
| `/progress` | `cli --progress` | Show the Hub-routed strand dashboard |
| `/show <tuple>` | `cli --show` | Resolve and display an entity or book |
| `/dcm` | `mcp doc_context_status` | Inspect active Dynamic Context Memory |
| `/logic-sweep` | `cli --logic-sweep` | Run the continuity audit and convergence workflow |
| `/commit` | `git review-and-commit` | Review, stage named files, create a new commit, verify status, and push when authorized |

A client integration should translate its slash command into one runner invocation or MCP call.
It must not copy the old `.claude/commands` bodies into another vendor folder.

The catalog exporter also records the global Claude command root (`%USERPROFILE%/.claude`) and the
project root in [`command-catalog.json`](command-catalog.json), including skills and hooks. This
keeps global `/commit` and global prompt behavior visible to every provider without granting a
non-Claude client an implicit dependency on Claude's home directory.
