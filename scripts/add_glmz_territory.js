#!/usr/bin/env node
// Adds glmzTerritory field to all CorpoNation JSON files.
// CorpoNations with no GLMZ presence are skipped (no field added).

const fs = require('fs');
const path = require('path');

const CORP_DIR = path.join(__dirname, '..', 'engine', 'data', 'CorpoNations');

// Full territory map. Key = exact CorpoNation name field.
const TERRITORY_MAP = {

  "Arcturus Defense Solutions": {
    zones: ["Z1"],
    primaryZone: "Z1",
    lakefrontAccess: false,
    description: "The Coldwall Quarter — former federal district south of the Chicago Loop, now Arcturus's primary GLMZ installation and home of the Civil Security Division. Provides contracted civil security across all Zone 1 sovereign territories; secondary monitoring presence in every zone via facility security contracts.",
    grayZoneRelationship: "Active perimeter monitoring on all Zone 1 Gray Zone margins. Does not patrol Gray Zone interiors except under CorpoNation contract."
  },

  "Ashford Signal": {
    zones: ["Z4", "Z1", "Z2", "Z3", "Z5", "Z6", "Z7", "Z8", "Z9", "Z10", "Z11", "Z12"],
    primaryZone: "Z4",
    lakefrontAccess: true,
    description: "Headquarters at the former Great Lakes Naval Station complex in Waukegan (Zone 4), plus 140 hardened relay stations distributed throughout every GLMZ zone. Sovereign territory is the 22-floor Ashford Pinnacle tower plus air rights to 400 meters above each relay station.",
    grayZoneRelationship: "Relay stations are positioned in both CorpoNation territory and Gray Zones. Gray Zone informal governance bodies do not challenge Ashford installations because losing the signal would cost them more than tolerating the presence."
  },

  "Ashgrave Materials": {
    zones: ["Z6", "Z11"],
    primaryZone: "Z6",
    lakefrontAccess: true,
    description: "The Ashgrave Synthesis Corridor — continuous industrial zone from South Chicago through Gary and Hammond along the southern Lake Michigan shore, extending east through Michigan City into the Zone 11 southern wrap. The largest single contiguous CorpoNation territory in the GLMZ by land area. Most polluted sovereign zone in the Corridor.",
    grayZoneRelationship: "Ashgrave territory is the lakefront industrial strip. The neighborhoods behind their facilities are Zone 6 Gray Zones — the most dangerous in the Corridor. Ashgrave maintains no formal relationship with these communities."
  },

  "Bathysphere Networks": {
    zones: ["Z∞"],
    primaryZone: "Z∞",
    lakefrontAccess: true,
    description: "The Bathysphere Deep Territories — 12,000 square kilometers of sovereign subsurface space beneath Lake Michigan, extending 40 meters below the lake bed. Bathysphere Hub primary installation sits 40 meters below the lake surface near the former Chicago lakefront. The sole sovereign entity of Zone ∞ and the most strategically critical infrastructure CorpoNation in the Corridor.",
    grayZoneRelationship: "Operates below-market access agreements with Gray Zone councils for subsurface data and power connectivity. The fees are modest; the dependency is absolute."
  },

  "Carrion Defense Works": {
    zones: ["Z12"],
    primaryZone: "Z12",
    lakefrontAccess: true,
    description: "The Carrion Yards — fortified industrial peninsula on the Lake Erie shore in the Cleveland tendril corridor. Produces autonomous defense platforms and hardened perimeter systems sold to both GLMZ CorpoNations and the independent Cleveland CorpoNation ecosystem.",
    grayZoneRelationship: "The Carrion Yards perimeter is aggressively defended by automated systems. Adjacent Gray Zone communities have learned not to test it."
  },

  "Charnel Propulsion": {
    zones: ["Z11"],
    primaryZone: "Z11",
    lakefrontAccess: false,
    description: "Sovereign industrial campus straddling the former Indiana-Michigan border in Zone 11's southern wrap. Specializes in propulsion systems for autonomous freight and military platforms. One of the largest independent employers in the Zone 11 corridor.",
    grayZoneRelationship: "Inland position creates extensive Gray Zone adjacency. Maintains a working labor-and-security arrangement with the St. Joseph Gray Zone Council."
  },

  "Cinderblock AI": {
    zones: ["Z12"],
    primaryZone: "Z12",
    lakefrontAccess: false,
    description: "The Cinderblock Campus — climate-controlled superstructure in the Detroit tendril corridor of Zone 12, housing the largest civilian AI substrate in the northern hemisphere. Six hardened Cold Nodes beneath the Great Lakes floor provide redundant processing infrastructure connected to the Bathysphere subsurface network.",
    grayZoneRelationship: "Detroit's Gray Zone communities have an uneasy relationship with Cinderblock — the AI substrate passively monitors everything within signal range, which covers most of the Detroit corridor."
  },

  "Cinderfall Energy": {
    zones: ["Z6", "Z∞"],
    primaryZone: "Z6",
    lakefrontAccess: false,
    description: "11 sovereign subterranean installations beneath the Zone 6 substrate, extending into Lake Michigan subsurface. Accesses geothermal and deep-water energy sources; controls the subsurface energy infrastructure for Zone 6 industrial operations. In active territorial dispute with Bathysphere Networks over subsurface boundary claims.",
    grayZoneRelationship: "Zone 6 Gray Zone communities access Cinderfall power through informal tap agreements that Cinderfall tolerates — the load is minimal and the political cost of enforcement exceeds the benefit."
  },

  "Copperveil Intelligence": {
    zones: ["Z3"],
    primaryZone: "Z3",
    lakefrontAccess: false,
    description: "The Veil Campus — fortified intelligence and behavioral analysis complex in the Evanston buffer zone of Zone 3. Sells CorpoNation intelligence to other CorpoNations; their neutral-zone position between Zone 1-2 power centers and Zone 4+ infrastructure allows independent operation without becoming a client of any single power.",
    grayZoneRelationship: "Pays premium rates for intelligence sourced from Gray Zone networks. Their most reliable assets are people the CorpoNations do not bother to watch."
  },

  "Cormorant Naval Systems": {
    zones: ["Z∞", "Z12"],
    primaryZone: "Z∞",
    lakefrontAccess: true,
    description: "Network of fortified offshore platforms and reclaimed lake infrastructure spanning the Great Lakes, with primary operations in Lake Michigan and Lake Erie (Zone 12 tendril). Manufactures and maintains the autonomous naval platforms that patrol CorpoNation maritime territories throughout the Corridor.",
    grayZoneRelationship: "No formal Gray Zone relationships. Operates exclusively in water."
  },

  "Crestfall Aquaculture": {
    zones: ["Z9"],
    primaryZone: "Z9",
    lakefrontAccess: true,
    description: "Platform farm network extending up to 15 kilometers from the Sheboygan shoreline, Zone 9. Largest aquaculture installation in the Great Lakes, producing approximately 60% of the Corridor's farmed fish and significant algae-derived protein supply.",
    grayZoneRelationship: "Supplies food to Zone 9 Gray Zone communities at below-market rates under bloc access agreements — a calculated investment in shoreline corridor goodwill."
  },

  "Crucible Genomics": {
    zones: ["Z6"],
    primaryZone: "Z6",
    lakefrontAccess: false,
    description: "Crucible Campus — reclaimed industrial land along the Calumet River in Zone 6. Genomic research and commercial gene therapy services for the GLMZ's industrial workforce. Zone 6 positioning is deliberate: their primary patient population is the workforce most exposed to Ashgrave and Slagworks industrial effluent.",
    grayZoneRelationship: "Provides discounted gene therapy services to Zone 6 Gray Zone residents. Medical access is leverage; Crucible maintains detailed records of which communities owe them."
  },

  "Dredge Mining Collective": {
    zones: ["Z7", "Z∞"],
    primaryZone: "Z7",
    lakefrontAccess: true,
    description: "Lake bed extraction operations from the Kenosha shoreline with sovereign extraction rights spanning sections of the lake bed from Zone 4's northern boundary through Zone 7. Surface headquarters at Kenosha's lakefront. Territorial overlap with Bathysphere Networks managed through revenue-sharing agreement.",
    grayZoneRelationship: "Employs Zone 7 Gray Zone labor on extraction platforms under short-term contracts. No formal relationship with Gray Zone governance structures."
  },

  "Emberlace Systems": {
    zones: ["Z7", "Z1", "Z2", "Z3", "Z4", "Z5", "Z6", "Z8", "Z9", "Z10", "Z11", "Z12"],
    primaryZone: "Z7",
    lakefrontAccess: false,
    description: "No consolidated physical territory. Distributed sensor network spanning the entire GLMZ — sovereign claim consists of the sensor installations plus a 10-meter exclusion radius around each. Headquarters in Kenosha interior, Zone 7. Sells environmental, structural, and atmospheric data to all GLMZ CorpoNations simultaneously, maintaining technical neutrality that has survived three major CorpoNation conflicts.",
    grayZoneRelationship: "Sensor arrays blanket Gray Zones as thoroughly as CorpoNation territories. Emberlace does not share Gray Zone sensor data with CorpoNations without Gray Zone council consent — this policy has been tested twice in arbitration and held both times."
  },

  "Fascia Global": {
    zones: ["Z5", "Z1", "Z2", "Z3", "Z4", "Z6", "Z7", "Z8"],
    primaryZone: "Z5",
    lakefrontAccess: false,
    description: "The Hyperlane Rights-of-Way — sovereign linear territories following major arterial freight routes through the southern GLMZ, analogous to Ferrogate's rail corridor sovereignty applied to surface and air freight lanes. Fascia controls the physical infrastructure of the GLMZ's surface freight network from Zone 1 through Zone 8.",
    grayZoneRelationship: "Gray Zones frequently encroach on Hyperlane margins. Fascia enforces exclusion zones with automated systems rather than personnel."
  },

  "Ferrogate Transit": {
    zones: ["Z5", "Z1", "Z2", "Z3", "Z4", "Z6", "Z7", "Z8", "Z9", "Z10", "Z11", "Z12"],
    primaryZone: "Z5",
    lakefrontAccess: false,
    description: "The Ferrogate Corridor — 1,400 kilometers of sovereign right-of-way encompassing all operated rail lines plus 50-meter exclusion zones on each side, threading through every GLMZ zone. Primary hub at the former O'Hare International Airport (now GLMZ Interzone Freight Exchange) and Union Station. The only CorpoNation in the Corridor whose territory is formally topological rather than geographic.",
    grayZoneRelationship: "Rail lines pass through Gray Zones without stopping — the corridor is sovereign regardless of what surrounds it. Gray Zone communities near Ferrogate lines use proximity for unofficial loading, which Ferrogate tolerates below a volume threshold they have never published."
  },

  "Gravemoss Biofoundry": {
    zones: ["Z8"],
    primaryZone: "Z8",
    lakefrontAccess: true,
    description: "The Ferment Quarter — 14-square-kilometer wetland-industrial zone on the southern Milwaukee lakefront, Zone 8. Experimental biotechnology research; one of the few CorpoNation laboratories in the GLMZ that publishes research findings. Maintains formal collaboration agreements with Vellichor Institute.",
    grayZoneRelationship: "The Ferment Quarter's edges are permeable by Zone 8 standards. Gravemoss recruits from Milwaukee Gray Zone communities — unusual enough to be noteworthy."
  },

  "Helix Biosystems": {
    zones: ["Z2"],
    primaryZone: "Z2",
    lakefrontAccess: true,
    description: "Streeterville campus on the former Northwestern University Medical campus, Zone 2 — the largest biomedical research installation in the Corridor. Blue exterior lighting is a sovereign trademark. 11 chartered zones globally; GLMZ primary is Zone 2. Actively monitors the Corridor for unlicensed biotech adoption.",
    grayZoneRelationship: "Runs periodic 'health initiative' operations in Zone 2 and Zone 3 Gray Zones. The medical services are genuine. The biotech surveillance embedded in those operations is also genuine."
  },

  "Ironclad Agrisystems": {
    zones: ["Z8", "Z9", "Z10"],
    primaryZone: "Z8",
    lakefrontAccess: false,
    description: "Regional food distribution headquarters in Milwaukee, Zone 8. Primary sovereign territory is the Iowa Exclusion Zone (approximately 145,000 square kilometers); GLMZ operations are the distribution end of the supply chain — Ironclad controls the Zone 8-10 food distribution network from Milwaukee's processing facilities to northern Corridor distribution points.",
    grayZoneRelationship: "Has twice restricted food distribution to Gray Zone communities in Zones 8-10 to force negotiating concessions. Both incidents are documented in Gray Zone governance archives. Neither was legally actionable. Both succeeded."
  },

  "Irontide Tidal Energy": {
    zones: ["Z9", "Z10"],
    primaryZone: "Z9",
    lakefrontAccess: true,
    description: "Irontide Anchor Platform — primary floating sovereign installation anchored 4.7 kilometers off the Wisconsin shoreline, Zone 9, with additional platforms distributed through Zone 10 Door Peninsula waters. Lake current generation sold to Zone 8-10 CorpoNations and Gray Zone councils.",
    grayZoneRelationship: "Zone 9 Gray Zone communities access Irontide power at the same pricing tier as small CorpoNations. The generosity is strategic: hostile Gray Zone communities would interfere with shore access corridors."
  },

  "Kelpline Logistics": {
    zones: ["Z9", "Z8", "Z10"],
    primaryZone: "Z9",
    lakefrontAccess: true,
    description: "Coastal freight distribution network operating shallow-draft vessels along the Zone 8-10 shoreline — the Zone 9 equivalent of Ferrogate's rail network. Sovereign territory consists of the vessels and their documented routes plus berthing rights at 34 recognized ports.",
    grayZoneRelationship: "Delivers to Gray Zone coastal communities on the same schedule as CorpoNation clients, at higher rates. The premium is not negotiable. Neither is the access."
  },

  "Lacuna Genomics": {
    zones: ["Z4"],
    primaryZone: "Z4",
    lakefrontAccess: false,
    description: "Lacuna Campus — fortified biozone in the North Shore suburbs of Zone 4, built atop former industrial land. Closed-loop genomic research with no published outputs. Lacuna's research direction is not publicly known, which is unusual enough to generate both industry speculation and Helix Biosystems institutional hostility.",
    grayZoneRelationship: "No documented Gray Zone relationships."
  },

  "Lazarus Pharmaceuticals": {
    zones: ["Z3"],
    primaryZone: "Z3",
    lakefrontAccess: false,
    description: "The Lazarus Compound — pharmaceutical manufacturing and research enclave in the Evanston buffer zone of Zone 3. Specializes in longevity therapeutics and cellular regeneration compounds. Zone 3 positioning provides access to Vellichor Institute research pipelines while maintaining distance from Zone 1-2 competitor surveillance.",
    grayZoneRelationship: "Runs clinical trials in Zone 3 Gray Zone communities. Compensation is above market; consent documentation is comprehensive; outcomes are not shared with participants."
  },

  "Liang-Petrova Consortium": {
    zones: ["Z7"],
    primaryZone: "Z7",
    lakefrontAccess: true,
    description: "GLMZ operations centered on the Racine port complex, Zone 7 — petrochemical processing and distribution hub for the northern Corridor. Primary sovereign territory is the Shanghai-Vladivostok Free Economic Zone; the Racine installation is the Consortium's primary Western Hemisphere operation, reflecting the founding families' need for Great Lakes access to the global supply chain.",
    grayZoneRelationship: "Proximity to the Zone 7 Gray Zone interior has produced informal labor arrangements that the Consortium's founding families view as pragmatic and Zone 1 CorpoNations view as a governance failure."
  },

  "Libation Corporation": {
    zones: ["Z1", "Z2", "Z3", "Z4", "Z5", "Z6", "Z7", "Z8", "Z9", "Z10", "Z11", "Z12"],
    primaryZone: "Z1",
    lakefrontAccess: false,
    description: "No sovereign territory. Operates under host-jurisdiction licensing throughout the entire GLMZ and beyond. Fine Feasts (Tier 4), Good Eats (Tier 3), Chow Trough (Tier 2), and Eat it! (Tier 1) outlets are present in every zone of the Corridor. The brand you eat at is a daily reinscription of your position in the social order.",
    grayZoneRelationship: "Eat it! dispensaries operate in Gray Zones under no-territory fee-for-access arrangements with local informal governance. They are frequently vandalized. They are always restocked."
  },

  "Marrowvault Cryogenics": {
    zones: ["Z5"],
    primaryZone: "Z5",
    lakefrontAccess: false,
    description: "The Marrowvault Preserve — vast underground sovereign facility beneath the western Chicago suburbs of Zone 5. Long-term biological preservation for CorpoNation executives and high-tier citizens. The most secure and least visible sovereign territory in the GLMZ — most Zone 5 residents above it have no idea it exists.",
    grayZoneRelationship: "No interface with Gray Zone communities. The Marrowvault's surface footprint is a parking structure."
  },

  "Mirrorwell Media": {
    zones: ["Z1"],
    primaryZone: "Z1",
    lakefrontAccess: false,
    description: "The Mirrorwell Arcology — 62-story broadcast and residential tower in Chicago's River North district, Zone 1. Produces the GLMZ's second-most-watched content slate after Waxwing Neuromedia. Mirrorwell's Zone 1 position is contested — Waxwing has attempted acquisition four times.",
    grayZoneRelationship: "Mirrorwell crews enter Gray Zones for content production. Their footage constitutes the primary visual record of Gray Zone life that Tier 4-5 citizens ever see."
  },

  "Nightshade Pharmatech": {
    zones: ["Z12"],
    primaryZone: "Z12",
    lakefrontAccess: false,
    description: "Nightshade Campus — 9-square-kilometer pharmaceutical research facility embedded within the Detroit Reclamation Zone of Zone 12's Michigan tendril corridor. Operates in Detroit's regulatory ambiguity, conducting research that Zone 1-3 CorpoNation governance would not permit.",
    grayZoneRelationship: "Detroit's Gray Zone communities are Nightshade's primary clinical test population. The relationship is transactional and not voluntary in any meaningful sense."
  },

  "Novafold Pharmaceuticals": {
    zones: ["Z2"],
    primaryZone: "Z2",
    lakefrontAccess: true,
    description: "The Novafold Medical Sovereign Zone — anchored by the Novafold Grand Campus in Lincoln Park, Zone 2. Zone 2's planned pharmaceutical residential district: employee housing, research facilities, and hospitality centers occupy a continuous enclave designed to demonstrate what a city looks like when it functions.",
    grayZoneRelationship: "Novafold's campus edges are hard borders. The contrast between the campus interior and the Argyle Street Gray Zone immediately adjacent is a recurring subject of Mirrorwell and Vantablack content."
  },

  "Oracle Drift Systems": {
    zones: ["Z4"],
    primaryZone: "Z4",
    lakefrontAccess: false,
    description: "No contiguous physical sovereign territory. Algorithmic trading infrastructure distributed through secured facilities in the Highland Park district of Zone 4. Functions as the GLMZ's disaster-recovery financial market and secondary exchange, activated when Zone 1 financial infrastructure is disrupted.",
    grayZoneRelationship: "Oracle's facilities are invisible from the street. No Gray Zone relationship."
  },

  "Ouroboros Energy": {
    zones: ["Z8", "Z7", "Z9"],
    primaryZone: "Z8",
    lakefrontAccess: false,
    description: "The Ouroboros Ring — continuous sovereign energy corridor running the Zone 7-9 power infrastructure, centered on the Milwaukee Menomonee River campus. Controls power distribution for Zones 7, 8, and 9. Every CorpoNation in these zones pays Ouroboros for electricity; the leverage this creates is Ouroboros's primary political instrument.",
    grayZoneRelationship: "Gray Zone communities in Zones 7-9 access Ouroboros power through informal tap agreements that Ouroboros monitors but does not enforce against, because the load is trivial and the political cost of enforcement is not."
  },

  "Pale Lantern Bioethics": {
    zones: ["Z12"],
    primaryZone: "Z12",
    lakefrontAccess: false,
    description: "Lantern Quarter — 0.8-square-kilometer neutral-zone enclave in the Detroit tendril corridor of Zone 12. Provides independent bioethics review services for CorpoNation research programs seeking ethical certification without Zone 1-3 regulatory scrutiny. The certification is genuine. The regulatory environment that makes the distinction meaningful is not.",
    grayZoneRelationship: "Occasionally represents Gray Zone communities in disputes with CorpoNations over research practices. This is genuinely unusual and makes Pale Lantern enemies in Zone 1."
  },

  "Palladian Construction": {
    zones: ["Z6", "Z11"],
    primaryZone: "Z6",
    lakefrontAccess: false,
    description: "Palladian Prime — sovereign industrial zone in the Gary, Indiana ruins and extending into Zone 11's southern wrap. Materials warehousing, fabrication facilities, and the largest crane fleet in the Corridor. Palladian built most of the GLMZ's CorpoNation facilities during the 2150-2190 construction surge and retained the territory they built on.",
    grayZoneRelationship: "Palladian's Gary territory borders some of Zone 6's most dangerous Gray Zones. They maintain a private security force of 2,400 personnel specifically for perimeter enforcement."
  },

  "Pelican Drift Aquatics": {
    zones: ["Z9"],
    primaryZone: "Z9",
    lakefrontAccess: true,
    description: "The Drift Yards — 34 semi-permanent floating platforms anchored along the Zone 9 coastline, plus exclusive rights to several hundred square kilometers of undeveloped shoreline maintained as managed conservation zone. Commercial fishing licensing, marine survey, and sustainable harvest management for the northern lake zone.",
    grayZoneRelationship: "Zone 9 coastal Gray Zone communities hold traditional fishing rights predating Pelican Drift's sovereign claims. Legal status is unresolved; the practical accommodation is that Pelican Drift does not enforce against subsistence-scale fishing."
  },

  "Pellucid Systems": {
    zones: ["Z3"],
    primaryZone: "Z3",
    lakefrontAccess: true,
    description: "The Pellucid Atrium — 12-square-kilometer campus in the Rogers Park lakefront district of Zone 3. Predictive analytics and behavioral modeling, specializing in Gray Zone market data. Pellucid knows what happens between sovereign territories and sells that knowledge to anyone with the Quanta to pay.",
    grayZoneRelationship: "Pellucid's primary product is information about Gray Zones. They pay Gray Zone residents for behavioral data through a distributed micro-payment system that most participants do not fully understand they have enrolled in."
  },

  "Rendstone Nuclear": {
    zones: ["Z10"],
    primaryZone: "Z10",
    lakefrontAccess: true,
    description: "The Rendstone Exclusion Corridor — 180-square-kilometer sovereign zone surrounding the Kewaunee nuclear installation on Lake Michigan's western shore south of Green Bay, Zone 10. Provides approximately 34% of Zone 10's power generation and sells surplus capacity south into Zone 9 and east into the Michigan tendril.",
    grayZoneRelationship: "The exclusion zone has no Gray Zone adjacency by design. Rendstone's perimeter enforcement is automated; the company has never publicly disclosed what the automated systems do to incursions."
  },

  "Rictus Entertainment": {
    zones: ["Z2"],
    primaryZone: "Z2",
    lakefrontAccess: false,
    description: "The Rictus Pleasure Corridor — 14-kilometer strip of sovereign entertainment territory in the Lakeview district, Zone 2, anchored by the Wrigley Field Entertainment Complex. The GLMZ's dominant entertainment CorpoNation engineers spaces that feel open and permissive while maintaining comprehensive surveillance. The feeling of permission is the product.",
    grayZoneRelationship: "Operates content-gathering operations in Gray Zones that feed their 'authentic experience' product lines. Gray Zone residents are frequently unaware they are being filmed for commercial distribution."
  },

  "Ringo CorpoNation": {
    zones: ["Z4", "Z1", "Z2", "Z3", "Z8"],
    primaryZone: "Z4",
    lakefrontAccess: false,
    description: "28 chartered zones globally; GLMZ presence concentrated in Zone 4's Northern Operations augmentation service corridor, Zone 1 financial and executive services, and Zone 8 Milwaukee distribution. Ringo's GLMZ operations function as a consumer access corridor for augmentation and transit services rather than a primary territorial holding.",
    grayZoneRelationship: "Ringo augmentation service centers at Zone 4's fringes accept Tier 2 walk-in clients — functionally serving the better-resourced Gray Zone residents. These centers are the most accessible point of the CorpoNation system for Gray Zone populations in Zones 3-5."
  },

  "Saltmarsh Telecom": {
    zones: ["Z4", "Z1", "Z2", "Z3", "Z5", "Z6", "Z7", "Z8", "Z9", "Z10", "Z11", "Z12"],
    primaryZone: "Z4",
    lakefrontAccess: true,
    description: "Waukegan lakefront relay hub plus 4,400 sovereign signal zones distributed throughout the GLMZ. Controls approximately 67% of data traffic north of Zone 2. Primary headquarters at the Waukegan lakefront complex; relay architecture threads through every zone in the Corridor.",
    grayZoneRelationship: "Provides limited bandwidth to Gray Zone communities at steep markup through reseller agreements with Gray Zone councils. The bandwidth is real. The 'limited' is adjustable based on political circumstances."
  },

  "Scoria Works": {
    zones: ["Z6"],
    primaryZone: "Z6",
    lakefrontAccess: true,
    description: "The Crucible Belt — 14-kilometer industrial strip along the Gary, Indiana lakefront, Zone 6. Heavy fabrication and raw materials processing. In chronic territorial dispute with Ashgrave Materials over Calumet lakefront access rights; eleven formal arbitrations in thirty years have produced eleven agreements neither party has honored.",
    grayZoneRelationship: "No formal Gray Zone relationships. Scoria's perimeter security is less sophisticated than Ashgrave's and generates more Gray Zone incidents as a result."
  },

  "Silkworm Data": {
    zones: ["Z8"],
    primaryZone: "Z8",
    lakefrontAccess: true,
    description: "Climate-controlled arcology tower cluster on the Milwaukee lakefront, Zone 8. Data storage, processing, and logistics for the northern Corridor. Silkworm grew from local Milwaukee capital during the city's industrial data infrastructure buildout — one of the few Zone 8 CorpoNations that was not assembled by Chicago expansion.",
    grayZoneRelationship: "Provides data storage services to Zone 8 Gray Zone mutual organizations at cost — the most significant example of cooperative CorpoNation-Gray Zone infrastructure in the northern Corridor."
  },

  "Slagworks Industrial": {
    zones: ["Z6"],
    primaryZone: "Z6",
    lakefrontAccess: true,
    description: "The Slagworks Foundry Belt — continuous sovereign industrial zone spanning the South Chicago port district, Zone 6. Processes and recycles industrial byproducts from Ashgrave Materials and Scoria Works. Exists in a dependency relationship with its Zone 6 neighbors — they process what Ashgrave and Scoria cannot or will not.",
    grayZoneRelationship: "The South Chicago Gray Zones surrounding Slagworks are among the most environmentally compromised in the Corridor. Slagworks has no documented relationship with these communities."
  },

  "Stonepath Logistics": {
    zones: ["Z5"],
    primaryZone: "Z5",
    lakefrontAccess: false,
    description: "The Stonepath Transit Sovereignty — recognized infrastructure sovereignty over the former O'Hare International Airport and adjacent freight corridors in Zone 5. Handles approximately 40% of zone-wide imports arriving by air; their sovereign territory is the primary point of entry for goods from outside the GLMZ footprint.",
    grayZoneRelationship: "Zone 5 Gray Zones adjacent to Stonepath freight corridors are active gray-market access points. Stonepath maintains this is not their concern. The volume of gray-market throughput suggests otherwise."
  },

  "Sulfur Crown Agriculture": {
    zones: ["Z8", "Z9", "Z10"],
    primaryZone: "Z8",
    lakefrontAccess: false,
    description: "The Crown Territories — discontinuous network of 23 agricultural and remediation zones across Zones 8-10. Former industrial brewing infrastructure converted to large-scale fermentation-based food production using the same equipment. Zone 8 sovereign territory is the most densely populated outside of Chicago, because industrial food production at scale requires human workers.",
    grayZoneRelationship: "Employs Gray Zone labor from Zones 8-10 on short-term contracts that are technically renewable and practically permanent."
  },

  "Tessera CorpoNation": {
    zones: ["Z1"],
    primaryZone: "Z1",
    lakefrontAccess: true,
    description: "18 chartered zones globally; GLMZ primary is the sovereign enclave centered on the former Millennium Park site extending north to the Chicago River, Zone 1. The Loop's premier address. Home to the Governance Consortium administrative chambers and the Grand Exchange financial market. Tessera sets the aesthetic and commercial standard for the Corridor and expects the Corridor to know it.",
    grayZoneRelationship: "Tessera has no Gray Zone relationship. The concept does not appear in their internal documentation except as a category of risk to be managed."
  },

  "Thornback Agrichemical": {
    zones: ["Z10"],
    primaryZone: "Z10",
    lakefrontAccess: true,
    description: "The Thornback Basin — 4,200 square kilometers of sovereign agricultural and industrial territory across the Door Peninsula and Fox River Valley, Zone 10. Agricultural chemistry, soil science, and specialty compound production derived from northern Great Lakes microbial ecosystems. Thornback maintains the Door Peninsula's farming communities as a managed resource under long-term supply agreements.",
    grayZoneRelationship: "Door Peninsula rural communities exist in an ambiguous relationship with Thornback that resembles CorpoNation tenancy without formal tier classification. Thornback prefers the ambiguity; it costs less than the alternative."
  },

  "Tollgate Systems": {
    zones: ["Z1", "Z2", "Z3", "Z4", "Z5"],
    primaryZone: "Z1",
    lakefrontAccess: false,
    description: "Distributed sovereign access-control infrastructure throughout the southern GLMZ. Tollgate holds sovereign rights-of-way at key transit choke points and levies access fees that function as informal taxation without the political liabilities of formal governance. No contiguous territory; control exercised through infrastructure rather than land.",
    grayZoneRelationship: "Tollgate systems at Gray Zone entry points are the primary physical manifestation of CorpoNation authority that most Gray Zone residents encounter in daily movement."
  },

  "Vantablack Media": {
    zones: ["Z2"],
    primaryZone: "Z2",
    lakefrontAccess: true,
    description: "The Vantablack Spire — Chicago Lakeshore Sector 7 in northern Zone 2, plus seventeen embedded broadcast installations throughout the Corridor. Produces the GLMZ's most politically aggressive content slate. Vantablack's adversarial positioning relative to Waxwing and Mirrorwell is deliberate — their audience exists because they show what the other two will not.",
    grayZoneRelationship: "Vantablack Gray Zone coverage is the most extensive and least exploitative of any media CorpoNation in the GLMZ. This is their brand as much as their ethics."
  },

  "Vellichor Institute": {
    zones: ["Z3"],
    primaryZone: "Z3",
    lakefrontAccess: true,
    description: "The Vellichor Campus — sprawling lakeside research enclave on the northern Chicago shoreline in Evanston, Zone 3. Occupies the former Northwestern University site. One of three functioning university-sovereigns in the Corridor. Admission by examination; residency by affiliation; Tier classification formally irrelevant within campus boundaries.",
    grayZoneRelationship: "Admits Gray Zone applicants at the same examination standard as all others. The Tier 1 acceptance rate is documented at 0.003%. The Institute considers this proof of meritocracy."
  },

  "Verdant Systems": {
    zones: ["Z10"],
    primaryZone: "Z10",
    lakefrontAccess: true,
    description: "The Verdant Canopy Zones — six legally sovereign atmospheric management districts in the Green Bay metropolitan region, Zone 10. Controls data infrastructure for the northern GLMZ and operates the sensor network extending into the northwoods wilderness beyond the Corridor boundary. Verdant's towers are the tallest structures in Zone 10.",
    grayZoneRelationship: "The Green Bay Mutual — Zone 10's largest Gray Zone governance body — has a formal data-sharing agreement with Verdant: sensor coverage in exchange for recognized governance status within Verdant's atmospheric management zones."
  },

  "Vespid Dynamics": {
    zones: ["Z6", "Z11"],
    primaryZone: "Z6",
    lakefrontAccess: false,
    description: "Vespid Arcology Cluster — network of hardened research and manufacturing facilities in the eastern Gary Sprawl and Zone 11 Indiana Corridor. Aerospace and autonomous systems; positioned as a lower-profile Arcturus competitor. Zone 6-11 positioning keeps Vespid outside Zone 2's Arcturus Civil Security monitoring range.",
    grayZoneRelationship: "Vespid facilities are heavily hardened. Gray Zone communities in the adjacent Indiana corridor are aware of where the perimeter is."
  },

  "Waxwing Neuromedia": {
    zones: ["Z1"],
    primaryZone: "Z1",
    lakefrontAccess: false,
    description: "Waxwing Spire District — 3.2-square-kilometer enclave in central Chicago's former Magnificent Mile corridor, Zone 1. The most-watched content in the Corridor is produced within four blocks of the old Water Tower. Waxwing understands that its environment is its product and maintains the most aesthetically controlled territory in the GLMZ.",
    grayZoneRelationship: "Waxwing does not cover Gray Zones and does not hire from them. Their content presents a GLMZ in which Gray Zones are not the primary reality of the majority of the population."
  },

  "Zheng-dao Bioelectric": {
    zones: ["Z1", "Z8"],
    primaryZone: "Z1",
    lakefrontAccess: false,
    description: "22 chartered zones globally; GLMZ presence through Zone 1 financial and commercial operations and a Zone 8 Milwaukee bioelectric research campus. The GLMZ Behavioral Cognitive Futures Market (ZCFM) — one of three major behavioral prediction exchanges alongside Tessera's TBX and Arcturus's ATPM — operates from the Zone 1 installation.",
    grayZoneRelationship: "Zheng-dao's behavioral futures market derives significant predictive value from Gray Zone population behavior. Data is acquired through Pellucid Systems resale agreements."
  }

};

// Load, patch, save
const files = fs.readdirSync(CORP_DIR).filter(f => f.endsWith('.json'));
let patched = 0;
let skipped = 0;

for (const file of files) {
  const filePath = path.join(CORP_DIR, file);
  const data = JSON.parse(fs.readFileSync(filePath, 'utf8'));
  const name = data.name;

  if (!name || !TERRITORY_MAP[name]) {
    skipped++;
    continue;
  }

  data.glmzTerritory = TERRITORY_MAP[name];
  fs.writeFileSync(filePath, JSON.stringify(data, null, 2), 'utf8');
  patched++;
  console.log(`  ✓ ${name} → ${TERRITORY_MAP[name].primaryZone}`);
}

console.log(`\nDone. ${patched} CorpoNations patched, ${skipped} skipped (no GLMZ presence).`);
