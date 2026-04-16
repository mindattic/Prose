const fs = require('fs');
const path = require('path');
const crypto = require('crypto');

const outDir = path.join(__dirname, '..', 'engine', 'data', 'weaponry');
const existing = new Set(fs.readdirSync(outDir));

function genId() {
  return crypto.randomBytes(16).toString('hex');
}

function toFileName(name) {
  return name
    .slice(0, 60)
    .toLowerCase()
    .replace(/['']/g, '')
    .replace(/[^a-z0-9]+/g, '_')
    .replace(/^_|_$/g, '')
    .slice(0, 80) + '.json';
}

const weapons = [
  // ===== PEPPER SPRAY / CHEMICAL SPRAYS (5) =====
  {
    id: genId(),
    name: "SentryGuard PepperShot PS-1 'Sting'",
    type: "weapon",
    aliases: ["Sting", "PS-1", "Pocket Pepper", "The Spritz"],
    category: "self_defense",
    description: "The most common self-defense item carried in GLMZ, the SentryGuard PS-1 is a palm-sized canister of oleoresin capsaicin rated at 4.2 million Scoville Heat Units. A half-second burst produces a cone of atomized irritant effective to three meters, causing immediate lacrimation, mucous membrane inflammation, and temporary blindness lasting eight to twelve minutes. SentryGuard sells more PS-1 units annually than any other single product in the GLMZ personal security market.\n\nThe canister contains twelve bursts and features a flip-top safety to prevent accidental discharge. The formulation includes a UV-reactive dye that marks the attacker's skin for up to seventy-two hours, aiding identification by security forces. At Tier 1, the PS-1 is often the only defensive tool a person can afford, and empty canisters are refilled from bulk pepper concentrate sold in open-air markets — a practice SentryGuard officially condemns but has never taken legal action to stop.\n\nThe PS-1's ubiquity has made it a cultural symbol. Carrying one is not a statement — not carrying one is. GLMZ residents refer to leaving home unarmed as 'going naked,' and the click of a PS-1 safety is one of the city's ambient sounds.",
    manufacturer: "SENTRYGUARD CONSUMER DEFENSE",
    tier_availability: "Tier 1+",
    legality: "Unrestricted",
    base_technologies: ["Oleoresin capsaicin atomization", "UV-reactive marking dye", "Pressurized aerosol delivery"],
    specifications: "capacity: 12 bursts\nrange: 3 meters effective cone\nSHU rating: 4.2 million\nweight: 48 g\ncanister dimensions: 9 cm x 3 cm\neffect duration: 8-12 minutes incapacitation\nUV dye persistence: 72 hours on skin",
    tactical_use: "Point and spray at the face. No training required. The PS-1 is effective against unaugmented attackers and those with standard-grade optics. Augmented individuals with sealed ocular implants may be partially resistant, but the respiratory irritant component still causes distress. Ineffective in strong wind or rain. Best deployed as a first response while creating distance to flee.",
    cultural_context: "SentryGuard has built an entire brand around the PS-1's accessibility. Their slogan — 'Everyone Deserves a Fighting Chance' — appears on transit ads across every tier. In Tier 1 communities, used PS-1 canisters are collected and refilled as a communal resource. Some neighborhoods maintain shared canisters chained to stairwell railings. The PS-1 is so common that muggers factor it into their approach tactics, often attacking from behind or targeting the spray hand first.",
    known_users: [],
    story_hooks: [
      "A batch of counterfeit PS-1 canisters has appeared in Tier 1 markets — they look identical but contain only colored water. Someone is profiting from leaving an entire neighborhood defenseless.",
      "A chemist in Tier 2 has developed a PS-1 refill formula that also contains a contact sedative — people who buy her refills don't realize their spray is now potentially lethal."
    ],
    ammunition_type: ["OC aerosol canister"],
    tags: ["weapon", "self_defense", "non_lethal", "pepper_spray", "chemical", "tier_1", "everyday_carry", "affordable"]
  },
  {
    id: genId(),
    name: "SentryGuard NeuralBurn NB-3 'Brainfreeze'",
    type: "weapon",
    aliases: ["Brainfreeze", "NB-3", "Aug Spray", "Neuro Mace"],
    category: "self_defense",
    description: "An advanced chemical spray formulated specifically to disrupt neural-interface connections in augmented attackers. The NB-3 combines a standard capsaicin base with SentryGuard's proprietary NeuroDisrupt compound — a synthetic irritant that interferes with the electrochemical signaling between biological neural tissue and cybernetic interface ports. When sprayed on exposed skin near BCI connection points, the compound causes temporary signal degradation that manifests as lag, phantom inputs, and sensory static in augmented systems.\n\nThe NB-3 is marketed as the answer to the 'augmentation gap' — the reality that a chemically enhanced or cybernetically augmented mugger has significant advantages over an unaugmented victim. By temporarily disrupting the attacker's augmentations, the spray theoretically levels the playing field. In practice, the effect is inconsistent — military-grade interfaces with sealed housings are largely immune, while consumer-grade open-port installations are significantly affected.\n\nSentryGuard prices the NB-3 at roughly four times the cost of the standard PS-1, putting it at the upper limit of what Tier 2 residents can afford. This pricing has drawn criticism that SentryGuard is essentially selling protection against augmented crime as a luxury product.",
    manufacturer: "SENTRYGUARD CONSUMER DEFENSE",
    tier_availability: "Tier 2+",
    legality: "Licensed",
    base_technologies: ["NeuroDisrupt compound synthesis", "BCI signal interference chemistry", "Dual-action capsaicin-neural formula"],
    specifications: "capacity: 8 bursts\nrange: 2.5 meters directed stream\nSHU rating: 3.8 million (capsaicin component)\nNeuroDisrupt effect: 3-8 minutes BCI degradation\nweight: 62 g\neffectiveness vs sealed BCI: minimal\neffectiveness vs open-port BCI: significant",
    tactical_use: "Target exposed skin near visible BCI ports — the neck, temples, and behind the ears are optimal. The NeuroDisrupt compound needs proximity to neural interface hardware to be effective. Against unaugmented attackers, it functions as a standard pepper spray with slightly reduced potency. Best used as an opening move to degrade the attacker's augmented reflexes before fleeing.",
    cultural_context: "The NB-3 represents a growing market segment: anti-augmentation self-defense. As the augmentation gap widens between those who can afford combat-grade cyberware and those who cannot, products that promise to neutralize that advantage are increasingly popular. Critics call them 'aug-hate in a can,' while proponents argue they are the only equalizer available to unaugmented citizens. SentryGuard walks a careful line in their marketing, never explicitly framing augmented people as threats while clearly implying it.",
    known_users: [],
    story_hooks: [
      "An augmented rights group is suing SentryGuard, claiming the NB-3 marketing constitutes incitement against augmented citizens and that the compound causes permanent damage to certain BCI models.",
      "Someone has been spraying NB-3 on augmented people who aren't committing crimes — a pattern of hate attacks that SentryGuard's PR team is desperately trying to distance from their product."
    ],
    ammunition_type: ["OC + NeuroDisrupt aerosol canister"],
    tags: ["weapon", "self_defense", "non_lethal", "pepper_spray", "chemical", "anti_augmentation", "bci_disruption", "tier_2"]
  },
  {
    id: genId(),
    name: "Kyūsei Chemical FogWall FW-2",
    type: "weapon",
    aliases: ["FogWall", "FW-2", "Cloud Can", "Smoke Pepper"],
    category: "self_defense",
    description: "A defensive spray that prioritizes area denial over direct incapacitation. The FW-2 releases a dense, persistent cloud of micro-encapsulated irritant particles that hang in the air for up to ninety seconds, creating a barrier of chemical fog between the user and their attacker. The cloud is opaque to visible light and most commercial-grade optical enhancement systems, combining visual obstruction with respiratory irritation.\n\nKyūsei Chemical, a Kyoto-based firm that relocated its consumer defense division to GLMZ in 2081, designed the FW-2 after market research showed that most self-defense spray deployments fail because the user panics and sprays wildly rather than targeting the attacker's face. The FW-2 removes the accuracy requirement — the user simply points it at the ground between themselves and the threat and runs the other direction.\n\nThe canister is larger than typical pocket sprays, about the size of a small water bottle, and contains three full-cloud deployments. The micro-encapsulation technology means the irritant particles burst on contact with moisture — eyes, nasal passages, and sweat-dampened skin — while remaining relatively inert on dry surfaces. This reduces collateral contamination of the area after the cloud disperses.",
    manufacturer: "KYUSEI CHEMICAL",
    tier_availability: "Tier 2+",
    legality: "Unrestricted",
    base_technologies: ["Micro-encapsulated irritant particles", "Persistent aerosol suspension", "Moisture-activated capsule rupture"],
    specifications: "capacity: 3 cloud deployments\ncloud radius: 4 meter sphere\ncloud persistence: 60-90 seconds\nweight: 180 g\ncanister dimensions: 15 cm x 5 cm\nvisual obstruction: opaque to visible light and standard-grade optics\nirritation onset: immediate on mucous membrane contact",
    tactical_use: "Deploy at ground level between yourself and the threat, then move away from the cloud. The FW-2 is not designed to incapacitate — it is designed to create space and break line of sight. Effective in enclosed spaces like corridors and transit stations where the cloud fills the available volume. Less effective outdoors in wind. The moisture-activation mechanism means it is particularly effective against sweating or rain-dampened attackers.",
    cultural_context: "The FW-2 has become the preferred self-defense tool for people who know they cannot outfight their attacker and just need to escape. Its Japanese engineering and clean design have given it a reputation as a thoughtful, non-aggressive defensive choice — the self-defense option for people who dislike the idea of self-defense weapons. Kyūsei's marketing emphasizes escape and de-escalation rather than punishment or retaliation.",
    known_users: [],
    story_hooks: [
      "A series of robberies in Tier 3 have been committed using modified FW-2 canisters loaded with a knockout agent instead of irritant — victims wake up stripped of everything valuable.",
      "Kyūsei Chemical is being pressured to add a law enforcement override to the FW-2's formula — a chemical that security forces could deploy to instantly neutralize the cloud, which would also neutralize its value as a defense against corrupt security."
    ],
    ammunition_type: ["Micro-encapsulated irritant fog canister"],
    tags: ["weapon", "self_defense", "non_lethal", "chemical", "area_denial", "fog", "escape_tool", "tier_2"]
  },
  {
    id: genId(),
    name: "Dawnlight Industries PurityMist PM-4 'Angel Breath'",
    type: "weapon",
    aliases: ["Angel Breath", "PM-4", "Holy Spray", "The Purifier"],
    category: "self_defense",
    description: "A premium chemical defense spray marketed to upper-tier residents who want personal protection without the stigma of carrying a weapon. The PM-4 dispenses a precisely metered dose of synthetic capsaicinoid in a fine mist from an elegant brushed-titanium canister designed to resemble a luxury perfume atomizer. The formulation includes a proprietary calming pheromone blend that Dawnlight claims reduces the user's own stress response during deployment.\n\nThe PM-4's active compound is a synthetic analog of capsaicin that Dawnlight developed to avoid the inconsistent potency of natural pepper extracts. The result is a spray with extremely predictable effects: exactly six minutes of incapacitating pain and blindness, followed by complete symptom resolution within twenty minutes. This predictability is the product's key selling point — it incapacitates reliably without causing the prolonged suffering or potential complications associated with cheaper formulations.\n\nDawnlight prices the PM-4 at Φ280 per canister — expensive enough to be aspirational, cheap enough for Tier 3 and above. The canister is refillable at Dawnlight boutiques located in Tier 3-5 commercial districts, where the refill service includes a complimentary deployment technique refresher. The entire experience is designed to feel like a spa visit rather than a weapons purchase.",
    manufacturer: "DAWNLIGHT INDUSTRIES",
    tier_availability: "Tier 3+",
    legality: "Unrestricted",
    base_technologies: ["Synthetic capsaicinoid formulation", "Precision metered dosing", "Calming pheromone integration"],
    specifications: "capacity: 6 precision bursts\nrange: 2 meters fine mist cone\nincapacitation duration: exactly 6 minutes\nfull recovery: 20 minutes\nweight: 85 g\ncanister material: brushed titanium\nrefill cost: Φ45 at Dawnlight boutiques",
    tactical_use: "The PM-4 is designed for close-range encounters — its fine mist disperses quickly beyond two meters. Dawnlight recommends a single burst aimed at the upper face, followed by immediate withdrawal. The spray's predictable six-minute incapacitation window gives the user a known timeframe to reach safety. Not recommended for multiple-attacker scenarios due to limited capacity.",
    cultural_context: "The PM-4 is a status symbol disguised as a self-defense tool. Carrying one signals that you can afford to live in a tier where personal defense is a lifestyle accessory rather than a survival necessity. Dawnlight's boutique refill service has become a social ritual in Tier 3-4 communities — a place to be seen, to demonstrate that you take personal safety seriously without being gauche about it. Lower-tier residents mock the PM-4 as 'rich people's pepper spray,' but its formulation is genuinely superior to budget alternatives.",
    known_users: [],
    story_hooks: [
      "Dawnlight's calming pheromone blend has been found to contain a mild euphoric compound that creates subtle brand loyalty — users feel slightly better every time they handle their PM-4, associating the product with positive emotions.",
      "A Tier 4 socialite deployed her PM-4 against an augmented mugger and it had no effect — the synthetic capsaicinoid doesn't trigger the same receptors as natural capsaicin in individuals with certain genetic modifications."
    ],
    ammunition_type: ["Synthetic capsaicinoid precision mist"],
    tags: ["weapon", "self_defense", "non_lethal", "chemical", "pepper_spray", "luxury", "tier_3", "status_symbol"]
  },
  {
    id: genId(),
    name: "Carrion Defense Works StreetSweeper CS-6 'Gutter Rain'",
    type: "weapon",
    aliases: ["Gutter Rain", "CS-6", "The Drencher", "Acid Spit"],
    category: "self_defense",
    description: "A heavy-duty chemical defense spray designed for the violent reality of Tier 1-2 street life, where attackers may be chemically numbed, heavily augmented, or simply too desperate to be deterred by pain. The CS-6 fires a pressurized stream of Carrion's proprietary Compound 6 — a synthetic irritant combined with a contact adhesive that bonds to skin and continues to burn for up to thirty minutes. The compound cannot be wiped off and is resistant to water, requiring a specific chemical neutralizer to remove.\n\nCarrion makes no attempt to market the CS-6 as a gentle deterrent. The packaging is industrial black with hazard-yellow warning stripes, and the instructions explicitly state that the compound causes second-degree chemical burns on prolonged skin contact. The canister is built to withstand being dropped, stepped on, or submerged, because Carrion's target demographic lives in environments where equipment takes abuse.\n\nThe CS-6 occupies a legal gray area — its effects technically exceed the threshold for 'non-lethal' classification, but GLMZ's self-defense statutes permit 'reasonable force proportional to perceived threat,' and in Tier 1-2, the perceived threat is usually severe enough to justify aggressive chemical deterrents. Law enforcement periodically moves to restrict the CS-6, and Carrion periodically lobbies to prevent it.",
    manufacturer: "CARRION DEFENSE WORKS",
    tier_availability: "Tier 1+",
    legality: "Licensed",
    base_technologies: ["Synthetic persistent irritant chemistry", "Contact adhesive binding agent", "Hardened pressurized delivery system"],
    specifications: "capacity: 4 stream bursts\nrange: 5 meters directed stream\nburn duration: up to 30 minutes without neutralizer\nneutralizer: Carrion CDW-Neut6 (sold separately, Φ15)\nweight: 210 g\ncanister construction: impact-rated polymer shell\nchemical burn severity: first to second degree on prolonged contact",
    tactical_use: "Aim for exposed skin — face, neck, hands. The stream delivery is more wind-resistant than aerosol sprays, making it effective outdoors. The adhesive compound means a single hit continues to cause escalating pain, even if the attacker initially pushes through. Carrion recommends spraying and running — the compound's thirty-minute burn will discourage pursuit. Carry the neutralizer separately in case of accidental self-exposure.",
    cultural_context: "In Tier 1-2, the CS-6 is known as the equalizer you carry when you know pepper spray won't cut it. Its aggressive branding is a deliberate contrast to the sanitized marketing of upscale defense products — Carrion's implicit message is 'we know what you're actually facing out there.' The CS-6 has generated controversy after several incidents where attackers suffered permanent scarring, but Carrion's legal position — that the alternative to chemical burns is often death — has held up in every challenge so far.",
    known_users: [],
    story_hooks: [
      "A vigilante in Tier 1 has been using CS-6 to permanently disfigure people they identify as predators — the compound's adhesive properties make the scarring deliberate and recognizable.",
      "Carrion's Compound 6 formula was leaked online, and home-brewed versions with inconsistent concentrations are causing severe injuries — including several cases of permanent blindness."
    ],
    ammunition_type: ["Compound 6 adhesive irritant stream"],
    tags: ["weapon", "self_defense", "non_lethal", "chemical", "aggressive", "persistent", "tier_1", "street_level"]
  },

  // ===== PERSONAL ALARMS / SONIC DETERRENTS (3) =====
  {
    id: genId(),
    name: "HarborTech SafeScream SS-1 'Banshee'",
    type: "weapon",
    aliases: ["Banshee", "SS-1", "Screamer", "Panic Button"],
    category: "self_defense",
    description: "A keychain-sized personal alarm that emits a 142-decibel omnidirectional sonic burst when activated, producing a sound loud enough to cause immediate pain and disorientation at close range while alerting anyone within a 200-meter radius. The SS-1 activates via a pull-pin mechanism — yank the pin and the device screams until the pin is reinserted or the battery depletes, whichever comes first. The frequency is specifically calibrated to the human pain threshold: a warbling tone between 2,800 and 3,400 Hz that is physically impossible to ignore.\n\nHarborTech designed the SS-1 after analyzing thousands of assault reports and finding that the single most effective deterrent was not pain or incapacitation but attention. Muggers, assailants, and opportunistic criminals in GLMZ rely on isolation and anonymity — the SS-1 eliminates both by making the attack the loudest thing happening within two city blocks.\n\nThe device costs Φ12 and runs on a standard coin cell battery that provides six minutes of continuous output. HarborTech distributes them through convenience stores, transit station kiosks, and community safety programs. In some Tier 1 neighborhoods, they are given away free by mutual aid organizations. The SS-1 is probably the single most widely carried self-defense device in GLMZ, surpassing even the SentryGuard PS-1 in sheer numbers.",
    manufacturer: "HARBORTECH CONSUMER ELECTRONICS",
    tier_availability: "Tier 1+",
    legality: "Unrestricted",
    base_technologies: ["High-output piezoelectric emitter", "Pain-threshold frequency calibration", "Pull-pin fail-safe activation"],
    specifications: "output: 142 dB at 30 cm\nfrequency: 2,800-3,400 Hz warbling\naudible range: 200+ meters\nbattery life: 6 minutes continuous\nweight: 22 g\ndimensions: 4 cm x 2.5 cm x 1.5 cm\npower source: CR2032 coin cell\ncost: Φ12",
    tactical_use: "Pull pin immediately when threatened. Do not wait to confirm danger. The SS-1's effectiveness comes from speed of deployment — the sound begins before the attacker can react. Hold the device away from your own ear if possible. The 142 dB output causes pain at one meter, making it a mild deterrent on its own, but its primary value is drawing attention to the situation. After activation, move toward populated areas while the sound draws witnesses.",
    cultural_context: "The Banshee is GLMZ's great equalizer — no training required, no legal restrictions, and cheap enough that no one has an excuse not to carry one. Its distinctive warbling scream is universally recognized as a distress signal, and cultural norms in most tiers dictate that hearing one obligates bystanders to at least look and record. The sound has become so associated with danger that HarborTech had to issue guidance asking people not to use it for non-emergency purposes after a series of 'Banshee pranks' caused neighborhood-wide panic responses.",
    known_users: [],
    story_hooks: [
      "Someone has been placing modified SS-1 units in public spaces on dead-man timers — they activate at random, creating panic responses that serve as cover for coordinated thefts during the confusion.",
      "A community organizer in Tier 1 has linked multiple Banshees together into a network that creates a rolling wall of sound — an improvised sonic barrier that has proven surprisingly effective at deterring organized criminal incursions into their block."
    ],
    ammunition_type: [],
    tags: ["weapon", "self_defense", "non_lethal", "sonic", "alarm", "affordable", "tier_1", "everyday_carry", "keychain"]
  },
  {
    id: genId(),
    name: "Tessera Industries PanicPulse PP-3 'Migraine'",
    type: "weapon",
    aliases: ["Migraine", "PP-3", "Brain Hammer", "The Headache"],
    category: "self_defense",
    description: "A palm-sized directional sonic weapon that fires a focused infrasonic pulse at 18 Hz, targeting the vestibular system to cause immediate nausea, vertigo, and loss of balance. Unlike omnidirectional alarms that rely on volume, the PP-3 uses frequency to incapacitate — the infrasonic pulse is barely audible but physically devastating, disrupting the inner ear's ability to maintain equilibrium. Targets typically collapse within two seconds of exposure and remain incapacitated for thirty to sixty seconds after the pulse stops.\n\nTessera adapted the PP-3's core technology from their military-grade sonic weapons, miniaturizing the emitter assembly to fit in a device roughly the size of a deck of cards. The civilian version is power-limited to prevent the bone-stress injuries their military products can cause, but the effect on balance and orientation is still severe enough that Tessera includes a wrist lanyard — if the user accidentally points it at themselves, they will fall down.\n\nThe PP-3 operates on a rechargeable cell that provides fifteen three-second pulses per charge. It is significantly more expensive than basic alarms, positioning it as a Tier 2-3 self-defense option for people who want active incapacitation rather than passive deterrence.",
    manufacturer: "TESSERA INDUSTRIES",
    tier_availability: "Tier 2+",
    legality: "Licensed",
    base_technologies: ["Miniaturized infrasonic emitter", "Vestibular disruption targeting", "Directional acoustic focusing"],
    specifications: "frequency: 18 Hz infrasonic pulse\neffective range: 5 meters directional cone\npulse duration: 3-second burst\nincapacitation onset: 1-2 seconds\nrecovery time: 30-60 seconds post-exposure\ncharges per battery: 15 pulses\nweight: 140 g\ndimensions: 9 cm x 6 cm x 2 cm\ncost: Φ340",
    tactical_use: "Point directly at the attacker's head and press the activation button. The PP-3 requires aim — the infrasonic cone is approximately 30 degrees wide. Hold the pulse for the full three seconds if possible. The attacker will experience immediate vertigo and most will fall. Use the incapacitation window to create distance. Do not remain in the pulse cone yourself — stand to the side of the device axis. Ineffective against attackers with sealed acoustic implants or active noise cancellation cyberware.",
    cultural_context: "The PP-3 occupies the middle ground between passive deterrents and active weapons. Its users tend to be people who have experienced situations where an alarm was not enough but carrying a lethal weapon feels disproportionate. Tessera markets it through self-defense instructors and personal safety consultants rather than retail stores, cultivating an image of informed preparedness. The device has developed a following among night-shift workers, delivery runners, and others who regularly traverse empty spaces.",
    known_users: [],
    story_hooks: [
      "A modified PP-3 with the power limiter removed has been used in a series of assaults — the unregulated infrasonic output is causing permanent inner ear damage and chronic vertigo in victims.",
      "Tessera is quietly settling a lawsuit from a user who experienced a seizure after their PP-3 malfunctioned and emitted a continuous pulse — the device's safety shutoff failed."
    ],
    ammunition_type: [],
    tags: ["weapon", "self_defense", "non_lethal", "sonic", "infrasonic", "directional", "vestibular_disruption", "tier_2"]
  },
  {
    id: genId(),
    name: "Meridian Audio Concepts SirenSphere S-7 'Dome'",
    type: "weapon",
    aliases: ["Dome", "S-7", "Sound Cage", "The Bubble"],
    category: "self_defense",
    description: "A throwable sonic deterrent that creates a spherical zone of intolerable sound when deployed. The S-7 is a 6-centimeter rubberized sphere containing a multi-directional speaker array powered by a high-density capacitor. When armed and thrown, impact sensors activate the device on landing, generating a 155-decibel omni-directional sonic field within a three-meter radius. The sound profile cycles through twelve frequencies specifically selected to cause maximum discomfort across different hearing ranges, including frequencies that resonate painfully with common cochlear implant models.\n\nMeridian Audio Concepts developed the S-7 as a 'drop and run' deterrent — the user throws the device at or near the threat, and the resulting wall of sound creates an impassable zone of pain while the user escapes in the opposite direction. The sphere operates for forty-five seconds before the capacitor depletes, after which the device is inert and non-recoverable. At Φ85 per unit, the S-7 is a single-use investment, but repeat customers are common enough that Meridian sells them in packs of three.\n\nThe S-7's impact on augmented individuals with cochlear implants or audio-processing cyberware is disproportionately severe — the resonance frequencies can cause feedback loops in audio hardware that result in incapacitating pain well beyond the physical sound's effect. This has led to complaints from augmented communities that the S-7 is discriminatory in its design.",
    manufacturer: "MERIDIAN AUDIO CONCEPTS",
    tier_availability: "Tier 2+",
    legality: "Licensed",
    base_technologies: ["Multi-directional speaker array miniaturization", "Impact-activated deployment", "Augmentation-resonant frequency cycling"],
    specifications: "output: 155 dB within 3 meter radius\nfrequency cycling: 12 targeted frequencies\nactivation: impact sensor on landing\nduration: 45 seconds\nweight: 95 g\ndiameter: 6 cm\ncost: Φ85 (single), Φ220 (3-pack)\nreusable: no — single deployment",
    tactical_use: "Arm by twisting the top hemisphere 90 degrees, then throw at the ground near the threat. The impact sensor triggers on any surface contact greater than 2G, so a firm toss to the ground is sufficient. Move away from the device before activation — the 155 dB output is harmful to the user within three meters. The S-7 is particularly effective in enclosed spaces where the sound reflects and amplifies. In open spaces, the effective radius may be slightly reduced.",
    cultural_context: "The S-7 has found a niche as the preferred deterrent for people who don't trust their aim with sprays or their nerve with direct confrontation. Throwing something is intuitive and doesn't require pointing at an attacker. Meridian Audio Concepts leans into this psychology with marketing that shows ordinary people — commuters, parents, night-shift workers — deploying the S-7 without breaking stride. The product's augmentation-resonance issue has made it controversial, with some arguing it's a feature and others calling it a design flaw that disproportionately harms the aug-dependent.",
    known_users: [],
    story_hooks: [
      "A street gang has weaponized the S-7 by deploying dozens simultaneously in enclosed transit stations — the overlapping sonic fields create conditions so severe that even filtered security forces cannot enter, allowing the gang to operate freely for the forty-five-second window.",
      "Meridian Audio Concepts is under investigation after a deaf individual with cochlear implants suffered permanent implant damage from an S-7 deployed by a panicking bystander — the device's aug-resonance feature is being called an accessibility rights violation."
    ],
    ammunition_type: [],
    tags: ["weapon", "self_defense", "non_lethal", "sonic", "throwable", "single_use", "area_denial", "tier_2"]
  },

  // ===== STUN DEVICES / ZAPPERS (5) =====
  {
    id: genId(),
    name: "VoltLine MicroTaser MT-2 'Bite'",
    type: "weapon",
    aliases: ["Bite", "MT-2", "Pocket Zapper", "Snap"],
    category: "self_defense",
    description: "A compact contact stun device the size of a cigarette lighter, delivering a 50,000-volt low-amperage electrical discharge through two prong electrodes on the business end. The MT-2 is the most basic stun device available in GLMZ — press it against an attacker, press the button, and the electrical discharge causes involuntary muscle contraction and immediate pain. The low amperage means it is unlikely to cause cardiac events in healthy individuals, though VoltLine's documentation includes the standard disclaimers about pre-existing conditions.\n\nVoltLine manufactures the MT-2 in a facility in Tier 2 using largely automated production, keeping costs low enough that the device retails for Φ35. It recharges via standard micro-cell, and a full charge provides approximately thirty discharges. The device is available in a range of colors and finishes, from utilitarian black to bright patterns — VoltLine recognized early that self-defense devices that people actually carry need to not look like weapons.\n\nThe MT-2's limitation is that it requires direct physical contact — the user must be within arm's reach of the attacker and must press the electrodes against skin or thin clothing. This means the user is already in danger when the device becomes useful, which critics argue makes it a reactionary tool rather than a preventive one. VoltLine's response is that most street attacks begin at close range, and having any deterrent at contact distance is better than having none.",
    manufacturer: "VOLTLINE PERSONAL ELECTRONICS",
    tier_availability: "Tier 1+",
    legality: "Unrestricted",
    base_technologies: ["High-voltage low-amperage discharge", "Micro-cell rechargeable power", "Compact electrode design"],
    specifications: "voltage: 50,000V\namperage: 3.6 milliamps (non-lethal threshold)\nelectrode gap: 1.2 cm\ndischarges per charge: 30\nrecharge time: 45 minutes from depleted\nweight: 38 g\ndimensions: 7 cm x 3 cm x 1.5 cm\ncost: Φ35",
    tactical_use: "Press electrodes firmly against attacker's exposed skin or thin clothing and hold the button for at least one second. Optimal target areas are the neck, armpit, and inner thigh where nerve density is highest. The discharge causes involuntary muscle contraction and pain but does not reliably incapacitate — treat it as a distraction tool that creates a one-to-two-second window to break away. Ineffective through thick clothing, armor, or dermal augmentations.",
    cultural_context: "The MT-2 is carried by millions of GLMZ residents who want something more than an alarm but can't afford or don't want to carry chemical sprays. Its lighter-like form factor means it disappears into a pocket or bag without bulk. VoltLine's marketing is matter-of-fact — no tough-guy imagery, no fear-based messaging, just the simple proposition that Φ35 buys you a chance to get away. The device has become so common that muggers have adapted, wearing thicker clothing and gloves specifically to defeat contact stun devices.",
    known_users: [],
    story_hooks: [
      "A modified MT-2 with boosted amperage has been linked to a cardiac arrest death in Tier 1 — the modification is a simple capacitor swap that any electronics hobbyist could perform.",
      "VoltLine's manufacturing facility has been producing devices with inconsistent voltage output — some units barely function while others deliver dangerous overcharges, suggesting sabotage on the production line."
    ],
    ammunition_type: [],
    tags: ["weapon", "self_defense", "non_lethal", "stun", "electrical", "compact", "affordable", "tier_1", "everyday_carry"]
  },
  {
    id: genId(),
    name: "VoltLine ShockBand SB-4 'Snakebite'",
    type: "weapon",
    aliases: ["Snakebite", "SB-4", "Zap Bracelet", "Stun Bangle"],
    category: "self_defense",
    description: "A wearable stun device disguised as a chunky fashion bracelet, delivering a 75,000-volt discharge through electrode contacts on the outer surface when the wearer makes a fist and presses a concealed activation pad with their thumb. The SB-4 eliminates the primary weakness of handheld stun devices — fumbling for the device while under attack. Because it is already on the wearer's wrist, it is deployable in the same motion as a defensive push or palm strike.\n\nVoltLine's design team included industrial fashion consultants who ensured the SB-4 looks like a statement accessory rather than a weapon. It is available in matte black, brushed silver, rose gold, and seasonal limited editions that consistently sell out. The exterior is a textured composite that conceals the electrode contacts until the device is activated, at which point the contacts extend 2mm from the surface — enough to penetrate thin fabric.\n\nThe SB-4 charges wirelessly on a standard induction pad and provides twenty discharges per charge. A subtle LED on the inner surface — visible only to the wearer — indicates charge level. The device has become particularly popular among young professionals in Tier 2-3 who commute through less secure areas and want protection that integrates into their daily wear without requiring a separate device to remember or carry.",
    manufacturer: "VOLTLINE PERSONAL ELECTRONICS",
    tier_availability: "Tier 2+",
    legality: "Unrestricted",
    base_technologies: ["Wearable electrode integration", "Concealed contact deployment", "Wireless induction charging"],
    specifications: "voltage: 75,000V\namperage: 4.1 milliamps\nelectrode extension: 2mm from surface on activation\ndischarges per charge: 20\ncharging: wireless induction pad\nweight: 64 g\nform factor: bracelet, 7-9 cm adjustable diameter\ncost: Φ180\navailable finishes: matte black, brushed silver, rose gold, seasonal editions",
    tactical_use: "Make a fist to position thumb over the concealed activation pad. Push or strike the attacker with the outside of the wrist where the electrodes are positioned. Hold contact for at least one second. The SB-4 is most effective when used in conjunction with a palm-heel push to the attacker's chest or face — the electrical discharge adds incapacitating pain to the physical impact. The device works through thin clothing but not through jackets or armor.",
    cultural_context: "The SB-4 has succeeded where many self-defense products fail: it has become something people want to wear rather than something they feel obligated to carry. VoltLine's seasonal limited editions generate genuine fashion buzz, and the bracelet has been spotted on GLMZ media personalities who may or may not be sponsored. This normalization of wearable defense technology reflects a broader cultural shift — in GLMZ, carrying protection is not paranoia, it is baseline sensibility dressed up as style.",
    known_users: [],
    story_hooks: [
      "A counterfeit SB-4 ring has been flooding Tier 1 markets with devices that look identical but use dangerously high amperage — they are causing burns and cardiac events, and VoltLine cannot identify the source.",
      "A Tier 3 resident accidentally activated her SB-4 while touching a transit station's biometric scanner — the discharge corrupted the scanner's database, erasing the last six hours of identity logs, which happened to include a person of interest in a corporate espionage case."
    ],
    ammunition_type: [],
    tags: ["weapon", "self_defense", "non_lethal", "stun", "electrical", "wearable", "fashion", "bracelet", "tier_2", "everyday_carry"]
  },
  {
    id: genId(),
    name: "Nimbus Security ShockRing NR-1 'Kiss'",
    type: "weapon",
    aliases: ["Kiss", "NR-1", "Stun Ring", "The Goodbye"],
    category: "self_defense",
    description: "A self-defense ring containing a miniaturized capacitor and twin needle electrodes concealed beneath a decorative bezel. When the wearer rotates the bezel 90 degrees with their opposite hand, the electrodes extend 3mm from the ring's surface, and a slap or grab delivers a 40,000-volt discharge directly into the attacker's skin. The discharge is sufficient to cause involuntary release of a grip, a sharp pain response, and a brief moment of muscle spasm — enough to break free from a grab and create space.\n\nNimbus Security designed the NR-1 specifically for scenarios where the victim has already been grabbed — a wrist seized, an arm held, a shoulder gripped. In these situations, the ring's electrodes are already in contact with the attacker, and activation is immediate. The ring is available in sizes 5 through 13 and in three metals: surgical steel (Φ95), titanium (Φ140), and a gold-plated variant (Φ260) that is functionally identical but aesthetically premium.\n\nThe NR-1 holds eight discharges per charge and recharges via a magnetic contact cradle included with purchase. Its limitation is power — at 40,000 volts with extremely low amperage, it causes pain and surprise but does not incapacitate. Nimbus is explicit in their marketing that the NR-1 buys you one second, and what you do with that second determines the outcome.",
    manufacturer: "NIMBUS SECURITY",
    tier_availability: "Tier 1+",
    legality: "Unrestricted",
    base_technologies: ["Micro-capacitor ring integration", "Retractable needle electrodes", "Magnetic contact charging"],
    specifications: "voltage: 40,000V\namperage: 2.1 milliamps\nelectrode extension: 3mm from bezel surface\ndischarges per charge: 8\ncharging: magnetic contact cradle\nweight: 18 g (steel), 14 g (titanium)\nsizes: 5-13\ncost: Φ95 (steel), Φ140 (titanium), Φ260 (gold-plated)\nactivation: 90-degree bezel rotation",
    tactical_use: "Pre-arm by rotating the bezel before entering high-risk areas. When grabbed, press the ring firmly against the attacker's skin — the palm, inner wrist, or any exposed area. The discharge causes involuntary grip release in most individuals. Immediately pull free and create distance. The NR-1 is a one-second tool — it does not incapacitate, it interrupts. Have a follow-up plan: run, deploy a secondary deterrent, or reach a secure location.",
    cultural_context: "The NR-1 has become particularly popular among women and smaller individuals who are disproportionately targeted for grab-and-hold assaults. Nimbus's marketing features real testimonials from people who credit the ring with helping them escape dangerous situations, and the product has developed a word-of-mouth reputation that outpaces its advertising budget. The gold-plated variant has become a popular gift in Tier 3-4 communities — a 'stay safe' present that doubles as real jewelry.",
    known_users: [],
    story_hooks: [
      "A modified NR-1 with a data-siphon payload has been found — instead of just shocking the attacker, it injects a micro-charge that reads biometric data from skin contact, harvesting identity information during the defensive discharge.",
      "Nimbus Security's founder was attacked despite wearing an NR-1 and is now funding development of a significantly more powerful successor — the question is whether the new device crosses the line from defense to weapon."
    ],
    ammunition_type: [],
    tags: ["weapon", "self_defense", "non_lethal", "stun", "electrical", "ring", "jewelry", "wearable", "tier_1", "anti_grab"]
  },
  {
    id: genId(),
    name: "VoltLine ArcPen AP-2 'Jolt'",
    type: "weapon",
    aliases: ["Jolt", "AP-2", "Shock Pen", "Lightning Stick"],
    category: "self_defense",
    description: "A functional writing instrument that doubles as a 65,000-volt stun device. The AP-2 writes with standard ink cartridges and passes casual inspection as an ordinary ballpoint pen, but pressing and holding the clip for two seconds activates a pair of electrodes concealed in the pen's tip housing. The device delivers a focused discharge through the writing tip, turning a mundane object into a pain-compliance tool that can be deployed from a natural grip without telegraphing intent.\n\nVoltLine positioned the AP-2 for professionals who carry pens as part of their daily routine — office workers, medical staff, couriers, anyone whose hand regularly holds a writing instrument. The pen's weight and balance are engineered to feel natural, and the stun components add only 12 grams to what would otherwise be a standard premium pen. The clip-hold activation requires deliberate intent, preventing accidental discharge during normal writing.\n\nThe AP-2's dual-purpose design has made it one of VoltLine's bestsellers in Tier 2-4, where it is carried by people who want protection without the social signal of carrying an obvious defense device. In workplaces and social settings where visible weapons are inappropriate, the AP-2 provides deniable capability. VoltLine's premium packaging and gift box options suggest they understand exactly who is buying this product and why.",
    manufacturer: "VOLTLINE PERSONAL ELECTRONICS",
    tier_availability: "Tier 2+",
    legality: "Unrestricted",
    base_technologies: ["Dual-function pen/electrode design", "Clip-activated power system", "Concealed stun integration"],
    specifications: "voltage: 65,000V\namperage: 3.2 milliamps\nelectrode location: concealed in pen tip housing\ndischarges per charge: 25\ncharging: USB-C magnetic cap\nink type: standard replaceable cartridge\nweight: 34 g\nlength: 14 cm\ncost: Φ120",
    tactical_use: "Grip the pen in a natural writing hold. Press and hold the clip for two seconds to arm. Jab or press the pen tip against the attacker's exposed skin. The concentrated electrode delivers the full discharge through a small contact area, maximizing pain response. Optimal targets are soft tissue areas: neck, hand, inner arm. The pen grip allows for rapid repeated jabs if the first contact does not create sufficient space to escape.",
    cultural_context: "The AP-2 represents the normalization of concealed defense technology in everyday objects. Its success has spawned a category of 'dual-purpose defense' products, but VoltLine's execution remains the benchmark. In corporate environments where carrying obvious defense devices would raise eyebrows, the AP-2 provides peace of mind in a form factor that sits in a breast pocket without comment. Some employers in Tier 3-4 include them in new-employee welcome kits.",
    known_users: [],
    story_hooks: [
      "An executive used an AP-2 to stun an assailant during a targeted attack in a Tier 4 office tower — security footage showed the pen being deployed so naturally that investigators initially didn't realize a weapon had been used.",
      "A batch of AP-2 units has been found with the voltage regulator bypassed — they deliver a single massive discharge that drains the entire battery, capable of causing cardiac arrest. The modification was done at the factory."
    ],
    ammunition_type: [],
    tags: ["weapon", "self_defense", "non_lethal", "stun", "electrical", "disguised", "pen", "everyday_carry", "tier_2", "concealed"]
  },
  {
    id: genId(),
    name: "Axiom Systems TouchShield TS-3 'Porcupine'",
    type: "weapon",
    aliases: ["Porcupine", "TS-3", "Shock Patch", "Contact Mine"],
    category: "self_defense",
    description: "An adhesive-backed electrical discharge patch that can be applied to clothing, bags, or directly to skin, delivering a 55,000-volt shock to anyone who touches the protected surface. The TS-3 is a thin, flexible circuit printed on a polymer substrate with a pressure-sensitive trigger — any contact pressure above 500 grams activates the discharge. The user's own skin is insulated by the adhesive backing, while the outer surface is the active electrode.\n\nAxiom Systems markets the TS-3 as passive defense — protection that works even when the user cannot actively respond. Patches are commonly applied to purse straps, jacket shoulders, backpack handles, and anywhere a pickpocket or grab-attacker is likely to make contact. The patches are disposable, with each containing a single-discharge capacitor that depletes on first activation. They are sold in packs of five for Φ60.\n\nThe TS-3 has found unexpected popularity among delivery workers and couriers who are frequently targeted for their cargo. Applied to the handles of delivery containers, the patches deter grab-and-run theft with painful consistency. The primary limitation is single-use — once triggered, the patch is spent and must be replaced. Axiom is developing a rechargeable variant, but the current disposable model dominates the market.",
    manufacturer: "AXIOM SYSTEMS",
    tier_availability: "Tier 1+",
    legality: "Unrestricted",
    base_technologies: ["Flexible printed circuit electrodes", "Pressure-sensitive discharge trigger", "Adhesive-insulated user protection"],
    specifications: "voltage: 55,000V\namperage: 2.8 milliamps\ntrigger threshold: 500 grams contact pressure\nuses: single discharge per patch\npatch dimensions: 5 cm x 5 cm\nthickness: 0.4 mm\nweight: 3 g per patch\ncost: Φ60 for 5-pack\nshelf life: 18 months",
    tactical_use: "Apply patches to surfaces most likely to be grabbed during an attack — bag straps, collar area, wrist cuffs, wallet pockets. The TS-3 is a deterrent, not an incapacitator — the shock is painful and surprising but brief. Its value is in discouraging follow-up grabs after the first contact. For maximum coverage, apply multiple patches to different grab points. Replace triggered patches promptly — a spent patch is just a sticker.",
    cultural_context: "The TS-3 has introduced the concept of 'passive defense zoning' to personal security — making your own body and belongings a hazard to touch without permission. This idea resonates strongly in a city where personal space is constantly violated by crowds, predators, and opportunists. Some Tier 1-2 residents apply patches to their children's school bags and jackets, normalizing from a young age the idea that one's body and property should be defended even passively.",
    known_users: [],
    story_hooks: [
      "A Tier 2 market vendor has been found applying TS-3 patches to merchandise to prevent shoplifting — a customer with a pacemaker was hospitalized after handling a patched product, raising questions about liability when passive defense harms innocent people.",
      "Someone has developed bootleg TS-3 patches with dramatically increased voltage that are being sold as genuine — the counterfeits are visually identical but deliver discharges that cause burns and tissue damage."
    ],
    ammunition_type: [],
    tags: ["weapon", "self_defense", "non_lethal", "stun", "electrical", "passive", "adhesive", "disposable", "tier_1", "anti_grab"]
  },

  // ===== DEFENSIVE BARRIERS (3) =====
  {
    id: genId(),
    name: "Aegis Personal Systems BarrierBand BB-5 'Cocoon'",
    type: "weapon",
    aliases: ["Cocoon", "BB-5", "Body Shield", "The Bubble"],
    category: "self_defense",
    description: "A belt-worn personal electromagnetic barrier that generates a brief repulsive field on activation, physically pushing back anyone within a one-meter radius with approximately 200 newtons of force — equivalent to a hard shove from a large adult. The BB-5 does not create a persistent shield; instead, it fires a single omnidirectional electromagnetic pulse that interacts with the iron content in blood and the metallic components of clothing, cyberware, and weapons to create a momentary repulsive wave.\n\nAegis developed the BB-5 from military crowd-control technology, scaling it down to a consumer wearable that clips onto a standard belt or waistband. The device is roughly the size of a smartphone and weighs 240 grams, most of which is the high-density capacitor that stores the charge for the repulsive pulse. A full charge provides three activations, with a fifteen-second recharge delay between pulses. The activation button is positioned for thumb access on the wearer's hip.\n\nThe BB-5 is Aegis's flagship consumer product and represents the most affordable personal barrier technology available. At Φ850, it is beyond Tier 1 budgets but accessible to Tier 2-3 residents who prioritize personal safety investments. The device has limitations — the repulsive force decreases with distance, is less effective against individuals with minimal metallic content (unaugmented, no metal jewelry or closures), and the three-pulse capacity means it is a short-duration deterrent rather than sustained protection.",
    manufacturer: "AEGIS PERSONAL SYSTEMS",
    tier_availability: "Tier 2+",
    legality: "Licensed",
    base_technologies: ["Electromagnetic repulsion field generation", "High-density capacitor miniaturization", "Metallic-content interaction physics"],
    specifications: "repulsive force: ~200 N at 0.5 meters\neffective radius: 1 meter\npulses per charge: 3\nrecharge delay between pulses: 15 seconds\nfull recharge time: 2 hours\nweight: 240 g\ndimensions: 14 cm x 7 cm x 2 cm\nmounting: belt clip or waistband\ncost: Φ850",
    tactical_use: "Activate when an attacker closes to within one meter. The repulsive pulse will push them back approximately one to two meters depending on their mass and metallic content. Use the created distance to flee or deploy secondary deterrents. The fifteen-second recharge between pulses means the BB-5 is best used as an opening move rather than sustained defense. Against multiple attackers, the omnidirectional pulse affects all individuals within range simultaneously, including the user's companions — warn allies before activation.",
    cultural_context: "The BB-5 represents the democratization of barrier technology that was previously available only to corporate executives and VIPs. Aegis markets it as 'the first push back' — a philosophical statement about personal space in a city that constantly violates it. The device has become a symbol of middle-tier aspiration: affording a BB-5 means you've reached a level of economic security where you can invest in not being touched against your will.",
    known_users: [],
    story_hooks: [
      "A BB-5 activation in a crowded transit car sent twelve people stumbling and caused a chain-reaction injury pile-up — the user claims they were being pickpocketed, but the victims are filing suit for the collateral damage.",
      "Aegis is quietly recalling a production batch after discovering the electromagnetic pulse interferes with certain models of cardiac pacemaker at close range — the recall notice was sent privately rather than publicly to avoid brand damage."
    ],
    ammunition_type: [],
    tags: ["weapon", "self_defense", "non_lethal", "barrier", "electromagnetic", "wearable", "belt", "tier_2", "push_field"]
  },
  {
    id: genId(),
    name: "Sterling-Nakamura PersonalAegis PA-7 'Rampart'",
    type: "weapon",
    aliases: ["Rampart", "PA-7", "Pocket Shield", "Wall"],
    category: "self_defense",
    description: "A handheld hardlight projector that generates a 60-centimeter-diameter disc of semi-solid photonic matter, functioning as a personal shield capable of deflecting physical blows, absorbing low-velocity projectiles, and blocking chemical sprays. The PA-7 is the first consumer-grade hardlight product, adapted from Sterling-Nakamura's military hardlight barrier systems with drastically reduced power and duration to meet civilian safety standards and battery constraints.\n\nThe shield disc projects from a grip-activated emitter roughly the size of a flashlight handle. When activated, the disc materializes in approximately 0.3 seconds and maintains coherence for up to eight seconds per activation, with four activations per charge. The hardlight surface has the approximate stopping power of 6mm polycarbonate — sufficient to block a punch, a knife slash, or a low-velocity flechette round, but not rated against standard firearms ammunition or high-energy weapons.\n\nSterling-Nakamura prices the PA-7 at Φ2,400, firmly in the Tier 3-4 market segment. The device is marketed through personal security consultants and high-end lifestyle retailers, emphasizing its technological sophistication and the prestige of carrying hardlight technology. The limited eight-second activation window per use means the PA-7 is a reactive tool — something deployed in the critical seconds of an encounter rather than a sustained defensive system.",
    manufacturer: "STERLING-NAKAMURA",
    tier_availability: "Tier 3+",
    legality: "Licensed",
    base_technologies: ["Consumer-grade hardlight photonic projection", "Semi-solid photonic matter generation", "Grip-activated rapid deployment"],
    specifications: "shield diameter: 60 cm disc\nactivation time: 0.3 seconds\nshield duration: 8 seconds per activation\nactivations per charge: 4\nstopping power: equivalent to 6mm polycarbonate\nweight: 320 g\nlength: 16 cm (handle)\nrecharge time: 4 hours\ncost: Φ2,400",
    tactical_use: "Deploy the shield between yourself and the immediate threat. The disc projects perpendicular to the handle axis, so hold the PA-7 like a flashlight pointed at the attacker. The eight-second window requires decisive action — deploy the shield, then immediately move to cover or escape. Against melee attackers, the hardlight disc can be used to physically push back — the surface is rigid enough to strike with. Not effective against firearms; do not rely on it to stop bullets.",
    cultural_context: "The PA-7 is the first time hardlight technology has appeared in a consumer product, and Sterling-Nakamura has leveraged this heavily in marketing. Carrying a PA-7 is a statement that you have access to the cutting edge of materials science in your pocket. Tech enthusiasts and early adopters drove initial sales, but the device has found practical adoption among Tier 3-4 residents who transit through areas where knife crime and physical assault are common. The brief shield duration is a frequent criticism, but Sterling-Nakamura frames it as a design feature — the PA-7 gives you eight seconds, and eight seconds is enough to change an outcome.",
    known_users: [],
    story_hooks: [
      "A PA-7 was used to block a knife attack on a corporate executive, and the attacker's blade shattered against the hardlight surface — fragments injured bystanders, raising questions about whether defensive hardlight creates secondary hazards.",
      "Sterling-Nakamura's military division is concerned that civilian PA-7 units are being reverse-engineered to understand hardlight emitter construction — consumer devices are appearing in black-market workshops where they are being studied for weaponization."
    ],
    ammunition_type: [],
    tags: ["weapon", "self_defense", "non_lethal", "barrier", "hardlight", "shield", "handheld", "tier_3", "premium"]
  },
  {
    id: genId(),
    name: "Aegis Personal Systems WardCloak WC-2 'Ghost'",
    type: "weapon",
    aliases: ["Ghost", "WC-2", "Vanish Coat", "Cloak Shield"],
    category: "self_defense",
    description: "A lightweight jacket with integrated micro-emitters in the lining that, when activated, projects a localized visual distortion field around the wearer. The WC-2 does not render the user invisible — instead, it creates a visual 'smear' effect that makes the wearer's outline indistinct and difficult to track, similar to looking at a figure through heavily textured glass. The distortion is most effective in low-light conditions and against optical systems that rely on edge detection algorithms.\n\nAegis developed the WC-2 after market research showed that many self-defense encounters could be avoided entirely if the potential victim was simply harder to see. The jacket's distortion field runs on a distributed battery network woven into the garment's lining, providing twelve minutes of continuous distortion on a full charge. The activation switch is inside the left pocket, accessible without removing the hand. The jacket itself is a nondescript urban design available in charcoal, navy, and dark green.\n\nThe WC-2 is priced at Φ1,600, positioning it as a premium tier investment. Its primary market is people who commute through high-risk areas during low-light hours — night-shift workers, late-service employees, anyone who regularly walks through places where being visible makes you a target. The distortion effect has limitations: it is ineffective in bright light, does not defeat thermal imaging, and the micro-emitter grid creates a faint shimmer visible to anyone who knows what to look for.",
    manufacturer: "AEGIS PERSONAL SYSTEMS",
    tier_availability: "Tier 3+",
    legality: "Licensed",
    base_technologies: ["Distributed micro-emitter visual distortion", "Edge-detection algorithm disruption", "Wearable photonic interference grid"],
    specifications: "distortion type: visual edge disruption\neffective conditions: low-light, artificial lighting\nduration: 12 minutes continuous\ncharging: removable battery pack, 3-hour charge\njacket weight: 680 g (including electronics)\navailable sizes: XS-3XL\navailable colors: charcoal, navy, dark green\ncost: Φ1,600\nlimitations: ineffective in daylight, does not defeat thermal imaging",
    tactical_use: "Activate before entering high-risk areas rather than in response to a threat — the distortion needs to be active before the attacker selects you as a target. Move steadily through low-light areas; the distortion is most effective when the wearer is in motion, as stationary silhouettes are easier to resolve through the interference. The WC-2 is a prevention tool, not a combat tool — if an attacker has already engaged, the distortion provides minimal advantage.",
    cultural_context: "The WC-2 has introduced a new concept to GLMZ's personal defense market: avoidance as defense. Rather than tools that react to attacks, it prevents targeting entirely. This philosophy has resonated with residents who are tired of the escalation cycle where better defense tools lead to more aggressive attackers. Some urban safety advocates call the WC-2 the most ethical defense product on the market because it prevents conflict rather than winning it. Critics note that only people who can afford Φ1,600 get to be invisible.",
    known_users: [],
    story_hooks: [
      "Security forces have complained that WC-2 wearers are invisible to their patrol algorithms, creating blind spots in surveillance coverage that criminals have begun to exploit — not by committing crimes in the jackets, but by mapping the blind spots the jackets create.",
      "Aegis has received reports that a competitor has reverse-engineered the micro-emitter grid and is selling distortion panels that can be sewn into any garment — destroying Aegis's monopoly on wearable visual distortion."
    ],
    ammunition_type: [],
    tags: ["weapon", "self_defense", "non_lethal", "barrier", "visual_distortion", "wearable", "jacket", "stealth", "tier_3", "avoidance"]
  },

  // ===== ESCAPE TOOLS (3) =====
  {
    id: genId(),
    name: "Crucible Industries SmokePop SP-1 'Curtain Call'",
    type: "weapon",
    aliases: ["Curtain Call", "SP-1", "Smoke Ball", "Poof"],
    category: "self_defense",
    description: "A golf-ball-sized smoke capsule that detonates on sharp impact, releasing a dense cloud of opaque white smoke that fills approximately 25 cubic meters within two seconds. The SP-1 uses a binary chemical system — two chambers separated by a breakable membrane mix on impact, producing an exothermic reaction that generates a thick, non-toxic smoke with light-scattering particles that defeat standard and infrared optical systems. The cloud persists for twenty to forty seconds depending on air movement.\n\nCrucible Industries adapted the SP-1 from military smoke-screen technology for the civilian self-defense market. The capsule is inert until impact — it can be carried in a pocket, dropped, or jostled without risk of accidental detonation. The impact threshold is calibrated to approximately 3G of sudden deceleration, meaning a firm throw at the ground or against a wall is sufficient, but dropping from pocket height is not. Each capsule is single-use and sold in packs of three for Φ45.\n\nThe SP-1 has become one of the most popular escape tools in GLMZ, valued for its simplicity and reliability. There is no aiming requirement, no activation sequence, and no skill involved — throw it at the ground and run in the other direction. Crucible's marketing slogan — 'Exit Stage Left' — has become common slang for escaping a dangerous situation.",
    manufacturer: "CRUCIBLE INDUSTRIES",
    tier_availability: "Tier 1+",
    legality: "Unrestricted",
    base_technologies: ["Binary chemical smoke generation", "Impact-triggered membrane rupture", "IR-scattering particle suspension"],
    specifications: "smoke volume: 25 cubic meters in 2 seconds\ncloud persistence: 20-40 seconds\nimpact threshold: 3G deceleration\ndiameter: 4.3 cm\nweight: 35 g per capsule\ncost: Φ45 per 3-pack\ntoxicity: non-toxic, mild respiratory irritation with prolonged exposure\noptical defeat: visible light and standard infrared",
    tactical_use: "Throw the capsule hard at the ground between yourself and the attacker. Do not throw it at the attacker — you want the smoke between you, not around them. Begin running before the capsule hits the ground; the smoke generates fast enough that you will be obscured by the time the attacker reacts. Move perpendicular to your last known direction of travel if possible. The twenty-second window is sufficient to reach cover or a populated area if you run immediately.",
    cultural_context: "The SP-1 has made 'smoke bombing' a verb in GLMZ. 'She smoke-bombed out of there' means someone escaped a bad situation cleanly. The capsules are carried by runners, couriers, and anyone whose survival strategy is speed rather than confrontation. Crucible Industries has leaned into the theatrical associations — their packaging features stage curtain imagery, and limited editions come in colored-smoke variants (red, blue, green) that are functionally identical but aesthetically dramatic.",
    known_users: [],
    story_hooks: [
      "A coordinated robbery crew has been deploying dozens of SP-1 capsules simultaneously in commercial districts, creating city-block-sized smoke screens that disable security cameras while the crew operates inside the cloud.",
      "Someone has modified SP-1 capsules to include a persistent marking dye that is invisible in the smoke but stains everything it touches — an anti-theft measure that Crucible denies manufacturing."
    ],
    ammunition_type: ["Binary chemical smoke capsule"],
    tags: ["weapon", "self_defense", "non_lethal", "smoke", "escape", "throwable", "single_use", "tier_1", "affordable"]
  },
  {
    id: genId(),
    name: "Tessera Industries FlashSnap FS-2 'Blindside'",
    type: "weapon",
    aliases: ["Blindside", "FS-2", "Flash Pop", "Whiteout"],
    category: "self_defense",
    description: "A thumb-sized civilian flashbang device that produces a disorienting burst of light and sound sufficient to cause temporary blindness and auditory confusion without the concussive force or permanent damage potential of military-grade flashbang grenades. The FS-2 activates by pulling a ring tab, producing a 3-million-candela flash and a 130-decibel crack after a 1.5-second delay. The effect on unprotected individuals within five meters is approximately four seconds of visual whiteout and eight seconds of auditory ringing.\n\nTessera designed the FS-2 with strict output limits to keep it within civilian defense regulations. The flash intensity is high enough to overwhelm unfiltered human eyes and most consumer-grade optical augmentations, but falls below the threshold that causes retinal damage in healthy eyes. The acoustic burst is painful but does not cause the pressure-wave injuries associated with military concussion grenades. The 1.5-second delay allows the user to throw the device, close their eyes, and cover their ears.\n\nThe FS-2 is sold individually at Φ30 and is popular enough that convenience stores in Tier 1-3 stock them alongside candy and batteries. The device has a shelf life of five years and requires no maintenance. Its simplicity has made it one of the default self-defense tools for people who cannot afford reusable electronic devices — Φ30 for one use that might save your life is an easy calculation.",
    manufacturer: "TESSERA INDUSTRIES",
    tier_availability: "Tier 1+",
    legality: "Unrestricted",
    base_technologies: ["Controlled photonic burst generation", "Civilian-rated acoustic pressure output", "Time-delayed chemical ignition"],
    specifications: "flash output: 3 million candela\nacoustic output: 130 dB at 1 meter\ndelay: 1.5 seconds after ring-pull\neffective radius: 5 meters\nblinding duration: ~4 seconds\nauditory disruption: ~8 seconds\nweight: 12 g\ndimensions: 3 cm x 1.5 cm diameter\ncost: Φ30\nshelf life: 5 years",
    tactical_use: "Pull the ring tab, throw the device toward the threat, close your eyes tightly, and cover your ears. After the 1.5-second delay, the flash and crack will disorient anyone within five meters who was not prepared. Open your eyes and run immediately — you have approximately four seconds before the attacker's vision clears. Combine with a SmokePop for extended concealment: flash to disorient, smoke to obscure your escape route.",
    cultural_context: "The FS-2 has become as commonplace as cigarette lighters in GLMZ's lower tiers. People carry them in pockets, on keychains, and in the bottom of bags alongside their other daily essentials. Tessera's success with the FS-2 has spawned numerous imitations, some of which exceed civilian output limits and cause genuine eye damage. The distinctive 'crack' of an FS-2 detonation is common enough that experienced GLMZ residents can identify it by sound and know to look away before the flash.",
    known_users: [],
    story_hooks: [
      "A batch of counterfeit FS-2 devices with military-grade flash output has blinded three people permanently — the counterfeits are visually identical to legitimate units and are circulating in the same retail channels.",
      "A creative thief has developed a technique using FS-2 devices to defeat biometric locks — the flash is intense enough to overload the retinal scanner's sensor, causing it to default to an unlocked state."
    ],
    ammunition_type: ["Chemical flash-bang capsule"],
    tags: ["weapon", "self_defense", "non_lethal", "flash", "escape", "throwable", "single_use", "tier_1", "affordable"]
  },
  {
    id: genId(),
    name: "NovaTread CaltroDisk CD-1 'Scatterfoot'",
    type: "weapon",
    aliases: ["Scatterfoot", "CD-1", "Spike Strip", "Runner's Friend"],
    category: "self_defense",
    description: "A palm-sized disc that, when thrown on the ground, breaks apart into eighteen spring-loaded micro-caltrops that scatter across approximately four square meters of surface. Each micro-caltrop is a 2-centimeter tetrahedral spike made of hardened polymer — sharp enough to puncture standard footwear soles and embed in the foot, but designed to break off at the base rather than cause deep penetration wounds. The intent is to make pursuit painful and slow rather than to cause serious injury.\n\nNovaTread developed the CD-1 specifically for the 'run and scatter' self-defense scenario — the user throws the disc behind them while fleeing, forcing the pursuer to either stop, navigate around the caltrop field, or step through it and suffer multiple foot punctures. The caltrops are bright orange, making them visible enough that a cautious pursuer will slow down to avoid them, while a reckless pursuer will hit several at full speed.\n\nThe CD-1 is sold in packs of two for Φ40. NovaTread includes a small collection magnet in each pack for retrieval of deployed caltrops — the polymer construction means they are not ferromagnetic, but each caltrop contains a small iron bead for magnetic cleanup. GLMZ municipal codes require deployed caltrops to be collected within one hour to avoid hazard to pedestrians, a regulation that is effectively unenforceable given the circumstances under which they are typically deployed.",
    manufacturer: "NOVATREAD TACTICAL CONSUMER",
    tier_availability: "Tier 1+",
    legality: "Licensed",
    base_technologies: ["Spring-loaded caltrop dispersal mechanism", "Breakaway-tip polymer spike design", "Magnetic recovery bead embedding"],
    specifications: "caltrops per disc: 18\nscatter radius: ~4 square meters\ncaltrop size: 2 cm tetrahedral\ncaltrop material: hardened polymer with iron recovery bead\npenetration depth: 4-8 mm (limited by breakaway tip)\nweight: 55 g per disc\ndisc diameter: 6 cm\ncost: Φ40 per 2-pack\ncolor: bright orange (high visibility)\nrecovery: included magnetic collection tool",
    tactical_use: "While running from a pursuer, throw the CD-1 disc hard at the ground behind you. The impact breaks the disc housing and the spring mechanism scatters the caltrops in a fan pattern. Do not throw backwards over your shoulder — throw down and behind at a 45-degree angle for optimal scatter. The bright orange color is intentional: a visible hazard that causes a pursuer to hesitate is as valuable as an invisible one they step on. Do not use in areas where barefoot pedestrians are common.",
    cultural_context: "The CD-1 has become the signature tool of GLMZ's runner culture — not recreational runners, but the delivery couriers, message carriers, and independent operators whose job is to move quickly through hostile territory. The orange caltrop scatter has become a visual shorthand in street art and graffiti for escape and freedom. NovaTread sponsors community 'scatter drills' in Tier 1-2 neighborhoods, teaching residents escape techniques that combine caltrops, smoke, and flash devices for maximum survival probability.",
    known_users: [],
    story_hooks: [
      "A modified CD-1 has appeared with caltrops that carry a contact irritant — the spikes not only puncture but deliver a chemical payload that causes the wound to burn intensely, making it impossible to continue running even after removing the caltrop.",
      "A Tier 1 neighborhood has started deploying CD-1 caltrops permanently across certain alleyways as improvised perimeter defense — the orange field has become a boundary marker that local residents navigate around but outsiders stumble into."
    ],
    ammunition_type: ["Polymer micro-caltrop disc"],
    tags: ["weapon", "self_defense", "non_lethal", "caltrops", "escape", "throwable", "area_denial", "single_use", "tier_1", "runner"]
  },

  // ===== ANTI-GRAB DEVICES (3) =====
  {
    id: genId(),
    name: "Meridian Weartech ShockWeave SW-3 'Thistle'",
    type: "weapon",
    aliases: ["Thistle", "SW-3", "Zap Jacket", "Don't Touch"],
    category: "self_defense",
    description: "A jacket with an electrically conductive outer fabric layer that delivers a 45,000-volt discharge to anyone who grabs the wearer. The ShockWeave system uses a grid of flexible conductive fibers woven into the garment's exterior, connected to a rechargeable battery pack in the jacket's lining. When the wearer activates the system via a switch inside the left sleeve, any firm grip on the jacket's exterior completes a circuit between two conductive zones and delivers a painful shock.\n\nMeridian Weartech designed the SW-3 for people who face frequent physical intimidation — primarily in Tier 1-2 environments where grabbing, pushing, and manhandling are daily hazards. The jacket looks like an ordinary urban outerwear garment; the conductive fibers are indistinguishable from standard fabric to the eye and touch at casual contact. The discharge only triggers with sustained grip pressure above a threshold that eliminates accidental activation from brushing past someone in a crowd.\n\nThe SW-3 retails at Φ420 and is rechargeable via a standard wall outlet, providing approximately fifty discharges per charge. The jacket is available in four sizes and two colors (charcoal and black), and the electrical components are removable for washing. Meridian Weartech's warranty explicitly does not cover damage from the wearer being grabbed by multiple attackers simultaneously — the system can deliver to multiple contact points but depletes the battery proportionally.",
    manufacturer: "MERIDIAN WEARTECH",
    tier_availability: "Tier 2+",
    legality: "Licensed",
    base_technologies: ["Conductive fabric integration", "Pressure-triggered circuit completion", "Flexible electrode grid weaving"],
    specifications: "voltage: 45,000V\namperage: 3.0 milliamps\ntrigger threshold: sustained grip above 800 grams\ndischarges per charge: ~50\ncharging: standard wall outlet, 2 hours\nweight: 720 g (jacket with electronics)\navailable sizes: S, M, L, XL\navailable colors: charcoal, black\ncost: Φ420\nelectronics: removable for washing",
    tactical_use: "Activate the system before entering high-risk areas using the sleeve switch. When grabbed, the attacker receives an immediate shock that causes involuntary grip release. The system resets in approximately one second, ready for subsequent grabs. The SW-3 is most effective as a surprise — attackers who know about electrified clothing will adjust their approach. Do not activate in rain, as water on the jacket surface can create unintended discharge paths.",
    cultural_context: "The SW-3 has normalized the concept of the body as a defended perimeter. In communities where it is common, a new social dynamic has emerged: the respectful distance people maintain around someone who might be wearing electrified clothing. This 'shock space' has been praised by personal autonomy advocates and criticized by those who argue it further isolates individuals in an already atomized city. The jacket has also created a secondary market for insulated gloves among security personnel who need to physically restrain suspects.",
    known_users: [],
    story_hooks: [
      "A Tier 1 community has pooled resources to buy SW-3 jackets for their most vulnerable members — the sudden appearance of electrified clothing in a neighborhood previously known as an easy target has disrupted local predatory dynamics.",
      "Someone has hacked the SW-3's trigger threshold, turning it from a defense against grabs into a weapon that discharges on any contact, including accidental brushing — several bystanders have been shocked in crowded transit."
    ],
    ammunition_type: [],
    tags: ["weapon", "self_defense", "non_lethal", "electrical", "wearable", "jacket", "anti_grab", "tier_2", "everyday_carry"]
  },
  {
    id: genId(),
    name: "VoltLine SpineBurst SB-7 'Hedgehog'",
    type: "weapon",
    aliases: ["Hedgehog", "SB-7", "Spike Back", "Prick Vest"],
    category: "self_defense",
    description: "A vest with retractable polymer spines embedded in the outer surface of the back and shoulders — the areas most commonly targeted in rear-approach assaults. When the wearer activates the system via a squeeze switch in either hand, twenty-four spines extend 3 centimeters from the vest surface. The spines are blunt-tipped polymer rods coated with a contact irritant — they do not puncture skin but deliver a sharp poking pain combined with a chemical burn sensation that makes grabbing or holding the wearer intensely unpleasant.\n\nVoltLine designed the SB-7 to address the most common assault vector in GLMZ: the rear approach. Analysis of security footage from thousands of street assaults showed that the majority begin with a grab from behind — shoulder, collar, hood, or backpack strap. The SB-7 makes all of these grab points hazardous. The spines retract automatically when the squeeze switch is released, allowing the wearer to sit, lean against walls, and interact normally when the system is not active.\n\nThe vest is worn under an outer layer and is invisible when the spines are retracted. When extended, the spines are visible through thin outer garments but not through jackets or coats. The contact irritant coating lasts approximately ninety days before requiring reapplication — VoltLine sells recoating kits for Φ25. The vest itself costs Φ310 and is available in sizes that accommodate a range of body types.",
    manufacturer: "VOLTLINE PERSONAL ELECTRONICS",
    tier_availability: "Tier 2+",
    legality: "Licensed",
    base_technologies: ["Retractable polymer spine mechanism", "Contact irritant coating", "Squeeze-activated deployment"],
    specifications: "spine count: 24\nspine length: 3 cm extended\nspine tip: blunt polymer with contact irritant coating\nirritant duration: burning sensation for 10-15 minutes\nirritant recoating interval: 90 days\nactivation: squeeze switch (either hand)\nweight: 280 g\ncost: Φ310 (vest), Φ25 (recoating kit)\nspine retraction: automatic on switch release",
    tactical_use: "Activate the spines when you sense a threat approaching from behind. The spines make it painful to grab, hold, or restrain the wearer from behind. If grabbed before activation, the sudden extension of spines into the attacker's grip is startling enough to cause involuntary release. After the attacker releases, the irritant coating continues to cause pain for ten to fifteen minutes, discouraging re-engagement. Turn to face the attacker after the initial grab is broken to deny further rear access.",
    cultural_context: "The SB-7 has contributed to a growing culture of 'defensive dressing' in GLMZ — choosing clothing based on its protective capabilities rather than purely its appearance. The vest is popular among people who work in exposed environments: street vendors, transit maintenance workers, and anyone who regularly has their back to public space. The visible spine extension has also been adopted as an intimidation display by some wearers, activating the spines preemptively to signal 'don't try it.'",
    known_users: [],
    story_hooks: [
      "The contact irritant used on SB-7 spines has been found to cause permanent skin discoloration in individuals with certain genetic profiles — VoltLine claims the correlation is not causation, but affected communities disagree.",
      "A street-level modification replaces the SB-7's blunt polymer spines with sharpened metal ones — turning a defensive deterrent into a concealed weapon that causes serious puncture wounds."
    ],
    ammunition_type: [],
    tags: ["weapon", "self_defense", "non_lethal", "anti_grab", "spines", "wearable", "vest", "irritant", "tier_2"]
  },
  {
    id: genId(),
    name: "Axiom Systems DermaShock DS-4 'Nettle'",
    type: "weapon",
    aliases: ["Nettle", "DS-4", "Shock Patch", "Skin Zapper"],
    category: "self_defense",
    description: "Adhesive patches applied directly to the skin that deliver a 30,000-volt contact discharge when touched by another person's bare skin. The DS-4 uses the body's natural bioelectric field as a circuit component — the wearer's skin provides the ground path, and the patch's outer surface is the active electrode. When another person's skin contacts the patch, their body completes the circuit, receiving the full discharge. The patches are flesh-toned and nearly invisible when applied, designed to be placed on commonly grabbed areas: forearms, upper arms, shoulders, and the back of the neck.\n\nAxiom developed the DS-4 as the most discreet anti-grab defense available. Unlike electrified clothing or visible deterrents, the patches are undetectable until triggered. Each patch contains a micro-capacitor that provides a single discharge, after which the patch is spent. They adhere for up to twelve hours before the medical-grade adhesive degrades, and they are waterproof and sweat-resistant.\n\nThe DS-4 is sold in sheets of ten patches for Φ35. Their disposable nature and low cost have made them popular across all tiers, from Tier 1 residents who apply them before walking through dangerous areas to Tier 4 professionals who wear them to business meetings where unwanted physical contact is a concern. Axiom's marketing is deliberately vague about use cases, allowing customers to project their own needs onto the product.",
    manufacturer: "AXIOM SYSTEMS",
    tier_availability: "Tier 1+",
    legality: "Unrestricted",
    base_technologies: ["Bioelectric circuit integration", "Micro-capacitor single-discharge design", "Medical-grade conductive adhesive"],
    specifications: "voltage: 30,000V\namperage: 1.8 milliamps\ndischarge trigger: skin-to-skin contact through patch\nuses: single discharge per patch\npatch dimensions: 4 cm x 4 cm\nthickness: 0.3 mm\nadhesion duration: up to 12 hours\nweight: 2 g per patch\ncost: Φ35 per sheet of 10\nwater resistance: waterproof",
    tactical_use: "Apply patches to areas most likely to be grabbed: forearms (for wrist-grab defense), shoulders (for rear-grab defense), and the back of the neck (for headlock defense). Space patches to cover the most probable contact points. The single-discharge nature means each patch defends against one grab. Apply multiple patches for multi-contact protection. The patches trigger on bare skin contact only — they will not discharge through thick gloves, though thin fabric may allow partial discharge.",
    cultural_context: "The DS-4 has introduced the concept of the body itself as a defended surface. In Tier 1-2 communities, applying patches before going out has become as routine as checking the weather. The patches have also found an unexpected market in nightlife districts, where they are used to deter unwanted touching in crowded venues — a Φ3.50 patch that shocks someone who grabs you without consent has resonated powerfully with communities tired of physical boundary violations.",
    known_users: [],
    story_hooks: [
      "Medical facilities have reported patients arriving with DS-4 patches still active — healthcare workers have been shocked while examining patients, creating a new workplace hazard in emergency medicine.",
      "A nightclub in Tier 3 has started selling DS-4 patches at the door alongside drink tickets — the club's assault incidents dropped dramatically, but the patches have also been used offensively by patrons shocking people they simply dislike."
    ],
    ammunition_type: [],
    tags: ["weapon", "self_defense", "non_lethal", "electrical", "anti_grab", "adhesive", "disposable", "skin_worn", "tier_1", "discreet"]
  },

  // ===== BCI DEFENSE TOOLS (3) =====
  {
    id: genId(),
    name: "NullPoint Technologies NeuroGuard NG-1 'Static'",
    type: "weapon",
    aliases: ["Static", "NG-1", "Brain Shield", "Null Field"],
    category: "self_defense",
    description: "A pendant-sized device that generates a localized electromagnetic interference field designed to disrupt BCI communications within a two-meter radius. The NG-1 does not damage augmentations — it creates a zone of signal noise that degrades the wireless protocols used by neural interfaces to communicate with external systems. For attackers relying on BCI-coordinated combat augmentations, reflex-enhancement systems, or wirelessly linked targeting assists, the NG-1's interference field introduces lag, dropped connections, and phantom inputs that severely degrade their performance.\n\nNullPoint Technologies is a small GLMZ firm founded by former neurosecurity researchers who recognized that the proliferation of combat-grade BCI augmentations among street-level criminals had created a defensive gap for unaugmented citizens. The NG-1 is their flagship product: affordable BCI disruption for people who cannot afford BCI augmentations of their own. The device is worn on a cord around the neck and activated by pressing a button on its face.\n\nThe NG-1's interference field is indiscriminate — it affects all BCI communications within range, including the user's own augmentations if they have them, and the augmentations of innocent bystanders. This has generated controversy, as the device can disrupt medical-grade neural interfaces (pain management, seizure prevention, mood regulation) in addition to combat augmentations. NullPoint's legal defense — that the device is no different from a radio jammer in principle — has not satisfied augmented rights advocates. The NG-1 retails for Φ190.",
    manufacturer: "NULLPOINT TECHNOLOGIES",
    tier_availability: "Tier 2+",
    legality: "Licensed",
    base_technologies: ["Localized BCI signal interference generation", "Wireless neural protocol disruption", "Miniaturized electromagnetic noise field"],
    specifications: "interference radius: 2 meters\naffected protocols: standard BCI wireless communications\neffect on augmentations: signal lag, dropped connections, phantom inputs\nactivation: button press on device face\nbattery life: 45 minutes continuous\ncharging: micro-cell, 1 hour recharge\nweight: 28 g\nworn: neck pendant on cord\ncost: Φ190\nlimitations: does not affect hardwired augmentations",
    tactical_use: "Activate when threatened by an augmented attacker. The interference field degrades their BCI-dependent systems — reflex enhancement, targeting assists, sensory augmentation, and communication links will all experience disruption. The effect is not incapacitation but degradation: an augmented attacker becomes significantly less effective but is not disabled. Combine with physical deterrents (sprays, stun devices) for maximum effect. Be aware that your own augmentations and those of nearby civilians will also be affected.",
    cultural_context: "The NG-1 sits at the intersection of personal defense and augmented rights, a position that generates constant debate. Unaugmented citizens view it as an essential equalizer in a world where augmented predators have overwhelming advantages. Augmented communities view it as a device that treats their bodies as threats to be disrupted. NullPoint navigates this tension by marketing exclusively to the unaugmented demographic and avoiding any messaging that frames augmented individuals as inherently dangerous — though the product's existence makes that implication unavoidable.",
    known_users: [],
    story_hooks: [
      "A hospital emergency room near a Tier 2 transit hub has seen a spike in augmentation-related medical emergencies — patients with seizure-prevention BCIs are having episodes triggered by NG-1 devices activated by commuters passing through the area.",
      "NullPoint Technologies has been approached by a corporate faction wanting to license the NG-1's technology for a building-scale system — the implications of BCI disruption as architectural security are profound and troubling."
    ],
    ammunition_type: [],
    tags: ["weapon", "self_defense", "non_lethal", "bci_defense", "electromagnetic", "neural_disruption", "pendant", "tier_2", "anti_augmentation"]
  },
  {
    id: genId(),
    name: "NullPoint Technologies SynapseJam SJ-3 'Migraine'",
    type: "weapon",
    aliases: ["Migraine", "SJ-3", "Neural Fog", "Brain Stutter"],
    category: "self_defense",
    description: "A directed BCI disruption device that fires a focused electromagnetic pulse calibrated to the operating frequencies of common neural interface models. Unlike the omnidirectional NG-1, the SJ-3 is a point-and-shoot device that delivers its disruption field in a narrow cone, affecting only the targeted individual within a five-meter range. The SJ-3's pulse causes a brief but intense disruption of BCI signal processing, manifesting as a 'neural stutter' — a two-to-four-second window during which augmented cognitive functions (enhanced reflexes, threat assessment, sensory processing) experience cascading errors.\n\nThe SJ-3 represents NullPoint's second-generation BCI defense technology, incorporating frequency profiles for the forty most common neural interface models in GLMZ. The device automatically scans for BCI signal emissions and adjusts its disruption pulse to match the detected interface's operating frequency, significantly increasing effectiveness compared to the broadband interference of the NG-1. This targeting capability also means the SJ-3 is less likely to affect bystanders' augmentations — though it will still disrupt any BCI system within its cone.\n\nPriced at Φ680, the SJ-3 is a significant investment but has found a market among individuals who have been specifically victimized by augmented criminals and are willing to invest in targeted countermeasures. NullPoint sells the device through security consultants rather than retail channels, partly for liability management and partly to ensure purchasers receive training in appropriate use.",
    manufacturer: "NULLPOINT TECHNOLOGIES",
    tier_availability: "Tier 3+",
    legality: "Licensed",
    base_technologies: ["Frequency-adaptive BCI disruption", "Neural interface model profiling", "Directed electromagnetic pulse focusing"],
    specifications: "disruption range: 5 meters directional cone (15-degree width)\neffect: 2-4 second neural stutter (cascading BCI processing errors)\nfrequency profiles: 40 common neural interface models\nauto-detection: scans for BCI signal emissions\npulses per charge: 10\nrecharge: 2 hours\nweight: 145 g\ndimensions: 12 cm x 4 cm cylinder\ncost: Φ680",
    tactical_use: "Point the device at the augmented attacker and press the activation button. The SJ-3 will auto-detect their BCI frequency and deliver a tuned disruption pulse. The resulting neural stutter causes a two-to-four-second window of impaired cognitive function — use this window to deploy other deterrents or flee. The directed cone means you can target an individual without affecting nearby civilians in most situations. Ineffective against individuals with hardwired-only augmentations or military-grade shielded interfaces.",
    cultural_context: "The SJ-3 has elevated the BCI defense conversation from broad disruption to targeted countermeasures, and this precision has made it simultaneously more socially acceptable and more controversial. Proponents argue that targeting a specific attacker's augmentations is more ethical than disrupting everyone nearby. Opponents argue that the device's frequency-profiling capability is one step from a neural weapon — a tool designed to attack someone's brain. The legal and ethical framework for BCI countermeasures is years behind the technology.",
    known_users: [],
    story_hooks: [
      "A modified SJ-3 has been recovered that extends the neural stutter duration to minutes rather than seconds — the victim experienced a prolonged cognitive shutdown that caused permanent memory loss around the event.",
      "NullPoint's frequency profile database has been leaked, and black-market developers are using it to create offensive neural attack tools that target specific BCI models with destructive rather than disruptive pulses."
    ],
    ammunition_type: [],
    tags: ["weapon", "self_defense", "non_lethal", "bci_defense", "directed", "neural_disruption", "frequency_adaptive", "tier_3"]
  },
  {
    id: genId(),
    name: "NullPoint Technologies CortexVeil CV-1 'Silence'",
    type: "weapon",
    aliases: ["Silence", "CV-1", "Brain Dome", "Neural Condom"],
    category: "self_defense",
    description: "A self-defense headband that creates a personal electromagnetic cocoon around the wearer's own neural interface, protecting against external BCI intrusion attempts. Unlike NullPoint's offensive products that disrupt attackers' augmentations, the CV-1 is purely defensive — it generates a precisely calibrated interference pattern that blocks unauthorized wireless access to the wearer's BCI without disrupting the interface's normal function.\n\nThe CV-1 addresses a growing threat in GLMZ: neural hacking. As BCI technology has become ubiquitous, so have tools for wireless BCI intrusion — devices that can access a victim's neural interface to inject sensory hallucinations, read surface thoughts, trigger pain responses, or disable motor control. These attacks are difficult to detect and nearly impossible to defend against without hardware-level protection. The CV-1 provides that protection in a wearable form factor.\n\nThe headband uses a thin conductive mesh that, when powered, generates a white-noise electromagnetic shell around the wearer's cranial BCI hardware. This shell is transparent to the wearer's own BCI signals (which are calibrated during a one-time setup process) but blocks external signals that don't match the authorized pattern. The CV-1 costs Φ450 and is available in several widths and fabric covers to blend with different styles. NullPoint's marketing for the CV-1 is notably more mainstream than their offensive products — protecting your own brain is a less controversial proposition than disrupting someone else's.",
    manufacturer: "NULLPOINT TECHNOLOGIES",
    tier_availability: "Tier 2+",
    legality: "Unrestricted",
    base_technologies: ["Calibrated BCI-transparent electromagnetic shielding", "Authorized signal pattern recognition", "Cranial white-noise interference generation"],
    specifications: "protection type: blocks unauthorized wireless BCI access\neffect on wearer's BCI: none (calibrated during setup)\nsetup: one-time 10-minute calibration per BCI model\nbattery life: 18 hours continuous\ncharging: wireless induction, 1 hour\nweight: 45 g\nform factor: headband, multiple width/color options\ncost: Φ450\ncompatible BCI models: all consumer and most commercial-grade interfaces",
    tactical_use: "Wear continuously when in public spaces. The CV-1 provides passive protection against wireless BCI intrusion without requiring activation or attention. The one-time calibration process ensures the headband does not interfere with normal BCI function. If you suspect an active intrusion attempt, the CV-1's indicator LED (hidden on the inner surface) will pulse red. In environments where neural hacking is common, wearing the CV-1 is not optional — it is baseline security hygiene.",
    cultural_context: "The CV-1 has become the BCI equivalent of a condom — protection so obvious that not using it is considered reckless. The street nickname 'Neural Condom' captures both the protective function and the cultural normalcy of the device. In augmented communities, wearing a CV-1 or equivalent is expected, and not wearing one is seen as either naive or inviting trouble. NullPoint's success with the CV-1 has spawned an entire category of 'neural hygiene' products, but the CV-1 remains the market leader.",
    known_users: [],
    story_hooks: [
      "A flaw in the CV-1's calibration process has been discovered — if exploited during setup, an attacker can register their own signal pattern as 'authorized,' gaining permanent undetected access to the wearer's BCI through the very device meant to protect it.",
      "A Tier 1 community health program is distributing free CV-1 units to augmented residents — but the units were donated by an anonymous corporate sponsor, raising questions about whether the headbands contain a backdoor."
    ],
    ammunition_type: [],
    tags: ["weapon", "self_defense", "non_lethal", "bci_defense", "protective", "headband", "wearable", "neural_security", "tier_2", "passive"]
  },

  // ===== DISGUISED WEAPONS (5) =====
  {
    id: genId(),
    name: "Umbraline PhoneShell PS-X 'Last Call'",
    type: "weapon",
    aliases: ["Last Call", "PS-X", "Shock Phone", "The Backup"],
    category: "self_defense",
    description: "A phone case with an integrated stun system that delivers a 70,000-volt discharge through electrode contacts on the case's outer edges. The PS-X fits standard smartphone form factors and adds only 4mm of thickness to the device, passing casual inspection as a ruggedized protective case. The electrodes are concealed within the case's textured grip ridges — invisible until activated by a specific grip pattern (squeeze both sides and press the volume button simultaneously).\n\nUmbraline designed the PS-X around the observation that the smartphone is the one object always in a person's hand during a street encounter — victims instinctively clutch their phone when threatened, making it the ideal platform for a concealed defensive tool. The case's grip-pattern activation prevents accidental discharge during normal phone use, and the electrode placement ensures the phone's screen and touchscreen functionality are unaffected.\n\nThe PS-X costs Φ160 and is available for fifteen popular smartphone models. It recharges independently from the phone via a micro-USB port on the case's bottom edge, providing thirty discharges per charge. Umbraline is a small GLMZ company that has built its entire product line around the principle of defense through everyday objects — the PS-X is their bestseller, accounting for seventy percent of revenue. The product has been copied extensively, with counterfeit versions of varying quality flooding lower-tier markets.",
    manufacturer: "UMBRALINE PERSONAL DEFENSE",
    tier_availability: "Tier 1+",
    legality: "Unrestricted",
    base_technologies: ["Concealed electrode integration in consumer electronics housing", "Grip-pattern activation", "Independent rechargeable stun system"],
    specifications: "voltage: 70,000V\namperage: 3.8 milliamps\nelectrode location: concealed in grip ridges\nactivation: bilateral squeeze + volume button\ndischarges per charge: 30\ncharging: micro-USB, independent from phone\nadded thickness: 4mm\nweight: 42 g (case only)\ncompatible models: 15 popular smartphone form factors\ncost: Φ160",
    tactical_use: "When threatened, shift to the activation grip pattern while the phone is already in your hand. The transition from normal phone grip to activation grip takes less than one second with practice. Press the electrified edge of the case against the attacker's exposed skin. The concealed nature of the weapon provides a significant advantage — the attacker does not perceive the phone as a threat and may allow it within striking distance. Optimal targets: hands reaching for you, forearms, neck.",
    cultural_context: "The PS-X has tapped into a truth about modern life in GLMZ: the phone is always there. By weaponizing the most ubiquitous object in daily life, Umbraline has created a defense tool with zero carry burden — it is already in your hand. The product has been particularly successful among younger demographics who view carrying a separate defense device as inconvenient but are never without their phone. 'Call for help' has acquired a double meaning in neighborhoods where PS-X cases are common.",
    known_users: [],
    story_hooks: [
      "A PS-X discharge during a mugging attempt shorted out the victim's phone, erasing encrypted data that turned out to be evidence in a corporate whistleblower case — the mugger may have been sent specifically to destroy that data under cover of a street crime.",
      "Umbraline's factory has been infiltrated by a competitor who is subtly sabotaging production — PS-X units from certain batches have a critical flaw that causes the electrode discharge to feed back through the phone's touchscreen, shocking the user."
    ],
    ammunition_type: [],
    tags: ["weapon", "self_defense", "non_lethal", "stun", "electrical", "disguised", "phone_case", "everyday_carry", "tier_1", "concealed"]
  },
  {
    id: genId(),
    name: "Umbraline KeyGuard KG-2 'Fang'",
    type: "weapon",
    aliases: ["Fang", "KG-2", "Shock Key", "Keychain Zapper"],
    category: "self_defense",
    description: "A keychain fob containing a compact 50,000-volt stun unit and a 120-decibel alarm, activated by different button presses on the fob's surface. The KG-2 combines two defense modalities in a device that clips onto any keyring and is no larger than a standard car remote. A short press of the main button activates the alarm; a long press activates the stun electrodes on the fob's end. The device provides a binary response option: noise for situations where attention is the best defense, and electricity for situations where physical deterrence is needed.\n\nUmbraline's design philosophy with the KG-2 is 'defense you already carry.' Keys are the second most commonly held object after smartphones, and a keychain attachment requires zero additional carry commitment. The fob's housing is impact-rated polymer in matte black, and the stun electrodes are concealed within what appears to be a decorative tip. The alarm speaker is hidden behind ventilation-style slots in the housing.\n\nThe KG-2 costs Φ75 and runs on a replaceable coin cell battery that provides approximately forty stun discharges or six hours of continuous alarm output. Umbraline sells the KG-2 as an entry-level product for customers who may later upgrade to their more sophisticated offerings. It is their second-bestselling product after the PS-X phone case and is frequently purchased in multiples as gifts.",
    manufacturer: "UMBRALINE PERSONAL DEFENSE",
    tier_availability: "Tier 1+",
    legality: "Unrestricted",
    base_technologies: ["Dual-mode alarm/stun integration", "Keychain form factor miniaturization", "Concealed electrode housing"],
    specifications: "stun voltage: 50,000V\nstun amperage: 2.5 milliamps\nalarm output: 120 dB\nactivation: short press (alarm), long press (stun)\nbattery: CR2032 replaceable\nstun discharges per battery: ~40\nalarm duration per battery: ~6 hours\nweight: 26 g\ndimensions: 6 cm x 2.5 cm x 1.5 cm\ncost: Φ75",
    tactical_use: "Keep keys in hand when walking through risk areas — this is standard advice regardless of whether the keychain is weaponized. When threatened, assess whether noise or electricity is the better response. In populated areas, the alarm draws attention; in isolated areas, the stun provides direct deterrence. The stun electrodes on the fob's end can be used in a jabbing motion similar to holding a key between the knuckles, but with electrical augmentation. The two-mode design means you are never deploying the wrong tool.",
    cultural_context: "The KG-2 occupies the same space as the SentryGuard PS-1 and HarborTech SS-1 — tools so basic and affordable that carrying one is a social minimum. Umbraline's contribution is combining two functions into a single device on the keychain, reducing clutter for people who already carry a spray, an alarm, and a stun device as separate items. In GLMZ's self-defense ecosystem, the KG-2 is a generalist tool: not the best alarm, not the best stunner, but always present and always ready.",
    known_users: [],
    story_hooks: [
      "A KG-2 was used to stun an attacker who turned out to be an undercover corporate security operative — the resulting investigation revealed that the operative was targeting the KG-2's owner for surveillance, not robbery.",
      "Umbraline has received a bulk order for 10,000 KG-2 units from an anonymous buyer — the quantity suggests either a corporate security contract or a community-scale armament program."
    ],
    ammunition_type: [],
    tags: ["weapon", "self_defense", "non_lethal", "stun", "alarm", "electrical", "disguised", "keychain", "everyday_carry", "tier_1", "dual_mode"]
  },
  {
    id: genId(),
    name: "Nimbus Security WristSafe WS-5 'Viper'",
    type: "weapon",
    aliases: ["Viper", "WS-5", "Defense Bracelet", "Smart Bangle"],
    category: "self_defense",
    description: "A chunky fashion bracelet integrating three self-defense functions: a 60,000-volt contact stun, a short-range pepper spray cartridge (single burst, 1.5-meter range), and a GPS-linked distress beacon that transmits the wearer's location to emergency contacts. The WS-5 is activated by voice command, specific wrist gestures, or a hidden button sequence, with each function mapped to a different activation method to prevent accidental deployment.\n\nNimbus Security positioned the WS-5 as a comprehensive personal safety ecosystem compressed into a wearable form factor. The bracelet's exterior is brushed stainless steel with inlaid composite panels that conceal the spray nozzle, stun electrodes, and antenna. The pepper spray cartridge is a sealed, replaceable module that clicks into the bracelet's inner housing — Nimbus sells refill cartridges at Φ20 each. The stun function recharges wirelessly, and the distress beacon uses the wearer's phone connection for transmission.\n\nThe WS-5 retails at Φ520, making it a mid-tier investment. Nimbus markets it through fashion-forward channels, partnering with GLMZ style influencers who normalize wearing it as an accessory rather than a weapon. The bracelet has become popular among young professionals who want comprehensive protection without carrying multiple devices. Its main limitation is the single-burst pepper spray — once deployed, that function is unavailable until the cartridge is replaced.",
    manufacturer: "NIMBUS SECURITY",
    tier_availability: "Tier 2+",
    legality: "Licensed",
    base_technologies: ["Multi-function defense integration", "Voice/gesture/button tri-modal activation", "GPS-linked distress beacon"],
    specifications: "stun voltage: 60,000V at 3.5 milliamps\npepper spray range: 1.5 meters (single burst)\npepper spray SHU: 3.5 million\ndistress beacon: GPS location to emergency contacts via phone\nactivation modes: voice command, wrist gesture, hidden button\nstun charges per battery: 15\nbracelet material: brushed stainless steel + composite\nweight: 78 g\ncost: Φ520\nrefill cartridge: Φ20",
    tactical_use: "The WS-5's three functions are designed for escalating threat levels. At range (1-2 meters), deploy the pepper spray burst to create distance. At contact range, use the stun function to break a grab or deter an attacker. If unable to physically defend, activate the distress beacon to alert emergency contacts with your GPS location. Practice the activation gestures until they are muscle memory — in a crisis, fumbling with a voice command or button sequence costs seconds you don't have.",
    cultural_context: "The WS-5 represents the convergence of fashion, technology, and personal defense that defines GLMZ's consumer landscape. It is worn by people who refuse to choose between style and safety, and Nimbus's influencer marketing has successfully positioned it as a desirable accessory rather than a concession to danger. The bracelet has spawned social media trends where wearers share their WS-5 color combinations and customization, inadvertently advertising their defensive capabilities to potential attackers — a tension Nimbus has not addressed.",
    known_users: [],
    story_hooks: [
      "A WS-5 distress beacon was activated during a kidnapping, allowing emergency contacts to track the victim's movement in real-time — but the kidnappers discovered the bracelet and destroyed it, raising questions about whether visible defense technology makes you a target for more thorough attackers.",
      "Nimbus's gesture-recognition algorithm has been spoofed — a signal broadcast in a crowded venue caused every WS-5 in range to deploy its pepper spray simultaneously, hospitalizing dozens."
    ],
    ammunition_type: ["OC spray cartridge (replaceable)"],
    tags: ["weapon", "self_defense", "non_lethal", "disguised", "bracelet", "multi_function", "stun", "pepper_spray", "distress_beacon", "wearable", "tier_2"]
  },
  {
    id: genId(),
    name: "Dawnlight Industries LuxDefend LD-1 'Clutch'",
    type: "weapon",
    aliases: ["Clutch", "LD-1", "Defense Purse", "The Safe"],
    category: "self_defense",
    description: "A high-end clutch purse with integrated self-defense systems concealed within its reinforced frame. The LD-1's rigid internal structure is ballistic-rated polycarbonate capable of deflecting knife strikes and low-velocity projectiles when held as a shield. The clutch's clasp mechanism conceals a 65,000-volt stun emitter, and the bag's strap is a detachable reinforced cord that can be used as an improvised restraint or escape tool. A squeeze-activated panic system in the handle triggers both a 135-decibel alarm and a GPS distress signal.\n\nDawnlight Industries designed the LD-1 for their existing customer base — Tier 3-5 women who carry luxury accessories and want protection integrated into their lifestyle rather than added to it. The clutch is available in genuine leather with premium hardware, and its self-defense features are completely hidden during normal use. The stun emitter activates when the clasp is opened in a specific pattern (open-close-open within one second), and the ballistic frame functions as passive protection without any activation required.\n\nThe LD-1 costs Φ1,200, placing it firmly in the luxury defense market. Dawnlight sells it through their boutiques alongside their PurityMist PM-4 spray, creating a coordinated defense-and-style ecosystem. The clutch comes in seasonal colors and limited collaborations with fashion designers, ensuring it remains a current accessory rather than a static tool. The internal compartment is large enough for essentials — phone, cards, PM-4 canister — while the ballistic frame adds structural rigidity that some users actually prefer to standard clutch construction.",
    manufacturer: "DAWNLIGHT INDUSTRIES",
    tier_availability: "Tier 3+",
    legality: "Licensed",
    base_technologies: ["Ballistic-rated integrated frame construction", "Concealed clasp-mounted stun emitter", "Multi-function handle panic system"],
    specifications: "stun voltage: 65,000V at 3.0 milliamps\nballistic rating: equivalent to NIJ Level IIA (soft armor)\nalarm output: 135 dB\ndistress signal: GPS via connected phone\nclasp stun activation: open-close-open within 1 second\nstun charges per charge: 20\ndetachable strap length: 60 cm reinforced cord\nweight: 340 g\ndimensions: 22 cm x 14 cm x 5 cm\ncost: Φ1,200\nmaterials: genuine leather, ballistic polycarbonate frame",
    tactical_use: "The LD-1 provides layered defense. Passively, the ballistic frame protects against knife slashes and can be held up as a shield. Actively, the clasp stun can be deployed against a grabbing hand. In extremis, the strap detaches as an improvised tool. The handle panic system is the last resort — squeeze it and run while the alarm and GPS beacon do their work. Practice the clasp activation pattern until it is automatic. The purse can also be swung as an impact weapon — at 340 grams with a rigid frame, it delivers a meaningful strike.",
    cultural_context: "The LD-1 is the definitive statement piece in GLMZ's luxury defense market. It acknowledges the reality that even Tier 4-5 residents face personal safety threats, while refusing to compromise on aesthetics. Dawnlight's seasonal releases generate genuine fashion coverage — reviews discuss the leather quality and hardware finish alongside the stun voltage and ballistic rating. The LD-1 has normalized the idea that a beautiful object can also be a weapon, a concept that resonates in a city where beauty and violence coexist on every street.",
    known_users: [],
    story_hooks: [
      "An LD-1's ballistic frame stopped a knife attack on a corporate executive's spouse — the incident generated enormous publicity for Dawnlight, but also revealed that the attacker specifically targeted the victim knowing she would be carrying the purse, suggesting the attack was a marketing stunt gone wrong.",
      "A stolen LD-1 prototype from Dawnlight's R&D lab features an unreleased upgrade: a micro-drone compartment in the clutch that deploys an autonomous tracker on the attacker. The prototype is now on the black market."
    ],
    ammunition_type: [],
    tags: ["weapon", "self_defense", "non_lethal", "disguised", "purse", "luxury", "ballistic", "stun", "alarm", "tier_3", "fashion"]
  },
  {
    id: genId(),
    name: "Umbraline PenPoint PP-1 'Inkwell'",
    type: "weapon",
    aliases: ["Inkwell", "PP-1", "Tac Pen", "Steel Writer"],
    category: "self_defense",
    description: "A tactical pen machined from solid titanium with a hardened glass-breaker tip, a concealed OC (oleoresin capsaicin) micro-cartridge in the cap, and a DNA-collection edge on the clip. The PP-1 functions as a premium writing instrument while concealing three self-defense capabilities in its construction. The glass-breaker tip can be used as a pain-compliance point in a closed-fist grip; the cap, when removed and squeezed, sprays a single burst of concentrated pepper extract; and the sharpened clip edge is designed to collect skin cells from an attacker for later identification.\n\nUmbraline positioned the PP-1 as the professional's self-defense multi-tool. Unlike the VoltLine ArcPen, the PP-1 uses no electronics — every defensive function is mechanical or chemical, meaning there are no batteries to deplete, no circuits to fail, and no electromagnetic signature to detect. The pen writes with a pressurized ink cartridge (the same technology used in zero-gravity pens), and the OC cartridge is replaceable by unscrewing the cap housing.\n\nThe PP-1 costs Φ95 and is sold in a presentation case that includes two spare ink cartridges and one spare OC micro-cartridge. It is popular among corporate employees, legal professionals, and anyone who carries a pen daily and wants a robust defensive backup that requires no maintenance or charging. The DNA-collection clip has generated particular interest from personal security consultants who advise clients to scratch-and-collect during attacks for post-incident identification.",
    manufacturer: "UMBRALINE PERSONAL DEFENSE",
    tier_availability: "Tier 1+",
    legality: "Unrestricted",
    base_technologies: ["Solid titanium machining", "Micro-cartridge OC delivery", "DNA-collection edge geometry"],
    specifications: "body material: solid titanium\ntip: hardened tungsten carbide glass-breaker\nOC cartridge: single burst, 0.5 meter range\nOC SHU: 5 million (concentrated micro-dose)\nDNA collection: sharpened clip edge retains skin cells\nink: pressurized cartridge, replaceable\nweight: 48 g\nlength: 15 cm\ncost: Φ95\nno electronics: fully mechanical/chemical",
    tactical_use: "Grip the pen in an ice-pick or forward-stab hold. The tungsten carbide tip concentrates impact force into a small area, making strikes to pressure points and soft tissue highly effective even from a person with limited upper-body strength. For the OC burst, remove the cap, point the nozzle at the attacker's face, and squeeze the cap body — the spray is effective to half a meter, making it a contact-range tool. During any physical confrontation, use the clip edge against exposed skin to collect DNA for post-incident identification.",
    cultural_context: "The PP-1 appeals to people who are philosophically opposed to electronic defense tools — those who don't trust batteries, software, or anything that can be hacked, jammed, or remotely disabled. Its purely mechanical nature makes it immune to EMP, BCI interference, and electronic countermeasures. In an increasingly connected and surveilled city, the PP-1 represents an older paradigm of self-defense: simple tools that work every time. Umbraline's marketing leans into this reliability, using the tagline 'No App Required.'",
    known_users: [],
    story_hooks: [
      "DNA collected from a PP-1 clip during a street attack matched a corporate security operative who was supposed to be off-duty — the operative's employer is now implicated in the attack.",
      "A collector's market has emerged for limited-edition PP-1 variants machined from exotic materials — a Damascus-steel PP-1 sold at auction for Φ2,800, blurring the line between weapon and art object."
    ],
    ammunition_type: ["OC micro-cartridge (replaceable)"],
    tags: ["weapon", "self_defense", "non_lethal", "disguised", "pen", "tactical", "titanium", "mechanical", "no_electronics", "tier_1", "everyday_carry"]
  }
];

// Write files
let written = 0;
let skipped = 0;

for (const w of weapons) {
  const fname = toFileName(w.name);
  if (existing.has(fname)) {
    console.log(`SKIP (exists): ${fname}`);
    skipped++;
    continue;
  }
  const fpath = path.join(outDir, fname);
  if (fs.existsSync(fpath)) {
    console.log(`SKIP (exists on disk): ${fname}`);
    skipped++;
    continue;
  }
  fs.writeFileSync(fpath, JSON.stringify(w, null, 2) + '\n');
  console.log(`WROTE: ${fname}`);
  written++;
}

console.log(`\nSelf-defense weapons: wrote ${written}, skipped ${skipped} (already existed)`);
console.log(`Total weaponry files now: ${fs.readdirSync(outDir).length}`);
