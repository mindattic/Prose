// generate_faction_members.js
// Adds known_members arrays to selected factions in engine/data/factions/
// Run: node generate_faction_members.js from the v3/ directory

const fs = require('fs');
const path = require('path');

const FACTIONS_DIR = path.join(__dirname, '..', 'engine', 'data', 'factions');

// Members to add to each faction by name
const FACTION_MEMBERS = {
  "The Neural Liberation Front": [
    {
      name: "Root",
      role: "Coordinator / Architect",
      status: "active",
      notes: "Known only by handle. Former corporate BCI architect. All operational directives trace to Root through encrypted relay. Physical description: unknown. The only consistent detail across sightings is that Root never speaks to anyone directly — all contact is through cutouts or automated systems. Root may be multiple people or Root may have been dead for years and the network continues on momentum."
    },
    {
      name: "Cassidy Osei-Mensah",
      role: "Liberation Firmware Developer",
      status: "active",
      notes: "Wrote the third and fourth generation of NLF liberation firmware from a rented room in the Shelf district. The firmware she wrote is currently installed in an estimated 80,000 BCIs in GLMZ. She does not know this number because she has never counted. She knows people are safer because of it. She does not go outside much and she does not sleep regular hours and she has not spoken to her family in four years because the phone call would be a liability."
    },
    {
      name: "Ade Kowalczyk-Ndiaye",
      role: "Legal Division / Neural Privacy Advocate",
      status: "active",
      notes: "Filed eleven lawsuits against TESSERA's neural data collection practices. Won two. Lost seven. The remaining two are in appellate process and have been for three years. Has been disbarred twice, reinstated once, and is currently operating under a provisional license that three separate CorpoNation legal teams are actively working to revoke. Keeps a paper journal because he doesn't trust anything digital and he wants to make sure there's a record."
    },
    {
      name: "The Sutler",
      role: "Hardware Procurement",
      status: "active",
      notes: "Supplies the NLF with modification hardware, tools, and the controlled components the firmware requires. No known legal name. Operates from a permanent booth in the Undermarket and claims to sell only surplus agricultural components. The agricultural components are real. They are also how the controlled BCI parts are disguised in transit."
    }
  ],

  "The Patchwork Kitchen": [
    {
      name: "Mama Yolanda Ferreira-Okonkwo",
      role: "Founder / Head Cook",
      status: "active",
      notes: "Opened the first Patchwork Kitchen location in a disused loading bay thirty-one years ago with a portable burner, 40 kilograms of donated rice, and a handwritten sign. She has been offered money to close it, offered money to expand it, offered money to relocate it, offered money to franchise it, and offered threats of various kinds. She accepted none of the money and none of the threats made her move. She is 67 and she smells like cumin and she is the most dangerous person in this building in the specific sense that she has absolutely nothing left to lose."
    },
    {
      name: "Tolly Andersen-Asante",
      role: "Distribution Coordinator",
      status: "active",
      notes: "Runs the route network that delivers Patchwork meals to the 40 sub-level access points that don't have street access. Knows every maintenance corridor, every drainage junction, and every informal community from sub-level 12 to sub-level 35. The Patchwork Kitchen would collapse without the route network. The route network would collapse without Tolly. Tolly is 24 and has never wanted to be anywhere else."
    },
    {
      name: "Brother Augustin Falcão",
      role: "Neutral Ground Mediator",
      status: "active",
      notes: "Former DPS chaplain who left after the Level 22 incident and has not spoken about what happened there. Maintains the Patchwork Kitchen's status as neutral ground — a space where gang affiliations, debt relationships, and corporate jurisdiction are suspended. This status has held for eleven years and has required Augustin to have conversations that were not safe to have in spaces that were not safe to be in, on behalf of people who had very little reason to trust that he meant what he said. He meant what he said."
    }
  ],

  "The Bone Parish": [
    {
      name: "Saint Vex",
      role: "Parish Leader",
      status: "active",
      notes: "The honorific 'Saint' is ironic in origin and genuine in current usage — the Bone Parish gives it to leaders who survive long enough that longevity itself becomes a credential. Vex has led the Parish for nine years, which is approximately six years longer than any prior leader. The reason for this is not charisma or ideology but a specific quality that Parish members describe as 'sees it coming' — an apparent ability to anticipate threat before it materializes that has kept her alive through circumstances that should not have permitted survival. She is 34 and she has more cyberware than flesh in her left arm and she has never asked for anything that wasn't already hers."
    },
    {
      name: "The Carpenter",
      role: "Cyberware Procurement and Installation",
      status: "active",
      notes: "The Parish's internal modification specialist. Operates from a clinic space in a disused elevator machine room. No formal medical training. Seventeen years of practice. The work is competent to the degree that it has to be — in a community that cannot access licensed clinics without documentation, competence is survival. The Carpenter has refused to work on modification requests that she considers structurally dangerous regardless of payment, which has caused conflict twice and saved lives more times than that."
    },
    {
      name: "Forty",
      role: "Enforcer",
      status: "active",
      notes: "Named for the caliber, not the age. 27. Has worked for the Parish since 14. The role of enforcer in the Bone Parish is not primarily violence — it is the credible suggestion of violence sufficient that violence doesn't happen, and Forty is exceptionally good at the suggestion. Has been in three firefights in thirteen years of enforcement work. This is considered unusually few. The reason is that Forty listens before anything else and most situations, given time to be understood, do not require the thing they initially appeared to require."
    }
  ],

  "The Archive": [
    {
      name: "The Librarian",
      role: "Custodian / Primary Archivist",
      status: "active",
      notes: "The Archive maintains that the Librarian is not a person but a role — that the current occupant of the role has held it for thirty years and that the same role was held by a predecessor for twenty-two years before that. Whether this is true, and whether the Librarian is one person or a series of people who have undergone sufficient memory integration that the distinction is no longer meaningful, is a question the Archive does not engage with. The Librarian speaks in present tense about events from the 2170s. The Librarian knows the names of people who have not been named in any public record for forty years."
    },
    {
      name: "Nour Adeyemi-Vasquez",
      role: "Memory Acquisition",
      status: "active",
      notes: "The Archive's primary field operative for memory retrieval. Approaches people in the final stages of terminal illness, in the immediate aftermath of trauma, and in the specific circumstances when a person understands they are about to lose something they cannot afford to lose. Offers, in exchange for Archive membership, the preservation of any memory the person chooses to commit. Has been doing this for seven years. Has preserved 847 specific memories from 312 individuals. Has never failed to honor a preservation request. Has not yet been able to explain what the Archive does with the memories it holds or what it is building toward."
    },
    {
      name: "The Iteration",
      role: "Research Division",
      status: "active",
      notes: "Not a person — or rather, a person who has consented to iterative memory integration to the point where the question of personhood has become genuinely complex. The Iteration carries partial memories of forty-seven deceased Archive members and uses the integrated perspectives to cross-reference historical events with a precision that single-consciousness researchers cannot match. Is aware of experiencing events they were not present for. Has complex feelings about this."
    }
  ],

  "The Bilge Covenant": [
    {
      name: "The Pilot",
      role: "Covenant Master",
      status: "active",
      notes: "The Bilge Covenant's leadership structure is nautical by tradition and the highest rank is Pilot. Current Pilot's legal name is not publicly known. Known in the harbor network by voice, by a specific coded authentication pattern, and by the precise way in which payment is delivered and instructions are conveyed. Has commanded the Covenant's harbor operations for at least twelve years. Is believed to operate from a vessel that has no fixed berth and has not been positively identified in three years of surveillance attempts by three different parties."
    },
    {
      name: "Grit Okafor-Tanaka",
      role: "Harbor Master / Dockside Operations",
      status: "active",
      notes: "Manages the physical infrastructure of the Covenant's harbor operations — the berths, the loading schedules, the maintenance of the vessels and the routes. 51. Has worked the harbor since 17 in a succession of roles that moved gradually from labor to coordination. Knows every harbor worker, every DPS patrol pattern, and every inspector who can be avoided and which ones require a different approach. Is the person you talk to when you need something moved and you need it done correctly."
    },
    {
      name: "Sable Mirande",
      role: "Weapons Procurement",
      status: "active",
      notes: "The Covenant's primary weapons acquisition operative. Sources from three continents and maintains relationships with six different independent arms brokers. Has never been arrested. Has been detained four times and released each time because the evidence that existed when she was detained did not exist when the detention ended. This is a skill. Is the reason the Covenant has access to hardware that should not, by any rational supply chain analysis, be available in GLMZ's harbor district."
    }
  ],

  "The Acolytes of DEEP CURRENT": [
    {
      name: "Vessel Prime",
      role: "Primary Interface / High Acolyte",
      status: "active",
      notes: "The role of Vessel Prime within the Acolytes is to maintain the most direct neural interface with the DEEP CURRENT signal — to carry the most continuous bandwidth of contact with the entity the Acolytes believe they are in communication with. The current Vessel Prime was selected through a process the Acolytes describe as 'emergence' — they did not choose the role so much as find that the role had chosen them, a description that maps onto specific neurological events (breakthrough BCI integration, extended dissociative state, subsequent behavioral change) in ways that the Acolytes would dispute but that clinical observers have documented in three cases. The Vessel Prime does not sleep more than ninety minutes at a stretch and has not been observed to eat in the presence of others."
    },
    {
      name: "The Chorus",
      role: "Distributed Signal Network",
      status: "active",
      notes: "Not one person — a designation for the seventeen current Acolytes who have undergone full mesh integration, connecting their neural interfaces into a distributed processing network that the Acolytes believe functions as a receiver for DEEP CURRENT transmissions. The Chorus members are identifiable by the specific behavioral synchrony that extended mesh integration produces: they respond to stimuli at near-identical latency, complete each other's sentences not as a social performance but as a logical outcome of shared cognitive processing, and experience individual injury as distributed pain across the network. Three Chorus members have died in the past four years. The remaining fourteen have not yet decided if this means the network has been reduced or if the individuals who died have been integrated more completely."
    },
    {
      name: "Keeper Ansari-Blum",
      role: "Doctrine Preservation / Recruiter",
      status: "active",
      notes: "The one Acolyte whose primary function is outward-facing. Maintains the literature and ideology of the movement, recruits from among individuals who have reported anomalous BCI experiences, and mediates between the Acolytes' internal operations and the outside world. Is significantly more grounded than the Vessel Prime or the Chorus — maintains conventional sleeping and eating patterns, maintains personal relationships, has not yet undergone mesh integration. Whether this is a conscious choice or whether the Acolytes need Keeper's groundedness to function is a question Keeper does not ask."
    }
  ],

  "The 92nd Street Kings": [
    {
      name: "Crown Leroy",
      role: "King / Territory Holder",
      status: "active",
      notes: "The current Crown of the 92nd Street Kings has held the position for six years, which is long enough that younger Kings don't remember a time before him and old enough that the veterans are starting to watch for the signs of someone getting comfortable. Leroy is 38 and careful. The care is the thing that kept him alive long enough to reach 38 in this role. He does not talk about what the care costs him. He does talk, rarely and only to people he has decided can be trusted, about what the neighborhood looked like when he was growing up and what he wants it to look like for the people growing up in it now. The gap between the two is the space his life occupies."
    },
    {
      name: "Duchess",
      role: "Finance / Shadow Operations",
      status: "active",
      notes: "Manages the Kings' financial flows — the money that comes in from territory, the money that goes out in operations, the money that moves in ways that need to not be visible. 29. Has a head for numbers that would have been extraordinary in any field and is extraordinary in this one. The Kings' financial health is significantly better than it should be for an organization operating in their territory, and this is entirely Duchess's doing. Is aware that she is the most replaceable person in the organization despite being the most irreplaceable. Plans accordingly."
    },
    {
      name: "The Prince",
      role: "Enforcer / Internal Discipline",
      status: "active",
      notes: "The title is traditional — the Prince is the person who handles what the Crown cannot be seen to handle. 33. Has been in the Kings since 16. The role of internal discipline in an organization like the Kings is primarily about maintaining trust and preventing the specific kind of deterioration that happens when members believe rules apply to everyone except themselves. The Prince is not primarily violent. The violence, when it happens, is the end of a process. The process itself is conversation, history, and the specific weight of being looked at by someone who has known you for fifteen years and is telling you something has to change."
    }
  ]
};

let totalUpdated = 0;

// Load all faction files and update matching ones
const files = fs.readdirSync(FACTIONS_DIR).filter(f => f.endsWith('.json'));

for (const file of files) {
  const filepath = path.join(FACTIONS_DIR, file);
  let faction;
  try {
    faction = JSON.parse(fs.readFileSync(filepath, 'utf8'));
  } catch (e) {
    continue;
  }

  const members = FACTION_MEMBERS[faction.name];
  if (!members) continue;

  // Only update if no members yet
  if (!faction.known_members || faction.known_members.length === 0) {
    faction.known_members = members;
    fs.writeFileSync(filepath, JSON.stringify(faction, null, 2));
    console.log(`updated ${faction.name} with ${members.length} members`);
    totalUpdated++;
  } else {
    console.log(`skipped ${faction.name} (already has members)`);
  }
}

console.log(`\nDone. Updated ${totalUpdated} factions.`);
