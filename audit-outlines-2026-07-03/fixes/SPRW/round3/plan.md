# SPRW Round 3 — Structural Consolidation (Beat 7 seam + Movement III middle)

Slug: `the-number-that-works-019ed367`. Panel score 85.5, flagship. **Files only — no DB writes.**
Full 58-beat text pulled read-only from `Beats`/`NodeBeats` (SortKey order) on 2026-07-03. Prior
rounds (`../plan.md` = round 1, `../round2/pos*.txt` = round 2) both attempted sentence-level
smoothing on beat 7 (4909) and left it at 2.9. This round diagnoses why smoothing couldn't work and
proposes structural fixes instead.

---

## Part 1 — Beat 7 (the 2.9 dip)

### Diagnosis

Position 7 (SortKey 325.0, `Beats.Number` 4909) sits between position 6 (SortKey 300.0, `Number`
4094 — "I wrote the third one," a strong reveal) and position 8 (SortKey 337.5, `Number` 4969 —
"He tries the front door," a strong escalation). Read as a run, beat 7 has three concrete, provable
defects that no amount of sentence-smoothing fixes because they are structural, not lexical:

1. **It contradicts the beat immediately before it.** Beat 6 (4094) ends with Elias *already having
   gotten past* the registry's query limit — "The query returns: '3 results. Research query limit
   exceeded — continue?' He continues." — and then fully analyzes all three results (masses,
   disposition text, the fact that he wrote the 2213 closure himself). Beat 7 opens: "Three results,
   and none of them accessible without credentials he didn't have, and the query limit had closed
   the door." This is the same three results, now described as inaccessible one beat after he
   accessed them. A reader who just watched him get through the wall in beat 6 hits a wall that
   wasn't there thirty seconds of reading earlier. **Round 2's rewrite (`round2/pos7.txt`) kept this
   exact contradiction verbatim** — it re-smoothed the sentences around the same false premise
   instead of removing the premise.
2. **It breaks the story's verb tense.** Every beat in Movement I (4089–4100) narrates in present
   tense: "Work Order 09-Milwaukee *arrives*," "He *runs* the orbital designation," "He *tries* the
   front door" (beat 8 itself). Beat 7 is written entirely in past tense — "He *looked* at the
   message," "He *stood* up," "The kettle *ran*." It reads as a page dropped in from a different
   draft, because it is one: this is very likely the actual mechanism behind "sentence-smoothing
   failed twice" — a reviewer smoothing individual sentences for voice will not catch a whole-beat
   tense mismatch, because every sentence is internally consistent; it's only wrong against its
   neighbors.
3. **It has no independent dramatic function.** Stripped of the two defects above, what's left is:
   he makes tea, notices the case sitting there, and can't decide who to put in the invoice's
   "client contact, second method" field. That's one beat of content (the isolation implied by an
   unfillable second-contact field) wearing the costume of a full scene-break — kettle, window-light
   change, Pulse-frequency tracking — that's already been used for the same "sits with it" transition
   at the end of beat 6 (crosses to the window and back, "the chair's still warm").

### Verdict: MERGE (not rewrite a third time)

Fold beat 7's one genuinely new idea — the unfillable "second contact" field, i.e. Elias has no one
else to loop in — into the **opening** of beat 8 (4969, "He tries the front door"), in present tense,
with the false "inaccessible" framing removed. This gives beat 8 its needed beat of time-passage
(discovery → evening → escalation) without re-litigating a wall he already got past. Disable 4909.

See `beats/beat7_merge.md` for the full merged text.

---

## Part 2 — Middle consolidation (Movement III)

### Function map (one line each, SortKey order, seq = reading position)

