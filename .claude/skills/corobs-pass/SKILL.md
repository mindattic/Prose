---
name: corobs-pass
description: Run one COROBS hand pass over a universe's entities (RFC 0016 §4, protocol H phases 1-2) — compare every person, place and thing against every other by hand, confirm the truths that emerge as rulings, fix each contradiction one record at a time with read-back, and log the baseline the bench will later compare against. Usage /corobs-pass [universe] [pass N]; default glmz, next pass number.
---

# COROBS hand pass

The author's design (2026-11-02, RFC 0016, campaign root 01a19a79-2689): before any COROBS code, the
geometry is done by hand twice. Each pass reads the whole catalog as plies (one field class at a time),
finds the groups that agree far more than chance, confirms what they mean, and fixes what contradicts
them. The pass is the evidence the instrument is sized by; its numbers are the bench's old-way column.

Universe GLMZ: `--universe glmz` on every CLI call. One `prose` call at a time. No export of books, no audio.
Scratch only for working files (never the repo, never the DB).

## Law that binds the pass
- Results not tools: scratch scripts may sort, count and draft patches (arithmetic); they never decide and
  never write to the Hub. Every write is one `prose` call per record with a read-back.
- No bulk find-and-replace. A list of 60 similar fixes is 60 patches, each read back.
- A record write un-reads every beat that tags the entity (`--confirm-unread` makes it explicit). Off-page
  records cost nothing; on-page cosmetic fixes are deferred to the book's next read; load-bearing facts
  (geography, status, membership) are made and the un-read accepted.
- A truth is recorded with `--ruling add --kind law --source agent`; a regex `--pattern` only when it is a
  zero-tolerance prohibition that cannot match legitimate text (`^(woman|man)$` yes; a word anywhere, no).
  Record patterns through `dotnet run --project src/Prose.Cli -- …` — `prose.cmd` goes through cmd.exe,
  which eats `^` and `|`.
- Draft books (SCON, IxS, Ballast, ICFI) are never fixed; their beats may be un-read as a side effect.

## One pass
1. Open an author order "COROBS hand pass N" under the campaign root; name it in every ruling.
2. Dump read-only to scratch: `prose --universe glmz --export global` (every repo but people) and
   `--export people` (zips land in `D:\Projects\MindAttic\ePub\MindAttic\GLMZ\`), `--graph-health --json`.
   Unzip into `<scratch>/records/<repo>/`.
3. Inventory the plies: `python -I -X utf8 tools/inventory.py <records> inventory.md` (field coverage per type).
   Fields populated on most records are plies: species, age, status, range, home_turf/location, affiliation,
   gender/pronouns, coordinates, tags, adjacent_to, timeline.
4. Run the plies: `python -I -X utf8 tools/plies.py <records> plies.md` — vocabulary variants, (0,0) places,
   turf→place resolution and clusters, frequented_by reciprocity, faction members vs affiliation, species×age.
5. Prose evidence: `powershell -File .claude/skills/glmz-wheel/tools/dump.ps1 -Work <scratch>/prose` with a
   `books.txt` of the canon slugs; grep the `_clean.txt` files for every name in question; index
   `<entity guid="…">` tags in the tagged dumps to know which records are on the page at all.
6. Confirm truths by reading records and beats, then record them (see Law above). A truth the records
   already obey needs no fix; one they break becomes a contradiction list.
7. Fix one record at a time: `--get-entity-fields --id` (full record) → patch JSON (merge patch: lists are
   replaced whole, so resend the full list; a timeline entry must carry the same field types as its
   siblings) → `--set-character-fields | --set-entity-fields --id --file [--confirm-unread]` → read back.
   New places: `--add-place --file` (DistrictData JSON); new factions: `--add-faction --file`.
8. Log the baseline in `pass<N>-log.md`: examined · truths · contradictions found/fixed · un-read beats ·
   **cascade** (items that closed without their own fix) · plies that carried signal · time.
9. Pass 2 repeats on every record modified since pass 1 began plus its 1-hop neighbours and its time window.
   Dry when zero new fixes. The author closes pass 2 before the first engine order under RFC 0016 §6.

## What pass 1 taught (2026-11-02)
- 1,695 of 1,906 GLMZ characters are tagged in no canon book: background population, not stale records.
- `frequented_by` and faction `known_members` were machine-generated (prose text; substring matches) — the
  old way cannot check who is where or who belongs to what. `age 0` means unknown.
- Fourteen districts named as home by ~160 characters had no place record and appear in no book; creating
  the places (T5) resolved all of them at once — the first cascade.
- Scratch tools live in the session scratchpad under `corobs-pass1/tools/` (inventory.py, plies.py,
  apply_patches.sh); copy them forward into the next pass's scratch, do not commit them.
