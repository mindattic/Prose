// add_vehicle_tags.js
// Adds vehicle-type, propulsion, and domain tags to transportation entries.
// Tags are inferred from name, category, description, and existing fields.
// Does NOT remove existing tags. Does NOT add speculative tags.
//
// Usage: node add_vehicle_tags.js

const fs = require('fs');
const path = require('path');

const DIR = path.join(__dirname, '..', 'engine', 'data', 'transportation');

// ── Tag definitions ────────────────────────────────────────────────────────
const VEHICLE_TYPE_TAGS = [
  'car', 'truck', 'van', 'motorcycle', 'moped', 'bicycle',
  'boat', 'submarine', 'aircraft', 'helicopter', 'zeppelin', 'blimp',
  'drone', 'train', 'rail', 'maglev', 'ferry', 'hovercraft',
  'walker', 'exosuit', 'cargo', 'military', 'civilian', 'luxury', 'utility'
];
const PROPULSION_TAGS = ['electric', 'combustion', 'hybrid', 'solar', 'nuclear', 'sail', 'manual'];
const DOMAIN_TAGS = ['ground', 'aerial', 'aquatic', 'subterranean', 'amphibious', 'space'];

const ALL_VALID_TAGS = new Set([...VEHICLE_TYPE_TAGS, ...PROPULSION_TAGS, ...DOMAIN_TAGS]);

// ── Keyword → tag mapping ──────────────────────────────────────────────────
// Each entry: [regex pattern, tag to add, optional: only if another field matches]
const RULES = [
  // Vehicle types
  { re: /\b(sedan|coupe|compact|hatchback|cab\b|taxi|autocab|rideshare)\b/i, tag: 'car' },
  { re: /\b(truck|hauler|lorry|semi|dump truck|flatbed|tanker truck|pickup)\b/i, tag: 'truck' },
  { re: /\b(van|panel van|minivan|cargo van|transit)\b/i, tag: 'van' },
  { re: /\b(motorcycle|motorbike|enduro|scrambler|chopper|cruiser bike|street bike|dirt bike|bike\b)\b/i, tag: 'motorcycle' },
  { re: /\b(moped|scooter|e-scooter|motor scooter)\b/i, tag: 'moped' },
  { re: /\b(bicycle|bike|e-bike|cargo bike|velomobile|pedal)\b/i, tag: 'bicycle' },
  { re: /\b(boat|vessel|craft|skiff|launch|speedboat|patrol boat|gunboat|watercraft|inflatable)\b/i, tag: 'boat' },
  { re: /\b(submarine|submersible|sub\b|underwater vehicle|rov)\b/i, tag: 'submarine' },
  { re: /\b(aircraft|plane|jet|vtol|tiltrotor|fixed.wing|airframe)\b/i, tag: 'aircraft' },
  { re: /\b(helicopter|helo|rotorcraft|gyrocopter|autogyro)\b/i, tag: 'helicopter' },
  { re: /\b(zeppelin|rigid airship|airship)\b/i, tag: 'zeppelin' },
  { re: /\b(blimp|non.rigid airship|lighter.than.air)\b/i, tag: 'blimp' },
  { re: /\b(drone|uav|uas|unmanned aerial|quadcopter|hexacopter|octocopter|multirotor|aerial drone)\b/i, tag: 'drone' },
  { re: /\b(train|rail car|railcar|locomotive|metro|subway|l.train|l train|elevated rail|tram|streetcar|monorail)\b/i, tag: 'train' },
  { re: /\b(rail|railway|railroad|track)\b/i, tag: 'rail' },
  { re: /\b(maglev|magnetic levitation|magnetic.levit)\b/i, tag: 'maglev' },
  { re: /\b(ferry|catamaran|passenger vessel|water taxi|river taxi|lake liner)\b/i, tag: 'ferry' },
  { re: /\b(hovercraft|air cushion|skimmer)\b/i, tag: 'hovercraft' },
  { re: /\b(walker|mech|bipedal|quadrupedal walker|walking machine|strider)\b/i, tag: 'walker' },
  { re: /\b(exosuit|exoskeleton|exo.frame|exo.runner|powered suit|mechanized suit|hardsuit)\b/i, tag: 'exosuit' },
  { re: /\b(cargo|freight|hauling|logistics|transport)\b/i, tag: 'cargo' },
  { re: /\b(military|armed|armored|combat|tactical|weapons platform|warship|gunship|infantry support)\b/i, tag: 'military' },
  { re: /\b(civilian|commercial|public transit|commuter|passenger|civilian transport)\b/i, tag: 'civilian' },
  { re: /\b(luxury|executive|private|prestige|premium|high.end|vip)\b/i, tag: 'luxury' },
  { re: /\b(utility|service|maintenance|repair vehicle|work vehicle|support vehicle)\b/i, tag: 'utility' },

  // Propulsion
  { re: /\b(electric|battery|ev\b|e.motor|electromag|electric motor|electromagnetic rail|electric turbine|electric servo|electric drive)\b/i, tag: 'electric' },
  { re: /\b(combustion|gasoline|diesel|petrol|internal combustion|gas engine|fuel.injected|fossil fuel|kerosene|turbofan|turbojet|piston engine)\b/i, tag: 'combustion' },
  { re: /\b(hybrid|dual.fuel|combined electric|electric.combustion|mixed propulsion)\b/i, tag: 'hybrid' },
  { re: /\b(solar|photovoltaic|solar.assist|solar panel|solar.power)\b/i, tag: 'solar' },
  { re: /\b(nuclear|fission|fusion|reactor.powered|atomic)\b/i, tag: 'nuclear' },
  { re: /\b(sail|wind.power|wind.driven|kite sail)\b/i, tag: 'sail' },
  { re: /\b(manual|pedal.power|human.powered|self.propelled|leg.powered|muscle.power|rowing|paddled)\b/i, tag: 'manual' },

  // Domain
  { re: /\b(ground|road|street|surface|land vehicle|wheeled|tracked|off.road|pavement|tarmac|urban terrain|terrain)\b/i, tag: 'ground' },
  { re: /\b(aerial|air|airborne|fly|flight|altitude|airspace|flying|sky|airframe)\b/i, tag: 'aerial' },
  { re: /\b(aquatic|water|lake|river|ocean|sea|harbor|marine|maritime|nautical|harbor|coastal)\b/i, tag: 'aquatic' },
  { re: /\b(subterranean|underground|tunnel|below.ground|subway|deep rail|deep.level)\b/i, tag: 'subterranean' },
  { re: /\b(amphibious|land.and.water|sea.and.land|dual.mode|multi.terrain)\b/i, tag: 'amphibious' },
  { re: /\b(space|orbital|spacecraft|rocket|launch vehicle|interplanetary|orbit)\b/i, tag: 'space' },
];

