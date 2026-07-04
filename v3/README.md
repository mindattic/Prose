# StreetSamurai v3

The active engine for the StreetSamurai literary fiction platform. A .NET 10 Blazor Server application paired with an MCP server, set in GLMZ — the Great Lakes Metropolitan Zone, year 2226.

## Solution structure

```
v3/
├── StreetSamurai.slnx
├── StreetSamurai.Shared/        POCOs, enums, DTOs shared by all projects
├── StreetSamurai.Core/          Canon services, generation pipeline, embeddings, review, continuity
├── StreetSamurai.Blazor/        Blazor Server host — the live site
├── StreetSamurai.Mcp/           Model Context Protocol server
├── StreetSamurai.UnitTests/     NUnit + bUnit (840+ tests)
├── ApplyMigrations/             Applies raw T-SQL migration files from Core/Data/Sql/
├── FixSableContinuity/          One-off continuity repair (migration record)
├── MaterializeChapters/         One-off canon backfill (migration record)
├── PromoteAndDehyphenate/       One-off data normalization (migration record)
└── ...                          Other Apply* / Promote* / Sync* / Write* consoles
```

Each `Apply*` / `Promote*` console is a single-purpose migration — built once, run once, left in place as a record.

## Running locally

```powershell
cd v3
dotnet restore
dotnet run --project ApplyMigrations        # apply schema migrations
dotnet run --project StreetSamurai.Blazor   # -> https://localhost:7103/
```

## Tests

```powershell
dotnet test StreetSamurai.UnitTests
```

NUnit fixtures cover: `WorldGraphService`, `SemanticIndexService`, `InferenceService`, `StoryStateService`, `EventLogService`, `KnowledgeMapService`, `OutlineService`, `JsonDirectoryRepository`, `ExportService`, `ActionConfigService`, `ExpertPersonaService`, `OutlineGateService`, and more.

## Python pipeline (`python/`)

A standalone SPO triple extraction and semantic consistency pipeline. Reads entity JSON files, extracts Subject-Predicate-Object claims via Claude, clusters semantically equivalent claims with HDBSCAN, and flags inconsistencies. See [`python/README.md`](python/README.md).

## MCP server (`StreetSamurai.Mcp/`)

Exposes canon as Model Context Protocol tools for Claude Desktop, Claude Code, or any MCP client. See [`StreetSamurai.Mcp/README.md`](StreetSamurai.Mcp/README.md).

## Key dependencies

| Package | Purpose |
| --- | --- |
| `ModelContextProtocol` | MCP server SDK (Mcp project only) |
| `Microsoft.Extensions.Hosting` 10.0.5 | Generic host for the MCP server |
| `Markdig` | Markdown-to-HTML rendering |
| `QuikGraph` | In-memory directed relationship graph |
| `System.Speech` | Windows SAPI TTS fallback |
| `Microsoft.Extensions.Http` 10.0.5 | HttpClient factory |
