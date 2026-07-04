# Round-2 Repair — Death Whispers in a Cat's Ear (DWIACE)

Slug: `death-whispers-in-a-cats-ear-019ec3fe` | NodeId: `019EC3FE-4AA7-75B8-915B-4222005F2E1C`
Trigger: 42-ballot review flagged "Beats 97-99" (SortKey read order) as orphaned/drafting
debris with broken spatial/temporal continuity (7 ballots).

## Diagnosis

Positions 92-105 (SortKey order) all sit inside "What the Room Holds" (Tamsin solo, reading
Jace Dalton's apartment). Prior to this fix the sequence was:

- 95 (Beat 3399, SK 3900) - enters apartment, crosses threshold.
- 96 (Beat 3402, SK 4000) - reads room, finds folded paper, "She crossed to the counter and
  picked up the folded paper."
- **97 (Beat 3404, SK 4100)** - "This room smelled like neither. Through the door she felt
  warmth... She opened the door the way she'd entered the building..." - describes *entering*
  a room, contradicting 96 where she is already inside holding the paper.
- **98 (Beat 3407, SK 4200)** - "The room had been waiting for someone to ask it a question...
  She stood in the doorway..." - still describes arrival/threshold, not the paper-reading
  already underway.
- **99 (Beat 3410, SK 4300)** - "No fear. She checked twice - once at the door, once when she
  crossed to the center... In a missing girl's bedroom..." - references a "missing girl's
  bedroom," which does not match the scene (Jace Dalton, an adult man who left deliberately,
  per beats 95/96/100+). No missing-girl case appears anywhere else in the book's enabled
  beats (checked via full-text search).
- 100 (Beat 3413, SK 4400) - "The paper held two paragraphs..." - directly and seamlessly
  continues from beat 96's "picked up the folded paper."

**Verdict: beats 3404/3407/3410 are true drafting debris** - an abandoned alternate-draft
fragment of a room-entry beat (written for a different, never-realized "missing girl" scene,
or an early draft pass of this scene's opening) that was left live in `NodeBeats` and got
threaded between 96 and 100 by SortKey coincidence. Removing them makes 96 -> 100 a direct,
clean continuation ("picked up the folded paper" -> "The paper held two paragraphs").

**Not caused by the 2026-07-03 SortKey relink.** Checked all three BeatIds against
`sortkey-backup.txt` (the 135 beats moved by the Celeste-interleave fix) - none of the three
appear there, and neither do their neighbors (95/96/100). "What the Room Holds" was one of
the chapters the round-1 plan explicitly left untouched (aside from relocating orphan beat
4392 elsewhere). The three SortKeys (4100/4200/4300) are a clean, evenly-spaced sequence
contiguous with their neighbors (3900/4000/4400), consistent with debris inserted at
authoring time, not a relink artifact.

**No unique plant/payoff content lost.** Full-text search across all enabled DWIACE beats for
"missing girl" and the fragment's other distinctive phrases found no other occurrence and no
downstream reference to a missing-girl case. The three beats carry no named entities, no case
facts, and no plot information not already present elsewhere in the scene.

## Fix applied

Soft-disabled (never deleted) the three beats via `NodeBeats.IsEnabled = 0`:

| BeatId | Beats.Number | Old SortKey |
|---|---|---|
| 019EC40C-966A-7DAD-9D05-690A0B13E55A | 3404 | 4100.0 |
| 019EC40C-A715-712B-9C5C-0A23435040C6 | 3407 | 4200.0 |
| 019EC40C-B6D0-7CD5-901F-D7EE7AD75BDF | 3410 | 4300.0 |

Pre-change snapshot: `sortkey-backup-2.txt` (this folder). Restore command included there.

No SortKey changes were needed - the surrounding beats (96 and 100) were already correctly
ordered and required no re-seating; disabling the three intruders alone restores continuity.

## Secondary findings (diagnosed, not patched)

**Position 333 (Beat 3708, SK 20100.0), "The Convergence":** Analog says "I want to say
something about the search." Read alongside neighbors (rows 328-338) this is a normal
ensemble-scene beat - Analog's contribution before Rennick reassigns tasks and the
cat-ears-victim call comes in. Not a verbatim duplicate of anything nearby or elsewhere.
Reads as a legitimate, if perhaps slow, beat in a talky sequence. Left unpatched per
instructions (act only on verbatim-duplicate class problems).

**Positions 372/374/375 (Beats 3741/3758/3762, SK 24000/24300/24400), "The Same Cold":**
Part of a longer escalating litany (368-376) reading a dead girl's final moments - "not
ending" (372) -> "near the end, the place changed" (373) -> "a moment of agreement... a
single unhesitating yes" (374) -> "then the dying... no fear and no struggle" (375) -> "she
came up out of it" (376). Each beat covers a distinct sub-beat of the read (anticipation,
environment shift, consent, death itself, return) - not verbatim or near-verbatim repeats of
each other. This reads as a deliberate rhetorical/pacing device (short fragment-beats
stacking for weight), which is very likely also the source of the standalone "convergence
scene runs long" complaint (4 ballots) - the device is legitimate craft but produces a
noticeably slower page-count-per-plot-beat ratio through this stretch. Not a data bug;
left for an author/editor pacing call rather than patched here, per the "verbatim-duplicate
only" instruction.

## Verification

- Positions 92-105 re-read: beat 96 ("...picked up the folded paper.") now flows directly
  into beat 97 ("The paper held two paragraphs...") with no jump. No location/time break
  remains in this window.
- Total enabled beats: 564 -> **561** (three disabled).
- Duplicate SortKeys within node: **0 rows** (none).
- Chapter-start order unchanged: Cel -> The Intake -> What the Room Holds -> What She Asked
  For -> What She Wouldn't Do -> Clean Sharps -> The Ghost Period -> The Convergence -> The
  Same Cold -> No Signal -> The Surfacing -> Voluntary Recall (12/12 matches round-1's
  verified order).
