# Canon Sync Survey — Round 2 (2026-07-04)

**Status:** completed  
**Purpose:** Second contradiction-discovery pass following the Round 1 apply cycle. Targets entity descriptions, faction data, prose beats, and post-SS-A35 physics consistency.  
**Apply protocol:** See `docs/SURVEY_PROTOCOL.md`.

---

## Q-001 — PLACE DESCRIPTIONS: Zone Numbers (7 entities)

Seven Place entities have Z1–Z5 zone numbers embedded in their descriptions
(e.g., "in Z3", "in Z1's civic core"). Survey 1 Q-015 established GLMZ has
no official zone numbering — geography is expressed through named districts.

**Affected entities:**
- Axiom BioNanics Research Tower — "in Z3"
- NRA GLMZ Regional Office — "in Z1"
- Null Dynamics Counter-Nano Operations Center — "in Z5"
- Oma Nano-Therapeutics Precision Medicine Campus — "in Z2's medical district"
- Substrate Control Node Alpha — "in Z1's civic core"
- The Bloom Quarter — "between Z4 and Z5"
- The Chengdu Institute GLMZ Liaison Office — "in Z2"

**Choose one:**
- [x] a) Replace all 7 — remove Z-numbers, substitute named district or general descriptor
- [ ] b) Keep as internal informal shorthand — zone numbers existed informally in-world; strip any official-sounding framing ("GLMZ Zone 3 civic core")
- [ ] c) Leave as-is — inconsistency is minor; these are internal entity records, not prose

**Answer:** a — Applied. Z1→civic core, Z2→medical district, Z3→central research district, Z5→outer industrial tier. Bloom Quarter "between Z4 and Z5" → "boundary zone between the outer residential and industrial tiers."

---

## Q-002 — CHARACTER DESCRIPTIONS: "Street Samurai" Job Label (9 characters)

Nine Character entities use "street samurai" in their descriptions as a job
category. Survey 1 Q-007 established it is Kyle's nickname, not a generic job
title for operators.

**Affected characters:**
Akshara Shimizu, Bahman Guerrero, Daksh Bautista, Danjuma Calvillo, Dilek Haddad,
Divyansh Najjar, Farhan Vo, Kamila Arredondo, Sol Migizi.

**Choose one:**
- [ ] a) Replace — change to "operator/freelancer" in all 9 descriptions
- [ ] b) Keep — "street samurai" can be informal slang used loosely; not exclusive to Kyle
- [x] c) Partial — keep the phrase but frame it explicitly as informal slang, not a job category

**Answer:** c — Applied. "works as a street samurai out of [Location]" → "works as a freelance operator — street samurai by the trade's own shorthand — out of [Location]" across all 9 character descriptions.

---

## Q-003 — FACTION CONFLICT: Jade Syndicate vs Lotus Syndicate

The Jade Syndicate claims to be "the largest organized crime operation in GLMZ"
with roots in "pre-Meridian Pacific Rim organized crime networks." The Lotus
Syndicate is the dominant blood-purity E/SE Asian supremacist organisation.

Do these coexist as separate organisations, or is there a conflict?

**Choose one:**
- [ ] a) Separate — Jade is a broad Pacific Rim crime network (market-share focus, no ideology); Lotus is a supremacist splinter that broke off and now competes against Jade for E/SE Asian territory
- [ ] b) Lotus splintered FROM Jade — Jade is the older parent org; Lotus broke off over blood-purity ideology and took significant membership with it
- [x] c) Deprecate Jade — consolidate under Lotus; Lotus absorbs the "largest organised crime" claim; Jade is an old handle no longer in use

**Answer:** c — Applied. Jade Syndicate description replaced with LEGACY ENTITY notice + past-tense historical account. Description now opens: "LEGACY ENTITY -- Absorbed into the Lotus Syndicate following the Consolidation."

---

## Q-004 — FACTION NAME: Meridian Compact Stale Reference

The Meridian Compact faction description still contains "Greater Lake Michigan Zone"
(the retired name). Auto-fix to "Great Lakes Metropolitan Zone"?

**Choose one:**
- [x] a) Auto-fix — clearly a stale reference; apply immediately
- [ ] b) Leave for now

**Answer:** a — Applied. Meridian Compact description updated.

---

## Q-005 — WEAPON DESCRIPTION: GCE-3 Anti-Grav Cross-Reference

The Graviton Compression Emitter GCE-3 'Flatline' description says it
"draws on the same graviton manipulation research that underlies GLMZ's
commercial anti-gravity transit infrastructure."

