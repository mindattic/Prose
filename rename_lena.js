// Rename Kyra Krastev-Okonjo → Lena Connor across the story, the cast lists, and the
// character stub. Adjust heritage narrative so the surname Connor reads plausibly
// (Celtic-dominant mix per Ubiquitous Diaspora); everything else about her — age 19,
// 4471-K designation, untrained gunslinger, offscreen arc, personality — stays.
const fs = require('fs');
const path = require('path');

const STORY = 'D:/Projects/MindAttic/StreetSamurai/engine/data/stories/019db31fe8887c97a04965978b5ccdb3';
const STUB  = 'D:/Projects/MindAttic/StreetSamurai/engine/data/people/019db33aedd17097b813f9e28da1ba5f.json';

function transformText(s) {
  let out = s;
  // Full-name first so it takes precedence over the short-name regex below
  out = out.split('Kyra Krastev-Okonjo').join('Lena Connor');
  out = out.split('Krastev-Okonjo').join('Connor');
  out = out.replace(/\bKyra\b/g, 'Lena');
  return out;
}

function walk(obj) {
  if (typeof obj === 'string') return transformText(obj);
  if (Array.isArray(obj)) return obj.map(walk);
  if (obj && typeof obj === 'object') {
    const o = {};
    for (const k of Object.keys(obj)) o[k] = walk(obj[k]);
    return o;
  }
  return obj;
}

// --- Story files --------------------------------------------------------------
console.log('=== Story file rewrites ===');
const storyFiles = fs.readdirSync(STORY).filter(f => f.endsWith('.json')).map(f => path.join(STORY, f));
for (const f of storyFiles) {
  const raw = fs.readFileSync(f, 'utf8');
  const json = JSON.parse(raw);
  const updated = walk(json);
  fs.writeFileSync(f, JSON.stringify(updated, null, 2));
  const after = fs.readFileSync(f, 'utf8');
  const kyraLeft = (after.match(/\bKyra\b/g) || []).length;
  const koLeft = (after.match(/Krastev-Okonjo/g) || []).length;
  console.log(`  ${path.basename(f).padEnd(22)} Kyra=${kyraLeft} Krastev-Okonjo=${koLeft}`);
}

// --- Character stub ----------------------------------------------------------
console.log('\n=== Character stub ===');
const stub = JSON.parse(fs.readFileSync(STUB, 'utf8'));
const transformedStub = walk(stub);
// Also patch the aliases (they were ['Kyra', '4471-K'] — keep the code, rename the alias)
transformedStub.aliases = ['Lena', '4471-K'];

// Refresh heritage narrative so the Connor surname tracks. Keep mixed origin per
// Ubiquitous Diaspora, keep the "same facility genetic base as Kyle" line.
if (transformedStub.physical_description) {
  transformedStub.physical_description.heritage =
    "Mixed Celtic (Irish / Scottish) with Korean and Ghanaian layers — the Irish line provides the surname and the bone structure around the eyes, the Korean and Ghanaian layers account for the warm olive-undertone skin and the specific geometry around the jaw. The facility preserved her legal identity intact because the continuation program's audit trail required verifiable tissue-origin paperwork, so the surname and the ancestry survived nineteen years of captivity unmodified. Same facility genetic base as Kyle — his cheekbones and jaw line carry through.";
}

fs.writeFileSync(STUB, JSON.stringify(transformedStub, null, 2));
console.log(`  Name:    ${transformedStub.name}`);
console.log(`  Aliases: ${JSON.stringify(transformedStub.aliases)}`);
console.log(`  Age:     ${transformedStub.age}`);
const raw = fs.readFileSync(STUB, 'utf8');
console.log(`  Residual "Kyra":          ${(raw.match(/\bKyra\b/g) || []).length}`);
console.log(`  Residual "Krastev-Okonjo": ${(raw.match(/Krastev-Okonjo/g) || []).length}`);
console.log(`  "Lena" count:              ${(raw.match(/\bLena\b/g) || []).length}`);
console.log(`  "Connor" count:            ${(raw.match(/\bConnor\b/g) || []).length}`);

// --- Rebuild story.json markdown from updated checkpoint ---------------------
console.log('\n=== Rebuilding story.json markdown ===');
const cp = JSON.parse(fs.readFileSync(`${STORY}/checkpoint.json`, 'utf8'));
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
const md = lines.join('\n').replace(/\s+$/, '');
const st = JSON.parse(fs.readFileSync(`${STORY}/story.json`, 'utf8'));
st.html = md;
st.modified = new Date().toISOString();
fs.writeFileSync(`${STORY}/story.json`, JSON.stringify(st, null, 2));
console.log('  markdown length:', md.length);
console.log('  "Lena" in markdown:', (md.match(/\bLena\b/g) || []).length);
console.log('  "Connor" in markdown:', (md.match(/\bConnor\b/g) || []).length);
