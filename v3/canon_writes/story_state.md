# Story State — working scratch for this session's canon edits

Last updated: 2026-05-16 (live).

## Canon spec status

### Weapons

**Silence** (CD8CE222-DE5F-44C4-B6F6-5C18721C1050) — **REWRITTEN 2026-05-16**
- Matte-black mono-edged katana, carbon-nanotube composite blade, 71cm blade / 102cm total / 0.94kg / tungsten-carbide tsuba.
- NO electronics. NO piezoelectric harvest. NO supercapacitor. NO neural-disruption layer. NO electroluminescent hamon. NO discharge of any kind. NO cardiac-reboot trick. NO strop. NO glow at any charge level.
- Edge maintenance = check carbon-nanotube fractures daily by hand. Edge chips, does not dull.
- The myth on the street (it shorts BCIs, etc.) is just that — a myth. Neither Seo nor Kyle has ever corrected it.
- Aliases: Silence, the blade, Nari, the Graunch.
- Manufacturer: Dae-jung Seo (custom, deceased).

**Chorus** (4AB24F74-61D4-4F45-B326-7C6B98C96279) — **REWRITTEN 2026-05-16**
- Five-chamber revolver shotgun. Bird's-head grip, no stock. 12-gauge.
- Reload via moon clip — ~3-6 seconds. Five rounds and that's it until reload.
- Specialty loads: buckshot, slug, flechette, rubber slug, electric bola, inductive disruption, signal flare. Kyle pre-loads moon clips by mission.
- Manufacturer: Torii Security Group (TSS-3 line, Tier 3 commercial).
- 480mm total, 360mm barrel, 2.1kg unloaded.

### Retired support tech (archived 2026-05-16)
These technology entities existed only to underpin Silence's old powers:
- Cascades TNG-7 Blade-Surface Triboelectric Nanogenerator Film
- Graphene Ultracapacitor Tsuka Module
- Nakago Electrode Interconnect Harness (2 dupes archived)
- Piezoelectric Shingane Core (PZT/Carbon-Fiber Composite)
- PZT Piezoelectric Composite Shingane Core
- TENG Triboelectric Nanogenerator Mune Film
- Plus likely a "Silence technical schematic" document — still need to verify/handle

### Transportation
- Kyle's motorcycle — canon as of 2026-05-16. Entity row TBD this session. Default: matte black, unbranded, kept at ground level outside The Pivot or wherever Kyle's last stop was.

## Bushido Coda chapter spine (committed 2026-05-16)

| # | Title | Beats | Status |
|---|---|---|---|
| 1 | Bearing Teeth | 0 | Synopsis: updating now to remove Silence powers |
| 2 | Day in the Life | 0 | NEW — synopsis-only, no prose yet |
| 3 | A Restless Mind | 3 (Watching, Walk, Pixel) | Synopsis: needs update — old version describes content that moved to Inside the Cage |
| 4 | Inside the Cage | 5 (Recognition, Market, Tier 1, Courtyard, Thanks) | NEW — synopsis-only, beats inherited from old A Restless Mind |
| 5 | The Rogue AI | 0 | Unchanged |
| 6 | The Interview | 11 (stubs) | Unchanged |
| 7 | Street Meat | 0 | Unchanged |
| 8 | A Borrowed Hand | 10 | Unchanged — still has Silence-powers prose that needs scrubbing |

New chapter GUIDs:
- Day in the Life: 367fdf7f-9760-4712-9f30-402a647d05d7
- Inside the Cage: cf64fefc-01e9-4ba9-8ec1-b760c8b9398d

## Beats needing prose rewrite (Silence powers + atmosphere/humor pass)

