// generate_everyday_weapons.js
// Generates ~1000 everyday firearms + all standard ammunition types for StreetSamurai GLMZ 2200
// Run: node generate_everyday_weapons.js [--dry-run] [--ammo-only] [--weapons-only] [--limit N]

'use strict';

const fs = require('fs');
const https = require('https');
const path = require('path');
const crypto = require('crypto');

// ── Config ──────────────────────────────────────────────────────────────────
const settings = JSON.parse(fs.readFileSync(
  path.join(process.env.LOCALAPPDATA, 'MindAttic', 'StreetSamurai', 'Settings.json'), 'utf8'));
const API_KEY = settings.ApiKey;
const MODEL = 'claude-sonnet-4-6';
const WEAPON_DIR = path.join(__dirname, '..', 'engine', 'data', 'weaponry');
const AMMO_DIR   = path.join(__dirname, '..', 'engine', 'data', 'ammunition');
const WAIT_MS    = 65000; // 65s between batches to stay under rate limit
const MAX_TOKENS = 32768;

// ── CLI args ─────────────────────────────────────────────────────────────────
const args = process.argv.slice(2);
const DRY_RUN      = args.includes('--dry-run');
const AMMO_ONLY    = args.includes('--ammo-only');
const WEAPONS_ONLY = args.includes('--weapons-only');
const limitIdx     = args.indexOf('--limit');
const BATCH_LIMIT  = limitIdx !== -1 ? parseInt(args[limitIdx + 1]) : null;

// ── Utilities ────────────────────────────────────────────────────────────────
function sleep(ms) { return new Promise(r => setTimeout(r, ms)); }

function slugify(name) {
  return name.toLowerCase().replace(/[^a-z0-9]+/g, '_').replace(/^_+|_+$/g, '').substring(0, 80);
}

function newId() { return crypto.randomBytes(16).toString('hex'); }

function getExistingFilenames(dir) {
  if (!fs.existsSync(dir)) { fs.mkdirSync(dir, { recursive: true }); return new Set(); }
  return new Set(fs.readdirSync(dir).filter(f => f.endsWith('.json')));
}

function getExistingNames(dir) {
  const files = fs.existsSync(dir) ? fs.readdirSync(dir).filter(f => f.endsWith('.json')) : [];
  const names = new Set();
  for (const f of files) {
    try {
      const d = JSON.parse(fs.readFileSync(path.join(dir, f), 'utf8'));
      if (d.name) names.add(d.name);
    } catch { /* skip corrupt */ }
  }
  return names;
}

function writeEntityFile(dir, entity) {
  const slug = slugify(entity.name || 'unnamed');
  const filePath = path.join(dir, slug + '.json');
  if (fs.existsSync(filePath)) return { path: filePath, skipped: true };
  if (!entity.id) entity.id = newId();
  fs.writeFileSync(filePath, JSON.stringify(entity, null, 2));
  return { path: filePath, skipped: false };
}

// ── Claude API call ───────────────────────────────────────────────────────────
function callClaude(system, user, maxTokens = MAX_TOKENS) {
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
          if (j.error) { reject(new Error(j.error.message || JSON.stringify(j.error))); return; }
          if (j.content && j.content[0]) resolve(j.content[0].text);
          else reject(new Error('No content in response: ' + data.substring(0, 500)));
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
  // Strip ```json ... ``` or ``` ... ``` fences
  if (json.startsWith('```')) {
    json = json.substring(json.indexOf('\n') + 1);
    const fence = json.lastIndexOf('```');
    if (fence !== -1) json = json.substring(0, fence);
    json = json.trim();
  }
  return JSON.parse(json);
}

// Call Claude with one retry on rate-limit (429) or parse error
async function callClaudeWithRetry(system, user, label) {
  for (let attempt = 1; attempt <= 2; attempt++) {
    try {
      const raw = await callClaude(system, user);
      return parseJsonArray(raw);
    } catch (e) {
      const msg = e.message || '';
      if (msg.includes('rate_limit') || msg.includes('529') || msg.includes('overloaded')) {
        console.log(`  [${label}] Rate limit hit, waiting 30s...`);
        await sleep(30000);
        continue;
      }
      if (attempt === 1) {
        console.warn(`  [${label}] Parse error on attempt 1: ${msg.substring(0, 120)} — retrying...`);
        await sleep(5000);
        continue;
      }
      throw e;
    }
  }
}

// ── World system prompt (shared) ──────────────────────────────────────────────
const WORLD_SYSTEM = `You are a worldbuilding assistant for StreetSamurai, a near-future cyberpunk setting.

WORLD RULES:
- Year 2200, city is GLMZ (Great Lakes Megacity Zone, formerly Chicago area, ~100M people)
- Currency is Φ (Quanta) — this is NOT the Greek letter phi, NOT dollars
- Tiers 1–5: Tier 1 = lowest access (shelf workers, excluded), Tier 5 = corporate elite. Most everyday weapons are Tier 1–2.
- No city police exist — Arcturus Civil Security is the closest equivalent; Meridian PD dissolved 2208
- Caseless ammunition is the military standard by 2200, but brass-cased is still common civilian
- LASER and PLASMA weapons: common but LESS RELIABLE than conventional firearms — batteries die, optics foul, plasma containment fails in wet/cold/EMP. Conventional and magnetic preferred in the field
- COILGUN/MAGNETIC weapons: very reliable, no propellant fouling — more reliable than energy, roughly equivalent to conventional gunpowder weapons
- Corponation sovereignty: Slagworks Industrial, Arcturus Defense Solutions, Crucible Genomics, Tessera Corponation, Zheng-dao Bioelectric
- Class tension: Shelf workers (Tier 1–2), Mids (Tier 2–3), Corps (Tier 3–5), Excluded (no tier, survival mode)
- Weapon names follow pattern: "MANUFACTURER Model-Designation 'Nickname'" e.g. "Kang-Petrov KP-19 'Workhorse'"
- DO NOT use real-world brand names (no Glock, Beretta, Sig Sauer, Smith & Wesson, etc.) — inspired by but rebranded
- Descriptions need grit, wear, class tension, and character — not sterile product copy
- Story hooks must be specific and interesting, not generic
- Protagonist Kyle Corbin-Vasik carries a KP-19 'Workhorse' and LOP-1
- Default to mixed heritage from unexpected global combinations for cultural flavor`;

// ── AMMUNITION list ───────────────────────────────────────────────────────────
const AMMO_TYPES = [
  // Ballistic — gunpowder
  { name: '.22 Short-Long',           category: 'ballistic', propulsion: 'gunpowder',          caliber: '.22' },
  { name: '.25 Pocket Auto',          category: 'ballistic', propulsion: 'gunpowder',          caliber: '.25' },
  { name: '.32 Auto Compact',         category: 'ballistic', propulsion: 'gunpowder',          caliber: '.32' },
  { name: '.380 Compact Auto',        category: 'ballistic', propulsion: 'gunpowder',          caliber: '.380' },
  { name: '9x19mm Standard',          category: 'ballistic', propulsion: 'gunpowder',          caliber: '9x19mm' },
  { name: '.357 Auto Express',        category: 'ballistic', propulsion: 'gunpowder',          caliber: '.357' },
  { name: '.38 Service Revolver',     category: 'ballistic', propulsion: 'gunpowder',          caliber: '.38' },
  { name: '.357 Service Magnum',      category: 'ballistic', propulsion: 'gunpowder',          caliber: '.357 Magnum' },
  { name: '.40 Smith Pattern',        category: 'ballistic', propulsion: 'gunpowder',          caliber: '.40' },
  { name: '.44 Service Revolver',     category: 'ballistic', propulsion: 'gunpowder',          caliber: '.44' },
  { name: '.44 Magnum Revolver',      category: 'ballistic', propulsion: 'gunpowder',          caliber: '.44 Magnum' },
  { name: '.45 Auto Composite',       category: 'ballistic', propulsion: 'gunpowder',          caliber: '.45' },
  { name: '10mm Combat Auto',         category: 'ballistic', propulsion: 'gunpowder',          caliber: '10mm' },
  { name: '.454 High Pressure',       category: 'ballistic', propulsion: 'gunpowder',          caliber: '.454' },
  { name: '.460 Ultra Magnum',        category: 'ballistic', propulsion: 'gunpowder',          caliber: '.460' },
  { name: '.50 Action Express',       category: 'ballistic', propulsion: 'gunpowder',          caliber: '.50 AE' },
  { name: '.500 Revolver Maximum',    category: 'ballistic', propulsion: 'gunpowder',          caliber: '.500' },
  { name: '5.7mm High-Velocity Auto', category: 'ballistic', propulsion: 'gunpowder',          caliber: '5.7mm' },
  { name: '4.6mm PDW Compact',        category: 'ballistic', propulsion: 'gunpowder',          caliber: '4.6mm' },
  { name: '5.56mm Combat Standard',   category: 'ballistic', propulsion: 'gunpowder',          caliber: '5.56mm' },
  { name: '7.62mm Soviet Pattern',    category: 'ballistic', propulsion: 'gunpowder',          caliber: '7.62mm Soviet' },
  { name: '7.62mm NATO Pattern',      category: 'ballistic', propulsion: 'gunpowder',          caliber: '7.62mm NATO' },
  { name: '.308 Precision Hunting',   category: 'ballistic', propulsion: 'gunpowder',          caliber: '.308' },
  { name: '.30 Springfield Long',     category: 'ballistic', propulsion: 'gunpowder',          caliber: '.30-06' },
  { name: '6.5mm Precision Match',    category: 'ballistic', propulsion: 'gunpowder',          caliber: '6.5mm' },
  { name: '6.8mm General Service',    category: 'ballistic', propulsion: 'gunpowder',          caliber: '6.8mm' },
  { name: '.300 Compact Subsonic',    category: 'ballistic', propulsion: 'gunpowder',          caliber: '.300 Blackout' },
  { name: '.300 Long Magnum',         category: 'ballistic', propulsion: 'gunpowder',          caliber: '.300 WM' },
  { name: '.338 Precision Long',      category: 'ballistic', propulsion: 'gunpowder',          caliber: '.338 LM' },
  { name: '.50 Heavy Machine Gun',    category: 'ballistic', propulsion: 'gunpowder',          caliber: '.50 BMG' },
  { name: '12.7mm Anti-Materiel',     category: 'ballistic', propulsion: 'gunpowder',          caliber: '12.7mm' },
  { name: '12-gauge Standard',        category: 'ballistic', propulsion: 'gunpowder',          caliber: '12-gauge' },
  { name: '12-gauge Caseless Slug',   category: 'ballistic', propulsion: 'caseless',           caliber: '12-gauge CL' },
  { name: '20-gauge Compact',         category: 'ballistic', propulsion: 'gunpowder',          caliber: '20-gauge' },
  { name: '.410 Pocket Shot',         category: 'ballistic', propulsion: 'gunpowder',          caliber: '.410' },
  { name: '10-gauge Heavy',           category: 'ballistic', propulsion: 'gunpowder',          caliber: '10-gauge' },
  { name: '9mm Rubber Less-Lethal',   category: 'ballistic', propulsion: 'gunpowder',          caliber: '9mm LL' },
  { name: '12-gauge Less-Lethal Bean',category: 'ballistic', propulsion: 'gunpowder',          caliber: '12-gauge LL' },
  // Ballistic — caseless
  { name: '9x19mm Caseless',          category: 'ballistic', propulsion: 'caseless',           caliber: '9x19mm CL' },
  { name: '.45 Caseless Combat',      category: 'ballistic', propulsion: 'caseless',           caliber: '.45 CL' },
  { name: '5.56mm Combat Caseless',   category: 'ballistic', propulsion: 'caseless',           caliber: '5.56mm CL' },
  { name: '7.62mm Caseless',          category: 'ballistic', propulsion: 'caseless',           caliber: '7.62mm CL' },
  { name: '6.5mm Caseless Match',     category: 'ballistic', propulsion: 'caseless',           caliber: '6.5mm CL' },
  // Magnetic/Coilgun
  { name: '4mm Ferro Penetrator',           category: 'magnetic', propulsion: 'magnetic/coilgun', caliber: '4mm ferro' },
  { name: '6mm Ferro Penetrator',           category: 'magnetic', propulsion: 'magnetic/coilgun', caliber: '6mm ferro' },
  { name: '8mm Ferro Penetrator',           category: 'magnetic', propulsion: 'magnetic/coilgun', caliber: '8mm ferro' },
  { name: '12mm Ferro Penetrator',          category: 'magnetic', propulsion: 'magnetic/coilgun', caliber: '12mm ferro' },
  { name: 'Magnetic Composite Slug Standard', category: 'magnetic', propulsion: 'magnetic/coilgun', caliber: 'slug' },
  { name: 'Magnetic Flechette Burst',       category: 'magnetic', propulsion: 'magnetic/coilgun', caliber: 'flechette' },
  // Energy
  { name: 'Class-1 Laser Cell',   category: 'energy', propulsion: 'laser',  caliber: 'Class-1' },
  { name: 'Class-2 Laser Cell',   category: 'energy', propulsion: 'laser',  caliber: 'Class-2' },
  { name: 'Class-3 Laser Cell',   category: 'energy', propulsion: 'laser',  caliber: 'Class-3' },
  { name: 'Plasma Core Type-A',   category: 'energy', propulsion: 'plasma', caliber: 'Type-A' },
  { name: 'Plasma Core Type-B',   category: 'energy', propulsion: 'plasma', caliber: 'Type-B' },
  { name: 'Plasma Core Type-C',   category: 'energy', propulsion: 'plasma', caliber: 'Type-C' },
];

