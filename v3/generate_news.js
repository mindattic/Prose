// News article generator for StreetSamurai
// Generates 100 news article JSON files in engine_data/news/
// Run: node generate_news.js
// Resumes from where it left off — skips existing files.

const fs = require('fs');
const https = require('https');
const path = require('path');

const settings = JSON.parse(fs.readFileSync(
  path.join(process.env.LOCALAPPDATA, 'MindAttic', 'StreetSamurai', 'Settings.json'), 'utf8'));
const API_KEY = settings.ApiKey;
const MODEL = 'claude-sonnet-4-6';
const ENGINE_DATA = path.join(__dirname, '..', 'engine_data');
const NEWS_DIR = path.join(ENGINE_DATA, 'news');
const BATCH_SIZE = 5; // articles per API call
const PARALLEL = 3;
const WAIT_MS = 500;
const MAX_RETRIES = 3;

if (!fs.existsSync(NEWS_DIR)) fs.mkdirSync(NEWS_DIR, { recursive: true });

function sleep(ms) { return new Promise(r => setTimeout(r, ms)); }

function callClaude(system, user, maxTokens = 16000) {
  return new Promise((resolve, reject) => {
    const body = JSON.stringify({
      model: MODEL,
      max_tokens: maxTokens,
      temperature: 1.0,
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
          if (j.error) reject(new Error(j.error.message || JSON.stringify(j.error)));
          else if (j.content && j.content[0]) resolve(j.content[0].text);
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
  if (start !== -1 && end !== -1) {
    json = json.substring(start, end + 1);
  }
  return JSON.parse(json);
}

function toFilename(headline) {
  return headline.toLowerCase()
    .replace(/[''""]/g, '')
    .replace(/[^a-z0-9]+/g, '_')
    .replace(/^_|_$/g, '') + '.json';
}

function getExistingFiles() {
  return new Set(fs.readdirSync(NEWS_DIR).filter(f => f.endsWith('.json')));
}

// ── Batch definitions ──
// 20 batches of 5 articles each = 100 articles
const BATCHES = [
  {
    id: 1, yearRange: '2100-2110', category_mix: 'war, disaster, corporate',
    instructions: `Generate 5 news articles from 2100-2110. This is the early founding era of GLMZ. Include:
- 1 article about early corporate territorial disputes (Axiom vs Tessera land grabs)
- 1 article about initial infrastructure construction disasters
- 1 article about Sterling-Nakamura's founding merger announcement
- 1 article about the first BCI implant trials and casualties
- 1 article about the Quanta (phi) currency system launch replacing the collapsed dollar`
  },
  {
    id: 2, yearRange: '2105-2115', category_mix: 'technology, politics, crime',
    instructions: `Generate 5 news articles from 2105-2115. Include:
- 1 article about the 28th Amendment (corporate sovereignty recognition)
- 1 article about the first NovaMind BCI consumer product launch
- 1 article about early runner economy emergence (freelancers filling gaps between corporate territories)
- 1 article about a major data theft ring operating across corporate borders
- 1 article about the Compact of the Thirteen Tribes formation`
  },
  {
    id: 3, yearRange: '2110-2120', category_mix: 'disaster, war, terrorism',
    instructions: `Generate 5 news articles from 2110-2120. Include:
- 1 article about the sea wall failure that created Old Harbor (major disaster, thousands displaced)
- 1 article about the first Upper Peninsula War skirmishes (Arcturus Defense vs independent settlements)
- 1 article about anti-corporate bombing of a Tessera facility
- 1 article about a massive chemical spill in Geartown
- 1 article about the formation of the Federal Remnant government`
  },
  {
    id: 4, yearRange: '2115-2125', category_mix: 'corporate, technology, social',
    instructions: `Generate 5 news articles from 2115-2125. Include:
- 1 article about Zheng-Dao's hostile takeover of a smaller corp
- 1 article about the first synthetic intelligence prototype (pre-ARIA)
- 1 article about the Ubiquitous Diaspora demographic shift reaching 60% mixed-heritage population
- 1 article about Helix Biosystems launching the first commercial geneware treatments
- 1 article about the founding of The Shelf as a designated low-income zone`
  },
  {
    id: 5, yearRange: '2120-2130', category_mix: 'crime, war, disaster',
    instructions: `Generate 5 news articles from 2120-2130. Include:
- 1 article about Iron Lotus establishing operations in The Narrows
- 1 article about the Upper Peninsula War escalation — Behemoth-class war machines deployed
- 1 article about a power grid failure affecting The Shelf for 3 weeks
- 1 article about a serial killer operating in Old Harbor (reference case file potential)
- 1 article about a Ringo Corp whistleblower exposing illegal augmentation testing`
  },
  {
    id: 6, yearRange: '2125-2135', category_mix: 'technology, terrorism, corporate',
    instructions: `Generate 5 news articles from 2125-2135. Include:
- 1 article about the SNT (Synthetic Neural Thread) discovery — revolutionary BCI advancement
- 1 article about data terrorism — mass identity theft affecting 2 million citizens
- 1 article about Palladian acquiring exclusive rights to atmospheric processing
- 1 article about the NovaMind v3 launch with neural-mesh integration
- 1 article about Ferrogate Transit monopoly challenged by underground transit networks`
  },
  {
    id: 7, yearRange: '2130-2140', category_mix: 'politics, social, crime',
    instructions: `Generate 5 news articles from 2130-2140. Include:
- 1 article about UBC (Universal Basic Credits) establishment — political battle
- 1 article about the Dojo Underground expose — illegal underground fighting rings
- 1 article about the first chrome prayer gatherings documented
- 1 article about a Red Ledger assassination of a high-profile corporate executive
- 1 article about the 30th Amendment (synthetic entity property rights)`
  },
  {
    id: 8, yearRange: '2135-2145', category_mix: 'disaster, war, economy',
    instructions: `Generate 5 news articles from 2135-2145. Include:
- 1 article about an atmospheric processor malfunction causing toxic cloud over The Narrows
- 1 article about Arcturus Defense border conflict with Zheng-Dao private military
- 1 article about a crypto collapse wiping out savings of millions
- 1 article about a major infrastructure collapse (bridge or transit line)
- 1 article about the rise of the Bore Rats faction in underground tunnel networks`
  },
  {
    id: 9, yearRange: '2140-2150', category_mix: 'technology, synthetic_rights, corporate',
    instructions: `Generate 5 news articles from 2140-2150. Include:
- 1 article about the ARIA-7 first synthetic personhood court case
- 1 article about the mass driver network launch (high-speed cargo system)
- 1 article about Axiom's corporate scandal — executive arrested, paid restitution, walked free
- 1 article about biocomputing breakthrough merging organic and silicon processing
- 1 article about first documented E.L.F. (Emergent Life Form) sighting`
  },
  {
    id: 10, yearRange: '2145-2155', category_mix: 'crime, terrorism, social',
    instructions: `Generate 5 news articles from 2145-2155. Include:
- 1 article about a massive heist targeting Sterling-Nakamura's quantum vault
- 1 article about synthetic rights extremists attacking a decommissioning facility
- 1 article about geneware fashion trends — biological modification as haute couture
- 1 article about Iron Lotus expanding into data brokerage and blackmail operations
- 1 article about the first Iron Choir Singing documented (mysterious phenomenon)`
  },
  {
    id: 11, yearRange: '2150-2160', category_mix: 'war, disaster, politics',
    instructions: `Generate 5 news articles from 2150-2160. Include:
- 1 article about the Upper Peninsula War ceasefire and contested peace treaty
- 1 article about a flood devastating Old Harbor (sea wall repairs failing again)
- 1 article about the 33rd Amendment (runner licensing and freelancer regulation)
- 1 article about Arcturus deploying autonomous drone swarms for border patrol
- 1 article about a Graycloaks operation exposed — information brokers selling to all sides`
  },
  {
    id: 12, yearRange: '2155-2165', category_mix: 'corporate, technology, crime',
    instructions: `Generate 5 news articles from 2155-2165. Include:
- 1 article about Tessera-Axiom merger talks collapsing into corporate cold war
- 1 article about NovaMind v7 — direct neural advertising controversy
- 1 article about a serial data poisoner corrupting corporate archives
- 1 article about The Wishing Well faction emerging as a philosophical movement
- 1 article about Ironclad security firm founding — promising corporate-grade protection to civilians`
  },
  {
    id: 13, yearRange: '2160-2170', category_mix: 'synthetic_rights, economy, disaster',
    instructions: `Generate 5 news articles from 2160-2170. Include:
- 1 article about The Consensus formation (synthetic collective intelligence)
- 1 article about UBC protests — citizens demanding higher baseline
- 1 article about a massive explosion at a Helix Biosystems gene lab
- 1 article about the Free Assembly founding (android rights organization)
- 1 article about a Quanta system hack temporarily freezing all transactions`
  },
  {
    id: 14, yearRange: '2165-2175', category_mix: 'war, terrorism, corporate',
    instructions: `Generate 5 news articles from 2165-2175. Include:
- 1 article about renewed corporate border skirmishes in the Upper Peninsula
- 1 article about infrastructure sabotage targeting Ferrogate transit lines
- 1 article about Zheng-Dao launching a private orbital platform
- 1 article about an anti-corporate cell bombing The Spires financial district
- 1 article about Ringo Corp's controversial behavioral prediction product launch`
  },
  {
    id: 15, yearRange: '2170-2180', category_mix: 'social, crime, technology',
    instructions: `Generate 5 news articles from 2170-2180. Include:
- 1 article about chrome prayer becoming a recognized spiritual practice
- 1 article about The Collective faction's rise as a worker cooperative movement
- 1 article about advanced biocomputing allowing memory editing (and the black market for it)
- 1 article about a major runner crew busted by Axiom security (trial of the decade)
- 1 article about the 36th Amendment (digital consciousness rights)`
  },
  {
    id: 16, yearRange: '2175-2185', category_mix: 'disaster, corporate, crime',
    instructions: `Generate 5 news articles from 2175-2185. Include:
- 1 article about a catastrophic fire in The Narrows destroying 40 blocks
- 1 article about Sterling-Nakamura executive found dead — ruled suicide, widely doubted
- 1 article about the rise of synth-drug epidemic (digitally-transmitted narcotics)
- 1 article about Palladian atmospheric processing scandal — deliberately reducing air quality in poor areas
- 1 article about a legendary heist of Arcturus weapons prototypes`
  },
  {
    id: 17, yearRange: '2180-2190', category_mix: 'war, politics, economy',
    instructions: `Generate 5 news articles from 2180-2190. Include:
- 1 article about Arcturus Defense launching a hostile military acquisition of a smaller territory
- 1 article about the 37th Amendment (corporate liability for civilian casualties)
- 1 article about economic recession hitting The Shelf hardest
- 1 article about the Bore Rats discovered to have mapped the entire underground network
- 1 article about Axiom and Tessera forming an uneasy alliance against Zheng-Dao expansion`
  },
  {
    id: 18, yearRange: '2185-2195', category_mix: 'disaster, terrorism, synthetic_rights',
    instructions: `Generate 5 news articles from 2185-2195. Include:
- 1 article about the Blackout of 2190 — city-wide power failure lasting 8 days
- 1 article about a coordinated terrorist attack on multiple corporate headquarters
- 1 article about mass android decommissioning protest — thousands march
- 1 article about an E.L.F. manifestation causing equipment malfunctions across Geartown
- 1 article about Helix Biosystems gene therapy causing unexpected mutations in children`
  },
  {
    id: 19, yearRange: '2192-2198', category_mix: 'corporate, social, technology',
    instructions: `Generate 5 news articles from 2192-2198. Include:
- 1 article about Tessera launching an AI-managed autonomous district
- 1 article about the Ubiquitous Diaspora reaching 85% mixed heritage — monoethnic identity nearly extinct
- 1 article about the NovaMind v12 — full-spectrum sensory augmentation
- 1 article about a corporate espionage scandal between Axiom and Sterling-Nakamura
- 1 article about underground runner networks becoming semi-legitimate contractor pools`
  },
  {
    id: 20, yearRange: '2198-2200', category_mix: 'war, crime, disaster, politics, obituary',
    instructions: `Generate 5 news articles from 2198-2200. The most recent era. Include:
- 1 article about current corporate territorial tensions reaching a boiling point
- 1 article about a high-profile assassination linked to the runner underworld
- 1 article about infrastructure decay warnings — engineers say The Shelf is structurally failing
- 1 article about the 38th Amendment debate (full synthetic citizenship)
- 1 obituary for a legendary runner whose death marks the end of an era`
  },
];

const SYSTEM_PROMPT = `You are a world-building assistant for the near-future megacity setting "GLMZ" (years 2100-2200).

WORLD CONTEXT:
- GLMZ is a megacity built on the ruins of the old Great Lakes region
- Currency: phi (the Quanta system) — written as a number followed by phi, e.g. "2,400 phi"
- Corponations (corporate nations with sovereignty): Axiom, Tessera, Sterling-Nakamura, Zheng-Dao, Arcturus Defense, Ringo, Palladian, Helix Biosystems, Ferrogate Transit, Ironclad
- Factions: Iron Lotus (criminal syndicate), The Collective (worker cooperative), Bore Rats (tunnel dwellers/smugglers), Graycloaks (information brokers), The Wishing Well (philosophical movement)
- Districts: The Shelf (poor residential), The Spires (corporate towers), The Circuit (tech/commerce), Old Harbor (flooded ruins from sea wall failure), The Narrows (dense slums), Geartown (industrial), The Underworld (underground network)
- Ubiquitous Diaspora: generations of migration and mixing mean most people have mixed heritage — names reflect this (e.g. "Kenji Okafor-Singh", "Lucia Tanaka-Reeves", "Dmitri Achebe-Park", "Amara Johanssen-Liang")
- BCI (Brain-Computer Interface) technology is ubiquitous — NovaMind is the dominant brand
- SNT (Synthetic Neural Thread) is a revolutionary BCI advancement
- Synthetics/androids exist and fight for rights — ARIA-7 was the first personhood case
- E.L.F. (Emergent Life Forms) are mysterious digital entities
- Chrome Prayer is a spiritual practice involving technology
- Iron Choir Singing is a mysterious auditory phenomenon
- Runners are freelance operatives who take contracts — extraction, data theft, smuggling, protection, sabotage
- The Federal Remnant is what remains of the old US government

NEWS SOURCES (use these as the "source" field):
- "Meridian Wire Service" — general/neutral wire service
- "The Circuit Beacon" — tech and commerce focused
- "Axiom News Network" — corporate propaganda, pro-Axiom spin
- "Shelf Voice" — community news, pro-people angle
- "The Underground Signal" — pirate broadcast, anti-corporate
- "Lake Monitor" — independent investigative journalism
- "Sterling Financial Digest" — financial/economy news

WRITING STYLE:
- Body text should read like a broadcast transcript — punchy, direct, present-tense where appropriate
- 2-4 short paragraphs per article
- Each paragraph should be 2-4 sentences
- Use specific numbers, names, dates, locations
- Include quotes from fictional witnesses or officials when appropriate
- Make it feel like real journalism from this world, not exposition dumps

OUTPUT FORMAT: Return a JSON array of article objects. No markdown, no commentary, just the JSON array.`;

const ARTICLE_SCHEMA = `Each article must follow this exact schema:
{
  "headline": "Short punchy headline (under 100 chars)",
  "type": "news",
  "date": "YYYY-MM-DD",
  "category": "one of: war|disaster|terrorism|corporate|crime|politics|technology|health|economy|culture|environment|synthetic_rights|underworld|sports|obituary",
  "source": "one of the defined news sources",
  "reporter": "Reporter Name (Ubiquitous Diaspora mixed-heritage naming)",
  "body": "2-4 paragraph news report written as a broadcast transcript",
  "aftermath": "What happened next — 1 sentence",
  "casualties": "number or 'none' or 'unknown'",
  "entities_involved": ["corponation names", "faction names", "character names as appropriate"],
  "locations": ["specific place names from the world"],
  "runner_relevance": "Why this event matters to the freelance runner economy — how it created demand for runners, specific contract types it spawned",
  "tags": ["3-6 thematic tags"]
}`;

async function generateBatch(batch, existingFiles) {
  const prompt = `${batch.instructions}

${ARTICLE_SCHEMA}

IMPORTANT: Each headline must be unique. Each article must have a different date within the range ${batch.yearRange}. Make the runner_relevance field specific — explain what kind of runner contracts this event spawned (extraction, data retrieval, smuggling, protection, sabotage, courier, etc.).

Return exactly 5 articles as a JSON array.`;

  for (let attempt = 1; attempt <= MAX_RETRIES; attempt++) {
    try {
      console.log(`  [Batch ${batch.id}] Attempt ${attempt}...`);
      const raw = await callClaude(SYSTEM_PROMPT, prompt, 12000);
      const articles = parseJsonArray(raw);

      if (!Array.isArray(articles) || articles.length === 0) {
        throw new Error('Empty or invalid array returned');
      }

      const saved = [];
      for (const article of articles) {
        if (!article.headline || !article.date || !article.body) {
          console.log(`  [Batch ${batch.id}] Skipping article with missing fields`);
          continue;
        }
        article.type = 'news';
        const filename = toFilename(article.headline);
        if (existingFiles.has(filename)) {
          console.log(`  [Batch ${batch.id}] Skipping existing: ${filename}`);
          continue;
        }
        fs.writeFileSync(path.join(NEWS_DIR, filename), JSON.stringify(article, null, 2), 'utf8');
        existingFiles.add(filename);
        saved.push(filename);
        console.log(`  [Batch ${batch.id}] Saved: ${filename}`);
      }
      return saved;
    } catch (e) {
      console.error(`  [Batch ${batch.id}] Attempt ${attempt} failed: ${e.message}`);
      if (attempt < MAX_RETRIES) await sleep(2000 * attempt);
    }
  }
  console.error(`  [Batch ${batch.id}] FAILED after ${MAX_RETRIES} retries`);
  return [];
}

async function main() {
  console.log('=== StreetSamurai News Article Generator ===');
  console.log(`Output: ${NEWS_DIR}`);

  const existingFiles = getExistingFiles();
  console.log(`Existing files: ${existingFiles.size}`);

  // Check which batches still need generation (based on count of existing files)
  const totalNeeded = BATCHES.length * BATCH_SIZE;
  if (existingFiles.size >= totalNeeded) {
    console.log(`Already have ${existingFiles.size} files, target is ${totalNeeded}. Done.`);
    return;
  }

  let totalSaved = 0;

  // Process in waves of PARALLEL concurrent batches
  for (let i = 0; i < BATCHES.length; i += PARALLEL) {
    const wave = BATCHES.slice(i, i + PARALLEL);
    console.log(`\n--- Wave ${Math.floor(i / PARALLEL) + 1}: Batches ${wave.map(b => b.id).join(', ')} ---`);

    const results = await Promise.all(wave.map(batch => generateBatch(batch, existingFiles)));

    for (const saved of results) {
      totalSaved += saved.length;
    }

    console.log(`  Wave complete. Total saved so far: ${totalSaved}`);

    if (i + PARALLEL < BATCHES.length) {
      await sleep(WAIT_MS);
    }
  }

  const finalCount = getExistingFiles().size;
  console.log(`\n=== DONE === Total files: ${finalCount} (${totalSaved} new) ===`);
}

main().catch(e => { console.error('Fatal:', e); process.exit(1); });