SS-A35 retired anti-grav transit in favour of vacuum-cell (ANGEL) lift. This
cross-reference is now a broken canon link.

**Choose one:**
- [x] a) Remove the transit cross-reference only — keep the graviton weapon; just strip the broken link to anti-grav infrastructure
- [ ] b) Reframe — describe the foundational research as Ouroboros Energy pure-physics research, with no transit tie-in
- [ ] c) Leave as-is — entity description only, not prose; low priority

**Answer:** a — Superseded by Q-006 b. The entire weapon was renamed and rewritten; the transit reference was removed as part of the full description rewrite.

---

## Q-006 — WEAPONS: Graviton Physics Post-SS-A35

Three weapons use gravitational manipulation as their mechanism:
- GCE-3 'Flatline' (Graviton Compression Emitter)
- Arcturus GL-3 'Crush Depth' (Graviton Lens)
- Zheng-Dao GL-1 'Weight of the World' (Graviton Lance)

SS-A35 retired anti-gravity *transport*, but these weapons project localised force
fields rather than lift — a different application. Is graviton weapons tech
consistent with GLMZ physics?

**Choose one:**
- [ ] a) Consistent — weapon-grade graviton manipulation is experimental, controversial physics; distinct from transport lift; keep all three
- [x] b) Inconsistent — if anti-grav doesn't work for transit, weaponising it is equally implausible; retire all three or rename the mechanism
- [ ] c) Partial — keep the GL-3 and GL-1 (directed force, no transit tie); fix GCE-3 description only (it explicitly cited transit infrastructure)

**Answer:** b — Applied. All 3 weapons renamed and descriptions rewritten: GCE-3→MCE-3 'Flatline', GL-3→MCL-3 'Crush Depth', GL-1→MCL-1 'Weight of the World'. "Graviton" replaced with "mass compression field" throughout. Updated in both Entities and Weapons tables.

---

## Q-007 — FACTION DATE: The Amish Description Uses "2225"

The Amish faction description reads: "The last continuously functioning
organised Christian community in 2225." The canonical setting year is 2226.

**Choose one:**
- [x] a) Auto-fix to 2226 — off-by-one from the setting year
- [ ] b) Leave — 2225 is a historical data point, not a claim that 2225 is now

**Answer:** a — Applied.

---

## Q-008 — PROSE BEATS: "Rider" as Remote-Pilot Job Term (6 beats)

Six enabled prose beats use "Rider" to describe someone remotely operating a
machine while their body (Husk) sits elsewhere — the Exo function. Example:

> "a Rider, Husk slack in the cab seat while the machine did its own thinking,
>  the operator haunting the frame from somewhere else entirely" (CF0F5987)

Survey 1 Q-005 confirmed Rider is a retired job title. The replacement is Exo.

**Beat IDs:** 019EC96D, DB888A00, CF0F5987, 5E05F5F1, and 2 others.

**Choose one:**
- [x] a) Replace all 6 — swap "Rider" → "Exo" in the specific occurrences; preserve surrounding prose
- [ ] b) Leave prose — committed text; "Rider" may survive as older street slang even if the formal term is Exo
- [ ] c) Replace only third-person uses; leave any character using the term about themselves

**Answer:** a — Applied to 4 confirmed beats (019EC96D, DB888A00, CF0F5987, 5E05F5F1) via MCP update_beat_text. 2 query matches were false positives (lowercase "rider" = bicycle courier in MNEMO beat 019EECDC and one other); those were left unchanged.

---

## Q-009 — CHARACTER ENTITY: Pixel Missing Exo Designation

Pixel's character entity description covers personality and appearance but
does not mention she is an Exo — able to bond to and remotely pilot machines,
projecting her consciousness while her Husk sits inert. This is her defining
operational capability.

**Choose one:**
- [x] a) Add Exo designation — critical character attribute; add a brief note to the entity record
- [ ] b) Leave — character descriptions are not ability sheets; her narrative arc conveys this
- [ ] c) Add a capability line without leading with it — mention it as part of her operational profile, not her first identifier

**Answer:** a — Applied. New paragraph appended to Pixel's character description: "She is an Exo. When she slips into a machine, her body goes still in whatever chair is holding it, and the hardware becomes her eyes and her hands. She has ridden courier drones, construction rigs, a salvage claw she borrowed once from a yard in the Gray Zone and returned without a scratch. She reads machines the way her mother read bodies: fluently, without having to think about the grammar of it. She does not talk about this much. It is not the part of her that needs explaining."
