// Repo expansion generator — adds 100 new entries to each of 7 engine_data repos
// Run: node generate_repo_expansion.js [skip:vocab,places,factions,corps,cyber,gene,ammo]
// Calls Claude API to generate entries, writes individual JSON files

const fs = require('fs');
const https = require('https');
const path = require('path');

const settings = JSON.parse(fs.readFileSync(
  path.join(process.env.LOCALAPPDATA, 'MindAttic', 'StreetSamurai', 'Settings.json'), 'utf8'));
const API_KEY = settings.ApiKey;
const MODEL = 'claude-sonnet-4-6';
const ENGINE_DATA = path.join(__dirname, '..', 'engine_data');

function callClaude(system, user, maxTokens = 4096) {
  return new Promise((resolve, reject) => {
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
      }
    }, res => {
      let data = '';
      res.on('data', chunk => data += chunk);
      res.on('end', () => {
        try {
          const j = JSON.parse(data);
          if (j.content && j.content[0]) resolve(j.content[0].text);
          else if (j.error) reject(new Error(j.error.message || JSON.stringify(j.error)));
          else reject(new Error(data.substring(0, 300)));
        } catch (e) { reject(e); }
      });
    });
    req.setTimeout(120000, () => { req.destroy(); reject(new Error('Timeout')); });
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
  if (start >= 0 && end > start) json = json.substring(start, end + 1);
  return JSON.parse(json);
}

function toFilename(name) {
  return name.toLowerCase().replace(/[^a-z0-9]+/g, '_').replace(/^_|_$/g, '') + '.json';
}

function getExistingFiles(dir) {
  if (!fs.existsSync(dir)) { fs.mkdirSync(dir, { recursive: true }); return []; }
  return fs.readdirSync(dir).filter(f => f.endsWith('.json'));
}

function getExistingNames(dir, nameField) {
  const files = getExistingFiles(dir);
  const names = [];
  for (const f of files) {
    try {
      const data = JSON.parse(fs.readFileSync(path.join(dir, f), 'utf8'));
      if (data[nameField]) names.push(data[nameField]);
    } catch (e) {}
  }
  return names;
}

function writeEntries(dir, entries, nameField) {
  let written = 0;
  for (const entry of entries) {
    const name = entry[nameField];
    if (!name) continue;
    const filename = toFilename(name);
    const filepath = path.join(dir, filename);
    if (fs.existsSync(filepath)) { continue; }
    fs.writeFileSync(filepath, JSON.stringify(entry, null, 2));
    written++;
  }
  return written;
}

async function sleep(ms) { return new Promise(r => setTimeout(r, ms)); }

async function callWithRetry(system, user, maxTokens, retries = 2) {
  for (let attempt = 0; attempt <= retries; attempt++) {
    try {
      const result = await callClaude(system, user, maxTokens);
      return parseJsonArray(result);
    } catch (e) {
      if (attempt < retries) {
        console.log(`    Retry ${attempt + 1}/${retries} after error: ${e.message.substring(0, 80)}`);
        await sleep(3000 * (attempt + 1));
      } else {
        throw e;
      }
    }
  }
}

const WORLD = `Setting: GLMZ, year 2200. Great Lakes megacity corridor (Chicago-Milwaukee). Currency: Φ. The Diaspora (internet successor) is ubiquitous. Major CorpoNations: Axiom Industries, Tessera Corporation, Sterling-Nakamura, Zheng-Dao Bioelectric, Arcturus Defense Solutions, Ringo Heavy Industries, Vespid Dynamics, Carrion Logistics, Helix Biosystems, Palladian Group, Ferrogate Security. E.L.F.s (Emergent Lifeforms, rogue AIs). Synthetic personhood is a civil rights issue. Multicultural future, names from everywhere globally. BCI technology ubiquitous. Tiers 1-5 socioeconomic strata. The Shelf is lowest, Old Harbor/Lakeshore mid-tier, the Spire is top.`;

// ═══════════════════════════════════════════════════════════
// GENERIC BATCH GENERATOR
// ═══════════════════════════════════════════════════════════
async function generateRepo({ label, dir, nameField, batchSize, maxTokens, systemPrompt, userPromptFn, target }) {
  const existing = getExistingNames(dir, nameField);
  console.log(`\n═══ ${label} ═══ (${existing.length} existing, generating ${target} new)`);

  let totalWritten = 0;
  const totalBatches = Math.ceil(target / batchSize);

  for (let i = 0; i < target && totalWritten < target; i += batchSize) {
    const count = Math.min(batchSize, target - totalWritten);
    const batchNum = Math.floor(i / batchSize) + 1;
    process.stdout.write(`  [${batchNum}/${totalBatches}] `);

    try {
      const system = systemPrompt(count);
      const user = userPromptFn(existing, count);
      const entries = await callWithRetry(system, user, maxTokens);
      const written = writeEntries(dir, entries, nameField);
      totalWritten += written;
      existing.push(...entries.map(e => e[nameField]).filter(Boolean));
      console.log(`+${written} (${totalWritten}/${target})`);
    } catch (e) {
      console.log(`ERROR: ${e.message.substring(0, 80)}`);
    }
    await sleep(500);
  }
  console.log(`${label} COMPLETE: ${totalWritten} new entries`);
  return totalWritten;
}

