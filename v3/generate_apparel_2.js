// Apparel generator for StreetSamurai — Tops & Jackets/Outerwear
// Generates 200 apparel JSON files in engine/data/apparel/
// Run: node generate_apparel_2.js
// Does NOT overwrite existing files.

const fs = require('fs');
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
      },
      timeout: 300000
    }, res => {
      let data = '';
      res.on('data', chunk => data += chunk);
      res.on('end', () => {
        try {
          const j = JSON.parse(data);
          if (j.error) reject(new Error(`API error: ${j.error.message || JSON.stringify(j.error)}`));
          else if (j.content && j.content[0]) resolve(j.content[0].text);
          else reject(new Error(data.substring(0, 500)));
        } catch (e) { reject(e); }
      });
    });
    req.setTimeout(300000, () => { req.destroy(); reject(new Error('Request timeout after 300s')); });
    req.on('error', reject);
    req.write(body);
    req.end();
  });
}

async function callClaudeWithRetry(system, user, maxTokens = 8192, retries = 3) {
  for (let attempt = 1; attempt <= retries; attempt++) {
    try {
      return await callClaude(system, user, maxTokens);
    } catch (err) {
      console.error(`    Attempt ${attempt}/${retries} failed: ${err.message}`);
      if (attempt < retries) {
        const wait = attempt * 10000;
        console.log(`    Retrying in ${wait / 1000}s...`);
        await sleep(wait);
      } else {
        throw err;
      }
    }
  }
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
  if (!fs.existsSync(OUTPUT_DIR)) return [];
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
  if (!fs.existsSync(OUTPUT_DIR)) return {};
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

function randomHex32() {
  let hex = '';
  for (let i = 0; i < 32; i++) hex += Math.floor(Math.random() * 16).toString(16);
  return hex;
}

// ── World Context ──
const WORLD_CONTEXT = `Setting: GLMZ, year 2200. A megacity in the Great Lakes corridor (Chicago-Milwaukee). Currency is Phi (the symbol is the Greek letter but in this world it stands for QUANTA). Society is tiered: Tier 1 (the Shelf — poorest, most dangerous), Tier 2 (working class), Tier 3 (middle class), Tier 4 (corporate comfort), Tier 5 (the Spire — ultra-elite).

Ubiquitous Diaspora: By 2200, humanity is fully racially interbred. Heritage comes from unexpected global combinations. Fashion draws from every cultural tradition freely — West African prints meet Korean minimalism meet Andean textiles meet Scandinavian utility.

Technology: BCI (brain-computer interfaces) are common. Augmentation (cyberware/chrome) ranges from basic to military-grade. Geneware allows cosmetic and functional genetic modification (tails, bioluminescence, fur, horns, non-functional wings). Most people dress NORMALLY — jeans, t-shirts, jackets. Tech-enhanced clothing exists but is not the default.

Corponations are sovereign corporate entities. They manufacture most goods. Street brands also exist — unlicensed, often higher quality for specific niches, always with underground cachet.

Brand names should sound like real brand names — not jokes or parodies. Think how Nike, Carhartt, Patagonia, Uniqlo, Arc'teryx sound — professional, sometimes evocative, sometimes just a name. Mix corp mega-brands with mid-tier labels and street/independent brands.

IMPORTANT: Most clothing is NORMAL. People wear t-shirts, hoodies, leather jackets, button-downs. Tech-enhanced items exist but are the exception, not the rule. Maybe 30% of items have some tech feature. The rest are just clothes — good clothes, interesting clothes, but clothes.`;

// ── Schema reference for the prompt ──
const SCHEMA_REF = `{
  "id": "<32-char hex string>",
  "name": "Brand Model Name",
  "type": "apparel",
  "category": "top|jacket|outerwear",
  "description": "1-2 paragraphs describing the item — what it looks like, feels like, who wears it",
  "tier_association": "Tier X",
  "materials": ["array of materials"],
  "functionality": "what tech features it has, if any (empty string for normal clothes)",
  "what_it_says": "what wearing this item says about the person",
  "worn_by": ["types of people who wear this"],
  "manufacturer": "Brand or corponation name",
  "price_range": "price in currency symbol + amount",
  "aug_compatible": true or false,
  "gene_compatible": true or false,
  "story_hooks": ["2-3 narrative hooks"],
  "tags": ["apparel", "category", "tier X", "other relevant tags"]
}`;

// ── Category Definitions ──
const CATEGORIES = [
  {
    category: 'top',
    count: 100,
    prompt: `Generate {count} TOPS (upper body garments, NOT jackets/outerwear) for GLMZ. Items {rankStart} through {rankEnd}.

Include a diverse mix of: t-shirts, tank tops, dress shirts, blouses, sweaters, hoodies, thermal underlayers, armored vests (concealed carry style), smart-fabric shirts, corporate uniforms, Shelf patchwork tops (scavenged/repaired), band tees (for M88 bands), gang color tops, workwear shirts, henleys, turtlenecks, crop tops.

Mix ALL tiers across the batch:
- Tier 1 (Shelf): cheap synth-fabric, repaired, scavenged, street vendor quality. Prices around 5-25 currency units.
- Tier 2 (working class): durable, functional, mass-produced. Prices around 15-60 currency units.
- Tier 3 (middle): decent quality, some style, maybe one tech feature. Prices around 40-150 currency units.
- Tier 4 (corporate): high quality, clean lines, often branded with corp logos. Prices around 100-400 currency units.
- Tier 5 (Spire): luxury fabrics, bespoke, may have subtle tech. Prices around 300-2000+ currency units.

CRITICAL: Most items are NORMAL CLOTHES. Only ~30% should have tech features (smart-fabric, temperature regulation, etc). The rest are just shirts, sweaters, tees — well-described, interesting, but mundane.

Aug-compatible means the garment accommodates cyberware (reinforced seams near chrome, open panels for arm/shoulder augments). Gene-compatible means it accommodates geneware modifications (tail holes, horn clearance, extra width for decorative wings/fins).

Do NOT just list "smart shirt" variants. Give variety: a plain cotton-analog henley, a corporate dress shirt, a Shelf punk's patched tank top, a grandmother's hand-knit sweater, a construction worker's reinforced thermal.`
  },
  {
    category: 'jacket',
    count: 60,
    prompt: `Generate {count} JACKETS for GLMZ. Items {rankStart} through {rankEnd}.

Include a diverse mix of: leather jackets, bomber jackets, corporate overcoats, armored coats (concealed ballistic layers), windbreakers, parkas, rain shells, lab coats, security vests, reflective safety jackets, denim jackets, varsity jackets, motorcycle jackets, blazers, sport coats.

Mix ALL tiers. CRITICAL: Most items are NORMAL JACKETS. Only ~30% should have tech features. The rest are leather, denim, wool, synth-fabric — just jackets.

Aug-compatible means accommodates cyberware. Gene-compatible means accommodates geneware mods.

Prices: Tier 1 (10-50), Tier 2 (30-120), Tier 3 (80-300), Tier 4 (200-800), Tier 5 (500-5000+). All in currency units.`
  },
  {
    category: 'outerwear',
    count: 40,
    prompt: `Generate {count} OUTERWEAR (heavier/specialized outer garments) for GLMZ. Items {rankStart} through {rankEnd}.

Include a diverse mix of: trenchcoats, dusters, stealth cloaks (rare, expensive, Tier 4-5), thermal heavy layers, ponchos, capes (yes, some people wear capes in 2200 — mostly geneware users with wings/dramatic flair), heavy parkas for lakefront weather, hazmat overcoats (Shelf necessity near toxic zones), ceremonial robes (corp or cultural).

Mix ALL tiers. CRITICAL: Most items are NORMAL OUTERWEAR. Only ~30% should have tech features like active camo, thermal regulation, or scanner blocking. The rest are wool coats, rain ponchos, heavy parkas — practical outerwear.

Aug-compatible means accommodates cyberware. Gene-compatible means accommodates geneware mods.

Prices: Tier 1 (15-60), Tier 2 (40-150), Tier 3 (100-400), Tier 4 (300-1200), Tier 5 (800-8000+). All in currency units.`
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

    const filledPrompt = prompt
      .replace('{count}', batchSize)
      .replace('{rankStart}', rankStart)
      .replace('{rankEnd}', rankEnd);

    const system = `You generate apparel items for the world of GLMZ. Return ONLY a JSON array of exactly ${batchSize} objects. No explanation, no markdown fencing, just the JSON array.

${WORLD_CONTEXT}

Each item MUST have exactly these fields:
${SCHEMA_REF}

Generate unique 32-character hex strings for each id. Use the currency symbol (the Greek letter that looks like a circle with a vertical line) followed by the amount for price_range.

ALREADY EXISTING (do NOT duplicate these names): ${allExisting.slice(-50).join(', ') || 'none yet'}`;

    const user = filledPrompt;

    console.log(`  Batch ${Math.floor(i / BATCH) + 1}: generating items ${rankStart}-${rankEnd}...`);

    try {
      const raw = await callClaudeWithRetry(system, user);
      const items = parseJsonArray(raw);

      for (const item of items) {
        // Ensure required fields
        if (!item.id) item.id = randomHex32();
        item.type = 'apparel';
        if (!item.category) item.category = category;
        if (!item.tags) item.tags = [];
        if (!item.tags.includes('apparel')) item.tags.unshift('apparel');
        if (!item.tags.includes(category)) item.tags.push(category);

        if (saveItem(item)) {
          generated++;
          console.log(`    [${generated}] ${item.name} (${item.tier_association})`);
        }
      }
    } catch (err) {
      console.error(`  ERROR in batch: ${err.message}`);
    }

    if (i + BATCH < needed) {
      console.log(`  Waiting ${WAIT_MS}ms...`);
      await sleep(WAIT_MS);
    }
  }

  console.log(`[${category}] Generated ${generated} new items.`);
  return generated;
}

async function main() {
  console.log('=== Apparel Generator (Tops & Jackets/Outerwear) ===');
  console.log(`Output: ${OUTPUT_DIR}`);
  console.log(`Target: 200 items (100 tops, 60 jackets, 40 outerwear)\n`);

  let totalGenerated = 0;

  for (const catDef of CATEGORIES) {
    const count = await generateCategory(catDef);
    totalGenerated += count;
  }

  // Final count
  const finalFiles = fs.readdirSync(OUTPUT_DIR).filter(f => f.endsWith('.json'));
  console.log(`\n=== DONE ===`);
  console.log(`Generated ${totalGenerated} new items this run.`);
  console.log(`Total files in apparel/: ${finalFiles.length}`);
}

main().catch(err => {
  console.error('Fatal error:', err);
  process.exit(1);
});
