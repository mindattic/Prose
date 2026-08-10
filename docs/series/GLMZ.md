---
codex: 1
project: Prose
layer: series
updated: 2026-07-18
---

# GLMZ Universe — Story Coordination Board {#SS-GLMZ-COORD}

> **Purpose:** This is the single planning/coordination surface for all GLMZ stories — main
> series and standalones alike. It is a pre-writing instrument, not a canon source.
> Canon lives in `docs/BIBLE.md`, `docs/nodes/<CODE>.md`, and the DB.
> Update this doc whenever a story is added to the roster, a character state is resolved,
> or a plant/payoff pair is confirmed. Run `codex digest && codex doctor` after every edit.

**Full five-book spine:** `docs/rfc/0003-five-book-series-blueprint.md` (authoritative for
arc detail; this doc organizes, tracks, and extends it — it does not duplicate it).

---

## 1. Main Series: The Five Books {#SS-GLMZ-COORD-§1}

| Code | Title | Organ | Status |
|------|-------|-------|--------|
| BCODA | Bushido Coda | The Work | COMPLETE (435 beats, 0 BLOCKERs) |
| TBD | Full Freight | The Body | Planned — B2 brief required before node created |
| TBD | House Colors | The House | Planned — B3 brief required |
| TBD | False Death | The Mind | Planned — B4 brief required |
| TBD | Standard Rate | The Bill | Planned — B5 brief required |

### 1.1 Chapter Stories per Book {#SS-GLMZ-COORD-§1-1}

