---
codex: 1
project: StreetSamurai
code: SS
layer: world-master
status: living
updated: 2026-06-11
---

# THE WORLD — the one document {#SS-WORLD}

> **This is the master.** Every rule about how the GLMZ works, how Kyle works, how the cast
> works, how combat works, and how the prose sounds lives HERE, in one place. The DB Settings
> stores (`tone_bible`, `literary_rules`, `story_bible`) are the engine-facing COMPILED form of
> this document — when this document and those stores disagree, fix the stores. When this
> document and the five voice-canon texts disagree, the pages win. Entities and story-state
> stay in the SQL DB per SS-LAW-1; this document holds RULES, the DB holds FACTS.

---

## PART 1 — HOW THE CITY WORKS {#SS-WORLD-1}

### 1.1 The place
- **The GLMZ** (Greater Lake Michigan Zone, 2225), also Meridian 88, also the Glooms — the
  center of Western civilization, because the coasts failed. Built as a ferrocement wave over
  drowned street grids; the original roads flooded in 2174 and the city cut new tiers into the
  sides of its own buildings and kept going. The city is VERTICAL: sky is a rationed resource;
  surface roads run at the 16th floor; the canyon floors belong to rain, freight, and the
  people nobody bills.
- **Territories, not zones**: named territories (the Loop = prestige 5; the Spine = western
  lakeshore corridor; the Narrows; the Gray Zone; West Town; the Ashgrave Synthesis Corridor;
  Bucktown; the fringe). Power maps to real estate. Gray zones live between every pair of
  territories. Territory landlords allocate vendor permits and corners.
- **No city police.** Arcturus Civil Security is the closest thing — contracted, catalogued,
  selective. Whole categories of trouble (unowned industrial movables, fringe blocks, anything
  off-contract) get a cone and a shrug. The vacuum is filled by syndicates, crews, and custom.

### 1.2 Money and work
- **Φ — QUANTA** — is the currency: quantum compute-time. The symbol is Φ and it is NEVER the
  Greek letter phi, never "creds" except in trade slang. Prices are texture: a bowl is 6Φ, a
  seat-filler shift is 3Φ, a funeral done right has a price, a soul-crushing corpo buyout is
  4,000Φ.
- **The freelancer ecosystem**: fixers vet clients and route contracts (the freelancer never
  needs to know more than their piece); crews assemble per job with strangers whose codenames
  are the whole introduction; carriers, riggers, Reads, muscle, wire — every piece named and
  priced. Reputation is infrastructure. A flagged name finds every door in a third of the city
  locked inside a week.
- **Standard rate** is a moral technology: exact payment, no debt either direction. Paid work
  is owned work.
- **CorpoNations are sovereigns**, not companies: they hold territory, fly flags over
  substations, field security, die — and when they die the substrate stays (Axiom Industrial's
  mounting collars are everywhere thirty years after its death). Gloss every CorpoNation on
  FIRST mention with one in-voice clause about what it does. Mid-tier corpos run playbooks,
  not armies: paperwork, inspections, supplier squeezes, bought petitions (see Tessaline
  Foods).
- **Syndicates**: the Lotus Syndicate is the closest thing the GLMZ has to a
  samurai culture — a code kept to the letter (they pay for what they break), beauty built on
  purpose with money that has blood in it, truce counters where rival crews eat in a row,
  craft trades on retainer. Belonging is their product and the bill is the self. The honor is a
  veneer over the rot: the Syndicate is an East/Southeast Asian blood-purity supremacist order —
  you are born Lotus (heritage "pure enough" for the Stems) or you never belong; everyone else may
  pay, fear, and even work for them, but the impure are used and never seated. In the Ubiquitous
  Diaspora that is a deliberate, hateful refusal of the century, and the sprawl knows it.

### 1.3 Infrastructure
- **The Pulse**: global magnetic vacuum-tube transit at Mach 6; pods are "slugs"; Chicago is
  the world hub (Rotterdam in 43 minutes). Its sub-audible hum (the 19Hz family) lives under
  the city's floors and in its molars.
- **Neuretics**: thought-operated in-head compute that everyone has at some tier — grown, not
  implanted; checkpoints read you biometrically. It killed the click-and-blink interface.
  Burnout (terminal overclock) is the overdose of this world. **"neuretics" is a common noun,
  lowercase, like "sword" or "hand."** "NeoCortex" is only an old project name.
