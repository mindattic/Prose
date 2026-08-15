---
description: Log durable canon/craft/structural facts into the real Dynamic Context Memory DB tables (not scratch .md files, not just Claude's own memory) so future prose generation and sessions recall them with perfect fidelity.
argument-hint: "[what to log, or omit to log whatever was just discussed/discovered]"
allowed-tools: Bash, PowerShell, Read, Edit, Write, Grep, Glob, mcp__prose__set_canon_section, mcp__prose__set_book_bible, mcp__prose__generate_node_doc, mcp__prose__sync_markdown_files, mcp__prose__create_character
---

# /dcm — write it into Dynamic Context Memory, not a scratch file

**The point of this command**: this project has hundreds of ephemeral `.md` files under `docs/`
and `docs/nodes/` that are all GENERATED MIRRORS, gitignored, and regenerated on demand (SS-A45).
They are not memory — they are a cache. The actual persistent memory is the SQL database:
`CanonDocumentSections` (world/craft/universe facts), `Nodes.NodeBible` (per-book facts),
character/entity records (`Speech*`/`Psychology*` fields, wounds, continuity claims). **When you
learn or decide something worth remembering across sessions, write it to the DB row that owns it,
then regenerate the mirror — never the reverse, and never leave it living only in a hand-edited
`.md` file, a scratchpad note, or your own Claude Code memory.** Claude's own memory system
(`~/.claude/projects/.../memory/`) is for facts about how to collaborate with the user, not for
project canon — canon belongs in this project's own DB so the prose engine itself can see it.

## Step 0 — figure out which table owns the fact

| The fact is about... | Lives in | Write via |
|---|---|---|
| Engine invariant, GLMZ world fact, Fantasy/Entos world fact | `CanonDocuments`/`CanonDocumentSections` | `mcp__prose__set_canon_section` (preferred) — regenerates `docs/BIBLE.md`/`docs/WORLD.md`/`docs/GLMZ.md`/`docs/SCRY.md`/`docs/universes/ENTOS.md` etc. If MCP isn't connected, do a parameterized DB update against `CanonDocumentSections.Content` (see Step 2) then run `prose --generate-canon-md --type <Type>` |
| One book's arc, characters, voice register, locks, blueprint, beat spine, or a **structural/state discrepancy note** (like "this book's chapter split regressed in the live DB") | `Nodes.NodeBible` (that book's row) | `mcp__prose__set_book_bible` (preferred, full-body write) — or CLI `prose --set-book-bible --slug <slug> --file <path>` (also a FULL OVERWRITE — read the current bible first, append/edit, write the whole thing back). For a small addendum, a parameterized `UPDATE Nodes SET NodeBible = NodeBible + @note WHERE ...` is safer than round-tripping the whole (often 50-100k char) document through a file. Always follow with `prose --generate-node-doc --slug <slug> --universe <u>` + `prose --sync-markdown` so the mirror and `MarkdownFiles` (what DocContextService actually injects) pick it up |
| A character's voice, psychology, wounds, relationships | That character's `Entity`/`Character` record | `mcp__prose__create_character` (pass the id + the changed field) — never a `docs/registers/*.md` file, those are retired (SS-A46) |
| A craft principle (universal prose rule, or a universe-specific craft addition) | `CanonDocumentSections` row inside the CraftGuide/GLMZ-craft/SCRY-craft document | Same as row 1 — **do NOT hand-edit `docs/CRAFT.md`/`docs/GLMZ.md`/`docs/SCRY.md` directly**, despite what CLAUDE.md's summary table says; those files carry a "GENERATED — do not hand-edit" banner and CLAUDE.md is stale on this point (verify DB-backed generation is still true via `Program.cs`'s `--generate-canon-md` handler before trusting either source blindly) |

If genuinely unsure which table owns it, query `CanonDocuments`/`CanonDocumentSections` for the
right `SectionKey`/document, or ask — don't guess and don't default to "just leave it in a memory
file" as the easy way out.

## Step 1 — check whether the MCP path is actually connected

Try the relevant `mcp__prose__*` tool first. If the prose MCP server isn't connected this session
(check via `ToolSearch` for `select:mcp__prose__set_canon_section` or similar — an empty/failed
match means it's not up), fall back to a **parameterized** DB write — never a raw `sqlcmd -Q` with
literal special characters/em-dashes embedded in the command string (that has silently mojibake-
corrupted text before). Use `scripts/gspl_db.ps1`'s `Invoke-SSNonQuery`/`Assert-SSCount` helpers
(or equivalent `System.Data.SqlClient` with a real `SqlParameter`), and **always verify the write
landed** with a follow-up `SELECT`/`CHARINDEX` check before reporting success.

## Step 2 — regenerate the mirror, don't skip it

A DB write with no regeneration means the DCM injection pipeline (`DocContextService`) still sees
the OLD content via `MarkdownFiles` until the next sync. Always finish with the narrowest
regeneration command that covers what you changed:
- `prose --generate-canon-md --type <Type>` (not `--all` unless you touched more than one canon doc)
- `prose --generate-node-doc --slug <slug> --universe <glmz|scry|nonfiction|...>`
- `prose --sync-markdown` (pushes the regenerated `.md` into `MarkdownFiles`, which is what
  `DocContextService.PrepareForNodeAsync` actually reads at generation time)

## Step 3 — report exactly what you wrote and where

State: which table/row changed, what the new content says (or a summary if it's long), which
regeneration commands ran, and confirmation the mirror file now reflects it (e.g. grep the
regenerated `docs/nodes/<CODE>.md` for the new content, or check `SyncedAt`/row count from
`--sync-markdown`'s own output). Do not just say "logged it" — show the verification.

## Argument handling

If the user gave you `$ARGUMENTS`, that's the fact/decision to log — go straight to Step 0 for
it. If they invoked `/dcm` bare, log whatever was just discussed, decided, or discovered
immediately before this command in the conversation — don't ask them to repeat it.
