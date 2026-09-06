# RFC 0009 — No Autonomous Prose Writes

**Status:** proposed · **Author ruling:** 2026-09-06 · **Supersedes:** nothing (this is new law)

> "I'm tired of having 100s of services each acting as its own boss; it's too many cooks in the
> kitchen."
> "Every beat must be verifiable in a way that some LLM can't just undo cause it thought THIS way
> or THAT way was better."
> "Gone is gone — forget the toggles."

---

## 1. The defect

The engine grew 310 services. Twenty-four of them file Findings. **Eleven code paths across seven
services rewrite finished prose with an LLM.** None of the eleven asks first.

Measured, corpus-wide, 2026-09-06:

| Measure | Value |
|---|---|
| Findings raised (lifetime) | ~26,000 |
| Findings **applied** | **8** |
| Findings dismissed | 12,011 |
| Findings open | 13,899 |
| Apply rate | **0.03%** |
| BCODA archived book versions | **68** |

Every finding ships with a `fix:` line — an imperative. The queue reads as work. It is not work.

### 1.1 The cascade

A beat write is not local. `UpdateBeatTextAsync` bumps `Beat.Version`, recomputes `TextHash`, and
transitively invalidates:

- the beat's **event summary** (`event_summary_state → stale`) — which is the 100 ft altitude every
  synopsis-based instrument reads;
- its **ledger claims** (hash-gated re-extraction re-derives them);
- its **entity tags** (re-derived on save);
- the **blast-radius recheck**;
- and the publish gate's **convergence clock** — docs/LOGIC.md §9 requires *two consecutive dry
  sweep rounds*, so **one unasked-for rewrite puts every other beat in the book back in question.**

So an audit that repairs a beat to satisfy its own rubric doesn't just change that beat. It
re-stales its summary, re-extracts its claims, and resets the gate that determines whether the book
is finished — creating fresh findings elsewhere, which the next repair pass then acts on.

### 1.2 The worst offender

`BookHealthService.SelfHealAsync` is a *generic helper*, used at three call sites. The design intent
is explicit: **audit check fails → LLM rewrites the beat → re-check → rewrite again.**
`MaxSelfHealAttempts = 2`, hardcoded. No setting, no flag, no opt-in. It runs inside
`prose --audit-book`, which is what `/full-battery` invokes.

What triggers it:

- a beat scores low on **DramaticQuestion** → rewrite with *"Reveal who this character really is
  beneath the surface action."*
- a beat fails **Swain classification** → *"Rewrite as a proper Scene (Goal→Conflict→Disaster)."*
- **EventType / SubplotCarrier / EscalationFloor** miss a numeric threshold → repair.

That is an LLM scoring the author's prose against a rubric and then rewriting the prose until its
own score passes. It is a Goodhart loop with the book as the target.

---

## 2. The law

> **An LLM may write prose the author asked for. It may never rewrite prose the author already
> accepted.**

Three corollaries:

1. **A finding describes; it never instructs.** The `fix:` field is removed from the finding
   contract. An instrument reports what it observed. Only the author decides what that means.
2. **Deletion, not toggles.** A toggle says "maybe someday." A path that lets an LLM overwrite
   finished prose on aesthetic judgment has no correct setting.
3. **Every beat write is attributable.** No code path may change beat text without declaring, in a
   type the compiler enforces, which authorised reason it is acting under.

---

## 3. What goes

| # | Path | Why |
|---|---|---|
| 1 | `BookHealthService.SelfHealAsync` + its **3** call sites | audit → LLM rewrite → recheck, ungated, 2 rounds/beat |
| 2 | `BeatRepairService` | orphaned once (1) and (4) are gone |
| 3 | `SwainAuditService.SpliceAsync` / `ApplySpliceAsync`, `--swain-repair` CLI, `swain_repair` MCP | an LLM adds "the missing DISASTER" to a finished beat |
| 4 | `AutoRunCli` repair loop (`MaxRepairAttempts`) | already known to damage prose — `--no-repair` was made standing policy 2026-08-23 |
| 5 | `GripePassService` splice write | rewrites prose from a reader complaint, gated only by another LLM's vote |
| 6 | `ProseReflowService` LLM path | **not a formatter** — `llm.GenerateAsync` at temperature 0.2, free to change words |
| 7 | `ProsePatternGuard` (495 lines) + its 3 Cliche producers + `--check-prose` CLI + `check_prose` MCP + tests (529 lines) | apply rate 0; cosmetics on finished prose (author ruling, this session) |

