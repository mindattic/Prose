"""Three changes:
1. Rename 'With Teeth' to 'Bearing Teeth' across all metadata + html
2. Rewrite the loading-dock fight: kneecap volley + samurai duel + piezoelectric discharge + chiburi
3. Rebuild A Restless Mind's recognition arc around 'two months' instead of 'nine years'
"""
import sys, json, datetime, re
sys.stdout.reconfigure(encoding='utf-8')

BUSHIDO = 'eb91080d9c9c4f2b9b405fa5996bdea1'
TEETH_ID = '019d6143ab61752da68e0bc71595cd6c'
RESTLESS_ID = '5a0959eb5619bf91f59ffb8632c80259'
now_iso = datetime.datetime.utcnow().isoformat() + 'Z'

# === 1. RENAME WITH TEETH -> BEARING TEETH ===
NEW_TITLE = 'Bearing Teeth'

# Update story.json
teeth_path = f'engine/data/stories/{TEETH_ID}/story.json'
with open(teeth_path, encoding='utf-8-sig') as f:
    t = json.load(f)
old_title = t['title']
t['title'] = NEW_TITLE
# Update html heading
t['html'] = t['html'].replace('# With Teeth', f'# {NEW_TITLE}', 1)
t['modified'] = now_iso
print(f'Story: {old_title} -> {NEW_TITLE}')

# === 2. REWRITE THE LOADING-DOCK FIGHT ===
html = t['html']

# Locate the existing fight section to replace
# Start: "Chorus spoke first." (just after the array activation paragraph)
# End: just before "He moved through the bodies. Methodical."
start_marker = 'Chorus spoke first.'
end_marker = 'He moved through the bodies. Methodical.'

s_idx = html.find(start_marker)
e_idx = html.find(end_marker)
if s_idx < 0 or e_idx < 0:
    print(f'WARN: fight section markers not found (start={s_idx}, end={e_idx})')
else:
    print(f'Fight section located: chars {s_idx}-{e_idx} ({e_idx - s_idx} chars)')

