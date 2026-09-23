# RFC 0014 — Instrument Value Audit: prove them, or shut them off

**Status:** in progress · opened 2026-09-22
**Predecessors:** [RFC 0009](0009-no-autonomous-prose-writes.md) (no autonomous prose writes),
[RFC 0010](0010-full-battery-value-audit.md) (the first value audit, which deleted 27 of 32 checks),
[RFC 0013](0013-narrative-obligation-ledger.md) (the only instrument with a real calibration harness)

---

## 1. Why this exists

On 2026-09-22 `prose --export-node --slug bushido-coda` failed four of six publish-readiness
checks and shipped only under `--force-export`. The author's response was that the utilities
behind those checks are not trusted: they burn tokens, may not do what they claim, and may insert
problems rather than find them. This RFC is the evidence needed to decide each one's fate.

Three things make that suspicion well-founded before a single new measurement is taken.

### 1.1 The findings table is a landfill, not a backlog

Measured 2026-09-22, corpus-wide, all time (`prose --findings stats`):

| Status | Count |
|---|---|
| New | 15,822 |
| Triaged | 1 |
| **Applied** | **8** |
| Dismissed | 14,914 |
| **Total** | **30,745** |

**8 findings out of 30,745 have ever been acted on — 0.026%.** RFC 0010 measured the same ratio
at ~26,000 findings and 8 applied in September; in the two-and-a-half weeks since, the corpus has
produced ~4,700 more findings and **not one** of them has been applied.

### 1.2 Which instruments earned those 8

`prose --findings stats --by-instrument` (added by this RFC; the rollup did not exist before, so
nobody could answer this question without reading the table by hand):

| Instrument | Total | New | Applied | Dismissed | Apply rate |
|---|---:|---:|---:|---:|---:|
| **LEDGER-CONFLICT** | 69 | 42 | **5** | 22 | **7.246%** |
| **LOGICSWEEP** | 74 | 66 | **2** | 6 | **2.703%** |
| **LINT** | 4,597 | 3,207 | **1** | 1,389 | 0.022% |
| `[outline-entity` | 5,271 | 558 | 0 | 4,713 | 0 |
| READABILITY | 4,840 | 4,840 | 0 | 0 | 0 |
| LOGICSWEEP-BLAST | 4,359 | 2 | 0 | 4,356 | 0 |
| CHECKLIST | 3,052 | 3,052 | 0 | 0 | 0 |
| LIBERTY-CONSIDER | 2,143 | 0 | 0 | 2,143 | 0 |
| CANON-ADDITION-CANDIDATE | 1,275 | 0 | 0 | 1,275 | 0 |
| SEMANTIC-DRIFT | 1,055 | 1,032 | 0 | 23 | 0 |
| CONTINUITY-VIOLATION | 528 | 528 | 0 | 0 | 0 |
| SANITY | 371 | 371 | 0 | 0 | 0 |
| COMPREHENSION | 237 | 222 | 0 | 15 | 0 |
| OBLIGATION | 140 | 140 | 0 | 0 | 0 |
| FACT-LEDGER | 129 | 69 | 0 | 60 | 0 |
| …30 more producers | | | **0** | | 0 |

Three instruments have ever produced an applied finding. Everything else — 30,000+ rows across
~40 producers — has produced none, ever.

Two corrections to the record while we are here. RFC 0010 attributed 5 applied findings to
"fact-ledger"; the producer is actually **LEDGER-CONFLICT** (the Story Ledger's
same-predicate/different-object detector). The summary prefix `FACT-LEDGER ` belongs to
`BookHealthService.FactLedgerAsync`, which has filed 129 findings and had **none** applied. They
are different instruments and the distinction matters, because LEDGER-CONFLICT is the single most
valuable thing in the tree by this measure and should not have its record credited elsewhere.

What LEDGER-CONFLICT actually caught, for the avoidance of doubt that these numbers are abstract:
`[Kyle Ellen Corbin] residence_duration: "eleven years" vs "four years"`. That is the eleven-year
spine of *Bushido Coda*. It is a real, load-bearing defect, found by a machine, fixed by the
author. This instrument works.

### 1.3 The instruments that gate publication are mostly not the instruments that work

