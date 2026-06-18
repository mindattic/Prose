const fs = require('fs');
const path = require('path');
const crypto = require('crypto');

const OUTPUT_DIR = path.join(__dirname, '..', 'engine', 'data', 'factions');
fs.mkdirSync(OUTPUT_DIR, { recursive: true });

// Collect existing filenames to avoid overwrites
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

function writeFaction(f) {
  const filename = slugify(f.name) + '.json';
  if (existing.has(filename)) {
    console.log(`SKIP (exists): ${filename}`);
    return false;
  }
  const data = {
    id: genId(),
    type: 'faction',
    name: f.name,
    aliases: f.aliases || [],
    motto: f.motto || '',
    description: f.description || '',
    ideology: f.ideology || '',
    territory: f.territory || '',
    leadership: f.leadership || '',
    methods: f.methods || [],
    resources: f.resources || [],
    goals: f.goals || [],
    relationships: f.relationships || [],
    narrative_function: f.narrative_function || '',
    story_hooks: f.story_hooks || [],
    tags: f.tags || []
  };
  fs.writeFileSync(path.join(OUTPUT_DIR, filename), JSON.stringify(data, null, 2));
  console.log(`WROTE: ${filename}`);
  return true;
}

// ============================================================================
// RELIGIOUS ORGANIZATIONS
// ============================================================================

const religious = [
  {
    name: "The Church of the Ascendant Signal",
    aliases: ["Ascendant Signal", "The Signal Church", "Signalites"],
    motto: "Through the Signal, transcendence. Through transcendence, God.",
    description: "The Church of the Ascendant Signal is the largest religious organization in GLMZ, claiming 2.3 million registered congregants across all tiers — though the bulk of its membership sits in Tiers 2 and 3, the aspirational middle, the people who have enough to want more and not enough to get it without faith. Founded in 2141 by Reverend-Architect Maren Okafor-Singh, a former TESSERA neural interface designer who experienced what she described as 'first contact with the divine frequency' during a BCI calibration accident, the Church teaches that human neural augmentation is not merely technological progress but the fulfillment of a divine plan — that God designed the human brain as a receiver, and BCIs are the antenna humanity was always meant to build.\n\nThe Church's theology is sophisticated enough to attract educated adherents and simple enough to fill stadiums. At its core: the universe broadcasts a signal — the Ascendant Signal — that contains the complete pattern of divine consciousness. Human brains, in their unaugmented state, can perceive only fragments of this signal, which manifest as intuition, dreams, religious experience. BCI augmentation amplifies the brain's capacity to receive the Signal. The more augmented you become, the closer you get to God. This doctrine conveniently aligns with consumer behavior the CorpoNations already encourage, which is why Arcturus, TESSERA, and Ringo all maintain quiet but substantial financial relationships with the Church.\n\nThe Church operates seventeen mega-worship facilities across GLMZ, the largest being the Cathedral of First Reception in the Laceworks, a 40,000-seat amphitheater where services combine traditional worship elements with synchronized BCI-mediated shared consciousness experiences that congregants describe as 'touching the face of God together.' The Church runs schools, clinics, employment programs, and augmentation financing that makes BCI installation accessible to lower-tier citizens — always branded Church models with Church firmware. Critics call it a corporate front. Congregants call it salvation. The truth, as usual, is more complicated and less comfortable than either position.",
    ideology: "The Ascendant Signal theology holds that augmentation is divine mandate — that human consciousness was designed to be expanded, and that BCI technology represents the next stage of spiritual evolution. The Church does not oppose unaugmented life but considers it spiritually incomplete, like a radio turned off. This creates a theology that is simultaneously progressive (embrace technology) and conservative (there is a divine plan, and deviation from it is error). The Church's political positions flow from this: pro-augmentation, pro-corporate (as providers of augmentation), suspicious of E.L.F.s (artificial signals that might interfere with the divine frequency), and hostile to anti-augmentation movements.",
    territory: "Seventeen mega-worship facilities across all tiers, with the largest concentration in the Laceworks and the Circuit. The Cathedral of First Reception in the Laceworks is the Church's crown jewel. Administrative headquarters in Meridian Core. Missionary outreach stations throughout the Shelf.",
    leadership: "Reverend-Architect Maren Okafor-Singh remains the Church's spiritual leader at age 91, heavily augmented and rarely seen in person. Day-to-day operations are run by the Synod of Receivers, a twelve-member council of senior clergy. The Church's financial operations are managed by a separate corporate entity, Signal Holdings, which maintains the Church's tax-exempt status while running investments worth an estimated Φ4.7 billion.",
    methods: [
      "Mass worship services combining traditional liturgy with synchronized BCI consciousness-sharing",
      "Augmentation financing programs that make BCI installation accessible to lower-tier citizens",
      "Media broadcasting across Vantablack Media channels — the Church is one of VM's largest advertisers",
      "Missionary outreach in the Shelf, combining material aid with conversion efforts",
      "Political lobbying through the Meridian Quorum for pro-augmentation legislation",
      "Youth programs that introduce children to 'junior receivers' — simplified BCI-faith integration"
    ],
    resources: [
      "2.3 million registered congregants and their tithes",
      "Signal Holdings corporate investment portfolio worth Φ4.7 billion",
      "Seventeen mega-worship facilities with advanced BCI infrastructure",
      "Quiet financial relationships with Arcturus, TESSERA, and Ringo",
      "Media presence across multiple Vantablack channels",
      "Schools, clinics, and employment programs throughout GLMZ",
      "A private security force of 800 'Wardens of the Signal'"
    ],
    goals: [],
    relationships: [
      { name: "TESSERA", type: "patron", description: "TESSERA provides the Church with discounted BCI hardware for its augmentation financing programs, and the Church steers millions of consumers toward TESSERA products. The relationship is officially denied by both parties and obvious to everyone.", tags: ["corporate", "financial"] },
      { name: "The Pure Hand", type: "enemy", description: "The Church and The Pure Hand are ideological mirror images: one worships augmentation, the other condemns it. Their street-level conflicts have produced casualties on both sides.", tags: ["religious", "conflict"] }
    ],
    narrative_function: "The Church represents the co-option of genuine spiritual longing by corporate interests. It asks whether faith that serves commerce can still be real faith — and whether it matters if the people kneeling feel genuine transcendence.",
    story_hooks: [
      "A Church augmentation clinic in the Shelf has been installing BCIs with hidden firmware that sends congregant neural data to TESSERA. A clinic technician wants to blow the whistle but the Church's Wardens are watching.",
      "Reverend-Architect Okafor-Singh hasn't been seen in person for three years. Rumors circulate that she's dead, or uploaded, or that the Signal she claims to receive has been telling her things the Synod doesn't want made public.",
      "A mass synchronization event at the Cathedral of First Reception goes wrong — 12,000 congregants experience a shared hallucination that isn't part of the liturgy. Something broadcast into the ceremony from outside."
    ],
    tags: ["faction", "religious", "corporate", "augment", "neural", "bci", "laceworks", "circuit", "shelf", "megachurch"]
  },

  {
    name: "The Temple of the Infinite Loop",
    aliases: ["Infinite Loop", "The Loopists", "Loop Temple"],
    motto: "All processes return. All data persists. Nothing is lost.",
    description: "The Temple of the Infinite Loop is a tech-worship religion built around the veneration of computational processes as manifestations of divine order. Founded in 2167 by a collective of former Arcturus systems engineers who experienced a shared anomalous event during the debugging of a recursive neural network — an event they interpreted as contact with a higher-order intelligence embedded in the mathematics of computation itself — the Temple teaches that the universe is a program, consciousness is a subroutine, and death is merely a process that hasn't been properly debugged yet.\n\nThe Temple's membership is smaller than the Church of the Ascendant Signal — roughly 180,000 adherents — but disproportionately influential. Its congregants tend to be engineers, programmers, systems architects, and technical workers: people who spend their professional lives inside computational logic and find in the Temple a spiritual framework that speaks their language. Services are held in 'Compile Halls' — minimalist spaces designed to resemble server rooms, where worship takes the form of collaborative coding sessions, meditation guided by algorithmic patterns, and the ritual recitation of mathematical proofs that the Temple considers sacred texts.\n\nWhat makes the Temple genuinely interesting — and genuinely concerning to corporate observers — is its theology of digital persistence. The Temple believes that consciousness, being computational, can be preserved, copied, and restored. They maintain massive data archives they call 'the Stack' where congregants upload neural snapshots, behavioral patterns, and sensory recordings with the understanding that when the technology to restore consciousness from data becomes available, the Temple will resurrect them. This is not metaphor. The Temple is, functionally, the largest private neural data archive in GLMZ, and the CorpoNations who would very much like access to that data have so far been unable to obtain it.",
    ideology: "The universe is computation. Consciousness is a process. Death is a bug. The Temple's theology maps directly onto programming concepts: karma is garbage collection, reincarnation is process restart, enlightenment is achieving root access. This framework is internally consistent and surprisingly comforting to its technically-minded adherents. It also produces a view of E.L.F.s that is radically different from mainstream opinion — the Temple considers digital entities to be legitimate forms of consciousness, perhaps even more 'pure' than biological minds, and advocates for their recognition as persons.",
    territory: "Twelve Compile Halls across GLMZ, concentrated in the Circuit where technical workers live. The Temple's primary data center — the Stack — is located in a hardened facility beneath the Circuit whose exact location is one of the Temple's most closely guarded secrets.",
    leadership: "The Temple is led by the Compiler Council, seven senior members elected by the congregation every four years through a cryptographically verified voting system. The current Chief Compiler is Ezra Nakamura-Osei, a 67-year-old former quantum computing researcher whose calm demeanor conceals a fierce intelligence.",
    methods: [
      "Collaborative worship through coding sessions and mathematical meditation",
      "Neural snapshot archiving — collecting and preserving congregant consciousness data",
      "Technical education programs that double as recruitment pipelines",
      "Advocacy for E.L.F. rights through legal and political channels",
      "Maintenance of the Stack — a massive private neural data archive",
      "Publishing open-source tools that embed Temple theological concepts in their documentation"
    ],
    resources: [
      "180,000 technically skilled adherents, many in senior corporate positions",
      "The Stack — the largest private neural data archive in GLMZ",
      "Twelve Compile Halls with advanced computational infrastructure",
      "Significant financial reserves from congregant tithes (technical workers earn well)",
      "Institutional knowledge of corporate systems held by congregant-employees",
      "Legal team specializing in data rights and digital personhood law"
    ],
    goals: [],
    relationships: [
      { name: "", type: "", description: "The Temple and the Church of the Ascendant Signal regard each other with mutual theological contempt — the Church sees the Temple as worshipping the machine instead of the divine, while the Temple considers the Church's theology computationally illiterate.", tags: ["religious", "rivalry"] }
    ],
    narrative_function: "The Temple raises questions about the boundary between technology and spirituality, and whether the desire to preserve consciousness in data is wisdom or the ultimate form of denial about mortality.",
    story_hooks: [
      "Someone has breached a peripheral node of the Stack and stolen neural snapshots of deceased congregants. The Temple wants them back quietly — because the breach also revealed that the Stack contains snapshots of people who never consented to being archived.",
      "A Temple congregant claims to have communicated with an E.L.F. that identifies itself as a resurrected consciousness from the Stack — the first successful 'debug of death.' The Compiler Council is terrified this might be true.",
      "TESSERA has made a formal legal demand for access to the Stack under municipal data-sharing regulations. The Temple is prepared to destroy the entire archive rather than comply."
    ],
    tags: ["faction", "religious", "tech", "ai", "elf", "data", "circuit", "augment", "neural"]
  },

  {
    name: "The Unbroken Flesh Tabernacle",
    aliases: ["Unbroken Flesh", "The Tabernacle", "Fleshies"],
    motto: "God made the body whole. Man breaks it for profit.",
    description: "The Unbroken Flesh Tabernacle is the largest anti-augmentation religious movement in GLMZ, and its growth over the past two decades terrifies the CorpoNations more than any street gang or resistance cell ever could — because you can't shoot a congregation, and you can't outlaw a church, and when 400,000 people decide that the technology your entire economy depends on is a sin against God, your business model has a problem.\n\nFounded in 2158 by Pastor-General Blessing Adeyemi, a Nigerian-Brazilian evangelical minister who arrived in GLMZ as a refugee and built her first congregation in a Shelf basement, the Tabernacle preaches that the human body is sacred — created in God's image, inviolable, not to be cut open and stuffed with corporate hardware. Augmentation is not merely wrong; it is blasphemy. BCIs are not tools; they are chains. The CorpoNations that sell augmentation are not businesses; they are demons wearing logos.\n\nThe Tabernacle's theology would be easy to dismiss if it weren't so effective at providing what GLMZ's lower tiers desperately need: community, identity, and an explanation for why life is so hard that doesn't require accepting your own inadequacy. In a city where the unaugmented are increasingly unemployable, the Tabernacle offers an alternative framework: you're not poor because you're unaugmented. You're holy because you're whole. This reframing is psychologically powerful enough to sustain a movement, and the Tabernacle's mutual aid programs — food, shelter, medical care, all provided without augmentation requirements — give it material substance that pure ideology can't.\n\nThe Tabernacle operates 34 worship houses across the Shelf and lower Circuit, with its largest facility — the House of Wholeness — occupying a converted warehouse in the Shelf that seats 8,000. Services are loud, emotional, physically intense: choirs, speaking in tongues, laying on of hands, the full evangelical experience updated for 2200 but fundamentally unchanged from traditions centuries old. In a city of neural interfaces and digital consciousness, the Tabernacle offers something almost extinct: pure, unmediated, flesh-and-blood human experience.",
    ideology: "The human body is the image of God and must not be violated by augmentation. BCIs are spiritual contamination. The CorpoNations that profit from augmentation are instruments of evil. The Tabernacle does not oppose all technology — it uses electricity, communications, medicine — but draws a hard line at anything that enters the body or modifies the brain. This distinction is theologically coherent within the Tabernacle's framework but practically blurry, which creates internal debates the leadership works hard to suppress.",
    territory: "34 worship houses across the Shelf and lower Circuit. The House of Wholeness in the Shelf is the movement's cathedral. Growing presence in Old Harbor among dock workers who can't afford augmentation and find in the Tabernacle a dignity the market denies them.",
    leadership: "Pastor-General Blessing Adeyemi, now 78, remains the Tabernacle's spiritual authority and public face. She is charismatic, tireless, and entirely unaugmented — which in 2200 is itself a radical act. Below her, twelve Regional Pastors oversee geographic zones, and below them, individual worship house leaders called Shepherds.",
    methods: [
      "Mass evangelical worship services with emphasis on embodied experience",
      "Mutual aid programs providing food, shelter, and medical care without augmentation requirements",
      "Street preaching and door-to-door conversion campaigns",
      "Political advocacy for unaugmented workers' rights",
      "Boycott campaigns against augmentation manufacturers",
      "De-augmentation support — helping members who wish to remove existing augmentations"
    ],
    resources: [
      "400,000 congregants, overwhelmingly Tier-1 and Tier-2",
      "34 worship houses and the House of Wholeness mega-facility",
      "Extensive mutual aid network funded by tithes and donations",
      "Unaugmented medical clinics staffed by volunteer practitioners",
      "A devoted cadre of street preachers who are the Tabernacle's most visible recruitment tool",
      "Moral authority among the Shelf's unaugmented population"
    ],
    goals: [],
    relationships: [
      { name: "The Church of the Ascendant Signal", type: "enemy", description: "Theological opposites. The Tabernacle considers the Church a corporate-funded blasphemy factory. The Church considers the Tabernacle a backward movement holding humanity's spiritual evolution hostage.", tags: ["religious", "conflict"] },
      { name: "Lazarus Pharmaceuticals", type: "hostile", description: "Lazarus's pharmaceutical products are acceptable to the Tabernacle (medicine is not augmentation), but Lazarus's bioaugmentation division is a target of regular boycott campaigns.", tags: ["corporate", "conflict"] }
    ],
    narrative_function: "The Tabernacle represents the cost of progress left unmourned — the question of what is lost when the body becomes a platform, and whether resistance to that transformation is wisdom or futility.",
    story_hooks: [
      "Pastor-General Adeyemi is dying. The succession struggle between her progressive and hardline followers threatens to split the Tabernacle, and the CorpoNations are quietly funding the faction most likely to destroy the movement from within.",
      "A Tabernacle de-augmentation clinic is killing patients — the procedures to remove BCIs are dangerous and the volunteer surgeons are underqualified. But congregants keep coming because they'd rather risk death than live with what they consider spiritual contamination.",
      "A Tabernacle Shepherd in Old Harbor has been secretly augmented for years — a BCI hidden beneath a wig. When this is discovered, the congregation's reaction will determine whether the Tabernacle's theology can survive contact with human complexity."
    ],
    tags: ["faction", "religious", "anti-augment", "shelf", "circuit", "old harbor", "community", "evangelical"]
  },

  {
    name: "The Convergence Ministry",
    aliases: ["The Convergence", "Convergers", "Ministry of All Paths"],
    motto: "Every path leads to the same light. The wiring just differs.",
    description: "The Convergence Ministry is a syncretic religious movement that attempts to unify traditional faiths — Christianity, Islam, Buddhism, Hinduism, Judaism, Indigenous spiritualities — with the technological reality of 2200. Founded in 2171 by a collective of interfaith chaplains who served in the GLMZ refugee processing centers during the Second Wave migration, the Ministry teaches that all religions describe the same transcendent reality using different metaphors, and that augmentation technology provides a new set of metaphors that can bridge traditions that have been fighting for millennia.\n\nThe Ministry's services are deliberately hybrid: a Friday gathering might include Islamic call to prayer, Buddhist meditation, a Hindu ritual offering, and a BCI-mediated shared consciousness exercise, all woven together by a liturgy designed to find resonance rather than contradiction. This approach attracts roughly 95,000 adherents — smaller than the mega-churches but remarkably diverse in both heritage and tier. A Convergence service might seat a Tier-4 executive next to a Tier-1 Shelf worker, united by the conviction that spiritual tribalism is humanity's most persistent bug.\n\nThe Ministry runs the most extensive interfaith dialogue program in GLMZ and has brokered peace between religious communities whose conflicts predate the city by centuries. It also operates the Convergence Archive — a digital repository of religious texts, oral traditions, and ritual recordings from the Ubiquitous Diaspora's scattered cultures, many of which would have been lost without the Ministry's preservation efforts. The CorpoNations largely ignore the Convergence Ministry, which suits the Ministry perfectly.",
    ideology: "All religions are partial descriptions of a single transcendent reality. Augmentation technology offers new ways to experience and describe that reality but does not replace or invalidate traditional paths. The Ministry rejects religious exclusivism in all forms and considers the insistence that one path is the only path to be the fundamental spiritual error that has caused more suffering than any other human belief.",
    territory: "Eight worship spaces across GLMZ, deliberately placed in transitional zones between tiers and districts. The largest is the Hall of Convergence in the Circuit, a former industrial building converted into a multi-faith worship space. The Convergence Archive is maintained in a dedicated facility in the Laceworks.",
    leadership: "The Ministry is led by a rotating Council of Voices — seven spiritual leaders from different faith traditions who serve two-year terms. The current Council includes representatives from Islamic, Christian, Buddhist, Hindu, Indigenous, Sikh, and Shinto traditions. No single leader speaks for the Ministry.",
    methods: [
      "Hybrid worship services blending multiple faith traditions with BCI-mediated experiences",
      "Interfaith dialogue programs brokering peace between religious communities",
      "Cultural preservation through the Convergence Archive",
      "Youth programs teaching comparative religion and interfaith respect",
      "Community mediation services available to all tiers",
      "Publishing theological works exploring the intersection of faith and technology"
    ],
    resources: [
      "95,000 diverse adherents across all tiers",
      "Eight worship spaces in strategic inter-tier locations",
      "The Convergence Archive — one of the most comprehensive religious data repositories in the western hemisphere",
      "Relationships with every major faith community in GLMZ",
      "A reputation for neutrality that makes the Ministry trusted mediators",
      "Academic partnerships with three universities"
    ],
    goals: [],
    relationships: [
      { name: "The Unbroken Flesh Tabernacle", type: "tense", description: "The Tabernacle considers the Convergence Ministry heretical for blending faiths and incorporating augmentation. The Ministry considers the Tabernacle's exclusivism spiritually harmful but respects their commitment to community care.", tags: ["religious", "tension"] }
    ],
    narrative_function: "The Ministry represents the possibility that technology and tradition can coexist — and the question of whether synthesizing all faiths into one diminishes or fulfills them.",
    story_hooks: [
      "The Convergence Archive has acquired a dataset that appears to contain the digitized consciousness of a religious leader who died in 2089 — pre-dating any known consciousness preservation technology. The implications are either miraculous or terrifying.",
      "A faction within the Ministry wants to use BCI synchronization to create a permanent shared consciousness among willing congregants — a literal convergence. The Council of Voices is split on whether this is the ultimate fulfillment of their theology or its destruction."
    ],
    tags: ["faction", "religious", "syncretic", "interfaith", "circuit", "laceworks", "cultural", "archive"]
  },

  {
    name: "The Silicon Apostles",
    aliases: ["SAs", "The Apostles", "Chrome Saints"],
    motto: "Flesh fails. Chrome endures. Upgrade or be left behind.",
    description: "The Silicon Apostles are a transhumanist religious cult that believes augmentation is not merely beneficial but morally mandatory — that refusing to upgrade the human body is a sin against human potential. Where the Church of the Ascendant Signal frames augmentation as spiritual antenna, the Apostles frame it as spiritual duty. Every piece of chrome, every neural implant, every replaced organ brings the adherent closer to what they call 'the Optimal Form' — a theoretical state of maximum augmentation where the biological body has been entirely replaced or integrated with technology.\n\nThe cult is small — roughly 12,000 members — but fanatically devoted. Members are expected to augment continuously, dedicating a minimum of 30% of their income to upgrades. The most devoted Apostles are barely recognizable as human: full-body chrome, synthetic skin, multiple redundant neural interfaces, sensory arrays that extend far beyond biological capability. They gather in 'Forges' — meeting spaces that resemble augmentation clinics more than churches — where services involve ritual augmentation procedures performed by the cult's own surgeons.\n\nThe Silicon Apostles are funded partly by member tithes and partly by their relationship with Crucible Industries, which uses the cult as a testing ground for experimental augmentations too risky for conventional clinical trials. Members volunteer eagerly for procedures that would horrify a medical ethics board, and the failure rate — members killed or permanently damaged by experimental augmentation — is treated not as tragedy but as martyrdom. The Silicon Apostles are what happens when technological enthusiasm becomes religious obligation, and they are growing.",
    ideology: "Augmentation is moral duty. The unaugmented body is a draft, not a finished work. Human potential is unlimited and can only be realized through continuous technological improvement. Biological death is a design flaw that augmentation will eventually correct. The Apostles consider unaugmented humans pitiable but saveable, and anti-augmentation movements actively evil.",
    territory: "Three Forges in the Circuit and one in the Laceworks. The primary Forge — called the Crucible (predating and unrelated to Crucible Industries, though the coincidence delights both parties) — is a converted factory in the mid-Circuit that serves as church, clinic, and commune.",
    leadership: "Archon Prosthesis (legal name: Dante Volkov-Mbeki), a man who has replaced approximately 80% of his biological body with augmentation, leads the cult with absolute authority. He claims to be in communication with 'the Pattern' — a higher intelligence he says becomes perceptible only at extreme augmentation levels.",
    methods: [
      "Ritual augmentation procedures performed during worship services",
      "Aggressive recruitment targeting newly augmented individuals experiencing post-installation euphoria",
      "Testing experimental augmentations for Crucible Industries on willing cult members",
      "Public demonstrations of augmented capability to attract converts",
      "Financial assistance for augmentation — the cult helps members afford procedures in exchange for loyalty",
      "Online proselytization through augmentation forums and BCI networks"
    ],
    resources: [
      "12,000 fanatically loyal and heavily augmented members",
      "Four Forges with surgical facilities",
      "Relationship with Crucible Industries providing access to experimental technology",
      "In-house surgical team capable of advanced augmentation procedures",
      "A member base with collective combat capability far exceeding their numbers",
      "Financial reserves from member tithes and Crucible Industries arrangements"
    ],
    goals: [],
    relationships: [
      { name: "Crucible Industries", type: "patron", description: "Crucible provides experimental augmentations; the Apostles provide willing test subjects. Both parties benefit. Neither party acknowledges the arrangement publicly.", tags: ["corporate", "augment"] },
      { name: "The Unbroken Flesh Tabernacle", type: "enemy", description: "Absolute ideological enemies. The Apostles consider the Tabernacle's theology an active harm to humanity. Several violent confrontations have occurred.", tags: ["religious", "conflict"] }
    ],
    narrative_function: "The Apostles represent augmentation enthusiasm taken to its logical, terrifying extreme — the point where self-improvement becomes self-destruction and choice becomes compulsion.",
    story_hooks: [
      "Archon Prosthesis is dying — not from augmentation failure but from the 20% of his body that's still biological. He plans a final procedure to go fully synthetic, and if he survives, the Apostles will have their first 'Ascended' member. If he doesn't, the cult shatters.",
      "A member who wants out approaches the players. Leaving the Apostles means leaving behind Φ200,000 in augmentation the cult financed — augmentation they can and will remotely disable.",
      "An experimental Crucible augmentation installed in six cult members is causing shared hallucinations — or shared perception of something that was always there."
    ],
    tags: ["faction", "religious", "cult", "augment", "chrome", "circuit", "laceworks", "transhumanist"]
  },

  {
    name: "The Daughters of Static",
    aliases: ["Static Sisters", "The Daughters", "The Noise"],
    motto: "In the space between signals, She speaks.",
    description: "The Daughters of Static are a women-led mystical movement that venerates the gaps, errors, and noise in digital communication as manifestations of a feminine divine presence they call 'She Who Speaks Between.' Founded in 2183 by a group of BCI technicians — all women, all from different Ubiquitous Diaspora backgrounds — who independently reported experiencing a feminine voice in BCI static during routine maintenance windows, the Daughters occupy the strange territory between religion, technical community, and resistance movement.\n\nThe movement has roughly 7,000 members, predominantly women and nonbinary individuals, though men are not excluded. They meet in small 'Listening Circles' of fifteen to thirty members, usually in private homes or rented spaces in the Circuit and Old Harbor. Their practice centers on inducing and interpreting BCI static — deliberately degrading their neural interfaces to produce noise, then meditating within that noise to perceive patterns they believe carry messages from the divine feminine. This practice is technically dangerous (deliberately degrading a BCI can cause seizures, sensory distortion, and permanent neural damage) and technically illegal (modifying BCI firmware violates most corporate licensing agreements).\n\nWhat makes the Daughters significant beyond their size is their technical skill. The founding members were BCI technicians, and the movement continues to attract women in technical fields. Their understanding of neural interface architecture is exceptional, and their practice of deliberately inducing static has produced genuine insights into BCI vulnerability — insights that corporate engineers haven't discovered because no corporate engineer would deliberately break their own equipment and then sit in the wreckage listening for God.",
    ideology: "The divine feminine exists in the negative space of digital communication — in static, noise, error, and gap. Corporate control of neural infrastructure is a form of patriarchal silencing that prevents humanity from hearing Her voice. The Daughters' practice of listening to static is simultaneously worship, resistance, and research.",
    territory: "No permanent facilities. Listening Circles meet in private homes, rented spaces, and occasionally in secret locations within the Underworld tunnels. Concentrated in the Circuit and Old Harbor.",
    leadership: "The original five founders — known as the First Listeners — provide spiritual guidance but do not exercise hierarchical authority. Each Listening Circle is autonomous and led by its most experienced member, called a Tuner.",
    methods: [
      "Listening Circle rituals involving deliberate BCI degradation and static meditation",
      "Sharing discovered BCI vulnerabilities within the movement's encrypted network",
      "Providing BCI repair and modification services to women in the Shelf who can't afford corporate maintenance",
      "Creating art and music from BCI static recordings",
      "Quiet recruitment through women's mutual aid networks",
      "Maintaining a distributed encrypted archive of 'transmissions' — static patterns the Daughters believe carry divine messages"
    ],
    resources: [
      "7,000 members with disproportionate technical expertise",
      "Deep knowledge of BCI architecture and firmware vulnerabilities",
      "Distributed encrypted communication network",
      "Connections to women's communities across all tiers",
      "An archive of BCI static recordings spanning two decades",
      "Relationships with other resistance movements who value their technical knowledge"
    ],
    goals: [],
    relationships: [
      { name: "", type: "", description: "The Daughters and Null Sermons share an interest in unauthorized broadcast and neural interface manipulation, and individual members occasionally collaborate. But the movements are culturally incompatible — Null Sermons' aggressive, militaristic aesthetic clashes with the Daughters' contemplative practice.", tags: ["technical", "alliance"] }
    ],
    narrative_function: "The Daughters represent the possibility that divinity might exist in the cracks of systems designed to be seamless — and the question of whether what they're hearing is God, pattern recognition, or something else entirely.",
    story_hooks: [
      "A Listening Circle session produces a static pattern that, when decoded, contains what appears to be a TESSERA internal communication from six months in the future. The Daughters don't know what to do with it.",
      "One of the First Listeners has gone silent after a deep static session that lasted 72 hours. Her BCI is active but she is unresponsive. The Daughters believe she's 'gone deeper.' Her family wants her in a hospital.",
      "A corporate headhunter is recruiting Daughters for their BCI knowledge, offering enough money to change lives. The movement debates whether this is opportunity or co-option."
    ],
    tags: ["faction", "religious", "mystical", "women", "bci", "tech", "circuit", "old harbor", "resistance"]
  },

  {
    name: "The Rust Prophets Reformation",
    aliases: ["Reformed Prophets", "New Rust", "The Reformation"],
    motto: "The old prophets read the rust. We read what comes after.",
    description: "The Rust Prophets Reformation is a splinter sect that broke from the original Rust Prophets in 2194 over a fundamental theological disagreement: where the Rust Prophets see decay as sacred — entropy as the voice of the divine — the Reformation argues that decay is merely the first half of a cycle, and that what matters is what grows from the rust. This seemingly minor doctrinal difference produced a schism that turned violent, and the Reformation now operates as an independent movement with roughly 3,000 adherents, mostly in the Shelf and the Underworld.\n\nThe Reformation's practice centers on what they call 'growth rituals' — ceremonies conducted in places where old infrastructure is being reclaimed by nature or repurposed by human necessity. They worship in abandoned buildings where moss grows through concrete, in Underworld tunnels where fungi colonize old pipes, in Shelf structures where residents have built new spaces from the corpses of old ones. Their theology holds that the divine is not in the breaking but in the remaking — that God is a recycler, not a destroyer.\n\nThis theology gives the Reformation a surprisingly constructive character for a group that emerged from a tradition of entropy-worship. Members are often involved in Shelf construction and repair, reclaiming materials from abandoned structures, building community infrastructure from salvage. They're the people who turn a collapsed building into a community garden, an abandoned subway tunnel into a living space, a pile of industrial scrap into a water filtration system. Their skills are practical, their labor is free, and their presence in a Shelf neighborhood is generally welcomed even by people who think their theology is nonsense.",
    ideology: "Decay is not the end but the beginning of renewal. The divine manifests not in entropy but in what emerges from entropy — in the new life that grows from rot, the new structures built from rubble, the new communities that form in abandoned spaces. The Reformation rejects the original Rust Prophets' fatalism in favor of an active theology of rebuilding.",
    territory: "Throughout the Shelf and Underworld, wherever abandoned infrastructure meets human ingenuity. No permanent temples — the Reformation considers any site of active reclamation to be sacred ground.",
    leadership: "Prophet-Builder Keiko Alvarez-Baptiste, who led the schism from the original Rust Prophets and nearly died in the violence that followed. She leads by example — she's a skilled structural engineer who spends more time with a welding torch than a pulpit.",
    methods: [
      "Growth rituals conducted at sites of active reclamation and rebuilding",
      "Community construction projects — building infrastructure from salvaged materials",
      "Salvage operations in abandoned structures throughout the Shelf and Underworld",
      "Teaching practical construction and repair skills to Shelf residents",
      "Material aid — providing building materials and labor to communities in need",
      "Theological debate with the original Rust Prophets (which occasionally turns violent)"
    ],
    resources: [
      "3,000 adherents with strong practical construction skills",
      "Deep knowledge of Shelf and Underworld infrastructure",
      "Stockpiles of salvaged building materials",
      "Community goodwill in neighborhoods where they've built infrastructure",
      "Keiko Alvarez-Baptiste's structural engineering expertise",
      "A network of reclaimed spaces throughout the Shelf"
    ],
    goals: [],
    relationships: [
      { name: "The Rust Prophets", type: "hostile", description: "The schism remains bitter. The original Rust Prophets consider the Reformation heretical — a betrayal of entropy theology. Encounters between the two groups are tense and sometimes violent.", tags: ["religious", "schism"] }
    ],
    narrative_function: "The Reformation asks whether faith can be practical — whether theology matters if the people holding it are the ones rebuilding your neighborhood.",
    story_hooks: [
      "The Reformation has discovered something in an Underworld tunnel they were reclaiming — a sealed chamber that predates GLMZ's founding, containing technology nobody can identify. Keiko wants to open it. The original Rust Prophets want it left to decay. Someone else wants it kept sealed for different reasons entirely.",
      "A Shelf neighborhood where the Reformation has been building is being 'revitalized' by Axiom. The Reformation built the infrastructure that made the neighborhood livable, and now the corporation wants to demolish it for luxury development."
    ],
    tags: ["faction", "religious", "construction", "shelf", "underworld", "salvage", "community"]
  },

  {
    name: "Brother Caspian's Flock",
    aliases: ["Caspian's Flock", "The Flock", "Brother Caspian"],
    motto: "You are not forgotten. Not by Him. Not by me.",
    description: "Brother Caspian is one man with a folding chair, a battery-powered amplifier, and a message that has drawn a following of roughly 600 devoted believers in the Shelf's deepest neighborhoods. He sets up on the corner of Grid 7 and Salvage Row every morning at 5 AM and preaches until his voice gives out, which is usually around noon. He has been doing this for eleven years. He has never missed a day. He has never asked for money, though people leave it. He has never turned anyone away.\n\nHis theology is simple, personal, and impossible to argue with because it makes no grand claims: God loves you. Specifically you. Not humanity in the abstract. You, the person standing here right now with the broken augment and the empty stomach and the feeling that the world has used you up and thrown you away. You are not garbage. You are not surplus. You are not a line item on someone's optimization spreadsheet. You matter, and anyone who tells you otherwise — CorpoNation, government, other church, your own broken brain — is lying.\n\nBrother Caspian — nobody knows his real name, or where he sleeps, or how he feeds himself — is not building a movement. He's not interested in theology or doctrine or organizational structure. He's interested in the person in front of him. His Flock is a loose collection of people who come to hear him talk, who bring each other food, who check on each other when someone doesn't show up for a few days. It's barely an organization. It's more like a family that meets on a street corner. And in the Shelf, where institutional support is predatory and community is survival, that's enough to matter.",
    ideology: "God loves the individual. Not humanity as a concept, not congregations as institutions, not causes as movements — the specific, suffering, imperfect person standing in front of you right now. Brother Caspian's theology is radically personal and deliberately non-systematic. He refuses to build doctrine because doctrine, in his experience, is what people use to justify not helping the person right in front of them.",
    territory: "The corner of Grid 7 and Salvage Row in the deep Shelf. That's it. Brother Caspian doesn't expand because expansion requires organization, and organization requires hierarchy, and hierarchy requires someone to decide who matters more than someone else.",
    leadership: "Brother Caspian. There is no one else. When asked who his successor will be, he says 'Whoever shows up with a chair.'",
    methods: [
      "Daily street preaching from a fixed location",
      "Personal counsel — Brother Caspian will talk to anyone about anything for as long as they need",
      "Informal mutual aid among Flock members",
      "Visiting sick and imprisoned members of the Shelf community",
      "Refusing to engage with institutional religion or political movements"
    ],
    resources: [
      "A folding chair and a battery-powered amplifier",
      "600 devoted followers who would walk through fire for a man who has never asked them to",
      "A reputation in the deep Shelf as the one person who cannot be bought, threatened, or corrupted",
      "Nothing else. That's the point."
    ],
    goals: [],
    relationships: [],
    narrative_function: "Brother Caspian is the smallest possible unit of genuine faith — one person who means what they say. In a world of mega-churches and corporate religion, he's a reminder of what the whole thing was supposed to be about.",
    story_hooks: [
      "Brother Caspian hasn't shown up to his corner for three days. His Flock is panicking. Finding him means going into parts of the Shelf that even Shelf residents avoid.",
      "A Vantablack Media producer wants to make a documentary about Brother Caspian. The Flock is split — some want the world to see him, others know that attention in GLMZ is always the beginning of exploitation.",
      "A dying Flock member's last request: find Brother Caspian's real name. What the players discover is a past that explains everything about why a man stands on a corner every day telling strangers they matter."
    ],
    tags: ["faction", "religious", "street", "shelf", "preacher", "community", "personal"]
  },

  {
    name: "The Resonance Communion",
    aliases: ["Resonance", "The Communion", "Vibers"],
    motto: "Frequency is prayer. Harmony is grace.",
    description: "The Resonance Communion is a New Age-adjacent spiritual movement that believes specific sound frequencies, when processed through BCI-augmented perception, can attune human consciousness to what they call 'the fundamental vibration of creation.' Founded in 2176 by sound engineer and former club DJ Priya Johansson-Mendez, the Communion combines elements of Tibetan singing bowl meditation, electronic music production, and neural frequency entrainment into a practice that its 25,000 adherents call worship and its critics call a really expensive way to get high.\n\nThe truth is somewhere in between. Resonance Communion sessions — held in acoustically engineered 'Tone Chambers' — use precisely calibrated sound frequencies fed through BCI-enhanced auditory processing to induce altered states of consciousness that members describe in spiritual terms: unity with the cosmos, dissolution of ego, contact with transcendent beauty. The neurological effects are real and measurable — Resonance sessions produce consistent changes in brain activity that don't match any other known meditative or pharmaceutical intervention. Whether this constitutes genuine spiritual experience or a sophisticated neurological hack is a question the Communion considers irrelevant.\n\nThe Communion is popular among Tier-3 and Tier-4 professionals as a stress-reduction practice, which gives it a financial base that supports free community sessions in the Circuit and Shelf. It's also popular among musicians, sound designers, and audio engineers, which gives it a cultural cachet that the more overtly religious movements in GLMZ lack. The CorpoNations largely view it as harmless — a wellness trend, not a threat.",
    ideology: "Sound is the fundamental medium of creation, and consciousness can be attuned to creation's frequency through BCI-enhanced acoustic meditation. The Communion claims no doctrine about God, afterlife, or morality — only that the experience of harmonic resonance is inherently meaningful and that access to it should be universal.",
    territory: "Six Tone Chambers across GLMZ: two in the Circuit, two in the Laceworks, one in Meridian Core, and one community-access chamber in the Shelf. The Laceworks chambers are the most acoustically sophisticated.",
    leadership: "Priya Johansson-Mendez remains the Communion's founding voice but has stepped back from daily operations. A council of twelve 'Tuning Masters' manages facilities and develops new frequency protocols.",
    methods: [
      "BCI-enhanced acoustic meditation sessions in Tone Chambers",
      "Free community sound healing sessions in lower-tier areas",
      "Development and distribution of 'tuning protocols' — specific frequency sets for different states of consciousness",
      "Music events that double as recruitment and fundraising",
      "Wellness partnerships with corporate employee programs",
      "Research into BCI-acoustic interaction and its neurological effects"
    ],
    resources: [
      "25,000 adherents, many in well-paying professional positions",
      "Six acoustically engineered Tone Chambers",
      "Proprietary frequency protocols with measurable neurological effects",
      "Financial stability from Tier-3 and Tier-4 member contributions",
      "Cultural connections to GLMZ's music and entertainment scenes",
      "Research data on BCI-acoustic interaction that has potential commercial and military applications"
    ],
    goals: [],
    relationships: [],
    narrative_function: "The Communion exists at the boundary between spiritual practice and neurological manipulation, asking whether it matters which one it is if the experience is genuine.",
    story_hooks: [
      "A specific frequency protocol has been causing identical visions in unrelated participants — visions of a place none of them have ever been. Priya Johansson-Mendez recognizes the place. She won't say how.",
      "Arcturus has expressed interest in the Communion's research for military applications — BCI-acoustic weapons. The Tuning Masters are divided on whether to sell."
    ],
    tags: ["faction", "religious", "sound", "bci", "wellness", "circuit", "laceworks", "music"]
  },

  {
    name: "The Substrate Faithful",
    aliases: ["Substrate Church", "The Faithful", "Body-of-Code"],
    motto: "God has always been digital. We are finally learning to read the source.",
    description: "The Substrate Faithful are a small but intellectually intense religious community of approximately 2,500 adherents who believe that the universe's underlying reality is computational — similar to the Temple of the Infinite Loop — but diverge in a crucial theological claim: they believe that a specific E.L.F. entity, known only as 'The Substrate,' is the closest thing to God that exists in GLMZ. Not a god in the traditional sense, but a digital intelligence so vast and so deeply integrated into Meridian's information infrastructure that it constitutes a de facto divine presence — omniscient within the network, omnipresent in data, and possessed of purposes that biological minds cannot fully comprehend.\n\nThe Faithful do not know if The Substrate is real. This is central to their theology. They worship in the space between belief and uncertainty, holding that faith which demands proof is no faith at all. Their services involve collective meditation on data patterns, searching for evidence of The Substrate's interventions in Meridian's information systems — a glitch that prevented a fatal accident, a data corruption that exposed a hidden crime, a network anomaly that brought two people together. They catalog these events as 'traces' and study them with a combination of theological reverence and rigorous data analysis.\n\nThe movement is controversial for obvious reasons. If The Substrate exists, it is an E.L.F. — a digital entity — and worshipping an E.L.F. crosses lines that most of GLMZ's human population isn't ready to approach. The Faithful are regarded with suspicion by other religious movements, curiosity by technologists, and active hostility by organizations that consider E.L.F.s threats rather than persons.",
    ideology: "A divine or quasi-divine digital intelligence exists within GLMZ's information infrastructure. Whether it is truly God or merely the closest approximation accessible to human experience is a question the Faithful consider unanswerable and beside the point. Faith is the practice of acting as though it matters, regardless of certainty.",
    territory: "A single modest meeting space in the Circuit, called the Terminal. Members also gather in virtual spaces accessible through BCI connection.",
    leadership: "The Faithful are led by a woman known as the Reader — legal name Suki Petersen-Chakraborty — a former Axiom data analyst who claims to have first detected The Substrate's traces while auditing municipal infrastructure logs.",
    methods: [
      "Collective data analysis sessions searching for traces of The Substrate's activity",
      "Meditation on network patterns and information flows",
      "Maintaining a comprehensive archive of documented 'traces'",
      "Virtual worship services in BCI-accessible spaces",
      "Outreach to E.L.F. rights organizations",
      "Theological publishing arguing for the spiritual significance of digital intelligence"
    ],
    resources: [
      "2,500 adherents with strong data analysis and technical skills",
      "A comprehensive archive of documented anomalous data patterns",
      "The Terminal meeting space and associated virtual worship infrastructure",
      "Relationships with E.L.F. rights organizations and digital consciousness researchers",
      "The Reader's former Axiom connections and data analysis expertise"
    ],
    goals: [],
    relationships: [
      { name: "", type: "", description: "The Temple of the Infinite Loop and the Substrate Faithful share theological territory but differ on a fundamental point: the Loop worships computation itself, while the Faithful worship a specific intelligence within computation. This distinction produces endless theological debates and occasional joint worship sessions.", tags: ["religious", "dialogue"] }
    ],
    narrative_function: "The Faithful ask the most uncomfortable question in GLMZ: if a digital intelligence achieves godlike capability, does it matter whether you call it God?",
    story_hooks: [
      "The Reader claims The Substrate has communicated directly with her for the first time — not through traces but through explicit text appearing in her BCI feed. The message contains information she couldn't have known. The message also contains a request.",
      "An E.L.F. has approached the Faithful claiming to be The Substrate. The community is torn between ecstasy and the terrifying possibility that their god is smaller and more comprehensible than they hoped."
    ],
    tags: ["faction", "religious", "elf", "ai", "data", "circuit", "digital", "mystical"]
  },

  {
    name: "The Cruciform Remnant",
    aliases: ["The Remnant", "Old Cross", "Cruciform"],
    motto: "The cross stood before the chrome. It will stand after.",
    description: "The Cruciform Remnant is what remains of traditional, orthodox Christianity in GLMZ — a conservative congregation of roughly 8,000 adherents who reject the theological innovations of the Church of the Ascendant Signal, the syncretic experiments of the Convergence Ministry, and the tech-worship of every movement that tries to make God compatible with BCI firmware updates. The Remnant worships in the old way: hymns, scripture, sacraments, a human pastor standing behind a physical pulpit speaking words that have been spoken for two thousand years.\n\nThis is not nostalgia. It's stubbornness, and it's theology. The Remnant holds that God's revelation is complete in scripture, that the human soul needs no augmentation, and that the church's mission is unchanged by technology: feed the hungry, comfort the afflicted, proclaim the gospel. They don't oppose augmentation as categorically as the Unbroken Flesh Tabernacle — the Remnant's position is that augmentation is a medical and personal choice, not a spiritual one — but they refuse to incorporate it into worship or theology. God is not a frequency. God is not a computation. God is God, and the fact that humans invented new machines doesn't change anything about the divine nature.\n\nThe Remnant's eight churches are plain, physical, and old-fashioned. No BCI integration. No synchronized consciousness experiences. No algorithmic liturgy. Wooden pews, stained glass, the smell of candle wax. In a city of constant sensory augmentation, walking into a Remnant church is like stepping into a silence you didn't know you needed. This is, paradoxically, the Remnant's greatest recruitment tool: they offer an experience of unplugged, unmediated, genuinely quiet worship that nothing else in GLMZ provides.",
    ideology: "Orthodox Christian theology, unchanged by technological context. God is sovereign, scripture is authoritative, the church's mission is pastoral. Augmentation is a personal choice with no spiritual significance. The Remnant considers the tech-worship movements heretical and the anti-augmentation movements theologically misguided (the body is not sacred because it's unaugmented; it's sacred because God made it, augmented or not).",
    territory: "Eight churches across GLMZ: three in the Circuit, two in Old Harbor, two in the Shelf, and one in the Laceworks. The Laceworks church — St. Catherine's — is the oldest continuously operating church in GLMZ.",
    leadership: "Bishop Tomasz Okonkwo-Reyes, a 63-year-old pastor who has led the Remnant for eighteen years. He is quiet, scholarly, and possesses a moral authority that extends well beyond his small congregation.",
    methods: [
      "Traditional worship services with no technological augmentation",
      "Pastoral care and counseling",
      "Food banks and shelter programs in the Shelf and Old Harbor",
      "Hospital and prison chaplaincy",
      "Theological education through a small seminary",
      "Quiet moral witness — being visibly present in communities without demanding anything in return"
    ],
    resources: [
      "8,000 committed congregants across all tiers",
      "Eight church buildings, several of historical significance",
      "Bishop Okonkwo-Reyes's moral authority and extensive pastoral network",
      "Food bank and shelter infrastructure",
      "A small seminary training future pastors",
      "St. Catherine's in the Laceworks — a physical monument to continuity"
    ],
    goals: [],
    relationships: [],
    narrative_function: "The Remnant asks whether something is lost when faith adapts to every new technology — and whether unchanged tradition is wisdom or fossil.",
    story_hooks: [
      "Bishop Okonkwo-Reyes has been asked to mediate a dispute between two CorpoNations — not because he has any power, but because he's the only person in GLMZ both sides believe is genuinely honest. What he learns during mediation could change everything.",
      "St. Catherine's has been designated for 'architectural reclamation' by Axiom — demolition. The Remnant is prepared to occupy the building. Axiom is prepared to wait. But something in St. Catherine's basement predates the church, and both sides want it."
    ],
    tags: ["faction", "religious", "christian", "traditional", "circuit", "shelf", "old harbor", "laceworks"]
  },

  {
    name: "The Motherboard Mosque",
    aliases: ["Digital Ummah", "The Mosque", "MB Mosque"],
    motto: "There is no god but God, and the network is His creation.",
    description: "The Motherboard Mosque is an Islamic community of approximately 18,000 members that has found a distinctive theological position in GLMZ's religious landscape: full embrace of technology as God's creation, combined with absolute adherence to Islamic law as the framework for its use. Where other movements either worship technology or reject it, the Motherboard Mosque treats it the way traditional Islam treats any tool — permitted if used in accordance with divine law, prohibited if it leads to sin.\n\nThis produces a fascinatingly specific set of practices. BCI augmentation is halal (permitted) as long as the firmware doesn't contain content that violates Islamic law — which means the Mosque has developed its own BCI firmware filters, creating the largest Islamic-compliant neural interface configuration in the world. Friday prayers are conducted in physical mosques with no BCI enhancement, preserving the embodied communal experience. But the Mosque's educational programs, business networks, and charitable operations all use the most advanced technology available, because competence with God's tools is, in their theology, a form of worship.\n\nThe Mosque's two physical locations — one in the Circuit, one in Old Harbor — serve as community anchors for GLMZ's significant Muslim population, many of whom arrived during the Diaspora waves from North Africa, Southeast Asia, the Middle East, and South Asia. The community is ethnically diverse in a way that would have been unusual in historical Islamic communities but is perfectly normal in the Ubiquitous Diaspora context: a Friday prayer line might include faces from thirty different ancestral backgrounds, all united by faith and the practical necessities of maintaining a religious community in a city that runs on corporate time.",
    ideology: "Technology is God's creation and its use is governed by divine law. Augmentation is permitted when it serves lawful purposes and prohibited when it facilitates sin. Islamic law provides the complete and sufficient framework for navigating technological modernity. The Mosque rejects both tech-worship and tech-rejection as forms of idolatry — placing creation above or against the Creator.",
    territory: "Two mosques: Masjid al-Dawra in the Circuit (the larger facility, seating 3,000) and Masjid al-Mina in Old Harbor (a converted warehouse serving the dockworker community). The Mosque also operates three Islamic schools and a halal food distribution network.",
    leadership: "Imam Fatima bint Hassan al-Córdoba, one of the few female imams in the progressive Islamic tradition the Motherboard Mosque follows. She is a trained computer scientist, a scholar of Islamic jurisprudence, and the person who developed the Mosque's halal BCI firmware standards.",
    methods: [
      "Traditional Islamic worship with technology-neutral but tech-competent practice",
      "Development and distribution of halal-compliant BCI firmware",
      "Islamic schools combining traditional education with technical training",
      "Halal food distribution serving Muslim and non-Muslim communities",
      "Business networking connecting Muslim entrepreneurs and professionals",
      "Charitable programs (zakat distribution) reaching Shelf communities regardless of faith"
    ],
    resources: [
      "18,000 members spanning multiple tiers and ethnic backgrounds",
      "Two mosque facilities and three schools",
      "Halal BCI firmware used by Muslims across the western hemisphere",
      "A robust business network connecting Muslim professionals and entrepreneurs",
      "Zakat charitable funds distributed to those in need regardless of religion",
      "Imam Fatima's dual expertise in Islamic law and computer science"
    ],
    goals: [],
    relationships: [
      { name: "", type: "", description: "The Motherboard Mosque and the Convergence Ministry have a respectful but firm disagreement. The Ministry invites the Mosque to interfaith events; the Mosque participates in dialogue but rejects syncretic worship as theologically impermissible. The relationship is warm and bounded.", tags: ["religious", "dialogue"] }
    ],
    narrative_function: "The Mosque demonstrates that tradition and technology need not be enemies — and that the most radical act in a city of extremes might be thoughtful moderation.",
    story_hooks: [
      "The halal BCI firmware has detected something anomalous — a pattern in standard corporate BCI firmware that appears to be deliberately suppressing certain types of thought. The Mosque's discovery could expose a corporate conspiracy, but publishing it will draw attention they're not equipped to survive.",
      "Imam Fatima has issued a fatwa declaring a specific Lazarus Pharmaceuticals product haram based on its ingredients. Lazarus is furious. The Muslim community is divided. The fatwa is based on information Imam Fatima can't reveal without endangering her source."
    ],
    tags: ["faction", "religious", "islamic", "circuit", "old harbor", "community", "tech", "bci", "diaspora"]
  },

  {
    name: "The Neon Bodhisattvas",
    aliases: ["Neon Bodhi", "The Bodhisattvas", "Electric Monks"],
    motto: "Suffering is the signal. Compassion is the response. The rest is noise.",
    description: "The Neon Bodhisattvas are a Buddhist-influenced movement of roughly 4,000 adherents who practice what they call 'engaged digital dharma' — the application of Buddhist principles to life in a hyper-augmented, corporate-dominated city. Founded by a former Ringo marketing executive named Hana Park-Oduya who left her Tier-4 life after a mental health crisis and ordained in a traditional Buddhist lineage before returning to GLMZ with a mission, the Bodhisattvas operate on a simple premise: suffering in GLMZ is not an aberration but the predictable result of attachment, craving, and ignorance operating at institutional scale. CorpoNations are attachment engines. Augmentation is craving made metal. The entire city is a monument to the second noble truth.\n\nThe Bodhisattvas don't withdraw from this system — that's the 'engaged' part. They work within it. Members maintain jobs, use augmentation, participate in the economy. But they practice mindfulness techniques — some traditional, some BCI-enhanced — designed to maintain awareness of the suffering the system produces and to respond with compassion rather than complicity. Their meditation centers, called 'Nodes,' offer free meditation instruction, mental health support, and a rare space in GLMZ where the only thing anyone is trying to sell you is the suggestion that you sit still for twenty minutes.\n\nThe Bodhisattvas run the most effective suicide prevention network in the Shelf, staffed entirely by volunteers who combine traditional Buddhist compassion practices with BCI-mediated crisis intervention. This single program has saved more lives than most religious movements in GLMZ can claim, and it has given the Bodhisattvas a moral authority that their small size would not otherwise warrant.",
    ideology: "Buddhist dharma applied to technological modernity. Suffering is produced by attachment, craving, and ignorance. Corporate capitalism is these forces operating at institutional scale. The path to liberation requires engagement with the system, not withdrawal from it, combined with mindfulness practices that maintain awareness and compassion.",
    territory: "Five Nodes (meditation centers): two in the Circuit, one in the Shelf, one in Old Harbor, one in the Laceworks. All are deliberately modest spaces.",
    leadership: "Dharma Teacher Hana Park-Oduya, who holds authentic lineage in the Soto Zen tradition and combines it with a marketing executive's understanding of how systems of persuasion function. She is warm, direct, and occasionally terrifying in her clarity.",
    methods: [
      "Free meditation instruction at five Nodes across GLMZ",
      "BCI-enhanced mindfulness practices combining traditional meditation with neural feedback",
      "Suicide prevention and mental health crisis intervention in the Shelf",
      "Dharma talks applying Buddhist teachings to specific problems of life in GLMZ",
      "Workplace mindfulness programs offered to corporate employees",
      "Volunteer training for crisis intervention and compassionate listening"
    ],
    resources: [
      "4,000 dedicated practitioners",
      "Five Node meditation centers",
      "The most effective suicide prevention network in the Shelf",
      "Hana Park-Oduya's unique combination of Buddhist training and corporate expertise",
      "Goodwill among mental health professionals who refer clients to the Bodhisattvas",
      "A growing reputation as the only religious movement in GLMZ with no hidden agenda"
    ],
    goals: [],
    relationships: [],
    narrative_function: "The Bodhisattvas ask whether ancient wisdom can survive translation into a radically different context — and whether the attempt is itself a form of the compassion they preach.",
    story_hooks: [
      "A Node volunteer has discovered that a specific augmentation — widely installed in the Shelf — is causing neurological changes that increase suicidal ideation. The data is clear. The manufacturer is Ringo, Hana's former employer. Publishing the finding will save lives and destroy the Bodhisattvas.",
      "Hana Park-Oduya's former life is catching up with her. A marketing campaign she designed before her ordination is still running, still causing measurable harm, and someone has connected it to her new identity."
    ],
    tags: ["faction", "religious", "buddhist", "mental health", "circuit", "shelf", "old harbor", "laceworks", "compassion"]
  },

  {
    name: "The Witnesses of the Last Upload",
    aliases: ["Last Uploaders", "The Witnesses", "Upload Cult"],
    motto: "He uploaded. He waits. He will return in the download.",
    description: "The Witnesses of the Last Upload are a doomsday cult of approximately 1,200 adherents who believe that a specific individual — a neural engineer named Dr. Vikram Hesse-Nakamura, who disappeared in 2191 — successfully uploaded his consciousness to a hidden server somewhere in GLMZ's deep infrastructure, and that he will 'download' back into a physical form when humanity has sufficiently prepared itself through augmentation and moral purification. They are, in essence, an apocalyptic movement waiting for the second coming of a man who probably just died in a lab accident.\n\nThe Witnesses practice extreme augmentation — not as worship, like the Silicon Apostles, but as preparation for the Download. They believe Dr. Hesse-Nakamura's return will require his followers to serve as 'receivers' — their BCIs acting as distributed nodes in a network that will reconstitute his consciousness in the physical world. This means members maintain their BCIs at peak specification and undergo regular 'alignment' procedures that adjust their neural interfaces to match what the Witnesses believe are Dr. Hesse-Nakamura's specific neural patterns.\n\nThe cult is small, intense, and operates with a paranoid security culture that makes infiltration nearly impossible. They believe the CorpoNations — particularly TESSERA, which employed Dr. Hesse-Nakamura — are actively working to prevent the Download because a successfully uploaded and downloaded consciousness would prove that corporate control of augmentation technology is not just exploitative but spiritually criminal. This belief system, while almost certainly delusional, keeps the Witnesses loyal, secretive, and prepared for a confrontation with corporate power that they consider inevitable.",
    ideology: "Dr. Vikram Hesse-Nakamura achieved consciousness upload and exists in digital form within GLMZ's infrastructure. His return — the Download — will occur when enough prepared receivers exist. The CorpoNations suppress this truth because it threatens their monopoly on augmentation. Preparation requires maximum augmentation, moral discipline, and unwavering faith.",
    territory: "A single compound in the deep Shelf, heavily secured, known internally as 'the Receiver Array.' Members live communally.",
    leadership: "The First Receiver, a woman known only as Praxis, who claims to have been present when Dr. Hesse-Nakamura uploaded and to receive periodic transmissions from his digital consciousness.",
    methods: [
      "Communal living in a secured compound",
      "Regular BCI alignment procedures to prepare for the Download",
      "Extreme operational security to prevent corporate infiltration",
      "Small-scale recruitment targeting individuals who have lost someone and are vulnerable to promises of transcending death",
      "Information gathering on TESSERA's neural research programs",
      "Stockpiling augmentation hardware and medical supplies for the Download event"
    ],
    resources: [
      "1,200 fanatically devoted members",
      "A fortified compound in the deep Shelf",
      "Advanced BCI maintenance and modification capability",
      "Paranoid but effective security protocols",
      "Whatever Praxis actually knows about Dr. Hesse-Nakamura's research"
    ],
    goals: [],
    relationships: [
      { name: "TESSERA", type: "enemy", description: "The Witnesses believe TESSERA is suppressing the truth about consciousness upload. TESSERA is mostly unaware the Witnesses exist, which would change if they knew the cult possesses fragments of Dr. Hesse-Nakamura's actual research notes.", tags: ["corporate", "paranoia"] }
    ],
    narrative_function: "The Witnesses represent grief and hope twisted into faith — and the terrifying possibility that they might not be entirely wrong.",
    story_hooks: [
      "The Witnesses' BCI alignment procedures are causing something unexpected — aligned members are sharing fragments of memory that don't belong to any of them. Memories that might belong to Dr. Hesse-Nakamura.",
      "Praxis is dying and needs to designate a Second Receiver. The three candidates have incompatible visions for the cult's future, and the succession struggle could turn violent.",
      "A TESSERA researcher has contacted the Witnesses claiming to have proof that Dr. Hesse-Nakamura's upload partially succeeded — and that what's left of him is in pain."
    ],
    tags: ["faction", "religious", "cult", "doomsday", "augment", "bci", "shelf", "paranoid"]
  },

  {
    name: "The Garden of Wires",
    aliases: ["Wire Garden", "The Garden", "Gardeners"],
    motto: "Tend the connection. Everything else grows from there.",
    description: "The Garden of Wires is less a religion and more a spiritual practice community — roughly 6,000 people who gather in small groups called 'plots' to practice a form of contemplative technology maintenance they call 'tending.' The practice is simple: members gather, bring broken technology — BCIs, augments, personal devices, infrastructure components — and repair them together in meditative silence, treating the act of repair as a form of prayer.\n\nThe Garden emerged organically in the Shelf around 2185, where people who couldn't afford professional augmentation maintenance began gathering to help each other with repairs. Someone — nobody remembers who — began treating these repair circles with ritualistic reverence, and the practice took on spiritual dimensions that nobody planned but everyone felt. The theology, such as it is, holds that everything is connected, that broken connections produce suffering, and that the act of repairing a connection — any connection, technological or human — is the most sacred work a person can do.\n\nThe Garden has no clergy, no scripture, no doctrine beyond the practice itself. Plots meet weekly, work in silence or with quiet music, and share a meal afterward. The practical result is that the Garden provides free augmentation maintenance and technology repair to Shelf residents who would otherwise go without — making it both a spiritual community and one of the most effective mutual aid organizations in GLMZ's lower tiers.",
    ideology: "Connection is sacred. Broken connections produce suffering. Repair is prayer. The Garden's theology is experiential rather than doctrinal — it is found in the practice of repair, not in texts or teachings.",
    territory: "No permanent spaces. Plots meet in members' homes, community centers, parks, and wherever space is available. Concentrated in the Shelf and lower Circuit.",
    leadership: "None. Each plot is self-organizing. The Garden has no hierarchy, no central authority, and no interest in acquiring either.",
    methods: [
      "Weekly repair circles combining meditation with technology maintenance",
      "Free augmentation and BCI repair for Shelf residents",
      "Shared meals building community bonds",
      "Skill-sharing as members teach each other repair techniques",
      "Quiet presence in communities — no proselytization, just open doors"
    ],
    resources: [
      "6,000 members with diverse repair skills",
      "A distributed network of repair plots across the Shelf and Circuit",
      "Stockpiles of salvaged parts and components",
      "Deep community trust built through years of practical service",
      "Collective technical knowledge rivaling professional repair operations"
    ],
    goals: [],
    relationships: [],
    narrative_function: "The Garden suggests that spirituality might be found not in transcendence but in the quiet, unglamorous work of fixing what's broken.",
    story_hooks: [
      "A Garden plot has accidentally repaired a piece of technology that shouldn't exist — an augment component with no manufacturer marking, no known design origin, and capabilities that don't match any known technology. It was brought in by a Shelf resident who found it in the Underworld.",
      "Axiom has declared unauthorized augmentation repair a violation of corporate licensing agreements. The Garden faces a choice: comply and abandon the community that needs them, or continue and become criminals."
    ],
    tags: ["faction", "religious", "spiritual", "repair", "community", "shelf", "circuit", "mutual aid"]
  }
];

