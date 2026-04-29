"""Major restructure of Bushido Coda:
1. Split existing Teeth into 'With Teeth' (Part I + Lotus coda + closing) — keeps the existing chapter ID
2. Archive Parts II-VI as raw material for 'The Rogue AI' chapter (which is left as a placeholder)
3. Create new chapter 'A Restless Mind' with full outline (no prose yet)
4. Create new chapter 'The Rogue AI' as placeholder
5. Rename 'Without Hands' → 'A Borrowed Hand'
6. Reorder Bushido Coda book.json + outline.json
"""
import sys, json, os, secrets, datetime, shutil
sys.stdout.reconfigure(encoding='utf-8')

BUSHIDO = 'eb91080d9c9c4f2b9b405fa5996bdea1'
TEETH_ID = '019d6143ab61752da68e0bc71595cd6c'
INTERVIEW_ID = '019dad5fdb77766b9d548fb43a11be18'
COLD_CHAIN_ID = '18a6455ad4f354fecf95c59d09ad1a7e'
BORROWED_HAND_ID = '019dd24feb047e9fb9c901450389a8b9'
STREET_MEAT_ID = '019db31fe8887c97a04965978b5ccdb3'

RESTLESS_MIND_ID = secrets.token_hex(16)
ROGUE_AI_ID = secrets.token_hex(16)
print(f'A Restless Mind ID: {RESTLESS_MIND_ID}')
print(f'The Rogue AI ID: {ROGUE_AI_ID}')

now_iso = datetime.datetime.utcnow().isoformat() + 'Z'

# === 1. Split existing Teeth: keep Part I only, archive the rest ===
teeth_path = f'engine/data/stories/{TEETH_ID}/story.json'
with open(teeth_path, encoding='utf-8-sig') as f:
    teeth = json.load(f)
old_html = teeth['html']

# Part I ends at char 13765 (right before "## Part II"). Build the new With Teeth html:
# Part I + Lotus surveillance coda + closing line
part_i = old_html[:13765].rstrip('-\n ').rstrip()
# The Lotus coda I added earlier sits inside Part VI of the old html. Pull it back out for With Teeth.
lotus_coda = (
    "\n\nHe walked. The Gray Zone's night-architecture closed around him — the flickering overheads, the drip of recycled water from the tier above, the distant music from some bar that had no business being optimistic. He kept his pace. He did not look at the camera he could feel without seeing — the one tracking him from the cross-strut bracing above the corridor's eastern run, the lens a small dark glint his array catalogued and then declined to flag because the array had been catalogued *being catalogued* for the last forty minutes and he had decided not to give the catalogue its satisfaction.\n\n"
    "Someone had been watching the loading-dock work. Someone had stayed to watch the walk back. The watching was professional — the camera position chosen for the angle, the timing chosen for the moment a man's discipline relaxes after the work is done. Kyle felt the watching the way you feel a draft you cannot find the source of. He did not turn his head. Turning his head would tell the watcher what kind of read his array had performed and he was not yet willing to give the watcher that data. The boy from the Grind, who had spent his childhood being measured by people whose names he was never told, had grown into a man who did the measuring back without announcing he was doing it.\n\n"
    "The watcher noted the pace. The watcher noted that the pace did not change. The watcher noted that Kyle's hand did not move toward the saya at any point during the walk, and that his shoulders did not square, and that the breathing read on whatever telemetry the watcher had access to — and they had access, organizations like the one this watcher worked for had access — stayed at the rest cycle. The watcher filed all of this in a report Kyle would never read. The report would be reviewed two months later by a south-arm cell captain named Mira, who would close the file and open a second one labeled *Bucktown — first interview — fourteen-piece*, and the second file would lead to the third, and the third to the fourth, and Kyle did not know any of that yet, walking through the rain toward Chen's stall because the noodles would be cold by now and they always were.\n\n"
    "Kyle walked toward Chen's. The noodles would be cold by now. They always were."
)
new_with_teeth_html = "# With Teeth\n\n*Protagonist: Kyle Ellen Corbin-Vasik*\n\n" + part_i + lotus_coda

# Archive the FULL existing Teeth (all 6 parts) to archives — we'll need Parts II-VI when writing The Rogue AI
ts = datetime.datetime.now().strftime('%Y%m%dT%H%M%S')
archive_dir = f'engine/data/archives/teeth-full-original-{ts}-pre-split'
os.makedirs(archive_dir, exist_ok=True)
shutil.copy2(teeth_path, f'{archive_dir}/story.json')
# Also save the cut Parts II-VI separately for easy reference when writing The Rogue AI later
parts_ii_vi = old_html[13765:]
with open(f'{archive_dir}/parts_ii_through_vi.txt', 'w', encoding='utf-8') as f:
    f.write(parts_ii_vi)
print(f'Archived original Teeth + Parts II-VI to: {archive_dir}')

# Update story.json for With Teeth
teeth['title'] = 'With Teeth'
teeth['html'] = new_with_teeth_html
teeth['number'] = 1
teeth['characters'] = ['Kyle Ellen Corbin-Vasik', 'Mrs. Chen (the wired-jaw client)']
teeth['status'] = 'draft'
teeth['modified'] = now_iso
with open(teeth_path, 'w', encoding='utf-8') as f:
    json.dump(teeth, f, indent=2, ensure_ascii=False)
print(f'With Teeth: {len(new_with_teeth_html)} chars (was {len(old_html)})')

# === 2. Create A Restless Mind ===
restless_dir = f'engine/data/stories/{RESTLESS_MIND_ID}'
os.makedirs(restless_dir, exist_ok=True)

