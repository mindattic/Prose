# UNDR (Underclan) — Logic & Continuity Audit

Report-only. No prose, beats, or DB rows were modified. Source: all 14 ChapterNodes under
`underclan-019eff97` (56 enabled beats read in full, plus a check of disabled beats), and
`docs/nodes/UNDR.md` (this is the correct current path — `docs/strands/UNDR.md` does not exist).

## Verdict

**13 findings: 3 BLOCKER / 4 MODERATE / 6 MINOR.**

The book's causality, timeline, and most plant/payoff threads are sound and in several places
(the shine/Oarsman payoff, the maze's Ch02→Ch11 echo, Noor's scar/cowlick identification, the
Mission-medicine tragedy) genuinely well-executed. But there is one clean, reader-visible
contradiction that undermines the entire back third of the book: **Sorrel is written both as
permanently lost at the Tartar threshold in Ch11 and as alive and working alongside the clan in
Ch12–Ch13**, with Ch14 then confirming the "lost" version. There is also a same-chapter arithmetic
error in the survivor headcount (Ch12), and the running population count never reconciles cleanly
back to its own Ch08 baseline. These read as leftover residue from a structural revision pass
(the maze/antagonist beats were evidently patched into Ch11 later — see the maze finding below —
without a corresponding pass through Ch12–14).

---

## 1. Causality chain

Walked all 14 chapters beat-by-beat. Most causal chains hold:
- Glim's Surfacing call (Ch04) → climb (Ch05) → capture (Ch06) → surface stay (Ch07-09) → return
  with Noor (Ch09) → Lamplighter raid finds Homewater (Ch10) → Bright Fever spreads (Ch10-11) →
  Mission bureaucracy costs Slip his life (Ch11 SortKey 250.0/300.0) → second raid forces flight
  (Ch11) → Tartar shelter (Ch12) → counter-raid on the Bindery (Ch13) → rebuild (Ch14). Every major
  turn has an established, on-page cause.
