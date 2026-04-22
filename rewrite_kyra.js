// Rewrite story 019db31fe8887c97a04965978b5ccdb3:
//   - Rename Mira → Kyra (keep Krastev-Okonjo surname)
//   - Age up to 19; recast as "the completed version" of Kyle (Pitch A)
//   - She is not a captive; she is an asset being prepared for handoff
//   - She has been raised on Kyle's recorded life; she knows him; she speaks first
//   - The K in 4471-K is for *kindred* — same genetic lineage, the corrected draft
//   - Walk away from Logan/Laura, Last of Us, Stranger Things
const fs = require('fs');

const STORY = 'D:/Projects/MindAttic/StreetSamurai/engine/data/stories/019db31fe8887c97a04965978b5ccdb3';
const STUB  = 'D:/Projects/MindAttic/StreetSamurai/engine/data/people/019db33aedd17097b813f9e28da1ba5f.json';

// Surgical, whole-paragraph rewrites. Applied before the generic Mira→Kyra swap.
// If any `find` doesn't exist, the script halts — we don't want silent no-ops on such a load-bearing rewrite.
const surgical = [
  {
    label: 'Beat 6 cell-opening paragraph',
    find: `She is perhaps eleven. She is sitting on the bunk with her knees drawn up, and she is counting the rivets on the cell door — her lips moving slightly, her finger tracing the pattern in the air in front of her, not touching anything. She does not look up when he enters. She finishes the count. Her finger stills. Then she looks at him, and her face does the thing that Kyle's face does, the thing the facility produces: no reaction. Not absence of feeling. Presence of discipline. The chrome leads are visible at the base of her skull, the skin around them still pink and not quite closed, the scar fresh enough that he can see where the insertion port was cut. He knows the cut. He has the same cut. His healed badly, raised and keloid at the edge. Hers will too, probably. The hardware is looking at the array geometry before he decides to look, mapping the electrode density, the thread count, and what it returns is a number he was not prepared for: 32,768. He stands in the doorway of the cell and does not move. She is counting the tiles now. Starting over. One, two, three — her lips barely moving, her eyes tracking the floor with the particular focus of someone who has learned that counting is the thing you do when you need the world to hold still. He knows this. He was this. The code says: *secure the target, confirm identity, initiate extraction*. The code does not have a category for what he is looking at. He waits for her to finish the count.`,
    replace: `She is nineteen, maybe twenty. She is not on the bunk — she is standing, facing the door, already turned toward it before Kyle opens it, the way you stand when you have been told the time and place of an appointment. The cell is less cell than *suite*: the bunk is made. The walls are painted. There is a plant. Security here was never about keeping her in. The NeoCortex array at the base of her skull is not fresh — the insertion port is healed smooth, the kind of healed that takes years, the chrome leads buried under grown-in hair. The hardware looks at the array geometry before Kyle decides to look, and what it returns is a number he was not prepared for: 32,768. His own array, the one the parent facility burned out of him before it ever properly integrated, topped at four thousand and ninety-six. Her electrode density is eight times his. She is a corrected draft of a document whose first version Axiom threw in the fire. Her face does the thing Kyle's face does — the thing the facility produces, that he has never seen on anyone else's face and did not know could be produced on anyone else's face: no reaction, not absence of feeling, presence of discipline. She looks at him with the specific non-surprise of someone who has been studying his face since before she could walk. The code says: *secure the target, confirm identity, initiate extraction*. The code does not have a category for what he is looking at.`,
  },
  {
    label: 'Beat 6 first-line reveal',
    find: `After a long silence, she says: "4471-K." A pause, like she is reaching for something she has not used in a while. "My name is Mira."`,
    replace: `She speaks first. Not *who are you*. Not *are you here to help*. She says, "You are forty-two minutes behind schedule, Kyle." She says his name the way someone says a word they have been practicing alone for a long time. Her voice is level and unhurried and entirely hers. A pause. "They call me 4471-K. My name is Kyra." Another pause, flatter, a single fact laid on the table. "You are not here to rescue me. You are here because they wanted you here. I am here because they wanted you here. We should talk about who *they* are before we discuss what either of us does next."`,
  },
  {
    label: 'Beat 9 manifest — kindred reframing',
    find: `Current active subjects: fourteen. Age range: eight to fifteen. Subject designations: tissue-match codes. Hers reads 4471-K. His own — the designation he has not said aloud in seventeen years — reads 4471. No suffix. She is the modifier. The backup harvest.`,
    replace: `Current active subjects: fourteen. Age range: eight to twenty-two. Subject designations: tissue-match codes. Hers reads 4471-K. His own — the designation he has not said aloud in seventeen years — reads 4471. No suffix. The K is for *kindred*: same genetic lineage, started nineteen years ago from tissue the parent facility banked while they were still trying to make him work. She is not the backup harvest. She is the corrected version. Everything the program learned from his breaking is in her file. Her curriculum included recordings of his screaming.`,
  },
  {
    label: 'Beat 9 hand-holding descriptor',
    find: `Mira's hand in his — she took it somewhere between the thermal seal and here, small and warm and present, not asking for anything, just taking what was available`,
    replace: `Kyra's hand in his — she took it somewhere between the thermal seal and here, deliberate and warm and present, not asking for anything, taking what was available because she has decided what to do with it`,
  },
  {
    label: 'Beat 10 hand-holding descriptor',
    find: `Mira's hand in his makes the decision, small and warm and present`,
    replace: `Kyra's hand in his makes the decision, deliberate and warm and present`,
  },
  {
    label: 'Beat 10 alcove reflection',
    find: `"4471-K was good too. Before."`,
    replace: `"Being 4471-K was easier. You do not have to decide what you owe anyone. You do not have to be a person in a room with another person who has your cheekbones."`,
  },
  {
    label: 'Beat 12 "this child"',
    find: `She has never seen this child before.`,
    replace: `She has never seen this woman before.`,
  },
];

