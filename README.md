# StreetSamurai

**A literary fiction engine for a cyberpunk century.**

StreetSamurai writes novels. Not snippets, not summaries — chapter-length prose, voice-disciplined, canon-grounded, ready for the bookshelf. It is the authoring stack for *Bushido Coda* and a hundred stories beyond it, set in the GLMZ: a 500-kilometer vertical megacity stacked along the western shore of Lake Michigan in the year 2225, where ferrocement waves rise a hundred stories above the lake and corponations hold sovereignty the old nations could not.

Live at **[streetsamurai.azurewebsites.net](https://streetsamurai.azurewebsites.net/)**.

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
| Embeddings | DI-registered `EmbeddingService`, ~10k entities cached, drift-detected |
| Generation | Multi-provider Quorum via [MindAttic.LLMVoting](https://github.com/mindattic/LLMVoting) — 11 LLM providers, scored evaluation, character personas |
| Review | `BookReviewService` + `WritingQualityService` + `MotifService` + continuity contradiction sweep (Legion-backed) |
| Export | EPUB / HTML / Markdown via `BookExportService` |
| Audio | ElevenLabs TTS pipeline, per-beat narration |
| Agents | `StreetSamurai.Mcp` — Model Context Protocol server exposing canon to Claude clients |
| Credentials | Cloud-native resolution via [MindAttic.Vault](https://github.com/mindattic/Vault) |

---

## Status

In active development. A live site, a working bookshelf, a Quorum review pipeline shipping findings, an MCP server registered, an audiobook MVP in flight, and a Strand / Episode pipeline being scaffolded in. The hundred-story outline is real; the world is being filled in around the prose as it gets written.

The graffiti philosophy applies: accumulation and imperfection make art. Sterile perfection makes nothing.
