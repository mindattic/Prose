---
codex: 1
project: StreetSamurai
code: SS
layer: rfc
status: planned
updated: 2026-06-07
---

# RFC 0001 — Register engine_data as the L5 canon-as-data layer

## Problem

StreetSamurai's structured narrative canon exists in two places: the **live SQL database** (the
only authoritative store, per [SS-LAW-1](../BIBLE.md#SS-§5)) and an on-disk **seed/export mirror**
under `engine_data/*.json` (people, geneware, elfs, quotes, documents, lab_specimens, psionics,
transportation, wasteland_entities). The on-disk corpus has no formal schema and no explicit stable
`id` field — files are keyed implicitly by filename and an in-blob `name`/`quote`/`file_name`. The
Codex standard wants L5 canon-as-data with schemas + stable ids + a master entity-identity table,
*without rewriting canon values*.

## Options compared

1. **Inject `id` into every `engine_data/*.json`.** Cleanest for tooling, but **rewrites canon
   values** — forbidden by this task's constraints and risky against the live DB seed path.
2. **Copy the corpus into `docs/data/*.json` with ids.** Duplicates ~1,200 files and creates a
   second home for a fact — violates "single home per fact."
3. **Register-in-place (chosen).** Author JSON Schemas under `docs/data/_schema/<type>.schema.json`
   describing the *existing* shapes, and a master entity-identity table
   ([ENTITY_IDENTITY.md](../data/ENTITY_IDENTITY.md)) defining the deterministic `id` derivation
   (`<type>.<slug-of-name>`) mapping name ↔ id ↔ key fields. No canon file is touched.

## Decision

Adopt option 3. The schemas are descriptive (additive `additionalProperties: true`) so they never
reject existing canon; the identity table documents the id scheme the live importer already implies
(slug ← name). The doctor validates any JSON that lives *inside* `docs/data/` against its schema;
the `engine_data/` corpus is registered (schema + identity table) but not relocated.

## What NOT to do

- Do **not** add `id` keys to `engine_data/*.json` (rewrites canon).
- Do **not** duplicate the corpus into `docs/data/` (two homes per fact).
- Do **not** make the generator read these files — SQL remains the live path
  ([SS-LAW-1](../BIBLE.md#SS-§5)).

## Phased plan (with risk)

1. **Schemas + identity table (this RFC, low risk).** Describe the nine canonical types; ship the
   master table. ✅ done as part of SS-A1.
2. **id-on-import (medium risk, future).** When the SQL importer runs, stamp the derived
   `<type>.<slug>` id onto the `Entities.Slug` so on-disk ↔ DB ↔ doc ids are one value end-to-end.
3. **Doctor coverage (low risk, future).** Extend `codex.ps1 doctor` to spot-check a sample of
   `engine_data` files against the registered schemas (currently it validates `docs/data/` only).

## Graduates into

- [BIBLE.md §4.4 Canon-as-data](../BIBLE.md#SS-§4)
- [USER_STORIES.md Epic A](../USER_STORIES.md) (canon-as-database foundation)