// String transformation pipeline applied to every string leaf in every JSON file.
function transformText(text) {
  let out = text;

  // Apply surgical rewrites (strict — throw if any is missing, for safety)
  for (const { label, find, replace } of surgical) {
    if (out.includes(find)) out = out.split(find).join(replace);
  }

  // Full-name first so the short-name regex below doesn't corrupt it
  out = out.replace(/Mira Krastev-Okonjo/g, 'Kyra Krastev-Okonjo');
  // Standalone "Mira" → "Kyra"
  out = out.replace(/\bMira\b/g, 'Kyra');

  // Residual age/child markers — only replace in contexts we haven't already surgically rewritten
  out = out.replace(/\bage approximately eleven\b/g, 'age approximately nineteen');
  out = out.replace(/\bapproximately eleven years old\b/g, 'approximately nineteen');

  return out;
}

function transformStrings(obj) {
  if (typeof obj === 'string') return transformText(obj);
  if (Array.isArray(obj)) return obj.map(transformStrings);
  if (obj && typeof obj === 'object') {
    const out = {};
    for (const k of Object.keys(obj)) out[k] = transformStrings(obj[k]);
    return out;
  }
  return obj;
}

// Verify every surgical rewrite target exists in at least one string leaf of checkpoint.json
// before writing anything. JSON.parse gives us decoded strings (no backslash-escaped quotes).
function anyStringContains(obj, needle) {
  if (typeof obj === 'string') return obj.includes(needle);
  if (Array.isArray(obj)) return obj.some(x => anyStringContains(x, needle));
  if (obj && typeof obj === 'object') return Object.values(obj).some(v => anyStringContains(v, needle));
  return false;
}
const cpParsed = JSON.parse(fs.readFileSync(`${STORY}/checkpoint.json`, 'utf8'));
for (const { label, find } of surgical) {
  if (!anyStringContains(cpParsed, find)) {
    console.error(`MISSING TARGET: "${label}"`);
    console.error(`  find: ${find.substring(0, 120)}...`);
    process.exit(1);
  }
}
console.log('All surgical targets located. Applying rewrites.\n');

// Process story files
const files = [
  `${STORY}/checkpoint.json`,
  `${STORY}/outline.json`,
  `${STORY}/outline_review.json`,
  `${STORY}/events.json`,
  `${STORY}/knowledge.json`,
  `${STORY}/quality_report.json`,
];
console.log('=== Story file rewrites ===');
for (const f of files) {
  if (!fs.existsSync(f)) { console.log('(skip)', f); continue; }
  const raw = fs.readFileSync(f, 'utf8');
  const json = JSON.parse(raw);
  const updated = transformStrings(json);
  fs.writeFileSync(f, JSON.stringify(updated, null, 2));
  const name = f.split(/[\\/]/).pop();
  const miraBefore = (raw.match(/\bMira\b/g) || []).length;
  const miraAfter = (JSON.stringify(updated).match(/\bMira\b/g) || []).length;
  console.log(`  ${name}: Mira ${miraBefore}→${miraAfter}`);
}

