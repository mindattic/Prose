// Apparel GAP generator for StreetSamurai
// Generates 200 apparel items across underrepresented categories:
//   headwear, gloves, accessory, base_layer, cultural, protective, outfit
// Run: node generate_apparel_gap.js
// Does NOT overwrite existing files.

const fs = require('fs');
const crypto = require('crypto');
const https = require('https');
const path = require('path');

const settings = JSON.parse(fs.readFileSync(
  path.join(process.env.LOCALAPPDATA, 'MindAttic', 'StreetSamurai', 'Settings.json'), 'utf8'));
const API_KEY = settings.ApiKey;
const MODEL = 'claude-sonnet-4-6';
const OUTPUT_DIR = path.join(__dirname, '..', 'engine', 'data', 'apparel');
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
    req.setTimeout(120000, () => {
      req.destroy();
      reject(new Error('Request timed out after 120s'));
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

function genId() {
  return crypto.randomBytes(16).toString('hex');
}

function saveItem(item) {
  const slug = slugify(item.name.slice(0, 60));
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

// ── World Context ──
const WORLD_CONTEXT = `Setting: GLMZ (GLMZ), year 2200. A megacity in the Great Lakes corridor (Chicago-Milwaukee). Currency is Phi (the symbol is \u03A6, representing QUANTA, not the Greek letter). Society is tiered:
- Tier 1 "The Shelf" — poorest, most dangerous. Reclaimed industrial zones, acid rain, patched infrastructure.
- Tier 2 "Circuit" — working class. Factory workers, transit operators, street vendors. Clean but functional.
- Tier 3 — middle management, cubicle workers, small business owners.
- Tier 4 — corporate comfort. Junior execs, specialists, skilled professionals.
- Tier 5 "The Spire" — ultra-elite. Corponation C-suite, power brokers, old money.

Ubiquitous Diaspora: By 2200, humanity is fully racially interbred. Default to mixed heritage from unexpected global combinations. Fashion reflects global fusion — no single cultural tradition dominates.

Technology: BCI (brain-computer interfaces) are common. Augmentation (cyberware/chrome) ranges from basic to military-grade. Geneware allows cosmetic and functional genetic modification (tails, bioluminescence, fur, horns, non-functional wings). Synthetics are artificial beings with non-human body proportions.

Corponations are sovereign corporate entities. They manufacture most goods. Street brands also exist — unlicensed, often better for specific niches, always with underground cachet.

Fashion notes: Clothing must accommodate augmentation (chrome arms, leg prosthetics, spinal rigs, neural ports) and geneware (tails, horns, wings, fur, scales). Aug-compatible means openings, channels, or adaptive seams for chrome. Gene-compatible means accommodation for biological modifications.`;

// ── Gap Categories ──
const CATEGORIES = [
  {
    tag: 'headwear',
    category: 'headwear',
    count: 30,
    prompt: `Generate {count} HEADWEAR items for GLMZ. Include a MIX of: tactical helmets, everyday hats and caps, hoods (standalone or detachable), masks (half-face, full-face, filter masks), visors (AR-enabled, welding, sun), neural-port covers and caps, AR glasses and smart eyewear, balaclavas, and head wraps. Spread across ALL tiers from Shelf scrap-metal face shields to Spire designer neural-port fascinators. Each must have a distinct brand/maker name. Some aug-compatible (accommodating neural ports, cranial chrome), some gene-compatible (horn cutouts, ear accommodation for animal ears).`
  },
  {
    tag: 'gloves',
    category: 'gloves',
    count: 30,
    prompt: `Generate {count} GLOVE items for GLMZ. Include: tactical/combat gloves, work gloves (industrial, welding, chemical-resistant), fashion gloves (Spire galas, club wear), shock-resistant insulated gloves, augmentation-interface gloves (that connect to chrome forearms or hand augments), haptic feedback gloves, medical/surgical gloves, thief/infiltration gloves (grip-enhanced, print-masking), and cold/environmental gloves. Spread across tiers. Some designed to cover chrome hands cosmetically. Some with cutaway fingers for augmented digits.`
  },
  {
    tag: 'accessory',
    category: 'accessory',
    count: 30,
    prompt: `Generate {count} ACCESSORY items for GLMZ. Include: belts (utility, fashion, holster-integrated), holsters and harnesses (concealed, tactical, over-jacket), bags and packs (messenger, tactical sling, courier, duffel), watches and wrist devices (smart, analog-retro, aug-interface), functional jewelry (rings with embedded tools, necklaces with data chips, bracelets that double as comms), dog tags and ID markers, scarves and neck wraps, arm bands, and ankle monitors (both voluntary and court-ordered). Spread across all tiers.`
  },
  {
    tag: 'base_layer',
    category: 'base_layer',
    count: 20,
    prompt: `Generate {count} UNDERGARMENT/BASE LAYER items for GLMZ. Include: thermal underlayers for Shelf cold, compression shirts and leggings (athletic, medical, tactical), smart-fabric base layers (biometric monitoring, temperature regulation), armor underlayers (soft ballistic weave worn under clothes), moisture-wicking tactical base layers, medical compression garments post-surgery, and aug-interface bodysuits (skin-tight layers that route power and data between chrome implants). Spread across tiers from Shelf hand-stitched thermals to Spire nano-fabric climate suits.`
  },
  {
    tag: 'cultural',
    category: 'cultural',
    count: 30,
    prompt: `Generate {count} CULTURAL/SUBCULTURAL apparel items for GLMZ. Include: gang insignia wear (colors, patches, specific garment modifications that mark territory), runner gear (signature looks that build street rep), rave and club culture wear (reactive fabrics, LED-threaded, sound-reactive), Shelf DIY fashion (hand-painted, salvage-art, deliberately anti-corporate), Spire haute couture (one-of-a-kind statement pieces by named designers), religious/spiritual wear adapted for 2200, protest fashion (anti-corponation messaging, subversive designs), and synthetic identity fashion (clothes that declare synthetic personhood). These are garments that make a CULTURAL STATEMENT.`
  },
  {
    tag: 'protective',
    category: 'protective',
    count: 30,
    prompt: `Generate {count} PROTECTIVE/WORK apparel items for GLMZ. Include: hazmat suits and chemical protection, radiation suits (for reactor zones and contaminated Shelf areas), welding gear and forge aprons, mining and tunneling suits, cleanroom suits (for chip fab, biotech labs, geneware clinics), environmental suits (acid rain, toxic atmosphere, extreme cold), blast suits (bomb disposal), firefighting gear adapted for chemical fires, construction exoskeletons with integrated clothing, and decontamination coveralls. Spread across tiers. Industrial and dangerous-environment focused.`
  },
  {
    tag: 'gap_outfit',
    category: 'outfit',
    count: 30,
    prompt: `Generate {count} COMPLETE OUTFIT SETS that fill specific gaps. Include: military/CorpSec field kits (full loadout descriptions), medical professional outfits (augmentation surgeons, gene clinic staff, street clinic ripperdocs), underworld complete looks (fixers, enforcers, info brokers), synthetic-adapted outfits (for non-human body proportions), and unique character-defining looks (a disgraced Spire judge, a Shelf inventor, a synthetic street artist, a tier-hopping courier, a burned corporate spy). Each outfit should be a complete head-to-toe look. Keep descriptions to 1-2 SHORT paragraphs.`
  },
];

// ── Generation Logic ──
async function generateBatch(catDef) {
  const { tag, category, count, prompt } = catDef;

  // Count existing for this category
  const files = fs.readdirSync(OUTPUT_DIR).filter(f => f.endsWith('.json'));
  let existingInCat = 0;
  for (const f of files) {
    try {
      const data = JSON.parse(fs.readFileSync(path.join(OUTPUT_DIR, f), 'utf8'));
      if (data.category === category && (category !== 'outfit' || (data.tags && data.tags.includes(tag)))) {
        existingInCat++;
      }
    } catch (e) { /* skip */ }
  }

  const needed = count - existingInCat;
  if (needed <= 0) {
    console.log(`[${tag}] Already have ${existingInCat}/${count}. Skipping.`);
    return 0;
  }

  console.log(`\n[${tag}] Have ${existingInCat}/${count}. Need ${needed} more.`);

  const BATCH = 5;
  let generated = 0;

  for (let i = 0; i < needed; i += BATCH) {
    const batchSize = Math.min(BATCH, needed - i);

    // Get a sample of existing names to avoid duplicates (limit to 200 to avoid token overflow)
    const allExisting = getExistingNames();
    const sampleNames = allExisting.length > 200
      ? allExisting.sort(() => Math.random() - 0.5).slice(0, 200)
      : allExisting;

    const filledPrompt = prompt.replace('{count}', batchSize);

    const system = `You generate apparel items for the world of GLMZ. Return ONLY a JSON array of exactly ${batchSize} objects. No explanation, no markdown fencing, just the JSON array.

${WORLD_CONTEXT}

Each item MUST have exactly these fields:
{
  "id": "<32-character hex string>",
  "name": "Brand Model Name (60 chars max)",
  "type": "apparel",
  "category": "${category}",
  "description": "1-2 SHORT paragraphs. Be specific about materials, colors, wear patterns, construction details. Keep it concise — no more than 150 words.",
  "tier_association": "Tier X or Tier X-Y",
  "materials": ["array of specific materials"],
  "functionality": "practical features, one SHORT paragraph",
  "what_it_says": "what this item communicates about the wearer — one sentence",
  "worn_by": ["types of people who wear this"],
  "manufacturer": "brand name — corponation, street label, or self-made",
  "price_range": "price range using the \u03A6 (QUANTA) symbol",
  "aug_compatible": true or false,
  "gene_compatible": true or false,
  "story_hooks": ["2-3 short narrative hooks"],
  "tags": ["apparel", "${category}", "${tag}", ...]
}

CRITICAL RULES:
- Each "id" must be a unique 32-character lowercase hex string.
- "name" must be 60 characters or fewer. Format: "Brand ModelName" or "Brand Descriptor Name".
- The \u03A6 symbol represents QUANTA currency, NOT the Greek letter phi.
- Descriptions MUST be 1-2 SHORT paragraphs, max 150 words total. Be vivid but concise.
- Tags must always include "apparel" and "${category}".
- Ubiquitous Diaspora: fashion draws from ALL global traditions freely mixed.
- Vary the tier associations across the batch.`;

    const user = `${filledPrompt}

EXISTING NAMES (DO NOT DUPLICATE): ${sampleNames.join(', ')}

Generate exactly ${batchSize} items. Return ONLY the JSON array.`;

    console.log(`  Batch ${Math.floor(i / BATCH) + 1}: generating ${batchSize} ${tag} items...`);

    let retries = 0;
    while (retries < 3) {
      try {
        const result = await callClaude(system, user, 16384);
        const items = parseJsonArray(result);

        let saved = 0;
        for (const item of items) {
          // Enforce schema
          item.type = 'apparel';
          item.category = category;
          if (!item.id || item.id.length !== 32) item.id = genId();
          if (!item.tags) item.tags = [];
          if (!item.tags.includes('apparel')) item.tags.unshift('apparel');
          if (!item.tags.includes(category)) item.tags.push(category);
          if (!item.tags.includes(tag)) item.tags.push(tag);
          item.name = (item.name || 'Unknown Item').slice(0, 60);

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
          console.log(`    Retrying in ${WAIT_MS / 1000}s...`);
          await sleep(WAIT_MS);
        }
      }
    }

    if (i + BATCH < needed) {
      await sleep(WAIT_MS);
    }
  }

  console.log(`[${tag}] Generated ${generated} new items.`);
  return generated;
}

async function main() {
  console.log('=== StreetSamurai Apparel GAP Generator ===');
  console.log(`Output: ${OUTPUT_DIR}`);
  const totalTarget = CATEGORIES.reduce((s, c) => s + c.count, 0);
  console.log(`Target: ${totalTarget} items across ${CATEGORIES.length} gap categories\n`);

  const existingFiles = fs.readdirSync(OUTPUT_DIR).filter(f => f.endsWith('.json'));
  console.log(`Existing files: ${existingFiles.length}`);

  let totalGenerated = 0;

  for (const catDef of CATEGORIES) {
    const n = await generateBatch(catDef);
    totalGenerated += n;
  }

  const finalCount = fs.readdirSync(OUTPUT_DIR).filter(f => f.endsWith('.json')).length;
  console.log(`\n=== DONE ===`);
  console.log(`Total files in apparel/: ${finalCount}`);
  console.log(`Generated this run: ${totalGenerated}`);
}

main().catch(e => {
  console.error('Fatal error:', e);
  process.exit(1);
});