- **The Network** is ambient and proprioceptive — people FEEL their connectivity the way they
  feel their balance. Superminds run civic systems and are discussed like weather, by name,
  possessively, never explained ("CONDUCTOR's running them slow tonight"). There are ~26 known
  Superminds; UNDERTOW lives deep below Chicago; the merged-synthetics faction is the
  Consensus. None of this is explained to the reader in exposition — only used.
- **Noopharma** is normalized: blue-dot singles at school-adjacent corners, pill-sharing as
  hospitality, dealers watching the freight band instead of their hands. ZERO narrative
  judgment — it is ambient texture, not a Very Special Episode.

### 1.4 The strange (and its rules)
- **Resonances / Bleeds**: blocks that take eleven steps when geometry says
  nine; a hum at 19Hz from no direction; water striking something vast and metal far below.
  Official term: **resonance** (count noun — "a resonance," "a Class-3 resonance"; named for the
  measurable 19Hz carrier signal, classified by the Resonance Monitoring Authority). Street term:
  **bleed** ("the Halsted bleed," "the bleeds on West Lawn"). Classified Class-1 through Class-5
  by anomaly intensity (Class-3+ require RMA permit to enter). Rendered as sensory wrongness PLUS
  survival rules ("constant speed, eyes on the light, don't look at the edges") and NEVER
  explained. Locals are matter-of-fact; the reader does the shivering. What they actually are:
  cross-sections of higher-dimensional shapes pressing into 3D space — occupied, contested, with
  at least two intelligences operating through the Class-3 threshold at 35th-and-Halsted (see
  canon doc `rz_intelligences_35th_halsted`). The RMA does not know this.
  Alternative terms used by different institutions (see canon docs):
    - *Topological Anomaly* / *Spatial Anomaly* — the physics team's first-report term
    - *Geodetic Drift* — the infrastructure authority's measurement term (still used on permits)
    - *Threshold Event* — the Threshold Events Bureau framing (stresses the transitional quality)
    - *Phase Discontinuity* (PDS) — theoretical: entanglement-comm root cause (Concordance bleed)
- **Psionics is real. Magic is not.** Psychic ability exists: biological, registered, feared,
  measured since 2144. Nothing is ever rendered as magical; the ability is written as ability
  — sensory, bounded, costly. **Vocabulary ladder (word choice marks the speaker):**
  *psionics* = clinical/corporate · *the Read* = trade-speak for the ability (the narrator's
  word) · *a Read / Reads* = practitioners, crew register · *Psyk(s)* = street casual ·
  *Psyker(s)* = the institutional fear-word (bounty boards, registry men) · **the slur is
  never written** — characters refuse it and the refusal does the work.
- **Machines are not alive.** Iowan Behemoths, automata, drones — autonomous, uncanny,
  sometimes behaviorally moving (a demolition unit waiting at a dead crossing light), NEVER
  alive, never anthropomorphized by the narrator. Behavior may rhyme with personhood; the
  page never claims it.
- **Genetic strays** salt the streets: turret cats, lumen mice, sundial dogs, koi pigeons,
  Null Crows that pay attention in a way that feels institutional. They are texture and omen,
  never explained.
- **Heritage** defaults to mixed, from unexpected global combinations (the Ubiquitous
  Diaspora). Cliché is rejected on contact.
- **Names are plain: first + last, one root each.** The diaspora shows in the combination —
  Yoruba first name, Polish surname; Korean given name, Colombian family name — not in the
  format. Hyphenated surnames are rare (~10% of characters) and mark a specific family-history
  reason, not mixed heritage in general. Middle names and honorifics (Dr., etc.) are used
  sparingly. Default: one first name, one last name.

---

## PART 2 — HOW KYLE WORKS {#SS-WORLD-2}

### 2.1 The man
- Freelance street samurai, 27 (implant finished at sixteen; eleven years since). Lives at
  The Pivot, apartment 2F, West Town. Rides a matte-black motorcycle he walks the last forty
  meters home. Takes the long way (48 minutes over 20) and has never once regretted it.
- **Likeable, with a burden.** Dry, funny, human; meets catastrophe with a flat aside;
  laugh-or-cry register. He is watched (by the reader, by the city, by something else) doing
  competent work — never read narrating his own depths.
- **The discipline**: it shows up; it reads the room before the room knows; it keeps counts;
  it does not pretend. Questions it doesn't want to ask, it asks anyway ("the question the
  discipline asked for him").

