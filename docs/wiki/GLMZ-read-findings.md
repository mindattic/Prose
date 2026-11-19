---
project: Prose
layer: report
updated: 2026-11-18
---

# GLMZ read findings (PRS-T1032)

Every unread beat in the GLMZ canon books and Babineaux's Guide to the Otherworldly (about 1,650 beats in 23 books)
was read in full on 2026-11-18 and receipted as `Claude reader (PRS-T1032)`, so none of those books is blocked from
export. Draft books (Iron & Silk, Ballast, Standing Contract, Read the Room (replan), It Came From Iowa, Fenris) were
left unread by the owner's choice. Nothing below was changed: each line is a problem the readers saw, for an author
or a fix ticket to resolve.

275 findings: text 149, markup 64, continuity 38, entity 24.
Most `text` findings are leftover unit conversions inside the prose ("33ft (10m)"); most `markup` findings are
ordinary words tagged as entities ("Park" in Hyde Park as Dr. Nadia Park, "the south side" as Milwaukee's South Side).

## Attendance (6)

| Beat | Kind | Quote | Problem |
|---|---|---|---|
| 9386 | entity | The thing was the size of a delivery van folded in half | Brickleg record's visible_slice says 'about the size of a large dog'; this beat makes it van-sized (Babineaux's Guide says delivery-cart). |
| 16029 | entity | The crews who still log that fence call it the Lure, because drawing things toward the boundary is all anyone has ever seen it do, and nobody has written down w | The Lure record says ''The Lure' is what the Counter calls it'; the beat gives the name a different origin (the crews' name). |
| 22193 | entity | a thing that offers a man a bargain wants. It could not tell Yemina from a wall, because Yemina was no use to it. It had told Milo from a wall at once. | The Lure record says it 'does not know what a person is' and 'cannot model the question'; the beat has it striking a bargain with Milo and telling him from a wall. If intended as a correction of Selvamani, the record should reflect it. |
| 9302 | markup | a babineaux being, all over <entity repo="place" guid="019d6143-a94e-728a-a754-075f608dd73a">the south side</entity> | 'the south side' (twice in this beat) is tagged as Milwaukee's The South Side (019d6143-a94e); the story is in Chicago's Pilsen Veil / Back of the Yards, so it should be The South Side (Chicago), 01a13683-458a. |
| 9327 | markup | ended at the same address in Hyde <entity repo="character" guid="0260b3a7-2d4f-8e1c-9b50-a637c8de2f10">Park</entity> | 'Park' in the place name Hyde Park is tagged as the character Dr. Nadia Park. |
| 9358 | markup | *<entity repo="place" guid="019d6143-a955-72ac-9d2d-78751976f2cb">Threshold</entity> Biota, egress E4* | 'Threshold' in the AAMA term 'Threshold Biota' is tagged as the place Threshold (the Lake Huron city); it is not that entity. |

## Babineaux's Guide to the Otherworldly (27)

| Beat | Kind | Quote | Problem |
|---|---|---|---|
| 24043 | continuity | that afternoon she was at the stove when the first cousins came for Sunday | Timeline: Margitte's note set the B-level trip for Tuesday 2 a.m., the thaw was 'Thursday, two days out', and Val's visit is the next morning, so 'that afternoon' cannot be Sunday. |
| 24046 | continuity | Three of my cousins work OmniPro's corridor security, which the company rents from Arcturus, under their wives' names | Beats 24006/24005 say the three Arcturus cousins work under their mothers' married names (mothers married out); here it is their wives' names. |
| 24067 | continuity | The Seams, if you're not from here, is everywhere in the city that isn't a CorpoNation's ground. ... My family lives there. I grew up there. | 24026 already explained the Seams (with the same 'if you're not from here') as the bottom districts distinct from the Glooms, and the family's Canaryville is repeatedly placed in the Glooms; here Ren says his family lives and he grew up in the Seams. |
| 24071 | continuity | They come in at the angles. Somebody said that to me once, years back. | In 24005 an old great-aunt says 'They come in at the angles' to Ren at the first Sunday dinner after the cut (March 2231), weeks before he is writing; 'years back' contradicts it. |
| 24086 | continuity | In the summer of 2229, Axiom had bought a piece of Archive Coral | The coral fell in August 2229, Ren kept it until it vanished in October (24075), and the broker sold it on inside a week (24074); Axiom's purchase was autumn, not summer. |
| 24086 | continuity | I've wanted to meet you for six years. | 24074 says Paula first learned the Babineaux name from the coral chain (late 2229) and only went back to her note a year and a half later; here she has wanted to meet Ren for six years via the Brickleg file sold in '24. The two accounts of how she found him don't line up. |
| 24088 | continuity | a thing somebody said to me years ago came up in my head as plain as the radio: they come in at the angles | This is the night of the cut (1-2 March 2231); per 24005 Ren only hears 'They come in at the angles' from the great-aunt at the first Sunday dinner after the cut, so he cannot recall it here, let alone from 'years ago'. |
| 24091 | continuity | I asked at Sunday dinner, two weeks before Margitte stood up | The basement muster is in February 2231 (before the cut), but Margitte stands up at Val's table in April (after Paula's truck visit, 24085); 'two weeks before' does not fit. |
| 24092 | continuity | And the coral. The summer of '29, to Axiom. | Same timing slip as 24086: per 24075 the coral piece vanished from the truck and was sold in October 2229, not summer. |
| 24094 | continuity | Every door in the city had shut but one. The thing in the fourth room was turned toward it. | Set in April/early May, but the next beat (24095, May) has Gil saying the AAMA board still had eleven Schisms left; 24096's list also leaves the Hole as only the last of several. |
| 24108 | continuity | It was about the length of her arm, long and soft | 24018 describes a Brine Lantern as 'a swimmer about the size of my fist'; here one is arm-length. |
| 24001 | entity | about the size of a delivery cart | Brickleg record's visible_slice says 'about the size of a large dog'; beat makes it delivery-cart size. |
| 24024 | entity | It's a knot of charge about the size of a cat | Gridcalf record's visible_slice says 'about the size of a calf, hanging a meter off the ground'; beat (and 24027) make it cat-sized. |
| 24077 | entity | It was a slab about the size of a delivery van ... From 33ft (10m) off it looked like coral: branching, white going to blue at the tips | Archive Coral record's visible_slice describes a slab of grey-violet mineral crust ribbed in fine parallel lines, not branching white-to-blue glass coral. |
| 23996 | markup | on <entity repo="place" guid="019d6143-a94e-728a-a754-075f608dd73a">the south side</entity> | 'the south side' here is tagged as Milwaukee's The South Side (019d6143-a94e); the book is set on Chicago's south side (01a13683-458a, as used in 24007/24008). Wrong entity. |
| 24007 | markup | If you're from <entity repo="place" guid="01a13683-458a-7ba1-a37f-1a97b6c7695f">the south side</entity> | 'the south side' is tagged with a different place guid (01a13683-...) than in beat 23996 (019d6143-a94e-...); one of them is likely the wrong entity. |
| 24013 | markup | an order up in Hyde <entity repo="character" guid="0260b3a7-2d4f-8e1c-9b50-a637c8de2f10">Park</entity> | 'Park' in the place name Hyde Park is tagged as the character Dr. Nadia Park. |
| 24073 | markup | at a seam in McKinley <entity repo="character" guid="0260b3a7-2d4f-8e1c-9b50-a637c8de2f10">Park</entity> | 'Park' in McKinley Park (twice in this beat) is tagged as the character Dr. Nadia Park; same defect as 'Hyde Park' in 24013. |
| 24073 | markup | <entity repo="place" guid="019d6143-a94e-7740-8aa9-4b82bbd9a223">Seams</entity> that had started acting up, thin spots opening on a clock | Here 'Seams' is the common noun (plural of seam, i.e. thin spots), but it is tagged as the place The Seams. |
| 24073 | markup | a seam in the <entity ...>Halsted</entity> <entity ...>Freight Yard</entity> | The place Halsted Freight Yard is split: 'Halsted' tagged as Halsted Street and 'Freight Yard' tagged alone as the Halsted Freight Yard entity; the whole name should be one tag. |
| 24086 | markup | "Mr. <entity repo="faction" guid="babecafe-dead-beef-0011-223344556677">Babineaux</entity>," she said. | 'Mr. Babineaux' (Ren) and 'Mrs. Babineaux' (Ramona) are tagged as the Babineaux Family faction; elsewhere (24028, 24035) 'Mr. Babineaux' is tagged as Ren. |
| 24108 | markup | A woman in Tinley <entity repo="character" guid="0260b3a7-2d4f-8e1c-9b50-a637c8de2f10">Park</entity> | 'Park' in the place name Tinley Park is tagged as the character Dr. Nadia Park (third instance, with 24013 and 24073). |
| 24007 | text | was sad about the bus for nearly 0.6mi (1km), which is a record | Auto unit-conversion artifact in first-person narration (also '0.6mi (1km) over your head' later in the beat); reads as scaffolding, not voice. |
| 24019 | text | at a weight of a hundred and seventy-six pounds (80kg) | Unit-conversion parentheticals inside quoted dialogue and narration ('I said it was 176lb (80kg)', 'the AAMA's 176lb (80kg)'); same artifact pattern as 24007. |
| 24026 | text | a freight tunnel six-tenths of a mile (1km) long, with drivers losing nine minutes at a time at fifty miles an hour (80km/h) | Unit-conversion parentheticals inside Ren's spoken dialogue (also 660ft (200m), 160ft (50m)); reads as scaffolding. |
| 24067 | text | 6.6 tons (6t) of grazer no higher than your knee | Unit-conversion parenthetical in narration; same artifact pattern as 24007/24019/24026 (also '5 foot three (1.6m)' in 24056, '3ft (1m)' in 24009/24033/24055). |
| 24089 | text | Three times a day, near enough to the minute: 02:14, noon, 12:04. | 'noon' and '12:04' are four minutes apart, so this is not three spread-out daily pulls; one of the times is likely wrong (later the beat confirms pulls at 02:14 and noon). |

## Between the Lines (1)

| Beat | Kind | Quote | Problem |
|---|---|---|---|
| 16974 | continuity | Bishop hands him something small, flat, warm from his own pocket — a passive scanner | Bishop hands the scanner to Idris, but later in the same beat (and in #16976) Bishop is the one holding and raising the scanner at bay four. |

## Bushido Coda (13)

| Beat | Kind | Quote | Problem |
|---|---|---|---|
| 20484 | continuity | had never said a word to her that needed an answer. She knew his order. That was the whole of what she knew. | Mrs. Chen hired Kyle in her apartment four nights earlier (beat 20435: she said his name, paid him a credstick), so 'his order' cannot be the whole of what she knows. |
| 20644 | continuity | the 18.7 Hz notation that's kept coming back since the carousel drops out | The recurring array notation is 'AMBIENT INTERFERENCE — 18.9 HZ' (20575, 20582); 18.7 is the carousel/Pixel reading, so the figure here looks wrong (minor). |
| 20768 | continuity | The hall between 2D and 2E is 13ft (4m) of ferrocement and worn linoleum | Hall width disagrees with 20590 ('reading clean 30ft (9.1m) across the hall') and 20544 ('crossed the four steps home'); 13ft vs 30ft for the same span. |
| 20880 | continuity | hung from cables she had spent the afternoon talking to | Gantry is 'she' here, but 'he' in 20838 ('the way, he said, his sister had talked to everything'). |
| 20736 | entity | She read it for nine seconds. | Dr. Nadia Park's record says she read the thermal log on Kyle's chest port for eleven seconds before saying 'They actually built one.' |
| 20542 | markup | a DominionReach subsidiary called <entity ...>Copper</entity> Reclamation West | 'Copper' inside the company name Copper Reclamation West is tagged as the material entity Copper. |
| 20575 | markup | *<entity repo="synthetic" guid="019d6143-aa12-7678-867a-5f6047cf2efb">Null</entity>*, and before that *Ñu* | Sift's old handle 'Null' is tagged as the synthetic Null (Axiom's Project Cruciform being), a different entity. |
| 20834 | markup | "<entity ... Walt Breckenridge>I</entity>'ll deal with that separately." | This line sits between two Breckenridge speeches and reads as Kyle's reply, but its 'I' is tagged as Walt Breckenridge; speaker attribution is unclear. |
| 20847 | markup | a salvage buyer's charger in <entity repo="corponation" ...>Ashgrave</entity> | 'Ashgrave' used as a place is tagged as the corponation AshgraveMaterials; elsewhere (20752, 20757) the same place usage is tagged as Ashgrave Synthesis Corridor. |
| 20849 | markup | on the <entity repo="corponation" ...>Ashgrave</entity> fringe | 'the Ashgrave fringe' is a place, but it is tagged as the corponation AshgraveMaterials (20752 tags 'Ashgrave fringe' as Ashgrave Synthesis Corridor). |
| 20877 | markup | which on the <entity repo="corponation" ...>Ashgrave</entity> fringe counted as a reference | 'the Ashgrave fringe' (a place) is tagged as the corponation AshgraveMaterials; 20873 tags the same phrase as Ashgrave Synthesis Corridor. |
| 20946 | markup | since an <entity repo="corponation" ...>Ashgrave</entity> yard | The custody yard (a place) is tagged as the corponation AshgraveMaterials; 20915 tags the same 'Ashgrave freight yards' as Ashgrave Synthesis Corridor. |
| 20953 | markup | The man in the <entity repo="corponation" ...>Ashgrave</entity> yard | Same place-vs-corponation mis-tag: the yard is tagged as AshgraveMaterials instead of Ashgrave Synthesis Corridor (as in 20915). |

## Crimson & Chrome (17)

| Beat | Kind | Quote | Problem |
|---|---|---|---|
| 6536 | continuity | Scout came last off the floor, and stood on the shore road | Same beat earlier has Scout coming up the ramp ahead of the clerks, and Goldberg 'came out last'; two people can't both come out last. |
| 6551 | continuity | because one building came down | The Reliquary of Solace was not destroyed (beds were rolled out and Anneke filed for more beds); 'came down' reads as literal collapse. |
| 6440 | entity | The good doors in GLMZ wheeze | GLMZ record: nobody who lives there says 'GLMZ' except on a form; Sefi says it in dialogue. |
| 6447 | entity | the one place in GLMZ where the bill comes due ... the only room in GLMZ they kept the lights on for | GLMZ record: residents never say 'GLMZ' aloud (it's 'the zone' or 'the city'); Rook says it twice in dialogue. |
| 6543 | entity | The Reibo sat in the holster, matte steel | Reibo record specifies a polymer pistol frame (matte, light-absorbing finish), not steel. |
| 6551 | entity | "The GLMZ doesn't stop being what it is because one building came down. | GLMZ record: residents don't say 'GLMZ' aloud; Rook says it in dialogue. |
| 6394 | markup | <entity repo="place" guid="ea28fdd3-7bdc-439b-962c-42cfe5d996ac">the Thread</entity> on the southern horizon dropping <entity repo="vocabulary" guid="01a1016d-9 | Possessive 'its' (referring to the Thread) tagged as a separate vocabulary entity. |
| 6412 | text | palm flat on the table, 0.4in (1cm) short of the gloss | Unit-conversion scaffolding '0.4in (1cm)' in prose. |
| 6436 | text | and stopped 0.4in (1cm) short | Unit-conversion scaffolding '0.4in (1cm)' in prose (the next sentence already says 'a centimeter'). |
| 6451 | text | The live woman 3ft (1m) from it | Unit-conversion scaffolding '3ft (1m)' in prose. |
| 6489 | text | recessed behind wire cages every 13ft (4m) | Unit-conversion scaffolding '13ft (4m)' in prose. |
| 6492 | text | The ramp leveled onto a landing 66ft (20m) on a side | Unit-conversion scaffolding '66ft (20m)' in prose. |
| 6496 | text | maybe 197ft (60m) off | Unit-conversion scaffolding '197ft (60m)' in prose. |
| 6505 | text | 33ft (10m) up the aisle, dead ahead | Unit-conversion scaffolding '33ft (10m)' in prose. |
| 6507 | text | The aisle ran 33ft (10m) to the door ... past that 197ft (60m) of woken floor | Unit-conversion scaffolding '33ft (10m)' and '197ft (60m)' in prose. |
| 6536 | text | 10ft (3m) of what the street called a drop-kite | Unit-conversion scaffolding '10ft (3m)' in prose. |
| 6554 | text | a dark stroke 131ft (40m) long | Unit-conversion scaffolding '131ft (40m)' in prose. |

## Critical Mass (1)

| Beat | Kind | Quote | Problem |
|---|---|---|---|
| 21963 | markup | a family that had worked small paper across <entity repo="place" guid="019d6143-a94e-728a-a754-075f608dd73a">the south side</entity> | 'the south side' (Babineaux, Canaryville, Chicago) is tagged as Milwaukee's The South Side (019d6143-a94e); should be The South Side (Chicago), 01a13683-458a. |

## Death Whispers in a Cat's Ear (1)

| Beat | Kind | Quote | Problem |
|---|---|---|---|
| 21373 | markup | the Meridian man on the door, the cleaner at eight | "the cleaner" (the night cleaner on the service walk) is tagged as character fe6ab919-9f3c-450b-aaf5-f24f84896521 (Stash Adeyemi, the doctor from The Long Cut). |

## Magenta & Gunmetal (5)

| Beat | Kind | Quote | Problem |
|---|---|---|---|
| 6968 | markup | A job in The Glooms where the only way out was a service tunnel | "The Glooms" is tagged as the GLMZ place entity; the GLMZ record defines the Glooms as the bottom of the stack, not the whole zone. |
| 7014 | markup | Grit came off the tiles — not blown, drawn — crumbs of aggregate | Common noun "Grit" (debris) is tagged as the character entity Grit (01a19ed5-...-8461cf4b9ebae). |
| 7111 | markup | the Thread had its own string of lights | "its" (referring to the Thread) is tagged as vocabulary entity 01a1016d-9a65-7f7d-9273-3680683cde1f instead of the Thread place (or untagged). |
| 7133 | markup | Gill on rugose gill, nacreous where the floods touched it | Common noun "Gill" (the lake creature's gills) is tagged as the character entity Gill (019d6143-a64f-7479-b472-0a8a86f2cf35). |
| 17292 | text | *That is correct,* the note had said.  Even, except for the ones who didn't walk away. | Final line of the book reads as a fragment ("Even, except for...") with no antecedent in the beat; check it is a deliberate callback and not a truncated sentence. |

## Mnemosync (24)

| Beat | Kind | Quote | Problem |
|---|---|---|---|
| 8125 | entity | "You've been pulling correlations from GLMZ health data | GLMZ record: residents don't say 'GLMZ' aloud; Ekow says it in dialogue. |
| 7915 | markup | grazing brake <entity repo="vocabulary" guid="019d6143-aaa4-7717-a761-2d580e56d107">dust off</entity> the tiles | Ordinary words 'dust off' tagged as a vocabulary entity. |
| 7982 | markup | a <entity repo="synthetic" guid="019d6143-a9f0-71b2-8cc2-3d8b31b58eeb">Carrion</entity> wagon in orange and black | Carrion (the reclamation company) tagged as a synthetic entity. |
| 7884 | text | A city seen from 5ft (1.5m) above the pavement | Unit-conversion scaffolding '5ft (1.5m)' in prose. |
| 7895 | text | coasted the last 100ft (30m) ... started 66ft (20m) east | Unit-conversion scaffolding '100ft (30m)' and '66ft (20m)' in prose. |
| 7917 | text | 1.2mi (2km) from his building | Unit-conversion scaffolding '1.2mi (2km)', and the beat opens with a numeral. |
| 7938 | text | 3ft (1m) of clearance on the left | Unit-conversion scaffolding '3ft (1m)' in prose. |
| 7946 | text | the concrete joints every 20ft (6m) | Unit-conversion scaffolding '20ft (6m)' in prose. |
| 7959 | text | She knew the table was 20ft (6m) long. | Unit-conversion scaffolding '20ft (6m)' in prose. |
| 7981 | text | 37mi (60km) of warning, maybe 62mi (100km) | Unit-conversion scaffolding in prose (rounded metric became odd imperial figures). |
| 7982 | text | 13ft (4m) of leg built for a lighter world | Unit-conversion scaffolding '13ft (4m)' in prose. |
| 8013 | text | 0.8in (2cm) above the collar | Unit-conversion scaffolding '0.8in (2cm)' in prose. |
| 8101 | text | "Not 853. The city." "A new standard of care." Amara read the cover line again. | Two separate quotations run together with no speaker or attribution; reads as garbled (one is Amara's thought, one the cover line). |
| 8125 | text | 10ft (3m) of open roof | Unit-conversion scaffolding '10ft (3m)' in prose. |
| 8126 | text | slid out of nothing 3ft (1m) over the rubble | Unit-conversion scaffolding '3ft (1m)' in prose. |
| 8133 | text | stood 33ft (10m) from the machine | Unit-conversion scaffolding '33ft (10m)' in prose. |
| 8323 | text | he read it 33ft (10m) out | Unit-conversion scaffolding '33ft (10m)' in prose. |
| 8353 | text | a loading-zone line repainted 6.5ft (2m) short of his door | Unit-conversion scaffolding '6.5ft (2m)' in prose. |
| 8357 | text | no beacons for the first 1.9mi (3km) | Unit-conversion scaffolding '1.9mi (3km)' in prose. |
| 8369 | text | The drop was 130ft (40m) in | Unit-conversion scaffolding '130ft (40m)' in prose. |
| 8373 | text | the north culvert was 1.9mi (3km) of dead air | Unit-conversion scaffolding '1.9mi (3km)' in prose. |
| 8424 | text | The building's patient threshold was 39ft (12m) past the outer ring, and those 39ft (12m) ran on different rules. | Unit-conversion scaffolding '39ft (12m)' twice in prose. |
| 23928 | text | The towpath ran 5ft (1.5m) wide ... a footbridge every 660ft (200m) ... decided tonight to move 66ft (20m), and the canal road was the 66ft (20m) ... 3ft (1m) l | Unit-conversion scaffolding repeated throughout the beat (about eight instances). |
| 23952 | text | freight loader 10ft (3m) tall | Unit-conversion scaffolding '10ft (3m)' in prose. |

## Neon & Rust (7)

| Beat | Kind | Quote | Problem |
|---|---|---|---|
| 7203 | continuity | somebody's mother meant to leave a light on until they found the way home. This year they were promising everybody. | The inserted Lantern Mothers sentence now sits between 'AAMA had ... promised a team' and 'This year they were promising everybody', so 'they' now reads as the lantern mothers rather than AAMA. |
| 7508 | continuity | the name came up through the cold reading on [Lace]'s two fingers before it came up through anything she'd have called hearing | The two-finger cold reading is Vox's (Tem Igwe's) Tap, and the beat's pronouns are tagged to Tem Igwe; naming Lace (Blessing Agwu) here looks like the wrong crew member. |
| 7548 | continuity | She opened the notebook and wrote the only one of the twenty-one she had a name for | Possible contradiction: in #7511 Adalemo gives two traced names, Sefi Nwachukwu and Taye Anwunobi; if Taye is one of the twenty-one, Rook has two names, not one. |
| 7221 | text | Her legs took her in before she'd decided to go. He set a roll of gauze on the counter that she hadn't asked for. | 'He' should be 'She': the shopkeeper is introduced as a woman (a Z6 auntie in a housedress) two sentences earlier. |
| 7282 | text | "Which means he didn't trust the purge to make it not have happened?" [Vox] turned the fragment over ... "People don't cover a search that clears itself unless  | Two consecutive dialogue paragraphs are both attributed to Vox (Tem Igwe), the first a question she then answers herself; one attribution is likely wrong. |
| 7302 | text | Letter-cats, the kids called them, because from the side you could post one under a door. Our thickness would crease them apart by the end of the week. | Stray first-person 'Our thickness' in a close-third-person narration; reads as a leftover/garbled phrase (likely meant 'this world's thickness' or similar). |
| 7443 | text | The door was theirs. The 131ft (40m) weren't, yet. | Unit-conversion artifact: a spelled distance ('forty meters') was turned into '131ft (40m)', which breaks the grammar here and in the one-line beat #7430 ('131ft (40m). Maybe less.'); other beats in the same scene spell it 'a hundred and thirty feet (40m)'. |

## Pieces (82)

| Beat | Kind | Quote | Problem |
|---|---|---|---|
| 23302 | continuity | the matched twin of the one Rampart had carried since Ozzie's couch and never put down | Contradicts The Way Down #6744 and the Veltrin record: Reza's own Ankle-Biter went into a drawer when he went corporate and he now carries a Veltrin. |
| 23315 | continuity | Selvamani had carried forty in the margins of her log for seven years | The forty missing children are Yemina Fola's case files (her box, her margins, #23196/#23206); Selvamani's log records the Hole's feed times, not the forty. |
| 23329 | continuity | the Haskett Ankle-Biter, the matched twin of the one Rampart carried at the other end of the bay | Repeats the #23302 problem: per The Way Down #6744 and the Veltrin record, Reza's Ankle-Biter went into a drawer and he carries a Veltrin. |
| 23346 | continuity | It's been a quiet seam since the winter the playground went, and it opened at noon | Selvamani's own log (#23194, #23197) has the Hole feeding three times a day, every day, for seven years, including since the playground went; 'quiet seam' contradicts it. |
| 23185 | entity | She had grown up on that voice. Everybody in the Seams had. | Pixel's record has her growing up in Detroit and arriving in Chicago at twenty-six; this says she grew up on Chicago site-control voices in the Seams. |
| 23210 | markup | a Sectionist from Hyde <entity repo="character" guid="0260b3a7-2d4f-8e1c-9b50-a637c8de2f10">Park</entity> | 'Park' in the place name Hyde Park is tagged as the character Dr. Nadia Park. |
| 23266 | markup | There's a clipping in the box Yemina gave me. She'd circled it. ... nobody could ever tell her where they went. | 'She'/'her' refer to Yemina but are tagged as Selvamani (019ec17b-4ee6-7d01-9776-050c1e154558). |
| 23266 | markup | walked into a wound in the air on <entity repo="place" guid="019d6143-a94e-728a-a754-075f608dd73a">the South Side</entity> | Linked to 'The South Side' of Milwaukee; this Chicago Underclan story (and other Pieces beats) use 'The South Side (Chicago)' (01a13683-458a-7ba1-a37f-1a97b6c7695f). |
| 23366 | markup | ringing the children down the stairs all over <entity repo="place" guid="019d6143-a94e-728a-a754-075f608dd73a">the south side</entity> | Linked to Milwaukee's 'The South Side'; the scene is Chicago (river to Back of the Yards), which has its own entity 'The South Side (Chicago)' (01a13683-458a-7ba1-a37f-1a97b6c7695f). |
| 23181 | text | On five she was cold and heavy and 13ft (4m) tall. | Unit-conversion artifact: parenthetical metric figure left in narration. |
| 23182 | text | The Warden weighed 12 tons (11t) ... stood 33ft (10m) off ... 130ft (40m) up ... 10ft (3m) tall | Unit-conversion artifacts (four) in one beat; the beat also spells 'thirty meters across' normally, so the parentheticals stand out as tool output. |
| 23183 | text | She watched the numbers drop through 660ft (200m), 980ft (300m). At four hundred they stopped dropping | Unit conversion broke the sentence logic: the count now runs in feet (660, 980) and then jumps to 'four hundred', which only makes sense in meters. |
| 23183 | text | a collar of steel set into the ground, 10ft (3m) across | Unit-conversion artifact: parenthetical metric figure left in narration. |
| 23184 | text | On the walkway 130ft (40m) overhead ... plasma 6.5ft (2m) long | Unit-conversion artifacts (two). |
| 23185 | text | 12 tons (11t) pulling 12 tons (11t) | Unit-conversion artifact, doubled. |
| 23191 | text | made them 3ft (1m) shorter inside than out | Unit-conversion artifact: parenthetical metric figure left in narration. |
| 23193 | text | He was 6ft 8in (2.03m) tall and weighed 385lb (175kg) ... cut 4.3in (11cm) too wide | Unit-conversion artifacts (three), including the oddly precise '4.3in'. |
| 23196 | text | Selvamani walked him 66ft (20m) along the tape | Unit-conversion artifact: parenthetical metric figure left in narration. |
| 23197 | text | began to slide toward the tape, 4in (10cm), 8in (20cm) ... came back off the tape 3ft (1m) | Unit-conversion artifacts (three). |
| 23205 | text | on the Pilsen Veil seam 10ft by 13ft (3m by 4m) | Unit-conversion artifact: parenthetical metric figure left in narration. |
| 23207 | text | a round hole 10ft (3m) across | Unit-conversion artifact: parenthetical metric figure left in narration. |
| 23220 | text | the same kind as the one in his own jacket, a long dark coat with a high collar, a healed scar across her palm | Garbled sentence: Rook's description ('a long dark coat with a high collar') is spliced in so it reads as describing Kyle's jacket. |
| 23223 | text | 6.5ft (2m) of gearing and coiled spring | Unit-conversion artifact: parenthetical metric figure left in narration. |
| 23226 | text | She took it up to 130ft (40m) | Unit-conversion artifact: parenthetical metric figure left in narration. |
| 23229 | text | a glass tube 130ft (40m) over the Exchange ... 130ft (40m) down, was the canopy | Unit-conversion artifacts (two). |
| 23230 | text | an ad on the hoardings 130ft (40m) below | Unit-conversion artifact: parenthetical metric figure left in narration. |
| 23232 | text | Pull to center 6.5ft (2m). ... at 35th and Halsted, 3.1mi (5km) away | Unit-conversion artifacts, one inside Selvamani's handwritten log entry, where a dual-unit figure is especially out of place. |
| 23233 | text | read raised lettering under thin paint at 130ft (40m) the way they read a crate stencil at 13ft (4m) | Unit-conversion artifacts (four in the beat, incl. 'Warden ... 13ft (4m) tall' and 'hoardings 130ft (40m) below'). |
| 23234 | text | 3ft (1m) above the glass roof | Unit-conversion artifact: parenthetical metric figure left in narration. |
| 23236 | text | She heard all of it all the time, 620mi (1,000km) of it | Unit-conversion artifact: parenthetical metric figure left in narration. |
| 23238 | text | through 620mi (1,000km) of ground | Unit-conversion artifact: parenthetical metric figure left in narration. |
| 23241 | text | The chalk doorway on the basement wall was 3ft (1m) wide and 6.5ft (2m) high | Unit-conversion artifacts (two). |
| 23247 | text | the glass tube 130ft (40m) overhead ... ran along the street for 660ft (200m), a wall of ads 10ft (3m) high | Unit-conversion artifacts (three). |
| 23248 | text | A young man was standing 33ft (10m) along ... when she was 10ft (3m) away | Unit-conversion artifacts (two). |
| 23249 | text | One moment he was 10ft (3m) away. | Unit-conversion artifact: parenthetical metric figure left in narration. |
| 23250 | text | along the hoardings for 660ft (200m) ... A service gap, 16in (40cm) high ... a crawlspace, 1.6ft (0.5m) wide | Unit-conversion artifacts (three). |
| 23252 | text | "There's a clinic," she said to Glim. "A mile and a quarter (2km)." | Unit-conversion artifact inside dialogue: Pixel speaks a parenthetical metric figure. |
| 23256 | text | The thermal went down thirteen hundred feet (400m) and turned around | Unit-conversion artifact inside Pixel's dialogue. |
| 23273 | text | She was 24lb (11kg) of plastic and wire and brushes | Unit-conversion artifact: parenthetical metric figure left in narration. |
| 23274 | text | 6.5ft (2m) of him in a jacket too wide at the shoulders | Unit-conversion artifact; also rounds Bear down from the 6ft 8in (2.03m) given in #23193. |
| 23283 | text | 660ft (200m) of dead refrigeration racks ... kept that one locker at 25?F (-4?C) | Unit-conversion artifacts (two). |
| 23284 | text | 6.5ft (2m) of it ... Eight columns across 6.5ft (2m) of white paper | Unit-conversion artifacts (two). |
| 23285 | text | looking at the columns from 66ft (20m) off | Unit-conversion artifact: parenthetical metric figure left in narration. |
| 23291 | text | 980ft (300m) below the lake floor | Unit-conversion artifact: parenthetical metric figure left in narration. |
| 23296 | text | Six hundred and twenty miles (1,000km). It's pumping like a heart attack. | Unit-conversion artifact inside Sasha's dialogue. |
| 23297 | text | "Thirteen hundred and fifty feet (412m)," Sasha said. ... about a hundred feet (30m) down ... thirteen hundred feet (400m) of rock from five miles (8km) away .. | Unit-conversion artifacts, five of them inside Sasha's dialogue, plus '660ft (200m)' and '5mi (8km)' in narration. |
| 23298 | text | anyone who could hear 1,300ft (400m) down | Unit-conversion artifact: parenthetical metric figure left in narration. |
| 23301 | text | was 1,300ft (400m) of steel racking | Unit-conversion artifact: parenthetical metric figure left in narration. |
| 23302 | text | Rampart stood 3ft (1m) to his left. Cutout stood 3ft (1m) to his right. | Unit-conversion artifacts (two). |
| 23303 | text | At the mouth of the aisle it stopped, 3ft (1m) from Rampart | Unit-conversion artifact: parenthetical metric figure left in narration. |
| 23307 | text | 660ft (200m) of corrugated roof leaking in three places. | Unit-conversion artifact: parenthetical metric figure left in narration. |
| 23308 | text | 66ft (20m) out on the frozen ballast ... across 66ft (20m) of frozen rail yard | Unit-conversion artifacts (two). |
| 23309 | text | so the samurai could read it by the depot's lamp across 66ft (20m) | Unit-conversion artifact: parenthetical metric figure left in narration. |
| 23311 | text | Kyle stood, came the 66ft (20m) across the frozen ballast | Unit-conversion artifact: parenthetical metric figure left in narration. |
| 23312 | text | And butcher paper. 6.5ft (2m) of it, under the lamp | Unit-conversion artifact: parenthetical metric figure left in narration. |
| 23314 | text | and stopped 3ft (1m) short, as if he did not dare | Unit-conversion artifact: parenthetical metric figure left in narration. |
| 23319 | text | understood in the first 164ft (50m) why the nurse | Unit-conversion artifact: parenthetical metric figure left in narration. |
| 23321 | text | the lid on the collar came up a full 3ft (1m) | Unit-conversion artifact: parenthetical metric figure left in narration. |
| 23325 | text | Large. Pull to center 10ft (3m). | Unit-conversion artifact inside Selvamani's handwritten log entry. |
| 23326 | text | You'll be on the deck at noon in twelve tons (11t). | Unit-conversion artifact inside Bear's dialogue. |
| 23327 | text | Not 3ft (1m) of it touched the cover. | Unit conversion broke an idiom ('not a meter of it'); the parenthetical reads as tool output. |
| 23329 | text | 66ft (20m) of concrete between the two of them | Unit-conversion artifact: parenthetical metric figure left in narration. |
| 23331 | text | Up on the walkway 130ft (40m) overhead ... a round black hole in the floor, 10ft (3m) across | Unit-conversion artifacts (two). |
| 23337 | text | On five she was 12 tons (11t) and cold | Unit-conversion artifact: parenthetical metric figure left in narration. |
| 23338 | text | 12 tons (11t) coming forward fast ... the whole 12 tons (11t) jumped sideways | Unit-conversion artifacts (two). |
| 23340 | text | pushed back 1.6ft (0.5m) across the concrete ... she was 12 tons (11t) and cold ... standing 39ft (12m) apart | Unit-conversion artifacts (three); the same beat also says 'a full meter' unconverted. |
| 23341 | text | plasma 6.5ft (2m) long ... walked, 12 tons (11t) ... At 13ft (4m) she cut the lance | Unit-conversion artifacts (three). |
| 23342 | text | colder, 660ft (200m), 980ft (300m), 1,300ft (400m), the turn | Unit-conversion artifacts (three) in a single depth count. |
| 23343 | text | Sasha was beside him, 3ft (1m) to his left | Unit-conversion artifact: parenthetical metric figure left in narration. |
| 23345 | text | 12 tons (11t) each ... cut 4.3in (11cm) too wide ... inside 12 tons (11t) of frosted Warden, across 66ft (20m) of white deck | Unit-conversion artifacts (four). |
| 23346 | text | and 12 tons (11t) went over his hip onto the concrete | Unit-conversion artifact: parenthetical metric figure left in narration. |
| 23347 | text | Selvamani said "Now" on the wire from 3.1mi (5km) away | Unit-conversion artifact: parenthetical metric figure left in narration. |
| 23348 | text | the slit of glass at eye height 0.4in (1cm) from Bear's face | Unit-conversion artifact: parenthetical metric figure left in narration. |
| 23349 | text | and at 1,300ft (400m) a band of color the pane did not seem to know how to draw | Unit-conversion artifact: parenthetical metric figure left in narration. |
| 23355 | text | 330ft (100m). Two hundred. The square of light at the top became a button ... Three hundred. Three-fifty. | Unit conversion broke the depth count: it starts in feet ('330ft (100m)') and then continues in bare meters ('Two hundred... Three-fifty'). |
| 23355 | text | a ring of lights bolted into it every 66ft (20m) | Unit-conversion artifact: parenthetical metric figure left in narration. |
| 23356 | text | At 1,300ft (400m) the bore turned. ... exactly 66ft (20m) below the last | Unit-conversion artifacts (two); Faith then says 'past four hundred' in meters in the same beat. |
| 23359 | text | 3ft (1m) away on his left ... up 1,300ft (400m) of rock | Unit-conversion artifacts (two). |
| 23365 | text | from 3.1mi (5km) away ... joined 100ft (30m) down | Unit-conversion artifacts (two). |
| 23366 | text | The walkway, 130ft (40m) up, was kinked in the middle | Unit-conversion artifact: parenthetical metric figure left in narration. |
| 23937 | text | talked about onions for 0.3mi (0.5km) | Unit-conversion artifact: parenthetical metric figure left in narration. |
| 23988 | text | inside 12 tons (11t) of frosted Warden ... 100ft (30m) down, then 66ft (20m) ... 110 tons (100t) of rock on the stair | Unit-conversion artifacts (four). |

## Read the Room (2)

| Beat | Kind | Quote | Problem |
|---|---|---|---|
| 6115 | entity | And the [Wolfpack], when it went, went enormous. | Ethan fires a Wolfpack taken from Koss's crew, but the weapon record's specifications say 'lock: DNA lock keyed to the registered owner' (the Veltrin record says the same); either the beat needs the lock defeated or the record's lock field is wrong (the record's own summary also has Ethan firing it). |
| 5878 | text | Every draft climbed like the busker he'd once let two [Pulse] departures go by to hear in [the Glooms] station, a shamisen, three strings and the tuning wrong,  | Tangled simile: the drafts are compared to the busker himself rather than his playing, and 'the Glooms station' (Glooms tagged as GLMZ) reads like a garbled place name; likely an edit seam. |

## Sparrow (9)

| Beat | Kind | Quote | Problem |
|---|---|---|---|
| 4092 | continuity | He has spent eleven years making loss manifests reconcile | Elias has held the desk thirteen years (job came with the address thirteen years ago; first month on the desk in 2213), not eleven |
| 4203 | continuity | He's routed freight across corridors exactly like this one for eleven years. | Same eleven-vs-thirteen-years discrepancy as #4092 (also 'worked at gaps for eleven years' in #4210); his desk tenure is otherwise thirteen years |
| 4090 | markup | <entity ...Elias guid>It</entity> hasn't blinked once in four years of nights | 'It' refers to Egregore's apex light but is tagged as the character Elias |
| 4212 | markup | "<I~Sparrow> cannot hold a 37-year pattern. <My~Sparrow> functional horizon, before the shape of a thing dissolves, is about three weeks." | Elias is the speaker (his three-week memory horizon), but I/My/I are tagged as Sparrow |
| 4232 | markup | <Stack> C, monitored, runs clean tonight. | 'Stack C' is the building's vent stack C (from #4210), but 'Stack' is tagged as the GLMZ vocabulary entity Stack |
| 4091 | text | the blade-and-code solo who works out of the Gray Zone core | Uses 'the Gray Zone' (tagged to the Seams); looks like the retired name for the Seams left in prose |
| 4100 | text | Makena back up the freight lift to sign off the recovery and helping, working from the case dimensions | Garbled sentence: dangling 'and helping' clause does not parse |
| 4231 | text | the smell the Gray Zone gets when the Atmospheric Processors finish their cycle | Retired name 'the Gray Zone' (tagged to the Seams) used in prose; same as #4091 |
| 4231 | text | filed the same way he's learned to file the eleven days: not resolved, just closed | 'eleven days' looks like a slip for the book's recurring 'eleven minutes' (the 2218 closure); no eleven-day span is filed this way |

## Steppin' Razor (5)

| Beat | Kind | Quote | Problem |
|---|---|---|---|
| 8567 | continuity | now a bar, because the freight had stopped and the drinking hadn't | Joliet's freight is dead here and in 8570 ('nobody rode them anymore'), but 8561/8565 have an active freight spur, a freight interchange north of Joliet, and freight that 'didn't stop at a taped spur'. |
| 8602 | entity | "You make a hole in a thing," he said | The Traveler's record gives pronouns it/its; the beat uses 'he' (may be intended as Sasha's perception, but it does not match the record). |
| 8564 | markup | out of Hyde <entity repo="character" guid="0260b3a7-2d4f-8e1c-9b50-a637c8de2f10">Park</entity> | 'Park' in the place name Hyde Park is tagged as the character Dr. Nadia Park. |
| 8748 | markup | Food's outside <entity repo="character" guid="01a01887-6df3-79ef-b0ac-428588f80711">your</entity> door. | 'your' in Fen's message refers to Sasha, but it is tagged as Fen. |
| 8680 | text | Less than 0.62mi (1km). | A whole-beat line reads as a mechanical unit conversion ('Less than a kilometer'); 0.62mi is awkward in prose. |

## The Cabal (26)

| Beat | Kind | Quote | Problem |
|---|---|---|---|
| 23618 | continuity | Twenty-three hundred feet (700m) under the bottom of the pyramid. | The same drawing puts the circle at the bottom of the pyramid's 700 m root (40 m water + 700 m rock, #23599); saying 700 m under the bottom of the pyramid doubles the depth |
| 23686 | continuity | As if 2,336ft (712m) of rock and 131ft (40m) of lake and a black pyramid were all sitting on the air in this corridor | #23684 puts the junction 712 m under the BASE of the pyramid, and the pyramid itself runs ~700 m into the lakebed (#23599, Egregore record 702 m); so there is ~1.4 km of rock above the junction, not 712 m. The book wavers between 'under the base' and 'under the lakebed' |
| 23703 | continuity | a chair on the 111th floor of a building he had not left in seventeen years | Sparrow has Elias indoors thirteen years in 2226 (since 2213); by February 2231 that is eighteen years, not seventeen |
| 23575 | entity | with her strawberry-red braid over her left shoulder | Sable's record gives her dark red hair; 'strawberry-red' reads as a different (lighter) shade |
| 23579 | entity | thigh-deep in water 262ft (80m) to the bottom | Egregore's record puts it in about 40 m of water (wet band 40 m); 80 m to the bottom at the ring contradicts it |
| 23603 | entity | THE DOOR IS UNLOCKED. Her mother's square capitals, on a strip of tape across the lid of a dented aluminum case, for seven years. | Pixel's record has her mother dying when Pixel was 23 (c. 2220) and Pixel carrying the case since; by 2231 the tape has been on the lid at least eleven years, not seven |
| 23647 | entity | THE DOOR IS UNLOCKED, in her mother's square capitals, for seven years. | Same as #23603: per Pixel's record the mother died c. 2220, so by 2231 the tape has been on the case at least eleven years |
| 23693 | entity | The stone of missing her mother's voice that she had carried for seven years in her chest. | Pixel's record has her mother dying the winter Pixel was 23 (c. 2220); by 2231 that is about eleven years, not seven (same slip as the tape 'for seven years' in #23603/#23647) |
| 23711 | entity | He had carried it for sixteen years. The last four he had carried it loaded. He had never once fired it. | Cacophony's record says it was fired often in the years before (Bushido Coda) and last fired in 2226, and #23624 says 'not one of them fired since 2226'; 'never once fired it' contradicts both. The same beat's 'since he loaded it in 2227' also sits oddly with #23624's 'carried since 2215' |
| 23751 | entity | Thigh-deep in water that was 262ft (80m) to the bottom | Same as #23579: Egregore's record puts it in about 40 m of water, so the ring cannot be 80 m deep |
| 23569 | markup | a crew kid in a coat too big for him ... and <he~Bear> stepped out of Bear's way without being asked | 'he' is the crew kid stepping out of Bear's way, but it is tagged as Bear |
| 23574 | markup | the way the array always did <it~Kyle> for <him~Kyle> | 'it' (the arithmetic) is tagged as the character Kyle |
| 23604 | markup | <Heat>. <Heat>, thick, the heat of an engine room | Literal heat is tagged as the GLMZ vocabulary entity Heat |
| 23606 | markup | to look at <her~Faith>, because <her~Faith> own voice had said it out loud, plainly, the way Faith said things: I know the way. | It was Pixel's own voice that said 'I know the way' on the frozen road (#23601); 'her' x2 is tagged Faith instead of Pixel |
| 23608 | markup | the Sectionists' seminary in Hyde <Park> | 'Park' in the place name Hyde Park is tagged as the character Park |
| 23611 | markup | each with its own edge, <drifting> apart on the black water | Ordinary verb 'drifting' (ice plates) is tagged as the vocabulary entity drifting |
| 23612 | markup | <Cut> off. Every Speaker of the Consensus in the chamber, cut off upstream at once | Ordinary 'Cut off' is tagged as the vocabulary entity Cut |
| 23624 | markup | Three. Full. A .45 <Colt>, for hardware. | 'Colt' in the cartridge name .45 Colt is tagged as the subsidiary entity Colt |
| 23657 | markup | "<Read> them to me," said Ledger. "Slowly. One at a time." | Ordinary verb 'Read' (read the keys aloud) is tagged as the psionic archetype entity Read |
| 23680 | markup | The water there comes from out here; <I~Oarsman> never knew how far. | Glim is speaking; 'I' is tagged as the Oarsman instead of Glim |
| 23701 | markup | "<Wired>," said Rampart. On the wire. Hoarse. "Every position, Chief." | 'Wired' here means the field-set wire is run to every position, but it is tagged as the GLMZ vocabulary entity Wired |
| 23744 | markup | Past the sixteen, which Choi still held and which, Kyle understood, he would now have to carry to <her~Selvamani> himself. | 'her' refers to Choi (who holds the sixteen), but it is tagged as Selvamani |
| 23751 | markup | Rook and her crew were standing with them, Sefi at the <end of the line>. | Idiom 'at the end of the line' (of people) is tagged as the place entity 'end of the line'; same mis-tag in #23755 ('at the end of the line she added one word') |
| 23763 | markup | <Green> onion cut on a slant because a straight cut was lazy. | 'Green' in 'green onion' is tagged as the synthetic character Green; same mis-tag in #23768 |
| 23939 | markup | Her <Read> went out over them before she could stop it | The ability ('the Read') is tagged to the archetype entity Read (a person who reads); the archetype record says the ability is the separate vocabulary record The Read. Same mis-tag recurs across the book (e.g. #23583 'let the Read go out', #23587/#23590 'Faith Read her' as a verb), while #23584 correctly tags 'The Read' to the vocabulary record |
| 23758 | text | one decommissioned in 2229, the Gray Zone body-bank | Retired name 'the Gray Zone' (tagged to the Seams) used in prose, as in Sparrow #4091/#4231 |

## The Long Cut (1)

| Beat | Kind | Quote | Problem |
|---|---|---|---|
| 10362 | text | Deep enough down, the ferrocement scatters the signal — narrowband tracking dies at anything under a mile and a quarter (2km). | Distance threshold makes no sense for tunnel depth (2km down) and contradicts the 800m signal range stated earlier; likely a garbled number or wrong measure. |

## The Way Down (16)

| Beat | Kind | Quote | Problem |
|---|---|---|---|
| 6684 | continuity | "I never told him that part," Rampart said. | Contradicts The Way Up #6658, where Reza tells Tavi about the stranger in his door during the fever week over dinner ('I never told you... I'm telling you because you'd have told me'). |
| 6738 | markup | the Thread climbed out of the haze with its running lights on, going where it always went | 'the Thread' is tagged as the place entity but the following 'its'/'it' are tagged as the separate vocabulary entity Thread (01a1016d-9a65-7f7d-9273-3680683cde1f); same referent, two different entities. |
| 23933 | markup | and his shadow lay out ahead of him across the gold | Pronouns for the pursuing ArcSec officer (Lusk) are tagged as Rafi (character 019f78f4-2b8e-7cc0-9be9-6e9a004085cc). |
| 23951 | markup | It was <entity repo="event" guid="01a1ada8-3707-7ff6-b161-5f2988005962">loading</entity>. | The common verb 'loading' is tagged as the event 'The Skillet Row loader'; the tag belongs on a naming phrase, not a verb. |
| 6684 | text | six-foot-three (1.91m) of good jacket | Unit-conversion artifact: parenthetical metric figure left in narration. |
| 6713 | text | straight down, 10ft (3m) to the floor below | Unit-conversion artifact: parenthetical metric figure left in narration. |
| 6721 | text | The corridor was 39ft (12m), and at 39ft (12m) the Ankle-Biter's cheapness started to show. | Unit-conversion artifact, twice in one sentence; reads as tool output. |
| 6722 | text | 20ft (6m) and whatever each man's hands knew | Unit-conversion artifact: parenthetical metric figure left in narration. |
| 6725 | text | 10ft (3m) off, Reza was doing the same. | Unit-conversion artifact, and a sentence opening on a numeral. |
| 6728 | text | Rampart said, the kid had told him upstairs. | Garbled sentence: reads as if Rampart is speaking; intended meaning (the kid had called him Rampart upstairs) is unclear. |
| 6732 | text | one flight up and 33ft (10m) on | Unit-conversion artifact: parenthetical metric figure left in narration. |
| 6738 | text | six-foot-three (1.91m) of paid-for muscle | Unit-conversion artifact: parenthetical metric figure left in narration. |
| 6743 | text | saw him at 130ft (40m) ... six-foot-three (1.91m) of it | Unit-conversion artifacts (two) in one beat. |
| 22153 | text | six foot three (1.91m) of paid-for frame | Unit-conversion artifact: parenthetical metric figure left in narration. |
| 23933 | text | the Spire's lights 330ft (100m) straight up | Unit-conversion artifact: parenthetical metric figure left in narration. |
| 23951 | text | a DL-5 freight loader, 10ft (3m) tall | Unit-conversion artifact: parenthetical metric figure left in narration. |

## The Way Up (6)

| Beat | Kind | Quote | Problem |
|---|---|---|---|
| 6639 | entity | and <entity repo="character" guid="019d6143-a72d-7595-b50b-c63de7827e67">Dolores</entity> had had both hands on the rail | The dead Crestwood hauler 'Dolores' is linked to Dolores Finetti, whose record is a living grandmother and ex-Axiom administrator (status active); wrong entity link. |
| 22146 | markup | with an <entity repo="place" guid="27138f67-4386-85cf-ee28-97b31853a4db">open hand</entity> | Common phrase 'open hand' (a strike) is tagged as the place Open Hand (a Bridgeport restaurant). |
| 6602 | text | So did every vendor within 33ft (10m). | Unit-conversion artifact in prose: dual imperial/metric figure in parentheses reads as tool output, not narration. |
| 6641 | text | cut sight to 50ft (15m) | Unit-conversion artifact: parenthetical metric figure left in narration. |
| 6654 | text | had 4in (10cm) on him | Unit-conversion artifact: parenthetical metric figure left in narration. |
| 23932 | text | 6 tons (5.4t) of steel rolled along the track | Unit-conversion artifact in prose: '6 tons (5.4t)' parenthetical metric figure left in narration. |

## Underclan (6)

| Beat | Kind | Quote | Problem |
|---|---|---|---|
| 23788 | markup | after the street name some city crew had stencilled on its door | Literal phrase 'street name' (the stencilled road name Halsted) is tagged as the vocabulary term Street Name (an underworld alias identity); wrong entity on a common phrase. |
| 23809 | text | (paragraph breaks) | Paragraphs are separated by three blank lines instead of one throughout the beat; stray whitespace from an edit that will show as extra gaps on export. |
| 23825 | text | (paragraph breaks) | Paragraphs are separated by three blank lines instead of one throughout the beat; stray whitespace from an edit that will show as extra gaps on export. |
| 23837 | text | (paragraph breaks) | Paragraphs are separated by three blank lines instead of one throughout the beat; stray whitespace from an edit that will show as extra gaps on export. |
| 23846 | text | (paragraph breaks) | Paragraphs are separated by three blank lines instead of one throughout the beat; stray whitespace from an edit that will show as extra gaps on export. |
| 23851 | text | (paragraph breaks) | Paragraphs are separated by three blank lines instead of one throughout the beat; stray whitespace from an edit that will show as extra gaps on export. |

## Vultures at the Door (14)

| Beat | Kind | Quote | Problem |
|---|---|---|---|
| 21804 | continuity | and that tonight he had told his name to two strangers | Ekow gave his name at dawn and this beat is set the same morning; 'tonight' should be 'last night'/'this morning'. |
| 21539 | entity | the GLMZ — the Glooms, to anyone who had to live in it | GLMZ record says residents call it 'the zone' or 'the city'; 'the Glooms' is only the bottom of the stack, not the whole sprawl. |
| 21543 | markup | <entity repo="place" guid="ca1aface-abba-0bad-f00d-1234567890ab">Canaryville</entity> | Placeholder-looking guid (ca1aface-abba-0bad-f00d-...) on Canaryville tag; likely not a real entity id. |
| 21607 | markup | <entity repo="faction" guid="0260c60e-7a18-b1e4-d5f6-a708b9c0d1e2">Carrion</entity> <entity repo="faction" guid="0260c60e-7a18-b1e4-d5f6-a708b9c0d1e2">Reclamati | One name split into two tags, and the faction guid looks hand-made; next beat tags Carrion as synthetic 019d6143-a9f0-... instead. |
| 21608 | markup | <entity repo="synthetic" guid="019d6143-a9f0-71b2-8cc2-3d8b31b58eeb">Carrion</entity> had no rate code for it | Carrion (the reclamation company/depot) tagged as a synthetic entity; inconsistent with the faction tag in #21607. |
| 21647 | markup | He needed <entity repo="faction" guid="0260c60e-7a18-b1e4-d5f6-a708b9c0d1e2">them</entity> to look like a crew on a break | Pronoun 'them' (Tomas and Levin) tagged as the Carrion faction. |
| 21707 | markup | <entity repo="synthetic" guid="019d6143-a9f0-71b2-8cc2-3d8b31b58eeb">Carrion</entity>'s ticket was for the arms | Carrion (the company) tagged as a synthetic entity; elsewhere tagged as faction 0260c60e-... |
| 21767 | markup | <entity repo="faction" guid="0260c60e-7a18-b1e4-d5f6-a708b9c0d1e2">We</entity> didn't take him ... breathing because of <entity repo="faction" guid="0260c60e-7a | Tomas's 'We'/'us' (Tomas and Levin) tagged as the Carrion faction. |
| 21797 | markup | <entity repo="place" guid="01a13683-458a-7ba1-a37f-1a97b6c7695f">the south side</entity> | 'the south side' tagged with a different guid than elsewhere in the book (019d6143-a94e-728a-...). |
| 21603 | text | in his chest 6.5ft (2m) from the cold racks | Leftover unit-conversion scaffolding '6.5ft (2m)' in narrative prose. |
| 21652 | text | he's 39ft (12m) behind me, friend | Unit-conversion scaffolding '39ft (12m)' inside Tomas's interior thought. |
| 21713 | text | It sat at the curb 39ft (12m) off, dark and ordinary. But 39ft (12m) was nothing | Unit-conversion scaffolding '39ft (12m)' repeated twice in prose. |
| 21820 | text | platform was still lit 3mi (5km) out | Unit-conversion scaffolding '3mi (5km)' in prose. |
| 23922 | text | She had stopped the chair 20ft (6m) off | Unit-conversion scaffolding '20ft (6m)' in prose. |

## Wells (6)

| Beat | Kind | Quote | Problem |
|---|---|---|---|
| 23455 | continuity | Inside the intelligence stack of ARC, the Arcturus Research Center, in Chicago. | ARC is expanded as 'Arcturus Research Center' here, but Between the Lines defines ARC as the Aerostatic Regulatory Command (Arcturus's crossing franchise) and ties the same 606 node to 'ARC's stack'; the two books disagree on what ARC is. |
| 23470 | continuity | The Arcturus Research Center's checkpoint grid, every one, numbered | Same ARC conflict as #23455: Bishop's checkpoint grid belongs to the Aerostatic Regulatory Command in Between the Lines, not an 'Arcturus Research Center'. |
| 23513 | continuity | She had known Cutout for five weeks | Too short for the timeline: Cutout was already with the crew three weeks after the great pump (#23373), and the vault week, Able evenings (Ledger's 'five weeks' at #23425), dunes, eight-day corridor and ~2 weeks on the Erie shore all follow, so Pixel has known him roughly two months by now. |
| 23524 | continuity | Fourteen hundred and fourteen feet (431m) to the turn. Not four-twelve like the Colonnade | Puts the Colonnade's turn at 412 m, but the book everywhere else gives it as 1,300ft (400m) (#23379, #23464, #23505). |
| 23541 | continuity | Here, at 790ft (240m), the stone under his hand was warm. At three hundred it was warmer. At three hundred and ninety, exactly where Sasha had said, it was hot | In #23524 Sasha says the rock first goes warm at 390 m and is hot at 420 m; here it is already warm at 240 m and 'hot' at 390 'exactly where Sasha had said', which doesn't match what she said. |
| 23439 | text | A table in a Remnant block | Beat title says the table is in a Remnant block (Cleveland), but the beat is set in a horse stall of the dairy barn in the dunes past Gary; the title looks stale. |

## Resolution (PRS-T1034)

Owner decisions (2026-11-18): unit pairs such as "33ft (10m)" are house style and stay; entity-tag mis-links are fixed;
continuity and entity-record findings are left for the author.

Done through the Hub, every beat re-read after its change: 31 tags moved to the right entity, 31 occurrences marked "not an entity" (Park in Hyde/McKinley/Tinley Park, Grit, Gill, Green onion, Heat, Cut and the like), 4 broken unit conversions rewritten in the scene's own style. That includes six more mis-links of the same kind in Vultures at the Door that the readers had not listed, and every 'Read' ability tag in The Cabal (moved from the archetype to The Read). Tag-only edits kept their read receipts; the text fixes were re-read and receipted; every touched book still reads 0 unread.

Left as they are (9):

| Book | Beat | Why |
|---|---|---|
| bushido-coda | 20834 | #20834: who says 'I'll deal with that separately' (Kyle or Breckenridge) is a story/attribution judgment |
| killing-things-from-other-worlds-01a1e80d | 24007 | #24007 already carries The South Side (Chicago), the right entity; the inconsistency was resolved by retagging #23996 (finding 16) |
| magenta-gunmetal | 6968 | #6968: 'The Glooms' is the right entity; whether the usage fits the record's definition is an entity-record question for the author |
| the-cabal | 23744 | #23744: 'carry to her' could be Choi or Selvamani (the sixteen's owner); story judgment |
| vultures-at-the-door-019ec467 | 21543 | #21543: guid ca1aface-abba-0bad-f00d-1234567890ab is the live Canaryville place record (looks hand-made but is real); tag is correct |
| vultures-at-the-door-019ec467 | 21647 | #21647: 'them' (Tomas and Levin, a Carrion crew) on The Vultures follows the book's own convention for the crew; one pronoun tag cannot name two characters, so changing it is a judgment |
| vultures-at-the-door-019ec467 | 21767 | #21767: 'We'/'us' (Tomas and Levin as the Carrion crew) on The Vultures; same reason as #21647 |
| vultures-at-the-door-019ec467 | 21797 | #21797 already carries The South Side (Chicago), the right entity. Not touched, for follow-up: #21608, #21686, #21837 in this book still tag 'the south side' as Milwaukee's The South Side |
| the-long-cut | 10362 | the original depth is not recoverable from the text (contradicts the 800m signal range); choosing a number is a story judgment |
