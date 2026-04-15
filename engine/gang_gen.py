"""
gang_gen.py - StreetSamurai gang data generation and character assignment
Steps:
  1. Find all 21 existing gang files
  2. Create 3 new gang files
  3. Update all 24 gang files with rivals/allies + size_class
  4. Scan characters and assign to gangs (10% cap = 121 max)
"""

import json
import os
import uuid
import random

PYTHON = "/c/Users/ryand/AppData/Local/Programs/Python/Python311/python"
FACTIONS_DIR = "D:/Projects/MindAttic/StreetSamurai/engine/data/factions"
PEOPLE_DIR = "D:/Projects/MindAttic/StreetSamurai/engine/data/people"

random.seed(42)

# ── Step 1: Find existing gang files ──────────────────────────────────────────

GANG_NAMES_21 = [
    "The 92nd Street Kings", "The Bone Parish", "The Burnside Guard",
    "The Causeway Collective", "The Coffin Nails", "The Erie Remnant",
    "The Fathom Line", "The Gauze", "The Interchange", "The Lakebed Scrapers",
    "The Last Mile", "The Neon Vipers", "The Pure Hand", "The Reclaimed",
    "The Shore Dogs", "The Siphon Collective", "The Third Rail", "The Undertow",
    "The Volt Runners", "The Voltage Saints", "Switchblade Alley"
]

gang_registry = {}  # name -> {path, id, data}

for fname in os.listdir(FACTIONS_DIR):
    if not fname.endswith('.json'):
        continue
    fpath = os.path.join(FACTIONS_DIR, fname)
    try:
        with open(fpath, 'r', encoding='utf-8') as f:
            data = json.load(f)
        name = data.get('name', '')
        if name in GANG_NAMES_21:
            gang_registry[name] = {'path': fpath, 'id': data.get('id', ''), 'data': data}
            print(f"[FOUND] {name} -> {fname}")
    except Exception as e:
        print(f"[SKIP] {fname}: {e}")

print(f"\nFound {len(gang_registry)}/{len(GANG_NAMES_21)} existing gangs")
missing = [n for n in GANG_NAMES_21 if n not in gang_registry]
if missing:
    print(f"Missing: {missing}")

# ── Step 2: Create 3 new gang files ───────────────────────────────────────────

def new_id():
    return uuid.uuid4().hex

GLASS_LADDER_ID = new_id()
FERMENT_ID = new_id()
NULL_WARD_ID = new_id()

print(f"\n[NEW] Glass Ladder ID: {GLASS_LADDER_ID}")
print(f"[NEW] The Ferment ID: {FERMENT_ID}")
print(f"[NEW] The Null Ward ID: {NULL_WARD_ID}")

glass_ladder = {
    "id": GLASS_LADDER_ID,
    "type": "faction",
    "name": "The Glass Ladder",
    "aliases": ["The Ladder", "Glass"],
    "motto": "Every secret has a price. Ours is reasonable.",
    "description": "A mid-sized gang operating in the corporate corridor of the Meridian Hub and adjacent arcology complexes. The Glass Ladder specializes in corporate espionage, blackmail, and industrial intelligence brokerage. They are not killers — they are leverage artists. The gang recruits from disgraced corporate middle management, washed-out BCI analysts, and corporate security contractors who realized their skills were transferable. Members typically pass as legitimate professionals. Most have clean records and maintain cover employment. The Glass Ladder's product is information: security schedules, biometric access data, executive communications, off-books financial flows. They sell to competitors, regulators they control, or directly back to the victimized corporation at a premium. Their violence is surgical and contracted out — they hire The Null Ward for anything requiring a physical presence. The gang's vulnerability is their internal information. Anyone who fully understands the network's scope could destroy it. This has made them pathologically compartmentalized and paranoid about their own people.",
    "ideology": "Leverage is power. Violence is crude. Information extracted cleanly and sold quietly beats any street operation. They are mercenary about politics and personally amoral about their clients.",
    "territory": "Chicago's Meridian Hub corporate corridor and the adjacent arcology complexes. No fixed street territory — they operate in offices, server rooms, and executive suites. Three rotating safe houses in commercial districts, changed on 90-day cycles.",
    "leadership": "",
    "methods": [
        "Corporate espionage — infiltrating target organizations through planted employees",
        "Blackmail — the long game, collecting leverage over years",
        "Data brokerage — selling corporate intelligence to competing interests",
        "Social engineering — impersonation, deep cover, relationship harvesting",
        "Subcontracting violence to specialists rather than maintaining muscle internally"
    ],
    "resources": "Extensive network of corporate informants. Multiple false identity packages. BCI extraction tools. Archived leverage on approximately 200 mid-to-senior corporate executives across the GLMZ.",
    "goals": "Become the GLMZ's premier intelligence brokerage. Expand into political leverage beyond pure corporate targets. Remain invisible.",
    "size_class": "medium",
    "relationships": [
        {"name": "The Pure Hand", "type": "rival", "reason": "The Pure Hand views corporate espionage as an extension of corporate corruption and treats Ladder operatives as ideological enemies"},
        {"name": "The Null Ward", "type": "rival", "reason": "Competing for corporate security and counter-intelligence contracts; the Ward resents being hired out as muscle"},
        {"name": "The Coffin Nails", "type": "ally", "reason": "The Glass Ladder purchases biometric and augmentation data from the Nails for use as blackmail leverage"},
        {"name": "The Volt Runners", "type": "ally", "reason": "Tech infrastructure support partnership; the Runners provide vertical access the Ladder needs for server-room operations"}
    ],
    "narrative_function": "Source of corporate secrets and leverage. Can be hired by players or be a complicating factor in corporate storylines. Their blackmail archives are a worldbuilding treasure.",
    "story_hooks": [
        "A Glass Ladder operative approaches the players with a file on someone they know",
        "Someone in the Glass Ladder's archive wants their file destroyed — violently if necessary",
        "The gang accidentally acquired information that implicates one of their own backers"
    ],
    "tags": ["faction", "gang", "espionage", "corporate", "chicago", "blackmail", "intelligence"],
    "related_entities": [],
    "known_members": []
}

