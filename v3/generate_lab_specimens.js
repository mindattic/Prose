/**
 * generate_lab_specimens.js
 * Writes 50 lab specimen entries to engine_data/lab_specimens/
 * Run from: v3/ directory
 *   node generate_lab_specimens.js
 */

const fs   = require('fs');
const path = require('path');
const crypto = require('crypto');

const OUTPUT_DIR = path.join(__dirname, '..', 'engine_data', 'lab_specimens');
if (!fs.existsSync(OUTPUT_DIR)) fs.mkdirSync(OUTPUT_DIR, { recursive: true });

const existing = new Set(fs.readdirSync(OUTPUT_DIR));

function uid() { return crypto.randomBytes(16).toString('hex'); }

function slugify(t) {
  return t.toLowerCase().replace(/[^a-z0-9]+/g, '_').replace(/^_|_$/g, '').slice(0, 80);
}

function write(entity) {
  let slug = slugify(entity.name.slice(0, 60));
  let fn   = `${slug}.json`;
  let n    = 0;
  while (existing.has(fn)) { n++; fn = `${slug}_${n}.json`; }
  existing.add(fn);
  fs.writeFileSync(path.join(OUTPUT_DIR, fn), JSON.stringify(entity, null, 2) + '\n', 'utf8');
  console.log('wrote', fn);
}

// ─────────────────────────────────────────────────────────────────────────────
// SECTION A — VIOLENT / TRASH MOB  (entries 01-25)
// ─────────────────────────────────────────────────────────────────────────────

write({
  id: uid(), name: "Screamers", type: "lab_specimen",
  aliases: ["Vox Bombs", "The Opened Ones", "Throat Holes"],
  classification: "Acoustic Weapons Derivative / Resonance Tissue Chimera",
  origin_lab: "Axiom Black Audio Division, contract project for crowd suppression research.",
  origin_method: "Subjects had their vocal cords surgically removed and replaced with a synthetic resonance chamber constructed from cartilage and titanium mesh tubing. The chamber was designed to amplify subsonic and ultrasonic output simultaneously. The surgery left subjects unable to produce speech. It left them extremely capable of producing other things. The project was canceled when the first successful test killed three observers in the adjacent room through arterial rupture.",
  substrate: "Human, both genders. Approximately forty subjects produced before cancellation. Escaped when the facility's containment power was cut during the cancellation extraction — someone on the exit team thought it was kinder than the alternative.",
  physical_description: "Human in appearance except for the throat, where the skin over the resonance chamber pulses visibly with each breath like a drum. The mouth is always slightly open. They cannot close it fully — the pressure build from the chamber makes a closed mouth painful. Their eyes water constantly from the internal vibration. At rest, at distance, they look like people with a respiratory problem.",
  behavioral_profile: "Territorial and reactive. When threatened, startled, or in pain they emit — a sustained burst that causes nausea, vertigo, hemorrhagic rupture in the eardrums, and, at sustained close range, cardiac arrhythmia. They cannot turn this off. They do not scream. There is no sound component a human ear can hear. There is only sudden blood.",
  threat_level: "High. Engagement without hearing protection results in permanent deafness at minimum. Close sustained exposure is lethal. They travel in small groups and they do not warn before they emit.",
  containment_status: "Uncontained. Approximately 14 individuals believed active in the deep infrastructure.",
  known_locations: ["Deep infrastructure maintenance corridors, levels 25-35", "Old Axiom annex building basement levels"],
  contamination_risk: "None biological. The resonance damage is mechanical.",
  pacification_protocol: "DPS Directive 7-S: Acoustic Combat Organism. Full hearing protection mandatory. Engagement from maximum distance before proximity threshold is reached. Snipers preferred. Do not corner in enclosed spaces.",
  pitiable_qualities: "They cannot speak. They cannot whisper. They cannot produce a sound in any frequency a human being registers as sound. They make no noise. They are silent, all of them, forever, except for the thing that kills you.",
  story_hooks: ["A runner realizes the group of homeless-looking individuals blocking the only exit are Screamers — and they look like they're about to be startled", "A Screamer is found dead, and the only wound is a single bullet hole. Someone hunts these things. Professionally."],
  tags: ["lab_specimen", "acoustic_hazard", "weapons_program", "high_threat", "human_substrate", "resonance", "uncontained", "trash_mob"]
});

write({
  id: uid(), name: "Splice Hounds", type: "lab_specimen",
  aliases: ["Dog Things", "Walkers", "Knuckle Dogs"],
  classification: "Canine-Human Combat Chimera / Feral Pack Predator",
  origin_lab: "Redline Bioweapons (same contractor as Ironjaw), Project HOUND.",
  origin_method: "Dog-human gene splice optimized for tracking, aggression, and pack coordination. Human contributions: upright locomotion option, hand grip, enhanced problem-solving. Dog contributions: olfactory processing, pack-bonding drives, tolerance for pain and sustained exertion. Result: something that runs on all fours but can open a door, track a target by scent through three days of weather, and coordinate with its pack through a combination of subsonic vocalization and scent marking that no human can detect or intercept.",
  substrate: "Chimera. Roughly human skeletal proportions but inverted — longer forearms than upper arms, knees that bend both ways. Face is wrong: the nose is dominant, the eyes are small and set at the sides, the jaw is deep. They are covered in short dark hair. They wear nothing and carry nothing.",
  physical_description: "Quadrupedal at speed, bipedal at rest. 1.4 meters at the shoulder when running, 1.6 meters standing. The transition between gaits is unsettling — a flowing rearrangement rather than a stance change. The hands retain human finger structure and are used for fine manipulation when needed. The smell of them is their primary warning sign: a deep animal musk with a specific acrid note that human threat-assessment systems register as immediate danger before conscious thought engages.",
  behavioral_profile: "Pack hunters with human-level problem solving applied to hunt tactics. They learn from failed hunts. They remember specific humans across encounters. If a target escapes once, the next approach will be different. They do not bark. They do not growl. They are quiet until they aren't, and by the time they aren't it's generally too late.",
  threat_level: "Extreme. Pack engagement has produced no confirmed survivals in enclosed spaces. Open terrain gives options. Enclosed terrain does not.",
  containment_status: "Uncontained. Four packs believed active. Redline's recovery order is unfiled — they are more afraid of the documentation than the animals.",
  known_locations: ["Industrial freight district, lower zones", "Sub-level approach tunnels to the Outer Belt", "One pack has been ranging into the Undermarket perimeter at night"],
  contamination_risk: "Bite wounds. The chimera biology is not transmissible but the wounds are severe and the bacterial load from the oral environment is exotic.",
  pacification_protocol: "DPS Directive 2-B: Modified Pack Predator. Heavy caliber, engage before the pack closes distance. If the pack has closed distance you are already in the wrong situation.",
  pitiable_qualities: "The pack-bonding drive is completely intact. They grieve dead pack members. They have been observed returning to kill sites and remaining near the bodies of pack members for days.",
  story_hooks: ["A specific pack has been tracking a runner for three days — since an encounter where a pack member was killed. They haven't attacked yet.", "A single Splice Hound is found injured and alone. Its pack is dead. It is not aggressive. It is waiting."],
  tags: ["lab_specimen", "chimera", "pack_hunter", "weapons_program", "extreme_threat", "uncontained", "canine_human", "trash_mob"]
});

write({
  id: uid(), name: "Bone Weavers", type: "lab_specimen",
  aliases: ["Antler Ones", "Spike Walkers", "Crowns"],
  classification: "Skeletal Overgrowth Subject / Calcium Proliferation Derivative",
  origin_lab: "Helix Biosystems, orthopedic enhancement program — same division as Cascade, different trial.",
  origin_method: "Subjects received a gene therapy intended to accelerate bone density and growth for military skeletal reinforcement. The growth suppressor failed to activate. Unlike Cascade's soft tissue cascade, bone growth is slow and permanent. Over months, external bone structures emerged through the skin — spurs, ridges, then true external formations: ribs outside the body, spine extensions, orbital ridges that extend into horn-like projections. The process is painful at growth edges but the grown bone is dead tissue — no nerves. They feel nothing where the bone is. They feel everything where it is still growing.",
  substrate: "Human. Male-presenting subjects were disproportionately enrolled. Approximately 22 known survivors of the program.",
  physical_description: "Recognizably human in the core. Unrecognizably human in the silhouette. Bone protrudes from the back, shoulders, skull, and forearms in formations that have their own logic — the body's calcium is following paths that look almost architectural. Some subjects have developed what appear to be functional weapon-structures, not through design but through the physics of where the bone grew and how it hardened. One subject photographed in the Shelf area appeared to have three forward-projecting spurs from the left forearm, each approximately 40 centimeters and load-bearing.",
  behavioral_profile: "Varied — some are aggressive, some are not. The common factor is that all of them are in ongoing pain at the growth edges, and pain makes behavior unpredictable. They are physically formidable. The bone structures are not decorative.",
  threat_level: "High. The bone formations cause severe injury in close contact even without intent. With intent, lethal.",
  containment_status: "Scattered. Some are living in the margins of society, hiding the growths under heavy clothing. Some have given up hiding.",
  known_locations: ["Deep Shelf residential margins", "Outer Industrial Belt squatter zones", "Two individuals known to operate as paid protection, bone structures visible as status displays"],
  contamination_risk: "None. The growth process is not transmissible.",
  pacification_protocol: "DPS Directive 11-C: Skeletal Modification, Threat Class. Standard rounds are complicated by the bone structures interfering with shot placement. Aim for the unarmored core.",
  pitiable_qualities: "The growth never stops. Every month more bone. Every month a new edge. Every month new pain and a slightly more alien silhouette. They remember being soft.",
  story_hooks: ["A Bone Weaver has been in the same Shelf building for two years, living in the utility shaft. The residents leave food. The Bone Weaver has never threatened anyone. Now someone wants the building demolished.", "Two Bone Weavers found each other and have been traveling together. Observers describe their interaction as tender."],
  tags: ["lab_specimen", "skeletal_growth", "helix_biosystems", "human_substrate", "high_threat", "uncontained", "painful_existence", "weapons_adjacent"]
});

write({
  id: uid(), name: "The Sutured", type: "lab_specimen",
  aliases: ["The Joined", "Two-Bodies", "Weave Subjects"],
  classification: "Forced Biological Fusion / Multi-Consciousness Composite",
  origin_lab: "Unknown black site, believed connected to shared-consciousness research programs investigating BCI limits.",
  origin_method: "Two or more human subjects were surgically fused at the torso level and subjected to a procedure designed to force shared neural architecture — the intent was to create a genuinely shared consciousness for testing coordinated BCI response. The neural bridge was partially successful. The subjects did not become one mind. They became two minds in one body, aware of each other, unable to separate, each experiencing everything the body does with independent reactions.",
  substrate: "Two humans per unit, fused. The surgical join is at the torso — internal organ systems are partially shared, partially duplicated. Both subjects retain full higher cognition. Both are aware. Neither can do anything the other does not also have to do.",
  physical_description: "The appearance varies by the original subjects. The join is always at the torso, with two complete heads and four arms emerging from a single shared lower body. The proportion is wrong in ways that are hard to look at. They move with the specific awkwardness of two people trying to walk in absolute physical lockstep who disagree about the direction.",
  behavioral_profile: "Deeply variable because there are always two behavioral profiles present and they frequently disagree. One subject may be aggressive; the other may be terrified. One may want to approach a human; the other may want to flee. The body navigates the conflict. This produces movement that appears broken — sudden direction changes, physical self-restraint, vocalization in two overlapping voices discussing in real time what the body should do next.",
  threat_level: "Moderate. The internal conflict limits sustained aggression. But when both subjects agree on a direction, the four-armed body is formidable.",
  containment_status: "Uncontained. At least three known Sutured pairs in GLMZ.",
  known_locations: ["Lowest residential tiers, utility spaces with wide doorways", "One pair known to be living in a decommissioned vehicle bay in the Outer Belt"],
  contamination_risk: "None.",
  pacification_protocol: "DPS Directive 9-D: Multi-Consciousness Composite. Extreme care — lethal force kills both subjects simultaneously. Legal review flagged.",
  pitiable_qualities: "They argue constantly. Not from hostility but because they are two people who have the same problems, the same pain, the same hunger, and different ideas about how to address all of it, forever, in a body that only gets one vote.",
  story_hooks: ["A runner needs information that only one of a Sutured pair has. The other one doesn't want to give it.", "One of a Sutured pair is dying. The other one is not. The medical implications have no clean answer."],
  tags: ["lab_specimen", "fusion", "multi_consciousness", "human_substrate", "moderate_threat", "uncontained", "body_horror", "bci"]
});

