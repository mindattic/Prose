// Rename "Seven" → "Mira Krastev-Okonjo" in story 019db31fe8887c97a04965978b5ccdb3
// and reframe the numbered-subject scheme as tissue-match codes (4471/4471-K),
// tying Kyle's NDC-4471 designation directly to hers.
const fs = require('fs');

const STORY = 'D:/Projects/MindAttic/StreetSamurai/engine/data/stories/019db31fe8887c97a04965978b5ccdb3';
const STUB  = 'D:/Projects/MindAttic/StreetSamurai/engine/data/people/019db33aedd17097b813f9e28da1ba5f.json';

// Surgical rewrites applied before the global \bSeven\b swap.
// Ordering matters — longer-context rewrites first so the generic swap doesn't corrupt them.
const rewrites = [
  {
    find: `After a long silence, she says: "They call me Seven."`,
    replace: `After a long silence, she says: "4471-K." A pause, like she is reaching for something she has not used in a while. "My name is Mira."`,
  },
  {
    find: `Current active subjects: fourteen. Age range: eight to fifteen. Subject designations: One through Fourteen. Seven is line seven.`,
    replace: `Current active subjects: fourteen. Age range: eight to fifteen. Subject designations: tissue-match codes. Hers reads 4471-K. His own — the designation he has not said aloud in seventeen years — reads 4471. No suffix. She is the modifier. The backup harvest.`,
  },
  {
    find: `"Seven was good too. Before."`,
    replace: `"4471-K was good too. Before."`,
  },
  {
    find: `thirteen subjects on the manifest between One and Fourteen with the gap where Seven used to be`,
    replace: `thirteen subjects on the manifest, tissue-match codes still active, with the gap where 4471-K used to be`,
  },
  {
    find: `not a numbered subject with an Axiom asset valuation`,
    replace: `not a tissue-matched subject with an Axiom asset valuation`,
  },
];

// Walk any JSON and apply the transformation to every string leaf.
function transformStrings(obj, fn) {
  if (typeof obj === 'string') return fn(obj);
  if (Array.isArray(obj)) return obj.map(x => transformStrings(x, fn));
  if (obj && typeof obj === 'object') {
    const out = {};
    for (const k of Object.keys(obj)) out[k] = transformStrings(obj[k], fn);
    return out;
  }
  return obj;
}

function transformText(text) {
  let out = text;
  for (const { find, replace } of rewrites) {
    out = out.split(find).join(replace);
  }
  // Global: capital-S standalone "Seven" → "Mira". Case-sensitive. Word-boundary
  // prevents matching "Seventeen" (t is a word char) or lowercase "seven" (count).
  out = out.replace(/\bSeven\b/g, 'Mira');
  // Handle stray "One through Fourteen" references that weren't part of the big rewrite
  out = out.replace(/\bOne through Fourteen\b/g, 'the tissue-match catalog');
  return out;
}

function processFile(path) {
  const raw = fs.readFileSync(path, 'utf8');
  const json = JSON.parse(raw);
  const updated = transformStrings(json, transformText);
  fs.writeFileSync(path, JSON.stringify(updated, null, 2));
  const before = (raw.match(/\bSeven\b/g) || []).length;
  const after = (JSON.stringify(updated).match(/\bSeven\b/g) || []).length;
  return { path: path.split(/[\\/]/).pop(), before, after };
}

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
  if (fs.existsSync(f)) console.log(processFile(f));
  else console.log('(skip, not present):', f);
}