- **Finding (MODERATE):** Candle-Two, who becomes the clan's presumptive new Listener by the end
  (Ch14 SortKey 100.0/400.0, beat 4823/4826 — "Knuckle thinks it's Candle-Two. I think he's
  probably right."), never appears, is never named, and has no scene with Glim anywhere in
  Ch01–13. A structurally important payoff (who inherits Vesh's role) rests on a character
  introduced in the same chapter she's promoted in. *Fix:* seed one earlier appearance (e.g. among
  the Braves in Ch02's teaching-chamber roster, or a specific action during the Ch10–12 crisis).
- **Finding (MODERATE):** The Ch11 maze failure is resolved via a capability ("the second branch
  he'd marked, half-remembered, on that same solitary descent," Ch11 SortKey 300.0, beat 4810)
  that is introduced in the very same beat it is used to save the clan — the "solitary descent" is
  never depicted or referenced anywhere in Ch01–10. See §4 and Extra Item (c) below for detail.
- No other capability, knowledge, or object is used before its establishment. Grale's Bindery
  sacrifice (Ch13 SortKey 400.0) is fully motivated by everything shown of his character since
  Ch01. Noor's document-filing competence (Ch11 SortKey 250.0, Ch13 SortKey 300.0) is grounded in
  her established Mission/caseworker background (Ch07 SortKey 250.0).

## 2. Knowledge states

Tracked what each named character knows and when:
- **Glim** learns he was found at the Works-border at 12 (Ch03, told by Knuckle), sees the
  shine-token's face in the Oarsman's basin at ~12 (Ch03 SortKey 300.0, "four years... by the time
  Knuckle called the Surfacing"), learns the surface has dark-vision crawler-machines on Day 4 of
  captivity (Ch08 SortKey 200.0), and only understands the basin-object was his own child-ident at
  the very end (Ch14 SortKey 400.0). He acts consistently with this knowledge state throughout —
  he never references the crawler-machines before Ch08, never explains the basin object to anyone
  before Ch14. No violations found.
- **Noor** learns of the match via Odalys's call (Ch07 SortKey 250.0) and acts on it correctly
  thereafter (never over- or under-claims certainty before the doorway confirmation).
- **Grale** knows only what's shown (his lifelong suspicion, per Ch01/04/05); he does not later
  reference anything he wasn't shown learning.
- **Vesh** conceals his own Hot Breath infection from Ch10 (implied) through Ch12 SortKey 200.0,
  which is explicitly interrogated on-page ("The Hot Breath was already in you when you agreed to
  take the Mission's medicine for Lark" / "Lark needed it more urgently") — a deliberate, motivated
  concealment, not a knowledge-state error.
- **Finding tied to Sorrel (see §5/BLOCKER):** Sorrel's knowledge/location state is the one place
  this breaks down — she is written as both aware-and-present (Ch12/13) and absent-since-the-seam
  (Ch11/14), so no consistent knowledge state can be assigned to her for the second half of the
  book.

## 3. Timeline

Reconstructed the story clock: ~12 sleep-cycles to Surfacing (Ch01) → called early to "the next
sleep" (Ch04, see Finding below) → climb/capture same night (Ch05-06) → 8 days with Noor (Ch07-09,
explicit: "three days," "on the fourth day," "on the sixth night," "left on the eighth night") →
return to Homewater, first raid "the third day after his return" (Ch10) → Bright Fever escalation
and Slip's death (Ch11, days not itemized but consistent with a fast pediatric fever) → flight to
the Tartar → Vesh dies "on the third day back" (Ch14 SortKey 200.0, "back" = back in Homewater
after the counter-raid) → Glim departs "at the end of the month" (Ch14 SortKey 400.0).

- No impossibilities found in travel, healing, or event pacing — the fever's lethal speed in a
  seven-year-old (Slip) and the elderly Vesh's slower decline are both biologically plausible given
  "no immunity" (bible §2/§3, Lock 2).
- **Finding (MINOR):** Ch04 SortKey 100.0 (beat 4783) opens "Knuckle called the Surfacing three
  sleep-cycles early," but the scene's own dialogue moves the timeline from "twelve more
  sleep-cycles" (Ch01) to "the next sleep" — the full twelve, not three. *Fix:* correct the header
  line to match the dialogue, or vice versa.
- **Finding (MINOR):** Vesh states "I have spent seventy years listening to the Current" (Ch12
  SortKey 200.0, beat 4813); the narration separately says he'd been "its primary attendant for
  sixty-three years" (Ch14 SortKey 200.0, beat 4824). Plausibly reconcilable (listening since
  childhood vs. formal Listener tenure) but never clarified on the page.

## 4. Plant/payoff ledger

**Column A — Plants and their payoffs:**

| Plant | Location | Payoff | Location |
|---|---|---|---|
| Marl's fragment / "wall where the passage had been" story | Ch02 SortKey 350.0 (beat 4980) | Direct echo: "The wall was where the passage should have been" | Ch11 SortKey 300.0 (beat 4810) — near-verbatim callback, well executed |
| Face/object in the Oarsman's basin (the shine) | Ch03 SortKey 300.0 (beat 4782) | Revealed as Toby's own transit-ident, surrendered at 4 | Ch14 SortKey 400.0 (beat 4826) |
| Noor's eyebrow scar + cowlick (missing-child match) | Ch07 SortKey 250.0 (beat 4981) | Confirmed at the doorway reunion, same chapter | Ch07 SortKey 300.0 (beat 4795) |
| Sorrel's unexplained palm scar | Ch01 SortKey 200.0/300.0 (beat 4761), reiterated Ch04 SortKey 100.0 (beat 4783) | **ORPHANED** — never explained anywhere in Ch01-14 | — |
| Candles as air/health oracle (Lark's early dust-cough, benign) | Ch01 SortKey 500.0 (beat 4827) | Escalates correctly into the real Bright Fever cough (Ch10 SortKey 300.0) that is explicitly contrasted with the earlier benign one | Ch10 SortKey 300.0 (beat 4806) |
| Grale's lifelong "surface-stained" suspicion | Ch01/04/05 | Resolves as heroic self-sacrifice, not the bible's promised "curdle" | Ch13 SortKey 400.0 (beat 4818) — see Extra Item (a), flagged as a bible/prose mismatch, not a clean payoff of what was planted |
| "Second Word" ambiguity (One Word teaching) | Ch02 SortKey 300.0 (beat 4766) | Deliberately never confirmed; single unrepeated "pulse" felt in deepest Tartar | Ch12 SortKey 300.0 (beat 4814), Ch13 SortKey 400.0 (beat 4818) — correctly upholds Lock 1 |

**Column B — Payoffs and their plants:**
- Candle-Two as new Listener (Ch14) — **UNPLANTED** (see §1).
- The maze's "second branch" alternate route (Ch11 SortKey 300.0) — **weakly planted**: the
  capability that saves the clan (a previously-walked alternate branch) is introduced in the same
  beat it resolves the crisis rather than being shown or referenced earlier. See Extra Item (c).
- The "full Fare" of three objects at once (Ch14 SortKey 400.0) — **UNPLANTED** against the bible's
  own rule (§4: a Fare is "any single object, freely surrendered"). No earlier beat establishes
  that a multi-object "full Fare" exists as a concept. Minor, but worth a one-line earlier seed.

## 5. Orphan references

Only one disabled beat exists in the entire node tree (checked via `IsEnabled=0`): an early,
truncated draft duplicate of the Ch09 opening ("Homewater smelled right...") superseded by the
enabled beat 4914 at Ch09 SortKey 350.0. It is not referenced by anything and causes no dangling
references — the corpus is otherwise fully enabled with no evidence of a merge/cut leaving stray
callbacks. **No orphan-reference problems caused by disabled content.**

However, one live, on-page contradiction functions like an orphan reference and is severe enough
to lead this report:

- **BLOCKER — Sorrel's fate contradicts itself across Ch11–14.** Ch11 SortKey 400.0 (beat 4811)
  has Sorrel refuse to cross into the Tartar and stay behind at the seam ("I'm not going past
  this"; "he came up one short, and did not say her name aloud"). Ch14 SortKey 400.0 (beat 4826)
  confirms this is permanent: "He had stood here after Sorrel did not come back through the Old
  Deep." **But in between**, Ch12 SortKey 200.0 (beat 4813) states "Noor was with Sorrel" inside
  the Tartarian vault the clan has just fled into, and Ch13 SortKey 100.0 (beat 4815) has "Noor and
  Sorrel" organizing food stocks together, and Ch13 SortKey 400.0 (beat 4818) has Sorrel actively
  working alongside a Cog Runner on the Bindery raid's mechanics. These three beats put Sorrel
  physically present, uninjured, and functioning normally in scenes that occur strictly *after* the
  point where Ch11 says she stayed behind and *before* Ch14 confirms she never returned. Two
  mutually exclusive states of the same character are both asserted as fact on the page.
  *Likely cause:* this looks like a structural-revision artifact — the maze-failure/Sorrel-refusal
  beat in Ch11 reads as a later addition (consistent with the completed task "UNDR structural fix —
  plant maze, build antagonists") that was never reconciled against the pre-existing Ch12/13 beats
  written before that patch. *Fix:* either (a) rename the Ch12/13 Sorrel mentions to another Brave
  (Knuckle, or an unnamed Brave) if her departure in Ch11 is the intended canon, or (b) cut the Ch11
  refusal and Ch14's "did not come back" line if her survival is intended.

- **BLOCKER — Same-chapter headcount contradiction.** Ch12 SortKey 100.0 (beat 4812) states "Forty-
  one people breathing" and "all forty-one people around him" (twice), which correctly reflects the
  post-Sorrel count established at the end of Ch11 (40 Underclan + Noor = 41). Two beats later in
  the *same chapter*, with no death or arrival in between, Ch12 SortKey 300.0 (beat 4814) states
  "forty-two people in the ghost-country without a home to go back to." *Fix:* change "forty-two"
  to "forty-one" in beat 4814.

## 6. Bible agreement

Read `docs/nodes/UNDR.md` in full. Most of the bible's locks and register rules are honored on the
page: DEEP CURRENT is never confirmed (Lock 1, upheld at Ch12 SortKey 300.0 and Ch13 SortKey
400.0); the Bright Fever is treated as plain biology, never mysticism (Lock 2, upheld throughout
Ch10-11); Glim's narration stays in-register with proper one-time glosses ("vehicle," "table,"
"window" are each explained once in Glim's own terms, e.g. Ch07 SortKey 100.0/300.0); the Tartarian
ambiguity is never resolved (Lock 9, upheld — Vesh and Glim's final exchange in Ch12 SortKey 200.0
deliberately leaves it open); the Daylight Mission is played as sincere and is the more devastating
antagonist (Ch11 SortKey 250.0, the Adaeze/registration scene directly costs Slip his life) —
exactly as the bible's antagonist ladder (§8) specifies.

Two prose facts contradict or fail to support the bible:

- **MODERATE — Grale's "curdle" never happens.** Bible §4/§8 promises that Grale's rightness
  ("vindicated when Glim carries the Hot Breath down") "curdles into something worse" and that he
  is "a true believer who would burn the village to save it." On the page, after being vindicated
  (Ch10 SortKey 100.0, "Surface-tainted"), Grale does nothing worse than remain correctly suspicious
  — he never argues for harming or exiling anyone — and his arc resolves as a clean, selfless
  sacrifice (Ch13 SortKey 400.0) that saves the Bindery operation. This is not merely an unresolved
  thread; it is a different, better-natured arc than the one the bible specifies. See Extra Item (a).
- **MODERATE — Population scale contradicts "a few hundred."** Bible §3: "Now there are perhaps a
  few hundred across Homewater." The prose depicts exactly one community, gives it an explicit
  census of 43 (Ch08 SortKey 100.0: "forty-three people were a Hollow"), and every later
  count/rebuild figure (38-42) treats that single group as the entirety of what's at stake — there
  is no second Hollow, no reference to other Underclan settlements anywhere in Ch01-14. The live
  prose is a village of ~40, not a "few hundred"-strong people. See Extra Item (b).

No prose fact was found that flatly contradicts a bible statement not already covered above (e.g.
job vocabulary, the Amish/Rumspringa comparison — not used in Glim's POV, consistent with bible
§4's restriction — and the Cogs/Engine Guild characterization all check out against §3-4).

---

## Extra Items (a)–(c)

**(a) Grale's "curdle" arc — adjudicated: it dangles, and the resolution that exists actively
contradicts the plan.** Everything the bible calls for through the setup is present and well
executed: the lifelong suspicion (Ch01 SortKey 200.0), the "surface-stained" prediction (Ch01
SortKey 300.0; Ch05 SortKey 100.0), the vindication (Ch10 SortKey 100.0: "Surface-tainted"). But
the bible's specified next beat — his rightness "curdling into something worse," making him "a
true believer who would burn the village to save it" — never appears anywhere on the page. No
beat shows Grale advocating harm, exile, or sacrifice of Glim, Noor, or anyone else. Instead his
arc is quietly redirected into heroism: he draws the Lamplighter patrol away from the Bindery
operation and drowns for it (Ch13 SortKey 400.0, beat 4818). This is a complete, satisfying arc in
its own right — but it is not the arc the bible documents, and nothing in Ch01-13 sets up the pivot
from "vindicated rival" to "silent hero" (there's no scene of him softening toward Glim before the
sacrifice; the pivot is enacted, not earned on the page). *Fix:* update the bible's §4/§8 Grale
entries to describe the sacrifice arc that was actually written (the simplest fix, since the
written arc is good), or add one beat before Ch13 SortKey 400.0 where Grale's certainty visibly
curdles (e.g., he pushes to leave Glim or Noor behind, or to seal a passage with people still in
it) before the sacrifice recontextualizes it.

**(b) Hollow population mismatch — adjudicated: real and unresolved, MODERATE.** Every population
figure quoted in the prose, in order: Ch08 SortKey 100.0 (beat 4797) — *"down below, forty-three
people were a Hollow, and a Hollow was a community of significant size"* (43); Ch10 SortKey 200.0
(beat 4805) — *"This was the Hollow. Forty-two people in the Hollow, and the white arrived at all
of them at once"* (42, at the instant the first raid begins, before Leaf's death is narrated in the
same beat); Ch11 SortKey 300.0 (beat 4810) — *"confirming forty-two lives at a time"* (the fleeing
chain, 41 Underclan + Noor); Ch11 SortKey 400.0 (beat 4811) — *"a gap where forty-one had been
forty"* (Sorrel's exit drops Underclan count 41→40); Ch12 SortKey 100.0 (beat 4812) — *"Forty-one
people breathing"* / *"all forty-one people around him"* (40 Underclan + Noor, consistent with the
above); Ch12 SortKey 300.0 (beat 4814) — *"forty-two people in the ghost-country"* (contradicts the
same chapter's "forty-one" — see §5 BLOCKER); Ch14 SortKey 100.0 (beat 4823) — *"Sixteen of the
forty-two people had lost goods"* ... *"He had started with forty-two ... Thirty-eight ... Plus
Noor. Thirty-nine"*; Ch14 SortKey 400.0 (beat 4826) — *"thirty-eight people were doing the work of
being alive"* in Homewater (Noor now separately "learning the routes," not folded into the count).
**These numbers are internally consistent with each other from Ch10 onward (using 42, not 43, as
the working baseline) except for the Ch12 "forty-two" slip and the fact that Ch14's death ledger
names only Leaf, Grale, an unnamed "third," and Vesh as losses — four names, yet the math implies
five losses are needed (Leaf, Slip, Grale, Sorrel, Vesh) to go from a 43-person Ch08 baseline down
to 38.** Slip (whose on-page death and funeral, Ch11 SortKey 300.0, is one of the most fully
dramatized losses in the book) is never mentioned in the Ch14 reckoning at all. Separately and more
fundamentally: none of this reconciles with the *bible's* "a few hundred across Homewater" — the
prose never depicts more than this single ~40-person group, with no other Hollow ever named.
*Fix:* pick a single baseline (recommend 43, matching Ch08) and hand-check the arithmetic name by
name through Ch10-14; separately, either revise the bible's "few hundred" to match the story that
was actually written (a single ~40-person clan), or add a line clarifying Glim's Hollow is one of
several within a larger Homewater population.

**(c) Round-of-addition threads:**
- **Maze plants (Ch02 SortKey 350.0 fragment; claimed Ch09 route-slip) → Ch11 SortKey 300.0
  failure.** The Ch02 plant (Marl's fragment/story) pays off directly and effectively — Ch11's "The
  wall was where the passage should have been" and the interior monologue quoting Vesh's own words
  back ("*He came to a wall where he said the passage had been...*") is a clean, well-executed
  Chekhov's gun. The claimed "route-slip in Ch09," however, is not literally about the underground
  maze: the only comparable moment in Ch09 is Glim misjudging a handhold while climbing down the
  *outside of Noor's surface building* (SortKey 100.0, beat 4801 — "the hold was a hand's width
  lower than the memory said"). This is a legitimate thematic echo (his perfect memory can still be
  wrong) but a different object/setting, not a direct plant of the specific alternate route he uses
  in Ch11. That specific route ("the second branch he'd marked... on that same solitary descent")
  is never planted anywhere before Ch11 SortKey 300.0 — it is introduced in the same beat that pays
  it off. **Verdict: the Ch02 plant→Ch11 payoff is confirmed and strong; the Ch09 plant is at best
  indirect, and the actual mechanism that saves the clan in Ch11 is unplanted.**
- **"Leaf" named in Ch01 → dies in Ch10 — confirmed, same character, no contradiction.** Ch01
  SortKey 100.0 establishes Leaf as the elder who tends "the chemical-heat stone." Ch10 SortKey
  200.0 depicts a death at "the chemical-heat stone, near where Leaf worked every morning of his
  life" — elliptical (never states "Leaf" was the one seized; relies on location + Sallow's
  "closing an account" tone), but Ch14 SortKey 100.0 explicitly confirms: "Leaf, in the Hollow,
  when the flood-lights first hit." Same character, death depicted then confirmed, no
  contradiction — a deliberate (if slightly risky) use of restraint rather than an error.
- **Noor's scar/cowlick (Ch07 SortKey 250.0) → reunion payoff — confirmed, same chapter.** The
  identifying detail and its payoff are both in Ch07 (250.0 sets it up via Odalys's call; 300.0 is
  the doorway reunion where Noor "understood the eyebrow and the crown of his head" on sight). No
  gap, no contradiction.
- **Sorrel's threshold refusal (Ch11 SortKey 400.0) → count in Ch12.** The refusal *does* correctly
  drive the headcount in Ch12 SortKey 100.0 ("forty-one," down from Ch11's "forty-two") — the
  numbers check out for that one beat. But as detailed in §5/(b) above, this correct arithmetic is
  immediately undercut twice: once by the in-chapter "forty-two" slip (Ch12 SortKey 300.0) and once
  by Sorrel's own literal reappearance as a living, present character two beats later (Ch12 SortKey
  200.0) and again in Ch13. **Verdict: the count itself briefly checks out; the character it's
  counting does not stay counted-out.**

---

## Summary of severities

- **BLOCKER (3):** Sorrel alive-and-present (Ch12/13) vs. lost-at-the-seam (Ch11/14); the in-chapter
  "forty-one" vs "forty-two" arithmetic slip in Ch12; the unresolved Ch08-vs-Ch10-onward population
  baseline (43 vs 42) compounded by Ch14's death ledger omitting Slip.
- **MODERATE (4):** Grale's bible-specified "curdle" never occurs (resolves as heroism instead);
  Hollow/Homewater population contradicts the bible's "a few hundred"; Candle-Two's promotion is
  unplanted; the Ch11 maze-escape route is invented in the same beat it resolves the crisis.
- **MINOR (6):** "three sleep-cycles early" vs. "the next sleep" wording mismatch (Ch04); Vesh's
  "seventy years" vs. "sixty-three years" of service; Leaf's death relies entirely on inference
  (fine if intentional); the shine's "four years... and sixteen since" time-anchor is ambiguous;
  the finale's "full Fare" of three objects is unplanted against the bible's single-object rule;
  Sorrel's palm scar is planted twice and never paid off.
