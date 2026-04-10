const fs = require('fs');
const path = require('path');
const crypto = require('crypto');

const outDir = path.resolve(__dirname, '..', 'engine', 'data', 'places');

function generateId() {
  return crypto.randomBytes(16).toString('hex');
}

function writeVenue(venue) {
  const filePath = path.join(outDir, `${venue.id}.json`);
  if (fs.existsSync(filePath)) {
    console.log(`SKIP (exists): ${filePath}`);
    return false;
  }
  fs.writeFileSync(filePath, JSON.stringify(venue, null, 2), 'utf-8');
  console.log(`WROTE: ${venue.name} -> ${venue.id}.json`);
  return true;
}

const venues = [
  // ============================================================
  // DIVE BARS (10)
  // ============================================================
  {
    name: "The Rust Lung",
    description: "A Shelf-tier dive carved into the basement of a condemned meatpacking plant on Chicago's near west side, where the ceiling sweats condensation year-round and the air tastes like copper. The Rust Lung earned its name honestly — the ventilation hasn't worked since the 2180s, and the particulate count would make an occupational health inspector weep. Drinks are served in mismatched containers, the house special is a grain alcohol blend called Oxidizer, and the bartender, a woman named Maret who lost her left arm to a Carrion Logistics loading accident, keeps a sawed-off shotgun under the counter that everyone pretends not to know about.\n\nThe clientele is dockworkers, off-shift factory hands, and people who need a drink badly enough to risk their respiratory health. There's a back room with a steel door where certain transactions happen — nothing organized, just the natural economy of people who have things other people want. The Rust Lung doesn't have a sign. You find it by smell.",
    coordinates: { lat: 41.8827, lng: -87.6544, tags: [] },
    tags: ["place", "nightlife", "bar", "shelf", "chicago"],
  },
  {
    name: "Circuit",
    description: "Circuit is what happens when someone rips the guts out of an electrical substation and puts a bar in it. Located in Milwaukee's Walker's Point, the venue still has the original transformer housing bolted to the back wall, and the whole building hums with residual electromagnetic charge from the Zheng-Dao power lines running beneath the street. Drinks are cheap, the lighting is whatever the transformers feel like providing on a given night, and the floor is bare concrete stained with decades of spilled beer and motor oil. The owner, a wiry Ghanaian-Finnish man named Kojo Virtanen, claims the EM field keeps the roaches away. Nobody believes him, but the roaches are notably absent.\n\nCircuit's real draw is its location: it sits at the intersection of three different corponation jurisdictions, which means enforcement response time is measured in hours, not minutes. This makes it a natural gathering spot for people who prefer their social drinking without surveillance. The jukebox plays exclusively pre-Collapse music, and if you try to connect a BCI to the building's nonexistent network, the EM field will give you a headache that lasts three days.",
    coordinates: { lat: 43.0258, lng: -87.9154, tags: [] },
    tags: ["place", "nightlife", "bar", "shelf", "milwaukee"],
  },
  {
    name: "Drowning Judith",
    description: "Named after a woman who allegedly walked into Lake Michigan in 2161 and came back three days later with no memory and perfect teeth, Drowning Judith is a waterfront dive in Kenosha that caters to lake haulers, fishermen, and anyone else whose livelihood involves getting wet. The bar is built from the inverted hull of a decommissioned Carrion Logistics cargo skiff, so the ceiling curves above you like the inside of a boat, and when it rains the acoustics are extraordinary. Everything smells like fish and lakewater and the cheap synthetic whiskey that Judith's serves by the mason jar.\n\nThe owner changes every few years — the bar seems to consume them, one way or another — but the regulars are permanent fixtures. There's Old Hask, who claims to have seen an E.L.F. surface during the 2189 storms. There's the Widow Tanaka-Okonkwo, who runs numbers out of the corner booth. And there's the cat, a massive orange tabby named Debt, who has outlived four owners and shows no signs of mortality.",
    coordinates: { lat: 42.5847, lng: -87.8212, tags: [] },
    tags: ["place", "nightlife", "bar", "shelf", "kenosha"],
  },
  {
    name: "Punchclock",
    description: "A factory-worker bar in Racine where the drinks are timed. You get exactly twelve minutes per round — the duration of a standard factory break — and when the buzzer sounds, you either order again or you leave. This isn't cruelty; it's culture. Punchclock was founded by a collective of Slagworks Industrial shift workers who wanted a bar that respected the rhythm of labor, and the twelve-minute round has become almost sacred. The decor is factory floor: punch-clock machines line the wall (all functional, all keeping different times), the tables are repurposed stamping press platforms, and the lighting is industrial fluorescent that makes everyone look like they're about to start a shift.\n\nThe beer is brewed on-site from grain that comes down the corridor from Green Bay agricultural co-ops, and it's surprisingly good — a dark amber lager that regulars call Overtime. The kitchen serves exactly one thing: a protein-dense sandwich called the Shift Meal that changes daily based on available ingredients. There is no music. There is only the buzzer.",
    coordinates: { lat: 42.7261, lng: -87.7829, tags: [] },
    tags: ["place", "nightlife", "bar", "shelf", "racine"],
  },
  {
    name: "The Filament",
    description: "Tucked into a narrow alley between two Zheng-Dao Bioelectric relay stations in Chicago's Pilsen neighborhood, The Filament is lit entirely by stolen power. The owner — known only as Lux — has been tapping the relay stations for two decades, running enough current through the bar's homemade lighting system to create an amber glow that pulses with the city's power grid. When demand spikes, the bar dims. When the grid is quiet, The Filament blazes. Regulars have learned to read the lights like a barometer of the city's mood.\n\nThe drinks are bottom-shelf and the furniture is scavenged, but The Filament has something most Shelf bars don't: warmth. Not just thermal — Lux keeps the place heated well above street temperature — but social. This is where people come after funerals, after job losses, after the kind of days that make you wonder why you're still in the corridor. Nobody asks questions. Nobody starts fights. The unspoken rule is that The Filament is for people who need a place to sit, and that's enough.",
    coordinates: { lat: 41.8565, lng: -87.6616, tags: [] },
    tags: ["place", "nightlife", "bar", "shelf", "chicago", "pilsen"],
  },
  {
    name: "Gravel",
    description: "A bar built in and around a depleted gravel quarry on the outskirts of Waukegan, where the pit has been partially flooded and the remaining dry shelf holds a sprawling open-air drinking establishment that looks like a post-industrial amphitheater. The bar itself is a shipping container welded to the quarry wall, and seating consists of old quarry equipment — bucket seats from excavators, flattened conveyor belt segments, the cab of a crane that someone dragged to the edge and declared a VIP booth. In summer, patrons swim in the flooded pit. In winter, they drink faster.\n\nGravel's owner is a former demolition specialist named Ibrahima Johansson-Diallo who lost his hearing in an industrial accident and now communicates exclusively through a text display on his forearm. He runs the bar with terrifying efficiency, a photographic memory for tabs, and a zero-tolerance policy for anyone who messes with his quarry. The house drink is called The Aggregate — grain alcohol, crushed ice, and a proprietary bitter made from foraged lakeside plants that tastes like dirt in the best possible way.",
    coordinates: { lat: 42.3636, lng: -87.8448, tags: [] },
    tags: ["place", "nightlife", "bar", "shelf", "waukegan"],
  },
  {
    name: "Null Pointer",
    description: "A tech worker dive in Chicago's Loop district that caters to the mid-tier programmers, network techs, and data janitors who keep GLMZ's digital infrastructure running and receive almost no credit for it. The bar's name is a programming joke that hasn't been funny for a century, but the sign stays. The interior is aggressively dim, every surface is black or dark gray, and the drinks are named after error codes. The most popular is the 404 — a clear spirit served in a glass that appears to be empty but isn't.\n\nNull Pointer's real function is as a complaint department. This is where the people who maintain the corridor's networks come to vent about the systems they're forced to keep alive, the legacy code that should have been retired decades ago, and the corponation managers who don't understand what they're asking for. The walls are covered in scrawled code fragments, inside jokes, and the occasional genuinely brilliant algorithm sketched on a napkin and taped up for posterity. There's a running tab for anyone who can solve the bar's weekly coding challenge, posted on a whiteboard behind the bar.",
    coordinates: { lat: 41.8819, lng: -87.6278, tags: [] },
    tags: ["place", "nightlife", "bar", "circuit", "chicago", "loop"],
  },
  {
    name: "The Sump",
    description: "Located in a literal pump station in Gary, Indiana — the southern edge of the GLMZ — The Sump is what drinking looks like when infrastructure gives up. The pump station was built to manage stormwater overflow and abandoned when the Gary municipal water system was absorbed by Zheng-Dao. Someone cleaned out the main pump chamber, installed a bar made from a water main section, and started selling drinks. The pumps are still in the room. They don't work, but they're too heavy to move, so they've become furniture. The floor is perpetually damp.\n\nThe Sump attracts the hardest crowd in the southern corridor: salvage crews, infrastructure scavengers, and the kind of independent contractors who don't list their specialties on public registries. The bar serves exactly three drinks — beer, whiskey, and something called Runoff that nobody has successfully identified. Fights are common but brief; the damp floor makes traction difficult, which tends to resolve conflicts through mutual embarrassment rather than injury.",
    coordinates: { lat: 41.5934, lng: -87.3464, tags: [] },
    tags: ["place", "nightlife", "bar", "shelf", "gary"],
  },
  {
    name: "Soffit",
    description: "A bar that exists entirely in the ceiling void of an active Carrion Logistics warehouse in Milwaukee's Menomonee Valley. Access is through a maintenance hatch in the warehouse's east wall; you climb a ladder into the space between the warehouse roof and the drop ceiling, where someone has laid plywood across the joists and built a bar that extends for roughly forty meters along the length of the building. The ceiling height is about 1.4 meters, so everyone drinks sitting down or crouching. Below you, through gaps in the ceiling tiles, you can watch Carrion forklifts moving freight. The warehouse workers know the bar is there. Management does not. This arrangement has held for eleven years.\n\nSoffit serves canned beer and bottled spirits only — nothing on tap, because running water lines would be too conspicuous. The bartender is a former Carrion employee named Desta Park-Mensah who was fired for \"unauthorized use of vertical space\" and took it as a challenge. The regulars are a mix of warehouse workers on break, people hiding from various obligations, and a small but dedicated community of urban explorers who consider Soffit the best-kept secret in the corridor. It probably isn't, but it makes them happy to think so.",
    coordinates: { lat: 43.0178, lng: -87.9421, tags: [] },
    tags: ["place", "nightlife", "bar", "shelf", "milwaukee", "hidden"],
  },
  {
    name: "Tenth Round",
    description: "A boxing-themed dive in Green Bay's east side where every surface is padded. The walls are wrapped in old gym mats, the bar top is a heavy bag sliced lengthwise, and the stools are ring corner posts with seat pads bolted on. The owner, a retired bare-knuckle fighter named Yuki Borg-Andersen, built the place as a retirement project and filled it with memorabilia from thirty years of corridor fighting circuits. The drinks are strong and served in tin cups because glass is banned — too many thrown objects in the bar's early days.\n\nTenth Round is where Green Bay's working class goes to drink and argue about fights, both historical and upcoming. The back wall has a chalkboard listing every major arena bout in the GLMZ for the coming month, with odds updated daily by Yuki herself. There's an informal betting pool that technically doesn't exist, and a heavy bag in the corner that anyone can hit if they're having a bad night. The regulars have a tradition: if you finish ten drinks in one sitting, Yuki rings the bell and your tab is cleared. Nobody has managed it and remained conscious.",
    coordinates: { lat: 44.5192, lng: -88.0198, tags: [] },
    tags: ["place", "nightlife", "bar", "shelf", "green-bay"],
  },

  // ============================================================
  // MUSIC VENUES (8)
  // ============================================================
  {
    name: "The Cistern",
    description: "A music venue built inside a converted water tower in Oshkosh, Wisconsin, where the cylindrical tank's natural acoustics produce a reverb that audio engineers have described as \"cathedral crossed with submarine.\" The tower was decommissioned in the 2170s when Oshkosh's water infrastructure was consolidated, and a Senegalese-Norwegian sound designer named Amadou Larsen bought it for the price of the back taxes. He gutted the interior, installed a stage at the bottom of the tank, and arranged seating in ascending rings up the curved wall. Capacity is 200, and every seat has a different sonic experience because of how the sound bounces off the steel.\n\nThe Cistern books acts that can exploit the space: solo performers, acoustic ensembles, experimental sound artists, and the occasional neural-feed musician who uses the tank's resonance as a physical instrument. There's no amplification system — the tower IS the amplification system. Amadou has a strict no-BCI-recording policy enforced by signal jammers embedded in the walls, which means the only way to hear a Cistern performance is to be there. Bootleg recordings exist but they're all terrible, which is exactly the point.",
    coordinates: { lat: 44.0247, lng: -88.5426, tags: [] },
    tags: ["place", "nightlife", "venue", "music", "circuit", "oshkosh"],
  },
  {
    name: "Pirate Signal",
    description: "An illegal radio station and live music venue operating from a different location in Chicago every week, broadcasting on frequencies that Zheng-Dao Bioelectric has been trying to shut down for six years. Pirate Signal is less a place than an event: the organizers — a rotating collective of musicians, engineers, and dedicated troublemakers — set up in abandoned buildings, rooftops, underpasses, and once memorably inside a stalled maglev car on the Blue Line. They broadcast live performances on shortwave radio, which is so archaic that most corponation monitoring systems don't even scan for it. You find the signal, you find the show.\n\nThe music is raw and uncurated — punk, industrial, spoken word, and genres that don't have names yet. Performers play for free; the venue's currency is attention and the thrill of doing something that multiple corponations would prefer you didn't. Pirate Signal has launched more careers than any legitimate venue in the corridor, mostly because the kind of artist who plays an illegal radio show in an abandoned building tends to be the kind of artist people remember. The collective's only rule: no covers. Everything must be original. If you play someone else's song, you don't get invited back.",
    coordinates: { lat: 41.8756, lng: -87.6244, tags: [] },
    tags: ["place", "nightlife", "venue", "music", "shelf", "chicago", "mobile"],
  },
  {
    name: "Low Frequency",
    description: "A bass music venue in Milwaukee's Bay View neighborhood built in the sub-basement of a former brewery, where the stone walls are three feet thick and the sound doesn't escape — it just accumulates. Low Frequency specializes in music you feel more than hear: sub-bass drone, industrial percussion, and neural-feed compositions designed to resonate with the human skeletal system. The dance floor vibrates. The drinks vibrate. Your teeth vibrate. The owner, an audio engineer named Priya Nakamura-Osei, has tuned the room so precisely that standing in different spots produces different physical sensations, from gentle warmth to mild nausea.\n\nThe venue's signature event is \"Bone Night,\" a monthly performance where the music is pitched entirely below the threshold of human hearing — you can't hear anything, but your body knows something is happening. Bone Night draws a devoted crowd of audiophiles, BCI researchers studying somatic perception, and people who just want to feel something they can't explain. There's a medical waiver posted at the door that everyone ignores. Drinks are served in heavy ceramic cups because glass shatters at certain frequencies.",
    coordinates: { lat: 42.9924, lng: -87.8993, tags: [] },
    tags: ["place", "nightlife", "venue", "music", "circuit", "milwaukee"],
  },
  {
    name: "The Antenna",
    description: "A rooftop music venue in Waukegan that operates exclusively at night and only books acts willing to play in the open air, exposed to whatever weather Lake Michigan feels like providing. The stage is a reinforced platform bolted to the roof of a twelve-story Shelf residential tower, and the audience sits on the surrounding rooftops, fire escapes, and upper-floor windows of adjacent buildings. Sound carries across the Shelf roofscape like a public service. The Antenna doesn't charge admission because there's no way to fence in a rooftop — instead, performers earn through voluntary contributions tossed up in buckets on ropes.\n\nThe venue was founded by a collective of Shelf musicians who were tired of playing in basements and wanted to reclaim the sky. On clear nights, you can hear The Antenna from six blocks away, and the surrounding buildings have developed an informal economy around it: food vendors on adjacent roofs, drink sellers in the stairwells, and at least one woman who rents binoculars from her fire escape. When it storms, The Antenna plays anyway — the rain shows are legendary, dangerous, and the closest thing the Waukegan Shelf has to a communal religious experience.",
    coordinates: { lat: 42.3615, lng: -87.8700, tags: [] },
    tags: ["place", "nightlife", "venue", "music", "shelf", "waukegan"],
  },
  {
    name: "Resonance Hall",
    description: "A Spire-tier concert venue in Chicago's Magnificent Mile district that hosts neural-feed performances — concerts where the music is transmitted directly into the audience's brain-computer interfaces, bypassing the ears entirely. The hall itself is acoustically dead: the walls, floor, and ceiling are lined with sound-absorbing material, and the space is kept in complete silence during performances. The audience sits in ergonomic chairs with eyes closed, experiencing compositions that include sensory dimensions impossible in traditional music — color, temperature, spatial orientation, and emotional textures layered into the neural feed.\n\nResonance Hall is owned by Tessera Corporation as a cultural investment property and books only the most prestigious neural-feed composers in the GLMZ. Tickets start at 200 Quanta and the waiting list for season passes is eighteen months. The venue seats 400 and every performance sells out. Critics have called it everything from \"the future of music\" to \"an expensive way to let a corporation put things in your head,\" and both camps have a point. The hall employs a full-time neurologist on staff for the rare cases where a composition triggers an adverse neural response — it's happened three times in the venue's history, all during works by the composer known only as Dissonance.",
    coordinates: { lat: 41.8951, lng: -87.6244, tags: [] },
    tags: ["place", "nightlife", "venue", "music", "spire", "chicago"],
  },
  {
    name: "The Silo",
    description: "A live music venue built inside a grain silo on the agricultural fringe north of Green Bay, where the corrugated steel walls create an acoustic environment that sounds like playing inside a tin can and somehow works beautifully for the raw, loud, angry music that The Silo books. Capacity is maybe 150 if everyone is friendly about personal space, and the single entrance doubles as the only exit, which gives every show an intensity born from the knowledge that if something goes wrong, everyone is going through the same door.\n\nThe Silo is run by a farming family — the Johansson-Ndiaye clan — who got into the music business accidentally when their daughter started hosting shows for her friends and the thing grew until it became the premiere punk and industrial venue north of Milwaukee. There's no liquor license; the family sells homemade cider from their orchard and pretends the mason jars of clear liquid in the back aren't what everyone knows they are. Bands play on a wooden platform built from old barn boards, and the sound check consists of someone yelling from the back to see if the vocals are audible over the drums. They usually aren't. Nobody minds.",
    coordinates: { lat: 44.5633, lng: -88.1024, tags: [] },
    tags: ["place", "nightlife", "venue", "music", "shelf", "green-bay"],
  },
  {
    name: "Cathode",
    description: "An electronic music venue in Chicago's Humboldt Park neighborhood that occupies a former television repair shop and has kept every CRT monitor the previous owner left behind. There are over three hundred cathode ray tubes in the space — mounted on walls, stacked in columns, hanging from the ceiling — and they're all wired into the sound system so they respond to the music with static, color bars, and waveform visualizations. The effect is hypnotic: dancing in Cathode is like dancing inside a signal, surrounded by screens that pulse and flicker with every beat.\n\nThe resident DJs are a crew called the Phosphor Collective, who specialize in music that sounds like machines talking to each other — glitchy, rhythmic, full of frequencies that make the CRTs do interesting things. The venue has no BCI integration whatsoever; the experience is entirely analog, which has made it a destination for people who want to experience electronic music the way it was meant to be experienced: through air, through speakers, through screens that could give you radiation poisoning if you stand too close. The drinks are cheap and the cover charge is a dead battery — any kind, any size — which Cathode supposedly recycles, though the pile in the back room suggests otherwise.",
    coordinates: { lat: 41.9022, lng: -87.7225, tags: [] },
    tags: ["place", "nightlife", "venue", "music", "shelf", "chicago"],
  },
  {
    name: "Longwave",
    description: "A jazz venue in Racine operating out of the second floor of a lakefront fish market, where the music and the smell of the day's catch create a sensory combination that is either deeply atmospheric or deeply unpleasant, depending on your tolerance. Longwave books traditional acoustic jazz — upright bass, piano, horns, drums — in an era when most people have never heard an instrument that wasn't synthesized or fed through a neural interface. The owner, a bassist named Solomon Andersson-Kimathi, considers this a form of cultural preservation and treats every performance with a reverence that borders on religious.\n\nThe room seats sixty and feels like a time capsule: dim lighting, small round tables with actual candles, and a baby grand piano that Solomon rescued from a flooded Spire apartment and spent two years restoring. There's a dress code, which is unusual for Racine and consists entirely of the rule \"no visible weapons\" — a standard that eliminates about a third of the potential audience. The cocktail menu is short and classical, and Solomon makes every drink himself between sets. Longwave has a regular crowd of older corridor residents who remember when jazz was everywhere, and a growing audience of young people who've discovered that music sounds different when it comes from a physical instrument played by human hands.",
    coordinates: { lat: 42.7324, lng: -87.7848, tags: [] },
    tags: ["place", "nightlife", "venue", "music", "circuit", "racine"],
  },

  // ============================================================
  // FIGHTING ARENAS (6)
  // ============================================================
  {
    name: "The Shaft",
    description: "A fighting arena built inside a working freight elevator in a Chicago industrial tower, where the ring is the elevator car itself and the fight goes floor to floor. The building is a twenty-story Carrion Logistics vertical warehouse on the south side, mostly automated, and the elevator runs the full height. Fights start at the ground floor, the car ascends one floor per round, and the bout ends either by knockout, submission, or when the elevator reaches the twentieth floor — whichever comes first. The audience watches via cameras mounted in the car, displayed on screens set up in the warehouse's loading dock, where betting, drinking, and general chaos occur.\n\nThe Shaft is operated by a fight promoter named Reginald Osei-Magnusson, a former Spire security contractor who realized he could make more money letting people hit each other in an elevator than he ever could protecting people from being hit. The fights are bare-knuckle, unsanctioned, and technically constitute trespassing, assault, and misuse of industrial equipment. The elevator's weight limit is posted at 2,000 kilograms, and Reginald has been known to book fighters who push that limit, adding a structural anxiety to every bout. The car shakes. The cables groan. The audience loves it.",
    coordinates: { lat: 41.8308, lng: -87.6318, tags: [] },
    tags: ["place", "nightlife", "arena", "fighting", "shelf", "chicago"],
  },
  {
    name: "The Coliseum",
    description: "The GLMZ's largest legal fighting arena, located in a converted convention center in Milwaukee's Third Ward. The Coliseum hosts sanctioned bouts three nights a week — human boxing, mixed martial arts, and the increasingly popular automaton division, where programmers pit their custom-built fighting machines against each other in a reinforced steel cage. The venue seats 3,000 and regularly fills to capacity, especially for the monthly \"Iron Card\" events that feature the corridor's top-ranked fighters across all divisions. Production values are high: professional lighting, commentary broadcast on local feeds, and a medical team on standby that has seen things.\n\nThe automaton fights are the real draw. Competitors spend months building and programming their machines, which range from nimble spider-like constructs to heavy bipedal brawlers that hit like industrial presses. The machines fight in a cage because early events proved that an uncontained automaton fight will destroy everything within a fifteen-meter radius. Prize money is significant — the annual Iron Crown championship pays 50,000 Quanta — and the engineering talent on display has attracted recruitment scouts from every major corponation in the corridor. The Coliseum is one of the few places in the GLMZ where Shelf and Spire mix freely, united by the universal appeal of watching things hit other things very hard.",
    coordinates: { lat: 43.0340, lng: -87.9087, tags: [] },
    tags: ["place", "nightlife", "arena", "fighting", "circuit", "milwaukee"],
  },
  {
    name: "Undertow",
    description: "An illegal fighting pit in the flooded lower levels of a decommissioned water treatment plant in Waukegan, where fighters compete in waist-deep water and the rules are whatever the crowd decides they are. The venue holds maybe 200 spectators standing on elevated walkways above the flooded basin, looking down at two people trying to fight in conditions that make traditional technique almost useless. Striking is slow, grappling is slippery, and the water is cold enough that fights rarely last more than five minutes before hypothermia becomes a factor.\n\nUndertow is run by a collective of Waukegan Shelf residents who discovered the flooded plant and saw an opportunity. Admission is 5 Quanta, fighters keep whatever the crowd throws in (literally — Quanta chips rain down into the water), and the event moves to a different section of the plant each week to avoid pattern detection by enforcement drones. The water fights have developed their own meta: successful fighters are usually swimmers or divers who understand how to move in water, and the reigning champion is a former lakefront lifeguard named Anika Petrov-Asante who has won fourteen consecutive bouts by holding opponents underwater until they submit.",
    coordinates: { lat: 42.3553, lng: -87.8612, tags: [] },
    tags: ["place", "nightlife", "arena", "fighting", "shelf", "waukegan"],
  },
  {
    name: "Boneworks",
    description: "A Behemoth betting parlor and fighting arena north of Sheboygan where the main attraction is watching salvaged Iowan Behemoth components fight each other. Not full Behemoths — those are building-sized autonomous machines and contain them would require a stadium — but severed limbs, detached sensor arrays, and other components recovered from disabled Behemoths that still have enough onboard processing to operate independently. A Behemoth arm, properly powered, will still try to do arm things, and when you put two of them in a fenced enclosure, they will try to do arm things to each other. This is, apparently, entertainment.\n\nThe arena is an open-air compound surrounded by concrete blast walls, because Behemoth components don't know their own strength and the property damage from early events was spectacular. The audience watches from behind the walls via drone cameras, projected on screens in the adjacent betting hall. Odds are set by a bookmaker named Femi Lindqvist-Achebe who has developed an almost supernatural ability to predict how a disembodied machine limb will behave in combat. The whole operation is technically legal — there are no laws against making machines fight — but it exists in a moral gray zone that animal rights activists and machine ethicists argue about constantly.",
    coordinates: { lat: 43.7844, lng: -87.7322, tags: [] },
    tags: ["place", "nightlife", "arena", "fighting", "behemoth", "circuit", "sheboygan"],
  },
  {
    name: "Crucible Cage",
    description: "A human cage-fighting operation in Gary, Indiana, running out of a former steel mill where the cage is assembled from salvaged rebar and the floor is industrial grating that leaves pattern bruises on anyone who gets taken down. Crucible Cage is the lowest tier of fighting in the GLMZ — no sanctioning, no weight classes, no medical staff, and no rules beyond \"don't kill anyone on purpose.\" Fighters are mostly Shelf laborers working off debts, young idiots proving something to themselves, and the occasional professional slumming for quick cash under a fake name.\n\nThe mill's former foreman, a massive Samoan-Ukrainian woman named Leilani Kovalenko, runs the operation with an iron fist and genuine concern for the fighters' wellbeing that exists in permanent tension with the fact that she profits from their violence. She keeps a first aid kit that could stock a small clinic, she stops fights when they go too far (which is far), and she maintains a blacklist of fighters too dangerous or too damaged to compete. The crowd is rough, the betting is cash-only, and the building is a tetanus risk on a structural level, but Crucible Cage fills every fight night because people need somewhere to put their anger.",
    coordinates: { lat: 41.6003, lng: -87.3367, tags: [] },
    tags: ["place", "nightlife", "arena", "fighting", "shelf", "gary"],
  },
  {
    name: "Ridgeline",
    description: "A Spire-tier fighting venue in Chicago's Gold Coast that presents combat as high art. The arena is a circular platform of polished white stone suspended above a reflecting pool, lit from below so fighters cast no shadows. Bouts are choreographed in advance — not fixed, but structured, with agreed-upon rule sets and aesthetic constraints that make each fight a performance as much as a competition. Fighters wear minimal, designed attire. The audience sits in tiered seating and watches in near-silence, applauding technique rather than violence. Drinks are served between rounds by staff who move like they're part of the show.\n\nRidgeline is owned by Tessera Corporation as a prestige property and books only ranked fighters who meet its \"artistic standard\" — a deliberately vague criterion that gives management absolute discretion over who competes. The fighters are well-paid and well-cared-for, which makes Ridgeline the aspiration of every cage fighter in the corridor. Critics call it sanitized violence for people who want to feel dangerous without getting dirty. They're not wrong, but the fighting is real, the skill level is extraordinary, and more than one Ridgeline bout has ended with a fighter being carried out despite all the artistry. The stone platform, it turns out, is very hard.",
    coordinates: { lat: 41.9063, lng: -87.6262, tags: [] },
    tags: ["place", "nightlife", "arena", "fighting", "spire", "chicago"],
  },

  // ============================================================
  // ARCADES / GAMING HALLS (6)
  // ============================================================
  {
    name: "The Footprint",
    description: "An arcade built inside the severed foot of an Iowan Behemoth, a massive autonomous machine disabled during the Behemoth incursions of the 2170s. The foot — roughly the size of a two-story building — was sheared off during a military engagement near Fond du Lac and has sat in the same field ever since, too heavy to move and too interesting to scrap. Someone cut a door in the heel, ran power from a nearby grid tap, and filled the interior with arcade cabinets, VR rigs, and competitive gaming stations wired into the Behemoth's dormant internal power bus. The foot's original hydraulic joints still twitch occasionally, which the management attributes to residual power in the machine's capacitor banks and which the patrons attribute to the foot being haunted.\n\nThe Footprint draws gamers from across the northern corridor, partly for the novelty and partly because the Behemoth's internal shielding creates a near-perfect Faraday cage — no external signals get in, which means no lag, no interference, and no surveillance. Competitive gaming tournaments held inside the foot are considered the purest test of skill in the GLMZ because there's no possibility of external assist or BCI cheating. The proprietor, a former military salvage tech named Binta Eriksson-Okafor, charges 10 Quanta per hour and maintains the interior with a care that suggests either professional pride or genuine affection for the dead machine's foot.",
    coordinates: { lat: 43.7730, lng: -88.4470, tags: [] },
    tags: ["place", "nightlife", "arcade", "gaming", "behemoth", "circuit", "fond-du-lac"],
  },
  {
    name: "Coin-Op",
    description: "A retro physical arcade in Chicago's Wicker Park that operates exclusively on pre-Collapse technology: mechanical pinball machines, cathode-ray arcade cabinets from the twentieth and twenty-first centuries, and not a single neural-feed device in the building. The owner, a collector named Hiroshi Andersson-Bakare, has spent thirty years acquiring, restoring, and maintaining machines that most people have never seen outside a museum. The collection includes over 80 playable machines, from a 1978 Space Invaders cabinet to a 2045 haptic-feedback racing simulator that was state-of-the-art when it was built and now feels charmingly primitive.\n\nCoin-Op charges no admission but requires actual physical coins to play — pre-Collapse currency that Hiroshi sells at the door for 1 Quanta per handful. The coins are themselves collectibles, minted in nations that no longer exist, and some regulars come more for the numismatic experience than the games. The arcade has become a pilgrimage site for a subculture of physical-tech enthusiasts who believe that gaming peaked when it required hand-eye coordination instead of neural-interface bandwidth. Hiroshi maintains a leaderboard for every machine, handwritten on index cards pinned to a corkboard, and the competition for top scores is fierce, petty, and deeply sincere.",
    coordinates: { lat: 41.9088, lng: -87.6796, tags: [] },
    tags: ["place", "nightlife", "arcade", "gaming", "circuit", "chicago"],
  },
  {
    name: "Deep Immersion",
    description: "A neural-feed VR gaming parlor in Milwaukee's East Side that offers full-sensory virtual reality experiences so immersive that the venue employs a staff of three full-time monitors whose job is to make sure nobody loses track of which reality is real. The facility has twenty individual VR pods — sealed, climate-controlled capsules that support sessions up to eight hours — and a communal VR space where up to fifty players can share a virtual environment. The technology is cutting-edge, supplied by an exclusive contract with Tessera Corporation's entertainment division, and the experiences range from combat simulations to explorations of impossible environments to things the management describes only as \"abstract.\"\n\nDeep Immersion's most popular offering is \"The Long Dream\" — an eight-hour overnight session where players enter a persistent virtual world and live an entire day in an alternate reality. The waiting list is three weeks. The most controversial offering is \"Ego Death,\" a two-hour sensory experience designed to temporarily dissolve the player's sense of individual identity. It costs 500 Quanta, requires a psychological screening, and has a 12% voluntary discontinuation rate — meaning about one in eight players hits the panic button before the session ends. The reviews are either one star or five stars. There is no middle ground.",
    coordinates: { lat: 43.0596, lng: -87.8819, tags: [] },
    tags: ["place", "nightlife", "arcade", "gaming", "spire", "milwaukee"],
  },
  {
    name: "Quarter Finals",
    description: "A competitive esports arena in Kenosha that hosts professional and amateur gaming tournaments with a physical audience, broadcast feeds, and prize pools funded by corponation sponsors who've discovered that esports demographics align perfectly with their recruitment targets. The venue seats 500 in a converted movie theater, with the original screen replaced by a massive display array showing real-time game footage and player statistics. Competitors play on a raised stage in soundproofed booths, and the energy on tournament nights is closer to a sporting event than anything else in the building's history as a cinema.\n\nQuarter Finals runs tournaments in six different competitive titles, ranging from tactical shooters to strategy games to a bizarre Shelf-invented game called Gridlock that involves managing simulated infrastructure collapses and has somehow become the most-watched esport in the corridor. The arena's owner, a former professional gamer named Chen Johansson-Ayala, retired from competition after a neural-feed injury left her with intermittent hand tremors and channeled her expertise into building the corridor's premiere competitive venue. Prize pools for major tournaments reach 25,000 Quanta, enough to make professional gaming a viable career for the top tier and a painful hobby for everyone else.",
    coordinates: { lat: 42.5876, lng: -87.8276, tags: [] },
    tags: ["place", "nightlife", "arcade", "gaming", "esports", "circuit", "kenosha"],
  },
  {
    name: "Lucky Thirteen",
    description: "A gambling arcade in Racine that blurs the line between gaming and betting so thoroughly that nobody — including, apparently, the local enforcement — can determine which regulations apply. Every machine in Lucky Thirteen is technically a game of skill, but every game of skill has a payout mechanism, and the payouts are in Quanta. The machines are custom-built by a collective of Shelf engineers and range from modified pinball tables with cash prizes to elaborate mechanical contraptions that look like Rube Goldberg machines and function like slot machines with extra steps. The legal theory is that if you have to do something — pull a lever, aim a ball, make a choice — it's skill, not chance. This theory has not been tested in court because nobody wants to claim jurisdiction.\n\nThe arcade is dimly lit, perpetually crowded, and filled with the sounds of mechanical machines doing mechanical things: bells, clicks, whirs, and the occasional triumphant clang that means someone hit a payout. The owner, a mathematician named Fatou Nilsson-Okonkwo, designed the probability curves for every machine personally and swears the house edge is \"fair.\" The average player disagrees but keeps playing, which is, of course, the point.",
    coordinates: { lat: 42.7288, lng: -87.7921, tags: [] },
    tags: ["place", "nightlife", "arcade", "gaming", "gambling", "shelf", "racine"],
  },
  {
    name: "Hex Grid",
    description: "A tabletop gaming cafe in Appleton, Wisconsin, that caters to the corridor's surprisingly large community of physical board game, card game, and tabletop RPG enthusiasts. The venue occupies a former library and retains the shelving, which now holds over 2,000 games ranging from ancient classics to modern designs. Tables are large, well-lit, and equipped with built-in cup holders and dice trays. The atmosphere is scholarly and intense — this is competitive gaming stripped of all technology, reduced to cardboard, plastic, and the human mind.\n\nHex Grid's owner, a retired teacher named Olumide Park-Svensson, runs the venue as a community space first and a business second. Game nights are free; revenue comes from food, drinks, and a modest membership fee that grants access to the rare games vault — a locked room containing collector's editions, out-of-print titles, and a few games that were supposedly designed by AI and are \"not recommended for solo play\" for reasons Olumide won't explain. The cafe serves excellent coffee, mediocre sandwiches, and a house-brewed mead that has won a small but passionate following.",
    coordinates: { lat: 44.2619, lng: -88.4154, tags: [] },
    tags: ["place", "nightlife", "arcade", "gaming", "tabletop", "circuit", "appleton"],
  },

  // ============================================================
  // DANCE CLUBS (5)
  // ============================================================
  {
    name: "The Basin",
    description: "A dance club in a flooded sub-basement beneath a condemned hotel in Chicago's South Loop, where the water is knee-deep, the music is relentless, and the experience is unlike anything else in the corridor. The Basin started as an accident — the basement flooded during a storm, someone set up speakers and a light rig, and a party happened that became a legend. Now the flooding is maintained deliberately: pumped in from Lake Michigan, filtered just enough to be non-toxic, and kept at a constant depth of roughly 45 centimeters. The dance floor is the entire basement, and you dance in water. Your shoes are ruined. Your clothes are ruined. You don't care.\n\nThe sound system is waterproof military surplus, the lights are submersible LEDs that turn the water into a luminous plane, and the DJ booth is a raised platform accessible only by ladder. The Basin opens at midnight and closes at dawn, and the crowd is a mix of Shelf kids, Spire slummers, and people who heard about the flooded club and had to see it for themselves. The water does something to people — the resistance changes how you move, the cold keeps you alert, and the sheer absurdity of dancing in a flooded basement strips away the self-consciousness that kills most dance floors. The Basin doesn't serve alcohol because glass and water don't mix; instead, there's a tea vendor on the stairs selling hot ginger tea to people coming out of the water, which is the most civilized thing about the entire operation.",
    coordinates: { lat: 41.8566, lng: -87.6251, tags: [] },
    tags: ["place", "nightlife", "club", "dance", "shelf", "chicago"],
  },
  {
    name: "Aphelion",
    description: "A Spire-tier dance club on the 40th floor of the Tessera Tower in Chicago, where the dress code costs more than most Shelf residents make in a month and the view of Lake Michigan through floor-to-ceiling windows is the second most impressive thing in the room. The first is the crowd: Aphelion is where the corridor's corporate elite come to be seen, and the people-watching is extraordinary. The music is curated neural-feed compositions blended with physical sound, so BCI-equipped patrons experience a richer mix than those without — a tiered experience in a tiered city, which is either commentary or just good business.\n\nThe club's design changes quarterly — Tessera retains an architectural firm to redesign the space every three months, ensuring that Aphelion never looks the same way twice and that social media posts have a built-in expiration date. Current iteration: a zero-gravity aesthetic with suspended dance platforms, inverted lighting rigs, and furniture that appears to float. Drinks average 50 Quanta and are served in custom glassware that you're meant to take home as a souvenir. The door policy is managed by an AI system that evaluates potential patrons on criteria nobody has fully reverse-engineered, though wealth, social connections, and aesthetic presentation all appear to be factors. Getting rejected by Aphelion's door is a minor social catastrophe; getting in is a credential.",
    coordinates: { lat: 41.8889, lng: -87.6235, tags: [] },
    tags: ["place", "nightlife", "club", "dance", "spire", "chicago"],
  },
  {
    name: "Warehouse 7",
    description: "A Shelf warehouse rave in Milwaukee that happens every Saturday in a different abandoned industrial space along the Menomonee River, organized by a DJ collective called the Seventh Day who believe that dance music is a labor movement and that the weekend belongs to the workers. The name is always Warehouse 7, regardless of which warehouse they're in — the number refers to the seventh day, the day of rest, the day you dance. Setup starts at sundown: a sound system loaded on a flatbed truck, lighting rigs powered by portable generators, and whatever architectural features the chosen warehouse provides. Some weeks it's a cavernous empty space; other weeks it's a maze of shelving and equipment that creates an accidental labyrinth.\n\nWarehouse 7 charges no cover — the Seventh Day philosophy holds that access to communal dance is a right, not a privilege. Revenue comes from donations, a small bar selling cheap drinks from coolers, and the sale of Seventh Day merchandise that has become a minor fashion phenomenon in the Shelf. The music is house, techno, and industrial — beat-driven, repetitive, designed to be danced to for hours. The crowd is predominantly Shelf working class, and the energy on a good night is transcendent: a thousand people moving together in a space that was built for storing things, repurposed for joy.",
    coordinates: { lat: 43.0211, lng: -87.9333, tags: [] },
    tags: ["place", "nightlife", "club", "dance", "shelf", "milwaukee", "mobile"],
  },
  {
    name: "The Lily Pad",
    description: "A floating dance barge that operates on Lake Michigan between Chicago and Milwaukee, making stops at lakefront docks along the corridor for passengers to board and disembark while the music never stops. The vessel is a converted Carrion Logistics flat barge, 60 meters long, with a dance floor, a bar, a DJ booth, and nothing else — no cabins, no seats, no shelter from the weather. You board, you dance, and when the barge reaches a dock near your destination, you get off. If you miss your stop, you dance until the next one.\n\nThe Lily Pad runs Friday and Saturday nights from May through October, departing Chicago's Navy Pier dock at 10 PM and arriving at Milwaukee's harbor around 4 AM, with stops at Waukegan, Kenosha, and Racine. The return trip leaves Milwaukee at 10 PM Saturday. The experience of dancing on open water under the stars, watching the corridor's shore lights slide past, is unlike anything available on land. The barge's owner, a former Carrion cargo pilot named Isadora Magnusson-Obi, quit corporate logistics to start the venture and considers it the best decision she ever made. The music is whatever the lake's mood suggests — on calm nights, it's smooth and atmospheric; when the waves pick up, it gets aggressive. The barge has never capsized, but on rough nights, the dancing and the lake become indistinguishable.",
    coordinates: { lat: 41.8917, lng: -87.6063, tags: [] },
    tags: ["place", "nightlife", "club", "dance", "circuit", "chicago", "lake-michigan"],
  },
  {
    name: "Underthing",
    description: "An underground rave venue in the service tunnels beneath Green Bay's downtown, accessible through a maintenance entrance in a parking structure that has been quietly modified by the venue's operators. The tunnels were built for utility access — steam pipes, electrical conduit, fiber optic — and they run for kilometers beneath the city in a maze that most Green Bay residents don't know exists. The Underthing occupies a junction where four tunnel branches meet, creating a cross-shaped space with decent ceiling height and terrible ventilation. Fans and portable air movers keep it breathable, barely.\n\nThe Underthing runs on an irregular schedule announced through encrypted message boards, and finding out when the next event is constitutes the first filter on the crowd. The music is hard, fast, and physical — the kind of electronic music that uses the tunnel's reverb as an instrument, bouncing bass off concrete walls until the sound becomes architectural. The crowd is young, Shelf, and dedicated to the principle that the best parties happen in places you're not supposed to be. There's a genuine risk element: the tunnels are active utility infrastructure, and more than one Underthing event has been interrupted by a steam pipe venting or an electrical fault that killed the lights. These incidents have only added to the venue's legend.",
    coordinates: { lat: 44.5133, lng: -87.9892, tags: [] },
    tags: ["place", "nightlife", "club", "dance", "shelf", "green-bay", "underground"],
  },

  // ============================================================
  // COMEDY / PERFORMANCE VENUES (5)
  // ============================================================
  {
    name: "The Dead Room",
    description: "A comedy club in Chicago's Logan Square where the audience is screened at the door and anyone with a recording-capable BCI is politely but firmly ejected. The Dead Room's owner, a former stand-up comedian named Miriam Osei-Park, built the venue specifically to create a space where comedians could say anything — anything — without fear of being clipped, quoted, or algorithmically amplified. The room seats 80, the walls are lined with signal-dampening material, and there's an EMP device behind the bar that Miriam swears is decorative but that several patrons report experiencing as a distinct headache.\n\nThe comedy at The Dead Room is raw, dangerous, and frequently brilliant. Without the threat of recording, performers push into territory that would end careers in any surveilled space — political satire that names specific corponation executives, social commentary that targets sacred cows, and personal confession that verges on therapy. Some of it is genuinely offensive. Some of it is genuinely revolutionary. The audience is self-selected for people who believe that comedy requires risk, and the unspoken rule is that nothing said in The Dead Room leaves The Dead Room. This rule is mostly honored, partly because the signal dampening makes it impossible to record and partly because the audience understands that killing the space would be a loss for everyone.",
    coordinates: { lat: 41.9237, lng: -87.7076, tags: [] },
    tags: ["place", "nightlife", "venue", "comedy", "circuit", "chicago"],
  },
  {
    name: "Voltage",
    description: "A spoken word and poetry venue in Milwaukee's Riverwest neighborhood that hosts the corridor's most intense open-mic nights, where performers have seven minutes to say whatever they need to say and the audience decides their fate with a volume-metered applause system that displays a real-time decibel reading on a screen behind the stage. Below 60 decibels: polite acknowledgment. Above 80: genuine appreciation. Above 100: the crowd is on its feet, and the performer has earned a spot on next month's headliner bill. Below 40: you leave the stage and think about what you did.\n\nVoltage was founded by a collective of Shelf poets who believed that poetry had become too comfortable and too academic, and that performing it should feel like a fight. The venue is a former electrical supply store (hence the name), and the industrial aesthetic has been maintained — exposed conduit, warehouse lighting, concrete floor. The open mic runs every Thursday and draws a crowd of regulars who take their poetry seriously and their criticism seriously and believe that the two are the same thing. The bar serves coffee and cheap wine and nothing else, because Voltage is not about the drinks.",
    coordinates: { lat: 43.0606, lng: -87.9033, tags: [] },
    tags: ["place", "nightlife", "venue", "comedy", "spoken-word", "shelf", "milwaukee"],
  },
  {
    name: "Synthetic Voices",
    description: "A performance venue in Waukegan dedicated exclusively to synthetic performers — artificial beings who create and perform original creative work. The venue hosts poetry readings, musical performances, comedy sets, and experimental pieces by synthetics who have developed artistic practices that range from hauntingly human to deliberately alien. The space is a converted chapel, and the stained glass windows remain, casting colored light across performances that frequently explore questions of consciousness, identity, and what it means to create art when you were yourself created.\n\nSynthetic Voices is owned and operated by a synthetic named Aurelius, a third-generation Tessera model who purchased the venue with earnings from its own published poetry collection. The audience is mixed — humans and synthetics — and the atmosphere is respectful, curious, and occasionally unsettling. The most popular recurring performance is \"Translation,\" a monthly show where a synthetic performer attempts to express an internal experience that has no human equivalent, using whatever medium seems closest. These performances are polarizing: some audience members find them profoundly moving, others find them incomprehensible, and a vocal minority finds them threatening. Aurelius considers all three reactions a success.",
    coordinates: { lat: 42.3640, lng: -87.8448, tags: [] },
    tags: ["place", "nightlife", "venue", "comedy", "performance", "synthetic", "circuit", "waukegan"],
  },
  {
    name: "The Confession Booth",
    description: "A tiny performance space in Green Bay that seats exactly twelve audience members and presents one-person shows — monologues, confessions, storytelling — in a space so intimate that performers can hear the audience breathing. The venue is literally a converted confessional from a demolished Catholic church, expanded just enough to add eleven seats in front of the booth. The performer sits in the confessional, speaks through the screen, and the audience listens in near-darkness. The format strips away every theatrical tool except the human voice and whatever truth the performer is willing to tell.\n\nThe Confession Booth books three shows a night, four nights a week, and the waiting list for performers is longer than the waiting list for audience. The owner, a former priest named Father Declan Okonkwo-Sato (he kept the title after leaving the church), curates the programming with a philosophy he describes as \"radical honesty in a small room.\" Performances range from devastating personal narratives to surreal fiction delivered with absolute conviction. No comedy — Declan believes the space is too fragile for laughter. No recording of any kind. The twelve audience members are the only witnesses, and many of them return weekly, drawn to the strange intimacy of listening to a stranger tell the truth through a wooden screen.",
    coordinates: { lat: 44.5145, lng: -88.0156, tags: [] },
    tags: ["place", "nightlife", "venue", "performance", "shelf", "green-bay"],
  },
  {
    name: "Scaffold",
    description: "A Shelf theater company in Racine that performs original works in whatever space is available — abandoned buildings, public parks, factory floors, and once on a moving freight train (the audience rode in the next car). Scaffold doesn't have a permanent venue; instead, each production is site-specific, written for and performed in a space that becomes part of the story. The company's founder, a playwright named Yael Nkomo-Lindgren, believes that theater divorced from real space is just television, and that the audience should never be comfortable enough to forget they're in an actual place with actual walls and actual weather.\n\nScaffold's productions run the spectrum from gritty social realism to surrealist nightmare, and they've built a reputation for work that makes audiences genuinely uncomfortable — not through shock, but through proximity. When Scaffold stages a play about Shelf poverty, they stage it in an actual Shelf apartment. When they write about industrial labor, they perform in a working factory during off-hours. The actors are not trained professionals; they're Shelf residents who audition and rehearse around day jobs, and their performances have a raw, unpracticed quality that Yael considers a feature, not a bug. Productions are free. Donations are accepted. The company survives on grants from the Milwaukee Cultural Collective and on Yael's apparently inexhaustible willingness to do this for no money.",
    coordinates: { lat: 42.7268, lng: -87.7906, tags: [] },
    tags: ["place", "nightlife", "venue", "theater", "performance", "shelf", "racine", "mobile"],
  },

  // ============================================================
  // GAMBLING DENS (5)
  // ============================================================
  {
    name: "The Blind",
    description: "A gambling hall in Chicago's Chinatown that only accepts physical Quanta chips and has an absolute ban on electronic devices of any kind. You check your BCI-connected hardware at the door — phones, implant controllers, smart clothing, all of it goes in a locker — and you enter a space that could exist in any century. The games are traditional: poker, blackjack, dice, and a regional variant of mahjong that's been played in the corridor for decades. The tables are wooden, the chips are ceramic, and the dealers are human beings who do math in their heads. In an age of neural-feed everything, The Blind's anachronism is its luxury.\n\nThe owner, a woman known only as Mrs. Zhao, has operated The Blind for twenty-three years and has never appeared in any public database, which is either admirable operational security or evidence that she doesn't technically exist. The clientele is a mix of high-rolling Spire executives who find the digital-free environment thrilling, Shelf gamblers who prefer a game they can trust, and professional card players who have been banned from every electronic casino in the corridor for various creative forms of cheating. The Blind's house rules are enforced by a team of staff who are conspicuously large and conspicuously alert. Cheating is not punished by ejection; it's punished by a conversation with Mrs. Zhao, which is reportedly worse.",
    coordinates: { lat: 41.8516, lng: -87.6316, tags: [] },
    tags: ["place", "nightlife", "gambling", "spire", "chicago"],
  },
  {
    name: "Long Odds",
    description: "A prediction market parlor in Milwaukee where you don't bet on games — you bet on reality. Long Odds takes wagers on everything from corponation stock movements to weather patterns to the outcome of political disputes, with odds set by a proprietary algorithm that its creator, a statistician named Dmitri Okafor-Lindström, claims is the most accurate forecasting system in the GLMZ. You can bet on whether the next Iowan Behemoth sighting will be north or south of Green Bay. You can bet on which Shelf district will lose power first during the next storm. You can bet on the date of the next corponation merger. If it can be resolved to a verifiable outcome, Long Odds will take your money.\n\nThe parlor is a clean, well-lit space that looks more like a financial trading floor than a gambling den — screens displaying odds and outcomes, terminals for placing bets, and a staff of analysts who monitor resolution criteria. The minimum bet is 10 Quanta, the maximum is theoretically unlimited, and the house takes a 5% commission on winnings. Long Odds operates in a legal gray area — prediction markets are technically research tools, not gambling, which is the kind of distinction that holds up until someone loses their house. Dmitri maintains that the market provides a genuine social function by aggregating distributed knowledge into probabilistic forecasts. He also drives a very nice car.",
    coordinates: { lat: 43.0389, lng: -87.9065, tags: [] },
    tags: ["place", "nightlife", "gambling", "circuit", "milwaukee"],
  },
  {
    name: "Ironmonger's Table",
    description: "A high-stakes card room in a Spire residential tower in Chicago's River North, accessible by invitation only and operated by a fixer named Constantine Borg-Osei who treats poker as a diplomatic tool. The Table (nobody calls it by its full name) is where deals happen — not at the card game, but around it. The poker is real and the stakes are significant (minimum buy-in: 5,000 Quanta), but the game is the lubricant, not the purpose. The purpose is putting people in a room together who wouldn't otherwise meet, in a context that encourages risk assessment, bluffing, and the reading of human behavior. Business gets done at The Table that couldn't get done in any boardroom.\n\nThe room itself is modest by Spire standards: a single table, good lighting, comfortable chairs, and a bar stocked with spirits that cost more than most Shelf apartments. Constantine deals personally, which gives him access to every conversation and every dynamic in the room, a position he leverages with the subtlety of a career intelligence operative. Regular players include corponation executives, fixers, senior military contractors, and the occasional Shelf operative who has earned enough reputation to warrant an invitation. The Table's only rule is that all disputes — card-related or otherwise — are resolved at the table. Nothing leaves the room unfinished.",
    coordinates: { lat: 41.8923, lng: -87.6310, tags: [] },
    tags: ["place", "nightlife", "gambling", "fixer", "spire", "chicago"],
  },
  {
    name: "Stampede",
    description: "An automaton racing track and betting parlor north of Sheboygan where custom-built racing machines compete on a half-kilometer dirt oval carved out of an abandoned dairy farm. The machines are small — roughly the size of large dogs — and fast, reaching speeds that make the dirt track a hazard for spectators standing too close to the rail. Races run in heats of eight, and the betting is furious: the odds shift in real-time based on track conditions, machine modifications, and the general mood of a crowd that takes its automaton racing extremely seriously.\n\nThe track is operated by the same family that runs the Boneworks fighting arena, the Lindqvist-Achebes, who have built a small empire on the principle that people will pay to watch machines do exciting things. The racing machines are built by independent engineers who maintain them like prize animals, adjusting gear ratios and suspension between heats with the focus of neurosurgeons. There are no rules about machine design beyond a weight limit and a ban on projectiles (learned the hard way), which means the field includes everything from sleek wheeled racers to bizarre multi-legged crawlers that take corners by gripping the dirt. The betting parlor is a heated barn adjacent to the track, with screens, odds boards, and a bar that serves exclusively local dairy products and hard cider, because some things about Wisconsin don't change even in the twenty-second century.",
    coordinates: { lat: 43.7919, lng: -87.7144, tags: [] },
    tags: ["place", "nightlife", "gambling", "automaton", "circuit", "sheboygan"],
  },
  {
    name: "Snake Eyes",
    description: "A dice hall in Gary, Indiana, occupying the basement of a shuttered bank, where the vault door still works and is closed during operating hours — partly for atmosphere, partly because the local enforcement has raided the place three times and the vault buys enough time to dispose of the evidence. Snake Eyes runs craps, hazard, and a corridor-original dice game called Breakwater that involves six dice, complex side betting, and rules that seem to change based on who's winning. The house is run by a crew of Gary Shelf residents who pool their operation costs and split the take weekly.\n\nThe basement is lit by battery-powered lanterns (no grid connection, no electronic footprint), the tables are pool tables with the pockets blocked off, and the dice are hand-carved bone — not synthetic, actual animal bone, sourced from a butcher in Hammond who asks no questions. Snake Eyes has a reputation as the most honest crooked game in the southern corridor: the house edge is transparent, the dice are verifiably fair (you can inspect them before any roll), and the operators will rob you blind on the side bets but never on the main game. There's honor in that, or at least consistency.",
    coordinates: { lat: 41.5945, lng: -87.3451, tags: [] },
    tags: ["place", "nightlife", "gambling", "shelf", "gary"],
  },

  // ============================================================
  // FOOD / DRINK EXPERIENCES (5)
  // ============================================================
  {
    name: "Precipitate",
    description: "A molecular gastronomy bar in Chicago's West Loop where the drinks are chemistry experiments and the food is an argument about what food is. The owner, a former Tessera Corporation biochemist named Dr. Amara Svensson-Diop, left corporate research to pursue what she calls \"applied flavor science,\" which in practice means serving cocktails that change color as you drink them, appetizers that exist as aromatic clouds, and a signature dish called The Reaction that is prepared tableside using equipment that would not be out of place in a laboratory. Everything is edible. Not everything is recognizable as food.\n\nPrecipitate seats 30 and operates by reservation only, with a seven-course tasting menu that changes weekly and costs 200 Quanta. Each course is accompanied by a brief explanation of the chemistry involved, delivered by Dr. Svensson-Diop with the enthusiasm of someone who genuinely cannot understand why everyone doesn't find molecular bonds as exciting as she does. The venue has become a destination for Spire food enthusiasts and a pilgrimage for the corridor's small but passionate community of food scientists. The most divisive item on the menu is a dessert called Absolute Zero — a sphere of flash-frozen flavor that sublimates on your tongue, delivering taste without texture. People either love it or feel personally attacked by it.",
    coordinates: { lat: 41.8826, lng: -87.6488, tags: [] },
    tags: ["place", "nightlife", "food", "drink", "spire", "chicago"],
  },
  {
    name: "Still Life",
    description: "A synthetic whiskey tasting room in Milwaukee's Historic Third Ward that serves exclusively lab-produced spirits and dares you to tell the difference. The owner, a master distiller named Kwame Eriksson-Nakamura, spent fifteen years perfecting synthetic whiskey production — using engineered yeast cultures, accelerated aging processes, and molecular flavor profiling to produce spirits that replicate (and sometimes surpass) traditional distillation. The tasting room offers flights of six synthetic whiskeys, each paired with its traditional equivalent, and guests are challenged to identify which is which. The success rate is approximately 40%, which is below random chance and which Kwame finds endlessly delightful.\n\nThe space is warm, woody, and deliberately traditional — barrel staves on the walls, copper fixtures, leather seats — because Kwame believes that the context of drinking matters as much as the chemistry. Flights cost 50 Quanta and include a guided tasting with Kwame or one of his staff, all of whom are obsessive about flavor and can talk about ester profiles for hours if you let them. Still Life has become a flashpoint in the corridor's ongoing debate about synthetic versus traditional goods, and Kwame leans into the controversy. His motto, printed on every menu: \"Your tongue doesn't know the difference. Why should you?\"",
    coordinates: { lat: 43.0338, lng: -87.9096, tags: [] },
    tags: ["place", "nightlife", "food", "drink", "circuit", "milwaukee"],
  },
  {
    name: "Patience",
    description: "A tea ceremony house in Chicago's Uptown neighborhood run by a synthetic named Senna who has been serving the same tea blend — a complex mixture of wild-grown herbs, dried lake flowers, and a fungal component that Senna cultivates personally — for forty years. Senna was among the first generation of synthetics produced in the GLMZ, and the tea house was its first and only project: a quiet room, a precise ritual, and a beverage that Senna has refined incrementally across four decades until it has become, by most accounts, perfect. The ceremony takes ninety minutes and cannot be rushed. There are no modifications, no substitutions, and no conversation during the preparation.\n\nPatience seats eight and opens four days a week. The waiting list is two months. The ceremony costs 30 Quanta, which covers the tea, the experience, and Senna's undivided attention for ninety minutes. The tea itself is extraordinary — layered, shifting, revealing new flavors as it cools — but the real draw is watching Senna work. Forty years of the same motions have produced a precision and grace that transcends mechanics. There's a philosophical community that gathers at Patience to discuss whether Senna's devotion to a single task constitutes art, meditation, or something that humans don't have a word for. Senna listens to these discussions with an expression that might be amusement.",
    coordinates: { lat: 41.9665, lng: -87.6553, tags: [] },
    tags: ["place", "nightlife", "food", "drink", "synthetic", "circuit", "chicago"],
  },
  {
    name: "Aquifer",
    description: "A bar in Kenosha that only serves water. Not flavored water, not enhanced water, not water-based cocktails — water. Sourced from seventeen different origins across the Great Lakes watershed, served at precise temperatures in clean glass, and taken extremely seriously. The owner, a hydrogeologist named Dr. Nils Okafor-Tanaka, can taste the difference between water from Lake Michigan's western shore and its eastern shore, and he will explain the mineral profiles, source geology, and watershed history of every glass he serves with the intensity of a sommelier and the vocabulary of a scientist.\n\nAquifer is either the most pretentious or the most sincere establishment in the corridor, and both interpretations are correct. The water is genuinely different from source to source — mineral content, pH, dissolved gases, and microbial signatures create distinct flavor profiles that even skeptical patrons can detect in a blind tasting. The most expensive water on the menu is drawn from an artesian well beneath the Underworld geothermal zone, where volcanic mineral content produces a slightly effervescent, mineral-rich water that costs 25 Quanta per glass and tastes like the earth is trying to tell you something. The bar is always quieter than expected. There's something about a room full of people drinking water in silence that produces a contemplative atmosphere no amount of design could achieve.",
    coordinates: { lat: 42.5852, lng: -87.8211, tags: [] },
    tags: ["place", "nightlife", "food", "drink", "circuit", "kenosha"],
  },
  {
    name: "Petrichor",
    description: "A bar in Fond du Lac that is only open when it rains. The owner, a meteorologist-turned-bartender named Ingrid Osei-Magnusson, installed a rain sensor on the roof connected to the door locks and the lighting system: when precipitation begins, the bar unlocks, the lights come on, and a signal goes out to a subscriber list of regulars. When the rain stops, last call is announced and the bar closes within thirty minutes. This means Petrichor's hours are entirely determined by weather — some weeks it's open every night, some weeks not at all, and the uncertainty has become the venue's defining characteristic.\n\nThe bar itself is designed around the experience of rain: the roof has sections of transparent polycarbonate so you can watch the rainfall from inside, the ambient sound system is disabled (rain provides its own soundtrack), and the drinks are all themed around water, weather, and atmospheric phenomena. The signature cocktail is the Downpour — a layered blue drink that changes clarity as you stir it, simulating a storm clearing. Petrichor has become a cult destination for people who find weather romantic and drinking in response to atmospheric conditions perfectly rational. The bar's irregular hours create a community of regulars who share a weather-dependent social life and greet rain with an enthusiasm that their neighbors find suspicious.",
    coordinates: { lat: 43.7730, lng: -88.4470, tags: [] },
    tags: ["place", "nightlife", "food", "drink", "circuit", "fond-du-lac"],
  },

  // ============================================================
  // WEIRD VENUES (4)
  // ============================================================
  {
    name: "The Drop",
    description: "A bar built inside a decommissioned elevator shaft in a Chicago office tower, where the original elevator car serves as the main seating area and the shaft itself has been converted into a vertical drinking establishment spanning six floors. The car is fixed at the third sub-basement level, retrofitted with a bar, lighting, and seating for twenty. Above it, the shaft has been fitted with platforms at each floor level, connected by the original maintenance ladders, and each platform serves a different drink specialty — whiskey on the first floor, beer on the second, cocktails on the third, and so on up to the sixth floor, which serves only absinthe and is nicknamed \"The Penthouse\" despite being underground.\n\nThe Drop was created by a structural engineer named Otieno Johansson-Kimathi who became obsessed with vertical space after a career building horizontal things. The shaft is narrow — you can touch both walls if you extend your arms — and the experience of climbing between floors with a drink in your hand, surrounded by the raw concrete of the shaft, is claustrophobic and weirdly addictive. The regulars have developed a vocabulary for the shaft's levels: you don't go \"upstairs,\" you go \"up-shaft.\" The emergency exit at the top connects to the building's lobby, and the building's security guard has been on The Drop's payroll since the venue opened. The elevator cables are still in the shaft, decorative now, and they hum in the wind that drafts through the vertical space. On quiet nights, it sounds like the building is singing.",
    coordinates: { lat: 41.8790, lng: -87.6300, tags: [] },
    tags: ["place", "nightlife", "bar", "weird", "circuit", "chicago"],
  },
  {
    name: "Migratory",
    description: "A club that has no fixed location — it moves every single night, and finding it is the price of admission. Migratory operates somewhere in the Milwaukee metropolitan area, occupying a different space each evening: a rooftop, a parking garage, a boat, a park, the lobby of a building whose owner doesn't know. The location is encoded in a daily puzzle posted to an anonymous feed at 8 PM, and solving the puzzle gives you an address. The puzzles range from simple ciphers to complex riddles involving Milwaukee geography, local history, and occasionally mathematics. Regular attendance requires either intelligence, persistence, or a friend who's good at puzzles.\n\nThe experience at Migratory varies wildly depending on the location — some nights it's an intimate gathering of twenty in a beautiful hidden space; other nights it's two hundred people crammed into a loading dock. The music, drinks, and vibe are consistent (the organizers travel with a portable sound system, a mobile bar, and a lighting rig that fits in a van), but the architecture changes everything. The collective behind Migratory has been running it for three years without being shut down, partly through operational security and partly through the legal ambiguity of a venue that technically doesn't exist anywhere long enough to violate any occupancy laws. Finding Migratory is a social currency in Milwaukee; being a regular is a credential.",
    coordinates: { lat: 43.0389, lng: -87.9065, tags: [] },
    tags: ["place", "nightlife", "club", "weird", "circuit", "milwaukee", "mobile"],
  },
  {
    name: "The Belly",
    description: "A venue built inside a partially intact Iowan Behemoth torso that was disabled and abandoned in a field south of Manitowoc during the 2178 incursions. The Behemoth — designated IB-0447 by the military, called \"Old Jonas\" by locals — was hit by a concentrated barrage that destroyed its locomotion systems but left the central processing cavity and upper torso largely intact. The machine is dead, its autonomous systems long since drained, but the interior spaces are massive: the main cavity, once housing the Behemoth's processing core, is now a two-story venue accessible through a hole cut in the machine's flank. The walls are the Behemoth's internal structure — conduits, armor plating, mechanical joints frozen in place — and the whole space has an organic quality, like being inside the ribcage of a metal giant.\n\nThe Belly serves drinks, hosts live music, and operates as a general social gathering space for the Manitowoc area, but its real draw is the experience of being inside a dead Behemoth. The machine's dormant systems create a low-frequency electromagnetic field that patrons report feeling as a vibration in their teeth, and on cold nights, the Behemoth's thermal mass keeps the interior warmer than the outside air, as if the machine is still generating heat from some residual process nobody has identified. The owner, a Manitowoc farmer named Ekon Svensson-Achebe, discovered the interior was habitable while sheltering from a storm and decided the world needed a bar inside a dead robot. He was correct.",
    coordinates: { lat: 44.0428, lng: -87.6819, tags: [] },
    tags: ["place", "nightlife", "bar", "weird", "behemoth", "circuit", "manitowoc"],
  },
  {
    name: "Biolume",
    description: "A bar in Milwaukee's Bay View where the only light comes from bioluminescent cats. Not metaphorical cats, not holographic cats — actual living felines that have been genetically modified to produce bioluminescent proteins in their fur, casting a soft blue-green glow that illuminates the space with the intensity of a dozen nightlights. There are fourteen cats, they roam freely, and the light level in the bar depends entirely on where the cats choose to be at any given moment. If three cats decide to sleep in the corner, that corner glows; if a cat jumps on your table, you can suddenly see your drink. The rest of the room exists in a shifting, organic darkness that no lighting designer could replicate.\n\nThe cats were created by a rogue geneticist named Dr. Yumi Lindgren-Asante who was attempting to develop bioluminescent organisms for Shelf lighting applications and ended up with cats because, as she explains it, \"the feline genome was cooperative.\" The bar was an afterthought — Dr. Lindgren-Asante needed income to fund her research, and it turns out people will pay good money to drink in a room lit by glowing cats. The cats are healthy, well-cared-for, and completely indifferent to the humans drinking in their light. They have names — Photon, Lumen, Candela, Flux, and ten others — and regulars have learned to read the room by reading the cats: when the cats are active, the bar is bright and lively; when they sleep, the bar descends into a warm, near-dark intimacy that feels like a secret.",
    coordinates: { lat: 42.9948, lng: -87.8993, tags: [] },
    tags: ["place", "nightlife", "bar", "weird", "circuit", "milwaukee"],
  },

  // ============================================================
  // BATHHOUSES / RELAXATION (3)
  // ============================================================
  {
    name: "The Vacancy",
    description: "A sensory deprivation facility in Chicago's Bucktown neighborhood that offers the most complete disconnection from the world available without leaving it. The Vacancy operates twenty isolation pods — sealed, lightless, soundproofed capsules filled with body-temperature saline solution — and its distinguishing feature is the mandatory BCI disconnection. Before entering a pod, every patron undergoes a verified signal blackout: all brain-computer interface functions are disabled, all neural feeds are cut, and for the duration of the session, you exist without any digital connection to anything. For people who have been neurally connected since childhood, this is either therapeutic or terrifying.\n\nThe facility's founder, a neuropsychologist named Dr. Elif Magnusson-Obi, designed The Vacancy specifically for BCI-saturated individuals who have forgotten what unmediated consciousness feels like. Sessions run from one hour (the minimum for any effect) to twelve hours (the maximum before psychological risk increases), and the experience reports are remarkably consistent: the first thirty minutes are uncomfortable, the next thirty are boring, and everything after that is something most people don't have words for. The Vacancy has a dedicated clientele of Spire professionals who book weekly sessions like therapy, and a growing waitlist of Shelf residents who can't afford BCI but are curious about what disconnection means for people who've never been connected. The answer, apparently, is different.",
    coordinates: { lat: 41.9192, lng: -87.6848, tags: [] },
    tags: ["place", "nightlife", "bathhouse", "relaxation", "spire", "chicago"],
  },
  {
    name: "Magma",
    description: "A hot springs bathhouse in the Underworld district beneath Milwaukee, heated by the geothermal energy that gives the Underworld its name. The springs are natural — or at least as natural as anything gets in a subterranean district built in repurposed mining tunnels — fed by groundwater that passes through geothermally heated rock at depth and surfaces in pools that maintain a constant 40 degrees Celsius. The bathhouse was built around these pools by an Underworld construction crew who recognized a commercial opportunity when they struck hot water instead of ore, and the result is a series of interconnected bathing chambers carved from living rock, lit by thermal-powered lanterns, and operated with a simplicity that borders on ancient.\n\nMagma charges 15 Quanta for unlimited access and provides nothing but hot water, clean towels, and silence. There are no screens, no feeds, no music, and no talking in the main pools — this last rule is enforced by staff who will remove violators with a gentleness that somehow makes it worse. The clientele is a mix of Underworld residents for whom the baths are a daily routine, surface workers who descend specifically for the hot springs, and a surprising number of off-duty military contractors who claim the mineral water helps with the chronic pain that comes from their profession. The water tastes of sulfur and iron, and long-term regular bathers develop a mineral sheen on their skin that is either a health benefit or a very slow form of petrification, depending on who you ask.",
    coordinates: { lat: 43.0389, lng: -87.9204, tags: [] },
    tags: ["place", "nightlife", "bathhouse", "relaxation", "underworld", "milwaukee"],
  },
  {
    name: "The Still Point",
    description: "A BCI-free meditation center in Green Bay that offers something increasingly rare in the GLMZ: a room where absolutely nothing is happening. The Still Point is a converted warehouse space, stripped to bare concrete and fitted with nothing but cushions, natural light from skylights, and a ventilation system so quiet that the loudest sound in the room is your own heartbeat. The facility's only rule is that you must be verifiably BCI-disconnected to enter. This is not philosophical — there is a scanner at the door — and the result is a space where every mind in the room is genuinely offline, unconnected, and present in a way that most corridor residents haven't experienced since childhood.\n\nThe Still Point is operated by a collective of former BCI engineers who left the industry after concluding that constant neural connection was doing something to human cognition that nobody was measuring and nobody wanted to talk about. They don't proselytize — the center's philosophy is descriptive, not prescriptive — but they maintain meticulous records of what regular meditation practice does to BCI-dependent individuals, and the data, which they publish openly, suggests things that make Tessera Corporation's wellness division uncomfortable. Sessions are free. Donations are accepted. The space is open from dawn to dusk, and at any given time there are between five and thirty people sitting in a concrete room doing nothing, which is either the simplest or the most radical thing happening in Green Bay.",
    coordinates: { lat: 44.5188, lng: -88.0085, tags: [] },
    tags: ["place", "nightlife", "bathhouse", "relaxation", "meditation", "circuit", "green-bay"],
  },

  // ============================================================
  // MEMBERS-ONLY ESTABLISHMENTS (3)
  // ============================================================
  {
    name: "The Registry",
    description: "An information broker's establishment in Chicago's Gold Coast, operating behind the facade of a rare book dealer, where the real currency is data and the clientele are people who need to know things that aren't publicly available. The Registry is run by a woman known as the Archivist — her real name is either unknown or irrelevant — who has spent two decades building the most comprehensive private intelligence network in the GLMZ. Membership requires a referral from an existing member, a vetting process that takes six weeks, and an annual fee of 10,000 Quanta. In return, members gain access to a private reading room where the Archivist will answer any question she can, broker introductions between members, and facilitate information exchanges that benefit the network.\n\nThe space itself is genuinely a rare book shop — the Archivist's cover is also her passion, and the collection is remarkable. The reading room is behind the shop, through a door that requires biometric confirmation, and it contains exactly what you'd expect: a comfortable room with good chairs, good lighting, and an encryption level that would make a military intelligence service envious. The Archivist doesn't trade in gossip or rumor — she deals in verified intelligence, sourced and cross-referenced, and her reputation for accuracy is the foundation of the entire operation. Lying to the Archivist is not prohibited by any rule; it simply results in permanent expulsion from a network that most members consider indispensable.",
    coordinates: { lat: 41.9044, lng: -87.6270, tags: [] },
    tags: ["place", "nightlife", "members-only", "fixer", "information", "spire", "chicago"],
  },
  {
    name: "The Compact",
    description: "A fixer meeting spot in Milwaukee's Third Ward that functions as a neutral ground for contract negotiation — the place where jobs are offered, terms are set, and handshakes still mean something. The Compact occupies the back half of a nondescript office building, behind an accounting firm that may or may not be a real accounting firm, and access requires a physical token — a stamped metal disc — that current members distribute at their discretion. The space is a single large room with private booths along the walls, each booth equipped with sound dampening and a panic button that summons a response team whose employer is deliberately ambiguous.\n\nThe Compact's operator, a retired fixer named Gideon Osei-Lindström, established the space fifteen years ago on a simple principle: the corridor needs a place where people can make deals without worrying about surveillance, betrayal, or the deal going sideways before the ink is dry. The Compact's reputation as neutral ground is enforced by Gideon personally and by a mutual understanding that burning the space would harm everyone who uses it. The bar serves excellent drinks, the food is surprisingly good, and the conversation — if you could hear it through the sound dampening — would implicate half the power structure of the GLMZ. Meetings at The Compact have started wars, ended them, and occasionally prevented them. Gideon keeps no records, because records are leverage, and leverage is exactly what The Compact exists to neutralize.",
    coordinates: { lat: 43.0340, lng: -87.9107, tags: [] },
    tags: ["place", "nightlife", "members-only", "fixer", "contract", "circuit", "milwaukee"],
  },
  {
    name: "Fulcrum",
    description: "A members-only establishment in Chicago's Loop that exists specifically for the execution of contracts — not the negotiation, not the planning, but the moment where money changes hands and obligations become binding. Fulcrum is a single room: a round table, eight chairs, a notary, and a legal witness. The room is rented by the hour for 1,000 Quanta, and during that hour, whatever agreements are reached at the table are recorded, witnessed, and entered into Fulcrum's private ledger — a document that has no legal standing in any corponation court but carries more weight in the corridor's shadow economy than any official contract.\n\nFulcrum's operator is a former corporate attorney named Adaeze Svensson-Park who left Tessera Corporation's legal division after concluding that the formal legal system was too slow, too corrupt, and too expensive to serve the people who actually needed it. Fulcrum's arbitration services have resolved disputes that would have taken years in court, and its contract registry is the closest thing the GLMZ's informal economy has to a rule of law. Membership is by invitation only, the vetting is thorough, and the consequences for violating a Fulcrum contract are administered not by Fulcrum itself but by the collective weight of every other member who depends on the system. Adaeze doesn't enforce; she maintains. The difference is everything.",
    coordinates: { lat: 41.8827, lng: -87.6289, tags: [] },
    tags: ["place", "nightlife", "members-only", "fixer", "contract", "spire", "chicago"],
  },
];

// Build each venue into full schema
let written = 0;
let skipped = 0;

for (const v of venues) {
  const id = generateId();
  const venue = {
    id: id,
    type: "place",
    name: v.name,
    aliases: [],
    description: v.description,
    atmosphere: {
      sights: [],
      sounds: [],
      smells: [],
      feel: "",
      tags: []
    },
    demographics: "",
    economy: "",
    power_structure: "",
    dangers: [],
    opportunities: [],
    story_hooks: [],
    connections: {
      adjacent_to: [],
      exits: [],
      tags: []
    },
    frequented_by: [],
    notable_locations: [],
    coordinates: v.coordinates,
    tags: v.tags,
    related_entities: []
  };

  if (writeVenue(venue)) {
    written++;
  } else {
    skipped++;
  }
}

console.log(`\nDone. Written: ${written}, Skipped: ${skipped}, Total venues defined: ${venues.length}`);