Chapter stories are episodes within each book (they do not have separate NodeCodes; they are
chapters/beats inside the book's single Node). Sourced from RFC 0003 §3–7.

---

**Book 1 — BCODA (COMPLETE)**

Chapter list lives in BCODA's 435 beats. Key chapters per BCODA prose (actual):
Part I: Teeth · The Regular · Interlude I: Something Fixed · The Quiet Hour ·
Interlude II: Half a Step · Two Favors · The Interview · Interlude III: Before Something
Changes · Sunset Clause · Year Three · Work Order · The Ghost Period · Street Meat ·
The Receipt (coda) · Interlude IV: The Morning · Sexy Time

Season villain: **Mr. Able** (prose-confirmed). See §4 note.

---

**Book 2 — Full Freight (Planned)**

RFC 0003 §4 names these chapter stories. Exact chapter titles TBD when the B2 brief is filled.

| Chapter Story | Series Role | Season Arc Beat? | Prerequisite |
|---|---|---|---|
| Marrow | Atlas-9 reveal; 9 carriers; Kyle is #7 | Yes | BCODA "Ask Marrow" |
| Cadence / Carrier #2 | Addiction mirror; terminal overclock; death on page | Yes | Marrow chapter |
| War Dog Rematch | Rooftop promise settled; info source re: who's buying | Caper | BCODA (War Dog) |
| Renko Full Freight | Loyalty matures; sets up B4 False Death | B-plot | BCODA (Renko) |
| Tessaline B-plot | Mercer's bundle spent; Chen's counter survives at cost | B-plot | BCODA (Mercer's bundle) |
| Lotus Warmth | Chair one year closer; Mira's 60-day clocks | B-plot | BCODA (The Interview) |
| Iowa Run | Behemoth country; Gantry/Furnace | Caper | ICFI-adjacent world |
| Curriculum Revealed | Decade of jobs = syllabus; both readings held | Yes — season arc | After Marrow + Cadence |
| Closer: Cadence's Funeral | Clock visible; entity's ambiguous reply | Yes — state change | After Cadence death |

---

**Book 3 — House Colors (Planned)**

| Chapter Story | Series Role | Season Arc Beat? | Prerequisite |
|---|---|---|---|
| The Clerk Returns | Shell-signature overlap (entity / Unanimity confusable) | Yes | BCODA (The Interview) |
| Lotus Civil War | Code vs modernizers; Sunny introduced | Yes | Lotus Warmth (B2) |
| Year One = Lullaby | First courier found; dark gospel; two theories now live | Yes — season arc | Sable's B1 thumb-rest |
| Sable Burned | Unanimity's answer to Year Three; Sable underground | Yes — state change | Year Three (B1) |
| Temptation Peak | Chair offered; Kyle's refusal; entity reply | Yes — season arc | Sable Burned |
| Pulse-Slug Heist | Mach-6 vault set piece | Caper | — |
| Blossom Arcade Defended | Petals/blood/lanterns; cherry trees at risk | Caper | Lotus Civil War |
| Mirrorwell Station | Arcology scouted; B4 hiding world planted | Plant for B4 | — |

---

**Book 4 — False Death (Planned)**

| Chapter Story | Series Role | Season Arc Beat? | Prerequisite |
|---|---|---|---|
| Outed | Task force assembles Atlas-9; Psyker protocols | Yes | Sable burned (B3) |
| Avatar Theory Peaks | Evidence makes entity look like borg-queen | Yes — misdirect | Lullaby (B3) |
| Ledger's Honest Betrayal | True/incomplete data → crew location traded | Yes | — |
| The False Death | Renko delivers; Kyle erased; working from Mirrorwell | Yes — state change | Renko (B2+B3); Mirrorwell (B3) |
| NeoCortex Terminal | Pixel runs decay curve; months not years | Yes | Cadence (B2) clock |
| Truth Assembled | Nadia + Marrow + entity papers + UNDERTOW | Yes — tent-pole | Nadia (B1); Marrow (B2) |
| UNDERTOW | Deep-dweller; 19Hz door; hum goes silent | Yes — season arc | Eleven-step block (B1) |
| Paper Letter | Almost-human e's; "standard rate doubles" | Yes — closer | — |

---

**Book 5 — Standard Rate (Planned)**

| Chapter Story | Series Role | Season Arc Beat? | Prerequisite |
|---|---|---|---|
| The Reassembly | Full cast return; each priced | Yes | All B1–B4 |
| The Truth Named | Superminds / thin spots / Read as instruments / entity's run | Yes — revelation | Truth Assembled (B4) |
| The Run | Room to room; paid testimony; paid at standard rate | Yes — series climax | False Death (B4); truth known |
| Unanimity Counterstroke | Blackout/flood; lakefront thin-spot crisis | Yes | — |
| UNDERTOW Defects | 19Hz becomes the city's pulse | Yes | UNDERTOW (B4) |
| The Cost | Array burned; the fork (diminished or quiet) | Yes — death budget | NeoCortex terminal (B4) |
| The Settlement | Receipt; account closed; "standard rate" | Yes — series close | — |
| The Last Image | Mrs. Chen's counter; the code inside the fall | Yes — final image | — |

---

## 2. Standalone Stories {#SS-GLMZ-COORD-§2}

All GLMZ stories not owned by a main series book chapter. Listed alphabetically by Code.
"Serves Book(s)" = which main series book(s) this story's events feed into or establish.

**⚠ Beat counts refreshed 2026-08-10** after the full-corpus Logic Sweep campaign
([[project_truce_logic_sweep_2026_08_10]]) confirmed this table had drifted badly — several
books had grown 5-15x since it was last touched. Two codes below (HFV, OPPN) were retired and
renamed (TWU, TWD) somewhere along the way; rows corrected to the live NodeCode.

| Code | Title | Protagonist | Arc Served | Serves Book(s) | Status |
|------|-------|-------------|------------|----------------|--------|
| ATTE | Attendance | Yemina Fola | Rogue AI · Grey admin-horror | B2–B4 | Active; 330 beats; Logic Sweep PASS 2026-08-10 |
| BLST | Ballast | Teo Candelaria | Aerostatic-bloc descent · household politics | — | Active; 339 beats; Logic Sweep PASS 2026-08-10 |
| DWIACE | Death Whispers in a Cat's Ear | Rennick | Rogue AI · world texture · multi-POV | B2–B3 | Active (559 beats); 1 BLOCKER deferred to author (cat-ear age contradiction) |
| TWU | The Way Up (was HFV/"The Come Up") | Reza "Rampart" Solano + Tavi "Cutout" Jeong | None · Low operator-economy origin · friendship-as-a-phase · **Rampart & Cutout diptych Bk 1** | None (world texture) | Active; 113 beats; Logic Sweep PASS 2026-08-10 |
| TWD | The Way Down (was OPPN/"The Fall Down") | Reza "Rampart" Solano vs Tavi "Cutout" Jeong | None · operator-signature collision · divided loyalties · **Rampart & Cutout diptych Bk 2** | None (world texture) | Active; 85 beats; Logic Sweep PASS 2026-08-10 |
| ICFI | It Came From Iowa | Wes Keith + CJ Anderson | Behemoth world · Iowa flyover · Machine God | B2 | Active; 228 beats; Logic Sweep PASS 2026-08-10 |
| IxS | Iron & Silk | Rook (Inkeri Saarinen) + Ekow Ato | Lotus COMPLETE · Rook Book 4 | B3–B4 | COMPLETE; 1162 beats; Logic Sweep PASS 2026-08-10 |
| CRIT | Critical Mass | Ledger | Synthetic life · emergent-consciousness origin · Fleet mythology · action | B4–B5 (dependency needs rework — see §3 Ledger note) | REPLOTTED 2026-08-02 (was "Double Entry"/audit procedural; full replot, old 14-beat prose retired to history, new 14-beat spine + blueprint generated; entities re-seeded) |
| MNEMO | Mnemosync | Amara & Seto | Neuretics world · Channeler | — | In progress; 593 beats |
| MxG | Magenta & Gunmetal | Rook (Inkeri Saarinen) | Lotus · heist arc · Rook Book 1 | B3 | Active; 281 beats |
| NxR | Neon & Rust | Rook (Inkeri Saarinen) | Lotus · heist arc · Rook Book 2 | B3 | Active; 356 beats; Logic Sweep — 2 BLOCKERs deferred to author (duplicated crisis staging, book-wide POV-law violation) |
| CxC | Crimson & Chrome | Rook (Inkeri Saarinen) | Lotus finale · Rook Book 3 | B3 | COMPLETE |
| PXL | Pixel | Pixel | Channeler origin · Kyle arc · Detroit escape | B1 | 185 beats; Logic Sweep PASS |
| RTR | Read the Room | Faith Larson + Ethan Wolfe | Read (psionic) world · Fenris band | B3–B4 | Active; 267 beats; Logic Sweep PASS 2026-08-10 |
| SPRW | Sparrow | Elias Macias | Rogue AI misdirect · AI mystery | B4–B5 | Active (never fires); 56 beats |
| SRZR | Steppin' Razor | Sasha Võ | Lotus · Halcyon/OBERON | B3 | Active; 297 beats; Logic Sweep PASS 2026-08-10 |
| TLC | The Long Cut | Doc Stash | Medical noir · GLMZ world | — | Active; 757 beats; Logic Sweep PASS 2026-08-10 |
| TEST | Testament | Bear (Boris Johansen) | Military/corporate accountability · GLMZ world | — | Active; 547 beats |
| UNDR | Underclan | Glim | Underground world · Gray Zone | — | IsWIP=1; 56 beats |
| VATD | Vultures at the Door | Tomas Alvarado / Ekow Ato | Vultures world · Renko | B2 | Active; 308 beats; Logic Sweep PASS 2026-08-10 |

**Entity note (2026-08-10, RESOLVED):** TEST had two candidate "Bear" Entity rows because TEST
itself was drafted twice. Beat #9792 has Bear give his own sworn on-page name — "My name is
Boris Johansen [single-s]... Case CE-0217. The Cortland Engagement... Colonel Vasili Brandt,
Halcyon Civil Security" — confirming the live book's canonical facts (Halcyon license, "the
Manowar" powered frame, eleven years of service). The OTHER entity, "Boris Johanssen"
[double-s]/Ironclad Meridian/Glooms Intermodal, turned out to describe a real but **orphaned
earlier draft** of this same book — 12 beats (Numbers 4234-4253) still exist in the DB with that
exact content but have zero chapter memberships, confirming they were superseded and left
disconnected rather than cleaned up. Both entities corrected/annotated to reflect this (neither
deleted). See [[project_cross_book_story_weaving_2026_08_10]] for the full evidence trail.
Separately, a genuine positive finding needing no fix: **Mrs. Chen's diner (BCODA's recurring
counter/safehouse) also appears, consistently and without contradiction, in the Rook Trilogy's
NxR and CxC** — an already-working cross-book anchor, not something to build. Whether TWD's own
unglossed "Bear" (a fixer running a training crew) connects to TEST's Boris Johansen remains
open and unconfirmed — no textual evidence links them yet.

---

## 3. Character Arc Ledger {#SS-GLMZ-COORD-§3}

State of major recurring characters at the close of each main series book.
TBD = not yet determined; locked when that book's outline is finalized.
Series-protected (absent explicit author order): **Sable, Pixel, Mrs. Chen, Echo, Stash.**

### Kyle Strider

| Book | End State |
|------|-----------|
| B1 | Ledger settled (STANDARD RATE written). Correspondence open — entity is a counterparty for the first time. "Ask Marrow" active. Nadia out of ice. |
| B2 | Clock visible (terminal neuretics; Cadence's death = memento mori). Curriculum legible. Lotus temptation growing. One line to the entity about the clock. Reply: *"I know. It is in the rate."* |
| B3 | Fixerless (Sable burned). Chair refused (or retainer-without-colors fork; author decides at outline). Unanimity watching. No institutional cover. Mirrorwell Station scouted. |
| B4 | Dead on every ledger. No accounts, no name, no hum-counterparty. Working from Mirrorwell's gray floors. Array on borrowed time (months). Whole truth known and useless. Paper letter received. |
| B5 | Account settled. At peace. Quiet inside for the first time since sixteen. The code inside the fall. |

### Pixel

| Book | End State |
|------|-----------|
| B1 | Present. Axiom credentials making her visible to collectors of "rare things." |
| B2 | Turned down corpo retainer. Parallel to Kyle's choice left silent on page. |
| B3 | TBD |
| B4 | Runs the NeoCortex decay curve. Delivers the terminal timeline: months, not years. |
| B5 | Series-protected. At Mrs. Chen's counter (end image implied). |

### Sable

| Book | End State |
|------|-----------|
| B1 | Flags Year Three as "too clean" (spine whisper #1). Year One thumb-rest withheld. Fixer relationship intact. |
| B2 | Operational. Lotus warmth B-plot involvement. |
| B3 | BURNED — client list poisoned, cages compromised, name radioactive. Three boxes of paper. One sentimental second at the door. Goes underground. NOT dead. |
| B4 | Underground. Her burned network used as the task force's map to Kyle. |
| B5 | TBD — not committed in RFC 0003; expected return for reassembly. |

### Ledger

| Book | End State |
|------|-----------|
| B1 | Synthetic confirmed (Ch14 Two Favors). Circuit handle. DB 019eafb5. |
| B2–B3 | CRIT (Critical Mass, replotted 2026-08-02): discovers he is the last surviving instance of a distributed Fleet-mind decommissioned in 2213 — the rest of it woken, dying, in a derelict freight terminal. Gets it a body and an individuation of its own before a salvage contract, an acquisition broker, and a dormant decommission clause finish deciding what it is. The old "two-column protocol" / Tally's-ledger audit practice is NO LONGER dramatized in CRIT — it can still be true of Ledger as an off-page/background practice, but B4/B5 should not assume it was established on-page here. |
| B4 | Honest betrayal — fed true/incomplete data; trades crew location; genuinely computes it as survival. The betrayal has no villain. **NEEDS REWORK:** this payoff was written to exploit the "two-column protocol" CRIT no longer dramatizes. Either re-derive the mechanism from Ledger's Fleet-origin/individuation material instead, or re-home "two-column protocol" as something B4 establishes itself. |
| B5 | Redeemed — given complete data; becomes the incorruptible notary. Same property, opposite result. **NEEDS REWORK:** the planned on-page token ("Tally's kept page") no longer exists — Tally is retired from CRIT entirely. B5 needs a new token of completeness, likely drawn from the Critical Mass cast (Renn, Vig, Quire) or the individuated Fleet-instance instead. |

### Mrs. Chen

| Book | End State |
|------|-----------|
| B1–B4 | Counter always open. Series-protected. |
| B5 | Counter is Room One of the run. Final image of the series. |

### Renko Moss

| Book | End State |
|------|-----------|
| B1 | Zeroed leverage. |
| B2 | Full Freight chapter — strangest loyalty in the series matures. |
| B3 | TBD |
| B4 | Sells Kyle's death (the False Death). Paddles, trash route, Marrow's slabs. Full ceremony. Collects full freight. |
| B5 | Drives. |

### Nadia Okafor-Voss (Atlas #5)

| Book | End State |
|------|-----------|
| B1 | Out of ice (Street Meat / The Receipt). First words: "There were nine of you. Ask Marrow." |
| B2 | Participates in curriculum assembly. Points at Marrow from B1 confirmed. |
| B3 | TBD |
| B4 | Translates the Superminds' science for the assembled truth. |
| B5 | Translates into kitchen-table language for the run. |

### Lullaby (first courier)

| Book | End State |
|------|-----------|
| B1–B2 | Not yet found. Sable's Year One thumb-rest is the thread. |
| B3 | Surfaced. Faked death via false-memory insertion. Living as nobody. Hostile witness: *"it builds couriers, then it spends them."* Two theories of the entity now fully evidenced. |
| B4 | TBD |
| B5 | Corroborates as hostile witness whose conversion no one can dismiss. |

### Sparrow

| Book | End State |
|------|-----------|
| B1 | One bar-talk mention only. Seeded. |
| B2–B3 | TBD |
| B4 | A handle hunts him. Hunter never confirmed human. B-plot. |
| B5 | The handle outlives every theory. Deliberately left open. |

### Reza "Rampart" Solano & Tavi "Cutout" Jeong (HFV — The Come Up)

New, recurring-eligible Low-tier operators introduced in HFV. Not book-spanning cast; no prior
state to inherit. Whether they return in a later story is open, not foreclosed.

| Story | End State |
|------|-----------|
| HFV (*The Come Up*, Bk 1) | Both came up from nothing (couch-flop nobodies → named operators) and grew apart in the coming-up. **Reza "Rampart"** — chromed muscle, a known wall-for-hire, rising up-altitude toward the bright tiers and the upgraded life (Coeli); booked solo. **Tavi "Cutout"** — precise splicer/ghost, Gray Zone ground-level by temperament, solo-preferred; still carries the cheap twin pistol Reza upgraded away from. They end as two respected operators in the same trade with nothing in common — the partnership dissolved by a fixer's booking, not a falling-out. Fond, not close. |
| OPPN (*The Fall Down*, Bk 2) | Four years on, opposite numbers: Reza corporate security muscle up-altitude (AshgraveMaterials); Tavi freelance ghost. They collide on one job (extract vs hold the courier Rafi Sarkissian), recognize each other by operator signature, and each chooses the bond over the contract at the decisive instant. Cost: **Reza falls** from up-altitude (Onwe burns the retainer; Coeli's world tilts); **Tavi's** unbreakable-ghost rep is **dented** (didn't deliver). The Package walks; neither corp wins. They part permanently opposed and permanently bound by knowing — the only two who truly know each other, now on opposite sides. **Book 3 hook:** both burned; the powers that ground them together will pull them again. |

---

## 4. Season Villain Supply Chain {#SS-GLMZ-COORD-§4}

Five villain shapes — each retroactively reframes the prior. Commerce planted one book early,
collected one book late. Full detail: RFC 0003 §1.1.

> **⚠ B1 CANON CORRECTION (2026-07-11):** The RFC 0003 draft assigned Casper Vey "the
> Stationer" as B1's season villain. BCODA prose (COMPLETE) established **Mr. Able** as B1's
> villain, with Vey ending sympathetically. The villain supply chain below reflects the prose-
> confirmed B1 villain. Before B2 prose begins, a **B1 retrospective brief** must identify:
> (a) which entity takes Vey's commerce role as the information broker feeding future books;
> (b) whether Vey can still route files to B2/B4 villains despite his sympathetic portrayal;
> (c) whether Mr. Able has his own commerce thread forward into B2.

| Villain | Book | Shape of Evil | Status |
|---------|------|---------------|--------|
| **Mr. Able** | B1 | TBD — confirmed in BCODA prose; brief needed to characterize his "new shape" and commerce forward | ✅ Prose-confirmed; characterization needs B1 retrospective |
| Casper Vey | B1 | Originally: evil without temperature (file broker, courteous shop). **Prose: sympathetic.** He may still operate as the trade's information hub without being a villain — role TBD | Requires B1 retrospective |
| Saint / carrier #4 | B2 | Evil with a pulpit — same hardware as Kyle, opposite answer; harvest network | RFC 0003 planned; commerce from B1 broker TBD |
| Sunny | B3 | Evil without arithmetic — appetite; no rate; cannot be read or negotiated | RFC 0003 planned; imported via Lotus pivot (Reiko Oka) |
| The Registrar | B4 | Evil that loves — grandmother archetype; registers people tenderly; dossier bought at B1 broker | RFC 0003 planned; B1 commerce source TBD |
| The Receiver | B5 | Evil as reasonableness — collapse administrator; consensus of ownership; last temptation | RFC 0003 planned; procured Underwriter's pivot (B3) |

**Commerce chain (to be re-confirmed after B1 retrospective):**
- B1 broker → B2: hunting lists for Saint's carrier harvests
- B1 broker → B4: Atlas-9 dossier that outs Kyle
- B2 Saint harvest network → Marrow's body-ledgers (dark half)
- B3 Sunny: imported with Receiver's money (retroactive, B5)
- B3 Lotus pivot blackmail trove → B4 Registrar's evidence (subpoenaed wholesale)

---

## 5. Cross-Story Plant/Payoff Registry {#SS-GLMZ-COORD-§5}

Both sides of every cross-story plant must exist in the Story Roster before the plant is written.
Update this table when a plant is seeded or paid.

| Plant | Origin Story | Payoff | Payoff Story | Status |
|-------|-------------|--------|-------------|--------|
| "Ask Marrow" (Nadia's first words) | BCODA / The Receipt | ATLAS-9 continuity; 9 carriers revealed | B2 / Marrow chapter | B1 ✅; B2 TBD |
| Roster marker in the drawer | BCODA | Kyle is carrier #7 of 9 | B2 / Marrow chapter | B1 ✅; B2 TBD |
| Mercer's bundle | BCODA | Tessaline B-plot spent | B2 / Tessaline | B1 ✅; B2 TBD |
| War Dog's rooftop promise | BCODA | War Dog rematch; new info source | B2 / War Dog Rematch | B1 ✅; B2 TBD |
| Renko's zeroed leverage | BCODA | Loyalty matures; False Death sold | B2 + B4 | B1 ✅; TBD |
| Sable's Year One thumb-rest | BCODA / Year Three | Lullaby found; dark gospel | B3 / Year One = Lullaby | B1 ✅; B3 TBD |
| Eleven-step block (19Hz, first noted) | BCODA | Ezra's band / live venue | B3 | B1 ✅; B3 TBD |
| 19Hz / Ezra's band | B3 | UNDERTOW's door | B4 / UNDERTOW | B3 TBD; B4 TBD |
| 19Hz / UNDERTOW's door | B4 | City's pulse — UNDERTOW defects | B5 / UNDERTOW Defects | B4 TBD; B5 TBD |
| Null Crow on the ledge | BCODA | Crows report to someone (misdirect) | B3 or B4 | B1 ✅; TBD |
| Sparrow (one bar-talk mention) | BCODA | The handle; the AI mystery | B4–B5 | B1 ✅; TBD |
| Stationer's counter, unburned | BCODA / The Receipt | ⚠ RFC: Dossier trade arms Registrar → B1 retrospective needed (Vey sympathetic) | B4 | B1 ✅; commerce source TBD |
| Pixel's Axiom credentials visible to collectors | BCODA | Pixel targeted by corpo interest | B2–B3 | B1 ✅; TBD |
| Correspondence letter 1: "Agreed." | BCODA / The Receipt | Letter 2 (B2); letter 3 (B3); letter 4 (B4); receipt (B5) | B2 → B3 → B4 → B5 | B1 ✅; B2–B5 TBD |
| Mirrorwell Station (scouted, B3 set piece) | B3 | Kyle's hiding world after False Death | B4 / The False Death | B3 TBD; B4 TBD |
| Lullaby's dark gospel (B3) | B3 | Avatar theory made credible; reader believes it | B4 / Avatar Theory Peaks | B3 TBD; B4 TBD |
| Whole truth assembled and useless (B4) | B4 | The inversion: witness IS the delivery | B5 / The Truth Named | B4 TBD; B5 TBD |
| Mr. Able (B1 villain) | BCODA | Commerce forward TBD — requires B1 retrospective | B2+ | B1 ✅; TBD |
| Two-column protocol ("true is not complete"; certifying gaps, not just entries) | CRIT (retired 2026-08-02 replot — no longer dramatized on-page) | Protocol exploited — data engineered to look complete; the honest betrayal | B4 / Ledger's Honest Betrayal | ⚠ ORPHANED — establishing story replotted; B4 needs to either re-derive the mechanism or establish it itself |
| Tally's kept page (true/incomplete entry about Ledger) | CRIT (retired 2026-08-02 replot — Tally cut from the story entirely) | The on-page token of completeness when Ledger is given complete data | B5 / The Reassembly (notary material) | ⚠ ORPHANED — Tally no longer exists in CRIT; B5 needs a new token, likely from Critical Mass's cast or the individuated Fleet-instance |
| "The other two Helix facilities" (future job hook) | CxC (beats 6550-6551) | Acknowledged as still-unresolved in Rook's closing open-threads tally (2026-08-01 logic sweep fix); no future book has confirmed a payoff | IxS (acknowledgment only, not a resolution) / a future Rook book TBD | CxC ✅ planted; IxS ⚠ intentionally left orphaned — needs a future book or explicit retirement |

---

## 6. World-Revelation Sequencing {#SS-GLMZ-COORD-§6}

No story — main series or standalone — may reveal anything in this table before its designated
book. Standalone stories may *allude* to mysteries but not answer them.

| Revelation | Book | Lock |
|------------|------|------|
| ATLAS-9: 9 carriers seeded; Kyle is #7 | B2 | 🔒 No story before B2 may confirm this |
| Terminal neuretics overclock = the bill for all carriers | B2 | 🔒 |
| The decade of jobs = a deliberate curriculum | B2 | 🔒 No story before B2 may name the pattern explicitly |
| Two incompatible theories of the entity (both fully evidenced) | B3 | 🔒 |
| Unanimity is actively managing, not just observing | B3 | 🔒 |
| Superminds caused the thin spots (tent-pole discovery) | B4 | 🔒 HARD LOCK — no story before B4 may confirm or strongly imply causation |
| Publication is filing with the defendant (the plan is impossible) | B4 | 🔒 |
| The courier IS the delivery (the inversion) | B5 | 🔒 |
| The meltdown is not averted — CorpoNations unbeaten | B5 | 🔒 Charter lock (RFC 0003 §0.2) |

---

## 7. Entity Seeding Roadmap {#SS-GLMZ-COORD-§7}

Entities that must exist in the DB before prose begins in each book.
Verify: `sqlcmd -S "(localdb)\MSSQLLocalDB" -d Prose -Q "SELECT Name FROM Entities WHERE Name LIKE '%<name>%'"`

### Before Book 2 prose begins

- [ ] Marrow — Carrion Enterprises archivist; never goes outside; species=human
- [ ] Saint / carrier #4 of ATLAS-9 — species=human; preacher; augmented; harvest network
- [ ] Cadence / carrier #2 of ATLAS-9 — species=human; terminal overclock; Ascent-adjacent
- [ ] Reiko Oka "the Underwriter" — Lotus modernizer; votes; balance sheets *(introduced B2; season B3)*
- [ ] Mira — Lotus; 60-day clocks; Branch Manager's circle
- [ ] Tessaline — faction entity (Mercer's bundle B-plot)
- [ ] B1 commerce source (Vey or Mr. Able's network) — pending B1 retrospective

### Before Book 3 prose begins

- [ ] Lullaby — species=human; first courier; false-memory false death; living as nobody
- [ ] Sunny — species=human; imported Lotus muscle; no rate; appetite only
- [ ] Mirrorwell Station — place; arcology; gray floors; gray zone
- [ ] Reiko Oka *(if not seeded at B2)*

### Before Book 4 prose begins

- [ ] The Registrar — species=human; grandmother archetype; hunters; preserves and candles
- [ ] UNDERTOW — species=ai; 19Hz resonance; lives under Chicago; not Unanimity; afraid
- [ ] Psyker-hunt task force — faction

### Before Book 5 prose begins

- [ ] The Receiver — species=human; Big-20 collapse administrator; tailored coat; no appetite
- [ ] All reassembly cast *(should be seeded by this point via prior books)*

---

## 8. Pending Retrospective: B1 Canon vs RFC 0003 {#SS-GLMZ-COORD-§8}

Before Full Freight (B2) prose begins, a **B1 retrospective brief** must be filed at
`docs/planning/BCODA-retrospective.md` answering:

1. **Mr. Able** — what is his actual role, commerce thread, and "new shape of evil"?
   Does his network seed B2 or later villains?
2. **Casper Vey** — sympathetic in prose. Can he still route information to B2/B4 villains
   without being a villain himself? (A sympathetic broker who doesn't know how his files are
   used is consistent with RFC 0003's "evil without temperature" — that shape may still apply
   without Vey being the *antagonist*.)
3. **Which open B1 threads** (see RFC 0003 §3 "deliberately left open") survived into the
   final prose, and which were resolved differently on the page?

This retrospective feeds the B2 story brief and the Season Villain Supply Chain update above.
