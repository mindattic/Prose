"""Rewrite the mutant beat: from monster to Lenny — pitiful, twisted-human, apologetic."""
import sys, json, re, datetime
sys.stdout.reconfigure(encoding='utf-8')

story_path = 'engine/data/stories/019dd24feb047e9fb9c901450389a8b9/story.json'

with open(story_path, encoding='utf-8') as f:
    s = json.load(f)
html = s['html']

# ---------- 1. EARLIER WORLDBUILDING IN BEAT 2/3 ----------
# Find the trench scan where Kyle first sees thermal signatures, add the mutant cosmology
ref_anchor = 'far south, past a bend he cannot see around, something warm. Multiple signatures.'
if ref_anchor in html:
    canon_paragraph = (
        ' He files what they are. The Cruciform Depot was mothballed eighteen years ago by Axiom, but the lower levels — the sealed maintenance corridors, the freight runoff trenches, the abandoned crew warrens — '
        'have been continuously inhabited for more than two centuries. They were where the displaced went when the surface stopped accepting them. Three generations down — perhaps four, by the bottom-tier estimates Kyle has read — and the population that stayed has not stayed *human* in the surface sense. '
        'They drink water that has been filtering through Axiom-era industrial waste sinks for a hundred and seventy years; they eat the rats, and the rats eat the residue, and the rats glow. Bioaccumulation across that many trophic levels does not produce health. '
        'Bone plates grow where soft tissue has run out of options. Skin calcifies. Limbs lengthen because the body, given a century of bad calcium and the wrong proteins, will reach for what it can. The faces compress. The jaw plates fuse into something that is no longer a jaw but is still, recognizably, where a jaw was. '
        'They are not animals. They are people who have spent two hundred years drinking what corponations dumped and eating what survived in it. They have language — limited, child-simple, the vocabulary of a body that has had to choose between forming words and surviving — and they have *families*. They live in family groups. They hunt heat signatures because heat signatures are food. They are the ones the surface does not have a word for, because the word that fits would be *cousin*, and nobody on the surface wants the word to fit. '
        'Kyle has read the bottom-tier dispatches. He has files them. The thermal signatures past the bend are not monsters. They are children of the tunnels.'
    )
    html = html.replace(ref_anchor, ref_anchor + canon_paragraph, 1)
    print('  Added mutant-cosmology worldbuilding paragraph (beat 3)')
else:
    print('  WARN: thermal-signature anchor not found')

# ---------- 2. REWRITE THE MUTANT ARRIVAL & FIGHT ----------
# Find the existing mutant-arrival passage and replace it with a Lenny-tragic version
# Boundaries: from "The sound comes from the tunnel mouth..." through "...the mutant's hind legs go first"
# That includes the auditory build, Kyle's thermal scan, the lunge, the chest hit, the throat strike, the glass shard, the kill.

old_passage_match = re.search(
    r'(Then he hears it\.\s+The sound comes from the tunnel mouth at the far end of the shelf,.*?The smell is copper and rot and something pharmaceutical, whatever the Depot\'s stock was eating before it ate nothing\.)',
    html, flags=re.DOTALL
)

