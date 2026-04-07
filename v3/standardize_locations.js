const fs = require('fs');
const path = require('path');

const CHAR_DIR = path.join(__dirname, '..', 'engine', 'data', 'characters');

// Canonical districts
const DISTRICTS = {
  'The Shelf': 'Tier 1',
  'The Circuit': 'Tier 2-3',
  'Old Harbor': 'Tier 2',
  'The Laceworks': 'Tier 3-4',
  'Meridian Core': 'Tier 3-4',
  'The Spires': 'Tier 4-5',
  'The Underworld': 'Below',
  'The Wasteland': 'Outside'
};

// Street references by district for appending
const STREET_REFS = {
  'The Shelf': [
    'Halsted and Division',
    'Ashland and Chicago Ave',
    'Western and Division',
    'Pulaski and Chicago Ave',
    'Kedzie and Division',
    'Halsted and Chicago Ave',
    'Ashland and Division',
    'Western and Chicago Ave'
  ],
  'The Circuit': [
    'Milwaukee and Damen',
    'North Ave and Damen',
    'Armitage and Milwaukee',
    'Fullerton and Damen',
    'Milwaukee and North Ave',
    'Damen and Armitage',
    'North Ave and Milwaukee'
  ],
  'Old Harbor': [
    'Lake Shore and Wacker',
    'Michigan Ave South and Cermak',
    'Lake Shore and Navy Pier',
    'Wacker and State',
    'Michigan Ave and Roosevelt'
  ],
  'The Laceworks': [
    'Lincoln and Belmont',
    'Clark and Addison',
    'Lincoln and Addison',
    'Clark and Belmont',
    'Lincoln and Clark'
  ],
  'Meridian Core': [
    'State and Madison',
    'LaSalle and Jackson',
    'State and Jackson',
    'LaSalle and Madison',
    'State and Monroe'
  ],
  'The Spires': [
    'Michigan Ave and Oak',
    'Lake Shore Drive and Division',
    'Michigan Ave and Walton',
    'Lake Shore Drive and North',
    'Michigan Ave and Chicago Ave'
  ],
  'The Underworld': [
    'sub-level access near State and Van Buren',
    'sub-level access near LaSalle and Congress',
    'sub-level access near Dearborn and Jackson'
  ],
  'The Wasteland': [
    'beyond the Western Perimeter',
    'past the Kedzie Barrier',
    'outside the Pulaski Gate'
  ]
};

