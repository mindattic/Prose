---
description: Write up N suggestions of cool things of one kind (texture, cyberware, landmarks, weapons, factions and so on) for the author to choose from.
argument-hint: "<kind> [count, default 10]"
allowed-tools: PowerShell, Bash
---

# /more: suggestions only

`/more <kind> [count]`, e.g. `/more texture 10`, `/more cyberware 10`, `/more landmarks 10`.

This command **only writes suggestions.** It makes nothing real: no rulings, no entities, no prose. The author decides, item by item, whether each one becomes prose, an entity, or nothing. Do that work only when the author says so, the way they say to.

## Do this

1. **Parse `$ARGUMENTS`.** The first word is the kind (free text: texture, cyberware, landmarks, weapons, vehicles, factions, gangs, food, slang, jobs, rituals, anything). The number is the count, default 10.
2. **Know what already exists, so you don't repeat it:**
   - Read the active rulings (`list_rulings`, law kind, for the book and the universe). Earlier suggestion rounds record what was accepted and rejected. Never re-offer a rejected idea, and never repeat an accepted one.
   - For entity-like kinds, check the world first (`find_entities` / `list_cyberware` / `list_places` and similar) so each suggestion is new, not a record that already exists.
3. **Write the list:** numbered items, each a **bold name** plus one to three concrete sentences. Each item must:
   - fit every standing world law, in particular:
     - no devices or cash; AR windows and credsticks only
     - hyperreality, never "HR"
     - the entity never speaks to Kyle
     - traffic is auto-driven
     - CorpoNations are right angles and ignore the Seams
   - be specific, and usable on a page or as a record, rather than a mood.
   For landmarks, give where it is (real Chicago geography only if checked live, per ruling 01a0d47e) and what you'd see there. For gear, give who makes it, what it does, and what it costs in Φ.
4. **Stop.** Ask which to keep, rename or change, and whether each should become prose, an entity, or both. Don't record, create or splice anything until the author answers.
