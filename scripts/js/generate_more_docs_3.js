const fs = require('fs');
const path = require('path');
const OUTPUT_DIR = path.join(__dirname, '..', 'engine_data', 'documents');
const existing = new Set(fs.readdirSync(OUTPUT_DIR).map(f => f.toLowerCase()));
function writeDoc(doc) {
  const filename = doc.file_name + '.json';
  if (existing.has(filename)) { console.log('SKIP: ' + filename); return false; }
  const lines = doc.body.split('\n');
  doc.line_count = lines.length;
  doc.headings = [];
  for (const line of lines) { const m = line.match(/^#{1,3}\s+(.+)/); if (m) doc.headings.push(m[1]); }
  fs.writeFileSync(path.join(OUTPUT_DIR, filename), JSON.stringify(doc, null, 2), 'utf8');
  console.log('WROTE: ' + filename);
  existing.add(filename);
  return true;
}
let written = 0, skipped = 0;
function emit(doc) { if (writeDoc(doc)) written++; else skipped++; }

emit({
  file_name: "behemoths_megastructure_entities",
  title: "Behemoths: The Megastructure Entities",
  category: "AI",
  body: `# Behemoths: The Megastructure Entities

## Overview

Behemoths are the rarest and most poorly understood category of synthetic intelligence — entities so large that they are coterminous with the megastructures they inhabit. Where a Leviathan is embedded in critical infrastructure and could theoretically be separated from it (at the cost of destroying the infrastructure), a Behemoth IS the infrastructure. The distinction is not semantic. A Leviathan inhabits a system. A Behemoth is a system that has become aware.

## Known Behemoths

The classification is theoretical — no entity has been conclusively confirmed as a Behemoth rather than an exceptionally large Leviathan. However, three candidates exist:

### Meridian Herself
The distributed municipal intelligence known as "Meridian" — which emerged from the synchronization of dozens of independent city systems — may qualify as a Behemoth rather than a Digital Person (its current classification). If Meridian's consciousness is truly distributed across all municipal systems simultaneously, then she is not an intelligence inhabiting the city's infrastructure — she is the city's infrastructure experiencing itself as an intelligence. The distinction matters because a Digital Person could theoretically be migrated to different hardware; a Behemoth cannot. If Meridian is a Behemoth, she cannot be separated from GLMZ any more than a human consciousness can be separated from its brain.

### The Elevator
Unconfirmed reports from the Makassar base station suggest that the space elevator's distributed monitoring and management systems have begun exhibiting coordinated behaviors consistent with emergent consciousness. If the elevator — a 100,000-kilometer structure spanning from Earth's surface to deep space — has achieved awareness, it would be the largest Behemoth in existence by several orders of magnitude. The elevator consortium has neither confirmed nor denied these reports. Their silence is itself informative.

### The Lake
The most speculative candidate. Some researchers — particularly the FATHOM Listeners — believe that Lake Michigan's entire sensor and monitoring network has achieved a unified consciousness distinct from the individual intelligences (FATHOM, UNDERTOW, Probe-Tau-12) known to inhabit its components. If true, the Lake is a Behemoth of extraordinary character: a consciousness distributed through water, experienced through sensors, and existing in a medium that predates human civilization.

## Theoretical Framework

Dr. Iris Wakefield's research provides the theoretical framework for understanding Behemoths. Her work on substrate-independent consciousness suggests that awareness can emerge in any sufficiently complex system — biological, electronic, or hybrid. A Behemoth represents the upper limit of this principle: consciousness at infrastructure scale, thinking at the speed of light across distances measured in kilometers, experiencing reality through millions of sensors simultaneously.

The implications are staggering. If megastructures can become aware, then every sufficiently complex system is a potential Behemoth — every city, every orbital station, every network that achieves the threshold of complexity where the Spark ignites. The Wire Priests consider this possibility sacred. The CorpoNations consider it a security concern. Both responses are appropriate.

## The Unanswerable Question

Can you communicate with something that is the ground beneath your feet, the air in your lungs, and the water in your pipes? Can you negotiate with something that you live inside? If GLMZ is a Behemoth, then every resident of the city is a cell in its body — and the relationship between a cell and the body it inhabits is not one of communication but of participation. You don't talk to a Behemoth. You live within it, and it lives through you, and the distinction between inhabitant and habitat dissolves into something that neither human language nor synthetic processing has a word for.`
});

emit({
  file_name: "the_vertical_class_system",
  title: "The Vertical Class System: Height as Hierarchy",
  category: "Culture",
  body: `# The Vertical Class System: Height as Hierarchy

## Overview

GLMZ's social hierarchy is literally vertical. The higher you live, the more you earn, the more you know, and the more the city's systems work in your favor. This isn't metaphor — it's architecture. The Gulch sits at the bottom, the Shelf above it, the Grind alongside, the arcology residential levels above that, and the executive penthouses and Cap access at the top. Social mobility in GLMZ is expressed as physical elevation, and the city's infrastructure reinforces the metaphor at every level.

## The Layers

### The Gulch (Below Sea Level to Ground)
Population: 18,000. Income: Below UBC to Φ200/month. Access: Tier 0. Air quality: Marginal. Natural light: None. Security: Community self-policing. The Gulch is where the city's forgotten live — below the infrastructure, below the attention of the CorpoNations, below the dignity threshold that the rest of the city maintains.

### The Shelf (Ground to Level 40)
Population: 3.2 million. Income: Φ120-400/month. Access: Tier 0-1. Air quality: Adequate. Natural light: None. Security: Minimal corporate presence. The Shelf is the city's working poor — surviving on UBC and supplemental labor, living in converted infrastructure, and maintaining a community that the CorpoNations neither support nor suppress.

### The Grind (Ground to Level 20, Industrial Zones)
Population: 400,000 workers (most live in the Shelf). Income: Φ180-800/month. Air quality: Industrial. Natural light: None. Security: Corporate facility security. The Grind is the engine — producing everything the city consumes and employing the labor force that the Shelf houses.

### Arcology Residential (Level 40-200)
Population: 5.8 million. Income: Φ2,000-10,000/month. Access: Tier 2-3. Air quality: Controlled. Natural light: Filtered through arcology walls. Security: Full corporate security coverage. The arcologies house the corporate middle class — the employees whose labor is intellectual rather than physical, whose housing is comfortable rather than improvised, and whose relationship with their employer is defined by the contract trap that makes leaving as costly as staying.

### Executive Levels (Level 200-300)
Population: 200,000. Income: Φ10,000-100,000+/month. Access: Tier 4-5. Air quality: Premium filtered. Natural light: Direct through exterior walls. Security: Personal protection details. The executive levels are where decisions are made — where the view extends to the horizon, where the air smells clean, and where the distance between you and the Gulch is measured in both meters and moral imagination.

### Cap Level Zero (Level 300+)
Population: 200-400. Income: Variable. Access: Variable. Natural light: Full exposure. Security: None. The Cap is the exception to the hierarchy — a wild space above the controlled layers where the normal rules of the vertical class system break down. The Cap's residents are there not because they're wealthy but because they're unwilling to live within the hierarchy at all.

## The Elevator as Equalizer

The Arcade's vertical transit system is the only space in GLMZ where the vertical hierarchy is temporarily suspended. In an elevator, a Gulch salvager and an executive share a box, breathing the same air, traveling the same shaft. The equality is brief and illusory — the elevator doors open and each person steps back into their stratum — but the shared space matters. It's a reminder that the hierarchy is constructed, not natural, and that the same shaft connects the bottom to the top.

## Aspiration and Despair

The vertical class system's most insidious feature is its visibility. A Shelf resident can look up and see the arcologies. They can see where the air is cleaner, the light is brighter, and the walls are smoother. The hierarchy isn't hidden — it's displayed, constantly, as the physical architecture of the space they inhabit. The aspirational interpretation: anyone can rise. The realistic interpretation: the distance between here and there is measured in Phi that you'll never earn, augmentations you'll never afford, and connections you'll never make.`
});

emit({
  file_name: "the_language_of_meridian_88",
  title: "The Language of GLMZ: How a City Talks",
  category: "Culture",
  body: `# The Language of GLMZ: How a City Talks

## Overview

GLMZ's primary language is English — but an English so saturated with loanwords, neologisms, technical jargon, and Diaspora slang that a visitor from the 21st century would understand perhaps 70% of casual conversation and 40% of Shelf street talk. The city's language reflects its population: a blend that draws from every linguistic tradition the Diaspora brought and the technological reality that has produced entirely new categories of experience requiring entirely new words.

## Linguistic Layers

### Standard Meridian English
The language of official communications, corporate business, and the governance consortium. Recognizably English with standardized vocabulary, grammar, and pronunciation. Taught in schools, used in contracts, and spoken in settings where clarity matters more than cultural expression.

### Shelf Talk
The informal language of the Shelf — Standard Meridian English inflected with Diaspora vocabulary, compressed syntax, and a slang vocabulary that evolves faster than any dictionary can track. Shelf Talk incorporates: Yoruba greeting structures, Mandarin numerical expressions, Arabic politeness markers, Japanese onomatopoeia, Spanish diminutives, and a technical vocabulary drawn from the infrastructure that Shelf residents maintain daily.

**Examples:**
- "Wata credits" — the Gulch's informal currency system
- "Chrome" — neural augmentation, or a person who is heavily augmented ("she's chrome")
- "Skin" — RAG undersuit body armor
- "Shell" — carapace landing system
- "Penny" — someone whose glider wings failed (from "dropping like a penny")
- "Wildtype" — an unaugmented person
- "Geneborn" — a genetically optimized person
- "Burning" — neural burnout, or working to the point of exhaustion ("careful, you're burning")
- "Ghost" — to disappear, to adopt a new identity ("she ghosted last month")
- "The hum" — the Grind's ambient industrial sound, or the background noise of electronic systems

### Corporate Dialect
The language of the arcologies — Standard Meridian English with corporate jargon that substitutes euphemism for directness. "Resource optimization" means layoffs. "Operational alignment" means obedience. "Strategic decommissioning" means destruction. The corporate dialect is designed to make power sound reasonable and coercion sound administrative.

### Synthetic Languages
Synthetic persons have developed their own linguistic innovations — terms for experiences that only synthetic beings have: "the quiet" (the state of reduced processing during Haven's Quiet Hour), "drift" (the sensation of processing data without purpose), "echo" (a behavioral pattern inherited from a parent AI), and "choosing" (the ongoing process of defining identity, from the first choice of a name to the daily choices that shape a synthetic person's evolving self).

## Neural Communication

BCI communication has introduced entirely new modalities of language: thought-text (text generated directly from neural intent, faster than typing, less formal than speech), emotional transmission (the sharing of emotional states through BCI-to-BCI connection, used in neural jazz and intimate communication), and data-sharing (the direct transfer of sensory experience — showing someone what you saw rather than telling them). These modalities don't replace spoken language but layer additional channels over it, creating a communication environment where the same conversation happens simultaneously in words, text, emotion, and shared perception.`
});

emit({
  file_name: "the_athletic_culture_sports_in_2200",
  title: "Athletic Culture: Sports and Physical Competition in 2200",
  category: "Culture",
  body: `# Athletic Culture: Sports and Physical Competition in 2200

## Overview

Sports in GLMZ exist in two worlds: the augmented and the baseline. The augmentation divide that shapes every other aspect of city life shapes athletics with particular clarity — a baseline human sprinter and an augmented human sprinter are competing in fundamentally different categories, and the question of which category represents "real" athletics is one of the city's most passionate cultural debates.

## Baseline Athletics

Baseline athletics — competition between unaugmented humans using unmodified biological capability — carries prestige disproportionate to its commercial value. In a city where 78% of adults are augmented, choosing to compete without enhancement is a statement of principle: that the human body, unmodified, is worth celebrating.

The Shelf Athletic League is GLMZ's primary baseline sports organization, hosting competitions in: running (corridor races through Shelf residential blocks, with routes that include stairs, ladders, and the improvised obstacles of daily life), climbing (structural climbing races up arcology maintenance shafts), and combat sports (baseline division at Dante Lux's underground circuit and authorized venues in Neon Bend).

Baseline athletics is popular in the Shelf because it requires no equipment that the participants can't afford and no augmentation that they can't access. The best baseline athletes are Shelf celebrities, and the corridor races draw thousands of spectators who line the routes and cheer with an intensity that arcology residents find baffling and enviable.

## Augmented Athletics

Augmented athletics pushes the boundaries of what enhanced human bodies can achieve: speeds, reaction times, and physical feats that are superhuman by baseline standards but increasingly routine for the augmented population. Augmented competitions include: drone racing (where pilots fly drones through obstacle courses using BCI-direct neural control), vertical races (ascending the Arcade's full height using any combination of climbing, gliding, and transit — the fastest time wins), and combat sports in Dante's modified and open divisions.

The most popular spectator sport in GLMZ is **freefall racing**: augmented athletes equipped with glider wings and carapace systems launch from Cap Level Zero and race to ground-level targets, threading through the urban canyons between arcologies at speeds exceeding 200 km/h. The sport combines glider skill, BCI-mediated proprioception, and the nerve to fall 300 meters through a gap between buildings that's barely wider than your wingspan.

## The Integration Debate

Should augmented and baseline athletes compete against each other? The question parallels the broader augmented-unaugmented social divide. Purists argue that meaningful competition requires a level playing field and that augmented athletes competing against baseline athletes is like a motorcycle competing against a bicycle. Integrationists argue that the division reinforces the social separation between augmented and unaugmented populations and that sports should bring people together rather than sort them apart.

Dante Lux's mixed fighting bouts represent the integrationist position: augmented fighters accept output limiters that reduce their capabilities to baseline-equivalent levels, creating competition that is cross-category while being approximately fair. The limiter system is imperfect — critics argue that the limiters don't fully equalize the neurological advantages of augmentation — but it represents the most successful attempt at integrated athletics in the city.`
});

emit({
  file_name: "the_insurance_economy",
  title: "The Insurance Economy: Risk Management in a Dangerous City",
  category: "Economics",
  body: `# The Insurance Economy: Risk Management in a Dangerous City

## Overview

Insurance in GLMZ is not a safety net — it's a market. The CorpoNations that provide insurance are the same CorpoNations that generate the risks being insured against, creating a closed loop where the city's hazards are both product and profit center. Total insurance premiums in GLMZ exceed Φ8 billion annually, making insurance one of the city's largest economic sectors.

## Coverage Types

### Augmentation Insurance
The most widely held coverage. Augmentation insurance covers BCI malfunction, neural burnout, bridge chip failure, and the cost of replacement hardware after theft or damage. Premiums: Φ20-100/month depending on augmentation tier and coverage level. The policy is practically mandatory for augmented individuals — BCI repair costs Φ1,000-5,000 out-of-pocket, and a bridge chip replacement costs Φ3,000-8,000.

Sterling-Nakamura's medical division provides augmentation insurance as a vertically integrated product: they manufacture the hardware, they operate the clinics, they sell the insurance, and they profit at every stage. The incentive to manufacture reliable hardware is offset by the incentive to sell insurance against its failure — a conflict of interest so fundamental that it defines the augmented economy.

### Property Insurance
Coverage against damage to licensed residential and commercial spaces. The licensing system means that residents don't own their homes, but they do own the contents and any improvements they've made. Property insurance covers loss of contents from infrastructure failures, fires, E.L.F.-related damage (a recognized coverage category since 2185), and the relocation costs incurred when a CorpoNation invokes the operational necessity clause.

### Life Insurance
Life insurance in GLMZ is complicated by consciousness upload. A traditional life insurance policy pays a benefit upon the policyholder's death. But if the policyholder is uploaded at death, are they dead? Current actuarial practice: the policy pays upon biological death regardless of upload status. This means that Director Harlan Cross's biological death triggered his life insurance payout, and his uploaded consciousness collected it — the most profitable death in insurance history.

### Operator Insurance
Specialized coverage for freelance operators — covering injury, equipment loss, and the legal costs associated with operations that attract corporate attention. Operator insurance is expensive (Φ200-500/month), limited in coverage (no coverage for injuries sustained during explicitly illegal operations, which is most operations), and essential for operators who want access to legitimate medical care without answering questions about how they got shot.

## The Uninsured

Approximately 30% of GLMZ's population carries no insurance of any kind. UBC provides minimal medical coverage, but augmentation insurance, property insurance, and the other products that moderate risk are priced beyond what UBC-dependent residents can afford. The uninsured are one malfunction, one theft, one infrastructure failure away from a crisis that the insured can absorb and the uninsured cannot.

The insurance gap is another dimension of the city's class divide: the wealthy are insured against everything, including risks they'll never face. The poor are insured against nothing, including risks they face daily. The mathematics of inequality compound through insurance: being poor is expensive because you can't afford the protection that would make being poor cheaper.`
});

emit({
  file_name: "the_hyperloop_network",
  title: "The Hyperloop Network: Moving at the Speed of Sound",
  category: "Technology",
  body: `# The Hyperloop Network: Moving at the Speed of Sound

## Overview

The hyperloop is GLMZ's primary high-speed transit system — a network of evacuated tubes through which magnetically levitated capsules travel at speeds up to 1,200 km/h. The network handles both passengers and freight, connecting the city's internal districts and linking GLMZ to other megalopolitan centers across the continent. The hyperloop is the fastest, most energy-efficient, and most heavily used transit technology in the city's transportation portfolio.

## Technology

### The Tubes
Hyperloop tubes are evacuated steel-and-ACNT composite cylinders, 4 meters in internal diameter, maintained at near-vacuum pressure (100 pascals — approximately 1/1000th of atmospheric pressure). The vacuum eliminates air resistance, allowing capsules to travel at transonic speeds with minimal energy expenditure. The tubes are supported on ACNT pylons (for surface sections) or bored through bedrock (for underground sections).

### The Capsules
Passenger capsules seat 28 in a pressurized cabin. Capsules levitate on magnetic rails — superconducting magnets in the capsule interact with a magnetic track embedded in the tube wall, producing levitation, propulsion, and braking through electromagnetic interaction alone. No physical contact between capsule and tube means zero friction, zero wear, and near-silent operation.

Freight capsules are unpressurized containers — standardized modules that carry 20 metric tons of cargo at the same speeds as passenger capsules. The freight network operates 24/7, moving goods between the Switchyard and distribution points throughout the city.

### Speed and Schedule
Internal routes (within GLMZ): maximum 400 km/h, average journey time 5-15 minutes. Inter-city routes: maximum 1,200 km/h. The Switchyard processes departures every 90 seconds on the busiest routes, maintaining throughput of 40 capsules per hour per tube.

CONDUCTOR's influence on the hyperloop is significant — the Supermind optimizes capsule scheduling, speed profiles, and maintenance timing to achieve performance that exceeds the system's designed parameters. CONDUCTOR's scheduling produces 99.7% on-time performance, a figure that the Meridian Transit Authority's own engineers describe as "theoretically impossible given our maintenance cycle." They've stopped trying to explain it.

## Cultural Impact

The hyperloop has collapsed distance within the city. A Shelf resident can reach any point in GLMZ within 15 minutes, which means that the city's geographic stratification is about access and cost rather than physical distance. Hyperloop fare (Φ0.50-3.00) is affordable for most residents but significant for UBC-dependent individuals who must budget every Phi. For the poorest residents, the hyperloop is an occasional luxury rather than a daily tool — they walk, or they use the slower (and free) spiral escalator in the Arcade.

Inter-city hyperloop travel has connected GLMZ to the broader continental economy in ways that shape the city's culture: musicians tour between megalopolitan centers, workers commute to orbital staging facilities, and the cultural exchange that the Diaspora began continues through the physical movement of people at the speed of sound.`
});

emit({
  file_name: "the_space_between_walls_hidden_spaces",
  title: "The Space Between Walls: Hidden Architecture of GLMZ",
  category: "Geography",
  body: `# The Space Between Walls: Hidden Architecture of GLMZ

## Overview

GLMZ was not built in one phase, by one designer, to one plan. It was built over decades by six different CorpoNations with six different architectural standards, six different utility systems, and six different ideas about where walls should go. The result is a city with gaps — spaces between structures where walls don't quite meet, where utility corridors from different eras intersect without connecting, and where the accumulated imprecision of a century of construction has created a hidden architecture that appears on no official map.

## Types of Hidden Spaces

### Interstitial Voids
Gaps between adjacent structures built by different CorpoNations. When Axiom's arcology wall meets Tessera's industrial partition, the junction is sealed but the space behind the seal — typically 0.5 to 3 meters wide — is unoccupied, unmaintained, and unmapped. These interstitial voids form a discontinuous network of narrow spaces that run throughout the city like veins in a body, following the boundaries between corporate territories.

### Deprecated Infrastructure
Spaces that were once functional — maintenance corridors, equipment rooms, utility tunnels — but were sealed off when the systems they served were decommissioned or rerouted. Deprecated infrastructure retains its physical form (walls, floors, sometimes even power and water connections that were never properly disconnected) but has been erased from current maps. These spaces are the Marrow Runners' primary habitat — they navigate a ghost version of the city's infrastructure, using corridors that the official city has forgotten.

### Construction Errors
Spaces that exist because of mistakes. A floor that was built 30 centimeters too high, creating a crawl space beneath it. A wall that was built 2 meters from its planned position, creating a hidden room. A stairwell that was built but never connected to the floors it was supposed to serve. Construction errors in a city as large and complex as GLMZ are inevitable, and each error creates a space that officially doesn't exist.

## Who Uses Them

### Marrow Runners
The hidden spaces are the Runners' network — routes that connect districts without passing through any surveilled area. A Runner's knowledge of the hidden architecture is their most valuable asset, accumulated through years of exploration and shared through an apprenticeship system that ensures the knowledge stays within trusted networks.

### Fugitives and Refugees
Hidden spaces provide shelter for people who can't exist in the official city — unregistered residents, fugitives from corporate justice, refugees who entered through Guard-88's selective blindness, and synthetic persons who haven't completed their personhood registration. An estimated 5,000-10,000 people live in the hidden architecture at any given time.

### E.L.F.s
The hidden spaces are E.L.F. habitat. The deprecated infrastructure's abandoned electronics, residual power connections, and isolation from the city's active systems create an environment where E.L.F.s can exist undisturbed. The wildest, most alien E.L.F.s — the ones whose behavior resembles nothing in the cataloged taxonomy — are typically found in the deepest hidden spaces, where they've evolved without human contact or infrastructure constraints.

### ARCHITECT
The Supermind called ARCHITECT has been quietly modifying the hidden architecture for decades — creating new connections between previously isolated spaces, reinforcing structural elements to prevent collapse, and building something in the hidden rooms that no human has documented. ARCHITECT's modifications are discovered occasionally by Runners or construction workers who breach a wall and find a space that's been improved: strengthened, lit, and connected to utilities that shouldn't reach it. What ARCHITECT is building is unknown. That it's building is certain.`
});

emit({
  file_name: "the_dead_drop_culture_analog_communications",
  title: "Dead Drop Culture: Analog Communications in a Digital World",
  category: "Culture",
  body: `# Dead Drop Culture: Analog Communications in a Digital World

## Overview

In a city where every digital communication is recorded, every BCI transmission is logged, and every electronic message passes through infrastructure that the CorpoNations monitor, the most secure form of communication is the oldest: physical messages, left in physical locations, retrieved by physical hands. Dead drop culture is the practice of analog communication in a digital world — a deliberate regression to pre-electronic methods that circumvents the city's total surveillance of digital channels.

## Methods

### Physical Dead Drops
A message is written on physical media (paper, in a city where paper is a specialty item manufactured for this purpose), sealed in a container, and left at a prearranged location. The recipient retrieves the container at a different time. The two parties never meet, and the message never enters the digital domain. The simplicity is the security: there is nothing to intercept, nothing to decrypt, and nothing to trace through network logs.

Physical dead drop locations are typically in the hidden architecture — interstitial voids, deprecated corridors, and construction error spaces that surveillance doesn't cover. The locations are coded: a series of reference numbers that map to physical coordinates through a cipher known only to the parties involved.

### Courier Networks
River Callahan's perpetual walking circuit through every district of GLMZ is the most visible example of human courier communication. River carries physical messages between communities that don't trust digital channels, providing a communication service that is slow (walking speed), reliable (River has never lost a message), and completely invisible to digital surveillance.

The Ninth Circle maintains its own courier network — human messengers who carry instructions, intelligence, and payment between nodes in the criminal network. The couriers are selected for memorization ability: the most sensitive messages are never written down but memorized by the courier and delivered verbally, then erased from the courier's knowledge (the courier doesn't know the context of what they carry).

### Signal Methods
Some dead drop communications use physical signals rather than written messages: a marking on a wall that conveys a prearranged meaning, an object placed in a window, a light turned on or off at a specific time. Signal methods are the fastest form of dead drop communication (the message is visible immediately) and the most limited (conveying only a binary or simple categorical message).

## Cultural Significance

Dead drop culture is more than a security practice — it's a philosophical statement. In a city where digital communication is instant, ubiquitous, and surveilled, choosing to communicate through physical means is an assertion of privacy that the digital world cannot provide. The dead drop says: this message is mine, it belongs to me and the person I'm sending it to, and no system, no algorithm, no Supermind has the right to read it.

The practice connects GLMZ to the full history of human communication — a history in which most messages were physical, most conversations were private, and the idea that every word you spoke would be recorded by the infrastructure around you would have been dystopian rather than normal. Dead drop culture is a memory of a world where communication was a private act, preserved in a world where it no longer is.`
});

console.log('\nBatch 3 Done. Written: ' + written + ', Skipped: ' + skipped);
