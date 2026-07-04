# Reassembly Plan — Death Whispers in a Cat's Ear (DWIACE)

Slug: `death-whispers-in-a-cats-ear-019ec3fe` | 564 enabled beats | DB score 90.6
Plan date: 2026-07-03. Read-only DB investigation; no writes performed. Files only.

## 0. Correction to the audit's premise (load-bearing for everything below)

The audit (`audit-outlines-2026-07-03/DWIACE.md`) assembled its chapter outline by
`ORDER BY Beats.Number`. **`Beats.Number` is a global, cross-strand auto-increment identity
column, not a reading-order field.** Confirmed by inspecting `Beats` rows with `Number` in
4850–5050: the neighbors of the orphan beat 4968 belong to five *different* strands
(Rook/Crimson & Chrome, Kyle/Bushido Coda) with zero NodeBeats links to DWIACE. Number order
across a single node is not guaranteed contiguous — beats get their Number at creation time,
and later editing passes (expansions, backfills) insert beats with much higher Numbers into
early story positions.

**The actual assembly/read/scoring/export order is `NodeBeats.SortKey`.** Confirmed in code:
`NodeReviewService.cs` (`.OrderBy(sb => sb.SortKey)`, used for both review scoring and the
"reading order" export path) and the same pattern in `Node.razor`, `PrintNodeCli`, and every
other node-consuming service. This plan uses `SortKey`, not `Number`, as ground truth
throughout.

Re-deriving the chapter list by `SortKey` (window-function grouping on `IsChapterStart`)
produces a **materially different and much better-formed picture** than the audit's
Number-ordered one:

| # | Chapter (SortKey order) | Beats | SortKey range |
|---|---|---|---|
| 1 | The Intake | 35 | 100.0 – 3500.0 |
| 2 | What the Room Holds | 55 | 3600.0 – 8900.0 |
| 3 | What She Wouldn't Do | 44 | 9000.0 – 13400.0 |
| 4 | Clean Sharps | 43 | 13500.0 – 17800.0 |
| 5 | The Convergence | 49 | 17900.0 – 22600.0 |
| 6 | The Same Cold | 37 | 22700.0 – 26400.0 |
| 7 | No Signal | 58 | 26500.0 – 32200.0 |
| 8 | The Surfacing | 65 | 32300.0 – 38700.0 |
| 9 | Voluntary Recall | 45 | 38800.0 – 43300.0 |
| 10 | Cel | 56 | 43400.0 – 48900.0 |
| 11 | What She Asked For | 39 | 49000.0 – 52900.0 |
| 12 | The Ghost Period | 38 | 53000.0 – 56700.0 |

Total 564. This matches the bible's non-Celeste chapter order exactly (Intake → Room Holds →
Wouldn't Do → Clean Sharps → Convergence → Same Cold → No Signal → Surfacing → Voluntary
Recall) once the three Celeste chapters are removed from consideration. **The only real
structural defect confirmed by this plan is Law #0** — Cel / What She Asked For / The Ghost
Period sit at the tail (positions 10–12) instead of interleaved at 1/4/7 — plus two small
linkage bugs (§3, §4 below). Findings #2 and #3 in the audit (climax bisection, 44%-of-book
imbalance) do **not** hold under the true read order; see §2.

## 1. Celeste interleave plan (Task 1)

Bible §SS-DWIACE-6 12-chapter order, with Celeste slots at 1/4/7, matches the current
non-Celeste ordering exactly once Cel/What-She-Asked-For/Ghost-Period are lifted out and
reinserted. No other chapter needs to move.

**New order:**

1. **Cel** (was position 10, 56 beats)
2. The Intake (unchanged)
3. What the Room Holds (unchanged)
4. **What She Asked For** (was position 11, 39 beats) + relocated orphan beat 4392 (see below)
5. What She Wouldn't Do (unchanged)
6. Clean Sharps (unchanged)
7. **The Ghost Period** (was position 12, 38 beats)
8. The Convergence (unchanged)
9. The Same Cold (unchanged)
10. No Signal (unchanged)
11. The Surfacing (unchanged)
12. Voluntary Recall (unchanged)