// Update the auto-scaffolded character stub
console.log('\n=== Character stub rewrite ===');
const stub = JSON.parse(fs.readFileSync(STUB, 'utf8'));
stub.name = 'Mira Krastev-Okonjo';
stub.aliases = ['Mira', '4471-K'];
stub.age = 11;
stub.gender = 'female';
stub.pronouns = 'she/her';
stub.role = 'Tissue-match subject extracted by Kyle Corbin-Vasik from a Ferrogate BioSystems secondary facility. Her designation 4471-K is a derivative of Kyle\'s own facility code (4471), marking her as the kidney-match — a backup harvest body grown in parallel to him seventeen years later. Cataloged human; documented legal identity preserved by paperwork while the body was rented to a clinical trial.';
stub.status = 'alive';
stub.location = 'Gray Zone safe room arranged through one of Kyle\'s contacts; transient';
stub.description = `Approximately eleven years old. Documented legal identity preserved — she was trafficked into the Ferrogate program as a cataloged human rather than an anonymous orphan, parents presumably paid or coerced into a Tier-3 clinical trial contract. The facility reduced her to tissue-match code 4471-K, a suffix-derivative of Kyle's own NDC-4471 designation — she is the kidney-match, the backup harvest, grown in parallel to him seventeen years after his own intake. Chrome leads at the base of her skull, fresh surgical wounds, NeoCortex array at 32,768 electrode density, array non-proprietary, subject integrity flagged compromised. Counts repeating patterns (rivets, tiles, rain impacts) as a self-regulation mechanism; stops at seventeen and starts over. Navigational instinct equal to Kyle's in the Ferrogate corridor geometry — she has been mapping the place with her eyes the same way he maps with his hardware. Extracted and delivered to a Gray Zone contact; current status safe, ongoing risk Tier 3 "when Axiom has time."`;
stub.relationships = [
  {
    name: 'Kyle Ellen Corbin-Vasik',
    type: 'protector',
    description: 'Kyle extracted her from the Ferrogate secondary facility after discovering her in a cell designated for Dr. Yuna Ferreira. He read her tissue-match code, 4471-K, and recognized his own designation (4471) in it. He arranged safe passage to a Gray Zone contact.',
    emotional_core: 'She is the first person in seventeen years to call Kyle by his designation without meaning it as property. Kyle is the first person to treat her designation as evidence of a crime rather than a filing system.',
    story_tension: 'Axiom recovery priority Tier 3 — inactive now, active when patrol rotations permit. Kyle has opened a file that cannot be closed.',
  },
  {
    name: 'Chen Wei-Lin',
    type: 'caretaker',
    description: 'Mrs. Chen, the noodle-stall operator, recognized what Mira was without being told. Called her xiao gui — little ghost — and fed her without asking questions.',
    emotional_core: 'Recognition of kind: Mrs. Chen has kept many of Kyle\'s secrets and adopts this one without ceremony.',
    story_tension: '',
  },
  {
    name: 'Ferrogate BioSystems',
    type: 'captor',
    description: 'The secondary facility that catalogued her as tissue-match code 4471-K. Asset valuation Φ0.00 current market, subject integrity compromised, array non-proprietary. Listed her as Axiom property on the active manifest of fourteen subjects.',
    emotional_core: '',
    story_tension: 'The file is not closed. The manifest persists.',
  },
];
stub.tags = ['auto-scaffolded', 'needs-review', 'extracted-subject', 'tissue-match'];
stub.physical_description = {
  heritage: 'Mixed — Eastern European and West African by surname combination (Krastev / Okonjo); pre-facility heritage preserved on birth registry',
  height_cm: 0,
  weight_kg: 0,
  build: 'small, undernourished, coiled',
  hair_color: '',
  hair_style: 'shaved to the scalp at the back for the surgical array; growing back in uneven patches',
  hair_length: 'short',
  eye_color: '',
  skin_tone: '',
  complexion: '',
  distinguishing_marks: [
    'NeoCortex array scar at base of skull — fresh, keloid-red',
    'chrome leads visible behind the left ear',
    'no personal jewelry, tattoos, or markings — the facility strips these on intake',
  ],
  visible_augmentations: 'NeoCortex array, fresh post-surgical',
  posture_movement: 'economical; tracks exits first; will not stand in the middle of a room',
  clothing_style: 'facility-issue gray smock at extraction; whatever Mrs. Chen has put on her since',
};
fs.writeFileSync(STUB, JSON.stringify(stub, null, 2));
console.log('Character stub renamed:', stub.name, '— aliases:', stub.aliases);

// Rebuild story.json markdown from the updated checkpoint
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
console.log('story.json markdown length:', md.length);
console.log('\nRemaining "Seven" in rebuilt markdown:', (md.match(/\bSeven\b/g) || []).length);
