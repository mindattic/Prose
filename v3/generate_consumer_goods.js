// Consumer goods generator for StreetSamurai
// Generates 1024 consumer good JSON files in engine_data/consumer_goods/
// Run: node generate_consumer_goods.js
// Does NOT overwrite existing files.

const fs = require('fs');
const https = require('https');
const path = require('path');

const settings = JSON.parse(fs.readFileSync(
  path.join(process.env.LOCALAPPDATA, 'MindAttic', 'StreetSamurai', 'Settings.json'), 'utf8'));
const API_KEY = settings.ApiKey;
const MODEL = 'claude-sonnet-4-6';
const ENGINE_DATA = path.join(__dirname, '..', 'engine_data');
const OUTPUT_DIR = path.join(ENGINE_DATA, 'consumer_goods');
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
  // Sometimes the model wraps in extra text — find the array
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

function fileExists(product) {
  const slug = slugify(product.name);
  return fs.existsSync(path.join(OUTPUT_DIR, `${slug}.json`));
}

function saveProduct(product) {
  const slug = slugify(product.name);
  const filePath = path.join(OUTPUT_DIR, `${slug}.json`);
  if (fs.existsSync(filePath)) {
    console.log(`    SKIP (exists): ${product.name}`);
    return false;
  }
  fs.writeFileSync(filePath, JSON.stringify(product, null, 2));
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

// ── World Context (shared across all prompts) ──
const WORLD_CONTEXT = `Setting: GLMZ, year 2200. A megacity in the Great Lakes corridor (Chicago-Milwaukee). Currency is Phi (Φ). Society is tiered: Tier 1 (the Shelf — poorest, most dangerous), Tier 2 (working class), Tier 3 (middle), Tier 4 (corporate comfort), Tier 5 (the Spire — ultra-elite).

CRITICAL FLAVOR CONTEXT — Ubiquitous Diaspora: By 2200, humanity is fully racially interbred. There is no "ethnic food" — all food is everyone's food. Tamarind-cardamom is as common as vanilla was in 2024. Gochujang-lime is a standard chip flavor. Ube-coconut is mainstream ice cream. Hibiscus-mango soda outsells cola. The flavor palette is GLOBAL — no single cuisine dominates. Flavor mashups cross every culinary tradition freely: West African + East Asian, South American + Middle Eastern, Nordic + Southeast Asian. "Exotic" has no meaning when everything is heritage.

Technology: BCI (brain-computer interfaces) are common. Augmentation (cyberware/chrome) ranges from basic to military-grade. Geneware allows cosmetic and functional genetic modification (tails, bioluminescence, fur, horns, wings that don't work). Synth-protein is the primary food source for Tiers 1-3. Real ingredients (real cocoa, real coffee, real meat) are luxury items.

Corponations are sovereign corporate entities. They manufacture most goods. Street brands also exist — unlicensed, often better quality for specific niches, always with an underground cachet.

Products should feel REAL — not parodies. Brand names should sound like actual brand names, not jokes. Think how Samsung, Unilever, Nestle, or craft brands sound — professional, sometimes evocative, sometimes just a name.`;

// ── Category Definitions ──
const CATEGORIES = [
  {
    category: 'soda',
    count: 90,
    prompt: `Generate {count} sodas/soft drinks for GLMZ, ranked {rankStart} to {rankEnd} within the soda category (1 = most popular, the Coca-Cola equivalent of GLMZ). Include: tamarind, hibiscus, yuzu, guava, ube, lychee, horchata, baobab, sorrel, passionfruit, mate, bissap flavors and many more global mashups. Some synth-flavored (cheaper, Tier 1-2), some real-extract (expensive, Tier 3-4). Brand names should sound futuristic but natural — real brand energy, not parody. Mix corporate mega-brands (#1-10) with mid-tier players (#11-40) and niche/street brands (#41-80). Prices range from Φ0.50 (cheapest synth) to Φ15 (premium real-extract).`
  },
  {
    category: 'candy',
    count: 80,
    prompt: `Generate {count} candy/sweets for GLMZ, ranked {rankStart} to {rankEnd}. Include: gummies, hard candy, chocolate analogs (real cocoa is rare/expensive), synth-chews, stim-candy (mild stimulant effect), mood-candy (mild emotional modulation via bioactive compounds). Global flavor mashups: matcha-tahini, ube-coconut, tamarind-chili, cardamom-rose, yuzu-ginger, mango-habanero, black sesame-honey. Mix massive brands with street-level candy makers. Prices from Φ0.25 (single synth candy) to Φ45 (real chocolate bar).`
  },
  {
    category: 'snack',
    count: 80,
    prompt: `Generate {count} snacks/chips for GLMZ, ranked {rankStart} to {rankEnd}. Include: crisps, puffs, bars, jerky (synth-meat and real), nuts, dried fruit, protein blocks. Flavors: berbere, gochujang, sumac, za'atar, miso, jerk seasoning, chimichurri, furikake, harissa, dukkah, shichimi, adobo, curry leaf, pandan. Street brands vs corporate brands. Some are nutrition-dense protein snacks for workers, others are pure junk. Prices from Φ0.75 to Φ25.`
  },
  {
    category: 'meal',
    count: 90,
    prompt: `Generate {count} prepared meals/fast food products for GLMZ, ranked {rankStart} to {rankEnd}. These are the instant ramen, frozen dinners, street food cart standards of 2200. Include: synth-protein bowls, nutrient paste tubes (Tier 1 — cheapest), flash-heated meal kits (Tier 2-3), premium real-ingredient meals (Tier 4-5). Flavor profiles span every global tradition: jollof-inspired, pho-style, tikka masala, rendang, mole, tagine, bibimbap, pierogi, empanada. Some are branded street cart standards, some are corporate meal replacement. Prices from Φ1.50 (nutrient paste) to Φ80 (real-ingredient premium).`
  },
  {
    category: 'stimulant',
    count: 55,
    prompt: `Generate {count} stimulants for GLMZ, ranked {rankStart} to {rankEnd}. The caffeine landscape of 2200 is varied and potent. Include: coffee analogs (synth and real), stim-tabs, focus gum, energy drinks, nootropic beverages, crash-tabs (come-down aids), concentration drops, endurance patches. Real coffee is luxury (Φ15-40/cup). Synth-caffeine products are everywhere. Some interact with BCI for enhanced focus. Street-level stims are stronger but rougher. Corporate stims are smoother but weaker. Prices from Φ0.50 (basic stim-tab) to Φ45 (real coffee blend).`
  },
  {
    category: 'alcohol',
    count: 65,
    prompt: `Generate {count} alcoholic beverages for GLMZ, ranked {rankStart} to {rankEnd}. Include: synth-spirits (cheap, precise intoxication curves — you choose your drunk), real-brewed beer (craft, expensive), rice wine variants, palm wine, mead, agave spirits, fermented mare's milk, sake-soju hybrids, cocktail premixes, BCI-interactive drinks (the buzz syncs with your neural interface). Shelf bars serve cheap synth. Spire lounges serve real-aged spirits. Street vendors sell unlicensed homebrew. Prices from Φ2 (synth shot) to Φ200+ (real aged whiskey).`
  },
  {
    category: 'hygiene',
    count: 65,
    prompt: `Generate {count} hygiene/personal care products for GLMZ, ranked {rankStart} to {rankEnd}. Include: soap, shampoo (for natural AND synthetic hair/fur from geneware), augment polish, chrome cleaner, dermal patch deodorant, geneware-compatible skincare, tail conditioner (for people with geneware tails), bio-luminescent skin moisturizer, anti-rejection dermal cream (for augment sites), neural port sanitizer, scale conditioner (for reptilian geneware). Must account for the full diversity of human modification. Prices from Φ1 to Φ60.`
  },
  {
    category: 'cleaning',
    count: 45,
    prompt: `Generate {count} cleaning products for GLMZ, ranked {rankStart} to {rankEnd}. Include: surface cleaners, air recycler filters, water purification tabs (critical for Tier 1-2), mold inhibitors (critical in Old Harbor district), rust prevention for chrome/augments, drone maintenance spray, hab-unit sanitizers, synth-fabric fresheners, grease cutters for street food prep, industrial decontaminants. The Shelf has different cleaning needs than the Spire. Prices from Φ0.75 to Φ35.`
  },
  {
    category: 'tobacco',
    count: 45,
    prompt: `Generate {count} tobacco/vapor products for GLMZ, ranked {rankStart} to {rankEnd}. Real tobacco is rare and expensive. Include: synth-nicotine vaporizers, herbal blends, stim-smoke (stimulant-laced), focus-vapor, calm-vapor, neural-vapor (interacts with BCI for enhanced effect — legally gray), ritual smoke blends, social vapor (designed for sharing at bars). Street blends are unregulated. Corporate vapor is precise-dosed. Prices from Φ1.50 to Φ75.`
  },
  {
    category: 'clothing',
    count: 65,
    prompt: `Generate {count} clothing brands/products for GLMZ, ranked {rankStart} to {rankEnd}. Include: street fashion labels, corporate uniforms, aug-compatible clothing (openings/channels for chrome augments), geneware-friendly (tail holes, horn clearance, extra-wide backs for non-functional wings), weatherproof for the Shelf (acid rain resistant), self-repairing fabric, temperature-regulating weave, privacy-fabric (blocks scanning). Mix street style with corporate minimalism with Spire luxury. Prices from Φ5 (basic synth-fabric) to Φ500+ (designer aug-wear).`
  },
  {
    category: 'medicine_otc',
    count: 55,
    prompt: `Generate {count} OTC medicines for GLMZ, ranked {rankStart} to {rankEnd}. Include: pain relief, stim-crash recovery, augment rejection suppressors (critical for chrome users), neural headache relief (BCI overuse), sleep aids, anti-nausea for mass driver/transit riders, hangover cures, geneware expression stabilizers (prevents geneware from drifting), anti-inflammatory for augment sites, mood stabilizers (OTC grade), immune boosters for Shelf conditions. Prices from Φ2 to Φ50.`
  },
  {
    category: 'pet_food',
    count: 35,
    prompt: `Generate {count} pet products for GLMZ, ranked {rankStart} to {rankEnd}. Pets in 2200 include gene-modded animals: bioluminescent fish, miniature big cats, synthetic-fur companions, augmented dogs with basic BCI. Include: bioluminescent fish food (maintains glow), synthetic fur conditioner, augmented-pet firmware update treats (nano-delivery nutrients that update pet aug firmware), gene-stabilizer pet food, exotic pet nutrients, companion animal mood supplements, pet chrome polish. Prices from Φ3 to Φ45.`
  },
  {
    category: 'electronics',
    count: 65,
    prompt: `Generate {count} consumer electronics/gadgets for GLMZ, ranked {rankStart} to {rankEnd}. Include: cheap data pads (the smartphones of 2200), disposable comm devices (burner phones), entertainment chips (slot into BCI for immersive media), holographic toys, privacy screens (blocks visual scanning), signal boosters, BCI accessories (decorative neural port covers, signal enhancers, comfort pads), augment cosmetic covers (snap-on shells that change your chrome's appearance), portable power cells. Prices from Φ2 to Φ300.`
  },
  {
    category: 'cosmetic',
    count: 55,
    prompt: `Generate {count} cosmetics for GLMZ, ranked {rankStart} to {rankEnd}. Makeup and cosmetics for EVERY skin tone (which is every skin tone, since humanity is fully interbred). Include: chrome accent paint (decorative paint for augments), bioluminescent nail polish, synthetic-skin compatible foundation, fur dye for geneware users, horn polish and decorative horn paint, scale gloss, dermal pattern applicators (temporary skin patterns), augment-site concealer, neural port jewelry adhesive, eye-mod color enhancers. Prices from Φ3 to Φ80.`
  },
  {
    category: 'synth_food_base',
    count: 50,
    prompt: `Generate {count} synth-food base products for GLMZ, ranked {rankStart} to {rankEnd}. These are the raw synth-protein and nutrient bases that most Tier 1-2 food is made from, plus flavoring packets that transform them. Include: plain nutrient paste blocks, protein slurry concentrates, flavor packets (turn paste into "chicken tikka masala" or "jollof rice" or "pho" or "mole negro"), texture modulators (make paste crunchy, chewy, or silky), nutrient fortifiers, calorie boosters for heavy labor, vitamin infusion drops. This is the foundation of how most people eat. Prices from Φ0.25 to Φ12.`
  },
  {
    category: 'stationery',
    count: 40,
    prompt: `Generate {count} stationery/analog products for GLMZ, ranked {rankStart} to {rankEnd}. Paper is rare, expensive, and fetishized by some. Writing by hand is a status symbol on the Shelf — it means you have thoughts worth hiding from your BCI. Include: actual paper (various grades), pens (some with bio-ink that only the writer can read), physical notebooks (a major status symbol), drawing supplies, calligraphy tools, ink (including privacy ink — invisible to cameras), sketchpads, journaling kits. Prices from Φ8 (basic recycled notepad) to Φ200+ (premium bound journal with real paper).`
  },
  {
    category: 'luxury',
    count: 44,
    prompt: `Generate {count} luxury items for GLMZ, ranked {rankStart} to {rankEnd}. These are items that were ordinary in 2024 but precious in 2200 because real originals are rare. Include: real chocolate (actual cacao), real coffee beans, real leather goods, real paper books (physical novels, hand-bound), real cotton clothing, real wood furniture pieces, real honey, real vanilla extract, real olive oil, heritage seeds, analog watches, vinyl records. These are status symbols of authenticity in a synthetic world. Prices from Φ50 to Φ2000+.`
  },
];

// ── Main Generation Loop ──
async function generateCategory(catDef, existingNames) {
  const { category, count, prompt } = catDef;

  // Check how many we already have for this category
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

    // Refresh existing names to avoid duplicates across batches
    const allExisting = getExistingNames();

    const filledPrompt = prompt
      .replace('{count}', batchSize)
      .replace('{rankStart}', rankStart)
      .replace('{rankEnd}', rankEnd);

    const system = `You generate consumer product entries for the world of GLMZ. Return ONLY a JSON array of exactly ${batchSize} product objects. No explanation, no markdown fencing, just the JSON array.

${WORLD_CONTEXT}

Each product MUST have exactly these fields:
{
  "name": "Product Name",
  "type": "consumer_good",
  "category": "${category}",
  "subcategory": "more specific subcategory",
  "manufacturer": "Brand/Corp name",
  "description": "1-2 sentence product description",
  "flavor_profile": "what it tastes/smells/feels like (or N/A for non-consumables)",
  "tier_availability": "Tier 1-2|Tier 2-3|Tier 3-4|All tiers",
  "price": "Φ amount",
  "popularity_rank": number,
  "slogan": "advertising tagline",
  "cultural_context": "how people relate to this product in GLMZ society",
  "story_hooks": ["array of 2-3 narrative hooks for this product"]
}

CRITICAL: popularity_rank must be the product's rank WITHIN the ${category} category. Rank ${rankStart} to ${rankEnd} for this batch. Every product must have a unique rank. These are NOT the top products — rank accordingly (higher number = less popular but still notable).`;

    const user = `${filledPrompt}

EXISTING PRODUCT NAMES (DO NOT DUPLICATE ANY): ${allExisting.join(', ')}

Generate exactly ${batchSize} products ranked ${rankStart} to ${rankEnd} in the ${category} category. Return ONLY the JSON array.`;

    console.log(`  Batch: ranks ${rankStart}-${rankEnd} (${batchSize} products)...`);

    let retries = 0;
    while (retries < 3) {
      try {
        const result = await callClaude(system, user, 8192);
        const products = parseJsonArray(result);

        let saved = 0;
        for (const product of products) {
          // Enforce correct category and type
          product.type = 'consumer_good';
          product.category = category;
          if (saveProduct(product)) {
            saved++;
            generated++;
          }
        }
        console.log(`    Saved ${saved}/${products.length} products.`);
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

  console.log(`[${category}] Generated ${generated} new products.`);
  return generated;
}

async function main() {
  console.log('=== StreetSamurai Consumer Goods Generator ===');
  console.log(`Output: ${OUTPUT_DIR}`);
  console.log(`Target: 1024 products across ${CATEGORIES.length} categories\n`);

  const existingFiles = fs.readdirSync(OUTPUT_DIR).filter(f => f.endsWith('.json'));
  console.log(`Existing files: ${existingFiles.length}`);

  const totalTarget = CATEGORIES.reduce((s, c) => s + c.count, 0);
  console.log(`Total target: ${totalTarget}`);

  let totalGenerated = 0;

  for (const catDef of CATEGORIES) {
    const n = await generateCategory(catDef, []);
    totalGenerated += n;
  }

  const finalCount = fs.readdirSync(OUTPUT_DIR).filter(f => f.endsWith('.json')).length;
  console.log(`\n=== DONE ===`);
  console.log(`Total files in consumer_goods/: ${finalCount}`);
  console.log(`Generated this run: ${totalGenerated}`);
}

main().catch(e => {
  console.error('Fatal error:', e);
  process.exit(1);
});
