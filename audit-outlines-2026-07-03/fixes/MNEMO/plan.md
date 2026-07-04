# MNEMO — Structural Fix Plan (2026-07-03)

Node: `mnemosync-019ee11e` (`019EE11E-6AE8-711D-B12D-530FF2497399`), kind=story, no ChapterNode
children — all 27 "chapters" are flat Beats on the story node (NodeBeats.SortKey ordered),
confirmed live via read-only query. **Files only. No DB writes performed or intended by this
plan's authoring pass** — SortKeys/Ids below are the values to use *when* someone applies these
beats to the DB.

Source of truth for the live reading order used below: `NodeBeats` join `Beats`, ordered by
`SortKey`, restricted to the block `SortKey 1500–4100` (the 27 "Chapter N outline" + renamed
beats). The `SortKey 100–1400` block (`The Anomaly Log` → `Zone 7`, 14 beats) is a superseded
compressed draft mixed into the same node and is NOT part of the live chapter sequence — confirmed
by cross-reading every title/content pair against the audit's reconstructed 1–27 outline. It is
the source of the orphaned Ekow scene (see Fix 2).

---

## Fix 1 — Deliver the locked §7 finale

**Path chosen: variant of (a) — full removal and replacement, not a hook.** The anonymous-caller
/ "Glooms" thread is not woven into the book anywhere except its own last two beats (SortKey 4000,
4100). It has zero setup in the preceding 25 chapters — no prior mention of "the Glooms," no prior
mention of an unidentified viewer, no prior mention of segments that didn't run. Per the task's own
test ("if the Glooms thread is too woven-in to remove cheaply, integrate beneath the locked
finale"), it is not woven in at all, so the cheapest *and* most structurally sound path is to
remove it outright rather than preserve it as a coda hook that would require inventing its own
justification from nothing. If a series hook is wanted later, it should be planted earlier in a
future revision pass (e.g., a single foreshadowing line in Act 2), not grafted onto the ending it
currently replaces.

**What's kept:** The beat at SortKey 3900 (Id `019EE125-2F36-722E-8CA2-35D8984E4791`, DB title
"Chapter 26 outline") — Nuru alone in her kitchen, the radio memory landing with its full weight
for the first time in six weeks — is genuinely good, in-voice, and *is* part of the locked design:
it's the direct, page-one payoff of Seto's reverse-transfer attempt in "The Procedure" (SortKey
3800), which ends on the exact same beat ("a woman was standing at a counter with a glass of water
she had not drunk"). No rewrite needed. Recommend retitling it "The Weight Returns" when applied to
the DB (content unchanged). It stays at SortKey 3900.

**What's replaced:** SortKey 4000 (`019EE125-4FB9-7A76-A817-CBD3F29B277B`, "Chapter 27 outline" —
the mystery-caller phone call) and SortKey 4100 (`019EE125-710C-760A-B3E5-A1CDBC5639FB`, "Chapter
28 outline" — Amara leaving to meet him at the Meridian Fold) are replaced wholesale with the three
chapters the bible's §7 actually locks: **Different Frequency** (Amara — aftermath, Phase II
ships), **Zone 7, Tuesday, 9AM** (Seto — Nuru's non-appearance), and **10:47** (Amara — the
fountain reunion, book close). This nets the book to 28 chapters (one more than the current 27),
which restores the pre-"Second Suitor merge" Act 3 chapter count the bible's own §7 language
("Ch27/Ch28... now Ch26/Ch27") already anticipates.

One deliberate deviation from the bible's literal staging: §7 says Ch28 is "whoever Amara or Seto
sees in the lobby at 10:47." The task brief asks for a fountain reunion where Amara and Seto
physically meet on the page — the two-hander's entire promised payoff. I've reconciled these by
staging Facility C-9's public plaza fountain across from the lobby entrance as the vantage point:
Seto watches the lobby from the plaza through the 9AM window (he can't enter without flagging his
courier tag per the Ch24 mechanism), and Amara arrives at the fountain at 10:47 to find him still
there. They confirm Nuru's non-appearance together, in person, for the first sustained
non-checkpoint scene since "The Move." This satisfies both the antagonist-defeat beat (the absence
that means everything) and the reunion the format has been building toward for 27 chapters.

