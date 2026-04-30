# tools/

Standalone CLI utilities for the StreetSamurai canon. These complement the
in-app pipeline (`StreetSamurai.Server` + the MCP server) by providing
bash-callable analyzers that future Claude Code sessions can run without
touching the C# build.

> **Note** — the prior `extract-lore-triples.js` and
> `nightly-lore-triple-sweep.cmd` were retired on 2026-04-30 when the unified
> `ContinuityService` + `ContinuityExtractionService` + `ContinuityApplyService`
> took over. Run continuity operations via the C# CLI (`dotnet run --project
> StreetSamurai.Blazor -- --continuity ...`) or the `/continuity` UI page,
> or via the MCP tools `extract_continuity_from_chapter` /
> `extract_continuity_from_book` / `list_continuity_contradictions` /
> `resolve_continuity_contradiction` / `apply_continuity_claim`. The old
> per-entity JSON store at `engine/data/continuity/*.json` was migrated into
> `engine/data/continuity.db` and is now legacy backup; leave it in place
> until you've verified the new store has everything you need.

## check-contradictions.js

Reads a chapter, builds a canon-context bundle from the entities the chapter
touches plus the book's prior canon, dispatches a [Legion](https://D:/Projects/MindAttic/MindAttic.Legion)
Quorum vote with a contradiction-finding rubric, and returns a JSON report of
flagged contradictions with citations.

### What it catches

The script is tuned for the kinds of contradictions that surface during
restructuring passes:

- **EPISTEMIC** — a character is shown to know or reference a fact they have no
  plausible source for knowing (e.g., Hua referencing Kyle's False Death
  Protocol when that capability is in his Atlas Division medical file and
  nowhere on his roster file)
- **TEMPORAL** — an event is referenced in a sequence inconsistent with prior
  chapters (e.g., Kyle being on his "fifth Lotus chamber visit" when The
  Interview just happened six weeks ago)
- **CAPABILITY** — a character demonstrates an ability they should not have, or
  fails to use one they should
- **CANON** — a stated fact directly conflicts with an entity record or book
  state_at_end

### Prerequisites

The Legion CLI must be built and reachable. By default the script looks at:

```
D:/Projects/MindAttic/MindAttic.Legion/MindAttic.Legion.Cli/bin/Debug/net10.0/legion.exe
```

Override with the `LEGION_BIN` environment variable.

API keys for the LLM providers are read from the shared MindAttic credential
store at `%APPDATA%/MindAttic/LLM/`. At least one provider must have a key.

### Usage

```bash
# Check a single chapter
node tools/check-contradictions.js 019db31fe8887c97a04965978b5ccdb3

# Stricter quorum
node tools/check-contradictions.js <chapter_id> --quorum twothirds

# Larger response budget per voter
node tools/check-contradictions.js <chapter_id> --max-tokens 4096

# Tighter canon-context budget (default 80000 chars)
node tools/check-contradictions.js <chapter_id> --max-context-chars 60000

# Skip the narrative synthesis (one fewer LLM call, faster)
node tools/check-contradictions.js <chapter_id> --no-narrative

# Inspect the assembled prompt without calling the LLMs
node tools/check-contradictions.js <chapter_id> --dry-run
```

### Output

JSON on stdout:

```jsonc
{
  "chapter_id": "...",
  "chapter_title": "Street Meat",
  "book_id": "...",
  "characters_in_scope": ["Kyle Ellen Corbin-Vasik", "Hua", "..."],
  "prior_chapters_count": 4,
  "canon_context_chars": 24818,
  "voters": 7,
  "total_voters": 8,
  "findings_count": 2,
  "findings": [
    {
      "type": "EPISTEMIC",
      "snippet": "False Death Protocol delivery. The Vultures will pick you up...",
      "conflict": "Hua's character record states she reads files; the FDP is in Kyle's Atlas Division medical file which is not in any roster Hua has access to. No source for this knowledge is established in canon.",
      "severity": "high",
      "fix_suggestion": "Reframe the FDP as Kyle's own internal proposal; have Hua specify only the egress window.",
      "flagged_by": ["claude", "openai", "gemini"]
    }
  ],
  "legion_narrative": "..."
}
```

Exit code: `0` if no contradictions, `1` if findings present, `2` on usage or
pipeline error.

### What it does NOT do (yet)

- It does not validate places, factions, or technology entities — only people +
  book state + prior chapter synopses are loaded. Add other entity types as
  needed.
- It does not parse the chapter's `html` for which characters are referenced;
  it relies on the chapter's `characters` field. If the field is incomplete,
  scope will be too narrow.
- It does not auto-fix. It reports. Apply fixes with the existing Edit tool or
  a manual rewrite pass.

### Companion future work

A C# MCP tool (`find_contradictions`) that runs the same logic in-process
would be a natural follow-on — it could be invoked from Claude Code without
the Legion CLI shell-out, and could share state with the rest of the
StreetSamurai server. The current Node script is the prototype; promote when
the design has stabilized.
