/**
 * Cross-Reference Pass
 *
 * Reads all JSON files across engine/data/ repos, builds a name index,
 * then populates related_entities arrays with cross-references to entities
 * mentioned in text fields.
 */

const fs = require('fs');
const path = require('path');

const DATA_ROOT = path.resolve(__dirname, '..', 'engine', 'data');

// Repos to index entity names from
const INDEX_REPOS = [
  'characters', 'corponations', 'factions', 'places', 'technology',
  'weaponry', 'cyberware', 'equipment', 'automata', 'synthetics',
  'entertainment', 'subsidiaries', 'materials'
];

// Repos to scan and update with cross-references
const SCAN_REPOS = ['documents', 'characters', 'factions', 'places'];

// Minimum name length to avoid false positives
const MIN_NAME_LENGTH = 4;

// Common English words that appear as aliases but cause massive false positives.
// These are too generic to be useful as cross-reference triggers.
const STOPWORD_ALIASES = new Set([
  'personal', 'standard', 'guardian', 'hammer', 'angel', 'pulse', 'storm',
  'flash', 'hunter', 'frost', 'iron', 'steel', 'chrome', 'copper', 'silver',
  'thread', 'signal', 'torch', 'needle', 'drift', 'anchor', 'shield', 'core',
  'wire', 'gate', 'edge', 'link', 'node', 'spark', 'forge', 'haven', 'tower',
  'dock', 'ward', 'cell', 'ring', 'dome', 'arch', 'veil', 'grid', 'shell',
  'frame', 'point', 'line', 'mark', 'lock', 'bolt', 'chain', 'band', 'wave',
  'beam', 'lens', 'coil', 'valve', 'pipe', 'tube', 'disc', 'coin', 'slab',
  'chip', 'tile', 'mesh', 'film', 'foam', 'dust', 'sand', 'clay', 'salt',
  'acid', 'fuel', 'rust', 'moss', 'vine', 'bark', 'root', 'leaf', 'seed',
  'bone', 'skin', 'horn', 'claw', 'fang', 'wing', 'tail', 'beak', 'nest',
  'hive', 'pack', 'herd', 'clan', 'cult', 'crown', 'realm', 'code', 'data',
  'byte', 'file', 'port', 'host', 'site', 'page', 'text', 'font', 'icon',
  'flag', 'sign', 'seal', 'crest', 'badge', 'patch', 'stamp', 'print',
  'label', 'brand', 'null', 'static', 'brick', 'zero', 'silence', 'silk',
  'grease', 'seven', 'bloom', 'shade', 'trace', 'grain', 'haze', 'surge',
  'glare', 'flare', 'blaze', 'gauge', 'clamp', 'brace', 'lever', 'basin',
  'ridge', 'creek', 'marsh', 'ledge', 'shelf', 'curve', 'slope', 'notch',
  'reach', 'depth', 'width', 'scale', 'ratio', 'cycle', 'phase', 'stage',
  'level', 'grade', 'class', 'order', 'group', 'block', 'strip', 'panel',
  'board', 'sheet', 'layer', 'cover', 'latch', 'hinge', 'mount', 'stand',
  'wrath', 'mercy', 'grace', 'pride', 'shame', 'spite', 'doubt', 'grief',
  'dread', 'vigor', 'nerve', 'focus', 'drive', 'force', 'power', 'might',
  'valor', 'vigor', 'honor', 'truth', 'peace', 'chaos', 'light', 'ivory',
]);

function readJsonFiles(dirPath) {
  if (!fs.existsSync(dirPath)) return [];
  const files = fs.readdirSync(dirPath).filter(f => f.endsWith('.json'));
  const results = [];
  for (const file of files) {
    const filePath = path.join(dirPath, file);
    try {
      const data = JSON.parse(fs.readFileSync(filePath, 'utf8'));
      results.push({ filePath, data, fileName: file });
    } catch (e) {
      // Skip malformed JSON
    }
  }
  return results;
}

