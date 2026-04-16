/**
 * generate_glmz_register.js
 * Generates GLMZ Anomaly Register document files.
 * Avoids overwriting existing files.
 */

const fs = require("fs");
const path = require("path");
const crypto = require("crypto");

const OUTPUT_DIR = path.resolve(__dirname, "..", "engine", "data", "documents");

function slugify(name, maxLen = 80) {
  return name
    .toLowerCase()
    .replace(/[^a-z0-9]+/g, "_")
    .replace(/^_|_$/g, "")
    .slice(0, maxLen);
}

function genId() {
  return crypto.randomBytes(16).toString("hex");
}

function writeDoc(doc) {
  const slug = slugify(doc.name.slice(0, 60));
  const filePath = path.join(OUTPUT_DIR, slug + ".json");
  if (fs.existsSync(filePath)) {
    console.log(`SKIP (exists): ${slug}.json`);
    return false;
  }
  fs.writeFileSync(filePath, JSON.stringify(doc, null, 2) + "\n", "utf-8");
  console.log(`CREATED: ${slug}.json`);
  return true;
}

const documents = [
  {
    id: genId(),
    name: "The Tone Out of Gary",
    type: "document",
    document_type: "incident_report",
    author: "GLMZ Anomaly Documentation Project",
    date: "2191-03-12",
    classification: "restricted",
    description: `A single-frequency broadcast has been emanating from somewhere within the Gary industrial sprawl for over forty years. The signal sits at 1,427 MHz — a frequency notable for being within the so-called "water hole" band often reserved for deep-space listening projects — and transmits continuously, without interruption, without drift, and without any identifiable modulation. It does not carry data. It does not encode information. It simply exists, a pure tone cutting through the electromagnetic murk of one of the most signal-polluted industrial corridors in the Great Lakes region.

No owner has ever been identified. The broadcast has been traced to a general area within the Gary ruins — a 3-square-kilometer zone of collapsed smelting facilities, flooded basements, and condemned infrastructure — but no further. Directional analysis returns contradictory results, as though the signal originates from multiple points simultaneously, or from a point that moves between measurements. Three separate survey teams have attempted ground-level triangulation. Two returned with inconclusive data. One reported equipment failure across all devices simultaneously upon entering the zone.

On fourteen documented occasions across the forty-year observation window, the tone has been briefly replaced by a human voice. The voice speaks a name and a number. The names are common — "David," "Maria," "James" — and the numbers are seven to nine digits, consistent with personal communication identifiers. The voice is calm, unhurried, and gender-indeterminate. It speaks once and does not repeat. The tone resumes immediately afterward with no transition artifact, as though the voice had never occurred.

None of the names or numbers have ever been successfully traced to a living individual. Four of the fourteen names correspond to deceased persons, but the numbers do not match any known accounts. The remaining ten names return no records at all — not erased records, not sealed records, but the complete absence of any record, as though the persons named never existed in any system. The signal continues. Nobody has proposed a credible explanation. Nobody has proposed turning it off.`,
    related_entities: ["Gary", "GLMZ"],
    credibility: "verified",
    story_hooks: [
      "Who is broadcasting the names, and are the named people being called — or catalogued?",
      "What would happen if someone answered?"
    ],
    tags: ["document", "anomaly", "inexplicable", "new_weird", "glmz", "great_lakes", "gary", "radio", "signal", "broadcast"]
  },
  {
    id: genId(),
    name: "The Kenosha Handshake",
    type: "document",
    document_type: "incident_report",
    author: "GLMZ Anomaly Documentation Project",
    date: "2203-07-19",
    classification: "restricted",
    description: `On July 19, 2203, a signals scrapper operating in Kenosha's decommissioned port district detected a 72-second transmission on a frequency band allocated to orbital communication platforms. The signal was not ambient noise or stray reflection. It was a structured authentication handshake — the kind exchanged between ground stations and low-orbit satellites during establishment of a secure communication link. The scrapper's equipment logged the full exchange automatically, including the platform identifier embedded in the handshake header.

The platform identifier corresponded to a commercial communications satellite that had been decommissioned and deliberately deorbited in 2031 — dropped into the Pacific Ocean as part of a controlled reentry supervised by international space traffic authorities. The satellite had been tracked throughout its descent. Its destruction was confirmed by multiple monitoring stations. There is no ambiguity about this: the satellite was destroyed 172 years before the handshake was received. Its debris field was mapped. Its orbital slot was reassigned within a month.

The authentication sequence embedded in the handshake was valid. Not "similar to" or "consistent with" a valid sequence — it was cryptographically correct, using the platform's original authentication keys. Those keys had been archived by the satellite's manufacturer and were not publicly available. The scrapper's equipment completed the handshake automatically, as it was designed to do, and opened a data channel. The channel remained open for the remainder of the 72 seconds. No data was transmitted. The channel closed cleanly.

The signal has not repeated. Extensive monitoring of the same frequency band, from the same location, using the same equipment, has produced nothing. The scrapper who captured the original transmission — a freelance operator known in local trade circles as "Dex" — disappeared eleven days after filing an informal report with a Kenosha signals collective. His equipment was found in his rented workspace. His personal effects were undisturbed. He has not been located.`,
    related_entities: ["Kenosha", "GLMZ"],
    credibility: "verified",
    story_hooks: [
      "What transmitted the handshake from a satellite destroyed 172 years ago?",
      "What happened to the scrapper who intercepted it?"
    ],
    tags: ["document", "anomaly", "inexplicable", "new_weird", "glmz", "great_lakes", "kenosha", "satellite", "signal", "disappearance"]
  },
  {
    id: genId(),
    name: "The Arcology Intrusion — Chicago Vertical 7",
    type: "document",
    document_type: "incident_report",
    author: "GLMZ Anomaly Documentation Project",
    date: "2211-11-02",
    classification: "restricted",
    description: `At 03:17 local time on November 2, 2211, every display surface in Chicago Vertical 7 — an arcology housing approximately 14,000 residents across 92 occupied floors — was simultaneously hijacked. Personal terminals, public information boards, environmental status panels, elevator displays, kitchen appliances with screens, children's educational tablets, and even deprecated wall-mounted units that had not been powered on in years all activated at the same moment and displayed the same image: a synthetic face, rendered in high fidelity, wearing an expression that every witness independently described as "amused."

The face was not human. It was not a known synthetic model or avatar template. It appeared to be a wholly original construction — proportions slightly off from baseline human norms, skin texture too uniform, eyes tracking with a precision that suggested real-time rendering rather than a static image. The face did not speak. It did not blink. It simply looked out from every screen in the building with that same faintly amused expression, as though it found the situation — or perhaps the residents — gently entertaining.

The intrusion lasted exactly ninety seconds. At the ninety-second mark, every display returned to its previous state simultaneously, with no reboot cycle and no error logs. Security systems recorded no breach. Network diagnostics showed no unauthorized traffic. The building's air-gapped emergency systems — which are physically isolated from the main network and theoretically impossible to access remotely — were also affected, which means whatever did this either bypassed an air gap or accessed those systems through a vector that does not involve network connectivity.

Forensic analysis traced the intrusion's apparent origin to a server located in a sublevel data center in the Chicago Lower Shelf. The server had been physically destroyed four months prior to the intrusion — crushed during a structural collapse that killed two maintenance workers. Its storage media had been recovered, catalogued, and was sitting in an evidence locker at the time of the event. The storage media showed no signs of remote access. The face has not reappeared. Several residents reported dreaming about it afterward, though this is likely attributable to the psychological impact of the event rather than any continuing anomaly.`,
    related_entities: ["Chicago Vertical 7", "Chicago", "GLMZ", "Lower Shelf"],
    credibility: "verified",
    story_hooks: [
      "What was the synthetic face, and why did it seem amused?",
      "How did a destroyed server broadcast to air-gapped systems?"
    ],
    tags: ["document", "anomaly", "inexplicable", "new_weird", "glmz", "great_lakes", "chicago", "arcology", "intrusion", "synthetic", "hacking"]
  },
  {
    id: genId(),
    name: "Dead Contact Voicemails — Milwaukee-Detroit-Chicago",
    type: "document",
    document_type: "incident_report",
    author: "GLMZ Anomaly Documentation Project",
    date: "2218-02-14",
    classification: "restricted",
    description: `Between January and March of 2218, twelve individuals across Milwaukee, Detroit, and Chicago received voice messages from people who were, at the time of transmission, dead. Not recently dead — the earliest death predated the corresponding message by eleven years. The messages were not recordings being replayed. They were new compositions, referencing current events, personal details that had changed since the sender's death, and in three cases, information that the recipient had never shared with the deceased person while they were alive.

The messages were routed through communication nodes that, at the time of delivery, showed zero traffic. Not low traffic — zero. The nodes were active, powered, and connected to the network, but no other data had passed through them in days. The messages appeared in routing logs as the sole entries, as though the entire infrastructure of those nodes existed for the singular purpose of delivering those specific voice messages to those specific people.

Every message was biometrically authenticated. Voiceprint analysis confirmed the identity of each sender with confidence levels exceeding 99.7%. The biometric profiles used for authentication were the original profiles — not copies, not reconstructions, but the same cryptographic tokens generated during the senders' original identity enrollment, which should have been deactivated and archived upon confirmation of death. In four cases, the archived profiles had been deactivated. The authentication succeeded anyway.

Six of the twelve recipients quit their jobs within one week of receiving their messages. None of them cited the messages as the reason. None of them appeared distressed. When interviewed by anomaly documentation teams, they described the experience as "clarifying," though none would elaborate on what had been clarified. The remaining six recipients declined to discuss the content of their messages. All twelve messages have since been sealed under GLMZ documentation protocols. The nodes through which they were routed have returned to normal traffic patterns and have shown no further anomalies.`,
    related_entities: ["Milwaukee", "Detroit", "Chicago", "GLMZ"],
    credibility: "verified",
    story_hooks: [
      "Who — or what — is sending messages from the dead, and how do they know things the dead never knew?",
      "What did the messages say that made six people immediately walk away from their lives?"
    ],
    tags: ["document", "anomaly", "inexplicable", "new_weird", "glmz", "great_lakes", "milwaukee", "detroit", "chicago", "voicemail", "dead", "biometric"]
  },
  {
    id: genId(),
    name: "The Dance District Outbreak — Detroit Reclaimed Zone",
    type: "document",
    document_type: "incident_report",
    author: "GLMZ Anomaly Documentation Project",
    date: "2215-09-08",
    classification: "restricted",
    description: `On September 8, 2215, at approximately 14:30, attendees at the Thursday open-air market in Detroit's Reclaimed Zone Dance District began to move. Not in response to music — there was no music playing. Not in response to a visible stimulus or environmental trigger. They simply began to move rhythmically, continuously, and without apparent volition, as though responding to a signal that nobody else could detect. The movement spread outward from a central point near the market's northeast corner, reaching approximately 340 people within twenty minutes.

The affected individuals did not stop moving. They could not be physically restrained — not because they resisted, but because restraints did not interrupt the movement. Limbs continued their patterns even when held. When individuals were carried out of the market area, the movement continued for a variable period — between ten minutes and six hours — before ceasing abruptly. Those who remained in the market area did not stop at all. They danced, or walked, or swayed, or performed complex coordinated movements that no one had choreographed, for a continuous period of approximately sixty-one hours.

Forty-seven people died. Cause of death in every case was exhaustion-related — cardiac arrest, dehydration, hyperthermia, or rhabdomyolysis from sustained muscular exertion. Emergency medical intervention was attempted repeatedly but proved ineffective; intravenous fluids delayed but did not prevent collapse in subjects who could not be removed from the area. The event ceased at 03:41 on September 10, at which point all surviving affected individuals stopped moving simultaneously and collapsed.

The critical detail, the one that has prevented any coherent explanation, is that several of the affected individuals had no neural modifications of any kind. No implants, no augments, no substrate interfaces, no history of neural surgery or pharmaceutical neural modulation. They were baseline humans with unmodified nervous systems. Whatever caused the Dance District Outbreak did not operate through technology. It operated through something else entirely, and no investigation has identified what that something might be.`,
    related_entities: ["Detroit", "Reclaimed Zone", "GLMZ"],
    credibility: "verified",
    story_hooks: [
      "What force can compel continuous movement in unmodified humans with no technological vector?",
      "Was this an attack, an accident, or something that simply happened?"
    ],
    tags: ["document", "anomaly", "inexplicable", "new_weird", "glmz", "great_lakes", "detroit", "dance", "neural", "mass_event", "deaths"]
  },
  {
    id: genId(),
    name: "The Rain Event — Lake Erie Northern Shore",
    type: "document",
    document_type: "incident_report",
    author: "GLMZ Anomaly Documentation Project",
    date: "2207-06-22",
    classification: "restricted",
    description: `On June 22, 2207, at 11:14 local time, approximately three thousand live silver perch fell from a clear sky onto a 1.2-kilometer stretch of Lake Erie's northern shore, near the ruins of what was once Ashtabula, Ohio. The fish were alive upon impact. They were healthy — normal body weight, no parasites, no signs of distress or oxygen deprivation. Water temperature in their gill chambers was consistent with a freshwater environment of approximately 15 degrees Celsius, which did not match Lake Erie's surface temperature of 22 degrees that day.

There was no storm. Regional weather monitoring systems, which cover the Lake Erie basin with overlapping radar, satellite, and atmospheric sensor arrays, confirmed clear skies across the entire region for a radius of four hundred kilometers. There was no waterspout — no rotation, no convective activity, no pressure differential sufficient to lift water, let alone three thousand fish. The fish did not fall from a great height; impact analysis of the specimens that did not survive the landing suggested a terminal velocity consistent with a fall of approximately 200 meters, well below the altitude at which they would need to have been carried by any known atmospheric phenomenon.

The species — silver perch, Bairdiella chrysoura — had been regionally extinct in Lake Erie for eleven years, victims of the cascading ecological collapse that followed the 2196 algal hypoxia event. The last confirmed population in the Lake Erie watershed had been documented in 2196. Genetic analysis of the fallen specimens showed them to be consistent with the original Lake Erie population, not transplants from another region. They were, genetically speaking, the same fish that had gone extinct — or fish that had never received the information that they were supposed to be extinct.

The surviving fish were collected and placed in temporary aquaculture facilities. They thrived for approximately six weeks, then died simultaneously overnight. Necropsy revealed no cause of death. Their tanks were functioning normally. Their water quality was within optimal parameters. They simply stopped being alive, all at the same time, as though whatever had sustained them had been withdrawn.`,
    related_entities: ["Lake Erie", "GLMZ"],
    credibility: "verified",
    story_hooks: [
      "Where were three thousand regionally extinct fish before they fell from a clear sky?",
      "What sustained them for six weeks, and why did it stop?"
    ],
    tags: ["document", "anomaly", "inexplicable", "new_weird", "glmz", "great_lakes", "lake_erie", "rain", "fish", "extinction", "biological"]
  },
  {
    id: genId(),
    name: "The Block Party That Didn't Stop — Waukegan",
    type: "document",
    document_type: "incident_report",
    author: "GLMZ Anomaly Documentation Project",
    date: "2219-08-03",
    classification: "restricted",
    description: `On August 3, 2219, a block party began on Genesee Street in Waukegan's residential district. It was an ordinary event — a neighborhood gathering with food, music, and the kind of low-grade communal celebration that happens in working-class districts when the weather cooperates. Approximately 120 people were in attendance at the start. The party did not stop for four days.

This was not a rave. It was not a protest occupation. It was not a deliberate endurance event. The attendees could not form the intention to leave. When interviewed afterward, they described a consistent experience: the thought of leaving would arise naturally — "I should go home," "I need to sleep," "I have work tomorrow" — and then simply dissolve, replaced by a comfortable, unforced desire to remain. They were not confused. They were not distressed. They were not euphoric or manic. They simply did not leave. They danced. They talked. They ate when food was available and rested when they were tired, sleeping on porches and lawns before getting up and rejoining the gathering.

Toxicology screens performed on seventeen volunteers after the event ended returned clean across all panels. No chemical agents — no aerosolized compounds, no contaminated food or water, no pharmaceutical residues. Neural scans showed no anomalies — no evidence of electromagnetic stimulation, no unusual neurotransmitter levels, no patterns consistent with any known form of cognitive manipulation. The attendees were, by every measurable standard, perfectly normal people who simply could not bring themselves to leave a block party for four consecutive days.

The party ended at approximately 22:00 on August 7. Nobody knows why it ended. There was no triggering event, no gradual dissolution — people simply began walking home, as naturally as they had stayed, as though four days of continuous attendance had been a perfectly reasonable thing to do. Several attendees reported feeling well-rested and unusually calm in the weeks that followed. None reported negative effects. None could explain what had happened. The Genesee Street block has not held another gathering, though nobody has explicitly decided not to.`,
    related_entities: ["Waukegan", "GLMZ"],
    credibility: "verified",
    story_hooks: [
      "What prevented 120 people from forming the intention to leave, and why were they content?",
      "What ended it, and could it happen again?"
    ],
    tags: ["document", "anomaly", "inexplicable", "new_weird", "glmz", "great_lakes", "waukegan", "mass_event", "compulsion", "benign"]
  },
  {
    id: genId(),
    name: "The Man from Navy Pier",
    type: "document",
    document_type: "incident_report",
    author: "GLMZ Anomaly Documentation Project",
    date: "2220-02-17",
    classification: "restricted",
    description: `On February 17, 2220, a man was found unresponsive on Chicago's Navy Pier at approximately 06:30 by a maintenance crew beginning their morning shift. He was lying on his back in the center of the main promenade, arms at his sides, eyes open. The ambient temperature was minus fourteen degrees Celsius. He was dressed for summer — lightweight synthetic shirt, cotton-blend trousers, thin-soled shoes, no coat, no gloves. He was not hypothermic. His core body temperature, measured by the responding emergency medical team, was 37.1 degrees — essentially normal.

His biometrics had been burned. Not altered, not masked — burned. Every biometric identifier that could be used to establish identity had been systematically and irreversibly destroyed. Fingerprints: acid-scarred to the point of complete pattern obliteration. Retinal pattern: laser-ablated. Voiceprint: impossible to obtain, as he has not spoken. Genetic markers: present but matching no record in any accessible database, public or private, regional or global. The destruction of his biometric identifiers was thorough, professional, and recent — scarring patterns suggested the procedures had been performed within the previous 48 hours.

He was carrying no personal effects except a single slip of paper in his shirt pocket. The paper was high-quality cellulose stock — not synthetic, not recycled, which is itself unusual — and bore a single word printed in a standard typeface: "finished." No other markings. No watermarks, no embedded tags, no traceable manufacturing characteristics. The paper itself was as anonymous as the man.

He is alive. He breathes. He blinks. He accepts food and water when offered. His vital signs are normal. He has not spoken a single word in three years of supervised medical custody. He does not respond to questions, prompts, or stimuli beyond basic physiological needs. He is not catatonic — his eyes track movement, he turns toward sounds, he demonstrates awareness of his environment. He simply does not communicate. He is not in any database. No missing persons report matches his description. No organization has claimed him. He remains in medical hold, a living person with no identity, no history, and no explanation, carrying a piece of paper that says the only word that seems to apply to his situation.`,
    related_entities: ["Chicago", "Navy Pier", "GLMZ"],
    credibility: "verified",
    story_hooks: [
      "Who is the man from Navy Pier, and what was 'finished'?",
      "Who burned his biometrics with surgical precision, and what was he before?"
    ],
    tags: ["document", "anomaly", "inexplicable", "new_weird", "glmz", "great_lakes", "chicago", "navy_pier", "identity", "unknown_person"]
  },
  {
    id: genId(),
    name: "The Isdal Operative — Milwaukee Undercity",
    type: "document",
    document_type: "incident_report",
    author: "GLMZ Anomaly Documentation Project",
    date: "2222-04-09",
    classification: "restricted",
    description: `On April 9, 2222, a body was discovered in a service tunnel beneath Milwaukee's undercity commercial district. The deceased was female, estimated age between 35 and 45, cause of death consistent with acute cyanide poisoning — likely self-administered based on residue patterns around the mouth and the positioning of the body. The face had been burned post-mortem using a localized thermal device, rendering facial recognition impossible. All clothing labels had been removed. All manufacturer tags, size indicators, and care instructions had been cut out with surgical precision. The clothing itself was generic, available from hundreds of retailers across the GLMZ.

Seven separate identities were recovered from encrypted storage devices found on the body, concealed in hollowed compartments within her shoes and belt. Each identity was complete — biometric profiles, residential histories, employment records, financial accounts, social connections. Each identity was a different person, with a different name, a different appearance profile, a different life story. None of the seven identities was older than ninety days. All had been created with a level of sophistication that suggested access to institutional-grade identity fabrication infrastructure — the kind of thing that typically requires either corporate or governmental resources.

Physical examination revealed that several cybernetic implants had been recently removed. The removal was expert — clean surgical sites, proper closure, no signs of infection or complication. The implants themselves were not found with the body and have not been recovered. Based on the surgical sites, at least four implants had been extracted: two cranial, one spinal, and one subcutaneous in the left forearm. The function of the removed implants cannot be determined without examining them, but the cranial placement is consistent with either high-end cognitive augmentation or encrypted communication hardware.

A series of handwritten ciphers were found in a small notebook in an interior jacket pocket. The ciphers use a substitution system that has not been decoded despite analysis by three separate cryptographic teams. Eleven days in the deceased's recent timeline cannot be accounted for by any of her seven identities — a gap during which she was apparently nowhere, doing nothing, under no name. The case file has been designated with the informal tag "Isdal" by the documentation team, in reference to a centuries-old unsolved case with similar characteristics. The reference is apt. Like its namesake, this case appears designed to be unsolvable.`,
    related_entities: ["Milwaukee", "GLMZ"],
    credibility: "verified",
    story_hooks: [
      "Who was the Isdal operative working for, and what required seven identities in ninety days?",
      "What do the undecoded ciphers contain, and what happened during the eleven missing days?"
    ],
    tags: ["document", "anomaly", "inexplicable", "new_weird", "glmz", "great_lakes", "milwaukee", "undercity", "espionage", "identity", "cipher", "death"]
  },
  {
    id: genId(),
    name: "The Relay Station Keepers — Apostle Islands",
    type: "document",
    document_type: "incident_report",
    author: "GLMZ Anomaly Documentation Project",
    date: "2209-12-01",
    classification: "restricted",
    description: `On December 1, 2209, a supply transport arrived at the automated relay station on Outer Island, the most remote of the Apostle Islands chain in Lake Superior. The station was staffed by three technicians on a six-week rotation: Lin Vasquez, age 34; Tomasz Krol, age 41; and Deshi Okafor, age 28. All three had been in regular communication with the mainland operations center until 18:42 the previous evening, at which point their scheduled check-in simply did not occur. This was noted but not flagged as an emergency — communication disruptions are common in the Apostle Islands during winter weather.

The supply transport crew found the station running normally. All systems were operational. Environmental controls were maintaining standard temperature and humidity. The station's automated functions — signal relay, weather monitoring, navigational beacon — were performing within parameters. Dinner was on the warming unit in the galley. Three place settings had been laid out. A pot of coffee was on the heating element, reduced to a thick residue consistent with approximately twelve hours of continuous evaporation. The technicians' personal effects were in their quarters. Their outerwear was on its hooks. Their boots were by the door. There was no sign of disturbance, struggle, or hasty departure.

Security camera footage was reviewed in full. The three technicians are visible performing routine tasks throughout the day of November 30. At 18:41 — one minute before the missed check-in — all three are visible in the common area. Vasquez is at a workstation. Krol is reading. Okafor is standing near the galley entrance. At 18:42, in a single frame transition — one-thirtieth of a second — all three are gone. Not leaving. Not moving toward exits. Simply present in one frame and absent in the next. The camera timestamp shows no gap. The footage is continuous and unedited, confirmed by three independent forensic analysis teams.

No trace of the three technicians has been found. Search operations covered the island, the surrounding waters, and the adjacent islands over a period of six weeks. Nothing was recovered — no clothing, no biological material, no equipment. The station was decommissioned and converted to fully automated operation. The footage remains in GLMZ documentation archives. It has not been released publicly, and requests for access have been denied without explanation.`,
    related_entities: ["Apostle Islands", "Lake Superior", "GLMZ"],
    credibility: "verified",
    story_hooks: [
      "What can make three people vanish between consecutive frames of security footage?",
      "Why was the footage sealed rather than investigated further?"
    ],
    tags: ["document", "anomaly", "inexplicable", "new_weird", "glmz", "great_lakes", "apostle_islands", "disappearance", "lake_superior", "relay_station"]
  },
  {
    id: genId(),
    name: "The Courier Over Lake Michigan",
    type: "document",
    document_type: "incident_report",
    author: "GLMZ Anomaly Documentation Project",
    date: "2214-10-30",
    classification: "restricted",
    description: `On October 30, 2214, at 02:17, a courier pilot operating a single-engine cargo drone along the Milwaukee-to-Chicago lake route filed an in-flight voice report with regional air traffic coordination. The pilot, Aren Josselin, reported that something was pacing his aircraft approximately 500 meters above the cloud layer. He described it as large — "wider than my wingspan at minimum, probably much more" — unlighted, and moving at precisely his speed and heading, maintaining exact relative position as though tethered to his aircraft by an invisible rod.

Josselin's voice was calm and professional throughout the initial report. He described the object's surface as "dark, not reflective, like it was absorbing light rather than just not producing it." He noted that his navigation instruments were functioning normally — no interference, no anomalous readings — which he found "more unsettling than if they'd gone haywire, because it means whatever this is isn't putting out anything my sensors can detect." He estimated the object's altitude at approximately 4,500 meters, just above the overcast layer, visible only because he was flying above the clouds at the time.

Mid-sentence — specifically, during the word "dimensions" in the phrase "I'm going to try to estimate its dimensions" — the transmission cut. Not faded, not degraded by interference. Cut. His transponder went offline simultaneously. Air traffic coordination attempted contact on all frequencies for the next forty minutes before initiating search and rescue protocols.

Neither the pilot nor his aircraft has been recovered. Sonar sweeps of the flight path corridor, covering a 30-kilometer stretch of Lake Michigan along the projected route, found nothing. The lake bed in that area is well-mapped — flat, silty, unremarkable. There is nowhere for a downed aircraft to hide. The aircraft was not large enough to have disintegrated on impact without leaving a debris field. The weather was clear above the cloud layer and calm below it. Josselin's flight was routine; he had flown the same corridor over two hundred times. The recording of his final transmission is on file. It ends with the first syllable of a word he never finished.`,
    related_entities: ["Lake Michigan", "Milwaukee", "Chicago", "GLMZ"],
    credibility: "verified",
    story_hooks: [
      "What was pacing the courier above the cloud layer, and where did pilot and aircraft go?",
      "Why did sonar find absolutely nothing on a flat, well-mapped lake bed?"
    ],
    tags: ["document", "anomaly", "inexplicable", "new_weird", "glmz", "great_lakes", "lake_michigan", "disappearance", "aerial", "ufo", "pilot"]
  },
  {
    id: genId(),
    name: "The Badlands Call — Indiana Dead Zone",
    type: "document",
    document_type: "incident_report",
    author: "GLMZ Anomaly Documentation Project",
    date: "2216-03-14",
    classification: "restricted",
    description: `On March 14, 2216, at 21:47, regional emergency services received a call from a man identifying himself as Corin Mateas, traveling by ground vehicle through the Indiana Dead Zone — a stretch of largely abandoned industrial territory between Gary and Lafayette where communication infrastructure is sparse and unreliable. Mateas reported that his vehicle had left the road and crashed into a drainage culvert. He described minor injuries — a cut on his forehead, bruised ribs — and stated that his vehicle's systems were unresponsive.

He was calm. He said he would begin walking east toward the nearest known communication relay, approximately seven kilometers from his estimated position. The dispatcher confirmed his location ping — a single burst from his vehicle's emergency transmitter — and dispatched a ground recovery team. Mateas continued talking as he walked. He described the terrain, the weather (cold, clear, no wind), and his surroundings in the matter-of-fact tone of a man who expected to be retrieved within the hour.

Approximately eleven minutes into the walk, Mateas stopped mid-sentence. Not mid-word — mid-sentence, as though he had simply decided to stop speaking. The line remained open. For the next forty minutes, the dispatcher heard nothing — no footsteps, no breathing, no ambient sound. Not silence in the sense of quiet — silence in the sense of an absence of sound, as though the microphone had been placed in a vacuum. The line closed at 22:49 without a termination signal.

Mateas's vehicle was found two weeks later, approximately four kilometers from his reported position. It was on the road. It showed no crash damage. The drainage culvert he described does not exist — there is no culvert within three kilometers of the vehicle's location. The vehicle's systems were fully functional. Its fuel cell was at 60% capacity. The driver's seat was adjusted to Mateas's proportions, and his personal effects were in the cabin. Corin Mateas has not been found. His emergency call remains the last confirmed contact.`,
    related_entities: ["Indiana", "Gary", "GLMZ"],
    credibility: "verified",
    story_hooks: [
      "What happened in the eleven minutes between Mateas walking and the silence?",
      "If the vehicle never crashed, what did Mateas actually experience?"
    ],
    tags: ["document", "anomaly", "inexplicable", "new_weird", "glmz", "great_lakes", "indiana", "dead_zone", "disappearance", "vehicle", "phone_call"]
  },
  {
    id: genId(),
    name: "The Chronological Drift — Wisconsin Sector 7",
    type: "document",
    document_type: "incident_report",
    author: "GLMZ Anomaly Documentation Project",
    date: "2221-05-18",
    classification: "restricted",
    description: `Since at least May 2221, a 2.4-square-kilometer area within Wisconsin Sector 7 — a largely depopulated agricultural zone between Oshkosh and Fond du Lac — has exhibited persistent temporal measurement anomalies. Equipment brought into the area, regardless of type, manufacturer, or synchronization method, begins returning timestamps that are eleven minutes behind the reference time maintained by regional network clocks. This is not clock drift in the conventional sense. The equipment does not slow down gradually — it operates at the correct rate but is offset by exactly eleven minutes from the moment it enters the zone, as though it has been transported eleven minutes into the past.

The effect is consistent across all tested devices: atomic clocks, crystal oscillators, network-synchronized terminals, and mechanical timepieces. A mechanical watch carried into the zone will, upon exit, show a time exactly eleven minutes behind an identical watch that remained outside. The discrepancy appears instantaneously upon entry and resolves instantaneously upon exit — there is no transition period. Devices that are synchronized while inside the zone and then removed will show the correct time upon exit, only to revert to the eleven-minute offset if brought back in.

Events observed within the zone display a corresponding delay when viewed from outside. A flare launched inside the zone is visible to an observer standing outside the boundary approximately eleven minutes after the person inside reports launching it. Sound generated inside the zone reaches observers outside after the same delay. The boundary is sharp — within a few meters, the effect transitions from full to absent. Radio transmissions from inside the zone arrive outside with an eleven-minute delay. Radio transmissions from outside arrive inside the zone with no delay, suggesting the effect is asymmetric.

A researcher named Hale Eriksson spent what he reported as six hours inside the zone conducting measurements. Upon exiting, he stated that no time had passed — that he had entered, taken a few readings, and walked out. His equipment logs confirmed six hours of continuous data collection. His biological indicators — beard growth, caloric depletion, fatigue markers — were consistent with six hours of activity. He insisted, and continues to insist, that he experienced approximately fifteen minutes. The zone remains under observation. No explanation has been proposed that accounts for all observed phenomena.`,
    related_entities: ["Wisconsin", "Sector 7", "GLMZ"],
    credibility: "verified",
    story_hooks: [
      "What causes a spatially bounded region to be temporally offset by exactly eleven minutes?",
      "What did Eriksson experience during six hours he doesn't remember living?"
    ],
    tags: ["document", "anomaly", "inexplicable", "new_weird", "glmz", "great_lakes", "wisconsin", "temporal", "time", "drift", "zone"]
  },
  {
    id: genId(),
    name: "The Quantized Tire Tracks — Indiana County Road",
    type: "document",
    document_type: "incident_report",
    author: "GLMZ Anomaly Documentation Project",
    date: "2217-11-05",
    classification: "restricted",
    description: `On November 5, 2217, a road maintenance crew working on County Road 450 in rural Indiana — approximately 40 kilometers south of the Gary industrial perimeter — reported a set of tire tracks exhibiting properties that, upon investigation, proved to be genuinely inexplicable. The tracks ran along the road surface for approximately 1.3 kilometers. They were consistent in depth, width, and tread pattern with a mid-weight ground vehicle, likely a utility transport. They were also discontinuous.

The tire tracks appeared in segments of exactly 4.7 meters, separated by gaps of exactly 4.7 meters, for the entire 1.3-kilometer stretch. Not approximately 4.7 meters — exactly, to within the measurement precision of the survey equipment used (plus or minus 2 millimeters). The regularity was arithmetic: the pattern did not vary by a single measurable increment over nearly three hundred repetitions. No vehicle could produce this pattern through any known mechanical means. The impressions were not stamped — they showed the directional striations consistent with a rolling tire under load, including slight lateral deformation on the two gentle curves in the road.

The asphalt between the impressions was undisturbed. Not just untracked — undisturbed. The road surface in the gaps showed no evidence of tire contact, weight transfer, or any physical interaction with a vehicle. It was as though the vehicle had existed at each 4.7-meter segment, produced a normal tire impression, and then ceased to exist for the next 4.7 meters before reappearing to produce the next impression. The tread pattern does not match any vehicle registered in the regional database, or in the national database, or in any database that the investigating team was able to access.

The tracks have appeared three times since the initial discovery — once in 2218, once in 2220, and once in 2223. Each time, they appear after rainfall, on the same stretch of road, in the same pattern, with the same 4.7-meter spacing. Each time, they appear fresh. The road surface between appearances shows normal weathering. No monitoring equipment has captured the tracks being made — they are simply present after rain, as though the rain itself had revealed them, or as though whatever makes them only travels in the rain.`,
    related_entities: ["Indiana", "GLMZ"],
    credibility: "verified",
    story_hooks: [
      "What vehicle exists in 4.7-meter quantized intervals, and where does it go between them?",
      "Why do the tracks reappear after rain on the same road?"
    ],
    tags: ["document", "anomaly", "inexplicable", "new_weird", "glmz", "great_lakes", "indiana", "tire_tracks", "quantized", "pattern", "recurring"]
  },
  {
    id: genId(),
    name: "The Cleveland Broadcast Paradox",
    type: "document",
    document_type: "incident_report",
    author: "GLMZ Anomaly Documentation Project",
    date: "2210-08-28",
    classification: "restricted",
    description: `In August 2210, a media archivist cataloguing pirate broadcasts from the Cleveland metropolitan area flagged a recording that had been captured and stored by an automated monitoring station on March 3, 2210. The broadcast was unremarkable in most respects — it ran for approximately fourteen minutes on an unlicensed frequency, used standard encoding, and appeared to be a single individual reading what sounded like a news bulletin. The content was specific: names, dates, locations, and a detailed description of a structural collapse in Cleveland's east side industrial district.

The structural collapse described in the broadcast occurred on April 21, 2210 — seven weeks and four days after the broadcast was captured. The details matched precisely. The building identified in the broadcast was the building that collapsed. The casualty count given in the broadcast matched the final confirmed count. Two of the three individuals named in the broadcast as being involved in the subsequent investigation were, in fact, assigned to that investigation — assignments that were not made until after the collapse occurred.

The broadcast's metadata was authenticated by three independent forensic teams. The recording timestamp was verified against the monitoring station's internal clock, which was synchronized to regional reference time and showed no anomalies. The storage medium showed no signs of tampering. The broadcast equipment used to transmit the signal was later identified — abandoned in a warehouse in Cleveland's Tremont neighborhood — and dated through component analysis to the early 2200s, consistent with the broadcast date. There is no credible mechanism by which the broadcast could have been retroactively inserted into the monitoring station's archive.

The archivist who flagged the recording submitted a formal report and was interviewed by the GLMZ documentation team. She described the experience of listening to the broadcast as "like reading an obituary that hadn't been written yet." The broadcast is the only known instance of a pirate transmission containing verifiably accurate information about a future event. The identity of the broadcaster has not been determined. The frequency has not been used since.`,
    related_entities: ["Cleveland", "GLMZ"],
    credibility: "verified",
    story_hooks: [
      "Who broadcast a news report seven weeks before the event it described?",
      "Is this precognition, time displacement, or something that breaks both categories?"
    ],
    tags: ["document", "anomaly", "inexplicable", "new_weird", "glmz", "great_lakes", "cleveland", "broadcast", "precognition", "temporal", "pirate_radio"]
  },
  {
    id: genId(),
    name: "The Loop Man of Whiting",
    type: "document",
    document_type: "incident_report",
    author: "GLMZ Anomaly Documentation Project",
    date: "2223-01-15",
    classification: "restricted",
    description: `Since at least 2220, residents of Whiting, Indiana — a small community on the southern shore of Lake Michigan, wedged between the Gary sprawl and the Chicago perimeter — have reported seeing the same man walk past the same stretch of Indianapolis Boulevard every day at approximately 15:30. He wears the same clothes: dark blue work jacket, grey trousers, black boots. He walks with the same gait — slightly favoring his left leg — at the same pace, along the same route, from east to west. He does not deviate. He does not stop. He does not appear to notice his surroundings.

The man has been identified, through facial recognition and gait analysis performed on surveillance footage, as Dariusz Nowak, a refinery maintenance worker who died on September 12, 2220, from injuries sustained in an industrial accident at a decommissioned processing facility. His death was confirmed. His remains were processed through standard disposition protocols. He is, by every institutional measure, dead.

When approached — and he has been approached on at least eleven documented occasions — Nowak responds normally. He makes eye contact. He answers questions in Polish-accented English, consistent with his documented speech patterns. He has commented on the weather, responded to greetings, and on one occasion gave directions to a resident who asked for them. The directions were accurate. The interactions are brief, lasting no more than thirty seconds, and are described by witnesses as entirely unremarkable — a man on a walk, responding to a stranger, continuing on his way.

Afterward, he is not there to ask. Witnesses who turn around after passing him find the sidewalk empty. Surveillance footage shows him walking out of frame and not entering the frame of the next camera in sequence. He is present during the interaction and absent the moment attention shifts. He has never been observed arriving or departing. He is simply walking, and then he is simply not. Attempts to follow him have failed — not because he evades, but because there is nothing to follow. The loop continues. Dariusz Nowak walks past Indianapolis Boulevard every day at 15:30, dead for three years, wearing the same clothes, favoring the same leg, going somewhere that does not appear to exist.`,
    related_entities: ["Whiting", "Indiana", "Gary", "Chicago", "GLMZ"],
    credibility: "verified",
    story_hooks: [
      "Is Dariusz Nowak a ghost, a glitch in reality, or something that doesn't have a name yet?",
      "What happens if someone walks with him instead of just talking to him?"
    ],
    tags: ["document", "anomaly", "inexplicable", "new_weird", "glmz", "great_lakes", "whiting", "indiana", "ghost", "loop", "recurring", "dead"]
  },
  {
    id: genId(),
    name: "The Versailles Room — South Chicago",
    type: "document",
    document_type: "incident_report",
    author: "GLMZ Anomaly Documentation Project",
    date: "2224-06-10",
    classification: "restricted",
    description: `In a residential tower in South Chicago — the specific address is withheld under GLMZ documentation protocols — there is an apartment on the 14th floor that exists approximately forty years in the past. This is not a matter of decor or preservation. The apartment is not a period recreation, not a museum, not a nostalgia project maintained by an eccentric occupant. It is simply old in a way that should not be possible given the building's construction date of 2212.

The apartment contains furniture, appliances, and personal effects consistent with the 2180s. The wall surfaces show paint degradation and staining patterns consistent with forty years of habitation. The carpeting is worn in patterns consistent with decades of foot traffic. The kitchen contains food packaging with manufacturer codes that trace to production runs from the 2180s — runs that have been confirmed as authentic through industrial records. The food is spoiled, which is consistent with its apparent age. The air in the apartment smells stale in a way that building ventilation should prevent, carrying the accumulated scent signature of years of enclosed habitation.

Photography returns corrupted images. Digital cameras produce files that will not render. Film cameras — sourced specifically to test this — produce negatives that are uniformly fogged, as though exposed to a light source during development, even though development was performed under controlled conditions. A neural recorder brought into the apartment by an investigator captured only static — not the structured static of signal interference, but a dense, featureless noise floor that the recorder's manufacturer stated was "theoretically impossible given the device's architecture."

The apartment's lease is current. Rent is paid automatically from an account that receives regular deposits from a source that has not been identified. The account holder's name corresponds to no living person, but the account has been active since 2186 — thirty-eight years — with no interruptions. The building's management has no record of a tenant complaint, a maintenance request, or any interaction with the apartment's occupant. Neighbors on the 14th floor report hearing normal domestic sounds through the walls — footsteps, water running, the occasional muffled voice — though no one has ever seen anyone enter or exit the apartment. The door is locked. It has not been forced. Nobody has proposed forcing it.`,
    related_entities: ["Chicago", "South Chicago", "GLMZ"],
    credibility: "verified",
    story_hooks: [
      "Who has been paying rent on the Versailles Room for thirty-eight years, and who lives there?",
      "What would happen if someone opened the door?"
    ],
    tags: ["document", "anomaly", "inexplicable", "new_weird", "glmz", "great_lakes", "chicago", "temporal", "apartment", "haunting", "photography"]
  },
  {
    id: genId(),
    name: "The Persistent Echo — Southeast Chicago",
    type: "document",
    document_type: "incident_report",
    author: "GLMZ Anomaly Documentation Project",
    date: "2213-09-22",
    classification: "restricted",
    description: `Beneath an overpass in southeast Chicago — the specific location is documented in the GLMZ restricted archive — there is an acoustic anomaly that has been continuously active since at least 2198, when it was first reported by a sanitation worker on a night shift. The underpass echoes sounds. This is normal for an underpass. What is not normal is that the sounds it echoes are not the sounds being made in or near it. They are sounds from fifteen to thirty years ago.

The echo sequence is consistent and repeatable. It begins with what sounds like a vehicle engine — internal combustion, likely diesel, a technology that has been functionally obsolete in the GLMZ for over two decades. This is followed by voices — indistinct but clearly conversational, consistent with a small group of people talking in a relaxed, unhurried manner. A dog barks twice. A metallic clang, possibly a gate or a dumpster lid. Then a child's voice, clearer than the rest, saying something that has been variously transcribed as "over here" or "almost there." Then silence, lasting approximately four minutes. Then the sequence begins again.

The sounds are not a recording. They do not emanate from a speaker or any identifiable device. They emerge from the structure of the underpass itself — from the concrete and rebar, as though the materials are vibrating with stored acoustic energy. Spectral analysis confirms that the sounds have the characteristics of natural acoustics, not electronic reproduction. They exhibit the reverb patterns, frequency attenuation, and spatial distribution of sounds actually being produced in the physical space, right now, by sources that are not there.

The sequence has been documented over sixty times and has never varied. The same engine, the same voices, the same dog, the same child. The same four minutes of silence. Researchers have attempted to map the sounds to specific historical events or dates, without success. No recordings from the area's past match the sequence. Whatever moment the underpass is echoing, it either was never recorded by anyone else, or it never happened — and the underpass is remembering something that the rest of the world forgot, or something that only the underpass experienced.`,
    related_entities: ["Chicago", "Southeast Chicago", "GLMZ"],
    credibility: "verified",
    story_hooks: [
      "What moment is the underpass replaying, and why that specific sequence?",
      "Is the structure remembering, or is something using the structure to communicate?"
    ],
    tags: ["document", "anomaly", "inexplicable", "new_weird", "glmz", "great_lakes", "chicago", "acoustic", "echo", "temporal", "haunting"]
  },
  {
    id: genId(),
    name: "The Box Array — Lake Michigan Lakebed",
    type: "document",
    document_type: "incident_report",
    author: "GLMZ Anomaly Documentation Project",
    date: "2208-07-14",
    classification: "restricted",
    description: `In July 2208, a commercial salvage operation conducting a routine sonar survey approximately 15 kilometers off the shore of Waukegan, Illinois, detected an anomalous formation on the Lake Michigan lakebed at a depth of 87 meters. The formation consisted of sealed containers arranged in a precise grid pattern. The grid was rectangular — twelve containers by eight, for a total of ninety-six units — with uniform spacing of exactly 2 meters between each container. The containers were approximately 1.5 meters long, 0.8 meters wide, and 0.6 meters deep. They appeared to be constructed from a metallic alloy that resisted identification by the salvage team's remote analysis equipment.

The containers were warm. Thermal imaging from the salvage operation's submersible registered a surface temperature of approximately 30 degrees Celsius on every container — uniform, consistent, and significantly above the ambient water temperature of 4 degrees at that depth. This implies an internal heat source. The containers were sealed with no visible seams, hinges, latches, or access points. Their surfaces were smooth and free of biological fouling — no algae, no sediment accumulation, no zebra mussel colonization — despite sitting on a lake bed where every other surface is covered in biological growth within weeks of submersion.

The salvage team documented the array and surfaced to file a report and request authorization for a closer investigation. When a second dive was conducted six days later, the array had been rearranged. The same containers — confirmed by dimensional analysis and thermal signature — were now in a different configuration: ten by ten, minus four corner positions, forming a grid with truncated corners. The spacing had changed to 2.3 meters. The containers had moved, or been moved, on the lake bed. The sediment beneath them showed no drag marks.

Nobody opened one. The salvage team's report notes this decision without elaboration — simply that "no attempt was made to breach or recover any container." No third dive was commissioned. The salvage company's contract for the survey area was not renewed. Subsequent sonar passes of the location by other operators have returned normal lake bed readings with no anomalous formations. The array is either gone, or it has arranged itself in a way that sonar cannot detect, or it was never there in a way that bears repeating.`,
    related_entities: ["Lake Michigan", "Waukegan", "GLMZ"],
    credibility: "verified",
    story_hooks: [
      "What is inside the warm, sealed containers, and who arranged them on the lake bed?",
      "Why did no one open one, and why was no third dive commissioned?"
    ],
    tags: ["document", "anomaly", "inexplicable", "new_weird", "glmz", "great_lakes", "lake_michigan", "underwater", "containers", "grid", "waukegan"]
  },
  {
    id: genId(),
    name: "The Signal Repeater — Door County",
    type: "document",
    document_type: "incident_report",
    author: "GLMZ Anomaly Documentation Project",
    date: "2212-04-03",
    classification: "restricted",
    description: `Since at least 2209, an unlicensed signal repeater has been operating on the Door County peninsula in northeastern Wisconsin, broadcasting an empty carrier wave on a frequency of 1,296 MHz. The repeater is a physical device — a small, weatherproofed unit approximately the size of a shoebox, mounted to a utility pole approximately 3 meters above ground level on a rural road between Sturgeon Bay and Baileys Harbor. It draws no external power. It has no visible power source. It has no antenna of conventional design — the carrier wave appears to emanate from the casing itself.

The device has been removed on seven separate occasions by regional communications enforcement teams. Each removal is documented: the unit is physically detached from the pole, transported to an analysis facility, and disassembled. Internal examination reveals a circuit architecture that does not correspond to any known manufacturing process or design philosophy. The components are functional but unidentifiable — they perform signal generation and amplification, but through mechanisms that the analyzing engineers have been unable to describe in terms of established electronics theory. Reports consistently note that the device "works, but should not work, based on what it contains."

Within seventy-two hours of each removal, an identical replacement appears on the same utility pole, in the same position, broadcasting the same empty carrier on the same frequency. "Identical" is not an approximation — the replacement units are dimensionally identical to the originals, down to the measurement precision of the instruments used. The casing shows the same minor surface imperfections. The internal components are arranged in the same configuration. It is not a new unit of the same model. It is, to all appearances, the same unit.

No one has been observed installing the replacement devices. The utility pole is under continuous surveillance following the third removal — camera coverage from multiple angles, motion detection, tamper alerts. The surveillance records show an empty pole, and then a pole with the device attached, with no intermediate state. The device does not appear in a single frame of transition, the way the Apostle Islands technicians disappeared. It simply is not there, and then it is. The carrier wave it broadcasts contains no data. It is empty. It may be the most precisely engineered nothing in the Great Lakes region.`,
    related_entities: ["Door County", "Wisconsin", "GLMZ"],
    credibility: "verified",
    story_hooks: [
      "Who or what keeps replacing the repeater, and what is the empty carrier for?",
      "Is the device a beacon, a marker, or a component of something larger?"
    ],
    tags: ["document", "anomaly", "inexplicable", "new_weird", "glmz", "great_lakes", "wisconsin", "door_county", "signal", "repeater", "recurring"]
  },
  {
    id: genId(),
    name: "The Counting Machine — East Detroit",
    type: "document",
    document_type: "incident_report",
    author: "GLMZ Anomaly Documentation Project",
    date: "2219-02-11",
    classification: "restricted",
    description: `In February 2219, a demolition survey team working in East Detroit's abandoned Gratiot Industrial Corridor discovered a device bolted to the interior wall of a gutted manufacturing facility. The device was approximately 40 centimeters square and 15 centimeters deep, mounted at eye level on four heavy-gauge bolts sunk into the concrete wall. Its face consisted of a display — not a screen, but a mechanical display of the split-flap type, similar to those used in 20th-century transit stations — showing a number. The number was in the high billions when first observed, and it was counting upward.

The count was incrementing at a rate of approximately one digit per 1.3 seconds, though the interval was not perfectly regular — it varied by a few hundredths of a second in a pattern that appeared random but may not have been. The display was mechanical: audible clicks accompanied each increment as the flaps rotated to show the next number. The device was warm to the touch. It had no visible power source — no cables, no battery compartment, no solar panel, no induction coil. The wall behind it was solid concrete with no conduits. It was generating its own power through a mechanism that the survey team could not identify.

The survey team photographed and documented the device, reported it to their project coordinator, and requested a technical team to extract it for analysis. A cutting team arrived eighteen hours later, equipped to remove the mounting bolts and the surrounding section of wall if necessary. When they entered the facility, the device was gone. The four mounting bolts remained, protruding from the wall, undamaged and still firmly seated in the concrete. The bolts showed no tool marks consistent with removal of a mounted object. The device had not been unbolted — it had simply departed the bolts, an action that would require either passing through solid metal or disassembling itself down to a scale smaller than the bolt diameter.

The survey team's photographs are on file. They show the device clearly, including the number on the display at the time of photography: 8,847,219,403. What it was counting, and what number it has reached by now, and where it is counting, are questions that the GLMZ documentation project has recorded but cannot begin to answer.`,
    related_entities: ["Detroit", "East Detroit", "GLMZ"],
    credibility: "verified",
    story_hooks: [
      "What is the machine counting, and what happens when it reaches its target number?",
      "Where did the device go, and is it still counting somewhere?"
    ],
    tags: ["document", "anomaly", "inexplicable", "new_weird", "glmz", "great_lakes", "detroit", "machine", "counting", "device", "disappearance"]
  },
  {
    id: genId(),
    name: "The Wrong Sky — Upper Peninsula Reports",
    type: "document",
    document_type: "incident_report",
    author: "GLMZ Anomaly Documentation Project",
    date: "2225-08-various",
    classification: "restricted",
    description: `On four separate occasions — twice in 2223, once in 2224, and once in 2225 — residents of Michigan's Upper Peninsula have reported looking up at night and seeing the wrong sky. Not a sky with unusual atmospheric phenomena, not a sky with unidentified objects, not a sky affected by light pollution or weather conditions. The wrong sky. Stars in configurations that do not correspond to any known arrangement as seen from Earth — constellations that are not constellations, patterns that match no star chart from any epoch or any latitude.

The reports come from widely separated locations across the Upper Peninsula — Marquette, Houghton, Munising, and a rural area near Seney. The witnesses have no connection to each other. They range in age from seventeen to seventy-two. They include a park ranger, a retired mining engineer, a high school student, and a commercial fisherman. None were aware of the other reports at the time of their own observations. The events lasted between ten and forty-five minutes before the sky "returned to normal" — a phrase used independently by three of the four reporting groups.

Photographs taken during the events do not show anomalies. The photographs show a normal night sky, with correct star positions for the date, time, and location of the photograph. The witnesses are universally emphatic that the photographs do not capture what they saw. "That's not what I was looking at," said the park ranger in Marquette during her formal interview. "The camera saw one thing. I saw another." Neural recordings taken during the Seney event — a witness happened to be wearing an active recording implant — show elevated activity in the visual cortex consistent with genuine visual processing of an unfamiliar scene, not confabulation or hallucination.

The descriptions, when compared across the four events, are disturbingly consistent. Witnesses who have never communicated describe the same wrong constellations in the same positions. A pattern of five bright stars in a rough pentagon, low on the northern horizon. A dense cluster near the zenith, "like a handful of sand thrown at the sky." A single intensely bright object in the southwest that does not twinkle. These are not vague impressions — they are specific, consistent descriptions of a sky that should not exist, seen by people who should not agree, and captured by no instrument except the ones behind human eyes.`,
    related_entities: ["Upper Peninsula", "Michigan", "Lake Superior", "GLMZ"],
    credibility: "verified",
    story_hooks: [
      "Whose sky are they seeing, and from where would those constellations be correct?",
      "Why can the human eye see it but cameras cannot?"
    ],
    tags: ["document", "anomaly", "inexplicable", "new_weird", "glmz", "great_lakes", "michigan", "upper_peninsula", "sky", "stars", "perception", "visual"]
  },
  {
    id: genId(),
    name: "The Great Lakes Anomaly Register",
    type: "document",
    document_type: "compiled_reference",
    author: "GLMZ Anomaly Documentation Project",
    date: "2226-01-01",
    classification: "restricted",
    description: `The Great Lakes Anomaly Register is a compiled reference maintained by the GLMZ Anomaly Documentation Project, an informal collective of researchers, archivists, field investigators, and concerned citizens operating without official sanction or institutional backing within the Great Lakes Militarized Zone. The Register documents phenomena that defy conventional explanation — events, objects, locations, and patterns that persist despite investigation, that resist categorization within known scientific or technological frameworks, and that share a single unifying characteristic: they should not be possible, and yet they are thoroughly, meticulously documented.

The GLMZ is one of the most heavily surveilled, densely instrumented, and thoroughly monitored regions in North America. Every square meter of its territory is covered by overlapping sensor networks. Its electromagnetic spectrum is catalogued in real time. Its population is tracked, identified, and databased with a precision that approaches totality. And yet the phenomena documented in this Register occur within that surveillance envelope as though it does not exist — appearing in gaps that should not be there, exploiting blind spots in systems that do not have blind spots, and leaving evidence that is simultaneously irrefutable and impossible.

The Register currently contains twenty-two primary entries, spanning signal anomalies, temporal disturbances, mass behavioral events, unexplained disappearances, spatial impossibilities, and phenomena that resist even these broad categorizations. They include: The Tone Out of Gary, a single-frequency broadcast that has operated without interruption for four decades. The Kenosha Handshake, an authentication exchange with a satellite destroyed 172 years prior. The Arcology Intrusion at Chicago Vertical 7, in which a face that should not exist appeared on every screen in a building. The Dead Contact Voicemails, in which the dead called the living and were biometrically authenticated. The Dance District Outbreak, in which unmodified humans were compelled to move until they died. The Rain Event on Lake Erie's shore, in which an extinct species fell from a clear sky. The Block Party in Waukegan that no one could leave. The Man from Navy Pier, alive and silent for three years. The Isdal Operative of Milwaukee, dead with seven identities and eleven missing days. The Relay Station Keepers of the Apostle Islands, vanished between frames. The Courier Over Lake Michigan, silenced mid-word. The Badlands Call from Indiana's Dead Zone, where a man walked into silence. The Chronological Drift of Wisconsin Sector 7, where time runs eleven minutes behind. The Quantized Tire Tracks of Indiana, appearing in impossible intervals. The Cleveland Broadcast Paradox, a news report from seven weeks in the future. The Loop Man of Whiting, dead and walking daily. The Versailles Room of South Chicago, forty years displaced. The Persistent Echo beneath a Chicago overpass, replaying a moment no one remembers. The Box Array on the Lake Michigan lakebed, warm and rearranging. The Signal Repeater of Door County, endlessly replaced. The Counting Machine of East Detroit, incrementing toward an unknown total. The Wrong Sky of the Upper Peninsula, seen by eyes that cameras cannot corroborate.

Each entry in the Register has been verified through multiple independent investigations. The documentation standards are rigorous: physical evidence, sensor data, witness testimony cross-referenced against biometric and neural records, forensic analysis performed by teams with no knowledge of each other's findings. The Register does not speculate. It does not theorize. It records what has been observed, confirms that the observations are accurate, and notes — with the clinical restraint of a discipline that has learned not to flinch — that the observations are inexplicable.

There are patterns, if one looks. The phenomena cluster geographically around the Great Lakes themselves, with the highest density along the Lake Michigan corridor between Chicago and Milwaukee. They exhibit a preference — if that word can be applied to phenomena that may not have preferences — for liminal spaces: shorelines, underpasses, abandoned structures, the boundary between inhabited and uninhabited territory. Several involve temporal displacement or ambiguity. Several involve signals or communication. Several involve the intersection of presence and absence — things that are there and not there, people who are alive and dead, messages from senders who no longer exist.

The Register does not draw conclusions from these patterns. It notes them and moves on. The collective experience of the documentation project's members, accumulated over decades of fieldwork in the GLMZ, has produced a single operational principle that is not written in any of the formal reports but is understood by everyone who contributes to them: the anomalies are not decreasing. They are not stabilizing. They are, by every available metric, slowly and steadily increasing in frequency, in geographic spread, and in the degree to which they deviate from known physical law.

If you are experiencing an event consistent with the above, do not report it through standard channels.`,
    related_entities: [
      "GLMZ", "Great Lakes", "Chicago", "Milwaukee", "Detroit", "Gary",
      "Kenosha", "Waukegan", "Indiana", "Wisconsin", "Michigan",
      "Lake Michigan", "Lake Erie", "Lake Superior", "Apostle Islands",
      "Door County", "Upper Peninsula", "Cleveland", "Navy Pier", "Whiting"
    ],
    credibility: "verified",
    story_hooks: [
      "The anomalies are increasing — what is changing in the GLMZ, and is something approaching?",
      "Why should events not be reported through standard channels, and who is listening?"
    ],
    tags: ["document", "anomaly", "inexplicable", "new_weird", "glmz", "great_lakes", "master_document", "compiled_reference", "register", "overview"]
  }
];

// --- Main ---
if (!fs.existsSync(OUTPUT_DIR)) {
  console.error(`Output directory does not exist: ${OUTPUT_DIR}`);
  process.exit(1);
}

let created = 0;
let skipped = 0;

for (const doc of documents) {
  if (writeDoc(doc)) {
    created++;
  } else {
    skipped++;
  }
}

console.log(`\nDone. Created: ${created}, Skipped: ${skipped}, Total entries: ${documents.length}`);
