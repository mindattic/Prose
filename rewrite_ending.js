// Ending rewrite for story 019db31fe8887c97a04965978b5ccdb3:
//   - Kyra is UNTRAINED: same neural architecture as Kyle, zero training.
//     The facility scheduled field instruction for *after* first buyer handoff.
//   - Gunslinger archetype arrives in her hands during the Beat 8 maintenance-corridor
//     ambush — first gun she has ever held, three clean shots.
//   - They fight out together; at the buffer zone she parts ways and walks east alone.
//   - Beat 12 becomes Kyle + Pixel at Mrs. Chen's; Kyra's parting joke is relayed.
//   - "There is hope for her yet."
const fs = require('fs');

const STORY = 'D:/Projects/MindAttic/StreetSamurai/engine/data/stories/019db31fe8887c97a04965978b5ccdb3';
const STUB  = 'D:/Projects/MindAttic/StreetSamurai/engine/data/people/019db33aedd17097b813f9e28da1ba5f.json';

// --- Surgical paragraph replacements -------------------------------------------------

const BEAT8_P3_OLD = `It goes wrong at the baton. He has already put the first contractor down — joint lock, two points of pressure, controlled — and he is inside the second one's reach when the baton connects with his right shoulder. The hardware catches the discharge and routes it and routes it wrong, the feedback going somewhere it should not go, and then there are three seconds that are not there. No gap in the world this time. The world is simply absent and then present again, and he is behind the coolant conduit with his back against the metal and Kyra's hand is releasing his jacket collar with the precision of someone who has already done the thing and does not need acknowledgment for having done it, and one contractor is down in the corridor in a position Kyle does not remember putting him in. His right hand is not working correctly. He opens and closes the fingers and the grip strength returns at approximately sixty percent and he does not look at his shoulder because looking at it will not change anything, and the other two are recalibrating, the third one speaking quietly into the tactical feed, requesting backup or confirming containment or both. Kyle exhales. He comes out from behind the conduit and finishes it. It is precise. It is very fast. It is the kind of precision that lives below thought, that the hardware learned from Seo's footage and the facility's drills and six years of jobs that did not appear in any ledger, and when it is done Kyle is standing in the corridor with his right arm hanging slightly wrong and two contractors breathing on the floor and the third one down at the east junction in the position he does not remember. The silence after is specific. It has a texture. He knows the texture. He does not like knowing it.`;

const BEAT8_P3_NEW = `It goes wrong at the baton. He has already put the first contractor down — joint lock, two points of pressure, controlled — and he is inside the second one's reach when the baton connects with his right shoulder. The hardware catches the discharge and routes it wrong, the feedback going somewhere it should not go, and then there are three seconds that are not there. No gap in the world this time. The world is simply absent and then present again.

He hears the sound before he sees the cause. Two shots, tight pattern, the specific acoustic signature of a Tier-2 sidearm fired from an unfamiliar grip. A pause. A third shot, wider, still clean. Then silence.

His vision comes back. He is behind the coolant conduit with his back against the metal. Kyra's hand is releasing his jacket collar — she dragged him to cover during the gap. The handgun in her other hand belonged to the first contractor he put down at joint-lock. She took it off the downed man while Kyle was still inside the second one's reach. The second contractor is in the corridor with a hole in the meat of the hip and another punched through the shoulder plate, breathing flat. The third contractor is against the east-junction wall, sliding, and the slide of the handgun in Kyra's hand is locked back.

She is looking at the slide with the same expression she looked at the array geometry with — *tell me what you are*. She has never held one before. The facility had not trained her yet. The program scheduled field instruction for the year after first buyer handoff — she was to be put through combat trials by whoever bought her before formal curriculum began. What is in her hand right now is the accident of pointing the barrel correctly the first time and a neural architecture the program built to be very good at precisely this, and the architecture met the opportunity ninety seconds ago and has not stopped absorbing the data. She releases the slide. She checks the magazine. She reseats it. Her grip shifts to the correct position and nothing about the shift is taught. She looks up. She looks at Kyle.

The silence after is specific. It has a texture. He knows the texture. He does not like knowing it, and he is not the one who made it this time.`;

