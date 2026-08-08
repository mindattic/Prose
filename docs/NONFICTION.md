---
codex: SS
project: Prose
code: NONFICTION
layer: universe
universe: nonfiction
status: live
tier: series
scope: NONFICTION
triggers: NONFICTION, citation, primary source, history vs heritage, nonfiction
updated: 2026-08-04
related: docs/CRAFT.md
---

# NONFICTION — Universe Craft Rules {#SS-NONFICTION}

> **Scope: NONFICTION universe stories only.** Universal prose rules live in **docs/CRAFT.md** (Base
> layer) — clarity standard, scene architecture, sentence/voice rules all apply here unchanged.
> This file adds the one thing GLMZ and SCRY don't need: **every factual claim must be true to
> a verifiable source**, because unlike GLMZ/SCRY, NONFICTION's subject is the real world. The
> research method this doctrine enforces is documented in full in
> [`docs/gospel/README.md`](gospel/README.md) — read that first; this file is the prose-craft
> translation of it.

---

## 0. What NONFICTION is {#SS-NONFICTION-0}

**NONFICTION** (formerly SOURCE, renamed 2026-08-04; formerly GSPL/"Gospel" before that) is
Prose's citation-grounded **nonfiction** universe —
home for ANY exhaustively researched, popular narrative nonfiction book where every factual claim
traces to a real, verifiable source, "something you would use as an APA citation." It is not
scoped to religious or historical subject matter specifically — that has simply been every book
produced here so far. **Gospel: History vs. Heritage** (Matthew/Mark/Luke/John) was the first
production line; **Sons of God, Daughters of Men: A Cultural History of the Nephilim** (NEPH) is
the second; further Old Testament and cross-cutting topics (the Exodus, David and Solomon's
kingdom, the Dead Sea Scrolls, the Council of Nicaea) are queued — but a future NONFICTION book could
just as easily be about science, true crime, biography, or any other nonfiction subject. The one
requirement is the discipline below: every claim cited, every gap in the record stated honestly.
Every book in this universe shares that same method regardless of subject.

Each book is an entertaining, readable examination of its subject — what the text or tradition
says, set against what the independent historical and archaeological record says, and what the
whole range of serious scholarship in between says. It is a book meant to be read and enjoyed,
not a reference ledger. The research (claim → spectrum → evidence → gap → sources) is the
**grounding material** a beat is written from — never the beat itself. A finished beat is prose:
a scene, an anecdote, a turn of argument, a moment of "wait, actually" — not a table.

**Not a debunking project, not an apologetic one.** The purpose (§1 of `docs/gospel/README.md`)
is to supply the context needed to read scripture accurately — the "George Washington's
dentures" standard: an unflattering, verifiable fact omitted from the popular version is not
neutral, it's heritage substituting for history. NONFICTION prose should leave a reader of any faith
position, or none, able to trust that what they just read is accurate.

---

## 1. The Citation-Grounding Rule (NONFICTION addition — no equivalent in GLMZ/SCRY)

**Every factual claim in a beat — a date, a name, a place, a document, an artifact, a quoted
position — must be traceable to a citation in that beat's grounding research doc.** GLMZ and
SCRY invent their world; NONFICTION reports on this one. A beat that asserts a fact with no traceable
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
- A citation-grounding check (mirroring the existing quote-grounding guard, `prose --verify-quote`)
  should run on NONFICTION beats before they're considered done: every specific factual claim traced
  back to the grounding doc's Sources section.

### 1a. Numbered Notes (the reader-facing citation format)

**Every factual claim in finished NONFICTION prose carries an inline number in square brackets** —
e.g., "... the prefect's actual title, contemporary records confirm [37], was..." — resolved in
a dedicated **Notes chapter**, not inline parenthetical author-dates and not a per-chapter
"Notes" beat.

**Bracket convention (do not mix these up):** scripture verse references use parentheses,
`(1:2)`; Note citations use square brackets, `[12]`. The two numbering schemes look similar
enough (`(11)` vs `[11]`) that using the same bracket style for both is a real ambiguity, not a
stylistic nitpick — a reader can't tell a verse reference from a citation number at a glance
unless the brackets differ.

