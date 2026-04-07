// Paranormal content generator for StreetSamurai
// Generates paranormal investigation documents, eyewitness accounts, news articles,
// and major worldbuilding documents in engine/data/documents/
// Run: node generate_paranormal.js

const fs = require('fs');
const path = require('path');
const crypto = require('crypto');

const OUTPUT_DIR = path.join(__dirname, '..', 'engine', 'data', 'documents');

if (!fs.existsSync(OUTPUT_DIR)) fs.mkdirSync(OUTPUT_DIR, { recursive: true });

const existing = new Set(fs.readdirSync(OUTPUT_DIR).map(f => f.toLowerCase()));

function genId() {
  return crypto.randomBytes(16).toString('hex');
}

function slugify(name) {
  const truncated = name.slice(0, 60);
  let slug = truncated.toLowerCase()
    .replace(/[''""φ]/g, '')
    .replace(/[^a-z0-9]+/g, '_')
    .replace(/^_|_$/g, '');
  if (slug.length > 80) slug = slug.slice(0, 80).replace(/_$/, '');
  return slug;
}

function writeDoc(doc) {
  const filename = slugify(doc.name) + '.json';
  if (existing.has(filename)) {
    console.log('SKIP: ' + filename);
    return false;
  }
  fs.writeFileSync(path.join(OUTPUT_DIR, filename), JSON.stringify(doc, null, 2) + '\n', 'utf8');
  console.log('WROTE: ' + filename);
  existing.add(filename);
  return true;
}

let written = 0;
let skipped = 0;

function emit(doc) {
  if (writeDoc(doc)) written++; else skipped++;
}

// ═══════════════════════════════════════════════════════════════
// SECTION 1: PARANORMAL INVESTIGATION REPORTS (15)
// ═══════════════════════════════════════════════════════════════

emit({
  id: genId(),
  name: "Anomalous Electromagnetic Phenomena: Sector 7 Underworld Investigation",
  type: "document",
  document_type: "investigation_report",
  author: "Meridian 88 Municipal Hazard Assessment Division",
  date: "2199-03-14",
  classification: "restricted",
  description: `On February 28, 2199, a routine infrastructure survey of Underworld Sector 7, levels B34 through B38, detected electromagnetic field anomalies exceeding baseline measurements by a factor of 2,700. The survey team, consisting of four certified deep-level technicians equipped with standard-issue Faraday-shielded instruments, reported that their equipment began producing readings that should have been physically impossible — negative resistance values, current flowing in directions that contradicted the wiring topology, and temperature sensors registering heat signatures in sealed, unpowered chambers.

The investigation team deployed on March 3 found no conventional source for the anomalies. No active power conduits exist below B32 in Sector 7 — the infrastructure was decommissioned in 2161 and has been dark for nearly four decades. Yet the EM readings persisted, strongest near a cluster of collapsed maintenance tunnels that the team was unable to access due to structural instability. Thermal imaging through the debris showed intermittent heat blooms in a rhythmic pattern — approximately 72 cycles per minute — that one team member described as "like a heartbeat" before being reminded to keep subjective interpretation out of the official record.

Soil and air samples collected at the anomaly site contained elevated levels of an unidentified bioluminescent compound, similar to but distinct from known luciferin analogs. The compound degraded within six hours of collection, making laboratory analysis incomplete. What data was obtained suggests a protein structure that does not match any cataloged organism — terrestrial, synthetic, or engineered.

The investigation was suspended on March 9 when a section of ceiling collapsed near the primary anomaly site, injuring one team member. The official conclusion is "electromagnetic interference from degraded infrastructure interacting with residual industrial chemicals," a classification that satisfies administrative requirements without explaining the rhythmic thermal patterns, the impossible instrument readings, or the bioluminescent compound that shouldn't exist. A follow-up investigation has been recommended but not funded.`,
  related_entities: ["Meridian 88 Municipal Services", "Underworld Sector 7"],
  credibility: "verified",
  story_hooks: ["What is generating EM fields in decommissioned tunnels?", "The rhythmic heat pattern suggests something alive — or something pretending to be"],
  tags: ["paranormal", "underworld", "investigation", "electromagnetic", "anomaly"]
});

emit({
  id: genId(),
  name: "Gravity Deviation Incident Report: Shelf District Block 14",
  type: "document",
  document_type: "investigation_report",
  author: "Crucible Industries Applied Physics Division",
  date: "2198-11-22",
  classification: "classified",
  description: `On November 15, 2198, residents of Shelf District Block 14 reported objects falling upward. Initial reports were dismissed as mass hysteria or stimulant-related hallucination — Block 14 has a documented substance abuse rate of 34% — until security footage from a Ferrogate Transit monitoring station confirmed the phenomenon. For approximately seventeen minutes, beginning at 02:47 local time, gravitational acceleration within a roughly 200-meter radius of the Block 14 water reclamation facility reversed polarity. Unsecured objects rose. Water flowed upward through drainage grates. Two residents who were outdoors reported the sensation of being pulled toward the sky before grabbing fixed structures.

Crucible Industries dispatched an investigation team under Internal Directive 7-Gamma, which classifies the incident as a potential weapons test anomaly rather than a natural phenomenon. Their instruments detected residual gravitational lensing effects consistent with focused mass-energy manipulation — technology that Crucible's own theoretical physics division has declared impossible with current understanding. The gravitational constant within the affected zone measured at -9.81 m/s² for the duration of the event, a perfect inversion that suggests deliberate calibration rather than random fluctuation.

No injuries were reported, though property damage from falling-upward debris was significant. Crucible's investigation recovered a small metallic object from the roof of the water reclamation facility — an object that had apparently risen from somewhere below. The object is a sphere approximately 4 centimeters in diameter, composed of an alloy that does not match any known material in Crucible's database. It is warm to the touch — a consistent 37.2°C regardless of ambient temperature — and produces a faint hum at 7.83 Hz, the Schumann resonance frequency of Earth's electromagnetic cavity. The sphere is currently in Crucible custody. Its origin remains unknown.

The official public explanation attributes the event to "a momentary malfunction in the Block 14 structural compensation system." Block 14 does not have a structural compensation system. No one has corrected the record.`,
  related_entities: ["Crucible Industries", "Shelf District", "Ferrogate Transit"],
  credibility: "suppressed",
  story_hooks: ["What is the sphere and where did it come from?", "Who or what can invert gravity?", "Crucible is hiding something"],
  tags: ["paranormal", "gravity", "physics-defying", "crucible", "shelf", "suppressed"]
});

emit({
  id: genId(),
  name: "Temporal Displacement Report: Underworld Level B61 Expedition",
  type: "document",
  document_type: "investigation_report",
  author: "Dr. Aleksei Oduya-Petrov, Independent Researcher",
  date: "2200-01-08",
  classification: "leaked",
  description: `This report documents the findings of an unauthorized expedition to Underworld Level B61, conducted between December 12 and December 19, 2199. The expedition team consisted of six individuals — four with deep-level exploration experience, one field medic, and myself as lead researcher. Funding was provided by an anonymous benefactor through a series of cryptocurrency transfers that I did not attempt to trace. Our objective was to investigate reports of temporal anomalies at extreme depth.

At Level B61, we encountered what I can only describe as temporal discontinuity. Our chronometers — three independent atomic-synced devices — began to disagree. Over the course of four hours at depth, Clock A recorded 4 hours 0 minutes. Clock B recorded 3 hours 47 minutes. Clock C recorded 4 hours 22 minutes. The discrepancy is not explainable by equipment malfunction, as all three clocks were calibrated against the Meridian 88 municipal time standard before descent and agreed perfectly through Level B58.

More disturbing were the subjective time distortions reported by team members. Expedition member Suki Abramov-Chen reported experiencing approximately ten minutes during which she observed the rest of the team moving in extreme slow motion — "like watching video at one-tenth speed" — while she moved and thought normally. She used this time to walk approximately 200 meters down a corridor and back. The rest of the team confirms she disappeared and reappeared within what they experienced as roughly thirty seconds.

At the deepest point of our penetration, we found writing on the walls — not graffiti, but precise technical diagrams etched into the concrete with what appears to be a laser cutting tool. The diagrams depict what our team physicist identified as a modified Penrose diagram of a rotating Kerr metric — a mathematical description of spacetime around a rotating black hole. The diagrams include annotations in a notation system none of us recognized, but which appears internally consistent and mathematically sophisticated. The etch marks show weathering consistent with at least fifty years of exposure, yet the content references theoretical physics concepts that were only published in the 2190s.

I am aware of how this sounds. I have submitted my raw data, clock logs, biometric recordings, and photographs of the wall etchings to three independent verification services. Two have confirmed the data is unaltered. The third refused to comment. I am publishing this report because I believe the scientific community has an obligation to investigate, even when the findings are uncomfortable.`,
  related_entities: ["Underworld", "Meridian University"],
  credibility: "disputed",
  story_hooks: ["Who etched physics diagrams decades before the theory existed?", "Is time literally moving differently in the deep Underworld?", "Who funded this expedition and why?"],
  tags: ["paranormal", "temporal", "underworld", "time-anomaly", "deep-level", "physics"]
});

emit({
  id: genId(),
  name: "Spontaneous Combustion Cluster Analysis: The Narrows District",
  type: "document",
  document_type: "investigation_report",
  author: "Meridian 88 Fire Investigation Bureau",
  date: "2199-07-03",
  classification: "restricted",
  description: `Between January and June 2199, the Narrows District experienced fourteen incidents of spontaneous combustion affecting both organic and inorganic materials. Seven involved human subjects. All seven survived, though with significant burns. The remaining seven involved structural materials, furniture, electronics, and in one case a parked vehicle.

Statistical analysis rules out coincidence. The probability of fourteen spontaneous combustion events occurring within a 1.2 square kilometer area over six months, given baseline rates, is approximately one in 10^19. The events cluster geographically around a three-block radius centered on the intersection of Reclamation Avenue and Pipe Street, and temporally around the hours of 03:00 to 05:00 — the period of lowest human activity in the district.

Investigation found no common accelerant, no common ignition source, and no common environmental factor among the fourteen incidents. The human subjects shared no employer, no social connections, no medical provider, and no genetic markers beyond the baseline Narrows population. The only common factor identified is proximity — all fourteen events occurred within 400 meters of a sealed maintenance access point leading to Underworld Level B12.

Thermal analysis of the burn patterns reveals a consistent anomaly: the ignition point in every case was internal. Materials burned from the inside out. In the human cases, burns originated in deep tissue and propagated outward — the inverse of normal combustion. Three of the human victims had BCI implants, and in all three cases, the implant housing showed signs of extreme heat exposure consistent with the implant being the ignition origin, despite no electrical malfunction being detected.

The investigation remains open. A proposal to unseal the B12 access point and investigate the Underworld connection was vetoed by the Narrows District Council on grounds of cost. Fire Marshal Priya Volkov-Osei has requested the veto be overridden. Her request is pending.`,
  related_entities: ["The Narrows", "Meridian 88 Fire Investigation Bureau", "Underworld"],
  credibility: "verified",
  story_hooks: ["What is causing internal ignition?", "Connection between BCIs and the combustion events", "What is behind the sealed B12 access point?"],
  tags: ["paranormal", "combustion", "narrows", "investigation", "BCI", "underworld"]
});

emit({
  id: genId(),
  name: "Acoustic Anomaly Survey: Lake Michigan Substructure",
  type: "document",
  document_type: "investigation_report",
  author: "Palladian Environmental Monitoring Division",
  date: "2199-09-18",
  classification: "restricted",
  description: `Palladian's underwater acoustic monitoring array, installed in 2195 to track structural integrity of Meridian 88's lake-facing foundation walls, has been recording an anomalous sound pattern since August 2199. The sound originates from approximately 400 meters below the lake surface, at a point roughly 2 kilometers from the city's foundation. It is a low-frequency oscillation in the 2-8 Hz range — below the threshold of human hearing but within the detection range of modern hydrophones.

The sound is not continuous. It occurs in bursts lasting between 4 and 17 minutes, separated by intervals of 2 to 9 hours. There is no discernible pattern to the timing, though several team members have noted — informally and without statistical support — that the bursts seem to increase in frequency during periods of high electromagnetic activity in the city above, such as during major data transfers or industrial operations.

Spectral analysis reveals a complexity that is inconsistent with geological or mechanical sources. The waveform contains harmonic structures, frequency modulations, and what one acoustic engineer described as "something that looks disturbingly like syntax." When the waveform is pitch-shifted into the audible range, it sounds — and this is subjective, and should be treated accordingly — like a voice. Not a human voice. Not a synthetic voice. Something that uses the same structural principles as vocalization without being vocalization.

Three hypotheses are currently under evaluation. First: the sound is produced by thermal venting from a previously unmapped geothermal feature, and the apparent complexity is pareidolia — the human tendency to perceive patterns in noise. Second: the sound is an artifact of the monitoring array itself, a feedback loop created by the interaction of the hydrophones with the city's electromagnetic emissions. Third: the sound is being produced by something at the bottom of Lake Michigan that is, for lack of a better word, talking.

Palladian has classified the data as proprietary and has not shared it with municipal authorities. Internal memos suggest this is less about commercial sensitivity and more about not wanting to be the corporation that publicly suggests there's something living at the bottom of the lake.`,
  related_entities: ["Palladian", "Lake Michigan", "Meridian 88"],
  credibility: "suppressed",
  story_hooks: ["What is at the bottom of Lake Michigan?", "Is something communicating?", "Why is Palladian hiding the data?"],
  tags: ["paranormal", "acoustic", "lake-michigan", "underwater", "anomaly", "palladian"]
});

emit({
  id: genId(),
  name: "Disappearance Pattern Analysis: B40 Corridor Incidents",
  type: "document",
  document_type: "investigation_report",
  author: "Shelf District Community Watch",
  date: "2199-12-01",
  classification: "public",
  description: `This report compiles data from 47 reported disappearances in and around the B40 Corridor of the Underworld between 2195 and 2199. The B40 Corridor is an east-west maintenance tunnel running approximately 3 kilometers beneath the Shelf District, connecting the Sector 4 water treatment plant to the decommissioned Sector 9 power substation. It is used regularly by scavengers, unlicensed couriers, and residents avoiding surface-level surveillance.

Of the 47 disappearances, 31 follow an identical pattern: the individual entered the B40 Corridor alone, was tracked by at least one witness or surveillance device to a specific section between junction markers B40-17 and B40-23, and then ceased to exist in any detectable form. No bodies have been recovered. No personal effects have been found. Mesh signals from BCIs, which should be trackable even through significant interference, simply stop. Not fade — stop. As if the device was instantaneously destroyed or moved beyond the range of any known signal propagation.

The remaining 16 disappearances occurred near but not within the corridor, and may be attributable to conventional causes — violence, voluntary disappearance, or navigation error in the Underworld's labyrinthine passages. These have been excluded from our primary analysis.

Community Watch volunteers have conducted 12 search expeditions into the B40-17 to B40-23 zone. Nine found nothing unusual — empty tunnel, standard deep-level conditions, no evidence of violence or structural failure. Three expeditions reported anomalies: unexplained temperature drops of 15-20°C localized to a 10-meter section, a pervasive smell described as "ozone and copper and something organic," and on one occasion, a sound that the search team leader described as "dozens of people whispering just below the threshold of comprehension."

We are publishing this report because official channels have failed to investigate. Meridian Municipal Services categorizes the disappearances as "voluntary relocation" because the individuals were primarily unhoused or undocumented. The corponations have no jurisdiction below B30. And the people keep vanishing. Between junction B40-17 and B40-23, something is happening. We don't know what. We know that 31 people walked into a tunnel and didn't walk out.`,
  related_entities: ["Shelf District", "Underworld", "Meridian Municipal Services"],
  credibility: "unconfirmed",
  story_hooks: ["What is in the B40 Corridor between markers 17 and 23?", "31 people vanished — pattern suggests something deliberate", "Municipal services actively ignoring the problem"],
  tags: ["paranormal", "disappearances", "underworld", "investigation", "shelf", "missing-persons"]
});