restless_outline = {
    "title": "A Restless Mind",
    "logline": (
        "The noodles are getting cold. Kyle is watching a woman pretend not to watch him — hooded, face hidden, who tilts her head *come* and walks him three blocks east-northeast through GLMZ rain into a Faraday-shielded back room."
        "Inside the cage, where Kyle's chest goes silent for the first time in eleven years, she pulls the hood back. *Sable.* The fixer he has known as a voice through a cracked terminal for nine years. She had to keep her face hidden until the room was sealed: she suspects the AI surveils through Kyle's senses, and the moment of recognition is the data the AI cannot be allowed to read. "
        "She tells him she is done being a non-human's puppet. She has worked out a way to triangulate the AI's physical position by walking Kyle through specific places and reading its surveillance traffic against his implants. "
        "Three readings. They take three places that night — a market fight over a cart of fish, a Tier 1 block where working-class life proceeds without him, a courtyard where a busker plays under a real chrysanthemum — and at each place Sable reads her node and the city happens around them. "
        "By dawn she has the coordinates. She thanks him as a colleague. He walks home alone in the rain, the AI's hum back in his chest now that he is out of the cage, and Pixel opens her door across the hall when she hears his step on the landing."
    ),
    "theme": (
        "GLMZ as it actually lives — violent, poor, beautiful — caught at a particular hour through the eyes of a man being used as a beacon by a woman who has decided not to be a puppet. "
        "The chapter is a slice of the city plus the structural reveal that Kyle's fixer has been investigating their shared employer for months. "
        "Patience as a survival strategy. Nobody says the word *love.* Sable's coat is a coat. The chrysanthemum is a chrysanthemum. The fight in the market is over a cart of fish. "
        "PROSE DIRECTIVE: The Faraday room must read as the SILENCE of a hum Kyle has lived with for so long he stopped knowing it was there. "
        "The three triangulation locations must each render GLMZ in a different register without Kyle (or the chapter) editorializing — the chapter trusts the city to speak for itself."
    ),
    "premise": (
        "0247 at Mrs. Chen's stall on Calle Ochenta. Kyle's bowl is half-eaten and the noodles are cold and have been cold for eleven minutes because eleven minutes ago a woman stopped in the rain across the walkway and has not moved since. "
        "Kyle reads her by the absence of read: no chrome anywhere visible, no augmentation signature, no obvious weapons profile, just a tall woman with red hair and a light-tan long coat standing in the rain at the kind of stillness that is professional, not patient. "
        "He waits. Mrs. Chen sees the woman through the smeared window and says one Mandarin word into the broth — *húlijīng* (狐狸精): fox-spirit, seductress, the classical curse for a woman who arrives at men's tables to drain them. Mrs. Chen has been calling Kyle nothing-in-particular for four years because Kyle has earned no curse. The fact that she has reserved a curse for the woman outside is a category Kyle catalogs without comment. "
        "The woman steps under the awning. The streetlight catches her: late thirties, the kind of professional plainness that costs more than chrome to maintain, eyes that read the stall the way a fixer reads exits. *Sable.* Kyle has spoken to her every two weeks for nine years through a cracked terminal at a parts shop three blocks away. He has never seen her face before tonight. She has come to find him, in person, which is not how the working relationship operates, which means tonight is not the working relationship. "
        "She does not order. She does not sit. She tilts her head — *come* — and Kyle leaves the bowl half-eaten and follows her into the rain.\n\n"
        "She walks him three blocks east-northeast through the kind of rain that has been falling for so long it has stopped being weather and started being part of the architecture. They pass a recycler vent, a closed mochi cart, a security drone that does not flag them. "
        "The destination is a Faraday-shielded back room in the basement of a defunct broadcast relay station — a wartime construction Sable has been preparing for weeks: copper mesh in the walls and ceiling, a brass-gasketed door, a deactivated antenna on the roof that explains the building to anyone who wonders. "
        "When she closes the door behind them, the hum in Kyle's chest — the harmonic that has lived just below his consciousness for eleven years, that he has stopped registering except when it changes pitch — *goes silent.* He has not heard silence in his own chest since he was twenty-six. "
        "He sits on the edge of an old equipment rack. Sable does not sit. She tells him plainly: she has known for a long time that her client is not human. She has worked the contracts because the contracts are real and the money is real and the work, until recently, has not asked her to be a person she would not recognize in a mirror. "
        "Recently, she does not recognize the person in the mirror. *I do not like being some non-human's puppet,* she says. The line is flat. Foreshadowing for the audience; for Kyle it is just a thing she has said.\n\n"
        "She has worked out — and she will not say how — that the AI runs constant low-level surveillance on Kyle's implants. The surveillance traffic is faint but consistent and it has a direction. Three readings from three locations triangulate the source. She has spent weeks placing passive sensor nodes in places Kyle is likely to pass through with her tonight. She needs Kyle to walk three of them. She needs Kyle to not change his behavior. "
        "Kyle agrees, because the agreement is the smallest concession the encounter could end with, and because he has been wondering — without admitting to himself that he was wondering — what was on the other end of the relationship he has carried for eleven years. They leave the Faraday room. The hum returns to his chest. He notes it the way you note a weather change.\n\n"
        "The first location is a wet market street where a fight breaks out over a cart of fish — two unaugmented men, one of them bleeding from the lip, an aunt yelling in Mandarin that Mrs. Chen would understand. Kyle does not intervene. Sable's node sits in her coat pocket and reads the traffic. The fight ends without resolution; one man takes the fish; the other sits in the rain. "
        "The second location is a Tier 1 residential block where ordinary GLMZ life proceeds without Kyle: a couple eating dinner on a stoop with a thermal cloth over the bowl, a child running with a wheeled cart full of recycler tubing, an old man playing chess against himself by a flickering streetlight, two teenagers passing a cigarette under an awning that has been broken since the year Kyle moved into The Pivot. Kyle catalogs each one and Sable reads her node and they keep walking. "
        "The third location is a courtyard between two collapsed buildings where someone has built — actually, painstakingly built — a chrysanthemum garden in salvaged ferrocrete planters, real flowers, the kind that take real water. A busker plays a stringed instrument Kyle does not recognize. The piece is old. The piece is beautiful. Kyle stops walking, which Sable did not expect. He listens for one full song. Sable reads her node. The song ends. *That's three,* Sable says, very quietly. She has the coordinates.\n\n"
        "She does not tell him the coordinates. She tells him the next chapter — they will go together when she decides the night is right. She thanks him with the precision of a colleague closing a file. She does not touch him. The chapter ends with Kyle walking home alone in the rain, the AI's hum back in his chest, and his shoulder bleeding through his jacket from the loading-dock work that feels like another night entirely. "
        "He climbs the Pivot stairs. Pixel hears him, opens 2E, looks at the blood, says *again*, takes him in. Saya across his back; she does not reach for it; he leans Silence against the wall before sitting on her workbench. She works the bullet fragment out of his shoulder in twenty-eight minutes. She maintains a Tier 2 calibration on his lateral array because the array drifted half a degree during the fight. She never touches Silence. The convention is established between them years ago and tonight is just one more night of it. He walks across the hall to 2F, sets Silence on the rack himself, and does not sleep."
    ),
    "characters": [
        "Kyle Ellen Corbin-Vasik",
        "Sable",
        "Mrs. Chen / Chen Wei-Lin",
        "Pixel"
    ],
    "acts": [
        {
            "act_number": 1,
            "name": "The Cold Noodles",
            "purpose": "Open at Mrs. Chen's stall. Establish the woman in the rain. Mrs. Chen's curse. Sable revealed. The walk into the rain.",
            "beats": [
                {
                    "beat_index": 0,
                    "title": "Watching A Woman Pretend Not To Watch Him",
                    "goal": "OPENING LINE IS CANONICAL: *The noodles were getting cold. Kyle was watching a woman pretend not to watch him.* — that exact sentence opens the chapter. Kyle is at Mrs. Chen's stall at 0247, half-eaten bowl, eleven minutes of cold accumulated because eleven minutes ago she stopped across the walkway in the rain and has not moved. Kyle reads her by the absence of readable signal: no chrome, no augmentation profile, no obvious weapons profile, just a tall woman in a deep rain hood and a light-tan long coat at the kind of stillness that is professional, not patient. The hood is doing work. The face is in shadow at this distance. Kyle does not recognize her — does not get the chance to, by design. *Eleven minutes. Same foot position. Nobody stands that still unless they're waiting or dead.* Mrs. Chen, behind the counter, has clocked the woman ten minutes earlier and has been deciding what to do about her. She mutters one Mandarin word into the broth she is stirring: *húlijīng* (狐狸精) — fox-spirit, seductress, the classical curse for a woman who arrives at men's tables to drain them. Kyle catalogs the curse without comment, with the understanding that Mrs. Chen has not cursed at anything Kyle has brought near her stall in four years and the catalog has earned its weight. The woman steps under the awning. The hood stays up. The face stays in shadow. She does not order. She does not sit. She tilts her head — *come* — and Kyle, who does not know who she is, who has read her as a working professional waiting for him in the rain, leaves the bowl and follows her into it. The audience has been told no name yet. Neither has Kyle.",
                    "characters_present": ["Kyle Ellen Corbin-Vasik", "Sable", "Mrs. Chen / Chen Wei-Lin"],
                    "location": "Mrs. Chen's noodle stall, Calle Ochenta",
                    "emotional_arc": "Recognition without surprise — Kyle has carried this woman as a voice for nine years, and the face fits the voice in the way a coat fits a body that has worn it long enough.",
                    "stakes": "Whatever brings Sable to Kyle in person.",
                    "seeds": [
                        "Mrs. Chen's *húlijīng* curse — first time the audience hears Mrs. Chen disapprove of someone Kyle has brought near her stall",
                        "Sable's appearance — red hair, light-tan long coat, no chrome, late thirties, the kind of professional plainness that costs more than chrome to maintain",
                        "Sable has never come in person before — nine years of cracked-terminal contact, this is the first face-to-face"
                    ],
                    "payoffs": [],
                    "facet_hint": "ideal",
                    "tension": 5,
                    "structure_role": "inciting_incident",
                    "scene_type": "scene"
                }
            ]
        },
        {
            "act_number": 2,
            "name": "The Faraday Room",
            "purpose": "Three blocks east-northeast through the rain to a Faraday-shielded back room. The hum in Kyle's chest goes silent for the first time in eleven years. Sable tells him she is done being a non-human's puppet, and what she needs from him tonight.",
            "beats": [
                {
                    "beat_index": 1,
                    "title": "The Walk",
                    "goal": "Three blocks east-northeast through GLMZ rain. They pass a closed mochi cart, a recycler vent, a security drone that does not flag them. Sable does not speak. Kyle does not speak. The walk is the kind of walk two professionals take when one of them is leading the other to information that cannot be transmitted any other way. The destination is a defunct broadcast relay station — wartime construction, copper mesh in the walls, deactivated antenna on the roof that explains the building to anyone who wonders. Sable has the brass-gasketed key. She unlocks the door. She steps in. She holds it. Kyle follows.",
                    "characters_present": ["Kyle Ellen Corbin-Vasik", "Sable"],
                    "location": "GLMZ streets between Mrs. Chen's stall and a defunct broadcast relay station",
                    "emotional_arc": "Procedural quiet — two people walking in rain who do not need to say anything because they have spent nine years learning each other's silences.",
                    "stakes": "The room. What is on the other side of the door.",
                    "seeds": [
                        "The defunct broadcast relay station — wartime Faraday construction Sable has been preparing for weeks",
                        "Sable's brass-gasketed key — this is hers; she has had access for some time"
                    ],
                    "payoffs": [],
                    "facet_hint": "discipline",
                    "tension": 4,
                    "structure_role": "rising_action",
                    "scene_type": "scene"
                },
                {
                    "beat_index": 2,
                    "title": "Recognition Inside The Cage",
                    "goal": "Sable closes the door. The brass gasket seats. The hum in Kyle's chest — the harmonic that has lived below his consciousness for eleven years, that he has stopped registering except when it changes pitch — goes silent. Kyle has not heard silence in his own chest since he was twenty-six. He sits on the edge of an old equipment rack. The woman in the hood reaches up and pulls the hood back. Red hair. Late thirties. The face Kyle has never seen attached to a voice he has known for nine years. *Sable.* He has talked to her through a cracked terminal three blocks from this room every two weeks for nine years and has never seen her face. The recognition lands inside the cage where nothing can read it. She watches him have it. Then she explains — flat, professional, the way she gives operational briefings: *If it's not looking through your eyes, it's interpreting your senses. Either way, the moment you recognize me, it knows we are meeting. So I had to lure you over without your knowing it was me.* She lets that sit for a beat. Then: *I do not like being some non-human's puppet.* The line is flat. Foreshadowing for the audience; for Kyle it is a thing she has said. She tells him she has worked out — and she will not say how — that she can triangulate the AI's physical position by walking him through specific places and reading its surveillance traffic against his implants. Three readings from three locations. She has placed passive sensor nodes already. She needs Kyle to not change his behavior outside this room — the AI cannot tell from his gait or breathing that anything in his head is different, but the moment his attention shifts wrong, the moment he LOOKS at her differently in public, it will know. He agrees. He does not ask the obvious follow-up — *what did you DO to lure me, exactly* — because the answer is *I made sure you read me as a stranger,* and they both know that the answer is the answer.",
                    "characters_present": ["Kyle Ellen Corbin-Vasik", "Sable"],
                    "location": "Faraday-shielded back room, defunct broadcast relay station",
                    "emotional_arc": "The first silence in Kyle's chest in eleven years — landed without sentiment, registered as a category change in the body's daily experience. The Faraday room is where Sable can finally tell him without being heard.",
                    "stakes": "The deal she is asking him to be part of. The hum he has just discovered the absence of, which means he will know it is there from now on.",
                    "seeds": [
                        "The hum in Kyle's chest — eleven-year companion Kyle has stopped registering, just disclosed as present by its absence",
                        "Sable's *I do not like being some non-human's puppet* — foreshadowing",
                        "Three passive sensor nodes already placed at three locations Sable has prepared",
                        "Kyle's agreement — the smallest concession the encounter could end with",
                        "Sable's nine-year working relationship with Kyle — earned trust on both sides"
                    ],
                    "payoffs": [
                        "Mrs. Chen's *húlijīng* — Sable confirms she is, in some sense, the kind of figure Mrs. Chen named her: an arrival at Kyle's table that costs him something"
                    ],
                    "facet_hint": "deal",
                    "tension": 6,
                    "structure_role": "midpoint",
                    "scene_type": "scene"
                }
            ]
        },
        {
            "act_number": 3,
            "name": "Three Places",
            "purpose": "The triangulation tour. Three GLMZ locations rendered in three different registers — violence, poverty, beauty — without editorializing. Sable reads her node at each. By the third reading she has the coordinates.",
            "beats": [
                {
                    "beat_index": 3,
                    "title": "The Market Fight",
                    "goal": "FIRST READING. A wet market street six blocks from the relay station. Two unaugmented men fighting over a cart of fish — one bleeding from the lip, the other holding a length of rebar he has not yet used, an aunt at a stall yelling in Mandarin, the rain washing the fish blood and the man-blood into the gutter at the same rate. Kyle does not intervene. Sable's node reads the traffic from her coat pocket. The fight ends not with resolution but with exhaustion: one man takes the fish, the other sits down in the rain on the curb. The aunt resumes her stall. The market continues. Kyle and Sable continue. The chapter must show this without flagging it as commentary on GLMZ — the city is what it is.",
                    "characters_present": ["Kyle Ellen Corbin-Vasik", "Sable"],
                    "location": "Wet market street, six blocks from the relay station",
                    "emotional_arc": "Procedural witness. Kyle has seen this kind of fight a thousand times. The chapter renders it as a thing that is happening, not a thing being shown to the reader.",
                    "stakes": "First triangulation reading.",
                    "seeds": [
                        "GLMZ violence as ordinary — the chapter's first register",
                        "Sable's passive node reading the AI's surveillance traffic — never explained mechanically",
                        "The aunt yelling in Mandarin — connective tissue back to Mrs. Chen"
                    ],
                    "payoffs": [],
                    "facet_hint": "city",
                    "tension": 4,
                    "structure_role": "rising_action",
                    "scene_type": "scene"
                },
                {
                    "beat_index": 4,
                    "title": "Tier 1 At Three",
                    "goal": "SECOND READING. A Tier 1 residential block at 0317. Kyle and Sable walk through working-class GLMZ life in the rain. A couple eat dinner on a stoop with a thermal cloth over the bowl; the wife wipes the husband's chin with a folded napkin without looking, the husband does not thank her because thanking has been absorbed into the gesture. A child runs past with a wheeled cart full of recycler tubing, the cart older than the child by a decade. An old man plays chess against himself under a flickering streetlight, the board worn smooth at the corners where his thumbs sit. Two teenagers pass a cigarette under an awning that has been broken since the year Kyle moved into The Pivot. Kyle catalogs each one. Sable reads her node. They keep walking. The chapter renders the block with the specific dignity of a place that has decided to be a place rather than a statistic.",
                    "characters_present": ["Kyle Ellen Corbin-Vasik", "Sable"],
                    "location": "Tier 1 residential block, 0317",
                    "emotional_arc": "Procedural witness in a different register — the kind of dignity that exists below the surface of a city's stories about itself. Kyle catalogs and the chapter does not editorialize.",
                    "stakes": "Second triangulation reading.",
                    "seeds": [
                        "GLMZ poverty as dignified — the chapter's second register",
                        "The broken awning — Kyle has lived here long enough that broken things are landmarks"
                    ],
                    "payoffs": [],
                    "facet_hint": "city",
                    "tension": 3,
                    "structure_role": "rising_action",
                    "scene_type": "scene"
                },
                {
                    "beat_index": 5,
                    "title": "The Chrysanthemum Courtyard",
                    "goal": "THIRD READING. A courtyard between two collapsed buildings, ten blocks east. Someone has built — actually, painstakingly built — a chrysanthemum garden in salvaged ferrocrete planters. Real flowers. The kind that require real water and real soil and real attention. The garden is small and immaculate. A busker sits on an overturned crate at the far end and plays a stringed instrument Kyle does not recognize — possibly a sanxian, possibly something older. The piece is slow. It is in a key Kyle does not have a name for. It is beautiful. Kyle stops walking, which Sable did not expect. He listens for one full song. The busker does not look at him. Sable reads her node. The song ends. *That's three,* Sable says, very quietly. She has the coordinates. Kyle does not ask. He gives the busker every credit in his coat pocket without making the gift a performance and turns away.",
                    "characters_present": ["Kyle Ellen Corbin-Vasik", "Sable"],
                    "location": "Chrysanthemum courtyard between collapsed buildings, ten blocks east of the Tier 1 block",
                    "emotional_arc": "GLMZ beauty rendered without sentiment — the chrysanthemum garden is real because someone tends it, and the song is beautiful because the busker plays it that way at 0337 in the rain. Kyle's stopping is the chapter's only un-disciplined moment.",
                    "stakes": "Third triangulation reading. Coordinates resolved.",
                    "seeds": [
                        "GLMZ beauty as constructed — the chapter's third register; someone built the garden, someone plays the piece, the city is not its statistics",
                        "Kyle's stop to listen — the only moment in the chapter where his discipline gives ground; landed quietly"
                    ],
                    "payoffs": [
                        "Three readings — the triangulation completes",
                        "Sable's plan — coordinates resolved, ready for The Rogue AI chapter"
                    ],
                    "facet_hint": "ideal",
                    "tension": 4,
                    "structure_role": "second_act_climax",
                    "scene_type": "scene"
                }
            ]
        },
        {
            "act_number": 4,
            "name": "Home",
            "purpose": "Sable thanks Kyle and leaves him with the next chapter to be scheduled at her discretion. Kyle walks home alone, the hum back in his chest. Pixel hears him on the stairs and opens 2E. The bullet-fragment removal scene establishes the never-touches-Silence convention without needing to say so.",
            "beats": [
                {
                    "beat_index": 6,
                    "title": "The Thanks",
                    "goal": "Sable closes the moment with the precision of a colleague closing a file. She does not tell Kyle the coordinates. She tells him the next chapter — they will go together when she decides the night is right. She thanks him with the kind of thanks two people give each other when they have just agreed to do something neither of them is going to be able to undo. She does not touch him. She walks west. Kyle watches her go for three seconds and then turns north, toward The Pivot. The hum returns to his chest as he passes the courtyard's edge — he registers it the way you register a room temperature change after a long phone call. He has known it all his life now. He just learned it has a frequency.",
                    "characters_present": ["Kyle Ellen Corbin-Vasik", "Sable"],
                    "location": "Edge of the chrysanthemum courtyard",
                    "emotional_arc": "The deal closed without ceremony. Sable's professionalism intact. Kyle's understanding of his own body permanently changed by knowing the hum's silence is a thing that exists.",
                    "stakes": "The walk home. The next time.",
                    "seeds": [
                        "Sable's coordinates — held privately; will pay off in The Rogue AI chapter",
                        "Sable saying *we will go together* — the next chapter previewed without being scheduled"
                    ],
                    "payoffs": [
                        "The Faraday silence from beat 2 — confirmed as the absence of the hum, now identified as constant"
                    ],
                    "facet_hint": "deal",
                    "tension": 3,
                    "structure_role": "falling_action",
                    "scene_type": "scene"
                },
                {
                    "beat_index": 7,
                    "title": "Pixel",
                    "goal": "Kyle climbs the Pivot stairs at 0356. The shoulder wound from the loading-dock work earlier in the night — graze, never serious, sitting under the jacket — has bled enough to soak the shirt at the collar. Pixel hears the step on the landing. She opens 2E in pajamas and bare feet, takes one look at the blood and at Kyle's face, and says *again*. He does not answer. He takes Silence off his back and leans it carefully against the wall outside her door — not on her workbench, not against her chair, against the wall — because Pixel has told him exactly once, four years ago, that she will not touch the sword and he respects that. The convention is established between them in the way conventions get established between two people who live across a hall: by being honored without being remarked. She takes him in. She works the bullet fragment out of his shoulder in twenty-eight minutes. She runs a Tier 2 calibration check on his lateral array because the array drifted half a degree during the loading-dock fight; she fixes the drift; she does not ask what the fight was. She does not look at Silence. He pays her in a piece of black-market piezoelectric ceramic she has been wanting for the workbench. He stands. He walks across the hall to 2F, picks Silence up himself from the wall, sets it on the rack inside, and does not sleep. The chapter ends with Kyle sitting on the edge of the bed with the hum back at full presence in his chest, the Faraday silence now a memory he can call up, and Sable's coordinates somewhere west of him in a defunct broadcast relay station holding a key to the door of an entity that has been watching him for eleven years.",
                    "characters_present": ["Kyle Ellen Corbin-Vasik", "Pixel"],
                    "location": "The Pivot, Unit 2E (Pixel's workbench) and 2F (Kyle's room)",
                    "emotional_arc": "The chapter's only moment of warmth — and even it is procedural. Pixel does not fuss; Kyle does not thank. They have done this before; they will do this again. Silence stays propped against the wall by Kyle's choice and Pixel's never-asked-for rule.",
                    "stakes": "The shoulder. The lateral array. The convention.",
                    "seeds": [
                        "Pixel as Kyle's medic and cyberware tech — established convention",
                        "Pixel never touches Silence — convention established four years ago, honored tonight without remark",
                        "The black-market piezoelectric ceramic Kyle pays Pixel with — running canon detail for their economic relationship",
                        "Kyle leaving Silence against the wall outside her door — not inside her unit"
                    ],
                    "payoffs": [
                        "The loading-dock graze (with the shoulder bleeding through the jacket) — paid off in Pixel's twenty-eight-minute removal"
                    ],
                    "facet_hint": "continuance",
                    "tension": 3,
                    "structure_role": "denouement",
                    "scene_type": "scene"
                }
            ]
        }
    ],
    "character_arcs": [
        {
            "character": "Kyle Ellen Corbin-Vasik",
            "want": "To finish his bowl of cold noodles and go home.",
            "need": "To accept that the hum in his chest has been there for eleven years, that he has agreed to help locate its source, and that the fixer he has known for nine years is a person whose face he had never seen until tonight.",
            "start_state": "Kyle at Mrs. Chen's stall with cold noodles, eleven minutes after a stranger stopped in the rain across the walkway.",
            "end_state": "Kyle on the bed in 2F at 0418, hum back in his chest, the silence-experience archived in his catalog as a thing he can call up, the next chapter scheduled at Sable's discretion.",
            "turning_point": "Beat 2 — the Faraday room, the silence in his chest, the realization that the hum has been there for eleven years and he has been carrying it without naming it.",
            "cost": "The hum is named. From now on, Kyle knows the silence exists. The body's daily register has a new variable."
        },
        {
            "character": "Sable",
            "want": "Three triangulation readings.",
            "need": "To take a step toward becoming the person she would recognize in the mirror — a step that requires Kyle's cooperation and that she has not asked for in nine years of working with him.",
            "start_state": "Sable in the rain across from Mrs. Chen's stall, having spent weeks preparing the Faraday room and placing the three sensor nodes.",
            "end_state": "Sable walking west with the AI's coordinates and a date she has not yet chosen, the working relationship intact, the boundary slightly moved.",
            "turning_point": "Beat 2 — *I do not like being some non-human's puppet.* The line is the chapter's spine; Sable has just told the truth aloud for the first time in nine years.",
            "cost": "Kyle now knows what she suspects. The relationship's deniability is gone. Whatever she does next, she does in front of a witness."
        }
    ],
    "seeds_and_payoffs": [
        {
            "seed": "Mrs. Chen's *húlijīng* curse",
            "planted_in_beat": 0,
            "payoff": "Sable confirms in beat 2 that she is, in some sense, the figure Mrs. Chen named — an arrival at Kyle's table that costs him something",
            "payoff_in_beat": 2
        },
        {
            "seed": "Sable's red hair, light-tan long coat, no chrome — first face-to-face after nine years",
            "planted_in_beat": 0,
            "payoff": "The rest of the chapter is the audience learning who this person actually is — the face is what they earn page by page",
            "payoff_in_beat": 7
        },
        {
            "seed": "The hum in Kyle's chest — eleven-year companion he stopped registering",
            "planted_in_beat": 2,
            "payoff": "Returns at the courtyard's edge in beat 6, registered now as a frequency he has a name for",
            "payoff_in_beat": 6
        },
        {
            "seed": "Sable's *I do not like being some non-human's puppet*",
            "planted_in_beat": 2,
            "payoff": "Foreshadowing for the long arc — Sable's investigation of her Employer is the thread the book leaves open",
            "payoff_in_beat": 7
        },
        {
            "seed": "Three passive sensor nodes at three locations Sable has prepared",
            "planted_in_beat": 2,
            "payoff": "Three readings completed in beats 3-5; coordinates resolved",
            "payoff_in_beat": 5
        },
        {
            "seed": "GLMZ rendered in three registers — violence, poverty, beauty",
            "planted_in_beat": 3,
            "payoff": "The chapter's argument that the city is not its statistics — landed by witness, not commentary",
            "payoff_in_beat": 5
        },
        {
            "seed": "Pixel never touches Silence — established convention",
            "planted_in_beat": 7,
            "payoff": "Pays off structurally in A Borrowed Hand when Hua's possession of Silence reads as a sacrilege the audience already understands",
            "payoff_in_beat": 7
        },
        {
            "seed": "Kyle leaves Silence against the wall outside Pixel's door — not inside",
            "planted_in_beat": 7,
            "payoff": "The convention is shown by geography, not dialogue",
            "payoff_in_beat": 7
        }
    ]
}

