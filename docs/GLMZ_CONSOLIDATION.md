---
codex: 1
project: Prose
layer: methodology
status: in-progress
updated: 2026-10-03
---

# GLMZ consolidation: reading every book into one world

Method for making every GLMZ book stand alone, agree with every other GLMZ book, and add one true
detail to the shared world. It runs as many passes. It adds no tool, ledger, outline or store: the
book is its beats, the world is its entities and edges, and decisions are rulings and orders
([RFC 0015](rfc/0015-novel-factory.md)). Author work order: `01a100da-1de3-71c9-9c92-075a96c14d50`.

## 1. Where each kind of fact lives

| Fact | Lives in |
|---|---|
| What happens in a book | its beats |
| A person, place, corponation, technology, event, document | an entity record (a node) |
| How two things relate (made_by, owns, cites, contradicts, held_by, adjacent_to) | an edge between two entities; beat-scoped validity with `--set-edge-validity` |
| In-world year and ordering | fields and edges on event and era entities; inside a book, time is beat order (`StoryPosition`) |
| A world rule, a banned name, a retcon | a `law` ruling (a regex makes `law_violations` enforce it) |
| A decision, including a deliberate retcon | a ruling (`--ruling add`, `--ruling supersede`) or a work order |
| An intentional mystery | a ruling stating which book may resolve it, or that it is never resolved |

Nothing about the story is stored anywhere else. A doc, wiki page or spreadsheet never outranks the
beats and entity records.

## 2. Truth and sources

- **Truth** is entity fields, dated events and `law` rulings. Two facts that cannot both be true are a bug and get fixed.
- **Sources** are things the fiction treats as evidence: documents, broadcasts, filings, logs, memory dumps, rumors, witnesses. Two sources that disagree are a feature. Link them with `contradicts` and note each speaker's bias.
- A source carries a reliability tag (raw capture, eyewitness, institutional, street/rumor, tampered/forged, synthesized) and custody edges (`held_by`, `could_edit`, `edited_by`).
- A loop of `cites` edges is tagged `accidental` (a bug), `engineered` (someone built it to deceive; keep, record who and why) or `causal` (a feature of the world).
- BCODA's Observation Rule outranks every cross-book document trail: no source Kyle reads may reveal what the entity is.

## 3. Rules that govern every pass

1. **Rule of cool is paramount** where logic and cool conflict, provided the world's own rules hold: consequences stay real and motivations earned. A cool-driven retcon is recorded with `--ruling supersede`.
2. **Each book stands alone.** Cross-references are texture, never plot-load.
3. **One origin per shared fact.** Find where it was first established (`--entity-mentions`). Later mentions are references, not corroboration. A fact held up only by later books citing each other is fragile and gets checked at its root.
4. **No silent back-fit.** A detail may be added to an older book if that book's point of view could know it. The origin beat is not rewritten to match later books without a recorded call.
5. **Canon hierarchy:** novels outrank short stories, which outrank companion material, which outranks the wiki.
6. **Never export, run audio or publish to KDP** unless the author says so, and only for books read as they stand.

## 4. One pass over one book

Follow these steps and watch the factory line (`prose --factory status --node <code>`):

1. Archive the book before editing prose.
2. Read it start to finish, one chapter at a time, in order; mark each beat read as the chapter finishes (`--read-beats`, `markRead`).
3. Per chapter: file anything unclear, contradictory or unmotivated as a read note; check plants and payoffs; verify tagged entities against the prose and repair the record where it is wrong; check the continuity graph.
4. Fix by hand with exact-text splices, dry-run first, one docket at a time. No bulk find-and-replace.
5. Re-read the changed beats. Confirm every beat is read and `law_violations` is 0.
6. Record each decision as a ruling or order.

## 5. Back-propagation rounds

After a book finishes a pass, every earlier book is checked against what it added.

1. For each entity or ruling the new book created or changed, compare its mentions in older books (`--entity-mentions`, `--entity-relationships`, `--entity-tree`) to the fact's origin.
2. Where an older book contradicts the origin, or can carry a true detail from its own point of view, file a read note and splice by hand.
3. Re-read only the changed beats.

A round is dry when, across every included book: no continuity contradictions; no read note without a ruling; no plant without a payoff or a mystery ruling; `law_violations` is 0; every shared entity agrees with its mentions; no loop is tagged `accidental`; and the last full round filed no new note. Run rounds until one is dry.

