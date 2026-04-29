"""Apply: timer sweep with correct anchors, crocodile-feeding lines, Hua-sheathes-and-hands-over, pain-returns intensification."""
import sys, json, re, datetime
sys.stdout.reconfigure(encoding='utf-8')

story_path = 'engine/data/stories/019dd24feb047e9fb9c901450389a8b9/story.json'

with open(story_path, encoding='utf-8') as f:
    s = json.load(f)
html = s['html']

# ---------- 1. TIMER SWEEP (14h -> 3h) — using actual phrases ----------
timer_replacements = [
    # Specific phrases from the prose
    ('fourteen-hour nerve cluster preservation window', 'three-hour nerve cluster preservation window'),
    ('fourteen-hour nerve preservation', 'three-hour nerve preservation'),
    ('*That gives me fourteen hours.*', '*That gives me three hours.*'),
    ('That gives me fourteen hours.', 'That gives me three hours.'),
    ('The fourteen hours have already started.', 'The three hours have already started.'),
    ('you will feel this for fourteen hours and then you will have to live with what it cost you.', 'you will feel this for three hours and then you will have to live with what it cost you.'),
    ('He has eleven hours and fifty-three minutes left.', 'He has one hour and fifty-three minutes left.'),
    ('eleven hours and forty minutes left', 'eighty-eight minutes left'),
    ('the math has fourteen hours left', 'the math has under three hours left'),
    ('inside the cauterizer band\'s fourteen-hour window', 'inside the cauterizer band\'s three-hour window'),
    ('inside the band\'s fourteen-hour window', 'inside the band\'s three-hour window'),
    ('fourteen-hour window', 'three-hour window'),
    ('fourteen hours of reattachment window', 'three hours of reattachment window'),
]
total_repls = 0
for old, new in timer_replacements:
    n = html.count(old)
    if n:
        html = html.replace(old, new)
        total_repls += n
        print(f'  Replaced {n}x: ...{old[:50]}...')

# Verify clean
remaining = re.findall(r'fourteen[\s-]hour|fourteen hours', html, flags=re.IGNORECASE)
if remaining:
    print(f'  WARN: {len(remaining)} stray references remain')
    for m in re.finditer(r'fourteen[\s-]hour|fourteen hours', html, re.IGNORECASE):
        print(f'    @ {m.start()}: {html[max(0,m.start()-40):m.start()+80]!r}')
else:
    print(f'  All timer references retimed ({total_repls} replacements)')

# ---------- 2. CROCODILE FEEDING IMPLICATION (beat 1) ----------
barrel_anchor = 'She places them palms up at the rim, parallel, the stump-ends pointing inward, and the arrangement is deliberate — a bracket shape, a parenthetical, the way a ledger closes a line.'
if barrel_anchor in html:
    new_block = barrel_anchor + (
        ' She does not put the lid back on the barrel. It stays open the way a feeding bowl is open, '
        'and the crocodile, which has not moved once since Kyle arrived, lifts its head from the heat rock and looks. Not at Kyle. At the barrel. The second eyelid retracts. The interior of the eye behind it is the color of nothing in particular. Hua does not look at the crocodile. She does not have to. The animal eats on a schedule it has been kept on long enough that schedules have a smell, and the smell is in the room now, and Kyle catalogs it the way he catalogs everything: *the hands have a feeding window. The feeding window is shorter than the cauterizer\'s window. He has two clocks now and they are not independent.*'
    )
    html = html.replace(barrel_anchor, new_block, 1)
    print('  Added crocodile-feeding implication at barrel placement')
else:
    print('  WARN: barrel anchor not found')

# ---------- 3. HUA SHEATHES SILENCE AND HANDS IT TO THE ARM (beat 8) ----------
# Anchor begins: "and he brings the cargo arm up and lays the borrowed fingers across the saya"
# Need to replace through "the geometry the room has been waiting..."
# Find the full original passage to replace
old_silence_pickup = re.search(
    r'(and he brings the cargo arm up and lays the borrowed fingers across the saya, palm down, the way a man places his hand on a document to stop it from being filed\. Hua looks at his hand\. Then she looks at her own\. Then she puts her real hand on the saya above his — one second, flat and deliberate, the principle of it — and the geometry the room has been waiting [^.]*\..*?the bank is empty and the weight is the weight he has carried since he was nineteen years old and the weight is correct\.)',
    html, flags=re.DOTALL
)
if old_silence_pickup:
    new_silence_handover = (
        "and he brings the cargo arm up and lays the borrowed fingers flat on the lacquered table, palm down, two centimeters from the saya. He does not touch the blade. He does not pick it up. He looks at her.\n\n"
        "*Sheathe it.*\n\n"
        "His own voice, level, no E.L.F. cadence behind it — the discipline operating the mouth alone. Hua does not move for the length of three breaths. Her real hands are on the table edge and they have started to do something they have not done all night, which is shake. The cargo arm does not move either. The E.L.F. is patient in the firmware and Kyle is patient in the body and patience is the variable they are working with now. Hua looks at the blade. The friction sheath is unbuckled and laid alongside Silence the way she set it three hours ago — *as her property, in the way she had arranged it.* She has not touched the saya since the moment she placed her real hand on it during the amputation, when she made a small possessive gesture in front of a witness. She picks the saya up now. Her hands shake at the buckle. She fits the saya over Silence and slides the blade home and the click of the friction-sheath catching is small and clean and final.\n\n"
        "She sets the sheathed sword on the table.\n\n"
        "*Hand it to me.*\n\n"
        "Her mouth opens. Closes. She does not argue. Arguing has left the room. She picks up the sheathed sword in both hands — the way you carry a thing that does not belong to you back to the person it does — and walks the two steps around the corner of the table to where the cargo arm is waiting. She places Silence across the cargo arm's open chrome palm. The arm closes around the saya. The transfer is complete. She steps back. Her hands are at her sides now. They are still shaking and she is no longer pretending they are not.\n\n"
        "The geometry the room has been waiting for has resolved, except not the way the room expected it to resolve. The natural-handed woman has, with her own real hands, sheathed the blade she claimed and surrendered it across her own table to a borrowed industrial limb. Kyle did not pick the sword up. He let her do that work. He has decided that the work was hers to do and that her hands were going to shake doing it, and they did, and the room has watched, and that is enough.\n\n"
        "The cargo arm holds Silence at low carry. The hamon is cold blue under the saya, the bank is empty and the weight is the weight he has carried since he was nineteen years old and the weight is correct."
    )
    html = re.sub(re.escape(old_silence_pickup.group(1)), new_silence_handover, html, count=1)
    print('  Rewrote Silence-handover: Hua sheathes and presents the blade to the cargo arm')
