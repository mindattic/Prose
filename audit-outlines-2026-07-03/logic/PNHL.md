# PNHL Logic & Continuity Audit — "Pinhole"

**Node:** 019EA46A-17CB-7077-909B-11825BA5CFFC | **Beats audited:** 22 enabled, SortKey 100–1300
**Bible:** `docs/nodes/PNHL.md` (updated 2026-07-03) | **Audit date:** 2026-07-03 | Report-only, no writes made to DB/code/docs.

## Overall Verdict: PASS WITH MODERATE ISSUES

The final 22-beat sequence is causally tight and the vast majority of it is excellent, deliberate
continuity engineering — the routing-gap thread, the Assessor's sabotage escalation, and especially
the Cotter evidence chain are all properly seeded before they pay off. There is exactly **one
finding that meets the BLOCKER bar by definition** — Kyle's line is stated as "four words" twice in
the prose itself (Beat 1175 and Beat 1300) while the actual quoted dialogue, `"Give em hell,"` is
three words. It is a hard, self-contradicting, citable bug, but it is a one-line fix (add a word to
the line, or correct the two narration references to "three words"). Two further MODERATE items
concern under-shown setup for the confrontation's dead-man's-switch mechanism and a stale bible
spine (§10) that never absorbed the Cotter subplot or three other now-shipped beats. Everything
else — knowledge states, timeline, the five other narrative locks, and orphan-scan for old-plot
residue — checks out clean.

---

## (a) Causality

Full arc traced: Invitation (900) → Dinner (950) → lockout/"first courtesy" (975) → Elevator Shaft
bribe (1000) → Pattern's three incidents: routing-table poison, escrow/safety-flag freeze, manifest
redirect (all in 1050) → The Address (1060) → ACS (1075) → Cotter discovery (1090) → What Staying
Requires (1100) → Preparation (1150) → Kyle (1175) → Confrontation (1200) → Finale (1300).

**Clean:**
- Every counter she throws at the Assessor in the Confrontation (1200) traces to an established source: the routing architecture used to find his office (900, 1000, 1050, 1060), the receiving-counter node she burns (mapped in 1060: "She'd mapped its access chain the day she walked out with her coupling"), and the Cotter leverage (1090). No effect appears without an established cause.
- The Assessor's escalation (door lockout → bribed teammate → routing-table/escrow/manifest sabotage → office confrontation) stays within his established methods (deniable, financial, infrastructural). No sudden new power.
- First-suspicion-to-confirmation arc is properly staggered: suspicion at 975, forensic confirmation (matching routing signature) at 1000 — not asserted all at once.

**Findings:**

| Severity | SortKey(s) | Finding | Fix |
|---|---|---|---|
| MODERATE | 1150, 1200 | The "six channels / scheduled cancel-or-it-sends" dead-man's-switch — the mechanism she leverages to force the Assessor's hand — is only described in dialogue at the Confrontation (1200: "It's staged across six channels I control..."). Preparation (1150) mentions the dossier being "staged and ready to project" but never shows her building the six-channel send/cancel system itself. | Add one sentence to Beat 1150 showing her configuring the multi-channel dead-man send (e.g., staging the dossier across six independent drops with a scheduled auto-release she must cancel), so the mechanism exists on the page before she cites it. |
| MODERATE | 975, 1200 | At 1200 she "aims" the Assessor's own "management-tier credential mechanism" at his receiving-counter node to kill it in nine seconds. Beat 975 only shows her *reading a cached access log* of that mechanism after it was used against her — reverse-engineering it into an offensive tool is a real capability jump not explicitly shown being built. | Add a line (in 1050 or 1150) indicating she reverse-engineered/cloned the credential-override protocol from the log, giving the confrontation move an on-page technical precedent rather than an implied one. |

## (b) Knowledge States