| Seq | SortKey | Number | Title | Function |
|---|---|---|---|---|
| 23 | 1050 | 4198 | The First Word | First transmission: selection dossier on Elias himself; answers *when*, not *why*; she opens with him, not data. |
| 24 | 1100 | 4199 | Why Him | 8-of-8 criteria checklist; he's contact #9 of 9, the only one who went; deflates "chosen" to "qualifying." |
| 25 | 1150 | 4200 | Wrong Questions | "Are you safe?" returns orbital telemetry; establishes the translation problem in the abstract (his question needs reframing into her terms). |
| 26 | 1200 | 4201 | Thirty-Seven Years | Catalog scale on the *time* axis: 847 events, no observer fatigue, 22 jobs logged before him. |
| 27 | 1250 | 4202 | The Map | Catalog scale on the *space* axis: six global clusters, correlation 0.91; plants the unnamed word he won't write. |
| 28 | 1300 | 4203 | Street Level | Embodied instance: he visits Zone 4 in person; the pavement feels wrong to his body, not his phone. |
| 29 | 1350 | 4204 | Below Resolution | Explains *why* she can't see what he felt: 4m floor vs. 0.5mm haptic sensitivity, factor of ~8,000. |
| 30 | 1400 | 4205 | She Asks | **Structural pivot**: her first-ever question to him. She needs data she can't collect and has to ask a person for it. |
| 31 | 1450 | 4206 | The Vocabulary Problem | Same exchange, one beat later: "quality"/"wrong" have no parameter mapping. **Redundant with 30 — see below.** |
| 32 | 1500 | 4207 | What She Knows | Isotopic match, lake site ↔ 35th-and-Halsted (Attendance); she's tracked both 14 years, never said so — no job 23 to say it to. |
| 33 | 1550 | 4208 | What He Knows | Reciprocal: he supplies the human interior of Attendance (the children came back calm); she updates her model. |
| 34 | 1600 | 4209 | Inference | New epistemic frame: "I do not know" (gap in coverage) vs. "my data cannot answer that" (outside capacity) — she audits and corrects her own past conflation. |
| 35 | 1650 | 4210 | Who Built It | Applies the frame to a real plot question — origin is unknowable, older than her own signature — new mystery data, not a repeat of 34. |
| 36 | 1700 | 4211 | Her Limits | The full, deliberate blind-spot catalog (4m, no acoustic, 12m subsurface, no contact, orbital window) — delivers three facts not stated before; framed explicitly as him building "a map of what she could see," distinct from 29's single reactive instance. |
| 37 | 1750 | 4212 | His Limits | Reciprocal: his own limit (3-week memory horizon); she has a model for data loss, none for "losing context you once held." |
| 38 | 1800 | 4213 | Not a Job | Reciprocity shift: six coordinates, no rate, no confirmation field — an ask, not an order. |
| 39 | 1850 | 4214 | Field Notes | He follows through on all six sites; three-vignette texture (freight district, grain corridor, middle Ring), new physical detail per site. |
| 40 | 1900 | 4215 | The Most Information | Validation-of-contribution beat: "the most information I have received... in 37 years" / "is that a thank you" / "I stated a fact." |
| 41 | 1950 | 4216 | The Full Catalog | Scale + humility: all 847 entries; even her mass figures are ranges, not exact. |
| 42 | 2000 | 4217 | 2189 | Origin-of-method mystery: unlabeled columns — "the header was supposed to come later." Sets up 43. |
| 43 | 2050 | 4218 | Delta-Actual | **LOCKED landmark.** They co-name the prediction-error column. |
| 44 | 2100 | 4219 | Seen | Validation-of-suffering beat: she reports his own filing history back to him (1,140 docs, 44 days indoors, the rising post-Mombasa curve). |
| 45 | 2150 | 4220 | The Elephant | **LOCKED landmark.** Elephant parable returned as "a bandwidth problem, not a blindness problem." |
| 46 | 2200 | 4221 | How She Sees | **LOCKED landmark** (task's "Seen/heat signature"). He finds his own heat signature, unremarked, in her mass-distribution layer. |
| 47 | 2250 | 4222 | How He Hears | Reciprocal: he sends raw ambient audio with no schema; "this is new data." |
| 48 | 2300 | 4223 | The Source | Depth confirmed (300m, ~400 years pre-city); source is not itself a schism — schisms cluster around it. |
| 49 | 2350 | 4224 | Not Purposeful | Mechanism hypothesis: unintentional sedimentation, "a drain running the wrong direction"; he withholds the Attendance children — no shared column for it. |
| 50 | 2400 | 4225 | The Record | Collaborative-document beat: she calls the lake "featureless," he corrects it, she appends an observer-relative caveat verbatim — the elephant parable enacted rather than stated. |
| 51 | 2450 | 4226 | For Whoever Comes After | **LOCKED landmark.** Her first unqualified "yes." |
| 52 | 2500 | 4227 | His Transit Pattern | Reports his behavioral delta back to him (factors, not just counts); leads into the unanswered "what you do or what you are." |
| 53 | 2550 | 4228 | The Eleven Days | **LOCKED landmark.** |
| 54 | 2600 | 4229 | New Work Order | **LOCKED landmark.** RATE: OPEN. |
| 55 | 2650 | 4230 | He Files It | He processes the filing; thinks of Siosaia and Tadesse; leaves the tab open. |
| 56 | 2675 | 4908 | (untitled) | Connective tissue to "Downstairs" — but re-narrates Tadesse's exhale and the 4227 behavioral-delta data almost verbatim, then delivers one new GAD-arc beat (doesn't reach for the pill case) buried in ~1,000 words of restatement. **Redundant padding — see below.** |
| 57 | 2700 | 4231 | Downstairs | He walks six blocks for the first time in the story's present. |
| 58 | 2750 | 4232 | The Window | **LOCKED landmark — the ending.** Types 400, clears it, leaves RATE: OPEN standing, opens the first coordinate. |

