// Pharmaceutical generator for StreetSamurai
// Generates 256 pharmaceutical JSON files in engine_data/pharmaceuticals/
// Run: node generate_pharmaceuticals.js
// Does NOT overwrite existing files.

const fs = require('fs');
const https = require('https');
const path = require('path');

const settings = JSON.parse(fs.readFileSync(
  path.join(process.env.LOCALAPPDATA, 'MindAttic', 'StreetSamurai', 'Settings.json'), 'utf8'));
const API_KEY = settings.ApiKey;
const MODEL = 'claude-sonnet-4-6';
const ENGINE_DATA = path.join(__dirname, '..', 'engine', 'data');
const OUTPUT_DIR = path.join(ENGINE_DATA, 'pharmaceuticals');
const WAIT_MS = 3000;
const sleep = ms => new Promise(r => setTimeout(r, ms));

if (!fs.existsSync(OUTPUT_DIR)) fs.mkdirSync(OUTPUT_DIR, { recursive: true });

function callClaude(system, user, maxTokens = 8192) {
  return new Promise((resolve, reject) => {
    const body = JSON.stringify({
      model: MODEL,
      max_tokens: maxTokens,
      temperature: 0.9,
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
  const start = json.indexOf('[');
  const end = json.lastIndexOf(']');
  if (start === -1 || end === -1) throw new Error('No JSON array found in response');
  json = json.substring(start, end + 1);
  return JSON.parse(json);
}

function slugify(name) {
  return name.toLowerCase()
    .replace(/[^a-z0-9]+/g, '_')
    .replace(/^_+|_+$/g, '');
}

function saveItem(item) {
  const slug = slugify(item.name);
  const filePath = path.join(OUTPUT_DIR, `${slug}.json`);
  if (fs.existsSync(filePath)) {
    console.log(`    SKIP (exists): ${item.name}`);
    return false;
  }
  fs.writeFileSync(filePath, JSON.stringify(item, null, 2));
  return true;
}

function getExistingNames() {
  const files = fs.readdirSync(OUTPUT_DIR).filter(f => f.endsWith('.json'));
  const names = [];
  for (const f of files) {
    try {
      const data = JSON.parse(fs.readFileSync(path.join(OUTPUT_DIR, f), 'utf8'));
      if (data.name) names.push(data.name);
    } catch (e) { /* skip bad files */ }
  }
  return names;
}

function getExistingByCategory() {
  const files = fs.readdirSync(OUTPUT_DIR).filter(f => f.endsWith('.json'));
  const byCat = {};
  for (const f of files) {
    try {
      const data = JSON.parse(fs.readFileSync(path.join(OUTPUT_DIR, f), 'utf8'));
      const cat = data.category || 'unknown';
      if (!byCat[cat]) byCat[cat] = [];
      byCat[cat].push(data.name);
    } catch (e) { /* skip */ }
  }
  return byCat;
}

// ── World Context ──
const WORLD_CONTEXT = `Setting: GLMZ, year 2200. A megacity in the Great Lakes corridor (Chicago-Milwaukee). Currency is Phi (Φ). Society is tiered: Tier 1 (the Shelf — poorest, most dangerous), Tier 2 (working class), Tier 3 (middle), Tier 4 (corporate comfort), Tier 5 (the Spire — ultra-elite).

Technology: BCI (brain-computer interfaces) are ubiquitous. Augmentation (cyberware/chrome) ranges from basic to military-grade. Geneware allows cosmetic and functional genetic modification. SNT (Synthetic Neural Tissue) is the foundational biotech of the era — living neural matter that bridges organic and synthetic systems.

Major pharmaceutical CorpoNations: Lazarus Pharmaceuticals (largest, most corporate, Tier 3-5 focus), Helix Biosystems (cutting-edge biotech, experimental), Novafold Pharmaceuticals (mid-market, reliable), Nightshade Pharmatech (gray market, dual-use — legal products with illegal applications). Street chemists and Shelf labs also produce unlicensed drugs.

Ubiquitous Diaspora: By 2200, humanity is fully racially interbred. Cultural traditions persist but ethnicity as a concept has dissolved. Drug naming reflects this — names draw from every linguistic tradition freely.`;

// ── Category Definitions ──
const CATEGORIES = [
  {
    category: 'combat_stimulant',
    count: 30,
    prompt: `Generate {count} combat stimulants for GLMZ. These are what runners, mercenaries, and soldiers take before or during violence. Include: adrenal boosters that flood the system with synthetic adrenaline, pain suppressors that let you fight through injuries that should drop you, reflex accelerators that shave milliseconds off reaction time (critical when facing augmented opponents), berserker compounds that trade judgment for raw aggression, fear inhibitors that chemically suppress the amygdala response, endurance sustainers for long ops, and combat focus drugs that narrow perception to threat-only awareness. Some are military-grade (Tier 5, prescription or military_only), some are street versions (Tier 1-2, illegal, impure but effective). Manufacturers range from Lazarus Pharmaceuticals to unnamed Shelf chemists.`
  },
  {
    category: 'recreational',
    count: 40,
    prompt: `Generate {count} recreational drugs for GLMZ — the highs of 2200. Include: neural euphoriants that stimulate pleasure centers directly via BCI interaction, sensory amplifiers that make colors brighter and music transcendent, synesthetic inducers that let you taste sound and hear color, memory replay drugs that let you re-experience your best memories in vivid detail (addictive because reality can't compete), empathy boosters that let you feel what others feel (used at parties, dangerous in crowds), dissociatives that disconnect you from your body (popular with people who hate their chrome), hallucinogens adapted for BCI interaction (the trip syncs with your neural interface for structured hallucinations), social lubricants, and bliss compounds. Range from Tier 1 Shelf party drugs to Tier 5 designer experiences.`
  },
  {
    category: 'cognitive_enhancer',
    count: 30,
    prompt: `Generate {count} cognitive enhancers for GLMZ. These are what corporate workers take to keep up, what students take before exams, what analysts take when parsing massive datasets through their BCI. Include: focus drugs that eliminate distraction for hours, memory consolidators that lock short-term memory into permanent storage, learning accelerators that speed up skill acquisition, pattern recognition boosters that let you see connections in data, creativity stimulants that unlock lateral thinking, processing speed enhancers that work with BCI to accelerate thought, and multi-tasking aids. Some are legal prescriptions (Tier 3-5), some are street versions (cheaper, rougher, more side effects). Lazarus and Novafold dominate the legal market.`
  },
  {
    category: 'emotional_regulator',
    count: 25,
    prompt: `Generate {count} emotional regulators for GLMZ. The pharmacology of feelings. Include: grief suppressors (popular after loss — suppress mourning so you can keep working), anxiety dampeners (the most prescribed drug category in GLMZ), confidence builders (chemical courage for job interviews, confrontations, public speaking), emotional flatliners that let you feel nothing for 8 hours (popular with trauma survivors, interrogators, and people doing terrible jobs), bonding enhancers that deepen trust and attachment (used in corporate team-building AND romantic relationships — ethically questionable), anger suppressors, motivation injectors, and contentment sustainers. These drugs prop up a society that demands emotional performance. Legal to restricted.`
  },
  {
    category: 'sleep_dream',
    count: 20,
    prompt: `Generate {count} sleep and dream drugs for GLMZ. Include: sleep inducers that knock you out in 30 seconds (necessary when your BCI keeps pinging you), dream enhancers that make dreams vivid and memorable, lucid dream enablers that let you control your dreamscape (some people live more in dreams than waking), nightmare suppressors (critical for people with PTSD from chrome rejection or combat), sleep eliminators that let you stay awake 72 hours with no cognitive decline (you crash HARD after — 20 hours of comatose sleep), sleep compressors (get 8 hours of rest in 3 hours — expensive, Tier 4-5), dream recorders that work with BCI to save dreams as playable files, and REM optimizers.`
  },
  {
    category: 'medical',
    count: 30,
    prompt: `Generate {count} medical pharmaceuticals for GLMZ. These are the drugs that keep augmented humanity functional. Include: augment rejection suppressors (CRITICAL — without these, your body attacks your chrome; most augmented people take these daily), SNT bonding accelerators (speed up integration of Synthetic Neural Tissue with organic nerves), geneware expression stabilizers (prevent your cosmetic geneware from drifting — your decorative tail doesn't grow scales you didn't want), neural inflammation reducers (BCI overuse causes brain swelling), BCI calibration aids (smooth out the neural-digital interface), tissue regeneration boosters, synthetic organ maintenance drugs, immune modulators for the heavily augmented, and anti-scarring compounds for augment sites. These range from cheap generics to premium formulations.`
  },
  {
    category: 'poison_toxin',
    count: 20,
    prompt: `Generate {count} poisons and toxins for GLMZ. The dark pharmacology. Include: assassination tools (fast-acting, slow-acting, untraceable), paralytic agents (used by kidnappers and corpo security), truth serums that work with BCI to suppress the ability to lie, memory erasers that wipe the last 1-72 hours (used to cover tracks), personality dissolvers that temporarily strip ego and identity (terrifying interrogation tool), slow-kill compounds that mimic natural illness (heart failure over 6 months, undetectable), neural disruptors that scramble BCI signals, sensory overload toxins (every nerve fires at once — incapacitating agony), and augment-specific poisons that cause chrome to malfunction. All illegal. All available if you know who to ask.`
  },
  {
    category: 'augment_interactive',
    count: 25,
    prompt: `Generate {count} augment-interactive drugs for GLMZ. These substances specifically interact with cyberware and BCI systems. Include: chrome enhancers that temporarily boost augment performance beyond spec (your arm hits harder, your eyes see further, your reflexes exceed design parameters), neural overclocks that dangerously boost BCI processing speed (risk of seizure, brain bleed, or permanent neural damage), synth-sync compounds that synchronize your augments with another person's for shared sensation (used recreationally and tactically), augment sensitivity modulators (make your chrome feel more or less), rejection cycle breakers (emergency treatment when your body starts fighting its chrome), BCI bandwidth expanders, and sensory augment boosters. Some are prescribed, most are street-level.`
  },
  {
    category: 'withdrawal_aid',
    count: 16,
    prompt: `Generate {count} withdrawal aids for GLMZ. Drugs to help you quit other drugs. Include: addiction breakers that chemically reset dependency pathways (painful but effective), craving suppressors that block the wanting without blocking other emotions, neurochemistry rebalancers that restore baseline brain chemistry after prolonged drug use, withdrawal symptom suppressors (ease the physical horror of coming off hard substances), dopamine system restorators, serotonin rebuilders, neural pathway rerouters (break the habitual triggers), and gradual step-down formulations. Some are legitimate medical products from Novafold. Some are street versions. Some are themselves mildly addictive — trading one dependency for a lesser one.`
  },
  {
    category: 'street_concoction',
    count: 20,
    prompt: `Generate {count} street concoctions for GLMZ. These are homemade Shelf drugs — impure, dangerous, cheap, made from whatever's available. Named by whoever cooked them up. Include: bathtub stimulants cooked from stolen medical supplies, homebrew euphoriants made from industrial chemicals, jury-rigged augment boosters that might enhance your chrome or might fry it, backyard psychedelics grown in abandoned buildings, repurposed veterinary drugs, cocktails of expired pharmaceuticals mixed into new combinations, and drugs made from scrapping old augment components for their chemical coatings. Names should be colorful and street-level — slang, nicknames, references to what the drug does or where it came from. These are DANGEROUS. Side effects are unpredictable. But they're cheap and available.`
  },
];

// ── Main Generation Loop ──
async function generateCategory(catDef) {
  const { category, count, prompt } = catDef;

  const existingByCat = getExistingByCategory();
  const existingInCat = existingByCat[category] || [];
  const needed = count - existingInCat.length;

  if (needed <= 0) {
    console.log(`[${category}] Already have ${existingInCat.length}/${count}. Skipping.`);
    return 0;
  }

  console.log(`\n[${category}] Have ${existingInCat.length}/${count}. Need ${needed} more.`);

  const BATCH = 10;
  let generated = 0;

  for (let i = 0; i < needed; i += BATCH) {
    const batchSize = Math.min(BATCH, needed - i);
    const allExisting = getExistingNames();

    const filledPrompt = prompt.replace('{count}', batchSize);

    const system = `You generate pharmaceutical entries for the world of GLMZ. Return ONLY a JSON array of exactly ${batchSize} pharmaceutical objects. No explanation, no markdown fencing, just the JSON array.

${WORLD_CONTEXT}

Each pharmaceutical MUST have exactly these fields:
{
  "name": "Drug Name",
  "type": "pharmaceutical",
  "aliases": ["street name", "slang term"],
  "category": "${category}",
  "subcategory": "more specific subcategory",
  "manufacturer": "Corp/Lab/Street chemist name",
  "description": "1-3 sentence description of what this drug is and does",
  "method_of_use": "injection|oral|vapor|dermal_patch|neural_direct|sublingual|ocular",
  "effects": ["array of primary effects"],
  "side_effects": ["array of side effects and risks"],
  "duration": "how long it lasts",
  "addiction_risk": "none|low|moderate|high|extreme",
  "tier_availability": "Tier 1-2|Tier 2-3|Tier 3-4|Tier 4-5|All tiers|Tier 1 only|Military only",
  "legality": "legal|prescription|restricted|illegal|military_only",
  "street_price": "Φ amount",
  "cultural_context": "how this drug fits into GLMZ society — who uses it, why, what it means",
  "story_hooks": ["array of 2-3 narrative hooks for this drug"]
}

CRITICAL: Drug names should sound REAL — like actual pharmaceutical brand names or street drug names. Not jokes, not parodies. Think how real drugs sound: Adderall, Xanax, Molly, Oxy, Ketamine, Fentanyl. Mix clinical names with street names. Every drug must feel like something people actually use.`;

    const user = `${filledPrompt}

EXISTING DRUG NAMES (DO NOT DUPLICATE ANY): ${allExisting.join(', ')}

Generate exactly ${batchSize} pharmaceuticals in the ${category} category. Return ONLY the JSON array.`;

    console.log(`  Batch: ${batchSize} items (${i + 1}-${i + batchSize} of ${needed})...`);

    let retries = 0;
    while (retries < 3) {
      try {
        const result = await callClaude(system, user, 8192);
        const items = parseJsonArray(result);

        let saved = 0;
        for (const item of items) {
          item.type = 'pharmaceutical';
          item.category = category;
          if (saveItem(item)) {
            saved++;
            generated++;
          }
        }
        console.log(`    Saved ${saved}/${items.length} items.`);
        break;
      } catch (e) {
        retries++;
        console.error(`    Error (attempt ${retries}/3): ${e.message}`);
        if (retries < 3) {
          console.log(`    Retrying in ${WAIT_MS/1000}s...`);
          await sleep(WAIT_MS);
        }
      }
    }

    if (i + BATCH < needed) {
      await sleep(WAIT_MS);
    }
  }

  console.log(`[${category}] Generated ${generated} new items.`);
  return generated;
}

async function main() {
  console.log('=== StreetSamurai Pharmaceutical Generator ===');
  console.log(`Output: ${OUTPUT_DIR}`);
  console.log(`Target: 256 pharmaceuticals across ${CATEGORIES.length} categories\n`);

  const existingFiles = fs.readdirSync(OUTPUT_DIR).filter(f => f.endsWith('.json'));
  console.log(`Existing files: ${existingFiles.length}`);

  const totalTarget = CATEGORIES.reduce((s, c) => s + c.count, 0);
  console.log(`Total target: ${totalTarget}`);

  let totalGenerated = 0;

  for (const catDef of CATEGORIES) {
    const n = await generateCategory(catDef);
    totalGenerated += n;
  }

  const finalCount = fs.readdirSync(OUTPUT_DIR).filter(f => f.endsWith('.json')).length;
  console.log(`\n=== DONE ===`);
  console.log(`Total files in pharmaceuticals/: ${finalCount}`);
  console.log(`Generated this run: ${totalGenerated}`);
}

main().catch(e => {
  console.error('Fatal error:', e);
  process.exit(1);
});