// ═══════════════════════════════════════════════════════════
// REPO DEFINITIONS
// ═══════════════════════════════════════════════════════════

const REPOS = {
  vocab: {
    label: 'VOCABULARY',
    dir: path.join(ENGINE_DATA, 'vocabulary'),
    nameField: 'term',
    batchSize: 10,
    maxTokens: 4096,
    target: 100,
    systemPrompt: (n) => `${WORLD}\n\nGenerate a JSON array of ${n} vocabulary entries. Each: {"term":"...","definition":"...","origin":"...","usage":"...","tier":"...","category":"...","example":"..."}\nCategories: street slang, corporate jargon, medical, underworld cant, geneware, synth life, combat. Be inventive.`,
    userPromptFn: (existing, n) => `DO NOT duplicate: ${existing.slice(-80).join(', ')}\n\nGenerate ${n} NEW terms. ONLY the JSON array.`
  },

  places: {
    label: 'PLACES',
    dir: path.join(ENGINE_DATA, 'places'),
    nameField: 'name',
    batchSize: 2,
    maxTokens: 6000,
    target: 100,
    systemPrompt: (n) => `${WORLD}\n\nGenerate a JSON array of ${n} place entries. Schema:\n{"type":"place","name":"...","aliases":[2-3],"description":"2 paragraphs 100-200 words","atmosphere":{"sights":[3],"sounds":[3],"smells":[3],"feel":"2-3 sentences"},"demographics":"1-2 sentences","economy":"1-2 sentences","power_structure":"1-2 sentences","dangers":[3],"opportunities":[3],"story_hooks":[2 hooks, 2-3 sentences each],"frequented_by":[3-4],"notable_locations":[],"connections":{"adjacent_to":[2-3]},"coordinates":{"lat":42-43,"lng":-87 to -88}}\nMix: GLMZ neighborhoods, underworld levels, Lake Michigan locations, suburban ruins, industrial zones. Keep CONCISE.`,
    userPromptFn: (existing, n) => `DO NOT duplicate: ${existing.slice(-60).join(', ')}\n\n${n} NEW places. ONLY JSON array.`
  },

  factions: {
    label: 'FACTIONS',
    dir: path.join(ENGINE_DATA, 'factions'),
    nameField: 'name',
    batchSize: 3,
    maxTokens: 6000,
    target: 100,
    systemPrompt: (n) => `${WORLD}\n\nGenerate a JSON array of ${n} faction entries. Schema:\n{"type":"faction","name":"...","aliases":[2-3],"motto":"...","description":"2 paragraphs 100-200 words","ideology":"1-2 paragraphs","territory":"...","leadership":"...","methods":[4-5 strings],"membership":"...","resources":[3-4 strings],"relationships_with_other_factions":"...","story_hooks":[3 hooks, 2-3 sentences each]}\nMix: gangs, corp subsidiaries, political movements, religious groups, hacker collectives, mutual aid, criminal enterprises, fight clubs, smuggling, resistance cells, synth rights, E.L.F. cults, mercenaries, medical collectives, scavenger guilds. resources MUST be array of strings. Keep CONCISE.`,
    userPromptFn: (existing, n) => `DO NOT duplicate: ${existing.join(', ')}\n\n${n} NEW factions. ONLY JSON array.`
  },

  corps: {
    label: 'CorpoNations',
    dir: path.join(ENGINE_DATA, 'CorpoNations'),
    nameField: 'name',
    batchSize: 2,
    maxTokens: 6000,
    target: 100,
    systemPrompt: (n) => {
      const existingCount = getExistingFiles(path.join(ENGINE_DATA, 'CorpoNations')).length;
      return `${WORLD}\n\nGenerate a JSON array of ${n} CorpoNation entries. Schema:\n{"number":${existingCount + 1},"name":"ALL CAPS","full_legal_name":"...","common_names":[3-4],"stock_designation":"...","sector":"...","valuation":"Φ...","revenue":"Φ...","employees":"...","sovereign_territory":"1-2 sentences","founding_story":"2 paragraphs 100-200 words","security_force":"1 paragraph","key_detail":"1 paragraph","relationship_to_big_20":"1 paragraph","full_text":"3 paragraphs with markdown, 200-300 words"}\nMix: small local, mid-tier regional, niche specialist, startup, old dynasty. Use Φ for currency. Keep CONCISE.`;
    },
    userPromptFn: (existing, n) => `DO NOT duplicate: ${existing.join(', ')}\n\n${n} NEW CorpoNations. ONLY JSON array.`
  },

  cyber: {
    label: 'CYBERWARE',
    dir: path.join(ENGINE_DATA, 'cyberware'),
    nameField: 'name',
    batchSize: 3,
    maxTokens: 6000,
    target: 100,
    systemPrompt: (n) => `${WORLD}\n\nGenerate a JSON array of ${n} cyberware entries. Schema:\n{"name":"...","type":"cyberware","aliases":[2-3],"category":"neural/optical/skeletal/dermal/organ/limb/sensory/combat/medical/communication","body_location":"...","description":"2 paragraphs 100-200 words","manufacturer":"...","tier_availability":"...","legality":"Unrestricted/Licensed/Restricted/Prohibited/Gray Market","installation_requirements":"...","rejection_risk":"...","maintenance":"...","specifications":"technical specs as string","side_effects":[2-3],"street_price":"Φ...","licensed_price":"Φ...","cultural_context":"1 paragraph","story_hooks":[2-3]}\nMix categories and price points. Keep CONCISE.`,
    userPromptFn: (existing, n) => `DO NOT duplicate: ${existing.slice(-60).join(', ')}\n\n${n} NEW cyberware. ONLY JSON array.`
  },

  gene: {
    label: 'GENEWARE',
    dir: path.join(ENGINE_DATA, 'geneware'),
    nameField: 'name',
    batchSize: 3,
    maxTokens: 6000,
    target: 100,
    systemPrompt: (n) => `${WORLD}\n\nGenerate a JSON array of ${n} geneware entries. Schema:\n{"name":"...","type":"geneware","aliases":[2-3],"category":"cosmetic/performance/medical/sensory/structural/metabolic/cognitive/defensive/reproductive/longevity","target_system":"...","description":"2 paragraphs 100-200 words","source_organism":"Species name (Latin name)","manufacturer":"...","tier_availability":"...","legality":"...","procedure":"...","expression_time":"...","reversibility":"...","side_effects":[3-4],"social_perception":"1 paragraph","story_hooks":[2-3]}\nMix categories. Use real species with Latin names. Keep CONCISE.`,
    userPromptFn: (existing, n) => `DO NOT duplicate: ${existing.join(', ')}\n\n${n} NEW geneware. ONLY JSON array.`
  },

  ammo: {
    label: 'AMMUNITION',
    dir: path.join(ENGINE_DATA, 'ammunition'),
    nameField: 'name',
    batchSize: 5,
    maxTokens: 6000,
    target: 100,
    systemPrompt: (n) => `${WORLD}\n\nGenerate a JSON array of ${n} ammunition entries. Schema:\n{"name":"formal designation","type":"ammunition","aliases":[2-3],"category":"ballistic/energy/chemical/smart/subsonic/explosive/non-lethal/specialty/caseless/railgun","caliber":"...","description":"2 paragraphs 100-200 words","manufacturer":"...","tier_availability":"...","legality":"...","specifications":"weight, velocity, penetration, range as string","compatible_weapons":[2-3],"variants":[2-3 variant strings],"cultural_context":"1 paragraph","story_hooks":[2-3]}\nMix categories and calibers. Keep CONCISE.`,
    userPromptFn: (existing, n) => `DO NOT duplicate: ${existing.join(', ')}\n\n${n} NEW ammo types. ONLY JSON array.`
  }
};

