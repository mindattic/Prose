---
description: Look up anything in the Prose database by loose natural-language tuple and view it as a private Artifact.
argument-hint: "<subject> [aspect]  e.g. \"character m-101\", \"entity lyra\", \"kyle weapon\", \"pixel friends\", \"arcsec employees\", \"silence description\""
allowed-tools: Bash, Artifact, AskUserQuestion, Skill
---

# /show — database lookup as an Artifact

The user gives a loose, unordered tuple of words identifying something in the live Prose SQL
database (not a file, not memory) and wants to *see* it — a readable profile page, not a wall of
data. `$ARGUMENTS` is that tuple, e.g. `character m-101`, `entity lyra`, `kyle weapon`,
`pixel friends`, `pixel home`, `silence description`, `arcsec employees`. Word order never
matters and the tuple is never a fixed schema — interpret it with judgment, the same way you'd
answer if the user had just asked the question in prose.

**Nothing reaches the database except through Prose.Hub — reads AND writes, no exceptions (HARD,
absolute, 2026-08-22).** All resolution below runs through `prose --show`, which is built and
routes through the running Prose.Hub process — never raw `sqlcmd`.

## 1. Split the tuple into a *subject* and an optional *aspect*

There is no fixed grammar — figure out which word(s) name the **thing** and which word(s) name
the **lens** to view it through. This split is Claude's judgment call, not something the CLI
does — the tool below takes the already-split parts.

- **Subject**: a proper name, slug, or book code — e.g. `m-101`, `lyra`, `kyle`, `pixel`,
  `silence`, `arcsec`. This is what gets resolved to a row.
- **Aspect** (optional): a relational lens naming what slice of the entity's data to show.
  Currently recognized: `friends`/`allies`/`family`/`relationships`, `weapon`/`weapons`/`gear`,
  `home`/`turf`, `employees`/`members`. Anything else (including no aspect) returns the broad
  profile (§3).

A bare single word (`/show lyra`) has no aspect — resolve the subject and show the full, broad
profile.

## 2. Run the lookup

```
prose --show --subject "<subject>" [--aspect "<aspect>"] --json
```

This returns one of three shapes:

- `{"resolved": false, "candidates": []}` — **zero matches.** Say so plainly and stop; don't guess.
- `{"resolved": false, "ambiguous": true, "candidates": [...]}` — **multiple plausible matches.**
  Use `AskUserQuestion` — one option per candidate, each labeled with enough context to tell them
  apart (the response already includes name, kind, source, universe, and a description snippet).
  Never guess silently when it's genuinely ambiguous.
- `{"resolved": true, ...}` — **exactly one match**, with the profile data already assembled.
  Proceed straight to §3.

If the very first call comes back ambiguous specifically because the aspect word got swept up in
`--subject` (e.g. you passed the whole raw tuple), re-split and call again with `--aspect` set.

## 3. What the response contains

For an **entity** (`"source": "entity"`): `name`, `kind` (EntityType), `universe`, `description`,
`mentionCount` (from `BeatEntityMentions` — can undercount older content, don't state it as exact
usage), and, for characters, whichever of `relationships`, `gear`, `homeTurf`, `affiliations` the
resolved aspect (or the broad no-aspect profile, which includes all four) populated. Curate for
what's actually present — an empty/absent field means the character doesn't have that data
recorded, not that the lookup failed.

For a **node** (`"source": "node"`, a book or chapter): `name`, `kind`, `universe`, `description`,
`chapterCount`, `score`.

**Not yet covered by `--show`** (the original design called for these; they need a follow-up
extension to `ShowCli.cs` rather than a workaround here): `wounds` (`WoundLedger`), `speech`/
`voice` (a character's `Speech*`/`Psychology*` columns), `timeline`
(`CharacterTimeline`/`CharacterTimelineBodyChanges`), and node-specific aspects beyond chapter
count/score (POV/protagonist, full bible). If the user asks for one of these, say plainly that
this specific lens isn't wired up yet rather than improvising a raw query.

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
- Never show raw internal ids/GUIDs in the body copy — they're plumbing, not content.
- Keep it to one focused page — this is a lookup, not a dashboard.

## 5. After publishing

Tell the user in one line what was resolved (name + kind + universe if it was ambiguous) and
hand them the artifact link. Nothing else.
