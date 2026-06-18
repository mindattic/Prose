// Entertainment generator (batch 2) for StreetSamurai
// Generates 200 film/TV/entertainment JSON files in engine/data/entertainment/
// Run: node generate_entertainment_2.js
// Does NOT overwrite existing files.

const fs = require('fs');
const https = require('https');
const path = require('path');

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
    .substring(0, 80);
}

function saveEntry(entry) {
  entry.name = entry.name.slice(0, 60);
  const slug = slugify(entry.name);
  const filePath = path.join(OUTPUT_DIR, `${slug}.json`);
  if (fs.existsSync(filePath)) {
    console.log(`    SKIP (exists): ${entry.name}`);
    return false;
  }
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
const WORLD_CONTEXT = `Setting: GLMZ, years 2183-2226. A megacity in the Great Lakes corridor (Chicago-Milwaukee). Currency is Phi (\u03A6) — the QUANTA currency symbol. Society is tiered: Tier 1 (the Shelf — poorest, most dangerous), Tier 2 (working class), Tier 3 (middle), Tier 4 (corporate comfort), Tier 5 (the Spire — ultra-elite).

Vantablack Media controls 73% of all licensed neural-feed content. They are the dominant entertainment CorpoNation. Their subsidiaries produce most mainstream content. Independent studios operate in the margins, sometimes illegally.

CorpoNations are sovereign corporate entities with their own laws. Some content is banned by specific CorpoNations — watching it in their territory is a crime. Underground content circulates via dead drops, pirate mesh networks, and physical media.

Technology: BCI (brain-computer interfaces) are common. Neural-feed entertainment lets you LIVE as the character — full sensory immersion. Holographic projection is standard for Tier 3+. Flat-screen is retro/cheap (Tier 1-2). Underground broadcasts use encrypted mesh networks.

Ubiquitous Diaspora: By 2200+, humanity is fully racially interbred. Mixed heritage from unexpected global combinations is the norm. Characters should reflect this — names and appearances draw from every culture freely.`;

// ── Schema description ──
const SCHEMA_DESC = `Each entry MUST have exactly these fields:
{
  "name": "Title or Name (max 60 chars)",
  "type": "entertainment",
  "category": "movie|show|documentary|personality|studio|network",
  "subcategory": "more specific (e.g. blockbuster, indie, propaganda, neural_drama, reality, news, expose, corporate_funded, actor, director, vantablack_subsidiary, independent, pirate_channel)",
  "medium": "neural_feed|holographic|flat_screen|broadcast|underground",
  "year": number (2183-2226),
  "description": "2-3 sentence description of the content/entity",
  "corporate_status": "licensed|independent|banned|underground|corporate_owned",
  "associated_corp": "name of associated CorpoNation or 'Independent' or 'Various'",
  "tier_availability": "Tier 1-2|Tier 2-3|Tier 3-4|Tier 4-5|All tiers|Underground only",
  "controversy": "any controversy or banned status, or null",
  "cultural_impact": "1 sentence on how this affected GLMZ culture",
  "story_hooks": ["array of 2-3 narrative hooks"]
}`;

// ── Category Definitions ──
const CATEGORIES = [
  {
    category: 'movie',
    count: 60,
    prompt: `Generate {count} MOVIES for the GLMZ entertainment landscape, spanning years 2183-2226. Include a diverse mix:
- Blockbusters: Vantablack Media tentpoles, massive neural-feed spectacles, CorpoNation-funded epics
- Indie films: Low-budget flat-screen or basic neural-feed, often politically charged
- Propaganda films: Corporate-commissioned content glorifying CorpoNation life, demonizing runners/street life
- Banned films: Content outlawed in specific CorpoNation territories — showing corporate atrocities, exposing experiments, documenting the Shelf
- Art house: Experimental neural-feed that pushes the medium (you experience synesthesia, time dilation, perspective shifts)

Some movies are neural-feed (you ARE the protagonist), some are holographic (3D projection), some are flat-screen (retro/Shelf). Mix corporate-approved with underground. Some are legendary — the "Citizen Kane" or "Blade Runner" of GLMZ. Some are trash that everyone watched anyway.`
  },
  {
    category: 'show',
    count: 60,
    prompt: `Generate {count} TV/NEURAL-FEED SHOWS for GLMZ, spanning years 2183-2226. Include:
- Serialized dramas: Crime dramas set on the Shelf, corporate intrigue in the Spire, runner crew adventures
- Reality shows: "Augment Swap" (trade chrome for a week), "Shelf Life" (Spire residents try to survive Tier 1), corpo dating shows
- News programs: Corporate-owned news (propaganda), independent news (dangerous), pirate news (underground mesh broadcasts)
- Corporate propaganda shows: Cheerful content about how great CorpoNation life is, subtly discouraging independent thought
- Underground broadcasts: Pirate shows that air on mesh networks, exposing corporate crimes, teaching self-defense against CorpoNation security

Some shows are neural-feed experiences (you LIVE as a character for the episode). Some have been running for decades. Some were cancelled after the corp found out what they were really saying.`
  },
  {
    category: 'documentary',
    count: 30,
    prompt: `Generate {count} DOCUMENTARIES for GLMZ, spanning years 2183-2226. Include:
- Corporate-funded whitewashes: "The Benevolent Hand: How [Corp] Saved GLMZ" — slick, well-produced lies
- Underground exposes: Dangerous to own, dangerous to watch, documenting corporate experiments on Shelf populations, illegal chrome testing, forced substrate migration
- Historical: Documenting the rise of CorpoNation sovereignty, the collapse of nation-states, the Great Lakes corridor formation
- Scientific: BCI development history, geneware ethics debates, the synthetic food revolution
- Cultural: The death of ethnic cuisine as a concept, the evolution of language in GLMZ, street art movements

Some documentaries got their makers killed. Some are required viewing in corporate orientation. Some circulate only on physical media because streaming them triggers CorpoNation surveillance.`
  },
  {
    category: 'personality',
    count: 25,
    prompt: `Generate {count} notable ACTORS, DIRECTORS, and entertainment PERSONALITIES for GLMZ, active between 2183-2226. Include:
- Corporate-owned talent: Actors under exclusive Vantablack contracts, unable to work outside the corp
- Independent auteurs: Directors who refuse corporate money, work in flat-screen or underground neural-feed
- Neural-feed stars: Actors whose neural signatures are so distinctive that audiences crave their "feel" — their emotional texture in neural-feed is unmistakable
- Controversial figures: Performers who crossed CorpoNation lines, disappeared, or went underground
- Legacy figures: The legends — directors who defined neural-feed cinema, actors who became icons

Remember Ubiquitous Diaspora — names and heritage should be globally mixed. Some personalities are human, some have notable geneware or chrome. For category use "personality". For medium, use the medium they primarily work in.`
  },
  {
    category: 'studio',
    count: 15,
    prompt: `Generate {count} STUDIOS for GLMZ's entertainment industry, active between 2183-2226. Include:
- Vantablack subsidiaries: Studios owned by Vantablack Media that produce specific genres (horror neural-feed, corporate training content, children's programming)
- Independent studios: Smaller operations producing content outside corporate control, often at legal risk
- Underground production houses: Illegal studios producing banned content, operating from Shelf basements and abandoned infrastructure

For category use "studio". For medium, use the primary medium they produce content for. Studios should have distinct identities — what they are known for, their reputation, their relationship with CorpoNation power.`
  },
  {
    category: 'network',
    count: 10,
    prompt: `Generate {count} NETWORKS and distribution channels for GLMZ, active between 2183-2226. Include:
- Vantablack-owned broadcast networks: The dominant neural-feed and holographic channels
- Independent networks: Smaller channels surviving on niche content and specific tier audiences
- Pirate channels: Underground mesh-network broadcasters, moving frequencies to avoid CorpoNation jamming

For category use "network". For medium, use their primary distribution medium. Networks control what people see — or in the case of pirate channels, what CorpoNations don't want them to see.`
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

    const system = `You generate entertainment entries for the world of GLMZ. Return ONLY a JSON array of exactly ${batchSize} entries. No explanation, no markdown fencing, just the JSON array.

${WORLD_CONTEXT}

${SCHEMA_DESC}

CRITICAL: "year" must be between 2183 and 2226. Distribute years across the full range. Every entry must be unique and feel authentic — not parody. Names should sound like real movie/show/studio names, not jokes.`;

    const user = `${filledPrompt}

EXISTING NAMES (DO NOT DUPLICATE ANY): ${allExisting.join(', ')}

Generate exactly ${batchSize} entries. Return ONLY the JSON array.`;

    console.log(`  Batch: ${batchSize} ${category} entries...`);

    let retries = 0;
    while (retries < 6) {
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
        console.error(`    Error (attempt ${retries}/6): ${e.message}`);
        if (retries < 6) {
          const backoff = WAIT_MS * (retries + 1);
          console.log(`    Retrying in ${backoff / 1000}s...`);
          await sleep(backoff);
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
  console.log('=== StreetSamurai Entertainment Generator (Batch 2) ===');
  console.log(`Output: ${OUTPUT_DIR}`);

  const totalTarget = CATEGORIES.reduce((s, c) => s + c.count, 0);
  console.log(`Target: ${totalTarget} entries across ${CATEGORIES.length} categories\n`);

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
