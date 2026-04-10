// Apparel generator (batch 4) for StreetSamurai
// Generates 200 apparel JSON files in engine/data/apparel/
// Run: node generate_apparel_4.js
// Does NOT overwrite existing files.

const fs = require('fs');
const https = require('https');
const path = require('path');
const crypto = require('crypto');

const settings = JSON.parse(fs.readFileSync(
  path.join(process.env.LOCALAPPDATA, 'MindAttic', 'StreetSamurai', 'Settings.json'), 'utf8'));
const API_KEY = settings.ApiKey;
const MODEL = 'claude-sonnet-4-6';
const OUTPUT_DIR = path.join(__dirname, '..', 'engine', 'data', 'apparel');
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
    .replace(/^_+|_+$/g, '')
    .slice(0, 80);
}

function saveItem(item) {
  const slug = slugify(item.name);
  const filePath = path.join(OUTPUT_DIR, `${slug}.json`);
  if (fs.existsSync(filePath)) {
    console.log(`    SKIP (exists): ${item.name}`);
    return false;
  }
  item.id = crypto.randomUUID().replace(/-/g, '');
  item.name = item.name.slice(0, 60);
  item.type = 'apparel';
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
    } catch (e) { /* skip */ }
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
const WORLD_CONTEXT = `Setting: GLMZ, year 2200. A megacity in the Great Lakes corridor (Chicago-Milwaukee). Currency is QUANTA, symbol Φ. Society is tiered: Tier 1 (the Shelf — poorest, most dangerous), Tier 2 (working class), Tier 3 (middle), Tier 4 (corporate comfort), Tier 5 (the Spire — ultra-elite).

Ubiquitous Diaspora: By 2200, humanity is fully racially interbred. Default to mixed heritage from unexpected global combinations. Cultural traditions persist but are shared freely across all backgrounds.

Technology: BCI (brain-computer interfaces) are common. Augmentation (cyberware/chrome) ranges from basic to military-grade. Geneware allows cosmetic and functional genetic modification (tails, bioluminescence, fur, horns, wings that don't work). Synthetics are artificial beings with full personhood debates ongoing.

Corponations are sovereign corporate entities. Key corponations: Arcturus (defense/security), TESSERA (biotech/geneware), Ringo (media/entertainment), Ouroboros (finance/insurance), Vantablack (stealth tech/intelligence), Lazarus (medical/pharmaceuticals), Crucible (heavy industry/manufacturing).

Apparel must account for augment compatibility (chrome openings, port access), geneware accommodation (tail holes, horn clearance, wing slits, fur-friendly fabrics), and the full spectrum of body modification. Clothing in GLMZ is functional, political, and personal. What you wear signals tier, affiliation, and survival strategy.`;

// ── Schema Definition ──
const SCHEMA_INSTRUCTIONS = `Each item MUST have exactly these fields:
{
  "name": "Item Name (max 60 chars)",
  "type": "apparel",
  "category": "uniform|formal_wear|cultural_subcultural|protective_work",
  "subcategory": "more specific subcategory",
  "manufacturer": "Brand/Corp name",
  "description": "1-2 paragraphs describing the item, its construction, materials, and how it fits into GLMZ life. Keep grounded and real, not parody.",
  "tier_availability": "Tier 1-2|Tier 2-3|Tier 3-4|Tier 4-5|All tiers",
  "price": "Φ amount",
  "augment_compatible": true/false,
  "geneware_compatible": true/false,
  "durability": "disposable|low|medium|high|extreme",
  "legal_status": "legal|restricted|gray_market|illegal",
  "cultural_context": "how people relate to this item in GLMZ society",
  "story_hooks": ["array of 2-3 narrative hooks"],
  "tags": ["array of relevant tags for search/filtering"]
}`;