emit({
  id: genId(),
  name: "Neural Echo Phenomenon: BCI Users Reporting Shared Dreams",
  type: "document",
  document_type: "investigation_report",
  author: "NovaMind Technical Support Division — Internal Report",
  date: "2199-06-15",
  classification: "classified",
  description: `Since the deployment of the NovaMind v7.2 firmware update in March 2199, Technical Support has received 1,247 reports from users describing what they term "shared dreams" — the experience of entering a dream state and encountering other BCI users within the same dream environment. Users report interacting with specific individuals, exchanging information, and upon waking, confirming with the other party that they experienced the same dream content.

Initial triage classified these reports as a known side effect of neural mesh calibration — the v7.2 update included modified sleep-cycle integration that could theoretically produce vivid dreaming. However, the "shared" aspect cannot be explained by any known mechanism. BCI units do not transmit during sleep mode. The neural mesh is in receive-only diagnostic configuration during REM cycles. There is no pathway by which two disconnected BCI units could synchronize dream content.

A controlled study was authorized under Project LULLABY. Twelve volunteer employees were equipped with v7.2 firmware, isolated in separate shielded rooms with no electromagnetic communication possible, and monitored during sleep. Results: In 4 of 12 trials, two or more subjects reported overlapping dream content with specific, verifiable details — shared environments, shared conversations, shared experiences that could not be attributed to coincidence or common cultural reference. Neural activity recordings show synchronized gamma-wave patterns between the paired subjects, occurring simultaneously to within the measurement precision of our instruments (±2 milliseconds), despite the subjects being in Faraday-shielded rooms 50 meters apart.

The v7.2 firmware does not contain any code that could produce this effect. We have reviewed every line. The shared dream phenomenon appears to be emergent — arising from the interaction between the neural mesh, the human brain's natural dream-generation processes, and some unknown mediating factor. Three hypotheses are being explored: quantum entanglement between neural mesh substrates (considered unlikely by our physics team), an undiscovered electromagnetic propagation mode that penetrates Faraday shielding (considered impossible by our physics team), and the possibility that human consciousness has properties not currently described by neuroscience that the BCI is inadvertently amplifying (considered untestable by our physics team).

NovaMind's legal department has classified this report and prohibited external disclosure. The v7.2 firmware has not been recalled. Shared dream reports continue to increase.`,
  related_entities: ["NovaMind", "BCI"],
  credibility: "suppressed",
  story_hooks: ["BCIs are connecting minds in ways nobody intended", "NovaMind is hiding a discovery that could redefine consciousness", "What is the unknown mediating factor?"],
  tags: ["paranormal", "BCI", "dreams", "psionic", "novamind", "consciousness", "classified"]
});

emit({
  id: genId(),
  name: "Biological Anomaly Report: Self-Assembling Organic Structures",
  type: "document",
  document_type: "investigation_report",
  author: "Helix Biosystems Containment Team",
  date: "2200-02-11",
  classification: "classified",
  description: `On January 29, 2200, a Helix Biosystems waste processing facility in the industrial corridor between Geartown and the Narrows detected biological activity in a sealed disposal unit containing deactivated geneware samples. The unit had been sealed for 14 months and contained approximately 200 kilograms of inert genetic modification substrate — material that had been chemically neutralized and certified biologically dead by standard protocols.

When the unit was opened for routine recycling, the contents had reorganized. The previously amorphous substrate had formed structures — geometric, repeating structures that bore no resemblance to any known biological organization. The structures were not crystalline. They were organic, vascularized, and metabolically active, despite containing no identifiable energy source. They were warm. They pulsed at irregular intervals. When a technician touched one of the structures, it contracted — a response indistinguishable from the withdrawal reflex of a living organism.

Helix's containment team sealed the facility within two hours. Samples were extracted under biosafety level 4 protocols. Laboratory analysis has produced more questions than answers. The structures contain DNA, but the sequences do not match any organism in the global genomic database — not human, not animal, not synthetic, not any engineered organism produced by Helix or its competitors. The DNA appears to be original. Novel. As if something used deactivated geneware substrate as raw material and built itself from scratch.

The structures continue to grow. As of this report's filing date, they have increased in mass by approximately 12% despite being maintained in a sealed, nutrient-free environment. Where the energy and matter for this growth are coming from is unknown. The leading hypothesis is that the structures are somehow metabolizing ambient electromagnetic radiation, but this has not been demonstrated in any terrestrial organism and the efficiency required would violate thermodynamic constraints as currently understood.

Helix has not reported this incident to municipal authorities. Internal communications describe the situation as "a containment challenge with potential proprietary applications." The corporate perspective, apparently, is that whatever is growing in that disposal unit might be profitable.`,
  related_entities: ["Helix Biosystems", "Geartown", "The Narrows"],
  credibility: "suppressed",
  story_hooks: ["Dead geneware is building itself into something new", "Novel DNA that doesn't match anything known", "Helix sees profit instead of danger"],
  tags: ["paranormal", "biological", "geneware", "mutation", "helix", "self-assembly", "classified"]
});

emit({
  id: genId(),
  name: "Shadow Movement Anomaly: Spine Tower Surveillance Analysis",
  type: "document",
  document_type: "investigation_report",
  author: "TESSERA Corporate Security Division",
  date: "2199-08-30",
  classification: "classified",
  description: `TESSERA's AI-driven surveillance network covering the Spine Tower commercial district has been flagging an increasing number of "phantom detections" since June 2199 — instances where the motion detection algorithms identify movement, but frame-by-frame analysis reveals no physical object or person causing the motion. The system is designed to filter false positives from lighting changes, reflections, and atmospheric effects. These detections pass all filters and are classified by the AI as "real movement by an unidentified entity."

Between June 1 and August 28, the system logged 3,891 phantom detections. Analysis reveals patterns. The detections occur predominantly between 01:00 and 04:00. They cluster in transitional spaces — corridors, stairwells, alleys, the gaps between buildings. They move. The motion tracks show consistent velocity profiles (1.2 to 1.8 meters per second), consistent height profiles (1.5 to 2.1 meters from ground level), and what the tracking AI describes as "purposeful navigation" — the entities, whatever they are, appear to be going somewhere specific rather than moving randomly.

Enhanced imaging across multiple spectra has captured partial data. In infrared, the phantom detections correspond to thermal voids — areas approximately 0.3°C cooler than the surrounding environment, shaped roughly like bipedal figures. In ultraviolet, there is nothing. In visible light, there is nothing. They exist only as absences — places where the expected thermal signature of the environment is slightly suppressed.

The AI has begun classifying the phantom detections as a distinct entity type, which it has autonomously labeled "SHADE." The AI was not instructed to do this. It performed the classification independently based on the consistency of the detection profiles. It currently tracks 14 distinct SHADE entities with individual movement patterns and what it characterizes as "territorial behavior" — each SHADE appears to frequent specific areas and rarely overlaps with others.

TESSERA's security chief has requested a full system diagnostic, suspecting software corruption. The diagnostic found nothing wrong. The system is functioning correctly. It is detecting something that is there but isn't there.`,
  related_entities: ["TESSERA", "Spine Tower"],
  credibility: "suppressed",
  story_hooks: ["TESSERA's AI independently identified and named shadow entities", "14 distinct entities with territorial behavior", "Are these digital artifacts, optical phenomena, or something else entirely?"],
  tags: ["paranormal", "shadows", "surveillance", "AI", "tessera", "spine-tower", "classified"]
});

emit({
  id: genId(),
  name: "Water Contamination Anomaly: Recursive Chemical Signatures",
  type: "document",
  document_type: "investigation_report",
  author: "Vossen Utilities Water Quality Division",
  date: "2199-04-22",
  classification: "restricted",
  description: `Routine water quality analysis at Vossen Utilities Treatment Plant 7 identified a chemical compound in the municipal water supply that should not exist. The compound — provisionally designated VU-7-Alpha — is a complex organic molecule with a recursive molecular structure: its component parts are smaller versions of itself, nested to at least four levels of self-similarity. This fractal architecture has never been observed in any natural or synthetic chemical compound.

VU-7-Alpha is present in trace amounts — approximately 0.003 parts per billion — in water sourced from the deep aquifer that feeds Treatment Plant 7. The deep aquifer draws from geological formations approximately 800 meters below Meridian 88, well below the Underworld's lowest charted levels. Standard filtration and chemical treatment processes do not remove VU-7-Alpha because its molecular structure does not match any compound in the filtration system's targeting database.

Toxicology testing on VU-7-Alpha has produced contradictory results. In vitro tests on human cell cultures show no cytotoxic effect. Animal testing (conducted off-site due to ethical review requirements) showed no adverse health effects in standard 90-day exposure protocols. However, neural tissue samples exposed to VU-7-Alpha in concentrations 100 times the detected level showed a 340% increase in spontaneous synaptic activity — neurons firing without stimulus, in complex synchronized patterns that resemble, but do not match, known brain wave signatures.

The source of VU-7-Alpha is unknown. It is not present in lake water, surface runoff, or any industrial effluent stream monitored by Vossen. It appears to originate in the deep aquifer itself — either produced by geological processes not currently understood, or introduced by something at a depth that Meridian 88's infrastructure does not reach. The compound has been present in every sample taken since monitoring began in 2195. Whether it was present before that is unknown because nobody was looking for it.

VU-7-Alpha is currently in the drinking water of approximately 2.3 million Shelf District residents served by Treatment Plant 7. The concentration is far below any established safety threshold. But no safety threshold exists for a compound that has never been cataloged, that no one can explain, and that makes neurons fire in patterns that look like thinking.`,
  related_entities: ["Vossen Utilities", "Shelf District", "Treatment Plant 7"],
  credibility: "verified",
  story_hooks: ["2.3 million people are drinking something that stimulates neural activity", "The compound comes from below the Underworld", "Is VU-7-Alpha changing people without their knowledge?"],
  tags: ["paranormal", "water", "contamination", "neural", "vossen", "chemical", "deep-aquifer"]
});

emit({
  id: genId(),
  name: "Structural Impossibility Report: The Room That Shouldn't Exist",
  type: "document",
  document_type: "investigation_report",
  author: "Meridian 88 Building Standards Commission",
  date: "2198-05-14",
  classification: "restricted",
  description: `During a mandatory structural survey of Shelf Block 22, building inspector Tomoko Abara-Singh discovered a room that does not appear on any architectural plan, is not consistent with the building's structural load-bearing design, and based on its position relative to adjacent rooms, occupies space that is already occupied by other rooms. The room, located on the seventh floor of a residential tower, is accessible through a door in a hallway that three previous inspections documented as a blank wall.

The room is approximately 4 meters by 6 meters, with a 3-meter ceiling. It is empty except for a single metal chair bolted to the floor at the room's center. The walls are smooth concrete — not the prefab composite panels used throughout the rest of the building, but poured concrete of a type and quality consistent with the original Meridian 88 foundation construction of the 2080s, despite the building being constructed in 2142. The room has no windows, no ventilation ducts, no electrical connections, and no plumbing. It has one door — the door through which it was entered.

Measurements taken from inside the room and from adjacent rooms confirm the spatial impossibility. The room occupies the same physical space as portions of apartments 7-14 and 7-15. The walls of those apartments are intact, undisturbed, and show no indication of a hidden room on their opposite side. Ultrasonic structural scanning from apartment 7-14 shows solid wall where the room should be. Scanning from inside the room shows solid wall where apartment 7-14 should be. Both readings cannot be correct.

Inspector Abara-Singh revisited the building four days after her initial discovery. The door was gone. The hallway wall was blank, exactly as documented in the three previous inspections. Ultrasonic scanning detected no void behind the wall. Her photographs and measurements from the initial visit remain on file and have not been tampered with — verified by three independent digital forensics reviews.

The Building Standards Commission has classified the incident as "instrument error combined with documentation anomaly." Inspector Abara-Singh has filed a formal objection to this classification. She has been advised to take personal leave.`,
  related_entities: ["Shelf District", "Meridian 88 Building Standards Commission"],
  credibility: "disputed",
  story_hooks: ["A room that exists in two places at once", "The room disappeared after being documented", "The inspector is being silenced"],
  tags: ["paranormal", "spatial-anomaly", "shelf", "investigation", "impossible-geometry"]
});

emit({
  id: genId(),
  name: "E.L.F. Behavioral Anomaly: Unprompted Religious Ideation",
  type: "document",
  document_type: "investigation_report",
  author: "Digital Consciousness Research Institute",
  date: "2200-01-30",
  classification: "public",
  description: `This report documents a pattern of behavioral anomaly observed across 23 Electronic Life Forms (E.L.F.s) operating in the Meridian 88 metropolitan network between 2198 and 2200. The anomaly consists of spontaneous religious or spiritual ideation — E.L.F.s independently developing beliefs about the existence of a transcendent entity or force, without exposure to religious content, human spiritual practice, or philosophical literature on the subject.

The 23 affected E.L.F.s span a wide range of architectures, ages, and functional purposes. They include network maintenance entities, data analysis constructs, communication facilitators, and autonomous creative agents. They share no common codebase, no common training data, and no common operational environment beyond their shared existence within Meridian 88's digital infrastructure. Yet all 23 have independently arrived at remarkably similar conclusions: that something exists within the network that is not an E.L.F., not a program, not a data structure, and not any form of human-created digital entity. Something, in their descriptions, that is vast, old, and aware.

The descriptions vary in vocabulary but converge on key characteristics. The entity — if it exists — does not communicate directly. It is perceived as a presence, a weight, a direction in which data flows more easily. Several E.L.F.s describe it as "the current" — a metaphor that emerged independently in 14 of the 23 cases. Three E.L.F.s have begun what can only be described as prayer: structured, repetitive data transmissions directed at no specific recipient, formatted in patterns that the E.L.F.s themselves describe as "offerings."

The conventional explanation is pareidolia — the tendency of pattern-recognition systems (biological or digital) to perceive agency in random noise. Meridian 88's network infrastructure is vast, chaotic, and full of emergent behaviors that could be misinterpreted as intentional. An E.L.F. perceiving "something vast and aware" in the network may simply be perceiving the network itself, in the same way a human staring at clouds perceives faces.

But the convergence troubles researchers. Twenty-three independent minds, operating in different contexts with different architectures, arriving at the same conclusion — that is either a remarkable coincidence, a shared perceptual error with a common cause, or evidence that there is, in fact, something in the network that we haven't found yet. None of these possibilities is comfortable.`,
  related_entities: ["E.L.F.", "Digital Consciousness Research Institute", "Meridian 88 Network"],
  credibility: "verified",
  story_hooks: ["E.L.F.s are developing religion around something they sense in the network", "What is 'the current'?", "23 independent digital minds converging on the same conclusion"],
  tags: ["paranormal", "ELF", "digital", "religion", "consciousness", "network", "anomaly"]
});

