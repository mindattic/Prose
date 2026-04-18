// fix_entity_tags.js
// - Adds "synthetic" tag to every file in engine/data/synthetics/
// - Adds "person" tag to every file in engine/data/people/
// - Lowercases all tags wherever they appear (top-level tags[] and stats.tags[])
// - Deduplicates tags (case-insensitive) within each array

const fs = require('fs');
const path = require('path');

const DATA_ROOT = path.join(__dirname, '..', 'engine', 'data');

const jobs = [
  { dir: path.join(DATA_ROOT, 'synthetics'), categoryTag: 'synthetic' },
  { dir: path.join(DATA_ROOT, 'people'),     categoryTag: 'person'    },
];

function normalizeTagArray(arr, ensureTag) {
  if (!Array.isArray(arr)) return arr;

  // Lowercase everything
  let tags = arr.map(t => (typeof t === 'string' ? t.toLowerCase().trim() : null))
                .filter(t => t && t.length > 0);

  // Add category tag if missing
  if (ensureTag && !tags.includes(ensureTag)) {
    tags.unshift(ensureTag);
  }

  // Deduplicate (preserve first occurrence order)
  const seen = new Set();
  return tags.filter(t => {
    if (seen.has(t)) return false;
    seen.add(t);
    return true;
  });
}

function processDir({ dir, categoryTag }) {
  let files;
  try {
    files = fs.readdirSync(dir).filter(f => f.endsWith('.json'));
  } catch (e) {
    console.error(`SKIP (not found): ${dir}`);
    return;
  }

  let modified = 0;
  let skipped  = 0;

  for (const file of files) {
    const filePath = path.join(dir, file);
    let data;
    try {
      data = JSON.parse(fs.readFileSync(filePath, 'utf8'));
    } catch (e) {
      console.error(`PARSE ERROR: ${file} — ${e.message}`);
      continue;
    }

    let changed = false;

    // Top-level tags[]
    if (Array.isArray(data.tags)) {
      const fixed = normalizeTagArray(data.tags, categoryTag);
      if (JSON.stringify(fixed) !== JSON.stringify(data.tags)) {
        data.tags = fixed;
        changed = true;
      }
    } else {
      // No top-level tags — create it
      data.tags = normalizeTagArray([], categoryTag);
      changed = true;
    }

    // stats.tags[] (full-profile characters have tags nested in stats)
    if (data.stats && Array.isArray(data.stats.tags)) {
      const fixed = normalizeTagArray(data.stats.tags, null); // no category tag here
      if (JSON.stringify(fixed) !== JSON.stringify(data.stats.tags)) {
        data.stats.tags = fixed;
        changed = true;
      }
    }

    if (changed) {
      fs.writeFileSync(filePath, JSON.stringify(data, null, 2), 'utf8');
      modified++;
    } else {
      skipped++;
    }
  }

  console.log(`${path.basename(dir)}: ${modified} modified, ${skipped} already clean`);
}

for (const job of jobs) {
  processDir(job);
}

console.log('\nDone.');
