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
- **Read each record before committing its verification.** Verifying in a loop commits whatever is tagged, including wrong-referent tags (a facility tagged to a character, a common word tagged to a crew member). List each tagged entity with its type and a line of its record, check it against its mention, fix the tag (`--retag-name` with an incidental ruling, or a new entity) and only then verify. `--retag-name` matches the surface case-sensitively.
- A scanner name that two entities share is unresolved, not tagged. Merge the duplicate or add a distinguishing alias.
- An incidental ruling is scoped to its book. Use it for a surface that means something else in that book.
- **`--beat insert|update --text "..."` loses text.** The argument drops every double quote and cuts the text at the first blank line. Send the beat on stdin instead, one paragraph per beat: `$text | prose --beat update --id <beatId> --node <chapter> --text -` (quotes and Unicode survive). Read the stored beat back after inserting.
- New chapter nodes land at the end of the book. Reorder with `prose --reparent-node --slug <chapter> --sort-key <n>` (a negative key goes first). Create one with `prose --create-book --kind chapter --parent <book> --title "…"`; `--beat insert --node` takes the chapter slug.
- To delete a chapter node use `prose --delete-node --id <guid>`; the guid shows in `prose --entity-mentions` output as `<chapterId>.<beatId>`. `prose --beat delete --id <beatId> --node <chapter slug>` removes one beat.
- Long background jobs can be stopped by the host when memory is short. Run bulk beat writes in foreground chunks of about 70 beats.
- **Prior drafts live in `engine/data/exports/`.** A book node can be rewritten while an earlier draft still holds canon the new one depends on (Critical Mass replaced Double Entry; the new text's "That's twice now" only works if the old story happened first). Check older exports before expanding a short book.
- **A book can hold two drafts at once.** The Way Down carried four long old-draft beats (one per chapter) beside the newer short beats; they told a different story (the extraction twice, a fire alarm, a different gun). When a chapter's first beat is far longer than its neighbours and contradicts them, archive the book, then delete the old beat (`--beat delete --id <guid>`) and bridge the gap with a new beat.
- **A first name can be shared by two entities, and the scanner then tags neither.** "Reza" belonged to Reza Solano and to an unrelated sociologist (Reza Tehrani); the book showed 19 unresolved "Reza". Fix: `--delete-alias --value "Reza" --type character --apply` (it removes every owner), `--add-alias` back onto the right one, then `--tag-entities --slug <book>`.
- **`--add-alias` works on weapons and places too** (Ankle-Biter, Spire, Bloom Quarter). Aliases can leave another book with an untagged mention (BCODA #20915 after "Bloom Quarter"): run `--factory capture --node <book> --retag` on every book that shares the name, then re-verify the entities in the beats it re-saved.
- **Wrong-referent tags are found by listing every tag in the book** (entity, type, surface). In The Way Down: "Clean Hands" (a show) tagged to the Hands motif, "Ghost" to an AI, "Package" to a vocabulary term, "The Lake" to a motif. Use an incidental ruling plus `--factory capture --node <book> --retag-name "<surface>" --from <entity id>`.
- **A record can carry a different name than the prose** (Khalid Farrukh for "Noor", Ildiko Varga for "Makena", the bar "Velvet Tine" as both a character and a place). Rename with a `{"name": "…"}` patch, clear the colliding alias owners with `--delete-alias … --apply`, re-add the alias to the right one, and merge duplicates (`--merge-entity --winner … --loser …` also works across entity types and relinks the beat tags). Check the other owner's mentions first (`--entity-mentions`).
- **The scanner skips two-letter surfaces.** "CJ" stays untagged even with the alias; add the surname alias so the character is tagged somewhere and verifiable.
- **A seeded record may be a different person who happens to share the surname or first name.** In Underclan "Lark" resolved to Lark Mbekele, a Hamtramck tattoo artist with a full record. Read the whole record (`get_character`) before renaming or overwriting. When it is a different person, `--delete-alias --value "Lark" --type character --apply`, create a new character, `--factory capture --retag-name "Lark" --from <old id>`, then `--tag-entities`. "Noor" was claimed by three records (Glim's mother, a logistics auditor, a blood-bank operator); the alias went to the one the book being read needs, and books already tagged keep their tags.
- **A new alias can tag the wrong thing in another book without turning any gate red.** The alias "Cooperative" on The Science Cooperative tagged BCODA's "Iowa Cooperative" to it, and capture stayed clean. After adding an alias or entity, run `--entity-mentions --entity <id>` and read which books it lands in. A common-word alias ("Gray Zone") can also leave a pressed book with untagged mentions: `--factory capture --node bushido-coda`, `--tag-entities`, re-read the re-saved beats, and re-verify the entities (the factory's next action lists the first unverified one).
- **Duplicate records for one person, and one person written two ways, are the usual Rook-series defects.** Lace/Blessing Agwu, Vox/Tem Okafor and Rook/Inkeri Saarinen were each two records: merge into the richer one after copying anything unique (image prompts, hooks). Stave is they/them in the record and in Neon & Rust, and Crimson & Chrome wrote Stave as he/him: pronouns are a hand splice across every beat, not a record edit. Check every secondary character's pronouns in the record against a dump grep for `he|his|him` near the name.
- **A pressed book can go red from a new alias through a part of a place name.** Adding "Park" for Park Gi-su turned "Leavitt Carousel Park" in BCODA into an unresolved "Carousel" and an untagged Syndicate. After every alias, run `--factory capture` on BCODA first; fix with a book-scoped incidental ruling for the stray words and `--tag-entities`, then clear the factory's next action (`--factory next` names the first unverified entity) in foreground loops.
- **Shared first names are resolved by renaming the record to the prose, not by deleting aliases.** In Iron & Silk the records were "Devraj" and "Uchenna" for the characters the prose calls Priya and Adaeze. Rename with `{"name": …}`, and when the bare first name is shared by other books' characters (Priya has five owners) leave it unresolved with a book-scoped incidental ruling rather than deleting their aliases; the full name tags.
- **A seeded record's alias can tag the wrong character in another book.** Testament's records were Hideo (prose: Hana), Nkiruka (Adaeze) and Detlev (Priya) Morimoto/Chukwura/Achterberg; their bare-first-name aliases tagged Priya and Adaeze in Iron & Silk. Rename the record and set `aliases` in the same `--set-character-fields` patch (never `--delete-alias`, which removes every owner), then check the other book's tags with `--entity-mentions` before trusting its verification.
- **Corponations take no aliases** (`--add-alias` has no table for them). A bare family name such as "Halcyon" gets a book-scoped incidental ruling; the parent/division link is an edge (`subsidiary_of`), not a name.
- **`--ruling supersede` drops the book scope** of the ruling it replaces: re-add the node-scoped ruling afterwards, and expect the capture to go red until you do.
- **Two entities for one corporation are a consolidation decision, not a bug.** Halcyon Combine (Steppin' Razor) and Halcyon Civil Security (Testament, The Long Cut) are linked `subsidiary_of`, so bare "Halcyon" is one corporation seen through different divisions. The ownership stays out of shared fields.
- **PowerShell: never name a helper `R`** (it is the Invoke-History alias); `Row` works.
- **Expand a short book with scenes that pay off what the other books already assume.** The Way Down names Bear, a Pilsen basement, a mop-handle drill, a seventeen-node contractor, a no-log channel and Ozzie's fifteen percent; The Way Up never showed any of it. The expansion wrote those scenes (Pilsen drill, Ozzie's cut, Scraps's confirmation, Ozzie's mop handle) and merged the Pilsen fixer Bear into Boris Johansen of Testament (`--merge-entity` has no dry run; it relinks beat tags and is restorable). Insert each beat with `$text | prose --beat insert --after <guid> --node <chapter slug> --text -`, then `--tag-entities`; a bare name that the scanner leaves unresolved can be tagged by `--beat update` with an explicit `<entity>` tag.
- **Common-noun aliases are cheap to add and cheap to check.** Aliases "Lamplighters", "Mission", "Oarsman", "Tartar" were added for existing entities; run `--factory capture` on every other book afterwards (all stayed clean). Tribe vocabulary and minor names (Brave, Skin, Fare, Slip, Marl, Tallow) get book-scoped incidental rulings instead of entities.
- **A field patch replaces the whole field and `--entity-history` truncates values to 120 characters.** Dump the whole record first (`--verify-entity begin`, `--get-place --print-raw`) and keep strand-sensitive reveals out of shared fields.
- **A record can break the no-cash law and block verification** ("verify refused: record breaks 1 law(s): economy 'Cash'"). Fix the record field (`--set-entity-fields`) and re-read the beats it un-read.
- **Incidental rulings are the right answer for a corporation shorthand** ("Halcyon", "Cordon", "Meridian"): corporations and vocabulary cannot take aliases, and the scanner will not guess.
- **Dates and ages that cannot both be true go to the author as a read note** (`--read-note add --kind question`), not into a silent edit: Sasha Võ is nineteen in Steppin' Razor and twenty-eight in her record.
- Aliases cannot be removed by `--set-character-fields`; use `prose --delete-alias --value "<alias>" --type character` (dry-run first; it lists every owner).

## 8. Checks

- `prose --progress` (page and score table, every universe)
- `prose --universe glmz --factory status --node <code>` and `prose --factory next`
- `prose --universe glmz --graph-health` (orphans, weak links, malformed entity names)
- `prose --universe glmz --ruling list --node <slug>`
- `prose --universe glmz --entity-mentions --entity <id|slug>`
- `prose --world-state --beat <beatId>`

MCP writes can return `ok:true` while the server runs a stale schema: read every write back, or use the CLI.

## Recipes the passes reuse (PowerShell, run from the repo root; nothing here is a tool, each line is an existing `prose` command)

**Start a book pass** (archive first, then the state a reader needs):

    prose --universe glmz --archive-book --slug <slug> --reason "<CODE> pass before edits"
    prose --universe glmz --factory capture --node <CODE>        # unresolved / untagged names
    prose --universe glmz --ruling violations --node <slug>      # law hits
    prose --universe glmz --read-beats --slug <slug> --from 1 --to 3000 > dump.txt   # strip <entity> tags to read

**Re-read and re-verify a whole book** after any record edit or retag (verification is per entity, so loop over the guids the beats carry; do it in the foreground in chunks under two minutes):

    prose --read-beats --slug <slug> --from 1 --to 3000 --mark-read --read-by claude
    # for each guid="..." in the dump:
    prose --verify-entity begin --entity <guid> --node <slug>     # read the record against its mentions
    prose --verify-entity commit --nonce <nonce from begin>

**Add a scene** (one beat per block of paragraphs; the beat is auto-tagged on insert):

    $text | prose --beat insert --after <previous beat guid> --node <chapter slug> --title "<optional>" --text -
    prose --tag-entities --slug <slug>; prose --factory capture --node <slug>
    prose --beat show --id <new guid>                             # read it back

**Fix a sentence by hand**: a splice docket of `{beat:#, old, new, count}` rows, dry-run first, then `--apply`, one chapter node per docket.

**Close a book pass**: `--factory capture` clean, `--ruling violations` 0, every beat re-marked read, every tagged entity verified, then `--factory status --node <CODE>` shows no failing cell before F7.

## Standing decisions recorded in the Hub (2026-10-03, rule of cool, all as rulings; read them with `prose --ruling list --node <slug>`)

- **Rook-series chronology** (01a10250): Magenta & Gunmetal Nov 2225 (Critical Mass Dec 2225), Neon & Rust 2227, Crimson & Chrome 2229, Iron & Silk early 2230. Iron & Silk's Headcount data covers 2211-2226; Phase 0 began 2229.
- **Soraya's partition** (01a10251, Crimson & Chrome): one partition sealed mid-2225, cracked in Neon & Rust, count held two years.
- **Devices** (01a10251, Bushido Coda): handheld screens are right for the unmeshed, for anyone who must stay off a traceable mesh, and for institutional or professional equipment; meshed characters use AR windows.
- **Meridian charters** (01a10245): the 2208 Identity Charter and the 2219 Charter are two documents.
- **Names:** only Dr. Nadia Park keeps Nadia (the Attendance courier is Nilsa); Bear the Pilsen fixer is Boris Johansen of Testament; Halcyon Civil Security is held through Halcyon Combine.
- **Steppin' Razor** is 2217 (Sasha at nineteen on her first break from Sigma).

## Round 2 world laws (2026-10-03, author's words, recorded as `law` rulings on bushido-coda; the world sweep reads each book against them)

- **The two worlds** (01a10319-b50b): CorpoNation arcologies hold the wealthy like kept pets, ignorant of their imprisonment; the Seams (the Gray Zones) hold the real people, abandoned, ignored, run by gangs. A wealthy character who knows is a ruled exception with a reason on the page.
- **Arcturus is the only police** (01a10319-e032): ArcSec is the one fascist police faction; the city police dissolved in 2208. Corporate security guards its own property and never polices the Seams.
- **Geography and the Blur** (01a10319-e91c): Chicago is the GLMZ, the center of commerce. The Spine runs Chicago to Green Bay, around Lake Superior, through Michigan and Detroit, on to Buffalo. No New York, no California, no contact with Florida: those are the Blur, seen from a Pulse tube to Denver or Seattle. Records: The Spine (extended), The Blur (new place).
- **The weather** (01a10319-f203): reality anomalies so old that people call them weather. Never a crisis on the page.
- **The dark truth** (01a10319-faf8, truth layer): the Super Minds broke reality trying to reach higher dimensions. Never stated by any source; only symptoms. The Observation Rule outranks it. Record: The Super Minds (faction, tagged hidden).
- **The Atmospheric Processors** (01a1031c-5241, supersedes 01a1031b-fef4): vague, unexplained weather infrastructure; perfect days for the CorpoNations, hot misting runoff that collects in and flows through the Seams, iridescent in every puddle (thin-film interference). Record: vocabulary entry "The Atmospheric Processors" (full term in prose; vocabulary entries take no aliases).
- **The pillars** (01a1031d-a72b): Atmospheric Processors, nanites, cyberware, synthetic intelligence, quanta, street wars, tiered civilian life. Each book shows what its POV meets; none contradicts one.
- **E.L.F.s** (01a1031e-9bdc): Emergent Life Forms, assembling from the wreckage of destroyed Super Minds; common knowledge that they exist, origin never stated. The vocabulary entry E.L.F. carries the new expansion; older documents keep the street's "electronic life form" gloss.

Sweep recipe used for the geography and police audit: loop `dev_scan`-style over every book's `--read-beats` dump with one case-insensitive regex (place names, police words) and read every hit by hand; the hit list is a to-do for the pass, not a stored artifact.

### Round 2 additions (2026-10-03)

- **Hyperreality** (01a1031f-dbd9, builds on 01a0da85-0af4): augmented reality masks the dingy streets; the lie only works if people want to believe it. The arcology wage slaves buy it; the Seams do not.
- **No police, no justice, ArcSec keeps people down** (01a10320-f122, supersedes 01a10319-e032 and 01a10320-721f): the Seams have protection rackets and mob rule, no courts. Any hearing is a corporate or military proceeding (review boards, charter desks, court-martial), never a public court. Round-2 splices replaced court, judge, magistrate, tribunal, felony, lawsuit, deposition and lawyer wording across IxS, TLC, VATD, NxR, DWIACE, ATTE, BLST, UNDR, SPRW and Testament; ArcSec and corporate security stay as written.
- **Thread is a pod line**, not a space elevator (TWU, RTR, SRZR fixed). **GLMZ** is the Great Lakes Metropolitan Zone (Mnemosync fixed). Empire State Building reference removed from I Cannot Find the Ignorance.
- **Method note:** a dump can stop short of the book. Check `grep -c "^--- \["` against the beat count before marking read; round-1 inserts through stdin can carry "??" for non-ASCII, so grep every dump for it.
- **Arcturus polices the physical, Halcyon the digital** (01a1034b-1fdd, author): Halcyon never runs street raids or armed ops; armed bodies wear Arcturus or a hired crew, Halcyon supplies license, paper, hold, review board. Testament's Cortland engagement, watchers, Caryatid operators and Priya Achterberg are now Arcturus Civil Security on Halcyon licenses; Steppin' Razor's recovery teams are hired crews on Halcyon paper. Sweep found no other Halcyon force in the 22 books (TLC's NCID and security bureau are data/records, left as is).

### Round 3: texture and cross-pollination (2026-10-03, author order 01a1035b-0db7-70d4-8376-130dc620478c)

- **What it is:** a light third pass over the books so each carries the world's texture and one true detail from its siblings, never a new fact. No new rulings. Every book archived first, edits are hand splices (dry-run, apply, tag, capture, re-read the changed beat).
- **Shared details used** (all already canon in BCODA or the glossary): the genetic strays (turret cats, lumen mice, koi-colored pigeons, Null Crows, as omen and watcher), the Atmospheric Processors' warm runoff with its rainbow skin in gutters and drains, the Arcturus guard who holds a gate or a stair line, the second AR street that people choose to walk (hyperreality).
- **Done:** SRZR, SPRW, RTR, ATTE, BLST, UNDR, MNEMO, TEST, TLC, IxS, MxG, CRIT, NxR, CxC, PXL, TWU, TWD, BTL, VATD, DWIACE. SRZR through TEST were read in full this round. TLC, IxS and the rest were checked with a marker count (Processors, rainbow, strays, AR, Arcturus) and edited where the scene had room, because their full re-reads were done in round 2. ICFI (Iowa, outside the Chicago weather) and BCODA (the source of these details) were left as written.
- **Method trap found:** `--tag-entities` after a splice can re-save a beat other than the one edited and un-read it (F5). Run `--factory status` afterwards and re-mark any beat that went red.
- **Open:** the F1 re-verification is still red on every book after the round-2 record edits. Verify each record against its mention beats before committing; a loop that commits blindly proves nothing.
- **Glossary conflicts noticed, not fixed:** The Spine glossary entry ("western lakeshore corridor") against the geography ruling, and the Halcyon/NCID "neuretics-crime enforcement" wording against ruling 01a1034b. (Fixed in round 4, below.)

### Round 4: canon decisions and propagation (2026-10-03, author order 01a1038e-ee09-7bb2-aa8e-28af02a307e2)

- **What it is:** the author asked for a pass that fixes inconsistencies, decides what makes most sense, makes that canon, propagates it across all 22 books, then does it again.
- **Pass 1 (decide and propagate):** BCODA-scoped laws were not governing the other books, so the first finding was that 0 law hits elsewhere meant nothing. Cross-book conflicts were decided as rulings and glossary entries (First names unique per universe 01a1039a, spelling 01a1039b, devices 01a1039c, plus the 35th-Halsted timeline, AAMA vs RMA, Ghana/NGRA, the Arrangement, the Spine aeroplex, Meridian, NCID/ARC, grown neuretics and the umbrella corporations Axiom, Arcturus, Halcyon, Orison, Sable). Two rulings carried errors and were superseded with corrected text. Glossary upserts: Spine, NCID, Meridian Compact, ArcSec, Halcyon, Low, Arrangement, Atmospheric Processors. 14 characters were renamed so no two share a first name; spelling and device sweeps and light SPRW and MNEMO prose fixes followed; the Sahar Farrukh and Bloom Quarter records were patched.
- **Pass 2 (do it again):** a fresh dump of every book, a first-name duplicate scan and the same ruling checks. It found one defect: ATTE tagged the boy Joaquín Reyes twice, once to a stale record, so he looked like two characters. Retagged to the one record with `--factory capture --retag-name`. Every book then reported all beats read as they stand, 0 law hits and clean capture.
- **F1 re-verification, done record by record (all 22 books green):** each record was read against its mention beats before it was committed. Records that disagreed with the books were fixed first. Examples: Continuity Office was a "government bureau" (now administrative, chartered by its corporate backers); the Arcturus record said it does not police the neuretic terrain (now the Arcturus-physical, Halcyon-digital line of 01a1034b); Pulse's Transit Authority was "the only law" (now staff with no badge, ArcSec enforces); Bloom Quarter "Null Enforcement" raids became Arcturus Civil Security sweeps; Corvin carried "synthetic" tags though he is human; Tadesse's years (2204 and 22) became 2202 and 24 to match the prose (one splice, SPRW V56 archived); Selvamani's paper year 2024 became 2224; Feliksas, Ilse, Chukwura, Mireille, Odette, Douglas, Mission and Halloran records lost stale names and cross-book debris; Sorrel lost "spine" and "wound ledger" wording; BTL's Cordero Desk leader (Joaquin and Mateo, both colliding with other first names) is now Octavio Cordero; Idris and Checkpoint 14 records now spell Gray.
- **Cost trap:** any record edit un-reads every beat that mentions the entity (Arcturus 133, Pulse 86), so the beats were skimmed against the new text and re-marked. A patch with a wrong shape (story_hooks must be an array) is refused with `"Ok": false`; read the output before committing a verification.
- **Remedios MedTech (author, 2026-10-04):** the corponation records "Sable Industries" and "Sable Industries MedTech" were merged and renamed Remedios MedTech; 26 occurrences in The Long Cut became Remedios; ruling 01a1049d.
- **Altered animals (author, 2026-10-04):** dogs, cats, rats and pigeons are genetically or cybernetically altered to serve purposes. "Turret cats" became Sentinel Strays (cyberware stuck into strays, loosed so the fitter gets a video feed; lookouts no one can catch) and "Null Crows" became Silver Pigeons (pigeons with nanites nesting and reproducing in their feathers, an ad hoc nanite transport, emergent life). 5 beats in 4 books and 11 beats in 8 books were spliced (each archived first), one Testament sentence was reworded so "the breed" became "the fitted ones", and vocabulary entries and ruling 01a104e9 were added. Koi-colored pigeons and lumen mice were left as they were.
- **Left for later (round 4):** the seeded "Sable Industries" character record, and the Gray Zone and GLMZ place records, which still carry older-generation text (a lake structure with a Council, "the Spine" as a name for the whole sprawl) that no book contradicts. "Null Crow" mentions tag the Null synthetic record (the scanner re-derives it). No export, no audio.

### Round 5: specific cross-pollination (2026-10-04, author order 01a108f4-f614-7631-8d0f-b5d27295d198)

- **What it is:** round 3 shared generic texture. This round carries *specific* things between books: a bar, a show, a product, a crew, a public incident or a street legend from one book appears in another, the way that book's narrator would meet it (gossip, a feed, a flyer, a product in hand, a sight on the skyline). Texture only, chronology-safe, no secrets spoiled, no new named people.
- **Method (no new tools):** every book was dumped and read in full by parallel readers; each wrote a working card in the session scratchpad (year, POV, public "exportables", openings with verbatim anchors). The cards were matched into one assignment list by date and point of view; drafters wrote one or two sentences per row in the target book's voice; every row was reviewed by hand, dry-run, then applied per book (archive first, splice, tag, capture, re-read). 87 splices across all 22 books, 2 to 5 per book.
- **Threads now running through several books:** Candelaria's 2026 touchdown (BCODA, ICFI, TWD, SRZR sight, SPRW thermal map); the 2223 runaway aerobloc nobody would own (BLST gossip, MNEMO's dropped story); the Z5 freight-rail bridge collapse and the bent Lake Platform crane (DWIACE feed, IXS, NXR, SPRW, VATD); Axiom's free nerve-regen formula (TLC, MNEMO); the Cermak Reclamation Crew arrests (TLC, ATTE); Yuki Osei-Reyes and the corridor disappearances (DWIACE board, BCODA and BTL Seam radio); the Seam radio; Clean Hands, Meridian Unsolved, Below the Threshold, The Children Beneath GLMZ and Lacuna on feeds; Fenris and Underbelly on playlists and flyers; Candor and Fade (VATD, TWU); CramIt, Koji Press, Liang blend, Kinetix, Thornfield, Thornback; Davorka's and Antiquity & Stationery (CRIT); Dinh Salvage & Sundry (ATTE, ICFI); the NREN Relay (TLC, CXC, SPRW, MNEMO); Cinderfall Logistics (PXL, SRZR, and TEST, where Bear's Ironbend dock is now named as Cinderfall's own rusting terminal, matching the record's original intent and CRIT); the samurai who doesn't stop as a bar story (NXR); THE SKY IS COUNTING graffiti in Milwaukee (RTR); Lamplighters slumming (TWU, UNDR).
- **Graph:** new entities Davorka's (place) and The NREN Relay (faction). Lacuna's record was rewritten to the prose (an old neural-feed courier serial whose theme still plays; not a band, not a film), ruling 01a10918-10ea.
- **Traps found:** the scanner splits hyphenated surnames ("Osei-Reyes" tagged two other characters in DWIACE; fixed with book-scoped incidental rulings and `--retag-name`). Multi-word titles drag single-word entities: "Clean Hands" (Hands motif), "The Children Beneath GLMZ" (Children motif), "Below the Threshold" (place Threshold, also pre-existing in SRZR), "Tessaline Damen Authentic" (street Damen), "Hokkien Instant Noodle" (a character Noodle), "drifting" and "cold storage" (vocabulary). Each got a book-scoped incidental ruling plus `--retag-name`. "Carrion" can resolve to a synthetic; `--retag-name "Carrion" --from <synthetic> --to <Carrion Logistics>` holds. "Dinh" resolved to Chinwendu Dinh, not Vo Dinh; moved the same way. After any splice batch, list the tags whose surface sits inside the new text and check each referent.
- **F1 after a cross-pollination pass:** a new sentence changes the mention fingerprint of every entity tagged in that beat, so F1 goes red on every unit of every touched book. Review each record the new text mentions, then re-verify; a record changed by someone else during the session goes to review instead of being committed.
- **Law sweep that followed (author: "yes, sweep those next"):** every device, legal, police, cash and geography hit in the 22 books (601 sentences) was judged against rulings 01a0da89, 01a10251 and 01a10320-f122. Allowed and kept: institutional and professional kit (checkpoint and dispatch terminals, clinic and studio monitors, vehicle displays), unmeshed users, off-mesh need (Stash's Austin dead-zone handsets, fugitives), relics, military proceedings (Testament's court-martial, counsel and judge advocate), corporate counsel inside corporate meetings, ArcSec officers and detectives, Rotterdam (BCODA's Pulse freight lane under the Atlantic), Sparrow's transit officer (on the Tunis platform), and Steppin' Razor's recovery team (already a hired crew on Halcyon paper). Fixed by hand splice, each book archived first: meshed people's convenience screens, displays, terminals, monitors and keyboards became overlays, windows, connect points and AR keys (TWD, SPRW, MXG, CRIT, TEST, IXS, MNEMO, PXL, BLST, TLC); cash became credsticks (IXS, SRZR, TLC); lawyers and courts became charter advocates, debt boards, review boards and compliance offices (BCODA, DWIACE, CXC, TLC). Iron & Silk's Z5/Z6 Legal Cooperative is now the Z5/Z6 Advocacy Cooperative (record renamed with its aliases; its attorneys are advocates), and its Root has run the institution forty years everywhere. The Undertow place record (a South Haven flood district) was renamed South Haven, and a new place, The Undertow, is the Seams Pulse Rock club (edge: Underbelly performs_at The Undertow); the scanner does not tag the common word "Undertow". The Meridian Charter's 2208 (identity) and 2211 (heritage field retired) dates are two acts, not a conflict. Open: a read note on The Long Cut #10568 (the departure beat sits before the tunnel confession; author's call).
### Round 6: the street sensorium (2026-10-04, author order 01a1099c-4022-7d61-baf4-4a3c1b1803aa)

- **What it is:** the author asked for a read of all 22 books, cross-pollinated "for authenticity", adding sights, smells, tastes and the feel of things: the arcologies on their Struts like a giant tortoise's legs carrying the better world on its back, the gray slivers of sky most of the year, nanite swarms eating whatever they are allowed to, lumen mice and the other genetic strays, the ramshackle open-air illegal market, cold Arcturus patrols that look away from the Lotus Syndicate, drugs in the open, bodies slumped against walls in DataEast Simspace, hyperreality leaves on dead trees and taped-off alleys with scrolling marquees, schisms at the fringes like predators waiting for a straggler, everyone strapped and chromed with a face that says "try me" and means "please don't hurt me", corporate tourists with armed escorts, VTOL streams among the Aeroblocs. Tone: grim, gritty, false, fierce, fear. Recorded as the universe-wide law 01a109a0 (THE STREET SENSORIUM).
- **Graph:** new records DataEast Simspace (technology, alias Simspace, made_by DataEast), lumen mice (vocabulary, genetic strays) and Schism Advisory (vocabulary, the public anomaly bulletin). The Silver Pigeons record lost its retired name and its physical sign, which had blocked verification. Universe-wide incidental rulings for the plurals Struts (01a109af-4c36), The Struts (01a109b9-d8e6) and Aeroblocs (01a109af-557d); book-scoped ones for TWU's Eighteenth and UNDR's Cogs.
- **Method:** seven parallel readers each read their books in full and drafted dockets in the book's voice against a brief and a validator (law words, exact-once anchors outside entity markup); every row was reviewed by hand, dry-run, then applied per book (archive, splice, tag, capture, re-mark the changed beats). 292 sensory splices in all, 7 to 20 per book, every book archived first.
- **Trap found: the template effect.** Drafted in parallel, the same images arrived in the same words in book after book ("try me ... please don't" in 21 books, "cold blue-green" in 21, "insects between hives" in 18, "eyes moving under half-lids" in 19). Six books had already been applied with the author's own phrasing (SRZR, SPRW, VATD, ICFI, CXC, PXL); for the other 16, a second pass banned the stock phrasing, cut the weakest rows (339 drafted, 292 kept) and re-rendered each image through the POV's own eye: Ledger's auditor ("the gun was the advertisement, and not reaching for it was the actual offer"), Stash's surgeon (VTOL light "like contrast moving through a vessel on an angiogram"), Glim's underground eye in UNDR (holographic leaves as "small green shapes of light" on "a dead thing like a pipe"), Teo's mass-and-numbers eye in BLST, Rook's cost-and-load eye in NXR. Run a phrase count across all dockets before applying any of them.
- **Also fixed:** MXG's "archology" spelling (now arcology), and one CRIT "ferrocrete" in new text (ferrocement under that book's spelling law).
- **Continuity fixes (author: "fix"), each book archived first, 11 splices:** SPRW #4110 "six years apart" became eight (2218 to 2226); CXC #6449 Stave "himself" became "themself"; MXG introduces Soraya's field alias Ohara at its first use (#7006) and #6979 no longer re-explains it; MNEMO #8103's stray leading period became an ellipsis; TEST #9890 no longer has Brandt take the hand before #9891 does; UNDR #4783 quote closed; BTL #16963 back in present tense; CRIT #15553 opening quote restored and #15608's garbled sentence rewritten ("A mind that has had someone else's blood spattered across its knee doesn't go back to being merely code."); TLC #10218 garbled sentence rewritten. Checked and already fine: MXG's bridge distances and IXS's "thirty years" (fixed in round 5). Most device and legal words the readers also reported were already judged and kept in the round 5 law sweep. No export, no audio.

