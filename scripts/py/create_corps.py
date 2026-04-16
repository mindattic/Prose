"""Create Meridian Orbital Dynamics and Liang-Petrova Consortium corponation entities."""
import json
import os
import uuid

CORP_DIR = os.path.join(os.path.dirname(__file__), "..", "..", "engine", "data", "corponations")

corps = [
    {
        "name": "Meridian Orbital Dynamics",
        "full_legal_name": "Meridian Orbital Dynamics Corporation (Registered: Singapore Free Trade Zone)",
        "common_names": ["Meridian", "MOD", "\"The Climbers\" (industry slang)", "\"Tortoise Killers\" (activist pejorative)"],
        "stock_designation": "MOD.SGX \u2014 Singapore Exchange, Tier 1 Strategic Infrastructure",
        "sector": "Space infrastructure, orbital logistics, carbon nanotube manufacturing, asteroid mining support, counterweight station operations",
        "valuation": "\u03a618.4 trillion (2225). The single most valuable corporation on Earth by a factor of three.",
        "revenue": "\u03a62.1 trillion annually from elevator operations alone. Additional \u03a6800 billion from orbital manufacturing and deep-space launch services.",
        "employees": "14,000 (Anchor Station) + 2,200 (Counterweight Station) + 45,000 (global operations, Singapore HQ, manufacturing facilities)",
        "sovereign_territory": "Northern Isla Isabela, Gal\u00e1pagos Archipelago (200-year lease from Ecuador). 50km restricted naval zone surrounding the Anchor Station. Counterweight Station in geostationary orbit (sovereign by International Space Authority charter).",
        "founding_story": "Founded in 2171 by Elias Karga, a Greek-Singaporean aerospace engineer who correctly predicted that carbon nanotube tensile strength would reach the threshold required for a space elevator within twenty years. Karga spent two decades securing patents, cultivating political relationships with equatorial nations, and building the manufacturing capability for nanotube ribbon production. He died in 2199, one year before commercial operations began. His body was the third payload to ascend the elevator. He is buried in orbit.",
        "security_force": "Meridian Naval Security Division. 12 patrol vessels, 2 attack submarines, autonomous drone swarm coverage, directed-energy weapons systems. The most powerful private navy in the Pacific. Authorized to use lethal force within the restricted zone.",
        "key_detail": "Meridian Orbital Dynamics destroyed the most biodiverse island ecosystem on Earth to build the most profitable piece of infrastructure in human history. They maintain a \u03a6500 million annual Environmental Stewardship Fund. The fund\u2019s primary output is a VR recreation of the pre-construction Gal\u00e1pagos, available to schools for \u03a612,000/year.",
        "relationship_to_big_20": "Not a Big 20 member. Meridian operates outside the GLMZ corponation structure entirely. Its relationship with GLMZ corponations is transactional \u2014 it sells orbital launch capacity to anyone who can pay. Tessera, Axiom, and Slagworks are its three largest clients.",
        "full_text": "Meridian Orbital Dynamics is the company that built a ladder to the stars and stepped on an island to reach the first rung. Its annual profit exceeds the GDP of most surviving nation-states. Its founder is buried in space. Its legacy is a gift shop where tortoises used to nest.",
        "tags": ["corponation", "space_elevator", "orbital", "infrastructure", "galapagos", "singapore", "navy", "monopoly"],
        "related_entities": ["Ascension Tether (Gal\u00e1pagos Orbital Elevator)", "Tessera Corponation", "Axiom", "Slagworks Industrial", "Liang-Petrova Consortium"]
    },
    {
        "name": "Liang-Petrova Consortium",
        "full_legal_name": "Liang-Petrova Industrial Consortium Ltd. (Registered: Shanghai-Vladivostok Free Economic Zone)",
        "common_names": ["LP", "Liang-Petrova", "\"The Consortium\" (formal)", "\"LP Heavy\" (engineering circles)"],
        "stock_designation": "LPC.SHV \u2014 Shanghai-Vladivostok Exchange, Heavy Industry Tier",
        "sector": "Fusion reactor manufacturing, heavy industrial engineering, orbital construction, asteroid mining, deep-space probe systems",
        "valuation": "\u03a64.2 trillion (2225). The largest heavy-industry manufacturer on Earth.",
        "revenue": "\u03a61.1 trillion annually. Primary revenue from fusion reactor sales (civilian and military), orbital construction contracts, and asteroid mining probe systems.",
        "employees": "380,000 globally. Manufacturing hubs in Shanghai, Vladivostok, Jakarta, and the Anchor Station (Gal\u00e1pagos).",
        "sovereign_territory": "Shanghai-Vladivostok Free Economic Zone (shared sovereignty with six other consortium members). Manufacturing enclaves in Jakarta and S\u00e3o Paulo. No GLMZ territorial holdings.",
        "founding_story": "Formed in 2158 from the merger of Liang Heavy Industries (Shanghai) and Petrova Energy Systems (Vladivostok) during the Corporate Consolidation period. The merger was driven by the shared realization that fusion reactor manufacturing and heavy orbital construction were converging industries. LP built the four fusion reactors that power the Ascension Tether\u2019s Anchor Station and manufactured the counterweight station\u2019s structural frame.",
        "security_force": "LP Industrial Security. Primarily facility protection \u2014 not a military force. 12,000 security personnel across all facilities. Relies on host-nation or partner-corponation military assets for territorial defense.",
        "key_detail": "Liang-Petrova builds the machines that make other corponations\u2019 ambitions possible. They built Meridian\u2019s reactors, Tessera\u2019s orbital communications platforms, and the mining probes that extract asteroid resources for half the Big 20. They do not build consumer products. They build the things that build the things.",
        "relationship_to_big_20": "Supplier to multiple Big 20 members but not a member itself. LP maintains strategic neutrality \u2014 they sell to everyone and ally with no one. This neutrality is their most valuable asset.",
        "full_text": "The Liang-Petrova Consortium is the quiet giant of the corporate world. They do not compete for territory, do not maintain a private army, and do not appear in headlines. They build fusion reactors and orbital infrastructure. Everyone needs what they sell. Nobody wants to be their enemy.",
        "tags": ["corponation", "heavy_industry", "fusion", "orbital", "manufacturing", "shanghai", "vladivostok", "neutral"],
        "related_entities": ["Meridian Orbital Dynamics", "Ascension Tether (Gal\u00e1pagos Orbital Elevator)", "Tessera Corponation"]
    }
]

for corp in corps:
    data = {
        "id": uuid.uuid4().hex,
        "number": 0,
        "name": corp["name"],
        "full_legal_name": corp["full_legal_name"],
        "common_names": corp["common_names"],
        "stock_designation": corp["stock_designation"],
        "sector": corp["sector"],
        "valuation": corp["valuation"],
        "revenue": corp["revenue"],
        "employees": corp["employees"],
        "sovereign_territory": corp["sovereign_territory"],
        "founding_story": corp["founding_story"],
        "security_force": corp["security_force"],
        "key_detail": corp["key_detail"],
        "relationship_to_big_20": corp["relationship_to_big_20"],
        "full_text": corp["full_text"],
        "tags": corp["tags"],
        "related_entities": corp["related_entities"]
    }
    fp = os.path.join(CORP_DIR, f'{data["id"]}.json')
    with open(fp, "w", encoding="utf-8") as f:
        json.dump(data, f, indent=2, ensure_ascii=False)
    print(f"Created: {corp['name']}")

print(f"Total corponations now: {len([f for f in os.listdir(CORP_DIR) if f.endswith('.json')])}")