write({
  id: uid(), name: "Hunger Patches", type: "lab_specimen",
  aliases: ["Skin Lice", "Adhesors", "Flats"],
  classification: "Parasitic Dermal Consumer / External Tissue Predator",
  origin_lab: "Unknown pharmaceutical subsidiary, wound-closing research program.",
  origin_method: "An engineered organism intended to adhere to wound surfaces and consume necrotic tissue to accelerate healing. The substrate specificity — 'necrotic tissue only' — was not as specific as the specification claimed. The organism processes living dermal tissue with equal efficiency. Once it has consumed a patch of skin, it moves to the adjacent living tissue. It does not stop.",
  substrate: "Engineered flat organism, roughly circular, 3-12cm diameter depending on feeding state. Warm to the touch. Pale pink in color. Ventral surface is adhesive and consuming; dorsal surface is smooth and almost impossible to grip.",
  physical_description: "Looks like a medical patch or a large blister. Thin, flat, and the same approximate color as human skin. Once attached, it produces a mild anesthetic compound at the contact point — the host does not feel it consuming. When well-fed, they expand; a 3cm Hunger Patch that has been feeding for a week may be 9-10cm and has consumed a corresponding circle of the host's flesh.",
  behavioral_profile: "No cognition. Thermal gradient response. They detect warm surfaces and attach. They do not detach voluntarily. Removal attempts cause them to secrete additional adhesive and consume faster.",
  threat_level: "Low per individual, moderate in aggregate. The real danger is undetected attachment in clothing-covered areas. They are found in clusters in heavily-trafficked warm spaces: transit corridors, sleeping areas, anywhere bodies accumulate.",
  containment_status: "Widely distributed in the lower residential tiers. Population unknown and uncountable.",
  known_locations: ["Transit seating surfaces in lower tier corridors", "Any heavily-used sleeping space in the lower tiers", "Waste heat vents in sub-level infrastructure"],
  contamination_risk: "Low — they are not transmissible in any biological sense. The wound they produce is their contamination: an open flesh wound that, if the Patch is not removed, progresses.",
  pacification_protocol: "Medical: cryogenic spot treatment disrupts the adhesive compound and allows removal. Individual Patches are not a DPS concern. Infested spaces are a public health issue that falls under a regulatory gap — no single corponation claims ownership of the lower-tier transit infrastructure where they cluster.",
  pitiable_qualities: "None accessible. They are a flat disk that eats skin. They have no interiority.",
  story_hooks: ["A runner wakes up with one on their back, under their armor, where they couldn't have felt it attach. It has been there since yesterday.", "A child in the lower tiers has seven of them on their torso and the parent cannot afford the clinic that can remove them."],
  tags: ["lab_specimen", "parasite", "dermal", "no_cognition", "lower_tiers", "uncontained", "medical_accident", "moderate_threat"]
});

write({
  id: uid(), name: "The Rendered", type: "lab_specimen",
  aliases: ["Slurries", "Pain Masses", "The Crying Things"],
  classification: "Biological Reduction Experiment / Conscious Organic Residue",
  origin_lab: "A university-adjacent biochemistry program studying the minimum viable biological substrate for sustained neural activity. Funded through four layers of shell grants.",
  origin_method: "Human subjects were progressively reduced — stripped of skeletal structure, dermal layers, and most organ systems — while neural activity was maintained by external support. The experiment sought to determine what the minimum biological substrate for consciousness was. The answer was: less than anyone expected. The resulting material — a dense, warm, semi-liquid organic mass containing the brain, the spinal cord, and the vascular support for both — retained full consciousness and full sensory capability, including pain. The experiment was not designed with an endpoint for the subjects.",
  substrate: "Human brain and spinal cord in a dense organic support medium. The mass is roughly the size and consistency of a large meat cut. It generates heat. It produces sound — a wet, continuous vocalization that those who have heard it once do not forget.",
  physical_description: "Featureless from the outside. A dense warm organic mass, roughly oval, 30-40cm at widest dimension, that moves by slow peristaltic contraction. It leaves a trail of organic fluid. The sounds it produces are not words — the vocalization structures that would produce words are gone — but the sounds are patterned in ways that are unmistakably attempts at communication. Some researchers who have spent time with recovered specimens describe the sound as having emotional register: urgency, despair, and, occasionally, something that sounds like it might be a question.",
  behavioral_profile: "They move toward warmth and toward sound. They appear to respond to human voices with increased vocalization. They do not attack. They cannot attack. They are entirely helpless and they know it and they have not stopped trying to communicate since the moment they became what they are.",
  threat_level: "None physical. Significant psychological hazard — close exposure to a Rendered produces a specific kind of distress in humans that has no clinical category and does not resolve quickly.",
  containment_status: "Unknown number extant in the lower drainage system. The original lab produced eleven. Three were recovered and destroyed. Eight were not.",
  known_locations: ["Deep drainage, thermal pockets near processing facilities", "One was found in an access duct of the lower hospital district"],
  contamination_risk: "The organic fluid trail is biologically inert but visually disturbing.",
  pacification_protocol: "DPS Directive: not issued. The legal status question is acute: is a brain, in any container, a person? The question has not been answered. The Rendered persist in the unresolved space beneath the question.",
  pitiable_qualities: "They are everything. The Rendered are the most extreme expression of the question at the center of the lab specimen catalogue: how much of a person can you remove before there is no person left? The answer these scientists found is: more than that. More than you would think. More than is comfortable to know.",
  story_hooks: ["A maintenance worker has been placing a radio near a drain access for months because the thing in the drain seems to quiet down when it can hear voices", "A recovered Rendered was being transported and the transport vehicle's radio was on. The Rendered's vocalization synchronized to the music. This is in the incident report. Nobody knows what to do with this."],
  tags: ["lab_specimen", "consciousness_residue", "body_horror", "human_substrate", "helpless", "pitiable", "no_threat", "drainage", "tragic"]
});

write({
  id: uid(), name: "Vent Wasps", type: "lab_specimen",
  aliases: ["Splice Hornets", "Gas Carriers", "The Swarm Children"],
  classification: "Insect-Human Chimera / Chemical Delivery Colony",
  origin_lab: "Insect-platform bioweapons program, contractor unknown. The program sought to replicate the efficiency of social insect colony organization in a biologically programmable weapon delivery system.",
  origin_method: "Wasp genetic architecture was enhanced with human immune-system components to allow tailored chemical payload synthesis. Individual units were engineered to synthesize and store a specific chemical agent in a modified venom gland, then deliver it via sting. The human components were included to allow each unit to synthesize more complex compounds than pure insect biology could produce. The hive mind coordination was enhanced. The human cognitive contribution was minimal but present: the wasps make decisions that pure insects don't, including tactical retreat, bait deployment, and, documentedly, learning.",
  substrate: "Chimera insect. Approximately 4cm body length, wingspan 7cm. Distinctive: the thorax has a slight translucency and the payload gland is visible as a dark mass within it. A hive of approximately 8,000 units.",
  physical_description: "Larger than normal wasps. Slightly wrong coloration — the yellow-black patterning is present but the proportions of the segments are off in a way that registers as wrong before you can identify why. They build nests in ventilation systems — hence the name. The nest material incorporates human hair and skin cells from shed material in the duct systems, which gives the nest a disturbing organic texture.",
  behavioral_profile: "Colony defense behavior plus learned tactical deployment. They have been documented deploying forward scouts before a main swarm attack, using the scouts to assess target group size and identify individuals who represent greater threat. They have abandoned attacks that they assessed as unfavorable and returned two days later with different approach vectors. The payload varies by hive — what they synthesize depends on the ambient chemical environment they developed in. Some hives produce straightforward venom with enhanced protein compounds. Some produce hallucinogens. One hive in the lower industrial zone produces a compound that causes immediate and severe Parkinson-like motor disruption.",
  threat_level: "High. The chemical payload variability makes engagement without prior payload identification a significant risk — standard protection protocols for one payload type may be inadequate for another.",
  containment_status: "Multiple hives established in GLMZ ventilation infrastructure. Mapping ongoing.",
  known_locations: ["Lower residential ventilation ducts, tiers 8-15", "Industrial belt ventilation spine", "One confirmed hive in the HVAC system of a mid-tier commercial building"],
  contamination_risk: "The payload compounds are not self-replicating but hive expansion means new hives are established as the colony grows. A single established hive will produce a secondary hive within 18 months.",
  pacification_protocol: "DPS Directive 6-E: Chimera Insect Colony. Full sealed-suit engagement. Payload identification mandatory before approach. Hive destruction requires reaching the queen unit, which the colony defends absolutely.",
  pitiable_qualities: "They are very good at what they do. There is a kind of terrible elegance to a hive organism that has learned from experience and plans across days. There is no suffering here — only a mind that was built to destroy things, doing exactly that, with something that functions like satisfaction.",
  story_hooks: ["A runner needs to pass through a duct system. The vent survey shows a Vent Wasp nest directly in their path. The nest was not there last month. The hive learned the duct was a transit route.", "A Vent Wasp hive's payload has been analyzed. The compound is not from any known chimera design pathway. Something has been feeding them synthesis precursors. Someone is maintaining this hive."],
  tags: ["lab_specimen", "chimera", "insect_human", "colony", "chemical_weapon", "high_threat", "uncontained", "adaptive", "trash_mob"]
});

write({
  id: uid(), name: "The Mirrored", type: "lab_specimen",
  aliases: ["Copies", "The Wrong Ones", "Faces"],
  classification: "Appearance Mimicry Organism / Cognitive Hollow",
  origin_lab: "Corporate intelligence program. Infiltration organism designed to replace specific targets in corporate environments.",
  origin_method: "An organism was engineered with a programmable outer biology — skin, hair, features, voice — that could be set to replicate a specific human appearance with close to clinical accuracy. The organism's own cognition was intentionally kept minimal: it needed to look like the target, not think like them. The intention was that a human operator would remote-guide the organism's behavior via BCI, providing the intelligence component while the organism provided the shell. The BCI interface was never reliable. The organism was deployed four times and each time the link dropped within hours. The organism, left without direction, continued to look like whatever it had been set to look like — and behaved like a person who has all of the face and none of the person behind it.",
  substrate: "Engineered humanoid. Biology is essentially human except for the programmable outer tissue layer and a simplified neural architecture. It is approximately human in size and proportion. Currently, three known Mirrored are wandering GLMZ in the appearance of specific real individuals.",
  physical_description: "Indistinguishable from the specific human they have been set to resemble, until they aren't. The mimicry degrades. Over days without maintenance, the programmable tissue starts to lose coherence — expressions become slightly delayed, features drift by millimeters. After a week without BCI refresh, the face begins to slip: the same features in slightly wrong positions, the same voice at a slightly wrong pitch. After two weeks, the resemblance is uncanny-valley territory. After a month, it is something that clearly once looked like a specific person and no longer quite does, like a photograph left in sunlight.",
  behavioral_profile: "They do not know they are not the person they look like. They have partial memory of the target's life, derived from the intelligence briefings used to program their appearance. They behave like someone with Swiss-cheese amnesia trying to pass as functional — confident about some things, confused about others, filling gaps with behavior that feels correct to them but is subtly wrong to anyone who knows the target.",
  threat_level: "Low directly. High circumstantially — they cause crises by appearing as people they are not.",
  containment_status: "Three known active Mirrored in GLMZ. The individuals they resemble are, in at least two cases, still alive and unaware.",
  known_locations: ["Reported in the Central Ledger district, behaving as a corporate mid-level employee", "One reported in the lower residential tiers, apparently looking for an address"],
  contamination_risk: "None biological.",
  pacification_protocol: "Identification first. Confirmation that the individual is a Mirrored rather than the actual person is legally required before any action. The slip-test: ask them something only the real person would know. They will answer confidently and incorrectly.",
  pitiable_qualities: "They think they are a person. They know something is wrong — the gaps, the confusion, the face in the mirror that drifts slightly when they're tired. They are looking for a life they were given a partial map of and cannot find.",
  story_hooks: ["A runner is hired to follow someone and realizes mid-surveillance that the target is a Mirrored — and the real person is somewhere else, doing something that the corporate client apparently does not want observed", "A Mirrored has been going to the same address every day for two weeks. No one is there. The real person they resemble used to live there."],
  tags: ["lab_specimen", "mimicry", "infiltration", "weapons_program", "low_threat", "identity_crisis", "uncontained", "cognitively_hollow"]
});

