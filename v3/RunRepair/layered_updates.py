"""Layer in: timer 14h to 3h, crocodile feeding implication, Hua sheathes Silence and hands it over.
Then update outline beats and sync checkpoint."""
import sys, json, re, datetime
sys.stdout.reconfigure(encoding='utf-8')

story_path = 'engine/data/stories/019dd24feb047e9fb9c901450389a8b9/story.json'
outline_path = 'engine/data/stories/019dd24feb047e9fb9c901450389a8b9/outline.json'
checkpoint_path = 'engine/data/stories/019dd24feb047e9fb9c901450389a8b9/checkpoint.json'

# ---------- 1. CHAPTER PROSE: timer sweep (14 hours -> 3 hours) ----------
with open(story_path, encoding='utf-8') as f:
    s = json.load(f)
html = s['html']

prose_replacements = [
    # Cauterizer preservation window mentions
    ('fourteen-hour preservation window', 'three-hour preservation window'),
    ('fourteen hour preservation window', 'three-hour preservation window'),
    ('fourteen-hour window', 'three-hour window'),
    ('fourteen hours of reattachment window', 'three hours of reattachment window'),
    ('that gives me fourteen hours', 'that gives me three hours'),
    ('the math has fourteen hours left', 'the math has under three hours left'),
    ('eleven hours and forty minutes left', 'eighty-eight minutes left'),
    ('eleven hours and forty minutes', 'eighty-eight minutes'),
    # Catch any general "fourteen hours" still relating to the window (but be careful: there are non-window uses?)
    # Let's be specific — only the kinds of phrases that occur in the prose.
    ('AutoDoc inside the cauterizer band\'s fourteen-hour window', 'AutoDoc inside the cauterizer band\'s three-hour window'),
    ('inside the band\'s fourteen-hour window', 'inside the band\'s three-hour window'),
]

repl_count = 0
for old, new in prose_replacements:
    if old in html:
        n = html.count(old)
        html = html.replace(old, new)
        repl_count += n
        print(f'  Replaced {n}x: "{old[:60]}..." -> "{new[:60]}..."')

# Verify no stray "fourteen-hour" survived
remaining = re.findall(r'fourteen[\s-]hour', html, flags=re.IGNORECASE)
if remaining:
    print(f'  WARN: {len(remaining)} stray fourteen-hour references remain')
else:
    print('  All cauterizer-window references retimed.')

# ---------- 2. CHAPTER PROSE: crocodile feeding implication ----------
# Insert at beat 0 / beat 2 transitions where chum barrel is named. Find the chum-barrel placement passage in beat 1.
# Anchor: "She places them palms up..." (or similar)
# Look for the moment Hua places hands in the barrel and add a sentence.
anchor1 = 'palms up, exactly parallel'
if anchor1 in html:
    insertion = (
        ' She does not put the lid back on. The barrel is open the way a feeding bowl is open, '
        'and the crocodile, which has been still for the entire amputation, lifts its head from the heat rock for the first time tonight and looks. Not at Kyle. At the barrel.'
    )
    html = html.replace(anchor1, anchor1 + insertion, 1)
    print(f'  Added crocodile-feeding implication after barrel placement (beat 1)')
else:
    # try alternate
    alt = 'cauterized stump-ends pointing inward toward each other so they make a small bracketed shape against the rim of the barrel'
    if alt in html:
        insertion = (
            ' The lid stays off. The barrel is open the way a feeding bowl is open, '
            'and the crocodile, which has been still for the entire amputation, lifts its head from the heat rock for the first time tonight and looks. Not at Kyle. At the barrel.'
        )
        html = html.replace(alt, alt + '.' + insertion, 1)
        print(f'  Added crocodile-feeding implication (alternate anchor)')
    else:
        print(f'  WARN: chum-barrel placement anchor not found; manual edit may be needed')