| Gate check | Instrument | Applied findings, all time |
|---|---|---|
| 1. logic-sweep BLOCKER/MODERATE | LOGICSWEEP | 2 |
| 2. story-ledger CONTRADICTED | LEDGER-CONFLICT + FACT-LEDGER + TUNEDREAD | 5 (all LEDGER-CONFLICT) |
| 3. two dry sweep rounds | LOGICSWEEP | — |
| 4. blast-radius recheck | LOGICSWEEP-BLAST | **0**, from 4,359 filed |
| 5. Reader-Proxy QA | COMPREHENSION / GRIPE / ENGAGEMENT | **0**, from 237 filed |
| 6. obligation ledger | OBLIGATION | **0**, from 140 filed, and 96% FP measured (RFC 0013 §8) |

Half the gate is made of instruments with no demonstrated value. The blast-radius recheck is the
starkest: it fires automatically on **every beat save**, six LLM dimensions per fire, with no cost
scope and no user-visible metering — continuous unmetered spend that has produced 4,359 findings
and zero applied ones.

---

## 2. Defects found and fixed while gathering this

These were not the point of the exercise; they were found on the way and fixed immediately.

### 2.1 Applying a reader-engagement finding would have pasted the instruction into the novel
*(commit `e755376e1`)*

`FindingApplyService` applies a finding by literal substitution:
`beat.Text.Replace(Snippet, SuggestedFix)`. That contract only holds when `SuggestedFix` is prose
to put in the manuscript.

`GripePassService.RunFullOrderReadAsync` filed its ENGAGEMENT findings with a quote-grounded
`Snippet` (real prose, so the `Contains` guard passes by construction), a `FilePath` of
`node:{slug}#fullorderread/beat:{guid}` (contains `beat:`, so `ExtractBeatId` routes it to the
beat path), and a static instructional `SuggestedFix`: *"Fix structurally, not stylistically: give
this beat more page-time…"*.

Every precondition was satisfied. One `prose --findings apply <id>` would have replaced a
paragraph of the book with that sentence and committed it as `BeatWriteReason.FindingApply`. It
is the only finding type in the tree that populates both fields; the other instruments
structurally cannot reach the apply path. Found before it fired — there were no live ENGAGEMENT
findings on BCODA.

Fixed in two places: the producer no longer sets `suggestedFix` (the guidance moved to the
`Summary`, the field a human reads), and `FindingApplyService.IsInstructionalFix` is the backstop
for the same shape from any future instrument.

**This is the direct answer to "do they insert problems?" — one of them was one keystroke away
from doing exactly that.**

### 2.2 The gate called a book clean on instruments that had never read it
*(commit `36cda43d8`)*

Three of six checks were plain `Findings` counts: read zero, print "clean". Nothing recorded
whether the instrument had ever looked. BCODA's report contained both of these, three lines
apart, about the same sweep:

```
✅ logic-sweep BLOCKER/MODERATE = 0 — clean
❌ 2 consecutive dry sweep rounds — not converged
```

Checks 2 and 6 already refused to pass silently, each having invented its own private evidence
(`ContinuityClaims.Any(BookSlug)`, `Beat.ObligationScanHash`). That idea is now generalized:

- **`InstrumentRuns`** — append-only record of every instrument run against a book: what it read,
  out of how much, against which book fingerprint, and how many findings it filed.
- **`CheckOutcome { Pass, Fail, CouldNotLook }`**. CouldNotLook blocks like a failure but reads
  differently, because the remedy is to run the thing, not to fix the prose.
- **`InstrumentRunLedger.Evaluate`** — a pure function, so the decision is testable without a
  database. Open findings prove the instrument looked; absent those, never-ran / read-nothing /
  read-only-part / read-prose-that-has-since-changed all outrank the finding count.

After the fix, the same command reports:

```
❓ logic-sweep BLOCKER/MODERATE = 0 — COULD NOT LOOK — the logic sweep has never run on this book
❌ story-ledger CONTRADICTED = 0 — the fact ledger has never been populated for this book
❌ 2 consecutive dry sweep rounds — not converged
❌ blast-radius recheck clean — 3 open finding(s) from the blast-radius recheck
❓ Reader-Proxy QA High/BLOCKER = 0 — COULD NOT LOOK — Reader-Proxy QA has never run on this book
❌ obligation ledger balanced — COULD NOT LOOK — ledger empty (scan coverage 0/521)
```

