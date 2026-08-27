# Consumer Onboarding — Using Prose as a Portable Writing Service

Prose is the repository of universes for the MindAttic family of projects (RFC 0007,
`docs/rfc/0007-universe-interchange.md`) — not just the writing program for its own books, but a
canon store and prose-generation service other local apps can read from, write to, and request
narrative/dialog from. This doc is the generic, step-by-step runbook for plugging in a **new**
sibling project ("the consumer") — no consumer-specific code changes to Prose are required for any
step below; every mechanism here is already generic (verified against source, not just docs).

If you're onboarding ExperimentEve specifically, its own integration already exists — this doc is
for the *next* consumer.

## 0. Prerequisites

- **Prose Hub must be running** on `http://127.0.0.1:5900` (loopback only). In this repo it's kept
  alive automatically by `.claude/hooks/start-prose-hub.ps1` (a SessionStart hook) — check
  `GET /api/health` returns 200 if you're unsure.
- **Get the shared Hub API key.** The Hub generates one automatically on first startup and stores
  it in the shared settings file at `%LOCALAPPDATA%\MindAttic\Prose\Settings.json`
  (`HubApiKey` field) — read it from there once. Every request to a protected endpoint (listed
  below) must send it as an `X-Prose-Key` header. Unprotected read endpoints (`/api/health`,
  `/api/universes`, entity/search/snapshot reads) need no header.

## 1. Register your universe

Nothing to do manually — universe registration is **automatic** on first import (confirmed live in
`UniverseInterchangeService.ImportAsync` → `FindOrCreateUniverseAsync`). Step 2 below both creates
your universe row and seeds its first entities in one call.

## 2. The interchange file — your canon, in Prose's terms

Write `<your-app>/universe/<slug>.universe.json` conforming to
`docs/schemas/universe.schema.json` (copy it into your repo if convenient — it's a stable,
Prose-agnostic contract, not something that changes per consumer):

```json
{
  "universe": {
    "id": "your-slug",
    "name": "Your Project Name",
    "tagline": "...", "era": "...", "setting": "...", "logline": "...",
    "rules": ["hard creative law 1", "hard creative law 2"]
  },
  "entities": [
    { "id": "some-character", "type": "character", "name": "...", "summary": "...",
      "details": {}, "relations": [{ "to": "another-entity", "kind": "ally" }], "tags": [] }
  ]
}
```

`type` is an open string set (core: character, faction, creature, location, artifact, event, rule,
organization, concept — anything else gets a generic `EntityType` and an auto-registered
`RepositoryDefinition`, no Prose-side code needed). A `relations[].to` pointing at an id not yet in
your file is fine — it imports as an edge to an auto-created stub entity, marking future work.

**Import / export / sync** (CLI — MCP equivalents are `import_universe_file`/`export_universe_file`,
both header-protected the same way):

```
prose --universe-import <path-to-your-file> [--universe <slug>]   # creates the universe on first run
prose --universe-export <slug> <path>                              # dump current DB state back to JSON
prose --universe-sync <path>                                       # import, then export back (normalizes)
```

Idempotent: re-importing the same file is a no-op diff. `POST /api/universes/{slug}/import` (with
the JSON body and the `X-Prose-Key` header) does the same thing over HTTP for a game-side push.

## 3. Register as an Outbox consumer (no registration step — just start using it)

The Outbox is Prose's proactive "I have something for you" channel — it's how a Prose session can
tell your project's own Claude Code window "the scene you asked for is ready" without a human
relaying it. `consumer` is a free-form string; there is nothing to pre-register. Pick a name
(convention: your universe slug) and:

- `GET /api/outbox/{consumer}` (header required) — drains pending events, marks them delivered.
  Add `?peek=true` to read without consuming.
- `POST /api/outbox/{consumer}` (header required) with `{kind, summary, data?}` — enqueue an event
  (either your session posting to itself/others, or a script noticing something worth flagging).

