# Fix Plan — Underclan (UNDR) {#SS-UNDR-FIXPLAN-2026-07-03}

Source: `docs/nodes/UNDR.md` (bible) + `audit-outlines-2026-07-03/UNDR.md` (structural audit).
Book node `underclan-019eff97`, 14 chapters, 55 live beats (verified against DB, read-only).
This plan is Sonnet-draft stage of a draft -> Opus-polish pipeline. No DB writes were made.

## Verified housekeeping (no action needed)

- **Audit finding 11 (duplicate Ch09 beat) is already resolved at the DB level.** Beat 4913
  (`019F1220-C1B5-7B1D-B33D-5C25FCB20931`) is `IsEnabled=0`; only beat 4914
  (`019F1221-3430-7DE2-BF4A-E86EC38AE489`, SortKey 350) is live. No DISABLE action required.

## Action counts

| Action | Count |
|---|---|
| KEEP | 42 |
| PATCH | 7 |
| REWRITE | 5 |
| NEW | 3 |
| DISABLE | 0 (already handled — see above) |
| **Total beats touched (non-KEEP)** | **15** |

## Requirement -> beat map

| # | Requirement | Beats implementing it |
|---|---|---|
| 1 | Plant + pay off the Tartarian navigation maze | NEW Ch02 beat (`ch02-350-what-doesnt-hold`, Marl + fragment plant); PATCH Ch09 4801 (route-confidence foreshadow, second plant); REWRITE Ch11 4810 (maze failure during flight); REWRITE Ch11 4811 (maze resolves + Sorrel's threshold refusal); PATCH Ch12 4812 (headcount confirms the cost, ambiguity preserved) |
| 2 | Corwin Sallow physical, on-page, does damage | REWRITE Ch10 4805 (Sallow named, embodied, kills Leaf during the Hollow raid) |
| 3 | Daylight Mission on-page coercive/culture-dissolving act | NEW Ch11 beat (`ch11-250-the-price-of-help`); PATCH Ch11 4809 (ties the delay to Slip's death) |
| 4 | Close/cut abandoned plants + identification gap + Lark-cough contradiction | REWRITE Ch03 4782 (basin object — removes false face-match, fixes hoard-depth logic, keeps artifact for Ch14 payoff); NEW Ch07 beat (`ch07-250-what-the-file-said`, Noor's scar/cowlick ID); PATCH Ch01 4827 (closes the pre-contact cough as a separate, resolved thing); PATCH Ch10 4806 (on-page acknowledgment that the Fever cough is not that earlier cough); REWRITE Ch14 4826 (shine payoff at the final Fare, replaces the "mother" continuity slip) |
| 5 | Leaf established as a person before her death | PATCH Ch01 4760 (names and characterizes Leaf); REWRITE Ch10 4805 (Leaf's on-page death, at Sallow's hand) |
| 6 | Resolve Sorrel's fork on the page | REWRITE Ch11 4811 (Sorrel refuses to cross into the Tartar, stays at the threshold); PATCH Ch12 4812 (the count comes up short — the loss registers without being over-explained, preserving the ambiguity lock) |

Sorrel's loss is folded into the existing, already-anonymous "a third whose name he repeated to
himself once" in Ch14 beat 4823 (`062E6E4C-6FF8-48EA-96A6-82B36747FE86`) — that beat is left as
KEEP. Once 4811/4812 establish on the page who was lost and where, 4823's existing restraint reads
as intentional (he can't yet say her name) rather than as an unexplained gap. No change to 4823 is
needed or made.

## Bonus fixes (not in the mandatory 6, cheap, done anyway)

- **Ch08 4798 — retired-term violation.** "A Rider's crawler... the Rider's own body waited
  somewhere above" reintroduces "Rider," retired by SS-A38. PATCHed to "Exo" (street register,
  correct for an illegal safari operation), consistent with Noor's civilian vocabulary.

## Judgment calls flagged (NOT executed — outside the mandatory 6, noted per audit but left alone)

- **Grale's "curdle" (audit finding 10).** The audit wants an intervening beat where Grale's
  vindication turns into active harm (advocating exile, sabotage) before his sacrifice. This is a
  real gap but was not in the 6 mandatory requirements and reworking his Ch13 arc risks undercutting
  the sacrifice that now does double duty (thematic counterweight to Sorrel's refusal — see below).
  Left as KEEP. Flagging for a future pass.
- **Population-scale mismatch (finding 12)** — Hollow=42 vs. bible's "few hundred across Homewater."
  Out of scope; would require a throwaway line establishing the Hollow as one of several communities.
  Not touched.
- **SRZR/Halcyon cross-pollination (finding 14)** — inert in this book per audit. Not mandated here;
  leaving 4814's ambiguous pulse exactly as written (touching it risks resolving Lock 1/9 by accident).
- **Sorrel + Grale as parallel sacrifices.** Once 4811 is rewritten, the book now has two peer
  characters lost in Act 3 by different mechanisms: Grale to human antagonists (the Lamplighters'
  equipment, a chosen tactical death) and Sorrel to the taboo/unknown (a refusal, deliberately
  unconfirmed). This is an intentional echo, not padding — flagging it explicitly so the Opus pass
  doesn't flatten one to match the other.
- **Basin-object payoff placement.** The audit suggested three options: pay it off at the reunion, at
  the ending Fare, or cut it. This plan resolves it at the **ending Fare** (Ch14 4826), via Glim's own
  realization rather than a physical recovery — the object stays in the Oarsman's hoard forever (per
  the Fare rule: it cannot be reused or returned), so a physical reunion-payoff would break the
  Oarsman's own law. Flagging this choice as deliberate.

## Per-chapter beat map

Legend: **K**=KEEP, **P**=PATCH, **R**=REWRITE, **N**=NEW. SortKeys unchanged unless noted.

### Ch01 — The Breath of the Deep (`019EFF98-3C78-71A4-BDE6-87C7C3BFFEAF`)

| Beat | Action | SortKey | Note |
|---|---|---|---|
| 4760 | **P** | 100 | Name and characterize Leaf (one of the two elders at the chemical-heat stone). Serves Req 5. |
| 4761 | K | 200 | — |
| 4762 | K | 300 | — |
| 4763 | K | 400 | — |
| 4827 | **P** | 500 | Close the Lark-cough thread on the page: she reports it, it's dust from the harvest, it passes in a day. Distinct, resolved, and explicitly not the Fever. Serves Req 4. |

### Ch02 — The One Word (`019EFF98-43E9-7EFA-BF7C-9A88EC27CEFE`)

| Beat | Action | SortKey | Note |
|---|---|---|---|
| 4764 | K | 100 | — |
| 4765 | K | 200 | — |
| 4766 | K | 300 | — |
| **NEW** `ch02-350-what-doesnt-hold` | **N** | 350 | Vesh/Glim: the story of Marl, the Brave who went deepest and came back wrong about where he'd been, and the carved-stone fragment he carried up — sourceless, kept, never explained. Serves Req 1 (plant #1). |

### Ch03 — The Fare (`019EFF98-4B01-71C3-B33B-CB7A1CA124DF`)

| Beat | Action | SortKey | Note |
|---|---|---|---|
| 4780 | K | 100 | — |
| 4781 | K | 200 | — |
| 4782 | **R** | 300 | Removes the false "matches the face in the fragments" claim (4781 explicitly states there is no face in the fragments — direct contradiction). Reframes the pull toward the basin object as bodily/inexplicable, not memory-matching. Adds a line establishing the basin's visible layer cycles down into a sealed hoard below, explaining how a twelve-year-old Fare could resurface near the top. Serves Req 4. |

### Ch04 — The Surfacing Called (`019EFF98-52CD-78C2-B9D1-8C3093D08761`)

All four beats (4783, 4784, 4785, 4828) — **K**. Strong as written; no requirement touches this chapter.

### Ch05 — Surface-Stained (`019EFF98-5A1D-790D-9518-B6FC1AD5755E`)

All three beats (4786, 4787, 4788) — **K**.

### Ch06 — The Lid Off the World (`019EFF98-61B3-7C5E-9B03-5698F48F684E`)

All four beats (4789, 4790, 4791, 4792) — **K**.

### Ch07 — The Smell Before the Face (`019EFF98-68B3-74C4-B18D-D89906E289EF`)

| Beat | Action | SortKey | Note |
|---|---|---|---|
| 4793 | K | 100 | — |
| 4794 | K | 200 | — |
| **NEW** `ch07-250-what-the-file-said` | **N** | 250 | [Marked Noor POV] The identification chain the audit found missing: a caseworker who has kept Noor's twelve-year-old file open flags a scar and a cowlick against intake photos; Noor is shown stills before she's allowed near him and recognizes him — by mark, not by face — before she dares believe it, per bible §4. This is the causal mechanism that gets her to the doorway in 4795. Serves Req 4. |
| 4795 | K | 300 | — |
| 4796 | K | 400 | — |
| 4829 | K | 500 | — |

### Ch08 — A Name He Will Not Answer (`019EFF98-6FBA-7D56-B8F2-A1DC4E4E1460`)

| Beat | Action | SortKey | Note |
|---|---|---|---|
| 4797 | K | 100 | — |
| 4798 | **P** | 200 | Retired-term fix: "a Rider's crawler... the Rider's own body" -> "an Exo's crawler... the Exo's own body" (SS-A38 compliance). Bonus fix. |
| 4799 | K | 300 | — |
| 4800 | K | 400 | — |

### Ch09 — The Way Down (`019EFF98-76E7-74EE-9F59-34A0013B1869`)

| Beat | Action | SortKey | Note |
|---|---|---|---|
| 4801 | **P** | 100 | Adds a brief moment where Glim's memorized route home doesn't match his memory exactly at one junction — he corrects by instinct and dismisses it as fatigue. Second plant for the maze mystery, light-touch. Serves Req 1 (plant #2). |
| 4802 | K | 200 | — |
| 4803 | K | 300 | — |
| 4914 | K | 350 | Live copy confirmed (4913 already disabled — see housekeeping note above). |

### Ch10 — Light Comes Down (`019EFF98-7EB7-76A4-B97E-9F0CDDB48794`)

| Beat | Action | SortKey | Note |
|---|---|---|---|
| 4804 | K | 100 | — |
| 4805 | **R** | 200 | Corwin Sallow enters the raid bodily, named, voiced, and takes a trophy — and it is Sallow's hand, specifically, that kills Leaf in the whiteout. Converts the "anonymous voice" into the antagonist ladder's rung 3. Serves Req 2 and Req 5. |
| 4806 | **P** | 300 | Adds one line explicitly distinguishing this cough (productive, systemic, the Fever) from Lark's earlier dust-cough weeks before — on-page acknowledgment rather than silent contradiction. Serves Req 4. |
| 4807 | K | 400 | — |

### Ch11 — When the Candles Sicken (`019EFF98-85C7-7442-8B94-37A2567A4626`)

| Beat | Action | SortKey | Note |
|---|---|---|---|
| 4808 | K | 100 | — |
| 4809 | **P** | 200 | Light edit connecting Slip's crisis to the new coercive-act beat that now precedes it structurally (the medicine that finally arrives is the medicine Noor took by force from the Mission's own procedure). Serves Req 3. |
| **NEW** `ch11-250-the-price-of-help` | **N** | 250 | The Mission's coercive act, on the page: the field administrator will not release the fever medicine without scanning/registering Slip into the Mission's system first — sincere, procedural, unbudging — and Knuckle and Noor have to force the exchange while Slip's fever climbs. Costs real minutes. This is rung 4 of the antagonist ladder finally doing damage. Serves Req 3. |
| 4810 | **R** | 300 | Slip's sending-off and the second Lamplighter raid stand; the flight into the Old Deep and toward the Tartar now genuinely fails to hold — wrong junctions, a wall where memory placed passage, backtracking under pursuit — before the chain finds a way through. Serves Req 1 (payoff). |
| 4811 | **R** | 400 | The chain's movement through the (still-disoriented) dark; Noor's uncanny composure stands; and at the taboo threshold — the line past which no living Brave has gone — Sorrel stops. She will not cross it. She stays to hold the line against the pursuit instead. Glim leaves her there. Unconfirmed, unresolved, per the ambiguity lock. Serves Req 1 (payoff) and Req 6. |

### Ch12 — The Ghost-Country (`019EFF98-8CF4-71A2-9ECC-0F54198F8CFE`)

| Beat | Action | SortKey | Note |
|---|---|---|---|
| 4812 | **P** | 100 | The arrival headcount is corrected (one short of the chain that entered the Old Deep) and Glim's counting ritual — established since Ch01 — lands on the gap without naming it outright. Serves Req 1 and Req 6. |
| 4813 | K | 200 | — |
| 4814 | K | 300 | — |

### Ch13 — The Second Word (`019EFF98-946E-7770-946D-90058B5AF0D4`)

All four beats (4815, 4816, 4817, 4818) — **K**. Grale's sacrifice stands as written (see judgment
call above re: the un-executed "curdle").

### Ch14 — A New Fare (`019EFF98-9BCC-785C-AB65-39A4AFA4E2D4`)

| Beat | Action | SortKey | Note |
|---|---|---|---|
| 4823 | K | 100 | Unchanged — its existing restraint ("a third whose name he repeated to himself once") now reads correctly once 4811/4812 land. |
| 4824 | K | 200 | — |
| 4825 | K | 300 | — |
| 4826 | **R** | 400 | Replaces the "did she go down into the Underclan willingly" continuity slip (finding 15) with Glim's recognition, while paying his final Fare, of what the basin-object in Ch03 must have been — his own child-ident, given up at four, still resting in the Oarsman's hoard forever. Closes the Ch03 plant. Serves Req 4. |

## Files

All beat files live in `audit-outlines-2026-07-03/fixes/UNDR/beats/`, named
`<chapter>-<order>-<slug>.md`, one per PATCH/REWRITE/NEW beat (16 files). Each contains a header
block (action, target beat Id or NEW, chapter node Id, BeatTitle, synopsis, proposed SortKey)
followed by full draft prose in the DEEP register, 400–900 words.
