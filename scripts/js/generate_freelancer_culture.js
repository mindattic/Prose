// generate_freelancer_culture.js
// Generates 40 in-world documents about freelancer life in GLMZ 2200
// Output: engine/data/documents/ (one JSON file per document)
// Resume-safe: skips documents whose file_name already exists

const fs = require('fs');
const path = require('path');
const crypto = require('crypto');
const https = require('https');

const settings = JSON.parse(fs.readFileSync(
  path.join(process.env.LOCALAPPDATA, 'MindAttic', 'StreetSamurai', 'Settings.json'), 'utf8'));
const API_KEY = settings.ApiKey;
const MODEL = 'claude-sonnet-4-6';
const OUTPUT_DIR = path.resolve(__dirname, '..', 'engine', 'data', 'documents');
const WAIT_MS = 3000;

const limitIdx = process.argv.indexOf('--limit');
const DOC_LIMIT = limitIdx !== -1 ? parseInt(process.argv[limitIdx + 1]) : null;

if (!fs.existsSync(OUTPUT_DIR)) fs.mkdirSync(OUTPUT_DIR, { recursive: true });

function sleep(ms) { return new Promise(r => setTimeout(r, ms)); }

function generateId() {
  return crypto.randomBytes(16).toString('hex');
}

function slugify(name) {
  return name.toLowerCase()
    .replace(/['']/g, '')
    .replace(/[^a-z0-9]+/g, '_')
    .replace(/^_|_$/g, '')
    .slice(0, 80);
}

function callClaude(system, user, maxTokens = 8192) {
  return new Promise((resolve, reject) => {
    const body = JSON.stringify({
      model: MODEL,
      max_tokens: maxTokens,
      temperature: 1.0,
      system: system,
      messages: [{ role: 'user', content: user }]
    });
    const req = https.request({
      hostname: 'api.anthropic.com',
      path: '/v1/messages',
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'x-api-key': API_KEY,
        'anthropic-version': '2023-06-01',
      }
    }, res => {
      let data = '';
      res.on('data', chunk => data += chunk);
      res.on('end', () => {
        try {
          const j = JSON.parse(data);
          if (j.content && j.content[0]) resolve(j.content[0].text);
          else reject(new Error(data.substring(0, 500)));
        } catch (e) { reject(e); }
      });
    });
    req.on('error', reject);
    req.write(body);
    req.end();
  });
}

function parseJsonArray(text) {
  let json = text.trim();
  if (json.startsWith('```')) {
    json = json.substring(json.indexOf('\n') + 1);
    if (json.endsWith('```')) json = json.slice(0, -3);
    json = json.trim();
  }
  return JSON.parse(json);
}

function getExistingFileNames() {
  const names = new Set();
  const files = fs.readdirSync(OUTPUT_DIR).filter(f => f.endsWith('.json'));
  for (const file of files) {
    names.add(file.replace('.json', '').toLowerCase());
    try {
      const data = JSON.parse(fs.readFileSync(path.join(OUTPUT_DIR, file), 'utf8'));
      if (data.file_name) names.add(data.file_name.toLowerCase());
    } catch (e) { /* skip */ }
  }
  return names;
}

