---
codex: 1
project: StreetSamurai
code: SS
layer: amendments
status: living
updated: 2026-06-23
---

# StreetSamurai — Amendments (append-only; amendment wins over the bible)

> Append-only. Never rewrite an amendment; supersede it with a new one. Beyond ~25, fold into the
> bible and start a new epoch (note the git tag); history stays in git.

## Epoch 1 — SS-A1 through SS-A14 — graduated 2026-06-23

All amendments through SS-A14 have been merged into their canonical destinations:

| Amendment | Graduated to |
|---|---|
| SS-A1 — Codex standard | `docs/BIBLE.md` structure + `CLAUDE.md` |
| SS-A2 — Multi-Universe design | `docs/BIBLE.md` §4.2, §5 (SS-LAW-15) |
| SS-A3 — Multi-Universe implementation | `docs/BIBLE.md` §4.2, §6 |
| SS-A4 — Universe segregation + UUIDv7 | `docs/BIBLE.md` §4.2 |
| SS-A5 — Fully relational canon | `docs/BIBLE.md` §4.2 (Records framing updated) |
| SS-A6 — Underlying Connection design | `docs/strands/MNEMO.md` §3–4 |
| SS-A7 — Sparrow Act 2+3 design | `docs/strands/SPRW.md` §0, §3, §4 |
| SS-A8 — ATTE resonance-trace taxonomy | `docs/strands/ATTE.md` §4b |
| SS-A9 — BCODA arc + 16-chapter spine | `docs/strands/BCODA.md` §1–9 |
| SS-A10 — Null history + chapter swap | `docs/strands/BCODA.md` §5, §7 |
| SS-A11 — Pixel origin + per-strand docs | `docs/strands/TDIU.md` §3; `CLAUDE.md` |
| SS-A12 — Sparrow expansion + Sasha Vo | `docs/strands/SPRW.md` §3, §5–6 |
| SS-A13 — TVYT redesign as MNEMOSYNC novel | `docs/strands/TVYT.md` (recreated) |
| SS-A14 — ULC → Mnemosync rename + redesign | `docs/strands/MNEMO.md` |

Full amendment text is preserved in git history. Tag `epoch-1-amendments` marks the graduation commit.

---

## SS-A15 — Emotional Intelligence Examination system {#SS-A15}

**Date:** 2026-06-23 · **Author:** emotional-depth pass · **Ref:** [RFC 0010](rfc/0010-emotional-intelligence-examination.md)

The engine's emotional examination was binary (Pass/Warn/Fail at strand granularity, no character
model). SS-A15 adds a parallel **Emotional Intelligence Examination** sub-system that scores prose
against an 8-dimension, 0–4 rubric — per beat, character-aware (Want/Need/Wound/Flaw via a
per-strand `CharacterEmotionalLedger`), and register-adaptive (CODA vs JOY/SORROW/Fantasy anchors).

**What ships:**
- `EmotionalDepthService` + `EmotionalLedgerService` (new services mirroring `StructuralDiagnosticService`)
- 4 new DB tables: `EmotionalExaminations`, `EmotionalDimensionResults`, `EmotionalBeatScores`,
  `CharacterEmotionalLedgers`; new `Beat.EmotionalScore` column (float?)
- CLI: `ss --examine-emotion --slug <slug> [--effort draft|standard|deep] [--json]`
- MCP: `examine_emotional_depth(strandIdOrSlug, effort, maxChars)`
- Advisory cap: at the Deep/publish gate, open blocking emotional findings (`WantNeedDivergence`,
  `CostFeltNotAsserted`) prevent publish-readiness. Does NOT alter the 82/85 headline score math.
- Craft authority: [CODA register](registers/CODA.md) + per-strand bibles (Want/Need/Wound/Flaw).

**Invariant added to BIBLE §10:** emotional depth score is a side-car signal with a Deep-tier
advisory cap; it never folds into the 82/85 headline gate.

<!-- Next amendment: SS-A16 -->