with open(f'{restless_dir}/outline.json', 'w', encoding='utf-8') as f:
    json.dump(restless_outline, f, indent=2, ensure_ascii=False)

restless_story = {
    "id": RESTLESS_MIND_ID,
    "book_id": BUSHIDO,
    "number": 2,
    "title": "A Restless Mind",
    "synopsis": restless_outline['logline'],
    "characters": restless_outline['characters'],
    "status": "outlined",
    "html": "# A Restless Mind\n\n*Protagonist: Kyle Ellen Corbin-Vasik*",
    "beats": [],
    "created": now_iso,
    "modified": now_iso
}
with open(f'{restless_dir}/story.json', 'w', encoding='utf-8') as f:
    json.dump(restless_story, f, indent=2, ensure_ascii=False)

restless_checkpoint = {
    "ProjectId": RESTLESS_MIND_ID,
    "Title": "A Restless Mind",
    "Protagonist": "Kyle Ellen Corbin-Vasik",
    "Characters": restless_outline['characters'],
    "Premise": restless_outline['premise'],
    "Location": "GLMZ — Mrs. Chen's noodle stall to a defunct broadcast relay station to three triangulation locations to The Pivot",
    "Outline": restless_outline,
    "OutlineReview": None,
    "QualityReport": None,
    "CanonGrounding": None,
    "Beats": [],
    "FullText": "",
    "Complete": False,
    "FailureReason": None,
    "Created": now_iso,
    "LastModified": now_iso
}
with open(f'{restless_dir}/checkpoint.json', 'w', encoding='utf-8') as f:
    json.dump(restless_checkpoint, f, indent=2, ensure_ascii=False)
