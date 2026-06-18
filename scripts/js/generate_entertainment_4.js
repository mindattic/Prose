// Entertainment generator batch 4 for StreetSamurai
// Generates 200 literary/art/cultural entries in engine/data/entertainment/
// Run: node generate_entertainment_4.js
// Does NOT overwrite existing files.

const fs = require('fs');
const https = require('https');
const path = require('path');
const crypto = require('crypto');

const settings = JSON.parse(fs.readFileSync(
  path.join(process.env.LOCALAPPDATA, 'MindAttic', 'StreetSamurai', 'Settings.json'), 'utf8'));
const API_KEY = settings.ApiKey;
const MODEL = 'claude-sonnet-4-6';
const OUTPUT_DIR = path.join(__dirname, '..', 'engine', 'data', 'entertainment');
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

function saveEntry(entry) {
  const slug = slugify(entry.name);
  const filePath = path.join(OUTPUT_DIR, `${slug}.json`);
  if (fs.existsSync(filePath)) {
    console.log(`    SKIP (exists): ${entry.name}`);
    return false;
  }
  entry.id = crypto.randomUUID().replace(/-/g, '');
  entry.name = entry.name.slice(0, 60);
  fs.writeFileSync(filePath, JSON.stringify(entry, null, 2));
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
    } catch (e) { /* skip bad files */ }
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

// ── World Context ──
const WORLD_CONTEXT = `Setting: GLMZ, years 2183-2226. A megacity in the Great Lakes corridor (Chicago-Milwaukee). Currency is Quanta (Φ). Society is tiered: Tier 1 (the Shelf — poorest, most dangerous), Tier 2 (working class), Tier 3 (middle), Tier 4 (corporate comfort), Tier 5 (the Spire — ultra-elite).

Ubiquitous Diaspora: By 2200, humanity is fully racially interbred. Culture is globally mixed. There is no dominant ethnicity or monoculture. Names, art, food, music, language — all draw from every human tradition freely blended. "Exotic" has no meaning when everything is heritage.

Technology: BCI (brain-computer interfaces) are common. Augmentation (cyberware/chrome) ranges from basic to military-grade. Geneware allows cosmetic and functional genetic modification. Neural feeds are the primary media consumption channel — content streams directly through BCI. Physical media (books, vinyl, canvas) exists as premium/countercultural artifacts.

CorpoNations are sovereign corporate entities that control most media production and distribution. Underground/pirate media thrives on the Shelf and in gray-market neural feed channels. Art and culture exist in tension between corporate-approved sanitized content and raw underground expression.

The Iowa Autonomous Zone is home to Iowan Behemoths — massive autonomous machines, NOT living beings. The Zone is a no-go area for most.`;

