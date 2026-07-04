# HOLDOUT Fix — Crimson & Chrome (CxC), Round 3

Files only. No database writes. This is a structural restructure of a single beat (Beat 12 /
Number 4855 / "The Executive" — the Anneke Oyelowo confrontation), which drew 17/20 "over-explains
stakes / resolves too clean" after round 2's −32% trim. Also disposes of the residual Beat 4
(4847) "backstory dump" complaint (3/20).

Read against: `docs/nodes/CxC.md`, `audit-outlines-2026-07-03/fixes/ROOK/plan.md` + `round2/*`
(round-1/round-2 rulings and fact verification), and the live `Beats`/`NodeBeats` rows for
`marrow-chrome-019f0968`, confirmed read-only via sqlcmd on 2026-07-03 (Beat 4855, SortKey 600.0,
IsEnabled 1, live text opening matches `round2/cxc-12-the-executive.txt` verbatim — that file is
the current canonical text, not a stale draft).

---

## Diagnosis

The 32% trim already done to Beat 4855 cut length but not the *mechanism*. The scene's climax is
still a debate: Anneke delivers a thirty-years-of-the-honest-room thesis, Rook rebuts it with an
equally verbal thesis of her own ("you believe it's quiet"), and the standoff resolves when
Anneke's conscience wins an argument — nobody's hand is forced, nothing changes state on the page,
and Rook walks off unmarked. That is what "over-explains stakes" and "resolves too clean" are
both pointing at: the same root cause. Trimming words without changing the mechanism just makes
the same monologue shorter; it doesn't stop it from being a monologue, and it doesn't stop the
resolution from being "I convinced her."

## Option analysis

**Option A — SPLIT into two beats around an action bar**, with the guard pivot / west-core
countdown promoted from something Rook narrates in the past tense to a live, on-screen event
between the halves.

Rejected. The guard pivot is already established, in the existing canonical sequence, as
something that happened *during* Beat 11 (Vox draws the PEREGRINE security off Sefi's column
toward the east fire core) — Beat 12 opens *after* that, with "the count already walking down" (a
rescue already in motion). Re-staging that pivot as a live event inside Beat 12 means either (a)
contradicting Beat 11's own resolution, or (b) inventing a second, redundant guard-response track
solely to have something to split around. Both cost more than the complaint is worth, and (b)
requires a new BeatId + a NodeBeats row + a SortKey between 600.0 and 650.0 for someone with DB
access to wire in later — exactly the kind of operational overhead a files-only pass should avoid
handing off if a cheaper fix exists inside the current beat.

**Option B — CONVERT: keep one beat, turn Anneke's stakes-explanation into staged behavior, drop
dialogue to load-bearing lines. CHOSEN.**

Same BeatId (`019F096A-DE7B-712B-892F-2974FE7B467B`), same Number (4855), same SortKey (600.0). No
`NodeBeats` change of any kind is required — this a pure text patch. It fixes both halves of the
17/20 complaint with one mechanism change:

- *Over-explains stakes:* the "we pay for a thing and someone else receives it... I have furnished
  it carefully for thirty years... you will make me say it, in front of myself" monologue is cut
  to two short volleys ("Everyone under this roof prices... Don't stand in my gap and call yours
  clean." / "It's not clean. It's just mine. And it's not for sale.") and "you believe it's quiet"
  moves from spoken dialogue to an unspoken interior line Rook doesn't say out loud. The theme
  survives; the lecture doesn't.
- *Resolves too clean:* the "not faster than a word... I have not finished the word" standoff used
  to resolve because Anneke *decided* not to finish it — a debate won on the merits. In the patch,
  Anneke actually starts saying the word (her mouth opens on the first syllable — an action bar
  marks the hard cut into real time) and Rook has to physically stop it: she takes the Reibo off
  aim, closes to a range where the flechette is useless, and rips the tablet — the harvest guard's
  open channel — out of Anneke's hands before the word lands. The cracked tablet glass opens
  Rook's palm. The word never finishes, but not because Anneke chose mercy; it finishes-not
  because there was nothing left in her hand to say it with. Anneke's hesitation ("I would have
  finished it") only surfaces *after* she's been physically disarmed — a forced pause, not a won
  argument — and Rook leaves the confrontation with a bleeding hand she carries into the next two
  beats.

This is the cheaper option: it stays inside the existing 14-beat spine (bible §6, locked), touches
no SortKeys, needs no new DB rows, and directly answers both halves of the panel's single
complaint with one causal change (talk → forced physical interruption) rather than two separate
fixes.

**Option C** — considered and folded into B: rather than inventing a wholly different device, the
cheapest version of "demonstrate the stakes instead of stating them" was to reuse the mechanism
the beat already has (the live guard channel on Anneke's own tablet) as the *object* of the
physical action, instead of inventing a new one. No separate Option C is needed.

---

## Beat map — before / after

| Spine # | Number | BeatId | Title | SortKey | Change |
|---|---|---|---|---|---|
| 10 | 4853 | 019F096A-B08C-... | The Run In | 500.0 | unchanged |
| 11 | 4854 | 019F096A-C84E-... | Vox Steps Into the Light | 550.0 | unchanged |
| 12 | 4855 | 019F096A-DE7B-712B-892F-2974FE7B467B | The Executive | 600.0 | **RESTRUCTURED** (Option B — see `beats/cxc-12-the-executive.md`) |
| 13 | 4856 | 019F096A-F6B7-... | The Burning-Down | 650.0 | unchanged (reviewed for a consequence hook — see below; none inserted) |
| 14 | 4857 | 019F096B-238D-... | The Count, With Names | 700.0 | **ONE-CLAUSE PATCH** (see `beats/cxc-14-the-count-with-names.md`) |

No new BeatIds, no new Numbers, no SortKey changes, no `NodeBeats` inserts/updates. The 14-beat
canonical spine is unchanged in shape.

### Consequence-thread check (13 vs. 14)

Beat 13 ("The Burning-Down") runs close-third on Boiler and briefly Adalemo/Scout; Rook does not
appear in it on the page. Inserting Rook's cut hand there would mean introducing her POV into a
beat that currently, deliberately, doesn't carry it — a bigger change than the task calls for.
Beat 14 ("The Count, With Names") is Rook-POV at the Sojourn and already has the exact hook to
carry it: the existing line "She put her hand on the counter, not the gun" is the same hand. One
clause was added there (see `beats/cxc-14-the-count-with-names.md`) — the cut from Beat 12, closed
now, left to scar rather than synth-patched smooth, because "some lines she'd decided were worth
keeping." Nothing else in Beat 14 changes. Beat 13 is untouched.

---

## Facts checklist (round-2 verification list) — survival status

| Fact | Status in round-3 text |
|---|---|
| East core (Anneke's descent route) | Preserved verbatim, opening line |
| Unarmed | Preserved verbatim ("She was not armed") |
| 21-row reconcile | Preserved verbatim |
| Reibo draw | Preserved (full draw description kept) |
| Amber Okonkwo row | Preserved verbatim (`Okonkwo, S.`) |
| Flechette-vs-word | Preserved — the race logic (a spoken word beats a trigger-pull) is the
  hinge of the new action-bar sequence, not removed |
| Count to twenty-two | Preserved (moved up two lines to sit directly against the drawn gun,
  where it was momentarily dropped in the trimmed round-2 text's paragraph order) |
| Guard pivot / west core | Preserved verbatim ("You pivoted your guard off Sefi's column to
  chase one loud voice up the west core...") |
| "Reconcile that on the way up" | Preserved verbatim as the beat's final line |
| Tidewell facts (21 in / 14 out / 7 lost / Wennick / ~2206) | Not touched by this pass — they
  live in Beats 1–4 (4844–4847), untouched in round 3 |
| Ending beat ("Thirty-one") | Untouched in substance; only the one clause above is added |

No fact from the checklist was dropped, renamed, or contradicted.

---

## Residual Beat 4 (4847) — "backstory dump" complaint (3/20)

Reviewed `round2/cxc-04-the-seam-in-the-trilogy.txt` in full. The paragraph carrying the Tidewell
confession ("Not Axiom. Tidewell... Paid by the head. I ran the count. Cashed it, called it a good
outcome, went home... The seven weren't the cost. They were the product.") is already written in
short, fragmented, present-tense-adjacent clauses explicitly framed as Rook's "bad-read voice" —
it is not an indulgent block of exposition, it is a controlled compression that mimics how Rook
delivers a number she doesn't want to be true. At 3/20 (a minor residual, not a majority
complaint), and given the paragraph is already doing the compression work a further cut would be
aiming at, **no further edit is made**. Per the instruction ("otherwise leave"), Beat 4847 is left
as-is from round 2.

---

## Flags for the user

- Beat 4855's restructure gives Rook a small, specific, visible wound (a palm cut from the
  tablet's broken glass) that did not exist before. It is load-bearing for the "not clean"
  resolution the panel wants, and it is now referenced once more in Beat 4857 (round 3). If this
  wound is unwanted for continuity reasons (e.g., conflicts with a physical-condition detail
  elsewhere in the trilogy not surfaced by this review), the surgical seams are narrow: one
  paragraph in 4855 (the tablet-grab/glass-cut passage) and one clause in 4857 — easy to re-word
  or remove without touching anything else.
- This pass did not re-run the review panel. Per the task, these are file-only deliverables for
  someone with DB write access to apply (`Beats.Text` update for 4855 and 4857 only — no
  `NodeBeats` changes needed) and then re-score.
