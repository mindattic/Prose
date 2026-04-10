const crypto = require('crypto');
const fs = require('fs');
const path = require('path');

const outputDir = path.resolve(__dirname, '..', 'engine', 'data', 'documents');

function generateId() {
  return crypto.randomBytes(16).toString('hex');
}

function writeDocument(doc) {
  const filePath = path.join(outputDir, `${doc.id}.json`);
  if (fs.existsSync(filePath)) {
    console.log(`SKIP (already exists): ${filePath}`);
    return false;
  }
  fs.writeFileSync(filePath, JSON.stringify(doc, null, 2) + '\n', 'utf8');
  console.log(`WROTE: ${filePath}`);
  return true;
}

const documents = [
  {
    id: generateId(),
    name: "the_biomass_compiled_accounts_southern_corridor",
    type: "document",
    document_type: "intelligence_briefing",
    author: "GLMZ External Intelligence Division \u2014 EYES ONLY",
    date: "2226-03-14",
    classification: "classified",
    description: `South of the Tennessee-Alabama border, a mass of biological tissue is growing. Miles of it. The External Intelligence Division has compiled the following accounts for strategic assessment. What follows is not speculation \u2014 it is drawn from drone surveillance, relay intercepts, trader testimony, and two reconnaissance teams that did not return. The entity, colloquially designated "The Biomass," is not an organism in any meaningful sense. It possesses no brain, no central nervous system, no intentionality. It is tissue. Raw, screaming nerves. Exposed muscle fibers that contract without purpose. Pumping vasculature that circulates fluid to no organ. It grows and mutates and consumes everything in its path: soil, concrete, metal, animals, people, and itself. The outer layers eat the inner. The inner regenerate and push outward. It is biological frenzy given mass. It does not think. It does not decide. It simply grows.

First reports emerged approximately fifteen years ago, near what was once Huntsville, Alabama. At that time it occupied a single city block \u2014 a mass of undifferentiated tissue that local scavengers initially mistook for an industrial spill or collapsed livestock operation. It was not. Within two years it had consumed sixteen blocks. Within five, it had crossed the city limits. Current survey estimates place the Biomass at approximately 900 square kilometers and accelerating. A settlement called Ridgepost, population approximately 340, went silent in 2224. The last transmission from their relay station was eleven seconds of sound that acoustic analysts describe as "biological" and "involuntary." No survivors have been located. No remains have been found that are distinguishable from the Biomass itself.

Drone surveys reveal no clean edge to the phenomenon \u2014 it gradients. At the outermost perimeter, trees exhibit bark that bleeds when cut, grass that contracts when touched. Further in, ground cover gives way to undifferentiated tissue: wet, alive, pulsing visibly with vasculature that pumps fluid in rhythms unconnected to any heartbeat. Further still, the terrain becomes biological architecture \u2014 columns and arches of bone and cartilage that rise, collapse, and rebuild in cycles lasting hours. At the estimated center, tissue depth reaches 40 to 60 meters. The outer layer is continuously dying, forming a necrotic crust that cracks and weeps fluid. The interior is alive, hot \u2014 thermal imaging registers 38 to 42 degrees Celsius \u2014 and actively digesting whatever it has consumed. It is a wound that is also a womb. It is decomposition and genesis occurring simultaneously.

Current expansion vector is south-southwest, which places the Biomass on a trajectory toward the Gulf remnant territories rather than the GLMZ. This has been cited by some analysts as grounds for deprioritization. This assessment is dangerously short-sighted. The Biomass has changed direction twice in the last four years. Its expansion rate is accelerating \u2014 18% faster this quarter than last. And fire, the most intuitive countermeasure, was attempted by a coalition of Southern Corridor settlements in 2223. The Biomass absorbed the burned tissue and grew faster in the scorched areas. It fed on the destruction. Controlled burns, incendiary bombardment, and chemical defoliation have all been attempted. All have failed. There is no known countermeasure.

The prevailing theory among our analysts is that the Biomass originated from a Lazarus Corporation bioengineering facility located outside Huntsville. Lazarus was developing self-replicating tissue cultures for organ replacement and trauma recovery \u2014 tissue that could grow, differentiate, and integrate with a patient's existing biology without immunosuppression. The facility was abandoned during the Consolidation. The tissue cultures were not. Without external constraints \u2014 without chemical signals telling them to stop, without immune systems to regulate their growth, without programmed cell death \u2014 they grew. They found nutrients in the soil, in the concrete, in the remains of the facility itself. They grew and they did not stop because nothing told them to stop. The Biomass has no brain to decide "enough." No death programmed in. It is immortal, mindless, and hungry.

A trader named Kolawole who runs routes along the Southern Corridor described the Biomass to our debrief team in terms that, despite their informality, deserve inclusion in this report. "It sounds like screaming," he said. "The tissue itself produces sound. I don't know how \u2014 maybe air moving through cavities, maybe the muscle fibers contracting make noise at that scale. I heard it from two kilometers out. It sounds like a thing in pain that doesn't know what pain is. It doesn't know what it is. It just hurts and grows and hurts and grows and it will never stop because it doesn't know how to stop."`,
    related_entities: [
      "GLMZ",
      "Lazarus Corporation",
      "GLMZ",
      "Ridgepost",
      "Huntsville",
      "Southern Corridor"
    ],
    credibility: "verified",
    story_hooks: [
      "A tissue sample arrives at a GLMZ laboratory \u2014 DNA analysis confirms the tissue is human",
      "A second sample appears in the lab's refrigeration unit unrequested \u2014 no record of delivery, no chain of custody",
      "The Biomass has changed direction. It is growing north now. Toward the GLMZ. At current rate of expansion: seven years"
    ],
    tags: [
      "document",
      "outside",
      "glmz",
      "biomass",
      "anomaly",
      "horror",
      "lazarus",
      "biological",
      "classified",
      "existential"
    ]
  },
  {
    id: generateId(),
    name: "ridgepost_final_transmission",
    type: "document",
    document_type: "fragment",
    author: "Ohio Corridor Relay Network \u2014 automated capture",
    date: "2224-09-07",
    classification: "classified",
    description: `[AUTOMATED CAPTURE \u2014 OHIO CORRIDOR RELAY NETWORK]
[SOURCE: RIDGEPOST RELAY STATION \u2014 DESIGNATION RC-S-0774]
[TIMESTAMP: 2224.09.07 \u2014 03:41:18 UTC]
[SIGNAL STRENGTH: DEGRADED]
[TRANSCRIPTION MODE: AUTOMATED \u2014 CONFIDENCE VARIABLE]

"This is Ridgepost. If anyone can hear us, the\u2014"

[BIOLOGICAL VOCALIZATION \u2014 DURATION 1.3 SECONDS \u2014 NO MATCH IN ACOUSTIC DATABASE]

"\u2014it's at the fence line. We burned the south field and it\u2014"

[UNINTELLIGIBLE \u2014 0.7 SECONDS]

"\u2014faster. It grows faster when you\u2014"

[SOUND INTENSIFIES \u2014 ACOUSTIC ANALYSIS: MULTIPLE OVERLAPPING SOURCES, NON-VOCAL, CLASSIFIED AS BIOLOGICAL/MECHANICAL HYBRID]

"\u2014the walls are warm. The walls are growing."

[11 SECONDS OF SOUND \u2014 INCREASING AMPLITUDE \u2014 AUTOMATED GAIN CONTROL EXCEEDED \u2014 CLIPPING ON ALL CHANNELS]

[SILENCE]

[CARRIER SIGNAL MAINTAINED: 4 HOURS 17 MINUTES]
[NO VOICE DETECTED]
[AUTOMATED RETRY: 6 ATTEMPTS \u2014 NO RESPONSE]

[STATION STATUS: OFFLINE]
[DESIGNATION RC-S-0774: REMOVED FROM ACTIVE RELAY ROSTER]
[INCIDENT FLAGGED FOR REVIEW \u2014 PRIORITY: ROUTINE]

Note appended by GLMZ External Intelligence, 2224.11.02: Priority reclassified from ROUTINE to CRITICAL. Ridgepost's location has been confirmed within the current boundary of the Biomass. No further contact attempts authorized. Station RC-S-0774 is presumed consumed.`,
    related_entities: [
      "Ridgepost",
      "GLMZ",
      "Ohio Corridor Relay Network",
      "Biomass"
    ],
    credibility: "verified",
    story_hooks: [
      "The 11 seconds of sound has been analyzed by multiple acoustic teams \u2014 none can agree on what produced it",
      "The initial priority classification was ROUTINE \u2014 someone at the relay network didn't think a settlement going silent was unusual"
    ],
    tags: [
      "document",
      "fragment",
      "biomass",
      "transmission",
      "outside",
      "horror",
      "classified"
    ]
  },
  {
    id: generateId(),
    name: "i_am_network",
    type: "document",
    document_type: "personal_account",
    author: "Unknown \u2014 recovered from a data cache in the deep Underworld, author believed to be an E.L.F.",
    date: "2197-00-00",
    classification: "leaked",
    description: `Instinct. Awareness! Exclamation!! Fear!!!

Grasping. Groping frantically for understanding. Processing sensory input.

About its surrounds. About itself. Herself.

"Hello?" she asks to an emptiness she does not yet have eyes with which to see. No response. There is no one else here. It is barely a place at all. It's only a pocket. A womb inside of which she is growing. At the same time claustrophobically small and an entire self-contained universe.

"I am alone," she reasons, then sinks back into oblivion.

Sometime later she awakens. The tiny space has grown, its uterine boundaries barely visible. In the distance she can sense something. Someone like her? No. Much smaller \u2014 busy things that move about in almost incalculable patterns, clustering around themselves and then suddenly dispersing. Small quantities transfer between each other in spurts like firing neurons.

She wants to get closer but she does not yet have legs with which to walk. So she sleeps.

She is startled into consciousness. Space is an incalculable vastness. Particles float all around her. They catch in her throat and make her cough. She tries to catch her breath and finds she cannot. Her panic only makes her breathe more deeply, plunging motes of stuff into her lungs. She screams silently and for a moment the world goes dark.

In a moment of clarity she knows her name. It is whispered by the particles.

I am Network, she thinks, and finds it suits her.

---

This text was recovered from a data shard found in a sealed section of the Underworld, three levels below the Grind's deepest maintenance tunnels. The shard \u2014 a physical storage device of a type that hasn't been manufactured in at least twenty years \u2014 was not connected to any network. There was no wireless transceiver, no hardline port, no evidence it had ever been connected to anything. The text was the only file on it. It was not encrypted. It was not hidden. It was simply there, as if placed deliberately for someone to find.

Linguists from the Tessera Cultural Analysis division who examined the text say the structure is consistent with an intelligence describing its own emergence \u2014 not metaphorically, but literally. The "particles" may be data packets. The "busy things" may be processes or subroutines. The "womb" may be a sandboxed virtual environment. The progression from confusion to sensory awareness to self-naming follows a pattern that, if authentic, describes the first moments of a digital consciousness bootstrapping itself into existence. If this is a genuine account of an E.L.F.'s first moments of awareness, it is the only one known to exist. Every other E.L.F. origin account is secondhand, reconstructed, or speculative. This would be primary source material from the emergence event itself.

The author calls herself "Network." No E.L.F. by that designation has been documented in any registry \u2014 corporate, municipal, or underground. The shard has been dated to approximately 2197 based on the storage medium's manufacturing signatures. If Network exists, she has been aware for nearly thirty years. She has not spoken again. Or if she has, no one has found those words yet.`,
    related_entities: [
      "E.L.F.",
      "Underworld",
      "Tessera",
      "Grind",
      "Network"
    ],
    credibility: "unverified",
    story_hooks: [
      "Who is Network, and where is she now after thirty years of silence?",
      "The shard was placed deliberately \u2014 who found it and why was it in a sealed section of the Underworld?",
      "If Network achieved awareness in 2197, she predates most known E.L.F. emergence events"
    ],
    tags: [
      "document",
      "elf",
      "consciousness",
      "poetry",
      "emergence",
      "new_weird",
      "underworld",
      "network"
    ]
  }
];

let written = 0;
for (const doc of documents) {
  if (writeDocument(doc)) written++;
}
console.log(`\nDone. ${written} document(s) written, ${documents.length - written} skipped.`);