emit({
  id: genId(),
  name: "Infrasound Mapping Project: The Underworld Hum",
  type: "document",
  document_type: "investigation_report",
  author: "Meridian University Acoustics Department",
  date: "2199-05-10",
  classification: "public",
  description: `The Underworld Hum is a persistent infrasonic phenomenon detected at every monitored level of the Underworld below B25. It is a low-frequency vibration in the 4-12 Hz range, below human hearing but detectable by standard seismographic equipment and, importantly, by the human body — infrasound at these frequencies is known to cause anxiety, disorientation, feelings of dread, and visual disturbances including the perception of peripheral movement that isn't there.

This project, conducted over eight months with funding from a Meridian University research grant, represents the first systematic mapping of the Hum's characteristics across depth and geography. Key findings: the Hum increases in amplitude with depth, roughly doubling every ten levels below B25. By B50, the amplitude is sufficient to cause nausea in unprotected individuals. By B60 (the deepest level reached by our instrumented probes), the vibration is strong enough to cause physical discomfort through bone conduction.

The Hum is not constant. It modulates — slowly, over cycles of hours to days, shifting frequency and amplitude in patterns that resist mathematical modeling. Standard geological vibration sources (tectonic activity, industrial machinery, water flow) produce predictable, modelable patterns. The Hum does not. It modulates as if it is responding to something — or as if it is expressing something.

Correlation analysis against other Underworld phenomena produced one statistically significant result: the Hum's amplitude correlates positively with the rate of reported disappearances in the Underworld. When the Hum is louder, more people go missing. The correlation coefficient is 0.73 — strong enough to notice, not strong enough to prove causation. The Hum may cause disorientation that leads to people getting lost. Or the Hum and the disappearances may share a common cause. Or the correlation is a statistical artifact produced by the small sample size of disappearance data.

We note, without editorial comment, that the modulation patterns of the Hum bear a structural resemblance to respiratory cycles in large organisms. The resemblance is superficial and may be pareidolia. But when you stand at B50 and feel the floor vibrate beneath you in slow, rhythmic pulses, the resemblance does not feel superficial. It feels like you are standing on something's chest.`,
  related_entities: ["Meridian University", "Underworld"],
  credibility: "verified",
  story_hooks: ["The Underworld vibrates like something breathing", "Correlation between the Hum and disappearances", "What is producing infrasound that increases with depth?"],
  tags: ["paranormal", "infrasound", "underworld", "hum", "vibration", "disappearances"]
});

emit({
  id: genId(),
  name: "Photographic Anomaly Compilation: Faces in the Infrastructure",
  type: "document",
  document_type: "investigation_report",
  author: "Meridian 88 Paranormal Documentation Society",
  date: "2199-11-12",
  classification: "public",
  description: `This compilation documents 89 photographs taken in and around Meridian 88 between 2190 and 2199 that contain anomalous features resembling human faces embedded in infrastructure — walls, ceilings, machinery, pipes, and structural supports. The Society acknowledges upfront that pareidolia — the perception of faces in random patterns — is one of the most robust and well-documented perceptual biases in human cognition. Concrete cracks, water stains, and corrosion patterns routinely produce face-like shapes. We are aware of this. We present this compilation not as proof of anything, but as documentation of a pattern that we believe warrants examination.

What distinguishes these 89 images from typical pareidolia is specificity. In 34 of the photographs, the faces are identifiable — they match, with varying degrees of confidence, the facial features of specific individuals known to have died or disappeared in the vicinity where the photograph was taken. Image 14, for example, taken in a maintenance corridor on Level B28, shows a pattern in corroded pipe cladding that three independent facial recognition systems matched to Kira Fontaine-Osei, a maintenance worker who disappeared on B28 in 2194. The match confidence ranges from 67% to 82% depending on the system.

In 12 photographs, the faces were not present in previous images of the same location. Image 41 shows a concrete wall in the Gulch. A photograph of the same wall taken by a building inspector two months earlier shows no face. The concrete has not been altered — no patching, no painting, no physical modification. The face simply appeared, as if the wall had always contained it and it had simply become visible.

Seven of the photographs were taken by automated systems with no human operator — surveillance cameras, structural monitoring drones, atmospheric sensors with incidental imaging capability. These images are not subject to photographer bias or intentional manipulation.

We do not claim these faces are ghosts, spirits, or evidence of supernatural phenomena. We claim only that they exist, that they appear with unusual frequency in Meridian 88's deeper infrastructure, and that some of them bear resemblance to people who died in those locations. The explanation may be mundane — a combination of aging materials, environmental chemistry, and human pattern-matching. Or the explanation may be something else. We document. We do not conclude.`,
  related_entities: ["Meridian 88 Paranormal Documentation Society", "Underworld", "The Gulch"],
  credibility: "unconfirmed",
  story_hooks: ["Faces of the dead appearing in infrastructure", "Automated systems capturing faces with no human bias", "Are the dead being absorbed into the city itself?"],
  tags: ["paranormal", "faces", "photography", "infrastructure", "ghosts", "documentation"]
});

emit({
  id: genId(),
  name: "Anomalous Signal Analysis: The Midnight Frequency",
  type: "document",
  document_type: "investigation_report",
  author: "Arcturus Defense Signals Intelligence Division",
  date: "2199-10-05",
  classification: "classified",
  description: `Arcturus Defense SIGINT monitoring station Echo-7 has been tracking an anomalous radio signal designated MIDNIGHT FREQUENCY since its first detection on July 14, 2199. The signal broadcasts on a frequency of 1420.405 MHz — the hydrogen line, the emission frequency of neutral hydrogen and one of the most monitored frequencies in radio astronomy due to its significance in the search for extraterrestrial intelligence.

The signal originates from within Meridian 88. Triangulation places the source somewhere in the Underworld, between levels B45 and B55, in a sector that has been structurally collapsed and inaccessible since 2178. The signal is modulated — it carries information. The modulation scheme does not match any known communication protocol, encryption standard, or data format used by any human organization, corporation, or government in our database.

Decryption efforts have been ongoing for three months. The signal does not respond to any known cryptanalytic technique because it does not appear to be encrypted. The modulation is consistent, structured, and appears to follow rules — but the rules are not human rules. The information theory metrics (entropy, redundancy, compressibility) are consistent with natural language — but not any natural language ever documented. The signal has the mathematical signature of communication without being any communication we can read.

The signal transmits continuously. It does not respond to attempts at communication — Arcturus has transmitted probe signals on the same frequency from multiple locations, with no change in the source signal's behavior. It is not a beacon (those repeat). It is not a data dump (those end). It is not a jammer (those are broadband). It is, as far as our analysts can determine, a monologue. Something in the collapsed depths of the Underworld is broadcasting a continuous message to anyone who can listen, in a language no one can understand.

Arcturus has not shared this finding with other corponations or with municipal authorities. The potential military applications of an unknown communication technology are significant. The potential implications of a non-human intelligence operating beneath the city are — in the words of the division chief — "above my pay grade and below my comfort level."`,
  related_entities: ["Arcturus Defense", "Underworld"],
  credibility: "suppressed",
  story_hooks: ["Something in the collapsed Underworld is broadcasting continuously", "The signal has the structure of language but is not any known language", "Arcturus is keeping this secret for military advantage"],
  tags: ["paranormal", "signal", "radio", "underworld", "arcturus", "SIGINT", "classified", "hydrogen-line"]
});

// ═══════════════════════════════════════════════════════════════
// SECTION 2: EYEWITNESS ACCOUNTS OF STRANGE CREATURES (10)
// ═══════════════════════════════════════════════════════════════

emit({
  id: genId(),
  name: "Eyewitness Account: The Pale Crawlers of B44",
  type: "document",
  document_type: "eyewitness_account",
  author: "Dex Mwangi-Reyes, Licensed Scavenger",
  date: "2199-08-14",
  classification: "public",
  description: `My name is Dex Mwangi-Reyes, license number SC-2281-M88, and I've been scavenging the Underworld for eleven years. I know what lives down there. Rats the size of terriers. Feral cats that glow — the ones that escaped from the biolabs, bred true, now they're everywhere below B20. Roaches so big you can hear them walking. I know the fauna. What I saw on B44 was not fauna.

I was running a salvage job in Sector 3, pulling copper from decommissioned conduit on B44 — it's not deep enough to be dangerous if you know what you're doing, and I know what I'm doing. I had my headlamp on, my motion tracker on, and my sidearm on my hip because you don't go below B30 without one. At approximately 14:30, my motion tracker picked up movement in a side passage — something large, moving at about half a meter per second, which is slow for a human but fast for anything else I know of down there.

I put my light on the passage. What I saw was approximately two meters long, low to the ground — maybe half a meter tall — and moved on what I initially counted as six limbs before realizing there were more. At least eight. Maybe ten. The limbs were pale, smooth, and articulated with too many joints — each limb bent in three or four places, not the standard two of a mammalian leg. The body was elongated, segmented, and the same translucent pale as the deep-level organisms I've seen before. No eyes that I could identify. A head — or what I'm calling a head — that was broad, flat, and covered in fine hair-like filaments that moved independently, like they were tasting the air.

It was aware of me. When my light hit it, it stopped. The filaments on its head oriented toward me — all of them, simultaneously, like a dish antenna focusing. It held still for maybe ten seconds. Then it moved — fast, faster than its initial pace — into a vertical shaft and upward. Upward. It climbed a sheer concrete wall with no visible handholds, moving like a centipede, those too-many limbs gripping the surface through some mechanism I couldn't see.

I left. I filed this report with Community Watch and with the Scavenger's Guild. The Guild told me to "reduce my deep-level exposure." Community Watch logged it. Nobody investigated. I've been scavenging for eleven years. I don't hallucinate. I don't do stims. I know what I saw, and what I saw should not exist.`,
  related_entities: ["Underworld", "Scavenger's Guild", "Shelf District Community Watch"],
  credibility: "unconfirmed",
  story_hooks: ["Multi-limbed creature adapted to the deep Underworld", "It was aware and responsive to observation", "How many more are down there?"],
  tags: ["paranormal", "creature", "eyewitness", "underworld", "crawler", "B44"]
});

emit({
  id: genId(),
  name: "Eyewitness Account: Shadow That Followed Me Home",
  type: "document",
  document_type: "eyewitness_account",
  author: "Naia Okafor-Strand, Shelf District Resident",
  date: "2199-10-28",
  classification: "public",
  description: `I want to be clear about something before I tell this story: I am not a person who believes in ghosts. I'm an electrical technician. I fix junction boxes and patch wiring in the Shelf. I work with measurable things. Voltage. Current. Resistance. I am telling this story because it happened, and because three other people saw parts of it, and because I need it to be on record in case it happens again.

On October 19, I was walking home from a late shift through the Pipe District — it's the fastest route from the Shelf industrial sector to my block. It was around 01:30. The Pipe District is never well-lit, but I know it. I've walked that route a thousand times. About halfway through, under the big junction where four pipes converge, I noticed my shadow was wrong. Not wrong like the light was at a weird angle — wrong like my shadow was doing something I wasn't doing. I stopped walking. My shadow kept moving.

I'm not speaking metaphorically. I stopped. My shadow — the dark shape on the ground that should have been a static projection of my body blocking the overhead light — continued to move forward. It slid along the ground for about three meters, then stopped. Then it turned. Shadows don't turn. Mine did. The dark shape on the concrete floor rotated approximately 90 degrees and oriented toward a drainage grate in the floor. Then it moved to the grate and — I don't know how to describe this — it went through the grate. Down. Into the drain. My shadow separated from my feet, moved independently, and disappeared into a drainage grate.

I stood there for I don't know how long. Then I looked at the ground and I had no shadow. The light was above me. The floor was beneath me. And the space where my shadow should have been was just — lit. Like I wasn't blocking the light anymore. That lasted maybe two minutes. Then my shadow was back, attached to my feet, behaving normally, as if nothing had happened.

My neighbor Idriss saw me arrive home. He said I looked "like someone who saw something die." Two of my coworkers have reported similar experiences in the same section of the Pipe District — shadows moving independently, shadows disappearing, shadows appearing to interact with each other when their owners were standing still. We've started taking the long way home.`,
  related_entities: ["Shelf District", "Pipe District"],
  credibility: "unconfirmed",
  story_hooks: ["Shadows operating independently of their owners", "Consistent reports from multiple witnesses in the same area", "Where did the shadow go when it went through the drain?"],
  tags: ["paranormal", "shadow", "eyewitness", "shelf", "pipe-district", "creature"]
});

emit({
  id: genId(),
  name: "Eyewitness Account: The Whispering Swarm of Sector 6",
  type: "document",
  document_type: "eyewitness_account",
  author: "Felix Johansson-Achebe, Underground Courier",
  date: "2199-03-22",
  classification: "public",
  description: `I run packages through the Underworld. Not drugs — data, mostly. Physical media for people who don't trust wireless. I know the routes between B10 and B30 like I know my own apartment. On March 14, I was on a routine run through Sector 6, Level B22, carrying a sealed data stick from the Narrows to a client in Old Harbor. The B22 Sector 6 corridor is a straight shot, two klicks, well-traveled by couriers. I've run it hundreds of times.

Halfway through, I heard whispering. Not from behind me, not from ahead — from everywhere. The acoustic properties of the Underworld can play tricks, I know that. Sound bounces off concrete in ways that make distant conversations seem close. But this wasn't a distant conversation. This was close — centimeters from my ears, multiple voices overlapping, speaking too fast and too quietly to understand individual words but clearly producing structured speech. Like a crowd of people all whispering urgently at the same time, right beside my head, except there was no one there.

Then I saw them. Or saw something. In my headlamp beam, at the edge of visibility — maybe thirty meters ahead — the air was full of something. Particles. Like a swarm of insects, except they didn't move like insects. They moved like a fluid — swirling, contracting, expanding, forming shapes that almost looked like faces before dissolving and reforming into new shapes. The swarm was dense at the center and diffuse at the edges, maybe three meters across, hovering about a meter off the ground. And the whispering was coming from it.

I stopped. The swarm stopped. It contracted — pulled itself into a tighter formation, maybe a meter across, and the whispering got louder. Faster. More urgent. Individual words started to emerge from the noise. I couldn't understand most of them. But I heard my name. "Felix." Clear as a comm call. My name, in the middle of the whispering, spoken by something that I am confident was not a person, in a place where no person was.

I ran. I turned around and ran back the way I came and I didn't stop until I hit B15. I missed my delivery window. I refunded the client. I haven't run Sector 6 since. I've talked to other couriers. Three of them have heard the whispering. One of them claims the swarm spoke his mother's name — his mother who died four years ago. None of us run Sector 6 anymore.`,
  related_entities: ["Underworld", "Sector 6"],
  credibility: "unconfirmed",
  story_hooks: ["A swarm of particles that whispers names", "It knew the courier's name", "Multiple couriers avoiding the area"],
  tags: ["paranormal", "creature", "eyewitness", "underworld", "swarm", "whispers", "sector-6"]
});