**Status as of session end:**
- A Borrowed Hand:
  - Beat: The Cleaver, the Sword, and the Drop — **CLEAN** (the "strop" SQL-LIKE hit was inside "catastrophic" — false positive)
  - Beat: Stitched Back Together — **CLEAN** (same false-positive)
  - Beat: Eighty-Five Thousand — **FIXED 2026-05-16** (replaced "the hamon is cold blue and the bank is empty" with "the saya is wet from the trench")
  - Beat: Rain, Then Anesthesia — **FIXED 2026-05-16** (replaced the long hamon/bank passage with "the saya wet and dark in the rain. The blade was always the point.")
- A Restless Mind:
  - Beat: Watching A Woman Pretend Not To Watch Him — **CLEAN** (no actual forbidden terms after refined scan; original "electric_AND_silence" SQL hit was a false positive on "electrostatic" or similar substring)
  - Beat: Pixel — **MOVED to Inside the Cage** (the fragment-extraction + piezoelectric-ceramic content makes sense as cage-aftermath, not noodle-stall-meeting aftermath; A Restless Mind now has 2 beats, needs a new closer written for the cup-of-tea-doorway scene from the synopsis)

## Chapter HTML rewrites (full bodies) — FINAL STATE 2026-05-16

| # | Title | Synopsis | Chapter Html | Beats | Forbidden-term scan |
|---|---|---|---|---|---|
| 1 | Bearing Teeth | rewritten | 33k chars (full rewrite) | 0 (Html is the body) | CLEAN |
| 2 | Day in the Life | new | 18k chars (new prose) | 0 | CLEAN |
| 3 | A Restless Mind | rewritten v2 | 17k chars (assembled from 3 beats) | 3 beats | CLEAN |
| 4 | Inside the Cage | new | 18k chars (assembled from 6 beats) | 6 beats | CLEAN* |
| 5 | The Rogue AI | unchanged | 14.8k chars | 0 | CLEAN |
| 6 | The Interview | rewritten | 38k chars (full rewrite) | 11 empty stubs | CLEAN |
| 7 | Street Meat | unchanged | 57.5k chars | 0 | CLEAN |
| 8 | A Borrowed Hand | unchanged | 89k chars (patched) | 10 beats | CLEAN |

*Inside the Cage has one `piezo` substring hit from the moved Pixel beat — "piezoelectric ceramic" as a workshop component Kyle hands Pixel, unrelated to Silence's mythology. Canon has many non-Silence piezoelectric tech entities; this is a benign worldbuilding detail, not a Silence-power reference.

**All 8 chapters are now canon-consistent.** Every reference to Silence-as-energy-weapon has been removed; every reference to Chorus-as-magazine-fed-shotgun has been updated to revolver-action with moon clips; the motorcycle, vertical-Chicago atmosphere, laugh-or-cry humor, and core ethos are folded into the rewritten chapters (Bearing Teeth, Day in the Life, The Interview); the cage-versus-noodle-stall structural split is consistent across synopses, beats, and assembled HTML.

### Per-chapter notes

- **Bearing Teeth**: synopsis + HTML rewritten with new ethos directives folded in. Motorcycle, vertical-Chicago atmosphere, chrome-arm shoulder seam (no powers), boss bleeding out from wrist (no electrical discharge), laugh-or-cry humor, "the myth is the weapon" framing.
- **Day in the Life**: continuous from Bearing Teeth, four-part structure: ride back, kitchen (fragment extraction with bird epistemology aside), going to bed clutching Silence, morning. The Sable contract drops on Kyle's phone at the very end as the day's pivot point.
- **A Restless Mind**: synopsis v2 dropped the hood-back-during-walk (it broke beat 32's logic about the AI reading recognition through Kyle's senses). The Walk beat now ends with Sable hooded, proposing tomorrow, leaving. The Door Across The Hall (new closer) has Kyle returning home with no knowledge of who the woman was. Chapter Html is the OLD body and still describes the cage scene — needs regeneration from new beats.
- **Inside the Cage**: 6 beats moved/added (Recognition Inside The Cage opens it, the three triangulation locations, the Thanks, the Pixel-back-at-home closer). No chapter-level Html yet — could be assembled from beats next session.
- **The Interview**: chapter Html is a long Lotus-ambush fight scene built around the energy-katana mechanics (hamon climbing through cyan/white/sodium-white, PZT core drinking impulses, full bank discharge, etc.). 16+ hamon references. Needs full rewrite the way Bearing Teeth got. Multi-hour task.
- **A Borrowed Hand**: beat-level scrub done; chapter Html had two duplicated passages patched. Now clean.