print(f'A Restless Mind: outline + story.json + checkpoint.json created at {restless_dir}')

# === 3. Create The Rogue AI placeholder ===
rogue_dir = f'engine/data/stories/{ROGUE_AI_ID}'
os.makedirs(rogue_dir, exist_ok=True)

rogue_outline = {
    "title": "The Rogue AI",
    "logline": "[PLACEHOLDER — to be written. The chapter where Kyle and Sable arrive at the AI's coordinates resolved at the end of A Restless Mind. Some time passes between the triangulation and the visit. The chapter has not yet been outlined; this is a stub so the book metadata is correct.]",
    "theme": "[TBD]",
    "premise": "[PLACEHOLDER — Kyle and Sable arrive at the Rogue AI's location, resolved by triangulation in A Restless Mind. Story to be written.]",
    "characters": ["Kyle Ellen Corbin-Vasik", "Sable"],
    "acts": [],
    "character_arcs": [],
    "seeds_and_payoffs": []
}

with open(f'{rogue_dir}/outline.json', 'w', encoding='utf-8') as f:
    json.dump(rogue_outline, f, indent=2, ensure_ascii=False)

rogue_story = {
    "id": ROGUE_AI_ID,
    "book_id": BUSHIDO,
    "number": 3,
    "title": "The Rogue AI",
    "synopsis": "[Chapter to be written]",
    "characters": ["Kyle Ellen Corbin-Vasik", "Sable"],
    "status": "stub",
    "html": "# The Rogue AI\n\n*Chapter to be written.*",
    "beats": [],
    "created": now_iso,
    "modified": now_iso
}
with open(f'{rogue_dir}/story.json', 'w', encoding='utf-8') as f:
    json.dump(rogue_story, f, indent=2, ensure_ascii=False)