new_fight = """Chorus came off his right hip in his left hand. Bird's-head grip. Single-action revolver shotgun: four standard twelve-gauge rounds in the cylinder, two empty chambers reserved for whatever specialty the night might require. He thumbed the hammer back.

The first round caught the younger man in the right kneecap at three meters and the kneecap disappeared the way kneecaps disappear under a twelve-gauge slug. The man went down wet and final and the sound he made was not screaming because screaming requires a kind of composure he no longer had. Kyle did not pause to register the kill that was not a kill. The hammer was already coming back.

Second round: the older one with the badly-done jaw, the one who had broken the husband's jaw in this same room six weeks ago, the one Kyle had come for first under the contract. Right kneecap. The slug took the patella and the ligaments and the small bones around them and left the man on the concrete with his hands flat against the floor as if he could push the room back upright by leveraging it.

Third round: the man with sub-dermal plating, the seams visible at his collar like a man wearing himself wrong. Kneecap. The plating did not extend to the knee.

Fourth round: the chrome-arm man on the left, the one whose hydraulic-assist had not yet fully committed to its swing. Kneecap. The chrome-arm did not protect the leg. The leg was not the part of him that had been worth the money.

Four shots. Four men down. Three seconds.

The cylinder was empty on the standard rounds. Kyle let the hammer rest. Chorus stayed in his hand.

*This is not the discipline. This is the math the discipline depends on. The math the discipline does not survive without and will not pretend it survives without. The bushido is the duel that comes after. The kneecaps are the cost of having the duel. A man who calls these two things the same is a liar. A man who calls them opposed is also a liar. The honest man calls them the two parts of the same job and pays for both.*

The fifth man — the second chrome-arm, the one whose right side had not yet committed — had the time only to reach for the weapon at his hip, and Kyle was inside the reach by the time the wrist had cleared the holster. Silence cleared the friction sheath in the same continuous gesture, the corundum strop along Kyle's left forearm catching the mune in three full passes during the draw. The piezoelectric core woke. The hamon flickered from cold blue to a thin cyan thread. Kyle did not engage the chrome arm. He passed Silence's edge across the cybernetic interface point at the elbow — passive disruption — and the arm went dead from the shoulder down, the cascade traveling inboard through whatever spinal augmentation the man had paid for, implant by implant, a sequence of small electrical betrayals running through his nervous system like a rumor. He sat down. He did not get up.

That left the sixth man.

---

The sixth man — the one with no visible augmentation, the expensive one, the decision-maker — had not drawn his pistol. He had not run. He stood at the center of the loading dock with his right hand resting at the back of his neck the way a man rests his hand when he is reaching for something across his shoulders, and he did not move, and he did not look afraid.

Then he reached.

What came off his back was not a pistol. It was a katana. Matte-black friction sheath, blade roughly the length of Silence, a working sword carried by a man who had not bought it for show. He drew it in a single unhurried motion and brought it to the center stance — saya tucked behind the hip, blade level to the deck, the point at Kyle's throat at distance. He bowed. Exactly an inch. The bow of a man who recognized another man as an opponent and not an obstacle.

*He has been waiting for someone to come who would draw on him. He has been waiting a long time.*

Kyle holstered Chorus.

He returned the bow. Exactly an inch.

---

The duel was twenty-three seconds. He counted it later.

The boss was good. Kyle had not been expecting the boss to be good. The first exchange — boss committing to a downward cut from the right shoulder, Kyle catching it on the flat of Silence — sent a clean ringing impact through the steel and into Kyle's wrists, and the hamon brightened from cyan thread to bright cyan in the half-second the ring decayed. *The piezoelectric core wants this. The piezoelectric core has always wanted this.* The boss disengaged, reset, came again. Lateral cut to Kyle's left ribs. Kyle parried with the edge against the boss's flat — another clean ring, the hamon brightening to white-blue, the supercapacitor in the tsuka taking the impulse the way a body takes a meal it has been waiting for since before the meal arrived.

The boss did not flinch at the brightening. He had seen blades like Silence before. He had been trained against them.

That registered. Kyle adjusted.

Third exchange: Kyle's strike, downward from his own right shoulder, a setup. The boss read it correctly — caught it on his own flat, returned the ring, countered low. The counter came faster than Kyle had calibrated for. Kyle stepped back a half-step that was not quite enough. The boss's edge found Kyle's left forearm at the inner sleeve and opened the fabric and the skin beneath in a clean four-inch slice that did not bleed for the first second because the cut was that clean. Then it bled. Kyle kept the edge between them. The hamon brightened another increment — bright white-blue now, the bank visibly fed, the light it cast across the dock's wet concrete the only beautiful thing in the room.

*He is the reason the family hired this contract. He is the reason the husband sits in the corner. He is the one to finish.*

Fourth exchange. The boss feinted the lateral, committed to a thrust at Kyle's throat — and Kyle had spent three exchanges reading his timing. He stepped inside the thrust, brought Silence's edge to the boss's blade at the seppa and caught the thrust clean. The hamon, fed by every prior impact, spiked from white-blue to sodium-white to the brightness that had not happened in any of Kyle's working engagements because no engagement had ever fed the bank to the discharge threshold before.

The boss did not have time to disengage.

Kyle drove Silence's edge along the boss's blade — a twenty-centimeter slide, the steel-on-steel ring sustained, the hamon at full bank casting an interference pattern of light across both men's faces — until the edge reached the boss's own tsuka, his own grip, and when the edge made contact with the boss's wrist Kyle *released the bank.*

The discharge was the first time the audience had seen what Silence could actually do.

A column of white-blue arc fired from the blade through the contact point at the boss's wrist and up through the boss's right arm and into his temple at the speed of an electrical event in a body that had not been designed to channel an electrical event of that magnitude. The boss's eyes — the second before the arc reached them — registered something that was not quite fear; it was the recognition of a thing he had not been trained against. Then the eyes did what eyes do when the brain stem behind them stops being a brain stem. The arc exited at the back of his skull in a small bright halo. Ozone. Copper. The smell of hair burning from the inside out. He fell.

Silence cooled three increments instantly. Bank empty. Cold blue thread again.

---

Kyle lowered the blade.

He performed chiburi — the small precise flick of the wrist that cleared the blood from the steel in a single arc to the floor — and the blood landed on the concrete in a dark line. He sheathed Silence. The friction sheath caught the saya in the small clean click that meant home.

He did not breathe for five seconds. He had not been breathing during the exchanges. Now he did.

*The duel was the discipline. The kneecaps were the math. They are not the same thing. They are not opposed. They are the two parts of the same job, and a man who pretends they are opposed is a man who does not survive the work. The bushido coda is what you call a discipline that knows what it is paying for.*

Twenty-six seconds total, kneecap volley to chiburi.

Kyle stood in the aftermath and breathed. The loading dock smelled like ozone and copper and hydraulic fluid and the new specific smell of a man whose brain stem had been cooked from the inside. Chrome limbs on the concrete, the four kneecapped men screaming or no longer screaming or quiet. Blood in the drainage channels, moving in slow dark lines toward the drain.

Chorus went back into the holster on his right hip, magazine empty. The cylinder needed reloading; he would do it later. Silence was at his back, cold, the bank emptied honestly.

Kyle's hands were steady. They would shake in four minutes. He had learned to use the window.

"""

