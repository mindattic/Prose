// One-shot script: convert all metric measurements to American imperial in prose exports.
// Run: node tools/convert-to-imperial.js
// Then: ss --import-md for each UPDATED strand listed below.

const fs = require('fs');
const path = require('path');

const EXPORTS_DIR = path.join(__dirname, '..', 'engine', 'data', 'exports');

const SKIP_FILES = new Set(); // process all files including compiled books

// Ordered: longer/more-specific phrases first to avoid partial matches.
const REPLACEMENTS = [
  // ── Temperature ──────────────────────────────────────────────
  ['temperature seventeen Celsius', 'temperature sixty-three degrees'],
  ['4.2 degrees Celsius', 'forty degrees'],
  ['minus four degrees Celsius', 'twenty-five degrees'],
  ['minus four point one degrees C, exactly where', 'twenty-five degrees, exactly where'],
  ['minus four point one degrees C', 'twenty-five degrees'],
  ['Minus four point one. Holding', 'Twenty-five degrees. Holding'],

  // ── Weight ───────────────────────────────────────────────────
  ['forty kilograms of it comes across his lap', 'ninety pounds of it comes across his lap'],
  ['the hand on the saya is forty kilograms of Axiom', 'the hand on the saya is ninety pounds of Axiom'],

  // ── Volume ───────────────────────────────────────────────────
  ['three liters', 'three quarts'],

  // ── Millimeters ──────────────────────────────────────────────
  ['thirty millimeters distal from the standard cluster position', 'about an inch distal from the standard cluster position'],
  ['approximately fourteen millimeters', 'approximately half an inch'],
  ['a millimeter, the way a man settles', 'a hair, the way a man settles'],
  ['— a millimeter, nothing professional', '— a hair, nothing professional'],

  // ── Centimeters → inches / feet ──────────────────────────────
  ['a 102-centimeter draw', 'a forty-inch draw'],
  ['a few square centimeters of the dull grey weave underneath', 'a square inch of the dull grey weave underneath'],
  ['Kyle returned the bow to the centimeter', 'Kyle returned the bow to the inch'],
  ['two centimeters lateral of the radial', 'three-quarters of an inch lateral of the radial'],
  ['two centimeters of a face under streetlight', 'an inch of a face under streetlight'],
  ['The hood came back two centimeters', 'The hood came back an inch'],
  ['backs out by two centimeters', 'backs out by an inch'],
  ['approximately forty centimeters short of Kyle\'s position', 'approximately sixteen inches short of Kyle\'s position'],
  ['three centimeters from the katana\'s guard', 'an inch from the katana\'s guard'],
  ['sixty centimeters from snout to tail', 'two feet from snout to tail'],
  ['a radius of maybe forty centimeters', 'a radius of maybe sixteen inches'],

  // Pulse platform micro-drop
  ['unloaded half a centimeter at once', 'unloaded a quarter inch at once'],
  ['settled half a centimeter and lifted again', 'settled a quarter inch and lifted again'],

  // ── Meters → feet ────────────────────────────────────────────
  // Specific long phrases first
  ['Three meters of Axiom industrial hardware moving at', 'Ten feet of Axiom industrial hardware moving at'],
  ['three meters of Axiom-era industrial hardware and approximately', 'ten feet of Axiom-era industrial hardware and approximately'],
  ['three meters at full extension', 'ten feet at full extension'],
  ['ten-meter freight container', 'thirty-three-foot freight container'],
  ['six meters of fall into the storm sluice', 'twenty feet of fall into the storm sluice'],
  ['six meters up in a chum barrel', 'twenty feet up in a chum barrel'],
  ['a crocodile sleeping six meters away', 'a crocodile sleeping twenty feet away'],
  ['Fifteen meters of sidewalk', 'Fifty feet of sidewalk'],
  ['fifteen meters of street', 'fifty feet of street'],
  ['three by four meters', 'ten by thirteen feet'],
  ['three meters below the basement floor', 'ten feet below the basement floor'],
  ['twenty meters down the pavement', 'sixty-five feet down the pavement'],
  ['He read the six positions at twenty meters.', 'He read the six positions at sixty-five feet.'],
  ['Not confirmed at twenty meters — triangulated.', 'Not confirmed at sixty-five feet — triangulated.'],
  ['four meters of rain', 'thirteen feet of rain'],
  ['across two meters of rain', 'across six feet of rain'],
  ['two and a half meters from the assembly\'s improvised podium', 'eight feet from the assembly\'s improvised podium'],
  ['two and a half meters from the podium', 'eight feet from the podium'],
  ['forty meters of vertical city', 'a hundred and thirty feet of vertical city'],
  ['last forty meters to the alley behind The Pivot', 'last hundred and thirty feet to the alley behind The Pivot'],
  ['The corridor beyond the door ran forty meters of the same dead fluorescent', 'The corridor beyond the door ran a hundred and thirty feet of the same dead fluorescent'],
  ['Kyle is forty meters above', 'Kyle is a hundred and thirty feet above'],
  ['The loading dock is forty meters ahead', 'The loading dock is a hundred and thirty feet ahead'],
  ['forty meters from the server room', 'a hundred and thirty feet from the server room'],
  ['thirty meters further down', 'a hundred feet further down'],
  ['thirty meters from the alcove', 'a hundred feet from the alcove'],
  ['twenty meters from the server room', 'sixty-five feet from the server room'],
  ['distance of four meters', 'distance of thirteen feet'],
  ['twelve meters on momentum', 'forty feet on momentum'],
  ['three hundred meters ahead', 'a thousand feet ahead'],
  ['with each meter — the dead subject\'s certainty', 'with each foot — the dead subject\'s certainty'],

  // Generic meter phrases (after specific ones)
  ['three meters across', 'ten feet across'],
  ['twelve meters north', 'forty feet north'],
  ['Two meters of it.', 'Ten feet of it.'],
  ['two meters from the channel wall', 'six feet from the channel wall'],
  ['two meters wide', 'six feet wide'],
  ['three meters wide', 'ten feet wide'],
  ['the two meters between them', 'the six feet between them'],

  // ── Kilometers → miles ───────────────────────────────────────
  ['three kilometers east', 'two miles east'],
  ['one point two kilometers', 'three-quarters of a mile'],
  ['two kilometers east', 'a mile and a quarter east'],
  ['two kilometers away', 'a mile and a quarter away'],
  ['ten kilometers east', 'six miles east'],

  // ── 100-meter distances ──────────────────────────────────────
  ['hundred meters from the warehouse bay door', 'a hundred yards from the warehouse bay door'],

  // ── Compiled-book-only phrases (bushido_coda) ────────────────
  ['four centimeters, clean, deep enough to unstring', 'an inch and a half, clean, deep enough to unstring'],
  ['measured to the centimeter.', 'measured to the inch.'],
  ['depth governed to the millimeter', 'depth governed to a hair'],

  // ── Compiled-book-only phrases (glmz_stories_vol_1) ─────────
  ['280 kilometers per hour', '175 miles per hour'],
  ['200-meter-wide spine', '200-yard-wide spine'],
  ['preceded him by half a meter, that particular metallic sweetness', 'preceded him by a step, that particular metallic sweetness'],
  ['Then it rises six centimeters and waits.', 'Then it rises two inches and waits.'],
  ['Two meters overhead, barely audible', 'Six feet overhead, barely audible'],
  ['several hundred meters above and behind them', 'several hundred yards above and behind them'],
  ['descends four meters and holds.', 'descends twelve feet and holds.'],
  ['four meters, three, two, and simply hovers', 'twelve feet, eight, four, and simply hovers'],
  ['two meters away, matte gray', 'six feet away, matte gray'],
];

function processFile(filename) {
  const filepath = path.join(EXPORTS_DIR, filename);
  if (!fs.existsSync(filepath)) {
    console.log(`  skip (missing): ${filename}`);
    return;
  }

  let content = fs.readFileSync(filepath, 'utf8');
  const original = content;

  for (const [from, to] of REPLACEMENTS) {
    if (content.includes(from)) {
      content = content.split(from).join(to);
    }
  }

  if (content !== original) {
    fs.writeFileSync(filepath, content, 'utf8');
    console.log(`  UPDATED: ${filename}`);
  }
}

const allFiles = fs.readdirSync(EXPORTS_DIR)
  .filter(f => f.endsWith('.numbered.md') && !SKIP_FILES.has(f));

console.log(`Processing ${allFiles.length} strand files...\n`);
allFiles.forEach(processFile);
console.log('\nDone. Import changed strands with: ss --import-md --slug <slug>');