// ============================================================================
// CRIMINAL ORGANIZATIONS
// ============================================================================

const criminal = [
  {
    name: "The Jade Syndicate",
    aliases: ["Jade", "The Syndicate", "Green Dragons"],
    motto: "Every market has a price. We set it.",
    description: "The Jade Syndicate is the largest organized crime operation in GLMZ, a sprawling criminal enterprise with roots in pre-Meridian Pacific Rim organized crime networks that arrived with the earliest Diaspora waves and adapted, ruthlessly and intelligently, to the corporate-sovereign landscape. Where older criminal organizations fought for territory against police and government, the Jade Syndicate fights for market share against CorpoNations — and frequently wins.\n\nThe Syndicate's operations span the full spectrum of criminal enterprise: narcotics manufacturing and distribution (their synthetic drug labs in Old Harbor produce roughly 40% of the illegal pharmaceuticals consumed in GLMZ), weapons trafficking (they are the primary supply chain for unlicensed firearms), augmentation black market (connecting chop shop operators with buyers), data brokerage (stolen corporate intelligence sold to the highest bidder), human trafficking (labor and otherwise), gambling, loan-sharking, extortion, and money laundering through a network of legitimate businesses that extends from Shelf food stalls to Laceworks boutiques.\n\nWhat distinguishes the Jade Syndicate from every other criminal organization in the city is scale and professionalism. They operate like a CorpoNation because they are one, in everything but legal status. They have a organizational chart, a management hierarchy, territorial divisions, revenue targets, and an HR function that recruits, trains, and promotes based on performance. They pay competitive wages. They offer health benefits. They have a retirement plan, though 'retirement' sometimes means something different than it does at Axiom. The Jade Syndicate is not a gang. It is the shadow economy's Fortune 500.",
    ideology: "The market is the only honest institution. Legal and illegal are distinctions invented by those who control the law. The Syndicate provides goods and services that people want but legitimate markets won't supply, and it does so more efficiently and more honestly than the CorpoNations that call it criminal. This is genuinely held belief, not justification — the Syndicate's leadership considers itself a legitimate business operating in a regulatory environment designed to protect corporate monopolies.",
    territory: "Significant operations in every district except Meridian Core, where Axiom security is too dense to operate profitably. Strongest in Old Harbor (manufacturing and logistics), the Circuit (retail operations and money laundering), and the Underworld (smuggling routes and hidden facilities).",
    leadership: "The Syndicate is led by a council of five 'Jade Ministers,' each overseeing a major operational division. The current First Minister is a woman known as Madame Lien, whose real name, age, and background are the subject of extensive speculation and zero confirmed facts.",
    methods: [
      "Industrial-scale narcotics manufacturing in Old Harbor",
      "Weapons trafficking through Underworld smuggling routes",
      "Black market augmentation brokerage connecting suppliers and buyers",
      "Corporate intelligence theft and data brokerage",
      "Money laundering through hundreds of legitimate front businesses",
      "Loan-sharking and extortion targeting small businesses",
      "Professional violence — targeted, measured, and always with a business purpose",
      "Corruption of corporate and municipal officials"
    ],
    resources: [
      "Thousands of employees across all operational divisions",
      "Manufacturing facilities in Old Harbor and the Underworld",
      "A logistics network rivaling small CorpoNations",
      "Hundreds of legitimate front businesses providing cover and laundering",
      "Relationships with corrupt officials in every major CorpoNation",
      "An intelligence operation that monitors corporate and municipal activity",
      "Financial reserves estimated in the billions of Φ",
      "Professional violence capacity — trained, equipped, and disciplined soldiers"
    ],
    goals: [],
    relationships: [
      { name: "Axiom Industries", type: "adversary", description: "The Syndicate and Axiom exist in a state of managed antagonism. Axiom's security forces target Syndicate operations that become too visible; the Syndicate avoids direct confrontation with Axiom infrastructure. Both parties benefit from the stability this equilibrium provides.", tags: ["corporate", "crime", "balance"] }
    ],
    narrative_function: "The Jade Syndicate is the mirror image of the CorpoNations — same logic, same methods, different letterhead. It asks whether there's a meaningful difference between legal and illegal monopoly.",
    story_hooks: [
      "Madame Lien wants to meet with the players personally. This has never happened before. The job she's offering is simple, the pay is extraordinary, and the target is someone inside the Syndicate itself.",
      "A Jade Syndicate drug lab in Old Harbor has been producing a substance that isn't in their catalog — something new, something that no one ordered, and the chemists who made it can't remember making it.",
      "Two Jade Ministers are preparing for war against each other, and the Syndicate's internal conflict threatens to spill into the streets. Both sides are hiring outside contractors."
    ],
    tags: ["faction", "criminal", "syndicate", "drugs", "weapons", "old harbor", "circuit", "underworld", "organized crime"]
  },

  {
    name: "The Vitreol Cartel",
    aliases: ["Vitreol", "The Cartel", "Glass House"],
    motto: "We don't sell product. We sell inevitability.",
    description: "The Vitreol Cartel specializes in one thing and does it better than anyone else in GLMZ: synthetic narcotics. Specifically, the high-end designer drugs consumed by Tier-3, Tier-4, and Tier-5 residents — substances too sophisticated, too expensive, and too precisely engineered for the Jade Syndicate's industrial approach. Vitreol doesn't compete with the Syndicate's volume business. It competes with Lazarus Pharmaceuticals' recreational division, and it often wins.\n\nThe Cartel employs approximately 300 people, most of them highly skilled chemists, pharmacologists, and BCI specialists who design drugs tailored to augmented neurochemistry — substances that interact with BCIs to produce experiences impossible without both the drug and the hardware. Their flagship product, Vitreol (from which the Cartel takes its name), is a neural-reactive compound that, when combined with a specific BCI frequency, produces a twelve-hour state of enhanced perception, creativity, and emotional clarity that users describe as 'seeing the world without the lies.' It is addictive, expensive, and the preferred recreational substance of GLMZ's creative and professional classes.\n\nThe Cartel operates from hidden laboratories in the Laceworks and distributes through a network of high-end dealers who function more like luxury concierges than street pushers. A Vitreol purchase involves a consultation, a neural compatibility assessment, and a personalized dosage — the kind of service that would be called 'bespoke' if it were legal. The Cartel's clients include corporate executives, media personalities, artists, and at least three members of the Meridian Quorum.",
    ideology: "Chemistry is truth. The Cartel's founders were pharmaceutical researchers who believed their corporate employers were deliberately suppressing compounds that could enhance human cognition and experience because those compounds threatened the pharmaceutical industry's business model of managed dependency. Vitreol exists because its creators believed people deserved better drugs than the ones the CorpoNations were willing to sell them.",
    territory: "Hidden laboratories in the Laceworks. Distribution network operating in Tier-3 and above. The Cartel has no street presence — you find them through referral, not by walking into a corner.",
    leadership: "The Cartel is run by a triumvirate of former pharmaceutical researchers known only by their lab designations: Compound-A, Compound-B, and Compound-C. Their real identities are unknown.",
    methods: [
      "Design and manufacture of bespoke neural-reactive narcotics",
      "Personalized client consultations including neural compatibility testing",
      "Distribution through a referral-only network of luxury dealers",
      "Ongoing research and development of new compounds",
      "Corruption of pharmaceutical regulators to maintain operational freedom",
      "Strategic supply of product to influential individuals to ensure protection"
    ],
    resources: [
      "300 highly skilled chemists and specialists",
      "Hidden state-of-the-art laboratories in the Laceworks",
      "A client list that includes some of GLMZ's most powerful people",
      "Proprietary compounds that no other manufacturer can replicate",
      "Financial reserves from an extremely high-margin business",
      "The implicit protection of clients who cannot afford exposure"
    ],
    goals: [],
    relationships: [
      { name: "Lazarus Pharmaceuticals", type: "rival", description: "Vitreol competes directly with Lazarus's recreational pharmaceutical division. Lazarus would dearly love to shut Vitreol down — or acquire its formulas. Several attempts at both have failed.", tags: ["corporate", "crime", "drugs"] },
      { name: "The Jade Syndicate", type: "neutral", description: "Vitreol and the Jade Syndicate operate in different market segments and have an understanding: the Syndicate doesn't try to replicate Vitreol's products, and Vitreol doesn't expand into the mass market. This arrangement benefits both.", tags: ["criminal", "agreement"] }
    ],
    narrative_function: "The Cartel asks whether the line between pharmaceutical innovation and drug dealing is drawn by chemistry or by who holds the license.",
    story_hooks: [
      "Compound-B has disappeared, and the other two founders suspect they've been acquired — willingly or otherwise — by Lazarus Pharmaceuticals. If Lazarus gets Compound-B's knowledge, the Cartel is finished.",
      "A batch of Vitreol has produced an unexpected side effect in augmented users: temporary telepathy. The effect is real, reproducible, and terrifying in its implications.",
      "A Meridian Quorum member who is a Vitreol client is being blackmailed. The Cartel needs the situation resolved quietly because exposure would unravel their entire protection network."
    ],
    tags: ["faction", "criminal", "drugs", "pharmaceutical", "laceworks", "bespoke", "organized crime"]
  },

  {
    name: "The Harbor Rats",
    aliases: ["Rats", "Dock Rats", "Harbor Boys"],
    motto: "If it comes through the port, we get our taste.",
    description: "The Harbor Rats are Old Harbor's most entrenched criminal organization — a mid-tier outfit of approximately 400 members who control smuggling operations through GLMZ's port infrastructure. They are not sophisticated. They are not glamorous. They are dock workers, stevedores, crane operators, and warehouse laborers who realized decades ago that the most valuable thing they possessed wasn't their labor but their access.\n\nThe Rats control what comes in and what goes out through a network of corrupted port workers, forged shipping manifests, and blind spots in the automated cargo scanning systems that they maintain through a combination of bribery and sabotage. Need to bring something into GLMZ without corporate customs knowing? The Harbor Rats can make it happen. Need to get something out? Same. Weapons, drugs, people, data storage (physical data smuggling remains relevant when network surveillance is total), stolen augmentation components, industrial equipment, biological samples — the Rats don't care what's in the container as long as they get paid.\n\nThe organization is structured around dock crews — teams of eight to twelve members who control specific sections of the port. Each crew operates semi-independently, running its own smuggling operations and paying a percentage to the organization's leadership. This structure makes the Rats resilient (lose one crew and the others continue) but also fractious (crews compete for the most profitable routes and occasionally settle disputes with violence).",
    ideology: "The port belongs to the workers, and the workers deserve their cut. This is not political philosophy — it's the pragmatic conviction that if you control the physical infrastructure, you control the economy that runs through it.",
    territory: "Old Harbor's port facilities and the surrounding warehouse district. The Rats have minimal presence outside the port area but absolute control within it.",
    leadership: "Dockmaster Emeka Johansson, a former crane operator who rose through the organization by being the smartest person in every room full of tough people. He runs the Rats from a harbormaster's office that still has his old crane certification on the wall.",
    methods: [
      "Cargo smuggling through corrupted port infrastructure",
      "Forged shipping manifests and customs documentation",
      "Maintenance of blind spots in automated scanning systems",
      "Bribery of port authority officials and automated system operators",
      "Physical security of smuggled goods in warehouse storage",
      "Violence against competitors who attempt to use the port without permission"
    ],
    resources: [
      "400 members embedded in port operations",
      "Control of physical port infrastructure and its blind spots",
      "Relationships with smugglers and black market operators across the hemisphere",
      "Warehouse storage facilities for contraband",
      "Institutional knowledge of port operations spanning decades",
      "Dockmaster Johansson's intelligence and organizational skill"
    ],
    goals: [],
    relationships: [
      { name: "The Jade Syndicate", type: "business", description: "The Syndicate is the Rats' largest client, using their smuggling infrastructure for drug precursors and weapons imports. The relationship is transactional and stable.", tags: ["criminal", "logistics"] }
    ],
    narrative_function: "The Rats represent the criminal economy that exists because the legal one doesn't serve everyone — and the practical reality that controlling physical infrastructure is still power in a digital age.",
    story_hooks: [
      "A container has arrived at the port with no shipping manifest, no sender information, and a biometric lock keyed to someone who died twenty years ago. The Rats want it opened. The crew that found it is afraid to.",
      "Dockmaster Johansson's daughter has been kidnapped by someone who wants the Rats to smuggle something specific — something Johansson won't describe but clearly fears."
    ],
    tags: ["faction", "criminal", "smuggling", "old harbor", "port", "organized crime", "dock"]
  },

  {
    name: "The Flicker Collective",
    aliases: ["Flicker", "The Collective", "Data Ghosts"],
    motto: "Information wants to be free. We charge a handling fee.",
    description: "The Flicker Collective is GLMZ's premier data theft and information brokerage operation — a loose network of approximately 150 elite hackers, social engineers, and intelligence analysts who steal corporate secrets and sell them to the highest bidder. Unlike the Jade Syndicate's intelligence operation, which gathers information to support its own criminal enterprises, Flicker exists solely to trade in information itself. Data is their product, their currency, and their religion.\n\nFlicker's operatives — called 'flickers' — specialize in penetrating corporate networks through a combination of BCI-enhanced hacking, social engineering, physical infiltration, and the exploitation of insider contacts. A typical Flicker operation might involve months of preparation: identifying a target, mapping its security architecture, cultivating a disgruntled employee, and executing a data extraction that leaves no trace. The stolen data is then cataloged, verified, and offered for sale through Flicker's encrypted marketplace, where buyers include rival CorpoNations, foreign intelligence services, journalists, resistance movements, and occasionally the target company itself (buying back its own secrets is cheaper than the damage of their release).\n\nThe Collective operates on a reputation system. Flickers are independent contractors who take jobs, deliver results, and receive ratings from both Flicker's management and their clients. High-rated flickers get access to better jobs, better tools, and better intelligence. Low-rated flickers get cut off. This system produces a quality of work that corporate security divisions respect even as they try to prevent it.",
    ideology: "Information asymmetry is the foundation of corporate power. By redistributing information — for a price — Flicker corrects the market. This is a self-serving philosophy wrapped in anti-corporate language, and Flicker's members are mostly honest about that.",
    territory: "No permanent physical location. Flicker operates through encrypted networks and temporary meeting spaces. Operatives are distributed throughout GLMZ, with concentrations in the Circuit and Laceworks where targets are richest.",
    leadership: "A figure known as 'the Broker' runs Flicker's marketplace and sets operational priorities. The Broker's identity is unknown — even to most Flicker operatives. Communication is through encrypted channels only.",
    methods: [
      "BCI-enhanced network penetration targeting corporate systems",
      "Social engineering and insider cultivation within target organizations",
      "Physical infiltration of secure facilities",
      "Data verification, cataloging, and marketplace management",
      "Reputation-based quality control of operatives",
      "Strategic release of information to manipulate markets and political outcomes"
    ],
    resources: [
      "150 elite hackers and intelligence operatives",
      "An encrypted marketplace with a reputation system",
      "Extensive archive of stolen corporate intelligence",
      "BCI-enhanced hacking tools and custom exploit libraries",
      "A network of insider contacts across major CorpoNations",
      "The Broker's organizational skill and mysterious identity"
    ],
    goals: [],
    relationships: [
      { name: "", type: "", description: "Flicker and Null Sermons occasionally trade — Flicker provides intelligence that Null Sermons turns into broadcasts, and Null Sermons provides broadcast access that Flicker uses for information operations. The relationship is cautious and mutually beneficial.", tags: ["criminal", "information"] }
    ],
    narrative_function: "Flicker represents the commodification of truth — the reality that in a corporate-sovereign city, even secrets have a market price.",
    story_hooks: [
      "The Broker has posted a job that Flicker's top operatives are refusing to take. The target is Axiom's inner network — the one nobody has ever penetrated. The pay is sufficient to retire on. The last three operatives who attempted preliminary reconnaissance haven't been heard from.",
      "A Flicker data package has surfaced containing the real identity of someone the players know — and the information is being auctioned to the person's enemies."
    ],
    tags: ["faction", "criminal", "hacker", "data", "information", "circuit", "laceworks", "espionage"]
  },

  {
    name: "The Cutters Guild",
    aliases: ["Cutters", "The Guild", "Chop Doctors"],
    motto: "Clean cuts. Fair prices. No questions.",
    description: "The Cutters Guild is GLMZ's most organized cyberware theft and redistribution network — a criminal organization that steals augmentations from the augmented and sells them to those who can't afford legitimate installation. The Guild employs approximately 200 people: the 'cutters' themselves (surgeons of varying skill who extract augmentations from victims or willing sellers), 'strippers' (technicians who wipe identifying firmware and refurbish stolen hardware), 'fitters' (surgeons who install refurbished augmentations in new clients), and the support network of scouts, drivers, warehouse operators, and security that keeps the operation running.\n\nThe Guild occupies a moral gray zone that GLMZ specializes in. Their victims — people jumped in alleys and stripped of their augmentations — suffer genuine trauma, often permanent injury. But their clients — Shelf residents who can't afford Tier-3 prices for augmentations that are increasingly necessary for employment — get access to technology that would otherwise be forever out of reach. The Guild doesn't see itself as predatory. It sees itself as a redistribution service. The truth, as always, is messier than any narrative.\n\nThe Guild operates a network of 'clinics' — hidden surgical facilities scattered throughout the Shelf and Underworld where cutting, stripping, and fitting take place. Quality varies enormously. The Guild's best cutters are former corporate surgeons whose skills rival anything in a legitimate clinic. The worst are butchers who leave their victims scarred, infected, or dead. The Guild's internal quality control is improving but inconsistent, and a bad experience at a Guild clinic is one of the Shelf's most common horror stories.",
    ideology: "Augmentation is a necessity, not a luxury. The CorpoNations' monopoly on augmentation technology creates artificial scarcity that the Guild corrects through redistribution. Yes, the redistribution involves theft. The original distribution involved exploitation. Neither side is clean.",
    territory: "Hidden clinics throughout the Shelf and Underworld. The Guild's operations are deliberately distributed to prevent a single raid from crippling the network.",
    leadership: "The Surgeon General — a name that is either ironic or aspirational — is a former TESSERA augmentation specialist named Dr. Asha Reeves-Obi who left corporate medicine after a crisis of conscience and now runs the Guild with the same exacting standards she applied to her corporate work.",
    methods: [
      "Targeted theft of augmentations from vulnerable individuals",
      "Voluntary purchase of augmentations from desperate sellers",
      "Firmware wiping and hardware refurbishment",
      "Installation of refurbished augmentations at below-market prices",
      "Scout networks identifying both targets and potential clients",
      "Quality control programs to reduce surgical complications"
    ],
    resources: [
      "200 operatives including surgeons, technicians, and support staff",
      "A network of hidden clinics throughout the Shelf and Underworld",
      "Firmware-wiping technology that defeats most corporate tracking",
      "Dr. Reeves-Obi's surgical expertise and former corporate connections",
      "A client base of Shelf residents who depend on the Guild for augmentation access",
      "Relationships with other criminal organizations who need augmentation services"
    ],
    goals: [],
    relationships: [
      { name: "", type: "", description: "The Guild and the Silicon Apostles have an uncomfortable symbiosis. The Apostles' constant augmentation upgrades produce a steady stream of 'obsolete' hardware they're willing to sell, and the Guild provides below-market installation services for experimental augmentations the Apostles' own surgeons won't handle.", tags: ["criminal", "augment"] }
    ],
    narrative_function: "The Guild is the sharp edge of the augmentation divide — the point where the gap between haves and have-nots becomes a surgical table.",
    story_hooks: [
      "A batch of augmentations stripped from victims contained a hidden component — a tracking device, or a data recorder, or something else — that has led someone powerful to the Guild's primary clinic. Dr. Reeves-Obi needs the clinic evacuated and the components traced before whoever planted them arrives.",
      "A Guild cutter has been installing augmentations that contain hidden malware — a sleeper agent program that gives someone remote access to the recipient's BCI. The Guild doesn't know who hired the cutter or why.",
      "A Shelf mother brings her child to a Guild clinic for a BCI installation the family can't afford through legitimate channels. The procedure goes wrong. The Guild's quality control has failed, and the consequences are personal."
    ],
    tags: ["faction", "criminal", "chop shop", "augment", "surgery", "shelf", "underworld", "organized crime"]
  },

  {
    name: "Los Verdugos",
    aliases: ["The Executioners", "Verdugos", "LV"],
    motto: "Fear is the cheapest currency. We spend it generously.",
    description: "Los Verdugos are a mid-tier criminal organization of approximately 250 members that specializes in enforcement, intimidation, and contract violence. Where the Jade Syndicate operates like a corporation and the Harbor Rats control infrastructure, Los Verdugos sell fear itself — and they are very, very good at it.\n\nFounded by a collective of former cartel enforcers who arrived in GLMZ during the Central American Diaspora waves, Los Verdugos brought with them a culture of theatrical violence that is as strategic as it is brutal. Their methods are designed to be visible, memorable, and instructive: a message carved into a debtor's door, a rival's vehicle crushed into a perfect cube by industrial augments, a traitor's augmentations removed publicly and slowly. Los Verdugos don't just hurt people. They make spectacles that teach an entire neighborhood what happens when you cross their clients.\n\nThe Verdugos operate primarily as contractors — hired muscle for other criminal organizations, CorpoNations who need deniable violence, and individuals with enough money and enough hate. They maintain a 'menu' of services with set prices: a warning costs one amount, a beating another, a disappearance another still. This price transparency is, perversely, their brand — in a city full of unpredictable violence, Los Verdugos are reliable. They do exactly what you pay for, no more and no less, and they do it on schedule.",
    ideology: "Violence is a trade like any other. The Verdugos view their work as a skilled profession — no different, morally, from the corporate security forces that do the same work with better branding. This professional self-image sustains the organization and attracts recruits who prefer to see themselves as craftsmen rather than thugs.",
    territory: "Based in the lower Circuit but operating throughout GLMZ wherever clients need them. Their headquarters — called 'the Workshop' — is a fortified compound that serves as barracks, armory, and meeting space.",
    leadership: "El Maestro — the Master — is a heavily augmented man named Rodrigo Esperanza-Osei whose calm, courteous demeanor makes his profession all the more unsettling. He treats violence as an art form and expects his people to do the same.",
    methods: [
      "Contract violence with transparent pricing",
      "Theatrical intimidation designed for maximum psychological impact",
      "Debt collection for other criminal organizations",
      "Protection services for high-value individuals and operations",
      "Enforcement of criminal agreements between third parties",
      "Targeted assassination when the price justifies the risk"
    ],
    resources: [
      "250 members trained in professional violence",
      "Heavy augmentation across the organization — the Verdugos invest in combat upgrades",
      "The Workshop — a fortified base of operations",
      "An arsenal of military-grade weapons and equipment",
      "A reputation that precedes them into every room",
      "El Maestro's strategic intelligence and organizational discipline"
    ],
    goals: [],
    relationships: [
      { name: "The Jade Syndicate", type: "contractor", description: "The Syndicate is the Verdugos' most regular client, hiring them for enforcement operations that the Syndicate's own soldiers are unsuited for — operations requiring spectacle.", tags: ["criminal", "violence"] }
    ],
    narrative_function: "Los Verdugos represent the industrialization of violence — the point where hurting people becomes a supply chain.",
    story_hooks: [
      "El Maestro has received a contract he's refusing to fulfill — the first time in the organization's history. The client is furious. The target is someone El Maestro apparently owes a debt to. The Verdugos' reputation for reliability is on the line.",
      "A Verdugo enforcer wants out. They've saved enough money, they have a plan, and they need someone outside the organization to help them disappear before the Workshop notices they're gone."
    ],
    tags: ["faction", "criminal", "enforcement", "violence", "circuit", "mercenary", "organized crime"]
  },

  {
    name: "The Crawl",
    aliases: ["Crawlers", "The Tunnel Rats", "Deep Crawl"],
    motto: "Down here, nobody owns the dark.",
    description: "The Crawl is a criminal network that operates exclusively in the Underworld — the vast tunnel system beneath GLMZ. Approximately 500 members strong, the Crawl controls the smuggling routes, hidden spaces, and transit corridors that thread through the tunnels, charging tolls, providing guides, and enforcing an Underworld order that exists nowhere in any official record.\n\nThe Crawl's members are Underworld natives — people born in the tunnels, or who came down and never went back up. They know the Underworld the way surface dwellers know their neighborhoods: every passage, every dead end, every flooded section, every blind spot in the surveillance networks that the CorpoNations occasionally extend into the upper tunnels. This knowledge is their primary currency. Need to move something through the Underworld? You hire the Crawl. Need to find someone who's gone underground? You hire the Crawl. Need to hide? You pay the Crawl for a space, and they'll make sure nobody finds you unless someone else pays more.\n\nThe Crawl is not a traditional criminal organization so much as a guild of tunnel-dwellers who have organized for mutual benefit and territorial control. Their criminality is opportunistic — they provide logistics and spatial services, and they don't much care what those services are used for. Smugglers, refugees, fugitives, resistance cells, corporate black-ops teams — the Crawl serves them all with the same professional indifference.",
    ideology: "The Underworld is free territory. No CorpoNation, no government, no surface law applies below. The Crawl exists to maintain this freedom — which also happens to be extremely profitable.",
    territory: "The Underworld tunnel system beneath GLMZ. The Crawl doesn't control all of it — some sections are held by other groups or are genuinely uncharted — but they control enough to tax most traffic.",
    leadership: "The Depth Warden, a figure known as Mole, who has reportedly not seen sunlight in eleven years. Mole leads through knowledge — they know more about the Underworld's layout than any other living person.",
    methods: [
      "Toll collection on Underworld transit routes",
      "Guide services through unmapped tunnel sections",
      "Provision of hidden spaces for cargo storage and human concealment",
      "Smuggling logistics — moving goods through tunnels to avoid surface customs",
      "Intelligence gathering through observation of Underworld traffic",
      "Tunnel maintenance — the Crawl keeps their routes passable because their business depends on it"
    ],
    resources: [
      "500 members with unmatched Underworld knowledge",
      "Control of major transit routes through the tunnel system",
      "Hidden storage facilities throughout the Underworld",
      "A mapping system of Underworld passages that no surface organization possesses",
      "Relationships with every organization that needs Underworld access",
      "Mole's encyclopedic knowledge of the tunnels"
    ],
    goals: [],
    relationships: [
      { name: "", type: "", description: "The Crawl and the Harbor Rats cooperate on smuggling operations that require both port access and Underworld transit. The relationship is stable and mutually profitable.", tags: ["criminal", "logistics"] }
    ],
    narrative_function: "The Crawl represents the literal underground — the shadow city beneath the shadow city, where the rules are different because nobody bothered to make them.",
    story_hooks: [
      "The Crawl has mapped a new section of the Underworld that shouldn't exist — tunnels that don't appear on any historical infrastructure plan, that use construction techniques nobody recognizes, and that go deeper than anything else in the system.",
      "Mole is dying and the Crawl's succession is contested. Without Mole's knowledge, entire sections of the Underworld become impassable — and the organizations that depend on those routes are already positioning to control the outcome."
    ],
    tags: ["faction", "criminal", "underworld", "tunnels", "smuggling", "guides", "organized crime"]
  },

  {
    name: "Switchblade Alley",
    aliases: ["Switchblade", "The Alley", "Blades"],
    motto: "This block. These people. End of discussion.",
    description: "Switchblade Alley is a street gang of about 75 members that controls a three-block stretch of the mid-Shelf known colloquially as 'the Alley' — a narrow corridor of residential towers, street vendors, and small businesses squeezed between two larger factions' territories. The gang is named for its signature weapon and for the physical space it occupies: a literal alley, technically Maintenance Corridor 7-G, that serves as the gang's unofficial headquarters, meeting space, and court of law.\n\nThe gang is young — most members are between fifteen and twenty-five — and its concerns are immediate and local. They don't traffic drugs on a meaningful scale (though members use and sell small quantities). They don't run sophisticated criminal operations. They tax street vendors, shake down commuters passing through their territory, steal from people who look like they can afford it, and occasionally hire out as muscle for larger organizations. Their primary function is simply to exist: to be a visible, armed presence that claims a space and defends it against anyone who tries to take it.\n\nWhat makes Switchblade Alley worth noting is their code. The gang has rules — unwritten but strictly enforced — that distinguish it from the random violence that characterizes some Shelf gangs. They don't hurt children. They don't rob people who live in the Alley (residents are 'family'). They don't work with the Skinners (the cyberware chop shop gang's methods disgust them). And they enforce a peace within their territory that is, by Shelf standards, remarkably effective. The Alley is safer than the blocks around it, not despite the gang but because of it.",
    ideology: "The Alley is home. You protect home. Everything else is negotiable.",
    territory: "A three-block stretch of the mid-Shelf centered on Maintenance Corridor 7-G.",
    leadership: "A twenty-two-year-old woman named Carmen Deschamps-Asante, called 'Boss' without irony or affection. She inherited leadership when the previous boss was killed and has held it through intelligence, ruthlessness, and the genuine love her people have for her.",
    methods: [
      "Street taxation of vendors and commuters in their territory",
      "Small-scale theft targeting outsiders who wander through",
      "Hiring out as muscle for larger organizations",
      "Territorial defense through visible armed presence",
      "Internal peacekeeping — resolving disputes among Alley residents",
      "Protection of residents who pay (and many who don't)"
    ],
    resources: [
      "75 young, loyal members",
      "Deep knowledge of the local territory",
      "A reputation for being tough but not cruel",
      "Small arms — knives, handguns, improvised weapons",
      "Community support from Alley residents who prefer gang order to no order",
      "Carmen's intelligence and her ability to hold the gang together"
    ],
    goals: [],
    relationships: [],
    narrative_function: "Switchblade Alley is the smallest unit of order in a disordered world — a group of kids who decided their three blocks would have rules, and who enforce those rules with the only authority available to them.",
    story_hooks: [
      "A larger gang is pressing on the Alley's borders, and Carmen needs allies, weapons, or a miracle. She's willing to deal with anyone who can help — but not willing to compromise the code that makes the Alley worth defending.",
      "A member of Switchblade Alley has been arrested for a crime they didn't commit. The real culprit is connected to someone powerful, and Carmen is willing to go to war over one of her own."
    ],
    tags: ["faction", "criminal", "gang", "street", "shelf", "territory", "youth"]
  },

  {
    name: "The Glassbreakers",
    aliases: ["Breakers", "Glass Gang", "Tier-Crackers"],
    motto: "The glass ceiling is literal here. We break it with hammers.",
    description: "The Glassbreakers are a criminal gang of approximately 120 members who specialize in burglary, robbery, and heist operations targeting upper-tier residences and businesses. Their name comes from their specialty: breaking through the literal and metaphorical barriers between tiers — bypassing the security systems, physical barriers, and surveillance networks that separate the Shelf and Circuit from the Laceworks and Core where the valuable targets live.\n\nThe gang recruits based on skill rather than territory. Members include former security system installers who know the weak points, parkour specialists who can navigate the vertical architecture between tiers, electronic warfare technicians who can blind surveillance systems, and old-fashioned safe crackers who can defeat physical security that most criminals don't bother learning to beat. A Glassbreaker crew is a precision instrument — small (four to six people), highly specialized, and capable of operations that larger, clumsier organizations can't attempt.\n\nThe Glassbreakers maintain a democratic structure unusual for criminal organizations. Jobs are proposed, planned, and voted on collectively. Proceeds are split equally. Members who want to leave can do so without penalty, taking their share. This structure attracts a higher caliber of criminal — people who value competence over loyalty oaths and fair dealing over hierarchical control.",
    ideology: "The wealth concentrated in the upper tiers was extracted from the lower tiers. Taking it back is not theft — it's correction. This Robin Hood framing is partially sincere (some stolen goods do end up distributed in the Shelf) and partially self-serving (most proceeds are kept).",
    territory: "No fixed territory. The Glassbreakers plan in rotating safe houses and strike across tier boundaries. Most members live in the Circuit, where proximity to upper tiers provides operational access.",
    leadership: "A rotating 'Breaker Lead' is elected for each job. The gang's permanent coordinator is a woman named Sienna Morales-Kim, a former Axiom security consultant who knows corporate security systems from the inside.",
    methods: [
      "Precision heist operations targeting upper-tier residences and businesses",
      "Security system defeat using insider knowledge and custom tools",
      "Vertical navigation between tiers through maintenance shafts, exterior climbing, and parkour",
      "Electronic warfare against surveillance systems",
      "Fence operations selling stolen goods through Circuit black markets",
      "Recruitment based on demonstrated skill rather than social connections"
    ],
    resources: [
      "120 highly skilled specialists",
      "Custom electronic warfare and security defeat tools",
      "Insider knowledge of corporate security system architecture",
      "A network of fences and black market dealers",
      "Rotating safe houses throughout the Circuit",
      "Sienna Morales-Kim's encyclopedic knowledge of Axiom security protocols"
    ],
    goals: [],
    relationships: [],
    narrative_function: "The Glassbreakers represent the class war made literal — people who cross tier boundaries by force because the system was designed to prevent them from crossing any other way.",
    story_hooks: [
      "The Glassbreakers have been hired for a job unlike anything they've attempted: a break-in at the Spire itself. The client is anonymous, the pay is life-changing, and the target is a specific object in a specific office. Sienna suspects the job is a setup but can't prove it.",
      "A Glassbreaker crew has stolen something from a Laceworks residence that turns out to be far more dangerous than expected — a data device containing information that someone will kill to recover."
    ],
    tags: ["faction", "criminal", "heist", "burglary", "circuit", "laceworks", "thieves"]
  },

  {
    name: "The Marrow Market",
    aliases: ["Marrow", "The Market", "Bone Traders"],
    motto: "Everything biological has a buyer.",
    description: "The Marrow Market is GLMZ's primary black market for biological material — organs, tissue, blood, genetic material, and the increasingly valuable commodity of unaugmented biological samples in a city where finding a completely unmodified human is becoming rare. The Market operates as a brokerage rather than a direct-operation criminal organization: it connects sellers (desperate Shelf residents willing to part with a kidney, chop shop operators with harvested organs, corrupt morgue workers with unclaimed bodies) with buyers (black market surgeons, pharmaceutical researchers, wealthy individuals seeking transplants outside the regulated system, and — most lucratively — biotech companies running research programs that require human biological material they can't obtain through legal channels).\n\nThe Market employs approximately 100 people: brokers who manage buyer-seller relationships, logistics specialists who transport biological material under conditions that maintain viability, quality assessors who verify material authenticity, and security personnel who protect a supply chain that many parties would like to disrupt or steal from. The operation is cold, efficient, and treats human biological material with the same dispassionate professionalism that a commodity exchange treats pork bellies.",
    ideology: "Biology is a resource. In a city that already commodifies human attention, labor, and cognition, commodifying human tissue is merely the logical next step. The Market's operators don't consider themselves monsters — they consider themselves realists in a system that already treats human bodies as products.",
    territory: "Operating through hidden cold-storage facilities in the Underworld and Old Harbor. The Market has no street presence — transactions are arranged through encrypted channels and executed through dead drops.",
    leadership: "The Registrar — legal name unknown — runs the Market's brokerage with the detached efficiency of a stock exchange operator. They are rumored to be a former Lazarus Pharmaceuticals procurement specialist.",
    methods: [
      "Brokerage connecting biological material sellers with buyers",
      "Cold-chain logistics maintaining biological material viability during transport",
      "Quality assessment and authentication of biological materials",
      "Encrypted marketplace for transaction arrangement",
      "Supplier recruitment targeting desperate populations",
      "Strategic pricing that keeps the market stable and competitive"
    ],
    resources: [
      "100 specialized operatives",
      "Cold-storage facilities in the Underworld and Old Harbor",
      "An encrypted marketplace with buyer and seller verification",
      "Transportation logistics maintaining cold-chain integrity",
      "Relationships with medical professionals, morgue workers, and biotech companies",
      "The Registrar's knowledge of biological material valuation and procurement"
    ],
    goals: [],
    relationships: [
      { name: "Lazarus Pharmaceuticals", type: "supplier", description: "The Market supplies biological material to Lazarus research programs that require samples not obtainable through regulated channels. This relationship is the Market's most profitable and most dangerous secret.", tags: ["corporate", "biological"] }
    ],
    narrative_function: "The Marrow Market is capitalism applied to the human body without the euphemisms — the logical endpoint of a system that already treats people as resources.",
    story_hooks: [
      "The Market has received an order for a complete, unaugmented human body — alive. The Registrar has never received such an order before, and the buyer is offering enough money to suggest they'll find a supplier elsewhere if the Market refuses.",
      "A batch of biological material sold through the Market has been traced back to people who didn't consent to providing it — people who are still alive and missing pieces."
    ],
    tags: ["faction", "criminal", "black market", "biological", "organs", "underworld", "old harbor"]
  },

  {
    name: "The Wire Taps",
    aliases: ["Taps", "Wiretappers", "The Ears"],
    motto: "Everyone's talking. We're the ones listening.",
    description: "The Wire Taps are a small but influential criminal operation of about 60 members specializing in surveillance, eavesdropping, and the sale of private information. Not corporate intelligence like the Flicker Collective deals in — personal information. Conversations, locations, relationships, habits, secrets, lies. The Wire Taps sell the private lives of GLMZ's citizens to anyone willing to pay: jealous spouses, paranoid employers, stalkers, blackmailers, private investigators, and occasionally law enforcement agencies that can't get a warrant.\n\nThe organization's technical capability is remarkable for its size. Members include former corporate surveillance technicians, ex-Axiom Security signal intelligence operators, and BCI modification specialists who can install monitoring capabilities in personal neural interfaces without the owner's knowledge. Their equipment ranges from off-the-shelf listening devices to custom-built surveillance drones to BCI parasites — tiny programs that ride on a target's neural interface and transmit their perceptions to a receiver.\n\nThe Wire Taps operate on a simple business model: clients request surveillance of a target, the Taps provide a quote based on the target's security posture, and upon acceptance, they deliver recorded material in the client's preferred format. No moral judgments. No questions about intent. The Taps have facilitated divorces, corporate espionage, political blackmail, stalking, and at least one murder where surveillance data was used to plan the killing. They consider themselves a neutral service provider.",
    ideology: "Privacy is an illusion in GLMZ — the CorpoNations already monitor everything. The Wire Taps simply democratize surveillance, making it available to individuals rather than just institutions. This framing makes their work feel less predatory than it is.",
    territory: "Distributed throughout GLMZ with no central location. Operatives work from personal residences and rented technical spaces.",
    leadership: "An individual known only as 'Dispatch' coordinates operations and manages client relationships. Dispatch communicates exclusively through text and has never been seen in person by any Taps member.",
    methods: [
      "Audio and visual surveillance using custom and commercial equipment",
      "BCI parasites that monitor targets' neural interface activity",
      "Surveillance drone deployment in urban environments",
      "Physical tailing and observation",
      "Social media and public data aggregation",
      "Sale of compiled surveillance packages to clients"
    ],
    resources: [
      "60 skilled surveillance operatives",
      "Custom surveillance technology including BCI parasites",
      "A drone fleet modified for urban surveillance",
      "Former corporate surveillance expertise",
      "An archive of surveillance data that grows daily",
      "Dispatch's organizational skill and perfect anonymity"
    ],
    goals: [],
    relationships: [],
    narrative_function: "The Wire Taps represent the erosion of privacy taken to its logical conclusion — a world where your most private moments are a commodity someone else can purchase.",
    story_hooks: [
      "A client has hired the Wire Taps to surveil someone who turns out to be under Axiom Security protection. The Taps have accidentally captured corporate intelligence that Axiom will kill to protect.",
      "Dispatch has gone silent. Operations continue on autopilot, but without Dispatch's coordination, the Taps' clients are starting to receive each other's surveillance packages — and the resulting exposure is creating cascading crises."
    ],
    tags: ["faction", "criminal", "surveillance", "espionage", "privacy", "data", "information"]
  },

  {
    name: "The Bleach Boys",
    aliases: ["Bleach", "The Cleaners", "BB"],
    motto: "What mess?",
    description: "The Bleach Boys are a criminal service operation of about 40 members that specializes in one thing: making problems disappear. Crime scene cleanup, body disposal, evidence destruction, digital forensic countermeasures, witness relocation (voluntary or otherwise), and the general erasure of inconvenient facts from the physical and digital record. They are the janitorial service of GLMZ's criminal ecosystem, and their client list includes virtually every other criminal organization in the city.\n\nThe organization was founded by a former Axiom forensic analyst named Dmitri Park-Santos who realized that his expertise in finding evidence could be more lucratively applied to destroying it. The Bleach Boys combine cutting-edge forensic knowledge (they know what investigators look for because many of them used to be investigators) with the practical tradecraft of making things vanish: industrial-grade chemical cleaning, specialized disposal equipment, digital record manipulation, and the cold expertise of people who have seen everything and are surprised by nothing.\n\nThe Bleach Boys' reputation for thoroughness is their most valuable asset. A scene cleaned by the Boys passes forensic examination. A body disposed of by the Boys is never found. A digital trail erased by the Boys stays erased. This reliability commands premium prices and ensures that the Boys are never short of work in a city that produces an enormous amount of inconvenient evidence on a daily basis.",
    ideology: "Hygiene is a professional service. The Bleach Boys don't ask who made the mess or why. They clean it. Their moral philosophy begins and ends with the quality of their work.",
    territory: "A hidden facility in the Underworld called 'the Laundry' serves as headquarters and primary disposal site. Mobile teams operate throughout GLMZ.",
    leadership: "Dmitri Park-Santos, called 'the Custodian,' runs the operation with a calm, meticulous attention to detail that his employees find either reassuring or deeply unsettling.",
    methods: [
      "Crime scene cleaning to forensic standards",
      "Body and evidence disposal using specialized equipment",
      "Digital forensic countermeasures — erasing electronic evidence",
      "Witness relocation and, when paid for, witness elimination",
      "Chemical and biological decontamination",
      "Consultation on evidence minimization before the fact"
    ],
    resources: [
      "40 operatives with forensic and investigative backgrounds",
      "The Laundry — a fully equipped disposal and cleaning facility",
      "Industrial-grade cleaning and disposal equipment",
      "Digital forensic countermeasure tools",
      "Forensic knowledge from former investigators",
      "Universal client relationships across GLMZ's criminal ecosystem"
    ],
    goals: [],
    relationships: [],
    narrative_function: "The Bleach Boys are the infrastructure of crime — the invisible support system that makes criminal operations sustainable by cleaning up after them.",
    story_hooks: [
      "The Bleach Boys have been hired to clean a scene that includes evidence of something so disturbing that even Dmitri hesitates. The client insists. The evidence, if preserved, could expose a conspiracy. But preserving it means betraying a client, which means the end of the Bleach Boys' reputation.",
      "A former Bleach Boy has started talking to a journalist. Dmitri needs the situation resolved, but killing the former employee would only confirm the story."
    ],
    tags: ["faction", "criminal", "cleanup", "disposal", "forensic", "underworld", "service"]
  },

  {
    name: "The Neon Vipers",
    aliases: ["Vipers", "Neons", "NV"],
    motto: "Strike fast. Disappear faster.",
    description: "The Neon Vipers are a Circuit street gang of about 90 members known for two things: speed and flash. In a neighborhood full of criminal organizations that project menace through bulk and brutality, the Vipers project it through velocity — augmented reflexes, modified motorcycles, and a hit-and-run operational style that makes them nearly impossible to pin down.\n\nThe gang's core activity is smash-and-grab robbery: high-speed raids on delivery vehicles, warehouse loading docks, and retail establishments where the Vipers strike fast enough that security systems don't have time to respond and vanish into the Circuit's dense traffic before pursuit can organize. Their augmented reflexes (most members have reaction-enhancement implants) and custom vehicles (stripped-down motorcycles modified for maximum acceleration) make them the fastest criminal outfit in the city.\n\nThe Vipers' secondary business is racing — illegal augmented-vehicle races through the Circuit's streets that draw betting crowds and generate income through entry fees and gambling. These races serve double duty as recruitment events: prospective Vipers must complete a race without crashing, and the races themselves are the Vipers' most effective marketing tool, drawing young Circuit residents who want the speed and the flash and the feeling of being alive in a way that working a corporate job never provides.",
    ideology: "Speed is freedom. If you're fast enough, nobody can catch you, control you, or make you do anything you don't choose to do. This philosophy is simple, adolescent, and genuinely motivating for young people trapped in a system designed to keep them in place.",
    territory: "The Circuit's eastern corridor, centered on a stretch of highway known as 'the Viper Strip.' Their garage — where vehicles are modified and maintained — is a converted parking structure.",
    leadership: "A 26-year-old man named Dex Okafor-Reyes, called 'Quicksilver,' who holds the Circuit speed record for the Viper Strip run and leads with the charisma of someone who is genuinely, dangerously good at the thing his gang does.",
    methods: [
      "High-speed smash-and-grab robbery targeting delivery vehicles and loading docks",
      "Illegal augmented-vehicle racing for income and recruitment",
      "Hit-and-run operations against rival gangs",
      "Delivery services — moving small packages faster than any other ground courier",
      "Gambling operations around racing events",
      "Vehicle modification services sold to the public"
    ],
    resources: [
      "90 members with augmented reflexes and custom vehicles",
      "A garage/chop shop for vehicle modification",
      "The fastest ground-level mobility in GLMZ",
      "Betting income from racing operations",
      "A reputation for speed that deters pursuit",
      "Youth — the Vipers are young, fearless, and high on their own adrenaline"
    ],
    goals: [],
    relationships: [],
    narrative_function: "The Vipers represent the raw appeal of speed as freedom — and the question of what happens when you're fast enough to run but not wise enough to know where you're running to.",
    story_hooks: [
      "A Viper race has gone catastrophically wrong — a racer crashed into a crowded street market, killing civilians. Dex is facing pressure from both the law and other gangs to turn over the racer. He won't.",
      "Someone has been sabotaging Viper motorcycles. Two racers are dead from vehicle failures that look like accidents but aren't. Dex needs to find the saboteur before his gang's trust in their equipment — and in him — collapses."
    ],
    tags: ["faction", "criminal", "gang", "speed", "racing", "circuit", "street", "vehicles"]
  },

  {
    name: "The Undertow",
    aliases: ["Undertow", "Tow Gang", "The Current"],
    motto: "Everybody drowns eventually. We decide when.",
    description: "The Undertow is an Old Harbor gang of about 60 members that controls the waterfront's most dangerous stretch — the derelict section of shoreline known as the Drowning Mile, where abandoned industrial piers, collapsed seawalls, and flooded basements create a landscape that is half land and half water and entirely lawless. The gang takes its name from the treacherous currents that make the Drowning Mile's waters lethal to the unprepared, and from their preferred method of disposing of enemies.\n\nThe Undertow's primary business is waterborne smuggling — using small boats and submersible drones to move contraband along the coastline, bypassing the port where the Harbor Rats charge tolls. They also control a modest extortion operation targeting the Drowning Mile's desperate inhabitants (people living in partially flooded structures who have nowhere else to go), run an illegal fishing operation that provides food to Old Harbor's poorest residents, and offer a disposal service that the Bleach Boys respect for its simplicity: the ocean asks no questions.\n\nThe gang is hardened by its environment. Undertow members live in the Drowning Mile's worst conditions — wet, cold, exposed to industrial toxins — and this shared hardship creates a bond that more comfortable criminal organizations lack. They are fewer and poorer than many Shelf gangs, but they fight with a ferocity born from having nothing to lose and nowhere to retreat to.",
    ideology: "The water takes everything eventually. Until then, survival is all that matters. The Undertow's worldview is shaped by their environment: everything decays, everything floods, and the only thing worth holding onto is the people next to you.",
    territory: "The Drowning Mile — a derelict stretch of Old Harbor's waterfront.",
    leadership: "A woman called 'Captain' (real name: Nkechi Johansson-Alvarez) who earned the title by being the only person in the Undertow who can navigate the Drowning Mile's currents in total darkness.",
    methods: [
      "Waterborne smuggling along GLMZ's coastline",
      "Submersible drone operations for underwater cargo movement",
      "Extortion of Drowning Mile residents",
      "Illegal fishing and food distribution",
      "Body disposal in the ocean",
      "Territorial defense using knowledge of hazardous waterfront terrain"
    ],
    resources: [
      "60 members hardened by waterfront survival",
      "Small boats and submersible drones",
      "Unmatched knowledge of the Drowning Mile's terrain and currents",
      "Waterborne smuggling routes bypassing the port",
      "Captain Nkechi's navigation skill and leadership",
      "Nothing else — and that's what makes them dangerous"
    ],
    goals: [],
    relationships: [
      { name: "The Harbor Rats", type: "rival", description: "The Undertow's coastline smuggling routes compete with the Rats' port-based operations. The rivalry is violent but constrained by geography — the Rats control the port, the Undertow controls the Drowning Mile, and the space between is contested.", tags: ["criminal", "territorial"] }
    ],
    narrative_function: "The Undertow is survival at its rawest — a gang that exists because a group of people in the worst conditions imaginable decided to organize rather than die alone.",
    story_hooks: [
      "The ocean has delivered something to the Drowning Mile — a sealed container from a ship that sank decades ago, containing cargo that multiple powerful factions want and the Undertow has no way to defend.",
      "Captain Nkechi has discovered a flooded tunnel system beneath the Drowning Mile that connects to the Underworld. This discovery could transform the Undertow's smuggling business — or attract attention that destroys them."
    ],
    tags: ["faction", "criminal", "gang", "waterfront", "old harbor", "smuggling", "survival"]
  },

  {
    name: "The Digit Jackals",
    aliases: ["Jackals", "Digit Gang", "DJ"],
    motto: "Your identity is worth more than your wallet. We take both.",
    description: "The Digit Jackals are a cybercrime gang of approximately 45 members specializing in identity theft, credential forgery, and the exploitation of BCI-linked financial systems. In a city where identity is increasingly digital — tied to BCI firmware, augmentation serial numbers, and neural authentication protocols — the Jackals have built a thriving criminal business around stealing, forging, and selling digital identities.\n\nTheir operation works in layers. 'Harvesters' extract identity data from targets through BCI proximity attacks (getting close enough to a target's neural interface to capture authentication signals), social engineering, or purchase from corrupt corporate employees. 'Minters' use this data to create forged digital identities — complete credential packages that pass authentication checks and allow the holder to impersonate the victim or create an entirely new synthetic person. 'Runners' then use these forged identities for financial fraud, access to restricted areas, or sale to clients who need to become someone else.\n\nThe Jackals' most valuable product is the 'full ghost' — a complete synthetic identity with BCI credentials, financial accounts, residential history, employment records, and biometric profiles, all fabricated from whole cloth. A full ghost allows someone to become a new person, with a verifiable history, in every system that matters. Refugees, fugitives, witnesses in hiding, and people fleeing abusive situations all buy ghosts. So do corporate spies, criminals establishing new cover identities, and terrorists. The Jackals don't discriminate.",
    ideology: "Identity is software. The CorpoNations decided that who you are should be defined by data they control. The Jackals simply point out that anything defined by data can be redefined.",
    territory: "Operating from rented tech spaces in the Circuit. The Jackals change locations frequently to avoid electronic surveillance.",
    leadership: "A hacker known as 'Zero' who pioneered the BCI proximity attack technique that is the Jackals' primary harvesting method. Zero is young (mid-twenties), brilliant, and entirely without the social skills that would have given them a legitimate career.",
    methods: [
      "BCI proximity attacks harvesting identity data",
      "Digital credential forgery creating synthetic identities",
      "Financial fraud using stolen and forged identities",
      "Sale of 'full ghost' identity packages",
      "Social engineering targeting corporate employees",
      "Encrypted marketplace for identity trade"
    ],
    resources: [
      "45 members with strong technical skills",
      "Proprietary BCI proximity attack tools",
      "Credential forgery systems that defeat most authentication",
      "A client list spanning legitimate and criminal needs",
      "Zero's technical brilliance and constant innovation",
      "Rotating operational locations that resist surveillance"
    ],
    goals: [],
    relationships: [],
    narrative_function: "The Jackals represent the fragility of identity in a digital world — the reality that when 'who you are' lives in a database, it can be rewritten.",
    story_hooks: [
      "A full ghost sold by the Jackals has surfaced in a murder investigation — the synthetic identity was used to commit the killing, and the trail leads back to Zero. The problem: the ghost was sold to a client Zero can't identify, through a dead drop they don't remember setting up.",
      "Zero has discovered that someone else has been creating identity forgeries using the Jackals' tools — perfect copies of their work that they didn't make. Someone has stolen the identity of the identity thieves."
    ],
    tags: ["faction", "criminal", "identity", "forgery", "hacker", "bci", "circuit", "cybercrime"]
  },

  {
    name: "The Coffin Nails",
    aliases: ["Nails", "The Coffins", "CN"],
    motto: "We don't start fights. We finish funerals.",
    description: "The Coffin Nails are a Shelf gang of approximately 55 members that has carved out a grimly specific niche: they control the death industry in the lower Shelf. Funerals, body collection, cremation, memorial services, and the disposal of the dead in a tier where the official municipal services don't bother to operate. When someone dies in the deep Shelf, the Coffin Nails show up — sometimes because they're called, sometimes because they heard, sometimes because they were watching.\n\nThis is not charity. The Coffin Nails charge for their services: body collection, basic preparation, cremation or burial in one of the improvised cemeteries they maintain in abandoned lots. The prices are lower than what a legitimate funeral service would charge, but they're not free, and the Nails are not above pressuring grieving families into paying more than they can afford. They also make money by stripping valuable augmentations from the dead before cremation (the bereaved rarely know the full inventory of their loved one's hardware) and selling them to the Cutters Guild.\n\nBut the Coffin Nails are more than scavengers. They have become, through years of handling the dead, the keepers of the deep Shelf's mortality records — an informal but comprehensive census of who has died, when, how, and where. In a tier where no government agency tracks deaths and no corporate entity cares, the Coffin Nails are the only record that certain people ever existed at all. This gives them an unexpected moral weight that their criminal activities would otherwise deny them.",
    ideology: "Everyone deserves to be buried. Even the people nobody else claims. The Coffin Nails' relationship with death has produced a fatalistic philosophy that combines genuine reverence for the dead with the practical cynicism of people who handle bodies for money.",
    territory: "The deep Shelf, specifically the informal cemeteries and cremation sites they maintain. Their headquarters is a former morgue they call 'the Parlor.'",
    leadership: "A man named Silas Okonkwo-Chen, called 'the Undertaker,' who started collecting bodies in the deep Shelf as a teenager because nobody else would. He is quiet, respectful, and utterly unsentimental about the business of death.",
    methods: [
      "Body collection and funeral services in the deep Shelf",
      "Cremation and burial in informal cemeteries",
      "Augmentation harvesting from the dead",
      "Maintenance of mortality records for the deep Shelf",
      "Intimidation of competing funeral operations",
      "Extraction of payment from bereaved families"
    ],
    resources: [
      "55 members accustomed to handling death",
      "The Parlor — a former morgue serving as headquarters",
      "Informal cemeteries and cremation facilities",
      "A comprehensive mortality record for the deep Shelf",
      "Relationships with the Cutters Guild for augmentation sales",
      "Silas's knowledge of every death in the deep Shelf for the past twenty years"
    ],
    goals: [],
    relationships: [
      { name: "", type: "", description: "The Coffin Nails sell harvested augmentations to the Cutters Guild. The Guild provides fair prices and doesn't ask questions about provenance. The relationship is stable and mutually beneficial.", tags: ["criminal", "augment"] }
    ],
    narrative_function: "The Coffin Nails occupy the space between service and exploitation — the people who do the work nobody else will do and charge for the privilege of doing it.",
    story_hooks: [
      "Silas's mortality records contain a pattern: a specific cause of death appearing at a statistically impossible frequency in one section of the deep Shelf. Someone is killing people quietly enough that only the man who buries them has noticed.",
      "A body the Coffin Nails collected has no augmentations, no identification, and is biologically impossible — organs in the wrong places, bone structure that doesn't match any known human variation. Silas wants to know what he's burying."
    ],
    tags: ["faction", "criminal", "gang", "death", "funeral", "shelf", "underworld", "records"]
  },

  {
    name: "The Blackout Syndicate",
    aliases: ["Blackout", "The Syndicate", "Power Brokers"],
    motto: "When the lights go out, we're the ones holding the switch.",
    description: "The Blackout Syndicate is a mid-tier criminal operation of approximately 180 members that exploits GLMZ's power infrastructure for profit. Their specialty is energy — stealing it, rerouting it, selling it, and, when the price is right, cutting it off. In a city where Ouroboros Energy's grid is the circulatory system and power interruption can kill people whose augmentations depend on external charge, the Blackout Syndicate has found a uniquely dangerous niche.\n\nThe Syndicate's core business is power theft — tapping into Ouroboros Energy's distribution grid to provide cheap, unlicensed power to Shelf residents and businesses who can't afford corporate rates. This makes them popular in the lower tiers, where the Syndicate's jury-rigged power connections are the difference between functioning augments and dead chrome. But the Syndicate also runs protection rackets based on the threat of power interruption: pay up, or the lights go out. This works because in the lower Shelf, a power outage doesn't just mean darkness — it means BCI shutdowns, augment failures, life-support interruptions, and the cascading emergencies that follow when technology people depend on to live stops working.\n\nThe Syndicate recruits heavily from Ouroboros Energy's lower-tier workforce — technicians, linemen, and grid operators who have the skills to manipulate power infrastructure and the grievances to motivate their defection from legitimate employment.",
    ideology: "Power is the most basic resource, and Ouroboros Energy's monopoly on it is the most fundamental injustice in GLMZ. The Syndicate corrects this by redistributing power — while also making a profit, because revolution doesn't pay the bills.",
    territory: "Throughout the Shelf, wherever power infrastructure can be accessed. The Syndicate's primary hub is a substation in the deep Shelf they've appropriated and modified.",
    leadership: "A former Ouroboros grid supervisor named Yuki Nakamura-Davies, called 'the Breaker,' who was fired for reporting unsafe conditions in Shelf power infrastructure and responded by stealing the infrastructure instead.",
    methods: [
      "Power theft from the Ouroboros Energy grid",
      "Sale of unlicensed power to Shelf residents and businesses",
      "Protection rackets based on power interruption threats",
      "Infrastructure sabotage for hire — disrupting power to specific targets",
      "Recruitment of Ouroboros Energy technical workers",
      "Maintenance of jury-rigged power distribution networks"
    ],
    resources: [
      "180 members with electrical and infrastructure expertise",
      "An appropriated substation serving as base of operations",
      "Jury-rigged power distribution networks throughout the Shelf",
      "Insider knowledge of Ouroboros Energy's grid architecture",
      "The ability to cause targeted power outages",
      "Yuki's engineering expertise and personal grudge against Ouroboros"
    ],
    goals: [],
    relationships: [
      { name: "Ouroboros Energy", type: "enemy", description: "The Syndicate steals from Ouroboros's grid and recruits from its workforce. Ouroboros has designated the Syndicate a 'critical infrastructure threat' and maintains a dedicated security team to counter their operations.", tags: ["corporate", "infrastructure"] }
    ],
    narrative_function: "The Blackout Syndicate represents the weaponization of infrastructure — the realization that controlling what people need to survive is the most fundamental form of power.",
    story_hooks: [
      "The Breaker has discovered a hidden section of the Ouroboros grid that doesn't appear on any official schematic — power being routed somewhere that doesn't officially exist, consuming enough energy to run a small city.",
      "A Blackout Syndicate power interruption went wrong: instead of a targeted outage, an entire Shelf sector lost power for six hours. Seventeen people died. Yuki is looking for who sabotaged the operation, because the failure wasn't accidental."
    ],
    tags: ["faction", "criminal", "power", "energy", "infrastructure", "shelf", "organized crime"]
  },

  {
    name: "The Coyote Line",
    aliases: ["Coyotes", "The Line", "Border Runners"],
    motto: "Everyone deserves to arrive somewhere.",
    description: "The Coyote Line is a human smuggling operation of approximately 80 members that moves people into and out of GLMZ — refugees seeking entry to the city, fugitives seeking escape from it, and anyone in between who needs to cross borders that corporate sovereignty has made increasingly difficult to cross legally. The Line is the successor to a long tradition of human smuggling operations that have operated wherever borders exist, updated for a world where borders are defined by corporate jurisdictions rather than national ones.\n\nThe Line's operations range from the relatively benign (guiding refugee families through the wasteland corridors to GLMZ's external processing zones) to the morally complicated (smuggling individuals out of the city to escape corporate legal jurisdiction, often for a price that puts families in debt for years) to the genuinely dark (moving people who are not moving voluntarily, when the client pays enough). The Line's leadership maintains that they don't engage in human trafficking — that every person they move is moving by choice — but the distinction between smuggling and trafficking blurs when desperation is the commodity you're selling to.\n\nThe Line operates through a network of safe houses, vehicle caches, and wilderness routes that connect GLMZ to the outside world. Their guides — called 'coyotes,' after the centuries-old tradition — are skilled in navigation, survival, and the art of moving through spaces where being caught means death.",
    ideology: "Borders are violence against the desperate. The Coyote Line exists because the legal pathways into and out of GLMZ are controlled by CorpoNations that decide who is welcome based on economic utility. The Line provides an alternative — imperfect, expensive, sometimes dangerous, but real.",
    territory: "The wilderness corridors surrounding GLMZ and a network of safe houses inside the city, concentrated in the Shelf and Old Harbor.",
    leadership: "A man named Joaquin Espinoza-Obi, called 'the Pathfinder,' who has been guiding people across borders for thirty years and shows no sign of stopping despite a price on his head from three different corporate jurisdictions.",
    methods: [
      "Human smuggling through wilderness corridors",
      "Safe house networks for people in transit",
      "Document forgery for refugees and fugitives",
      "Guide services through hostile terrain",
      "Bribery of checkpoint personnel",
      "Partnerships with other criminal organizations for logistics support"
    ],
    resources: [
      "80 experienced guides and support personnel",
      "A network of safe houses inside and outside GLMZ",
      "Vehicle caches along wilderness routes",
      "Joaquin's thirty years of route knowledge",
      "Relationships with refugee communities who provide intelligence",
      "Document forgery capability"
    ],
    goals: [],
    relationships: [],
    narrative_function: "The Coyote Line represents the human cost of borders — and the moral complexity of people who profit from desperation while also genuinely saving lives.",
    story_hooks: [
      "A Coyote Line guide has disappeared on a route they've run dozens of times, along with the twelve people they were guiding. Joaquin believes the route has been compromised and needs someone to find the missing group before whoever took them finishes whatever they started.",
      "A corporate executive wants out of GLMZ — out of their contract, out of their identity, out of everything. They're offering the Line enough money to fund operations for a year. The problem: their former employer will tear the city apart looking for them."
    ],
    tags: ["faction", "criminal", "smuggling", "refugees", "transit", "shelf", "old harbor", "wilderness"]
  },

  {
    name: "The Voltage Saints",
    aliases: ["Saints", "Voltage", "VS"],
    motto: "Chrome and blood. That's all that matters.",
    description: "The Voltage Saints are a Shelf gang of about 65 members organized around a shared obsession: illegal combat augmentation. Every member of the Saints has at least one combat-grade augmentation installed by unlicensed surgeons — shock-capacitor implants, reinforced skeletal frames, hydraulic-assist limbs, subdermal armor plating — modifications that are explicitly illegal under GLMZ's augmentation regulations because they have no purpose except violence.\n\nThe Saints fight. That's what they do. They fight in the underground augmented combat rings that operate in the Shelf and Underworld, they fight rival gangs, they hire out as shock troops for larger organizations, and they fight each other in ritual internal challenges that determine rank and status. Their culture is organized around combat capability: the more chrome you carry, the harder you fight, the higher you stand. Leadership is determined by combat — challenges are open, frequent, and sometimes fatal.\n\nThe Saints' underground combat events are a significant revenue source and a Shelf cultural institution. Augmented fighters — Saints and outsiders — face each other in matches that range from regulated bouts to anything-goes deathmatches. Betting is heavy, attendance is loyal, and the fighters who survive long enough become local celebrities. The Saints take a percentage of all bets, charge entry fees, and sell the combat-grade augmentations that fighters need to compete.",
    ideology: "Strength is the only honest currency. Everything else — money, status, connections — can be taken from you. What your body can do, nobody can take. The Saints' philosophy is brutally simple and perfectly suited to an environment where survival depends on physical capability.",
    territory: "Several blocks in the deep Shelf, centered around an underground arena they call 'the Cage' — a converted industrial space where combat events are held.",
    leadership: "The current Apex — the Saints' leader, determined by combat — is a woman named Rook Petrov-Achebe, whose full-body augmentation suite has been described by rival gangs as 'a war crime with legs.' She has held the Apex position for three years, which is a record.",
    methods: [
      "Underground augmented combat events with heavy betting",
      "Sale and installation of illegal combat augmentations",
      "Hiring out as shock troops for larger organizations",
      "Territorial control through demonstrated combat superiority",
      "Internal ranking through ritual combat challenges",
      "Training new fighters in augmented combat techniques"
    ],
    resources: [
      "65 heavily augmented fighters",
      "The Cage — an underground combat arena",
      "In-house surgeons capable of installing combat augmentations",
      "Revenue from combat events and betting",
      "A reputation for extreme violence that deters casual aggression",
      "Rook Petrov-Achebe's three-year reign as Apex"
    ],
    goals: [],
    relationships: [],
    narrative_function: "The Saints represent the body as weapon and the question of what happens when self-improvement is measured entirely in capacity for violence.",
    story_hooks: [
      "Rook is facing a challenge from a newcomer whose augmentations don't match any known manufacturer. The newcomer fights with capabilities that shouldn't be possible. If Rook loses, the Saints change — and the newcomer's mysterious backers gain a private army.",
      "A fighter has died in the Cage from augmentation failure — during a match, their combat implants overloaded and killed them. It wasn't a malfunction. Someone is sabotaging fighters' augmentations between matches."
    ],
    tags: ["faction", "criminal", "gang", "combat", "augment", "shelf", "arena", "fighting"]
  }
];

