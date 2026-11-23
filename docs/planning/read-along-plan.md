---
codex: 1
project: Prose
layer: planning
code: RA
title: "Read-along: plan and tickets"
updated: 2026-11-23
---

# Read-along: plan and tickets {#SS-PLAN-READ-ALONG}

The plan for bringing the read-along to Prose: a manuscript is read chapter by chapter, and Prose keeps the
story bible, the continuity checks, the map and the conversation, **without writing a word of the book**.
The design is [RFC 0019](../rfc/0019-read-along.md).

**State on 2026-11-23:** Phase 1 is built and pushed on `claude/nifty-mayer-ov2vgx` (commit `64e84d3`). Nothing in
Phases 2–4 has been started. Tickets RA-xx are provisional ids. Register each one with the tickets CLI to get its
PRS-T number, and open a work order before building, per the factory's law 3. Phase 1 went in without a work order
because the live database is paused until 2026-12-01.

---

## Feature inventory (what the read-along must do)

| # | Capability | Phase 1 status |
| --- | --- | --- |
| 1 | Import one Word document or a folder of them; find chapter breaks; read first page to last | done (docx/txt/md; formatting not kept, see RA-34) |
| 2 | Reading bar ("13 of 20 chapters", chips for names just met) and reading log (who arrived, details, contradictions) | done |
| 3 | Automatic story bible: characters, places, items, organizations, concepts; traits, aliases, relationships | done (deterministic patterns) |
| 4 | Drafts with **Keep it** / **Not quite**; tossed stays tossed; confirmed facts never overwritten | done |
| 5 | Every detail quotes the exact line and chapter it came from (**Why?** / Hide source) | done |
| 6 | Entry panel: Details / Timeline, aliases, group, description, attributes, Delete | done (group by hand only, RA-29) |
| 7 | Continuity checks against everything written so far: character facts, timelines, items, places, world rules, descriptions | done for same-attribute disagreements; differently-worded ones are RA-26 |
| 8 | Continuity Inbox cards: both quotes, kind, confidence, Accept new / Keep existing / Retcon / Dismiss / Explain | done |
| 9 | Gold issue counter in the editor header; one tap from a card to the contested line | done in the Story workspace; Writer has the counter, no deep link yet (RA-22) |
| 10 | Nothing flagged mid-sentence: check a chapter once it is written | done (job with a quiet period) |
| 11 | Ask your book: answers cite chapters, door chips jump to passages; talks plot, never writes prose | done (LLM optional) |
| 12 | Story Atlas: constellation by category, hover preview, click to entry, time scrubber back to chapter 1 | done |
| 13 | Brainstorm canvas | done |
| 14 | Writing workspace: volumes/chapters/scenes tree with drag reorder, focus mode, readable typography, word counts, autosave, "remembered this chapter" | done except scenes in the tree (RA-24) and names highlighted inside the Writer editor (RA-23) |
| 15 | Works on a phone (bottom bar: Editor, Notes, Chat, Brainstorm, Story) | Story workspace responsive; reaching the Hub from a phone is RA-35 |
| 16 | Export to Word, formatting intact, any time | endpoint built; refused by the read gate until the book is read (RA-33) |
| 17 | Free allowance (words read, questions asked); nothing deleted at the limit | done as an optional setting, default unlimited |
| 18 | Your words never train anything; manuscript is private | inherent (local-only) |

---

## Phase 1 — Core read-along (DONE, `64e84d3`)

