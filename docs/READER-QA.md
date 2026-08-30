---
title: Reader-Proxy QA — the reader-facing quality methodology
status: canonical
adopted: 2026-08-03
---

# Reader-Proxy QA {#SS-RQA}

The default reader-facing quality instrument for every book. Peer of
[docs/LOGIC.md](LOGIC.md): the Logic Sweep owns continuity/causality QA; Reader-Proxy
QA owns craft/comprehension/reader-experience QA. **It emits no scores, ever.**

## 1. Why the score panels were retired {#SS-RQA-1}

The persona review panels (Expert/FocusGroup, 1024-stranger ballots, 0–100 means)
were retired as the default QA on 2026-08-03 (author ruling: "remove scores; they
mean nothing"). The research basis is structural, not a tuning problem:

- **Correlated errors.** Nine judges from seven model families provide ~2 effective
  independent votes ([arXiv 2605.29800](https://arxiv.org/abs/2605.29800)); the single
  best judge matches the whole panel. 1024 same-model personas ≈ one vote at 1024×
  the cost. The observed 70–90 score waffle was judge noise, not signal.
- **Persona prompting fakes diversity.** Sociodemographic personas create the
  appearance of diverse opinion without behavioral differentiation (ACL Findings
  2025, "The Prompt Makes the Person(a)"). Real persona fidelity requires
  interview-grounded conditioning (Stanford generative agents, arXiv 2411.10109) —
  a possible future MindAttic.Legion upgrade, not a prompt trick.
- **Pointwise scores are unstable; pairwise and binary judgments are not.**
  LLM judges have no stable internal 0–100 scale; they are reliable at "which of
  these two is better" and at yes/no checks with evidence (Pair2Score; EQ-bench
  creative-writing v3 methodology).
- **Measured comprehension beats simulated opinion.** A small model's *genuine*
  misreading of a chapter is a defensible proxy for a median reader's confusion;
  a large model *pretending* to be a reader is not.

The legacy panel machinery is quarantined, not deleted — `--review-node` /
`review_book` still run behind the SS-A44 VotingGate on explicit request only, and
the 1024-persona library lives on in the MindAttic.Legion package for other projects.

## 2. The five instruments {#SS-RQA-2}

| # | Instrument | What it measures | Cost model |
|---|---|---|---|
| 1 | **Comprehension probes** (`prose --reader-qa`) | A cheap model (Haiku) reads each chapter cold with only a rolling recap; its genuine reading is diffed against the fidelity-strict Sonnet synopsis (`SynopsisExportService`); a Sonnet arbiter keeps only mismatches the chapter text plausibly supports. | Hash-cached per chapter (`NodeChapterSummary.ComprehensionJson`) — unchanged chapters re-run free. |
| 2 | **Craft/delight checklist** (`prose --craft-checklist`) | Binary checks per beat: CRAFT.md §8 DON'Ts (literal) + "≥1 applicable DELIGHT move lands" (short connective beats exempt), rules parsed live from CanonDocumentSections. Book-level move-monotony counters implement DELIGHT §14 — never "all 13 moves per beat". | Hash-gated per beat (`BeatChecklistResults`, Beat.TextHash + rule-set hash) — only changed beats re-bill. |
| 3 | **Pairwise edit gate** (`prose --duel`) | Before-vs-after for every splice: cross-family jury (one lens per live model family; Claude tiers fill in when families are dead), each lens judging BOTH presentation orders — an order-flipped verdict is discarded as noise. REPLACE ≥2 better + 0 worse. | Verdicts cached by text-hash pair (`BeatDuelVerdicts`). SS-A44: duels ARE votes — `--allow-votes` required. |
| 4 | **Gripe jury** (`prose --reader-qa --gripe-pass`) | 3–5 cross-family full-read readers emit ONLY page-anchored complaints (beat + verbatim quote + what's wrong). Deterministic quote-grounding kills hallucinated citations free; a Sonnet arbiter confirms each against the beat text; triage blocker/moderate/minor. | Fresh per run (readers should re-read changed books); arbitration only on unique grounded complaints. |
| 5 | **Full-Order Read** (`prose --reader-qa --full-order-read`) | 3–5 cross-family readers narrate ONE continuous read start-to-finish and flag only where their own engagement died — the beat it started, and whether it ever recovered. NOT a complaint list: no craft judgment, only "where did I stop caring." Severity comes from the recovery signal (never recovers = blocker; recovers after a long stretch = moderate; brief dip = minor). | Fresh per run, same as instrument 4; arbitration only on unique grounded spans. |

All five file into the **Findings** table (categories `ComprehensionDefect`,
`CraftChecklist`, `ReaderGripe`) with delete-then-recreate supersession per run —
`list_findings` / `apply_finding` / `set_finding_status` are the workflow surface.
Reports land in `audit-outlines-<date>/reader-qa/<SLUG>.md`.

Instruments 1, 2, 4, and 5 (report mode) are **measurements, not votes** — they are
outside the SS-A44 VotingGate, same exemption as `craft_checklist` and the logic sweep.
Instrument 3 and any automated apply arm go through the gate.

**Instrument 5 is a proxy, not the ritual itself (docs/LOGIC.md §10).** The felt-pass doctrine's
one sacred instrument is a human author's own full-order read at reader speed — an LLM doesn't
get bored the way a person does, though it can be prompted to notice textual flatness.
Instrument 5 makes running an *approximation* of that ritual unattended and cheap; it does not
replace the author actually reading the book straight through before calling it done.

## 3. Fix discipline {#SS-RQA-3}

Same law as the logic sweep (SS-A44 / "target not score"): **fix what a finding
names; if you can't name the failure, leave the beat alone.** Minimal splices via
`update_beat_text`; a contested splice goes through the duel gate and a KEEP verdict
returns the dissent rationales as revision fuel, never a silent force-replace.

## 4. What replaced the number {#SS-RQA-4}

Dashboards show **open findings by category/severity + last-run date** instead of
`Node.Score`. The 0–100 columns and the ≥82/≥85 gates are retired; historical
`NodeReviews`/`NodeScoreHistory` rows remain in the DB as history (temporal-table
discipline — nothing deleted). Nothing writes new scores except an explicitly
requested legacy panel run.

Publish-readiness = logic sweep clean at BLOCKER level + Reader-Proxy QA open
BLOCKER/High findings = 0. Counts of open MODERATE/MINOR findings are editorial
judgment, not a gate.

## 5. Jury provider policy {#SS-RQA-5}

Jury roster: `ReaderQaJuryProviders` setting (default
`claude-api,openai,gemini,deepseek,kimi`); extra OpenAI-compatible families declare
in `ExtraJuryProvidersJson` (id/baseUrl/cheapModel/pricing — a settings edit, not
code; keys resolve from MindAtticCredentialStore by id). Every candidate is
liveness-pinged once per session (~fractions of a cent); dead/unfunded accounts are
excluded with a logged warning and juries degrade gracefully — a single live family
still produces verdicts (tier-diversified within Claude), and a refreshed key joins
automatically. **No run ever fails, and no new funding is ever required, because a
provider died.**

## 6. What the instruments refuse to file {#SS-RQA-6}

A finding that argues in its own evidence that it is not a finding discredits every
other finding in the report. Two verified guards, not prompt requests:

- **Self-declared intentional ambiguity** (`ComprehensionProbeService.DemoteSelfDeclaredIntentional`,
  2026-08-24). The arbiter prompt already says deliberately open mysteries the text
  marks as unresolved are the text working as intended and must be rejected — but that
  was prompt-side with nothing checking it, and the BCODA run of 2026-08-24 filed
  confirmed defects describing themselves as "an intentional mystery the text marks as
  such", "left deliberately unstated", "inherent to the text's style rather than a
  comprehension failure". Those are now demoted to `kind="intentional-ambiguity"`,
  counted separately in the report, and never filed as findings. Applied on the cache
  read path too, so rows written before the guard heal without a re-bill.
- **Self-declared non-findings** in the logic sweep
  (`LogicSweepService.IsSelfDeclaredNonFinding`) — the same failure mode, fixed first.

Both phrase-match **narrowly**, on explicit verdict/intent language only. Neither may
key on "genuine"/"genuinely": the arbiter uses those words to mean *the text really
does under-establish this*, which is a confirmation. If a real finding is ever
suppressed, the phrase that did it is in one of those two lists.

**Known false-positive source, not yet fixed:** the probe reads each chapter cold with
a recap of only the previous three chapters, and the arbiter judges against that
chapter's text alone. So a term established early and paid off late (Cacophony,
introduced in BCODA ch1 with detail) gets confirmed as "never defined anywhere in the
chapter" when it reaches ch34 — an artifact of the instrument's window, not a defect in
the book. Treat "never explained in this chapter" findings on late chapters as suspect
until the arbiter is given the earlier chapters' synopses to check against.