// ── AMMO generation ───────────────────────────────────────────────────────────
async function generateAllAmmo() {
  console.log('\n── AMMUNITION GENERATION ──────────────────────────────');
  const existingFilenames = getExistingFilenames(AMMO_DIR);
  const existingNames = getExistingNames(AMMO_DIR);

  // Filter to ammo not yet generated (check both slug filename and name)
  const todo = AMMO_TYPES.filter(a => {
    const slug = slugify(a.name) + '.json';
    return !existingFilenames.has(slug) && !existingNames.has(a.name);
  });

  if (todo.length === 0) { console.log('  All ammo types already generated. Skipping.'); return; }
  console.log(`  Need to generate ${todo.length} ammo types (${AMMO_TYPES.length - todo.length} already exist)`);

  // Chunk into batches of 10
  const CHUNK = 10;
  for (let i = 0; i < todo.length; i += CHUNK) {
    const chunk = todo.slice(i, i + CHUNK);
    const label = `ammo batch ${Math.floor(i / CHUNK) + 1}`;
    console.log(`\n  [${label}] Generating: ${chunk.map(a => a.name).join(', ')}`);

    if (DRY_RUN) { console.log('  [DRY RUN] Skipping API call.'); continue; }

    const system = WORLD_SYSTEM + `\n\nYou generate ammunition entries for GLMZ 2200. Return a JSON array only — no markdown, no explanation.

Each entry must have EXACTLY these fields:
{
  "id": "<32-char hex>",
  "name": "<exact name as given>",
  "type": "ammunition",
  "aliases": ["alt name", "street name"],
  "category": "ballistic|energy|magnetic",
  "caliber": "<caliber string>",
  "propulsion": "gunpowder|caseless|laser|plasma|magnetic/coilgun",
  "description": "<2 paragraphs — what this round is in GLMZ 2200, how it differs from its 21st-century ancestor, who uses it>",
  "compatibility_note": "<what weapon types and manufacturers use this>",
  "reliability_note": "<for energy: note laser/plasma less reliable than conventional — batteries die, optics foul, containment fails in wet/cold/EMP. For coilgun/magnetic: very reliable, no propellant fouling, more reliable than energy. For ballistic: most reliable, still dominant civilian choice>",
  "tags": ["ammunition", "<category-tag>"]
}`;

    const user = `Generate JSON entries for EXACTLY these ${chunk.length} ammunition types. Use the exact names given. Return ONLY the JSON array.

Ammo types:
${chunk.map(a => `- name: "${a.name}", category: "${a.category}", propulsion: "${a.propulsion}", caliber: "${a.caliber}"`).join('\n')}`;

    try {
      const items = await callClaudeWithRetry(system, user, label);
      let written = 0, skipped = 0;
      for (const item of items) {
        // Ensure correct fields from the spec
        if (!item.id) item.id = newId();
        item.type = 'ammunition';
        const result = writeEntityFile(AMMO_DIR, item);
        if (result.skipped) skipped++;
        else { written++; console.log(`    + ${item.name}`); }
      }
      console.log(`  [${label}] Written: ${written}, Skipped: ${skipped}`);
    } catch (e) {
      console.error(`  [${label}] FAILED: ${e.message}`);
    }

    if (i + CHUNK < todo.length) {
      console.log(`  Waiting ${WAIT_MS / 1000}s for rate limit...`);
      await sleep(WAIT_MS);
    }
  }
}

// ── WEAPON BATCHES ─────────────────────────────────────────────────────────────
// 100 batches × ~10 weapons = ~1000 weapons
// Format: { batchId, manufacturer, corponation, count, type, inspiration, calibers, tiers, flavor }