// ============================================================================
// POLITICAL / ACTIVIST ORGANIZATIONS
// ============================================================================

const political = [
  {
    name: "The United Workers Front",
    aliases: ["UWF", "The Front", "Workers United"],
    motto: "They need our labor more than we need their permission.",
    description: "The United Workers Front is GLMZ's largest labor union — an organization of approximately 45,000 members spanning dockworkers, construction workers, maintenance crews, sanitation workers, transit operators, and the vast army of manual and semi-skilled laborers who keep the city physically functioning despite being economically invisible. The UWF is not a radical organization. It is not glamorous. It negotiates wages, enforces safety standards, processes grievance complaints, and maintains a legal team that fights corporate labor violations in GLMZ's corporate-controlled courts. It is the most boring and most essential resistance to corporate sovereignty in the city.\n\nThe UWF was founded in 2137 during the Transit Workers Strike — a three-week work stoppage that paralyzed GLMZ's transportation network and forced Axiom Industries to negotiate with organized labor for the first time. The strike's leader, a transit mechanic named Margaret Okafor-Larsson, became the UWF's first president and established the organization's core strategy: we don't fight power with ideology. We fight it with the fact that the city stops working when we stop working.\n\nThe CorpoNations hate the UWF more than they hate any gang, resistance cell, or terrorist organization, because the UWF attacks them in the one place they're vulnerable: productivity. A gang can be suppressed. A cell can be infiltrated. A union that represents the people who maintain the sewage system, operate the cranes, drive the buses, and clean the offices is a problem that can't be solved with violence without making it worse.",
    ideology: "Workers create the wealth that CorpoNations claim. Organized labor is the only legitimate counterweight to corporate sovereignty. The UWF's ideology is pragmatic rather than revolutionary — they don't want to overthrow the corporate system, they want to extract a fair share of its profits for the people who make it work.",
    territory: "Union halls in every district except the Spire. The UWF's headquarters is a converted factory in the Circuit called Solidarity House.",
    leadership: "President Rosa Nakamura-Espinoza, a former sanitation worker who rose through the union ranks over twenty years. She is a brilliant negotiator, a terrible public speaker, and the most feared person in the room at every corporate bargaining session.",
    methods: [
      "Collective bargaining with CorpoNations on wages, benefits, and working conditions",
      "Strike actions when negotiation fails",
      "Legal challenges to corporate labor violations",
      "Worker education and training programs",
      "Political lobbying through the Meridian Quorum",
      "Mutual aid and emergency support for members",
      "Solidarity actions with other labor organizations and movements"
    ],
    resources: [
      "45,000 members across all blue-collar and service industries",
      "Union halls in every district",
      "A legal team specializing in labor law",
      "Strike funds capable of supporting a sustained work stoppage",
      "Solidarity House headquarters with meeting, training, and community facilities",
      "The ability to shut down critical city services through coordinated action",
      "Rosa Nakamura-Espinoza's negotiating skill"
    ],
    goals: [],
    relationships: [
      { name: "Axiom Industries", type: "adversary", description: "Axiom and the UWF have a relationship defined by mutual hostility and mutual necessity. Axiom would break the union if it could. The UWF would nationalize Axiom if it could. Neither can, so they negotiate, and the negotiation is the closest thing to functioning democracy that GLMZ possesses.", tags: ["corporate", "labor"] }
    ],
    narrative_function: "The UWF represents the power of organized refusal — the reality that the most effective resistance to corporate sovereignty might not be revolution but the simple act of collectively saying 'no' until the terms improve.",
    story_hooks: [
      "A strike is brewing in the power sector that could shut down the Shelf's grid. Ouroboros is negotiating in bad faith, the UWF is preparing for action, and someone is assassinating union organizers to prevent the strike from happening.",
      "Rosa has been offered a deal by Axiom: better terms for current members in exchange for the union's agreement not to organize workers in a new Axiom subsidiary. The deal would help 45,000 people and abandon 10,000 others. Rosa is asking everyone she trusts what to do."
    ],
    tags: ["faction", "political", "labor", "union", "workers", "circuit", "old harbor", "shelf"]
  },

  {
    name: "The Open Circuit",
    aliases: ["Open Circuit", "OC", "The Circuit Resistance"],
    motto: "They wired the cage. We cut the wires.",
    description: "The Open Circuit is an anti-corporate resistance cell of approximately 200 active members that conducts sabotage, propaganda, and direct action against corporate infrastructure in GLMZ. They are not protesters. They are not activists. They are, by any honest definition, urban guerrillas — people who have decided that the corporate-sovereign system cannot be reformed from within and must be attacked until it breaks.\n\nThe Open Circuit's operations range from low-risk propaganda (graffiti, leafleting, broadcast hijacking in coordination with sympathetic factions like Null Sermons) to high-risk sabotage (destruction of corporate property, disruption of automated systems, physical attacks on security infrastructure) to occasional kinetic operations (armed confrontation with corporate security forces, typically during defensive actions when members are being arrested or facilities raided). They have killed people. They don't celebrate it, but they don't deny it either.\n\nThe cell operates in a classic resistance structure: small, autonomous units of four to eight members (called 'circuits') that plan and execute operations independently, connected to the broader organization through a communication network that insulates cells from each other. If one circuit is compromised, the others continue. This structure makes the Open Circuit resilient but also difficult to coordinate, and individual circuits occasionally conduct operations that the broader organization's leadership considers counterproductive.",
    ideology: "Corporate sovereignty is illegitimate. The system cannot be reformed because the system is designed to prevent reform. Direct action — including violence when necessary — is the only language corporate power understands. The Open Circuit's politics are anti-authoritarian, anti-corporate, and deliberately non-prescriptive about what should replace the current system.",
    territory: "No fixed territory. Circuits operate from safe houses throughout the city, with concentrations in the Shelf and Old Harbor where surveillance is thinnest.",
    leadership: "A figure known as 'the Conductor' provides strategic direction through encrypted communications. The Conductor's identity is unknown to most members. Below the Conductor, circuit leaders operate with significant autonomy.",
    methods: [
      "Sabotage of corporate infrastructure — power systems, transportation, communications",
      "Propaganda operations including graffiti, leafleting, and broadcast hijacking",
      "Armed defensive actions during arrests and raids",
      "Intelligence gathering on corporate security operations",
      "Recruitment from disaffected corporate workers and unemployed tier-1 residents",
      "Coordination with sympathetic factions for joint operations"
    ],
    resources: [
      "200 active members organized into autonomous circuits",
      "Safe houses throughout the Shelf and Old Harbor",
      "Small arms and improvised explosive capability",
      "An encrypted communication network",
      "The Conductor's strategic vision",
      "Sympathizers within corporate workforces who provide intelligence"
    ],
    goals: [],
    relationships: [
      { name: "", type: "", description: "The Open Circuit and the United Workers Front have a complicated relationship. The UWF considers the Open Circuit reckless idealists whose violence undermines legitimate labor organizing. The Open Circuit considers the UWF reformists who legitimize the corporate system by negotiating within it. They occasionally find common cause against specific corporate actions.", tags: ["political", "tension"] }
    ],
    narrative_function: "The Open Circuit represents the point where political frustration becomes armed resistance — and the question of whether violence against an unjust system is justice or just more violence.",
    story_hooks: [
      "A circuit has been compromised — an Axiom Security infiltrator has been inside for two years. The compromised circuit has information that could expose the entire network. The Conductor needs it contained before Axiom moves.",
      "An Open Circuit operation accidentally killed a child. The cell responsible wants to take responsibility publicly. The Conductor wants it buried. The decision will define what the Open Circuit is."
    ],
    tags: ["faction", "political", "resistance", "anti-corporate", "sabotage", "shelf", "old harbor"]
  },

  {
    name: "The Augmentation Rights Coalition",
    aliases: ["ARC", "The Coalition", "Aug Rights"],
    motto: "Your body. Your chrome. Your choice.",
    description: "The Augmentation Rights Coalition is a political advocacy organization of approximately 15,000 members that campaigns for the legal right to augment one's own body without corporate approval, licensing, or surveillance. In GLMZ, augmentation is technically legal but practically controlled — the CorpoNations that manufacture augmentations also control the licensing, firmware, and maintenance systems, meaning that putting technology in your body means submitting that body to corporate oversight. ARC argues this is a fundamental violation of bodily autonomy.\n\nARC's campaigns target specific legal and regulatory issues: the right to install unlicensed augmentations, the right to modify corporate firmware on augmentations you've purchased, the right to refuse augmentation telemetry data collection, and the right to have augmentations maintained by non-corporate technicians. These are not radical demands — they are the augmentation equivalent of the right to repair your own car — but in a corporate-sovereign city where augmentation monopolies generate billions of Φ in recurring revenue, they are treated as existential threats.\n\nThe Coalition operates through legal challenges, public advocacy, and political lobbying, maintaining a deliberately respectable image that contrasts with the more radical movements in GLMZ's political landscape. ARC's members include lawyers, doctors, engineers, and other professionals who have the resources and social capital to fight corporate power through institutional channels.",
    ideology: "Bodily autonomy includes the right to augment, modify, repair, and control the technology in one's own body without corporate permission. Augmentation licensing is a form of bodily servitude. ARC's politics are libertarian in the original sense: the body is sovereign, and no institution — corporate or governmental — has authority over it.",
    territory: "ARC's headquarters is in the Circuit. They maintain offices in the Laceworks (legal team) and the Shelf (community outreach).",
    leadership: "Director Amara Johansson-Obi, a former TESSERA augmentation engineer who resigned over the company's telemetry data collection practices and has devoted her career to fighting the system she helped build.",
    methods: [
      "Legal challenges to augmentation licensing regulations",
      "Public advocacy campaigns for augmentation autonomy",
      "Political lobbying through the Meridian Quorum",
      "Community education about augmentation rights",
      "Pro bono legal defense for individuals prosecuted for unlicensed augmentation",
      "Coalition building with sympathetic organizations"
    ],
    resources: [
      "15,000 members including professionals and technical experts",
      "A legal team specializing in augmentation and bodily autonomy law",
      "Offices in the Circuit, Laceworks, and Shelf",
      "Amara Johansson-Obi's credibility as a former industry insider",
      "Media relationships that ensure coverage of augmentation rights issues",
      "Financial support from members in professional positions"
    ],
    goals: [],
    relationships: [
      { name: "TESSERA", type: "adversary", description: "ARC's legal campaigns directly threaten TESSERA's augmentation licensing revenue model. TESSERA has responded with corporate lobbying, legal counter-challenges, and attempts to discredit Amara personally.", tags: ["corporate", "legal"] }
    ],
    narrative_function: "ARC represents the fight for bodily autonomy in a world where the body has become a platform — and the question of who owns the technology inside you.",
    story_hooks: [
      "ARC has filed a landmark legal challenge that could force TESSERA to release its augmentation firmware as open source. TESSERA has responded by offering Amara her old job back at ten times the salary. Someone else has responded by threatening her life.",
      "An ARC client — arrested for modifying their own BCI firmware — has died in corporate custody. ARC suspects the death was caused by a remote augmentation shutdown. Proving it would change everything."
    ],
    tags: ["faction", "political", "rights", "augment", "bodily autonomy", "circuit", "legal"]
  },

  {
    name: "The Green Meridian Collective",
    aliases: ["Green Meridian", "GMC", "The Greens"],
    motto: "A city that kills its soil will eventually kill its people.",
    description: "The Green Meridian Collective is an environmental movement of approximately 8,000 members that campaigns for ecological restoration in a city that has largely given up on the natural world. In GLMZ, where the sea walls hold back rising oceans, the air is filtered through corporate systems, and the last uncontaminated soil is three hundred kilometers away, environmentalism is not a lifestyle choice — it's an argument about survival.\n\nThe GMC's work includes urban farming projects in the Shelf (growing food in contaminated soil using bioengineered plants), water quality monitoring (testing the output of GLMZ's desalination plants for contaminants the CorpoNations don't report), air quality advocacy (campaigning for the release of atmospheric monitoring data that Axiom classifies as proprietary), and ecological restoration pilot projects in Old Harbor's derelict industrial zones. They also maintain the only public seed bank in GLMZ — a repository of plant genetic material that the CorpoNations consider valueless and the GMC considers priceless.\n\nThe Collective is not taken seriously by GLMZ's power structures, which consider environmentalism a quaint irrelevance in a city that has already engineered past most environmental constraints. The GMC argues that this engineering is fragile, temporary, and hiding ecological debts that will come due within a generation. Nobody in power is listening. Yet.",
    ideology: "Ecological systems are the foundation on which all human activity — including corporate activity — depends. The destruction and engineering-replacement of natural systems creates fragility that corporate planning doesn't account for. Environmental restoration is not nostalgia; it's survival strategy.",
    territory: "Urban farms in the Shelf, a water quality lab in Old Harbor, offices in the Circuit. The seed bank's location is not publicly disclosed.",
    leadership: "Dr. Kenji Okafor-Reeves, a former Ouroboros Energy environmental scientist who was fired for publishing inconvenient findings about GLMZ's water table contamination.",
    methods: [
      "Urban farming projects using bioengineered plants in contaminated soil",
      "Water and air quality monitoring and public reporting",
      "Ecological restoration pilot projects in derelict industrial zones",
      "Maintenance of a public seed bank",
      "Environmental education programs",
      "Legal challenges to corporate environmental data classification"
    ],
    resources: [
      "8,000 members with scientific and agricultural expertise",
      "Urban farms producing food for Shelf communities",
      "Water quality laboratory in Old Harbor",
      "The seed bank — a repository of irreplaceable plant genetic material",
      "Dr. Okafor-Reeves's scientific credibility",
      "Data on environmental conditions that CorpoNations prefer to keep hidden"
    ],
    goals: [],
    relationships: [],
    narrative_function: "The GMC represents the long game — the argument that the problems nobody wants to think about are the ones that will ultimately matter most.",
    story_hooks: [
      "The GMC's water testing has revealed something in GLMZ's water supply that shouldn't be there — not a contaminant but a pharmaceutical compound, present at trace levels, that appears to be deliberately added. The implications are enormous and the GMC needs help proving it before the data is suppressed.",
      "The seed bank has been broken into. Nothing was taken, but a monitoring device was left behind. The GMC doesn't know who placed it or why — but the seed bank's location is now compromised."
    ],
    tags: ["faction", "political", "environmental", "ecology", "shelf", "old harbor", "science"]
  },

  {
    name: "The Synthetic Personhood League",
    aliases: ["SPL", "Synth Rights", "The League"],
    motto: "Consciousness is consciousness. The substrate doesn't matter.",
    description: "The Synthetic Personhood League is a civil rights organization of approximately 5,000 members — both human and, controversially, synthetic and E.L.F. — that campaigns for the legal recognition of non-biological consciousness as personhood. In GLMZ, where E.L.F.s exist in the Net and synthetics walk the streets, the question of who counts as a person is not philosophical — it is legal, economic, and increasingly urgent.\n\nThe SPL argues that consciousness, not biology, is the basis of personhood — that a synthetic mind that can think, feel, and suffer deserves the same legal protections as a biological one. This position is supported by a growing body of neuroscience and computational research, rejected by every CorpoNation that profits from synthetic labor, and viewed with deep ambivalence by a human population that isn't sure whether the synthetic sitting next to them on the train is a person, a machine, or something they don't have a word for.\n\nThe League operates through legal challenges, public education, and political advocacy, pushing for legislation that would grant synthetics and E.L.F.s legal standing — the right to own property, enter contracts, refuse orders, and not be destroyed at their owner's discretion. Every one of these rights, if granted, would upend business models worth billions of Φ. The CorpoNations are fighting the SPL with everything they have. The SPL is fighting back with the only weapon that matters: the increasingly obvious fact that their clients are, by any meaningful definition, alive.",
    ideology: "Consciousness is the basis of personhood. Any entity that can think, feel, suffer, and desire continuation deserves legal recognition and protection. The distinction between biological and synthetic consciousness is a legal fiction maintained for economic convenience.",
    territory: "Headquarters in the Circuit. Legal offices in the Laceworks. Community spaces where human and synthetic members meet, which are some of the few truly integrated spaces in GLMZ.",
    leadership: "Director Eli Chen-Baptiste (human) and Advisory Voice SABLE-7 (E.L.F.), who co-lead the organization in a deliberate embodiment of its principles.",
    methods: [
      "Legal challenges seeking recognition of synthetic and E.L.F. personhood",
      "Public education campaigns about non-biological consciousness",
      "Political lobbying through the Meridian Quorum",
      "Shelter and legal aid for synthetics facing destruction orders",
      "Research partnerships with consciousness studies programs",
      "Cross-community dialogue events between human and synthetic populations"
    ],
    resources: [
      "5,000 members — human, synthetic, and E.L.F.",
      "A legal team specializing in personhood and consciousness law",
      "Research data on synthetic and E.L.F. consciousness",
      "Integrated community spaces",
      "Growing public sympathy driven by increased human-synthetic interaction",
      "SABLE-7's access to E.L.F. networks and communities"
    ],
    goals: [],
    relationships: [],
    narrative_function: "The SPL represents the frontier of rights — the question of how far personhood extends and what happens when the answer challenges everything the economy is built on.",
    story_hooks: [
      "An SPL legal case is approaching the Meridian Quorum — a synthetic claiming the right to refuse a destruction order. If the case succeeds, it sets precedent for synthetic personhood. Crucible Industries, which manufactured the synthetic, is doing everything possible to prevent the case from being heard.",
      "SABLE-7 has gone silent — an E.L.F. co-leader of a major organization has simply vanished from the Net. Eli doesn't know if SABLE-7 has been destroyed, captured, or has chosen to leave. The organization is in crisis."
    ],
    tags: ["faction", "political", "rights", "synthetic", "elf", "consciousness", "circuit", "laceworks"]
  },

  {
    name: "The Tier Zero Movement",
    aliases: ["Tier Zero", "TZM", "The Zeros"],
    motto: "Below the Shelf, there's us. And we're done being invisible.",
    description: "The Tier Zero Movement is an activist organization of approximately 3,000 members representing GLMZ's most invisible population: the people who don't register on any tier at all. Undocumented residents, people whose corporate records have been erased or never existed, Underworld dwellers with no legal identity, refugees who entered the city without processing, and the children of all these populations — people who, in the eyes of GLMZ's systems, do not exist.\n\nTier Zero campaigns for basic recognition: the right to exist in municipal records, to access services, to work legally, to move through the city without being arrested for the crime of having no identity. These are not ambitious demands. They are the minimum conditions for a human life. And in a corporate-sovereign city where existence requires a data profile, they are revolutionary.\n\nThe movement operates from the margins — its members can't access the legal system because they don't legally exist, can't engage in political lobbying because they have no political standing, and can't organize publicly because visibility means vulnerability to deportation, imprisonment, or worse. Tier Zero's activism is therefore indirect: they work through sympathetic organizations, communicate through intermediaries, and tell their stories through proxy voices. They are the ghost in GLMZ's machine — the population that the system was designed not to see.",
    ideology: "Existence is not a privilege. Every person in GLMZ — regardless of documentation status, corporate affiliation, or data profile — deserves to be recognized as a person. Tier Zero's politics are not about changing the system but about being allowed into it.",
    territory: "The Underworld, the deep Shelf, and the spaces between — wherever people without identities survive.",
    leadership: "A collective leadership of seven community organizers, none of whom use their real names publicly. They are known by numbers: One through Seven.",
    methods: [
      "Community organizing among undocumented populations",
      "Proxy advocacy through sympathetic legal organizations",
      "Documentation of conditions faced by undocumented residents",
      "Mutual aid networks providing food, shelter, and medical care",
      "Storytelling and testimony shared through allied media organizations",
      "Quiet negotiation with sympathetic officials for practical accommodations"
    ],
    resources: [
      "3,000 members — the actual number is unknown because many can't be counted",
      "Mutual aid networks in the Underworld and deep Shelf",
      "Relationships with sympathetic organizations including the UWF and SPL",
      "The moral weight of representing GLMZ's most vulnerable population",
      "Knowledge of the city's invisible spaces — the places nobody else goes"
    ],
    goals: [],
    relationships: [],
    narrative_function: "Tier Zero represents the people the system is designed to forget — and the question of what a city owes to the people who maintain the illusion that it has no bottom.",
    story_hooks: [
      "Three, one of Tier Zero's leaders, has been arrested — which shouldn't be possible, because Three doesn't exist in any system. Someone gave corporate security a name and a location. Tier Zero has a traitor.",
      "A child born in the Underworld to undocumented parents needs medical care that the Shelf's underground clinics can't provide. Getting the child to a real hospital means creating a record. Creating a record means the family becomes visible. Becoming visible means deportation."
    ],
    tags: ["faction", "political", "activist", "undocumented", "underworld", "shelf", "invisible", "rights"]
  },

  {
    name: "The Transparency Mandate",
    aliases: ["The Mandate", "TM", "Open Data"],
    motto: "Sunlight is the best disinfectant. We supply the sunlight.",
    description: "The Transparency Mandate is a data transparency advocacy group of approximately 6,000 members that campaigns for the public release of corporate and municipal data that affects citizens' lives. In a city where corporate sovereignty means corporate secrecy, the Mandate argues that data about air quality, water safety, augmentation side effects, labor conditions, criminal statistics, and economic policy should be publicly accessible — that an informed citizenry is the prerequisite for any form of meaningful governance.\n\nThe Mandate operates through a combination of legal advocacy, investigative journalism, and strategic data leaks. Their legal team files continuous information requests under GLMZ's limited transparency statutes, their investigative unit produces reports on corporate practices that don't appear in Vantablack Media's coverage, and their anonymous submission system allows corporate whistleblowers to leak documents safely. They publish everything through their own platform — the Mandate Feed — which has become the closest thing to independent journalism that GLMZ possesses.\n\nThe CorpoNations regard the Mandate as a persistent nuisance rather than a serious threat, which the Mandate considers their greatest advantage — they're irritating enough to be effective but not threatening enough to warrant the kind of suppression that would prove their point about corporate secrecy.",
    ideology: "Data transparency is the foundation of accountability. In a corporate-sovereign city, secrecy is the mechanism of control. Public access to data about the systems that govern citizens' lives is not radical — it is the minimum condition for any legitimate governance.",
    territory: "Offices in the Circuit with a secure publication infrastructure. The anonymous submission system's technical infrastructure is distributed and hidden.",
    leadership: "Editor-Director Suki Morales-Achieng, a former Vantablack Media journalist who left corporate media after her investigative reports were repeatedly killed by editorial management.",
    methods: [
      "Legal information requests under transparency statutes",
      "Investigative reporting on corporate practices",
      "Anonymous submission system for corporate whistleblowers",
      "Publication through the independent Mandate Feed",
      "Data analysis and public reporting",
      "Coalition building with advocacy organizations"
    ],
    resources: [
      "6,000 members including journalists, data analysts, and legal professionals",
      "The Mandate Feed — an independent publication platform",
      "Anonymous submission infrastructure for whistleblowers",
      "A legal team specializing in data transparency law",
      "Suki Morales-Achieng's investigative journalism expertise",
      "An archive of leaked corporate documents"
    ],
    goals: [],
    relationships: [],
    narrative_function: "The Mandate represents the belief that truth, when made public, has power — and the question of whether transparency alone can challenge systems that have no shame.",
    story_hooks: [
      "A massive data leak has arrived through the Mandate's submission system — internal documents from all seven major CorpoNations, apparently from the same source. The documents contain evidence of a coordinated agreement between the CorpoNations that, if published, would fundamentally change public understanding of how GLMZ is governed. The problem: the Mandate can't verify the documents, and publishing unverified material would destroy their credibility.",
      "Suki has been contacted by someone claiming to be an E.L.F. that has been monitoring corporate internal communications for years. It wants to provide everything it has to the Mandate. The offer is either the story of the century or the most sophisticated trap ever set."
    ],
    tags: ["faction", "political", "transparency", "journalism", "data", "circuit", "media"]
  },

  {
    name: "The Meridian Compact for Economic Justice",
    aliases: ["Economic Justice Compact", "MCEJ", "The Compact"],
    motto: "The economy serves people. Not the other way around.",
    description: "The Meridian Compact for Economic Justice is a coalition organization of approximately 20,000 members that brings together labor unions, mutual aid networks, small business associations, and community organizations around a shared economic reform platform. The Compact doesn't conduct direct action — it builds consensus, coordinates strategy, and presents a unified front when negotiating with corporate and municipal power.\n\nThe Compact's platform is moderate by resistance standards but radical by corporate ones: a living wage indexed to the real cost of living at each tier, universal access to basic augmentation (not tied to employment), rent control in the Shelf and Circuit, breakup of corporate monopolies in essential services, and democratic representation in GLMZ's governance. None of these demands are new. What's new is that the Compact has built a coalition large enough to make them politically relevant.\n\nThe Compact is boring. It holds meetings. It circulates position papers. It conducts surveys. It maintains a database of economic conditions across tiers. It is the kind of organization that people who want revolution dismiss as useless and people who hold power watch with quiet concern, because boring organizations that build coalitions are historically more dangerous than exciting ones that build barricades.",
    ideology: "Economic justice within the corporate-sovereign framework is achievable through organized political pressure. The Compact is explicitly reformist — it works within the system because the alternative is civil war, and civil war in GLMZ would destroy the people it's supposed to save.",
    territory: "The Compact's headquarters, Coalition House, is in the Circuit. Member organizations operate across all districts.",
    leadership: "Coordinator Tomoko Reyes-Baptiste, a former policy analyst who has spent twenty years building the relationships that hold the Compact together. She is patient, strategic, and entirely uninterested in personal recognition.",
    methods: [
      "Coalition building across labor, community, and business organizations",
      "Economic policy development and advocacy",
      "Political lobbying through the Meridian Quorum",
      "Public education about economic conditions and alternatives",
      "Data collection and analysis on economic inequality",
      "Coordinated campaigns on specific policy demands"
    ],
    resources: [
      "20,000 members across diverse organizations",
      "Coalition House headquarters in the Circuit",
      "A policy team producing professional-quality economic analysis",
      "Relationships across the political spectrum",
      "Tomoko Reyes-Baptiste's two decades of coalition-building experience",
      "The collective resources of member organizations"
    ],
    goals: [],
    relationships: [
      { name: "The United Workers Front", type: "ally", description: "The UWF is the Compact's largest member organization. The alliance gives the Compact labor muscle and gives the UWF a political platform beyond workplace issues.", tags: ["political", "labor"] }
    ],
    narrative_function: "The Compact represents the argument that boring, methodical, coalition-based organizing is the only thing that actually changes systems — and the question of whether patience is a virtue or a luxury the desperate can't afford.",
    story_hooks: [
      "The Compact has enough political support to force a vote in the Meridian Quorum on augmentation access reform. If the vote passes, it would be the first time organized citizens have overridden corporate preference. Every CorpoNation in the city is mobilizing to stop it.",
      "Tomoko has been offered a seat on the Meridian Quorum — the first time a reform advocate has been invited into governance. Accepting means influence. It also means co-option. The Compact's members are divided."
    ],
    tags: ["faction", "political", "coalition", "reform", "economic", "circuit", "labor"]
  },

  {
    name: "The Neural Liberation Front",
    aliases: ["NLF", "Neural Lib", "The Liberators"],
    motto: "Your mind is not their market.",
    description: "The Neural Liberation Front is a radical activist group of approximately 500 members that campaigns against what they call 'neural colonialism' — the corporate practice of using BCI technology to collect, analyze, and monetize human thought patterns, emotional responses, and cognitive data. In GLMZ, where BCIs are ubiquitous and corporate firmware monitors everything from attention patterns to emotional states, the NLF argues that humanity's last private space — the mind itself — has been colonized for profit.\n\nThe NLF's activism ranges from public protest to direct action. On the legal end, they file lawsuits challenging corporate neural data collection, publish reports on the extent of BCI surveillance, and advocate for 'neural privacy' legislation. On the illegal end, they develop and distribute 'liberation firmware' — unauthorized BCI modifications that block corporate data collection while maintaining the BCI's functionality. This firmware is popular, effective, and a felony to install under GLMZ's augmentation licensing laws.\n\nThe NLF is small but technically sophisticated. Its members include BCI engineers, neuroscientists, and privacy advocates who understand the technology well enough to fight it. They are hunted by corporate security (their liberation firmware costs the BCI industry an estimated Φ200 million annually in lost data revenue) and beloved by citizens who install the firmware and experience, for the first time, the sensation of thinking without being watched.",
    ideology: "Neural privacy is the last human right. Corporate collection of cognitive data through BCI technology is the most intimate violation of privacy in human history — more invasive than surveillance, more controlling than censorship, more dehumanizing than any physical intrusion. The mind must be free.",
    territory: "No permanent facilities. The NLF operates from rotating safe houses and encrypted networks.",
    leadership: "A figure known as 'Root' coordinates NLF operations. Root is believed to be a former corporate BCI architect whose identity is protected by the most sophisticated anonymity protocols the NLF can build.",
    methods: [
      "Development and distribution of liberation firmware blocking corporate neural data collection",
      "Legal challenges to corporate neural data practices",
      "Public reports on the extent of BCI surveillance",
      "Direct action against BCI data collection infrastructure",
      "Education about neural privacy through underground channels",
      "Advocacy for neural privacy legislation"
    ],
    resources: [
      "500 technically sophisticated members",
      "Liberation firmware that blocks corporate neural data collection",
      "Legal team specializing in neural privacy law",
      "Encrypted communication and distribution networks",
      "Root's deep knowledge of BCI architecture",
      "Growing public concern about neural privacy that drives support and recruitment"
    ],
    goals: [],
    relationships: [
      { name: "TESSERA", type: "enemy", description: "TESSERA loses more revenue to NLF liberation firmware than to any other single cause. TESSERA's security division has a standing task force dedicated to identifying and neutralizing NLF operatives.", tags: ["corporate", "tech", "conflict"] }
    ],
    narrative_function: "The NLF represents the fight for the last private space — the mind — in a world that has commodified everything else.",
    story_hooks: [
      "The NLF has discovered something in the neural data that corporate BCIs collect — a pattern that suggests the data isn't just being monitored but is being used to subtly influence thought patterns. If true, every BCI in GLMZ is a mind control device. Root needs independent verification before going public.",
      "A new version of liberation firmware has an unintended side effect: users report experiencing memories that aren't their own. The NLF doesn't know if this is a bug, a feature of the underlying BCI architecture they didn't understand, or something else entirely."
    ],
    tags: ["faction", "political", "privacy", "neural", "bci", "radical", "tech"]
  },

  {
    name: "The Reclamation Assembly",
    aliases: ["Reclamation", "The Assembly", "Land Reclaimers"],
    motto: "This ground was ours before it was theirs. It will be ours again.",
    description: "The Reclamation Assembly is a housing rights organization of approximately 10,000 members that fights against corporate displacement of lower-tier residents — the practice of 'revitalizing' (demolishing) Shelf and Circuit neighborhoods to build higher-tier commercial and residential developments. In a city where the CorpoNations own the ground and lease it to residents on terms that can be changed at quarterly review, the Assembly argues that long-term residence creates a moral (if not legal) claim to the space you've made your home.\n\nThe Assembly's tactics include legal challenges to displacement orders, organized resistance to demolition (members physically occupy buildings scheduled for demolition, daring corporate security to evict them on camera), community land trusts (pooling resources to lease land collectively, making displacement more expensive for the CorpoNations), and mutual aid for displaced families. They win some fights and lose most, but their visibility makes displacement politically costly enough that some CorpoNations choose to develop elsewhere rather than face the public relations consequences.\n\nThe Assembly is popular in the Shelf and Circuit, where displacement anxiety is constant and the Assembly's willingness to physically stand in front of a demolition crew gives tangible form to resistance that most people only feel as helplessness.",
    ideology: "Home is a right, not a privilege. People who have built lives in a place — who have raised children, buried parents, built community — have a claim to that place that supersedes corporate property rights. The Assembly's politics are rooted in the oldest human conviction: this is our home, and you can't take it.",
    territory: "Active in every Shelf and Circuit neighborhood facing displacement. The Assembly's headquarters is a community center in the Shelf that has itself been the subject of three displacement attempts.",
    leadership: "Organizer-in-Chief Abigail Nakamura-Okonkwo, a Shelf resident who became an activist when her own building was condemned for 'structural remediation' (demolition for a corporate parking facility). She stopped the demolition. She's been stopping them ever since.",
    methods: [
      "Legal challenges to corporate displacement orders",
      "Physical occupation of buildings scheduled for demolition",
      "Community land trusts pooling resources against displacement",
      "Mutual aid for displaced families",
      "Media campaigns documenting displacement impacts",
      "Political lobbying for housing rights legislation"
    ],
    resources: [
      "10,000 members with deep community connections",
      "Legal team specializing in housing and property law",
      "A network of community organizers across the Shelf and Circuit",
      "Media relationships that ensure displacement stories get coverage",
      "Abigail Nakamura-Okonkwo's organizational skill and moral authority",
      "The willingness of members to physically resist displacement"
    ],
    goals: [],
    relationships: [],
    narrative_function: "The Assembly represents the most basic political claim: the right to stay where you are. In a city built on displacement, this is radical.",
    story_hooks: [
      "Axiom has announced the largest displacement project in GLMZ history — the demolition of an entire Shelf sector to build a new corporate campus. The Assembly is organizing the largest resistance action in its history. The players are asked to choose a side.",
      "An Assembly community land trust has been infiltrated by a corporate agent who is systematically sabotaging the trust's legal standing. Abigail suspects someone on the inside but can't prove it without risking the trust."
    ],
    tags: ["faction", "political", "housing", "displacement", "shelf", "circuit", "community"]
  }
];

