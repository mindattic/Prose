// Entertainment generator batch 5: Subcultures & Social Phenomena
// Generates 200 JSON files in engine/data/entertainment/
// Run: node generate_entertainment_5.js
// Does NOT overwrite existing files.

const fs = require('fs');
const https = require('https');
const path = require('path');
const crypto = require('crypto');

process.on('uncaughtException', (err) => {
  console.error('Uncaught exception:', err.message);
});
process.on('unhandledRejection', (err) => {
  console.error('Unhandled rejection:', err.message || err);
});

const settings = JSON.parse(fs.readFileSync(
  path.join(process.env.LOCALAPPDATA, 'MindAttic', 'StreetSamurai', 'Settings.json'), 'utf8'));
const API_KEY = settings.ApiKey;
const MODEL = 'claude-sonnet-4-6';
const OUTPUT_DIR = path.join(__dirname, '..', 'engine', 'data', 'entertainment');
const WAIT_MS = 5000;
const RETRY_WAIT_MS = 15000;
const sleep = ms => new Promise(r => setTimeout(r, ms));

if (!fs.existsSync(OUTPUT_DIR)) fs.mkdirSync(OUTPUT_DIR, { recursive: true });

function callClaude(system, user, maxTokens = 8192) {
  return new Promise((resolve, reject) => {
    let settled = false;
    const finish = (err, val) => {
      if (settled) return;
      settled = true;
      if (err) reject(err); else resolve(val);
    };
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
      timeout: 180000
    }, res => {
      let data = '';
      res.on('data', chunk => data += chunk);
      res.on('end', () => {
        try {
          const j = JSON.parse(data);
          if (j.content && j.content[0]) finish(null, j.content[0].text);
          else finish(new Error(data.substring(0, 500)));
        } catch (e) { finish(e); }
      });
    });
    req.on('timeout', () => { req.destroy(); finish(new Error('Request timed out after 180s')); });
    req.on('error', e => finish(e));
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

// ── World Context ──
const WORLD_CONTEXT = `Setting: GLMZ, years 2183-2226. A megacity in the Great Lakes corridor (Chicago-Milwaukee). Currency is Phi (\u03A6) — the QUANTA currency symbol. Society is tiered: Tier 1 (the Shelf — poorest, most dangerous), Tier 2 (working class), Tier 3 (middle), Tier 4 (corporate comfort), Tier 5 (the Spire — ultra-elite).

Ubiquitous Diaspora: By 2200, humanity is fully racially interbred. There are no distinct ethnic groups — everyone is mixed heritage from unexpected global combinations. Names, cultural practices, and aesthetics blend freely across all traditions. "Exotic" has no meaning when everything is heritage.

Technology: BCI (brain-computer interfaces) are common. Augmentation (cyberware/chrome) ranges from basic to military-grade. Geneware allows cosmetic and functional genetic modification (tails, bioluminescence, fur, horns, wings that don't work). Synthetics are artificial beings. E.L.F.s (Electronic Life Forms) are digital intelligences. The Underworld is the deep net / dark web equivalent.

Corponations are sovereign corporate entities that function as nation-states. They control most infrastructure, media, and commerce. Street culture exists in resistance to and alongside corporate dominance.

The Shelf (Tier 1) has its own vibrant culture — art collectives, underground markets, ghost markets (pop-up illegal bazaars), and a rich tradition of making do with less. Shelf art is raw, accumulative, imperfect — graffiti not gallery.

Synthetic beings range from near-human to obviously artificial. Some pass as human, others embrace their synthetic nature. Synthetic appreciation societies exist. Synthetic awareness/rights is a contested social issue.`;

// ── Category Definitions ──
const CATEGORIES = [
  {
    category: 'subculture',
    count: 40,
    prompt: `Generate {count} subcultures and social movements for GLMZ (2183-2226). Include:
- Youth movements (anti-corporate, aug-positive, aug-rejection, identity-fluid)
- Aug-positive communities (chrome pride, competitive augmentation, body modification circles)
- Tech-abstinence groups (neo-Luddites, BCI refusers, analog purists, off-grid communes)
- Retro movements (people obsessed with specific past eras — 1990s, 2020s, pre-digital)
- Synthetic appreciation societies (groups that celebrate synthetic beings, advocate for synthetic rights)
- Shelf art collectives (raw street art, accumulative sculpture, found-object installation, guerrilla murals)
- Geneware subcultures (fur communities, bioluminescent ravers, tail culture, horn aesthetics)
- Underground music/art scenes that define identity for their members
Each must feel like a REAL subculture — with internal politics, fashion markers, slang, meeting spots, and social hierarchies.`
  },
  {
    category: 'platform',
    count: 30,
    prompt: `Generate {count} social media platforms and digital networks for GLMZ (2183-2226). Include:
- Neural-feed social networks (BCI-direct content sharing — thoughts, sensations, memories)
- Image/experience sharing platforms (like Instagram but for neural recordings)
- Dating platforms (matching by aug compatibility, geneware aesthetic, tier, BCI compatibility)
- Anonymous boards (Underworld-adjacent, unmoderated, where whistleblowers and criminals coexist)
- Reputation systems (social credit scores that affect tier mobility, employment, housing)
- Corporate-run platforms vs independent/underground alternatives
- Synthetic-specific social networks
Each platform should have a distinct identity, user base, and cultural significance. Some are tier-locked, some are universal.`
  },
  {
    category: 'celebrity',
    count: 15,
    prompt: `Generate {count} celebrities for GLMZ (2183-2226). Include:
- Neural-feed stars (people famous for sharing their experiences/emotions via BCI)
- Corporate spokespeople (the face of corponations — some willing, some contracted)
- Synthetic celebrities (artificial beings who became famous)
- Controversial public figures (activists, provocateurs, whistleblowers who became famous)
Each celebrity needs a name reflecting the Ubiquitous Diaspora (mixed heritage from unexpected global combinations), a rise-to-fame story, and current cultural status.`
  },
  {
    category: 'influencer',
    count: 15,
    prompt: `Generate {count} influencers and underground icons for GLMZ (2183-2226). Include:
- Underground icons (Shelf-famous, street-level legends, never corporate)
- Aug-fluencers (famous for their chrome modifications and body art)
- Geneware models (famous for extreme or beautiful genetic modifications)
- Tier-crossers (people who document life across different tiers)
- Underworld personalities (famous in the dark net, identity unknown or contested)
Each needs a name reflecting the Ubiquitous Diaspora, a platform/medium, and why people follow them.`
  },
  {
    category: 'phenomenon',
    count: 30,
    prompt: `Generate {count} urban phenomena for GLMZ (2183-2226). Include:
- Flash mobs (BCI-coordinated, sometimes political, sometimes just chaos)
- Ghost markets (pop-up illegal bazaars that appear and vanish — specific famous ones)
- Pop-up events (temporary experiences — rooftop concerts, abandoned-building galleries, sewer raves)
- Urban exploration groups (people who explore Old Chicago ruins, abandoned corporate infrastructure, sealed Shelf tunnels)
- Graffiti movements (specific styles, crews, turf wars, the art scene of the Shelf)
- Competitive parkour / freerunning leagues (aug-enhanced and natural divisions)
- Viral challenges and trends that sweep through the tiers
- Strange recurring phenomena that no one can fully explain`
  },
  {
    category: 'tradition',
    count: 15,
    prompt: `Generate {count} traditions for GLMZ (2183-2226). Include:
- Corporate-era holidays (mandatory celebration days created by corponations for productivity/morale)
- Remembrance days (marking disasters, wars, the founding of GLMZ, the Fall of old nations)
- Shelf community celebrations (grassroots holidays born from Tier 1 culture — raw, genuine, defiant)
- Synthetic awareness days (marking milestones in synthetic rights or synthetic-related events)
- Cross-tier traditions that everyone participates in differently depending on their tier
Each tradition should have a specific date or time of year, origin story, and how different tiers observe it.`
  },
  {
    category: 'holiday',
    count: 15,
    prompt: `Generate {count} holidays for GLMZ (2183-2226). Include:
- Official corporate holidays (days off mandated by corponation charters — some genuine, some cynical)
- Underground holidays (not officially recognized but widely observed on the Shelf)
- Memorial days for specific events in GLMZ history (2183-2226)
- Celebration days for technological milestones (first BCI, first synthetic awakening, etc.)
- Dark holidays (days that mark tragedies — observed quietly, some suppressed by corponations)
Each needs a name, date, origin, and how people actually observe it across tiers.`
  },
  {
    category: 'slang',
    count: 20,
    prompt: `Generate {count} slang terms and language trends for GLMZ (2183-2226). Include:
- Shelf-specific slang (Tier 1 expressions, often spreading upward through tiers)
- Corporate-speak that leaked into common usage (terms that started in boardrooms)
- Aug-related terminology (slang for types of chrome, augmentation states, glitches)
- BCI slang (terms for neural experiences, connection states, data sharing)
- Synthetic-related language (how people talk about/to synthetic beings)
- Tier-specific dialects and expressions (how Tier 1 speech differs from Tier 5)
- Insults, compliments, greetings that are unique to 2200
Each term needs the word/phrase, definition, usage example, tier of origin, and whether it has spread across tiers.`
  },
  {
    category: 'conspiracy',
    count: 20,
    prompt: `Generate {count} conspiracy theories and popular beliefs for GLMZ (2183-2226). Include:
- Corponation conspiracies (what people believe corps are secretly doing — some true, some not)
- Underworld myths (what the deep net supposedly contains or connects to)
- E.L.F. theories (fears and beliefs about emergent digital life forms)
- BCI paranoia (what people think BCIs really do to your brain — some concerns are valid)
- Synthetic conspiracies (are they replacing humans? do they have a secret network? are they truly conscious?)
- Historical cover-ups (what really happened during key events in GLMZ history)
- Tier conspiracy (is tier mobility actually possible or is it an illusion?)
For each, indicate whether it is TRUE, PARTIALLY TRUE, or FALSE — but the public doesn't know which.`
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

  const BATCH = 5;
  let generated = 0;

  for (let i = 0; i < needed; i += BATCH) {
    const batchSize = Math.min(BATCH, needed - i);
    const rankStart = existingInCat.length + i + 1;
    const rankEnd = existingInCat.length + i + batchSize;

    // Only pass category-specific names to avoid oversized prompts
    const refreshedByCat = getExistingByCategory();
    const categoryNames = refreshedByCat[category] || [];

    const filledPrompt = prompt
      .replace('{count}', batchSize);

    const system = `You generate social and cultural entries for the world of GLMZ. Return ONLY a JSON array of exactly ${batchSize} objects. No explanation, no markdown fencing, just the JSON array.

${WORLD_CONTEXT}

Each entry MUST have exactly these fields:
{
  "id": "32-char hex UUID",
  "name": "Entry Name (max 60 chars)",
  "type": "entertainment",
  "category": "${category}",
  "subcategory": "more specific subcategory",
  "description": "2-4 sentence description with specific details, names, locations",
  "origin_year": number between 2183 and 2226,
  "tier_association": "Tier 1|Tier 1-2|Tier 2-3|Tier 3-4|Tier 4-5|All tiers",
  "status": "active|growing|declining|underground|suppressed|defunct",
  "cultural_impact": "1-2 sentences on how this affects daily life in GLMZ",
  "key_figures": ["1-3 names of notable people/entities associated with this"],
  "locations": ["1-3 specific places in GLMZ where this is centered"],
  "corporate_stance": "endorsed|tolerated|monitored|opposed|co-opted|unaware",
  "story_hooks": ["3 narrative hooks for stories involving this entry"],
  "tags": ["6-10 relevant tags for graph DB connectivity"]${category === 'slang' ? `,
  "term": "the actual slang word or phrase",
  "definition": "what it means",
  "usage_example": "example sentence using the term",
  "tier_of_origin": "which tier it originated from",
  "spread": "how far it has spread across tiers"` : ''}${category === 'conspiracy' ? `,
  "truth_status": "true|partially_true|false",
  "believers": "who tends to believe this",
  "evidence": "what evidence exists for or against"` : ''}${category === 'celebrity' || category === 'influencer' ? `,
  "real_name": "their actual name (Ubiquitous Diaspora naming)",
  "platform": "primary platform or medium",
  "follower_count": "approximate neural-feed follower count",
  "controversy": "their biggest controversy or defining moment"` : ''}${category === 'tradition' || category === 'holiday' ? `,
  "date": "when it occurs (month/day or season)",
  "origin_event": "what event or reason created this tradition",
  "tier_observations": "how each tier observes this differently"` : ''}${category === 'platform' ? `,
  "user_base": "approximate user count and primary demographic",
  "monetization": "how the platform makes money",
  "notable_feature": "what makes this platform unique"` : ''}
}

CRITICAL RULES:
- Names must be max 60 characters
- IDs must be 32-character lowercase hex strings (like UUIDs without dashes)
- The currency symbol \u03A6 is QUANTA, never Greek phi
- All names should reflect the Ubiquitous Diaspora — mixed heritage from unexpected global combinations
- Make entries feel REAL, grounded, and specific — not generic genre parody
- Span origin years across 2183-2226`;

    const user = `${filledPrompt}

EXISTING NAMES IN THIS CATEGORY (DO NOT DUPLICATE): ${categoryNames.join(', ')}

Generate exactly ${batchSize} entries for the ${category} category. Return ONLY the JSON array.`;

    console.log(`  Batch: ${batchSize} entries (${rankStart}-${rankEnd})...`);

    let retries = 0;
    while (retries < 5) {
      try {
        const result = await callClaude(system, user, 6000);
        const entries = parseJsonArray(result);

        let saved = 0;
        for (const entry of entries) {
          // Enforce correct type and category
          entry.type = 'entertainment';
          entry.category = category;
          // Enforce name length
          if (entry.name && entry.name.length > 60) {
            entry.name = entry.name.slice(0, 60);
          }
          // Ensure valid id
          if (!entry.id || entry.id.length !== 32) {
            entry.id = crypto.randomBytes(16).toString('hex');
          }
          if (saveEntry(entry)) {
            saved++;
            generated++;
          }
        }
        console.log(`    Saved ${saved}/${entries.length} entries.`);
        break;
      } catch (e) {
        retries++;
        const backoff = RETRY_WAIT_MS * retries;
        console.error(`    Error (attempt ${retries}/5): ${e.message}`);
        if (retries < 5) {
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
  console.log('=== StreetSamurai Entertainment Generator (Batch 5: Subcultures & Social Phenomena) ===');
  console.log(`Output: ${OUTPUT_DIR}`);
  const totalTarget = CATEGORIES.reduce((s, c) => s + c.count, 0);
  console.log(`Target: ${totalTarget} entries across ${CATEGORIES.length} categories\n`);

  if (fs.existsSync(OUTPUT_DIR)) {
    const existingFiles = fs.readdirSync(OUTPUT_DIR).filter(f => f.endsWith('.json'));
    console.log(`Existing files: ${existingFiles.length}`);
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
