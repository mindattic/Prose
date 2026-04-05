// Substrate/material generator for StreetSamurai
// Generates 100 substrate JSON files in engine_data/substrates/
// Run: node generate_substrates.js
// Does NOT overwrite existing files.

const fs = require('fs');
const https = require('https');
const path = require('path');

const settings = JSON.parse(fs.readFileSync(
  path.join(process.env.LOCALAPPDATA, 'MindAttic', 'StreetSamurai', 'Settings.json'), 'utf8'));
const API_KEY = settings.ApiKey;
const MODEL = 'claude-sonnet-4-6';
const ENGINE_DATA = path.join(__dirname, '..', 'engine_data');
const OUTPUT_DIR = path.join(ENGINE_DATA, 'substrates');
const WAIT_MS = 3000;
const sleep = ms => new Promise(r => setTimeout(r, ms));

if (!fs.existsSync(OUTPUT_DIR)) fs.mkdirSync(OUTPUT_DIR, { recursive: true });

function callClaude(system, user, maxTokens = 8192) {
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
    .replace(/^_+|_+$/g, '');
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

function getExistingByCategory() {
  const files = fs.readdirSync(OUTPUT_DIR).filter(f => f.endsWith('.json'));
  const byCat = {};
  for (const f of files) {
    try {
      const data = JSON.parse(fs.readFileSync(path.join(OUTPUT_DIR, f), 'utf8'));
      const cat = data.category || 'unknown';
      if (!byCat[cat]) byCat[cat] = [];
      byCat[cat].push(data.name);
    } catch (e) { /* skip */ }
  }
  return byCat;
}

// ── World Context ──
const WORLD_CONTEXT = `Setting: Meridian 88, year 2200. 200 years of materials science advancement. SNT (Synthetic Neural Tissue) — living neural matter that bridges organic and synthetic systems — has enabled entirely new categories of materials: bio-metal hybrids, living composites, neural-responsive substrates.

Quantum-level manufacturing is available at Tier 4-5, enabling exotic alloys and materials impossible with conventional processes. Nano-assembly is routine. Self-healing and programmable matter exist but remain expensive.

Major materials corponations: Crucible Industries (heavy metals and alloys), Tessera Materials (composites and smart materials), BioForge Labs (bio-hybrid substrates, SNT-derived materials), Apex Nanofabrication (nano-materials), Veilweave Textiles (advanced fabrics). Street-level fabricators work with salvage and knockoff formulations.

Currency is Phi (Φ). Society is tiered: Tier 1 (the Shelf — poorest) to Tier 5 (the Spire — ultra-elite). Materials availability varies dramatically by tier.`;

// ── Category Definitions ──
const CATEGORIES = [
  {
    category: 'nano_material',
    count: 15,
    prompt: `Generate {count} carbon nanotube variant materials for Meridian 88, year 2200. 200 years of CNT development has produced wildly diverse variants. Include: ultra-high tensile strength cables, flexible CNT meshes for armor, CNT-organic hybrids that bond with living tissue, conductive CNT weaves for electronics, CNT foam for insulation and impact absorption, transparent CNT sheets for displays, magnetic CNT composites, CNT-ceramic hybrids, and specialized variants for augment/cyberware construction. Each should have a distinct trade name and manufacturer. These are the steel and aluminum of 2200 — foundational materials everything is built from.`
  },
  {
    category: 'composite',
    count: 10,
    prompt: `Generate {count} graphene composite materials for Meridian 88. Graphene has had 200 years of development. Include: graphene-reinforced structural panels, flexible graphene electronics substrates, graphene thermal management layers, graphene filtration membranes (water purification in the Shelf), graphene-polymer armor laminates, graphene supercapacitor materials, and hybrid graphene-CNT composites. Each with distinct trade names, applications, and tier availability.`
  },
  {
    category: 'bio_hybrid',
    count: 15,
    prompt: `Generate {count} bio-metal hybrid materials for Meridian 88. These are the revolutionary materials enabled by SNT and biotech — living metal, organic steel, tissue-bonded ceramics. Include: metals that heal like skin when damaged, ceramics that bond permanently with bone and grow with the body, alloys with embedded living neural pathways (they can transmit signals), bio-steel that strengthens in response to stress (like muscle), organic titanium that the immune system doesn't reject (critical for augments), living armor that repairs itself using the wearer's metabolic energy, and composite materials that blur the line between organism and material. These materials are WHY augments work as well as they do in 2200.`
  },
  {
    category: 'smart_material',
    count: 15,
    prompt: `Generate {count} smart materials for Meridian 88. Materials that respond, adapt, and change. Include: shape-memory alloys that return to programmed forms when triggered, self-healing polymers that seal cracks and cuts automatically, programmable matter that can change shape on command (expensive, Tier 4-5), thermochromic materials that change color with temperature, piezoelectric substrates that generate power from movement, materials with programmable stiffness (rigid or flexible on command), acoustically active materials that can dampen or amplify sound, and phase-change materials for thermal regulation.`
  },
  {
    category: 'exotic',
    count: 10,
    prompt: `Generate {count} exotic alloy materials for Meridian 88. Materials only possible with quantum-level manufacturing — atomic-precision assembly that arranges matter atom by atom. Include: zero-expansion alloys (no thermal expansion at any temperature), metamaterials with negative refractive index (bending light around objects), superconducting alloys that work at room temperature, ultra-dense materials (small volume, enormous mass — used in counterweights and armor), perfectly elastic metals (100% energy return), and alloys with quantum properties exploitable at macro scale. These are Tier 4-5, extremely expensive, and represent the bleeding edge.`
  },
  {
    category: 'ceramic',
    count: 10,
    prompt: `Generate {count} ceramic and armor materials for Meridian 88. Include: transparent ceramics harder than diamond (used in windows and visors), reactive armor ceramics that ablate incoming kinetic energy, thermal ceramics for re-entry and extreme heat applications, bio-compatible ceramics for augment housings, piezoelectric ceramics for power generation, structural ceramics that replace steel in construction, and layered ceramic-composite armor systems. Each with trade names and specific applications.`
  },
  {
    category: 'textile',
    count: 10,
    prompt: `Generate {count} textile and fabric materials for Meridian 88. Include: ballistic cloth that stops projectiles while remaining flexible, chameleon fabric that changes color and pattern on command (BCI-linked for thought-controlled outfit changes), thermal-reactive clothing material that adjusts insulation based on temperature, self-cleaning fabrics, conductive thread for wearable electronics, radiation-shielding textiles (for Shelf areas near old reactors), stealth fabric that absorbs radar and IR, slash-resistant weaves, and smart-compression textiles for medical and athletic use.`
  },
  {
    category: 'organic',
    count: 10,
    prompt: `Generate {count} organic substrate materials for Meridian 88. Grown, not manufactured. Include: fungal mycelium composites stronger than concrete, coral-derived structural calcium matrices, bacterial cellulose sheets with industrial strength, algae-based bioplastics, wood-analog grown in vats (since real forests are scarce), chitin-based armor plates grown from engineered insects, silk variants from modified organisms (spider silk cables), living building materials that photosynthesize and produce oxygen, and root-network structural foundations. These are cheaper and more sustainable than synthetic materials, popular in Tier 2-3 construction.`
  },
  {
    category: 'metal',
    count: 5,
    prompt: `Generate {count} energy materials for Meridian 88. Materials specifically designed for power storage, generation, and transmission. Include: room-temperature superconductor cables, ultra-dense battery substrate materials (100x the energy density of 2024 lithium), thermoelectric conversion materials (turn any heat differential into power), wireless power transmission materials, and quantum-dot power storage matrices. These materials underpin M88's power infrastructure.`
  },
];

// ── Main Generation Loop ──
async function generateCategory(catDef) {
  const { category, count, prompt } = catDef;

  const existingByCat = getExistingByCategory();
  const existingInCat = existingByCat[category] || [];
  const needed = count - existingInCat.length;

  if (needed <= 0) {
    console.log(`[${category}] Already have ${existingInCat.length}/${count}. Skipping.`);
    return 0;
  }

  console.log(`\n[${category}] Have ${existingInCat.length}/${count}. Need ${needed} more.`);

  const BATCH = 10;
  let generated = 0;

  for (let i = 0; i < needed; i += BATCH) {
    const batchSize = Math.min(BATCH, needed - i);
    const allExisting = getExistingNames();

    const filledPrompt = prompt.replace('{count}', batchSize);

    const system = `You generate substrate/material entries for the world of Meridian 88. Return ONLY a JSON array of exactly ${batchSize} material objects. No explanation, no markdown fencing, just the JSON array.

${WORLD_CONTEXT}

Each substrate MUST have exactly these fields:
{
  "name": "Material Trade Name",
  "type": "substrate",
  "aliases": ["alternative names", "shorthand"],
  "category": "${category}",
  "description": "2-3 sentence description of the material — what it is, what makes it special, how it's made",
  "properties": ["array of key material properties: strength, flexibility, conductivity, weight, thermal_resistance, self_healing, biocompatibility, etc."],
  "developers": ["array of corponations/labs that produce this material"],
  "applications": ["array of specific applications — where and how this material is used"],
  "tier_availability": "Tier 1-2|Tier 2-3|Tier 3-4|Tier 4-5|All tiers|Military only",
  "cost": "Φ per kg or per unit (be specific)",
  "story_hooks": ["array of 2-3 narrative hooks — how this material creates stories"]
}

CRITICAL: Material names should sound like real trade names — the way Kevlar, Gorilla Glass, Carbon Fiber, Teflon, and Inconel sound. Professional, sometimes evocative, never jokes.`;

    const user = `${filledPrompt}

EXISTING MATERIAL NAMES (DO NOT DUPLICATE ANY): ${allExisting.join(', ')}

Generate exactly ${batchSize} substrates in the ${category} category. Return ONLY the JSON array.`;

    console.log(`  Batch: ${batchSize} items (${i + 1}-${i + batchSize} of ${needed})...`);

    let retries = 0;
    while (retries < 3) {
      try {
        const result = await callClaude(system, user, 8192);
        const items = parseJsonArray(result);

        let saved = 0;
        for (const item of items) {
          item.type = 'substrate';
          item.category = category;
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
          console.log(`    Retrying in ${WAIT_MS/1000}s...`);
          await sleep(WAIT_MS);
        }
      }
    }

    if (i + BATCH < needed) {
      await sleep(WAIT_MS);
    }
  }

  console.log(`[${category}] Generated ${generated} new items.`);
  return generated;
}

async function main() {
  console.log('=== StreetSamurai Substrate Generator ===');
  console.log(`Output: ${OUTPUT_DIR}`);
  console.log(`Target: 100 substrates across ${CATEGORIES.length} categories\n`);

  const existingFiles = fs.readdirSync(OUTPUT_DIR).filter(f => f.endsWith('.json'));
  console.log(`Existing files: ${existingFiles.length}`);

  const totalTarget = CATEGORIES.reduce((s, c) => s + c.count, 0);
  console.log(`Total target: ${totalTarget}`);

  let totalGenerated = 0;

  for (const catDef of CATEGORIES) {
    const n = await generateCategory(catDef);
    totalGenerated += n;
  }

  const finalCount = fs.readdirSync(OUTPUT_DIR).filter(f => f.endsWith('.json')).length;
  console.log(`\n=== DONE ===`);
  console.log(`Total files in substrates/: ${finalCount}`);
  console.log(`Generated this run: ${totalGenerated}`);
}

main().catch(e => {
  console.error('Fatal error:', e);
  process.exit(1);
});
