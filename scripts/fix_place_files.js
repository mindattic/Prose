#!/usr/bin/env node
// fix_place_files.js
// Phase 3: Add zoneType and controllingCorpoNation fields to place files.
// Also scrubs any remaining "the sprawl" / "Shelf" nickname references.

import fs from 'fs';
import path from 'path';

const PLACES_DIR = 'engine/data/places';

// Zone classification lookup — infer zone from coordinates or name patterns
// We'll do a best-effort based on lat/lng if present, otherwise leave unknown.
// Chicago Loop ~ 41.88, -87.63; Green Bay ~ 44.51, -88.00

function inferZone(doc) {
  // If the doc has a zone field already, keep it
  if (doc.zone) return doc.zone;

  const lat = doc.coordinates?.lat ?? doc.lat;
  const lng = doc.coordinates?.lng ?? doc.lng;

  if (lat == null || lng == null) return null;

  // Western spine (lng roughly -87.5 to -88.2, lat 41.5 to 44.6)
  if (lng > -88.5 && lng < -87.3) {
    if (lat >= 44.4) return 'Z10'; // Green Bay
    if (lat >= 43.8) return 'Z9';  // Sheboygan / north shore WI
    if (lat >= 43.0) return 'Z8';  // Milwaukee
    if (lat >= 42.5) return 'Z7';  // Kenosha-Racine
    if (lat >= 42.3) return 'Z4';  // North Shore suburbs
    if (lat >= 42.0) return 'Z3';  // Rogers Park / Evanston
    if (lat >= 41.85) return 'Z2'; // North Lakeshore Chicago
    if (lat >= 41.75) return 'Z1'; // Loop Core
    if (lat >= 41.5) return 'Z6';  // South Side
  }

  // West of Chicago (inner suburbs)
  if (lng <= -87.7 && lng > -88.2 && lat >= 41.6 && lat <= 42.1) return 'Z5';

  // Southern wrap (Indiana / SW Michigan)
  if (lat < 41.75 && lat > 41.4 && lng > -87.5) return 'Z11';
  if (lat >= 41.75 && lat < 42.3 && lng > -87.0) return 'Z11'; // SW Michigan reach

  // Eastern tendrils (roughly)
  if (lng > -86.5) return 'Z12';

  return null;
}

const stringFixes = [
  [/\bthe sprawl\b/g, 'the Gray Zone'],
  [/\bthe Sprawl\b/g, 'the Gray Zone'],
  [/\bShelf District\b/g, 'Gray Zone'],
  [/\bshelf district\b/g, 'Gray Zone'],
];

let updated = 0;
let errors = 0;
let total = 0;

const files = fs.readdirSync(PLACES_DIR).filter(f => f.endsWith('.json'));
total = files.length;

for (const filename of files) {
  const filepath = path.join(PLACES_DIR, filename);
  try {
    const raw = fs.readFileSync(filepath, 'utf8');
    let serialized = raw;

    // Apply string replacements first
    for (const [pattern, replacement] of stringFixes) {
      serialized = serialized.replace(pattern, replacement);
    }

    const doc = JSON.parse(serialized);

    let changed = serialized !== raw;

    // Add zone field if missing
    if (!doc.zone) {
      const inferredZone = inferZone(doc);
      if (inferredZone) {
        doc.zone = inferredZone;
        changed = true;
      }
    }

    // Add controllingCorpoNation field if missing (default null)
    if (!Object.prototype.hasOwnProperty.call(doc, 'controllingCorpoNation')) {
      doc.controllingCorpoNation = null;
      changed = true;
    }

    // Fix tags: remove "shelf"
    if (Array.isArray(doc.tags) && doc.tags.includes('shelf')) {
      doc.tags = doc.tags.filter(t => t !== 'shelf');
      if (!doc.tags.includes('gray-zone')) doc.tags.push('gray-zone');
      changed = true;
    }

    if (changed) {
      fs.writeFileSync(filepath, JSON.stringify(doc, null, 2), 'utf8');
      updated++;
    }
  } catch (e) {
    console.error(`  ERROR: ${filename}: ${e.message}`);
    errors++;
  }
}

console.log(`\nDone. Total: ${total}, Updated: ${updated}, Errors: ${errors}`);
