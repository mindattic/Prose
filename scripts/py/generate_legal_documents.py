"""Generate legal system documents for the GLMZ world."""
import json
import os
import uuid

DOCS_DIR = os.path.join(os.path.dirname(__file__), "..", "..", "engine", "data", "documents")

documents = [
    {
        "name": "Jurisdictional Sovereignty and the County Line Problem",
        "document_type": "legal analysis",
        "author": "Dr. Emeka Osei-Lindqvist, GLMZ Institute of Corporate Governance",
        "date": "2224-03-12",
        "classification": "public",
        "body": """The Great Lakes Metropolitan Zone contains sixty-four registered corponations, each exercising sovereign jurisdiction over its territorial holdings. This sovereignty is not metaphorical. Each corponation maintains its own security apparatus, judicial system, detention facilities, and criminal database. A citizen who commits assault in Tessera territory and crosses into Ironclad Agrisystems holdings has, in the most literal legal sense, crossed an international border.

This creates what enforcement professionals call the County Line Problem: pursuit authority terminates at territorial boundaries. A Tessera Security officer chasing a suspect through the Narrows cannot follow that suspect into an Ironclad-administered block without triggering a diplomatic incident. The suspect knows this. Everyone knows this.

The practical effect is that crime in the GLMZ operates in isolated bureaucratic bubbles. Your criminal record with Slagworks Industrial may be catastrophic \u2014 multiple warrants, flagged biometrics, shoot-on-sight classification \u2014 while your profile with the neighboring Pinnacle Holdings is pristine. The databases do not synchronize. They are not required to. Each corponation considers its criminal intelligence a proprietary asset, and sharing it with a rival would be a competitive disadvantage.

For low-threat individuals, pursuit simply stops at the boundary. Security teams radio in a code, log the escape vector, and return to their patrol. The paperwork required to initiate a cross-jurisdictional extradition request takes between six and eighteen months, and approval rates hover around 12%. Most corponations consider the administrative burden greater than the value of apprehending a petty offender.

For high-threat individuals \u2014 those who have caused significant financial damage, compromised proprietary data, or killed corporate personnel \u2014 the calculus changes. Corponations maintain Extraction Compacts: bilateral agreements that allow limited pursuit into partner territory under specific conditions. These compacts are rare, expensive, and politically fraught. Activating one signals to the entire GLMZ that someone important is angry.

The result is a tiered enforcement reality:

TIER 1 (Nuisance): Pursuit terminates at boundary. Incident logged. No follow-up.
TIER 2 (Notable): Pursuit terminates. Biometric flag distributed to allied corponations. Passive detection only.
TIER 3 (Significant): Cross-jurisdictional warrant requested. 6-18 month processing. May be approved.
TIER 4 (Critical): Extraction Compact activated. Pursuit continues with partner authorization. Target is typically a corporate spy, mass saboteur, or someone who killed the wrong person.
TIER 5 (Existential): All jurisdictional protocols suspended. Bounty issued. Private contractors engaged. The target has done something that makes every corponation nervous.

The space between Tier 1 and Tier 3 is where freelancers live. It is the bureaucratic crack in the wall that allows an entire shadow economy to exist. A runner who steals data from a Tessera subsidiary and delivers it to an Ironclad client has committed a crime in Tessera territory and performed a service in Ironclad territory. Tessera wants them arrested. Ironclad wants them paid. Neither jurisdiction has a mechanism to reconcile this contradiction, and neither has an incentive to create one.

This is not a flaw in the system. It is the system. Corponations benefit from the shadow economy as much as freelancers do. The jurisdictional gaps provide plausible deniability for corporate espionage, allow informal market corrections that formal channels cannot facilitate, and create a pressure valve for inter-corporate conflict that might otherwise escalate to direct military confrontation.

The freelancer is not outside the law. The freelancer is inside a gap in the law that every corponation has agreed, through deliberate inaction, to preserve.""",
        "tags": ["legal", "jurisdiction", "corponation", "freelancer", "shadow_economy", "enforcement", "sovereignty"],
        "related_entities": ["GLMZ", "Tessera Corponation", "Ironclad Agrisystems", "Slagworks Industrial", "Pinnacle Holdings"]
    },
    {
        "name": "The Shadow Economy: Informal Markets and Jurisdictional Arbitrage",
        "document_type": "economic analysis",
        "author": "Yuki Alvarez-Nkemelu, Shadow Market Research Initiative",
        "date": "2225-07-19",
        "classification": "restricted",
        "body": """The shadow economy of the GLMZ is not an underground market. It is an economy that exists in the spaces between legitimate economies \u2014 in the jurisdictional seams where no single corponation\u2019s law applies cleanly.

Conservative estimates place shadow economy transactions at \u03a640 billion annually, approximately 18% of the GLMZ\u2019s total economic output. This figure is almost certainly low, as the most sophisticated transactions are designed to be invisible to the measurement tools of any single jurisdiction.

The shadow economy operates on three primary mechanisms:

JURISDICTIONAL ARBITRAGE: Exploiting differences between corponation legal frameworks. An action that is criminal in one territory may be legal, tolerated, or unregulated in another. Freelancers who understand these differences can structure operations to minimize legal exposure while maximizing profit. A data extraction performed in Tessera space becomes a consulting delivery in Axiom space becomes a market research product in Slagworks space. Same data. Three different legal classifications. Three different tax implications.

REPUTATIONAL SEGMENTATION: Because criminal databases are proprietary and non-synchronized, individuals can maintain multiple reputational profiles simultaneously. A freelancer might be a wanted felon in two jurisdictions, a licensed contractor in three others, and an unknown entity in the remaining fifty-nine. Managing these profiles \u2014 knowing which territories are safe, which are hostile, which are ambiguous \u2014 is a core professional skill.

ENFORCEMENT GAPS: The physical spaces between corponation territories \u2014 contested blocks, neutral zones, infrastructure corridors \u2014 operate under reduced or absent law enforcement. These gaps are not lawless. They develop their own informal governance: market rules, reputation systems, community enforcement. The Burnished Market, Hamtramck Enclave, and portions of the Shelf operate primarily on informal governance rather than corporate jurisdiction.

The freelancer class \u2014 runners, fixers, specialists, contractors \u2014 exists because the jurisdictional architecture of the GLMZ makes their existence inevitable. They are not rebels. They are not criminals in any universal sense. They are rational economic actors operating in the spaces that the system itself created.

Attempts to eliminate the shadow economy have failed consistently because corponations benefit from it. When Tessera Corponation needs industrial intelligence about a competitor, it cannot send its own personnel \u2014 that would be an act of corporate war. But it can hire a freelancer who operates across jurisdictional lines, maintains plausible deniability, and disappears into the seams afterward. The freelancer is a tool of corporate competition, and the jurisdictional gaps are the workshop where that tool is built.

The shadow economy is not parasitic on the formal economy. It is symbiotic. Remove the freelancers and the corponations would need to fight their own shadow wars directly, which is more expensive, more visible, and more likely to escalate into the kind of open conflict that destroyed Gary in 2171.""",
        "tags": ["economics", "shadow_economy", "freelancer", "jurisdiction", "corponation", "arbitrage"],
        "related_entities": ["GLMZ", "Tessera Corponation", "Axiom", "Slagworks Industrial", "The Burnished Market", "Hamtramck Enclave", "The Shelf"]
    },
    {
        "name": "Corponation Security Force Protocols: Cross-Boundary Engagement Rules",
        "document_type": "security manual excerpt",
        "author": "GLMZ Inter-Corporate Security Standards Board",
        "date": "2223-11-01",
        "classification": "internal",
        "body": """SECTION 7: TERRITORIAL BOUNDARY ENGAGEMENT

7.1 HARD STOP PROTOCOL
All pursuit operations terminate at recognized corponation territorial boundaries unless an active Extraction Compact is in effect. Upon reaching a boundary, pursuing units will:
(a) Halt forward movement
(b) Log the suspect\u2019s exit vector, timestamp, and last known biometric signature
(c) Issue a Code BOUNDARY to dispatch
(d) Return to assigned patrol zone within 15 minutes

Violation of the Hard Stop Protocol constitutes unauthorized entry into foreign sovereign territory and may be classified as an act of corporate aggression under the Gradient Compact.

7.2 THREAT ESCALATION MATRIX
Response level determines post-boundary options:

GREEN (Property crime < \u03a61,000): Log and close. No cross-boundary action.
YELLOW (Property crime > \u03a61,000 or assault): Log and distribute biometric flag to allied jurisdictions. Passive detection only.
ORANGE (Major property damage, data theft, or aggravated assault): Initiate cross-jurisdictional warrant request through Legal Affairs. Estimated processing: 6-18 months.
RED (Critical infrastructure damage, corporate espionage, or homicide of personnel): Activate Extraction Compact if available. If no compact exists with the destination territory, escalate to Corporate Council for emergency authorization.
BLACK (Existential threat to corporate operations): All protocols suspended. Director-level authorization required. Bounty issuance authorized. Private military contractor engagement authorized.

7.3 THE GRAY ZONE
Security personnel will encounter situations where a suspect is within visual range but has crossed into disputed or neutral territory. In these situations:
\u2014 DO NOT pursue into neutral zones without Zone Authority clearance
\u2014 DO NOT engage suspects who have entered rival territory, even if they are visible and within weapons range
\u2014 DO NOT communicate pursuit information to rival security forces without Legal Affairs approval
\u2014 DO maintain observation from your side of the boundary and log all activity

7.4 FREELANCER CLASSIFICATION
Individuals identified as freelancers (unlicensed contractors, runners, fixers) operating across jurisdictional lines are to be classified based on their current action, not their historical profile. A freelancer with outstanding warrants in another jurisdiction but committing no offense in your territory is NOT a valid pursuit target. Your jurisdiction, your laws, your warrants only.

This is not a courtesy. This is sovereign law. Arresting someone based on another corponation\u2019s warrant without a valid extradition agreement is kidnapping under GLMZ Inter-Corporate Convention Article 14.""",
        "tags": ["legal", "security", "protocol", "jurisdiction", "enforcement", "boundary", "freelancer"],
        "related_entities": ["GLMZ", "The Gradient Compact"]
    },
    {
        "name": "The Extraction Compact System: When Borders Open",
        "document_type": "legal framework analysis",
        "author": "Ingrid Matsuda-Okonkwo, Corporate Law Review",
        "date": "2225-02-28",
        "classification": "public",
        "body": """An Extraction Compact is a bilateral agreement between two corponations that permits limited cross-boundary pursuit and detention under specific, pre-negotiated conditions. As of 2225, there are 247 active Extraction Compacts in the GLMZ, covering approximately 31% of possible corponation pairings.

The remaining 69% of corponation pairs have no pursuit agreement whatsoever. If you commit a crime in one of these territories and reach the other, you are, for all practical purposes, free.

COMPACT ACTIVATION REQUIREMENTS:
1. The offense must meet the compact\u2019s minimum severity threshold (varies by agreement)
2. The pursuing corponation must file a formal activation request with the target corponation\u2019s Legal Liaison Office
3. The target corponation must approve the request (approval is not guaranteed)
4. Approved pursuit must be conducted by a joint team including personnel from both corponations
5. The suspect must be remanded to the requesting corponation within 72 hours of capture
6. All costs of the operation are borne by the requesting corponation

In practice, compact activation takes between 4 and 48 hours. For time-sensitive pursuits, this delay is often fatal to the operation. Freelancers who understand the compact network \u2014 who knows which corponations have agreements with which, and what the activation thresholds are \u2014 can plan escape routes that exploit gaps in the compact coverage.

The most sophisticated runners maintain what the shadow economy calls a Compact Map: a mental or digital model of every active agreement, every threshold, every processing delay. Knowing that Tessera and Ironclad have a compact but that the activation threshold is corporate espionage and above means a runner who only committed theft can cross from Tessera into Ironclad territory and be safe from organized pursuit.

Compact Maps are valuable intelligence. They are bought, sold, and traded in the shadow economy. They go stale quickly \u2014 compacts are renegotiated annually, and new ones are established or allowed to lapse based on shifting corporate alliances. A runner operating on last year\u2019s map is a runner who gets caught.

NOTABLE GAPS:
\u2014 No corponation has an active Extraction Compact with the Highland Park Autonomous Zone, which operates under its own governance structure
\u2014 The Shelf\u2019s fragmented ownership (portions claimed by seven different corponations) makes compact activation nearly impossible in practice
\u2014 Neutral zones and contested territories are explicitly excluded from all compacts
\u2014 The Hamtramck Enclave has negotiated a unique status: no corponation may pursue or detain within its boundaries regardless of compact status""",
        "tags": ["legal", "extraction_compact", "jurisdiction", "corponation", "enforcement", "freelancer", "pursuit"],
        "related_entities": ["GLMZ", "Tessera Corponation", "Ironclad Agrisystems", "Highland Park Autonomous Zone", "The Shelf", "Hamtramck Enclave"]
    },
    {
        "name": "Criminal Profile Fragmentation: How One Person Can Be Six People",
        "document_type": "criminology paper",
        "author": "Dr. Kwame Petrov-Nguyen, GLMZ Criminological Institute",
        "date": "2224-09-15",
        "classification": "public",
        "body": """In a traditional nation-state, a citizen has one criminal record. In the GLMZ, a person may have sixty-four \u2014 one for each corponation that maintains a criminal database.

These records are not synchronized. They are not shared. They are not compatible. Each corponation uses its own database architecture, its own classification system, its own biometric protocols. Tessera uses a 47-point facial recognition system. Slagworks uses gait analysis. Axiom uses neural-pattern authentication. A person flagged in one system may be invisible to another.

This creates what we term Criminal Profile Fragmentation (CPF): the condition in which a single individual exists as multiple distinct legal entities across different jurisdictions, each with its own history, threat assessment, and warrant status.

A typical freelancer\u2019s CPF profile might look like this:

TESSERA CORPONATION: Wanted. Three outstanding warrants (data theft, trespass, destruction of property). Biometric flag active. Shoot-to-detain authorization.
IRONCLAD AGRISYSTEMS: Clean. No record. No flags.
SLAGWORKS INDUSTRIAL: Licensed contractor. Active security clearance (Level 2). Positive employment history.
AXIOM: Person of interest. One closed investigation (insufficient evidence). Passive monitoring.
PINNACLE HOLDINGS: Unknown. No biometric match in system.

This is the same person. Five different corponations. Five completely different legal realities. The freelancer who manages their CPF effectively \u2014 who knows which territories are safe, which are dangerous, which are ambiguous \u2014 can operate indefinitely.

CPF management is a professional skill. Fixers specialize in it. They know which corponation databases have been updated recently, which ones have gaps, which ones can be manipulated through social engineering or bribery. A good fixer can get your Tessera warrant downgraded from RED to YELLOW in six weeks. A great fixer can get it expunged entirely \u2014 not deleted, but reclassified as resolved, which is harder to detect.

The corponations are aware of CPF. They could, in theory, create a unified criminal database. They choose not to. A unified database would require data sharing, which would expose intelligence sources. It would require standardized biometrics, which would give competitors insight into security capabilities. And it would eliminate the jurisdictional gaps that corponations themselves exploit when they need shadow economy services.

The system is broken by design. The cracks are features.""",
        "tags": ["legal", "criminology", "identity", "jurisdiction", "corponation", "freelancer", "biometrics", "profile"],
        "related_entities": ["GLMZ", "Tessera Corponation", "Ironclad Agrisystems", "Slagworks Industrial", "Axiom", "Pinnacle Holdings"]
    },
    {
        "name": "The Gradient Compact: GLMZ Inter-Corporate Convention",
        "document_type": "legal treaty summary",
        "author": "GLMZ Administrative Authority, Office of Corporate Relations",
        "date": "2220-01-01",
        "classification": "public",
        "body": """The Gradient Compact is the foundational legal framework governing relations between corponations within the Great Lakes Metropolitan Zone. Ratified in 2187 following the Corporate Wars, it establishes the minimum standards for territorial sovereignty, inter-corporate conduct, and the rights of individuals moving between jurisdictions.

KEY PROVISIONS:

ARTICLE 1: TERRITORIAL SOVEREIGNTY
Each corponation exercises sovereign authority within its registered territorial holdings. This authority includes but is not limited to: law enforcement, taxation, infrastructure management, environmental regulation, and the administration of justice. No corponation may exercise sovereign authority within another\u2019s territory without explicit bilateral agreement.

ARTICLE 3: FREEDOM OF TRANSIT
Individuals may move freely between corponation territories. No corponation may restrict entry based on an individual\u2019s criminal status in another jurisdiction. This provision was fiercely debated and narrowly adopted. It remains the single most important legal protection for freelancers, though it was not designed with freelancers in mind. It was designed to prevent corponations from using border controls as economic weapons against each other\u2019s workforce.

ARTICLE 7: PROHIBITION OF CROSS-BOUNDARY FORCE
No corponation security force may pursue, detain, or use force against any individual within another corponation\u2019s territory without authorization. Violation constitutes an act of corporate aggression and may trigger retaliatory measures under Article 12.

ARTICLE 9: EXTRADITION FRAMEWORK
Corponations may negotiate bilateral Extraction Compacts governing the transfer of wanted individuals. These compacts are voluntary, revocable, and subject to annual review. No corponation is obligated to surrender any individual to another jurisdiction.

ARTICLE 14: INDIVIDUAL PROTECTIONS
Individuals detained by a corponation security force are subject to that corponation\u2019s judicial system only. Detention based solely on warrants issued by other jurisdictions is prohibited without a valid extradition agreement. Unauthorized detention of this nature is classified as kidnapping.

ARTICLE 18: NEUTRAL ZONES
Designated neutral zones within the GLMZ operate under reduced corporate authority. The Burnished Market, Hamtramck Enclave, and seventeen other registered neutral zones are governed by local authority structures recognized by the Compact. Corporate security forces may not operate within neutral zones without Zone Authority approval.

The Gradient Compact is not a constitution. It is a ceasefire agreement that has slowly evolved into a framework for coexistence. Its protections are not idealistic \u2014 they are pragmatic compromises between entities that would prefer to have no restrictions at all but recognize that unrestricted corporate sovereignty leads to the kind of warfare that destroyed Gary, collapsed the Indiana corridor, and killed an estimated 340,000 people between 2168 and 2174.""",
        "tags": ["legal", "treaty", "gradient_compact", "corponation", "sovereignty", "jurisdiction", "rights", "neutral_zone"],
        "related_entities": ["GLMZ", "The Gradient Compact", "The Burnished Market", "Hamtramck Enclave", "Gary Ruins"]
    },
    {
        "name": "Freelancer Legal Status: Neither Criminal Nor Citizen",
        "document_type": "legal opinion",
        "author": "Magistrate Adaeze Khoury-Yamamoto, GLMZ Administrative Tribunal",
        "date": "2225-06-03",
        "classification": "public",
        "body": """The legal status of freelancers in the GLMZ is, to use the technical term, a mess.

A freelancer is an individual who performs contracted work across multiple corponation jurisdictions without permanent employment or citizenship affiliation with any single corponation. This is not illegal. The Gradient Compact\u2019s Freedom of Transit provisions explicitly protect the right of individuals to move between territories and to engage in economic activity.

Where the law becomes complicated is in the nature of the work. A freelancer hired to deliver a package from point A to point B is a courier. A freelancer hired to extract proprietary data from a competitor\u2019s server is a corporate spy. The same person may perform both tasks in the same week. Their legal status changes not based on who they are but on what they are doing and where they are doing it.

This creates a legal paradox: a freelancer can be simultaneously a legitimate contractor (in the jurisdiction that hired them), a criminal (in the jurisdiction they are targeting), and a neutral party (in every other jurisdiction they pass through). The law treats them differently at every territorial boundary they cross.

CORPORATE CITIZENSHIP VS. INDEPENDENT STATUS:
Most GLMZ residents hold Corporate Citizenship with one or more corponations, which provides legal protections, healthcare access, UBC stipend eligibility, and judicial standing. Freelancers typically hold Independent Status \u2014 they are residents of the GLMZ but citizens of no corponation.

Independent Status is not a second-class designation. It provides Freedom of Transit protections, access to neutral zone services, and immunity from corporate conscription. But it also means no corporate healthcare, no UBC stipend, no legal representation in corporate courts, and no protection from corporate security beyond the basic Gradient Compact provisions.

THE PRACTICAL REALITY:
Freelancers exist because the system needs them. Corponations cannot conduct shadow operations through official channels. They cannot steal from competitors, sabotage rival infrastructure, or gather intelligence without breaking the Gradient Compact. Freelancers do these things for them, absorbing the legal risk in exchange for payment.

In return, the system provides freelancers with the jurisdictional gaps necessary for survival. Freedom of Transit means they can run. Profile Fragmentation means they can hide. Neutral zones mean they can rest. The shadow economy means they can work.

It is a relationship of mutual exploitation disguised as mutual indifference. The corponations pretend freelancers do not exist. The freelancers pretend the corponations are not their clients. Everyone benefits. No one admits it.""",
        "tags": ["legal", "freelancer", "status", "citizenship", "jurisdiction", "corponation", "independent"],
        "related_entities": ["GLMZ", "The Gradient Compact"]
    },
]

created = 0
for doc in documents:
    data = {
        "id": uuid.uuid4().hex,
        "name": doc["name"],
        "type": "document",
        "document_type": doc["document_type"],
        "author": doc["author"],
        "date": doc["date"],
        "classification": doc["classification"],
        "body": doc["body"],
        "tags": doc["tags"],
        "related_entities": doc.get("related_entities", [])
    }
    fp = os.path.join(DOCS_DIR, f'{data["id"]}.json')
    with open(fp, "w", encoding="utf-8") as f:
        json.dump(data, f, indent=2, ensure_ascii=False)
    created += 1
    print(f"  Created: {doc['name']}")

print(f"\nTotal legal documents created: {created}")
