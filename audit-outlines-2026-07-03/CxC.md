# Structural Audit — Crimson & Chrome (CxC / marrow-chrome-019f0968)

Audit date: 2026-07-03. Read-only. Sources: `docs/nodes/CxC.md`, `docs/nodes/MxG.md`, `docs/nodes/NxR.md`,
and the beats attached to the node via `NodeBeats` (queried by `b.Number`, read-only).

**Important data note up front:** the `NodeBeats` join for this node returns 24 rows, not 14. Rows
with BeatId 4844–4857 are the documented 14-beat spine (Synopsis populated, matches `docs/nodes/CxC.md`
§6). Rows with BeatId 4890–4897, 4919, 4920, 4967 have `Synopsis = NULL` and contain ~9,000 words of
additional prose that is **not** described anywhere in the bible's workflow log (§10) and tells a
materially different version of several scenes. Both sets are linked to the node and would be pulled
by any export/review that queries `NodeBeats` for this slug, so both are in scope below.

## Outline

**Canonical 14-beat spine (BeatId 4844–4857):**

1. **The Job She Dreaded** — Soraya tells Rook the 21 Axiom survivors are alive and hunted; Rook says yes before hearing the terms. Establishes Rook's "21 in / 14 out / Wennick" wound as four years old, tied to "the Axiom job."
2. **The Trusted List** — Rook reassembles Vox, Lace, Boiler, Scout; Stave flagged as the reluctant, unreachable fifth. Nothing changes hands yet except commitment.
3. **The Survivor List** — Stave's manifest surfaces; Sefi Okonkwo arrives in person at Mrs. Chen's, gives her own account ("twenty-one of us… I can give you nine"), hands over a barge photo. Rook orders the full 40-name manifest.
4. **The Seam in the Trilogy** — Rook and Soraya name the retcon: MxG's extraction proved the harvest could be walked out "clean"; NxR's partition crack surfaced the list. The crew is its own audit trail.
5. **The Mirror Crosses** — Adalemo defects on the record, reveals he audited 40 names and "never read a forty-first." Asks Rook to find that name. Alliance formed.
6. **The Marrow** — Sefi describes the processing floor from the inside (procedural horror, no smell, doors that "learned to be every door"). Establishes registered Reads as substrate.
7. **The Fatal Thread** — Adalemo flags Vox's face (sold to Stave in NxR) as the thread Helix will pull to ID the crew. Rook commits to building two ways in so no one person is a single point of failure. Vox volunteers for the east core.
8. **Soraya Becomes Her** — Adalemo reveals he kept the full substrate ledger for 4 years and still holds a live, unrevoked Helix credential — the actual key to the vault. Soraya, in the same beat, separately unlocks her own hidden partition (a shutdown procedure for the racks). Neither payoff is "the forty-first name" being named on-page — it's shown on a wall but never spoken.
9. **The Setup** — Route established: down through the working floor, not around it. Boiler flags that a floor full of living, "singing" bodies limits how hard he can push his kit. Scout commits to riding in physically rather than holding a perimeter, with Gerald audibly on the channel. Clock: Adalemo's credential is valid only until the next Helix shift-audit.
10. **The Run In** — Scout descends in her own body (explicitly not as a Rider-in-absence) and finds the 21 in Bay C, Sefi among them. PEREGRINE-contracted security locks her in the aisle with Sefi in her arms before the "soft knock" can land — the building reconciled early.
11. **Vox Steps Into the Light** — Helix broadcasts an ultimatum naming Vox by her sold Cinderblock file. Vox breaks cover deliberately, draws the sweep off Bay C, Lace and Stave back her over comms.
12. **The Executive** — Rook confronts Anneke Oyelowo at a landing between levels. Both are revealed to share the identical three-finger counting tic. Rook draws the Reibo; Anneke holds her off with an unfinished kill order on Sefi; Rook talks her into not finishing it by naming the one line Anneke can't reconcile — that she let the 21 run once before and never priced that mercy.
13. **The Burning-Down** — Boiler wrecks the harvest manifold by hand, costing his arm/his cheer. "Adalemo crosses on screen" via the Bay-C feed, described as "the PEREGRINE that had been Adalemo" turning toward the children before the feed cuts. Helix's own PA system announces the audit is "now, regrettably, public."
14. **The Count, With Names** — Rook and Soraya at the Sojourn, writing names on an analog roll. Total comes to 31 (21 + 3 near-confirmed + others found en route), "more than it came in with." Soraya then reveals the Marrow was one of **three** Helix facilities running the same program, and "the count isn't finished." Final line on the page: "3 facilities," underlined.

