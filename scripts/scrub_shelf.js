#!/usr/bin/env node
// Scrubs "The Shelf" concept from all worldbuilding JSON files.
// Tier language (Tier 1, Tier 2, etc.) is left intact — it's social status, not geography.

const fs = require('fs');
const path = require('path');

const DATA_DIR = path.join(__dirname, '..', 'engine', 'data');

// Text replacements applied in order (order matters — more specific patterns first).
const TEXT_REPLACEMENTS = [
  // Directional compound phrases first
  [/above the [Ss]helf/g,         'inside corponation territory'],
  [/below the [Ss]helf/g,         'in the Gray Zone'],
  [/into the [Ss]helf/g,          'into the sprawl'],
  [/from the [Ss]helf/g,          'from the sprawl'],
  [/across the [Ss]helf/g,        'across the sprawl'],
  [/through the [Ss]helf/g,       'through the sprawl'],
  [/on the [Ss]helf/g,            'in the sprawl'],
  [/of the [Ss]helf/g,            'of the sprawl'],
  // Qualified Shelf references
  [/the lower [Ss]helf/g,         'the Gray Zone'],
  [/the upper [Ss]helf/g,         'the corponation quarter'],
  [/the mid[- ]?[Ss]helf/g,       'the mid-sprawl'],
  [/lower [Ss]helf/g,             'the Gray Zone'],
  [/upper [Ss]helf/g,             'the corponation quarter'],
  // Shelf District (named location in documents)
  [/[Ss]helf [Dd]istrict/g,       'the Gray Zone district'],
  // Generic "the Shelf" / "The Shelf"
  [/[Tt]he [Ss]helf/g,            'the sprawl'],
  // Bare "Shelf" used as a noun (after all compound forms already handled)
  [/\bShelf\b/g,                  'the sprawl'],
];

function scrubText(text) {
  let result = text;
  for (const [pattern, replacement] of TEXT_REPLACEMENTS) {
    result = result.replace(pattern, replacement);
  }
  return result;
}

function scrubArray(arr, valuesToRemove) {
  return arr.filter(v => !valuesToRemove.some(r => v.toLowerCase() === r.toLowerCase()));
}

function processFile(filePath) {
  let raw;
  try {
    raw = fs.readFileSync(filePath, 'utf8');
  } catch {
    return { changed: false };
  }

  // Quick bail — skip files with no shelf-related content at all
  if (!/shelf/i.test(raw)) return { changed: false };

  let data;
  try {
    data = JSON.parse(raw);
  } catch {
    return { changed: false, error: `parse error: ${filePath}` };
  }

  let changed = false;

  // Remove "shelf" from tags arrays
  if (Array.isArray(data.tags)) {
    const cleaned = scrubArray(data.tags, ['shelf']);
    if (cleaned.length !== data.tags.length) {
      data.tags = cleaned;
      changed = true;
    }
  }

  // Remove "The Shelf" from related_entities arrays
  if (Array.isArray(data.related_entities)) {
    const cleaned = scrubArray(data.related_entities, ['The Shelf', 'the shelf', 'Shelf']);
    if (cleaned.length !== data.related_entities.length) {
      data.related_entities = cleaned;
      changed = true;
    }
  }

  // Scrub text fields
  const textFields = ['body', 'description', 'full_text', 'founding_story',
                      'title', 'name', 'file_name', 'key_detail'];
  for (const field of textFields) {
    if (typeof data[field] === 'string') {
      const scrubbed = scrubText(data[field]);
      if (scrubbed !== data[field]) {
        data[field] = scrubbed;
        changed = true;
      }
    }
  }

  // Scrub nested string fields (subsidiaries, connections, etc.)
  function scrubObject(obj) {
    if (!obj || typeof obj !== 'object') return;
    for (const key of Object.keys(obj)) {
      if (typeof obj[key] === 'string' && /shelf/i.test(obj[key])) {
        const scrubbed = scrubText(obj[key]);
        if (scrubbed !== obj[key]) {
          obj[key] = scrubbed;
          changed = true;
        }
      } else if (Array.isArray(obj[key])) {
        obj[key].forEach(item => scrubObject(item));
      } else if (obj[key] && typeof obj[key] === 'object') {
        scrubObject(obj[key]);
      }
    }
  }

  // Process all remaining fields not already handled
  for (const key of Object.keys(data)) {
    if (!textFields.includes(key) && key !== 'tags' && key !== 'related_entities') {
      scrubObject(data[key]);
      if (typeof data[key] === 'string' && /shelf/i.test(data[key])) {
        const scrubbed = scrubText(data[key]);
        if (scrubbed !== data[key]) {
          data[key] = scrubbed;
          changed = true;
        }
      }
    }
  }

  if (changed) {
    fs.writeFileSync(filePath, JSON.stringify(data, null, 2), 'utf8');
  }

  return { changed };
}

function walk(dir) {
  const entries = fs.readdirSync(dir, { withFileTypes: true });
  const files = [];
  for (const entry of entries) {
    const full = path.join(dir, entry.name);
    if (entry.isDirectory()) {
      files.push(...walk(full));
    } else if (entry.name.endsWith('.json')) {
      files.push(full);
    }
  }
  return files;
}

const files = walk(DATA_DIR);
let totalChanged = 0;
let totalErrors = 0;

for (const file of files) {
  const result = processFile(file);
  if (result.changed) totalChanged++;
  if (result.error) {
    totalErrors++;
    console.error('ERROR:', result.error);
  }
}

console.log(`Done. ${files.length} files scanned, ${totalChanged} updated, ${totalErrors} errors.`);
