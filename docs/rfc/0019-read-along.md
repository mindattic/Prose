---
codex: 1
project: Prose
code: SS
layer: rfc
status: implemented
updated: 2026-11-23
---

# RFC 0019: The read-along — a story bible that keeps itself {#SS-RFC-0019-READ-ALONG}

**Status:** implemented 2026-11-23 · **Builds on:** RFC 0009 (no autonomous prose writes), RFC 0015 (the
Novel Factory: deterministic checks only), RFC 0002 (entity X-ray) · **Surfaces:** the Story workspace at
`/story`, the Writer's status bar, `prose --read-along …`, eleven MCP tools · **Migration:** `AddReadAlong`

> Write first; the engine does the bookkeeping from your prose while you do the only part that matters.

---

## 1. Goal

Prose has always kept canon for books Claude helps write. This RFC makes it do the same for books an author
writes (or already wrote) by hand: **read the manuscript chapter by chapter, the way a reader does, and keep the
books** — a story bible built from the prose with a receipt for every fact, a Continuity Inbox for every place
the story disagrees with itself, a map of the world that can be scrubbed back to any chapter, and a way to ask the
book about itself. **It never writes a word of the book.**

The feature set is the one a pantser (a write-by-the-seat-of-the-pants novelist) needs at chapter two hundred:

