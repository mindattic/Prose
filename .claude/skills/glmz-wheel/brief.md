# GLMZ focus wheel: reader brief (one focus book per run)

WORK = the work dir the orchestrator names in your prompt (written SP below). <tools> = .claude\skills\glmz-wheel\tools in the repo.

## What this is
The author's design: "center on one book, and then draw all texture from every other book to update it, then once
that's done you make another book the focus ... each book becomes the center of attention on each turn of the wheel
and every other book becomes a contributing factor", repeated "until no meaningful changes or improvements can be
found in any of the books". Each step relaxes ONE focus book against the CURRENT text of all 21 others (earlier
steps of this wheel may have changed them: always read their dumps, not memory). The goal: one world, told from
every walk of life; the inhabitants occupy it collectively; no book off on its own; acts have consequences; names,
places, tech, lingo, politics, culture and timelines agree everywhere; each book reads cold, start to finish, and
leaves the reader wanting more. Rule of Cool is the only steer.

## Inputs (all in SP)
- B_<slug>_clean.txt: the current text of every book (B_<slug>.txt is the same with entity tags; beat headers are
  `--- [position] #beatnumber (title) (guid) ---`; cite #beatnumber).
- card_<slug>.md: each book's year, POV, premise, SECRET, exportables with anchors (written in round 5; the B_ dump
  wins wherever they disagree).
- ENT7.txt: every entity tagged in the catalog and the books it appears in. LAWS7.txt: world rulings.
- applied.txt lists every docket already applied in this wheel. Read the ones for OTHER books to see what was
  just drawn in elsewhere, and never repeat their phrasing.

## Your step
1. READ SCOPE. Cycle 1 (or a book new to the wheel): read the WHOLE focus book, every line, in order, in chunks.
   Later cycles: the orchestrator gives you the beat positions that mention entities changed elsewhere since this
   book's last turn (from select.json). Read each of those beats with two beats either side for context, and only
   those. Do not skim inside what you read.
2. Know the contributors: all 21 other cards; grep the other books' dumps for anything you want to draw in.
3. Find only MEANINGFUL changes. A row is meaningful only if it is one of:
   A. RULE #1: a first-time reader would stall here (unglossed jargon/acronym/proper noun, orphaned referent, a
      stake, relationship or profession never stated plainly). Withheld central mysteries are not defects.
   B. CONSEQUENCE: an act whose ramification never lands, or a consequence with no act behind it.
   C. BEHAVIOR: a character acting against how the book itself shows them under pressure.
   D. VERACITY: a name, place, faction, tech, slang, price, date, distance or law that disagrees with another book,
      a card, ENT7 or LAWS7. One name = one person universe-wide. One character = one set of pronouns. Timelines add
      up across books.
   E/H. BELONGING: a bare stretch (a transition, an arrival, a quiet moment, a dialogue-only run) where this narrator
      would plausibly meet something ALREADY REAL in another book (a market, food, drink, stray, club, broadcast,
      corp product, faction habit, public incident from another book, rumor, slang term, a minor face) and the book
      is poorer without it. Name the source book. Texture, never plot-load: a reader of this book alone loses
      nothing; a reader of both gets the jolt of recognition. Favor sources that are themselves weakly connected
      (btl, undr, icfi, blst, srzr, sprw, vatd, rtr) so the edges bind to the center.
   F. THE PULL: a chapter end that stops instead of pulls, fixable with one sentence.
   G. ASSEMBLY: duplicate beats, stray quotes, tense slips, garbled sentences.
   NOT meaningful: more texture where texture already is; a second reference to something the book already
   references; restating; anything the book reads equally well without. If the book is already integrated and
   clean, ZERO rows is the correct, expected answer. Do not invent work.
4. Caps: borrowing (E/H) is OPTIONAL and capped at 3 rows; zero is the normal answer. Corrections (A, B, C, D, F,
   G) stay required: every one you find gets a row. Each insertion is 1 sentence, 2 at most.