// ═══════════════════════════════════════════════════════════
// MAIN
// ═══════════════════════════════════════════════════════════
async function main() {
  console.log('StreetSamurai Repo Expansion Generator');
  console.log('======================================');
  console.log(`Engine data: ${ENGINE_DATA}`);
  console.log(`Model: ${MODEL}\n`);

  const repoOrder = ['vocab', 'places', 'factions', 'corps', 'cyber', 'gene', 'ammo'];

  for (const r of repoOrder) {
    const count = getExistingFiles(REPOS[r].dir).length;
    console.log(`  ${REPOS[r].label}: ${count} existing files`);
  }

  const skipArg = process.argv[2] || '';
  const skipSet = new Set(skipArg.replace('skip:', '').split(',').filter(Boolean));

  const startTime = Date.now();
  const results = {};

  for (const key of repoOrder) {
    if (skipSet.has(key)) {
      console.log(`\n═══ ${REPOS[key].label} ═══ SKIPPED`);
      continue;
    }
    results[key] = await generateRepo(REPOS[key]);
  }

  console.log('\n======================================');
  console.log('FINAL COUNTS:');
  for (const r of repoOrder) {
    const count = getExistingFiles(REPOS[r].dir).length;
    console.log(`  ${REPOS[r].label}: ${count} files`);
  }
  const elapsed = ((Date.now() - startTime) / 1000 / 60).toFixed(1);
  console.log(`\nTotal time: ${elapsed} minutes`);
}

main().catch(e => { console.error('FATAL:', e); process.exit(1); });