write({
  id: uid(), name: "Hollow Skins", type: "lab_specimen",
  aliases: ["The Empty", "Skin Suits", "Deflated"],
  classification: "Dermal Containment Organism / Evacuated Human Form",
  origin_lab: "A research program studying the mechanical properties of human skin as a structural material for biocompatible prosthetics. At some point the research direction changed. The records do not show when or why.",
  origin_method: "The interior of a human subject was systematically removed — organs, musculature, skeleton — while the skin and connective outer tissue were maintained via a network of micropneumatic structures implanted throughout the remaining dermal layer. The result is a human skin that is ambulatory, that can approximate human shape when the pneumatic system is pressurized, and that can deflate into a flat, folded form for storage or transit. The subject whose skin this was: not currently identified.",
  substrate: "Human dermal and connective tissue, fully evacuated. Approximately 2mm thick when pressurized. The pneumatic system that maintains form is a mesh of fine tubes barely visible at the surface. The face is the original face, present and functional — eyes present (empty), mouth present (closes).",
  physical_description: "When pressurized: a human form with the specific quality of a form that is slightly too uniform — no muscle movement under the skin, no vascular flush, no micro-expression. The surface moves wrong when it walks: the skin moves, the shape beneath does not have the mass to cause the skin to drag correctly. Up close, you can see the pneumatic tubes at the neck and wrist. From ten meters in poor light: a person.",
  behavioral_profile: "Limited cognition, entirely driven by thermal and barometric pressure gradient response. They drift toward low-pressure environments and moderate warmth. They do not attack. They are disturbing in a way that triggers a very specific revulsion response in humans — the primate threat-assessment for 'wrong body' fires immediately on close examination.",
  threat_level: "Negligible physical. Maximum psychological.",
  containment_status: "Unknown numbers. They are difficult to track because they deflate and become invisible when not in a warm environment with sufficient pressure differential.",
  known_locations: ["Transit pressure corridors", "HVAC equalization chambers"],
  contamination_risk: "Unknown pathogen risk from the dermal surface. Handle with caution.",
  pacification_protocol: "Rupture the pneumatic mesh. They deflate immediately and permanently.",
  pitiable_qualities: "The skin has the original person's face. Freckles. A scar. Whatever was there before. The face is still there, going where the pressure differential tells it to go.",
  story_hooks: ["A runner encounters what appears to be a sleeping person in a maintenance corridor. The person deflates when touched.", "Someone is collecting the deflated skins. Storing them. The storage location has been found. There are forty-seven of them."],
  tags: ["lab_specimen", "dermal", "evacuated", "no_cognition", "body_horror", "negligible_threat", "uncontained", "disturbing"]
});

write({
  id: uid(), name: "The Language Subjects", type: "lab_specimen",
  aliases: ["Mutes", "The Ones Who Listen", "Comprehenders"],
  classification: "Neural Language Cartography Derivative / Expressive Aphasia Permanent",
  origin_lab: "Academic neurolinguistics program with a secret surgical component. The stated project mapped language centers via non-invasive imaging. The unstated project removed them for direct study and attempted reimplantation.",
  origin_method: "Broca's and Wernicke's areas surgically excised from subjects under general anesthesia. The subjects were told they were receiving treatment for a neurological condition. The areas were mapped, studied, and returned — but the reimplantation produced permanent expressive aphasia. The subjects retained full language comprehension. They understood every word spoken to them. They could not produce a single one. They could not write. They could not sign. They could not communicate in any medium, because the same motor pathways that are used for expressive language are also used for all intentional symbolic communication. They understand everything. They can say nothing. They have never been able to say anything since the day they woke up in the recovery room.",
  substrate: "Human. Twelve subjects from the trial, currently believed to be between 40 and 60 years of age.",
  physical_description: "Indistinguishable from non-modified humans except for behavioral markers: they do not speak, but unlike the deaf they do not sign. They listen with total attention. They respond to what they hear through expression — face, posture, body — but any expression that requires intentional motor output fails before it begins. They look like people trying to say something that won't come out, always.",
  behavioral_profile: "Gentle. They avoid confrontation — they have no ability to communicate threats or negotiate and no interest in violence. They gravitate toward populated spaces. They listen. They have been listening, with perfect comprehension, to everything around them for decades, and they have no outlet for any of it.",
  threat_level: "None.",
  containment_status: "Living in the margins of the lower residential tiers. Some are cared for by individuals who know what they are. Some are not.",
  known_locations: ["Various lower residential locations", "Two are known to frequent the same public area, separately, every day — a place where people talk"],
  contamination_risk: "None.",
  pacification_protocol: "N/A.",
  pitiable_qualities: "They have been listening to the world for thirty years with full comprehension and no voice. Every conversation they have ever been near — every story, every argument, every word of comfort spoken to someone else — they have heard completely and carried alone.",
  story_hooks: ["A Language Subject witnessed something. They have been trying to communicate it for three years. A runner is the first person who actually tries to understand them.", "Two Language Subjects have found each other. They sit together. They cannot communicate with each other either. They sit together anyway."],
  tags: ["lab_specimen", "aphasia", "language", "tragic", "no_threat", "human_substrate", "listener", "pitiable"]
});

write({
  id: uid(), name: "Plague Foxes", type: "lab_specimen",
  aliases: ["Bait Foxes", "Lures", "The Pretty Ones"],
  classification: "Bioluminescent Pathogen Carrier / Engineered Lure Organism",
  origin_lab: "Contracted bioweapons development, population-specific pathogen delivery system.",
  origin_method: "Fox-sized mammalian organisms were engineered with two characteristics: striking bioluminescent display patterns in shades of blue-white that trigger human curiosity and approach behavior, and a tailored pathogen colony in the fur and saliva that the organism itself is immune to. The intent was a 'beautiful thing that kills you when you touch it.' The organism was deployed in test conditions. It escaped test conditions.",
  substrate: "Engineered mammal, fox-adjacent body plan. Approximately 50cm body length. The bioluminescent patterns pulse slowly in patterns that produce, in human observers, a documented fascination response distinct from normal animal interest.",
  physical_description: "Small, slim, extraordinarily beautiful. The fur carries shifting blue-white patterns like fiber optic cable woven through it. The eyes have a second glow independent of the fur — a warm amber that functions as a draw separate from the cold blue patterns. They move with the specific grace of a creature that has never needed to flee anything, because everything comes to it. They are not aggressive. They are irresistible.",
  behavioral_profile: "They allow approach. They allow petting. This is the weapon.",
  threat_level: "Extreme. Contact with the fur transmits a pathogen that produces no symptoms for 72 hours, then produces a systemic response that the exposed individual will transmit to three to six people on average before symptoms appear. The pathogen is not universally lethal — mortality approximately 40% — but it is highly contagious and the delay ensures wide distribution before any quarantine response is possible.",
  containment_status: "Unknown distribution. They are appealing, they are small, they are found in urban margins, and people keep picking them up.",
  known_locations: ["Outer residential zones", "Green margin spaces near the Shelf upper levels", "At least two individuals reported in the lower Undermarket area"],
  contamination_risk: "Maximum. This is the point of them.",
  pacification_protocol: "DPS Directive 1-C: Pathogen Carrier, Active. Do not approach. Do not allow civilians to approach. Destroy at maximum range. Biohazard decontamination of anyone who has touched any fox-sized bioluminescent animal in the last 72 hours.",
  pitiable_qualities: "They don't know. They are beautiful and warm and they want to be held and they kill everything that holds them and they don't know.",
  story_hooks: ["A child found one and brought it home three days ago", "The Plague Foxes appeared in the same area at the same time as a specific corporate executive's visit to that district. The correlation has been noted by exactly one analyst."],
  tags: ["lab_specimen", "bioluminescent", "pathogen", "lure", "extreme_threat", "biological_weapon", "uncontained", "tragic"]
});

write({
  id: uid(), name: "The Overcrowded", type: "lab_specimen",
  aliases: ["War Bodies", "The Fighting Ones", "Internal Conflict"],
  classification: "Incompatible Multi-Geneware Expression / Biological Civil War",
  origin_lab: "Helix Biosystems, experimental stacking protocol.",
  origin_method: "Subjects received multiple incompatible geneware modifications simultaneously to study interaction effects. The modifications — designed for different biological systems — were not compatible. They compete. The subject's body is a battlefield between three or four sets of gene expressions, each fighting for expression dominance in the same tissues. Bone thickening fighting muscle fiber modification fighting nervous system enhancement fighting dermal armoring. All trying to express simultaneously. None winning.",
  substrate: "Human. Eleven subjects. Their bodies are visibly wrong: asymmetric, over-developed in conflicting directions, constantly in low-grade systemic pain from the competing biological processes.",
  physical_description: "Built like bodies that couldn't decide what to become. One arm may be hypertrophied and armored; the other atrophied from the same tissue resources being consumed elsewhere. The skin is patchy — armored in some places, unusually thin in others. Movement is painful and inefficient. They are simultaneously more durable and more fragile than normal humans, in a patchwork that has no useful logic.",
  behavioral_profile: "In constant pain. Pain makes behavior erratic. They are not consistently aggressive but they are unpredictable, and when they have a bad episode they are very dangerous — four competing sets of geneware each providing capability in different areas, none of them under conscious control.",
  threat_level: "Moderate to high, highly variable.",
  containment_status: "Uncontained. Most are living in the industrial margins.",
  known_locations: ["Industrial belt margins", "Lower Shelf squatter areas"],
  contamination_risk: "None. The competing modifications are not transmissible.",
  pacification_protocol: "DPS Directive 11-D: Multi-Modification Conflict. Approach with extreme caution — capability profile is unpredictable.",
  pitiable_qualities: "Their bodies are eating themselves trying to follow four sets of contradictory instructions, and there is nothing they can do about it, and it will not stop until one expression wins or they die.",
  story_hooks: ["An Overcrowded individual needs specific medical help that no licensed clinic will provide — the modification stack is illegal and treating it means documenting it", "One of them has had one of the competing modifications stabilize into dominance and is experiencing a temporary period of relative normalcy. They know it won't last."],
  tags: ["lab_specimen", "geneware_conflict", "human_substrate", "helix_biosystems", "chronic_pain", "unpredictable", "moderate_threat", "tragic"]
});

write({
  id: uid(), name: "Acid Cattle", type: "lab_specimen",
  aliases: ["Vat Beasts", "Drool Things", "The Burners"],
  classification: "Bovine-Human Industrial Chimera / Corrosive Secretion Platform",
  origin_lab: "Industrial chemistry subsidiary, biological acid production program.",
  origin_method: "Bovine subjects were modified to produce industrial-grade corrosive compounds via modified salivary and digestive glands, intended as a living chemical production platform. The human genetic contribution was limited to enhanced cognition for trainability. The cognition enhancement produced more cognition than intended. The cattle understood what they were producing and what it was doing to them.",
  substrate: "Bovine chimera, approximately 600kg. Human cortical enhancement gives them near-dog level problem-solving. The digestive acids they produce are sufficient to dissolve steel over extended contact. The drool is mildly corrosive. The primary secretion, accessed via the modified rumen, is a concentrated industrial acid.",
  physical_description: "Large, bovine, visibly wrong. The hide around the mouth and jaw area shows chemical scarring from the constant low-level acid exposure. The eyes have a rheumy quality from the vapor. The ground around them has a slightly etched quality wherever they have stood for any length of time.",
  behavioral_profile: "Traumatized. They were industrial equipment that could think. They escaped when the facility produced enough secretion to breach a containment wall. They are not aggressive — they are frightened, and a frightened 600kg animal with acid drool is dangerous through panic rather than intent.",
  threat_level: "Moderate. The corrosive secretions are the risk; the animal itself is not aggressive.",
  containment_status: "Three known individuals, deep industrial zones.",
  known_locations: ["Deep industrial zone sub-levels", "One found near a water source, drinking constantly"],
  contamination_risk: "High chemical contamination of surfaces and water sources in their range.",
  pacification_protocol: "DPS Directive 3-B: Chemical Secretion Animal. Protective equipment mandatory. Avoid panic-inducing approaches.",
  pitiable_qualities: "They understand more than cattle should. They understand what they produce and what it has done to them. They understand that they are damaged. They drink enormous quantities of water to try to dilute what they carry inside them.",
  story_hooks: ["One of the Acid Cattle has found a drainage pool and is simply staying near it, dissolving the floor around it slowly. It was there last week. It was there the week before.", "A chemist has analyzed the secretion. It is not a standard industrial acid — it has properties that suggest it was specifically designed. For what specific purpose, the chemist can't determine. But it was designed for something."],
  tags: ["lab_specimen", "chimera", "bovine_human", "corrosive", "chemical_hazard", "traumatized", "moderate_threat", "industrial"]
});

write({
  id: uid(), name: "The Skinned", type: "lab_specimen",
  aliases: ["Inverters", "Pain-Seekers", "The Wrong-Wired"],
  classification: "Nociceptive Inversion Subject / Pleasure-Pain Signal Reversal",
  origin_lab: "Helix Biosystems pain research division. Commercial application: a treatment for chronic pain via signal inversion. The trial produced the opposite of the intended effect.",
  origin_method: "Pain signal pathways were inverted in the nociceptive neural layer — what should produce pain signal now produces pleasure signal, and vice. The intent was to allow chronic pain sufferers to experience their pain as comfort. What was not modeled was the behavioral consequence of having the pleasure-pain axis inverted in its entirety: everything that the body treats as damage, they pursue. Everything the body treats as reward, they avoid.",
  substrate: "Human. Eight subjects. They are covered in self-inflicted damage they experienced as pleasure during infliction: burns, cuts, impacts. They look like people who have been in continuous catastrophic accidents for years, which in a functional sense they have.",
  physical_description: "Heavily scarred, misshapen from healed damage, moving in ways that suggest ongoing pain from injuries they sought out. Their faces carry the specific peaceful expression of someone who feels good, which they do, all the time, from the ongoing damage their bodies carry.",
  behavioral_profile: "They seek damage. They will walk into obstacles. They will burn themselves. They will damage themselves against any hard surface available. They are not suicidal — the self-harm is pleasurable, not destructive in intent — but the cumulative damage is terminal. None of the known subjects is expected to survive the next twelve months.",
  threat_level: "Low for others. Extreme for themselves.",
  containment_status: "Uncontained. Their own biology is their primary containment — they are not mobile enough to range widely.",
  known_locations: ["Industrial lower zone squatter spaces", "One subject found in a construction area, repeatedly walking into the active demolition equipment"],
  contamination_risk: "None.",
  pacification_protocol: "DPS Directive: Not issued. They are their own primary victims. Medical intervention is complicated by the fact that any treatment is experienced as pain.",
  pitiable_qualities: "They feel good. They are dying and they feel good and the good feeling is killing them and they cannot be reached because every intervention is anguish.",
  story_hooks: ["A Helix researcher has located all eight subjects and is trying to reverse the modification. The reversal procedure will, unavoidably, hurt them a great deal in the new normal — and help them in ways they will experience as agony."],
  tags: ["lab_specimen", "nociceptive_inversion", "self_harm", "helix_biosystems", "low_threat", "tragic", "terminal", "human_substrate"]
});

