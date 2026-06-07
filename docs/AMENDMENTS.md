---
codex: 1
project: StreetSamurai
code: SS
layer: amendments
status: living
updated: 2026-06-07
---

# StreetSamurai — Amendments (append-only; amendment wins over the bible)

> Append-only. Never rewrite an amendment; supersede it with a new one. Beyond ~25, fold into the
> bible and start a new epoch (note the git tag); history stays in git.

## SS-A1 — Adopt the Codex documentation standard (supersedes —)

**What changed.** Installed the MindAttic Codex standard. `ARCHITECTURE.md` (the prior software
source of truth) was migrated into [docs/BIBLE.md](BIBLE.md) (L0). Its goal tables became
[docs/USER_STORIES.md](USER_STORIES.md) (L2). The continuity-invariants list from
`v3/canon_writes/story_state.md` was promoted into BIBLE §5 as narrative laws
[SS-LAW-9](BIBLE.md#SS-§5)…[SS-LAW-14](BIBLE.md#SS-§5); the engine invariants from `ARCHITECTURE.md`
§2a became [SS-LAW-1](BIBLE.md#SS-§5)…[SS-LAW-6](BIBLE.md#SS-§5); CLAUDE.md code/world rules became
[SS-LAW-7](BIBLE.md#SS-§5)/[SS-LAW-8](BIBLE.md#SS-§5).

**Why.** One source of truth, stable IDs, a doctor that catches drift, and a SessionStart digest so
every Claude session loads the canon. Replaces ad-hoc, scattered docs.

**Migration / preservation (no content deleted).**
- `ARCHITECTURE.md` is retained as a 1-line pointer to `docs/BIBLE.md` (README links it; tooling may
  still read the path).
- `v3/canon_writes/story_state.md` remains the **session/state scratch notes**; its *invariants*
  now also live (authoritatively) in BIBLE §5.
- `engine_data/*.json` is registered as the **L5 data layer** via schemas under `docs/data/_schema/`
  and the master entity-identity table [docs/data/ENTITY_IDENTITY.md](data/ENTITY_IDENTITY.md). Its
  canon *values were not rewritten*; per [SS-LAW-1](BIBLE.md#SS-§5) it is the seed/export mirror,
  not the live read path.
- Prose draft sprawl recorded, not deleted: the canon prose register is **v8**
  (`engine/bushido_coda_v3/01_bearing_teeth_v8.md` + `00_style_guide.md`). Earlier drafts
  (`engine/bushido_coda_v2/*_v2..v6`, `*_v7`) are superseded historical drafts kept on disk. Prose
  HTML bodies are treated as `generatedFrom` the chapter beats.
- The project rule "no Markdown files except README" (CLAUDE.md) is amended: the Codex `docs/*.md`
  set is the documented exception (it is documentation, not app data). Data files remain JSON.
