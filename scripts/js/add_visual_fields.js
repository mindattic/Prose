// add_visual_fields.js
// Adds physical_description (string, 1-3 sentences) and visual_prompt (string) to:
//   transportation, weaponry, apparel, equipment entries
//
// Rules:
//   - physical_description: only add if the field is completely absent (no string, no object)
//     Files with existing physical_description objects are already enriched — skip PD for those.
//   - visual_prompt: add to every file where it's missing/empty.
//     If image_prompt already exists, copy it directly (no API call needed).
//     Otherwise, generate via Claude API.
//   - Never overwrite existing non-empty fields.
//
// Usage: node add_visual_fields.js
// Processes all four dirs in sequence, batching API calls 5 at a time.

const fs = require('fs');
const https = require('https');
const path = require('path');

// ── Config ─────────────────────────────────────────────────────────────────
const settings = JSON.parse(fs.readFileSync(
  path.join(process.env.LOCALAPPDATA, 'MindAttic', 'StreetSamurai', 'Settings.json'), 'utf8'));
const API_KEY = settings.ApiKey;
// Use haiku for fast/cheap batch enrichment
const MODEL = 'claude-haiku-4-5';
const BATCH_SIZE = 8;
const WAIT_BETWEEN_BATCHES_MS = 1000;
const sleep = ms => new Promise(r => setTimeout(r, ms));

const DATA_ROOT = path.join(__dirname, '..', 'engine', 'data');
const DIRS = [
  path.join(DATA_ROOT, 'transportation'),
  path.join(DATA_ROOT, 'weaponry'),
  path.join(DATA_ROOT, 'apparel'),
  path.join(DATA_ROOT, 'equipment'),
];

// ── Claude API ─────────────────────────────────────────────────────────────
function callClaude(system, user, maxTokens = 512) {
  return new Promise((resolve, reject) => {
    const body = JSON.stringify({
      model: MODEL,
      max_tokens: maxTokens,
      temperature: 0.75,
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
        'anthropic-version': '2023-06-01',
      }
    }, res => {
      let data = '';
      res.on('data', chunk => data += chunk);
      res.on('end', () => {
        try {
          const j = JSON.parse(data);
          if (j.content && j.content[0]) resolve(j.content[0].text.trim());
          else reject(new Error('API error: ' + data.substring(0, 400)));
        } catch (e) { reject(e); }
      });
    });
    req.setTimeout(90000, () => { req.destroy(); reject(new Error('Timeout')); });
    req.on('error', reject);
    req.write(body);
    req.end();
  });
}

// ── System prompt ──────────────────────────────────────────────────────────
const SYSTEM = `You are a world-building assistant for StreetSamurai, a near-future neo-noir tabletop RPG.
Setting: GLMZ (Great Lakes Militarized Zone) — stratified, rain-slicked, neon-lit, industrial decay meets desperate high-tech.
Tech is worn, practical, jury-rigged. Aesthetics: gritty neo-noir, not flashy cyberpunk chrome.
Currency is Φ (Quanta). Respond ONLY with the requested JSON object. No markdown, no preamble.`;

// ── Parse JSON from Claude response ───────────────────────────────────────
function extractJson(text) {
  let s = text.trim();
  // Strip markdown fences
  s = s.replace(/^```[a-z]*\n?/i, '').replace(/\n?```$/i, '').trim();
  const start = s.indexOf('{');
  const end = s.lastIndexOf('}');
  if (start === -1 || end === -1) throw new Error('No JSON object found');
  return JSON.parse(s.substring(start, end + 1));
}

// ── Generate fields via Claude ─────────────────────────────────────────────
async function generateFields(item, needsPD, needsVP) {
  const name = item.name || 'Unknown';
  const category = item.category || item.type || '';
  const desc = (item.description || '').substring(0, 700);
  const manufacturer = item.manufacturer || '';

  const fields = [];
  if (needsPD) {
    fields.push(`"physical_description": <string: 1-3 sentences describing the item's physical appearance — materials, shape, color, wear, notable features. Third person, present tense. Grounded in worn near-future neo-noir aesthetic.>`);
  }
  if (needsVP) {
    fields.push(`"visual_prompt": <string: comma-separated Stable Diffusion / Midjourney prompt, ~50-80 words. Include: cyberpunk, neo-noir, gritty, detailed product photography, relevant materials/colors, dark background.>`);
  }

  const user = `Item name: "${name}"
Type/category: ${category}
Manufacturer: ${manufacturer}
Description: ${desc}

Return a JSON object with exactly these fields:
${fields.join('\n')}`;

  for (let attempt = 1; attempt <= 3; attempt++) {
    try {
      const raw = await callClaude(SYSTEM, user, 600);
      return extractJson(raw);
    } catch (e) {
      if (attempt === 3) {
        console.error(`    FAIL [${name}]: ${e.message.substring(0, 120)}`);
        return null;
      }
      await sleep(2000 * attempt);
    }
  }
  return null;
}