| Surface | What it does |
| --- | --- |
| **Import** | One Word document, a folder of them, or `.txt`/`.md`: volumes and chapters found from the headings, one beat per paragraph, the words exactly as written. Preview before anything is written. |
| **Read-along** | Each chapter is read in order against everything before it. A reading bar ("reading your story — 13 of 20 chapters", chips for names just met) and a reading log (who each chapter introduced, how many story details, possible contradictions). |
| **Automatic story bible (Codex)** | Every character, place, item, organization and concept the prose names becomes a **draft** with the sentences behind it. `Keep it` makes it an entry; `Not quite` tosses it for good. Entries carry details (age, eyes, scar, role, kin, what they carry, world rules…), each with **Why?** → the exact quote and chapter. A **Timeline** shows every value a detail has had. Hand-written entries and details are first-class. |
| **Continuity Inbox** | When a newly read line disagrees with one already on the page, a card quotes both, each a door onto its passage, with a kind (contradiction / updated) and a confidence. The author rules: **Accept new**, **Keep existing**, **Retcon**, **Dismiss**, or asks **Explain**. A confirmed fact is never quietly overwritten. A card whose line the author rewrote closes itself as *fixed*. |
| **Story Atlas** | The world as a constellation: stars coloured by category, threads for stated relationships and for names that share paragraphs. Hover to preview, click to open the entry, drag the timeline back to see the world as the reader knew it at chapter N. |
| **Ask your book** | Questions answered from the manuscript with **door chips** to the exact passages. Uses the configured LLM when there is one (told never to write prose), and the read-along's own records when there is not. |
| **Brainstorm canvas** | Free cards (idea, question, plot, character, world) placed by dragging, threaded together or tied to an entry. Never canon, never read by anything. |
| **Writing workspace** | The manuscript tree (volumes, chapters; drag to reorder; add and rename), the open chapter with every story-bible name highlighted, *Saved · Prose remembered this chapter*, words and minutes to read, focus mode, typography, Word export, a phone layout with a bottom bar. The Writer itself gains the remembered tick, the gold ⚠ counter and focus mode. |
| **Allowance** | Optional limits on words read and questions asked (default: unlimited — it is the author's machine). Reaching one deletes nothing; reading simply stops until it is raised. |

## 2. Principles

1. **Never writes prose.** The read-along reads beats and writes only its own tables (and, on *Keep it*, the
   entity the author approved). Ask is a chat pane; its prompt forbids drafting and it has no write path.
2. **Nothing final until the author says so.** Drafts are invisible to the codex, the atlas's kept view and Ask's
   story-bible block until kept. A tossed name or detail is fingerprinted and never raised again.
3. **Receipts or it didn't happen.** Every fact stores the verbatim sentence, the beat and the chapter.
4. **Deterministic.** Extraction and comparison are pattern-based (`StoryFactExtractor`) — Law 7 of the factory
   (no LLM judges). Same book, same bible, every time; and no tokens are spent reading.
5. **The author is the continuity editor.** Cards are questions, never corrections; every ruling is recorded on
   the card.
6. **Opt-in per book.** The background job only follows books that have been read once.

## 3. Reading a chapter

`ReadAlongService.GetChaptersAsync` walks the book in reading order (the same walk as every other reader) and
groups beats by their deepest chapter-type node; volumes are chapter collections (ARCHITECTURE §2c). A chapter's
**content hash** is the SHA-256 of its beats' text hashes, so *remembered this chapter* means *read as it stands*.

`ReadChapterCoreAsync`:

1. loads what the bible holds — every non-archived entity of the universe (so imported prose meets canon it
   already has), confirmed aliases, and pending drafts;
2. runs `StoryFactExtractor.Extract` over the chapter's paragraphs:
   * **names**: capitalized runs, minus sentence starters, stop words and words the chapter also uses in lowercase;
     categorized by title, suffix (`… Tower` → place, `… Council` → organization, `the Siege` → concept, plural
     `the …s` → organization), dialogue/action verbs and prepositions; a first name or titled surname folds into the
     full name as an alias;
   * **details**: age, eyes, hair, scar, role (appositive), birthplace, dead/alive, travelling alone, capabilities,
     aliases, "N years since the Siege", "the gate faced north", "the staff needs three months to refill", the look
     of a possessed item ("her grey cloak"), what a character carries;
   * **relations**: kin ("her brother, Tobin"), feelings and allegiance ("hated", "served"), membership, residence;
   * pronouns resolve to the nearest character earlier in the same sentence, else the last character on the page,
     skipping a character whose gender the prose has already shown to be the other one;
3. drafts new names (`entry`) and new details of kept entries (`detail`), stores every detail as a `StoryFact`
   observation (unconfirmed until kept), and compares single-valued details against the current value from earlier
   chapters (this book's, and the same entity's in any book). A difference files a `ContinuityConflict` with a
   fingerprint, so re-reading never files it twice and a dismissed card stays dismissed;
4. closes as **fixed** any open card whose quoted line is no longer in the chapter;
5. writes the `ChapterReading` (met names, every mention with counts, which names shared a beat).

**Single-valued** attributes (they can conflict): age, eyes, hair, scar, born, status, years-ago, direction,
companion, role, every `time to …` world rule, and the look of each item noun. Everything else accumulates.

**Updated vs contradiction:** an age that rose by ≤ 10 in a later chapter, a "years since" that rose by ≤ 2, and
any role change are *updated* (the story may have moved on); everything else is a *contradiction*. A character
recorded dead who speaks in a later chapter raises a card (a ghost, a flashback, or a slip). A card against a
confirmed fact gets +10 confidence.

## 4. Rulings

| Action | Facts | Card |
| --- | --- | --- |
| Keep it (entry) | entity created (or linked to a same-named one), every gathered detail confirmed | — |
| Keep it (detail) | the detail confirmed; an older confirmed single value is superseded | — |
| Not quite | the draft's observations deleted; fingerprint kept as *tossed* | open cards for that draft dismissed |
| Accept new | old fact superseded, new confirmed | `accepted-new` |
| Keep existing | old fact confirmed (protected), new observation dropped | `kept-existing` |
| Retcon | as Accept new, the new fact sourced `retcon`; the old value stays on the timeline | `retcon` |
| Dismiss | nothing changes | `dismissed` |
| (author rewrites the line) | the chapter's stale observations are replaced on re-read | `fixed` |

## 5. Data (migration `AddReadAlong`)

All rows carry `UniverseId` (stamped like every universe-scoped root) and `BookNodeId` (cascade with the book).
Beat and chapter references are plain columns: a receipt can outlive its beat as a quote.

| Table | Holds |
| --- | --- |
| `ChapterReadings` | the reading log: chapter, index, title, content hash, words, `MetJson` (met, mentions, pairs), detail and contradiction counts |
| `StoryBibleSuggestions` | drafts: kind (`entry`/`detail`), category, name, entity (for details), attributes JSON (value, quote, beat), quote, fingerprint, status (`pending`/`kept`/`tossed`) |
| `StoryFacts` | details with receipts: entity (null while a draft), subject key/name, suggestion, continuity category, attribute, value, quote, beat, chapter, chapter index, source (`read-along`/`author`/`retcon`), confirmed, superseded-at |
| `ContinuityConflicts` | the inbox: entity, kind, category, attribute, confidence, existing and proposed value/quote/beat/chapter, fingerprint, status, note |
| `BrainstormCards` | canvas cards: title, body, x/y, colour, optional entity and linked card |
| `BookChatMessages` | ask-your-book turns, with door citations JSON |

The allowance is the shared setting `readalong.allowance` (`{ wordLimit, questionLimit }`, 0 = unlimited).

## 6. The job

`ReadAlongBackgroundService` (registered with the Hub's background services): every `ReadAlong:IntervalMinutes`
(default 5) it reads, in every followed book, each chapter whose hash changed since its last reading **and** whose
newest beat edit is older than `ReadAlong:QuietMinutes` (default 3) — nothing is read mid-sentence. Off with
`ReadAlong:Enabled=false` or the Hub-wide `BackgroundServices:Enabled=false`. `prose --read-along --all` runs the same
sweep on demand (for an external scheduler such as MindAttic.Scheduler).

## 7. Surfaces

* **`/story?book=<id|slug|code>&tab=manuscript|codex|continuity|ask|atlas|brainstorm|import`** — the Story
  workspace (`Prose.WriterUi/Components/Story/*`, `story.css`, `js/story.js`). `/story/export/<bookId>.docx`
  downloads the book as Word. The read gate still stands (Law 9, no override): a book with unread beats is refused
  with the gate's own message and a pointer to Read Mode — the read-along's reading is bookkeeping, not a read
  receipt, and never counts as one.
* **Writer** status bar: *✓ Remembered this chapter* / *Not read along yet*, the gold **⚠ N** counter (opens the
  inbox), chapter words and minutes to read, **Story** and **Focus**.
* **CLI** (`ReadAlongCli`): `--import-manuscript`, `--read-along`, `--story-bible`, `--continuity-inbox`,
  `--story-atlas`, `--ask-book`, `--brainstorm`, `--read-allowance` — see [CLI_COMMANDS.md](../CLI_COMMANDS.md).
* **MCP** (`ReadAlongTools`): `read_along`, `read_along_status`, `import_manuscript`, `story_bible`,
  `decide_story_bible_draft`, `set_story_bible_detail`, `continuity_inbox`, `rule_continuity_card`, `story_atlas`,
  `ask_book`, `brainstorm`, `read_allowance` — see [MCP_TOOLS.md](../MCP_TOOLS.md). Keeping, tossing and ruling are
  the author's decisions; an agent does them only when the author has said which.

## 8. Limits

* Pattern extraction misses what it has no pattern for (a detail stated obliquely, a character only ever called
  "the girl"). Misses cost nothing — the author adds by hand — and precision is what keeps the bible trustworthy.
* Pronoun resolution is local (same sentence, then the last characters on the page) with gender learned only from
  titles and "She…/He…" right after a single named character.
* A detail is compared only with the same attribute; "dead until spring" vs "three months to refill" (different
  wording of one rule) is not caught.
* Ask without an LLM lists the bible's details and the passages; it does not reason across them.

## 9. Tests

`StoryFactExtractorTests` (names, pronouns, gender, aliases, details, normalization, single-valued attributes) and
`ReadAlongServiceTests` (reading in order, Keep it / Not quite, cards and rulings, world-rule and item-look cards,
fixed-by-edit, the atlas scrubber, Ask with doors and the question limit, the word allowance, the brainstorm
canvas, the manuscript parser) — SQLite, no Hub. The workspace was driven end to end in Chromium against a local
SQL Server 2022 (import → read → keep → rule → door → ask → atlas → brainstorm → phone layout).
