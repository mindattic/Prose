# Structural Fix Plan — Steppin' Razor (SRZR)

Slug `steppin-razor-019ef7be`. Files only — no database writes performed. This plan documents the
rulings; the beat-by-beat prose lives in `beats/`. Applying these changes to the DB (via `ss --beat`
CLI, never raw SQL) is a follow-up step outside this task's scope.

Beat order below is `NodeBeats.SortKey` order (true reading order), each cited by `Beats.Number`.

## 1. Camel-man delock — REQUIRED

**Ruling: DISABLE beat 4875 in full; REWRITE beat 4419 to strip the "Devereux" opening.**

- Beat 4875 (`019F1170-DF7D-771C-BB02-562E6F873800`, SortKey 49.951171875) exists *only* to stage
  the man on the camel arriving at the End of the Line as an ordinary traveler, before the bar scene
  and before his bible-designated Ch12 reveal (beat 4420). It does no other structural work — no
  plot, no character beat that survives without him. Per the task's own branching rule ("DISABLE if
  the beat exists only to stage him"), this is a clean disable, not a rewrite. Removing it entirely
  also resolves audit finding 2 (the two incompatible camel explanations in adjacent beats) — the
  mundane "Corridor pack-animal, two hundred kilometers off route" explanation was 4875's, and it
  directly undercut 4420's "there are no camels in Joliet" uncanny beat. With 4875 gone, 4420 is the
  reader's first and only camel encounter, unspoiled.
- Beat 4419 (`019EF7F7-6364-71D2-98E1-141FF36E2BB2`, SortKey 50.0) opened with a sentence naming the
  rider "Devereux" and staging him entering the bar with her. That sentence is deleted; the beat now
  opens directly on "The Hereafter occupied the same building..." Nothing else in 4419 references
  him, so no replacement traveler was needed — the bar scene's antagonist (the man who decides her
  size is an invitation) is unrelated and unaffected. This satisfies the requirement's other branch
  (rewrite to remove him, no replacement markers needed since removal is total).
- Confirmed: after this pass, the man on the camel does not appear, speak, or get named anywhere
  before beat 4420 (Ch12, his designated reveal). Act 1 (4860–4419) is now camel-man-clean.

## 2. Sigma/Ferreira purge — REQUIRED

**Ruling: REWRITE 4421, 4423, 4876 in place; 4870 rewritten (below, folded into the market-well cut);
4872 disabled (below, folded into the market-well cut) — its Ferreira line goes with it.**

- **4421**: `*Sigma gave her a lot of things. It didn't give her a word for this.*` → replaced with a
  gray-zone-origin internal line (no institution, no curriculum — the SS-A20 canon).
- **4423**: `*She had spent eleven years making sure she was never in the same room as a Sigma
  installation. The towers were full of them.*` → replaced with a line keyed to "never
  corpo-registered" (a credential reader, not an institution) — same narrative function (dread of
  the towers' density), zero Sigma content.
- **4876**: the "pharmaceutical regimen... cost her most of what she cleared on Ferreira" line →
  replaced with the bible's actual coping mechanism (violence as the volume knob, not medication).
  This also removes the only other named clinician reference in the drafted beats.
- **4870**: `Ferreira had said—` (cut off mid-sentence) and a numeric error (`the pull has been true
  for twenty-three years` — Sasha is 19) → both replaced. See §4 for the rest of this beat's changes.
- **4872**: `Ferreira said drift was managed. Ferreira said ten years was early...` → moot; this beat
  is disabled entirely under the market-well ruling (§4), which removes the reference along with the
  scene it sat in.

All five target beats named in the task are addressed: three by direct rewrite, two by the
market-well cut (§4) subsuming them.

## 3. Standing-order bounty thread — REQUIRED

**Ruling: PATCH beat 4869 (add a closing paragraph). No new beat inserted — cheapest sound payoff,
landed at the point where the thread already sat (immediately after 4868, the beat where the order
is last live on the page).**

- Payoff chosen: **the order's money doesn't cross the gray-zone/corpo boundary.** The order is
  explicitly gray-zone currency — hunters bought on reputation and a folding table's word, no ledger,
  no registration. Joliet's north side, the freight interchange into the GLMZ proper, runs a
  Consensus credential check. The order can buy a hub's watch list; it cannot buy a checkpoint that
  wants a registered identity neither Sasha nor the order's hunters have. This uses world logic
  already established in the story (TESSERA/Consensus credentialing appears explicitly two beats
  later, in 4870) rather than inventing a new mechanism, and it reads as the character's own
  reasoning, not authorial cleanup.
- This was picked over "rescinded" or "was a probe" because both of those require someone on the
  order's side to make a decision the reader never sees and would read as a coincidence rescuing her.
  A structural boundary she reasons through herself keeps the thread in her POV and matches the
  bible's §10-3 recommendation ("let it die... the GLMZ swallows the old life whole") without an
  unearned off-page event.

## 4. Market-well subplot — REQUIRED, reconciliation-or-cut choice made

**Ruling: CUT.** Disable 4871 and 4872 in full; rewrite 4870 to keep only its transitional function
(surface exit into the Glooms, heading toward the bridge in 4769) and drop everything that gestured
at a fourth, separate well.