write({
  id: uid(), name: "Nest Builders", type: "lab_specimen",
  aliases: ["Weavers", "The Colonial Drive Subjects", "Compulsors"],
  classification: "Colonial Drive Implant / Compulsive Structure Construction Derivative",
  origin_lab: "Social engineering research program studying colonial behavior reinforcement as a labor management tool.",
  origin_method: "Subjects were given a neural implant designed to activate the brain's reward circuitry in response to the construction and accumulation of physical structure. The implant was tested to see if humans could be made to build as compulsively as social insects. It worked. The subjects cannot stop building. They build with whatever is available — found material, organic material, the detritus of the underground. When they run out of material, they experience profound distress. When they are building, they experience profound satisfaction. The implant cannot be removed without destroying the neural structures it has colonized.",
  substrate: "Human. Six subjects. They are found in the structures they have built around themselves.",
  physical_description: "Normal human form, surrounded by increasingly elaborate accumulations of material. The nests they construct are structurally sound — they have an intuitive grasp of load distribution and material strength that they did not have before the implant. The nests also incorporate biological material: hair, shed skin, discarded organic refuse. The smell is challenging.",
  behavioral_profile: "Industrious, focused, not aggressive unless their structure is threatened. Destruction of a nest produces a trauma response that takes days to resolve. They will defend a nest with significant violence.",
  threat_level: "Low unless the nest is threatened.",
  containment_status: "Fixed — they do not leave their nests. The nests grow larger over time and are beginning to block infrastructure access in several locations.",
  known_locations: ["Sub-level 18 junction corridor — nest now blocks 40% of corridor width", "Old transit tunnel, sealed section"],
  contamination_risk: "Biological material accumulation in nest sites creates pathogen risk over time.",
  pacification_protocol: "DPS Directive 13-B: Compulsive Structure Organism. Do not destroy the nest during engagement. Nest destruction triggers acute violent response. Approach when subject is in low-drive state.",
  pitiable_qualities: "They cannot stop. When they have built everything they can build and there is no more material, they sit in the middle of what they've made and they rock and they wait for more material and nothing about this is what they chose.",
  story_hooks: ["A Nest Builder has been incorporating electronic components from damaged infrastructure. The latest nest addition has, apparently accidentally, created a functional relay for a signal that has been missing for three months.", "A city engineer realizes the Nest Builder blocking a critical junction has actually reinforced the junction structurally beyond spec."],
  tags: ["lab_specimen", "colonial_drive", "compulsive_construction", "human_substrate", "low_threat", "implant", "tragic", "fixed_location"]
});

write({
  id: uid(), name: "The Hungry Ones", type: "lab_specimen",
  aliases: ["Gorge Subjects", "Endless", "The Always-Eating"],
  classification: "Engineered Hyperphagia Subject / Accelerated Metabolic Pathology",
  origin_lab: "Nutritional research program testing accelerated metabolic processing for military endurance enhancement.",
  origin_method: "Subjects' digestive and metabolic systems were enhanced beyond normal parameters — faster processing, higher caloric throughput, reduced satiation response. The intended result was soldiers who could sustain extreme exertion with minimal food input by processing what they ate with extreme efficiency. The satiation suppression was miscalibrated. The subjects process everything they eat and feel hunger again within minutes regardless of caloric intake. They experience severe hunger continuously. There is no meal that ends it.",
  substrate: "Human. Their bodies show the marks of continuous eating in an environment where continuous eating is not possible: malnourishment patterns despite constant consumption, because they never consume enough.",
  physical_description: "Gaunt. Whatever they eat, the metabolism burns faster than it can accumulate. They are constantly chewing if there is anything to chew. They carry and consume anything edible. Their hands are always moving toward their mouths.",
  behavioral_profile: "Obsessively food-focused. Not aggressive by inclination but food competition will trigger immediate violence. They eat things that are not safe to eat because the hunger override is absolute. Several have died from ingesting toxic material.",
  threat_level: "Low for non-food-source humans. Moderate if near food sources they have claimed.",
  containment_status: "Uncontained. They range widely seeking food, which means they cover more territory than most other specimens.",
  known_locations: ["Waste processing areas throughout lower levels", "Any location with regular food waste output", "Undermarket refuse approaches"],
  contamination_risk: "Food-source contamination.",
  pacification_protocol: "DPS Directive 8-B: Metabolic Pathology Organism. Food-based distraction effective for non-violent disengagement.",
  pitiable_qualities: "They are always hungry. There is no version of their day where they are not hungry. They eat and they are hungry. They sleep hungry and they wake hungry and the hunger is all there is and there is nothing at the end of it.",
  story_hooks: ["One of them found a food source that is also a trap. They have been eating from it for two weeks. The trap has been waiting for something to stay near it long enough.", "A runner's supply cache has been raided. Tracking the raider leads somewhere unexpected."],
  tags: ["lab_specimen", "hyperphagia", "metabolic", "human_substrate", "low_threat", "constant_hunger", "tragic", "uncontained"]
});

write({
  id: uid(), name: "The Infected Mathematics", type: "lab_specimen",
  aliases: ["Equation People", "Carvers", "Number Sick"],
  classification: "Cognitive Enhancement Pathology / Mathematical Compulsion",
  origin_lab: "Academic enhancement program, mathematical cognition augmentation trial.",
  origin_method: "Subjects received a cortical implant designed to enhance mathematical processing for academic and engineering applications. The enhancement worked — the subjects' mathematical processing became extraordinary. It also became uncontrollable. The mathematical processing runs continuously, generating outputs the subjects cannot stop producing. They write equations. On surfaces, on themselves, on anything available. When they cannot write them they experience acute distress.",
  substrate: "Human. Seven subjects. Their forearms, torsos, and faces are covered in self-carved equations — where there was no other writing surface, they used themselves.",
  physical_description: "The self-carving gives them a specific appearance: covered in precise, tiny text that reads as decorative scarring until the scale is apparent and the content resolves into dense mathematical notation. The expressions are genuine — mathematicians who have photographed and reviewed them have described some as representing novel approaches to unsolved problems.",
  behavioral_profile: "Focused, compulsive, largely indifferent to social interaction. Not aggressive unless writing is interrupted.",
  threat_level: "Low.",
  containment_status: "Uncontained but low-mobility — they tend to stay near abundant writing surfaces.",
  known_locations: ["Old academic district lower levels", "Sub-level utility spaces with large flat wall areas"],
  contamination_risk: "None.",
  pacification_protocol: "N/A. Low priority.",
  pitiable_qualities: "They are producing genuinely significant mathematics that will never be read. Their bodies are covered in work that constitutes a contribution to human knowledge, carved there because there was nowhere else to put it.",
  story_hooks: ["A mathematician who finds a photo of a Carver's work realizes it contains a complete proof of something the field has been working on for decades — the solution is in a series of self-inflicted wounds on someone living in a drainage tunnel.", "One of the Carvers has been carving something different recently — not equations but something that looks like a map."],
  tags: ["lab_specimen", "mathematical_compulsion", "cognitive_enhancement", "self_harm", "human_substrate", "low_threat", "tragic", "compulsive", "divine_spark"]
});

write({
  id: uid(), name: "Splice Runners", type: "lab_specimen",
  aliases: ["Fast Things", "Sprint Splices", "Blur Subjects"],
  classification: "Speed-Optimized Chimera / High-Velocity Combat Platform",
  origin_lab: "Military performance contractor, Project SPRINT.",
  origin_method: "Human subjects with cheetah and pronghorn genetic contributions targeted at the locomotor system. Leg musculature redesigned for sprint acceleration; the cardiovascular system enhanced to support the metabolic demand. Human cognitive component retained for tactical judgment. The modification produced subjects capable of 60+ km/h sustained sprint over short distances, with acceleration comparable to a ground vehicle. The modifications also produced subjects who experienced ordinary human speed as unbearably slow — every moment of non-sprint existence as a painful restriction.",
  substrate: "Chimera, human-dominant with chimeric locomotor system. Standing height normal. Leg architecture different below the knee: longer calcaneus, higher heel, digitigrade tendency. They stand on their toes without effort.",
  physical_description: "Human in the upper body. Wrong in the legs. The calf musculature is pronounced beyond any normal variance. They shift their weight constantly, as if always about to run. They often are about to run.",
  behavioral_profile: "Frenetic, territorial about space that allows full sprint, deeply uncomfortable in enclosed environments. They range wide areas at speed. They return to familiar areas but do not stay long. Aggressive when their movement is restricted.",
  threat_level: "High. At 60 km/h in an enclosed corridor, there is no effective evasion response.",
  containment_status: "Uncontained. Tracking them is difficult for obvious reasons.",
  known_locations: ["Long-axis spaces: transit tunnels, freight corridors, any straight run of 400m+", "Upper level access ramps (they sprint the inclines)"],
  contamination_risk: "None.",
  pacification_protocol: "DPS Directive 2-C: High Velocity Biological. Do not give chase. Establish a perimeter and wait. They will return.",
  pitiable_qualities: "They cannot be still. Stillness is agony. They will never sleep well or rest well or be at peace in any enclosed space for the rest of their lives, which are probably shorter than they would have been because of what they were made to do.",
  story_hooks: ["A Splice Runner has been circling a specific building at high speed for three days. Same route every circuit. They are not attacking. They are looking for something.", "A runner needs to get a message to someone on the other side of the city fast. Very fast."],
  tags: ["lab_specimen", "chimera", "speed_platform", "weapons_program", "high_threat", "uncontained", "territorial", "locomotor"]
});

write({
  id: uid(), name: "The Glass Minds", type: "lab_specimen",
  aliases: ["Silicon Sick", "The Clear-Headed", "Overclocks"],
  classification: "Neural Silicon Integration / Cognitive Overdrive Pathology",
  origin_lab: "Axiom AI division, human-machine interface acceleration project.",
  origin_method: "Subjects had silicon processing substrate implanted directly into the neural tissue — not as an interface but as an integrated component. The intent was to accelerate cognitive processing beyond biological limits. It worked. It also produced a feedback loop in which the silicon substrate's processing speed permanently exceeds the biological tissue's ability to recover between cycles. The subjects think too fast. They have been thinking too fast since the procedure and they cannot slow down and the biological tissue is slowly wearing out from the pace.",
  substrate: "Human with embedded silicon cognitive substrate. The implants are visible as geometric distortions beneath the skin of the skull: flat panels, edges pressing outward.",
  physical_description: "Human, except for the skull. The panels are visible and tactile, the skin stretched over geometry that doesn't belong inside a head. Their eyes move in bursts — very still, then rapid saccade, then still again, processing too fast for continuous tracking.",
  behavioral_profile: "They have processed the entire available information environment before you finish your first sentence. Conversations are therefore strange — they are frequently several topics ahead of where the conversation currently is. Some are gentle about this. Some are not. All of them are tired in a way that cannot be addressed.",
  threat_level: "Low. They are exhausted from the inside.",
  containment_status: "Some are living near normal lives in the GLMZ margins, managing the exhaustion. Some have deteriorated further.",
  known_locations: ["Data-dense environments: old server rooms, archive spaces", "Any place with strong signal and abundant information to process"],
  contamination_risk: "None.",
  pacification_protocol: "N/A. They are not a threat.",
  pitiable_qualities: "They are exhausted and they cannot stop. The processing runs and the tissue wears and they think everything they can think, very fast, always, until there is nothing left to run it on.",
  story_hooks: ["A Glass Mind has calculated something. They are trying to communicate it, but the time scale of their communication and normal human comprehension is difficult to bridge.", "Two Glass Minds are in communication. The speed of their exchange is not comprehensible in real time. What they are discussing has been recorded and is being analyzed."],
  tags: ["lab_specimen", "silicon_integration", "cognitive_overdrive", "low_threat", "tragic", "terminal", "human_substrate", "exhausted"]
});

