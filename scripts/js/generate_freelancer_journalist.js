// generate_freelancer_journalist.js
// Generates a journalist character + 20 interview documents for StreetSamurai/GLMZ 2200
// Step 1: Creates journalist character → engine/data/people/
// Step 2: Creates 20 interview documents → engine/data/documents/
// Resume-safe: skips existing files by slug/file_name

const fs = require('fs');
const path = require('path');
const crypto = require('crypto');
const https = require('https');

const settings = JSON.parse(fs.readFileSync(
  path.join(process.env.LOCALAPPDATA, 'MindAttic', 'StreetSamurai', 'Settings.json'), 'utf8'));
const API_KEY = settings.ApiKey;
const MODEL = 'claude-sonnet-4-6';
const PEOPLE_DIR = path.resolve(__dirname, '..', 'engine', 'data', 'people');
const DOCS_DIR = path.resolve(__dirname, '..', 'engine', 'data', 'documents');
const WAIT_MS = 3000;

// Journalist's established file name — used to check existence
const JOURNALIST_FILE = 'yuki_osei_ashikaga.json';
// Journalist's established details (set once so interviews can reference consistently)
const JOURNALIST_NAME = 'Yuki Osei-Ashikaga';
const JOURNALIST_ROLE = 'Investigative journalist, The Undercurrent';
const PUBLICATION = 'The Undercurrent';

if (!fs.existsSync(PEOPLE_DIR)) fs.mkdirSync(PEOPLE_DIR, { recursive: true });
if (!fs.existsSync(DOCS_DIR)) fs.mkdirSync(DOCS_DIR, { recursive: true });

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

function parseJson(text) {
  let json = text.trim();
  if (json.startsWith('```')) {
    json = json.substring(json.indexOf('\n') + 1);
    if (json.endsWith('```')) json = json.slice(0, -3);
    json = json.trim();
  }
  return JSON.parse(json);
}

function getExistingDocFileNames() {
  const names = new Set();
  const files = fs.readdirSync(DOCS_DIR).filter(f => f.endsWith('.json'));
  for (const file of files) {
    names.add(file.replace('.json', '').toLowerCase());
    try {
      const data = JSON.parse(fs.readFileSync(path.join(DOCS_DIR, file), 'utf8'));
      if (data.file_name) names.add(data.file_name.toLowerCase());
    } catch (e) { /* skip */ }
  }
  return names;
}

