# Fix Plan — Vultures at the Door (VATD)

Slug: `vultures-at-the-door-019ec467` · Score: 91.0 · File-only pass, no DB writes performed.
Live beat table pulled read-only 2026-07-03. All beat edits below are proposals in this folder;
someone with write access applies them via the CLI (`ss --beat ...`), never raw SQL, per the
project's no-direct-SQL-delete rule and general DB-write discipline.

**Data-hygiene note (updates the audit's count):** the audit (2026-07-03) reported 49 total
beats / 21 disabled. A fresh pull today shows **51 total beats / 28 enabled / 23 disabled** —
two more disabled rows than the audit counted (`4858`, `4917`), both alternate/superseded
transition beats (see quarantine list below, items 1–2). Enabled count and reading order are
unchanged from the audit. Use `IsEnabled=1` for any future review/score/continuity pass; the
naive unfiltered query still silently includes 23 rows of draft material.

---

## Fix 1 — Reinstall scene: RE-ENABLE beat 3915 (patched), not a new beat

**Route chosen: RE-ENABLE with a patched frame**, not a new beat from scratch.

Reasoning: beat 3915 already contains the exact on-page procedure the bible requires — Daria
Holme's door, the twenty-six-minute operation, "You came back," Levin's flat "Change of heart."
That prose is on-voice and doesn't need reinventing. Its only real defect is the frame around
it:

- **Opening contradiction:** 3915 opens with an invented "06:40 inventory query... excised
  twelve weeks prior" trigger. The live decision beat (4912) already establishes a different,
  simpler origin: Tomas says "I pulled it this morning before we went to the Ward and never
  moved it. It's still viable." Re-enabling verbatim would give the kidney two contradictory
  backstories back to back.
- **Closing redundancy:** 3915's tail ("in the van... That's not in any playbook... Carrion's
  going to flag it eventually... Worth it? We'll see.") duplicates ground the live closing beat
  3885 already covers, better, with the photograph beat and the "three on the queue"
  continuation. Keeping both reads as the same conversation twice.

The patch (full text in `beats/reinstall-3915-patch.md`) replaces the opening with a short
depot stop that reuses the "REALLOCATE / RETURN TO ORIGINAL ACCOUNT" clerical-form beat — kept,
because it's a nice piece of the story's black-comedy procedure-not-speech register — but
re-anchors it to "this morning" instead of "twelve weeks," matching 4912 exactly. The tail is
cut at "the Wagon was waiting at the curb... engine running," which now flows directly into
3885's "In the Wagon, Levin drove, and for a while neither of them said anything" — one
continuous scene instead of two overlapping ones.

**Placement:** SortKey 875.0 — between beat 3884 (850.0) and beat 3885 (900.0). This keeps it
immediately adjacent to 3885, whose opening line is written to continue directly from a scene
where Tomas and Levin are already back in the Wagon together; that's the strongest textual
join available. (See note below on beat 3884 — a secondary observation, not part of this fix.)

**Secondary observation (not actioned, flagging only):** beat 3884 depicts Tomas riding the
Pulse train home *alone*, having "locked the Wagon and walked away," worried about "the viable
window, twelve more hours, maybe fourteen" — language that reads more naturally as happening
*before* he goes to Levin's stairwell (4912) than after. Its current placement (after 4912,
before 3885) was already slightly discontinuous before this fix — the live text jumps from
Tomas alone on foot straight to Levin driving the Wagon in 3885 with no bridge. Re-enabling
3915 between 3884 and 3885 does not create this tension, it was already there; it also doesn't
resolve it. Worth a look in a future pass, but it's outside this task's three named fixes and
touching an already-enabled beat's placement is a bigger edit than "small and surgical."

---

## Fix 2 — Timeline: line patch to beat 4859

**Patch:** "Three nights I have been finding that out." → "One night." (full rationale and
before/after in `beats/4859-timeline-patch.md`).

The loaner clock (72:00:00 at the shooting → 48 hours at the transplant), the wedding fixed for
"tomorrow" from the night of the shooting (beat 3873), and the bible's own framing ("spend 18
hours cleaning it up") all compress the crisis into one night and the dawn after. Ekow is
dispatched in parallel with the crew's shift the same night (beat 3916, 3:40 a.m.). "Three
nights" in 4859 is a hard number that falls outside that clock — not a hunter's stylistic
exaggeration, an actual contradiction.

**Likely origin, for context:** the disabled "Orlan Bek" alternate-subplot cluster (beats
3886–3903, see quarantine list) runs on its own three-day clock — "a three-day gap" (3902),
"the three days had a shape" (3903). "Three nights" reads like connective tissue that leaked
over from that cut draft rather than a deliberate choice for the live Do-yun timeline.

---

## Fix 3 — Name resolution: Ekow Ato / "Casimir" — ruling

**Ruling: the prose already resolves this correctly. No required prose patch.** An optional,
cheap insurance patch is provided but flagged non-essential (`beats/3916-name-gloss-patch-OPTIONAL.md`).

**What the prose actually does (verified beat-by-beat, occurrence counts of each name per
enabled beat):**