if s_idx > 0 and e_idx > 0:
    new_html = html[:s_idx] + new_fight + html[e_idx:]
    print(f'Old chapter: {len(html)} chars')
    print(f'New chapter: {len(new_html)} chars')
    print(f'Delta: {len(new_html) - len(html):+d}')
    t['html'] = new_html

with open(teeth_path, 'w', encoding='utf-8') as f:
    json.dump(t, f, indent=2, ensure_ascii=False)
print('Bearing Teeth: fight scene rewritten')

# Update outline.json title
to_path = f'engine/data/stories/{TEETH_ID}/outline.json'
if __import__('os').path.exists(to_path):
    with open(to_path, encoding='utf-8-sig') as f:
        to = json.load(f)
    to['title'] = NEW_TITLE
    with open(to_path, 'w', encoding='utf-8') as f:
        json.dump(to, f, indent=2, ensure_ascii=False)

# Update Bushido Coda book outline chapter entry
bo_path = f'engine/data/books/{BUSHIDO}.outline.json'
with open(bo_path, encoding='utf-8-sig') as f:
    bo = json.load(f)
for c in bo['chapters']:
    if c['chapter_id'] == TEETH_ID:
        c['title'] = NEW_TITLE
        if 'short_synopsis' in c:
            c['short_synopsis'] = c['short_synopsis'].replace('With Teeth', NEW_TITLE)
bo['modified'] = now_iso
with open(bo_path, 'w', encoding='utf-8') as f:
    json.dump(bo, f, indent=2, ensure_ascii=False)

# === 3. REBUILD A RESTLESS MIND'S RECOGNITION ARC ===
restless_path = f'engine/data/stories/{RESTLESS_ID}/story.json'
with open(restless_path, encoding='utf-8-sig') as f:
    r = json.load(f)
r_html = r['html']

# Replace 'nine years' references with 'two months'
replacements = [
    ('every two weeks for nine years', 'every Tuesday and Friday for two months'),
    ('nine years of cracked-terminal audio', 'two months of cracked-terminal audio'),
    ('twelve operational briefings in the last two years alone', 'eleven operational briefings in the two months since she picked him up'),
]
for old, new in replacements:
    if old in r_html:
        r_html = r_html.replace(old, new)
        print(f'  Replaced: "{old[:50]}..." -> "{new[:50]}..."')

# Insert a paragraph clarifying Kyle's recognition timing — he sees her FIRST at the stall (face/beauty), hears her voice when she SPEAKS, matches them, expects ambush
# Find the moment she steps under the awning. Before she tilts her head come.
awning_marker = "The woman stepped under the awning."
recognition_insert = """The woman stepped under the awning. The hood came back two centimeters — not all the way, just enough that the streetlight caught the shape of a face and the dark of red hair beneath the rain hood, and Kyle catalogued *attractive* the way the array catalogues anything: as a fact registered without commentary. Tall. Late thirties. Bone structure that had cost her nothing because she had been born with it. He had not seen her face before tonight and he was not entirely sure he was supposed to be seeing it now.

Then she spoke.

Not loud. Not for Mrs. Chen. For him, across two meters of rain. *"Kyle."* Just the name. Level, unhurried, the tongue she had chosen to say it in the same tongue she used through the cracked terminal at the parts shop three blocks east — the modulated register, the older one, the voice he had been getting contracts in for two months and had not yet built a face for.

The match landed in his head before he had time to refuse it. Voice plus face. *Sable.*

*She has never come in person before. The relationship is two months old. A fixer he barely knows showing up at his stall in person, in the rain, hood up, calling his name across the walkway, is the shape of an ambush.*

He set the chopsticks down across the bowl. He did not stand. He did not move his hands away from the counter. The right hand was already where Chorus would be in eight-tenths of a second; the left hand was already where Silence's draw would begin in three-tenths. He let her see him not move and let the not-moving be the answer to the question she had not asked yet."""