// ── Category-based overrides ────────────────────────────────────────────────
const CATEGORY_TAGS = {
  'public_transit': ['civilian'],
  'underground': ['subterranean', 'rail', 'train'],
  'personal_ground': ['ground'],
  'aerial': ['aerial'],
  'aquatic': ['aquatic'],
  'military': ['military'],
  'luxury': ['luxury'],
  'cargo': ['cargo'],
  'drone': ['drone', 'aerial'],
  'walker': ['walker'],
  'exosuit': ['exosuit', 'ground'],
  'motorcycle': ['motorcycle', 'ground'],
  'bicycle': ['bicycle', 'ground', 'manual'],
  'moped': ['moped', 'ground'],
  'hovercraft': ['hovercraft', 'amphibious'],
  'submarine': ['submarine', 'aquatic'],
  'maglev': ['maglev', 'rail', 'electric'],
};

// ── Process a single file ──────────────────────────────────────────────────
function processFile(filePath) {
  const data = JSON.parse(fs.readFileSync(filePath, 'utf8'));

  const existingTags = new Set((data.tags || []).map(t => t.toLowerCase().trim()));
  const newTags = new Set();

  // Combine all text fields for matching
  const searchText = [
    data.name || '',
    data.description || '',
    data.category || '',
    data.propulsion || '',
    data.common_usage || '',
    data.armament || '',
    data.autonomy || '',
    (data.aliases || []).join(' '),
  ].join(' ');

  // Apply category-based tag overrides first
  const cat = (data.category || '').toLowerCase().trim();
  const catTags = CATEGORY_TAGS[cat] || [];
  for (const t of catTags) newTags.add(t);

  // Apply keyword rules
  for (const rule of RULES) {
    if (rule.re.test(searchText)) {
      newTags.add(rule.tag);
    }
  }

  // Filter to only valid tags not already present
  const tagsToAdd = [];
  for (const t of newTags) {
    const normalized = t.toLowerCase();
    if (!existingTags.has(normalized) && ALL_VALID_TAGS.has(normalized)) {
      tagsToAdd.push(normalized);
    }
  }

  if (tagsToAdd.length === 0) return { name: data.name, added: [] };

  data.tags = [...(data.tags || []), ...tagsToAdd];
  fs.writeFileSync(filePath, JSON.stringify(data, null, 2));
  return { name: data.name, added: tagsToAdd };
}

// ── Main ───────────────────────────────────────────────────────────────────
function main() {
  console.log('add_vehicle_tags.js — StreetSamurai transportation tag enrichment\n');

  const files = fs.readdirSync(DIR).filter(f => f.endsWith('.json'));
  console.log(`Found ${files.length} transportation files\n`);

  let modified = 0;
  let unchanged = 0;
  const tagCounts = {};

  for (const f of files) {
    const filePath = path.join(DIR, f);
    try {
      const result = processFile(filePath);
      if (result.added.length > 0) {
        modified++;
        for (const t of result.added) {
          tagCounts[t] = (tagCounts[t] || 0) + 1;
        }
        console.log(`  + [${result.added.join(', ')}]  ${result.name}`);
      } else {
        unchanged++;
      }
    } catch (e) {
      console.error(`  ERROR ${f}: ${e.message}`);
    }
  }

  console.log(`\n─────────────────────────────────────────`);
  console.log(`Files modified: ${modified}`);
  console.log(`Files unchanged: ${unchanged}`);
  console.log(`\nTag distribution (tags added):`);

  // Sort by count descending
  const sorted = Object.entries(tagCounts).sort((a, b) => b[1] - a[1]);
  for (const [tag, count] of sorted) {
    console.log(`  ${tag.padEnd(20)} ${count}`);
  }
}

main();
