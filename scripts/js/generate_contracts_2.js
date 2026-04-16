// generate_contracts_2.js
// Generates 100 new contracts for StreetSamurai/GLMZ 2200
// Output: engine/data/contracts/ (one JSON file per contract)
// Resume-safe: skips contracts whose slugs already exist

const fs = require('fs');
const path = require('path');
const crypto = require('crypto');
const https = require('https');

const settings = JSON.parse(fs.readFileSync(
  path.join(process.env.LOCALAPPDATA, 'MindAttic', 'StreetSamurai', 'Settings.json'), 'utf8'));
const API_KEY = settings.ApiKey;
const MODEL = 'claude-sonnet-4-6';
const OUTPUT_DIR = path.resolve(__dirname, '..', 'engine', 'data', 'contracts');
const WAIT_MS = 3000;

const limitIdx = process.argv.indexOf('--limit');
const BATCH_LIMIT = limitIdx !== -1 ? parseInt(process.argv[limitIdx + 1]) : null;

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

// Load existing contract slugs (from codename field, slugified)
function getExistingSlugs() {
  const slugs = new Set();
  const files = fs.readdirSync(OUTPUT_DIR).filter(f => f.endsWith('.json'));
  for (const file of files) {
    try {
      const data = JSON.parse(fs.readFileSync(path.join(OUTPUT_DIR, file), 'utf8'));
      if (data.codename) slugs.add(slugify(data.codename));
      if (data.name) slugs.add(slugify(data.name));
    } catch (e) { /* skip bad files */ }
  }
  return slugs;
}

function writeContract(contract, existingSlugs) {
  const slug = slugify(contract.codename || contract.name);
  if (existingSlugs.has(slug)) {
    console.log(`  SKIP: ${contract.codename || contract.name}`);
    return false;
  }
  const id = contract.id || generateId();
  contract.id = id;
  const filename = id + '.json';
  fs.writeFileSync(path.join(OUTPUT_DIR, filename), JSON.stringify(contract, null, 2), 'utf8');
  existingSlugs.add(slug);
  return true;
}

const SYSTEM_PROMPT = `You generate contract entries for StreetSamurai, a worldbuilding project set in GLMZ (Great Lakes Metropolitan Zone megacity corridor), year 2200.

WORLD RULES:
- Φ is the Quanta currency symbol (never "phi", never the Greek letter)
- No city police exist. Arcturus Civil Security is the enforcement arm (corporate, not public)
- Tier 1-5 society: Tier 1 = poorest Shelf districts, Tier 5 = corporate elite
- Freelancers are NOT romantic rebels — they are laborers in a brutal informal economy
- Some freelancers are heroes, some are war criminals, most are trying to survive
- No simple moral answers. The world is grinding and specific.
- Missouri is flooded, Kentucky is gone. GLMZ is a megacity corridor across the Great Lakes region.

THE SIGNAL NETWORK (freelancer ranking system — decentralized, NOT a magic database):
- Vouching chains: your reputation is a web of who trusts you, who they trust. No central authority.
- Dead Drops: physical reputation tokens — anonymous encrypted scraps left at physical locations. Brokers aggregate these.
- Tier designations (C/B/A/S/Ghost) awarded by consensus of active brokers in a region, not any single authority.
- C Tier: New/unproven. Gets jobs nobody else wants. Survival rate is the filter.
- B Tier: Proven. Has references. Gets actual work. Most freelancers die here.
- A Tier: Known quantity. Brokers compete to offer them contracts. Enough rep to say no.
- S Tier: Legend status. The job comes to them. Their reputation is a weapon.
- Ghost Tier: Doesn't officially exist. Freelancers where visibility itself is the threat. No vouches, no records, no signal. Just results brokers recognize.

CONTRACT JSON SCHEMA — return each contract as an object with EXACTLY these fields:
{
  "id": "generate a random 32-char hex string",
  "name": "Contract name or mission title",
  "type": "contract",
  "codename": "One or two word street codename (all caps)",
  "contract_tier": "C|B|A|S|Ghost",
  "posted_by": "Entity posting (can be anonymous, front company, named faction, a person)",
  "target": "What/who is the target",
  "objective": "What needs to happen — one or two sentences, operational",
  "location": "Where in GLMZ or outside (be specific — district, building, zone)",
  "payout": "Φ amount (realistic for tier: C=Φ5k-40k, B=Φ40k-200k, A=Φ200k-1M, S=Φ1M+, Ghost=negotiated or unknown)",
  "deadline": "Time pressure if any, or null",
  "complications": ["complication 1 (specific, operational)", "complication 2", "complication 3 (optional)"],
  "moral_weight": "clean|grey|dirty|black",
  "description": "Two paragraphs: first — what the job looks like on the surface to a freelancer reading the posting. Second — what is actually going on underneath, the real context, who actually benefits, what the freelancer is really walking into.",
  "story_hooks": ["Hook 1 — specific narrative possibility", "Hook 2"],
  "related_entities": [],
  "tags": ["contract", "tier-X", "type-keyword"]
}

CRITICAL: Return ONLY a valid JSON array. No prose, no commentary, no markdown fences unless wrapping the array.`;

