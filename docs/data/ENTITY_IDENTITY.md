---
codex: 1
project: StreetSamurai
code: SS
layer: data
status: living
updated: 2026-06-07
---

# StreetSamurai — Master Entity-Identity Table (L5)

> The register of structured canon-as-data. Per [SS-LAW-1](../BIBLE.md#SS-§5) the **live**
> authoritative canon is the SQL database (`Entities` + `Records.Json`); `engine_data/*.json` is the
> **seed/export mirror**. This table maps, per type: the on-disk corpus → its JSON Schema → its
> stable `id` derivation → its name/key fields. Registration only — **no canon values were
> rewritten** and the files were **not relocated** (see [RFC 0001](../rfc/0001-canon-as-data-registration.md)).

## Identity scheme

Stable entity id = `<type>.<slug>` where `slug` is the lowercase, hyphen-collapsed form of the
entity's name (the same slug the SQL importer writes to `Entities.Slug`). Three types already carry
an explicit opaque hex `id` on disk and use that verbatim. Cross-references in prose/docs cite the
`id`, never the fields.

## Types

| Type (`EntityType`) | Corpus dir | Files | Schema | Name/key field | `id` derivation |
|---|---|---:|---|---|---|
| `character` | `engine_data/people/` | 200 | [character.schema.json](_schema/character.schema.json) | `name` (+ `species`) | `character.<slug(name)>` |
| `geneware` | `engine_data/geneware/` | 100 | [geneware.schema.json](_schema/geneware.schema.json) | `name` | `geneware.<slug(name)>` |
| `elf` | `engine_data/elfs/` | 107 | [elf.schema.json](_schema/elf.schema.json) | `name` | `elf.<slug(name)>` |
| `quote` | `engine_data/quotes/` | 512 | [quote.schema.json](_schema/quote.schema.json) | `attribution` + `source` + `quote` | `quote.<slug(attribution)>-<hash>` |
| `document` | `engine_data/documents/` | 157 | [document.schema.json](_schema/document.schema.json) | `file_name` / `title` | `document.<file_name>` |
| `lab_specimen` | `engine_data/lab_specimens/` | 52 | [lab_specimen.schema.json](_schema/lab_specimen.schema.json) | `name` (has on-disk `id`) | on-disk hex `id` |
| `psionic` | `engine_data/psionics/` | 5 | [psionic.schema.json](_schema/psionic.schema.json) | `name` (has on-disk `id`) | on-disk hex `id` |
| `transportation` | `engine_data/transportation/` | 100 | [transportation.schema.json](_schema/transportation.schema.json) | `name` | `transportation.<slug(name)>` |
| `wasteland_entity` | `engine_data/wasteland_entities/` | 6 | [wasteland_entity.schema.json](_schema/wasteland_entity.schema.json) | `name` (has on-disk `id`) | on-disk hex `id` |

`engine_data/archives/ceramic_men/` (4 files) is an archived sub-corpus and is intentionally not
registered as a live type.

## Notes

- The full ~28-type canon lives in SQL; this table registers only the structured types that have an
  on-disk seed corpus under `engine_data/`. Other types (places, factions, weapons, materials, …)
  are DB-native and described in [BIBLE.md §4.2](../BIBLE.md#SS-§4).
- Schemas are **descriptive** (`additionalProperties: true`) so they register existing shapes
  without rejecting valid canon. They are not a migration; canon values are untouched.
- The `id`-on-import phase (stamping `<type>.<slug>` onto `Entities.Slug` so on-disk ↔ DB ↔ doc ids
  are one value) is RFC 0001 phase 2, future.