function writeDocument(doc, existingNames) {
  const fileName = doc.file_name || slugify(doc.name || 'untitled');
  doc.file_name = fileName;
  if (existingNames.has(fileName.toLowerCase())) {
    console.log(`  SKIP: ${fileName}`);
    return false;
  }
  if (!doc.id) doc.id = generateId();
  const lines = (doc.body || '').split('\n');
  doc.line_count = lines.length;
  doc.headings = [];
  for (const line of lines) {
    const m = line.match(/^#{1,3}\s+(.+)/);
    if (m) doc.headings.push(m[1]);
  }
  const filename = fileName + '.json';
  fs.writeFileSync(path.join(DOCS_DIR, filename), JSON.stringify(doc, null, 2), 'utf8');
  existingNames.add(fileName.toLowerCase());
  return true;
}

const WORLD_CONTEXT = `You are generating content for StreetSamurai, set in GLMZ (Great Lakes Metropolitan Zone megacity corridor), year 2200.

WORLD RULES — embed naturally, do not explain:
- Φ is the Quanta currency symbol (NEVER "phi", NEVER the Greek letter — it is the QUANTA currency symbol)
- No city police. Arcturus Civil Security is the enforcement arm (corporate, brutal, not public)
- Meridian PD dissolved in 2208 — no "Metro Police"
- Tier 1-5 society: Tier 1 = poorest Shelf districts, Tier 5 = corporate elite
- CorpoNations are sovereign — they have territories, borders, their own law
- The Shelf is dense lower-tier residential. The Narrows is mid-tier. The Canopy is upper-tier.
- Missouri is flooded. Kentucky is gone. GLMZ is real and ongoing.
- BCIs (brain-computer interfaces) common in Tier 2+. Many Tier 1 residents unaugmented.
- Ringo CorpoNation is a real entity in this world.
- Freelancers are NOT romantic rebels — they are laborers in a brutal informal economy.

THE SIGNAL NETWORK (freelancer ranking — decentralized, not a database):
- Vouching chains: reputation is literally a web of who trusts you and who they trust. No central authority.
- Dead Drops: physical reputation tokens at locations. Brokers aggregate.
- C Tier: unproven, survival filter. B Tier: proven, most die here. A Tier: known, can say no.
- S Tier: legend, the job finds them. Ghost Tier: doesn't officially exist — no records, no signal.

THE JOURNALIST:
- Name: ${JOURNALIST_NAME}
- Role: ${JOURNALIST_ROLE}
- Publication: ${PUBLICATION} (independent street-level news feed, Shelf/Narrows corridor)
- Background: early 20s, ambitious, not naive but not burned out. Mixed East Asian + West African heritage. Grew up in the Mids. Journalism scholarship from Ringo CorpoNation — she owes them something and they know it. Immediately started covering the stories they didn't want covered. She believes in the work.
- Voice in interviews: direct, specific questions. Not performing cynicism. Lets subjects talk. Pushes on contradictions without being combative. Occasionally reveals her own position.`;

// ─── STEP 1: JOURNALIST CHARACTER ────────────────────────────────

async function generateJournalist() {
  const filePath = path.join(PEOPLE_DIR, JOURNALIST_FILE);
  if (fs.existsSync(filePath)) {
    console.log(`  Journalist already exists: ${JOURNALIST_FILE}`);
    return JSON.parse(fs.readFileSync(filePath, 'utf8'));
  }

  console.log(`  Generating journalist character: ${JOURNALIST_NAME}`);

  const system = WORLD_CONTEXT + `

Generate a complete character JSON for ${JOURNALIST_NAME}. Use the full schema from the existing person files in this world — all fields below are required. Return ONLY a valid JSON object.

REQUIRED SCHEMA:
{
  "id": "32-char hex",
  "type": "character",
  "name": "Full Name",
  "aliases": [],
  "species": "human",
  "gender": "female",
  "pronouns": "she/her",
  "role": "${JOURNALIST_ROLE}",
  "age": 23,
  "status": "active",
  "location": "specific GLMZ location",
  "description": "2-3 paragraphs of vivid, specific character description",
  "psychology": {
    "facet_weights": { "wound": 0-1, "ideal": 0-1, "id": 0-1, "shadow": 0-1, "mask": 0-1, "ghost": 0-1 },
    "core_fears": [],
    "core_desires": [],
    "coping_mechanisms": [],
    "blind_spots": [],
    "secret": "one specific secret"
  },
  "speech_patterns": {
    "vocabulary": "",
    "cadence": "",
    "verbal_tics": [],
    "example_lines": []
  },
  "relationships": [],
  "story_hooks": [],
  "narrative_function": "",
  "augmentations": "",
  "daily_life": "",
  "affiliation": "${PUBLICATION}",
  "uses_facets": false,
  "narration_voice": "",
  "stats": {
    "physical": { "strength": 1-10, "dexterity": 1-10, "vitality": 1-10, "perception": 1-10 },
    "mental": { "cognition": 1-10, "willpower": 1-10, "creativity": 1-10, "spatial": 1-10 },
    "social": { "presence": 1-10, "empathy": 1-10, "expression": 1-10, "integrity": 1-10 },
    "personality": { "openness_conviction": -5 to 5, "empathy_detachment": -5 to 5, "impulsivity_deliberation": -5 to 5, "assertion_deference": -5 to 5, "transparency_guardedness": -5 to 5 },
    "drives": [],
    "thresholds": {},
    "strengths": [],
    "weaknesses": [],
    "tags": []
  },
  "behavioral": {
    "decision_rules": [],
    "escalation_ladder": [],
    "interpersonal_modes": { "strangers": "", "friends": "" },
    "stress_responses": { "low": "", "medium": "", "high": "" },
    "contradictions": [],
    "habits": [],
    "breaking_points": []
  },
  "cyberware_inventory": [],
  "belongings": {
    "primary_weapon": "",
    "secondary_weapon": "",
    "armor": "",
    "vehicle": "",
    "residence": "",
    "clothing_style": "",
    "favorite_drink": "",
    "favorite_food": "",
    "stimulant": "",
    "comm_device": "",
    "signature_gear": [],
    "pharmaceuticals": [],
    "other": {}
  },
  "archetypes": {},
  "operating_territory": {
    "home_turf": "",
    "familiar_zones": [],
    "zone_reputation": {},
    "no_go_zones": [],
    "range": ""
  },
  "timeline": [],
  "changelog": [],
  "related_entities": [],
  "district": "",
  "physical_description": {
    "heritage": "Mixed East Asian (Japanese) and West African (Ghanaian) — Ubiquitous Diaspora. Describe phenotype specifically.",
    "height_cm": number,
    "weight_kg": number,
    "build": "",
    "hair_color": "",
    "hair_style": "",
    "hair_length": "",
    "eye_color": "",
    "skin_tone": "",
    "complexion": "",
    "distinguishing_marks": [],
    "visible_augmentations": "",
    "posture_movement": "",
    "clothing_style": ""
  },
  "image_prompt": "detailed image generation prompt",
  "genetic_ancestry": { "East Asian": number, "Sub-Saharan African": number },
  "ancestry_harmonized": true,
  "tags": ["person", "sentient", "human", "journalist", "undercurrent"],
  "tier": "2",
  "dalle3_prompt": "detailed portrait prompt"
}`;

  const user = `Generate the complete character JSON for ${JOURNALIST_NAME}.

Key details to incorporate:
- Early 20s (23), ambitious, mixed Japanese + Ghanaian heritage (Ubiquitous Diaspora)
- Grew up in the Mids (mid-tier GLMZ neighborhood)
- Received a Ringo CorpoNation journalism scholarship — she owes them something specific, and they use this
- Works for The Undercurrent, an independent street-level news feed she joined immediately after her scholarship
- She started covering stories Ringo specifically didn't want covered — corporate labor abuses, Signal Network discrimination, Arcturus brutality in the Shelf
- Not naive but not burned out. She believes in the work genuinely, not performatively.
- Has a specific relationship with at least one freelancer she's interviewed, at least one broker who has helped her, and one Ringo contact who monitors her
- Her secret: she has been offered a buyout — a full Ringo staff position with full protection — and she has not refused it yet
- Practical gear: field recorder, a basic BCI (Tier 2 level, nothing fancy), encrypted comms, knowledge of the Signal Network structure from her reporting

Return ONLY the JSON object.`;

  try {
    const result = await callClaude(system, user, 8192);
    const journalist = parseJson(result);
    journalist.id = journalist.id || generateId();

    fs.writeFileSync(path.join(PEOPLE_DIR, JOURNALIST_FILE), JSON.stringify(journalist, null, 2), 'utf8');
    console.log(`  WROTE: ${JOURNALIST_FILE}`);
    return journalist;
  } catch (e) {
    console.error(`  ERROR generating journalist: ${e.message}`);
    return null;
  }
}

// ─── STEP 2: INTERVIEW DOCUMENTS ─────────────────────────────────

const INTERVIEW_SPECS = [
  {
    file_name: 'undercurrent_interview_callsign_seven',
    name: 'The Weight of the Work: A Conversation with an A-Tier Operator',
    subject_type: 'A-tier freelancer',
    subject_name: 'Known only as "Seven" — A-tier, combat and extraction specialist, fifteen years active',
    doc_type: 'interview_transcript',
    topic: 'An A-tier freelancer known as Seven agrees to speak to The Undercurrent on condition that no identifying details are published. The conversation covers: how they became a freelancer (not a heroic origin — a specific circumstance that left no other options), what fifteen years of the work has actually done to them physically and psychologically, one specific job they are proud of and one they cannot stop thinking about, what they think of the Signal Network (it is what it is; they have opinions about specific brokers), what they would say to a C-tier just starting out, and whether they plan to stop. Seven is thoughtful and specific. They push back twice on questions they consider naive. At one point they pause for a long time before answering. The interviewer — ${JOURNALIST_NAME} of ${PUBLICATION} — pushes gently on the contradiction between Seven\'s obvious skill and their obvious tiredness.'
  },
  {
    file_name: 'undercurrent_interview_the_architect',
    name: 'The Architect: An S-Tier Freelancer Speaks',
    subject_type: 'S-tier freelancer',
    subject_name: '"The Architect" — S-tier, operational planning specialist, nearly mythological reputation in broker circles',
    doc_type: 'interview_transcript',
    topic: 'Extremely rare: an S-tier freelancer agrees to an interview with ${PUBLICATION}, with several conditions. The conversation is partially redacted — five exchanges have been replaced with [REDACTED AT SUBJECT\'S REQUEST]. What remains: how the Architect thinks about risk (mathematical, specific, unsettling in its precision), their philosophy on what makes an operation succeed versus fail (answer: not skill — preparation, and most operators don\'t prepare correctly), their opinion on the current state of the Signal Network (it is being gamed by a small number of brokers they name obliquely), whether they have ever refused a job on moral grounds (yes; brief; they do not elaborate), a brief exchange about why they are talking to a journalist at all that goes somewhere unexpected. ${JOURNALIST_NAME}\'s interview notes, appended, say she is not certain the Architect told her the truth about any of it, but she is also not certain they lied.'
  },
  {
    file_name: 'undercurrent_interview_ghost_adjacent',
    name: 'On the Edge of Ghost: What Lies Past S-Tier',
    subject_type: 'A-tier freelancer with Ghost-adjacent knowledge',
    subject_name: 'Unnamed — A-tier contractor who has worked with Ghost-tier operators on two occasions',
    doc_type: 'interview_transcript',
    topic: 'Interview with an A-tier who has had two direct working experiences with what they believe were Ghost-tier operators. They agreed to speak because they are retiring and want the record to reflect something real rather than the mythology. Their account: what those two jobs actually looked like from the outside (specific operational details without identifying information), what made those operators different from S-tier (not power — invisibility, which is a specific operational discipline), what they think Ghost tier actually costs the people who reach it (their answer involves isolation in a specific way), whether they wanted to go Ghost themselves (no, for a specific reason they explain carefully). ${JOURNALIST_NAME} is fascinated and skeptical in equal measure. The interview ends with a question she wishes she hadn\'t asked.'
  },
  {
    file_name: 'undercurrent_interview_war_criminal',
    name: 'The Sudbury Incident: A Perpetrator\'s Account',
    subject_type: 'infamous freelancer — war criminal',
    subject_name: '"Plague" — B-tier at time of incident, subsequently blacklisted from Signal Network; agreed to speak in exchange for publication',
    doc_type: 'interview_transcript',
    topic: 'Interview with a freelancer who was part of a crew that carried out what is known in broker circles as "the Sudbury incident" — a job that involved civilian displacement on a scale that crossed into atrocity. The interview is uncomfortable. Plague does not fully admit wrongdoing; they use a kind of operational language that distances the act from the moral weight. They explain the job from a contractor perspective and, in doing so, reveal how something this bad happens: incrementally, with plausible deniability at each step, with everyone following orders that were technically within their stated parameters. The interviewer pushes hard. Plague pushes back. At one point, the interview nearly ends. What makes this piece more than a simple condemnation: Plague says one thing about the system that allows this that ${JOURNALIST_NAME} cannot find a counter-argument for. She includes this in the final piece against the advice of her editor.'
  },
  {
    file_name: 'undercurrent_interview_crossed_lines',
    name: 'Lines That Were Crossed: On the Moral Arithmetic of Three Jobs',
    subject_type: 'infamous freelancer — morally compromised career',
    subject_name: '"Threshold" — B-tier, known for taking contracts other freelancers refused, history of grey-to-black work',
    doc_type: 'interview_transcript',
    topic: 'Threshold is not a monster in the traditional sense — they are a person who, faced with a series of choices under economic pressure, made the worse choice enough times that their career became defined by it. The interview reconstructs three specific jobs: one where Threshold made a choice that harmed civilians for financial reasons they explain clearly, one where they tried to refuse a bad contract but were leveraged into it, and one they are genuinely ashamed of and still took because they needed the Quanta. The interview is not a redemption narrative. Threshold does not seek absolution. They explain, with uncomfortable clarity, how economic pressure and moral fatigue interact. At the end, they are asked if they would do it differently. Their answer is not what ${JOURNALIST_NAME} expected and she includes it verbatim.'
  },
  {
    file_name: 'undercurrent_interview_the_handler',
    name: 'Accountable to No One: An Interview With a Freelancer Who Was Never Caught',
    subject_type: 'infamous freelancer — operates without accountability',
    subject_name: '"The Handler" — claimed A-tier, disputed; known to brokers as someone who completes jobs but leaves unacceptable collateral',
    doc_type: 'interview_transcript',
    topic: 'The Handler has a reputation in broker circles as someone you go to when you absolutely need the job done and you don\'t want to know how. They agreed to speak and the interview is, itself, a kind of performance — they are presenting a version of themselves. The piece explores: what the Handler thinks accountability means in the informal economy (nothing, unless it damages your reputation), how they have survived doing jobs that blacklisted others (specific, involving broker relationships and strategic information management), what they think of freelancers who refused the jobs they took (contempt, lightly masked as pragmatism), one story they tell that ${JOURNALIST_NAME} believes is designed to frighten her, whether it worked. The interview ends with a question ${JOURNALIST_NAME} asks off the record that the Handler answers on the record, and neither of them is prepared for the consequence of that exchange.'
  },
  {
    file_name: 'undercurrent_interview_retired_teacher',
    name: 'Where the Work Ends: A Retired Freelancer in the Narrows',
    subject_type: 'retired freelancer — living quietly',
    subject_name: 'Amara Jovanovic-Okonkwo, former B-tier, now teaches informal self-defense in a Narrows community space',
    doc_type: 'interview_transcript',
    topic: 'Amara retired twelve years ago after a knee injury that made field work impossible. She runs an informal self-defense class three mornings a week in a reclaimed community space in the Narrows. She agreed to speak because she thinks young people considering freelance work should hear something other than the mythology. The interview covers: what she did (protection and escort, twelve years), what ended it (specific injury, specific job), how she transitioned (not smoothly — specific about the financial difficulty of the transition and who helped), what she teaches now and why, what she thinks she got right and wrong about the work, one piece of advice she gives to every new student that she wishes she had received. The piece is warm without being sentimental. Amara is practical, occasionally funny, and once, briefly, angry at something that happened decades ago that she still carries.'
  },
  {
    file_name: 'undercurrent_interview_retired_broker',
    name: 'The Exit Interview: A Retired Broker Reflects',
    subject_type: 'retired freelancer — former broker',
    subject_name: 'Name withheld; former mid-tier broker, now lives in the outer Shelf, no longer active',
    doc_type: 'interview_transcript',
    topic: 'A retired broker who spent twenty years in the Signal Network agrees to speak because they are past caring about consequences. The interview covers: how they became a broker (specific path), what the job actually required (not just matching contractors with jobs but intelligence gathering, reputation management, managing dangerous clients, and occasionally making decisions about who lives), the three decisions they still question, how the system changed over their twenty years (more concentrated broker power, more CorpoNation clients, less freelancer autonomy), why they left (not a dramatic exit — a slow withdrawal that took three years), what they do now (specific, modest, human), what they would change about the system if they could. The interview ends with the broker saying something about ${JOURNALIST_NAME} herself that she finds unsettling and includes anyway.'
  },
  {
    file_name: 'undercurrent_interview_new_c_tier',
    name: 'Month Three: A C-Tier Freelancer in Progress',
    subject_type: 'C-tier freelancer, currently active',
    subject_name: 'Identified only as "K" — C-tier, three months active, originally from Shelf district 7',
    doc_type: 'interview_transcript',
    topic: 'K is three months into their freelance career and agreed to speak because ${JOURNALIST_NAME} found them through a community mutual-aid network. The interview is different from the others — less polished, more immediate. K describes: the specific job that started their career (mundane retrieval, went fine), the second job (didn\'t go fine — specific), what the money actually looks like (less than expected, more irregular), what they\'ve learned in three months that they didn\'t know going in (several things, specific), what they\'re afraid of (specific, honest), whether they think they made the right choice (they don\'t know yet). K pushes back on one of ${JOURNALIST_NAME}\'s questions in a way that reveals something about how freelancers are perceived versus how they experience themselves. The interview ends with something K says that ${JOURNALIST_NAME} puts in her notes as "the most honest thing anyone has said to me during this project."'
  },
  {
    file_name: 'undercurrent_interview_barely_surviving',
    name: 'The Precarity Report: A C-Tier at the Edge',
    subject_type: 'C-tier freelancer, struggling',
    subject_name: 'Name withheld; C-tier, eighteen months active, currently in debt',
    doc_type: 'interview_transcript',
    topic: 'This interview is difficult. The subject is eighteen months in and is not doing well — they\'ve accumulated medical debt from an injury, they\'re taking jobs they know are too risky because they can\'t afford to say no, and they\'re aware this is the cycle that kills people. They agreed to speak because they want someone to hear what this actually is. The interview covers: the injury and the debt (specific Quanta figures), the pressure to keep working, the specific way brokers treat contractors who are visibly desperate (not well), whether they\'ve asked for help from community networks (yes; specific about what was available and what wasn\'t), what they think happens to them if this continues, whether they would advise anyone to become a freelancer. ${JOURNALIST_NAME} tries to provide a specific resource at the end of the interview. The subject\'s response to this is included in the piece.'
  },
  {
    file_name: 'undercurrent_interview_broker_one',
    name: 'How the Market Actually Works: A Broker Speaks',
    subject_type: 'active broker, anonymous',
    subject_name: 'Anonymous; active Signal Network broker, Shelf/Narrows corridor, identity withheld by mutual agreement',
    doc_type: 'interview_transcript',
    topic: 'A working broker agreed to speak on condition of full anonymity and right to approve quotes before publication. The interview is structured around questions ${JOURNALIST_NAME} has been building across her entire freelancer reporting project. Covers: how brokers actually set payout rates (specific mechanisms, including information the broker is uncomfortable sharing), how the vouching consensus actually works in practice versus theory, what brokers do when a contractor goes wrong (specific options, none of them ideal), the broker\'s opinion on the moral landscape of the market they operate in (honest, uncomfortable, not what you\'d expect from someone who profits from it), what they think is broken about the current system, whether they would share this information if they weren\'t anonymous. One exchange that the broker asked to cut but ${JOURNALIST_NAME} kept, with disclosure, because it reveals something important.'
  },
  {
    file_name: 'undercurrent_interview_broker_two',
    name: 'The Other Side of the Table: A Broker on What Gets Hidden',
    subject_type: 'active broker, anonymous',
    subject_name: 'Anonymous; senior active broker, upper Shelf/lower Narrows; different perspective from Broker One',
    doc_type: 'interview_transcript',
    topic: 'A second broker agreed to speak specifically because they disagreed with how the Signal Network is typically portrayed — they want to argue that it is more protective of freelancers than critics claim. The interview becomes a debate. The broker makes a genuine case: that the decentralized vouching system protects contractors from the kind of centralized control that formal labor markets impose, that the tier system is more meritocratic than critics acknowledge, that the informal nature of the market allows for flexibility that formal employment can\'t. ${JOURNALIST_NAME} pushes back with specific cases from her reporting. The broker has answers for some of them and not others. At the end, the broker concedes one specific thing they previously defended. ${JOURNALIST_NAME}\'s post-interview note: "This person is not wrong about the decentralization argument. They are also not fully honest about who the decentralization protects."'
  },
  {
    file_name: 'undercurrent_interview_hired_mother',
    name: 'What a Mother Does When the System Won\'t Help',
    subject_type: 'civilian who hired a freelancer',
    subject_name: 'Patience Ajani-Svensson — Shelf Tier 1 resident, mother; hired a freelancer to find her missing son',
    doc_type: 'interview_transcript',
    topic: 'Patience\'s sixteen-year-old son disappeared four months ago. Arcturus Civil Security conducted a two-day search and closed the case. She hired a C-tier freelancer from a community referral using money borrowed from three neighbors. The interview covers: the specific moment she decided to hire a freelancer (what Arcturus told her, what they didn\'t do), how she found the contractor (community mutual-aid network), what the contractor actually did (specific, methodical, very unglamorous), what the outcome was (not clean — the son was found but the circumstances were complicated, and the interview doesn\'t resolve everything cleanly), how much it cost (Φ figure) and where the money came from, what she thinks about a system where a mother in Tier 1 has to do this, what she would say to someone in her position now. The interview is the most emotionally direct piece in the series.'
  },
  {
    file_name: 'undercurrent_interview_small_business_protection',
    name: 'Arcturus Said No: How a Small Business Owner Hired Private Security',
    subject_type: 'civilian who hired a freelancer',
    subject_name: 'Desmond Osei-Harrington — runs a small repair shop in the Shelf, Tier 2 adjacent; hired protection when extortion threats escalated',
    doc_type: 'interview_transcript',
    topic: 'Desmond\'s repair shop started receiving protection demands from a local gang affiliated (loosely) with a Tier 3 logistics subsidiary. When he reported it, Arcturus told him it was below their operational threshold. He hired a B-tier freelancer. The interview covers: what the extortion looked like (specific, grinding, not dramatic), his decision to hire a freelancer (what it cost, what he understood the risks to be), what the freelancer actually did (he describes it in detail — negotiation more than combat, with a very specific threat delivered very precisely), the outcome (the demands stopped; the gang did not escalate; he still doesn\'t know exactly what was said to them), what he thinks about depending on an informal security market because formal security is unaffordable and corrupt, what he tells other shop owners in his block who ask him what he did. The interview is also a piece about the specific gap that freelancers fill in the GLMZ economy.'
  },
  {
    file_name: 'undercurrent_interview_survived_target',
    name: 'What It\'s Like to Be a Target',
    subject_type: 'person targeted by a freelancer — survived',
    subject_name: 'Identified as "M" — mid-level administrator at a Tier 3 CorpoNation subsidiary; target of an elimination contract; survived',
    doc_type: 'interview_transcript',
    topic: 'M was the target of an elimination contract three years ago. They survived because the contractor — A-tier — apparently assessed the job and decided the moral weight wasn\'t clean enough to complete. They later confirmed through indirect channels that the job had been cancelled after the contractor withdrew. The interview covers: when M realized they were a target (specific, frightening in its ordinariness), what the experience of being surveilled before an attempt felt like in retrospect, the period of uncertainty about whether the contract was active or complete (how they functioned during this), what they think about the contractor who didn\'t complete the job (complicated — they owe their life to someone whose business is killing people), what they know about who posted the contract and why (some information, not complete), how their life has changed since. M asked that certain details be changed; ${JOURNALIST_NAME} confirms this in a note. The interview is about what it means to live in a city where this is a real thing that happens.'
  },
  {
    file_name: 'undercurrent_interview_survived_collateral',
    name: 'Collateral: What Happens When a Job Goes Wrong Around You',
    subject_type: 'civilian affected by freelancer activity — survived',
    subject_name: 'Linh Nguyen-Baptiste — Shelf resident, Tier 1; injured in crossfire from a job that was not related to her at all',
    doc_type: 'interview_transcript',
    topic: 'Linh was injured in an incident that turned out to be a freelance job gone sideways — she was not the target, not involved, was walking home. The injury required surgery she could not fully afford. The interview covers: the incident itself (her account, which is fragmentary and physical), the immediate aftermath (medical care, cost, what community resources existed), the year since (recovery, financial impact, ongoing pain), whether she has any recourse (no; specific about why not — Arcturus investigated, found it was a freelance operation, closed the case), her opinion of freelancers (complex, honest, not simply angry), whether she knows who was involved (partially; she doesn\'t want to pursue it), what she thinks the city owes people in her position. ${JOURNALIST_NAME}\'s post-interview note: "I have been working on this series for eight months. This is the interview that I think about most."'
  },
  {
    file_name: 'undercurrent_why_freelancers',
    name: 'Why They Do It: A Collage of First Reasons',
    subject_type: 'thematic piece — multiple voices',
    subject_name: 'Compiled by ${JOURNALIST_NAME}; drawn from interviews across the series and additional brief conversations',
    doc_type: 'interview_transcript',
    topic: 'A thematic piece drawing together the "why" from multiple sources across the series and additional brief conversations. Format: short excerpts, each clearly attributed to a type of speaker (a C-tier six months in, a retired B-tier, a broker, someone who wanted to become a freelancer but didn\'t, a family member of a freelancer, an A-tier who has never explained their beginning to anyone before). The reasons are varied and specific: economic necessity (most common, described in specific terms), no other options after a specific life event, attraction to autonomy that turned out to be more complicated than expected, following someone else into the work, a specific injustice that required capabilities the person didn\'t have yet and so they built them. ${JOURNALIST_NAME}\'s framing paragraph at the start and end does not moralize. She notes that no one she spoke to said "for the adventure." She notes what they said instead.'
  },
  {
    file_name: 'undercurrent_where_they_go',
    name: 'When It\'s Over: An Anthology of Endings',
    subject_type: 'thematic piece — end-of-career anthology',
    subject_name: 'Multiple voices; compiled by ${JOURNALIST_NAME}',
    doc_type: 'interview_transcript',
    topic: 'Companion piece to "Why They Do It": a collection of endings — where freelancers go when the career ends. Format: similar to the first, but focused on the end rather than the beginning. Include: someone who stopped before they were ready and why, someone who stopped at exactly the right time and how they knew, someone who didn\'t stop when they should have and survived anyway and the cost, someone whose career ended involuntarily (injury, blacklisting, a specific CorpoNation action), someone who transitioned into broker work and how that felt, someone who left GLMZ entirely and the brief account of where and why, someone who died on their last job and is represented here by one person who knew them (a very short entry, specifically because there is not more to say). The piece is structured like a collection of photographs from different angles of the same subject. ${JOURNALIST_NAME}\'s framing: she does not know what the right ending to a freelancer career is. She is not sure any ending is the right one.'
  },
  {
    file_name: 'undercurrent_ringo_CorpoNation_piece',
    name: 'What Ringo Bought: On Scholarship Programs and Controlled Voices',
    subject_type: 'controversial investigative piece — got journalist in trouble',
    subject_name: 'Reported by ${JOURNALIST_NAME}; published in ${PUBLICATION} over Ringo CorpoNation objections',
    doc_type: 'interview_transcript',
    topic: 'The article that got ${JOURNALIST_NAME} called into a formal meeting with Ringo\'s communications office and was the first explicit use of the scholarship leverage against her. The piece investigates Ringo\'s journalism scholarship program — which ${JOURNALIST_NAME} herself benefited from — and argues it functions as a soft-control mechanism: recipients are technically free but are embedded in a gratitude debt that shapes their coverage. The piece includes: interviews with four other scholarship recipients (all anonymous) describing similar pressures, a statistical analysis of what scholarship recipients cover vs. independent journalists (significant divergence on CorpoNation-critical stories), specific communications from Ringo that ${JOURNALIST_NAME} obtained through a source, her own first-person disclosure of her own scholarship and its conditions, and a response from Ringo\'s communications office that she includes in full. The article ends with a question she cannot answer: whether she herself has been shaped by the scholarship she is writing about, and how she would know.'
  },
  {
    file_name: 'undercurrent_arcturus_shelf_piece',
    name: 'The Door at Night: Arcturus Civil Security in the Shelf',
    subject_type: 'controversial investigative piece — got journalist in trouble',
    subject_name: 'Reported by ${JOURNALIST_NAME}; published in ${PUBLICATION}; Arcturus filed a formal complaint with The Undercurrent\'s hosting platform',
    doc_type: 'interview_transcript',
    topic: 'Investigative piece documenting Arcturus Civil Security\'s enforcement patterns in Tier 1 Shelf districts: the frequency of nighttime operations compared to Tier 3+ areas, the rate of "operational incidents" (Arcturus\'s term for injuries and deaths during enforcement), the specific neighborhoods where enforcement is highest and the demographic profile of those neighborhoods, interviews with six Shelf residents (all anonymous) describing specific incidents with Arcturus, the specific contract terms between Arcturus and the CorpoNations that effectively create enforcement-free zones for corporate assets in Shelf space, the formal complaint from a Tier 2 neighborhood council that was dismissed. ${JOURNALIST_NAME} includes the formal Arcturus response, which disputes every specific figure while declining to provide alternative figures. The piece ends with a document she obtained: an internal Arcturus scheduling memo that reveals enforcement operations in the Shelf are calendared around CorpoNation delivery windows rather than crime patterns. The publication of this piece led to a six-week period during which ${JOURNALIST_NAME} needed the protection arrangements she later wrote about.'
  },
];

async function generateInterviewDoc(spec, existingNames) {
  // Replace template references to journalist
  const resolvedSpec = {
    ...spec,
    topic: spec.topic
      .replace(/\$\{JOURNALIST_NAME\}/g, JOURNALIST_NAME)
      .replace(/\$\{PUBLICATION\}/g, PUBLICATION)
  };

  if (existingNames.has(resolvedSpec.file_name.toLowerCase())) {
    console.log(`  SKIP: ${resolvedSpec.file_name}`);
    return null;
  }

  const system = WORLD_CONTEXT + `

You are writing an in-world journalism document for ${PUBLICATION}.
The journalist is ${JOURNALIST_NAME}: early 20s, ambitious, mixed East Asian + West African heritage, grew up in the Mids. She received a Ringo CorpoNation journalism scholarship and immediately started covering the stories they didn't want covered. Not naive but not burned out. She believes in the work.

Generate a SINGLE document as a JSON object with EXACTLY these fields:
{
  "id": "32-char hex",
  "name": "Document title",
  "type": "document",
  "file_name": "the exact file_name slug provided",
  "doc_type": "interview_transcript",
  "author": "${JOURNALIST_NAME}, ${PUBLICATION}",
  "description": "One paragraph archive description of what this document is and why it exists.",
  "body": "The FULL document text (700-1600 words). Write IN-WORLD as an actual published piece or transcript — not a summary, not notes, the actual journalism. Include ${JOURNALIST_NAME}'s byline at the top and publication name.",
  "tags": ["document", "journalism", "undercurrent", "freelancer", "...relevant tags"]
}

Return ONLY the JSON object. No markdown fences.`;

  const user = `Generate the journalism document with file_name: "${resolvedSpec.file_name}"
Title: ${resolvedSpec.name}
Subject type: ${resolvedSpec.subject_type}
Subject: ${resolvedSpec.subject_name}
Doc type: ${resolvedSpec.doc_type}
Content guidance: ${resolvedSpec.topic}

Write this as actual published journalism — the full transcript, article, or piece as it would appear in ${PUBLICATION}. 700-1600 words of body text. Include ${JOURNALIST_NAME}'s byline, the publication name, and write the journalism with her specific voice: direct, specific questions, not performing cynicism, lets subjects talk, pushes on contradictions without being combative.`;

  try {
    let text = await callClaude(system, user, 8192);
    text = text.trim();
    if (text.startsWith('```')) {
      text = text.substring(text.indexOf('\n') + 1);
      if (text.endsWith('```')) text = text.slice(0, -3);
      text = text.trim();
    }
    const doc = JSON.parse(text);
    doc.file_name = resolvedSpec.file_name; // enforce slug
    return doc;
  } catch (e) {
    console.error(`  ERROR generating ${resolvedSpec.file_name}: ${e.message}`);
    return null;
  }
}

async function main() {
  console.log('=== generate_freelancer_journalist.js ===');
  console.log(`People dir: ${PEOPLE_DIR}`);
  console.log(`Docs dir: ${DOCS_DIR}`);

  // Step 1: Generate journalist character
  console.log('\n--- Step 1: Journalist Character ---');
  const journalist = await generateJournalist();
  if (!journalist) {
    console.error('Failed to generate journalist. Continuing to documents...');
  }

  await sleep(WAIT_MS);

  // Step 2: Generate interview documents
  console.log('\n--- Step 2: Interview Documents ---');
  const existingDocNames = getExistingDocFileNames();
  console.log(`Existing documents: ${existingDocNames.size}`);

  let totalWritten = 0;
  let totalSkipped = 0;

  for (let i = 0; i < INTERVIEW_SPECS.length; i++) {
    const spec = INTERVIEW_SPECS[i];
    console.log(`\n[${i + 1}/${INTERVIEW_SPECS.length}] ${spec.file_name}`);

    if (existingDocNames.has(spec.file_name.toLowerCase())) {
      console.log(`  SKIP (already exists)`);
      totalSkipped++;
      continue;
    }

    const doc = await generateInterviewDoc(spec, existingDocNames);
    if (doc) {
      if (writeDocument(doc, existingDocNames)) {
        totalWritten++;
        console.log(`  WROTE: ${spec.file_name}`);
      } else {
        totalSkipped++;
      }
    }

    if (i < INTERVIEW_SPECS.length - 1) {
      await sleep(WAIT_MS);
    }
  }

  const finalPeopleCount = fs.readdirSync(PEOPLE_DIR).filter(f => f.endsWith('.json')).length;
  const finalDocsCount = fs.readdirSync(DOCS_DIR).filter(f => f.endsWith('.json')).length;

  console.log(`\n=== DONE ===`);
  console.log(`Interview documents written this run: ${totalWritten}`);
  console.log(`Interview documents skipped: ${totalSkipped}`);
  console.log(`Total people in directory: ${finalPeopleCount}`);
  console.log(`Total documents in directory: ${finalDocsCount}`);
}

main().catch(e => {
  console.error('Fatal error:', e);
  process.exit(1);
});
