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

// Directories whose entities should NOT be used as search terms
// (but they still GET cross-referenced if other entities mention them)
// Archetypes are role descriptors ("Analyst", "Fixer", "Enforcer") - not entity references
const SKIP_AS_SEARCH_TERMS = new Set(['archetypes']);

// Single-word names from these directories are too generic to match reliably.
// Only multi-word names from these dirs will be used as search terms.
const SINGLE_WORD_SKIP_DIRS = new Set([
  'materials',      // "Steel", "Copper", "Leather" etc. are common in descriptions
  'synthetics',     // "Mother", "Needle", "Compass", "Witness" etc.
  'entertainment',  // "Stratum", "Consensus", etc.
  'vocabulary',     // vocabulary words appear everywhere in text
  'places',         // "Charcoal", "Gravel" etc. - single word place names
  'quotes'          // quotes are text, not entity references
]);

// Specific single-word entity names that ARE distinctive enough to match
// (override the SINGLE_WORD_SKIP_DIRS rule for these)
const ALLOWED_SINGLE_WORDS = new Set([
  // Well-known places with distinctive names
  'glmz', 'meridian', 'irkalla',
  // Key corponation short names
  'tessera', 'arcturus', 'ringo',
  // Distinctive character names
  'vandal', 'razorback', 'tenderloin', 'switchboard', 'axiom-prime',
  // Distinctive pharmaceutical names (not English words)
  'adrenyx', 'aurocet', 'celerix', 'duravex', 'korrath', 'omnipex',
  'oxytrel', 'praevex', 'serafin', 'somalex', 'tethrin', 'sustrel',
  'velorin', 'tharcon', 'voraxin', 'kairos', 'narrowcast', 'nullform',
  'blessura', 'calydrin', 'dravecor', 'focadrin', 'kethavar', 'nullthar',
  'phaedrin', 'strathex', 'vantabloc', 'ferrocaine', 'nullocaine',
  'strelazine', 'vorrathine', 'amygdacet', 'chromasin', 'drexaline',
  'hexadrine', 'marathine', 'prismatol', 'synaptrel', 'vellichor',
  'amytal-x',
]);

// Common English words to skip even if multi-word entities contain them individually
// These would cause false positives if they happen to be registered as entity names
// Comprehensive skip list: any single-word name (including aliases) that matches
// a common English word will be skipped to avoid false positives.
// Multi-word names containing these words are fine (e.g. "The Gradient Compact" is OK).
const SKIP_WORDS = new Set([
  // Basic English
  'the', 'edge', 'signal', 'code', 'wire', 'zero',
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
  'silt', 'clay', 'zinc', 'wool', 'teak', 'lead', 'alon',
  'hdpe', 'ptfe', 'voss', 'host', 'numb', 'muse', 'fury', 'liar',
  'sine', 'loom', 'whim',
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
  'filter', 'system', 'harbor', 'market', 'street', 'paper',
  'copper', 'chrome', 'silver', 'amber', 'coral', 'ivory',
  'carbon', 'plasma', 'neural', 'cyber', 'synth', 'nano',
  'basic', 'cargo', 'delta', 'gamma', 'alpha', 'beta',
  'omega', 'sigma', 'theta', 'proxy', 'depot', 'forge', 'haven',
  'oasis', 'vault', 'nexus', 'crest', 'crown', 'spire', 'shard',
  'prism', 'helix', 'venom', 'toxin', 'serum', 'vigor', 'boost',
  'titan', 'atlas', 'hydra', 'viper', 'cobra', 'raven',
  'crane', 'eagle', 'tiger', 'ember', 'solar', 'lunar',
  'tidal', 'saint', 'rebel', 'exile', 'cabal',
  'coven', 'triad', 'mafia', 'cartel', 'posse', 'horde', 'swarm',
  'brood', 'flock', 'batch', 'model', 'build', 'setup', 'input',
  'output', 'debug', 'error', 'fault', 'glitch',
  'hazard', 'breach', 'panic', 'siege', 'brawl', 'clash',
  'feint', 'blitz', 'joust', 'duel', 'rally', 'march', 'quest',
  'mimic', 'decoy', 'shade', 'wraith', 'nomad',
  // Common English that are entity names or aliases
  'corponation', 'handmade', 'obsolete', 'darkroom',
  'independent', 'witness', 'consensus', 'interval',
  'circuit', 'origin', 'canopy', 'indigo', 'patience',
  'scaffold', 'gossamer', 'solstice', 'threshold', 'parallax',
  'elevation', 'provisions', 'stampede', 'undertow', 'switchback',
  'petrichor', 'boneworks', 'frequency', 'precipitate',
  'porcelain', 'phosphene', 'delivered', 'optionality',
  // Common words found as aliases in the data
  'personal', 'standard', 'classic', 'compact', 'direct',
  'custom', 'prime', 'single', 'double', 'triple',
  'warm', 'bright', 'wrong', 'empty', 'closed',
  'badge', 'stamp', 'knock', 'catch', 'switch', 'pocket',
  'fuse', 'wren', 'iris', 'lark', 'reed', 'penn', 'ruby',
  'jade', 'pearl', 'onyx', 'opal', 'olive', 'sage',
  'finn', 'mack', 'ward', 'vale', 'dale', 'glen', 'lane',
  'ford', 'mill', 'dock', 'pier', 'port', 'shed', 'barn',
  'farm', 'camp', 'lodge', 'manor', 'ranch', 'villa', 'suite',
  'buzz', 'nose', 'mole', 'tick', 'worm', 'nana', 'bayo',
  'beke', 'daze', 'addy', 'disa', 'satu', 'whet', 'mara',
  // Common materials
  'steel', 'brass', 'copper', 'bronze', 'nickel', 'cobalt',
  'leather', 'cotton', 'nylon', 'marble', 'granite', 'concrete',
  'aluminum', 'titanium', 'tungsten', 'platinum', 'chromium',
  'silicone', 'graphene', 'obsidian', 'limestone', 'sandstone',
  'mahogany', 'rosewood', 'fiberglass', 'epoxy', 'kevlar',
  'bamboo', 'walnut', 'maple', 'birch', 'cedar', 'ebony',
  'aerogel', 'basalt', 'slate', 'glass', 'gold', 'iron', 'silk',
  'silver', 'lead', 'zinc', 'wool', 'teak',
]);