const BEAT8_P4_OLD = `Kyra is on pipe joint seventeen. She finishes the count, whatever the count is, and then she looks at him — not at the contractors, not at the corridor, at him — with the same flat assessment she had in the cell, the eyes tracking the same way the hardware tracks: exits first, then threat, then status. She does not ask if he is all right. She does not look at his shoulder. She looks at his hands, and then she looks at his face, and then she looks back at the pipe joints and starts over from one. He stands there in the corridor with the 19Hz hum in the walls and the cycling alarm and the thing he did in the three seconds he was not present, and he thinks: *she is still not afraid of me*, and he does not know what to do with that, so he says, voice flat, clipped, no space for what it costs to say it: "Move."`;

const BEAT8_P4_NEW = `Kyra lowers the handgun. She does not holster it — she has no holster — she slides it into the waistband of the facility smock and her hand lingers on the grip for a moment, the way you linger on a thing you have just discovered you are fluent in. Then she looks back at the pipe joints on the opposite wall and starts the count over from one. Her lips barely move. She is regulating. The neural load from what just happened is pulsing under her scalp and the counting is how she told the program she was not overloaded during cognitive-demand trials. He stands there in the corridor with the 19Hz hum in the walls and the cycling alarm and the thing she did in the three seconds he was not present, and he thinks: *she is still not afraid of me, and now I understand why*, and he does not know what to do with that, so he says, voice flat, clipped, no space for what it costs to say it: "Move."`;

// Beat 11 parting — appended after the existing last paragraph of Beat 11.
const BEAT11_ENDING_TAIL = `

---

Kyra is waiting at the buffer-zone fence line. She did not stay at the Gray Zone contact's locked room. The handgun she took off the first contractor is tucked into the waistband of the facility smock the way it was tucked when she fired it ninety seconds into her life as a person outside the facility. She does not explain. She falls in beside him, half-step back and left, and he does not ask. They walk two blocks without speaking. The rain has stopped. The light is coming up in the way it comes up over the Lateral Junction in the spring — flat, gold, honest in a way the rest of the day is not.

At the alley mouth she stops. He knows what the stop means before she says anything. She does not make him wait for it. "I am going," she says, level. "Not with you." A pause. "I have questions about what I am that I cannot answer in your house. You have a life. I am not in it yet. I might be, later. But the answer is not here and it is not you." She looks at him. No discipline on her face now — just tiredness and something close to resolve. "Do you understand what I am saying."

He does. He has been waiting for her to say it since the moment the handgun came up in her hand in the maintenance corridor and the shots went clean and something in her face opened that had been closed before. He nods. The hardware does not argue.

"Good luck," he says. He means it. He has said it twice in his life and meant it. This is the second time.

She almost smiles. Not quite — the machinery of it is untested and she is discovering it in real time — but the corner of her mouth moves in the direction of a thing she has not done before. "I will be back," she says, "when I have something to offer." A pause, and then the thing — the first joke she has ever made, the language she is still learning to speak, the cadence borrowed from nineteen years of listening to him — "or when I need some bullets dislodged."

She turns. She walks east. He watches her for nine seconds and then the alley takes her. He stands with the drive in his jacket and the blade on his back and the Lateral Junction morning in his eyes and does not move. The hardware logs, without comment, a thought: *there is hope for her yet*. He starts walking.`;

