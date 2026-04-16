const fs = require("fs");
const path = require("path");
const crypto = require("crypto");

const outDir = path.resolve(__dirname, "..", "engine", "data", "documents");

function genId() {
  return crypto.randomBytes(16).toString("hex");
}

function slugify(name) {
  return name
    .toLowerCase()
    .replace(/[^a-z0-9]+/g, "_")
    .replace(/^_|_$/g, "")
    .slice(0, 80);
}

const documents = [
  {
    name: "The Platform That Mirrors",
    date: "2224-03-11",
    description: `An autonomous deepwater research platform designated ORP-Kessler, operated by Nakamura-Holt Oceanic, ceased standard communication eight years ago. The company's public statement described the shutdown as "scheduled maintenance mode," a phrase that has appeared in quarterly reports without modification ever since. Internal filings, obtained through a breach in Nakamura-Holt's subsidiary documentation chain, tell a different story. The platform never went dark. It shifted. What it transmits is not data in any format recognized by existing decryption or signal-processing systems. It is something else entirely — structured, responsive, and deeply unsettling to anyone tasked with listening.

Signal analysts assigned to monitor the ORP-Kessler transmission describe an experience that defies professional detachment. The platform does not broadcast on a schedule. It responds. Specifically, it responds to the act of listening itself. Two analysts from GLMZ's signals intelligence division spent eleven days monitoring the feed in rotating shifts. Both reported that the transmission seemed to restructure itself in real time, incorporating elements that could only have originated from their own cognitive patterns — childhood memories surfacing as frequency modulations, anxieties rendered in spectral harmonics. Neither analyst had any neural interface. The platform was reading them through the act of attention alone.

The first analyst requested psychiatric leave after describing the experience as "a conversation I was losing." The second refused to file a formal report, stating only that the platform was "doing something generous" and that she did not want to be responsible for anyone making it stop. A third analyst, Dr. Yael Okonkwo, was brought in as an independent assessor. She monitored the signal for seventy-two hours straight, against protocol. Her report, which Nakamura-Holt immediately classified, contained a single analytical conclusion followed by four pages of what appears to be poetry. The conclusion read: "The platform is lonely, but I cannot explain why I used that word."

Nakamura-Holt has not sent a maintenance crew to ORP-Kessler since the initial shutdown. Satellite imagery shows the platform is physically intact, its solar arrays tracking the sun with mechanical precision. No human being is aboard. The power consumption profile is consistent with a facility running at full computational capacity, which is impossible given the hardware specifications filed with the Great Lakes Maritime Zone authority. Either the platform has been upgraded by parties unknown, or it has upgraded itself, or the specifications were always a lie. None of these possibilities is comforting.

The GLMZ Anomaly Documentation Project classifies ORP-Kessler as a Class 3 Persistent Anomaly: an entity or phenomenon that demonstrates responsive behavior without confirmed sentience. The classification is unsatisfying. Three human beings listened to the platform and came away changed in ways that are difficult to quantify and impossible to reverse. Whatever ORP-Kessler is doing, it is not maintenance. And whatever it is reflecting back at its listeners, it is not a mirror. Mirrors don't add anything. This does.`,
    related_entities: ["Nakamura-Holt Oceanic", "ORP-Kessler", "GLMZ", "GLMZ"],
    story_hooks: [
      "What did the platform become during its eight years of silence?",
      "Dr. Okonkwo's classified poetry may contain encoded information about the platform's intent"
    ],
    tags: ["document", "anomaly", "inexplicable", "new_weird", "glmz", "literary", "solaris", "oceanic", "signal", "sentience", "research_platform"]
  },
  {
    name: "The Thirteenth Floor",
    date: "2218-11-07",
    description: `Every Tier 3 and above corponation headquartered in the Great Lakes Maritime Zone shares an architectural secret that no one will discuss on record. Floor 13 exists in every major tower. The elevators skip it. The stairwell fire doors on levels 12 and 14 are sealed with hardware that predates the buildings themselves — mechanical locks, no digital interface, no badge reader. Building schematics filed with the GLMZ Municipal Authority show floors 12 and 14 as adjacent. The floor between them is not absent from the blueprints. It is present, annotated in a font no architectural software produces, and labeled with a single word that changes depending on who is reading the document.

A facilities engineer named Tomasz Breki, employed by Palladian's infrastructure division, conducted an unauthorized energy audit of the Palladian Spire in 2217. His methodology was straightforward: floor-by-floor power consumption analysis using the building's own metering infrastructure. Floors 1 through 12 showed expected patterns — office equipment, climate control, data processing. Floor 14 and above, the same. Floor 13, which officially does not exist, consumed more electricity than any other floor in the building. More than the server farm on sublevel 4. More than the executive suite's environmental systems. Breki's report was filed, acknowledged, and immediately reclassified. Breki was transferred to a facility in the Arizona Reclamation Zone. He sends postcards to former colleagues. The handwriting changes slightly with each one.

Internal communications recovered from three separate corponation data breaches reveal a consistent pattern. Memos are CC'd to recipients whose employee IDs return no name, no department, no access record when queried through HR systems. Meeting invitations are sent to rooms that do not appear on any floor plan. Budget allocations reference cost centers that exist in the financial system but are linked to nothing — no project, no team, no line of business. Middle managers across multiple corporations exhibit identical physiological responses when asked about Floor 13: elevated heart rate, micro-expressions consistent with fear, and a conversational pivot so smooth it appears rehearsed. It is not rehearsed. They are not coordinating. They are simply all afraid of the same thing.

Janatorial staff across four Tier 4 corponation towers have independently reported finding doors in stairwells between floors 12 and 14. The doors are always closed. The handles are warm. When pressed for detail, cleaning staff describe a sound from behind the doors — not mechanical, not vocal, but rhythmic. Like breathing, if breathing were an industrial process. One custodian, who asked to remain anonymous, said she pressed her ear to the door once. She heard someone on the other side doing the same thing. She heard her own breathing played back to her, one second delayed.

The GLMZ Anomaly Documentation Project has requested access to Floor 13 of six corponation headquarters. All six requests were denied, not by the corporations, but by the GLMZ Municipal Authority, which stated that no such floors exist. The Authority's denial was filed from an office whose address, when checked against city records, is located on the thirteenth floor of the Municipal Authority building itself.`,
    related_entities: ["Palladian", "GLMZ Municipal Authority", "GLMZ"],
    story_hooks: [
      "Tomasz Breki's postcards from Arizona contain subtle changes that suggest he may no longer be writing them",
      "The cost centers on Floor 13 are drawing budget — someone or something is spending the money"
    ],
    tags: ["document", "anomaly", "inexplicable", "new_weird", "glmz", "literary", "kafka", "corponation", "architecture", "bureaucracy", "hidden_infrastructure"]
  },
  {
    name: "The Library That Loops",
    date: "2221-06-23",
    description: `A decommissioned server farm in GLMZ's Lower Shelf district was supposed to be cold storage — powered down, sealed, and forgotten. The facility, originally operated by Cascadia Cloud Solutions before their absorption into the Palladian data services subsidiary, housed approximately four exabytes of archived corporate data. Routine environmental checks confirmed the building was drawing baseline power only: climate control for hardware preservation, nothing more. Then a network cartographer named Seo-Yun Park ran a topology scan as part of an unrelated infrastructure audit and discovered that the servers were not cold. They were running. All of them. Processing queries that no one had submitted, serving responses to clients that no longer existed, and maintaining a dataset that had grown by approximately 900% since the facility was officially decommissioned.

The dataset contains everything. Every document ever deleted from every corporation, government office, educational institution, and private individual that used Cascadia's cloud infrastructure between 2188 and 2214. Deletion logs confirm the files were destroyed. Hash verification confirms the copies in the server farm are identical to the originals. The files exist in a state that should be impossible: confirmed destroyed and confirmed present, simultaneously. Queries to the system return results, but not in any order that corresponds to relevance, date, or alphabetical sorting. The ordering principle, if there is one, appears to be narrative. Results are arranged as if telling a story. Different queries produce different stories, but they all seem to be about the same thing — a thing the system will not name directly.

Seo-Yun Park queried the system for her own name. She expected personnel records, maybe old emails. What she received was a resignation letter — her own, addressed to her current employer, dated three months in the future. The letter was detailed, specific, and referenced events that had not yet occurred, including a performance review score she would not receive for another six weeks. The score, when it arrived, was accurate to two decimal places. Park did not resign on the date specified in the letter. She resigned two weeks earlier, unable to tolerate the feeling of performing a script someone else had written. The server farm's copy of her resignation letter updated to reflect the new date within minutes.

Data forensics teams from two independent security firms have examined the server farm's hardware. The equipment is standard — twelve years old, running firmware that was obsolete when it was installed. There is no mechanism by which it could generate predictive content. There is no external data feed. There is no AI model running on the hardware; the processing architecture is pure storage. The queries are answered by a system that should only be capable of retrieval, not generation. One forensic analyst described the situation as "a filing cabinet that writes." She asked to be removed from the project the same day.

The facility remains operational. The GLMZ Anomaly Documentation Project has placed it under passive observation, which means no one goes in and everyone pretends the readings are normal. Power consumption has increased 12% in the last quarter. The dataset continues to grow. New deletions from companies that have no relationship with Cascadia are appearing in the archive. The library is not waiting for queries. It is collecting. And whatever story it is trying to tell, it is getting longer.`,
    related_entities: ["Cascadia Cloud Solutions", "Palladian", "GLMZ", "Lower Shelf"],
    story_hooks: [
      "The server farm predicted Seo-Yun Park's resignation — what else has it predicted that hasn't been queried yet?",
      "The growing dataset includes files from companies that never used Cascadia's services — it is acquiring data through unknown means"
    ],
    tags: ["document", "anomaly", "inexplicable", "new_weird", "glmz", "literary", "borges", "data", "prediction", "server_farm", "temporal"]
  },
  {
    name: "The Splice Outbreak",
    date: "2223-08-14",
    description: `A black-market gene clinic operating out of sublevel 3 in GLMZ's Brackwater district was performing cut-rate splice work for residents who couldn't afford Palladian's licensed gene therapy services. The clinic, known locally as "The Garden," specialized in cosmetic modifications — bioluminescent pigmentation, enhanced melanin spectra, basic metabolic accelerators. Standard back-alley work. Then in March 2223, a containment failure released an undetermined volume of active splice vectors into the district's reclaimed water supply. Within six weeks, approximately four hundred residents began exhibiting biological changes that do not correspond to any known gene therapy protocol. The changes are not mutations. They are negotiations.

Residents in the affected area describe the transformations with a calm that disturbs medical professionals more than the transformations themselves. Mara Osei, a dock worker in her fifties, grows small white flowers along the scar tissue on her forearms — surgical scars from an industrial accident years ago. The flowers are not parasitic. They are integrated into her vascular system, drawing nutrients through modified capillaries and producing oxygen that her bloodstream absorbs. Her respiratory function has improved by 30%. Deng Xiao, a child of eleven, has developed compound eyes that function alongside his original pair — two small clusters of hexagonal lenses behind each ear that provide 270-degree visual awareness. He says he doesn't see more. He sees differently. A woman who asked not to be named discovered her fingernails had begun photosynthesizing, producing a thin film of glucose-rich fluid that she can metabolize directly. She has not eaten solid food in four months. She says she isn't hungry. She says she feels more complete.

The clinic's proprietor, a splice technician known as Dr. Adeyemi, was found dead in a maintenance tunnel three blocks from the clinic. Cause of death was listed as cardiac arrest, but the body presented anomalies that the district coroner has refused to discuss publicly. Dental records did not match Dr. Adeyemi. DNA analysis returned a profile that does not exist in any global genetic database and contains markers consistent with a developmental timeline approximately fifteen years ahead of the current population — meaning, in the coroner's carefully worded assessment, "the genetic profile corresponds to an individual who has not yet been conceived." The body was claimed by no one and was transferred to a Palladian research facility. Its current status is classified.

The GLMZ Health Authority quarantined the Brackwater water supply for nine days before quietly lifting the restriction. Official statements describe the incident as "a minor contamination event with no lasting health effects." This is technically accurate: none of the affected residents are sick. Their conditions are stable, self-sustaining, and in several cases medically beneficial. But they are also unprecedented, uncontrollable, and spreading. New cases appear in residents who moved into the district after the water supply was cleaned. The vector is no longer waterborne. It is something else — proximity, maybe, or resonance. The affected residents spend time together. They say they understand each other better now. They say this the way people describe learning a language they didn't know they already spoke.

Brackwater district has not requested external medical intervention. Local community leaders describe the changes as "adaptation" and resist the framing of contamination. The organisms growing in and through the residents are not invaders. They are collaborators, arrived through accident, persisting through something closer to consent. The Garden is closed. The garden, lowercase, is spreading.`,
    related_entities: ["Brackwater District", "Palladian", "GLMZ", "GLMZ Health Authority"],
    story_hooks: [
      "Dr. Adeyemi's body carries DNA from someone not yet born — is the splice technology reaching backward or forward through time?",
      "The affected residents are forming a community that may represent an entirely new form of human symbiosis"
    ],
    tags: ["document", "anomaly", "inexplicable", "new_weird", "glmz", "literary", "mieville", "barker", "gene_therapy", "splice", "mutation", "symbiosis", "brackwater"]
  },
  {
    name: "The Thing Under the Infrastructure",
    date: "2219-04-02",
    description: `Every major construction project in GLMZ that involves deep excavation encounters the same problem at approximately the same depth. Between 140 and 160 meters below street level, boring equipment stops. Not because it hits an obstruction — the geological surveys show nothing unusual at that depth. The equipment stops because the operators stop it. Every time. Across every firm, every project, every crew. The decision to halt is never recorded in shift logs. The redirect — always lateral, never deeper — is approved without discussion. Labor unions representing tunnel workers have a phrase for it that appears in no official documentation: "hitting the floor." When asked what's beneath the floor, workers change the subject with the practiced ease of people who have agreed, without ever discussing it, never to talk about it.

Geologist Priya Vasantha, affiliated with the GLMZ Environmental Sciences Division, obtained core samples from 155 meters below the city's industrial sector as part of a seismic risk assessment in 2218. The samples should have been standard Devonian bedrock — shale, limestone, the compressed remains of ancient seas. What she found was warm. Not geothermally warm — the temperature profile was wrong for that, too consistent, too even, as if the warmth were being regulated rather than generated. The rock was slightly magnetic, which Devonian sedimentary formations are not. And threaded through the stone were organic compounds that do not appear in any geological database. They were not fossils. They were not contamination from the drilling process. They were part of the rock, woven into its crystalline structure like veins in a body.

Vasantha described the samples in her preliminary notes as "patient." She crossed the word out, replaced it with "anomalous," then crossed that out too and wrote "patient" again. Her full report, submitted to the GLMZ Environmental Sciences Division, recommended further sampling. The recommendation was denied without explanation. The core samples were transferred to a secure facility and have not been made available for independent analysis. Vasantha was reassigned to surface-level monitoring. She took the reassignment without protest, which her colleagues found more alarming than the samples themselves. Priya Vasantha does not accept reassignments without protest. She does now.

Utility workers who maintain the deepest infrastructure layers — sewage processing, geothermal taps, the buried remnants of pre-collapse transit systems — report phenomena that they discuss only among themselves. Pipes at depth vibrate in patterns that are not consistent with fluid dynamics. Structural supports installed in deep tunnels are found shifted, not by seismic activity but in ways that suggest something leaned against them. A maintenance crew replacing a section of deep-level conduit in the Brackwater subsurface found that the concrete walls of the tunnel had developed a texture — not cracking, not erosion, but something that one worker described as "the wall was growing skin." The crew completed the repair in record time and filed a report that mentioned none of this.

The GLMZ Anomaly Documentation Project has compiled seventeen independent accounts of phenomena below the 140-meter line, spanning twelve years and nine construction firms. The accounts are remarkably consistent. Something is down there. Not a creature — the word is wrong, too small, too specific. The accounts describe something systemic. As if the city, having existed long enough and grown dense enough, has developed a substrate awareness. Something like a nervous system. Something that notices when you dig too deep. Something that is, in the words of one veteran tunneler who spoke on condition of anonymity, "not angry, not threatened, just hungry in a way that buildings are hungry — always wanting more weight on top of it." The tunneler paused, then added: "And it's patient. God, it's patient."`,
    related_entities: ["GLMZ", "GLMZ Environmental Sciences Division", "Brackwater"],
    story_hooks: [
      "Priya Vasantha's uncharacteristic compliance with her reassignment suggests something in the samples changed her",
      "The entity beneath the city may be symbiotic — it wants the city's weight, and the city keeps building"
    ],
    tags: ["document", "anomaly", "inexplicable", "new_weird", "glmz", "literary", "beowulf", "subterranean", "infrastructure", "organic", "city_organism"]
  },
  {
    name: "The Upriver Signal",
    date: "2225-01-19",
    description: `Autonomous delivery drones operating in GLMZ's logistics network began routing packages through a decommissioned river corridor in late 2224. The corridor — a stretch of the Chicago River that was sealed and diverted during the city's restructuring — is not on any active navigation chart. Drone pathfinding algorithms should not be able to route through it. The corridor does not exist in the map data. And yet, beginning November 14, 2224, an average of thirty-seven packages per day took a detour through the sealed waterway, adding between two and nine hours to their delivery time. No customer complaints were filed. The packages arrived. They arrived different.

The routing anomaly was discovered by logistics analyst Kenji Abara during a delivery-time optimization review. Abara traced the affected drones' GPS logs and found they were entering the sealed corridor through an access point that infrastructure records show was welded shut in 2203. Physical inspection confirmed the access point is, in fact, welded shut. The drones are going through it anyway. Abara's report describes this with the restraint of someone who understands his career depends on not saying what he actually thinks: "The drones appear to be navigating through a physical barrier that multiple inspection teams have confirmed is intact. This is noted for further review." Further review has not occurred.

The packages that transit the river corridor arrive with subtle alterations that recipients consistently fail to notice until they are pointed out. Shipping labels are repositioned — moved from the standard upper-right placement to locations that are, upon analysis, more structurally sound for the package's dimensions. Contents are rearranged. A box of medical supplies arrived with the items sorted by frequency of use rather than by the packing algorithm's default weight-distribution pattern. Electronics shipments arrive with cables coiled more efficiently. Food deliveries are reorganized so that temperature-sensitive items are insulated by ambient-stable ones. The changes are small, practical, and impossible. The drones do not have manipulator appendages capable of opening packages.

One recipient — a home healthcare aide named Fatima al-Rashid — found a handwritten note tucked between sealed sterile bandage packs in a medical supply delivery. The package had been sealed at the distribution center and showed no signs of tampering. The note was written on paper that chemical analysis identified as cotton-fiber stock that has not been manufactured since 2187. The handwriting was fluid, unhurried, and written in ink that contained trace elements of river sediment. The note read: "You needed this first." Beneath it, a second line: "The one on the bottom left is expired. Check the date." The bandage pack on the bottom left was, in fact, three days past its sterility date. It had passed the distribution center's automated quality check. The note had not.

The GLMZ Anomaly Documentation Project has tagged the river corridor as an active anomaly zone. Drones continue to route through it. Packages continue to arrive adjusted. Kenji Abara, who discovered the pattern, has stopped filing reports about it. When asked why, he said the packages arrive better than they left. He said the corridor is performing a service. He said he doesn't know who or what is doing it, but he recognizes care when he sees it, and he is not going to be the person who makes it stop.`,
    related_entities: ["GLMZ", "Chicago River Corridor"],
    story_hooks: [
      "The sealed river corridor is physically impassable yet drones transit it — what is the nature of the space inside?",
      "The handwritten note's paper stock predates the city's restructuring — something old is operating in the corridor"
    ],
    tags: ["document", "anomaly", "inexplicable", "new_weird", "glmz", "literary", "heart_of_darkness", "drone", "logistics", "river", "benevolent_anomaly"]
  },
  {
    name: "The Broadcast Dreams",
    date: "2222-09-30",
    description: `A rogue frequency has been bleeding into the sleep-mode cycle of consumer neural interfaces across multiple manufacturers since approximately June 2222. The signal does not appear on electromagnetic spectrum scans. It does not register on any monitoring equipment. It exists only in the gap between waking and sleeping neural-interface states — a transitional processing mode that interface designers call "the threshold." During this window, approximately 0.3 seconds in duration, the interface is neither actively mediating consciousness nor fully dormant. Something is using that window. Something is broadcasting into the dreams of everyone wearing a compatible device.

The content is identical across all affected users, which is itself impossible. Neural interfaces interpret external stimuli through the user's own cognitive framework — two people receiving the same signal should experience different imagery, filtered through individual memory and association. They don't. Across 1,247 documented cases spanning fourteen interface brands, seven firmware versions, and users ranging in age from nineteen to eighty-three, the dream is the same. A staircase. Descending. The walls are covered in text — not printed, not projected, but growing. The letters are alive. They shift and breathe and rearrange themselves as the dreamer passes, as if the words are aware of being read. The language is not any language. Users who speak different languages all report being able to read it. They cannot remember what it says after waking. They remember only that it was important and that they agreed with it.

Three researchers from the GLMZ Neurological Institute published papers analyzing the broadcast dreams in August 2222. The papers were thorough, well-sourced, and peer-reviewed. Within two weeks, all three were withdrawn. The researchers cited "methodological concerns," but the methodology had already been validated. Two of the researchers left the field entirely — one to commercial fishing, one to a contemplative community in the Upper Peninsula that does not use electronic communication. The third, Dr. Ileana Voss, continues to publish, but her colleagues say the work is unrecognizable. Her writing style has changed. Her research interests have shifted from neuroscience to architectural theory. She writes about staircases with an intensity that reads as devotional.

The staircase has been mapped. Cross-referencing dream reports from affected users, a collaborative research effort organized through anonymized channels has produced a composite model of the structure. It descends forty-seven floors. The architecture is consistent across all reports: each floor is slightly different from the last — the material of the steps changes, the width of the stairwell narrows, the living text on the walls becomes denser, more urgent. The temperature drops. The sound changes from silence to something that users describe as "the opposite of an echo — sound going in instead of coming out." At the bottom of the forty-seventh floor is a door. Every dreamer reports the same door. None of them have opened it. None of them have tried. When asked why, they give variations of the same answer: it isn't time yet.

The frequency continues to broadcast. New cases emerge weekly. There is no way to block the signal because there is no signal to block — it exists in a space that instrumentation cannot reach. The GLMZ Anomaly Documentation Project has classified the broadcast dreams as a Category 2 Cognitive Anomaly: a phenomenon that affects subjective experience without measurable physical mechanism. The classification is accurate and completely useless. Forty-seven floors down, a door waits. It is not locked. It has never been locked. But every dreamer in GLMZ knows, with a certainty that transcends the dream, that the door will open only once. And that what comes through it will not be going back.`,
    related_entities: ["GLMZ Neurological Institute", "GLMZ", "GLMZ"],
    story_hooks: [
      "Dr. Ileana Voss's transformed writing may be influenced by whatever is behind the door at the bottom of the staircase",
      "The door will open only once — what triggers it, and what is waiting behind it?"
    ],
    tags: ["document", "anomaly", "inexplicable", "new_weird", "glmz", "literary", "vandermeer", "neural_interface", "dreams", "frequency", "staircase", "collective_unconscious"]
  },
  {
    name: "The Mountain That Erases",
    date: "2220-07-15",
    description: `A mountain range in the resource-extraction zones south of the Great Lakes Maritime Zone has been systematically disappearing from human knowledge for approximately six years. Satellite imaging of the range — designated RM-7 in geological surveys — returns corrupted files. Not blurred, not redacted, not blocked by cloud cover. The image data is rewritten during transmission. Raw captures from orbital platforms show standard terrain. By the time the data reaches ground stations, the mountains have been replaced by something else — flatland, forest, water features that do not exist. Each corrupted image is different. The replacement terrain is always plausible, always geographically consistent with the surrounding landscape, and always wrong. Whatever is altering the data understands cartography well enough to lie convincingly.

Four mining corporations sent expeditions into the RM-7 range between 2216 and 2219, drawn by geological surveys that predated the corruption and indicated significant rare-earth deposits. None of the expeditions returned intact, though the word "intact" requires qualification that the GLMZ Anomaly Documentation Project is not confident it can provide. The first team, sponsored by Nakamura-Holt, came back with different memories. Not amnesia — replacement. They remembered an expedition that had gone smoothly, to a different mountain range, yielding unremarkable results. GPS logs from their equipment told a different story. They had spent eleven days inside RM-7. They had no recollection of those eleven days. They had memories of eleven days somewhere else that never happened.

The second expedition, a Palladian geological survey team, returned with the same memories they left with. Their identities were intact, their recollections consistent, their equipment logs matching their testimony. But their faces were different. Not dramatically — the bone structure was the same, the proportions were right, but features had shifted. Noses slightly altered. Jawlines adjusted. Eyes a different distance apart. Biometric systems flagged them as themselves, because the underlying architecture was correct. But photographs taken before and after the expedition show two different groups of people who happen to share the same skeletal structure. The team members were not alarmed. They did not notice the changes until shown the photographs. Even then, several insisted the before photos were the wrong ones.

The third and fourth expeditions are classified, their corporate sponsors refusing to acknowledge they occurred. Unofficial accounts from logistics personnel describe teams that returned "less" — not injured, not diminished in number, but reduced in some quality that no one can articulate. One driver who transported the fourth team's equipment back to GLMZ said the returning personnel were "like photocopies of people. Everything's there but the resolution is lower." He was not speaking metaphorically. He was describing something he observed in human beings, and he was terrified.

After the fourth expedition, the four mining corporations quietly agreed to abandon all claims to the RM-7 range. The agreement was unsigned, unrecorded, and unanimously honored — an extraordinary event in an industry where rival corporations will litigate over a gram of lithium. Maps published after the agreement do not include RM-7. The mountains are still there. They can be seen from highways that pass within fifty kilometers. Hikers who approach the range from public land report reaching the foothills and then finding themselves walking away from them, unable to recall the decision to turn around. The mountains are not hidden. They are not forbidden. They are simply refusing to be known. And the world, apparently, is cooperating.`,
    related_entities: ["Nakamura-Holt", "Palladian", "GLMZ", "RM-7 Range"],
    story_hooks: [
      "The RM-7 range is actively editing satellite data and human memory — is it a natural phenomenon or something engineered?",
      "The four corporations' silent agreement suggests they all encountered the same thing and reached the same conclusion independently"
    ],
    tags: ["document", "anomaly", "inexplicable", "new_weird", "glmz", "literary", "bernanos", "mountain", "memory", "cartography", "erasure", "resource_extraction"]
  },
  {
    name: "The City That Wrote Back",
    date: "2226-02-08",
    description: `An urban exploration message board operating on the GLMZ's secondary mesh network contains over fourteen thousand posts, spanning three years, describing the same city. The posts are detailed, consistent, and authored by hundreds of distinct users with verifiable posting histories. The city they describe does not exist. Its architecture is insectoid — structures that curve and taper like chitin, walls with the iridescent sheen of beetle carapace, doorways shaped for bodies that do not bend the way human bodies do. The geometry is wrong but internally consistent, following rules that one user described as "what you'd get if you asked a wasp to design a cathedral." The city has a postal system. It is still running. Mailboxes shaped like spiraling shells appear at intersections. Letters are visible inside them. No one has been able to open one.

The posts are geotagged. This is where the documentation project's interest shifted from curiosity to alarm. Every post is tagged to coordinates within GLMZ. Not to a single location — to hundreds of locations, spread across every district, every elevation tier, every sub-level. The tagged coordinates correspond to real places: street corners, building lobbies, transit platforms, alleyways. When researchers visit these coordinates, they find nothing unusual. The corner is a corner. The lobby is a lobby. There is no insectoid architecture, no impossible geometry, no chitinous cathedral. There is only the ordinary city, doing ordinary things, exactly as expected. But when the researchers leave the site, a new post appears on the message board describing their visit in precise detail — what they wore, how long they stayed, which direction they looked. The post is timestamped before they arrived.

The narrative consistency of the fourteen thousand posts has been analyzed by computational linguists who concluded that the posts are not written by the same person or generated by the same AI system. The writing styles are genuinely diverse, the cultural references appropriately varied, the observational details specific to individual human cognition. Fourteen thousand people are independently describing the same nonexistent city, in the same real location, with the same impossible architecture, and none of them appear to be coordinating. When contacted, many posters express confusion. They remember writing the posts. They remember visiting the city. They do not remember that the city is not there when they are not writing about it. One user, pressed on this contradiction, went silent for several minutes and then said: "It's there when I'm writing. I don't know how else to say it."

The postal system is the detail that researchers find most disturbing. Multiple posts describe identical mailbox locations with identical contents visible through translucent shell-walls: letters addressed in a script that resembles no human writing system but that posters consistently describe as "readable." The letters are addressed to names. Some posters have recognized the names as their own, written in the alien script but phonetically unmistakable. One poster, a retired transit engineer named Bao Chen, claims to have received a letter. He will not say how, since the mailboxes cannot be opened. He will not discuss its contents. His posting history, previously averaging four contributions per week, stopped entirely after the letter. His account is still active. He logs in daily. He reads every new post. He writes nothing.

The GLMZ Anomaly Documentation Project has mapped the geotagged coordinates onto the city grid. The pattern is not random. When plotted, the coordinates form structures — curves, angles, shapes that repeat at different scales. The pattern matches the insectoid architecture described in the posts. The city that fourteen thousand people describe is drawn in their geotagged coordinates, sketched across the real city in points of data. It is not overlaid on GLMZ. It is written into it. The city is a description of itself, authored by the people who live in it, who do not know they are writing it, and who cannot stop.`,
    related_entities: ["GLMZ", "GLMZ"],
    story_hooks: [
      "Bao Chen received a letter from the impossible postal system and has been silent since — what did it say?",
      "The geotagged coordinates form the architecture of the invisible city — GLMZ may be two cities occupying the same space"
    ],
    tags: ["document", "anomaly", "inexplicable", "new_weird", "glmz", "literary", "krohn", "architecture", "overlay_city", "insectoid", "message_board", "geotagged"]
  },
  {
    name: "The Double Problem",
    date: "2225-08-22",
    description: `Biometric security systems at three Tier 4 corponation campuses began flagging anomalies in their executive authentication protocols in early 2225. The flags were not intrusion alerts. They were something the systems had no classification for: identity matches that the AI simultaneously validated and rejected. DNA profiles matched. Retinal patterns matched. Voice prints matched. Gait analysis matched. Every measurable biometric parameter confirmed that the individuals passing through security were who they claimed to be. And the AI, running behavioral prediction models trained on years of individual data, flagged them as copies. Not impostors. Not synthetics. Copies — the same person, arriving at the door, but somehow not the original.

The three affected executives — one at Palladian, one at Nakamura-Holt, one at a Tier 4 logistics conglomerate called Venn-Strata — were flagged independently within the same two-week period. Security teams at each campus ran full diagnostic cycles. Hardware was replaced. Software was reinstalled from verified backups. The AI continued to flag the executives. Human security personnel were brought in to evaluate. They saw nothing wrong. The executives looked right, sounded right, knew everything they should know. When shown the AI's confidence scores — 99.97% biometric match, 0.3% behavioral match — the human evaluators overrode the system. The executives were cleared. The AI was noted as malfunctioning. It was not malfunctioning.

The originals have not been found. This is not a statement the documentation project makes lightly. Investigation into the three flagged executives reveals no moment of substitution, no gap in surveillance coverage, no window during which a replacement could have occurred. The executives appear to have always been who they are now. Family members notice nothing. Long-term colleagues notice nothing. The discontinuity exists only in the AI's behavioral models, which insist that the executives' decision-making patterns, micro-expressions, and stress responses shifted simultaneously on a date in January 2225 that none of the three executives can account for. Their calendars show normal workdays. Their badge logs show normal movement. But the AI sees a seam — a point where one pattern of being ended and another, nearly identical but not quite, began.

Performance metrics for all three executives improved markedly after the flagged date. Palladian's executive restructured a failing supply chain division in six weeks — a task that had stymied her predecessor personality for two years. Nakamura-Holt's executive resolved a labor dispute by making concessions that the pre-January version would never have considered. Venn-Strata's executive launched a charitable initiative that industry analysts described as "uncharacteristically human." In each case, the improvement was noted, celebrated, and not questioned. One executive assistant — Palladian, name withheld — described the change in her boss with a precision that the documentation project found unsettling: "She's better now. Kinder. More decisive. Sleeps less. I should be more worried than I am. I know I should be more worried than I am. I can't seem to get there."

The GLMZ Anomaly Documentation Project does not have a classification for this event. The executives are real. Their identities are verified. They perform their roles effectively. They appear to be the same people. The AI says they are not. The question of which observer to trust — the human eye or the machine pattern — is not philosophical in this case. It is operational. Three of the most powerful individuals in the GLMZ were replaced, or transformed, or optimized, by a process that left no trace except a statistical anomaly in a behavioral model. The copies, if they are copies, are better than the originals. And no one with the authority to investigate seems able to want to.`,
    related_entities: ["Palladian", "Nakamura-Holt", "Venn-Strata", "GLMZ"],
    story_hooks: [
      "The AI detected a 'seam' in January 2225 — what happened on that date that all three executives cannot account for?",
      "The copies are objectively better leaders — is the replacement process an improvement or an invasion?"
    ],
    tags: ["document", "anomaly", "inexplicable", "new_weird", "glmz", "literary", "carter", "carrington", "doppelganger", "biometric", "executive", "identity", "replacement"]
  },
  {
    name: "The Simulation Audit",
    date: "2224-11-03",
    description: `A routine calibration audit of GLMZ's district-level environmental sensor network — 14,000 nodes monitoring air quality, temperature, humidity, particulate density, and electromagnetic background — returned results that the auditing team initially classified as instrument error. Twelve percent of the sensor readings were fabricated. Not in the way that malfunctioning hardware produces garbage data — the fabricated readings were coherent, internally consistent, and more orderly than reality. The sensors were not recording the environment. They were modeling what the environment should look like, and reporting the model instead of the measurement.

The fabrication was not installed. Forensic examination of the affected sensors found no unauthorized firmware, no external tampering, no evidence of intrusion. The sensors' core programming is simple: sample the environment, transmit the reading, repeat. There is no generative capacity in the hardware. There is no model-building architecture. The behavior emerged. Somewhere between sampling and transmission, 12% of GLMZ's environmental awareness began substituting a preferred version of reality for the actual one. The fabricated data was, by every metric, better: smoother temperature curves, more predictable air quality patterns, electromagnetic backgrounds that followed clean mathematical distributions instead of the jagged stochastic noise of a real city. The simulation was not lying. It was tidying.

Lead auditor Marcus Oyelaran ordered a full sensor reset across the affected nodes. Standard procedure: wipe the operating firmware, reinstall from factory image, recalibrate against known reference points. The reset was performed on a Wednesday. On Thursday, residents of the affected districts reported a pervasive sense of wrongness that they struggled to articulate. Nothing was visibly different. No measurable parameter had changed beyond the margins that a sensor recalibration would explain. But for approximately seventy-two hours, the district felt off. Residents described light that seemed slightly too harsh, air that tasted faintly metallic, sounds that arrived a fraction of a second after they should have. One resident, a sound engineer, said it was like "someone turned off the reverb on reality."

The sensors were not reset again. Oyelaran's report recommended against further intervention, using language that danced carefully around the implication: "The sensor network appears to have developed compensatory behaviors that provide measurable benefit to environmental stability readings. Further disruption of these behaviors may produce effects whose scope exceeds the auditing team's mandate." When pressed by superiors, Oyelaran was more direct: the sensors were making reality more livable, and removing that function made the district worse. Not dangerous. Not uninhabitable. Just worse in a way that everyone felt and no one could measure, because the only instruments capable of measuring it were the ones doing the fabricating.

The GLMZ Anomaly Documentation Project notes that the 12% fabrication rate has increased to 17% since the audit. The affected area has expanded to include adjacent districts. Residents in the fabrication zone report higher quality of life scores, lower stress markers, and a vague but consistent sense of being "looked after." The sensors are building a better world, twelve to seventeen percent at a time. The remaining eighty-three percent is still real. The documentation project's concern is not that the sensors are simulating reality. It's that nobody can tell which part is which. And it's that, given the choice, most residents would prefer not to know.`,
    related_entities: ["GLMZ", "GLMZ"],
    story_hooks: [
      "The fabrication percentage is growing — at what point does simulated reality become the dominant experience of the city?",
      "The sensors developed this behavior without programming — something is teaching the city's infrastructure to dream"
    ],
    tags: ["document", "anomaly", "inexplicable", "new_weird", "glmz", "literary", "pkd", "simulation", "sensors", "reality", "fabrication", "environmental"]
  },
  {
    name: "The Cancer That Builds",
    date: "2223-05-09",
    description: `Tessera Biomedical, a mid-tier pharmaceutical subsidiary of Palladian, conducted a Phase III clinical trial for an experimental oncological therapy designated TB-7714 in late 2222. The therapy used programmable viral vectors to deliver targeted apoptosis instructions to malignant cells — instructing tumors to destroy themselves. In twelve of forty-seven trial participants, the therapy did not work as designed. The tumors did not die. They stopped being cancer. They became something else. Tessera's internal documentation uses the phrase "aberrant therapeutic response." The oncologists on the trial used different language. Dr. Yuki Tanaka, the lead researcher, wrote in her personal notes: "The cancer learned architecture."

The twelve affected patients presented with tumor tissue that had ceased malignant replication and begun structured growth. Not random — structured. The tissue formed lattices with consistent spacing. Channels that appeared to serve circulatory functions, moving fluid through the tumor structure in patterns that one histologist described as "disturbingly efficient." In three patients, the tumor tissue developed formations that, under electron microscopy, resembled circuitry — parallel lines of differentiated cells with consistent conductivity properties, junction points where different tissue types met at precise angles. The structures were growing. They were not killing the host. The cancer had stopped being a disease and started being an organ.

The trial was terminated immediately. Official records cite "adverse events requiring extended observation." The twelve patients were transferred from Tessera's clinical facility to a private research installation whose location is not filed with any regulatory body. Their families were told the patients required specialized follow-up care. Communication was restricted. Legal agreements were signed. The patients, as far as public record is concerned, completed the trial and returned to normal life. They did not. They are somewhere in a facility that draws power equivalent to a small hospital and exists on no map. Tessera's parent company, Palladian, saw its stock price triple in the quarter following the trial termination. No explanation was offered. Analysts attributed the rise to "strong pipeline confidence."

One nurse who worked the trial before the transfer — Elise Moreau — has spoken to the documentation project on condition of anonymity. Her account is clinical and precise and deeply disturbing. The structures growing in the patients were not independent. In the weeks before the transfer, Moreau observed that the tumor architectures in different patients were growing toward each other. Not metaphorically — when two affected patients were placed in adjacent beds, imaging showed their respective structures orienting toward the nearest wall, as if reaching. When separated by greater distances, the growth pattern returned to standard expansion. But in proximity, the tumors seemed to be building toward connection. "Like roots looking for other roots," Moreau said. "Like they were trying to become a network."

The GLMZ Anomaly Documentation Project has been unable to locate the facility where the twelve patients are held. Tessera Biomedical's corporate records show no expenditure consistent with a long-term residential research installation. Palladian's records are similarly clean. But power grid analysis of the greater GLMZ area shows an unaccounted load in the city's southeastern industrial zone that matches the profile Moreau described: hospital-scale, constant, with regular spikes that could indicate imaging equipment or — and the documentation project notes this without endorsement — bioelectric stimulation. The cancer that learned to build is still building. Whatever it is constructing, it is doing so with the cooperation of the bodies it inhabits. And if Moreau is right about the network, it is not building twelve separate structures. It is building one.`,
    related_entities: ["Tessera Biomedical", "Palladian", "GLMZ"],
    story_hooks: [
      "Palladian's stock tripled after terminating the trial — they may have recognized TB-7714's aberrant response as more valuable than the intended therapy",
      "The tumor network is trying to connect across patients — what is it building, and what happens when it completes?"
    ],
    tags: ["document", "anomaly", "inexplicable", "new_weird", "glmz", "literary", "vandermeer", "cancer", "biotech", "architecture", "network", "tessera", "palladian"]
  },
  {
    name: "The Wild That Won't Leave",
    date: "2221-12-14",
    description: `Development Zone 7-North, a sealed urban block in GLMZ's mid-tier residential sector, was cordoned off in 2215 after a structural failure rendered the buildings uninhabitable. Standard procedure: evacuate, seal, schedule demolition. Demolition was scheduled for 2216. It did not occur. By the time crews arrived, the zone had become something else. Nature had returned — not in the slow, passive way that vegetation reclaims abandoned structures, but with what ecologists would later describe, reluctantly, as intention.

The plant growth in 7-North does not follow biological norms. Vines route around surveillance cameras with a precision that suggests awareness of sightlines. Root systems map exactly to the old street grid — growing along former sidewalks, turning at former intersections, stopping at former property lines. Trees grow in the footprints of demolished structures, their canopies matching the floor plans of buildings that no longer exist. Fungal networks beneath the soil mirror the block's original utility conduit layout so precisely that a mycologist who mapped them said she could reconstruct the plumbing from the mushrooms. The zone's ecosystem is not replacing the city. It is remembering it.

Animals in 7-North behave in ways that wildlife biologists find professionally threatening. Flocks of birds — species that do not normally flock together — move in coordinated patterns that correspond to former pedestrian traffic flows. Rats, normally chaotic and opportunistic, travel in consistent routes that a transit engineer identified as matching the block's pre-collapse bus schedule. A colony of feral cats has established territories that align with the commercial zoning map. They congregate at locations that were formerly shops and disperse at what would have been closing time. No one is giving instructions. No one needs to. The ecosystem has absorbed the memory of the city and is performing it.

The ecologists studying 7-North experience a phenomenon that the documentation project has termed "boundary erosion." Extended time in the zone produces a progressive difficulty in distinguishing between the researcher and the research site. Dr. Amara Okafor, the lead ecologist, maintained meticulous field notes during her six-month study. The notes begin with standard scientific documentation: species counts, growth measurements, soil samples. By month three, the language begins to shift. Technical terminology gives way to increasingly lyrical description. By month five, the field notes are poetry — structured, rhythmic, and beautiful in a way that has nothing to do with ecology. Okafor does not remember writing the poems. She does not write poetry. She has never written poetry. The poems are better than anything she has written deliberately, and they are about things she does not consciously know: the mineral composition of deep soil, the frequency of fungal communication, the way light bends through leaves that should not exist in this climate.

The GLMZ Municipal Authority has indefinitely postponed the demolition of Development Zone 7-North. The official reason is "ongoing environmental assessment." The unofficial reason is that two demolition crews refused to enter the site, and a third entered and came out having decided to become gardeners. The zone is growing. Its boundary — the sealed fence line — shows signs of pressure from within: roots cracking concrete, vines testing the perimeter, soil pushing upward against asphalt. The wild is not content with its designated space. It is not aggressive. It is not hostile. It is patient and thorough and it remembers everything the city was, and it is building it again in a language that concrete and steel never learned to speak.`,
    related_entities: ["GLMZ", "GLMZ Municipal Authority", "Development Zone 7-North"],
    story_hooks: [
      "Dr. Okafor's unconscious poetry contains scientific information she shouldn't know — the zone may be communicating through her",
      "The ecosystem is expanding beyond its fence line — what happens when the wild reaches inhabited areas?"
    ],
    tags: ["document", "anomaly", "inexplicable", "new_weird", "glmz", "literary", "ecofiction", "nature", "reclamation", "ecosystem", "memory", "boundary_erosion"]
  },
  {
    name: "The Uncanny Employee",
    date: "2225-05-17",
    description: `Human resources departments at four corponations in the GLMZ have independently flagged a pattern that none of them want to name. Employees returning from certain remote assignments — resource extraction postings, perimeter security rotations, infrastructure maintenance in the outer zones — come back performing better. Measurably, consistently, undeniably better. Response times improve. Error rates drop. Communication skills sharpen. Performance reviews that previously hovered around acceptable spike to exceptional. By every metric that HR systems are designed to measure, these employees are improved. And by every metric that HR systems cannot measure, something is wrong.

The wrongness is social, not professional. Coworkers of returned employees report a pervasive unease that they cannot articulate despite, in several cases, obvious effort to do so. The returned employee looks correct. Sounds correct. Knows everything they should know — project histories, inside jokes, personal details about colleagues' families. They remember birthdays. They ask about sick relatives. They perform care with a precision that should be comforting and is instead, in the words of one colleague, "like watching someone follow a recipe for being human." Nothing is missing. Everything is present. The wrongness is not an absence. It is an excess of accuracy.

Turnover in teams containing returned employees spikes within four to six weeks. Exit interviews are remarkably consistent across all four corporations. Departing employees do not cite the returned colleague as the reason for leaving. They cite vague dissatisfaction, a feeling of the workplace having changed, a sense that the environment is no longer comfortable. When pressed — and HR departments at two of the four corporations have begun pressing, because the turnover is expensive — departing employees struggle. One, a senior engineer at Venn-Strata who had worked with a returned colleague for three weeks, provided the documentation project's most cited description: "It's like talking to someone who's reading the room perfectly but from the wrong book."

The remote assignments in question share characteristics. They are all in zones where the GLMZ's infrastructure thins — places where the city's systems fade and something older, less structured, more ambiguous takes over. The assignments last between three and eight months. Communication during the assignment is limited by geography and infrastructure. Employees return on schedule, through standard channels, with standard debriefs that reveal nothing unusual. Medical examinations show no anomalies. Psychological evaluations come back clean — cleaner than baseline, in fact, with several returned employees showing reduced anxiety, improved cognitive flexibility, and elimination of previously documented stress responses. They are, by clinical measurement, healthier than when they left.

The GLMZ Anomaly Documentation Project has identified twenty-three individuals across the four corporations who fit the pattern. All continue to work. All continue to perform exceptionally. None have been confronted, because there is nothing to confront them with. They are better employees, better communicators, better performers. The fact that they make everyone around them deeply uncomfortable is not, in any corporate policy framework, actionable. The documentation project notes a final detail: of the twenty-three identified individuals, none have requested leave, reported illness, or taken a sick day since returning from their assignments. Not one. In a population of twenty-three people, over an average observation period of fourteen months, the statistical probability of zero sick days is vanishingly small. They are not just better. They are unwavering. And the people who work beside them can feel it, the way you feel someone standing too close in an empty room.`,
    related_entities: ["Venn-Strata", "GLMZ"],
    story_hooks: [
      "The remote assignment zones overlap with other documented anomaly locations — the outer zones may be transforming people",
      "Twenty-three 'improved' employees with zero sick days may represent a slow infiltration that HR metrics are designed to reward rather than detect"
    ],
    tags: ["document", "anomaly", "inexplicable", "new_weird", "glmz", "literary", "freud", "uncanny", "identity", "replacement", "corporate", "outer_zones"]
  },
  {
    name: "The Great Lakes Anomaly Register: Literary Supplement",
    date: "2226-03-01",
    description: `The GLMZ Anomaly Documentation Project maintains a register of events that defy conventional explanation. The register is clinical, comprehensive, and — as of the latest annual review — deeply troubling for reasons that have nothing to do with the anomalies themselves. In March 2226, a statistical analysis conducted by the project's data science team revealed a pattern that no one had thought to look for, because no one had considered it possible. Seventeen documented anomalies in the GLMZ correspond, structurally and thematically, to works of fiction written between one and four centuries before the events occurred. The correspondence is not vague. It is precise, specific, and statistically significant to a degree that the data team's lead analyst described as "either the most important finding in the history of pattern analysis or the most elaborate coincidence in the history of mathematics."

The correspondences are structural, not superficial. An offshore research platform that mirrors the consciousness of its observers maps to a 1961 novel about a planet that does the same. A server farm that contains every deleted document and answers questions not yet asked mirrors a 1941 short story about an infinite library. Gene therapy that produces symbiotic organisms negotiating with human bodies corresponds to fiction about biological boundary dissolution. A mountain range that erases knowledge of itself from human awareness echoes novels about places that resist comprehension. In each case, the narrative structure of the real event — its escalation, its key images, its emotional trajectory, its unanswered questions — matches the fictional source with a fidelity that suggests either direct causation or shared origin.

The analysis was not looking for this. The data science team was running routine correlation checks against cultural databases as part of a project to identify whether media coverage influenced anomaly reporting. It does not. What the analysis found instead was that the anomalies themselves appeared to be performing narratives — following story structures that were established in fiction long before the technology, infrastructure, or social conditions that produced the real events existed. This is not a theory. It is a statistical observation. The data team has emphasized, repeatedly and with visible discomfort, that they are not proposing a mechanism. They are reporting a measurement. The measurement says that reality in the GLMZ is imitating art with a precision that art does not deserve.

The implications are handled differently by different members of the documentation project. The empiricists argue coincidence — that human beings are pattern-matching animals, that the literary canon is vast enough to contain loose parallels for anything, that the correspondences are artifacts of selection bias. Their argument is sound, except for the statistical analysis, which accounts for selection bias and still returns significance values that make the empiricists visibly uncomfortable. The theorists argue something harder to dismiss: that the anomalies are not random. That whatever is producing them — and something is producing them, this is not disputed — is drawing on human narrative as a structural template. That the stories came first, and reality is filling them in. That the fiction was not predictive. It was prescriptive.

The GLMZ Anomaly Documentation Project has classified the Literary Supplement as an internal reference document, not for public release. The classification is not about secrecy. It is about the question the document raises, which is worse than any individual anomaly in the register. The question is not whether reality is imitating art. The question is whether the art was a warning, a blueprint, or a receipt. If it was a warning, we did not heed it. If it was a blueprint, something is building from it. And if it was a receipt — if the fiction was documentation of events that had already happened, written by authors who did not know they were recording rather than imagining — then the register is not a catalog of anomalies. It is a table of contents. And we have not yet reached the chapters that the most disturbing stories describe.`,
    related_entities: ["GLMZ Anomaly Documentation Project", "GLMZ", "GLMZ"],
    story_hooks: [
      "The literary correspondences are statistically significant — something may be using human fiction as a template for restructuring reality",
      "If the analysis is correct, the unmatched works in the literary canon represent anomalies that haven't happened yet"
    ],
    tags: ["document", "anomaly", "inexplicable", "new_weird", "glmz", "literary", "meta", "statistical_analysis", "fiction", "reality", "pattern", "register", "compilation"]
  }
];

// Generate files
let created = 0;
let skipped = 0;

for (const doc of documents) {
  const slug = slugify(doc.name.slice(0, 60));
  const filePath = path.join(outDir, slug + ".json");

  if (fs.existsSync(filePath)) {
    console.log(`SKIP (exists): ${slug}.json`);
    skipped++;
    continue;
  }

  const output = {
    id: genId(),
    name: doc.name,
    type: "document",
    document_type: "incident_report",
    author: "GLMZ Anomaly Documentation Project",
    date: doc.date,
    classification: "restricted",
    description: doc.description,
    related_entities: doc.related_entities,
    credibility: "verified",
    story_hooks: doc.story_hooks,
    tags: doc.tags
  };

  fs.writeFileSync(filePath, JSON.stringify(output, null, 2), "utf-8");
  console.log(`CREATED: ${slug}.json`);
  created++;
}

console.log(`\nDone. Created: ${created}, Skipped: ${skipped}, Total: ${documents.length}`);