ferment = {
    "id": FERMENT_ID,
    "type": "faction",
    "name": "The Ferment",
    "aliases": ["The Kitchen", "Ferment"],
    "motto": "It's not illegal if they haven't named it yet.",
    "description": "The Ferment is a dispersed network of underground chemists, brewers, and pharmacologists operating across the GLMZ's industrial belt — particularly Gary (Indiana), the Detroit chemical corridor, and the ruins of the old Milwaukee Port Authority. The gang doesn't look like a gang. Its members present as small-batch brewers, gray-market pharmaceutical distributors, DIY biology hobbyists, and industrial chemical salvagers. The Ferment's actual business is the synthesis and distribution of compounds that exist in regulatory gray zones: synthetic food compounds that skirt pharma classification, tailored biochemicals for underground augmentation, and unlicensed pharmaceutical alternatives to corporate-controlled medicine. The gang's organizational structure is deliberately loose. There is no central leadership — only a network of chemist cells, each operating independently, all sharing synthesis protocols through encrypted relay. A cell can be destroyed without compromising the others. Their product reaches the street through legitimate-seeming small businesses: kombucha bars, gray-market supplement shops, food service operations. The Ferment is one of the primary suppliers of affordable pharmaceutical alternatives to the GLMZ's working poor.",
    "ideology": "Chemistry is liberation. Corporate pharma holds people hostage through patent control. The Ferment believes in accessible chemistry and the right of anyone to synthesize what their body needs.",
    "territory": "Distributed industrial zones. Primary nodes in Gary (Indiana), Detroit's chemical corridor south of the ruins belt, and Milwaukee's collapsed port district. No street territory claimed.",
    "leadership": "",
    "methods": [
        "Synthesis networks — decentralized production cells sharing protocols",
        "Gray-market distribution — through food service fronts and supplement shops",
        "Protocol sharing — encrypted synthesis documentation passed between affiliated chemists",
        "Corporate formula reversal — analyzing and reproducing proprietary pharmaceutical compounds"
    ],
    "resources": "Distributed synthesis equipment hidden across multiple locations. Extensive knowledge base of gray-zone chemistry. Supply relationships with industrial chemical salvagers.",
    "goals": "Make pharmaceutical independence possible for the GLMZ's working population. Expand synthesis capabilities into areas currently dominated by corporate pharma. Never become centralized enough to be decapitated.",
    "size_class": "medium",
    "relationships": [
        {"name": "The Gauze", "type": "rival", "reason": "Both supply medical and pharmaceutical needs to the same underserved population; the Gauze resents the Ferment's cheaper unlicensed alternatives eroding their client base"},
        {"name": "The Pure Hand", "type": "rival", "reason": "The Ferment's chemical liberation ideology clashes directly with the Pure Hand's purity doctrine — both see the other as a fundamental threat"},
        {"name": "The Reclaimed", "type": "ally", "reason": "Shared DIY, anti-corporate ethos and territory overlap in Detroit; the Ferment supplies pharmaceutical alternatives to Reclaimed communities"},
        {"name": "Switchblade Alley", "type": "ally", "reason": "Distribution network through Shelf maintenance corridors; the Alley moves Ferment product to markets neither can reach alone"},
        {"name": "The Bone Parish", "type": "ally", "reason": "The Ferment provides biochemical compounds for Parish surgical operations; neither side discusses the specifics"}
    ],
    "narrative_function": "Source of affordable chemicals and pharmaceuticals. Can be a quest giver (need a specific compound synthesized) or a target (corporate pharma wants them destroyed). Their ideology creates interesting moral tension.",
    "story_hooks": [
        "A Ferment cell has synthesized something they don't fully understand",
        "Corporate pharma has identified a synthesis node and hired contractors to shut it down",
        "The Ferment needs a specific industrial precursor that's been locked behind corporate supply chains"
    ],
    "tags": ["faction", "gang", "chemistry", "pharmaceutical", "gary", "detroit", "milwaukee", "distributed", "anti-corpo"],
    "related_entities": [],
    "known_members": []
}