const BATCHES = [
  // ── KANG-PETROV ARMS (Slagworks Industrial) — 7 batches ──────────────────
  {
    batchId: 1,
    manufacturer: 'KANG-PETROV ARMS',
    corponation: 'Slagworks Industrial',
    count: 10,
    type: 'pistol',
    inspiration: 'Glock 17/19/43 family evolution — striker-fired polymer pistols, 200 years improved, the workhorse everyone owns',
    calibers: ['9x19mm Standard', '9x19mm Caseless', '10mm Combat Auto'],
    tiers: ['Tier 1+', 'Tier 2+'],
    flavor: 'Kang-Petrov pistols are everywhere. Shelf workers, mid-tier security, anyone who needs a gun and not much else. The KP-19 is Kyle Corbin-Vasik\'s sidearm — the default pistol of GLMZ.'
  },
  {
    batchId: 2,
    manufacturer: 'KANG-PETROV ARMS',
    corponation: 'Slagworks Industrial',
    count: 10,
    type: 'rifle',
    inspiration: 'AK-pattern rifle family — rotating bolt, stamped steel, brutal reliability, 200 years of iteration',
    calibers: ['7.62mm Soviet Pattern', '7.62mm Caseless', '5.56mm Combat Standard'],
    tiers: ['Tier 1+', 'Tier 2+'],
    flavor: 'If the KP pistols are the handshake of GLMZ violence, KP rifles are the conversation. Cheap, loud, and durable enough to outlast the person firing them.'
  },
  {
    batchId: 3,
    manufacturer: 'KANG-PETROV ARMS',
    corponation: 'Slagworks Industrial',
    count: 10,
    type: 'rifle',
    inspiration: 'AR-pattern rifle family — direct impingement and piston variants, carbine-length and full, 200 years of evolution',
    calibers: ['5.56mm Combat Standard', '5.56mm Combat Caseless', '6.8mm General Service'],
    tiers: ['Tier 1+', 'Tier 2+'],
    flavor: 'KP\'s AR-pattern line competes with their own AK line. Lighter and modular but slightly more finicky — still far tougher than anything with a battery.'
  },
  {
    batchId: 4,
    manufacturer: 'KANG-PETROV ARMS',
    corponation: 'Slagworks Industrial',
    count: 10,
    type: 'smg',
    inspiration: 'MP5/UMP/Vector family evolution — compact SMGs for close-quarters, PDW-length stocks',
    calibers: ['9x19mm Standard', '9x19mm Caseless', '4.6mm PDW Compact'],
    tiers: ['Tier 1+', 'Tier 2+'],
    flavor: 'KP SMGs are the weapon of corridor workers — security checkpoints, access-control squads, any place where rifle length is a liability.'
  },
  {
    batchId: 5,
    manufacturer: 'KANG-PETROV ARMS',
    corponation: 'Slagworks Industrial',
    count: 10,
    type: 'shotgun',
    inspiration: 'Mossberg 500/590 and Remington 870 evolution — pump and semi-auto combat shotguns',
    calibers: ['12-gauge Standard', '12-gauge Caseless Slug', '12-gauge Less-Lethal Bean'],
    tiers: ['Tier 1+', 'Tier 2+'],
    flavor: 'KP shotguns are the door-breaker of the shelf economy. Simple, brutal, and loud enough to serve as negotiation.'
  },
  {
    batchId: 6,
    manufacturer: 'KANG-PETROV ARMS',
    corponation: 'Slagworks Industrial',
    count: 8,
    type: 'revolver',
    inspiration: 'Ruger GP100/SP101 family — double-action revolvers, budget-tier reliability for civilians',
    calibers: ['.357 Service Magnum', '.38 Service Revolver', '.44 Service Revolver'],
    tiers: ['Tier 1+', 'Tier 2+'],
    flavor: 'The KP revolver line exists for one reason: no magazine to jam, no battery to die. Old technology, still working.'
  },
  {
    batchId: 7,
    manufacturer: 'KANG-PETROV ARMS',
    corponation: 'Slagworks Industrial',
    count: 8,
    type: 'coilgun',
    inspiration: 'Mass-market magnetic pistols and carbines — KP adapts coilgun tech for Tier 1–2 civilian market',
    calibers: ['4mm Ferro Penetrator', '6mm Ferro Penetrator', 'Magnetic Composite Slug Standard'],
    tiers: ['Tier 2+', 'Tier 3+'],
    flavor: 'KP magnetic weapons are the affordable coilgun option. Reliable, legal-ish, and quiet — an attractive package for people who can\'t afford the Arcturus lineup.'
  },

  // ── HEARTHSTONE FIREARMS (Slagworks Industrial) — 4 batches ───────────────
  {
    batchId: 8,
    manufacturer: 'HEARTHSTONE FIREARMS',
    corponation: 'Slagworks Industrial',
    count: 10,
    type: 'pistol',
    inspiration: 'Hi-Point / cheap polymer pistol family — bottom-end civilian market, basic defensive use',
    calibers: ['9x19mm Standard', '.380 Compact Auto', '.32 Auto Compact'],
    tiers: ['Tier 1+'],
    flavor: 'Hearthstone pistols are what you buy when you can\'t afford a KP. Heavy, ugly, but they fire when you pull the trigger — usually. Shelf staple.'
  },
  {
    batchId: 9,
    manufacturer: 'HEARTHSTONE FIREARMS',
    corponation: 'Slagworks Industrial',
    count: 8,
    type: 'revolver',
    inspiration: 'Charter Arms / Taurus budget revolver family — simple, cheap, no frills',
    calibers: ['.38 Service Revolver', '.357 Service Magnum', '.22 Short-Long'],
    tiers: ['Tier 1+'],
    flavor: 'Hearthstone revolvers have a reputation: reliable as rocks, ugly as sin. The kind of gun that ends up in a drawer for thirty years and still fires.'
  },
  {
    batchId: 10,
    manufacturer: 'HEARTHSTONE FIREARMS',
    corponation: 'Slagworks Industrial',
    count: 8,
    type: 'shotgun',
    inspiration: 'Single-shot and double-barrel break-action shotguns — cheapest possible scattergun',
    calibers: ['12-gauge Standard', '20-gauge Compact', '.410 Pocket Shot'],
    tiers: ['Tier 1+'],
    flavor: 'Two shots and done. Hearthstone break-actions are apartment defense guns, farm guns, end-of-the-line guns. Nothing to maintain, nothing to learn.'
  },
  {
    batchId: 11,
    manufacturer: 'HEARTHSTONE FIREARMS',
    corponation: 'Slagworks Industrial',
    count: 8,
    type: 'rifle',
    inspiration: 'Single-shot and tube-fed .22 rifles, budget semi-auto rimfire carbines',
    calibers: ['.22 Short-Long', '.380 Compact Auto'],
    tiers: ['Tier 1+'],
    flavor: 'Hearthstone rifles exist because sometimes you need a rifle and a pistol is all you can afford. Marginal upgrades for marginal situations.'
  },

  // ── MERIDIAN MUNITIONS (Slagworks Industrial) — 4 batches ────────────────
  {
    batchId: 12,
    manufacturer: 'MERIDIAN MUNITIONS',
    corponation: 'Slagworks Industrial',
    count: 8,
    type: 'pistol',
    inspiration: 'Jennings/Bryco-style ultra-budget zinc-alloy pistols — the absolute floor of firearms quality',
    calibers: ['9x19mm Standard', '.25 Pocket Auto', '.32 Auto Compact'],
    tiers: ['Tier 1+'],
    flavor: 'Meridian pistols are survival objects. They have a failure rate that would disqualify them from any legitimate market — but they\'re cheaper than food for a week. People buy them anyway.'
  },
  {
    batchId: 13,
    manufacturer: 'MERIDIAN MUNITIONS',
    corponation: 'Slagworks Industrial',
    count: 8,
    type: 'pistol',
    inspiration: 'Sub-compact and pocket pistols for concealed carry by people with almost nothing',
    calibers: ['.380 Compact Auto', '.25 Pocket Auto', '.22 Short-Long'],
    tiers: ['Tier 1+'],
    flavor: 'The Meridian pocket series exists for the excluded — people who can\'t register a weapon anyway, buying a last resort at a pawn stall.'
  },
  {
    batchId: 14,
    manufacturer: 'MERIDIAN MUNITIONS',
    corponation: 'Slagworks Industrial',
    count: 8,
    type: 'revolver',
    inspiration: 'Ultra-cheap small-frame revolvers — pot metal, minimal safety, maximum desperation',
    calibers: ['.38 Service Revolver', '.22 Short-Long', '.32 Auto Compact'],
    tiers: ['Tier 1+'],
    flavor: 'Meridian revolvers are sometimes called "one-trip guns." They may not survive a full cylinder. They may not need to.'
  },
  {
    batchId: 15,
    manufacturer: 'MERIDIAN MUNITIONS',
    corponation: 'Slagworks Industrial',
    count: 8,
    type: 'smg',
    inspiration: 'MAC-10/Sten-style open-bolt blowback SMGs — minimal parts count, extremely cheap production',
    calibers: ['9x19mm Standard', '.380 Compact Auto'],
    tiers: ['Tier 1+'],
    flavor: 'Meridian SMGs look like they were assembled in a shipping container — because most of them were. Gang weapons, black market staples, disposable hardware for disposable conflicts.'
  },

  // ── NKOMO-LINDQVIST (Slagworks Industrial) — 4 batches ───────────────────
  {
    batchId: 16,
    manufacturer: 'NKOMO-LINDQVIST',
    corponation: 'Slagworks Industrial',
    count: 10,
    type: 'pistol',
    inspiration: 'SIG P365/Hellcat family — micro-compact carry pistols with higher capacity than their size suggests',
    calibers: ['9x19mm Standard', '9x19mm Caseless', '.380 Compact Auto'],
    tiers: ['Tier 2+', 'Tier 3+'],
    flavor: 'Nkomo-Lindqvist makes the carry pistol of choice for people who know what they\'re doing. Small, light, reliable — priced for mid-tier professionals, not shelf workers.'
  },
  {
    batchId: 17,
    manufacturer: 'NKOMO-LINDQVIST',
    corponation: 'Slagworks Industrial',
    count: 8,
    type: 'pistol',
    inspiration: 'Micro-compact and subcompact deep-concealment pistols — Ruger LCP, Smith Bodyguard lineage',
    calibers: ['.380 Compact Auto', '.32 Auto Compact', '9x19mm Standard'],
    tiers: ['Tier 2+', 'Tier 3+'],
    flavor: 'NL micro-compacts disappear into a jacket lining. The kind of carry option that makes Tier 3 professionals feel safe at Tier 1 meetings.'
  },
  {
    batchId: 18,
    manufacturer: 'NKOMO-LINDQVIST',
    corponation: 'Slagworks Industrial',
    count: 8,
    type: 'revolver',
    inspiration: 'S&W J-frame / Ruger LCR family — pocket revolvers for concealed carry',
    calibers: ['.38 Service Revolver', '.357 Service Magnum', '9x19mm Standard'],
    tiers: ['Tier 2+', 'Tier 3+'],
    flavor: 'NL makes the rare carry revolver that doesn\'t feel like a compromise. Small, accurate, and with a trigger pull that rewards practice.'
  },
  {
    batchId: 19,
    manufacturer: 'NKOMO-LINDQVIST',
    corponation: 'Slagworks Industrial',
    count: 8,
    type: 'smg',
    inspiration: 'B&T USW / Scorpion EVO 3 — compact PDW/SMG hybrids for personal protection professionals',
    calibers: ['9x19mm Caseless', '4.6mm PDW Compact', '5.7mm High-Velocity Auto'],
    tiers: ['Tier 2+', 'Tier 3+'],
    flavor: 'NL PDWs are the off-duty weapons of people with security clearances. Not a military weapon — a professional tool for professionals.'
  },

  // ── VOLKOV-SAITO PRECISION (Zheng-dao Bioelectric) — 6 batches ────────────
  {
    batchId: 20,
    manufacturer: 'VOLKOV-SAITO PRECISION',
    corponation: 'Zheng-dao Bioelectric',
    count: 10,
    type: 'pistol',
    inspiration: 'CZ Shadow 2 / Tanfoglio / Walther Q5 match — competition pistols with target triggers and match accuracy',
    calibers: ['9x19mm Standard', '9x19mm Caseless', '.40 Smith Pattern'],
    tiers: ['Tier 3+', 'Tier 4+'],
    flavor: 'Volkov-Saito competition pistols are not fighting guns. They are instruments. The people who carry them in the field are making a statement about their confidence.'
  },
  {
    batchId: 21,
    manufacturer: 'VOLKOV-SAITO PRECISION',
    corponation: 'Zheng-dao Bioelectric',
    count: 8,
    type: 'pistol',
    inspiration: 'M1911 family evolution — single-action steel pistols, 200 years of refinement for the traditionalist',
    calibers: ['.45 Auto Composite', '.45 Caseless Combat', '10mm Combat Auto'],
    tiers: ['Tier 3+', 'Tier 4+'],
    flavor: 'VS 1911-pattern pistols are heirlooms manufactured on demand. Mechanically two centuries old, every tolerance refined to a tolerance impossible in 2011.'
  },
  {
    batchId: 22,
    manufacturer: 'VOLKOV-SAITO PRECISION',
    corponation: 'Zheng-dao Bioelectric',
    count: 8,
    type: 'revolver',
    inspiration: 'Dan Wesson / Korth / Python lineage — match-grade target revolvers with adjustable triggers',
    calibers: ['.357 Service Magnum', '.38 Service Revolver', '.44 Magnum Revolver'],
    tiers: ['Tier 3+', 'Tier 4+'],
    flavor: 'VS competition revolvers exist for one market: the precision enthusiast who refuses to accept that automatics won. And they\'re not wrong.'
  },
  {
    batchId: 23,
    manufacturer: 'VOLKOV-SAITO PRECISION',
    corponation: 'Zheng-dao Bioelectric',
    count: 10,
    type: 'sniper',
    inspiration: 'AI AXSR / Sako TRG / Accuracy International family — precision bolt-action rifles',
    calibers: ['6.5mm Precision Match', '6.5mm Caseless Match', '.338 Precision Long'],
    tiers: ['Tier 3+', 'Tier 4+'],
    flavor: 'Volkov-Saito bolt guns are the choice of professional snipers who pay for their own equipment. When corponations issue scoped rifles, they issue Arcturus. When snipers buy their own, they buy VS.'
  },
  {
    batchId: 24,
    manufacturer: 'VOLKOV-SAITO PRECISION',
    corponation: 'Zheng-dao Bioelectric',
    count: 8,
    type: 'sniper',
    inspiration: 'CheyTac M200 / Barrett MRAD — extreme long range precision rifles at the edge of ballistic capability',
    calibers: ['.338 Precision Long', '.300 Long Magnum', '12.7mm Anti-Materiel'],
    tiers: ['Tier 4+', 'Tier 5'],
    flavor: 'VS long-range precision work is the pinnacle of conventional ballistics in 2200. These rifles cost more than most people earn in a year and perform accordingly.'
  },
  {
    batchId: 25,
    manufacturer: 'VOLKOV-SAITO PRECISION',
    corponation: 'Zheng-dao Bioelectric',
    count: 8,
    type: 'dmr',
    inspiration: 'Mk 12 SPR / SR-25 / SCAR-H PR — designated marksman rifles for semi-auto precision',
    calibers: ['7.62mm NATO Pattern', '6.5mm Precision Match', '6.8mm General Service'],
    tiers: ['Tier 3+', 'Tier 4+'],
    flavor: 'VS DMRs bridge competition and combat — bought by operators who want match accuracy in a semi-auto platform they can actually run fast.'
  },

  // ── ARCTURUS DEFENSE SOLUTIONS (Arcturus Defense Solutions) — 7 batches ───
  {
    batchId: 26,
    manufacturer: 'ARCTURUS DEFENSE SOLUTIONS',
    corponation: 'Arcturus Defense Solutions',
    count: 10,
    type: 'pistol',
    inspiration: 'Beretta M9A3 / SIG P320 / Glock 17 MHS — full-size military service pistols',
    calibers: ['9x19mm Caseless', '9x19mm Standard', '10mm Combat Auto'],
    tiers: ['Tier 3+', 'Tier 4+'],
    flavor: 'Arcturus service pistols are what Civil Security issues. When you see an ADS sidearm, you\'re either looking at authorized force or someone who stole from authorized force.'
  },
  {
    batchId: 27,
    manufacturer: 'ARCTURUS DEFENSE SOLUTIONS',
    corponation: 'Arcturus Defense Solutions',
    count: 8,
    type: 'pistol',
    inspiration: 'SIG P365 / HK P30SK — compact military and law enforcement sidearms',
    calibers: ['9x19mm Caseless', '9x19mm Standard'],
    tiers: ['Tier 3+', 'Tier 4+'],
    flavor: 'Arcturus compact service pistols are issued to plainclothes Civil Security and corporate investigators. Understated, authoritative, and backed by the Arcturus maintenance network.'
  },
  {
    batchId: 28,
    manufacturer: 'ARCTURUS DEFENSE SOLUTIONS',
    corponation: 'Arcturus Defense Solutions',
    count: 10,
    type: 'rifle',
    inspiration: 'M4A1 / HK416 / CQBR family — standard military assault rifles and carbines',
    calibers: ['5.56mm Combat Caseless', '5.56mm Combat Standard', '6.8mm General Service'],
    tiers: ['Tier 3+', 'Military/law enforcement only'],
    flavor: 'Arcturus service rifles are the spine of Civil Security ground operations. Seeing one in civilian hands means something went wrong somewhere in the supply chain.'
  },
  {
    batchId: 29,
    manufacturer: 'ARCTURUS DEFENSE SOLUTIONS',
    corponation: 'Arcturus Defense Solutions',
    count: 8,
    type: 'rifle',
    inspiration: 'HK417 / M14 EBR / SCAR-H — battle rifles for harder targets',
    calibers: ['7.62mm Caseless', '7.62mm NATO Pattern', '6.8mm General Service'],
    tiers: ['Tier 4+', 'Military/law enforcement only'],
    flavor: 'ADS battle rifles are for environments where the standard carbine isn\'t enough. Issued to squad leaders, breach teams, and anyone operating in degraded or armored-threat environments.'
  },
  {
    batchId: 30,
    manufacturer: 'ARCTURUS DEFENSE SOLUTIONS',
    corponation: 'Arcturus Defense Solutions',
    count: 8,
    type: 'heavy',
    inspiration: 'M249 SAW / M240B / PKM — light and medium machine guns',
    calibers: ['5.56mm Combat Caseless', '7.62mm Caseless', '7.62mm NATO Pattern'],
    tiers: ['Tier 4+', 'Military/law enforcement only'],
    flavor: 'ADS machine guns are area-denial weapons. Seeing one deployed means Civil Security has decided this situation is a military situation.'
  },
  {
    batchId: 31,
    manufacturer: 'ARCTURUS DEFENSE SOLUTIONS',
    corponation: 'Arcturus Defense Solutions',
    count: 8,
    type: 'shotgun',
    inspiration: 'Benelli M4 / Mossberg 590A1 / Remington 870 MCS — military combat shotguns',
    calibers: ['12-gauge Standard', '12-gauge Caseless Slug', '12-gauge Less-Lethal Bean'],
    tiers: ['Tier 3+', 'Tier 4+'],
    flavor: 'ADS combat shotguns serve Civil Security door-breach and riot-control roles. The less-lethal loadings are technically non-lethal — technically.'
  },
  {
    batchId: 32,
    manufacturer: 'ARCTURUS DEFENSE SOLUTIONS',
    corponation: 'Arcturus Defense Solutions',
    count: 8,
    type: 'sniper',
    inspiration: 'Barrett M82 / M107 / KSVK — anti-materiel rifles for vehicle, structure, and armor defeat',
    calibers: ['12.7mm Anti-Materiel', '.50 Heavy Machine Gun', '.338 Precision Long'],
    tiers: ['Tier 5', 'Military/law enforcement only'],
    flavor: 'ADS anti-materiel platforms are for ending things at distance. Licensed to Arcturus Civil Security strike teams and corponation military forces. Nothing about them is subtle.'
  },

  // ── IRONSIDE ARMAMENTS (Arcturus Defense Solutions) — 4 batches ───────────
  {
    batchId: 33,
    manufacturer: 'IRONSIDE ARMAMENTS',
    corponation: 'Arcturus Defense Solutions',
    count: 10,
    type: 'rifle',
    inspiration: 'Dragunov SVD / PSG-1 / MSG-90 — heavy precision rifles and heavy-duty battle carbines',
    calibers: ['7.62mm NATO Pattern', '7.62mm Caseless', '.308 Precision Hunting'],
    tiers: ['Tier 3+', 'Tier 4+'],
    flavor: 'Ironside builds weapons to last decades of hard use. Heavier than ADS equivalents, more expensive than Arcturus civilian, but built for sustained operations in brutal conditions.'
  },
  {
    batchId: 34,
    manufacturer: 'IRONSIDE ARMAMENTS',
    corponation: 'Arcturus Defense Solutions',
    count: 8,
    type: 'shotgun',
    inspiration: 'USAS-12 / AA-12 / Kel-Tec KSG — heavy-duty and bullpup combat shotguns',
    calibers: ['12-gauge Standard', '12-gauge Caseless Slug', '10-gauge Heavy'],
    tiers: ['Tier 3+', 'Tier 4+'],
    flavor: 'Ironside shotguns are the ones you pick when the ADS M4 feels too fragile for the job. Built for long-duration operations where maintenance windows are theoretical.'
  },
  {
    batchId: 35,
    manufacturer: 'IRONSIDE ARMAMENTS',
    corponation: 'Arcturus Defense Solutions',
    count: 8,
    type: 'heavy',
    inspiration: 'M60E6 / MAG-58 / Negev NG7 — general purpose machine guns for extended suppression',
    calibers: ['7.62mm Caseless', '7.62mm NATO Pattern', '.50 Heavy Machine Gun'],
    tiers: ['Tier 4+', 'Military/law enforcement only'],
    flavor: 'Ironside machine guns are long-duration suppression platforms. Built to run hot for hours. Found on vehicle mounts, fixed installations, and the shoulders of very large operators.'
  },
  {
    batchId: 36,
    manufacturer: 'IRONSIDE ARMAMENTS',
    corponation: 'Arcturus Defense Solutions',
    count: 8,
    type: 'sniper',
    inspiration: 'M82A1 / Steyr HS .50 / PGM Hecate II — heavy anti-materiel rifles for long-range armor defeat',
    calibers: ['12.7mm Anti-Materiel', '.50 Heavy Machine Gun', '.338 Precision Long'],
    tiers: ['Tier 5', 'Military/law enforcement only'],
    flavor: 'Ironside anti-materiel rifles are built to kill vehicles. The humans are incidental. Found at the top of Civil Security armories and the bottom of very black markets.'
  },

  // ── CRUCIBLE INDUSTRIES (Crucible Genomics) — 6 batches ──────────────────
  {
    batchId: 37,
    manufacturer: 'CRUCIBLE INDUSTRIES',
    corponation: 'Crucible Genomics',
    count: 10,
    type: 'pistol',
    inspiration: 'HK USP / VP9 / P30 — quality tactical pistols for professionals',
    calibers: ['9x19mm Caseless', '.45 Caseless Combat', '10mm Combat Auto'],
    tiers: ['Tier 2+', 'Tier 3+'],
    flavor: 'Crucible pistols are for people who take weapons seriously enough to buy quality but don\'t have Arcturus contracts. The choice of private security, freelancers, and off-duty operators.'
  },
  {
    batchId: 38,
    manufacturer: 'CRUCIBLE INDUSTRIES',
    corponation: 'Crucible Genomics',
    count: 10,
    type: 'rifle',
    inspiration: 'HK G36 / XM8 / SL8 — polymer tactical rifles with modern design philosophy',
    calibers: ['5.56mm Combat Caseless', '5.56mm Combat Standard', '6.8mm General Service'],
    tiers: ['Tier 2+', 'Tier 3+'],
    flavor: 'Crucible rifles occupy the tactical civilian market ADS doesn\'t bother with. Quality of build the KP line can\'t match, price point that\'s actually achievable.'
  },
  {
    batchId: 39,
    manufacturer: 'CRUCIBLE INDUSTRIES',
    corponation: 'Crucible Genomics',
    count: 8,
    type: 'rifle',
    inspiration: 'G3 / CETME / HK91 family — roller-delayed battle rifles, known for reliability in adverse conditions',
    calibers: ['7.62mm NATO Pattern', '7.62mm Caseless', '.308 Precision Hunting'],
    tiers: ['Tier 2+', 'Tier 3+'],
    flavor: 'Crucible battle rifles inherit roller-delay DNA from two centuries back. Violent extraction, loud, accurate to distances where the ADS carbine gives up. Loved by people who shoot through cover.'
  },
  {
    batchId: 40,
    manufacturer: 'CRUCIBLE INDUSTRIES',
    corponation: 'Crucible Genomics',
    count: 8,
    type: 'shotgun',
    inspiration: 'HK FABARM / Benelli Nova — tactical semi-auto and pump shotguns for the professional civilian',
    calibers: ['12-gauge Standard', '12-gauge Caseless Slug', '20-gauge Compact'],
    tiers: ['Tier 2+', 'Tier 3+'],
    flavor: 'Crucible shotguns are as serious as their rifles. Not a door-popper or a panic gun — a precision close-quarters tool for people who run it deliberately.'
  },
  {
    batchId: 41,
    manufacturer: 'CRUCIBLE INDUSTRIES',
    corponation: 'Crucible Genomics',
    count: 8,
    type: 'smg',
    inspiration: 'MP7 / P90 / B&T APC9 — PDW-style SMGs with armor-defeating capability',
    calibers: ['4.6mm PDW Compact', '5.7mm High-Velocity Auto', '9x19mm Caseless'],
    tiers: ['Tier 2+', 'Tier 3+'],
    flavor: 'Crucible PDWs are what private security contracts prefer over cheap KP SMGs. Enough authority to defeat light armor, small enough for vehicle deployment.'
  },
  {
    batchId: 42,
    manufacturer: 'CRUCIBLE INDUSTRIES',
    corponation: 'Crucible Genomics',
    count: 8,
    type: 'smg',
    inspiration: 'MP5 SD / HK53 — suppressed SMG systems for low-observable operations',
    calibers: ['9x19mm Standard', '9x19mm Caseless', '.300 Compact Subsonic'],
    tiers: ['Tier 3+', 'Tier 4+'],
    flavor: 'Crucible suppressed SMGs are the operators\' choice for close-in quiet work. Legal to own in GLMZ with proper licensing — licensing that costs more than the weapon.'
  },

  // ── FORGE-SMITH COLLECTIVE (Crucible Genomics) — 5 batches ───────────────
  {
    batchId: 43,
    manufacturer: 'FORGE-SMITH COLLECTIVE',
    corponation: 'Crucible Genomics',
    count: 10,
    type: 'revolver',
    inspiration: 'Ruger Redhawk / S&W 686 / Taurus Raging Hunter — working-class service revolvers',
    calibers: ['.357 Service Magnum', '.44 Service Revolver', '.44 Magnum Revolver'],
    tiers: ['Tier 1+', 'Tier 2+'],
    flavor: 'Forge-Smith revolvers are built for people who work with their hands and might need to defend themselves while doing it. No frills, no electronics, no batteries, no excuses.'
  },
  {
    batchId: 44,
    manufacturer: 'FORGE-SMITH COLLECTIVE',
    corponation: 'Crucible Genomics',
    count: 8,
    type: 'revolver',
    inspiration: 'S&W Model 500 / Taurus Raging Bull / Freedom Arms — big-bore revolvers for heavy loads',
    calibers: ['.44 Magnum Revolver', '.454 High Pressure', '.460 Ultra Magnum', '.500 Revolver Maximum'],
    tiers: ['Tier 2+', 'Tier 3+'],
    flavor: 'Forge-Smith big-bore revolvers are for people who operate in environments where wildlife or armored threats require something more convincing than 9mm. Loud statement guns with practical applications.'
  },
  {
    batchId: 45,
    manufacturer: 'FORGE-SMITH COLLECTIVE',
    corponation: 'Crucible Genomics',
    count: 8,
    type: 'rifle',
    inspiration: 'Marlin 1895 / Winchester 1894 / Henry Big Boy — lever-action rifles, 200 years refined',
    calibers: ['.44 Magnum Revolver', '.357 Service Magnum', '.45 Auto Composite'],
    tiers: ['Tier 1+', 'Tier 2+'],
    flavor: 'Forge-Smith lever-actions are the weapon of people who think in generations. Mechanically simple, historically loaded, and surprisingly effective in 2200 with modern ammunition.'
  },
  {
    batchId: 46,
    manufacturer: 'FORGE-SMITH COLLECTIVE',
    corponation: 'Crucible Genomics',
    count: 8,
    type: 'rifle',
    inspiration: 'Remington 700 / Winchester Model 70 / Savage 110 — workingman\'s bolt-action rifles',
    calibers: ['.308 Precision Hunting', '.30 Springfield Long', '6.5mm Precision Match'],
    tiers: ['Tier 1+', 'Tier 2+'],
    flavor: 'Forge-Smith bolt rifles are hunting weapons that double as defensive weapons if needed. The same gun that feeds a family is the same gun that protects it.'
  },
  {
    batchId: 47,
    manufacturer: 'FORGE-SMITH COLLECTIVE',
    corponation: 'Crucible Genomics',
    count: 8,
    type: 'shotgun',
    inspiration: 'Ithaca 37 / Mossberg 500 / Winchester Model 12 — pump shotguns as working tools',
    calibers: ['12-gauge Standard', '20-gauge Compact', '10-gauge Heavy'],
    tiers: ['Tier 1+', 'Tier 2+'],
    flavor: 'Forge-Smith pump shotguns are tools. Used for pests, used for food, used for the kind of problems that don\'t have bureaucratic solutions. The most common long gun in GLMZ\'s outer zones.'
  },

  // ── CARRION DEFENSE WORKS (Crucible Genomics) — 3 batches ────────────────
  {
    batchId: 48,
    manufacturer: 'CARRION DEFENSE WORKS',
    corponation: 'Crucible Genomics',
    count: 8,
    type: 'pistol',
    inspiration: 'Chiappa Rhino / Mateba autorevolver / exotic semi-autos — unconventional designs with real performance',
    calibers: ['.357 Service Magnum', '10mm Combat Auto', '.50 Action Express'],
    tiers: ['Tier 2+', 'Tier 3+'],
    flavor: 'Carrion Defense makes weapons that don\'t look like weapons until they\'re being used. Buyers are specialists, eccentrics, and people whose taste in violence is highly specific.'
  },
  {
    batchId: 49,
    manufacturer: 'CARRION DEFENSE WORKS',
    corponation: 'Crucible Genomics',
    count: 8,
    type: 'rifle',
    inspiration: 'Kel-Tec RFB / RDB / bullpup designs — unconventional but functional configurations',
    calibers: ['7.62mm Caseless', '5.56mm Combat Caseless', '6.8mm General Service'],
    tiers: ['Tier 2+', 'Tier 3+'],
    flavor: 'Carrion rifles are for operators who want something nobody else will recognize. Functionally excellent, visually bizarre. The kind of weapon that shows up in incident reports as "unusual firearm."'
  },
  {
    batchId: 50,
    manufacturer: 'CARRION DEFENSE WORKS',
    corponation: 'Crucible Genomics',
    count: 8,
    type: 'shotgun',
    inspiration: 'SPAS-12 / SRM Arms 1216 / Neostead — exotic shotgun platforms with unique operating mechanisms',
    calibers: ['12-gauge Standard', '12-gauge Caseless Slug', '20-gauge Compact'],
    tiers: ['Tier 2+', 'Tier 3+'],
    flavor: 'Carrion shotguns are collector pieces that work. They cost three times a Forge-Smith and weigh twice as much, but they do things no conventional scattergun can.'
  },

  // ── VESPID DYNAMICS (Crucible Genomics) — 2 batches ──────────────────────
  {
    batchId: 51,
    manufacturer: 'VESPID DYNAMICS',
    corponation: 'Crucible Genomics',
    count: 8,
    type: 'rifle',
    inspiration: 'Experimental flechette rifles — O\'Dwyer VLE / SPIW lineage, 200 years of development',
    calibers: ['Magnetic Flechette Burst', '4mm Ferro Penetrator'],
    tiers: ['Tier 3+', 'Tier 4+'],
    flavor: 'Vespid flechette weapons are experimental products sold commercially because the military contracts never materialized. Unique terminal effects, unusual legality status, no standard maintenance ecosystem.'
  },
  {
    batchId: 52,
    manufacturer: 'VESPID DYNAMICS',
    corponation: 'Crucible Genomics',
    count: 8,
    type: 'pistol',
    inspiration: 'G11 pistol / exotic compact semi-autos — experimental compact platforms with unusual operating systems',
    calibers: ['4.6mm PDW Compact', 'Magnetic Flechette Burst', '4mm Ferro Penetrator'],
    tiers: ['Tier 3+', 'Tier 4+'],
    flavor: 'Vespid compact platforms are proof-of-concept weapons that made it to market. The kind of thing a well-funded freelancer or collector purchases when they want something nobody else has.'
  },

  // ── TESSERA CORPONATION (Tessera Corponation) — 4 batches ─────────────────
  {
    batchId: 53,
    manufacturer: 'TESSERA CORPONATION',
    corponation: 'Tessera Corponation',
    count: 10,
    type: 'pistol',
    inspiration: 'Walther PPQ / CZ P-10 — clean corporate-aesthetic service pistols',
    calibers: ['9x19mm Caseless', '9x19mm Standard', '.40 Smith Pattern'],
    tiers: ['Tier 2+', 'Tier 3+'],
    flavor: 'Tessera security pistols look expensive because they are. Clean lines, muted finish, corporate aesthetic. Issued to Tessera facility security and sold to the corporate Tier 3 market.'
  },
  {
    batchId: 54,
    manufacturer: 'TESSERA CORPONATION',
    corponation: 'Tessera Corponation',
    count: 8,
    type: 'pistol',
    inspiration: 'SIG P239 / Walther PPS — discreet compact pistols for corporate executive protection',
    calibers: ['9x19mm Caseless', '9x19mm Standard'],
    tiers: ['Tier 3+', 'Tier 4+'],
    flavor: 'Tessera compact pistols are executive accessories. Slim, well-finished, discreet enough to carry in a tailored jacket. The weapon of people who think of security as a lifestyle expense.'
  },
  {
    batchId: 55,
    manufacturer: 'TESSERA CORPONATION',
    corponation: 'Tessera Corponation',
    count: 8,
    type: 'rifle',
    inspiration: 'Steyr AUG / FN F2000 — clean bullpup rifles with distinctive corporate design language',
    calibers: ['5.56mm Combat Caseless', '6.8mm General Service', '5.56mm Combat Standard'],
    tiers: ['Tier 3+', 'Tier 4+'],
    flavor: 'Tessera rifles look like corporate property because they are. Every surface finished. Every edge radiused. The same design philosophy as Tessera\'s office furniture, expressed in polymer and steel.'
  },
  {
    batchId: 56,
    manufacturer: 'TESSERA CORPONATION',
    corponation: 'Tessera Corponation',
    count: 8,
    type: 'rifle',
    inspiration: 'Colt CM901 / Sig MCX — modular corporate carbines that change caliber and role',
    calibers: ['5.56mm Combat Caseless', '7.62mm Caseless', '6.8mm General Service'],
    tiers: ['Tier 3+', 'Tier 4+'],
    flavor: 'Tessera modular carbines serve corporate security forces that operate across multiple threat environments. One rifle, multiple configurations, all under the same maintenance contract.'
  },

  // ── STERLING-NAKAMURA (Tessera Corponation) — 4 batches ──────────────────
  {
    batchId: 57,
    manufacturer: 'STERLING-NAKAMURA',
    corponation: 'Tessera Corponation',
    count: 10,
    type: 'pistol',
    inspiration: 'Ruger-57 / FN Five-seveN — law enforcement pistols with enhanced penetration for body armor threats',
    calibers: ['5.7mm High-Velocity Auto', '9x19mm Caseless', '9x19mm Standard'],
    tiers: ['Tier 2+', 'Tier 3+'],
    flavor: 'Sterling-Nakamura service pistols are issued to licensed private law enforcement. The SN certification mark on a weapon means it came through proper channels — which is meaningful in GLMZ.'
  },
  {
    batchId: 58,
    manufacturer: 'STERLING-NAKAMURA',
    corponation: 'Tessera Corponation',
    count: 8,
    type: 'rifle',
    inspiration: 'Ruger MPR / Rock River LAR — compliance-optimized semi-auto rifles for law enforcement licensing',
    calibers: ['5.56mm Combat Standard', '9x19mm Caseless', '.300 Compact Subsonic'],
    tiers: ['Tier 2+', 'Tier 3+'],
    flavor: 'SN compliance rifles are designed to satisfy legal requirements while remaining functional. They carry all the required restrictions built-in, which is either a feature or an obstacle depending on who\'s asking.'
  },
  {
    batchId: 59,
    manufacturer: 'STERLING-NAKAMURA',
    corponation: 'Tessera Corponation',
    count: 8,
    type: 'shotgun',
    inspiration: 'Taurus Judge / S&W Governor — less-lethal compliance shotguns for law enforcement use',
    calibers: ['12-gauge Less-Lethal Bean', '9mm Rubber Less-Lethal', '.410 Pocket Shot'],
    tiers: ['Tier 2+', 'Tier 3+'],
    flavor: 'SN less-lethal platforms are civil compliance weapons. Designed to incapacitate, not kill — though GLMZ incident reports suggest the distinction is sometimes theoretical at close range.'
  },
  {
    batchId: 60,
    manufacturer: 'STERLING-NAKAMURA',
    corponation: 'Tessera Corponation',
    count: 8,
    type: 'rifle',
    inspiration: 'SR-25 / Mk.17 SCAR — law enforcement precision rifles for hostage/sniper response',
    calibers: ['6.5mm Precision Match', '7.62mm NATO Pattern', '7.62mm Caseless'],
    tiers: ['Tier 3+', 'Tier 4+'],
    flavor: 'SN precision rifles are issued to Tessera Civil Compliance snipers and sold to licensed security contractors. Proper documentation required — documentation that SN will verify, unlike other manufacturers.'
  },

  // ── AXIOM SYSTEMS (Tessera Corponation) — 4 batches ──────────────────────
  {
    batchId: 61,
    manufacturer: 'AXIOM SYSTEMS',
    corponation: 'Tessera Corponation',
    count: 10,
    type: 'pistol',
    inspiration: 'Smart pistols with BCI integration — Armatix-inspired but actually functional, networked weapons with user verification',
    calibers: ['9x19mm Caseless', '5.7mm High-Velocity Auto', '.40 Smith Pattern'],
    tiers: ['Tier 3+', 'Tier 4+'],
    flavor: 'Axiom smart pistols verify the shooter before they fire. Neural handshake, biometric confirmation, registered user only. This feature is alternately praised as revolutionary security and condemned as corporate surveillance of lethal force.'
  },
  {
    batchId: 62,
    manufacturer: 'AXIOM SYSTEMS',
    corponation: 'Tessera Corponation',
    count: 8,
    type: 'rifle',
    inspiration: 'Networked smart rifles with digital scopes, target acquisition, and BCI fire control',
    calibers: ['5.56mm Combat Caseless', '6.5mm Caseless Match', '6.8mm General Service'],
    tiers: ['Tier 3+', 'Tier 4+'],
    flavor: 'Axiom smart rifles have more processing power than most implants. Fire control AI, ballistic compensation, target prioritization — and a live data feed back to Tessera infrastructure. Always read the EULA.'
  },
  {
    batchId: 63,
    manufacturer: 'AXIOM SYSTEMS',
    corponation: 'Tessera Corponation',
    count: 8,
    type: 'energy',
    inspiration: 'BCI-integrated laser sidearms — energy weapons with neural trigger for faster response time',
    calibers: ['Class-1 Laser Cell', 'Class-2 Laser Cell'],
    tiers: ['Tier 3+', 'Tier 4+'],
    flavor: 'Axiom laser sidearms are the promise of energy weapons married to BCI precision. Neural trigger — faster than mechanical. But still subject to all the reliability issues energy weapons carry: battery drain, optic fouling, EMP vulnerability. Impressive at demos, spotty in the field.'
  },
  {
    batchId: 64,
    manufacturer: 'AXIOM SYSTEMS',
    corponation: 'Tessera Corponation',
    count: 8,
    type: 'coilgun',
    inspiration: 'Precision-grade BCI-integrated coilguns — accurate magnetic weapons with networked targeting',
    calibers: ['6mm Ferro Penetrator', '8mm Ferro Penetrator', 'Magnetic Composite Slug Standard'],
    tiers: ['Tier 3+', 'Tier 4+'],
    flavor: 'Axiom coilguns combine the reliability advantage of magnetic propulsion with BCI targeting integration. Unlike their laser siblings, these actually perform in wet, cold, and EMP environments. The premium of the Axiom line.'
  },

  // ── ZHENG-DAO HEAVY INDUSTRIES (Zheng-dao Bioelectric) — 5 batches ────────
  {
    batchId: 65,
    manufacturer: 'ZHENG-DAO HEAVY INDUSTRIES',
    corponation: 'Zheng-dao Bioelectric',
    count: 10,
    type: 'pistol',
    inspiration: 'Desert Eagle / IMI Magnum Research lineage — massive frame semi-auto pistols for heavy calibers',
    calibers: ['.50 Action Express', '.44 Magnum Revolver', '.357 Auto Express'],
    tiers: ['Tier 2+', 'Tier 3+'],
    flavor: 'Zheng-Dao heavy pistols are statements. Gas-operated rotating bolt, massive frame, chambered for rounds that were rifles-only in past centuries. Carried by people who want to be taken seriously before a trigger is pulled.'
  },
  {
    batchId: 66,
    manufacturer: 'ZHENG-DAO HEAVY INDUSTRIES',
    corponation: 'Zheng-dao Bioelectric',
    count: 8,
    type: 'revolver',
    inspiration: 'S&W Model 500 Competitor / Magnum Research BFR — industrial-grade large-frame revolvers',
    calibers: ['.500 Revolver Maximum', '.460 Ultra Magnum', '.454 High Pressure'],
    tiers: ['Tier 2+', 'Tier 3+'],
    flavor: 'Zheng-Dao heavy revolvers started as industrial tools — dealing with the wildlife and industrial hazards of the Great Lakes outer zones. The combat applications were inevitable.'
  },
  {
    batchId: 67,
    manufacturer: 'ZHENG-DAO HEAVY INDUSTRIES',
    corponation: 'Zheng-dao Bioelectric',
    count: 8,
    type: 'rifle',
    inspiration: 'KSVK / OSV-96 / heavy industrial carbines — Zheng-Dao adaptation of industrial tools to combat roles',
    calibers: ['12.7mm Anti-Materiel', '.50 Heavy Machine Gun', '.338 Precision Long'],
    tiers: ['Tier 3+', 'Tier 4+'],
    flavor: 'Zheng-Dao heavy rifles exist because their engineers were already solving industrial problems that required this kind of energy. Combat applications came later and the transition was seamless.'
  },
  {
    batchId: 68,
    manufacturer: 'ZHENG-DAO HEAVY INDUSTRIES',
    corponation: 'Zheng-dao Bioelectric',
    count: 8,
    type: 'heavy',
    inspiration: 'M2HB / NSV / KPV — heavy machine guns at the top end of conventional ballistics',
    calibers: ['.50 Heavy Machine Gun', '12.7mm Anti-Materiel', '7.62mm Caseless'],
    tiers: ['Tier 4+', 'Tier 5'],
    flavor: 'Zheng-Dao heavy machine guns are vehicle and emplacement weapons. The kind of hardware that signals escalation — when Civil Security or corponation forces mount one, the situation has changed.'
  },
  {
    batchId: 69,
    manufacturer: 'ZHENG-DAO HEAVY INDUSTRIES',
    corponation: 'Zheng-dao Bioelectric',
    count: 8,
    type: 'coilgun',
    inspiration: 'Industrial-grade coilguns adapted from electromagnetic construction and demolition equipment',
    calibers: ['12mm Ferro Penetrator', '8mm Ferro Penetrator', 'Magnetic Composite Slug Standard'],
    tiers: ['Tier 3+', 'Tier 4+'],
    flavor: 'Zheng-Dao industrial coilguns were never designed as weapons. The path from electromagnetic pile-driver to combat weapon required only a different power regulation profile. Extremely powerful, extremely reliable, extremely heavy.'
  },

  // ── BLACKWOOD COMMERCIAL GROUP (Zheng-dao Bioelectric) — 4 batches ────────
  {
    batchId: 70,
    manufacturer: 'BLACKWOOD COMMERCIAL GROUP',
    corponation: 'Zheng-dao Bioelectric',
    count: 10,
    type: 'rifle',
    inspiration: 'Browning BAR / Winchester Model 70 Sporter — premium hunting rifles for the commercial market',
    calibers: ['.308 Precision Hunting', '.30 Springfield Long', '6.5mm Precision Match'],
    tiers: ['Tier 2+', 'Tier 3+'],
    flavor: 'Blackwood hunting rifles are sold at proper shops with proper service contracts. The commercial face of Zheng-Dao — polished stock, professional presentation, and a ballistic performance that respects the tradition of the thing.'
  },
  {
    batchId: 71,
    manufacturer: 'BLACKWOOD COMMERCIAL GROUP',
    corponation: 'Zheng-dao Bioelectric',
    count: 8,
    type: 'shotgun',
    inspiration: 'Browning Citori / Beretta 686 — over-under and side-by-side sporting shotguns',
    calibers: ['12-gauge Standard', '20-gauge Compact', '.410 Pocket Shot'],
    tiers: ['Tier 2+', 'Tier 3+'],
    flavor: 'Blackwood sporting shotguns are the most civilized products in the Zheng-Dao lineup. They exist for trap, skeet, bird, and the kind of estate weekend that Tier 3 corporate culture considers a flex.'
  },
  {
    batchId: 72,
    manufacturer: 'BLACKWOOD COMMERCIAL GROUP',
    corponation: 'Zheng-dao Bioelectric',
    count: 8,
    type: 'pistol',
    inspiration: 'Springfield 1911 EMP / S&W SW1911 — premium commercial pistols for the sport-shooting civilian',
    calibers: ['.45 Auto Composite', '.45 Caseless Combat', '9x19mm Standard'],
    tiers: ['Tier 2+', 'Tier 3+'],
    flavor: 'Blackwood commercial pistols are the kind of weapon bought at a licensed dealer with an appointment, not at a counter. Proper provenance, proper paperwork, proper quality.'
  },
  {
    batchId: 73,
    manufacturer: 'BLACKWOOD COMMERCIAL GROUP',
    corponation: 'Zheng-dao Bioelectric',
    count: 8,
    type: 'rifle',
    inspiration: 'CZ 455 / Anschutz 1710 — precision target rifles for competitive sport shooting',
    calibers: ['6.5mm Precision Match', '6.5mm Caseless Match', '.22 Short-Long'],
    tiers: ['Tier 2+', 'Tier 3+'],
    flavor: 'Blackwood sport rifles are built for competition in the organized shooting leagues that Tessera and Zheng-Dao both sponsor. More culture than combat, which is either their strength or their weakness.'
  },

  // ── ENERGY WEAPONS — cross-manufacturer — 7 batches ──────────────────────
  {
    batchId: 74,
    manufacturer: 'ARCTURUS DEFENSE SOLUTIONS',
    corponation: 'Arcturus Defense Solutions',
    count: 8,
    type: 'energy',
    inspiration: 'Military laser pistols — directed energy sidearms for specialized roles',
    calibers: ['Class-1 Laser Cell', 'Class-2 Laser Cell'],
    tiers: ['Tier 3+', 'Tier 4+'],
    flavor: 'ADS laser pistols are issued as supplemental sidearms in environments where conventional ballistics are contraindicated — pressurized environments, flammable atmospheres, electronic-rich installations. LESS RELIABLE than the KP sidearms they supplement: batteries die, optics foul, containment fails in wet or cold conditions.'
  },
  {
    batchId: 75,
    manufacturer: 'ARCTURUS DEFENSE SOLUTIONS',
    corponation: 'Arcturus Defense Solutions',
    count: 8,
    type: 'energy',
    inspiration: 'Military laser rifles — sustained-fire directed energy for area suppression',
    calibers: ['Class-2 Laser Cell', 'Class-3 Laser Cell'],
    tiers: ['Tier 4+', 'Military/law enforcement only'],
    flavor: 'ADS laser rifles are doctrine weapons, not preference weapons. Used in specific tactical situations by trained operators who understand their limitations. Veterans who\'ve used them in wet weather or EMP-adjacent environments mostly prefer their conventional counterparts.'
  },
  {
    batchId: 76,
    manufacturer: 'TESSERA CORPONATION',
    corponation: 'Tessera Corponation',
    count: 8,
    type: 'energy',
    inspiration: 'Corporate plasma pistols — elegant energy sidearms for Tessera corporate security aesthetic',
    calibers: ['Plasma Core Type-A', 'Plasma Core Type-B'],
    tiers: ['Tier 3+', 'Tier 4+'],
    flavor: 'Tessera plasma pistols are the most stylish weapons in GLMZ — and some of the most unreliable. Beautiful containment vessels, clean design, proprietary power cells. Carried by corporate security who look good in photos. Avoided by corporate security who expect to get wet.'
  },
  {
    batchId: 77,
    manufacturer: 'TESSERA CORPONATION',
    corponation: 'Tessera Corponation',
    count: 8,
    type: 'energy',
    inspiration: 'Corporate plasma rifles — heavy energy weapons for Tessera security forces',
    calibers: ['Plasma Core Type-B', 'Plasma Core Type-C'],
    tiers: ['Tier 4+', 'Tier 5'],
    flavor: 'Tessera plasma rifles are the premier energy weapon aesthetic of the mid-tier corporate world. Deeply impressive. Genuinely lethal. Genuinely prone to plasma containment failure in any weather that isn\'t climate-controlled office space.'
  },
  {
    batchId: 78,
    manufacturer: 'VESPID DYNAMICS',
    corponation: 'Crucible Genomics',
    count: 8,
    type: 'energy',
    inspiration: 'Experimental plasma weapons — pushing energy weapon technology further than established doctrine',
    calibers: ['Plasma Core Type-A', 'Plasma Core Type-B', 'Plasma Core Type-C'],
    tiers: ['Tier 3+', 'Tier 4+'],
    flavor: 'Vespid plasma weapons are experimental products sold commercially. Their failure modes are novel, their performance ceiling is high, and their warranty is very clearly non-transferable. The choice of people who find reliable weapons boring.'
  },
  {
    batchId: 79,
    manufacturer: 'ZHENG-DAO HEAVY INDUSTRIES',
    corponation: 'Zheng-dao Bioelectric',
    count: 8,
    type: 'energy',
    inspiration: 'Industrial laser weapons — adapted from heavy manufacturing and demolition tools',
    calibers: ['Class-2 Laser Cell', 'Class-3 Laser Cell'],
    tiers: ['Tier 3+', 'Tier 4+'],
    flavor: 'Zheng-Dao laser weapons are industrial tools that happen to be lethal. No pretense of elegance. The Class-3 cell equivalents will cut through light vehicle plating given enough exposure time, which is their appeal and their problem: exposure time requires the operator to keep pointing the thing.'
  },
  {
    batchId: 80,
    manufacturer: 'AXIOM SYSTEMS',
    corponation: 'Tessera Corponation',
    count: 8,
    type: 'energy',
    inspiration: 'BCI-integrated laser weapons — neural aim assist for energy weapons',
    calibers: ['Class-1 Laser Cell', 'Class-2 Laser Cell'],
    tiers: ['Tier 3+', 'Tier 4+'],
    flavor: 'Axiom laser sidearms add neural trigger to an already-fragile platform. The BCI integration genuinely improves accuracy — when the weapon functions. The data feed back to Tessera is non-optional. Field operators have learned to pack a conventional backup.'
  },

  // ── COILGUN/MAGNETIC — cross-manufacturer — 7 batches ────────────────────
  {
    batchId: 81,
    manufacturer: 'ARCTURUS DEFENSE SOLUTIONS',
    corponation: 'Arcturus Defense Solutions',
    count: 8,
    type: 'coilgun',
    inspiration: 'Military-grade coilguns — standard-issue magnetic weapons for specialized Arcturus roles',
    calibers: ['6mm Ferro Penetrator', '8mm Ferro Penetrator', 'Magnetic Composite Slug Standard'],
    tiers: ['Tier 4+', 'Military/law enforcement only'],
    flavor: 'ADS coilguns are the quiet option. No propellant signature, no sound spike, no brass on the floor. More reliable than energy weapons, on par with conventional firearms — and increasingly standard-issue for operations requiring reduced forensic signature.'
  },
  {
    batchId: 82,
    manufacturer: 'KANG-PETROV ARMS',
    corponation: 'Slagworks Industrial',
    count: 8,
    type: 'coilgun',
    inspiration: 'Mass-market magnetic pistols — affordable coilgun for the civilian Tier 1–2 market',
    calibers: ['4mm Ferro Penetrator', '6mm Ferro Penetrator'],
    tiers: ['Tier 2+', 'Tier 3+'],
    flavor: 'KP mass-market magnetic weapons are the affordable coilgun option — not quite as cheap as their conventional pistols, but the lack of propellant cost over time makes them economically competitive. Shelf workers who can afford the initial investment appreciate never needing to buy powder again.'
  },
  {
    batchId: 83,
    manufacturer: 'FORGE-SMITH COLLECTIVE',
    corponation: 'Crucible Genomics',
    count: 8,
    type: 'coilgun',
    inspiration: 'Magnetically-boosted revolvers — hybrid magnetic/mechanical revolver mechanisms',
    calibers: ['Magnetic Composite Slug Standard', '8mm Ferro Penetrator', '6mm Ferro Penetrator'],
    tiers: ['Tier 2+', 'Tier 3+'],
    flavor: 'Forge-Smith mag-boosted revolvers are a typically practical FS solution: take a reliable revolver mechanism, add a magnetic acceleration stage to the barrel, get a hybrid that\'s more reliable than energy and hits harder than pure conventional. Ugly, heavy, and beloved.'
  },
  {
    batchId: 84,
    manufacturer: 'IRONSIDE ARMAMENTS',
    corponation: 'Arcturus Defense Solutions',
    count: 8,
    type: 'coilgun',
    inspiration: 'Heavy coilguns — large-bore magnetic weapons for vehicle and structure penetration',
    calibers: ['12mm Ferro Penetrator', 'Magnetic Composite Slug Standard'],
    tiers: ['Tier 4+', 'Military/law enforcement only'],
    flavor: 'Ironside heavy coilguns are breach weapons. They go through walls, vehicles, and light armor with the same mechanical reliability their conventional firearms line is known for. Very few moving parts. Very high consequences when the trigger is pulled.'
  },
  {
    batchId: 85,
    manufacturer: 'CRUCIBLE INDUSTRIES',
    corponation: 'Crucible Genomics',
    count: 8,
    type: 'coilgun',
    inspiration: 'Tactical coilguns — professional-grade magnetic weapons for operator market',
    calibers: ['6mm Ferro Penetrator', '8mm Ferro Penetrator', 'Magnetic Flechette Burst'],
    tiers: ['Tier 3+', 'Tier 4+'],
    flavor: 'Crucible tactical coilguns bring the same professional quality standard as their conventional firearms to the magnetic market. Operators who use Crucible rifles often transition to Crucible coilguns for environments where propellant residue creates problems.'
  },
  {
    batchId: 86,
    manufacturer: 'AXIOM SYSTEMS',
    corponation: 'Tessera Corponation',
    count: 8,
    type: 'coilgun',
    inspiration: 'Precision BCI-integrated coilguns — the best smart weapon Axiom makes',
    calibers: ['6mm Ferro Penetrator', '8mm Ferro Penetrator', 'Magnetic Composite Slug Standard'],
    tiers: ['Tier 3+', 'Tier 4+'],
    flavor: 'Axiom precision coilguns are the flagship product of the Axiom line. BCI integration on a platform that actually works in the rain. Neural targeting, magnetic propulsion, and a reliability profile that justifies the premium. The weapon ADS operators wish their laser lines were.'
  },
  {
    batchId: 87,
    manufacturer: 'ZHENG-DAO HEAVY INDUSTRIES',
    corponation: 'Zheng-dao Bioelectric',
    count: 8,
    type: 'coilgun',
    inspiration: 'Industrial-converted heavy coilguns — Zheng-Dao applies industrial electromagnetic tech to anti-armor roles',
    calibers: ['12mm Ferro Penetrator', '8mm Ferro Penetrator', 'Magnetic Composite Slug Standard'],
    tiers: ['Tier 3+', 'Tier 4+'],
    flavor: 'Zheng-Dao coilguns exist because the same electromagnetics that drive their industrial equipment can drive a slug through 40mm of plate. The engineering conversion was trivial. The implications for corponation security arms races were not.'
  },

  // ── MISC / ANTIQUE-PATTERN — 4 batches ───────────────────────────────────
  {
    batchId: 88,
    manufacturer: 'VOLKOV-SAITO PRECISION',
    corponation: 'Zheng-dao Bioelectric',
    count: 8,
    type: 'pistol',
    inspiration: '1911 revival market — premium 1911-pattern pistols for collectors and traditionalists',
    calibers: ['.45 Auto Composite', '.45 Caseless Combat', '9x19mm Standard'],
    tiers: ['Tier 3+', 'Tier 4+'],
    flavor: 'VS 1911-revival pistols are the definitive answer to the question: what would John Browning design if he had 200 more years and access to GLMZ manufacturing? Single-action steel, but machined to a tolerance that 2011 couldn\'t dream of.'
  },
  {
    batchId: 89,
    manufacturer: 'FORGE-SMITH COLLECTIVE',
    corponation: 'Crucible Genomics',
    count: 8,
    type: 'revolver',
    inspiration: 'Classic revolver revivals — Colt SAA / Python / Detective Special patterns carried forward',
    calibers: ['.357 Service Magnum', '.45 Auto Composite', '.44 Magnum Revolver'],
    tiers: ['Tier 2+', 'Tier 3+'],
    flavor: 'Forge-Smith classic-pattern revolvers are for people who believe the original design was correct. They\'re not wrong. Two centuries of refinement on fundamentally sound mechanisms have produced weapons that embarrass modern designs in the areas that matter.'
  },
  {
    batchId: 90,
    manufacturer: 'BLACKWOOD COMMERCIAL GROUP',
    corponation: 'Zheng-dao Bioelectric',
    count: 8,
    type: 'rifle',
    inspiration: 'Classic combination guns / drilling patterns — multi-barrel hunting rifles combining rifle and shotgun barrels',
    calibers: ['.308 Precision Hunting', '12-gauge Standard', '.30 Springfield Long'],
    tiers: ['Tier 2+', 'Tier 3+'],
    flavor: 'Blackwood combination guns are luxury anachronisms — double rifle, drilling, and cape gun patterns updated for 2200 tolerances and materials. Carried by people who consider themselves more hunter than gunfighter.'
  },
  {
    batchId: 91,
    manufacturer: 'BLACKWOOD COMMERCIAL GROUP',
    corponation: 'Zheng-dao Bioelectric',
    count: 8,
    type: 'rifle',
    inspiration: 'Target pistols and sport rifles for competitive shooting — GLMZ organized competition circuit',
    calibers: ['.22 Short-Long', '6.5mm Precision Match', '9x19mm Standard'],
    tiers: ['Tier 2+', 'Tier 3+'],
    flavor: 'Blackwood target platform weapons serve the GLMZ competitive shooting circuit — an organized sport with Tessera and Zheng-Dao sponsorship that functions partly as a recruiting ground for both corporate security and freelancer networks.'
  },

  // ── ADDITIONAL COVERAGE BATCHES to reach 100 ─────────────────────────────
  {
    batchId: 92,
    manufacturer: 'HEARTHSTONE FIREARMS',
    corponation: 'Slagworks Industrial',
    count: 8,
    type: 'pistol',
    inspiration: 'Budget semi-auto pistols — additional Hearthstone variants for different Tier 1 niches',
    calibers: ['9x19mm Standard', '.40 Smith Pattern', '.45 Auto Composite'],
    tiers: ['Tier 1+'],
    flavor: 'More Hearthstone workhorse pistols filling gaps in the Tier 1 market: slightly better than Meridian, never as good as KP. The guns people graduate to after their first Meridian breaks.'
  },
  {
    batchId: 93,
    manufacturer: 'CRUCIBLE INDUSTRIES',
    corponation: 'Crucible Genomics',
    count: 8,
    type: 'smg',
    inspiration: 'Additional Crucible SMG variants — suppressed and compact PDW configurations',
    calibers: ['9x19mm Caseless', '.300 Compact Subsonic', '9x19mm Standard'],
    tiers: ['Tier 2+', 'Tier 3+'],
    flavor: 'More Crucible compact platforms for contractors and private security who operate in close urban environments where rifle-length weapons create problems.'
  },
  {
    batchId: 94,
    manufacturer: 'ARCTURUS DEFENSE SOLUTIONS',
    corponation: 'Arcturus Defense Solutions',
    count: 8,
    type: 'smg',
    inspiration: 'ADS service SMGs — military and law enforcement compact weapons',
    calibers: ['9x19mm Caseless', '4.6mm PDW Compact', '5.7mm High-Velocity Auto'],
    tiers: ['Tier 3+', 'Military/law enforcement only'],
    flavor: 'Arcturus service SMGs for Civil Security vehicle crews, facility security, and officers who carry a long gun as secondary.'
  },
  {
    batchId: 95,
    manufacturer: 'STERLING-NAKAMURA',
    corponation: 'Tessera Corponation',
    count: 8,
    type: 'pistol',
    inspiration: 'Compliance-certified revolvers for civil enforcement — SN-branded revolvers for law enforcement backup use',
    calibers: ['.38 Service Revolver', '.357 Service Magnum', '9mm Rubber Less-Lethal'],
    tiers: ['Tier 2+', 'Tier 3+'],
    flavor: 'Sterling-Nakamura compliance revolvers: backup weapons for licensed law enforcement, certified less-lethal configurations for civil control operations.'
  },
  {
    batchId: 96,
    manufacturer: 'NKOMO-LINDQVIST',
    corponation: 'Slagworks Industrial',
    count: 8,
    type: 'rifle',
    inspiration: 'Compact carbines and PDWs — NL applies compact carry philosophy to rifle-caliber platforms',
    calibers: ['5.56mm Combat Caseless', '9x19mm Caseless', '4.6mm PDW Compact'],
    tiers: ['Tier 2+', 'Tier 3+'],
    flavor: 'Nkomo-Lindqvist compact carbines for professionals who need rifle-caliber performance in a package that fits under a jacket. Expensive for Slagworks lineage, worth it for what they are.'
  },
  {
    batchId: 97,
    manufacturer: 'CARRION DEFENSE WORKS',
    corponation: 'Crucible Genomics',
    count: 8,
    type: 'smg',
    inspiration: 'Exotic compact weapons — Carrion applies its unconventional design philosophy to SMG platforms',
    calibers: ['9x19mm Caseless', '.357 Auto Express', '10mm Combat Auto'],
    tiers: ['Tier 2+', 'Tier 3+'],
    flavor: 'Carrion SMGs for operators who want a compact weapon that nobody else in the room will recognize. Functionally excellent and visually unlike anything on the conventional market.'
  },
  {
    batchId: 98,
    manufacturer: 'TESSERA CORPONATION',
    corponation: 'Tessera Corponation',
    count: 8,
    type: 'smg',
    inspiration: 'Corporate compact security weapons — clean aesthetic SMGs for Tessera facility security',
    calibers: ['9x19mm Caseless', '5.7mm High-Velocity Auto'],
    tiers: ['Tier 2+', 'Tier 3+'],
    flavor: 'Tessera compact security weapons match the corporate aesthetic. Everything matte, everything clean, everything branded. The weapon of people in Tessera-branded body armor who are very serious about facility perimeter control.'
  },
  {
    batchId: 99,
    manufacturer: 'KANG-PETROV ARMS',
    corponation: 'Slagworks Industrial',
    count: 8,
    type: 'rifle',
    inspiration: 'KP precision and designated marksman rifles — budget precision options for the Tier 1–2 market',
    calibers: ['7.62mm NATO Pattern', '6.5mm Precision Match', '.308 Precision Hunting'],
    tiers: ['Tier 1+', 'Tier 2+'],
    flavor: 'KP precision rifles: the affordable option when a freelancer needs reach. Not as good as Volkov-Saito or Arcturus. Good enough to make the shot at 600 meters if the operator is.'
  },
  {
    batchId: 100,
    manufacturer: 'FORGE-SMITH COLLECTIVE',
    corponation: 'Crucible Genomics',
    count: 8,
    type: 'pistol',
    inspiration: 'Forge-Smith working pistols — practical semi-autos for the trades and outer-zone economy',
    calibers: ['.357 Auto Express', '10mm Combat Auto', '.45 Auto Composite'],
    tiers: ['Tier 1+', 'Tier 2+'],
    flavor: 'Forge-Smith pistols for people who use their hands all day and carry a weapon because the outer zones require it. Practical, durable, backed by a repair network that actually exists in the places people live.'
  }
];

