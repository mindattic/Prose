"""
assign_tiers.py — Assigns social tiers (1-5) and corporate affiliations to all character JSON files.

Tier system:
  1 = Laborers, service workers, gang members, scavengers, street-level criminals
  2 = Skilled tradespeople, small business owners, junior corporate, police/military enlisted
  3 = White-collar corporate, mid-level managers, IT, licensed professionals
  4 = Educated professionals (doctors, lawyers, engineers), senior managers, academics, researchers
  5 = C-suite, directors, executives, wealthy investors, top scientists, celebrities

Run with: py -3 assign_tiers.py
"""

import json
import glob
import os
import re

PEOPLE_DIR = "D:/Projects/MindAttic/StreetSamurai/engine/data/people"


def assign_tier(data):
    role = (data.get("role") or "").lower()
    aff = (data.get("affiliation") or "").lower()
    desc = (data.get("description") or "")[:600].lower()
    tags_list = data.get("stats", {}).get("tags", [])
    tags = " ".join(t.lower() for t in tags_list if isinstance(t, str))

    # --------------- EARLY EXITS: explicit overrides BEFORE other checks ---------------
    # Children/students/youth — always tier 1 regardless of what else matches
    if re.search(r'\b(student|school student|child prodigy|school kid|youth|apprentice|intern)\b', role):
        return 1
    # Retired (non-elite) — tier 1
    if re.search(r'\bretired\b', role) and not re.search(r'\b(executive|director|general|admiral|ceo|president)\b', role):
        return 1
    # Street-level explicit markers — tier 1
    if re.search(r'\b(gang member|gang prospect|street orphan|junior prospect|beggar|homeless|vagrant)\b', role):
        return 1

    # --------------- TIER 5 ---------------
    # Only trigger on ROLE for tier 5 — affiliation can say "executive" after script runs
    tier5_role_patterns = [
        r'\bceo\b', r'\bchief executive\b', r'\bpresident\b',
        r'\bexecutive director\b', r'\bvice president\b',
        r'\bchairman\b', r'\bchairwoman\b',
        r'\bc-suite\b',
        r'\bchief financial officer\b', r'\bchief operating officer\b',
        r'\bchief technology officer\b', r'\bchief medical officer\b',
        r'\bchief science officer\b', r'\bchief information officer\b',
        r'\bevp\b',  # executive vice president
    ]
    # "executive vice president" or "senior vice president" in role = tier 5
    if re.search(r'\b(executive|senior) vice president\b', role):
        return 5
    if re.search(r'\bdirector\b', role) and re.search(r'\b(corporate|division|strategic|communications|intelligence)\b', role):
        return 5
    for pat in tier5_role_patterns:
        if re.search(pat, role):
            return 5
    # Check tags for tier-5 explicit tags
    if "tier-5" in tags or "tier 5" in tags:
        return 5

    # --------------- TIER 4 ---------------
    # Use more precise patterns to avoid false positives
    # Doctors, physicians, surgeons — but NOT street-level/unlicensed
    if re.search(r'\b(doctor|physician|surgeon|psychiatrist|psychologist)\b', role):
        # Exclude street-level/unlicensed/informal practitioners
        if re.search(r'\b(street|unlicensed|informal|underground|black market)\b', role):
            pass  # Falls through to tier 3 or lower
        else:
            return 4
    # Lawyers, attorneys, judges
    if re.search(r'\b(lawyer|attorney|solicitor|barrister|judge|magistrate)\b', role):
        return 4
    # Engineers (but NOT street-level roles accidentally containing "engineer")
    if re.search(r'\bengineer\b', role) and "junior" not in role:
        # Ensure it's actually engineering, not e.g. "social engineering"
        if not re.search(r'\b(social engineering|negotiation)\b', role):
            return 4
    # Architects (building, not "social architect" etc)
    if re.search(r'\barchitect\b', role) and not re.search(r'\b(social|political)\b', role):
        return 4
    # Professors, researchers, scientists (professional academic roles)
    # But NOT students or academic-adjacent criminals
    if re.search(r'\b(professor|researcher|scientist|academic|xenolinguist|xenobiologist|astrophysicist|computational theorist)\b', role):
        # Exclude students, prodigies, or criminal academic roles
        if re.search(r'\b(student|prodigy|child|kid|theft|thief)\b', role):
            pass  # Falls through
        else:
            return 4
    # Senior analyst, lead analyst, principal analyst
    if re.search(r'\b(senior|lead|principal|chief) analyst\b', role):
        return 4
    # Senior manager, department head, division head
    if re.search(r'\b(senior manager|department head|division head|operations director|strategic director)\b', role):
        return 4
    # Corporate intelligence director, communications director in senior roles
    if re.search(r'\b(intelligence director|communications director)\b', role):
        return 4
    # Bioethicist, ethicist
    if re.search(r'\b(bioethicist|ethicist)\b', role):
        return 4
    # Forensic specialist, forensic examiner (not just "forensic" in desc)
    if re.search(r'\bforensic (specialist|examiner|analyst)\b', role):
        return 4
    # Licensed pharmacist (not "street pharmacist")
    if re.search(r'\bpharmacist\b', role) and "street" not in role:
        return 4
    # Veterinarian
    if re.search(r'\bveterinarian\b', role):
        return 4
    # Neural surgeon, neurosurgeon, neurologist
    if re.search(r'\b(neurosurgeon|neurologist|neural surgeon)\b', role):
        return 4
    # Trauma surgeon, cardiac surgeon etc
    if re.search(r'\b(trauma|cardiac|neurovascular|orthopedic) surgeon\b', role):
        return 4
    # Prosthetics specialist (medical-grade, NOT small shop owner)
    if re.search(r'\b(prosthetics fitter|prosthetics specialist|rehabilitation specialist)\b', role):
        # Small shop owner prosthetics fitter = tier 2; hospital/clinic level = tier 4
        if re.search(r'\b(small shop|workshop|geartown|independent)\b', role):
            pass  # Falls to tier 2
        else:
            return 4
    # Geneticist, genomicist
    if re.search(r'\b(geneticist|genomicist|gene therapist|reproductive gene therapist)\b', role):
        return 4
    # Consultant (senior professional consultants)
    if re.search(r'\b(senior consultant|principal consultant)\b', role):
        return 4
    # Urban design director, urban planner (senior)
    if re.search(r'\b(senior urban|urban design director|lead urban)\b', role):
        return 4
    # PhD/doctorate in description
    if re.search(r'\b(ph\.?d\.?|doctorate|doctoral candidate|board-certified|board certified)\b', desc):
        # But only if role suggests professional position
        if re.search(r'\b(researcher|scientist|professor|analyst|specialist|consultant|engineer|architect)\b', role):
            return 4
    # Private investigator with clear professional signals in affiliation
    if "private investigator" in role and re.search(r'\b(certified|licensed|forensic|court-recognized|boutique|senior)\b', aff):
        return 4
    # Explicit tier tags from previous runs
    if "tier-4" in tags:
        return 4

    # --------------- TIER 3 ---------------
    tier3_role_kw = [
        "technician", "lab technician", "field technician",
        "systems operator",  # "operator" alone is too broad (matches food stall operator)
        "transit operator",  # transit/systems operators
        "coordinator", "project coordinator", "logistics coordinator",
        "administrator", "systems administrator", "network administrator",
        "clerk", "senior clerk", "records clerk",
        "accountant", "bookkeeper",
        "security guard", "security officer", "corporate security",
        "nurse", "registered nurse", "paramedic",
        "field medic", "combat medic",
        "agent", "field agent", "special agent",
        "journalist", "reporter", "correspondent", "editor",
        "programmer", "software developer", "developer", "coder",
        "contractor", "independent contractor",
        "data analyst", "systems analyst",
        "archivist", "records keeper",
        "social worker", "case worker",
        "dispatcher", "communications officer",
        "photographer", "videographer",
        "designer", "graphic designer",
        "auditor",
        "insurance agent", "claims adjuster",
        "compliance officer",
        "private investigator",  # baseline tier for PI
        "investigator",  # corporate or general investigator
        "fixer",  # skilled operators
        "netrunner",  # digital specialists
        "infiltrator",  # skilled operatives
        "surveillance specialist",
        "counter-surveillance",
        "extraction specialist",
        "information broker",
        "underground writer", "zine publisher",
        "faith healer",  # biotech-adjacent
        "chemist", "biologist",
        "neural calibrator",
        "wheelman",  # skilled driver
        "forger", "identity fabricator",
        "smuggler",  # skilled trade
    ]
    # Role keywords that indicate tier 3 when present as primary role
    tier3_role_patterns = [
        r'\bmedic\b',  # "medic" (not "street medic" which is tier 1)
        r'\banalyst\b',  # generic analyst
        r'\bwriter\b',  # professional writer (not "underground writer" which is already in tier3_role_kw)
        r'\bcurator\b',
        r'\blibrarian\b',
        r'\bbiotech\b',  # biotech worker
        r'\bface\b',    # social engineering specialist
        r'\bsecurity specialist\b',  # professional security
        r'\bpersonal security\b',   # professional bodyguard/security
    ]
    # Corporate affiliation = at least tier 3
    tier3_aff_kw = [
        "ferrogate", "tessera", "helix", "lazarus", "fascia", "saltmarsh",
        "silkworm", "emberlace", "copperveil", "mirrorwell", "vantablack",
        "waxwing", "zheng-dao", "arcturus", "cinderfall", "vellichor",
        "nightshade", "novafold", "crucible", "gravemoss", "lacuna",
        "pale lantern", "pellucid", "scoria", "slagworks", "palladian",
        "rendstone", "ironclad", "stonepath", "tollgate", "vespid",
        "oracle drift", "charnel", "cormorant", "ashford", "bathysphere",
        "ouroboros", "irontide", "ashgrave", "dredge", "doyon",
        "kelpline", "marrowvault", "sulfur crown", "thornback",
        "ahtna", "aleut", "arctic slope", "bering straits", "bristol bay",
        "calista", "carrion", "chugach", "cook inlet", "cinderblock",
        "crestfall", "pelican", "ringo", "sealaska", "koniag", "nana",
        "meridian orbital", "aurochs medical complex",
    ]
    # Medical facility affiliations = at least tier 3
    # Use specific named facilities/corponations, not generic words like "clinic"
    medical_aff_patterns = [
        r'\baurochs medical complex\b', r'\bhelix medical\b',
        r'\bhelix biosystems\b', r'\bnightshade pharmatech\b',
        r'\bnovafold pharmaceuticals\b', r'\blazarus pharmaceuticals\b',
        r'\bzheng-dao\b', r'\bcrucible genomics\b',
    ]

    if any(kw in role for kw in tier3_role_kw):
        return 3
    for pat in tier3_role_patterns:
        if re.search(pat, role):
            return 3
    if any(kw in aff for kw in tier3_aff_kw):
        return 3
    for pat in medical_aff_patterns:
        if re.search(pat, aff):
            return 3
    # Check tier-3 tag
    if "tier-3" in tags:
        return 3

    # --------------- TIER 2 ---------------
    # Check tier-2 tag FIRST (before role keywords, so explicit tagging wins)
    if "tier-2" in tags:
        return 2
    tier2_role_kw = [
        "mechanic", "auto mechanic", "ship mechanic",
        "electrician",
        "plumber",
        "carpenter", "woodworker",
        "welder",
        "pilot", "shuttle pilot", "cargo pilot",
        "truck driver", "transit driver",
        "cook", "line cook",
        "chef",
        "teacher", "schoolteacher", "instructor",
        "police officer", "beat officer",
        "sergeant", "corporal", "lance corporal",
        "firefighter",
        "licensed mechanic",
        "small business owner", "shop owner", "stall owner",
        "tradesperson",
        "craftsperson", "artisan",
        "bartender",
        "tattoo artist", "piercing artist",
        "bouncer",
        "bodyguard",  # physical protection — tier 2 baseline
        "prosthetics fitter",  # small shop / independent prosthetics = tier 2 skilled trade
        "gang leader", "gang boss", "crew boss", "cartel lieutenant",
        "street preacher",
        "mutual aid organizer",
        "imam", "priest", "pastor", "religious leader",
        "seamstress", "tailor",
        "vendor", "street vendor",
        "market stall", "food stall",
        "caravan leader",
        "trader", "wandering trader",
        "merchant",
        "street pharmacist",  # unlicensed, street-level pharma
        "drug dealer",
        "scavenger",
        "junk merchant", "junk dealer", "scrap dealer",
        "waste disposal",
        "bootleg",  # bootleg sellers
        "fortune teller",
        "neighborhood council",  # community leaders
    ]
    tier2_aff_kw = [
        "police", "metro police", "enforcement", "metro pd",
        "military", "army", "navy", "marines",
        "union", "guild",
        "fire department",
    ]

    if any(kw in role for kw in tier2_role_kw):
        return 2
    # Role patterns for tier 2
    if re.search(r'\bofficer\b', role) and re.search(r'\b(police|security|metro|transit)\b', role):
        return 2
    # Driver roles
    if re.search(r'\bdriver\b', role) and not re.search(r'\b(wheelman|specialist)\b', role):
        return 2
    # Union members
    if re.search(r'\bunion (member|organizer|rep|representative|delegate)\b', role):
        return 2
    if any(kw in aff for kw in tier2_aff_kw):
        return 2
    # Small established businesses / markets
    if any(kw in aff for kw in ["vendors guild", "market guild"]):
        if any(kw in role for kw in ["vendor", "seller", "merchant", "stall", "shop", "operator"]):
            return 2
    # Gang leadership
    if any(kw in aff for kw in ["gang", "reclaimers", "crew", "cartel", "syndicate", "outfit", "mob"]):
        if any(kw in role for kw in ["leader", "boss", "chief", "head", "lieutenant", "captain", "second", "lead"]):
            return 2
    # Check tier tags
    if "tier-2" in tags:
        return 2

    # --------------- TIER 1 (default) ---------------
    # Explicit tier-1 signals
    if re.search(r'\b(gang member|prospect|street kid|orphan|beggar|homeless|vagrant|scavenger|laborer)\b', role):
        return 1
    if "tier-1" in tags:
        return 1

    return 1


