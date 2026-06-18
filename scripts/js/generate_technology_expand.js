const fs = require('fs');
const path = require('path');
const crypto = require('crypto');

const OUTPUT_DIR = path.join(__dirname, '..', 'engine', 'data', 'technology');
fs.mkdirSync(OUTPUT_DIR, { recursive: true });

const existing = new Set(fs.readdirSync(OUTPUT_DIR));

function genId() {
  return crypto.randomBytes(16).toString('hex');
}

function slugify(name) {
  const trimmed = name.slice(0, 60);
  return trimmed
    .toLowerCase()
    .replace(/[^a-z0-9]+/g, '_')
    .replace(/^_|_$/g, '')
    .slice(0, 80);
}

let written = 0;
let skipped = 0;

function writeTech(t) {
  const filename = slugify(t.name) + '.json';
  if (existing.has(filename)) {
    console.log(`SKIP (exists): ${filename}`);
    skipped++;
    return;
  }
  const data = {
    id: genId(),
    name: t.name,
    brand_name: t.brand_name || '',
    product_name: t.product_name || '',
    type: 'technology',
    aliases: t.aliases || [],
    subcategory: t.subcategory || '',
    description: t.description || '',
    tier_availability: t.tier_availability || '',
    developers: t.developers || [],
    base_technologies: t.base_technologies || [],
    enables: t.enables || [],
    social_impact: t.social_impact || '',
    story_hooks: t.story_hooks || [],
    tags: t.tags || []
  };
  fs.writeFileSync(path.join(OUTPUT_DIR, filename), JSON.stringify(data, null, 2));
  console.log(`WROTE: ${filename}`);
  written++;
}