null_ward = {
    "id": NULL_WARD_ID,
    "type": "faction",
    "name": "The Null Ward",
    "aliases": ["The Ward", "Null"],
    "motto": "Professional. Confidential. Final.",
    "description": "The Null Ward is what happens when a corporate security contractor goes out of business but its employees don't. Twelve years ago, a mid-tier corporate security firm called Hexagram Protective Services collapsed after a contract dispute with their primary client left them holding unpayable liability. The thirty-two employees who were still on payroll when the accounts froze never received severance. They had no legitimate market to re-enter — corporate security blacklists move fast. What they had was training, equipment, and each other. The Null Ward formed in the months afterward, offering their services on the gray market: executive protection, facility security assessment, and what the contract called 'problem resolution.' Today the gang numbers slightly under thirty full members, plus a variable number of contract associates. They are the most professionally trained armed group in the GLMZ that isn't technically a corporate asset. They operate on clean contracts with defined deliverables. They do not do ideology, do not do drugs, and do not do work they consider beneath their professional standards. This last constraint has cost them several high-value clients and generated enemies who expected compliance. Their physical base is mobile — a convoy of three commercial vehicles operating in the Midwest transit corridors between GLMZ anchor cities, with a semi-permanent waystation in an industrial park east of Gary.",
    "ideology": "Professionalism is a form of ethics. A job done cleanly, without civilian casualties and delivered on time, is morally superior to a job done with ideology. They are mercenaries and are comfortable with that identity.",
    "territory": "No fixed street territory. Mobile operations across the Indianapolis corridor, Gary-Chicago transit route, and throughout Midwest transit hubs. Available anywhere payment clears.",
    "leadership": "",
    "methods": [
        "Contract security — executive protection, facility assessment, corporate counter-intelligence",
        "Kidnapping-for-ransom — high-value targets only, clean extraction and delivery",
        "Problem resolution — euphemistic contracting term for whatever the client needs resolved",
        "Training services — for a fee, will train client security teams to professional standard"
    ],
    "resources": "Professional-grade weapons and equipment maintained to corporate security standards. Multiple vehicles modified for secure transport. Contractual relationships with several legitimate corporations and multiple criminal clients.",
    "goals": "Remain solvent and professional. Eventually accumulate enough capital to legitimize. Take revenge on Hexagram's former client — this is an unofficial goal that no one discusses.",
    "size_class": "medium",
    "relationships": [
        {"name": "The Glass Ladder", "type": "rival", "reason": "Competing for corporate security and counter-intelligence contracts; the Ladder hires the Ward out as muscle, which the Ward finds degrading"},
        {"name": "The Neon Vipers", "type": "rival", "reason": "The Ward has intercepted Viper couriers claiming contracted authority; both sides dispute whether the contract was legitimate"},
        {"name": "The Shore Dogs", "type": "ally", "reason": "Complementary maritime and land operations; mutual client referrals where the other's specialty is required"},
        {"name": "The Third Rail", "type": "ally", "reason": "The Ward holds infrastructure security contracts protecting critical Third Rail nodes; payment is reliable"}
    ],
    "narrative_function": "The GLMZ's most professional hired force. Can be employed by players or be a credible threat. Their military precision makes them stand out from street gangs.",
    "story_hooks": [
        "The Null Ward took a contract that their client is now refusing to pay for",
        "They've been hired to extract someone the players need to protect",
        "A Ward member is trying to quietly exit the gang — and the gang won't let them"
    ],
    "tags": ["faction", "gang", "security", "contractors", "mobile", "gary", "indianapolis", "professional"],
    "related_entities": [],
    "known_members": []
}

# Write new gang files
for gang_data, gang_name in [
    (glass_ladder, "The Glass Ladder"),
    (ferment, "The Ferment"),
    (null_ward, "The Null Ward")
]:
    fpath = os.path.join(FACTIONS_DIR, f"{gang_data['id']}.json")
    with open(fpath, 'w', encoding='utf-8') as f:
        json.dump(gang_data, f, indent=2, ensure_ascii=False)
    gang_registry[gang_name] = {'path': fpath, 'id': gang_data['id'], 'data': gang_data}
    print(f"[CREATED] {gang_name} -> {fpath}")

# ── Step 3: Update all 24 gang files ──────────────────────────────────────────

