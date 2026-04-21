// generate-territory-map.js
// Generates territory-map.json — explicit non-overlapping polygon tiles
// for every corponation territory and major gray zone along the GLMZ.
// Run: node generate-territory-map.js  (from v3/ directory)

const fs   = require('fs');
const path = require('path');

const OUTPUT_PATH = path.resolve(__dirname, 'StreetSamurai.Blazor/wwwroot/data/territory-map.json');

// sw/nw/ne/se helper — simple rectangle [SW,NW,NE,SE]
function rect(s, n, w, e) {
  return [
    { lat: s, lng: w },
    { lat: n, lng: w },
    { lat: n, lng: e },
    { lat: s, lng: e }
  ];
}

const PRESTIGE_COLORS = {
  5: '#e6c44a',   // gold
  4: '#58a6ff',   // blue
  3: '#bc8cff',   // purple
  2: '#3fb950',   // green
  1: '#f0883e'    // orange
};
const PRESTIGE_OPACITIES = {
  5: 0.22, 4: 0.17, 3: 0.14, 2: 0.11, 1: 0.09
};
const GZ_COLOR   = '#8b949e';
const GZ_OPACITY = 0.09;

function t(id, name, label, prestige, loopProximity, corponation, corpName, territoryType, paths) {
  return {
    id, name, label,
    type: 'territory',
    corponation, corponationName: corpName,
    prestige, loopProximity, territoryType,
    color: PRESTIGE_COLORS[prestige],
    opacity: PRESTIGE_OPACITIES[prestige],
    paths
  };
}
function gz(id, name, gzType, governance, paths) {
  return { id, name, label: '', type: 'grayzone', gzType, governance: (governance || '').substring(0, 100), color: GZ_COLOR, opacity: GZ_OPACITY, paths };
}