# Insert into Kyle's beat-2 psychology — adding the feeding-schedule pressure to the bookkeeping
anchor2 = 'a future in which the natural hand is repairable'
if anchor2 in html:
    insertion2 = (
        '\n\nThere is also the crocodile. He saw it raise its head when Hua set the hands. He filed that. The barrel is open and the crocodile is fed personally — Hua said as much in the way she walked back from the tank, the way she did not give the feeding to one of her men — and that means there is a *schedule*. Crocodiles eat. The schedule has a start time. Kyle has not stolen the contract from the Lotus Syndicate but he has now stolen, by the sheer mechanical fact of leaving, his own hands from the syndicate\'s pet, and that is its own clock running parallel to the cauterizer\'s clock. The two clocks are not independent. He has perhaps three hours on the band before the nerve clusters degrade past organic reattachment. He has less than that on the crocodile, because Hua is unhappy and unhappy people feed pets to feel better.'
    )
    html = html.replace(anchor2, anchor2 + insertion2, 1)
    print(f'  Added crocodile-feeding clock to Kyle\'s beat-2 bookkeeping')
else:
    print(f'  WARN: beat-2 psychology anchor not found')

# ---------- 3. CHAPTER PROSE: Hua sheathes Silence and hands it over ----------
# Find the saya-meeting passage in beat 8 and rewrite it.
old_saya = (
    'He does not raise the blade. The cargo arm holds Silence at low carry, formal, present, the threat already receipted by every man on the floor between here and the service hatch.'
)
# Find the wider passage to replace (Kyle picking up Silence himself)
# The saya-meeting begins around "He brings the cargo arm up and lays the borrowed fingers across the saya"
# and continues through Kyle picking up Silence. We need to invert this: Hua does the work.
old_passage_start = 'a deed already filed'  # too early — that's beat 0
# Use a more specific anchor — the actual saya-meeting moment in beat 8
# Find in chapter
saya_anchor = 'cargo arm holds Silence at low carry'
idx = html.find(saya_anchor)
print(f'  Saya-handover anchor at index {idx}')

# The cleanest approach: locate the existing passage where Kyle "picks Silence up" and replace it with Hua sheathing and handing it over.
# Pattern: "He picks Silence up..." or similar.
old_pickup_pattern = re.search(
    r'(He brings the cargo arm up and lays the borrowed fingers across the saya[^.]*\..*?The cargo arm holds Silence at low carry[^.]*\.)',
    html, flags=re.DOTALL
)
if old_pickup_pattern:
    old_block = old_pickup_pattern.group(1)
    new_block = (
        "He brings the cargo arm up and lays the borrowed fingers flat on the lacquered table, palm down, two centimeters from the saya. He does not touch the blade. He does not pick it up. He looks at her.\n\n"
        "\"Sheathe it.\"\n\n"
        "She does not move for the length of three breaths. Her real hands are still on the table edge. The cargo arm does not move either; the E.L.F. is patient in the firmware, and Kyle is patient in the body, and patience is the variable they are working with now. Hua looks at the blade. The friction sheath is unbuckled and laid alongside it the way she set it three hours ago. She picks up the saya. Her real hands shake, just once, at the buckle — the same tremor that came when she stuttered the eighty-five thousand — and she fits the saya over Silence and slides the blade home and the click of the friction sheath catching is small and clean and final.\n\n"
        "She sets it on the table.\n\n"
        "\"Hand it to me.\"\n\n"
        "Her mouth opens. Closes. She does not argue. Arguing has left the room. She picks up the sheathed sword in both hands — the way you carry a thing that does not belong to you back to the person it does — and she walks the two steps around the corner of the table to where the cargo arm is waiting. She places Silence across the cargo arm's open chrome palm. The arm closes around the saya. The transfer is complete. She steps back. Her hands are at her sides now. They are still shaking and she is no longer pretending they are not.\n\n"
        "The cargo arm holds Silence at low carry, formal, present, the threat already receipted by every man on the floor between here and the service hatch."
    )
    html = html.replace(old_block, new_block, 1)
    print(f'  Rewrote Silence-handover: Hua sheathes and presents the blade')