GANG_NETWORK = {
    "The 92nd Street Kings": {
        "size_class": "large",
        "rivals": [
            {"name": "The Burnside Guard", "reason": "Overlapping Chicago south side turf claims have kept these two gangs in low-grade conflict for years"},
            {"name": "The Pure Hand", "reason": "The Pure Hand views the Kings as corruptors of neighborhood youth through drug sales"}
        ],
        "allies": [
            {"name": "The Interchange", "reason": "The Kings provide muscle and territory access; the Interchange provides distribution infrastructure and a cut"},
            {"name": "The Voltage Saints", "reason": "Joint operations in the Shelf fringe; the Kings supply foot traffic to the Saints' underground arena"}
        ]
    },
    "The Bone Parish": {
        "size_class": "medium",
        "rivals": [
            {"name": "The Coffin Nails", "reason": "Both harvest and traffic augmentation components; territorial overlap in Detroit has produced violence"},
            {"name": "The Reclaimed", "reason": "The Parish operates in Detroit's ruins belt, which the Reclaimed consider their sovereign territory"}
        ],
        "allies": [
            {"name": "The Gauze", "reason": "Medical supply relationship — the Gauze acquires surgical materials from the Parish; neither discusses provenance"},
            {"name": "The Ferment", "reason": "The Ferment provides biochemical compounds for Parish surgical operations"}
        ]
    },
    "The Burnside Guard": {
        "size_class": "large",
        "rivals": [
            {"name": "The 92nd Street Kings", "reason": "Adjacent Chicago territory; the Guard claims protection authority the Kings do not recognize"},
            {"name": "The Siphon Collective", "reason": "The Guard taxes businesses the Collective also taps for utility siphons — double-extraction creates friction"}
        ],
        "allies": [
            {"name": "The Third Rail", "reason": "Both operate in Chicago's infrastructure layer; non-aggression pact and occasional joint use of underground access"},
            {"name": "The Pure Hand", "reason": "Shared interest in neighborhood stability — different methods, compatible goals in some overlapping zones"}
        ]
    },
    "The Causeway Collective": {
        "size_class": "large",
        "rivals": [
            {"name": "The Fathom Line", "reason": "Milwaukee's water routes are finite; the Collective and the Line dispute access to several key corridors"},
            {"name": "The Shore Dogs", "reason": "Coastal distribution conflicts with the Shore Dogs' Great Lakes operation at several handoff points"}
        ],
        "allies": [
            {"name": "The Interchange", "reason": "The Collective feeds Milwaukee commerce into the Interchange's GLMZ-wide distribution network"},
            {"name": "The Last Mile", "reason": "The Last Mile delivers what the Collective moves; operational partnership in the Milwaukee-Chicago corridor"}
        ]
    },
    "The Coffin Nails": {
        "size_class": "medium",
        "rivals": [
            {"name": "The Bone Parish", "reason": "Competition for Detroit's augmentation black market has produced violence over supply and pricing"},
            {"name": "The Volt Runners", "reason": "Shared territory in the deep Shelf has led to several territorial confrontations"}
        ],
        "allies": [
            {"name": "The Undertow", "reason": "The Undertow provides harbor access for shipping harvested components; the Nails provide cargo the Undertow doesn't examine"},
            {"name": "The Glass Ladder", "reason": "The Glass Ladder purchases biometric and augmentation data from the Nails for use as leverage"}
        ]
    },
    "The Erie Remnant": {
        "size_class": "large",
        "rivals": [
            {"name": "The Reclaimed", "reason": "Both are survivor-era territorial gangs with ideological claims to Great Lakes ruins; their zones overlap near Cleveland"},
            {"name": "The Third Rail", "reason": "The Remnant controls Cleveland's lakefront and resists the Third Rail's push to extend transit infrastructure into their zone"}
        ],
        "allies": [
            {"name": "The Shore Dogs", "reason": "The Shore Dogs' lake operations include Cleveland; they pay the Remnant protection tithe in exchange for safe port access"},
            {"name": "The Last Mile", "reason": "Supply route agreement — the Last Mile operates through Remnant territory with permission and payment"}
        ]
    },
    "The Fathom Line": {
        "size_class": "medium",
        "rivals": [
            {"name": "The Causeway Collective", "reason": "Milwaukee water route disputes"},
            {"name": "The Shore Dogs", "reason": "Surface water territory overlap near Milwaukee's drowned port"}
        ],
        "allies": [
            {"name": "The Undertow", "reason": "Complementary operations — the Fathom Line works flooded infrastructure, the Undertow works the surface harbor; mutual handoff arrangements"},
            {"name": "The Lakebed Scrapers", "reason": "Both work underwater environments; salvage coordination agreement prevents equipment conflicts in shared zones"}
        ]
    },
    "The Gauze": {
        "size_class": "small",
        "rivals": [
            {"name": "The Ferment", "reason": "Both supply medical and pharmaceutical needs to the same underserved population; the Gauze resents the Ferment's cheaper unlicensed alternatives eroding their client base"}
        ],
        "allies": [
            {"name": "The Bone Parish", "reason": "Supply relationship — surgical materials without questions"},
            {"name": "The 92nd Street Kings", "reason": "The Kings provide protection for Gauze clinic locations in exchange for treatment of members at reduced cost"}
        ]
    },
    "The Interchange": {
        "size_class": "massive",
        "rivals": [
            {"name": "The Last Mile", "reason": "Competing distribution networks; the Interchange resents the Last Mile's point-to-point model cutting into their hub-and-spoke volume"},
            {"name": "The Null Ward", "reason": "The Interchange has hired the Null Ward before and had disputes over contract terms; the relationship soured"}
        ],
        "allies": [
            {"name": "The Causeway Collective", "reason": "Milwaukee commerce feeds through the Collective into the Interchange"},
            {"name": "The Neon Vipers", "reason": "Vehicle logistics — the Vipers run product along the Circuit for the Interchange at negotiated rates"},
            {"name": "The Shore Dogs", "reason": "The Interchange uses Shore Dogs' lake routes for bulk cargo that can't move by road"}
        ]
    },
    "The Lakebed Scrapers": {
        "size_class": "medium",
        "rivals": [
            {"name": "The Fathom Line", "reason": "Underwater territory overlap near Milwaukee"},
            {"name": "The Shore Dogs", "reason": "Surface salvage rights disputes"}
        ],
        "allies": [
            {"name": "The Siphon Collective", "reason": "The Collective provides power for deep equipment in exchange for salvaged electrical components"},
            {"name": "The Volt Runners", "reason": "Tech partnership — the Runners provide technical equipment maintenance; the Scrapers share salvage finds"}
        ]
    },
    "The Last Mile": {
        "size_class": "large",
        "rivals": [
            {"name": "The Interchange", "reason": "The Interchange views the Last Mile as a disruptive competitor cutting into volume business"},
            {"name": "The Neon Vipers", "reason": "Road corridor disputes — both need the same highway infrastructure; the Vipers tax Last Mile vehicles moving through their Circuit zone"}
        ],
        "allies": [
            {"name": "The Causeway Collective", "reason": "Corridor partnership in the Milwaukee-Chicago route"},
            {"name": "Switchblade Alley", "reason": "The Alley provides vehicle maintenance for Last Mile runners in exchange for route access"}
        ]
    },
    "The Neon Vipers": {
        "size_class": "large",
        "rivals": [
            {"name": "The Last Mile", "reason": "Road territory disputes in Circuit corridor"},
            {"name": "The Volt Runners", "reason": "Speed technology competition — the Runners have better tech, the Vipers have more road"},
            {"name": "The Null Ward", "reason": "The Ward has intercepted Viper couriers and claimed it was a contracted operation; both sides dispute the story"}
        ],
        "allies": [
            {"name": "The Interchange", "reason": "Logistics partnership — the Vipers run product for the Interchange at set rates"},
            {"name": "The Voltage Saints", "reason": "Performance culture connection — the Saints run underground vehicle events the Vipers dominate"}
        ]
    },
    "The Pure Hand": {
        "size_class": "large",
        "rivals": [
            {"name": "The Bone Parish", "reason": "Augmentation trafficking is Pure Hand heresy; the Parish is a frequent target"},
            {"name": "The Coffin Nails", "reason": "Same ideological opposition to augmentation harvesting"},
            {"name": "The Glass Ladder", "reason": "The Pure Hand views corporate espionage as an extension of corporate corruption"}
        ],
        "allies": [
            {"name": "The Reclaimed", "reason": "Shared anti-corporate ideology creates operational alignment despite theological differences"},
            {"name": "The Burnside Guard", "reason": "Neighborhood protection goals align in some districts; tactical cooperation has occurred"}
        ]
    },
    "The Reclaimed": {
        "size_class": "large",
        "rivals": [
            {"name": "The Bone Parish", "reason": "Detroit territorial conflict — the Parish operates in ruins the Reclaimed consider their sovereign zone"},
            {"name": "The Erie Remnant", "reason": "Great Lakes survivor factions with overlapping ideology and zone edges"}
        ],
        "allies": [
            {"name": "The Pure Hand", "reason": "Anti-corporate alignment despite ideological friction on other issues"},
            {"name": "The Ferment", "reason": "Shared DIY, anti-corporate ethos; the Ferment supplies the Reclaimed with pharmaceutical alternatives"}
        ]
    },
    "The Shore Dogs": {
        "size_class": "massive",
        "rivals": [
            {"name": "The Fathom Line", "reason": "Water territory near Milwaukee's drowned port"},
            {"name": "The Lakebed Scrapers", "reason": "Surface salvage rights"},
            {"name": "The Undertow", "reason": "Old Harbor waterfront competition"}
        ],
        "allies": [
            {"name": "The Interchange", "reason": "Bulk cargo lake routes"},
            {"name": "The Erie Remnant", "reason": "Cleveland port access agreement"},
            {"name": "The Null Ward", "reason": "Complementary land/maritime operations; client referrals"}
        ]
    },
    "The Siphon Collective": {
        "size_class": "medium",
        "rivals": [
            {"name": "The Burnside Guard", "reason": "Double-extraction friction in shared Chicago neighborhoods"},
            {"name": "The Volt Runners", "reason": "Competing for control of Chicago's energy infrastructure; the Runners take power the Collective has already tapped"}
        ],
        "allies": [
            {"name": "The Third Rail", "reason": "Chicago underground infrastructure partnership — shared tunnels, mutual access"},
            {"name": "The Lakebed Scrapers", "reason": "Power exchange for salvaged components"}
        ]
    },
    "The Third Rail": {
        "size_class": "massive",
        "rivals": [
            {"name": "The Volt Runners", "reason": "Competing transit alternatives in Chicago's vertical vs horizontal infrastructure divide"},
            {"name": "The Erie Remnant", "reason": "The Rail wants to extend infrastructure into Remnant territory; the Remnant resists"}
        ],
        "allies": [
            {"name": "The Siphon Collective", "reason": "Underground Chicago infrastructure partnership"},
            {"name": "The Burnside Guard", "reason": "Surface-level protection agreements in shared Chicago zones"},
            {"name": "The Null Ward", "reason": "Infrastructure security contracts — the Ward is paid to protect critical Rail nodes"}
        ]
    },
    "The Undertow": {
        "size_class": "medium",
        "rivals": [
            {"name": "The Shore Dogs", "reason": "Old Harbor waterfront competition"},
            {"name": "The Harbor Rats", "reason": "Existing rival — port vs Drowning Mile"}
        ],
        "allies": [
            {"name": "The Fathom Line", "reason": "Harbor/underwater operational coordination"},
            {"name": "The Coffin Nails", "reason": "Provides harbor export routes for Nails cargo"}
        ]
    },
    "The Volt Runners": {
        "size_class": "medium",
        "rivals": [
            {"name": "The Siphon Collective", "reason": "Chicago energy infrastructure competition"},
            {"name": "The Coffin Nails", "reason": "Shelf territory overlap"},
            {"name": "The Third Rail", "reason": "Vertical vs horizontal Chicago transit divide"}
        ],
        "allies": [
            {"name": "The Neon Vipers", "reason": "Speed technology culture connection and mutual respect"},
            {"name": "The Glass Ladder", "reason": "Tech infrastructure support partnership; the Runners provide vertical access"},
            {"name": "Switchblade Alley", "reason": "Maintenance relationship — the Alley maintains Volt Runner equipment"}
        ]
    },
    "The Voltage Saints": {
        "size_class": "medium",
        "rivals": [
            {"name": "The Null Ward", "reason": "The Ward shut down a Saints' arena operation claiming a client contract; the Saints haven't forgotten"},
            {"name": "The Coffin Nails", "reason": "Deep Shelf territorial overlap near the Saints' underground arena"}
        ],
        "allies": [
            {"name": "The 92nd Street Kings", "reason": "The Kings supply fighters and crowd to the Saints' events"},
            {"name": "The Neon Vipers", "reason": "Performance culture overlap — joint vehicle events and betting arrangements"}
        ]
    },
    "Switchblade Alley": {
        "size_class": "medium",
        "rivals": [
            {"name": "The Volt Runners", "reason": "Overlapping Shelf maintenance corridor territory claims"},
            {"name": "The Null Ward", "reason": "The Ward has operated in Shelf maintenance corridors without paying the Alley's access fee"}
        ],
        "allies": [
            {"name": "The Last Mile", "reason": "Vehicle maintenance for Last Mile runners in exchange for route access"},
            {"name": "The Siphon Collective", "reason": "Shared Shelf infrastructure access and mutual non-interference"},
            {"name": "The Ferment", "reason": "The Alley distributes Ferment product through Shelf corridors"}
        ]
    },
    "The Glass Ladder": {
        "size_class": "medium",
        "rivals": [
            {"name": "The Pure Hand", "reason": "Anti-corporate ideology makes Ladder operatives targets for the Pure Hand"},
            {"name": "The Null Ward", "reason": "Competing for corporate security and counter-intelligence contracts"}
        ],
        "allies": [
            {"name": "The Coffin Nails", "reason": "Purchases biometric and augmentation data for blackmail leverage"},
            {"name": "The Volt Runners", "reason": "Tech infrastructure support; the Runners provide vertical access for server-room operations"}
        ]
    },
    "The Ferment": {
        "size_class": "medium",
        "rivals": [
            {"name": "The Gauze", "reason": "Competing pharmaceutical supply to the same underserved population"},
            {"name": "The Pure Hand", "reason": "Chemical purification ideology clashes with Ferment's liberation ethos"}
        ],
        "allies": [
            {"name": "The Reclaimed", "reason": "Shared DIY anti-corporate ethos and territory overlap"},
            {"name": "Switchblade Alley", "reason": "Distribution through Shelf corridors"},
            {"name": "The Bone Parish", "reason": "Chemistry supply for surgical operations"}
        ]
    },
    "The Null Ward": {
        "size_class": "medium",
        "rivals": [
            {"name": "The Glass Ladder", "reason": "Corporate contract competition and resentment at being hired as muscle"},
            {"name": "The Neon Vipers", "reason": "Road operation interference disputes"}
        ],
        "allies": [
            {"name": "The Shore Dogs", "reason": "Complementary land/maritime operations; client referrals"},
            {"name": "The Third Rail", "reason": "Infrastructure security contracts protecting critical Rail nodes"}
        ]
    }
}

