const crypto = require('crypto');
const fs = require('fs');
const path = require('path');

const outDir = path.resolve(__dirname, '..', 'engine', 'data', 'documents');

function genId() {
  return crypto.randomBytes(16).toString('hex');
}

const documents = [
  {
    name: "The Census Discrepancy — GLMZ Population Analysis, 2226",
    document_type: "census_report",
    author: "GLMZ Bureau of Census and Population Analytics",
    date: "2226-03-14",
    classification: "restricted",
    description: `The 2226 annual population survey for GLMZ has identified a statistically significant discrepancy between projected and observed child populations. The under-12 demographic exceeds actuarial projections by 14.3%, representing approximately 31,000 more children than birth rate trends, immigration records, and mortality tables can account for. This deviation has been growing steadily since at least 2218, when it first exceeded the 5% threshold that triggers formal review. The Bureau has classified this finding as a Category 3 Demographic Anomaly, requiring interdepartmental investigation.

Birth registrations account for only 82% of children identified through integrated surveillance and education enrollment systems. The remaining 18% — roughly 12,400 individuals — have no corresponding birth records in any GLMZ medical facility, no hospital admission records for delivery, and no BCI installation dates logged in the neural infrastructure registry. Standard identity verification through biometric and BCI cross-referencing confirms these children exist as physical persons with valid biological signatures, but their origin documentation is entirely absent.

The Bureau has conducted three independent audits of its data collection methodology and found no systemic errors. Surveillance identification algorithms were validated against a control population of registered children and demonstrated 99.7% accuracy. The discrepancy is not an artifact of miscounting. The children are real. They appear in school attendance records, medical clinic visits, food distribution queues, and residential surveillance feeds. They participate in the social infrastructure of the city. They simply have no documented origin.

Attempts to trace these children backward through historical surveillance data have produced inconsistent results. In 73% of cases, the earliest surveillance identification occurs within Shelf districts, where camera coverage is sparse and intermittent. The children appear in the data as if they have always been there — integrated into households and community structures with no discernible arrival event. The Bureau recommends expanded investigation but notes that current resource allocation does not support the scope of inquiry this anomaly demands.

This report is classified as restricted pending review by the GLMZ Governance Council. The Bureau advises against public disclosure until the discrepancy can be attributed to a methodological cause, as premature publication may generate unproductive speculation.`,
    related_entities: ["GLMZ", "Bureau of Census and Population Analytics", "GLMZ Governance Council"],
    credibility: "verified",
    story_hooks: [
      "Who or what is generating children that bypass all registration systems?",
      "Why has the anomaly been growing since 2218 — what changed that year?"
    ],
    tags: ["document", "children_paradox", "census", "anomaly", "meridian_88", "population", "demographics", "surveillance"]
  },
  {
    name: "Where Are the Children Coming From?",
    document_type: "investigation",
    author: "Maren Okafor-Lindqvist, The Meridian Independent",
    date: "2226-06-22",
    classification: "public",
    description: `I started this investigation because of a number. Fourteen point three percent. That's how much the under-12 population of GLMZ exceeds what the birth rate says it should be. I've spent four months talking to the people who count children for a living — census workers, school administrators, Shelf community organizers, clinic nurses — and every single one of them told me the same thing before I even asked the question. "There are more kids than there should be." They know. They've known for years. They just don't have anywhere to report it that would make a difference.

Deshi Okonkwo has been a census enumerator in the Shelf's Ward 7 for eleven years. She walks the blocks, counts the faces, logs the names. "Every year, there are new ones," she told me. "Not babies — children. Four, five, six years old. They show up in apartments, in community shelters, in the back rooms of shops. The families say they're cousins, or they took them in from a friend who moved away, or they just shrug." Okonkwo's count for Ward 7 has exceeded the official projection every year since 2220. She's filed discrepancy reports. Nothing has come of them.

School administrator Priya Vasquez-Chen at Shelf District 4 Primary told me she's enrolled 340 students this year in a facility designed for 280. "Forty-three of them have no transfer records from another school and no birth documentation on file. We don't turn children away. We don't have that luxury and we don't have that cruelty." She showed me enrollment forms filled out with single names, no family surnames, no addresses. Under "previous school" — blank. Under "guardian" — names she's never been able to verify against any directory.

Community organizer Tomoko Adeyemi-Ruiz has been tracking the phenomenon in her own way — hand-drawn maps of her block, updated monthly, with colored pins for every child she can identify. She pulled out the map from January and the map from June. Twenty-three new pins. "Some of them, I saw them arrive. A kid sitting on a stoop one Tuesday morning who wasn't there Monday. Some of them, I can't even tell you when they appeared. They were just suddenly part of the landscape. Neighbors say they've always been here. But my maps don't lie."

I asked every person I interviewed the same final question: Why doesn't anyone investigate? The answers were variations on a theme. "You don't investigate children. You feed them." "Nobody wants to be the person who gets a kid deported." "What are you going to do, arrest a six-year-old for not having a birth certificate?" The children exist in a moral blind spot — their presence is irregular, but their needs are immediate, and the systems that should track them are the same systems that can't afford to lose them.`,
    related_entities: ["GLMZ", "The Meridian Independent", "Shelf"],
    credibility: "verified",
    story_hooks: [
      "Tomoko's hand-drawn maps could be a critical piece of evidence if someone connects arrival patterns to another data set",
      "The 43 students with no records at a single school suggest a pipeline, not random drift"
    ],
    tags: ["document", "children_paradox", "investigation", "journalism", "census", "shelf", "anomaly", "community"]
  },
  {
    name: "The Migration Theory",
    document_type: "academic_paper",
    author: "Dr. Alejandro Mwangi-Petrov, GLMZ Institute for Urban Demographics",
    date: "2225-11-08",
    classification: "public",
    description: `This paper proposes that the observed surplus in GLMZ's child population is attributable to unregistered migration from neighboring city-states, with children traveling through interstitial territories — including Behemoth-active zones — either alone or in small, self-organized groups. The hypothesis is grounded in the well-documented phenomenon of child migration in resource-scarce urban environments, adapted to the specific geographic and logistical conditions of the Great Lakes Megalopolis Zone.

We mapped the earliest surveillance identification points for 847 unregistered children against known transit corridors, rail infrastructure, and Behemoth patrol routes. The correlation coefficient between arrival clusters and active corridor endpoints is 0.67, which we characterize as suggestive. Children appear most frequently in districts adjacent to the southern and eastern transit approaches — consistent with arrivals from the direction of the Indiana Reclamation Zone and the Detroit Autonomous Collective. Arrival timing shows loose correlation with seasonal patterns in Behemoth migratory behavior, suggesting children may be timing their travel to windows of reduced autonomous machine activity.

However, several features of the data resist this explanation. Approximately 19% of arrival clusters are concentrated in the northern Shelf districts, oriented toward Lake Michigan and the Wisconsin lakeshore — a direction from which no significant population center exists within 200 kilometers. The nearest settlement of any size is the Sturgeon Bay outpost, population approximately 400, which has not reported any child emigration. Children arriving from this direction would have to traverse open lakeshore territory with no infrastructure, no shelter, and significant Behemoth presence. Survival would require resources and knowledge that six-year-olds do not typically possess.

Additionally, interviews with children who were willing to speak about their origins produced contradictory accounts. Some described other cities in terms consistent with known settlements. Others described places that don't correspond to any mapped location. Three children described the same place — a "white building where it was always quiet" — but placed it in three different directions from GLMZ. Whether these are genuine memories, confabulations, or something else entirely remains undetermined.

We present this hypothesis as the most parsimonious explanation for the majority of the demographic surplus, while acknowledging that it cannot account for the full scope of the anomaly. Supplementary explanations are almost certainly required. We recommend cross-referencing our arrival data with census discrepancies reported by other GLMZ city-states.`,
    related_entities: ["GLMZ", "GLMZ", "Indiana Reclamation Zone", "Detroit Autonomous Collective", "Iowan Behemoths"],
    credibility: "disputed",
    story_hooks: [
      "The 'white building where it was always quiet' described by three children could be a Lazarus facility",
      "Children surviving Behemoth-active zones alone implies either extraordinary luck or deliberate protection"
    ],
    tags: ["document", "children_paradox", "academic_paper", "migration", "transit", "behemoth", "glmz", "demographics"]
  },
  {
    name: "The Clone Hypothesis",
    document_type: "leaked_memo",
    author: "Dr. Yuki Abara-Singh, Lazarus Pharmaceuticals (Internal)",
    date: "2219-08-30",
    classification: "leaked",
    description: `INTERNAL MEMORANDUM — LAZARUS PHARMACEUTICALS — BIOETHICS REVIEW BOARD — CLASSIFICATION: SIGMA-7

RE: Feasibility Assessment — Accelerated Growth Cloning for Therapeutic Organ Supply

Per the Board's request, this memo summarizes the current state of the accelerated growth cloning program (codename GARDEN) as of Q3 2219. The program has successfully produced 1,247 viable tissue-matched clone bodies at developmental stages ranging from neonatal to approximately 12 years biological age. Growth acceleration protocols have been refined to achieve a 12-year biological age in approximately 14 months of tank gestation, with a viability rate of 89%. Organ harvest success rates remain above 94% for cardiac, hepatic, and renal tissues.

The Board has asked me to address the accountability gap identified in the Q2 audit. Of the 1,247 viable bodies produced since program inception, 1,203 have been processed through standard organ harvest protocols. The remaining 44 bodies — 3.5% of total production — are classified as "disposition unknown." These units were logged as viable at their final developmental checkpoint but do not appear in harvest records, disposal records, or any downstream processing documentation. They are simply absent from the system.

I want to be transparent about what this means. Forty-four clone bodies, biologically aged between 4 and 12, with full neural development, functional autonomic systems, and no BCI implantation, are unaccounted for. If these bodies achieved consciousness during the growth process — which our neural monitoring data suggests is possible after month 9 of accelerated gestation — they would be indistinguishable from naturally born children of the same apparent age, with the exception of having no birth records, no medical history, and no identity documentation. Their growth plate structures would show subtle anomalies consistent with accelerated early development, but this would only be detectable through targeted radiological examination.

The mathematical implications are straightforward. If the GARDEN program has been operating at similar scale across multiple Lazarus facilities — and I have no visibility into operations outside this site — and if the disposition gap is consistent, then the total number of unaccounted bodies could range from several hundred to several thousand, depending on the number of active sites. This figure is consistent with the scale of the demographic anomaly reported in recent GLMZ census data.

This memo constitutes my formal recommendation that the Board conduct a full audit of all GARDEN program sites and reconcile the disposition records. I am not alleging that clone bodies have been deliberately released or have escaped. I am stating that we cannot prove they haven't. The distinction matters. — Dr. Yuki Abara-Singh, Senior Bioethicist, Lazarus Pharmaceuticals. [MEMO AUTHENTICATED BY THREE INDEPENDENT CRYPTOGRAPHIC ANALYSTS. LAZARUS PHARMACEUTICALS HAS ISSUED A STATEMENT DENYING THE EXISTENCE OF ANY PROGRAM MATCHING THIS DESCRIPTION.]`,
    related_entities: ["Lazarus Pharmaceuticals", "GARDEN Program", "GLMZ"],
    credibility: "disputed",
    story_hooks: [
      "44 unaccounted clone bodies from a single facility — how many facilities are there?",
      "The memo author Dr. Abara-Singh could be located and interviewed, if she's still alive"
    ],
    tags: ["document", "children_paradox", "lazarus", "clone", "leaked_memo", "bioethics", "organ_harvesting", "garden_program", "classified"]
  },
  {
    name: "The Wise Children",
    document_type: "personal_account",
    author: "Nadira Johansson-Osei, Shelf District 9 Primary Educator",
    date: "2226-01-15",
    classification: "public",
    description: `I've been teaching primary school in the Shelf for fourteen years. I know what children are like. I know how they learn, how they process information, how they build understanding piece by piece over years of patient instruction and chaotic play. I know the difference between a bright child and an unusual one. What I'm about to describe is neither. These are children who know things they have no mechanism for knowing, and who behave in ways that are not accelerated development but displaced competence — as if the knowledge was already there and the child is simply remembering it.

The first one I noticed was a boy named Eko, age seven, no registration records, taken in by a family in Block 14. He was quiet and well-behaved in a way that wasn't shyness — it was patience. He waited for other children to finish speaking before he responded. He used conditional phrasing — "if that's the case, then" — that I don't hear from most adults. When I taught a basic lesson on how GLMZ's water reclamation system works, he raised his hand and asked why the tertiary filtration stage used reverse osmosis instead of forward osmosis, "since the energy cost is lower and the membrane fouling rate is comparable." He was seven.

Then there was Lian, age eight, who corrected my explanation of Φ exchange rates during a civics lesson. Not in the way a child repeats something they overheard — she walked me through the arbitrage mechanism between Meridian Φ and Detroit Collective scrip with the fluency of someone who had executed the trades herself. When I asked her where she learned about currency exchange, she paused for a long time and said, "Before." I asked before what. She said, "Before I was small." I wrote it off as imagination. I shouldn't have.

There are others. A nine-year-old who can read corporate contract language and identify the liability-shifting clauses. A six-year-old who drew a schematic of a BCI neural interface from memory — or from something — that my engineer friend said was "terrifyingly accurate." A ten-year-old who mediates disputes between other children with the practiced calm of a professional arbitrator. None of these children have records. All of them were taken in by Shelf families who found them or were found by them.

I want to be clear about what I'm not saying. I'm not saying these children aren't children. They play. They laugh. They cry when they scrape their knees. They are children in every way that matters for how I treat them. But something is different about the architecture of their understanding. They don't learn the way children learn — building up from nothing. They learn the way someone re-learns — recovering what was already there. I don't know what that means. I don't think I want to.`,
    related_entities: ["GLMZ", "Shelf"],
    credibility: "unconfirmed",
    story_hooks: [
      "Lian's phrase 'before I was small' implies a previous existence in a larger body",
      "The six-year-old's BCI schematic could be matched against known Lazarus designs"
    ],
    tags: ["document", "children_paradox", "personal_account", "anomaly", "consciousness", "education", "shelf", "wise_children"]
  },
  {
    name: "Neural Transplant Ethics and the Body Problem",
    document_type: "academic_paper",
    author: "Dr. Katarina Nwosu-Bergmann, GLMZ University, Department of Applied Neuroethics",
    date: "2221-04-17",
    classification: "suppressed",
    description: `This paper examines the theoretical ethical framework surrounding the transplantation of adult human consciousness into cloned biological bodies, with particular attention to cases in which the recipient body is developmentally juvenile. While no institution has publicly acknowledged the technical capability to perform such a procedure, the convergence of three existing technologies — accelerated-growth cloning, high-fidelity neural state mapping via BCI architecture, and targeted neural pattern imprinting — suggests that the procedure is within the current technological horizon, if not already achievable.

The central ethical problem is one of identity and consent. If an adult consciousness is transferred into a cloned child body, the resulting entity occupies an unprecedented legal and moral category. The body is biologically a child — subject to protections, developmental needs, and physical limitations appropriate to its apparent age. The consciousness, however, retains adult memories, preferences, and cognitive frameworks. The entity cannot meaningfully consent to its own situation in any conventional sense, because the child body's neurochemistry will inevitably modify the transplanted consciousness over time, creating a hybrid identity that is neither the original adult nor a natural child.

The second ethical axis concerns the cloned body itself. If accelerated-growth cloning produces bodies with emergent consciousness — and current neural monitoring data suggests this is likely after approximately nine months of gestation — then the transplant procedure necessarily involves the destruction or displacement of an existing nascent consciousness to make room for the transplanted one. This constitutes, by any reasonable ethical framework, the destruction of a person for the benefit of another person. The fact that the destroyed person is an artificially created consciousness does not diminish the ethical weight of the act.

Third, we must consider the social implications. A population of entities who appear to be children but possess adult knowledge and motivations would be functionally invisible within existing social structures. They would pass through educational and child welfare systems unchallenged. They would have access to resources and protections designed for genuinely vulnerable populations. And they would carry with them the accumulated social capital, strategic knowledge, and — potentially — the wealth of their previous lives. The inequality implications are staggering.

RETRACTION NOTICE [2221-09-03]: This paper has been retracted by the author. The retraction letter, filed with the journal, reads in its entirety: "I have been asked to consider the implications of publication. I have considered them. I retract the paper." Dr. Nwosu-Bergmann has declined all subsequent interview requests and has not published further work in any field.`,
    related_entities: ["GLMZ University", "Lazarus Pharmaceuticals"],
    credibility: "suppressed",
    story_hooks: [
      "Who asked Dr. Nwosu-Bergmann to 'consider the implications' — and what were those implications?",
      "The paper's theoretical framework maps precisely onto what the Wise Children accounts describe"
    ],
    tags: ["document", "children_paradox", "academic_paper", "consciousness", "clone", "neural_transplant", "ethics", "suppressed", "retracted"]
  },
  {
    name: "The Shelter Census",
    document_type: "personal_account",
    author: "Amara Delgado-Kimathi, Shelf Ward 12 Community Organizer",
    date: "2226-02-28",
    classification: "public",
    description: `I don't trust databases. I trust my feet and my notebook. Every month for the past three years, I've walked every block of Ward 12 and counted every child I could find. I knock on doors. I talk to families. I sit in the community kitchens and watch who comes to eat. I cross-reference my count against the official census numbers published by the Bureau. And every month, my count is higher. Not by a little. By a lot.

As of February 2226, the Bureau's official count for Ward 12 lists 614 children under the age of 12. My count is 637. That's 23 children who exist in my notebook but not in any official record. I have names for all of them — or at least the names they go by. I have approximate ages. I have the addresses where they sleep. I have the names of the adults who feed them. What I don't have, for seven of them, is any identifiable parent or guardian who claims biological or legal relationship.

These seven children live with families who took them in. The families didn't adopt them through any formal process — there is no formal process in the Shelf for most things. They simply absorbed them. When I asked how each child came to be in their home, the answers were remarkably similar. "She was sitting on the steps one morning." "He was in the alley behind the kitchen, just waiting." "The neighbor's kid brought her home and said she didn't have anywhere to go." One woman, Fatima Olsson-Chandra, told me: "I opened my door on a Tuesday and he was standing there. Not crying, not scared. Just standing. Like he was waiting for me to open the door. He's been mine since."

I've tried to trace these children backward. I've talked to every block captain, every kitchen volunteer, every night watch in Ward 12. Nobody saw them arrive. Nobody remembers a time before they were here, even though my own records — my own monthly counts — show exactly when each one appeared in my data. Three appeared in a single month: August 2225. Two more in October. The last two in December. They didn't trickle in. They arrived in clusters.

I don't have a theory. I have data. Twenty-three children who shouldn't exist by the Bureau's count. Seven who have no identifiable origin. Arrival patterns that suggest coordination rather than chance. And a community that has folded them in without question, because that's what the Shelf does — it takes care of what shows up at its door. I'm publishing this count because someone should be paying attention, and the institutions that should be paying attention are not.`,
    related_entities: ["GLMZ", "Shelf", "Bureau of Census and Population Analytics"],
    credibility: "verified",
    story_hooks: [
      "The August 2025 cluster of three arrivals could correlate with a specific event at a Lazarus facility",
      "Fatima's description of the child 'waiting for her to open the door' suggests intentional placement"
    ],
    tags: ["document", "children_paradox", "census", "personal_account", "shelf", "community", "hand_count", "anomaly"]
  },
  {
    name: "Are They Really Children?",
    document_type: "investigation",
    author: "Dr. Suki Adebayo-Reyes, Child and Adolescent Psychologist",
    date: "2226-04-10",
    classification: "restricted",
    description: `I have been practicing child psychology in GLMZ for nineteen years. In the past three years, I have been referred eleven children who were flagged by educators or community workers as displaying atypical cognitive and behavioral profiles. All eleven are unregistered — no birth records, no BCI installation dates, no medical histories. Nine of them present as entirely normal children with normal developmental trajectories. Two of them do not. This essay concerns those two.

Patient A is a nine-year-old boy, referred by his school after he was observed using vocabulary and syntactic structures far beyond his developmental stage. In our sessions, he presents as calm, measured, and cooperative — not in the way a well-behaved child is cooperative, but in the way a professional is cooperative with a process they understand and have decided to participate in. His emotional regulation is extraordinary. Not suppressed — regulated. He experiences frustration, joy, sadness — but he manages these emotions with a sophistication I associate with adults who have undergone years of therapeutic work. His sentence construction, word choice, and conceptual reasoning are consistent with a person approximately 45 years of age with professional-level education.

His body is nine years old. I ordered full medical workups to rule out any physiological explanation. BCI scan: no implant present, which is itself anomalous — virtually all children in GLMZ receive BCI installation by age three. Blood work: normal for a nine-year-old male. Dental examination: dentition consistent with approximately nine years of development. Bone density scan: normal, with minor anomalies in growth plate structure that the radiologist noted as "unusual but not pathological." Genetic screening: no known markers for enhanced cognitive development. By every measurable biological criterion, he is a nine-year-old boy.

During our fourth session, I asked him to tell me about his earliest memory. He was quiet for a long time. Then he said, "Which set?" I asked what he meant. He said, "I have memories from before that don't match. They're in a different place. A different body. They feel older." He described a room with white walls, a desk, a window overlooking water. He described sitting at the desk and reading a report about pharmaceutical supply chains. He described the feeling of adult hands — his hands, he said — holding a pen. Then he stopped and said, "I shouldn't talk about this. It makes people uncomfortable." He was right.

I am not drawing conclusions. I am reporting observations. Patient A is biologically a nine-year-old child. Psychologically, he is something else — or something additional. I have no framework for what I am observing. I have no diagnosis that fits. I have a nine-year-old boy who remembers being a man, and whose memories include details that are too specific and too consistent to be confabulation. I have filed this report with the GLMZ Medical Ethics Board. I have not received a response.`,
    related_entities: ["GLMZ", "GLMZ Medical Ethics Board"],
    credibility: "verified",
    story_hooks: [
      "Patient A's memory of pharmaceutical supply chain reports may connect him to a specific corporate identity",
      "The absence of BCI in all eleven children suggests they were never processed through standard GLMZ systems"
    ],
    tags: ["document", "children_paradox", "investigation", "psychology", "consciousness", "anomaly", "identity", "wise_children", "medical"]
  },
  {
    name: "The Second Life Program — Rumor or Reality?",
    document_type: "investigation",
    author: "Anonymous",
    date: "2225-09-01",
    classification: "leaked",
    description: `This document has been circulating on Shelf networks since mid-2225. Its author is unknown. Its claims are unverified. Its technical specificity is what makes it impossible to dismiss.

The document alleges the existence of a program — referred to as "Second Life" — operated by or through Lazarus Pharmaceuticals, in which wealthy Tier 5 individuals purchase cloned child bodies and undergo a consciousness transfer procedure, effectively beginning life again in a new body. The program allegedly costs between 8 and 15 million Φ, depending on the specificity of the body requirements and the complexity of the transfer. The document claims that the procedure has been available since approximately 2218 and that between 200 and 500 individuals have undergone it.

The technical description of the transfer process is what has drawn the most attention from those who have read the document. It describes a three-stage procedure: first, the adult subject undergoes a full neural state capture through a modified BCI operating at what the document calls "deep-map resolution" — a level of scanning detail that current public BCI specifications do not support but that is theoretically achievable with hardware modifications described in the document. Second, the captured neural state is processed through a "translation matrix" that adapts adult cognitive architecture to the developing neural substrate of the child body, preserving memories and personality while allowing the biological brain's developmental processes to proceed normally. Third, the translated state is imprinted onto the clone body's brain, overwriting whatever nascent consciousness existed.

The document names no sources, provides no evidence, and identifies no specific individuals who have undergone the procedure. What it does provide is a level of technical detail that several independent neuroscientists — consulted anonymously — have described as "plausible," "disturbingly well-informed," and "not something you could write without access to research that hasn't been published." One neuroscientist noted that the translation matrix concept addresses a specific technical problem in neural state transfer that has only been discussed in classified research circles.

The document concludes with a question: "If you were dying and you were rich, wouldn't you?" It's a question that doesn't require an answer because everyone already knows what it is. The Shelf networks where this document circulates have added their own commentary, mostly variations of the same observation: the rich have always consumed the young. This would just be the most literal version.`,
    related_entities: ["Lazarus Pharmaceuticals", "Second Life Program", "Shelf"],
    credibility: "unconfirmed",
    story_hooks: [
      "The 8-15 million Φ price point could be cross-referenced with unusual financial transactions in Tier 5",
      "The 'translation matrix' concept could be validated or debunked by someone with access to classified neural research"
    ],
    tags: ["document", "children_paradox", "lazarus", "consciousness", "clone", "second_life", "immortality", "leaked", "tier_5", "quanta"]
  },
  {
    name: "Why Nobody Investigates",
    document_type: "opinion_piece",
    author: "Kai Nakamura-Obi, The Meridian Independent",
    date: "2226-05-30",
    classification: "public",
    description: `The Children Paradox is the most open secret in GLMZ. Everyone who works with population data knows about it. Everyone who works with children in the Shelf knows about it. The Bureau of Census published a restricted report documenting a 14.3% surplus in the under-12 population. Investigative journalists have written about it. Academics have theorized about it. And yet no institution — not the Bureau, not CorpSec, not the Governance Council, not any CorpoNation — has launched a formal investigation. The question is not why the anomaly exists. The question is why nobody in a position of authority wants to know why it exists.

The answer is that the Children Paradox is an equilibrium. Every major institutional actor in GLMZ benefits from not knowing its cause. The CorpoNations benefit because more children mean more future consumers, more future workers, more future BCI subscribers. Population growth is good for the bottom line, and CorpoNations do not ask where growth comes from — they monetize it. If the surplus children are clones, or migrants, or something else entirely, the revenue they generate as adults will be identical regardless of origin.

CorpSec benefits because investigating the paradox would require acknowledging a massive gap in their surveillance and identity infrastructure — a gap that calls into question the fundamental premise of their authority. CorpSec's power derives from the claim that they know who everyone is and where everyone is at all times. Admitting that 12,000 children appeared without being detected would be an institutional humiliation. It is easier to not count them than to explain how they were missed.

The Shelf communities benefit because more children means more hands, more future capacity, more life in districts that have been slowly dying for decades. The Shelf doesn't ask where children come from because the Shelf runs on a different moral calculus — if a child is hungry, you feed it; if a child needs shelter, you provide it. Origin is irrelevant. Need is everything. Any investigation that resulted in children being removed or institutionalized would face violent community resistance, and every institution knows it.

The only people who do not benefit from this equilibrium are the children themselves — if they are, in fact, children. If even some of them are displaced consciousnesses in manufactured bodies, then they exist in a category for which no legal framework, no ethical guideline, and no social infrastructure has been designed. They are invisible not because no one can see them, but because no one has built the language to describe what they are. And until someone does, the count will keep rising, and the questions will keep going unasked, and the children will keep appearing in the data like ghosts that eat breakfast.`,
    related_entities: ["GLMZ", "CorpSec", "GLMZ Governance Council", "Shelf", "The Meridian Independent"],
    credibility: "verified",
    story_hooks: [
      "The 'equilibrium' framing suggests that any disruption to the paradox would destabilize multiple institutions simultaneously",
      "CorpSec's reluctance to investigate implies they may already know the answer"
    ],
    tags: ["document", "children_paradox", "opinion_piece", "corpsec", "CorpoNation", "shelf", "institutional", "surveillance", "cover_up"]
  },
  {
    name: "The Playground Recordings",
    document_type: "investigation",
    author: "Dr. Ren Achebe-Park, GLMZ University, Department of Sociolinguistics",
    date: "2226-03-05",
    classification: "restricted",
    description: `As part of a broader study on linguistic development in under-resourced communities, I placed passive audio recording equipment at three Shelf playgrounds in Districts 4, 9, and 12 over a period of six weeks in January and February 2226. The equipment was positioned to capture ambient conversation and was disclosed to community leaders per ethics board requirements. The purpose was to analyze vocabulary acquisition and code-switching patterns among Shelf children aged 5-12. What the recordings captured instead has fundamentally altered the direction of my research.

During daylight hours, the recordings contain exactly what you would expect: children playing, arguing, laughing, inventing games, using language consistent with their developmental age. Vocabulary is appropriate. Syntax is age-typical. The children sound like children. The data from daylight recordings supports my original hypotheses about linguistic development in multilingual Shelf communities, and under normal circumstances, this would be the entirety of my findings.

The equipment continued recording after dark. This was an oversight — the timers were set incorrectly, and the devices captured approximately six hours of nighttime audio per day. On fourteen separate nights across all three locations, the equipment recorded children's voices — identifiable by vocal register and pitch as belonging to individuals aged approximately 6-10 — engaged in extended conversation using language that is categorically inconsistent with any model of child language development I am aware of.

On one recording from District 9, two voices — estimated age 7-8 based on vocal characteristics — discuss the implications of a specific corporate merger between two GLMZ logistics firms, using correct legal terminology and referencing regulatory frameworks by their proper designations. On another, a voice estimated at age 6 describes the architecture of a BCI neural mesh in technical language consistent with an engineering graduate. On a third recording — the one that made me stop sleeping well — a child's voice, no older than nine or ten, describes in calm, detailed language the experience of dying. Not imagining death. Describing it. The physiological cascade. The narrowing of sensory input. The moment of cessation. Described in the past tense, as a personal experience.

I have not published this data. I have shared it with two colleagues under strict confidence. Both have urged me not to publish. I am publishing this preliminary account because the recordings are evidence of something, and I believe that something is important enough to risk the professional consequences of being associated with it.`,
    related_entities: ["GLMZ University", "Shelf"],
    credibility: "verified",
    story_hooks: [
      "The child who described dying in the past tense may be a consciousness transplant who remembers their previous death",
      "The corporate merger discussion could be traced to identify which specific firms were mentioned and who had insider knowledge"
    ],
    tags: ["document", "children_paradox", "investigation", "linguistics", "recordings", "anomaly", "consciousness", "wise_children", "shelf", "playground"]
  },
  {
    name: "A Child's Drawing",
    document_type: "personal_account",
    author: "Miriam Tanaka-Adeyemi, Licensed Child Therapist",
    date: "2226-01-30",
    classification: "restricted",
    description: `I use drawing as a standard therapeutic tool with children. It externalizes internal states, provides a non-verbal communication channel, and often reveals cognitive structures that verbal interviews miss. In twelve years of practice, I have interpreted thousands of children's drawings. I know what normal looks like. I know what trauma looks like. I know what imagination looks like. What seven-year-old Kofi drew does not fit any of these categories.

Kofi is an unregistered child living with a foster family in Shelf District 7. He was referred to me for behavioral assessment after his school reported that he was "unusually withdrawn and appeared to be grieving, though no loss event had been identified." In our second session, I asked him to draw "where you were before you lived with your family." This is a standard prompt for children with unknown histories — it often produces drawings of previous homes, shelters, or outdoor spaces that provide clues about their background.

Kofi drew a building. Not a child's approximation of a building — a rendering with consistent perspective, proportional accuracy, and architectural detail that I initially assumed was traced or copied. The building is rectangular, three stories, with a flat roof and a row of narrow windows on the second floor. The ground floor has no windows. There is a loading dock on the left side with a specific style of hydraulic platform that Kofi rendered in cross-section, showing the internal mechanism. The building is surrounded by a fence with angled tops — consistent with security fencing. In the upper right corner of the drawing, he included a logo: a stylized caduceus with a modification I didn't recognize.

I showed the drawing to a colleague who works in corporate facilities management. She identified the building immediately. It is a Lazarus Pharmaceuticals research and logistics complex located in the industrial district of Gary, approximately 40 kilometers southeast of GLMZ. The logo is a variant of the Lazarus corporate mark used on research facilities, distinct from their public-facing branding. Kofi has never been to Gary. His foster family has never been to Gary. There is no record of Kofi existing anywhere before he appeared in District 7 approximately fourteen months ago.

When I asked Kofi about the drawing, he said it was "where I woke up." I asked him what he meant. He said, "The first time. When I was new." He then asked if he could draw something else instead, and drew a picture of his foster family's cat. The cat drawing was consistent with a typical seven-year-old's motor skills and spatial reasoning. The building was not. I have secured the original drawing in my clinical files. I have not reported it to any authority because I do not know which authority to trust with it.`,
    related_entities: ["Lazarus Pharmaceuticals", "Gary", "Shelf", "GLMZ"],
    credibility: "verified",
    story_hooks: [
      "The Lazarus facility in Gary could be a GARDEN program site — physical investigation could confirm or deny",
      "Kofi's phrase 'when I was new' aligns with clone consciousness emergence described in the Abara-Singh memo"
    ],
    tags: ["document", "children_paradox", "personal_account", "drawing", "lazarus", "clone", "therapy", "gary", "anomaly", "consciousness"]
  },
  {
    name: "The Growth Acceleration Problem",
    document_type: "academic_paper",
    author: "Dr. Olumide Sato-Fernandez and Dr. Anika Mbeki-Johansson, GLMZ General Hospital, Department of Pediatric Radiology",
    date: "2225-06-14",
    classification: "suppressed",
    description: `This paper presents radiological findings from a cohort of 34 unregistered children who received medical care at GLMZ General Hospital between 2223 and 2225. As part of standard pediatric assessment for children with no medical history, skeletal surveys including growth plate imaging were performed. In 34 of 89 unregistered children examined during this period — 38.2% — we observed a distinctive pattern of growth plate morphology that is not consistent with normal developmental timelines and does not correspond to any known pathological condition.

Specifically, the affected children display a pattern we are terming "compressed early ossification." The growth plates of the long bones — particularly the distal femur, proximal tibia, and distal radius — show layering patterns consistent with extremely rapid growth during the first 2-3 years of biological development, followed by a normalization to standard growth velocity. The metaphyseal architecture suggests that these children passed through infancy and early toddlerhood at a rate approximately 4-6 times faster than normal, then transitioned to normal developmental speed at approximately age 3-4 biological years.

This pattern has no known natural etiology. Endocrine disorders that accelerate growth — such as precocious puberty or growth hormone excess — produce different radiological signatures. Nutritional factors do not produce this pattern. Genetic conditions associated with accelerated development do not match the specific layering we observe. The pattern is, to our knowledge, unprecedented in the pediatric radiology literature. It is, however, consistent with the theoretical predictions of accelerated-growth cloning protocols described in publicly available biotechnology literature — specifically, protocols designed to bring a biological organism from embryonic state to a target developmental age in a compressed timeframe, then arrest the acceleration.

We submitted this paper to three peer-reviewed journals between June 2225 and January 2226. All three declined to publish. The Journal of Pediatric Radiology stated that the paper "addresses a topic outside the scope of our editorial mission." The Meridian Medical Review stated that the findings were "insufficiently supported by established precedent." The GLMZ Journal of Clinical Medicine did not provide a reason for rejection. None of the three engaged with the radiological evidence on its merits.

We are making this paper available through pre-print channels because we believe the findings are clinically significant and potentially relevant to the ongoing demographic anomaly documented by the Bureau of Census. We note that the 38.2% incidence rate among unregistered children is remarkably close to independent estimates of the proportion of surplus children that cannot be accounted for by migration or record-keeping errors. We draw no conclusions. We present the data and invite our colleagues to examine it.`,
    related_entities: ["GLMZ General Hospital", "Bureau of Census and Population Analytics", "GLMZ"],
    credibility: "suppressed",
    story_hooks: [
      "The compressed ossification pattern is a physical biomarker that could definitively identify clone-origin children",
      "Three journal rejections suggest coordinated suppression of the findings"
    ],
    tags: ["document", "children_paradox", "academic_paper", "medical", "radiology", "clone", "growth_acceleration", "suppressed", "biomarker"]
  },
  {
    name: "Letter to My Future Body",
    document_type: "personal_account",
    author: "Unknown (attributed to Henrik Calder-Osei, deceased)",
    date: "2224-11-02",
    classification: "leaked",
    description: `The following document was discovered in a sealed container within the Tier 5 residential apartment of Henrik Calder-Osei, a retired pharmaceutical logistics executive, during routine post-mortem property clearance on November 2, 2224. Calder-Osei died of natural causes on October 28, 2224, at the age of 87. He had no surviving children, no designated heirs, and no will on file. The container was a fireproof document safe, biometrically locked, which required override authorization to open. The contents consisted of a single handwritten letter.

"To myself, when I am small again. If you are reading this, the procedure worked. You will not remember everything immediately — they said the first months are disorienting, and the new brain needs time to integrate the patterns. Be patient with yourself. You have done this before, even if you don't remember doing it.

The following information is essential for recovering your previous life's assets: The primary account is held at Meridian First Federal under the name Calder Trust, account designation CF-7791-ALPHA. Access requires the passphrase 'the river remembers its source.' Secondary holdings are distributed across four shell entities registered in the Detroit Autonomous Collective — the names and access codes are in the second safe, third floor, northwest corner of the apartment you are currently inheriting from yourself.

Contact Dr. [REDACTED] at the [REDACTED] clinic. The address is [REDACTED]. This clinic does not appear in any public directory. You found it through [REDACTED] in 2221. They will have your follow-up protocol. Do not skip the cognitive integration sessions — last time you skipped them and lost eleven months of procedural memory.

You will be frightened. You will feel wrong in your body. Your hands will be too small and your voice will be too high and you will dream of being old in a way that feels more real than being young. This passes. By year two, the body feels like yours. By year three, you won't remember what the old one felt like. Trust the process. Trust yourself. You have the rest of a life to live. — H.C.O."

The letter is written in handwriting confirmed by forensic analysis to be that of Henrik Calder-Osei. The account referenced at Meridian First Federal exists but is sealed under fiduciary lock. The apartment contained no second safe. The property clearance team filed the letter as an artifact of cognitive decline. No further investigation was conducted.`,
    related_entities: ["Meridian First Federal", "Detroit Autonomous Collective", "Lazarus Pharmaceuticals"],
    credibility: "verified",
    story_hooks: [
      "The sealed Calder Trust account could be monitored — if a child attempts to access it, that would confirm the transfer",
      "'Last time you skipped them' implies Calder-Osei had done this before — this may not have been his first transfer"
    ],
    tags: ["document", "children_paradox", "personal_account", "consciousness", "immortality", "tier_5", "lazarus", "transfer", "letter", "leaked"]
  },
  {
    name: "The Children Paradox — A Summary of What We Don't Know",
    document_type: "investigation",
    author: "Compiled by the GLMZ Independent Research Collective",
    date: "2226-07-01",
    classification: "public",
    description: `This document is a compilation of verified data, credible reports, and documented anomalies related to what has become known as the Children Paradox — the persistent and growing discrepancy between projected and observed child populations in GLMZ. It is not a theory. It is not an argument. It is an inventory of what we know and, more importantly, what we do not know. We publish it because the accumulation of unknowns has reached a mass that demands acknowledgment, even if it does not yet permit explanation.

What we know: Birth rates in GLMZ have declined 31% over the past 40 years, consistent with trends across the GLMZ and global urban centers. Contraception is ubiquitous and effectively free. The economic conditions of the Shelf — where 68% of GLMZ's population lives — make child-rearing financially catastrophic for most families. And yet the under-12 population exceeds actuarial projections by 14.3%, a gap that has widened every year since at least 2218. Birth registrations account for only 82% of identified children. The remaining 18% — approximately 12,400 individuals — have no birth records, no hospital records, and no BCI installation dates.

What we suspect: A subset of unregistered children — estimates range from several hundred to several thousand — display cognitive and behavioral characteristics inconsistent with their biological age. Radiological evidence suggests a significant minority of unregistered children experienced dramatically accelerated physical development during early life. A leaked internal memo from Lazarus Pharmaceuticals describes a cloning program with a documented accountability gap matching the scale of the demographic anomaly. An academic paper exploring the ethics of consciousness transfer into cloned bodies was published and then retracted under apparent pressure. A document circulating on Shelf networks describes a commercial program for transferring adult consciousness into child clone bodies.

What we cannot prove: That the Lazarus cloning program produced the surplus children. That consciousness transfer technology exists and has been used. That any specific unregistered child is a clone, a transplant, or anything other than a child who fell through the cracks of an imperfect system. That any institution is deliberately concealing the truth. That there is a single truth to conceal. The evidence is suggestive, convergent, and incomplete. Every thread leads to a door that is closed.

What we observe: No institution is investigating. The Bureau of Census classified its own findings as restricted. CorpSec has issued no statements. The Governance Council has not convened a hearing. Lazarus Pharmaceuticals denies everything. The journals that could publish the evidence decline to do so. The children themselves — the ones who might be able to answer the question — are children, and we do not interrogate children. We feed them and send them to school and watch them play, and we try not to think about the fact that some of them play like children and some of them play like people who remember what it was like to be something else.

We counted the children again today. There are more than yesterday.`,
    related_entities: ["GLMZ", "Lazarus Pharmaceuticals", "Bureau of Census and Population Analytics", "CorpSec", "GLMZ Governance Council", "Shelf", "GLMZ", "GLMZ Independent Research Collective"],
    credibility: "verified",
    story_hooks: [
      "The final line — 'there are more than yesterday' — implies the phenomenon is accelerating, not stabilizing",
      "The compiled data could be the foundation for someone to finally connect all the threads into a provable case"
    ],
    tags: ["document", "children_paradox", "investigation", "summary", "census", "anomaly", "lazarus", "clone", "consciousness", "meridian_88", "meta_document"]
  }
];

// Generate and write files
let written = 0;
let skipped = 0;

for (const doc of documents) {
  const id = genId();
  const filePath = path.join(outDir, `${id}.json`);

  // Avoid overwrites
  if (fs.existsSync(filePath)) {
    console.log(`SKIP (exists): ${filePath}`);
    skipped++;
    continue;
  }

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

  fs.writeFileSync(filePath, JSON.stringify(entity, null, 2), 'utf-8');
  console.log(`WROTE: ${filePath}`);
  written++;
}

console.log(`\nDone. Written: ${written}, Skipped: ${skipped}, Total: ${documents.length}`);
