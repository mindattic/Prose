// Spray weapons & electric shock rod generator for StreetSamurai
// Generates 40 weapon JSON files in engine/data/weaponry/
// Run: node generate_sprays_and_rods.js
// Does NOT overwrite existing files.

const fs = require('fs');
const https = require('https');
const path = require('path');

const settings = JSON.parse(fs.readFileSync(
  path.join(process.env.LOCALAPPDATA, 'MindAttic', 'StreetSamurai', 'Settings.json'), 'utf8'));
const API_KEY = settings.ApiKey;
const MODEL = 'claude-sonnet-4-6';
const OUTPUT_DIR = path.join(__dirname, '..', 'engine', 'data', 'weaponry');
const WAIT_MS = 3000;
const sleep = ms => new Promise(r => setTimeout(r, ms));

if (!fs.existsSync(OUTPUT_DIR)) fs.mkdirSync(OUTPUT_DIR, { recursive: true });

function callClaude(system, user, maxTokens = 16384) {
  return new Promise((resolve, reject) => {
    const body = JSON.stringify({
      model: MODEL,
      max_tokens: maxTokens,
      temperature: 0.9,
      system: system,
      messages: [{ role: 'user', content: user }]
    });
    const req = https.request({
      hostname: 'api.anthropic.com',
      path: '/v1/messages',
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'x-api-key': API_KEY,
        'anthropic-version': '2023-06-01',
      }
    }, res => {
      let data = '';
      res.on('data', chunk => data += chunk);
      res.on('end', () => {
        try {
          const j = JSON.parse(data);
          if (j.content && j.content[0]) resolve(j.content[0].text);
          else reject(new Error(data.substring(0, 500)));
        } catch (e) { reject(e); }
      });
    });
    req.on('error', reject);
    req.write(body);
    req.end();
  });
}

function parseJsonArray(text) {
  let json = text.trim();
  if (json.startsWith('```')) {
    json = json.substring(json.indexOf('\n') + 1);
    if (json.endsWith('```')) json = json.slice(0, -3);
    json = json.trim();
  }
  const start = json.indexOf('[');
  const end = json.lastIndexOf(']');
  if (start === -1 || end === -1) throw new Error('No JSON array found in response');
  json = json.substring(start, end + 1);
  return JSON.parse(json);
}

function slugify(name) {
  return name.toLowerCase()
    .replace(/[^a-z0-9]+/g, '_')
    .replace(/^_+|_+$/g, '')
    .slice(0, 80);
}

function saveWeapon(weapon) {
  const slug = slugify(weapon.name);
  const filePath = path.join(OUTPUT_DIR, `${slug}.json`);
  if (fs.existsSync(filePath)) {
    console.log(`    SKIP (exists): ${weapon.name}`);
    return false;
  }
  fs.writeFileSync(filePath, JSON.stringify(weapon, null, 2));
  console.log(`    SAVED: ${weapon.name} -> ${slug}.json`);
  return true;
}

function getExistingWeaponNames() {
  const files = fs.readdirSync(OUTPUT_DIR).filter(f => f.endsWith('.json'));
  const names = [];
  for (const f of files) {
    try {
      const data = JSON.parse(fs.readFileSync(path.join(OUTPUT_DIR, f), 'utf8'));
      if (data.name) names.push(data.name);
    } catch (e) { /* skip bad files */ }
  }
  return names;
}

// ── World Context ──
const WORLD_CONTEXT = `Setting: GLMZ, year 2200. A megacity in the Great Lakes corridor (Chicago-Milwaukee). Currency is QUANTA, symbol Φ. Society is tiered: Tier 1 (the Shelf — poorest, most dangerous), Tier 2 (working class), Tier 3 (middle), Tier 4 (corporate comfort), Tier 5 (the Spire — ultra-elite).

Technology: BCI (brain-computer interfaces) are common. Augmentation (cyberware/chrome) ranges from basic to military-grade. Geneware allows cosmetic and functional genetic modification. Synthetics (artificial humans) exist but are socially marginalized.

CorpoNations are sovereign corporate entities — Arcturus Defense Solutions (military/security), TESSERA CorpoNation (biotech/consumer tech), Lazarus Pharmaceutical (pharma/bioweapons). Consumer brands also exist: SafeStep, Guardian, ShieldTech, Meridian Municipal Supply, etc.

Products should feel REAL — not parodies. Think how real defense contractors (Taser/Axon, Safariland, Defense Technology) name their products. Professional, sometimes clinical, sometimes evocative.

Ubiquitous Diaspora: By 2200, humanity is fully racially interbred. Default to mixed heritage from unexpected global combinations.`;