// ── Weapon system prompt builder ──────────────────────────────────────────────
function buildWeaponSystemPrompt() {
  return WORLD_SYSTEM + `

You generate individual firearm entries for GLMZ 2200. Return a JSON array only — no markdown, no explanation.

Each entry must have EXACTLY these fields:
{
  "id": "<32-char hex string>",
  "name": "<Manufacturer Model-Designation 'Nickname'> — e.g. Kang-Petrov KP-19 'Workhorse'",
  "type": "weapon",
  "aliases": ["Nickname", "Model number", "Street name"],
  "category": "pistol|revolver|rifle|smg|shotgun|sniper|dmr|heavy|energy|coilgun",
  "description": "<3 paragraphs: first = technical specs and operation; second = cultural/social context, class dynamics, who buys it; third = story flavor — grit, wear, history, feel>",
  "manufacturer": "<MANUFACTURER NAME IN CAPS>",
  "tier_availability": "Tier 1+|Tier 2+|Tier 3+|Tier 4+|Tier 5|Military/law enforcement only",
  "legality": "Legal — standard registration|Legal — minimal registration|Restricted — licensed carry|Military/law enforcement only",
  "base_technologies": ["tech1", "tech2"],
  "specifications": "caliber: X\\neffective_range: Xm\\nrate_of_fire: Semi-automatic|etc\\nmagazine_capacity: X rounds\\nweight: X kg",
  "tactical_use": "<one paragraph on tactical role and how operators run it>",
  "cultural_context": "<one paragraph on cultural significance in GLMZ, class meaning, street reputation>",
  "known_users": ["type of user1", "type of user2"],
  "story_hooks": ["Specific hook 1.", "Specific hook 2."],
  "ammunition_type": ["caliber name"],
  "tags": ["weapon", "<category>", "other relevant tags"]
}

CRITICAL RULES:
- Weapon names: "MANUFACTURER Model-Designation 'Nickname'" — DO NOT copy real brand names (no Glock, Beretta, etc.)
- Energy weapons: MUST include reliability warnings — batteries die, optics foul, plasma containment fails in wet/cold/EMP conditions
- Coilgun/magnetic: reliable, no propellant fouling, preferred for operations requiring clean signature
- Story hooks must be specific and memorable, not generic
- Tags always include "weapon" and the weapon category
- Class tension is real: a KP pistol and an Arcturus pistol occupy different social worlds even if they fire the same caliber`;
}