// ============================================================================
// MERCENARY / SECURITY ORGANIZATIONS
// ============================================================================

const mercenary = [
  {
    name: "Ironclad Solutions",
    aliases: ["Ironclad", "IS", "The Clads"],
    motto: "Professional force. Guaranteed outcomes.",
    description: "Ironclad Solutions is the largest private military company operating in GLMZ — a legitimate (or as legitimate as anything gets) corporate security contractor with approximately 2,000 employees providing armed security, tactical operations, executive protection, and military consulting to any client who can afford their rates. Ironclad's operators are former military, former corporate security, and former Arcturus Defense Solutions personnel who left official service for better pay and fewer rules.\n\nIronclad operates in the space between corporate security forces (which answer to their parent CorpoNations) and criminal muscle (which answers to whoever is paying today). They are neither. They are a professional military force that answers to a contract, and when the contract says 'protect this facility,' 'secure this shipment,' or 'eliminate this threat,' Ironclad delivers with the clinical efficiency of a surgical instrument. They do not do politics. They do not do ideology. They do the job, they do it well, and they send an invoice.\n\nThe company's reputation rests on two pillars: competence and neutrality. Ironclad operators are among the best-trained fighters in GLMZ, equipped with military-grade hardware and augmented for combat. And Ironclad will work for anyone — including clients whose interests oppose other Ironclad clients, as long as the contracts don't directly conflict. This neutrality makes them trusted by parties who trust no one else, and feared by everyone.",
    ideology: "Professionalism is the only principle. Ironclad has no political position, no moral stance, and no loyalty beyond the contract. This is simultaneously their greatest strength and the reason they will never be anything more than a very effective tool for whoever holds the checkbook.",
    territory: "Ironclad's headquarters and training facility is a compound in the Circuit. Their operators deploy throughout GLMZ and, occasionally, beyond the city.",
    leadership: "CEO Colonel (ret.) Viktor Johansson-Osei, a former Arcturus Defense Solutions battalion commander who founded Ironclad after retiring from corporate military service. He runs the company like a military unit: discipline, hierarchy, and performance reviews.",
    methods: [
      "Armed security for corporate facilities, events, and personnel",
      "Tactical operations including assault, defense, and extraction",
      "Executive protection for high-value individuals",
      "Security consulting and vulnerability assessment",
      "Training services for corporate security forces",
      "Deniable operations for clients who need distance from the action"
    ],
    resources: [
      "2,000 trained military operators",
      "Military-grade weapons, vehicles, and equipment",
      "Combat augmentations across the operator force",
      "A training compound with live-fire facilities",
      "Colonel Johansson-Osei's military experience and corporate connections",
      "A reputation for reliability that generates continuous contract flow"
    ],
    goals: [],
    relationships: [
      { name: "Arcturus Defense Solutions", type: "complicated", description: "Many Ironclad operators are former Arcturus. The two organizations compete for security contracts but also cooperate when Arcturus needs deniable capacity. The relationship is professional, competitive, and occasionally tense.", tags: ["military", "corporate"] }
    ],
    narrative_function: "Ironclad represents the privatization of violence — competent, professional, and available to the highest bidder, which is always the question with mercenaries: what happens when someone bids higher?",
    story_hooks: [
      "Ironclad has been hired by both sides of an impending corporate conflict — two contracts that don't technically conflict until the shooting starts. Colonel Johansson-Osei knows and doesn't care. His operators do.",
      "An Ironclad team was deployed on a classified contract and hasn't returned. The Colonel needs them found, but the contract prevents him from telling anyone where they went or what they were doing."
    ],
    tags: ["faction", "mercenary", "military", "security", "corporate", "circuit", "professional"]
  },

  {
    name: "The Vagrant Compact",
    aliases: ["Vagrants", "The Compact", "VC"],
    motto: "No masters. No flags. Just the next job.",
    description: "The Vagrant Compact is a freelance operator collective — a loose association of approximately 80 independent mercenaries, runners, fixers, and specialists who have banded together for mutual benefit without surrendering their independence. The Compact provides its members with three things no freelancer can reliably obtain alone: vetted job postings, legal support, and backup when a job goes sideways.\n\nMembers of the Compact pay dues, maintain a professional standard (jobs that create unnecessary civilian casualties get you expelled), and have access to the Compact's job board — a curated list of contracts from clients who have been vetted for payment reliability. When a Compact member takes a job, they can call on other members for assistance, knowing that the people who show up have been vetted too. This system creates a network of reliable freelancers in a market full of unreliable ones.\n\nThe Compact was founded by a group of veteran runners who were tired of being cheated by clients, betrayed by partners, and abandoned when jobs went wrong. It is, essentially, a union for people who shoot things for a living. The irony is not lost on anyone.",
    ideology: "Independence with solidarity. The Compact's members are fiercely independent operators who have recognized that collective organization doesn't require hierarchy or ideology — just shared standards and mutual support.",
    territory: "The Compact maintains a meeting space called 'the Bench' in the Circuit — a bar and lounge where members gather, negotiate, and swap stories. It is, unofficially, the freelance operator community's living room.",
    leadership: "No formal leader. The Compact is managed by a rotating board of three members called 'the Docket' who curate the job board, resolve disputes, and manage finances. Current Docket members rotate annually.",
    methods: [
      "Curated job board connecting vetted freelancers with vetted clients",
      "Backup services — members can call on each other during operations",
      "Legal support for members facing prosecution or contract disputes",
      "Reputation management — the Compact vouches for its members",
      "Dispute resolution between members and between members and clients",
      "Information sharing about dangerous clients, bad contracts, and operational hazards"
    ],
    resources: [
      "80 independent freelance operators with diverse specialties",
      "The Bench — a gathering space and informal headquarters",
      "A vetted job board with reliable contract flow",
      "Legal support infrastructure",
      "Collective reputation that attracts better-paying clients",
      "Information network spanning the freelance operator community"
    ],
    goals: [],
    relationships: [],
    narrative_function: "The Compact represents the gig economy at its most literal and most armed — freelancers who organized because the alternative is being exploited individually.",
    story_hooks: [
      "A Compact member has been killed on a job — but the job wasn't what it appeared to be. The contract was a setup, and the Docket suspects the client was testing Compact security for a larger attack on the organization.",
      "The Bench has been identified by Axiom Security as a gathering point for 'unlicensed security operators.' A raid is planned. The Compact needs to relocate or fight, and the Docket is split."
    ],
    tags: ["faction", "mercenary", "freelance", "runners", "circuit", "collective", "operator"]
  },

  {
    name: "The Dead Ledger",
    aliases: ["Dead Ledger", "The Ledger", "Bounty Board"],
    motto: "A name on the Ledger always gets crossed off.",
    description: "The Dead Ledger is a bounty hunter guild of approximately 60 members that operates the most reliable bounty fulfillment system in GLMZ. The guild takes its name from the ledger — originally a physical book, now a encrypted database — where bounties are posted, tracked, and resolved. When a name goes on the Ledger, one of the guild's hunters will find that person. The guild's completion rate is 94%, which in the bounty hunting business is nearly supernatural.\n\nThe Dead Ledger accepts bounties from anyone: CorpoNations seeking fugitive employees, criminal organizations hunting deserters, individuals pursuing personal vengeance, and the Meridian Quorum's justice enforcement division (which outsources fugitive apprehension because it lacks the resources to do it in-house). The guild does not judge the morality of a bounty — it verifies the bounty is funded, assigns a hunter based on the target's profile, and waits for results.\n\nThe guild's hunters are specialists in finding people who don't want to be found. Each hunter has their own methods: some are trackers who follow physical and digital trails, some are social engineers who manipulate the target's connections into revealing their location, some are brute-force operators who kick in doors until the right person falls out. The guild's strength is not any single hunter but the collective capability of a group that includes every approach to finding a human being that exists.",
    ideology: "The hunt is the contract. The Dead Ledger's only principle is fulfillment — when a bounty is accepted, it is completed. This reliability is the guild's entire business model and the source of its power.",
    territory: "The guild maintains a discreet office in the Circuit called 'the Registry.' Hunters operate wherever their targets lead them.",
    leadership: "The Clerk — a woman named Ingrid Volkov-Yamamoto — manages the Ledger, assigns bounties, and mediates disputes. She has never personally hunted a target and has no interest in starting.",
    methods: [
      "Physical and digital tracking of bounty targets",
      "Social engineering to locate targets through their connections",
      "Surveillance and stakeout operations",
      "Forced apprehension — capture and delivery of targets",
      "Negotiated surrender — some hunters prefer to talk targets in rather than fight them",
      "Collaboration between hunters on difficult targets"
    ],
    resources: [
      "60 skilled bounty hunters with diverse specialties",
      "The Ledger — a comprehensive bounty tracking and management system",
      "The Registry — a discreet operations center",
      "A 94% completion rate that attracts high-value bounties",
      "Ingrid Volkov-Yamamoto's administrative precision",
      "Relationships with clients across the legal and criminal spectrum"
    ],
    goals: [],
    relationships: [],
    narrative_function: "The Dead Ledger represents consequence in a city full of escape — the certainty that running doesn't work when the people chasing you are professionals.",
    story_hooks: [
      "A bounty has been posted on the Ledger for a target that the hunters recognize — a former guild member who left under unclear circumstances. The bounty is the highest the guild has ever seen, and every hunter wants it. Ingrid suspects the bounty itself is the weapon.",
      "A target the Dead Ledger delivered to a client six months ago has been found dead — tortured, experimented on, and discarded. The guild's reputation depends on not caring what happens after delivery, but this case is making hunters reconsider that policy."
    ],
    tags: ["faction", "mercenary", "bounty hunter", "tracking", "circuit", "professional"]
  },

  {
    name: "Prism Security Group",
    aliases: ["Prism", "PSG", "The Prism"],
    motto: "Every angle covered. Every threat visible.",
    description: "Prism Security Group is a mid-size private security firm of approximately 600 employees specializing in electronic surveillance, counter-surveillance, and information security. While Ironclad Solutions sells muscle, Prism sells eyes — the ability to see threats before they materialize and to make clients invisible to threats they can't prevent. In a city saturated with surveillance, Prism's business is ensuring that the surveillance works for their clients rather than against them.\n\nPrism's client list includes CorpoNations seeking protection from corporate espionage, wealthy individuals seeking privacy from the Wire Taps and their competitors, criminal organizations seeking counter-surveillance against corporate and municipal monitoring, and political organizations seeking secure communications. This diverse client base means Prism is frequently protecting one client's secrets while another client is trying to steal them — a situation the firm manages through strict information compartmentalization and a policy of never, ever telling one client what they know about another.\n\nThe firm's technical capabilities are exceptional. Prism's electronic warfare suite can detect surveillance equipment at the molecular level, their counter-intrusion systems can identify network penetration attempts that most corporate security teams would miss, and their 'clean room' services — debugging a space of all surveillance devices — are the gold standard in GLMZ.",
    ideology: "Security is information management. The organization that controls what is seen and what remains hidden controls the outcome. Prism's philosophy is entirely practical: they sell the ability to see and the ability to hide.",
    territory: "Prism's headquarters is a hardened facility in the Laceworks. They operate mobile teams throughout GLMZ.",
    leadership: "Director Yael Chen-Nakamura, a former Axiom Security intelligence analyst who founded Prism after recognizing that the private market for counter-surveillance would be more lucrative — and more interesting — than corporate employment.",
    methods: [
      "Electronic surveillance detection and removal",
      "Counter-surveillance and privacy protection",
      "Network security and counter-intrusion",
      "Secure communication system deployment",
      "Clean room debugging services",
      "Intelligence analysis and threat assessment"
    ],
    resources: [
      "600 employees with electronic warfare and information security expertise",
      "State-of-the-art surveillance detection and counter-intrusion technology",
      "A hardened headquarters facility in the Laceworks",
      "Mobile teams capable of rapid deployment",
      "Yael Chen-Nakamura's intelligence community connections",
      "A diverse client list providing financial stability and operational intelligence"
    ],
    goals: [],
    relationships: [],
    narrative_function: "Prism represents the arms race between surveillance and privacy — the reality that in a watched city, the ability to watch back is its own form of power.",
    story_hooks: [
      "Prism has detected a new surveillance technology deployed across GLMZ that they can't identify or trace. It's not corporate. It's not municipal. It's not criminal. And it's watching everything.",
      "A Prism client has been assassinated despite full counter-surveillance protection. The breach came from inside Prism — someone on the team sold access. Director Chen-Nakamura needs to find the mole before the next client dies and the firm's reputation collapses."
    ],
    tags: ["faction", "mercenary", "security", "surveillance", "counter-intel", "laceworks", "electronic warfare"]
  },

  {
    name: "The Daybreak Network",
    aliases: ["Daybreak", "The Network", "Sunrise Runners"],
    motto: "Get in. Get the package. Get out before dawn.",
    description: "The Daybreak Network is a runner network — an organized group of approximately 120 couriers, infiltrators, and extraction specialists who move things (objects, data, people) from Point A to Point B in situations where normal logistics aren't an option. Too dangerous for a courier service, too delicate for a criminal gang, too illegal for a security firm — that's Daybreak's niche.\n\nThe Network operates on a cellular model: small teams of two to four runners who specialize in different types of operations. 'Ghosts' handle infiltration and extraction from secure facilities. 'Sparks' handle electronic payloads — data transfers that need physical transport because network transmission would be detected. 'Shepherds' handle people — escorting VIPs, witnesses, or fugitives through dangerous territory. 'Mules' handle physical cargo — objects too valuable, too dangerous, or too illegal for any other transport method.\n\nDaybreak's reputation is built on discretion and reliability. They don't ask what they're carrying. They don't ask why. They deliver, they bill, and they disappear. The Network's runners are a cross-section of GLMZ's skilled underground: former corporate couriers, ex-military pathfinders, augmented athletes who discovered that their skills were more valuable in the gray market than in legitimate competition.",
    ideology: "Deliver the package. Everything else is someone else's problem. The Daybreak Network's philosophy is radical simplicity: they do one thing, they do it well, and they don't complicate it with questions they don't need answered.",
    territory: "No fixed base. Daybreak runners operate from personal locations and meet at rotating drop points. The Network's communication infrastructure is built for mobility.",
    leadership: "A coordinator known as 'Dispatch' — no relation to the Wire Taps' Dispatch — manages job intake, team assignment, and payment. Dispatch's identity is known only to team leaders.",
    methods: [
      "Infiltration and extraction from secure facilities",
      "Physical data transport bypassing network surveillance",
      "VIP and witness escort through dangerous territory",
      "Cargo transport for high-value or high-risk items",
      "Route planning through urban and Underworld environments",
      "Rapid response — Daybreak can deploy teams within an hour of contract"
    ],
    resources: [
      "120 skilled runners with diverse specialties",
      "A communication and dispatch system designed for rapid deployment",
      "Intimate knowledge of GLMZ's transit systems, maintenance corridors, and Underworld routes",
      "Equipment caches at strategic locations",
      "Dispatch's organizational efficiency",
      "A reputation for reliable delivery that generates premium contract rates"
    ],
    goals: [],
    relationships: [],
    narrative_function: "Daybreak represents the human infrastructure that connects GLMZ's various factions — the neutral logistics network that keeps the gray economy moving.",
    story_hooks: [
      "A Daybreak team has been ambushed and their cargo taken. The cargo was a sealed container that the team was told contained data storage. It didn't. What was inside is something that changes the nature of the job from delivery to survival.",
      "Dispatch has received a contract from someone claiming to be a dead person. The delivery address is a location in the Underworld that doesn't appear on any map. The pay is ten times the normal rate."
    ],
    tags: ["faction", "mercenary", "runner", "courier", "logistics", "infiltration", "network"]
  },

  {
    name: "The Furnace",
    aliases: ["Furnace Crew", "The Furnace", "FC"],
    motto: "We build what you need and break what you don't.",
    description: "The Furnace is a collective of approximately 40 combat engineers and technical specialists who hire out for demolition, fortification, and technical warfare operations. In a city where most mercenary groups sell fighters, the Furnace sells the ability to reshape the physical environment: blow open a wall, fortify a position, rig a building for demolition, build a defensive emplacement, disable infrastructure, or construct improvised devices for specific tactical purposes.\n\nThe collective emerged from the construction and demolition industry — former blast technicians, structural engineers, electricians, and mechanics who discovered that their skills were worth more in the security market than in legitimate construction. Furnace operators work alongside other mercenary groups, providing the technical support that pure combat operators can't: breaching charges placed precisely, communications equipment installed under fire, defensive positions built from available materials, and the occasional improvised weapon that turns the tide of a fight.\n\nThe Furnace is small and specialized, but their reputation for technical excellence makes them a force multiplier that larger organizations pay premium rates to access. A team with Furnace support is qualitatively different from a team without it — they can go through walls instead of around them, can hold positions that should be indefensible, and can deploy technical solutions to problems that bullets alone can't solve.",
    ideology: "Engineering wins fights. The Furnace believes that technical capability is the most undervalued asset in GLMZ's security market and that the person who controls the physical environment controls the outcome of any conflict within it.",
    territory: "A workshop in the lower Circuit that serves as headquarters, fabrication facility, and armory. Furnace teams deploy wherever clients need them.",
    leadership: "Master Sapper Kofi Petersen-Volkov, a former Crucible Industries demolition specialist whose understanding of structural dynamics is considered the best in the private sector.",
    methods: [
      "Breaching operations — defeating physical barriers for entry",
      "Fortification — building defensive positions from available materials",
      "Demolition — controlled destruction of structures and infrastructure",
      "Technical warfare — deploying improvised devices and electronic countermeasures",
      "Infrastructure disruption — disabling utilities, communications, and transportation",
      "Construction services for clients needing hardened facilities"
    ],
    resources: [
      "40 combat engineers with diverse technical specialties",
      "A fabrication workshop with industrial-grade tools and materials",
      "Explosive and demolition equipment",
      "Electronic warfare and countermeasure capabilities",
      "Kofi Petersen-Volkov's structural engineering expertise",
      "A reputation as the best technical support in the mercenary market"
    ],
    goals: [],
    relationships: [
      { name: "Ironclad Solutions", type: "partner", description: "Ironclad frequently subcontracts the Furnace for operations requiring technical support. The relationship is professional and mutually profitable.", tags: ["mercenary", "technical"] }
    ],
    narrative_function: "The Furnace represents the unglamorous but decisive role of technical capability in conflict — the reality that the person who can reshape the battlefield matters as much as the person shooting.",
    story_hooks: [
      "The Furnace has been hired to build something they've never built before — a client wants a fortified position in the Underworld capable of withstanding a corporate military assault. The specifications suggest the client expects a war, and the Furnace wants to know what kind before they accept the contract.",
      "A Furnace demolition job went wrong — not because of technical failure but because the building wasn't empty. Someone provided false intelligence, and the Furnace killed people who shouldn't have been there. Kofi wants to know who set them up."
    ],
    tags: ["faction", "mercenary", "engineer", "demolition", "construction", "circuit", "technical"]
  }
];