// Beat 12 — complete replacement
const BEAT12_NEW = `The stool next to his is empty when he sits down. It stays empty. Mrs. Chen sets a bowl in front of him — broth, thick-cut noodles, no bean sprouts — and she looks at the empty stool once, briefly, and does not ask. She has noticed the empty stool. She is not going to make it a conversation.

Kyle sets the blade against the counter leg because the shoulder will not allow the back-carry, not today, and he opens the notebook and writes the last entry without deciding to write it: hardware damage, facility-origin, the drive in his jacket, the number of active load warnings currently logging against the lateral array. Then below it, in the same handwriting, a code: 4471-K. Below that, a name: *Kyra*. Below that, three words: *somewhere east of here*. The notebook is what he uses when the hardware cannot file something and he cannot say it aloud and there is nowhere else for it to go.

Pixel comes through the door ten minutes later. She is wearing the oversized sweater that swallows her hands and the steel-toed boots she has had since she was fourteen, and she sits on the empty stool without asking, and Mrs. Chen sets a second bowl in front of her with extra protein because she has been feeding Pixel since before Pixel knew she needed to be fed. The pink hair is more neon at the ends this week. The pale grey-blue eyes notice the shoulder, notice the blade leaning, notice the entry in the notebook. She does not ask about any of it. She eats three mouthfuls. Then, without turning her head, she looks at the notebook — at the code and the name and the three words — and says:

"Are we going to see her again."

Kyle does not answer right away. He turns the chopsticks over in his hand. He thinks about the handgun in the maintenance corridor and the clean line of the third shot and the way Kyra's face opened. He thinks about the alley at the buffer zone and the almost-smile, the corner of her mouth moving in the direction of a thing she had not done before.

"She said she would come back when she had something to offer." A pause. "Or when she needed some bullets dislodged." He looks at the empty stool past Pixel's elbow. "She said it like a joke. It was the first joke she has ever made." Another pause, longer. "She almost smiled."

Pixel absorbs this. She eats another mouthful. Then, quiet, the kind of quiet that means she has understood exactly what Kyle is not saying: "There is hope for her yet."

Kyle looks at her. Pixel has been across the hall long enough to know when something matters, and she does not make a thing of it. That is most of what Pixel is good at, besides everything else she is good at. Kyle goes back to his bowl. The chili oil burns the same. The blade leans against the counter leg — Seo's blade, maintained as prayer for six years — unanswered, a question he has not yet learned to ask correctly. The stool to his right is Pixel, and the stool to his left is empty, and somewhere east of here a nineteen-year-old woman with an array that has never been trained and a handgun she took off a corporate contractor is walking the Glooms alone. She will come back or she will not. The arithmetic is not complicated. It is only costly.

Mrs. Chen refills the tea without being asked. She sets a third cup down on the empty stool's place-setting and turns back to the pot and that is all. The cup is there. It will still be there at the end of breakfast. It will be there tomorrow, probably. *Xiao gui*, she says under her breath, not quite to the room, not quite to either of them — the tone she uses for things she has decided to care about without making it into a conversation. Little ghost.

Kyle eats. Pixel eats. Outside, the rain does not come back. The hardware is quiet. The morning holds.`;

// --- Apply rewrites ------------------------------------------------------------------

const cp = JSON.parse(fs.readFileSync(`${STORY}/checkpoint.json`, 'utf8'));

function findBeat(bi) { return cp.Beats.find(b => b.BeatIndex === bi); }

// Verify all surgical targets exist before mutating anything
const b8 = findBeat(8), b11 = findBeat(11), b12 = findBeat(12);
function assertContains(beat, needle, label) {
  if (!beat.Text.includes(needle)) {
    console.error(`MISSING TARGET in ${label}`);
    console.error('  looking for:', needle.substring(0, 140) + '...');
    process.exit(1);
  }
}
assertContains(b8, BEAT8_P3_OLD, 'Beat 8 P#3');
assertContains(b8, BEAT8_P4_OLD, 'Beat 8 P#4');
console.log('Surgical targets located. Applying rewrites.\n');

// Beat 8 swaps
b8.Text = b8.Text.replace(BEAT8_P3_OLD, BEAT8_P3_NEW);
b8.Text = b8.Text.replace(BEAT8_P4_OLD, BEAT8_P4_NEW);

// Beat 11 — append parting tail (only if not already appended)
if (!b11.Text.includes('Kyra is waiting at the buffer-zone fence line')) {
  b11.Text = b11.Text.trimEnd() + BEAT11_ENDING_TAIL;
}

// Beat 12 — complete replace + retitle
b12.Text = BEAT12_NEW;
b12.Title = "Final Image — 'Somewhere East of Here'";

// Cast list — add Pixel (keep Sable, Chen, drop if absent keep as-is)
if (!cp.Characters.includes('Pixel')) {
  cp.Characters.splice(2, 0, 'Pixel'); // after Kyra
}

fs.writeFileSync(`${STORY}/checkpoint.json`, JSON.stringify(cp, null, 2));

