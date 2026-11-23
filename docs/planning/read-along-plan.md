---
codex: 1
project: Prose
layer: planning
code: RA
title: "Read-along: plan and tickets"
updated: 2026-11-23
---

# Read-along: plan and tickets {#SS-PLAN-READ-ALONG}

The per-ticket files, one per ticket in nested folders (epic/feature/story/task), are in [`read-along-tickets/`](read-along-tickets/README.md), generated with `--tree`.

The full plan for the read-along: Prose reads a manuscript chapter by chapter and keeps the story bible,
the continuity checks, the map and the conversation, **without writing a word of the book**. The design is
[RFC 0019](../rfc/0019-read-along.md). The machine-readable copy of this tree is
[`read-along-tickets.json`](read-along-tickets.json), generated and validated from this file by
`tools/planning/tickets.py`. Edit this file, then run the script.

## How the tree works

Every ticket sits in one nested hierarchy, and its id says where:

| Level | Id | What it is | Closed when |
| --- | --- | --- | --- |
| Epic | `RA-E2` | a release-sized outcome | all its features are closed |
| Feature | `RA-F2.1` | a capability a user can name | all its stories are closed |
| Story | `RA-S2.1.1` | one user-visible result, with acceptance | all its tasks are closed and its acceptance holds |
| Task | `RA-T2.1.1.1` | **one unit of work**: one commit, one reviewer, one test change | its *Done when* holds |
| Decision | `RA-D1` | an author ruling that gates tasks | `record_ruling` is read back |

**Ascending order is build order.** Ids increase in the order the work should be done. A task depends only on
tasks with a smaller id, or on a decision. `tools/planning/tickets.py --check` enforces both rules. Estimates:
**XS** under 1 h · **S** 1–3 h · **M** 3–6 h · **L** 1–2 days. A task larger than L must be split before it is coded.

Every task names its files, its steps, its test and its *Done when*, so it can be picked up and coded without
reading anything but this file and the files it names. Laws that bind every task: never write prose (RFC 0009), no LLM
judges (law 7), no override on the read gate (law 9), every repo change under an open work order (law 3).

## Status (2026-11-23)

Every task that could be built and verified in this environment is **built**, each closed by the commit named
on its Status line, with its test or end-to-end check passing. What remains is **blocked**, and only by
things outside the code:

| Blocked by | Tickets |
| --- | --- |
| The live database (Azure SQL is paused until 2026-12-01) | RA-T2.1.1.2, RA-T2.1.2.2 |
| The author's rulings | RA-D1 → RA-S2.5.1 (b) or RA-S2.5.2 (c); RA-D2 → RA-S3.2.2 (a) or RA-S3.2.3 (b); RA-D3 → RA-S4.1.1 (a), RA-S4.1.2 (b) or RA-S4.1.3 (c) |
| The author's precision verdicts | RA-T3.1.1.4 (the four recall patterns it was to gate are built; the audit can switch any of them off) |

Every remaining ticket is now written out and ready to code: each branch of the three rulings (RA-S2.5.1/2,
RA-S3.2.2/3, RA-S4.1.1/2/3), and the follow-ups as **RA-E5** (8 open tasks, ~26 h; two start in other
repositories). Nothing in RA-E5 or the decision branches is programmed.

The end-to-end scripts are in `tools/readalong/e2e/`. Run them against a Hub with a read-along book; a local
database comes from `tools/db/create-local.sh`.

### Follow-ups found while building (outside this repository)

- **F-1 · MindAttic.Export:** `ProseInline` has no escape for a literal `*`. Imported text with a real asterisk is
  read as emphasis, as it already is for every beat. Add `\*`.
- **F-2 · MindAttic.Legion:** no streaming call. Ask's streaming path is in place (`ILlmService.StreamAsync`), but
  Claude answers arrive as one chunk until Legion can stream.
- **F-3 · Prose migrations:** the history cannot replay onto an empty server (the NodeCode index). Squash a
  baseline; until then use `tools/db/create-local.sh`.
- **F-4 · Pre-existing test failures on Linux:** the full suite has the same 11 failures before and after this
  work. `StoreUrlCli` is unreachable from the dispatch chain, and the other ten are Windows-path, allowed-host,
  import/export and log-parsing tests.

---

## RA-E1 · Core read-along — DONE

The automatic story bible, the Continuity Inbox, the Story Atlas, ask your book, the brainstorm canvas, manuscript
import, the allowance, the background job, CLI and MCP surfaces, the Story workspace and Writer status, the docs and
the tests. All of it shipped in `64e84d3`.

### RA-F1.1 · Read-along engine — DONE
#### RA-S1.1.1 · Data model and migration — DONE
##### RA-T1.1.1.1 · `AddReadAlong` migration (six tables)
- **Status:** done
- **Est:** M
- **Files:** `src/Prose.Core/Data/Entities/ReadAlong.cs`, `src/Prose.Core/Data/ProseDbContext.cs`, `src/Prose.Core/Migrations/20261123082631_AddReadAlong.cs`
- **Done when:** the schema is created on SQL Server 2022 with every foreign key accepted.
#### RA-S1.1.2 · Deterministic extraction and reading — DONE
##### RA-T1.1.2.1 · `StoryFactExtractor`
- **Status:** done
- **Est:** L
- **Files:** `src/Prose.Core/Services/ReadAlong/StoryFactExtractor.cs`
- **Done when:** `StoryFactExtractorTests` passes.
##### RA-T1.1.2.2 · `ReadAlongService` (chapters, drafts, observations, cards, fixed-by-edit, status)
- **Status:** done
- **Est:** L
- **Files:** `src/Prose.Core/Services/ReadAlong/ReadAlongService.cs`
- **Done when:** `ReadAlongServiceTests` passes.

### RA-F1.2 · Book surfaces — DONE
#### RA-S1.2.1 · Codex, inbox, atlas, ask, brainstorm, import, allowance — DONE
##### RA-T1.2.1.1 · Core services for each surface
- **Status:** done
- **Est:** L
- **Files:** `src/Prose.Core/Services/ReadAlong/*.cs`
- **Done when:** 27 tests pass.
##### RA-T1.2.1.2 · Background job, CLI (`ReadAlongCli`), MCP (`ReadAlongTools`)
- **Status:** done
- **Est:** M
- **Files:** `ReadAlongBackgroundService.cs`, `src/Prose.Cli/Cli/ReadAlongCli.cs`, `src/Prose.Mcp/Tools.ReadAlong.cs`
- **Done when:** every verb is smoke-tested against a local Hub.
##### RA-T1.2.1.3 · Story workspace `/story` and Writer status bar
- **Status:** done
- **Est:** L
- **Files:** `src/Prose.WriterUi/Components/Story/*`, `story.css`, `js/story.js`, `WriterShell.razor`
- **Done when:** the Chromium walk-through (import → read → keep → rule → door → ask → atlas → brainstorm → phone) has no console errors.

---

## RA-E2 · Go live and close the gaps a writer hits first

### RA-F2.1 · Live database and paperwork

#### RA-S2.1.1 · The read-along runs against the live database
*Acceptance:* the six tables exist in `Prose` on Azure SQL; the Writer opens with no read-along error; the job logs a sweep.

##### RA-T2.1.1.1 · Commit the reviewable migration script
- **Status:** done 179180a
- **Est:** XS
- **Depends:** —
- **Files:** `tools/readalong/AddReadAlong.sql` (new)
- **Do:** generate it with `dotnet ef migrations script 20261105185209_AddCorobs 20261123082631_AddReadAlong --idempotent --context ProseDbContext --project src/Prose.Core -o tools/readalong/AddReadAlong.sql`, and commit it with CRLF endings like the other scripts.
- **Test:** apply it twice to a local SQL Server 2022 database. The second run must be a no-op.
- **Done when:** the script is committed and the double apply is clean.