| Ticket | Title | Delivered |
| --- | --- | --- |
| RA-01 | Data model + `AddReadAlong` migration | `ChapterReadings`, `StoryBibleSuggestions`, `StoryFacts`, `ContinuityConflicts`, `BrainstormCards`, `BookChatMessages`; verified on SQL Server 2022 |
| RA-02 | Deterministic extractor | `StoryFactExtractor`: names, categories, alias folding, pronouns with learned gender, details, relations, normalization, compare |
| RA-03 | Read-along service | chapter grouping, content hash, read book/chapter, drafts, observations, cards, fixed-by-edit, status/log |
| RA-04 | Story bible service | codex, drafts, Keep it / Not quite / Keep all, entries with receipts + timeline + links, hand-written entries/details, remove |
| RA-05 | Continuity Inbox service | list/count/explain/rule/reopen |
| RA-06 | Story Atlas service | stars, typed + co-occurrence threads, first-chapter scrubbing |
| RA-07 | Ask your book | deterministic retrieval, doors, LLM answer with no-prose prompt, fallback, history, question allowance |
| RA-08 | Brainstorm canvas | cards CRUD, links, entity tie |
| RA-09 | Manuscript import | parser (Heading styles, "Chapter N", Part/Volume, scene breaks, natural file order) + `AppendBeatsAsync` |
| RA-10 | Allowance | `readalong.allowance` shared setting, usage |
| RA-11 | Background job | `ReadAlongBackgroundService` (opt-in per book, quiet period, config switches) |
| RA-12 | CLI | `ReadAlongCli`: 8 verbs; `CLI_COMMANDS.md` regenerated |
| RA-13 | MCP | `ReadAlongTools`: 12 tools; `MCP_TOOLS.md` regenerated |
| RA-14 | Story workspace UI | `/story` (manuscript, codex, continuity, ask, atlas, brainstorm, import), `/story/export/{id}.docx`, phone layout |
| RA-15 | Writer integration | remembered tick, ⚠ counter, chapter words + minutes, Story link, focus mode |
| RA-16 | Docs | RFC 0019, README |
| RA-17 | Tests | 27 tests (extractor + services on SQLite); Chromium walk-through against local SQL Server |

---

## Phase 2 — Ship it (open)

### RA-20 · Apply `AddReadAlong` to the live database
- **When:** after Azure SQL resumes (2026-12-01).
- **Do:** start the Hub (it applies pending migrations), confirm the six tables, run `prose --read-along --status` on a test book.
- **Done when:** the tables exist, the Writer opens with no read-along error in the status bar, and the job logs a sweep.

### RA-21 · Register tickets and the work order
- **Do:** create PRS-T tickets for RA-01…RA-17 (closed, citing `64e84d3`) and for every open RA ticket here. Open an engine work order covering `src/**/ReadAlong*`, `src/Prose.WriterUi/Components/Story/**`, `docs/rfc/0019-read-along.md`.
- **Done when:** `docs/USER_STORIES.md` re-renders with them.

### RA-22 · Deep links into the Writer
- **Do:** `/writer?book=<id>&beat=<id>` opens that book and beat. Story's **✎ Write**, every door, and the Writer's ⚠ counter use it.
- **Done when:** a door on a continuity card opens the Writer on the contested beat.

### RA-23 · Story-bible names highlighted in the Writer editor
- **Do:** in Display mode, mark kept names and aliases (dotted underline). Click opens the entry panel beside the prose. This is a display layer only and never changes the beat markup.
- **Depends:** RA-22.
- **Done when:** the Writer shows the same highlights as `/story`, and a save shows no tag diff from them.

### RA-24 · Scenes in the manuscript tree
- **Do:** show scene and sequence nodes under chapters, and support drag reorder within and across chapters (`ReparentNode*`). Add "+ Scene".
- **Done when:** a scene dragged between chapters moves its beats with it, and the read-along re-reads both chapters.

### RA-25 · "Follow this book" switch
- **Do:** an explicit per-book on/off for the job, instead of "followed once read". Show it in the reading bar.
- **Done when:** turning it off stops sweeps for that book, and nothing already read is lost.

### RA-33 · Word export versus the read gate (**author decision needed**)
- **Problem:** the read gate (law 9, no override) refuses to export a freshly imported book until every beat has been read in Read Mode.
- **Options:** (a) keep it as is: the export page explains and points to Read Mode (current behaviour); (b) add a "Read Mode → mark this chapter read" flow from the Story workspace so authors can clear the gate quickly; (c) a manuscript-only "export what I wrote" mode for author-written books. Option (c) changes law 9 and needs a ruling.
- **Done when:** a ruling is recorded and the chosen option is built.

### RA-34 · Import keeps formatting
- **Do:** carry italic and bold from `.docx` runs (and `*`/`**` from Markdown) into beat inline markup. Keep scene breaks as a marker beat or `IsSceneBreak`. Accept `.odt`, `.rtf` and `.epub`. Add folder drag-and-drop in the UI (`webkitdirectory`).
- **Done when:** a round trip of docx in, Word out keeps the italics.

---

## Phase 3 — Make it smarter (open)