Two of the six results that were previously green were never measurements at all.

### 2.3 A "full-book logic sweep" of BCODA reads under a tenth of the book
*(same commit)*

`LogicSweepService` reported beats *handed to* the rules, not beats the model *saw*.
`BuildClampedProse` truncates at 100,000 characters; BCODA is ~193,000 words, over a million
characters. So a full-book sweep of it shows the model under 10% of the manuscript — and both the
report and the gate recorded that as a clean sweep of the whole book.

The run ledger now records `VisibleBeatCount`, and a partial read is **void**, not a pass. This is
the same failure the obligation ledger took six calibration runs to notice about itself
(RFC 0013 §8, run 6: 55 of 96 beats read, precision and recall reported as if over the whole book).

### 2.4 The gate was documented as seven checks, coded as six, advertised as five
*(commit `6ebd5e66f`)*

`docs/LOGIC.md` §9 said "ALL SEVEN". `PublishReadinessAsync` computes six. The MCP tool
description and three CLI docstrings said "five-point" and enumerated five, omitting the
obligation check that does run and does block.

The seventh — "zero unentailed entity-record claims" — was never implemented. What stood in the
code was a doc comment describing it with no method body underneath; nothing ever called
`EntityRecordGroundingService`. It is deliberately **still** not wired in: entity-record grounding
has no calibration behind it, and adding unmeasured conditions to a gate is how this gate got here.

### 2.5 Nothing recorded that an export bypassed the gate
*(same commit)*

`--force-export` warned to stderr and vanished; the MCP path with `forceExport:true` returned a
plain `ok:true` with no indication the gate had been bypassed at all. Nothing in the database knew
a manuscript had shipped past a failing gate, or which checks were unmet at the time. Both paths
now write a `publish-gate-bypass` entry to the decision ledger, and the MCP response carries a
`gate_bypassed` array.

---

## 3. Standing rules for this audit

**Verdicts.** Three outcomes, and the distinction between the last two matters:

- **PROVEN** — passes its bar. Stays; the gate may rely on it.
- **FAILED** — measurably below bar. **Commented out in place**, with a header giving the date,
  the measured number and the section of this RFC; removed from the gate and from any default
  battery; left in the tree for later review. Not deleted.
- **UNPROVEN** — not yet measured. **Left running**, but marked here and removed from the *gate*,
  because a gate condition nobody has measured is exactly the false green §2.2 was about.

**Budget.** Real-LLM spend for this audit is capped under **$50**, cheapest-first: negative
control on the existing Gutenberg fixtures, then self-agreement, then seeded-defect recall. The
census names ~90 files with LLM call sites; $50 does not calibrate ninety instruments. It buys the
ladder for the handful that gate publication. Everything else lands as UNPROVEN with a costed
estimate, and that is the author's call to spend into, not something to spend unprompted.

**Method.** Per the four-test doctrine already established in this repo: an instrument's findings
are worthless until the instrument itself has been measured, and a harness that does not reset
state between runs is not measuring anything (RFC 0013 §8 — four runs, ~$14, zero valid
measurements).

---

## 4. Verdicts

| Instrument | Status | Evidence |
|---|---|---|
| LEDGER-CONFLICT (Story Ledger, same-predicate) | **PROVEN** | 5 of the corpus's 8 applied findings; 7.2% apply rate; caught the eleven-year spine conflict |
| LOGICSWEEP (full-book) | **PROVEN, with a caveat** | 2 applied findings; but see §2.3 — on a book past the clamp it reads under a tenth of the manuscript, so its coverage claim was false even where its findings were good |
| OBLIGATION extractor | **FAILED** | RFC 0013 §8: 96% false positives on GCOBN, 20 beats engineered to owe nothing produced 25 obligations; precision 0.037 on the one valid calibration run; 140 findings, 0 applied |
| LOGICSWEEP-BLAST (blast-radius) | **UNPROVEN** | 4,359 filed, 0 applied, no calibration; runs automatically on every beat save with no cost scope |
| COMPREHENSION probes | **UNPROVEN** | 237 filed, 0 applied, no calibration — and it gates publication |
| GRIPE / ENGAGEMENT | **UNPROVEN** | no calibration; ENGAGEMENT additionally carried the §2.1 defect |
| FACT-LEDGER (`FactLedgerAsync`) | **UNPROVEN** | 129 filed, 0 applied — distinct from LEDGER-CONFLICT, and not credited with its record |
| TUNEDREAD | **UNPROVEN** | not separately measured |
| ~30 other producers | **UNPROVEN** | 0 applied findings between them, all time |