### Round 7: the Gauss-Seidel wheel, cycle 1 (2026-10-05, author order 01a10a85-4389-7680-851d-a0fb326de8a7)

- **What it is:** the author asked for a round table that keeps every book in one world: "center on one book, and then draw all texture from every other book to update it, then once that's done you make another book the focus", cycling until no meaningful improvement is left (at most ten cycles), so the inhabitants occupy the world together and no book drifts off on its own. After cycle 1 the author asked to stop and codify the process. The reusable prompt is `GLMZ_living_universe_cycle_prompt.md` in the author's Downloads. Orders 01a10a88-* (rounds 8 to 12: reverse, random, Fibonacci, graph and inverse seat orders) stay open as alternatives.
- **Method:** strict sequence in catalog order (ATTE, BLST, BTL, BCODA, CXC, CRIT, DWIACE, IXS, ICFI, MXG, MNEMO, NXR, PXL, RTR, SPRW, SRZR, TEST, TLC, TWD, TWU, UNDR, VATD). One reader read the focus book in full against the current text of all 21 others and drafted only meaningful rows: cold-reader gaps, consequences, behavior, veracity, belonging (texture drawn from another book, named), the pull, assembly. Every row was reviewed by hand against the quoted beats, then each book was archived, dry-run, spliced, tagged, captured, its changed beats re-read, its dump refreshed for the next focus, and F1 run on the changed beats' entities. A cross-book phrase check blocked any four-word phrase already added to another book.
- **Result:** 337 splices in 22 books (346 rows drafted; about 30 rejected or rewritten in review), every book 0 law hits and all beats read as they stand. Ten "ferrocrete" spellings became ferrocement (BCODA 5, BLST 2, CXC, DWIACE, VATD); DWIACE's ten "licence" became "license" (law 01a1039b). Representative fixes: ATTE's Rogers Park distance and the 35th-and-Halsted playground's age; BLST's vote scheduled before it existed, and Sigrun's survey seam (her "no survey" now means none of the co-op's own); BTL's ARC spelled out as the Aerostatic Regulatory Command, its street patrol made Arcturus; BCODA's last police and lawyer words; CXC's crew gap made two years (NXR has them together in 2227); MXG's bridge north at two hundred meters throughout and its chase by daylight; MNEMO's eight-month log; IXS's Spine depth (the safe house is nearly five hundred meters below the 480-meter aeroplex); SPRW, RTR, PXL and UNDR's stray hyphens made each book's own em dash.
- **Belonging drawn in (each from a named source book):** ICFI's farm walker as corridor gossip (BTL, BCODA, CRIT); BTL's viral essay (DWIACE, MNEMO); BTL's Phantom Load (CXC, SPRW); BTL's NO UNITS AVAILABLE (RTR); BTL's green diagnostic pulse (UNDR, VATD); UNDR's Mission, Lamplighters and the Cogs' covenant and knock (TLC, DWIACE, SPRW, MNEMO); SRZR's shamisen busker (RTR); SPRW's "the number" (PXL); VATD's blood rig (TEST). The least-connected books by shared entity tags (BTL, UNDR, ICFI, BLST) gained graph edges; their leads had none.
- **Graph and records:** edges 3113-3147 (Idris Kovac, Glim, Noor, CJ Anderson, Wes Keith, Yemina, Ballast's co-op, BTL's cast, BCODA's Lotus members); edge 3113 (Idris member_of Arcturus) invalidated and replaced by member_of the Aerostatic Regulatory Command (3138, law 01a103b1). New law 01a10ad7 (IOWA: the Exclusion Zone is the interior, ICFI's farm the Mississippi margin). Incidentals: Velos Street (IXS), Biter (TWU), One Word (UNDR). New place Rogers Park; six wrong "Park" tags removed; "Silver" in Silver Pigeon (BCODA, CRIT, PXL, TLC) and "Noodle" in the Hokkien noodle pack (PXL, SPRW) moved to the right records; Cordon Freight tagged in ICFI.
- **Open for the author:** about sixty flags, filed as one author order under 01a10a85: Mrs. Chen's place across BCODA, CXC, DWIACE and NXR; the Ledger, Bear and Stash records each spanning several books; the Okafor and Vasquez name collisions; IXS's leftover Nari-extraction fragment and missing scenes; TLC's Femi movements; BLST's touchdown shown as a public feed item in four books; TWD's Rotterdam stairwell; UNDR's "the Works" tagged as The Warm and its duplicate Underclan records; and others listed per book. No export, no audio.