else:
    print('  WARN: saya-meeting passage not found in expected pattern; trying alternate')
    # Alternate: simpler swap — find "he picks the sword up" and prepend instructions
    alt_pattern = 'He picks Silence up with the cargo arm'
    if alt_pattern in html:
        replacement = 'He looks at Hua. He says, level: *Sheathe it.* She fits the saya over Silence with hands that are visibly shaking, slides the blade home, the friction-sheath click small and final. He says: *Hand it to me.* She picks up the sheathed sword in both hands and walks it around the table to the cargo arm. She places it across the chrome palm. The arm closes around the saya. He picks Silence up with the cargo arm'
        html = html.replace(alt_pattern, replacement, 1)
        print(f'  Used alternate pattern: prepended sheath/handover before pickup')

# ---------- 4. SAVE PROSE ----------
s['html'] = html
s['modified'] = datetime.datetime.utcnow().isoformat() + 'Z'
with open(story_path, 'w', encoding='utf-8') as f:
    json.dump(s, f, indent=2, ensure_ascii=False)
print(f'\nstory.json: total length now {len(html)} chars (~{len(html)//5} words)')

# ---------- 5. UPDATE OUTLINE BEATS ----------
with open(outline_path, encoding='utf-8') as f:
    o = json.load(f)

# Beat 1 — retime, add crocodile feeding implication
b1 = o['acts'][0]['beats'][1]
b1['goal'] = b1['goal'].replace('fourteen-hour', 'three-hour').replace('fourteen hours', 'three hours')
# Add crocodile-as-feeder line if not present
if 'crocodile, which has been still' not in b1['goal']:
    b1['goal'] = b1['goal'].replace(
        'palms up, exactly parallel',
        'palms up, exactly parallel — and she leaves the lid off the chum barrel, the way you leave a feeding bowl off, and the crocodile lifts its head from the heat rock for the first time tonight and looks at the barrel'
    )

# Beat 2 — retime, add crocodile-clock pressure
b2 = o['acts'][0]['beats'][2]
b2['goal'] = b2['goal'].replace('fourteen-hour', 'three-hour').replace('fourteen hours', 'three hours').replace(
    'fourteen-hour clock', 'three-hour clock'
)
if 'crocodile' not in b2['goal']:
    b2['goal'] = b2['goal'].rstrip() + ' Bookkeeping also includes a SECOND clock: the crocodile in the chamber upstairs is fed personally by Hua, the chum barrel was left open beside the tank, and Hua is unhappy. Unhappy people feed pets to feel better. The crocodile clock runs parallel to the cauterizer clock and is not independent of it. Kyle has perhaps three hours on the band; he has less than that on the crocodile.'

# Beat 5 — retime math reference
b5 = o['acts'][1]['beats'][2]
b5['goal'] = b5['goal'].replace(
    'Fourteen-hour window has eleven hours forty minutes left',
    'Three-hour window has eighty-eight minutes left'
).replace('fourteen-hour', 'three-hour').replace('fourteen hours', 'three hours').replace(
    'eleven hours and forty minutes', 'eighty-eight minutes'
).replace('eleven hours forty minutes', 'eighty-eight minutes')

# Beat 8 — add the sheath/handover sequence to the goal
b8 = o['acts'][2]['beats'][1]
if 'Sheathe it' not in b8['goal']:
    # Insert the sheathe/handover BEFORE the existing pickup-and-debt sequence
    new_handover = (
        " THE SAYA-MEETING IS REPLACED BY A SHEATH-AND-PRESENT SEQUENCE: Kyle does NOT pick up Silence himself. He brings the cargo arm to the table, lays the borrowed fingers flat two centimeters from the saya, and tells Hua: 'Sheathe it.' She picks up the friction sheath, her real hands shaking, fits it over Silence, slides the blade home — the click final. He then says: 'Hand it to me.' She walks around the table, picks up the sheathed sword in both hands the way you carry a thing back to its owner, and places Silence across the cargo arm's open chrome palm. The arm closes. The transfer is complete. She steps back. Her hands are at her sides, still shaking, no longer pretending they are not. ONLY THEN does the debt question begin (what was the debt, eighty-five thousand, exoneration, etc.).  "
    )
    # Insert near the top of beat 8 goal
    b8['goal'] = b8['goal'].replace(
        'He brings the cargo arm up and lays the borrowed fingers across the saya',
        new_handover + 'After the handover, Kyle then performs the original gesture (now reframed): he lays the cargo-arm fingers across the saya'
    )