*Every FAILED and UNPROVEN row above was deactivated on 2026-09-22 — see §4.1.*

---

## 4.1 Author ruling, 2026-09-22: deactivate, don't calibrate

The §3 ladder was overtaken by the author's decision partway through gathering the above:

> *"just comment out all of these they've never proven their worth … tired of it burning $400 and
> fixing nothing … when I need them I will uncomment them and fix them."*

So the <$50 calibration budget is moot for now, and UNPROVEN was collapsed into FAILED-by-default:
an instrument that has filed thousands of findings over months without one being applied does not
get the benefit of the doubt, it gets switched off until someone wants it enough to prove it.

**What was switched off** (all of it commented in place; nothing deleted, every handler intact):

| Surface | What changed |
|---|---|
| Per-beat-save tails | The four LLM tails in `NodeWorkbenchService.UpdateBeatTextAsync` — semantic-fidelity drift, blast-radius narrow sweep, continuity re-extraction, obligation scan. ~9 LLM calls on **every beat save**, unasked, unmetered, no cost scope. |
| Unattended daily sweeps | `ContinuityLongSweepService` (daily LLM continuity re-scan) and `SanityScanBackgroundService` (daily, free, 5,211 findings) unregistered in `AddProseBackgroundServices`. |
| The publish gate | The `PublishReadinessAsync` pre-flight in `ExportNodeCli` **and** `Tools.Nodes.ExportNodeImpl`. Export no longer blocks; `--force-export` / `forceExport:true` are accepted no-ops. |
| 36 CLI commands | Listed in `src/Prose.Cli/DeactivatedInstruments.cs`, refused by one gate at the top of the dispatch chain. `docs/CLI_COMMANDS.md` is generated from that same list, so the reference cannot drift from the gate (§2.4 is exactly that failure). |
| 25 MCP tools | `[McpServerTool]` commented out on each; two whole classes (`Tools.BeatLens`, `Tools.ReaderQa`) unadvertised at `[McpServerToolType]`. The methods still compile and still work if re-advertised. |

**What stays live, and why:**

- **`--logic-sweep`, `--ledger-adjudicate`, `--lint-prose`** — the only three producers that have
  ever had a finding applied (2, 5 and 1 of the corpus's 8).
- **`--findings`** (including `stats --by-instrument`) — the measurement, not the instrument.
- **`--publish-readiness`** — still computes the six-check report on demand. It just no longer
  decides whether a book may ship.
- **`--calibrate-obligations` / `--inject-calibration-defects`** — the harness. Deactivating the
  way back in would make this permanent, which is not what was decided.
- **The mojibake guard and the BLOCKER verification gate on export** — deterministic, and they
  block on things that are unambiguously wrong.

**How to restore any of it:** delete the flag's line from `DeactivatedInstruments.cs`, or uncomment
the named block. Each commented block carries the date, this RFC and the measured number.

---

## 5. Work remaining

1. Generalize `ObligationCalibrationService` into an instrument-agnostic harness — seeded-universe
   guard, byte-exact revert, pure static scorer, `CouldNotLook` → VOID — so logic-sweep,
   continuity and reader-QA can be scored by the same thing instead of each inventing one.
2. `BeginCostScope` on every instrument entry point. No instrument has one today, so "what did
   that run cost" is unanswerable.
3. The does-it-insert-problems proof, generalized from RFC 0009's acceptance test: clone BCODA,
   snapshot `Beat.Version` and `TextHash` for all 521 beats, run every instrument in the census
   against the clone, assert zero version movements and byte-identical text.
4. The paid ladder in §3, in priority order, stopping at $50.
5. Narrow gate check 4 to §9's actual wording — "every fix applied *since the last dry round*" —
   which needs a per-beat check stamp. Currently it counts any open blast finding whenever raised.
