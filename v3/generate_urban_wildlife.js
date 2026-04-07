const fs = require('fs');
const path = require('path');
const crypto = require('crypto');

const documentsDir = path.resolve(__dirname, '..', 'engine', 'data', 'documents');
const genewareDir = path.resolve(__dirname, '..', 'engine', 'data', 'geneware');

function generateId() {
  return crypto.randomBytes(16).toString('hex');
}

function writeEntity(dir, entity) {
  const filePath = path.join(dir, `${entity.id}.json`);
  if (fs.existsSync(filePath)) {
    console.log(`SKIP (exists): ${filePath}`);
    return false;
  }
  fs.writeFileSync(filePath, JSON.stringify(entity, null, 2), 'utf-8');
  console.log(`WROTE: ${filePath}`);
  return true;
}

// ─────────────────────────────────────────────────────────────
// 15 DOCUMENTS — Urban Wildlife of Meridian 88
// ─────────────────────────────────────────────────────────────

const documents = [
  {
    name: "Does That Pigeon Glow in the Dark?",
    document_type: "news_feature",
    author: "Maren Solís-Ng, Vantablack Media",
    date: "2224-06-14",
    classification: "public",
    description: `For the last three decades, residents of Meridian 88's mid-tier districts have been treated to an unexpected amenity: pigeons that glow. The birds — common rock doves, Columba livia, the same species that has infested human cities for millennia — emit a faint blue-green bioluminescence along their breast feathers, visible at dusk and in darkened alleyways. The effect is subtle but unmistakable. In certain lighting conditions, a roosting flock looks like a constellation that decided to nest on a fire escape.

The source of the glow has been traced to geneware contamination in the municipal water supply. Specifically, a luminescence modification originally developed by Helix Biosystems for cosmetic pet applications entered the groundwater table through improperly disposed biowaste sometime around 2190. The modification codes for a variant of green fluorescent protein that integrates into feather keratin during molt. The pigeons drink the water. The pigeons grow new feathers. The feathers glow. It is, in the clinical language of the environmental impact assessment, "an uncontrolled transspecies cosmetic expression event."

Residents of the Shelf have largely embraced the glowing pigeons. Night market vendors in the Circuit sell pigeon-watching tours. Children name the birds. A Shelf community board in Block 14 voted to designate them "neighborhood wildlife ambassadors," which carries no legal weight but was accompanied by a very nice ceremony. The pigeons attended. They did not appear to care.

Biologists at the Great Lakes Metropolitan Zone Environmental Monitoring Station are less charmed. Dr. Yuki Andersen-Okafor, lead ornithologist, describes the contamination as "a multi-generational genetic alteration affecting a wild population numbering in the hundreds of thousands, with no containment strategy and no reversibility pathway." The bioluminescence itself is cosmetically harmless, but it indicates that geneware agents are actively incorporating into wild genomes — and luminescence is the modification that happens to be visible. What other modifications are expressing silently?

Perhaps most unsettling is the navigation shift. Andersen-Okafor's team has documented that Meridian 88's pigeons no longer navigate primarily by the Earth's magnetic field, as their ancestors did. Instead, they orient by BCI signal strength gradients — the invisible topology of neural interface broadcasts that saturate the city. They fly toward signal density. They roost near relay nodes. They have, without anyone designing it or intending it, rewired their migratory instincts to treat the city's digital infrastructure as terrain. They've been doing this for at least thirty years. Nobody noticed until someone thought to check.`,
    related_entities: ["Meridian 88", "Helix Biosystems", "GLMZ", "Shelf", "Circuit District"],
    credibility: "verified",
    story_hooks: [
      "If pigeons navigate by BCI signal, they could be used to map signal dead zones — or to find hidden signal sources",
      "What other geneware modifications are silently expressing in wild populations?",
      "A pigeon flock suddenly abandons a district — what changed in the signal landscape?"
    ],
    tags: ["document", "news", "urban_wildlife", "pigeon", "bioluminescence", "geneware", "contamination", "ecology", "meridian_88", "shelf", "vantablack"]
  },
  {
    name: "The Rat Situation",
    document_type: "community_report",
    author: "Block 7 Community Health & Safety Board",
    date: "2225-02-08",
    classification: "internal",
    description: `This report addresses the ongoing and escalating pest management challenges in Block 7 and adjacent Shelf communities, specifically regarding the brown rat (Rattus norvegicus) populations that have demonstrated behavioral anomalies exceeding normal adaptive capacity. The Board has compiled incident reports, pest control contractor assessments, and resident testimony over a fourteen-month period. The findings are presented without editorialization, though the Board acknowledges that several findings are difficult to present without editorializing.

Standard snap traps deployed in Block 7 waste processing areas in January 2224 achieved a 34% capture rate during the first week of deployment. By the second week, the rate had dropped to 6%. By the third week, it was zero. Traps were found sprung but empty, repositioned, or in one case, flipped upside down. Glue traps were avoided entirely from the first day of deployment. Electronic traps with variable bait dispensing achieved initial success rates of 22%, declining to zero within five days. The pest control contractor, RodentX Solutions, reported that the rats were adapting to new trap designs "within hours, not days," and that this adaptation rate was "not consistent with individual learning — it suggests information transfer between animals."

More concerning is the observed logistical behavior. Surveillance footage from Block 7's eastern dumpster array shows rat activity patterns that correlate precisely with the municipal waste collection schedule. The rats do not forage randomly. They arrive at specific dumpsters on specific nights — the nights before those dumpsters are emptied, when food waste is most abundant. They do not visit dumpsters that were emptied that morning. They have learned the schedule, and they follow it with more consistency than some human residents.

The most anomalous observations concern the colony structure itself. Dr. Tariq Mensah, a behavioral ecologist contracted by the Board, spent six weeks observing the Block 7 colony using infrared surveillance. His report identifies what he cautiously terms "role differentiation" within the colony. Specific individuals consistently perform scouting functions — entering new areas first, returning to the colony, after which larger groups follow established routes. Other individuals position themselves at colony perimeter points during foraging activity, exhibiting vigilance behavior — elevated posture, head scanning, alarm vocalizations when humans approach. This is not unusual for some rodent species. It is unusual for urban brown rats at this level of consistency and coordination.

Pest control technician Dae-Ho Park, who has serviced Shelf communities for twenty years, submitted a personal observation that the Board includes here with his permission. On the night of November 3, 2224, Park observed a large adult rat positioned at the base of a propped-open service door in Block 7's recycling facility. As Park watched from a concealed position, a line of approximately fifteen rats passed through the doorway in single file. The door-holding rat remained in position until the last animal passed through, then followed. Park states: "I've been killing rats for twenty years. I've never seen that before. I don't want to see it again."`,
    related_entities: ["Meridian 88", "Shelf", "Block 7"],
    credibility: "verified",
    story_hooks: [
      "Are the rats' enhanced intelligence related to geneware contamination, or something else entirely?",
      "A character needs information about a building's security patterns — the rats already know",
      "The rat colony's behavior escalates: they begin interfering with infrastructure"
    ],
    tags: ["document", "community_report", "urban_wildlife", "rat", "intelligence", "contamination", "ecology", "meridian_88", "shelf"]
  },
  {
    name: "Feral Cats of the Circuit District",
    document_type: "field_guide",
    author: "Lian Vasquez-Keita, Independent Naturalist",
    date: "2223-09-01",
    classification: "public",
    description: `The feral cat population of Meridian 88's Circuit District numbers approximately four thousand individuals, distributed across an estimated three hundred loosely territorial colonies. They are, in most respects, ordinary domestic cats — Felis catus, the same companion species that has cohabited with humans for ten thousand years. They hunt, they sleep, they fight, they breed. They are cats. But their eyes glow.

The bioluminescent modification originated with a cosmetic geneware product marketed in the early 2180s under the brand name "Starlight Eyes," manufactured by a now-defunct subsidiary of NovaPharma. The product was intended for domestic cats — a simple modification that caused the tapetum lucidum, the reflective layer behind the retina that gives cats their natural eyeshine, to emit a soft bioluminescence instead of merely reflecting ambient light. The product was popular for approximately three years before reports emerged that modified cats were passing the trait to offspring. NovaPharma quietly discontinued the product line. By then, enough modified cats had been abandoned or escaped to establish breeding populations throughout the Shelf.

Three color variants have been documented. The most common produces a green bioluminescence, visible in low light as a steady, soft glow emanating from the cat's eyes — not the brief flash of reflected light that all cats exhibit, but a sustained emission. The second variant, found in approximately 15% of the population, produces an amber glow. The third and rarest variant, found in less than 3% of observed individuals, produces a blue luminescence of striking intensity. Blue-eyed ferals are considered lucky by Circuit residents, and sighting one is treated as an auspicious event. The cats are indifferent to this designation.

The modification has had unintended functional consequences. The bioluminescent tapetum appears to have expanded the cats' visual sensitivity into near-ultraviolet wavelengths. Circuit ferals have been observed tracking prey — primarily augmented cockroaches — in conditions where no visible light is present, suggesting they are perceiving reflected UV or detecting the faint electromagnetic emissions of the cockroaches' metallic exoskeletons. They are seeing the world in spectra their ancestors could not access. They are, through an accident of cosmetic vanity and environmental contamination, better predators than any cat that has ever lived.

Most intriguingly, the Circuit ferals have developed vocalizations outside the normal feline range. Standard domestic cats vocalize between 25 Hz and 1,520 Hz. Circuit ferals have been recorded producing calls at frequencies up to 3,400 Hz, and infrasonic rumbles as low as 12 Hz. These vocalizations appear to serve communicative functions across distances that exceed normal feline social range — colony-to-colony signaling across multiple city blocks. The content of these communications, if they are communications, remains undeciphered. The cats are talking to each other. We do not know what they are saying.`,
    related_entities: ["Meridian 88", "Circuit District", "NovaPharma", "Shelf"],
    credibility: "verified",
    story_hooks: [
      "A character follows a blue-eyed cat to find something hidden that no human could see",
      "The cats' cross-colony communication network could be used — or disrupted — for strategic purposes",
      "What are the cats saying to each other, and does it matter?"
    ],
    tags: ["document", "field_guide", "urban_wildlife", "cat", "feral", "bioluminescence", "geneware", "contamination", "ecology", "meridian_88", "circuit_district"]
  },
  {
    name: "The Dogs That Remain",
    document_type: "essay",
    author: "Imani Delacroix-Watanabe",
    date: "2224-11-20",
    classification: "public",
    description: `There are maybe six hundred dogs in the Shelf. Nobody keeps an exact count. You know where they are because you can hear them — a bark carries farther than a voice in the concrete canyons of the lower tiers, and a dog's bark is one of the few sounds in Meridian 88 that hasn't been synthesized, optimized, or replaced by something more efficient. It is an analog sound in a digital world, and it cuts through the noise like a knife through static.

Keeping a dog in the Shelf is an act of irrational devotion. Dogs eat real food — not printed protein, not nutrient paste, but actual food with actual caloric content that costs actual Φ. They need medical care from one of the Shelf's three practicing veterinarians, whose fees are high because their skills are rare. They cannot be meaningfully augmented — canine neurology rejects most BCI interfaces, and the few that have been developed are widely regarded as cruel. A dog cannot work a job. A dog cannot be networked. A dog cannot contribute to a household's economic output in any measurable way. In a world that has optimized nearly every living thing for utility, a dog is the most useless creature you can own.

They are also the most loved. Dog ownership in the Shelf is rarely individual. A family acquires a dog — usually a mixed breed from one of the informal breeding networks that operate in the outer districts — and the dog becomes the neighborhood's dog. It sleeps in one home but eats in several. Children from multiple families walk it. It knows every resident on its block by scent. It greets the mail carrier, the food printer technician, the community board representative. It is the social infrastructure that no one planned and everyone relies on.

When a Shelf dog dies, the mourning is public and communal. There are no formal rituals — the Shelf invents its own ceremonies for everything — but the pattern is consistent. The dog's collar is hung on the family's door. Neighbors bring food. Children draw pictures. Someone, always someone, tells the story of the dumbest thing the dog ever did, and everyone laughs, and then everyone cries. The dog's name is added to an informal registry maintained by the Shelf community boards — a list of every dog that has lived and died in the Shelf since anyone started keeping track. The list goes back to 2188. It contains over four thousand names.

There is something unbearable about loving a thing that cannot be backed up, cannot be resleeved, cannot be restored from a checkpoint. A dog is a twenty-year commitment to inevitable loss, in a world that has tried very hard to make loss optional. The people who keep dogs in the Shelf are not sentimental. They are the hardest, most practical people in Meridian 88. They keep dogs because they understand something that the upper tiers have forgotten: that the point of loving something mortal is that it ends. That the grief is not a bug. It is the entire architecture.`,
    related_entities: ["Meridian 88", "Shelf"],
    credibility: "verified",
    story_hooks: [
      "A neighborhood dog goes missing — the entire block mobilizes to find it",
      "The dog registry contains the social history of the Shelf in miniature",
      "A character from the upper tiers encounters a Shelf dog and is undone by the simplicity of it"
    ],
    tags: ["document", "essay", "urban_wildlife", "dog", "shelf", "community", "mortality", "ecology", "meridian_88"]
  },
  {
    name: "Cockroaches: The Augmented Survivors",
    document_type: "scientific_paper",
    author: "Dr. Sable Okonkwo-Reeves, GLMZ Entomological Research Division",
    date: "2225-01-15",
    classification: "public",
    description: `Abstract: This paper documents the ongoing morphological and behavioral evolution of the German cockroach (Blattella germanica) and American cockroach (Periplaneta americana) populations within the Meridian 88 metropolitan zone, with specific focus on exoskeletal metallization and electromagnetic behavioral responses observed over the period 2218–2225. Findings indicate adaptive trajectories that exceed projected evolutionary timelines by several orders of magnitude, suggesting external mutagenic influence consistent with environmental geneware contamination.

The pesticide resistance profile of Meridian 88's cockroach populations is, by any standard metric, comprehensive. Laboratory testing of captured specimens against the full catalog of registered commercial and industrial pesticides — 847 compounds as of 2224 — yielded a resistance rate of 99.6%. The three compounds that produced mortality in test populations were prototype agents not yet commercially available. Field deployment of these prototype agents in controlled test areas produced effective mortality rates for approximately eleven days, after which resistance was observed in surviving populations. The cockroaches of Meridian 88 are not merely resistant to pesticides. They are, in the assessment of this division, functionally immune to chemical pest control as a strategy.

More significant is the exoskeletal metallization phenomenon first documented in 2218. Specimens collected from the Circuit District and adjacent industrial zones exhibit exoskeletons that incorporate trace metals — primarily copper, tin, and aluminum — scavenged from electronic waste. The metals are not surface contaminants. They are structurally integrated into the chitin matrix of the exoskeleton during molting, producing a composite material that is measurably harder and more conductive than standard cockroach cuticle. The mechanism of incorporation is not fully understood, but appears to involve modified digestive enzymes that extract and transport metallic compounds from ingested electronic waste into the hemolymph, where they are deposited during exoskeletal formation. The cockroaches are, in a literal sense, building themselves out of the city's garbage.

Behavioral observations reveal an additional anomaly: electromagnetic sensitivity. Test populations exposed to controlled BCI frequency emissions exhibit consistent avoidance behavior at frequencies between 2.1 and 2.4 GHz — the primary operating range of standard civilian neural interfaces. The cockroaches do not merely avoid BCI transmitters; they navigate around BCI signal fields with a precision that suggests active sensing rather than passive irritation. In field conditions, this manifests as cockroach populations that are conspicuously absent from areas with high BCI signal density and concentrated in signal shadows and dead zones. They have mapped the city's electromagnetic topology and arranged themselves within it.

The implications are difficult to overstate. The cockroaches of Meridian 88 are chemically invulnerable, structurally reinforced with salvaged metals, and electromagnetically aware. They occupy a niche that did not exist fifty years ago — an organism adapted not to a natural environment but to the specific technological and chemical landscape of a 23rd-century megacity. Dr. Okonkwo-Reeves describes them as "pre-adapted to a world that hasn't arrived yet." The division recommends continued observation and a cessation of eradication attempts, which are both futile and, at this point, arguably disrespectful.`,
    related_entities: ["Meridian 88", "Circuit District", "GLMZ"],
    credibility: "verified",
    story_hooks: [
      "Cockroach distribution maps reveal hidden BCI dead zones — what's in those dead zones?",
      "The metallic exoskeletons are conductive: could cockroaches be used as improvised circuitry?",
      "A new pesticide that actually works raises the question of what happens when the cockroach ecosystem collapses"
    ],
    tags: ["document", "scientific_paper", "urban_wildlife", "cockroach", "metallization", "geneware", "contamination", "ecology", "meridian_88", "circuit_district"]
  },
  {
    name: "The Coyote Comeback",
    document_type: "wildlife_report",
    author: "GLMZ Outer District Wildlife Management",
    date: "2224-08-22",
    classification: "restricted",
    description: `Coyotes (Canis latrans) have re-established breeding populations in the outer districts of Meridian 88, following an absence of approximately forty years from the metropolitan zone. The first confirmed sighting occurred in 2221, when a surveillance camera in the Tier 1 industrial buffer zone captured footage of a single adult female moving along a drainage channel at 0340 hours. Since then, Wildlife Management has confirmed the presence of at least seven distinct packs operating in the outer districts, with an estimated total population of sixty to eighty individuals.

The animals are larger than historical Great Lakes coyote populations. Adult males average 24 kilograms, compared to the historical average of 14–18 kilograms. Females average 19 kilograms. Genetic sampling from scat and hair indicates hybridization with wolf and domestic dog lineages, consistent with the "coywolf" hybrid populations that dominated the Great Lakes region in the late 21st century — but with additional genetic markers that do not match any catalogued canid genome. The source of these novel genes has not been determined. The animals may have been exposed to geneware contamination in the wasteland water table, or they may have inherited modifications from domesticated ancestors. Or they may have acquired them through a mechanism we have not yet identified.

Their hunting behavior is what prompted the classification of this report as restricted. Pack coordination is expected in canids. What is not expected is the level of tactical sophistication these animals demonstrate. Pack 3, operating in the western industrial zone, has been observed using a rotating pursuit strategy in which individuals take turns as the primary pursuer, maintaining chase pressure on prey without individual exhaustion — a technique documented in African wild dogs but never previously observed in North American canids. Pack 5, in the eastern buffer zone, uses ambush tactics: two or three individuals drive prey toward concealed pack members positioned along a predicted escape route. This requires anticipation of prey behavior, spatial coordination, and patience. It requires, in the assessment of this division, planning.

Most concerning: the coyotes avoid surveillance cameras. Not buildings, not lights, not human activity — cameras specifically. Pack movements consistently route around known camera positions. When cameras are relocated, the coyotes adjust their routes within 48 hours. They have also learned to exploit automaton patrol patterns. Automated security units in the outer districts follow predictable patrol routes with predictable timing. Pack 7 has been observed timing its movements to pass through areas during the gap between patrol sweeps. On one documented occasion, a single coyote from Pack 7 triggered a patrol unit's motion sensor, drawing it away from the pack's intended crossing point while the rest of the pack moved through undetected. Wildlife Management's official term for this is "diversionary behavior." The unofficial term, used by the field team lead in her incident report, is "that thing used a decoy."

This division recommends against lethal control measures at this time. The coyote population is providing a degree of pest management in the outer districts — they prey on feral dogs, rats, and other pest species — and their removal would create ecological vacancies with unpredictable consequences. Additionally, and the division acknowledges this is not a scientific consideration but a practical one: these animals are demonstrating a capacity for behavioral adaptation that suggests lethal control would be temporary at best. They would learn. They would adapt. And we would be in an arms race with an adversary that has already demonstrated it can outthink our automated systems.`,
    related_entities: ["Meridian 88", "GLMZ", "Tier 1"],
    credibility: "verified",
    story_hooks: [
      "A character in the outer districts realizes the coyotes are watching them — and recognizing them",
      "Pack 7's decoy tactics could be useful to someone who needs to bypass automated security",
      "The novel genes in the coyote population — where did they really come from?"
    ],
    tags: ["document", "wildlife_report", "urban_wildlife", "coyote", "intelligence", "ecology", "meridian_88", "outer_districts", "restricted"]
  },
  {
    name: "Birds of the Spires",
    document_type: "essay",
    author: "Nadia Osei-Lund, Architecture & Nature Quarterly",
    date: "2224-03-10",
    classification: "public",
    description: `Six hundred meters above the Shelf, where the air is thin enough to taste like metal and the wind carries the hum of atmospheric processors, peregrine falcons nest on the spires of Tier 5. They have been there for as long as anyone has kept records — since the first arcology towers rose high enough to mimic the cliff faces that the species evolved to hunt from. The peregrines of Meridian 88 are the fastest animals in the city. In a stoop, they exceed 380 kilometers per hour. They are small, violent missiles of bone and feather, and they are the last purely wild predators in the metropolitan zone.

The falcons carry no geneware contamination. They drink rainwater collected on the upper spires, above the altitude where most atmospheric pollutants concentrate. They eat pigeons — the same bioluminescent pigeons that glow blue-green in the lower districts — but the geneware modifications in the pigeon tissue do not survive the falcon's digestive process. The contamination is, in the language of wildlife biology, "non-transmissible through predation." The falcons eat the glowing birds and remain unglowed. They are clean. They are original. In a city where nearly every organism has been touched by accidental modification, the peregrines are what everything else used to be.

Tier 5 residents have adopted them with the fervor of people who can afford to be sentimental about nature. The Meridian Raptor Conservancy, funded by a consortium of Tier 5 property owners, maintains nesting platforms, monitors breeding pairs, and employs a full-time falconer whose job is to ensure the birds are undisturbed during nesting season. Individual falcons are named. Their hunting successes are tracked on a public feed. A mated pair nesting on the Nakamura-Singh Tower has a dedicated camera stream with forty thousand regular viewers. The female, named Sovereign by a public vote, laid four eggs this spring. Three hatched. The feed's comment section was ecstatic.

The irony is architectural. The peregrine falcon — the last unmodified apex predator in Meridian 88 — lives exclusively on the most artificial structures ever built. The Tier 5 spires are engineered environments, temperature-controlled, atmospherically managed, designed to specifications that leave nothing to chance. The falcons nest in human-built eyries on human-built towers in human-conditioned air, and they are the most natural things in the city. They are wild in a space that has no wildness in it. They hunt contaminated prey from pristine perches. They are the purest expression of nature in Meridian 88, and they exist entirely because of artifice.

Nobody finds this ironic. Or rather, everyone finds it ironic, but nobody finds it surprising. In Meridian 88, purity is always a product of privilege. The cleanest water is on Tier 5. The cleanest air is on Tier 5. The cleanest animals are on Tier 5. This is not a metaphor. It is a budget line item. The falcons are wild because someone pays for them to be wild. Below, in the Shelf, the pigeons glow, the rats organize, the cats speak in frequencies no one can hear, and nature does what it has always done in the absence of funding: it improvises.`,
    related_entities: ["Meridian 88", "Tier 5", "Nakamura-Singh Tower", "Meridian Raptor Conservancy"],
    credibility: "verified",
    story_hooks: [
      "A falcon dies and the autopsy reveals it was not, in fact, uncontaminated — someone has been feeding them modified prey",
      "The falcon camera feed captures something it wasn't supposed to — activity on a Tier 5 terrace",
      "Tier purity as ecological metaphor: a character from the Shelf visits Tier 5 and sees the falcons"
    ],
    tags: ["document", "essay", "urban_wildlife", "falcon", "peregrine", "tier_5", "ecology", "meridian_88", "purity", "class"]
  },
  {
    name: "The Lake Fish Problem",
    document_type: "environmental_report",
    author: "Old Harbor Fisheries Cooperative & GLMZ Aquatic Biology Division",
    date: "2225-03-01",
    classification: "public",
    description: `Lake Michigan's fish population collapsed in 2187. The collapse was not sudden — it was the culmination of two centuries of industrial contamination, thermal pollution from power generation, invasive species proliferation, and the cumulative effect of geneware runoff from the metropolitan zones that ring the lake's southern shore. By 2187, commercial fishing yields had declined to less than 4% of 2050 levels. The lake was, in the assessment of GLMZ Aquatic Biology, "functionally depauperate" — alive, but barely. The fishing communities of Old Harbor, which had sustained themselves on lake fish for generations, turned to aquaculture and printed protein. The lake was written off.

It came back. Between 2195 and 2210, fish populations in the southern basin of Lake Michigan recovered to approximately 60% of pre-collapse biomass. The recovery was rapid, unexpected, and — from a taxonomic standpoint — deeply confusing. The species that returned were not the species that had declined. Lake trout, walleye, perch, and whitefish — the historical species assemblage — did not recover. Instead, the lake filled with organisms that do not match any species in the Great Lakes biological record. They are fish. They have fins, scales, gills, and the general morphology of freshwater teleosts. But they are not any fish that has existed before.

Genetic analysis conducted by the GLMZ Aquatic Biology Division over the period 2210–2224 has produced results that the division's lead geneticist, Dr. Amara Nwosu-Lindqvist, describes as "creative." The new fish appear to be hybrids of multiple native species — combinations that are reproductively impossible under normal biological conditions. Specimens have been identified with genetic material from lake trout and smallmouth bass, from walleye and alewife, from species that occupy different ecological niches and could not naturally interbreed. The hybridization events appear to have been mediated by geneware agents in the lake water — gene transfer vectors originally designed for human therapeutic applications that, in the lake's contaminated environment, acted as indiscriminate genetic shufflers, combining available genetic material into novel configurations.

The fish taste fine. The Old Harbor fishing cooperative resumed commercial operations in 2212, initially with considerable trepidation. Toxicological testing of the new species found no harmful compounds at concentrations exceeding safety thresholds. Nutritional profiles are comparable to historical lake fish. The flesh is firm, mildly flavored, and takes well to smoking. Old Harbor sells fresh and smoked fish at the Circuit night market under the brand name "New Catch." The packaging does not mention the genetic anomalies. The customers do not ask.

The question that the Aquatic Biology Division cannot answer is whether the new species are stable. Fifteen years of monitoring suggests they are breeding true — offspring resemble parents, populations are self-sustaining, ecological niches are being filled. But the geneware agents that created them are still in the water. New combinations continue to appear. Last year, a fisherman in Old Harbor pulled up a specimen with bioluminescent lateral lines and what appeared to be rudimentary electric organs — structures found in no freshwater fish native to North America. The specimen was delivered to the division for analysis. The results are pending. The fisherman says it tasted like walleye.`,
    related_entities: ["Meridian 88", "Lake Michigan", "Old Harbor", "GLMZ"],
    credibility: "verified",
    story_hooks: [
      "The geneware agents in the lake are still active — what happens when they produce something that isn't a fish?",
      "Old Harbor's economy depends on selling fish that technically shouldn't exist — what happens if someone publicizes the genetics?",
      "The specimen with electric organs — is something in the lake deliberately engineering new species?"
    ],
    tags: ["document", "environmental_report", "urban_wildlife", "fish", "lake_michigan", "geneware", "contamination", "ecology", "meridian_88", "old_harbor"]
  },
  {
    name: "Urban Foxes and the Garbage Intelligence",
    document_type: "behavioral_study",
    author: "Dr. Kofi Strand-Nakamura, GLMZ Urban Ecology Lab",
    date: "2224-07-15",
    classification: "public",
    description: `The red fox (Vulpes vulpes) population of Meridian 88's mid-tier districts has been the subject of ongoing behavioral study since 2219, when waste management reports first noted a statistically significant increase in automated waste bin tampering events. The bins in question — Cleanflow Model 7 units, deployed across Tier 2 and Tier 3 districts — feature a latch mechanism requiring a specific grip-and-lift action to open. The mechanism was designed to prevent access by pest animals. It prevents access by every pest animal except foxes.

Initial observations suggested trial-and-error learning: individual foxes interacting with bin mechanisms until discovering a successful opening technique. This is unremarkable — foxes are well-documented problem solvers, and bin-raiding is a behavior observed in urban fox populations worldwide for over two centuries. What is remarkable is what the surveillance footage actually shows. In seventeen of the twenty-three documented initial bin-opening events, the fox did not discover the technique through experimentation. It observed a human operating the bin, from a concealed position, and then replicated the human's action. The fox watched a person open a bin, waited for the person to leave, approached the bin, and performed the same grip-and-lift sequence. Not an approximation. The same sequence.

The observational learning alone would be noteworthy. The teaching behavior is unprecedented. Adult foxes that have acquired bin-opening techniques have been filmed demonstrating these techniques to juvenile foxes — their cubs and, in two documented cases, unrelated juveniles from adjacent territories. The teaching follows a consistent pattern: the adult approaches the bin, performs the opening action slowly, allows the juvenile to observe, then steps back while the juvenile attempts the action. Failed attempts are followed by the adult repeating the demonstration. Successful attempts are followed by shared access to the bin's contents. This is not instinctive behavior. This is pedagogy.

Furthermore, different fox family groups have specialized in different bin models. The foxes in the Block 12 territory exclusively raid Cleanflow Model 7 units. The family group operating near the Circuit night market has mastered the WasteSafe Pro units, which use a foot-pedal mechanism. A family in the Tier 2 residential zone opens BioBin units, which require a two-step latch-and-slide action. Each family group's technique is distinct, and each is transmitted vertically — from parent to offspring — and occasionally horizontally between cooperating adults. The foxes have not developed one universal bin-opening behavior. They have developed multiple specialized techniques, each culturally transmitted within a specific lineage.

Dr. Strand-Nakamura's lab has begun referring to this phenomenon as "garbage intelligence" — a term that is intended to be descriptive rather than flippant, though the lab acknowledges it is also flippant. The foxes of Meridian 88 are not merely adapting to an urban environment. They are developing and transmitting cultural knowledge — specific, learned, non-genetic behavioral traditions that vary between populations and are passed through social learning rather than inheritance. This is, by any behavioral ecology standard, culture. It is culture about garbage, but it is culture nonetheless.`,
    related_entities: ["Meridian 88", "GLMZ", "Circuit District", "Tier 2", "Tier 3"],
    credibility: "verified",
    story_hooks: [
      "A fox family that has learned to open a specific secure container — what else can they open?",
      "The foxes' observational learning could be exploited: teach a fox to open a door, a panel, a latch",
      "Cultural transmission in foxes raises questions about what other urban animals are teaching each other"
    ],
    tags: ["document", "behavioral_study", "urban_wildlife", "fox", "intelligence", "culture", "ecology", "meridian_88"]
  },
  {
    name: "The Moth Migration",
    document_type: "naturalist_observation",
    author: "Esme Gowda-Larsen, Shelf Naturalist Collective",
    date: "2224-10-31",
    classification: "public",
    description: `Every autumn, between the second week of October and the first week of November, they come. Hundreds of thousands of moths — a species tentatively classified as Manduca meridiana, though its relationship to known sphinx moths is disputed — descend on Meridian 88 in a migration event that has no historical precedent and no satisfying biological explanation. They arrive from the south, from the direction of the wasteland, in swarms dense enough to trigger atmospheric particle alerts in the lower districts. For three weeks, the air in the Shelf tastes like dust and wings.

Manduca meridiana should not exist in the Great Lakes region. Sphinx moths of the Manduca genus are subtropical and tropical organisms. Their larval host plants do not grow in the Great Lakes Metropolitan Zone. Their thermal tolerance range, based on physiological analysis of captured specimens, should preclude survival in the October temperatures of the upper Midwest. And yet they come, every year, in numbers that suggest a breeding population of millions somewhere to the south — a population that no survey has located, in a landscape that should not support it. They come from somewhere. Nobody knows where.

They are drawn to BCI emissions. This has been confirmed through controlled experiments by the Shelf Naturalist Collective and independently by GLMZ entomologists. Moths exposed to simulated BCI signal fields orient toward the signal source with the same reliability that their ancestors oriented toward light. In the field, this manifests as swarm behavior that concentrates in high-signal-density areas — the moths are thickest where BCI usage is heaviest. They land on relay nodes. They cluster on the walls of signal processing facilities. And they land on people's heads.

Specifically, they land on or near the neural port. Anyone walking through a moth swarm during peak migration will find moths alighting on their head, their neck, the area around the BCI interface. The moths settle there and remain, wings folded, antennae extended toward the port. It feels, according to consistent testimony from dozens of residents, like being chosen. Like something ancient and nonverbal has recognized you. Like the moth knows you are there, specifically you, and has decided to rest on you, specifically on the place where your biology meets your technology. It is electromagnetic attraction. The moth is sensing the signal emissions from your neural hardware and orienting toward the strongest source. It is not mystical. It is not meaningful. It is physics.

But it doesn't feel like physics. Every autumn, during the migration, the Shelf becomes briefly reverent. People walk slowly through moth-thick air with insects resting on their shoulders, their hair, their outstretched hands. Children collect them gently. Night market vendors sell moth-themed food and art. For three weeks, the Shelf — pragmatic, hard-edged, unsentimental — treats a swarm of insects with a tenderness that borders on worship. Then the moths leave, as suddenly as they came, and the Shelf goes back to being the Shelf. Nobody talks about the moths until next year. And next year, when they come again, the tenderness returns, as reliable as the migration itself. It is probably just electromagnetic attraction. Probably.`,
    related_entities: ["Meridian 88", "Shelf", "GLMZ"],
    credibility: "verified",
    story_hooks: [
      "A moth lands on someone who doesn't have a neural port — what is it sensing?",
      "The moths come from the south, from the wasteland — what else is down there?",
      "During peak migration, BCI signals in the Shelf experience subtle interference — the moths are absorbing signal"
    ],
    tags: ["document", "naturalist_observation", "urban_wildlife", "moth", "migration", "bci", "ecology", "meridian_88", "shelf", "atmospheric"]
  },
  {
    name: "What's Living in the Underworld?",
    document_type: "incident_compilation",
    author: "Various (compiled by Meridian 88 Infrastructure Division)",
    date: "2224-12-01",
    classification: "restricted",
    description: `The following is a compilation of sighting reports, incident logs, and worker testimony from the Underworld — the subterranean infrastructure levels beneath Meridian 88, comprising abandoned transit tunnels, flooded utility corridors, decommissioned sublevel foundations, and unmapped spaces of uncertain origin. Reports span the period 2215–2224 and are presented in the order received. The Infrastructure Division notes that the Underworld is poorly surveyed, inconsistently monitored, and contains areas that have not been accessed by authorized personnel in decades. The reliability of individual reports varies. The consistency of reports across independent sources is noted.

Blind fish. These are the most frequently reported organisms. Tunnel maintenance crews working in the partially flooded Level -3 corridors beneath the Circuit District have reported small, pale, eyeless fish in standing water pools since at least 2215. Specimens collected in 2219 were identified as a cave-adapted morph of the common killifish (Fundulus heteroclitus), displaying complete eye regression and depigmentation consistent with prolonged subterranean habitation — likely many generations. The fish are unremarkable except for their existence, which implies a self-sustaining aquatic ecosystem in the flooded Underworld levels that no one has surveyed.

Albino rats. Workers and Underworld residents consistently report rat populations in the deeper levels that are significantly larger than surface populations, fully depigmented, and — according to multiple independent accounts — coordinated in ways that exceed the already-anomalous behavior documented in surface rat populations. An urban explorer operating under the handle "Depthwalker" posted a widely viewed recording in 2222 showing a group of approximately thirty albino rats moving in formation through a Level -5 corridor, stopping at intersections, and apparently responding to vocalizations from unseen animals deeper in the tunnel system. The recording has not been independently verified but is consistent with other accounts.

Luminescent fungus. Multiple reports describe fungal growths in the deeper levels that emit light when disturbed — a faint blue-white glow that intensifies with physical contact or vibration. Specimens have not been successfully collected, as the fungus degrades rapidly when removed from its substrate. The luminescence appears to be a stress response, possibly serving as a warning signal. To what, and from what, is unknown.

The tracks. Beginning in 2220, work crews in Level -7 and below have reported finding tracks in the silt and sediment of deep flooded corridors. The tracks are described consistently: four-toed, approximately 20 centimeters in length, with deep impressions suggesting an animal massing 80–100 kilograms. They do not match the track morphology of any animal known to inhabit the Great Lakes region — or, when submitted to the GLMZ zoological database for matching, any animal in the database at all. The tracks are always found in flooded corridors, always heading deeper into the tunnel system, and never accompanied by return tracks heading back toward the surface.

The breathing. This is the report the Infrastructure Division is least comfortable including, but it appears with sufficient frequency and consistency to warrant documentation. Workers in the deepest accessible levels of the Underworld — Level -8 and below — report hearing, in the silence between their own movements, the sound of breathing. Not mechanical ventilation. Not water flow. Breathing. Rhythmic inhalation and exhalation, slow and deep, emanating from deeper in the tunnel system. The sound suggests a large animal — significantly larger than anything that should be able to enter or survive in the tunnel infrastructure. No visual confirmation has been obtained. No source has been identified. The sound is always described the same way: too large. Whatever is breathing in the deep Underworld is too large to be in a tunnel. And yet the sound is there.`,
    related_entities: ["Meridian 88", "Underworld", "Circuit District"],
    credibility: "mixed",
    story_hooks: [
      "Someone needs to go deep enough to find out what's making the tracks",
      "The blind fish ecosystem implies flowing water and food sources — mapping it maps the Underworld",
      "The breathing — is it biological, mechanical, or something else entirely?"
    ],
    tags: ["document", "incident_compilation", "urban_wildlife", "underworld", "subterranean", "mystery", "ecology", "meridian_88", "restricted", "new_weird"]
  },
  {
    name: "The Beehive Collapse and Recovery",
    document_type: "apicultural_report",
    author: "Meridian 88 Urban Apiary Collective",
    date: "2224-05-18",
    classification: "public",
    description: `The bees left, and then they came back, and they were different. This is the short version. The long version takes seventy years and raises more questions than it answers.

Colony collapse in the Great Lakes Metropolitan Zone reached terminal velocity around 2120. By 2125, wild honeybee populations were functionally extinct in the region. Managed colonies survived in controlled environments — sealed apiaries with filtered air and curated forage — but wild pollination ceased. The ecological consequences were severe and well-documented: crop failures in the agricultural zones, wildflower meadow die-offs in the wasteland margins, cascading declines in insectivorous bird populations. The bees were gone, and everything that depended on the bees began to fail.

Recovery began in 2186, when apiarists in the outer districts of Meridian 88 reported wild swarms establishing colonies in structural voids, abandoned HVAC systems, and the walls of derelict buildings. By 2200, wild colonies were common enough to map. By 2210, they were abundant. By 2224, the Meridian 88 Urban Apiary Collective estimates a wild honeybee population in the metropolitan zone of approximately 50 million individuals distributed across 2,000–3,000 colonies. The recovery is, by any measure, remarkable. The bees that recovered are, by any measure, not the bees that collapsed.

The new hive architecture is the most visually striking difference. Historical honeybee comb follows a regular hexagonal pattern — the most efficient tessellation for maximizing storage volume relative to wax expenditure. The new bees build comb that incorporates hexagonal cells but also pentagonal, heptagonal, and irregularly shaped cells in patterns that are geometrically complex and, according to structural analysis by the GLMZ Engineering Division, mechanically superior to standard hexagonal comb. The comb is stronger. It distributes load more efficiently. It contains internal channels that appear to serve ventilation and thermal regulation functions that standard comb does not. The bees have, through whatever process drives their construction behavior, invented a better architecture. How they arrived at it is unknown. Bees do not iterate. Bees do not prototype. Bees build what their genetics tell them to build. These bees are building something their genetics should not know.

The waggle dance has new movements. Honeybee communication through dance is one of the most studied phenomena in behavioral biology — the vocabulary is well-mapped, the grammar well-understood. The new bees perform the standard dance repertoire and also produce movement patterns that apiarists cannot decode. The new dances are consistent — the same patterns appear across multiple colonies — which argues against random variation. They are communicating something. We do not know what.

And the honey is wrong. Not wrong in the sense of harmful — toxicological testing consistently clears it for consumption, and it is, by all accounts, excellent honey. But spectroscopic analysis reveals trace compounds — complex organic molecules — that do not correspond to any plant species growing within the bees' documented foraging range. The bees are visiting flowers. Some of those flowers do not appear to exist in any botanical survey of the region. Where are the bees going? The Apiary Collective has attempted to track foraging scouts using miniaturized GPS transponders. The transponders lose signal within 3 kilometers of the hive, in a direction that leads toward the wasteland. The scouts return. The transponders do not resume transmitting until the scouts are back within range. Whatever is out there, wherever the bees are going, is in a dead zone. They go where we cannot follow, and they come back with honey that shouldn't exist.`,
    related_entities: ["Meridian 88", "GLMZ"],
    credibility: "verified",
    story_hooks: [
      "The bees' foraging destination in the wasteland — what's growing there?",
      "The new dance patterns could be decoded — and might reveal something about the wasteland",
      "The superior hive architecture attracts the attention of structural engineers — or corponations looking for biomimetic designs"
    ],
    tags: ["document", "apicultural_report", "urban_wildlife", "bee", "hive", "architecture", "ecology", "meridian_88", "wasteland", "mystery"]
  },
  {
    name: "Geese: Unchanged and Furious",
    document_type: "wildlife_bulletin",
    author: "GLMZ Urban Wildlife Advisory",
    date: "2224-04-01",
    classification: "public",
    description: `The Canada goose (Branta canadensis) is the only species in the Meridian 88 metropolitan zone that shows no measurable genetic, behavioral, or morphological change from baseline populations documented in the early 21st century. This is, from a biological standpoint, extraordinary. Every other vertebrate species in the urban environment has been affected — to greater or lesser degree — by the geneware contamination that pervades the regional water table, the atmospheric chemical load of the industrial zones, the electromagnetic saturation of BCI infrastructure, or the cascading secondary effects of all three. The geese are unchanged. They are exactly as aggressive, exactly as territorial, and exactly as utterly unafraid of anything as they were two hundred years ago.

This stability is statistically improbable. The GLMZ genetics lab has sequenced the genomes of Meridian 88's resident goose population across three generations and found no novel insertions, no geneware markers, no evidence of environmental mutagenesis. The geese drink the same contaminated water as the bioluminescent pigeons. They eat from the same grounds as the enhanced rats. They share the lakefront with the hybridized fish. And they are unmodified. The working hypothesis — and the genetics lab emphasizes that this is a hypothesis, not a conclusion — is that the Canada goose genome possesses an unusual degree of resistance to exogenous gene insertion. The mechanism is not understood.

In terms of behavior, the geese continue to exhibit the characteristics that have defined the species' relationship with human civilization since long before Meridian 88 existed. They occupy public spaces with proprietary confidence. They hiss at pedestrians, cyclists, automated vehicles, maintenance drones, and security automata with equal conviction. They nest in inconvenient locations and defend those nests with physical violence against anything that approaches, regardless of the approacher's size, capability, or legal authority. A Tier 3 security automaton was filmed retreating from a nesting goose in the Lakeshore Greenway in 2223. The video has been viewed nine million times.

They are, in the considered assessment of this division, magnificent. They are the one organism in Meridian 88 that treats corponation executives, augmented mercenaries, and military-grade automata with identical contempt. They do not recognize hierarchy. They do not acknowledge threat. They have decided that every space they occupy belongs to them, and two hundred years of technological advancement has not produced a force capable of convincingly arguing otherwise.

The GLMZ Urban Wildlife Advisory issues its annual reminder that Canada geese are a protected species under Great Lakes Metropolitan Zone wildlife statutes, that interfering with nesting geese carries a fine of Φ500, and that the advisory takes no responsibility for injuries sustained by individuals who approach nesting geese despite posted warnings. The geese do not read the warnings either. But in their case, this is because they are geese, not because they are negligent. The advisory wishes to make this distinction clear.`,
    related_entities: ["Meridian 88", "GLMZ", "Lakeshore Greenway"],
    credibility: "verified",
    story_hooks: [
      "Why are the geese immune to geneware contamination? Someone wants to find out — for medical or military applications",
      "A critical scene is interrupted by nesting geese and everything goes sideways",
      "The goose genome's resistance to modification could be the key to a geneware antidote"
    ],
    tags: ["document", "wildlife_bulletin", "urban_wildlife", "goose", "unchanged", "humor", "ecology", "meridian_88"]
  },
  {
    name: "The Spider Webs of the Circuit",
    document_type: "research_note",
    author: "Dr. Pei-Shan Okafor-Müller, GLMZ Materials Science Division",
    date: "2224-09-05",
    classification: "public",
    description: `The orb-weaving spiders of the Circuit District — primarily Araneus meridianus, a locally adapted variant of the common garden spider — construct webs that have attracted the attention of structural engineers, materials scientists, and electrical engineers for the better part of a decade. The webs are, at first glance, ordinary. They are built in the usual locations — doorways, window frames, gaps in infrastructure — and they follow the basic orb-web architecture that spiders have used for 100 million years. Radial threads, spiral capture threads, a hub. It is a web. But the geometry is wrong, in ways that are right.

Standard orb webs follow a logarithmic spiral pattern — a mathematically predictable structure that balances capture area against silk expenditure. Circuit spiders build webs that incorporate logarithmic spirals and also Fibonacci spirals, Archimedean spirals, and — in three independently documented specimens — patterns that correspond to no named mathematical spiral but that, when analyzed computationally, optimize for structural properties that no spider should need. Specifically: tensile load distribution across uneven anchor points. The webs are solving an engineering problem. The problem is: how do you build a structure that maintains integrity when the things it's attached to are vibrating at different frequencies? In the Circuit, where building surfaces vibrate with industrial machinery, data center cooling systems, and the constant low hum of power distribution, this is the dominant structural challenge. The spiders have solved it. They solved it better than the buildings did.

The conductive filament incorporation is the second anomaly. Circuit spiders integrate strands of conductive material — primarily copper and aluminum wire fragments, carbon fiber strands, and occasionally fiber-optic threads — into their web structures. The materials are scavenged from electronic waste, which is abundant in the Circuit. The integration is not random. Conductive filaments are consistently placed in radial threads rather than spiral threads, creating a spoke-pattern conductivity structure that, when the web is moist (from rain, fog, or the condensation that is ubiquitous in the Circuit's microclimate), conducts small amounts of electrical current. The current is tiny — microamps at most — but it is there. The webs glow during rain. Faintly, briefly, a blue-white flicker that traces the radial lines and dies as the moisture evaporates. It is beautiful. It is also, potentially, functional.

The question of intent is the third anomaly, and the one this division is least equipped to address. Are the spiders building conductive webs on purpose? The integration of conductive filaments is consistent across hundreds of observed webs. It is not a behavioral accident — the spiders select conductive materials preferentially over non-conductive materials of similar size and shape. They place them in structurally specific positions. But why? One hypothesis: the electrical conductivity enhances prey detection. A conductive web could sense the electromagnetic emissions of the augmented cockroaches that are the Circuit spiders' primary prey, alerting the spider to prey presence before physical contact. This would make the webs not just sticky traps but electromagnetic sensors. We have not confirmed this. We have also not ruled it out.

The structural engineering implications are the reason this division is involved. Dr. Okafor-Müller's team has been studying Circuit spider web geometry as a model for adaptive structural design in vibrationally complex environments — buildings, bridges, and infrastructure that must maintain integrity despite variable and unpredictable mechanical loads. The spiders, working with silk and scavenged wire, have produced solutions to problems that the division's computational models take weeks to approximate. We are learning from them. We are learning structural engineering from spiders. Nobody in this division expected to write that sentence.`,
    related_entities: ["Meridian 88", "Circuit District", "GLMZ"],
    credibility: "verified",
    story_hooks: [
      "A spider web in the Circuit captures a signal — not a bug, but an actual data transmission",
      "The spiders' structural solutions inspire a new building technique — or a new weapon",
      "A massive web complex in an abandoned section of the Circuit has grown large enough to be architecturally significant"
    ],
    tags: ["document", "research_note", "urban_wildlife", "spider", "circuit_district", "engineering", "ecology", "meridian_88", "conductive", "biomimicry"]
  },
  {
    name: "The Veterinarian's Casebook — 10 Years of Urban Wildlife",
    document_type: "medical_casebook",
    author: "Dr. Joaquín Moreau-Adeyemi, DVM",
    date: "2225-01-01",
    classification: "public",
    description: `I have practiced veterinary medicine in the Shelf for ten years. I treat pets — dogs, cats, the occasional bird or rodent — and I treat urban wildlife that comes to me injured, sick, or simply strange enough that someone thought a vet should look at it. What follows are selected cases from my files, presented with clinical detail and without the editorial commentary I save for my personal journal, because the cases speak for themselves.

Case 0047: Female domestic cat, approximately 4 years old, brought in by a Shelf resident who had been feeding her for two years. Presenting complaint: "She sounds like she has two heartbeats." She did have two heartbeats. Examination revealed two functional hearts — the original organ in the standard thoracic position, and a second, smaller but fully formed heart located posterior and slightly left of the original. Both hearts were beating in coordinated rhythm. The cat was asymptomatic. Both hearts were healthy. The second heart appeared to be a developmental duplicate triggered by a geneware modification — likely cosmetic in origin, possibly intended for a human recipient — that had integrated into feline developmental pathways. The cat had twice the cardiac redundancy of any natural feline. She was, cardiovascularly speaking, the most robust cat I have ever examined. I advised the owner to continue feeding her and try not to think about it.

Case 0112: Male rock dove (pigeon), found injured in Block 9. Wing fracture, standard treatment. During examination, feather analysis revealed copper integration in the rachis (feather shaft) structure — not surface contamination, but copper molecules incorporated into the keratin matrix during feather growth. The feathers had a faint metallic sheen visible under magnification. Conductivity testing confirmed the feathers were weakly conductive. The pigeon was also bioluminescent — standard blue-green breast feather presentation. The copper incorporation appeared to be independent of the bioluminescence modification. This bird was carrying at least two separate geneware modifications, neither of which was designed for pigeons. Recovery was uneventful. Released after four weeks.

Case 0198: Brown rat, male, approximately 300 grams — large for the species. Captured alive by a Shelf community board pest assessment team and brought for health screening. Genetic screening revealed markers for three separate geneware modifications: a metabolic optimization suite (originally designed for human athletic enhancement), a retinal modification (originally designed for human low-light vision enhancement), and a neural plasticity promoter (originally designed for human cognitive therapy). None of these modifications were designed for rats. All three were expressing, at varying levels, in this rat's biology. The metabolic suite had increased the rat's baseline body temperature by 1.2°C and likely contributed to its above-average size. The retinal modification status was unclear — I lack the equipment for rat visual acuity testing. The neural plasticity promoter was active. I noted this in my file and spent the rest of the evening wondering what enhanced neural plasticity does to a rat, and whether I wanted to know.

Case 0256: Mixed-breed dog, male, 7 years old, owned communally by the residents of Block 4. Presenting complaint: "He barks at the radio." More specifically, the dog reacted to radio frequency transmissions — becoming agitated when specific frequencies were broadcast from a handheld transceiver used by the owner's neighbor for community communications. Testing confirmed auditory response to transmissions in the 400–500 MHz range. The dog could hear radio. Not the speaker output — the electromagnetic transmission itself. Examination revealed no visible ear abnormalities, but otoacoustic emission testing showed cochlear responses to electromagnetic stimulation that should not produce auditory perception in any known mammal. The modification, if it is a modification, has no known origin. The dog was otherwise healthy, well-loved, and extremely good. I prescribed nothing. Some things don't need treatment. They need documentation.

These are four cases from a file that contains over three hundred. Each case is an individual animal. Each animal is a data point in a pattern so large that no single veterinarian, no single lab, no single institution can see its full shape. The wildlife of Meridian 88 is changing. It is changing faster than wildlife should change, in directions that nobody planned, through mechanisms that nobody fully understands. I document what I see. I treat what I can. And I note, in my personal journal, that the most honest thing a scientist can say about the state of urban biology in Meridian 88 is: we don't know what's happening, and we're not sure we can keep up.`,
    related_entities: ["Meridian 88", "Shelf", "Block 4", "Block 9"],
    credibility: "verified",
    story_hooks: [
      "The dog that hears radio — could it detect covert communications?",
      "The rat with three geneware modifications: a tracker could follow the modification trail back to the contamination source",
      "The vet's casebook becomes evidence in a corponation liability case"
    ],
    tags: ["document", "medical_casebook", "urban_wildlife", "veterinary", "geneware", "contamination", "ecology", "meridian_88", "shelf"]
  }
];

