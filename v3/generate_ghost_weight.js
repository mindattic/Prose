const fs = require('fs');
const path = require('path');
const crypto = require('crypto');

const outDir = path.resolve(__dirname, '..', 'engine', 'data', 'documents');

function generateId() {
  return crypto.randomBytes(16).toString('hex');
}

function writeDoc(doc) {
  const filePath = path.join(outDir, `${doc.id}.json`);
  if (fs.existsSync(filePath)) {
    console.log(`SKIP (exists): ${filePath}`);
    return false;
  }
  fs.writeFileSync(filePath, JSON.stringify(doc, null, 2) + '\n', 'utf8');
  console.log(`WROTE: ${filePath}`);
  return true;
}

const documents = [
  {
    id: generateId(),
    name: "Hand-Me-Down Minds \u2014 The Practice of Inherited BCIs",
    type: "document",
    document_type: "investigative_report",
    author: "Maren Ifechi-Johansson, independent journalist",
    date: "2226-03-14",
    classification: "public",
    description: "An estimated thirty percent of Tier 1 residents in the Shelf are running unregistered brain-computer interfaces inherited from family members. The practice is technically illegal under Lazarus licensing agreements, which require each BCI to be registered to a single user and decommissioned upon that user's death. Enforcement is nonexistent. CorpSec has never once prosecuted a case of BCI inheritance in the Shelf. The devices are too expensive for most families to replace \u2014 a new Lazarus Tier 1 BCI costs between \u03a680 and \u03a6400 depending on capability tier \u2014 and the used ones cost nothing except the courage to let a street clinic perform the transfer surgery.\n\nFactory reset is supposed to wipe the device clean. It clears stored data, personal configurations, feed preferences, and communication logs. What it does not clear are the physical neural pathway adaptations that form in the BCI's substrate over years of continuous use. The device literally reshapes itself to match its host's brain. When that host dies and the device moves to a new skull, those physical formations remain. They are hardware, not software. No reset touches them.\n\nThe Shelf has a word for what happens next: ghost weight. The new user carries the residual neural habits of the previous owner. A daughter inherits her mother's BCI and finds herself reaching for masala chai her mother drank every morning \u2014 a beverage she has never tasted and does not enjoy. A fourteen-year-old boy receives his dead brother's implant and begins flinching at the sound of slamming doors. His brother was beaten by a landlord who slammed the door before every blow. The boy has no memory of this. His body remembers anyway.\n\nThird-generation devices \u2014 grandmother to mother to granddaughter \u2014 show the most disturbing manifestations. The ghost weight layers. A granddaughter running her grandmother's BCI, passed through her mother, exhibits knowledge of locations that were demolished before she was born. She navigates to addresses that no longer exist. She recognizes faces in crowds that belong to people decades dead. She possesses fragments of skills the original owner practiced \u2014 soldering techniques, haggling strategies, the muscle-memory of lullabies sung to children who are now grandmothers themselves.\n\nThis investigation interviewed forty-seven families across six Shelf blocks who confirmed multi-generational BCI inheritance. Every single one reported ghost weight phenomena. Every single one considered it normal. When asked if the practice frightened them, a common response emerged: \"She's still in there. Why would that scare me?\"",
    related_entities: [
      "Lazarus Pharmaceuticals",
      "the Shelf",
      "CorpSec"
    ],
    credibility: "verified",
    story_hooks: [
      "A Shelf mother is dying and her daughter is too young for BCI implantation. The family must find someone to hold the device for three years until the girl is old enough \u2014 but every month in another skull adds someone else's ghost weight to the chain.",
      "CorpSec begins quietly cataloging inherited BCIs in the Shelf. Not to enforce the law. To study the ghost weight phenomenon. Someone in Lazarus wants to know what the devices are learning on their own."
    ],
    tags: [
      "ghost_weight",
      "bci",
      "inheritance",
      "maternal",
      "generational",
      "memory",
      "shelf",
      "lazarus",
      "corpsec",
      "illegal",
      "family"
    ]
  },
  {
    id: generateId(),
    name: "Ghost Weight \u2014 A Neuroscientist's Field Notes",
    type: "document",
    document_type: "academic_paper",
    author: "Dr. Amara Osei-Lindqvist, independent neuroscience researcher",
    date: "2225-11-02",
    classification: "suppressed",
    description: "This paper represents four years of field research into residual neural patterning in inherited brain-computer interfaces, conducted in the Shelf district of GLMZ between 2221 and 2225. Three peer-reviewed journals declined to publish these findings. The Journal of Neural Interface Studies, the Meridian Neuroscience Quarterly, and Cognitive Architecture Review all rejected the manuscript within days of submission. All three journals receive primary funding from Lazarus Pharmaceuticals. Dr. Osei-Lindqvist elected to release the paper through independent channels.\n\nThe core finding: BCIs develop physical channel formations in their neural substrate that are specific to each user. These formations are created by sustained electrochemical interaction between the device and the host brain. They are not data. They are topology \u2014 physical grooves, ridges, and crystalline microstructures in the substrate material. A factory reset, which operates at the software level, cannot alter physical structure any more than reformatting a hard drive can change the shape of the disk. The formations persist indefinitely. They are the ghost weight.\n\nThe study's most significant and most controversial finding concerns the maternal line. In cases where BCIs pass from mother to daughter, the residual patterning exhibits a selectivity that current neuroscience cannot explain. Maternal memories \u2014 experiences belonging to the mother \u2014 persist at measurably higher fidelity than paternal memories in cases where both parents used the device. Emotional memories persist more strongly than factual ones. And within emotional memory, three categories dominate with overwhelming statistical significance: memories related to childbirth, memories related to child-rearing, and memories related to survival threat. The BCI preferentially retains the experiences of being a mother and staying alive.\n\nThe substrate material is silicon-carbide lattice. It is not DNA. It has no biological mechanism for sex-linked information retention. It should not \u2014 by any known principle of materials science \u2014 preferentially encode maternal experience. And yet across 200 inherited devices studied, the pattern held with 94% consistency. Dr. Osei-Lindqvist's hypothesis: the BCI interface is shaped by the host's hormonal environment. Estrogen-dominant neurochemistry creates deeper, more durable substrate impressions. The device is not selecting for maternal memory. It is simply recording women's experiences more deeply because of the biochemical context in which those experiences occur.\n\nThe paper's final line has been widely quoted in the Shelf since its independent release: \"The BCI is teaching her to be someone who died before she was born. We built a machine to augment cognition. Instead, we built a vessel for the dead.\"",
    related_entities: [
      "Lazarus Pharmaceuticals",
      "the Shelf",
      "Journal of Neural Interface Studies",
      "Meridian Neuroscience Quarterly",
      "Cognitive Architecture Review"
    ],
    credibility: "verified",
    story_hooks: [
      "Lazarus sends a legal team to compel Dr. Osei-Lindqvist to retract the paper. She refuses. Her research data is stored on a BCI she inherited from her own mother. If they confiscate the device, they prove her point.",
      "A Lazarus engineer reads the paper and realizes the company has known about substrate patterning for decades. The factory reset was never designed to clear it. It was designed to leave it intact. The question is why."
    ],
    tags: [
      "ghost_weight",
      "bci",
      "inheritance",
      "maternal",
      "generational",
      "memory",
      "academic",
      "suppressed",
      "lazarus",
      "neuroscience",
      "substrate",
      "estrogen"
    ]
  },
  {
    id: generateId(),
    name: "My Mother's Hands Know Things Mine Don't",
    type: "document",
    document_type: "personal_account",
    author: "Yuki Okafor-Petersen, age 19, Shelf resident",
    date: "2226-01-08",
    classification: "public",
    description: "My grandmother died six years before I was born. My mother died when I was five. The BCI in my head was my grandmother's first, then my mother's, then mine. I got it when I was seven. The clinic guy said the factory reset went clean. He said it was like new. He was wrong, or he was lying, or he didn't know. I think he didn't know. Most people don't know what ghost weight feels like from the inside.\n\nI don't have memories. I want to be clear about that. I don't see my mother's face or hear my grandmother's voice. It's not like that. What I have are reflexes. My hands do things before I decide to do them. I flinch at the sound of pressure seals releasing \u2014 that hiss-click that airlocks make. My mother was in a decompression accident when she was sixteen. Three people died. She survived. I didn't learn this until I was fourteen and an aunt told me. By then I'd been flinching at that sound for seven years. My body knew something my mind didn't. My body has always known things my mind doesn't.\n\nI know my way around the Gulch. I have never been to the Gulch. I could draw you a map of the market stalls on level three, tell you which vendor sells the best synth-protein wraps, tell you where the CorpSec cameras have blind spots. My grandmother lived in the Gulch for thirty years. I carry her navigation like it's my own. Last year I walked the route without thinking, from the B-line station to an apartment that was demolished in 2208. I stood in front of the empty lot and cried and I didn't know why.\n\nAt night, when the feed goes quiet and the Shelf settles into its version of silence, I feel something in the device. Not my mother. Something older. It doesn't move. It doesn't speak. It doesn't push thoughts into my head or images into my eyes. It holds still. Perfectly, absolutely still. Like something curled up in a small space, breathing slowly, waiting. I've felt it since I was seven. I used to think it was a glitch. Now I think it's my grandmother. Not her memory. Not her data. Something that was pressed into the machine by the weight of her living in it for forty years. It doesn't want anything from me. It just listens. It has been listening for a very long time.\n\nPeople ask me if it scares me. It doesn't. What scares me is the idea of a BCI with no ghost weight. A clean device. Empty. That sounds like loneliness in a way I can't describe. At least I'm not alone in here.",
    related_entities: [
      "the Shelf",
      "the Gulch"
    ],
    credibility: "firsthand_account",
    story_hooks: [
      "Yuki discovers her grandmother was involved in something dangerous in the Gulch \u2014 and the ghost weight is leading her back to finish it. The reflexes aren't random. They're a map to something her grandmother hid.",
      "A researcher contacts Yuki wanting to study her third-generation BCI. The researcher works for Lazarus. Yuki's aunt warns her that Lazarus doesn't study things \u2014 it collects them."
    ],
    tags: [
      "ghost_weight",
      "bci",
      "inheritance",
      "maternal",
      "generational",
      "memory",
      "personal",
      "shelf",
      "gulch",
      "third_generation",
      "identity"
    ]
  },
  {
    id: generateId(),
    name: "The Maternal Line and the Memory Substrate",
    type: "document",
    document_type: "technical_paper",
    author: "Dr. Amara Osei-Lindqvist and Renzo Vizcarra-Nakamura, independent researchers",
    date: "2226-02-19",
    classification: "public",
    description: "This paper addresses the central unanswered question from our previous publication on ghost weight: why does the BCI substrate preferentially retain maternal experience? The neural substrate is silicon-carbide lattice manufactured by Lazarus Pharmaceuticals. It is an inorganic crystalline material. It has no DNA, no epigenetic markers, no biological mechanism for sex-linked information encoding. By every principle of materials science, the substrate should record all neural impressions with equal fidelity regardless of the host's sex or hormonal profile. It does not.\n\nOur working theory centers on the electrochemical environment of the host brain. The BCI substrate forms its physical channel formations through sustained exposure to the host's neural electromagnetic field. This field is not uniform \u2014 it is shaped by neurotransmitter concentrations, hormone levels, and the specific patterns of neural firing that accompany different cognitive and emotional states. Estrogen and progesterone \u2014 present at significantly higher concentrations in female neurochemistry \u2014 alter the electromagnetic profile of neural signals in measurable ways. Specifically, estrogen-modulated signals produce a lower-frequency, higher-amplitude electromagnetic envelope that appears to create deeper physical impressions in the silicon-carbide lattice.\n\nThe implication is straightforward and profound: the BCI literally records women's experiences more deeply than men's. Not because of any design choice. Because the physics of the substrate material responds more strongly to the electromagnetic signature of estrogen-dominant neurochemistry. When a BCI passes from mother to daughter \u2014 remaining within an estrogen-dominant hormonal environment across generations \u2014 the substrate impressions compound. Each generation's experiences are recorded on top of the last, in the same deep-impression modality. The device becomes a repository of women's accumulated survival knowledge, encoded not as retrievable data but as physical tendency. The granddaughter does not remember her grandmother's skills. She exhibits them. Her hands move in ways her grandmother's hands moved. Her threat responses mirror her grandmother's threat responses. The knowledge is stored in the shape of the machine.\n\nWe propose the term \"epigenetic hardware\" for this phenomenon. In biology, epigenetics describes information encoded not in DNA sequence but in the physical and chemical modifications of the DNA molecule. Epigenetic hardware describes information encoded not in a device's software or data storage but in the physical structure of a machine that was shaped by the bodies it inhabited. The BCI carries the cumulative physical impression of every woman who wore it, each layer laid down by the particular electromagnetic signature of a female brain processing the experiences of survival, motherhood, grief, love, and endurance.\n\nThe factory reset cannot touch this. You cannot reset topology. You cannot erase the shape of a thing by telling it to forget. The ghost weight is the device's body remembering what its mind was told to forget.",
    related_entities: [
      "Lazarus Pharmaceuticals",
      "the Shelf"
    ],
    credibility: "verified",
    story_hooks: [
      "A Lazarus materials scientist independently confirms Osei-Lindqvist's findings and discovers something additional: the latest generation of BCI substrates uses a modified lattice that does not form persistent channel structures. Lazarus solved the ghost weight problem. And chose not to deploy the fix.",
      "Shelf mothers begin deliberately choosing not to factory-reset inherited BCIs, wanting their daughters to carry the full ghost weight. The practice spreads. A generation of girls grows up with their mothers' reflexes layered onto their own."
    ],
    tags: [
      "ghost_weight",
      "bci",
      "inheritance",
      "maternal",
      "generational",
      "memory",
      "technical",
      "substrate",
      "estrogen",
      "epigenetic",
      "lazarus",
      "neuroscience",
      "electromagnetic"
    ]
  },
  {
    id: generateId(),
    name: "The Grandmother Frequency",
    type: "document",
    document_type: "community_report",
    author: "Shelf Block 7 Residents' Association",
    date: "2226-04-01",
    classification: "public",
    description: "This report documents a phenomenon observed among six young women, ages 14 to 22, living in Block 7 of the Shelf district. All six use inherited brain-computer interfaces. All six BCIs are third-generation devices \u2014 grandmother to mother to granddaughter. The six women do not share a family. They did not know each other before this investigation. What they share is a sound.\n\nBeginning in late 2225, each of the six women independently reported hearing a humming during periods of low neural activity \u2014 typically at night, in the interval between waking and sleep. The humming is not transmitted through the BCI's audio feed. It manifests as a neural pattern that the brain interprets as sound. Each woman described the same melody: a simple lullaby, four phrases, rising on the second phrase and falling on the fourth. None of them knew the name of the song. None of them had been taught it. When asked to hum it, all six produced the same melody within a quarter-tone tolerance.\n\nCross-referencing the serial numbers of the six BCIs revealed that all six original devices \u2014 the grandmother-generation units \u2014 were purchased between 2187 and 2191 from the same Lazarus distribution point in the Gulch. Further investigation through Block 7 elder residents identified the original owners: six women who lived in the same Gulch neighborhood, Tenement Row, before the district was restructured in 2203. Tenement Row was a close community. The women knew each other. According to surviving neighbors, they all sang the same lullaby to their children \u2014 a song called \"Little Fish\" that originated in the coastal communities of the old Pacific Northwest. No recording of the song exists in any accessible archive. It was transmitted only by voice, mother to child.\n\nThe song died when Tenement Row was demolished and its residents scattered across the Shelf. The women aged. Their children grew up. The BCIs passed from mother to daughter. The lullaby persisted in the substrate of six devices, carried as a neural firing pattern pressed into silicon-carbide by women who sang it so many times that the repetition carved physical channels in the machine. The granddaughters hum it without knowing what it is. They hum it when they're tired. They hum it when they're afraid. One of them hums it to a neighbor's baby when the child won't sleep. She doesn't know why it works. It always works.\n\nWhen the investigation team informed one of the six women \u2014 Priya Achebe-Svensson, age 17 \u2014 of the lullaby's origin, she was silent for a long time. Then she cried. When she could speak, she said: \"I knew it was for me. I always knew it was for me. I just didn't know who was singing it.\" The other five, when told separately, each reported the same certainty: that the song had always felt directed at them, personally, as though someone specific was singing it to someone specific. As though the device remembered not just the melody but the intention behind it. As though the ghost weight of a grandmother's love had a frequency, and it had been humming in the dark for thirty years, waiting for the right ears to hear it.\n\nThe Shelf Block 7 Residents' Association does not have the scientific resources to explain this phenomenon. We submit this report for the record. The song is real. The granddaughters sing it. The grandmothers are dead. Draw your own conclusions.",
    related_entities: [
      "the Shelf",
      "the Gulch",
      "Lazarus Pharmaceuticals",
      "Tenement Row"
    ],
    credibility: "community_sourced",
    story_hooks: [
      "A music historian attempts to reconstruct the full version of \"Little Fish\" from the fragments six granddaughters carry in their BCIs. Each has a slightly different variation. Together they assemble something close to the original \u2014 but there's a fifth phrase none of the grandmothers' neighbors remember. The devices added something.",
      "Lazarus sends a team to Block 7 to study the synchronized phenomenon. They are not interested in the lullaby. They are interested in the fact that six unconnected devices developed the same substrate pattern independently. That is not ghost weight. That is convergence. And convergence in neural substrates has implications that terrify the people who build them."
    ],
    tags: [
      "ghost_weight",
      "bci",
      "inheritance",
      "maternal",
      "generational",
      "memory",
      "community",
      "lullaby",
      "shelf",
      "gulch",
      "third_generation",
      "convergence",
      "tenement_row"
    ]
  }
];

let written = 0;
for (const doc of documents) {
  if (writeDoc(doc)) written++;
}

console.log(`\nDone. ${written} document(s) written, ${documents.length - written} skipped.`);