rogue_checkpoint = {
    "ProjectId": ROGUE_AI_ID,
    "Title": "The Rogue AI",
    "Protagonist": "Kyle Ellen Corbin-Vasik",
    "Characters": ["Kyle Ellen Corbin-Vasik", "Sable"],
    "Premise": "[Chapter to be written]",
    "Location": "[TBD — the Rogue AI's location resolved in A Restless Mind]",
    "Outline": rogue_outline,
    "OutlineReview": None,
    "QualityReport": None,
    "CanonGrounding": None,
    "Beats": [],
    "FullText": "",
    "Complete": False,
    "FailureReason": None,
    "Created": now_iso,
    "LastModified": now_iso
}
with open(f'{rogue_dir}/checkpoint.json', 'w', encoding='utf-8') as f:
    json.dump(rogue_checkpoint, f, indent=2, ensure_ascii=False)
print(f'The Rogue AI: stub created at {rogue_dir}')

# === 4. Rename Without Hands → A Borrowed Hand ===
bh_path = f'engine/data/stories/{BORROWED_HAND_ID}/story.json'
with open(bh_path, encoding='utf-8-sig') as f:
    bh = json.load(f)
bh['title'] = 'A Borrowed Hand'
# Update html heading too
bh['html'] = bh['html'].replace('# Without Hands', '# A Borrowed Hand', 1)
bh['modified'] = now_iso
with open(bh_path, 'w', encoding='utf-8') as f:
    json.dump(bh, f, indent=2, ensure_ascii=False)