const polygons = [

  // ═══════════════════════════════════════════════════════════════════════════
  // SOUTH INDUSTRIAL — Gary / Calumet / South Chicago  (41.54 – 41.84)
  // ═══════════════════════════════════════════════════════════════════════════

  t('scoria-gary-lakefront', 'The Scoria Crucible Belt', 'Scoria', 1, 'south-industrial',
    'scoria-works', 'Scoria Works', 'polygon',
    rect(41.554, 41.618, -87.388, -87.290)),

  t('palladian-gary-ruins', 'The Palladian Prime Enclave', 'Palladian', 2, 'south-industrial',
    'palladian-construction', 'Palladian Construction', 'polygon',
    rect(41.560, 41.620, -87.415, -87.388)),

  t('vespid-arcology-cluster', 'The Vespid Arcology Cluster', 'Vespid', 1, 'south-industrial',
    'vespid-dynamics', 'Vespid Dynamics', 'polygon',
    rect(41.552, 41.605, -87.318, -87.242)),

  t('ashgrave-synthesis-corridor', 'The Ashgrave Synthesis Corridor', 'Ashgrave', 3, 'south-industrial',
    'ashgrave-materials', 'Ashgrave Materials', 'polygon',
    rect(41.620, 41.840, -87.558, -87.436)),

  t('slagworks-foundry-belt', 'The Slagworks Foundry Belt', 'Slagworks', 1, 'south-industrial',
    'slagworks-industrial', 'Slagworks Industrial', 'polygon',
    rect(41.700, 41.748, -87.610, -87.555)),

  t('crucible-calumet', 'The Crucible Belt', 'Crucible', 1, 'south-industrial',
    'crucible-genomics', 'Crucible Genomics', 'polygon',
    rect(41.716, 41.776, -87.650, -87.608)),

  t('cinderfall-subterranean', 'The Cinderfall Subterranean Network', 'Cinderfall', 2, 'south-industrial',
    'cinderfall-energy', 'Cinderfall Energy', 'subsurface',
    rect(41.695, 41.738, -87.545, -87.495)),

  // ── South industrial gray zones ──────────────────────────────────────────

  gz('gary-dead-zone', 'The Gary Dead Zone', 'wasteland',
    'Gary Survivors\' Council — ~15,000 residents, hospital, reclamation market.',
    rect(41.540, 41.620, -87.480, -87.390)),

  gz('calumet-wasteland', 'The Calumet Wasteland', 'wasteland',
    'No formal governance. Calumet Recovery Collective runs contamination clinics.',
    rect(41.618, 41.705, -87.660, -87.555)),

  gz('hammond-seam', 'The Hammond Seam', 'seam',
    'No council. Population clusters around Calumet River crossing.',
    rect(41.558, 41.620, -87.510, -87.415)),

  gz('east-gary-drift', 'The East Gary Drift', 'drift',
    'No formal governance. Transient labor camps follow Vespid and Charnel contracts.',
    rect(41.555, 41.620, -87.250, -87.145)),

  gz('south-chicago-seam', 'The South Chicago Seam', 'seam',
    'South Chicago Mutual Aid Collective provides emergency services.',
    rect(41.720, 41.845, -87.610, -87.555)),

  gz('bridgeport-pocket', 'The Bridgeport Pocket', 'pocket',
    'Bridgeport Block Federation — oldest functioning gray zone government in the GLMZ.',
    rect(41.820, 41.862, -87.700, -87.640)),

  gz('printer-row-drift', 'The Printer\'s Row Drift', 'pocket',
    'No formal council. Transit and service gray zone for Coldwall workers.',
    rect(41.840, 41.868, -87.640, -87.605)),

  // ═══════════════════════════════════════════════════════════════════════════
  // CHICAGO LOOP CORE — prestige 4-5  (41.842 – 41.940)
  // ═══════════════════════════════════════════════════════════════════════════

  t('coldwall-quarter', 'The Coldwall Quarter', 'Coldwall', 4, 'prime',
    'arcturus-defense-solutions', 'Arcturus Defense Solutions', 'polygon',
    rect(41.842, 41.866, -87.672, -87.608)),

  t('zheng-dao-financial-corridor', 'The Zheng-dao Financial Corridor', 'Zheng-dao', 5, 'core',
    'zheng-dao-bioelectric', 'Zheng-dao Bioelectric', 'polygon',
    rect(41.864, 41.898, -87.694, -87.658)),

  t('waxwing-spire-district', 'The Waxwing Spire District', 'Waxwing', 4, 'core',
    'waxwing-neuromedia', 'Waxwing Neuromedia', 'polygon',
    rect(41.864, 41.896, -87.657, -87.637)),

  t('tessera-sovereign-enclave', 'The Tessera Sovereign Enclave', 'Tessera', 5, 'core',
    'tessera-corponation', 'Tessera Corponation', 'polygon',
    rect(41.864, 41.900, -87.636, -87.600)),

  // Mirrorwell covers River North (west/inland half of the north-of-river band)
  t('mirrorwell-arcology-district', 'The Mirrorwell Arcology District', 'Mirrorwell', 4, 'prime',
    'mirrorwell-media', 'Mirrorwell Media', 'polygon',
    rect(41.900, 41.942, -87.672, -87.642)),

  // ── Loop gray zones ───────────────────────────────────────────────────────

  gz('south-loop-seam', 'The South Loop Seam', 'seam',
    'No formal council. Three competing protection crews along Balbo and Congress.',
    rect(41.838, 41.845, -87.672, -87.608)),

  gz('west-loop-gap', 'The West Loop Gap', 'pocket',
    '12,000 residents in contested space between Pulse corridors and sovereign towers.',
    rect(41.842, 41.942, -87.810, -87.695)),

  gz('grant-park-drift', 'The Grant Park Drift', 'pocket',
    'The Parkers — rotating artist encampment in the former park for 30 years.',
    rect(41.840, 41.870, -87.605, -87.596)),

  gz('river-north-pocket', 'The River North Pocket', 'pocket',
    'Freelancer coalition in the former Merchandise Mart. Off-books talent corridor.',
    rect(41.900, 41.918, -87.642, -87.610)),

  gz('financial-district-seam', 'The Financial District Seam', 'seam',
    'Ungoverned blocks between Zheng-dao and Waxwing perimeters. Gray-market finance.',
    rect(41.864, 41.898, -87.658, -87.656)),

  gz('coldwall-perimeter-seam', 'The Coldwall Perimeter Seam', 'seam',
    'Arcturus Civil Security monitors the perimeter. Street vendors during shift changes.',
    rect(41.838, 41.842, -87.672, -87.608)),

  // ═══════════════════════════════════════════════════════════════════════════
  // NEAR NORTH — Streeterville / Gold Coast / Lincoln Park  (41.90 – 41.98)
  // ═══════════════════════════════════════════════════════════════════════════

  // Helix covers Streeterville (east/lakefront half of north-of-river band)
  t('helix-streeterville', 'The Helix Streeterville Campus', 'Helix', 4, 'inner',
    'helix-biosystems', 'Helix Biosystems', 'polygon',
    rect(41.898, 41.932, -87.641, -87.598)),

  t('vantablack-spire', 'The Vantablack Spire', 'Vantablack', 3, 'inner',
    'vantablack-media', 'Vantablack Media', 'polygon',
    rect(41.928, 41.948, -87.642, -87.618)),

  t('novafold-medical-zone', 'The Novafold Medical Sovereign Zone', 'Novafold', 3, 'inner',
    'novafold-pharmaceuticals', 'Novafold Pharmaceuticals', 'polygon',
    rect(41.942, 41.976, -87.668, -87.600)),

  t('rictus-pleasure-corridor', 'The Rictus Pleasure Corridor', 'Rictus', 3, 'inner',
    'rictus-entertainment', 'Rictus Entertainment', 'polygon',
    rect(41.944, 41.982, -87.692, -87.670)),

  // ── Near North gray zones ─────────────────────────────────────────────────

  gz('gold-coast-seam', 'The Gold Coast Seam', 'seam',
    'Remnant Gold Coast residential association. Helix pays quarterly stipend.',
    rect(41.942, 41.952, -87.648, -87.618)),

  gz('streeterville-drift', 'The Streeterville Drift', 'seam',
    'Medical gray zone — unlicensed practitioners outside Helix perimeter.',
    rect(41.898, 41.928, -87.618, -87.598)),

  gz('lincoln-park-pocket', 'The Lincoln Park Pocket', 'pocket',
    'Lincoln Park Community Alliance — 14 elected representatives, meets monthly.',
    rect(41.940, 41.965, -87.672, -87.644)),

  gz('lakeview-seam', 'The Lakeview Seam', 'seam',
    'Rictus patrols south edge; Pellucid surveillance covers north. Seam ungoverned.',
    rect(41.976, 41.986, -87.692, -87.668)),

  gz('wrigleyville-pocket', 'The Wrigleyville Pocket', 'pocket',
    'Wrigley Block Committee maintains former ballpark. Rictus annexation pending.',
    rect(41.946, 41.966, -87.698, -87.680)),

  // ═══════════════════════════════════════════════════════════════════════════
  // ESTABLISHED NORTH — Rogers Park / Evanston  (41.980 – 42.080)
  // ═══════════════════════════════════════════════════════════════════════════

  t('pellucid-atrium', 'The Pellucid Atrium', 'Pellucid', 4, 'established',
    'pellucid-systems', 'Pellucid Systems', 'polygon',
    rect(41.978, 42.030, -87.718, -87.606)),

  t('vellichor-campus', 'The Vellichor Campus', 'Vellichor', 3, 'established',
    'vellichor-institute', 'Vellichor Institute', 'polygon',
    rect(42.005, 42.058, -87.728, -87.650)),

  t('lazarus-compound', 'The Lazarus Compound', 'Lazarus', 3, 'established',
    'lazarus-pharmaceuticals', 'Lazarus Pharmaceuticals', 'polygon',
    rect(42.022, 42.072, -87.760, -87.722)),

  t('copperveil-campus', 'The Veil Campus', 'Copperveil', 2, 'established',
    'copperveil-intelligence', 'Copperveil Intelligence', 'polygon',
    rect(42.032, 42.072, -87.785, -87.758)),

  // ── Evanston gray zones ───────────────────────────────────────────────────

  gz('rogers-park-commons', 'The Rogers Park Commons', 'pocket',
    'Rogers Park Commons Council — 22 elected representatives, most democratic in Z3.',
    rect(41.975, 42.012, -87.740, -87.720)),

  gz('evanston-fringe', 'The Evanston Fringe', 'pocket',
    'Evanston Fringe Association — governs via academic structures; "faculty senates."',
    rect(42.012, 42.080, -87.762, -87.730)),

  // ═══════════════════════════════════════════════════════════════════════════
  // NORTH SHORE / WAUKEGAN  (42.080 – 42.420)
  // ═══════════════════════════════════════════════════════════════════════════

  t('lacuna-north-shore', 'The Lacuna North Shore Campus', 'Lacuna', 3, 'established',
    'lacuna-genomics', 'Lacuna Genomics', 'polygon',
    rect(42.152, 42.232, -87.800, -87.732)),

  t('ringo-northern-operations', 'The Ringo Northern Operations Corridor', 'Ringo', 3, 'established',
    'ringo-corponation', 'Ringo Corponation', 'polygon',
    rect(42.282, 42.365, -87.968, -87.874)),

  t('ashford-naval-waukegan', 'The Ashford Naval Station', 'Ashford', 2, 'established',
    'ashford-signal', 'Ashford Signal', 'polygon',
    rect(42.338, 42.388, -87.888, -87.820)),

  // ── North Shore gray zones ────────────────────────────────────────────────

  gz('north-shore-gap', 'The North Shore Gap', 'seam',
    'North Shore Residential Councils — legacy organizations from pre-Collapse wealthy suburbs.',
    rect(42.072, 42.155, -87.812, -87.730)),

  gz('highland-park-seam', 'The Highland Park Seam', 'seam',
    'Between Copperveil and Lacuna — most surveilled ungoverned space in the GLMZ.',
    rect(42.078, 42.160, -87.768, -87.745)),

  gz('waukegan-seam', 'The Waukegan Seam', 'seam',
    'Waukegan Port Authority — maintains civilian port infrastructure for Ashford.',
    rect(42.340, 42.400, -87.822, -87.795)),

  gz('kenosha-corridor-gap', 'The Kenosha Corridor Gap', 'corridor',
    'Kenosha Industrial Council — managing gap between Ashford and Liang-Petrova.',
    rect(42.392, 42.580, -87.900, -87.840)),

  // ═══════════════════════════════════════════════════════════════════════════
  // WEST CORRIDOR — O'Hare / Suburbs  (41.84 – 42.02)
  // ═══════════════════════════════════════════════════════════════════════════

  t('stonepath-ohare', 'The Stonepath O\'Hare Sovereignty', 'Stonepath', 3, 'west',
    'stonepath-logistics', 'Stonepath Logistics', 'polygon',
    rect(41.942, 41.998, -87.972, -87.858)),

  t('marrowvault-preserve', 'The Marrowvault Preserve', 'Marrowvault', 3, 'west',
    'marrowvault-cryogenics', 'Marrowvault Cryogenics', 'subsurface',
    rect(41.898, 41.978, -88.118, -87.982)),

  // ── West corridor gray zones ──────────────────────────────────────────────

  gz('ohare-sprawl', 'The O\'Hare Sprawl', 'wasteland',
    'Multiple factions in former terminals. Largest inner-GLMZ gray zone, 40 sq km.',
    rect(41.930, 42.002, -88.000, -87.858)),

  gz('des-plaines-drift', 'The Des Plaines Drift', 'drift',
    'No council. Rotating encampments around seasonal Pulse freight work.',
    rect(41.862, 41.940, -88.080, -87.972)),

  gz('western-suburb-gap', 'The Western Suburb Gap', 'pocket',
    'Western Collective — former suburban municipalities maintaining pre-Collapse services.',
    rect(41.870, 41.958, -88.200, -88.120)),

  // ═══════════════════════════════════════════════════════════════════════════
  // CORRIDOR TYPES — Pulse / Ferrogate  (spine along the corridor)
  // ═══════════════════════════════════════════════════════════════════════════

  t('pulse-hyperlane-rights', 'The Pulse Hyperlane Rights-of-Way', 'Pulse', 5, 'distributed',
    'pulse-mass-transit-international', 'Pulse Mass Transit International', 'corridor',
    [
      { lat: 41.780, lng: -87.692 },
      { lat: 43.108, lng: -87.970 },
      { lat: 43.108, lng: -87.946 },
      { lat: 41.780, lng: -87.668 }
    ]),

  t('ferrogate-rail-corridor', 'The Ferrogate Rail Sovereignty', 'Ferrogate', 3, 'distributed',
    'ferrogate-transit', 'Ferrogate Transit', 'corridor',
    [
      { lat: 41.855, lng: -87.666 },
      { lat: 43.050, lng: -87.942 },
      { lat: 43.050, lng: -87.932 },
      { lat: 41.855, lng: -87.656 }
    ]),

  // ═══════════════════════════════════════════════════════════════════════════
  // RACINE / KENOSHA  (42.58 – 42.80)
  // ═══════════════════════════════════════════════════════════════════════════

  t('dredge-kenosha', 'The Dredge Kenosha Extraction Field', 'Dredge', 1, 'mid-corridor',
    'dredge-mining-collective', 'Dredge Mining Collective', 'offshore',
    rect(42.558, 42.618, -87.852, -87.772)),

  t('liang-petrova-racine', 'The Liang-Petrova Racine Complex', 'Liang-Petrova', 3, 'mid-corridor',
    'liang-petrova-consortium', 'Liang-Petrova Consortium', 'polygon',
    rect(42.705, 42.762, -87.842, -87.748)),

  // ── Racine gray zones ─────────────────────────────────────────────────────

  gz('racine-seam', 'The Racine Seam', 'seam',
    'Racine City Council — maintains civic services for Liang-Petrova contract workforce.',
    rect(42.760, 42.810, -87.828, -87.776)),

  gz('wind-point-pocket', 'The Wind Point Pocket', 'pocket',
    'No formal council. Salvage and fishing community — eyes on the lake for both corps.',
    rect(42.605, 42.660, -87.836, -87.800)),

  // ═══════════════════════════════════════════════════════════════════════════
  // MILWAUKEE  (42.80 – 43.15)
  // ═══════════════════════════════════════════════════════════════════════════

  t('gravemoss-ferment-quarter', 'The Ferment Quarter', 'Gravemoss', 1, 'mid-corridor',
    'gravemoss-biofoundry', 'Gravemoss Biofoundry', 'polygon',
    rect(42.952, 43.010, -87.948, -87.878)),

  t('silkworm-data-arcology', 'The Silkworm Arcology Cluster', 'Silkworm', 3, 'mid-corridor',
    'silkworm-data', 'Silkworm Data', 'polygon',
    rect(43.022, 43.058, -87.938, -87.878)),

  t('ironclad-milwaukee-hq', 'The Ironclad Milwaukee Headquarters', 'Ironclad', 3, 'mid-corridor',
    'ironclad-agrisystems', 'Ironclad Agrisystems', 'polygon',
    rect(43.032, 43.075, -87.995, -87.948)),

  t('ouroboros-ring', 'The Ouroboros Ring', 'Ouroboros', 3, 'mid-corridor',
    'ouroboros-energy', 'Ouroboros Energy', 'corridor',
    [
      { lat: 42.680, lng: -88.028 },
      { lat: 43.282, lng: -88.018 },
      { lat: 43.282, lng: -87.790 },
      { lat: 42.680, lng: -87.792 }
    ]),

  // ── Milwaukee gray zones ──────────────────────────────────────────────────

  gz('milwaukee-gap', 'The Milwaukee Gap', 'pocket',
    'Milwaukee Civic Authority — 200,000 residents; one of the largest gray zone cities.',
    rect(43.010, 43.080, -87.958, -87.902)),

  gz('south-milwaukee-pocket', 'The South Milwaukee Pocket', 'pocket',
    'South Milwaukee Neighborhood Council — monitors Gravemoss bioreactor runoff.',
    rect(42.915, 42.965, -87.950, -87.906)),

  gz('menomonee-valley-seam', 'The Menomonee Valley Seam', 'seam',
    'Transit corridor between Ironclad and Sulfur Crown. Valley council dissolved.',
    rect(43.066, 43.108, -88.005, -87.965)),

  // ═══════════════════════════════════════════════════════════════════════════
  // SHEBOYGAN COAST  (43.15 – 44.10)
  // ═══════════════════════════════════════════════════════════════════════════

  t('kelpline-coastal', 'The Kelpline Coastal Network', 'Kelpline', 2, 'mid-corridor',
    'kelpline-logistics', 'Kelpline Logistics', 'offshore',
    rect(43.380, 44.020, -87.790, -87.650)),

  t('crestfall-platforms', 'The Crestfall Platform Network', 'Crestfall', 1, 'mid-corridor',
    'crestfall-aquaculture', 'Crestfall Aquaculture', 'offshore',
    rect(43.698, 43.808, -87.758, -87.670)),

  t('pelican-drift-yards', 'The Pelican Drift Yards', 'Pelican Drift', 2, 'mid-corridor',
    'pelican-drift-aquatics', 'Pelican Drift Aquatics', 'offshore',
    rect(43.758, 43.858, -87.648, -87.548)),

  t('irontide-anchor-platform', 'The Irontide Anchor Platform', 'Irontide', 1, 'upper-corridor',
    'irontide-tidal-energy', 'Irontide Tidal Energy', 'offshore',
    rect(43.958, 44.055, -87.565, -87.452)),

  // ── Sheboygan gray zones ──────────────────────────────────────────────────

  gz('sheboygan-seam', 'The Sheboygan Seam', 'seam',
    'Sheboygan Harbor Council — manages dock access for Kelpline, Pelican, Crestfall.',
    rect(43.220, 43.285, -87.928, -87.862)),

  gz('port-washington-pocket', 'The Port Washington Pocket', 'pocket',
    'Port Washington Harbor Council — 2,000 residents, critical platform resupply.',
    rect(43.358, 43.400, -87.906, -87.862)),

  // ═══════════════════════════════════════════════════════════════════════════
  // GREEN BAY / UPPER CORRIDOR  (44.10 – 45.10)
  // ═══════════════════════════════════════════════════════════════════════════

  t('rendstone-exclusion', 'The Rendstone Exclusion Corridor', 'Rendstone', 2, 'upper-corridor',
    'rendstone-nuclear', 'Rendstone Nuclear', 'polygon',
    rect(44.172, 44.330, -87.648, -87.466)),

  t('thornback-basin', 'The Thornback Basin', 'Thornback', 1, 'upper-corridor',
    'thornback-agrichemical', 'Thornback Agrichemical', 'polygon',
    rect(44.448, 45.105, -88.058, -86.742)),

  // ── Green Bay gray zones ──────────────────────────────────────────────────

  gz('green-bay-fringe', 'The Green Bay Fringe', 'pocket',
    'Green Bay Urban Council — 80,000 residents beneath Verdant atmospheric districts.',
    rect(44.405, 44.618, -88.118, -88.008)),

  gz('kewaunee-gap', 'The Kewaunee Gap', 'seam',
    'Within Rendstone exclusion monitoring zone. Nobody lives here by choice.',
    rect(44.228, 44.275, -87.615, -87.548)),

  gz('door-peninsula-gap', 'The Door Peninsula Gap', 'pocket',
    'Door Peninsula Alliance — most rural gray zone in the GLMZ. Subsistence farming.',
    rect(44.800, 45.200, -87.420, -86.980)),

  // ═══════════════════════════════════════════════════════════════════════════
  // EASTERN CORRIDOR Z11 — Indiana / Michigan  (41.60 – 41.80)
  // ═══════════════════════════════════════════════════════════════════════════

  t('charnel-propulsion-campus', 'The Charnel Propulsion Campus', 'Charnel', 2, 'eastern-corridor',
    'charnel-propulsion', 'Charnel Propulsion', 'polygon',
    rect(41.652, 41.752, -86.902, -86.718)),

  // ── Z11 gray zones ────────────────────────────────────────────────────────

  gz('indiana-seam', 'The Indiana Seam', 'seam',
    'Indiana Seam Traders Association — manages the only market at Michigan City junction.',
    rect(41.610, 41.680, -87.230, -87.010)),

  gz('michigan-city-gap', 'The Michigan City Gap', 'pocket',
    'Michigan City Remnant Council. The Settling drowned the shoreline mid-23rd century.',
    rect(41.682, 41.762, -87.010, -86.825)),

  gz('st-joseph-corridor', 'The St. Joseph Corridor', 'corridor',
    'St. Joseph Gray Zone Council — supplies workers to Charnel, sets labor rates.',
    rect(41.750, 41.820, -86.538, -86.390)),

  // ═══════════════════════════════════════════════════════════════════════════
  // EASTERN CORRIDOR Z12 — Detroit  (42.28 – 42.46)
  // ═══════════════════════════════════════════════════════════════════════════

  t('cinderblock-campus', 'The Cinderblock Campus', 'Cinderblock', 3, 'eastern-corridor',
    'cinderblock-ai', 'Cinderblock AI', 'polygon',
    rect(42.295, 42.382, -83.128, -82.968)),

  t('nightshade-detroit', 'The Nightshade Detroit Campus', 'Nightshade', 1, 'eastern-corridor',
    'nightshade-pharmatech', 'Nightshade Pharmatech', 'polygon',
    rect(42.375, 42.435, -83.148, -83.060)),

  t('pale-lantern-quarter', 'The Lantern Quarter', 'Pale Lantern', 1, 'eastern-corridor',
    'pale-lantern-bioethics', 'Pale Lantern Bioethics', 'polygon',
    rect(42.342, 42.378, -83.098, -83.060)),

  // ── Detroit gray zones ────────────────────────────────────────────────────

  gz('detroit-metro-gap', 'The Detroit Metro Gap', 'wasteland',
    'Detroit Provisional Council — 300,000 residents, the largest ungoverned urban area.',
    rect(42.255, 42.490, -83.215, -82.908)),

  // ═══════════════════════════════════════════════════════════════════════════
  // EASTERN CORRIDOR Z12 — Cleveland / Lake Erie  (41.38 – 41.62)
  // ═══════════════════════════════════════════════════════════════════════════

  t('carrion-defense-yards', 'The Carrion Yards', 'Carrion', 3, 'eastern-corridor',
    'carrion-defense-works', 'Carrion Defense Works', 'polygon',
    rect(41.378, 41.622, -81.848, -81.555)),

  // ── Cleveland gray zones ──────────────────────────────────────────────────

  gz('cleveland-seam', 'The Cleveland Seam', 'seam',
    'Cleveland Civic Authority — 50,000 residents adjacent to Carrion exclusion zone.',
    rect(41.400, 41.558, -81.985, -81.840)),

  gz('toledo-gap', 'The Toledo Gap', 'wasteland',
    'Toledo Recovery Authority — remnant population of a drowned former port city.',
    rect(41.540, 41.700, -83.720, -83.490)),

  gz('lake-erie-fringe', 'The Lake Erie Fringe', 'seam',
    'Shoreline salvage and fishing communities. Lake Erie most contested water in east.',
    rect(41.600, 41.680, -82.010, -81.848)),

  // ═══════════════════════════════════════════════════════════════════════════
  // LAKE / SUBSURFACE
  // ═══════════════════════════════════════════════════════════════════════════

  t('bathysphere-deep-territories', 'The Bathysphere Deep Territories', 'Bathysphere', 3, 'subsurface',
    'bathysphere-networks', 'Bathysphere Networks', 'subsurface',
    rect(41.700, 43.200, -87.512, -86.692)),

  t('cormorant-platform-network', 'The Cormorant Platform Network', 'Cormorant', 1, 'subsurface',
    'cormorant-naval-systems', 'Cormorant Naval Systems', 'offshore',
    rect(43.005, 44.012, -87.512, -86.392)),

  // ── Subsurface gray zones ─────────────────────────────────────────────────

  gz('subsurface-seam-north', 'The Northern Subsurface Seam', 'seam',
    'No council. Bathysphere monitors via acoustic arrays.',
    rect(43.200, 44.100, -87.520, -87.300)),

  gz('subsurface-seam-south', 'The Southern Subsurface Seam', 'seam',
    'Contested — Bathysphere and Cinderfall both claim monitoring rights.',
    rect(41.600, 41.700, -87.560, -87.350)),

];

fs.mkdirSync(path.dirname(OUTPUT_PATH), { recursive: true });
fs.writeFileSync(OUTPUT_PATH, JSON.stringify(polygons, null, 2));

const territories = polygons.filter(x => x.type === 'territory');
const grayZones   = polygons.filter(x => x.type === 'grayzone');
console.log(`Generated ${polygons.length} polygons to ${OUTPUT_PATH}`);
console.log(`  Territories: ${territories.length}`);
console.log(`  Gray zones:  ${grayZones.length}`);