// ── Weapon Schema Template ──
const SCHEMA_TEMPLATE = `Each weapon MUST be a JSON object with EXACTLY these fields:
{
  "id": "<32-char lowercase hex string, unique per weapon>",
  "name": "Product Name (60 chars max)",
  "type": "weapon",
  "aliases": ["alias1", "alias2"],
  "category": "<CATEGORY>",
  "description": "2-3 paragraphs with world flavor. Describe the weapon, how it works, its reputation, and any controversy.",
  "manufacturer": "MANUFACTURER NAME IN CAPS",
  "tier_availability": "Tier N+",
  "legality": "Legal/Licensed/Restricted/Prohibited — brief reason",
  "base_technologies": ["tech1", "tech2", "tech3"],
  "specifications": "key: value\\nkey: value (multi-line specs string)",
  "tactical_use": "1 paragraph on how it is used in practice",
  "cultural_context": "1 paragraph on cultural perception, street reputation, slang",
  "known_users": ["user group 1", "user group 2"],
  "story_hooks": ["A specific narrative hook that could drive a story beat"],
  "ammunition_type": [],
  "tags": ["weapon", "<category>", "<lethality_tag>", "other relevant tags"]
}

CRITICAL: The "id" field must be a 32-character lowercase hexadecimal string (0-9, a-f only). Generate a unique one for each weapon.
CRITICAL: "name" must be 60 characters or fewer.
CRITICAL: Lethality tags — use exactly one of: "non_lethal", "less_lethal", or "lethal" as appropriate.`;

