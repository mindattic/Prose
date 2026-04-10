/**
 * extract_places.js
 * Scans all JSON entity files across all repos in engine/data/
 * Identifies mentions of known real-world and fictional locations
 * Creates Place repo entries for locations that don't already exist
 * Adds related_entities links between source documents and places
 */

const fs = require('fs');
const path = require('path');
const crypto = require('crypto');

const DATA_DIR = path.resolve(__dirname, '..', 'engine', 'data');
const PLACES_DIR = path.join(DATA_DIR, 'places');

// ── PHASE 2: Known locations with coordinates ──────────────────────────

const KNOWN_LOCATIONS = [
  // US States & Regions
  { name: "Florida", lat: 27.6648, lng: -81.5158, tags: ["place", "outside", "us-state"], desc: "The state of Florida in the former United States, now a contested territory in the post-collapse American landscape." },
  { name: "Kentucky", lat: 37.8393, lng: -84.2700, tags: ["place", "outside", "us-state"], desc: "The region formerly known as Kentucky, a buffer zone between the Biomass frontier and the Ohio Corridor." },
  { name: "The Missouri Wetlands", lat: 38.5767, lng: -92.1735, aliases: ["Missouri", "Missouri Wetlands"], tags: ["place", "outside", "wetlands"], desc: "The Missouri Wetlands — what remains of the state of Missouri after ecological collapse transformed its river basins into permanent marshland." },
  { name: "Free Peoples Republic", lat: 44.0682, lng: -114.7420, aliases: ["Idaho", "Montana", "Free Peoples"], tags: ["place", "outside", "sovereign-territory"], desc: "The Free Peoples Republic, a breakaway territory spanning the former states of Idaho and Montana, governed by a loose confederation of survivalist communities." },
  { name: "West Virginia", lat: 38.5976, lng: -80.4549, tags: ["place", "outside", "us-state"], desc: "West Virginia, an isolated mountain territory largely cut off from the major trade corridors and power structures of the 23rd century." },
  { name: "Huntsville", lat: 34.7304, lng: -86.5861, aliases: ["Alabama", "Biomass Zone"], tags: ["place", "outside", "biomass"], desc: "The Huntsville area of former Alabama, now at the edge of the Biomass zone where engineered vegetation has consumed most human infrastructure." },
  { name: "Tennessee", lat: 35.5175, lng: -86.5804, tags: ["place", "outside", "us-state"], desc: "Tennessee, a fractured territory caught between the Biomass expansion from the south and the Ohio Corridor trade routes to the north." },
  { name: "Ohio Corridor", lat: 40.4173, lng: -82.9071, aliases: ["Ohio"], tags: ["place", "outside", "trade-route"], desc: "The Ohio Corridor, a critical overland trade route connecting the Great Lakes city-states to the eastern seaboard remnants." },
  { name: "Lone Star Consolidated", lat: 31.9686, lng: -99.9018, aliases: ["Texas", "Lone Star"], tags: ["place", "outside", "sovereign-territory"], desc: "Lone Star Consolidated, the corporate-sovereign entity that governs the territory formerly known as Texas." },
  { name: "Underground Phoenix", lat: 33.4484, lng: -112.0740, aliases: ["Arizona", "Phoenix"], tags: ["place", "outside", "subterranean"], desc: "Underground Phoenix — the subterranean city built beneath the ruins of Phoenix, Arizona, after surface temperatures made above-ground habitation lethal." },
  { name: "New Mexico", lat: 35.0844, lng: -106.6504, tags: ["place", "outside", "us-state"], desc: "New Mexico, a sparsely populated desert territory with scattered research installations and abandoned military infrastructure." },
  { name: "Nevada", lat: 38.8026, lng: -116.4194, tags: ["place", "outside", "us-state"], desc: "Nevada, a largely depopulated wasteland with a few fortified settlements clustered around remaining water sources." },
  { name: "Utah", lat: 39.3210, lng: -111.0937, tags: ["place", "outside", "us-state"], desc: "Utah, a theocratic enclave that maintained unusual social cohesion through the collapse, now one of the more stable outside territories." },
  { name: "Denver", lat: 39.7392, lng: -104.9903, aliases: ["Colorado"], tags: ["place", "outside", "city"], desc: "Denver, Colorado — a fortified mountain city that leveraged its altitude and water access to survive the collapse as a major trade hub." },
  { name: "New Orleans", lat: 29.9511, lng: -90.0715, tags: ["place", "outside", "city"], desc: "New Orleans, a partially submerged city that refused to die, now operating as a major port and cultural center despite perpetual flooding." },
  { name: "Detroit", lat: 42.3314, lng: -83.0458, tags: ["place", "outside", "city", "great-lakes"], desc: "Detroit, a sprawling industrial ruin and reclamation zone on the western shore of Lake Erie, with deep ties to the Great Lakes city-state network." },
  { name: "Milwaukee", lat: 43.0389, lng: -87.9065, aliases: ["The Milwaukee Core"], tags: ["place", "city", "great-lakes"], desc: "Milwaukee, a Great Lakes city-state on the western shore of Lake Michigan, known for its industrial output and brewing traditions that survived two centuries of upheaval." },
  { name: "Green Bay", lat: 44.5133, lng: -88.0133, tags: ["place", "city", "great-lakes"], desc: "Green Bay, a northern Great Lakes settlement at the mouth of the Fox River, serving as a gateway to the Wisconsin interior." },
  { name: "Chicago", lat: 41.8781, lng: -87.6298, tags: ["place", "city", "great-lakes"], desc: "The geographic territory formerly known as Chicago, now the foundation upon which GLMZ was built." },
  { name: "Indiana Dead Zone", lat: 39.7684, lng: -86.1581, aliases: ["Indiana"], tags: ["place", "outside", "dead-zone"], desc: "The Indiana Dead Zone, a vast depopulated region between the Great Lakes city-states and the southern territories, rendered uninhabitable by industrial contamination." },
  { name: "Wisconsin Quiet Zone", lat: 46.0, lng: -89.5, aliases: ["Quiet Zone"], tags: ["place", "outside", "quiet-zone"], desc: "The Wisconsin Quiet Zone, a region of northern Wisconsin where electromagnetic interference makes electronic communication unreliable, inhabited by communities that prefer it that way." },
  { name: "Lake Huron Signal", lat: 44.0, lng: -82.5, aliases: ["Huron Signal"], tags: ["place", "outside", "anomaly", "great-lakes"], desc: "The Lake Huron Signal, an unexplained electromagnetic phenomenon detected in the waters of Lake Huron that has resisted all attempts at identification or explanation." },
  { name: "Toledo", lat: 41.6528, lng: -83.5379, tags: ["place", "outside", "city", "great-lakes"], desc: "Toledo, a border city between the Great Lakes network and the Ohio Corridor, serving as a critical trade junction." },
  { name: "Toronto", lat: 43.6532, lng: -79.3832, aliases: ["Canada"], tags: ["place", "outside", "city", "great-lakes"], desc: "Toronto, the largest surviving Canadian city, operating as a semi-independent city-state with deep trade connections to the Great Lakes network." },
  { name: "Thunder Bay", lat: 48.3809, lng: -89.2477, tags: ["place", "outside", "city", "great-lakes"], desc: "Thunder Bay, a remote northern settlement on Lake Superior's western shore, known for its isolation and the independent character of its inhabitants." },

  // Lake Michigan Colonies
  { name: "Freeport", lat: 42.8, lng: -87.2, tags: ["place", "lake-colony", "great-lakes"], desc: "Freeport, a mid-lake floating colony east of Milwaukee, one of the major independent settlements on Lake Michigan." },
  { name: "The Kettle", lat: 42.6, lng: -87.4, tags: ["place", "lake-colony", "great-lakes"], desc: "The Kettle, a floating colony near the Kenosha shoreline on Lake Michigan, known for its dense population and industrial character." },
  { name: "Iron Ring", lat: 43.1, lng: -87.0, tags: ["place", "lake-colony", "great-lakes"], desc: "Iron Ring, a deep-water floating colony east of Milwaukee, one of the more fortified and defensible settlements on Lake Michigan." },
  { name: "The Nursery", lat: 43.5, lng: -86.8, tags: ["place", "lake-colony", "great-lakes"], desc: "The Nursery, a mobile floating colony that drifts northward through Lake Michigan, known for its agricultural and biological research programs." },

  // GLMZ Specific (mapped to Chicago geography)
  { name: "The Biomass", lat: 34.7, lng: -86.6, tags: ["place", "outside", "biomass", "ecological-hazard"], desc: "The Biomass, a massive zone of engineered vegetation that has consumed most of the former southeastern United States, advancing northward and consuming human infrastructure in its path." },
  { name: "Ridgepost", lat: 35.2, lng: -86.4, tags: ["place", "outside", "consumed", "biomass"], desc: "Ridgepost, a settlement consumed by the Biomass advance, now referenced only in historical records and cautionary tales about the vegetation front's relentless expansion." },
  { name: "The Spine", lat: 41.5, lng: -87.6, tags: ["place", "corridor", "infrastructure"], desc: "The Spine, a major transit and infrastructure corridor running through GLMZ, serving as the primary north-south artery of the city." },
];

