#!/usr/bin/env node
// fix_shelf_docs.js
// Fixes the 23 shelf documents: replaces "the sprawl" with "the Gray Zone",
// updates related_entities and tags, fixes titles/names.

import fs from 'fs';
import path from 'path';

const DOCS_DIR = 'engine/data/documents';

// Files with "shelf" in the filename
const shelfFiles = fs.readdirSync(DOCS_DIR).filter(f => f.includes('shelf'));

const stringReplacements = [
  // The physical-tier-era sprawl references → Gray Zone
  [/\bthe sprawl\b/g, 'the Gray Zone'],
  [/\bthe Sprawl\b/g, 'the Gray Zone'],
  // "Shelf District" as a proper noun → Gray Zone
  [/\bShelf District\b/g, 'Gray Zone'],
  [/\bshelf district\b/g, 'Gray Zone'],
  // Shelf Vernacular abbreviation
  [/\bSV\b(?= —| is| has| \()/g, 'GZV'],
  [/\bShelf Vernacular\b/g, 'Gray Zone Vernacular'],
  [/\bshelf vernacular\b/g, 'Gray Zone vernacular'],
  [/\bthe sprawl Vernacular\b/g, 'the Gray Zone Vernacular'],
  [/\bthe sprawl vernacular\b/g, 'the Gray Zone vernacular'],
  // "the sprawl children" / "the sprawl child" → "Gray Zone children"
  [/\bthe sprawl children\b/g, 'Gray Zone children'],
  [/\bthe sprawl child\b/g, 'Gray Zone child'],
  // "the sprawl register" / "the sprawl Register"
  [/\bthe sprawl register\b/gi, 'the Gray Zone register'],
  // Titles: "the sprawl: " → "Gray Zone: "
  [/^the sprawl: /i, 'Gray Zone: '],
  // "Growing Up the sprawl" → "Growing Up in the Gray Zone"
  [/Growing Up the sprawl/g, 'Growing Up in the Gray Zone'],
  // "the sprawl survival" → "Gray Zone survival"
  [/the sprawl survival/gi, 'Gray Zone survival'],
  // "the sprawl cooking" → "Gray Zone cooking"
  [/the sprawl cooking/gi, 'Gray Zone cooking'],
  // "the sprawl market" → "Gray Zone market"
  [/the sprawl market/gi, 'Gray Zone market'],
  // catch-all remaining "the sprawl" in field strings
  [/the sprawl/g, 'the Gray Zone'],
];

let updated = 0;
let errors = 0;

for (const filename of shelfFiles) {
  const filepath = path.join(DOCS_DIR, filename);
  try {
    let raw = fs.readFileSync(filepath, 'utf8');
    const original = raw;
    const doc = JSON.parse(raw);

    // Apply string replacements to the full serialized JSON
    // (catches body, title, name, description, etc.)
    let serialized = JSON.stringify(doc, null, 2);
    for (const [pattern, replacement] of stringReplacements) {
      serialized = serialized.replace(pattern, replacement);
    }
    const fixed = JSON.parse(serialized);

    // Fix related_entities: remove "Shelf District", add "Gray Zone" if not present
    if (Array.isArray(fixed.related_entities)) {
      fixed.related_entities = fixed.related_entities
        .map(e => e === 'Shelf District' ? 'Gray Zone' : e)
        .filter((e, i, arr) => arr.indexOf(e) === i); // deduplicate
    }

    // Fix tags: remove "shelf", add "gray-zone" if not present
    if (Array.isArray(fixed.tags)) {
      fixed.tags = fixed.tags.filter(t => t !== 'shelf');
      if (!fixed.tags.includes('gray-zone')) {
        fixed.tags.push('gray-zone');
      }
    }

    // Fix file_name: replace "shelf" with "gray_zone"
    if (fixed.file_name && fixed.file_name.includes('shelf')) {
      fixed.file_name = fixed.file_name.replace(/shelf/g, 'gray_zone');
    }

    const newContent = JSON.stringify(fixed, null, 2);
    if (newContent !== JSON.stringify(JSON.parse(original), null, 2)) {
      fs.writeFileSync(filepath, newContent, 'utf8');
      console.log(`  Updated: ${filename}`);
      updated++;
    } else {
      console.log(`  No change: ${filename}`);
    }
  } catch (e) {
    console.error(`  ERROR: ${filename}: ${e.message}`);
    errors++;
  }
}

console.log(`\nDone. Updated: ${updated}, Errors: ${errors}`);
