const fs = require('fs');
const path = require('path');
const crypto = require('crypto');

const outputDir = path.resolve(__dirname, '..', 'engine', 'data', 'documents');

if (!fs.existsSync(outputDir)) {
  fs.mkdirSync(outputDir, { recursive: true });
}

function genId() {
  return crypto.randomBytes(16).toString('hex');
}

function writeFragment(fragment) {
  const filePath = path.join(outputDir, `${fragment.id}.json`);
  if (fs.existsSync(filePath)) {
    console.log(`SKIP (exists): ${filePath}`);
    return false;
  }
  fs.writeFileSync(filePath, JSON.stringify(fragment, null, 2), 'utf8');
  console.log(`WROTE: ${filePath}`);
  return true;
}

const fragments = [
  // ── RADIO INTERCEPTS (1-10) ──
  {
    name: "Fragment — Ohio Corridor, Frequency 7.4",
    type: "document",
    document_type: "fragment",
    author: "Unknown / Trucker Radio Intercept",
    date: "unknown",
    classification: "unclassified",
    description: "...three of them on the road, just standing there, and I said to Mike that they looked like [STATIC] ...no, not people, I said they looked like people but they weren't moving right, and then the one in the middle turned its— [SIGNAL LOST]",
    related_entities: [],
    credibility: "unconfirmed",
    story_hooks: ["What were the three figures on the Ohio Corridor road?", "Who is Mike and did he survive?"],
    tags: ["document", "fragment", "incomplete", "transmission", "radio intercept", "ohio corridor", "unidentified entities"]
  },
  {
    name: "Fragment — Lake Huron Coastal Band",
    type: "document",
    document_type: "fragment",
    author: "Unknown / Repeating Station",
    date: "unknown",
    classification: "unclassified",
    description: "...this is [CORRUPTED] station, we are still here, we are still [CORRUPTED] ...if anyone can hear this, the water is rising and the [UNTRANSLATABLE] ...repeat, the water is— [SIGNAL LOST]",
    related_entities: [],
    credibility: "unconfirmed",
    story_hooks: ["What station is broadcasting?", "What is happening to the water near Lake Huron?"],
    tags: ["document", "fragment", "incomplete", "transmission", "radio intercept", "lake huron", "repeating signal", "distress"]
  },
  {
    name: "Fragment — Kentucky Border, Last Received",
    type: "document",
    document_type: "fragment",
    author: "Unknown / Kentucky Border Transmission",
    date: "2198",
    classification: "restricted",
    description: "...the green is moving faster now. It's not growing. It's— [7 second silence] ...we're pulling back to the ridgeline. If you don't hear from us by [CORRUPTED] ...tell my wife I— [TRANSMISSION ENDS]",
    related_entities: [],
    credibility: "unconfirmed",
    story_hooks: ["What is 'the green' expanding across Kentucky?", "This is the last verified transmission from Kentucky — what happened after?"],
    tags: ["document", "fragment", "incomplete", "transmission", "radio intercept", "kentucky", "GLMZ", "final transmission", "the green"]
  },
  {
    name: "Fragment — Unidentified Broadcast, 3 AM Band",
    type: "document",
    document_type: "fragment",
    author: "Unknown / Pirate Radio Intercept",
    date: "unknown",
    classification: "unclassified",
    description: "...and the children will know the way because they have always known the way. The city remembers what the people forget. [4 seconds of a lullaby in an unidentified language] ...do not look for us. We are already— [CARRIER SIGNAL ONLY]",
    related_entities: [],
    credibility: "unconfirmed",
    story_hooks: ["Who is broadcasting on the 3 AM band?", "What does 'the city remembers' mean?", "What language is the lullaby in?"],
    tags: ["document", "fragment", "incomplete", "transmission", "radio intercept", "pirate radio", "the shelf", "3am", "cryptic"]
  },
  {
    name: "Fragment — Convoy Radio, Indiana Dead Zone",
    type: "document",
    document_type: "fragment",
    author: "Unknown / Convoy Operator",
    date: "unknown",
    classification: "unclassified",
    description: "...visibility is zero. The dust is— hold on. Hold on. There's something in the dust. It's— no, it's not a Behemoth, it's smaller but it's— [UNINTELLIGIBLE] ...Dave? Dave, are you seeing this? [12 seconds silence] ...Dave's not responding. I'm turning around. I'm— [SIGNAL LOST]",
    related_entities: [],
    credibility: "unconfirmed",
    story_hooks: ["What did the convoy encounter in the Indiana Dead Zone?", "What happened to Dave?"],
    tags: ["document", "fragment", "incomplete", "transmission", "radio intercept", "indiana", "dead zone", "behemoth", "convoy", "dust"]
  },
  {
    name: "Fragment — Frequency Unknown, Repeating",
    type: "document",
    document_type: "fragment",
    author: "Unknown",
    date: "unknown",
    classification: "unclassified",
    description: "...seven. Seven. Seven. The number is seven. Remember seven. When you see seven, stop. When you hear seven, run. Seven is the— [SIGNAL LOST, RESUMES 4 MINUTES LATER] ...seven. Seven. Seven...",
    related_entities: [],
    credibility: "unconfirmed",
    story_hooks: ["What is the significance of the number seven?", "Who or what is broadcasting this?"],
    tags: ["document", "fragment", "incomplete", "transmission", "radio intercept", "repeating", "numbers", "cryptic", "warning"]
  },
  {
    name: "Fragment — Canadian Border Patrol",
    type: "document",
    document_type: "fragment",
    author: "Canadian Border Patrol",
    date: "unknown",
    classification: "restricted",
    description: "...crossed the border at 0300. Six individuals. No BCIs. Repeat, no BCIs. They were carrying [REDACTED] and spoke a dialect we couldn't— [CORRUPTED] ...detained for questioning but by morning they were— [REDACTED]",
    related_entities: [],
    credibility: "unconfirmed",
    story_hooks: ["Who are these BCI-less individuals crossing the border?", "How did they disappear from custody?"],
    tags: ["document", "fragment", "incomplete", "transmission", "radio intercept", "canadian border", "BCI", "redacted", "disappearance"]
  },
  {
    name: "Fragment — Emergency Broadcast, Source Unverified",
    type: "document",
    document_type: "fragment",
    author: "Unknown / Emergency Broadcast System",
    date: "unknown",
    classification: "unclassified",
    description: "THIS IS NOT A TEST. THIS IS NOT A [CORRUPTED] ...all residents of sectors 7 through 12 are advised to [SIGNAL DEGRADED] ...do NOT look at the [CORRUPTED] ...remain indoors until— [BROADCAST ENDS. NO FOLLOW-UP ISSUED.]",
    related_entities: [],
    credibility: "unconfirmed",
    story_hooks: ["What are residents not supposed to look at?", "Why was no follow-up issued?"],
    tags: ["document", "fragment", "incomplete", "transmission", "radio intercept", "emergency broadcast", "warning", "sectors"]
  },
  {
    name: "Fragment — Shelf CB Channel, Late Night",
    type: "document",
    document_type: "fragment",
    author: "Unknown / Shelf Resident",
    date: "unknown",
    classification: "unclassified",
    description: "...I'm telling you, I saw it. Under the street. Through the grate. Something moving down there that was too big to be in a pipe and too quiet to be a machine. It looked like it was made of [STATIC] ...no, I'm not drunk. I know what I— [CHANNEL CHANGES]",
    related_entities: [],
    credibility: "unconfirmed",
    story_hooks: ["What is living beneath the Shelf's streets?", "How large is the infrastructure beneath the Shelf?"],
    tags: ["document", "fragment", "incomplete", "transmission", "radio intercept", "the shelf", "underground", "CB radio", "creature"]
  },
  {
    name: "Fragment — Behemoth Tracker Field Report",
    type: "document",
    document_type: "fragment",
    author: "Unknown / Behemoth Tracker",
    date: "unknown",
    classification: "restricted",
    description: "...the Foundry has stopped. Repeat, the Foundry has stopped for the first time in recorded observation. It is standing at coordinates [REDACTED] and appears to be— I don't have a word for this. It appears to be waiting. For something. [23 seconds silence] ...it's looking at me. I don't know how I know that. It doesn't have— it's looking at me. [TRANSMISSION ENDS]",
    related_entities: [],
    credibility: "unconfirmed",
    story_hooks: ["Why did the Foundry stop?", "Can Behemoths perceive individual humans?", "What is the Foundry waiting for?"],
    tags: ["document", "fragment", "incomplete", "transmission", "radio intercept", "behemoth", "the foundry", "iowan behemoths", "anomalous behavior"]
  },

  // ── PARTIAL NEWS REPORTS (11-20) ──
  {
    name: "Fragment — Vantablack Evening Broadcast, 2224",
    type: "document",
    document_type: "fragment",
    author: "Vantablack News Network",
    date: "2224",
    classification: "public",
    description: "...the mayor's office issued a statement today regarding the [FEED INTERRUPTED] ...sources within CorpSec confirm that the incident at Building 7C has been [FEED INTERRUPTED] ...we apologize for the technical difficulties. In other news, the weather— [BROADCAST RESUMES NORMALLY]",
    related_entities: [],
    credibility: "unconfirmed",
    story_hooks: ["What happened at Building 7C?", "Why was the broadcast interrupted twice on the same story?"],
    tags: ["document", "fragment", "incomplete", "news report", "vantablack", "corpsec", "censorship", "building 7C"]
  },
  {
    name: "Fragment — Recovered Neural Feed Clip, 2219",
    type: "document",
    document_type: "fragment",
    author: "Unknown / BCI Recording",
    date: "2219",
    classification: "restricted",
    description: "Someone's BCI recorded 14 seconds of what appears to be a news broadcast from a network that doesn't exist, reporting on events that haven't happened, in a city that isn't GLMZ. The anchor's face is partially corrupted. The chyron reads: \"[CITY NAME CORRUPTED] — YEAR 2— [CORRUPTED]\"",
    related_entities: [],
    credibility: "unconfirmed",
    story_hooks: ["What network broadcast this?", "Is this a transmission from another time or place?", "What city is referenced?"],
    tags: ["document", "fragment", "incomplete", "news report", "BCI", "neural feed", "temporal anomaly", "unknown city"]
  },
  {
    name: "Fragment — Shelf Pirate News, Partial Transcript",
    type: "document",
    document_type: "fragment",
    author: "Shelf Pirate News",
    date: "unknown",
    classification: "unclassified",
    description: "...so the official story is that the census numbers are normal. But we pulled the raw data before they cleaned it and the numbers don't— [RECORDING DAMAGED] ...more children than births can account for. Where are they— [RECORDING ENDS]",
    related_entities: [],
    credibility: "unconfirmed",
    story_hooks: ["Where are the extra children coming from?", "Who altered the census data?"],
    tags: ["document", "fragment", "incomplete", "news report", "pirate news", "the shelf", "census", "children", "cover-up"]
  },
  {
    name: "Fragment — Print News, Water Damaged",
    type: "document",
    document_type: "fragment",
    author: "Unknown / Print Publication",
    date: "unknown",
    classification: "unclassified",
    description: "Physical newspaper found in Old Harbor, water-damaged. Only partial headline visible: \"LAZARUS DENIED ACC—\" and partial body text: \"...the facility, described by former employees as a [ILLEGIBLE] ...approximately 200 [ILLEGIBLE] ...maintained in a state of [ILLEGIBLE] ...the spokesperson declined to [REMAINDER DESTROYED]\"",
    related_entities: [],
    credibility: "unconfirmed",
    story_hooks: ["What was Lazarus denied access to?", "What are the 200 subjects maintained in?"],
    tags: ["document", "fragment", "incomplete", "news report", "print", "old harbor", "lazarus", "water damaged", "physical artifact"]
  },
  {
    name: "Fragment — Corporate Internal Memo, Partially Redacted",
    type: "document",
    document_type: "fragment",
    author: "Unknown CorpoNation",
    date: "unknown",
    classification: "classified",
    description: "RE: The [REDACTED] Protocol. As discussed in Tuesday's [REDACTED], the board has approved Phase [REDACTED] of the [REDACTED] initiative. All personnel assigned to Floor 13 are to report to [REDACTED] by [REDACTED]. Non-compliance will result in [REDACTED]. This memo will not be archived.",
    related_entities: [],
    credibility: "unconfirmed",
    story_hooks: ["What protocol requires this level of redaction?", "What happens on Floor 13?", "Why is this memo excluded from archives?"],
    tags: ["document", "fragment", "incomplete", "memo", "corporate", "redacted", "floor 13", "classified", "protocol"]
  },
  {
    name: "Fragment — Academic Journal, Pages Missing",
    type: "document",
    document_type: "fragment",
    author: "Unknown Academic",
    date: "unknown",
    classification: "restricted",
    description: "A study on temporal anomalies in the GLMZ, pages 14-23 torn out. The surviving text references a \"Temporal Displacement Index\" measured at various locations. The conclusion page survives: \"...therefore we can no longer maintain that these anomalies are isolated. The data suggests a [PAGES MISSING] ...which, if correct, implies that the GLMZ is not experiencing temporal anomalies. It IS a temporal anomaly.\"",
    related_entities: [],
    credibility: "unconfirmed",
    story_hooks: ["What data was on the missing pages?", "What does it mean for the GLMZ itself to be a temporal anomaly?"],
    tags: ["document", "fragment", "incomplete", "academic", "GLMZ", "temporal anomaly", "research", "pages missing"]
  },
  {
    name: "Fragment — Weather Report, Wrong City",
    type: "document",
    document_type: "fragment",
    author: "Unknown / Weather Broadcast",
    date: "unknown",
    classification: "unclassified",
    description: "A weather broadcast received on standard GLMZ frequencies that describes weather conditions for a city called \"Meridian 89.\" The weather patterns described are familiar but inverted — where GLMZ had rain, 89 had sun. Where GLMZ was cold, 89 was warm. The broadcast lasted 47 seconds and has not repeated.",
    related_entities: [],
    credibility: "unconfirmed",
    story_hooks: ["Does Meridian 89 exist?", "Why are the weather patterns exactly inverted?"],
    tags: ["document", "fragment", "incomplete", "weather", "glmz", "meridian 89", "inverted", "anomaly", "broadcast"]
  },
  {
    name: "Fragment — Missing Persons Report, Incomplete",
    type: "document",
    document_type: "fragment",
    author: "Unknown / Law Enforcement",
    date: "unknown",
    classification: "unclassified",
    description: "MISSING: [NAME CORRUPTED], age [CORRUPTED], last seen in the vicinity of [CORRUPTED] Block. Description: approximately [CORRUPTED] tall, wearing [CORRUPTED]. Distinguishing feature: [CORRUPTED]. If found, do NOT approach. Contact [NUMBER CORRUPTED] immediately. This individual may be [REMAINDER CORRUPTED]",
    related_entities: [],
    credibility: "unconfirmed",
    story_hooks: ["Why should this person not be approached?", "What is the missing individual capable of?"],
    tags: ["document", "fragment", "incomplete", "missing persons", "warning", "corrupted", "law enforcement"]
  },
  {
    name: "Fragment — Old Harbor Fishing Report",
    type: "document",
    document_type: "fragment",
    author: "Unknown / Old Harbor Fisher",
    date: "unknown",
    classification: "unclassified",
    description: "...the catch was normal until Thursday. Thursday the nets came up with [RECORDING DAMAGED] ...not fish. Not exactly. They had [RECORDING DAMAGED] ...we threw them back. Most of them swam away. The one that didn't [RECORDING DAMAGED] ...we don't fish that sector anymore.",
    related_entities: [],
    credibility: "unconfirmed",
    story_hooks: ["What came up in the nets?", "What happened to the one that didn't swim away?"],
    tags: ["document", "fragment", "incomplete", "old harbor", "fishing", "mutation", "water", "creature"]
  },
  {
    name: "Fragment — Transit Authority Announcement",
    type: "document",
    document_type: "fragment",
    author: "Transit Authority",
    date: "unknown",
    classification: "unclassified",
    description: "Attention passengers: The 7:15 service to [DESTINATION CORRUPTED] has been canceled due to [REASON CORRUPTED]. Alternative transit will be provided via [ROUTE CORRUPTED]. We apologize for the [CORRUPTION INTENSIFIES] ...the 7:15 service has always been canceled. There has never been a 7:15 service. Please disregard this announcement.",
    related_entities: [],
    credibility: "unconfirmed",
    story_hooks: ["What is the 7:15 service?", "Why does the announcement contradict itself?", "Is the transit system sentient?"],
    tags: ["document", "fragment", "incomplete", "transit", "announcement", "paradox", "glitch", "surreal"]
  },

  // ── DATA LOGS AND RECORDS (21-30) ──
  {
    name: "Fragment — BCI Error Log, Anomalous",
    type: "document",
    document_type: "fragment",
    author: "Unknown / BCI Diagnostic System",
    date: "unknown",
    classification: "restricted",
    description: "A BCI diagnostic log that shows the device attempting to connect to a neural mesh that doesn't exist. The mesh ID resolves to no known network. The connection attempts occur at exactly 3:17 AM every night. The user is asleep. The BCI is not in sleep mode during these attempts.",
    related_entities: [],
    credibility: "unconfirmed",
    story_hooks: ["What network is the BCI trying to reach?", "Why 3:17 AM specifically?", "Is the user aware?"],
    tags: ["document", "fragment", "incomplete", "data log", "BCI", "neural mesh", "3:17 AM", "anomaly", "diagnostic"]
  },
  {
    name: "Fragment — Elevator Maintenance Record",
    type: "document",
    document_type: "fragment",
    author: "Building Maintenance System",
    date: "unknown",
    classification: "restricted",
    description: "Log showing that the elevator in a Ghost Building traveled to Floor 13 seventeen times in one month. Floor 13 does not exist. The elevator's weight sensor recorded passengers on each trip. The cumulative weight varied but averaged 73 kg — consistent with one adult human.",
    related_entities: [],
    credibility: "unconfirmed",
    story_hooks: ["Who is riding the elevator to a floor that doesn't exist?", "What is on Floor 13?", "Why does 73 kg recur?"],
    tags: ["document", "fragment", "incomplete", "data log", "elevator", "ghost building", "floor 13", "73 kg", "anomaly"]
  },
  {
    name: "Fragment — Water Treatment Sensor Data",
    type: "document",
    document_type: "fragment",
    author: "Water Treatment Monitoring System",
    date: "unknown",
    classification: "restricted",
    description: "Sensor readings from the Shelf water supply showing a compound that the system cannot identify. The compound appears for 4 hours every 11 days on a precise schedule. It is not harmful. Chemical analysis returns: \"STRUCTURE: [UNRESOLVABLE]. ORIGIN: [UNRESOLVABLE]. RECOMMENDATION: [NO DATA].\"",
    related_entities: [],
    credibility: "unconfirmed",
    story_hooks: ["What is the unidentifiable compound?", "Why does it appear on a precise 11-day schedule?", "Who or what is introducing it?"],
    tags: ["document", "fragment", "incomplete", "data log", "water treatment", "the shelf", "unknown compound", "schedule", "anomaly"]
  },
  {
    name: "Fragment — Automaton Behavioral Log",
    type: "document",
    document_type: "fragment",
    author: "Sentry Automaton System",
    date: "unknown",
    classification: "restricted",
    description: "A sentry automaton's log showing it detected, tracked, and classified an entity that does not appear on any of its cameras. The classification returned was a category the automaton's software does not contain: \"RECOGNIZED.\"",
    related_entities: [],
    credibility: "unconfirmed",
    story_hooks: ["What did the automaton detect that its cameras could not see?", "How did it classify something outside its programming?"],
    tags: ["document", "fragment", "incomplete", "data log", "automaton", "sentry", "classification error", "anomaly", "invisible entity"]
  },
  {
    name: "Fragment — Power Grid Anomaly Report",
    type: "document",
    document_type: "fragment",
    author: "Power Grid Monitoring / Unknown Engineer",
    date: "unknown",
    classification: "restricted",
    description: "Sector 14 drew 340% of baseline power for 0.7 seconds at 03:17:44.002. No equipment in Sector 14 accounts for this draw. The spike originated from [COORDINATES RESOLVE TO EMPTY LOT]. Recommend: [FIELD LEFT BLANK BY RESPONDING ENGINEER].",
    related_entities: [],
    credibility: "unconfirmed",
    story_hooks: ["What drew 340% power from an empty lot?", "Why did the engineer leave the recommendation blank?", "03:17 again — what happens at that time?"],
    tags: ["document", "fragment", "incomplete", "data log", "power grid", "sector 14", "3:17 AM", "anomaly", "empty lot"]
  },
  {
    name: "Fragment — Lazarus Medical Record, Leaked",
    type: "document",
    document_type: "fragment",
    author: "Lazarus Group / Leaked",
    date: "unknown",
    classification: "classified",
    description: "Patient file with most fields redacted. Visible: Age: 9. Condition: [REDACTED]. Treatment: [REDACTED]. Notes: \"Patient demonstrates cognitive function consistent with [REDACTED] years of age. Neural architecture shows patterns of [REDACTED]. Recommend transfer to [REDACTED] facility. DO NOT discharge to general population.\"",
    related_entities: [],
    credibility: "unconfirmed",
    story_hooks: ["What cognitive age does this 9-year-old demonstrate?", "What are the unusual neural patterns?", "Why can't the patient be released?"],
    tags: ["document", "fragment", "incomplete", "medical record", "lazarus", "redacted", "child", "neural anomaly", "classified"]
  },
  {
    name: "Fragment — Shipping Manifest, Anomalous",
    type: "document",
    document_type: "fragment",
    author: "Old Harbor Port Authority",
    date: "unknown",
    classification: "restricted",
    description: "A cargo manifest for a shipment that arrived at the Old Harbor docks from a port that doesn't exist. Contents listed as \"[CLASSIFICATION PENDING]\" — 47 sealed containers, each weighing exactly 73 kg. The containers were collected by a subsidiary that traces back to no parent CorpoNation. The containers have not been opened.",
    related_entities: [],
    credibility: "unconfirmed",
    story_hooks: ["What is in the 47 containers?", "73 kg again — is this weight significant?", "What is the phantom subsidiary?"],
    tags: ["document", "fragment", "incomplete", "shipping manifest", "old harbor", "73 kg", "phantom port", "containers", "anomaly"]
  },
  {
    name: "Fragment — CorpSec Incident Log, Partial",
    type: "document",
    document_type: "fragment",
    author: "CorpSec",
    date: "unknown",
    classification: "classified",
    description: "Responding officers arrived at [LOCATION REDACTED] at 22:14. Found [REDACTED] in a state of [REDACTED]. Officers describe the scene as [REDACTED]. One officer's BCI malfunctioned upon entry and has not been [REDACTED]. Recommend: classifying this incident as [REDACTED]. No further action.",
    related_entities: [],
    credibility: "unconfirmed",
    story_hooks: ["What did the officers find?", "Why did the BCI malfunction?", "Why was no further action taken?"],
    tags: ["document", "fragment", "incomplete", "incident log", "corpsec", "redacted", "BCI malfunction", "classified", "cover-up"]
  },
  {
    name: "Fragment — Personal Journal, Last Entry",
    type: "document",
    document_type: "fragment",
    author: "Unknown Individual",
    date: "unknown",
    classification: "unclassified",
    description: "Day 147. I've been mapping the pattern. It's not random. The anomalies follow a [PAGE TORN] ...if you overlay the locations on a map and connect them, they form [PAGE TORN] ...I showed Dr. [NAME TORN] and she went pale. She said I needed to stop. She said [PAGE TORN] ...I'm not stopping. Tomorrow I'm going to [REMAINDER MISSING]",
    related_entities: [],
    credibility: "unconfirmed",
    story_hooks: ["What pattern do the anomalies form on a map?", "What did the doctor recognize?", "Did this person survive their next expedition?"],
    tags: ["document", "fragment", "incomplete", "journal", "personal", "pattern", "anomalies", "mapping", "last entry", "investigation"]
  },
  {
    name: "Fragment — The Last Complete Sentence",
    type: "document",
    document_type: "fragment",
    author: "Unknown",
    date: "unknown",
    classification: "unclassified",
    description: "A document consisting of a single line, recovered from a data shard of unknown origin: \"They built the city on top of it, and it has been dreaming ever since.\"",
    related_entities: [],
    credibility: "unconfirmed",
    story_hooks: ["What is beneath the city?", "What does it mean for something beneath a city to dream?"],
    tags: ["document", "fragment", "incomplete", "data shard", "cryptic", "origin unknown", "dreaming", "beneath the city"]
  }
];

// Assign IDs and write
let written = 0;
let skipped = 0;

for (const frag of fragments) {
  frag.id = genId();
  if (writeFragment(frag)) {
    written++;
  } else {
    skipped++;
  }
}

console.log(`\nDone. Written: ${written}, Skipped: ${skipped}, Total: ${fragments.length}`);