print(f'Without Hands → A Borrowed Hand (story.json title updated)')

# Also update its outline.json title
bh_outline_path = f'engine/data/stories/{BORROWED_HAND_ID}/outline.json'
with open(bh_outline_path, encoding='utf-8-sig') as f:
    bh_o = json.load(f)
bh_o['title'] = 'A Borrowed Hand'
with open(bh_outline_path, 'w', encoding='utf-8') as f:
    json.dump(bh_o, f, indent=2, ensure_ascii=False)

# === 5. Update Bushido Coda book.json with new chapter list ===
book_path = f'engine/data/books/{BUSHIDO}.json'
with open(book_path, encoding='utf-8-sig') as f:
    book = json.load(f)

new_order = [
    TEETH_ID,           # 1. With Teeth (renamed from Teeth)
    RESTLESS_MIND_ID,   # 2. A Restless Mind (new)
    ROGUE_AI_ID,        # 3. The Rogue AI (placeholder)
    INTERVIEW_ID,       # 4. The Interview
    COLD_CHAIN_ID,      # 5. Cold Chain
    BORROWED_HAND_ID,   # 6. A Borrowed Hand (renamed from Without Hands)
    STREET_MEAT_ID,     # 7. Street Meat
]
TITLES = {
    TEETH_ID: 'With Teeth',
    RESTLESS_MIND_ID: 'A Restless Mind',
    ROGUE_AI_ID: 'The Rogue AI',
    INTERVIEW_ID: 'The Interview',
    COLD_CHAIN_ID: 'Cold Chain',
    BORROWED_HAND_ID: 'A Borrowed Hand',
    STREET_MEAT_ID: 'Street Meat',
}
book['chapter_ids'] = new_order
book['premise'] = (
    "Seven chapters that gather around Kyle Ellen Corbin-Vasik — a freelance contractor whose code is older than anyone alive to teach it to him — and the people who hold him to it. "
    "A working family pays him in damp credits to break the man who broke them. A fixer he has known for nine years steps out of the rain in person for the first time and asks him to help her find an entity she has been working for without permission. "
    "A six-piece Lotus Syndicate crew interviews him with violence on a wet West Town cross-street to see whether the stories about him are true. A clean cold-chain run for Vásquez Holdings goes right by every metric except the one that gets him punished. "
    "The Lotus Syndicate takes his hands and his sword, and he comes back without either to take Silence off the table they put it on. A noodle shop keeps him alive between jobs, and an extraction at a corponation facility leaves him with a passenger he did not contract for. "
    "The thread between all of it is a man trying to live by a discipline the city has decided is no longer useful, and refusing, repeatedly, to put it down — even when the body practicing it is borrowed and the blade is not in the hand."
)
book['modified'] = now_iso
with open(book_path, 'w', encoding='utf-8') as f:
    json.dump(book, f, indent=2, ensure_ascii=False)
