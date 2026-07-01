// Apparel outfit generator for StreetSamurai
// Generates 200 complete outfit JSON files in engine/data/apparel/
// Run: node generate_apparel_5.js
// Does NOT overwrite existing files.

const fs = require('fs');
const crypto = require('crypto');
const https = require('https');
const path = require('path');

const settings = JSON.parse(fs.readFileSync(
  path.join(process.env.LOCALAPPDATA, 'MindAttic', 'StreetSamurai', 'Settings.json'), 'utf8'));
const API_KEY = settings.ApiKey;
const MODEL = 'claude-sonnet-4-6';
const OUTPUT_DIR = path.join(__dirname, '..', 'engine', 'data', 'apparel');
const WAIT_MS = 3000;
const sleep = ms => new Promise(r => setTimeout(r, ms));

if (!fs.existsSync(OUTPUT_DIR)) fs.mkdirSync(OUTPUT_DIR, { recursive: true });

function callClaude(system, user, maxTokens = 16384) {
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
    req.setTimeout(120000, () => {
      req.destroy();
      reject(new Error('Request timed out after 120s'));
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
    .replace(/^_+|_+$/g, '')
    .slice(0, 80);
}

function genId() {
  return crypto.randomBytes(16).toString('hex');
}

function saveOutfit(outfit) {
  const slug = slugify(outfit.name.slice(0, 60));
  const filePath = path.join(OUTPUT_DIR, `${slug}.json`);
  if (fs.existsSync(filePath)) {
    console.log(`    SKIP (exists): ${outfit.name}`);
    return false;
  }
  fs.writeFileSync(filePath, JSON.stringify(outfit, null, 2));
  return true;
}

function getExistingNames() {
  const files = fs.readdirSync(OUTPUT_DIR).filter(f => f.endsWith('.json'));
  const names = [];
  for (const f of files) {
    try {
      const data = JSON.parse(fs.readFileSync(path.join(OUTPUT_DIR, f), 'utf8'));
      if (data.name && data.category === 'outfit') names.push(data.name);
    } catch (e) { /* skip bad files */ }
  }
  return names;
}

function getExistingByTag() {
  const files = fs.readdirSync(OUTPUT_DIR).filter(f => f.endsWith('.json'));
  const byTag = {};
  for (const f of files) {
    try {
      const data = JSON.parse(fs.readFileSync(path.join(OUTPUT_DIR, f), 'utf8'));
      const tags = data.tags || [];
      for (const t of tags) {
        if (!byTag[t]) byTag[t] = [];
        byTag[t].push(data.name);
      }
    } catch (e) { /* skip */ }
  }
  return byTag;
}

// ── World Context ──
const WORLD_CONTEXT = `Setting: GLMZ (GLMZ), year 2200. A megacity in the Great Lakes corridor (Chicago-Milwaukee). Currency is Phi (the symbol is \u03A6, representing QUANTA, not the Greek letter). Society is tiered:
- Tier 1 "The Shelf" — poorest, most dangerous. Reclaimed industrial zones, acid rain, patched infrastructure.
- Tier 2 "Circuit" — working class. Factory workers, transit operators, street vendors. Clean but functional.
- Tier 3 — middle management, cubicle workers, small business owners.
- Tier 4 — corporate comfort. Junior execs, specialists, skilled professionals.
- Tier 5 "The Spire" — ultra-elite. CorpoNation C-suite, power brokers, old money.

Ubiquitous Diaspora: By 2200, humanity is fully racially interbred. Default to mixed heritage from unexpected global combinations. Fashion reflects global fusion — no single cultural tradition dominates.

Technology: BCI (brain-computer interfaces) are common. Augmentation (cyberware/chrome) ranges from basic to military-grade. Geneware allows cosmetic and functional genetic modification (tails, bioluminescence, fur, horns, non-functional wings). Synthetics are artificial beings with non-human body proportions.

CorpoNations are sovereign corporate entities. They manufacture most goods. Street brands also exist — unlicensed, often better for specific niches, always with underground cachet.

Fashion notes: Clothing must accommodate augmentation (chrome arms, leg prosthetics, spinal rigs, neural ports) and geneware (tails, horns, wings, fur, scales). Aug-compatible means openings, channels, or adaptive seams for chrome. Gene-compatible means accommodation for biological modifications.`;

// ── Outfit Categories ──
const CATEGORIES = [
  {
    tag: 'shelf_street',
    count: 30,
    prompt: `Generate {count} COMPLETE OUTFIT SETS for Shelf (Tier 1) street dwellers. These are worn, patched, functional looks. Acid-rain-resistant patches, self-repair tape holding seams together, boots resoled six times, jackets with salvaged thermal lining. Practical layering. Cargo pockets stuffed with survival gear. Some outfits incorporate salvaged aug-parts as accessories. Colors are muted — grays, rust, faded blacks, stained olive. These people dress to survive, not impress. But there IS style in the Shelf — it's just hard-won. Each outfit should be a complete head-to-toe look that a narrator can reference by name.`
  },
  {
    tag: 'circuit_working',
    count: 30,
    prompt: `Generate {count} COMPLETE OUTFIT SETS for Circuit (Tier 2) working class. Clean but practical. Factory workers, transit operators, street food vendors, dock workers, maintenance crews. Durable synth-fabrics, steel-toe boots, hi-vis elements, tool loops, name patches. Some have company logos (CorpoNation branding). Better construction than Shelf wear but still utilitarian. Colors: navy, charcoal, safety orange accents, company colors. Some incorporate basic aug-accommodation. Each outfit should be a complete head-to-toe look.`
  },
  {
    tag: 'corporate_office',
    count: 20,
    prompt: `Generate {count} COMPLETE OUTFIT SETS for corporate office workers across tiers. Mix Tier 3 cubicle drone looks (mass-produced, conformist, subtly uncomfortable) with Tier 4 specialist wear (better fabrics, personal touches) and Tier 5 executive power outfits (bespoke, temperature-regulating, posture-enhancing, privacy-fabric lined). Corporate fashion is about signaling your tier without overstepping. Tier 3 wears the uniform. Tier 4 bends the rules. Tier 5 IS the rule. Include aug-compatible formal wear and geneware-friendly executive attire.`
  },
  {
    tag: 'runner_operative',
    count: 20,
    prompt: `Generate {count} COMPLETE OUTFIT SETS for runners and operatives — people who do illegal or quasi-legal work for hire. Tactical but not military. Quick-change friendly (reversible jackets, detachable panels). Hidden pockets for weapons and tools. Scanner-resistant fabrics. Reinforced without looking armored. Dark colors but not all-black (that's too obvious). Some blend into Tier 2-3 crowds. Others are recognizable — the runner's rep matters. Integrated holster channels, aug-power routing, and comm gear accommodation. Each is a complete tactical look.`
  },
  {
    tag: 'nightlife_club',
    count: 20,
    prompt: `Generate {count} COMPLETE OUTFIT SETS for nightlife and club culture in GLMZ. Bioluminescent threads, reactive fabrics that pulse with music, chrome-accent fashion, holographic trim, synth-leather, mesh, and sheer panels. Some designed to showcase augmentation — chrome arms and legs become the centerpiece. Geneware-friendly club wear for people with tails, horns, or bioluminescent skin. Gender-fluid across the board. Ranges from Shelf underground rave wear to Spire VIP lounge couture. Bold, expressive, sometimes dangerous-looking.`
  },
  {
    tag: 'military_corpsec',
    count: 15,
    prompt: `Generate {count} COMPLETE OUTFIT/KIT SETS for military and CorpSec (corporate security) personnel. Full tactical kits: armor, boots, helmets/headgear, load-bearing gear, identification markings. CorpoNation security has branded tactical gear — corporate colors and logos on military hardware. National military remnants have their own look. Private security contractors have mixed gear. Some are intimidation-forward (Tier 5 penthouse guards), some are utilitarian (Tier 2 factory security). Include full aug-integration rigs for heavily augmented soldiers.`
  },
  {
    tag: 'medical_scientific',
    count: 15,
    prompt: `Generate {count} COMPLETE OUTFIT SETS for medical and scientific professionals. Hospital scrubs have evolved — antimicrobial smart-fabric, haptic feedback gloves built into sleeves, biometric monitoring woven in. Lab coats with integrated displays. Surgical attire for augmentation technicians (chrome installers). Gene clinic staff wear (adapted for handling geneware procedures). Field medic kits. CorpoNation R&D lab attire. Tier 1 street clinic splicer gear vs Tier 5 pristine surgical suites. Each a complete look.`
  },
  {
    tag: 'underworld_criminal',
    count: 15,
    prompt: `Generate {count} COMPLETE OUTFIT SETS for underworld and criminal figures. Fixer fashion — expensive but understated. Gang identifier looks — specific color combinations, patches, or cybernetic display patterns that mark territory. Smuggler practical wear with hidden compartments. Loan shark intimidation outfits. Black market dealer looks. Underground fighting pit attire. Some are flashy (showing off ill-gotten wealth), others are deliberately anonymous. Scanner-defeating fabrics. Quick-strip designs for evading capture.`
  },
  {
    tag: 'synthetic_android',
    count: 15,
    prompt: `Generate {count} COMPLETE OUTFIT SETS designed for synthetics (artificial beings). Synthetic bodies don't always match human proportions — extra-long limbs, unusual joint placement, non-standard torsos. These outfits are designed for non-human bodies. Some synthetics want to look human (passing outfits). Others embrace their synthetic nature (exposing chassis, transparent panels showing internals). Some are utility-focused (maintenance-friendly, modular). Include outfits for service synthetics, companion synthetics, labor synthetics, and free synthetics expressing individual identity.`
  },
  {
    tag: 'mixed_unique',
    count: 20,
    prompt: `Generate {count} COMPLETE OUTFIT SETS that are unique, character-defining looks. Each should be specific enough to define a character the moment you describe what they're wearing. A retired Spire exec slumming on the Shelf. A geneware-modded street preacher. A synth jazz musician. A Tier 3 office worker moonlighting as a runner. A combat medic turned bartender. A CorpoNation whistleblower in hiding. Each outfit tells a story — it's the intersection of who someone was, who they are, and who they're pretending to be. Wildly varied. No two should feel similar.`
  },
];

// ── Generation Logic ──
async function generateBatch(catDef, existingNames) {
  const { tag, count, prompt } = catDef;

  // Count existing for this tag
  const byTag = getExistingByTag();
  const existingInTag = (byTag[tag] || []).length;
  const needed = count - existingInTag;

  if (needed <= 0) {
    console.log(`[${tag}] Already have ${existingInTag}/${count}. Skipping.`);
    return 0;
  }

  console.log(`\n[${tag}] Have ${existingInTag}/${count}. Need ${needed} more.`);

  const BATCH = 5;
  let generated = 0;

  for (let i = 0; i < needed; i += BATCH) {
    const batchSize = Math.min(BATCH, needed - i);
    const allExisting = getExistingNames();

    const tierMap = {
      'shelf_street': 'Tier 1',
      'circuit_working': 'Tier 2',
      'corporate_office': 'Tier 3-5',
      'runner_operative': 'Tier 2-3',
      'nightlife_club': 'Tier 1-5',
      'military_corpsec': 'Tier 2-5',
      'medical_scientific': 'Tier 1-5',
      'underworld_criminal': 'Tier 1-4',
      'synthetic_android': 'Tier 1-5',
      'mixed_unique': 'Tier 1-5',
    };

    const filledPrompt = prompt.replace('{count}', batchSize);

    const system = `You generate complete outfit entries for the world of GLMZ. Return ONLY a JSON array of exactly ${batchSize} outfit objects. No explanation, no markdown fencing, just the JSON array.

${WORLD_CONTEXT}

Each outfit MUST have exactly these fields:
{
  "id": "<32-character hex string>",
  "name": "Descriptive Outfit Name",
  "type": "apparel",
  "category": "outfit",
  "description": "1-2 paragraphs describing the full look head to toe. Be specific about fabrics, colors, wear patterns, accessories, footwear, and how it all comes together. The narrator should be able to reference this outfit by name and immediately convey a complete visual.",
  "tier_association": "${tierMap[tag] || 'Tier 1-5'}",
  "materials": ["array of materials used"],
  "functionality": "practical features of the outfit",
  "what_it_says": "what this outfit communicates about the wearer — social class, occupation, attitude, history",
  "worn_by": ["types of people who wear this"],
  "manufacturer": "brand or maker — CorpoNation, street label, self-made, etc.",
  "price_range": "price range using the \u03A6 (QUANTA) symbol",
  "aug_compatible": true or false,
  "gene_compatible": true or false,
  "story_hooks": ["2-3 narrative hooks — situations where this outfit matters to a story"],
  "tags": ["apparel", "outfit", "${tag}", "tier X", ...]
}

CRITICAL RULES:
- Each "id" must be a unique 32-character lowercase hex string (like a UUID without dashes).
- "name" must be 60 characters or fewer.
- The \u03A6 symbol represents QUANTA currency, NOT the Greek letter phi.
- Descriptions should be 1-2 paragraphs, vivid and specific.
- Tags must always include "apparel", "outfit", and "${tag}".
- Ubiquitous Diaspora: fashion draws from ALL global traditions freely mixed.`;

    const user = `${filledPrompt}

EXISTING OUTFIT NAMES (DO NOT DUPLICATE ANY): ${allExisting.join(', ')}

Generate exactly ${batchSize} outfits. Return ONLY the JSON array.`;

    console.log(`  Batch ${Math.floor(i / BATCH) + 1}: generating ${batchSize} outfits...`);

    let retries = 0;
    while (retries < 3) {
      try {
        const result = await callClaude(system, user, 16384);
        const outfits = parseJsonArray(result);

        let saved = 0;
        for (const outfit of outfits) {
          // Enforce schema
          outfit.type = 'apparel';
          outfit.category = 'outfit';
          if (!outfit.id || outfit.id.length !== 32) outfit.id = genId();
          if (!outfit.tags) outfit.tags = [];
          if (!outfit.tags.includes('apparel')) outfit.tags.unshift('apparel');
          if (!outfit.tags.includes('outfit')) outfit.tags.push('outfit');
          if (!outfit.tags.includes(tag)) outfit.tags.push(tag);
          // Truncate name for filename
          outfit.name = (outfit.name || 'Unknown Outfit').slice(0, 60);

          if (saveOutfit(outfit)) {
            saved++;
            generated++;
          }
        }
        console.log(`    Saved ${saved}/${outfits.length} outfits.`);
        break;
      } catch (e) {
        retries++;
        console.error(`    Error (attempt ${retries}/3): ${e.message}`);
        if (retries < 3) {
          console.log(`    Retrying in ${WAIT_MS / 1000}s...`);
          await sleep(WAIT_MS);
        }
      }
    }

    if (i + BATCH < needed) {
      await sleep(WAIT_MS);
    }
  }

  console.log(`[${tag}] Generated ${generated} new outfits.`);
  return generated;
}

async function main() {
  console.log('=== StreetSamurai Apparel Outfit Generator ===');
  console.log(`Output: ${OUTPUT_DIR}`);
  const totalTarget = CATEGORIES.reduce((s, c) => s + c.count, 0);
  console.log(`Target: ${totalTarget} outfits across ${CATEGORIES.length} categories\n`);

  const existingFiles = fs.readdirSync(OUTPUT_DIR).filter(f => f.endsWith('.json'));
  console.log(`Existing files: ${existingFiles.length}`);

  let totalGenerated = 0;

  for (const catDef of CATEGORIES) {
    const n = await generateBatch(catDef, []);
    totalGenerated += n;
  }

  const finalCount = fs.readdirSync(OUTPUT_DIR).filter(f => f.endsWith('.json')).length;
  console.log(`\n=== DONE ===`);
  console.log(`Total files in apparel/: ${finalCount}`);
  console.log(`Generated this run: ${totalGenerated}`);
}

main().catch(e => {
  console.error('Fatal error:', e);
  process.exit(1);
});
