---
description: Suggest N new pieces of world texture for a book's setting, record the author's picks as rulings, and weave them into the page by hand.
argument-hint: "[count, default 10] [book, default BCODA]"
allowed-tools: PowerShell, Bash
---

# More texture: grow the world, then put it on the page

Use this when the author wants the world richer. It has two halves:
- **Suggest.** Offer ideas and wait for the author to answer.
- **Weave.** Record what they accept, then write it into the prose.

Nothing goes into canon or onto the page until the author has picked it.

## 1. Suggest (then stop)

1. Read the active rulings for the book (`list_rulings`, law kind) and the universe. Earlier texture rulings list what was accepted and what was rejected. Never re-offer a rejected idea, and never repeat an accepted one.
2. Write `$ARGUMENTS` new items (default 10), numbered, each with a **bold short name** and one or two sentences. Each item must:
   - fit every standing world law, in particular:
     - no devices or cash; AR windows and credsticks only
     - hyperreality, never "HR"
     - the entity never speaks to Kyle
     - traffic is auto-driven
     - CorpoNations are right angles and ignore the Seams
     - the Vultures and Street Meat
   - be something a rider or walker could see, hear or smell from the street, or a trade or ritual of the Seams
   - be concrete and specific rather than a mood.
3. Ask the author to pick: yes/no per number, plus any renames or changes. **Stop and wait.**

## 2. Record (when the author answers)

1. Record one `record_ruling` (kind law, source author), in the author's words where given:
   - each accepted item, with its number and any rename the author made
   - a final line listing the rejected ones, so they are never offered again.
2. If the author names or renames something, that name is the ruling. Update any vocabulary or entity that already carries the old name.
3. A coined term that will appear in two or more beats gets a `create_vocabulary` entry.

## 3. Weave (by hand, per the read spec)

1. Pick ride and walk beats that don't already carry texture. Kyle does his thinking on the bike, so rides are the first choice. Put one or two details in a beat, never a list.
2. Check the chosen beat's clock, place and tense against the new detail before writing. If they clash, move the detail to a beat where it fits.
3. Archive before the first prose edit of the session:
   `prose --archive-book --slug <slug> --reason "..." --universe <u>`
4. Write each insertion as a minimal `splice_beats` docket in the book's voice. Dry-run it, then apply it. One docket at a time.
5. Re-read every changed beat as it now stands, and mark it read (`read_beats` numbersCsv, markRead, readBy "claude read N").
6. Run `prose --universe <u> --factory capture --node <slug>`:
   - Resolve new names with `--pin` or `--pin-name "<name>" --entity <id>`, then `--retag`.
   - Watch for a coined phrase that auto-tags to an unrelated entity with the same name, and reword it.
7. Confirm `law_violations` is 0. Re-verify F1 for every entity mentioned in the changed beats (`verify_entity_begin` / `verify_entity_commit`).
8. Report in plain words:
   - what was recorded and where each item landed (beat numbers)
   - anything the author still needs to decide.