const BATCHES = [
  {
    num: 1,
    label: 'Retrieval jobs',
    detail: 'Extract a person, object, or data. The thing being retrieved can be wanted, dangerous, stolen, or not what it seems. Include: a buried BCI from a corpse, confidential medical records from a Tier 3 hospital, a whistleblower being held in a corporate wellness facility, an antique from a looted museum, data from a server that technically no longer exists. Be specific about what makes retrieval hard — location, opposition, time window, condition of the thing being retrieved.'
  },
  {
    num: 2,
    label: 'Elimination targets',
    detail: 'Contracts to remove someone or something. Targets range from: a mid-level Arcturus Civil Security commander running extortion on Shelf merchants, a data broker who sold the wrong list to the wrong buyer, a rival corponation\'s chief R&D director, a gang enforcer who has gone off-script, a blackmailer using synthetic-identity recordings. IMPORTANT: Some of these are morally ambiguous — the person being removed might have done something awful, or might be a scapegoat. Show both clean and dirty moral weights.'
  },
  {
    num: 3,
    label: 'Protection/escort contracts',
    detail: 'Keep something or someone alive and in one piece. Include: protecting a Shelf-district community leader during a rezoning hearing that could get violent, escorting a Tier 2 accountant who is testifying against a subsidiary company, protecting a synthetic person who has been marked for deactivation, guarding a shipment of legitimate medical supplies through a gang-controlled corridor, protecting a journalist for 72 hours while she files a dangerous story. The threat should be specific and credible.'
  },
  {
    num: 4,
    label: 'Infiltration/recon',
    detail: 'Find out what is happening inside something. No one will tell you; you have to go in and look. Include: documenting labor conditions inside a sealed manufacturing facility, finding out what a corponation is actually testing in a residential neighborhood, mapping security inside a property before another operation, confirming whether a missing person is being held at a specific location, discovering what a shell company actually does. Emphasis on information gathering over violence.'
  },
  {
    num: 5,
    label: 'Delivery under pressure',
    detail: 'Courier jobs that have gone or will go wrong. The freight is never just freight. Include: a package that must arrive before a legal deadline or a property transfer goes through, a living transplant organ that can\'t be traced through official channels, a document that will incriminate someone powerful if it reaches its destination, a person who needs to be moved without appearing to move, a physical object of unclear but significant value that three different parties are already trying to intercept. The courier doesn\'t always know what they\'re carrying.'
  },
  {
    num: 6,
    label: 'Corporate warfare',
    detail: 'One corponation hiring indirectly against another. The client never says who they are. The targets are always described in operational terms, never as employees of the real enemy. Include: disrupting a competitor\'s supply chain, seeding disinformation into a rival\'s internal communications, creating an incident that will trigger a regulatory audit of a competitor, recruiting a target\'s key technical asset to defect, ensuring a contract bid fails. The freelancer is a proxy weapon; they may or may not realize this.'
  },
  {
    num: 7,
    label: 'Rescue/extraction',
    detail: 'People who cannot get themselves out. The rescuer has to go in and bring them out. Include: a Tier 1 family trapped in a disputed zone during a corponation border dispute, a freelancer who took a job and is now being held by the client, a child who was taken as leverage in a debt dispute, someone in a corporate psychiatric facility who does not belong there, a person who went undercover three months ago and has not checked in. The obstacle to extraction is specific — legal, physical, political, or logistical.'
  },
  {
    num: 8,
    label: 'Sabotage',
    detail: 'Infrastructure, supply chains, reputation. The damage is the point. Include: disabling the automated inspection system at a specific checkpoint for six hours, contaminating a batch of corporate PR data with false documentation before it goes public, ensuring a water processing relay has an outage on a specific night, destroying a corponation\'s public reputation around a product launch, burning a physical warehouse while ensuring the insurance fraud fails. Some are aimed at legitimate targets; some are aimed at civilian infrastructure. Moral weight matters.'
  },
  {
    num: 9,
    label: 'The weird ones',
    detail: 'Contracts nobody can fully explain the purpose of. The client has reasons that are not disclosed. Include: photographing the same building from outside once a month for six months with no explanation given, retrieving personal items from a deceased person\'s apartment before family can claim them (no explanation), ensuring a specific song plays at a specific time in a specific location, keeping watch on someone who turns out to be doing nothing unusual at all, delivering a handwritten letter to someone who is not supposed to know who sent it. The weirdness should be unsettling, not comedic. Something is going on that the freelancer cannot see.'
  },
  {
    num: 10,
    label: 'Ghost-tier jobs',
    detail: 'Things that would get an S-tier freelancer killed if done wrong. Ghost tier. The postings themselves are encoded; brokers only share them with people at Ghost tier. Include: removing evidence of a corponation atrocity that occurred twenty years ago and is still covered up, assassinating someone so protected by layered corponation security that even approaching them requires months of work, obtaining a specific piece of technology that a corponation has declared does not exist, ending something that has been ongoing for a decade and has killed everyone who previously tried to end it, a contract posted by unknown parties with a payout so large it is almost certainly a trap. These should feel genuinely dangerous — not just hard combat, but existentially threatening in terms of what getting caught means.'
  }
];