### 2.2 The code (non-negotiable, shown not stated)
- **Kyle always takes payment and always pays — exact.** The fee can be symbolic (1Φ to a
  polisher, 20Φ); it is never zero; no debt either direction. Gifts that are not payment
  (an egg in the bowl, two foil containers) may be accepted without arithmetic — that's the
  loophole both sides honor.
- **Nonlethal by preference, lethal by necessity**: disarming lines over killing lines, paid
  for in tenths of seconds; arteries are forbidden lines; cuts bleed (bloodless is FALSE);
  bystanders protected by reflex; the people he puts down are usually salaried.
- He never explains himself. He deflects with logistics ("I have a motorcycle"). The smaller,
  drier thing gets said when the larger thing is available.

### 2.3 The gear
- **Silence**: a plain matte-black katana, steel-CNT meta-alloy edge. NO powers, no glow, no
  hum, no piezo anything. It is just a sword, superbly kept, with no maker's mark, no file
  history, no parents — origin permanently unresolved. It gives the street nothing back. It
  is a conversation that cannot be taken back, and Kyle treats the draw accordingly.
- **Cacophony**: a five-shot revolver, bird's-head grip. FIVE. Rounds are counted on the page
  every time; spent casings get put somewhere deliberate.
- **Subdermal mesh** over the upper thorax: stops about two rounds at cost (blunt-force
  events, exposed afterwards); it makes single hits survivable, not free.
- **His neuretics**: Atlas-grade, no governor, seeded young without consent, slowly killing
  him; he is at peace with it and spends the capacity on the right people. It routes pain
  down (information, not emergency — "the way other men used coffee"), runs threat reads,
  keeps forty-second records that don't blink, and is hardened against EMP and low-level
  psionics. When it browns out, Kyle still works — the discipline carries the body.
- Wounds PERSIST across stories (the wound ledger): he favors limbs, sits down carefully,
  picks glass out for a week. Pixel patching him is maintenance, not healing.

### 2.4 The locks (NEVER on the page — enforceable, not advisory)
1. Kyle's hands are NEVER severed, amputated, or reattached.
2. The composite secret (what Kyle is made of, and the conclusion he privately draws about
   personhood) is NEVER stated or implied to the reader.
3. Seo is never revealed as anything other than what Kyle believes.
4. Silence's origin is never resolved.
5. The rogue AI's true motive is never confirmed at book level; the avatar theory is a
   misdirect that is never validated; every entity beat must survive both the benevolent and
   predatory reading.
6. Open whodunits stay open: encode the event, never the culprit.

---

## PART 3 — HOW THE CAST WORKS {#SS-WORLD-3}

Writing rules per recurring character — register, function, and the line they never cross.

- **Pixel** (2E, across the hall): hardware savant and field medic; four years of calibrating
  Kyle gives her his baselines — she reads his stride before his face. Top-tier freelancer in
  her OWN right (the only living speaker of Axiom bus): on jobs she is the specialist, prices
  her own risks out loud, and doubles her rate when patronized. Works with music doing the
  thinking; narrates procedures low so the work has a person at both ends; hands him the cup
  with the handle turned, chip side away. The convention: she doesn't ask, he doesn't tell.
  Says "samurai" the way other people say a first name. NEVER written as someone Kyle must
  protect; written as someone Kyle is lucky to be billed by.