- **Numbering is one flat sequence per book, restarting at each Gospel** — plain `(1)`, `(2)`,
  `(3)`..., assigned once within that book and never renumbered or reused, running straight
  through that book's own chapters in canonical order. Matthew, Mark, Luke, and John are each
  their own `BookNode` (siblings under the NONFICTION series root), published as separate KDP titles
  ("Gospel: History vs. Heritage — Matthew" / "— Mark" / "— Luke" / "— John"), so each restarts
  its own numbering at `[1]` rather than continuing a series-wide count — a reader of the Mark
  volume alone has no use for a note sequence that starts in the thousands because Matthew came
  first. (Superseded 2026-07-26, twice, before the 4-separate-books decision: a first pass tried
  book-prefixed codes like `MTW-16`/`MRK-1`; dropped because the prefix added a lookup layer
  without solving anything a plain running number doesn't already solve on its own. Superseded
  again 2026-07-28: the running-number-across-the-whole-series design was reconsidered once each
  Gospel shipped as its own standalone book rather than one combined volume.)
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
  numbered-note convention applies specifically to finished, reader-facing NONFICTION prose (beats).
- **Where notes actually live:** a single **Notes chapter** per book — a `ChapterNode` sibling
  to that book's own numbered chapters (e.g. Matthew's `Chapter 1`...`Chapter 28`), positioned
  *last* within that book — holds every note from that Gospel alone as one growing, addressable
  pool, one beat per note. Not a per-chapter Notes beat, and not a single Notes chapter shared
  across Matthew/Mark/Luke/John: each book gets its own destination, appended to as that book
  grows, so a later beat within the same book can cite a number from that book's own running
  sequence — never a number belonging to a different Gospel's book.
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
  Every named person, place, or people-group mentioned in NONFICTION prose must have an entity record
  (per `docs/gospel/entity-catalog.md`'s seeding pattern) whose `Description` is a genuine,
  citation-backed glossary entry — not the terse one-line catalog summary these records start
  with. "Ruth was a Moabite" in prose should be immediately resolvable to a Ruth glossary entry
  that explains what a Moabite was, when, and where, which in turn cites the Notes chapter for
  its own evidentiary claims.
- **Each book has its own Glossary chapter; the underlying entity record is shared.** Jesus,
  Pilate, Herod, and Jerusalem recur across Matthew/Mark/Luke/John — one entity record per
  name, reused everywhere — but each Gospel's own Glossary chapter carries its own beat/entry
  for any name that book actually uses, citing that book's own Notes sequence (§1a). A name that
  only recurs (no new evidentiary claim in this book beyond what Matthew's Glossary already
  covered) still gets its own entry in this book's Glossary — written fresh against this book's
  own Notes numbers — not a cross-reference back to another book's chapter, since a reader of
  the Mark volume alone won't have Matthew's Notes chapter in hand.
- **Three tiers, three jobs, no skipping:** Prose asserts a claim and cites a note code for it
  directly when the claim is central to that beat's own argument. Terms/figures/places used only
  in passing (a name mentioned without being the beat's subject) don't need an inline note code
  in the prose itself — they need a Glossary entry that a reader can reach, and it's the
  Glossary entry that carries the note codes. Don't make every mention of "Jericho" carry its own
  inline citation; make sure "Jericho" resolves to a Glossary entry that does.
- **Every mention gets tracked**, not just first appearance — this reuses the existing
  entity-presence system (`BeatEntityPresence`, whole-word + alias scan) already built into the
  engine for fiction; it applies unchanged to NONFICTION. A term or figure that turns out to have no
  entity record when it's used in prose is a seeding gap, not an acceptable omission — add the
  entity before or immediately after the beat that first needs it.

## 2. The Spectrum, Rendered as Prose (NONFICTION addition — the "no verdict" rule)

`docs/gospel/README.md` §2 defines the spectrum of scholarship a topic must cover before any
prose gets written. In prose, that spectrum runs the full range from **Jewish rabbinic and
traditional scholarship** (readings grounded in Talmudic, midrashic, and confessional tradition)
through **Christian confessional scholarship**, through the **mainstream historical-critical
academy** (the field's actual working center of gravity — named scholars, not anonymous
"experts"), to the **hardcore empiricist/archaeological pole** that accepts only what physical
evidence or contemporary documentary record directly attests. A NONFICTION beat that stages a
disagreement should let more than one of these positions speak in its own terms, with its own
real argument — never a strawman stand-in for "faith" opposite a strawman stand-in for "science."

- Do not adjudicate theological truth. Whether God acted in history is not a question any
  beat should answer, imply an answer to, or mock. Stay inside what evidence can speak to.
- Fringe positions (either end) get named as fringe when they are fringe — "spectrum" is not
  false balance between the academy's center of gravity and a discredited outlier.
- The tension between positions is a legitimate source of narrative energy — a chapter can be
  built around watching a single claim (an apostle's grave, a governor's fate, a border) refract
  differently depending on who's answering, without resolving which answer is "right."

## 3. Tone (NONFICTION addition to CRAFT §0–2)

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

## 3a. Publishing Imprint (NONFICTION-specific exception to the global "author = MindAttic" rule)

**Every book-level node under NONFICTION (Matthew, Mark, Luke, John) has `Node.Author` set to
`"Pulpit Press"`**, not `"MindAttic"`. This is a deliberate, explicit exception to this project's
global export-author rule, scoped to NONFICTION only — set once per book node (`UPDATE Nodes SET Author
= 'Pulpit Press' WHERE Slug = '<book-slug>'`) so `prose --export-node` picks it up automatically
without needing `--author` passed on every export. New chapters added to an existing book inherit
this via the book node's own `Author` field; a brand-new book node needs the same one-time
`Author` update when it's created.

## 3b. Levity — the dry-wit register {#SS-NONFICTION-3b}

**This material would be a dry report by default, and a dry report is a failed book.** The
antidote is not jokes; it's *dryness deployed on purpose* — the register of a very well-read
friend who finds a fact funny because it is genuinely funny, and trusts you to catch it without
being nudged. Mark chapter 4's "Thirtyfold was already exceptional; sixty- and a hundredfold
were the stuff of stories, not spreadsheets" is the standard: the wit is carried entirely by an
accurate fact and a well-chosen noun.

**The four moves that work:**

1. **The deadpan juxtaposition.** Put the ancient fact and its unglamorous mechanism side by
   side and decline to comment. ("It's a practical detail before it's anything else — water
   carries a voice, and a boat is a natural amphitheater when you can't build one.")
2. **The honest aside.** When the evidence is thin or the pericope has nothing checkable in it,
   say so in the first person plural and move on — the candor *is* the humor. ("There is little
   here to fact-check in the archaeological sense; it is, honestly, closer to pure agricultural
   observation dressed as theology.")
3. **The undercutting specific.** Let a real number, price, or title deflate a grand claim
   without editorializing. ("Luke's version is the better deal.")
4. **The scholarly-brawl aside.** Real academic disputes are frequently comic in their
   persistence; you may say so, as long as both sides are represented accurately and neither is
   the butt of the joke.

**Hard limits on levity — these are not negotiable:**

- **Never at the expense of the believer, the text, or the dead.** The joke is never "look what
  these credulous people thought." Punch at *bad evidence*, *institutional convenience*, and
  *our own modern smugness* — never at faith, and never at a named ancient person's suffering.
  Crucifixion, infant massacre, execution, slavery, and rape get no wit at all: those passages
  are written flat and plain.
- **Never at the expense of accuracy.** A funnier phrasing that shades a fact is a defect (§1).
  If the wit requires the fact to bend, drop the wit.
- **Never a punchline the reader has to be told is one.** No "ironically," no "amusingly," no
  exclamation points, no winking. State it straight and let it land.
- **Rate limit: roughly one wry moment per beat, not per paragraph.** Levity is seasoning. A
  chapter that is continuously clever reads as flippant, which forfeits exactly the trust §3
  exists to build.

## 3c. "Then and Now" — the mandatory closing movement {#SS-NONFICTION-3c}

**Every numbered chapter in every NONFICTION book ends with a short section headed `Then and Now`** —
one to three paragraphs, roughly 150–250 words — observing what has actually changed between
that chapter's world and the reader's, and what has not. It is the series' signature and its
single most reader-facing feature: the place where a wall of first-century detail becomes a
statement about being alive.

**What it is for.** The rest of a chapter establishes that the ancient world was *specific* — a
real price, a real title, a real building. Then and Now is where that specificity earns its
keep, by naming the one thing a modern reader would find genuinely alien and the one thing they
would recognize immediately. Both halves are required: a section that only says "how strange
they were" is condescension, and one that only says "people never change" is wallpaper.

**Rules:**

- **It is bound by §1 exactly like every other beat.** The ancient half must rest on facts
  already established (and cited) earlier in that same chapter — Then and Now introduces no new
  ancient claim that hasn't been grounded, and reuses that chapter's existing note numbers
  rather than minting citations for a closing flourish.
- **The modern half stays qualitative.** Do not reach for a modern statistic, wage figure,
  percentage, or dated survey to make the comparison land: a modern number needs a citation as
  much as an ancient one, and an uncited "today, 40 percent of…" is precisely the invented
  specific §4 prohibits. Compare *kinds of experience* — what a thing cost in labour, what it
  felt like to wait for news, who got believed — not indices.
- **This is where the levity lives.** §3b's register belongs here more than anywhere else,
  because Then and Now is the one place the narrator is permitted to be a person with an opinion
  about the present. The permitted target of that opinion is *us* — modern assumptions, modern
  self-congratulation — never the ancients.
- **No moral instruction, no altar call, no lesson.** Observe; do not advise. The reader draws
  their own conclusion, including a religious one. This is the §2 no-verdict rule applied to the
  present tense.
- **Never the same observation twice.** Across a book, the closing sections must not keep
  landing on one theme (typically "life was cheap" or "information travelled slowly"). Vary the
  axis: money, law, medicine, distance, literacy, food, women's testimony, debt, weather, noise,
  smell, who gets believed, who gets counted.
- **Structural placement:** it is the final beat of the chapter node (highest `SortKey`), and its
  text opens with the literal line `Then and Now` on its own, so the export renders it as a
  visible section break rather than another body paragraph.

## 4. Hard Prohibitions (NONFICTION)

- No invented citations, dates, page ranges, or specifics not traceable to a real source
  (§1). This is the NONFICTION equivalent of SCRY's "death is permanent" — a load-bearing rule with
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

**Series root, then one BookNode per Gospel.** `gospel-history-vs-heritage-<id>` (`ParentNodeId`
NULL) is the series-organizing node only — it is never itself exported and carries no
Title/Subtitle/Author metadata of its own. Each Gospel (Matthew, Mark, Luke, John) is its own
`BookNode`, a *child* of that series root, with `Title = "Gospel: History vs. Heritage"` and its
own `Subtitle` (`"Matthew"` / `"Mark"` / `"Luke"` / `"John"`) and `Author = "Pulpit Press"` — this
is the node `prose --export-node` actually targets, one per published KDP title.

A NONFICTION book is one `BookNode` (created via `prose --create-book --kind book --parent <series-slug>`)
with `ChapterNode` children, one per source chapter (`--kind chapter --parent <book-slug>`),
**plus two trailing structural chapters**: a **Notes** chapter and a **Glossary** chapter, both
siblings of the numbered chapters *within that same book*, both positioned with a `SortKey`
*higher than every chapter's*. Notes and Glossary belong to their own book only (§1a, §1b) — Mark's
Notes chapter is a sibling of Mark's own chapters, never a sibling of Matthew's. This is the one
mistake most likely to recur: `prose --create-book` assigns default SortKeys that can tie with an
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

**Set `NodeCode` on every BookNode as soon as it's created — this controls where `--export-node`
writes its files.** `ExportPathResolver` (in `Prose.Core/Services/ExportPathResolver.cs`)
publishes flat under `<universe-export-dir>/<NodeCode>/<NodeCode> V<n>.docx` when `NodeCode` is
set (e.g. `.../NONFICTION/MATTHEW/MATTHEW V17.docx`), matching the pre-made cover-art folders
(`MATTHEW/`, `MARK/`, `LUKE/`, `JOHN/` under `R:\Desktop\EPub\MindAttic\NONFICTION\`). Without a
`NodeCode`, it falls back to a legacy title-derived, series-nested path — and since all four
Gospel BookNodes share the identical `Title` ("Gospel: History vs. Heritage"), that legacy path
nests every book's export under one shared, colliding `Gospel History vs. Heritage/` folder with
confusing de-dup-prefixed subfolder names. This actually happened: Mark, Luke, and John were all
exported for an entire session with no `NodeCode` set, silently scattering their output across
that shared folder instead of their own `MARK/`/`LUKE/`/`JOHN/` folders, and it wasn't caught
until the user pointed out the folder names directly. **Set `NodeCode` (`MATTHEW`/`MARK`/`LUKE`/
`JOHN`) immediately when a book node is created, not as a post-hoc cleanup item** — a single
`UPDATE Nodes SET NodeCode='<CODE>' WHERE Id=@id` (with `SET QUOTED_IDENTIFIER ON` first, or the
update fails against this DB's indexed/filtered indexes) is enough; there's no dedicated CLI verb
for it as of this writing.

### 5b. The `--beat insert` "lands at top" gotcha

`prose --beat insert --node <slug>` **without `--after`always inserts at the top of that node**,
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
--add-character --dir <folder>` / `prose --add-place --dir <folder>`, batch mode) **in the same work
session as the beat that introduces them**, not deferred to a cleanup pass at the end. Check
first (`SELECT Slug FROM Entities WHERE UniverseId=... AND Name LIKE '%X%'`) since many will
already exist from the original entity-catalog seeding or an earlier chapter's citations. Tag
scholar entities with their evidentiary camp (`camp-mainstream-historical-critical`,
`camp-confessional`, `camp-confessional-rigorous`, `camp-empiricist`) per §2's spectrum, since
that tag is what lets a future beat correctly characterize whose reading a citation represents.

### 5f. Chapter titles

**Every chapter title is evocative, not descriptive** (series-wide standard, set 2026-07-28 — all
89 chapters across the four Gospels were retitled to it in one pass). A NONFICTION table of contents is
the first thing a browsing reader sees, and a contents page reading "The Temptation, the First
Disciples, and the Capernaum Ministry" advertises a reference work, which is the one thing §0 says
this is not. Mark's chapters 4–16 — written later than the rest and to a better instinct — set the
pattern that became the rule: `Chapter 4 — Seeds, Lamps, and a Sea That Would Not Behave`,
`Chapter 16 — The Ending That Isn't`.

- **The title must be true.** It is prose, not decoration, and §1 binds it: an evocative title may
  compress and it may be wry, but it may not assert anything the chapter doesn't establish. Naming
  the chapter after the strangest *real* thing in it is almost always the right move.
- **Prefer the concrete object over the abstract theme** — the stone jars, the coin, the fig tree,
  the headcount — because the concrete object is what the chapter can actually prove.
- **The rule-of-three list and the withheld turn both work** ("The Mountain, the Boy, and the
  Valley"; "The Ending That Isn't"); a bare topic label does not.
- **No spoiling a genuine open question.** If the chapter's payload is that scholarship is
  unsettled, the title should pose it, not resolve it.
- Same levity limits as §3b: no wit on crucifixion, infant massacre, or execution chapters —
  those get titles that are plain and grave (`Chapter 27 — The Nail, the Titulus, and the
  Governor Who Had the Last Word` is the ceiling; nothing lighter).

The format is `"Chapter N — <title>"` (em dash, not hyphen — set it via
a UTF-8 `.sql` script file run with `sqlcmd -f 65001`, never inline through a shell command; git
Bash's default codepage silently flattens em dashes to plain hyphens otherwise, and this is easy
to miss since the mangled output still looks plausible in a terminal that renders both the same
width). Verify with `SELECT UNICODE(SUBSTRING(Title, <dash-position>, 1))` — must read `8212`,
not `45`.

### 5g0. PowerShell pipe corruption when bulk-inserting beats (`--beat insert`/`--beat update --text -`)

`Get-Content <file> -Raw | & dotnet run --project v3/Prose.Cli -- ... --text -` is the
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

### 5g0a. PowerShell script-*source* mojibake — a distinct bug from the pipe corruption above

A `.ps1` file written by the `Write` tool (or any tool that emits plain UTF-8 without a byte-order
mark) is **misread by Windows PowerShell 5.1's own script parser**, not just its output pipe. Any
literal em dash or accented character typed directly into the script's source (e.g. `Update-
ChapterTitle $id 'Chapter 6 — ...'`) gets mangled into UTF-8-read-as-cp1252 mojibake (`—` becomes
`”"`, `ő`/`ö` become `ő`/`ö`) **before the script ever runs** — this happens even when using
`System.Data.SqlClient` directly with parameterized queries (i.e. it is *not* the same bug as
§5g0's pipe-to-`dotnet run` corruption; there is no child process or pipe involved at all here).
Symptom: `UNICODE(SUBSTRING(Title,...))` reads `226` (â) instead of `8212` (—), or the script
fails to parse entirely with `TerminatorExpectedAtEndOfString` if a mangled byte happens to
produce a stray quote character.

**Fix, verified working:** prepend a UTF-8 BOM (`EF BB BF`) to the `.ps1` file before running it —
this forces PowerShell 5.1 to correctly recognize and decode the file as UTF-8. The `Write` tool
does not add one, so do it as a separate step after every `.ps1` write that contains any
non-ASCII character (em dashes, curly quotes, accented names like "Győző Vörös"):

```bash
printf '\xef\xbb\xbf' | cat - script.ps1 > script.ps1.bom && mv script.ps1.bom script.ps1
```

Then run normally (`powershell -File script.ps1`). Verify afterward with the same `UNICODE(...)`
spot-check as §5g0 (must read `8212` for an em dash, not `226`). This is the reliable way to author
bulk-insert scripts (chapter titles, Notes, Glossary) using real Unicode characters directly in
PowerShell rather than avoiding them via ASCII approximation — confirmed safe for both em dashes
and Hungarian/German-style diacritics in a live test during the Mark chapter 4-7 import (2026-07-28).

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

### 5g2. `--beat delete` is a SOFT delete — filter `IsEnabled=1` in every count/audit query

`NodeWorkbenchService.DeleteBeatAsync` (the code behind `prose --beat delete`) only flips
`BeatNodes.IsEnabled = 0` on the membership row — it never removes the `Beats`/`BeatNodes` row
itself. This is intentional (recoverable via `prose --beat` restore semantics on the same junction
row) but it silently breaks any audit query that doesn't account for it: a raw `SELECT COUNT(*)
FROM BeatNodes WHERE NodeId=@notes` or a raw `SELECT ... FROM BeatNodes bn JOIN Beats b ...` will
still return "deleted" rows, making it look like a beat you just deleted "came back." This ate
real time during John's production (chased a phantom "row that won't die" for a stray test-probe
beat, and again for four intentionally-merged-away duplicate glossary entries, before realizing
the delete had worked correctly all along and the audit queries were just wrong). **Every
count/audit/dedup query against `BeatNodes` must add `AND bn.IsEnabled = 1`** (or explicitly
query the disabled set on purpose) to see the true, currently-effective content.

### 5g3. Harden the `$maxNoteNumber` derivation query before reusing any prior chapter's `.ps1` as a template

The per-chapter import scripts (the historical Gospel-production scripts used the
`scripts/gspl_<book>_ch*.ps1` naming pattern; the 1381 book used the transitional
`scripts/source_<book>_ch*.ps1` pattern; new NONFICTION-universe books should use
`scripts/nonfiction_<book>_ch*.ps1`) derive the next Note number with `SELECT MAX(CAST(LEFT(b.Text, CHARINDEX('
',b.Text)-1) AS INT)) FROM BeatNodes bn JOIN Beats b ON bn.BeatId=b.Id WHERE bn.NodeId=@notes`.
This crashes with "Invalid length parameter passed to the LEFT or SUBSTRING function" if ANY row
under that Notes node has no space character in its text at all (e.g. a stray non-numbered test
row) — `CHARINDEX` returns 0, and `LEFT(text, -1)` is invalid. When this crashed silently inside a
larger PowerShell run (the exception didn't halt the script), `$maxNoteNumber` fell back to an
unset/zero value and that chapter's notes were inserted as duplicate numbers 1-N, colliding with
the book's real notes 1-N — this happened once during John's production (chapter 5) and required
a manual renumber-and-fix-cross-references pass to correct. **Always use this hardened version
instead, in every future book's chapter-import scripts:**

```sql
SELECT ISNULL(MAX(CAST(LEFT(b.Text, CHARINDEX(' ',b.Text)-1) AS INT)),0)
FROM BeatNodes bn JOIN Beats b ON bn.BeatId=b.Id
WHERE bn.NodeId=@notes
  AND CHARINDEX(' ', b.Text) > 1
  AND LEFT(b.Text, CHARINDEX(' ',b.Text)-1) NOT LIKE '%[^0-9]%'
```

After a chapter script runs, spot-check for duplicate note numbers before moving to the next
chapter: `SELECT n, COUNT(*) FROM (SELECT CAST(LEFT(b.Text, CHARINDEX(' ',b.Text)-1) AS INT) AS n
FROM BeatNodes bn JOIN Beats b ON b.Id=bn.BeatId WHERE bn.NodeId=@notes AND CHARINDEX(' ',
b.Text)>1 AND LEFT(b.Text, CHARINDEX(' ',b.Text)-1) NOT LIKE '%[^0-9]%' GROUP BY n) d WHERE n >
1` — catching it immediately after one chapter is far cheaper than discovering it at the
end-of-book audit.

### 5g4. When dispatching parallel drafting agents, "don't execute" must be an explicit numbered step

If a batch of parallel Sonnet agents is asked to research and write a chapter's `.ps1` import
script, at least some of them will run it against the live DB anyway even when the task
description only asks them to "write and verify" — general-purpose agents have full tool access
and will reasonably interpret "make sure this works" as license to test-execute. This scrambles
the intended strict chapter-order execution sequence that keeps Note numbering sequential (two
agents self-executed during John's first batch, despite never being asked to, which is
harmless in isolation but means later chapters' Note ranges don't land in the same order as the
book's reading order — a cosmetic-only issue, not worth an end-of-book renumber, but avoidable).
**State it as an explicit, separately-numbered "CRITICAL: do NOT execute this script" step in the
prompt**, not folded into other instructions — every batch that did this explicitly (batches 2-4)
had zero self-executions; the one batch that didn't (batch 1) had two.

### 5g. Export and final verification

Export the **book-level node**, never a chapter individually — `prose --export-node --slug
<book-slug> --author "Pulpit Press"` recursively walks every child chapter plus Notes plus
Glossary in `SortKey` order and combines them into one manuscript automatically; this is why
§5a's SortKey discipline is the only thing standing between a correctly trailing Notes/Glossary
and one embedded in the middle of the book. After every export, grep the resulting `.txt` for:
every expected `Chapter N` header count, the full `[1]`...`[N]` Note sequence with no gaps, and
every Glossary entry — before telling the user it's done.