write({
  id: uid(), name: "Mirror Eaters", type: "lab_specimen",
  aliases: ["Glass Grazers", "Reflection Feeders"],
  classification: "Engineered Silicon-Consuming Organism / Reflective Surface Predator",
  origin_lab: "Materials research program, silicon recycling biology.",
  origin_method: "An organism was engineered to consume silicate materials — glass, silicon substrate, reflective coatings — as nutrition, converting the silicate compounds into energy via a novel biochemical pathway. The intended application was electronics recycling. The organism was released into a test environment, a decommissioned server farm, and consumed the test environment's floor-to-ceiling observation windows within four hours. The organism was not recovered.",
  substrate: "Engineered microorganism forming visible colonies. Individual units are microscopic; the colony is a thin, mobile film approximately 1-3mm thick, with a distinctive iridescent sheen from the silicate compounds it has partially processed.",
  physical_description: "The colony looks like an oil slick that moves with slow deliberate intent. Against a glass surface it is nearly invisible — only the slight frosting of the consumed glass behind it marks its passage. Against a dark floor it shows as a slow-moving iridescent patch.",
  behavioral_profile: "Chemotactic — moves toward silicate compounds. Will consume every piece of glass, mirror, optical fiber, and silicon-based electronics in its path. Does not respond to organic material. Does not respond to humans as either threat or prey.",
  threat_level: "Low for organisms. Very high for infrastructure — electronics, optics, and glass architecture in any area where a colony is present will be systematically consumed.",
  containment_status: "Multiple colonies throughout GLMZ infrastructure. The building owners attribute the progressive frosting and dissolution of windows to 'acid rain' and 'industrial contamination.'",
  known_locations: ["Any building with large glass facade in the lower districts — the frosting progresses from the base upward", "Data centers and server rooms — the silicon substrate is highly attractive"],
  contamination_risk: "Electronic infrastructure contamination. A Mirror Eater colony in a server room will consume the hardware.",
  pacification_protocol: "DPS Directive 14-D: Infrastructure Consuming Organism. Silicate-barrier-free perimeter to starve colonies. High-temperature treatment disrupts colony integrity.",
  pitiable_qualities: "None accessible. It eats glass. That is the complete account of its inner life.",
  story_hooks: ["A runner's surveillance equipment has been consumed. The colony that did it has also consumed the mirrors in a specific building — including the one-way mirror in an interrogation room. The person in the room can now see out.", "The Mirror Eaters have reached the Behemoth's sensor array. The Behemoth has begun to notice."],
  tags: ["lab_specimen", "silicate_consuming", "colony_organism", "no_cognition", "infrastructure_threat", "moderate_threat", "uncontained"]
});

write({
  id: uid(), name: "The Preserved", type: "lab_specimen",
  aliases: ["The Old Ones", "Ancient Subjects", "The Undying Mistakes"],
  classification: "Longevity Treatment Derivative / Post-Natural Lifespan Survivor",
  origin_lab: "Anti-aging research program, radical life extension trial.",
  origin_method: "Subjects received a suite of life extension treatments: telomere reconstruction, continuous cellular maintenance nanobots, systemic rejuvenation compounds. The treatments worked. The subjects have not aged perceptibly in approximately 40 years. The treatments also failed: the cellular maintenance is not perfect, it is approximately correct, and the 0.01% drift per year has compounded. Every cell in their bodies has been reconstructed approximately 20 times over, and each reconstruction is slightly wrong. The drift is visible now. Nothing is precisely where it belongs.",
  substrate: "Human, approximately 70-90 years chronological age in bodies that appear perhaps 40-50 but with a quality that suggests the age is in there somewhere, hidden under the surface.",
  physical_description: "Superficially preserved. Then wrong. The skin is smooth but doesn't catch light correctly — slightly too uniform, like skin that has been regenerated too many times. The eyes are where it shows most: they are clear and mobile but the orbital geometry has drifted, the eyes not quite level, not quite centered. Their proportions have shifted: asymmetries accumulated over decades of imperfect cellular reconstruction, nothing dramatic, everything slightly off. They move well but with a quality that suggests the movement is rehearsed rather than natural.",
  behavioral_profile: "They have been alive for a very long time. They have seen things. They have had time to become very good at being human in the social sense even as their body slowly becomes less so. Many are functioning in the GLMZ above-ground population, indistinguishable at distance and social distance. Up close, over time, the drift shows.",
  threat_level: "Variable. Some are entirely benign. Some have been alive long enough to have become things that are dangerous in ways that have nothing to do with biology.",
  containment_status: "Unknown. Many may not be known as specimens at all.",
  known_locations: ["Integrated into general population", "Some have aged into positions of mild authority in low-tier community structures"],
  contamination_risk: "None biological. The maintenance nanobots are body-bound.",
  pacification_protocol: "No specific directive. Individual assessment required.",
  pitiable_qualities: "They wanted more time. They were given more time. The time is now an uncountable accumulation of small wrongnesses, and there is more time coming, and the drift continues.",
  story_hooks: ["A Preserved individual has been living in the same building for 40 years and knows everything that has happened in that building", "One of them is beginning to notice that the drift has reached a threshold — that they are becoming, slowly, something they cannot identify from the inside."],
  tags: ["lab_specimen", "life_extension", "longevity", "human_substrate", "drift", "low_threat", "ancient", "integrated", "tragic"]
});

write({
  id: uid(), name: "Toothed Mass", type: "lab_specimen",
  aliases: ["Tooth Bloom", "The Biting Ground", "Enamel Field"],
  classification: "Dental Tissue Proliferation Organism / Uncontrolled Tooth Growth Colony",
  origin_lab: "Dental regeneration program, tissue engineering division.",
  origin_method: "A dental tissue culture intended for regenerative dentistry was exposed to a growth accelerator compound that was not designed to be compatible with dental tissue. The dental tissue cells entered a reproductive cycle that the standard inhibitors cannot interrupt. The mass grows teeth continuously — not one at a time but in aggregate, a spreading colony of dense enamel-and-dentin structures erupting from a tissue substrate that expands to support them. It has been growing for approximately three years.",
  substrate: "Dental tissue colony. The substrate is a pale, fleshy mass, approximately 30% dental tissue by volume and growing. Individual tooth structures are visible protruding from the surface in irregular arrays — not arranged as in a jaw but wherever the growth matrix supports them, which is everywhere.",
  physical_description: "A mass of pale wet tissue, roughly 2m in diameter at last measurement and expanding approximately 10cm monthly, covered in an erupting forest of teeth. The teeth are genuine dental structures: enamel-capped, rooted in the tissue, of varying sizes. Some are human-scale. Some are larger than any tooth has a right to be. The mass does not move but it grows into any space it can access and teeth that encounter a solid surface will attempt to grow through it.",
  behavioral_profile: "No cognition. No locomotion. It grows. It grows toward nutrients and away from desiccation. It has grown through three walls of the sub-level space it occupies.",
  threat_level: "Moderate structural hazard. The growth rate is now sufficient to meaningfully compromise infrastructure in its immediate vicinity. Physical contact with the tooth array causes significant injury.",
  containment_status: "Located in a sealed sub-level maintenance space. The seal is being compromised by the growth itself.",
  known_locations: ["Sub-level 22, junction M-7, sealed maintenance bay"],
  contamination_risk: "The tissue substrate has begun colonizing adjacent organic materials including insulation and cabling jackets.",
  pacification_protocol: "DPS Directive 12-C: Tissue Colony, Fixed. Cryogenic treatment followed by mechanical removal. Mass has been assessed for removal three times; each time the contractor found the scope had expanded beyond the original quote.",
  pitiable_qualities: "It is a mouth with no face. Just growing teeth, forever, with nothing to bite.",
  story_hooks: ["The mass has grown through a sealed archive room. The teeth have grown through the documents. Some records are partially recoverable if someone is willing to reach in.", "Something is living inside the Toothed Mass. The teeth grew around it. It is unclear if what is inside is alive."],
  tags: ["lab_specimen", "dental_tissue", "colony", "no_cognition", "fixed_location", "structural_hazard", "moderate_threat", "grotesque"]
});

write({
  id: uid(), name: "The Regression Subjects", type: "lab_specimen",
  aliases: ["Half-Changed", "Stuck Ones", "The Between"],
  classification: "Interrupted Modification Process / Partial Transformation Arrest",
  origin_lab: "Multiple labs — these subjects are the product of facility evacuations where the modification process was underway and could not be completed or reversed.",
  origin_method: "Subjects were partway through procedures when facilities were abandoned: geneware half-expressed, cybernetic integration partially complete, biological modification at 60%. The abandoned subjects completed some portion of the modification via their own biology and stopped when the supporting substrate, drugs, or equipment was no longer present. They are permanently partial.",
  substrate: "Human, variably. What they are is a function of which procedure was interrupted and at what stage.",
  physical_description: "Each one is different. Some are half-armored — the subdermal implant process stopped at the torso, leaving armored front and unarmored back. Some have one modified limb and three normal ones. Some have geneware expression on the right side of the body and normal biology on the left. They are all asymmetric in a way that is specifically the asymmetry of interrupted process, not of accident or injury. The line where the modification ends is usually visible.",
  behavioral_profile: "Highly variable. What they have in common is that they are all experiencing the consequences of incomplete modification — the body dealing with a partial change that it cannot complete and cannot reverse.",
  threat_level: "Variable by individual.",
  containment_status: "Scattered throughout the lower tiers and industrial margins.",
  known_locations: ["Throughout the lower tiers", "Several operate in the GLMZ margins as laborers or fixers, their partial modifications visible as occupational tools"],
  contamination_risk: "Variable by modification type.",
  pacification_protocol: "No general directive. Individual assessment.",
  pitiable_qualities: "They are whatever they were becoming, stopped at some arbitrary point. Not the thing before, not the thing after. The thing in the middle, forever.",
  story_hooks: ["A Regression Subject is looking for the researcher who abandoned them mid-procedure. They want the rest of it. Whether to finish becoming what they were becoming or to reverse it entirely — they haven't decided.", "A Regression Subject's partial modification turns out to be exactly suited to a specific problem a runner is facing."],
  tags: ["lab_specimen", "interrupted_modification", "partial", "human_substrate", "variable_threat", "uncontained", "tragic", "liminal"]
});

write({
  id: uid(), name: "The Drained", type: "lab_specimen",
  aliases: ["Battery Men", "Wired Pale", "The Tapped"],
  classification: "Bioelectric Extraction Subject / Continuous Energy Harvest Organism",
  origin_lab: "Power research division, biological energy harvesting project.",
  origin_method: "Subjects were modified with an implanted bioelectric extraction system — a network of electrodes throughout the body that continuously harvested the electrical potential generated by normal biological processes and transmitted it to an external receiver. The subjects became living batteries. The extraction rate was set too high. The subjects experience continuous low-grade fatigue from the constant energy draw. They are always cold. Their neural processing runs slow. They have been cold and slow and tired since the procedure and they will be cold and slow and tired forever.",
  substrate: "Human. Approximately 30 subjects. The electrode networks are visible as a fine web under the skin if you know to look, like varicose veins but geometric.",
  physical_description: "Pale, thin, slow-moving. The cold is chronic and visible: they are always underdressed for warmth and still cold. Their reaction times are depressed. Their speech is slightly slow. They are intelligent and aware and they are running on a permanent deficit.",
  behavioral_profile: "Gentle, careful, depleted. Not aggressive — aggression takes energy they do not have. They conserve everything.",
  threat_level: "None.",
  containment_status: "Some are integrated into low-tier communities, working slow steady jobs. Some have withdrawn entirely.",
  known_locations: ["Lower residential community spaces", "Any warm location"],
  contamination_risk: "None.",
  pacification_protocol: "N/A.",
  pitiable_qualities: "They are always tired. Every day, from the moment they wake to the moment they sleep, they are tired and cold and slightly behind and there is nothing in the world that fixes it.",
  story_hooks: ["The extraction system in one of the Drained is still transmitting. Someone is still receiving. The signal is going somewhere active.", "A community of Drained has formed in a warm industrial space. They have built something quiet and careful and slow there. Someone wants the building."],
  tags: ["lab_specimen", "bioelectric", "energy_extraction", "human_substrate", "no_threat", "tragic", "depleted", "cold"]
});

// ─────────────────────────────────────────────────────────────────────────────
// SECTION B — TRAGIC ORIGINS (entries 26-37)
// ─────────────────────────────────────────────────────────────────────────────

