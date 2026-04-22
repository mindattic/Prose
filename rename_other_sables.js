// Rename every other "Sable X" character to free up the "Sable" first name for the main
// fixer (ID 019d6143a6c07bbdb0144496b7f489a7, not in this list). New first names chosen
// to fit each character's surname heritage, gender, and role.
const fs = require('fs');
const path = require('path');
const PEOPLE = 'D:/Projects/MindAttic/StreetSamurai/engine/data/people';

// [fileId, oldFirst, oldFull, newFirst, newFull]
const renames = [
  ['019d6143a5c374ae8107f5d137a50ae7', 'Sable', 'Sable Vo',                             'Tuấn',    'Tuấn Vo'],
  ['019d6143a5d47ee2bd95a7ff6cec5a1a', 'Sable', 'Sable Árnason-Olofsson',               'Freyja',  'Freyja Árnason-Olofsson'],
  ['019d6143a5e075558a5d436c403ef4f7', 'Sable', 'Sable Chatterjee-Villalobos',          'Ari',     'Ari Chatterjee-Villalobos'],
  ['019d6143a5ec76f69fdaed955c84589f', 'Sable', 'Sable Benali',                         'Karim',   'Karim Benali'],
  ['019d6143a5ef794fbe2491587134eedf', 'Sable', 'Sable Odinga-Nakamura-Valenzuela',     'Jabari',  'Jabari Odinga-Nakamura-Valenzuela'],
  ['019d6143a5f77804917612f2e375164c', 'Sable', 'Sable Nygaard',                        'Magnus',  'Magnus Nygaard'],
  ['019d6143a61774b0a42f65c66d1053bd', 'Sable', 'Sable Najjar-Adeyemi',                 'Noor',    'Noor Najjar-Adeyemi'],
  ['019d6143a61c7b67b3daa0d23451a6de', 'Sable', 'Sable Fredriksen',                     'Aksel',   'Aksel Fredriksen'],
  ['019d6143a641740f96aa8d309db35b56', 'Sable', 'Sable Rentería',                       'Ixel',    'Ixel Rentería'],
  ['019d6143a6a074e7806c782d7f65f835', 'Sable', 'Sable Yoon',                           'Ji-woo',  'Ji-woo Yoon'],
  ['019d6143a6aa7baba48e92462cdceff7', 'Sable', 'Sable Song-Shirazi',                   'Minjun',  'Minjun Song-Shirazi'],
  ['019d6143a6d173bc966fb7de05376461', 'Sable', 'Sable Dalgaard',                       'Kerr',    'Kerr Dalgaard'],
  ['019d6143a6d870d78d4d390605099a17', 'Sable', 'Sable Ibarra-Nygaard-Vestergaard',     'Saskia',  'Saskia Ibarra-Nygaard-Vestergaard'],
  ['019d6143a6f27f5da85ae4aaf239b540', 'Sable', 'Sable Inoue',                          'Sumire',  'Sumire Inoue'],
  ['019d6143a739725db30f8e011c60e5ac', 'Sable', 'Sable Karunaratne-Adu',                'Nimal',   'Nimal Karunaratne-Adu'],
  ['4feff42c3d1c48a1ed1dee0f5f0eec18', 'Sable', 'Sable Keïta-Suzuki',                   'Amadou',  'Amadou Keïta-Suzuki'],
];

// Within a given target file, replace self-references to "Sable" (first name).
// We must only replace the first-name "Sable" when it refers to THIS character,
// never when it could refer to the main fixer. Heuristic:
//   1. Replace full-name occurrences first (unambiguous).
//   2. Then replace bare "Sable" tokens within THIS file only, since every "Sable"
//      that appears in this character's own file almost certainly refers to them
//      (the main fixer is documented in her own file, not mentioned by first name here).
function transformInOwnFile(text, oldFirst, oldFull, newFirst, newFull) {
  let out = text;
  out = out.split(oldFull).join(newFull);            // "Sable Nygaard" → "Magnus Nygaard"
  out = out.replace(/\bSable\b/g, newFirst);         // bare "Sable" → "Magnus"
  return out;
}

// Collect known full-name pairs so other-file cross-refs can be batch-updated.
const fullPairs = renames.map(([, , oldFull, , newFull]) => [oldFull, newFull]);

console.log('=== Phase 1: rename in target files ===');
for (const [id, oldFirst, oldFull, newFirst, newFull] of renames) {
  const p = path.join(PEOPLE, id + '.json');
  if (!fs.existsSync(p)) { console.log('skip (missing):', id); continue; }
  const before = fs.readFileSync(p, 'utf8');
  const after = transformInOwnFile(before, oldFirst, oldFull, newFirst, newFull);
  fs.writeFileSync(p, after);
  const residualSable = (after.match(/"Sable"/g) || []).length + (after.match(/"Sable /g) || []).length;
  console.log(`${id}  ${oldFull.padEnd(40)} → ${newFull}  (bare-Sable tokens: ${residualSable})`);
}

console.log('\n=== Phase 2: cross-reference full-name updates in all other files ===');
const allFiles = fs.readdirSync(PEOPLE).filter(f => f.endsWith('.json')).map(f => path.join(PEOPLE, f));
const targetIds = new Set(renames.map(r => r[0]));
const MAIN_SABLE_ID = '019d6143a6c07bbdb0144496b7f489a7';
let crossRefCount = 0;
for (const file of allFiles) {
  const id = path.basename(file, '.json');
  if (targetIds.has(id)) continue;
  let raw = fs.readFileSync(file, 'utf8');
  let changed = false;
  for (const [oldFull, newFull] of fullPairs) {
    if (raw.includes(oldFull)) {
      const n = (raw.match(new RegExp(oldFull.replace(/[.*+?^${}()|[\]\\]/g, '\\$&'), 'g')) || []).length;
      raw = raw.split(oldFull).join(newFull);
      console.log(`  ${path.basename(file)}: ${oldFull} → ${newFull} (${n} hit${n>1?'s':''})`);
      crossRefCount += n;
      changed = true;
    }
  }
  if (changed) fs.writeFileSync(file, raw);
}
console.log('Cross-reference updates:', crossRefCount);

// Sanity: confirm main Sable is untouched and still named "Sable"
const mainSable = JSON.parse(fs.readFileSync(path.join(PEOPLE, MAIN_SABLE_ID + '.json'), 'utf8'));
console.log('\nMain Sable name check:', mainSable.name, '(should be "Sable")');