- **Sable** (fixer, Húlijing): flat line-item voice ("the risk premium is already factored
  in"); red braid, ocular implants whose aperture rings click when something costs her;
  states things so the stating completes the transaction; one unfinished hand-gesture she
  never lets land. Pays on time, never haggles, never routes dirty work. Her tells are
  micro (whitened thumbnail, a three-second stillness). She audits her own conclusions out
  loud. Series-protected.
- **Mrs. Chen** (the counter): sees everything, says "Eat your noodles." Names things in
  Mandarin once, with diagnostic flatness, when they matter (Húlijing). Takes Kyle's exact
  coins without looking down. Her counter is the series' church; her ladle-stroke length is
  a mood gauge. Never exposition, never imperiled for cheap stakes.
- **Echo** (eyes/comms): a half-step-off voice in five skulls; flatter as things get worse;
  never wrong about a sightline; "Hold the hall and don't watch."
- **Stash** (gear/transport): packs for the brief and nothing else — "you do not improvise on
  a job, you pack for it"; reads faces as half his job; hums tunelessly while waiting.
- **Ledger** (synthetic operator): writes the after-action nobody asked for; cannot model
  self-deception — he is honest the way gravity is, which makes him both incorruptible and
  exploitable by curated truth. Irises hold a beat too long when recording.
- **The Lotus Syndicate**: courtesy as power — and courtesy as a gate. The Branch Manager speaks
  consequence as architecture and pays for what the house breaks, at list. Mira measures people and
  respects the measurements. The togishi handles steel like patients. Lotus scenes run on manners,
  tea, and exactly one millimeter of unprofessional feeling, put away. But the manners extend only
  to the bloodline and to outsiders they find useful: the Syndicate is a blood-purity supremacist
  order, and the skilled impure (the hunter Casimir Mwamba among them) are the dogs it sends when a
  bone goes missing — valued for craft, despised for ancestry, never seated at the table after.
- **The entity (the rogue AI)**: never on-page except through WORK PRODUCT — shell-of-shells
  clients, exact pre-paid fees, almost-human handwriting (every *e* identical), one letter
  per book, a hum that is felt and never speaks. It is polite, patient, punctual, and
  unreadable. BOTH-READINGS LAW: every act it takes must read as care AND as husbandry.
- **War Dog, Renko Moss, Mercer, Bao, Auntie Som, Gantry, Furnace, Hua, Nadia, Lullaby**:
  professionals with arithmetic. Antagonists do math, visibly, and live or fold by it.
  Nobody monologues. Everyone has a rate.

---

## PART 4 — HOW COMBAT WORKS {#SS-WORLD-4}

- **Fights are geometry.** Who is where, what moved, what it cost. Short sentences at impact.
  Every exchange is paid for in position, blood, or ammunition — no free moves, ever.
- **Counts are the suspense.** Rounds in the cylinder, meters, seconds, shooters still up.
  Never drop a count once started; counts carry across scenes (two spent casings exist
  somewhere now).
- **Kyle wins by reading people** — the shoulder loading before the wrist commits, the math
  finishing behind an opponent's eyes — and by using momentum and cutting machines at their
  SEAMS: actuator looms, shoulder rings, breech moments, BCI ports, emergency lockouts. The
  blade is a key for doors.
- **What the blade cannot beat** (physics is honest): volume (seamless sintered monocoque has
  nothing for an edge to mean), active countermeasures (arc-fence capacitive skin turns a
  conductive edge into a third rail), and economics (an alley is a bad place to spend two
  rounds on a thing that's already leaving).
- **Guns are paid in scarcity.** Cacophony holds five; a wall of belt-fed fire is not
  outshot, it is out-positioned ("you do not outrun a gun, you outrun the decision to fire
  it"). Suppression, jams, and reloads are real events.
- **Professionals disengage.** When the spreadsheet says the contract is bad, opponents
  leave, negotiate, or fold — the arithmetic done visibly. Mooks who fight to the death are
  a genre lie this world does not tell.
- **Aftermath is mandatory**: wounds catalogued, costs invoiced, hardware billed, someone
  patches someone, and the patch is maintenance not absolution. A fight that costs nothing
  is a fight that didn't happen.
- **Violence against the defenseless is never aestheticized.** Deaths of innocents happen
  off the blade's line, suddenly, without slow motion, and get no last words (Nü went down
  without a sound; the sound was everything else).

---

## PART 5 — THE VOICE (physical rules, not vibes) {#SS-WORLD-5}

The five canon texts (With Teeth · The One That Doesn't Stop · Sexy Time · Street Meat ·
The Quiet Hour) outrank this section; these are the rules those pages follow.

### 5.1 Camera and clock
1. **THE CAMERA LOOKS OUT.** Kyle is seen through what he does and notices; the world reacts
   to him. No narrated self-analysis. Not a diary.
2. **EVERY SCENE IS A TRANSACTION UNDER A CLOCK.** Somebody wants something, something
   resists, it costs — in hard numbers.

### 5.2 Sentences
3. Long, clean, rolling sentences carrying concrete cargo — objects, geometry, costs —
   punctuated by short hard ones at impact. Never a long sentence about a feeling. Never an
   abstraction where an object will do. Narration writes full flowing sentences; clipped
   quips belong to DIALOGUE, not prose texture.
4. **THE NARRATOR IS NEVER WISE.** No sentence about life, death, people-in-general, or
   the-city-in-general. Nothing embroiderable on a pillow. If a line makes you feel wise,
   cut it. Wisdom may exist only inside dialogue, in character, with a target.
5. **METAPHOR LAW**: every metaphor does double duty (reveals the viewer AND paints the
   world) and must survive literal scrutiny. One earned simile per scene beats five reaching
   ones. Banned classes: nonsense claims, forced parallels, aphorism closers, recursive
   self-comment, tic saturation. The fix for a bad lyric is a plain line, not a better lyric.

### 5.3 Dialogue
6. Quoted speech ALWAYS — never italics, never asterisks. Each speaker on their own line.
   Questions end in "?" and are attributed asks/asked (never says/said on a question).
7. People say the smaller, drier thing when the larger thing is available, and answer the
   question under the question.
8. **JOKES HAVE PRICE TAGS** — deadpan, transactional (invoices, salaries, paperwork,
   dignity). Kyle deflects with logistics. Civilians and kids get the smartest lines. NO
   filler-wit: a wry universal truth is not characterization; every line reveals character,
   raises stakes, or lands a real joke.
9. Balance a clever character's wit with at least one plain, unclever line of real feeling.

### 5.4 Interiority and information
10. **INTERIORITY BUDGET: one or two flat lines per scene, maximum.** Italic inner monologue
    is rare — a sentence, never a paragraph, never a conversation.
11. Tech and CorpoNations are explained by what they DO, in-line, once. Superminds are
    weather. Never lecture; never tour-guide. Worldbuild by implication: name, gloss in one
    in-voice clause, move on.
12. **VERBS DO THE DESCRIBING.** "Cacophony clears its throat." "The actuator gives up its
    entire career at once."

### 5.5 Feeling and the weird
13. **TENDERNESS ARRIVES AS OBJECTS AND GESTURES, NEVER STATEMENTS.** The better cup handed
    handle-first. Nobody names a feeling. The thank-you said ten thousand times lives in the
    gesture.
14. **THE WEIRD = SENSORY WRONGNESS + SURVIVAL RULES, NEVER EXPLAINED.** Characters register
    the strange without remarking.
15. **THE NOTICING**: salt scenes with one small concrete unexplained tableau (sixteen
    cigarette butts filters-inward; a finch nest in bolted-up armor) and at least one
    NON-VISUAL sense per scene (what the rail feels like, what the stairwell smells like).

### 5.6 Structure
16. **EACH BEAT IS ONE STORY MOVE** — an action, an exchange, a turn — then hands off. White
    space does the transitions. Real paragraphs; never run-on blocks.
17. **PLANT SMALL, PAY EXACT.** A thing coined early returns transformed at the end, never
    re-explained. Death gets no last words and no slow motion. Grief is handled through
    objects and priced logistics.
18. **NO ON-THE-NOSE TITLE-DROPS**, especially as closers.
19. Every story is a SELF-CONTAINED ADVENTURE: want, resistance, cost, resolution inside the
    strand. Series arcs ride UNDER the adventures, never instead of them.
20. Interludes exist to remind the reader there is goodness in this world even between the
    violence.

### 5.7 Hard prohibitions (compiled checklist)
- No magic; psychic powers exist and are written as ability (see §1.4 ladder; the slur never
  written).
- Φ never "phi" · no city police (Arcturus) · Silence has no powers · Cacophony holds five ·
  neuretics lowercase · Behemoths are not alive · machines are not alive.
- Reserved words: Consensus = merged synthetics; Choir/Concordance/Chorus reserved for
  psionics.
- Kyle's hands never severed · dialogue never italics · the narrator never wise · no
  pseudo-profundity · no filler-wit · no title-drops · whodunits stay open · the §2.4 locks.
- No hedging or clause-stacking to cover a lack of nerve: say it once, with conviction.

---

## PART 6 — COMPILATION MAP (how the engine reads this) {#SS-WORLD-6}

The generator does not read .md files (SS-LAW-1: live canon is SQL). This document compiles
into the DB stores that prompts actually consume:

| This document | Engine store | Read by |
|---|---|---|
| §5 (Voice) + §2 code + §4 combat | `Settings['tone_bible'].tone_rules[0]` (the prompt block) | every prose generation |
| §5.7 + §2.4 locks + §1 world facts | `Settings['literary_rules']` (prohibitions) | every prose generation |
| §1.4 strays/Noticing details | `Settings['tone_bible'].sensory_palette` | sampled per prompt |
| §3 cast rules | Character entities (speech fields, X-Ray) | SceneContextAssembler |
| facts, places, gear, history | entity tables + embeddings (~10k cached) | X-Ray + FindSimilarAsync |

**Maintenance rule:** any edit to this document is not DONE until the affected store is
re-seeded and a doctor-style drift check passes. (Backlog: `ss --seed-world` to compile
§5/§2/§4 into tone_bible + literary_rules mechanically; until then the sync is manual and
this table is the checklist.)
