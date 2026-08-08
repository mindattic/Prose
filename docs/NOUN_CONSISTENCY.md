# Noun Consistency — Deprecated Name Registry

**Service:** `NounConsistencyService`
**Table:** `DeprecatedEntityNames`
**CLI:** `prose --validate-nouns --slug <slug>`
**MCP:** `validate_nouns`, `add_deprecated_name`, `list_deprecated_names`

---

## What It Is

A deterministic (no-LLM) scan that flags prose beats using a deprecated or renamed noun
reference. Any named thing — character handle, drone name, job title, place name, tech brand —
that gets renamed has its old name registered here. The scanner catches beats that still use
the old name before export.

## Why It Exists

The PNHL VacCell → Nit rename (2026-07-04) slipped through to export. Nine beats in PNHL
still called Pixel's drone "VacCell" when the bible and all other references used "Nit".
This system makes that class of error detectable in one command.

## How Rules Work

Rules live in `DeprecatedEntityNames`. Each rule has:

| Field | Description |
|---|---|
| `DeprecatedName` | The old/wrong name to flag in prose |
| `CanonicalName` | The correct replacement to show in violation reports |
| `UniverseId` | GLMZ rules don't fire in Fantasy and vice versa |
| `EntityId` | Optional FK to the canonical Entity row when it exists |
| `Notes` | Reason for the rename (e.g. "Renamed in SS-A38") |

Matching is **whole-word, case-insensitive**. "VacCell" matches "VacCell" and "vaccell"
but not "VacCellular". One violation is reported per rule per beat.

## Seeded Rules

| Deprecated | Canonical | Universe | Notes |
|---|---|---|---|
| `VacCell` | `Nit` | GLMZ | Pixel's drone — old name used before PNHL noun audit 2026-07-04 |

## Adding a New Rule

**Via MCP** (preferred in prose sessions):
```
add_deprecated_name("OldName", "NewName", notes="Why it was renamed")
```

**Via CLI** (not yet implemented — use MCP):
```
prose --validate-nouns --slug <slug>
```

**Via sqlcmd** (emergency / bulk seed):
```sql
SET QUOTED_IDENTIFIER ON;
INSERT INTO DeprecatedEntityNames (UniverseId, DeprecatedName, CanonicalName, Notes, AddedAt)
VALUES ('0197E9C9-0001-7000-8000-000000000001', 'OldName', 'NewName', 'reason', GETUTCDATE());
```

## When to Run

- **Before export** — run `validate_nouns` on any story that has had noun renames since its
  last export.
- **After a rename** — scan all GLMZ stories immediately after registering the deprecated rule.
- **During logic sweep** — a Logic Sweep (SS-A44) checks causality and continuity; a noun scan
  checks naming consistency. They are complementary, not overlapping.

## Relationship to Existing Alias Tables

`CharacterAlias`, `WeaponAlias`, etc. store **valid alternate names** that *can* appear in
prose (nicknames, handles, formal titles). `DeprecatedEntityNames` stores names that **must
not** appear in prose. These are complementary, not overlapping.
