# tools/

Standalone utilities for the StreetSamurai canon. These complement the in-app pipeline and MCP server by providing CLI tools that can run independently.

## check-contradictions.js

Reads a chapter, builds a canon-context bundle from the entities the chapter touches plus the book's prior canon, dispatches a Legion Quorum vote with a contradiction-finding rubric, and returns a JSON report of flagged contradictions.

### What it catches

- **EPISTEMIC** — a character references a fact they have no plausible source for
- **TEMPORAL** — an event is referenced in a sequence inconsistent with prior chapters
- **CAPABILITY** — a character uses an ability they shouldn't have, or fails to use one they should
- **CANON** — a stated fact directly conflicts with an entity record or book `state_at_end`

### Prerequisites

Legion CLI built and reachable (default: `D:/Projects/MindAttic/MindAttic.Legion/MindAttic.Legion.Cli/bin/Debug/net10.0/legion.exe`). Override with `LEGION_BIN` env var. At least one LLM provider key in `%APPDATA%/MindAttic/LLM/`.

### Usage

```bash
node tools/check-contradictions.js <chapter_id>
node tools/check-contradictions.js <chapter_id> --quorum twothirds
node tools/check-contradictions.js <chapter_id> --max-tokens 4096
node tools/check-contradictions.js <chapter_id> --dry-run   # inspect prompt, no LLM calls
```

Exit codes: `0` = no contradictions, `1` = findings present, `2` = usage or pipeline error.

> **Note:** The prior `extract-lore-triples.js` and `nightly-lore-triple-sweep.cmd` were retired 2026-04-30. Continuity operations now run via `dotnet run --project v3/StreetSamurai.Cli -- --continuity ...` or the MCP tools `extract_continuity_from_chapter` / `list_continuity_contradictions` / `resolve_continuity_contradiction`.

---

## chatterbox/

Free, fully-local expressive TTS adapter using Resemble AI's **Chatterbox** model (MIT license). Heavier than Kokoro — benefits from a CUDA GPU but runs on CPU for overnight batches. See [`chatterbox/README.md`](chatterbox/README.md) for setup and voice-cloning options.

---

## kokoro/

Free, fully-local TTS adapter using **Kokoro-82M** (Apache-2.0). Runs comfortably on CPU — the recommended free default. See [`kokoro/README.md`](kokoro/README.md) for setup and voice options.

---

## Other scripts

One-off SQL and JS migration scripts used during canon restructuring passes. These are left in place as a record and are not part of the active runtime.
