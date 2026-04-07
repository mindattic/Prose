// Entertainment music generator for StreetSamurai
// Generates 200 music JSON files in engine/data/entertainment/
// Run: node generate_entertainment_1.js
// Does NOT overwrite existing files.

const fs = require('fs');
const https = require('https');
const path = require('path');

const settings = JSON.parse(fs.readFileSync(
  path.join(process.env.LOCALAPPDATA, 'MindAttic', 'StreetSamurai', 'Settings.json'), 'utf8'));
const API_KEY = settings.ApiKey;
const MODEL = 'claude-sonnet-4-6';
const OUTPUT_DIR = path.join(__dirname, '..', 'engine', 'data', 'entertainment');
const WAIT_MS = 5000;
const sleep = ms => new Promise(r => setTimeout(r, ms));

if (!fs.existsSync(OUTPUT_DIR)) fs.mkdirSync(OUTPUT_DIR, { recursive: true });

function callClaude(system, user, maxTokens = 8192) {
  return new Promise((resolve, reject) => {
    let settled = false;
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
        if (settled) return;
        settled = true;
        clearTimeout(timer);
        try {
          const j = JSON.parse(data);
          if (j.content && j.content[0]) resolve(j.content[0].text);
          else reject(new Error(data.substring(0, 500)));
        } catch (e) { reject(e); }
      });
    });
    const timer = setTimeout(() => {
      if (settled) return;
      settled = true;
      req.destroy();
      reject(new Error('Request timed out after 180s'));
    }, 180000);
    req.on('error', e => {
      if (settled) return;
      settled = true;
      clearTimeout(timer);
      reject(e);
    });
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
  if (start === -1) throw new Error('No JSON array found in response');
  json = json.substring(start);

  // Try parsing as-is first
  try { return JSON.parse(json); } catch (e) { /* try repair */ }

  // Find last complete object by looking for }\n] or },\n  {
  // Truncated responses often cut off mid-object
  let lastGoodEnd = -1;
  let depth = 0;
  let inString = false;
  let escape = false;
  for (let i = 0; i < json.length; i++) {
    const ch = json[i];
    if (escape) { escape = false; continue; }
    if (ch === '\\' && inString) { escape = true; continue; }
    if (ch === '"') { inString = !inString; continue; }
    if (inString) continue;
    if (ch === '{' || ch === '[') depth++;
    if (ch === '}' || ch === ']') {
      depth--;
      if (depth === 1 && ch === '}') lastGoodEnd = i; // end of a top-level object in array
    }
  }

  if (lastGoodEnd > 0) {
    const repaired = json.substring(0, lastGoodEnd + 1) + '\n]';
    try { return JSON.parse(repaired); } catch (e) { /* fall through */ }
  }

  // Last resort: try to find the closing bracket
  const end = json.lastIndexOf(']');
  if (end > start) {
    try { return JSON.parse(json.substring(0, end + 1)); } catch (e) { /* fall through */ }
  }

  throw new Error('Could not parse JSON array from response');
}

function slugify(name) {
  return name.toLowerCase()
    .replace(/[^a-z0-9]+/g, '_')
    .replace(/^_+|_+$/g, '')
    .substring(0, 80);
}

function randomHex(len) {
  const chars = '0123456789abcdef';
  let result = '';
  for (let i = 0; i < len; i++) result += chars[Math.floor(Math.random() * 16)];
  return result;
}