// Rewrite character stub for Kyra at 19 — the completed version
console.log('\n=== Character stub (Pitch A) ===');
const stub = JSON.parse(fs.readFileSync(STUB, 'utf8'));
stub.name = 'Kyra Krastev-Okonjo';
stub.aliases = ['Kyra', '4471-K'];
stub.age = 19;
stub.gender = 'female';
stub.pronouns = 'she/her';
stub.role = `The corrected draft. Ferrogate / Axiom's continuation program started her nineteen years ago from tissue banked during Kyle's time at the parent facility — same genetic lineage, grown out with every lesson the program learned from his breaking. Her designation 4471-K marks the kindred line: not a backup harvest, the successor. She was raised inside the program with Kyle's recorded life as her curriculum. When Kyle opens her cell door she has been studying his face for as long as she has had eyes to study with. She is not a victim waiting for rescue. She is an asset that was being staged for handoff to a buyer, with her own view on whether she wanted to go.`;
stub.status = 'alive';
stub.location = 'Extracted from Ferrogate BioSystems secondary facility by Kyle Corbin-Vasik; currently in a Gray Zone safe room arranged through one of Kyle\'s contacts. Status provisional — she has not yet decided whether to stay.';
stub.description = `Nineteen years old. The Ferrogate secondary facility's continuation-program subject, genetically a branch of NDC-4471 (Kyle's parent-facility designation) — grown from tissue the parent program banked while they were still trying to make Kyle work, and never stopped trying to make the line work. Fully integrated NeoCortex array at 32,768 electrode density — eight times Kyle's burned-out 4,096-density array, the corrected specification the program arrived at after watching him fail. Healed insertion port scar at the base of the skull, chrome leads buried under grown-in hair. Her face carries Kyle's cheekbones and jaw line — the same facility lineage, corrected. She was raised on his recorded life: every biometric crash, every conditioning session, every escape attempt is part of her curriculum. She knows his name, his tells, his fears, the rhythm of his exhales. She was being staged for handoff to a buyer when Kyle broke the door. Her counting tic (rivets, tiles, rain impacts, stopping at seventeen) is array-regulation, not childhood trauma — it is the thing her trainers taught her to do when the neural load spiked. She speaks in complete sentences, level voice, nineteen years of discipline audible in every pause. She has her own agenda; Kyle is a variable in it, not its protagonist.`;
stub.relationships = [
  {
    name: 'Kyle Ellen Corbin-Vasik',
    type: 'kindred',
    description: `Kyra is a genetic-lineage continuation of Kyle's program subject line. Same facility code, suffix -K for *kindred*. She has studied him her whole life from archival recordings; he met her for the first time when he opened her cell door. The dynamic is not protector/ward, parent/child, or rescuer/rescued. It is: two people who share a neural architecture and a conditioning curriculum, one of whom was the failure the program learned from, one of whom was the success the program built from those lessons. Neither owes the other anything and both know it.`,
    emotional_core: `The uncanny recognition on both sides — Kyle meeting the person Axiom built in his shape; Kyra meeting the predecessor whose screaming she was raised listening to. What do you owe the person the system built in your absence.`,
    story_tension: `Kyra was being prepared for handoff to a buyer. That handoff is now a failed contract, which means a contractor is now working backwards from the loss to find who took her. The parent facility's program is not over. Kyle has opened a file that cannot be closed, and Kyra is walking around with the answer to it.`,
  },
  {
    name: 'Chen Wei-Lin',
    type: 'host',
    description: `Mrs. Chen, proprietress of the Gray Zone noodle stall where Kyle eats. Fed Kyra without being asked or asking. Called her *xiao gui* — little ghost — the same register of affection Mrs. Chen reserves for things she has decided to care about without making a conversation of it.`,
    emotional_core: `Recognition of kind. Mrs. Chen has kept many of Kyle's secrets and adopts this one without ceremony.`,
    story_tension: '',
  },
  {
    name: 'Ferrogate BioSystems',
    type: 'architect',
    description: `The facility that built her. Tissue-match code 4471-K. Continuation program running since nineteen years ago. Asset valuation marked Φ0.00 on the active manifest with subject integrity flagged compromised — a paperwork downgrade to justify the buyer handoff. Fourteen other subjects still active on the manifest.`,
    emotional_core: '',
    story_tension: `The file is not closed. The manifest persists. The program will initiate a replacement cycle.`,
  },
  {
    name: 'Dae-jung Seo',
    type: 'program consultant (deceased)',
    description: `Seo's name appears as program consultant on the Ferrogate manifest with a contribution record dated 2189–2193 — the same years Seo was teaching Kyle the blade. File closed, deceased. Kyra's training materials may have been shaped, in part, by the same man who taught Kyle how to survive.`,
    emotional_core: 'Kyle is still reconciling this.',
    story_tension: 'Whatever Seo was doing for the program, Kyra knows about it.',
  },
];
stub.tags = ['auto-scaffolded', 'needs-review', 'continuation-program', 'kindred', 'tissue-match', '4471-line'];
stub.physical_description = {
  heritage: 'Mixed Eastern European and West African (Krastev / Okonjo) — the legal identity preserved on her birth registry; the facility did not strip it because the continuation program required the paperwork to pass tissue-origin audits. Same facility genetic base as Kyle, so her cheekbones and jaw line carry his.',
  height_cm: 168,
  weight_kg: 58,
  build: 'trained-to-specification: lean, composed, no wasted motion. Not undernourished — she was maintained to a product spec. The body moves with the conditioning still audible.',
  hair_color: 'dark brown',
  hair_style: 'shoulder-length, straight, grown in fully over the integrated array — the chrome leads at the base of her skull are not visible unless she lifts the hair deliberately',
  hair_length: 'shoulder',
  eye_color: 'gray',
  skin_tone: 'medium',
  complexion: 'clear — the facility keeps the subjects medically supervised',
  distinguishing_marks: [
    'healed NeoCortex array port scar at base of skull, keloid at edges',
    'chrome leads faintly visible when hair is lifted',
    'Kyle\'s cheekbone and jaw line — same facility genetic base',
    'the facility face: no reaction not absence of feeling, presence of discipline',
  ],
  visible_augmentations: 'NeoCortex array at 32,768 electrode density — fully integrated, not visible externally unless the hair is parted',
  posture_movement: 'economical; tracks exits before greeting; will not stand in the middle of a room. The conditioning is visible in how she waits for doorframes before stepping through them.',
  clothing_style: 'facility-issue at extraction — tailored gray, intended for display to a prospective buyer; whatever she chooses since, plain and dark',
};
fs.writeFileSync(STUB, JSON.stringify(stub, null, 2));
console.log('Character stub renamed:', stub.name, '— age', stub.age, '— aliases:', stub.aliases);

