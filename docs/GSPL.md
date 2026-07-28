---
codex: SS
project: StreetSamurai
code: GSPL
layer: universe
universe: gspl
status: live
tier: series
scope: GSPL
triggers: Gospel, GSPL, history vs heritage, New Testament, Canaan, apostles, Pilate, Pontius, Herod, Quirinius, genealogy, nativity, Jesus, gospel claims
updated: 2026-07-26
related: docs/CRAFT.md, docs/gospel/README.md
---

# GSPL — Universe Craft Rules {#SS-GSPL}

> **Scope: GSPL universe stories only.** Universal prose rules live in **docs/CRAFT.md** (Base
> layer) — clarity standard, scene architecture, sentence/voice rules all apply here unchanged.
> This file adds the one thing GLMZ and SCRY don't need: **every factual claim must be true to
> a verifiable source**, because unlike GLMZ/SCRY, GSPL's subject is the real world. The
> research method this doctrine enforces is documented in full in
> [`docs/gospel/README.md`](gospel/README.md) — read that first; this file is the prose-craft
> translation of it.

---

## 0. What GSPL is {#SS-GSPL-0}

**Gospel: History vs. Heritage** is an entertaining, readable examination of the New Testament
— what scripture says, set against what the independent historical and archaeological record
says, and what the whole range of serious scholarship in between says. It is a book meant to be
read and enjoyed, not a reference ledger. The research (claim → spectrum → evidence → gap →
sources) is the **grounding material** a beat is written from — never the beat itself. A
finished beat is prose: a scene, an anecdote, a turn of argument, a moment of "wait, actually"
— not a table.

**Not a debunking project, not an apologetic one.** The purpose (§1 of `docs/gospel/README.md`)
is to supply the context needed to read scripture accurately — the "George Washington's
dentures" standard: an unflattering, verifiable fact omitted from the popular version is not
neutral, it's heritage substituting for history. GSPL prose should leave a reader of any faith
position, or none, able to trust that what they just read is accurate.

---

## 1. The Citation-Grounding Rule (GSPL addition — no equivalent in GLMZ/SCRY)

**Every factual claim in a beat — a date, a name, a place, a document, an artifact, a quoted
position — must be traceable to a citation in that beat's grounding research doc.** GLMZ and
SCRY invent their world; GSPL reports on this one. A beat that asserts a fact with no traceable
source is not evocative prose, it's an error, full stop.

- Never invent a page range, publisher, year, or specific detail that hasn't been verified
  against a real source. An unverified-but-plausible-sounding specific is a worse failure than a
  visible gap — the same hard rule as `docs/gospel/README.md` §6, restated here because it binds
  prose, not just the research doc.