## 6. Order and scope

- Included: BCODA, DWIACE, VATD, Iron & Silk, Magenta & Gunmetal, Crimson & Chrome, Neon & Rust, Pixel, Mnemosync, The Long Cut, Testament, Sparrow, Attendance, Ballast, Read the Room, Steppin' Razor, Underclan, It Came From Iowa, The Way Up, The Way Down, Critical Mass, Between the Lines.
- Under 50 pages (Critical Mass, Pixel, Between the Lines, The Way Up, It Came From Iowa, The Way Down): expanded organically to novellas in their own pass.
- Parked: Eyes on the Light. Standing Contract feeds the BCODA sequel and is not on this line.
- Reading order is world-chronological, read from the graph. BCODA is the anchor (present day 2226).

## 7. Operating the line (what each step costs)

Commands that do each step, all through the Hub, all with `--universe glmz`:

| Step | Command |
|---|---|
| Read a range and mark it read | `prose --read-beats --slug <s> --from N --to M --mark-read --read-by "<who>"` |
| File, list, resolve a read note | `prose --read-note add\|list\|resolve --node <s> --beat <#id> --kind defect\|question\|note --text "…"` (`--beat` takes the `#` id, not the position) |
| Archive before editing prose | `prose --archive-book --slug <s> --reason "…"` |
| Hand splice | `prose --splice-beats --node <s> --file docket.json` (dry-run), then `--apply`. A docket row is `{beat, old, new, count}` with exact stored text; keep `old` clear of `<entity …>` tags |
| Move or delete a beat | `prose --beat insert\|delete --node <chapter node id> …` (the chapter node, not the book) |
| Entity gaps in a unit | `prose --factory capture --node <s>` (unresolved names, untagged mentions); `--retag` tags them; `--retag-name "<surface>" --from <id> [--to <id>]` takes a tag off |
| A word that is not an entity | `prose --ruling add --kind incidental --pattern "<surface>" --node <s> --text "…"` |
| Merge duplicates | `prose --merge-entity --winner <guid> --loser <guid>` (restorable with `--restore-entity`) |
| New entities | `--add-corponation`, `--add-faction`, `--add-place` (JSON file), `--create-vocabulary`; `--add-alias` works for characters, places and factions, not corporations or vocabulary |
| Fix an entity record | `prose --set-character-fields \| --set-entity-fields --id <guid> --file patch.json --confirm-unread` (send only the changed fields) |
| Verify an entity | `prose --verify-entity begin --entity <guid> --node <s>`, then `commit --nonce <nonce>` |

Costs and traps found in use:

- **An entity record edit un-reads every beat that mentions the entity, in every book.** The command refuses and lists the beats until `--confirm-unread` is passed. Editing Stash un-read 107 beats: 85 in The Long Cut and 22 in Bushido Coda. Batch record edits, re-read the listed beats in each book, then verify the entity in each book.
- **A new alias or entity can turn another book's plain text into an untagged known name** and put that book's F4 red. After creating an entity or alias, run `--factory capture` on every book that shares the name. Adding the alias "Gray Zone" did this to Bushido Coda.
- **Retagging a verified book un-verifies every entity in the retagged beats.** Undo with `--restore-beat-text --id <beat> --as-of <utc>` (dry-run first) after removing the alias that caused it. This restored Bushido Coda to its pressed state.
- A scanner name that two entities share is unresolved, not tagged. Merge the duplicate or add a distinguishing alias.
- An incidental ruling is scoped to its book. Use it for a surface that means something else in that book.
- Aliases cannot be removed by `--set-character-fields`; use `prose --delete-alias --value "<alias>" --type character` (dry-run first; it lists every owner).

## 8. Checks

- `prose --progress` (page and score table, every universe)
- `prose --universe glmz --factory status --node <code>` and `prose --factory next`
- `prose --universe glmz --graph-health` (orphans, weak links, malformed entity names)
- `prose --universe glmz --ruling list --node <slug>`
- `prose --universe glmz --entity-mentions --entity <id|slug>`
- `prose --world-state --beat <beatId>`

MCP writes can return `ok:true` while the server runs a stale schema: read every write back, or use the CLI.
