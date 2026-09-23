# RFC 0015 — the Novel Factory: complete system design and 48-hour build

**Status:** APPROVED by the author 2026-09-23 (~05:00 UTC). Build in progress; the live build state is the factory itself (prose --order list, prose --factory next), not this file. This document changes only through an engine work order.

> This is the blueprint. Its first engine order commits it as `docs/rfc/0015-novel-factory.md` and seeds its build tree into the database (`WorkOrders`). From then on, no session works from memory or from this file. Every session is driven by the factory's live state in the Hub.
>
> Revision 2 folds in an adversarial review that checked every claim against the code and the live database. Each defect it found is marked **[RT#n]** where it's fixed.

## 0. Context

Six months and 1,267 commits left a pile of disconnected organs. The author has given a 48-hour deadline, or the project shuts down. They want **a factory that writes novels**: engineered plumbing where every part leans on the others and keeps itself going. It must exist between sessions, record every change, and have no loopholes, no unrecorded ideas and no vagueness.

**What the full history shows:**
- **What worked:**
  - Claude writing in-session through the Hub, then reading it in full and fixing it with exact hand splices. That shipped VIGL, TFAH, RESIST, TRUCE and the Gospels, and produced about 427 kept BCODA fixes.
  - Checks the engine computes from its own state: TextHash, temporal history, and tonight's read gate.
- **What failed:** everything claimed instead of computed.
  - 43 instruments, which produced 8 applied findings out of 30,745.
  - Parallel copies of the story.
  - Services that were never wired in.
  - Autonomous rewriters.
  - Plans that no session loaded.

**The design principle:** every fact the factory relies on is either stored truth (prose, world, read receipts, decisions) or computed from it. Nothing is maintained by hand. Nothing counts because Claude says so. Every change is a logged Hub call or a commit tied to an approved work order.

**Not splitting the book.** The scale problem is work that outlives a session, and it's solved by state in the database. A chapter is a unit of *work*. Reading happens in one continuous pass in order, so a chapter is never a boundary of sight.

**No new technology.** No neural net, no graph database, no schemaless store. SQL Server, EF Core, the Hub and the MCP server stay as they are.

---

## 0.5 State baseline: what works right now, measured before anything is proposed

Measured 2026-09-23 around 04:50 UTC with read-only checks. Every feature in this RFC must close a gap in this table (see the traceability table in §0.6). Anything that closes no gap is out of scope.

| # | Component | State | Evidence |
|---|---|---|---|
| B1 | Hub | **working**, build `cc1e9a687e76`, pid 60496 | `GET /api/health` returned `status=ok` |
| B2 | Deployed code vs source | the live Hub is the pre-removal tree plus the read gate and splice (deployed 22:50). **The source tree is ahead of it and partly broken** | Hub.exe mtime 22:50; the stage-1 agent reports Core, Cli and Mcp build, while Hub, WriterUi, ObserverUi and UnitTests don't (**unverified, re-measure in I0**) |
| B3 | Git | master at `cfaabaa64`. **Nothing from tonight is committed:** 149 tracked files modified or deleted (57 deleted), 31 untracked | `git status --porcelain` |
| B4 | Command ledger | **working and recording every Hub call.** 40 calls in the last 2 hours, all present. **Actor is null in 40 of 40.** No book column | `command_log` |
| B5 | Prose read | **working.** BCODA has 521 beats (the recursive count) | `read_beats`: total 521 |
| B6 | Prose write (splice) | **working** through the CLI; the dry-run guard was verified live | earlier session runs; 13 `BeatSpliceTests` |
| B7 | Read gate | **working, no override.** BCODA 521/521 unread, export refused | a live `--export-node` refusal; 12 `ReadGateTests` |
| B8 | Export pipeline | **working** before the gate; V72 produced at 21:34 | the export folder listing |
| B9 | Entity write | **partial.** `create_character` and `create_weapon` work with read-back (5 records fixed tonight). Behaviour, timeline, cyberware, neural and ancestry fields can't be written. `location` is silently dropped. Lists split on commas | tonight's read-backs; the audit agent |
| B10 | Entity truth | **wrong in places.** Kyle has a mother, severed hands, piezo and Seo as mentor; Pixel has a hand-reattachment hook; War Dog describes another book; age 0 across the cast; random ancestry. BCODA's cast (≥3 tags) is 76 entities across 11 types | `get_character` samples; review queries |
| B11 | Mentions | **incomplete and asynchronous.** 50 of 476 top-level beats have no tags. Mentions are written by a fire-and-forget task | review queries; `NodeWorkbenchService.cs:453/641` |
| B12 | Change signal | **noisy.** `CharacterRepository.Save` bumps `ModifiedAt` even on a no-op save and rewrites every bridge table | `Repositories.cs:217` |
| B13 | Findings | **dead weight:** 15,823 new, 14,914 dismissed, 1 triaged, 8 applied | `findings_stats` |
| B14 | Session start | **injects stale instructions.** SessionStart runs `inject-digest.ps1` (37.6KB, dated 09-04, mandates ledgers, cites a non-existent AMENDMENTS.md), plus `build-prose-mcp.ps1` and `start-prose-hub.ps1`. UserPromptSubmit runs `quickload-on-do.ps1`. PostToolUse runs a WorldValidation test hook for `engine/data` edits. **There is no Stop hook** | `.claude/settings.json` |
| B15 | Session continuity | **12 archived `quicksave.md.00N` files** and `agent-queue.json.001`; no live quicksave | `.prose/` listing |
| B16 | Claude memory | 662 files, 3.2MB; MEMORY.md is 17.7KB (limit about 24KB) | directory listing |
| B17 | MCP server | **stale schema.** Tonight's tools (`splice_beats`, `read_status`, read notes, `read_beats` markRead) aren't visible until Claude Code restarts; the CLI twins work now | the session's tool list |
| B18 | TTS / audiobook | **not working.** No Kokoro module and no `piper.exe`; the last MP3s are from June (ElevenLabs). The audiobook export bypasses the read gate | the review's checks; `NodeWorkbenchService.cs` ≈2995 |
| B19 | Unit tests | **unknown now.** 2,837 passed before the removal edits; not run since | last run 21:57 |
| B20 | Autonomous writer | exists and has been unused since RFC 0009. Known defects: `sceneSoFar` resets every chapter and is capped at 6KB; its facts come from ledgers; `RelationshipContext` is never set | the audit agent |
| B21 | Writer UI, KdpPublish | **unknown.** Not exercised since 09-20 and 08-11 respectively | none (**measure only when needed**) |
| B22 | Book content | good prose, but not read in order since tonight's fixes. A spot check found a real candidate defect: beat #19900 says the katana is "forty years older than he was", which conflicts with the late-2100s Seo date | `read_beats` #19900 |

## 0.6 Traceability: every feature closes a measured gap

| Feature (§) | Gaps closed |
|---|---|
| I0 baseline re-measure | B2, B19, B18, B21 (turn "unknown" into measured) |
| WIP backup plus finishing stage 1 (I0–I1) | B3, B2 |
| `FactorySessions`, SessionStart/Stop hooks (§6) | B14, B15 |
| `WorkOrders` (§2.2) | B15 (queue), plans that live in md files, B3 (every commit tied to an order) |
| Actor stamping plus journal from temporal history (§3.13) | B4 |
| `Rulings` (§2.2, §3.6) | rulings living only in B16 memory |
| `CharacterFieldWriter`, the `location` error (§3.4) | B9 |
| Save no-op fix (§3.5) | B12 |
| `EntityVerifications` / F1 (§3.3) | B10 (proves records were checked) |
| Tag-derived mentions in the gate; `CaptureScanner` part (b) (§3.2, §3.8) | B11 |
| `CaptureScanner` part (a) (§3.8) | new names never captured |
| Audiobook gate; TTS smoke test; audio station (§3.2, I0) | B18 |
| `Exports` / F7 (§2.2) | the version number was never tied to content (B8) |
| `ContextBundleService` (§3.9) | B20 (in-session writing without the reset) |
| `MetricsReport` (§3.7) | measured LLM tics |
| MEMORY.md index rewrite (§6) | B16 |
| **Not built** (no measured gap): new UI, new instruments, graph database, embeddings, autonomous-writer changes, Findings cleanup | B13 stays dead weight, dropped with the PENDING DROP tables after GO |

---

## 1. System architecture

```
                        ┌──────────────── AUTHOR ────────────────┐
                        │ rulings · approves engine roots ·      │
                        │ listens to audio · optional notes      │
                        └──────▲─────────────────────────┬───────┘
                               │ MP3 (if TTS passes I0)  │ words
┌──────────────────────────────┴───┐        ┌────────────▼──────────────────────────────┐
│ CLAUDE CODE SESSION (the worker) │        │ HOOKS (.claude/settings.json)             │
│ writes, reads, fixes, corrects   │◄──ctx──┤ SessionStart  factory-start.ps1  (HTTP)   │
│ records, records decisions —     │◄──1ln──┤ UserPrompt    factory-reminder.ps1 (HTTP) │
│ ONLY through the tools in §5     │──stop─►│ Stop          factory-guard.ps1 (after I1)│
└───────┬───────────────┬──────────┘        │ PostToolUse   memory-nudge.ps1            │
        │MCP            │CLI (prose.cmd)    └──────────────────────┬────────────────────┘
┌───────▼───────┐ ┌─────▼────────┐                                 │ HTTP 127.0.0.1:5900
│ Prose.Mcp →   │ │ Prose.Cli →  │  forwarders only; never touch   │ /api/factory/*
│ HubInvoker    │ │ HubCliClient │  the DB                         │
└───────┬───────┘ └─────┬────────┘                                 │
┌───────▼───────────────▼──────────────────────────────────────────▼───────────────────┐
│ PROSE.HUB — only process that reaches the DB; every call → CommandLedgerEntries      │
│            (Actor now stamped: source + factory session id)                          │
│  FactoryService: stations · status matrix · next action · journal · usage check      │
│   ├ ReadGateService (F5, export gate)       ├ RulingService (F6 zero-tolerance law)  │
│   ├ EntityVerificationService (F1, last)    ├ MetricsReport (book-level tics)        │
│   ├ CaptureScanner (F4)                     ├ ExportRecorder (F7)                    │
│   ├ ContextBundleService (working memory)   ├ WorkOrderService (engine/author)       │
│   ├ CharacterFieldWriter (full write path)  └ FactorySessionService                  │
│   └ NodeWorkbenchService / BeatSpliceService — the one prose door                    │
└───────────────┬──────────────────────────────────────────────────────────────────────┘
┌───────────────▼──────────────────────────────────────────────────────────────────────┐
│ SQL SERVER  prose: Nodes·Beats·BeatNodes (temporal)   world: Entities·Characters+    │
│  bridges·CharacterReadModels·Places·…   senses: BeatReadReceipts·BeatReadNotes       │
│  law: Rulings   factory: EntityVerifications·WorkOrders·FactorySessions·Exports      │
│  record: CommandLedgerEntries · *_History temporal tables · ArchivedBooks            │
└──────────────────────────────────────────────────────────────────────────────────────┘
 Git repo: code only; every commit names a work order.   Exports: ePub\MindAttic\<U>\<CODE>\
```

---

## 2. Data model

### 2.1 Existing tables, reused
| Table | Role | Change |
|---|---|---|
| `Nodes`, `BeatNodes`, `Beats` (temporal) | the prose; its order is the timeline | none |
| `Entities` (ModifiedAt), `Characters` + bridges, `CharacterReadModels`, `Places`, … | the world | **[RT#1]** saves stop bumping `ModifiedAt` when nothing changed (§3.5) |
| `BeatReadReceipts`, `BeatReadNotes` | the senses | none |
| `CanonDocumentSections` | universe laws | none |
| `CommandLedgerEntries` | every Hub call | **[RT#12]** `Actor` is stamped with the source plus the factory session id |
| `*_History` temporal tables | row-level change history | read by the journal (**[RT#12]**) |

`BeatEntityMentions` is **no longer relied on** by the factory or the gate (**[RT#3]**). Mentions are derived on the spot from the beat text (§3.2).

### 2.2 New tables (one additive migration, `AddNovelFactory`)
**`Rulings`: law, not facts** (**[RT#16]**: there is no EntityId, so entity facts live only on entity records).

| Column | Type | Notes |
|---|---|---|
| Id | guid PK | |
| UniverseId | guid | |
| BookId | guid? | null means universe-wide |
| Kind | nvarchar(16) | `law`, `metric` or `incidental` |
| Text | nvarchar(2000) | the author's words, verbatim |
| Pattern | nvarchar(400)? | .NET regex, IgnoreCase, matched on tag-stripped text |
| MaxPer1kWords | decimal(9,3)? | `metric` only |
| Source | nvarchar(64) | `author` or `session:<id>` |
| At | datetime2 | |
| SupersededById | guid? | |

- **Kinds:**
  - `law`: a constraint with a zero-tolerance Pattern (e.g. `AGREED` inside the Coda region, `piezo`, a mother for Kyle), or pattern-less text shown to the writer. Examples of pattern-less text: the Observation Rule, and "Kyle never learns the entity is himself".
  - `metric`: a book-level tic target.
  - `incidental`: a proper name that intentionally has no entity.
- **Invariants:** the Pattern must compile, or the insert is refused. A `metric` needs both Pattern and MaxPer1kWords. A superseded row is inert.
- Reuse of `ProseLessons` was checked and rejected: it has no pattern and no scope, and it holds LLM-derived craft lessons with different meaning.

**`EntityVerifications`: "this record was examined against this book".**
- **Columns:** composite PK (EntityId, BookId), RecordModifiedAt datetime2, MentionsFingerprint char(64), VerifiedAt, By, SessionId.
- **[RT#2]** A record's version is simply `Entities.ModifiedAt`, the same signal the read gate already trusts. There are no per-type serializers. It's correct once §3.5 stops no-op saves from bumping it.

**`WorkOrders`: the build tree and author requests.** Book work is computed, never stored.
- **Columns:** Id, ParentId?, RootApprovedBy? (the author's approval, on roots only), Kind (`engine` or `author`), Title, Detail, PathsJson, ChecksJson, Blocking bit, Status (`open`, `closed` or `abandoned`), OpenedAt, OpenedInSessionId, ClosedAt?, EvidenceJson, CommitHash?.
- **[RT#13] Gaming closed:**
  - An engine order must descend from a root the author approved. The seeded RFC tree counts as approved when this plan is approved. New roots need the author.
  - It closes only when the Hub has validated every check itself.

**`FactorySessions`: sessions as rows.** These replace `.prose/quicksave.md*` and `.prose/agent-queue.json`.
- **Columns:** Id, ClaudeSessionId, StartedAt, EndedAt?, GitHeadStart, GitHeadEnd?, StartActionJson, EndSummaryJson (`{done[], decisions[{text, rulingId|orderId}], next}`).
- **Invariant:** `session_end` is refused while any decision lacks an existing Ruling or WorkOrder id.

**`Exports`: proof of what was pressed.**
- **Columns:** Id, BookId, Format (`docx`, `epub`, `pdf`, `txt` or `mp3`), Version, BookFingerprint char(64), Path, At.
- **[RT#7]** A row is written only after a successful export that passed the gate, stamped with the book fingerprint at that moment.

---

## 3. Components (in `Prose.Core`, executed in the Hub)

**3.1 `BookFingerprint`.** Move `LogicSweepService.ComputeBookFingerprintAsync` (`Audit/LogicSweepService.cs:384`, currently `internal`) to a public static `BookFingerprint.ComputeAsync`. The logic sweep calls the shared copy.

**3.2 `ReadGateService`** (`Services/ReadGateService.cs`, exists). Changes:
- **[RT#3]** EntityChanged derives a beat's mentions from `BeatMarkup.ExtractEntityGuids(beat.Text)` at computation time, instead of from `BeatEntityMentions`. That removes the race with the background tag write (`NodeWorkbenchService.cs:453/641`).
- **[RT#4]** `NodeWorkbenchService.ExportAudiobookAsync` (≈:2995) gets `EnsureReadAsync` as its first statement, and ReadGateTests gets a TestCase for it.
- The note kinds gain `heard`.

**3.3 `EntityVerificationService`** (F1: after the read, written only at the end, never resets reads):
1. `BeginAsync(entity, book)` returns the canonical record, every mention beat's text (tag-derived, in reading order) and a nonce. The nonce is an HMAC over (entityId, bookId, ModifiedAt, mentionsFingerprint, expiry = now + 2h).
2. `CommitAsync(nonce)` is refused if ModifiedAt or the mention beats changed since Begin. Otherwise it upserts the row. It never writes the entity, so it doesn't reset any read.

**F1 for a unit:** every entity tagged in the unit has a verification for this book with RecordModifiedAt = the current ModifiedAt.

**3.4 `CharacterFieldWriter` and cost-visible writes.**
- **`SetFieldsAsync(id, fields)`.** Keys are the snake_case names `get_character` returns. A whitelist covers every writable field: identity, description, psychology, speech_patterns, behavioral, relationships, timeline, cyberware_inventory, neural_abilities, knowledge, conditions, stats, belongings, archetypes, operating_territory, physical_description, genetic_ancestry, ancestry_detail, narration_voice, daily_life, story_hooks, tags, aliases (replace), and the image prompts.
- **Writing:** a value replaces the field, `null` clears it, and an absent key is untouched. Lists are JSON arrays, so commas never split them.
- **Save path:** it saves through `CharacterRepository.Save` (`Data/Repositories.cs:173-238`), then `RefreshReadModelAsync`, and returns the fresh record.
- **`location` is refused loudly,** because it lives in `EntityStateEvents` until §11. `create_character` returns an error, instead of `ok:true`, when it's given `location`.
- **[RT#1] Cost-visible:** every entity write (`set_character_fields`, `create_*` update) first computes N, the beats that are currently read and would become unread. If N > 0 and `confirmUnread` isn't true, it refuses with "this edit un-reads N beats (positions …)". That's deterministic friction, and it is logged.

**3.5 Save no-op fix [RT#1].** `CharacterRepository.Save` (`Repositories.cs:217`) and `EfRepository.Save` (`Data/EfRepository.cs:241`) compare the canonical projection before and after. If nothing changed, they skip the bridge wipe and reinsert, and leave `ModifiedAt` alone. Tests prove that a no-op save leaves ModifiedAt untouched and a real change bumps it.

**3.6 `RulingService`.**
- `RecordAsync` validates the regex.
- `ListAsync`, `SupersedeAsync`.
- `FindViolationsAsync(book)` runs the active `law` patterns over tag-stripped prose, and over the tagged entities' canonical records.

**3.7 `MetricsReport` [RT#10].** This is **book-level**, not a per-chapter station.
- It counts each active `metric` Pattern over the whole book and compares against MaxPer1kWords × bookWords / 1000.
- It gates F7 **only when metric rulings exist**, and those must have Source = `author`.
- The author sees the targets below as part of this plan's approval:

| Metric | Pattern | Max per 1k words | Book cap on 194k words | V72 count |
|---|---|---|---|---|
| em_dash | `—` | 7.7 | ≈1,500 | 2,479 |
| not_fragment | `(?<=^|[.!?]["”’]?\s)Not [^.!?\n]{1,40}\.` | 0.62 | ≈120 | 213 |
| the_way | `\bthe way (a|an|you|someone|people|men|a man)\b` | 0.46 | ≈90 | 151 |
| whole_of | `(that )?was the whole of( it)?` | 0.016 | ≈3 | — |

**3.8 `CaptureScanner` (F4) [RT#9, RT#3].** It has two conditions:
- **(a) Unknown names.** A candidate is a run of 1–4 capitalized words (`\p{Lu}[\p{Ll}'’-]+`, which may join on of/and/the) that:
  - occurs at least twice in the book;
  - appears mid-sentence at least once;
  - is not at the start of a dialogue line;
  - isn't all capitals and isn't non-Latin script.

  It's resolved by matching, case-insensitive with the possessive stripped, against the **book-tagged entities plus universe entities with Status = `canon`** (not the 12,432-row universe, which includes junk aliases), the `incidental` rulings, and a code-level English stop set.
- **(b) Untagged known names.** Any occurrence of a name or alias of an entity tagged elsewhere in this book, without a tag in this beat. That's what makes the gate's entity-change coverage complete. It's fixed with `add_entity_tags` or a re-save.

**Calibration:** the first BCODA run is evidence. If (a) yields more than 150 distinct candidates, the English stop set is widened in code (tested). If F4 isn't green by h18, it ships as **NOT COMPUTED** (shown in the matrix, never faked).

**3.9 `ContextBundleService` [RT#11].** `BuildAsync(book, unit, priorUnits = 1 | all, budgetChars = 400_000)` writes one derived file, `%LOCALAPPDATA%\Prose\factory\context\<slug>.context.md`. It's overwritten every call, sits outside the repo, and is never edited. It contains:
1. the active `law` rulings;
2. the universe's World and Craft canon sections (document types confirmed via `list_canon_document_types` in I6);
3. the canonical records of the unit's tagged entities and places;
4. prior prose, tag-stripped: the previous unit by default, or all preceding units within budget, dropping the oldest first, **never reset at a chapter boundary**;
5. the unit itself: its text, or its planned beats.

It returns a manifest: entity ids and ModifiedAt, the prior range, characters included versus total, what was truncated, and a bundle hash.

- **Healing BCODA** uses one continuous in-order read, so the read *is* the context and the default of 1 is enough.
- **New units** use `all`.

**3.10 `ExportRecorder`.** Runs after a successful export in `DocxExportService.ExportNodeAsync`, `ManuscriptExportService.Export{Pdf,Epub,AudioTxt}Async` and `ExportAudiobookAsync`. It inserts an `Exports` row with the current BookFingerprint.

**3.11 `WorkOrderService`.** `AddAsync` refuses an engine order that doesn't descend from an approved root. `CloseAsync` validates the checks (§5.2), writes the evidence, and closes a parent automatically when all its children are closed. `AbandonAsync(reason)`. `FactoryRepoPath` = `D:\Projects\MindAttic\Prose`.

**3.12 `FactorySessionService`.** `StartAsync` records the start action and returns the last summary and the other active sessions (no end, and a ledger call in the last 30 minutes). `EndAsync` validates decision references.

**3.13 `FactoryService`.** `StatusAsync(book)` is the matrix: stations, blocking orders, a metrics summary and unused tools. `NextAsync(book?)`, `JournalAsync(filter)`, `UsageCheckAsync()`.

The journal **[RT#12]** is `*_History` rows for Beats, BeatNodes, Entities and Characters in the time window, joined by time to the ledger rows and to the receipts, notes, verifications, exports and orders written in that window.

The usage check reads tools marked `[FactoryTool(Since = "yyyy-MM-dd")]`: any with 0 non-test ledger calls 7 days after Since gets an automatic (idempotent) "use or delete" engine order.

---

## 4. The line: stations in execution order, with predicates and invalidation [RT#1 reorder]

| # | Scope | Passes when (computed) | Cleared by |
|---|---|---|---|
| F2 Planned | unit | at least 1 beat, and each has text, or both a Title and a Description | `insert_beat(title, description)` |
| F3 Written | unit | every beat has non-whitespace text and a LastWriteReason | `update_beat_text` / `splice_beats` |
| F4 Captured | unit | CaptureScanner (a) and (b) both = 0 | `create_*` / `add_entity_tags` / `record_ruling(incidental)` |
| F5 Read | unit | `ReadGateService` shows every beat read as it stands | `read_beats(markRead, readBy)` |
| F6 Clean | unit | 0 open `defect` notes and 0 `law`-pattern hits in the unit's prose | `splice_beats` → re-read → `resolve_read_note` |
| F1 Verified | unit | every tagged entity is verified at its current ModifiedAt | `verify_entity_begin` → `verify_entity_commit` |
| F7 Pressed | book | all units F2–F6 and F1; **MetricsReport** passes (when author metrics exist); docx, epub and pdf `Exports` rows at the current BookFingerprint | `--export-node` |
| (A) Audio | book | an mp3 `Exports` row at the current fingerprint | `export_audiobook` (only if I0 passes) |
| (H) Heard | unit | *informational, never gating and not part of GO* [RT#8] | the author's notes, relayed by Claude and marked as a trust point |

**`factory_next`:** the first open blocking order; otherwise units in reading order through F2 → F3 → F4 → F5 → F6 → F1, taking the first station that is actionable. A station whose prerequisite is red is skipped, not reported. Then F7, then A.

**Protocol rule [RT#1]:** during a read pass, entity corrections are filed as `defect` notes, not applied. They're applied as one batch after the pass (the cost-visible write shows N), the affected beats are re-read (the gate lists them), and F1 is verified last.

**What undoes what** (all computed):

| Event | Stations that go red |
|---|---|
| beat edited | F5 at that beat and its seams; F6 re-evaluated; F7 and A (the fingerprint changes) |
| beat inserted, deleted or moved | seams F5, F2/F3 for the new beat, F7, A |
| entity record really changed | F1 for the units that tag it; F5 for the beats that tag it; F7, A. A no-op save changes nothing (§3.5) |
| a law ruling is recorded or superseded | F6 wherever it matches; F7 |
| a new capitalized name, or an untagged known name | F4 for that unit |

---

## 5. Interfaces

### 5.1 Tools
Every tool has an MCP name and a CLI twin, and every call goes to the ledger with Actor stamped. The CLI twins work right after a Hub deploy, without an MCP restart.

| MCP tool | CLI twin | Purpose |
|---|---|---|
| `factory_status(book)` | `--factory status --node X` | the matrix |
| `factory_next(book?)` | `--factory next [--node X]` | the one next action, with its exact calls |
| `factory_context(book, unit, priorUnits?, budgetChars?)` | `--factory context …` | the bundle file plus manifest |
| `factory_journal(since, until?, book?)` | `--factory journal …` | history, ledger and factory rows in the window |
| `record_ruling`, `list_rulings`, `supersede_ruling` | `--ruling add / list / supersede` | law |
| `verify_entity_begin`, `verify_entity_commit` | `--verify-entity begin / commit` | F1 |
| `set_character_fields(id, fieldsJson, confirmUnread?)` | `--set-character-fields --id X --file f.json [--confirm-unread]` | the world write path |
| `work_order_add / list / close / abandon` | `--order …` | orders |
| `session_start`, `session_end` | used by hooks and `/quicksave` | sessions |
| existing, unchanged: `read_beats(markRead)`, `read_status`, `add/list/resolve_read_note`, `splice_beats`, `insert_beat`, `update_beat_text`, `create_*`, `get_*`, `add_entity_tags`, `set_canon_section`, `export_node`, `export_audiobook` | existing twins | |

### 5.2 Work-order checks (validated Hub-side)
- **`commit`:** `git merge-base --is-ancestor <hash> HEAD`, and the changed files match PathsJson.
- **`tests {names[], trx}`:** the TRX shows every named test Passed, with StartTime later than the commit time.
- **`ledger {handler, method?, minCalls}`:** non-test calls since the order opened, from CommandLedgerEntries.
- **`factory {book, station, scope}`:** the station passes.
- **`deploy`:** `/api/health` build differs from the one recorded when the order opened.
- **`author`:** an author-attributed item exists. This is a **trust point** (§8), used only for taste and for approving roots.

### 5.3 Hub HTTP (local only)
- `GET /api/factory/next?format=block|line`
- `GET /api/factory/orders?status=open&kind=engine`
- `POST /api/factory/session/start` and `POST /api/factory/session/end`
- `GET /api/factory/status?node=`

---

## 6. Session lifecycle and hooks [RT#13, RT#14, RT#17]

| Hook | Script | Behaviour | Failure mode |
|---|---|---|---|
| SessionStart | `.prose/hooks/factory-start.ps1` | Reads stdin `session_id`. POSTs session/start, GETs next?format=block. Prints the `FACTORY NEXT ACTION` (with id), `LAST SESSION`, `OTHER ACTIVE SESSIONS`, and the laws plus forbidden list | Hub unreachable: "FACTORY UNREACHABLE — src\tools\deploy-apps.ps1 -Start Hub; do no story work from memory". Exit 0. 10s timeout |
| UserPromptSubmit | `.prose/hooks/factory-reminder.ps1` | One line from next?format=line, cached 60s in `%TEMP%` | Hub down: "factory unreachable". 5s timeout |
| Stop (installed after I1) | `.prose/hooks/factory-guard.ps1` | (1) `git status --porcelain --untracked-files=all`: every changed or `??` repo path must match PathsJson of an open engine order that descends from an approved root. (2) A scan of the session transcript's (`transcript_path`) Bash/PowerShell tool inputs for `sqlcmd` with UPDATE, INSERT or DELETE. A violation writes stderr and exits 2 | Hub down: warning, exit 0. Honours `stop_hook_active`. Blocks once per distinct violation set (hash in `%TEMP%`), then warns: no loops |
| PostToolUse (Write\|Edit) | `.prose/hooks/memory-nudge.ps1` | For a memory-directory path, injects: "Canon belongs in the world: record_ruling / set_character_fields + read-back." | none |

- **Removed:** `inject-digest.ps1` (the stale 37.6KB digest) and `quickload-on-do.ps1`.
- **Kept:** `start-prose-hub.ps1` and `build-prose-mcp.ps1`.
- **Redefined:** `/quicksave` calls `session_end`. "do" / `/quickload` is the start summary. `/queue` is `work_order_add(kind: author)`.
- **Archived:** `.prose/quicksave.md*` and `agent-queue.json*` go to `.prose/archive/`.
- **MEMORY.md** is rewritten as a ≤6KB index: a pointer to the factory, the collaboration feedback, and the gotchas. The remaining index lines go to ARCHIVE_INDEX.md. The 662 files aren't curated; they're simply no longer indexed.

---

## 7. Protocols (the heartbeat)
- **A. Author ruling intake.**
  1. If it's law, style or a metric: `record_ruling` (verbatim, with a pattern where mechanical).
  2. If it's an entity fact: write it to the record (cost-visible), confirm the read-back, then verify begin → commit (after the read, if one is in progress).
  3. Reply with the ids and the read-back.
- **B. Heal a book** (one continuous in-order pass):
  1. `factory_next`.
  2. For each unit in order: `read_beats(markRead)`, checking the text against the bundle's records and law.
  3. Prose defects: `add_read_note`, then `splice_beats` (dry run, then apply), re-read, `resolve_read_note`.
  4. Entity errors: a `defect` note only (the record stays frozen during the pass).
  5. Capture: fix F4 (tags and entities).
  6. After the pass: batch the entity corrections (cost-visible), re-read the listed beats, verify every tagged entity (F1).
  7. Export (F7).
- **C. Create a book:**
  1. `create_book` and its chapters.
  2. `insert_beat(title, description)` (F2).
  3. `factory_context(priorUnits: all)`, then write in-session through `update_beat_text` (F3).
  4. Capture (F4: new entities through protocol A, or incidental rulings).
  5. Then protocol B.
- **D. Engine change:**
  1. `work_order_add` under an approved root, with paths and checks.
  2. Edit, `dotnet test --logger trx`, commit with `WO:<id>`, deploy if Hub code changed.
  3. `work_order_close` (Hub-validated).
  4. Real use (the `ledger` check).
- **E. Press and listen:** F7, then A (if I0 passed). The author listens; their notes are relayed as `heard` or `defect` notes marked relayed.
- **F. Session end:** `/quicksave` calls `session_end`, which is refused until every decision references an id.

---

## 8. Enforcement matrix
| Failure mode | Mechanism | Strength |
|---|---|---|
| forgets between sessions | SessionStart injects the live next action and last summary from the database | enforced |
| stale instructions | digest and quickload hooks removed; PROSE_PROTOCOL / commit.md fixed; review-node / completeness / storm removed; MEMORY.md index ≤6KB | enforced |
| builds unused or unplanned code | Stop guard (paths must match an order under an approved root); order close needs real ledger use; usage check opens delete orders | enforced (roots are author-approved) |
| changes unrecorded | story writes only through the Hub (ledger plus temporal history); repo writes need orders and commits; raw `sqlcmd` writes block the Stop hook; the journal reconstructs everything | enforced; Writer-UI edits are visible through temporal history |
| claims done | stations computed; order checks validated Hub-side | enforced |
| decisions left in chat | `session_end` refuses unreferenced decisions; memory nudge | enforced at the boundary |
| ruling drift | no entity facts in Rulings; law patterns at F6; records verified at F1 | enforced |
| writer blind | context bundle, never reset at a chapter boundary (tested) | enforced |
| unwritable or silent entity fields | whitelist coverage (reflection test); `location` refused loudly | enforced |
| accidental mass unread | cost-visible writes with `confirmUnread` | enforced friction |
| gate bypass | no override; audiobook export gated too | enforced |
| machine prose | book-level metrics gate F7 (author-approved) | enforced |
| LLM judging LLM | none in the factory; forbidden in the start banner; wiring test asserts no LLM dependency in `FactoryService` | enforced |

**Trust points, stated rather than hidden:**
1. A read receipt certifies delivery, not understanding.
2. A verification certifies that a record was examined against delivered beats, not that it is correct.
3. Author-attributed items (root approvals, heard notes) are relayed by Claude.

**Mitigations:**
- deterministic law, capture and metrics;
- the author's ear;
- the journal, which the author can audit;
- every item carries a session id and is labelled `relayed`.

---

## 9. Tests (in `src/Prose.UnitTests`, SQLite fixtures in the style of `ReadGateTests`)

**Existing, kept:** `ReadGateTests` (plus TestCases for the audiobook gate and tag-derived mentions), `BeatSpliceTests`, `WriterFunnelTests`.

**New:**
- **`FactoryStationTests`:** a pass and a fail fixture for each of F1–F7 and A, plus every invalidation row, plus the `factory_next` skip-when-blocked rule.
- **`SaveNoOpTests`:** a no-op save keeps ModifiedAt; a real change bumps it (characters and EfRepository types).
- **`EntityVerificationTests`:** the happy path; refused when ModifiedAt changed; refused when mentions changed; refused on an expired nonce; commit doesn't touch the entity.
- **`CharacterFieldWriterTests`:** reflection coverage; round-trip; null clears; absent keys untouched; commas kept; `location` refused; cost-visible refusal when N > 0.
- **`RulingServiceTests`:** a bad regex is refused; violations are found in prose and in records; superseded rulings are inert.
- **`MetricsReportTests`:** each seed pattern gives the exact counts on fixtures; the book threshold.
- **`CaptureScannerTests`:** (a) the rules for twice, mid-sentence, dialogue-initial, all-caps, non-Latin, incidental, canon-only matching; (b) an untagged known name is flagged.
- **`ContextBundleTests`:** prior prose across chapter boundaries; budget drops the oldest first; the manifest is complete.
- **`WorkOrderServiceTests`:** commit in a temporary git repo; TRX fixture; ledger counts; factory check; deploy; approved-root enforcement; parent auto-close.
- **`FactorySessionTests`:** `session_end` gives 422 on an unreferenced decision.
- **`ExportRecorderTests`:** a row is written with the fingerprint; F7 flips when the prose changes.
- **`FactoryWiringTests` [RT#15, narrowed]:** every station is registered and has both a pass and a fail test; the `settings.json` hooks exist and no digest hook is present; `FactoryService`'s dependency graph contains no `ILlmService`.

**Hooks** are thin wrappers over tested endpoints. Each is proven by its run output in §10 (**[RT#14]**: no `claude -p` grading).

---

## 10. Build (incremental, used immediately; hours from approval)

**The rule:** each increment is one engine order. It isn't done until its checks validate, **including real use on BCODA**. Never build more than one increment at a time.

| Inc | Hours | Builds | Real use (the proof it's used) | Checks |
|---|---|---|---|---|
| **I0** | h0–h2 | **nothing. Measure the baseline first** (the author: "find the working and state before you start suggesting features") | **Safety:** a WIP backup of the uncommitted tree. `git switch -c backup/wip-2026-09-23`, then `git add -A && git commit -m "WIP snapshot before factory"`, then `git switch master`, then `git checkout backup/wip-2026-09-23 -- .` **[RT#5]**. **Re-measure every "unknown" and "unverified" baseline row:**<br>• B2: `dotnet build` on each project, recording errors per project<br>• B19: `dotnet test --logger trx`, recording pass/fail counts<br>• B18: TTS smoke test (Kokoro install, or the ElevenLabs key and credits; one real chapter to MP3)<br>• B5: `read_status` beat count<br>• B21: open the Writer UI once and run a KdpPublish dry listing, only if I9 or a publish is attempted<br>**Update §0.5 with the measured values** | the backup branch exists and the master tree is identical to it; every B-row has a measured value and its command output (backfilled as evidence into the I2 order). **If a measurement contradicts this RFC, fix the RFC before building** |
| **I1** | h1–h6 | finish removal stage 1 (the list is in `C:\Users\ryand\.claude\plans\immutable-enchanting-lighthouse-agent-ae76b46847a16e8e8.md`). **Stage 2 is deferred until after GO** **[RT#6]** | full build, full tests, commit, deploy | 0 build errors; 0 test failures; `git status` clean; `/api/health` build changed |
| **I2** | h6–h10 | migration `AddNovelFactory`; `WorkOrderService`; `FactorySessionService`; `FactoryService` (F2, F3, F5); the §5.3 endpoints; tools and CLI twins; the SessionStart and reminder hooks (the Stop guard after this commit); Actor stamping in Cli/ToolDispatch; retire digest, quickload and queue; fix PROSE_PROTOCOL, commit.md and skills; rewrite the MEMORY.md index; commit this RFC | **seed the build tree** (I0–I8 plus leaves) as WorkOrders under the approved root; `factory_status BCODA` (F5 red for every unit); one real session through start → end | TRX green; running `factory-start.ps1` prints a next-action id and the forbidden list; the guard exits 2 on a temporary `FooCli.cs` and 0 once it's matched |
| **I3** | h10–h13 | `RulingService`, `MetricsReport`, F6 | record **every author ruling** (enumerated from the memory files, each as a law or an entity fact routed to its record list for I4) and the 4 metric rulings; run F6 and metrics on BCODA | `list_rulings` count equals the enumerated count; F6 and metrics reports produced |
| **I4** | h12–h17 | the save no-op fix; `CharacterFieldWriter` plus cost-visible writes; `EntityVerificationService`; F1; the `create_character` location error; the audiobook gate; tag-derived mentions in the ReadGate | **world pass before any reading** **[RT#1]**: every cast entity (≥3 BCODA tags, 76 entities across 11 types) is corrected against its mention beats. That covers Kyle (mother, hands, piezo, Seo as mentor), Pixel, War Dog, Chen ("Mrs." alias, random ancestry), and age 0 across the cast. No verifying yet | the law scan over records = 0 hits; per-entity read-backs are in the journal |
| **I5** | h16–h19 | `CaptureScanner`, F4 | the BCODA calibration run; resolve names and tags | the calibration evidence; F4 green, or NOT COMPUTED |
| **I6** | h18–h21 | `ContextBundleService`, `ExportRecorder`, F7, journal, usage check, `BookFingerprint` moved | the bundle for BCODA unit 1; the journal of the I2–I5 sessions | ContextBundleTests; the journal reconstructs I4 |
| **I7** | h21–h40 | none (the heartbeat only) | **heal BCODA through protocol B:** one in-order pass → batch entity fixes → re-read the listed beats → verify all → export V73 | `factory_status BCODA`: F2–F6 and F1 green for every unit, and F7 green; V73 exists |
| **I8** | h30–h42, **only if I7 is past unit 30 by h30** | none | **a new life:** a 3-chapter GLMZ story through protocol C | its `factory_status`: F1–F7 green; the journal shows every step |
| **I9** | h38–h46, **only if I0 passed** | none | audio: BCODA Ch1–3 and the new story (A) | mp3 `Exports` rows exist; the files play |

**Stop rules:**
- A station not green by its increment's end is marked **NOT COMPUTED** in the matrix. It is never faked, and the work continues by protocol, by hand.
- **Metrics not met by h36:** the author decides whether to supersede them. Metrics never block the read.

**GO at h48:** I1–I7 closed with validated checks, and `factory_status BCODA` green through F7, with V73 pressed through an unoverridden gate. I8 and I9 count if their conditions are met.

**NO-GO:** the factory's own status and journal are the report. No new systems.

---

## 11. After GO (each becomes a branch of engine orders under an author-approved root)
- **Removal stage 2:** the ledger code and the writer's feeds from it.
- **One canonical store per entity:** fold `EntityStateEvents` (location) and `WoundLedger` into Character; lift the location refusal.
- **One drop migration** for PENDING DROP tables, after a backup and with the author's consent.
- **The autonomous writer:** rebuilt on `ContextBundleService`, or deleted, decided from I8's evidence.
- **F4 precision tuning** if it shipped NOT COMPUTED.
- **More books through the same line.** The world grows by capture, which is how the factory remembers characters, motivations and events across books.

## 12. Verification (end to end)
1. `dotnet build`: 0 errors. `dotnet test`: 0 failures across §9. The TRX files are attached to their orders.
2. The Hub `/api/health` build changes after each deploy.
3. `factory-start.ps1` output shows the Hub's next-action id and the forbidden list. The guard blocks unmatched repo changes and raw `sqlcmd` writes.
4. `prose --factory status --node BCODA` shows every unit green through F7, and the V73 files exist with an `Exports` row at the current fingerprint.
5. `prose --factory journal --since <I1 start>` reconstructs every change: history rows, ledger calls and commits.
6. Every I1–I7 work order is closed with Hub-validated evidence.

## 13. As built: where the build differs from the text above

Each note below records a decision made while building an increment, with its reason, so this document stays the design that is actually running.

**I4 (the world true)**
- **The no-op guard covers every relational repository**, not just the two §3.5 names. All 29 `Save` overrides in `Repositories.cs` open with `SaveGuard.IsUnchanged`: the item is compared with the repository's own `LoadOne` projection, and with the `Entities` row's name and (where that save syncs it) description. A unit test fails the build if an override loses the guard. The base `EfRepository.Save` compares its JSON blob.
- **`set_character_fields` uses JSON Merge Patch (RFC 7396).** Objects merge one level at a time, so changing `behavioral.habits` leaves `behavioral.decision_rules` alone. Lists and plain values replace. `null` resets a member to empty and removes a dictionary key. Every write is read back: a field that did not land makes the call fail, never return ok.
- **F1 waits for the full read.** While any beat of the book is unread, F1 reports `waiting`, not `fail`, and `factory_next` goes on to the reading. This is how [RT#1]'s deadlock is avoided in the computed line itself, not only in the protocol.
- **F1 includes the mentions fingerprint.** A verification is current only while the record's `ModifiedAt` *and* the fingerprint of the beats that tag it (which beats, at which text) both match. Editing a beat therefore voids the verification of every entity that beat tags.
- **Commit also requires the reading.** It is refused while any mention beat is unread. That makes a verification mean "the record was delivered after its beats were read", not only "the record was delivered".
- **Records are held to the law.** `law` patterns now bind the canonical records of the entities a book tags, as well as the prose (`--ruling violations --records`, MCP `record_law_violations`). `verify_entity_commit` is refused while the record breaks one.
- **A new ruling kind, `page-law`,** binds the prose only. It is for facts the world holds but the page never says, such as Seo making Silence, or Mrs. Chen's own child. Its pattern would otherwise flag the very records that are meant to hold the fact.
- **Sessions resume on compaction.** A second SessionStart under the same Claude session id resumes that session's row. Before this, it opened a new row and reported the session to itself as "another session may be working in this tree".

**I5 (capture)**
- **F4 uses the save path's own scan**, not a new one. Part (a) is `EntityMentionScanner.FindUnresolvedProperNouns` over the tag-stripped text. Part (b) uses the same candidate index, plus the tags a save pins, restricted to entities the book already tags. So F4 can never disagree with what a save would tag.
- **Part (a) lives within its limits.** A name counts only once it is used in at least 2 beats. Words capitalized only by their format (Street, Avenue, North, weekday plurals and similar) are a stop set in code, with tests. No threshold was widened to make the numbers pass: every residue from the BCODA calibration run was resolved by an alias, an `incidental` ruling or a named pin.
- **A name the universe makes ambiguous is resolved from the book.** When the book already tags that exact surface as one entity (`BookSays`), `--pin` extends that tag to the untagged uses. `--pin-name "<name>" --entity <id>` is the named decision that one surface means one entity throughout the book. All of these are tag-only saves through the one door (`TagMaintenance`).
- **Memoized.** The scan is cached against a fingerprint of its inputs: beat ids and text hashes, the entity table's stamp, and the incidental rulings. That makes it cheap enough to run inside every `factory_status`.
- **Name drift is a question, not an alias.** When one person carries two names (Brennan Parr and Brennan Drum, Ezra Fonu and Ezra Vance), capture files a read `question` for the author. An alias would have hidden the contradiction.

**I6 (working memory and the press)**
- **What the bundle's canon is.** glmz holds WorldMaster, WorldBible and UniverseCraft. UniverseCanon is Entos's. The shared CraftGuide, CharacterDoctrine and DelightGuide are read from `Universe.SharedId`, because UniverseCraft's sections extend them. EngineGuide and Franchise are left out. The WorldBible still carries about 40KB of the old engine's architecture (§4, §11–14), which a writer doesn't need. Moving it is the author's call, not a section filter the bundle would have to maintain.
- **The unit's own text is placed first in the budget.** The prose before the unit gets what is left, newest first. A unit that fits only partly keeps its tail, marked `…`. The manifest states what was dropped.
- **Presses are recorded for every format**: docx, epub, pdf, txt, and mp3 or wav. Station A is book-level: audio of any node of the book at the current fingerprint passes it. Per-node audio coverage would need a `NodeId` on `Exports` (after GO).
- **The journal reads SQL Server's temporal history for `Beats` and `Entities`.** A record's field-level history stays with `EntityHistoryService`. On SQLite the journal says it could not look. With a book, a ledger call is attributed to it only when its arguments name the book's slug, code or id. A call that named only a chapter is not attributed, and this limit is stated here rather than guessed around.
- **The usage check.** Each MCP `…Impl` carries `[FactoryTool(name, since, Cli = …)]`. A call through either door counts as use; failed calls and test actors do not. The check runs at every session start. It files "Use or delete: <tool>" under the open RFC 0015 root once per tool, identified by title. The order closes only by a ledger check, or is abandoned with the commit that deleted the tool. A wiring test fails the build if any `Impl` on a factory tool class lacks the attribute.