function extractSearchableText(data, repo) {
  const parts = [];

  // description field (characters, factions, places, etc.)
  if (data.description && typeof data.description === 'string') {
    parts.push(data.description);
  }

  // body field (documents)
  if (data.body && typeof data.body === 'string') {
    parts.push(data.body);
  }

  // story_hooks (array of strings)
  if (Array.isArray(data.story_hooks)) {
    for (const hook of data.story_hooks) {
      if (typeof hook === 'string') parts.push(hook);
    }
  }

  // location field (characters)
  if (data.location && typeof data.location === 'string') {
    parts.push(data.location);
  }

  return parts.join('\n');
}

function main() {
  console.log('Cross-Reference Pass');
  console.log('====================\n');

  // Phase 0: Clear existing related_entities from scan repos (idempotent re-runs)
  console.log('Phase 0: Clearing existing related_entities from scan targets...');
  for (const repo of SCAN_REPOS) {
    const dirPath = path.join(DATA_ROOT, repo);
    const files = readJsonFiles(dirPath);
    let cleared = 0;
    for (const { filePath, data } of files) {
      if (Array.isArray(data.related_entities) && data.related_entities.length > 0) {
        data.related_entities = [];
        fs.writeFileSync(filePath, JSON.stringify(data, null, 2), 'utf8');
        cleared++;
      }
    }
    console.log(`  ${repo}: ${cleared} files cleared`);
  }
  console.log();

  // Phase 1: Build entity name index
  console.log('Phase 1: Building entity name index...');

  // Map: lowercased name -> { canonicalName, repo, filePath }
  const nameIndex = new Map();
  // Also track by file path so we can skip self-references
  const fileToEntity = new Map();
  let totalIndexed = 0;

  for (const repo of INDEX_REPOS) {
    const dirPath = path.join(DATA_ROOT, repo);
    const files = readJsonFiles(dirPath);
    let repoCount = 0;

    for (const { filePath, data } of files) {
      const name = data.name || data.title;
      if (!name || typeof name !== 'string') continue;
      if (name.length < MIN_NAME_LENGTH) continue;

      const key = name.toLowerCase();
      if (!nameIndex.has(key)) {
        nameIndex.set(key, { canonicalName: name, repo, filePath });
        repoCount++;
        totalIndexed++;
      }
      fileToEntity.set(filePath, name);

      // Also index aliases if present
      if (Array.isArray(data.aliases)) {
        for (const alias of data.aliases) {
          if (!alias || typeof alias !== 'string') continue;
          if (alias.length < MIN_NAME_LENGTH) continue;
          const aliasKey = alias.toLowerCase();
          // Skip generic English words that cause false positives
          if (STOPWORD_ALIASES.has(aliasKey)) continue;
          if (!nameIndex.has(aliasKey)) {
            nameIndex.set(aliasKey, { canonicalName: name, repo, filePath });
          }
        }
      }

      // Index common_names for corponations
      if (Array.isArray(data.common_names)) {
        for (const cn of data.common_names) {
          if (!cn || typeof cn !== 'string') continue;
          // Strip quotes and parenthetical notes
          const cleaned = cn.replace(/^["']|["']$/g, '').replace(/\s*\(.*?\)\s*/g, '').trim();
          if (cleaned.length < MIN_NAME_LENGTH) continue;
          const cnKey = cleaned.toLowerCase();
          if (STOPWORD_ALIASES.has(cnKey)) continue;
          if (!nameIndex.has(cnKey)) {
            nameIndex.set(cnKey, { canonicalName: name, repo, filePath });
          }
        }
      }
    }

    console.log(`  ${repo}: ${repoCount} entities indexed`);
  }

  console.log(`\nTotal indexed: ${totalIndexed} unique entities (${nameIndex.size} including aliases)\n`);

  // Phase 2: Build regex patterns for efficient matching
  // Sort names by length descending so longer names match first
  console.log('Phase 2: Building search patterns...');

  const allNames = Array.from(nameIndex.entries())
    .sort((a, b) => b[0].length - a[0].length);

  // Build batches of regex patterns (one giant regex would be too slow)
  // Use word boundary matching
  const BATCH_SIZE = 500;
  const batches = [];
  for (let i = 0; i < allNames.length; i += BATCH_SIZE) {
    const batch = allNames.slice(i, i + BATCH_SIZE);
    const pattern = batch
      .map(([name]) => name.replace(/[.*+?^${}()|[\]\\]/g, '\\$&'))
      .join('|');
    const regex = new RegExp(`\\b(${pattern})\\b`, 'gi');
    batches.push({ regex, names: batch });
  }

  console.log(`  Created ${batches.length} search batches\n`);

  // Phase 3: Scan and update files
  console.log('Phase 3: Scanning files for cross-references...');

  let totalFilesProcessed = 0;
  let totalRefsAdded = 0;
  let totalFilesUpdated = 0;
  const refCounts = new Map(); // canonical name -> count of times referenced

  for (const repo of SCAN_REPOS) {
    const dirPath = path.join(DATA_ROOT, repo);
    const files = readJsonFiles(dirPath);
    let repoRefsAdded = 0;
    let repoFilesUpdated = 0;

    for (const { filePath, data, fileName } of files) {
      totalFilesProcessed++;

      const text = extractSearchableText(data, repo);
      if (!text) continue;

      const selfName = data.name || data.title || '';
      const selfKey = selfName.toLowerCase();

      // Find all entity mentions
      const foundEntities = new Set(); // canonical names found
      const existingRefs = new Set(
        (data.related_entities || []).map(r => r.toLowerCase())
      );

      for (const { regex } of batches) {
        regex.lastIndex = 0;
        let match;
        while ((match = regex.exec(text)) !== null) {
          const matchedKey = match[1].toLowerCase();
          const entry = nameIndex.get(matchedKey);
          if (!entry) continue;

          const canonical = entry.canonicalName;
          const canonicalLower = canonical.toLowerCase();

          // Skip self-reference
          if (canonicalLower === selfKey) continue;
          // Skip if entity is from the same file
          if (entry.filePath === filePath) continue;

          // For non-document repos, only add cross-repo references
          if (repo !== 'documents' && entry.repo === repo) continue;

          if (!existingRefs.has(canonicalLower) && !foundEntities.has(canonicalLower)) {
            foundEntities.add(canonicalLower);
          }
        }
      }

      if (foundEntities.size > 0) {
        // Initialize related_entities if missing
        if (!Array.isArray(data.related_entities)) {
          data.related_entities = [];
        }

        // Add new references
        for (const lowerName of foundEntities) {
          const entry = nameIndex.get(lowerName);
          if (entry) {
            data.related_entities.push(entry.canonicalName);
            repoRefsAdded++;
            totalRefsAdded++;
            refCounts.set(entry.canonicalName, (refCounts.get(entry.canonicalName) || 0) + 1);
          }
        }

        // Write back
        fs.writeFileSync(filePath, JSON.stringify(data, null, 2), 'utf8');
        repoFilesUpdated++;
        totalFilesUpdated++;
      }

      if (totalFilesProcessed % 500 === 0) {
        process.stdout.write(`  Processed ${totalFilesProcessed} files...\r`);
      }
    }

    console.log(`  ${repo}: ${files.length} files scanned, ${repoFilesUpdated} updated, ${repoRefsAdded} refs added`);
  }

  // Phase 4: Report
  console.log('\n====================');
  console.log('Results');
  console.log('====================');
  console.log(`Total files processed: ${totalFilesProcessed}`);
  console.log(`Total files updated:   ${totalFilesUpdated}`);
  console.log(`Total cross-refs added: ${totalRefsAdded}`);

  // Top 10 most referenced
  const sorted = Array.from(refCounts.entries())
    .sort((a, b) => b[1] - a[1])
    .slice(0, 10);

  console.log('\nTop 10 most-referenced entities:');
  for (let i = 0; i < sorted.length; i++) {
    const [name, count] = sorted[i];
    const entry = nameIndex.get(name.toLowerCase());
    console.log(`  ${i + 1}. ${name} (${entry?.repo || '?'}) — ${count} references`);
  }
}

main();