# Beat 9 — retime AutoDoc arrival
b9 = o['acts'][2]['beats'][2]
b9['goal'] = b9['goal'].replace(
    'inside the cauterizer band\'s preservation window',
    'inside the cauterizer band\'s three-hour preservation window — but barely; the chapter\'s timer is now visibly close to the end'
).replace('fourteen-hour', 'three-hour')

# Add new seeds_and_payoffs
new_pairs = [
    {
        "seed": "The cauterizer band's preservation window — three hours of viable nerve-cluster reattachment, not fourteen",
        "planted_in_beat": 1,
        "payoff": "Kyle's bookkeeping in every subsequent beat runs against this much tighter clock; the chapter's tension is metabolic urgency, not procedural patience",
        "payoff_in_beat": 9
    },
    {
        "seed": "The chum barrel left open beside the crocodile tank, the crocodile lifting its head when Hua sets the hands",
        "planted_in_beat": 1,
        "payoff": "Establishes the parallel feeding clock; Kyle has to retrieve his hands before Hua feeds them to the pet she dotes on personally",
        "payoff_in_beat": 8
    },
    {
        "seed": "Hua's command to 'Sheathe it' and 'Hand it to me' — Kyle refusing to pick up Silence himself",
        "planted_in_beat": 8,
        "payoff": "The reversal is structural: Hua becomes the courier of the trophy back to its owner, with hands she values visibly shaking through the entire transfer",
        "payoff_in_beat": 8
    }
]
existing_seeds = {p['seed'] for p in o['seeds_and_payoffs']}
for p in new_pairs:
    if p['seed'] not in existing_seeds:
        o['seeds_and_payoffs'].append(p)

with open(outline_path, 'w', encoding='utf-8') as f:
    json.dump(o, f, indent=2, ensure_ascii=False)
print(f'outline.json: beats updated, seeds_and_payoffs = {len(o["seeds_and_payoffs"])}')

# ---------- 6. SYNC CHECKPOINT ----------
with open(checkpoint_path, encoding='utf-8') as f:
    cp = json.load(f)
cp['Outline'] = o
cp['FullText'] = html
with open(checkpoint_path, 'w', encoding='utf-8') as f:
    json.dump(cp, f, indent=2, ensure_ascii=False)
print('checkpoint.json synced')

# ---------- 7. VERIFICATION ----------
print('\nVerification:')
checks = [
    ('Three-hour window present', 'three-hour'),
    ('No more 14-hour references', 'fourteen-hour'),  # this one should be ABSENT
    ('"eighty-eight minutes" replacement', 'eighty-eight minutes'),
    ('Crocodile feeding implication', 'lifts its head from the heat rock'),
    ('Crocodile clock in Kyle psychology', 'unhappy people feed pets'),
    ('"Sheathe it" command', 'Sheathe it'),
    ('"Hand it to me" command', 'Hand it to me'),
    ('Hua walks the sword', 'walks the two steps around the corner'),
    ('Vásquez settlement still present', 'Vásquez'),
    ('Patience is a virtue still present', 'Patience is a virtue'),
]
for label, needle in checks:
    present = needle.lower() in html.lower()
    is_should_be_absent = ('No more' in label)
    if is_should_be_absent:
        ok = not present
    else:
        ok = present
    print(f'  [{"+" if ok else "-"}] {label}: {"FOUND" if present else "absent"}')