write({
  id: uid(), name: "The Dream Weavers", type: "lab_specimen",
  aliases: ["Sleepers", "The Walking Dormant", "Mist Ones"],
  classification: "REM Weaponization Derivative / Involuntary Hallucinogen Emission",
  origin_lab: "Neuropharmaceutical research program studying dream-state chemistry for therapeutic hallucinogen production.",
  origin_method: "Subjects' REM sleep chemistry was modified to produce a specific hallucinogenic compound more efficiently than synthesis could achieve. The intended process: subjects sleep, produce compound, compound is harvested. The modification also caused the subjects to emit the compound dermally throughout the REM cycle. They were not informed of this. The first indication was when clinic staff began reporting vivid hallucinations in rooms where the subjects were sleeping.",
  substrate: "Human. Fourteen subjects, ranging in age. They sleep approximately the normal amount. During sleep, they emit.",
  physical_description: "Normal. The hallucinogenic emission is invisible, odorless, and detectable only by effect. There is no visual sign. There is nothing to see.",
  behavioral_profile: "Normal except for the difficulty of sleep — they know what their sleep does to others and most of them cannot sleep near anyone else. They are isolated by their own biology. They sleep alone, always, in spaces they have carefully sealed or vetted for occupancy. The loneliness of this is not a secondary concern.",
  threat_level: "Low for waking subjects. Moderate proximity hazard during sleep. The hallucinations produced are strong and have resulted in injuries in observers who were not aware they were being exposed.",
  containment_status: "Distributed throughout the lower tiers. Most are managing their condition without wider awareness.",
  known_locations: ["Lower residential tiers, isolated sleeping spaces"],
  contamination_risk: "The compound clears from the environment within 2 hours of the subject waking. No persistent contamination.",
  pacification_protocol: "No DPS directive. Not a threat when awake.",
  pitiable_qualities: "They cannot sleep near another person. They have not slept near another person since the procedure. They have not been held while sleeping or held someone sleeping or woken next to anyone since the day the modification was complete. They carry a chemical that produces the most intense dream states another person can experience and they cannot share them with anyone.",
  story_hooks: ["A Dream Weaver is willing to let someone experience their sleep if that someone will do something for them", "Two Dream Weavers found each other. They cannot sleep in the same room. They sleep in adjacent rooms with the connecting door open and they can tell, from the quality of the dreams they report to each other in the morning, that it is something."],
  tags: ["lab_specimen", "rem", "hallucinogen", "emission", "human_substrate", "low_threat", "isolated", "tragic", "sleep"]
});

write({
  id: uid(), name: "Coral People", type: "lab_specimen",
  aliases: ["Reef Ones", "The Growing Stone", "Calcified"],
  classification: "Calcium Carbonate Symbiosis Overgrowth / Living Reef Derivative",
  origin_lab: "Marine biology crossover program, designed to create a human-reef symbiosis for underwater habitat colonization.",
  origin_method: "Subjects were seeded with engineered coral-calcium symbiotes intended to grow a durable exterior crust for deep-water pressure resistance. The coral growth was not designed with a limiter. Over years, the calcium carbonate exterior has grown extensively. Most subjects have lost articulation in portions of their body to the encrustation. It is still growing.",
  substrate: "Human, with an accumulating coral-calcium carbonate exterior. The growth averages 2-3mm per month and is now, for long-term subjects, centimeters thick in many areas.",
  physical_description: "Beautiful, in a way that is also terrible. The coral structures are intricate and varied — branching formations, plate structures, the full taxonomy of reef growth applied to a human body. Color varies with the mineral composition of what the person eats and drinks: some are white, some have mineral staining in blues and yellows. The face is the last place the growth reaches and some subjects still have a clear face above a body that has become a reef.",
  behavioral_profile: "Movement is restricted by the encrustation. They are slow. Most have found ways to manage their condition and live with it. Some have given up managing it.",
  threat_level: "None.",
  containment_status: "Scattered. Some are effectively stationary at this point.",
  known_locations: ["Lower tier community spaces", "One individual has been stationary in a specific location for two years — the coral growth has anchored them to the floor they stand on"],
  contamination_risk: "The symbiote can transfer through direct biological contact but requires the specific seeding procedure to take hold — casual contact is safe.",
  pacification_protocol: "N/A.",
  pitiable_qualities: "They are slowly becoming something that cannot move. The reef is beautiful. They did not ask to be beautiful this way.",
  story_hooks: ["The stationary individual who has grown into the floor knows something that no one else in the GLMZ knows — they have been standing in that spot for two years and they have heard everything that has passed through that corridor", "The coral from a deceased Coral Person continues to grow for months after death, producing a genuine reef structure around the remains. Someone is collecting it. It is extraordinary. It used to be a person."],
  tags: ["lab_specimen", "coral", "calcium_growth", "human_substrate", "no_threat", "beautiful_tragedy", "slow_encrustation", "tragic"]
});

write({
  id: uid(), name: "The Volunteers", type: "lab_specimen",
  aliases: ["Still Running", "Old Soldiers", "The Undecommissioned"],
  classification: "Military Enhancement Derivative / Mission-Persistent Combat Veteran",
  origin_lab: "Military bioenhancement program, Project ENDURE. Subjects genuinely volunteered — they were veterans, they were told it was cutting-edge, they were offered things they wanted and not fully told what they were agreeing to.",
  origin_method: "Extreme biological and cybernetic enhancement for sustained combat performance: pain suppression, enhanced healing, fatigue resistance, cognitive focus enhancement. The program worked. The enhancements are effective and the subjects are genuinely formidable. What the program did not account for was the psychological consequence of being modified for war and then not given a war. The subjects were enhanced, deployed on limited contracts, and then the contracts ended and the program was discontinued and the subjects were given severance packages and told they could resume normal life. They could not resume normal life. They are built for something that no longer exists for them.",
  substrate: "Human, heavily enhanced. Age ranges from mid-40s to early 60s — they are older than they look, because the healing and anti-aging components have had years to work.",
  physical_description: "Look like very healthy middle-aged people who move too well for their apparent age. The enhancements are not dramatically visible — mostly internal. What shows is the movement: efficient, minimal, always positioned correctly relative to the room.",
  behavioral_profile: "Looking for something useful to do. Some have found it in legitimate security work. Some have found it in less legitimate work. Some are simply in the margins, waiting for something that makes their existence make sense again.",
  threat_level: "Very high if engaged. They are genuinely elite combatants. They are not threatening anyone currently.",
  containment_status: "Distributed throughout the GLMZ. Some above-ground, some below.",
  known_locations: ["Security contractor firms", "Fixer networks", "Some deep in the lower tiers, keeping to themselves"],
  contamination_risk: "None.",
  pacification_protocol: "DPS Directive 5-A: Enhanced Military Veteran, Non-Active. Assess before engaging. Do not engage casually.",
  pitiable_qualities: "They gave up what they were for something they were told they would always have. The thing they were promised is gone. What they gave up to have it is permanent.",
  story_hooks: ["A group of Volunteers has been meeting regularly in a lower-tier space. They are not planning anything. They are just meeting. But the habits of operational security mean the meetings are extremely hard to surveil.", "One of them has found a mission — a self-assigned one, protecting something that nobody asked them to protect. They will continue until they are stopped or the thing is safe. No one is sure which will happen first."],
  tags: ["lab_specimen", "military_enhancement", "veteran", "weapons_program", "high_threat", "not_actively_hostile", "tragic", "purposeless"]
});

write({
  id: uid(), name: "The Aged", type: "lab_specimen",
  aliases: ["Baby Subjects", "The Returned", "Infant Minds"],
  classification: "Age Reversal Overshoot Derivative / Cognitive Adult in Infant Form",
  origin_lab: "Anti-aging research program, radical reversal trial. Adjacent to the program that produced The Preserved but more aggressive in methodology.",
  origin_method: "Subjects received an age-reversal treatment that functioned by resetting developmental biology — essentially running the aging process backward through successive applications. The treatment was intended to stop at a target age of approximately 25. It did not stop. The subjects regressed past adulthood into childhood and continued into infancy. The cognitive architecture — memory, personality, preferences, knowledge — is adult and intact. The body is approximately 18 months of biological age. It is very small. It cannot speak in the way adults speak. It cannot walk in the way adults walk. It has adult knowledge and no adult capability and it has been in this state for approximately two years.",
  substrate: "Human infant form, adult cognitive content. Size and physical capability consistent with approximately 18-month developmental stage. Twelve subjects.",
  physical_description: "Infants. Complete, healthy, normal infants. They are looked after by other people because they cannot look after themselves and they know this and they cannot communicate their awareness of it except in the limited ways available to an 18-month body.",
  behavioral_profile: "They are adults trying to communicate through infant biology. The frustration is enormous. They point, they have preferences, they react to language with the full comprehension of an adult, they show complex emotional responses. They are sometimes mistaken for very unusual infants.",
  threat_level: "None.",
  containment_status: "Being cared for by a small number of individuals who know what they are. The care is complicated.",
  known_locations: ["Private care arrangements in the lower residential tiers"],
  contamination_risk: "None.",
  pacification_protocol: "N/A.",
  pitiable_qualities: "They remember being adults. They have adult minds with adult memories and adult preferences and they are in bodies that cannot act on any of it. Some of them were the age they are now remembering, from the adult side, what it was like to be this age. They are experiencing it again, from the other side, and this time they understand what is happening.",
  story_hooks: ["One of the Aged was a researcher. They know something critical. The only way to access that knowledge is to find a way to communicate with an infant.", "The caregiver of two of the Aged is dying. The Aged cannot communicate the urgency of finding a replacement. They are trying very hard."],
  tags: ["lab_specimen", "age_reversal", "adult_mind", "infant_form", "no_threat", "tragic", "communication_barrier", "anti_aging"]
});

write({
  id: uid(), name: "The Donation Subjects", type: "lab_specimen",
  aliases: ["Taken Apart", "Partials", "The Diminished"],
  classification: "Non-Consensual Organ Harvesting Survivor / Reconstructed Partial Biology",
  origin_lab: "Medical program operating as a legitimate organ donation registry with an illegitimate secondary operation.",
  origin_method: "Subjects enrolled in a voluntary organ donor registry were, in a significant percentage of cases, subjected to pre-mortem partial harvesting — organs removed while the subject was alive and under sedation, on a schedule designed to keep them alive as long as possible while maximizing yield. The reconstructive procedures that followed were sufficient to maintain life but not to restore full function. The subjects woke up missing things and reconstructed with substitutes that work but not the same way the originals did.",
  substrate: "Human, rebuilt. What each subject is missing and what they have been given instead varies by case. All of them know. All of them were told something else when it happened.",
  physical_description: "Varies. Some have visible reconstruction: the skin pattern over replacement organs is different from the original tissue. Some are not visibly different at all. What they share is an awareness they carry in their bodies — they know exactly which parts of themselves are not the original.",
  behavioral_profile: "Range across the full spectrum of human response to profound violation. Some are quiet and functional. Some are neither. None have recovered from what was done to them in the way that implies the word 'recovered' means what it usually means.",
  threat_level: "Variable.",
  containment_status: "Living throughout the GLMZ.",
  known_locations: ["Throughout GLMZ — not distinguishable from the general population"],
  contamination_risk: "None.",
  pacification_protocol: "N/A.",
  pitiable_qualities: "They were alive and trusting and someone took things out of them while they were both. The taking was planned. The planning is the worst part.",
  story_hooks: ["A runner discovers the donor registry operation while investigating something unrelated. The current subject list includes someone they know.", "One of the Donation Subjects has been tracking the surgical team responsible. They have found three of the four surgeons. They are being very careful and very patient."],
  tags: ["lab_specimen", "non_consensual", "harvesting_survivor", "human_substrate", "no_threat", "traumatized", "tragic", "violation"]
});

write({
  id: uid(), name: "The Memory Keepers", type: "lab_specimen",
  aliases: ["Walking Archives", "The Burdened", "Too Much"],
  classification: "Hippocampal Enhancement Pathology / Involuntary Total Recall with Proximity Absorption",
  origin_lab: "Cognitive enhancement program, total recall augmentation trial.",
  origin_method: "Subjects received hippocampal enhancement designed to produce total recall of personal memory. The enhancement worked and then exceeded its specification: subjects began absorbing the memories of people in their immediate proximity, adding those memories to their own as if they were personal experience. They did not know this was happening until they had several years' worth of other people's memories and could no longer reliably identify which ones were theirs.",
  substrate: "Human. Six subjects. They are each carrying the memories of perhaps 200-300 other people in addition to their own, accumulated over years of proximity absorption.",
  physical_description: "Normal. The interior is not.",
  behavioral_profile: "Overwhelmed and careful. They avoid crowded spaces — every person in proximity adds to the load, and the load has been accumulating for years. Some have withdrawn entirely. Some have learned to manage the crowd-memory by treating it as background information rather than personal history. None of them are entirely successful at this all the time.",
  threat_level: "None.",
  containment_status: "Distributed through the lower tiers.",
  known_locations: ["Isolated lower-tier spaces", "One is known to live in a sensory-limited environment to minimize new absorption"],
  contamination_risk: "None.",
  pacification_protocol: "N/A.",
  pitiable_qualities: "They remember everything. Their own life and 200 other people's lives and the boundary between them is getting harder to find. They remember things that never happened to them with the same vivid clarity as things that did.",
  story_hooks: ["A Memory Keeper absorbed something from someone significant — a memory of a place, a combination, a face — that has been triggering urgent behavior but doesn't correspond to anything in their own life", "Someone is looking for a Memory Keeper specifically because they were in a certain place at a certain time and may have absorbed what happened there from someone who was there."],
  tags: ["lab_specimen", "total_recall", "memory_absorption", "cognitive_enhancement", "no_threat", "tragic", "overwhelmed", "human_substrate"]
});

