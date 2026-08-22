---
description: Look up anything in the Prose database by loose natural-language tuple and view it as a private Artifact.
argument-hint: "<subject> [aspect]  e.g. \"character m-101\", \"entity lyra\", \"kyle weapon\", \"pixel friends\", \"arcsec employees\", \"silence description\""
allowed-tools: Bash, Artifact, AskUserQuestion, Skill
---

# /show — database lookup as an Artifact

## Status: blocked pending a Hub-routed lookup path (2026-08-22)

This command previously instructed querying the database directly via raw `sqlcmd`. That is no
longer allowed under any circumstances — nothing reaches the database except through Prose.Hub
(HARD, absolute rule; see project memory `feedback_all_writes_through_hub`). No CLI `--flag` or
MCP tool currently exposes the flexible, arbitrary-lookup capability this command relies on.

**Do not fall back to raw `sqlcmd` to make this command work.** If the user invokes `/show`, tell
them a proper Hub-routed lookup path needs to exist first (e.g. an MCP tool or CLI command that
accepts a subject/aspect query and returns the resolved data), and ask whether they want that
built now or want the specific lookup done some other already-Hub-routed way.

Everything below (§1-5) is preserved as domain-knowledge reference for building that real
replacement — the resolution logic and table mappings are still correct, they just need to run
through Hub instead of a raw connection string.

---

The user gives a loose, unordered tuple of words identifying something in the live Prose SQL
database (not a file, not memory) and wants to *see* it — a readable profile page, not a wall of
SQL output. `$ARGUMENTS` is that tuple, e.g. `character m-101`, `entity lyra`, `kyle weapon`,
`pixel friends`, `pixel home`, `silence description`, `arcsec employees`. Word order never
matters and the tuple is never a fixed schema — interpret it with judgment, the same way you'd
answer if the user had just asked the question in prose.

## 1. Split the tuple into a *subject* and an optional *aspect*

There is no fixed grammar — figure out which word(s) name the **thing** and which word(s) name
the **lens** to view it through:

- **Subject**: a proper name, slug, or book code — e.g. `m-101`, `lyra`, `kyle`, `pixel`,
  `silence`, `arcsec`. This is what gets resolved to a row.
- **Aspect** (optional): either
  - an **EntityType filter** narrowing which kind of row to resolve to (`character`, `weapon`,
    `place`, `faction`, `technology`, `document`, `vocabulary`, etc. — the full list is
    `SELECT DISTINCT EntityType FROM Entities`), or
  - a **relational lens** naming what slice of that entity's data to show: `friends`/`allies`,
    `family`, `employees`/`members`, `weapon`/`weapons`/`gear`, `home`/`turf`, `description`,
    `wounds`, `speech`/`voice`, `timeline`, `relationships`, etc. Treat these as hints toward
    real tables (see §3), not a closed enum — infer intent for a lens word you haven't seen
    before rather than refusing.