// ── Batch Definitions ──
const BATCHES = [
  {
    label: 'Spray Weapons (Batch 1 of 2)',
    count: 10,
    category: 'spray',
    prompt: `Generate exactly 10 SPRAY WEAPONS (chemical/aerosol) for GLMZ. These are the first 10 of 20 total sprays.

Include in this batch:
1. A cheap Tier 1 "Shelf" pepper spray — the cheapest self-defense option, sold in vending machines
2. A mid-tier OC/CS gas spray — standard security issue
3. A premium OC spray — fast-acting, minimal blowback
4. A BCI-disrupting chemical spray — interferes with neural implants on skin contact, causes BCI glitches/static
5. A cyberware-corroding spray — attacks augmentation seals and exposed chrome joints, causes oxidation
6. A UV marking spray — tags attackers with invisible dye for later identification by security systems
7. An incapacitating nerve agent spray — restricted corporate security weapon, causes instant motor paralysis
8. An adhesive spray — rapid-curing polymer that glues attackers to surfaces
9. A hallucinogenic spray — causes 30-60 seconds of confusion and disorientation, synth-compound based
10. A cryo spray — flash-freeze contact agent, causes intense pain and numbness on exposed skin

Mix manufacturers: SafeStep (consumer), Guardian (consumer), ARCTURUS DEFENSE SOLUTIONS (military), TESSERA CorpoNation (tech), LAZARUS PHARMACEUTICAL (pharma), ShieldTech (security), plus 1-2 street/unlicensed brands.

Price range: Φ5 (cheapest) to Φ2,500 (military nerve agent).
Tier range: Tier 1 through Tier 4.

Lethality guidelines:
- Pure pepper sprays, marking sprays = "non_lethal"
- Incapacitating sprays, cryo, adhesive, hallucinogenic = "less_lethal"
- Nerve agent = "less_lethal" (primary purpose is incapacitation, but tag description should note lethality risk)
- BCI/cyberware-disrupting = "less_lethal"`
  },
  {
    label: 'Spray Weapons (Batch 2 of 2)',
    count: 10,
    category: 'spray',
    prompt: `Generate exactly 10 MORE SPRAY WEAPONS (chemical/aerosol) for GLMZ. These are the second 10 of 20 total sprays.

Include in this batch:
1. A nausea-inducing spray — triggers extreme vomiting within seconds, crowd dispersal tool
2. A sensory overload spray — activates pain receptors across exposed skin, "liquid agony"
3. An anti-augmented-animal spray — designed for encounters with gene-modded or cybered predators
4. A mood-suppressing spray — Lazarus pharmaceutical compound that chemically suppresses aggression/rage
5. A high-end executive personal defense spray — sleek, expensive, multi-mode (OC + marking + BCI disruption)
6. A street-improvised chemical spray — homebrewed from industrial chemicals, unreliable but devastating
7. A tear gas canister spray (handheld) — municipal security standard, area denial
8. A dermal irritant spray — causes intense skin inflammation, non-lethal crowd control
9. A phobic-response spray — triggers irrational fear response through synthetic pheromone compounds
10. An anti-synthetic spray — designed to damage synthetic skin/sensory arrays on artificial humans

Mix manufacturers: LAZARUS PHARMACEUTICAL, ARCTURUS DEFENSE SOLUTIONS, Meridian Municipal Supply, street/improvised, Guardian, plus others.

Price range: Φ8 to Φ4,000.
Tier range: Tier 1 through Tier 4.

Lethality guidelines:
- Nausea, dermal irritant, tear gas = "less_lethal"
- Sensory overload, phobic-response = "less_lethal"
- Mood-suppressing = "non_lethal"
- Anti-animal, anti-synthetic = "less_lethal"
- Street improvised = "less_lethal" (but description should note unpredictable lethality risk)
- Executive multi-mode = "less_lethal"`
  },
  {
    label: 'Electric Shock Rods (Batch 1 of 2)',
    count: 10,
    category: 'melee',
    prompt: `Generate exactly 10 ELECTRIC SHOCK RODS/BATONS for GLMZ. These are the first 10 of 20 total.

Include in this batch:
1. A basic security stun baton — standard issue for building security, affordable
2. An extendable/telescoping shock stick — compact carry, extends for use
3. A cattle-prod style device — agricultural tool repurposed for self-defense, crude but effective
4. A neural disruption baton — scrambles BCI signals on contact, causes temporary neural static
5. A variable-voltage rod — adjustable from mild deterrent to cardiac-arrest levels
6. A dual-mode baton — impact weapon + electric discharge, switchable
7. A shock tonfa — martial arts style L-shaped baton with integrated capacitor
8. An arc whip — flexible cable/chain weapon that delivers electrical discharge along its length
9. A shock lance — long-reach polearm-style weapon with electrified tip
10. A micro stun rod — pen-sized concealed weapon, single-use or limited charges

Mix manufacturers: ARCTURUS DEFENSE SOLUTIONS (military-grade), ShieldTech (security), Guardian (consumer), TESSERA CorpoNation (tech), street/improvised brands, agricultural equipment companies.

Price range: Φ15 (repurposed cattle prod) to Φ8,000 (military shock lance).
Tier range: Tier 1 through Tier 4.

Lethality guidelines:
- Basic stun baton, micro stun = "less_lethal"
- Variable-voltage on max setting = "less_lethal" (note lethal capability in description)
- Neural disruption = "less_lethal"
- All others = "less_lethal"
- Cattle prod = "less_lethal"`
  },
  {
    label: 'Electric Shock Rods (Batch 2 of 2)',
    count: 10,
    category: 'melee',
    prompt: `Generate exactly 10 MORE ELECTRIC SHOCK RODS/BATONS for GLMZ. These are the second 10 of 20 total.

Include in this batch:
1. A heavy shock maul — two-handed weapon, massive electrical discharge, anti-vehicle capable
2. A chain-lightning baton — discharge arcs between multiple nearby targets
3. An EMP baton — fries electronics and cyberware on contact, no physical shock but devastating to augmented targets
4. A plasma-edge shock sword — high-end melee weapon with superheated plasma edge + electrical discharge
5. A security riot baton — reinforced for crowd control, moderate shock, emphasis on durability
6. A concealed cane stun weapon — disguised as a walking cane, popular with older Tier 3-4 residents
7. A shock knuckleduster — brass knuckles with integrated micro-capacitors
8. A dual-baton set — paired short batons with synchronized discharge
9. A street-improvised shock club — car battery + pipe, dangerous to user and target alike
10. A corporate executive defense rod — sleek, expensive, concealed in a briefcase or umbrella

Mix manufacturers: ARCTURUS DEFENSE SOLUTIONS, TESSERA CorpoNation, ShieldTech, Crucible Industries, street/improvised, luxury brands.

Price range: Φ20 (improvised) to Φ25,000 (plasma-edge sword).
Tier range: Tier 1 through Tier 5.

Lethality guidelines:
- Heavy shock maul = "less_lethal" (note lethal capability)
- Chain-lightning = "less_lethal"
- EMP baton = "less_lethal" (non-lethal to person but devastating to cyberware)
- Plasma-edge = "lethal"
- Improvised = "less_lethal" (note danger to user)
- All others = "less_lethal"`
  }
];

