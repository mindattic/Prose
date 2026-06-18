// Fringe Rumors Generator
// Generates 100 rumors and urban legends about things OUTSIDE or on the FRINGES of GLMZ.
// Run: node generate_fringe_rumors.js
// Resume-safe: skips existing files (by slug filename).

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
const OUTPUT_DIR = path.join(__dirname, '..', 'engine', 'data', 'documents');
const WAIT_MS = 5000;
const RETRY_WAIT_MS = 20000;
const sleep = ms => new Promise(r => setTimeout(r, ms));

if (!fs.existsSync(OUTPUT_DIR)) fs.mkdirSync(OUTPUT_DIR, { recursive: true });

function genId() {
  return crypto.randomBytes(16).toString('hex');
}

function slugify(name) {
  return name
    .toLowerCase()
    .replace(/[^a-z0-9]+/g, '_')
    .replace(/^_+|_+$/g, '')
    .substring(0, 80);
}

function saveRumor(rumor) {
  const slug = slugify(rumor.name);
  const filePath = path.join(OUTPUT_DIR, `${slug}.json`);
  if (fs.existsSync(filePath)) {
    console.log(`    SKIP (exists): ${rumor.name}`);
    return false;
  }
  fs.writeFileSync(filePath, JSON.stringify(rumor, null, 2));
  console.log(`    WROTE: ${slug}.json`);
  return true;
}

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
      temperature: 0.95,
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
  return JSON.parse(json.substring(start, end + 1));
}

// ─── WORLD CONTEXT ────────────────────────────────────────────────────────────

const WORLD_CONTEXT = `Setting: GLMZ (Great Lakes Megacity Zone), year ~2200. A massive megacity stretching from Chicago/Lake Michigan westward 500km, population 100 million. Currency is Φ (QUANTA — never the Greek letter phi). Society is tiered 1-5.

The world OUTSIDE GLMZ:
- Missouri: partially flooded inland sea/wetlands since the 2140s levee cascade. Platform villages on stilts, houseboats, drowned cities. People arrive in GLMZ damp and quiet.
- Kentucky: "lost" since 2198. Uniform green vegetation overtook everything. Four expeditions sent; the fourth didn't return. No thermal signatures of humans. Status: "No entry authorized."
- Central Michigan: vast semi-empty buffer zone between GLMZ eastern edge and Detroit (its own separate megacity). No major cities — small communities, ruins, things that grow in empty spaces.
- Interior Wisconsin: rural, isolated, disconnected from GLMZ culture for 200 years. Developed its own customs, changed language, strange evolution.
- Upper Peninsula of Michigan: isolated, harsh, almost a separate nation. Cultural cousins to the 13 Tribes of Alaska.
- Southern Canada (Ontario, Manitoba): formally sovereign but practically tangled with GLMZ economically. Automated federal government, uncanny perfect maintenance, no visible humans.
- St. Paul/Minneapolis (Twin Cities corridor): once midwestern normal, now a separate cultural zone developed independently for 200 years.
- Iowa: automated farm zone controlled by Iowan Behemoths — autonomous machines (NOT alive, not synthetic life). The machines run the farms. Towns serve the machines without the machines noticing.
- The Great Plains: empty in a different way than before. Something happened out there.
- Federal Remnant in Denver: a scarecrow government. People laugh at it but sometimes it matters.
- Lake Michigan itself: 200 years of industrial runoff and strange biology. Something is in the lake.

People in GLMZ: Ubiquitous Diaspora — everyone is mixed heritage from unexpected global combinations. Names blend freely across all traditions. No distinct ethnic groups.

Key facts: No city police in GLMZ (Arcturus Civil Security is closest thing, Meridian PD dissolved 2208). Tier 1 = Shelf (poorest, darkest). Tier 5 = Spires (ultra-elite). BCI = brain-computer interfaces, common. CorpoNations are sovereign corporate nation-states.`;

// ─── BATCH DEFINITIONS ────────────────────────────────────────────────────────