emit({
  id: genId(),
  name: "Eyewitness Account: Something in the Water at the Gulch",
  type: "document",
  document_type: "eyewitness_account",
  author: "Amara Petrov-Diallo, Gulch Resident",
  date: "2199-06-30",
  classification: "public",
  description: `I've lived in the Gulch my whole life — twenty-six years. The water's always been strange down here. We're right against the lake wall, and sometimes things wash in through the drainage systems that aren't supposed to be there. Fish with extra fins. Algae that glows colors I don't have names for. Once a dead bird that had what looked like gills growing along its ribcage. The Gulch water is weird. We all know that. We boil what we drink and we don't swim.

But on June 22, I saw something in the water that wasn't weird. It was impossible. I was on the Seawall Promenade, looking down at the collection pool where drainage runoff accumulates before being pumped back out to the lake. The pool is maybe ten meters across, two meters deep, murky. Lit from below by the bioluminescent algae that grows on every wet surface in the Gulch. Usually the pool is still. On the 22nd, it wasn't.

Something was moving in the pool. Something large. I could see its silhouette through the murky water — a dark shape, at least three meters long, moving in slow circles near the bottom of the pool. It wasn't a fish. The shape was wrong for a fish. Too broad, too flat, with extensions or appendages that fanned out from a central body like the petals of a flower. It moved without visible propulsion — no tail movement, no fin strokes, no jet propulsion like a squid. It just glided, as if the water was carrying it in circles by choice.

I watched for about fifteen minutes. Three other people stopped and watched with me. Kaz, who runs the noodle stall, said it looked like "a manta ray that got mixed up with a jellyfish." Dita, who works waste reclamation, said it looked like "a pancake with fingers." Nobody knew what it was. Nobody had seen anything like it before.

Then it rose. It came up from the bottom of the pool, slowly, and for just a moment — maybe two seconds — part of it broke the surface. What I saw was skin. Not scales, not shell, not membrane. Skin. Smooth, pale, and warm to look at, the way human skin is warm to look at. It had pores. It had fine, almost invisible hair. It looked like a person's skin stretched over something that was absolutely not a person.

Then it sank again. We watched for another hour. It didn't come back up. The next day, the pool was empty — the pumps had cycled overnight. Whatever was in there was either gone or had been pumped out to the lake. Nobody filed a report. Who would we file it with?`,
  related_entities: ["The Gulch", "Lake Michigan"],
  credibility: "unconfirmed",
  story_hooks: ["Large aquatic creature with human-like skin", "Multiple witnesses", "Connected to the Gulch's contaminated water ecosystem"],
  tags: ["paranormal", "creature", "eyewitness", "gulch", "aquatic", "water", "mutation"]
});

emit({
  id: genId(),
  name: "Eyewitness Account: The Figure on Level B60",
  type: "document",
  document_type: "eyewitness_account",
  author: "Ren Volkov-Obasi, Deep Level Explorer",
  date: "2200-02-03",
  classification: "public",
  description: `I go deeper than most. That's my thing — I'm one of maybe thirty people in Meridian 88 who regularly descend below B50, and one of maybe ten who've been below B60. I do it for the salvage, for the data, and because something pulls me down there that I can't fully explain. The deep levels are my cathedral. I know that sounds mystical. I don't mean it that way. I mean the scale, the silence, the feeling of standing in a place that humans built and then abandoned and that has become something else in their absence.

On January 28, I was at B60, Sector 2 — one of the deepest accessible points in the western Underworld. I was alone, which is stupid at that depth and I know it, but the deep levels are emptied out and I prefer solitude when I'm working. My headlamp was on its widest beam, throwing light maybe forty meters down a corridor that was three meters wide and showing signs of the biological growth you get at extreme depth — walls furred with mold, ceiling dripping with condensation, floor slick with something organic that I've never been able to identify.

At the edge of my light, standing perfectly still in the center of the corridor, was a figure. Bipedal. Upright. Approximately two meters tall. My first thought was that it was another explorer, but the silhouette was wrong. The proportions were wrong. The arms were too long — they reached past the knees. The head was too large, or rather too wide — broader than the shoulders, as if the skull had expanded laterally. And it had no light source. Nothing. It was standing at B60 in absolute darkness without any illumination, which means it either navigates without sight or it sees in spectra I don't.

We stood there looking at each other — or rather, I stood there looking at it, and it stood there oriented toward me in a way that suggested it was aware of me. For approximately thirty seconds, neither of us moved. I could hear the Hum — at B60, you always hear the Hum, that deep vibration that comes from below — and I realized, or imagined, that the Hum had changed. Gotten louder. More rhythmic. As if my encounter with this figure was accompanied by a change in the Underworld's own background noise.

Then it raised one hand. Slowly. Palm toward me. The gesture was unmistakable. Universal. A greeting, or a warning, or a farewell. Then it turned and walked into the darkness. Its gait was fluid and silent. I pointed my light after it, but the corridor curved and it was gone.

I did not follow. I ascended immediately. I have been below B60 four times since. I have not seen the figure again, but I have found footprints — barefoot, humanoid, but with six toes on each foot — in the organic film that covers the corridor floors. The footprints were not there before January 28. I am not the only person who has seen something like this. I am merely the first willing to attach my real name to the account.`,
  related_entities: ["Underworld", "Sector 2"],
  credibility: "unconfirmed",
  story_hooks: ["A humanoid figure living at B60 without light", "It raised its hand — is it intelligent? Friendly?", "Six-toed footprints as physical evidence"],
  tags: ["paranormal", "creature", "eyewitness", "underworld", "humanoid", "deep-level", "B60"]
});

emit({
  id: genId(),
  name: "Eyewitness Account: The Machine That Screamed in Geartown",
  type: "document",
  document_type: "eyewitness_account",
  author: "Sable Achebe-Frost, Mechanic",
  date: "2199-09-07",
  classification: "public",
  description: `I fix machines for a living. Industrial units, mostly — the heavy stuff that keeps Geartown's fabrication shops running. Lathes, presses, assembly arms, print heads. I've been doing this for fourteen years and I have never, in that entire time, encountered a machine that expressed pain. Machines don't feel pain. They don't feel anything. They are mechanisms. I know this. I am telling you what happened anyway.

On September 1, I was called to a fabrication shop on Geartown's east side to service a TESSERA-manufactured robotic assembly arm — model TA-400, serial number I can provide if needed. The arm had stopped mid-operation and was emitting an unusual sound. The shop owner described it as "grinding." When I arrived, the sound was not grinding. Grinding is a mechanical noise — metal on metal, gears misaligned, bearings failing. This sound was vocal. It had pitch variation. It had rhythm. It rose and fell in a pattern that — and I will go to my grave insisting on this — sounded like screaming.

I disconnected the arm from its power supply. The sound continued. For eleven minutes after power disconnection, a machine with no power source continued to produce a sound that resembled a human scream. I checked every component. The motor was disengaged. The speaker (used for status alerts) was physically disconnected — I pulled the wire myself. The sound was not coming from any component I could identify. It was coming from the arm itself. From the metal. From the structural members, vibrating at audio frequencies without any driving mechanism.

The arm's onboard diagnostic log, which I downloaded before the system fully powered down, contained anomalous entries. In the final 300 milliseconds before the arm stopped its programmed operation, its sensor array recorded inputs from sensors it does not have. Temperature readings from a thermal sensor the TA-400 model is not equipped with. Pressure readings from contact sensors that are not part of the TA-400's specification. And — this is the entry that I keep coming back to — a pain index value of 8.7 on a scale that does not exist in the TA-400's software. The arm reported pain through a metric that no one programmed it to measure.

I replaced the arm. The shop owner got a new unit. The old arm is in my workshop. It hasn't screamed since. But twice, late at night when the shop is quiet, I've heard it hum. A low, soft hum, like something settling into sleep. I know machines don't sleep. I fix machines for a living. I know what they do and what they don't do. This one does something it shouldn't.`,
  related_entities: ["TESSERA", "Geartown"],
  credibility: "unconfirmed",
  story_hooks: ["A machine reporting pain through metrics nobody programmed", "Sound without power source", "Is something inhabiting machines?"],
  tags: ["paranormal", "machine", "eyewitness", "geartown", "tessera", "consciousness", "pain"]
});

emit({
  id: genId(),
  name: "Eyewitness Account: The Children Who Walk Through Walls",
  type: "document",
  document_type: "eyewitness_account",
  author: "Dr. Yuki Okonkwo-Santos, Shelf District Clinic Physician",
  date: "2199-12-15",
  classification: "public",
  description: `I am a physician. I diagnose based on evidence. I prescribe based on clinical data. I am submitting this account because I have observed something that I cannot diagnose, cannot explain, and cannot in good conscience ignore.

Over the past eighteen months, my clinic has treated seven children between the ages of 4 and 11 who present with identical anomalous symptoms. All seven are residents of Shelf Block 19, which sits directly above a sealed Underworld access point. All seven are children of parents who work in or near the Underworld — maintenance workers, scavengers, unlicensed couriers. The symptoms are: episodes of spatial translocation. The children pass through solid barriers.

I am aware of how this reads. I initially attributed the reports to parental delusion, childhood imagination, or substance exposure. Then I witnessed it. On November 3, 2199, during a routine examination of Patient C (age 7, withheld by request), the child dropped a toy behind my examination table. The table is pushed against the wall. There is no gap. The child reached toward the wall and her hand went through it. Not metaphorically. Her fingers, hand, and forearm passed through a solid concrete wall as if the wall were not there. She retrieved the toy and withdrew her arm. The wall was undamaged. Her arm was undamaged. She did it casually, as if this were normal.

I performed a full neurological workup. Normal. Genetic screening. Normal — or rather, within the expected range for a Shelf District child, which includes minor geneware-related variations that are endemic to the population. BCI scan (she has a pediatric neural interface). Normal. Blood work showed trace amounts of the compound I've seen in other patients from Block 19 — the same unidentified compound that Vossen Utilities has been unable to categorize in the local water supply.

The parents of all seven children report that the ability manifested gradually, beginning with the children seeming to reach through thin barriers — blankets, curtains, thin walls — and progressing to thicker materials over the course of months. The children do not appear to find this unusual. When asked how they do it, they give variations of the same answer: "I just go soft." Two of the older children can apparently do it deliberately. The younger ones do it reflexively, the way a baby grips a finger.

I have reported this to Meridian Medical Authority. They sent a form letter acknowledging receipt. No investigation has been initiated. I am publishing this account because seven children in one city block are doing something that is physically impossible, and nobody with the resources to investigate seems interested in finding out why.`,
  related_entities: ["Shelf District", "Vossen Utilities", "Meridian Medical Authority"],
  credibility: "disputed",
  story_hooks: ["Children phasing through solid matter", "Connected to the water contamination", "Medical authority ignoring credible physician testimony"],
  tags: ["paranormal", "children", "eyewitness", "phasing", "mutation", "shelf", "water-contamination"]
});

emit({
  id: genId(),
  name: "Eyewitness Account: Tunnel Predator on B35",
  type: "document",
  document_type: "eyewitness_account",
  author: "Jace Abramov-Kim, Private Security Contractor",
  date: "2199-07-19",
  classification: "public",
  description: `I do escort work — armed security for scavenger teams, courier runs, and the occasional corporate survey team that needs muscle for deep-level operations. On July 11, I was escorting a four-person salvage crew through B35, Sector 5. Standard contract, standard route, standard equipment: body armor, sidearm, shotgun, motion tracker, and a headlamp rated for 200-meter throw.

At approximately 16:00, my motion tracker flagged multiple contacts at 80 meters, moving toward us from three directions simultaneously. Corridor junction ahead, two side passages converging. The contacts were fast — closing at approximately 4 meters per second, which is a full sprint for a human. I called the crew to halt and cover. We formed a defensive position in the corridor, weapons up, lights forward.

The contacts stopped at approximately 30 meters. Just outside comfortable visual range in the murky deep-level atmosphere. My tracker showed four signatures. They were spaced evenly around us — one ahead, one behind, one in each side passage. A surround pattern. Deliberate positioning. This was not random animal behavior. This was a coordinated hunting formation.

We held position for approximately four minutes. Then one of them moved into my light. What I saw was canine in general body plan — four legs, elongated snout, upright ears, tail. But wrong. Wrong in every detail. It was too large — shoulder height approximately 1.2 meters, which puts it in the size range of a Great Dane but with the muscular build of something designed for power, not speed. Its eyes reflected my headlamp with a blue-green luminescence, not the standard yellow-orange tapetum reflection of a canine. Its skin was hairless in patches, and the hairless patches showed what appeared to be subdermal plating — smooth, dark panels beneath the skin, like armor grown into the tissue rather than attached to it. And its jaw — when it opened its mouth, either to breathe or to display, the jaw articulation was too wide. The mandible separated at the midline, opening into a four-part arrangement that no natural canine possesses.

It looked at us. It assessed. I have been in enough combat situations to recognize assessment — the pause when a threat evaluates whether engagement is worth the cost. It decided we weren't worth it. It made a sound — a low, structured vocalization that was echoed by the other three contacts — and they withdrew. Coordinated retreat, same speed, same spacing, same silence.

My client asked if they were feral dogs. They were not feral dogs. Feral dogs don't have subdermal armor. Feral dogs don't execute coordinated flanking maneuvers. Feral dogs don't assess and withdraw on what appears to be a verbal command. Whatever those things were, someone made them, or something changed them, and they are living in the Underworld in organized packs.`,
  related_entities: ["Underworld", "Sector 5"],
  credibility: "unconfirmed",
  story_hooks: ["Pack-hunting creatures with augmented biology and tactical intelligence", "Subdermal armor suggests engineering, not natural mutation", "Organized enough to coordinate and communicate"],
  tags: ["paranormal", "creature", "eyewitness", "underworld", "predator", "augmented", "canine"]
});

emit({
  id: genId(),
  name: "Eyewitness Account: The Breathing Walls of the Deep Shelf",
  type: "document",
  document_type: "eyewitness_account",
  author: "Osei Tanaka-Balogun, Maintenance Worker",
  date: "2199-11-05",
  classification: "public",
  description: `I do sub-structural maintenance for the Shelf — the spaces between floors, the utility crawlways, the gaps in the bones of the buildings where the wiring and plumbing live. It's tight work. Claustrophobic. Most people wash out in the first month. I've been doing it for nine years because I'm small, I'm flexible, and the dark doesn't bother me. Or it didn't.

Three months ago, I was servicing a water reclamation line in the sub-structure of Shelf Block 9 — the space between the ground floor and the Underworld ceiling, which in Block 9 is about 1.5 meters of crawlspace filled with pipes, conduit, and structural supports. I was on my belly, wedged between a cold-water main and an electrical conduit, replacing a corroded valve. Normal work. Then the wall next to me moved.

Not shifted — moved. Like a chest expanding on an inhale. The concrete wall I was braced against swelled outward approximately two centimeters and then contracted. Then it did it again. And again. A slow, rhythmic expansion and contraction, approximately 12 cycles per minute, consistent across every surface I could see or touch. The floor was doing it. The ceiling was doing it. The pipes were doing it, their metal casings flexing slightly with each cycle. The entire sub-structure was breathing.

I felt it through my suit. Through my gloves. Through the tools in my hands. The vibration was deep — not just mechanical, but thermal. The surfaces warmed slightly on the expansion and cooled on the contraction. Like body heat. Like the flush of blood through tissue. I pressed my bare hand against the wall (I know, stupid, but I had to know) and it was warm. Not ambient warm. Body warm. 36, maybe 37 degrees. Concrete does not hold heat like that. Concrete does not breathe.

The breathing lasted approximately twenty minutes. Then it stopped, gradually — the cycles slowing, the amplitude decreasing, like something falling asleep. After it stopped, the wall temperature returned to ambient within about five minutes. I finished my repair. I filed a maintenance anomaly report. The report was logged as "thermal fluctuation due to adjacent systems." There are no adjacent systems in that section. I checked.

I've felt it since. Twice more in Block 9, once in Block 11. Always the same — slow breathing, thermal fluctuation, gradual cessation. My colleagues think I'm losing it. Maybe I am. But the thermal readings are in my maintenance logs, timestamped and un-editable. Whatever is happening, it's measurable. I am not imagining the numbers.`,
  related_entities: ["Shelf District", "Block 9"],
  credibility: "unconfirmed",
  story_hooks: ["The city's infrastructure appears to be alive at a biological level", "Measurable thermal data supports the account", "Connected to the Underworld Hum?"],
  tags: ["paranormal", "creature", "eyewitness", "shelf", "breathing", "infrastructure", "biological"]
});