def build_relationships(existing_rels, network_entry):
    """Merge new relationships into existing, avoiding duplicates by name."""
    existing_by_name = {}
    for rel in existing_rels:
        existing_by_name[rel.get('name', '')] = rel

    for r in network_entry.get('rivals', []):
        if r['name'] not in existing_by_name:
            existing_by_name[r['name']] = {
                'name': r['name'],
                'type': 'rival',
                'reason': r['reason']
            }

    for a in network_entry.get('allies', []):
        if a['name'] not in existing_by_name:
            existing_by_name[a['name']] = {
                'name': a['name'],
                'type': 'ally',
                'reason': a['reason']
            }

    return list(existing_by_name.values())

updated_count = 0
for gang_name, network_entry in GANG_NETWORK.items():
    if gang_name not in gang_registry:
        print(f"[WARN] {gang_name} not in registry, skipping relationship update")
        continue

    entry = gang_registry[gang_name]
    fpath = entry['path']
    try:
        with open(fpath, 'r', encoding='utf-8') as f:
            data = json.load(f)

        existing_rels = data.get('relationships', [])
        # Normalize relationships if they're strings
        normalized = []
        for rel in existing_rels:
            if isinstance(rel, dict):
                normalized.append(rel)
            elif isinstance(rel, str):
                normalized.append({'name': rel, 'type': 'unknown', 'reason': ''})
        existing_rels = normalized

        data['relationships'] = build_relationships(existing_rels, network_entry)
        data['size_class'] = network_entry['size_class']

        # Ensure known_members exists
        if 'known_members' not in data:
            data['known_members'] = []

        with open(fpath, 'w', encoding='utf-8') as f:
            json.dump(data, f, indent=2, ensure_ascii=False)

        # Update registry data
        gang_registry[gang_name]['data'] = data
        updated_count += 1
        print(f"[UPDATED] {gang_name} -> size_class={network_entry['size_class']}, {len(data['relationships'])} relationships")
    except Exception as e:
        print(f"[ERROR] updating {gang_name}: {e}")