// Keyword-based mapping rules (checked in order, first match wins)
// Each rule: [testFn, districtName]
function classifyLocation(loc) {
  if (!loc) return null;
  const lower = loc.toLowerCase();

  // === UNDERWORLD (check before others since Deepwell/tunnel keywords are specific) ===
  if (lower.includes('deepwell') ||
      lower.includes('underworld') ||
      lower.includes('underbelly') ||
      lower.includes('abyssal threshold') ||
      lower.includes('the undertow') ||
      lower.includes('sub-level') ||
      lower.includes('sublevel') ||
      lower.includes('underground') ||
      lower.includes('tunnel network') ||
      lower.includes('maintenance corridor') ||
      lower.includes('sub-street tunnel') ||
      lower.includes('south deering sump')) {
    return 'The Underworld';
  }

  // === WASTELAND ===
  if (lower.includes('wasteland') ||
      lower.includes('badlands') ||
      lower.includes('western border') ||
      lower.includes('outside the city') ||
      lower.includes('kenosha crossing') ||
      lower.includes('camp eleven') ||
      lower.includes('grand crossing gate') ||
      lower.includes('hyperlane') ||
      lower.includes('morgan\'s ridge') ||
      lower.includes('manitowoc drydock') ||
      lower.includes('maglev network') ||
      lower.includes('northern hyperlane')) {
    return 'The Wasteland';
  }

  // === SPIRES (check before Core since both can have "corporate") ===
  if (lower.includes('the spire') ||
      lower.includes('highland park') ||
      lower.includes('beverlynn') ||
      lower.includes('pinnacle tower') ||
      lower.includes('penthouse') ||
      lower.includes('tier 5') ||
      lower.includes('tier 4') ||
      lower.includes('87th floor') ||
      lower.includes('executive floor') ||
      lower.includes('grosse pointe') ||
      lower.includes('tessera corporate tower') ||
      lower.includes('tessera security nexus') ||
      lower.includes('lincoln fortress')) {
    return 'The Spires';
  }

  // === MERIDIAN CORE ===
  if (lower.includes('meridian core') ||
      lower.includes('the core') ||
      lower.includes('axiom industries central') ||
      lower.includes('axiom corporate campus') && !lower.includes('gage circuit') ||
      lower.includes('vantage meridian corporate') ||
      lower.includes('glm arbitration') ||
      lower.includes('glm diplomatic') ||
      lower.includes('central tower') ||
      lower.includes('meridian station')) {
    return 'Meridian Core';
  }

  // === LACEWORKS ===
  if (lower.includes('lacework') ||
      lower.includes('edgewater prism') ||
      lower.includes('the canopy') ||
      lower.includes('avalon quiet') ||
      lower.includes('engelheim') ||
      lower.includes('university spine') ||
      lower.includes('lakeview neon') ||
      lower.includes('norwood quiet') ||
      lower.includes('montclare quiet') ||
      lower.includes('nordpark')) {
    return 'The Laceworks';
  }

  // === OLD HARBOR ===
  if (lower.includes('old harbor') ||
      lower.includes('dockside') ||
      lower.includes('bay view dock') ||
      lower.includes('collinwood dock') ||
      lower.includes('waterfront') ||
      lower.includes('whitecap') ||
      lower.includes('pier ') ||
      lower.includes('bunker 14') ||
      lower.includes('escanaba gateway') ||
      lower.includes('waukegan industrial') ||
      lower.includes('lakefront cargo') ||
      lower.includes('grindstone shore') ||
      lower.includes('dock ')) {
    return 'Old Harbor';
  }

  // === SHELF neighborhoods ===
  if (lower.includes('the shelf') ||
      lower.includes('hamtramck') ||
      lower.includes('shallowgrave') ||
      lower.includes('ashfield') ||
      lower.includes('ashfeld') ||  // alternate spelling in data
      lower.includes('kessler row') ||
      lower.includes('fort anchor') ||
      lower.includes('the stockyard') ||
      lower.includes('gravesend') ||
      lower.includes('mexicantown') ||
      lower.includes('oxidian market') ||
      lower.includes('alban souk') ||
      lower.includes('pilsen veil') ||
      lower.includes('brightmoor') ||
      lower.includes('irkalla') ||
      lower.includes('washburn commons') ||
      lower.includes('the rookery') ||
      lower.includes('the lattice') ||
      lower.includes('the overhang') ||
      lower.includes('the rampart') ||
      lower.includes('hough reclamation') ||
      lower.includes('chatham flats') ||
      lower.includes('mckinley flats') ||
      lower.includes('crucible square') ||
      lower.includes('lockhaven') ||
      lower.includes('squat') ||
      lower.includes('slum') ||
      lower.includes('tier 1')) {
    return 'The Shelf';
  }

  // === CIRCUIT neighborhoods ===
  if (lower.includes('the circuit') ||
      lower.includes('geartown') ||
      lower.includes('the narrows') ||
      lower.includes('burnished market') ||
      lower.includes('grand corridor') ||
      lower.includes('kessler interchange') ||
      lower.includes('glassway') ||
      lower.includes('bronzeline') ||
      lower.includes('dearborn forge') ||
      lower.includes('edison grid') ||
      lower.includes('copperhead') ||
      lower.includes('ironvein') ||
      lower.includes('steamvent') ||
      lower.includes('burnside') ||
      lower.includes('gage circuit') ||
      lower.includes('ferment quarter') ||
      lower.includes("brewer's spine") ||
      lower.includes('blackpipe corridor') ||
      lower.includes('calumet rise') ||
      lower.includes('grainfort') ||
      lower.includes('aurochs medical') ||
      lower.includes('archer\'s line') ||
      lower.includes('freestone') ||
      lower.includes('bridgepoint') ||
      lower.includes('north branch') ||
      lower.includes('kenwood gate') ||
      lower.includes('milwaukee core') ||
      lower.includes('the garret') ||
      lower.includes('harrowgate') ||
      lower.includes('jefferson switch') ||
      lower.includes('lincoln spear') ||
      lower.includes('workshop') ||
      lower.includes('garage') ||
      lower.includes('tier 2') ||
      lower.includes('tier 3')) {
    return 'The Circuit';
  }

  // Catch distributed/unknown AI entities — default to Meridian Core
  if (lower.includes('distributed') ||
      lower.includes('no single location') ||
      lower.includes('unknown') ||
      lower.includes('no fixed') ||
      lower.includes('network infrastructure')) {
    return 'Meridian Core';
  }

  return null;
}