### Redundancy findings (consecutive-pair scan, all 35 adjacent pairs checked)

Scanning every consecutive pair in seq 23–58 for beats that repeat the same dramatic function, only
two runs are **provably** redundant — not merely similar in surface form (the exchange-format
repetition itself is bible rule 7, LOCKED, "mathematical floor of the form," and is not touched
here):

**1. Seq 30–31 (4205 "She Asks" / 4206 "The Vocabulary Problem") — MERGE.**
These are the same conversation told twice. 4206 opens mid-exchange with Elias apparently back at
the physical site ("sat down on the low concrete barrier at the edge of the site") when 4204 and
4205 both explicitly place him on the balcony at home — an unstated location jump. There is also a
tense-boundary problem: the text runs present-tense frame narration from beat 1 through seq 30
(4205), then pivots cleanly to past-tense frame narration for the rest of the piece starting at seq
32 (4207, "What She Knows") through the end — plausibly an intentional register shift (procedural
urgency giving way to settled retrospect once the Attendance/isotopic thread deepens the
relationship). 4206, at seq 31, jumps into that past-tense register one beat before the apparent
pivot point, landing the tense change mid-exchange instead of at the exchange boundary. Content-wise,
4206's core beat — he tries to describe the felt wrongness in words, she replies "no parameter
mapping available" — is the same beat 4205 already ran (he offers "forgotten what it's supporting,"
she asks him to clarify in her terms). Two consecutive beats spend the vocabulary-gap idea; one beat
can carry it with both of 4206's better images ("furniture moved in the night," "they overlap. they
do not coincide.") folded into 4205's ending, in present tense. This has the side benefit of moving
the present→past pivot to the clean seq-30/seq-32 boundary instead of splitting it mid-exchange. This
is exactly the "another blind-spot demonstration already demonstrated" pattern the audit flagged.
Disable 4206; merge into 4205.