// ── Analyze a file to determine what it needs ─────────────────────────────
function analyzeFile(filePath) {
  const data = JSON.parse(fs.readFileSync(filePath, 'utf8'));

  // physical_description: skip if any form already exists (string or object)
  const hasPD = data.physical_description !== undefined && data.physical_description !== null && data.physical_description !== '';
  const needsPD = !hasPD;

  // visual_prompt: needed if absent or empty
  const hasVP = !!(data.visual_prompt && typeof data.visual_prompt === 'string' && data.visual_prompt.trim());

  // image_prompt can satisfy visual_prompt directly (no API call needed)
  const hasImagePrompt = !!(data.image_prompt && typeof data.image_prompt === 'string' && data.image_prompt.trim());

  const needsVP = !hasVP;
  const vpFromImagePrompt = needsVP && hasImagePrompt;
  const vpNeedsApi = needsVP && !hasImagePrompt;

  return { data, needsPD, needsVP, vpFromImagePrompt, vpNeedsApi };
}

// ── Process directory ──────────────────────────────────────────────────────
async function processDir(dir) {
  const dirName = path.basename(dir);
  console.log(`\n━━━ ${dirName.toUpperCase()} ━━━`);

  const files = fs.readdirSync(dir).filter(f => f.endsWith('.json'));
  console.log(`  Files: ${files.length}`);

  // Categorize files
  const copyOnly = [];    // just copy image_prompt → visual_prompt, no PD needed
  const needsApi = [];    // need Claude API for PD and/or VP
  let skippedCount = 0;

  for (const f of files) {
    const filePath = path.join(dir, f);
    try {
      const info = analyzeFile(filePath);
      if (!info.needsPD && !info.needsVP) {
        skippedCount++;
        continue;
      }
      if (!info.needsPD && info.vpFromImagePrompt) {
        copyOnly.push({ filePath, info });
      } else if (info.vpNeedsApi || info.needsPD) {
        needsApi.push({ filePath, info });
      } else {
        copyOnly.push({ filePath, info });
      }
    } catch (e) {
      console.error(`  Error reading ${f}: ${e.message}`);
    }
  }

  console.log(`  Already complete: ${skippedCount}`);
  console.log(`  Copy image_prompt→visual_prompt only: ${copyOnly.length}`);
  console.log(`  Needs API generation: ${needsApi.length}`);

  let modified = 0;

  // Pass 1: Copy-only (instant)
  for (const { filePath, info } of copyOnly) {
    const data = info.data;
    if (info.vpFromImagePrompt) {
      data.visual_prompt = data.image_prompt;
      modified++;
      fs.writeFileSync(filePath, JSON.stringify(data, null, 2));
    }
  }
  if (copyOnly.length > 0) console.log(`  Pass 1 (copy): ${copyOnly.length} files updated`);

  // Pass 2: API generation in batches
  if (needsApi.length > 0) {
    console.log(`  Pass 2 (API): generating ${needsApi.length} items...`);
    let apiDone = 0;
    let apiErrors = 0;

    for (let i = 0; i < needsApi.length; i += BATCH_SIZE) {
      const batch = needsApi.slice(i, i + BATCH_SIZE);

      await Promise.all(batch.map(async ({ filePath, info }) => {
        const data = info.data;
        const generated = await generateFields(data, info.needsPD, info.vpNeedsApi);

        if (generated) {
          if (info.needsPD && generated.physical_description) {
            data.physical_description = generated.physical_description;
          }
          if (info.vpNeedsApi && generated.visual_prompt) {
            data.visual_prompt = generated.visual_prompt;
          } else if (info.vpFromImagePrompt) {
            data.visual_prompt = data.image_prompt;
          }
          fs.writeFileSync(filePath, JSON.stringify(data, null, 2));
          modified++;
        } else {
          apiErrors++;
        }
        apiDone++;
      }));

      if (i % (BATCH_SIZE * 5) === 0 && i > 0) {
        process.stdout.write(`    API progress: ${apiDone}/${needsApi.length} (${apiErrors} errors)\r`);
      }
      if (i + BATCH_SIZE < needsApi.length) await sleep(WAIT_BETWEEN_BATCHES_MS);
    }

    console.log(`    API progress: ${apiDone}/${needsApi.length} done (${apiErrors} errors)  `);
  }

  console.log(`  Modified: ${modified} files`);
  return modified;
}

// ── Main ───────────────────────────────────────────────────────────────────
async function main() {
  console.log('add_visual_fields.js — StreetSamurai visual enrichment');
  console.log('Processing: transportation, weaponry, apparel, equipment\n');

  let grandTotal = 0;
  for (const dir of DIRS) {
    grandTotal += await processDir(dir);
  }

  console.log(`\n═══════════════════════════════`);
  console.log(`COMPLETE. Total files modified: ${grandTotal}`);
}

main().catch(err => {
  console.error('Fatal error:', err);
  process.exit(1);
});