| Beat | Ekow | Casimir | POV |
|---|---|---|---|
| 3916 (dispatch) | 2 | 1 | Ekow's own — narration uses "Ekow"; the one "Casimir" is the Lotus woman's call-name for him, explicitly glossed on the page ("She called him Casimir. He had corrected her once, early... he had not corrected her again. The name cost him nothing. The name was a collar...") |
| 3917 (den read) | 0 | 0 | Ekow's own, pronoun-only |
| 3918 (interrogates Osei) | 1 | 0 | Ekow's own — "Ekow" |
| 3919 (stand-down) | 2 | 0 | Ekow's own — "Ekow" |
| 3882 (breakwater) | 2 | 0 | Ekow's own — "Ekow" |
| 3883 | 0 | 0 | Ekow's own, pronoun-only |
| 3879 (Osei's Full Repossession) | 0 | 1 | **Tomas's POV** — "Casimir - Tomas didn't have a name yet, only the shape of him" (narrator supplies the reader's label; explicitly flagged as a name Tomas doesn't actually possess) |
| 4859 (confrontation) | 0 | 7 | **Tomas's POV** throughout — "Casimir" |
| 3881 (staging the bedroom) | 0 | 6 | **Tomas's POV** throughout — "Casimir" |

The pattern is exact and consistent: **narration uses "Ekow" whenever the scene is filtered
through his own interiority, and "Casimir" whenever the scene is filtered through Tomas's (who
never learns the real name).** This is a deliberate, well-executed POV device, not an
accident — and it is dramatized on the page twice (3916's "the name was a collar" and 4859's
near-echo, "They call me Casimir. I stopped correcting them, because the name is a collar, and
a man can wear a collar and still be the thing inside it").

**Entity record check (read-only, `Entities` table):** there are, as the audit worried, **two
separate entity rows**:
- `D15E074B-92F0-4126-BD42-C3B2C778EC8A` — **Ekow Ato**, `character`, `canon`, active. Description:
  *"Ghanaian immigrant operative working in the GLMZ for the Lotus Syndicate under the work name
  'Casimir.' Real name: Ekow Ato — Ekow is a Fante day-name, Tuesday, The Ocean..."* — this
  record **already states the exact reconciliation the prose dramatizes**, word for word
  (day-name, Tuesday, ocean, work-name Casimir).
- `019EC6EF-F35F-7651-8EA1-6B736A4282D6` — **Casimir Mwamba**, `character`, `canon`, active, no
  description — an orphaned duplicate from the pre-RETCON version of this character, never
  archived or merged.

**Conclusion:** the prose is not the problem and does not need a structural fix — it is ahead
of both the bible and the entity table. Two things are stale and should be corrected, but both
are *outside* prose/beat scope (per this task's instruction to flag rather than patch when the
fix genuinely belongs elsewhere):

1. **`docs/nodes/VATD.md` §3** still headers the character "Casimir Mwamba (Congolese)" and
   carries a "⚠ NAME DISCREPANCY (resolve in back-check)" flag. This should be updated to
   **"Ekow Ato"** as the header name, with "Casimir" documented as the Lotus-imposed work-name
   already dramatized in beats 3916/3879/4859/3881 — matching the entity record verbatim. The
   discrepancy flag should be removed. (Not done in this pass — file-only fix scope for this
   task was the `fixes/VATD/` folder; flagging here for whoever applies the bible edit.)
2. **DB entity cleanup:** `Casimir Mwamba` (`019EC6EF-...`) is a dead duplicate and should be
   archived or merged into `Ekow Ato` (`D15E074B-...`) so no future entity lookup, mention-scan,
   or continuity check resolves this character to the wrong row. This is a DB write and out of
   scope for a files-only pass — flagging, not performing.

The optional prose patch (`3916-name-gloss-patch-OPTIONAL.md`) just pulls the day-name/"Ekow"
link one paragraph earlier for a first-time reader; it changes nothing structural and can be
skipped.

---

## Quarantine audit — 23 disabled beats

All beats below have `NodeBeats.IsEnabled = 0`. Confirmed by content read: **all 23 should stay
disabled**, with one becoming the target of Fix 1 (re-enable + patch, not "stay disabled" —
listed here for completeness, marked accordingly).

| # | Beat | SortKey | One-line description | Disposition |
|---|---|---|---|---|
| 1 | 4858 | 425.0 | Alternate driving-transition beat after the Reuben Sclose pickup (Levin talking as the Tears thin); superseded by the live 3875→4974→3876 transition. | STAY DISABLED (redundant draft) |
| 2 | 4917 | 512.5 | Alternate/duplicate "Full Repossession" order-notification beat; superseded by enabled 3878's version. | STAY DISABLED (redundant draft) |
| 3 | 3886 | 950.0 | Opens the alternate "Orlan Bek" subplot — a pancreatic-filter recall uncovers a family hiding Orlan Bek's body for two weeks. Entirely separate incident from the canon Han Do-yun shooting. | STAY DISABLED (whole alternate-continuity subplot) |
| 4 | 3887 | 1000.0 | Orlan Bek subplot cont. — riding down with the cooler, the daughter in the kitchen. | STAY DISABLED |
| 5 | 3888 | 1050.0 | Alternate domestic phone-call beat (homework/therapy check-in) set inside the Orlan Bek timeline. | STAY DISABLED (duplicate of function served elsewhere) |
| 6 | 3889 | 1100.0 | **LOCK VIOLATION.** Levin's own financed-kidney backstory — a photo of him post-transplant at 31; "he also knew exactly what the kidney in that photo had cost." Directly contradicts Character Lock #3 ("NO financed-kidney past... struck"). | **STAY DISABLED — never re-enable.** This is the beat the audit flagged as a live lock violation sitting in the table. |
| 7 | 3890 | 1150.0 | Orlan Bek subplot cont. — the job call-in (pancreatic filter recall) that leads to the alternate shooting. | STAY DISABLED |
| 8 | 3891 | 1200.0 | Orlan Bek subplot — the alternate second-shooting incident itself (brother with a stun baton, then a second man forces Tomas to fire). A wholly different inciting-incident draft, incompatible with the canon shooting mechanism and its locks. | STAY DISABLED |
| 9 | 3913 | 1225.0 | Orlan Bek subplot — the sister-witness insists on coming along; the witness-management thread begins. | STAY DISABLED |
| 10 | 3892 | 1250.0 | Orlan Bek subplot — the crew's risk calculus about reporting a "live recovery"; names Devorah Bek as the Lotus coordinator/mother. | STAY DISABLED |
| 11 | 3893 | 1300.0 | Orlan Bek subplot — an alternate call to Dr. Yuen about the alternate victim's heart ("three-week window"), not Do-yun's. | STAY DISABLED (duplicate/alternate of canon Yuen calls) |
| 12 | 3914 | 1325.0 | Orlan Bek subplot — paying off the witness fixer "Solange" to manage the call-in. This is the "Solange witness-blackmail thread" the audit names. | STAY DISABLED |
| 13 | 3894 | 1350.0 | Orlan Bek subplot — driving north with Orlan Bek's body in cold storage, Levin monitoring the readouts. | STAY DISABLED |
| 14 | 3901 | 1375.0 | Orlan Bek subplot — stakeout outside an alternate off-books clinic (converted loading bay); competes with the canon transplant-clinic scene (enabled 3880). | STAY DISABLED |
| 15 | 3902 | 1387.5 | Orlan Bek subplot — Dr. Yuen delivers the post-op alternate patient ("a three-day gap") plus a second, separate donor-death scene. Runs on a three-day clock, not the canon 18/72-hour clock. | STAY DISABLED |
| 16 | 3903 | 1393.75 | Orlan Bek subplot — an explicit "the three days had a shape" recap. Likely the source of the drifted "Three nights" line patched in Fix 2. | STAY DISABLED |
| 17 | 3895 | 1400.0 | Alternate epilogue arc — a new, unrelated job "four weeks later" (corneal lens recall); a competing weeks-later timeframe for the ending, incompatible with the canon same-dawn reinstall. | STAY DISABLED |
| 18 | 3896 | 1450.0 | Alternate epilogue arc — Daria Holme spotted in the laundry room, four weeks on; an earlier/rejected way of revisiting her building. | STAY DISABLED (superseded by Fix 1's same-night reinstall) |
| 19 | 3897 | 1500.0 | Alternate epilogue arc — an unrelated dialysis-equipment collection, filler for the four-weeks-later timeframe. | STAY DISABLED |
| 20 | 3898 | 1550.0 | Alternate epilogue arc — Levin secures the cooler, Tomas sees Daria Holme in the mirror as they drive off (four-weeks-later version). | STAY DISABLED |
| 21 | 3899 | 1600.0 | Alternate domestic beat — the "Sunday still?" call about Paz's therapy schedule; a duplicate/alternate placement of a Paz-math beat referenced in the bible's original Act V outline. | STAY DISABLED (redundant with domestic beats already in the enabled cut, e.g. 4911) |
| 22 | 3915 | 1625.0 → 875.0 | **The reinstall procedure** — the only on-page version of the "Change of heart" scene. Contains a "excised twelve weeks prior" detail contradicting live beat 4912's "this morning." | **RE-ENABLE, patched — see Fix 1.** Not staying disabled. |
| 23 | 3900 | 1650.0 | Alternate/earlier draft of the closing photograph + "the Wagon moved north" beat. | STAY DISABLED (superseded by the polished version now in enabled 3885) |

**Summary:** 22 of 23 disabled beats are confirmed dead draft material (two separate abandoned
continuities — the "Orlan Bek/Devorah Bek/Solange" alternate inciting-incident subplot spanning
13 beats on its own three-day clock, plus a "four weeks later" alternate epilogue spanning 5
beats, plus 4 miscellaneous superseded/duplicate transition beats). One (3889) is a confirmed
character-lock violation that must never be re-enabled. One (3915) is the fix target and moves
from "quarantined" to "re-enabled, patched." No other action needed on this list; nothing here
requires deletion (per the project's no-direct-SQL-delete rule, quarantine-in-place via
`IsEnabled=0` is already the correct state for 22 of these 23).