// ── Weapon batch generation ───────────────────────────────────────────────────
async function generateWeaponBatch(batch, existingNames, batchIndex, totalBatches) {
  const label = `batch ${batch.batchId}/${totalBatches} [${batch.manufacturer}/${batch.type}]`;

  // Count existing weapons from this manufacturer+type to determine skip
  const existingThisCombo = [...existingNames].filter(n =>
    n.startsWith(batch.manufacturer.split(' ')[0]) ||
    n.toLowerCase().includes(batch.manufacturer.toLowerCase().split(' ')[0].toLowerCase())
  ).length;
  // Simple skip: if we already have enough weapons in the dir, we'll just try and skip existing
  // (writeEntityFile is idempotent on filename)

  console.log(`\n[${label}] Generating ${batch.count} ${batch.type}s`);
  console.log(`  Inspiration: ${batch.inspiration}`);
  console.log(`  Calibers: ${batch.calibers.join(', ')}`);

  if (DRY_RUN) { console.log('  [DRY RUN] Skipping API call.'); return { written: 0, skipped: 0 }; }

  const system = buildWeaponSystemPrompt();

  const recentNames = [...existingNames].slice(-40).join(', ');

  const user = `Generate ${batch.count} unique firearms for these parameters:

MANUFACTURER: ${batch.manufacturer} (${batch.corponation})
WEAPON TYPE: ${batch.type}
CALIBERS (use at least these, may add variants): ${batch.calibers.join(', ')}
TIER ACCESS: ${batch.tiers.join(', ')}
DESIGN INSPIRATION: ${batch.inspiration}
FLAVOR: ${batch.flavor}

Recent weapon names already generated (DO NOT duplicate, but DO create new original names in the same manufacturer family):
${recentNames || 'none yet'}

Generate ${batch.count} DISTINCT weapons. Each needs a unique model designation and nickname. Return ONLY the JSON array.`;

  let items;
  try {
    items = await callClaudeWithRetry(system, user, label);
  } catch (e) {
    console.error(`  [${label}] FAILED after retries: ${e.message}`);
    return { written: 0, skipped: 0 };
  }

  let written = 0, skipped = 0;
  for (const item of items) {
    if (!item.id) item.id = newId();
    item.type = 'weapon';
    // Enforce manufacturer field
    if (!item.manufacturer) item.manufacturer = batch.manufacturer;
    const result = writeEntityFile(WEAPON_DIR, item);
    if (result.skipped) {
      skipped++;
    } else {
      written++;
      existingNames.add(item.name);
      console.log(`    + ${item.name}`);
    }
  }
  console.log(`  [${label}] Written: ${written}, Skipped: ${skipped}`);
  return { written, skipped };
}