Audits **keep their detection halves**. Swain still classifies; BookHealth still scores; the gripe
jury still gripes. They lose only the ability to act on the book.

## 4. What stays, and why

| Path | Why it survives |
|---|---|
| Generation (`ProseWriterRouter`, `--write-story`, `--expand-beat`) | this *is* the author asking for prose |
| `--edit-beat`, `--import-md`, `RestoreBeatTextCli` | the author's own hand |
| `FindingApplyService` | **deterministic** (verified: no LLM call) — the author applying a specific finding |
| `TrinityReconciliationService` beat patch | refuses unless the snippet matches **verbatim**, records a revertible `ReconciliationDecision`, and arbitrates a real fact conflict rather than a taste judgment. The most disciplined writer in the tree; keep it as the reference design. |
| Deterministic reflow | to be written — paragraph normalisation with no model in the loop |

---

## 5. The structural guarantee

Deletion fixes today. This stops it recurring.

```csharp
public enum BeatWriteReason
{
    AuthorEdit,          // --edit-beat, MCP update_beat_text
    Generation,          // ProseWriterRouter — prose that was asked for
    Import,              // --import-md
    Restore,             // --restore-beat-text, archive rollback
    FindingApply,        // deterministic application of a specific finding
    TrinityArbitration,  // verbatim-only patch with a recorded decision
    StructuralSplit,     // split/merge/join — moves text, never rewrites it
}
```

`UpdateBeatTextAsync` takes it as a **required** parameter and persists it. Consequences:

- Every beat change is attributable, forever, in a system-versioned table.
- A new autonomous writer cannot be added silently — it must add an enum member, which is a
  reviewable one-line diff in a file whose whole purpose is to be read.
- `Beat.Version` stops being an anonymous counter and becomes an audit trail.

---

## 6. Verifiability

New, free, deterministic:

```
prose --beat-provenance --slug <slug> [--suspect]
```

Per beat: `Version`, last write reason, `TextHash`, whether its event summary and ledger claims are
current against that hash. `--suspect` lists only beats whose history contains a write reason that
no longer exists — i.e. **prose an LLM changed on its own, before this RFC.** That is the list the
author needs and cannot currently obtain.

Companion measurement, answering "how bad was it":

```
prose --edit-distribution [--slug <slug>]
```

`Beat.Version` histogram corpus-wide. A finished beat should sit at a low, explicable number. This
quantifies the damage and gives a baseline that must not rise again.

---

## 7. The findings diet

Twenty-four categories, eight applies. Rather than 24 toggles, apply the same rule: **a category
that has never produced an applied finding is deleted, not disabled.** Retain, for now, only those
with hand-validated evidence:

- `Contradiction` — 2 of 2 hand-read real, one applied by the author; backs the publish gate.
- `Causality` — logic sweep, the canonical QA (SS-A44), gates publication.
- `OutlineDrift` — feeds the altitude audit the author reads directly.

Everything else is reviewed category by category against its measured apply rate before it is
allowed to keep filing. `FindingCategory` enum members are retained (historical rows store the
name); only the producers are removed — the same pattern used when `BehaviorContradiction` was
deleted 2026-09-06.

---

## 8. Order of work

| Phase | Work | Risk | Verifies |
|---|---|---|---|
| **0** | `--edit-distribution` + `--beat-provenance --suspect` | none — read-only | measures the damage **before** anything changes |
| **1** | Delete §3 items 1–5 (the LLM rewriters) | medium — touches `BookHealthService`, `AutoRunCli`, MCP surface | full test suite; `--audit-book` still reports, writes nothing |
| **2** | Delete §3 items 6–7 (reflow LLM path, ProsePatternGuard) | low | ditto |
| **3** | `BeatWriteReason` required on `UpdateBeatTextAsync` | medium — every caller updated | compiler enforces completeness |
| **4** | Findings diet (§7) | low | apply-rate evidence per category |
| **5** | Deterministic reflow to replace the deleted LLM path | low | golden-file tests |

Phase 0 first, deliberately: once the rewriters are gone the evidence of what they did is harder to
separate from ordinary edits.

**Nothing in phases 1–5 touches prose.** The book is not edited by this work.

---

## 9. Acceptance

- `grep -rn "UpdateBeatTextAsync" v3/` returns only callers passing a `BeatWriteReason`.
- No path from an audit, score, checklist, or finding reaches beat text without `AuthorEdit`,
  `FindingApply`, or `TrinityArbitration`.
- `prose --audit-book --full` on an unchanged book leaves `Beat.Version` unchanged for every beat.
  **This is the test that would have caught the whole defect**, and it does not exist today.
