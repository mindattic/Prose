# StreetSamurai

**A literary fiction engine for a cyberpunk century.**

StreetSamurai writes novels. Not snippets, not summaries — chapter-length prose, voice-disciplined, canon-grounded, ready for the bookshelf. It is the authoring stack for *Bushido Coda* and a hundred stories beyond it, set in the GLMZ: a 500-kilometer vertical megacity stacked along the western shore of Lake Michigan in the year 2225, where ferrocement waves rise a hundred stories above the lake and CorpoNations hold sovereignty the old nations could not.

Live at **[streetsamurai.azurewebsites.net](https://streetsamurai.azurewebsites.net/)**.

> **[docs/BIBLE.md](docs/BIBLE.md) is the architecture bible** (Codex L0) — the endpoint, laws, and architecture canon. [docs/USER_STORIES.md](docs/USER_STORIES.md) is the goal table with acceptance tests. This README is the quick tour.

---

## What it is

A C# / .NET 10 Blazor Server application — web-only, fully responsive — that pairs a disciplined data layer with a multi-LLM generation pipeline. The canon lives in SQL Server: 1,000+ named entities (characters, places, factions, CorpoNations, weapons, biotech, documents) bound together by a directional graph of relationships, vector embeddings, and a two-axis time model.

On top of that data layer sits the writing surface: a Book → Chapter → Beat hierarchy with outline-first workflow, a Quorum-voted review pipeline (Legion-backed, 11 LLM providers), a motif planter, and an export pipeline (EPUB, HTML, Markdown). Reader review panels with synthetic personas and multi-objective segment analysis let you measure reception from data, not vibes.

For agentic authoring, an MCP server exposes the entire canon as Model Context Protocol tools so Claude — Desktop, Code, or any MCP client — can call the world directly.

---

## Stack at a glance

| Layer | Technology |
| --- | --- |
| Host | Blazor Server (.NET 10), cookie auth, role-gated builds |
| Canon | SQL Server with vector indexes, `FOR SYSTEM_TIME` audit, directional Edges graph |
| Writing surface | `/strand/{id}` page — writer + recorder + listener on one screen |
| Embeddings | `EmbeddingService`, ~10k entities cached, drift-detected |
| Generation | Multi-provider Quorum via `MindAttic.Legion` — 11 LLM providers, scored evaluation |
| Review | `BookReviewService` + `WritingQualityService` + `MotifService` + continuity contradiction sweep |
| Export | EPUB / HTML / Markdown via `BookExportService` |
| Audio | ElevenLabs TTS pipeline; free local alternatives via Kokoro or Chatterbox (see `tools/`) |
| Agents | `StreetSamurai.Mcp` — MCP server exposing canon to Claude clients |
| Credentials | Cloud-native resolution via `MindAttic.Vault` |

---

## Repository layout

```
StreetSamurai/
├── v3/                          # Active engine
│   ├── StreetSamurai.slnx
│   ├── StreetSamurai.Shared/    # POCOs, enums, DTOs
│   ├── StreetSamurai.Core/      # Canon services, generation pipeline, embeddings, review
│   ├── StreetSamurai.Blazor/    # Blazor Server host — the live site
│   ├── StreetSamurai.Mcp/       # Model Context Protocol server
│   ├── StreetSamurai.UnitTests/ # NUnit + bUnit tests
│   ├── ApplyMigrations/         # One-shot EF Core migration runner
│   └── ...                      # Apply* / Promote* / Sync* one-off consoles
├── tools/                       # Standalone utilities
│   ├── check-contradictions.js  # Legion-Quorum chapter-vs-canon sweep
│   ├── chatterbox/              # Free local TTS via Resemble AI Chatterbox (MIT)
│   └── kokoro/                  # Free local TTS via Kokoro-82M (Apache-2.0)
├── v3/python/                   # SPO triple extraction + semantic consistency pipeline
├── infra/                       # Azure SQL + GitHub Actions setup
├── docs/                        # Codex docs (BIBLE.md, AMENDMENTS.md, USER_STORIES.md)
├── engine_data/                 # Canon entity JSON files (1,000+ entities)
├── cypress/                     # Cypress end-to-end tests
└── cypress.config.js
```

Each `Apply*` / `Promote*` / `Sync*` console under `v3/` is a single-purpose migration runner — built once, run once against the live database, left in place as a record.

---

## Running locally

Prerequisites: .NET 10 SDK, SQL Server (LocalDB is fine), at least one LLM provider API key wired through `MindAttic.Vault`.

```powershell
cd v3
dotnet restore
dotnet run --project ApplyMigrations   # apply schema + seed

dotnet run --project StreetSamurai.Blazor
# -> https://localhost:7103/
```

The MCP server runs as its own host:

```powershell
dotnet build StreetSamurai.Mcp/StreetSamurai.Mcp.csproj -c Release
dotnet run --project StreetSamurai.Mcp --no-build --configuration Release
```

Register it permanently in Claude Code:

```
claude mcp add streetsamurai dotnet run --project D:/Projects/MindAttic/StreetSamurai/v3/StreetSamurai.Mcp/StreetSamurai.Mcp.csproj --no-build --configuration Release
```

---

## Database migrations

Raw T-SQL files live under `v3/StreetSamurai.Core/Data/Sql/`. All scripts are idempotent.

```powershell
dotnet run --project v3/ApplyMigrations
```

---

## Deploying to Azure

The live site runs on Azure App Service at **streetsamurai.azurewebsites.net** against Azure SQL (Serverless GP). CI/CD runs `build → migrate → deploy` on every push to master via GitHub Actions OIDC (no passwords). Full provisioning guide: [`infra/README.md`](infra/README.md).

---

## Tests

```powershell
dotnet test v3/StreetSamurai.UnitTests   # NUnit + bUnit

npx cypress run     # headless e2e
npx cypress open    # interactive
```

---

## Status

In active development. Live site running, working bookshelf, Quorum review pipeline shipping findings, MCP server registered, audiobook MVP in flight.
