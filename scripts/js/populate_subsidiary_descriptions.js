/**
 * Populate Subsidiary Descriptions
 *
 * Reads all subsidiaries with empty descriptions and calls Claude to generate
 * a concise 2-paragraph description for each. Processes in batches of 20.
 *
 * Run: node populate_subsidiary_descriptions.js
 *      node populate_subsidiary_descriptions.js --dry-run
 *      node populate_subsidiary_descriptions.js --limit 50
 */

'use strict';

const fs   = require('fs');
const path = require('path');
const https = require('https');

const SUBS_DIR = path.join(__dirname, '..', 'engine', 'data', 'subsidiaries');
const settings = JSON.parse(fs.readFileSync(
  path.join(process.env.LOCALAPPDATA, 'MindAttic', 'StreetSamurai', 'Settings.json'), 'utf8'));
const API_KEY = settings.ApiKey;
const MODEL = 'claude-haiku-4-5-20251001';  // cheap and fast for short descriptions

const args = process.argv.slice(2);
const DRY_RUN  = args.includes('--dry-run');
const LIMIT    = (() => { const i = args.indexOf('--limit'); return i >= 0 ? parseInt(args[i+1]) : Infinity; })();

// ── Claude API ──────────────────────────────────────────────────────────────

function callClaude(system, user, maxTokens = 2048) {
  return new Promise((resolve, reject) => {
    const body = JSON.stringify({
      model: MODEL,
      max_tokens: maxTokens,
      temperature: 0.8,
      system,
      messages: [{ role: 'user', content: user }]
    });
    const req = https.request({
      hostname: 'api.anthropic.com',
      path: '/v1/messages',
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'x-api-key': API_KEY,
        'anthropic-version': '2023-06-01'
      }
    }, res => {
      let data = '';
      res.on('data', chunk => data += chunk);
      res.on('end', () => {
        try {
          const j = JSON.parse(data);
          if (j.content && j.content[0]) resolve(j.content[0].text);
          else reject(new Error(JSON.stringify(j).substring(0, 300)));
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
  const fence = json.indexOf('```');
  if (fence !== -1) {
    json = json.substring(json.indexOf('\n', fence) + 1);
    const end = json.lastIndexOf('```');
    if (end !== -1) json = json.substring(0, end);
    json = json.trim();
  }
  return JSON.parse(json);
}

const sleep = ms => new Promise(r => setTimeout(r, ms));

// ── Main ────────────────────────────────────────────────────────────────────

const SYSTEM = `You generate descriptions for corporate subsidiaries in GLMZ 2200, a near-future cyberpunk megacity.
World rules:
- Currency is Φ (Quanta), never dollars
- Tiers 1-5 (1=poorest Shelf districts, 5=corporate elite)
- No city police — Arcturus Civil Security is enforcement
- CorpoNations are sovereign entities, subsidiaries are their operating arms with separate branding
- Tone: matter-of-fact worldbuilding prose, slightly cynical, specific and grounded
- Do NOT mention brand names of real-world companies
- Parent CorpoNation names: use them exactly as given

For each subsidiary, write exactly 2 paragraphs (100-200 words total):
- Paragraph 1: What the subsidiary does, what market it operates in, what it makes or provides
- Paragraph 2: Its relationship to the parent CorpoNation (why does the parent use a subsidiary brand for this?),
  what tier of society it primarily serves, and one specific cultural or political detail that makes it interesting

Return a JSON array. Each element: { "name": "<exact name>", "description": "<two paragraphs joined by \\n\\n>" }
Return ONLY the JSON array, no other text.`;

async function main() {
  const files = fs.readdirSync(SUBS_DIR).filter(f => f.endsWith('.json'));
  const toProcess = [];

  for (const fname of files) {
    const fpath = path.join(SUBS_DIR, fname);
    try {
      const d = JSON.parse(fs.readFileSync(fpath, 'utf8'));
      if (d.type === 'subsidiary' && !d.description?.trim()) {
        toProcess.push({ file: fpath, data: d });
      }
    } catch { /* skip malformed */ }
  }

  console.log(`Found ${toProcess.length} subsidiaries with empty descriptions`);
  if (LIMIT < Infinity) console.log(`Limiting to ${LIMIT}`);
  if (DRY_RUN) {
    console.log('DRY RUN — first 10:');
    toProcess.slice(0, 10).forEach(x =>
      console.log(`  ${x.data.name} | ${x.data.parent_CorpoNation} | ${x.data.line_of_business}`));
    return;
  }

  const queue = toProcess.slice(0, LIMIT);
  const BATCH = 20;
  let written = 0;
  let failed  = 0;

  for (let i = 0; i < queue.length; i += BATCH) {
    const batch = queue.slice(i, i + BATCH);
    const batchNum = Math.floor(i / BATCH) + 1;
    const totalBatches = Math.ceil(queue.length / BATCH);
    console.log(`\nBatch ${batchNum}/${totalBatches} (${batch.length} subsidiaries)...`);

    const userPrompt = batch.map(x =>
      `{ "name": ${JSON.stringify(x.data.name)}, "parent_CorpoNation": ${JSON.stringify(x.data.parent_CorpoNation)}, "line_of_business": ${JSON.stringify(x.data.line_of_business || '')}, "known_products": ${JSON.stringify(x.data.known_products || [])} }`
    ).join('\n');

    let results = null;
    for (let attempt = 0; attempt < 2; attempt++) {
      try {
        const raw = await callClaude(SYSTEM, `Generate descriptions for these ${batch.length} subsidiaries:\n${userPrompt}`);
        results = parseJsonArray(raw);
        break;
      } catch (e) {
        if (attempt === 0 && (e.message?.includes('rate') || e.message?.includes('overloaded'))) {
          console.log('  Rate limited — waiting 30s...');
          await sleep(30000);
        } else {
          console.error(`  Batch ${batchNum} failed: ${e.message}`);
          failed += batch.length;
          break;
        }
      }
    }

    if (!results) continue;

    // Write descriptions back to source files
    for (const result of results) {
      const match = batch.find(x => x.data.name === result.name);
      if (!match || !result.description?.trim()) continue;
      match.data.description = result.description.trim();
      try {
        fs.writeFileSync(match.file, JSON.stringify(match.data, null, 2), 'utf8');
        console.log(`  WROTE: ${result.name}`);
        written++;
      } catch (e) {
        console.error(`  WRITE FAILED: ${result.name}: ${e.message}`);
        failed++;
      }
    }

    // Polite pause between batches
    if (i + BATCH < queue.length) await sleep(3000);
  }

  console.log(`\n=== Done ===`);
  console.log(`Written: ${written}`);
  console.log(`Failed:  ${failed}`);
}

main().catch(console.error);
