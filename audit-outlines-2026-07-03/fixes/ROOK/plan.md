# Rook Trilogy Reconciliation Plan — Magenta & Gunmetal (MxG) + Crimson & Chrome (CxC)

Files only. No database writes. This plan documents every ruling, the beat map each ruling
touches, the CxC alternate-draft disable list, and the terminology sweep. Full patched prose
for every touched beat lives in `beats/<book>-<order>-<slug>.md` in this folder.

Read against: `docs/nodes/MxG.md`, `docs/nodes/CxC.md`, `docs/nodes/NxR.md`,
`audit-outlines-2026-07-03/MxG.md`, `audit-outlines-2026-07-03/CxC.md`, and the live `Beats`/`NodeBeats`
rows pulled read-only via sqlcmd on 2026-07-03.

---

## Beat maps (DB Number → spine # → BeatId)

### MxG (`magenta-gunmetal-019f00a6`)

| Spine # | Number | BeatId | Title | Touched? |
|---|---|---|---|---|
| 1 | 4745 | 019F00B0-D892-702D-96CB-690051709CDA | The Offer | YES — terminology |
| 2 | 4746 | 019F00B0-E180-7B32-84E1-891CD8B2418A | Assembly | no |
| 3 | 4747 | 019F00B0-EC7B-70F6-80EC-81A27EFB2550 | Legwork | no (plant stays as the setup; payoff lands in Beat 9 / 4752) |
| 4 | 4748 | 019F00B0-F606-7EC9-A460-D2F6750A52D3 | The Plan | no |
| 5+6 | 4749 | 019F00B1-0063-7B5B-8EFC-0580B52B1960 | The Complication/Counter-Client | YES — payment arithmetic |
| 7 | 4750, 4905 | 019F00B1-09C3-7304-A725-EF932A398ABD, 019F11DA-2C3C-7375-A6F1-F1B94EEFDFEB | The Pursuit Opens | no |
| 8 | 4751 | 019F00B1-1405-70DF-9C38-FB1F6C5DD9C1 | The Bridge | no |
| 9 | 4752 | 019F00B1-1D86-7D2A-AA10-106AAABD9C8C | The Safe House | YES — backup copy payoff, seven-weeks payoff, terminology |
| 10 | 4906, 4966 | — | Burned | no |
| 11 | 4754 | — | Gault | no |
| 12 | 4755, 4907 | 019F00B1-3973-7892-9AB5-3C633C905EFF (4755) | The Approach | YES — terminology |
| 13 | 4757 | 019F00B1-59B6-7B78-937C-A1B5904F8460 | The Lake | YES — force accounting, crane payoff |
| 14 | 4758 | 019F00B1-62A0-7C7B-99E5-445DE15A84E1 | The Count | no (resolves cleanly once 4749 is patched) |

### CxC (`marrow-chrome-019f0968`) — 14-beat canonical spine

| Spine # | Number | BeatId | Title | Touched? |
|---|---|---|---|---|
| 1 | 4844 | 019F0969-C7D9-70F2-9D23-338A6570D2DC | The Job She Dreaded | YES — Axiom-wound reattribution |
| 2 | 4845 | 019F0969-E214-7353-805E-8E661B46CA0B | The Trusted List | YES — Axiom-wound reattribution (1 line) |
| 3 | 4846 | 019F0969-FC91-7B5A-BC13-2E7AC349C5BE | The Survivor List | YES — Axiom-wound reattribution |
| 4 | 4847 | 019F096A-1536-760F-BAA1-281FB7B49625 | The Seam in the Trilogy | YES — Axiom-wound reattribution + rehearsal reassignment |
| 5 | 4848 | 019F096A-3318-7944-80F8-0C5177B61994 | The Mirror Crosses | no |
| 6 | 4849 | 019F096A-4A8F-7136-B5D7-E0D7CEACE8E7 | The Marrow | no (no "Axiom" wound references found in canonical text) |
| 7 | 4850 | 019F096A-6C7E-7E10-95CE-BBA2B085E52E | The Fatal Thread | no |
| 8 | 4851 | 019F096A-80C3-790F-91C8-EB11DB81A546 | Soraya Becomes Her | no |
| 9 | 4852 | 019F096A-98E4-78B2-BEF2-AE750268AD1C | The Setup | YES — terminology |
| 10 | 4853 | 019F096A-B08C-75B3-928A-CD1A627B1AB6 | The Run In | YES — terminology |
| 11 | 4854 | 019F096A-C84E-7990-A5D0-8EABC3DF7839 | Vox Steps Into the Light | YES — Scout Husk/Shell contradiction |
| 12 | 4855 | 019F096A-DE7B-712B-892F-2974FE7B467B | The Executive | no |
| 13 | 4856 | 019F096A-F6B7-7065-9C65-875B2932E606 | The Burning-Down | YES — Adalemo's fate |
| 14 | 4857 | 019F096B-238D-7AF5-A7A1-B4FC939968D0 | The Count, With Names | YES — ending closure |