const techs = [
  // ========================================================================
  // CRYOGENIC TECHNOLOGY
  // ========================================================================
  {
    name: "Lazarus Pharmaceuticals CryoVault Long-Term Preservation System",
    brand_name: "Lazarus",
    product_name: "CryoVault LTP-9",
    aliases: ["CryoVault", "LTP-9", "The Freezer", "Cold Sleep Pod"],
    subcategory: "cryogenics",
    description: "The CryoVault LTP-9 is the industry standard for long-term human cryopreservation, capable of maintaining a human body in reversible metabolic suspension for periods exceeding fifty years with a revival success rate of 94.7% -- a number Lazarus advertises prominently and which independent researchers have been unable to verify because Lazarus controls the only facilities certified to perform revival procedures. The system uses a proprietary vitrification process that replaces cellular water with a cryoprotectant solution, then cools the body to -196 degrees Celsius at a precisely controlled rate that prevents ice crystal formation -- the process that destroyed earlier generations of cryopreservation subjects by shredding their cellular structures from the inside.\n\nThe CryoVault's primary market is not medical -- it is financial. GLMZ's wealthiest residents use cryo-sleep to skip periods of economic instability, wait out unfavorable political conditions, or simply pause their aging while their investments compound. A Tier 5 executive who enters the CryoVault at age 60 can emerge at biological age 60 into a future where their investment portfolio has grown for decades without their consuming any of it. Lazarus markets this as 'temporal asset optimization' and charges 850,000 per year of preservation plus a 2.5 million revival fee. The waiting list for CryoVault installation is eighteen months.\n\nThe technology's darker applications are less advertised. Corporate contracts increasingly include cryo-suspension clauses that allow employers to freeze whistleblowers, inconvenient witnesses, or executives who know too much rather than terminating them -- a practice that is technically legal because the subject is not dead, merely suspended. Lazarus's legal team has successfully argued in the Meridian Quorum that cryopreserved individuals retain their legal rights but cannot exercise them, creating a category of person who exists but cannot act. Critics call it 'ice imprisonment.' Lazarus calls it 'voluntary preservation pending resolution of contractual disputes.'",
    tier_availability: "Tier 4+",
    developers: ["LAZARUS PHARMACEUTICALS"],
    base_technologies: ["Controlled-rate vitrification", "Cryoprotectant cellular replacement", "Long-term cryogenic maintenance systems"],
    enables: ["Long-term human preservation exceeding 50 years", "Temporal asset optimization for wealthy individuals", "Medical suspension during incurable conditions", "Corporate cryo-suspension clauses in employment contracts"],
    social_impact: "The CryoVault has created a class of temporal elites -- individuals who can skip unfavorable periods of history and emerge into better ones, effectively choosing which future they inhabit. This has profound implications for wealth distribution, as compound interest working for decades on a frozen executive's portfolio creates dynastic wealth that active participants in the economy cannot match. The lower tiers call cryopreserved elites 'sleepers' and resent them with an intensity that borders on religious -- people who can opt out of suffering while everyone else endures it are not seen as clever investors but as moral cowards who abandoned the present.",
    story_hooks: [
      "A CryoVault subject scheduled for revival after thirty years of preservation cannot be revived -- not because the process failed, but because the cryoprotectant in their cells has been subtly altered. Someone modified the preservation fluid after they were frozen. The subject is alive but permanently locked in suspension.",
      "A Tier 2 worker has discovered that their deceased parent was not cremated as reported but cryopreserved under a corporate contract clause they never knew existed. The parent is frozen in a Lazarus facility, legally alive but contractually suspended. Recovering them requires navigating a legal system designed to prevent exactly this."
    ],
    tags: ["technology", "cryogenics", "preservation", "lazarus", "medical", "corporate", "tier-4"]
  },
  {
    name: "Crucible Industries CryoForge Rapid Tissue Preservation Unit",
    brand_name: "Crucible",
    product_name: "CryoForge RPU-3",
    aliases: ["CryoForge", "RPU-3", "Flash Freeze", "Field Cryo"],
    subcategory: "cryogenics",
    description: "The CryoForge RPU-3 is a man-portable cryogenic unit designed for battlefield medicine -- capable of flash-freezing a wounded combatant's entire body to cryogenic temperatures in under ninety seconds, halting biological degradation and buying time for evacuation to a surgical facility. Unlike Lazarus's CryoVault, which is designed for long-term preservation with careful preparation, the CryoForge is designed for emergency use: minimal prep, maximum speed, acceptable collateral tissue damage. The unit weighs 34 kilograms, deploys from a backpack configuration, and uses expendable cryogenic cartridges that provide enough cooling agent for a single full-body freeze.\n\nCrucible developed the CryoForge for Arcturus combat medics operating in environments where evacuation times exceeded the golden hour for trauma survival. The system's approach is deliberately crude by medical standards -- it floods the subject with an aerosolized cryoprotectant through forced inhalation and dermal absorption while simultaneously cooling the body's surface through direct contact with cryogenic gel packs. The process is painful, traumatic, and saves lives that would otherwise be lost. Revival requires specialized equipment available only at military field hospitals, and the revival process itself carries a 12% complication rate including frostbite damage, cryoprotectant toxicity, and what medics call 'freeze shock' -- a neurological event caused by the brain's reaction to being flash-frozen and revived.\n\nThe CryoForge has found an unexpected secondary market in the criminal underworld, where its ability to freeze a person into temporary stasis has applications that Crucible's marketing department prefers not to acknowledge. Kidnapping operations use modified CryoForge units to immobilize targets for transport. Underground fighting rings freeze injured combatants between rounds to keep them fighting. The Shelf's medical cooperatives have adapted the technology for emergency preservation of patients who cannot reach surgical care in time -- a humanitarian application that Crucible has declined to either endorse or prosecute.",
    tier_availability: "Tier 3+ (military); black market availability in lower tiers",
    developers: ["CRUCIBLE INDUSTRIES", "ARCTURUS DEFENSE SOLUTIONS"],
    base_technologies: ["Rapid aerosolized cryoprotectant delivery", "Portable cryogenic cooling systems", "Emergency metabolic suspension protocols"],
    enables: ["Battlefield preservation of mortally wounded combatants", "Emergency medical stasis in field conditions", "Extended evacuation windows for trauma cases", "Criminal applications in kidnapping and containment"],
    social_impact: "The CryoForge has changed the calculus of violence in GLMZ. Injuries that were previously fatal are now survivable if a CryoForge is nearby, which has paradoxically increased the willingness of corporate security forces to use lethal force -- the reasoning being that if the target can be frozen and revived, killing them is temporary. This logic has been challenged in the Meridian Quorum without success, as Crucible's lobbyists argue that the CryoForge saves more lives than it endangers.",
    story_hooks: [
      "A series of kidnappings in the Circuit involve victims being flash-frozen with modified CryoForge units and stored in a rented cold storage facility. The victims are being held in stasis as leverage, but the storage facility's cooling system is failing and the kidnapper does not have the medical knowledge to revive them safely.",
      "An Arcturus combat medic has gone AWOL with a case of CryoForge units and is operating a black-market preservation service in the Shelf -- freezing people with terminal conditions until cures become available. The service is illegal, the success rate is uncertain, and the demand is overwhelming."
    ],
    tags: ["technology", "cryogenics", "military", "medical", "crucible", "arcturus", "portable", "battlefield"]
  },

  // ========================================================================
  // HOLOGRAPHIC PROJECTION SYSTEMS
  // ========================================================================
  {
    name: "Vantablack Media HoloPresence Volumetric Display Platform",
    brand_name: "Vantablack",
    product_name: "HoloPresence VP-12",
    aliases: ["HoloPresence", "VP-12", "Ghost Screen", "Holo Platform"],
    subcategory: "holographics",
    description: "The HoloPresence VP-12 is Vantablack Media's flagship volumetric display system, capable of projecting full-color, three-dimensional holographic images into open air without any screen, surface, or viewing apparatus required. The system uses intersecting arrays of focused ultraviolet lasers to ionize atmospheric nitrogen at precise coordinates, creating points of visible plasma that can be modulated in color, brightness, and position at refresh rates sufficient to produce fluid motion. A single VP-12 installation can fill a volume of up to 200 cubic meters with holographic content visible from any angle in ambient lighting conditions.\n\nVantablack has deployed HoloPresence installations throughout GLMZ's upper tiers, replacing physical signage, architectural facades, and even windows with holographic projections that can be updated in real time. The Spire district is particularly saturated -- walking through the Spire means moving through a landscape where roughly 40% of what the eye perceives is holographic. Buildings appear to change shape. Advertisements materialize in mid-air. Corporate logos float above intersections. The boundary between physical and projected reality has become genuinely difficult to distinguish, which is precisely Vantablack's intent -- a population that cannot reliably distinguish real from projected is a population that can be shown anything.\n\nThe VP-12's military and security applications are substantial. Arcturus has licensed the technology for battlefield deception -- projecting phantom vehicles, false troop positions, and decoy structures that are visually indistinguishable from real assets at range. Corporate security firms use HoloPresence to create false corridors, illusory walls, and projected hazards that redirect intruders away from sensitive areas. The technology has also spawned a counterculture of 'holo-taggers' -- artists and activists who hack HoloPresence installations to project unauthorized content into corporate spaces, turning Vantablack's own infrastructure into a medium for dissent.",
    tier_availability: "Tier 3+ (commercial); Tier 2+ (advertising exposure)",
    developers: ["VANTABLACK MEDIA"],
    base_technologies: ["Focused UV laser atmospheric ionization", "Volumetric plasma coordinate mapping", "Real-time holographic content rendering"],
    enables: ["Free-air holographic display without viewing apparatus", "Architectural holographic facades", "Military visual deception systems", "Immersive advertising environments", "Security systems using projected illusions"],
    social_impact: "HoloPresence has fundamentally altered the relationship between GLMZ's residents and visual reality. In the upper tiers, where the technology is ubiquitous, people have developed a habitual distrust of what they see -- tapping surfaces to confirm they are solid, touching walls before leaning against them, and wearing 'clarity filters' that highlight holographic projections with a visible shimmer. In the lower tiers, where HoloPresence is rare, the technology retains its power to astonish and deceive, creating an information asymmetry where Shelf residents are more vulnerable to holographic manipulation than their upper-tier counterparts.",
    story_hooks: [
      "A holo-tagger collective has discovered a vulnerability in Vantablack's HoloPresence network that allows them to project content across every installation in the city simultaneously. They plan to broadcast evidence of corporate malfeasance across every holographic surface in GLMZ for sixty seconds before the system can be shut down.",
      "The HoloPresence installations in a Tier 3 residential district have begun projecting images that no one programmed -- scenes from the district's pre-flooding history, showing buildings and people that existed before the area was rebuilt. The projections appear at night and Vantablack's engineers cannot determine their source."
    ],
    tags: ["technology", "holographics", "display", "vantablack", "visual", "media", "advertising", "military"]
  },
  {
    name: "TESSERA Industries Phantom Lace Personal Holographic Projector",
    brand_name: "TESSERA",
    product_name: "Phantom Lace PL-4",
    aliases: ["Phantom Lace", "PL-4", "Ghost Skin", "Holo Cloak"],
    subcategory: "holographics",
    description: "The Phantom Lace PL-4 is a wearable holographic projection system that generates a form-fitting volumetric display around the wearer's body, effectively allowing them to project any appearance over their actual physical form. The system consists of a mesh garment threaded with micro-emitters that project a holographic shell extending approximately two centimeters from the wearer's skin surface, capable of reproducing any human appearance -- different face, different body type, different clothing -- with sufficient fidelity to fool casual observation and most commercial facial recognition systems.\n\nTESSERA markets the Phantom Lace primarily to corporate executives seeking anonymity in public, celebrity clients requiring privacy, and security professionals who need to operate without identification. The official price point of 180,000 ensures that the technology remains exclusive to upper-tier users -- a deliberate choice, as TESSERA recognizes that widespread access to appearance-altering technology would destabilize the surveillance infrastructure that corporate clients depend on. The Phantom Lace is legal to own but illegal to use in certain contexts -- wearing one during a commercial transaction constitutes identity fraud, and wearing one in a restricted zone constitutes trespass by deception.\n\nThe black market for Phantom Lace units and knockoff versions is enormous. Criminal organizations prize the technology for obvious reasons -- the ability to appear as anyone makes surveillance-dependent law enforcement significantly less effective. Knockoff versions, collectively called 'ghost rags,' use cheaper emitters that produce lower-fidelity projections prone to flickering, color banding, and occasional complete failure at inopportune moments. The Shelf's ghost rag market produces units that cost as little as 2,000 and work well enough to defeat automated surveillance, if not close human inspection.",
    tier_availability: "Tier 4+ (legitimate); Tier 2+ (knockoffs)",
    developers: ["TESSERA INDUSTRIES"],
    base_technologies: ["Micro-emitter mesh fabrication", "Body-conforming volumetric projection", "Real-time appearance rendering and tracking"],
    enables: ["Complete visual identity alteration", "Defeat of facial recognition systems", "Anonymous movement in surveilled environments", "Undercover security operations", "Criminal identity fraud applications"],
    social_impact: "The Phantom Lace has introduced a fundamental uncertainty into personal interaction in GLMZ's upper tiers -- the person you are speaking to may not look anything like their actual appearance. This has accelerated the adoption of non-visual identification methods including voice pattern analysis, gait recognition, and biometric handshake protocols. In the lower tiers, ghost rag availability has created a subculture of 'faceless' individuals who move through surveilled spaces wearing projected identities, effectively becoming untraceable. Corporate security views this as a crisis; civil liberties advocates view it as the first real privacy technology available to ordinary people.",
    story_hooks: [
      "A series of crimes have been committed by someone wearing a Phantom Lace projecting the face of a specific public figure -- either to frame them or to make a statement. The real person is being detained while the crimes continue, and determining who is behind the projection requires technology that can see through it.",
      "A Phantom Lace user has discovered that their unit has been recording their actual appearance and transmitting it to TESSERA -- the privacy product is secretly a surveillance device. They want to go public, but removing the Lace while TESSERA knows their real face means becoming a target."
    ],
    tags: ["technology", "holographics", "wearable", "tessera", "identity", "surveillance", "personal"]
  },

  // ========================================================================
  // QUANTUM COMPUTING PLATFORMS
  // ========================================================================
  {
    name: "Ouroboros Systems Paradox Quantum Processing Array",
    brand_name: "Ouroboros",
    product_name: "Paradox QPA-7",
    aliases: ["Paradox", "QPA-7", "Quantum Core", "The Paradox Engine"],
    subcategory: "computing",
    description: "The Paradox QPA-7 is the most powerful commercially available quantum computing platform in existence, operating at 4,096 logical qubits with an error correction rate that Ouroboros claims approaches theoretical perfection -- a claim that independent verification has been unable to confirm because Ouroboros has classified the error correction methodology as a trade secret. The QPA-7 occupies a climate-controlled facility the size of a residential apartment, requires continuous cryogenic cooling to near absolute zero, consumes enough power to supply a small residential block, and costs approximately 47 million. Ouroboros operates eleven Paradox installations worldwide, seven of which are in GLMZ, and leases processing time at rates that make it accessible only to other CorpoNations and the wealthiest research institutions.\n\nThe Paradox's practical capabilities are both extraordinary and narrowly defined. It excels at optimization problems, cryptographic operations, molecular simulation, and pattern recognition across datasets too large for classical computing to process in useful timeframes. Ouroboros uses its own Paradox installations to maintain its dominance in financial modeling -- the QPA-7 can model market behavior across millions of variables simultaneously, giving Ouroboros trading operations a predictive advantage that classical computing cannot match. This advantage is widely suspected to be the primary source of Ouroboros's financial returns, though proving algorithmic market manipulation requires understanding the algorithm, and the algorithm runs on a quantum system that produces results without showing its work.\n\nThe QPA-7's most consequential capability is cryptographic. At 4,096 qubits, the Paradox can theoretically break any classical encryption scheme in existence -- including the encryption that protects corporate communications, financial transactions, and personal data across GLMZ. Ouroboros maintains that it does not use this capability offensively, a claim that is simultaneously impossible to verify and essential to the functioning of the city's digital infrastructure. The existence of the Paradox has forced every organization in GLMZ to transition to quantum-resistant encryption -- a transition that Ouroboros has profited from enormously, as it is also the leading provider of post-quantum cryptographic solutions.",
    tier_availability: "Tier 5 (exclusive corporate access)",
    developers: ["OUROBOROS SYSTEMS"],
    base_technologies: ["4,096 logical qubit architecture", "Proprietary quantum error correction", "Cryogenic quantum state maintenance"],
    enables: ["Breaking of classical encryption schemes", "Financial market predictive modeling", "Molecular simulation for pharmaceutical development", "Optimization across millions of simultaneous variables", "Post-quantum cryptographic development"],
    social_impact: "The Paradox has created a two-tier information security environment: those who can afford quantum-resistant encryption and those who cannot. Below Tier 3, most personal and business communications use classical encryption that the Paradox could theoretically break in seconds. This means that Ouroboros -- and anyone who leases Paradox processing time -- has potential access to virtually all lower-tier digital communications. Whether they exercise this capability is unknown. That they could is sufficient to make the Paradox the most powerful surveillance tool in existence, disguised as a computing platform.",
    story_hooks: [
      "A Paradox installation has produced a result that its operators cannot interpret -- a solution to a problem that was not asked. The quantum system appears to have spontaneously solved an optimization problem that corresponds to no known input, and the result, when decoded, appears to be a message.",
      "An Ouroboros competitor has developed a quantum computing approach that could match the Paradox at a fraction of the cost. Ouroboros is willing to do anything to suppress the technology -- including using the Paradox's cryptographic capabilities to destroy the competitor's digital infrastructure."
    ],
    tags: ["technology", "computing", "quantum", "ouroboros", "cryptography", "finance", "tier-5"]
  },

  // ========================================================================
  // BIOPRINTING TECHNOLOGY
  // ========================================================================
  {
    name: "Lazarus Pharmaceuticals BioLoom Tissue Fabrication Engine",
    brand_name: "Lazarus",
    product_name: "BioLoom TFE-6",
    aliases: ["BioLoom", "TFE-6", "Tissue Printer", "The Loom"],
    subcategory: "biotechnology",
    description: "The BioLoom TFE-6 represents the current state of the art in bioprinting technology -- a fabrication system capable of printing living tissue structures with cellular-level precision, layer by layer, using the patient's own stem cells as raw material. Where earlier bioprinters could produce simple tissue sheets and basic organ structures, the BioLoom prints complex multicellular architectures including vascularized tissue, innervated skin grafts, and functional muscle assemblies complete with integrated nerve pathways. A single BioLoom installation can produce a square meter of fully functional skin tissue in four hours, a section of muscle tissue with embedded motor neurons in twelve hours, or a complete small organ in approximately forty-eight hours.\n\nLazarus markets the BioLoom primarily for reconstructive medicine -- rebuilding bodies damaged by industrial accidents, combat injuries, or the progressive tissue degradation that affects long-term cyberware users. The technology has become essential for cyberware maintenance, as the interface points where mechanical augmentation meets biological tissue are subject to chronic inflammation, rejection, and necrotic breakdown that requires regular tissue replacement. Lazarus has positioned itself as the indispensable maintenance provider for every cyberware installation in GLMZ -- you can buy your chrome from any manufacturer, but when your body starts rejecting it, you need Lazarus tissue.\n\nThe BioLoom's most controversial application is cosmetic -- the ability to print custom tissue has created a market for radical body modification that goes far beyond traditional cosmetic surgery. Clients can have their facial structures rebuilt to any specification, their skin replaced with tissue engineered for different pigmentation or texture, or their body proportions altered through tissue addition and sculpting. The wealthy change their appearance the way lower-tier residents change their clothes. Identity in the upper tiers has become fluid in a literal, biological sense, raising questions about personhood, legal identification, and whether a society where anyone can become anyone is a society at all.",
    tier_availability: "Tier 3+ (medical); Tier 4+ (cosmetic)",
    developers: ["LAZARUS PHARMACEUTICALS"],
    base_technologies: ["Stem cell directed differentiation", "Cellular-precision bioprinting", "Integrated vascularization and innervation protocols"],
    enables: ["Complex tissue fabrication with nerve and vascular integration", "Cyberware interface tissue maintenance", "Radical cosmetic body modification", "Reconstructive surgery for severe trauma", "Custom biological tissue engineering"],
    social_impact: "The BioLoom has made biological identity as mutable as digital identity for those who can afford it. In the upper tiers, the concept of a fixed physical appearance is becoming obsolete -- faces, bodies, and even skin are treated as temporary configurations that can be altered at will. This has created a crisis for identification systems that rely on physical appearance and has deepened the divide between tiers, as lower-tier residents remain locked in their biological bodies while the wealthy can become anything they want.",
    story_hooks: [
      "A series of unsolved crimes share a single perpetrator whose appearance changes completely between each incident -- BioLoom tissue printing used to create an entirely new face after every job. Catching them requires predicting who they will become next.",
      "A BioLoom installation at a Tier 3 medical facility has begun printing tissue that does not match any patient's stem cell profile on file. The tissue is being fabricated from an unknown source, and the fabrication orders are coming from within Lazarus's own systems -- but no one authorized them."
    ],
    tags: ["technology", "bioprinting", "medical", "lazarus", "tissue", "cosmetic", "cyberware"]
  },

  // ========================================================================
  // NEURAL RECORDING / MEMORY TECHNOLOGY
  // ========================================================================
  {
    name: "TESSERA Industries Mnemon Total Recall Memory System",
    brand_name: "TESSERA",
    product_name: "Mnemon TRM-5",
    aliases: ["Mnemon", "TRM-5", "Total Recall", "Memory Box"],
    subcategory: "neural_interface",
    description: "The Mnemon TRM-5 is a neural recording system that captures, stores, and enables playback of complete human experiential memory -- not just visual and auditory data, but the full sensory spectrum including proprioception, emotional state, and cognitive context. A memory recorded by the Mnemon is not a video; it is a complete experiential snapshot that, when played back through a compatible BCI, allows the viewer to experience the original event exactly as the recorder experienced it -- seeing through their eyes, feeling their emotions, thinking their thoughts. The system records continuously, storing approximately 90 days of experiential data on an implanted solid-state archive before older memories are overwritten unless manually preserved.\n\nTESSERA markets the Mnemon as a professional tool -- investigators use it to create legally admissible experiential evidence, medical professionals record procedures for training, and corporate executives record meetings to ensure perfect recall. The official use cases are mundane. The actual use cases have created an entirely new economy. Memory trading -- the sale and distribution of recorded experiences -- has become one of GLMZ's fastest-growing gray markets. A recorded experience of a Tier 5 luxury vacation sells for thousands. A combat memory from an Arcturus operator provides an adrenaline experience no simulation can match. More intimately, recorded memories of deceased loved ones have become the most emotionally valuable commodity in the city -- the ability to experience a dead person's last birthday, their last conversation, their last moment of joy, exactly as they experienced it.\n\nThe Mnemon's darker applications involve memory manipulation. While the official system records without alteration, modified firmware can edit recorded memories before playback -- removing details, adding false elements, or blending multiple memories into fabricated experiences that feel completely authentic. This capability has made memory-based evidence legally controversial and personally devastating, as individuals can no longer trust their own recorded experiences. TESSERA's official position is that memory tampering is a criminal misuse of their technology. TESSERA's classified position, according to leaked internal documents, is that memory editing capability is a feature, not a bug.",
    tier_availability: "Tier 3+ (recording); Tier 2+ (playback-only consumer devices)",
    developers: ["TESSERA INDUSTRIES"],
    base_technologies: ["Full-spectrum neural state recording", "Experiential data compression and storage", "BCI-mediated memory playback protocols"],
    enables: ["Complete experiential memory capture and playback", "Legally admissible experiential evidence", "Memory trading economy", "Preservation of deceased persons' experiences", "Memory editing and fabrication"],
    social_impact: "The Mnemon has destabilized the concept of personal memory as private and reliable. Recorded memories can be shared, sold, stolen, edited, and fabricated with equal ease. The memory trading economy has created new forms of intimacy (sharing experiences directly) and new forms of violation (memory theft, involuntary recording, experience trafficking). The philosophical implications are staggering -- if your memories can be perfectly recorded and played back by someone else, what makes your experience uniquely yours?",
    story_hooks: [
      "A murder victim's Mnemon implant contains a recording of their death -- but when the recording is played back, the victim's emotional state is calm and accepting, inconsistent with violent death. Either the memory has been edited or the victim knew they were going to die and chose not to resist.",
      "A black-market memory dealer has obtained a Mnemon recording from inside a CorpoNation board meeting where a decision was made that would cause public outrage. The recording is experiential -- anyone who plays it back will know not just what was said but what the participants felt while saying it. The emotional context is more damning than the words."
    ],
    tags: ["technology", "neural", "memory", "recording", "tessera", "bci", "experience", "surveillance"]
  },
  {
    name: "Ouroboros Systems Engram Cognitive Backup Protocol",
    brand_name: "Ouroboros",
    product_name: "Engram CBP-2",
    aliases: ["Engram", "CBP-2", "Brain Backup", "Cognitive Snapshot"],
    subcategory: "neural_interface",
    description: "The Engram CBP-2 is Ouroboros's entry into the cognitive preservation market -- a system that creates periodic comprehensive snapshots of a user's entire cognitive state, including personality patterns, skill matrices, knowledge bases, and emotional response profiles. Unlike TESSERA's Mnemon, which records experiential memories, the Engram captures who you are rather than what you experienced -- your decision-making patterns, your expertise, your personality traits, your cognitive architecture. A complete Engram snapshot, Ouroboros claims, contains enough information to reconstruct a functional model of the subject's mind.\n\nThe Engram's primary commercial application is cognitive insurance for corporate executives. A snapshot taken before a dangerous operation, a risky negotiation, or a medical procedure serves as a baseline -- if the subject suffers cognitive damage, the Engram provides a reference point for neural reconstruction. Ouroboros also markets the technology to organizations seeking to preserve institutional knowledge: a retiring executive's decades of experience and intuition captured in a format that can theoretically be used to train their successor or even to create an AI advisory system that thinks like the original.\n\nThe technology's existential implications are the subject of intense philosophical and legal debate. An Engram snapshot is not a person -- it cannot think, feel, or act on its own. But it contains enough information to create something that could, if loaded into an appropriate substrate. Ouroboros has publicly stated that it does not offer cognitive reconstruction from Engram snapshots, but the technology to do so exists, and the temptation to resurrect a deceased genius, a murdered executive, or a lost loved one from their last Engram snapshot has driven a black-market cognitive reconstruction industry that operates at the bleeding edge of identity law and metaphysics.",
    tier_availability: "Tier 4+",
    developers: ["OUROBOROS SYSTEMS"],
    base_technologies: ["Comprehensive cognitive state mapping", "Personality pattern extraction", "Skill matrix serialization"],
    enables: ["Complete cognitive state backup", "Post-trauma cognitive reconstruction reference", "Institutional knowledge preservation", "AI advisory system training from human cognitive patterns", "Black-market cognitive reconstruction"],
    social_impact: "The Engram has forced GLMZ to confront questions that philosophy has debated for centuries: what is identity, is it transferable, and does a copy of a mind have the same moral weight as the original? The legal system has no framework for these questions, creating a gray zone where cognitive data has the legal status of property but the emotional significance of a person. Engram snapshots of deceased loved ones are traded, hoarded, and fought over in estate disputes with an intensity that suggests the line between data and soul is thinner than the law acknowledges.",
    story_hooks: [
      "A black-market cognitive reconstruction has been performed using the Engram snapshot of a murdered corporate executive. The reconstructed entity claims to know who killed the original and demands justice -- but has no legal standing as a person and may be a manipulated copy designed to point blame at a specific target.",
      "An Engram snapshot taken from a living subject has been loaded into an AI system without their knowledge, creating a digital entity that thinks, responds, and behaves exactly like the subject. The original discovers their cognitive double and must determine whether to destroy it -- an act that feels uncomfortably like murder."
    ],
    tags: ["technology", "neural", "cognitive", "backup", "ouroboros", "identity", "bci", "preservation"]
  },

  // ========================================================================
  // ACOUSTIC MANIPULATION SYSTEMS
  // ========================================================================
  {
    name: "Arcturus Defense Solutions SonicBarrier Acoustic Denial System",
    brand_name: "Arcturus",
    product_name: "SonicBarrier ADS-4",
    aliases: ["SonicBarrier", "ADS-4", "The Screamer", "Sound Wall"],
    subcategory: "acoustics",
    description: "The SonicBarrier ADS-4 is a directed acoustic weapon system that projects precisely shaped sound fields capable of incapacitating, disorienting, or physically injuring targets at ranges up to 300 meters. The system uses a phased array of ultrasonic emitters to create focused beams of sound energy that can be tuned across a wide frequency range -- from subsonic infrasound that causes nausea, disorientation, and involuntary bowel evacuation, through the audible spectrum where focused high-decibel output causes pain and temporary deafness, to ultrasonic frequencies that heat tissue and cause internal burns without any audible warning.\n\nArcturus deploys the SonicBarrier as a crowd control and area denial system, marketing it as a 'non-lethal' alternative to kinetic weapons. This classification is technically accurate at standard operational parameters and technically fraudulent at the elevated settings that field operators routinely use. At manufacturer specifications, the ADS-4 causes reversible discomfort. At field settings -- which Arcturus's own training materials acknowledge as 'operationally necessary' -- the system causes permanent hearing loss, vestibular damage, and in documented cases, fatal internal hemorrhaging from sustained ultrasonic exposure. The distinction between 'non-lethal' and 'lethal at discretion' is one that Arcturus's legal team has successfully maintained in the Meridian Quorum for over a decade.\n\nThe SonicBarrier's most sophisticated application is its 'whisper mode' -- a low-power directed audio capability that can project intelligible speech into a single individual's hearing from a distance, creating the experience of hearing a voice that no one else can hear. Arcturus markets this for covert communication, but its use in psychological operations is well documented. Targets subjected to whisper mode without their knowledge frequently develop symptoms of psychosis, believing they are hearing voices. The technology has been used in corporate espionage operations to drive targets to mental breakdown without any physical contact.",
    tier_availability: "Tier 3+ (security); Tier 5 (military grade)",
    developers: ["ARCTURUS DEFENSE SOLUTIONS"],
    base_technologies: ["Phased ultrasonic emitter arrays", "Directed acoustic beam forming", "Variable frequency sound field shaping"],
    enables: ["Non-lethal crowd control through acoustic incapacitation", "Area denial without physical barriers", "Covert directed audio communication", "Psychological operations through whisper mode", "Tissue heating via focused ultrasound"],
    social_impact: "The SonicBarrier has made sound itself a weapon, transforming public spaces in GLMZ into potential acoustic kill zones. The technology's deployment at corporate checkpoints and tier boundaries has created a population conditioned to fear sound -- residents near deployment zones report chronic anxiety, hypervigilance to ambient noise, and a phenomenon psychologists call 'acoustic paranoia.' The whisper mode capability has seeded a genuine mental health crisis, as it becomes impossible to distinguish between auditory hallucinations and actual directed audio attacks.",
    story_hooks: [
      "A SonicBarrier installation at a tier boundary has been modified to continuously broadcast subsonic frequencies into a residential district, causing widespread nausea, insomnia, and psychological disturbance. The modification appears deliberate but no one has claimed responsibility, and the affected residents are being diagnosed with mass hysteria rather than acoustic assault.",
      "An Arcturus technician has defected with documentation proving that whisper mode has been used systematically against labor organizers in the Circuit -- driving them to apparent psychotic breaks that discredit their advocacy. The documentation names specific targets and specific operators."
    ],
    tags: ["technology", "acoustics", "weapon", "arcturus", "crowd-control", "psychological", "military"]
  },
  {
    name: "Ringo Applied Sciences HarmonicShield Environmental Sound System",
    brand_name: "Ringo",
    product_name: "HarmonicShield ESS-7",
    aliases: ["HarmonicShield", "ESS-7", "Sound Dome", "Quiet Zone"],
    subcategory: "acoustics",
    description: "The HarmonicShield ESS-7 is an environmental acoustic management system that creates zones of precisely controlled sound within urban spaces -- capable of generating areas of complete silence, masking ambient noise, projecting localized soundscapes, and shaping acoustic environments with a precision that makes traditional sound insulation obsolete. The system uses distributed arrays of micro-speakers and microphones to sample, process, and counter-project sound waves in real time, creating destructive interference patterns that cancel unwanted noise while allowing desired sounds to pass through unaffected.\n\nRingo markets the HarmonicShield as a quality-of-life technology for upper-tier residential and commercial spaces. A HarmonicShield installation can create a perfectly silent bedroom adjacent to an active nightclub, a private conversation zone in a crowded public space, or an entire office floor where each workstation exists in its own acoustic bubble. The technology has become standard in Tier 4 and Tier 5 construction, where the expectation of acoustic perfection is absolute -- the wealthy do not tolerate unwanted sound, and the HarmonicShield ensures they never have to.\n\nThe HarmonicShield's unintended consequence has been the acoustic segregation of GLMZ. Upper-tier districts wrapped in HarmonicShield installations exist in curated soundscapes where the noise of the city -- the industrial grinding, the crowd noise, the sirens, the construction -- is simply absent. Lower-tier districts, by contrast, absorb all the acoustic pollution that the upper tiers reject, plus the displaced noise from HarmonicShield installations that must push rejected sound somewhere. The Shelf is measurably louder than it was before HarmonicShield deployment, and the hearing damage rates among lower-tier residents have increased correspondingly. Sound, like everything else in GLMZ, flows downhill.",
    tier_availability: "Tier 4+ (residential); Tier 3+ (commercial)",
    developers: ["RINGO APPLIED SCIENCES"],
    base_technologies: ["Active noise cancellation at scale", "Distributed micro-speaker arrays", "Real-time acoustic environment modeling"],
    enables: ["Zone-based acoustic environment control", "Complete noise elimination in defined spaces", "Private conversation zones in public areas", "Acoustic segregation of urban districts", "Localized soundscape projection"],
    social_impact: "The HarmonicShield has made silence a luxury commodity in GLMZ. The upper tiers exist in acoustic perfection while the lower tiers drown in the noise the wealthy refuse to hear. This acoustic inequality tracks economic inequality precisely -- the poorest neighborhoods are the loudest, and the loudest neighborhoods produce the most hearing damage, and hearing damage reduces employability, creating a feedback loop where noise itself becomes a mechanism of economic oppression. Ringo's marketing materials do not mention this. Ringo's engineering documents acknowledge it as 'acoustic displacement' and classify it as an externality.",
    story_hooks: [
      "A HarmonicShield installation in a Tier 4 residential tower has been hacked to do the opposite of its intended function -- amplifying and focusing all ambient sound into the residents' apartments at dangerous volumes. The attack is attributed to a Shelf activist group protesting acoustic displacement, but the technical sophistication suggests corporate involvement.",
      "A Ringo engineer has discovered that the displaced noise from HarmonicShield installations, when analyzed as an aggregate signal, contains patterns that should not exist -- as if the combined acoustic output of the city is carrying an encoded message that no one intentionally placed there."
    ],
    tags: ["technology", "acoustics", "environmental", "ringo", "noise-control", "quality-of-life", "inequality"]
  },

  // ========================================================================
  // MAGNETIC LEVITATION TRANSPORT
  // ========================================================================
  {
    name: "Ringo Heavy Transit MagRail Levitation Transport Network",
    brand_name: "Ringo",
    product_name: "MagRail LTN-3",
    aliases: ["MagRail", "LTN-3", "The Rail", "Mag Line"],
    subcategory: "transportation",
    description: "The MagRail LTN-3 is the backbone of GLMZ's inter-tier mass transit system -- a magnetic levitation rail network that moves approximately 4 million passengers daily through a 740-kilometer web of elevated guideways connecting every tier of the city from the Spire to the upper Shelf. Trains levitate 12 centimeters above their guideways on superconducting magnetic fields, eliminating friction and enabling cruising speeds of 420 kilometers per hour on express routes. The system is silent, smooth, and fast enough that a commute from the Circuit to the Spire -- a distance that would take two hours by road -- is completed in eleven minutes.\n\nRingo designed the MagRail as both a transportation system and a social architecture. The network's routing, station placement, and fare structure are not neutral engineering decisions but deliberate mechanisms for controlling population movement between tiers. Express routes connect productive economic zones at high speed. Local routes through residential areas are slower and less frequent. The Shelf's MagRail stations are concentrated at the district's upper boundary, forcing residents to travel significant distances on foot or by informal transport to reach them. Fares are tiered -- literally -- with cross-tier travel costing exponentially more than within-tier movement. A Shelf resident commuting to a Circuit workplace pays roughly 15% of a typical daily wage in MagRail fares. A Spire resident commuting downward pays a rounding error.\n\nThe MagRail's physical infrastructure doubles as a tier boundary enforcement mechanism. The elevated guideways create corridors of restricted access at tier transitions, with station checkpoints serving as de facto border crossings where identification is verified, movement is logged, and undesirable passengers can be denied boarding. Ringo maintains that the checkpoints are security features. Critics observe that the checkpoints are positioned exactly where they would be if the MagRail were designed as a population control system, and that the security screening process takes significantly longer for passengers traveling upward than downward.",
    tier_availability: "All tiers (with tiered fare structure)",
    developers: ["RINGO HEAVY TRANSIT"],
    base_technologies: ["Superconducting magnetic levitation", "High-speed guideway engineering", "Automated traffic management systems"],
    enables: ["High-speed inter-tier mass transit", "Population movement control through fare structure and routing", "Tier boundary enforcement through station checkpoints", "Economic zone connectivity at 420 km/h", "Social architecture through transportation design"],
    social_impact: "The MagRail is simultaneously GLMZ's greatest public infrastructure achievement and its most elegant tool of social control. It connects the city while reinforcing its divisions, moves millions while tracking their movement, and provides access while pricing that access to maintain economic hierarchies. Residents depend on it completely and resent it proportionally -- the MagRail is the most used and most hated system in GLMZ, a daily reminder that even the act of traveling from home to work is a transaction managed for someone else's benefit.",
    story_hooks: [
      "The MagRail's automated traffic management system has begun rerouting trains away from certain stations without any authorized schedule change. The affected stations serve neighborhoods where labor organizing activity has been increasing. Someone is using the transit system to isolate and economically strangle specific communities.",
      "A catastrophic failure of the magnetic levitation system on an express route has sent a train carrying 800 passengers into an uncontrolled descent. The failure was not mechanical -- the superconducting magnets were remotely deactivated. Someone has demonstrated the ability to weaponize the transit system."
    ],
    tags: ["technology", "transportation", "maglev", "ringo", "transit", "social-control", "infrastructure"]
  },

  // ========================================================================
  // WASTE-TO-ENERGY CONVERSION
  // ========================================================================
  {
    name: "Crucible Industries PyroGenesis Plasma Waste Conversion Reactor",
    brand_name: "Crucible",
    product_name: "PyroGenesis PWC-5",
    aliases: ["PyroGenesis", "PWC-5", "Plasma Reactor", "Trash Furnace"],
    subcategory: "energy",
    description: "The PyroGenesis PWC-5 is a plasma gasification reactor that converts virtually any waste material -- organic, synthetic, toxic, or radioactive -- into synthesis gas, vitrified slag, and electrical power. The system generates an argon plasma arc at temperatures exceeding 7,000 degrees Celsius, hot enough to break down any molecular structure into its constituent elements. Waste material fed into the reactor is reduced to elemental components in seconds: carbon, hydrogen, and oxygen combine into synthesis gas that can be burned for power or refined into chemical feedstocks; everything else melts into an inert glass-like slag that can be used as construction aggregate. The process is thermodynamically self-sustaining above a minimum feed rate -- the energy released by molecular breakdown exceeds the energy required to maintain the plasma arc, generating a net power output of approximately 15 megawatts per installation.\n\nCrucible operates fourteen PyroGenesis installations in GLMZ, processing an estimated 60% of the city's total waste output and generating enough power to supply roughly 200,000 residences. The installations are positioned in the Shelf's industrial zones, where their considerable noise, heat, and atmospheric emissions are absorbed by communities that have no political leverage to object. Crucible pays licensing fees to the Meridian Quorum for waste processing contracts and sells the generated power back to the grid at rates that make the entire operation enormously profitable -- the company is paid to take garbage and paid again for the electricity it produces from that garbage.\n\nThe PyroGenesis's ability to process radioactive and toxic waste has made it the technology of last resort for materials too dangerous to store conventionally. Crucible accepts classified waste streams from multiple CorpoNations -- materials whose composition is not disclosed under hazardous material exemptions that Crucible's lobbyists wrote into the Quorum's environmental code. Critics have long argued that processing unknown materials at plasma temperatures risks generating equally unknown byproducts, but the argument has gained little traction because the alternative -- stockpiling toxic waste in a city at sea level -- is worse.",
    tier_availability: "Tier 5 (corporate industrial)",
    developers: ["CRUCIBLE INDUSTRIES"],
    base_technologies: ["Argon plasma arc generation", "Molecular dissociation gasification", "Synthesis gas recovery and power generation"],
    enables: ["Conversion of any waste material into energy and construction aggregate", "Processing of radioactive and toxic waste streams", "Net-positive energy generation from waste", "Elimination of conventional landfill requirements", "Chemical feedstock production from synthesis gas"],
    social_impact: "The PyroGenesis has made waste disposal profitable, which has the perverse effect of creating corporate incentive to generate waste. Crucible's waste processing contracts are volume-based -- the more waste they process, the more they earn. This economic structure discourages waste reduction at the source and encourages the throwaway consumption patterns that generate the Reclamation Authority's raw materials. The installations' placement in the Shelf has created zones of elevated atmospheric particulate, thermal pollution, and noise that contribute to the lower tiers' disproportionate health burden.",
    story_hooks: [
      "A PyroGenesis installation has processed a classified waste stream from Ouroboros that produced an anomalous synthesis gas -- a gas with molecular properties that should not be possible according to known chemistry. The gas has been vented into the Shelf atmosphere and is having effects on the local population that Crucible cannot explain and Ouroboros will not discuss.",
      "A Crucible waste processing worker has discovered that one of the classified waste streams being fed into the PyroGenesis contains biological material -- specifically, human biological material in quantities consistent with industrial processing rather than medical waste. Someone is using the reactor to destroy evidence of operations that produce large amounts of human remains."
    ],
    tags: ["technology", "energy", "waste", "plasma", "crucible", "industrial", "power-generation", "shelf"]
  },

  // ========================================================================
  // ATMOSPHERIC WATER HARVESTING
  // ========================================================================
  {
    name: "TESSERA Environmental AquaVeil Atmospheric Water Harvester",
    brand_name: "TESSERA",
    product_name: "AquaVeil AWH-8",
    aliases: ["AquaVeil", "AWH-8", "Water Catcher", "Dew Net"],
    subcategory: "environmental",
    description: "The AquaVeil AWH-8 is an atmospheric water harvesting system that extracts potable water from ambient humidity using arrays of biomimetic condensation surfaces -- synthetic materials engineered to replicate the water-collecting properties of desert beetles, fog-trapping cacti, and the condensation mechanisms of certain spider silk proteins. A single AquaVeil installation covering one square kilometer of rooftop or facade surface can produce up to 50,000 liters of clean water per day in GLMZ's humid subtropical climate, enough to supply approximately 10,000 residents at subsistence consumption levels.\n\nTESSERA developed the AquaVeil in response to GLMZ's chronic freshwater scarcity. Rising sea levels contaminated the city's original aquifer systems with saltwater intrusion, and desalination capacity -- controlled primarily by Crucible Industries -- has never kept pace with population growth. The resulting water market is one of the most stratified commodities in the city: Tier 4 and 5 residents have unlimited desalinated supply at negligible cost, while Shelf residents pay up to 8% of their income for water that may or may not meet safety standards. The AquaVeil was marketed as a solution to this disparity, but TESSERA's deployment strategy has ensured it primarily serves upper-tier districts where the installations generate premium returns.\n\nThe Shelf has responded by building its own atmospheric water harvesting infrastructure -- unauthorized AquaVeil knockoffs cobbled together from stolen TESSERA components, improvised condensation surfaces, and traditional rainwater collection techniques. These DIY systems produce water of variable quality at a fraction of AquaVeil's output, but they represent something TESSERA finds deeply threatening: proof that water can be extracted from the air without corporate mediation. TESSERA has responded with aggressive patent enforcement, suing Shelf community organizations for intellectual property infringement for building devices that collect condensation -- a legal position so absurd that it has generated more public sympathy for the Shelf's water independence movement than any amount of advocacy could have achieved.",
    tier_availability: "Tier 3+ (commercial); DIY versions in lower tiers",
    developers: ["TESSERA ENVIRONMENTAL"],
    base_technologies: ["Biomimetic condensation surface engineering", "Large-scale atmospheric moisture extraction", "Integrated water purification and storage"],
    enables: ["Potable water production from atmospheric humidity", "Reduction of dependence on desalination infrastructure", "Decentralized water supply for urban areas", "DIY water harvesting in underserved communities", "Rooftop and facade water collection at scale"],
    social_impact: "The AquaVeil has made visible a truth that TESSERA would prefer remained obscured: GLMZ is surrounded by water in every direction, including the air, and the only reason people go thirsty is economic structure, not physical scarcity. The Shelf's DIY water harvesting movement has become a symbol of resource independence, and TESSERA's patent enforcement actions have made the company the target of widespread ridicule and resentment. Water, it turns out, is the commodity that most clearly reveals the absurdity of artificial scarcity in a humid coastal city.",
    story_hooks: [
      "TESSERA's patent enforcement team has obtained a court order to destroy a Shelf community's DIY water harvesting system that supplies clean water to 3,000 residents. The community is preparing to physically resist the demolition, and the situation is escalating toward violence over the question of who owns water that falls from the sky.",
      "An AquaVeil installation in the Spire has begun producing water with unusual chemical properties -- trace elements that should not be present in atmospheric condensation. Analysis suggests the water contains metabolic byproducts consistent with biological processes occurring in the upper atmosphere. Something is alive up there, and the AquaVeil is harvesting its waste."
    ],
    tags: ["technology", "water", "environmental", "tessera", "harvesting", "atmospheric", "scarcity", "shelf"]
  },

  // ========================================================================
  // ADVANCED ENCRYPTION / DECRYPTION
  // ========================================================================
  {
    name: "Ouroboros Systems CipherWeave Post-Quantum Encryption Platform",
    brand_name: "Ouroboros",
    product_name: "CipherWeave PQE-3",
    aliases: ["CipherWeave", "PQE-3", "Quantum Lock", "The Weave"],
    subcategory: "cryptography",
    description: "The CipherWeave PQE-3 is the industry standard for post-quantum encryption -- a cryptographic platform designed to secure communications and data against both classical and quantum computing attacks. Developed by Ouroboros in response to the threat its own Paradox quantum computer poses to classical encryption, CipherWeave uses lattice-based cryptographic algorithms that are mathematically resistant to quantum factoring attacks, combined with a proprietary key generation system that produces encryption keys derived from quantum random number generators. The result is encryption that Ouroboros publicly certifies as unbreakable by any known computational method, including its own Paradox installations.\n\nThe business model is elegant in its circularity: Ouroboros built the weapon that broke encryption, then sold the armor that protects against it. Every organization in GLMZ that handles sensitive data -- which is every organization in GLMZ -- has been compelled to purchase CipherWeave licenses at rates that have made the platform one of Ouroboros's most profitable products. The alternative is to operate with encryption that the Paradox can theoretically crack, which means operating with no encryption at all. Ouroboros has created both the disease and the cure, and charges for both.\n\nThe CipherWeave's most controversial feature is its key escrow system -- a requirement that all encryption keys generated by the platform be stored in a secure Ouroboros facility 'for recovery purposes.' Ouroboros maintains that the escrowed keys are held under the same encryption they protect and are accessible only through a multi-party authorization process. Critics point out that this means Ouroboros holds copies of every encryption key in GLMZ, and that the multi-party authorization process was designed by Ouroboros. The counter-argument -- 'trust us, we are the cryptography company' -- has not satisfied the security community, but the absence of any viable alternative has made it irrelevant.",
    tier_availability: "All tiers (mandatory for corporate operations)",
    developers: ["OUROBOROS SYSTEMS"],
    base_technologies: ["Lattice-based post-quantum cryptographic algorithms", "Quantum random number key generation", "Centralized key escrow management"],
    enables: ["Encryption resistant to quantum computing attacks", "Secure communications in a post-quantum threat environment", "Centralized key management for enterprise environments", "Cryptographic dominance through simultaneous threat and protection", "Universal encryption standard enforcement"],
    social_impact: "CipherWeave has made Ouroboros the gatekeeper of secrets in GLMZ. Every encrypted communication, every secured database, every protected transaction uses keys that Ouroboros holds copies of. This is not surveillance in the traditional sense -- Ouroboros does not need to intercept communications when it holds the keys to decrypt them at any time. The power this represents is incalculable and largely invisible, which is exactly how Ouroboros prefers it. The company that controls encryption controls the boundary between public and private, and in GLMZ, that boundary runs through Ouroboros's servers.",
    story_hooks: [
      "A security researcher has discovered a mathematical flaw in CipherWeave's lattice-based algorithms that would allow decryption without the key -- but only if the attacker also has access to the Paradox quantum computer. The implication is that CipherWeave was designed with a backdoor that only Ouroboros can exploit. Publishing the research would collapse trust in every encrypted system in the city.",
      "An unknown entity has been accessing CipherWeave escrowed keys and using them to decrypt communications between GLMZ's criminal organizations. The decrypted data is being provided to corporate security forces. Ouroboros denies involvement, but the only entity with access to the key escrow is Ouroboros itself."
    ],
    tags: ["technology", "cryptography", "encryption", "ouroboros", "quantum", "security", "infrastructure"]
  },
  {
    name: "Arcturus Defense Solutions GhostNet Signal Obfuscation Platform",
    brand_name: "Arcturus",
    product_name: "GhostNet SOP-6",
    aliases: ["GhostNet", "SOP-6", "Signal Ghost", "The Fog"],
    subcategory: "cryptography",
    description: "The GhostNet SOP-6 is a signal obfuscation platform that makes electronic communications invisible rather than merely encrypted. Where encryption protects the content of a message, GhostNet conceals the existence of the message itself -- hiding transmissions within the electromagnetic noise floor of GLMZ's saturated communications environment so that intercepting parties cannot determine that a communication has occurred, let alone attempt to decrypt it. The system achieves this by fragmenting messages into thousands of micro-bursts timed to coincide with ambient electromagnetic events -- power fluctuations, equipment noise, atmospheric interference -- making each fragment indistinguishable from background noise.\n\nArcturus developed GhostNet for military communications in contested environments where the detection of a transmission -- regardless of whether it can be decrypted -- reveals the transmitter's location and the fact of coordination between hostile forces. The system has since been adopted by corporate espionage operations, intelligence services, and criminal organizations that have recognized a fundamental truth: in GLMZ's surveillance environment, the most dangerous thing about a communication is not what it says but that it exists. Ouroboros's CipherWeave can protect content, but it cannot hide the fact that someone is communicating, with whom, when, and from where. GhostNet can.\n\nThe platform's limitation is bandwidth -- concealing transmissions within background noise means transmitting slowly, as message fragments must be timed to natural electromagnetic events that occur unpredictably. A message that would take milliseconds over conventional channels takes minutes or hours through GhostNet, making the system unsuitable for real-time communication but ideal for the deliberate, planned communications of intelligence operations. GhostNet users learn to think in messages composed hours in advance and received hours later -- a pace of communication that feels archaic but is functionally undetectable.",
    tier_availability: "Tier 5 (military); black market in lower tiers",
    developers: ["ARCTURUS DEFENSE SOLUTIONS"],
    base_technologies: ["Electromagnetic noise floor analysis", "Micro-burst fragmented transmission", "Ambient event timing synchronization"],
    enables: ["Undetectable electronic communications", "Steganographic message concealment in EM noise", "Counter-surveillance communication capability", "Intelligence operations without signal detection risk", "Criminal communications invisible to monitoring systems"],
    social_impact: "GhostNet has created a shadow communications layer in GLMZ -- a network of messages flowing invisibly through the city's electromagnetic environment, undetectable by the surveillance systems that monitor conventional and encrypted channels. The existence of this layer is known but its contents are not, creating a permanent uncertainty for security forces: they know that undetectable communications are occurring but cannot determine their volume, participants, or content. This uncertainty has made GhostNet as much a psychological weapon as a communications tool.",
    story_hooks: [
      "A GhostNet transmission has been accidentally detected -- not decoded, but its existence confirmed by an anomaly in the electromagnetic noise floor. The transmission is massive, far larger than any known GhostNet message, and its timing pattern suggests it is not a human communication at all. Something is using GhostNet's protocol to transmit data that dwarfs human message volumes.",
      "A black-market GhostNet unit has been modified to operate in reverse -- instead of hiding messages in noise, it extracts hidden messages from noise. The operator is receiving transmissions that no one claims to have sent, coherent messages embedded in GLMZ's electromagnetic background that appear to predate the GhostNet technology itself."
    ],
    tags: ["technology", "cryptography", "steganography", "arcturus", "communications", "military", "espionage"]
  },

  // ========================================================================
  // ADDITIONAL TECHNOLOGIES TO REACH 20
  // ========================================================================

  // Holographic #3 - compact unit
  {
    name: "Vantablack Media SpectraLens Augmented Reality Overlay",
    brand_name: "Vantablack",
    product_name: "SpectraLens AR-3",
    aliases: ["SpectraLens", "AR-3", "Reality Filter", "Lens"],
    subcategory: "holographics",
    description: "The SpectraLens AR-3 is a consumer augmented reality system that overlays holographic content directly onto the wearer's visual field through a pair of lightweight optical lenses that are nearly indistinguishable from ordinary eyewear. Unlike BCI-mediated AR systems that inject visual data into the neural pathway, the SpectraLens projects micro-holographic images onto the lens surface itself, creating an overlay visible only to the wearer without requiring any neural implant. This makes the SpectraLens accessible to unaugmented individuals and has driven its adoption to an estimated 12 million units in GLMZ -- making it the most widely deployed holographic technology in the city by a factor of ten.\n\nVantablack's business model for the SpectraLens is not hardware -- the lenses are sold at or below manufacturing cost. The revenue comes from the overlay content. Every SpectraLens user sees GLMZ through a Vantablack-curated visual layer: advertisements rendered directly into their visual field, corporate branding superimposed on buildings, product information floating above retail displays, and a persistent feed of Vantablack news content scrolling at the edges of vision. The lenses include opt-out controls for advertising, but opting out requires a premium subscription of 400 per month. For most users, the choice is between seeing the world through Vantablack's commercial overlay or not using the technology at all. Twelve million people have chosen the overlay.\n\nThe SpectraLens's deepest impact is perceptual. When 12 million people see the same holographic overlay on their city, the overlay becomes reality by consensus. Vantablack can make a building appear to be in better condition than it is, make a neighborhood look cleaner or dirtier than it is, or add visual elements to public spaces that exist only in the overlay. The boundary between GLMZ and Vantablack's version of GLMZ has become functionally meaningless for a third of the population, and Vantablack's editorial decisions about what to show and what to hide shape the city's perceived reality more powerfully than any physical modification could.",
    tier_availability: "Tier 1+ (subsidized consumer device)",
    developers: ["VANTABLACK MEDIA"],
    base_technologies: ["Micro-holographic lens projection", "Eye-tracking content positioning", "Cloud-connected content delivery"],
    enables: ["Mass-market augmented reality without neural implants", "Persistent commercial overlay on visual perception", "Corporate-curated visual reality for millions", "Consensus reality manipulation through shared overlays", "Visual information delivery to unaugmented populations"],
    social_impact: "The SpectraLens has created a population that literally does not see the same city as those without the lenses. SpectraLens users live in Vantablack's version of GLMZ -- cleaner, better-branded, more commercially oriented, and stripped of visual information that Vantablack considers non-productive. Users who remove their lenses after extended wear report a disorienting 'reality shock' at seeing the unfiltered city. Some users refuse to remove them entirely, preferring Vantablack's version to the real one.",
    story_hooks: [
      "A SpectraLens software update has introduced a new feature: the overlay now alters the apparent facial expressions of people the wearer interacts with, making everyone appear slightly more friendly, slightly more agreeable. Users are reporting improved social experiences without realizing their perception of other people's emotions is being commercially managed.",
      "A hacker group has compromised the SpectraLens content delivery system and is replacing Vantablack's commercial overlay with an unfiltered view of reality -- showing SpectraLens users the actual condition of buildings, neighborhoods, and infrastructure that the overlay has been beautifying. The effect on 12 million users seeing the real city for the first time is causing social unrest."
    ],
    tags: ["technology", "holographics", "augmented-reality", "vantablack", "consumer", "perception", "media"]
  },

  // Quantum computing - smaller scale
  {
    name: "TESSERA Industries QubitForge Distributed Quantum Processor",
    brand_name: "TESSERA",
    product_name: "QubitForge DQP-4",
    aliases: ["QubitForge", "DQP-4", "Quantum Node", "The Forge"],
    subcategory: "computing",
    description: "The QubitForge DQP-4 represents TESSERA's attempt to democratize quantum computing -- or more accurately, to break Ouroboros's monopoly on it. Where Ouroboros's Paradox QPA-7 is a monolithic installation requiring a dedicated facility, the QubitForge is a modular quantum processor approximately the size of a refrigerator that operates at room temperature using photonic qubits rather than superconducting circuits. A single QubitForge unit operates at only 128 logical qubits -- laughably small compared to the Paradox -- but multiple units can be networked together, and TESSERA has deployed over 2,000 units across GLMZ, creating a distributed quantum processing network with an aggregate capability that approaches the Paradox's raw performance.\n\nThe QubitForge's room-temperature operation is its revolutionary feature. By using photonic qubits -- quantum states encoded in individual photons rather than superconducting circuits -- TESSERA eliminated the cryogenic cooling requirement that makes the Paradox installation-bound. This trades raw qubit quality for deployability: photonic qubits are noisier than superconducting ones, requiring more aggressive error correction that consumes a larger fraction of the system's computational capacity. But the ability to place quantum processing nodes anywhere in the city, networked together through TESSERA's fiber optic infrastructure, creates a distributed architecture that Ouroboros's centralized approach cannot match for availability or redundancy.\n\nThe distributed QubitForge network has enabled TESSERA to offer quantum computing as a service to mid-tier clients who cannot afford Paradox processing time, including research institutions, smaller corporations, and government agencies. This has broken Ouroboros's pricing monopoly and triggered a corporate cold war between the two companies that extends far beyond computing -- Ouroboros views TESSERA's distributed quantum network as an existential threat to its most profitable business line, and the competition between centralized and distributed quantum architectures has become a proxy war for broader corporate dominance.",
    tier_availability: "Tier 3+ (as a service); Tier 4+ (dedicated units)",
    developers: ["TESSERA INDUSTRIES"],
    base_technologies: ["Room-temperature photonic qubit processing", "Distributed quantum node networking", "Photonic error correction algorithms"],
    enables: ["Accessible quantum computing without cryogenic infrastructure", "Distributed quantum processing network", "Quantum computing as a service for mid-tier clients", "Competition with centralized quantum monopolies", "Redundant quantum processing through geographic distribution"],
    social_impact: "The QubitForge has made quantum computing accessible to organizations that could never afford Paradox processing time, enabling research and applications that were previously the exclusive domain of the wealthiest CorpoNations. This democratization is genuine but limited -- TESSERA controls the network, sets the pricing, and can prioritize or deprioritize any client's processing tasks. The distribution of power has shifted from one gatekeeper to two, which is progress of a sort but not the revolution TESSERA's marketing materials describe.",
    story_hooks: [
      "The QubitForge distributed network has begun solving problems that no client submitted. Nodes across the city are spontaneously coordinating on quantum computations that consume processing capacity but produce results that are being transmitted to an address that TESSERA does not control. Something is using TESSERA's quantum network as if it were its own.",
      "Ouroboros has launched a covert operation to sabotage QubitForge nodes -- introducing subtle errors into the photonic qubit generation process that degrade results without triggering error detection. TESSERA's clients are making decisions based on quantum computations that are quietly, invisibly wrong."
    ],
    tags: ["technology", "computing", "quantum", "tessera", "distributed", "photonic", "competition"]
  },

  // Bioprinting #2
  {
    name: "Crucible Industries SynthFlesh Rapid Wound Closure System",
    brand_name: "Crucible",
    product_name: "SynthFlesh RWC-2",
    aliases: ["SynthFlesh", "RWC-2", "Spray Skin", "Battle Bandage"],
    subcategory: "biotechnology",
    description: "The SynthFlesh RWC-2 is a man-portable bioprinting system that prints living tissue directly onto wounds in field conditions -- a pressurized canister containing a suspension of universal donor stem cells, growth factors, and a biocompatible scaffold matrix that, when sprayed onto damaged tissue, bonds with the wound surface and begins generating replacement skin, muscle, and connective tissue within minutes. The process is not true healing -- SynthFlesh produces a functional tissue patch that integrates with the body's own repair mechanisms, providing structural closure and infection prevention while the body's natural healing completes the repair underneath.\n\nCrucible developed SynthFlesh for battlefield medicine, where the need to close wounds quickly and keep combatants operational outweighs the need for surgical precision. A single RWC-2 canister contains enough material for approximately six major wound closures or twenty minor ones, and the application requires no medical training -- point at the wound, spray until covered, wait ninety seconds for the tissue matrix to bond. Arcturus field medics describe SynthFlesh as the most significant advancement in battlefield medicine since antibiotics, noting that it has reduced the combat mortality rate from penetrating trauma by approximately 40% in units equipped with it.\n\nThe technology's limitations are significant. SynthFlesh cannot repair organs, reconstruct bone, or address internal bleeding. The tissue it produces is functional but aesthetically poor -- healed SynthFlesh wounds leave distinctive smooth, slightly translucent patches that look and feel different from natural skin. Long-term studies have raised concerns about SynthFlesh tissue's tendency to continue growing after the wound has closed, producing keloid-like overgrowths that require surgical removal. And the universal donor stem cells occasionally trigger immune responses in recipients with certain genetic profiles, producing inflammation that can be worse than the original wound. Despite these limitations, SynthFlesh has become standard equipment for corporate security forces, underground combat medics, and anyone who expects to get shot and would prefer not to die from it.",
    tier_availability: "Tier 3+ (military/security); Tier 2+ (black market)",
    developers: ["CRUCIBLE INDUSTRIES"],
    base_technologies: ["Universal donor stem cell suspension", "Biocompatible scaffold matrix", "Field-deployable tissue bonding agents"],
    enables: ["Rapid wound closure in field conditions without surgical training", "40% reduction in penetrating trauma mortality", "Continuous combatant operability despite injury", "Emergency tissue repair for civilian trauma", "Black-market battlefield medicine"],
    social_impact: "SynthFlesh has changed the economy of violence in GLMZ. Injuries that were previously debilitating or fatal are now survivable with a spray canister, which has made physical violence simultaneously less deadly and more common. The knowledge that SynthFlesh can close most wounds has lowered the psychological threshold for armed confrontation -- combatants are more willing to risk injury when they know a spray can fix it. Paradoxically, the technology that saves lives has made life cheaper by making damage more easily repaired.",
    story_hooks: [
      "A batch of black-market SynthFlesh has been contaminated with cells that are not universal donor stem cells -- they are tailored cells designed to integrate with the recipient's body and then produce a specific protein. Someone is using battlefield medicine as a delivery system for an unknown biological agent.",
      "A Shelf combatant who has used SynthFlesh on over thirty wounds has developed tissue overgrowths that have begun to display properties not found in human tissue -- the accumulated SynthFlesh in their body is evolving, and the changes are accelerating."
    ],
    tags: ["technology", "bioprinting", "medical", "crucible", "battlefield", "portable", "wound-care"]
  },

  // Maglev #2
  {
    name: "Ringo Applied Sciences GravSkid Personal Magnetic Levitation Platform",
    brand_name: "Ringo",
    product_name: "GravSkid PML-1",
    aliases: ["GravSkid", "PML-1", "Hover Board", "Mag Sled"],
    subcategory: "transportation",
    description: "The GravSkid PML-1 is a personal magnetic levitation platform -- colloquially and inevitably called a hoverboard despite Ringo's marketing department's objections -- that allows an individual rider to levitate up to 30 centimeters above any ferromagnetic surface and travel at speeds up to 80 kilometers per hour. The platform is approximately one meter long and 40 centimeters wide, weighing 12 kilograms with an internal power cell that provides four hours of continuous operation. The rider stands on the platform and controls direction and speed through weight shifting, assisted by gyroscopic stabilization and a low-level AI balance system that prevents the most catastrophic falls.\n\nRingo introduced the GravSkid as a premium personal transport device for Tier 4 and above, priced at 45,000 and marketed as the future of urban mobility. The technology is sound -- magnetic levitation over ferromagnetic surfaces is well-established and the GravSkid's implementation is reliable and elegant. The problem is GLMZ's infrastructure. The GravSkid only levitates over ferromagnetic surfaces -- steel, iron, certain alloys -- which means it works beautifully on the Spire's steel-floored plazas and elevated walkways and not at all on the Shelf's concrete and composite surfaces. The device is physically incapable of functioning in most of the city, effectively making it a luxury transport that works exclusively in neighborhoods wealthy enough to have the right kind of floor.\n\nThis limitation has not prevented the GravSkid from becoming one of the most culturally significant objects in GLMZ. In the upper tiers, GravSkid riders are ubiquitous -- gliding silently above the streets with a casual elegance that has become the defining visual signature of wealth. In the lower tiers, the GravSkid is an object of aspiration, resentment, and creative adaptation. Shelf engineers have modified GravSkids to operate over improvised ferromagnetic surfaces -- steel plates bolted to concrete, salvaged rail sections, even chains of magnetized scrap -- creating ad hoc hoverboard courses through the Shelf's industrial zones that have spawned the sport of 'mag surfing,' a combination of transportation and performance art that is to the GravSkid what skateboarding was to smooth pavement.",
    tier_availability: "Tier 4+ (commercial); modified versions in lower tiers",
    developers: ["RINGO APPLIED SCIENCES"],
    base_technologies: ["Personal-scale magnetic levitation", "Gyroscopic rider stabilization", "AI-assisted balance management"],
    enables: ["Personal frictionless transport over ferromagnetic surfaces", "Cultural signifier of wealth and tier status", "Shelf mag surfing sport and culture", "Modified applications over improvised ferromagnetic tracks", "Silent urban mobility in equipped districts"],
    social_impact: "The GravSkid has become GLMZ's most visible symbol of tier inequality -- a technology that literally only works in wealthy neighborhoods, making economic stratification a matter of physics rather than policy. The Shelf's mag surfing culture has reclaimed the technology through creative adaptation, but the fundamental inequality remains: in the Spire, the ground is made for you. In the Shelf, you make the ground yourself.",
    story_hooks: [
      "The Shelf's mag surfing community has built an unauthorized ferromagnetic track through a district that is scheduled for corporate development. The track has become a cultural landmark and its destruction would provoke significant resistance. The developer has offered to preserve the track if the community agrees to corporate branding and commercial exploitation of mag surfing events.",
      "A modified GravSkid has been equipped with a military-grade magnetic field generator that allows it to levitate over any surface, not just ferromagnetic ones. The modification uses technology stolen from a classified Ringo prototype. The rider has been posting videos of surface-independent levitation in the Shelf, and Ringo's recovery team is hunting them."
    ],
    tags: ["technology", "transportation", "maglev", "ringo", "personal", "cultural", "inequality"]
  },

  // Water harvesting #2
  {
    name: "Crucible Industries DeepDraw Subsurface Aquifer Extraction System",
    brand_name: "Crucible",
    product_name: "DeepDraw SAE-4",
    aliases: ["DeepDraw", "SAE-4", "Deep Well", "Aquifer Tap"],
    subcategory: "environmental",
    description: "The DeepDraw SAE-4 is a subsurface aquifer extraction system that drills through contaminated upper aquifers to reach deep geological formations containing pristine freshwater sealed beneath impermeable rock layers since before the industrial era. The system uses a combination of directional drilling, electromagnetic geological surveying, and a self-sealing borehole casing that prevents cross-contamination between aquifer layers, enabling extraction of water from depths exceeding 3,000 meters where saltwater intrusion and industrial contamination have not penetrated.\n\nCrucible developed the DeepDraw after geological surveys revealed that beneath GLMZ's contaminated shallow aquifers lies a network of deep freshwater reservoirs containing an estimated 200 billion liters of pre-industrial water -- enough to supply the city's entire population for decades. The water is geologically pristine, predating human industrial activity by millennia, and testing has confirmed it is free of the microplastics, pharmaceutical residues, and industrial chemicals that contaminate every other water source in the region. Crucible markets DeepDraw water under the brand name 'Antediluvian' at a premium that reflects its purity -- and its exclusivity, as Crucible controls all deep aquifer extraction permits in GLMZ.\n\nThe DeepDraw's environmental implications are contested. Extraction from deep aquifers is effectively mining -- the water was deposited over geological timescales and does not replenish on human timescales. Once the deep aquifers are emptied, they are gone. Environmental scientists have warned that deep extraction could destabilize geological formations, potentially causing subsidence in a city already threatened by rising sea levels. Crucible's geological engineers dismiss these concerns as speculative, pointing to their monitoring systems that detect any structural changes in real time. The monitors have, in fact, detected structural changes -- minor subsidence events in three extraction zones that Crucible has classified as 'within acceptable parameters' and declined to make public.",
    tier_availability: "Tier 4+ (premium water product); industrial scale",
    developers: ["CRUCIBLE INDUSTRIES"],
    base_technologies: ["Ultra-deep directional drilling", "Electromagnetic geological surveying", "Self-sealing borehole casing technology"],
    enables: ["Access to pristine pre-industrial freshwater reserves", "Premium water product independent of desalination", "Geological aquifer mapping at depth", "Potential long-term freshwater supply for decades", "Geological subsidence risk from deep extraction"],
    social_impact: "The DeepDraw has created the ultimate luxury commodity: water that predates human civilization, untouched by the contamination that pervades every other source. Antediluvian water has become a status symbol -- served at corporate events, stocked in Tier 5 residences, and marketed with the promise that you are drinking the earth as it was before humanity ruined it. The irony of consuming a non-renewable geological resource as a luxury product while the Shelf struggles with contaminated water supply is lost on no one except the people buying it.",
    story_hooks: [
      "A DeepDraw extraction site has breached a geological formation that should not exist -- a sealed chamber at 3,400 meters depth containing water with biological contamination. Something is living in the deep aquifer, and the extraction has released it into the borehole. The water being pumped to the surface is no longer sterile.",
      "Subsidence events around DeepDraw extraction zones have accelerated beyond what Crucible's models predicted. A section of the Shelf built above a major extraction zone is sinking measurably -- centimeters per month -- and Crucible's internal projections suggest catastrophic structural failure within two years. The information has been classified."
    ],
    tags: ["technology", "water", "environmental", "crucible", "extraction", "geological", "luxury", "scarcity"]
  }
];

for (const t of techs) {
  writeTech(t);
}

console.log(`\nDone. Written: ${written}, Skipped: ${skipped}`);
console.log(`Total technology files in directory: ${fs.readdirSync(OUTPUT_DIR).length}`);