function writeDocument(doc, existingNames) {
  const fileName = doc.file_name || slugify(doc.name || doc.title || 'untitled');
  doc.file_name = fileName;
  if (existingNames.has(fileName.toLowerCase())) {
    console.log(`  SKIP: ${fileName}`);
    return false;
  }
  if (!doc.id) doc.id = generateId();
  // Compute line_count and headings
  const lines = (doc.body || '').split('\n');
  doc.line_count = lines.length;
  doc.headings = [];
  for (const line of lines) {
    const m = line.match(/^#{1,3}\s+(.+)/);
    if (m) doc.headings.push(m[1]);
  }
  const filename = fileName + '.json';
  fs.writeFileSync(path.join(OUTPUT_DIR, filename), JSON.stringify(doc, null, 2), 'utf8');
  existingNames.add(fileName.toLowerCase());
  return true;
}

const WORLD_CONTEXT = `You are writing in-world documents for StreetSamurai, set in GLMZ (Great Lakes Metropolitan Zone megacity corridor), year 2200.

WORLD RULES — embed these naturally, don't explain them:
- Φ is the Quanta currency symbol (NEVER "phi", NEVER the Greek letter phi — it is the QUANTA symbol)
- No city police exist. Arcturus Civil Security is the enforcement arm (corporate, brutal, not public)
- Tier 1-5 society: Tier 1 = poorest Shelf districts (packed vertical housing), Tier 5 = corporate elite
- Freelancers are NOT romantic rebels — they are laborers in a brutal informal economy
- Some freelancers are heroes. Some are war criminals. Most are just trying to survive.
- No simple moral answers. The world is grinding and specific.
- Missouri is flooded. Kentucky is gone. GLMZ is real and ongoing.
- BCIs (brain-computer interfaces) are common, especially in Tier 2+. Many Tier 1 residents are unaugmented.
- Augmentation is common but not universal. Chrome limbs, neural overlays, sensory mods.
- The Shelf is the dense lower-tier residential stack. The Canopy is upper-tier. The Narrows is mid-tier.

THE SIGNAL NETWORK (freelancer ranking — decentralized, not a database):
- Vouching chains: reputation is literally who trusts you and who they trust. No central authority.
- Dead Drops: physical reputation tokens left at locations. Brokers aggregate these.
- Tiers: C (unproven, survival filter), B (proven, most die here), A (known quantity, can say no), S (legend, job finds them), Ghost (doesn't officially exist — visibility is the threat)
- Ghost tier has no vouches, no records, no signal. Just results that brokers recognize.

WRITING STYLE — these are in-world documents. Write them AS IF:
- Zines are actually printed zines: opinionated, personal, sometimes badly photocopied
- Personal logs are messy, honest, often written in exhaustion
- Guides are practical with specific details, not generic advice
- Forum archives have multiple voices, tangents, petty arguments, real expertise buried in noise
- Manifestos are earnest and occasionally overwritten
- Obituaries are specific and grieving
- Letters are addressed to specific people and carry their relationship

The reader should feel like they found this document somewhere real. No summaries. No meta-commentary. Write the actual thing.`;

const DOCUMENT_SPECS = [
  {
    file_name: 'c_tier_survival_guide',
    name: 'So You\'re C-Tier Now: A Practical Guide to Not Dying in Your First Month',
    doc_type: 'guide',
    author: 'Anonymous (attributed to "Twice-Dead Malika" in various Signal Network circles)',
    topic: 'Survival guide for new C-tier freelancers. Cover: how to evaluate a posting without getting scammed, what gear matters at this tier vs. what\'s marketing, how to read a broker\'s actual reliability vs. their reputation, the most common ways C-tiers die (not combat — logistics, overconfidence, bad intel, dehydration during extended jobs), how to build your first vouching chain, what not to spend your first payout on. Be mercilessly practical. No romanticism. Specific Quanta figures where relevant.'
  },
  {
    file_name: 'gear_matters_c_vs_b_tier',
    name: 'What Gear Actually Matters (A C-Tier vs. B-Tier Breakdown)',
    doc_type: 'zine',
    author: 'Printed by the Narrows Mutual Aid Press, compiled from community submissions',
    topic: 'In-world zine comparing what equipment is actually worth buying at C-tier versus what you should save for B-tier. Covers: comms (cheap vs. encrypted), medkits (basic vs. full surgical), footwear (the most important thing nobody talks about), weapons (what C-tiers actually carry vs. what kills them), shelter gear (because not every job ends cleanly), BCI add-ons that are worth the Quanta vs. gimmicks. Written in zine voice — opinionated, specific, occasionally wrong about something, with a comment from another contributor arguing about one point.'
  },
  {
    file_name: 'protection_detail_72_hours',
    name: 'Protection Detail Log: 72 Hours',
    doc_type: 'personal_log',
    author: 'B-tier freelancer, name redacted per request',
    topic: 'First-person log of a 72-hour protection detail. The client is a mid-level Shelf-district community organizer who is trying to stop a CorpoNation rezoning. The freelancer is professional but tired. Cover: the first meeting with the client and establishing protocols, the first threat (not what was expected), hours 14-26 where nothing happens and the boredom is its own problem, the real threat that arrives on hour 51, the negotiation that happens instead of violence, the end of the job and the moral residue. Written in a log format, timestamped, honest about exhaustion and doubt.'
  },
  {
    file_name: 'jobs_you_never_take',
    name: 'Jobs You Never Take: Hard Rules from Twelve Years at B-Tier',
    doc_type: 'zine',
    author: 'Signed "Old Bone", widely believed to be Terttu Jarvinen-Ekwueme',
    topic: 'Experienced B-tier freelancer writing a personal set of rules about which contracts to refuse. Not moral rules — operational rules, with moral consequences embedded. Include: any job where you cannot independently verify the target\'s identity, any extraction job where you can\'t meet the person being extracted first, any sabotage job where the brief mentions "no casualties" without specifying how that\'s enforced, jobs posted by anonymous clients offering above-market payout (they always have a reason for the premium), any job that requires you to work with someone you can\'t vouch for. Some rules have specific anecdotes. One rule is broken at the end of the document, and the author knows it.'
  },
  {
    file_name: 'freelancer_ethics_contradiction',
    name: 'Being a Hero on the Weekends',
    doc_type: 'manifesto',
    author: 'Unknown; copies appeared across Shelf-district notice boards in 2197',
    topic: 'Essay on the fundamental ethical contradiction of freelancer existence: that the same person who protects a family from eviction on Tuesday took a corporate sabotage contract on Sunday that probably hurt someone\'s livelihood. The author does not resolve this. They describe it, live inside it, and resist easy answers. They are not burned out — they are genuinely trying to think through something real. Include: what it feels like to walk both sides, what the alternatives actually are (and why they don\'t work), whether refusing dirty contracts is a moral position or just a luxury for people with enough reputation to say no, what "harm reduction" looks like when you\'re a laborer in an informal economy with no other options.'
  },
  {
    file_name: 'why_i_went_ghost',
    name: 'Why I Went Ghost',
    doc_type: 'personal_log',
    author: 'Anonymous',
    topic: 'First-person account from someone who transitioned to Ghost tier. The document is written carefully — they are not explaining how they did it, only why. Cover: the point at which S-tier reputation became a liability rather than an asset (people know your name, which means people can reach you, which means people can threaten the people around you), the specific event that made the decision, what the transition cost (relationships, identity, stability), what Ghost tier actually is from the inside (not mystical — just invisible, which is its own kind of loneliness), whether it was worth it. Tone: calm, retrospective, not romanticizing. They miss things about being visible.'
  },
  {
    file_name: 'the_outlaw_math',
    name: 'The Outlaw Math',
    doc_type: 'manifesto',
    author: 'Originally delivered as a spoken piece at The Drip Bar; transcribed and circulated',
    topic: 'Philosophical essay/spoken piece about freelancer morality as arithmetic rather than ethics. The argument: every freelancer is doing math constantly — acceptable harm vs. payout, personal risk vs. necessity, one person\'s suffering vs. another\'s survival. The author argues that the "outlaw" frame is a lie, that freelancers are not outside the system but are one of its mechanisms, and that the math they\'re doing is the same math that runs the entire city — it\'s just that at the bottom of the tier system, you have to do it explicitly instead of pretending it\'s invisible. Not nihilistic — the author thinks the math can be done better and worse, and that "better" matters even when the framework is broken. Some people in the audience argue back; their objections are embedded in the text.'
  },
  {
    file_name: 'living_out_of_a_bag',
    name: 'Living Out of a Bag: Freelancer Housing in GLMZ',
    doc_type: 'guide',
    author: 'Compiled by the Narrows Mutual Aid Network, 2199 edition',
    topic: 'Practical guide to freelancer housing: how to find short-term rentals that don\'t require identity verification, what to actually keep in a go-bag vs. what gets left at a base, how to maintain a functional "home base" on C/B-tier income, the informal hospitality networks that exist among freelancers (staying at other people\'s places, the etiquette, what you owe), how to handle mail and communications without a fixed address, areas of GLMZ where short-term freelancer-friendly housing clusters. Specific neighborhoods mentioned. Specific Quanta figures. Mentions the risks: illegal subletting, eviction with no notice, landlords who tip off Arcturus.'
  },
  {
    file_name: 'injuries_medical_prep',
    name: 'Injuries: What to Stockpile, Who to Trust',
    doc_type: 'guide',
    author: 'Dr. Anonymous — clearly medically trained, currently practicing off-books',
    topic: 'Medical prep guide written for freelancers who cannot go to official medical facilities (because of outstanding warrants, Arcturus flags, or lack of Tier access). Cover: the injuries that kill you if you don\'t treat them in the first hour (and what to do), the injuries that can wait and the ones that only feel like they can wait, building a basic medical kit on a C-tier budget, who actually provides off-books medical care in GLMZ and how to find them, the two most common mistakes freelancers make with wounds (both kill you slowly), augmentation injuries and why they\'re different from organic injuries, when to go to a real clinic regardless of the risk. Specific drug names (in-world). Practical and frightening in places.'
  },
  {
    file_name: 'the_burnout_pattern',
    name: 'The Burnout Pattern: A Field Analysis',
    doc_type: 'guide',
    author: 'A Signal Network broker writing under the name "Clearwater"',
    topic: 'Analysis of freelancer psychological decline written from a broker\'s perspective — they see it from the outside, in patterns across dozens of contractors. Cover: the early warning signs (the ones that look like focus and dedication), the middle phase where performance is still good but judgment is degrading, the late phase where the freelancer becomes dangerous (to themselves and clients), the specific decision patterns that indicate someone is past saving, what good brokers do when they see a freelancer burning out (and what they should do versus what they actually do, which is often keep offering work). The author is uncomfortable with their own role in the system. Includes brief case studies with identifying details changed.'
  },
  {
    file_name: 'a_tier_retrospective_log',
    name: 'Log: Year Eight',
    doc_type: 'personal_log',
    author: 'A-tier freelancer, withheld',
    topic: 'A personal log written at the end of year eight of a freelancer career, now solidly A-tier. Not a triumph narrative — a balance sheet. Cover: what eight years costs physically (be specific about injuries, chronic conditions, augment replacements), what it costs relationally (who is still in their life and who isn\'t), what the Quanta situation actually is after eight years (not as simple as it sounds — irregular income, medical debt, periods of nothing), what they are good at now that they weren\'t good at before, what they\'ve learned to refuse, what they still take because they have to, whether they would do it again (the answer is complicated). Written in a spare, factual voice that is honest about ambivalence.'
  },
  {
    file_name: 'the_freelancers_widow',
    name: 'To Whoever Finds This First',
    doc_type: 'letter',
    author: 'Name withheld',
    topic: 'A letter written by someone who loved a freelancer who did not come back from a job. Not addressed to the freelancer — addressed to whoever finds their things first (likely another freelancer). The letter deals with: the specific texture of loving someone in this profession (the waiting, the not-being-told things, the practiced calm when they leave), the specific moment of knowing something was wrong, what the letter writer wants done with the freelancer\'s equipment (there is a specific list), what they want said to the broker who gave the last contract, a message for someone the letter writer cannot contact directly but the reader might be able to reach. Grief without sentimentality. Specific and particular.'
  },
  {
    file_name: 'last_will_c_tier',
    name: 'Last Will and Testament of Nobody in Particular',
    doc_type: 'letter',
    author: 'Unsigned; found folded inside a standard C-tier go-bag',
    topic: 'Informal last will written by a young C-tier freelancer before a difficult job. They are being practical but the document reveals who they are. Cover: the specific equipment and what should happen to it, the people they owe Quanta and the people who owe them, a few things they want to say to specific people (using only first names or nicknames), one regret they want written down even if no one reads it, a strange specific request for something to be done that only makes sense if you knew them. The voice is young — maybe 22, maybe 24. Not melodramatic. Practical in the way people get when they are genuinely afraid and trying to function anyway.'
  },
  {
    file_name: 'signal_network_fairness_debate',
    name: 'FORUM ARCHIVE: Is the Signal Network Rigged? (Thread 7,841 — The Narrows Board)',
    doc_type: 'forum_archive',
    author: 'Multiple contributors; archived from The Narrows Board, a freelancer community forum',
    topic: 'A community forum debate about whether the Signal Network is fair. Arguments on multiple sides: that it advantages people who already have social capital and punishes those who enter from disadvantaged networks, that the vouching system replicates existing social hierarchies (certain communities vouching for each other, closing out others), that it\'s still better than a centralized authority, that Ghost tier is a rumor used by powerful people to avoid accountability, that Tier designations are too slow to update and leave people underrated for years. Several voices in the thread: a C-tier who feels locked out, a B-tier defending the system, a broker explaining how it actually works from their end, someone who knows a specific case of manipulation, a contrarian who thinks the whole premise of the debate is a distraction. Real argument with good-faith positions and bad-faith ones mixed.'
  },
  {
    file_name: 'signal_network_inside_view',
    name: 'How Broker Consensus Actually Works (And Why It\'s Broken)',
    doc_type: 'guide',
    author: 'A broker writing anonymously — believed to be active in the Shelf/Narrows corridor',
    topic: 'From a broker\'s perspective: how the Signal Network tier designations actually happen in practice versus the theory. The theory is consensus of active brokers. The practice involves: which brokers talk to which other brokers (cliques, rivals, people who haven\'t spoken in two years because of a bad job in 2194), how personal relationships between brokers affect how they evaluate a freelancer\'s vouches, the specific informal channels through which Dead Drop data gets aggregated and argued about, who gets promoted quickly (and why it\'s not always merit), what it takes to get someone demoted (it\'s harder than most freelancers think — brokers protect their own reputation by not admitting they misjudged someone). Includes: the two most common ways the system gets gamed, and why the people gaming it don\'t always win.'
  },
  {
    file_name: 'famous_freelancer_oral_history',
    name: 'Talking About Crane: Seven Accounts',
    doc_type: 'interview_transcript',
    author: 'Collected by an unnamed researcher; circulated in the freelancer community',
    topic: 'Oral history of a famous (probably Ghost-tier) freelancer known only as "Crane" — told by seven different people who encountered them. The accounts contradict each other in some details and agree in others, which is how oral histories work. Include: someone who worked a job alongside Crane years ago and describes them in specific physical detail, a broker who never met Crane but processed their work three times, someone who was Crane\'s target and survived and describes what that was like, someone who claims Crane saved their life without being contracted to, someone who thinks "Crane" is a legend assembled from multiple real people, someone who is afraid to say much but says it anyway, a brief note from someone who may have known Crane personally and is the most cautious of all. The shape of the person emerging from these accounts is never complete and never consistent — but something comes through.'
  },
  {
    file_name: 'what_brokers_remember',
    name: 'What Brokers Remember: Stories Told Off the Record',
    doc_type: 'interview_transcript',
    author: 'Anonymous collection; claimed to be drawn from fifteen years of broker conversations',
    topic: 'A collection of short anecdotes from brokers, told informally — the kind of stories that don\'t make it into official Signal Network history. Each anecdote is short (100-200 words). Include: the best job anyone ever ran and why nobody ever talks about it publicly, the worst failure a broker facilitated and what they learned, a freelancer who should have been Ghost tier but refused any designation, a contract so strange that two brokers refused it and the third who took it wishes they hadn\'t, a time when the Signal Network got it exactly right, a time when the consensus system protected someone it should have expelled and the fallout. Seven to ten anecdotes. Different voices. The collection feels like it was assembled by someone who cares about this history.'
  },
  {
    file_name: 'the_tier_trap_essay',
    name: 'The Tier Trap: Why B-Tier is Where Careers Go to Die',
    doc_type: 'zine',
    author: 'Published in "Flatline Quarterly", issue 22, 2198',
    topic: 'An analytical piece about why B-tier is statistically the most dangerous place to be in the Signal Network — not because the jobs are harder than A-tier, but because of the specific trap: you\'re proven enough to take serious jobs, but not known enough for clients to treat you with full professionalism, you\'re taking more risk for less payout than you would at A-tier, the vouching network around you is not yet dense enough to protect you if something goes wrong, and you haven\'t yet developed the scar tissue that A-tier gives you. Include: actual (in-world) rough statistics that the author claims to have compiled from broker records, the specific psychological state that leads B-tiers to overextend, what the path from B to A actually looks like (including how long it takes and who makes it), what happens to people who plateau at B-tier for more than four years.'
  },
  {
    file_name: 'c_tier_first_payout',
    name: 'What I Did With My First Payout',
    doc_type: 'forum_archive',
    author: 'Multiple contributors; The Narrows Board community thread, 2199',
    topic: 'Community forum thread where C-tier and newly B-tier freelancers share what they did with their first significant payout — and the older hands react. The mix: someone who bought gear and describes what they chose and why, someone who paid off a debt and the complicated feelings around that, someone who spent it on something practical for their family, someone who blew it on something stupid and admits it, someone who saved it and is quietly smug about this, a veteran responding to each entry with a rating of "survivable choice / bad choice / exactly right", a final comment from someone who made the worst possible choice and is still here to tell it. Human and specific and occasionally funny and occasionally sad.'
  },
  {
    file_name: 'freelancer_housing_zine',
    name: 'The Crashbook: Where to Sleep When You Can\'t Go Home',
    doc_type: 'zine',
    author: 'Published by the Mutual Roof Collective, 3rd edition',
    topic: 'Community zine listing informal housing resources for freelancers in GLMZ — written as a real resource guide but also as a document of a community that takes care of itself because no one else does. Format: listings with brief descriptions and contact info (fictional but specific), interspersed with short personal notes from people who used these resources, a section on the etiquette of the informal hospitality network (what you owe, what it\'s not okay to ask, the specific things that get you blacklisted), a rant from the editor about a Tier 3 landlord who reported a freelancer to Arcturus, a thank-you note from someone who would have been homeless without the network. Specific addresses (fictional but placed in named GLMZ districts). Practical and warm and a little fierce.'
  },
  {
    file_name: 'the_freelancer_economy',
    name: 'What They Don\'t Tell You About the Freelancer Economy',
    doc_type: 'zine',
    author: 'Signed "The Ledger"; published in various Shelf-district zine drops',
    topic: 'Economic analysis of the informal freelancer labor market written for freelancers by someone who clearly has economic training. Cover: why payout rates for the same tier of job have declined in real Quanta terms over the last fifteen years (the market is more crowded), how brokers\' take has crept up, the way "tier designation" functions as a wage-suppression tool at the C/B level (you can\'t negotiate above your tier), the informal secondary economy around jobs (gear rental, information brokerage, medical prep), why freelancers rarely accumulate real wealth even on A-tier income (the reasons are specific), what collective action would look like and why it hasn\'t happened (structural answer, not a naive one), what the author thinks is actually possible. Not defeatist. Angry and specific.'
  },
  {
    file_name: 'augmentation_injuries_log',
    name: 'The Parts That Fail: An Augmentation Injury Log',
    doc_type: 'personal_log',
    author: 'Name withheld; submitted to a community medical archive',
    topic: 'Personal log documenting all augmentation injuries and failures over six years of freelance work. Each entry is dated and describes: which augmentation failed, in what circumstances, what the immediate consequence was, how it was repaired (by whom, for how much Quanta, through what channel), the residual effect. Cover four to six different augmentations across the six years. Include: a chrome limb joint that failed at the worst possible moment, a BCI overlay that fragmented during a high-stress operation and what that was like from the inside, sensory mod that started giving false readings and the job that nearly went wrong because of it, the repair that the author couldn\'t afford and what they did instead, the one modification they removed permanently because the risk wasn\'t worth it. Medical detail that is specific and uncomfortable.'
  },
  {
    file_name: 'freelancer_community_zine',
    name: 'The Narrows Freelancer Collective: Third Anniversary Issue',
    doc_type: 'zine',
    author: 'The Narrows Freelancer Collective editorial board',
    topic: 'Community zine from an informal freelancer mutual-aid collective, published for their third anniversary. Include: a letter from the founding member describing why they started it (specific incident), a community board of who is available for referrals, a list of resources the collective has built (a small medical fund, a gear-lending library, a vouching network of their own that feeds into the Signal Network), a tribute to a collective member who died on a job that year, a new-member introduction section where three new members describe themselves in their own words, a section of practical advice submissions from members, an argument between two members about a policy dispute that got included in print because the editor thought it was important to show. Warm but not saccharine — a real community document with real friction.'
  },
  {
    file_name: 'the_job_before_last',
    name: 'The Job Before Last',
    doc_type: 'personal_log',
    author: 'Attributed to "Seven-Stitch", a B-tier freelancer who did not survive the following job',
    topic: 'Personal log written after what turned out to be the second-to-last job a freelancer ever ran. They don\'t know it is the job before last. The log is about a job that went well — an escort job that went smoothly, a client who was grateful, a specific moment of unexpected kindness from someone being protected. The writer reflects on what makes a job feel worth it, mentions the next job they\'ve accepted (obliquely, careful about operational security), says something about someone they are going to see when they get back. The log ends mid-thought — not dramatically, just the way personal logs end, because you close the file and go do something else. The document is framed by a short introductory note from a friend who found the log and published it, explaining why. The friend is brief and specific and doesn\'t editorialize.'
  },
  {
    file_name: 'what_old_freelancers_do',
    name: 'Where the Old Ones Go',
    doc_type: 'guide',
    author: 'Anonymous; circulated in Signal Network broker channels',
    topic: 'Essay and informal guide about what happens to freelancers at the end of their careers — the ones who survive long enough to have a career end. Cover the real range: some become brokers (the path and what it requires), some become trainers (informal — teaching newer freelancers specific skills, for Quanta), some become fixers (logistics, contacts, the infrastructure work), some go to ground in Shelf-adjacent communities (what that looks like, the ones who find peace and the ones who don\'t), some die on jobs they took past the point they should have stopped, some leave GLMZ entirely (where do they go? Specific places mentioned). The essay is not nostalgic — it is practical, with genuine affection for the people it describes. Mentions two or three specific people by name or nickname. Notes that the ones who talk about retiring rarely do.'
  },
  {
    file_name: 'freelancer_obituary_collective',
    name: 'Lost, Not Forgotten: Memorial Records, Vol. 4',
    doc_type: 'obituary',
    author: 'The Narrows Memorial Collective',
    topic: 'A collection of brief obituaries for freelancers who died in the past year. Format: each entry is short — name (real or operational name), tier designation, a sentence about what they were known for, a sentence about how they died (specific but not graphic), a note from someone who knew them. Include seven to nine obituaries. The range: a C-tier who died on their third job (the entry is written by someone who barely knew them and is struggling with that), a B-tier who had been around for years and is deeply mourned, someone at A-tier whose death surprised everyone, an older freelancer who had technically retired but took one more job, someone who died in a non-job accident which is its own kind of grief, someone whose death is listed as "circumstances unknown" which the writer notes is a specific kind of loss. The collection is a document of a community that keeps track of its dead because no one else does.'
  },
  {
    file_name: 'running_solo_guide',
    name: 'Running Solo: The Case Against Crews',
    doc_type: 'guide',
    author: 'B-tier freelancer, pseudonym "Needlepoint"',
    topic: 'Practical and philosophical argument for running solo rather than with a crew. Cover: the operational advantages (no split of payout, no negotiation on in-field decisions, no weak links in the vouching chain), the specific ways crew jobs go wrong (one person\'s failure affects everyone, interpersonal friction under pressure, the problem of a crew member making a moral call you disagree with mid-job), what to do when a job requires multiple people (subcontracting vs. partnering, and the difference), the emotional reality of solo work (specific about what it feels like to have no backup, and how to manage that), the things solo operators can\'t do and have to account for, the author\'s specific methodology for assessing whether a job is within solo capacity. One section where the author admits they are wrong about a specific thing — solo operators have higher variance outcomes; more of them die young, and more of them accumulate faster if they don\'t.'
  },
  {
    file_name: 'running_with_crew_guide',
    name: 'Running With a Crew: How to Not Get Killed by the People You\'re Working With',
    doc_type: 'guide',
    author: 'A-tier freelancer, credited as "Rook"',
    topic: 'Counter-argument: why crews are better, if done right. Cover: how to vet crew members before a job (Signal Network checks, but also the informal stuff — how they talk about past jobs, whether they are honest about their limits, how they handle unexpected changes in a plan), crew communication protocols that actually work under pressure, role distribution and why "everyone does everything" is how crews die, what to do when a crew member freezes or panics, the economics of crew work (payout splits, who covers medical, who carries the overhead), the specific kinds of jobs where solo operators are simply outmatched and how to recognize them before you take them. Includes a section on the difference between a crew and a partnership and a crew and "people who happen to be on the same job."'
  },
  {
    file_name: 'broker_diary_fragments',
    name: 'Fragments From a Working Broker\'s Log, Undated',
    doc_type: 'personal_log',
    author: 'Unknown; format suggests active broker, GLMZ Shelf/Narrows corridor',
    topic: 'Fragments from a broker\'s personal log — not the official records, the private notes. Include: a note about a freelancer who turned down a job and the broker\'s reaction (admiration or frustration), a record of a job that went wrong and the broker\'s processing of their role in it, the broker\'s observation of a C-tier who is improving and the decision about when to start circulating their name, a note about a client who wants something the broker won\'t facilitate (and the negotiation that happened), a late-night entry about the emotional weight of the job, a very short note that is just a name and a Quanta figure and "never again", a reflection on the difference between a good job and a just job. The fragments don\'t build to a story — they are the texture of a working life, disconnected, specific, human.'
  },
  {
    file_name: 'the_clean_jobs_myth',
    name: 'The Clean Job Myth',
    doc_type: 'manifesto',
    author: 'Signed "Meridian, formerly A-tier"',
    topic: 'Essay arguing that "clean" contracts (rated clean on the moral_weight scale) are a marketing category, not a moral reality. The argument: every retrieval has someone it harms. Every protection job has someone it leaves unprotected. Every delivery job is part of a supply chain that someone benefits from and someone doesn\'t. The author is not arguing for nihilism — they are arguing against self-deception. They want freelancers to know what they\'re actually doing. They also describe the specific jobs they consider as close to genuinely clean as the work gets, and why they take them when they can. The essay ends with a list: jobs the author has refused and the reason, jobs the author took despite doubts and what happened, and one job that still bothers them fifteen years later, with no resolution offered.'
  },
  {
    file_name: 'shelter_district_freelancer_profile',
    name: 'Freelancing From the Shelf: A Different Calculation',
    doc_type: 'guide',
    author: 'Anonymous; written for and distributed within Shelf-district communities',
    topic: 'Guide specifically for Tier 1 Shelf-district residents considering or beginning freelance work. Acknowledges that the calculation is different when your starting point is the Shelf: you have less capital, more desperation, and less margin for error. Cover: how to enter without a vouching network (there are paths, they are slow), what Tier 1 community resources exist that can substitute for what other entrants take for granted, the specific risks that Shelf-based freelancers face that other entrants don\'t (Arcturus over-polices the Shelf, so even being seen in certain company creates risk), what to expect from brokers at the bottom of the market (some are decent, some exploit the desperation), the honest math on how long it takes to build enough reputation to improve your situation. Not a recruitment document — a realistic orientation for people who are already considering this because they don\'t see another option.'
  },
  {
    file_name: 'freelancer_medical_debt',
    name: 'The Debt That Doesn\'t Heal',
    doc_type: 'personal_log',
    author: 'Submitted anonymously to the Narrows Community Medical Archive',
    topic: 'Personal log about medical debt accumulated through freelance injuries. Specific and financial: the injury, the cost, the treatment available vs. treatment needed, the Quanta borrowed and from whom, the interest structure of informal Shelf-district debt, how the debt shaped the next job decisions (taking work that should have been refused because the debt demanded income), how the debt accumulated further, the specific point where it became unmanageable, what happened next (not a clean resolution). Also covers: the specific indignity of off-books medical care, the quality difference between what freelancers can access and what legitimate Tier 3 medicine looks like, one doctor who was genuinely good and the complicated feelings about depending on someone operating illegally to survive.'
  },
  {
    file_name: 'the_language_of_freelancers',
    name: 'How Freelancers Talk: A Field Glossary',
    doc_type: 'guide',
    author: 'Compiled by "Dispatch", a freelancer-adjacent archivist',
    topic: 'An in-world glossary of Signal Network slang and freelancer terminology. Not a dictionary — written with commentary, etymology notes, and examples of usage. Include fifteen to twenty terms: the vocabulary around contracts (how jobs are described, what certain phrases in a posting actually mean), the vocabulary around people (how freelancers describe clients, brokers, targets, each other), terms for specific types of situations (a job that has gone wrong in a specific way, a client who is not what they claimed, a broker who owes you), Signal Network-specific language (how tiers are discussed, how vouching is described), Shelf-district slang that has entered freelancer vocabulary. Several entries include a note from a contributor who disagrees with the definition. One entry has a note from someone who coined the term and finds the current usage wrong. The glossary captures language as living, contested, imprecise.'
  },
  {
    file_name: 'the_vanishing_freelancers',
    name: 'The Missing: Freelancers Who Disappeared Without a Record',
    doc_type: 'guide',
    author: 'Signal Network community archive project, 2199',
    topic: 'Community archive documenting freelancers who disappeared — not died on jobs with witnesses, but simply stopped. No body, no final Dead Drop, no last contact. The document is part practical (the Signal Network\'s informal protocols for declaring someone missing versus dead versus gone dark deliberately), part memorial, part warning. Include: the statistical baseline for disappearances vs. expected deaths by tier, the three most common explanations (went deep cover deliberately, taken by a CorpoNation they worked against, failed a Ghost-tier job), several specific case summaries (names changed or coded), the community debate about what obligations exist to missing freelancers\' dependents when cause of disappearance is unknown, a section on how to make disappearing easier if you have to (operational notes, not morbid — practical). The document is maintained by people who think this history matters.'
  },
  {
    file_name: 'client_horror_stories',
    name: 'Client Horror: When the Employer Is the Problem',
    doc_type: 'forum_archive',
    author: 'Community thread, The Narrows Board, compiled from 2196-2199',
    topic: 'Forum archive of freelancer experiences with bad clients — clients who lied about the job, withheld information that changed the risk, tried to expand the scope mid-job without paying for it, endangered the freelancer, or refused to pay on completion. Multiple voices, specific incidents, practical advice embedded in the complaints. Include: a client who turned out to be the target (and how the freelancer handled it), a client who reported a freelancer to Arcturus after completion to avoid paying, a client whose stated goal turned out to be different from their real goal and the job\'s actual outcome caused harm the freelancer hadn\'t agreed to, a client who tried to recruit a freelancer into something ongoing mid-job, a client who was actually decent but communicated so badly the job almost failed anyway. Veterans of the thread offer patterns to watch for. The thread ends with someone posting a client they\'ve blacklisted and others confirming with their own experiences of the same client.'
  },
  {
    file_name: 'the_freelancer_and_community',
    name: 'The Freelancer and the Community: On Belonging to a Place You Operate In',
    doc_type: 'manifesto',
    author: 'Signed by the Brightmoor Reclamation Freelancer Caucus',
    topic: 'Essay arguing that freelancers who work in and around their home communities have specific obligations that freelancers operating elsewhere don\'t. The argument: you know the people affected by your jobs, which means "just following the contract" is not sufficient. But the essay does not moralize — it tries to work out what the obligation actually looks like in practice. Cover: the specific cases where freelancers refused jobs that would harm their home community (and what they did instead), the cases where they took those jobs anyway and what happened to their standing in the community, the informal local-community vouching systems that exist alongside the Signal Network and are sometimes more powerful, what "being from somewhere" gives you as a freelancer (access, protection, local knowledge) and what it costs (accountability, visibility, the impossibility of true anonymity at home). Ends with a practical argument about why locally-embedded freelancers are actually more effective, not just more ethical.'
  },
  {
    file_name: 'what_i_know_now',
    name: 'What I Know Now That I Wish I\'d Known Then',
    doc_type: 'personal_log',
    author: 'A-tier freelancer, known only as "Callus"',
    topic: 'Personal retrospective from an A-tier freelancer to their younger C-tier self. Not advice — more like testimony. Cover: the specific mistake that almost ended the career (and what saved it), the thing that took years to learn that should have been obvious (about people, not tactics), the relationship they should have handled differently, the job they are still angry about taking, the one they are grateful they refused even though it cost them financially at the time, what "getting better at the work" actually felt like from the inside versus what they expected, the physical cost (specific), the thing they still do not know after ten years of practice, the thing they would say if they could say one thing to someone just starting. Written with the earned authority of someone who has genuinely thought about this for a long time. Not a success story — a survival story with a complicated ending.'
  },
  {
    file_name: 'contract_rating_culture',
    name: 'Rating Clients: How the Back Channel Works',
    doc_type: 'guide',
    author: 'A broker writing as "Secondary Source"',
    topic: 'Guide to how freelancers and brokers share informal intelligence about clients — outside the Signal Network\'s formal structure. Cover: the informal channels where client reputations are discussed (specific in-world venues and communication methods), how freelancers flag problem clients without creating legal liability for themselves, the broker\'s role in aggregating client intelligence (they know more than they share, and why), the specific signals that indicate a client is worth avoiding (patterns, not single incidents), the cases where a bad client gets protected because of their CorpoNation affiliation, and the cases where even powerful clients get effectively blacklisted through informal consensus. Includes a section on the reverse: how clients evaluate brokers, and what makes a broker trustworthy to a client who needs genuine operational security. The document is practical and also quietly describes a shadow governance structure operating in the gaps of corporate law.'
  },
  {
    file_name: 's_tier_interview_fragments',
    name: 'Talking to the Legends: What S-Tier Actually Means',
    doc_type: 'interview_transcript',
    author: 'Compiled from community oral history project, 2198-2199',
    topic: 'Oral history compilation of people who have interacted with S-tier freelancers — not the freelancers themselves (they don\'t give interviews), but people who worked alongside them, were clients, were targets, or were brokers who interfaced with them. Include six to eight voices: a B-tier who ran one job with an S-tier and describes the experience, a broker who represents three S-tier contractors and describes what "representing them" actually means at that level, a client who hired an S-tier and describes what the negotiation was like, someone who was almost a target and escaped (and their account of the S-tier\'s method), a community member in the area where an S-tier grew up who knew them before they were known, someone who argues the entire S-tier category is manufactured mythology (and their argument is partially convincing), a brief account from someone who may have seen a Ghost-tier operator and may not be able to tell the difference. What emerges is a picture of what extreme competence looks like at the top of an informal labor market, and what it costs.'
  },
  {
    file_name: 'the_ethics_of_information_work',
    name: 'When the Job Is Knowing Things: Ethics in Recon and Intelligence Contracts',
    doc_type: 'manifesto',
    author: 'Freelancer collective document, unnamed contributors',
    topic: 'Essay about the specific ethical complexity of information-gathering contracts — the jobs rated "recon" or "intelligence" that seem clean because nobody gets physically hurt. The argument: information work causes harm downstream, and the people who sell intelligence don\'t always know what it will be used for. Cover: the specific ways recon jobs feed harm (mapping a location so someone else can do a job the recon contractor wouldn\'t have taken, verifying a target\'s patterns for an elimination contract, documenting a community\'s internal structure for a displacement operation), the operational security of the recon contractor versus their moral exposure, the cases where the information gathered was used for something the contractor would have refused if asked directly, what "informed consent" looks like for an information contract (impossible to fully achieve, but some attempts are better than others), a practical list of questions to ask before taking an information job that might reveal uncomfortable answers. Not a refusal to do the work — a framework for doing it with eyes open.'
  },
  {
    file_name: 'freelancer_relationships_essay',
    name: 'The Relationship Problem: Intimacy in an Unstable Life',
    doc_type: 'zine',
    author: 'Published by The Narrows Community Press; multiple anonymous contributors',
    topic: 'Community essay collection about romantic and close relationships in freelancer life. Not advice — different people\'s experiences, honestly told. Include: someone who is in a stable long-term relationship with a person who knew what they were getting into and made peace with it (specific about what that peace cost and what it looks like), someone who could not make it work and is honest about why (the secrecy, the irregular presence, the slow leak of stress into ordinary time), someone who only dates other freelancers (the advantages and the specific problems), someone who stopped trying to have intimate relationships and is writing about what that decision cost, a brief account from a non-freelancer partner about what the waiting is actually like from that side, someone who found a different structure entirely and describes it without either romanticizing or dismissing it. Warm and honest and specific in the way community publications can be when people trust the space.'
  },
];

async function generateDocument(spec, existingNames) {
  if (existingNames.has(spec.file_name.toLowerCase())) {
    console.log(`  SKIP: ${spec.file_name}`);
    return null;
  }

  const system = WORLD_CONTEXT + `\n\nYou will generate a SINGLE in-world document as a JSON object with EXACTLY these fields:
{
  "id": "a random 32-char hex string",
  "name": "Document title",
  "type": "document",
  "file_name": "the exact file_name slug provided",
  "doc_type": "zine|personal_log|guide|forum_archive|interview_transcript|manifesto|obituary|letter",
  "author": "name, anonymous, or pseudonym — in-world",
  "description": "One paragraph: what this document is and why it exists. Written in-world as if describing it in an archive entry.",
  "body": "The FULL document text (600-1500 words). Write IN-WORLD. This is prose the reader would actually find. NOT a summary. The actual thing.",
  "tags": ["document", "freelancer", "culture", "...relevant tags"]
}

Return ONLY the JSON object. No prose, no commentary, no markdown fences.`;

  const user = `Generate the document with file_name: "${spec.file_name}"
Title: ${spec.name}
Doc type: ${spec.doc_type}
Author: ${spec.author}
Content guidance: ${spec.topic}

Write the full in-world document. Body should be 600-1500 words of actual document text — the thing itself, not a description of it.`;

  try {
    let text = await callClaude(system, user, 8192);
    // Strip markdown fences if present
    text = text.trim();
    if (text.startsWith('```')) {
      text = text.substring(text.indexOf('\n') + 1);
      if (text.endsWith('```')) text = text.slice(0, -3);
      text = text.trim();
    }
    const doc = JSON.parse(text);
    doc.file_name = spec.file_name; // enforce the slug
    return doc;
  } catch (e) {
    console.error(`  ERROR generating ${spec.file_name}: ${e.message}`);
    return null;
  }
}

async function main() {
  console.log('=== generate_freelancer_culture.js ===');
  console.log(`Output: ${OUTPUT_DIR}`);

  const existingFileNames = getExistingFileNames();
  console.log(`Existing documents: ${existingFileNames.size}`);

  let totalWritten = 0;
  let totalSkipped = 0;

  const specsToRun = DOC_LIMIT ? DOCUMENT_SPECS.slice(0, DOC_LIMIT) : DOCUMENT_SPECS;
  if (DOC_LIMIT) console.log(`Limiting to ${specsToRun.length} document(s).`);

  for (let i = 0; i < specsToRun.length; i++) {
    const spec = specsToRun[i];
    console.log(`\n[${i + 1}/${specsToRun.length}] ${spec.file_name}`);

    if (existingFileNames.has(spec.file_name.toLowerCase())) {
      console.log(`  SKIP (already exists)`);
      totalSkipped++;
      continue;
    }

    const doc = await generateDocument(spec, existingFileNames);
    if (doc) {
      if (writeDocument(doc, existingFileNames)) {
        totalWritten++;
        console.log(`  WROTE: ${spec.file_name}`);
      } else {
        totalSkipped++;
      }
    }

    if (i < specsToRun.length - 1) {
      await sleep(WAIT_MS);
    }
  }

  const finalCount = fs.readdirSync(OUTPUT_DIR).filter(f => f.endsWith('.json')).length;
  console.log(`\n=== DONE ===`);
  console.log(`Documents written this run: ${totalWritten}`);
  console.log(`Documents skipped: ${totalSkipped}`);
  console.log(`Total documents in directory: ${finalCount}`);
}

main().catch(e => {
  console.error('Fatal error:', e);
  process.exit(1);
});
