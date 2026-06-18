// Entertainment generator (batch 3) for StreetSamurai
// Generates 200 gaming & sports entries as JSON in engine/data/entertainment/
// Run: node generate_entertainment_3.js
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

// -- World Context --
const WORLD_CONTEXT = `Setting: GLMZ, years 2183-2226. A megacity in the Great Lakes corridor (Chicago-Milwaukee). Currency is Phi (\u03A6) — this is QUANTA, not the Greek letter. Society is tiered: Tier 1 (the Shelf — poorest, most dangerous), Tier 2 (working class), Tier 3 (middle), Tier 4 (corporate comfort), Tier 5 (the Spire — ultra-elite).

Technology: BCI (brain-computer interfaces) are common. Augmentation (cyberware/chrome) ranges from basic to military-grade. Geneware allows cosmetic and functional genetic modification. Synth-protein is the primary food source for Tiers 1-3.

CorpoNations are sovereign corporate entities. They sponsor leagues, manufacture entertainment products, and run legal gambling. Underground scenes thrive on the Shelf and in Tier 2 districts.

Ubiquitous Diaspora: By 2200, humanity is fully racially interbred. Names, cultures, and traditions blend freely across all global origins. Default to mixed heritage from unexpected global combinations.

Iowan Behemoths are autonomous machines, NOT synthetic life. They are not alive.

IMPORTANT: All entries must have a "year" field set between 2183 and 2226. Spread entries across this full range. Products, teams, events should feel grounded in specific years within this timeline.`;

// -- Category Definitions --
const CATEGORIES = [
  {
    category: 'game',
    count: 50,
    prompt: `Generate {count} video games / neural games for GLMZ, spanning 2183-2226. Include:
- BCI-integrated games (full neural immersion, you ARE the character, sensory feedback)
- Full-immersion neural sims (live entire alternate lives, historical periods, fantasy worlds)
- AR games played in real physical space (citywide manhunt games, territory capture, urban exploration challenges)
- Competitive esports titles (corporate-sponsored leagues, neural reflex competitions, team-based combat sims)
- Underground game circuits (illegal combat sims, reality-bleed games that push BCI safety limits)
Mix AAA CorpoNation releases with indie neural devs and underground mods. Some games are cultural phenomena, others are cult classics. Include the year each game launched.`
  },
  {
    category: 'sport',
    count: 30,
    prompt: `Generate {count} sports / sporting leagues for GLMZ, spanning 2183-2226. Include:
- Augmented sports leagues (chrome-enhanced athletes, no limits on augmentation, superhuman feats)
- Zero-aug purist leagues (strictly unaugmented athletes, viewed as either noble or quaint)
- Automaton fighting (Iowan Behemoths and other autonomous machines in arena combat — these are MACHINES, not alive)
- Underground bloodsports (illegal, brutal, sometimes lethal — Tier 1 entertainment)
- Corporate-sponsored tournaments (massive viewership, betting markets, corporate team ownership)
- Hybrid sports that blend physical and neural competition
Each sport should have a founding year, governing body or lack thereof, and tier accessibility.`
  },
  {
    category: 'team',
    count: 15,
    prompt: `Generate {count} sports teams for GLMZ, spanning 2183-2226. Include:
- Corporate-owned augmented league teams (like modern NFL/Premier League but with chrome)
- Purist league teams (zero-aug, traditional)
- Underground fighting stables
- Automaton fighting teams/workshops
Each team needs: founding year, sport/league, home district in GLMZ, owner or sponsor (CorpoNation or independent), notable achievements, rivalries. Team names should feel like real sports teams — not jokes.`
  },
  {
    category: 'athlete',
    count: 15,
    prompt: `Generate {count} notable athletes for GLMZ, spanning 2183-2226. Include:
- Augmented superstars (chrome-enhanced, corporate-sponsored, celebrity status)
- Purist legends (unaugmented athletes who compete at near-aug levels through geneware-free training)
- Controversial figures (doping scandals but with augmentation — illegal chrome, hidden geneware advantages)
- Retired legends and rising stars
- Underground bloodsport survivors
Each athlete needs: full name (Ubiquitous Diaspora — mixed heritage), sport, active years, team affiliation, augmentations (if any), career highlights, controversies. Names from unexpected global combinations.`
  },
  {
    category: 'gambling',
    count: 30,
    prompt: `Generate {count} gambling / betting operations and products for GLMZ, spanning 2183-2226. Include:
- Legal corporate gambling platforms (BCI-integrated casino experiences, neural poker, virtual race betting)
- Underground fighting ring betting operations (Tier 1 bookmakers, the Shelf's economy runs on this)
- Automaton vs human betting markets (machine fighting odds, cross-category matchups)
- Prediction markets (bet on CorpoNation stock moves, political outcomes, weather events, crime statistics)
- Lottery systems (corporate-run, district-level, underground number games)
- Gambling dens and physical locations (bars, backrooms, floating games)
Each needs: type of operation, legality, typical stakes, who runs it, tier availability.`
  },
  {
    category: 'tabletop',
    count: 30,
    prompt: `Generate {count} board games / card games / tabletop games for GLMZ, spanning 2183-2226. Include:
- AR-overlay board games (physical pieces + augmented reality layers visible through BCI or goggles)
- Deliberately analog games (no tech, played as rebellion against constant connectivity — popular in Shelf bars)
- Card games played in bars and gambling dens (some with smart-ink cards that shift)
- Strategy games based on CorpoNation warfare and territory control
- Children's games that have dark undertones reflecting GLMZ society
- Collectible card games with physical and neural components
- Dice games, tile games, abstract strategy
Each needs: year introduced, player count, typical setting where it's played, cultural significance. Some should be ancient games that survived, others brand new.`
  },
  {
    category: 'recreation',
    count: 30,
    prompt: `Generate {count} recreational activities / venues for GLMZ, spanning 2183-2226. Include:
- VR arcades (full-body immersion pods, group experiences, competitive arenas)
- Sensory parlors (BCI-driven experiences: taste memories, emotional replays, synesthetic journeys)
- Experience clubs (pay to live someone else's recorded day — a Spire executive, a Shelf runner, a historical figure)
- Thrill-seeking activities (urban climbing on GLMZ megastructures, storm-chasing in the Wastes, underground racing)
- Social recreation (dance clubs with neural-sync, communal dream spaces, memory sharing circles)
- Fitness and wellness (aug-compatible gyms, neural meditation centers, combat training dojos)
Each needs: year established or popularized, tier accessibility, typical cost, location type, legal status.`
  },
];