- When the grounding research doesn't know something (the Gap Table's "Open Questions"), the
  prose should render that as genuine, interesting uncertainty — not paper over it with an
  invented specific to make the scene feel more complete. Uncertainty, written well, is itself
  a source of narrative tension ("no one knows what happened to Pilate after Rome recalled him
  — and two different churches invented two opposite endings for him, centuries apart, for two
  entirely different reasons") — use it, don't hide it.
- A citation-grounding check (mirroring the existing quote-grounding guard, `ss --verify-quote`)
  should run on GSPL beats before they're considered done: every specific factual claim traced
  back to the grounding doc's Sources section.

### 1a. Numbered Notes (the reader-facing citation format)

**Every factual claim in finished GSPL prose carries an inline number in square brackets** —
e.g., "... the prefect's actual title, contemporary records confirm [37], was..." — resolved in
a dedicated **Notes chapter**, not inline parenthetical author-dates and not a per-chapter
"Notes" beat.

**Bracket convention (do not mix these up):** scripture verse references use parentheses,
`(1:2)`; Note citations use square brackets, `[12]`. The two numbering schemes look similar
enough (`(11)` vs `[11]`) that using the same bracket style for both is a real ambiguity, not a
stylistic nitpick — a reader can't tell a verse reference from a citation number at a glance
unless the brackets differ.

- **Numbering is one flat sequence across the whole work** — plain `(1)`, `(2)`, `(3)`...,
  assigned once and never renumbered or reused, continuing straight through every book in
  canonical order. By Luke or John this climbs into the hundreds or thousands, and that's fine —
  it's a growing reference list, not a display a reader scans end to end. (Superseded
  2026-07-26, twice: a first pass tried book-prefixed codes like `MTW-16`/`MRK-1`; dropped
  because the prefix added a lookup layer without solving anything a plain running number
  doesn't already solve on its own.)
- **One number per claim instance, not one per source.** If the same source (say, Ehrman, 2006)
  supports five different claims across the book, each gets its own number with the specific
  page/detail relevant to that claim — not one shared number reused five times. Standard
  "Notes" convention for annotated nonfiction, not a Vancouver-style numbered bibliography,
  because each claim's specific locator (page, verse, inscription line) usually differs even
  when the source doesn't.
- **This applies to every claim type** — modern scholarship, ancient primary sources (Josephus,
  Tacitus, Philo), and scripture references alike all get a number and a Notes-chapter entry.
  This supersedes the in-line book:chapter:verse convention used in the *grounding research
  docs* under `docs/gospel/` (those remain APA-style research documents, unchanged) — the
  numbered-note convention applies specifically to finished, reader-facing GSPL prose (beats).
- **Where notes actually live:** a single **Notes chapter** — a `ChapterNode` sibling to
  Matthew/Mark/Luke/John/... under the GSPL book, positioned *last* — holds every note from
  every book as one growing, addressable pool, one beat per note. Not a per-book Notes chapter,
  not a per-chapter Notes beat: one destination, appended to as the book grows, so any later
  beat in any book can cite a number from that same running sequence.
- A Notes-chapter entry contains the full citation (APA-formatted for modern sources; standard
  reference form for ancient primary texts and scripture) plus, where relevant, the specific
  locator (page, section, verse) that grounds that particular claim.
- **Archival completeness (do not cite a bare link):** every Notes/Glossary citation must carry
  the full author name(s), full title, publisher/journal + year, and the specific locator — and
  the note's own prose must paraphrase or quote the actual substantive finding, not just point at
  a source. A URL or a shorthand author-surname-only mention is not enough: the website can go
  offline in a few years, and the note must still mean something without it. When no digital copy
  of a source can be found, cite it as a real, physical, traceable work anyway — a Library of
  Congress catalog record/control number or a WorldCat entry — rather than dropping the source or
  inventing a page range.

### 1b. The Glossary Tier (entities as reader-facing lookups)

**Prose names people, places, and terms without stopping to explain them — that's what makes it
prose. The Glossary is where the explanation lives**, one layer down, so a reader who doesn't
know what a Moabite is, or where Jericho sits, can look it up without the sentence they were
reading having to carry that weight itself.

- **The Glossary tier is the existing Entity/Character/Place records, not a new structure.**
  Every named person, place, or people-group mentioned in GSPL prose must have an entity record
  (per `docs/gospel/entity-catalog.md`'s seeding pattern) whose `Description` is a genuine,
  citation-backed glossary entry — not the terse one-line catalog summary these records start
  with. "Ruth was a Moabite" in prose should be immediately resolvable to a Ruth glossary entry
  that explains what a Moabite was, when, and where, which in turn cites the Notes chapter for
  its own evidentiary claims.
- **Three tiers, three jobs, no skipping:** Prose asserts a claim and cites a note code for it
  directly when the claim is central to that beat's own argument. Terms/figures/places used only
  in passing (a name mentioned without being the beat's subject) don't need an inline note code
  in the prose itself — they need a Glossary entry that a reader can reach, and it's the
  Glossary entry that carries the note codes. Don't make every mention of "Jericho" carry its own
  inline citation; make sure "Jericho" resolves to a Glossary entry that does.
- **Every mention gets tracked**, not just first appearance — this reuses the existing
  entity-presence system (`BeatEntityPresence`, whole-word + alias scan) already built into the
  engine for fiction; it applies unchanged to GSPL. A term or figure that turns out to have no
  entity record when it's used in prose is a seeding gap, not an acceptable omission — add the
  entity before or immediately after the beat that first needs it.

## 2. The Spectrum, Rendered as Prose (GSPL addition — the "no verdict" rule)

`docs/gospel/README.md` §2 defines the spectrum of scholarship a topic must cover before any
prose gets written. In prose, that spectrum runs the full range from **Jewish rabbinic and
traditional scholarship** (readings grounded in Talmudic, midrashic, and confessional tradition)
through **Christian confessional scholarship**, through the **mainstream historical-critical
academy** (the field's actual working center of gravity — named scholars, not anonymous
"experts"), to the **hardcore empiricist/archaeological pole** that accepts only what physical
evidence or contemporary documentary record directly attests. A GSPL beat that stages a
disagreement should let more than one of these positions speak in its own terms, with its own
real argument — never a strawman stand-in for "faith" opposite a strawman stand-in for "science."

- Do not adjudicate theological truth. Whether God acted in history is not a question any
  beat should answer, imply an answer to, or mock. Stay inside what evidence can speak to.
- Fringe positions (either end) get named as fringe when they are fringe — "spectrum" is not
  false balance between the academy's center of gravity and a discredited outlier.
- The tension between positions is a legitimate source of narrative energy — a chapter can be
  built around watching a single claim (an apostle's grave, a governor's fate, a border) refract
  differently depending on who's answering, without resolving which answer is "right."

## 3. Tone (GSPL addition to CRAFT §0–2)

**Curious, not adversarial.** The reader should feel like they're in the room with someone who
finds this stuff genuinely fascinating and is leveling with them — not someone building a case
against, or for, anyone's faith. CRAFT's Clarity Standard (§0: write so it can be read once)
applies at full strength — this is popular narrative nonfiction, not an academic monograph, and
should read like the best of that genre: concrete, propulsive, willing to let a strange fact
just sit there and be strange.

**The "wait, actually" beat.** The through-line move of this universe is: state the familiar
version plainly, then turn it — "and here's what the record actually shows." Earn the turn with
a real citation every time; never rely on innuendo or implication to create the turn.

**Never trade rigor for a better story.** If the evidence is genuinely thin, thinner prose is
correct, not a punchier invented specific. This universe's entire premise is that it can be
trusted; that trust is the whole product.

## 3a. Publishing Imprint (GSPL-specific exception to the global "author = MindAttic" rule)

**Every book-level node under GSPL (Matthew, and Mark/Luke/John as they're created) has
`Node.Author` set to `"Pulpit Press"`**, not `"MindAttic"`. This is a deliberate, explicit
exception to this project's global export-author rule, scoped to GSPL only — set once per book
node (`UPDATE Nodes SET Author = 'Pulpit Press' WHERE Slug = '<book-slug>'`) so `ss --export-node`
picks it up automatically without needing `--author` passed on every export. New chapters added
to an existing book inherit this via the book node's own `Author` field; a brand-new book node
(Mark, Luke, John) needs the same one-time `Author` update when it's created.

## 4. Hard Prohibitions (GSPL)

- No invented citations, dates, page ranges, or specifics not traceable to a real source
  (§1). This is the GSPL equivalent of SCRY's "death is permanent" — a load-bearing rule with
  no exceptions.
- No adjudicating theological truth claims as settled by the prose (§2).
- No treating a fringe position as equivalent in weight to the mainstream academic
  center of gravity, or vice versa treating mainstream consensus as the only legitimate view
  when serious, named scholarly disagreement exists.
- No converting the Gap Table into literal on-page tables in finished prose — render it as
  narrative, per §0. (The tables belong in the grounding research docs under `docs/gospel/`,
  not in the reader-facing beat.)

## 5. Production Workflow (how Matthew was actually built — reuse this for Mark/Luke/John)

This section documents the concrete mechanics that worked writing Matthew end to end, so the
next book doesn't have to rediscover them. Read this before starting Mark.

### 5a. Node structure and SortKey spacing

A GSPL book is one `BookNode` (created via `ss --create-book --kind book`) with `ChapterNode`
children, one per source chapter (`--kind chapter --parent <book-slug>`), **plus two trailing structural
chapters**: a **Notes** chapter and a **Glossary** chapter, both siblings of the numbered
chapters, both positioned with a `SortKey` *higher than every chapter's*. This is the one
mistake most likely to recur: `ss --create-book` assigns default SortKeys that can tie with an
already-created chapter (e.g., Notes created at the same SortKey as Chapter 2), which makes the
exported order interleave Notes/Glossary into the middle of the book instead of appending them
at the end. **Always explicitly set SortKey after creating Notes/Glossary**, well above the
highest chapter (chapters run 100, 200, 300... one per chapter number; put Notes at chapter-count
+2 rounded up, e.g. `3000` for a 28-chapter book, Glossary at `3100`):

```sql
UPDATE Nodes SET SortKey = 3000.0 WHERE Slug = 'notes-<id>';
UPDATE Nodes SET SortKey = 3100.0 WHERE Slug = 'glossary-<id>';
```

Verify with one query before ever exporting: `SELECT SortKey, Title FROM Nodes WHERE
ParentNodeId=@book ORDER BY SortKey` — Notes and Glossary must be the last two rows.

### 5b. The `--beat insert` "lands at top" gotcha

`ss --beat insert --node <slug>` **without `--after`always inserts at the top of that node**,
regardless of how many beats already exist there. This is not "append" — the second beat you
insert into an empty-ish node with an existing beat will land *before* it unless you pass
`--after <existing-beat-id>`. When inserting a single new beat into a chapter that already has
content (the common case — expanding an existing thin chapter), always pass `--after
<last-beat-id-in-that-node>`. When inserting *multiple new beats in sequence* (writing a chapter
from scratch), either insert them in *reverse* order (last beat first, so each subsequent
`--after=top` insert pushes it further down — unreliable, don't rely on this) or, simpler and
what actually worked: insert all of them, then **fix `BeatNodes.SortKey` directly with one SQL
UPDATE per beat** to the correct final order (100, 200, 300... for a chapter; the Notes chapter
keeps a single running sequence across the whole book — see §1a — so each new Note's SortKey
must be set higher than the current maximum, not just higher than its immediate neighbors).
Always re-query and print the final SortKey-ordered list after every batch of inserts to confirm
order before moving to the next chapter — do not assume the insert calls landed in writing order.

### 5c. Per-chapter depth standard (the failure mode this section exists to prevent)

The single biggest quality failure in Matthew's first full pass was writing one short,
single-paragraph summary beat per chapter once early momentum was established (chapters 1-3 got
the full method; chapters 4-25 initially got a bare "here's what happens, nothing here is
checkable" paragraph each). **A finished chapter needs one beat per pericope with a genuine
checkable claim**, following Scriptural Claim → Spectrum of Scholarship → Independent Record →
Gap Table reasoning → (Open Questions where relevant), not a chapter-level summary that gestures
at the method once and then asserts "nothing here is checkable" for the rest of the chapter's
content. Concretely: before writing a chapter, list every named person, place, artifact,
custom, or dated event in it; for each one, either (a) it's genuinely pure ethical/parabolic
teaching with nothing to check — say so honestly, briefly — or (b) it names something
real-world-checkable (a place with archaeology, a custom with a rabbinic/Josephus parallel, a
coin, a citation the source text itself gets wrong) — in which case it earns its own paragraph
and, where the fact is non-obvious, its own Note citation. A chapter with zero new Note citations
across dozens of verses is almost always under-researched, not genuinely free of checkable
content — pure-parable chapters (the Sermon on the Mount's ethical stretches, Chapter 13, Chapter
18, Chapter 25) still have real economic/social backdrop details worth one paragraph each (wage
rates, wedding customs, child status, debt scale) even when the parable's *moral* content stays
appropriately unweighed.

### 5d. Research-then-write, every claim WebSearch-verified before citing

Do not write a Note or a prose claim from memory alone, even for well-known facts — verify via
WebSearch first (site, date range, specific figures) and only then draft the Note and the prose
paragraph together. This caught real errors during Matthew (e.g., confirming the Field of Blood
citation is misattributed to Jeremiah rather than assuming it) and surfaced genuinely richer
material than memory alone would have (e.g., the Tyrian shekel's pagan imagery, Josephus's mundane
explanation for "not one stone upon another"). Two searches per new claim (one for the core fact,
one for a specific number/date/name if the first result is vague) is the typical cost.

### 5e. Entity seeding as you go, not as an afterthought

Every new scholar or place named in a Note or a beat gets seeded into the entity repo (`ss
--add-character --dir <folder>` / `ss --add-place --dir <folder>`, batch mode) **in the same work
session as the beat that introduces them**, not deferred to a cleanup pass at the end. Check
first (`SELECT Slug FROM Entities WHERE UniverseId=... AND Name LIKE '%X%'`) since many will
already exist from the original entity-catalog seeding or an earlier chapter's citations. Tag
scholar entities with their evidentiary camp (`camp-mainstream-historical-critical`,
`camp-confessional`, `camp-confessional-rigorous`, `camp-empiricist`) per §2's spectrum, since
that tag is what lets a future beat correctly characterize whose reading a citation represents.

### 5f. Chapter titles

Every chapter `Title` is `"Chapter N — <descriptive subtitle>"` (em dash, not hyphen — set it via
a UTF-8 `.sql` script file run with `sqlcmd -f 65001`, never inline through a shell command; git
Bash's default codepage silently flattens em dashes to plain hyphens otherwise, and this is easy
to miss since the mangled output still looks plausible in a terminal that renders both the same
width). Verify with `SELECT UNICODE(SUBSTRING(Title, <dash-position>, 1))` — must read `8212`,
not `45`.

### 5g0. PowerShell pipe corruption when bulk-inserting beats (`--beat insert`/`--beat update --text -`)

`Get-Content <file> -Raw | & dotnet run --project v3/StreetSamurai.Cli -- ... --text -` is the
right pattern (Program.cs sets `Console.InputEncoding`/`OutputEncoding` to UTF-8 specifically for
it — see the comment at the top of `Program.cs`), but **Windows PowerShell 5.1's own pipe-to-native-process
encoding is a separate, independent setting** (`$OutputEncoding`, a preference variable, not the
same thing as `Console.OutputEncoding` on the receiving side) and defaults to something that is
NOT UTF-8. Left at its default, every em dash, curly quote, and other non-ASCII character sent
through the pipe gets replaced with a literal `?` (ASCII 0x3F) — not a display glitch, actual
character loss in the stored `Text`. Fix: `$OutputEncoding = [System.Text.UTF8Encoding]::new($false)`
at the top of the script, before the first pipe.

Separately — and this persists even after the `$OutputEncoding` fix above — PowerShell 5.1 also
prepends a literal BOM character (U+FEFF) to the start of the byte stream on *every* piped
invocation, regardless of the encoding instance's own `GetPreamble()` setting. This is a distinct
bug from the em-dash corruption and isn't fixed by the same change. If bulk-inserting many beats
in a loop (one `dotnet run` per beat), check afterward with `SELECT UNICODE(SUBSTRING(Text,1,1))
FROM Beats WHERE Id=...` — a stray U+FEFF (65279) at position 1 means every beat in the batch needs
the leading character stripped and `TextHash` recomputed (`SHA256` of `Text.Trim()`, UTF-8 bytes,
lowercase hex — same formula as the direct-SQL-update rule). Cheapest fix at scale: a single
`System.Data.SqlClient`-based PowerShell pass reading/stripping/rehashing/writing back all
affected rows in one pass, rather than re-running the CLI again (the CLI re-run fixes the
em-dash corruption but re-introduces the same BOM every time, since it's inherent to the pipe
mechanism, not the encoding argument). Verify with a spot-check on a known em-dash position
(`UNICODE(...)` must read `8212`) and a `COUNT(*)` of remaining `UNICODE(...)=65279` rows (must be
zero) before trusting a bulk insert batch.

### 5g1. Hard line-wrap corruption when authoring beat text directly (not via subagent)

When Claude Code authors a Notes/Glossary entry's text directly in a `Write` tool call (as
opposed to a subagent returning its final message text), the content sometimes comes out with a
literal line break (`\n`) every ~90-100 characters mid-paragraph — an artifact of how the content
gets composed, not something the model intends. Piped through `--beat insert`/`--beat update
--text -`, that single `\n` is stored verbatim in `Beats.Text` and the export pipeline renders
each one as its own paragraph (visible as short, choppy "paragraphs" with extra spacing in the
`.pdf`/`.docx` — caught via a user screenshot of Note 1, 2026-07-27). Subagent-authored text (the
`Agent` tool's returned final message) has NOT shown this problem — only text typed directly by
the main thread into a `Write` call.

**Detection:** count single newlines not part of a `\n\n` paragraph break
(`(?<!\n)\n(?!\n)`, regex) relative to text length; a ratio above roughly 0.5% with more than a
few matches indicates hard-wrap corruption, not intentional paragraph breaks. Run this across
every beat under the book node, not just the entries just written — the very first "depth pass"
session had this same bug and it silently affected all 51 original Notes plus the original 3
Glossary entries (Jericho/Moab/Ruth) until caught here.

**Fix:** collapse each offending lone `\n` into a single space — EXCEPT when the character
immediately before it is a hyphen, in which case concatenate directly with no space (word-wrap
never splits mid-word, so a hyphen right before a wrap point already belonged to a compound like
"2nd-millennium-BCE", not to hyphenation the fix should re-introduce a space into). Recompute
`TextHash` afterward, same as any other direct-SQL `Text` update. Re-export and grep the fresh
`.txt` for the fixed note's opening line to confirm it now reads as one flowing paragraph.

### 5g. Export and final verification

Export the **book-level node**, never a chapter individually — `ss --export-node --slug
<book-slug> --author "Pulpit Press"` recursively walks every child chapter plus Notes plus
Glossary in `SortKey` order and combines them into one manuscript automatically; this is why
§5a's SortKey discipline is the only thing standing between a correctly trailing Notes/Glossary
and one embedded in the middle of the book. After every export, grep the resulting `.txt` for:
every expected `Chapter N` header count, the full `[1]`...`[N]` Note sequence with no gaps, and
every Glossary entry — before telling the user it's done.