def is_freelance_or_independent(data):
    """Returns True if character is explicitly freelance/independent with no corporate tie."""
    aff = (data.get("affiliation") or "").lower()
    role = (data.get("role") or "").lower()
    # Freelance/independent signals
    indie_signals = [
        "independent", "freelance", "self-employed", "no affiliation",
        "unaffiliated", "none formally", "none.", "no formal", "stateless",
    ]
    # If has strong indie signals
    if any(kw in aff for kw in indie_signals):
        # But check if they ALSO mention a corponation (could be "Independent, formerly Tessera")
        corp_signals_in_aff = [
            "tessera", "helix", "lazarus", "ferrogate", "arcturus", "fascia",
            "mirrorwell", "silkworm", "zheng-dao", "vellichor", "ringo",
            "nightshade", "novafold", "crucible", "saltmarsh",
        ]
        # If currently working for a corp despite "independent" label, not truly indie
        # "formerly" or "ex-" = truly independent now
        if any(kw in aff for kw in corp_signals_in_aff):
            if "formerly" in aff or "ex-" in aff or "previous" in aff or "left" in aff:
                return True  # truly indie now
            # Currently employed despite label
            return False
        return True
    if "freelance" in role:
        return True
    return False


def has_corporate_affiliation(data):
    """Returns True if the character already has a recognized corporate or organizational affiliation
    that we should NOT overwrite."""
    aff = (data.get("affiliation") or "").lower()

    # Truly independent = no corporate affiliation to keep
    if is_freelance_or_independent(data):
        return False

    # Blank/empty affiliation
    if not aff or aff in ["none", "n/a", "-", "unknown"]:
        return False

    # Gang/criminal affiliation — keep as-is, don't assign corp
    if any(kw in aff for kw in ["gang", "cartel", "syndicate", "outfit", "mafia", "crew", "reclaimers"]):
        # Exception: if they also have a corporate role
        corp_signals = ["tessera", "helix", "lazarus", "ferrogate", "arcturus", "fascia",
                        "ringo", "mirrorwell", "silkworm", "zheng-dao"]
        if any(kw in aff for kw in corp_signals):
            return True
        return False

    # Community/neighborhood/religious organizations — keep as-is
    community_signals = [
        "autonomous zone", "reclamation", "community", "council", "masjid",
        "church", "parish", "congregation", "mosque", "temple", "shrine",
        "neighborhood", "assembly", "collective", "cooperative",
        "mutual aid", "last light", "bright path",
    ]
    if any(kw in aff for kw in community_signals):
        return True  # Keep these — don't overwrite with a corponation

    # Named corponations — keep as-is
    corp_signals = [
        "corporation", "corponation", "systems", "industries", "dynamics",
        "pharmaceuticals", "biotech", "biosystems", "genomics", "institute",
        "agency", "department", "division", "group",
        "medical complex", "hospital", "clinic",
        "bureau", "authority", "ministry",
        # Named corponations
        "tessera", "helix", "lazarus", "ferrogate", "fascia", "arcturus",
        "mirrorwell", "vantablack", "waxwing", "zheng-dao", "silkworm",
        "emberlace", "copperveil", "cinderfall", "vellichor", "nightshade",
        "novafold", "crucible", "gravemoss", "lacuna", "pale lantern",
        "pellucid", "scoria", "slagworks", "palladian", "rendstone",
        "ironclad", "stonepath", "tollgate", "vespid", "oracle drift",
        "charnel", "cormorant", "ashford", "bathysphere", "ouroboros",
        "irontide", "ashgrave", "dredge", "doyon", "kelpline",
        "marrowvault", "sulfur crown", "thornback", "ahtna", "aleut",
        "arctic slope", "bering straits", "bristol bay", "calista",
        "carrion", "chugach", "cook inlet", "cinderblock", "crestfall",
        "pelican", "ringo", "sealaska", "koniag", "nana",
        "meridian orbital", "aurochs",
        # Specific named organizations
        "metro police", "glmz metro", "axiom", "wellspring",
        "transit workers", "last light", "bright path recovery",
        "meridian corporate intelligence", "sterling-nakamura",
    ]
    if any(kw in aff for kw in corp_signals):
        return True

    # Anything else with non-trivial content — assume it's already described
    # If aff has more than ~30 chars, it's probably already meaningful
    if len(aff.strip()) > 30:
        return True

    return False