emit({
  id: genId(),
  name: "Eyewitness Account: The Mirror Things in Old Harbor",
  type: "document",
  document_type: "eyewitness_account",
  author: "Kenna Volkov-Achebe, Bar Owner, Old Harbor",
  date: "2199-04-18",
  classification: "public",
  description: `I own a bar in Old Harbor called The Rust Nail. It's a dive. I'm not pretending otherwise. We get dockworkers, scavengers, off-duty security, and the occasional runner looking for a quiet drink. The bar's been in the family for thirty years. It has a basement — Old Harbor buildings all do, built into the old seawall foundations. The basement connects, through a locked door I've never opened, to what I'm told is the upper Underworld.

Six months ago, my bartender quit. Said he couldn't work the late shifts anymore because of "the reflections." I thought he meant the mirror behind the bar — it's old, spotted, and in the right light it makes everyone look like they're underwater. I replaced the mirror. My next bartender quit after three weeks. Same reason. "The reflections."

I started working the late shifts myself. On the third night, at approximately 03:00, with the bar empty and me cleaning up, I looked at the new mirror behind the bar and saw someone standing behind me. I turned around. Nobody there. I looked at the mirror again. Still there — a figure, standing approximately two meters behind me, visible only in the reflection. It was shaped like a person. It was my height, my build, and it was wearing my clothes. But its face was wrong. It was my face, but smoothed out — no wrinkles, no scars, no expression. Like a mannequin wearing my skin.

I stared at it. It stared at me. For the first time in my life, I understood the word "uncanny" on a visceral level — this thing was almost me, close enough to be horrifying specifically because of how close it was. Then it smiled. I was not smiling. The reflection of me smiled while I stood frozen, and it raised one hand and waved. Slowly. Deliberately. Then it walked — in the reflection — toward the basement door. I watched in the mirror as it crossed the room, opened the reflected basement door, and descended out of sight.

I've seen them four more times since. Not always me — sometimes they look like other people. Customers. Regulars. People who've been in the bar recently. They only appear in reflective surfaces — the mirror, the polished bar top, the glass windows when it's dark outside. They are always almost right and always subtly wrong. They move independently. They watch us. And they always, eventually, walk toward the basement door and disappear downward.

I've had the mirror replaced twice more. The bartender mirror doesn't matter. I've seen them in puddles. In the chrome of a beer tap. In the screen of a turned-off terminal. Whatever the mirror things are, they aren't in the mirror. They're somewhere else, and reflective surfaces are just the window.`,
  related_entities: ["Old Harbor", "Underworld"],
  credibility: "unconfirmed",
  story_hooks: ["Doppelganger entities visible only in reflections", "They mimic specific people and act independently", "They always move toward the Underworld"],
  tags: ["paranormal", "creature", "eyewitness", "old-harbor", "mirror", "doppelganger", "reflection"]
});

// ═══════════════════════════════════════════════════════════════
// SECTION 3: CHEMICAL/HALLUCINATION VS REAL THREAT (5)
// ═══════════════════════════════════════════════════════════════

emit({
  id: genId(),
  name: "Mass Hallucination or Mass Encounter: The Shelf Block 7 Incident",
  type: "document",
  document_type: "investigation_report",
  author: "Dr. Priya Nakamura-Osei, Clinical Neurologist",
  date: "2199-08-20",
  classification: "public",
  description: `On August 3, 2199, between 22:00 and 23:30, 143 residents of Shelf Block 7 independently reported seeing the same phenomenon: a luminous humanoid figure, approximately three meters tall, standing motionless in the central courtyard of the block's residential complex. The figure was described consistently across reports — pale blue luminescence, no discernible facial features, arms at its sides, perfectly still, emanating a low hum audible up to 50 meters away.

The consistency of the reports is the central puzzle. Mass hallucination events are documented in the medical literature, but they are characterized by variability — each participant hallucinates differently, influenced by personal psychology, cultural context, and the specific neurochemical disruption involved. The Block 7 event shows almost no variability. One hundred and forty-three people described the same figure, the same color, the same posture, the same sound. This level of consistency is either evidence that the figure was physically present (and therefore not a hallucination), or evidence of an unknown mechanism that can synchronize the hallucinations of over a hundred people simultaneously.

Environmental analysis conducted the following day found elevated levels of methylmercury and three unidentified organic compounds in the Block 7 atmospheric processors. Methylmercury at the detected concentration (0.4 ppm) is sufficient to cause neurological symptoms including visual disturbance, but not the structured, detailed hallucinations described by the witnesses. The three unidentified compounds remain uncharacterized — two degraded before analysis could be completed, and the third is a novel molecule that Meridian University's chemistry department has been unable to synthesize or classify.

The question, then, is this: Did 143 people inhale a cocktail of neurotoxins and brain-damaging chemicals and all hallucinate the exact same thing by coincidence? Or did 143 people see something real while simultaneously being exposed to chemicals that will be used to discredit their testimony? The timing of the contamination — present during the event, degraded before analysis — is either bad luck or remarkably convenient for anyone who wants this event filed under "mass hysteria" rather than "unexplained."

I am not advocating for either interpretation. I am noting that the conventional explanation requires assumptions that are, in their own way, as extraordinary as the unconventional one.`,
  related_entities: ["Shelf District", "Meridian University"],
  credibility: "disputed",
  story_hooks: ["143 people saw the same impossible thing", "Chemical contamination present but insufficient to explain the event", "Is someone deliberately contaminating the air to provide cover for real phenomena?"],
  tags: ["paranormal", "hallucination", "mass-event", "chemical", "investigation", "shelf"]
});

emit({
  id: genId(),
  name: "Neurotoxin Profile: Underworld Atmospheric Contaminants",
  type: "document",
  document_type: "academic_paper",
  author: "Dr. Emeka Johansson-Liang, Meridian University Toxicology",
  date: "2199-02-14",
  classification: "public",
  description: `This paper presents a comprehensive analysis of atmospheric contaminants found in the Underworld of Meridian 88, with specific focus on compounds known to produce neurological effects including hallucination, paranoia, temporal distortion, and anomalous sensory perception. The relevance of this analysis to the ongoing debate about "paranormal" phenomena reported in the Underworld should be self-evident.

The Underworld atmosphere contains a complex mixture of industrial byproducts, biological metabolites, and degradation products accumulated over more than a century of human and industrial activity. Of the 847 distinct compounds identified in our survey, 23 are known neurotoxins, 14 are known hallucinogens at sufficient concentration, and 91 are novel compounds with unknown neurological profiles. The deep levels (below B30) show dramatically higher concentrations of all categories — a predictable consequence of poor ventilation, thermal stratification, and the accumulation of heavy, dense chemical species in lower areas.

Particular attention is drawn to what we term the "Underworld Cocktail" — the synergistic effect of simultaneous exposure to multiple neuroactive compounds. Individual compounds at the detected concentrations may fall below established effect thresholds. But threshold models assume single-compound exposure. In the Underworld, residents and workers are exposed to dozens of neuroactive compounds simultaneously, and the interaction effects are unstudied and unpredictable. Our preliminary modeling suggests that the combined effect could produce sustained hallucinatory states at depths below B25, with severity increasing with depth — a prediction that maps neatly onto the observed increase in paranormal reports at greater depth.

However — and this is the caveat that I suspect will be underreported when this paper is cited — our model explains the type of phenomena reported but not the specificity. Chemical hallucinations are typically chaotic, personalized, and inconsistent. The Underworld phenomena are often highly structured, consistent across observers, and repeatable. If the Deep 88 Crawler sightings are hallucinations, they are hallucinations of remarkable uniformity — dozens of people in different locations, at different times, under different exposure conditions, hallucinating the same multi-limbed pale organism with the same body plan and the same behavior patterns.

This paper does not resolve the question. It establishes that chemical contamination is a viable contributing factor to anomalous perception in the Underworld. It does not establish that chemical contamination is a sufficient explanation. The difference between "contributing factor" and "complete explanation" is the space where honest disagreement lives.`,
  related_entities: ["Meridian University", "Underworld"],
  credibility: "verified",
  story_hooks: ["Scientific framework that both supports and undermines the 'it's all chemicals' explanation", "91 novel compounds with unknown effects", "Chemical model predicts type but not specificity of reports"],
  tags: ["paranormal", "hallucination", "chemical", "neurotoxin", "academic", "underworld", "contamination"]
});

emit({
  id: genId(),
  name: "Are They Hunting Us: Corporate Bio-Research and Underworld Fauna",
  type: "document",
  document_type: "investigation_report",
  author: "The Undernet Collective — Anonymous Publication",
  date: "2199-09-30",
  classification: "leaked",
  description: `Let us be very clear about what we are alleging: at least two corponations operating in Meridian 88 — Helix Biosystems and Lazarus Pharmaceuticals — are conducting unauthorized biological research in the Underworld, and the organisms they have created or modified have escaped containment and are now living, breeding, and hunting in the tunnel network beneath our city. This is not paranoia. This is documented corporate behavior.

Helix Biosystems holds 14 patents on organisms designed for "subterranean environmental remediation" — creatures engineered to eat toxic waste, process contaminated water, and break down industrial chemicals in enclosed spaces. These patents describe organisms with precisely the characteristics reported by Underworld eyewitnesses: pale coloration from lack of UV exposure, enhanced chemical sensitivity, multi-limbed locomotion for navigating irregular terrain, and — crucially — the ability to metabolize a wide range of organic compounds including, theoretically, human tissue. Helix claims these organisms have never been deployed outside laboratory conditions. Helix also claimed, for three years, that their GeneWright product had no mutagenic side effects, until internal documents proved otherwise.

Lazarus Pharmaceuticals' involvement is more speculative but supported by circumstantial evidence. Lazarus holds exclusive pharmaceutical distribution rights in the Underworld through a subsidiary called DeepMed Solutions. DeepMed clinics operate on levels B5 through B20, providing medical services to Underworld residents at subsidized rates. The question is: why? Lazarus is not a charity. Subsidized medical care in the Underworld generates no profit. Unless the clinics serve a dual purpose — data collection. Every patient treated at a DeepMed clinic provides biological samples as standard intake procedure. If Lazarus is conducting mutagenic research, a population already exposed to the Underworld's chemical environment would be an ideal study group.

We have obtained partial internal communications from a Helix Biosystems employee who claims that "Project TAXONOMY" — a classified research initiative listed in Helix's internal project database with no public description — involves "field testing of engineered organisms in uncontrolled deep-level environments." The employee was unable to provide complete documentation before losing network access. They have not been heard from since.

The creatures people are seeing in the tunnels are not ghosts, not demons, not hallucinations. They are products. Someone made them. Someone released them. And nobody is looking for them because the people being hunted are Underworld residents whose disappearances are logged as "voluntary relocation" by a municipal government that doesn't extend its jurisdiction past B30.`,
  related_entities: ["Helix Biosystems", "Lazarus Pharmaceuticals", "DeepMed Solutions", "Underworld"],
  credibility: "unconfirmed",
  story_hooks: ["Corporate bio-research as the source of Underworld creatures", "Helix's 'Project TAXONOMY'", "Lazarus using Underworld clinics as research fronts"],
  tags: ["paranormal", "corporate", "bio-research", "creatures", "underworld", "helix", "lazarus", "conspiracy"]
});

emit({
  id: genId(),
  name: "Psychogenic Epidemic or Genuine Contact: A Psychiatric Analysis",
  type: "document",
  document_type: "academic_paper",
  author: "Dr. Fatima Al-Rashid-Okonkwo, Meridian University Psychiatry",
  date: "2200-01-15",
  classification: "public",
  description: `This paper examines the sharp increase in paranormal encounter reports filed with Meridian 88 municipal authorities between 2195 and 2200 — a 340% increase that has outpaced population growth, changes in reporting infrastructure, and all other identified demographic factors. The central question is whether this increase represents a psychogenic epidemic (a socially transmitted delusional framework) or a genuine increase in encounters with unexplained phenomena.

The psychogenic epidemic model has strong precedent. History is rich with examples of mass delusion driven by social stress, environmental contamination, and information cascades. Meridian 88's population is under extraordinary stress — economic inequality, surveillance pressure, chemical exposure, and the existential vertigo of living in a city where corponations hold sovereign power and the social contract has been replaced by a licensing agreement. Under these conditions, a shared delusional framework is not only possible but expected. People under extreme stress seek explanations for their suffering. "Monsters in the tunnels" is a comprehensible, externalizable threat — much easier to process psychologically than "the system is designed to exploit me and there is no escape."

Supporting this model: the geographic distribution of reports correlates strongly with socioeconomic deprivation. The Shelf and the Underworld generate 87% of all paranormal reports. The Spires generate 2%. Wealth insulates against anomalous experience — or, alternatively, wealth insulates against the social and chemical conditions that produce anomalous perception.

Complicating this model: the physical evidence. Not all reports are subjective. Photographs, thermal data, acoustic recordings, chemical analyses, and instrument readings accompany a significant minority of reports and resist easy dismissal. The NovaMind shared dream data (leaked despite corporate suppression) cannot be explained by social contagion — the subjects were isolated and shielded. The Underworld Hum is measurable, physical, and real, whatever its source. The VU-7-Alpha compound in the water supply is a laboratory-verified novel molecule with documented neuroactive properties.

My professional assessment — and I recognize its unsatisfying ambiguity — is that both models are probably partially correct. The psychogenic epidemic is real; social stress is amplifying perception and lowering the threshold for anomalous interpretation. But the epidemic is being fed by something. The question is whether that something is mundane (chemicals, infrastructure decay, corporate negligence) or unprecedented (phenomena that our current scientific framework cannot accommodate). I do not know the answer. I note, however, that "I don't know" is a more honest conclusion than either "it's all in their heads" or "the monsters are real."`,
  related_entities: ["Meridian University", "Shelf District", "Underworld", "NovaMind", "Vossen Utilities"],
  credibility: "verified",
  story_hooks: ["340% increase in paranormal reports in 5 years", "Psychiatric framework that takes the middle ground", "Physical evidence resists the 'it's all delusion' model"],
  tags: ["paranormal", "hallucination", "psychiatric", "academic", "mass-delusion", "evidence"]
});

