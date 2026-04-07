// Cyberlimbs generator for StreetSamurai
// Generates 70 cyberware JSON files in engine/data/cyberware/
// Run: node generate_cyberlimbs_2.js
// Does NOT overwrite existing files.

const fs = require('fs');
const https = require('https');
const path = require('path');

const settings = JSON.parse(fs.readFileSync(
  path.join(process.env.LOCALAPPDATA, 'MindAttic', 'StreetSamurai', 'Settings.json'), 'utf8'));
const API_KEY = settings.ApiKey;
const MODEL = 'claude-sonnet-4-6';
const OUTPUT_DIR = path.join(__dirname, '..', 'engine', 'data', 'cyberware');
const WAIT_MS = 5000;
const RETRY_WAIT_MS = 15000;
const sleep = ms => new Promise(r => setTimeout(r, ms));

if (!fs.existsSync(OUTPUT_DIR)) fs.mkdirSync(OUTPUT_DIR, { recursive: true });

function callClaude(system, user, maxTokens = 16000) {
  return new Promise((resolve, reject) => {
    const body = JSON.stringify({
      model: MODEL,
      max_tokens: maxTokens,
      temperature: 0.9,
      system: system,
      messages: [{ role: 'user', content: user }]
    });
    console.log(`    [API] Sending request (${body.length} bytes)...`);
    const req = https.request({
      hostname: 'api.anthropic.com',
      path: '/v1/messages',
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'x-api-key': API_KEY,
        'anthropic-version': '2023-06-01',
      },
      timeout: 300000,
    }, res => {
      let data = '';
      res.on('data', chunk => data += chunk);
      res.on('end', () => {
        console.log(`    [API] Response received (${data.length} bytes, status ${res.statusCode})`);
        try {
          const j = JSON.parse(data);
          if (j.content && j.content[0]) resolve(j.content[0].text);
          else reject(new Error(data.substring(0, 500)));
        } catch (e) { reject(e); }
      });
    });
    req.setTimeout(300000, () => { req.destroy(); reject(new Error('Request timeout after 300s')); });
    req.on('error', e => { console.error(`    [API] Request error: ${e.message}`); reject(e); });
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

  try {
    return JSON.parse(json);
  } catch (e) {
    json = json.replace(/,\s*([}\]])/g, '$1');
    json = json.replace(/(?<=": ")((?:[^"\\]|\\.)*)(?=")/gs, (m) => {
      return m.replace(/\n/g, '\\n').replace(/\r/g, '\\r').replace(/\t/g, '\\t');
    });
    try {
      return JSON.parse(json);
    } catch (e2) {
      const objects = [];
      let depth = 0;
      let objStart = -1;
      for (let i = 0; i < json.length; i++) {
        if (json[i] === '{' && depth === 0) { objStart = i; depth++; }
        else if (json[i] === '{') depth++;
        else if (json[i] === '}') {
          depth--;
          if (depth === 0 && objStart >= 0) {
            try {
              let objStr = json.substring(objStart, i + 1);
              objStr = objStr.replace(/,\s*([}\]])/g, '$1');
              objects.push(JSON.parse(objStr));
            } catch (e3) { /* skip malformed object */ }
            objStart = -1;
          }
        }
      }
      if (objects.length > 0) return objects;
      throw new Error(`No parseable JSON found: ${e.message}`);
    }
  }
}

function slugify(name, maxLen = 80) {
  let slug = name.toLowerCase()
    .replace(/[^a-z0-9]+/g, '_')
    .replace(/^_+|_+$/g, '');
  if (slug.length > maxLen) slug = slug.substring(0, maxLen).replace(/_+$/, '');
  return slug;
}

function generateId() {
  const bytes = [];
  for (let i = 0; i < 16; i++) bytes.push(Math.floor(Math.random() * 256));
  return bytes.map(b => b.toString(16).padStart(2, '0')).join('');
}

function saveItem(item) {
  const slug = slugify(item.name);
  const filePath = path.join(OUTPUT_DIR, `${slug}.json`);
  if (fs.existsSync(filePath)) {
    console.log(`    SKIP (exists): ${item.name}`);
    return false;
  }
  fs.writeFileSync(filePath, JSON.stringify(item, null, 2));
  return true;
}