### Round 7, continued: the wheel codified, author calls applied (2026-10-05)

- **Codified:** the focus wheel is the project skill `/glmz-wheel` (`.claude/skills/glmz-wheel`, engine order 01a10d2e). Its rules: mechanical defects are page-laws, never reader work (spaced hyphen as dash 01a10d17-e920, doubled punctuation 01a10d19-40c1, retired spellings 01a10d17-fe30); `tools/select.py` re-reads a book only where another book's applied rows renamed, rewrote or drew in an entity it shares (cycle 2 would read 9% of the catalog's words); borrowing is capped at three rows per book with zero as normal; author questions come before the next cycle. Dumps are written as UTF-8 (the cycle-1 dumps garbled em dashes; nothing garbled reached the books). The mechanical sweep fixed 90 stray spaced hyphens in ten books.
- **Tool gaps:** two were data, not code: a stale incidental ruling kept "Rogers Park" untagged (superseded by law 01a10d13; Rogers Park is a place), and "the Works" was an alias on The Warm (new place The Works 01a10d14; 40 tags moved; The Warm narrowed to the heat ring). Edges never touch F1; the red units came from record and tag edits. Capture's `--pin-name` now names the tag that blocks it and any same-name incidental ruling, and `--retag-name` says how to make a removal stick (commit 2a7c3628a, Hub build 102abc45ef9a). Mnemosync's checkpoint "Shen" is suppressed by an incidental ruling (01a10d16).
- **Author calls (rulings 01a10ee7-*, 01a10f07-*):** Mrs. Chen cooks daily at the open-air Wentworth Passage stall and keeps the sixty-year Halsted counter under the Lake Street elevated for wakes (DWIACE, CXC and NXR spliced: no Pilsen counter, no booth, no glass, the NXR drop under a paving stone). Ledger is only in Bushido Coda and Critical Mass; Testament's broker is Raafe Choi (new record 01a10f0a, nine mentions renamed and retagged). Stash of The Long Cut and Bushido Coda is one woman. Candelaria's touchdown is public news but no book except Ballast says Ashgrave did it (ICFI, TWD, BCODA spliced). Bear is one man across TWU, TWD, TEST and ATTE, and his job is warehouse security for paramilitary equipment: the Ironbend depot in Testament, the Halsted Freight Yard's bonded bay in Attendance. Tavi taught Reza the click-signal in the upper Yards (TWD spliced; the Rotterdam rumour stays). Iron & Silk's leftover extraction fragment (#11071-#11087) was reworked into the later version: the threat is an unmanned Lotus sightline node, Lace's single alarm stands.
- **No waterfront shipping (page-law 01a10f07-da62, author: "there are no DOCKS in Chicago"):** 186 hits in 17 books spliced by hand-reviewed dockets: docks became loading bays, yards, depots and freight terminals; the Old Harbor barge is the Old Harbor freight lift, an Eigenlift freight platform (CXC, NXR, IXS agree); Bushido Coda's "Chapter 6 — The Dock" is "Chapter 6 — The Loading Bay"; dockhand frames are yardhand frames (CRIT, SRZR). Old Harbor survives as a district name; its record and Ironbend's and Bear's were rewritten to match.
- **State after:** all 22 books 0 law hits, every beat read as it stands, F1 green on every unit. No export, no audio.