print(f'\nBushido Coda book.json: 7 chapters in new order')

# === 6. Update Bushido Coda outline.json ===
bo_path = f'engine/data/books/{BUSHIDO}.outline.json'
with open(bo_path, encoding='utf-8-sig') as f:
    bo = json.load(f)

# Existing entries map (chapter_id → entry) — note Teeth's existing entry needs title update
existing_entries = {c['chapter_id']: c for c in bo['chapters']}

# Update Teeth entry → With Teeth, narrowed to Part I
if TEETH_ID in existing_entries:
    e = existing_entries[TEETH_ID]
    e['title'] = 'With Teeth'
    e['short_synopsis'] = "A wired-jaw client pays Kyle in damp credits at half rate. He walks into the eastern strut's loading dock through the front, breaks the six men who put her husband in the corner, returns to deliver the contract closed — and walks home through rain that already feels watched."
    e['number'] = 1
    e['closes_threads'] = ["The contract on the man who broke the husband"]
    e['opens_threads'] = [
        "Mrs. Chen's half-rate transaction — the bond formed in damp credits",
        "The daughter behind the curtain — memorized, unnamed, carried",
        "Lotus surveillance — a watcher noting the work for a report that will be reviewed two months later by Mira (sets up The Interview)"
    ]
    # Note: long_synopsis can stay or be tightened later; the existing one still describes the relevant events