### CxC alternate-draft block — DISABLE (see ruling 4 below)

All eleven rows below are linked into `NodeBeats` for `marrow-chrome-019f0968` with `IsEnabled = 1`
and `Beats.Synopsis IS NULL`. They are not part of the documented 14-beat spine.

| Number | BeatId | SortKey |
|---|---|---|
| 4890 | 019F11B0-3024-7E49-8293-5502EDF3AE58 | 75.0 |
| 4897 | 019F11BC-EAC3-791F-82C2-7263E26CB419 | 125.0 |
| 4891 | 019F11B6-C83C-7D43-A9C3-791FD59064AF | 225.0 |
| 4919 | 019F136D-9A54-7073-B353-76731614C13B | 275.0 |
| 4892 | 019F11B6-FAFE-701D-8775-398538A5ADD1 | 375.0 |
| 4920 | 019F136E-26A0-72AC-A615-C2D3273E1B01 | 462.5 |
| 4893 | 019F11B7-3739-7E10-97AD-72BF3F672057 | 475.0 |
| 4894 | 019F11B7-6939-71DF-AB02-8DDD0326EED6 | 525.0 |
| 4895 | 019F11B7-CE6A-7891-A445-06A04593F7D7 | 675.0 |
| 4896 | 019F11BA-A93B-761B-87A5-C3BFC7CD6FBC | 687.5 |
| 4967 | 019F21AF-2281-797C-9D36-EFB2EAE9264D | 575.0 |

**Action: set `NodeBeats.IsEnabled = 0` for all eleven `BeatId`s above, for `NodeId` =
`marrow-chrome-019f0968`.** (Plan only — no DB write performed by this pass.)

---

## Rulings

### CxC Ruling 1 — Axiom-job reconciliation (the finale's emotional spine)

**Finding confirmed as described.** Beats 4844, 4845, 4846, 4847 (and the alt-draft blocks, which
are being disabled anyway) repeatedly label a 21-in/14-out, seven-dead personal wound as "the
Axiom job," and Beat 4846 has Sefi Okonkwo claim she personally witnessed Rook lead that
operation ("I counted you going in, too") — which is impossible (Sefi was 19 four years ago, and
the wound predates Axiom/MxG by years) and which flatly contradicts MxG's documented Axiom
extraction: a five-person, no-casualty, Φ40,000 job to pull one researcher (Soraya) out cleanly.