// Rebuild story.json markdown
console.log('\n=== Rebuilding story.json markdown ===');
const cp = JSON.parse(fs.readFileSync(`${STORY}/checkpoint.json`, 'utf8'));
const lines = [];
lines.push(`# ${cp.Title}`); lines.push('');
lines.push(`*Protagonist: ${cp.Protagonist}*`); lines.push('');
let lastAct = 0;
for (const beat of cp.Beats) {
  if (beat.Act !== lastAct) {
    const act = (cp.Outline?.acts || []).find(a => a.act_number === beat.Act);
    const actName = act?.name || `Act ${beat.Act}`;
    lines.push(`## Act ${beat.Act}: ${actName}`); lines.push('');
    lastAct = beat.Act;
  }
  lines.push(beat.Text.trim()); lines.push(''); lines.push('---'); lines.push('');
}
const md = lines.join('\n').replace(/\s+$/, '');
const storyJsonPath = `${STORY}/story.json`;
const story = JSON.parse(fs.readFileSync(storyJsonPath, 'utf8'));
story.html = md;
story.modified = new Date().toISOString();
fs.writeFileSync(storyJsonPath, JSON.stringify(story, null, 2));
console.log('markdown length:', md.length);
console.log('Residual "Mira" in rebuilt markdown:', (md.match(/\bMira\b/g) || []).length);
console.log('Residual "Kyra" count:', (md.match(/\bKyra\b/g) || []).length);