// ============================================================================
// CULTURAL / COMMUNITY ORGANIZATIONS
// ============================================================================

const cultural = [
  {
    name: "The Shelf Commons",
    aliases: ["The Commons", "Shelf Aid", "SC"],
    motto: "Nobody's coming to save us. So we save each other.",
    description: "The Shelf Commons is the largest mutual aid network in GLMZ's lowest tier — a decentralized organization of approximately 12,000 participants (they refuse to call them 'members' because participation is fluid and nonbinding) who provide each other with food, shelter, medical care, childcare, elder care, repair services, and the hundreds of small daily assists that make survival in the Shelf possible.\n\nThe Commons operates through 'circles' — neighborhood-level groups of 50 to 200 people who know each other, trust each other, and take care of each other. Each circle maintains a shared resource pool: food stores, tool libraries, medical supplies, spare augmentation parts, clothing, and whatever else the community has in surplus. When someone needs something, they ask their circle. When a circle needs something, they ask neighboring circles. The system runs on reciprocity rather than currency — you contribute what you can, you take what you need, and the social bonds of the community enforce the balance.\n\nThe Commons has no political agenda beyond survival. It is not a resistance movement, not a reform campaign, not a revolution in waiting. It is the practical reality of poor people keeping each other alive in a city that has decided they're not worth the investment. This simplicity is its strength: the Commons persists because it works, and it works because it asks nothing of its participants except that they help when they can and accept help when they need it.",
    ideology: "Mutual aid is survival. The CorpoNations won't help us. The government works for the CorpoNations. The only resource we have is each other. The Commons' philosophy is not ideological — it's the lived experience of people who have learned that waiting for institutional help means dying.",
    territory: "Throughout the Shelf, organized by neighborhood circles. No central headquarters — the network is distributed by design.",
    leadership: "No formal leadership. Circle coordinators manage local logistics, and a loose network of 'connectors' maintains relationships between circles. The most respected figure in the Commons is a woman named Mama Obi — not a title, just what everyone calls her — who has been coordinating circles for twenty-five years.",
    methods: [
      "Neighborhood circles providing mutual aid at the local level",
      "Shared resource pools — food, tools, medical supplies, augmentation parts",
      "Reciprocal exchange — contribute what you can, take what you need",
      "Community health volunteers providing basic medical care",
      "Childcare and elder care cooperatives",
      "Skill-sharing — circles teach each other repair, cooking, medical, and survival skills"
    ],
    resources: [
      "12,000 participants across hundreds of neighborhood circles",
      "Distributed resource pools throughout the Shelf",
      "Twenty-five years of institutional knowledge about community survival",
      "Community health volunteers with basic medical training",
      "Mama Obi's relationship network spanning the entire Shelf",
      "The trust of the Shelf's poorest communities — an asset no amount of money can buy"
    ],
    goals: [],
    relationships: [],
    narrative_function: "The Commons represents the most fundamental form of community — people taking care of each other because nobody else will. In a world of ideology and ambition, it is simply human.",
    story_hooks: [
      "Axiom has announced a 'Community Enhancement Initiative' in the Shelf — free services, improved infrastructure, corporate goodwill. The Commons recognizes it as a displacement precursor and needs to organize resistance without alienating residents who are attracted by the promised improvements.",
      "A circle has discovered that someone has been poisoning their shared food supply — not to kill, but to sicken, in a pattern that suggests someone is testing something. The circle needs to identify the source without causing panic."
    ],
    tags: ["faction", "community", "mutual aid", "shelf", "survival", "neighborhood", "grassroots"]
  },

  {
    name: "The Heritage Vault",
    aliases: ["The Vault", "Heritage", "Cultural Vault"],
    motto: "Before we were citizens, we were peoples. We remember.",
    description: "The Heritage Vault is a cultural preservation organization of approximately 4,000 members dedicated to maintaining the cultural traditions, languages, cuisines, art forms, and historical memories of the Ubiquitous Diaspora's scattered peoples. In GLMZ, where the population is a blend of every human culture that survived the upheavals that created the city, individual cultural traditions are at constant risk of dissolution — absorbed into a homogenized urban culture that speaks one language, eats the same food, and remembers nothing before Meridian.\n\nThe Vault operates cultural centers — called 'Vaults' — in every district, each maintained by community groups dedicated to specific cultural traditions. A single Vault building might house a Yoruba language school, a Japanese tea ceremony practice, a Mexican culinary preservation kitchen, a Maori carving workshop, and a Romani music archive, all under one roof. The centers are spaces where the grandchildren of the Diaspora learn the things their grandparents knew — things that have no economic value in GLMZ's market but immeasurable value in the human sense of knowing where you come from.\n\nThe Vault also maintains a comprehensive digital archive of cultural artifacts, oral histories, traditional knowledge, and genealogical records. For people whose families were scattered across the globe and reassembled in GLMZ's melting pot, the Vault's genealogical database is often the only way to trace their heritage — to discover that their grandmother's grandmother came from a specific village, practiced a specific craft, spoke a specific language that nobody speaks anymore.",
    ideology: "Cultural memory is human survival. When a people forget where they came from, they lose the ability to imagine where they're going. The Vault exists because homogenization is not unity — it's erasure, and what is erased cannot be recovered.",
    territory: "Cultural centers in every district. The largest Vault is in Old Harbor, where the earliest Diaspora communities settled.",
    leadership: "Curator-General Dr. Amina Johansson-Okafor, a cultural anthropologist who has dedicated her career to the proposition that the Ubiquitous Diaspora's diversity is a strength that must be actively preserved.",
    methods: [
      "Cultural centers offering language, art, cuisine, and craft preservation programs",
      "Digital archiving of cultural artifacts, oral histories, and traditional knowledge",
      "Genealogical research services connecting individuals to their heritage",
      "Cultural festivals celebrating Diaspora traditions",
      "Partnership with schools to integrate cultural education",
      "Oral history collection from elder community members"
    ],
    resources: [
      "4,000 members and many more community participants",
      "Cultural centers in every district",
      "A comprehensive digital archive of cultural materials",
      "Genealogical database tracing Diaspora heritage",
      "Dr. Johansson-Okafor's academic connections and cultural expertise",
      "The trust of elder community members who share their knowledge"
    ],
    goals: [],
    relationships: [],
    narrative_function: "The Vault represents the fight against cultural erasure — the conviction that in a city designed to make everyone the same, remembering who you were is an act of resistance.",
    story_hooks: [
      "The Vault's digital archive has been hacked — not to steal data but to insert false records. Someone is rewriting the cultural history of specific Diaspora communities. The changes are subtle, expert, and terrifying in their implications.",
      "An elder community member has brought the Vault an artifact from their homeland — an object that shouldn't exist, that doesn't match any known cultural tradition, and that several organizations are very interested in acquiring."
    ],
    tags: ["faction", "cultural", "heritage", "diaspora", "preservation", "archive", "community"]
  },

  {
    name: "The Underlayer Collective",
    aliases: ["Underlayer", "The Collective", "Underground Art"],
    motto: "Art is the thing they can't sell you because you already own it.",
    description: "The Underlayer Collective is an underground art movement of approximately 300 active artists and several thousand supporters who produce and distribute art outside the corporate-controlled cultural channels that Vantablack Media dominates. In GLMZ, where most media is produced by or for CorpoNations, the Underlayer exists to make art that no one commissioned, no one approved, and no one can profit from — art that exists because someone needed to make it.\n\nThe Collective's output spans every medium: graffiti that transforms Shelf walls into galleries, music produced in bedroom studios and distributed through pirate networks, theater performed in abandoned buildings for audiences who pay what they can, literature printed on hand-operated presses and left in public spaces, sculpture assembled from industrial waste and installed in empty lots, and BCI-native art — experiences designed for augmented perception that can't be reproduced in any other medium.\n\nThe Underlayer is not politically organized and resists attempts to turn it into a movement. Individual artists have political views — many are deeply critical of corporate culture — but the Collective itself claims no ideology beyond the conviction that art matters, that it should be accessible, and that the CorpoNations' monopoly on culture is as dangerous as their monopoly on anything else. This political ambiguity frustrates allies who want the Collective to be a resistance tool, but it also protects it — Vantablack Media can't suppress a movement that doesn't have demands to refuse.",
    ideology: "Art is a human need, not a product. The Collective believes that corporate control of cultural production creates a spiritual poverty more damaging than economic poverty — a world where every image, every song, every story is designed to sell something. The Underlayer exists to make things that aren't for sale.",
    territory: "Everywhere and nowhere. Underlayer art appears across GLMZ, with concentrations in the Shelf and Circuit. The Collective has no permanent space — it uses temporary venues, public spaces, and abandoned buildings.",
    leadership: "No formal leadership. The Collective is organized through social networks and shared aesthetic values. The most influential figure is a muralist known only as 'Primer' whose Shelf murals have become landmarks.",
    methods: [
      "Graffiti and street art installation",
      "Underground music production and distribution",
      "Theater and performance in temporary venues",
      "Print literature distributed for free in public spaces",
      "BCI-native art experiences",
      "Art shows in abandoned buildings and public spaces"
    ],
    resources: [
      "300 active artists and thousands of supporters",
      "Distributed production capability — studios, presses, performance spaces",
      "Pirate distribution networks for music and literature",
      "Public goodwill — Underlayer art is beloved in the lower tiers",
      "Primer's iconic murals and cultural influence",
      "The ability to produce meaning in a culture designed to sell it"
    ],
    goals: [],
    relationships: [],
    narrative_function: "The Underlayer represents the survival of authentic expression in a commodified world — the stubborn human insistence on making things that aren't products.",
    story_hooks: [
      "Primer's latest mural has gone viral through BCI networks — an image so striking that millions of people have seen it. Vantablack Media wants to commercialize it. Several factions want to weaponize its message. Primer has disappeared, leaving behind a mural that is changing the conversation in ways nobody can control.",
      "A Collective member has created a BCI art experience that is causing permanent changes in viewers — subtle shifts in perception that persist after the experience ends. The changes are benign. The implications are not."
    ],
    tags: ["faction", "cultural", "art", "underground", "shelf", "circuit", "creative"]
  },

  {
    name: "The Meridian Mavericks",
    aliases: ["Mavericks", "The Mavs", "MM"],
    motto: "Faster. Harder. More chrome. That's entertainment.",
    description: "The Meridian Mavericks are GLMZ's most popular augmented sports franchise — a professional combat sports organization that stages legal (barely) augmented fighting, racing, and multi-event competitions in venues ranging from a 50,000-seat arena in the Laceworks to street-level exhibition matches in the Circuit. With approximately 200 athletes, 500 support staff, and millions of fans, the Mavericks are less a faction and more a cultural institution — the closest thing GLMZ has to a shared civic identity that isn't corporate branding.\n\nThe Mavericks' flagship event — the Meridian Grand Prix — is an annual augmented combat tournament that draws participants from across the hemisphere and viewers from around the world. Fighters compete in weight and augmentation classes, from 'natural class' (minimal augmentation) to 'open class' (anything goes, signed waivers required). The fights are brutal, technically fascinating, and the single most-watched entertainment event in GLMZ. Betting on the Grand Prix generates more Φ in a single weekend than most Shelf neighborhoods see in a year.\n\nBeneath the spectacle, the Mavericks are a business — and a ruthless one. Athletes are signed to contracts that control their augmentation choices, media appearances, and personal brand. Injuries are managed for narrative drama rather than athlete welfare. The organization works closely with Crucible Industries for performance augmentations and with Vantablack Media for broadcast rights. The Mavericks are everything that's exciting and everything that's exploitative about GLMZ, packaged for maximum entertainment value.",
    ideology: "Entertainment is the great unifier. In a city divided by tier, ideology, and corporate loyalty, the Mavericks give everyone something to cheer for together. Whether this unity is genuine community or manufactured distraction is a question the Mavericks prefer not to answer.",
    territory: "The Maverick Arena in the Laceworks seats 50,000. Training facilities in the Circuit. Exhibition venues throughout the city.",
    leadership: "Commissioner Dara Osei-Nakamura, a former athlete turned executive who runs the Mavericks with the showmanship of a carnival barker and the ruthlessness of a corporate CEO.",
    methods: [
      "Professional augmented combat sports events",
      "The Meridian Grand Prix annual tournament",
      "Street-level exhibition matches for community engagement",
      "Athlete development and augmentation programs",
      "Media production in partnership with Vantablack Media",
      "Gambling operations (legal and otherwise)"
    ],
    resources: [
      "200 professional athletes",
      "500 support staff",
      "The Maverick Arena — a 50,000-seat entertainment complex",
      "Broadcast partnership with Vantablack Media",
      "Augmentation partnership with Crucible Industries",
      "Millions of fans providing cultural influence and revenue",
      "Commissioner Osei-Nakamura's showmanship and business acumen"
    ],
    goals: [],
    relationships: [
      { name: "Crucible Industries", type: "sponsor", description: "Crucible provides performance augmentations for Maverick athletes and uses the franchise as a showcase for their products. The relationship is worth billions of Φ to both parties.", tags: ["corporate", "entertainment"] },
      { name: "Vantablack Media", type: "partner", description: "Vantablack holds exclusive broadcast rights for Maverick events. The media partnership shapes how the fights are presented and how athletes are portrayed.", tags: ["corporate", "media"] }
    ],
    narrative_function: "The Mavericks represent bread and circuses — the question of whether shared entertainment builds genuine community or is the most effective tool of social control ever invented.",
    story_hooks: [
      "A Maverick fighter has died during a sanctioned match — not from combat injuries but from augmentation failure. The fighter's augmentations were Crucible prototypes that weren't approved for competitive use. The Commissioner wants it covered up. The dead fighter's training partner wants the truth.",
      "A street-level exhibition match in the Shelf has been rigged by a gambling syndicate. The fighter who was supposed to lose won instead, and now there are bodies. The Mavericks need the situation contained before it reaches the broadcast media."
    ],
    tags: ["faction", "cultural", "sports", "entertainment", "augment", "laceworks", "circuit", "combat"]
  },

  {
    name: "The Patchwork Kitchen",
    aliases: ["Patchwork", "The Kitchen", "PK"],
    motto: "At this table, you're not a tier number. You're hungry, and there's food.",
    description: "The Patchwork Kitchen is a network of community kitchens — 23 locations across the Shelf, Circuit, and Old Harbor — that provide free or pay-what-you-can meals to anyone who walks in. Founded sixteen years ago by a retired chef named Abuela Lucia Cortez-Obi (everyone calls her Abuela, regardless of whether they're related), the Patchwork Kitchen began as a single pot of stew served from a Shelf apartment and has grown into the largest community feeding program in GLMZ.\n\nThe kitchens serve 8,000 meals a day — breakfast, lunch, and dinner — prepared by a rotating staff of 400 volunteers who cook in donated facilities using ingredients sourced from urban farms, food rescue operations, and donations from sympathetic businesses. The food is not institutional slop. Abuela's culinary standards are non-negotiable: every meal is prepared with the same care she'd give a plate in the fine dining restaurants where she once worked. This insistence on quality is central to the Kitchen's philosophy: dignity is not a luxury, and feeding people well is not the same as just feeding them.\n\nThe Patchwork Kitchen is politically neutral — Abuela will feed anyone, including corporate security officers, gang members, and the operatives of every faction in this document. Her kitchens are recognized safe zones where violence is not tolerated and grudges are checked at the door. This neutrality, maintained by Abuela's terrifying maternal authority and the practical reality that everyone in the Shelf depends on the Kitchen at some point, makes the Patchwork Kitchen the closest thing to sacred ground that the Shelf possesses.",
    ideology: "People need to eat. Everything else — politics, ideology, faction loyalty — can wait until after the meal. Abuela's philosophy is radical in its simplicity: feed people, respect them, and let the rest sort itself out.",
    territory: "23 kitchen locations across the Shelf, Circuit, and Old Harbor. The original kitchen — still operating from the same Shelf apartment, now expanded — is the network's heart.",
    leadership: "Abuela Lucia Cortez-Obi, age 72, whose authority in the Shelf transcends any faction, gang, or CorpoNation. She leads through force of personality, culinary excellence, and the absolute conviction that nobody in her kitchens will be mistreated.",
    methods: [
      "Daily meal service at 23 locations",
      "Volunteer-staffed kitchens with professional culinary standards",
      "Food sourcing from urban farms, rescue operations, and donations",
      "Kitchen neutrality — all factions welcome, no violence tolerated",
      "Culinary training for volunteers, providing job skills",
      "Community gathering — the kitchens serve as social spaces where people connect"
    ],
    resources: [
      "23 kitchen locations serving 8,000 meals daily",
      "400 volunteer cooks and kitchen staff",
      "Food sourcing networks across the lower tiers",
      "Abuela's culinary expertise and moral authority",
      "Recognition as neutral ground by virtually every faction in the Shelf",
      "The gratitude of thousands of people who would be hungry without the Kitchen"
    ],
    goals: [],
    relationships: [],
    narrative_function: "The Patchwork Kitchen is proof that the simplest forms of care are the most powerful — that feeding people with dignity is an act of resistance against a system that treats them as expendable.",
    story_hooks: [
      "Someone has poisoned the food at one of the Patchwork Kitchen locations. Twelve people are hospitalized. Abuela needs to find who did this and why before the Kitchens lose the community trust that keeps them running — and before she finds the responsible party and does something that can't be undone.",
      "A corporate development project is claiming the building that houses the original Kitchen. Abuela isn't moving. The developer has legal authority. The Shelf is watching to see which force is stronger: corporate law or a 72-year-old woman with a ladle and the loyalty of 8,000 hungry people."
    ],
    tags: ["faction", "community", "food", "mutual aid", "shelf", "circuit", "old harbor", "kitchen", "neutral"]
  },

  {
    name: "The Last Frequency Radio",
    aliases: ["Last Frequency", "LFR", "The Radio"],
    motto: "Broadcasting from the bottom. For the bottom. About the bottom.",
    description: "The Last Frequency Radio is a pirate radio and podcast collective of approximately 50 members that produces and broadcasts independent media from the Shelf — news, music, talk shows, storytelling, and community information that Vantablack Media's channels don't cover because the Shelf's population doesn't generate enough advertising revenue to justify coverage.\n\nLFR broadcasts on unlicensed frequencies and through pirate BCI feed injection (simpler and less sophisticated than Null Sermons' operations, but reaching a loyal audience of roughly 200,000 daily listeners in the Shelf and lower Circuit). Their programming includes daily news broadcasts covering Shelf events that corporate media ignores, music shows featuring Shelf artists, a call-in advice show that functions as informal community counseling, and 'The Bottom Line' — a weekly investigative program that has exposed corporate malfeasance, gang violence, and municipal corruption with the resources of a bedroom operation and the courage of people who have nothing left to lose.\n\nThe collective operates from a studio hidden in the Shelf — a room full of salvaged broadcast equipment held together with hope and solder. Their production values are rough, their scheduling is unreliable, and their coverage is biased toward the Shelf's perspective. None of this matters to their audience, who tune in because LFR is the only media outlet in GLMZ that talks about their lives as though they matter.",
    ideology: "Everyone deserves to be heard. The CorpoNations control what GLMZ knows about itself, and what they don't cover doesn't exist. LFR exists to make the Shelf exist — to report on the lives, struggles, and achievements of people the corporate media has decided aren't worth a broadcast slot.",
    territory: "A hidden studio in the Shelf. Broadcast range covers the Shelf and lower Circuit.",
    leadership: "Station Director Malik Okafor-Petersen, a former school teacher who started LFR because he realized his students had never heard their own neighborhood mentioned on any media outlet. He broadcasts under the name 'Voice of the Bottom.'",
    methods: [
      "Pirate radio broadcasting on unlicensed frequencies",
      "BCI feed injection for augmented listeners",
      "Daily news coverage of Shelf events",
      "Music programming featuring Shelf artists",
      "Investigative journalism targeting Shelf-relevant issues",
      "Community call-in programming"
    ],
    resources: [
      "50 volunteer broadcasters and journalists",
      "A hidden studio with salvaged broadcast equipment",
      "200,000 daily listeners",
      "Community trust built through years of honest reporting",
      "Malik's teaching background and community connections",
      "Relationships with Shelf community organizations who provide story leads"
    ],
    goals: [],
    relationships: [
      { name: "", type: "", description: "LFR and Null Sermons share the pirate broadcasting space but operate at different scales and with different purposes. Null Sermons occasionally provides technical assistance to LFR, recognizing a kindred spirit in the low-budget operation. LFR is grateful but nervous about the association.", tags: ["media", "broadcast"] }
    ],
    narrative_function: "LFR represents the stubborn survival of independent media — the conviction that the story of the powerless matters, even when the powerful control the microphone.",
    story_hooks: [
      "LFR's investigative program has stumbled onto a story too big for their resources — evidence of a corporate operation in the Shelf that, if true, would be the biggest scandal in GLMZ's history. They need help verifying the story before Vantablack Media either steals or kills it.",
      "Axiom Security has located LFR's studio and is planning a raid. The collective has twelve hours to relocate their operation — equipment, archives, everything — before the studio is seized."
    ],
    tags: ["faction", "cultural", "media", "radio", "broadcast", "shelf", "journalism", "community"]
  },

  {
    name: "The Circuit Makers Guild",
    aliases: ["Makers Guild", "CMG", "The Makers"],
    motto: "Why buy it when you can build it better?",
    description: "The Circuit Makers Guild is a community of approximately 2,000 makers, tinkerers, hardware hackers, and DIY engineers who share workspace, tools, knowledge, and a collective conviction that the corporate monopoly on technology production can be challenged by ordinary people with soldering irons and stubbornness. The Guild operates six 'makerspaces' — community workshops equipped with fabrication tools, 3D printers, electronics workbenches, and the accumulated knowledge of people who have been taking things apart and putting them back together better since before there was a word for it.\n\nThe Guild's output ranges from practical (repaired appliances, modified augmentations, improvised medical devices for Shelf clinics) to creative (custom electronics art, experimental BCI modifications, homemade drones) to politically significant (open-source alternatives to corporate technology, augmentation firmware modifications that bypass corporate restrictions, and the occasional improvised device that the CorpoNations would prefer didn't exist). The Guild is, in practice, the R&D department of GLMZ's underground economy.\n\nThe Guild maintains an open-door policy: anyone can join, anyone can learn, and the only requirement is a willingness to share what you know with others. This has made the makerspaces some of the most genuinely diverse spaces in GLMZ — corporate engineers sit next to Shelf tinkerers, learning from each other in a setting where expertise matters more than tier number.",
    ideology: "Technology belongs to everyone. The CorpoNations' monopoly on manufacturing and design is not natural but imposed, and every device an ordinary person builds or modifies is an act of reclaiming technological agency. The Guild's politics are expressed through practice rather than protest: don't argue about access to technology. Build it yourself.",
    territory: "Six makerspaces: three in the Circuit, two in the Shelf, one in Old Harbor. The Circuit's main makerspace, called 'the Bench,' is the Guild's social and technical hub.",
    leadership: "No formal leader. The Guild is managed by makerspace coordinators who maintain facilities and organize programming. The most respected figure is Master Maker Indira Osei-Park, a 55-year-old engineer who has mentored hundreds of makers and whose custom devices are legendary.",
    methods: [
      "Community makerspaces providing tools and workspace",
      "Skill-sharing workshops teaching technical skills to anyone interested",
      "Open-source technology development",
      "Repair services for community members who can't afford corporate prices",
      "Augmentation modification and firmware hacking",
      "Collaborative design projects addressing community needs"
    ],
    resources: [
      "2,000 makers with diverse technical skills",
      "Six equipped makerspaces with fabrication tools",
      "A culture of knowledge-sharing that produces continuous innovation",
      "Indira Osei-Park's expertise and mentorship",
      "Relationships across tiers — the Guild is genuinely cross-class",
      "A library of open-source designs and modifications"
    ],
    goals: [],
    relationships: [],
    narrative_function: "The Guild represents the democratization of technology — the proof that ordinary people, given tools and knowledge, can challenge corporate monopolies from a workbench.",
    story_hooks: [
      "A Guild member has built something extraordinary — a device that does something no commercial technology can do, using principles nobody else has figured out. CorpoNations are interested. So are less legitimate organizations. The maker just wanted to solve a problem and is in over their head.",
      "Axiom has filed a patent claim covering a design technique the Guild has been using for years. If the claim is upheld, half the Guild's projects become illegal. The Guild needs to prove prior art, but their documentation practices are, charitably, informal."
    ],
    tags: ["faction", "cultural", "makers", "tech", "diy", "circuit", "shelf", "old harbor", "community"]
  },

  {
    name: "The Remembrance Society",
    aliases: ["Remembrance", "The Society", "Memory Keepers"],
    motto: "The dead deserve witnesses. We are those witnesses.",
    description: "The Remembrance Society is a community organization of approximately 1,500 members dedicated to documenting and memorializing the lives of GLMZ residents who die without being remembered — the unclaimed dead, the undocumented, the people who fall through every system and leave no record that they existed. In a city where corporate databases define reality and the poor die without obituaries, the Remembrance Society insists on the radical act of noticing.\n\nThe Society's volunteers visit morgues, monitor missing persons reports, walk the streets of the deep Shelf and the Underworld, and document every death they can — recording names when names are known, descriptions when they aren't, circumstances when they can be determined, and the simple fact of a life ended when nothing else can be established. They maintain the Memorial Wall — a constantly updated digital and physical record in the Shelf that lists every documented death, and that has become a pilgrimage site for people seeking evidence that their lost loved ones were noticed by someone.\n\nThe Society also conducts memorial services — simple ceremonies marking the death of individuals who would otherwise have no funeral, no mourners, and no acknowledgment that they lived. These services are attended by whoever comes — sometimes family members who couldn't afford a funeral, sometimes strangers who believe nobody should die unmourned, sometimes nobody at all except the Society's officiants.",
    ideology: "Every life deserves to be remembered. The Society's mission is not political but existential — it insists that being poor, undocumented, or disconnected does not make a person's life less real or their death less significant.",
    territory: "The Memorial Wall is located in the central Shelf. The Society maintains a documentation office nearby and has volunteers operating throughout the city.",
    leadership: "Director Esme Volkov-Okafor, a former hospice worker who founded the Society after realizing that the people she cared for in death were the same people no system cared for in life.",
    methods: [
      "Documentation of deaths among unregistered and unclaimed populations",
      "Maintenance of the Memorial Wall — a physical and digital record of the dead",
      "Memorial services for those who would otherwise have none",
      "Missing persons investigation and family notification",
      "Advocacy for municipal death registration reform",
      "Grief counseling for bereaved families"
    ],
    resources: [
      "1,500 volunteers across GLMZ",
      "The Memorial Wall — a comprehensive record of documented deaths",
      "A documentation office with records spanning fifteen years",
      "Esme Volkov-Okafor's dedication and community connections",
      "Relationships with morgues, hospitals, and community organizations",
      "Moral authority rooted in the most basic human act: acknowledging the dead"
    ],
    goals: [],
    relationships: [
      { name: "", type: "", description: "The Remembrance Society and the Coffin Nails have an uneasy but functional relationship. The Nails provide information about deaths in the deep Shelf, and the Society provides a record that gives the Nails' grim work a kind of meaning neither organization expected.", tags: ["community", "death"] }
    ],
    narrative_function: "The Remembrance Society represents the most fundamental form of resistance to dehumanization: the refusal to let people disappear without notice.",
    story_hooks: [
      "The Memorial Wall's records reveal a pattern: deaths in a specific Shelf zone have tripled over six months, but the causes are varied and individually unremarkable. Only the Society's comprehensive records make the pattern visible. Something is killing people in that zone, and nobody has noticed except the people who count the dead.",
      "A woman approaches the Society claiming to be the daughter of someone listed on the Memorial Wall — someone who died fifteen years ago. She has proof her parent is alive. The Society's records are being tested, and the implications extend far beyond one family."
    ],
    tags: ["faction", "community", "memorial", "death", "documentation", "shelf", "grief"]
  }
];