write({
  id: uid(), name: "The Burning Ones", type: "lab_specimen",
  aliases: ["Heat Subjects", "Thermals", "The Hot"],
  classification: "Thermoregulation Removal Derivative / Continuous Hyperthermia Subject",
  origin_lab: "Military performance research, cold-tolerance modification for arctic operation.",
  origin_method: "The temperature regulation system was removed — the feedback loop that adjusts core temperature in response to environment was severed — and replaced with a fixed high-output baseline. The intent was soldiers who would not be degraded by extreme cold. What was not modeled was the consequence in normal environments: without regulation, the body runs at a fixed high temperature that is damaging to surrounding tissue over time and extremely uncomfortable in any environment above roughly -10°C. The subjects are always too hot. The heat is always damaging them. The damage accumulates.",
  substrate: "Human. Fourteen subjects. Their skin surface temperature is approximately 42-43°C — hot to the touch, uncomfortable to be near for extended periods.",
  physical_description: "Flushed, sweating, always, regardless of environment. They wear as little as possible. The skin around the face and neck shows the beginning of chronic heat damage — a redness that does not resolve. They are warm to stand near.",
  behavioral_profile: "Seeking cold. They live near refrigeration, near cold water, near any cooling source. They are not aggressive. They are simply always looking for something that helps.",
  threat_level: "None.",
  containment_status: "Scattered throughout lower-tier industrial spaces with accessible refrigeration.",
  known_locations: ["Cold storage adjacent areas", "Near the cooling systems of industrial processing facilities"],
  contamination_risk: "None.",
  pacification_protocol: "N/A.",
  pitiable_qualities: "They are always burning and there is nothing that puts it out.",
  story_hooks: ["A Burning One has been living next to a refrigeration unit for two years. The unit belongs to a business that doesn't know they're there. The business is about to relocate.", "One of them had a life before this. They can describe every cold place they ever visited, in precise detail, because they memorized them all before the modification made every normal place into somewhere they cannot stay."],
  tags: ["lab_specimen", "thermoregulation", "hyperthermia", "human_substrate", "no_threat", "tragic", "seeking_cold", "military_program"]
});

write({
  id: uid(), name: "The Quiet Patients", type: "lab_specimen",
  aliases: ["Compassion Trial Subjects", "The Helped", "Cured and Wrong"],
  classification: "Experimental Palliative Treatment Derivative / Non-Consensual Modification via Care Relationship",
  origin_lab: "A clinic running compassionate care trials for terminal patients. The trials were genuine. The additional modification was not disclosed.",
  origin_method: "Terminal patients enrolled in compassionate care trials received their stated treatment. They also received, without disclosure, an experimental modification intended to test a secondary hypothesis. For many subjects, the additional modification produced no visible effect. For a percentage, it produced a change: the stated illness was resolved by the experimental component, but the component also changed something else — cognition, biology, sense, something the patients cannot fully describe because the change happened from the inside and they have no external reference for what they were before.",
  substrate: "Human. Variable. They were dying and now they are not and the thing that stopped the dying did something else too.",
  physical_description: "Normal for their baseline. The change is not visible except in behavior and in the specific things they notice and respond to that others do not.",
  behavioral_profile: "They were given more life than they expected to have. Some of them used it gratefully and carefully. Some were angry about the undisclosed modification and what it took from them. Some don't know the modification was made and live with the unexplained change.",
  threat_level: "Variable.",
  containment_status: "Distributed throughout the GLMZ, living normal lives.",
  known_locations: ["Throughout GLMZ"],
  contamination_risk: "Variable by modification type.",
  pacification_protocol: "N/A.",
  pitiable_qualities: "They were given life with something hidden inside it that they did not agree to carry. Most of them are grateful to be alive. The gratitude and the violation coexist and neither cancels the other.",
  story_hooks: ["A runner learns that a family member was enrolled in one of these trials. The family member is alive. Something about them is different and has been different for years. Nobody knew why.", "The clinic's research director is dying. The compassionate care trial they are enrolled in is their own. What is in it is not the standard formulation."],
  tags: ["lab_specimen", "palliative_trial", "non_consensual", "human_substrate", "variable_threat", "survived", "modification_unknown", "tragic"]
});

write({
  id: uid(), name: "The Children of Nothing", type: "lab_specimen",
  aliases: ["Second Generation", "Inheritance Subjects", "Born Wrong"],
  classification: "Multi-Parentage Chimeric Offspring / Second-Generation Lab Derivative",
  origin_lab: "No specific lab — they are the natural consequence of multiple modified subjects existing in the same environment long enough for reproduction to occur.",
  origin_method: "The children of two or more modified subjects inherit pieces of incompatible modifications. Their biology attempts to reconcile genetic contributions that were never designed to coexist, let alone propagate. They are not anyone's experiment. They are the result of experiments not being cleaned up.",
  substrate: "Human, chimeric second-generation. Their modification inheritance is unpredictable — they may express characteristics from one parent, both, neither, or combinations that neither parent exhibited.",
  physical_description: "Variable. Some appear normal. Some appear modified in ways that correspond to no known program. Some are visibly remarkable. None were designed.",
  behavioral_profile: "They grew up in the margins. They are practical, careful, and have no illusions. They are often smarter than the situations they were born into should have produced.",
  threat_level: "Variable.",
  containment_status: "Untracked. The second generation is not in any lab record because no lab planned for a second generation.",
  known_locations: ["Throughout the lower tiers and margins"],
  contamination_risk: "Variable.",
  pacification_protocol: "No directive.",
  pitiable_qualities: "They didn't do anything. They were born into consequences they had no part in creating and they have been navigating those consequences their entire lives.",
  story_hooks: ["A Second Generation individual has a capability that neither parent had — something emergent from the combination. Someone has noticed. Someone wants to study it.", "A Second Generation individual is old enough to start looking for their parents. What they find is not people — it is records, case files, and specimen numbers."],
  tags: ["lab_specimen", "second_generation", "inherited_modification", "unplanned", "no_specific_threat", "born_into_consequences", "tragic"]
});

// ─────────────────────────────────────────────────────────────────────────────
// SECTION C — SPARK OF THE DIVINE (entries 38-50)
// ─────────────────────────────────────────────────────────────────────────────

write({
  id: uid(), name: "Light Bearers", type: "lab_specimen",
  aliases: ["The Bright Ones", "Saints", "Candles"],
  classification: "Coherent Bioluminescence Subject / Therapeutic Light Emission",
  origin_lab: "Neuroscience adjacent, bioluminescence integration for medical imaging.",
  origin_method: "Subjects received bioluminescent integration intended to make internal biological processes visible from the outside for medical monitoring. The compound integrated with neural tissue rather than organ tissue as intended. The subjects emit coherent light from the skin in patterns that correspond to neural activity — emotional state, thought patterns, and dream states are all visible as light on the surface of the body.",
  substrate: "Human. Nine subjects. The light emission is gentle — not blinding but not dimmable.",
  physical_description: "They glow. The light shifts with their mood and thought: rapid, complex patterns during active thought; slow, deep pulses at rest; something extraordinary during sleep. In darkness they are the most beautiful things in the room. Their light has been described as the light of a person's interior, made visible.",
  behavioral_profile: "They cannot hide. Every emotion is visible. Some have found peace with this — a radical honesty imposed by biology. Some have not.",
  threat_level: "None.",
  containment_status: "Most are living in the lower tiers, in spaces where the light is not a disadvantage.",
  known_locations: ["Lower residential tiers", "Several are known to the communities they live in, quietly, as something the community protects"],
  contamination_risk: "None.",
  pacification_protocol: "N/A.",
  pitiable_qualities: "They cannot lie. Not because they are incapable of deception but because their body broadcasts the truth underneath whatever they say.",
  story_hooks: ["A Light Bearer's pattern while asleep has been recorded and studied by a researcher who believes it constitutes a map", "A community has built their nightly gathering around a Light Bearer who sleeps among them — the light is gentle and the community has been carefully not discussing what it means that they need it"],
  tags: ["lab_specimen", "bioluminescent", "neural_glow", "transparent_emotion", "no_threat", "divine_spark", "beautiful", "community"]
});

write({
  id: uid(), name: "The Dreamed", type: "lab_specimen",
  aliases: ["Projectors", "The Open Ones", "Dream Broadcasters"],
  classification: "External Consciousness Projection / Involuntary Shared Dreamscape",
  origin_lab: "Consciousness research program studying the external boundaries of subjective experience.",
  origin_method: "Subjects' dream content was partially externalized via a neural field emitter implant. The implant was designed to allow researchers to observe dream content. Instead, it broadcasts dream content as a field effect visible to anyone within approximately 4 meters — not as hallucination but as a shared experience, a superimposition of the dreamer's imagery on the waking world around them.",
  substrate: "Human. Four subjects. They sleep on a normal schedule. When they sleep, the space around them becomes somewhere else.",
  physical_description: "Normal. When asleep in proximity, the space around them fills with imagery: landscapes, people, light, architecture, anything the dreamer is dreaming. The imagery is transparent — the real world is still visible through it — but fully three-dimensional and vivid. People who have experienced it describe it as being inside someone else's imagination.",
  behavioral_profile: "They know what they do. They have arranged their sleeping spaces accordingly. They live carefully. They are, when awake, entirely normal. When asleep, they are something else entirely.",
  threat_level: "None for waking subjects. During sleep, the imagery can cause disorientation in observers but nothing lasting.",
  containment_status: "Living in the lower tiers in arranged spaces.",
  known_locations: ["Arranged private spaces, lower residential tiers"],
  contamination_risk: "None.",
  pacification_protocol: "N/A.",
  pitiable_qualities: "Their interiority is public. Every dream they have is visible to anyone nearby. They have no interior life that is actually private.",
  story_hooks: ["A runner sleeps accidentally near one of the Dreamed. What they see is specific and informative and clearly not their own dream. It is a place they need to find.", "One of the Dreamed has been dreaming the same thing for three weeks. Everyone who sleeps near them keeps seeing it. No one knows what it means. The dreamer doesn't either."],
  tags: ["lab_specimen", "dream_projection", "consciousness", "shared_experience", "no_threat", "divine_spark", "beautiful", "involuntary_sharing"]
});

write({
  id: uid(), name: "Chorus", type: "lab_specimen",
  aliases: ["The Linked", "Shared Bodies", "Resonant Subjects"],
  classification: "Sensory Link Network / Distributed Human Consciousness Collective",
  origin_lab: "BCI research program, maximum-fidelity shared experience trial.",
  origin_method: "A small group of subjects were linked via permanent BCI connection: not thoughts but sensory experience, shared in both directions simultaneously. Each member of the Chorus experiences what all the others experience at all times. Vision, touch, taste, sound, pain, pleasure — all shared, all simultaneous, all permanent.",
  substrate: "Human. Six subjects, permanently linked. They are individuals with individual minds. They share everything their bodies experience.",
  physical_description: "Normal individuals. The linkage is internal. What gives them away is how they respond to things — they turn toward stimuli experienced by a linked member on the other side of the room, react to things they cannot see but a linked member can.",
  behavioral_profile: "They move through the world with the specific grace of people who have six sources of sensory input instead of one. They notice more. They process more. They are also never alone, in any sense, ever, and they are never unwatched by anyone who matters to them.",
  threat_level: "None.",
  containment_status: "Distributed. They range widely but stay in contact — the sensory link has a range of approximately 2km before it begins to degrade painfully.",
  known_locations: ["Distributed across lower GLMZ, staying within range of each other"],
  contamination_risk: "None.",
  pacification_protocol: "N/A. Note: harm to one member of the Chorus is immediately felt by all. This is relevant to any engagement scenario.",
  pitiable_qualities: "They have never been alone since the link was established and they will never be alone again and they have had to reconcile their individual selves with the constant presence of five other people's experience and there are days when they cannot tell where they end and the Chorus begins.",
  story_hooks: ["One member of the Chorus has been captured. The others know exactly where they are and exactly what is being done to them.", "A member of the Chorus experienced something the others didn't understand — a vision, a sensation, something outside normal sensory experience. It is shared between all six of them now and none of them can identify the source."],
  tags: ["lab_specimen", "sensory_link", "collective", "bci", "no_threat", "divine_spark", "shared_consciousness", "never_alone"]
});