// ── Helper functions ───────────────────────────────────────────────────

function generateId() {
  return crypto.randomBytes(16).toString('hex');
}

function readJsonFile(filePath) {
  try {
    return JSON.parse(fs.readFileSync(filePath, 'utf8'));
  } catch {
    return null;
  }
}

function writeJsonFile(filePath, data) {
  fs.writeFileSync(filePath, JSON.stringify(data, null, 2), 'utf8');
}

function getAllJsonFiles(dir) {
  const results = [];
  try {
    const entries = fs.readdirSync(dir, { withFileTypes: true });
    for (const entry of entries) {
      const fullPath = path.join(dir, entry.name);
      if (entry.isDirectory() && entry.name !== 'chromadb' && entry.name !== 'graph' && entry.name !== 'stories') {
        results.push(...getAllJsonFiles(fullPath));
      } else if (entry.isFile() && entry.name.endsWith('.json')) {
        results.push(fullPath);
      }
    }
  } catch { /* skip unreadable dirs */ }
  return results;
}

function extractTextFields(obj) {
  const texts = [];
  if (!obj || typeof obj !== 'object') return texts;

  const textKeys = [
    'description', 'body', 'cultural_context', 'story_hooks',
    'location', 'atmosphere', 'demographics', 'economy',
    'power_structure', 'dangers', 'opportunities', 'flavor_text',
    'context', 'narrative', 'summary', 'content', 'notes',
    'backstory', 'history', 'lore', 'overview', 'effect',
    'role', 'status_text'
  ];

  for (const key of textKeys) {
    if (obj[key]) {
      if (typeof obj[key] === 'string') {
        texts.push(obj[key]);
      } else if (Array.isArray(obj[key])) {
        for (const item of obj[key]) {
          if (typeof item === 'string') texts.push(item);
          else if (typeof item === 'object') texts.push(...extractTextFields(item));
        }
      } else if (typeof obj[key] === 'object') {
        texts.push(...extractTextFields(obj[key]));
      }
    }
  }

  return texts;
}