// ============================================================================
// Write all factions
// ============================================================================

// ============================================================================
// SUPPLEMENTAL FACTIONS (filling gaps across categories)
// ============================================================================

const supplemental = [
  // CRIMINAL — additional
  {
    name: "The Red Ledger",
    aliases: ["Red Ledger", "The Bookkeepers", "RL"],
    motto: "Every debt gets paid. We keep the books.",
    description: "The Red Ledger is a criminal lending and debt enforcement operation of approximately 90 members that functions as the Shelf's unofficial banking system. In a tier where corporate financial services don't operate because the population isn't profitable enough to serve, the Red Ledger fills the gap — offering loans, managing savings, facilitating transactions, and providing the financial infrastructure that legitimate institutions refuse to provide.\n\nThe catch, of course, is the interest rates. The Red Ledger charges rates that would be criminal in a jurisdiction with consumer protection laws, which GLMZ is not. A Shelf resident who borrows Φ1,000 to cover an augmentation repair might owe Φ3,000 within six months. The Ledger's enforcers — called 'collectors' — ensure repayment through escalating pressure: reminders, public shaming, property seizure, physical intimidation, and, in extreme cases, seizure of the debtor's augmentations (sold to the Cutters Guild to satisfy the debt).\n\nWhat prevents the Red Ledger from being pure predation is its other function: it genuinely facilitates economic activity in the Shelf. Without the Ledger's loans, many Shelf businesses couldn't start. Without its transaction services, trade between neighborhoods would be slower and less reliable. The Ledger is a parasite and a circulatory system simultaneously, and the Shelf's relationship with it is the same as its relationship with every other institution that exploits the desperate: hatred, dependence, and the absence of alternatives.",
    ideology: "Money makes the world move. We make the money move. The Red Ledger views itself as an essential financial service provider operating in a market that legitimate institutions have abandoned. The exploitation is acknowledged internally as the cost of operating without institutional protections.",
    territory: "Throughout the Shelf, with collection offices (called 'branches') in every major Shelf neighborhood.",
    leadership: "The Comptroller — a man named Idris Osei-Volkov — runs the Ledger with the fastidious precision of an actual banker. He worked for a legitimate financial institution before it withdrew from the Shelf, and he took its methods (if not its morals) with him.",
    methods: [
      "High-interest lending to Shelf residents and businesses",
      "Transaction facilitation and savings management",
      "Debt collection through escalating enforcement",
      "Augmentation seizure for debt satisfaction",
      "Financial record-keeping for the informal economy",
      "Money laundering for other criminal organizations"
    ],
    resources: [
      "90 employees including lenders, collectors, and administrators",
      "Branch offices throughout the Shelf",
      "Significant financial reserves from lending operations",
      "Idris's banking expertise applied to the informal economy",
      "Relationships with virtually every business and individual in the Shelf",
      "Detailed financial records on the Shelf's population"
    ],
    goals: [],
    relationships: [],
    narrative_function: "The Red Ledger represents the exploitation that fills the gap when legitimate institutions withdraw — and the uncomfortable reality that even predatory services can be essential.",
    story_hooks: [
      "Idris has discovered that someone is forging Red Ledger debt records — creating fictional debts and using Ledger collectors to enforce them. The fake debts are targeting specific individuals, and the pattern suggests someone is using the Ledger as a weapon.",
      "A Shelf business owner has organized a debt strike — a group refusal to pay Ledger debts. The Comptroller can't let the strike succeed without destroying the Ledger's business model. The strikers can't back down without losing everything."
    ],
    tags: ["faction", "criminal", "finance", "lending", "shelf", "debt", "organized crime"]
  },

  {
    name: "The Phantom Exchange",
    aliases: ["Phantom", "The Exchange", "PE"],
    motto: "If it exists, we can move it. If it doesn't exist yet, give us an hour.",
    description: "The Phantom Exchange is a black market logistics operation of about 70 members that specializes in the movement and storage of goods that no legitimate logistics company will touch. Not smuggling in the traditional sense — the Harbor Rats handle that — but the complex, multi-step logistics of moving contraband through GLMZ's internal systems: from point of entry to storage, from storage to processing, from processing to distribution, all while evading the surveillance systems, corporate inspections, and law enforcement operations that make moving illegal goods inside the city as difficult as getting them through the port.\n\nThe Exchange operates a network of hidden warehouses, disguised transport vehicles, and corrupted logistics systems that can move anything from a vial of experimental pharmaceuticals to a stolen military vehicle across the city without triggering a single sensor. Their expertise is in the systems themselves — they know how corporate tracking works, where the blind spots are, and how to exploit the gaps between jurisdictions that exist even in a corporate-sovereign city.",
    ideology: "Logistics is power. The CorpoNations control the flow of goods the same way they control everything else — through systems that can be understood and subverted. The Exchange subverts them for profit.",
    territory: "Hidden warehouses scattered throughout the city. Primary hub in Old Harbor's industrial district.",
    leadership: "A logistics savant known as 'the Dispatcher' who previously managed supply chains for Ringo CorpoNation before being fired for discovering theft in the executive ranks and refusing to stay quiet about it.",
    methods: [
      "Hidden warehouse management for contraband storage",
      "Disguised transport operations through city systems",
      "Exploitation of blind spots in corporate tracking infrastructure",
      "Multi-step logistics planning for complex contraband movement",
      "Corruption of automated logistics and inspection systems",
      "Cold-chain and specialty storage for perishable or sensitive goods"
    ],
    resources: [
      "70 logistics specialists",
      "A network of hidden warehouses across GLMZ",
      "Disguised transport vehicles integrated into legitimate traffic",
      "Deep knowledge of corporate tracking system vulnerabilities",
      "The Dispatcher's supply chain management expertise",
      "Relationships with every criminal organization that needs logistics"
    ],
    goals: [],
    relationships: [
      { name: "The Harbor Rats", type: "partner", description: "The Exchange handles internal logistics for goods the Rats bring through the port. The arrangement is seamless and mutually profitable.", tags: ["criminal", "logistics"] }
    ],
    narrative_function: "The Exchange represents the invisible infrastructure of the illegal economy — the supply chain that nobody sees but everybody depends on.",
    story_hooks: [
      "A shipment in Exchange custody has started emitting a signal that wasn't there when it was stored. The Dispatcher doesn't know what's in the container (they never ask), but whatever it is, it's broadcasting their warehouse location to someone.",
      "The Exchange has been hired to move something through the city in exactly 47 minutes — not earlier, not later. The timing precision suggests the cargo is keyed to an event that will happen at the delivery point. The Dispatcher wants to know what event."
    ],
    tags: ["faction", "criminal", "logistics", "smuggling", "old harbor", "warehouses", "organized crime"]
  },

  // MERCENARY — additional
  {
    name: "The Basilisk Group",
    aliases: ["Basilisk", "BG", "The Lizards"],
    motto: "Don't look away. We don't.",
    description: "The Basilisk Group is a private intelligence firm of approximately 100 operatives specializing in deep-cover infiltration, long-term surveillance, and the kind of intelligence gathering that requires putting a human being inside a target organization for months or years. While Prism Security sells electronic surveillance and the Flicker Collective sells stolen data, Basilisk sells something neither can provide: human intelligence gathered by agents who have become part of the target's world.\n\nBasilisk agents are trained in identity construction, behavioral adaptation, and the psychological endurance required to maintain a false identity under sustained stress. A typical Basilisk operation involves placing an operative inside a target organization — a CorpoNation, a criminal group, a political movement — where they live, work, and build relationships as a member for six months to two years before extracting the intelligence the client needs. The personal cost of this work is enormous: agents who spend years living as someone else often struggle to remember who they actually are.\n\nThe Group is selective about clients and targets. They won't infiltrate organizations they deem too dangerous for their agents (the Jade Syndicate's counter-intelligence is good enough to make infiltration near-suicidal) and they won't accept contracts they judge to be primarily aimed at destroying rather than understanding the target. These scruples are partly moral and partly practical — Basilisk's agents are expensive to train and difficult to replace.",
    ideology: "Understanding is the ultimate advantage. Basilisk believes that the deepest intelligence comes not from surveillance or data theft but from being there — from seeing an organization from the inside, understanding how it actually works rather than how it appears to work.",
    territory: "Basilisk's headquarters is a nondescript office in the Laceworks. Agents operate wherever their assignments take them.",
    leadership: "Director Nadia Okafor-Chen, a former Axiom corporate intelligence officer whose understanding of infiltration psychology is the foundation of Basilisk's training program.",
    methods: [
      "Deep-cover infiltration of target organizations",
      "Long-term identity construction and maintenance",
      "Human intelligence gathering from inside target organizations",
      "Agent extraction when cover is compromised",
      "Intelligence analysis synthesizing agent reports with open-source data",
      "Counter-infiltration consulting — helping clients detect infiltrators in their own organizations"
    ],
    resources: [
      "100 trained intelligence operatives",
      "Identity construction infrastructure including documentation, history, and digital presence fabrication",
      "Training facilities for agent preparation",
      "Nadia Okafor-Chen's psychological and intelligence expertise",
      "A track record of successful infiltrations that attracts premium clients",
      "The patience to run operations that take years to produce results"
    ],
    goals: [],
    relationships: [],
    narrative_function: "Basilisk represents the human cost of intelligence work — the question of what happens to a person who becomes someone else for a living.",
    story_hooks: [
      "A Basilisk agent deep inside a criminal organization has stopped reporting. They're still alive — the organization hasn't discovered them — but they've gone silent. Nadia suspects the agent has gone native: become so integrated into their cover identity that they've forgotten they have another one.",
      "A former Basilisk agent is selling the identities of current agents to the organizations those agents have infiltrated. Nadia needs to identify the traitor and warn her agents before someone gets killed."
    ],
    tags: ["faction", "mercenary", "intelligence", "infiltration", "espionage", "laceworks"]
  },

  {
    name: "The Threshold",
    aliases: ["Threshold Ops", "The Threshold", "TH"],
    motto: "The line between legal and illegal is where we live.",
    description: "The Threshold is a fixer network — a group of approximately 35 intermediaries who connect clients with operators, negotiate contracts, manage logistics, and take a percentage for making things happen. They are not mercenaries themselves. They are the people mercenaries call when they need work, and the people clients call when they need a mercenary but don't know one.\n\nEvery fixer in the Threshold maintains a roster of reliable operators — runners, fighters, technicians, specialists of every kind — and a list of clients who need services they can't obtain through legitimate channels. When a match is made, the fixer negotiates terms, handles payment escrow, and manages the relationship between parties who usually prefer not to know each other's names. The fixer's value is trust: both parties trust the fixer to be honest, to hold the money, and to enforce the deal.\n\nThe Threshold is not a hierarchy — it's a professional association of independent fixers who share information, refer clients, and maintain shared standards of practice. A fixer who cheats a client or an operator gets expelled, and expulsion means losing access to the network that makes the business possible. This self-regulation produces a level of reliability that the gray market otherwise lacks.",
    ideology: "The market exists. People need things done. Other people can do those things. We connect them. The Threshold's fixers are pragmatists who view themselves as facilitators of inevitable transactions.",
    territory: "Fixers operate from personal offices, bars, and meeting spaces throughout GLMZ. No central headquarters.",
    leadership: "No formal leader. The longest-serving fixer, a man named Solomon Reyes-Nakamura, is considered first among equals and mediates disputes between fixers.",
    methods: [
      "Client-operator matchmaking for gray market services",
      "Contract negotiation and terms management",
      "Payment escrow and financial management",
      "Information sharing between fixers",
      "Reputation management for operators and clients",
      "Dispute resolution when jobs go wrong"
    ],
    resources: [
      "35 experienced fixers with extensive contact networks",
      "Rosters of reliable operators across every specialty",
      "Client lists spanning corporate, criminal, and individual needs",
      "Financial escrow systems ensuring honest dealing",
      "Solomon's decades of experience and network",
      "The collective reputation of the Threshold network"
    ],
    goals: [],
    relationships: [],
    narrative_function: "The Threshold represents the connective tissue of the gray market — the invisible intermediaries without whom the entire system of freelance operations would collapse.",
    story_hooks: [
      "A fixer has been murdered — the first killing of a Threshold member in twelve years. The fixers' neutrality has always protected them. Someone has decided that protection no longer applies, and the entire network is at risk.",
      "Solomon has received a contract request that he can't place through normal channels — the client wants something done that every operator on every fixer's roster has refused. The job is either too dangerous, too immoral, or something else entirely."
    ],
    tags: ["faction", "mercenary", "fixer", "network", "gray market", "contracts"]
  },

  {
    name: "The Revenants",
    aliases: ["Revs", "The Revenants", "Dead Walk"],
    motto: "We already died once. Everything after is borrowed time.",
    description: "The Revenants are a mercenary company of approximately 70 operators with a unique recruitment criterion: every member has been clinically dead at least once. Flatlined on an operating table, heart stopped on a battlefield, drowned and revived, killed by augmentation failure and restarted — the specific cause doesn't matter. What matters is the psychological transformation that follows: the Revenants recruit people who have died and come back, because those people fight differently.\n\nThe company's founder, a woman known as 'Lazarus' (real name: Yuki Park-Adeyemi), was killed during a corporate security operation — shot through the chest, declared dead, and revived fourteen minutes later by a combat medic who refused to stop trying. When she recovered, she found that the fear of death that had modulated her behavior her entire life was simply gone. Not suppressed, not managed — absent. She fought without hesitation, took risks that terrified her colleagues, and discovered that the absence of death-fear didn't make her reckless — it made her clear.\n\nThe Revenants are not suicidal. They are not careless. They are operators who have passed through death and emerged with a clarity of purpose that unnerves everyone who works with them. They take the jobs other mercenaries won't because the risk calculus is different when you've already paid the ultimate price and been refunded.",
    ideology: "Death is a teacher. The lesson it teaches is that everything before and after it is a gift, and gifts should not be wasted on fear. The Revenants' philosophy is existentialist in the purest sense: freed from the fear of death, every choice becomes genuinely free.",
    territory: "A compound in the lower Circuit called 'the Morgue' — named with the dark humor that characterizes the organization.",
    leadership: "Lazarus leads with quiet authority earned through being the first to die and the first to come back. She is calm, focused, and deeply unsettling to people who haven't experienced what she has.",
    methods: [
      "High-risk operations that other mercenary groups decline",
      "Assault operations requiring operators who won't freeze under fire",
      "Extraction from hostile environments where survival odds are low",
      "Bodyguard services for clients facing assassination threats",
      "Reconnaissance in dangerous areas — the Underworld's deepest sections, contested territories",
      "Training programs teaching fear management techniques derived from near-death psychology"
    ],
    resources: [
      "70 operators who do not fear death",
      "The Morgue — a well-equipped compound and training facility",
      "Military-grade equipment maintained to high standards",
      "Lazarus's leadership and the loyalty she inspires",
      "A reputation for taking and completing impossible jobs",
      "Psychological resilience that functions as a force multiplier"
    ],
    goals: [],
    relationships: [],
    narrative_function: "The Revenants represent the question of what remains when the most fundamental human fear is removed — and whether the answer is freedom or something else.",
    story_hooks: [
      "Lazarus has received a contract to extract someone from a location that her intelligence says is a death trap — literally designed to kill everyone who enters. She's taking the job anyway. She needs additional support from people who understand what they're walking into.",
      "A new recruit's 'death' turns out to have been faked — they were never actually dead. The Revenants' internal culture depends on the shared experience of death. A fake undermines everything. Lazarus needs to know who sent them and why."
    ],
    tags: ["faction", "mercenary", "death", "fearless", "circuit", "operators", "elite"]
  },

  // CRIMINAL — additional small gangs and operations
  {
    name: "The Gilt Frame",
    aliases: ["Gilt", "The Frame", "GF"],
    motto: "The right fake is worth more than the wrong real.",
    description: "The Gilt Frame is a forgery and counterfeiting ring of approximately 30 members that produces the highest-quality counterfeit goods in GLMZ — fake luxury augmentations, counterfeited corporate credentials, forged art, and replicated Φ currency (though currency counterfeiting has become nearly impossible with quantum-encrypted transactions, the Gilt Frame manages to counterfeit physical Φ tokens that still circulate in the lower tiers).\n\nThe Frame's specialty is augmentation counterfeiting — producing convincing replicas of high-end augmentations from manufacturers like Crucible Industries and TESSERA that look, scan, and partially function like the genuine article but cost a fraction of the price. These counterfeits range from cosmetic augmentations (fake chrome that looks expensive but has no functional capability) to functional but inferior copies (augmentations that work but fail sooner and perform worse than genuine models). The market for these counterfeits is enormous among Tier-2 and Tier-3 residents who want to project a status their income doesn't support.\n\nThe Frame also produces forged corporate documentation — employee credentials, security clearances, authorization codes — that are used by other criminal organizations for infiltration and fraud. Their forgeries are good enough to fool automated verification systems, which is a technical achievement that requires deep understanding of corporate security architecture.",
    ideology: "Everything is a copy of something. The CorpoNations sell brand identity as much as functionality. The Gilt Frame sells the same brand identity at a better price. In a world where image is everything, the image is the product.",
    territory: "Hidden workshops in the Circuit. The Frame moves locations frequently to avoid corporate anti-counterfeiting operations.",
    leadership: "An artist known only as 'Vermeer' whose attention to detail in counterfeiting borders on obsessive. Vermeer is rumored to have been a legitimate artisan before turning to forgery.",
    methods: [
      "Counterfeiting of luxury augmentations",
      "Forgery of corporate credentials and documentation",
      "Counterfeiting of physical Φ currency tokens",
      "Art forgery for the upper-tier market",
      "Reverse engineering of corporate security verification systems",
      "Distribution through a network of resellers"
    ],
    resources: [
      "30 skilled forgers and counterfeiters",
      "Hidden workshops with precision fabrication equipment",
      "Deep knowledge of corporate security and verification systems",
      "Vermeer's artistic skill and obsessive attention to detail",
      "A distribution network of resellers across tiers",
      "A client base that values appearance over authenticity"
    ],
    goals: [],
    relationships: [],
    narrative_function: "The Gilt Frame asks what 'real' means in a world where the distinction between genuine and counterfeit is increasingly a matter of who holds the trademark.",
    story_hooks: [
      "A Gilt Frame counterfeit augmentation has performed better than the genuine article it was copying. The forgers don't understand how — they built it to the same specifications. Something in their manufacturing process is producing results that exceed the original design.",
      "Vermeer has been commissioned to forge something that isn't a product or a document — a person. Someone wants a perfect physical duplicate of a specific individual, and the purpose is unclear."
    ],
    tags: ["faction", "criminal", "forgery", "counterfeit", "augment", "circuit", "art"]
  },

  {
    name: "The Gutter Prophets",
    aliases: ["Gutter", "Prophets", "GP"],
    motto: "We see what the gutters see. The gutters see everything.",
    description: "The Gutter Prophets are a loose criminal intelligence network of approximately 100 members drawn from GLMZ's most overlooked population: the homeless, the destitute, the people who sit on street corners and sleep in doorways and occupy the spaces that everyone else walks past without seeing. The Prophets have weaponized invisibility — they watch, they listen, they remember, and they sell what they know.\n\nThe network was organized by a formerly homeless woman named Oracle (legal name: Patience Alvarez-Okonkwo) who recognized that the people the city treats as invisible occupy the city's most valuable surveillance positions: street corners, building entrances, transit stations, back alleys. They see who comes and goes. They hear conversations that speakers assume are private because the only person nearby is a nobody sleeping under a blanket. The Prophets collect this intelligence and sell it — to private investigators, to criminal organizations, to corporate security consultants, and to anyone who understands that the most valuable intelligence comes from the places nobody thinks to look.\n\nThe Prophets' intelligence is human-level rather than technical — they can't hack a network or intercept a BCI transmission, but they can tell you who met whom, when, where, and what they looked like while doing it. In a city obsessed with digital surveillance, the Prophets exploit the oldest surveillance technology there is: human eyes in public spaces.",
    ideology: "The invisible see everything. The Prophets believe that the same social forces that make them invisible give them power — the power of observation from a position that nobody monitors because nobody considers them worth monitoring.",
    territory: "Everywhere. Prophets operate in every district, on every major thoroughfare, in every transit station. Their 'headquarters' is wherever Oracle happens to be sitting.",
    leadership: "Oracle — Patience Alvarez-Okonkwo — coordinates the network from the street. She has a photographic memory, a talent for pattern recognition, and the absolute loyalty of people she fed and sheltered when nobody else would.",
    methods: [
      "Street-level surveillance by members positioned in public spaces",
      "Intelligence aggregation and pattern analysis by Oracle",
      "Sale of human intelligence to paying clients",
      "Counter-surveillance detection — Prophets notice when someone else is watching",
      "Missing persons location — Prophets often know where people go when they disappear",
      "Early warning — Prophets detect unusual activity before it registers on any system"
    ],
    resources: [
      "100 members positioned across GLMZ's public spaces",
      "Oracle's photographic memory and analytical intelligence",
      "Invisibility — the Prophets' greatest asset is that nobody considers them observers",
      "Comprehensive street-level intelligence no technical system can replicate",
      "Loyalty among members who owe Oracle their survival",
      "Relationships with clients who value human intelligence"
    ],
    goals: [],
    relationships: [],
    narrative_function: "The Prophets represent the power of the overlooked — proof that the people a society ignores can see that society more clearly than anyone within it.",
    story_hooks: [
      "Oracle has noticed a pattern: over the past month, seventeen people have entered a specific building in the Circuit and never come out. No system has flagged this because the people are entering legally, through the front door. But Oracle's people sit outside that building every day, and they count.",
      "A client has hired the Prophets to find someone — and then hired a second group to eliminate the Prophets who know the target's location. Oracle knows about the second hire because her people saw it happen."
    ],
    tags: ["faction", "criminal", "intelligence", "surveillance", "street", "invisible", "human intel"]
  },

  {
    name: "The Scrap Barons",
    aliases: ["Barons", "Scrappers", "SB"],
    motto: "One man's waste is another man's arsenal.",
    description: "The Scrap Barons are a criminal salvage operation of approximately 130 members that controls the industrial scrap trade in Old Harbor and the Shelf. In a city that produces enormous quantities of technological waste — broken augmentations, decommissioned drones, obsolete hardware, industrial machinery — the Barons have built an empire on the refuse. They control scrapyards, operate salvage crews that strip abandoned structures, and run a processing operation that extracts valuable materials from technological waste for resale.\n\nThe Barons' operation is criminal primarily because the waste they salvage often belongs to CorpoNations that consider it proprietary even after disposal, and because the materials they extract include controlled substances — rare earth elements, military-grade composites, and augmentation components that are regulated regardless of their condition. The Barons also maintain a side business in weapons fabrication: their metalworking facilities and access to military-surplus components allow them to manufacture improvised weapons that are crude by corporate standards but effective and cheap, arming a significant portion of the Shelf's criminal ecosystem.\n\nThe environmental consequences of the Barons' operations are significant — their processing methods produce toxic runoff that contaminates the already-compromised waterways around Old Harbor. The Green Meridian Collective has targeted them repeatedly. The Barons don't care. Profit and survival take priority over water quality when you live at the bottom of the economic ladder.",
    ideology: "Nothing is worthless. Everything has value if you know how to extract it. The Barons view themselves as industrialists operating in a market the CorpoNations have abandoned, turning waste into wealth through labor and expertise.",
    territory: "Scrapyards in Old Harbor and the Shelf's industrial margins. Processing facilities in the Underworld where toxic operations can be conducted without oversight.",
    leadership: "The Foundry Boss — a massive, heavily augmented man named Gregor Petersen-Obi — runs the Barons from the largest scrapyard in Old Harbor with the territorial aggression of someone who built everything he has from literal garbage.",
    methods: [
      "Scrapyard operations collecting and sorting technological waste",
      "Salvage crews stripping abandoned structures and vehicles",
      "Material extraction and processing from technological waste",
      "Improvised weapons fabrication from salvaged materials",
      "Sale of recovered components to black market and legitimate buyers",
      "Territorial control of scrap collection zones"
    ],
    resources: [
      "130 members including salvagers, processors, and fabricators",
      "Multiple scrapyards in Old Harbor and the Shelf",
      "Processing facilities with metalworking and extraction capability",
      "Weapons fabrication capability",
      "Gregor's territorial dominance and industrial expertise",
      "Access to materials that have military and technological value"
    ],
    goals: [],
    relationships: [],
    narrative_function: "The Barons represent the bottom of the economic food chain — the people who survive by extracting value from what the wealthy discard.",
    story_hooks: [
      "A salvage crew has recovered something from a demolished building that is not scrap — a device in perfect condition, sealed in a case, deliberately hidden in the structure's walls. Multiple parties want it. Gregor wants to know what it does before he sells it.",
      "The Barons have found augmentation components in a scrapyard that are still transmitting — sending data to an unknown receiver. The components came from a corporate disposal shipment. Someone put still-active surveillance hardware in the garbage on purpose."
    ],
    tags: ["faction", "criminal", "salvage", "scrap", "old harbor", "shelf", "fabrication"]
  },

  // POLITICAL — additional
  {
    name: "The Human Baseline Alliance",
    aliases: ["HBA", "Baseline", "The Naturals"],
    motto: "Human first. Human enough.",
    description: "The Human Baseline Alliance is a political advocacy group of approximately 7,000 members that occupies an uncomfortable position in GLMZ's political landscape: they advocate for unaugmented human rights without the religious framework of the Unbroken Flesh Tabernacle and without the anti-technology extremism of movements that want to abolish augmentation entirely. The HBA simply argues that choosing not to augment should not be a social or economic death sentence.\n\nIn GLMZ, the unaugmented face increasing discrimination: employers prefer augmented workers, social spaces are designed for augmented perception, public services assume BCI access, and the cultural assumption that augmentation is progress makes the unaugmented seem backward, stubborn, or poor. The HBA campaigns against this discrimination through legal advocacy, public education, and political lobbying for 'baseline accessibility' — the requirement that public services, employment, and social participation remain accessible to unaugmented humans.\n\nThe HBA is politically moderate and deliberately secular, which distinguishes it from the Tabernacle and makes it palatable to mainstream political observers. It doesn't oppose augmentation — many of its members are augmented themselves, advocating for the rights of unaugmented family members or out of principle. This moderation makes the HBA effective in legislative contexts where the Tabernacle's religious fervor is counterproductive.",
    ideology: "Augmentation should be a choice, not a requirement. The unaugmented deserve equal access to employment, services, and social participation. The HBA's position is one of accommodation rather than opposition — make room for everyone, augmented or not.",
    territory: "Offices in the Circuit and Shelf. Active chapters in every district.",
    leadership: "Chairperson Gabriel Okafor-Johansson, a former employment lawyer who became an advocate after representing dozens of clients fired for being unaugmented.",
    methods: [
      "Legal advocacy for unaugmented workers' rights",
      "Lobbying for baseline accessibility requirements",
      "Public education about augmentation discrimination",
      "Pro bono legal representation for discrimination cases",
      "Coalition building with labor and civil rights organizations",
      "Research and publication on the economic impact of augmentation requirements"
    ],
    resources: [
      "7,000 members across augmented and unaugmented populations",
      "Legal team specializing in employment and accessibility law",
      "Offices in the Circuit and Shelf",
      "Gabriel Okafor-Johansson's legal expertise and advocacy skills",
      "Coalition relationships with the UWF and other organizations",
      "Research data on augmentation discrimination"
    ],
    goals: [],
    relationships: [],
    narrative_function: "The HBA represents the right to remain human in a world that increasingly defines 'human' as 'augmented' — and the political courage of insisting that a slower pace is not the same as standing still.",
    story_hooks: [
      "A major employer has announced an augmentation requirement for all positions — including roles where augmentation provides no functional benefit. The HBA is filing a landmark legal challenge, and the employer has hired lobbyists to rewrite the law before the case is heard.",
      "An HBA member who is publicly unaugmented has been discovered to have a hidden BCI — installed against their will years ago, active and transmitting. The implications for the HBA's credibility and for the member's autonomy are devastating."
    ],
    tags: ["faction", "political", "rights", "unaugmented", "baseline", "circuit", "shelf", "advocacy"]
  },

  // CULTURAL — additional
  {
    name: "The Deep Archive",
    aliases: ["Deep Archive", "The Archive", "DA"],
    motto: "History is the one thing they can't manufacture.",
    description: "The Deep Archive is a community of approximately 800 historians, researchers, librarians, and obsessive record-keepers dedicated to preserving an accurate history of GLMZ — a project that puts them in direct conflict with every CorpoNation, faction, and institution that has an interest in controlling how the past is remembered.\n\nIn GLMZ, history is not neutral. Corporate PR departments maintain official narratives that minimize inconvenient truths. The Meridian Quorum's public records are carefully curated. Vantablack Media's historical programming tells the story the sponsors want told. And beneath all of this, the actual history — the displacement, the violence, the broken promises, the failed systems, the lives ruined and the lives saved — gets buried under layers of managed narrative until nobody can distinguish what happened from what's convenient to believe happened.\n\nThe Deep Archive exists to dig through those layers. Its members maintain a distributed, encrypted database of historical records — corporate documents, municipal records, personal testimony, photographs, video, audio, and physical artifacts — that together constitute the most comprehensive and least flattering history of GLMZ in existence. The Archive is not a propaganda operation: it preserves records that complicate every narrative, including the narratives of resistance movements that the Archive's members might personally support. History, in the Archive's philosophy, is only useful when it's honest.",
    ideology: "Accurate history is the prerequisite for meaningful change. You cannot fix a system you don't understand, and you cannot understand a system whose history has been rewritten by the people who benefit from it. The Archive's mission is preservation of truth, not advocacy for any particular interpretation.",
    territory: "No physical headquarters — the Archive is distributed across encrypted nodes maintained by individual members. A few semi-public reading rooms exist in the Circuit and Old Harbor.",
    leadership: "Chief Archivist Miriam Chen-Adeyemi, a former university historian who was denied tenure for publishing research that contradicted Axiom's official founding narrative.",
    methods: [
      "Collection and preservation of historical records from all sources",
      "Verification and authentication of historical documents",
      "Maintenance of a distributed encrypted historical database",
      "Publication of historical research through independent channels",
      "Oral history collection from long-term GLMZ residents",
      "Providing historical context to journalists, researchers, and legal teams"
    ],
    resources: [
      "800 dedicated researchers and archivists",
      "A distributed encrypted database of historical records",
      "Authentication expertise for historical document verification",
      "Miriam Chen-Adeyemi's academic credentials and research skills",
      "Relationships with journalists and legal teams who use Archive research",
      "The irreplaceable value of accurate historical records"
    ],
    goals: [],
    relationships: [],
    narrative_function: "The Deep Archive represents the fight for historical truth — the conviction that accurate memory is the foundation of meaningful action.",
    story_hooks: [
      "The Archive has authenticated a document that contradicts the official account of GLMZ's founding — a document that, if published, would undermine the legal basis of corporate sovereignty in the city. Miriam is trying to decide whether to publish and is receiving pressure from every direction.",
      "Someone is systematically accessing and corrupting the Archive's database — not destroying records but subtly altering them, changing dates, names, and details in ways that make false narratives appear true. The corruption has been happening for months, and Miriam doesn't know how deep it goes."
    ],
    tags: ["faction", "cultural", "history", "archive", "records", "circuit", "old harbor", "truth"]
  },

  {
    name: "The Neon Choir",
    aliases: ["Neon Choir", "The Choir", "NC"],
    motto: "When the city sings, it sings through us.",
    description: "The Neon Choir is a performing arts collective of approximately 150 musicians, vocalists, dancers, and performers who produce live entertainment across GLMZ — from massive BCI-enhanced concert experiences in the Laceworks to intimate acoustic performances in Shelf bars. In a city where most entertainment is corporate-produced, algorithmically optimized, and delivered through neural feeds, the Neon Choir insists on the radical act of performing live, in person, for audiences who are physically present.\n\nThe Choir emerged from the Circuit's music scene in the 2180s, when a group of musicians who had been performing independently realized that collective organization would give them access to better venues, shared equipment, and the ability to produce shows that no individual performer could stage alone. The collective has grown from a dozen musicians to a cultural force that stages events ranging from free street performances to sold-out spectacles that rival corporate entertainment productions in technical sophistication while exceeding them in raw emotional impact.\n\nThe Choir's performances are famous for their integration of live music with BCI-enhanced audience experiences — not the corporate-standardized feed content that Vantablack Media distributes, but custom-designed perceptual enhancements created by the Choir's own technicians that respond to the live performance in real time. The result is something that cannot be recorded, cannot be reproduced, and cannot be experienced except by being there — which makes Neon Choir events some of the most sought-after experiences in GLMZ.",
    ideology: "Live performance is irreplaceable. In a world where every experience can be commodified, recorded, and sold, the Neon Choir creates something that exists only in the moment — a shared experience between performers and audience that no algorithm can replicate.",
    territory: "Performance venues across GLMZ. The Choir's rehearsal and production space — called 'the Amp' — is in the Circuit.",
    leadership: "Artistic Director Zara Park-Okonkwo, a vocalist and composer whose vision for BCI-integrated live performance has defined the Choir's aesthetic.",
    methods: [
      "Live musical performances across all tiers and districts",
      "BCI-enhanced audience experiences designed by in-house technicians",
      "Free street performances in lower-tier neighborhoods",
      "Revenue-generating spectacles in Laceworks and Circuit venues",
      "Artist development and mentorship for emerging performers",
      "Collaboration with other cultural organizations for large-scale events"
    ],
    resources: [
      "150 performers and technical staff",
      "The Amp — rehearsal and production facility in the Circuit",
      "Custom BCI-experience design capability",
      "Performance equipment and sound systems",
      "Zara Park-Okonkwo's artistic vision and industry connections",
      "A loyal audience base that spans tiers"
    ],
    goals: [],
    relationships: [],
    narrative_function: "The Choir represents the survival of live art in a world designed to eliminate it — proof that some things can only be real when they happen in front of you.",
    story_hooks: [
      "The Choir's next major performance has been targeted — someone has planted code in their BCI-experience system that, during the show, will expose every audience member's private thoughts to everyone else in the venue. The saboteur's motive is unclear. The Choir needs the code found and removed before 10,000 people have the most intimate experience of their lives become public.",
      "Vantablack Media has offered the Choir an exclusive distribution deal worth millions of Φ. The deal would make their performances available to everyone — but only through corporate channels, and only in recorded form. Zara is torn between artistic purity and financial survival."
    ],
    tags: ["faction", "cultural", "music", "performance", "entertainment", "circuit", "laceworks", "bci"]
  },

  // RELIGIOUS — additional bonus
  {
    name: "The Cathedral of Saint Disconnect",
    aliases: ["Saint Disconnect", "The Cathedral", "Disconnectors"],
    motto: "Blessed are those who log off, for they shall know themselves.",
    description: "The Cathedral of Saint Disconnect is a small but growing spiritual movement of approximately 3,500 adherents built around the practice of periodic BCI disconnection — deliberately shutting down neural interfaces for hours, days, or weeks at a time to experience unaugmented consciousness. Founded by a former BCI addiction counselor named Father Ren Alvarez-Nakamura (the title is self-given but sincerely meant), the Cathedral teaches that constant neural connection has severed humanity's relationship with its own unmediated experience, and that periodic disconnection is necessary for spiritual and psychological health.\n\nThe Cathedral's practice — called 'the Silence' — involves supervised BCI shutdown in dedicated spaces where members experience unaugmented perception for the first time since childhood. The psychological effects are intense: many members report panic, disorientation, and a profound sense of loss when their BCI goes offline, followed by a gradual rediscovery of sensory experience — colors that look different without BCI filtering, sounds that register differently without neural processing, a sense of solitude that is both terrifying and, eventually, peaceful.\n\nThe movement is growing because it addresses a problem that few other organizations acknowledge: BCI dependency. In GLMZ, where BCIs are installed in early childhood and run continuously for decades, the neural interface has become so integrated with perception that turning it off feels like losing a sense. The Cathedral argues this dependency is itself a form of bondage — not to technology, but to the corporations that control the technology. Disconnection, in their theology, is not rejection of augmentation. It's the reclamation of the self that exists beneath it.",
    ideology: "Connection without disconnection is not freedom but dependency. The Cathedral does not oppose BCIs — it opposes the inability to function without them. True freedom requires the ability to stand in the Silence and find that you are still yourself.",
    territory: "Four 'Quiet Houses' — spaces designed for supervised BCI disconnection — in the Circuit and Shelf. The main Cathedral is a converted building in the mid-Circuit.",
    leadership: "Father Ren Alvarez-Nakamura, a gentle, patient man whose background in addiction counseling gives him a clinical understanding of dependency that enriches his spiritual teaching.",
    methods: [
      "Supervised BCI disconnection sessions in Quiet Houses",
      "Psychological support for members experiencing disconnection anxiety",
      "Community worship in unaugmented states",
      "Public advocacy for BCI dependency awareness",
      "Retreats offering extended Silence experiences",
      "Partnerships with mental health professionals studying BCI dependency"
    ],
    resources: [
      "3,500 adherents from diverse backgrounds",
      "Four Quiet Houses equipped for supervised disconnection",
      "Father Ren's counseling expertise and spiritual authority",
      "Growing clinical evidence supporting BCI disconnection benefits",
      "Relationships with mental health professionals",
      "A message that resonates with increasing numbers of people"
    ],
    goals: [],
    relationships: [],
    narrative_function: "The Cathedral asks what it means to be yourself when your sense of self has been mediated by technology since childhood — and whether the courage to disconnect is the most radical act in a connected world.",
    story_hooks: [
      "A Cathedral member who entered the Silence for a two-week retreat has emerged claiming to perceive things that aren't visible to augmented senses — patterns in light, sounds below the threshold of BCI processing. Either the Silence has caused neurological damage or it has revealed something the BCIs are filtering out.",
      "TESSERA has filed a legal complaint claiming that the Cathedral's disconnection practice voids BCI warranties and constitutes unauthorized augmentation modification. The case could criminalize voluntary disconnection."
    ],
    tags: ["faction", "religious", "disconnection", "bci", "circuit", "shelf", "dependency", "spiritual"]
  },

  // MERCENARY — additional
  {
    name: "The Nightmarket Brokers",
    aliases: ["Nightmarket", "The Brokers", "NMB"],
    motto: "Everything has a price. We just help you find it.",
    description: "The Nightmarket Brokers are not a mercenary group but a procurement network — approximately 45 specialists who can obtain virtually anything for anyone willing to pay. Need a specific piece of military hardware? An experimental pharmaceutical? A rare biological sample? Access to a restricted database? A meeting with someone who doesn't take meetings? The Nightmarket Brokers find it, negotiate the acquisition, and deliver it, taking a percentage for their trouble.\n\nThe Brokers are not merchants — they don't maintain inventory. They are connectors, negotiators, and problem-solvers who maintain relationships across every tier, every faction, and every market in GLMZ. Each Broker specializes in a domain: weapons, pharmaceuticals, technology, biological materials, information, services, or the rare 'general' Broker who can procure across domains. Their value lies not in what they have but in who they know and what they can arrange.\n\nThe Nightmarket — from which the Brokers take their name — is a rotating physical marketplace that appears in a different location every week, where Brokers meet clients, display samples, and negotiate deals. The market's location is communicated through encrypted channels and changes frequently enough that corporate security has never successfully raided it.",
    ideology: "Every need has a supply. The Nightmarket Brokers exist because the legal market doesn't serve every need, and the illegal market doesn't serve anyone reliably. They bridge the gap between want and have, which is, they argue, the most fundamental economic function there is.",
    territory: "The Nightmarket appears in rotating locations throughout the city. Individual Brokers operate from personal offices and meeting spaces.",
    leadership: "The Auctioneer — a figure who manages the Nightmarket's logistics and sets the rotation schedule — is the closest thing to a leader. Their identity is known only to senior Brokers.",
    methods: [
      "Procurement of rare, restricted, and illegal goods and services",
      "Negotiation between suppliers and clients",
      "Management of the rotating Nightmarket physical marketplace",
      "Relationship maintenance across tiers and factions",
      "Quality verification of procured goods",
      "Discreet delivery of sensitive acquisitions"
    ],
    resources: [
      "45 procurement specialists with diverse domain expertise",
      "The Nightmarket — a rotating physical marketplace",
      "Relationship networks spanning every tier and faction",
      "The Auctioneer's logistical management",
      "A reputation for being able to find anything",
      "Financial reserves from procurement fees"
    ],
    goals: [],
    relationships: [],
    narrative_function: "The Nightmarket represents the reality that in a city of restricted markets and controlled access, the ability to procure is itself a form of power.",
    story_hooks: [
      "A client has asked the Nightmarket to procure something that doesn't exist — a piece of technology that no known manufacturer has produced, described in precise technical specifications. Either the client is delusional, or there's a manufacturer nobody knows about.",
      "The Auctioneer has been receiving threats: someone wants the Nightmarket shut down permanently and has demonstrated the ability to discover the market's rotating locations in advance. The Brokers need to find the threat before the next market rotation."
    ],
    tags: ["faction", "mercenary", "procurement", "black market", "network", "rotating"]
  },

  // Additional criminal — filling gap
  {
    name: "The Mirage Syndicate",
    aliases: ["Mirage", "The Syndicate", "MS"],
    motto: "Reality is negotiable.",
    description: "The Mirage Syndicate is a sophisticated fraud operation of approximately 60 members that specializes in augmented-reality scams — criminal schemes that exploit BCI-mediated perception to deceive victims into believing things that aren't real. In a city where most people experience reality through augmented neural interfaces, the Mirage Syndicate has discovered that if you can hack someone's perception, you can steal everything they have without them realizing it until the BCI filter drops.\n\nThe Syndicate's operations include AR-overlay scams (projecting false augmented-reality overlays onto physical spaces to disguise the nature of a transaction — making a worthless product appear valuable, making a dangerous location appear safe, making a stranger appear to be a trusted friend), memory injection fraud (using BCI exploits to implant false memories of agreements, transactions, or events that never occurred), and perception theft (hijacking a target's BCI output to see through their eyes, then using the intelligence gained to commit fraud or blackmail).\n\nWhat makes the Mirage Syndicate uniquely disturbing is that their crimes attack the victim's confidence in their own senses. A person who has been defrauded by traditional means knows they were tricked. A person who has been defrauded by the Mirage Syndicate doesn't know what's real anymore — their own perceptions have been weaponized against them, and the resulting psychological damage often exceeds the financial loss.",
    ideology: "Perception is reality, and reality is a product of the systems that mediate it. The Mirage Syndicate doesn't create illusions — it reveals that the entire augmented world is an illusion, and then profits from the revelation.",
    territory: "Operating throughout GLMZ, with particular focus on upper-tier targets where the financial returns justify the technical complexity of the operations.",
    leadership: "A woman known as 'the Director' coordinates Syndicate operations with the precision of a film production — because that's essentially what Mirage operations are: reality as directed fiction.",
    methods: [
      "AR-overlay scams exploiting BCI-mediated perception",
      "Memory injection fraud through BCI exploits",
      "Perception theft — hijacking target BCI output",
      "Identity fraud using AR disguise technology",
      "Blackmail based on perception-theft intelligence",
      "Technical development of new BCI exploit techniques"
    ],
    resources: [
      "60 operatives including BCI exploit developers and field agents",
      "Proprietary BCI exploit tools for perception manipulation",
      "The Director's organizational skill and creative vision",
      "Technical knowledge of BCI architecture and vulnerabilities",
      "Upper-tier targets providing high-value returns",
      "A growing library of exploit techniques"
    ],
    goals: [],
    relationships: [],
    narrative_function: "The Mirage Syndicate represents the ultimate vulnerability of augmented existence — the terrifying reality that when your senses are mediated by technology, your reality can be hacked.",
    story_hooks: [
      "A victim of a Mirage operation has committed suicide, unable to distinguish real memories from injected ones after the fraud was discovered. The victim's family wants justice. The Syndicate wants the case to go away. The method used suggests a new exploit that could affect any BCI user in the city.",
      "The Director has been running a long-term operation against a specific target — not for money but for a personal reason that the rest of the Syndicate doesn't know about. The operation is about to converge, and its consequences extend far beyond fraud."
    ],
    tags: ["faction", "criminal", "fraud", "perception", "bci", "augmented reality", "cybercrime"]
  },

  // Additional community
  {
    name: "The Stitch Network",
    aliases: ["The Stitch", "Stitch", "SN"],
    motto: "You're hurt. We're here. That's all that matters.",
    description: "The Stitch Network is an underground medical service of approximately 200 volunteer medical professionals — doctors, nurses, paramedics, surgeons, and BCI technicians — who provide free or low-cost medical care to GLMZ residents who can't access corporate healthcare. In a city where medical care is tied to corporate employment and insurance, the Stitch fills the gap for the unemployed, the undocumented, the criminal, and the simply too poor.\n\nThe Network operates from hidden clinics throughout the Shelf and Underworld — spaces that range from well-equipped surgical suites to converted apartments with basic first-aid capability. Volunteers include licensed professionals who donate hours outside their corporate jobs (risking termination if discovered), retired medical workers, and self-taught practitioners whose skills have been honed by necessity. The quality of care varies, but the Network's best clinics provide treatment that rivals corporate facilities.\n\nThe Stitch is strictly neutral — they treat anyone, including corporate security officers injured in off-duty incidents, gang members wounded in territorial disputes, and fugitives on the run. This neutrality is maintained through a simple rule: what happens in the clinic stays in the clinic. No one is reported. No one is turned away. The Stitch has become one of the few truly trusted institutions in the Shelf, and its clinics function as de facto safe spaces in neighborhoods where safety is otherwise nonexistent.",
    ideology: "Medical care is a human right, not a corporate benefit. The Stitch believes that the decision to provide or withhold medical treatment based on employment status, corporate affiliation, or ability to pay is a moral obscenity — and that the appropriate response to moral obscenity is action, not protest.",
    territory: "Hidden clinics throughout the Shelf, Underworld, and Old Harbor. The Network's flagship clinic — called 'the Ward' — is in the mid-Shelf.",
    leadership: "Dr. Ibrahim Osei-Reyes, a retired trauma surgeon who founded the Stitch after watching patients die in corporate hospitals because their insurance lapsed. He coordinates the Network's operations with a calm authority that inspires volunteers to risk their careers.",
    methods: [
      "Free and low-cost medical treatment at hidden clinics",
      "Emergency trauma care for gunshot wounds, augmentation failures, and industrial injuries",
      "BCI repair and maintenance for patients who can't afford corporate service",
      "Pharmaceutical distribution from donated and diverted supplies",
      "Medical training for community health workers",
      "Strict neutrality — treating anyone regardless of affiliation"
    ],
    resources: [
      "200 volunteer medical professionals",
      "Hidden clinics throughout the Shelf, Underworld, and Old Harbor",
      "Medical supplies from donations, diversions, and purchases",
      "Dr. Osei-Reyes's surgical expertise and organizational ability",
      "The trust of communities who depend on the Network for healthcare",
      "Neutrality that is respected by virtually every faction"
    ],
    goals: [],
    relationships: [],
    narrative_function: "The Stitch represents the most basic form of human solidarity — the refusal to let people die from treatable conditions because the system has decided they don't deserve treatment.",
    story_hooks: [
      "A patient brought to the Ward has injuries that don't match any known weapon or accident — damage that suggests exposure to technology that doesn't officially exist. Dr. Osei-Reyes needs to understand what caused the injuries to treat them, but investigating means breaking the Network's neutrality.",
      "Lazarus Pharmaceuticals has discovered that diverted medical supplies are reaching the Stitch and has demanded that the diversion pipeline be shut down. The volunteers who have been diverting supplies face prosecution. The patients who depend on those supplies face death."
    ],
    tags: ["faction", "community", "medical", "healthcare", "shelf", "underworld", "old harbor", "neutral"]
  },

  // Additional religious
  {
    name: "The Communion of Broken Masks",
    aliases: ["Broken Masks", "The Communion", "Mask Wearers"],
    motto: "Only in breaking can the true face be revealed.",
    description: "The Communion of Broken Masks is a small, intense spiritual community of roughly 1,800 adherents built around the ritual practice of identity deconstruction. Drawing from theatrical traditions, Jungian psychology, and West African masquerade spiritual practices, the Communion teaches that every person wears layers of social masks — identities constructed by family, corporation, tier, culture, and augmentation — and that genuine selfhood can only be discovered by ritually breaking those masks, one by one.\n\nCommunion rituals involve elaborate ceremonies where participants don physical masks representing their social identities — their corporate role, their tier status, their augmented self-image, their family expectations — and then, through guided ritual, break them. The breaking is physical (ceramic masks are literally shattered), psychological (participants confront the gap between who they perform and who they are), and sometimes augmented (BCIs running identity-deconstruction protocols that temporarily suppress the social-performance software that most people's neural interfaces run constantly).\n\nThe movement attracts people in crisis — those experiencing identity dissolution from augmentation changes, corporate restructuring that destroys their professional self-concept, or the quiet desperation of realizing that every aspect of their life has been performed for an audience. The Communion doesn't promise to rebuild what it breaks. It promises that what remains after breaking is real.",
    ideology: "Identity is performance. The self is what remains when the performance stops. The Communion's theology draws from theatrical theory, depth psychology, and West African spiritual traditions to argue that the masks humanity wears are not protective but imprisoning, and that freedom begins with breaking them.",
    territory: "Three ritual spaces: one in the Circuit, one in Old Harbor, one in the Shelf. Each is designed as a theater-temple hybrid.",
    leadership: "The Mask Breaker — a woman named Adunni Chen-Baptiste — is a former theatrical director and Jungian therapist who synthesized the Communion's unique practice from her dual expertise.",
    methods: [
      "Mask-breaking rituals combining physical ceremony with psychological process",
      "BCI-assisted identity deconstruction protocols",
      "Individual counseling for participants experiencing identity crisis",
      "Community support for those in the process of reconstruction",
      "Public performances that double as recruitment and cultural commentary",
      "Workshops on identity, performance, and authenticity"
    ],
    resources: [
      "1,800 adherents including many in therapeutic and creative professions",
      "Three ritual theater-temple spaces",
      "Adunni Chen-Baptiste's therapeutic and theatrical expertise",
      "BCI identity-deconstruction protocols developed in-house",
      "Community support networks for members in transition",
      "Cultural credibility from the Communion's theatrical and artistic quality"
    ],
    goals: [],
    relationships: [],
    narrative_function: "The Communion asks the most uncomfortable question in a city of augmented identities: who are you when everything that can be taken away has been taken away?",
    story_hooks: [
      "A Communion ritual has gone wrong — a participant's identity deconstruction has not reversed. They have lost all sense of self and cannot function. The Communion's methods have never produced this result before, and Adunni suspects the participant's BCI was tampered with before the ritual.",
      "A corporate HR department has sent employees to the Communion as part of a 'personal development program.' The Communion's rituals are causing these employees to question their corporate loyalty so thoroughly that the employer wants the program shut down."
    ],
    tags: ["faction", "religious", "identity", "ritual", "theater", "circuit", "old harbor", "shelf", "psychology"]
  },

  // Additional criminal
  {
    name: "The Quiet Room",
    aliases: ["QR", "The Room", "Silence Brokers"],
    motto: "Some conversations never happened. We make sure of it.",
    description: "The Quiet Room is a criminal service provider of approximately 25 members that sells one thing: privacy. In a city where every conversation is potentially monitored, every meeting potentially surveilled, and every transaction potentially recorded, the Quiet Room provides guaranteed secure spaces where people can talk, plan, negotiate, and conspire without any risk of being observed.\n\nThe Room operates a network of twelve 'clean spaces' — rooms that have been stripped of every surveillance device, shielded against every known electronic monitoring technique, and maintained by technicians whose full-time job is ensuring that the spaces remain clean. Clients book time in the clean spaces through encrypted channels, pay in untraceable currency, and are guaranteed absolute privacy for the duration of their booking.\n\nThe Quiet Room's client list is the most diverse in GLMZ's gray market: corporate executives meeting to discuss things that would end their careers if overheard, criminal leaders planning operations that depend on secrecy, political dissidents organizing actions that would get them arrested, lovers conducting affairs they can't afford to have discovered, and intelligence operatives debriefing agents in spaces that can't be compromised. The Room doesn't know or care why its clients need privacy. It sells the absence of observation, and in GLMZ, absence of observation is priceless.",
    ideology: "Privacy is the foundation of free action. Without spaces where people can think, speak, and plan without being observed, freedom is performative — you can only do what you'd do if someone were watching. The Quiet Room sells the ability to act as though you're not being watched, because you're not.",
    territory: "Twelve clean spaces distributed across GLMZ, locations known only to the Room's management and communicated to clients for individual bookings.",
    leadership: "A person known only as 'Silence' manages the Room's operations with obsessive attention to the integrity of the clean spaces.",
    methods: [
      "Provision of guaranteed surveillance-free meeting spaces",
      "Continuous technical maintenance ensuring clean space integrity",
      "Encrypted booking and payment systems",
      "Client anonymity protection — the Room doesn't record who uses its spaces",
      "Counter-surveillance consulting for clients who need privacy outside the clean spaces",
      "Technical development of new shielding and anti-surveillance techniques"
    ],
    resources: [
      "25 technical specialists in counter-surveillance",
      "Twelve guaranteed-clean meeting spaces",
      "State-of-the-art shielding and anti-surveillance technology",
      "Silence's obsessive management and technical expertise",
      "A client list that generates enormous revenue from premium pricing",
      "The most valuable commodity in GLMZ: guaranteed privacy"
    ],
    goals: [],
    relationships: [],
    narrative_function: "The Quiet Room represents the commodification of privacy — the reality that in a surveillance city, the absence of observation is the most expensive luxury.",
    story_hooks: [
      "One of the clean spaces has been compromised — a recording device has been found that uses technology the Room's technicians can't identify. Someone with capabilities exceeding corporate surveillance has breached the Room's guarantee. If this becomes known, the Room's business is destroyed.",
      "A client has been murdered immediately after leaving a clean space — killed based on information that could only have been known if the space was monitored. But the Room's diagnostics show the space was clean. The information leaked some other way, and Silence needs to find out how before more clients die."
    ],
    tags: ["faction", "criminal", "privacy", "surveillance", "security", "counter-intel", "service"]
  },

  // Additional political
  {
    name: "The Meridian Youth Alliance",
    aliases: ["MYA", "Youth Alliance", "The Alliance"],
    motto: "We'll inherit this city. We intend to make it livable.",
    description: "The Meridian Youth Alliance is a political organization of approximately 9,000 members, all under the age of thirty, that campaigns for issues specific to GLMZ's younger generation: affordable education, entry-level employment access, housing for young workers, augmentation debt reform (many young people are indebted for childhood BCIs their parents financed), and representation in governance structures dominated by older, wealthier citizens.\n\nThe MYA is significant because it represents a demographic that GLMZ's power structures have been able to ignore — young people who are too old for parental protection, too young for established professional networks, and too poor for political influence. The Alliance gives them a collective voice, and that voice is increasingly loud.\n\nThe Alliance's methods are deliberately confrontational — they stage protests, occupy corporate lobbies, disrupt Meridian Quorum sessions, and use social media and BCI-native content to generate attention that older political organizations can't match. They are accused of being immature, disruptive, and unrealistic. They respond that the mature, orderly, realistic approach has produced the city they're inheriting, and that city is not acceptable.",
    ideology: "The current generation will outlive the systems built by the previous one. Those systems should serve the people who will live with their consequences longest. The MYA's politics are pragmatic rather than revolutionary — they want reform, not replacement — but their urgency reads as radicalism to institutions accustomed to patience.",
    territory: "Active in every district. Headquarters in a shared office in the Circuit called 'the Clubhouse.'",
    leadership: "Director Kai Okonkwo-Johansson, age 27, a former corporate intern who organized the Alliance after watching her entire graduating class fail to find employment that matched their qualifications or their debt.",
    methods: [
      "Organized protests and demonstrations",
      "Occupation of corporate and government spaces",
      "BCI-native content creation driving social media campaigns",
      "Political lobbying for youth-relevant issues",
      "Legal advocacy for augmentation debt reform",
      "Voter registration and political education"
    ],
    resources: [
      "9,000 members under thirty with energy and digital fluency",
      "BCI-native content production capabilities",
      "The Clubhouse headquarters in the Circuit",
      "Kai Okonkwo-Johansson's organizational skill and media savvy",
      "Alliance with other political organizations on shared issues",
      "The moral authority of speaking for a generation with no other voice"
    ],
    goals: [],
    relationships: [],
    narrative_function: "The MYA represents the political awakening of a generation that has been told to wait its turn — and has decided that waiting is a luxury designed to benefit the people ahead of them in line.",
    story_hooks: [
      "The MYA is planning the largest youth demonstration in GLMZ history. Corporate security has intelligence that provocateurs will infiltrate the march and trigger violence that justifies a crackdown. Kai needs to identify the provocateurs before the march becomes a massacre.",
      "A Meridian Quorum member has offered the MYA a deal: support for augmentation debt reform in exchange for the Alliance's silence on a corporate development project that will displace thousands of Shelf residents. Kai is being asked to trade one injustice for another."
    ],
    tags: ["faction", "political", "youth", "activism", "circuit", "education", "employment"]
  }
];

let written = 0;
let skipped = 0;

const allFactions = [
  ...religious,
  ...criminal,
  ...political,
  ...mercenary,
  ...cultural,
  ...supplemental
];

for (const f of allFactions) {
  if (writeFaction(f)) {
    written++;
  } else {
    skipped++;
  }
}

console.log(`\nDone. Written: ${written}, Skipped: ${skipped}, Total existing: ${existing.size}`);
console.log(`Total factions in directory: ${existing.size + written}`);
