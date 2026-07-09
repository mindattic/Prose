# BLST Authoring Brief — binding for every authoring agent

Read FIRST: `docs/nodes/BLST.md` (whole file — HARD FACTS, premise, characters §2, locks §3, voice §4)
and `audit-outlines-20260707/blst-beats/spine-goals.txt` (all 30 goals). Then this sheet. The spine
goal for each beat is WHAT HAPPENS; you write HOW IT READS. Execute the goal exactly — invent no
additional plot events.

## Non-negotiable structure (seven prior drafts died here)
1. ONE vote in the whole story. It happens in beat 25, on Ruslan's AshgraveMaterials offer.
   Tally: 23 yes / 17 no / 1 abstention (the abstention is SIGRUN). No straw polls, no show of
   hands, no re-votes, no reversals, no "we vote again" anywhere, in any beat, ever.
2. Nobody's vote is flipped by a speech or revelation. Positions harden early and hold. The drama
   is in what the vote costs, not in persuasion.
3. Nothing is hidden and nothing is revealed. All facts are public from the start: the descent
   schedule is posted weekly, measurements are open, the offer is read aloud. No secret
   recalculations, no concealed costs, no covert anything, no disclosure that recontextualizes.
4. No danger to life, ever. The bloc NEVER collapses, fractures, falls, or threatens anyone.
   The descent is slow, controlled, public. The stakes are class, law, and money: at the
   320-meter ceiling of The Low, insurance voids, tenancy law stops, the bloc becomes
   salvage-eligible. Jettison = deliberately unloading mass to slow the sink. Never cutting
   load-bearing structure, never structural failure, never "buying time" against catastrophe.
5. Closed cast — these eight names and NO other named character (background residents stay
   unnamed: "the widow in 8-D", "a machinist from the third floor"): Teo Mamani (POV, woman,
   she/her), Ruslan Adeyinka, Sigrun Ferreira, Priya Guðmundsen, Kaja (Priya's daughter, TWELVE,
   appears on-page sparingly), Wen Castellanos, Dagny Obuya, Almagre (governor automaton — a
   MACHINE that enforces trim limits; competent, careful, never person-like, no farewell).
6. Teo's fifty-one kilos: she owns fifty-one kilos of personal effects and knows the number
   (establish lightly mid-story). In beat 28, what she does with her own allocation is SHOWN as
   action — never explained, never reflected upon.
7. Ending: the vote passes, the descent proceeds as a controlled landing, consequences land
   together (avalanche). Beat 30 ends ON the descent — no epilogue, no "in thirty days…"
   forward summary, no meaning-narration.
8. Dagny's yes-vote and allocation tip the payout large enough to close Kaja's fund — that is
   the ONLY resolution of Priya's money problem, it happens at beats 25-26, and Dagny never
   explains herself. No heirloom is passed, nothing is gifted, nobody dies.

## Numbers sheet — use EXACTLY these; introduce NO new figures, dates, or thresholds
- 41 households; the bloc (Aerobloc Candelaria) is 60 years old (built 2166).
- Year is 2226. NEVER write a 19xx or 20xx (pre-2100) date for anything in living memory.
- The Low ceiling: 320 meters. Current altitude at story start: 383 meters, losing roughly a
  meter a day and slowly worsening. Jettison program begins in 42 days per Teo's schedule.
- AshgraveMaterials offer: Φ847,000 total ≈ Φ20,650 per household. It does not decay, tick,
  or change during the story.
- Priya's fund for Kaja's corrective neuretics procedure is short exactly Φ8,400.
- Jettison quotas: Priya ≈ Φ2,100 in goods; Dagny ≈ Φ4,800 (largest allocation aboard).
- Wen's workshop: bolted to the frame by his grandfather 40 years ago (2186); thirty years of
  jig calibration. Currency is Φ (QUANTA) — spoken "QUANTA", written Φ.

## World physics and texture
- Lift is EIGENLIFT only: a coherence frame holds the bloc's mass in a partially decoherent
  state — the structure declines most of its weight. Nothing floats, nothing holds vacuum,
  no gas, no cells, no pumps. Coherence Drivers re-tune the frame constantly.
- Sound design: a healthy frame is SILENT (the Hush). A faint rising hum means the frame is
  straining (the Hum). More hum = worse. Never hum-means-healthy.
- Aging frames "fall out of coherence" gradually — lift fades on a measurable curve.
- Comms are neuretics-only (sub-vocal, wrist displays). No phones, no email, no federal
  agencies, no police (security is CorpoNation ArcSec, if ever needed — it isn't).
- The Schism is never named. The peer roster (Kyle, Bear, etc.) never appears.
- Gloss AshgraveMaterials at first mention, in Teo's voice, one clause. Term before acronym
  everywhere (SS-LAW-20).

## Voice (bible §4)
Close-third, Teo's POV for Teo scenes; scene-anchor close-third elsewhere. Accessible prose:
shorter sentences, contractions, Anglo-Saxon vocabulary. Teo thinks in trim: load, list, moment
arm, freeboard, center-of-mass. Full narrator sentences (fragments only under stress). Body
before mind for emotion; metaphors must survive literal scrutiny; no universal-truth wit; the
narrator never names the theme. ~950–1,050 words per beat. Do NOT start beats with a markdown
title/header — begin with prose.

## Mechanics for each beat you write
1. Write the beat to `audit-outlines-20260707/blst-beats/beat-NN.txt` (overwrite; UTF-8).
2. Push to DB (beat GUIDs in `blst-beats/ids.txt`, format `GUID|N|title`) via a UTF-8-BOM .sql
   file — NEVER inline `-Q` (it truncates on double-quoted dialogue and mojibakes em dashes):
   ```powershell
   $t = (Get-Content "<file>" -Raw -Encoding utf8).Trim().Replace("'","''")
   $sql = "SET QUOTED_IDENTIFIER ON;`nUPDATE Beats SET [Text]=N'$t' WHERE Id='<GUID>';"
   [System.IO.File]::WriteAllText("push-NN.sql", $sql, (New-Object System.Text.UTF8Encoding $true))
   sqlcmd -S "(localdb)\MSSQLLocalDB" -d StreetSamurai -I -f 65001 -i "push-NN.sql"
   ```
   Then verify LEN > 4000 AND no mojibake:
   `sqlcmd ... -Q "SELECT LEN([Text]), CASE WHEN [Text] LIKE '%â€%' THEN 'MOJIBAKE' ELSE 'ok' END FROM Beats WHERE Id='<GUID>'"`
3. After your batch: update `blst-beats/authoring-state.md` — 10 lines max: where the story
   stands, each character's current position, open threads for the next agent, and the altitude/
   day-counter values you last used (the next agent must continue them exactly).