## Worked recommended next-session moves

This session executed beyond the original handoff. Updated next-priority queue:

1. **Rewrite The Interview HTML** — the only remaining deeply-stale chapter. ~31k chars of energy-katana prose to remap to the new ordinary-katana mythology. Same surgical approach as Bearing Teeth: preserve every scene beat, strip every power reference, fold in ethos directives.
2. **Regenerate A Restless Mind chapter Html** — assemble from the three current beats (Watching / The Walk / The Door Across The Hall) plus connective tissue. Smaller than #1.
3. **Write Inside the Cage chapter Html** — assemble from its 6 beats with connective tissue. Smaller than #1.
4. **Continue 100-story outline** — premises 21+.
5. **Seed peer characters** with DB entities.
6. **Build out the network/AI worldbuilding** per directive #13.

## Chapter HTML rewrites (full bodies)

- **Bearing Teeth HTML** — **REWRITTEN 2026-05-16**. New ~33k-char body folds in: motorcycle, vertical-Chicago atmosphere, the chrome-arm-shoulder-seam cut (no powers), the boss bleeding out from wrist (no electrical discharge), laugh-or-cry humor in Kyle's interior catalogue, the "myth is the weapon" framing, the watcher coda updated so the surveillance organization notes "no flare, glow, or arc" and recalibrates its literature. Scene beats preserved end to end.
- **A Borrowed Hand HTML** — still has old Silence-powers prose. **PENDING** full rewrite.
- Day in the Life — no HTML yet (chapter is new). **PENDING** full prose.
- Inside the Cage — no HTML yet (chapter is new). **PENDING** full prose.

## Continuity invariants (use to validate any future rewrite)

- Silence is JUST a sword. No glow, no discharge, no charge state. If any prose says otherwise, fix it.
- Chorus is JUST a five-shot revolver shotgun. No magazine. No semi-auto. Reload is moon clips.
- Kyle has a motorcycle. He uses it for distance travel. Default reading: matte black, ground-level parking.
- Sable revealed her identity to Kyle face-to-face in A Restless Mind (chapter 3), at the noodle stall, NOT in a Faraday cage.
- The Faraday cage AI-revelation + triangulation happens the NEXT night, in Inside the Cage (chapter 4).
- Pixel does NOT know about the cage / AI / Sable's revelation as of the end of A Restless Mind. She finds out later.
- The day-in-the-life (chapter 2) sits between the Bearing Teeth mission and the noodle-stall recognition with Sable. There is at least one night of sleep and one morning between the kneecap-shotgun mission and Sable revealing herself.
- The rogue AI is real and manipulating Kyle's contracts, but the FULL reveal (it has been orchestrating his life as chess pieces) does not happen until much later — many books out. Bushido Coda lands the avatar misdirect (Kyle/Sable's wrong hypothesis), nothing more.

## Open questions / TBD

- The 'Silence' Technical Schematic document — needs to be rewritten to match the new no-powers spec, or archived. Currently still claims piezoelectric stack etc.
- The Resonance Blades doc — may reference Silence; needs read-through.
- Bearing Teeth full HTML body — still has the original prose with sodium-white discharges. Needs full rewrite for canon consistency, or annotation that the HTML is stale relative to the new synopsis.
- A Borrowed Hand HTML — same issue.
- Memory file `project_kyle_strop_ritual.md` — retire (strop is gone).
- Memory file `project_kyle_weapons_specs.md` — update to new specs.
