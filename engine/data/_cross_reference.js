/**
 * Cross-reference script: populate related_entities on all entities
 * by scanning text fields for mentions of other entity names.
 *
 * Usage: node _cross_reference.js [--dry-run]
 */

const fs = require('fs');
const path = require('path');

const DRY_RUN = process.argv.includes('--dry-run');
const dataDir = __dirname;

// Subdirectories to process
const SUBDIRS = [
  'ammunition', 'apparel', 'archetypes', 'automata', 'characters',
  'consumer_goods', 'corponations', 'cyberware', 'documents',
  'entertainment', 'equipment', 'factions', 'genemods', 'materials',
  'news', 'pharmaceuticals', 'places', 'quotes', 'subsidiaries',
  'synthetics', 'technology', 'transportation', 'vocabulary', 'weaponry'
];

// Common words to skip even if they happen to be entity names
const SKIP_WORDS = new Set([
  'the', 'circuit', 'edge', 'ghost', 'signal', 'code', 'wire', 'zero',
  'void', 'pulse', 'echo', 'line', 'node', 'link', 'core', 'flux',
  'mark', 'null', 'grid', 'base', 'apex', 'root', 'data', 'black',
  'white', 'blue', 'grey', 'gray', 'gold', 'iron', 'bone', 'blood',
  'fire', 'smoke', 'dust', 'rust', 'salt', 'rain', 'snow', 'cold',
  'dark', 'dawn', 'dusk', 'noon', 'night', 'west', 'east', 'north',
  'south', 'high', 'deep', 'long', 'wide', 'thin', 'flat', 'hard',
  'soft', 'loud', 'dead', 'live', 'open', 'last', 'next', 'full',
  'half', 'back', 'down', 'over', 'under', 'near', 'away', 'home',
  'door', 'wall', 'roof', 'room', 'floor', 'block', 'tower', 'bridge',
  'road', 'path', 'gate', 'lock', 'ring', 'band', 'chain', 'steel',
  'glass', 'stone', 'sand', 'wood', 'silk', 'cloth', 'skin', 'hand',
  'face', 'eyes', 'mouth', 'mind', 'body', 'soul', 'word', 'name',
  'time', 'year', 'city', 'land', 'lake', 'river', 'field', 'hill',
  'real', 'true', 'good', 'free', 'safe', 'pure', 'type', 'kind',
  'form', 'size', 'mass', 'rate', 'cost', 'pack', 'unit', 'tier',
  'note', 'term', 'rule', 'test', 'case', 'work', 'tool', 'gear',
  'load', 'fuel', 'heat', 'cool', 'burn', 'glow', 'flow', 'wave',
  'beam', 'bolt', 'shot', 'drop', 'grip', 'hold', 'pull', 'push',
  'turn', 'move', 'jump', 'fall', 'rise', 'spin', 'flip', 'roll',
  'slide', 'drift', 'crash', 'break', 'crack', 'split', 'slash',
  'punch', 'kick', 'bite', 'claw', 'blade', 'point', 'sharp', 'blunt',
  'heavy', 'light', 'quick', 'slow', 'fast', 'still', 'quiet', 'clear',
  'rough', 'smooth', 'plain', 'round', 'blank', 'clean', 'fresh',
  'human', 'guard', 'scout', 'pilot', 'chief', 'agent', 'judge',
  'maker', 'smith', 'craft', 'trade', 'price', 'value', 'stock',
  'share', 'asset', 'claim', 'right', 'force', 'power', 'drive',
  'shift', 'phase', 'surge', 'spike', 'burst', 'flash', 'spark',
  'flame', 'frost', 'storm', 'flood', 'quake', 'shock', 'blast',
  'noise', 'sound', 'voice', 'drone', 'motor', 'wheel', 'frame',
  'shell', 'plate', 'sheet', 'strip', 'patch', 'layer', 'fiber',
  'alloy', 'resin', 'oxide', 'vapor', 'fluid', 'solid', 'dense',
  'toxic', 'alert', 'alarm', 'watch', 'trace', 'track', 'sweep',
  'probe', 'relay', 'array', 'optic', 'laser', 'radar', 'sonar',
  'ether', 'class', 'grade', 'level', 'range', 'scale', 'index',
  'group', 'crowd', 'tribe', 'guild', 'order', 'union', 'pact',
  'truce', 'peace', 'mercy', 'faith', 'honor', 'pride', 'shame',
  'angel', 'ghost', 'dream', 'sleep', 'death', 'birth', 'child',
  'youth', 'elder', 'woman', 'thing', 'stuff', 'place', 'space',
  'world', 'earth', 'above', 'below', 'after', 'first', 'final',
  'total', 'count', 'whole', 'piece', 'chunk', 'parts', 'about',
  'along', 'since', 'until', 'while', 'might', 'could', 'would',
  'should', 'shall', 'other', 'every', 'these', 'those', 'there',
  'where', 'which', 'being', 'state', 'their', 'three', 'seven',
  'eight', 'large', 'small', 'great', 'major', 'minor', 'upper',
  'lower', 'inner', 'outer', 'front', 'rear', 'cross', 'multi',
  'micro', 'macro', 'ultra', 'super', 'extra', 'proto', 'semi',
  // Some more specific words that might be entity-ish but are too generic
  'filter', 'system', 'harbor', 'market', 'street', 'paper',
  'copper', 'chrome', 'silver', 'amber', 'coral', 'ivory',
  'carbon', 'plasma', 'neural', 'cyber', 'synth', 'nano',
  'atlas', 'basic', 'cargo', 'delta', 'gamma', 'alpha', 'beta',
  'omega', 'sigma', 'theta', 'proxy', 'depot', 'forge', 'haven',
  'oasis', 'vault', 'nexus', 'crest', 'crown', 'spire', 'shard',
  'prism', 'helix', 'venom', 'toxin', 'serum', 'vigor', 'boost',
  'titan', 'atlas', 'hydra', 'omega', 'viper', 'cobra', 'raven',
  'crane', 'eagle', 'tiger', 'frost', 'ember', 'solar', 'lunar',
  'tidal', 'storm', 'saint', 'rebel', 'exile', 'omega', 'cabal',
  'coven', 'triad', 'mafia', 'cartel', 'posse', 'horde', 'swarm',
  'brood', 'flock', 'batch', 'model', 'build', 'setup', 'input',
  'output', 'patch', 'debug', 'error', 'fault', 'glitch', 'crash',
  'vapor', 'hazard', 'breach', 'panic', 'siege', 'brawl', 'clash',
  'feint', 'blitz', 'joust', 'duel', 'rally', 'march', 'quest',
  'mimic', 'decoy', 'proxy', 'ghost', 'shade', 'wraith', 'nomad'
]);