### RA-26 · Differently-worded contradictions (**author decision needed**)
- **Problem:** "needs three months to refill" against "dead until spring" is not caught, because the attributes differ.
- **Options:** (a) more normalizing patterns, deterministic; (b) an optional LLM pass that only **proposes** cards (marked "suggested by model"), off by default. Option (b) touches law 7 (no LLM judges) and needs a ruling.

### RA-27 · Extractor recall, measured
- **Do:** add patterns for height and build, handedness, weapon/hand, "N siblings", "X lay north of Y", titles in apposition, unnamed referents ("the girl"), and dialogue tags with speech verbs.
- **Measure:** run the read-along on two finished GLMZ books in a scratch copy. The author marks a sample of 100 drafts and 50 cards as right or wrong. Record precision per pattern (RFC 0014 instrument value audit) and switch off any pattern under 80%.
- **Done when:** a precision table is committed and the low-value patterns are retired.

### RA-28 · Pronouns and coreference
- **Do:** learn gender from more evidence ("his own", kin nouns, prior entries' records), handle plural "they", and detect nested dialogue speakers.
- **Done when:** a fixture of 30 pronoun cases passes.

### RA-29 · Codex editing
- **Do:** rename an entry, change its category, merge two entries (facts, aliases and cards follow), edit Group inline, and collapse to "Show all (N more)" past 6 details.
- **Done when:** merging "Hollis" into "Hollis Marsh" leaves one entry with both aliases and every receipt.

### RA-30 · Atlas depth
- **Do:** show typed thread labels on hover, toggle categories, pin and drag stars, show an "as of chapter N" entry view (facts the reader knew then), search, and render the threads of the highlighted star only.
- **Done when:** scrubbing to chapter 3 and opening an entry shows only facts from chapters 1–3.

### RA-31 · Ask depth
- **Do:** stream answers, make the conversation length configurable (the "much longer conversations" tier), offer "Ask about this card" from the inbox, and save an answer to the brainstorm board.
- **Done when:** a 30-turn conversation stays grounded and every answer has at least one door.

### RA-32 · Brainstorm depth
- **Do:** pick an entity for a card, zoom and pan the board, support touch drag on phones, and filter cards by colour.

---

## Phase 4 — Hardening (open)

### RA-35 · Phone access (**author decision needed**)
- **Problem:** the Hub binds to 127.0.0.1, and the hosted deployment was retired on 2026-08-23. The Story workspace is responsive, but a phone cannot reach it.
- **Options:** (a) LAN binding with the API key and a QR code; (b) Remote Control or a tunnel; (c) bring hosting back for `/story` only.

### RA-36 · Accessibility and light theme
- **Do:** keyboard paths for drag reorder and the board, screen-reader text for the atlas (a list fallback), light-theme contrast check, and `prefers-reduced-motion`.

### RA-37 · Performance on big books
- **Do:** cache chapter grouping and hashes per book (invalidate on beat write). Today `IsRememberedAsync` walks the whole book on every beat open in the Writer. Load the known-entity index once per sweep instead of once per chapter (now up to 20k rows). Batch saves during import.
- **Done when:** a 200-chapter book reads in under 2 minutes, and a Writer beat opens in under 50 ms of read-along overhead.

### RA-38 · Fresh databases cannot replay the migration history (pre-existing)
- **Found:** `dotnet ef database update` on an empty SQL Server fails at an old migration ("Column 'NodeCode' … invalid for use as a key column"). Building the schema from the model works.
- **Do:** add a squashed baseline or a documented "create from model + stamp history" script, so local test databases are reproducible.

### RA-39 · Scheduler and operations
- **Do:** add a MindAttic.Scheduler job that runs `prose --read-along --all` nightly as a backstop to the resident job, add the job's last sweep to the `/app` dashboard, and log tokens (zero) and duration to the command ledger.

### RA-40 · Agent protocol
- **Do:** add the read-along to `docs/agent/PROSE_PROTOCOL.md`. Agents may read the codex, cards and atlas. Agents never keep, toss or rule without the author's word in the session. Consider a `/story` prompt alias.

---

## Order of work

1. RA-20, RA-21 (unblock: database live, tickets registered)
2. RA-33 and RA-26 rulings (decisions only)
3. RA-22 → RA-23 → RA-24 → RA-25, RA-34
4. RA-27 measurement before any further extractor work (RA-28), then RA-29 to RA-32
5. RA-35 ruling, then RA-36 to RA-40
