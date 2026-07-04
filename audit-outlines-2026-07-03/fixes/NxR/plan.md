# Neon & Rust (NxR) — Structural Fix Plan

Slug `neon-rust-019f06da` · Book 2 of the Rook Trilogy · Current score 89.6 · Target: unblock 90+ ceiling

Source: `docs/nodes/NxR.md` (bible, Lock #1 binding), `docs/nodes/MxG.md`, `docs/nodes/CxC.md`,
`audit-outlines-2026-07-03/NxR.md` (structural audit), live beats pulled read-only from
`Beats`/`NodeBeats` on 2026-07-03. **No DB writes were made — this plan and the beat drafts are
files only, staged for a human/CLI-driven apply pass.**

## Beat Map (final intended order)

| Order | Action | Beat Id | Title (working) | Current SortKey | Proposed SortKey |
|---|---|---|---|---|---|
| 01 | KEEP | 4830 | The Message | 50 | 50 |
| 02 | KEEP | 4898 | *(interstitial)* Walking the Count | 75 | 75 |
| 03 | KEEP | 4831 | Assembly | 100 | 100 |
| 04 | KEEP | 4899 | *(interstitial)* Ohara's Address | 125 | 125 |
| 05 | **PATCH** | 4832 | Ohara's Briefing | 150 | 150 |
| 06 | KEEP | 4833 | Vox's Read | 200 | 200 |
| 07 | **NEW** | — | The Search He Didn't Log | — | 210 |
| 08 | KEEP | 4915 | *(interstitial)* The Ghost Channel | 225 | 225 |
| 09 | KEEP | 4834 | Into Z2 | 250 | 250 |
| 10 | KEEP | 4900 | *(interstitial)* Approach to Stave | 275 | 275 |
| 11 | KEEP | 4835 | The Debt | 300 | 300 |
| 12 | KEEP | 4836 | Stave's Condition | 350 | 350 |
| 13 | KEEP | 4901 | *(interstitial)* The Site Decision | 375 | 375 |
| 14 | KEEP | 4837 | Lace Surfaces | 400 | 400 |
| 15 | **PATCH (reorder)** | 4902 | *(interstitial)* The Two Hours Before | 475 | **445** |
| 16 | **PATCH** | 4838 | The Setup | 450 | 450 |
| 17 | KEEP | 4916 | *(interstitial)* PEREGRINE Closes In | 462.5 | 462.5 |
| 18 | **PATCH** | 4972 | *(interstitial)* The Door | 468.75 | 468.75 |
| 19 | **NEW** | — | What the Building Knew (set-piece) | — | 470 |
| 20 | **PATCH** | 4839 | PEREGRINE Closes | 500 | 500 |
| 21 | **PATCH** | 4840 | The Procedure | 550 | 550 |
| 22 | **REWRITE** | 4903 | *(interstitial)* The Second Wall | 575 | 575 |
| 23 | KEEP | 4841 | The Contents | 600 | 600 |
| 24 | **REWRITE** | 4842 | The Decision | 650 | 650 |
| 25 | **NEW** | — | What Was Left | — | 660 |
| 26 | **PATCH** | 4904 | *(interstitial)* The Barge | 675 | 675 |
| 27 | **REWRITE** | 4843 | The Count | 700 | 700 |

**Counts:** KEEP 14 · PATCH 9 · REWRITE 3 · NEW 3 · DISABLE 0 (27 beats total, up from 24; no beat
is cut — the story's problems are contradiction, thinness, and one ordering bug, not excess length).

**SortKey correction for #4902 (Requirement 5):** moves from **475 → 445**, ahead of #4838 (450),
#4916 (462.5), and #4972 (468.75). As delivered, #4902 narrates the crew's two-hour setup/arrival
*after* the story has already shown PEREGRINE detected (#4916, 462.5) and Rook's knuckles scraped
open on the hijacked blast door (#4972, 468.75) — an unsignaled flashback the audit correctly flags
as a SortKey assignment error, not intentional structure. At 445 it reads in the order it happened:
setup → "morning" cold-open (#4838) → PEREGRINE detected → the door → the crew's fight (new #19) →
the interior procedure. #4838 is patched in the same pass to drop its own restaged "Ohara came in
last" arrival, since #4902 (now upstream of it) already puts her in the room two hours earlier.

## Requirement → Beat Coverage

| # | Requirement | Implementing beats |
|---|---|---|
| 1 | Re-thread the partition reveal off the third-party/"vessel" framing; resolve 18-months-vs-two-years | **REWRITE #4903** (removes "someone built this inside you... used you as a vessel"; Stave now reads the inner layer as Ohara's own earlier, unlicensed hand — built in the six months between the Old Harbor barge and the day she sealed the outer partition). **PATCH #4832** (creation date corrected to match bible §3: "eighteen months ago," not "two years ago"; the manifest/barge date stays "two years back," so the inner layer now predates the outer partition by ~6 months — no contradiction, and it explains why there's a *second* wall at all). #4840 and #4841 needed no plot changes — they already say "Ohara's own credential, Ohara's own hand" and never used the vessel language; they just inherited #4903's contradiction. Two minor location-word fixes land in #4840 as part of the same pass (see Req 5/location note below). |
| 2 | Plant Adalemo with a real earlier appearance + visible seed of doubt | **NEW #07 "The Search He Didn't Log"** (SortKey 210) — Vox's channel sweep catches Adalemo running an unlogged, off-tasking query against the Old Harbor manifest, then manually scrubbing it rather than letting it auto-clear. Gives him a body (the cheap post-lake shoulder repair — PEREGRINE's new paper funded the operation, not his own maintenance, which is the rust theme applied to the antagonist), a voice, and a specific name he's already carrying (Sefi Okonkwo — one of the twenty-one, seeded so CxC's "name and a face" survivor has an earlier thread to pick up). Rook reads it as usable intel, not sentiment — keeps MxG Lock #7 ("PEREGRINE is competent") intact. **REWRITE #4842** cashes this plant in: Adalemo's mercy is now explicitly the same private thread continuing, not a cold open. |
| 3 | Restore heist register — a genuine skill-based set-piece for Boiler/Scout/Lace under visible pressure; crew earns the exit before Adalemo's mercy | **NEW #19 "What the Building Knew"** (SortKey 470, set-piece length ~1100 words) — while Vox is inside the wall, the four-person PEREGRINE stack breaches the north corridor Rook failed to seal (#4972). Boiler collapses the compromised stairwell junction on a charge he's had planted since Act One (paying off "he is doing something with a building... it is relevant," bible §4), using four months of structural knowledge; Scout runs the crawlers to peel two operators off the direct line while her own body sits exposed and nearly found; Lace, patched into PEREGRINE's channel by Vox before going under, impersonates a stand-down order to fracture the stack's cohesion the way she impersonated their commander in MxG. Three of four are delayed/redirected at real cost (Boiler takes debris, Scout is a half-second from being found, Lace's ruse nearly cracks). The fourth is the one who forces the blast door in #4972/#4839 — meaning PEREGRINE reaching the wall at all is the *residue* of a fight the crew mostly won, not a freebie. **REWRITE #4842** makes this explicit: Adalemo notes his team's losses before he offers the walk, so his mercy opens a door the crew has already kicked most of the way through. |
| 4 | Show Ohara's post-procedure condition on the page | **NEW #25 "What Was Left"** (SortKey 660) — Ohara asks "Did it open?", a beat later asks it again without registering she already had, and then catches the repetition herself — Stave's specific horror-image ("asked a question they'd already asked") happens in miniature and self-corrects, landing the stake as real cost without erasing her. **PATCH #4904** adds one paragraph confirming she's upright, walking, present for the barge extraction — closing the "never seen again on the page" gap the audit flagged. |
| 5 | Fix the #4902 ordering bug | See Beat Map above — **445**, ahead of #4916/#4972/#4838's PEREGRINE-contact beats. |
| 6 | Terminology — Husk/Shell/eject-inject-return; retire "Rider" for Exo/RFO/Jockey | Audited the delivered 24 beats: **zero violations found** (the existing text already avoids "Rider" and never needed Husk/Shell vocabulary since no one ejects/injects across bodies in this strand — Scout's QCE crawler work is described natively as "going in"/"coming back," which is bible-compliant). All new prose (Adalemo plant, set-piece, Ohara-condition beat) was written to the same standard — Scout is referred to only by the established idiom already in the strand, and "Exo" is used once in the new set-piece beat where her profession is named on the page for the first time in the strand. |

### Bonus fix folded into the same pass (not separately requested, but load-bearing for #1 and #3)

**Location contradiction, Boiler's building vs. "the substation."** While tracing the partition
reveal and staging the set-piece, a second continuity bug surfaced: beat #4901 (kept) has the crew
explicitly relocate the procedure to **Boiler's condemned building** for hardware-frequency reasons,
and #4902/#4842 already refer to it that way ("the feed that fed Stave's cabinet in Boiler's
building," #4842). But #4838, #4839, #4840, and #4972 as delivered still describe the climax site as
**"the substation"** — Stave's separate Z2 broker building from Act Two, complete with "transformer
bays" and "iron coffins." This isn't cosmetic: Requirement 3 depends on the climax happening inside
the building Boiler has been personally studying for four months, and Lock #8 ("Boiler's building is
load-bearing to the resolution... not just color") only pays off if that's actually where the fight
happens. Fixed by swapping the four stray "substation" passages in #4838/#4839/#4840/#4972 to
Boiler's-building-consistent language (freight/loading-bay imagery in place of transformer-room
imagery) while leaving every other line of those beats untouched. Flagged here as a judgment call
rather than a silent fix because it wasn't named in the brief.

### #4843 ("The Count") legibility fix (folded into Requirement 4's spirit, not separately requested)

The audit flags the capstone image — "Six. At the Sojourn it had been five." — as illegible: it never
says what's being counted, and it names a diner ("the Sojourn") that doesn't otherwise appear in NxR,
which collides with CxC's bible (§8) treating "NxR's Sojourn" as *this* diner. Rewritten so (a) the
unnamed Z1 diner is now explicitly named the Sojourn on the page, satisfying CxC's assumption, and
(b) the callback points at MxG's actual ending location (the diner near the Spine) instead of an
unestablished self-reference, and (c) what Rook is counting is made legible — not money, but the
number of people now carrying a piece of the truth about the twenty-one (herself, Vox, Lace, Stave,
Ohara, Adalemo — six, against the five who carried the Class V truth at the Spine). This directly
serves bible §8's requirement that "what is different is not just the amount."

## Judgment Calls

1. **Adalemo's planted search targets a specific name, "Sefi Okonkwo."** This is deliberately the
   same survivor CxC's bible (§4, "New" entities) gives "a name and a face" to. Using her name in NxR
   is a forward plant, not a contradiction — CxC hasn't shipped yet as prose, only as bible/entities,
   so there's no fact to conflict with, and it gives CxC's "Adalemo finishes crossing" arc a second,
   earlier data point beyond the bible's existing "I never read a forty-first name" seed. If this
   reads as over-reaching into CxC's territory, the name can be swapped for an unnamed placeholder
   without touching the beat's structure.
2. **The partition-date fix assumes "eighteen months ago" = creation and "two years back" = the
   Old Harbor barge**, six months apart, with Ohara building the inner (personal, unlicensed) layer
   shortly after the barge and wrapping the outer partition around it six months later when Axiom
   pressure mounted. This is a reading, not an invention — every other date in the delivered beats
   (the 18-month decay curve, the barge's "two years back," Stave's "eighteen months back" crack)
   is already consistent with it; only Ohara's one line in #4832 needed to change.
3. **The set-piece (#19) assumes Boiler pre-planted a demolition contingency during his four months
   in the building**, retroactively giving weight to the bible's "he is doing something with a
   building... it is relevant" (§4) beyond the frequency/room payoff #4901 already delivers. This is
   additive, not a rewrite of #4901 — it's a second, escalated use of the same planted fact.
4. **Scout's near-discovery in the set-piece is written as a genuine physical risk**, not just
   tension color, per MxG Lock #11 ("Scout's eyes are closed for the finale... this is the point") —
   she is maximally vulnerable in her body at exactly the moment she's doing the most important work,
   consistent with how MxG and the bible treat her QCE cost.
5. **Location fix leaves Stave's actual substation (Act Two, beats #4834/#4835/#4836) untouched** —
   only the Act Three climax-site mislabeling is corrected. Stave's building is a real, different,
   correctly-named place throughout.

## Files

- `plan.md` (this file)
- `beats/05-oharas-briefing.md` (PATCH, #4832)
- `beats/07-the-search-he-didnt-log.md` (NEW)
- `beats/15-the-two-hours-before.md` (PATCH/reorder, #4902)
- `beats/16-the-setup.md` (PATCH, #4838)
- `beats/18-the-door.md` (PATCH, #4972)
- `beats/19-what-the-building-knew.md` (NEW, set-piece)
- `beats/20-peregrine-closes.md` (PATCH, #4839)
- `beats/21-the-procedure.md` (PATCH, #4840)
- `beats/22-the-second-wall.md` (REWRITE, #4903)
- `beats/24-the-decision.md` (REWRITE, #4842)
- `beats/25-what-was-left.md` (NEW)
- `beats/26-the-barge.md` (PATCH, #4904)
- `beats/27-the-count.md` (REWRITE, #4843)

13 beat files covering 9 PATCH + 3 REWRITE + 3 NEW actions (one PATCH — #4902 — is reorder-only and
carries no prose change beyond the trims noted above; it is still included in full per the
deliverable spec). 14 beats are KEEP and are not reproduced as files.