// ── Main ──
async function main() {
  const existingNames = getExistingWeaponNames();
  console.log(`Found ${existingNames.length} existing weapons in ${OUTPUT_DIR}`);

  const existingList = existingNames.slice(-50).map(n => `- ${n}`).join('\n');

  let totalSaved = 0;
  let totalSkipped = 0;

  for (const batch of BATCHES) {
    console.log(`\n── ${batch.label} ──`);

    const systemPrompt = `${WORLD_CONTEXT}

You are a world-building assistant generating weapons for a near-future megacity setting. Generate EXACTLY ${batch.count} weapons as a JSON array.

${SCHEMA_TEMPLATE.replace('<CATEGORY>', batch.category)}

IMPORTANT: Do NOT duplicate any of these existing weapons:
${existingList}

Return ONLY a JSON array of ${batch.count} weapon objects. No commentary, no markdown fences.`;

    let attempts = 0;
    let weapons = null;

    while (attempts < 3 && !weapons) {
      attempts++;
      try {
        console.log(`  Calling Claude (attempt ${attempts})...`);
        const raw = await callClaude(systemPrompt, batch.prompt);
        weapons = parseJsonArray(raw);
        console.log(`  Received ${weapons.length} weapons`);
      } catch (err) {
        console.error(`  Error: ${err.message}`);
        if (attempts < 3) {
          console.log(`  Retrying in ${WAIT_MS}ms...`);
          await sleep(WAIT_MS);
        }
      }
    }

    if (!weapons) {
      console.error(`  FAILED after 3 attempts for: ${batch.label}`);
      continue;
    }

    for (const weapon of weapons) {
      // Enforce name length
      if (weapon.name && weapon.name.length > 60) {
        weapon.name = weapon.name.slice(0, 60).trim();
      }
      // Enforce type
      weapon.type = 'weapon';
      // Enforce category
      if (batch.category === 'spray') {
        weapon.category = 'spray';
      }
      // Ensure ammunition_type is array
      if (!Array.isArray(weapon.ammunition_type)) {
        weapon.ammunition_type = [];
      }

      if (saveWeapon(weapon)) {
        totalSaved++;
      } else {
        totalSkipped++;
      }
    }

    console.log(`  Batch complete. Waiting ${WAIT_MS}ms...`);
    await sleep(WAIT_MS);
  }

  console.log(`\n── COMPLETE ──`);
  console.log(`  Saved: ${totalSaved}`);
  console.log(`  Skipped: ${totalSkipped}`);
  console.log(`  Total weaponry files: ${fs.readdirSync(OUTPUT_DIR).filter(f => f.endsWith('.json')).length}`);
}

main().catch(err => {
  console.error('Fatal error:', err);
  process.exit(1);
});
