#!/usr/bin/env node
// scrub_sprawl_all.js
// Replaces "the sprawl" / "The Sprawl" with "the Gray Zone" across ALL engine/data/ JSON files.
// Also catches any remaining "the Shelf" / "Shelf District" proper-noun usage.
// Safe: does NOT touch the lowercase word "shelf" as a common noun (e.g., "on the shelf").

import fs from 'fs';
import path from 'path';

const DATA_ROOT = 'engine/data';
const SKIP_DIRS = new Set(['graph']); // world_graph.json regenerated separately

const replacements = [
  // "the sprawl" / "The Sprawl" → "the Gray Zone"
  [/\bthe [Ss]prawl\b/g, 'the Gray Zone'],
  // Remaining proper-noun Shelf references from first scrub
  [/\bthe [Ss]helf\b/g, 'the Gray Zone'],
  [/\bShelf [Dd]istrict\b/g, 'Gray Zone'],
  [/\blower [Ss]helf\b/g, 'the Gray Zone'],
  [/\bdeep [Ss]helf\b/g, 'the Gray Zone'],
  [/\b[Ss]helf [Bb]lock\b/g, 'Gray Zone block'],
  [/\b[Ss]helf [Nn]eighborhood\b/g, 'Gray Zone neighborhood'],
  [/\b[Ss]helf [Cc]ommunity\b/g, 'Gray Zone community'],
  [/\b[Ss]helf [Cc]hildren\b/g, 'Gray Zone children'],
  [/\b[Ss]helf [Cc]hild\b/g, 'Gray Zone child'],
  [/\b[Ss]helf [Rr]esident\b/g, 'Gray Zone resident'],
  [/\b[Ss]helf [Mm]arket\b/g, 'Gray Zone market'],
  [/\b[Ss]helf [Cc]ooking\b/g, 'Gray Zone cooking'],
  [/\b[Ss]helf [Ss]urvival\b/g, 'Gray Zone survival'],
  [/\b[Ss]helf [Ll]ife\b(?!span)/g, 'Gray Zone life'], // "shelf life" as in food = leave alone
  // Tags: "shelf" tag → "gray-zone"
];

function walk(dir) {
  const entries = fs.readdirSync(dir, { withFileTypes: true });
  const files = [];
  for (const e of entries) {
    if (e.isDirectory()) {
      if (!SKIP_DIRS.has(e.name)) files.push(...walk(path.join(dir, e.name)));
    } else if (e.name.endsWith('.json')) {
      files.push(path.join(dir, e.name));
    }
  }
  return files;
}

let updated = 0;
let errors = 0;
let scanned = 0;

const files = walk(DATA_ROOT);
console.log(`Scanning ${files.length} files...`);

for (const filepath of files) {
  scanned++;
  try {
    const raw = fs.readFileSync(filepath, 'utf8');
    let modified = raw;

    for (const [pattern, replacement] of replacements) {
      modified = modified.replace(pattern, replacement);
    }

    // Fix tags array: remove "shelf" tag, add "gray-zone" if it had shelf
    if (modified.includes('"shelf"')) {
      const parsed = JSON.parse(modified);
      if (Array.isArray(parsed.tags) && parsed.tags.includes('shelf')) {
        parsed.tags = parsed.tags.filter(t => t !== 'shelf');
        if (!parsed.tags.includes('gray-zone')) parsed.tags.push('gray-zone');
        modified = JSON.stringify(parsed, null, 2);
      }
    }

    // Fix related_entities: "The Shelf" → "Gray Zone"
    if (modified.includes('"The Shelf"') || modified.includes('"Shelf District"')) {
      const parsed = JSON.parse(modified);
      if (Array.isArray(parsed.related_entities)) {
        parsed.related_entities = parsed.related_entities
          .map(e => (e === 'The Shelf' || e === 'Shelf District') ? 'Gray Zone' : e)
          .filter((e, i, arr) => arr.indexOf(e) === i);
        modified = JSON.stringify(parsed, null, 2);
      }
    }

    if (modified !== raw) {
      fs.writeFileSync(filepath, modified, 'utf8');
      updated++;
    }
  } catch (e) {
    errors++;
    // Silently skip parse errors (binary files, etc.)
  }
}

console.log(`Done. Scanned: ${scanned}, Updated: ${updated}, Errors: ${errors}`);