| SortKey | Action | Beat | POV | File |
|---|---|---|---|---|
| 3800 | none | The Procedure (existing) | Seto | — |
| 3900 | keep, retitle only | The Weight Returns (ex-"Chapter 26 outline") | Nuru | — |
| 4000 | **REPLACE** | Different Frequency | Amara | `07-different-frequency.md` |
| 4100 | **REPLACE** | Zone 7, Tuesday, 9AM | Seto | `08-zone-7-tuesday-9am.md` |
| 4200 | **NEW** | 10:47 | Amara | `09-1047-reunion.md` |

---

## Fix 2 — Restore Ekow Ato's introduction

**Orphaned draft found and salvaged.** Beat `019EE11E-70D5-7DA8-820A-3AB719982165` ("Minimum
Footprint," 1,909 chars, SortKey 100–1400 compressed-draft block) is the missing scene. It is
already in Seto's voice, already contains the locked details (machete at the hip, handkerchief
folded in quarters, "seven days," the VATD-established minimum-footprint doctrine), and the
dialogue is clean and usable almost verbatim. It runs ~300 words as stored — too thin to stand as
its own chapter next to the rest of Act 2's chapter lengths, and it has no bodily-response beat
(voice rule: one involuntary bodily response per scene, body before mind). I expanded it to ~700
words: added arrival/sensory setup, one involuntary physical beat (Seto's weight shifting to his
back foot before he consciously registers the threat), and a slightly fuller close that bridges
into the investigative momentum Ch11 assumes already exists. The dialogue itself — the machete,
the handkerchief, "seven days," "walk away from the Orison work" — is preserved unchanged; it was
already correct.

**Placement:** new beat, SortKey 2450, between "Chapter 10 outline" (What She Knows, SortKey 2400,
Act 1 close) and "Chapter 11 outline" (The Cleanup List, SortKey 2500, Act 2 open). This is the
earliest point consistent with Ch11's internal timeline — Ch11 states "Four days. Ekow Ato had
given him seven... and three were already spent" partway through its own action, meaning Ekow's
warning must land several days before Ch11's opening scene, i.e., at the Act 1/Act 2 seam. Filed
as `04-minimum-footprint.md`.

---

## Fix 3 — Dramatize the razor payoff on-page

**Diagnosis confirmed against source, and it's worse than the audit's summary suggests:** the
bible assigns Ch19 ("The Turn") to Amara — "Forced leave; razor payoff (open blade); platform
loss." The beat actually stored under that title (`019EECF2-F2BD-7D8F-9E5A-585FAAAD3E2A`, SortKey
3300) is not a thinner version of that scene — it is a completely different scene, in Seto's POV,
about his insomnia and a Zone 4 dead-drop plan. Ciro never appears in it. The forced leave and the
razor are then relayed a chapter later in "Story Runs" (SortKey 3400) as two lines of dialogue
summary ("She told him about the razor open on the desk, the phrase 'loss mitigation'...").

**Fix:** new beat, "The Turn" (Amara, Ciro, on-page), inserted at SortKey 3250 — after "Pressure
and Counter-Pressure" (3200) and before the existing Seto beat currently mistitled "The Turn"
(3300). ~950 words: Ciro delivers the two-week forced leave in person, opens the bone-handle razor
established in "Account Liaison" (Ch5) as a checking gesture before the ultimatum, says "loss
mitigation," provisions and then narrates exactly what he's revoking. One involuntary bodily
response (Amara's hand, before her mind catches up to what she's looking at) and one bleed-
intrusion sentence from Seto, per the Act 2 rate (one per Amara chapter). Filed as
`05-the-turn-razor-confrontation.md`.