async function runBatch(batch, existingSlugs) {
  console.log(`\n=== Batch ${batch.num}: ${batch.label} ===`);

  const system = SYSTEM_PROMPT;
  const user = `Generate exactly 10 contracts for the category: ${batch.label.toUpperCase()}.

Category details and guidance: ${batch.detail}

Existing contract codenames to avoid duplicating (slugified for comparison — just don't reuse similar concepts): ${[...existingSlugs].slice(0, 100).join(', ') || 'none yet'}

All 10 contracts must be in this category. Vary the moral weight, tier, location, and who is posting. Make them feel like they come from different layers of GLMZ society — Shelf-level gigs alongside corporate warfare. Be specific and grounded. No generic cyberpunk clichés.

Return ONLY a valid JSON array of 10 contract objects.`;

  try {
    const result = await callClaude(system, user, 16384);
    const contracts = parseJsonArray(result);

    let written = 0;
    let skipped = 0;
    for (const contract of contracts) {
      if (writeContract(contract, existingSlugs)) {
        written++;
        console.log(`  WROTE: [${contract.contract_tier}] ${contract.codename} — ${contract.name}`);
      } else {
        skipped++;
      }
    }
    console.log(`  Batch ${batch.num} complete: ${written} written, ${skipped} skipped`);
    return written;
  } catch (e) {
    console.error(`  ERROR in batch ${batch.num}: ${e.message}`);
    return 0;
  }
}

async function main() {
  console.log('=== generate_contracts_2.js ===');
  console.log(`Output: ${OUTPUT_DIR}`);

  const existingFiles = fs.readdirSync(OUTPUT_DIR).filter(f => f.endsWith('.json'));
  console.log(`Existing contracts: ${existingFiles.length}`);

  const existingSlugs = getExistingSlugs();
  console.log(`Existing slugs loaded: ${existingSlugs.size}`);

  let totalWritten = 0;
  const batchesToRun = BATCH_LIMIT ? BATCHES.slice(0, BATCH_LIMIT) : BATCHES;
  if (BATCH_LIMIT) console.log(`Limiting to ${batchesToRun.length} batch(es).`);

  for (let i = 0; i < batchesToRun.length; i++) {
    const batch = batchesToRun[i];
    const written = await runBatch(batch, existingSlugs);
    totalWritten += written;

    if (i < batchesToRun.length - 1) {
      console.log(`  Waiting ${WAIT_MS / 1000}s before next batch...`);
      await sleep(WAIT_MS);
    }
  }

  const finalCount = fs.readdirSync(OUTPUT_DIR).filter(f => f.endsWith('.json')).length;
  console.log(`\n=== DONE ===`);
  console.log(`Contracts written this run: ${totalWritten}`);
  console.log(`Total contracts in directory: ${finalCount}`);
}

main().catch(e => {
  console.error('Fatal error:', e);
  process.exit(1);
});