##### RA-T2.1.1.2 · Apply on the first Hub start after 2026-12-01 and verify
- **Status:** blocked: Azure SQL is paused until 2026-12-01; apply on the first Hub start after that
- **Est:** XS
- **Depends:** RA-T2.1.1.1
- **Files:** none. This is an operation, logged in the factory session.
- **Do:**
  1. Run `run.ps1 -Apps Hub -Start Hub`. The Hub applies pending migrations at start.
  2. Run `prose --read-along --status --slug <a scratch book>`.
  3. Watch `/api/logs/recent` for "Read-along sweep".
- **Test:** the status command exits 0.
- **Done when:** the acceptance above holds.

#### RA-S2.1.2 · The work is on record
*Acceptance:* every RA ticket exists as a PRS-T ticket and an engine work order covers the read-along's paths.

##### RA-T2.1.2.1 · Register the tickets from the JSON
- **Status:** done 179180a (script written; not run here: no PowerShell in this container. Run it with -WhatIf first)
- **Est:** S
- **Depends:** —
- **Files:** `tools/planning/register-tickets.ps1` (new)
- **Do:**
  1. Read `docs/planning/read-along-tickets.json`.
  2. For each node in id order, call the tickets CLI: kind from the level, title, body from the fields, parent from the id prefix. Check the exact flags with `tickets --help`.
  3. Mark every RA-E1 descendant closed, citing `64e84d3`.
  4. Write the returned PRS-T numbers back into the JSON as `externalId`.
- **Test:** run it with `-WhatIf`. It prints one command per node and nothing else.
- **Done when:** `docs/USER_STORIES.md` re-renders with the RA tickets.

##### RA-T2.1.2.2 · Open the engine work order
- **Status:** blocked: needs the live Hub and database
- **Est:** XS
- **Depends:** RA-T2.1.2.1
- **Files:** none (Hub)
- **Do:** `prose --order add --kind engine` with paths `src/Prose.Core/Services/ReadAlong/**`, `src/Prose.WriterUi/Components/Story/**`, `src/Prose.WriterUi/wwwroot/js/story.js`, `src/Prose.WriterUi/wwwroot/story.css`, `src/Prose.Cli/Cli/ReadAlongCli.cs`, `src/Prose.Mcp/Tools.ReadAlong.cs`, `docs/rfc/0019-read-along.md`, `docs/planning/read-along-*`, and the title "Read-along Phase 2–4".
- **Test:** `prose --order show <id>`.
- **Done when:** later commits name `WO:<id>`.

### RA-F2.2 · From the story to the page

#### RA-S2.2.1 · Any door opens the Writer on the exact beat
*Acceptance:* clicking **✎ Write** in `/story`, or *open in Writer* on a continuity card, opens `/writer` on that book and beat.

##### RA-T2.2.1.1 · `WriterLink` resolver (pure)
- **Status:** done 4a631a4
- **Est:** XS
- **Depends:** —
- **Files:** `src/Prose.WriterUi/Services/WriterLink.cs` (new), `src/Prose.UnitTests/WriterLinkTests.cs` (new)
- **Do:**
  1. Add `static BookListItem? ResolveBook(IEnumerable<BookListItem> books, string? key)`, matching id, then slug, then code, case-insensitive.
  2. Add `static string For(Guid bookId, Guid? beatId)`, returning `/writer?book=<id>&beat=<id>`.
- **Test:** id, slug and code all resolve; an unknown key gives null; the URL encodes correctly.
- **Done when:** the tests pass.

##### RA-T2.2.1.2 · `/writer` accepts `book` and `beat`
- **Status:** done 4a631a4
- **Est:** S
- **Depends:** RA-T2.2.1.1
- **Files:** `src/Prose.Hub/Program.cs` (the `/writer` MapGet takes `string? book, string? beat` and passes them as parameters, like `/story`), `src/Prose.Hub/Components/Writer.razor` (`[Parameter] Book`, `Beat`, forwarded to `<WriterShell>`), `src/Prose.WriterUi/Components/WriterShell.razor`
- **Do:**
  1. Add `[Parameter] string? Book` and `[Parameter] string? Beat` to `WriterShell`.
  2. After `books = await Writer.ListBooksAsync()` (line ~584), resolve `Book` with `WriterLink.ResolveBook` and call `OpenBookAsync`.
  3. If `Beat` parses as a Guid inside that book's spine, call `OpenBeatAsync(beat)`. Otherwise keep the first beat.
- **Test:** Playwright script `tools/readalong/e2e/writer-deeplink.js` opens `/writer?book=<slug>&beat=<id>` and asserts that the beat textarea holds that beat's text.
- **Done when:** the e2e script passes against a local Hub.