def pick_corponation(data):
    """Pick the most fitting corponation for a tier 3+ character without one."""
    role = (data.get("role") or "").lower()
    desc = (data.get("description") or "").lower()
    aff = (data.get("affiliation") or "").lower()
    combined = role + " " + desc + " " + aff

    # Neural/BCI/bioelectric
    if re.search(r'\b(neural|bci|brain.computer|neuro|synaptic|bioelectric|implant|interface)\b', combined):
        return "Zheng-dao Bioelectric"
    # Genomics / genetics
    if re.search(r'\b(gene|genom|dna|cloning|synthetic biology|bioengineering)\b', combined):
        return "Crucible Genomics"
    # Pharmaceuticals (licensed, clinical)
    if re.search(r'\b(pharmaceutical|drug synthesis|clinical trial|apothecary|pharmacol)\b', combined):
        return "Lazarus Pharmaceuticals"
    # Medical / surgery / biotech
    if re.search(r'\b(medical|surgery|clinic|hospital|biotech|biosystem|life extension|gene therapy)\b', combined):
        return "Helix Biosystems"
    # Surveillance / intelligence / analytics
    if re.search(r'\b(surveillance|intelligence|analytics|predictive|counterintelligence|espionage)\b', combined):
        return "Copperveil Intelligence"
    # Military / weapons / defense
    if re.search(r'\b(military|weapon|defense|armament|tactical|combat|soldier|mercenary|armor)\b', combined):
        return "Arcturus Defense Solutions"
    # Transit / rail / freight / hyperlane
    if re.search(r'\b(hyperlane|fascia global)\b', combined):
        return "Fascia Global"
    if re.search(r'\b(transit|rail|freight|shipping|logistics|transport|cargo|hauling)\b', combined):
        return "Ferrogate Transit"
    # Telecom / communications
    if re.search(r'\b(telecom|telecommunication|radio|broadcast|network infrastructure)\b', combined):
        return "Saltmarsh Telecom"
    # Media / journalism / entertainment
    if re.search(r'\b(media|entertainment|film|journalism|reporter|journalist|zine|music|broadcast)\b', combined):
        return "Mirrorwell Media"
    # Underground/dark media
    if re.search(r'\b(underground media|pirate broadcast|illicit media|dark entertainment)\b', combined):
        return "Vantablack Media"
    # Neuromedia / BCI entertainment
    if re.search(r'\b(neuromedia|dream.simulation|vr experience|sensory media|waxwing)\b', combined):
        return "Waxwing Neuromedia"
    # AI / machine learning
    if re.search(r'\b(artificial intelligence|machine learning|algorithm|neural network|ai systems)\b', combined):
        return "Cinderblock AI"
    # Data / software / IT / cybersecurity
    if re.search(r'\b(data management|software|coding|cybersecurity|hacking|netrunner|digital infrastructure)\b', combined):
        return "Silkworm Data"
    # Construction / infrastructure / urban
    if re.search(r'\b(construction|infrastructure|building|urban planning|civil engineering)\b', combined):
        return "Palladian Construction"
    # Smart city / urban systems
    if re.search(r'\b(smart city|urban systems|smart infrastructure|city management|emberlace)\b', combined):
        return "Emberlace Systems"
    # Energy / power
    if re.search(r'\b(energy|power generation|geothermal|petroleum|fuel|reactor)\b', combined):
        return "Cinderfall Energy"
    # Agriculture / food
    if re.search(r'\b(agriculture|farming|crop|aquaculture|food production|agri)\b', combined):
        return "Ironclad Agrisystems"
    # Finance / banking / economics
    if re.search(r'\b(finance|banking|investment|economics|financial|accounting)\b', combined):
        return "Cook Inlet Region, Inc."
    # Mining / materials / industrial chemistry
    if re.search(r'\b(mining|extraction|metallurgy|industrial chemistry|materials science)\b', combined):
        return "Ashgrave Materials"
    # Research / academic / archival / education
    if re.search(r'\b(research|academic|university|institute|archive|archivist|historian|librarian)\b', combined):
        return "Vellichor Institute"
    # Bioethics / regulatory
    if re.search(r'\b(bioethic|ethics board|regulatory|compliance|policy maker)\b', combined):
        return "Pale Lantern Bioethics"
    # Navigation / guidance / intelligence systems
    if re.search(r'\b(navigation|guidance|orbital|aerospace|satellite)\b', combined):
        return "Oracle Drift Systems"
    # Security / bodyguard / enforcement (that isn't already military)
    if re.search(r'\b(bodyguard|security consultant|protection detail|private security)\b', combined):
        return "Arcturus Defense Solutions"
    # Default: major general corponation in GLMZ
    return "Tessera Corponation"


