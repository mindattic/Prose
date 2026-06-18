# StreetSamurai MCP server

Exposes the world canon (characters, places, factions, books, outlines, motifs, literary rules) and the semantic index as Model Context Protocol tools, so Claude â€” Desktop, Code, or any MCP client â€” can call into the data without you copy-pasting JSON.

## What it does

Tool surface (read-mostly; the only mutation is `plant_motif`):

| Tool | Purpose |
| --- | --- |
| `list_characters` / `get_character` | Character roster + full canon record (psychology, behavioral, speech_patterns, augmentations, story_hooks). |
| `list_places` / `get_place` | Districts with atmosphere, sensory detail, story hooks. |
| `list_factions` / `get_faction` | Gangs, syndicates, cells. |
| `list_CorpoNations` / `get_CorpoNation` | Corporate sovereigns. |
| `get_literary_rules` | Prohibitions, paragraph requirements, POV voice rules, register permissions. |
| `list_books` / `get_book` / `get_chapter` | Bookshelf + per-chapter prose and beats. |
| `get_book_outline` | Shared plot spine: per-chapter outlines, threads, pending adjustments. |
| `get_director_context` | "WHERE WE ARE" block â€” prior/this/upcoming chapter context for prose generation. |
| `search_semantic` | TF-IDF cosine search across every entity description. |
| `get_neighbors` | Walk graph relationships from a known entity. |
| `get_motifs` / `plant_motif` | Per-book motif inventory. |

## Why it exists

The Quorum-based generation pipeline (`BookReviewService`, the multi-LLM voting in `BookOutlineService`) is excellent for *review* â€” multiple voters catch what one misses â€” but for **generation** it averages voice toward mediocrity. This MCP server is the alternative path: keep the disciplined data layer, drop the multi-voter generator, let one writer (Claude in conversation) call the canon as needed.

## Toggle model â€” one-time registration, not per-session

The server only runs when an MCP client launches it. Registration is **one-time** and persists in the client's config. You set it once and forget it. To stop using it, remove the entry. There is no per-session toggle to manage.

### Claude Code (recommended)

```bash
claude mcp add streetsamurai dotnet run --project D:/Projects/MindAttic/StreetSamurai/v3/StreetSamurai.Mcp/StreetSamurai.Mcp.csproj --no-build --configuration Release
```

This writes to `~/.claude.json` and persists across every future Claude Code session. The tools appear automatically as `mcp__streetsamurai__list_characters`, `mcp__streetsamurai__get_book_outline`, etc.

To remove: `claude mcp remove streetsamurai`. The next session has no tools.

### Claude Desktop

Edit `claude_desktop_config.json`:

- macOS: `~/Library/Application Support/Claude/claude_desktop_config.json`
- Windows: `%APPDATA%\Claude\claude_desktop_config.json`

```json
{
  "mcpServers": {
    "streetsamurai": {
      "command": "dotnet",
      "args": [
        "run",
        "--project",
        "D:\\Projects\\MindAttic\\StreetSamurai\\v3\\StreetSamurai.Mcp\\StreetSamurai.Mcp.csproj",
        "--no-build",
        "--configuration",
        "Release"
      ]
    }
  }
}
```

Restart Claude Desktop after editing. The tools appear under the plug icon in the chat input. To disable: remove the `streetsamurai` block (or the whole `mcpServers` key) and restart.

### Disable behaviour

When the server is **not** registered:
- No tools appear in Claude's inventory.
- Claude works exactly as it does today â€” chat-only, reading JSON files via the regular `Read`/`Grep`/`Bash` tools when explicitly asked.
- The Blazor host and CLI Quorum review pipeline are unaffected. They never depended on this server.

You can flip between registered and unregistered freely; nothing in the project state is altered by toggling.

## Build

```bash
cd v3
dotnet build StreetSamurai.Mcp/StreetSamurai.Mcp.csproj --configuration Release
```

The `--no-build` flag in the client config above means the client launches the pre-built binary; rebuild manually after code changes to this project. (The other StreetSamurai projects do not need to be rebuilt for MCP server changes â€” the server only depends on `StreetSamurai.Core`.)

## Logs

The server writes to `<canon-root>/engine/data/logs/mcp-{date}.txt`. **Stdout is reserved for the MCP wire protocol** â€” anything written to stdout corrupts the transport. Serilog is configured to file-only; if you add code paths that log, never use `Console.WriteLine`. The log level is `Information` by default; tools log when called and when they complete (with `IsError` flag).

