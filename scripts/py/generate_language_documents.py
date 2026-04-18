"""Generate language evolution documents for the GLMZ world -- how people talk in 2200."""
import json
import os
import uuid

DOCS_DIR = os.path.join(os.path.dirname(__file__), "..", "..", "engine", "data", "documents")

documents = [
    {
        "name": "Linguistic Drift in the GLMZ: How a Megacity Talks",
        "document_type": "linguistic study",
        "author": "Dr. Fatima Lindqvist-Okafor, GLMZ Linguistics Department, University Spine",
        "date": "2225-04-22",
        "classification": "public",
        "body": """The Great Lakes Metropolitan Zone is the most linguistically compressed environment in human history. Twelve million people from every surviving culture on Earth, packed into a single megacity, speaking to each other every day for two centuries. The result is not a new language. It is a new way of using all languages simultaneously.

GLMZ English \u2014 the baseline communication medium \u2014 has absorbed vocabulary from every major language group on the planet. This absorption is not academic. It is pragmatic. People use the word that works, regardless of its origin. A Shelf resident might use a Yoruba noun, a Mandarin verb modifier, a Hindi intensifier, and an English sentence structure in a single utterance, and every person within earshot will understand them perfectly.

This is not pidgin. This is not creole. This is accelerated linguistic evolution driven by density, necessity, and the absence of any central authority enforcing language standards. Corponations tried to standardize corporate communications in the 2180s. The workers ignored them. Language is the one thing that corponation sovereignty cannot touch.

KEY FEATURES OF GLMZ SPEECH (2225):

CODE-MIXING AS DEFAULT: Monolingual speech is unusual in the GLMZ. Most residents operate in at least three linguistic registers \u2014 a home language (often tied to ancestry), GLMZ English (the street baseline), and corporate standard (the formal register used in employment contexts). Switching between these registers mid-sentence is not a sign of confusion. It is a sign of fluency.

COMPRESSION: GLMZ speech is fast and compressed. Articles are frequently dropped. Pronouns are optional when context is clear. Verb tenses are simplified \u2014 the present tense does most of the work, with temporal markers borrowed from Mandarin and Yoruba replacing complex conjugation. The sentence "I went to the market yesterday and bought three things" becomes something closer to "Market yesterday, got three things" in casual Shelf speech.

DISTRICT DIALECTS: Each district has developed its own linguistic character. The Shelf speaks a compressed, rapid-fire dialect heavy on Yoruba and Tagalog loanwords. Geartown\u2019s speech is peppered with technical jargon that has evolved into general vocabulary \u2014 "torqued" means angry, "calibrated" means reliable, "slag" means worthless. The Spires use a deliberately formal register that signals corporate affiliation. Hamtramck Enclave preserves the most diverse linguistic mix, with entire conversations shifting languages multiple times.

BCI INFLUENCE: Brain-computer interfaces have introduced a new dimension to language. BCI users can transmit emotional context alongside spoken words \u2014 a kind of linguistic metadata that adds tone, intensity, and sincerity markers to speech. This has created a generational divide: older speakers who rely on vocal tone and body language, and younger speakers who assume their audience is receiving BCI emotional overlay. The result is that younger GLMZ speech sounds flat and affectless to older ears, because the emotional content is being transmitted on a channel that non-augmented listeners cannot access.

CORPORATE SPEAK: Each corponation has developed its own internal dialect. Tessera employees use a clipped, efficiency-focused register that strips all emotional content from communication. Slagworks personnel speak in a technical dialect so dense with industry jargon that outsiders cannot follow it. Axiom\u2019s corporate language is deliberately ambiguous \u2014 a communication style designed to say nothing while appearing to say everything. These corporate dialects are not just habits. They are identity markers. How you speak tells everyone who you work for.""",
        "tags": ["language", "linguistics", "culture", "glmz", "dialect", "bci", "evolution"],
        "related_entities": ["GLMZ", "The Shelf", "Geartown", "The Spires", "Hamtramck Enclave", "Tessera Corponation", "Slagworks Industrial", "Axiom"]
    },
    {
        "name": "Slang Lexicon of the GLMZ (2225 Edition)",
        "document_type": "reference guide",
        "author": "Compiled by the GLMZ Oral History Project",
        "date": "2225-01-15",
        "classification": "public",
        "body": """A working glossary of common GLMZ slang, street terminology, and evolved vocabulary as of 2225. This is not exhaustive \u2014 the lexicon changes faster than any publication can track.

GENERAL VOCABULARY:
\u2022 Chrome \u2014 cyberware; also used as an adjective meaning enhanced, upgraded, or superior. "That\u2019s chrome" = that\u2019s excellent.
\u2022 Meat \u2014 unaugmented flesh. Not derogatory in most contexts. "Meat hand" = natural hand, as opposed to a prosthetic.
\u2022 Ghost \u2014 (v.) to disappear from surveillance. "She ghosted after the Tessera job." (n.) someone who has no digital footprint.
\u2022 Burn \u2014 to use up a contact, safe house, or identity. "That fixer\u2019s burned" = that fixer has been compromised.
\u2022 Slag \u2014 worthless, broken, or contemptible. From Geartown foundry culture. "Slag output" = garbage work.
\u2022 Torqued \u2014 angry, agitated. Also from Geartown. "Don\u2019t get torqued" = calm down.
\u2022 Calibrated \u2014 reliable, trustworthy, well-prepared. "She\u2019s calibrated" = she can be trusted.
\u2022 Drift \u2014 to move between corponation territories. "Drifting Tessera-to-Ironclad tonight." Also: a person with no fixed affiliation.
\u2022 The Line \u2014 a corponation territorial boundary. "Hit the line" = reach the border. "Over the line" = in another jurisdiction.
\u2022 Compact \u2014 (n.) an Extraction Compact. "They\u2019ve got a compact" = pursuit can follow you across that border.
\u2022 Clean \u2014 no warrants, no flags, no criminal profile in a given jurisdiction. "I\u2019m clean with Slagworks."
\u2022 Hot \u2014 wanted, flagged, under active pursuit. "Running hot in Tessera space."
\u2022 Fade \u2014 to cross a jurisdictional boundary to escape pursuit. "Faded across the Ironclad line."
\u2022 Profile \u2014 your criminal/reputation status with a specific corponation. "My Axiom profile is yellow."

MONEY AND ECONOMICS:
\u2022 Quanta (\u03a6) \u2014 the universal GLMZ currency. Always abbreviated with the symbol.
\u2022 Stipend \u2014 Universal Basic Compensation. \u03a6120/month. Enough to not starve. Not enough to live.
\u2022 Under the Compact \u2014 legal, above-board work. "Earning under the Compact" = legitimate employment.
\u2022 Shadow-side \u2014 shadow economy work. "Shadow-side Quanta" = money earned through freelance work.
\u2022 A clean \u03a6 \u2014 money that can\u2019t be traced to illegal activity.

PEOPLE AND ROLES:
\u2022 Runner \u2014 a freelancer who performs direct operations (extraction, delivery, sabotage).
\u2022 Fixer \u2014 a freelancer who arranges contracts, manages profiles, and connects runners with clients.
\u2022 Corpo \u2014 a corporate employee. Neutral to mildly contemptuous depending on context.
\u2022 Suit \u2014 corporate management specifically. More contemptuous than corpo.
\u2022 Badge \u2014 corporate security officer. "Tessera badges" = Tessera security.
\u2022 Shelf rat \u2014 a Shelf resident. Self-applied with pride, contemptuous when used by outsiders.
\u2022 Spire kid \u2014 someone from the wealthy Spires district. Implies privilege and naivete.

TECHNOLOGY:
\u2022 Jack \u2014 a BCI port or the act of connecting. "Jack in" = connect to a neural network.
\u2022 Wired \u2014 augmented with cyberware. "She\u2019s wired heavy" = extensive augmentation.
\u2022 Flatline \u2014 death, especially from cyberware malfunction or neural overload.
\u2022 Signal \u2014 a BCI communication channel. "On signal" = communicating via neural link.
\u2022 Glitch \u2014 a cyberware malfunction. Also: a person behaving erratically. "He\u2019s glitching."

PLACES AND MOVEMENT:
\u2022 The Zone \u2014 the GLMZ itself. "Born in the Zone" = native.
\u2022 Outside \u2014 anything beyond the GLMZ\u2019s boundaries. Carries connotations of danger and strangeness.
\u2022 Topside \u2014 the upper levels of the Shelf and Spires. Where the money is.
\u2022 Down below \u2014 Deepwell Station, Irkalla, and other underground/lower-level districts.
\u2022 The Seam \u2014 the jurisdictional gap between two corponation territories. "Working the seam" = operating in jurisdictional gray areas.""",
        "tags": ["language", "slang", "vocabulary", "culture", "glmz", "reference"],
        "related_entities": ["GLMZ", "The Shelf", "Geartown", "The Spires", "Tessera Corponation", "Ironclad Agrisystems", "Slagworks Industrial", "Axiom", "Deepwell Station"]
    },
    {
        "name": "The Death of Monolingualism: Language in the Ubiquitous Diaspora",
        "document_type": "cultural essay",
        "author": "Professor Kenji Achebe-Svensson, Department of Cultural Studies, University Spine",
        "date": "2224-11-08",
        "classification": "public",
        "body": """Nobody in the GLMZ speaks one language. This is not an exaggeration. Even the most isolated, least educated, most stubbornly monolingual individual in the Zone speaks at least two registers of English \u2014 the formal corporate standard they hear in announcements and the street variant they use with their neighbors. Most people operate in three to five linguistic modes without conscious effort.

This is the legacy of the Ubiquitous Diaspora.

When the climate migrations, corporate consolidations, and territorial collapses of the 21st and 22nd centuries compressed Earth\u2019s population into megacity clusters, they created linguistic pressure cookers. The GLMZ absorbed waves of migration from every continent over a span of eighty years. There was no time for gradual assimilation. There was no dominant culture to assimilate into. Everyone arrived at once, and everyone needed to communicate immediately.

The result was not a melting pot. It was a mosaic that learned to read itself.

HERITAGE LANGUAGES survive but transform. A family that emigrated from Lagos four generations ago still speaks Yoruba at home, but it is a Yoruba filtered through two centuries of GLMZ life \u2014 peppered with English technical terms, Mandarin food vocabulary, and Hindi affectionate terms picked up from neighbors. The language is alive precisely because it is impure. Pure languages die in the GLMZ. Languages that absorb survive.

NAMES reflect this reality. The Ubiquitous Diaspora means that heritage comes from unexpected global combinations. A person named Kenji Achebe-Svensson is not unusual \u2014 they are normal. Hyphenated surnames, triple-barrel names, and first names drawn from cultures unrelated to the family\u2019s ancestry are standard. Names are aspirational, commemorative, and accidental in equal measure. A Nigerian-Swedish family names their child Kenji because they loved a Japanese neighbor who helped them during the Shelf floods of 2209.

WRITTEN LANGUAGE has diverged from spoken language more sharply than at any point in history. Corporate communications are written in a sanitized, globally standardized English that sounds like it was generated by committee (because it was). Street writing \u2014 graffiti, message boards, personal correspondence \u2014 uses a fluid mix of scripts. A single message board post might contain Latin alphabet English, Hangul Korean, Devanagari Hindi, and emoji-derived pictograms, all in the same paragraph. BCI users increasingly bypass written language entirely, transmitting concepts directly.

The linguists at University Spine have stopped trying to classify GLMZ speech as a dialect, a creole, or a new language. They have settled on calling it an ecology: a living system of languages that interact, compete, merge, and diverge in real time, shaped by the pressures of twelve million people trying to understand each other in a city that never stops talking.""",
        "tags": ["language", "culture", "diaspora", "heritage", "names", "evolution", "glmz"],
        "related_entities": ["GLMZ", "University Spine", "The Shelf"]
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

print(f"\nTotal language documents created: {created}")
