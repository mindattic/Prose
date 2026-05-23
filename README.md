# StreetSamurai

**A literary fiction engine for a cyberpunk century.**

StreetSamurai writes novels. Not snippets, not summaries — chapter-length prose, voice-disciplined, canon-grounded, ready for the bookshelf. It is the authoring stack for *Bushido Coda* and a hundred stories beyond it, set in the GLMZ: a 500-kilometer vertical megacity stacked along the western shore of Lake Michigan in the year 2225, where ferrocement waves rise a hundred stories above the lake and corponations hold sovereignty the old nations could not.

Live at **[streetsamurai.azurewebsites.net](https://streetsamurai.azurewebsites.net/)**.

---

## Table of Contents

- [What it is](#what-it-is)
- [What makes it different](#what-makes-it-different)
- [The world, briefly](#the-world-briefly)
- [Stack at a glance](#stack-at-a-glance)
- [Repository layout](#repository-layout)
- [The Strand / Beat model](#the-strand--beat-model)
- [Running locally](#running-locally)
- [Database migrations](#database-migrations)
- [Deploying to Azure](#deploying-to-azure)
- [Tests](#tests)
- [Status](#status)

---

## What it is

A C# / .NET Blazor Server application — web-only, fully responsive, phone-accessible — that pairs a disciplined data layer with a multi-LLM generation pipeline. The canon lives in SQL Server: 1,000+ named entities (characters, places, factions, corponations, weapons, biotech, documents) bound together by a directional graph of relationships, vector embeddings, and a two-axis time model that distinguishes wall-clock audit history from in-world chronology.

On top of that data layer sits the writing surface: a Book → Chapter → Beat hierarchy with outline-first workflow, a Quorum-voted review pipeline drawing on eleven LLM providers, a heuristic Writing Quality service, a motif planter that threads imagery across chapters, and an export pipeline that produces Calibre-friendly EPUB, HTML, and Markdown.

For agentic authoring, an MCP server exposes the entire canon as Model Context Protocol tools so Claude — Desktop, Code, or any MCP client — can call the world directly without prompt-engineering a librarian.

---

## What makes it different

**Canon as a database, not a folder.** Every fact — character psychology, weapon specs, faction territories, who-knew-what-when — lives in indexed, queryable tables. Substring search has been retired wherever it touched generation; semantic embeddings ground prompts in the real corpus, not the most recent fifty kilobytes of context.

**Quorum for review, single voice for prose.** The multi-LLM voting pipeline is excellent at catching what one voter misses — continuity breaks, motif drift, voice slips — but the prose itself is written by one author at a time. No averaging toward mediocrity.

**A world that pushes back.** Story-time is a real axis. Death is recorded. The continuity service runs a contradiction sweep across the corpus. The outline must be approved before prose ever fires. When canon evolves, slugs follow, embeddings re-index, and findings surface in a dedicated review panel.

**Voice over template.** The MCP layer is a librarian, not a co-author — it returns data and gets out of the way. Sentence rhythm, register, italicized inner monologue, dialogue cadence: all of that stays with the writer.

---

## The world, briefly

The GLMZ — Greater Lake Michigan Zone, Meridian 88, *The Glooms* depending on who's asking — is the center of what's left of Western civilization. The coasts failed. The middle didn't. Twelve named territories rise in concentric vertical strata above ferrocement seawalls, served by The Pulse — a Mach-6 magnetic vacuum transit network that puts Rotterdam 43 minutes from Chicago. Iowan Behemoths walk the prairie beyond, autonomous and enormous and emphatically not alive. Currency is Φ — quantum compute-time, the QUANTA, never the Greek letter phi.

People are from everywhere. Heritage is mixed, names cross continents, the diaspora is ubiquitous. Corponations hold the sovereignty Westphalia used to. There is no city police force. The Vultures pick up the bodies and quietly repossess the organs. Cyberpunk cliché is rejected on contact.

This is the literary substrate. Eleven concurrent creative directives govern voice; a hundred-story corpus is in flight; the rogue-AI long con runs underneath the slice-of-life surface.

---

## Stack at a glance

| Layer | Technology |
| --- | --- |
| Host | Blazor Server (.NET 10), cookie auth, role-gated builds |
| Canon | SQL Server 2025 with vector indexes, FOR SYSTEM_TIME audit, directional Edges graph |
| Writing surface | Unified `/strand/{id}` page — writer + recorder + listener on one screen. Beats are the atom of prose+audio; one strand per work, chapters are beats flagged `IsChapterStart` |
| Embeddings | DI-registered `EmbeddingService`, ~10k entities cached, drift-detected |
| Generation | Multi-provider Quorum via [MindAttic.LLMVoting](https://github.com/mindattic/LLMVoting) — 11 LLM providers, scored evaluation, character personas |
| Review | `BookReviewService` + `WritingQualityService` + `MotifService` + continuity contradiction sweep (Legion-backed) |
| Export | EPUB / HTML / Markdown via `BookExportService` |
| Audio | ElevenLabs TTS pipeline, per-beat narration, per-beat trailing silence via `Beat.GapAfterMs` |
| Agents | `StreetSamurai.Mcp` — Model Context Protocol server exposing canon to Claude clients |
| Credentials | Cloud-native resolution via [MindAttic.Vault](https://github.com/mindattic/Vault) |

---

## Repository layout

```
StreetSamurai/
└── v3/                              # The active version of the engine
    ├── StreetSamurai.slnx           # Solution file
    ├── StreetSamurai.Shared/        # POCOs, enums, DTOs shared by every project
    ├── StreetSamurai.Core/          # Canon services, generation pipeline, embeddings, review
    ├── StreetSamurai.Blazor/        # ASP.NET Core Blazor Server host — the live site
    ├── StreetSamurai.Mcp/           # Model Context Protocol server exposing canon to Claude clients
    ├── StreetSamurai.UnitTests/     # NUnit + bUnit tests
    ├── ApplyMigrations/             # One-shot EF Core migration runner
    ├── MaterializeChapters/         # One-shot canon backfill tool
    ├── RebuildCanon/                # One-shot canon re-index tool
    ├── PromoteAndDehyphenate/       # One-shot data-normalization tool
    └── ... other Apply* / Promote* / Sync* / Write* consoles for one-off migrations
```

Each `Apply*` / `Promote*` / `Sync*` / `Write*` folder under `v3/` is a single-purpose console — built once, run once against the live database, then left in place as a record of the migration. They are not part of the runtime.

## The Strand / Beat model

The writing surface is built on two entities: **Strand** (a work — book, story, episode; the name is semantic) and **Beat** (one paragraph of prose + its one audio rendering). A strand contains a flat, ordered list of beats. There are no nested strands; chapter structure lives on the beats themselves via flags.

**Beat fields that drive the UI:**

| Field | Type | What it does |
| --- | --- | --- |
| `Number` | `int` | Stable global counter ("Beat #134" in CLI / MCP references). Does not shift on reorder. |
| `Text` | `string` | The prose. Authoritative; nothing else holds a copy. |
| `BeatTitle` | `string?` | Short label. Doubles as the chapter heading when `IsChapterStart=true`, and as the attribution when `Kind='quote'`. |
| `IsChapterStart` | `bool` | When true, the writer/listener renders a divider above the beat with `BeatTitle` as the heading, and the beat appears in the chapter-index jump links. Orthogonal to `Kind`. |
| `Kind` | `string` | `prose` (default), `book-title` (front-matter), `dedication` (centered italic), `quote` (blockquote, `BeatTitle` is the attribution). Free-form so new kinds need no schema migration. |
| `GapAfterMs` | `int?` | Explicit silence in ms after this beat, before the next. `null` = use the auto-computed default (`ComputeTrailingSilenceMs`, 200 / 400 / 1000 / 1800ms by SceneType + terminator). `0` is a valid override (no silence). |
| `GapAfterAudioPath` | `string?` | Optional recorded clip (rain, sigh, ambient) to play in the gap instead of digital silence. |
| `BeatTitle / Synopsis / EmotionalTone / PaceHint / FacetTag / StructureRole / Act / SceneType` | various | Narrative metadata feeding the TTS prompt builder. |

**The `/strand/{idOrSlug}` page** unifies writer, recorder, and listener:

- A **chapter index** at the top lists every beat where `IsChapterStart=true` as a jump link.
- Each beat-card uses a **three-row layout**:
  - **Row 1 — header band.** Inline checkbox · `Beat #001` (zero-padded positional rank) · status/char chips · **format toolbar** (Bold / Italic / Underline / Strikethrough, icon-only, enabled only while editing) · right-aligned hover actions (copy-text · ✨ LLM · 🎙 re-record · 🗑 delete).
  - **Row 2 — body.** Click-to-edit toggle between the rendered read view (markdown + emoji-replaced tone tags) and a textarea that exposes the raw markers. The two states share the same box — no padding / border / font shift on switch, just a 3-pixel left rule appears on focus.
  - **Row 3 — footer.** Audio player (if recorded) · meta chips (tone / pace / facet / details) · meta-panel (when open) · inline **gap-after editor** (`gap after  N ms (auto|custom) ✓ ↺`).
- Clicking the prose body opens the inline editor; the standalone "Edit" button was retired. Save / Cancel during edit are icon-only and float in the top-right corner so they don't push the body.
- The ✨ button opens a **bottom-sheet LLM panel** seeded with the `strand-guid.beat-guid` handle and the current beat text. Header has an `id` copy button (CLI-friendly handle). Free-text instruction → preview → Apply (replaces the beat text and invalidates audio) or Discard.
- The meta-panel exposes the `Kind` dropdown and `IsChapterStart` checkbox alongside the narrative-tone fields.

**Inline markup in `Beat.Text`** — the read view and the audio narration both consume the same string. Authoring is plain text plus:

| Marker / Tag | Rendered in read view | Audio behaviour |
| --- | --- | --- |
| `**bold**`           | `<strong>bold</strong>`         | passed through to TTS as-is (asterisks invisible to ElevenLabs) |
| `*italic*`           | `<em>italic</em>`               | "" |
| `__underline__`      | `<u>underline</u>`              | "" |
| `~~strikethrough~~`  | `<s>strikethrough</s>`          | "" |
| `[WHISPERING]` etc.  | 🤫 (hover reveals the original) | recognized by ElevenLabs v3 voices as an inline audio cue |

Supported ElevenLabs tone tags (case-insensitive, rendered as emoji in the writer):

- **Emotional tone:** `[EXCITED]` 🤩 · `[NERVOUS]` 😬 · `[FRUSTRATED]` 😤 · `[TIRED]` 😴
- **Reactions:** `[GASP]` 😮 · `[SIGH]` 😮‍💨 · `[LAUGHS]` 😂 · `[GULPS]` 😰
- **Volume / energy:** `[WHISPERING]` 🤫 · `[SHOUTING]` 📢 · `[QUIETLY]` 🔉 · `[LOUDLY]` 🔊
- **Pacing / rhythm:** `[PAUSES]` ⏸️ · `[STAMMERS]` 🗣️ · `[RUSHED]` 💨

The textarea, the CLI, the MCP layer, and the SQL row all see the raw bracketed form. Only the prose render swaps to emoji. New tags are a one-line addition to `BeatFormatter.ToneTagEmoji`.

A worked example of how a book lays out under this model:

```
Beat1  Kind=book-title  Text="The Story"  BeatTitle="Ryan DeBraal"
Beat2  Kind=dedication  Text="For my mother."
Beat3  Kind=prose       BeatTitle="1. The thing that happened"  IsChapterStart=true
Beat4  Kind=quote       Text="This is a small quote"  BeatTitle="Bill Coolman"
Beat5..7  Kind=prose
Beat57  Kind=prose      BeatTitle="2. The story continues"  IsChapterStart=true
Beat58..60  Kind=prose
```

The 2026-05-23 schema migration folded any legacy nested-strand chapters into their root strand and stamped `IsChapterStart` on each former child's first beat. Adding a new structural kind ("epigraph", "epilogue", "toc") is a Kind value, not a schema change.

## Running locally

Prerequisites:

- .NET 10 SDK
- SQL Server 2025 (vector index support is required) — LocalDB is fine for development
- A few LLM provider API keys, wired through `MindAttic.Vault` (User Secrets / `%APPDATA%\MindAttic\LLM\providers.json` / environment variables)

```powershell
cd v3
dotnet restore
dotnet ef database update --project StreetSamurai.Blazor   # initial schema + canon seed

# Launch the Blazor app
dotnet run --project StreetSamurai.Blazor
# → https://localhost:5001
```

The MCP server runs as its own host:

```powershell
dotnet run --project v3/StreetSamurai.Mcp
```

Point an MCP client (Claude Desktop / Claude Code / any MCP-compatible tool) at the resulting endpoint to call canon tools directly from a chat session.

**Authoring a strand from the LLM / CLI side** — the `StrandTools` class in `StreetSamurai.Mcp` exposes the workbench as MCP tools. Beat-level operations accept either a plain `Beat.Id` Guid OR the dotted `strand-guid.beat-guid` handle that the writer UI surfaces in the LLM bottom sheet:

| Tool | Purpose |
| --- | --- |
| `list_strands` / `get_strand` / `create_strand` | Browse and create strands. |
| `get_beat`            | Pull one beat by handle. Returns every authoring field — text, kind, IsChapterStart, BeatTitle, gap-after, tone metadata, position in strand, and the prev/next beat ids so the caller can place new beats relative to it. |
| `insert_beat`         | Add a new beat (top-of-strand or after a given beat). |
| `update_beat_text`    | Replace the prose. Accepts the inline markdown + tone-tag conventions above; stored verbatim. |
| `update_beat_metadata`| Set BeatTitle, Synopsis, EmotionalTone, PaceHint, FacetTag, StructureRole, Act, SceneType, **IsChapterStart**, **Kind** (`prose` / `book-title` / `dedication` / `quote`). |
| `set_beat_gap_after` / `clear_beat_gap_after` | Override or clear the silence the audio engine inserts after this beat. |
| `split_beat` / `join_beat` / `delete_beat` | Standard beat manipulation. |
| `narrate_strand`      | Kick off TTS for every un-narrated beat in the strand. |

This is enough for a CLI / LLM client to do full authoring: walk a strand, fetch a beat, rewrite it with markdown markers and tone tags, mark it as a chapter start, set its gap-after, and insert / delete neighbouring beats — all without touching the SQL layer directly.

## Database migrations

Schema changes ship as raw T-SQL files under `v3/StreetSamurai.Core/Data/Sql/`. The `ApplyMigrations` console reads them in order, splits each script on `GO` boundaries, executes each batch in its own EF `ExecuteSqlRawAsync` call (so a column added in batch 1 is queryable from batch 2), then runs any in-process C# data migration that follows. Every script is idempotent (`IF COL_LENGTH(...) IS NULL`, `IF OBJECT_ID(...) IS NULL`) so re-runs are safe.

```powershell
dotnet run --project v3/ApplyMigrations
```

Recent migrations (illustrative, not exhaustive):

| Script | Purpose |
| --- | --- |
| `add_beat_number_20260522.sql` | `Beats.Number` stable global counter |
| `fold_gaps_into_beats_20260523.sql` | Moves the standalone `Gaps` table into `Beat.GapAfterMs` / `GapAfterAudioPath`, drops the table |
| `add_beat_is_chapter_start_20260523.sql` | `Beats.IsChapterStart` BIT flag for the chapter-divider marker |
| `add_beat_kind_20260523.sql` | `Beats.Kind` NVARCHAR for prose / book-title / dedication / quote |

The runner also performs a **nested-strand fold** as a C# data migration: it walks each strand tree in DFS preorder, re-parents all beats to the root with contiguous SortKeys, stamps `IsChapterStart=true` + chapter heading on each formerly-nested strand's first beat, and deletes the now-empty child strand rows. Idempotent — already-flat strands skip the re-parent step.

Adding a new migration: drop a new `*.sql` file under `Data/Sql/`, append its filename to the `migrations` array in `ApplyMigrations/Program.cs`, and re-run the console. Keep the file idempotent so prod and dev can run it without coordination.

## Deploying to Azure

The live site is on Azure App Service at **streetsamurai.azurewebsites.net**. `v3/StreetSamurai.Blazor/scripts/cli/deploy.ps1` packages and ships the Blazor app; secrets resolve through App Service Application Settings (with optional Key Vault references) via `MindAttic.Vault`.

The connection string follows the family priority chain:

1. `ConnectionStrings__StreetSamurai` env var (App Service Application Setting in production)
2. `ConnectionStrings:StreetSamurai` from `IConfiguration` (`appsettings.json`)
3. LocalDB fallback

Production never reads from the LocalDB fallback — it always resolves the connection string from Application Settings.

## Tests

```powershell
dotnet test v3/StreetSamurai.UnitTests
```

NUnit fixtures cover canon services, the Quorum review aggregator, the embedding cache, and Razor component rendering through bUnit. The `StrandWorkbenchServiceTests` fixture in particular covers the writing surface — insert / split / join / delete / update-text, gap-after-beat round-trip (`SetGapAfterAsync` / `ClearGapAfterAsync` / `ComputeTrailingSilenceMs`), and the `BeatMetadataUpdate` flow for `IsChapterStart` + `Kind` (lowercased, trimmed, blank falls back to `prose`).

End-to-end UI coverage runs via Cypress at the repository root:

```powershell
npx cypress run                       # headless
npx cypress open                      # interactive
```

The strand suite is split in two:

- `cypress/e2e/strand-smoke.cy.js` — round-trip happy path: create strand → insert beat → edit via click → insert second → delete. Doesn't touch TTS.
- `cypress/e2e/strand-ux.cy.js` — full UX coverage: positional `Beat #N`, footer action buttons (copy-text / ✨ LLM / 🎙 re-record / 🗑 delete), inline gap-after editor with save + reset, `IsChapterStart` toggle producing a divider + chapter-index entry, `Kind` dropdown rendering a beat as a blockquote with attribution, LLM bottom-sheet with `strand-guid.beat-guid` handle and id-copy.

Both specs require an authenticated session (`cy.ensureAuthenticated()`); in dev the auto-login middleware handles it, otherwise pass an auth cookie via `cypress run --env auth_cookie=...`.

---

## Status

In active development. A live site, a working bookshelf, a Quorum review pipeline shipping findings, an MCP server registered, an audiobook MVP in flight, and a Strand / Episode pipeline being scaffolded in. The hundred-story outline is real; the world is being filled in around the prose as it gets written.

The graffiti philosophy applies: accumulation and imperfection make art. Sterile perfection makes nothing.