def build_affiliation_with_corp(data, corp, tier):
    """Build a new affiliation string embedding the corponation name and a role descriptor."""
    role = data.get("role") or ""
    # Take the first clause of the role before common separators
    short_role = re.split(r'[—\-–|,]', role)[0].strip()
    if len(short_role) > 50:
        short_role = short_role[:50].rstrip()
    short_role = short_role.lower()

    if tier == 5:
        seniority = "executive-level"
    elif tier == 4:
        seniority = "senior"
    elif tier == 3:
        seniority = "mid-level"
    else:
        seniority = ""

    # Don't prefix if the short_role already starts with the seniority word
    if seniority and short_role.startswith(seniority.split('-')[0]):
        return f"{corp} ({short_role})"
    elif seniority:
        return f"{corp} ({seniority} {short_role})"
    else:
        return f"{corp} ({short_role})"


KNOWN_CORPS = [
    "Zheng-dao Bioelectric", "Crucible Genomics", "Lazarus Pharmaceuticals",
    "Helix Biosystems", "Copperveil Intelligence", "Arcturus Defense Solutions",
    "Fascia Global", "Ferrogate Transit", "Saltmarsh Telecom", "Mirrorwell Media",
    "Vantablack Media", "Waxwing Neuromedia", "Cinderblock AI", "Silkworm Data",
    "Palladian Construction", "Emberlace Systems", "Cinderfall Energy",
    "Ironclad Agrisystems", "Cook Inlet Region, Inc.", "Ashgrave Materials",
    "Vellichor Institute", "Pale Lantern Bioethics", "Oracle Drift Systems",
    "Tessera Corponation", "Nightshade Pharmatech", "Novafold Pharmaceuticals",
    "Gravemoss Biofoundry", "Lacuna Genomics", "Pellucid Systems", "Scoria Works",
    "Slagworks Industrial", "Palladian Construction", "Rendstone Nuclear",
    "Rictus Entertainment", "Stonepath Logistics", "Sulfur Crown Agriculture",
    "Thornback Agrichemical", "Tollgate Systems", "Vespid Dynamics",
    "Charnel Propulsion", "Cormorant Naval Systems", "Ashford Signal",
    "Bathysphere Networks", "Ouroboros Energy", "Irontide Tidal Energy",
    "Dredge Mining Collective", "Doyon Limited", "Kelpline Logistics",
    "Marrowvault Cryogenics", "Ahtna Corporation", "Aleut Corporation",
    "Arctic Slope Regional Corporation", "Bering Straits Native Corporation",
    "Bristol Bay Native Corporation", "Calista Corporation", "Carrion Defense Works",
    "Chugach Alaska Corporation", "Cinderblock AI", "Crestfall Aquaculture",
    "Pelican Drift Aquatics", "Ringo Corponation", "Sealaska Corporation",
    "Koniag Incorporated", "Nana Regional Corporation", "Meridian Orbital Dynamics",
    "Liang-Petrova Consortium",
]