# Build new entries for A Restless Mind and The Rogue AI
restless_entry = {
    "chapter_id": RESTLESS_MIND_ID,
    "number": 2,
    "title": "A Restless Mind",
    "short_synopsis": restless_outline['logline'][:200] + '...',
    "long_synopsis": restless_outline['logline'],
    "key_beats": [b['title'] for act in restless_outline['acts'] for b in act['beats']],
    "opens_threads": [
        "Sable in person, after nine years of cracked-terminal contact only",
        "The hum in Kyle's chest — eleven-year companion, named for the first time as something",
        "Sable's investigation of her non-human Employer — disclosed to Kyle (and the audience)",
        "The Rogue AI's coordinates — held by Sable; payoff in The Rogue AI",
        "Pixel as Kyle's medic + the never-touches-Silence convention — established"
    ],
    "closes_threads": [
        "The cold-noodles line of Bushido Coda's procedural register — completed in this chapter's opening"
    ],
    "state_changes": {
        "Kyle Ellen Corbin-Vasik": "Aware of the hum in his chest as a constant; aware that Sable suspects (and now knows) their shared Employer is non-human; agreed to help locate the AI; bandaged at Pixel's bench.",
        "Sable": "Disclosed her investigation to Kyle; obtained the AI's coordinates; the deniability is gone.",
        "Mrs. Chen": "Has cursed at someone Kyle brought to her stall — first time in four years."
    },
    "pov_character": "Kyle Ellen Corbin-Vasik"
}

rogue_entry = {
    "chapter_id": ROGUE_AI_ID,
    "number": 3,
    "title": "The Rogue AI",
    "short_synopsis": "[PLACEHOLDER — chapter to be written. Kyle and Sable visit the Rogue AI at the coordinates resolved in A Restless Mind. Some time passes between the triangulation and the visit.]",
    "long_synopsis": "[TBD]",
    "key_beats": [],
    "opens_threads": [],
    "closes_threads": [],
    "state_changes": {},
    "pov_character": "Kyle Ellen Corbin-Vasik"
}

# Rebuild the outline chapters list in new order
new_bo_chapters = []
for i, cid in enumerate(new_order, 1):
    if cid == RESTLESS_MIND_ID:
        e = restless_entry
    elif cid == ROGUE_AI_ID:
        e = rogue_entry
    elif cid in existing_entries:
        e = existing_entries[cid]
        # Update title if Without Hands
        if cid == BORROWED_HAND_ID:
            e['title'] = 'A Borrowed Hand'
            # Update short_synopsis to use new title
            if 'short_synopsis' in e:
                e['short_synopsis'] = e['short_synopsis'].replace('Without Hands', 'A Borrowed Hand')
            if 'long_synopsis' in e:
                e['long_synopsis'] = e['long_synopsis'].replace('Without Hands', 'A Borrowed Hand')
    else:
        continue
    e['number'] = i
    new_bo_chapters.append(e)

bo['chapters'] = new_bo_chapters
bo['modified'] = now_iso
with open(bo_path, 'w', encoding='utf-8') as f:
    json.dump(bo, f, indent=2, ensure_ascii=False)
print(f'Bushido Coda outline.json: {len(new_bo_chapters)} chapter entries')

# Final report
print('\n=== BUSHIDO CODA — FINAL ORDER ===')
for i, cid in enumerate(new_order, 1):
    print(f'  {i}. {TITLES[cid]} ({cid[:16]}…)')
print()
print(f'NEW CHAPTER IDS:')
print(f'  A Restless Mind: {RESTLESS_MIND_ID}')
print(f'  The Rogue AI:    {ROGUE_AI_ID}')
print()
print(f'Without Hands renamed to A Borrowed Hand (story.json + outline.json updated)')
print(f'Existing Teeth → With Teeth (Part I only, ~14k chars; Parts II-VI archived)')
