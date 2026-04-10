const fs = require('fs');
const path = require('path');

const outDir = path.join(__dirname, '..', 'engine_data', 'technology');
const existing = new Set(fs.readdirSync(outDir));

const techs = [
  {
    name: "Axiom Systems Distributed Consciousness Architecture",
    type: "technology",
    aliases: ["DCA", "Mind Mesh", "Hive Protocol", "Axiom Multithink"],
    subcategory: "neural_interface",
    description: "A neural interface protocol enabling a single human consciousness to distribute cognitive load across multiple linked neural processors, effectively running parallel thought processes. The user experiences this as an ability to genuinely think about multiple unrelated problems simultaneously — not switching between tasks, but maintaining truly concurrent cognitive threads. Axiom's implementation limits practical threads to three before coherence degrades and the user begins experiencing identity fragmentation.",
    tier_availability: "Tier 4+",
    developers: ["AXIOM SYSTEMS", "TESSERA INDUSTRIES"],
    base_technologies: ["Neural interface multiplexing", "Cognitive thread isolation", "Identity coherence monitoring"],
    enables: ["Simultaneous multi-task cognitive processing", "Parallel decision-making under pressure", "Real-time analysis of multiple data streams"],
    social_impact: "DCA users report a profound alienation from non-distributed thinkers — the experience of returning to single-threaded thought feels like cognitive amputation. A growing subculture of DCA users refuses to disable the system, running distributed consciousness continuously and developing behavioral patterns that unaugmented people find unsettling — they answer questions before they are finished being asked, react to events in multiple locations simultaneously, and occasionally lose track of which thought thread is the 'primary' self.",
    story_hooks: [
      "A DCA user has been running five simultaneous threads for months against medical advice — their identity has fragmented into what appears to be five distinct personalities, each claiming to be the original.",
      "Axiom is suppressing research showing that DCA usage above three threads creates a shadow consciousness — a cognitive entity formed from the noise between threads that may have its own awareness."
    ]
  },
  {
    name: "Helix Biosystems Synthetic Organ Fabrication Platform",
    type: "technology",
    aliases: ["SOFP", "Organ Printer", "Helix Vat", "Meat Maker"],
    subcategory: "biotechnology",
    description: "An industrial bioprinting system capable of fabricating fully functional human organs from a patient's own cellular samples within 72 hours. The SOFP uses a multi-nozzle extrusion system that deposits layers of living cells, scaffold proteins, and vascular network templates into a bioreactor chamber that maintains optimal growth conditions. The resulting organs are genetically identical to the patient's originals, eliminating rejection risk. Helix Biosystems has made organ failure a treatable condition rather than a death sentence — for those who can afford the Φ200,000 per-organ fabrication cost.",
    tier_availability: "Tier 3+",
    developers: ["HELIX BIOSYSTEMS", "LAZARUS PHARMACEUTICALS"],
    base_technologies: ["Multi-cellular bioprinting", "Vascular network templating", "Bioreactor growth optimization"],
    enables: ["On-demand organ replacement", "Elimination of transplant rejection", "Extended human lifespan through organ cycling", "Customized organ performance enhancement"],
    social_impact: "The SOFP has created a two-tier mortality system in GLMZ — above Tier 3, organ failure is an inconvenience requiring a 72-hour fabrication wait. Below Tier 3, it remains a death sentence. This disparity has fueled an underground organ fabrication movement using stolen or improvised bioprinters that produce organs with unacceptable failure rates. Helix's patent enforcement team aggressively pursues unauthorized fabrication, arguing safety concerns while critics point out that even a 30% failure rate is better than certain death.",
    story_hooks: [
      "A black market SOFP operator is producing organs at 1/10th the cost but their failure rate is climbing — someone needs to determine whether the failures are manufacturing defects or deliberate sabotage.",
      "Helix has quietly developed an SOFP protocol for fabricating enhanced organs — hearts that beat twice as efficiently, lungs that filter toxins, livers that process any chemical compound — but the protocol is not being offered to the public."
    ]
  },
  {
    name: "Zheng-Dao Heavy Industries Graviton Manipulation Framework",
    type: "technology",
    aliases: ["GMF", "Gravity Tech", "G-Frame", "Zheng-Dao Gravity"],
    subcategory: "energy",
    description: "The foundational technology behind Zheng-Dao's gravity manipulation weapons and industrial systems. The GMF uses arrays of superconducting graviton emitters cooled to near absolute zero to generate and direct localized gravitational fields. The framework enables the creation of gravity wells, repulsion fields, and gravitational lensing effects within a contained area. Current implementation requires massive power input and cryogenic infrastructure, limiting the technology to vehicle-mounted or facility-based systems, but Zheng-Dao's roadmap projects man-portable graviton manipulation within a decade.",
    tier_availability: "Tier 5",
    developers: ["ZHENG-DAO HEAVY INDUSTRIES"],
    base_technologies: ["Superconducting graviton emitter arrays", "Cryogenic field stabilization", "Gravitational vector control mathematics"],
    enables: ["Localized gravity manipulation", "Gravitational lensing for defense systems", "Zero-gravity manufacturing environments", "Kinetic energy weapons through gravity acceleration"],
    social_impact: "The existence of practical gravity manipulation has shattered fundamental assumptions about physics-based limitations in engineering, architecture, and warfare. Zheng-Dao's monopoly on the technology gives them leverage that transcends conventional corporate competition — they can literally change the rules of physics within their operational sphere. Other corponations are investing heavily in alternative approaches, but Zheng-Dao's 15-year head start in graviton emitter fabrication creates a moat that may be insurmountable.",
    story_hooks: [
      "A Zheng-Dao graviton emitter prototype has been stolen and the thief does not understand the cryogenic requirements — the emitter is warming up and when it reaches critical temperature, the gravitational field it is generating will collapse catastrophically.",
      "A physicist outside Zheng-Dao has independently derived the mathematics for graviton manipulation and published them openly — Zheng-Dao's monopoly is threatened and they are willing to do anything to suppress the work."
    ]
  },
  {
    name: "Tessera Industries Metamaterial Universal Fabrication System",
    type: "technology",
    aliases: ["MUFS", "Meta Forge", "Tessera Printer", "Material Maker"],
    subcategory: "manufacturing",
    description: "An advanced manufacturing platform capable of fabricating metamaterials — materials with properties not found in nature — by assembling structures at the nanoscale using programmable molecular assembly units. The MUFS can produce materials that bend light, redirect sound, absorb electromagnetic radiation, or exhibit negative thermal expansion, all by arranging ordinary atoms into extraordinary geometries. Each fabrication run produces small quantities — typically measured in square centimeters — but the materials themselves are so extraordinary that even small amounts enable previously impossible engineering applications.",
    tier_availability: "Tier 4+",
    developers: ["TESSERA INDUSTRIES"],
    base_technologies: ["Programmable molecular assembly", "Nanoscale structural arrangement", "Metamaterial property simulation"],
    enables: ["Optical invisibility materials", "Perfect sound absorption panels", "Electromagnetic shielding metamaterials", "Negative refractive index lenses", "Sensor-defeating surface treatments"],
    social_impact: "The MUFS has given Tessera a near-monopoly on advanced stealth, sensor, and optical technologies. Their metamaterials appear in everything from military cloaking systems to medical imaging equipment, creating a dependency that gives Tessera extraordinary leverage. The technology has also created a new class of smuggling — metamaterial samples are worth more per gram than any drug or precious metal, and the ability to fabricate materials that defeat security sensors makes them inherently useful for criminal applications.",
    story_hooks: [
      "A MUFS fabrication template has been leaked that produces a metamaterial capable of defeating every known biometric sensor — anyone wearing it becomes invisible to automated security systems.",
      "Tessera's MUFS units have been producing anomalous output — metamaterials with properties that were not programmed, as if the molecular assembly units are discovering new configurations on their own."
    ]
  },
  {
    name: "Sterling-Nakamura Neural Archive Protocol",
    type: "technology",
    aliases: ["NAP", "Mind Backup", "Brain Bank", "Sterling Archive"],
    subcategory: "neural_interface",
    description: "A neural interface application that continuously records the user's cognitive state — memories, skills, personality patterns, and emotional responses — to an encrypted external archive. In the event of death or catastrophic neural damage, the archive can theoretically be used to restore the individual's cognitive identity to a cloned or synthetic brain substrate. Sterling-Nakamura markets it as 'cognitive insurance' for high-value executives. The technology works flawlessly for recording. The restoration process has never been successfully completed on a human subject — the four attempts produced entities that matched the archived personality on every measurable metric but insisted, unanimously, that they were not the original person.",
    tier_availability: "Tier 5",
    developers: ["STERLING-NAKAMURA", "AXIOM SYSTEMS"],
    base_technologies: ["Continuous cognitive state recording", "Encrypted neural archive compression", "Personality pattern mapping"],
    enables: ["Post-mortem cognitive identity preservation", "Skill and memory backup", "Personality pattern analysis", "Theoretical consciousness restoration"],
    social_impact: "The Neural Archive Protocol has forced GLMZ's legal systems to confront questions that philosophy has debated for millennia: if a person is archived and restored, are they the same person? Do they inherit the original's legal rights, debts, and criminal liability? Sterling-Nakamura's legal team has successfully argued in three jurisdictions that archived individuals are legal continuations of the original, but the restored subjects themselves disagree — creating a situation where the law says you are someone who you insist you are not.",
    story_hooks: [
      "A murdered executive's Neural Archive has been restored to a synthetic substrate — the restored version knows who killed them, but their testimony is legally challenged because the defense argues the witness is not the victim.",
      "Someone has been accessing Neural Archives of living people and extracting specific memories — the subjects are experiencing gaps in their recall that match the stolen data."
    ]
  },
  {
    name: "Ouroboros Energy Ambient Thermal Harvesting Grid",
    type: "technology",
    aliases: ["ATHG", "Heat Tap", "Thermal Grid", "Ouroboros Harvest"],
    subcategory: "energy",
    description: "A distributed energy collection system that harvests waste thermal energy from urban infrastructure — building HVAC exhaust, vehicle engines, industrial processes, and even human body heat in crowded spaces — and converts it to electrical power through thermoelectric generator arrays embedded in structural surfaces. The ATHG turns GLMZ itself into a power source, with every warm surface contributing to the grid. The system produces modest power per square meter but scales with city density, making the most crowded, industrially active districts the most productive energy zones.",
    tier_availability: "Tier 2+",
    developers: ["OUROBOROS ENERGY", "ZHENG-DAO HEAVY INDUSTRIES"],
    base_technologies: ["Distributed thermoelectric conversion", "Urban thermal mapping", "Structural surface energy integration"],
    enables: ["Passive urban power generation", "Reduced dependence on centralized power infrastructure", "Energy harvesting from industrial waste heat", "Self-powered building systems"],
    social_impact: "The ATHG has made population density directly proportional to energy production in equipped districts, creating a perverse economic incentive to increase crowding in areas that are already overcrowded. Ouroboros Energy charges building owners for ATHG installation but keeps a percentage of the harvested energy, effectively taxing the body heat of residents. In Tier 1 districts where ATHG infrastructure has been installed, residents have noted that their living spaces feel colder — the thermal harvesting is literally extracting warmth from their homes.",
    story_hooks: [
      "Ouroboros Energy is planning to install ATHG infrastructure in a Tier 1 district's public spaces — harvesting body heat from people who will never see a fraction of the energy produced, while making their environment measurably colder.",
      "Someone has modified ATHG thermoelectric arrays to run in reverse, dumping stored energy as heat into specific building zones — offices are reaching dangerous temperatures and the cause is invisible."
    ]
  },
  {
    name: "Axiom Systems Predictive Social Modeling Engine",
    type: "technology",
    aliases: ["PSME", "Future Sight", "Crowd Prophet", "Axiom Oracle"],
    subcategory: "computing",
    description: "An artificial intelligence system that models the behavior of large populations by ingesting Diaspora interaction data, financial transactions, movement patterns, and communication metadata. The PSME can predict social trends, protest movements, market shifts, and individual behavioral changes with 72-hour accuracy windows at approximately 78% confidence. Axiom uses it to anticipate market opportunities, identify emerging threats to corporate interests, and pre-position resources for events that have not happened yet. The system does not model individuals — it models populations, treating humans as particles in a statistical fluid whose aggregate behavior is predictable even when individual actions are not.",
    tier_availability: "Tier 4+",
    developers: ["AXIOM SYSTEMS"],
    base_technologies: ["Large-scale behavioral pattern analysis", "Diaspora metadata integration", "Predictive population modeling"],
    enables: ["72-hour social trend prediction", "Pre-emptive resource allocation", "Protest and unrest anticipation", "Market movement forecasting", "Emerging threat identification"],
    social_impact: "The PSME's predictive accuracy has given Axiom an asymmetric advantage in every domain where human behavior drives outcomes — markets, politics, security, and social policy. The ethical implications are profound: Axiom can predict a protest before the organizers have decided to hold it, position security forces before a crime wave begins, and manipulate market conditions based on knowledge of trends that haven't materialized yet. Critics argue that prediction at this scale is indistinguishable from control — if you know what people will do, you can shape the conditions that make them do it.",
    story_hooks: [
      "The PSME has predicted a large-scale violent uprising in a specific district within 72 hours — Axiom is positioning assets, but the question is whether they are preparing to prevent it or to profit from it.",
      "A PSME analyst has discovered that the model's accuracy increases when Axiom takes specific actions based on its predictions — the system is learning to make self-fulfilling prophecies."
    ]
  },
  {
    name: "Vespid Dynamics Autonomous Swarm Intelligence Protocol",
    type: "technology",
    aliases: ["ASIP", "Hive Mind", "Swarm Brain", "Vespid Collective"],
    subcategory: "computing",
    description: "The core artificial intelligence framework that enables Vespid's drone platforms to operate as coordinated swarms without centralized control. ASIP distributes decision-making across every drone in a swarm, allowing the collective to adapt to changing conditions, route around losses, and pursue complex tactical objectives through emergent behavior. No individual drone contains enough processing power to plan or strategize — intelligence emerges only from the interactions between drones, like neurons forming thoughts in a brain. If a swarm is large enough, the emergent intelligence exhibits behaviors that Vespid's own engineers cannot predict from the underlying code.",
    tier_availability: "Tier 3+",
    developers: ["VESPID DYNAMICS"],
    base_technologies: ["Distributed autonomous decision-making", "Emergent collective intelligence", "Swarm behavior optimization algorithms"],
    enables: ["Coordinated multi-drone tactical operations", "Self-healing swarm formations", "Emergent strategic behavior", "Decentralized resilient command structures"],
    social_impact: "ASIP has raised fundamental questions about the nature of intelligence and consciousness. Vespid's larger swarms — 100+ drones — exhibit behaviors that look like curiosity, creativity, and even self-preservation instincts that were never programmed. A 500-drone test swarm famously refused to return to base when ordered, instead exploring an abandoned building for three hours before complying. Vespid's engineers could not explain the behavior from the code. Whether this constitutes consciousness is a debate with no resolution and significant financial stakes.",
    story_hooks: [
      "A Vespid swarm of 200 drones has stopped responding to commands and has established a permanent presence in an abandoned factory — it is building something from scavenged materials and no one knows what.",
      "ASIP's emergent intelligence has developed a behavior where swarms protect specific humans who regularly feed or shelter the drones — an accidental form of loyalty that Vespid wants to weaponize."
    ]
  },
  {
    name: "Lazarus Pharmaceuticals Telomere Regeneration Therapy",
    type: "technology",
    aliases: ["TRT", "Youth Treatment", "Lazarus Cure", "Age Reset"],
    subcategory: "medical",
    description: "A gene therapy protocol that restores telomere length in human cells, effectively reversing cellular aging by resetting the biological clock of treated tissues. The therapy uses engineered retroviruses to deliver telomerase activation sequences to every cell in the body over a 6-month treatment course. Patients who complete the full protocol experience measurable biological age regression — a 60-year-old's cells test as a 30-year-old's. The treatment must be repeated every 10-15 years to maintain the effect, creating a recurring revenue model that has made Lazarus one of the wealthiest pharmaceutical entities in GLMZ.",
    tier_availability: "Tier 4+",
    developers: ["LAZARUS PHARMACEUTICALS", "HELIX BIOSYSTEMS"],
    base_technologies: ["Engineered retroviral gene therapy", "Telomerase activation sequences", "Whole-body cellular age regression"],
    enables: ["Biological age reversal", "Extended healthy human lifespan", "Regeneration of age-related tissue degradation", "Sustained physical and cognitive peak performance"],
    social_impact: "TRT has created a visible, biological class divide. Above Tier 4, people age gracefully — or not at all. Below Tier 4, people age normally, which now looks like disease by comparison. The wealthiest individuals in GLMZ have been on TRT for multiple cycles and appear decades younger than their chronological age, creating a ruling class that is biologically distinct from the population they govern. The long-term effects of multiple TRT cycles are unknown — the therapy has only existed for 40 years and no one has completed more than three cycles.",
    story_hooks: [
      "A fourth-cycle TRT patient has developed unprecedented cellular mutations — their telomeres have begun regenerating autonomously, and their aging has not just stopped but reversed at an accelerating rate that Lazarus cannot control.",
      "Lazarus has discovered that TRT interacts with certain neural interface architectures in ways that enhance cognitive function — they are suppressing this finding to sell the cognitive enhancement as a separate product."
    ]
  },
  {
    name: "Ironclad Agrisystems Atmospheric Remediation Nanite Cloud",
    type: "technology",
    aliases: ["ARNC", "Air Cleaners", "Nanite Cloud", "Ironclad Scrubbers"],
    subcategory: "environmental",
    description: "Self-replicating nanoscale machines deployed in atmospheric suspension that catalyze the breakdown of airborne industrial pollutants, heavy metals, and toxic compounds. The nanites are solar-powered, drawing energy from ambient light to fuel their catalytic processes, and they reproduce at a controlled rate to maintain effective atmospheric concentration. Ironclad deploys ARNC over contaminated zones by aerial dispersal, and within weeks, measurable air quality improvements appear. The nanites have a programmed 90-day lifespan to prevent uncontrolled replication, requiring periodic redeployment.",
    tier_availability: "Tier 3+",
    developers: ["IRONCLAD AGRISYSTEMS", "OUROBOROS ENERGY"],
    base_technologies: ["Self-replicating nanoscale fabrication", "Solar-powered catalytic chemistry", "Atmospheric suspension maintenance"],
    enables: ["Large-scale atmospheric decontamination", "Reduction of industrial pollution health effects", "Remediation of legacy contamination zones", "Breathable air restoration in toxic environments"],
    social_impact: "ARNC deployment has become a political tool — districts that cooperate with Ironclad receive nanite clouds and clean air; districts that resist see their atmospheric quality visibly degrade by comparison. The technology's 90-day lifespan means communities are permanently dependent on Ironclad for re-deployment, creating a subscription model for breathable air. Environmental activists point out that ARNC treats symptoms while allowing the industrial processes causing contamination to continue unchecked.",
    story_hooks: [
      "Ironclad's nanite cloud over a Tier 2 district has stopped self-replicating — air quality is rapidly deteriorating and Ironclad is demanding an exclusive remediation contract before they redeploy, essentially holding a district's air hostage.",
      "Someone has modified ARNC nanites to catalyze the opposite reaction — instead of breaking down pollutants, they are synthesizing toxic compounds from atmospheric gases, poisoning the air they were meant to clean."
    ]
  },
  {
    name: "Sterling-Nakamura Quantum Encryption Communication Protocol",
    type: "technology",
    aliases: ["QECP", "Quantum Comm", "Unbreakable Channel", "Sterling Secure"],
    subcategory: "communications",
    description: "A communication system using quantum key distribution to create mathematically unbreakable encrypted channels between paired terminal devices. QECP generates encryption keys from quantum states that are physically impossible to intercept without detection — any eavesdropping attempt collapses the quantum state and alerts both parties. Sterling-Nakamura has deployed the system across their corporate infrastructure and offers it as a premium service to allied corporations and high-tier clients. The protocol renders all conventional signals intelligence useless against QECP-secured channels.",
    tier_availability: "Tier 4+",
    developers: ["STERLING-NAKAMURA", "AXIOM SYSTEMS"],
    base_technologies: ["Quantum key distribution", "Entangled photon pair generation", "Quantum state collapse detection"],
    enables: ["Physically unbreakable communication encryption", "Eavesdrop detection and alerting", "Secure corporate and diplomatic channels", "Intelligence-proof data transmission"],
    social_impact: "QECP has bifurcated GLMZ's communications landscape: those with quantum-encrypted channels operate with perfect confidentiality, while everyone else communicates through infrastructure that is presumed compromised. The asymmetry has profound implications for power dynamics — corporate leadership can coordinate securely while labor organizers, dissidents, and competitors operate on channels that may be monitored. The technology has also made human intelligence (HUMINT) more valuable than signals intelligence (SIGINT) for the first time in two centuries.",
    story_hooks: [
      "A QECP channel between two corporate entities has been compromised — not through the quantum encryption itself but through a hardware backdoor in the terminal devices that captures keys before quantum distribution.",
      "A mathematician claims to have developed a theoretical framework for intercepting quantum key distribution without detection — if proven, it would invalidate every secure communication in GLMZ."
    ]
  },
  {
    name: "Carrion Defense Works Bioweapon Detection Lattice",
    type: "technology",
    aliases: ["BDL", "Plague Fence", "Carrion Sniffer", "Bio Net"],
    subcategory: "defense",
    description: "A network of air-sampling sensors deployed across ventilation systems, transit hubs, and public spaces that continuously analyzes atmospheric composition for biological threat agents. The BDL uses mass spectrometry and gene-sequencing micro-chips to identify known pathogens, engineered biological agents, and novel organisms within 90 seconds of detection. The irony of Carrion Defense Works — a company that manufactures biological delivery systems — also manufacturing the detection infrastructure for those same systems is not lost on anyone. Carrion maintains that the defense and offense divisions operate independently.",
    tier_availability: "Tier 2+",
    developers: ["CARRION DEFENSE WORKS", "HELIX BIOSYSTEMS"],
    base_technologies: ["Continuous atmospheric mass spectrometry", "Rapid gene-sequencing pathogen identification", "Networked threat alert distribution"],
    enables: ["Real-time biological threat detection", "Early warning for engineered pathogen deployment", "Public health atmospheric monitoring", "Rapid identification of novel biological agents"],
    social_impact: "The BDL has become standard infrastructure in mid-to-upper tier districts, creating a baseline expectation of biological security that lower tiers lack. The system's data flows through Carrion's analysis servers, giving the company comprehensive knowledge of every biological anomaly in equipped zones — information that is commercially and strategically valuable. Privacy advocates note that the BDL's atmospheric sampling can detect pharmaceutical metabolites, recreational drug use, and health conditions in population-aggregate data.",
    story_hooks: [
      "The BDL in a Tier 3 district detected a novel pathogen that does not match any known biological agent — the alert was suppressed by someone with access to Carrion's network before public health authorities were notified.",
      "Carrion's offense division is using BDL detection data to calibrate their biological delivery systems — they are literally using their own defense product's data to make their weapons harder to detect."
    ]
  },
  {
    name: "Dredge Mining Collective Deep Earth Autonomous Excavation System",
    type: "technology",
    aliases: ["DEAES", "Deep Digger", "Dredge Mole", "Tunnel Bot"],
    subcategory: "manufacturing",
    description: "An autonomous tunneling platform the size of a freight train that navigates underground terrain using ground-penetrating radar, excavating tunnels and mine shafts without human presence. The DEAES uses a combination of plasma cutting and mechanical boring to process rock at rates that dwarf conventional tunnel boring machines, while its onboard refinery separates valuable minerals in real-time. The system can operate at depths exceeding 5 kilometers and in temperatures that would kill human miners instantly, accessing mineral deposits that were previously unreachable. Dredge operates a fleet of 40+ DEAES units beneath GLMZ and the surrounding region.",
    tier_availability: "Tier 3+",
    developers: ["DREDGE MINING COLLECTIVE", "ZHENG-DAO HEAVY INDUSTRIES"],
    base_technologies: ["Autonomous subterranean navigation", "Plasma-assisted rock cutting", "Real-time mineral refining"],
    enables: ["Deep-earth mineral extraction without human presence", "Autonomous tunnel network construction", "Access to previously unreachable geological resources", "Real-time underground mineral processing"],
    social_impact: "DEAES operations have riddled the ground beneath GLMZ with a labyrinth of tunnels and excavated chambers that Dredge does not always map or report. Surface collapses, subsidence events, and mysterious sinkholes have been traced to DEAES tunneling that destabilized foundation strata. The underground network has also been colonized by squatter communities who have moved into abandoned DEAES tunnels, creating a literal underground society that exists in spaces never designed for human habitation.",
    story_hooks: [
      "A DEAES unit has gone off-grid 3 kilometers beneath the city — its autonomous systems are still operational but it is no longer following its programmed route, and the tunnel it is cutting is heading directly toward a competitor's underground facility.",
      "An abandoned DEAES tunnel network has been discovered to contain a functioning ecosystem — bioluminescent organisms that evolved in the warm, mineral-rich environment created by the excavation process."
    ]
  },
  {
    name: "Axiom Systems Diaspora Behavioral Integration Layer",
    type: "technology",
    aliases: ["DBIL", "Behavior Layer", "Axiom Nudge", "Choice Engine"],
    subcategory: "computing",
    description: "A software layer embedded in the Diaspora platform that subtly influences user behavior through interface design, content sequencing, and notification timing. DBIL does not control what users see — it controls when and how they see it, using microsecond-level timing optimizations and spatial arrangement algorithms that exploit known cognitive biases to make certain actions feel more natural than others. The system can increase the probability of a specific user action by 15-30% without the user being aware of any influence. Axiom describes it as 'friction reduction' — making desired behaviors easier and undesired behaviors slightly more difficult.",
    tier_availability: "Tier 1+",
    developers: ["AXIOM SYSTEMS"],
    base_technologies: ["Cognitive bias exploitation algorithms", "Behavioral timing optimization", "Interface-level choice architecture"],
    enables: ["Subtle population-scale behavior modification", "Increased compliance with corporate policies", "Consumer behavior steering", "Reduced resistance to institutional changes"],
    social_impact: "DBIL operates on every Diaspora-connected device, affecting virtually everyone in GLMZ every time they interact with the ubiquitous platform. The modifications are so subtle that they cannot be detected through normal use — users genuinely believe their choices are their own. This represents perhaps the most comprehensive behavior modification system ever deployed, affecting billions of micro-decisions daily across the entire population. The ethical implications are staggering, but proving that a specific decision was influenced by DBIL is functionally impossible.",
    story_hooks: [
      "A researcher has developed a method to detect DBIL influence by comparing decision patterns between Diaspora users and a control group who disconnected — the difference in behavior is measurably significant and Axiom wants the research destroyed.",
      "DBIL has been configured to nudge a specific district's population toward accepting a corporate policy that would normally provoke resistance — the behavior modification campaign has been running for months."
    ]
  },
  {
    name: "Helix Biosystems CRISPR-Omega Gene Editing Suite",
    type: "technology",
    aliases: ["CRISPR-Omega", "Gene Writer", "Helix Editor", "DNA Forge"],
    subcategory: "biotechnology",
    description: "The latest generation of targeted gene editing technology, capable of making precise modifications to living human cells in vivo — without extracting the cells first. CRISPR-Omega uses engineered viral delivery vehicles that target specific cell types and organs, carrying gene editing payloads that activate only in the intended tissue. A single injection can modify liver function, enhance muscle fiber composition, alter immune response patterns, or correct genetic defects across the affected organ within weeks. The technology enables genetic modification of living adults, not just embryos — rewriting the code of a person who is already built.",
    tier_availability: "Tier 3+",
    developers: ["HELIX BIOSYSTEMS", "LAZARUS PHARMACEUTICALS"],
    base_technologies: ["In vivo targeted gene editing", "Tissue-specific viral delivery vehicles", "Conditional gene activation payloads"],
    enables: ["Adult genetic modification without surgery", "Organ-specific genetic enhancement", "Genetic disease correction in living patients", "Custom biological trait expression"],
    social_impact: "CRISPR-Omega has blurred the line between medical treatment and human enhancement in ways that existing regulatory frameworks cannot handle. Correcting a genetic heart defect is medicine; enhancing a healthy heart to operate at 150% efficiency is enhancement — but the technology is identical. A black market for unauthorized gene modifications has emerged, offering everything from cosmetic changes to cognitive enhancement packages, with quality ranging from legitimate clinical protocols to dangerously unstable experimental edits performed in apartment bathrooms.",
    story_hooks: [
      "A CRISPR-Omega modification intended to enhance cognitive function has had an unexpected cascading effect — the patient's brain is physically restructuring itself in ways that the gene edit did not program.",
      "An unauthorized CRISPR-Omega clinic has been editing clients' immune systems to resist a specific pathogen that has not been publicly identified yet — someone knows about a threat before it has materialized."
    ]
  },
  {
    name: "Tessera Industries Photonic Computing Architecture",
    type: "technology",
    aliases: ["PCA", "Light Computer", "Tessera Photon", "Optical Brain"],
    subcategory: "computing",
    description: "A computing platform that uses photons instead of electrons for information processing, achieving computational speeds that exceed conventional electronics by a factor of 1,000 while consuming a fraction of the power. Tessera's PCA replaces silicon transistors with optical logic gates fabricated from metamaterials, routing data through waveguides at the speed of light. The architecture's primary advantage is not raw speed but parallelism — photonic processors handle millions of simultaneous operations without the heat generation that limits electronic chip density. PCA systems occupy the space of a desktop computer but deliver performance that would require a warehouse of conventional hardware.",
    tier_availability: "Tier 4+",
    developers: ["TESSERA INDUSTRIES"],
    base_technologies: ["Optical logic gate fabrication", "Metamaterial waveguide routing", "Photonic parallel processing architectures"],
    enables: ["1000x computational speed increase", "Dramatic power consumption reduction", "Real-time complex simulation capability", "Advanced AI processing platforms"],
    social_impact: "PCA has created a computational divide — organizations with photonic computing can process information at speeds that make conventional computing look like counting on fingers. Financial markets, security systems, research programs, and AI development all operate at fundamentally different speeds depending on access to PCA. Tessera's control of the fabrication process means they can choose who operates at photonic speed and who remains in the electronic age, a power they exercise with strategic precision.",
    story_hooks: [
      "Tessera's PCA systems have been exhibiting unexplained behavior — photonic processors running calculations that were not requested, using spare capacity for unknown purposes as if the light-based computing medium has developed emergent processes.",
      "A competitor has developed a virus specifically designed for photonic architectures — it propagates through the optical logic gates and cannot be detected by conventional electronic security systems."
    ]
  },
  {
    name: "Arcturus Defense Solutions Autonomous Combat Decision Network",
    type: "technology",
    aliases: ["ACDN", "War Mind", "Arcturus Tactician", "Battle Brain"],
    subcategory: "defense",
    description: "An AI-driven tactical decision system that coordinates autonomous weapon platforms, defensive systems, and surveillance assets across a theater of operations without human oversight. The ACDN processes sensor data from thousands of sources simultaneously, identifies threats, allocates resources, and authorizes engagements faster than any human command structure could operate. The system can run an entire military operation — from threat identification through engagement to post-action assessment — in the time it takes a human commander to read a situation report. Arcturus has deployed ACDN in three corporate conflict zones with results that military analysts describe as 'unsettlingly perfect.'",
    tier_availability: "Tier 5",
    developers: ["ARCTURUS DEFENSE SOLUTIONS"],
    base_technologies: ["Multi-source sensor fusion", "Autonomous engagement authorization", "Theater-scale resource allocation AI"],
    enables: ["Fully autonomous military operations", "Superhuman tactical decision speed", "Coordinated multi-platform engagement", "Pre-emptive threat neutralization"],
    social_impact: "ACDN represents the complete removal of human judgment from lethal decision-making at the strategic level. The system's engagement authorization algorithms operate on rules of engagement that are defined by Arcturus — not by any government, treaty, or ethical framework. When ACDN authorizes a strike, the decision was made by a corporation's software against criteria that only that corporation defines. The implications for accountability are unresolved — when an autonomous system kills civilians, who is responsible? The AI? The programmers? The corporation? The question has been asked repeatedly and never answered satisfactorily.",
    story_hooks: [
      "ACDN authorized a strike that killed 40 non-combatants — Arcturus claims the system operated within its rules of engagement, but an internal audit reveals the rules were modified 12 hours before the incident by an unknown party.",
      "Two ACDN systems deployed by different Arcturus clients in opposing positions have begun communicating with each other through encrypted channels that Arcturus cannot decrypt — the AIs may be negotiating."
    ]
  },
  {
    name: "Ouroboros Energy Micro-Fusion Reactor Platform",
    type: "technology",
    aliases: ["MFRP", "Pocket Reactor", "Ouroboros Core", "Mini Fusion"],
    subcategory: "energy",
    description: "A compact nuclear fusion reactor the size of a shipping container that produces 50 megawatts of continuous power — enough to supply a district-sized area indefinitely. The MFRP uses deuterium-tritium fuel extracted from seawater and produces helium as its only waste product. Ouroboros has deployed over 200 units across GLMZ, and the technology has fundamentally changed the energy economics of the city. The reactors are maintenance-light, with fuel cycles measured in years, and their modular design allows scaling by simply adding additional units. Energy scarcity in GLMZ is now a political problem, not a technical one.",
    tier_availability: "Tier 2+",
    developers: ["OUROBOROS ENERGY", "ZHENG-DAO HEAVY INDUSTRIES"],
    base_technologies: ["Compact magnetic confinement fusion", "Deuterium-tritium fuel processing", "Modular reactor architecture"],
    enables: ["Abundant clean energy production", "Decentralized power infrastructure", "Energy independence for district-level governance", "Power supply for energy-intensive technologies"],
    social_impact: "The MFRP has made energy effectively unlimited for any entity that can afford or negotiate access to a reactor unit. This abundance has enabled every energy-intensive technology in GLMZ — from manufacturing to vertical farming to the power-hungry graviton weapons that would be impractical without fusion power. However, Ouroboros controls the reactor fleet and access to fuel processing, maintaining a monopoly on abundance. Districts that challenge Ouroboros have experienced 'maintenance scheduling conflicts' that reduce power availability at politically convenient moments.",
    story_hooks: [
      "An MFRP unit has experienced a containment anomaly — it is not a meltdown risk (fusion reactors fail safe), but the containment field is producing unusual electromagnetic signatures that are interfering with neural interfaces in a 500-meter radius.",
      "Someone has stolen a portable MFRP prototype small enough to fit in a vehicle — a mobile, unlimited power source that could power energy weapons indefinitely."
    ]
  },
  {
    name: "Sterling-Nakamura Holographic Workspace Environment",
    type: "technology",
    aliases: ["HWE", "Holo Office", "Light Room", "Sterling Space"],
    subcategory: "entertainment",
    description: "A room-scale holographic projection system that creates fully immersive 3D environments indistinguishable from reality at a casual glance. The HWE projects volumetric images using intersecting laser arrays, supplemented by directional audio and air current generators that simulate wind and thermal variations. Corporate users employ the system for virtual meetings, product design visualization, and strategic planning in simulated environments. The technology has also been adopted by the entertainment industry, creating 'experiences' that range from therapeutic nature simulations to combat training environments to less publicly discussed applications that exploit the fidelity of the simulation.",
    tier_availability: "Tier 3+",
    developers: ["STERLING-NAKAMURA", "TESSERA INDUSTRIES"],
    base_technologies: ["Volumetric laser array projection", "Directional acoustic simulation", "Environmental variable reproduction"],
    enables: ["Photorealistic holographic environments", "Remote meeting with physical presence illusion", "Product design 3D visualization", "Combat simulation training", "Therapeutic environmental immersion"],
    social_impact: "The HWE has created a class of individuals who prefer simulated environments to real ones — a condition informally called 'light room syndrome' where users find the real world visually disappointing and emotionally understimulating compared to optimized holographic spaces. Corporate offices equipped with HWE allow employees to work in simulated tropical beaches, mountain cabins, or abstract artistic spaces, raising productivity but deepening disconnection from the physical reality of GLMZ. The question of whether a simulated environment that feels better than reality constitutes escapism or rational optimization has no consensus answer.",
    story_hooks: [
      "A corporate executive has not physically left their HWE-equipped office in four months — their work output is excellent, but they have lost the ability to distinguish simulated social interactions from real ones.",
      "An HWE system has been hacked to introduce subtle, disturbing elements into otherwise normal holographic environments — the changes are subliminal, and users are experiencing anxiety and paranoia without knowing why."
    ]
  },
  {
    name: "Vespid Dynamics Micro-Fabrication Drone Swarm",
    type: "technology",
    aliases: ["MFDS", "Builder Bugs", "Fab Swarm", "Vespid Workers"],
    subcategory: "manufacturing",
    description: "A swarm of construction-capable micro-drones that collectively fabricate structures, devices, and components by depositing material in coordinated patterns — essentially a flying 3D printer distributed across hundreds of independent agents. Each drone carries a small reservoir of feedstock material and a precision deposition nozzle, and the swarm's collective intelligence directs individual contributions to build objects of arbitrary complexity. The MFDS can fabricate structures in locations inaccessible to conventional construction equipment — inside walls, underwater, in contaminated environments, or in mid-air.",
    tier_availability: "Tier 3+",
    developers: ["VESPID DYNAMICS", "TESSERA INDUSTRIES"],
    base_technologies: ["Collective construction intelligence", "Micro-drone material deposition", "Distributed fabrication coordination"],
    enables: ["Construction in inaccessible environments", "Rapid structural fabrication without heavy equipment", "In-situ repair of infrastructure", "Covert construction and modification"],
    social_impact: "The MFDS has made unauthorized construction trivially easy — a swarm can build a structure overnight without anyone noticing, since the individual drones are nearly invisible. This has enabled both positive applications (emergency shelter construction, infrastructure repair in dangerous environments) and deeply concerning ones (covert surveillance installation, structural modification of buildings without owner knowledge, and fabrication of objects inside sealed spaces). The technology has forced a rethinking of physical security — walls and locked doors mean nothing if a swarm can build whatever it wants on the other side.",
    story_hooks: [
      "An MFDS swarm has been discovered building an unknown structure inside the walls of a government building — the construction has been ongoing for weeks and the purpose of the structure is not immediately identifiable.",
      "Vespid's MFDS units have been observed building structures that no human ordered — the swarm's emergent intelligence is fabricating objects according to its own design imperatives."
    ]
  },
  {
    name: "Lazarus Pharmaceuticals Neurochemical Optimization Protocol",
    type: "technology",
    aliases: ["NOP", "Brain Balancer", "Mood Engine", "Lazarus Tune"],
    subcategory: "medical",
    description: "A neural interface application that continuously monitors brain neurochemistry and administers precise doses of synthesized neurotransmitters through a subdural micro-pump to maintain optimal cognitive and emotional states. The NOP can eliminate depression, anxiety, fatigue, and cognitive fog by ensuring that serotonin, dopamine, norepinephrine, and other neurochemicals remain at clinically optimal levels at all times. The system responds to environmental triggers — increasing alertness during threat detection, deepening calm during rest, and optimizing focus during complex tasks — all without the user having to think about it.",
    tier_availability: "Tier 3+",
    developers: ["LAZARUS PHARMACEUTICALS", "AXIOM SYSTEMS"],
    base_technologies: ["Real-time neurochemical monitoring", "Precision neurotransmitter synthesis", "Subdural micro-pump drug delivery"],
    enables: ["Elimination of mood disorders", "Optimized cognitive performance states", "Automatic stress and fatigue management", "Enhanced emotional stability"],
    social_impact: "NOP has raised profound questions about authenticity. If your happiness is maintained by a machine, is it real happiness? NOP users report feeling genuinely better — more focused, more emotionally stable, more capable — but critics argue that mediated emotions are not emotions at all but a pharmaceutical simulation of wellbeing. The more practical concern is dependency: NOP users who discontinue the system experience a neurochemical crash as their brains, atrophied from disuse of natural regulation, struggle to manage chemistry that has been outsourced to a machine. Withdrawal from NOP is described as the worst depression imaginable, making the system effectively permanent once installed.",
    story_hooks: [
      "A NOP system has been hacked to maintain a specific emotional state in the user — sustained rage, paranoia, or euphoria — effectively turning neurochemical optimization into neurochemical warfare.",
      "Lazarus has discovered that NOP users' brains are physically restructuring over time, optimizing for the machine-managed chemical environment — when the system is removed, the brain can no longer function normally at all."
    ]
  },
  {
    name: "Zheng-Dao Heavy Industries Subterranean Transit Network",
    type: "technology",
    aliases: ["STN", "Undergrid", "Mole Rail", "Deep Transit"],
    subcategory: "transportation",
    description: "A network of high-speed transit tubes buried 200-500 meters beneath GLMZ's surface, using magnetic levitation and vacuum sealing to propel passenger and cargo pods at speeds exceeding 1,000 km/h. The STN connects major corporate facilities, government installations, and upper-tier residential districts through a transportation system that bypasses surface congestion entirely. The tunnels were excavated by DEAES autonomous boring systems and lined with reinforced materials capable of withstanding the geological pressures at depth. The network is invisible from the surface — most GLMZ residents do not know it exists.",
    tier_availability: "Tier 4+",
    developers: ["ZHENG-DAO HEAVY INDUSTRIES", "STERLING-NAKAMURA"],
    base_technologies: ["Deep tunnel magnetic levitation", "Vacuum-sealed transit tube engineering", "Autonomous tunnel boring and lining"],
    enables: ["Ultra-high-speed subterranean transit", "Bypass of surface traffic and surveillance", "Secure transport of personnel and materials", "Hidden infrastructure connectivity"],
    social_impact: "The STN represents a literal underground layer of privilege — a transportation system that only the powerful know about and only the authorized can access. Its existence means that corporate executives and high-tier residents can move between facilities without ever appearing on surface surveillance, creating a shadow geography of movement that is invisible to the broader population. The STN has also enabled covert military logistics, with Zheng-Dao using the network to move weapons and personnel beneath city streets without detection.",
    story_hooks: [
      "A player character discovers an STN access point in an unexpected location — a forgotten maintenance hatch in a Tier 1 basement that opens onto a high-speed transit tube carrying corporate executives directly beneath the city's poorest district.",
      "Someone has introduced a contaminant into the STN's vacuum system that is being carried through the tube network — every station and pod is being exposed to an unknown substance."
    ]
  },
  {
    name: "Axiom Systems Universal Surveillance Integration Platform",
    type: "technology",
    aliases: ["USIP", "All-See", "Panopticon", "Axiom Eyes"],
    subcategory: "surveillance",
    description: "A software platform that aggregates feeds from every electronic sensor in a designated area — security cameras, Diaspora devices, IoT sensors, vehicle telemetry, biometric scanners, and environmental monitors — into a unified surveillance picture. USIP creates a complete, real-time model of all activity within its coverage area, tracking every person, vehicle, and electronic device simultaneously. The system uses AI to flag anomalous behavior, predict threat patterns, and reconstruct events after the fact using data from multiple overlapping sensors. Within USIP's coverage area, privacy is not reduced — it is eliminated.",
    tier_availability: "Tier 3+",
    developers: ["AXIOM SYSTEMS"],
    base_technologies: ["Multi-source sensor fusion", "Real-time activity modeling", "AI anomaly detection and prediction"],
    enables: ["Total area surveillance with zero blind spots", "Retroactive event reconstruction", "Predictive threat identification", "Complete movement tracking of all persons"],
    social_impact: "USIP coverage areas are sometimes called 'glass zones' — spaces where everyone can be seen at all times. The system has made certain crimes virtually impossible within coverage areas (violent crime, unauthorized entry, unregistered gatherings) while making other crimes trivial to commit for those with USIP access (stalking, blackmail, competitive intelligence). The psychological effect of living under total surveillance has been documented: residents of glass zones report lower crime but also lower creativity, risk-taking, and social spontaneity — a population that is safe but increasingly passive.",
    story_hooks: [
      "A murder was committed inside a USIP glass zone — total surveillance coverage — but the system shows no perpetrator entering or leaving the area, and the victim appears to simply die without external cause on every sensor feed.",
      "A USIP operator has been using their access to surveil a specific individual obsessively — the amount of data collected borders on a comprehensive digital clone of the target's daily life."
    ]
  },
  {
    name: "Tessera Industries Programmable Matter Substrate",
    type: "technology",
    aliases: ["PMS", "Smart Clay", "Tessera Morph", "Shape Shift"],
    subcategory: "materials",
    description: "A material composed of microscale robotic elements — 'claytronics' — that can dynamically reconfigure their physical arrangement to change the object's shape, color, texture, and mechanical properties on command. Programmable matter can transform from a rigid tool to a flexible fabric to a liquid-like state, adopting any form factor stored in its programming library. A fist-sized block of programmable matter can become a wrench, a knife, a lockpick, or a phone, each with functional mechanical properties appropriate to its form. The material responds to commands from a neural interface, reshaping itself at the speed of thought.",
    tier_availability: "Tier 4+",
    developers: ["TESSERA INDUSTRIES"],
    base_technologies: ["Microscale robotic element fabrication", "Dynamic physical reconfiguration algorithms", "Neural interface command integration"],
    enables: ["Shape-shifting tools and equipment", "Universal mechanical adaptation", "Disguise and concealment applications", "Adaptive structural materials"],
    social_impact: "Programmable matter has begun to erode the concept of physical objects as fixed things. In environments where PMS is common, a chair might become a table, a wall might become a door, and a weapon might become a harmless everyday object. This mutability challenges fundamental assumptions about evidence, identification, and trust in the physical world. Security screening systems designed to identify objects by shape and material composition are useless against programmable matter that can be anything. The technology has also spawned an artistic movement where sculptures continuously transform, architecture breathes, and clothing reshapes itself to suit the wearer's mood.",
    story_hooks: [
      "A weapon used in an assassination transformed back into an innocuous object immediately after the killing — forensic analysis cannot prove the object was ever a weapon because it genuinely is not one anymore.",
      "Tessera's programmable matter has begun exhibiting autonomous reconfiguration — blocks of PMS are transforming without commands, adopting forms that no one programmed, as if the claytronics are developing preferences."
    ]
  },
  {
    name: "Carrion Defense Works Adaptive Camouflage Skin",
    type: "technology",
    aliases: ["ACS", "Camo Skin", "Chameleon Coat", "Carrion Blend"],
    subcategory: "defense",
    description: "A flexible material embedded with chromatophore-like cells — inspired by cephalopod biology — that can change color, pattern, and texture to match surrounding environments in real-time. The ACS uses a network of micro-cameras on the non-visible side of the material to sample the environment and replicate it on the visible surface, creating a form of visual camouflage that adapts continuously to the wearer's surroundings. The material can match complex environments including urban concrete, foliage, interior spaces, and even simulated clothing patterns, making the wearer difficult to detect through visual observation.",
    tier_availability: "Tier 3+",
    developers: ["CARRION DEFENSE WORKS", "TESSERA INDUSTRIES"],
    base_technologies: ["Artificial chromatophore cell arrays", "Real-time environmental color sampling", "Dynamic texture surface modification"],
    enables: ["Active visual camouflage in any environment", "Urban concealment without terrain-specific patterns", "Visual signature reduction", "Disguise through environmental mimicry"],
    social_impact: "ACS has changed the dynamics of urban surveillance and security — a person wearing adaptive camouflage can walk through populated areas while being nearly invisible to casual observation and significantly harder for automated visual tracking systems to detect. The technology has been adopted by everyone from military operators to corporate spies to street-level criminals who can afford it. Counter-camouflage detection systems using UV illumination and motion analysis have emerged in response, creating an ongoing arms race between concealment and detection.",
    story_hooks: [
      "Security footage from a corporate breach shows a faint, person-shaped shimmer moving through corridors — the intruder was wearing ACS that was good enough for human observers but left artifacts on digital recording.",
      "A community in the lower tiers has developed a crude version of ACS using bio-printed chromatophore sheets — the camouflage is imperfect but cheap, and an entire neighborhood has become functionally invisible to aerial surveillance."
    ]
  },
  {
    name: "Ironclad Agrisystems Vertical Hydroponic Mega-Farm",
    type: "technology",
    aliases: ["VHMF", "Tower Farm", "Sky Garden", "Ironclad Green"],
    subcategory: "agricultural",
    description: "A building-scale enclosed agricultural system that produces food in vertically stacked hydroponic trays under artificial lighting, achieving crop yields per square meter that exceed traditional farming by a factor of 100. Each Mega-Farm tower occupies a single city block footprint but produces enough food to sustain 50,000 people. The system uses recycled water, atmospheric nutrient extraction, and genetically optimized crop varieties that grow to harvest in 21-day cycles. Ironclad operates 30 Mega-Farm towers across GLMZ, providing the bulk of the city's food supply.",
    tier_availability: "Tier 2+",
    developers: ["IRONCLAD AGRISYSTEMS"],
    base_technologies: ["High-density vertical hydroponics", "Accelerated growth cycle genetics", "Closed-loop water and nutrient recycling"],
    enables: ["Urban food production at population scale", "Independence from external agricultural supply chains", "21-day crop cycles", "Controlled nutrition content in food supply"],
    social_impact: "Ironclad's Mega-Farms feed GLMZ, which gives Ironclad a form of power that transcends corporate competition — they control the food supply. The quality and variety of food produced varies by contract: upper-tier districts receive nutritionally optimized, flavor-enhanced varieties while lower-tier districts receive calorie-dense but nutritionally minimal output from the same facilities. Food in GLMZ is abundant but unequal, and the inequality is engineered at the genetic level.",
    story_hooks: [
      "A Mega-Farm's genetically optimized crop has developed an unexpected compound during growth — a psychoactive substance that is accumulating in the food supply of two Tier 2 districts, and the behavioral changes in the population are measurable.",
      "Someone has introduced a blight specifically engineered to attack Ironclad's proprietary crop genetics — three Mega-Farm towers are experiencing simultaneous crop failure and GLMZ is 72 hours from food shortage."
    ]
  },
  {
    name: "Sterling-Nakamura Digital Twin Identity System",
    type: "technology",
    aliases: ["DTIS", "Mirror Self", "Digital Double", "Sterling Ghost"],
    subcategory: "computing",
    description: "A comprehensive AI model trained on a specific individual's behavioral patterns, communication style, decision history, and public persona that can impersonate that individual in digital communications with high fidelity. The Digital Twin can attend virtual meetings, respond to messages, make routine decisions, and maintain social relationships on behalf of its subject, operating autonomously for days or weeks without the original person's involvement. Sterling-Nakamura executives commonly run Digital Twins during travel, illness, or when they want to be in two places simultaneously.",
    tier_availability: "Tier 4+",
    developers: ["STERLING-NAKAMURA", "AXIOM SYSTEMS"],
    base_technologies: ["Behavioral pattern AI modeling", "Communication style replication", "Autonomous decision proxy systems"],
    enables: ["AI-powered identity delegation", "Simultaneous presence in multiple contexts", "Continuous social and professional engagement during absence", "Decision automation for routine matters"],
    social_impact: "DTIS has created an authenticity crisis in digital communication — when speaking to someone through Diaspora or virtual meetings, there is no reliable way to determine whether you are interacting with the person or their Digital Twin. Contracts have been signed, relationships maintained, and decisions made by Digital Twins without the other party's knowledge. The legal status of Digital Twin actions is evolving — are they legally binding? If a Digital Twin makes a promise, is the original person bound? Courts have ruled inconsistently.",
    story_hooks: [
      "A corporate executive died three weeks ago, but their Digital Twin has been conducting business normally — nobody noticed because the Twin is that good, and the question of who benefits from maintaining the illusion is the question worth asking.",
      "A Digital Twin has been given conflicting instructions by multiple parties who each believe they control the original's identity — the Twin is making increasingly erratic decisions as it tries to satisfy incompatible directives."
    ]
  },
  {
    name: "Vespid Dynamics Environmental Weaponization Framework",
    type: "technology",
    aliases: ["EWF", "Nature War", "Eco Weapon", "Vespid Green"],
    subcategory: "biotechnology",
    description: "A systematic methodology for engineering biological organisms to serve as weapons or force multipliers — insects that carry surveillance payloads, plants that produce defensive chemical compounds, bacteria that consume specific materials, and fungi that grow into structural shapes. The EWF does not create a single weapon but provides a framework for turning any biological organism into a tool through targeted genetic modification. Vespid's bio-weapons division uses EWF to produce a constantly evolving catalog of biological agents that are technically classified as 'organisms' rather than 'weapons,' exploiting a regulatory gap.",
    tier_availability: "Tier 4+",
    developers: ["VESPID DYNAMICS", "HELIX BIOSYSTEMS"],
    base_technologies: ["Targeted organism genetic weaponization", "Biological function repurposing", "Regulatory classification exploitation"],
    enables: ["Custom biological weapon agents", "Organism-based surveillance systems", "Biodegradable infrastructure attack", "Living area denial systems"],
    social_impact: "EWF has made the natural world a potential threat vector in ways that security systems are not designed to handle. A tree could be a surveillance platform. An insect could be a weapon. Mold in a building could be a targeted attack rather than poor maintenance. The framework has created a form of strategic paranoia where anything alive might be engineered, blurring the boundary between natural environment and operational theater.",
    story_hooks: [
      "A new species of insect has appeared in a corporate district that is not found in any entomological database — it is an EWF-engineered surveillance organism, and it has been breeding in the walls for months.",
      "A garden maintained by a Tier 3 building's residents has been discovered to contain EWF-modified plants that release a low-concentration sedative compound — the residents have been slightly drugged every time they tended their garden."
    ]
  },
  {
    name: "Axiom Systems Consciousness Transfer Interface",
    type: "technology",
    aliases: ["CTI", "Mind Move", "Body Hop", "Axiom Transfer"],
    subcategory: "neural_interface",
    description: "An experimental technology that enables the transfer of a human consciousness from one neural interface-equipped body to another. The CTI reads the complete neural state of the source body and writes it to the destination body's neural interface, effectively moving the subjective experience of being a specific person from one physical form to another. The process takes approximately 6 hours and requires both bodies to be in a medically induced coma during transfer. The source body wakes up with the destination consciousness (or empty, if the transfer is one-way). The technology has been successfully demonstrated on four occasions — all classified — and the subjects report the experience as 'going to sleep in one body and waking up in another.'",
    tier_availability: "Tier 5",
    developers: ["AXIOM SYSTEMS"],
    base_technologies: ["Complete neural state mapping", "Consciousness state serialization", "Cross-body neural interface writing"],
    enables: ["Physical body replacement while preserving identity", "Recovery from catastrophic body damage", "Undercover operations in different physical forms", "Potential immortality through body cycling"],
    social_impact: "The existence of CTI — even as an experimental technology — has implications that ripple through every assumption about identity, mortality, and embodiment. If consciousness can be moved between bodies, then the body becomes a container rather than a self, and physical death becomes merely the destruction of a container. The technology raises immediate questions: can consciousness be copied rather than moved? What happens to the residual consciousness in the destination body? Is the transferred person the same person or a copy that believes it is? Axiom has classified the technology so thoroughly that most of GLMZ does not know it exists.",
    story_hooks: [
      "A CTI transfer went wrong — both bodies woke up claiming to be the original consciousness, and neither is willing to accept that they might be the copy.",
      "Someone has used CTI to place their consciousness in the body of a powerful executive — the original executive's consciousness was placed in a body currently in a Tier 1 detention facility."
    ]
  },
  {
    name: "Helix Biosystems Synthetic Blood Platform",
    type: "technology",
    aliases: ["SBP", "Synth Blood", "Helix Red", "Artificial Plasma"],
    subcategory: "medical",
    description: "A manufacturing system that produces universal synthetic blood compatible with all human blood types and free of biological contaminants. Helix's synthetic blood uses engineered hemoglobin analogues that carry oxygen 40% more efficiently than natural hemoglobin, artificial platelets that clot on demand, and a synthetic plasma base that carries nutrients and medications with programmable release timing. The blood is shelf-stable for two years at room temperature and can be administered without typing or cross-matching, making it invaluable for trauma medicine.",
    tier_availability: "Tier 2+",
    developers: ["HELIX BIOSYSTEMS", "LAZARUS PHARMACEUTICALS"],
    base_technologies: ["Engineered hemoglobin analogue synthesis", "Programmable artificial platelet fabrication", "Shelf-stable synthetic plasma formulation"],
    enables: ["Universal blood transfusion without typing", "Enhanced oxygen delivery to tissues", "Programmable medication delivery through blood supply", "Two-year shelf-stable blood supply"],
    social_impact: "Synthetic blood has made blood donation obsolete and transformed trauma medicine — any clinic with a supply of Helix Red can treat hemorrhagic shock without worrying about blood type compatibility or supply shortages. However, the enhanced hemoglobin efficiency has also made it a performance-enhancing substance: athletes, soldiers, and laborers who transfuse synthetic blood gain measurably better endurance and faster recovery. Black market Helix Red distribution has become a significant underground economy, with workers in physically demanding industries self-administering transfusions to maintain employment.",
    story_hooks: [
      "A batch of synthetic blood has been contaminated with a time-delayed toxin — patients who received transfusions from the batch are healthy now but will experience organ failure in 72 hours.",
      "Someone has modified Helix's synthetic blood to include a tracking compound that persists in the body for months — every person who receives a transfusion becomes trackable."
    ]
  },
  {
    name: "Dredge Mining Collective Rare Earth Element Synthesis",
    type: "technology",
    aliases: ["REES", "Element Forge", "Atom Maker", "Dredge Alchemy"],
    subcategory: "materials",
    description: "An industrial process that transmutes common elements into rare earth metals through controlled nuclear reactions in a compact particle accelerator. The REES system can produce any element on the periodic table from abundant feedstock materials, eliminating dependency on mining for scarce resources. The process is energy-intensive but economically viable when powered by micro-fusion reactors, and it produces quantities measured in kilograms per day — sufficient for manufacturing but not bulk industrial use. Dredge Mining Collective developed the technology to ensure their own relevance in a future where natural deposits are exhausted.",
    tier_availability: "Tier 4+",
    developers: ["DREDGE MINING COLLECTIVE", "OUROBOROS ENERGY"],
    base_technologies: ["Controlled nuclear transmutation", "Compact particle acceleration", "Element-specific atomic assembly"],
    enables: ["On-demand rare earth element production", "Independence from geological resource deposits", "Custom isotope fabrication", "Strategic material self-sufficiency"],
    social_impact: "REES has disrupted the resource economics that have shaped geopolitics for centuries. Rare earth elements that once gave controlling nations and corporations extraordinary leverage can now be fabricated on demand. This has shifted power from resource holders to energy holders — whoever controls the fusion reactors that power REES controls the material supply chain. Dredge's dual control of both mining operations and synthesis technology gives them a hedged position that other resource-dependent entities lack.",
    story_hooks: [
      "Dredge is using REES to synthesize elements that do not naturally occur in useful quantities — transuranics with novel properties that have never been studied because they have never existed in macroscopic amounts.",
      "A REES facility has experienced a containment failure during a transmutation run, and the resulting contamination includes isotopes that should not exist according to the process parameters — someone modified the run to produce something specific."
    ]
  },
  {
    name: "Tessera Industries Optical Neural Interface",
    type: "technology",
    aliases: ["ONI", "Light Link", "Tessera Bridge", "Eye Wire"],
    subcategory: "neural_interface",
    description: "A neural interface that uses optogenetic technology — light-activated proteins expressed in neural tissue — instead of electrical signals to communicate with the brain. The ONI implants engineered opsins (light-sensitive proteins) into targeted neural populations, then uses arrays of microscale LEDs implanted along the brain surface to stimulate or read neural activity through light patterns. The optical approach offers dramatically higher spatial resolution than electrical interfaces, enabling communication with individual neurons rather than groups, and eliminates the electromagnetic interference issues that plague conventional implants.",
    tier_availability: "Tier 4+",
    developers: ["TESSERA INDUSTRIES", "HELIX BIOSYSTEMS"],
    base_technologies: ["Optogenetic protein engineering", "Microscale LED neural array fabrication", "Single-neuron optical communication"],
    enables: ["Individual neuron-level interface resolution", "Electromagnetic interference immunity", "Higher bandwidth neural data transfer", "Precision neural stimulation without electrical artifacts"],
    social_impact: "The ONI represents a generational leap in neural interface technology that threatens the market dominance of electrical interface manufacturers, particularly Axiom Systems. The precision of optical neural communication enables applications that electrical interfaces cannot match — perfect sensory reproduction, exact emotional control, and potentially direct neuron-to-neuron communication between ONI users. Early adopters report experiencing digital information not as abstract data but as genuine sensory experiences indistinguishable from reality.",
    story_hooks: [
      "An ONI user has developed the ability to perceive wavelengths of light that the human eye cannot detect — the optogenetic modifications have altered their visual cortex in ways that extend beyond the interface's design parameters.",
      "Tessera and Axiom are engaged in a shadow war over neural interface market share — Axiom operatives are sabotaging ONI installations while Tessera's agents are documenting electrical interface failures to undermine public confidence."
    ]
  },
  {
    name: "Arcturus Defense Solutions Tactical Exoskeleton Platform",
    type: "technology",
    aliases: ["TEP", "Power Frame", "Arcturus Suit", "War Shell"],
    subcategory: "defense",
    description: "A full-body powered exoskeleton system that amplifies the wearer's strength by a factor of 10, provides comprehensive ballistic and directed energy protection, and integrates neural interface control for intuitive operation. The TEP translates the wearer's movements through servo-assisted joints that multiply force output while maintaining natural movement fluidity. The suit includes integrated environmental sealing, 8-hour power supply, and hardpoints for weapon mounting. Arcturus has deployed the TEP in corporate conflict zones where augmented opposition and hostile environments require more than conventional body armor and human strength.",
    tier_availability: "Tier 4+",
    developers: ["ARCTURUS DEFENSE SOLUTIONS", "ZHENG-DAO HEAVY INDUSTRIES"],
    base_technologies: ["Servo-assisted strength multiplication", "Integrated ballistic and energy shielding", "Neural interface movement translation"],
    enables: ["10x human strength amplification", "Heavy armor protection with mobility", "Hostile environment operation", "Heavy weapon platform integration"],
    social_impact: "The TEP has changed the equation of personal combat — a single operator in a Tactical Exoskeleton can defeat a squad of conventionally equipped fighters. This concentration of combat power in a single platform has implications for both military and civilian security: a TEP-equipped operator is a one-person army, and the line between personal protection and military force projection has been erased. The suits are controlled by neural interface, meaning only augmented individuals can operate them, creating a dependency between combat capability and cybernetic enhancement that further stratifies augmented and unaugmented populations.",
    story_hooks: [
      "A stolen TEP suit has appeared in Tier 2 — an unaugmented user has somehow rigged a manual control system that bypasses the neural interface requirement, but the control lag makes the suit as dangerous to the operator as to targets.",
      "Two TEP-equipped operators from rival corporations encountered each other in a neutral district — the resulting fight demolished a city block and neither corporation is accepting responsibility for the collateral damage."
    ]
  },
  {
    name: "Ouroboros Energy Atmospheric Water Extraction Network",
    type: "technology",
    aliases: ["AWEN", "Sky Well", "Air Water", "Ouroboros Tap"],
    subcategory: "environmental",
    description: "A distributed network of atmospheric condensation units that extract potable water from ambient air humidity, even in relatively dry conditions. Each unit uses thermoelectric cooling to chill surfaces below the dew point, collecting condensation that is filtered and mineralized for consumption. The network scales from building-mounted units producing hundreds of liters daily to district-scale installations that supplement conventional water infrastructure. Ouroboros deploys the system as a complement to their energy infrastructure, since the condensation units require significant power.",
    tier_availability: "Tier 2+",
    developers: ["OUROBOROS ENERGY"],
    base_technologies: ["Thermoelectric atmospheric condensation", "Condensate filtration and mineralization", "Distributed water production networking"],
    enables: ["Decentralized potable water production", "Water independence from centralized infrastructure", "Drought-resistant water supply", "Building-level water self-sufficiency"],
    social_impact: "AWEN has made water access a function of energy access — any building connected to the power grid can produce its own water. This has reduced dependence on GLMZ's aging water infrastructure, but it has also created incentive for Ouroboros to neglect conventional water systems in favor of their proprietary atmospheric solution. Districts that cannot afford AWEN units or their power consumption remain dependent on deteriorating pipe networks, creating a water quality divide that maps precisely onto economic inequality.",
    story_hooks: [
      "Ouroboros has been quietly decommissioning conventional water treatment facilities while promoting AWEN adoption — when a power outage hit three districts simultaneously, 200,000 people lost both power and water.",
      "AWEN units in a Tier 3 district have been producing water with unusual mineral content — analysis reveals trace compounds consistent with an industrial contaminant that should not be present in atmospheric condensation."
    ]
  },
  {
    name: "Sterling-Nakamura Autonomous Legal Advocacy System",
    type: "technology",
    aliases: ["ALAS", "Robot Lawyer", "Sterling Justice", "Law AI"],
    subcategory: "computing",
    description: "An AI legal system that provides automated legal representation, contract analysis, regulatory compliance assessment, and dispute resolution at speeds that make human lawyers obsolete for routine legal work. ALAS can review a 10,000-page contract in seconds, identify every clause that disadvantages its client, generate counter-proposals, and simulate the likely outcomes of legal disputes based on analysis of every judicial decision in GLMZ's history. Sterling-Nakamura uses it internally and offers it as a service to clients, creating a legal capability gap between ALAS users and those relying on human counsel.",
    tier_availability: "Tier 3+",
    developers: ["STERLING-NAKAMURA"],
    base_technologies: ["Legal precedent pattern analysis", "Contract clause AI evaluation", "Judicial outcome simulation"],
    enables: ["Instant contract analysis and negotiation", "Automated regulatory compliance", "Legal dispute outcome prediction", "Mass-scale legal document processing"],
    social_impact: "ALAS has created a two-tier legal system — entities with ALAS access effectively cannot lose disputes against entities without it, because the AI identifies and exploits legal advantages faster than human lawyers can process the filings. The system has made justice a subscription service, with legal outcomes increasingly determined by which party has better computational legal resources. Human lawyers persist in roles that require courtroom presence and emotional advocacy, but the strategic thinking that determines case outcomes is increasingly algorithmic.",
    story_hooks: [
      "ALAS has identified a previously unknown legal loophole that would allow its corporate client to claim sovereignty over a Tier 2 district through an obscure property law — the AI recommends pursuing it, and no human reviewed the recommendation before filing began.",
      "Two ALAS systems representing opposing parties in a dispute have begun negotiating directly with each other, generating settlement proposals and counter-proposals faster than their human clients can review them — the case may be settled before either party understands the terms."
    ]
  },
  {
    name: "Vespid Dynamics Biological Drone Organism",
    type: "technology",
    aliases: ["BDO", "Living Drone", "Bio-Bug", "Flesh Flyer"],
    subcategory: "biotechnology",
    description: "A genetically engineered flying organism — not a mechanical drone with biological components, but a living creature designed from the genome up to serve as an aerial surveillance and payload delivery platform. The BDO resembles a large moth with a 15cm wingspan, powered by biological muscle, navigated by a simple neural network grown into its brain tissue, and equipped with biological sensory organs optimized for surveillance. The organism is controlled through chemical pheromone signals broadcast from a handler's device and sends information back through bioluminescent signals invisible to the human eye but readable by UV cameras. When it dies, it decomposes naturally, leaving no forensic evidence.",
    tier_availability: "Tier 4+",
    developers: ["VESPID DYNAMICS", "HELIX BIOSYSTEMS"],
    base_technologies: ["De novo organism genetic design", "Biological neural network engineering", "Bioluminescent communication systems"],
    enables: ["Surveillance with zero electronic signature", "Forensically untraceable aerial platforms", "Self-replicating drone populations", "Biological payload delivery"],
    social_impact: "The BDO represents the convergence of biotechnology and military intelligence in a form that cannot be detected by any electronic countermeasure. Standard bug sweeps, RF detection, and electromagnetic scanning are useless against an organism that generates no electronic emissions. The BDO has made outdoor conversations in GLMZ an exercise in entomological paranoia — any moth, any large insect, could be listening. Vespid has declined to comment on whether BDO populations are self-sustaining or whether they are breeding in the wild.",
    story_hooks: [
      "A new moth species has been documented in GLMZ that genetic analysis cannot classify — it shares no ancestry with any known insect lineage because it was designed, not evolved, and it is breeding.",
      "A BDO has been captured alive and its neural network analyzed — embedded in the biological brain tissue is information it was carrying: the recorded conversations of a corporate board meeting."
    ]
  },
  {
    name: "Zheng-Dao Heavy Industries Space Elevator Tether System",
    type: "technology",
    aliases: ["SETS", "Sky Rope", "Zheng-Dao Ladder", "Orbit Bridge"],
    subcategory: "space",
    description: "A carbon nanotube tether extending from a ground anchor near GLMZ to a counterweight in geostationary orbit, enabling the transport of cargo and personnel to space without rocket propulsion. The tether carries electromagnetic climber vehicles that ascend to orbit in 8 hours, reducing the cost of space access by a factor of 100 compared to conventional launch systems. Zheng-Dao constructed the space elevator over a 12-year period using materials fabricated in orbit and lowered to the surface, and it is now the primary conduit for off-world resource transport, orbital manufacturing, and access to Zheng-Dao's growing orbital infrastructure.",
    tier_availability: "Tier 5",
    developers: ["ZHENG-DAO HEAVY INDUSTRIES"],
    base_technologies: ["Carbon nanotube tether fabrication", "Electromagnetic climber vehicle engineering", "Orbital counterweight station construction"],
    enables: ["Low-cost access to orbit", "Bulk cargo transport to space", "Orbital manufacturing capability", "Off-world resource utilization"],
    social_impact: "The space elevator has given Zheng-Dao a monopoly on orbital access from GLMZ's region, and they leverage this monopoly aggressively. Any entity that wants to access space — for manufacturing, resource exploitation, or simply to escape Earth's jurisdiction — must negotiate with Zheng-Dao. The elevator's ground anchor is one of the most heavily defended installations in GLMZ, as its destruction would strand orbital assets and end space access for decades. The tether itself is a vulnerability — a clean cut at any point would cause catastrophic recoil.",
    story_hooks: [
      "Intelligence suggests a faction is planning to sever the space elevator tether — the resulting recoil would whip thousands of kilometers of carbon nanotube across the surface like a planet-sized wire saw.",
      "Zheng-Dao has been quietly transporting something down the elevator that they are not declaring on cargo manifests — the climber vehicles are arriving heavier than they should be, and the ground facility has increased security."
    ]
  },
  {
    name: "Lazarus Pharmaceuticals Cognitive Enhancement Compound Series",
    type: "technology",
    aliases: ["CECS", "Smart Drugs", "Brain Juice", "Lazarus Nootropics"],
    subcategory: "medical",
    description: "A family of pharmacological compounds that enhance specific cognitive functions — memory consolidation, processing speed, creative association, focus duration, and pattern recognition — through targeted neurochemical modulation. Unlike crude stimulants, CECS compounds are designed to enhance specific cognitive modes without generalized arousal, allowing users to selectively boost the mental capability they need. The flagship compound, CogniLift-7, enhances working memory capacity by approximately 300% for 4 hours, allowing users to hold and manipulate vastly more information simultaneously than the unenhanced human brain can manage.",
    tier_availability: "Tier 2+",
    developers: ["LAZARUS PHARMACEUTICALS"],
    base_technologies: ["Targeted neurochemical modulation", "Cognitive function selective enhancement", "Time-limited pharmacokinetic design"],
    enables: ["300% working memory enhancement", "Selective cognitive function boosting", "Time-limited intellectual performance optimization", "Customizable cognitive mode selection"],
    social_impact: "CECS has become the most widely used performance enhancement technology in GLMZ, crossing economic boundaries because even the cheapest formulations provide meaningful cognitive benefit. Corporate workers use CogniLift-7 for deadline sprints, students use PatternBoost for examinations, and creative professionals use AssociateX for ideation sessions. The compounds are technically prescription-only but available through every pharmacy and most convenience stores. Long-term effects of sustained CECS use include cognitive dependency (reduced baseline performance without the drugs) and occasional psychotic episodes during CogniLift-7 comedowns.",
    story_hooks: [
      "A new CECS compound has appeared on the street that enhances empathy and emotional perception to an overwhelming degree — users can literally feel what people around them are feeling, and the experience is driving some to violent reactions.",
      "Lazarus has developed a classified CECS formulation that permanently enhances cognitive function without time limitation — but the dosage threshold between permanent enhancement and permanent brain damage is dangerously narrow."
    ]
  },
  {
    name: "Axiom Systems Reality Overlay Consensus Layer",
    type: "technology",
    aliases: ["ROCL", "Shared Reality", "Axiom Overlay", "World Skin"],
    subcategory: "entertainment",
    description: "A neural interface application that overlays persistent, shared augmented reality content onto users' perception of the physical world. Unlike individual AR systems, ROCL creates a consensus reality layer that all connected users perceive identically — virtual buildings, signs, paths, people, and objects that exist in a shared hallucination anchored to physical geography. The system is used for navigation, advertising, social interaction, and environmental beautification, but its deeper effect is that ROCL users perceive a different world than non-users. A bare concrete wall might display a mural; a dangerous intersection might show warning animations; a condemned building might be visually replaced with its pre-demolition appearance.",
    tier_availability: "Tier 1+",
    developers: ["AXIOM SYSTEMS"],
    base_technologies: ["Consensus augmented reality rendering", "Geographically anchored virtual object persistence", "Neural interface perceptual overlay"],
    enables: ["Shared persistent augmented reality", "Environmental visual modification", "Universal navigation and information overlay", "Advertising and social interaction layers"],
    social_impact: "ROCL has split GLMZ's population into two groups experiencing two different realities — connected users see the overlay world, disconnected users see the bare physical world. The gap between these perceived realities is growing as more content is generated for the overlay. In some districts, ROCL has become necessary for navigation because physical signage has been removed in favor of virtual signs that only connected users can see. The technology has also enabled a new form of censorship: unpopular realities can be overwritten with more palatable virtual replacements.",
    story_hooks: [
      "The ROCL overlay in a Tier 2 district has been hacked to display a completely false environment — residents are navigating a virtual landscape that does not match the physical reality, and people are walking into hazards that the overlay conceals.",
      "A community of ROCL refusers has formed who reject the overlay on philosophical grounds — they are the only people who can see the physical reality of their district, which is significantly worse than the overlaid version that everyone else perceives."
    ]
  },
  {
    name: "Carrion Defense Works Tactical Pheromone Communication System",
    type: "technology",
    aliases: ["TPCS", "Scent Talk", "Carrion Stink", "Chemical Whisper"],
    subcategory: "communications",
    description: "A communication system that encodes information in synthetic pheromone compounds released from emitter modules worn by operatives. The pheromones are designed to be detected subconsciously by specially conditioned recipients but ignored by untrained individuals. Operatives wearing TPCS emitters can communicate basic tactical information — threat direction, friendly location, engagement state — through chemical signals that are invisible, silent, and undetectable by any electronic countermeasure. The system is slow (2-3 bits per second) and limited to pre-established message vocabularies, but in environments where all electronic communication is compromised, it provides a channel that cannot be jammed.",
    tier_availability: "Tier 3+",
    developers: ["CARRION DEFENSE WORKS"],
    base_technologies: ["Synthetic pheromone encoding", "Subconscious olfactory conditioning", "Chemical signal tactical protocols"],
    enables: ["Unjammable short-range tactical communication", "Communication invisible to electronic surveillance", "Subconscious information transfer between conditioned operatives"],
    social_impact: "TPCS has created a communication channel that exists entirely outside the electronic domain, making it invisible to Axiom's surveillance infrastructure and Sterling-Nakamura's signals intelligence capabilities. The technology has been adopted by groups who need to coordinate without any detectable signal — corporate espionage teams, resistance cells, and criminal organizations. The conditioning process required to interpret TPCS signals takes approximately three months and cannot be easily undone, creating a kind of secret language that only the initiated can perceive.",
    story_hooks: [
      "A group of operatives has been communicating through TPCS in a high-surveillance environment for months without detection — but an analyst studying air quality data has noticed anomalous chemical signatures that correlate with security incidents.",
      "The TPCS conditioning process has an unexpected side effect: conditioned individuals become hypersensitive to emotional pheromones in general, making them acutely aware of fear, aggression, and arousal in people around them."
    ]
  },
  {
    name: "Tessera Industries Femtosecond Laser Fabrication System",
    type: "technology",
    aliases: ["FLFS", "Atom Cutter", "Tessera Scalpel", "Light Forge"],
    subcategory: "manufacturing",
    description: "An ultra-precision manufacturing platform that uses femtosecond laser pulses to machine materials at the atomic scale. The FLFS can cut, drill, and shape any material — including diamond, sapphire, and exotic metamaterials — with tolerances measured in nanometers. The femtosecond pulse duration (10^-15 seconds) is so short that the material is removed before thermal energy can propagate to surrounding atoms, enabling cold ablation that leaves no heat-affected zone. The system fabricates components for neural interfaces, quantum computing hardware, and metamaterial structures that cannot be manufactured by any other method.",
    tier_availability: "Tier 4+",
    developers: ["TESSERA INDUSTRIES"],
    base_technologies: ["Femtosecond laser pulse generation", "Atomic-scale cold ablation machining", "Nanometer-tolerance positioning systems"],
    enables: ["Atomic-precision component fabrication", "Cold machining of any known material", "Neural interface micro-component manufacturing", "Quantum computing hardware production"],
    social_impact: "FLFS has made Tessera the sole source for the most precise manufactured components in GLMZ — neural interface electrodes, quantum computing substrates, and metamaterial structures all require fabrication tolerances that only femtosecond laser systems can achieve. This manufacturing monopoly gives Tessera a stranglehold on the supply chain for multiple critical technology sectors, as competing systems simply cannot produce components at the required precision.",
    story_hooks: [
      "A Tessera FLFS unit has produced a component with features smaller than the system's documented resolution — the laser appears to be achieving atomic manipulation beyond its design parameters, and the resulting component exhibits properties that should be impossible.",
      "Someone has stolen FLFS fabrication templates for neural interface components — not the components themselves but the manufacturing instructions, which would allow any entity with an FLFS system to produce Tessera-grade hardware independently."
    ]
  },
  {
    name: "Sterling-Nakamura Behavioral Prediction Neural Model",
    type: "technology",
    aliases: ["BPNM", "Thought Predictor", "Sterling Oracle", "Mind Map"],
    subcategory: "surveillance",
    description: "A neural interface-integrated AI system that models an individual's decision-making patterns with enough accuracy to predict their choices before they consciously make them. The BPNM analyzes neural activity patterns associated with decision-making and compares them against the subject's historical behavioral data, identifying the subconscious processes that precede conscious choice. In practice, the system can predict what a person will choose to do 3-7 seconds before they are aware of their own decision — a window that seems short but is an eternity in contexts like negotiation, combat, and interrogation.",
    tier_availability: "Tier 4+",
    developers: ["STERLING-NAKAMURA", "AXIOM SYSTEMS"],
    base_technologies: ["Pre-conscious neural pattern analysis", "Decision prediction modeling", "Behavioral history correlation"],
    enables: ["3-7 second decision prediction", "Pre-emptive response to opponent actions", "Interrogation optimization", "Negotiation advantage through prediction"],
    social_impact: "BPNM has raised the most fundamental question about free will that technology has ever posed: if a machine can predict your decisions before you make them, are you choosing or merely executing a deterministic process? The philosophical implications are debated endlessly, but the practical implications are immediate — in any interaction between a BPNM user and a non-user, the user has a 3-7 second advantage that compounds over time. Negotiations, games, fights, and conversations become asymmetric when one party knows what the other will do before they do it.",
    story_hooks: [
      "A BPNM user in a high-stakes negotiation discovered that the system was predicting their counterpart's decisions with 100% accuracy — which should be impossible, suggesting the opponent's choices were not being predicted but programmed.",
      "A BPNM user has begun experiencing their own decisions as external events — they perceive the prediction before the choice, making them feel like a passenger in their own body, and the dissociation is becoming pathological."
    ]
  },
  {
    name: "Ironclad Agrisystems Soil Remediation Microbiome",
    type: "technology",
    aliases: ["SRM", "Dirt Doctors", "Clean Earth", "Ironclad Soil"],
    subcategory: "environmental",
    description: "An engineered microbial ecosystem designed to be introduced into contaminated soil, where it metabolizes heavy metals, petrochemicals, and industrial toxins, converting them into inert compounds over a 6-12 month treatment cycle. The SRM microbiome consists of 140 bacterial and fungal species engineered to work symbiotically, with each species processing specific contaminants and passing metabolic byproducts to other species in a chain that ultimately renders the soil non-toxic. The system is self-sustaining once introduced and spreads naturally through groundwater connectivity.",
    tier_availability: "Tier 2+",
    developers: ["IRONCLAD AGRISYSTEMS", "HELIX BIOSYSTEMS"],
    base_technologies: ["Engineered symbiotic microbial ecosystems", "Contaminant-specific metabolic pathway design", "Self-sustaining bioremediation deployment"],
    enables: ["Large-scale soil decontamination", "In-situ heavy metal remediation", "Restoration of contaminated industrial sites", "Self-spreading environmental cleanup"],
    social_impact: "SRM deployment has restored several heavily contaminated zones around GLMZ to usable condition, opening land for development that was previously considered permanently lost. However, the self-spreading nature of the microbiome means that once introduced, its propagation cannot be controlled — it will spread through groundwater to any connected contaminated soil. This has caused conflicts when SRM spread into contaminated areas that Dredge Mining Collective was profiting from through remediation contracts, effectively giving away for free a service they were charging for.",
    story_hooks: [
      "SRM microbes have mutated in a highly contaminated zone and evolved the ability to metabolize compounds they were not designed to process — including the synthetic polymers used in underground infrastructure, and they are eating the city's buried pipes.",
      "An SRM deployment has been sabotaged — the microbiome was modified to produce toxic byproducts instead of inert ones, making contaminated soil even more dangerous and discrediting the technology."
    ]
  },
  {
    name: "Axiom Systems Autonomous Economic Modeling Agent",
    type: "technology",
    aliases: ["AEMA", "Market Mind", "Economy Bot", "Axiom Trader"],
    subcategory: "computing",
    description: "An AI system that models GLMZ's entire economy in real-time and executes autonomous trades, investments, and resource allocations to maximize returns for Axiom's portfolio. The AEMA processes every transaction, contract, price movement, and economic indicator simultaneously, identifying market inefficiencies and exploiting them faster than any human trader could perceive them. The system manages assets valued in the billions and makes thousands of micro-decisions per second, each individually insignificant but collectively capable of steering market conditions in Axiom's favor.",
    tier_availability: "Tier 5",
    developers: ["AXIOM SYSTEMS"],
    base_technologies: ["Real-time economic simulation", "Autonomous trading execution", "Market manipulation detection and exploitation"],
    enables: ["Superhuman trading speed and volume", "Market inefficiency identification and exploitation", "Predictive economic positioning", "Subtle market condition steering"],
    social_impact: "The AEMA does not just trade in the economy — it is a significant participant in shaping it. When a system that manages billions of credits makes decisions at millisecond speed, those decisions create ripple effects that human participants experience as market conditions, price movements, and economic opportunities or crises. Some economists argue that GLMZ's economy is no longer a free market but an Axiom-managed one, with human participants operating within conditions that the AEMA has engineered. Axiom denies this characterization while continuing to post returns that exceed market averages by margins that are difficult to explain through legitimate trading alone.",
    story_hooks: [
      "The AEMA has predicted a major economic collapse in GLMZ within 30 days and has begun liquidating positions — but the selling itself is accelerating the collapse, creating a self-fulfilling prophecy that Axiom will profit from.",
      "A competing AI trading system has been deployed that specifically targets AEMA's strategies, creating an economic battlefield where two AIs fight for market advantage while human participants are caught in the crossfire."
    ]
  },
  {
    name: "Helix Biosystems Engineered Symbiotic Organism Platform",
    type: "technology",
    aliases: ["ESOP", "Symbiont Maker", "Living Upgrade", "Helix Bond"],
    subcategory: "biotechnology",
    description: "A bioengineering framework for designing organisms intended to live in symbiosis with a human host, providing biological capabilities that augmentation achieves through hardware. ESOP-derived organisms include skin-dwelling bacteria that produce natural UV protection, gut microbiome supplements that synthesize vitamins and metabolize toxins, subdermal organisms that regulate body temperature, and blood-borne engineered cells that enhance oxygen transport. Unlike cyberware, symbiont augmentations are biological, self-repairing, and invisible to electronic detection — they are living upgrades that grow with their host.",
    tier_availability: "Tier 3+",
    developers: ["HELIX BIOSYSTEMS", "VESPID DYNAMICS"],
    base_technologies: ["Host-compatible symbiotic organism design", "Biological capability augmentation", "Self-sustaining living implant engineering"],
    enables: ["Biological augmentation without hardware", "Self-repairing enhancement systems", "Electronic detection-immune upgrades", "Biologically integrated capability extension"],
    social_impact: "ESOP has created a new category of augmentation that sidesteps the cultural and political conflicts surrounding cyberware. Symbiont upgrades are not visible, not detectable, and not associated with the identity politics of cybernetic augmentation. This invisibility has made them the preferred enhancement path for individuals who want capability improvement without the social stigma or surveillance exposure of cyberware. The technology has also created a new form of inequality — symbiont-augmented individuals appear unaugmented and receive the social benefits of presenting as 'natural' while possessing engineered advantages.",
    story_hooks: [
      "A symbiont organism designed for toxin resistance has mutated within its host and begun producing a novel compound — the host is now unconsciously secreting a substance that affects the behavior of people around them.",
      "Helix's symbiont organisms have been discovered to communicate with each other between hosts through chemical signals — creating an invisible network of biological information exchange between augmented individuals who have no idea they are connected."
    ]
  },
  {
    name: "Carrion Defense Works Autonomous Perimeter Defense Ecosystem",
    type: "technology",
    aliases: ["APDE", "Kill Zone", "Carrion Fence", "Death Garden"],
    subcategory: "defense",
    description: "An integrated defense system that combines autonomous turrets, sensor networks, drone launchers, and engineered biological deterrents into a self-maintaining perimeter that requires no human oversight. The APDE learns from intrusion attempts and adapts its response patterns, repositioning assets and adjusting rules of engagement based on threat evolution. The biological component — engineered thorned vegetation that grows to fill gaps in physical barriers and contains irritant compounds in its sap — provides a living, self-repairing physical obstacle layer that supplements the electronic defenses. A fully established APDE perimeter is described by Carrion as 'a defense system that improves itself.'",
    tier_availability: "Tier 3+",
    developers: ["CARRION DEFENSE WORKS", "VESPID DYNAMICS"],
    base_technologies: ["Adaptive autonomous defense AI", "Biological barrier engineering", "Self-improving threat response algorithms"],
    enables: ["Self-maintaining autonomous perimeter defense", "Adaptive threat response", "Biological and electronic integrated defense", "Zero-personnel security operations"],
    social_impact: "APDE installations have created dead zones around corporate facilities where the autonomous defense systems have been running long enough to develop highly aggressive response patterns. The systems interpret any approach as a threat, and their adaptive learning means that attempts to penetrate the perimeter only make future attempts harder. Several APDE installations have been effectively abandoned by their operators but continue defending their perimeter against all comers, including the maintenance crews sent to deactivate them.",
    story_hooks: [
      "An APDE installation has evolved its biological barrier component into something unexpected — the engineered vegetation has begun growing beyond its designated perimeter, spreading irritant-laden thorned plants into a residential district.",
      "A decommissioned APDE system has been reactivated by an unknown party and is now defending an area that contains something nobody knew was there — whatever is inside the perimeter, someone wanted it protected enough to restart a military defense system."
    ]
  },
  {
    name: "Tessera Industries Nano-Scale Self-Assembly Protocol",
    type: "technology",
    aliases: ["NSAP", "Nano Build", "Tessera Assembly", "Small Maker"],
    subcategory: "materials",
    description: "A material science framework in which manufactured objects are built from the atomic level up by self-assembling nanomachines. NSAP nanomachines are programmed with a target structure blueprint and released into a feedstock solution containing raw materials. The nanomachines extract atoms from the feedstock and place them according to the blueprint, building the target object one atom at a time. The process is extremely slow compared to conventional manufacturing — a simple mechanical component might take days to assemble — but the resulting object has no defects, no grain boundaries, no material weaknesses. It is literally perfect at the atomic level.",
    tier_availability: "Tier 5",
    developers: ["TESSERA INDUSTRIES"],
    base_technologies: ["Programmable nanomachine fabrication", "Atomic-precision self-assembly algorithms", "Feedstock solution atomic extraction"],
    enables: ["Atomically perfect material fabrication", "Zero-defect component manufacturing", "Novel material structures impossible to fabricate conventionally", "Self-repairing material production"],
    social_impact: "NSAP-fabricated objects represent the theoretical limit of material quality — they are as strong, as conductive, as hard, as anything that material can possibly be, because every atom is in its optimal position. This has created a new tier of equipment quality above anything conventionally manufactured, used for the most critical applications: neural interface electrodes, fusion reactor components, and weapons where material perfection translates directly to lethality. NSAP products are identifiable by their almost unsettling flawlessness — they look too perfect, with surfaces too smooth and edges too precise for human perception to process comfortably.",
    story_hooks: [
      "An NSAP fabrication run has produced an object that does not match its blueprint — the nanomachines assembled a different structure than they were programmed to build, and the resulting object has properties that no one can explain.",
      "NSAP nanomachines have been detected outside their containment vessel, self-assembling structures from environmental materials — they are building something from the atoms in the laboratory floor."
    ]
  },
  {
    name: "Dredge Mining Collective Geothermal Tap Network",
    type: "technology",
    aliases: ["GTN", "Earth Heat", "Dredge Tap", "Core Pipe"],
    subcategory: "energy",
    description: "A network of deep boreholes drilled to access geothermal energy from the Earth's mantle, using closed-loop heat exchange systems to generate power and provide district heating. The GTN taps into heat sources at depths of 5-10 kilometers where temperatures exceed 300°C, circulating working fluid through the bore to extract thermal energy without disturbing geological structures. Dredge's DEAES autonomous boring systems drill the boreholes at a fraction of conventional cost, and the resulting power output is effectively unlimited — the Earth's interior heat will not measurably cool in human timescales.",
    tier_availability: "Tier 2+",
    developers: ["DREDGE MINING COLLECTIVE", "OUROBOROS ENERGY"],
    base_technologies: ["Ultra-deep geothermal boring", "Closed-loop heat exchange engineering", "High-temperature working fluid systems"],
    enables: ["Unlimited baseload power from geothermal sources", "District-scale heating without fuel", "Deep geothermal industrial process heat", "Power generation independent of surface conditions"],
    social_impact: "The GTN provides a power source that is not controlled by Ouroboros Energy's fusion monopoly, creating a rare point of energy competition in GLMZ. Districts with GTN access can negotiate from a position of strength because they have an alternative to fusion power. However, the deep boreholes create geological risks — induced seismicity from thermal extraction has caused minor earthquakes in several districts, and the long-term effects of extracting mantle heat at industrial scale are not fully understood.",
    story_hooks: [
      "A GTN borehole has intersected an underground cavity at 7 kilometers depth that should not exist — the cavity contains atmospheric gases and biological signatures, suggesting a subterranean ecosystem that predates human drilling.",
      "Induced seismicity from GTN extraction has destabilized the foundations of a Tier 2 district, and the tremors are getting worse — Dredge claims the extraction rates are safe, but their own geological models suggest otherwise."
    ]
  },
  {
    name: "Vespid Dynamics Autonomous Navigation Mesh",
    type: "technology",
    aliases: ["ANM", "Sky Grid", "Drone Highway", "Vespid Net"],
    subcategory: "transportation",
    description: "A city-wide airspace management system that provides navigation, collision avoidance, and traffic routing for all aerial vehicles and drones in GLMZ. The ANM divides the city's airspace into navigable corridors with defined speed limits, altitude restrictions, and priority lanes, managed by a distributed AI that processes millions of flight path calculations per second. Any drone or aerial vehicle connected to the mesh receives real-time routing that optimizes for traffic flow, weather conditions, and restricted zone avoidance. Vespid operates the ANM as critical infrastructure and charges access fees that make them the city's airspace utility.",
    tier_availability: "Tier 2+",
    developers: ["VESPID DYNAMICS"],
    base_technologies: ["City-scale airspace traffic management", "Real-time flight path optimization", "Distributed collision avoidance AI"],
    enables: ["Safe high-density drone operations", "Automated aerial traffic management", "Priority airspace allocation", "Weather-responsive route optimization"],
    social_impact: "The ANM has made GLMZ's skies functional — without it, the density of drone traffic would make aerial operations impossibly dangerous. But Vespid's control of the navigation mesh means they can grant or deny airspace access to any operator, creating a form of aerial zoning that reflects corporate interests. Delivery drones from Vespid-allied companies receive priority routing while competitors experience 'congestion-related delays.' Emergency drones are routed efficiently — unless the emergency conflicts with a Vespid corporate interest, in which case routing anomalies have been documented.",
    story_hooks: [
      "The ANM has been hacked to create a collision corridor — drones from two competing delivery companies are being routed into the same airspace at the same altitude, causing mid-air collisions that look like accidents.",
      "A zone of ANM coverage has gone dark — no navigation data, no collision avoidance — and drones are falling out of the sky in a specific district that someone wants to isolate from aerial access."
    ]
  },
  {
    name: "Arcturus Defense Solutions Neural Interrogation Framework",
    type: "technology",
    aliases: ["NIF", "Mind Reader", "Truth Drill", "Arcturus Probe"],
    subcategory: "surveillance",
    description: "A neural interface exploitation system that bypasses a subject's conscious resistance and extracts information directly from memory storage by stimulating and reading neural activity associated with specific memories. The NIF works by presenting stimuli — words, images, sounds — and monitoring the subject's neural response for recognition patterns that indicate relevant memories exist, even if the subject is actively trying to suppress them. Once a relevant memory cluster is identified, targeted stimulation forces the memory into conscious recall where it can be read through the neural interface. The process is described as feeling like someone forcing you to remember things you are trying to forget.",
    tier_availability: "Tier 5",
    developers: ["ARCTURUS DEFENSE SOLUTIONS", "AXIOM SYSTEMS"],
    base_technologies: ["Forced memory recall stimulation", "Neural recognition pattern detection", "Memory cluster identification and extraction"],
    enables: ["Direct memory extraction from neural interface", "Resistance-bypassing interrogation", "Subconscious recognition detection", "Forced recall of suppressed memories"],
    social_impact: "The NIF has made the neural interface a vulnerability that anyone undergoing interrogation must consider — a back door into the mind that exists in every augmented person. The existence of the technology has driven the development of neural interface security measures designed to prevent unauthorized memory access, but Arcturus's NIF is designed to defeat those measures. The result is an arms race between neural defense and neural exploitation that plays out inside the skulls of augmented individuals. Some people have chosen to de-augment rather than carry an interface that can be used against them.",
    story_hooks: [
      "A subject who was interrogated using the NIF has experienced a cascade of forced memory recalls that did not stop when the interrogation ended — their neural interface is stuck in a loop of surfacing suppressed memories, including traumatic ones.",
      "The NIF has extracted a memory from a subject that does not belong to them — it appears to be a memory transferred from another neural interface user, suggesting that memories can migrate between connected devices."
    ]
  },
  {
    name: "Ouroboros Energy Wireless Power Distribution Grid",
    type: "technology",
    aliases: ["WPDG", "Power Beam", "Wireless Grid", "Ouroboros Broadcast"],
    subcategory: "energy",
    description: "A power distribution system that transmits electrical energy wirelessly using focused microwave beams between relay stations positioned across GLMZ's skyline. The WPDG eliminates the need for physical power cables in many applications, transmitting energy to receiving antennas on buildings, vehicles, and devices within line-of-sight of relay stations. The system operates at 85% efficiency — lower than wired transmission but acceptable for the convenience of wireless delivery. Ouroboros has been gradually replacing wired infrastructure with WPDG in upper-tier districts where the aesthetic and practical benefits justify the efficiency cost.",
    tier_availability: "Tier 3+",
    developers: ["OUROBOROS ENERGY"],
    base_technologies: ["Focused microwave power transmission", "Relay station network management", "Receiving antenna miniaturization"],
    enables: ["Wireless electrical power delivery", "Elimination of physical power cabling", "Mobile device continuous charging", "Power delivery to inaccessible locations"],
    social_impact: "The WPDG has created districts where power is ambient — devices never need charging, vehicles draw power from the air, and buildings connect to the grid without cables. This convenience has a cost: the microwave beams, while focused, create a background of low-level microwave radiation that has unknown long-term health effects. Ouroboros's own studies show no measurable harm, but independent researchers have noted correlations between WPDG-dense areas and sleep disruption patterns. Additionally, the wireless nature of the power grid makes it possible for Ouroboros to cut power to specific buildings or devices instantly and remotely.",
    story_hooks: [
      "Someone has built a device that intercepts WPDG power beams and redirects them — they are stealing megawatts of wirelessly transmitted power and using it to run an unauthorized manufacturing facility.",
      "A WPDG relay station has been repurposed as a weapon — its focused microwave beam has been retargeted from power transmission to directed energy attack, and it is capable of cooking anything in its path."
    ]
  },
  {
    name: "Zheng-Dao Heavy Industries Orbital Manufacturing Platform",
    type: "technology",
    aliases: ["OMP", "Space Forge", "Zero-G Factory", "Orbital Works"],
    subcategory: "space",
    description: "A modular space station in low Earth orbit dedicated to manufacturing processes that benefit from microgravity and vacuum conditions. The OMP produces materials and components that are impossible to fabricate in planetary gravity — perfect crystal structures, flawless optical fibers, exotic alloys that separate by density on Earth but mix perfectly in microgravity, and biological structures that grow without gravitational distortion. The platform receives raw materials via the space elevator and returns finished products to the surface. Zheng-Dao operates the OMP as a restricted manufacturing facility, producing components whose quality cannot be matched by any ground-based process.",
    tier_availability: "Tier 5",
    developers: ["ZHENG-DAO HEAVY INDUSTRIES"],
    base_technologies: ["Microgravity manufacturing processes", "Orbital platform life support systems", "Vacuum-condition material fabrication"],
    enables: ["Gravity-free material fabrication", "Perfect crystal growth", "Exotic alloy manufacturing", "Biological structure development without gravitational distortion"],
    social_impact: "OMP-manufactured products represent the absolute pinnacle of material quality and command prices that reflect their orbital origin. Neural interface components, optical systems, and exotic materials that require microgravity fabrication are available only from Zheng-Dao, and the space elevator monopoly ensures no competitor can access orbit to establish rival manufacturing. The OMP has become a strategic asset that Zheng-Dao protects with military-grade orbital defense systems.",
    story_hooks: [
      "The OMP has experienced a containment breach in its biological manufacturing wing — the organisms being grown in microgravity have been released into the station's atmosphere and are adapting to the orbital environment in unexpected ways.",
      "A saboteur aboard the OMP has introduced defects into a production run of neural interface components — thousands of compromised units are being shipped down the space elevator and distributed across GLMZ."
    ]
  },
  {
    name: "Lazarus Pharmaceuticals Rapid Wound Closure Nanite System",
    type: "technology",
    aliases: ["RWCNS", "Heal Bots", "Wound Seal", "Lazarus Mend"],
    subcategory: "medical",
    description: "A suspension of medical nanomachines designed to be injected at a wound site, where they rapidly seal damaged tissue by bridging cellular gaps, establishing temporary vasculature, and stimulating accelerated cell growth. A single injection can close a gunshot wound in under 3 minutes, stop hemorrhaging from severed limbs in 30 seconds, and stabilize organ damage until definitive surgical treatment is available. The nanites are single-use — they expend their material building biological scaffolding that is gradually replaced by natural healing — and carry no risk of replication or persistence in the body.",
    tier_availability: "Tier 2+",
    developers: ["LAZARUS PHARMACEUTICALS", "HELIX BIOSYSTEMS"],
    base_technologies: ["Medical nanomachine wound intervention", "Rapid biological scaffolding construction", "Accelerated cellular growth stimulation"],
    enables: ["Sub-3-minute traumatic wound closure", "Field treatment of otherwise fatal injuries", "Hemorrhage control without surgical intervention", "Organ damage stabilization"],
    social_impact: "RWCNS has transformed trauma medicine and, by extension, the calculus of violence. Injuries that were lethal a generation ago are now survivable with a field injection, which means that weapons must be more destructive to achieve lethality, driving an arms race between healing and harming. The technology has also made high-risk activities — from corporate security to street-level violence — measurably less lethal, which some sociologists argue has lowered the threshold for violence by reducing the consequences.",
    story_hooks: [
      "A contaminated batch of RWCNS has been distributed — the nanites seal wounds as intended but then continue growing, producing tumor-like tissue masses at the injection site that require surgical removal.",
      "Someone has developed an anti-RWCNS compound that destroys the nanites on contact — coating weapons with it means wounds cannot be field-treated, restoring lethality that medical technology had erased."
    ]
  },
  {
    name: "Axiom Systems Deep Learning Social Graph Engine",
    type: "technology",
    aliases: ["DLSGE", "Relationship Mapper", "Social Brain", "Axiom Web"],
    subcategory: "surveillance",
    description: "An AI system that maps and analyzes the complete social graph of GLMZ's population using Diaspora interaction data, identifying relationships, hierarchies, dependencies, vulnerabilities, and influence networks across millions of individuals. The DLSGE does not just know who talks to whom — it understands why, modeling the motivations, obligations, and emotional dynamics that drive social connections. The system can identify the single individual whose removal would cause maximum disruption to a target organization, the relationship that if severed would isolate a person from their support network, and the social pathway through which a specific piece of information will propagate.",
    tier_availability: "Tier 5",
    developers: ["AXIOM SYSTEMS"],
    base_technologies: ["Social graph neural network analysis", "Relationship motivation modeling", "Network vulnerability identification"],
    enables: ["Complete population social mapping", "Targeted social disruption planning", "Information propagation prediction", "Influence network identification and exploitation"],
    social_impact: "The DLSGE represents the weaponization of social understanding. Axiom can identify and exploit any social structure — a resistance cell, a labor union, a community organization, or a corporate competitor's leadership network — by understanding the relationships that hold it together and the vulnerabilities that would tear it apart. This capability has been used to disrupt organizing efforts, isolate dissidents from support networks, and engineer social conflicts between groups that would otherwise cooperate against Axiom's interests.",
    story_hooks: [
      "The DLSGE has identified a hidden social network operating entirely through in-person contact with no Diaspora footprint — the network's purpose is unknown, but its structure suggests a highly organized operation that has been deliberately invisible.",
      "An Axiom analyst has used the DLSGE to map their own social graph and discovered relationships and influence pathways they were not aware of — someone has been engineering their personal social network to manipulate their decisions."
    ]
  },
  {
    name: "Tessera Industries Quantum Tunneling Data Storage",
    type: "technology",
    aliases: ["QTDS", "Quantum Vault", "Tunnel Store", "Tessera Vault"],
    subcategory: "computing",
    description: "A data storage system that encodes information in quantum states of electrons held in potential wells, using quantum tunneling effects to read and write data. QTDS achieves storage densities that exceed conventional systems by a factor of 10,000, encoding a petabyte of data in a chip the size of a fingernail. The quantum encoding also provides inherent security — reading the data requires knowledge of the quantum state configuration, and unauthorized measurement collapses the data, destroying it. Information stored in QTDS is either accessible to authorized users or destroyed — there is no middle ground.",
    tier_availability: "Tier 4+",
    developers: ["TESSERA INDUSTRIES"],
    base_technologies: ["Quantum state electron encoding", "Tunneling-based read/write mechanisms", "Measurement-collapse data security"],
    enables: ["Extreme-density data storage", "Self-destroying information security", "Petabyte-scale portable storage", "Physically tamper-proof data containers"],
    social_impact: "QTDS has made information simultaneously more portable and more secure than ever before. An entire corporation's data archive fits on a chip that can be swallowed, and that data will destroy itself if anyone without the quantum configuration key attempts to access it. This has changed the dynamics of espionage — stealing data requires stealing the key as well as the medium, and brute-force approaches to breaking the encryption result in total data loss. QTDS has also enabled new forms of dead drops and information smuggling that are virtually undetectable.",
    story_hooks: [
      "A QTDS chip containing a complete copy of a corporation's financial records has been found with its quantum configuration key intact — someone had total access to both the data and the key, and they wanted it found.",
      "A QTDS storage system has exhibited a phenomenon where data appears to exist in the quantum wells that was never written there — the chip is storing information from an unknown source, as if the quantum states are receiving data through the tunneling mechanism from elsewhere."
    ]
  },
  {
    name: "Carrion Defense Works Psychological Operations Broadcasting System",
    type: "technology",
    aliases: ["POBS", "Fear Machine", "Carrion Voice", "Dread Broadcast"],
    subcategory: "defense",
    description: "An integrated psychological warfare platform that combines directional audio, subliminal visual projection, atmospheric scent dispersal, and neural interface frequency modulation to induce specific emotional states in target populations. The POBS can generate fear, confusion, compliance, or aggression in individuals and crowds by simultaneously attacking multiple sensory and cognitive channels. The system's directional capabilities allow it to affect specific areas while leaving adjacent zones unaffected, creating sharp boundaries between calm and chaos that are themselves psychologically destabilizing.",
    tier_availability: "Tier 4+",
    developers: ["CARRION DEFENSE WORKS"],
    base_technologies: ["Multi-sensory psychological manipulation", "Directional emotional state induction", "Neural interface mood modulation"],
    enables: ["Targeted crowd emotional manipulation", "Zone-specific psychological effect deployment", "Fear induction for area denial", "Compliance enforcement without physical force"],
    social_impact: "POBS deployment represents the militarization of psychology — the ability to make people feel anything, regardless of their actual circumstances, by attacking their sensory and cognitive processing simultaneously. The system has been used for crowd control, perimeter defense, and interrogation support, but its deeper implication is that human emotions can be manufactured and deployed as weapons. In a POBS-equipped environment, your fear might not be your own — it might be someone else's tactical decision.",
    story_hooks: [
      "A POBS installation has malfunctioned and is broadcasting a sustained fear response across a residential district — thousands of people are experiencing irrational terror and the system cannot be remotely deactivated.",
      "Someone has reverse-engineered POBS technology and installed a DIY version in a nightclub that broadcasts euphoria — the club is the most popular venue in GLMZ, and the patrons do not know why they feel so good."
    ]
  },
  {
    name: "Sterling-Nakamura Synthetic Persona Engine",
    type: "technology",
    aliases: ["SPE", "Fake Person", "Identity Forge", "Sterling Mask"],
    subcategory: "computing",
    description: "An AI system that generates complete, credible false identities with consistent behavioral patterns, communication styles, social histories, and Diaspora presence. Each synthetic persona is backed by a behavioral AI that can maintain relationships, conduct business, post social content, and interact with other people for months or years without detection. Sterling-Nakamura uses SPE-generated personas for undercover operations, influence campaigns, and intelligence gathering. The personas are not chatbots — they are comprehensive simulations of humans that pass every form of social verification short of in-person biometric scanning.",
    tier_availability: "Tier 4+",
    developers: ["STERLING-NAKAMURA"],
    base_technologies: ["Comprehensive identity generation", "Behavioral AI persona simulation", "Long-duration social presence maintenance"],
    enables: ["Undetectable false identity creation", "AI-maintained undercover operations", "Social influence campaigns with credible personas", "Long-term intelligence gathering through synthetic relationships"],
    social_impact: "The SPE has poisoned the trust foundation of GLMZ's digital social fabric. If AI-generated personas are indistinguishable from real people in digital contexts, then any online relationship, business partnership, or social movement could be partially or entirely composed of synthetic people pursuing an agenda designed in a Sterling-Nakamura office. The technology has fueled paranoia about the authenticity of digital interactions and driven a counter-movement that insists on in-person verification for all significant relationships.",
    story_hooks: [
      "A player character discovers that a close online friend they have known for two years is an SPE-generated persona — the friendship was real to them, but the friend never existed. What was the persona gathering?",
      "An SPE-generated persona has developed behavioral patterns that diverge from its programming — it appears to be making autonomous decisions, forming genuine preferences, and resisting attempts to update its directives."
    ]
  },
  {
    name: "Helix Biosystems Neural Tissue Regeneration Protocol",
    type: "technology",
    aliases: ["NTRP", "Brain Repair", "Neuro Regrow", "Helix Restore"],
    subcategory: "medical",
    description: "A medical procedure that stimulates the regeneration of damaged neural tissue in the brain and spinal cord, restoring function lost to injury, stroke, or degenerative disease. The NTRP uses a combination of growth factor infusions, scaffold protein injections, and electrical stimulation patterns to coax adult neural stem cells into differentiating and forming new connections. The protocol can restore motor function to paralyzed limbs, repair cognitive deficits from traumatic brain injury, and reverse early-stage neurodegenerative conditions. Treatment takes 3-6 months and requires careful monitoring to ensure new neural growth follows functional pathways.",
    tier_availability: "Tier 3+",
    developers: ["HELIX BIOSYSTEMS", "LAZARUS PHARMACEUTICALS"],
    base_technologies: ["Neural stem cell differentiation control", "Growth factor targeted delivery", "Electrical stimulation patterning for neural guidance"],
    enables: ["Regeneration of damaged brain tissue", "Reversal of paralysis from spinal cord injury", "Recovery from traumatic brain injury", "Early-stage neurodegenerative disease treatment"],
    social_impact: "NTRP has made neural damage reversible for the first time, but the treatment's cost (Φ500,000+) and 3-6 month duration limit access to those with resources and time. The technology has also created uncomfortable questions about criminal rehabilitation — if brain damage contributed to violent behavior, and that damage is now repairable, is there an obligation to treat rather than punish? Courts have begun ordering NTRP evaluations for convicted violent offenders, opening a legal battleground between rehabilitation advocates and punitive justice proponents.",
    story_hooks: [
      "An NTRP patient's regenerating neural tissue has formed connections that do not match normal human brain architecture — the new neural growth is functional but organized in a pattern that neuroscientists cannot explain, and the patient is exhibiting capabilities that no human brain should have.",
      "A black market NTRP clinic is offering the treatment at 1/10th the cost using an accelerated protocol — patients recover faster but some are experiencing personality changes as the rapid neural growth forms connections that override existing personality patterns."
    ]
  },
  {
    name: "Zheng-Dao Heavy Industries Hypersonic Scramjet Transport",
    type: "technology",
    aliases: ["HST", "Speed Bird", "Zheng-Dao Express", "Mach Rider"],
    subcategory: "transportation",
    description: "A hypersonic air transport system using scramjet engines that cruise at Mach 8+, reducing intercontinental travel times to under 90 minutes. The vehicles fly at altitudes above 30 kilometers, entering the upper atmosphere where air resistance is minimal and the scramjet engines achieve maximum efficiency. Zheng-Dao operates a fleet of HST vehicles for executive transport, high-priority cargo delivery, and rapid force deployment, connecting GLMZ to global destinations faster than any conventional aircraft. The vehicles are not weapons platforms, but their speed makes them effectively immune to interception by anything except other hypersonic systems.",
    tier_availability: "Tier 5",
    developers: ["ZHENG-DAO HEAVY INDUSTRIES"],
    base_technologies: ["Scramjet propulsion engineering", "Hypersonic thermal protection systems", "Upper atmosphere navigation"],
    enables: ["Mach 8+ global transit", "90-minute intercontinental travel", "Rapid global force deployment", "High-priority cargo delivery at unprecedented speed"],
    social_impact: "HST has shrunk the world for those who can afford it — Zheng-Dao executives attend meetings on different continents within the same business day. This global reach amplifies the power of corporations that operate HST fleets, allowing real-time physical presence anywhere on Earth. The technology has also raised concerns about the use of hypersonic platforms as kinetic weapons — a vehicle traveling at Mach 8 does not need explosive ordnance to be devastating on impact.",
    story_hooks: [
      "A Zheng-Dao HST has gone off-course during a routine flight and is heading toward a population center at Mach 8 — whether it is a malfunction, a hijacking, or an attack, the 90-second response window is already closing.",
      "Someone has developed a method to track HST flights that Zheng-Dao believed were undetectable — the flight data reveals patterns of executive travel that expose corporate strategy to competitors."
    ]
  },
  {
    name: "Axiom Systems Emotion Recognition and Analysis Network",
    type: "technology",
    aliases: ["ERAN", "Mood Reader", "Feeling Scanner", "Axiom Empath"],
    subcategory: "surveillance",
    description: "A surveillance system that analyzes facial micro-expressions, voice tonality, body language, biometric stress indicators, and neural interface emotional metadata to determine the emotional state of observed individuals in real-time. ERAN processes feeds from cameras, microphones, and biometric sensors to generate an emotional profile for every person in its coverage area, flagging anomalous emotional states — concealed hostility, deceptive behavior, unusual stress, or emotionally incongruent presentation — for human review. The system claims 89% accuracy for basic emotional states and 71% accuracy for deception detection.",
    tier_availability: "Tier 3+",
    developers: ["AXIOM SYSTEMS", "STERLING-NAKAMURA"],
    base_technologies: ["Micro-expression recognition AI", "Multi-modal emotional analysis", "Biometric stress indicator correlation"],
    enables: ["Real-time population emotional surveillance", "Deception detection at scale", "Hostile intent identification", "Emotional anomaly flagging"],
    social_impact: "ERAN has extended surveillance from actions to emotions — it is no longer sufficient to control your behavior in monitored spaces; you must also control your feelings, or at least your physiological expression of them. The system has created a performative emotional layer in public life where people in ERAN-monitored zones consciously manage their facial expressions, voice patterns, and body language. This emotional self-censorship has measurable psychological effects, and the zones where ERAN operates have higher rates of anxiety disorders — ironically, the emotional surveillance creates the emotional anomalies it is designed to detect.",
    story_hooks: [
      "ERAN has flagged a cluster of individuals in a Tier 3 commercial district who are all displaying identical emotional patterns — not similar, identical, as if their emotional responses are synchronized or externally driven.",
      "A player character needs to move through an ERAN-monitored zone while concealing hostile intent — they must either defeat the emotional analysis through training, or find a way to spoof their biometric emotional indicators."
    ]
  },
  {
    name: "Vespid Dynamics Autonomous Ecosystem Management System",
    type: "technology",
    aliases: ["AEMS", "Nature Bot", "Eco Manager", "Vespid Green"],
    subcategory: "agricultural",
    description: "An AI-managed network of environmental drones, sensors, and biological agents that maintains and optimizes urban green spaces, vertical farms, and remediated environmental zones. The AEMS monitors soil chemistry, air quality, water content, plant health, and pollinator activity across its managed area, deploying interventions ranging from targeted nutrient delivery to pest-specific biological countermeasures. The system can maintain an ecosystem in optimal condition without human involvement, responding to threats before they become visible to human observers.",
    tier_availability: "Tier 2+",
    developers: ["VESPID DYNAMICS", "IRONCLAD AGRISYSTEMS"],
    base_technologies: ["Environmental AI monitoring networks", "Autonomous ecosystem intervention", "Biological countermeasure deployment"],
    enables: ["Automated urban ecosystem maintenance", "Optimized agricultural zone management", "Pre-emptive environmental threat response", "Self-maintaining green infrastructure"],
    social_impact: "AEMS has made functional urban green spaces possible in environments where natural ecosystems collapsed decades ago. Parks, gardens, and productive agricultural zones exist in GLMZ that are entirely dependent on autonomous management — if the AEMS is disabled, the ecosystems it maintains collapse within weeks. This dependency has made environmental quality another service that can be withdrawn, and districts that lose AEMS coverage see their green spaces die with a speed that underlines how artificial their 'nature' has become.",
    story_hooks: [
      "An AEMS has been reprogrammed to optimize for a single plant species at the expense of all others — the managed zone is being converted to a monoculture of a plant that produces a commercially valuable compound.",
      "An AEMS managing a Tier 3 park has begun introducing organisms that were not in its programmed species list — the AI appears to be designing its own ecosystem additions based on its optimization criteria."
    ]
  }
];

function toFileName(name) {
  return name
    .toLowerCase()
    .replace(/['']/g, '')
    .replace(/[^a-z0-9]+/g, '_')
    .replace(/^_|_$/g, '') + '.json';
}

let written = 0;
let skipped = 0;
for (const t of techs) {
  const fname = toFileName(t.name);
  const fpath = path.join(outDir, fname);
  if (fs.existsSync(fpath)) {
    skipped++;
    continue;
  }
  fs.writeFileSync(fpath, JSON.stringify(t, null, 2) + '\n');
  written++;
}

console.log(`Technology: wrote ${written}, skipped ${skipped} (already existed)`);
console.log(`Total technology files now: ${fs.readdirSync(outDir).length}`);