// ── Category Definitions ──
const CATEGORIES = [
  {
    category: 'uniform',
    count: 50,
    prompt: `Generate {count} UNIFORM apparel items for GLMZ, year 2200. These are work uniforms, corporate wear, and service clothing. Include a diverse mix of:
- CorpSec tactical gear (body armor integrated, threat-response fabrics) for multiple corponations: Arcturus, TESSERA, Ringo, Ouroboros, Vantablack, Lazarus, Crucible — each with distinct design language
- Corporate office wear by corponation (branded blazers, smart-fabric suits, company-mandated attire)
- Medical scrubs and surgical gowns (aug-compatible, biohazard-rated)
- Maintenance jumpsuits (industrial, self-cleaning, tool-loop integrated)
- Kitchen and food service uniforms (heat-resistant, synth-proof, hairnet alternatives for horns/ears)
- Delivery driver outfits (weather-resistant, GPS-woven, high-vis)
- Dockworker gear (heavy-duty, crane-operator rated, radiation-shielded for cargo from irradiated zones)
Each uniform should feel like something a real person wears to work. Brand names should sound professional. Prices range from Φ15 (basic food service apron) to Φ800 (CorpSec tactical kit).`
  },
  {
    category: 'formal_wear',
    count: 50,
    prompt: `Generate {count} FORMAL WEAR apparel items for GLMZ, year 2200. Range from Shelf-formal (the cleanest thing you own, maybe hand-repaired once-nice clothes) to Spire gala couture (holographic thread, self-adjusting fit, privacy-weave). Include:
- Suits (synth-fabric to real-wool, aug-compatible cuts with chrome-display panels)
- Dresses and gowns (bioluminescent hems, geneware-accommodating cuts for tails/wings)
- Tuxedos (classic and futuristic, some with integrated BCI-responsive color shifting)
- Formal robes (cultural, corporate ceremony, synthetic personhood hearings)
- Ceremonial garb (corponation investiture robes, tier-ascension formal wear)
- Diplomatic attire (neutral-signal clothing designed to offend no faction)
The gap between Shelf-formal and Spire-formal should be viscerally clear. A Shelf person's best outfit costs what a Spire person spends on socks. Prices from Φ8 (Shelf thrift formal) to Φ5000+ (Spire couture).`
  },
  {
    category: 'cultural_subcultural',
    count: 50,
    prompt: `Generate {count} CULTURAL and SUBCULTURAL apparel items for GLMZ, year 2200. These are identity-signaling garments. Include:
- Gang insignia clothing (colors, patterns, specific garment modifications that signal affiliation)
- Runner gear (tactical casual — looks normal but has concealed carry, signal-blocking, quick-release)
- Shelf street fashion (DIY, patchwork, repurposed corporate uniforms turned into anti-corporate statements, visible repair as aesthetic)
- Spire haute couture (designer pieces that cost more than a Shelf apartment's annual rent)
- Synthetic-adapted clothing (designed for synthetic beings — no sweat management needed, different joint articulation, identity-expression focused)
- Religious vestments (adapted for 2200 — augment-blessing ceremonies, digital prayer shawls)
- Protest gear (anonymizing, camera-resistant, tear-gas filtering built into scarves/masks)
- Rave and club wear (reactive fabrics, BCI-synced color change, sound-responsive patterns)
These items tell the world who you are or who you want them to think you are. Prices from Φ3 (DIY patch) to Φ3000 (Spire designer piece).`
  },
  {
    category: 'protective_work',
    count: 50,
    prompt: `Generate {count} PROTECTIVE and WORK apparel items for GLMZ, year 2200. Functional gear that keeps people alive. Include:
- Hazmat suits (chemical, biological, radiological — different grades for different threats)
- Radiation gear (for workers near reactors, irradiated zones, cargo handling)
- Welding aprons and gear (for augment installation techs, industrial welders, chop-shop operators)
- Construction vests and suits (smart-fabric, impact-absorbing, fall-detection)
- Fishing gear (Lake Michigan is toxic in parts — sealed suits for deep-trawl, basic waders for safe zones)
- Mining suits (underground resource extraction beneath the megacity)
- Cleanroom suits (for chip fabrication, geneware labs, pharmaceutical manufacturing)
- Thermal survival gear (Great Lakes winters are brutal, especially for Shelf residents with no heating)
- Riot gear (both for CorpSec and for civilians who expect to be on the wrong end of CorpSec)
- Environmental suits (for venturing outside the megacity envelope into contaminated zones)
Prices from Φ10 (basic construction vest) to Φ2000 (military-grade environmental suit).`
  }
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
    const rankStart = existingInCat.length + i + 1;
    const rankEnd = existingInCat.length + i + batchSize;

    const allExisting = getExistingNames();

    const filledPrompt = prompt.replace('{count}', batchSize);

    const system = `You generate apparel entries for the world of GLMZ, year 2200. Return ONLY a JSON array of exactly ${batchSize} apparel objects. No explanation, no markdown fencing, just the JSON array.

${WORLD_CONTEXT}

${SCHEMA_INSTRUCTIONS}

CRITICAL RULES:
- Descriptions should be 1-2 paragraphs. Grounded, real, lived-in. Not parody.
- Currency symbol is Φ (QUANTA). Always use Φ for prices.
- Names must be 60 characters or fewer.
- Every item must have category "${category}".
- type must always be "apparel".`;

    const user = `${filledPrompt}

EXISTING ITEM NAMES (DO NOT DUPLICATE ANY): ${allExisting.join(', ')}

Generate exactly ${batchSize} items for the ${category} category. Return ONLY the JSON array.`;

    console.log(`  Batch: ${batchSize} items (offset ${i})...`);

    let retries = 0;
    while (retries < 3) {
      try {
        const result = await callClaude(system, user, 8192);
        const items = parseJsonArray(result);

        let saved = 0;
        for (const item of items) {
          item.type = 'apparel';
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
          console.log(`    Retrying in ${WAIT_MS / 1000}s...`);
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
  console.log('=== StreetSamurai Apparel Generator (Batch 4) ===');
  console.log(`Output: ${OUTPUT_DIR}`);
  console.log(`Target: 200 apparel items across ${CATEGORIES.length} categories\n`);

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
  console.log(`Total files in apparel/: ${finalCount}`);
  console.log(`Generated this run: ${totalGenerated}`);
}

main().catch(e => {
  console.error('Fatal error:', e);
  process.exit(1);
});