const BATCHES = [
  {
    id: 1,
    label: 'Central Michigan Buffer Zone',
    count: 10,
    prompt: `Generate exactly 10 rumors and urban legends about Central Michigan — the vast semi-empty buffer zone between GLMZ's eastern edge and Detroit. This is not wilderness and not city. Small communities, ruins, things that grow in empty spaces. 200 years of being between two megacities. What lives there? What do people whisper about those communities? What comes out of the buffer zone and into GLMZ?

Tones: eerie, practical, human. Some rumors are practical (freight routes, communities to trade with, dangers). Some are deeply strange. Some are mundane-strange (communities with very specific rules, crops no one can identify, roads that don't appear on maps). A few can involve the Behemoths (Iowa's machines) occasionally straying east.`
  },
  {
    id: 2,
    label: 'Wisconsin Interior Legends',
    count: 10,
    prompt: `Generate exactly 10 rumors and urban legends about interior Wisconsin — rural communities that turned inward for 200 years of disconnection from GLMZ culture. They developed their own customs, changed language, kept some things and abandoned others. What do GLMZ residents whisper about Wisconsin? What do Wisconsin travelers say about themselves when they arrive in the city? Mix: strange customs that became sacred, language drift, things that were preserved perfectly for 200 years, things that mutated past recognition.`
  },
  {
    id: 3,
    label: 'Upper Peninsula Deep North Legends',
    count: 10,
    prompt: `Generate exactly 10 rumors and urban legends about the Upper Peninsula of Michigan and the deep north — isolation, brutal winters, near-total disconnection from GLMZ, cultural cousins to the 13 Tribes of Alaska. Almost a separate nation. What do GLMZ people say about the UP? What comes down from there? What do people from the UP say about the south (GLMZ)? Mix: survival culture, spiritual practices shaped by extreme isolation, things that came down from Alaska and took root, things that only exist in places where winter lasts 7 months.`
  },
  {
    id: 4,
    label: 'Southern Canada Border Weirdness',
    count: 10,
    prompt: `Generate exactly 10 rumors and urban legends about southern Canada — formally sovereign but practically economically entangled with GLMZ. Automated federal government that runs perfectly with no humans visible. Uncanny maintenance, perfect infrastructure, voices that welcome you by name before you've introduced yourself. Borders that know things. Towns where lights are on and no one is home. What do GLMZ freelancers say about working in Canada? What rumors circulate about what happened to the humans? Some rumors: hopeful, some unsettling, some purely practical for people trying to disappear.`
  },
  {
    id: 5,
    label: 'St. Paul / Minneapolis Divergence',
    count: 10,
    prompt: `Generate exactly 10 rumors and urban legends about the Twin Cities corridor — once midwestern-normal, now a separate cultural zone developed independently for 200 years. What did Minneapolis and St. Paul become when left to their own devices? What do GLMZ residents say when someone from the Twin Cities arrives? What do Twin Cities people say about GLMZ? Mix: genuine cultural pride, things that survived that shouldn't have, things that emerged from isolation, trade relationships, a city that found a different path.`
  },
  {
    id: 6,
    label: 'Iowa and the Behemoths',
    count: 10,
    prompt: `Generate exactly 10 rumors and urban legends about Iowa and the Iowan Behemoths — autonomous machines (NOT alive, not synthetic life, emphatically not conscious) that control the vast automated farmland. Towns that serve the machines without the machines noticing. The Behemoth maintenance cycle. What happens when a Behemoth malfunctions? What do the towns between the machine-farms look like? CRITICAL: Behemoths are machines. Rumors about them being "alive" or "wanting things" are RUMORS that GLMZ people make up — the rumors should feel like scared people anthropomorphizing massive autonomous farm equipment. The machines don't have feelings. But they're enormous and inscrutable and people tell stories about them.`
  },
  {
    id: 7,
    label: 'Missouri and Kentucky Lost Territories',
    count: 10,
    prompt: `Generate exactly 10 rumors and urban legends about Missouri (partially flooded, wetlands, platform villages, drowned cities) and Kentucky ("lost" since 2198, uniform vegetation that took everything, four expeditions, the fourth didn't return). What do GLMZ residents whisper about these places? What do the wet, quiet travelers from Missouri say? What happened to the Kentucky expedition? Mix: genuinely mysterious, survival-practical, human-scale tragedy, the way people talk about places they're afraid to think about too hard.`
  },
  {
    id: 8,
    label: 'Great Plains and Denver Federal Remnant',
    count: 10,
    prompt: `Generate exactly 10 rumors and urban legends about the Great Plains (empty in a different way than before — something happened out there over 200 years) and the Federal Remnant government in Denver (a scarecrow government, people laugh at it, but sometimes it matters). Mix: what GLMZ residents say about the federal government with contempt and occasional unease, what travelers from the Plains say about the emptiness, what the Denver government actually DOES that matters even though no one respects it, and what the Plains looks like now after 200 years of whatever happened to it.`
  },
  {
    id: 9,
    label: 'Lake Michigan Deep Water Rumors',
    count: 10,
    prompt: `Generate exactly 10 rumors and urban legends about Lake Michigan itself — 200 years of industrial runoff and strange biology have made it strange. Something is in the lake. The deepwater. Things that came UP from the lake. Things that went IN and didn't come back. The lake supplies 60% of GLMZ's water after treatment. It receives all processed wastewater. What do Old Harbor workers whisper? What do Shelf kids know about the lake? What have fishers reported? What do the bathysphere surveys find? The lake is eerie, old, and full of things that don't appear in any catalog.`
  },
  {
    id: 10,
    label: 'Meta-Rumors and Cross-Location Patterns',
    count: 10,
    prompt: `Generate exactly 10 meta-rumors — rumors that span multiple locations, patterns that GLMZ residents notice across different outside territories, rumors that might be connected. Examples of the META quality: "People from Missouri, Kentucky, and the UP all describe the same sound." "Travelers from three different regions have all mentioned the same phrase." "The Federal Remnant and the Canadian automated government have been exchanging the same document every year." "Something in the Great Plains correlates with something in Lake Michigan." These are the rumors that make people pause — the ones where the pattern is bigger than any single location.`
  }
];