Reasoning:
- The market-well material (4870's back half, all of 4871, all of 4872) is not in the bible's
  chapter table at all — it was inserted after the 26-chapter redesign without a spine slot. It
  introduces a phenomenon ("something very large and very awake breathing," a market that reacts to
  her specifically, four unnamed people closing off a lane) that is never identified as the colonnade
  site or distinguished from it (audit finding 8), and it ends on an unresolved cliffhanger — the
  four people in 4872 never get a page-two (audit finding 7).
- It also duplicates the shape of the "Found" set piece (4774) — a body count of exactly four closing
  in on her — with unnamed antagonists that can't be the same event as 4774's named Halcyon
  operatives. Keeping both scenes would require either merging them (a heavier rewrite than the
  task's structural scope) or explaining two separate four-person convergences, neither of which pays
  for itself.
- Reconciling instead of cutting (i.e., stating "this is the same site as the colonnade, approached
  from underneath") was considered and rejected: the colonnade is reached later, from a different
  approach (the elevated walkway, per 4772), at a different point in the story, and forcing a
  same-site claim would require rewriting the colonnade reveal's geography too — out of proportion to
  a continuity-audit fix.
- Cutting removes the market-well material cleanly because it was never load-bearing: nothing in
  4769, 4770, 4771, or later beats references the market, the vendor, or the sealed door. The only
  thing 4870 needs to preserve is the physical transition from the checkpoint (4768) to street level
  to the bridge (4769), which the rewritten beat still does.
- Side effect: cutting 4872 also removes one of the two near-identical "saved by the patrol clock"
  interruptions the audit flagged in finding 11 (4872's cliffhanger and 4971's patrol-rotation
  near-miss were structurally identical beats). 4971 is untouched and remains the sole occurrence —
  not requested by the task, but noted as a bonus resolution.
- Also fixed in the surviving 4870: the dog/cat mismatch (4870 had "the dog pressed its flank" after
  the dog had already been replaced by the cat as of beat 4420, three beats prior in reading order).
  Since this beat was already being opened up for the Ferreira purge, the one-word fix (dog → cat)
  was folded in rather than leaving a contradiction sitting inside a beat this pass had already
  touched. This is *not* a fix of audit finding 5 generally (the broader dog→cat handoff across
  4419→4420 has no on-page acknowledgment anywhere and is out of this task's required scope) — it is
  narrowly the instance inside a beat already being edited.

## 5. Beat 4775 — Axiom → Halcyon — REQUIRED

**Ruling: PATCH.** Both occurrences ("indifferent to Axiom's cover story" and "Axiom had just sent
four people who'd drilled against the Read") replaced with "Halcyon." No other content touched.

## 6. Halcyon AI-ownership conceit — OPTIONAL, DONE

**Ruling: one on-page beat, minimal footprint.** Patched beat 4771 (the credential pickup, the
existing DRAFTED beat where she takes the Halcyon Strategic Resources credential off a downed
operative) to add one sentence: the credential's small print names its chartered officer of record
as "OBERON" — a machine's name where a signature belongs — which she files without comment and keeps
moving. This surfaces the AI-ownership idea exactly once, in a place that's already look-at-this-object
beat, without requiring a new scene or committing the climax to using it. It does not confirm
OBERON/Halcyon as the AI cabal (the bible's canon-locked ambiguity is preserved) — it's a detail she
notices and files, same register as everything else she notices and files.

## Summary of file counts

| Requirement | Beats touched | Action |
|---|---|---|
| 1. Camel-man delock | 4875, 4419 | DISABLE, REWRITE |
| 2. Sigma/Ferreira purge | 4421, 4423, 4876, 4870*, 4872* | REWRITE ×3, folded into §4 ×2 |
| 3. Bounty thread closure | 4869 | PATCH (new closing paragraph) |
| 4. Market-well subplot | 4870, 4871, 4872 | REWRITE, DISABLE, DISABLE |
| 5. Axiom → Halcyon | 4775 | PATCH |
| 6. OBERON gesture (optional) | 4771 | PATCH |

Net: **2 beats disabled, 5 beats rewritten/patched with new prose, 2 beats patched with small
surgical insertions.** No new beats created — every fix landed inside an existing beat's slot, per
the "cheapest sound payoff" instruction. Total distinct beats touched: 8 (4419, 4421, 4423, 4771,
4775, 4869, 4870, 4876) plus 2 disabled (4871, 4872) plus 1 disabled (4875) = 11 beats total.

## Judgment calls flagged for author review

1. **Bounty closure mechanism** (credential-checkpoint boundary) is a judgment call among several
   sound options (rescinded, expired, probe). Chosen because it uses standing world logic and stays
   in Sasha's POV rather than an off-page decision by the order.
2. **Market-well cut vs. reconcile** — cut was chosen over reconciling it as "the same site as the
   colonnade" because the geography doesn't line up without a heavier rewrite of the colonnade
   reveal. If a future pass wants the extra dread beat, the right fix is a *new* purpose-built
   connective beat between 4768 and 4769, written clean against current canon from the start, rather
   than trying to salvage 4870–4872.
3. **Dog→cat handoff** (audit finding 5) is NOT fixed as a general matter — only the one instance
   inside 4870, which this pass was already touching for the Ferreira purge. The actual handoff (why
   the dog is gone and the cat is suddenly there, first visible at the 4419→4420 seam) has no
   explanation anywhere in the current text and was out of this task's required scope. Flagging it
   as the next continuity item worth a dedicated pass.
4. **OBERON gesture** — deliberately minimal (one sentence, no plot consequence) so it doesn't
   over-commit the climax to a reveal the bible keeps ambiguous. If the story wants Halcyon's
   AI-ownership to matter later, this line is a plantable seed, not a payoff.