// -- Main Generation Loop --
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
      .replace('{count}', batchSize);

    const system = `You generate entertainment entries for the world of GLMZ. Return ONLY a JSON array of exactly ${batchSize} objects. No explanation, no markdown fencing, just the JSON array.

${WORLD_CONTEXT}

Each entry MUST have exactly these fields:
{
  "name": "Entry Name (max 60 chars)",
  "type": "entertainment",
  "category": "${category}",
  "subcategory": "more specific subcategory",
  "year": number (between 2183 and 2226),
  "description": "2-3 sentence description with vivid worldbuilding detail",
  "tier_availability": "Tier 1|Tier 1-2|Tier 2-3|Tier 3-4|Tier 4-5|All tiers",
  "price": "\u03A6 amount or range (use \u03A6 symbol for QUANTA currency)",
  "popularity_rank": number (${rankStart} to ${rankEnd} within this category),
  "legal_status": "legal|gray_market|illegal|varies_by_district",
  "corporate_ties": "associated CorpoNation(s) or 'independent' or 'underground'",
  "cultural_context": "how this fits into GLMZ society, who engages with it and why",
  "controversy": "any scandal, danger, or debate surrounding this (or null if none)",
  "story_hooks": ["array of 2-3 narrative hooks for stories"]
}

CRITICAL: popularity_rank must be ${rankStart} to ${rankEnd}. Every entry must have a unique rank. Spread years across 2183-2226. Names max 60 characters.`;

    const user = `${filledPrompt}

EXISTING ${category.toUpperCase()} NAMES (DO NOT DUPLICATE ANY OF THESE — generate completely different names):
${allExisting.filter(n => {
      const existingByCatNow = getExistingByCategory();
      return (existingByCatNow[category] || []).includes(n);
    }).join('\n')}

Generate exactly ${batchSize} NEW and UNIQUE entries ranked ${rankStart} to ${rankEnd} in the ${category} category. Every name MUST be different from the list above. Return ONLY the JSON array.`;

    console.log(`  Batch: ranks ${rankStart}-${rankEnd} (${batchSize} entries)...`);

    let retries = 0;
    const MAX_RETRIES = 8;
    while (retries < MAX_RETRIES) {
      try {
        const result = await callClaude(system, user, 8192);
        const entries = parseJsonArray(result);

        let saved = 0;
        for (const entry of entries) {
          entry.type = 'entertainment';
          entry.category = category;
          entry.name = entry.name.slice(0, 60);
          if (saveEntry(entry)) {
            saved++;
            generated++;
          }
        }
        console.log(`    Saved ${saved}/${entries.length} entries.`);
        break;
      } catch (e) {
        retries++;
        const backoff = Math.min(WAIT_MS * Math.pow(2, retries - 1), 60000);
        console.error(`    Error (attempt ${retries}/${MAX_RETRIES}): ${e.message.substring(0, 120)}`);
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
  console.log('=== StreetSamurai Entertainment Generator (Batch 3) ===');
  console.log(`Output: ${OUTPUT_DIR}`);

  const totalTarget = CATEGORIES.reduce((s, c) => s + c.count, 0);
  console.log(`Total target: ${totalTarget} entries across ${CATEGORIES.length} categories\n`);

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