**134 beats get new SortKeys** (56 + 40 + 38). The other 430 beats are untouched.

### Early-knowledge check

- **Cel → position 1**: opening beat (4052, "The room is hers and has not been hers for a
  while. She is nineteen...") is pure establishing description — no investigator references.
  Chapter ends cleanly (beat 3958, "for the first time in eight months, not in her parents'
  house") flowing straight into What She Asked For's first morning. Safe to open the book.
- **What She Asked For → position 4** and **The Ghost Period → position 7**: scanned every
  beat in both ranges for `Rennick|Tamsin|Teller|Corvin|Analog|Voss|Sol|Castellanos|investigat`
  — one hit, a false positive (lowercase "analog setup" describing paper-and-pen interview
  gear, not the character Voss/"Analog"). **No forward-references to the investigation found.**
  This is expected — the bible's Illusion Requirement deliberately keeps Celeste's POV
  hermetic from the detectives' information.
- **Flagged anomaly (not fixed here, needs an author call):** beats 4055 and 4054 (Pulse
  terminus arrival / chamber-seal departure) are interleaved by SortKey into the *middle* of
  what reads as a separate, more granular walk-to-the-station-and-ride sequence (beats
  3926–3944, 4037–3944 continuing after). Beat 4055 has her arriving at the terminus and
  beat 4054 has the pod already sealed and departing (SortKey rows 444–445), then row 446
  rewinds to "Twelve minutes to the Pulse station at her pace" and re-walks the same trip in
  more detail through Logan Square (rows 446–460). Read straight through, she appears to
  depart twice. This looks like two draft passes of the same scene (a condensed version and
  an expanded walk-and-ride version) both left live in NodeBeats. Recommend an editor read
  both versions and either delete the condensed pair (4054/4055) or move them to open the
  chapter as a flash-forward. Not touched in this plan — resolving it is a prose/continuity
  call, not an assembly one, and I don't have enough confidence to pick a side blind.

### Orphan beat 4392 (relocated as part of this task)

Beat 4392 ("Cermak corridor, same afternoon... *There's a woman in the room*... She'll find
the cold... Rennick Investigations. She'd left it exactly where it was.") is currently
misfiled at SortKey 8250.0, inside "What the Room Holds" (chapter 3, Tamsin's Evanston
bedroom scene) — a beat about Celeste already at large in the city, aware of the detectives,
does not belong inside the office/bedroom chapter. Content ("same afternoon" of the departure
day; the voice pre-announcing "a woman... She reads rooms") places it as Celeste's first full
day away, immediately before her settling-in beats — i.e., the front of **What She Asked
For**. Relocated to the head of that block (new SortKey 8902.0, ahead of beat 3959).

## 2. Climax chapter boundary (Task 2) — no boundary change needed

The audit's finding #2 (the 41%→89% Scatter countdown "bisected" by the Voluntary
Recall/Same Cold boundary at Beat.Number 3666/3667) and finding #3 (Same Cold swallowing 249
beats / 44% of the book) are **artifacts of Number-ordering and do not describe the actual
assembled book.**

Traced by true SortKey order:
- The entire status-panel countdown — 41% (beat 3666) → 48% → 54% → 61% → 67% → 72% → 78% →
  84% → freezes dark at 89% (beat 3789) — plus its immediate aftermath (Celeste's reaction,
  Rennick's cost accounting) all sit **inside one chapter, "The Surfacing"** (SortKey rows
  322–386), which opens with its own scene header ("EP9 - ensemble - under-city staging room,
  Cermak sub-level... procedure window: 22 minutes") and is exactly what the bible describes
  ("The Surfacing (EP9, Ensemble climax approach)"). It is not split by any other chapter.
- **"Voluntary Recall"** (rows 387–431) begins cleanly and separately with "The call from
  Arcturus came in at 06:14" — the corpo-burial aftermath, matching its bible description. It
  ends on a genuinely excellent closing beat (3867: new client on the stairs, "The case would
  come in cold. They would read it anyway." — the agency motto, closing the loop).
- **"The Same Cold"** (rows 227–263, 37 beats — not 249) opens with its own clean scene header
  ("EP7 - Tamsin Yabe solo, Teller confirms - Cermak corridor, unlicensed south") and is
  entirely Tamsin's death-scene investigation, exactly as billed. It ends cleanly on her text
  to Rennick ("Same cold as Celeste's room - it's one thing, many faces").
- The tail of **"The Convergence"** (rows 178–226, 49 beats) — which is where the audit's
  Number-order analysis picked up scattered mid-3600s/3700s/3800s beats and mistook them for
  chapter bleed — is in fact one continuous, well-built ensemble scene: the four detectives
  present their reads, a possible-Celeste body scare sends Tamsin running to a scene, she
  confirms it's a second Jane Doe, and the chapter ends on the text-message tease that "The
  Same Cold" then dramatizes in full (a deliberate tell-then-show flashback structure, not a
  scrambled cut).
- **"No Signal"** (rows 264–321, 58 beats) opens cleanly with its own header ("EP8 - Voss
  Caldera, solo... Day 5 of the Hartley case").

Chapter sizes (37/58/65/45/49 beats) are proportionate; nothing swallows 44% of the book.
**No `IsChapterStart` changes and no chapter-title changes are needed in this section.**

*Minor ancillary observation (not part of the 4 tasks, not patched):* three of the twelve
chapters ("The Same Cold," "No Signal," "The Surfacing") open with an inline
"EP# - POV - location - detail" production-slug line baked directly into the beat text; the
other nine chapters don't have this. Worth a consistency pass later (either add it everywhere
or strip it from these three before final export) but it doesn't affect read order.

## 3. Sol Castellanos "brother" contradiction (Task 3) — flagged, not patched

Bible §SS-DWIACE-2 / narrative lock #4: "Sol Castellanos... same want as Celeste (reach the
dead boyfriend)... Sol and Celeste are mirrors."

Beat **3800** (Teller/Tamsin, phone): *"You have a name," he says. "Mateo," she says. "A
brother, not a boyfriend. Dead before her. She died reaching for him."*

**Checked for corroboration — the "brother" version is load-bearing in four separate beats,
including the reveal-pivot beat itself:**
- **3800** — the name reveal, quoted above.
- **3829** — "Sol had gone toward a brother and what had arrived was dissemination."
- **3842** — the reveal-pivot beat itself: *"Operative entity: E.L.F., predatory,
  grief-targeting... Confirmed two faces - Jace Dalton (Hartley), Mateo Castellanos
  (Castellanos) - one author, per Teller's cadence analysis. Proposed tag: the Tributary."*
- **3860** — "The cold had given her a brother to wal[k toward]."

Per the task brief's own instruction ("if the brother version is load-bearing elsewhere, flag
it instead of patching blind") — **this is exactly that case.** Rewriting "brother" to
"boyfriend" would require touching the reveal-pivot beat (3842) itself and would weaken it:
the actual reveal as written is *the Tributary is relationship-agnostic — it wears whoever a
raw grief-channel needs, a boyfriend for one victim and a brother for another* — which is a
sharper, more general statement of the predator's nature than "it always impersonates a dead
boyfriend." The prose is deliberate and internally consistent across four beats; the bible
line is almost certainly the stale one (pre-dating a later prose revision that generalized the
predation logic).

**Recommendation:** Amend `docs/nodes/DWIACE.md` §SS-DWIACE-2 and narrative lock #4 to read
something like: *"Sol = same want, poorer options, earlier victim — reaching a specific dead
person (a brother, not a boyfriend) via the same raw grief-channel exploit. The mirror is the
predation pattern, not the relationship type."* This plan does not make that edit directly
(out of the file scope given), but recommends it over touching the prose.

**Patch file provided as a low-risk safety net, not the primary recommendation:** see
`beats/sol-mirror-clarify-3842.md` — a two-line addition to beat 3842 that makes the
relationship-agnostic mirror logic explicit for a reader/reviewer, in case the bible is not
amended. This does **not** change "brother" to "boyfriend" anywhere.

## 4. Finale linkage audit (Task 4) — finale exists, is complete, is simply mis-sorted

Beat 4968 is not evidence of a missing ending. Read in full, it's a complete, polished scene:
Rennick calls the mod-clinic that installed Celeste's cat-ear mod to pull her intake record;
a clerk starts reading it aloud; a Meridian "legal preservation order" banner slams down and
kills the record mid-sentence while Rennick is on the line watching it happen in real time.
It ends on his signature tell ("He turned the cup a quarter-turn on the desk, and stopped.")
— a complete, well-crafted beat, not a fragment.

**Checked for a larger unlinked finale sequence** — searched `Beats` for `BeatTitle =
'Voluntary Recall'` and for text matching `%chair in a tower%` / `%write this one%` across the
*entire* database, not just this node: no other candidate beats exist, linked or unlinked.
Also checked all `Beats.Number` 4850–5050 (the "600-Number gap" the audit flagged) — every
other beat in that range belongs to unrelated strands (Rook/Crimson & Chrome, Kyle/Bushido
Coda) with zero NodeBeats rows for DWIACE. Beat.Number is a global cross-strand counter, so
number-adjacency was never meaningful evidence here.

**Verdict: the finale is not missing. It is one single beat, mis-sorted.** Its content (a
last, defiant investigative act, corpo burial closing over him in real time) fits inside
**Voluntary Recall**, immediately after the team's post-raid regrouping and before Rennick
writes the official case-file close-out — i.e., between beat 3832 ("I found the stable
signature") and beat 3844 ("He opened the case record for the final time and wrote the
close-out entry"). It should NOT go after the book's actual final beat (3867, the new client
on the stairs) — that ending is already complete and shouldn't be followed by anything.

Currently 4968 sits at SortKey 22550.0, squeezed with a half-step between two early-investigation
beats inside **The Convergence** (3838 at 22500.0, 3847 at 22600.0) — an unrelated chapter,
five chapters before "Voluntary Recall" in read order. This is a pure SortKey placement bug.
Fix: move it to 42600.0 (there is already a clean 200-point gap between 3832 at 42500.0 and
3844 at 42700.0 inside Voluntary Recall — no renumbering of neighbors required).

## 5. SQL — ready to execute (UPDATE only, no DELETE/INSERT)

Four independent `UPDATE ... FROM (VALUES ...)` statements. Each is self-contained and can be
run in any order. All target `NodeBeats.SortKey` only, scoped to this node's `BeatId`s, which
are already unique to this book (Beats.Id is a global PK; the `WHERE nb.NodeId = ...` guard is
included anyway for safety).

```sql
DECLARE @NodeId UNIQUEIDENTIFIER = (SELECT Id FROM Nodes WHERE Slug = 'death-whispers-in-a-cats-ear-019ec3fe');

-- =====================================================================
-- Block A: "Cel" chapter -> new chapter position 1 (SortKey 1.0-56.0,
-- all < 100.0, the current start of "The Intake"). 56 beats, order preserved.
-- =====================================================================
UPDATE nb
SET nb.SortKey = v.NewSortKey
FROM NodeBeats nb
JOIN (VALUES
  ('019ECBE0-C093-7D95-80D5-AFE0FDAEAF23', 1.0),
  ('019EC969-A434-7F00-8EF1-BF27A4A92B46', 2.0),
  ('019EC969-C69A-77D2-8AF4-B1F1396BA0FA', 3.0),
  ('019EC969-DF9F-7612-A5D4-5060CD32E4F6', 4.0),
  ('019EC969-F96E-7A46-A393-213A0C99A276', 5.0),
  ('019EC96A-147A-71EE-B1C0-FCD17611EFD9', 6.0),
  ('019EC96A-2E18-77DB-A076-5005ADC8FB42', 7.0),
  ('019ECBE0-D71B-7A1F-A41A-4EADFBD4230A', 8.0),
  ('019EC96A-48C9-741A-BC61-EA82F5C1E7B7', 9.0),
  ('019EC96A-66A9-7FEF-8E0D-4F0832D92BEA', 10.0),
  ('019EC96A-8217-75ED-A96B-E43C3DD8DC69', 11.0),
  ('019EC96A-A145-7973-BEAD-1AF93D4760D1', 12.0),
  ('019ECBF7-87DF-7CB4-B3C2-FCC932F79497', 13.0),
  ('019ECBE0-E595-73A5-9E2D-2D8F23F72C99', 14.0),
  ('019EC96A-BDF8-7082-A365-B65EE91A4E79', 15.0),
  ('019EC96A-DB14-75E0-AE65-3EC249041FB4', 16.0),
  ('019EC96A-F477-73C2-908B-465620599559', 17.0),
  ('019EC96B-0D7E-75F0-B3F4-05AC06C7339A', 18.0),
  ('019EC96B-2D8D-7FC4-89C8-95ED26FE7DFE', 19.0),
  ('019EC96B-4F0C-73FB-806A-DBC4B51224F9', 20.0),
  ('019EC96B-6740-7AB3-99C9-DEC94BE3DCEE', 21.0),
  ('019EC96B-82D5-7380-AC92-7A803115662A', 22.0),
  ('019EC96B-9C0D-763C-87EC-FFC426877413', 23.0),
  ('019EC96B-BA91-7C6D-9FF0-CBD2257DEC5D', 24.0),
  ('019EC96B-DA22-7DD0-B2E9-E46EC5B795FE', 25.0),
  ('019EC96B-FBCF-7BCA-9E60-FF7DCF085AAA', 26.0),
  ('019EC96C-1902-7976-B458-A3FC895A263A', 27.0),
  ('019EC96C-36C1-72C1-A1BE-C60B16341F11', 28.0),
  ('019EC96C-5474-7101-9D29-9DD6D398697A', 29.0),
  ('019EC9C5-1DBF-7F6E-9A87-E189A3CFF0D6', 30.0),
  ('019EC9C5-5100-78F2-8A86-8785926B8A72', 31.0),
  ('019EC9C5-8245-773B-BF7C-DF0E5D3C12C8', 32.0),
  ('019EC9C5-A8AB-76F6-A3F2-5E8C6051767C', 33.0),
  ('019EC9C5-E63F-7973-9CC7-60EC71E33474', 34.0),
  ('019EC9C6-1E55-7D23-971C-0C6E23D191B0', 35.0),
  ('019EC9C6-5897-75CD-BD52-A8A75D3D383B', 36.0),
  ('019EC9C6-97DB-7FA3-BDFC-C09F54CC73DA', 37.0),
  ('019EC9C6-D8AC-7078-AFD0-EC792AB11CF3', 38.0),
  ('019EC9C7-13B1-7E03-A385-E59E3C80951E', 39.0),
  ('019EC9C7-46CE-7D55-B1EC-4C4EF1626400', 40.0),
  ('019EC9C7-85FD-7B27-B31F-2A042FED04B5', 41.0),
  ('019EC9C7-CBB1-7B86-A5F0-781B50417C09', 42.0),
  ('019EC96C-70B8-7222-AF7E-6BA7B370CD4F', 43.0),
  ('019EC96C-7BB2-7F80-A668-AC093FA69860', 44.0),
  ('019EC96C-99C1-72DB-B4E3-0048AA8F8D4E', 45.0),
  ('019EC96C-B544-7FC6-A0F5-9470ACCF6CAA', 46.0),
  ('019EC96C-BDFA-7EAA-94DD-2FDA1A631DAA', 47.0),
  ('019EC96C-CEEC-7B99-9525-B3EFA40B01C5', 48.0),
  ('019EC96C-E727-78BF-AEEC-2EE85657E15E', 49.0),
  ('019EC96D-0BB3-700D-8DCC-27DDE1D97CFC', 50.0),
  ('019EC96D-23F9-726B-8427-6729F2F7ECA9', 51.0),
  ('019EC96D-3D12-740E-8A6D-B349D2A5F13D', 52.0),
  ('019EC96D-53A8-7BCF-A2B2-0A7513498ACE', 53.0),
  ('019EC96D-6C7B-714F-82F4-11731EDCBB6A', 54.0),
  ('019EC96D-8904-7F9F-A6B8-D087CFA36D76', 55.0),
  ('019EC96D-9F32-780C-8929-D0C50D20679E', 56.0)
) AS v(BeatId, NewSortKey) ON nb.BeatId = TRY_CAST(v.BeatId AS UNIQUEIDENTIFIER)
WHERE nb.NodeId = @NodeId AND nb.IsEnabled = 1;

-- =====================================================================
-- Block B: orphan beat 4392 + "What She Asked For" chapter -> new
-- chapter position 4 (SortKey 8902.0-8980.0, between "What the Room
-- Holds" ending at 8900.0 and "What She Wouldn't Do" starting at
-- 9000.0). Orphan 4392 relocated to the front of the block (see §1).
-- =====================================================================
UPDATE nb
SET nb.SortKey = v.NewSortKey
FROM NodeBeats nb
JOIN (VALUES
  ('ACD767DC-592C-4923-90A7-EFB5CCD135E1', 8902.0),
  ('019EC974-F8C4-7DC1-82B1-1690D7B22EE7', 8904.0),
  ('019EC975-10D6-7318-B0EA-569737D03F21', 8906.0),
  ('019EC975-22DB-79F1-9171-163610939405', 8908.0),
  ('019EC975-4097-7638-A832-F4872A574694', 8910.0),
  ('019EC975-5C72-7244-9CE8-6387F01ADE78', 8912.0),
  ('019ECB90-AAFE-7547-AE8C-583E10CE2AE3', 8914.0),
  ('019EC975-75B3-7A89-8389-275165C12ADF', 8916.0),
  ('019EC975-8F18-70F2-B783-6F6E8CAE16FA', 8918.0),
  ('019EC975-BC42-75C5-A849-FD7C146C38D1', 8920.0),
  ('019ECB91-4A75-7983-894A-B69906059C43', 8922.0),
  ('019EC975-D8B6-7113-A24C-B73F87FA2EF2', 8924.0),
  ('019EC976-005F-70ED-AB77-E3F40643001B', 8926.0),
  ('019EC976-1F39-78EC-86FD-6DB715F06216', 8928.0),
  ('019EC976-3B33-7AC4-9855-A5E3DE99C060', 8930.0),
  ('019EC976-53EA-74BA-9587-DC0E12C2BEC5', 8932.0),
  ('019EC976-6659-73AD-B179-681B41833CC4', 8934.0),
  ('019EC976-7E97-740A-9E46-B4EA93711C26', 8936.0),
  ('019EC976-9283-70BA-A765-179FF2681C79', 8938.0),
  ('019EC976-A2EB-7E2F-A07F-F951A064A5A2', 8940.0),
  ('019EC976-B9FC-77F4-8BF4-8010C4B3316C', 8942.0),
  ('019EC976-CC5D-7F4F-B680-827C32FBE877', 8944.0),
  ('019EC976-DB70-705F-983F-4522639D76B8', 8946.0),
  ('019EC976-F129-70F1-89FF-1D8D27CF8328', 8948.0),
  ('019EC976-FB34-7641-BD4E-9D81026DF00A', 8950.0),
  ('019EC977-128D-7A93-A8E8-E97C643CF4D6', 8952.0),
  ('019EC977-403B-717D-B71A-72F6E8D0D311', 8954.0),
  ('019EC977-56B5-72A4-B66A-76FA24BB8C3F', 8956.0),
  ('019EC977-6BF4-78DD-ACDF-1500B6F180B4', 8958.0),
  ('019EC977-8375-732E-9DFC-A4A730B11437', 8960.0),
  ('019EC977-98B7-7EDA-B568-1F54A70A76D8', 8962.0),
  ('019EC977-ACD0-7B39-85CF-179EC55B6ECA', 8964.0),
  ('019EC977-C9CE-77F6-A6EA-24CBBD31081C', 8966.0),
  ('019EC977-E2E7-7F57-AC70-D5544EC89185', 8968.0),
  ('019EC977-F568-7704-976B-60AC4A27F2A1', 8970.0),
  ('019EC978-0D0A-7EBC-814B-113DC29C0304', 8972.0),
  ('019EC978-226A-785F-B289-F135B9E42E7B', 8974.0),
  ('019EC978-3E54-7A67-97AC-38E9206383DE', 8976.0),
  ('019EC978-541A-748D-91BB-87C48C348398', 8978.0),
  ('019EC978-6B32-7BD0-AD3D-CDA1B4393E87', 8980.0)
) AS v(BeatId, NewSortKey) ON nb.BeatId = TRY_CAST(v.BeatId AS UNIQUEIDENTIFIER)
WHERE nb.NodeId = @NodeId AND nb.IsEnabled = 1;

-- =====================================================================
-- Block C: "The Ghost Period" chapter -> new chapter position 7
-- (SortKey 17802.0-17876.0, between "Clean Sharps" ending at 17800.0
-- and "The Convergence" starting at 17900.0). 38 beats, order preserved.
-- =====================================================================
UPDATE nb
SET nb.SortKey = v.NewSortKey
FROM NodeBeats nb
JOIN (VALUES
  ('019EC976-867C-7251-BF53-3854033AB103', 17802.0),
  ('019EC976-A392-7A46-B4AC-64E5B109A615', 17804.0),
  ('019EC976-CA04-7CCC-A090-E2BCCDDCDEDB', 17806.0),
  ('019EC976-D7DF-76CB-825A-6861E3CF9992', 17808.0),
  ('019EC976-EF25-742A-B7AA-CB94C736099E', 17810.0),
  ('019EC976-FFE5-718A-8D04-1EE30D44B351', 17812.0),
  ('019EC977-17BE-7714-90AF-7EEC20892212', 17814.0),
  ('019EC977-3733-77CE-BE30-95E95C52B84A', 17816.0),
  ('019EC977-4B65-7595-AA2A-D1E2D70D49BD', 17818.0),
  ('019EC977-5515-75C1-8EB5-2D354973D3E8', 17820.0),
  ('019EC977-6921-7D8C-9884-34F55ACF8C43', 17822.0),
  ('019EC977-77BF-7DFD-8D26-DBA7379E7D27', 17824.0),
  ('019EC977-9414-7D92-BF32-17B4EAEA22BC', 17826.0),
  ('019EC977-A82C-75CD-B8D3-C9D9251901AC', 17828.0),
  ('019EC977-C39E-7FE5-A516-1C70744654E4', 17830.0),
  ('019EC977-DA07-76FE-8F61-9D2F04B0804B', 17832.0),
  ('019EC977-F377-7C94-BA22-E65ED4193F0E', 17834.0),
  ('019EC978-0DB9-7635-A7BC-79248956A372', 17836.0),
  ('019EC978-21E3-72A4-81BC-52C83CF58696', 17838.0),
  ('019EC978-3DE5-723D-B80D-E66F5A156E1E', 17840.0),
  ('019EC978-58D2-7F53-B2A9-49D07374ED44', 17842.0),
  ('019EC978-6921-7941-9FD9-EB1EFB14102C', 17844.0),
  ('019EC978-74D4-7CAC-B049-764CA0D33EDB', 17846.0),
  ('019EC978-8B4B-7BF7-B93A-28E4E5F71A47', 17848.0),
  ('019EC978-A340-7025-8BF6-ABD96204329A', 17850.0),
  ('019EC978-AD57-7B90-8DBE-777C7134A818', 17852.0),
  ('019EC978-C600-7B11-BFCE-52F33A591C73', 17854.0),
  ('019EC978-CFDE-71B1-802A-5C4936F9262F', 17856.0),
  ('019EC978-EBE5-7295-86FC-C719109928A6', 17858.0),
  ('019EC979-03E7-769B-AFB6-E32953E86EE3', 17860.0),
  ('019EC979-1964-701D-ACE4-7D68ACFAF1B5', 17862.0),
  ('019EC979-3A6B-752F-B1DD-163A37513704', 17864.0),
  ('019EC979-4A7B-7353-A9CF-9996CB25024E', 17866.0),
  ('019EC979-75DE-7A5B-8D08-D91584FB8707', 17868.0),
  ('019EC979-7F36-7CFB-875E-5197F39D1DF4', 17870.0),
  ('019EC979-8B02-7A9F-8A7D-82A97CC4FD60', 17872.0),
  ('019EC979-A549-7865-B67A-6B596A804853', 17874.0),
  ('019EC979-C648-7E93-9D15-A96327E4D050', 17876.0)
) AS v(BeatId, NewSortKey) ON nb.BeatId = TRY_CAST(v.BeatId AS UNIQUEIDENTIFIER)
WHERE nb.NodeId = @NodeId AND nb.IsEnabled = 1;

-- =====================================================================
-- Block D: finale-linkage fix (Task 4). Relocate orphan beat 4968
-- ("He didn't wait for them to come back with something...") from its
-- stray half-step SortKey (22550.0, wedged inside "The Convergence")
-- into "Voluntary Recall", between beat 3832 (SK 42500.0) and beat
-- 3844 (SK 42700.0) -- a pre-existing 200-point gap, no other beat
-- needs to move.
-- =====================================================================
UPDATE nb
SET nb.SortKey = 42600.0
FROM NodeBeats nb
WHERE nb.NodeId = @NodeId AND nb.IsEnabled = 1
  AND nb.BeatId = '019F21AF-2A9E-72DB-8DF6-B0F209CF9F9E';
```

No `IsChapterStart` changes are required anywhere (see §2 — the climax chapters are already
correctly bounded; the three relocated Celeste chapters keep their existing chapter-start
flags on the same beats, which remain each new block's first beat).

## Verification after running

```sql
-- Re-run the audit's chapter-grouping query (see Task instructions §3) and confirm:
--  1. Chapter order is Cel, Intake, Room Holds, What She Asked For, Wouldn't Do,
--     Clean Sharps, Ghost Period, Convergence, Same Cold, No Signal, Surfacing,
--     Voluntary Recall.
--  2. Beat counts: Cel=56, Intake=35, Room Holds=54 (one moved out: 4392),
--     What She Asked For=40 (39+relocated orphan), Wouldn't Do=44, Clean Sharps=43,
--     Ghost Period=38, Convergence=48 (one moved out: 4968), Same Cold=37,
--     No Signal=58, Surfacing=65, Voluntary Recall=46 (one moved in: 4968).
--  3. Total still 564.
```

## Summary of what changed vs. what didn't

- **Moved:** 134 beats (Cel + What She Asked For + orphan 4392 + The Ghost Period) to new
  chapter positions 1/4/7. **Moved:** 1 beat (4968) within the existing Voluntary Recall
  chapter to fix a linkage bug.
- **Not moved:** all 9 non-Celeste chapters keep their current beats and positions —
  the audit's climax-bisection and 44%-imbalance findings do not hold under the true
  (SortKey) order and needed no fix.
- **Not patched:** the Sol "brother" line — it's load-bearing across 4 beats including the
  reveal-pivot; the bible is the side that likely needs the edit, not the prose (see §3).
- **Not resolved (flagged for an editor):** the apparent duplicate Pulse-departure sequence
  (beats 4054/4055 vs. 3926-3944/4037-3944) inside the relocated Cel chapter (see §1).