// story.json — rebuild markdown + update cast
const st = JSON.parse(fs.readFileSync(`${STORY}/story.json`, 'utf8'));
if (!st.characters.includes('Pixel')) st.characters.splice(2, 0, 'Pixel');
const lines = [];
lines.push(`# ${cp.Title}`); lines.push('');
lines.push(`*Protagonist: ${cp.Protagonist}*`); lines.push('');
let lastAct = 0;
for (const beat of cp.Beats) {
  if (beat.Act !== lastAct) {
    const act = (cp.Outline?.acts || []).find(a => a.act_number === beat.Act);
    lines.push(`## Act ${beat.Act}: ${act?.name || 'Act ' + beat.Act}`); lines.push('');
    lastAct = beat.Act;
  }
  lines.push(beat.Text.trim()); lines.push(''); lines.push('---'); lines.push('');
}
st.html = lines.join('\n').replace(/\s+$/, '');
st.modified = new Date().toISOString();
fs.writeFileSync(`${STORY}/story.json`, JSON.stringify(st, null, 2));

// --- Character stub: untrained + gunslinger + parting --------------------------------
const stub = JSON.parse(fs.readFileSync(STUB, 'utf8'));
stub.role = `The untrained successor. Ferrogate / Axiom's continuation program started her nineteen years ago from tissue banked during Kyle's time at the parent facility — same genetic lineage, corrected architecture, never field-trained. Her designation 4471-K marks the kindred line. The program had scheduled combat instruction for the year after first buyer handoff; they never got to start. When Kyle broke the door she was a blank-slate neural weapon with nineteen years of captivity-conditioning (observation, exit-tracking, economy of gesture) and no tested skills. She found her archetype by accident during the maintenance-corridor ambush — she took a Tier-2 sidearm off a downed contractor and put two people down with three shots, having never held a firearm before. The gunslinger arrived in her hands like a language she had been dreaming in. At dawn after the Ferrogate extraction she parted ways with Kyle at the buffer zone and walked east alone. Current status: unknown, working. Will return when she has something to offer — or when she needs some bullets dislodged.`;
stub.status = 'alive — at large, operating independently in the GLMZ east of the Lateral Junction';
stub.location = 'Last seen walking east from the buffer-zone fence line on the morning after the Ferrogate extraction. Currently: unknown. She did not stay with the Gray Zone contact Kyle arranged.';
stub.description = `Nineteen years old. Ferrogate secondary-facility continuation-program subject, genetically a branch of NDC-4471 (Kyle's parent-facility designation) — grown from tissue banked while the parent program was still trying to make Kyle work, and never stopped trying to make the line work. Fully integrated NeoCortex array at 32,768 electrode density — eight times the 4,096 Kyle's burned-out array topped at, the corrected specification the program arrived at from watching him fail. Healed insertion port scar at the base of the skull, chrome leads buried under grown-in hair. Her face carries Kyle's cheekbones and jaw line — same facility genetic base, corrected. She was raised on his recorded life: every biometric crash, every conditioning session, every escape attempt was part of her curriculum.

Untrained in combat. The program scheduled field instruction for the year after first buyer handoff — her combat trials were to be run by whoever bought her, before formal curriculum began. What she walked out of the facility with was raw neural capacity and nineteen years of passive conditioning. She has never thrown a punch, never been taught a stance, never had a weapon put in her hand by anyone qualified to teach. She has never seen a fight before the one Kyle had in the Ferrogate maintenance corridor.

During that fight she picked up a Tier-2 sidearm off a contractor Kyle had downed at joint-lock, fired three shots, and put two trained operators on the ground — including one with a head-shot that she made by feel. The grip, the slide, the magazine check, the corrective stance for the second and third shots all arrived without instruction. The gunslinger archetype was not on her curriculum and no one in her training materials demonstrated it. It was in her. Kyle watched it arrive and understood what the program was trying to build.

Her counting tic (rivets, tiles, rain impacts, stopping at seventeen) is neural-load regulation the facility used during cognitive-demand trials, not childhood trauma. She speaks in complete sentences, level voice — the cadence of nineteen years spent primarily with her own thoughts and a curriculum of Kyle's recorded life. She uses his name the way someone says a word they have been practicing alone.

She walked out on her own at dawn and she meant it. She will be back when she has something to offer, or when she needs some bullets dislodged. The joke was the first one she has ever made.`;
stub.relationships = [
  {
    name: 'Kyle Ellen Corbin-Vasik',
    type: 'kindred',
    description: `Kyra is a genetic-lineage continuation of Kyle's program subject line. Same facility code, suffix -K for *kindred*. She has studied him her whole life from archival recordings; he met her for the first time when he opened her cell door. At the Ferrogate extraction she pulled him out of a corridor ambush, made her first kill, and walked east at dawn without him. Kyle wished her luck and meant it — the second time in his life he has said those words and meant them. The dynamic is not protector/ward, parent/child, or mentor/student. It is: two people who share a neural architecture and a program file, one trained and burned out, one untrained and intact, neither of whom owes the other anything.`,
    emotional_core: 'What do you owe the person the system built in your absence. What do you owe the predecessor whose screaming was your curriculum.',
    story_tension: 'She will come back when she has something to offer — or when she needs some bullets dislodged. Until then Kyle writes her code in his notebook and Mrs. Chen sets an extra cup on the counter.',
  },
  {
    name: 'Pixel',
    type: 'witness',
    description: `Kyle's augment tech and neighbor in The Pivot. Was not present at the extraction. Heard about it at the noodle stall the morning after, asked the question Kyle had been waiting to be asked: "Are we going to see her again."`,
    emotional_core: 'Pixel understood without being told what Kyra was. She does not make a thing of it.',
    story_tension: '',
  },
  {
    name: 'Chen Wei-Lin',
    type: 'host',
    description: `Mrs. Chen, proprietress of the Gray Zone noodle stall where Kyle eats. Set an extra cup of tea at an empty place-setting the morning after the extraction and called the empty space *xiao gui* — little ghost — the same register of affection Mrs. Chen reserves for things she has decided to care about without making a conversation of it.`,
    emotional_core: 'Recognition of kind.',
    story_tension: '',
  },
  {
    name: 'Ferrogate BioSystems',
    type: 'architect',
    description: `The facility that built her. Tissue-match code 4471-K. Continuation program running nineteen years. Asset valuation marked Φ0.00 on the active manifest with subject integrity flagged compromised — a paperwork downgrade to justify the buyer handoff that is now a failed contract. Fourteen other subjects still active on the manifest.`,
    emotional_core: '',
    story_tension: 'The file is not closed. The manifest persists. Axiom knows someone came back for the drive.',
  },
  {
    name: 'Dae-jung Seo',
    type: 'program consultant (deceased)',
    description: `Seo's name appears as program consultant on the Ferrogate manifest, contribution record 2189–2193 — the same years he was teaching Kyle the blade. File closed, deceased. Kyra's training materials may have been shaped in part by the same man who taught Kyle how to survive.`,
    emotional_core: 'Kyle is still reconciling this.',
    story_tension: 'Whatever Seo was doing for the program, Kyra knows about it.',
  },
];