// ── Category Definitions ──
const CATEGORIES = [
  {
    category: 'book',
    count: 25,
    prompt: `Generate {count} books (novels, poetry collections, manifestos, banned texts, corporate-approved literature, underground zines) from the world of GLMZ, created between 2183-2226. Include a mix of: bestselling corporate-published novels, banned revolutionary texts that circulate on the Shelf, poetry collections (some neural-feed optimized, some physical-only), philosophical manifestos about augmentation/identity/consciousness, zines from underground scenes, children's literature for a chrome-and-geneware world. Authors should have globally mixed names (Ubiquitous Diaspora). Some books exist only as neural feed downloads, some as precious physical objects.`
  },
  {
    category: 'author',
    count: 15,
    prompt: `Generate {count} notable authors/writers from GLMZ, active between 2183-2226. Include: celebrated literary novelists, underground poets, banned revolutionary writers, corporate ghostwriters who went rogue, AI-collaborative authors, zine publishers from the Shelf, spoken-word artists who publish through neural feed. Each author has a distinct voice and cultural significance. Names reflect Ubiquitous Diaspora — globally mixed heritage. Some are household names, some are underground legends.`
  },
  {
    category: 'art',
    count: 18,
    prompt: `Generate {count} notable artworks or art movements from GLMZ, 2183-2226. Include: famous holographic installations, street art pieces that became legendary, neural-feed art experiences (you feel the art through BCI), corporate-commissioned public works, underground gallery exhibitions, augmented reality murals, bioart using geneware organisms, protest art, graffiti movements, chrome sculpture. Art should feel grounded and specific — not generic near-future.`
  },
  {
    category: 'artist',
    count: 12,
    prompt: `Generate {count} notable visual artists from GLMZ, active 2183-2226. Include: famous street artists, holographic sculptors, neural-feed experience designers, underground gallery owners who are also artists, corporate art directors who moonlight as subversive creators, bioartists working with geneware organisms, graffiti legends from the Shelf. Names reflect Ubiquitous Diaspora.`
  },
  {
    category: 'podcast',
    count: 18,
    prompt: `Generate {count} podcasts/audio shows from GLMZ, 2183-2226. In 2200, podcasts stream through neural feed or old-school audio. Include: pirate radio shows broadcasting from the Shelf, corporate-sponsored interview programs, underground political commentary, comedy shows, true crime about GLMZ's underworld, tech review shows about new chrome/geneware, philosophy discussions about consciousness and augmentation, storytelling shows, music critique. Some are massive with millions of neural-feed subscribers, some are tiny Shelf operations.`
  },
  {
    category: 'broadcast',
    count: 12,
    prompt: `Generate {count} broadcast programs/networks/channels from GLMZ, 2183-2226. These are the major media outlets — corporate news networks, entertainment channels, pirate broadcast stations, emergency information networks, Shelf community radio. Include the dominant corporate media voices AND the underground alternatives. Some broadcast through neural feed, some through legacy audio/video, some through both.`
  },
  {
    category: 'festival',
    count: 18,
    prompt: `Generate {count} festivals and annual cultural events from GLMZ, 2183-2226. Include: massive corporate-sponsored galas (Spire events, Tier 4-5), underground raves in abandoned Shelf infrastructure, annual street art festivals, music festivals spanning multiple tiers, Shelf community celebrations (harvest festivals for vertical farms, chrome appreciation days), geneware fashion shows, memorial events for historical tragedies, tech expos, food festivals celebrating synth cuisine innovation.`
  },
  {
    category: 'event',
    count: 12,
    prompt: `Generate {count} specific notable one-time cultural events from GLMZ, 2183-2226. These are moments that everyone remembers — the cultural touchstones. Include: a legendary concert, a controversial art exhibition, a broadcast that changed public opinion, a festival that ended in disaster, a corporate product launch that became a cultural phenomenon, a protest that became legendary, a sporting event everyone watched. Each should have a specific date/year.`
  },
  {
    category: 'meme',
    count: 18,
    prompt: `Generate {count} memes and viral content from GLMZ's neural feed, 2183-2226. These are things that went viral — catchphrases everyone knows, infamous moments, neural-feed clips that became universal references. Include: corporate PR disasters that became jokes, Shelf slang that went mainstream, viral neural-feed experiences, infamous BCI glitches that everyone references, absurd product failures, political gaffes, underground art that accidentally went corporate. Each should feel like something people actually reference in conversation.`
  },
  {
    category: 'trend',
    count: 12,
    prompt: `Generate {count} viral trends from GLMZ's neural feed, 2183-2226. These are behavioral trends, challenges, movements that swept through the population — the equivalent of TikTok trends, social movements, flash mobs. Include: BCI-based experiences people shared, geneware expression challenges, augmentation aesthetics movements, slang evolution, collective neural-feed art projects, protest movements that went viral. Each should have a clear peak year.`
  },
  {
    category: 'fashion',
    count: 20,
    prompt: `Generate {count} fashion trends/movements from GLMZ, 2183-2226. These are NOT individual clothing items but cultural movements expressed through clothing and appearance. Include: chrome minimalism (hiding augments under skin-tone covers), chrome maximalism (displaying all chrome proudly), geneware expression movements (tails as fashion, bioluminescent skin patterns), Shelf survival aesthetics that became high fashion, corporate uniform subversion, retro movements (2020s nostalgia), anti-scanning fashion, augment-integrated haute couture. Each trend has a peak period and cultural meaning.`
  },
  {
    category: 'cuisine',
    count: 20,
    prompt: `Generate {count} food/drink culture entries from GLMZ, 2183-2226. Include: famous restaurants (from Shelf street carts to Spire fine dining), signature drinks that define an era, food trends that swept the city, celebrity chefs, street food innovations, synth-cuisine breakthroughs, underground supper clubs, famous bars/lounges, food competitions, cuisine movements. Remember: real ingredients are luxury, synth-protein is baseline. The flavor palette is globally mixed — no single cuisine dominates. Prices in Φ (Quanta).`
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

    const system = `You generate entertainment and cultural entries for the world of GLMZ. Return ONLY a JSON array of exactly ${batchSize} objects. No explanation, no markdown fencing, just the JSON array.

${WORLD_CONTEXT}

Each entry MUST have exactly these fields:
{
  "name": "Entry Name (max 60 chars)",
  "type": "entertainment",
  "category": "${category}",
  "subcategory": "more specific subcategory",
  "creator": "Creator/originator name or organization",
  "description": "2-3 sentence description of this cultural artifact",
  "year": number between 2183 and 2226,
  "tier_association": "Tier 1|Tier 2|Tier 3|Tier 4|Tier 5|All tiers|Tier 1-2|Tier 3-4|Tier 4-5",
  "medium": "how this is consumed/experienced (neural feed, physical, holographic, audio, etc.)",
  "cultural_impact": "1-2 sentences on why this matters in GLMZ society",
  "controversy": "any controversy or tension around this (or null if none)",
  "status": "active|defunct|banned|underground|legendary|archived",
  "tags": ["array", "of", "relevant", "tags"],
  "story_hooks": ["array of 2-3 narrative hooks for stories involving this"]
}

CRITICAL: category must be exactly "${category}". Names must reflect Ubiquitous Diaspora — globally mixed heritage from unexpected combinations. Make entries feel REAL and specific, not generic genre parody.`;

    const user = `${filledPrompt}

EXISTING NAMES (DO NOT DUPLICATE ANY): ${allExisting.join(', ')}

Generate exactly ${batchSize} entries with category "${category}". Return ONLY the JSON array.`;

    console.log(`  Batch: ${batchSize} entries (${i + 1}-${i + batchSize} of ${needed})...`);

    let retries = 0;
    while (retries < 3) {
      try {
        const result = await callClaude(system, user, 8192);
        const entries = parseJsonArray(result);

        let saved = 0;
        for (const entry of entries) {
          entry.type = 'entertainment';
          entry.category = category;
          if (saveEntry(entry)) {
            saved++;
            generated++;
          }
        }
        console.log(`    Saved ${saved}/${entries.length} entries.`);
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

  console.log(`[${category}] Generated ${generated} new entries.`);
  return generated;
}

async function main() {
  console.log('=== StreetSamurai Entertainment Generator (Batch 4) ===');
  console.log(`Output: ${OUTPUT_DIR}`);

  const totalTarget = CATEGORIES.reduce((s, c) => s + c.count, 0);
  console.log(`Total target: ${totalTarget} entries across ${CATEGORIES.length} categories\n`);

  const existingFiles = fs.existsSync(OUTPUT_DIR)
    ? fs.readdirSync(OUTPUT_DIR).filter(f => f.endsWith('.json')).length
    : 0;
  console.log(`Existing files: ${existingFiles}`);

  let totalGenerated = 0;

  for (const catDef of CATEGORIES) {
    const n = await generateCategory(catDef);
    totalGenerated += n;
  }

  const finalCount = fs.readdirSync(OUTPUT_DIR).filter(f => f.endsWith('.json')).length;
  console.log(`\n=== DONE ===`);
  console.log(`Total files in entertainment/: ${finalCount}`);
  console.log(`Generated this run: ${totalGenerated}`);
}

main().catch(e => {
  console.error('Fatal error:', e);
  process.exit(1);
});