**Ruling:** the 21/14/Wennick wound is re-attributed to a separate, earlier operation, named
**"the Tidewell job."** Chronology: Tidewell happened while Rook was still a Meridian PD
intelligence analyst (pre-2208), a joint task-force operation that used contracted freelance
"hired hands" (including Lace) alongside PD personnel to extract a group of 21 people under a
"rescue" framing; the operation was secretly an early harvest-for-hire rehearsal (paid by the
head) that a proto-Helix buyer used as its proof of concept. Twenty-one went in; fourteen came
out; the other seven, Wennick among them, did not — Wennick is the one Rook carries personally
(a door she called clean that wasn't), the seven are the collective wound. This slots under the
MxG bible's existing wound description ("a bad call during a Meridian surveillance operation") as
an elaboration, not a contradiction, and explains why Rook only ever spoke of "a colleague" in
MxG — she was compressing a seven-person loss into the one name she could say out loud.

This also lets Beat 4847's "I ran the rehearsal... they paid me by the head and called it a
rescue" become the Tidewell confession specifically, while Beat 4847's separate, valid claim
("That job [the Soraya extraction] got Soraya out clean for them — proof the harvest could be
pulled live") stays attached to the real Axiom/MxG job, per CxC bible §9's clue-plant refactor.
Net effect: the trilogy's "we are the audit trail" reveal gets *stronger* — two of Rook's past
jobs (Tidewell, years before; Axiom, MxG's own job) unknowingly built the machine, not one
retconned one.

**MxG verification (per task instruction):** Wennick is already referenced in MxG, three times
(Beat 1 / 4745: "she counted them differently since Wennick"; Beat 3 / 4747: "before Wennick — the
surveillance op where the window she'd called clean wasn't, and he went through a door she'd said
was safe"; Beat 4 / 4748: "before Wennick," "the same way Wennick had"; Beat 12 / 4755:
"Wennick had trusted her numbers too"). All four references are consistent with Wennick predating
the Axiom job, and MxG never states or implies Wennick died *on* Axiom. **No MxG edits are
required or made for this ruling** — MxG already reads correctly once CxC's job label changes.

Patched (CxC): Beats 4844, 4845 (one line), 4846, 4847.

### CxC Ruling 2 — Scout's arc reversal

Confirmed: Beats 4852–4853 (Setup, Run In) build Scout's arc explicitly around physical presence —
"She was here, in her body, in her boots, making weight on the tread surface... This once, she
was the thing in the room the room could feel." Beat 4854 then narrates, in Vox's internal
reflection, "Scout was the one who rode in absence, ejected from her own Husk and injected into
Gerald — her oldest Shell — leaving the Husk behind," stated as a *current* fact, directly negating
the beat it sits three paragraphs below.

**Ruling:** patch Beat 4854 so the sentence is unambiguously about Scout's *usual, prior*
tradecraft (contrasted with tonight), not what she is doing right now. Minimal tense/scope fix —
Vox is still allowed to draw the comparison to Scout's normal MO; she just can't assert Scout
ejected *tonight*, since Scout is provably still down in Bay C, present, at this exact moment in
the same beat.

Patched: Beat 4854.

### CxC Ruling 3 — Adalemo's fate

Confirmed: Beat 4856's "the last thing the PEREGRINE that had been Adalemo did" implies a
body-snatch/conversion with zero setup, and contradicts PEREGRINE's established nature (human
contractors — NxR bible, and CxC's own Beat 4853 description of PEREGRINE-grade security as
human operators in hardened gear, "ex-military chassis" meaning kit, not a machine host) and
Adalemo's own on-page status as Rook's ally through Beats 4851–4855.

**Ruling:** Beat 4851 already stages Adalemo's full, legible crossing on-page (he hands over the
substrate ledger and his live Helix credential, commits fully — this satisfies Lock §7 on its
own). Beat 4856's "PEREGRINE that had been Adalemo" language is cut. He is rewritten as
unambiguously human, still wearing Helix-issued gray for cover access, watched via the same
Bay-C feed Rook left open, using his last window on the credential to reroute the compliance
alert away from Bay C rather than answer Helix's broadcast — a second, quieter completion of the
same crossing, not an unexplained conversion.

Patched: Beat 4856.

### CxC Ruling 4 — Alternate-draft block (Numbers 4890–4967, ~9k words, ENABLED)

**Verdict: DISABLE the entire block.** Read in full. It is a materially different, unreconciled
telling of the same job:

- Soraya briefs Rook with a 17-circle routing map, not the Mrs. Chen's/Sefi-in-person scene.
- Sefi is introduced as an unmet Z6 profile-target ("the forty-first name") in Beat 4919 — which
  directly contradicts canonical Beat 4846, where Sefi is already identified, present, and
  testifying in person at Mrs. Chen's in Beat 3.
- A new character, "Yanneke," appears in the alt block (Beats 4893–4894, 4896) and is absent from
  the bible and from the canonical spine entirely.
- The raid resolves as 24 + 7 = 31 found via Yanneke's group, not through the canonical Bay-C
  standoff/Vox-steps-into-the-light sequence.
- Beat 4967 stages an entirely different Anneke confrontation (a blast door, "come and count with
  me") that does not match Beat 4855's landing-scene confrontation and cannot both be canon.

**Confirmed: the documented 14-beat spine (4844–4857) is complete and internally coherent without
the alt block.** Read start to finish in SortKey order, it delivers every beat the bible's §6
table promises, with no gaps the alt material was silently patching.

**Action:** mark all eleven alt rows `IsEnabled = 0` (list above). No prose from the alt block is
reused in this pass — Yanneke, the 17-circle map, and the second Anneke confrontation are not
canon.

### CxC Ruling 5 — Beat 14 ending

Confirmed: Soraya's "It came out whole" is immediately undercut by the reveal that the Marrow was
1 of 3 Helix facilities running the same program, "the count isn't finished," with the literal
last line of the book being "3 facilities," underlined — an operational hook, not a close.

**Ruling:** keep the "it came out whole" resolution as the true emotional beat (thirty-one people,
more than it came in with — that stands, unedited). Keep Soraya's information that the harvest
is bigger than one facility (this is real, and it's consistent with the "no clean wins" lock —
erasing it entirely would oversell the victory). Cut the scene's pivot into active mission-planning
(floor plans, compliance-archive access, "start with the one closest to the next delivery cycle")
and the underlined "3 facilities" as a literal last-page directive. Rook receives the information,
feels its weight, and explicitly declines to let it become tonight's job — the trilogy closes on
the number she came in to protect (thirty-one, named), with the larger fight acknowledged as the
world's ongoing burden, not this book's cliffhanger. This is "soft-life continuation," not an
unresolved operational hook, per instruction.

Patched: Beat 4857.

### MxG Ruling 6 — Finale force-accounting (Beat 13)

Confirmed: PEREGRINE is four personnel (commander/Adalemo, close-quarters specialist,
network/signal operator, driver-pilot), but Beat 13 needs five simultaneous bodies: specialist
(downed by Boiler, deck 1), network operator (downed by Rook, deck-2 corridor), Adalemo (railing
duel, deck 3), "the fourth PEREGRINE member" chasing Ohara (deck 3), and the driver-pilot (holding
position in the VTOL throughout).

**Ruling — cheapest coherent fix:** cut "the fourth PEREGRINE member" as a distinct body. Ohara's
lateral repositioning behind the structural column becomes an autonomous precaution (consistent
with her established characterization — she already independently reads the deck for four
seconds and decides on her own, elsewhere in the same beat), not evasion of an active,
unaccounted-for fifth pursuer. The math now closes exactly at four: specialist, operator,
Adalemo, driver-pilot.

Patched: Beat 4757.

### MxG Ruling 7 — Crane payoff

Confirmed: Boiler gets into a firing position on the crane arm, sighted through the viewport
glass exactly as the bible promises (§2, Lock §5), and is told to stand down without firing —
three beats of setup (implicitly since Beat 8, explicitly the crane/viewport buildup within Beat
13 itself) that never pay off on the page.

**Ruling: deliver it.** Boiler fires one shot through the reinforced viewport glass into a relay
junction carrying the platform's authentication trunk — the same trunk Axiom's revocation request
is racing down (per Vox's stated four-minute/eighty-seven-node stakes in the same beat). The shot
is not decorative: it measurably widens Ohara's authentication window, ties the money-shot
directly into the mission's actual stakes, and gives the maintenance team a structurally
significant, inexplicable piece of storm damage to talk about for years — satisfying Lock §5
without inventing a fifth body for him to shoot (see Ruling 6).

Patched: Beat 4757 (same beat as Ruling 6, same patch pass).

### MxG Ruling 8 — Orphaned plants

**(a) "Seven weeks" (Beat 3).** **(b) Ohara's "one backup copy" (Beat 9).**

**Ruling (a):** payoff delivered in Beat 9 (4752), where it naturally belongs alongside Ohara's
full-scope disclosure. Rook raises the seven-week flag; Ohara explains it was Axiom's own internal
audit tightening around her account (badge-log pulls, a rotated-off contractor) once her slow
deletion campaign started producing anomalies — this also resolves audit Finding 8 ("pre-deployed
bait has no supporting mechanism") in the same stroke: Axiom was never watching for *this crew*
specifically; it already had a live nerve running to Soraya, and anything that touched it read as
urgent the instant it moved. As a low-cost bonus (not separately required, but free to close in
the same passage), Ohara's same confession also accounts for finding 12's dangling pod-transit
priority flag ("a flag I never requested and didn't dare remove").

**Ruling (b):** payoff delivered in the same beat, immediately after Ohara confirms "One." She
clarifies on the page that the backup is *testimony* (dates, filings, the pathway she found and
who she told), not the formula — it buys Axiom's accountability after the fact, not a bypass of
the terminal. This closes the safety-net leak without contradicting Lock §2 (Ohara must publish
herself; no shortcuts).

Patched: Beat 4752 (both payoffs, same beat).

### MxG Ruling 9 — Payment arithmetic vs. Lock §3

Confirmed: Beat 1 sets Φ40,000 split five ways through Gault's Axiom-linked channel. Beat 5/6 has
Ohara promise "a second escrow, structured identically to the first" once the real stakes surface
— read literally, a second Φ40,000, implying Φ80,000 total. Beat 14 shows exactly one Φ8,000/head
payment arriving. Lock §3 requires the total to be exactly Φ40,000, stated on the page.

**Ruling:** patch Ohara's line in Beat 5/6 so the second escrow explicitly *replaces* the
Axiom-routed original (which she flags as certain to be frozen or clawed back the moment Axiom
realizes what happened), rather than adding to it. Total stays Φ40,000, funded entirely by Ohara
once the original channel goes bad. Beat 14 needs no further change — its single Φ8,000/head
payment now unambiguously is the whole of it, and the Gault-was-paid-the-identical-figure detail
reads as the intended irony (two unrelated Φ8,000 transactions, same number) rather than a
bookkeeping ambiguity.

Patched: Beat 4749.

### MxG Ruling 10 — Terminology ("Rider" → Exo/RFO/Jockey, SS-A38)

**Found and patched, MxG:**
- Beat 1 / 4745 (line "Diallo. Rider, drone support...") → "Exo."
- Beat 9 / 4752 ("Riders," "a Rider never rode...") → "Exos," "an Exo never went in..."
- Beat 12 / 4755 ("the Rider lying defenseless," "You didn't interrupt a Rider who'd ejected") →
  "the Exo lying defenseless," "You didn't interrupt an Exo who'd ejected."

**Found and patched, CxC:**
- Beat 9 / 4852 ("A Rider left her own body... a Rider's whole tradecraft") → "An Exo left her own
  body... an Exo's whole tradecraft."
- Beat 10 / 4853 ("A Rider's whole tradecraft was the absence") → "An Exo's whole tradecraft was
  the absence."

**Not changed (generic word, not the profession title — no SS-A38 conflict):** CxC Beat 7 / 4850,
"I'm a rider people have seen" (Scout describing herself as *someone who rides Gerald*, not
invoking the retired job title); CxC alt-draft Beat 4893, "You don't send a single rider to
collect nine people" (same generic sense; also inside the block being disabled under Ruling 4).

**Untouched beats confirmed clean (no "Rider"/"rider" job-title residue found) after this pass:**
every other beat in both books. A full case-insensitive sweep of both books' exported beat text
turned up zero remaining profession-title usages once the five patches above are applied. No
further sweep is required for these two books; MCRM/NxR terminology was already compliant per
NxR's own bible (§5a uses Exo/RFO/Jockey throughout).

---

## Patch summary

| Book | Beats touched | Rulings applied |
|---|---|---|
| MxG | 5 (4745, 4749, 4752, 4755, 4757) | 6, 7, 8a, 8b, 9, 10 |
| CxC | 9 (4844, 4845, 4846, 4847, 4852, 4853, 4854, 4856, 4857) | 1, 2, 3, 5, 10 |
| CxC (link-only, no prose) | 11 alt rows | 4 (disable) |

## Flags for the user

- **CxC Ruling 1** required inventing a new job name ("Tidewell") and a light backstory (Meridian
  PD-era joint task force, contracted hired hands, harvest-dressed-as-rescue) that is not
  previously documented anywhere. This is the largest interpretive leap in this pass. It was
  chosen over the alternative (retconning MxG's own Axiom job to have been large/bloody) because
  that would break MxG's Lock §3 ("everyone gets paid, no casualties") and its 93.9 score's
  foundation. If "Tidewell" as a name or backstory doesn't fit intended canon, the surgical seams
  are narrow (four lines in 4844/4846/4847, one in 4845) and easy to re-word to a different name.
- **CxC Ruling 4 (disable list)** is a plan only; no `NodeBeats.IsEnabled` write was performed
  (per the files-only, no-DB-writes constraint on this task). Someone with DB write access needs
  to execute the eleven-row update before the next export/review pass, or those beats will keep
  contaminating scoring.
- **MxG Ruling 7 (crane payoff)** ties the shot mechanically to Ohara's authentication window
  rather than to a body count, since Ruling 6 removes the only spare PEREGRINE body it could have
  been aimed at. This is a slightly bigger addition than a pure terminology/line swap, but it's the
  only way to deliver Lock §5's money-shot without re-opening the finding-1 contradiction.