SCRIPT_GENERATED_PATTERN = re.compile(
    r'^(' + '|'.join(re.escape(c) for c in KNOWN_CORPS) + r')\s*\(',
    re.IGNORECASE
)


def is_script_generated_affiliation(aff_str):
    """Returns True if the affiliation string looks like it was generated by this script."""
    if not aff_str:
        return False
    return bool(SCRIPT_GENERATED_PATTERN.match(aff_str.strip()))


def process_file(filepath):
    """Process a single character file. Returns (modified, tier_int) or (False, None) on error."""
    try:
        with open(filepath, encoding="utf-8") as f:
            data = json.load(f)
    except Exception as e:
        print(f"  SKIP (parse error): {filepath} — {e}")
        return False, None

    modified = False
    current_aff = data.get("affiliation") or ""

    # If the affiliation was previously written by this script, temporarily clear it
    # so tier assignment uses the original role/desc data, not the script-generated corp
    aff_was_script_generated = is_script_generated_affiliation(current_aff)
    if aff_was_script_generated:
        data_for_tier = dict(data)
        data_for_tier["affiliation"] = ""
        tier = assign_tier(data_for_tier)
    else:
        tier = assign_tier(data)
    tier_str = str(tier)

    # Update or set tier field
    existing_tier = data.get("tier")
    if existing_tier != tier_str:
        data["tier"] = tier_str
        modified = True

    # If tier is <= 2 but affiliation was script-generated from a prior run, clear it
    if tier <= 2 and aff_was_script_generated:
        data["affiliation"] = ""
        current_aff = ""
        modified = True
    else:
        current_aff = data.get("affiliation") or ""

    # Handle affiliation for tier 3+ characters
    if tier >= 3:
        if is_freelance_or_independent(data):
            # Keep independent — do not assign corponation
            # But if they had a script-generated corp aff, clear it
            if aff_was_script_generated:
                data["affiliation"] = ""
                modified = True
        elif aff_was_script_generated or not has_corporate_affiliation(data):
            # Re-assign if affiliation was script-generated (may have been wrong)
            # or no recognized corp affiliation exists yet
            # Use data with cleared aff to avoid re-selecting based on old script-written corp name
            data_for_corp = dict(data) if aff_was_script_generated else data
            if aff_was_script_generated:
                data_for_corp["affiliation"] = ""
            corp = pick_corponation(data_for_corp)
            new_aff = build_affiliation_with_corp(data, corp, tier)
            if new_aff != current_aff:
                data["affiliation"] = new_aff
                current_aff = new_aff
                modified = True

    if modified:
        try:
            with open(filepath, "w", encoding="utf-8") as f:
                json.dump(data, f, ensure_ascii=False, indent=2)
        except Exception as e:
            print(f"  ERROR writing {filepath}: {e}")
            return False, tier

    return modified, tier


def main():
    files = glob.glob(os.path.join(PEOPLE_DIR, "*.json"))
    total = len(files)
    print(f"Found {total} people JSON files.")
    print(f"Processing...\n")

    tier_counts = {1: 0, 2: 0, 3: 0, 4: 0, 5: 0}
    updated_count = 0
    skipped_count = 0

    for i, filepath in enumerate(files, 1):
        modified, tier = process_file(filepath)
        if tier is None:
            skipped_count += 1
            continue
        tier_counts[tier] += 1
        if modified:
            updated_count += 1

        if i % 100 == 0:
            print(f"  Processed {i}/{total}...")

    print(f"\n{'='*50}")
    print(f"COMPLETE: {total} files processed")
    print(f"  Updated: {updated_count}")
    print(f"  Skipped (errors): {skipped_count}")
    print(f"\nTier Distribution:")
    for t in sorted(tier_counts):
        count = tier_counts[t]
        pct = count / max(total - skipped_count, 1) * 100
        print(f"  Tier {t}: {count:4d} ({pct:.1f}%)")
    print(f"{'='*50}")


if __name__ == "__main__":
    main()