// Deterministic but varied street selection based on character name
function pickStreet(district, name) {
  const refs = STREET_REFS[district];
  if (!refs || refs.length === 0) return null;
  // Simple hash from name
  let hash = 0;
  for (let i = 0; i < name.length; i++) {
    hash = ((hash << 5) - hash) + name.charCodeAt(i);
    hash |= 0;
  }
  return refs[Math.abs(hash) % refs.length];
}

// Check if location already has a street-like reference
function hasStreetRef(loc) {
  const streetWords = [
    'halsted', 'ashland', 'division', 'chicago ave', 'western', 'pulaski', 'kedzie',
    'milwaukee', 'damen', 'north ave', 'armitage', 'fullerton',
    'lake shore', 'wacker', 'michigan ave', 'navy pier', 'cermak', 'roosevelt',
    'lincoln', 'clark', 'belmont', 'addison',
    'state', 'madison', 'lasalle', 'jackson', 'monroe',
    'oak st', 'walton', 'gold coast',
    'and street', 'avenue', ' st ', ' st,', ' ave ', ' ave,', ' blvd'
  ];
  const lower = loc.toLowerCase();
  return streetWords.some(w => lower.includes(w));
}

// Main
function main() {
  const files = fs.readdirSync(CHAR_DIR).filter(f => f.endsWith('.json'));
  console.log(`Found ${files.length} character files`);

  const districtCounts = {};
  let unmapped = 0;
  let total = 0;
  const unmappedLocations = [];

  for (const file of files) {
    const filePath = path.join(CHAR_DIR, file);
    let data;
    try {
      data = JSON.parse(fs.readFileSync(filePath, 'utf8'));
    } catch (e) {
      console.error(`Failed to parse ${file}: ${e.message}`);
      continue;
    }

    if (data.type !== 'character') continue;
    total++;

    const loc = data.location || '';
    let district = classifyLocation(loc);

    if (!district) {
      // Default to The Circuit
      district = 'The Circuit';
      unmapped++;
      unmappedLocations.push({ name: data.name, location: loc });
    }

    data.district = district;

    // Add street reference if not already present
    if (!hasStreetRef(loc) && loc.length > 0) {
      const street = pickStreet(district, data.name || file);
      if (street) {
        data.location = `${loc}, near ${street}`;
      }
    }

    fs.writeFileSync(filePath, JSON.stringify(data, null, 2), 'utf8');

    districtCounts[district] = (districtCounts[district] || 0) + 1;
  }

  console.log(`\nProcessed ${total} characters\n`);
  console.log('=== District Distribution ===');
  const sorted = Object.entries(districtCounts).sort((a, b) => b[1] - a[1]);
  for (const [district, count] of sorted) {
    const pct = ((count / total) * 100).toFixed(1);
    console.log(`  ${district.padEnd(20)} ${String(count).padStart(4)} (${pct}%)`);
  }

  console.log(`\n${unmapped} characters could not be mapped (defaulted to The Circuit)`);
  if (unmappedLocations.length > 0) {
    console.log('\nUnmapped characters:');
    for (const { name, location } of unmappedLocations) {
      console.log(`  - ${name}: "${location}"`);
    }
  }
}

main();