else:
    print('  WARN: Silence-pickup pattern not found, attempting fallback')
    # Fallback: just inject the sheath/hand command before the existing pickup
    fallback_anchor = 'and he brings the cargo arm up and lays the borrowed fingers across the saya, palm down,'
    if fallback_anchor in html:
        injection = (
            "He looks at her. *Sheathe it,* he says, his own voice. Hua's hands shake on the buckle as she fits the friction sheath over Silence; the click of the catch is small and final. *Hand it to me,* he says, and she picks the sheathed sword up in both hands and walks it around the table and places Silence across the cargo arm's open chrome palm, and the arm closes around the saya, and the transfer is complete. Then "
        )
        html = html.replace(fallback_anchor, injection + fallback_anchor, 1)
        print('  Used fallback: prepended sheathe-and-hand-over before existing pickup')

# ---------- 4. PAIN RETURNS DURING ARM ATTACHMENT ----------
# Find the integration / needle-seating section in beat 4 and intensify pain
pain_anchor = 'antibiotics start being a thing the package will pay to push'
if pain_anchor in html:
    pain_extension = (
        '. ' +
        'And from that point — minute twenty-three of the integration, with the needles seated and the cable beginning its first loop around his forearm — the pain comes back. '
        'Not all at once. The hardware does not concede the channel cleanly. It cedes in stages, the way a dam fails: first a leak around the outer edge of the seal where the band\'s residual cauterization is no longer holding, then the seam at the wrist crease where the cleaver came down, then the deeper signal from the nerve clusters themselves where the maintenance needles have driven past their design depth into living tissue. '
        'Kyle has spent the last two hours in the dispassionate register of a man performing field surgery on himself. The register stops being available. '
        'The pain is bright. It is specific. It is the pain of an industrial procedure on a body that was not the design target, conducted without analgesic, by an entity that is learning his nervous system in real time and is making the kind of mistakes that come with first-time work. He feels each finger of the cargo arm find the cable and tighten the loop. He feels each tightening as a discrete pulse in the seated needles, the cable pressure transmitting through the bus crosswalk into the nerve clusters, every loop costing him a measure of composure he was not budgeting to spend. '
        'He breathes. He keeps breathing. The chrome bracket in his jaw flares again. His eyes water — actual tears, not protocol-driven, the body asserting that this is, in fact, a thing happening to him. '
        '*Three loops,* his mouth says, the E.L.F. announcing the progress because Kyle has stopped being able to look. *Two loops.* *One loop.* Each announcement is a finish line he is racing to survive. The last loop knots through the hollowed shoulder coupling and the cauterizer band closes back over the seated needles and the pain does not stop. The pain stays. The body has been distant from the pain for as long as the body could be distant; the distance is gone'
    )
    html = html.replace(pain_anchor, pain_anchor + pain_extension, 1)
    print('  Intensified pain-return during arm attachment')
else:
    print('  WARN: pain anchor not found')

# ---------- 5. SAVE PROSE & SYNC ----------
s['html'] = html
s['modified'] = datetime.datetime.utcnow().isoformat() + 'Z'
with open(story_path, 'w', encoding='utf-8') as f:
    json.dump(s, f, indent=2, ensure_ascii=False)

cp_path = 'engine/data/stories/019dd24feb047e9fb9c901450389a8b9/checkpoint.json'
with open(cp_path, encoding='utf-8') as f:
    cp = json.load(f)
cp['FullText'] = html
with open(cp_path, 'w', encoding='utf-8') as f:
    json.dump(cp, f, indent=2, ensure_ascii=False)

# ---------- 6. VERIFY ----------
print(f'\nFinal chapter: {len(html)} chars (~{len(html)//5} words)')
final_checks = [
    ('three-hour preservation', 'three-hour'),
    ('NO fourteen-hour stragglers', 'fourteen-hour'),  # absent expected
    ('Crocodile lifts head', 'lifts its head from the heat rock'),
    ('Two clocks for Kyle', 'two clocks now'),
    ('"Sheathe it"', 'Sheathe it'),
    ('"Hand it to me"', 'Hand it to me'),
    ('Hua walks the sword', 'walks the two steps around the corner'),
    ('Pain comes back staged', 'pain comes back'),
    ('"Three loops" / "Two loops" / "One loop"', 'Three loops'),
    ('Vásquez settlement', 'Vásquez'),
    ('Patience is a virtue', 'Patience is a virtue'),
    ('Puppeteer named', 'Puppeteer'),
    ('SAY IT scream', 'SAY IT'),
    ('Chinese curse 操', '操'),
]
for label, needle in final_checks:
    present = needle in html
    is_absent_check = 'NO ' in label
    ok = (not present) if is_absent_check else present
    print(f'  [{"+" if ok else "-"}] {label}: {"FOUND" if present else "absent"}')
