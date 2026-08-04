---
codex: 1
project: StreetSamurai
layer: planning
code: RESIST
title: "Resistance: Three Centuries of Irish Rebellion"
universe: NONFICTION
updated: 2026-08-03
---

> **CURRENT IDENTITY (authoritative — the book was renamed twice on 2026-08-03):**
>
> | Field | Value |
> |---|---|
> | Title | Resistance: Three Centuries of Irish Rebellion |
> | NodeCode | `RESIST` |
> | Book node Id | `019fc926-c5d9-7663-b08f-fbfb82b43219` |
> | Slug | `resistance-three-centuries-of-irish-rebellion-019fc926` |
> | Brief | `docs/planning/RESIST-brief.md` (this file) |
> | Node doc | `docs/nodes/RESIST.md` |
> | Author | Ars Historica |
>
> **Rename history:** built as `TIRE` (from the working title "Tyranny: Ireland") → briefly
> `PURSUED` ("The Pursued: Three Centuries of Irish Rebels, 1594–1921") → now `RESIST`. The slug
> was rewritten at each step (verified safe: `RunLedger`, `WoundLedger`, and `ContinuityClaims`
> all hold zero references to this node's slug). Older sections of this brief still say "The
> Pursued" or "TIRE" in places; the table above wins.
>
> **Export note:** version numbering does NOT reset on a NodeCode change — it tracks
> `Nodes.Version`, not the output folder. Exports under earlier codes remain orphaned in
> `NONFICTION/TIRE/` (V1–V3) and `NONFICTION/PURSUED/` (V4–V5).
>
> **Dropped date range:** the previous title carried `1594–1921`; the current one does not.
> Earlier in planning we judged the dates useful for distinguishing the book from the many
> 1916/Troubles titles on the shelf. Worth revisiting as a subtitle if it goes to KDP.

> **TITLE LOCKED 2026-08-03: "The Pursued: Three Centuries of Irish Rebels, 1594–1921."** Set in
> the DB (Nodes.Title, en dash verified). Do NOT justify the title with the toraí etymology in
> the introduction — the title stands on the history (these men were, in plain fact, hunted), not
> on a contested word derivation. See the corrected etymology note in Chapter 1 §3.

# Story Brief: TIRE — Three Centuries of Irish Rebellion, 1650-1950 (working title) {#SS-BRIEF-TIRE}

> **SCOPE LOCKED 2026-08-03, end of session, user-confirmed — read this before touching anything
> else.** The book went through several rounds of scope expansion in one sitting (two wars → six
> rounds spanning 1650-1950 → seven rounds spanning 1594-1921 → a floated eighth round, the
> Troubles, explicitly declined). The user then named the actual problem directly: "this went
> feature creep crazy; I have no idea what we're writing any more; find the common thread
> throughout history and use that to hang all this death and suffering on." The answer that
> followed, and that the user confirmed ("yes, I think that's a good way to tell the story"), is
> now this book's PRIMARY THESIS, ranking above the tactical-comparison framing that organized
> the chapter spine up to this point:
>
> **"Ordinary, named people repeatedly chose to fight for control of their own ground, against
> terrible odds, generation after generation — and it's that choice, not the tactics or the
> outcome, that survives as legend."**
>
> The tactical-problem chapter structure (Ch. 3-9) is KEPT as the book's organizing mechanism —
> it's how the comparison gets made concrete — but every chapter must now visibly serve this
> human thesis, not just the tactical one. The underlying causal engine (why Britain kept
> re-asserting control, provoking each round) is: strategic security against continental rivals
> using Ireland as a back door (Spain in the Nine Years' War, France in 1798); land, seized and
> redistributed to a loyal settler class as both payment and security; post-Reformation
> religious/political anxiety about a Catholic Ireland allied with Catholic Europe; and, once
> heavily settled and integrated (especially post-1801 Union), the institutional inertia of
> letting go. State this plainly in Chapter 1 as the "why," then let the human thesis carry
> every chapter after it.
>
> **SCOPE IS NOW FINAL: seven rounds, 1594-1921** (Nine Years' War through the War of
> Independence/Truce). No eighth round, no Troubles, no Wallace/Scotland/Wales, no pushing the
> start date earlier than 1594. Do not reopen any of these without the user asking again.
>
> Everything below marked "ORIGINAL SCOPE" describes the earliest two-war version (now
> superseded); "EXPANDED SCOPE" marks the six/seven-round widening (now the locked, final
> structure). Read both as historical record of how the book got here, not as live open
> questions.

> **Mandatory before creating a node bible or any DB records** (per `docs/NONFICTION.md` and the New
> Story Workflow). Universe: NONFICTION (`Universe` slug `nonfiction` (was `source`, was `gspl`)), same schema/pipeline as
> GLMZ/SCRY, same citation-grounding discipline as the Gospel books, NEPH, and 1381. Fourth
> production line in NONFICTION, second fully secular-historical subject (after 1381), and the
> first **explicitly comparative** NONFICTION book — two events studied side by side rather than
> one event studied in isolation. Per SS-A45 / the 1381 precedent: **no permanent `docs/tire/`
> research folder** — research is WebSearch-verified as each claim is drafted, landing directly
> in Entity `Description` fields and Notes-chapter beats, not in a hand-committed markdown pile.

---

## 1. Series Position {#SS-BRIEF-TIRE-§1}

**Universe:** NONFICTION. **Story type:** Standalone NONFICTION book, fourth production line after the
Gospel tetralogy, NEPH, and 1381. Applies NONFICTION's citation-grounding method to a **comparative**
secular-historical subject: three centuries of insurgency and counterinsurgency in Ireland,
1650-1950, read as one continuous, recurring "sparring match" with the same opponent rather than
a set of unrelated episodes. **Node code: `TIRE`.**

**Book(s) this story serves:** None as a prerequisite. Fully standalone.

**Timing (EXPANDED SCOPE):** Real-world history, no in-fiction continuity, spanning six
documented rounds of armed and civil resistance across three centuries:
1. **The tory insurgency** (c. 1650-early 1660s) — after Rathmines (1649) and Scarrifhollis
   (1650) destroy the last Confederate field armies. *[Deepest-documented anchor #1.]*
2. **The 1798 Rebellion** — Wolfe Tone's United Irishmen, the Wexford rising (Father John
   Murphy, Vinegar Hill), and Michael Dwyer's Wicklow guerrilla holdout, which runs on past the
   main rising's defeat into a five-and-a-half-year individual campaign (1798-1803) — the
   longest sustained guerrilla holdout this book documents, bridging directly into:
3. **Robert Emmet's 1803 rising** — total tactical failure, disproportionate lasting legend.
4. **The Fenian Rising** (1867) — James Stephens's failed organizing, Thomas J. Kelly's rescue,
   and the Manchester Martyrs — another failure-into-legend case, on British soil this time.
5. **The Land War** (1879-82) — Michael Davitt's Land League, a civil-resistance model distinct
   from every armed episode either side of it.
6. **The War of Independence** (1919-21) — flying columns, Soloheadbeg to the Truce, with
   Crossbarry as the tactical high-water mark. *[Deepest-documented anchor #2.]* The 1922-23
   Civil War and Collins's death are noted only as a closing epilogue fact, not covered in depth.

The 1650s and 1919-21 remain the two most heavily documented, deepest case studies (they anchor
most of the book's Notes and the fullest entity rosters) — but every tactical-problem chapter now
threads in the intervening rounds wherever they sharpen or complicate the comparison, rather than
treating them as connective filler between two isolated wars.

**Explicit scope correction (recorded here so it isn't re-litigated mid-draft):** this is NOT the
1381→1649→1921 "coercive labour" arc (a different book), NOT a continuity/lineage thesis running
tories→Ribbonmen→Fenians→IRA (a defensible but different book, and the one this project
considered and set aside), and NOT primarily about land/dispossession (the Adventurers' Act,
transplantation) as the causal spine — land confiscation is retained only as the necessary
background explaining *why* dispossessed men had cause and cover to fight, not as the thing being
argued. **The chosen thesis is convergent evolution of tactical form under similar constraints,
270 years apart — a contribution to insurgency theory, not a claim of unbroken lineage.**

---

## 2. Mission Contribution {#SS-BRIEF-TIRE-§2}

Which part of NONFICTION's mission does this book advance?

- **[x] The "George Washington's dentures" standard, applied to a national foundation myth.**
  The popular version of both wars flattens them into sentiment — "sixteenth/seventeenth-century
  tyranny" versus "twentieth-century freedom fighters" — when the documentary record shows two
  specific, comparable *tactical systems* responding to specific, comparable material constraints
  (no field army, chronic arms shortage, a hostile-to-ambivalent regular army, a population the
  insurgents depend on and the state tries to punish collectively). Naming the actual mechanism
  under the sentiment, chapter by chapter, is this book's version of the Gospel books' historicity
  work.
- **[x] Spectrum-of-scholarship as historiography, not confessional camps.** No religious
  spectrum applies here (as with 1381). The equivalent spectrum runs **nationalist/republican
  popular memory** (unbroken resistance, "the fight that never stopped") through the
  **constitutional/revisionist correction** (Foster and others: 1649 is confessional/dynastic, not
  proto-national; reading nationalism backward into 1649 is anachronism) to the **comparative
  insurgency-studies reading** (Kalyvas's control/collaboration model; Townshend's institutional-
  memory argument) that treats the two wars as structurally comparable *without* claiming either
  side knew about the other. A chapter is built around watching this spectrum disagree about how
  much continuity is real, never adjudicating "the" answer.
- **[x] EXPANDED SCOPE addition — how folk heroes get made.** A recurring, explicitly comparative
  thread (not a standalone chapter, but a lens applied throughout, paying off hardest in Chapter
  11): watching the *mechanism* by which a costly or outright failed action turns into durable
  legend, across three centuries of different media — Robert Emmet's speech from the dock
  becomes a text nationalists memorize; the Manchester Martyrs become a ballad ("God Save
  Ireland") within months; Michael Dwyer's five-year mountain holdout becomes local oral
  tradition and eventually a road (the Military Road) that still exists; Tom Barry's own memoir
  does the work of legend-making in his own lifetime, on his own terms. The book's honest claim
  is not "these men were legends" but "here is the specific, traceable mechanism (song, memoir,
  monument, road, museum plaque) by which each one became one" — NONFICTION's history-vs-heritage
  method applied to hero-making itself, not just to the underlying events.
- **[ ] None** — not applicable.

---

## 3. Prerequisites {#SS-BRIEF-TIRE-§3}

None. Fully standalone; no GLMZ/SCRY/other-NONFICTION content required. (1381 is a thematic cousin —
same universe, same "heritage vs. history" mission — but TIRE does not depend on it and shares no
entities with it.)

---

## 4. Figure Roster — Entry States {#SS-BRIEF-TIRE-§4}

Not a recurring-cast continuity question (nonfiction). Roster of who this book must seed, with
their state as their respective conflict opens — see §10 for the full entity seeding table.

| Figure | Era | Entry State |
|---|---|---|
| Oliver Cromwell | Cromwellian | Lands at Dublin, August 1649, as Lord Lieutenant and Commander-in-Chief; departs Ireland May 1650, leaving the campaign to subordinates |
| Michael Jones | Cromwellian | Parliamentarian commander holding Dublin; wins the decisive field battle at Rathmines, August 1649, before Cromwell even lands |
| Henry Ireton | Cromwellian | Cromwell's son-in-law; succeeds him as Lord Deputy/Commander-in-Chief in Ireland, 1650; dies of plague at the siege of Limerick, November 1651 |
| Charles Fleetwood | Cromwellian | Succeeds Ireton as Commander-in-Chief, 1652; oversees the settlement/transplantation phase and the anti-tory sweeps |
| Owen Roe O'Neill | Cromwellian | Confederate Ulster army's ablest commander; dies (illness, not battle) November 1649, before Cromwell's campaign is decided |
| Heber MacMahon | Cromwellian | Catholic Bishop of Clogher; assumes command of the leaderless Ulster army after O'Neill's death, against the advice of his own officers |
| Murrough O'Brien, Earl of Inchiquin | Cromwellian | Royalist commander of mixed and much-distrusted loyalty (previously fought for Parliament, switched sides) |
| Edmund O'Dwyer | Cromwellian | Minor Confederate officer before 1652; becomes a tory leader operating out of the Glen of Aherlow after the formal war ends |
| Tom Barry | War of Independence | Returned WWI British Army veteran (Mesopotamia); joins the IRA's Cork No. 3 Brigade, 1920; commands its flying column |
| Dan Breen | War of Independence | Tipperary IRA Volunteer; co-leads the Soloheadbeg ambush, January 1919, conventionally dated as the war's opening action |
| Seán Treacy | War of Independence | Tipperary IRA Volunteer; co-leads Soloheadbeg alongside Breen |
| Ernie O'Malley | War of Independence | IRA GHQ organizer; tours brigades nationwide training flying columns in tactics and doctrine |
| Michael Collins | War of Independence | IRA Director of Intelligence and Adjutant General; runs the counter-intelligence network that dismantles Dublin Castle's detective apparatus |
| Richard Mulcahy | War of Independence | IRA Chief of Staff, GHQ Dublin |
| Cathal Brugha | War of Independence | Dáil Minister for Defence |
| Éamon de Valera | War of Independence | Dáil Éireann President; largely in the United States on a fundraising/diplomatic mission for much of 1919–20 |
| William "Rick" Joyce | War of Independence | Rank-and-file Volunteer, West Mayo Brigade Flying Column under Michael Kilroy — named ("R. Joyce") in the Military Archives' 1920-21 roster/photograph, the same column that fought Tourmakeady, Kilmeena, and Carrowkennedy; individual actions beyond column membership not yet found in available sources — an openly stated gap, not smoothed over |
| William Brooke Joyce | War of Independence | Teenage son of a strongly pro-Unionist Galway family; scouts/informs for British forces against the local IRA — the counterinsurgent-side informer case this book uses (later "Lord Haw-Haw"; his post-1921 career is explicitly out of scope) |

---

## 5. Figure Roster — Exit States {#SS-BRIEF-TIRE-§5}

| Figure | Era | Exit State |
|---|---|---|
| Oliver Cromwell | Cromwellian | Returns to England May 1650 to fight the Scots; never returns to Ireland; dies 1658 |
| Henry Ireton | Cromwellian | Dies of plague/fever outside Limerick, November 1651, campaign still unfinished |
| Charles Fleetwood | Cromwellian | Oversees the Act for the Settlement (1652) and the Connacht transplantation; recalled to England 1655 |
| Heber MacMahon | Cromwellian | Ulster army destroyed at the Battle of Scarrifhollis, June 1650; MacMahon captured and executed shortly after |
| Edmund O'Dwyer | Cromwellian | Exiled to continental military service with his remaining men (the "Wild Geese" pattern) rather than surrendering to summary justice; killed in battle in France, 1654 — a negotiated exit that still ends in death, not a clean contrast to the men who stayed and were hanged |
| Tom Barry | War of Independence | Survives the war; commands the column through Kilmichael (Nov. 1920) and the Crossbarry breakout (March 1921); writes the memoir *Guerilla Days in Ireland* |
| Dan Breen | War of Independence | Survives; writes *My Fight for Irish Freedom* |
| Seán Treacy | War of Independence | Killed in a gun battle in Dublin, October 1920 |
| Michael Collins | War of Independence | Signs the Anglo-Irish Treaty, December 1921 (post-Truce, outside this book's core window); killed in the Civil War at Béal na Bláth, August 1922 — noted only as an epilogue fact, not covered in depth |
| William "Rick" Joyce | War of Independence | Survives as one name among the West Mayo column's photographed roster; no further individually-attested record found — the book's window on him closes exactly where the primary source (the photograph caption) does, deliberately illustrating that even the better-documented war still loses most of its rank and file to the record |
| William Brooke Joyce | War of Independence | Caught by IRA intelligence using an RIC cipher in intercepted correspondence; flees to England within days and enlists in the British Army — the book's window on him closes here, before his later Nazi-propagandist career |
| The tory insurgency (as a phenomenon) | Cromwellian | Not a negotiated end — attrition, transportation, and the slow re-absorption of the dispossessed rather than a single defeat or victory; contrast this directly against the IRA's negotiated Truce/Treaty ending in the closing chapter |

---

## 6–7. Plants / Payoffs {#SS-BRIEF-TIRE-§6-7}

Not applicable in the fiction sense. The one long-range thread this book must pay off internally
(no cross-book dependency): the book opens (Chapter 1) posing the evidence-asymmetry problem —
the 1650s survive almost entirely through the counterinsurgent's archive, the 1919–21 material
overwhelmingly through the insurgent's own testimony (BMH Witness Statements, memoirs) — and must
resolve it explicitly in the closing chapter, not leave it as an unexamined caveat. The closing
chapter's inversion payoff (the state remembers institutionally; the insurgent reinvents each
time) is set up by naming, early, that British counterinsurgency practice in Ireland has a
continuous documentary trail Elizabethan → Cromwellian → Restoration statute → 1920, and is paid
off by tracing that same practice's later export to Palestine, Malaya, and Kenya.

---

## 8. Thematic Complement {#SS-BRIEF-TIRE-§8}

**Theme:** Twice, an Irish rebellion loses its field army in a single afternoon, and twice the
fighting doesn't stop — it changes shape. Both times the same handful of problems reassert
themselves: no rifles, no safe billet without a farmer's risk, a state that answers an ambush by
fining or burning the townland that sheltered it. The book's spine is watching two forces that
never read each other's manuals arrive, 270 years apart, at strikingly similar tactical answers —
and watching exactly where the answers diverge, because the divergences (a counter-state with a
treasury and a treaty-making capacity vs. a leaderless woods-band; a war fought for foreign opinion
vs. a war with no such audience) are where the real argument lives.

**Register:** Same as the rest of NONFICTION — curious, not adversarial; dry wit per `docs/NONFICTION.md`
§3b (rate-limited, never at the fighters', the state's, or the dead's expense); "Then and Now"
closer per chapter (§3c); no invented specifics; genuine open questions (Jack Straw-style identity
puzzles don't recur here, but the Anonimalle-Chronicle-style problem does: which Bureau of
Military History witness statements are reliable given they were taken decades after the events,
by participants building their own legacy) rendered as real uncertainty, not smoothed over.

**EXPANDED SCOPE — hero-centric, not institution-centric (user's explicit direction, 2026-08-03):
"individual heros is more entertaining then just a dry retelling of events."** Every tactical
point this book makes should be carried by a specific named person doing a specific traceable
thing, not by an abstract institutional description. "The tory insurgency relied on collective
punishment of sympathetic baronies" is correct but inert; "Edmund O'Dwyer's men in the Glen of
Aherlow forced Fleetwood's administration to fine the whole barony, and it still didn't stop
them" is the same fact, carried by a person. This governs how every chapter should eventually be
drafted (a future step) — lead with the person and the specific incident, let the tactical/
comparative point emerge from it, never the reverse.

**EXPANDED SCOPE — tangible, holdable primary sources as the recurring texture (user's explicit
direction, 2026-08-03): "letters from family, news paper clippings, registers, real facts that
you can hold in your hands."** Wherever the record permits, ground a chapter's claims in the
specific physical form the evidence survives in — a specific letter, a specific newspaper report
of an execution or a rescue, a specific parish or estate register entry, a specific court or
gaol-delivery record, a specific witness statement page — named and dated, not "the record shows."
This is not a new rule so much as NONFICTION's existing citation discipline (§1) pushed toward its
most concrete, physical expression: the Notes chapter should let a reader picture the actual
document each claim comes from.

**EXPANDED SCOPE — tactics, weapons, food, and culture as a recurring Then-and-Now axis (user's
explicit direction, 2026-08-03): "things that were invented at this time, new tactics or weapons,
or food; who it shaped culture and how those changes resonate even until today."** This maps
directly onto the existing mandatory "Then and Now" closer (`docs/NONFICTION.md` §3c), which already
requires varying its axis chapter to chapter (money, law, medicine, distance, literacy, food,
women's testimony, debt, weather, noise, smell, who gets believed, who gets counted) and forbids
repeating the same observation twice across the book. This book should lean hard on the
material/cultural axis specifically, given how much of its span invents or repurposes concrete
things a modern reader still encounters: the word "boycott" itself (Land War, 1880); "flying
column" as a phrase; the ballad tradition each failed rising leaves behind; the Military Road
(built to hunt Michael Dwyer, still a real road in County Wicklow today); rations and rough field
food across three centuries of men living outdoors on the run. Each Then-and-Now closer should
pick ONE such thread per chapter and follow it to something the reader can recognize now — never
inventing a modern statistic (§3c already forbids this), always a real, citable, traceable
survival.

**HARD RULE, user's explicit instruction 2026-08-03 — even-handedness, not villain/hero framing:**
"make sure to keep it neutral, its easy to make the british out to be villians - but history is
more nuanced then that. One atrocity begets another and nobody gets out of war with their hands
clean." This book must NOT read as a one-sided nationalist narrative of British villainy and
Irish victimhood, across any of the seven rounds. Concretely: every chapter that documents a
Crown/English/British atrocity (Drogheda and Wexford's mass killings, 1649; Mountjoy's
scorched-earth famine tactics against Ulster, 1600-03; the Black and Tan/Auxiliary reprisals,
1920-21) must, in the same chapter or the immediately adjacent one, give equally unflinching
treatment to actions on the Irish/insurgent side that don't fit a clean-hero narrative (targeted
killings of suspected informers without trial, sectarian or ethnically-targeted violence where
the record shows it, the real human cost the Cromwellian-era "discoverer" bounty system and the
War of Independence's own informer-execution policy both inflicted on individuals whose actual
guilt is sometimes genuinely unclear in the record). This is not "both-sidesing" for its own sake
-- it is NONFICTION's existing no-verdict rule (docs/NONFICTION.md SS2) applied with extra discipline to
a subject where the temptation to let one side play villain is unusually strong. Where the record
is genuinely asymmetric (some things really were worse, more frequent, or more systematic on one
side in a given round), say so plainly -- neutrality means accuracy, not manufactured balance.

**No dramatization — same explicit departure as 1381, carried forward.** Per standing project
instruction: this book stays in expository/narrative-nonfiction register throughout —
reconstructed scene-setting grounded entirely in the documentary record, never invented dialogue,
interiority, or sensory detail beyond what a chronicle, memoir, or record actually attests. Where
a source *does* record direct speech or first-person account (Barry's own description of
Crossbarry, a chronicler's account of a tory ambush), it is quoted and sourced as reported speech,
not staged as scene.

**Terminology discipline (stated once, in Chapter 1, then held throughout — corrected after
WebSearch verification, 2026-08-03):** "tory" (from *tóraí*, pursuer) is the term this book uses
for the Cromwellian-era-into-1650s/60s insurgency. "Rapparee" (from *rapaire*, half-pike) is
popularly associated with the Williamite War (1689–91), but the best-documented individual
example, Redmond O'Hanlon (c. 1640 – 25 April 1681), was already dead seven years before that war
began — the Dictionary of Irish Biography itself describes him as "a tóraidhe or rapparee,"
i.e. the terms already overlapped in his own, Restoration-era lifetime (he was active in Ulster
in the late 1670s/early 1680s). The clean two-term split (tory=Cromwellian, rapparee=Williamite)
the earlier planning conversation proposed does not survive contact with the record; this book
states the more honest version instead — the terminology shifted gradually across the
Restoration interval, and O'Hanlon is seeded explicitly as a **Restoration-era** figure (neither
Cromwellian-phase nor Williamite-War-era) precisely to make that overlap visible rather than
paper over it, per NONFICTION's genuine-uncertainty standard.

**What would be duplicated if this book didn't exist:** nothing — 1381 examines a single English
medieval uprising; TIRE is the first NONFICTION book built as a structural comparison across two
periods, and the first to engage guerrilla/insurgency-studies theory (Kalyvas) as an explicit
analytical frame rather than pure narrative history.

---

## 9. Structural Blueprint Seed {#SS-BRIEF-TIRE-§9}

**Resolution mode:** External/situational, and asymmetric between the two wars — the tory
insurgency ends in attrition and transportation with no negotiated settlement; the War of
Independence ends in a negotiated Truce and Treaty. The book does not resolve which "worked
better" (that's a category error — the political contexts aren't equivalent) and states plainly
that it's a category error.

**No moral-polarity/POV apparatus** — unlike TFAH, this book carries no committed narrative
"side." It compares tactical systems; it does not adjudicate the justice of either war.

**Organizing principle — BY TACTICAL PROBLEM, NOT CHRONOLOGICAL HALVES.** Each numbered chapter
(after the introduction) takes one shared tactical/structural problem and runs both eras through
it in the same chapter, so the comparison is continuous rather than two stapled-together
narratives read side by side only in a conclusion.

**Chapter spine (11 numbered chapters + 4 trailing structural chapters + Notes + Glossary) —
EXPANDED SCOPE: chapter titles/nodes as already created in the DB are listed first; each entry
now notes which of the six rounds (§1) it threads in beyond the two original anchors. Chapter 1
and 2's DB titles still read "Two Wars..." from the original build — RENAME THESE before drafting
body prose (see Checklist); the content plan below already reflects the widened scope:**

1. *(rename pending — was "Two Wars, One Question")* — method, terminology (tory vs. rapparee,
   and the Restoration-era overlap the O'Hanlon research surfaced), and the evidence-asymmetry
   problem stated up front. EXPANDED: introduce the full six-round span here, and preview the
   folk-hero-genesis lens (§2) as a thread the reader should watch for throughout.
2. *(rename pending — was "After the Field Armies Died")* — second-phase warfare born of the same
   disaster. EXPANDED: adds 1798 (Vinegar Hill) and Michael Dwyer's Wicklow campaign as a THIRD
   data point between Rathmines/Scarrifhollis (1649-50) and 1916 -- Dwyer's is the longest single
   guerrilla holdout in the book, a structural bridge case.
3. The Quartermaster Is the Enemy — ambush as a capture economy. Two-anchor case study stays
   central (Kilmichael/Crossbarry vs. tory raiding); 1798/1803/1867 add contrast cases where
   capture-economy logic does NOT apply (Vinegar Hill and Emmet's rising were conventional-style
   confrontations, not ambush campaigns) — a useful negative case.
4. Wood, Bog, and Motor Lorry — the mobility contest. EXPANDED: Michael Dwyer's Wicklow terrain
   and the British Military Road (built specifically to reach him) becomes a THIRD case between
   the Cromwellian tories and the War of Independence columns — the road itself still exists.
5. Eyes in the Barony — informers, intelligence, counterintelligence. EXPANDED: William Brooke
   Joyce's Galway informer case (Michael Collins's counter-intelligence war) sits alongside the
   Fenian Rising's informer-riddled 1867 failure (Comerford) as a second, earlier case of an
   entire rising collapsing on bad information security.
6. The Barony Pays — collective reprisal. Two-anchor case study (Cromwellian fines vs. 1920s
   reprisals) stays central; Land War-era "agrarian outrage" and boycotting (Davitt) offers a
   contrast case of *civil*, non-violent collective pressure achieving what armed reprisal could not.
7. A Counter-State, and Its Absence — Confederation of Kilkenny vs. the Dáil. EXPANDED: the Land
   League (Davitt, 1879-82) added as a THIRD model -- neither armed remnant nor wartime
   counter-government, but an open, legal, mass civil organization.
8. Ungovernable, Not Defeated — theory of victory. EXPANDED: contrast the War of Independence's
   foreign-audience strategy against Wolfe Tone's actual foreign-military-alliance strategy
   (France, 1798) -- two very different ways of NOT fighting the war alone.
9. Custom and Doctrine — oral transmission vs. written doctrine. Two-anchor case study stays
   central (tories vs. *An t-Óglach*/O'Malley).
10. The Archive Problem — revisited directly, now spanning all six rounds: a state's court
    records and bounty warrants (1650s); a Rebellion-centre's uncovered primary sources
    (O'Donnell's Wicklow work, drawing on previously unused Irish/British/Australian archives);
    a single legendary speech that outlives its own trial transcript (Emmet); a folk ballad
    outliving the event that produced it (the Manchester Martyrs); a movement leader's own
    750-page memoir (Davitt); and the Bureau of Military History's decades-later testimony
    (1919-21). Six different evidentiary problems, one running argument.
11. What the State Remembered — the inversion and the book's real argument, now also paying off
    the folk-hero-genesis thread (§2): British counterinsurgency practice in Ireland has a
    continuous institutional memory (Elizabethan → Cromwellian → Restoration statute → 1798 →
    1867 → 1920) later exported to Palestine, Malaya, and Kenya, while the insurgents, each time,
    reinvent the tactical form from nothing -- but the *insurgents* are the ones who get
    remembered as individuals, in song and legend, while the state's continuity stays
    institutional and comparatively anonymous. That asymmetry, not just the tactical one, is the
    closing argument.
12. (trailing) A Gazetteer of Three Centuries — Glen of Aherlow, Rathmines, Scarrifhollis,
    Drogheda, Wexford, Connacht; Vinegar Hill, Glen of Imaal, Thomas Street (Dublin), Manchester,
    County Mayo; Soloheadbeg, Kilmichael, Crossbarry, Dublin Castle — with coordinates
13. (trailing) What Survives — Dunlop, O Siochru, Pakenham, O'Donnell (x2), Elliott, Comerford,
    Davitt's own memoir, the Bureau of Military History Witness Statements, Barry's *Guerilla
    Days in Ireland*, Breen's *My Fight for Irish Freedom*, O'Malley's *On Another Man's Wound*,
    *An t-Óglach*
14. (trailing) The Theoretical Frame — Kalyvas's *The Logic of Violence in Civil War* and
    Townshend's *The British Campaign in Ireland 1919–1921*, the two secondary works whose
    analytical apparatus the book borrows explicitly
15. Notes — 17 entries as of 2026-08-03 (see below)
16. Glossary — 41 entries as of 2026-08-03 (see below)

**Event-type palette:** ambush-for-materiel / punitive reprisal / intelligence catch / negotiated
terms / battlefield rout / show trial / column concentration-and-dispersal — cycled across both
eras per chapter, not one invented dramatic palette.

**Intertextual anchors (grounding the comparative method, not narrative style):**
1. Charles Townshend, *The British Campaign in Ireland 1919–1921* (Oxford University Press,
   1975) — the institutional-memory argument this book's closing chapter borrows and extends
   backward across three centuries
2. Stathis N. Kalyvas, *The Logic of Violence in Civil War* (Cambridge University Press, 2006) —
   the control/collaboration/collective-reprisal theoretical model applied throughout
3. Tom Barry, *Guerilla Days in Ireland* (The Irish Press, 1949; Anvil Books paperback from 1962) —
   the primary first-person tactical account of the ambush-as-capture-economy doctrine
4. Robert Dunlop (ed.), *Ireland Under the Commonwealth* (Manchester University Press, 1913) —
   the documentary calendar for the Cromwellian settlement/tory-suppression administrative record
5. *(EXPANDED SCOPE, added 2026-08-03)* Ruán O'Donnell's two-volume Wicklow study and Marianne
   Elliott's Emmet biography — the model for how this book treats failure-into-legend across the
   1798-1803 rounds specifically

**Subplot thread:** not applicable in the fiction sense — the "thematically parallel carrier" this
book uses instead is a pair of small, sharply human counterpoints against the book's otherwise
systems-level comparison: William Brooke Joyce's case (Ch. 5), one teenager's actual, documented
experience of being on the losing end of an intelligence catch, and William "Rick" Joyce's case
(Ch. 10), a named Volunteer whose entire surviving record is a line in a photograph caption —
together showing that even the war with the richer archive still individually erases most of the
people who fought it.

**Form device:** none — straight comparative-thematic chapter structure, no frame narrative.

---

## 10. Entity Seeding Required {#SS-BRIEF-TIRE-§10}

### People — Cromwellian era (seeded)
Oliver Cromwell, Michael Jones, Henry Ireton, Charles Fleetwood, Owen Roe O'Neill, Heber MacMahon,
Murrough O'Brien (Earl of Inchiquin), Edmund O'Dwyer, Redmond O'Hanlon (Restoration-era contrast,
not strictly Cromwellian or Williamite — see corrected terminology note in §8).

### People — 1798/1803 (EXPANDED SCOPE, seeded 2026-08-03)
Theobald Wolfe Tone, Father John Murphy, Michael Dwyer (the Wicklow guerrilla holdout — a
structural bridge figure between the Cromwellian and War of Independence anchors), Robert Emmet.

### People — Fenian Rising / Land War (EXPANDED SCOPE, seeded 2026-08-03)
James Stephens, Thomas J. Kelly, the Manchester Martyrs (William Allen, Michael Larkin, Michael
O'Brien — seeded as one grouped entity), Michael Davitt.

### People — War of Independence era (seeded)
Tom Barry, Dan Breen, Seán Treacy, Ernie O'Malley, Michael Collins, Richard Mulcahy, Cathal Brugha,
Éamon de Valera, William "Rick" Joyce (West Mayo Flying Column Volunteer). **William Brooke Joyce
(the Galway informer case, later "Lord Haw-Haw") was seeded, then REMOVED at the user's explicit
instruction ("not important aside") — his Character entity, Glossary beat, and Notes citation
were all deleted/disabled 2026-08-03. Do not re-add him without the user asking again.**

### Places (seeded)
Glen of Aherlow, Rathmines, Scarrifhollis, Drogheda, Wexford, Connacht; Vinegar Hill, Glen of Imaal
(Wicklow Mountains), Thomas Street (Dublin), Manchester, County Mayo (Land League origin);
Soloheadbeg, Kilmichael, Crossbarry, Dublin Castle. **Galway was seeded (for the Joyce informer
case) then REMOVED alongside William Brooke Joyce — see above.**

### Documents / Sources (17 Notes as of 2026-08-03)
Robert Dunlop's *Ireland Under the Commonwealth* [1]; the Bureau of Military History Witness
Statements [2]; Tom Barry's *Guerilla Days in Ireland* [3]; Dan Breen's *My Fight for Irish
Freedom* [4]; Ernie O'Malley's *On Another Man's Wound* [5]; *An t-Óglach* [6]; Charles
Townshend's *The British Campaign in Ireland 1919–1921* [7]; Stathis Kalyvas's *The Logic of
Violence in Civil War* [8]; Micheál Ó Siochrú's *God's Executioner* [9]; the Military Archives'
West Mayo Flying Column photograph [10]; Thomas Pakenham's *The Year of Liberty* (1798) [12];
Ruán O'Donnell's *The Rebellion in Wicklow, 1798* [13] and *Aftermath: Post-Rebellion Insurgency
in Wicklow, 1799-1803* [14]; Marianne Elliott's *Robert Emmet: The Making of a Legend* [15]; R. V.
Comerford's *The Fenians in Context* [16]; Michael Davitt's own memoir *The Fall of Feudalism in
Ireland* [17]. (Note [11], the Dictionary of Irish Biography's William Brooke Joyce entry, was
inserted then deleted alongside his removal — the numbering has a deliberate gap at 11.)

### Terms
Tory (*tóraí*), rapparee (*rapaire* — Restoration-era overlap with "tory," not a clean
Williamite-only split), flying column, collective/official reprisal, boycott (coined 1880, Land
War), the Act for the Settlement of Ireland (1652) and the Connacht transplantation (background
driver only).

Seeded via `ss --add-character --dir` / `ss --add-place --dir` (batch mode — one JSON file per
entity, imported in one process). Checked `Entities` first (confirmed clean before the original
seeding pass). Every figure's `Description` carries citation-backed detail per `docs/NONFICTION.md`
§1b — the `Description` IS the Glossary-tier content, and 41 corresponding Glossary beats plus
17 Notes beats are already live in the DB (Glossary/Notes chapters under the TIRE BookNode) — see
Checklist.

---

## Checklist Before Proceeding

- [x] All 10 sections filled (revised in place 2026-08-03 for the 300-year scope expansion)
- [x] Core facts WebSearch-verified per claim as entities/Notes were drafted (docs/NONFICTION.md §5d)
      — Michael Dwyer, 1798 rising, Emmet, Fenian Rising, Land War, and both Joyce candidates all
      independently verified before writing; the William Brooke Joyce case was subsequently
      removed at user instruction, not for accuracy reasons
- [x] Entity seeding — **DONE**: 27 characters (19 original + 8 EXPANDED SCOPE, minus 1 removed
      = 26 live) + 16 places (11 original + 5 EXPANDED SCOPE, minus 1 removed = 15 live) all
      seeded via `ss --add-character --dir` / `ss --add-place --dir`, `--universe source`
- [x] BookNode `TIRE` created, Id `019fc926-c5d9-7663-b08f-fbfb82b43219`,
      Author = "Ars Historica" (secular-history NONFICTION imprint, matching 1381/JOAN precedent —
      NOT "Pulpit Press"), NodeCode = `TIRE`. Title currently a working-title placeholder pending
      the user's final naming decision (candidates floated: *Three Hundred Years a Rebel*, *The
      Long Sparring*, *Held in the Hand*, *Ambush Country* — user said "keep working, I'll think
      about it").
- [x] ChapterNodes created (11 numbered + Gazetteer + What Survives + Theoretical Frame + Notes
      + Glossary), SortKey-ordered and verified. **OUTSTANDING: Chapter 1 and Chapter 2's DB
      titles still read "Two Wars, One Question" / "After the Field Armies Died" from the
      original two-war build — Chapter 1's title needs renaming to reflect the six-round scope
      before body prose is drafted (see §9); Chapter 2's title still works as-is.**
- [ ] Node bible hand-authored (arc, chapter spine, locks, no-dramatization rule, terminology
      discipline) — **NOT YET DONE.** MCP `set_book_bible` is unavailable this session (no MCP
      server connected); requires either a direct `NodeBibleSection` SQL write or a future
      session with MCP access. This brief is the interim source of truth for bible content.
- [ ] Chapter prose written, one beat per checkable claim cluster, Then and Now closers (0/11) —
      **NOT YET DONE**, and NOT attempted this session — a future, separately-scoped task per
      chapter, following the hero-centric register (§8) and the six-round comparative slotting
      already planned per chapter (§9)
- [x] Notes chapter populated — **17 entries live** (numbered 1-10, 12-17; 11 deliberately
      retired, see §10), archival-complete citations per docs/NONFICTION.md §1a
- [x] Glossary chapter populated — **41 entries live**, every live seeded entity has a
      corresponding beat with "Cited in:" tags; coordinates included for every place per user
      instruction
- [ ] Export + QA — not started; blocked on chapter prose (currently zero body chapters have
      any beats beyond Notes/Glossary)
- [ ] Structural blueprint (`ss --generate-blueprint --slug tyranny-s-long-war-guerrilla-ireland-
      1649-1921-019fc926 --universe source`) — queued as this session's next step