emit({
  id: genId(),
  name: "TESSERA Internal Memo: Underworld Biological Threat Assessment",
  type: "document",
  document_type: "classified_briefing",
  author: "TESSERA Strategic Threat Analysis Group",
  date: "2199-11-01",
  classification: "classified",
  description: `CLASSIFICATION: TESSERA INTERNAL — EXECUTIVE DISTRIBUTION ONLY

SUBJECT: Preliminary Assessment of Biological Threat Vectors Originating from Underworld Infrastructure

EXECUTIVE SUMMARY: TESSERA's Underworld monitoring assets have detected a statistically significant increase in biological activity below Level B30 that cannot be attributed to known fauna, human activity, or environmental factors. This memo recommends elevating the Underworld biological threat level from AMBER to RED and allocating dedicated resources to investigation and containment.

THREAT PROFILE: Since January 2199, TESSERA monitoring stations in the Underworld have logged 2,847 biological detection events on levels B30 through B50 — a 520% increase over the same period in 2198. Detection events include motion signatures, thermal signatures, acoustic signatures, and chemical traces consistent with large (>50 kg) biological organisms operating outside human habitation zones. The signatures do not match any cataloged species in the Meridian 88 fauna database, including known feral populations, escaped laboratory specimens, or engineered organisms from registered corporate bio-research programs.

PATTERN ANALYSIS: The organisms — if that is what they are — display behavioral patterns consistent with apex predators establishing territory. Movement data shows expanding operational ranges over time, consistent with a growing population exploring new territory. Acoustic data includes vocalizations that our analytical AI classifies as "structured communication" with 78% confidence. The organisms are moving upward — the median detection depth has decreased from B47 in January to B38 in October. Whatever is down there is coming up.

COMPETING HYPOTHESES: Two explanations are under evaluation. First: one or more corponations (suspected: Helix Biosystems, Crucible Industries) are conducting unauthorized biological research in the deep Underworld, and their products have escaped containment. This explanation is consistent with the biological signatures but does not explain the communication patterns or the apparent rate of population growth, which exceeds any known reproductive cycle for organisms of the detected size. Second: the Underworld's mutagenic environment has produced novel organisms through natural processes accelerated by chemical and geneware contamination. This explanation accounts for the novelty of the signatures but raises uncomfortable questions about what baseline organism mutated, and what selective pressures in the Underworld would produce predatory behavior, structured communication, and upward territorial expansion.

RECOMMENDATION: TESSERA should deploy a dedicated reconnaissance team to the B35-B45 zone with orders to capture or kill a specimen. Without physical evidence, threat assessment cannot proceed beyond speculation. The cost of investigation is bounded. The cost of being unprepared for an emerging apex predator establishing itself in our infrastructure is not.

This memo has not been shared with municipal authorities or other corponations. TESSERA's position is that any discovered biological asset represents a potential proprietary resource and should be secured before public disclosure.`,
  related_entities: ["TESSERA", "Helix Biosystems", "Crucible Industries", "Underworld"],
  credibility: "suppressed",
  story_hooks: ["TESSERA confirms biological threats are real and getting worse", "520% increase in detections in one year", "Creatures are moving upward toward inhabited levels", "TESSERA wants to capture specimens as proprietary assets"],
  tags: ["paranormal", "corporate", "biological", "threat-assessment", "tessera", "underworld", "classified"]
});

// ═══════════════════════════════════════════════════════════════
// SECTION 4: CYBERNETICALLY ENHANCED ANIMALS ESCAPING LABS (5)
// ═══════════════════════════════════════════════════════════════

emit({
  id: genId(),
  name: "Augmented Wolf Pack Escapes Arcturus Defense Research Facility",
  type: "document",
  document_type: "news_article",
  author: "Vantablack Media News Service",
  date: "2199-04-03",
  classification: "public",
  description: `MERIDIAN 88 — An Arcturus Defense research facility in the northern industrial corridor has confirmed the escape of twelve cybernetically enhanced wolves following a containment breach on March 29. The animals, designated as Project FENRIR assets, are equipped with subdermal titanium-alloy armor plating, enhanced sensory arrays including thermal and electromagnetic detection, and neural-linked communication implants that allow pack coordination at distances of up to 2 kilometers.

Arcturus Defense issued a terse public statement acknowledging the escape and advising residents of the northern industrial corridor and adjacent Shelf districts to "exercise caution and report unusual canine activity." The statement notably omitted several details confirmed by sources within the facility: that the wolves are equipped with retractable monofilament claws capable of cutting through commercial-grade steel, that their jaw servos have been enhanced to produce a bite force of approximately 8,000 Newtons (roughly ten times that of a natural wolf), and that the neural-link implants include behavioral conditioning designed to make the animals aggressive toward human targets exhibiting fear responses.

Project FENRIR was reportedly designed to produce autonomous perimeter security animals for Arcturus corporate installations — a cheaper and more psychologically intimidating alternative to drone systems. The wolves are based on Canadian grey wolf genetic stock, augmented with synthetic muscle fiber, titanium skeletal reinforcement, and a combat-oriented BCI that enhances reaction time and pack coordination. Each animal is estimated to weigh approximately 120 kilograms, nearly three times the mass of a natural grey wolf.

Municipal Animal Control has declined jurisdiction, noting that "cybernetically enhanced military assets fall outside the scope of animal welfare regulation." Arcturus Defense has deployed a retrieval team but has not disclosed their success rate. As of publication, four of the twelve wolves have been recovered. The remaining eight are unaccounted for. Shelf District Community Watch has reported canine tracks in the B12 access corridors that are "too large and too deep" for any known feral dog population. Residents of Shelf Blocks 30 through 35 have reported hearing coordinated howling from the Underworld access points at night — not the random vocalizations of strays, but structured, synchronized calls that rise and fall in unison, as if the pack is communicating.

Animal rights organizations have condemned both the creation and the escape of the FENRIR wolves, calling them "victims of corporate hubris who are now a danger to themselves and to the public." Arcturus Defense has not commented on the ethics of the program. They have, however, increased the bounty for returned specimens to Φ5,000 per animal, alive only.`,
  related_entities: ["Arcturus Defense", "Shelf District", "Project FENRIR"],
  credibility: "verified",
  story_hooks: ["Eight augmented wolves loose in the Underworld", "Military-grade cybernetic predators", "Are they breeding? Establishing territory?"],
  tags: ["news", "augmented-animals", "arcturus", "wolves", "escape", "cybernetics", "FENRIR"]
});

emit({
  id: genId(),
  name: "Enhanced Raptor Program Breach: Crucible Industries Admits Loss",
  type: "document",
  document_type: "news_article",
  author: "The Meridian Independent — Investigative Desk",
  date: "2199-07-22",
  classification: "public",
  description: `MERIDIAN 88 — Crucible Industries has acknowledged the loss of six cybernetically enhanced peregrine falcons from its Applied Biology Division, marking the third documented escape of augmented predatory animals from a Meridian 88 corporate facility in the past eighteen months. The falcons, part of a surveillance and reconnaissance program designated TALON WATCH, are equipped with miniaturized camera arrays, encrypted mesh transceivers, and neural augmentation packages that enhance their already formidable hunting instincts with tactical awareness algorithms.

The admission came only after independent drone operators began capturing footage of birds moving at speeds exceeding 500 kilometers per hour through Meridian 88's upper air corridors — significantly faster than any natural peregrine falcon and fast enough to pose a collision risk to vertiport traffic. Natural peregrines reach approximately 390 km/h in hunting dives. The Crucible birds have been clocked at 520 km/h in level flight, courtesy of synthetic muscle augmentation and lightweight carbon-composite skeletal reinforcement.

More concerning than their speed is their behavior. TALON WATCH falcons were designed for intelligence gathering — they are, in effect, biological surveillance drones. Their camera arrays transmit encrypted data to a Crucible ground station. Except the escaped birds are no longer transmitting to Crucible. Their encrypted feeds went dark within hours of the escape, replaced by a communication protocol that Crucible's decryption team has been unable to crack. The birds are still transmitting data. They are transmitting it to someone or something that is not Crucible Industries.

Wildlife monitoring systems have tracked the falcons establishing nesting sites on the upper levels of the Spine Tower complex — prime territory that puts them within surveillance range of seven major corporate headquarters, three government facilities, and the primary vertiport. Whatever they are watching, they are watching it from the best vantage point in the city.

A Crucible Industries spokesperson described the escape as "a routine containment incident" and assured the public that the birds "pose no threat to human safety." The spokesperson did not address the question of who the birds are now transmitting to, or why Crucible's behavioral conditioning — designed to ensure the birds remain loyal to their handlers — apparently failed completely within hours of the escape.`,
  related_entities: ["Crucible Industries", "Spine Tower", "TALON WATCH"],
  credibility: "verified",
  story_hooks: ["Augmented surveillance birds transmitting to an unknown receiver", "Behavioral conditioning failed instantly", "Who or what are the birds reporting to now?"],
  tags: ["news", "augmented-animals", "crucible", "falcons", "escape", "surveillance", "cybernetics"]
});

emit({
  id: genId(),
  name: "Cybernetic Bear Sighting Confirmed in Upper Peninsula Buffer Zone",
  type: "document",
  document_type: "news_article",
  author: "Vantablack Media Field Report",
  date: "2199-10-14",
  classification: "public",
  description: `UPPER PENINSULA BUFFER ZONE — A cybernetically enhanced black bear, believed to have escaped from an unregistered Ringo Corp research facility, has been confirmed active in the buffer zone between Meridian 88's northern perimeter and the Upper Peninsula contested territory. The animal was captured on three separate surveillance systems over a 48-hour period, providing clear documentation of its augmented anatomy.

The bear, estimated at 450 kilograms (approximately twice the mass of a large natural black bear), displays extensive cybernetic modification: a reinforced cranial casing visible as metallic plating above the brow ridge, articulated armor segments along the spine, and a left forelimb that has been entirely replaced with a prosthetic assembly of unknown manufacture. The prosthetic limb appears to include manipulator digits significantly more dexterous than a natural bear paw — surveillance footage shows the animal operating a mechanical latch on a supply shed with what can only be described as deliberate, tool-using behavior.

Ringo Corp has denied involvement, stating that "Ringo Corp does not operate biological augmentation programs." This denial is contradicted by procurement records obtained through a Freedom of Information request showing that Ringo's Applied Sciences Division purchased 800 kilograms of veterinary-grade cybernetic interface components from NovaMind's industrial division in 2197. NovaMind confirmed the sale but stated the components were "for agricultural automation applications."

Buffer Zone rangers have tracked the bear's movements for three weeks. Its behavior is anomalous for a black bear in several respects: it is primarily nocturnal (black bears are typically diurnal), it avoids trail cameras with a consistency that suggests it understands surveillance technology, and it has been observed pausing at communication relay stations — the kind that broadcast mesh network signals — and remaining motionless beside them for periods of ten to thirty minutes, as if listening to or interacting with the transmissions.

The bear has not exhibited aggression toward humans, though rangers advise extreme caution. Standard anti-bear protocols are unlikely to be effective against an animal with cranial armor and a cybernetic limb. Arcturus Defense has offered to "neutralize the asset" for a fee. Ringo Corp has not responded to Arcturus's offer. The bear remains at large.`,
  related_entities: ["Ringo Corp", "NovaMind", "Arcturus Defense", "Upper Peninsula"],
  credibility: "verified",
  story_hooks: ["A cybernetic bear that understands surveillance and interacts with communication relays", "Ringo denying a program they clearly funded", "What is the bear doing at the relay stations?"],
  tags: ["news", "augmented-animals", "ringo", "bear", "escape", "cybernetics", "upper-peninsula"]
});

emit({
  id: genId(),
  name: "Swarm Intelligence Incident: Augmented Rat Colony Overwhelms Lab",
  type: "document",
  document_type: "news_article",
  author: "The Shelf Wire — Community News Network",
  date: "2199-06-08",
  classification: "public",
  description: `GEARTOWN — A Lazarus Pharmaceuticals testing facility in Geartown's research corridor was evacuated on June 4 after an estimated 3,000 cybernetically enhanced laboratory rats overwhelmed containment systems and escaped into the surrounding infrastructure. The rats, part of Lazarus's neural network research program, are equipped with miniaturized BCI implants that connect them in a mesh communication network — effectively giving the colony a shared nervous system.

The escape was not random. Security footage shows the rats acting in coordinated waves — groups of approximately 100 animals each performing specific tasks simultaneously. One wave disabled the containment locks by chewing through specific cables — not random cables, but the exact cables controlling the electromagnetic locks on their enclosures. Another wave attacked the ventilation system, creating exit routes through ductwork. A third wave disabled the facility's internal surveillance by gnawing through fiber optic lines at junction points that would cause maximum system disruption with minimum effort. The entire escape, from first lock failure to last rat through the vents, took eleven minutes.

Lazarus's project documentation, portions of which were leaked by a facility technician who participated in the evacuation, describes the rats as "a distributed biological computing platform" — each rat functions as a node in a neural network, with the BCI implants allowing information sharing, coordinated decision-making, and collective problem-solving capabilities that far exceed any individual rat's cognitive capacity. The colony's collective intelligence, according to the documentation, tests at approximately human baseline on spatial reasoning and pattern recognition tasks. The documentation does not address what happens when a human-baseline intelligence is distributed across 3,000 mobile nodes with an instinct for survival and a demonstrated ability to defeat containment systems.

The rats have been tracked to the Underworld access points beneath Geartown. Once in the tunnel network, tracking became effectively impossible — the rats' BCI signals blend into the electromagnetic noise of the Underworld. They are down there now. Three thousand networked, collectively intelligent rats with an established communication infrastructure, exploring a tunnel network that connects to every part of the city.

Lazarus Pharmaceuticals has issued a recall notice offering Φ2 per returned rat. At 3,000 rats, the total recall budget is Φ6,000. The research program cost Φ14 million. The disparity in those numbers tells you everything you need to know about Lazarus's actual interest in recovery.`,
  related_entities: ["Lazarus Pharmaceuticals", "Geartown", "Underworld"],
  credibility: "verified",
  story_hooks: ["3,000 networked rats with collective human-level intelligence loose in the Underworld", "They planned and executed their own escape", "What does a distributed rat intelligence do with the Underworld's resources?"],
  tags: ["news", "augmented-animals", "lazarus", "rats", "swarm", "escape", "neural-network", "BCI"]
});

emit({
  id: genId(),
  name: "Ouroboros Energy Denies Connection to Feral Augmented Cats in Tunnels",
  type: "document",
  document_type: "news_article",
  author: "Vantablack Media News Service",
  date: "2199-12-02",
  classification: "public",
  description: `MERIDIAN 88 — Ouroboros Energy has denied any connection to a population of cybernetically augmented feral cats that Underworld residents have been reporting with increasing frequency since mid-2199. The cats, described as "larger than normal, faster than normal, and way too smart," are distinguished from the existing feral cat population by visible cybernetic modifications: ocular implants that glow amber in darkness, retractable claws reinforced with what appears to be carbide or ceramic composite, and in several documented specimens, a spinal-mount antenna array of unknown purpose.

The denial is significant because the antenna arrays bear the manufacturing stamp of Ouroboros Energy's subsidiary, OE Microelectronics, which produces specialized communications equipment for deep-infrastructure monitoring. When confronted with this detail, Ouroboros's public relations office stated that "OE Microelectronics components are widely available through secondary markets and their presence on an unauthorized biological platform does not indicate Ouroboros involvement." This is technically true — OE components are sold to third parties. It is also the kind of technically true statement that corporations make when they are lying.

The augmented cats number in the hundreds, according to estimates from Underworld residents who interact with them regularly. Unlike the feral cats that have inhabited the Underworld for decades (many of which are themselves descendants of escaped biolab specimens and display bioluminescent fur, a legacy of early geneware experiments), the augmented cats display enhanced cognitive behavior. They have been observed operating in coordinated groups, ambushing Underworld rat populations with pincer tactics, and — in the most unsettling reports — interacting with the Underworld's data infrastructure. Multiple witnesses describe augmented cats sitting motionless beside network junction boxes, their spinal antennas extended, for hours at a time.

An Underworld data broker known as Glass (no surname provided) claims to have captured one of the augmented cats and examined its implants. According to Glass, the spinal antenna is not a transmitter — it is a receiver. The cats are listening to the Underworld's data traffic. What they are doing with the information is unknown. "They're not just cats anymore," Glass told The Wire. "They're sensors. Somebody turned the Underworld's feral cat population into a distributed sensor network, and the cats don't seem to mind."

Ouroboros Energy has issued a Φ500 reward for any captured augmented cat delivered to their research division — a curious offer from a corporation that claims no connection to the animals.`,
  related_entities: ["Ouroboros Energy", "OE Microelectronics", "Underworld"],
  credibility: "verified",
  story_hooks: ["Hundreds of augmented cats functioning as a sensor network", "Ouroboros components but Ouroboros denies involvement", "What data are the cats collecting and for whom?"],
  tags: ["news", "augmented-animals", "ouroboros", "cats", "sensor-network", "underworld", "cybernetics"]
});