const MIN_NAME_LENGTH = 4;

// ---- Step 1: Build the entity index ----
console.log('Building entity index...');

const entityIndex = new Map();
const nameLookup = new Map();
const allEntities = [];

function shouldRegisterName(name, subdir) {
  if (!name || name.length < MIN_NAME_LENGTH) return false;

  const lcName = name.toLowerCase();

  // Always skip common words
  if (SKIP_WORDS.has(lcName)) return false;

  // Skip entire directories from being search terms
  if (SKIP_AS_SEARCH_TERMS.has(subdir)) return false;

  // Check if it's a single word (no spaces)
  const isSingleWord = !name.includes(' ') && !name.includes('-');
  const isSingleOrHyphenated = !name.includes(' ');

  // For single-word names from certain directories, only allow if explicitly allowed
  if (isSingleOrHyphenated && SINGLE_WORD_SKIP_DIRS.has(subdir)) {
    if (!ALLOWED_SINGLE_WORDS.has(lcName)) return false;
  }

  return true;
}

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
      if (shouldRegisterName(entityName, subdir)) {
        entityIndex.set(entityName, { dir: subdir, file, id: data.id });
        nameLookup.set(entityName.toLowerCase(), entityName);
      }

      // Also register aliases if present (but apply same filtering)
      if (Array.isArray(data.aliases)) {
        for (const alias of data.aliases) {
          if (shouldRegisterName(alias, subdir)) {
            const lcAlias = alias.toLowerCase();
            if (!nameLookup.has(lcAlias)) {
              nameLookup.set(lcAlias, entityName);
            }
          }
        }
      }

      // Register common_names for corponations
      if (Array.isArray(data.common_names)) {
        for (let cn of data.common_names) {
          cn = cn.replace(/^["']|["']$/g, '').replace(/\s*\(.*\)$/, '').trim();
          if (shouldRegisterName(cn, subdir)) {
            const lcCn = cn.toLowerCase();
            if (!nameLookup.has(lcCn)) {
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
const allNames = [...nameLookup.entries()]
  .sort((a, b) => b[0].length - a[0].length);

console.log(`Total searchable names: ${allNames.length}`);

// Log some stats about what we're searching for
const multiWord = allNames.filter(([n]) => n.includes(' ')).length;
const singleWord = allNames.length - multiWord;
console.log(`  Multi-word names: ${multiWord}, Single-word names: ${singleWord}`);

function escapeRegex(s) {
  return s.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
}

function findMentions(text, selfName) {
  if (!text || typeof text !== 'string') return new Set();

  const textLower = text.toLowerCase();
  const found = new Set();
  const selfNameLower = selfName ? selfName.toLowerCase() : '';

  for (const [lcName, canonical] of allNames) {
    if (canonical.toLowerCase() === selfNameLower) continue;
    if (!textLower.includes(lcName)) continue;

    const escaped = escapeRegex(lcName);
    let pattern;
    try {
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

  const arrayKeys = ['story_hooks', 'known_users', 'carried_weapons', 'registered_firearms',
    'key_products', 'notable_products', 'key_figures', 'subsidiaries_list',
    'base_technologies', 'components'];
  for (const key of arrayKeys) {
    if (Array.isArray(data[key])) {
      texts.push(...data[key].filter(x => typeof x === 'string'));
    }
  }

  if (data.physical_description && typeof data.physical_description === 'object') {
    for (const val of Object.values(data.physical_description)) {
      if (typeof val === 'string') texts.push(val);
      if (Array.isArray(val)) texts.push(...val.filter(x => typeof x === 'string'));
    }
  }

  if (data.operating_territory && typeof data.operating_territory === 'object') {
    for (const val of Object.values(data.operating_territory)) {
      if (typeof val === 'string') texts.push(val);
      if (Array.isArray(val)) texts.push(...val.filter(x => typeof x === 'string'));
    }
  }

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
let entitiesSkippedNoText = 0;

const startTime = Date.now();
let processed = 0;

// Track the top referenced entities
const refCounts = new Map();

for (const entity of allEntities) {
  const { data, filePath, raw } = entity;
  const entityName = data.name || data.title;

  const existingRelated = Array.isArray(data.related_entities) ? data.related_entities : [];
  const hadEmpty = existingRelated.length === 0;

  if (hadEmpty) {
    entitiesWithEmptyRelated++;
  } else {
    entitiesAlreadyPopulated++;
  }

  const fullText = getTextFields(data);
  if (!fullText || fullText.trim().length === 0) {
    entitiesSkippedNoText++;
    processed++;
    continue;
  }

  const mentions = findMentions(fullText, entityName);

  const existingLower = new Set(existingRelated.map(e => e.toLowerCase()));
  const newRefs = [...mentions].filter(m => !existingLower.has(m.toLowerCase()));

  if (newRefs.length > 0) {
    const merged = [...existingRelated, ...newRefs.sort()];
    data.related_entities = merged;

    if (!DRY_RUN) {
      const output = JSON.stringify(data, null, 2);
      fs.writeFileSync(filePath, output, 'utf8');
    }

    entitiesUpdated++;
    totalRefsAdded += newRefs.length;

    // Track reference counts
    for (const ref of newRefs) {
      refCounts.set(ref, (refCounts.get(ref) || 0) + 1);
    }

    if (entitiesUpdated <= 10 || entitiesUpdated % 200 === 0) {
      console.log(`  [${entitiesUpdated}] ${entityName}: +${newRefs.length} refs (${newRefs.slice(0, 4).join(', ')}${newRefs.length > 4 ? '...' : ''})`);
    }
  }

  processed++;
  if (processed % 1000 === 0) {
    const elapsed = ((Date.now() - startTime) / 1000).toFixed(0);
    console.log(`  ... processed ${processed}/${allEntities.length} entities (${elapsed}s elapsed, ${entitiesUpdated} updated so far)`);
  }
}

const elapsed = ((Date.now() - startTime) / 1000).toFixed(1);

// Top referenced entities
const topRefs = [...refCounts.entries()].sort((a, b) => b[1] - a[1]).slice(0, 25);

console.log('\n========================================');
console.log('CROSS-REFERENCE COMPLETE');
console.log('========================================');
console.log(`Mode: ${DRY_RUN ? 'DRY RUN (no files written)' : 'LIVE (files updated)'}`);
console.log(`Total entities scanned: ${allEntities.length}`);
console.log(`Entities with empty related_entities (before): ${entitiesWithEmptyRelated}`);
console.log(`Entities already populated: ${entitiesAlreadyPopulated}`);
console.log(`Entities skipped (no text): ${entitiesSkippedNoText}`);
console.log(`Entities updated: ${entitiesUpdated}`);
console.log(`Total new references added: ${totalRefsAdded}`);
console.log(`Average refs per updated entity: ${(totalRefsAdded / entitiesUpdated).toFixed(1)}`);
console.log(`Time: ${elapsed}s`);
console.log('\nTop 25 most referenced entities:');
for (const [name, count] of topRefs) {
  console.log(`  ${count.toString().padStart(4)} x ${name}`);
}
console.log('========================================');