print(f"\nUpdated {updated_count}/24 gang files")

# ── Step 4: Scan characters and assign to gangs ───────────────────────────────

# Build lookup: gang name -> id
gang_id_map = {name: info['id'] for name, info in gang_registry.items()}

# All 24 gang names
ALL_GANG_NAMES = list(gang_registry.keys())

# Also build aliases lookup
gang_aliases = {
    "The 92nd Street Kings": ["92nd street kings", "92nd street", "the kings"],
    "The Bone Parish": ["bone parish"],
    "The Burnside Guard": ["burnside guard"],
    "The Causeway Collective": ["causeway collective"],
    "The Coffin Nails": ["coffin nails"],
    "The Erie Remnant": ["erie remnant"],
    "The Fathom Line": ["fathom line"],
    "The Gauze": ["the gauze", "gauze"],
    "The Interchange": ["the interchange", "interchange"],
    "The Lakebed Scrapers": ["lakebed scrapers"],
    "The Last Mile": ["last mile"],
    "The Neon Vipers": ["neon vipers"],
    "The Pure Hand": ["pure hand"],
    "The Reclaimed": ["the reclaimed", "reclaimed"],
    "The Shore Dogs": ["shore dogs"],
    "The Siphon Collective": ["siphon collective"],
    "The Third Rail": ["third rail"],
    "The Undertow": ["the undertow", "undertow"],
    "The Volt Runners": ["volt runners"],
    "The Voltage Saints": ["voltage saints"],
    "Switchblade Alley": ["switchblade alley"],
    "The Glass Ladder": ["glass ladder", "the ladder"],
    "The Ferment": ["the ferment", "ferment", "the kitchen"],
    "The Null Ward": ["null ward", "the ward"],
}

def find_gang_match(text):
    """Return gang name if text mentions a gang, else None."""
    text_lower = text.lower()
    for gang_name, aliases in gang_aliases.items():
        if gang_name.lower() in text_lower:
            return gang_name
        for alias in aliases:
            if alias in text_lower:
                return gang_name
    return None

