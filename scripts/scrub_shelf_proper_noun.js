#!/usr/bin/env node
// scrub_shelf_proper_noun.js
// Final pass: cleans up remaining proper-noun "The Shelf" references in
// people/, entertainment/, apparel/, and remaining directories.
// Targeted patterns only — avoids common-noun "shelf" (storage, bracket, etc.)

import fs from 'fs';
import path from 'path';

const DATA_ROOT = 'engine/data';
const SKIP_DIRS = new Set(['graph']);

// Only match Shelf as a proper noun (capitalized or in specific phrases)
const replacements = [
  // "The Shelf" / "the Shelf" proper noun → "the Gray Zone"
  [/\bThe Shelf\b/g, 'the Gray Zone'],
  [/\bthe [Ss]helf(?= [Cc]ommunity| [Cc]ommunities| [Cc]ompact| [Dd]ispensary| [Dd]ispensaries| [Gg]enerate| [Ww]as| [Hh]as| [Ii]s| [Aa]re| [Ww]here| [Tt]hat| [Ww]hose| [Oo]f)\b/g, 'the Gray Zone'],
  // "upper Shelf" → "corponation territory"
  [/\bupper [Ss]helf\b/g, 'corponation territory'],
  // "in The Shelf" / "in the Shelf"
  [/\bin [Tt]he [Ss]helf\b/g, 'in the Gray Zone'],
  // "Shelf communities" / "Shelf community"
  [/\b[Ss]helf [Cc]ommunities\b/g, 'Gray Zone communities'],
  [/\b[Ss]helf [Cc]ommunity\b/g, 'Gray Zone community'],
  // "Shelf Compact" (named org)
  [/\b[Ss]helf [Cc]ompact\b/g, 'Gray Zone Compact'],
  // "Shelf dispensaries" / "Shelf dispensary"
  [/\b[Ss]helf [Dd]ispensari/g, 'Gray Zone dispensari'],
  [/\b[Ss]helf [Dd]ispensary\b/g, 'Gray Zone dispensary'],
  // "this shelf" when referring to a district (not a physical shelf)
  // Only when preceded by "on" in context of district
  [/\bon this [Ss]helf alone\b/g, 'in this district alone'],
  [/\bon this [Ss]helf\b/g, 'in this district'],
  // "Shelf" as an adjective modifier for places/groups
  [/\b[Ss]helf(-| )(based|adjacent|adjacent|adjacent|side)\b/g, 'Gray Zone$1$2'],
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

const files = walk(DATA_ROOT);

for (const filepath of files) {
  try {
    const raw = fs.readFileSync(filepath, 'utf8');
    let modified = raw;

    for (const [pattern, replacement] of replacements) {
      modified = modified.replace(pattern, replacement);
    }

    if (modified !== raw) {
      fs.writeFileSync(filepath, modified, 'utf8');
      updated++;
    }
  } catch (e) {
    errors++;
  }
}

console.log(`Done. Scanned: ${files.length}, Updated: ${updated}, Errors: ${errors}`);