// Build search patterns — need word boundary matching for short names
function buildSearchPatterns(loc) {
  const names = [loc.name];
  if (loc.aliases) names.push(...loc.aliases);

  const patterns = [];
  for (const name of names) {
    // Escape regex special chars
    const escaped = name.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
    // Use word boundary for names, case-insensitive
    patterns.push(new RegExp(`\\b${escaped}\\b`, 'i'));
  }
  return patterns;
}

// ── MAIN ───────────────────────────────────────────────────────────────

function main() {
  console.log('=== Place Extraction Script ===\n');

  // ── PHASE 1: Build existing places index ───────────────────────────
  console.log('PHASE 1: Indexing existing places...');
  const existingPlaces = new Map(); // lowercase name -> { id, name, filePath }
  const placeFiles = fs.readdirSync(PLACES_DIR).filter(f => f.endsWith('.json'));

  for (const f of placeFiles) {
    const data = readJsonFile(path.join(PLACES_DIR, f));
    if (!data || !data.name) continue;
    existingPlaces.set(data.name.toLowerCase(), {
      id: data.id,
      name: data.name,
      filePath: path.join(PLACES_DIR, f)
    });
    // Also index aliases
    if (data.aliases) {
      for (const alias of data.aliases) {
        existingPlaces.set(alias.toLowerCase(), {
          id: data.id,
          name: data.name,
          filePath: path.join(PLACES_DIR, f)
        });
      }
    }
  }
  console.log(`  Found ${placeFiles.length} existing place files`);
  console.log(`  Indexed ${existingPlaces.size} names/aliases\n`);

  // ── PHASE 2: Prepare location lookup ────────────────────────────────
  console.log('PHASE 2: Preparing location lookup...');

  // Filter out locations that already exist
  const locationsToTrack = [];
  const newLocations = [];

  for (const loc of KNOWN_LOCATIONS) {
    const allNames = [loc.name, ...(loc.aliases || [])];
    const alreadyExists = allNames.some(n => existingPlaces.has(n.toLowerCase()));

    if (alreadyExists) {
      // Still track for cross-referencing
      const existing = allNames.map(n => existingPlaces.get(n.toLowerCase())).find(Boolean);
      locationsToTrack.push({
        ...loc,
        existingName: existing.name,
        isNew: false
      });
    } else {
      newLocations.push(loc);
      locationsToTrack.push({ ...loc, isNew: true });
    }
  }

  console.log(`  ${KNOWN_LOCATIONS.length} known locations defined`);
  console.log(`  ${KNOWN_LOCATIONS.length - newLocations.length} already exist in places repo`);
  console.log(`  ${newLocations.length} new locations to potentially create\n`);

  // ── Build search patterns for all locations ─────────────────────────
  const searchEntries = locationsToTrack.map(loc => ({
    loc,
    patterns: buildSearchPatterns(loc),
    canonicalName: loc.isNew ? loc.name : loc.existingName
  }));

  // ── PHASE 3: Scan all documents ────────────────────────────────────
  console.log('PHASE 3: Scanning all entity files...');

  const allFiles = getAllJsonFiles(DATA_DIR).filter(f => {
    // Skip place files themselves, and non-entity JSON
    const rel = path.relative(DATA_DIR, f);
    return !rel.startsWith('places') &&
           !rel.startsWith('chromadb') &&
           !['neo-noir_tone_bible.json', 'kyle.json', 'literary_rules.json',
             'motifs.json', 'story_bible.json', 'trivia.json', 'tts_rules.json'].includes(path.basename(f));
  });

  console.log(`  Found ${allFiles.length} entity files to scan\n`);

  // Track which locations were found (to know which new ones to create)
  const locationMentions = new Map(); // canonicalName -> Set of source file paths
  const fileMentions = new Map(); // filePath -> Set of canonicalNames

  let scanned = 0;
  for (const filePath of allFiles) {
    const data = readJsonFile(filePath);
    if (!data) continue;

    scanned++;
    if (scanned % 500 === 0) {
      process.stdout.write(`  Scanned ${scanned}/${allFiles.length}...\r`);
    }

    // Extract all text from the entity
    const texts = extractTextFields(data);
    const fullText = texts.join(' ');

    // Also check character location field specifically (PHASE 4)
    if (data.location && typeof data.location === 'string') {
      texts.push(data.location);
    }

    const combinedText = texts.join('\n');

    if (combinedText.length < 10) continue;

    for (const entry of searchEntries) {
      const matched = entry.patterns.some(p => p.test(combinedText));
      if (matched) {
        if (!locationMentions.has(entry.canonicalName)) {
          locationMentions.set(entry.canonicalName, new Set());
        }
        locationMentions.get(entry.canonicalName).add(filePath);

        if (!fileMentions.has(filePath)) {
          fileMentions.set(filePath, new Set());
        }
        fileMentions.get(filePath).add(entry.canonicalName);
      }
    }
  }

  console.log(`  Scanned ${scanned} files`);
  console.log(`  Found ${locationMentions.size} locations mentioned`);
  console.log(`  Found ${fileMentions.size} files with location mentions\n`);

  // ── Create new place files ──────────────────────────────────────────
  console.log('Creating new place files...');
  let placesCreated = 0;
  const newPlaceNames = new Map(); // name -> place name for cross-ref

  for (const loc of newLocations) {
    // Only create if actually mentioned somewhere
    if (!locationMentions.has(loc.name)) {
      console.log(`  SKIP (no mentions): ${loc.name}`);
      continue;
    }

    const id = generateId();
    const placeData = {
      id: id,
      type: "place",
      name: loc.name,
      aliases: loc.aliases || [],
      description: loc.desc,
      coordinates: { lat: loc.lat, lng: loc.lng, tags: [] },
      connections: { adjacent_to: [], exits: [], tags: [] },
      tags: loc.tags,
      related_entities: []
    };

    const filePath = path.join(PLACES_DIR, `${id}.json`);
    writeJsonFile(filePath, placeData);
    placesCreated++;

    // Register in existing places for cross-referencing
    existingPlaces.set(loc.name.toLowerCase(), { id, name: loc.name, filePath });
    if (loc.aliases) {
      for (const alias of loc.aliases) {
        existingPlaces.set(alias.toLowerCase(), { id, name: loc.name, filePath });
      }
    }
    newPlaceNames.set(loc.name, loc.name);

    console.log(`  CREATED: ${loc.name} (${id}.json) — ${locationMentions.get(loc.name).size} mentions`);
  }

  console.log(`\n  Total new places created: ${placesCreated}\n`);

  // ── Add cross-references ────────────────────────────────────────────
  console.log('Adding cross-references...');
  let crossRefsAdded = 0;
  let filesModified = 0;

  for (const [filePath, placeNames] of fileMentions) {
    const data = readJsonFile(filePath);
    if (!data) continue;

    if (!data.related_entities) {
      data.related_entities = [];
    }

    let modified = false;
    for (const placeName of placeNames) {
      // Get the canonical place name from existing places
      const placeInfo = existingPlaces.get(placeName.toLowerCase());
      const nameToAdd = placeInfo ? placeInfo.name : placeName;

      if (!data.related_entities.includes(nameToAdd)) {
        data.related_entities.push(nameToAdd);
        crossRefsAdded++;
        modified = true;
      }
    }

    if (modified) {
      writeJsonFile(filePath, data);
      filesModified++;
    }
  }

  console.log(`  Cross-references added: ${crossRefsAdded}`);
  console.log(`  Files modified: ${filesModified}\n`);

  // ── Also add back-references from places to mentioning entities ─────
  console.log('Adding back-references to place files...');
  let backRefsAdded = 0;

  for (const [placeName, mentioningFiles] of locationMentions) {
    const placeInfo = existingPlaces.get(placeName.toLowerCase());
    if (!placeInfo || !placeInfo.filePath) continue;

    const placeData = readJsonFile(placeInfo.filePath);
    if (!placeData) continue;

    if (!placeData.related_entities) {
      placeData.related_entities = [];
    }

    let modified = false;
    for (const srcPath of mentioningFiles) {
      const srcData = readJsonFile(srcPath);
      if (!srcData || !srcData.name) continue;

      if (!placeData.related_entities.includes(srcData.name)) {
        placeData.related_entities.push(srcData.name);
        backRefsAdded++;
        modified = true;
      }
    }

    if (modified) {
      writeJsonFile(placeInfo.filePath, placeData);
    }
  }

  console.log(`  Back-references added to places: ${backRefsAdded}\n`);

  // ── Summary ─────────────────────────────────────────────────────────
  console.log('=== SUMMARY ===');
  console.log(`New places created:       ${placesCreated}`);
  console.log(`Cross-references added:   ${crossRefsAdded}`);
  console.log(`Files modified:           ${filesModified}`);
  console.log(`Back-references to places: ${backRefsAdded}`);
  console.log(`Total locations tracked:  ${locationMentions.size}`);

  // Print mention counts per location
  console.log('\n=== LOCATION MENTION COUNTS ===');
  const sorted = [...locationMentions.entries()].sort((a, b) => b[1].size - a[1].size);
  for (const [name, files] of sorted) {
    const isNew = newLocations.some(l => l.name === name);
    console.log(`  ${name}: ${files.size} mentions ${isNew ? '(NEW)' : '(existing)'}`);
  }
}

main();
