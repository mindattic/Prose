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