// ─────────────────────────────────────────────────────────────
// 5 GENEWARE ENTRIES — Wildlife Geneware
// ─────────────────────────────────────────────────────────────

const genewareEntries = [
  {
    name: "Bioluminescent Pigeon Feather Contamination",
    brand_name: "N/A — environmental contamination",
    product_name: "Bioluminescent Pigeon Feather Contamination",
    aliases: ["Glow Pigeon Mod", "Pigeon Shine", "Blue-Green Breast"],
    category: "cosmetic (unintended transspecies expression)",
    target_system: "integumentary (feather keratin bioluminescence via GFP variant)",
    description: "A cosmetic bioluminescence modification originally developed by Helix Biosystems for domestic pet applications, which entered the Meridian 88 groundwater table through improperly disposed biowaste circa 2190. The modification codes for a variant of green fluorescent protein (GFP) that integrates into feather keratin during molt, producing a faint blue-green bioluminescence along the breast feathers of rock doves (Columba livia). The modification is now endemic in the city's pigeon population, affecting an estimated 80% of individuals. It is purely cosmetic in direct effect, but its presence in wild genomes indicates active geneware integration into uncontrolled populations. Secondary behavioral effects include navigation by BCI signal strength gradients rather than the Earth's magnetic field — a shift documented over at least thirty years.",
    source_organism: "Aequorea victoria (jellyfish, GFP donor) via Helix Biosystems cosmetic pet product line",
    manufacturer: "Unknown — environmental contamination (original product: Helix Biosystems)",
    tier_availability: "N/A — wild occurrence",
    legality: "Unregulated (environmental contaminant; no legal framework for wild population geneware)",
    expression_time: "Expresses during first post-exposure molt cycle (4-8 weeks in pigeons)",
    reversibility: "Irreversible in wild populations; heritable through standard reproduction",
    side_effects: [
      "Navigation shift from geomagnetic to BCI signal-based orientation",
      "Increased visibility to predators at dusk and in darkness",
      "Unknown long-term effects of GFP accumulation in feather tissue",
      "Potential for secondary modification transfer to predator species (not yet observed)"
    ],
    social_perception: "Generally positive among Shelf residents, who find the glowing pigeons charming. Biologists and environmental scientists view the contamination as alarming evidence of uncontrolled geneware integration into wild ecosystems.",
    story_hooks: [
      "Pigeon flocks used to map BCI signal topology — their flight patterns reveal the invisible infrastructure",
      "The contamination pathway from Helix Biosystems to the water table could be traced — and someone is liable",
      "A pigeon flock suddenly loses its bioluminescence — the water supply has changed"
    ],
    tags: ["geneware", "urban_wildlife", "pigeon", "bioluminescence", "contamination", "ecology", "helix_biosystems", "cosmetic"]
  },
  {
    name: "Enhanced Rat Colony Intelligence",
    brand_name: "N/A — environmental contamination",
    product_name: "Enhanced Rat Colony Intelligence",
    aliases: ["Rat Uplift", "Colony Mind", "The Swarm"],
    category: "cognitive (unintended transspecies expression)",
    target_system: "neurological (enhanced neural plasticity, synaptic density, and social cognition)",
    description: "A composite cognitive enhancement resulting from the accumulation of multiple human-targeted geneware modifications in the brown rat (Rattus norvegicus) populations of Meridian 88. Genetic screening of affected rats reveals markers for neural plasticity promoters, metabolic optimization suites, and retinal modifications — none designed for rodent biology, all expressing at varying levels. The cumulative effect is a measurable increase in problem-solving capacity, social coordination, and adaptive behavior that exceeds normal rat cognition by a significant margin. Affected colonies demonstrate role differentiation (scouts, foragers, sentinels), schedule-based foraging behavior, tool-adjacent manipulation (holding doors, triggering mechanisms), and adaptation to new threat patterns within hours rather than days. The enhancement is not a single modification but an emergent property of multiple unrelated geneware agents interacting within rat neurology.",
    source_organism: "Multiple human-targeted geneware products (neural plasticity, metabolic, retinal modifications)",
    manufacturer: "Unknown — environmental contamination (multiple original manufacturers)",
    tier_availability: "N/A — wild occurrence",
    legality: "Unregulated (environmental contaminant; classified as pest management concern)",
    expression_time: "Cumulative; behavioral changes emerge over multiple generations of exposure",
    reversibility: "Irreversible in wild populations; modifications are heritable and cumulative across generations",
    side_effects: [
      "Increased metabolic rate requiring higher caloric intake",
      "Above-average body size in heavily modified individuals",
      "Behavioral complexity that renders standard pest control ineffective",
      "Potential for continued cognitive escalation as additional geneware agents accumulate",
      "Unknown ceiling on rat cognitive enhancement"
    ],
    social_perception: "Alarming to pest control professionals and public health officials. Shelf residents oscillate between grudging respect and genuine unease. The rats are harder to kill and appear to be getting smarter. Nobody is comfortable with the trajectory.",
    story_hooks: [
      "A rat colony's behavior suggests they have learned something about a building's security that humans haven't noticed",
      "The cognitive enhancement has no known ceiling — how smart can the rats get?",
      "Someone attempts to weaponize the rats' organizational capacity"
    ],
    tags: ["geneware", "urban_wildlife", "rat", "intelligence", "contamination", "ecology", "cognitive", "emergent"]
  },
  {
    name: "Feline Ocular Bioluminescence",
    brand_name: "Starlight Eyes (original product, discontinued)",
    product_name: "Feline Ocular Bioluminescence",
    aliases: ["Cat Glow Eyes", "Starlight Mod", "Feral Eyes"],
    category: "cosmetic (transspecies transmission from domestic to feral populations)",
    target_system: "ocular (tapetum lucidum bioluminescent modification with extended spectral sensitivity)",
    description: "Originally marketed as 'Starlight Eyes' by a NovaPharma subsidiary in the early 2180s, this cosmetic modification replaces the tapetum lucidum's passive reflective function with active bioluminescence, causing the cat's eyes to emit a soft, sustained glow in low-light conditions. The product was intended for domestic cats and was commercially successful for approximately three years before the manufacturer discovered the modification was heritable — passing from modified cats to all offspring regardless of the other parent's modification status. By the time the product was discontinued, enough modified domestic cats had been abandoned or escaped to establish breeding populations throughout the Shelf. The feral cat population of the Circuit District now carries the modification at an estimated 90% prevalence. Three color variants exist: green (dominant, ~82%), amber (~15%), and blue (rare, ~3%). An unintended functional consequence: the modified tapetum has expanded feline visual sensitivity into near-ultraviolet wavelengths, making Circuit ferals significantly more capable predators in low-light and no-light conditions.",
    source_organism: "Renilla reniformis (sea pansy, luciferase donor) via NovaPharma cosmetic product line",
    manufacturer: "Unknown — environmental contamination (original product: NovaPharma subsidiary, name redacted)",
    tier_availability: "N/A — wild occurrence",
    legality: "Unregulated (original product discontinued; feral population modification outside regulatory scope)",
    expression_time: "Present from birth in offspring of modified parents; full luminescence develops by 8-12 weeks of age",
    reversibility: "Irreversible in wild populations; dominant allele ensures transmission to all offspring",
    side_effects: [
      "Extended spectral sensitivity into near-ultraviolet wavelengths",
      "Enhanced predatory capability in darkness",
      "Development of extra-range vocalizations (up to 3,400 Hz and down to 12 Hz infrasonic)",
      "Cross-colony communication at distances exceeding normal feline social range",
      "Blue variant associated with higher luminescence intensity and possible UV sensitivity beyond green/amber variants"
    ],
    social_perception: "Circuit District residents consider the glowing-eyed cats a defining feature of the neighborhood. Blue-eyed ferals are considered lucky. The cats are fed, tolerated, and in some blocks actively protected by residents. NovaPharma's liability in the contamination event is an open legal question that nobody with standing has pursued.",
    story_hooks: [
      "The cats' UV vision lets them see things humans can't — markings, residues, biological traces",
      "The cross-colony communication network could be intercepted or decoded",
      "A blue-eyed feral leads someone to something hidden in a place only UV-sensitive eyes could find"
    ],
    tags: ["geneware", "urban_wildlife", "cat", "feral", "bioluminescence", "contamination", "ecology", "novapharma", "cosmetic", "circuit_district"]
  },
  {
    name: "Cockroach Exoskeletal Metallization",
    brand_name: "N/A — spontaneous environmental adaptation",
    product_name: "Cockroach Exoskeletal Metallization",
    aliases: ["Metal Roach", "Chrome Shell", "Circuit Bug"],
    category: "structural (environmentally driven exoskeletal modification)",
    target_system: "exoskeletal (chitin-metal composite matrix formation during molting)",
    description: "An adaptive modification observed in cockroach populations (Blattella germanica and Periplaneta americana) within Meridian 88's industrial and electronic waste zones, in which trace metals — primarily copper, tin, and aluminum — are structurally integrated into the chitin matrix of the exoskeleton during molting. The mechanism involves modified digestive enzymes that extract metallic compounds from ingested electronic waste, transport them via hemolymph, and deposit them into the exoskeletal structure during formation. The resulting composite material is measurably harder and more conductive than standard cockroach cuticle. Whether this adaptation is driven by geneware contamination altering the cockroaches' digestive biochemistry or by conventional natural selection in a metal-rich environment is not determined. The adaptation co-occurs with comprehensive pesticide resistance (99.6% of registered compounds) and rudimentary electromagnetic sensitivity manifesting as avoidance of BCI frequencies in the 2.1-2.4 GHz range.",
    source_organism: "Blattella germanica / Periplaneta americana (self-modified through environmental adaptation)",
    manufacturer: "Unknown — environmental contamination (possibly spontaneous adaptation)",
    tier_availability: "N/A — wild occurrence",
    legality: "Unregulated (classified as pest species adaptation; no legal framework applies)",
    expression_time: "Progressive; metallization increases with each molt cycle in metal-rich environments",
    reversibility: "Potentially reversible if populations are removed from metal-rich environments for multiple generations; untested",
    side_effects: [
      "Comprehensive pesticide resistance (99.6% of 847 registered compounds)",
      "Electromagnetic sensitivity and BCI frequency avoidance behavior",
      "Increased exoskeletal hardness reducing predation by standard predators",
      "Weak electrical conductivity of exoskeleton",
      "Population concentration in BCI signal dead zones"
    ],
    social_perception: "Universally disliked but grudgingly respected by entomologists. Pest control professionals have largely given up chemical approaches. The cockroaches' BCI avoidance behavior is considered 'eerie' by residents and 'fascinating' by researchers. The GLMZ Entomological Division has officially recommended ceasing eradication attempts.",
    story_hooks: [
      "Cockroach distribution maps as BCI dead zone maps — what's hiding in the signal shadows?",
      "The conductive exoskeletons could be harvested for improvised electronics",
      "A new effective pesticide threatens to collapse the cockroach ecosystem, with unpredictable cascading effects"
    ],
    tags: ["geneware", "urban_wildlife", "cockroach", "metallization", "contamination", "ecology", "electromagnetic", "adaptation", "circuit_district"]
  },
  {
    name: "Apian Geometric Hive Restructuring",
    brand_name: "N/A — environmental contamination",
    product_name: "Apian Geometric Hive Restructuring",
    aliases: ["New Comb", "Bee Architecture", "Geometric Hive"],
    category: "behavioral/structural (emergent construction behavior modification)",
    target_system: "neurological/behavioral (modified construction instincts producing novel comb geometry)",
    description: "An emergent modification in the honeybee (Apis mellifera) populations that recolonized Meridian 88 following the regional colony collapse of 2120-2125. The recovered bee populations — which began reappearing around 2186 from unknown origin — construct hive comb using geometric patterns that deviate significantly from the standard hexagonal tessellation used by honeybees for over 100 million years. The new comb incorporates hexagonal, pentagonal, heptagonal, and irregularly shaped cells in patterns that computational analysis identifies as mechanically superior to standard hexagonal comb: stronger, better at distributing structural loads, and incorporating internal channels for ventilation and thermal regulation. The modification appears to affect construction behavior rather than physiology — the bees themselves are morphologically standard, but their building instincts produce architecture that should be beyond the capacity of insect cognition to design. The modification co-occurs with new waggle dance patterns that apiarists cannot decode and honey production containing trace compounds from unidentified botanical sources.",
    source_organism: "Apis mellifera (modified population of unknown origin, post-collapse recovery)",
    manufacturer: "Unknown — environmental contamination (origin of recovered bee populations undetermined)",
    tier_availability: "N/A — wild occurrence",
    legality: "Unregulated (wild insect population; honey produced is approved for consumption)",
    expression_time: "Present from colony establishment; new colonies immediately construct modified comb",
    reversibility: "Unknown; no unmodified honeybee populations exist in the region for comparison breeding",
    side_effects: [
      "Novel comb geometry with superior mechanical properties",
      "New waggle dance patterns of unknown communicative content",
      "Honey containing trace compounds from unidentified plant sources",
      "Foraging scouts visit locations beyond GPS tracking range in the wasteland",
      "Complete replacement of pre-collapse bee genetics with novel population"
    ],
    social_perception: "The bees' return is universally welcomed. The honey is popular at the Circuit night market. The architectural anomalies fascinate engineers and apiarists. The unanswered questions — where the bees came from, where they forage, what the new dances mean — generate more wonder than anxiety in most residents.",
    story_hooks: [
      "Following the bees to their wasteland foraging site reveals something unexpected",
      "The new comb geometry inspires a structural innovation — or is reverse-engineered by a corponation",
      "Decoding the new waggle dance patterns reveals information about the wasteland that no human survey has captured"
    ],
    tags: ["geneware", "urban_wildlife", "bee", "hive", "architecture", "contamination", "ecology", "wasteland", "mystery", "emergent"]
  }
];

// ─────────────────────────────────────────────────────────────
// GENERATE
// ─────────────────────────────────────────────────────────────

let documentCount = 0;
let genewareCount = 0;

for (const doc of documents) {
  const entity = {
    id: generateId(),
    ...doc,
    type: "document"
  };
  if (writeEntity(documentsDir, entity)) documentCount++;
}

for (const gw of genewareEntries) {
  const entity = {
    id: generateId(),
    ...gw,
    type: "geneware"
  };
  if (writeEntity(genewareDir, entity)) genewareCount++;
}

console.log(`\nDone. Wrote ${documentCount} documents and ${genewareCount} geneware entries (${documentCount + genewareCount} total).`);
