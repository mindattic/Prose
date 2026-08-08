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

## 2. The four instruments {#SS-RQA-2}

| # | Instrument | What it measures | Cost model |
|---|---|---|---|
| 1 | **Comprehension probes** (`prose --reader-qa`) | A cheap model (Haiku) reads each chapter cold with only a rolling recap; its genuine reading is diffed against the fidelity-strict Sonnet synopsis (`SynopsisExportService`); a Sonnet arbiter keeps only mismatches the chapter text plausibly supports. | Hash-cached per chapter (`NodeChapterSummary.ComprehensionJson`) — unchanged chapters re-run free. |
| 2 | **Craft/delight checklist** (`prose --craft-checklist`) | Binary checks per beat: CRAFT.md §8 DON'Ts (literal) + "≥1 applicable DELIGHT move lands" (short connective beats exempt), rules parsed live from CanonDocumentSections. Book-level move-monotony counters implement DELIGHT §14 — never "all 13 moves per beat". | Hash-gated per beat (`BeatChecklistResults`, Beat.TextHash + rule-set hash) — only changed beats re-bill. |
| 3 | **Pairwise edit gate** (`prose --duel`) | Before-vs-after for every splice: cross-family jury (one lens per live model family; Claude tiers fill in when families are dead), each lens judging BOTH presentation orders — an order-flipped verdict is discarded as noise. REPLACE ≥2 better + 0 worse. | Verdicts cached by text-hash pair (`BeatDuelVerdicts`). SS-A44: duels ARE votes — `--allow-votes` required. |
| 4 | **Gripe jury** (`prose --reader-qa --gripe-pass`) | 3–5 cross-family full-read readers emit ONLY page-anchored complaints (beat + verbatim quote + what's wrong). Deterministic quote-grounding kills hallucinated citations free; a Sonnet arbiter confirms each against the beat text; triage blocker/moderate/minor. | Fresh per run (readers should re-read changed books); arbitration only on unique grounded complaints. |

All four file into the **Findings** table (categories `ComprehensionDefect`,
`CraftChecklist`, `ReaderGripe`) with delete-then-recreate supersession per run —
`list_findings` / `apply_finding` / `set_finding_status` are the workflow surface.
Reports land in `audit-outlines-<date>/reader-qa/<SLUG>.md`.

Instruments 1, 2, and 4 (report mode) are **measurements, not votes** — they are
outside the SS-A44 VotingGate, same exemption as `craft_checklist` and the logic sweep.
Instrument 3 and any automated apply arm go through the gate.

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