function saveEntry(entry) {
  const slug = slugify(entry.name.slice(0, 60));
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
const WORLD_CONTEXT = `Setting: Meridian 88, years 2183-2226. A megacity in the Great Lakes corridor (Chicago-Milwaukee). Currency is Phi (Φ) — the QUANTA currency symbol. Society is tiered: Tier 1 "the Shelf" (poorest, most dangerous), Tier 2 (working class "the Circuit"), Tier 3 (middle), Tier 4 (corporate comfort), Tier 5 "the Spire" (ultra-elite).

CRITICAL — Ubiquitous Diaspora: By 2200, humanity is fully racially interbred. There is no "ethnic music" — all music is everyone's music. Musical traditions from every global culture blend freely. West African polyrhythm + East Asian pentatonic + South American percussion + Nordic drone + Middle Eastern quarter-tones. "World music" has no meaning when everything is heritage. Names reflect mixed heritage from unexpected global combinations.

Technology: BCI (brain-computer interfaces) are common. Neural feeds allow direct music streaming into the brain. Augmentation (cyberware/chrome) ranges from basic to military-grade. Geneware allows cosmetic and functional genetic modification. AI-composed music exists alongside human artists. Some music is designed for augmented perception — frequencies and layers only chrome-enhanced ears can hear.

Vantablack Media is the dominant media conglomerate, owning most major labels. Underground music is distributed through pirate neural feeds, physical media (retro vinyl, data chips), and encrypted mesh networks. The Shelf has a raw, angry, political music scene. The Circuit has blues, folk, comfort music for working people. The Spire has polished, expensive, exclusive music. Some cross-tier hits transcend class boundaries. Some music is banned or suppressed by corporate censors.

Music venues range from Shelf dive bars with makeshift stages, to Circuit dance halls and worker pubs, to Spire concert halls with holographic orchestras and neural-sync experiences. Pirate radio stations broadcast from rooftops and abandoned buildings. Underground clubs move locations to avoid corporate raids.

Names should sound REAL — not parodies. Band names, album titles, song names should feel authentic and lived-in, not like jokes or puns. Think how real band names work: evocative, sometimes abstract, sometimes literal.`;

// ── Schema template ──
const SCHEMA = `{
  "id": "<32-char hex string — generate a unique one>",
  "name": "Name",
  "type": "entertainment",
  "category": "band|album|song|genre|venue|label",
  "subcategory": "more specific (e.g. protest_song, neural_ambient, underground_club)",
  "aliases": ["any alternate names or abbreviations"],
  "description": "1-2 paragraphs describing this entry in vivid detail",
  "creator": "artist/band name (or 'various' for genres/venues, or founding entity for labels)",
  "distributor": "label name or 'self-released' or 'pirate_feed' or 'Vantablack Media subsidiary'",
  "tier_availability": "Tier 1+|Tier 2+|Tier 3+|Tier 4+|Tier 5 only|All tiers|Banned",
  "legality": "legal|restricted|banned|gray_market|underground_only",
  "genre": "genre name(s)",
  "medium": "neural_feed|audio|live|broadcast|physical|mixed",
  "audience": "who listens — be specific about tier and subculture",
  "cultural_impact": "1 paragraph on significance to Meridian 88 society",
  "known_fans": ["list of character types or factions who are fans"],
  "story_hooks": ["2-3 narrative hooks for how this could appear in a story"],
  "tags": ["entertainment", "music", "category_value", "other relevant tags"]
}`;

// ── Category Definitions ──
const CATEGORIES = [
  {
    category: 'band',
    count: 50,
    prompt: `Generate {count} bands/musical artists for Meridian 88, spanning 2183-2226.

Mix of:
- Shelf underground acts (raw, angry, political, anti-corporate)
- Circuit working-class bands (blues, folk, comfort, solidarity anthems)
- Spire elite artists (polished, expensive, exclusive, avant-garde)
- Cross-tier phenomena (artists who transcend class boundaries)
- Banned/suppressed acts (censored by Vantablack Media or corponation security)
- AI-generated or BCI-composed artists (some controversial, some celebrated)
- Solo artists and full bands

For each band/artist include: band name, genre, member names (with mixed heritage names reflecting Ubiquitous Diaspora), a vivid description of their sound and cultural significance. Some should be legendary (active in 2183-2200s), some current (2220s), some defunct but iconic. Include at least 3 neural-feed exclusive artists, 2 bands known for illegal live shows, and 2 AI-composed projects.`
  },
  {
    category: 'album',
    count: 50,
    prompt: `Generate {count} music albums for Meridian 88, spanning 2183-2226.

Some tied to bands from previous generation, some from new artists. Each album should have:
- Album title and artist
- Year of release (between 2183-2226)
- Genre classification
- Track listing with brief descriptions of 2-3 standout tracks
- Cultural context (was it banned? did it go viral? did it cause riots?)

Mix of: underground classics passed around on physical media, corporate-approved hits, neural-feed exclusive experiences, live recordings from legendary shows, AI-generated concept albums, protest compilations, Spire-exclusive luxury releases (limited to Tier 4+).`
  },
  {
    category: 'song',
    count: 50,
    prompt: `Generate {count} individual notable songs for Meridian 88, spanning 2183-2226.

These are the songs EVERYONE knows, or that specific subcultures treat as sacred. Mix of:
- Protest anthems (anti-corporate, anti-surveillance, workers' rights)
- Love songs (across tier boundaries, human-synthetic love, BCI-mediated emotion)
- Club bangers (Shelf dive bar staples, Circuit dance hall standards, Spire exclusive drops)
- Funeral/memorial standards (what plays at Shelf funerals vs Spire memorials)
- Work songs (sung/played in factories, on the docks, in maintenance tunnels)
- Children's songs/lullabies (what do kids in M88 grow up hearing?)
- Banned songs (why were they banned? who still plays them?)
- Neural-feed compositions (experienced directly through BCI, no traditional audio)

Each song should have artist, genre, year, and what makes it culturally significant.`
  },
  {
    category: 'genre',
    count: 30,
    prompt: `Generate {count} music genres unique to the 2183-2226 era of Meridian 88.

These are genres that could NOT exist before neural interfaces and augmented perception. Include:
- Neural-feed genres (music composed for direct brain stimulation, no audio component)
- BCI-composed genres (music created by thought alone, with distinctive qualities)
- AI-generated genres (algorithmic music with its own aesthetic movements)
- Augmented-perception genres (music with frequencies only chrome-enhanced ears can hear, layered compositions for different augment levels)
- Hybrid genres (traditional instruments + neural overlay)
- Tier-specific genres (genres that only exist in certain social tiers)
- Banned/suppressed genres (genres that are illegal because they exploit BCI vulnerabilities or cause involuntary emotional responses)

Each genre should have a vivid description of what it sounds/feels like, its origins, its cultural context, and who listens to it.`
  },
  {
    category: 'venue',
    count: 10,
    prompt: `Generate {count} music venues for Meridian 88.

Mix of:
- Shelf underground clubs (move locations, makeshift, dangerous, legendary)
- Circuit dance halls and worker pubs (stable, community-owned, tradition-rich)
- Spire concert halls (holographic orchestras, neural-sync seating, exclusive)
- Converted spaces (abandoned factories, rooftops, flooded basements, maintenance tunnels)
- Mobile venues (concert barges, rooftop to rooftop, drone-stage shows)

Each should have a name, location within M88, capacity, what kind of music they host, their reputation, and any notable events that happened there.`
  },
  {
    category: 'label',
    count: 10,
    prompt: `Generate {count} music labels/distribution networks for Meridian 88.

Mix of:
- Vantablack Media subsidiaries (corporate, controlling, well-funded, censorship-compliant)
- Independent labels (small, tier-specific, fighting for survival against corporate consolidation)
- Pirate radio stations (illegal broadcasts from rooftops and abandoned buildings, beloved by the Shelf)
- Underground distribution networks (encrypted mesh nets, physical media trading circles)
- AI-curated labels (algorithms that discover and promote artists, controversial among traditionalists)

Each should have a name, tier alignment, what kind of music they handle, their reputation, notable artists, and how they distribute music.`
  }
];

const BATCH = 5;

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

  let generated = 0;

  for (let i = 0; i < needed; i += BATCH) {
    const batchSize = Math.min(BATCH, needed - i);
    const batchNum = Math.floor(i / BATCH) + 1;
    const totalBatches = Math.ceil(needed / BATCH);

    console.log(`  Batch ${batchNum}/${totalBatches}: generating ${batchSize} ${category} entries...`);

    const existingByCatNow = getExistingByCategory();
    const allMusicNames = ['band', 'album', 'song', 'genre', 'venue', 'label']
      .flatMap(c => existingByCatNow[c] || []);

    const filledPrompt = prompt.replace('{count}', batchSize);

    const system = `You generate music/entertainment entries for the world of Meridian 88 (a cyberpunk megacity, years 2183-2226). Return ONLY a JSON array of exactly ${batchSize} objects. No explanation, no markdown fencing, just the JSON array.

${WORLD_CONTEXT}

Each entry MUST have exactly these fields:
${SCHEMA}

CRITICAL RULES:
- The "id" field must be a unique 32-character hexadecimal string (like a UUID without dashes).
- The "name" field must be 60 characters or fewer.
- The "type" field must always be "entertainment".
- The "category" field must always be "${category}".
- Names must sound REAL, not like parodies or jokes.
- Reflect Ubiquitous Diaspora in all names, member names, and cultural references.
- Φ is the QUANTA currency symbol, never the Greek letter phi.
- Vantablack Media owns most major labels.
- Tags array must always include "entertainment", "music", and "${category}".`;

    const user = `${filledPrompt}

EXISTING MUSIC NAMES (DO NOT DUPLICATE): ${allMusicNames.join(', ')}

Generate exactly ${batchSize} ${category} entries. Return ONLY the JSON array.`;

    let retries = 0;
    const MAX_RETRIES = 6;
    while (retries < MAX_RETRIES) {
      try {
        const result = await callClaude(system, user, 8192);
        const entries = parseJsonArray(result);

        let saved = 0;
        for (const entry of entries) {
          entry.type = 'entertainment';
          entry.category = category;
          if (!entry.id || entry.id.length !== 32) entry.id = randomHex(32);
          if (entry.name) entry.name = entry.name.slice(0, 60);
          if (!entry.tags) entry.tags = [];
          if (!entry.tags.includes('entertainment')) entry.tags.unshift('entertainment');
          if (!entry.tags.includes('music')) entry.tags.push('music');
          if (!entry.tags.includes(category)) entry.tags.push(category);
          if (saveEntry(entry)) {
            saved++;
            generated++;
          }
        }
        console.log(`    Saved ${saved}/${entries.length} entries.`);
        break;
      } catch (e) {
        retries++;
        const isOverloaded = e.message && e.message.includes('overloaded');
        const backoff = isOverloaded ? WAIT_MS * retries * 2 : WAIT_MS;
        console.error(`    Error (attempt ${retries}/${MAX_RETRIES}): ${e.message}`);
        if (retries < MAX_RETRIES) {
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
  console.log('=== StreetSamurai Entertainment Music Generator ===');
  console.log(`Output: ${OUTPUT_DIR}`);
  const totalTarget = CATEGORIES.reduce((s, c) => s + c.count, 0);
  console.log(`Target: ${totalTarget} entries across ${CATEGORIES.length} categories\n`);

  if (fs.existsSync(OUTPUT_DIR)) {
    const existingFiles = fs.readdirSync(OUTPUT_DIR).filter(f => f.endsWith('.json'));
    console.log(`Existing files: ${existingFiles.length}`);
  } else {
    console.log('Existing files: 0 (directory will be created)');
  }

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