A bare single word (`/show lyra`) has no aspect — resolve the subject and show a full, broad
profile (§3's "no aspect given" case).

## 2. Resolve the subject to exactly one row

The corpus has two resolvable kinds of thing:

- **Entities** (`Entities` table, `EntityType` discriminator, TPT joined to a type-specific table
  by shared `Id` — `Characters`, `Weapons`, `Places`, `Factions`, `Technologies`, etc.). Match
  against `Entities.Name`, `Entities.Slug`, **and** the matching `<Type>Aliases` table
  (`CharacterAliases`, `PlaceAliases`, `FactionAliases`, `WeaponAliases`, `TechnologyAliases`,
  ... — same naming pattern per type) so nicknames/handles resolve too (e.g. "Doc Stash" finding
  the entity whose real name is different).
- **Nodes** (`Nodes` table, `Kind` = `book`/`chapter`) — book codes like `m-101`, `bcoda`, titles,
  slugs. A subject like `m-101`, `pixel`, `silence` is very likely a Node, not an Entity — check
  both, but weight a strong title/slug match on Nodes highly for anything that looks like a book
  code or a book-length concept.

Search case-insensitively, unanchored (`LIKE '%term%'`) if an exact match returns nothing.
Combine candidates from all matching sources into one list. Scope by `UniverseId` only if the
aspect or an obvious cue narrows it — otherwise search every universe (`glmz`, `scry`,
`nonfiction`, `horror`, `erotica`, `gospel`, `fiction`) since the same name can legitimately exist
in more than one (e.g. "Adam" in `nonfiction` vs. an unrelated "Adam" in `fiction` — not a bug,
just two different books).

- **Zero matches**: say so plainly, show the closest few `LIKE` hits you did find (if any) as
  suggestions, and stop — don't guess.
- **Exactly one strong match**: proceed straight to §3, no need to ask.
- **Multiple plausible matches**: use `AskUserQuestion` — one option per candidate, each labeled
  with enough context to tell them apart (name, EntityType or Node Kind, universe, book/slug,
  and a one-line description snippet). Never guess silently when it's genuinely ambiguous.

If the aspect named an EntityType (e.g. `/show pixel character` vs. a place also named "Pixel"),
use it as a hard filter during resolution, not just afterward.

## 3. Gather the data for that one resolved row

**If an aspect names a specific relational lens**, query precisely that — don't dump everything.
Map the lens to the real table(s) that hold it for this entity's type, e.g.:

- `friends`/`allies` → `CharacterRelationships` (both directions, `SourceEntityId`/`TargetEntityId`)
  filtered/sorted toward positive relationship types
- `family` → `CharacterRelationships` filtered toward kin-type relationships, plus
  `CharacterAncestryDetails`/`CharacterGeneticAncestries` if relevant
- `employees`/`members` → `FactionMembers` (for a faction subject) or `CharacterAffiliations`
  (for a character subject, the reverse direction)
- `weapon`/`weapons`/`gear` → `CharacterBelongingsGear`, `WeaponKnownUsers`, or the `Weapons`
  row itself if the subject *is* a weapon
- `home`/`turf` → `CharacterHomeTurfs`, `CharacterTerritoryZones`, or the linked `Places` row
- `description` → just `Entities.Description` (and the type-specific table's own description
  field if richer) — no need to join anything else
- `wounds` → `WoundLedger`
- `speech`/`voice` → the `Characters` row's `Speech*`/`Psychology*` columns (SS-A46 register)
- `timeline` → `CharacterTimeline` / `CharacterTimelineBodyChanges`
- a Node subject with any book-shaped aspect (chapters, beats, characters, arc) → `Nodes.NodeBible`,
  `BookChapterOrder`/`Chapters`, `BookProtagonists`, `NodeChapterSummaries`

If a lens word doesn't map cleanly to a table you can see, introspect first
(`SELECT name FROM sys.columns WHERE object_id = OBJECT_ID('<Table>')`) rather than guessing at
a column name that doesn't exist.

**If no aspect was given**, assemble a broad, generally-interesting profile instead of a full
data dump: description, key relationships/affiliations, a couple of the most distinctive
type-specific fields (e.g. a character's psychology/speech register, a weapon's specs, a
faction's goals), mention count (`BeatEntityMentions`, note it can undercount older content —
don't state it as exact usage), and — if it's a Node — chapter count, POV/protagonist, and
status. Curate for what's actually populated; don't render empty sections.

## 4. Render as an Artifact

Load the **`artifact-design`** skill before writing the HTML — this is a real per-invocation
design decision (a character profile reads differently than a weapon spec sheet or a book
overview), not a fixed template to reuse verbatim across calls. Write the file to the scratchpad
directory, then `Artifact` it (private by default — don't ask before publishing, `/show` is
meant to produce a quick private view every time).

- Title: the resolved entity/node's name (not "Show" or a description of the lookup).
- Favicon: pick per kind — e.g. 🧑 character, 🗡️ weapon, 🏛️ faction, 📍 place, 📖 book/node,
  🔧 technology/cyberware/equipment — reuse the same emoji choice consistently across `/show`
  calls for the same EntityType so repeat lookups feel like the same "app."
- Never show raw internal ids/GUIDs in the body copy — they're plumbing, not content. If you
  need to reference a specific row for a later action, that's fine in your own reasoning, just
  don't print it to the page.
- Keep it to one focused page — this is a lookup, not a dashboard.

## 5. After publishing

Tell the user in one line what was resolved (name + kind + universe if it was ambiguous) and
hand them the artifact link. Nothing else.