Install a `UserPromptSubmit` hook in your own repo that drains your consumer's queue on every
prompt — copy-paste template (PowerShell, mirrors this repo's own hook conventions):

```powershell
# .claude/hooks/prose-outbox.ps1 — drains this project's Prose Outbox queue on every prompt.
$ErrorActionPreference = 'Continue'
$consumer = 'your-slug'
$apiKey   = (Get-Content "$env:LOCALAPPDATA\MindAttic\Prose\Settings.json" -Raw | ConvertFrom-Json).HubApiKey

try {
    $resp = Invoke-RestMethod -Uri "http://127.0.0.1:5900/api/outbox/$consumer" `
        -Headers @{ 'X-Prose-Key' = $apiKey } -TimeoutSec 3
    if ($resp.Count -gt 0) {
        $lines = $resp | ForEach-Object { "- [$($_.kind)] $($_.summary)" }
        $context = "Prose Outbox — new events since your last prompt:`n" + ($lines -join "`n")
        Write-Output (@{ hookSpecificOutput = @{ hookEventName = 'UserPromptSubmit'; additionalContext = $context } } | ConvertTo-Json -Depth 5)
        exit 0
    }
} catch { } # Hub unreachable / no key yet — fail silent, never block the prompt

Write-Output '{}'
exit 0
```

Register it in your `.claude/settings.json` under `hooks.UserPromptSubmit`, same shape as any other
hook. A Prose session enqueues `POST /api/outbox/your-slug` deliberately when it wants your window
to know something; some services also auto-enqueue (universe import/export, entity upserts) using
the universe slug as the consumer name by convention — keep your hook's consumer name matching your
universe slug so those land too.

## 4. Ask Prose to write something

**`generate_scene` / `--generate-scene`** — a scene or line of dialog **without** a pre-existing
Book/Chapter/Beat row. This is the actual "give me narrative/dialog" capability.

Three equivalent entry points, all requiring the `X-Prose-Key` header over HTTP:

| Entry point | Call |
|---|---|
| CLI | `prose --generate-scene "<beat goal>" [--characters "A,B"] [--location "..."] [--subtext "..."] [--node <slug>] [--universe <slug>]` |
| MCP | `generate_scene(beatGoal, characters?, location?, subtext?, node?, universe?)` |
| HTTP | `POST /api/generate-scene` with the same fields as JSON body |

**Ephemeral mode (default, no `--node`)**: pacing, dialogue voice profiles, canon-fact grounding
against your universe's world facts, consequence/gear constraints, ambient sensory grounding, and
entity pre-check warnings all still apply — everything keyed off what you pass in, not persisted
history. Nothing is written to your book corpus.

**Attached mode (`--node <slug>`)**: borrows an existing Book or Chapter's canon and continuity
(its doc-context stack, entity working memory, open threads, prior-chapter summary) without
writing a Beat row to it — useful for "give me a line Kat would say here, consistent with what's
already on the page."

Always pass `--universe <your-slug>` (or set `PROSE_UNIVERSE`) — like every other Prose command,
`generate_scene` does not silently default to GLMZ.

## 5. Pull dialog once it exists

**`export_barks` / `--barks-export`** — walk your universe's (or one book/chapter's) beats and
return every beat with a single recorded POV speaker as `{barkId, speakerEntitySlug, text,
context}`. A beat with no recorded speaker is skipped and counted, never silently dropped.

```
prose --barks-export <your-slug> <output-path> [--node <slug>]
```

MCP: `export_barks(universe, node?)`. No HTTP endpoint yet (added alongside a real push/pull
convention once your project needs one, the same way RFC 0007 added `/api/universes/{slug}/import`
specifically for ExperimentEve's `npm run universe -- push`).

## 6. What's NOT built yet

RFC 0007's "Phase 2" idea — treating full game-writing deliverables (a Game Design Document, a
complete game script, a bark *sheet* authored end-to-end as a book) as ordinary Prose Books with
their own Book→Chapter→Beat spine — remains **design-only**, not implemented. `generate_scene` and
`export_barks` cover the "ask for narrative/dialog" and "get dialog back out" halves generically;
authoring a whole structured deliverable still means the normal human-directed
Book→Chapter→Beat authoring workflow (see this repo's own `docs/BIBLE.md` §10 pipeline), just in
your universe instead of GLMZ/SCRY.