# Geographic/thematic assignment weights
# For characters without direct gang mention, assign based on location/theme
GEO_GANG_MAP = {
    # Chicago
    'chicago': ['The 92nd Street Kings', 'The Burnside Guard', 'The Interchange', 'The Third Rail', 'The Siphon Collective', 'The Volt Runners', 'The Glass Ladder'],
    'burnside': ['The Burnside Guard', 'The 92nd Street Kings'],
    'shelf': ['Switchblade Alley', 'The Volt Runners', 'The Voltage Saints', 'The Coffin Nails'],
    'meridian hub': ['The Glass Ladder'],
    'arcology': ['The Glass Ladder'],
    # Milwaukee
    'milwaukee': ['The Causeway Collective', 'The Fathom Line', 'The Last Mile', 'The Ferment'],
    'causeway': ['The Causeway Collective'],
    # Detroit
    'detroit': ['The Bone Parish', 'The Coffin Nails', 'The Reclaimed', 'The Ferment'],
    'ruins': ['The Reclaimed', 'The Bone Parish'],
    # Cleveland
    'cleveland': ['The Erie Remnant', 'The Shore Dogs'],
    'lakefront': ['The Erie Remnant', 'The Shore Dogs'],
    # Gary / Indiana
    'gary': ['The Ferment', 'The Null Ward'],
    'indianapolis': ['The Null Ward'],
    # Water / harbor
    'harbor': ['The Shore Dogs', 'The Undertow', 'The Fathom Line'],
    'lake': ['The Shore Dogs', 'The Lakebed Scrapers', 'The Fathom Line'],
    'underwater': ['The Fathom Line', 'The Lakebed Scrapers'],
    'port': ['The Undertow', 'The Shore Dogs', 'The Causeway Collective'],
    # Vehicles / road
    'courier': ['The Last Mile', 'The Neon Vipers'],
    'racing': ['The Neon Vipers', 'The Voltage Saints'],
    'vehicle': ['The Neon Vipers', 'The Last Mile', 'Switchblade Alley'],
    # Medical / pharma
    'medic': ['The Gauze', 'The Ferment'],
    'surgeon': ['The Bone Parish', 'The Gauze'],
    'pharmaceutical': ['The Ferment', 'The Gauze'],
    'chemist': ['The Ferment'],
    # Security / corporate
    'security': ['The Null Ward', 'The Glass Ladder'],
    'corporate': ['The Glass Ladder'],
    'intelligence': ['The Glass Ladder'],
    'espionage': ['The Glass Ladder'],
    # Underground fighting
    'fighter': ['The Voltage Saints', 'The 92nd Street Kings'],
    'arena': ['The Voltage Saints'],
    # Infrastructure
    'infrastructure': ['The Third Rail', 'The Siphon Collective', 'Switchblade Alley'],
    'transit': ['The Third Rail', 'The Last Mile'],
    'tunnel': ['The Third Rail', 'The Siphon Collective'],
    'power': ['The Siphon Collective', 'The Volt Runners'],
    # Salvage
    'salvage': ['The Lakebed Scrapers', 'The Reclaimed'],
    'scavenger': ['The Lakebed Scrapers', 'The Reclaimed'],
    # Augmentation
    'augmentation': ['The Coffin Nails', 'The Bone Parish', 'The Pure Hand'],
    'implant': ['The Coffin Nails', 'The Bone Parish'],
    # Criminal general
    'smuggler': ['The Interchange', 'The Shore Dogs', 'The Undertow'],
    'trafficker': ['The Interchange', 'The Coffin Nails'],
    'fence': ['The Interchange', 'The Last Mile'],
    'enforcer': ['The 92nd Street Kings', 'The Burnside Guard', 'The Null Ward'],
    'hitman': ['The Null Ward'],
    'mercenary': ['The Null Ward'],
    # Anti-corpo / survivor
    'anti-corpo': ['The Reclaimed', 'The Pure Hand', 'The Ferment'],
    'survivor': ['The Erie Remnant', 'The Reclaimed'],
    'rebel': ['The Reclaimed', 'The Pure Hand'],
}

CRIMINAL_KEYWORDS = [
    'gang', 'criminal', 'thug', 'dealer', 'smuggler', 'trafficker', 'enforcer',
    'fence', 'hitman', 'mercenary', 'fixer', 'runner', 'street', 'hustle',
    'underground', 'black market', 'outlaw', 'bandit', 'pirate', 'raider',
    'syndicate', 'cartel', 'crew', 'outfit', 'racket', 'operator'
]

def score_character(data):
    """
    Returns (gang_name, role, reason) or (None, None, None).
    Priority: direct mention > geo/theme match.
    """
    affiliation = str(data.get('affiliation', '') or '')
    role = str(data.get('role', '') or '')
    desc = str(data.get('description', '') or '')
    tags = ' '.join(data.get('tags', []) or [])
    combined = f"{affiliation} {role} {desc} {tags}".lower()

    # Direct gang mention
    for field in [affiliation, role, desc]:
        match = find_gang_match(field)
        if match:
            # Determine role
            role_text = role.lower()
            if any(w in role_text for w in ['leader', 'boss', 'chief', 'head', 'captain']):
                member_role = 'Leader'
            elif any(w in role_text for w in ['lieutenant', 'sergeant', 'officer', 'commander']):
                member_role = 'Lieutenant'
            elif any(w in combined for w in ['affiliate', 'associate', 'contact', 'informant']):
                member_role = 'Affiliate'
            else:
                member_role = 'Member'
            return match, member_role, f"Direct affiliation mention in character data"

    return None, None, None

def geo_assign(data):
    """Assign gang based on geography/theme. Returns (gang_name, role)."""
    affiliation = str(data.get('affiliation', '') or '')
    role_text = str(data.get('role', '') or '')
    desc = str(data.get('description', '') or '')
    tags = ' '.join(data.get('tags', []) or [])
    combined = f"{affiliation} {role_text} {desc} {tags}".lower()

    # Check for criminal keywords first
    is_criminal = any(kw in combined for kw in CRIMINAL_KEYWORDS)
    if not is_criminal:
        return None, None

    # Gather candidate gangs from geo/theme
    candidates = {}
    for keyword, gangs in GEO_GANG_MAP.items():
        if keyword in combined:
            for g in gangs:
                candidates[g] = candidates.get(g, 0) + 1

    if not candidates:
        return None, None

    # Pick highest scoring
    best = max(candidates, key=lambda g: candidates[g])

    # Determine role
    if any(w in role_text.lower() for w in ['leader', 'boss', 'chief', 'head', 'captain']):
        member_role = 'Lieutenant'  # downgrade since it's probabilistic
    else:
        member_role = 'Affiliate'

    return best, member_role