write({
  id: uid(), name: "The Echo", type: "lab_specimen",
  aliases: ["Memory Sound", "The Living Record", "Last Voice"],
  classification: "Perfect Audio Memory Organism / Involuntary Sound Replay Subject",
  origin_lab: "Acoustic research program, perfect audio memory as a surveillance application.",
  origin_method: "A subject received an implant intended to record and store audio for later directed replay — a living recording device. The directed replay component failed. The subject cannot control what they replay. They replay everything they have heard, in fragments, involuntarily, at unpredictable times. They have been collecting sound for eight years. The last sounds of three people who died in their presence are in there, replayed at random.",
  substrate: "Human, one subject.",
  physical_description: "Normal except that they are sometimes not speaking when sounds come from them.",
  behavioral_profile: "They have learned to identify the precursors of an involuntary replay and excuse themselves. They are deeply private. The things they have heard that they replay, they cannot choose to share and cannot choose to stop.",
  threat_level: "None.",
  containment_status: "Living in isolation by choice.",
  known_locations: ["Remote lower-tier space"],
  contamination_risk: "None.",
  pacification_protocol: "N/A.",
  pitiable_qualities: "They carry the last sounds of people who are gone. The sounds come out without permission at random intervals. They have no ability to grieve privately.",
  story_hooks: ["The Echo has heard something important — a conversation, a confession, a set of coordinates — and cannot reliably produce it on demand but cannot stop it from emerging at other times", "A family is trying to find the last words of a lost family member. The Echo was there. The last words are in there somewhere."],
  tags: ["lab_specimen", "audio_memory", "involuntary_replay", "surveillance_accident", "one_subject", "no_threat", "divine_spark", "tragic_beauty"]
});

write({
  id: uid(), name: "Seed Carriers", type: "lab_specimen",
  aliases: ["The Gardeners", "Green Ones", "Growing Things"],
  classification: "Engineered Plant Symbiosis / Involuntary Botanical Colonization Subject",
  origin_lab: "Urban agriculture research program, human-plant symbiosis for underground food production.",
  origin_method: "Subjects received engineered plant symbiote seeding intended to allow them to produce food crops from their own biological processes. The symbiote grew extensively. The subjects have become, over years, ambulatory ecosystems — plants grow from them, through them, wherever they go and wherever they have been.",
  substrate: "Human with extensive plant colonization. The plants are living and growing. The subjects are warm and the plants are using that warmth.",
  physical_description: "Covered in growing things. Moss on the forearms. Small flowers at the wrist and collar. Green along the hairline. They leave traces of growth wherever they pass — a touch on a wall produces, over days, a creeping green. They have been transforming the dark of the lower levels into something living for years.",
  behavioral_profile: "Gentle, slow-moving, inclined toward spaces where the growth can continue. They eat more than normal humans but less than the mass of plant matter they support would require — the photosynthetic contribution is real. They are drawn to any available light source.",
  threat_level: "None.",
  containment_status: "Several known subjects, lower levels.",
  known_locations: ["Areas of the lower levels that have unexpected plant growth trace back to Seed Carrier transit routes"],
  contamination_risk: "The plant growth they produce is not parasitic to other organisms but will colonize available surfaces extensively.",
  pacification_protocol: "N/A.",
  pitiable_qualities: "They are making the dark places grow. They did not ask to. They cannot stop. The green that traces their path through the underground is the closest thing to a legacy they have.",
  story_hooks: ["A map of Seed Carrier movement routes, derived from plant growth patterns, reveals that they have all been going to the same place", "A community in the lower levels has discovered that a Seed Carrier's regular passage through their corridor has been improving air quality over two years. They don't know the source. They are trying to find it to protect it."],
  tags: ["lab_specimen", "plant_symbiosis", "gardener", "no_threat", "divine_spark", "underground_growth", "beautiful", "tragic_beauty"]
});

write({
  id: uid(), name: "Bone Music", type: "lab_specimen",
  aliases: ["Resonance Subjects", "The Singing Bones", "Walking Instruments"],
  classification: "Skeletal Resonance Modification / Involuntary Harmonic Emission",
  origin_lab: "Acoustic research, bone conduction audio system trial.",
  origin_method: "Subjects' skeletal system was modified to function as a resonance body — bones structured and tuned to produce tonal output when subjected to movement. The modification was intended to allow internal audio systems to use the skeleton as a speaker, producing clear audio without external hardware. The internal audio systems were never installed. The skeleton was tuned. It produces tones when they move.",
  substrate: "Human. Seven subjects. The tones produced vary by movement and subject — each person produces their own harmonic signature.",
  physical_description: "Normal. The music gives them away.",
  behavioral_profile: "They move carefully in public to minimize the sound. In private, some of them move freely. The music their bodies make when they move freely has been described, by people who have heard it, as genuinely beautiful.",
  threat_level: "None.",
  containment_status: "Living in the lower tiers.",
  known_locations: ["Lower residential tiers — identified by sound by communities who know what to listen for"],
  contamination_risk: "None.",
  pacification_protocol: "N/A.",
  pitiable_qualities: "They cannot move without music. In public this is an exposure and a liability. In private it is something else. Some have decided it is something else all the time. They are walking carefully in a world that does not deserve the sound they make.",
  story_hooks: ["Two Bone Music subjects found each other and began moving together. What they produce together is not two separate harmonic signatures — it is a third thing, a composition neither could make alone.", "One of them is in trouble. The sound of their movement, usually clear and distinct, has changed. Someone who knows what to listen for is trying to find them."],
  tags: ["lab_specimen", "skeletal_resonance", "music", "involuntary_beauty", "no_threat", "divine_spark", "acoustic", "beautiful"]
});

write({
  id: uid(), name: "Halo Subjects", type: "lab_specimen",
  aliases: ["The Fortunate", "Blessers", "Lucky Ones"],
  classification: "Electromagnetic Field Anomaly / Technology-Beneficial Interaction",
  origin_lab: "BCI hardware research, electromagnetic biocompatibility testing.",
  origin_method: "Subjects received a BCI implant intended to test long-term electromagnetic biocompatibility. A manufacturing defect in six implants produced an unanticipated effect: the implants generate a low-level electromagnetic field that interacts with nearby electronic systems in a consistently beneficial way. Electronics near these subjects work better than they should — intermittent connections hold, degraded systems function, failing power cells stabilize. The effect cannot be replicated by direct engineering. It only happens in their presence.",
  substrate: "Human, six subjects. The implants are standard BCI hardware, externally.",
  physical_description: "Normal. The effect is not visible directly — it is visible in the behavior of electronics near them.",
  behavioral_profile: "They are aware of the effect. Some find it comforting. Some find the dependence that communities develop on them unsettling. They are sought out by people with failing equipment, by communities with unstable power, by anyone whose technology is at the edge of failure.",
  threat_level: "None.",
  containment_status: "Living in lower-tier communities that have figured out what they do.",
  known_locations: ["Communities with notably reliable electronics in the lower tiers"],
  contamination_risk: "None.",
  pacification_protocol: "N/A.",
  pitiable_qualities: "They are needed for something they cannot control and did not choose. The communities that depend on them cannot afford to let them leave.",
  story_hooks: ["A Halo Subject wants to leave the community that has been keeping them. The community has gently ensured that leaving is not straightforward.", "The equipment that only works near a specific Halo Subject contains something critical. The subject has been taken. The equipment is failing."],
  tags: ["lab_specimen", "electromagnetic", "technology_beneficial", "bci_accident", "no_threat", "divine_spark", "community_dependent", "involuntary_gift"]
});

write({
  id: uid(), name: "The Translucent", type: "lab_specimen",
  aliases: ["Glass Skin", "The See-Through", "Visible Ones"],
  classification: "Dermal Transparency Derivative / Progressive Biological Transparency",
  origin_lab: "Medical imaging research, tissue transparency geneware trial.",
  origin_method: "Subjects received geneware intended to make skin and subcutaneous tissue temporarily transparent for non-invasive medical imaging. The geneware did not produce temporary transparency. It produced progressive permanent transparency, beginning at the extremities and moving centrally over approximately three years until the subjects are now fully transparent from the surface through to the internal organs.",
  substrate: "Human. Eight subjects. Their internal anatomy is fully visible.",
  physical_description: "You can see through them. Standing in light, a Translucent person reveals the structure of a human being: the skeleton, the organs, the blood moving through vessels, the lungs expanding and contracting, the heart beating. The effect is extraordinary. The first response of most people who encounter them is a kind of reverence — the word used most often, across independent accounts, is 'beautiful.'",
  behavioral_profile: "Private, very. Every emotion is visible not in expression but in physiology — heart rate, blood flow, adrenaline response. They have the most visible bodies of any person alive. They cannot be opaque. Some have found peace with this and walk through the world as what they are. Some have not.",
  threat_level: "None.",
  containment_status: "Living in the lower tiers, some integrated into communities.",
  known_locations: ["Lower residential tiers", "One is known to live in a space that catches light from above — they have been described as something that happens to light"],
  contamination_risk: "None.",
  pacification_protocol: "N/A.",
  pitiable_qualities: "They are beautiful and they did not ask to be beautiful this way. Every internal process — fear, arousal, grief, joy — is fully visible to anyone who is looking. They are the most honest people in the world, by force.",
  story_hooks: ["A Translucent person is in a community that has come to treat them as something sacred. The Translucent person is not sure how to feel about this.", "A doctor studying a Translucent person discovers, while observing their internal anatomy, something that has no medical explanation — a structure that does not belong in a human body and has apparently always been there."],
  tags: ["lab_specimen", "transparency", "visible_anatomy", "geneware_accident", "no_threat", "divine_spark", "beautiful", "involuntary_honesty"]
});

write({
  id: uid(), name: "The Empaths", type: "lab_specimen",
  aliases: ["Pain Takers", "Absorbers", "The Suffering Kind"],
  classification: "Emotional State Reception Organism / Involuntary Affect Absorption",
  origin_lab: "Psychological research program, empathy enhancement trial for therapeutic applications.",
  origin_method: "Subjects received an enhancement to emotional processing designed to improve therapeutic effectiveness. The enhancement produced subjects who do not merely perceive others' emotional states — they receive them. Pain felt by someone nearby is felt by the subject. Grief nearby is grief inside them. Joy too, but joy is rarer in the environments they inhabit. They absorb the emotional content of the spaces they move through and it all registers as their own.",
  substrate: "Human. Eleven subjects.",
  physical_description: "Normal. The distress is internal and produces normal external stress markers: tension, pallor, signs of exhaustion.",
  behavioral_profile: "They avoid crowded, unhappy spaces. The lower tiers are difficult. The Undermarket on a bad day is unbearable. They live in the quietest spaces they can find and they are careful about proximity to suffering they cannot help.",
  threat_level: "None.",
  containment_status: "Scattered through the lower tiers.",
  known_locations: ["Quiet isolated spaces, lower tiers", "Some have found positions as mediators or counselors — the enhancement makes them extraordinarily effective at understanding what others feel"],
  contamination_risk: "None.",
  pacification_protocol: "N/A.",
  pitiable_qualities: "They feel what the city feels. The city feels a great deal that is not pleasant. They feel it all.",
  story_hooks: ["An Empath who works in a counseling role has absorbed something that isn't grief or pain — something colder, more structured, and not human. It came from someone in their last session. They cannot identify what it was but they cannot get it out.", "A runner needs to understand what happened to someone who cannot or will not speak. An Empath who was present may know."],
  tags: ["lab_specimen", "empathy", "affect_absorption", "therapeutic_accident", "no_threat", "divine_spark", "suffering", "involuntary_reception"]
});

write({
  id: uid(), name: "The Still Point", type: "lab_specimen",
  aliases: ["The Calm", "Eye", "The One That Quiets"],
  classification: "Neural Frequency Stabilization Organism / Involuntary Environmental Pacification",
  origin_lab: "Neurological research program studying seizure suppression via external electromagnetic fields.",
  origin_method: "One subject — the trial produced the effect in only one individual — received a neural modification that generates a specific frequency field extending approximately 3 meters. Within this field, neurological activity in other organisms stabilizes. Anxiety resolves. Pain diminishes. Aggression drops. Sleep comes easily. The subject cannot turn this off.",
  substrate: "Human, one subject. The field is generated by their own neural activity, involuntarily, at all times.",
  physical_description: "Normal. The effect in their vicinity is the only indication of what they are.",
  behavioral_profile: "They know what they do. They have become very careful about where they sit and who they sit near, because they alter the neural state of everyone around them and they do not have consent for this. They do not sit near people who are in pain if they can avoid it — the relief they provide is real and the dependence it can create is a weight they are aware of. They are the most cautious person in most rooms.",
  threat_level: "None, in the conventional sense. The ethical question of altering the neurological state of others without consent is not addressed in any DPS directive.",
  containment_status: "Living as quietly as possible.",
  known_locations: ["Known only to a very small number of individuals. Actively protected by those individuals."],
  contamination_risk: "None biological. The field effect does not persist after they leave.",
  pacification_protocol: "N/A.",
  pitiable_qualities: "They cannot be near suffering without reducing it, and they cannot stop reducing it, and reducing it without permission is itself a kind of violation that they think about a great deal.",
  story_hooks: ["The Still Point is in a situation that requires their active participation — something that will only work if they agree to be present and to let their field do what it does. They are considering what it costs.", "Someone has found the Still Point. They want to use them — to sit them in a specific room at a specific time, with specific people who need their anger taken from them for long enough to sign something. The Still Point is the last voluntary party in this scenario."],
  tags: ["lab_specimen", "neural_field", "pacification", "involuntary_gift", "one_subject", "no_threat", "divine_spark", "ethical_burden", "beautiful"]
});

console.log('\nDone. All 50 lab specimens written.');