// Physical description: untrained-but-capable
stub.physical_description.build = 'lean, composed, maintained — the facility kept her physically healthy to spec but had not yet begun field-operator training. The body moves with captivity-conditioning audible (observation, exit-tracking, economy of gesture) but not with combat discipline. She does not know how she carries herself yet — she is figuring it out on the walk east.';
stub.physical_description.posture_movement = 'economical; tracks exits before greeting; will not stand in the middle of a room. When she first picked up the handgun her grip shifted to the correct position without being taught — her body knew what it was built to do before her mind had the language for it.';
stub.physical_description.visible_augmentations = 'NeoCortex array at 32,768 electrode density — fully integrated, not visible externally unless the hair is parted. The array has never been field-tested before the Ferrogate extraction. What it can do at operator load is an open question.';

// Belongings: add the handgun
stub.belongings = stub.belongings || {};
stub.belongings.primary_weapon = 'Tier-2 sidearm taken off a downed corporate contractor in the Ferrogate maintenance corridor — first firearm she has ever held. Carried in her waistband for lack of a holster.';

stub.tags = ['auto-scaffolded', 'needs-review', 'continuation-program', 'kindred', 'tissue-match', '4471-line', 'gunslinger', 'untrained', 'at-large'];

fs.writeFileSync(STUB, JSON.stringify(stub, null, 2));

console.log('=== Summary ===');
console.log("Beat 8: gunslinger moment inserted (Kyra takes down contractors during Kyle's blackout).");
console.log("Beat 11: parting scene appended (buffer zone, Kyle wishes her luck, the joke).");
console.log("Beat 12: rewritten as Kyle + Pixel at Mrs. Chen's, empty stool, the relayed joke.");
console.log('Cast:', cp.Characters);
console.log('Stub:', stub.name, '— aliases:', stub.aliases, '— tags:', stub.tags);
console.log('Markdown length:', st.html.length);