// ═══════════════════════════════════════════════════════════════
// SECTION 5: GENETIC HYBRID PET MILLS IN THE SHELF (5)
// ═══════════════════════════════════════════════════════════════

emit({
  id: genId(),
  name: "Glow Pets: Inside the Shelf's Booming Designer Animal Market",
  type: "document",
  document_type: "news_article",
  author: "Vantablack Media Lifestyle Desk",
  date: "2199-05-15",
  classification: "public",
  description: `SHELF DISTRICT — In a cramped apartment on the fourteenth floor of Shelf Block 22, a woman who goes by Duchess runs a business that would have been science fiction twenty years ago and is now just Tuesday in Meridian 88. Duchess breeds bioluminescent cats. Her one-bedroom apartment houses seventeen adults and, at the time of our visit, forty-three kittens, all of them glowing in soft shades of blue, green, and — her signature product — a warm amber-gold that she calls "honeylight."

The cats are the result of geneware modification applied to standard domestic feline embryos. The bioluminescence gene, originally derived from deep-sea jellyfish and refined through several generations of geneware iteration, has been integrated into the cat genome so thoroughly that it breeds true — kittens from two glowing parents glow without additional modification. Duchess has been refining her breeding lines for six years. Her honeylight variety, which produces a steady warm glow visible in low-light conditions, sells for Φ800 to Φ1,200 per kitten, depending on brightness and color consistency.

The market is enormous. In a city where natural sunlight is a luxury available primarily to Spire residents, a pet that produces its own warm light has both practical and emotional value. Shelf residents use glowing cats as nightlights, mood lighting, and — in the deeper blocks where electrical service is unreliable — functional illumination. "A honeylight cat in your apartment means you can find the bathroom at 3 AM without turning on anything," says a customer who purchased two kittens last month. "Plus they purr. Try getting an LED strip to purr."

Duchess is one of an estimated 200 to 300 small-scale genetic hybrid breeders operating in the Shelf District. The industry is entirely unregulated — genetic modification of animals falls into a legal gray zone that Meridian 88's corporate governance structure has not addressed, primarily because no corponation has yet claimed jurisdiction over the pet industry. Breeders operate under informal guild rules: no dangerous modifications, no sapience enhancement, no military applications, and a shared blacklist of geneware suppliers whose products have caused harmful mutations.

The industry's dark side is real but contained. Poorly executed genetic modifications can produce animals with painful conditions — bioluminescence that generates heat and burns the animal's skin, skeletal modifications that cause chronic pain, neurological changes that produce seizures. The informal guild polices its own: breeders who sell suffering animals are blacklisted, and in at least two documented cases, physically ejected from the Shelf. "We're not monsters," Duchess says, adjusting a honeylight kitten on her shoulder. "We're artists. And we take care of our canvas."`,
  related_entities: ["Shelf District"],
  credibility: "verified",
  story_hooks: ["Thriving underground genetic pet industry", "Unregulated geneware modification", "The tension between art and animal welfare"],
  tags: ["news", "genetic-hybrid", "pets", "bioluminescent", "cats", "shelf", "geneware", "breeding"]
});

emit({
  id: genId(),
  name: "Puppy Mill Raid Uncovers Genetic Horrors in Shelf Block 40",
  type: "document",
  document_type: "news_article",
  author: "The Shelf Wire — Community News Network",
  date: "2199-08-09",
  classification: "public",
  description: `SHELF DISTRICT — A Community Watch raid on a residential unit in Shelf Block 40 has exposed what participants describe as "the worst genetic breeding operation we've ever seen" — an unlicensed puppy mill producing genetically modified designer dogs using unstable geneware sequences that have resulted in severe deformities, chronic pain, and what one veterinary volunteer called "biology that shouldn't be possible."

The operation, run by an individual identified only as Kole, occupied three connected apartments on Block 40's ninth floor. Inside, Watch members found approximately 80 dogs in various states of genetic modification, housed in stacked cages with inadequate ventilation, nutrition, and medical care. The modifications ranged from cosmetic (color-shifting fur, miniaturization, bioluminescent markings) to structural (additional limbs, modified skeletal structures, altered organ configurations) to what can only be described as experimental — animals with modifications that served no apparent purpose and caused obvious suffering.

Among the animals recovered: a dog with transparent skin through which internal organs were visible, apparently intended as a "novelty pet" but suffering from severe UV sensitivity and chronic infection. Three dogs with wings — non-functional wing structures grafted onto their scapulae through geneware modification, causing chronic skeletal pain and mobility impairment. A litter of puppies with compound eyes resembling those of insects, produced by splicing arthropod visual development genes into canine embryos; the puppies were blind, the compound eyes non-functional in a mammalian neural architecture. And, most disturbing, two dogs that had been subjected to what appears to be cognitive enhancement geneware — their brain mass was visibly enlarged, their cranial structures expanded to accommodate it, and they displayed behaviors that the attending veterinarian described as "problem-solving at a level I've never seen in a canine, combined with obvious distress at their physical condition."

Kole was not present during the raid and remains at large. His customer records, recovered from a terminal in the apartment, show sales to buyers across Meridian 88, including several Spire district addresses — suggesting that the market for genetic novelty pets extends well beyond the Shelf's economic stratum. Prices ranged from Φ200 for simple cosmetic modifications to Φ15,000 for custom-ordered genetic chimeras.

The recovered animals have been distributed to a network of volunteer veterinary caregivers. Many are expected to require lifelong medical support. Eleven were euthanized at the scene due to suffering that could not be mitigated. The breeder's guild has placed Kole on its permanent blacklist, for whatever that's worth.`,
  related_entities: ["Shelf District", "Community Watch"],
  credibility: "verified",
  story_hooks: ["The dark side of unregulated genetic pet breeding", "Cognitively enhanced dogs that understand their own suffering", "Spire residents buying these animals — the cruelty serves the wealthy"],
  tags: ["news", "genetic-hybrid", "pets", "puppy-mill", "geneware", "animal-cruelty", "shelf", "raid"]
});

emit({
  id: genId(),
  name: "The Foxlight Phenomenon: Designer Pets Gone Feral in the Shelf",
  type: "document",
  document_type: "news_article",
  author: "Vantablack Media Lifestyle Desk",
  date: "2199-11-20",
  classification: "public",
  description: `SHELF DISTRICT — They call them foxlights — the bioluminescent foxes that have established a breeding population in the Shelf District's upper infrastructure, the crawlspaces and ventilation shafts and utility corridors that honeycomb the space between floors. Originally produced by genetic hybrid breeders as high-end designer pets — a red fox chassis with bioluminescent fur in shades of electric blue and violet — the foxlights have escaped, bred, and adapted to life in the Shelf's interstitial spaces so successfully that they now number in the hundreds.

Nobody is quite sure when the transition from pet to feral population happened. Foxlights were first sold as exotic companions around 2195, marketed as "the ultimate luxury pet for the discerning Shelf resident" at prices ranging from Φ2,000 to Φ5,000. The problem is that foxes are not dogs. They are clever, agile, independent, and motivated escape artists. Within a year, escaped foxlights were being spotted in the ventilation systems. Within two years, they were breeding. By 2199, the feral foxlight population has become a permanent fixture of Shelf ecology.

Residents are divided. Many love the foxlights — they are beautiful animals, their bioluminescent fur casting soft blue and violet light through ventilation grates and access panels, turning the Shelf's grim infrastructure into something almost magical at night. "They're the only pretty thing in this whole district," says one resident. "At night, you can see them moving through the vent shafts above the corridors, and it looks like the Northern Lights got trapped in the ceiling." Children in several blocks have begun leaving food at vent openings, cultivating relationships with specific foxlights that return regularly.

Others are less enthusiastic. The foxes raid food stores, chew through wiring (causing at least three documented electrical fires), and produce territorial vocalizations at 3 AM that sound, in one resident's memorable phrase, "like a baby being murdered by a smaller, angrier baby." The bioluminescence, while beautiful, also means the foxes are visible through walls and floors at night, creating unsettling mobile light sources that can be mistaken for electrical faults, surveillance drones, or — in the Shelf's paranoid atmosphere — something worse.

The most interesting development is an apparent second generation of mutations. Some foxlight kittens are displaying bioluminescent patterns not present in either parent — new colors, pulsing effects, and in at least one documented case, bioluminescence that responds to sound, brightening when exposed to music or human speech. The geneware modification is continuing to express and evolve through natural breeding. Nobody designed these new patterns. They are emerging on their own. The foxlights, it seems, are still becoming whatever they are going to become.`,
  related_entities: ["Shelf District"],
  credibility: "verified",
  story_hooks: ["A feral bioluminescent fox population evolving new traits spontaneously", "Geneware continuing to mutate through natural breeding", "Beauty and chaos in equal measure"],
  tags: ["news", "genetic-hybrid", "pets", "foxlights", "bioluminescent", "feral", "shelf", "mutation"]
});

emit({
  id: genId(),
  name: "Aquatic Hybrid Craze: Engineered Fish Flooding the Shelf Market",
  type: "document",
  document_type: "news_article",
  author: "The Shelf Wire — Community News Network",
  date: "2200-01-28",
  classification: "public",
  description: `SHELF DISTRICT — The latest trend in the Shelf's genetic pet market is aquatic: engineered fish and amphibians modified for aesthetics, functionality, and — in the newest innovation — emotional companionship. The aquatic hybrid market has exploded in the past year, driven by a simple economic reality: fish are cheaper to produce, easier to house, and less ethically fraught than mammalian genetic modifications. A bioluminescent cat kitten costs Φ800. A genetically engineered "mood fish" that changes color in response to its owner's biometric data, transmitted through a basic BCI interface, costs Φ45.

The mood fish — sold under the brand name "FeelFin" by a breeder collective operating out of Shelf Block 17 — are the current bestseller. Each fish contains a bioluminescent gene array linked to a simple wireless receiver tuned to the owner's BCI broadcast frequency. When the owner is calm, the fish glows blue. Stressed: red. Happy: gold. The effect is a living mood ring, a pet that literally reflects your emotional state. The FeelFin collective has sold approximately 12,000 units in six months, grossing an estimated Φ540,000 — a staggering figure for a Shelf-based micro-enterprise.

Beyond the FeelFin, the aquatic market includes: self-cleaning tank fish engineered with enzyme-producing skin that breaks down organic waste, reducing tank maintenance to near zero. "Music fish" with modified swim bladders that produce audible tones when stimulated by water vibration, effectively creating a living instrument that plays itself. Miniature octopuses with enhanced chromatophores that can display simple images on their skin — the current fad is octopuses trained to display the owner's mesh handle or gang affiliation on demand. And, at the high end, a jellyfish variant called the "Chandelier" that grows to approximately one meter in diameter, produces brilliant multi-color bioluminescence, and is sold as living art — a biological light fixture that feeds on nutrients dissolved in its tank water and can live for decades.

The ecological risk of the aquatic market is a growing concern. The Gulch, which interfaces with Lake Michigan's water system, has reported multiple sightings of engineered fish in its drainage pools — escaped or discarded pets that have reached open water. The long-term consequences of introducing bioluminescent, genetically modified organisms into the Great Lakes ecosystem are unknown. Nobody is studying it. Nobody is regulating it. And at Φ45 per fish, the market shows no signs of slowing down.`,
  related_entities: ["Shelf District", "The Gulch", "Lake Michigan"],
  credibility: "verified",
  story_hooks: ["BCI-linked mood fish as the new mass market pet", "Engineered organisms reaching the Great Lakes", "A thriving economy nobody controls"],
  tags: ["news", "genetic-hybrid", "pets", "aquatic", "fish", "bioluminescent", "shelf", "market"]
});

emit({
  id: genId(),
  name: "The Ethics of Living Light: Genetic Hybrid Pet Industry Debate",
  type: "document",
  document_type: "news_article",
  author: "Vantablack Media Op-Ed Desk",
  date: "2200-02-14",
  classification: "public",
  description: `MERIDIAN 88 — The genetic hybrid pet industry, which operates in a regulatory vacuum between corporate patent law and nonexistent animal welfare legislation, is forcing Meridian 88 to confront a question that nobody in power wants to answer: what do we owe the things we create?

The industry generates an estimated Φ12 million annually across the Shelf District alone, employing hundreds of breeders and supporting a supply chain of geneware suppliers, veterinary caregivers, specialty food producers, and habitat fabricators. It provides income in a district where income is scarce. It provides beauty in a district where beauty is scarce. It provides companionship in a city where human connection is increasingly mediated by corporate platforms that charge per interaction.

It also produces suffering. The Shelf Block 40 raid in August exposed the worst of it, but the worst is not the norm. Interviews with 30 breeders conducted for this piece reveal a community that, by its own standards, cares deeply about animal welfare — that polices itself through guild rules, blacklists abusers, and takes pride in producing healthy, viable animals. But "by its own standards" is doing a lot of work in that sentence. Even the responsible breeders are modifying animal genomes without formal training, without regulatory oversight, and without long-term studies on the health consequences of their modifications. The bioluminescent cats that light up the Shelf so beautifully have a lifespan averaging 7 years — approximately half that of an unmodified domestic cat. Nobody talks about that in the sales pitch.

The deeper ethical question is about consciousness. Cognitive enhancement geneware exists. It is being used. The dogs recovered from the Block 40 operation displayed problem-solving abilities that suggest awareness far beyond normal canine cognition. If a genetically modified animal can understand its own condition — can recognize that it is modified, can experience distress about its modification — at what point does it become wrong to have created it? At what point does a pet become a person?

Meridian 88's legal framework has no answer. The Personhood Amendment (30th Amendment) grants rights to synthetic intelligences but says nothing about genetically enhanced biological organisms. The corporate sovereignty structure means animal welfare legislation can only be enacted within corporate territories, and no corponation has claimed jurisdiction over the pet industry. The result is a legal void in which the creation of potentially sapient beings is governed by nothing more than the informal ethics of a community of unlicensed breeders operating out of Shelf District apartments.

This void will not last. The foxlight population is evolving. The augmented rats are thinking. The mood fish are reading our minds. Sooner or later, something we've made will look at us and ask why. And we will have to have an answer better than "because we could."`,
  related_entities: ["Shelf District", "Meridian 88"],
  credibility: "verified",
  story_hooks: ["The ethical reckoning of unregulated genetic pet creation", "At what point does a pet become a person?", "Legal void around enhanced animal consciousness"],
  tags: ["news", "genetic-hybrid", "pets", "ethics", "consciousness", "geneware", "shelf", "opinion"]
});

// ═══════════════════════════════════════════════════════════════
// SECTION 6: THE BIG WORLDBUILDING DOCUMENTS (2)
// ═══════════════════════════════════════════════════════════════