## Voice preservation guarantee

When the MCP server is registered, **Claude writes prose the same way it does without it.** The tools do not:

- Inject prompt fragments into Claude's context.
- Run a Quorum vote.
- Apply a "writing style" template.
- Force a workflow ("you must call X before writing Y").

Every tool returns *data* â€” a character record, a motif inventory, a search result list, an outline. Claude reads the data and decides what to do with it, including how to phrase a sentence, where to break a paragraph, when to use italicized inner monologue. Voice judgment, sentence rhythm, sensory specificity, dialogue cadence, paragraph economy â€” all of that stays with Claude. The tools just give Claude sharper memory than chat-loaded JSON gives.

This is intentional. The whole reason the MCP layer exists separately from the Quorum pipeline is to **avoid** the homogenising effect of multi-voter generation. Tools are a librarian, not a co-author.

## How Claude uses the tools

In a registered session, Claude calls tools autonomously based on the request size. Examples:

**"Draft chapter 5 of Bushido Coda."**
1. `list_books` â†’ find the book id.
2. `get_book(bookId)` â†’ metadata, chapter ids, state_at_end (what carried over from chapter 4).
3. `get_book_outline(bookId)` â†’ premise, arc, theme, per-chapter spine, open threads.
4. `get_director_context(bookId, chapter5Id)` â†’ "WHERE WE ARE" block.
5. `get_character("Kyle Ellen Corbin-Vister")` â†’ POV character's full canon: speech_patterns, psychology, behavioral, augmentations, story_hooks.
6. `get_motifs(bookId)` â†’ registered motifs to thread through the prose.
7. `get_literary_rules` â†’ prohibitions and POV voice rules.
8. `search_semantic("â€¦")` â†’ thematic neighbors that aren't named yet but are relevant.

Then the prose gets written. No prompt template; no voting; just one author with a complete picture.

**"What motifs are registered for Bushido Coda?"**
1. `list_books` â†’ find the book id.
2. `get_motifs(bookId)` â†’ the inventory.

Then a direct answer.

**"Tighten this paragraph."**
No tool calls. The request is local; canon access doesn't help.

**"Does Sasha know about the Reliquary by chapter 3?"**
1. `list_books` â†’ find the book id.
2. `get_book_outline(bookId)` â†’ check `state_changes` for Sasha across chapters 1â€“3.

Or possibly: `search_semantic("Reliquary Sasha")` if the outline doesn't say.

### Calibration

Claude calibrates tool calls to request size. If you ever feel the tools are pulling Claude toward over-research and away from prose ("you keep loading every character file before answering simple questions"), tell it to dial back; the next response follows the new bar. There is no global "tools off" switch short of removing the registration â€” calibration happens in conversation.

### Things Claude won't do automatically

- **Won't write back to canon.** The only mutation tool is `plant_motif`. Edits to characters, places, factions, books, and chapters happen through the Blazor UI; the MCP layer is read-mostly by design.
- **Won't run the Quorum review.** Triggered explicitly from the chapters page or via CLI.
- **Won't decide that a chapter is "done."** That's a human judgment.

## What's not exposed (yet)

- `WritingQualityService.Analyze` â€” heuristic pass over an entire book. Cheap to add, not in the initial surface.
- `BookReviewService.ReviewAsync` â€” the full Quorum review. Slow + expensive; better triggered explicitly from the UI.
- Mutations to characters / places / factions / books / chapters. The Blazor UI is the editing surface; the MCP layer is read-mostly by design.

## Verifying it's working

After registration, start a fresh Claude Code session and ask: *"What motifs are registered for Bushido Coda?"*

If Claude calls `mcp__streetsamurai__list_books` and `mcp__streetsamurai__get_motifs` (instead of reading `engine/data/books/*.motifs.json` with `Read`), the server is wired and Claude is using it.

If you don't see tool calls, check:
1. Did you build? `dotnet build StreetSamurai.Mcp/StreetSamurai.Mcp.csproj -c Release`
2. Did the registration take? `claude mcp list` should show `streetsamurai`.
3. Did the server start? Tail `<canon-root>/engine/data/logs/mcp-{date}.txt` â€” you should see `transport reading messages` shortly after the session opens.
4. Is the canon root auto-detected? `SettingsService.AutoDetectCanonRoot` runs at startup; the log will warn if it can't find data.
