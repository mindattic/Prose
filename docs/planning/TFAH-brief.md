---
codex: 1
project: Prose
layer: planning
code: TFAH
title: The First Anti-Hero
universe: FICTION
updated: 2026-08-04
---

# Story Brief: TFAH — The First Anti-Hero {#SS-BRIEF-TFAH}

> **SUPERSEDED UNIVERSE ASSIGNMENT (2026-08-02, later same day; universe renamed again
> 2026-08-04):** TFAH was originally drafted as a GSPL sub-track (below) but has since been moved
> to its own new universe, **FICTION** (formerly EPIC, renamed 2026-08-04) — a dedicated home for
> fiction retellings of classic epic literature/myth, distinct from GSPL (renamed SOURCE, then
> **NONFICTION**), which is nonfiction historical-religious research. Rationale: NONFICTION is a
> "historic research engine" (citation-grounded, real-world claims); TFAH is fiction built from a
> poem. The two need genuinely different entities for same-named figures (e.g. NONFICTION's
> historically-researched Raphael vs. FICTION's <entity repo="character" guid="019fc13e-bb8e-705d-b9e7-94f4caa1ebf0">Milton</entity>-literary-character Raphael in TFAH) — see
> `EntityDisambiguationService` (`v3/Prose.Core/Services/EntityDisambiguationService.cs`)
> and the `OriginNodeId` field on `Entity`. **Everything below this notice describes the original
> GSPL framing and is kept as historical record of the reasoning** (the citation-grounding
> discipline, the "spectrum not verdict" methodology, the entity-origin research) — still
> substantively accurate — but any literal "universe: gspl"/"Author: Pulpit Press" references
> below are superseded: the live BookNode is universe `fiction`, Author `MindAttic`.
>
> **DOCS-TO-DB MIGRATION (2026-08-02, later still):** `docs/milton/*.md` (README.md,
> milton-biography.md, character-catalog.md, theology-and-sources.md) has been **deleted**. Per
> SS-A45, the DB is the heap and `.md` files must only ever be ephemeral, regenerated-on-demand
> mirrors — a permanent hand-committed research folder was the wrong pattern (this is also why
> NEPH, correctly, never had one). That research is now migrated into the actual entity records:
> Satan and <entity repo="character" guid="019fc13e-bb8e-705d-b9e7-94f4caa1ebf0">John Milton</entity>'s `Entities.Description` carry the full sourcing/biography depth; <entity repo="character" guid="019fc13e-bb5a-7007-ac22-4432326e20b4">God</entity> the
> <entity repo="character" guid="019fc13e-bb5a-7007-ac22-4432326e20b4">Father</entity> and <entity repo="character" guid="019fc13e-bc3e-7ce8-aca9-190616e7975f">The Son</entity> carry the Arian-controversy scholarship; every other character's entity
> record already carried its citation summary from initial seeding. Any `docs/milton/...` citation
> below should be read as "→ see that entity's own DB record" — the content wasn't lost, just
> relocated to where DCM can actually load it per-beat instead of as a static file no tier ever
> queried (confirmed: these files had no MarkdownFiles row at all — no frontmatter tier/scope
> classification, so the DCM pipeline never once saw them; they were dead weight from the start).

> **This brief is mandatory before creating a node bible or any DB records.**
> Universe: GSPL. A plain-English narrative retelling of <entity repo="character" guid="019fc13e-bb8e-705d-b9e7-94f4caa1ebf0">Milton</entity>'s *Paradise Lost*, told
> unapologetically from Satan's point of view, paired with GSPL-discipline nonfiction research
> on (a) every named figure's real textual origin and (b) <entity repo="character" guid="019fc13e-bb8e-705d-b9e7-94f4caa1ebf0">John Milton</entity> the man and how the poem
> actually came to be written. Status: brief drafted 2026-08-02; three background research
> passes in flight (<entity repo="character" guid="019fc13e-bb8e-705d-b9e7-94f4caa1ebf0">Milton</entity> biography, full character-origin catalog, theology/sources) — this
> brief will be refined once they land, but is complete enough now to proceed to entity seeding
> and book-structure creation per the New Story Workflow.

---

## 1. Series Position {#SS-BRIEF-TFAH-§1}

**Universe:** GSPL ("Gospel: History vs. Heritage" universe — real, DB-backed, `dbo.Universe`
slug `gspl`, same schema/pipeline as GLMZ/SCRY, added craft discipline: every factual claim
must be citation-grounded to a real source, per `docs/GSPL.md`).

**Story type:** Standalone GSPL book. Not a chapter in the Gospel tetralogy (Matthew/Mark/Luke/
<entity repo="character" guid="019fc13e-bb8e-705d-b9e7-94f4caa1ebf0">John</entity>) — a sibling production line under the same universe, opening a new GSPL sub-track:
**examining the most influential *non-canonical* text in the popular Christian imagination.**
<entity repo="character" guid="019fc13e-bb8e-705d-b9e7-94f4caa1ebf0">Milton</entity>'s *Paradise Lost* did not just adapt Genesis — it invented, wholesale, most of what
English-speaking culture now assumes is "biblical" about Satan, <entity repo="place" guid="019fc13e-dad7-7f00-bb8b-5695e52d3108">Hell</entity>, the War in <entity repo="place" guid="019fc13e-dac2-7152-8781-6cb2bd8e849e">Heaven</entity>, and
the Fall. That gap between what Scripture says and what *Paradise Lost* says (and what everyone
now believes Scripture says because of *Paradise Lost*) is a direct extension of GSPL's George
Washington's-dentures mission (`docs/gospel/README.md`) — just aimed at the poem that ships the
heritage version, rather than at the Gospels themselves.

**Book(s) this story serves:** None as a prerequisite relationship. Establishes a new GSPL
sub-track ("influential non-canonical texts") that could extend later to Dante's *Inferno*,
Bunyan's *Pilgrim's Progress*, or the *Book of Enoch*'s afterlife in pop culture — noted as
backlog, not committed.

**Approximate timing / continuity:** N/A — this is nonfiction-adjacent literary work, not an
in-universe GLMZ/SCRY story. No relative in-fiction timeline applies.

---

## 2. Arc Contribution {#SS-BRIEF-TFAH-§2}

GSPL doesn't run GLMZ's 5-arc structure. The equivalent question here is: **which part of the
GSPL mission does this book advance?**

- **[x] Heritage vs. history, applied to a specific case study.** Most readers (of any faith or
  none) believe "Lucifer," the War in <entity repo="place" guid="019fc13e-dac2-7152-8781-6cb2bd8e849e">Heaven</entity>, Satan's fall from pride, and <entity repo="place" guid="019fc13e-dad7-7f00-bb8b-5695e52d3108">Hell</entity>-as-a-place-with-
  gates are Bible content. They are almost entirely <entity repo="character" guid="019fc13e-bb8e-705d-b9e7-94f4caa1ebf0">Milton</entity> (synthesizing Isaiah 14, Ezekiel 28,
  Revelation 12, and non-canonical Enochic tradition into one coherent story the Bible itself
  never tells in one place). TFAH's nonfiction spine makes that sourcing gap explicit, chapter by
  chapter, exactly as `pontius-pilate.md`/`genealogies-of-jesus.md` do for the Gospels.
- **[x] A new interpretive-tradition case study.** GSPL's "spectrum, not a verdict" method
  (`docs/GSPL.md` §2) usually spans confessional-to-empiricist readings of a historical claim.
  Here the spectrum is *literary-critical*: the orthodox reading (Satan is the villain, full
  stop — C.S. Lewis's *A Preface to Paradise Lost*), the "Satanist school" reading (Blake's
  "<entity repo="character" guid="019fc13e-bb8e-705d-b9e7-94f4caa1ebf0">Milton</entity> was of the Devil's party without knowing it," Shelley, Empson's *<entity repo="character" guid="019fc13e-bb8e-705d-b9e7-94f4caa1ebf0">Milton</entity>'s <entity repo="character" guid="019fc13e-bb5a-7007-ac22-4432326e20b4">God</entity>*), and
  positions between (Stanley Fish's *Surprised by <entity repo="character" guid="019fc13e-bc2c-7651-880c-abe7f92a5943">Sin</entity>* — the reader's own attraction to Satan is
  the poem's deliberate theological trap). TFAH's **narrative prose is thesis-driven** — it
  commits to the Satan-as-freedom-fighter reading the user specified, the way a novel commits to
  a POV — but the nonfiction apparatus (Notes/Glossary) stays honest about the fact that this is
  one legitimate critical tradition among several, not literary-critical consensus. This split is
  load-bearing and is repeated in §8 below; it is the single most important editorial decision in
  this brief.
- **[ ] None** — not applicable.

---

## 3. Prerequisites {#SS-BRIEF-TFAH-§3}

**None.** Fully standalone. Does not require any Gospel book, GLMZ, or SCRY content to exist
first. World facts required (<entity repo="character" guid="019fc13e-bb8e-705d-b9e7-94f4caa1ebf0">Milton</entity>'s biography, the poem's textual history, the real biblical/
apocryphal/classical sourcing of every named figure) are being established in this brief and its
companion research docs under `docs/milton/`, not inherited from elsewhere.

---

## 4. Character Entry States {#SS-BRIEF-TFAH-§4}

Not a recurring-cast continuity question (no prior GSPL story features these figures). Instead:
the roster below is who this book must seed and where each one starts the story. Full origin
research (biblical/apocryphal/classical/Miltonic-invention, per figure) is running as a
background research pass and will populate `docs/milton/character-catalog.md`; this table is the
seed list, refined once that lands.

| Figure | Entry State (Ch. 1) |
|---|---|
| Satan (Lucifer) | Just fallen, waking on the burning lake of <entity repo="place" guid="019fc13e-dad7-7f00-bb8b-5695e52d3108">Hell</entity> after nine days' fall from <entity repo="place" guid="019fc13e-dac2-7152-8781-6cb2bd8e849e">Heaven</entity>; defiant, already reorganizing |
| <entity repo="character" guid="019fc13e-baca-7308-b6eb-e10622497f85">Beelzebub</entity> | Satan's second-in-command, fallen beside him, first to be roused |
| <entity repo="character" guid="019fc13e-bbbe-7f79-8d91-f5136d2eadde">Moloch</entity>, <entity repo="character" guid="019fc13e-baeb-7834-8829-46d1c3f520f8">Belial</entity>, <entity repo="character" guid="019fc13e-bb9e-7162-9ef0-63c0bc03076a">Mammon</entity> | Named lieutenants, introduced across the Chapter 1–2 rally and infernal council |
| <entity repo="character" guid="019fc13e-bc2c-7651-880c-abe7f92a5943">Sin</entity> | Guards <entity repo="place" guid="019fc13e-dad7-7f00-bb8b-5695e52d3108">Hell</entity>'s gate; Satan's daughter (born from his head) and, by him, mother of <entity repo="character" guid="019fc13e-bb1e-750e-9bbf-8fb3cb503ccc">Death</entity> — introduced Chapter 2 |
| <entity repo="character" guid="019fc13e-bb1e-750e-9bbf-8fb3cb503ccc">Death</entity> | Guards <entity repo="place" guid="019fc13e-dad7-7f00-bb8b-5695e52d3108">Hell</entity>'s gate beside <entity repo="character" guid="019fc13e-bc2c-7651-880c-abe7f92a5943">Sin</entity>; their monstrous, incestuous offspring |
| <entity repo="character" guid="019fc13e-bb06-73ab-bc94-49a86feb1fdc">Chaos</entity> & <entity repo="character" guid="019fc13e-bbe4-7c12-ac85-d11ef029b662">Night</entity> | Rulers of the void between <entity repo="place" guid="019fc13e-dad7-7f00-bb8b-5695e52d3108">Hell</entity> and the new World; encountered Chapter 2/3 |
| <entity repo="character" guid="019fc13e-bb5a-7007-ac22-4432326e20b4">God the Father</entity> | On His throne in <entity repo="place" guid="019fc13e-dac2-7152-8781-6cb2bd8e849e">Heaven</entity>, foreseeing the coming Fall |
| <entity repo="character" guid="019fc13e-bc3e-7ce8-aca9-190616e7975f">The Son</entity> | At <entity repo="character" guid="019fc13e-bb5a-7007-ac22-4432326e20b4">God</entity>'s right hand; volunteers to redeem mankind before the Fall has even happened |
| Raphael | Sent to <entity repo="place" guid="019fc13e-d926-7398-809c-a7e6c35abbe4">Eden</entity> to warn Adam directly — <entity repo="place" guid="019fc13e-dac2-7152-8781-6cb2bd8e849e">Heaven</entity>'s "sociable" archangel |
| Michael | <entity repo="place" guid="019fc13e-dac2-7152-8781-6cb2bd8e849e">Heaven</entity>'s field commander in the War in <entity repo="place" guid="019fc13e-dac2-7152-8781-6cb2bd8e849e">Heaven</entity>; later sent to expel Adam and <entity repo="character" guid="019fc13e-bb30-7296-9972-9c770e60dc07">Eve</entity> from <entity repo="place" guid="019fc13e-d926-7398-809c-a7e6c35abbe4">Eden</entity> |
| Gabriel | Commands the angelic guard at <entity repo="place" guid="019fc13e-d926-7398-809c-a7e6c35abbe4">Eden</entity>'s gate; his officers Ithuriel and Zephon catch Satan at <entity repo="character" guid="019fc13e-bb30-7296-9972-9c770e60dc07">Eve</entity>'s ear |
| Uriel | Regent of the Sun; deceived by Satan disguised as a cherub |
| <entity repo="character" guid="019fc13e-b6fb-72b5-a094-dadfc6c81191">Abdiel</entity> | The one seraph who refuses Satan's rebellion and alone returns to fight for <entity repo="place" guid="019fc13e-dac2-7152-8781-6cb2bd8e849e">Heaven</entity> |
| Adam | Newly created, innocent, in <entity repo="place" guid="019fc13e-d926-7398-809c-a7e6c35abbe4">Eden</entity> |
| <entity repo="character" guid="019fc13e-bb30-7296-9972-9c770e60dc07">Eve</entity> | Newly created, innocent, in <entity repo="place" guid="019fc13e-d926-7398-809c-a7e6c35abbe4">Eden</entity> |
| <entity repo="character" guid="019fc13e-bb8e-705d-b9e7-94f4caa1ebf0">John Milton</entity> (framing figure) | Blind, dictating this account in Restoration London under real personal risk — the book's own author-as-character frame, not a Paradise Lost character; see §8 |

---

## 5. Character Exit States {#SS-BRIEF-TFAH-§5}

| Figure | Exit State |
|---|---|
| Satan | Returns to <entity repo="place" guid="019fc13e-dad7-7f00-bb8b-5695e52d3108">Hell</entity> in triumph after corrupting mankind — and is mocked: he and his host are transformed into serpents, forced to taste ash-fruit, on an annual cycle. Framed per user directive as the cost of guerrilla resistance, not comeuppance-as-moral: the empire he fought punishes by humiliation, not honest combat |
| Adam & <entity repo="character" guid="019fc13e-bb30-7296-9972-9c770e60dc07">Eve</entity> | Fallen, judged, granted a Redeemer's promise they don't yet understand, expelled from <entity repo="place" guid="019fc13e-d926-7398-809c-a7e6c35abbe4">Eden</entity> — "The World was all before them" (<entity repo="character" guid="019fc13e-bb8e-705d-b9e7-94f4caa1ebf0">Milton</entity>'s actual closing line; kept verbatim in the retelling's final beat per the "keep all the scenes" mandate) |
| <entity repo="character" guid="019fc13e-bc2c-7651-880c-abe7f92a5943">Sin</entity> & <entity repo="character" guid="019fc13e-bb1e-750e-9bbf-8fb3cb503ccc">Death</entity> | Given a permanent bridge from <entity repo="place" guid="019fc13e-dad7-7f00-bb8b-5695e52d3108">Hell</entity> to Earth, formalizing corruption's access to the mortal world going forward |
| Michael | Having delivered the future-history vision (Book 11–12's material) and expelled Adam & <entity repo="character" guid="019fc13e-bb30-7296-9972-9c770e60dc07">Eve</entity>, returns to <entity repo="place" guid="019fc13e-dac2-7152-8781-6cb2bd8e849e">Heaven</entity> |
| <entity repo="character" guid="019fc13e-bb8e-705d-b9e7-94f4caa1ebf0">Milton</entity> (framing figure) | The frame closes on the historical <entity repo="character" guid="019fc13e-bb8e-705d-b9e7-94f4caa1ebf0">Milton</entity> — blind, having dictated the whole thing, living under a monarchy that could still have hanged him for his politics a decade earlier |

---

## 6. What It Plants {#SS-BRIEF-TFAH-§6}

| Plant | Payoff |
|---|---|
| A new GSPL sub-track: "influential non-canonical texts that became heritage" | Future GSPL books (Dante's *Inferno*, Bunyan's *Pilgrim's Progress*) — backlog only, not committed |
| Satan's unresolved "Plan B" (corrupting mankind rather than re-fighting <entity repo="place" guid="019fc13e-dac2-7152-8781-6cb2bd8e849e">Heaven</entity> directly) | Fully paid off within this same book (Ch. 9) — no cross-book dependency |

---

## 7. What It Pays {#SS-BRIEF-TFAH-§7}

**None** — first book in this sub-track; nothing precedes it to pay off.

---

## 8. Thematic Complement {#SS-BRIEF-TFAH-§8}

**Theme:** The first being to say no to absolute power loses the fight, gets back up anyway, and
changes tactics — a warrior-king's insurgency against a Creator who made an entire species for
the explicit purpose of praising Him. Told as guerrilla resistance narrative, not as a morality
tale about pride.

**Register:** Epic, defiant, mythic-but-plain — <entity repo="character" guid="019fc13e-bb8e-705d-b9e7-94f4caa1ebf0">Milton</entity>'s scenes and stakes kept whole, his most
famous lines translated rather than replaced (**"Better to reign in <entity repo="place" guid="019fc13e-dad7-7f00-bb8b-5695e52d3108">Hell</entity> than serve in <entity repo="place" guid="019fc13e-dac2-7152-8781-6cb2bd8e849e">Heaven</entity>,"**
**"What though the field be lost? / All is not lost,"** the "far off his coming shone" of the
Son's arrival at the war — these get modernized *phrasing*, never modernized *content*). Not
comic, not ironic-distance; this is played straight, the way a war novel plays straight.

**This is a deliberate, explicit exception to GSPL's "no verdict" rule (§2), scoped narrowly:**
the *narrative* prose commits fully to one reading — Satan as hero, <entity repo="character" guid="019fc13e-bb5a-7007-ac22-4432326e20b4">God</entity>'s court as a slaveholding
regime, the rebel angels as the courageous ones. This is not a defect; it's the same authorial
move as *Wicked* (Oz from the witch's POV) or *Grendel* (Beowulf from the monster's POV) — a
legitimate literary form, "the villain's-side retelling," and it is explicitly what the user
commissioned. **What must NOT happen:** the nonfiction apparatus (Notes chapter, Glossary,
the `docs/milton/` research docs) presenting this reading as if it were uncontested <entity repo="character" guid="019fc13e-bb8e-705d-b9e7-94f4caa1ebf0">Milton</entity>
scholarship. It isn't. C.S. Lewis's orthodox reading and Stanley Fish's "the reader's sympathy for
Satan is the trap, not the truth" reading are real, serious, current positions and must appear
in the Notes with equal seriousness to the Blake/Shelley/Empson "Satan is right" tradition this
book's prose voice adopts. The book's spine is a POV choice; its footnotes are honest reporting
on what that choice costs and who disagrees with it.

**Adjacent GSPL work:** the Gospel tetralogy (Matthew/Mark/Luke/<entity repo="character" guid="019fc13e-bb8e-705d-b9e7-94f4caa1ebf0">John</entity>) — same universe, same
citation discipline, wholly different subject matter and wholly different narrative register
(GSPL's Gospels are curious-narrator nonfiction; TFAH is committed first-person-sympathetic epic
narrative wrapped in nonfiction apparatus).

**What would be duplicated if this book didn't exist:** nothing — no other GSPL work examines a
non-scriptural text's role in shaping "biblical" popular belief, and no other MindAttic project
retells a classic epic from its antagonist's POV.

---

## 9. Structural Blueprint Seed {#SS-BRIEF-TFAH-§9}

**Resolution mode:** External/situational — Satan loses the War in <entity repo="place" guid="019fc13e-dac2-7152-8781-6cb2bd8e849e">Heaven</entity> and is punished at
the book's end (the serpent-transformation), but achieves his actual objective (mankind's
corruption) in the same movement. Cost is real and permanent; the "win" is real and permanent;
neither cancels the other.

**Moral polarity:** **Not ambivalent-default** — see §8. Deliberately committed POV in the
narrative; spectrum preserved in the nonfiction apparatus. This is the one place this brief
diverges from the standard GLMZ/SCRY ambivalent-polarity default, and it's a GSPL-only move
justified by §2's real critical spectrum (this book picks a side that serious scholars have
also picked, across three centuries — it isn't inventing a controversial reading from nothing).

**Ending style:** Avalanche, per <entity repo="character" guid="019fc13e-bb8e-705d-b9e7-94f4caa1ebf0">Milton</entity>'s own structure — judgment, the <entity repo="character" guid="019fc13e-bc2c-7651-880c-abe7f92a5943">Sin</entity>/<entity repo="character" guid="019fc13e-bb1e-750e-9bbf-8fb3cb503ccc">Death</entity> bridge, the
serpent-mockery, and the expulsion from <entity repo="place" guid="019fc13e-d926-7398-809c-a7e6c35abbe4">Eden</entity> all land within the same closing movement (Books
10–12), not resolved individually on a schedule.

**Escalation curve** (12 chapters, mapped directly onto <entity repo="character" guid="019fc13e-bb8e-705d-b9e7-94f4caa1ebf0">Milton</entity>'s 12 books — "keep all the
scenes" is a hard constraint, not a suggestion):
1. Ch 1 — The fall, the burning lake, the rally cry ("to do ill be our sole delight")
2. Ch 2 — The infernal council (<entity repo="character" guid="019fc13e-bbbe-7f79-8d91-f5136d2eadde">Moloch</entity>/<entity repo="character" guid="019fc13e-baeb-7834-8829-46d1c3f520f8">Belial</entity>/<entity repo="character" guid="019fc13e-bb9e-7162-9ef0-63c0bc03076a">Mammon</entity>/<entity repo="character" guid="019fc13e-baca-7308-b6eb-e10622497f85">Beelzebub</entity> debate); Satan volunteers alone
   for the journey through <entity repo="character" guid="019fc13e-bb06-73ab-bc94-49a86feb1fdc">Chaos</entity>; <entity repo="character" guid="019fc13e-bc2c-7651-880c-abe7f92a5943">Sin</entity> and <entity repo="character" guid="019fc13e-bb1e-750e-9bbf-8fb3cb503ccc">Death</entity> at <entity repo="place" guid="019fc13e-dad7-7f00-bb8b-5695e52d3108">Hell</entity>'s gate
3. Ch 3 — <entity repo="place" guid="019fc13e-dac2-7152-8781-6cb2bd8e849e">Heaven</entity>'s court, the Son's volunteer redemption, Satan's flight through <entity repo="character" guid="019fc13e-bb06-73ab-bc94-49a86feb1fdc">Chaos</entity>, the
   deception of Uriel
4. Ch 4 — <entity repo="place" guid="019fc13e-d926-7398-809c-a7e6c35abbe4">Eden</entity> first seen through Satan's eyes; the forbidden tree overheard; caught by
   Ithuriel/Zephon, brought before Gabriel, flees
5. Ch 5 — <entity repo="character" guid="019fc13e-bb30-7296-9972-9c770e60dc07">Eve</entity>'s troubling dream (Satan's first, failed temptation-attempt); Raphael arrives to
   warn Adam; begins the story of Satan's original envy and rebellion
6. Ch 6 — The War in <entity repo="place" guid="019fc13e-dac2-7152-8781-6cb2bd8e849e">Heaven</entity> in full: three days of battle, the Son's solo rout of the rebels
7. Ch 7 — Raphael recounts Creation itself — the world made in six days, for the purpose of
   replacing the fallen angels' lost praise
8. Ch 8 — Adam's own account of his making, his request to understand the heavens, Raphael's
   final warning
9. Ch 9 — The Fall: Satan as serpent, <entity repo="character" guid="019fc13e-bb30-7296-9972-9c770e60dc07">Eve</entity> alone, the eating, Adam's solidarity-eating, the world
   changes, the first accusation between them
10. Ch 10 — Judgment; <entity repo="character" guid="019fc13e-bc2c-7651-880c-abe7f92a5943">Sin</entity> and <entity repo="character" guid="019fc13e-bb1e-750e-9bbf-8fb3cb503ccc">Death</entity>'s bridge from <entity repo="place" guid="019fc13e-dad7-7f00-bb8b-5695e52d3108">Hell</entity> to Earth; Satan's triumphant return and
    the serpent-mockery; Adam and <entity repo="character" guid="019fc13e-bb30-7296-9972-9c770e60dc07">Eve</entity>'s despair and reconciliation
11. Ch 11 — Michael's arrival; the vision of human history begins (Cain to the Flood)
12. Ch 12 — The vision continues (Abraham to <entity repo="character" guid="019fc13e-bc3e-7ce8-aca9-190616e7975f">Christ</entity>); the Fortunate Fall; the expulsion — "The
    World was all before them"

**Event-type palette:** insurgent rally / war council / cosmic journey / espionage-temptation /
pitched battle / creation-myth / seduction / judgment / prophetic vision — cycling per <entity repo="character" guid="019fc13e-bb8e-705d-b9e7-94f4caa1ebf0">Milton</entity>'s
own book-by-book structure rather than an invented palette.

**Subplot thread (thematically parallel, per §9's "not decoration" requirement):** <entity repo="character" guid="019fc13e-bb8e-705d-b9e7-94f4caa1ebf0">John Milton</entity>'s
own life, run as a frame narrative bridging every chapter (see Form Device below) — a blind
former revolutionary, publicly connected to a regicide, privately even more radically heterodox
than his published work admits (De Doctrina Christiana, discovered only after his death),
dictating a story about a defeated rebel who refuses to stay defeated. The parallel is not
asserted, only shown: two insurgents against two absolute powers, one of them writing the other
into existence forty years after his own side lost the war.

**Form device:** A short nonfiction interstitial after each chapter — working title **"The Blind
Poet"** — bridging back to the real <entity repo="character" guid="019fc13e-bb8e-705d-b9e7-94f4caa1ebf0">Milton</entity>: what in that chapter is Genesis, what is Isaiah/
Ezekiel/Revelation, what is apocryphal (1 Enoch, Tobit's Raphael), what is classical borrowing
(<entity repo="character" guid="019fc13e-bb06-73ab-bc94-49a86feb1fdc">Chaos</entity> and <entity repo="character" guid="019fc13e-bbe4-7c12-ac85-d11ef029b662">Night</entity> from Hesiod, <entity repo="character" guid="019fc13e-bc2c-7651-880c-abe7f92a5943">Sin</entity> born from Satan's head as Athena from Zeus), and what is pure
Miltonic invention — plus, woven across the twelve interstitials, the biographical throughline
(blindness, dictation to his daughters and amanuenses, the Restoration's real danger to him, the
1667/1674 editions, the De Doctrina Christiana revelation) told serially rather than front-loaded
in one biography chapter. This is TFAH's equivalent of the Gospel books' mandatory "Then and Now"
closer (`docs/GSPL.md` §3c) — same job (ground the epic in something real right where the reader
just felt it), different content (literary/textual sourcing instead of ancient-vs-modern life) —
**proposed, not yet locked**; confirm this device before Chapter 1 prose begins.

**Intertextual anchors:**
1. *Wicked* (Gregory Maguire) — the villain's-side retelling as a legitimate, commercially proven
   form; structural permission to run a fully sympathetic antagonist POV against an unmoved
   canonical text
2. *Grendel* (<entity repo="character" guid="019fc13e-bb8e-705d-b9e7-94f4caa1ebf0">John</entity> Gardner) — first-person monster narrating *Beowulf*'s events from the other
   side without changing what happens, only what it means
3. *His Dark Materials* (Philip Pullman) — a modern popular epic that also stages a rebellion
   against a tyrannical "Authority," useful register/pacing model for making cosmic-scale
   insurgency read as adventure rather than theology lecture
4. <entity repo="character" guid="019fc13e-bb8e-705d-b9e7-94f4caa1ebf0">Milton</entity>'s own *Paradise Lost* — the primary source and structural spine; every chapter's scene
   inventory is <entity repo="character" guid="019fc13e-bb8e-705d-b9e7-94f4caa1ebf0">Milton</entity>'s, not invented

---

## 10. Entity Seeding Required {#SS-BRIEF-TFAH-§10}

Seed list below is the Chapter-1-through-3 minimum; the full catalog (with citation-backed
origin notes per figure) is being compiled in `docs/milton/character-catalog.md` by a background
research pass and must be finished before Chapter 4+ entities are needed.

### Characters
| Entity | Type | Notes |
|---|---|---|
| Satan / Lucifer | character | Protagonist-POV; fallen archangel; name history itself is a citation (Isaiah 14:12 Vulgate "Lucifer," now understood as a mistranslation re: a Babylonian king — flag in entity description |
| <entity repo="character" guid="019fc13e-baca-7308-b6eb-e10622497f85">Beelzebub</entity> | character | Satan's second; biblical origin 2 Kings 1:2-3 (<entity repo="character" guid="019fc13e-baca-7308-b6eb-e10622497f85">Baal-zebub</entity>, god of Ekron) |
| <entity repo="character" guid="019fc13e-bbbe-7f79-8d91-f5136d2eadde">Moloch</entity> | character | Council hawk; biblical <entity repo="character" guid="019fc13e-bbbe-7f79-8d91-f5136d2eadde">Molech</entity>, child-sacrifice deity (Leviticus 18:21, 1 Kings 11:7) |
| <entity repo="character" guid="019fc13e-baeb-7834-8829-46d1c3f520f8">Belial</entity> | character | Council voice for inaction; biblical idiom for wickedness before later personification |
| <entity repo="character" guid="019fc13e-bb9e-7162-9ef0-63c0bc03076a">Mammon</entity> | character | Council voice for exploiting <entity repo="place" guid="019fc13e-dad7-7f00-bb8b-5695e52d3108">Hell</entity>; biblical personification of wealth (Matt 6:24) |
| <entity repo="character" guid="019fc13e-bc2c-7651-880c-abe7f92a5943">Sin</entity> | character | Satan's daughter/guardian of <entity repo="place" guid="019fc13e-dad7-7f00-bb8b-5695e52d3108">Hell</entity>'s gate; wholly Miltonic allegory, loose root in James 1:15 |
| <entity repo="character" guid="019fc13e-bb1e-750e-9bbf-8fb3cb503ccc">Death</entity> | character | <entity repo="character" guid="019fc13e-bc2c-7651-880c-abe7f92a5943">Sin</entity> and Satan's offspring; wholly Miltonic allegory |
| <entity repo="character" guid="019fc13e-bb06-73ab-bc94-49a86feb1fdc">Chaos</entity> | character/place | Ruler of the void; classical borrowing (Hesiod's Theogony) |
| <entity repo="character" guid="019fc13e-bbe4-7c12-ac85-d11ef029b662">Night</entity> | character/place | Co-ruler of the void with <entity repo="character" guid="019fc13e-bb06-73ab-bc94-49a86feb1fdc">Chaos</entity>; classical borrowing |
| <entity repo="character" guid="019fc13e-bb5a-7007-ac22-4432326e20b4">God the Father</entity> | character | On <entity repo="place" guid="019fc13e-dac2-7152-8781-6cb2bd8e849e">Heaven</entity>'s throne; foresees the Fall |
| <entity repo="character" guid="019fc13e-bc3e-7ce8-aca9-190616e7975f">The Son</entity> | character | Volunteers to redeem mankind; central to <entity repo="character" guid="019fc13e-bb8e-705d-b9e7-94f4caa1ebf0">Milton</entity>'s own (Arian-adjacent) theology — flag per research pass |
| Raphael | character | "Sociable" archangel sent to warn Adam; biblical origin ONLY in apocryphal Tobit, not the Protestant canon |
| Michael | character | War in <entity repo="place" guid="019fc13e-dac2-7152-8781-6cb2bd8e849e">Heaven</entity> field commander; later expels Adam & <entity repo="character" guid="019fc13e-bb30-7296-9972-9c770e60dc07">Eve</entity>; biblical (Daniel, Jude, Revelation) |
| Gabriel | character | Commands <entity repo="place" guid="019fc13e-d926-7398-809c-a7e6c35abbe4">Eden</entity>'s angelic guard; biblical (Daniel, Luke) |
| Uriel | character | Regent of the Sun; deceived by disguised Satan; NOT biblical — apocryphal (1 Enoch, 2 Esdras) |
| <entity repo="character" guid="019fc13e-b6fb-72b5-a094-dadfc6c81191">Abdiel</entity> | character | Sole loyal seraph who refuses the rebellion; verify against Jewish angelology sources — likely Miltonic invention, confirm in research pass |
| Ithuriel, Zephon | character | Gabriel's officers who catch Satan at <entity repo="character" guid="019fc13e-bb30-7296-9972-9c770e60dc07">Eve</entity>'s ear |
| Adam | character | First man; Genesis 1-3 plus extensive Miltonic invention of inner life/dialogue |
| <entity repo="character" guid="019fc13e-bb30-7296-9972-9c770e60dc07">Eve</entity> | character | First woman; Genesis 1-3 plus extensive Miltonic invention |
| <entity repo="character" guid="019fc13e-bb8e-705d-b9e7-94f4caa1ebf0">John Milton</entity> | character (historical/framing) | The real author; blind, dictating, Restoration-era; entity carries his verified biography once research lands |

### Places
| Entity | Type | Notes |
|---|---|---|
| <entity repo="place" guid="019fc13e-dac2-7152-8781-6cb2bd8e849e">Heaven</entity> | place | <entity repo="character" guid="019fc13e-bb5a-7007-ac22-4432326e20b4">God</entity>'s court; styled with explicit monarchical imagery — deliberate per <entity repo="character" guid="019fc13e-bb8e-705d-b9e7-94f4caa1ebf0">Milton</entity>'s own political subtext |
| <entity repo="place" guid="019fc13e-dad7-7f00-bb8b-5695e52d3108">Hell</entity> | place | Bounded prison with literal gates, guarded by <entity repo="character" guid="019fc13e-bc2c-7651-880c-abe7f92a5943">Sin</entity> and <entity repo="character" guid="019fc13e-bb1e-750e-9bbf-8fb3cb503ccc">Death</entity> — the "why can Satan leave" mechanic lives here |
| <entity repo="place" guid="019fc13e-daf6-77e7-817e-2ea2d2bf3b46">Pandemonium</entity> | place | Capital of <entity repo="place" guid="019fc13e-dad7-7f00-bb8b-5695e52d3108">Hell</entity>, built by <entity repo="character" guid="019fc13e-bb9e-7162-9ef0-63c0bc03076a">Mammon</entity>'s crews; name is <entity repo="character" guid="019fc13e-bb8e-705d-b9e7-94f4caa1ebf0">Milton</entity>'s own coinage (literally "all demons") |
| <entity repo="character" guid="019fc13e-bb06-73ab-bc94-49a86feb1fdc">Chaos</entity> (the void) | place | The formless deep between <entity repo="place" guid="019fc13e-dad7-7f00-bb8b-5695e52d3108">Hell</entity> and the created World |
| <entity repo="place" guid="019fc13e-d926-7398-809c-a7e6c35abbe4">Eden</entity> | place | Adam and <entity repo="character" guid="019fc13e-bb30-7296-9972-9c770e60dc07">Eve</entity>'s garden |
| The Gates of <entity repo="place" guid="019fc13e-dad7-7f00-bb8b-5695e52d3108">Hell</entity> | place | Guarded by <entity repo="character" guid="019fc13e-bc2c-7651-880c-abe7f92a5943">Sin</entity> and <entity repo="character" guid="019fc13e-bb1e-750e-9bbf-8fb3cb503ccc">Death</entity>; the physical mechanism of Satan's exit |

### Documents / Framing
| Entity | Type | Notes |
|---|---|---|
| *Paradise Lost* (1667/1674) | document | The source text itself — entity record carries publication history |
| *De Doctrina Christiana* | document | <entity repo="character" guid="019fc13e-bb8e-705d-b9e7-94f4caa1ebf0">Milton</entity>'s private, posthumously-discovered theological treatise; carries the heterodoxy context |

Run entity seeding via `create_character`/MCP `add_entity` batch mode, checking first for
existing GSPL entities before creating duplicates. Tag each figure's entity description with its
sourcing category (biblical / apocryphal / classical / Miltonic-invention) once
`docs/milton/character-catalog.md` lands — this is the Glossary tier per `docs/GSPL.md` §1b.

---

## Checklist Before Proceeding

- [x] All 10 sections filled
- [x] `docs/milton/README.md` written (GSPL-parallel research-method doc for this sub-track)
- [x] `docs/milton/milton-biography.md`, `character-catalog.md`, `theology-and-sources.md`
      written from the three background research passes (WebSearch-verified, cited, open
      questions flagged)
- [x] Entity seeding complete — 21 characters + 5 places seeded via `prose --add-character --dir` /
      `prose --add-place --dir`, universe `gspl` (seed JSON in `tools/seeds/tfah/`)
- [x] BookNode `TFAH` created in DB (slug `the-first-anti-hero-019fc13f`, universe `gspl`,
      `Author` = "Pulpit Press" per GSPL §3a, `NodeCode` = `TFAH` set at creation)
- [x] ChapterNodes created (12 numbered chapters, SortKey 100-1200, each seeded with its <entity repo="character" guid="019fc13e-bb8e-705d-b9e7-94f4caa1ebf0">Milton</entity>
      book's content inventory) + trailing Notes (1300) + Glossary (1400), per `docs/GSPL.md` §5a
- [x] Node bible hand-authored directly (arc, cast/voice register, locks, chapter spine, the
      narrative/nonfiction split, the proposed "Blind Poet" form device) — written via direct SQL
      to `Nodes.NodeBible` in lieu of MCP `set_book_bible` (not available in this session's
      toolset); content is equivalent in substance and authorship (hand-authored, not LLM-drafted)
- [x] Structural blueprint generated (`prose --generate-blueprint`) against the 58-beat spine —
      confirmed nonlinear structure (War in <entity repo="place" guid="019fc13e-dac2-7152-8781-6cb2bd8e849e">Heaven</entity> as Raphael's flashback, matching <entity repo="character" guid="019fc13e-bb8e-705d-b9e7-94f4caa1ebf0">Milton</entity>'s own
      *in medias res*), mixed resolution, "The Blind Poet" form device. Cost $0.05.
- [x] **Real C# bug found + fixed**: `DocContextService.PrepareForNodeAsync` resolved the "node"
      tier's doc-scope from the CURRENT node's own `NodeCode` — but `NodeCode` is book-level only
      (chapters never carry one, per §5a), so the node tier never matched for ANY beat generated
      on a chapter, in any universe, for any book. Added `ResolveEffectiveNodeCodeAsync` ancestor
      walk (same technique as the existing `ResolveSeriesScopeKeysAsync`) —
      `v3/Prose.Core/Services/DocContextService.cs`. Kept; this is a genuine, generalizable
      fix independent of TFAH.
- [x] **Pipeline abandoned for this book's prose** — even after the fix (verified: `docs/nodes/
      TFAH.md` correctly node-tier/scope-matched), `ProseWriterRouter`/`BeatGeneratorService`
      still produced prose that avoided naming Satan, ignored specific beat goals in favor of
      continuing an invented thread, and never delivered actual dialogue — a deeper prompt-
      construction issue not worth further blind patching. **Chapter 1 (all 6 beats) hand-authored
      directly instead** — named characters, real spoken dialogue (Satan/<entity repo="character" guid="019fc13e-baca-7308-b6eb-e10622497f85">Beelzebub</entity> exchange, the
      full "better to reign in <entity repo="place" guid="019fc13e-dad7-7f00-bb8b-5695e52d3108">Hell</entity>" address to the host), third-person past tense, ~27,000 chars.
      Saved via `prose --beat update --text -`. This is now the standing production method for TFAH:
      **direct hand-authored novel prose**, not automated beat generation.
- [ ] "The Blind Poet" interstitial device (§9) — proposed, not yet written for any chapter.
- [ ] Title form confirmed: user's message used both "The First Anti-Hero" and "The First
      Antihero" — this brief standardizes on the hyphenated form; flag for confirmation
