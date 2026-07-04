# Canon Sync Survey — Round 3 (2026-07-04)

**Status:** completed  
**Purpose:** Third contradiction-discovery pass. Primary target: the graviton physics retirement — 9 Technologies and 1 Weapon still use graviton/anti-gravity mechanics that directly contradict the canonical `Graviton Manipulation Theory` entry. Secondary targets: one remaining Z-number place, and Lotus Syndicate sizing post-Jade deprecation.  
**Apply protocol:** See `docs/SURVEY_PROTOCOL.md`.

---

## Q-001 — PLACE: Cauldron Industrial Nano Park — "Z8"

The Round 1–2 passes removed Z-numbers from 7 Places (Z1–Z5). One miss:

> "A 2.4-square-kilometer manufacturing complex **in Z8** where Cauldron Applied Sciences operates its heaviest industrial nano systems…"

**Choose one:**
- [x] a) Auto-fix — "in Z8" → "in the outer industrial sector" (same tier-language pattern as Round 2)
- [ ] b) Leave — minor internal record; low priority

**Answer:** a — Applied. `Places.Description` updated via REPLACE.

---

## Q-002 — TECHNOLOGIES: Graviton Physics Contradiction (9 entities)

`Graviton Manipulation Theory` already contains the canonical in-universe explanation:

> *"Detection was achieved; manipulation has not been. … The absence of anti-gravity in GLMZ is not a gap in the technology — it is confirmed by the infrastructure. GLMZ has space elevators (tensile and counterweight; no gravity cancellation needed) and mass drivers (electromagnetic catapult launch; physically coherent, no magic required). Both solutions were built because they work. Anti-gravity was not built because it does not exist."*

Nine other Technology entities directly contradict this by claiming to use working graviton manipulation. They fall into three natural groups:

**Group A — Lift & hover platforms (5 entities)**  
These provide lift/buoyancy using the same physics that ANGEL vacuum-cell tech replaced:
- `Floating Architecture Design Principles` — "buildings suspended by graviton emitters"
- `Arcturus Defense Solutions Specter Hover Combat Platform` — "graviton emitters to achieve terrain-independent mobility"
- `Crucible Industries Levitas Heavy Cargo Platform` — "12 graviton emitters to cancel gravitational mass"
- `Crucible Industries Orbital Elevator Graviton Assist` — "graviton-based mass reduction system" along elevator lower 100km
- `Ringo GravLounge Residential Anti-Gravity Suite` — residential room with variable-gravity graviton micro-emitters

**Group B — Manipulation tools & infrastructure (3 entities)**  
These claim to weaponise or industrialise graviton fields:
- `Crucible Industries GravWell Containment System` — "focused graviton beam that amplifies gravity up to 10G"
- `TESSERA SkyForge Construction Graviton Crane` — "focused graviton beam" to reduce weight of construction components
- `Zheng-Dao Heavy Industries Graviton Manipulation Framework` — foundational superconducting graviton emitter system

**Group C — Uncertain match (1 entity)**  
- `Crucible Industries Phalanx Building-Scale Deflector` — primary mechanism is electromagnetic; matched query because "graviton" appears late in the description. Likely a minor incidental reference.

**Choose one:**
- [x] a) Group A → vacuum-cell rewrites; Group B → retire; Phalanx → strip any graviton reference (3-way split matching natural categories)
- [ ] b) Bulk rename — replace "graviton" mechanism with "mass compression field" across all 9 (matches the Round 2 weapons fix)
- [ ] c) Retire all 9 — these products don't exist; Theory is the canonical proof they never worked
- [ ] d) Leave Group A and B for now — only graviton *weapons* were inconsistent; technologies are background texture and lower priority

**Answer:** a — Applied. Group A (5): Floating Architecture, Specter, Levitas descriptions rewritten to ANGEL vacuum-cell lift; Orbital Elevator renamed `Crucible Industries Orbital Elevator Electromagnetic Speed Assist` (OEGA→OESA) with EM linear motor description; GravLounge renamed `Ringo GravLounge Residential Buoyancy Suite` with ANGEL+dense-phase gas buoyancy description. Group B (3): GravWell, SkyForge, Graviton Manipulation Framework descriptions replaced with LEGACY ENTITY notices. Phalanx: "phase-synchronized graviton interference patterns" → "phase-synchronized electromagnetic interference arrays". Both Entities and Technologies tables updated for renames; DeprecatedEntityNames entries inserted for the 2 renamed technologies.

---

## Q-003 — WEAPON: GSP-1 'Aegis' Graviton Shield Projector

One defensive weapon was not covered in Round 2's graviton-weapons sweep (Q-006):

**`Arcturus Defense Solutions Graviton Shield Projector GSP-1 'Aegis'`**  
> *"An experimental defensive system that projects a localized gravity distortion field in a 3-meter hemisphere around the emitter, deflecting incoming projectiles by altering their trajectory through gravitational lensing."*

Round 2 retired three offensive graviton weapons (GCE-3, GL-3, GL-1) because graviton manipulation doesn't work in GLMZ. The GSP-1 uses the same physics defensively.

**Choose one:**
- [ ] a) Rename + rewrite — consistent with Round 2; "Graviton Shield Projector" → "Mass Compression Shield Projector"; description rewritten around mass compression fields
- [x] b) Leave as experimental — it's explicitly described as "experimental" and "consumes enormous power"; could survive as fringe-science tech on the bleeding edge
- [ ] c) Retire entirely — it can't work for the same reason the offensive weapons couldn't; remove the entity

**Answer:** b — No change applied. GSP-1 retained with experimental framing intact.

---

## Q-004 — FACTION: Lotus Syndicate — "Largest org" Claim

Round 2 Q-003 deprecated the Jade Syndicate and noted Lotus should "absorb the 'largest organised crime' claim." The current Lotus description is vivid and detailed, but does not explicitly state Lotus is the largest criminal network in GLMZ.

Relevant excerpt:
> *"In the vacuum left by Meridian's absent public sector, the Syndicate built a parallel governance of protection rackets, patronage, free clinics, smuggling, and a very firm understanding of who owns what. They are, in the most cynical and literal sense, the social safety net of the lower tiers…"*

**Choose one:**
- [x] a) Add sizing — insert one sentence establishing Lotus as the largest organised crime network in GLMZ, somewhere in the opening paragraph
- [ ] b) Leave — Lotus is clearly dominant in every detail; explicit sizing language is unnecessary and slightly blunt

**Answer:** a — Applied. Opening sentence updated: "The Lotus Syndicate is organized crime that would like you to know..." → "The Lotus Syndicate is the largest organised crime network in the Great Lakes Metropolitan Zone — organized crime that would like you to know..."

---

## Q-005 — TECHNOLOGY: `Graviton Manipulation Theory` — Name Ambiguity

This entity is now the canonical in-universe *explanation for why graviton manipulation doesn't work*. But its name — "Graviton Manipulation Theory" — reads as if it's a theory *about how to do it*, not a theory *that it can't be done*. Every other graviton technology in the DB refers to working products.

Should the name be revised for clarity?

**Choose one:**
- [ ] a) Rename — e.g., "Graviton Detection & Manipulation Research Programme" to make clear it is ongoing research with no engineering application
- [x] b) Keep as-is — "Theory" correctly signals speculation/aspiration rather than working tech; no rename needed

**Answer:** b — No change applied. Name retained.