const MIN_NAME_LENGTH = 4;

// ---- Step 1: Build the entity index ----
console.log('Building entity index...');

// Map: canonical name -> { dir, file, id }
// Map: lowercase name -> canonical name
const entityIndex = new Map(); // canonical -> metadata
const nameLookup = new Map();  // lowercase -> canonical

// Also track all entities for processing
const allEntities = []; // { dir, file, filePath, data }

let filesRead = 0;
for (const subdir of SUBDIRS) {
  const dirPath = path.join(dataDir, subdir);
  const files = fs.readdirSync(dirPath).filter(f => f.endsWith('.json'));

  for (const file of files) {
    const filePath = path.join(dirPath, file);
    try {
      const raw = fs.readFileSync(filePath, 'utf8');
      const data = JSON.parse(raw);

      const entityName = data.name || data.title;
      if (!entityName) continue;

      allEntities.push({ dir: subdir, file, filePath, data, raw });

      // Register the main name
      const lcName = entityName.toLowerCase();
      if (entityName.length >= MIN_NAME_LENGTH && !SKIP_WORDS.has(lcName)) {
        entityIndex.set(entityName, { dir: subdir, file, id: data.id });
        nameLookup.set(lcName, entityName);
      }

      // Also register aliases if present
      if (Array.isArray(data.aliases)) {
        for (const alias of data.aliases) {
          if (alias && alias.length >= MIN_NAME_LENGTH) {
            const lcAlias = alias.toLowerCase();
            if (!SKIP_WORDS.has(lcAlias) && !nameLookup.has(lcAlias)) {
              nameLookup.set(lcAlias, entityName); // alias resolves to canonical name
            }
          }
        }
      }
      // Register common_names for corponations
      if (Array.isArray(data.common_names)) {
        for (let cn of data.common_names) {
          // Strip quotes
          cn = cn.replace(/^["']|["']$/g, '').replace(/\s*\(.*\)$/, '').trim();
          if (cn && cn.length >= MIN_NAME_LENGTH) {
            const lcCn = cn.toLowerCase();
            if (!SKIP_WORDS.has(lcCn) && !nameLookup.has(lcCn)) {
              nameLookup.set(lcCn, entityName);
            }
          }
        }
      }

      filesRead++;
    } catch (e) {
      console.error(`Error reading ${filePath}: ${e.message}`);
    }
  }
}

console.log(`Read ${filesRead} entity files.`);
console.log(`Entity index: ${entityIndex.size} primary names, ${nameLookup.size} total lookup entries.`);

// ---- Step 2: Build optimized search structures ----
// We need to find entity names in text. Names can be multi-word.
// Strategy: build a sorted list of names by length (longest first) to prefer longer matches.

// Get all unique lookup names sorted by length descending
const allNames = [...nameLookup.entries()]
  .sort((a, b) => b[0].length - a[0].length);

console.log(`Total searchable names: ${allNames.length}`);

// For efficiency, precompile regex patterns for each name
// We escape regex special chars and use word boundary matching
function escapeRegex(s) {
  return s.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
}

// Build a function that finds all entity mentions in a text
function findMentions(text, selfName) {
  if (!text || typeof text !== 'string') return new Set();

  const textLower = text.toLowerCase();
  const found = new Set();
  const selfNameLower = selfName ? selfName.toLowerCase() : '';

  for (const [lcName, canonical] of allNames) {
    // Skip self
    if (canonical.toLowerCase() === selfNameLower) continue;

    // Quick substring check first (fast filter)
    if (!textLower.includes(lcName)) continue;

    // Use regex for word boundary check
    // For names that start/end with word characters, use word boundaries
    const escaped = escapeRegex(lcName);
    let pattern;
    try {
      // Use word boundaries when the name starts/ends with word chars
      const prefix = /^\w/.test(lcName) ? '\\b' : '';
      const suffix = /\w$/.test(lcName) ? '\\b' : '';
      pattern = new RegExp(prefix + escaped + suffix, 'i');
    } catch (e) {
      continue;
    }

    if (pattern.test(text)) {
      found.add(canonical);
    }
  }

  return found;
}

// ---- Step 3: Extract text fields from an entity for scanning ----
function getTextFields(data) {
  const texts = [];

  // Direct text fields
  const textKeys = [
    'description', 'body', 'full_text', 'narrative_function',
    'cultural_context', 'tactical_use', 'daily_life', 'affiliation',
    'location', 'founding_story', 'key_detail', 'relationship_to_big_20',
    'security_force', 'role', 'augmentations',
    'content', 'text', 'lore', 'history', 'notes', 'effect',
    'mechanism', 'side_effects', 'flavor_text', 'summary',
    'backstory', 'quote', 'context', 'gameplay_effect',
    'specifications', 'origin', 'function', 'social_impact',
    'street_reputation', 'narration_voice'
  ];

  for (const key of textKeys) {
    if (data[key] && typeof data[key] === 'string') {
      texts.push(data[key]);
    }
  }

  // Nested object text fields
  if (data.psychology && typeof data.psychology === 'object') {
    if (data.psychology.secret) texts.push(data.psychology.secret);
    for (const arr of ['core_fears', 'core_desires', 'coping_mechanisms', 'blind_spots']) {
      if (Array.isArray(data.psychology[arr])) {
        texts.push(...data.psychology[arr].filter(x => typeof x === 'string'));
      }
    }
  }

  if (data.speech_patterns && typeof data.speech_patterns === 'object') {
    for (const key of Object.keys(data.speech_patterns)) {
      const val = data.speech_patterns[key];
      if (typeof val === 'string') texts.push(val);
      if (Array.isArray(val)) texts.push(...val.filter(x => typeof x === 'string'));
    }
  }

  if (data.behavioral && typeof data.behavioral === 'object') {
    for (const key of Object.keys(data.behavioral)) {
      const val = data.behavioral[key];
      if (typeof val === 'string') texts.push(val);
      if (Array.isArray(val)) texts.push(...val.filter(x => typeof x === 'string'));
      if (val && typeof val === 'object' && !Array.isArray(val)) {
        for (const v of Object.values(val)) {
          if (typeof v === 'string') texts.push(v);
        }
      }
    }
  }

  // Array text fields
  const arrayKeys = ['story_hooks', 'known_users', 'carried_weapons', 'registered_firearms',
    'key_products', 'notable_products', 'key_figures', 'subsidiaries_list',
    'base_technologies', 'components'];
  for (const key of arrayKeys) {
    if (Array.isArray(data[key])) {
      texts.push(...data[key].filter(x => typeof x === 'string'));
    }
  }

  // Physical description
  if (data.physical_description && typeof data.physical_description === 'object') {
    for (const val of Object.values(data.physical_description)) {
      if (typeof val === 'string') texts.push(val);
      if (Array.isArray(val)) texts.push(...val.filter(x => typeof x === 'string'));
    }
  }

  // Operating territory
  if (data.operating_territory && typeof data.operating_territory === 'object') {
    for (const val of Object.values(data.operating_territory)) {
      if (typeof val === 'string') texts.push(val);
      if (Array.isArray(val)) texts.push(...val.filter(x => typeof x === 'string'));
    }
  }

  // Belongings
  if (data.belongings && typeof data.belongings === 'object') {
    for (const val of Object.values(data.belongings)) {
      if (typeof val === 'string' && val) texts.push(val);
      if (Array.isArray(val)) texts.push(...val.filter(x => typeof x === 'string'));
    }
  }

  return texts.join('\n');
}

// ---- Step 4: Process all entities ----
console.log('\nProcessing entities...');

let entitiesWithEmptyRelated = 0;
let entitiesUpdated = 0;
let totalRefsAdded = 0;
let entitiesAlreadyPopulated = 0;

const startTime = Date.now();
let processed = 0;

for (const entity of allEntities) {
  const { data, filePath, raw } = entity;
  const entityName = data.name || data.title;

  // Check if related_entities needs work
  const existingRelated = Array.isArray(data.related_entities) ? data.related_entities : [];
  const hadEmpty = existingRelated.length === 0;

  if (hadEmpty) {
    entitiesWithEmptyRelated++;
  } else {
    entitiesAlreadyPopulated++;
  }

  // Extract all text and find mentions
  const fullText = getTextFields(data);
  const mentions = findMentions(fullText, entityName);

  // Remove already-existing entries (case-insensitive comparison)
  const existingLower = new Set(existingRelated.map(e => e.toLowerCase()));
  const newRefs = [...mentions].filter(m => !existingLower.has(m.toLowerCase()));

  if (newRefs.length > 0) {
    // Merge: keep existing + add new
    const merged = [...existingRelated, ...newRefs.sort()];
    data.related_entities = merged;

    if (!DRY_RUN) {
      // Write back preserving formatting
      const output = JSON.stringify(data, null, 2);
      fs.writeFileSync(filePath, output, 'utf8');
    }

    entitiesUpdated++;
    totalRefsAdded += newRefs.length;

    if (entitiesUpdated <= 20 || entitiesUpdated % 100 === 0) {
      console.log(`  [${entitiesUpdated}] ${entityName}: +${newRefs.length} refs (${newRefs.slice(0, 5).join(', ')}${newRefs.length > 5 ? '...' : ''})`);
    }
  }

  processed++;
  if (processed % 1000 === 0) {
    console.log(`  ... processed ${processed}/${allEntities.length} entities`);
  }
}

const elapsed = ((Date.now() - startTime) / 1000).toFixed(1);

console.log('\n========================================');
console.log('CROSS-REFERENCE COMPLETE');
console.log('========================================');
console.log(`Mode: ${DRY_RUN ? 'DRY RUN (no files written)' : 'LIVE (files updated)'}`);
console.log(`Total entities scanned: ${allEntities.length}`);
console.log(`Entities with empty related_entities (before): ${entitiesWithEmptyRelated}`);
console.log(`Entities already populated: ${entitiesAlreadyPopulated}`);
console.log(`Entities updated: ${entitiesUpdated}`);
console.log(`Total new references added: ${totalRefsAdded}`);
console.log(`Time: ${elapsed}s`);
console.log('========================================');
