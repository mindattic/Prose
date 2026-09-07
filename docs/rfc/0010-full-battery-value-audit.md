# RFC 0010 — What the Full Battery Actually Brings to the Table

**Status:** research, no code changed · **Requested by author, 2026-09-06:** *"research 3 — what
does full battery even bring to the table"* · **Companion to:** RFC 0009

---

## 1. The question, answered in one table

`prose --audit-book` runs 32 checks across three tiers. The only evidence that a check is worth
anything is a finding the author **applied**. Across the whole corpus, all time, there are **eight**.

| Applied finding | Category | Produced by | Battery check | Tier |
|---|---|---|---|---|
| LEDGER-CONFLICT residence_duration | Contradiction | Story Ledger (`ContinuityService.Upsert`) | `fact-ledger` | DEEP |
| LEDGER-CONFLICT residence_duration (dup row) | Contradiction | Story Ledger | `fact-ledger` | DEEP |
| LEDGER-CONFLICT physical_feature_height (Mira) | Contradiction | Story Ledger | `fact-ledger` | DEEP |
| LEDGER-CONFLICT contract_count | Contradiction | Story Ledger | `fact-ledger` | DEEP |
| LEDGER-CONFLICT contract_count_total | Contradiction | Story Ledger | `fact-ledger` | DEEP |
| LOGICSWEEP outline_agreement (#5221) | Contradiction | Logic Sweep | `logic-sweep` | DEEP |
| LOGICSWEEP outline_agreement (#5220) | Contradiction | Logic Sweep | `logic-sweep` | DEEP |
| LINT ALT-SCENE (#4349 / #5366) | CraftChecklist | RepetitionLint (deterministic) | `lint-prose` | DEEP |

**Three checks out of thirty-two have ever produced a finding you acted on.** All three are DEEP
tier. All three are the instruments this project already names as canonical QA — the Story Ledger
(docs/LEDGER.md), the Logic Sweep (docs/LOGIC.md, SS-A44), and the deterministic linter.

**The FULL tier — the expensive one — has produced zero applied findings, ever.**

## 2. What it costs

`prose --cost --history`, `--audit-book`, 19 runs:

| Runs | Tier (inferred from cost) | Actual per run |
|---|---|---|
| 5 on 08-30 | FREE | **$0.54 – $0.87** |
| 13 on 08-29 → 08-30 | FULL | **$27 → $135** |
| 1 on 08-13/14 | — | $6 – $21 |

Thirteen FULL runs in two days: **≈ $1,100.** Applied findings from them: **0.** The estimator said
$1.50 for every one of them (static fallback), then swung to a $69 blended mean that it now quotes
for a $0.60 FREE run.

The standalone instruments are cheap — `--storyscope-audit` ≈ $1, `--chekhov-audit` ≈ $0.08,
`--examine-emotion` ≈ $0.30, `--tuned-read` ≈ $0.09. The battery's cost is not any one check; it is
the FULL tier's per-beat fan-out (dramatic-question, swain, gripe jury, reader probes) — and, until
RFC 0009, the self-heal rewrites those checks triggered, twice per failing beat, plus their
re-audits.

## 3. What the battery feeds

Two consumers read the battery's output:

1. **The publish gate** (docs/LOGIC.md §9). It reads logic-sweep findings, Story-Ledger rows plus
   `FACT-LEDGER`/`TUNEDREAD` findings, the blast-radius recheck, and Reader-Proxy QA High/BLOCKER.
   Of the 32 checks, the gate consumes the output of **four or five**: `logic-sweep`,
   `fact-ledger`, `tuned-read`, `reader-qa` (comprehension probes), and indirectly `verify-book`.
2. **The Structural Integrity Index.** Retired in spirit by author ruling 2026-08-03 ("remove
   scores; they mean nothing") and shown 2026-09-06 to grade F by arithmetic — twenty Low findings
   max a category, so a perfect book scores 0. BCODA scored **0** tonight with zero High findings.
   Twenty-odd checks exist primarily to feed this number.

So the honest description of the FULL battery is: *five checks the gate reads, wrapped in
twenty-seven that feed a retired score, at $27–$135 a run.*

## 4. Per-tier verdict, by evidence

### FREE (9 checks, ≈ $0.60/run) — keep, it is nearly free and two of its checks gate publication
`plant-audit`, `plant-density`, `validate-nouns`, `timeline-check`, `verify-book`, `coordinate`,
`voice-consistency`, `duplicate-beats`, `sanity-scan`. Deterministic or near-zero-cost. `verify-book`
and `sanity-scan` are the pre-export gates. None has an applied finding, but none costs anything
either. The one to watch: `verify-book`'s Swain/DramaticQuestion-adjacent classification checks
were the triggers for the deleted self-heal loop; they now only report.

### DEEP (15 checks, one LLM call per check) — keep 3, question 12
| Keep (all applied findings live here) | Zero applied findings, ever |
|---|---|
| `logic-sweep` — canonical QA, gates publication | `examine-emotion`, `book-audit`, `diagnose-book`, `check-fidelity`, `craft-checklist` (LLM half), `check-canon`, `altitude-audit`\*, `theme-coherence`, `applied-claim-drift`, `pov-audit`, `hook-audit` |
| `fact-ledger` — Story Ledger detector 1, gates publication | |
| `lint-prose` — deterministic, $0, one real ALT-SCENE catch | |
| `reader-qa` (comprehension probes) — gate reads its High/BLOCKER; 0 applied so far | |

\* `altitude-audit` is already on record as producing 100 % false positives on a lossy synopsis
(feedback memory 2026-09-05). It is the instrument the author reads *directly*; whether it belongs
in an automated bundle is a separate question from whether it belongs at all.

### FULL (8 checks, the $27–$135) — nothing here has ever paid for itself
`storyscope-audit`, `swain-audit`, `chekhov-audit`, `five-act-map`, `dramatic-question`,
`sacred-flaw`, `gripe-pass`, `tuned-read`. Zero applied findings across all eight, all time.
`tuned-read` is the exception worth defending on design rather than record: it is Story Ledger
detector 2, it is cheap (≈ $0.09, per uncached candidate), it gates publication, and the ledger
programme is four days old. The other seven are the checks whose verdicts used to trigger prose
rewrites; with RFC 0009 they can only file findings — into a queue with a 0.03 % apply rate.

## 5. The cost gate is tier-blind — and that is a bug, not a quirk

`HubCliClient.ForwardWithCostGateAsync` keys the estimate on `commandName` alone (`"--audit-book"`).
`--deep` and `--full` never reach the estimator. One blended history therefore quotes **$69 for a
$0.60 run** and quoted **$1.50 for a $135 run** for thirteen runs straight. The 13-run spend in §2
happened *because* the gate said $1.50.

Fix (small, not done in this RFC): make the cost-history key include the tier —
`--audit-book`, `--audit-book --deep`, `--audit-book --full` as three commands — so each tier
learns its own number. Until then, treat the quote as noise.

## 6. Recommendation

1. **Stop running `--audit-book --full` as routine.** It has never produced an applied finding and
   costs $27–$135 a run. Nothing in it is required by the publish gate except `tuned-read`, which
   is cheap and can be run on its own (`prose --tuned-read`).
2. **Redefine the battery as what the gate reads:** `logic-sweep`, `fact-ledger`, `tuned-read`,
   `reader-qa`, `lint-prose`, plus the FREE tier. That is the set with evidence. Everything else
   becomes a standalone instrument the author invokes deliberately, by name, when a question calls
   for it — the same status the retired score panels already have (SS-A44).
3. **Apply the RFC 0009 findings-diet rule to the rest:** a check that has never produced an
   applied finding is deleted from the bundle, not toggled. Twelve DEEP checks and seven FULL checks
   qualify today. Re-admission is by evidence — an applied finding — not by argument.
4. **Fix the tier-blind estimator** before the next `/full-battery` invocation, or the next
   $1,100 is one keystroke away.
5. **Retire the SII from the report.** It is the only consumer most of these checks have, the
   author has already ruled scores meaningless, and it reads 0 on a book that just passed 5/5.

## 7. What was not measured

- Apply rate is a lower bound on value: a finding can inform a hand edit without ever being marked
  Applied. But 26,000 findings and 8 applications is not a bookkeeping gap; and the three
  instruments that *did* produce applications are exactly the three the author already trusts.
- Costs are from the cost ledger's scoped runs only; `--logic-sweep` is not cost-scoped and does
  not appear. Its per-round cost is unknown here — worth adding a scope (`BeginCostScope`).
- FREE/FULL attribution in §2 is inferred from cost magnitude; the ledger does not record the tier
  (see §5 — same root cause).