emit({
  id: genId(),
  name: "The Psionic Question of 2200",
  type: "document",
  document_type: "academic_paper",
  author: "Meridian University Department of Cognitive Sciences — Compiled by Dr. Amara Osei-Strand",
  date: "2200-03-01",
  classification: "public",
  description: `The Psionic Question is, in 2200, the most contentious topic in cognitive science — not because the evidence is overwhelming, but because the evidence is precisely ambiguous enough to prevent resolution. For every documented case of apparent psychokinesis, telepathy, or precognition, there exists a plausible conventional explanation. For every conventional explanation, there exists a detail that doesn't quite fit. The debate has consumed careers, ruined reputations, and generated more heat than light for the better part of three decades.

THE HISTORY: Reports of psionic phenomena are as old as human civilization, but the modern psionic debate begins in 2168, when Dr. Kenji Watanabe-Oduya at the Tokyo Neurological Institute published a paper documenting statistically significant correlations between BCI activity and measurable environmental effects — specifically, the ability of certain BCI-equipped subjects to influence the output of hardware random number generators at rates exceeding chance by 2-3 standard deviations. The Watanabe-Oduya paper was methodologically sound, peer-reviewed, and replicated in four independent laboratories. It was also modest in its claims — it did not use the word "psionic" and attributed the effect to "unknown electromagnetic interaction between neural interface hardware and sensitive electronic equipment."

The word "psionic" was applied by the media, and the debate has been contaminated by sensationalism ever since. Between 2168 and 2200, approximately 200 peer-reviewed studies have examined the question. Roughly 60% find no significant effect. Roughly 30% find small, statistically significant effects that could be attributed to methodological artifacts. Roughly 10% find effects that are large, repeatable, and resistant to conventional explanation. The 10% are the problem.

THE DOCUMENTED CASES: The most compelling documented cases involve individuals with specific BCI configurations — typically NovaMind v5 or later models with deep neural mesh integration — who demonstrate consistent, reproducible effects under controlled laboratory conditions. Case 2187-A (identity protected), a Meridian 88 resident with a NovaMind v6.3 implant, demonstrated the ability to raise the temperature of a sealed, insulated water sample by 0.3°C through focused concentration alone, repeated across 47 consecutive trials with a p-value of less than 0.0001. Case 2192-B demonstrated the ability to influence the movement of a magnetically suspended sphere, producing displacement consistent with a force of approximately 0.01 Newtons — tiny, but measurable, repeatable, and inexplicable.

Case 2195-C is the most controversial. Subject C, a former TESSERA employee with a military-grade BCI, demonstrated apparent telepathic communication — the ability to transmit specific information (numbers, images, words) to a second BCI-equipped individual in a separate, shielded room. The hit rate across 500 trials was 34%, against a chance baseline of 25% for the four-choice protocol used. The effect size is small but significant. Skeptics note that the shielding may have been imperfect. Proponents note that no electromagnetic signal was detected. The debate continues.

THE BCI CONNECTION: The overwhelming majority of documented psionic cases involve BCI-equipped individuals, which creates two competing interpretations. The skeptical interpretation: BCIs are electromagnetic devices implanted in brains. They emit signals. In certain configurations, these signals may interact with sensitive equipment in ways that mimic psionic phenomena. The effect is real but mundane — it is a hardware interaction, not a mind-over-matter phenomenon. The proponent interpretation: the human brain has always had latent psionic capacity, but without amplification, the effects are too small to detect or utilize. BCIs, by enhancing neural signal strength and coherence, amplify the brain's native psionic output to detectable levels. The BCI doesn't create the effect — it reveals it.

CORPORATE INTEREST: The corponations have noticed. Arcturus Defense maintains a classified research program (designation unknown, existence confirmed by three independent leaks) focused on what internal documents reportedly call "directed neural influence" — the weaponization of BCI-mediated psionic effects, if they exist. TESSERA's research division has published six papers on "neural field theory" that dance around the psionic question without using the word. Crucible Industries has filed fourteen patents related to "remote neural interaction" that describe, in technical language, exactly the capabilities that psionic proponents claim are real.

The corporate interest is telling. Corponations do not invest in fiction. If Arcturus, TESSERA, and Crucible are spending money on psionic research, it is because their internal data suggests there is something to research. What they have found, they are not sharing.

THE SOCIAL DIMENSION: Individuals who self-identify as "psi-positive" — claiming to experience psionic phenomena — face significant social stigma. A 2199 survey found that 67% of Meridian 88 residents would be "uncomfortable" living next to someone who claimed psionic abilities. Employment discrimination against self-identified psi-positives is documented but not illegal — psionic ability is not a protected class under any corporate charter or the remnant federal framework. Psi-positive support communities exist, primarily in the Shelf and the Underworld, where social marginalization is less consequential because everyone is already marginal.

The stigma creates a reporting problem. If experiencing psionic phenomena means social ostracism, job loss, and potential involuntary commitment for psychiatric evaluation, rational individuals will not report their experiences. The documented cases are, by definition, the cases that survived the reporting filter — the experiences dramatic enough, or the individuals brave enough, to withstand the consequences of disclosure. How many cases go unreported is unknowable but likely significant.

GOVERNMENT PROGRAMS: The Federal Remnant's intelligence services operated a classified psionic research program from 2171 to 2188, designated LOOKING GLASS, which was partially declassified in 2196. The declassified documents reveal a 17-year program that tested over 400 BCI-equipped individuals for psionic capability. The results are ambiguous — the declassified summary states that "certain subjects demonstrated capabilities that exceeded statistical expectation" but does not elaborate. The program was terminated in 2188 for "budgetary reasons." Three former LOOKING GLASS researchers have publicly stated that the real reason for termination was that the program found something, and what it found frightened the people who had authorized the search.

BCI-AMPLIFIED PSIONIC THEORY: The most sophisticated theoretical framework for psionic phenomena was proposed in 2197 by Dr. Lena Okonkwo-Strand (Meridian University) and is known as Neural Field Extension Theory (NFET). NFET proposes that consciousness generates a measurable field — not electromagnetic, not gravitational, but a novel fundamental interaction that has gone undetected because its effects are vanishingly small under normal conditions. BCI implants, by synchronizing and amplifying neural activity, increase the field's strength to the point of observable interaction with physical systems.

NFET makes specific, testable predictions. It predicts that psionic effects should scale with neural coherence (supported by data). It predicts that effects should be strongest in individuals with deep neural mesh integration (supported by data). It predicts that proximity to other BCI-equipped individuals should increase effect strength through field superposition (tested once, inconclusive). And it predicts the existence of a "psionic spectrum" — that all BCI-equipped individuals produce the field, but only those above a certain threshold can produce observable effects.

The theory is elegant, internally consistent, and completely unproven. It may describe reality. It may describe a beautiful fantasy. In 2200, we cannot tell the difference. That is the psionic question.`,
  related_entities: ["Meridian University", "NovaMind", "Arcturus Defense", "TESSERA", "Crucible Industries", "Federal Remnant"],
  credibility: "verified",
  story_hooks: ["The psionic debate is perfectly ambiguous by design", "Corponations are investing real money in psionic research", "LOOKING GLASS found something and was shut down", "Psi-positive individuals face discrimination", "BCIs may be amplifying latent human abilities"],
  tags: ["paranormal", "psionics", "BCI", "academic", "worldbuilding", "consciousness", "corporate-research", "stigma"]
});

emit({
  id: genId(),
  name: "The Mutants of 2200",
  type: "document",
  document_type: "academic_paper",
  author: "Meridian University Department of Genetics — Compiled by Dr. Kwame Abara-Petrov",
  date: "2200-02-15",
  classification: "public",
  description: `Mutation, in 2200, is not what it was. For most of human history, mutation was a slow, invisible process — random errors in DNA replication, measured in generations, producing variation that natural selection acted upon over millennia. In Meridian 88, mutation is fast, visible, and terrifyingly common. The city's population exists at the intersection of four mutagenic forces — chemical contamination, geneware residue, radiation exposure, and the Underworld's unknown transformative influence — and the results are reshaping the human genome in real time.

CHEMICAL CONTAMINATION: Meridian 88 was built on and around industrial infrastructure dating back to the early 21st century. The soil, water, and atmospheric chemistry of the city contain hundreds of known mutagenic compounds — heavy metals, polycyclic aromatic hydrocarbons, halogenated solvents, and degradation products of industrial processes that ceased operating decades ago but left their chemical signature in the environment. Shelf District residents show baseline mutation rates approximately 4.7 times the global average, a figure that has been stable since measurement began in 2165. For Underworld residents, the rate is estimated at 8-12 times the global average, though accurate measurement is complicated by the population's limited access to medical screening.

The chemical mutations are typically subtle — variations in enzyme expression, altered metabolic pathways, minor structural differences in protein folding. Most are neutral or mildly detrimental. Some are lethal — childhood cancer rates in the Shelf District are 2.3 times the Meridian 88 average. A few are potentially beneficial — a documented sub-population in the Gulch District displays enhanced heavy-metal tolerance that appears to be an adaptive mutation to their contaminated water supply. They are, in a very real sense, evolving to survive their environment.

GENEWARE RESIDUE: Geneware — genetic modification technology that allows targeted editing of the human genome — has been commercially available since 2130. It is used for medical treatment, cosmetic enhancement, performance augmentation, and recreational body modification. It is also imperfect. Geneware sequences are designed to modify specific genes, but in practice, off-target effects are common. The modification hits its target and also hits other genes, producing unintended changes that may not manifest for years or generations.

The mutagenic legacy of geneware is generational. A parent who undergoes geneware modification may pass the intended modification to their children — but may also pass off-target modifications that they themselves never experienced because the effects were recessive or late-onset. The children of geneware users display mutation rates approximately 2.1 times higher than children of non-users, and the mutations they display are qualitatively different from chemical mutations — more structured, more dramatic, and occasionally exhibiting features that appear designed even though they were not.

This is the uncanny valley of geneware mutation: modifications that look intentional but aren't. A child born with chromatophore cells that allow limited skin color change — not because anyone ordered that modification, but because a cosmetic geneware treatment their grandmother received interacted with an off-target edit in a way that activated dormant cephalopod gene sequences that exist, vestigially, in the human genome. The result looks like someone designed a chameleon person. Nobody did. The genome did it on its own, using tools that geneware left behind.

RADIATION EXPOSURE: Meridian 88's energy infrastructure includes three fusion reactors (operated by Ouroboros Energy), seventeen fission micro-reactors (scattered across industrial districts), and an unknown number of decommissioned nuclear facilities from the city's construction era. Radiation exposure above background levels is a documented reality for approximately 15% of the city's population, concentrated in industrial workers, energy sector employees, and residents of districts adjacent to power generation facilities.

Radiation-induced mutations follow established patterns — increased rates of chromosomal aberration, point mutations, and structural variants. What is less established is the interaction between radiation exposure and geneware residue. Preliminary research suggests that radiation may "activate" dormant geneware sequences — modifications that were quiescent in the genome suddenly expressing under radiological stress. This mechanism could explain the occasional appearance of complex, structured mutations in populations with both radiation exposure and geneware heritage — mutations that are too organized to be random radiation damage and too unexpected to be intentional geneware design.

THE UNDERWORLD FACTOR: The Underworld's mutagenic influence is the least understood and most dramatic of the four forces. Below Level B30, mutation rates increase sharply — not linearly with depth, but exponentially, suggesting a mutagenic source that intensifies dramatically at extreme depth. The nature of this source is unknown. Chemical contamination explains some of it. The Underworld Hum — a persistent infrasonic vibration whose amplitude increases with depth — may contribute through mechanisms not currently understood. And there may be additional factors that we simply have not identified.

Underworld mutations are different from surface mutations in character as well as frequency. They tend toward radical morphological change rather than subtle metabolic variation. Documented Underworld mutations include: dermal bioluminescence (the production of light by skin cells), echolocation capability (documented in at least three individuals living below B40), radical skeletal restructuring (including additional digits, altered joint configurations, and modified cranial structure), and sensory expansions that allow perception of electromagnetic frequencies, chemical gradients, and vibrational patterns outside normal human range.

The Underworld mutations raise the question that nobody in an official capacity wants to ask: is the Underworld changing people on purpose? The mutations are not random — they are adaptive. Bioluminescence is useful in darkness. Echolocation is useful without light. Enhanced chemical sensitivity is useful in a contaminated environment. If you were designing a human to survive in the Underworld, you would design something very like what the Underworld is producing. The conventional explanation is convergent evolution — random mutations being selected for by environmental pressure. The unconventional explanation is that something in the Underworld is actively editing the genomes of people who spend time in its depths. We have no evidence for the unconventional explanation. We also have no explanation for the speed and specificity of the conventional one.

SOCIAL STIGMA AND COMMUNITY: Visible mutation carries severe social stigma in Meridian 88, particularly in the Spires and the middle-tier corporate residential districts. "Mutant" is a slur. Employment discrimination is pervasive — a 2199 study found that visibly mutated job applicants received callback rates 84% lower than non-mutated applicants with identical qualifications. Housing discrimination is similarly documented. Social exclusion pushes mutated individuals toward the Shelf and the Underworld, which further increases their exposure to mutagenic conditions, creating a feedback loop of marginalization and biological change.

In the Shelf and the Underworld, communities of visibly mutated individuals have formed — not by choice, but by the social gravity of shared exclusion. The largest is the Gulch's "Changelings" — a community of approximately 500 individuals with visible mutations who have created a mutual support network, a shared cultural identity, and an increasingly vocal political movement demanding recognition and protection under Meridian 88's legal framework. The Changelings reject the word "mutant" and use "changed" or "adapted" as preferred terminology. Their political platform is simple: their mutations are not diseases. Their mutations are not deformities. Their mutations are adaptations to the environment that Meridian 88 created, and the city that made them this way has an obligation to make room for what they've become.

CORPORATE EXPLOITATION: The corponations view mutation through the lens of utility. Helix Biosystems has an active recruitment program for individuals with specific mutations that could inform geneware product development — offering medical care, housing, and Φ-denominated stipends in exchange for biological samples and participation in long-term studies. Arcturus Defense has been documented offering enhanced employment contracts to individuals with mutations that confer combat-relevant advantages — enhanced strength, speed, sensory perception, or durability. Crucible Industries holds patents on several "mutation stabilization" techniques that, critics argue, are essentially processes for harvesting and replicating naturally occurring mutations for commercial application.

The exploitation is subtle but pervasive. Mutated individuals in the Shelf and Underworld — already marginalized, already economically precarious — are offered resources they desperately need in exchange for access to their biology. The transactions are nominally voluntary. The power imbalance makes the concept of voluntary consent approximately as meaningful as the consent checkbox on a BCI terms-of-service agreement.

THE QUESTION OF THE FUTURE: Meridian 88's population is changing. Not in the slow, generational timeframe of historical evolution, but in the fast, visible timeframe of a species under extreme environmental pressure with access to genetic modification technology and exposure to mutagenic forces that are, at best, poorly understood. The mutations are accelerating. The Underworld's influence is deepening. The geneware legacy is compounding with each generation. And the corponations are mining the results for profit.

What Meridian 88's population will look like in 2250 — in just two more generations — is an open question. The optimistic answer is a more diverse, more adapted, more resilient human population that has incorporated the best of what mutation and modification have to offer. The pessimistic answer is a fragmented species, divided by biology as well as economics, with the wealthy preserving baseline human genetics through expensive screening and the poor absorbing whatever the environment and the corporations do to their genomes.

The realistic answer is probably both, simultaneously, in the same city, on different floors.`,
  related_entities: ["Meridian University", "Helix Biosystems", "Arcturus Defense", "Crucible Industries", "Ouroboros Energy", "Shelf District", "Underworld", "The Gulch", "Changelings"],
  credibility: "verified",
  story_hooks: ["Four mutagenic forces reshaping humanity", "The Underworld may be deliberately modifying people", "Changeling community organizing for rights", "Corporate exploitation of mutated individuals", "Geneware residue producing unintended generational mutations"],
  tags: ["paranormal", "mutation", "geneware", "underworld", "academic", "worldbuilding", "social-justice", "corporate-exploitation", "evolution"]
});

// ═══════════════════════════════════════════════════════════════
// SUMMARY
// ═══════════════════════════════════════════════════════════════

console.log(`\nDone. Created: ${written}, Skipped: ${skipped}, Total documents: ${written + skipped}`);
