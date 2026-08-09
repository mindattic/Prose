# Prose.Mcp

Model Context Protocol server exposing the Prose world canon as MCP tools so Claude — Desktop, Code, or any MCP client — can call into the data without copy-pasting JSON.

The server uses `ModelContextProtocol` (Anthropic's C# MCP SDK), targets .NET 10, and re-uses every Core service (`Prose.Core`) via `AddProseServices()`. All `[McpServerToolType]` classes are auto-discovered by `WithToolsFromAssembly()` — adding a new tool only requires building.

## Tool surface

Tools are grouped by class. All tools are read-only except `plant_motif`.

| Group | Tools |
| --- | --- |
| `CanonTools` | `list_characters` / `get_character` / `get_character_profile`, `list_places` / `get_place`, `list_factions` / `get_faction`, `list_CorpoNations` / `get_CorpoNation`, weapon / cyberware / equipment encyclopedia getters, `get_literary_rules`, `get_tone_bible`, `get_story_bible` |
| `StoryTools` | `list_books`, `get_book`, `get_chapter`, `get_book_outline`, `get_director_context` |
| `ContextTools` | `search_semantic`, `get_neighbors`, `get_neighbors_by_relation`, `get_motifs`, `plant_motif`, `extract_entities`, `validate_canon_text`, `analyze_writing_quality` |
| `CombatTools` | `draft_combat_scene` |
| `ContinuityTools` | `find_contradictions` (chapter), `find_contradictions_book` (full book sweep) — Legion-Quorum rubric with EPISTEMIC / TEMPORAL / CAPABILITY / CANON classifications |
| `FactTools` | `extract_facts`, `extract_facts_book`, `get_facts`, `list_unresolved_contradictions`, `resolve_contradiction` |
| `ConsequenceTools` | `predict_behavior`, `get_consequences_for`, `get_recent_consequences`, `get_consequence_context` |

`get_director_context` builds the "WHERE WE ARE" block (prior chapters, this chapter's outline, open threads) — the highest-value starting point for prose generation.

## Why it exists

The Quorum-based generation pipeline is excellent for *review* — multiple voters catch what one misses — but for *generation* it averages voice toward mediocrity. This server is the alternative: keep the disciplined data layer, drop the multi-voter generator, let one writer (Claude in conversation) call the canon as needed. Tools return data; Claude decides what to do with it. Voice, sentence rhythm, and structure stay with Claude.

## Registration (one-time)

### Claude Code

```bash
claude mcp add prose dotnet run --project <path-to-your-clone>/v3/Prose.Mcp/Prose.Mcp.csproj --no-build --configuration Release
```

Writes to `~/.claude.json` and persists across sessions. Tools appear as `mcp__prose__*`.

To remove: `claude mcp remove prose`

### Claude Desktop

Edit `%APPDATA%\Claude\claude_desktop_config.json`:

```json
{
  "mcpServers": {
    "prose": {
      "command": "dotnet",
      "args": [
        "run", "--project",
        "D:\\Projects\\MindAttic\\Prose\\v3\\Prose.Mcp\\Prose.Mcp.csproj",
        "--no-build", "--configuration", "Release"
      ]
    }
  }
}
```

Restart Claude Desktop after editing.

## Build

```bash
cd v3
dotnet build Prose.Mcp/Prose.Mcp.csproj --configuration Release
```

The `--no-build` flag in the registration command means the client launches the pre-built binary. Rebuild manually after code changes.

## Logs

The server writes to `<canon-root>/engine/data/logs/mcp-{date}.txt`. **Stdout is reserved for the MCP wire protocol** — writing anything else to stdout corrupts the transport. Never use `Console.WriteLine` in any code path reached from the MCP server.

## Verify it is working

After registration, start a fresh Claude Code session and ask: *"What motifs are registered for Bushido Coda?"*

If Claude calls `mcp__prose__list_books` and `mcp__prose__get_motifs`, the server is wired.

If tools do not appear:
1. Build: `dotnet build Prose.Mcp/Prose.Mcp.csproj -c Release`
2. Check registration: `claude mcp list` should show `prose`
3. Tail the log: `<canon-root>/engine/data/logs/mcp-<today>.txt` should show `transport reading messages`