**Existing beat at SortKey 3300** (Seto's insomnia/dead-drop planning): keep the content unchanged
— it doesn't contradict anything, it just needs to stop being titled "The Turn" since that title
now belongs to the new scene. Recommend retitling it "No Clean Line" (its own opening line) when
applied to the DB. No SortKey change needed; 3300 already sits correctly after the new 3250 scene.

**"Story Runs" patched** (SortKey 3400, `019EE124-80E2-7C5A-AB1B-961BA4FF98C1`) to remove the
double-tell: the paragraph that re-narrates the razor/loss-mitigation/wrong-coffee details is
replaced with a shorter exchange that acknowledges the leave without re-describing it (Seto already
felt it arrive through the bleed before she says the word for it), preserving the scene's actual
function — confirming they're both now committed — without repeating information the reader has
already seen on the page. Filed as `06-story-runs-patch.md` (full chapter, patch applied).

This also resolves the audit's Finding 7 (Ciro vanishing for the back half of Act 2) as a direct
side effect: Ciro now has an on-page appearance at SortKey 3250, later in Act 2 than his previous
last appearance (Off Timing, SortKey 3100). No separate "presence beat" was needed once the razor
scene existed on-page.

---

## Fix 4 — Unify the surveillance mechanism

Both patches keep the "in-person visit + associative-node suppression sub-protocol" mechanism
(§1, LOCKED) as the one true account, and reframe the two outlier mechanisms as consistent with it
and with Ciro's later-established methods (proximity pings, batch wellness reports, portal
visibility) rather than as unexplained one-off capabilities.

**Ch1 (SortKey 1500, `019EE121-BBD0-7114-A642-2EF9CEBC03BE`) — "The Log She Didn't File."** The
original has an unnamed process reaching into Amara's account and *executing a query she never
ran*, mid-commute, and forwarding it to Orison — an active remote-command capability with no other
appearance anywhere in the book. Patched so the query is hers: a standing background check she
built eight months ago (matches her private-log habit established the same paragraph) that
re-runs automatically and was always going to sync through the partnership portal in real time,
because the portal is Orison's, not hers. She "didn't run it" that evening in the sense that she
didn't consciously trigger it — but the infrastructure making it visible to Orison is something
she built and forgot, not a mystery intrusion. This preserves the scene's function (real-time
visibility into what she's investigating, discovered the same night) while removing the
implausible mechanism and tying it to the same "portal visibility" logic Ciro relies on later.
Filed as `01-the-log-she-didnt-file-patch.md` (full chapter, patch applied to one section).

**Ch6 (SortKey 2000, `019EE122-7559-736D-9C5E-CAC8DB8FBA4D`) — "Something Borrowed."** The
original has a calibration visit fabricated for a date/zone Amara's transit logs prove she never
physically visited, with her own certification ID nonsensically filed in the technician-of-record
field. Patched so the visit is real and in-person: her transit log did register a Zone 8 transfer
that day, logged under a corridor code she'd never bothered to decode as a zone name (so she
initially misses it, then finds it) — she was there, in the chair, and remembers nothing of it
mattering, which is exactly the Daud mechanism (§1: "the memory remains, the weight does not")
applied to herself for the first time. The impossible "technician field" becomes a plausible
paperwork detail: Orison auto-enrolls press-credentialed subjects as their own file's sign-off
reviewer, a shortcut that means no second person's name has to appear next to a segment subject's
record. Same horror (the record is intact and says nothing is wrong), no impossible mechanism.
Filed as `03-something-borrowed-patch.md` (full chapter, patch applied to one section).

---

## Fix 5 — Cheap remaining items

**Seto's unpaid grief plant (Finding 6, Ch3 "Interference Pattern," Fragment 22) — fixed, cheaply,
by reattribution rather than deletion.** The original fragment reads as grief for someone lost
("someone missing a person they had not stopped missing"), with a two-syllable held-low name,
and never connects to anything — Seto's only established emotional tie is Nuru, alive, and
elsewhere described as "gravity" not loss-grief. Patched the fragment to read as protective
vigilance for a living person rather than mourning for an absent one — compatible with Nuru
(two syllables), unnamed at this point in the book, paying off quietly when the reader later
learns her name. This turns a dangling, contradicted plant into a small piece of early,
un-flagged foreshadowing instead of cutting it outright. **Bonus, same paragraph:** the original
also uses "Rider" (retired per SS-A38) and a "Husk slack, slotted into the rig" description that
doesn't match the established Husk=dormant-body/Shell=inhabited-machine vocabulary for a live
human camera operator — both are removed in the same edit since they're in the exact sentence
being patched. Filed as `02-interference-pattern-patch.md` (full chapter, patch applied to one
section).