function getExistingNames() {
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

// -- World Context --
const WORLD_CONTEXT = `Meridian 88, year 2200. Great Lakes megacity. Currency: \u03A6 (QUANTA). Tiers: 1 (Shelf/poorest) to 5 (Spire/elite). Ubiquitous Diaspora: fully interbred humanity, default to mixed heritage from unexpected global combinations. Iowan Behemoths are autonomous machines, NOT synthetic life.

Cyberlimb Manufacturers:
- CHROMEWORKS: Premium cyberlimbs, sleek chrome aesthetics, known for seamless neural integration. The "Apple" of limbs.
- IRONLIMB: Rugged industrial and military-grade limbs. Built to last, not to look pretty.
- MERIDIAN PROSTHETICS: The workhorse brand. Affordable, reliable, widely available. Hospital default.
- ARCTURUS DEFENSE SOLUTIONS: Military combat limbs, restricted distribution. Lethal hardware.
- LAZARUS PHARMACEUTICALS: Medical-grade prosthetics with biocompatible coatings and rejection-minimizing tech.
- CRUCIBLE: Boutique artisan cyberlimbs. Hand-built, expensive, each unit unique. Status symbols.
- STORMVEIN DYNAMICS: Aquatic and extreme-environment specialist. Waterproof, pressure-rated.
- KESSLER KINETICS: Performance sports and runner limbs. Speed-obsessed engineering.
- FENRIS INDUSTRIAL: Heavy labor and construction limbs. Overbuilt, modular, unglamorous.
- VESPER AESTHETICS: Fashion-forward cyberlimbs. Form over function. Popular with celebrities and tier 4-5 socialites.
- ZHENGDAO BIOELECTRIC: Eastern-tradition bioelectric integration, balancing organic and synthetic.`;

// -- Schema Instructions --
const SCHEMA_INSTRUCTIONS = `JSON schema (all fields required):
{ "id": "32-hex-chars", "name": "Manufacturer Model Name (max 60 chars)", "brand_name": "Short brand", "product_name": "Full product name", "type": "cyberware", "aliases": ["street names, 2-3"], "category": "CATEGORY_HERE", "body_location": "LOCATION_HERE", "description": "EXACTLY 2 short paragraphs separated by \\n\\n. P1: what it is and how it works technically. P2: who uses it and cultural context. KEEP EACH PARAGRAPH TO 3-4 SENTENCES MAX. Do NOT exceed 2 paragraphs.", "manufacturer": "FULL NAME IN CAPS", "tier_availability": "Tier X-Y", "legality": "Unrestricted/Licensed/Restricted/Military-Only/Prohibited", "installation_requirements": "brief", "rejection_risk": "Minimal/Low/Moderate/High + why", "maintenance": "brief", "specifications": "JSON string of tech specs object", "side_effects": ["2-4 items"], "cultural_context": "1 paragraph, 2-3 sentences", "known_users": ["3-5 demographics"], "story_hooks": ["2-3 RPG hooks"], "street_price": "amount in QUANTA", "licensed_price": "amount in QUANTA (1.5-3x street)", "tags": ["cyberware", "category_tag", ...] }

Rules:
- id = random 32 hex chars (unique per item)
- name <= 60 chars
- \u03A6 = QUANTA currency symbol (NOT Greek phi)
- specifications = stringified JSON object
- description = EXACTLY 2 SHORT paragraphs, no more
- side_effects must be an array of strings
- aliases must be an array of 2-3 street names`;

// -- Category Definitions --
const CATEGORIES = [
  {
    category: 'ocular',
    bodyLocation: 'eyes',
    count: 15,
    prompt: `Generate {count} CYBER EYE cyberware products. Mix of tactical, fashion, medical, and stealth variants.

Include a spread of:
- Tactical combat eyes with threat-tracking, IFF overlay, trajectory prediction
- Fashion eyes with color-shifting irises, holographic pupil patterns, bioluminescent sclera
- Medical eyes for surgeons (microscopic zoom, tissue analysis overlay, vein mapping)
- Stealth eyes with recording, facial recognition scrambling, low-light amplification
- A few hybrid models that cross categories (e.g., fashion eyes with hidden tactical features)

Manufacturers: CHROMEWORKS, ARCTURUS DEFENSE SOLUTIONS, LAZARUS PHARMACEUTICALS, VESPER AESTHETICS, TESSERA CORPONATION, plus SynapTech for consumer models.
Price range: \u03A64,000 - \u03A6180,000 depending on tier.
Legality varies: fashion=Unrestricted, medical=Licensed, tactical=Restricted, stealth=Restricted/Prohibited.`
  },
  {
    category: 'arm',
    bodyLocation: 'arm',
    count: 15,
    prompt: `Generate {count} CYBER ARM cyberware products. Full arm replacements or major augmentations.

Include a spread of:
- Combat arms with integrated blade housings, reinforced striking surfaces, recoil absorption
- Precision arms for surgeons, watchmakers, demolitions techs (sub-millimeter motor control)
- Strength arms for dockworkers, construction, heavy lifting (hydraulic-assisted)
- Stealth arms with hidden compartments, retractable tools, signal-dampened servos
- Luxury arms with designer chrome, synthetic skin options, gem inlays, artisan engraving

Manufacturers: CHROMEWORKS, IRONLIMB, MERIDIAN PROSTHETICS, ARCTURUS, CRUCIBLE, FENRIS INDUSTRIAL.
Price range: \u03A68,000 - \u03A6250,000 depending on tier.
Legality varies: civilian=Licensed, combat=Restricted, concealed weapons=Prohibited.`
  },
  {
    category: 'leg',
    bodyLocation: 'legs',
    count: 15,
    prompt: `Generate {count} CYBER LEG cyberware products. Full leg replacements or major augmentations, standard human-form knee configuration.

Include a spread of:
- Runner legs optimized for speed (carbon-composite, lightweight, energy-return)
- Combat legs with reinforced joints, magnetic boot interface, impact absorption
- Worker legs for standing 16-hour shifts (fatigue elimination, load distribution)
- Aquatic legs with hydrodynamic shaping, integrated swim fins, pressure compensation
- Stealth legs with vibration-dampened footfalls, thermal masking, silent servos

Manufacturers: CHROMEWORKS, IRONLIMB, KESSLER KINETICS, STORMVEIN DYNAMICS, MERIDIAN PROSTHETICS, FENRIS INDUSTRIAL.
Price range: \u03A610,000 - \u03A6200,000 depending on tier.
Legality: civilian=Licensed, combat=Restricted, aquatic=Licensed.`
  },
  {
    category: 'digitigrade_leg',
    bodyLocation: 'legs',
    count: 10,
    prompt: `Generate {count} DIGITIGRADE LEG cyberware products. These are backward-bending animal-style cybernetic legs (like a dog or cat's hind legs). The knee bends backward compared to human legs. They are a radical body modification.

IMPORTANT: Digitigrade legs carry SIGNIFICANT SOCIAL STIGMA. Most people find them unsettling or threatening. Wearers are often denied entry to businesses, face employment discrimination, and are associated with the fringe underground. Include this stigma in cultural_context.

Include a spread of:
- Sprint-optimized digitigrade legs (extreme acceleration, top speed 70+ km/h)
- Jump-optimized (5-8 meter vertical leap, parkour capability, rooftop runners)
- Climb-optimized (gecko-grip toe pads, wall-running capability, urban climbing)
- Heavy-load industrial (backward joint distributes weight differently, useful for specific lifting scenarios)
- Fashion/subculture digitigrade legs (the stigma IS the point, body-mod subculture embraces alienness)

Manufacturers: KESSLER KINETICS, CRUCIBLE (artisan), IRONLIMB, CHROMEWORKS (reluctantly), FENRIS INDUSTRIAL.
Price range: \u03A625,000 - \u03A6300,000 depending on tier.
Legality: Licensed to Restricted. Some jurisdictions ban them outright in public spaces.`
  },
  {
    category: 'auditory',
    bodyLocation: 'ears',
    count: 5,
    prompt: `Generate {count} HEARING IMPLANT cyberware products. Cochlear and auditory augmentations.

Include exactly these 5 types:
1. Directional hearing (isolate specific sound sources in crowded environments, 200m+ range)
2. Sonar/echolocation (ultrasonic pulse emitter + receiver, spatial mapping through sound)
3. Music-optimized (audiophile-grade frequency response, built-in DAC, bone-conduction sub-bass)
4. Tactical hearing (gunshot triangulation, footstep counting, whisper amplification, encrypted squad comms)
5. E.L.F.-tuned hearing (can perceive Emergent Lifeform communication frequencies, experimental, controversial)

Manufacturers: CHROMEWORKS, ARCTURUS DEFENSE SOLUTIONS, SynapTech, LAZARUS PHARMACEUTICALS, one experimental/unnamed lab.
Price range: \u03A65,000 - \u03A6120,000 depending on type.
Legality: consumer=Licensed, tactical=Restricted, E.L.F.-tuned=Prohibited (unauthorized E.L.F. contact is a felony).`
  },
  {
    category: 'misc',
    bodyLocation: 'varies',
    count: 10,
    prompt: `Generate {count} MISCELLANEOUS CYBERLIMB products that don't fit standard limb categories. Each should have a different body_location.

Include exactly these types:
1. Spine reinforcement (titanium-alloy vertebral replacement, load-bearing, anti-paralysis)
2. Spine reinforcement #2 (flexible graphene spinal sheath, impact absorption, posture correction)
3. Jaw replacement (reinforced mandible, bite force enhancement, hidden compartment in molar)
4. Hand replacement (just the hand, not full arm; precision grip, interchangeable fingertips)
5. Hand replacement #2 (heavy-duty industrial hand, crush-rated, magnetic palm)
6. Torso plating (internal chest/abdomen armor plating, organ shielding)
7. Torso plating #2 (lighter concealed variant, passes casual pat-down)
8. Dermal armor (full-body subdermal mesh, ballistic protection, visible geometric patterns under skin)
9. Dermal armor #2 (military-grade, heavier, obvious chrome plating sections)
10. Shoulder mount (reinforced shoulder joint for weapon mounting or heavy tool support)

Set body_location to: "spine", "spine", "jaw", "hand", "hand", "torso", "torso", "subdermal", "subdermal", "shoulder" respectively.
Set category to: "spine", "spine", "misc", "misc", "misc", "dermal", "dermal", "dermal", "dermal", "misc" respectively.

Manufacturers: IRONLIMB, CHROMEWORKS, MERIDIAN PROSTHETICS, ARCTURUS, FENRIS INDUSTRIAL, CRUCIBLE.
Price range: \u03A66,000 - \u03A6200,000 depending on type.
Legality varies: medical=Licensed, armor=Restricted, weapon mounts=Restricted.`
  },
];

// -- Main Generation Loop --
async function generateCategory(catDef) {
  const { category, bodyLocation, count, prompt } = catDef;

  console.log(`\n[${category}] Target: ${count} items.`);

  const BATCH = 5;
  let generated = 0;

  for (let i = 0; i < count; i += BATCH) {
    const batchSize = Math.min(BATCH, count - i);

    const allExisting = getExistingNames();
    const existingShort = allExisting.slice(-80).map(n => n.replace(/[^a-zA-Z0-9 ]/g, '').substring(0, 40));

    const system = `You generate cyberlimb and cyberware entries for the world of Meridian 88. Return ONLY a JSON array of exactly ${batchSize} cyberware objects. No explanation, no markdown fencing, just the raw JSON array.

${WORLD_CONTEXT}

${SCHEMA_INSTRUCTIONS}

CRITICAL: Keep descriptions to EXACTLY 2 SHORT paragraphs. Each paragraph should be 3-4 sentences max. Do NOT write long descriptions.`;

    const user = `${prompt.replace('{count}', String(batchSize))}

EXISTING NAMES (DO NOT DUPLICATE OR CREATE SIMILAR): ${existingShort.join(', ')}

Generate exactly ${batchSize} unique cyberware products. Each must have a distinctive manufacturer + model name (max 60 chars). Return ONLY the JSON array.`;

    console.log(`  Batch ${Math.floor(i / BATCH) + 1}: generating ${batchSize} items...`);

    let retries = 0;
    while (retries < 3) {
      try {
        const result = await callClaude(system, user);
        const items = parseJsonArray(result);

        let saved = 0;
        for (const item of items) {
          item.type = 'cyberware';

          // For misc category, trust the per-item category/body_location from the prompt
          if (category !== 'misc') {
            item.category = category;
            item.body_location = bodyLocation;
          }

          if (!item.id || !/^[0-9a-f]{32}$/.test(item.id)) {
            item.id = generateId();
          }

          if (item.name && item.name.length > 60) {
            item.name = item.name.substring(0, 60).trim();
          }

          if (item.specifications && typeof item.specifications === 'object') {
            item.specifications = JSON.stringify(item.specifications);
          }

          if (!Array.isArray(item.tags)) item.tags = [];
          if (!item.tags.includes('cyberware')) item.tags.unshift('cyberware');
          if (!item.tags.includes(item.category || category)) item.tags.push(item.category || category);

          if (saveItem(item)) {
            saved++;
            generated++;
          }
        }
        console.log(`    Saved ${saved}/${items.length} items.`);
        break;
      } catch (e) {
        retries++;
        console.error(`    Error (attempt ${retries}/3): ${e.message}`);
        if (retries < 3) {
          console.log(`    Retrying in ${RETRY_WAIT_MS / 1000}s...`);
          await sleep(RETRY_WAIT_MS * retries);
        }
      }
    }

    if (i + BATCH < count) {
      await sleep(WAIT_MS);
    }
  }

  console.log(`[${category}] Generated ${generated} new items.`);
  return generated;
}

async function main() {
  console.log('=== StreetSamurai Cyberlimbs Generator (Wave 2) ===');
  console.log(`Output: ${OUTPUT_DIR}`);
  const totalTarget = CATEGORIES.reduce((s, c) => s + c.count, 0);
  console.log(`Target: ${totalTarget} cyberlimb items across ${CATEGORIES.length} categories\n`);

  const existingFiles = fs.readdirSync(OUTPUT_DIR).filter(f => f.endsWith('.json'));
  console.log(`Existing cyberware files: ${existingFiles.length}`);

  let totalGenerated = 0;

  for (const catDef of CATEGORIES) {
    const n = await generateCategory(catDef);
    totalGenerated += n;
  }

  const finalFiles = fs.readdirSync(OUTPUT_DIR).filter(f => f.endsWith('.json'));
  console.log(`\n=== COMPLETE ===`);
  console.log(`New files created: ${totalGenerated}`);
  console.log(`Total cyberware files: ${finalFiles.length}`);
}

main().catch(e => { console.error('FATAL:', e); process.exit(1); });