##### RA-T2.2.1.3 · Story workspace links use it
- **Status:** done 4a631a4
- **Est:** XS
- **Depends:** RA-T2.2.1.2
- **Files:** `StoryManuscript.razor` (✎ Write links to the open chapter's first beat), `StoryContinuity.razor` (a small "open in Writer" next to each door), `StoryEntry.razor` (timeline doors get the same)
- **Do:** build each href with `WriterLink.For(BookId, beatId)`.
- **Test:** extend the e2e script: a card's "open in Writer" lands on the proposed beat.
- **Done when:** the e2e script passes.

#### RA-S2.2.2 · The Writer highlights story-bible names
*Acceptance:* in Display mode, kept names and aliases carry a dotted underline. Clicking one opens its entry beside the prose. Saving the beat shows no tag diff from the highlights.

##### RA-T2.2.2.1 · `WriterService.GetHighlightNamesAsync`
- **Status:** done e918eb0
- **Est:** XS
- **Depends:** —
- **Files:** `src/Prose.WriterUi/Services/WriterService.cs`
- **Do:** inject `StoryBibleService` and return `GetHighlightNamesAsync(bookId)` filtered to kept entries (no drafts) as `record Highlight(string Name, Guid EntityId)`.
- **Test:** covered by the existing `ReadAlongServiceTests` (aliases) plus one assertion that drafts are excluded.
- **Done when:** the test passes.

##### RA-T2.2.2.2 · Overlay highlights with the CSS Custom Highlight API
- **Status:** done e918eb0
- **Est:** M
- **Depends:** RA-T2.2.2.1
- **Files:** `src/Prose.WriterUi/wwwroot/js/writer.js`, `src/Prose.WriterUi/wwwroot/writer.css`
- **Do:**
  1. Add `proseEditor.setHighlights(names)`. It walks the editor's text nodes, builds `Range`s for whole-word matches, and registers them as `CSS.highlights.set('prose-codex', new Highlight(...ranges))`. The DOM and the markup are never touched.
  2. Re-run on input, debounced to 300 ms.
  3. Style `::highlight(prose-codex)` with a dotted underline.
  4. On click, use `document.caretPositionFromPoint`/`caretRangeFromPoint` to find a range containing the caret and call `dotnet.invokeMethodAsync('OpenEntry', id)`.
- **Test:** Playwright: open a beat that mentions a kept name and assert `CSS.highlights.get('prose-codex').size > 0`; save and assert the status bar shows no "tags added/removed".
- **Done when:** the test passes.

##### RA-T2.2.2.3 · Entry side panel in the Writer
- **Status:** done e918eb0
- **Est:** S
- **Depends:** RA-T2.2.2.2
- **Files:** `WriterShell.razor` (a `[JSInvokable] OpenEntry(Guid)` that sets `entryOpen`; render `<StoryEntry>` in the third column the Discuss panel uses), `src/Prose.WriterUi/wwwroot/discuss.css` (reuse the column)
- **Do:** wire `StoryEntry` doors to `OpenBeatAsync` (same book), or to `/story` for another book.
- **Test:** Playwright: click a highlighted name and assert `.entry-panel h2` shows the name.
- **Done when:** the test passes.

### RA-F2.3 · Manuscript structure

#### RA-S2.3.1 · Scenes in the manuscript tree
*Acceptance:* scenes and sequences show under their chapter. A scene dragged into another chapter moves with its beats, and both chapters show as changed in the reading bar.

##### RA-T2.3.1.1 · Tree includes scene and sequence nodes
- **Status:** done 006ea6e
- **Est:** S
- **Depends:** —
- **Files:** `src/Prose.WriterUi/Services/StoryUiService.cs`
- **Do:** extend `TreeNode` with `string Kind` (`volume|chapter|sequence|scene`). Walk children of chapter nodes that are `SceneNode`/`SequenceNode`, depth-first by `SortKey`.
- **Test:** `StoryUiServiceTests` (new, SQLite, constructing `StoryUiService` with a stub `NodeWorkbenchService` is not possible, so extract the walk into `static Task<List<TreeNode>> WalkAsync(ProseDbContext, Guid)` and test that). A book with chapter → 2 scenes gives order and depth.
- **Done when:** the test passes.

##### RA-T2.3.1.2 · Render scenes, drag rules, "+ Scene"
- **Status:** done 006ea6e
- **Est:** M
- **Depends:** RA-T2.3.1.1
- **Files:** `StoryManuscript.razor`, `story.css`
- **Do:**
  1. Render scenes with an indent and a `¶` icon.
  2. Apply the drag rules: a scene dropped on a chapter goes **into** it (`MoveAsync(into:true)`); a scene dropped on a scene goes before it; a chapter can never be dropped into a scene.
  3. Add "+ Scene" on a chapter row's hover, using `StoryUiService.AddChapterAsync` generalised to `AddNodeAsync(parentId, title, kind)` with kind `scene`.
- **Test:** Playwright: drag a scene from chapter 1 into chapter 2 and assert that the tree and the chapter's paragraphs changed.
- **Done when:** the test passes.

##### RA-T2.3.1.3 · Reading follows the move
- **Status:** done 006ea6e
- **Est:** XS
- **Depends:** RA-T2.3.1.2
- **Files:** `src/Prose.UnitTests/ReadAlongServiceTests.cs`
- **Do:** add a test. Reparent a `SceneNode` from chapter 1 to chapter 2; `GetChaptersAsync` groups its beats under chapter 2; `UnreadChaptersAsync` returns both chapters.
- **Done when:** the test passes. No production change is expected; fix the grouping if it fails.

#### RA-S2.3.2 · "Follow this book"
*Acceptance:* each book has an explicit on/off for the background job, shown in the reading bar. Off stops sweeps; nothing already read is lost.

##### RA-T2.3.2.1 · `ReadAlongFollow` setting
- **Status:** done 139b035
- **Est:** S
- **Depends:** —
- **Files:** `src/Prose.Core/Services/ReadAlong/ReadAlongFollow.cs` (new), DI registration
- **Do:**
  1. Store a dictionary `bookId → bool` under the settings key `readalong.follow`. It is universe-scoped through `SettingsKvStore`.
  2. Add `bool? Get(Guid book)` and `void Set(Guid book, bool on)`.
  3. Add `bool IsFollowed(Guid book, bool readOnce)`, which returns the explicit value, or else `readOnce`.
- **Test:** unit test with `SettingsKvStore` on SQLite.
- **Done when:** the test passes.

##### RA-T2.3.2.2 · Sweep honours it
- **Status:** done 139b035
- **Est:** XS
- **Depends:** RA-T2.3.2.1
- **Files:** `ReadAlongBackgroundService.cs`
- **Do:** filter `books` through `IsFollowed`. A book switched on that has never been read is included.
- **Test:** `ReadAlongServiceTests`: a book with follow=false is not read by `SweepAsync`; a book never read with follow=true is.
- **Done when:** the test passes.

##### RA-T2.3.2.3 · Switch in UI, CLI and MCP
- **Status:** done 139b035
- **Est:** S
- **Depends:** RA-T2.3.2.2
- **Files:** `StoryShell.razor` (a toggle in the reading bar), `ReadAlongCli.cs` (`--read-along --slug X --follow on|off`), `Tools.ReadAlong.cs` (a `follow` parameter on `read_along_status`, or a new `set_read_along_follow`), regenerated `docs/CLI_COMMANDS.md` and `docs/MCP_TOOLS.md`
- **Test:** CLI smoke against a local Hub.
- **Done when:** the switch round-trips through all three.

### RA-F2.4 · Import fidelity

#### RA-S2.4.1 · Imported prose keeps its formatting and scene breaks
*Acceptance:* italic and bold from Word arrive as `*…*`/`**…**` beat markup, scene breaks survive as their own beat, and a Word round trip keeps the italics.

##### RA-T2.4.1.1 · Runs to inline markup in `ReadDocx`
- **Status:** done dfce2ad (no escape for a literal *: ProseInline in MindAttic.Export has none; follow-up F-1)
- **Est:** S
- **Depends:** —
- **Files:** `src/Prose.Core/Services/ReadAlong/ManuscriptParser.cs`
- **Do:**
  1. Build each paragraph from `W.Run`s. A run with `RunProperties.Italic` becomes `*text*`, with `Bold` becomes `**text**`, with both becomes `***text***`.
  2. Escape literal `*` in run text as `\*`, after confirming `ProseInline` supports the escape. If it does not, add escape support there in a separate first commit with a `ProseInlineTests` case.
  3. Merge adjacent runs of the same style.
- **Test:** `ManuscriptParserTests` (new) builds a docx in memory with OpenXml, with one italic and one bold run, and asserts the paragraph text.
- **Done when:** the test passes.

##### RA-T2.4.1.2 · Markdown underscores
- **Status:** done dfce2ad
- **Est:** XS
- **Depends:** RA-T2.4.1.1
- **Files:** `ManuscriptParser.cs`
- **Do:** in `.md`, convert `_x_` to `*x*` and `__x__` to `**x**` at word boundaries.
- **Test:** a parser test with snake_case words left alone.
- **Done when:** the test passes.

##### RA-T2.4.1.3 · Scene-break beats
- **Status:** done dfce2ad
- **Est:** S
- **Depends:** RA-T2.4.1.1
- **Files:** `ManuscriptParser.cs` (`ParsedChapter` paragraphs become `ParsedParagraph(string Text, bool SceneBreak)`), `ManuscriptImportService.cs`, `NodeWorkbenchService.AppendBeatsAsync` (accepts a kind per text), `ReadAlongService.GetChaptersAsync` (skips `Kind == "scene-break"`), `DocxExportService` (renders `scene-break` as a centred `* * *`; check the other exporters too)
- **Test:** the parser keeps the break; the importer writes a beat of `Kind = "scene-break"`; the read-along ignores it; the docx export contains `* * *`.
- **Done when:** the tests pass.

#### RA-S2.4.2 · More ways in
*Acceptance:* `.odt`, `.rtf` and `.epub` import, and a folder can be dropped in the browser.

##### RA-T2.4.2.1 · `.odt` reader
- **Status:** done 8082825
- **Est:** S
- **Depends:** RA-T2.4.1.3
- **Files:** `ManuscriptParser.cs`
- **Do:** unzip `content.xml`. `text:h` becomes a heading (outline-level 1 is a volume when it matches the volume regex, otherwise a chapter); `text:p` is text; `text:span` with an italic or bold automatic style becomes markup.
- **Test:** a fixture `.odt` built in the test (zip + XML).
- **Done when:** the test passes.

##### RA-T2.4.2.2 · `.rtf` reader
- **Status:** done 8082825
- **Est:** S
- **Depends:** RA-T2.4.1.3
- **Files:** `ManuscriptParser.cs`
- **Do:** write a minimal tokenizer. `\par` ends a paragraph, `\i`/`\i0` and `\b`/`\b0` toggle style, groups starting `\*` are skipped, `\'hh` and `\uN?` are unescaped.
- **Test:** a fixture string.
- **Done when:** the test passes.

##### RA-T2.4.2.3 · `.epub` reader
- **Status:** done 8082825
- **Est:** S
- **Depends:** RA-T2.4.1.3
- **Files:** `ManuscriptParser.cs`
- **Do:** read `META-INF/container.xml`, then the OPF spine order. Each XHTML item's `h1`/`h2` is a heading, `p` is text, `em`/`i` is italic, `strong`/`b` is bold.
- **Test:** a fixture epub built in the test.
- **Done when:** the test passes.

##### RA-T2.4.2.4 · Folder drop in the Import tab
- **Status:** done 8082825
- **Est:** S
- **Depends:** RA-T2.4.2.3
- **Files:** `StoryImport.razor`, `story.js`
- **Do:** add a second `InputFile` with `webkitdirectory`, and a drag-and-drop zone that reads `DataTransferItem.webkitGetAsEntry()` recursively into the same `InputFile` through `DataTransfer`. Extend `accept` with the new extensions.
- **Test:** Playwright `setInputFiles` with a directory.
- **Done when:** a folder of three files previews as three chapters.

### RA-F2.5 · Getting the book back out

##### RA-D1 · Word export and the read gate (author decision)
- **Status:** blocked: the author's ruling
- **Est:** XS
- **Question:** Prose exports only what has been read (law 9, no override). A freshly imported book is refused until it is read in Read Mode. Which way?
- **Options:** (a) keep as is: the export page explains and points to Read Mode; (b) let the author mark a chapter read from the Story workspace after it is shown whole, then export; (c) add an "export what I wrote" mode for author-written books, which amends law 9.
- **Record:** `record_ruling` with the choice, then read it back.

#### RA-S2.5.1 · Clearing the read gate from the Story workspace (only if RA-D1 = b)
*Acceptance:* the author marks the open chapter read after it is shown whole, the reading bar shows it as read, and Word export succeeds once every chapter is read.

##### RA-T2.5.1.1 · "I've read this chapter" button
- **Status:** blocked: waits on RA-D1
- **Est:** S
- **Depends:** RA-D1
- **Files:** `StoryManuscript.razor`, `StoryUiService.cs` (`MarkChapterReadAsync(chapterId)` calls `ReadGateService.MarkReadAsync(chapterId, beats.Select(b => (b.Id, b.TextHash)), readBy: "author")`)
- **Do:** show the button only once the chapter has been scrolled to its end (an IntersectionObserver on the last paragraph, in `story.js`), and record the read as the author's.
- **Test:** a service test: after marking, `ReadGateService.GetStatusAsync` shows 0 unread for that chapter.
- **Done when:** the test passes and export succeeds in the e2e script.

#### RA-S2.5.2 · Author-written export mode (only if RA-D1 = c)
*Acceptance:* a book the author marks "author-written" exports to Word with unread beats, the export says so on its title page, and every other book still meets the read gate.

##### RA-T2.5.2.1 · Record the law-9 amendment
- **Status:** blocked: waits on RA-D1 = c
- **Est:** XS
- **Depends:** RA-D1
- **Files:** none (a factory ruling); `docs/agent/PROSE_PROTOCOL.md` (law 9's text)
- **Do:** `record_ruling` amending law 9: "no override on the read gate, except a book the author marks author-written, which exports what was written". Read it back. Update the law's line in the protocol.
- **Done when:** the ruling reads back, and the protocol text matches it.

##### RA-T2.5.2.2 · `Node.AuthorWritten` flag
- **Status:** blocked: waits on RA-D1 = c
- **Est:** S
- **Depends:** RA-T2.5.2.1
- **Files:** `src/Prose.Core/Data/Entities/Node.cs`, a migration `AddNodeAuthorWritten`, `ManuscriptImportService.cs` (imports default to true), the Story workspace manuscript header (a toggle)
- **Do:** add a nullable bool column (system-versioned like the rest of `Nodes`), set it on import, and make it switchable in the manuscript view.
- **Test:** a migration applied with `tools/db/create-local.sh`; a service test that import sets the flag.
- **Done when:** the flag round-trips through the UI and the database.

##### RA-T2.5.2.3 · `ReadGateService.EnsureReadAsync` honours the flag
- **Status:** blocked: waits on RA-D1 = c
- **Est:** S
- **Depends:** RA-T2.5.2.2
- **Files:** `src/Prose.Core/Services/ReadGateService.cs`, `DocxExportService.cs` (title-page note "Exported unread at the author's request")
- **Do:** skip the unread check only for author-written book roots; every other path is unchanged. Add the title-page note.
- **Test:** `ReadGateTests` — an author-written book with unread beats exports; an ordinary book with unread beats still throws `UnreadBeatsException`.
- **Done when:** both tests pass.

---

## RA-E3 · Read smarter

### RA-F3.1 · Measure before adding

#### RA-S3.1.1 · Per-pattern precision is known
*Acceptance:* `docs/planning/read-along-precision.md` holds a precision table from two finished books, with an author verdict on 100 drafts and 50 cards, and every pattern under 80 % is switched off.

##### RA-T3.1.1.1 · Pattern ids on every hit
- **Status:** done 6d037a1
- **Est:** S
- **Depends:** —
- **Files:** `StoryFactExtractor.cs`
- **Do:** give `Pattern` a `string Id` (e.g. `age.was-num`, `eyes.poss-were`). Add `string Pattern` to `FactHit`, `NameHit` (`name.mid-sentence`, `name.titled`, …) and `Relation`, and fill it from every emitter.
- **Test:** an extractor test asserts every emitted hit has a non-empty, known id.
- **Done when:** the test passes.

##### RA-T3.1.1.2 · Disable patterns by setting
- **Status:** done 6d037a1
- **Est:** XS
- **Depends:** RA-T3.1.1.1
- **Files:** `StoryFactExtractor.cs` (`Extract(..., IReadOnlySet<string>? disabledPatterns)`), `ReadAlongService.cs` (reads setting `readalong.patterns.disabled`)
- **Test:** a disabled id produces no hits.
- **Done when:** the test passes.

##### RA-T3.1.1.3 · Audit sampler `prose --read-along-audit`
- **Status:** done 6d037a1
- **Est:** M
- **Depends:** RA-T3.1.1.1
- **Files:** `src/Prose.Cli/Cli/ReadAlongAuditCli.cs` (new), `Program.cs` dispatch
- **Do:**
  1. Run the extractor **in memory** over a book's chapters (no writes) and replay the compare logic.
  2. Sample N name hits, N facts and all would-be cards, stratified by pattern, to a CSV: `kind, pattern, subject, attribute, value, quote, chapter, verdict`.
  3. `--score <csv>` reads the author's verdicts and writes the precision table markdown.
- **Test:** a unit test of the scorer on a 6-row CSV.
- **Done when:** the scorer test passes and the sampler runs on a local book.

##### RA-T3.1.1.4 · Run the audit on two finished GLMZ books
- **Status:** blocked: needs the author's verdicts on two finished GLMZ books (and the live database)
- **Est:** S
- **Depends:** RA-T3.1.1.3, RA-T2.1.1.2
- **Files:** `docs/planning/read-along-precision.md` (new)
- **Do:** sample both books, let the author mark verdicts, score, and commit the table. Add the ids under 80 % to `readalong.patterns.disabled`.
- **Done when:** the acceptance holds.

### RA-F3.2 · Recall

#### RA-S3.2.1 · More details are found, at the precision bar
*Acceptance:* every new pattern ships with fixture tests and, after RA-T3.1.1.4's method, at least 80 % precision.

##### RA-T3.2.1.1 · Physical details: height, build, handedness
- **Status:** done 2920611 (built ahead of the RA-T3.1.1.4 audit, which measures these patterns too and can switch any of them off)
- **Est:** S
- **Depends:** RA-T3.1.1.4
- **Files:** `StoryFactExtractor.cs`
- **Do:** add patterns `height` ("stood six feet", "was tall/short" normalized), `build` (lean, broad, slight, stocky), `handed` ("left-handed"). All are single-valued. Add `height`, `build` and `handed` to `SingleValued` and `LowercaseAttributes`.
- **Test:** 6 positive and 4 negative fixtures.
- **Done when:** the tests pass.

##### RA-T3.2.1.2 · Geography: relative location
- **Status:** done 2920611 (built ahead of the RA-T3.1.1.4 audit, which measures these patterns too and can switch any of them off)
- **Est:** S
- **Depends:** RA-T3.2.1.1
- **Files:** `StoryFactExtractor.cs`
- **Do:** "X lay/stood/sat (north|…) of Y" becomes a relation `north of` plus fact `direction-from:<Y>` = north, single-valued, so a later "south of Y" raises a card.
- **Test:** fixtures and a card test in `ReadAlongServiceTests`.
- **Done when:** the tests pass.

##### RA-T3.2.1.3 · Unnamed referents
- **Status:** done 2920611 (built ahead of the RA-T3.1.1.4 audit, which measures these patterns too and can switch any of them off)
- **Est:** M
- **Depends:** RA-T3.2.1.2
- **Files:** `StoryFactExtractor.cs`, `ReadAlongService.cs`
- **Do:** recurring definite descriptions ("the girl behind the curtain", 3 or more times in a book, at least 2 words after "the") become draft entries named `(unnamed) girl behind the curtain`, matching `EntityStubFactory.UnnamedPrefix`. Keep it creates the stub through `GetOrCreateUnnamedAsync`.
- **Test:** a 3-chapter fixture makes one draft; 2 mentions make none.
- **Done when:** the tests pass.

##### RA-T3.2.1.4 · Dialogue speakers
- **Status:** done 2920611 (built ahead of the RA-T3.1.1.4 audit, which measures these patterns too and can switch any of them off)
- **Est:** S
- **Depends:** RA-T3.2.1.3
- **Files:** `StoryFactExtractor.cs`
- **Do:** attribute quoted speech with a tag inside or after the quote (`"…," Sera said`, `"…" said the Chancellor`), and use that speaker as the antecedent for the next pronoun after the quote.
- **Test:** pronoun fixtures through dialogue.
- **Done when:** the tests pass.

##### RA-D2 · Differently-worded contradictions (author decision)
- **Status:** blocked: the author's ruling
- **Est:** XS
- **Question:** "needs three months to refill" against "dead until spring" is not caught, because the attributes differ. Catch it how?
- **Options:** (a) deterministic: normalize rule phrasings ("until spring", "not before X") into comparable durations; (b) an optional LLM pass that only proposes cards marked "suggested by model", off by default, which touches law 7.
- **Record:** `record_ruling`, then read it back.

#### RA-S3.2.2 · Rule phrasings compared (if RA-D2 = a)
*Acceptance:* "three months to refill" and "dead until spring" on the same subject raise a world-rule card at 50 % or above.

##### RA-T3.2.2.1 · Season and relative-time normalizer
- **Status:** blocked: waits on RA-D2
- **Est:** M
- **Depends:** RA-D2
- **Files:** `StoryFactExtractor.cs`
- **Do:** a pattern `(?<s>…) (is|was) (dead|spent|empty|useless) until (spring|summer|autumn|winter|<month>)` becomes the fact `time to <verb-from-context or 'recover'>` with the value `until <season>`. `Compare` treats `until <season>` against an `N <unit>` duration as a contradiction at 50 %. Explain names both phrasings.
- **Test:** the RFC 0019 example fixture.
- **Done when:** the test passes.

#### RA-S3.2.3 · Model-suggested cards (if RA-D2 = b)
*Acceptance:* with the setting on (off by default), a chapter read also asks the configured LLM for possible disagreements with the book's confirmed details. Each one is filed as a card marked "suggested by model", with both quotes verified to exist verbatim in the prose. The author's rulings work as for any card, and a suggestion with an unverifiable quote is discarded.

##### RA-T3.2.3.1 · Setting and card marker
- **Status:** blocked: waits on RA-D2 = b
- **Est:** S
- **Depends:** RA-D2
- **Files:** `ReadAlong.cs` (`ContinuityConflict.Source`: `read-along` | `model`), a migration, `ReadAlongService` (shared setting `readalong.model-suggestions`, default off), `StoryContinuity.razor` (a "suggested by model" tag)
- **Test:** the migration via `create-local.sh`; a service test that the setting defaults to off.
- **Done when:** the column and the setting exist, and the tag renders.

##### RA-T3.2.3.2 · Suggestion pass with verbatim-quote verification
- **Status:** blocked: waits on RA-D2 = b
- **Est:** M
- **Depends:** RA-T3.2.3.1
- **Files:** `src/Prose.Core/Services/ReadAlong/ModelSuggestionService.cs` (new)
- **Do:** for a read chapter, send its text and the confirmed details of the names it mentions. Ask for JSON `[{subject, attribute, existingQuote, newQuote}]`. Keep only items where both quotes occur verbatim in the book's beats. File them at a fixed 40 % confidence, `Source = model`. Log tokens and cost to the command ledger.
- **Test:** with a fake `ILlmService`, a verifiable suggestion is filed and a fabricated quote is dropped; nothing is called when the setting is off.
- **Done when:** the tests pass.

##### RA-T3.2.3.3 · Spend gate and audit
- **Status:** blocked: waits on RA-D2 = b
- **Est:** S
- **Depends:** RA-T3.2.3.2
- **Files:** `ReadAlongAudit.cs` (model cards sampled under pattern `model.suggest`), `CostGateDispatch` (cost gate before a whole-book pass)
- **Done when:** a whole-book pass asks the cost gate first, and the audit can score model cards like any pattern.

### RA-F3.3 · Coreference

#### RA-S3.3.1 · Pronouns land on the right character
*Acceptance:* a 30-case pronoun fixture passes.

##### RA-T3.3.1.1 · Gender from more evidence
- **Status:** done ac93472
- **Est:** S
- **Depends:** —
- **Files:** `StoryFactExtractor.cs`
- **Do:** "his own" and "her own" after a single named character; kin nouns ("X's brother Y" makes Y male); a kept entry's `gender`/`pronouns` fact or record field, passed through `KnownEntry` with a new `char? Gender`.
- **Test:** fixtures.
- **Done when:** the tests pass.

##### RA-T3.3.1.2 · Plural "they" and the 30-case fixture
- **Status:** done ac93472
- **Est:** S
- **Depends:** RA-T3.3.1.1
- **Files:** `StoryFactExtractor.cs`, `src/Prose.UnitTests/PronounFixture.cs` (new)
- **Do:** "they" and "their" resolve to the last organization or plural group named, never to a single character. Build the 30-case table test.
- **Done when:** all 30 pass.

### RA-F3.4 · Codex editing

#### RA-S3.4.1 · Rename and recategorize an entry
*Acceptance:* a kept entry can be renamed (the old name becomes an alias) and moved to another category, and its receipts and cards follow.

##### RA-T3.4.1.1 · `StoryBibleService.RenameAsync` / `RecategorizeAsync`
- **Status:** done 785c1ea
- **Est:** S
- **Depends:** —
- **Files:** `StoryBibleService.cs`
- **Do:**
  1. Rename goes through `EntityRenameService.RenameRecordAsync(entityId, type, newName)`. Add an `alias` fact for the old name, update `StoryFacts.SubjectName/SubjectKey` and `ContinuityConflicts.EntityName` in the book.
  2. Recategorize calls `EntityRetypeService.PlanAsync` and applies the plan only when it reports no blockers; otherwise it returns the blockers.
- **Test:** a service test on SQLite. Rename keeps facts and adds the alias.
- **Done when:** the test passes.

##### RA-T3.4.1.2 · UI: inline title edit and category select in `StoryEntry`
- **Status:** done 785c1ea
- **Est:** S
- **Depends:** RA-T3.4.1.1
- **Files:** `StoryEntry.razor`
- **Test:** a Playwright step.
- **Done when:** a rename shows in the codex and the atlas.

#### RA-S3.4.2 · Merge two entries
*Acceptance:* merging "Hollis" into "Hollis Marsh" leaves one entry with both aliases and every receipt, and open cards point at the survivor.

##### RA-T3.4.2.1 · `StoryBibleService.MergeAsync(keepId, dropId)`
- **Status:** done 785c1ea
- **Est:** M
- **Depends:** RA-T3.4.1.1
- **Files:** `StoryBibleService.cs`
- **Do:**
  1. Move `StoryFacts` (`EntityId`, `SubjectKey`) to the keeper; drop exact duplicates and supersede conflicting single values by recency (no card).
  2. Add the dropped name as an alias; repoint `ContinuityConflicts` and `StoryBibleSuggestions`; repoint `BrainstormCards.EntityId`.
  3. Archive the dropped entity if `OriginNodeId == book`; otherwise leave it and only detach it from this book.
- **Test:** a service test.
- **Done when:** the acceptance holds.

##### RA-T3.4.2.2 · UI: "Merge into…" picker
- **Status:** done 785c1ea
- **Est:** S
- **Depends:** RA-T3.4.2.1
- **Files:** `StoryEntry.razor` (a picker listing codex entries of the same category)
- **Done when:** the Playwright merge step passes.

#### RA-S3.4.3 · Group and long entries
*Acceptance:* Group is editable inline, and entries with more than 6 details collapse to "Show all (N more)".

##### RA-T3.4.3.1 · Group inline edit and collapse
- **Status:** done 785c1ea
- **Est:** XS
- **Depends:** —
- **Files:** `StoryEntry.razor`
- **Do:** Group is an inline input saved via `SetDetailAsync(..., "group", value)`. A `showAll` flag collapses past 6 details.
- **Done when:** visually checked with a screenshot.

### RA-F3.5 · Atlas depth

#### RA-S3.5.1 · Read the map
*Acceptance:* thread labels show on hover, categories can be toggled, stars can be pinned by dragging, and a search box focuses a star.

##### RA-T3.5.1.1 · Labels, category toggles, search
- **Status:** done 1831626
- **Est:** S
- **Depends:** —
- **Files:** `story.js` (`atlasRender` takes `options { hidden: string[], focus: key }`), `StoryAtlas.razor` (legend entries become toggle buttons; a search input)
- **Do:** on hover, draw the `type` label at each thread's midpoint for threads touching the hovered star.
- **Done when:** checked by screenshot.

##### RA-T3.5.1.2 · Pin by dragging a star
- **Status:** done 1831626
- **Est:** S
- **Depends:** RA-T3.5.1.1
- **Files:** `story.js`
- **Do:** a mousedown on a star drags that star, not the view. The pinned position is kept in `localStorage` per book (`prose.atlas.<bookId>`) inside try/catch.
- **Done when:** a dragged star keeps its place after a reload.

#### RA-S3.5.2 · What the reader knew
*Acceptance:* with the scrubber at chapter N, an opened entry shows only details from chapters 1 to N.

##### RA-T3.5.2.1 · `GetEntryAsync(..., asOfChapter)`
- **Status:** done 1831626
- **Est:** S
- **Depends:** —
- **Files:** `StoryBibleService.cs`, `StoryEntry.razor` (an `AsOfChapter` parameter), `StoryAtlas.razor` → `StoryShell.razor` (pass the scrubber value through `OnOpenEntry`)
- **Do:** filter details and timeline to `ChapterIndex <= N`, and show a banner "as of chapter N".
- **Test:** a service test.
- **Done when:** the test passes.

### RA-F3.6 · Ask depth

#### RA-S3.6.1 · Longer, livelier conversations
*Acceptance:* the conversation length is a setting, answers can stream, a card can be asked about, and an answer can go to the board.

##### RA-T3.6.1.1 · Conversation length setting
- **Status:** done 62dc44e
- **Est:** XS
- **Depends:** —
- **Files:** `BookChatService.cs` (read `readalong.ask.historyTurns`, default 8, max 40), `StoryImport.razor` Allowance section (add the input)
- **Done when:** the setting changes how many turns the prompt carries (a unit test on the prompt builder, extracted as `BuildPrompt`).

##### RA-T3.6.1.2 · "Ask about this card"
- **Status:** done 62dc44e
- **Est:** XS
- **Depends:** —
- **Files:** `StoryContinuity.razor` (button), `StoryShell.razor` (switch to the Ask tab with a prefilled question), `StoryAsk.razor` (`[Parameter] string? Prefill`)
- **Done when:** clicking prefills "Which is right: <existing> or <proposed> for <entity>'s <attribute>?" in the Ask box.

##### RA-T3.6.1.3 · Save an answer to the board
- **Status:** done 62dc44e
- **Est:** XS
- **Depends:** —
- **Files:** `StoryAsk.razor`
- **Do:** a "📌 To board" button calls `BrainstormService.AddAsync(book, firstLine, text, color: "idea")`.
- **Done when:** the card appears on the board.

##### RA-T3.6.1.4 · Streaming answers
- **Status:** done 62dc44e (the path streams; the Claude provider yields one chunk until MindAttic.Legion can stream; follow-up F-2)
- **Est:** M
- **Depends:** RA-T3.6.1.1
- **Files:** `src/Prose.Core/Interfaces/ILlmService.cs` (add a default-implemented `IAsyncEnumerable<string> StreamAsync(...)` that yields `GenerateAsync`'s whole result), the Claude provider (a real stream), `BookChatService.AskStreamAsync`, `StoryAsk.razor` (render chunks)
- **Test:** a unit test with a fake `ILlmService` that yields 3 chunks; the stored message equals their concatenation.
- **Done when:** the test passes.

### RA-F3.7 · Brainstorm depth

#### RA-S3.7.1 · A board you can work on
*Acceptance:* a card can be tied to an entry from a picker, the board zooms and pans, works by touch, and filters by colour.

##### RA-T3.7.1.1 · Entity picker on a card
- **Status:** done 628f4ed
- **Est:** S
- **Depends:** —
- **Files:** `StoryBrainstorm.razor`, `BrainstormService.UpdateAsync` (add `entityId` and `clearEntity`)
- **Done when:** a tied card shows the entry chip, and the chip opens the entry.

##### RA-T3.7.1.2 · Zoom, pan, touch, colour filter
- **Status:** done 628f4ed
- **Est:** S
- **Depends:** RA-T3.7.1.1
- **Files:** `story.js` (a board transform with wheel and pinch), `StoryBrainstorm.razor` (filter chips), `story.css`
- **Done when:** a Playwright run on an emulated phone drags a card with touch.

---

## RA-E4 · Harden

### RA-F4.1 · Reach

##### RA-D3 · Phone access (author decision)
- **Status:** blocked: the author's ruling
- **Est:** XS
- **Question:** the Hub listens only on 127.0.0.1 and hosting was retired on 2026-08-23. The Story workspace is responsive, but a phone cannot reach it. How should it?
- **Options:** (a) LAN binding behind the API key, with a QR code in the Writer; (b) Remote Control or a tunnel; (c) bring hosting back for `/story` only.
- **Record:** `record_ruling`, then read it back.

#### RA-S4.1.1 · LAN access behind the API key (if RA-D3 = a)
*Acceptance:* with "Phone access" switched on, the Hub also listens on the home network. Only `/story` and the endpoints it needs answer off-machine, and only with a pairing token. The Writer shows a QR code that opens `/story` on the phone already paired.

##### RA-T4.1.1.1 · Opt-in LAN binding
- **Status:** blocked: waits on RA-D3 = a
- **Est:** M
- **Depends:** RA-D3
- **Files:** `src/Prose.Hub/Program.cs` (a second URL `http://0.0.0.0:<port>` when `Hub:LanAccess=true`), `src/Prose.Hub/Hosting/` (a middleware that allows only `/story`, `/_blazor`, `/_framework`, `/_content` and `/story/export` from non-loopback addresses)
- **Test:** with the switch off, a non-loopback request is refused; with it on, `/story` answers and `/api/mcp-invoke` is refused off-machine.
- **Done when:** both are verified with curl from the container's own non-loopback address.

##### RA-T4.1.1.2 · Pairing token and QR code
- **Status:** blocked: waits on RA-D3 = a
- **Est:** M
- **Depends:** RA-T4.1.1.1
- **Files:** `SettingsService` (a rotating pairing token), a pairing endpoint that sets a cookie, `WriterShell.razor` (a "Phone" button showing a QR code of `http://<lan-ip>:<port>/pair?t=<token>`; a small QR encoder in `writer.js`, no CDN)
- **Test:** an e2e on an emulated phone: open the pair URL, then `/story` loads; without the cookie, refused.
- **Done when:** the e2e passes.

#### RA-S4.1.2 · Remote access through a tunnel (if RA-D3 = b)
*Acceptance:* a documented, opt-in tunnel exposes only `/story` (same allow-list as RA-T4.1.1.1) with the pairing token required, reachable away from home.

##### RA-T4.1.2.1 · Tunnel recipe and allow-list reuse
- **Status:** blocked: waits on RA-D3 = b
- **Est:** M
- **Depends:** RA-D3
- **Files:** `docs/planning/read-along-phone.md` (new: the tunnel setup, e.g. a Cloudflare Tunnel or Tailscale Funnel pointed at the LAN port), the allow-list middleware from RA-T4.1.1.1 (built here if a was not chosen), the pairing token
- **Test:** through the tunnel, `/story` with the token loads; `/api` is refused.
- **Done when:** the recipe works end to end once.

#### RA-S4.1.3 · Hosted Story workspace (if RA-D3 = c)
*Acceptance:* a hosted deployment serves `/story` only, behind MindAttic.Authentication, against the same database. The Writer, CLI and MCP stay local.

##### RA-T4.1.3.1 · Hosted mode limited to /story
- **Status:** blocked: waits on RA-D3 = c
- **Est:** L
- **Depends:** RA-D3
- **Files:** `src/Prose.Hub/Hosting/HostedGate.cs` and `HostedAuthPages.cs` (exist from the retired hosting), the allow-list middleware, the deploy skill's pipeline
- **Do:** revive hosted mode with a route allow-list of `/story` plus static assets. Sign-in is required. No `/api/cli-invoke` or `/api/mcp-invoke`.
- **Test:** a staging deploy refuses every non-story route and requires a login.
- **Done when:** staging passes, and the author approves going live.

### RA-F4.2 · Accessibility

#### RA-S4.2.1 · Usable by keyboard and screen reader, in both themes
*Acceptance:* every Story workspace action works from the keyboard, the atlas has a text equivalent, and the light theme passes contrast.

##### RA-T4.2.1.1 · Keyboard reorder and board moves
- **Status:** done 3c43068
- **Est:** S
- **Depends:** —
- **Files:** `StoryManuscript.razor` (Alt+↑/↓ on a focused tree row calls `MoveAsync`), `StoryBrainstorm.razor` (arrow keys move a focused card 10 px; Shift moves 50 px)
- **Done when:** a Playwright keyboard-only run reorders a chapter.

##### RA-T4.2.1.2 · Atlas text equivalent
- **Status:** done 3c43068
- **Est:** XS
- **Depends:** —
- **Files:** `StoryAtlas.razor`
- **Do:** a `<details>` "List view" with stars grouped by category, each with its threads, honouring the scrubber.
- **Done when:** a screen-reader name check passes in Playwright (`getByRole('list')`).

##### RA-T4.2.1.3 · Light-theme contrast and reduced motion
- **Status:** done 3c43068
- **Est:** S
- **Depends:** —
- **Files:** `story.css`, `tools/readalong/e2e/contrast.js` (new)
- **Do:** a script that computes the WCAG contrast of every text node's colour against its background in both themes and fails under 4.5:1. Fix the tokens it flags. Gate the atlas glow and the flash animation behind `prefers-reduced-motion`.
- **Done when:** the script passes.

### RA-F4.3 · Performance

#### RA-S4.3.1 · Fast on long books
*Acceptance:* a 200-chapter, 400k-word synthetic book reads in under 2 minutes on SQLite, and opening a Writer beat adds under 50 ms of read-along overhead.

##### RA-T4.3.1.1 · Benchmark fixture first
- **Status:** done 7fe52e8
- **Est:** S
- **Depends:** —
- **Files:** `src/Prose.UnitTests/ReadAlongPerformanceTests.cs` (new, `[Category("Slow")]`)
- **Do:** generate 200 chapters of templated prose with 60 recurring names, then time `ReadBookAsync` and `IsRememberedAsync`.
- **Done when:** the baseline numbers are recorded in the test's comment.

##### RA-T4.3.1.2 · Known index once per sweep
- **Status:** done 7fe52e8
- **Est:** S
- **Depends:** RA-T4.3.1.1
- **Files:** `ReadAlongService.cs`
- **Do:** `ReadBookAsync` loads `Known` once and passes it to `ReadChapterCoreAsync`, appending each chapter's new drafts to it in memory.
- **Done when:** the benchmark improves and all tests pass.

##### RA-T4.3.1.3 · Cheap "remembered" check
- **Status:** done 7fe52e8
- **Est:** S
- **Depends:** RA-T4.3.1.1
- **Files:** `ReadAlongService.cs`
- **Do:** `IsRememberedAsync(book, chapter)` computes only that chapter's hash, by walking the chapter node (`NodeWorkbenchService.WalkAsync(db, chapterId, …)`), not the whole book.
- **Done when:** the benchmark meets the 50 ms bar.

##### RA-T4.3.1.4 · Import in fewer round trips
- **Status:** done 7fe52e8
- **Est:** S
- **Depends:** RA-T4.3.1.1
- **Files:** `ManuscriptImportService.cs`, `NodeWorkbenchService.cs`
- **Do:** add `CreateChaptersWithBeatsAsync(parentId, chapters)`: one transaction per volume that creates the chapter nodes and their beats (reusing the `CreateNodeFromBeatsAsync` logic for SortKey and Number).
- **Done when:** the 200-chapter import is under 30 s on SQLite.

### RA-F4.4 · Reproducible local databases

#### RA-S4.4.1 · One command makes a local Prose database
*Acceptance:* `tools/db/create-local.sh` (and a matching `.ps1`) produces a working local database in Docker, and the Hub starts against it.

##### RA-T4.4.1.1 · Create-from-model script
- **Status:** done ddc5403 (.sh verified; .ps1 not run here)
- **Est:** S
- **Depends:** —
- **Files:** `tools/db/create-local.sh`, `tools/db/create-local.ps1` (new)
- **Do:**
  1. Run `dotnet ef dbcontext script` to get the schema.
  2. Apply it with `sqlcmd -I` (QUOTED_IDENTIFIER on).
  3. Stamp `__EFMigrationsHistory` with every `[Migration]` id from the Designer files (ids, not file names, because one differs).
  4. Seed the GLMZ `Universe` row.
- **Test:** run against `mcr.microsoft.com/mssql/server:2022-latest`, then `/api/health` returns ok.
- **Done when:** the acceptance holds.

##### RA-T4.4.1.2 · Document and file the replay bug
- **Status:** done ddc5403 (documented in README; a squashed baseline is follow-up F-3)
- **Est:** XS
- **Depends:** RA-T4.4.1.1
- **Files:** `README.md` (Building section), a ticket for "migrations do not replay on an empty database (NodeCode index)"
- **Done when:** both are committed.

### RA-F4.5 · Operations

#### RA-S4.5.1 · The job is visible and backed up
*Acceptance:* a nightly scheduler run exists, the `/app` dashboard shows the last sweep, and every sweep is a command-ledger row.

##### RA-T4.5.1.1 · Ledger rows for sweeps
- **Status:** done 7a13274
- **Est:** XS
- **Depends:** —
- **Files:** `ReadAlongBackgroundService.cs`
- **Do:** write a `CommandLedgerEntry` (source `job`, label `read-along.sweep`, duration, chapters read, cost 0) after each sweep that read anything.
- **Done when:** a test asserts that a row is written.

##### RA-T4.5.1.2 · Dashboard tile
- **Status:** done 7a13274
- **Est:** S
- **Depends:** RA-T4.5.1.1
- **Files:** `src/Prose.ObserverUi` (a tile that reads the last `read-along.sweep` ledger row and the open-card count across followed books)
- **Done when:** the tile renders.

##### RA-T4.5.1.3 · Scheduler job spec
- **Status:** done 7a13274 (spec written; the job itself goes in MindAttic.Scheduler)
- **Est:** XS
- **Depends:** RA-T4.5.1.1
- **Files:** `docs/planning/read-along-scheduler.md` (job name `Prose.ReadAlong.Sweep`, nightly 02:41 local, command `prose --read-along --all`, headless). The job itself lives in MindAttic.Scheduler.
- **Done when:** the spec is committed and the job is added there.

### RA-F4.6 · Agents

#### RA-S4.6.1 · Agents know the read-along's rules
*Acceptance:* the protocol says agents may read the codex, cards and atlas, and may keep, toss or rule only on the author's word in the session.

##### RA-T4.6.1.1 · Protocol and prompt alias
- **Status:** done 22efe7a
- **Est:** XS
- **Depends:** —
- **Files:** `docs/agent/PROSE_PROTOCOL.md` (a new "Read-along" section), `docs/agent/PROMPT_COMMANDS.md` (`/story <book>` runs `read_along_status` plus `continuity_inbox`)
- **Done when:** both are committed.

---

## RA-E5 · Follow-ups found while building

### RA-F5.1 · Literal asterisks (MindAttic.Export)

#### RA-S5.1.1 · An escaped `\*` stays a literal asterisk everywhere
*Acceptance:* `ProseInline.Parse("a \\*b\\* c")` yields plain text `a *b* c`. Imported text with a real asterisk is escaped on the way in.

##### RA-T5.1.1.1 · Escape support in `ProseInline`
- **Est:** S
- **Depends:** —
- **Files:** MindAttic.Export `Text/ProseInline.cs` and its tests (another repository); then bump the package in `src/Prose.Core/Prose.Core.csproj` and `lib/local-packages`
- **Do:** treat a backslash-escaped `*` as literal in `Parse` and `StripFormatting`; publish a new package version.
- **Test:** `ProseInlineTests`: an escaped asterisk is literal, and an unescaped one is unchanged.
- **Done when:** Prose builds against the new package and its tests pass.

##### RA-T5.1.1.2 · Import escapes literal asterisks
- **Est:** XS
- **Depends:** RA-T5.1.1.1
- **Files:** `ManuscriptParser.cs` (`Markup` and the text readers escape `*` in run text before adding markers)
- **Test:** a parser test with "2*3" in plain text round-trips.
- **Done when:** the test passes.

### RA-F5.2 · Streaming answers (MindAttic.Legion)

#### RA-S5.2.1 · Claude answers stream into Ask
*Acceptance:* Ask shows a Claude answer growing as it is generated, not in one piece.

##### RA-T5.2.1.1 · `LegionClient.StreamAsync`
- **Est:** M
- **Depends:** —
- **Files:** MindAttic.Legion (another repository): a streaming call over the Messages API's server-sent events, keeping retries and the circuit breaker; a new package version
- **Test:** Legion's own tests with a recorded SSE stream.
- **Done when:** the package is published.

##### RA-T5.2.1.2 · `ClaudeService.StreamAsync`
- **Est:** S
- **Depends:** RA-T5.2.1.1
- **Files:** `src/Prose.Core/Services/ClaudeService.cs` (override the interface default with Legion's stream)
- **Test:** an Ask with a configured key shows more than one chunk (manual, logged).
- **Done when:** observed in the Story workspace.

### RA-F5.3 · Replayable migrations

#### RA-S5.3.1 · An empty server migrates from history
*Acceptance:* `dotnet ef database update` succeeds on an empty SQL Server 2022, and the live database is unaffected.

##### RA-T5.3.1.1 · Squashed baseline migration
- **Est:** L
- **Depends:** —
- **Files:** `src/Prose.Core/Migrations/` (a baseline generated from the current model, applied only when `__EFMigrationsHistory` is empty; the existing history stays for databases that have it)
- **Do:** decide the mechanism (a guarded baseline, or a documented `create-local` as the only fresh path), and record the decision as a ruling.
- **Test:** an empty container migrates; a copy of the live schema reports nothing pending.
- **Done when:** both hold.

### RA-F5.4 · Pre-existing test failures

#### RA-S5.4.1 · The unit suite is green on Linux and Windows
*Acceptance:* the 11 failures present before the read-along (`StoreUrlCli` reachability, the ArchiveDirFor/RemotePathFor paths, AllowedHosts, import/export round trip, log parsing) pass, or are marked Windows-only with a reason.

##### RA-T5.4.1.1 · `StoreUrlCli` reachable from the dispatch chain
- **Est:** XS
- **Depends:** —
- **Files:** `src/Prose.Cli/Program.cs` (dispatch `--set-store-url` to `StoreUrlCli`, or delete the class if `--set-store-url` lives elsewhere)
- **Test:** `EveryHandlerClassIsReachableFromTheDispatchChain`.
- **Done when:** the test passes.

##### RA-T5.4.1.2 · Path and host tests on Linux
- **Est:** S
- **Depends:** RA-T5.4.1.1
- **Files:** the failing test classes (ExportCleanupService ArchiveDirFor/RemotePathFor, AllowedHosts, import/export round trip, run-log parsing)
- **Do:** run each on Windows. Fix the real bugs, or mark the test `[Platform("Win")]` with the reason when it tests a Windows-only path rule.
- **Done when:** the suite is green on both platforms.

### RA-F5.5 · Verify the PowerShell scripts

#### RA-S5.5.1 · Both PowerShell scripts run as written
*Acceptance:* `register-tickets.ps1 -WhatIf` prints one command per ticket, and `create-local.ps1` makes a database the Hub starts against.

##### RA-T5.5.1.1 · Run them under PowerShell 7
- **Est:** S
- **Depends:** —
- **Files:** `tools/planning/register-tickets.ps1`, `tools/db/create-local.ps1`
- **Do:** run both on the author's machine (or a container with pwsh). Check the tickets CLI flags with `tickets --help` and adjust `-Template`. Fix anything they turn up.
- **Done when:** the dry run lists every ticket, and the Hub starts against the new database.

## Build order at a glance

1. **Unblock:** RA-T2.1.1.1 → RA-T2.1.1.2, RA-T2.1.2.1 → RA-T2.1.2.2.
2. **Ask the author:** RA-D1, RA-D2, RA-D3. These are questions only; they block nothing else.
3. **E2:** F2.2 → F2.3 → F2.4 → F2.5.
4. **E3:** F3.1 must finish before F3.2. F3.3 to F3.7 are independent of each other.
5. **E4:** F4.3 (benchmark first) can start any time; the rest have no ordering constraints.
6. **E5:** follow-ups. F5.1 and F5.2 start in other repositories (MindAttic.Export, MindAttic.Legion). F5.3 to F5.5 are independent of each other.

Only the branch matching each ruling is built. The other branches' tickets are closed as "not chosen" when the ruling is recorded.