if old_passage_match:
    old_passage = old_passage_match.group(1)
    new_passage = (
        "Then he hears it.\n\n"
        "The sound comes from the tunnel mouth at the far end of the shelf, ten meters into the dark, and it arrives in three layers, but the layers are not the layers Kyle expected. First, beneath the wet articulated step, there is a *cadence* — the alternating heel-then-hand of a quadrupedal gait that is not native to anything four-legged. Something that used to walk upright has learned to use its long arms as front legs because the back has bent forward and the spine has not been negotiating well with the ceiling for a very long time. Second, beneath the cadence, there is a breath. Not the ventilatory rasp of a hunting machine — something wetter and more organic, a wheeze that has a child's pitch under it, the breath of a body whose lungs have been doing what lungs do under three centuries of contaminated air. Third, beneath the breath, the soft *clack* of bone-plates settling against each other as the lower face moves: the plates that used to be a jaw, that have fused over the jaw because the jaw stopped being able to do the work alone, and that move now in the rhythm of speech without producing speech.\n\n"
        "The NeoCortex pulls a thermal trace from his auditory data alone — the room is dark, the visual is useless, but the sound carries the information his cognition needs — and the trace resolves: bipedal substrate, currently quadrupedal, mass approximately one hundred and forty kilograms, *Homo sapiens* base template, third- or fourth-generation subterranean adaptation, bone-plate face, calcified upper-extremity skin, lower-extremity musculature consistent with a population that has not stood fully erect in two generations.\n\n"
        "Not a monster. A *person*. Or something a person had been when there was still time to be one.\n\n"
        "Kyle has four seconds.\n\n"
        "It comes onto the shelf at the back end of those four seconds. It is, when he sees it, exactly what the trace resolved: a *human shape that has been forced into a shape that is no longer human*. The silhouette is not animal. The silhouette is wrong-human, the way a face seen through old glass is wrong-human, the way a body in a poorly-lit medical photograph is wrong-human. Long arms. Bent back. Broad chest where the ribs have plated outward. The face — what used to be a face — is the worst part, because the eyes are still in approximately the right places and they are still recognizably eyes, set above bone-plates that have grown across what used to be a mouth in the gradual way coral grows across a wreck. Two meters of it. The hind legs spread wide for purchase on the wet ferrocrete. The head, which is still a head, drops. And then it speaks.\n\n"
        "*Hungry,* it says. The bone-plates work the air. The word is wet at the edges, the consonants softened by anatomy that no longer has lips, but it is *the word*, and Kyle hears it. *Hungry. Family.*\n\n"
        "He understands. There are others. Up the tunnel, in whatever crew warren this thing came down from, there are others — children, partners, parents, the family-group it is hunting *for* — and this one has been sent or has volunteered or has simply *gone*, the way the strongest member of a group goes when the group cannot eat. Kyle is heat. Kyle is meat. Kyle is what the family needs and this one is the means of acquiring it. The math is not the math of a predator. The math is the math of a person doing what they have to do for the people they cannot let down.\n\n"
        "Kyle does not have time to let the math land. The math will land later. The math will land for the rest of his life.\n\n"
        "It lunges.\n\n"
        "He steps inside. The cargo arm swings. Too high, too late — the E.L.F. routing AROUND the intent instead of THROUGH it, compensating for a body that no longer has the geometry the firmware expects — and the pipe catches the calcified skin at the shoulder joint and rings off, the vibration traveling back through the coupling into his right stump and he feels it as pressure without pain, which is worse somehow than pain would be. The mutant's shoulder hits his chest. The wall arrives at his back. He takes it on the shoulder blades and his knees bend and his feet find the shelf edge and he does not go down because going down is the option that ends the math, and he is still inside the math, and the math has under three hours left, and he is going to use them. The bone-plates drive at his throat. He turns his head. The plates rake his jaw at the chrome bracket and the specific ache he has been using as a handhold all night flares white and he files it and his left stump comes up.\n\n"
        "The glass is at his foot. Was at his foot. He does not remember picking it up. The E.L.F. does not remember picking it up either — it is not in the arm's movement log, which means he did it, which means some part of him that operates below the NeoCortex's timestamp threshold picked up a hand-sized shard of cargo crate glass while the arm was swinging and missed, and the glass is in his left stump now, end-on, the cauterizer band's residual heat conducting into the cut edge and the cut edge into his grip, which is not a grip because he has no fingers to grip with, but the glass does not fall. The heat holds it.\n\n"
        "The mutant raises one of its long arms over Kyle's head. The arm is going to come down. Kyle has perhaps half a second between now and the moment he stops being able to do anything about anything. And in that half-second, the mutant says it.\n\n"
        "*Sorry.*\n\n"
        "Wet at the edges. Bone-plates working the word. The way you say *sorry* when you have understood, in the part of you that is still a person, that what you are about to do is the wrong thing, and you are going to do it anyway because the family upstairs is hungrier than you are sorry. The eyes — the human eyes, set above the plate-face — are looking at Kyle the way a person looks at another person. Apologetic. Not malicious. Not feral. *Sorry.*\n\n"
        "Kyle drives the shard up.\n\n"
        "Through the soft seam under the bone-plates, through the loose connective tissue where the jaw plates fuse and the calcified skin never grew, through the throat that still produces *sorry* in a register the body has retained when it had to give up so much else. The blade finds something arterial and opens it. Hot blood comes in three liters and continues coming. The mutant's hind legs go first. Then the front. The bone-plates work twice more on air — *Sor— Sor—* — and then stop. The shelf is dark with it. The smell is copper and rot and something pharmaceutical, whatever the Depot's stock was eating before it ate nothing."
    )

    html = html.replace(old_passage, new_passage, 1)
    print('  Mutant arrival/fight rewritten as Lenny-tragic')
else:
    print('  WARN: mutant-arrival passage not matched')

# ---------- 3. ADD AFTERMATH MOMENT (Kyle registers what he killed) ----------
# Find "Kyle stands over it" and add interiority
post_kill_anchor = 'Kyle stands over it.'
if post_kill_anchor in html:
    addition = (
        ' He looks at it. The body, in the wet light off the trench, is recognizable. The eyes are still open. They are still in approximately the right places. Above the plate-face there is a forehead that is unmistakably the forehead of a person, and the hairline is wrong but it is a hairline, and Kyle can see — without choosing to — what this thing was, before. Someone\'s sibling. Someone\'s child. Someone\'s parent who went down the tunnel because the family was hungry, and did not come back, and now would not. He files it. He files it where he files the things that are going to matter later, alongside Hua\'s real hand on Silence\'s saya and the pork smell and the AutoDoc address and the specific weight of his own hands when Hua placed them in the chum barrel. The file is getting heavy. He does not have time to feel the weight of it now. He is going to feel it later, when the work is done, in some kitchen at four in the morning with a bowl of something hot and Mrs. Chen across the counter who will not ask him what he did tonight and will not have to. *Sorry,* the mutant said. He files that too.\n\n'
    )
    html = html.replace(post_kill_anchor, post_kill_anchor + addition, 1)
    print('  Added Lenny-aftermath interiority')

# ---------- 4. SAVE & SYNC ----------
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

print(f'\nFinal chapter: {len(html)} chars (~{len(html)//5} words)')
checks = [
    ('"Hungry. Family." line', 'Hungry. Family.'),
    ('"Sorry." line', '*Sorry.*'),
    ('Bone-plates not chitin', 'bone-plates'),
    ('Human-template description', 'wrong-human'),
    ('200-year worldbuilding', 'two hundred years'),
    ('Glow-in-the-dark rats', 'rats glow'),
    ('Lenny aftermath: "Someone\'s sibling"', "Someone's sibling"),
    ('Mrs. Chen four-in-the-morning callback', 'four in the morning'),
]
for label, needle in checks:
    print(f'  [{"+" if needle in html else "-"}] {label}')
