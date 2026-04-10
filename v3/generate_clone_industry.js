const fs = require('fs');
const path = require('path');
const crypto = require('crypto');

const outputDir = path.resolve(__dirname, '..', 'engine', 'data', 'documents');

function generateId() {
  return crypto.randomBytes(16).toString('hex');
}

const documents = [
  {
    name: "The Vessel Program",
    document_type: "leaked_memo",
    author: "Lazarus Pharmaceuticals — Internal Operations Division",
    date: "2287-03-14",
    classification: "leaked",
    credibility: "leaked",
    description: `What follows is an internal Lazarus Pharmaceuticals operations manual, classification level ONYX, originally distributed to facility directors overseeing Biological Reserve Unit cultivation floors. The document was exfiltrated by a former bioethics compliance officer who resigned in 2286 and subsequently disappeared. It describes, in the sterile language of corporate logistics, the full lifecycle of what Lazarus officially designates "Biological Reserve Units" — cloned human bodies grown to physiological maturity in accelerated gestation tanks, maintained in a persistent vegetative state, and harvested for organs as client needs arise. The program's internal codename is VESSEL. The document refers to the clones exclusively as "units."

Each unit is genetically sequenced from a paying client's tissue sample, grown over an eleven-month accelerated cycle in amniotic suspension tanks, and decanted into long-term maintenance cradles upon reaching target biomass. Neural development is chemically suppressed during gestation — the cerebral cortex forms but is never activated. The result is a body that breathes, metabolizes, heals, and grows, but has never experienced a single moment of consciousness. Lazarus's legal team classifies them as "cultivated biological material." The facility staff calls them sleepers. There are entire floors of them in the GLMZ campus — rows of bodies on ventilators, hair growing, fingernails lengthening, skin warm to the touch. Alive by every metric except the one that matters.

The harvesting process is described with the same procedural detachment as equipment maintenance. When a client requires an organ — a kidney, a liver, a heart — the matched unit is prepped for extraction. Single-organ harvests leave the unit viable for future use; the body regenerates with pharmaceutical assistance. Multi-organ harvests are terminal. The document notes that a single unit can provide an average of 3.7 "material events" before total systemic failure. The manual includes a troubleshooting section for when units develop spontaneous neural activity — the recommended response is increased chemical suppression, not investigation. The section is four pages long. It has been revised six times.

Facility headcount as of the document's date: 1,847 active units across three GLMZ campuses. Monthly operating cost per unit: 14,200 Φ. Client tier requirement: Tier 4 minimum, with Tier 5 clients receiving dedicated units and priority scheduling. The waiting list for new unit cultivation is fourteen months. The document ends with a quarterly performance summary noting a 2.3% improvement in organ viability rates and a recommendation to expand cultivation capacity by 40% to meet projected demand. There is no ethical review section. There is no mention of ethics at all. The word does not appear in the document.

The final appendix — labeled "Contingency Protocol STILLWATER" — describes procedures for facility compromise, including rapid unit termination via chemical injection and incineration of biological material. The protocol can be executed in under ninety minutes for a full facility. It has been tested twice. The appendix does not say why.`,
    related_entities: ["Lazarus Pharmaceuticals", "GLMZ"],
    story_hooks: [
      "The missing bioethics compliance officer who leaked this document — where are they now, and who is hunting them?",
      "Contingency Protocol STILLWATER has been tested twice — what triggered those tests?"
    ],
    tags: ["document", "clone", "lazarus", "vessel_program", "organ_harvesting", "leaked", "biological_reserve_unit", "ethics", "meridian_88"]
  },
  {
    name: "Companion Bodies — The Tier 5 Market",
    document_type: "investigation",
    author: "Maren Solvieg, Independent Journalist",
    date: "2288-01-22",
    classification: "suppressed",
    credibility: "verified",
    description: `The term "companion body" entered Tier 5 vocabulary sometime around 2280, though its exact origin is disputed. The concept is simple and horrifying: a brain-dead genetic duplicate of yourself, maintained in a private facility, available as both a spare-parts reserve and — for those willing to pay the ultimate premium — a vessel for consciousness transfer when your original body fails. It is the final luxury. It is also, depending on which jurisdiction's laws you consult, somewhere between a medical device, a piece of property, and a person. Lazarus Pharmaceuticals offers the service through a subsidiary called Elysian Health Partners. The brochure uses words like "continuity" and "legacy preservation." It does not use the word "clone."

The investigation began with facility records obtained from a disgruntled maintenance contractor. The documents describe private suites — not medical bays, suites — where companion bodies are kept. The rooms have furniture. Some have windows. The bodies lie in adjustable beds with high-thread-count sheets, wearing clothes selected by their owners. One client sends seasonal wardrobe updates. Another has a standing order for fresh flowers every Monday. The bodies don't see the flowers. They don't feel the sheets. Their eyes never open. But the rooms are maintained to the standards of a luxury hotel, because the people paying for them need to believe that what they're doing is somehow civilized.

Interviews with facility staff paint a picture that corporate PR would prefer to remain invisible. Three nurses, speaking on condition of anonymity, described the psychological toll of caring for bodies that are simultaneously their patients and their patients' property. One described a Tier 5 client who visits his companion body every Sunday, sits in a chair beside the bed, and reads aloud from financial reports. He calls the body by his own name. Another described a client who insisted on cosmetic procedures for her companion body — hair styling, skin treatments, manicures — so that it would "look its best" for the eventual transfer. A third nurse, who lasted only four months before resigning, said simply: "They're not dead. I don't care what the paperwork says. You can feel their breath on your hand."

The legal framework enabling companion bodies is a masterwork of corporate lobbying. Lazarus spent an estimated 280 million Φ over six years ensuring that cloned bodies without activated neural tissue are classified as "cultivated biological assets" rather than persons under GLMZ corporate charter law. The distinction rests on a single criterion: demonstrated cognitive activity. No cognition, no personhood. The fact that cognition could be initiated at any time — that the infrastructure for thought exists in every companion body's intact brain — is legally irrelevant. A loaded gun isn't a shooting. A dormant brain isn't a mind. The analogy is Lazarus's own, from their legal briefs. They seem proud of it.

The market is smaller than one might expect and larger than anyone should be comfortable with. Best estimates suggest between 200 and 400 active companion body contracts in GLMZ, each costing between 8 and 15 million Φ annually for maintenance, medical monitoring, and facility fees. The clients are exclusively Tier 5 — executives, legacy families, individuals for whom death is not a personal inevitability but a logistical problem to be solved. Several clients have maintained companion bodies for over a decade. At least two are known to have completed consciousness transfers, though Lazarus denies this officially. The bodies that were left behind — the original bodies — were cremated. No death certificates were filed. The clients simply continued living, younger, in rooms they had been decorating for years.`,
    related_entities: ["Lazarus Pharmaceuticals", "Elysian Health Partners", "GLMZ"],
    story_hooks: [
      "The disgruntled maintenance contractor who leaked facility records — are they still alive?",
      "At least two consciousness transfers have allegedly been completed — who are the clients living in their younger bodies?"
    ],
    tags: ["document", "clone", "lazarus", "companion_body", "tier_5", "consciousness_transfer", "ethics", "investigation", "meridian_88"]
  },
  {
    name: "How Many Clones Are There?",
    document_type: "investigation",
    author: "Dr. Kael Okonkwo, GLMZ Census Analytics (Unofficial)",
    date: "2288-06-09",
    classification: "restricted",
    credibility: "disputed",
    description: `The question sounds simple. The answer is anything but. No official body in GLMZ tracks the clone population because no official body in GLMZ acknowledges that there is a clone population. Cloned bodies — whether Lazarus's Biological Reserve Units, private companion bodies, or products of unauthorized operations — do not appear on any census, any registry, any demographic survey. They consume resources. They occupy space. They breathe air and metabolize nutrients and generate medical waste. They are, by every logistical measure, residents of the Greater Lazarus Manufacturing Zone. They are counted as none of them.

This analysis uses three independent methodologies to estimate the total number of clone bodies currently being maintained in GLMZ. The first is energy analysis: clone maintenance facilities consume distinctive power signatures — continuous life support, climate control, accelerated gestation tanks — that can be identified in municipal power grid data. Cross-referencing consumption patterns with known facility locations and extrapolating to anomalous high-draw sites produces an estimate of 87 probable clone maintenance facilities in the GLMZ, of which only 23 are registered with Lazarus. The remaining 64 are either subsidiary operations under shell companies or unauthorized facilities. Combined capacity estimate: 6,200 to 9,400 bodies.

The second methodology is supply chain analysis. Clone bodies require specific pharmaceutical inputs — neural suppressants, accelerated growth hormones, anti-rejection compounds — in quantities that far exceed the legitimate medical demand of GLMZ's registered population. Pharmaceutical distribution records, obtained through a source inside Lazarus's logistics division, show that the GLMZ receives approximately 340% more neural suppressant compound than its hospitals and clinics could account for. Working backward from dosage requirements per body, the supply chain data suggests a population of 5,500 to 8,800 maintained clone bodies. The third methodology is the simplest and most disturbing: a Lazarus whistleblower, a former facility technician, provided a direct count from a single mid-sized campus — 412 active units in a facility designed for 500. If that occupancy rate holds across known and estimated facilities, the total reaches 7,100 to 10,600.

The convergence of all three methods produces a combined estimate of 4,000 to 11,000 brain-dead clone bodies currently being maintained in the Greater Lazarus Manufacturing Zone. The wide range reflects the inherent uncertainty of counting a population that powerful interests have every reason to hide. The midpoint — roughly 7,500 — exceeds the population of at least fourteen individual Shelf blocks. These bodies have heartbeats. They have blood types. They have fingerprints that match real people walking the streets of GLMZ. If they were counted as residents, they would constitute the single largest demographic cohort in the GLMZ that cannot vote, speak, or object to its own existence.

The political implications are staggering and studiously ignored. GLMZ's corporate charter allocates infrastructure resources — water, power, waste processing — based on registered population. Thousands of unregistered biological entities consuming resources means the official population is subsidizing clone maintenance without knowing it. Every resident of the GLMZ is paying, fractionally, to keep these bodies alive. The philosophical implications are worse. Somewhere in this city, there are more clone bodies than there are teachers, or firefighters, or children under the age of five. They are the largest invisible community in GLMZ. They have never opened their eyes. Some of them have been breathing longer than most Shelf residents have been alive.`,
    related_entities: ["Lazarus Pharmaceuticals", "GLMZ", "Greater Lazarus Manufacturing Zone"],
    story_hooks: [
      "64 unregistered clone facilities operating under shell companies — who runs the ones Lazarus doesn't?",
      "The whistleblower facility technician who provided direct counts — what happened to them after this analysis was published?"
    ],
    tags: ["document", "clone", "lazarus", "census", "population", "investigation", "GLMZ", "meridian_88", "data_analysis", "ethics"]
  },
  {
    name: "The Ethics of the Sleeper",
    document_type: "academic_paper",
    author: "Dr. Yumiko Tanaka-Ferreira, Department of Applied Ethics, Korolev University",
    date: "2287-11-03",
    classification: "restricted",
    credibility: "verified",
    description: `This paper examines the moral status of the brain-dead human clone — designated herein as the "sleeper" — through the lenses of personhood theory, property law, and medical ethics. The sleeper presents a genuinely novel philosophical problem: it possesses human DNA, human physiology, a human heartbeat, and an intact (if dormant) human neural architecture. It grows. It heals. It can, under specific experimental conditions, be activated into a state of consciousness. By any biological definition, it is human. By current GLMZ corporate charter law, it is equipment. The gap between these two classifications is not a gray area. It is an abyss.

Traditional personhood frameworks fail spectacularly when applied to the sleeper. Cognitive criteria — the capacity for thought, self-awareness, language — would deny personhood to the sleeper, but also to the comatose, the anesthetized, and the sleeping. Biological criteria — human DNA, human form, biological viability — would grant personhood to the sleeper, but GLMZ law explicitly excludes "cultivated biological material" from biological personhood protections. Potentiality arguments — the sleeper COULD become conscious if activated — are dismissed by legal precedent establishing that potential states do not confer current rights. An acorn is not an oak. A dormant brain is not a mind. The analogy is legally convenient and philosophically bankrupt, because unlike an acorn, the sleeper's brain is already fully formed. It is not becoming anything. It is being prevented from being what it already is.

The property framework is equally inadequate, though for different reasons. Property does not breathe. Property does not have a heartbeat that quickens under stress, as sleeper bodies demonstrably do. Property does not exhibit REM-like neural activity during maintenance cycles, as approximately 8% of sleepers do, according to suppressed Lazarus internal studies. Property does not have immune systems that reject foreign tissue, or endocrine systems that respond to external stimuli, or bodies that flinch — flinch — when subjected to painful stimulation, as documented in three separate facility incident reports. The legal classification of the sleeper as property requires ignoring everything the sleeper's body is telling us about what it is. We have decided not to listen because listening would be expensive.

The medical ethics dimension introduces perhaps the most troubling consideration. Medical devices do not require ongoing consent frameworks because they are not moral patients. But the sleeper can be converted from a non-conscious entity to a conscious one through a known medical procedure. This means that every sleeper exists in a state of imposed non-personhood — their lack of consciousness is not natural but manufactured, maintained through continuous chemical suppression of neural activity that their brains are architecturally prepared to support. The sleeper is not unconscious in the way a rock is unconscious. The sleeper is unconscious in the way a person who has been drugged is unconscious. The distinction matters. Or it should.

This paper concludes that the moral status of the sleeper cannot be coherently determined within existing legal and philosophical frameworks, and that this incoherence is not accidental but engineered. The classification of sleepers as property serves the financial interests of Lazarus Pharmaceuticals and its Tier 5 clientele. The philosophical ambiguity is a feature, not a bug — it allows different stakeholders to believe whatever is most convenient. The owner believes the sleeper is a medical device. The nurse believes the sleeper is a patient. The ethicist believes the sleeper is a person. And the sleeper, if it could believe anything, would believe it is alive. The fact that the answer to "what is a sleeper?" depends entirely on who owns it is not a philosophical conclusion. It is an indictment. The most damning thing about GLMZ's clone industry is not that it creates brain-dead bodies. It is that it has created a legal and philosophical architecture in which the definition of personhood is a function of purchasing power.`,
    related_entities: ["Lazarus Pharmaceuticals", "Korolev University", "GLMZ"],
    story_hooks: [
      "The suppressed Lazarus studies showing 8% REM activity in sleepers — what else did those studies find?",
      "Dr. Tanaka-Ferreira's academic career after publishing this paper — did Lazarus retaliate?"
    ],
    tags: ["document", "clone", "lazarus", "ethics", "philosophy", "personhood", "academic_paper", "sleeper", "consciousness", "meridian_88"]
  },
  {
    name: "My Father's Other Body",
    document_type: "personal_account",
    author: "Celeste Adeyemi-Park (pseudonym)",
    date: "2288-04-17",
    classification: "leaked",
    credibility: "verified",
    description: `I was nineteen when I found out. Not because anyone told me — my family doesn't tell, it arranges. I found the invoice on my father's desk, physical paper, which should have been my first clue that something was wrong because my father hasn't used paper for anything since I was a child. A monthly maintenance statement from Elysian Health Partners, a name I didn't recognize, for "Continuation Suite 7, Client A-4419." The amount was more than most people in the Shelf make in a year. I looked up Elysian Health Partners. I found nothing. I asked my mother. She told me to forget I'd seen it. I asked my father. He sat me down, poured himself a drink, and explained that he had a companion body.

He said it like he was telling me about a vacation home. Casual. Practical. "It's just planning ahead, sweetheart." He'd had it grown eleven years ago, when he turned fifty. It was maintained in a private suite in a facility he described as "very comfortable." He visited quarterly for what he called "compatibility checks" — medical assessments ensuring the clone body remained viable for eventual consciousness transfer. He told me the body was twenty-three years old physiologically, the age he was when he married my mother. He told me it was wearing clothes my mother had picked out. He told me it looked just like him, which of course it did, because it was him. He told me all of this over dinner. We had seared tuna. I don't eat fish anymore.

My mother took me to visit the facility six months later. She said it would help me "understand." The suite was beautiful — hardwood floors, real plants, a window that I later learned was a high-resolution display simulating an ocean view. The bed was king-sized. Medical equipment was built into the walls, disguised as furniture. And in the bed was my father. Not my father. My father's face on a body twenty years younger, breathing slowly, eyes closed, chest rising and falling with mechanical regularity. His hands were folded on his chest. His hair was the black my father's used to be before it went gray. He looked peaceful. He looked like he was sleeping. My mother adjusted the collar of his pajamas and said, "I picked these out last month. The blue brings out his eyes." His eyes were closed. His eyes had never opened.

I stood in that room for eleven minutes. I know because I watched the clock on the wall — another decorative touch, a clock in a room for someone who will never need to know the time. I watched the body breathe. I watched my mother smooth the sheets. I tried to understand what I was feeling and I couldn't, because there is no framework for this. There is no self-help book for "my father has a spare body in a luxury suite and my mother dresses it." I felt grief, but nobody had died. I felt horror, but nothing violent was happening. I felt something close to jealousy, which makes no sense — jealous of what? A body that has never thought a thought? But that body will get to be young when my father is old. That body will get to keep going when my father's original heart stops. That body has a future, and I am not sure my father's original one does, because why would you maintain the old model when the new one is waiting?

I don't visit the facility anymore. I don't talk to my father about it. At family dinners I sometimes catch myself studying his face, looking for signs of age, calculating how long before he decides the original model has depreciated enough to trade in. My therapist says this is a normal response to an abnormal situation. My therapist does not know what the situation is — I told her my father is planning an elective surgery. It's not entirely a lie. It's just that the surgery involves dying in one body and waking up in another, and the body he'll wake up in has been breathing in a room with an ocean-view screen for eleven years, wearing pajamas my mother picked out, and I am supposed to be okay with this because we can afford it. Being Tier 5 means never having to call the thing by its name.`,
    related_entities: ["Lazarus Pharmaceuticals", "Elysian Health Partners"],
    story_hooks: [
      "Celeste's father — when he eventually transfers, will his family accept the 'new' version as the same person?",
      "The therapist who doesn't know the truth — what happens when companion bodies become public knowledge?"
    ],
    tags: ["document", "clone", "companion_body", "tier_5", "personal_account", "family", "consciousness_transfer", "ethics", "identity", "lazarus"]
  },
  {
    name: "Braindead Pets",
    document_type: "academic_paper",
    author: "Dr. Noelle Achterberg, GLMZ Institute for Social Anthropology",
    date: "2288-08-02",
    classification: "restricted",
    credibility: "verified",
    description: `The term "braindead pet" emerged in Tier 5 social circles approximately four years ago, initially as dark humor and subsequently as casual shorthand. It refers to a companion body — a brain-dead genetic clone maintained in a private facility — and its widespread adoption reveals more about the ultra-wealthy relationship with their clones than any corporate brochure or ethics paper. The term is simultaneously affectionate and dismissive, intimate and dehumanizing. It acknowledges the clone as something that requires care while categorizing it as something less than human. It is, in the precise anthropological sense, a domestication narrative. The Tier 5 elite have domesticated their own genetic material.

This study is based on interviews with 34 individuals connected to companion body ownership — 12 owners, 8 family members, and 14 facility staff — conducted over eighteen months. The behavioral taxonomy that emerges is striking in its variety. At one end of the spectrum are owners who have never visited their clone and show no emotional attachment whatsoever; the companion body is an insurance policy, maintained the way one maintains a backup generator. These owners tend to use clinical language — "the unit," "the asset," "the reserve." At the other end are owners who visit weekly, who have furnished their clone's suite with personal items, who speak to the body, who touch it. One owner, a woman in her seventies, brings fresh flowers to her clone's room every week — orchids, specifically, because they were her favorite when she was the age her clone appears to be. When asked why, she was silent for a long time before saying, "I don't know. I genuinely don't know why I do it. But I can't stop."

The middle ground is where the most psychologically complex behaviors emerge. Several owners described a phenomenon the research team has termed "identity bleed" — a gradual erosion of the psychological boundary between self and clone. One owner reported dreaming as his clone, lying in the bed, eyes closed, unable to move. Another described a growing conviction that her clone was "waiting for her" and that the transfer was not a choice but an obligation — that the clone body had been waiting patiently for decades and deserved to be used. A third owner, a retired executive, confessed that he had begun to think of his aging body as "the temporary one" and his clone as "the real me, just not yet." Identity bleed correlates strongly with frequency of visits and years of ownership. The longer you maintain a younger version of yourself, the more you begin to see your current self as the draft and the clone as the final copy.

Family dynamics around companion bodies are equally complex and almost entirely unexamined in existing literature. Spouses of companion body owners reported a range of responses from pragmatic acceptance to profound existential distress. Three spouses described the clone as "the other woman" or "the other man" — not as a romantic rival, but as a competing version of their partner that they would eventually be expected to accept as the original. Children of owners, particularly those who learned about the companion body in adolescence, showed the highest rates of psychological disturbance. The clone represents a parent who has chosen to outlive the natural order, who has purchased a way to remain while everything and everyone around them ages and dies. Two interview subjects described their parent's companion body as a form of abandonment — "He's already planning to leave us behind. He's just doing it slowly."

Facility staff occupy a unique anthropological position: the caretakers of bodies that belong to someone else, in every sense. Staff interviews revealed a near-universal tendency to develop what one nurse called "phantom personhood" — the involuntary attribution of personality to bodies that have none. Staff give the clones nicknames. They apologize before performing medical procedures. They report feeling watched, despite knowing the bodies cannot see. One long-term staff member described her relationship with a clone she had cared for over seven years: "I know she's not in there. I know that. But I also know that she likes it when I open the blinds in the morning. I know that's not real. But I do it anyway, every day." The line between knowing and feeling, it seems, is thinner than Lazarus's legal briefs would suggest. The braindead pet is not a person. But it is not nothing. And the space it occupies — between furniture and family, between property and patient — is the space where GLMZ's most uncomfortable truths live.`,
    related_entities: ["Lazarus Pharmaceuticals", "GLMZ", "GLMZ Institute for Social Anthropology"],
    story_hooks: [
      "Identity bleed in long-term companion body owners — is this a psychological phenomenon or something more, given the genetic connection?",
      "The staff member who has cared for the same clone for seven years — what happens to her when that clone is harvested or transferred into?"
    ],
    tags: ["document", "clone", "companion_body", "tier_5", "anthropology", "academic_paper", "identity", "ethics", "braindead_pet", "meridian_88", "lazarus"]
  },
  {
    name: "The Wake-Up Problem",
    document_type: "academic_paper",
    author: "Dr. Ren Hashimoto & Dr. Priya Venkatesh-Oduya, Lazarus Neurological Research Division (Resigned)",
    date: "2286-09-28",
    classification: "classified",
    credibility: "suppressed",
    description: `This paper documents the neurological outcomes of three cases of unintended clone activation — instances in which brain-dead clone bodies transitioned to a state of consciousness without deliberate medical intervention. All three cases occurred at Lazarus Pharmaceuticals facilities between 2284 and 2286. All three were classified at the highest corporate security level. Both authors have since resigned from Lazarus. This paper was never submitted for peer review. It exists because we could not, in professional conscience, allow the data to disappear.

The mechanism of accidental activation remains incompletely understood. In all three cases, the clone bodies had been maintained in standard vegetative protocols with continuous neural suppression via intravenous administration of compound LZ-4471. In Case 1, a supply chain disruption resulted in a 72-hour gap in LZ-4471 delivery to a secondary facility. In Case 2, a dosage calculation error reduced neural suppression to approximately 15% of therapeutic levels for eleven days. In Case 3, no external cause was identified — the clone's neural tissue spontaneously overcame standard suppression levels, suggesting either individual neurological variance or the possibility that prolonged maintenance creates conditions favorable to autonomous activation. Case 3 is the most troubling because it implies that any sleeper, given sufficient time, might wake up on its own.

The phenomenology of activation was consistent across all three cases and deeply disturbing. The clones — bodies that had never experienced consciousness, that had no memories, no language acquisition, no sensory experience beyond the monotony of a maintenance cradle — exhibited immediate and overwhelming emotional distress. Case 1 (female, physiological age 26) began vocalizing within four minutes of reaching detectable neural activity — not words, but sustained, arrhythmic screaming that staff described as "animal-like." Case 2 (male, physiological age 31) displayed severe motor agitation, pulling at IV lines and monitoring equipment with uncoordinated but forceful movements, while emitting a continuous low moan. Case 3 (female, physiological age 24) was the most unsettling: she was quiet. She opened her eyes. She looked around the room. She looked at the medical staff. She did not scream. She wept, silently, for the entire duration of her consciousness. Staff reported that her eyes tracked movement and that she appeared to be trying to understand what she was seeing. She could not speak. She had never learned how.

All three activated clones were re-sedated — Case 1 after 47 minutes, Case 2 after 3 hours and 12 minutes due to difficulty establishing IV access during motor agitation, Case 3 after 6 hours at the discretion of the attending physician, who later described the delay as "the worst professional decision of my career, and also the only humane one — she was looking at me, and I couldn't just put her back under without... without acknowledging that she was there." Upon re-sedation, all three clones returned to baseline vegetative states. Follow-up monitoring showed no lasting neural changes in Cases 1 and 2. Case 3 exhibited persistently elevated neural activity for 19 days following re-sedation, including pronounced REM-like cycles. She was transferred to a high-security unit. We were not informed of her subsequent status.

The implications for the clone industry are existential and have been systematically suppressed. If brain-dead clones can achieve consciousness — spontaneously, accidentally, or through trivial lapses in chemical suppression — then every sleeper in every facility is not a medical device but a sedated person. The legal distinction between a brain-dead clone and a person rests entirely on the absence of cognitive activity, and that absence is maintained artificially. Remove the drug, and the person appears. This is not a theoretical concern. It has happened three times that we know of. Lazarus's response to each case was identical: re-sedate, classify, suppress. The activated clones were not studied. They were not given names. They were returned to inventory. This paper exists because Case 3 looked at one of us, and we understood — in a way that no amount of corporate classification can undo — that she was looking back.`,
    related_entities: ["Lazarus Pharmaceuticals", "Lazarus Neurological Research Division"],
    story_hooks: [
      "Case 3 — the quiet clone who looked at the staff and wept — what happened to her after transfer to the high-security unit?",
      "The spontaneous activation in Case 3 suggests any sleeper might eventually wake up on its own — how many have, without anyone noticing?"
    ],
    tags: ["document", "clone", "lazarus", "consciousness", "activation", "classified", "neurology", "ethics", "sleeper", "wake_up", "suppressed"]
  },
  {
    name: "When the Parts Run Out",
    document_type: "investigation",
    author: "Joaquin Reyes-Naidu, The Meridian Independent",
    date: "2288-02-15",
    classification: "leaked",
    credibility: "verified",
    description: `The official Lazarus Pharmaceuticals lifecycle for a Biological Reserve Unit ends with a designation: "Decommissioned." The word appears on internal tracking systems when a unit has provided its final viable organ harvest and is no longer capable of sustaining further material events. According to Lazarus's public-facing materials, decommissioned units are "processed with dignity in accordance with applicable bioethics standards." According to three facility workers who spoke to this reporter over a period of seven months, the reality is considerably more complicated than that.

Worker A, a crematorium technician employed at Lazarus's South Campus for four years, confirmed that the majority of decommissioned units are cremated within 48 hours of final harvest. The bodies arrive on gurneys, covered in standard medical draping, and are processed through industrial cremation units rated for biological waste. "They come in as bodies and leave as ash," Worker A said. "The ash goes into standard medical waste containers. No ceremony. No identification. Just weight and date." Worker A estimates he has cremated approximately 300 bodies during his employment. He does not call them clones. He does not call them people. He calls them "the quiet ones." He requested that this interview be conducted in a bar, not his home, because his wife does not know what he does for a living.

Worker B, a facility maintenance engineer, told a different story. Not all decommissioned units are cremated. Some — Worker B estimates 10 to 15 percent — are retained in what internal documentation refers to as "Extended Utility Protocol." These are bodies that have been harvested multiple times but remain physiologically viable through aggressive use of synthetic organ replacements, cloned tissue grafts, and mechanical life support augmentation. Worker B described units he had serviced that contained more replacement parts than original tissue — synthetic kidneys, a lab-grown liver, mechanical cardiac assist devices, artificial vascular networks. "At some point," Worker B said, "you're not looking at a clone anymore. You're looking at a life support system that grew a person around it." These extended utility units are maintained for ongoing tissue and blood product harvesting — bone marrow, plasma, stem cells, skin grafts. They are, in a sense, biological factories operating inside a human-shaped vessel. Worker B has filed two internal ethics complaints about the Extended Utility Protocol. Both were acknowledged. Neither received a response.

Worker C, who was employed as a biomedical disposal specialist for only seven months before quitting, provided the most disturbing testimony. She described a sub-basement level at the North Campus facility that she was assigned to clean once per week. The level contained approximately forty bodies in various states of harvest — some missing limbs, some with open surgical sites that had been left to heal without closure, some connected to machines she did not recognize. "They were breathing," she said. "All of them. Some of them were making sounds. Not words. Just... sounds. Like the air going in and out was hitting something wrong." She described one body that had been so extensively harvested that she could not initially determine its orientation — which end was the head. She quit the following day. She has not been able to provide more specific details because, she says, her memory of that level has become "blurry," which she attributes to a mandatory facility beverage she was required to consume before accessing secure areas.

Lazarus Pharmaceuticals declined to comment on any specific claims in this investigation. A corporate spokesperson provided a prepared statement affirming that "all biological material is handled in strict accordance with GLMZ corporate charter regulations and internal bioethics guidelines." The statement did not address the Extended Utility Protocol, the sub-basement level, or the status of bodies maintained beyond standard decommission criteria. When pressed, the spokesperson said: "A Biological Reserve Unit is not a patient. It is not a person. It is a cultivated medical resource, and its lifecycle is managed accordingly." The spokesperson did not explain what "accordingly" means. Based on the testimony gathered for this investigation, it means whatever Lazarus needs it to mean on any given day.`,
    related_entities: ["Lazarus Pharmaceuticals", "The Meridian Independent", "GLMZ"],
    story_hooks: [
      "The mandatory facility beverage that causes memory blurring — what is Lazarus giving its workers, and is it related to compound LZ-4471?",
      "The extended utility bodies that are more synthetic than organic — at what point do they fall under synthetic life regulations instead of clone regulations?"
    ],
    tags: ["document", "clone", "lazarus", "organ_harvesting", "decommission", "investigation", "ethics", "extended_utility", "meridian_88", "whistleblower"]
  },
  {
    name: "The Clone Underground",
    document_type: "investigation",
    author: "Anonymous (attributed to Shelf District Collective Journalism Network)",
    date: "2288-05-30",
    classification: "leaked",
    credibility: "disputed",
    description: `Lazarus Pharmaceuticals would like you to believe they are the only game in town. They are not. Beneath the sanitized floors and corporate euphemisms of the official clone industry, a parallel market has been operating in the Underworld and Shelf districts of GLMZ for at least six years. It is cheaper. It is faster. It is considerably more dangerous. And unlike the Tier 5 luxury of companion bodies and the clinical sterility of the Vessel Program, the clone underground serves anyone with enough Φ and enough desperation to walk through the right door.

The operations are scattered across at least a dozen locations identified during this investigation — converted industrial spaces in the Shelf, sealed-off utility corridors in the Underworld, and at least two operations running inside decommissioned Lazarus subsidiary facilities that were supposedly demolished. The technology is recognizable but degraded: accelerated gestation tanks built from salvaged medical equipment, neural suppression administered through imprecise chemical cocktails rather than Lazarus's proprietary compound LZ-4471, growth cycles compressed from Lazarus's eleven months to as few as four. The results reflect the shortcuts. Underground clones exhibit higher rates of tissue malformation, organ rejection, and what operators call "neural bleed" — unintended consciousness fragments that manifest as involuntary muscle movement, vocalization, or in the worst cases, something that looks very much like fear.

Organ pricing in the underground market runs approximately 30 to 40 percent of Lazarus's rates, which makes the service accessible to Tier 3 and even some Tier 2 clients. A kidney runs 40,000 to 60,000 Φ. A liver, 85,000 to 120,000 Φ. A heart — rare due to the technical difficulty of maintaining cardiac viability in fast-grown clones — can reach 300,000 Φ. Full companion bodies, grown to specification from a client's genetic material, are available for 1.5 to 3 million Φ, compared to Lazarus's estimated 8 to 15 million Φ annual maintenance cost. The catch, beyond the obvious medical risks, is quality. Underground organs fail at roughly triple the rate of Lazarus products. Rejection episodes are more severe. And the clone bodies themselves are less stable — several sources described underground companion bodies that aged at accelerated rates post-decanting, appearing to gain a decade of physiological age every two years.

The most disturbing aspect of the clone underground is not the discount organs or the makeshift facilities. It is the sourcing. Lazarus grows its clones from client tissue samples, ensuring genetic match. The underground is not always so scrupulous. Three separate sources confirmed the existence of operations that clone individuals without their knowledge or consent, using genetic material obtained from medical facilities, black market tissue banks, or even environmental DNA collection — shed skin cells, hair follicles, saliva from discarded containers. These unauthorized clones are grown as generic organ stock, tissue-typed and matched to buyers after cultivation rather than before. The implications are stark: anyone in GLMZ who has visited a medical facility, used a public restroom, or discarded a coffee cup could theoretically have a clone growing in a Shelf basement right now.

And then there are the bodies that weren't grown blank. Two sources, speaking independently and with visible distress, described underground operations that do not grow clones from scratch but instead acquire living individuals — typically Underworld residents with no registered identity, no social connections, no one who would notice their absence — and subject them to chemical neural suppression to render them brain-dead before harvesting their organs. These are not clones. They are people, sedated into the same state as clones, processed through the same supply chain, sold as the same product. When asked how to distinguish a real clone from a suppressed person, one operator shrugged and said: "You can't. That's the point." The clone underground does not merely replicate Lazarus's sins at lower fidelity. It extends them to their logical conclusion: in a system where the only thing separating a person from a product is whether their brain is active, deactivating someone's brain turns them into inventory.`,
    related_entities: ["Lazarus Pharmaceuticals", "GLMZ", "Shelf District", "Underworld"],
    story_hooks: [
      "Underground operations running in supposedly demolished Lazarus subsidiary facilities — is Lazarus secretly enabling the black market to maintain deniability?",
      "People being chemically brain-killed and sold as clone stock — how many missing persons cases in the Underworld are actually organ harvesting?"
    ],
    tags: ["document", "clone", "lazarus", "black_market", "underground", "organ_harvesting", "investigation", "shelf_district", "underworld", "ethics", "meridian_88"]
  },
  {
    name: "I Was Cloned Without Consent",
    document_type: "personal_account",
    author: "Name Withheld (Tier 3 Resident, GLMZ)",
    date: "2287-08-11",
    classification: "leaked",
    credibility: "leaked",
    description: `I am violating a non-disclosure agreement by allowing this testimony to be published. The settlement I received included a clause stipulating that any breach of confidentiality would result in forfeiture of the settlement amount and criminal prosecution under GLMZ corporate charter intellectual property statutes. I have been advised that this document, if traced to me, could result in imprisonment. I am publishing it anyway, because what Lazarus Pharmaceuticals did to me is not a proprietary secret. It is a crime, and the fact that they were able to settle it as a civil matter tells you everything you need to know about whose laws these are.

In 2285, I underwent a routine gynecological procedure at a Lazarus-affiliated medical center in the Mid-Ring district. A tissue sample was collected as part of standard diagnostics. I signed a consent form that authorized the use of my tissue for "diagnostic purposes and related medical applications." I did not read the fine print. Nobody reads the fine print. The fine print, I later learned, included a clause granting Lazarus Pharmaceuticals and its subsidiaries a perpetual, irrevocable license to utilize collected biological material for "therapeutic development, including but not limited to regenerative cultivation." Regenerative cultivation. That is the phrase they used to give themselves permission to grow a copy of me.

I found out eighteen months later, when a billing error routed a monthly maintenance invoice to my residential address instead of to the client account it was intended for. The invoice was from Elysian Health Partners — a Lazarus subsidiary — for "Continuation Suite 14, Unit BRU-7741, Maintenance Cycle 14." The amount was 14,200 Φ. I did not have a continuation suite. I did not know what a BRU was. I called the number on the invoice and, after being transferred four times and placed on hold for forty minutes, was told that the invoice had been sent in error and that I should disregard it. Something about the way they said "disregard" made me not disregard it.

It took three months, a private investigator, and 80,000 Φ in legal fees to confirm what the invoice implied. Lazarus had used my tissue sample to cultivate a full Biological Reserve Unit — a brain-dead clone of me — which was being maintained in a private facility as an organ reserve for a Tier 5 client whose tissue profile matched mine. My body. My DNA. My face on a body I never consented to create, breathing in a room I had never seen, maintained for a person I had never met, using genetic material that was taken from me under the pretense of a routine medical exam. The clone had been active for fourteen months. It was physiologically twenty-four years old — two years younger than me. I was told it looked "healthy."

The legal proceedings lasted five months and never reached a courtroom. Lazarus's legal team argued that the consent form I signed constituted valid authorization for clone cultivation, that the BRU was their intellectual property rather than a derivative of my person, and that I had no standing to claim ownership of or rights over a "cultivated biological asset." My legal team argued that a reasonable person would not interpret "related medical applications" as permission to grow a full human clone. The judge — a corporate arbitrator appointed under GLMZ charter law — encouraged settlement. The settlement was substantial enough that I will never need to work again. It included the termination of BRU-7741. It included a non-disclosure agreement that I am now violating. It did not include an apology. It did not include an admission of wrongdoing. It did not include any guarantee that Lazarus would not do the same thing to someone else, because of course they will. They already have. The consent form has not been revised. The fine print has not changed. If you have visited a Lazarus-affiliated medical facility in the last ten years, I suggest you read your paperwork. Then I suggest you be afraid.`,
    related_entities: ["Lazarus Pharmaceuticals", "Elysian Health Partners", "GLMZ"],
    story_hooks: [
      "How many other people have been cloned without consent through the same fine-print loophole — and how many never found out?",
      "BRU-7741 was supposedly terminated as part of the settlement — but was it really, or was it simply reassigned?"
    ],
    tags: ["document", "clone", "lazarus", "consent", "personal_account", "leaked", "legal", "tissue_rights", "elysian_health_partners", "ethics", "meridian_88"]
  },
  {
    name: "The Transfer",
    document_type: "clinical_report",
    author: "Dr. Emeka Azikiwe (name changed), Board-Certified Anesthesiologist",
    date: "2287-12-04",
    classification: "classified",
    credibility: "leaked",
    description: `I have performed anesthesia for approximately 6,400 procedures over a seventeen-year career. Cardiac cases, neuro cases, transplants, traumas. I have watched patients die on the table — not many, but enough. I have never experienced anything like Procedure 2287-TC-003. I am writing this account because I need it to exist somewhere that is not my own memory, because my memory of this procedure has made it impossible for me to sleep, and I have begun to worry that if I do not put it on paper, I will convince myself it didn't happen. It happened. I was there. I am the one who counted backward from ten.

The patient — I will call him Mr. L — was a Tier 5 male, chronological age 73, presenting with advanced cardiac degeneration, early-stage renal failure, and a neurological profile consistent with a six- to eighteen-month terminal trajectory. His companion body — his clone — was a physiologically 29-year-old male in optimal health, maintained in a dedicated suite at a Lazarus facility for eleven years. The procedure was consciousness transfer, classified internally as "Therapeutic Continuity Protocol." I was briefed by a Lazarus neurosurgical team that I had never met and would never see again. The briefing lasted forty minutes. I was told the procedure had been performed twice before, successfully. I was not given details of the previous procedures. I was told that my role was to manage anesthesia for both bodies simultaneously. Both bodies. One patient, two bodies.

The procedure was conducted in a dual-theater configuration — Mr. L's original body in Theater A, the clone body in Theater B, connected by a neural-bridge apparatus that I am not qualified to describe in technical detail and that I was told operates on principles that are "proprietary." Both bodies were prepped simultaneously. I administered general anesthesia to Mr. L's original body at 06:14. The clone body was already in a chemically maintained vegetative state; my task was to titrate its sedation downward in coordination with the neural bridge activation. At 06:47, the neurosurgical team initiated what they called "the cascade." I was told to watch Mr. L's vitals. I watched them.

At 07:12, Mr. L's original body went into cardiac arrest. This was, I was told, expected — "the original substrate decompensates as neural continuity transfers." I was instructed not to resuscitate. I stood beside a dying man and did nothing, because I had been told that the man was not dying but relocating. His heart stopped at 07:14. His brain activity ceased at 07:16. In Theater B, at 07:15 — one minute after cardiac arrest in the original body — the clone body exhibited the first spontaneous neural activity ever recorded in its existence. EEG readings spiked from flat baseline to patterns consistent with deep REM sleep, then rapid transition through light sleep stages to wakefulness indicators. At 07:23, the clone body opened its eyes. At 07:24, it began to breathe independently. At 07:31, it spoke. It said: "Oh God."

Mr. L — the person who had been Mr. L, who was now in a body thirty years younger than the one cooling in Theater A — cried for three hours. The neurosurgical team documented this as "standard emotional dysregulation consistent with neural reintegration." I sat with him because nobody else would. When he could speak coherently, I asked him what he was feeling. He said: "I remember dying. I felt my heart stop. I felt everything go dark. And then I was here, and everything was bright and wrong and I could feel my own heartbeat and it wasn't my heartbeat, it was someone else's heartbeat, except it's mine now." He looked at his hands — young hands, smooth, unlined — and he said, "These aren't my hands." Then he said, "They are now." Then he cried again. The procedure was, by every clinical metric, a success. Mr. L is alive. Mr. L is twenty-nine years old. Mr. L's original body was cremated that evening. I signed the anesthesia report. I filed my documentation. I went home. I have not slept through the night since. I keep thinking about the moment between 07:14 and 07:15 — the sixty seconds when Mr. L's heart had stopped but the clone's brain had not yet activated. For sixty seconds, Mr. L was nowhere. He was not in either body. He was dead. And then he wasn't. I do not know what that means. I do not know if anyone does.`,
    related_entities: ["Lazarus Pharmaceuticals"],
    story_hooks: [
      "The sixty-second gap between death and reactivation — was there truly continuity of consciousness, or did the original Mr. L die and a new person wake up with his memories?",
      "The anesthesiologist who can't sleep — what happens when he decides he can't stay silent?"
    ],
    tags: ["document", "clone", "lazarus", "consciousness_transfer", "clinical_report", "classified", "identity", "ethics", "anesthesiology", "meridian_88"]
  },
  {
    name: "What If They Dream?",
    document_type: "leaked_memo",
    author: "Dr. Sable Osei-Mensah, Lazarus Pharmaceuticals Sleep Research Division (notes reconstructed from paper originals)",
    date: "2286-05-19",
    classification: "classified",
    credibility: "suppressed",
    description: `These notes were smuggled out of Lazarus Pharmaceuticals' GLMZ neurological research campus on paper — handwritten, in a personal shorthand that took three months to decode — because paper cannot be remotely wiped. Every digital record of the study described below has been purged from Lazarus's systems. The researcher who conducted the study, Dr. Sable Osei-Mensah, was reassigned to an off-site facility two weeks after submitting her findings. Her current location is unknown. Her employee file lists her status as "on extended leave." Her apartment has been vacated. What follows is a reconstruction of her work, as faithful to her original notes as the shorthand allows.

The study was initiated in 2285 as a routine monitoring protocol — Lazarus periodically assesses neural baseline activity in maintained clone bodies to calibrate chemical suppression dosages. Dr. Osei-Mensah was assigned a cohort of 240 Biological Reserve Units across two facilities for standard EEG monitoring over a 90-day period. The monitoring was expected to confirm flat neural baselines with minor autonomic variation. That is not what it confirmed. Within the first two weeks of continuous monitoring, Dr. Osei-Mensah identified anomalous neural activity patterns in 19 of the 240 subjects — approximately 8 percent. The patterns were unmistakable: cyclical, organized, and bearing the precise electrophysiological signature of REM sleep. The brain-dead clones, maintained on neural suppressants designed to prevent any cognitive activity, were dreaming.

The implications were immediately apparent to Dr. Osei-Mensah and apparently to her supervisors, because her request to expand the study was denied within hours of submission. REM sleep is not a simple neural event. It is associated with memory consolidation, emotional processing, and — most critically — subjective conscious experience. The scientific consensus is unambiguous: REM activity implies some form of internal phenomenological state. Something is being experienced. The clones have no memories to consolidate, no emotional experiences to process, no sensory input to integrate. And yet the REM patterns in the 19 affected subjects were robust, sustained, and recurring on cycles consistent with natural sleep architecture. Whatever the clones were experiencing, it was organized. It was patterned. It was, by every neurological measure, dreaming.

Dr. Osei-Mensah's notes become increasingly agitated over the following weeks. She records attempts to correlate REM activity with subject characteristics — age, time in maintenance, facility location, suppression dosage — and finds only one statistically significant variable: duration of maintenance. Clones maintained for longer periods were more likely to exhibit REM activity. The longest-maintained subject in the cohort, a unit that had been in continuous vegetative maintenance for nine years, showed REM activity during 23% of monitored sleep cycles — a rate higher than the human average. The clone's brain, never activated, never exposed to language or light or touch, had apparently been developing its own internal activity over nearly a decade of silence. What it was dreaming about is unknowable. That it was dreaming at all should have been the most significant neurological finding in a generation. Instead, it was classified.

The final entries in Dr. Osei-Mensah's notes are the most haunting. She describes returning to the facility alone, after hours, to observe the REM-active subjects. She watched their faces. She recorded micro-expressions — tiny movements of facial muscles that, in conscious individuals, correlate with emotional states. She logged what she interpreted as distress responses in three subjects, and what she could only describe as "a smile" in one — a brief, involuntary contraction of the zygomatic major muscles lasting approximately two seconds, observed at 03:17 AM in a clone that had been maintained for seven years. "I do not know what she is dreaming," Dr. Osei-Mensah wrote. "I do not know if 'dreaming' is even the right word for what is happening in a brain that has never been awake. But I watched her face change, and for two seconds she looked like a person who was remembering something good. She has never experienced anything good. She has never experienced anything at all. And yet." The note ends there. The next page is a list of personal items Dr. Osei-Mensah removed from her office. The page after that is blank. Paper cannot be remotely wiped. But people, it seems, can be.`,
    related_entities: ["Lazarus Pharmaceuticals", "Lazarus Neurological Research Division"],
    story_hooks: [
      "Dr. Osei-Mensah's disappearance after submitting her findings — is she in hiding, or has Lazarus ensured her permanent silence?",
      "The nine-year clone dreaming at rates exceeding human averages — what kind of consciousness develops in a brain that has never been awake?"
    ],
    tags: ["document", "clone", "lazarus", "consciousness", "dreaming", "REM", "classified", "suppressed", "neurology", "ethics", "sleeper", "meridian_88"]
  }
];

// Generate and write files
if (!fs.existsSync(outputDir)) {
  fs.mkdirSync(outputDir, { recursive: true });
}

let count = 0;
for (const doc of documents) {
  const id = generateId();
  const entity = {
    id,
    name: doc.name,
    type: "document",
    document_type: doc.document_type,
    author: doc.author,
    date: doc.date,
    classification: doc.classification,
    description: doc.description,
    related_entities: doc.related_entities,
    credibility: doc.credibility,
    story_hooks: doc.story_hooks,
    tags: doc.tags
  };

  const filePath = path.join(outputDir, `${id}.json`);
  if (fs.existsSync(filePath)) {
    console.log(`SKIP (exists): ${filePath}`);
    continue;
  }
  fs.writeFileSync(filePath, JSON.stringify(entity, null, 2), 'utf-8');
  console.log(`WROTE: ${doc.name} -> ${id}.json`);
  count++;
}

console.log(`\nDone. ${count} files written to ${outputDir}`);