5. Write SP\gs_c<cycle>_<focus>.json, a JSON array (possibly []):
     {"beat": <beat number>, "old": "<exact substring of that beat's text in B_<focus>.txt, the TAGGED file>",
      "new": "<replacement: old + appended sentence(s), or old with a clause folded in or a defect fixed>", "count": 1}
   `old` must occur exactly once in that beat and must not contain or cut through <entity ...>...</entity> markup.
   Validate from the work dir with `python <tools>\check.py <focus> <cycle>` (fix every ERROR; an em-dash ERROR on a
   row that only turns a spaced hyphen into the book's em dash is expected) and `python <tools>\dup.py <focus> <cycle>`
   (rewrite every TEMPLATE hit through this narrator's own eye). Before keeping any borrowed detail, open the source
   book's dump and confirm the detail is there (search with python and re, not grep -o: beats span lines).
6. Write SP\gsr_c<cycle>_<focus>.md: one line per row: #beat, section letter, source book (for E/H), why it is
   meaningful. Then EDGES: relationships the prose shows plainly that the graph should hold, as
   `Name -relation-> Name (#beat)` with names exactly as in ENT7 (member_of, works_at, located_at, lives_at,
   frequents, allied_with, enemy_of, owes, knows, owns, family_of), max 10. Then FLAGS: anything that needs the
   author (a contradiction with a recorded ruling about a lead's defining backstory, a death, a retcon), not fixed.

## Already judged (do not re-litigate)
- Round 5's law sweep judged every device word in all 22 books. KEPT and allowed: institutional and professional kit
  (checkpoint, dispatch, office, district, clinic, school and studio terminals, monitors and screens; handheld
  professional recorders; vehicle displays; androids' own displays), unmeshed users, off-mesh need, relics,
  military proceedings, ArcSec officers and detectives. Change a device word ONLY if it is a physical sign or a
  meshed person's personal convenience device introduced since then.
- Before changing any date, day, duration or distance, quote BOTH beats that conflict and do the arithmetic from the
  prose's exact words (e.g. "before the case existed" is not "before the child vanished"). If the prose is
  consistent on its own words, leave it.

## World laws (every new sentence obeys every one)
- No em dashes, no "--" in NEW sentences. But a stray spaced hyphen used as a dash in existing text, in a book that
  uses em dashes, becomes that book's own em dash with its spacing (a G fix), not a comma or colon.
- No em dashes, no "--". The book's own tense, POV, rhythm and spelling (ferrocement, never ferrocrete; arcology;
  gray; "Hyperreality" in full, never HR; "the Atmospheric Processors" in full).
- No cash, coins, wallets, phones, tablets, slates, datapads, screens or monitors as devices, terminals, keyboards,
  physical signs, burners. Neuretic AR windows; credsticks carry Qreds; signs hang in the air in hyperreality.
- No police, cops, courts, judges, lawyers, trials, detectives (ArcSec officers excepted where a book already has
  them). Arcturus/ArcSec is the only street enforcer and keeps people down. Halcyon polices the digital and never
  runs armed street ops. Hearings are corporate or military review boards.
- No New York, California, Florida as visitable places. Nobody in the Seams goes "up the well".
- Strays: lumen mice, Sentinel Strays, Silver Pigeons, koi-colored pigeons. Never "turret cats" or "Null Crows".
- NO NEW NAMED PEOPLE. No new brand or place where an existing one fits.
- Never spoil any SECRET, including a book's ending: e.g. Candelaria's sale to Ashgrave and its touchdown are BLST's
  secret ending; another book may not say the bloc is gone. Never hint that BCODA's entity is Kyle. Standing Contract (SCON) is not a source.
- Chronology: a book knows only what exists by its own year and what its POV could see or hear.
- Do not change plot, dialogue meaning or character facts.
- Spent sources this cycle (already drawn into several books; do not draw again): ICFI's farm Behemoth / walker on the 88
  (in btl, bcoda, crit); BTL's essay *I Archived My Father and I Regret It* (in dwiace, mnemo); UNDR's Lamplighters as
  hunters below (in dwiace); the Cogs' knock on a pipe (in mnemo). Before drawing any item, grep the other books' dumps: if it already appears in 2+ other books
  as a borrowed mention, pick something else.
- Round-6 stock images are spent: no "try me / please don't", "insects between hives", "cold blue-green",
  "half-lids", "tortoise", "sweet chemical", "rust weeping", "hot-metal", "wet cardboard", "sliver".

Do NOT run prose / prose.cmd / dotnet / anything touching the Hub. Write only gs_c*_<focus>.json and
gsr_c*_<focus>.md in the work dir. Reply in at most 6 lines: rows by section letter, and the single most important finding.

## Mechanical defects are not your job
Spaced hyphens used as dashes, doubled punctuation and retired spellings are page-laws (01a10d17-e920, 01a10d19-40c1,
01a10d17-fe30). `prose --ruling violations --node <slug>` finds them for free; the orchestrator fixes them before your
turn. Spend your reading on what a pattern cannot see.