// ── Main ──────────────────────────────────────────────────────────────────────
async function main() {
  console.log('=== GENERATE EVERYDAY WEAPONS ===');
  console.log(`Model: ${MODEL}`);
  console.log(`Weapon output: ${WEAPON_DIR}`);
  console.log(`Ammo output:   ${AMMO_DIR}`);
  if (DRY_RUN)      console.log('MODE: DRY RUN (no API calls)');
  if (AMMO_ONLY)    console.log('MODE: AMMO ONLY');
  if (WEAPONS_ONLY) console.log('MODE: WEAPONS ONLY');
  if (BATCH_LIMIT)  console.log(`LIMIT: ${BATCH_LIMIT} batches`);

  // Ensure output dirs exist
  if (!fs.existsSync(WEAPON_DIR)) fs.mkdirSync(WEAPON_DIR, { recursive: true });
  if (!fs.existsSync(AMMO_DIR))   fs.mkdirSync(AMMO_DIR,   { recursive: true });

  // ── Phase 1: Ammunition ──
  if (!WEAPONS_ONLY) {
    await generateAllAmmo();
  }

  // ── Phase 2: Weapons ──
  if (!AMMO_ONLY) {
    console.log('\n── WEAPON GENERATION ──────────────────────────────────');
    const existingWeaponNames = getExistingNames(WEAPON_DIR);
    console.log(`  Existing weapon files: ${getExistingFilenames(WEAPON_DIR).size}`);

    const batchesToRun = BATCH_LIMIT ? BATCHES.slice(0, BATCH_LIMIT) : BATCHES;
    let totalWritten = 0, totalSkipped = 0;

    for (let i = 0; i < batchesToRun.length; i++) {
      const batch = batchesToRun[i];
      const { written, skipped } = await generateWeaponBatch(
        batch, existingWeaponNames, i + 1, batchesToRun.length
      );
      totalWritten  += written;
      totalSkipped  += skipped;

      // Rate limit wait between batches (skip on last batch)
      if (i < batchesToRun.length - 1 && !DRY_RUN) {
        console.log(`  Waiting ${WAIT_MS / 1000}s for rate limit...`);
        await sleep(WAIT_MS);
      }
    }

    console.log('\n── WEAPON GENERATION COMPLETE ──────────────────────');
    console.log(`  Total written: ${totalWritten}`);
    console.log(`  Total skipped (already existed): ${totalSkipped}`);
  }

  // ── Final stats ──
  console.log('\n=== FINAL STATS ===');
  const finalWeaponCount = getExistingFilenames(WEAPON_DIR).size;
  const finalAmmoCount   = getExistingFilenames(AMMO_DIR).size;
  console.log(`Weapons in ${WEAPON_DIR}: ${finalWeaponCount}`);
  console.log(`Ammo     in ${AMMO_DIR}: ${finalAmmoCount}`);
  console.log('=== DONE ===');
}

main().catch(e => {
  console.error('Fatal error:', e);
  process.exit(1);
});
