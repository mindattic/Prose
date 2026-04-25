#!/usr/bin/env node
// Scan every place file for named people and diff against existing character entries.
// Emits a report of candidate names that appear in places but have no character file.

const fs = require('fs');
const path = require('path');

const REPO_ROOT = path.resolve(__dirname, '..', '..');
const PEOPLE_DIR = path.join(REPO_ROOT, 'engine', 'data', 'people');
const PLACES_DIR = path.join(REPO_ROOT, 'engine', 'data', 'places');

function loadJsonDir(dir) {
  const out = [];
  for (const entry of fs.readdirSync(dir, { withFileTypes: true })) {
    if (!entry.isFile() || !entry.name.endsWith('.json')) continue;
    const full = path.join(dir, entry.name);
    try {
      const j = JSON.parse(fs.readFileSync(full, 'utf8'));
      out.push({ file: full, data: j });
    } catch (e) {
      console.error(`Failed to parse ${full}: ${e.message}`);
    }
  }
  return out;
}

const people = loadJsonDir(PEOPLE_DIR);
const places = loadJsonDir(PLACES_DIR);

// Build name index: normalized full name -> canonical name
const nameIndex = new Map();
function addName(n) {
  if (!n || typeof n !== 'string') return;
  const key = n.trim().toLowerCase();
  if (!nameIndex.has(key)) nameIndex.set(key, n.trim());
}
for (const p of people) {
  addName(p.data.name);
  if (Array.isArray(p.data.aliases)) {
    for (const a of p.data.aliases) addName(a);
  }
}

// Also treat single first-name-only matches conservatively: store a set of first names
const firstNameIndex = new Set();
for (const k of nameIndex.keys()) {
  const firstToken = k.split(/\s+/)[0];
  firstNameIndex.add(firstToken);
}

// Regex for candidate names in free-text. Handles accents, hyphens, apostrophes.
// Patterns:
//   FirstName (Capitalized)
//   FirstName Lastname (with optional hyphenated chain)
//   Optional Last-Last-Last chain
const UPPER = "A-ZÀ-ÖØ-ÞŠŽŁĐĆČÐÑÕØÙÚÛÜÝŒÆẞ";
const LOWER = "a-zà-öø-ÿšžłđćčðñõøùúûüýÿœæßÕ";
const NAME_TOKEN = `[${UPPER}][${LOWER}${UPPER}'']{1,40}`;
const NAME_RE = new RegExp(
  `\\b(${NAME_TOKEN}(?:[- ](?:${NAME_TOKEN}))*)\\b`,
  'gu'
);

// A stoplist of common capitalized words / place terms / corp names that shouldn't count as people names.
const STOP = new Set([
  // Common English words that start sentences / proper nouns that aren't people
  'The','A','An','And','Or','But','For','Of','On','In','At','To','From','By','With','As','Is','Was','Are','Were','Be','Been','Being',
  'He','She','It','They','We','You','I','His','Her','Its','Their','Our','Your','My','Not','No','Yes','All','Some','Most','Many','Few','One','Two','Three','Four','Five','Six','Seven','Eight','Nine','Ten',
  'Tier','Building','Buildings','Floor','Level','North','South','East','West','Northern','Southern','Eastern','Western',
  'Monday','Tuesday','Wednesday','Thursday','Friday','Saturday','Sunday',
  'January','February','March','April','May','June','July','August','September','October','November','December',
  'GLMZ','Meridian','Meridian 88','Chicago','Illinois','Iowa','Michigan','Ohio','Indiana','Wisconsin','Minnesota','Kentucky','Missouri','Detroit','Cleveland','Milwaukee','Toledo','Rotterdam',
  'Spires','Circuit','Canopy','Loop','Gray','Zone','Brightmoor','Reclamation','Narrows','Shelf','Spine','Pale','Mile',
  'Arcturus','Axiom','Vossen','Tessera','Crucible','CRUCIBLE','Zheng','Zheng-Dao','Libation','Palladian','Ferrogate',
  'Lake','Michigan','Ontario','Huron','Erie','Superior','Mississippi','Ohio',
  'Ghost','Building','Ghost Building','Ghost Buildings','Pulse','Behemoth','Behemoths','Iowan',
  'Settling','Sponsorship','Program','Quanta','Φ',
  'Blazor','Server','Claude','Anthropic',
  'Street','Avenue','Boulevard','Road','Drive','Court','Plaza','Square','Park','Lane',
  'Ashland','Division','Halsted','Clark','Clybourn','Milwaukee','Diversey','Fullerton','Belmont','Addison','Irving','Lawrence','Foster','Bryn','Mawr',
  'Lincoln','Park','Pocket','Wicker','Bucktown','Pilsen','Bronzeville','Hyde','Austin','Edgewater','Rogers','Uptown','Logan','Ravenswood','Albany',
  'Al-Noor','Masjid','El','Elf','ELF','E.L.F.',
  'BCI','AI','API','CEO','CFO','COO','CTO','IT','HR','PR','AD','R&D','UI','UX','SDK','CLI',
  'CRISPR','DNA','RNA',
  'Nobody','Someone','Something','Anyone','Everyone','Everything','Anywhere','Everywhere','Nowhere','Somewhere',
  'Wallahi','Allah','God','Jesus','Christ','Buddha','Amen',
  'Fresh','Clean','Dark','Light','Old','New','Modern','Ancient','Classic',
  'About','After','Before','Between','During','Through','Until','While','Without','Within','Against','Toward','Into','Onto','Upon','Across',
  'This','That','These','Those','There','Then','Than','Thus','Also','Very','Just','Only','Even','Ever','Never','Always','Often','Sometimes','Usually','Rarely',
  'Mrs','Mr','Ms','Dr','Prof','Sr','Jr',
  'Co','Corp','Corporation','Company','Industries','Solutions','Group','Inc','LLC','Ltd','Systems','Services','Holdings','Partners','Associates',
  'Tessera-adjacent','Vantage',
  'Vantage Artisan Precision Hand','CrisisNet','BT-4','Battlefield Triage','Space Elevator Tether System',
  'Déjà Vu','Patchwork','Firefly','Ember-9','Ember','Triage','Vantage',
  'Phi','Theta','Alpha','Beta','Gamma','Delta','Sigma','Omega','Quantum','Qubit',
  'Prismatic','Consulting','Heliotrope','Data','Canopy','Strategic',
]);

