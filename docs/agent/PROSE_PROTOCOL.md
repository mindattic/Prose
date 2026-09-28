# Prose Agent Protocol

Version: `2.0` (RFC 0015, the Novel Factory, 2026-09-23)
Status: active

Prose is a Hub-backed prose engine. The database is authoritative and all database access goes
through `Prose.Hub`; an agent must never use direct SQL, EF, or a private copy of the database.
The design is `docs/rfc/0015-novel-factory.md`. **The plan is not this file or any file: it is the
factory's live state in the Hub.**

## Start every session

1. The SessionStart hook opens a factory session and injects the FACTORY block: the next action
   (computed from the book's state), the books on the line, the last session's summary, the laws.
   If it says FACTORY UNREACHABLE, start the Hub (`src\tools\deploy-apps.ps1 -Start Hub`) and do no
   story work from memory.
2. Without the hook: `prose --factory next` (MCP `factory_next`) and `prose --order list`.
3. Do the next action. A bare `do` means exactly that.

## The laws

1. The book is its beats; the world is its entities. Nothing else is stored about the story — no
   outline, spine, blueprint, ledger, summary or reconciler.
2. Every story change is a logged Hub call (MCP or the prose CLI). Never raw SQL.
3. Repo changes need an open engine work order whose paths cover them, and a commit that names it
   (`WO:<id>`). The Stop hook refuses changes no open order covers.
4. Done is computed (a station passes) or Hub-validated (a work order's checks). Claims do not count.
5. A decision goes into the world the moment it is made: `record_ruling`, or the entity record,
   with a read-back.
6. The book is read and written whole: a chapter is a unit of work, never a boundary of sight.
7. Deterministic checks only. No LLM judges, votes, or scores.
8. A failed check is reported, never compensated with a new system.
9. No override on the read gate. Export only what has been read as it stands.
10. End every session with `/quicksave` (`session_end`): every decision references a ruling or order.

## The line (per chapter)

F2 Planned → F3 Written → F4 Captured → F5 Read → F6 Clean → F1 Verified, then F7 Pressed and
A Audio for the book. `prose --factory status --node <book>` shows the matrix.

- **Heal a written book:** read it in order with `read_beats(markRead)` / `prose --read-beats …
  --mark-read --read-by claude`; file defects as read notes; fix with `splice_beats` (dry run,
  then apply); re-read what changed; entity corrections are filed as notes during the pass and
  applied as one batch after it; verify each record; export.
- **Write a new book:** create the book and chapters; plan beats (title + description); write each
  chapter in-session with the full prior prose and the cast's records in view; capture new names and
  facts into the world; then heal it as above.
- **Rebuild a written book** (structure, not polish: cut, expand, re-slot, deepen, change the
  ending). The story itself is never stored anywhere but the beats; the rebuild adds no outline,
  spine or registry. Apply the whole-book checklist (CraftGuide §12, `SS-CRAFT-NOVEL`) on every
  read.
  0. **Clone and fence.** `--archive-book` the original, then `--clone-book --slug <orig> --title
     "<same title>" --book-code <CODE>2 --draft`. Assert that beats and words match. Rulings are not
     copied: re-seed the original's book rulings (`--ruling seed --node <CODE>2 --file …`) and
     confirm F4 and the metrics match the original. Create a `<CODE>2CUTS` book with one chapter to
     hold cuts. Open an author order on the clone (`work_order_add kind:author nodeIdOrSlug:<CODE>2`)
     so it is on the line. The original stays frozen.
  1. **Diagnosis read.** Read the clone front to back with `markRead`. File structural failures
     against the checklist as read notes on the beat where they show. Run `get_entity_beat_mentions`
     for every named character.
  2. **Gate G1 (author).** Propose the restructure: cuts with word counts, new scenes, merged or
     deepened characters, re-slotted chapters, the ending. Only the decisions the author approves
     persist, each as a `record_ruling` on the clone.
  3. **Restructure.** Create and re-slot chapters (`create_chapter`, `--reparent-node --after-slug`).
     Move each cut beat to the cuts book with `--move-beat-to-node`, which moves the same row so the
     text is kept byte for byte. Insert new scenes as planned beats: a title and a description of
     what the scene does, including what it plants or pays off. Check the counts: book beats plus
     cuts beats equal the original beats plus the planned beats inserted.
  4. **Write and revise.** Work in reading order, in session, with `factory_context(priorUnits: all)`.
     Write planned beats. Splice revisions with a dry run first, against the raw tagged text, by
     hand. Capture new names and facts into entities and read each one back.
  5. **Heal** as above, until F1–F6 pass on every unit, metrics hold and `law_violations` is 0.
  6. **Gate G2 (author).** The author reads and decides whether to press. No audio unless the author
     asks for it.

  **Every later change** to prose, entities or facts in any book follows the same rules:
  - archive before editing prose;
  - splice with a dry run first;
  - record every decision (`record_ruling` or the entity record) and read it back;
  - never press over an unread beat.

  The read gate marks a beat unread when its text changes, when it moves, or when an entity it
  mentions changes. Re-read the flagged beats before pressing.

## Transport-neutral operation envelope

Every adapter should preserve this envelope, whether it is MCP, CLI, or HTTP:

```json
{
  "protocolVersion": "2.0",
  "operation": "factory_next",
  "universe": "glmz",
  "arguments": {},
  "requestId": "client-generated-id"
}
```

Responses must identify `requestId`, operation, universe, status, result, warnings and ledger
identifiers. Errors are structured and name the failed precondition (`hub_unreachable`,
`missing_universe`, `unknown_argument`, `write_gate_rejected`, `unread_beats`,
`check_failed`, `decision_unrecorded`).

## Handoff

There is no transcript file. The session is a `FactorySessions` row; `/quicksave` ends it with a
summary whose every decision references a recorded ruling or work order, and the next session's
start hook shows that summary beside the live next action.