if awning_marker in r_html:
    # Find the next paragraph after the awning marker (the "She did not order. She did not sit. She tilted her head" passage)
    after_idx = r_html.find(awning_marker)
    # Find the end of the existing awning paragraph (look for next "She tilted her head")
    tilted_idx = r_html.find('She tilted her head', after_idx)
    if tilted_idx > 0:
        # Replace the existing awning-to-tilted block with the new recognition insert + the tilted-head moment
        # Find the paragraph end after "tilted her head" — the line ends with "into it."
        end_phrase = 'leaves the bowl and follows her into it.'
        end_idx = r_html.find(end_phrase, tilted_idx)
        if end_idx > 0:
            end_idx = end_idx + len(end_phrase)
        else:
            end_idx = r_html.find('\n\n', tilted_idx)

        # Build new section: replace the awning-to-end-of-paragraph block
        old_block = r_html[after_idx:end_idx]
        new_block = recognition_insert + """

She tilted her head — the smallest possible geometry of *come* — and Kyle, who now knew exactly who she was and was therefore in a worse strategic position than if he had not known, did the calculation a freelancer does when his fixer of two months shows up at his stall in person and says only his name. *If she wanted me dead, the angle from the doorway would already be open. If she wanted to ambush me, she would not have spoken. She is here for something the cracked terminal cannot carry. She is here because the room she lives in is not safe.* He left the bowl and followed her into the rain, and his right hand stayed where Chorus would be, and his left hand stayed where Silence's draw would begin, and he did not stop tracking her three-quarter profile for the entire walk."""
        r_html = r_html.replace(old_block, new_block, 1)
        print('  Recognition arc rebuilt: face first, voice second, recognition + ambush expectation')

r['html'] = r_html
r['modified'] = now_iso
with open(restless_path, 'w', encoding='utf-8') as f:
    json.dump(r, f, indent=2, ensure_ascii=False)
print('A Restless Mind: recognition arc rebuilt around two-month relationship')

# === Verifications ===
print()
print('=== VERIFICATION ===')
with open(teeth_path, encoding='utf-8-sig') as f:
    t2 = json.load(f)
bt_html = t2['html']
checks_bt = [
    ('Title: Bearing Teeth', t2['title'] == NEW_TITLE),
    ('Heading uses Bearing Teeth', '# Bearing Teeth' in bt_html),
    ('Four kneecap shots', 'Four shots. Four men down' in bt_html),
    ('Hypocrisy thesis', 'A man who calls these two things the same is a liar' in bt_html),
    ('Boss draws his katana', 'What came off his back was not a pistol. It was a katana' in bt_html),
    ('Formal bow exchange', 'Returned the bow. Exactly an inch.' in bt_html or 'returned the bow' in bt_html.lower()),
    ('Piezoelectric brightening', 'piezoelectric core wants this' in bt_html),
    ('Discharge into temple', 'A column of white-blue arc' in bt_html),
    ('Chiburi performed', 'performed chiburi' in bt_html),
    ('Bushido coda thesis', 'The bushido coda is what you call a discipline' in bt_html),
]
for label, ok in checks_bt:
    print(f'  [{"+" if ok else "-"}] Bearing Teeth: {label}')

with open(restless_path, encoding='utf-8-sig') as f:
    r2 = json.load(f)
rm_html = r2['html']
checks_rm = [
    ('Two months relationship', 'two months' in rm_html),
    ('No more "nine years" references', 'nine years' not in rm_html),
    ('Beauty registered first', 'attractive' in rm_html),
    ('Voice -> recognition match', 'voice plus face' in rm_html.lower() or 'Voice plus face' in rm_html),
    ('Sable speaks his name', '*"Kyle."*' in rm_html or '"Kyle."' in rm_html),
    ('Ambush expectation', 'shape of an ambush' in rm_html),
    ('Hands stay near weapons', 'where Chorus would be' in rm_html),
]
for label, ok in checks_rm:
    print(f'  [{"+" if ok else "-"}] A Restless Mind: {label}')