**Additional attached prose (BeatId 4890–4967, no Synopsis, not in the documented spine):** an
alternate/expanded telling of the same job — Soraya briefs Rook with a 17-circle routing map (not the
Mrs. Chen's-with-Sefi-in-person scene); Sefi is introduced as an unmet Z6 target profiled from a file,
not as a survivor already sitting across the table; the raid resolves with 24 found in the north block
plus 7 more behind a second seal (31 total) rescued via a named new character, "Yanneke," who does not
appear in the 14-beat spine at all; and a final confrontation beat (BeatId 4967) has Anneke sealing Rook
alone behind a blast door and inviting her to "come and count with me" — a scene that does not match
Beat 12's landing confrontation. This material reads as an earlier or parallel draft pass that was never
reconciled with, or removed in favor of, the 14-beat spine.

## Structural Findings

1. **Trilogy-breaking retcon of Rook's core wound and the MxG crew (Beats 1, 3, 4, 6).** CxC's opening
   beat establishes Rook's central wound as "Twenty-one going in. Fourteen out" on "the Axiom job,"
   explicitly "four years" ago — i.e., MxG's timeframe — with Sefi later confirming "you carried
   fourteen out of Axiom... I counted you going in, too" (Beat 3), and Rook repeating "fourteen out of
   Axiom" again in Beat 4 and Beat 6. But `docs/nodes/MxG.md` §0/§2/§3 documents the Axiom extraction as
   a **four-or-five-person** freelance crew (Rook, Lace, Boiler, Vox, Scout) with an explicit no-casualty
   coda: "Everyone got paid. Exact amount." Rook's documented MxG wound is a *different, prior* incident
   ("got a colleague killed on a bad call during a Meridian surveillance operation"). CxC's "21 in / 14
   out / Wennick" backstory is neither in MxG nor NxR and directly contradicts the one on-record account
   of what happened at Axiom. This isn't a minor detail — it's restated four times and is the emotional
   spine the entire strand hangs its stakes on (§8 "Emotional Architecture"). As written, a reader of all
   three books hits a direct contradiction about how many people went into Axiom and how many came back.

2. **Scout's central arc beat is asserted and then reversed one beat later (Beat 10 vs. Beat 11).**
   The bible (§4, §6 Beat 9) makes Scout's arc explicit: "the rider who lives in absence has to be
   present." Beat 9 and Beat 10 deliver on exactly that, at length and repeatedly: "She was here, in her
   body, in her boots, making weight on the tread surface... This once, she was the thing in the room
   the room could feel." Then Beat 11's exposition flatly reverses it: "Scout was the one who rode in
   absence, ejected from her own Husk and injected into Gerald — her oldest Shell — leaving the Husk
   behind." That is the old Rider tradecraft the whole point of Beats 9–10 was to subvert. As written,
   the text asserts both that Scout is physically down there and that she ejected her Husk and is riding
   a Shell — mutually exclusive — and it undoes the arc's central beat in the same breath it's invoked to
   praise it.

3. **Adalemo's fate is staged as a body-snatch with zero setup (Beat 13).** Beat 13 describes "the
   last thing the PEREGRINE that had been Adalemo did" — language that only makes sense if Adalemo has
   been captured and converted into (or is being puppeted as) one of Helix's PEREGRINE security
   contractors. Nothing between Beat 9 (Adalemo at the planning table, human, giving Rook a clock) and
   Beat 13 depicts a capture, conversion, or even an encounter that would explain this. It also
   contradicts NxR's own definition of PEREGRINE as a private security contractor org (human operators
   under new funding, not a chassis people get put into) and contradicts Adalemo's established, ongoing
   status in this same strand as Rook's ally standing beside her. Lock §7 ("Adalemo finishes crossing,
   on screen") needed a legible beat showing what "on screen" means and how he got there; instead the
   payoff reads as a different character's death dressed in Adalemo's name.

4. **The finale directly contradicts the bible's own closure mandate (Beat 14 vs. §0b/§5/Lock 5).**
   The bible states "The Rook Trilogy completes here" and describes Beat 14 as the count coming out
   "whole" — Lock 5: "Rook's arithmetic completes, it does not reset." The delivered Beat 14 text does
   have Soraya say "It came out whole," but the beat's last two beats of dialogue immediately undercut
   it: Soraya reveals the Marrow was 1 of 3 Helix facilities running the identical program, "the count
   isn't finished," and Rook's last written line on the page is "3 facilities," underlined — a sequel
   hook, not closure. As written this is not a finale resolving into a whole count; it's a first-act button
   for a fourth job. If that's the intent, it needs to be stated as an intentional amendment to "the
   trilogy completes here" — right now the beat and the bible's own governing claim about that beat
   contradict each other.

5. **A loudly planted mystery — "the forty-first name" — never pays off on-page (Beats 5, 8).**
   Adalemo's alliance beat is built around a named, weighted request: "First thing you do for me — you
   find me the forty-first name. The one you never read... that's the head nobody got paid for." Beat 8
   escalates it ("There's your forty-first... the one with no mark") but the beat withholds the actual
   name. No beat from 9 through 14 ever states who the forty-first name is. As the 14-beat spine stands,
   this is a Chekhov's gun raised twice with real emotional weight and never fired. (The orphaned
   BeatId-4919 material resolves it as Sefi Okonkwo — but that resolution directly contradicts Beat 3,
   where Sefi is already identified, present, and testifying in person; it can't also be true that she's
   an unmet, unbilled name Adalemo has to locate in Z6 eight months later. Neither version of "who is the
   forty-first" is currently consistent with the rest of its own draft.)