**Ciro vanishing for the back half of Act 2 (Finding 7)** — resolved as a side effect of Fix 3
(see above). No separate beat needed.

---

## Explicitly skipped (flagged, not fixed — out of this pass's budget)

1. **Ch12 content mismatch** (bible's Act 2 Ch12 "Managed Liability" — Amara/Ciro editorial-contact
   scene where "razor trimmed to static presence" — is entirely absent; the stored beat at SortKey
   2600 is Seto POV with no Ciro or editorial content). This is a real gap and part of the same
   razor motif chain the bible describes ("Account Liaison intro → The Lunch reading → Grooming →
   The Turn payoff"), but it isn't one of the four mandatory items or the two flagged "cheap if
   possible" items in this task's brief, and restoring it well means writing a new scene from
   nothing rather than patching or salvaging one — a larger job than the budget here allows.
   Recommend a follow-up pass.
2. **"Grooming" chapter underlength** (Finding 9) — bible's amendment log says the ex-"Second
   Suitor" gathering-warmth → floor-32 provisioning-trap arc was merged into the front of
   "Grooming," but what's stored is a ~300-word phone call with no floor-32 scene at all. Not in
   this task's mandatory or cheap-item list; flagged for a follow-up pass alongside Ch12.
3. **Ch18/Ch19 POV-label vs. content mismatches predating this fix** — the bible's chapter-map
   table assigns Ch18 ("Pressure and Counter-Pressure") to Seto and Ch19 ("The Turn," pre-fix) to
   Amara; the content stored is the reverse (Ch18 is Amara's train reflection, pre-fix Ch19 is
   Seto's insomnia scene). This plan does not touch "Pressure and Counter-Pressure" — it's a
   pre-existing bookkeeping drift, not something this fix's scope requires resolving, and the new
   Ch19 razor scene I've added is Amara POV per the bible regardless of the table's Ch18/19 label
   swap.
4. **Finding 10** (no protagonist ever turns the diagnostic question on themselves) — not
   addressed directly; the Ch6 patch (Fix 4) incidentally gives Amara a partial version of this
   moment (finding her own unfelt visit) as a side effect, but a full fix would require its own
   beat. Flagged, not built.
5. **Finding 8** (Ch7–9 are one scene split into three padding chapters) — untouched; out of scope
   for a finale/structural-hole pass, would need its own consolidation pass.

---

## File manifest

| # | File | Action | Target/New Beat Id | SortKey |
|---|---|---|---|---|
| 1 | `01-the-log-she-didnt-file-patch.md` | PATCH | `019EE121-BBD0-7114-A642-2EF9CEBC03BE` | 1500 |
| 2 | `02-interference-pattern-patch.md` | PATCH | `019EE122-0958-72C8-AA4D-48E570B00376` | 1700 |
| 3 | `03-something-borrowed-patch.md` | PATCH | `019EE122-7559-736D-9C5E-CAC8DB8FBA4D` | 2000 |
| 4 | `04-minimum-footprint.md` | NEW | (new) | 2450 |
| 5 | `05-the-turn-razor-confrontation.md` | NEW | (new) | 3250 |
| 6 | `06-story-runs-patch.md` | PATCH | `019EE124-80E2-7C5A-AB1B-961BA4FF98C1` | 3400 |
| 7 | `07-different-frequency.md` | REPLACE | `019EE125-4FB9-7A76-A817-CBD3F29B277B` | 4000 |
| 8 | `08-zone-7-tuesday-9am.md` | REPLACE | `019EE125-710C-760A-B3E5-A1CDBC5639FB` | 4100 |
| 9 | `09-1047-reunion.md` | NEW | (new) | 4200 |

All beats attach to node `mnemosync-019ee11e` (`019EE11E-6AE8-711D-B12D-530FF2497399`) — no
ChapterNode children exist to attach to instead.