**2. Seq 55–56 (4230 "He Files It" / 4908 untitled) — MERGE.**
4908 spends roughly 1,000 words re-stating material already on the page: Tadesse's exhale is quoted
almost identically to 4230's own closing paragraph ("Tadesse in a facility on the East African coast
who had exhaled twenty-two years of held breath at the word yes"); the field-visit-frequency data was
already fully delivered in 4227; the RATE: OPEN / SITE: ALL / QUANTITY: ONGOING triplet is restated
in full for the third time (4229, 4230, 4908). The one load-bearing thing 4908 does that nothing else
does — he stands at the window, doesn't reach for the pill case, puts on the coat, decides the number
needs the walk first — is a real GAD-arc beat (bible §2) and must survive, compressed, as the lead-in
to 4231 "Downstairs." Disable 4908; merge its unique tail into the head of 4231.

### Pairs considered and rejected as false positives

Several seq pairs look repetitive at a glance but are load-bearing, deliberate mirrors central to the
two-minds conceit, not blur — cutting any of these would damage the LOCKED form, not tighten it:

- **32–33 (What She Knows / What He Knows)** — a matched diptych (her data / his witness) that is
  the mechanism by which the Attendance correlation gets built; each side supplies what the other
  structurally cannot.
- **36–37 (Her Limits / His Limits)** — the explicit "accounting" pair; 37's text says outright he's
  reciprocating because she disclosed first. Removing either breaks the reciprocity beat itself.
- **46–47 (How She Sees / How He Hears)** — same mirrored-disclosure pattern, sensory register
  instead of data register.
- **23–24 (First Word / Why Him)** — two different questions (when vs. why) 48 hours apart with two
  different emotional landings (wonder → deflation); collapsing them loses the escalation.
- **34–35 (Inference / Who Built It)** — 35 isn't a repeat of 34's epistemic frame, it's the frame's
  first real-world application, and it delivers new mystery information (the source predates her own
  signature by an unbounded margin).
- **41–42–43 (Full Catalog / 2189 / Delta-Actual)** — a three-beat build to a locked landmark; each
  beat supplies a fact the next one needs.

### Net count

58 → **55 enabled beats** (3 disabled: 4909, 4206, 4908). This is short of the 4–8 target named in
the brief. Given the "every cut must be provably redundant" standard for the flagship, and the
explicit caution in project memory against non-monotonic lever passes over a strand that already
scores 85.5, I did not manufacture additional cuts to hit a number. The panel's "blur/momentum sags"
complaint is real for the two pairs identified above; for the rest of Movement III, what reads as
sameness on a fast pass is, on a beat-by-beat function check, the deliberate mirrored-exchange
architecture the bible locks in rule 7. If the panel re-flags this after the two cuts land, the next
lever to pull is trimming individual paragraph lengths inside the diptych beats (32/33, 36/37, 46/47)
rather than removing another beat outright — flagging this as the fallback, not doing it now.

---

## Part 3 — File manifest

| File | Action | Target(s) | Result |
|---|---|---|---|
| `beats/beat7_merge.md` | MERGE, disable 4909 | 4909 → 4969 | Beat 8 (4969) gains a present-tense evening-transition opening; the false "inaccessible" claim and the tense break are removed. |
| `beats/vocab_merge.md` | MERGE, disable 4206 | 4206 → 4205 | Beat 30 (4205) absorbs 4206's two strongest images and its closing line; location/tense inconsistency resolved; also fixes an unrelated small error (4205 misattributed Sparrow's 37-year observation span to Elias's own career length — corrected to match his canon "eleven years... to the gram" job description). |
| `beats/tab_merge.md` | MERGE, disable 4908 | 4908 → 4231 | Beat 57 (4231) gains a compressed lead-in (~150 words replacing ~1,000) that keeps the one load-bearing GAD beat (doesn't reach for the pill case) and cuts the Tadesse/behavioral-delta restatement. |

All three merges preserve every item on the untouchable-landmarks list: Delta-actual (43), Seen /
heat signature (46), the elephant-bandwidth return (45), "for whoever comes after" + first
unqualified yes (51), the eleven days (53), RATE: OPEN (54), and the ending (58) — none of the merged
or disabled beats fall within two positions of any of these.