// ─── SYSTEM PROMPT ────────────────────────────────────────────────────────────

const SYSTEM_PROMPT = `You are a worldbuilding content generator for StreetSamurai, a cyberpunk fiction project set in GLMZ ~2200.

You generate rumors, urban legends, and fringe reports — things GLMZ residents whisper about the territories OUTSIDE their megacity. These are in-world documents: bar stories, zine entries, schoolyard legends, deathbed confessions.

CRITICAL RULES:
- The Φ symbol is the QUANTA currency. Never call it the Greek letter phi.
- Iowan Behemoths are autonomous machines. NOT alive. NOT synthetic life. Rumors about them being "alive" are rumors made up by scared humans anthropomorphizing machines.
- No city police in GLMZ. Arcturus Civil Security is the closest equivalent.
- Everyone in GLMZ is mixed heritage — Ubiquitous Diaspora. Mixed names from unexpected global combinations.
- Write with literary quality. These are living-world documents that will be used for storytelling.
- Each rumor must feel distinct — different tones, different origins (bar story vs zine vs whispered testimony vs child's schoolyard legend).
- source_reliability should feel honestly assessed: most rumors are "unverified" or "disputed", occasionally "plausible", rarely "documented".

OUTPUT FORMAT: Return a valid JSON array of exactly {count} objects. No markdown outside the array. Each object:
{
  "name": "The [Rumor Title]",
  "type": "document",
  "doc_type": "rumor|urban_legend|fringe_report|testimony|folklore",
  "source_reliability": "unverified|disputed|plausible|documented",
  "origin_location": "where this rumor comes from (which community/tier in GLMZ)",
  "subject_location": "where the rumor is ABOUT",
  "description": "One paragraph: the rumor itself, stated as a GLMZ resident might tell it",
  "body": "The full telling (300-600 words). Written as in-world text — bar story, zine entry, schoolyard legend, deathbed confession. Voice and format should vary.",
  "known_variations": ["alt version 1", "alt version 2"],
  "story_hooks": ["Hook 1", "Hook 2"],
  "tags": ["document", "rumor", "fringe", "outside-glmz"]
}`;

// ─── MAIN ─────────────────────────────────────────────────────────────────────

async function runBatch(batch) {
  console.log(`\n── Batch ${batch.id}: ${batch.label} (${batch.count} rumors) ──`);

  const systemPrompt = SYSTEM_PROMPT.replace('{count}', batch.count);
  const userPrompt = `${WORLD_CONTEXT}\n\n${batch.prompt.replace('{count}', batch.count)}\n\nGenerate exactly ${batch.count} rumors now. Return only the JSON array.`;

  let attempts = 0;
  while (attempts < 3) {
    attempts++;
    try {
      console.log(`  Calling Claude (attempt ${attempts})...`);
      const text = await callClaude(systemPrompt, userPrompt, 8192);
      const rumors = parseJsonArray(text);
      console.log(`  Received ${rumors.length} rumors from API`);

      let saved = 0;
      let skipped = 0;
      for (const rumor of rumors) {
        rumor.id = genId();
        if (!rumor.tags) rumor.tags = ['document', 'rumor', 'fringe', 'outside-glmz'];
        if (saveRumor(rumor)) saved++;
        else skipped++;
      }
      console.log(`  Batch ${batch.id} complete: ${saved} saved, ${skipped} skipped`);
      return saved;
    } catch (err) {
      console.error(`  Batch ${batch.id} attempt ${attempts} failed: ${err.message}`);
      if (attempts < 3) {
        console.log(`  Waiting ${RETRY_WAIT_MS / 1000}s before retry...`);
        await sleep(RETRY_WAIT_MS);
      } else {
        console.error(`  Batch ${batch.id} failed after 3 attempts. Continuing.`);
        return 0;
      }
    }
  }
  return 0;
}

async function main() {
  console.log(`Fringe Rumors Generator`);
  console.log(`Output: ${OUTPUT_DIR}`);
  console.log(`Model: ${MODEL}`);
  console.log(`Batches: ${BATCHES.length} (${BATCHES.reduce((s, b) => s + b.count, 0)} total rumors)\n`);

  let totalSaved = 0;

  for (let i = 0; i < BATCHES.length; i++) {
    const saved = await runBatch(BATCHES[i]);
    totalSaved += saved;
    if (i < BATCHES.length - 1) {
      console.log(`  Waiting ${WAIT_MS / 1000}s before next batch...`);
      await sleep(WAIT_MS);
    }
  }

  console.log(`\n══════════════════════════════════════`);
  console.log(`Generation complete. ${totalSaved} rumors saved to:`);
  console.log(OUTPUT_DIR);
}

main().catch(err => {
  console.error('Fatal error:', err);
  process.exit(1);
});