- **First connects sabotage to the Assessor as a single antagonist:** suspicion at **975** ("First Contact Is the Last Courtesy" — she infers it's him/his crew after the door lockout), forensic confirmation at **1000** ("His mark. Invisible unless you already knew where to look, and she did.") via the Ryokan routing signature logged back in Beat 800. Properly staggered, no break.
- **Assessor learns she's onto him:** only confirmed on-page at the Confrontation itself, **1200** ("Pixel," he said. "I wondered when."). Since the story is close-third on Pixel throughout, there is no earlier beat that could show his awareness — this is not a violation, just the natural limit of POV.
- **Dead-man's-send mechanism vs. its use:** NOT clearly established before use — see MODERATE finding in (a) above (SortKey 1150/1200).
- **Cotter evidence chain:** rests entirely on tools she is shown having access to earlier — gray-zone equipment lien/registry lookups, manifest tracing (already used on her own redirected coupling in **1050**), and reputation-ledger access (she checks her own ledger in the same beat, **1090**, immediately after building Cotter's). No unearned tool appears. Clean.
- No other instance found of a character acting on information the text hasn't shown them acquiring.

## (c) Timeline

Best-effort reconstruction: Day 1 — boards Pulse 05:47 Iowa (100); Blur transit (200/250); arrives GLMZ, walks to the Pivot near dusk (300/400); fixes climate system, gets room (500); first night, restless until ~02:00 (600). Day 2 (or shortly after) — West Town Market trip (700); ghost-op relay job (800) some days later. Invitation arrives "four days after the relay job" (900); dinner is same evening, 21:00 (950); door-lockout same night (975). Elevator Shaft (1000) some time after. Pattern's three incidents span "two weeks" (1050), immediately followed by The Address (1060) same night. ACS (1075) and Cotter research (1090, "took most of an evening") follow in short order. Confrontation (1200) is explicitly "three days" after the Address visit per her own line at 1200 ("I stood at his counter three days ago"). Total elapsed time by the finale is consistent with the bible's and the text's own framing of "a little under two months" (1100).

**Finding:**

| Severity | SortKey | Finding | Fix |
|---|---|---|---|
| MINOR | 700 | Beat 600 ends by framing the market trip as happening "in the morning," her "first morning" in the city — but Beat 700 states she'd "been in GLMZ for thirty-seven hours," which (counting from the 05:47 Iowa boarding) lands in the afternoon/evening of Day 2, not first-thing morning. Arithmetically reconcilable (she could have run errands later that day) but the two framings sit awkwardly next to each other. | Either drop the specific "thirty-seven hours" figure or adjust Beat 600's "in the morning" framing to something looser ("the next day"). |

No impossibilities found — all other duration markers ("four days," "two weeks," "three days") are mutually consistent and fit inside the "under two months" total.

## (d) Locks

| # | Lock | Status | SortKey(s) | Notes |
|---|---|---|---|---|
| 1 | Nit never coveted/stolen | **HOLDS** | 100, 200, 250, 500, 600, 1150, 1175 | Nit appears only as her instrument; no character ever expresses interest in it. |
| 2 | Pixel solves it alone | **HOLDS** | 1075, 1175, 1200 | ACS explicitly does not help (1075); Kyle gives four (sic — see #3) words and closes his door (1175); confrontation is solo (1200). |
| 3 | Kyle: one beat, four words | **BLOCKER** | 1175, 1300 | Kyle appears on-page in exactly one beat (1175) — that half holds. But the quoted line is `"Give em hell,"` — **three words**, not four — and the prose itself asserts "four words" twice: at 1175 ("Four words carried no inflection...") and again at 1300 ("given her four words"). This is a direct, self-contradicting textual bug, not a matter of interpretation. |
| 4 | Routing gap left unerased | **HOLDS** | 1300 | "The routing log was still there... She closed the lid without erasing it." |
| 5 | Boots: padded→unpadded, noticed | **HOLDS (with a note)** | 100 (padded), 1150 & 1300 (unpadded, noticed) | The noticing is done by Pixel herself, not a third character — this matches the bible's own design (§1: "she notices... hadn't noticed until now"), so it satisfies the lock as authored. Flag only if a third-party remark was actually intended; nothing in the bible calls for one. |
| 6 | No dress/makeover beat | **HOLDS** | 900 | Explicitly rejected on-page: "She did not buy anything for the evening... she was not going to become a different person to sit at somebody else's table." Clean, no violations anywhere in the 22 beats. |

**Fix for #3:** Either add one word to the line so it scans as four ("Give 'em hell, kid." / "Now give 'em hell." / "Go give 'em hell."), or correct both narration references (1175, 1300) from "four words" to "three words." Given the bible states the count as a deliberate, load-bearing detail (§0, §8, §9 all cite "four words"), the cheaper fix is almost certainly to add a word to the line rather than rewrite the bible's repeated framing.

## (e) Orphans

Scanned all 22 beats for: an old theft/MacGuffin plot, a removed "messenger" character, and "two-Kyle" material.

- **No messenger residue** — the invitation arrives as an unsigned slip of paper under the door (900), matching the bible's explicit note that there is no messenger beat.
- **No two-Kyle material** — Kyle appears in exactly one beat (1175); no second instance or duplicate anywhere.
- **No old theft/drone-MacGuffin residue** — the Assessor's dinner pitch (950) is entirely about recruiting *her*, never her drone; no character ever asks for Nit or her chassis design.

This section is clean. No orphaned references to the retired plot found.

## (f) Bible Agreement — "at least two" (§8)

Bible §8: *"At least two of his past recruits actually agreed — the registry shows two; the true number may be higher."*

Cross-checked against the story text:

- **Yes-side, explicit count of two:** Beat **1060** — "She didn't know if he was one of the two names she'd have found if she'd kept digging in the gray-zone registry, the ones who'd looked at the offer and signed rather than left." Beat **1100** — "She didn't know for certain that he was one of the two who'd looked at the offer and signed rather than left. She'd started to suspect it didn't matter whether he was one of exactly two or one of considerably more." Both match the bible's "registry shows two; true number may be higher" exactly.
- **Cotter, distinct no-side figure:** Beat **1090** — the market kid's earlier anecdote (700, unnamed "guy") is confirmed to be Cotter, who said no and was punitively erased (bench sold at a loss, manifest bought by a stranger, incompletes logged post-departure). He is a single, clearly distinct "no" data point — the story never conflates him with the "two" who agreed.
- **Receiving-counter man, distinct and deliberately unresolved:** Beat **1060/1100** — the story explicitly declines to confirm whether he is one of the "two" or a data point suggesting the true number is higher, consistent with the project's "keep whodunits open" convention. This is a correct, intentional ambiguity, not a contradiction.

No numeric contradiction found. This section passes cleanly.

**Documentation-sync note (MODERATE, process not story-logic):** the bible's authorial spine (§10) is an 18-beat outline that predates the shipped 22-beat text. It has no entry at all for Beat 1090 (Cotter/"The One Who Said No") — the single largest addition in the final draft — nor for Beats 250, 1060, or 1075 as their own spine entries. Per the bible's own stated rule ("When prose and spine disagree, fix one in the same change"), §10 should be updated to reflect the shipped beat list. This does not create any story-logic problem — Cotter's material is well-seeded and consistent — it is purely a canon-doc gap.
