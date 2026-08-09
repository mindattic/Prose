# Prose v3

The active engine for the Prose literary fiction platform. A .NET 10 Blazor Server application paired with an MCP server, set in GLMZ — the Great Lakes Metropolitan Zone, year 2226.

## Solution structure

```
v3/
├── Prose.slnx
├── Prose.Shared/        POCOs, enums, DTOs shared by all projects
├── Prose.Core/          Canon services, generation pipeline, embeddings, review, continuity
│   ├── Migrations/      EF Core migrations (schema)
│   └── Data/Sql/        Raw T-SQL — mostly historical, pre-EF-migration deltas; see root README
├── Prose.Cli/           Standalone CLI — includes --migrate-sql (schema bootstrap) and --seed
├── Prose.Writer/        Blazor Server host — writing/recording UI (the live site)
├── Prose.Codex/         Blazor Server host — canon/world-health/reports UI
├── Prose.Mcp/           Model Context Protocol server
└── Prose.UnitTests/     NUnit + bUnit
```

> The one-off `Apply*`/`Promote*`/`FixSableContinuity`/`MaterializeChapters` migration-record
> console apps once listed here (built once, run once, left as a historical record) have all
> been deleted — their work is done and doesn't need re-running. If you see empty directories
> with these names on an existing local checkout, they're untracked leftovers safe to remove;
> a fresh clone never has them.

## Running locally

```powershell
cd v3
dotnet restore
dotnet run --project Prose.Cli -- --migrate-sql --schema   # apply schema (idempotent)
dotnet run --project Prose.Writer   # -> https://localhost:7200/
```

## Tests

```powershell
dotnet test Prose.UnitTests
```

NUnit fixtures cover: `WorldGraphService`, `SemanticIndexService`, `InferenceService`, `StoryStateService`, `EventLogService`, `KnowledgeMapService`, `OutlineService`, `JsonDirectoryRepository`, `ExportService`, `ActionConfigService`, `ExpertPersonaService`, `OutlineGateService`, and more.

## Python pipeline (`python/`)

A standalone SPO triple extraction and semantic consistency pipeline. Reads entity JSON files, extracts Subject-Predicate-Object claims via Claude, clusters semantically equivalent claims with HDBSCAN, and flags inconsistencies. See [`python/README.md`](python/README.md).

## MCP server (`Prose.Mcp/`)

Exposes canon as Model Context Protocol tools for Claude Desktop, Claude Code, or any MCP client. See [`Prose.Mcp/README.md`](Prose.Mcp/README.md).

## Key dependencies

| Package | Purpose |
| --- | --- |
| `ModelContextProtocol` | MCP server SDK (Mcp project only) |
| `Microsoft.Extensions.Hosting` 10.0.5 | Generic host for the MCP server |
| `Markdig` | Markdown-to-HTML rendering |
| `QuikGraph` | In-memory directed relationship graph |
| `System.Speech` | Windows SAPI TTS fallback |
| `Microsoft.Extensions.Http` 10.0.5 | HttpClient factory |