const seen = new Map(); // normalized name -> { name, places: [{file, excerpt}] }
function textFields(obj, acc = []) {
  if (obj == null) return acc;
  if (typeof obj === 'string') { acc.push(obj); return acc; }
  if (Array.isArray(obj)) { for (const v of obj) textFields(v, acc); return acc; }
  if (typeof obj === 'object') {
    for (const k of Object.keys(obj)) {
      // Skip structural/id keys
      if (['id','type','tags','zone','territory','lat','lng','coordinates'].includes(k)) continue;
      textFields(obj[k], acc);
    }
  }
  return acc;
}

function looksLikePersonName(candidate) {
  // Must have at least one space (First Last) OR be a multi-hyphen chain that's obviously a person surname pattern.
  // Also reject anything in the stoplist, anything all-caps, anything with digits.
  if (/\d/.test(candidate)) return false;
  const tokens = candidate.split(/[ ]/);
  if (tokens.length < 2) return false; // require at least First Last
  for (const t of tokens) {
    if (STOP.has(t)) return false;
  }
  // Require the first token to be a reasonable first-name shape (not all caps)
  if (candidate === candidate.toUpperCase()) return false;
  // Heuristic: reject if any token is 1-2 chars (likely initials etc)
  if (tokens.some(t => t.replace(/-/g, '').length < 2)) return false;
  // Also reject if whole candidate happens to be in stoplist
  if (STOP.has(candidate)) return false;
  return true;
}

for (const pl of places) {
  const name = pl.data.name || path.basename(pl.file);
  const texts = textFields(pl.data);
  const combined = texts.join(' \n ');
  const matches = combined.matchAll(NAME_RE);
  for (const m of matches) {
    const cand = m[1];
    if (!looksLikePersonName(cand)) continue;
    const key = cand.trim().toLowerCase();
    if (nameIndex.has(key)) continue; // already a character
    if (!seen.has(key)) seen.set(key, { name: cand, places: [] });
    const entry = seen.get(key);
    if (entry.places.length < 3) {
      // capture short excerpt around the match
      const idx = combined.indexOf(cand);
      const start = Math.max(0, idx - 60);
      const end = Math.min(combined.length, idx + cand.length + 80);
      entry.places.push({ file: path.basename(pl.file), place: name, excerpt: combined.slice(start, end).replace(/\s+/g, ' ') });
    }
  }
}

// Sort by occurrence count then alphabetically
const results = Array.from(seen.values()).sort((a, b) => {
  if (b.places.length !== a.places.length) return b.places.length - a.places.length;
  return a.name.localeCompare(b.name);
});

console.log(`Existing character names: ${nameIndex.size}`);
console.log(`Place files scanned:      ${places.length}`);
console.log(`Unlinked candidate names: ${results.length}\n`);
for (const r of results) {
  console.log(`== ${r.name} (in ${r.places.length} place${r.places.length===1?'':'s'}) ==`);
  for (const p of r.places) {
    console.log(`  [${p.place}] ${p.excerpt}`);
  }
}

// Also write machine-readable JSON output
const outPath = path.join(__dirname, 'unlinked_people_report.json');
fs.writeFileSync(outPath, JSON.stringify(results, null, 2));
console.log(`\nReport written to ${outPath}`);