6. **Antagonist plan hole: Anneke's chase priority is asserted, not earned (Beat 12).** Rook's read of
   the confrontation depends on Anneke having "pivoted your guard off Sefi's column to chase one loud
   voice up the west/east core" — i.e., the executive chose to redirect security from her most valuable,
   most at-risk asset (21 live registered Reads) to chase down a single loose informant (Vox). For a
   character built as "a planner who priced people and never stopped" (§4), this trade only makes sense
   if exposure risk > asset value, and that calculus is never stated on the page — the reader has to
   infer it from Rook's accusation rather than see Anneke make the call. As-is it reads as the plot
   needing Anneke to be looking the wrong way, not Anneke's own logic producing that outcome.

7. **The climax resolves as a talked-down surrender with no counter-move (Beat 12→13).** A 30-year
   corporate operator who has "priced people and never stopped" folds inside a two-minute exchange when
   confronted with her own counting tic and one unpriced mercy. That's a legitimate beat for Anneke as a
   mirror to Rook, but there is no cost or complication returned from her side afterward — no
   retaliation, no re-sealing attempt, no institutional response beyond Helix's own PA system announcing
   the audit is "now, regrettably, public." Combined with finding 6, the antagonist's operational
   presence evaporates the moment the theme needs her to lose, which reads as a soft/unearned win for a
   finale-grade confrontation, even though Lock 6 ("the win is not clean") is nominally satisfied by
   Boiler's arm.

8. **Proportional cost is thin for a trilogy finale (Beat 13, Lock 6).** The entire visible cost of
   assaulting a corp-wide harvesting operation across three books' accumulated stakes is Boiler's
   arm and a bruise/dislocation on Vox. No deaths, no lasting loss among the eight named crew/allies, no
   capture. Lock 6 is technically met, but the size of the cost doesn't scale with the size of the
   target (an industrial atrocity, a Nano Triumvirate member, three facilities) — worth checking against
   whether the reader will feel the "not clean" claim or just the crew's competence.

9. **Duplicate/contradictory prose attached to the node (BeatId 4890–4967).** Regardless of which
   version is meant to be canon, having ~9,000 words of unlabeled alternate-draft prose linked into
   `NodeBeats` for this slug is itself a structural risk: any tool that reads beats by `NodeId` (export,
   review, coverage) will ingest both the finished spine and the abandoned draft, and the two disagree
   on who Sefi is, how the twenty-one/thirty-one are found, who "Yanneke" is (a named character absent
   from the bible entirely), and how the Anneke confrontation plays out. This needs to be resolved
   (pick one version, detach the other from `NodeBeats`) before any 90+ target is meaningful, since a
   reviewer or exporter pulling the full beat set will hit the same contradictions documented above,
   twice over, with different specifics each time.

## Verdict

Not structurally sound at 90+ as currently linked. The 14-beat spine alone has two severe,
citable contradictions — the MxG "21 in / 14 out" retcon (finding 1) and the Scout Husk/Shell reversal
(finding 2) — plus an unexplained antagonist-adjacent character fate (finding 3) and a finale that
undercuts its own bible's closure claim (finding 4). None of these are prose-level; they are plot facts
that disagree with other plot facts, either within CxC or against MxG's documented canon. A panel score
of 93.1 for this content most likely reflects sentence-level craft (voice, pacing, imagery are all
genuinely strong) rather than cross-book fact-checking, which a structural audit is built to catch and a
prose-quality panel usually isn't.

Recommended structural changes, in priority order:
1. **Reconcile Rook's Axiom-job wound with MxG's documented 5-person, no-casualty crew** — either retcon
   MxG on record (amendment) or change CxC's "21 in / 14 out / Wennick" backstory to a different,
   unclaimed incident that doesn't contradict the one Axiom job already on the books.
2. **Fix the Scout Husk/Shell contradiction** — Beat 11's "ejected from her own Husk and injected into
   Gerald" needs to be cut or rewritten; it directly negates the beat the whole arc was built on (Beat
   9–10: she is physically present this time).
3. **Either build the missing beat that explains Adalemo's fate, or remove the PEREGRINE framing from
   Beat 13** — as written it implies a body-snatch/conversion that never happens on the page and
   contradicts his established status as an ally standing next to Rook.
4. **Decide whether Beat 14 is a finale or a launchpad** — if the trilogy is meant to close, cut the
   "3 facilities" hook or reframe it as thematic epilogue rather than the literal last line on the page;
   if a fourth book is intended, update the bible's §0b/§5/Lock 5 language so it stops claiming closure
   it doesn't deliver.

Separately, and before any of the above: detach the orphaned BeatId 4890–4967 rows from `NodeBeats` (or
confirm which set is canon) so future review/export passes aren't scoring or shipping two incompatible
drafts of the same story under one slug.