# Load and scan all character files
people_files = [f for f in os.listdir(PEOPLE_DIR) if f.endswith('.json')]
print(f"\nScanning {len(people_files)} character files...")

MAX_ASSIGNMENTS = 121
assignments = []  # list of (char_id, char_name, gang_name, role, reason, fpath)
direct_assignments = []
geo_assignments = []

for fname in people_files:
    fpath = os.path.join(PEOPLE_DIR, fname)
    try:
        with open(fpath, 'r', encoding='utf-8') as f:
            data = json.load(f)
    except Exception:
        continue

    char_id = data.get('id', fname.replace('.json', ''))
    char_name = data.get('name', 'Unknown')

    gang_name, member_role, reason = score_character(data)
    if gang_name:
        direct_assignments.append((char_id, char_name, gang_name, member_role, reason, fpath, data))
    else:
        gang_name_geo, member_role_geo = geo_assign(data)
        if gang_name_geo:
            geo_assignments.append((char_id, char_name, gang_name_geo, member_role_geo, 'Geographic/thematic match', fpath, data))

print(f"Direct mentions: {len(direct_assignments)}")
print(f"Geo/thematic candidates: {len(geo_assignments)}")

# Build final assignment list: all direct first, then geo up to cap
all_direct = direct_assignments
remaining_cap = MAX_ASSIGNMENTS - len(all_direct)
if remaining_cap < 0:
    # Even direct assignments exceed cap — take all direct
    print(f"[WARN] Direct assignments ({len(all_direct)}) exceed cap ({MAX_ASSIGNMENTS})")
    all_direct = all_direct[:MAX_ASSIGNMENTS]
    remaining_cap = 0

# Sample geo assignments to fill remaining cap
random.shuffle(geo_assignments)
geo_selected = geo_assignments[:remaining_cap]

final_assignments = all_direct + geo_selected
print(f"Final assignments: {len(final_assignments)} (cap={MAX_ASSIGNMENTS})")

# Count per gang
gang_member_counts = {name: 0 for name in ALL_GANG_NAMES}
for _, _, gname, _, _, _, _ in final_assignments:
    if gname in gang_member_counts:
        gang_member_counts[gname] += 1

# ── Now write updates ──────────────────────────────────────────────────────────

# Build gang known_members additions
gang_new_members = {name: [] for name in ALL_GANG_NAMES}
for char_id, char_name, gang_name, member_role, reason, fpath, char_data in final_assignments:
    if gang_name in gang_new_members:
        gang_new_members[gang_name].append({
            'name': char_name,
            'id': char_id,
            'role': member_role,
            'status': 'active',
            'notes': reason
        })

# Write gang files with new members
print("\nUpdating gang known_members...")
for gang_name, new_members in gang_new_members.items():
    if not new_members:
        continue
    if gang_name not in gang_registry:
        continue
    fpath = gang_registry[gang_name]['path']
    try:
        with open(fpath, 'r', encoding='utf-8') as f:
            data = json.load(f)
        existing_ids = {m.get('id') for m in data.get('known_members', [])}
        added = 0
        for m in new_members:
            if m['id'] not in existing_ids:
                data.setdefault('known_members', []).append(m)
                existing_ids.add(m['id'])
                added += 1
        with open(fpath, 'w', encoding='utf-8') as f:
            json.dump(data, f, indent=2, ensure_ascii=False)
        print(f"  {gang_name}: +{added} members (total {len(data['known_members'])})")
    except Exception as e:
        print(f"  [ERROR] {gang_name}: {e}")

# Update character files with gang affiliation
print("\nUpdating character affiliations...")
char_updated = 0
for char_id, char_name, gang_name, member_role, reason, fpath, char_data in final_assignments:
    try:
        with open(fpath, 'r', encoding='utf-8') as f:
            data = json.load(f)
        current_affil = data.get('affiliation', '') or ''
        if gang_name.lower() not in current_affil.lower():
            if current_affil:
                data['affiliation'] = f"{current_affil}; {gang_name}"
            else:
                data['affiliation'] = gang_name
        with open(fpath, 'w', encoding='utf-8') as f:
            json.dump(data, f, indent=2, ensure_ascii=False)
        char_updated += 1
    except Exception as e:
        print(f"  [ERROR] char {char_name}: {e}")

print(f"Updated {char_updated} character files")

# ── Final Report ───────────────────────────────────────────────────────────────
print("\n" + "="*60)
print("FINAL REPORT")
print("="*60)
print(f"\n{'Gang Name':<35} {'Members':>7}")
print("-"*45)
total_members = 0
zero_member_gangs = []
for gang_name in sorted(gang_member_counts.keys()):
    count = gang_member_counts[gang_name]
    total_members += count
    if count == 0:
        zero_member_gangs.append(gang_name)
    print(f"{gang_name:<35} {count:>7}")

print("-"*45)
print(f"{'TOTAL':<35} {total_members:>7}")
print(f"\nCap: {MAX_ASSIGNMENTS} | Assigned: {total_members} | {'OK' if total_members <= MAX_ASSIGNMENTS else 'OVER CAP!'}")
print(f"\nGangs with 0 members ({len(zero_member_gangs)}):")
for g in zero_member_gangs:
    print(f"  - {g}")
