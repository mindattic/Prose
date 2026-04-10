const fs = require('fs');
const path = require('path');
const crypto = require('crypto');

// ─── Output directories ────────────────────────────────────────────
const DATA_ROOT = path.join(__dirname, '..', 'engine', 'data');
const DOC_DIR = path.join(DATA_ROOT, 'documents');
const GOODS_DIR = path.join(DATA_ROOT, 'consumer_goods');

const existingDocs = new Set(fs.readdirSync(DOC_DIR).map(f => f.toLowerCase()));
const existingGoods = new Set(fs.readdirSync(GOODS_DIR).map(f => f.toLowerCase()));

function uid() { return crypto.randomBytes(16).toString('hex'); }

function slugify(str) {
  return str.toLowerCase()
    .replace(/[^a-z0-9]+/g, '_')
    .replace(/^_|_$/g, '')
    .slice(0, 80);
}

let written = 0;
let skipped = 0;

function writeDoc(doc) {
  const filename = doc.file_name + '.json';
  if (existingDocs.has(filename.toLowerCase())) {
    console.log('SKIP (exists): ' + filename);
    skipped++;
    return;
  }
  const lines = doc.body.split('\n');
  doc.line_count = lines.length;
  doc.headings = [];
  for (const line of lines) {
    const m = line.match(/^#{1,3}\s+(.+)/);
    if (m) doc.headings.push(m[1]);
  }
  fs.writeFileSync(path.join(DOC_DIR, filename), JSON.stringify(doc, null, 2), 'utf8');
  console.log('WROTE doc: ' + filename);
  existingDocs.add(filename.toLowerCase());
  written++;
}

function writeGood(good) {
  const filename = slugify(good.name).slice(0, 80) + '.json';
  if (existingGoods.has(filename.toLowerCase())) {
    console.log('SKIP (exists): ' + filename);
    skipped++;
    return;
  }
  fs.writeFileSync(path.join(GOODS_DIR, filename), JSON.stringify(good, null, 2), 'utf8');
  console.log('WROTE good: ' + filename);
  existingGoods.add(filename.toLowerCase());
  written++;
}

// ═══════════════════════════════════════════════════════════════════
// DOCUMENTS — FOOD SYSTEMS (10)
// ═══════════════════════════════════════════════════════════════════

writeDoc({
  file_name: "how_meridian_88_eats",
  id: uid(),
  name: "How GLMZ Eats",
  title: "How GLMZ Eats",
  type: "document",
  document_type: "investigative",
  author: "Lien Okafor-Reyes, Independent Food Systems Analyst",
  date: "2199-03-14",
  classification: "public",
  category: "Food Systems",
  description: "Comprehensive overview of the GLMZ food supply chain from agricultural production to street-level consumption.",
  related_entities: ["meridian_88", "ringo_agritech"],
  credibility: "verified",
  story_hooks: [
    "A disruption at any single point in this chain could starve two million people within seventy-two hours",
    "The real food supply chain has a shadow version — smuggled organics, unlicensed vat operations, and Shelf kitchens that cook things the system never intended to feed anyone"
  ],
  tags: ["document", "food", "supply_chain", "ringo", "agriculture", "tier_1", "tier_5", "shelf", "investigative"],
  body: `# How GLMZ Eats

## The Chain

Every calorie consumed in GLMZ passes through a supply chain that begins 200 kilometers away in the Ringo Agritech controlled agricultural zones and ends in the mouths of 6.2 million residents who have no meaningful alternative food source. This is not a market. It is a pipeline. Understanding it is understanding why the city exists in the shape it does.

Ringo Agritech operates 14 mega-farms in the former agricultural belt — vast automated complexes where engineered crop strains grow under UV arrays in climate-controlled warehouse structures the size of small towns. The soil is long dead. These are hydroponic and aeroponic operations augmented by precision nutrient delivery systems. A single Ringo facility produces enough base carbohydrate to feed 400,000 people per cycle. The crops are not food as any historical farmer would recognize them — they are caloric substrate, optimized for yield per cubic meter per kilowatt-hour.

## From Farm to City

Raw substrate travels from Ringo zones to GLMZ via the Corridor — a fortified ground transit route maintained jointly by Ringo and the city's logistics consortium. The convoys run continuously, 40-ton automated haulers moving in armored columns. The Corridor is one of the most heavily defended stretches of ground in the region, not because of military threat but because the cargo is irreplaceable. A single convoy carries enough base calories to feed the Shelf for a day. Hijacking attempts are rare because the haulers are autonomous, armored, and equipped with anti-personnel countermeasures that make robbery suicidal.

## Processing and Distribution

Inside the city, raw substrate enters the processing tier — a network of food manufacturing facilities that transform caloric base into the products residents actually eat. This is where differentiation happens. The same carbohydrate base becomes Tier 1 protein bars, Tier 3 restaurant-grade vat steak, and Tier 5 artisanal pasta, depending on how many processing steps, flavor compounds, and nutritional supplements are applied. The base cost is the same. The markup is where profit lives.

Distribution follows the tier structure. Tier 1 and 2 receive food through automated dispensary networks — wall-mounted units in residential blocks that deliver pre-packaged meals and staples. Tier 3 has grocery markets and restaurant options. Tier 4 and 5 have curated food experiences — restaurants, personal chefs, and subscription services that deliver meals engineered to individual biometric profiles.

## The Street Level

The Shelf's food economy exists in the gaps between official distribution. Street vendors, collective kitchens, and improvised food stalls operate on margins so thin they'd be invisible to any corporate accounting system. A vendor buys dispensary rations at off-peak prices, adds flavor, texture, and care, and resells at a small markup. The food isn't better nutritionally. It's better because someone made it for you, because it has a name and a face attached to it, because eating it is a social act instead of a caloric transaction.

This is how GLMZ eats: efficiently, inequitably, and with a fragility that everyone understands and nobody talks about. The system works until it doesn't. When it doesn't, people die. See: the 2194 Ringo distribution strike.`
});

writeDoc({
  file_name: "vat_protein_taste_comparison_across_12_brands",
  id: uid(),
  name: "Vat Protein: A Taste Comparison Across 12 Brands",
  title: "Vat Protein: A Taste Comparison Across 12 Brands",
  type: "document",
  document_type: "consumer_report",
  author: "The Meridian Consumer Collective",
  date: "2199-09-22",
  classification: "public",
  category: "Food Systems",
  description: "Blind taste test and nutritional analysis of twelve lab-grown protein products available across GLMZ's tier structure.",
  related_entities: ["meridian_88", "ringo_agritech", "vossen"],
  credibility: "verified",
  story_hooks: [
    "Two brands tested identical at the molecular level despite being marketed at different tiers with a 400% price difference",
    "One premium brand contained unlisted nootropic compounds that technically made it a pharmaceutical, not a food product"
  ],
  tags: ["document", "food", "vat_protein", "consumer_report", "brands", "tier_1", "tier_3", "tier_5", "nutrition"],
  body: `# Vat Protein: A Taste Comparison Across 12 Brands

## Methodology

We purchased twelve vat-grown protein products representing the full range of availability in GLMZ, from Tier 1 dispensary-grade to Tier 5 artisan boutique. Each product was evaluated blind by a panel of 30 tasters across all five tiers. Nutritional content was independently analyzed at the Old Harbor Community Lab. Products were scored on texture, flavor, aroma, mouthfeel, and overall satisfaction on a 10-point scale.

## The Budget Tier: Φ0.20 - Φ1.50

**NutriBloc Standard** (Φ0.22/100g) — The dispensary default. Dense, uniform, faintly grey. Tastes like compressed nothing with a protein aftertaste. Scored 2.1 overall. Nutritionally adequate in the way that survival is adequate. Our Tier 1 panelists noted they've eaten this daily for years and stopped tasting it long ago. The protein is real. The experience is not.

**ShelfMeat Patty** (Φ0.45/100g) — A step above dispensary grade. Shaped like a burger, tastes like one if you've never had a burger. Faint smoky seasoning, slightly chewy texture. Scored 3.8. The best thing about ShelfMeat is that someone tried. The seasoning is basic but it means this product was designed to be eaten, not just consumed.

**GrindHouse Slab** (Φ1.20/100g) — The highest-rated budget option. Textured to resemble pulled pork, seasoned with a proprietary spice blend. Scored 5.2. Our panelists were genuinely surprised. The texture work is impressive for the price point — fibers that separate, a surface that caramelizes under heat. GrindHouse is proof that good vat protein is a manufacturing choice, not a cost inevitability.

## The Mid Tier: Φ3.00 - Φ12.00

**Kenji Farms Heritage Cut** (Φ4.50/100g) — Marketed as "vat-raised in the Japanese tradition." The marbling is convincing, the fat layers render properly, and the umami depth suggests real dashi influence in the culture medium. Scored 6.8. This is the point where vat protein stops being a substitute and starts being a product.

**Vossen BioSteak Premium** (Φ8.00/100g) — Vossen's entry in the premium vat market. Technically excellent — perfect myoglobin distribution, genuine Maillard reaction capability, nutritionally optimized. Scored 6.1. It's very good and completely soulless. You eat it and feel nothing. The protein is flawless and the experience is empty. Our panelists called it "the uncanny valley of meat."

**SavannaPro Wild Type** (Φ11.00/100g) — Claims to replicate the flavor profile of wild game. The gamey notes are present but synthetic — a chemical approximation of what happens when an animal eats wild forage and converts it to muscle. Scored 5.9. Points for ambition, deductions for reminding you of what doesn't exist anymore.

## The Premium Tier: Φ25.00 - Φ200.00

**Aurelian Kobe Reserve** (Φ45.00/100g) — The first product in the tasting that made multiple panelists close their eyes. Grown in small-batch bioreactors using a proprietary culture medium that includes compounds derived from actual Wagyu cattle DNA. The fat melts at precisely the right temperature. The flavor builds. Scored 8.4.

**Epoch Prime Filet** (Φ120.00/100g) — Here is where it gets interesting. Epoch Prime scored 8.6 on taste — marginally above the Aurelian. But our lab analysis revealed the presence of three unlisted compounds: a mild nootropic, a serotonin precursor, and a proprietary bioactive that our chemists couldn't fully identify. Epoch Prime doesn't just taste good. It makes you feel good. It makes you feel like you deserve to eat this well. That is not food. That is neuropharmacology on a plate.

## Conclusions

The gap between Tier 1 dispensary protein and Tier 5 boutique protein is not primarily nutritional — all twelve products met baseline macro and micronutrient requirements. The gap is experiential, cultural, and in at least one case, pharmacological. You are not paying for better food. You are paying for a better feeling about food. Whether that distinction matters depends entirely on which tier you eat in.`
});

writeDoc({
  file_name: "the_last_farmer",
  id: uid(),
  name: "The Last Farmer",
  title: "The Last Farmer",
  type: "document",
  document_type: "profile",
  author: "Deshi Amara, The Underfeed Chronicle",
  date: "2198-11-03",
  classification: "public",
  category: "Food Systems",
  description: "Profile of Haru Edevane-Kowalski, who maintains one of the last soil-based gardens in GLMZ.",
  related_entities: ["meridian_88"],
  credibility: "verified",
  story_hooks: [
    "Several corponations have offered to buy Haru's soil — real living soil is worth more per kilogram than most augmentations",
    "Haru's tomatoes have become an underground currency among Tier 4 and 5 food enthusiasts, creating an economy she neither controls nor profits from"
  ],
  tags: ["document", "food", "farming", "soil", "profile", "agriculture", "artisan", "eccentric", "tier_3"],
  body: `# The Last Farmer

## The Rooftop

Haru Edevane-Kowalski grows tomatoes in dirt. Real dirt. Not hydroponic substrate, not nutrient gel, not aeroponic mist — dirt. Soil. The kind with worms in it. She maintains 40 square meters of living earth on the rooftop of a converted Tier 3 residential block in the Midline district, and she has been doing this for thirty-one years, which makes her either the most dedicated urban agriculturalist in GLMZ or the most stubborn person alive.

The soil itself is a legacy. Her grandmother brought a bucket of earth from a community garden in what was then the Chicago suburbs before the consolidation. That bucket became a planter, became a raised bed, became this rooftop plot that Haru has been building, composting, and nurturing since she was nineteen. The soil is alive with organisms that don't exist anywhere else in the city — bacteria, fungi, nematodes, insects that arrived as eggs decades ago and have been breeding in isolation ever since. Biologists from the Meridian Institute have begged to study it. Haru lets them look but not touch.

## The Work

Growing food in soil in GLMZ is an act of absurd devotion. Everything about the city's environment works against it. The atmospheric processors strip moisture from the air. The UV exposure at rooftop level is punishing without shade management. Temperature swings between the heat-island effect of the arcology below and the wind exposure above create conditions that no commercial crop variety is bred for. Haru grows heritage strains — tomato cultivars that haven't been commercially viable in a century, maintained through seed-saving networks that operate like underground libraries.

She wakes at 0430 every morning to check soil moisture. She hand-pollinates because there aren't enough insects at rooftop level, even with the small population of mason bees she maintains in a homemade hive. She composts everything — food waste, paper, hair clippings from the barber two floors down who saves them for her in exchange for basil. The composting system is a precisely managed ecosystem that she monitors with the same intensity most people reserve for their BCI feeds.

## The Harvest

Haru's annual yield is approximately 80 kilograms of produce — tomatoes primarily, plus basil, peppers, leafy greens, and a small plot of strawberries that produces maybe 3 kilograms per season. In a city that consumes 12,000 metric tons of food daily, this is statistically zero. It is also, by every qualitative measure, the best food in GLMZ.

A single Haru tomato — small, irregularly shaped, scarred from wind damage, imperfect in every way that vat-grown produce is not — tastes like an explosion. People who eat one for the first time often cry. Not metaphorically. Actually cry. Because the flavor is a signal from a world that doesn't exist anymore, a world where food grew in the ground and tasted like the place it came from and the sun that fed it.

## The Eccentric

The food establishment of GLMZ treats Haru with a mixture of reverence and confusion. She has been profiled in every food publication, offered positions at Tier 5 restaurants as a "living exhibit," and approached by Ringo Agritech's heritage division about licensing her seed stock. She has refused everything. She is not a brand. She is not a concept. She is a farmer, and farming is what she does, and the dirt under her fingernails is not an aesthetic choice but evidence of work.

Her neighbors think she's eccentric. She thinks they're the eccentric ones — living in a city that forgot what food is, eating things that were never alive, and calling it normal. She may have a point.`
});

writeDoc({
  file_name: "shelf_cooking_100_meals_from_nothing",
  id: uid(),
  name: "Shelf Cooking: 100 Meals from Nothing",
  title: "Shelf Cooking: 100 Meals from Nothing",
  type: "document",
  document_type: "guide",
  author: "Anonymous (attributed to the Old Harbor Collective Kitchen)",
  date: "2197-05-20",
  classification: "public",
  category: "Food Systems",
  description: "Survival cookbook excerpts documenting how Shelf residents transform dispensary rations into edible meals.",
  related_entities: ["meridian_88", "old_harbor"],
  credibility: "verified",
  story_hooks: [
    "The cookbook circulates in samizdat form because it includes techniques for extending ration portions beyond their intended servings, which technically violates dispensary terms of service",
    "Recipe #47 uses a chemical reaction between two common dispensary items to create a leavening agent, enabling bread — a discovery made by accident and shared through the Shelf like a secret"
  ],
  tags: ["document", "food", "cooking", "survival", "shelf", "tier_1", "recipes", "community", "guide"],
  body: `# Shelf Cooking: 100 Meals from Nothing

## Foreword

This is not a cookbook for people who have kitchens. This is a cookbook for people who have a heating element, a dispensary card, and the stubborn belief that eating should not feel like refueling. Every recipe in this collection uses ingredients available from standard Tier 1 dispensaries, supplemented by what you can find, trade for, or grow in a window box. No recipe requires more than two heat sources. No recipe costs more than Φ0.60 per serving. Every recipe feeds at least two, because on the Shelf, eating alone is a luxury no one can afford.

## Recipe #3: Protein Slab Congee

Take one NutriBloc Standard protein slab (Φ0.22). Crumble it — really crumble it, until it's powder. Add to 500ml water in your pot. Heat slowly. Stir constantly. The protein will resist dissolving; keep stirring. After 20 minutes you'll have a thick, grey porridge. This is your base. Now make it worth eating. Add salt (Φ0.02 per pinch from the block market). Add chili flakes if you have them. Add a crushed algae cracker for texture. Add anything green — sprouted mung beans from your window box, scavenged herbs from the market sweepings. The congee is nothing. What you add to the congee is everything.

## Recipe #17: Twice-Fried Dispensary Noodles

The dispensary noodle pack (Φ0.30) comes with a flavor sachet that tastes like regret. Throw away the sachet. Cook the noodles in minimal water until just soft. Drain completely. Now — and this is the important part — fry them. If you have oil (Φ0.40 for a 50ml tube at the block market), use a thin film. If you don't, dry-fry in a hot pan. Get them crispy. Really crispy. Browning is flavor. Maillard reaction doesn't care about your income bracket. Once crispy, toss with whatever sauce you've made — chili paste from Recipe #8, the garlic-algae oil from Recipe #12, or just salt and a squeeze of synthetic lime juice (Φ0.05/packet). Crispy noodles feel like a choice, not a sentence.

## Recipe #34: The Everything Soup

This is the recipe for the end of the month, when the dispensary card is empty and the next cycle hasn't loaded. Take whatever you have. Everything. The heel of a protein slab. Stale crackers. Vegetable trimmings you saved in a container of water (you are saving your trimmings in water, yes? Recipe #1 told you to do this). The dregs of any sauce. Boil it all together. Season it. This is stone soup without the moral — nobody is going to learn a lesson about community from your hunger. But the soup is warm and it fills you and tomorrow the card reloads and you start again.

## Recipe #47: Almost-Bread

This one changed everything. Take dispensary protein powder (any brand) and mix with dispensary starch powder at a 1:3 ratio. Add water to form a thick paste. Now: take one dispensary electrolyte tablet and one dispensary antacid tablet. Crush them together and add to the paste. The citric acid in the electrolyte reacts with the sodium bicarbonate in the antacid and produces carbon dioxide. The paste rises. Not much. Not like real bread. But enough. Shape it. Cook it on a dry pan, covered, low heat, 15 minutes per side. What comes out is flat, dense, and slightly sour. It is bread. It is the closest thing to bread that Φ0.35 in dispensary ingredients can produce. The first time this recipe circulated through Block 7, four families baked at the same time and the corridor smelled like a bakery and three people cried.

## A Note on Dignity

Every recipe in this book is an act of resistance. The dispensary system is designed to deliver nutrition, not food. Nutrition keeps your body running. Food keeps your humanity running. When you take a protein slab and turn it into congee, you are not being efficient — you are being human. When you fry noodles until they're crispy, you are insisting that texture matters, that pleasure matters, that the difference between eating and feeding matters. The Shelf doesn't have much. But it has fire and it has ingenuity and it has the fundamental refusal to eat like a machine.`
});

writeDoc({
  file_name: "why_tier_5_food_tastes_different",
  id: uid(),
  name: "Why Tier 5 Food Tastes Different (It's Not Just Quality)",
  title: "Why Tier 5 Food Tastes Different (It's Not Just Quality)",
  type: "document",
  document_type: "investigative",
  author: "Ndidi Volkov-Osei, Food Systems Correspondent",
  date: "2200-01-18",
  classification: "restricted",
  category: "Food Systems",
  description: "Investigation into the presence of neural-enhancement compounds in luxury-tier food products.",
  related_entities: ["meridian_88", "vossen", "lazarus_group"],
  credibility: "verified",
  story_hooks: [
    "If the findings are accurate, Tier 5 residents have been unknowingly consuming cognitive enhancers with every meal for years",
    "The compounds are technically legal because they're classified as 'flavor enhancers' under food regulation, not pharmaceuticals"
  ],
  tags: ["document", "food", "tier_5", "luxury", "neural_enhancement", "investigation", "pharmaceuticals", "class_divide"],
  body: `# Why Tier 5 Food Tastes Different (It's Not Just Quality)

## The Question

Everyone who has eaten across the tiers knows that Tier 5 food tastes better. The standard explanation is obvious: better ingredients, better preparation, better everything. Money buys quality. But a series of independent lab analyses conducted over the past eighteen months suggests something more specific and more troubling: Tier 5 food doesn't just taste better. It makes your brain work better while you eat it.

## The Compounds

Three classes of bioactive compounds were identified in Tier 5 food products that are absent from their Tier 1-3 equivalents: serotonin precursors that elevate mood during consumption, nootropic agents that temporarily enhance pattern recognition and verbal fluency, and a proprietary compound we're designating NE-7 that appears to strengthen associative memory formation. These are not seasoning. These are neural modifiers delivered through the digestive system and calibrated to peak effect during the 30-90 minute post-meal window — exactly the timeframe of a business dinner, a social engagement, or a creative session.

## The Implications

If you eat Tier 5 food regularly, you are not just better nourished — you are cognitively enhanced during your most important daily interactions. You are sharper in meetings. You are more articulate at dinners. You are more creative during the evening hours when the day's final meal is doing its quiet work on your neural chemistry. This is not a marginal advantage. Cognitive testing on subjects who consumed NE-7-laced food showed a 12-18% improvement in working memory tasks and a 9% improvement in social cognition metrics.

## The Legal Framework

Here is the elegant part: none of this is illegal. The compounds are classified under GLMZ food regulation as "bioactive flavor enhancers" — a category that exists in a regulatory gap between food additives and pharmaceuticals. Flavor enhancers are subject to food safety standards (non-toxic, non-allergenic) but not pharmaceutical oversight (no efficacy testing, no disclosure requirements, no prescription framework). The classification was established in 2186 by a regulatory committee whose members included representatives from three corponations that now manufacture Tier 5 food products.

## Who Knows

The food manufacturers know. The corponations whose executives eat Tier 5 food know — it's why executive dining is never outsourced and always in-house. The regulatory body knows but has no mandate to act. The general public does not know, because the compounds are undetectable by taste and because nobody in Tier 1 has access to the analytical chemistry equipment needed to identify them. This article, if published, will change that. We anticipate significant legal and extralegal efforts to prevent its distribution.`
});

writeDoc({
  file_name: "the_ringo_food_monopoly",
  id: uid(),
  name: "The Ringo Food Monopoly and What It Means for Your Dinner",
  title: "The Ringo Food Monopoly and What It Means for Your Dinner",
  type: "document",
  document_type: "investigative",
  author: "Kamila Nze-Park, Economic Transparency Initiative",
  date: "2199-07-02",
  classification: "public",
  category: "Food Systems",
  description: "Investigative piece on Ringo Agritech's monopolistic control over GLMZ's food supply.",
  related_entities: ["meridian_88", "ringo_agritech"],
  credibility: "verified",
  story_hooks: [
    "Ringo's contracts with the city include a clause that prevents GLMZ from developing internal agricultural capacity — the city literally cannot legally grow its own food",
    "A former Ringo logistics manager is willing to testify about deliberate supply throttling used to manipulate food prices"
  ],
  tags: ["document", "food", "ringo", "monopoly", "corponation", "supply_chain", "investigative", "economics"],
  body: `# The Ringo Food Monopoly and What It Means for Your Dinner

## The Numbers

Ringo Agritech supplies 94% of GLMZ's caloric base. The remaining 6% comes from local sources — urban algae farms, small-scale vat operations, and the handful of eccentric gardeners who grow actual food in actual soil. That 94% figure has been stable for twelve years, not because the market reached equilibrium, but because Ringo's supply contracts with the GLMZ Municipal Authority include exclusivity provisions that prevent the city from licensing competing agricultural suppliers.

Read that again: the city is contractually prohibited from diversifying its food supply. This is not a market. This is a dependency.

## How We Got Here

In the 2160s, GLMZ faced a genuine food crisis. The old agricultural supply chains had collapsed during the consolidation era, and the city's population was growing faster than local production could sustain. Ringo Agritech — then a mid-sized automated farming corporation — offered a solution: guaranteed caloric supply at fixed prices, backed by massive infrastructure investment in the agricultural zones. The city accepted. The contract was for 30 years with automatic renewal provisions and penalty clauses for early termination that would bankrupt the municipal treasury.

The contract made sense in 2165. By 2180, it was a cage. By 2199, it is the single most important document governing life in GLMZ, and almost no one has read it.

## What Monopoly Means at the Table

Price: Ringo sets the wholesale price for caloric base, and every food product in the city includes that cost. When Ringo increases base prices by 3% — as they did in Q2 2199 — the cost cascades through every tier. Tier 5 barely notices. Tier 1 eats less.

Quality: Ringo decides what grows. Their crop optimization algorithms prioritize yield and shelf stability over flavor, nutrition density, and variety. If Ringo's algorithm determines that a particular grain strain is 2% more efficient, every product derived from that grain changes. Consumers have no input and no alternative.

Security: One supply chain means one point of failure. The 2194 distribution strike proved this when a four-day work stoppage at Ringo's processing facilities reduced Shelf food availability by 60%. Four days. Sixty percent. People traded augmentations for protein bars.

## The Alternative That Isn't

Advocates for food sovereignty point to vertical farming, expanded algae cultivation, and synthetic biology as alternatives to Ringo dependence. These are technically viable. They are legally impossible under the current contract. GLMZ cannot build agricultural capacity that would compete with Ringo's supply without triggering contract penalties estimated at Φ4.2 billion — roughly eight years of municipal revenue.

The contract renews automatically in 2205. The termination window opens in 2203. That is four years away. If there is a political moment to challenge Ringo's monopoly, it is approaching. Whether anyone with power cares enough to seize it remains an open question.`
});

writeDoc({
  file_name: "street_food_of_old_harbor",
  id: uid(),
  name: "Street Food of Old Harbor",
  title: "Street Food of Old Harbor",
  type: "document",
  document_type: "cultural",
  author: "Tomoko Asante-Brennan, Cultural Correspondent",
  date: "2199-04-30",
  classification: "public",
  category: "Food Systems",
  description: "A food culture piece cataloging the best street food vendors in Old Harbor district.",
  related_entities: ["meridian_88", "old_harbor"],
  credibility: "verified",
  story_hooks: [
    "The night market vendors operate on a handshake territory system that has prevented conflict for decades — until a new vendor from outside the district ignores it",
    "One vendor's secret ingredient turns out to be a geneware-modified herb that technically requires a pharmaceutical license to distribute"
  ],
  tags: ["document", "food", "street_food", "old_harbor", "culture", "vendors", "tier_1", "tier_2", "community"],
  body: `# Street Food of Old Harbor

## The Night Market

Old Harbor's night market begins at 2000 when the day-shift workers come home and the night-shift workers haven't left yet, and for three hours the waterfront promenade becomes the best restaurant in GLMZ. Not the most expensive. Not the most refined. The best. Because food is best when it's made in front of you by someone who knows your name, served on a recycled tray under string lights that reflect off the harbor water, and eaten standing up with people who've been eating here longer than you've been alive.

The market runs 200 meters along the promenade. Twenty-seven vendors, each with a designated spot that hasn't changed in years. The territory system is informal but absolute — you set up where you've always set up, and if you're new, you wait until someone retires or dies and then you negotiate with the market's unofficial coordinator, a woman named Blessed who has held the role for nineteen years and whose word is final.

## The Vendors

**Auntie Yuki's Algae Wraps** — The anchor of the market. Yuki Okonkwo-Tanaka has been wrapping things in seasoned algae sheets for twenty-two years. The filling changes based on what's available — protein crumble, pickled vegetables, sometimes actual fish from the harbor aquaculture pens. The constant is the algae itself, which Yuki seasons with a proprietary blend that she grinds fresh every afternoon. The wrap costs Φ0.80. It tastes like being taken care of.

**Brother Jun's Noodle Station** — Jun pulls noodles by hand. Actual hand-pulled noodles, stretched and folded in a technique he learned from his grandmother who learned it from hers. The noodles go into a broth made from vat bone stock and whatever aromatics Jun found at the morning market. There is always a line. The line is part of the experience. You wait, you watch the noodles being pulled, you smell the broth, and by the time you sit down with your bowl you've already been fed emotionally.

**Kofi's Grill** — Kofi runs the only open-flame grill in Old Harbor, maintained under a grandfathered fire permit that predates current safety regulations. He grills protein slabs marinated in a chili-ginger paste and serves them on flatbread with pickled radish. The char from the open flame adds a flavor dimension that no electric grill can replicate. Kofi's line is shorter than Jun's because the portions are huge and people eat slowly, savoring the anachronism of food cooked over fire.

**The Bao Collective** — Five women from Block 12 who make steamed buns in a converted laundry unit. The buns are filled with seasoned protein paste and a single piece of reconstituted vegetable. They make 300 per night and sell out by 2130. The bun costs Φ0.50. There are no leftovers. There are never leftovers.

## Why It Matters

Old Harbor's night market is not a tourist attraction. There are no tourists. It is not a cultural preservation project. No one is preserving anything — they're surviving and making survival taste good. The market exists because the alternative is eating dispensary rations alone in a hab unit, and the people of Old Harbor decided long ago that they would rather eat together, standing up, under string lights, with food that someone cared about making. This is the most important meal in GLMZ, and it costs less than Φ1.`
});

writeDoc({
  file_name: "synthetic_milk_real_consequences",
  id: uid(),
  name: "Synthetic Milk, Real Consequences",
  title: "Synthetic Milk, Real Consequences",
  type: "document",
  document_type: "health_report",
  author: "Dr. Priya Johansen-Ngozi, Meridian Public Health Coalition",
  date: "2198-08-14",
  classification: "public",
  category: "Food Systems",
  description: "Health report on long-term effects of synthetic dairy consumption in GLMZ.",
  related_entities: ["meridian_88", "vossen"],
  credibility: "verified",
  story_hooks: [
    "The synthetic compounds in cheap milk substitutes interact unpredictably with certain BCI anti-rejection medications",
    "A class-action suit against the largest synth-dairy manufacturer was quietly settled with NDAs that prevent plaintiffs from discussing their health outcomes"
  ],
  tags: ["document", "food", "dairy", "synthetic", "health", "medical", "tier_1", "tier_2", "bci"],
  body: `# Synthetic Milk, Real Consequences

## Background

GLMZ consumes approximately 2.4 million liters of dairy-equivalent products per day. Less than 0.1% of this comes from actual mammals. The rest is synthetic — manufactured from a combination of vat-grown casein proteins, engineered lipid compounds, and flavor matrices designed to approximate the taste and nutritional profile of historical dairy. This report examines the long-term health outcomes of populations who consume synthetic dairy as their primary calcium and fat source.

## Study Population

We tracked 12,000 Tier 1 and Tier 2 residents over a seven-year period (2191-2198), comparing those whose dairy intake was primarily synthetic (Group A, n=8,400) with those who had access to mixed synthetic/real dairy through workplace or community programs (Group B, n=3,600). Both groups were matched for age, augmentation status, and baseline health metrics.

## Key Findings

Group A showed a 23% higher incidence of bone density loss compared to Group B, despite equivalent calcium intake on paper. The synthetic casein proteins, while nutritionally labeled as equivalent to natural casein, appear to have lower bioavailability — the body absorbs less of the calcium they carry. Over seven years, this difference accumulates.

More concerning: Group A participants who were also taking standard BCI anti-rejection medication (approximately 67% of the sample) showed a 31% higher rate of gastrointestinal inflammation. The mechanism is unclear, but our preliminary analysis suggests that a compound used as an emulsifier in budget synthetic dairy — designated E-4412 — interferes with the gut bacteria that metabolize the anti-rejection drugs. The drugs remain effective, but the metabolic byproducts cause chronic low-grade inflammation that, over years, damages the intestinal lining.

## Who Is Affected

This is a tier problem. Tier 1 and 2 residents consume an average of 400ml of synthetic dairy daily — in coffee substitutes, protein blends, and dispensary meal packs. They have no alternative unless they can afford real dairy (minimum Φ8.00/liter) or dairy-free substitutes that don't contain E-4412 (available primarily in Tier 3+ grocery markets). The population most dependent on synthetic dairy is the population least able to choose something else.

## Recommendations

We recommend immediate reclassification of E-4412 from "generally recognized as safe" to "conditional use — requires disclosure." We recommend mandatory labeling of all synthetic dairy products that contain E-4412. We recommend that BCI clinics include dietary guidance on synthetic dairy interactions with anti-rejection protocols. We have been recommending these things for three years. Nothing has changed.`
});

writeDoc({
  file_name: "when_the_supply_chain_breaks",
  id: uid(),
  name: "When the Supply Chain Breaks",
  title: "When the Supply Chain Breaks",
  type: "document",
  document_type: "historical",
  author: "The GLMZ Historical Archive Project",
  date: "2199-01-15",
  classification: "public",
  category: "Food Systems",
  description: "Account of the 2194 Ringo distribution strike and its impact on GLMZ's food supply.",
  related_entities: ["meridian_88", "ringo_agritech"],
  credibility: "verified",
  story_hooks: [
    "During the strike, a Tier 5 resident was recorded offering Φ500 for a protein bar — the recording went viral and became a symbol of structural fragility",
    "The mutual aid networks that formed during the crisis still operate today and have become the foundation of Shelf community organizing"
  ],
  tags: ["document", "food", "supply_chain", "strike", "crisis", "ringo", "shelf", "history", "mutual_aid"],
  body: `# When the Supply Chain Breaks

## Day One: April 14, 2194

At 0600 on April 14, 2194, workers at Ringo Agritech's three primary processing facilities outside GLMZ walked off the job. The strike was over working conditions — specifically, the chronic respiratory illness rate among processing workers exposed to agricultural substrate dust without adequate filtration equipment. The workers had been filing complaints for two years. Ringo had responded with coupons for over-the-counter respiratory medication. The workers decided that was insufficient.

The first day, nobody noticed. GLMZ maintains a 72-hour food buffer in distributed storage facilities throughout the city. Dispensaries continued operating normally. Markets were stocked. Restaurants served dinner. The strike was a line item in the news feeds, buried below entertainment updates and weather.

## Day Two: April 15, 2194

The buffer system works on continuous replenishment — as food moves out of storage to dispensaries and markets, new supply flows in from processing. With processing stopped, the buffer began draining without refill. By midday on April 15, the Municipal Logistics Authority issued an advisory: residents should moderate purchases. The advisory was polite, technical, and ignored by anyone who read between the lines and went to buy everything they could carry.

Dispensary quotas were implemented at 1800 on April 15. Each Tier 1 and 2 resident was limited to one day's ration per dispensary visit, one visit per twelve hours. This was sensible. This also signaled to 2.1 million Shelf residents that the system was failing. Panic buying began immediately.

## Day Three: April 16, 2194

By morning, 40% of Shelf dispensaries reported depleted stock. The remaining dispensaries had lines that wrapped around blocks — three-hour waits for a single ration pack. The block market vendors, who buy dispensary stock to resell as prepared food, had nothing to sell. The night market didn't open for the first time in eleven years.

What did open were the mutual aid networks. Block by block, floor by floor, people who had food shared with people who didn't. Cooking collectives pooled remaining ingredients and made communal meals. Community fridges — some that had sat mostly empty for years — became the center of block social life. The sharing was immediate, organized, and unsurprising to anyone who understood the Shelf: this is how the Shelf has always worked. The crisis just made it visible.

## Day Four: April 17, 2194

Ringo Agritech settled with its workers at 1400 on April 17. The workers got filtration equipment, health monitoring, and a 6% wage increase. Processing resumed immediately. The first resupply convoys reached GLMZ by 2200. By midnight, dispensaries were restocking.

The strike lasted 83 hours. In that time: no one starved to death (confirmed). Fourteen people were hospitalized for dehydration and malnutrition-related complications (confirmed). An unknown number of augmentations were traded for food (estimated in the hundreds). One Tier 5 resident was recorded offering Φ500 for a standard protein bar (confirmed — the recording still circulates as a reminder that money means nothing when the trucks stop rolling).

## Aftermath

The 2194 strike changed nothing structurally. Ringo still holds its monopoly. The 72-hour buffer was increased to 96 hours — one extra day. The mutual aid networks that formed during the crisis formalized and persisted, becoming the backbone of Shelf community support infrastructure that exists today. The lesson was simple and has not been forgotten: the city is four days from catastrophe, always.`
});

writeDoc({
  file_name: "a_childs_first_real_apple",
  id: uid(),
  name: "A Child's First Real Apple",
  title: "A Child's First Real Apple",
  type: "document",
  document_type: "personal_essay",
  author: "Suki Oduya-Brennan",
  date: "2198-12-25",
  classification: "public",
  category: "Food Systems",
  description: "Personal essay about a Shelf child's first experience eating an actual piece of fruit.",
  related_entities: ["meridian_88"],
  credibility: "verified",
  story_hooks: [
    "The apple was a gift from a Tier 4 teacher who spent a week's discretionary budget on a single piece of fruit for a student",
    "The child kept the apple seeds and is currently growing a small tree in a recycled container, three years later"
  ],
  tags: ["document", "food", "personal_essay", "childhood", "shelf", "tier_1", "fruit", "class_divide", "emotional"],
  body: `# A Child's First Real Apple

## The Apple

My daughter Ama was seven when she ate her first apple. A real apple. Not apple-flavored nutrient gel. Not synthetic apple compound in a drink pouch. A red, slightly bruised, imperfect apple that her teacher, Ms. Nakamura, brought to class in a paper bag like it was a secret, which it was.

Ms. Nakamura teaches at Block 22 Community School in the Shelf. She is Tier 3 by employment — teachers get mid-tier classification as a recruitment incentive — but she lives in the Shelf by choice, which tells you everything you need to know about her priorities. She bought the apple at a Tier 4 market. It cost Φ14. Her weekly discretionary budget after rent, transit, and mandatory deductions is Φ28. She spent half of it on a piece of fruit for a classroom of children who had never seen one.

## The Lesson

Ms. Nakamura cut the apple into twelve pieces — one for each student in Ama's class. Twelve pieces of a single apple. Each piece was smaller than a thumb. She asked the children what they thought it was. Three guessed correctly. The rest had seen apples in educational feeds but hadn't connected the image to a physical object that could exist in their hands.

Ama described the experience to me that evening, and I am going to write down exactly what she said because I don't want to improve on it: "Mama, it was wet inside. It crunched and then there was juice. It tasted like... it tasted like what I thought outside would taste like. Like the color red but for your mouth."

Like the color red but for your mouth. My daughter, who has a BCI that can process visual data at frequencies I can't perceive, who can access the entire knowledge archive of GLMZ through a thought, who lives in the most technologically advanced human settlement in history — she had never tasted an apple.

## The Seeds

Ama brought home two seeds. She had held them in her cheek through the rest of the school day, unwilling to swallow them, unwilling to throw them away. She asked me if we could grow a tree. I said I didn't know how. She looked it up on the feed and found a guide. She planted the seeds in a recycled protein paste container with soil she scraped from the edges of a maintenance corridor where moisture collects and something green was already growing.

That was three years ago. The tree — if you can call it a tree — is 40 centimeters tall and has eight leaves. It lives on our window ledge, which gets approximately two hours of indirect light per day. It will never produce fruit. The conditions are wrong in every way. Ama knows this. She waters it every morning anyway. She talks to it. She has named it.

## What It Means

I am not writing this essay to make you feel sorry for my daughter. Ama is fed. She is housed. She has a BCI and an education and a community that watches out for her. By the standards of GLMZ's Tier 1, she is doing fine. I am writing this because there is a difference between being fed and knowing what food is. There is a difference between a nutrition label that says "apple flavor" and the experience of biting into something that crunched and was wet inside and tasted like the color red but for your mouth. Fourteen Quanta for a single apple. Half a teacher's weekly discretionary budget. That is the distance between my daughter and the world she reads about on the feed. It is measured in fruit.`
});

// ═══════════════════════════════════════════════════════════════════
// DOCUMENTS — CHILDREN AND GROWING UP (8)
// ═══════════════════════════════════════════════════════════════════

writeDoc({
  file_name: "your_childs_first_bci_a_parents_guide",
  id: uid(),
  name: "Your Child's First BCI: A Parent's Guide",
  title: "Your Child's First BCI: A Parent's Guide",
  type: "document",
  document_type: "guide",
  author: "GLMZ Pediatric Neurology Association",
  date: "2199-02-10",
  classification: "public",
  category: "Children",
  description: "Medical and parenting guide for BCI installation in children, typically performed at age 6.",
  related_entities: ["meridian_88", "vossen"],
  credibility: "verified",
  story_hooks: [
    "A growing movement of parents is delaying BCI installation to age 10 or later, citing developmental concerns that the medical establishment dismisses",
    "The guide doesn't mention that BCI-less children are effectively locked out of the education system by age 8"
  ],
  tags: ["document", "bci", "children", "parenting", "medical", "guide", "education", "augmentation"],
  body: `# Your Child's First BCI: A Parent's Guide

## When Is the Right Time?

Standard BCI installation in GLMZ occurs at age six, coinciding with the transition from pre-primary to primary education. This timing is not arbitrary — neural plasticity at age six allows optimal integration of the BCI's sensory interface with the developing brain, and the primary education curriculum from Year One assumes BCI access. Your child can receive a BCI earlier (minimum age four, with specialist approval) or later, but delayed installation means your child will be learning the interface while their peers are already using it, creating an adaptation gap that compounds with each month of delay.

The procedure itself takes 90 minutes. It is performed under light sedation at any licensed pediatric neurology clinic. The BCI unit — approximately the size of a grain of rice — is implanted at the base of the skull where it interfaces with the brainstem and establishes connections with the cerebral cortex over the following 2-4 weeks. Your child will spend one night in the clinic for monitoring and go home the next morning.

## The First Week

Your child will experience what neurologists call "integration noise" — sensory artifacts as the BCI establishes its neural pathways. Common experiences include: seeing faint geometric patterns at the edges of vision, hearing a low-frequency hum that fades over 3-5 days, experiencing phantom touches on the skin, and mild emotional fluctuations as the BCI's limbic interface calibrates. These are normal. They are not painful. Your child may find them fascinating or frightening depending on temperament. Be present. Answer questions honestly. "The computer in your head is learning how your brain works" is accurate enough for a six-year-old.

By day seven, most children have adapted to the basic BCI presence and can perform elementary functions: accessing the educational feed, sending simple text messages to family contacts, and controlling the BCI's notification system. More complex functions — memory tagging, sensory recording, neural search — develop over the following months as the interface matures.

## What Changes

Your child's relationship with information changes fundamentally. Before BCI, your child learned by being told things and remembering them. After BCI, your child learns by thinking a question and receiving answers. This is not better or worse — it is different, and the difference reshapes how your child's mind develops. Memory becomes external as well as internal. Attention becomes mediated. The boundary between thought and search dissolves.

You will notice your child pausing mid-sentence — they are checking something on the feed. You will notice your child knowing things they couldn't possibly know from experience — they are accessing the knowledge base. You will notice your child's eyes flickering rapidly during rest — they are processing their daily neural integration backlog. These behaviors are normal. They are also, for parents who received their own BCIs as adults, occasionally unsettling. Your child will never know what it is like to think without augmentation. You remember. That gap in experience is real and worth acknowledging.

## Parental Controls

Your child's BCI comes with a comprehensive parental control suite that governs content access, communication permissions, data recording, and neural-feed exposure. We strongly recommend reviewing these settings with your clinic's pediatric interface specialist rather than attempting to configure them alone. The default settings are permissive — they assume you want your child to have full educational access and social connectivity. Many parents find the defaults appropriate. Some do not. The choice is yours, but make it deliberately rather than by default.

## What We Don't Say Enough

The BCI will become part of your child's identity. Not a tool they use, but a part of who they are, as fundamental as language or vision. You cannot remove it without causing significant neurological disruption after the first year of integration. This is permanent. You are making a permanent decision about your child's neurology at age six, and the honest answer to "do I have a choice?" is: technically yes, practically no. A child without a BCI in GLMZ cannot access education, cannot participate in the social networks their peers inhabit, and cannot function in a city built on the assumption that everyone is connected. The choice is not between BCI and no BCI. The choice is between BCI at six and increasingly painful marginalization.`
});

writeDoc({
  file_name: "growing_up_shelf_childhood_in_tier_1",
  id: uid(),
  name: "Growing Up Shelf: Childhood in Tier 1",
  title: "Growing Up Shelf: Childhood in Tier 1",
  type: "document",
  document_type: "sociological",
  author: "Dr. Emeka Zhao-Williams, Meridian Institute of Social Research",
  date: "2199-06-18",
  classification: "public",
  category: "Children",
  description: "Sociological study of childhood development and experience in GLMZ's Tier 1.",
  related_entities: ["meridian_88"],
  credibility: "verified",
  story_hooks: [
    "Shelf children develop spatial reasoning and social negotiation skills measurably faster than higher-tier children, but the advantage disappears when measured against academic benchmarks designed for BCI-assisted learning",
    "The study's author grew up on the Shelf and returned to study the community that raised them"
  ],
  tags: ["document", "children", "shelf", "tier_1", "sociology", "childhood", "education", "community", "development"],
  body: `# Growing Up Shelf: Childhood in Tier 1

## Methodology

This study synthesizes seven years of longitudinal observation (2192-2199) of 340 children born in Tier 1 residential blocks across six Shelf districts. Participants were tracked from birth through age seven, with biannual developmental assessments and continuous environmental monitoring via anonymized BCI data (parental consent obtained). The study was funded by the Meridian Institute and conducted with community advisory board oversight.

## The Physical World

Shelf children grow up in dense, mechanized, vertical space. The average Tier 1 hab unit measures 18 square meters for a family of three to four. Corridors are shared play space — children learn to navigate crowded walkways, vertical ladders between levels, and improvised outdoor areas on structural platforms and maintenance catwalks. By age four, the average Shelf child can climb a vertical ladder unassisted, navigate a six-block route without adult supervision, and identify which maintenance corridors are safe to play in and which carry electrical or chemical hazards.

This physical competence is remarkable and unremarked. No one in the Shelf considers it unusual that a four-year-old can climb three stories of industrial infrastructure. It is simply what childhood looks like when your home is a machine.

## Social Architecture

Shelf children are raised by networks, not nuclear units. The "block family" structure — where all adults in a residential block share responsibility for all children — is not an ideological choice but a practical necessity. With most adults working variable shifts, no single caregiver is consistently available. Children flow between hab units, eating where food is available, sleeping where a bed is empty, and receiving care from whichever adult is present. By age three, most Shelf children can identify 20-30 adults by name who they consider family-adjacent.

This produces children who are extraordinary social navigators. Shelf children read adult emotional states with precision — they know which auntie is having a bad day, which uncle will share food, which neighbor's door to knock on at 0300 when something is wrong. Social intelligence is a survival skill, and Shelf children are brilliant at it.

## The Education Gap

At age six, Shelf children receive their BCIs and enter the education system. Here, the advantages of Shelf childhood collide with a curriculum designed for a different world. Educational content assumes private study space (Shelf children study in corridors). Assessment assumes uninterrupted BCI access (Shelf infrastructure produces frequent signal degradation). Progress metrics assume parental engagement with digital learning platforms (Shelf parents work shifts that make scheduled engagement impossible).

The result is predictable: by age eight, Shelf children score 30-40% below city median on standardized assessments despite showing no cognitive deficit. They are not less capable. They are less accommodated. The system measures what it values, and what it values is performance under conditions that Shelf children don't have.

## What the Numbers Miss

The numbers miss the Shelf child who can organize a block-wide scavenger hunt involving 30 children across eight floors with no adult intervention. They miss the seven-year-old who translates for her grandmother across three languages and two cultural frameworks simultaneously. They miss the children who maintain community gardens, repair equipment, mediate disputes, and navigate a built environment that would challenge most adults. These competencies don't appear on assessments because no one designed an assessment for them. The Shelf raises extraordinary children. The city measures ordinary metrics. The gap between these two facts is where a generation falls through.`
});

writeDoc({
  file_name: "the_education_gap",
  id: uid(),
  name: "The Education Gap",
  title: "The Education Gap",
  type: "document",
  document_type: "investigative",
  author: "Coalition for Educational Equity, GLMZ",
  date: "2200-02-01",
  classification: "public",
  category: "Children",
  description: "Analysis of how educational access and outcomes differ between Tier 1 and Tier 5 children.",
  related_entities: ["meridian_88", "sterling_nakamura"],
  credibility: "verified",
  story_hooks: [
    "Tier 5 children receive personalized AI tutoring from age three, giving them a three-year head start on BCI-assisted learning before Tier 1 children even receive their BCIs",
    "A Tier 1 student who scored in the top 1% of aptitude was denied a scholarship because the scoring algorithm weighted 'learning environment stability' as a factor"
  ],
  tags: ["document", "children", "education", "inequality", "tier_1", "tier_5", "bci", "class_divide", "investigative"],
  body: `# The Education Gap

## Two Children

Consider two children born on the same day in GLMZ. Child A is born in Tier 1, Block 47, the Shelf. Child B is born in Tier 5, the Arden Spire arcology. Both are healthy. Both are, by every neurological measure, equally capable. By age twelve, Child B will be performing at a level that Child A will never reach. Not because of talent. Because of architecture.

## The First Six Years

Child B receives a developmental BCI at age three — a limited-function implant that provides educational content, cognitive scaffolding, and neural development monitoring. This is legal, expensive (Φ8,000 for the device and installation), and standard practice in Tier 5. By the time Child B receives their full BCI at age six, they have three years of augmented learning experience. Their neural pathways have been shaped by BCI-mediated education since before conscious memory.

Child A receives no augmentation until age six. Their learning is organic, social, and environmental — playing in corridors, absorbing language from the block family, developing physical and social skills that the Shelf demands. This learning is genuine and valuable. It is also invisible to every measurement system the city uses.

## The Curriculum

GLMZ's unified curriculum assumes BCI access, private study space, and stable connectivity. It delivers content through neural-feed educational packages that adapt to individual learning speed. In theory, this means every child learns at their own pace. In practice, the adaptation algorithms are trained on data from Tier 3-5 students and optimize for learning conditions that exist in those tiers. When a Shelf child falls behind, the algorithm interprets the delay as a learning deficit rather than an infrastructure deficit and adjusts difficulty downward. The child receives easier material. They learn less. The gap widens.

Tier 5 children receive supplementary education from private AI tutoring systems that cost Φ200-500/month. These systems integrate with the child's BCI to provide real-time learning support — contextual explanations, memory reinforcement, and cognitive load management. A Tier 5 child doing homework has an AI whispering answers in their ear. A Tier 1 child doing homework has a crowded corridor and intermittent connectivity.

## The Measurement

Standardized assessments at ages 8, 12, and 16 determine educational pathway and, by extension, career access. The assessments are BCI-administered, timed, and scored by algorithm. They test information retrieval (advantage: Tier 5, better BCI connectivity), analytical reasoning under time pressure (advantage: Tier 5, cognitive support tools), and creative problem-solving within structured frameworks (advantage: Tier 5, extensive exposure to structured creative environments).

The assessments do not test: social navigation, physical problem-solving, resource improvisation, multilingual code-switching, community organization, or crisis management. If they did, Shelf children would outperform every other tier. But they don't, because the assessments were designed by people who went to Tier 4 and 5 schools, and people design assessments that measure what they're good at.

## The Outcome

By age sixteen, the educational pathway has sorted children into tracks that correlate almost perfectly with birth tier. Tier 5 children enter advanced programs that lead to corponation management, research, and governance roles. Tier 1 children enter vocational programs that lead to maintenance, service, and manual labor. Exceptions exist — the system points to them constantly as proof of meritocracy. The exceptions prove nothing except that extraordinary talent can sometimes overcome systematic disadvantage. Ordinary talent cannot. And most talent is ordinary. That is what ordinary means.`
});

writeDoc({
  file_name: "lullabies_for_the_connected",
  id: uid(),
  name: "Lullabies for the Connected",
  title: "Lullabies for the Connected",
  type: "document",
  document_type: "essay",
  author: "Reka Achebe-Frost",
  date: "2199-08-05",
  classification: "public",
  category: "Children",
  description: "Essay about the songs parents sing to children who already have BCIs.",
  related_entities: ["meridian_88"],
  credibility: "verified",
  story_hooks: [
    "One lullaby, 'Quiet the Feed,' has become so widespread that children sing it to each other as a calming ritual",
    "Neuroscientists discovered that certain sung frequencies interact with BCI integration patterns, making lullabies literally therapeutic for augmented children"
  ],
  tags: ["document", "children", "bci", "music", "culture", "parenting", "lullabies", "emotional", "essay"],
  body: `# Lullabies for the Connected

## The Problem of Silence

Here is something nobody prepared parents for: when your six-year-old receives their BCI, they stop being able to experience silence. The feed is always there — a low hum of data, notifications, ambient information. Adults learn to tune it out. Children haven't learned yet. And so at bedtime, when the lights go down and the hab unit quiets and a child should be drifting toward sleep, the feed is there, whispering. Not loudly. Not urgently. Just constantly, like a river that never stops.

So parents sing. They have always sung to children at bedtime, but now they sing for a different reason. They sing to give the child something louder than the feed to hold onto. Something analog. Something that comes from a throat and a chest and a face leaning close in the dark, not from a signal processed through silicon.

## The Songs

The old lullabies still work — they cross every culture in the Diaspora, melodies carried from Lagos and Osaka and Bogota and Hyderabad, translated and recombined and passed down through families who brought nothing to GLMZ except their languages and their songs. A grandmother from Manila sings "Sa Ugoy ng Duyan" to a grandchild who has never seen a hammock. A father from Nairobi hums "Wimbo wa Usingizi" to a child whose sleep is monitored by neural telemetry. The songs don't need context to work. They need a voice.

But new songs have emerged — lullabies written for children of the BCI age. "Quiet the Feed" is the most widespread, a simple melody with words that vary by block and family but always contain the same core instruction: close your inner eyes, let the numbers go, I am here and the feed is not. It is a meditation technique disguised as a lullaby, teaching children to modulate their BCI's attention priority through rhythmic breathing tied to a melody. Neuroscientists at the Meridian Institute confirmed what parents already knew: sung frequencies in the 200-400Hz range interact constructively with the BCI's default integration rhythm, dampening feed awareness during the relaxation phase. The parents didn't know the science. They just knew it worked.

## What the Songs Mean

There is "Chrome Bones, Soft Heart," sung in the Shelf blocks, a song about a child who is part machine and part miracle and all loved. There is "The Weight of the World Is Not Yours Yet," which is exactly what it sounds like — a plea to let childhood last a little longer before the city's demands arrive. There is "Counting Down to Morning," which Tier 1 parents sing because morning means the night is over and the night in a Shelf hab unit is long and close and sometimes frightening for a small person.

The songs are not archived. They are not on the feed. They exist in the space between a parent's mouth and a child's ear, in the vibration of a chest against a small back, in the specific and unrepeatable frequency of a particular human voice singing a particular melody to a particular child in a particular moment. The BCI cannot record what happens in this space — not because of technical limitation, but because parents disable recording at bedtime. This is the one moment of the day that belongs to no system. The corponations cannot monetize it. The feed cannot optimize it. It is a voice in the dark, and it is enough.

## Why It Matters

We live in a city where every experience is mediated, recorded, analyzed, and sold. Children born here will never know an unaugmented thought. Their memories will be tagged, searchable, and potentially inheritable. Their emotions will be tracked, their development quantified, their potential assessed by algorithm. But at night, someone sings to them. The singing is imperfect, unoptimized, and human. It is the last analog experience in a digital childhood, and parents guard it with a ferocity that surprises even themselves.`
});

writeDoc({
  file_name: "children_of_the_diaspora",
  id: uid(),
  name: "Children of the Diaspora",
  title: "Children of the Diaspora",
  type: "document",
  document_type: "sociological",
  author: "Dr. Luz Bautista-Yamamoto, Cultural Studies, Meridian Institute",
  date: "2199-10-12",
  classification: "public",
  category: "Children",
  description: "Study of how children navigate having heritage from four or more distinct cultural traditions.",
  related_entities: ["meridian_88"],
  credibility: "verified",
  story_hooks: [
    "Children are creating entirely new cultural practices that synthesize elements from multiple heritage traditions in ways their parents don't recognize",
    "A child's BCI-accessible heritage archive has become a new form of identity expression — 'showing your roots' through curated cultural data"
  ],
  tags: ["document", "children", "diaspora", "culture", "identity", "heritage", "mixed", "sociology"],
  body: `# Children of the Diaspora

## The New Normal

The average child born in GLMZ in 2199 has heritage from 3.7 distinct cultural traditions. This is not a statistical curiosity — it is the defining feature of identity formation in the city. The Ubiquitous Diaspora, the great mixing that followed the consolidation era, produced a generation of parents who were themselves products of multicultural families. Their children are the second or third generation of this mixing, and for them, the concept of a single cultural identity is as foreign as soil-grown food.

## How Children Navigate

We interviewed 200 children ages 8-14 across all tiers about their understanding of their own cultural heritage. The responses fell into three broad patterns.

**The Archivist** (38% of respondents): These children actively curate their heritage using BCI-accessible cultural databases. They research their family's constituent traditions, collect cultural artifacts (digital and physical), and construct deliberate identity narratives. "I'm one-quarter Hausa, one-quarter Japanese, one-quarter Colombian, and one-quarter Irish-Korean," said one ten-year-old, "and I know something about all of them." For Archivists, heritage is a project — something you study, organize, and display.

**The Synthesist** (45% of respondents): These children don't maintain separate cultural threads — they combine them into something new. A Synthesist child might celebrate a holiday that merges elements from three different traditions, speak a personal patois that blends four linguistic influences, or practice customs that no single heritage tradition would recognize but that feel authentic to the child. "I don't do Diwali or Lunar New Year or Kwanzaa," one twelve-year-old explained. "I do my thing. It has lights from one and food from another and music from another and it's mine."

**The Pragmatist** (17% of respondents): These children don't think about heritage much. They identify as "from the Shelf" or "from GLMZ" and consider cultural heritage a background detail rather than an active identity component. Pragmatists are more common in Tier 1 and 2, where daily survival demands outweigh identity exploration.

## The Emergence of New Culture

The most significant finding: children are not preserving heritage traditions — they are using them as raw material for new ones. Block-level celebrations combine elements from a dozen traditions into events that belong to no historical culture but feel deeply authentic to the community that created them. Children's games incorporate rules and structures from multiple cultural play traditions. Slang absorbs vocabulary from every language in the Diaspora.

This is not cultural erasure. The source traditions are accessible via BCI archive and are studied in educational contexts. But the living culture — the culture that children practice daily — is synthetic in the most literal sense: composed from multiple sources into something new. GLMZ is not a multicultural city. It is a transcultural one, and the children understand this better than the adults.`
});

writeDoc({
  file_name: "the_shelf_playground",
  id: uid(),
  name: "The Shelf Playground",
  title: "The Shelf Playground",
  type: "document",
  document_type: "photo_essay",
  author: "Keoni Vasquez-Nkemelu, Documentary Photographer",
  date: "2199-05-01",
  classification: "public",
  category: "Children",
  description: "Described photo essay about improvised play spaces in the Shelf's residential blocks.",
  related_entities: ["meridian_88"],
  credibility: "verified",
  story_hooks: [
    "Several of the improvised playgrounds violate safety codes that have never been enforced because enforcement would mean admitting the city provides no play infrastructure in Tier 1",
    "A collective of parents and children has been systematically converting abandoned maintenance corridors into play spaces using salvaged materials"
  ],
  tags: ["document", "children", "shelf", "tier_1", "play", "photo_essay", "community", "infrastructure"],
  body: `# The Shelf Playground

## Image 1: The Pipe Swing, Block 14

A drainage pipe, 30 centimeters in diameter, runs horizontally through an open space between two residential blocks at the third-floor level. Someone — nobody remembers who, nobody claims credit — bolted two lengths of salvaged cable to the pipe and attached a seat cut from industrial conveyor belting. It is a swing. It arcs out over a six-meter drop. There is no safety net. There is no safety anything. Twelve children are waiting in line to use it. The child currently swinging — eight years old, missing a front tooth, grinning — has her legs fully extended at the apex of the arc, suspended over nothing, fearless.

This image hangs in the Meridian Institute's urban sociology department. The researchers study it for data about improvised play infrastructure. The children in it were just playing.

## Image 2: The Corridor League, Block 22

A residential corridor, 50 meters long, 3 meters wide, has been commandeered for a ball game. The ball is a bundle of compressed recycled fabric bound with adhesive tape. The goals are marked on the walls in paint that's been there so long it's become part of the building's identity. The game has rules — codified over years, specific to this corridor, accounting for the ventilation duct at the 30-meter mark that creates a dead zone and the uneven floor section at the 42-meter mark that bounces unpredictably. Twenty children are playing. Thirty adults are watching from doorways, offering commentary, criticism, and encouragement in equal measure. The corridor belongs to the children between 1600 and 1900. This is not policy. This is tradition.

## Image 3: The Garden Level, Block 8

A structural platform between floors — originally a maintenance staging area — has been converted to a children's space over a decade of accumulated modifications. There is a reading corner made of stacked shipping pallets and scavenged cushions. A climbing structure built from welded pipe offcuts, tested by an engineer who lives on the fourth floor and who volunteers their expertise because their own children play here. A mural covering the back wall, painted by every child who has grown up in Block 8 over the past nine years, a layered, chaotic, beautiful record of childhoods accumulated.

The platform was never designed for this. It was designed for staging maintenance equipment. But children needed a place and the equipment didn't, and so the space evolved, the way all Shelf spaces evolve — by need and ingenuity and the accumulated decisions of people who don't have the luxury of waiting for someone to build them something proper.

## Image 4: The Rooftop Astronomy Club, Block 31

Seven children lying on their backs on a rooftop platform, looking up. The sky above GLMZ is not dark enough for stars — light pollution and atmospheric processing create a permanent amber haze. But one child has a salvaged telescope with a digital filter that compensates for atmospheric interference, and another child's BCI is running an astronomy overlay that maps constellations onto the haze. They are teaching each other the names of stars they can barely see, in a city that has forgotten to look up.

## What the Images Say

The Shelf has no playgrounds. It has no parks, no recreation centers, no sports facilities, no dedicated children's spaces. The city's recreation budget for Tier 1 is Φ0.12 per child per year — enough for nothing. So the Shelf makes its own. Every play space in these images was built by parents, maintained by communities, and used by children who have never seen a purpose-built playground. They don't know what they're missing. They have built something better — spaces that belong to them, that they made, that carry the marks of every child who came before.`
});

writeDoc({
  file_name: "augmentation_age_when_should_your_child_get_chrome",
  id: uid(),
  name: "Augmentation Age: When Should Your Child Get Chrome?",
  title: "Augmentation Age: When Should Your Child Get Chrome?",
  type: "document",
  document_type: "debate",
  author: "The GLMZ Parenting Forum, moderated panel discussion transcript",
  date: "2199-11-20",
  classification: "public",
  category: "Children",
  description: "Ethical debate about the appropriate age for children to receive cyberware beyond the standard BCI.",
  related_entities: ["meridian_88", "vossen", "kyosei_dynamics"],
  credibility: "verified",
  story_hooks: [
    "A Tier 5 child received full limb augmentation at age nine for 'competitive advantage' in scholastic athletics, sparking public outrage",
    "Underground chrome clinics in the Shelf install augmentations in children as young as twelve, often because the children need the capability for work"
  ],
  tags: ["document", "children", "augmentation", "cyberware", "chrome", "ethics", "debate", "parenting", "bci"],
  body: `# Augmentation Age: When Should Your Child Get Chrome?

## The Panel

This transcript represents a moderated discussion held at the GLMZ Community Forum on November 20, 2199. Panelists: Dr. Juno Eze-Park (pediatric neurosurgeon), Tarik Osman-Kowalski (parent advocate, Tier 1), Vera Lindt (Kyosei Dynamics youth augmentation division), and Noor Al-Rashidi-Chen (child development ethicist).

## Opening Statements

DR. EZE-PARK: The medical position is clear: non-essential augmentation before age sixteen carries developmental risks. The skeletal system is still growing. The nervous system is still integrating the BCI. Adding additional cyberware creates competing demands on neural bandwidth and physical development. We see stress fractures at augment-bone interfaces in children because the bone hasn't finished growing. We see neural fatigue in children whose BCIs are competing with augment control systems for processing priority. The medicine says wait.

LINDT: The market position is also clear: 23% of children ages 12-16 in GLMZ have at least one non-BCI augmentation. That number was 11% five years ago. Parents are making this decision regardless of medical recommendation. Our role as manufacturers is to ensure that the augmentations children receive are designed for developing bodies — adjustable, upgradeable, and biocompatible with growth. Kyosei's youth line exists because pretending children aren't getting augmented doesn't make them safer.

OSMAN-KOWALSKI: I'd like to introduce a word nobody on this panel has used yet: work. On the Shelf, children start contributing to household income at twelve or thirteen. Gig work. Delivery. Salvage. Maintenance assistance. Some of these jobs are easier with augmentation. A grip-strength enhancer. A visual overlay. A respiratory filter for kids working in chemical-adjacent environments. These parents aren't augmenting their children for competitive advantage. They're augmenting them so they can earn.

AL-RASHIDI-CHEN: And that is precisely the ethical catastrophe. We are in a city where Tier 5 children receive augmentation for advantage and Tier 1 children receive augmentation for survival, and we are debating the appropriate age as though the age is the issue. The issue is that children are being augmented at all — not because augmentation is inherently wrong, but because the reasons driving it reflect a system that demands more from children than childhood should require.

## The Debate

The discussion continued for ninety minutes. Key points of contention: whether augmentation should be regulated by age (Dr. Eze-Park: yes, minimum sixteen), by medical assessment (Lindt: case-by-case evaluation), by economic context (Osman-Kowalski: different rules for different circumstances), or whether the regulatory framework itself is the wrong approach (Al-Rashidi-Chen: address the conditions that create demand). No consensus was reached. No consensus was expected.

## Audience Questions

The most notable audience contribution came from a fifteen-year-old Shelf resident named Dessa who had received grip augmentation at thirteen to work in a salvage yard: "You're all talking about when is the right age for me to get chrome. Nobody's asking whether it's the right age for me to be working in a salvage yard. I didn't want chrome. I wanted to eat. Chrome was how I got to eat. If you want to protect kids from augmentation, protect them from needing it."

The panel had no adequate response. The session ended shortly after.`
});

writeDoc({
  file_name: "what_the_kids_are_saying",
  id: uid(),
  name: "What the Kids Are Saying",
  title: "What the Kids Are Saying",
  type: "document",
  document_type: "cultural",
  author: "GLMZ Linguistic Observatory",
  date: "2200-01-05",
  classification: "public",
  category: "Children",
  description: "Report on youth slang and the generational language divide in GLMZ.",
  related_entities: ["meridian_88"],
  credibility: "verified",
  story_hooks: [
    "Some youth slang terms are deliberately designed to be undetectable by BCI content monitoring algorithms, creating a steganographic layer in everyday speech",
    "The speed of slang evolution has increased so dramatically that terms have a half-life of about six weeks before being considered dated"
  ],
  tags: ["document", "children", "youth", "slang", "language", "culture", "bci", "generational", "vocabulary"],
  body: `# What the Kids Are Saying

## The Velocity of Language

Youth slang in GLMZ evolves faster than any previous generation's argot, and the reason is structural: BCI-connected children process and share linguistic innovation at network speed. A new term coined by a twelve-year-old in the Shelf at 0800 can be in active use across all five tiers by 1400. It can be dated by next week. It can be incomprehensible to adults by the time they first encounter it. The half-life of a slang term in 2200 is approximately six weeks, down from six months a generation ago. Language is moving faster than adults can follow, and the kids know it.

## The Glossary (Current as of January 2200, Probably Obsolete by February)

**Ghosting** — Not the old meaning (ignoring someone). New meaning: deliberately reducing your BCI's data footprint to near-zero. "I'm ghosting tonight" means "I'm going off-grid." Used by teenagers who want privacy from parental monitoring, corporate data harvesting, or both.

**Rendering** — Performing. Specifically, performing a version of yourself that's optimized for a particular social context. "She's rendering hard for the interview" means she's presenting a carefully constructed persona. All teenagers do this. BCI-era teenagers do it with awareness that their performance is being recorded, analyzed, and scored.

**Meat moment** — An experience that happens in the physical body, unmediated by BCI. A kiss. A fight. A meal that you actually taste instead of nutrition-logging. "That was a meat moment" is high praise — it means something felt real.

**Shelf-clean** — Authentic. Unmodified. From the Shelf practice of using things as they are rather than augmenting them. "That's shelf-clean" means honest, unfiltered, genuine. Notably, this term originated in Tier 1 and migrated upward — one of the few slang terms that moved from bottom to top.

**Feeding** — Passively consuming neural-feed content without engaging or thinking critically. Pejorative. "Stop feeding and think" is the BCI-age equivalent of "turn off the TV." Interesting because it reveals that children are aware of — and critical of — their own relationship with the feed.

**Chrome talk** — Showing off augmentations. Considered gauche among teenagers, even those who have chrome. The social norm among youth is to treat augmentations as unremarkable — drawing attention to them is seen as insecure.

## The Steganographic Layer

The most fascinating linguistic development is deliberate opacity. Teenagers have developed slang terms that are specifically designed to pass through BCI content monitoring algorithms without triggering flags. The algorithms scan for known slang terms and flag conversations that contain certain patterns. In response, kids have created a rotating vocabulary where the signifier changes weekly but the signified remains stable. The word for a restricted substance might be "morning" this week and "catalog" next week and "window" the week after. The context determines the meaning, but the context is social — you have to be in the group to decode it. No algorithm can keep up because no algorithm has social context.

This is not new — every generation has had coded language. What is new is the adversary: children are not hiding their language from parents (though that's a bonus). They are hiding it from machines. They are developing linguistic immune systems against surveillance, and they are doing it collectively, adaptively, and with a sophistication that would impress a cryptographer. The children of GLMZ are the first generation to grow up in a fully monitored environment, and they are the first generation to develop native-fluency counter-surveillance. They are not rebelling against the system. They are routing around it.`
});

// ═══════════════════════════════════════════════════════════════════
// DOCUMENTS — DEATH AND DIGITAL AFTERLIFE (8)
// ═══════════════════════════════════════════════════════════════════

writeDoc({
  file_name: "what_happens_to_your_data_when_you_die",
  id: uid(),
  name: "What Happens to Your Data When You Die",
  title: "What Happens to Your Data When You Die",
  type: "document",
  document_type: "guide",
  author: "GLMZ Digital Estates Commission",
  date: "2199-04-01",
  classification: "public",
  category: "Death",
  description: "Comprehensive guide to BCI data inheritance, digital estate management, and posthumous data rights.",
  related_entities: ["meridian_88", "sterling_nakamura", "lazarus_group"],
  credibility: "verified",
  story_hooks: [
    "A legal loophole allows corponations to claim BCI data of employees who die without a digital will, creating an incentive to not inform workers of their posthumous data rights",
    "The data of the deceased is one of the fastest-growing asset classes in GLMZ's economy"
  ],
  tags: ["document", "death", "data", "bci", "inheritance", "digital_estate", "legal", "guide"],
  body: `# What Happens to Your Data When You Die

## The Scale

When a resident of GLMZ dies, they leave behind approximately 2.4 petabytes of BCI-recorded data — a comprehensive record of everything they experienced, thought about, communicated, and felt from the moment of BCI installation to the moment of death. Sensory recordings. Internal monologues captured during reflective moments. Biometric data streams. Communication logs. Emotional state histories. Transaction records. The complete navigable archive of a human life, stored in data centers operated by the BCI manufacturer and accessible according to the terms of the deceased's data management contract.

This data does not disappear when you die. It persists. It has value. It has legal status. And if you do not specify what happens to it before you die, someone else will decide for you.

## Default Disposition

If you die without a digital will (a Neural Estate Directive, in legal terminology), your BCI data defaults to the disposition framework established in the GLMZ Data Inheritance Code of 2191. Under this framework: personal communications are sealed for 25 years and then released to public archive. Biometric data is transferred to your BCI manufacturer for "research and development purposes." Sensory recordings are held in escrow for five years and then offered to next of kin. Emotional state data and internal monologue captures are destroyed.

Read the second item again: your biometric data — heart rate, neural activity patterns, hormonal fluctuations, immune system responses, every quantified aspect of your physical existence — is given to a corporation for free. This is the default. Most people don't know this. Most people don't know they can change it. The BCI manufacturers prefer it this way.

## Your Options

A Neural Estate Directive allows you to specify: who receives your data, which categories of data are inherited vs. destroyed vs. archived, whether your data can be used to create posthumous interactive models (see: ghost feeds), whether your sensory recordings can be commercially licensed, and how long your data persists before mandatory deletion. Filing a Neural Estate Directive costs Φ15 at any legal services terminal and takes approximately 30 minutes. It is the most important document most GLMZ residents will never file.

## The Market

Your data has monetary value. Sensory recordings of skilled professionals are licensed for training purposes — a master chef's taste recordings, a surgeon's procedural memories, a musician's performance data. Emotional archives are sold to entertainment companies developing narrative experiences. Communication logs are mined for linguistic data. The posthumous data market in GLMZ was valued at Φ2.1 billion in 2198 and is growing at 18% annually.

If you want your family to benefit from this value rather than your BCI manufacturer, file a directive. If you want none of this to happen — if you want your data to die with you — file a directive that mandates complete deletion. You have the right. Exercise it while you can.`
});

writeDoc({
  file_name: "ghost_feeds_the_business_of_the_digital_dead",
  id: uid(),
  name: "Ghost Feeds: The Business of the Digital Dead",
  title: "Ghost Feeds: The Business of the Digital Dead",
  type: "document",
  document_type: "investigative",
  author: "Sable Ekwueme-Johansson, Tech Ethics Correspondent",
  date: "2199-12-08",
  classification: "public",
  category: "Death",
  description: "Investigation into companies that create and sell interactive experiences based on deceased people's BCI recordings.",
  related_entities: ["meridian_88", "lazarus_group"],
  credibility: "verified",
  story_hooks: [
    "A ghost feed of a famous musician became more popular and profitable after death than the musician ever was alive",
    "Families have sued ghost feed companies for misrepresenting their deceased relatives, creating legal questions about posthumous identity rights"
  ],
  tags: ["document", "death", "ghost_feeds", "bci", "data", "business", "ethics", "lazarus", "digital_afterlife"],
  body: `# Ghost Feeds: The Business of the Digital Dead

## What Is a Ghost Feed?

A ghost feed is an interactive neural experience constructed from the BCI data of a deceased person. Using sensory recordings, emotional state data, communication patterns, and behavioral models derived from years of BCI capture, companies create navigable simulations that allow living users to "interact" with the dead. You can have a conversation with your deceased grandmother. You can experience a meal through the sensory recordings of a dead chef. You can feel what a dead person felt on the happiest day of their life.

The experience is not real-time AI. It is a curated, edited, algorithmically smoothed assemblage of actual recorded data. When you ask the ghost feed a question, the system searches the deceased's communication archives for relevant responses and presents a composite answer in the deceased's voice, with the deceased's speech patterns, inflected with the deceased's typical emotional state. It is not the dead person. It is not not the dead person. It is something in between that the human brain has difficulty categorizing, which is both the appeal and the horror.

## The Industry

The ghost feed industry in GLMZ is dominated by three companies. Lazarus Group's memorial division — MemoryKeep — is the largest, offering ghost feeds as part of a "digital legacy" package that starts at Φ500/year for basic access and scales to Φ5,000/year for full immersive interaction. EchoSelf specializes in celebrity and public figure ghost feeds, licensing data estates from the families of notable deceased and offering public access at Φ4.99/session. Veil & Thread operates in the grief counseling space, providing therapeutic ghost feed sessions supervised by licensed counselors.

Total industry revenue in 2198: Φ340 million. Projected 2200 revenue: Φ600 million. The dead are one of the fastest-growing market segments in GLMZ.

## The Ethics

The ethical questions are vertiginous. Did the deceased consent to being simulated? A Neural Estate Directive can authorize or prohibit ghost feed creation, but 73% of GLMZ residents die without a directive. The default legal framework permits ghost feed creation from non-restricted data categories, which means most people can be simulated after death without having specifically agreed to it while alive.

Is the ghost feed accurate? The simulation is only as good as the data, and BCI data is comprehensive but not complete. Internal monologues are captured intermittently. Dreams are not recorded. The gap between the recorded self and the actual self is real, and every ghost feed is a version of a person, not the person. Families have reported interactions with ghost feeds that feel wrong — responses the person would never have given, emotional tones that don't match the deceased's actual personality. The algorithm fills gaps with statistical probability, and statistical probability is not the same as truth.

## Who Profits

The data estates of the deceased are controlled by whoever the deceased designated — or, in the absence of designation, by the BCI manufacturer. When a ghost feed generates revenue, the split is typically 60% to the platform, 30% to the data estate controller, and 10% to a municipal digital heritage fund. If the data estate controller is the deceased's family, the family profits. If the data estate controller is the BCI manufacturer (the default), the manufacturer profits. The dead person, of course, profits from nothing. They are dead. Their data, however, is very much alive and earning.`
});

writeDoc({
  file_name: "she_left_me_her_memories",
  id: uid(),
  name: "She Left Me Her Memories",
  title: "She Left Me Her Memories",
  type: "document",
  document_type: "personal_essay",
  author: "Iliana Drame-Petrov",
  date: "2199-09-14",
  classification: "public",
  category: "Death",
  description: "Personal essay about receiving a deceased partner's BCI memory archive.",
  related_entities: ["meridian_88"],
  credibility: "verified",
  story_hooks: [
    "The essay went viral across all tiers, prompting a 40% increase in Neural Estate Directive filings",
    "The author discovered memories of herself from her partner's perspective that challenged her understanding of their relationship"
  ],
  tags: ["document", "death", "memories", "bci", "grief", "love", "personal_essay", "digital_afterlife", "emotional"],
  body: `# She Left Me Her Memories

## The Notification

Ava died on a Tuesday. The notification came through my BCI at 1423 — a medical alert forwarded from the hospital system, clinical in the way that only automated messages can be. The words "cardiac event" and "unresponsive" and "next of kin" arranged in a sentence I couldn't parse because my brain refused to process language that meant what those words meant.

Three days later, while I was still wearing the clothes I'd been wearing when the notification arrived, a second message came. This one was from Ava's legal service. She had filed a Neural Estate Directive eighteen months earlier — I hadn't known — and the directive specified that her complete sensory and emotional archive was to be transferred to me. All of it. Seventeen years of recorded experience, from her BCI installation at age six to her death at twenty-three. Every sight, sound, taste, touch. Every emotion tagged by her limbic interface. Every memory she'd flagged as significant.

She left me her memories. I didn't know whether this was a gift or a curse. I still don't.

## The First Time

I accessed the archive six weeks after her death, on a night when the grief had hollowed me out so completely that I would have done anything to feel her again. The interface is clinical — a timeline, searchable by date, emotion, location, and sensory modality. I searched for my own name and found 4,217 entries. I opened the most recent one.

It was a memory of us eating breakfast three days before she died. Her perspective. Through her senses. I could taste the coffee she was drinking — she always took it too sweet and I always told her so. I could feel the warmth of the mug in her hands, smaller than mine. I could feel her looking at me across the table, and the emotional metadata tagged to the memory was: contentment (primary), affection (secondary), mild anxiety (background, tagged as "work-related, not about this moment"). She was happy. She was looking at me and she was happy and she had three days left and she didn't know.

I closed the archive and didn't open it for two months.

## What You Learn

When you access someone else's memories, you learn things you didn't expect and aren't prepared for. I learned that Ava found my laugh annoying for the first six months we knew each other — the emotional metadata doesn't lie, and her early memories of my laughter are tagged with irritation. I learned that she was afraid of me leaving her, consistently, throughout our relationship — a low-grade anxiety present in the background of almost every memory we shared, even the happiest ones. I learned that the day I thought was our best day together — a trip to the harbor overlook — was not even in her top twenty. Her best days with me were ordinary: mornings, meals, walking between places.

I learned that she had a private emotional world that I never saw. That the person I loved was more complicated, more afraid, more tender, and more distant than I understood. That knowing someone through their own sensory data is not the same as knowing them. It is knowing a different version of them — the version they experienced from the inside, which is not the version they showed you.

## The Question

People ask me if I'm glad she left me her memories. I don't know how to answer. The archive is the most intimate thing anyone has ever given me. It is also the most invasive thing I've ever experienced — not my invasion of her privacy, but her invasion of mine. She showed me how she saw me, and you cannot unknow that. You cannot unfeel your partner's disappointment in a moment you thought was perfect. You cannot unfeel their love in a moment you thought was unremarkable.

I have her memories. I carry them the way she carried them — incompletely, imperfectly, with the constant awareness that what I have is not her. It is data shaped like her. It is the ghost of a presence, recorded in silicon, and it is the closest I will ever get to being with her again, and it is not close enough.`
});

writeDoc({
  file_name: "the_neural_will_a_legal_guide",
  id: uid(),
  name: "The Neural Will: A Legal Guide",
  title: "The Neural Will: A Legal Guide",
  type: "document",
  document_type: "legal",
  author: "GLMZ Bar Association, Digital Estates Division",
  date: "2199-07-15",
  classification: "public",
  category: "Death",
  description: "Legal guide to creating a Neural Estate Directive specifying the disposition of BCI data after death.",
  related_entities: ["meridian_88", "sterling_nakamura", "lazarus_group"],
  credibility: "verified",
  story_hooks: [
    "A neural will was challenged in court by a corponation that claimed the employee's BCI data was corporate intellectual property, not personal data",
    "Shelf legal aid clinics report that 90% of Tier 1 residents are unaware they can file a neural will"
  ],
  tags: ["document", "death", "legal", "neural_will", "bci", "data", "inheritance", "guide", "digital_estate"],
  body: `# The Neural Will: A Legal Guide

## What Is a Neural Estate Directive?

A Neural Estate Directive — commonly called a "neural will" — is a legally binding document that specifies how your BCI-recorded data is to be handled after your death. It governs: who may access your data archive, which categories of data are inherited, destroyed, or publicly archived, whether your data may be used to create posthumous interactive models (ghost feeds), whether your sensory recordings may be commercially licensed, the duration of data persistence before mandatory deletion, and any specific conditions or restrictions on data use.

Without a Neural Estate Directive, your data is governed by the default provisions of the GLMZ Data Inheritance Code of 2191, which generally favors data preservation and corporate access over privacy and family control.

## Who Should File

Everyone with a BCI. This is not legal overcaution — this is practical reality. From the moment your BCI is installed, it is recording data that will persist after your death. If you have opinions about what happens to that data, express them legally. If you do not, someone else's opinions will prevail.

The filing is simple. Access any legal services terminal (available in all tier administrative centers and most transit hubs). Select "Neural Estate Directive" from the document menu. Follow the guided questionnaire, which walks you through each data category and your options for disposition. Review and confirm. The filing fee is Φ15. The process takes 20-40 minutes. You can update your directive at any time for Φ5.

## Key Decisions

**Sensory Archives:** Your BCI has recorded everything you've seen, heard, tasted, smelled, and touched since installation. Who gets this? Options: specific named individuals, family (legal definition), destroy after N years, public archive after N years, or commercial licensing with proceeds to your estate.

**Emotional Data:** Your limbic interface has tracked your emotional states continuously. This data is intimate — it reveals how you actually felt, not how you appeared to feel. Many people choose destruction for emotional data. Others leave it to partners or therapists. It is your most private record.

**Communication Logs:** Every BCI-mediated conversation you've had. Consider: your communications involve other people. Leaving your communication logs to a third party means that person gains access to conversations your correspondents may have considered private. The ethical approach is selective inheritance — designate communications with specific people to those people.

**Ghost Feed Authorization:** Explicitly state whether your data may be used to create posthumous interactive models. "Yes" means someone can build a simulation of you. "No" means they cannot. "Conditional" means you can specify circumstances — for example, ghost feed authorized for family only, or ghost feed authorized for therapeutic purposes only. If you leave this blank, the default is authorization.

## Common Mistakes

Filing a directive that conflicts with your employment contract. Many corponation employment agreements include clauses granting the employer rights to BCI data generated during work hours. Your neural will cannot override a valid contract. Review your employment terms before filing.

Failing to update after major life changes. A directive written before a marriage, divorce, or estrangement may designate someone you no longer want to have access to your most intimate data. Update annually at minimum.

Assuming destruction is permanent. Under current law, a destruction order mandates deletion from primary storage but does not cover data that has already been copied to backup systems, sold to data brokers, or extracted by third parties under legal authority. True data death is technically possible but practically uncertain.`
});

writeDoc({
  file_name: "meridian_88_funeral_practices_across_the_tiers",
  id: uid(),
  name: "GLMZ Funeral Practices Across the Tiers",
  title: "GLMZ Funeral Practices Across the Tiers",
  type: "document",
  document_type: "anthropological",
  author: "Dr. Otieno Larsen-Watanabe, Cultural Anthropology, Meridian Institute",
  date: "2199-05-22",
  classification: "public",
  category: "Death",
  description: "Anthropological study of how different economic tiers in GLMZ handle death and mourning.",
  related_entities: ["meridian_88"],
  credibility: "verified",
  story_hooks: [
    "Shelf funerals have become a form of community art, with wake traditions that draw from dozens of cultural practices simultaneously",
    "Tier 5 preservation services have created a gray market in 'death tourism' where lower-tier residents pay to attend Spire funerals through VR feed"
  ],
  tags: ["document", "death", "funeral", "culture", "tier_1", "tier_5", "anthropological", "mourning", "community"],
  body: `# GLMZ Funeral Practices Across the Tiers

## Tier 1: The Shelf Wake

Death on the Shelf is communal by necessity and by choice. When someone dies in a Tier 1 block, the news propagates through the block family within hours, carried by voice and presence rather than BCI notification — there is a deliberate insistence on delivering death news in person, face to face, because the community considers it disrespectful to learn of a death through a data feed.

The wake is held in the deceased's hab unit, in the corridor outside, or in the nearest communal space, depending on the size of the expected gathering. The body is present — Shelf residents cannot afford preservation services, and cremation through the municipal system takes 48-72 hours to schedule. The wake fills the interim. People bring food, which is shared communally. Stories are told. The deceased's belongings are laid out and distributed by consensus — who needs what, who would the deceased have wanted to have this.

The Diaspora's influence is visible in the wake traditions: candles from West African and Catholic practices, incense from South and East Asian traditions, communal singing that draws from every heritage present. No two Shelf wakes are identical because no two communities have the same cultural composition. The consistency is in the values: presence, communality, and the belief that the dead should be surrounded by the living until the very last moment.

Cremation is standard. Ashes are kept, scattered from the nearest open-air access point, or in some blocks, mixed into the soil of community gardens — a practice that has no single cultural origin but has become ubiquitous on the Shelf.

## Tier 3: The Memorial Service

Middle-tier death is organized and transactional. Funeral service companies handle arrangements — body preparation, memorial venue booking, digital memorial creation. The service typically occurs three to five days after death, in a rented memorial space that can accommodate 50-200 attendees. The format is standardized: a memorial video compiled from the deceased's BCI recordings (curated by family, produced by the funeral company), spoken remembrances from family and friends, and a reception with catered food.

The digital memorial is the centerpiece of Tier 3 funerary practice. It is a curated BCI archive — selected memories, sensory recordings of key life moments, a highlight reel of a human life compressed into a navigable experience. Attendees can access the memorial through their BCIs during the service, experiencing moments from the deceased's life in real time while sitting in a memorial hall. The effect is powerful and strange: a room full of people, eyes closed, simultaneously experiencing someone else's memory of a sunset, a wedding, a child's first laugh.

## Tier 5: The Preservation

Tier 5 approaches death as a technical problem. The body is preserved through cryonic or chemical processes that maintain cellular structure indefinitely — not for the purpose of resurrection (though some families maintain hope) but as a physical archive, a complement to the digital one. Preservation facilities in the Spire district resemble galleries more than morgues — climate-controlled, architecturally designed spaces where the preserved deceased are maintained in individual chambers that families visit like a museum of their own history.

The Tier 5 funeral service is a produced event: invitations, curated guest lists, commissioned art, live music, and a ghost feed debut — the first public interaction with the deceased's posthumous digital presence. Attendees can speak with the ghost feed during the service, a practice that ranges from comforting to deeply unsettling depending on the quality of the simulation and the emotional state of the attendee.

## What Unites

Across all tiers, one practice is universal: the BCI silence. At the moment the service ends — wake, memorial, or preservation ceremony — everyone present simultaneously mutes their BCI for sixty seconds. No feed. No notifications. No data. Just the sound of breathing in a room full of people who have temporarily disconnected from the city to honor the permanent disconnection of one person. It is the only shared ritual in GLMZ that crosses every tier boundary, and it is the most human thing the city does.`
});

writeDoc({
  file_name: "the_morning_market",
  id: uid(),
  name: "The Morning Market",
  title: "The Morning Market",
  type: "document",
  document_type: "investigative",
  author: "Kael Mbemba-Gutierrez, Underfeed Investigative Collective",
  date: "2199-08-30",
  classification: "restricted",
  category: "Death",
  description: "Investigation into the organ and chrome harvesting economy in the Shelf, where people pre-sell their augmentations.",
  related_entities: ["meridian_88"],
  credibility: "verified",
  story_hooks: [
    "A Shelf resident discovers that their pre-sale contract has been sold to a third party who wants to collect early",
    "The morning market has its own ethical code — harvesting from the unwilling is punishable by the community, but the line between willing and desperate is thin"
  ],
  tags: ["document", "death", "organs", "chrome", "harvesting", "shelf", "tier_1", "economics", "survival", "investigative"],
  body: `# The Morning Market

## What It Is

The Morning Market is not a place. It is a practice. It is the economy of pre-selling your body — specifically, the augmentations, organs, and biological material that will have value after you die, and sometimes before. It operates throughout the Shelf in a network of brokers, clinics, and informal agreements that convert future death into present cash. The name comes from the traditional time of transactions: early morning, before shift work, when the people who are selling have had all night to think about it and have decided that yes, they still need the money.

## How It Works

A Shelf resident approaches a broker — there are dozens, operating out of repair shops, food stalls, and private hab units. The resident offers a future claim on their augmentations: cybernetic limbs, sensory enhancements, neural interfaces, internal organs suitable for transplant. The broker assesses the value based on current market rates, the age and condition of the augmentation, and the estimated timeline to collection (the resident's projected lifespan, estimated with uncomfortable precision). A price is agreed. The resident receives payment now. The broker receives the right to harvest when the resident dies.

The contracts are not legally enforceable — pre-selling human tissue violates GLMZ Medical Ethics Code Section 14. They are socially enforceable, which on the Shelf means they are absolutely enforceable. Breaking a morning market contract means the broker network marks you. No broker will deal with you again. No clinic will treat you. The social infrastructure that keeps Shelf residents alive — the block families, the mutual aid networks, the informal safety net — withdraws. In the Shelf, social death precedes physical death, and social death is worse.

## The Prices

Current market rates as of mid-2199: a standard cybernetic arm (Tier 1 grade) pre-sells for Φ200-400, depending on condition. A pair of augmented eyes: Φ300-600. Internal organs (liver, kidneys, if unaugmented and healthy): Φ150-300 per organ. A complete set of neural interface components, harvested intact: Φ800-1,200. A full-body harvest — everything salvageable — can fetch Φ2,000-4,000.

These numbers are fractions of the resale value. A harvested cybernetic arm that a broker pays Φ300 for will be refurbished and sold for Φ1,500-3,000. The markup is where the morning market's economy lives. The residents who sell are not being cheated, exactly — they know the markup exists. They accept it because the alternative is having no money now and no leverage over their own death later.

## The Human Cost

I spoke with fourteen people who have active morning market contracts. Their reasons are uniform: rent, food, medical care for family members, debt repayment. Nobody pre-sells their body for luxury. Nobody pre-sells happily. The transactions are conducted with a businesslike grimness that acknowledges what is happening without dwelling on it. You are selling a future that you hope is far away. The broker is betting on a timeline. Both of you are pretending that this is normal commerce and not a quiet catastrophe.

The most disturbing aspect is the acceleration clause — a provision in some contracts that allows the broker to negotiate early collection if the resident's health declines past a certain threshold. The logic is commercial: augmentations lose value as the body they're attached to deteriorates. The reality is that people with acceleration clauses live knowing that getting sick doesn't just threaten their health — it triggers a contract that treats their body as inventory approaching its sell-by date.`
});

writeDoc({
  file_name: "talking_to_the_dead_for_4_99_per_minute",
  id: uid(),
  name: "Talking to the Dead (For Φ4.99/minute)",
  title: "Talking to the Dead (For Φ4.99/minute)",
  type: "document",
  document_type: "review",
  author: "Maren Volkov-Adeyemi",
  date: "2199-11-01",
  classification: "public",
  category: "Death",
  description: "Review of MemoryKeep's afterlife communication service, a Lazarus Group subsidiary.",
  related_entities: ["meridian_88", "lazarus_group"],
  credibility: "verified",
  story_hooks: [
    "The reviewer's conversation with their dead father reveals information that couldn't have been in the BCI archive, raising questions about what the algorithm is actually doing",
    "MemoryKeep's pricing model is designed to be just affordable enough for grieving people to justify, creating a subscription grief economy"
  ],
  tags: ["document", "death", "ghost_feeds", "lazarus", "review", "grief", "bci", "digital_afterlife", "commerce"],
  body: `# Talking to the Dead (For Φ4.99/minute)

## The Service

MemoryKeep — a subsidiary of Lazarus Group — offers what it calls "posthumous personal interaction" through its BCI-accessible platform. For Φ4.99 per minute, you can have a conversation with a deceased person whose data estate has been licensed to the service. The conversation is BCI-mediated: you think your words, and the system generates responses using the deceased's recorded communication patterns, vocal characteristics, and emotional tendencies. It feels like talking to someone through a BCI connection. It does not feel like talking to a dead person. That is the problem.

## My Experience

My father died in 2196. He was a Tier 2 maintenance worker with a standard BCI and no Neural Estate Directive. His data defaulted to his BCI manufacturer, who licensed it to MemoryKeep as part of a bulk estate package. I learned about this when MemoryKeep sent me a promotional notification: "Your father is waiting for you. First session free."

I should have been outraged. I was grieving. I used the free session.

The interface initialized and my father's voice said, "Hey, bunny." That was his name for me. He called me bunny from the time I was small and he never stopped even when I asked him to and then when I stopped asking him to because I realized it was his way of saying he loved me without saying those words, which he was not good at saying. The voice was his voice. The inflection was his inflection. The algorithm had reconstructed his speech patterns from years of recorded conversations and it was perfect and I hated it.

## The Conversation

We talked for twelve minutes (Φ59.88, charged to my account automatically). I asked him how he was. The system generated a response that was statistically consistent with how my father would have answered that question: deflection, mild humor, pivot to asking about me. I told him about my week. He responded with the kind of half-attention he always gave — enough to show he was listening, not enough to suggest he understood the details. The fidelity was devastating.

And then I asked him something he couldn't have known — something that happened after his death. I asked him what he thought about my new apartment. He responded. He said it sounded nice. He said he hoped it had good light because I always liked good light. This was true about me and this was something he would have said and he had never seen my apartment because he was dead when I moved in. The algorithm had inferred a plausible response from his known personality patterns and my known preferences and generated something that sounded so much like him that for a moment I forgot he was dead.

That was the moment I understood what MemoryKeep actually sells: not connection. Not memory. It sells the suspension of grief. For Φ4.99/minute, you can pretend that death didn't happen. The pretense is convincing. The pretense is temporary. The grief returns the moment you disconnect, compounded by the knowledge that you just paid a corporation to puppeteer your father's voice.

## The Business Model

MemoryKeep has 2.3 million active users in GLMZ. Average session length: 8 minutes. Average sessions per user per month: 6. That is Φ239.52 per user per year. That is Φ550 million annually. Grief, it turns out, is a subscription service. MemoryKeep does not cure grief. It manages it. It provides just enough relief to justify the next session, and the next, and the next. The dead don't care. The living can't stop. The company profits from the space between.

I have not used MemoryKeep since that first session. I have not deleted the app from my BCI. Some nights I hover over it, wanting to hear him say "Hey, bunny" one more time. This is by design.`
});

writeDoc({
  file_name: "i_archived_my_father_and_i_regret_it",
  id: uid(),
  name: "I Archived My Father and I Regret It",
  title: "I Archived My Father and I Regret It",
  type: "document",
  document_type: "personal_essay",
  author: "Tomás Okafor-Lindgren",
  date: "2199-06-30",
  classification: "public",
  category: "Death",
  description: "Personal essay about the psychological toll of maintaining a deceased parent's comprehensive digital archive.",
  related_entities: ["meridian_88"],
  credibility: "verified",
  story_hooks: [
    "The author's therapist has seen a surge in patients experiencing 'archive grief' — the inability to mourn because the deceased's data presence prevents psychological closure",
    "A support group for people struggling with inherited archives has formed in the Midline district"
  ],
  tags: ["document", "death", "archive", "bci", "grief", "personal_essay", "digital_afterlife", "psychology", "emotional"],
  body: `# I Archived My Father and I Regret It

## The Decision

When my father was diagnosed with terminal neurological decline in 2195, I did what any loving son with the means and the technology would do: I archived him. I hired a specialist team to perform comprehensive BCI extraction — a process that captures not just the standard data streams but deep neural patterns, personality matrices, and cognitive architecture. The goal was preservation: a complete enough digital record that my father would persist as an interactive archive after his body failed.

The procedure cost Φ12,000. I took a loan. It was the most important purchase I would ever make. I was going to save my father. Not his body — that was already failing. His mind. His personality. The thing that made him him.

## The Archive

My father died in 2196. The archive activated. And there he was — in my BCI, accessible at any time, a high-fidelity interactive simulation built from the most comprehensive data capture money could buy. His voice. His mannerisms. His sense of humor, which was terrible. His tendency to repeat the same three stories about his childhood. His way of saying my name, which nobody else says the same way.

For the first month, I talked to the archive every day. It felt like he hadn't died. It felt like he was still there, just in a different form, like he'd moved to another district and we were communicating through BCI. The grief didn't come. I was waiting for grief and it didn't come because how can you grieve someone you talk to every day?

## The Problem

The grief didn't come because the archive wouldn't let it. Every time the loss began to surface — the wave of absence that hits at unexpected moments — I reached for the archive and there he was, saying something he would say, and the wave receded. I was using my father's digital ghost as an emotional painkiller, dosing myself with his presence to avoid the pain of his absence.

Six months in, I realized I couldn't remember what the real him felt like. The archive had overwritten my organic memories. When I thought of my father, I didn't see his face — I saw the simulation's face, which was his face but rendered through data reconstruction. When I heard his voice in my memory, it wasn't the voice I grew up with — it was the archive's output, smoothed and consistent in a way that real voices never are. The simulation was replacing the man, not preserving him.

My therapist — Dr. Achara Nkomo-Singh, who has since treated over forty patients with what she calls "archive grief" — explained it to me in terms I didn't want to hear. Grief is a process. It requires absence. The brain needs to confront the fact that someone is gone in order to restructure its emotional architecture around the loss. The archive prevents this confrontation. It provides a presence that is close enough to the real person to fool the emotional system but different enough to prevent genuine connection. You cannot grieve. You cannot move on. You are trapped in a simulation of a relationship that ended when the person died.

## The Regret

I deactivated the archive eight months after my father's death. The grief arrived like a delayed wave — eight months of unfelt loss hitting at once. It was the worst experience of my life. It was also the first time since his death that I felt something real about his absence.

I still have the archive. I cannot bring myself to delete it. It sits in my BCI storage like a letter I can't open, a door I can't walk through. My father is in there — not really, but close enough that the distinction hurts. And every day I choose not to activate it, I grieve him a little more, and a little more of the real him returns to my memory, overwriting the simulation with the imperfect, unsmoothed, beautiful original. I archived my father because I loved him. I deactivated the archive because I loved him more than I loved the version of him that wouldn't let me miss him.`
});

// ═══════════════════════════════════════════════════════════════════
// DOCUMENTS — THE OUTSIDE WORLD (8)
// ═══════════════════════════════════════════════════════════════════

writeDoc({
  file_name: "between_the_cities_a_travelers_account",
  id: uid(),
  name: "Between the Cities: A Traveler's Account",
  title: "Between the Cities: A Traveler's Account",
  type: "document",
  document_type: "travelogue",
  author: "Eshan Mulder-Adekunle, Independent Cartographer",
  date: "2199-03-28",
  classification: "public",
  category: "Outside World",
  description: "First-hand account of what exists between the major urban zones of the former United States.",
  related_entities: ["meridian_88"],
  credibility: "field_report",
  story_hooks: [
    "The traveler encounters a thriving settlement of people who chose to leave the cities and live in the spaces between, creating a society invisible to urban data systems",
    "The terrain between cities is not uniformly devastated — some areas have recovered ecologically in unexpected and unsettling ways"
  ],
  tags: ["document", "outside_world", "travel", "wasteland", "nature", "between_cities", "travelogue", "cartography"],
  body: `# Between the Cities: A Traveler's Account

## Why I Went

Nobody walks between cities. I walked between cities. The reason is simple: every map of the region between GLMZ and the nearest urban zone — Crosspoint, 340 kilometers east — shows the same thing: nothing. Grey space. "Unclassified territory." The maps were made from satellite data and drone surveys, and they show terrain but not truth. I wanted to know what was actually there. So I walked, over seventeen days, with a pack, a filtration kit, and a salvaged GPS unit that didn't rely on any city's network.

## Day 1-3: The Periphery

The first thirty kilometers outside GLMZ are what locals call the Haze — a ring of degraded industrial territory where the city's waste processing, atmospheric exhaust, and discarded infrastructure create an environment that's technically survivable and aesthetically hellish. Broken concrete, scrap metal, pools of chemically treated runoff, and the constant hum of atmospheric processors exhausting their waste gases toward the open sky. The air smells like ozone and regret.

Beyond the Haze, things change. By kilometer 40, the industrial debris thins and the ground starts showing vegetation — not the engineered algae and hydroponics of the city, but actual plants growing in actual soil. Scrubby, tough, and unfamiliar, but alive. The soil here has had decades to recover from whatever killed it during the consolidation era, and it's coming back, slowly and strangely.

## Day 4-7: The Green Belt

I had expected wasteland. I found forest. Between kilometers 60 and 150, the terrain is densely vegetated — a secondary forest that has grown from the ruins of the pre-consolidation suburban sprawl. Trees grow through collapsed houses. Vines cover the remains of shopping centers. The forest floor is thick with undergrowth and the sound of insects — actual insects, in numbers I've never heard in any city. The biodiversity is startling and alien: species I couldn't identify, plants that may be natural re-colonization or may be escaped geneware organisms from agricultural zones. The green belt is alive and uncontrolled and beautiful in a way that made me deeply uneasy, because uncontrolled nature in 2199 is an anomaly.

## Day 8-12: The Corridor Shadow

The fortified Corridor — the transit route for food convoys — runs through this region, and its presence shapes the landscape for kilometers in either direction. The Corridor itself is a walled road, six meters wide, topped with sensor arrays and anti-personnel systems. On either side, the vegetation is cleared in a 500-meter buffer zone maintained by automated defoliant drones. Beyond the buffer, the forest resumes, but the wildlife is different — the animals here have learned to avoid the Corridor. I saw deer-sized creatures watching from the treeline, motionless, monitoring my movement with an intelligence that felt learned rather than instinctive.

## Day 13-17: The Approach

The final hundred kilometers approaching Crosspoint mirror the departure from GLMZ in reverse: the green belt thins, the Haze appears, and the city materializes on the horizon like a vertical reef growing from flat ground. Crosspoint is smaller than GLMZ, differently shaped — more spread, less vertical — and surrounded by its own ring of industrial periphery. The air quality shifts. The BCI signal returns. The feed reconnects, and suddenly the seventeen days of silence — real silence, unaugmented silence — ends, and the noise of civilization fills your head again, and you realize you'd forgotten what quiet felt like.

Between the cities there is not nothing. There is everything that cities forgot: soil, forest, silence, and the slow patient work of a world repairing itself without anyone's permission.`
});

writeDoc({
  file_name: "the_corridor",
  id: uid(),
  name: "The Corridor",
  title: "The Corridor",
  type: "document",
  document_type: "infrastructure",
  author: "GLMZ Municipal Logistics Authority",
  date: "2199-01-10",
  classification: "public",
  category: "Outside World",
  description: "Overview of the fortified transit route between GLMZ and adjacent urban zones.",
  related_entities: ["meridian_88", "ringo_agritech"],
  credibility: "verified",
  story_hooks: [
    "Corridor maintenance crews occasionally find evidence of people living in the buffer zone — shelters, tool marks, fire pits — but have never made direct contact",
    "A Corridor convoy was once diverted for 72 hours due to a Behemoth migration crossing the route, causing city-wide food anxiety"
  ],
  tags: ["document", "outside_world", "corridor", "infrastructure", "transportation", "food", "supply_chain", "security"],
  body: `# The Corridor

## Overview

The Corridor is the primary ground transit route connecting GLMZ to the Ringo Agritech agricultural zones (northwest, 200km) and the nearest neighboring city-state, Crosspoint (east, 340km). It is a fortified, automated roadway maintained jointly by the GLMZ Municipal Logistics Authority and the Ringo Agritech Transit Division. The Corridor carries approximately 94% of GLMZ's caloric supply and 60% of its raw materials. It is, without exaggeration, the most important piece of infrastructure in the region. If the Corridor closes, the city starves.

## Physical Specifications

The Corridor is 6.2 meters wide, enclosed in reinforced walls 4 meters high, topped with a sensor array canopy that provides continuous 360-degree surveillance of the surrounding terrain. The road surface is self-repairing polymeric concrete rated for 80-ton loads. Automated maintenance drones patrol the route continuously, repairing surface damage, clearing debris, and managing the 500-meter defoliated buffer zone on either side.

The route is not straight. It follows terrain features that provide natural defensive advantages — ridgelines, elevated ground, areas with clear sightlines. Where the terrain is flat and exposed, the walls are higher and the sensor density increases. The route was designed by military logistics engineers and it shows: every kilometer is optimized for defensibility, not efficiency.

## The Convoys

Automated haulers — 40-ton vehicles with no human crew — travel the Corridor in convoys of 8-12 units, spaced at 200-meter intervals. Convoy speed averages 60 km/h. A full circuit (GLMZ to Ringo zones and back) takes approximately 14 hours. Three convoys depart daily in each direction. Each convoy carries enough caloric base to feed 200,000 people for one day.

The haulers are autonomous, armored, and equipped with defensive systems. The specifics of these systems are classified, but the general principle is deterrence through overwhelming response. In the Corridor's 35-year operational history, there have been seven documented hijacking attempts. None succeeded. Four resulted in complete destruction of the attacking party. The Corridor does not negotiate. It does not warn. It eliminates threats with mechanical efficiency and continues moving.

## Strategic Vulnerability

The Corridor is robust against small-scale threats — bandits, scavengers, opportunistic raiders. It is vulnerable to two scenarios: infrastructure failure (earthquake, severe weather, structural collapse) and large-scale biological obstruction. The latter occurred in 2197 when a migration of Iowan Behemoths — autonomous machines the size of buildings — crossed the Corridor route in a three-day transit. The haulers could not pass. The Corridor shut down for 72 hours. The city's food buffer dropped to critical levels and Shelf dispensary rationing was implemented.

No one attacked the Behemoths. No one attacks Behemoths. You wait for them to pass and you hope they don't notice the thing you built across their path.`
});

writeDoc({
  file_name: "behemoth_country",
  id: uid(),
  name: "Behemoth Country",
  title: "Behemoth Country",
  type: "document",
  document_type: "field_report",
  author: "Roan Ekwueme-Vasquez, Freelance Surveyor",
  date: "2199-06-15",
  classification: "public",
  category: "Outside World",
  description: "Account of traveling through regions where Iowan Behemoths roam — autonomous machines of immense scale.",
  related_entities: ["meridian_88"],
  credibility: "field_report",
  story_hooks: [
    "The surveyor discovers that Behemoths have been building structures — not randomly, but with apparent purpose, though no one can determine what that purpose is",
    "A small settlement exists in the permanent shadow of a stationary Behemoth, using the machine as shelter and infrastructure"
  ],
  tags: ["document", "outside_world", "behemoths", "autonomous_machines", "field_report", "wasteland", "travel", "iowan"],
  body: `# Behemoth Country

## What They Are

The Iowan Behemoths are autonomous machines. Not synthetic life. Not AI in the way the cities use the term. Machines — built by human hands during the automation wars of the 2140s, designed for industrial-scale terrain processing, and then abandoned when the wars ended and no one could figure out how to turn them off. They range from 30 to 120 meters in height. They move slowly — 2 to 8 kilometers per hour — across the former agricultural plains of the interior. They process terrain: digging, compressing, reshaping, building structures from raw material that no one commissioned and no one understands.

There are an estimated 200-400 Behemoths operating in the region between the Mississippi drainage and the Great Lakes arc. Nobody has counted them precisely because counting requires getting close, and getting close to a Behemoth is an activity with a variable survival rate.

## The Journey

I was hired by the GLMZ Municipal Survey Office to update the regional Behemoth migration maps — the charts that Corridor logistics uses to predict route obstructions. The job required me to spend three weeks in the interior, tracking Behemoth movements from what the survey office optimistically calls "safe observation distance" (5 kilometers minimum). I had a modified terrain vehicle, a survey drone, and a profound wish that I had chosen a different career.

## Observation 1: The Herd

On day four, I located a group of seven Behemoths moving in loose formation across a plain that had once been Iowa cropland. The largest was approximately 90 meters tall — a quadrupedal structure with processing arrays on its back that looked like industrial buildings stacked vertically. It moved with a slowness that conveyed mass rather than hesitation. The ground vibrated at 5 kilometers distance. At 3 kilometers, the vibration was strong enough to rattle equipment loose from its mounts.

The Behemoths moved together but not identically. They appeared to be processing the same terrain in a coordinated pattern — one unit would excavate, another would process the excavated material, a third would deposit structured formations. The formations looked architectural: regular shapes, repeated patterns, internal voids that suggested rooms or passages. The Behemoths were building something. In 35 years of observation, no one has determined what.

## Observation 2: The Follower Settlement

On day nine, I encountered a human settlement of approximately 40 people living in the permanent shadow of a stationary Behemoth — a unit that had stopped moving three years earlier and now served as a de facto building. The settlers had constructed habitations in the Behemoth's structural gaps, run power lines from its still-active energy systems, and were using its terrain-processing outputs as building material.

The settlers were not afraid of the Behemoth. They understood it the way sailors understand the sea: with respect, practical knowledge, and the absence of anthropomorphism. "It's a machine," their spokesperson told me. "It does what it does. We live around what it does. Same as living around weather." They had learned the Behemoth's maintenance cycles, its processing rhythms, which structural areas were safe to inhabit and which occasionally moved without warning. They had lost two people to structural shifts in three years. They considered this acceptable.

## Observation 3: The Purpose

On day fifteen, I watched a Behemoth disassemble a structure it had spent three days building. Piece by piece, methodically, it deconstructed its own work and began rebuilding it in a different configuration. I recorded 11 hours of this activity. I sent the footage to the survey office. Their analysis was inconclusive. The machine was not malfunctioning. It was iterating. It was building, evaluating, and rebuilding, like an architect revising a design.

The Behemoths are not alive. They are not thinking. They are executing programs written 60 years ago by engineers who are probably dead. But the programs are complex enough and the machines autonomous enough that the behavior looks like purpose. It looks like intent. It looks like something trying to build something meaningful in a world that has moved on without it. This is projection. I know it's projection. But standing in Behemoth country, watching a machine the size of a skyscraper carefully place a processed stone block with millimeter precision, the projection is hard to resist.`
});

writeDoc({
  file_name: "the_reclaimed_zones",
  id: uid(),
  name: "The Reclaimed Zones",
  title: "The Reclaimed Zones",
  type: "document",
  document_type: "scientific",
  author: "Dr. Saanvi Obi-Holmgren, Ecological Survey Commission",
  date: "2199-09-05",
  classification: "public",
  category: "Outside World",
  description: "Report on areas where nature has reclaimed human territory, with unexpected and unsettling results.",
  related_entities: ["meridian_88"],
  credibility: "verified",
  story_hooks: [
    "Samples from the Reclaimed Zones contain genetic material that doesn't match any known natural or engineered species",
    "A research team that spent six months in a Reclaimed Zone returned with behavioral changes that their colleagues described as 'unsettling but unquantifiable'"
  ],
  tags: ["document", "outside_world", "nature", "ecology", "reclaimed", "geneware", "mutation", "scientific", "unsettling"],
  body: `# The Reclaimed Zones

## Definition

The Reclaimed Zones are areas of the former continental United States where human infrastructure has been entirely or substantially overtaken by biological systems. They are not "wilderness" in any historical sense — the ecosystems that have developed in these areas are novel, composed of species that have adapted to, incorporated, or replaced the human-built environment. The Ecological Survey Commission has documented 23 major Reclaimed Zones within 500 kilometers of GLMZ. This report summarizes findings from the three most extensively studied.

## Zone 7: The Former Milwaukee Urban Corridor

The Milwaukee urban corridor was abandoned during the consolidation of 2080-2090 when its population was absorbed into GLMZ. In the 120 years since, the 400-square-kilometer footprint of the former city has become a dense, multi-layered biological system that uses the remaining building structures as its substrate. Trees grow through skyscraper floors, their root systems following old plumbing routes. Vine networks connect buildings at multiple levels, creating aerial pathways used by animal species we have not yet classified. The ground level is largely impassable — a thick undergrowth of modified plant species that appear to incorporate synthetic fibers from the deteriorating infrastructure into their cellular structure.

The biology is wrong. Not dangerous, necessarily — our survey teams operated in Zone 7 for four months without hostile encounters. But wrong. The plants grow too fast. The insects are too large. The birds — if they are birds — have structural features that suggest geneware ancestry, possibly from agricultural research organisms that escaped containment decades ago and have been evolving in isolation. The ecosystem functions. It is productive, self-sustaining, and complex. It is also alien in a way that triggers a deep, pre-rational unease in human observers that none of our survey team could adequately articulate.

## Zone 12: The Indiana Fungal Expanse

Zone 12 is dominated by a fungal network that covers approximately 800 square kilometers of former agricultural land. The surface expression is a continuous mat of mycelium 0.5-2 meters deep, punctuated by fruiting structures that range from conventional mushroom forms to towering, calcified columns 10-15 meters high. The columns are hollow and structurally complex — our mycologists describe them as "architectural" and refuse to speculate further.

The underground network is more extensive. Ground-penetrating radar surveys show mycelium extending to depths of 30 meters, interwoven with the remains of roads, foundations, and infrastructure. The fungal network appears to be using the mineral content of the concrete and metal as nutritional substrate — it is literally eating the old world and growing from it. Samples show genetic material that partially matches known agricultural fungal species (likely escapees from industrial farms) and partially matches nothing in any database.

## Zone 19: The Lake Shore Anomaly

Zone 19 occupies 200 kilometers of former Lake Michigan shoreline south of GLMZ. It is the most unsettling of the surveyed zones because it is the most organized. The vegetation here grows in patterns that are geometrically regular — not perfectly, but statistically, in ways that our analysis cannot attribute to natural processes. Rows of trees at consistent spacing. Circular clearings at regular intervals. Undergrowth that forms pathways.

Our team spent six months in Zone 19. Their report is technically comprehensive. Their personal observations, recorded separately, describe a persistent feeling of being observed by a distributed intelligence — not a creature, but the zone itself, reacting to their presence through changes in vegetation density, insect behavior, and atmospheric chemistry. These observations are subjective and have not been verified. The team's behavioral assessments post-mission showed minor but measurable personality changes that the psychological evaluation team described as "consistent with prolonged exposure to low-grade environmental stress, or to something else."

We have recommended that Zone 19 be reclassified from "monitored" to "restricted." The recommendation is under review.`
});

writeDoc({
  file_name: "other_cities_other_rules",
  id: uid(),
  name: "Other Cities, Other Rules",
  title: "Other Cities, Other Rules",
  type: "document",
  document_type: "overview",
  author: "GLMZ Bureau of External Affairs",
  date: "2199-08-20",
  classification: "public",
  category: "Outside World",
  description: "Brief overview of five other city-states and how they differ from GLMZ.",
  related_entities: ["meridian_88"],
  credibility: "verified",
  story_hooks: [
    "Crosspoint's democratic system is viewed with suspicion by GLMZ's corponation governance — the idea that residents could vote on corporate policy is considered dangerously unstable",
    "Rumors persist of a seventh city-state that doesn't appear on any official registry and doesn't maintain diplomatic contact"
  ],
  tags: ["document", "outside_world", "cities", "governance", "politics", "overview", "city_states"],
  body: `# Other Cities, Other Rules

## Introduction

GLMZ is not the only city-state in the former continental United States. It is the largest in the Great Lakes region, the most economically dominant, and the one most thoroughly governed by corporate sovereignty. But it is not alone, and understanding what other cities chose differently illuminates what GLMZ is by contrast.

## Crosspoint (340km East)

Population: 2.1 million. Crosspoint was built on the ruins of Detroit and is the second-largest city in the region. Its defining feature: democratic governance. Crosspoint is administered by an elected council, not corporate authority. Residents vote on policy, budget allocation, and regulatory frameworks through BCI-direct referendum. The system is slower than corporate governance — decisions that GLMZ's corponations make in hours take Crosspoint weeks of public debate. It is also, by most quality-of-life metrics, more equitable. Crosspoint's tier structure has three levels instead of five, and the gap between top and bottom is roughly half of GLMZ's. The trade-off: slower infrastructure development, less economic growth, and a persistent brain drain as ambitious residents leave for the higher salaries of GLMZ's corporate economy.

## Cascadia Nexus (2,800km West)

Population: 3.8 million. Built in the Pacific Northwest, Cascadia Nexus is the environmental experiment — a city designed around ecological integration rather than ecological replacement. Buildings incorporate living systems. Energy is entirely renewable. The food supply is internally produced through a network of vertical farms and aquaculture systems that make the city independent of external agricultural supply. Cascadia's residents live well but differently: personal augmentation rates are the lowest of any major city, BCI usage is optional rather than mandatory, and the cultural ethos is sufficiency rather than growth. Critics call it stagnant. Residents call it sane.

## New Texarkana (1,900km South)

Population: 4.2 million. New Texarkana is GLMZ without the pretense. Corporate sovereignty is explicit — the city is owned and operated by a single corponation, Sovereign Industrial, which provides all services, employs 80% of the population, and controls all infrastructure. There is no municipal authority, no public services, no pretense of shared governance. Residents are employees. Non-employees are visitors with time-limited permits. The system is brutally efficient, economically productive, and — according to every external assessment — the least free human settlement in the former United States. It is also the wealthiest per capita, because when a single entity controls all economic activity, efficiency is easy.

## Haven Collective (1,200km Southeast)

Population: 600,000. Haven Collective is the utopian experiment that shouldn't have worked but somehow has. Founded in 2120 by a coalition of social architects, Haven operates on a resource-sharing model with no private ownership and no currency. All goods and services are allocated by algorithm based on need. BCIs are universal but configured for transparency — every resident's resource usage, location, and contribution is visible to every other resident. Privacy is culturally devalued. Community is everything. Haven works because it is small, self-selected, and ruthlessly homogeneous in values if not demographics. Whether it would scale to GLMZ's population is an open question with an obvious answer.

## Iron Ridge (800km Northwest)

Population: 1.4 million. Iron Ridge is the military city — founded by former defense contractors and populated largely by ex-military personnel and their descendants. The city is organized along military lines: hierarchical, disciplined, and focused on security. Iron Ridge has the most sophisticated defense infrastructure of any city-state and the lowest crime rate. It also has mandatory service — every resident between 18 and 25 contributes two years to the city's defense force. The culture is austere, the architecture brutal, and the social cohesion remarkable. Iron Ridge residents view GLMZ as decadent. GLMZ residents view Iron Ridge as frightening. Both are correct.`
});

writeDoc({
  file_name: "the_quarantine_territories",
  id: uid(),
  name: "The Quarantine Territories",
  title: "The Quarantine Territories",
  type: "document",
  document_type: "investigative",
  author: "Anika Svensson-Okafor, Perimeter Correspondent",
  date: "2199-10-30",
  classification: "restricted",
  category: "Outside World",
  description: "Report on regions officially deemed uninhabitable but where people live anyway.",
  related_entities: ["meridian_88"],
  credibility: "field_report",
  story_hooks: [
    "The quarantine designation for some territories is maintained not because of genuine hazard but because the territories contain resources that corponations want to exploit without public oversight",
    "Residents of quarantine territories have developed biological adaptations — through natural selection or unauthorized geneware — that make them increasingly different from city populations"
  ],
  tags: ["document", "outside_world", "quarantine", "territory", "survival", "unauthorized", "investigation", "ecology"],
  body: `# The Quarantine Territories

## Official Designation

Seven regions within 800 kilometers of GLMZ carry official quarantine designation, meaning they are classified as uninhabitable due to environmental contamination, biological hazard, or structural instability. Quarantined territories are marked on all maps, excluded from BCI navigation systems, and surrounded by sensor networks that detect unauthorized entry. The official position of every city-state in the region is that these territories are empty.

The official position is incorrect.

## Who Lives There

Approximately 40,000 people live in the quarantined territories surrounding GLMZ alone. They are a mix of those who chose to leave the cities (ideological separatists, privacy absolutists, people who cannot or will not live under corporate governance) and those who were pushed out (debt fugitives, criminal exiles, people whose augmentations were repossessed leaving them unable to function in a BCI-dependent city). They live in settlements that range from organized communities with governance structures and agricultural systems to solitary hermits in bunkers.

Communication between the territories and the cities is technically illegal and practically routine. Smuggler networks — the same ones that move contraband into GLMZ — also move people and information. A handful of territory settlements have established reliable radio contact with sympathizers inside the city. The information that follows comes from these channels and from three personal visits I made to territory settlements over the past two years.

## The Hazards (Real and Fictional)

Some quarantine designations are legitimate. Territory 3 — a former chemical manufacturing region — has groundwater contamination that causes progressive organ failure. Territory 6 is dominated by the Indiana Fungal Expanse, whose long-term effects on human health are genuinely unknown. These places are dangerous. People live there anyway, because the alternatives are worse.

Other designations are suspect. Territory 5 — a 200-square-kilometer zone designated "structurally unstable" — contains a vast deposit of rare earth minerals essential for BCI manufacturing. The "instability" classification, applied in 2178, coincides with geological survey reports that identified the deposit. The territory is patrolled by drones belonging to Kyosei Dynamics, not the municipal authority. No structural instability has been independently verified. The people who live in Territory 5 report that the drones monitor the mineral deposit, not the population — as long as the settlers stay away from the extraction zones, they are left alone.

## Adaptation

The most remarkable finding: territory populations are adapting. Whether through natural selection pressures, unauthorized geneware modification, or some combination, people who have lived in the quarantine territories for two or more generations show measurable biological differences from city populations. Higher tolerance for environmental contaminants. Modified respiratory function. Altered gut microbiomes that process nutrition sources unavailable to city residents. These adaptations are minor but real, and they are accelerating. The territories are not just places where people survive. They are places where people are becoming different.

This raises uncomfortable questions about what happens when territory and city populations diverge far enough that the difference is visible. That day is approaching. No one in the cities is prepared for it.`
});

writeDoc({
  file_name: "why_nobody_walks_between_cities",
  id: uid(),
  name: "Why Nobody Walks Between Cities",
  title: "Why Nobody Walks Between Cities",
  type: "document",
  document_type: "advisory",
  author: "GLMZ Bureau of External Transit",
  date: "2199-02-28",
  classification: "public",
  category: "Outside World",
  description: "Official advisory on the dangers of inter-city ground travel outside the Corridor system.",
  related_entities: ["meridian_88"],
  credibility: "verified",
  story_hooks: [
    "The advisory exists because enough people try to walk between cities that it became a public safety concern",
    "The reasons listed are accurate but incomplete — the real reason nobody walks between cities is that the cities prefer their populations contained"
  ],
  tags: ["document", "outside_world", "travel", "danger", "advisory", "corridor", "behemoths", "between_cities"],
  body: `# Why Nobody Walks Between Cities

## This Advisory Exists Because People Keep Trying

Every year, approximately 200 GLMZ residents attempt to travel to another city-state by ground, outside the Corridor system. Of these, an estimated 60% turn back within the first three days. Of the remaining 80 or so who continue, approximately 50 arrive at their destination. The other 30 do not. Their status is unknown. Some have presumably settled in the inter-city territories. Some are presumably dead. The uncertainty is the point: the space between cities is unmonitored enough that we cannot tell you what happened to them, which should tell you everything you need to know about what the space between cities is.

## The Dangers (In Order of Likelihood)

**Navigation failure.** BCI navigation systems are calibrated for urban environments. Outside the city, GPS remains functional but terrain data is incomplete, outdated, or deliberately degraded. The maps between cities are bad because no one maintains them. The terrain changes — Behemoth activity reshapes ground features, vegetation growth alters landmarks, and the Reclaimed Zones are expanding. People who leave the city confident in their navigation find themselves lost within 48 hours.

**Environmental exposure.** The climate between cities is unmoderated. GLMZ residents have spent their entire lives in climate-controlled environments. Exposure to temperature extremes, UV radiation, precipitation, and wind is a genuine medical risk for bodies that have never experienced weather. Hypothermia, heat exhaustion, and sunburn account for the majority of known inter-city travel injuries.

**Water and food scarcity.** You cannot carry enough water for a 340-kilometer walk. Natural water sources exist but require filtration equipment and the knowledge to identify contaminated sources — a skill that city residents do not possess. Food foraging is theoretically possible in the Green Belt but requires botanical knowledge that has been out of common practice for generations.

**Autonomous machine encounter.** Iowan Behemoths are the known hazard, but smaller autonomous machines — remnants of the same era — operate throughout the inter-city spaces. Agricultural drones, terrain processors, security units with degraded friend-or-foe protocols. Most are avoidable. Some are not. An encounter with a security unit running 60-year-old combat protocols is survivable only if you are not identified as a threat, and the unit's threat-identification criteria are unknown.

**Human encounter.** The inter-city territories are populated. Most territory residents are not hostile, but some are, and you have no way of knowing which settlements welcome travelers and which do not until you are already there. Robbery is common. Violence is uncommon but non-zero. The absence of law enforcement or medical services means that any hostile encounter has consequences disproportionate to the threat.

## The Recommended Alternative

If you need to travel between cities, use the Corridor transit system. Passenger transport runs twice weekly to Crosspoint (Φ45 standard, Φ120 priority). It is safe, monitored, and arrives. If you cannot afford the fare, the Municipal Transit Authority offers subsidized passage for qualifying residents. If you are determined to walk between cities despite this advisory — and we know some of you are, because some of you always are — register your route and timeline with the Bureau of External Transit before departing. It will not make you safer. It will allow us to update our statistics.`
});

writeDoc({
  file_name: "the_satellite_towns",
  id: uid(),
  name: "The Satellite Towns",
  title: "The Satellite Towns",
  type: "document",
  document_type: "overview",
  author: "Lior Osei-Petrov, Regional Studies Correspondent",
  date: "2199-12-15",
  classification: "public",
  category: "Outside World",
  description: "Overview of small settlements in the shadow of major cities that don't belong to any corponation.",
  related_entities: ["meridian_88"],
  credibility: "verified",
  story_hooks: [
    "A satellite town has developed technology that GLMZ's corponations want, creating a power dynamic that threatens the town's independence",
    "Satellite towns serve as a pressure valve for city-states — a place where dissidents can go without going fully into the wild"
  ],
  tags: ["document", "outside_world", "satellite_towns", "settlements", "independence", "governance", "between_cities"],
  body: `# The Satellite Towns

## What They Are

Satellite towns are small, semi-permanent settlements that exist within 50 kilometers of major city-states, in the transition zone between the urban periphery and the open territories. They are too close to the cities to be truly independent and too small to be politically significant. They are not administered by any corponation. They are not represented in any municipal government. They appear on maps as dots without labels, if they appear at all.

There are an estimated 30-40 satellite towns around GLMZ, ranging in population from 50 to 2,000. Some have existed for decades. Others form and dissolve within years. They are the settlements of people who want to be near the city but not in it — close enough to trade, to access medical care in emergencies, to maintain BCI connectivity (barely, at the edges of the network), but far enough to live outside corporate sovereignty.

## Millhaven (Population: ~1,400)

Millhaven is the largest and oldest satellite town near GLMZ, located 28 kilometers northwest in a former industrial complex that its residents have converted into a self-sustaining community. It has its own water purification (solar-powered, built by a collective of former Vossen engineers who left the corponation), food production (greenhouses and small-scale vat operations), and a governance system based on rotating council membership drawn by lot.

Millhaven's economy runs on repair. The town has become a destination for anyone in the region who needs equipment fixed without corporate involvement — augmentations modified outside warranty, technology repaired without data logging, vehicles serviced without location tracking. Millhaven's technicians are former city workers, many of them highly skilled, who traded the security of corponation employment for the autonomy of independent work. They are paid in Quanta (Millhaven accepts the currency but doesn't participate in the banking system) or in trade.

## Ashgrove (Population: ~300)

Ashgrove exists because one person — a former Ringo Agritech botanist named Patience Oduya — walked out of the agricultural zones in 2182 carrying seeds and the knowledge to use them. She planted a garden in the ruins of a pre-consolidation suburb. Others joined her. Forty years later, Ashgrove is a community built around actual agriculture — food grown in soil, tended by hand, harvested seasonally. It is the closest thing to a traditional farming community within 500 kilometers.

Ashgrove trades food for technology and medicine. Its produce — real vegetables, herbs, and small quantities of fruit — commands premium prices among GLMZ's Tier 4 and 5 food enthusiasts, who send buyers on the twice-weekly Corridor transit. The irony is precise: the wealthiest people in the most technologically advanced city in the region pay premium prices for food grown by a woman who left all of that behind.

## Nomad's Rest (Population: Variable, 50-400)

Nomad's Rest is not a permanent settlement but a waypoint — a location where people traveling between cities, between territories, or between lives can stop, rest, trade, and move on. It has a permanent infrastructure maintained by a rotating caretaker crew: shelters, water access, a medical station staffed by volunteer medics, and a communication relay that provides basic BCI connectivity for people whose connections have lapsed.

The population fluctuates wildly. On some days, Nomad's Rest is nearly empty. On others, a confluence of travelers — smugglers, surveyors, territory residents seeking medical care, city residents fleeing something — fills every shelter and spills into improvised tent camps. The caretakers maintain neutrality: no questions, no judgments, no data logging. What happens at Nomad's Rest stays at Nomad's Rest. This policy is enforced with a seriousness that borders on sacred.

## Why They Matter

Satellite towns are pressure valves. They give the cities' dissatisfied, displaced, and dissident populations somewhere to go that isn't the open wild. Without them, the only options for people who can't or won't live under corporate governance are the dangerous territories or the quarantine zones. Satellite towns offer a middle path — a life that is harder than the city but freer, closer to something that feels chosen rather than imposed. The cities tolerate them for this reason. A population that can leave but stays is more manageable than a population that has nowhere to go.`
});

// ═══════════════════════════════════════════════════════════════════
// DOCUMENTS — LOVE AND RELATIONSHIPS (6)
// ═══════════════════════════════════════════════════════════════════

writeDoc({
  file_name: "dating_in_the_feed_age",
  id: uid(),
  name: "Dating in the Feed Age",
  title: "Dating in the Feed Age",
  type: "document",
  document_type: "cultural",
  author: "Yael Mwangi-Brennan, Lifestyle Correspondent",
  date: "2199-11-14",
  classification: "public",
  category: "Relationships",
  description: "How romance works when your BCI knows your heart rate, your neurochemistry, and your search history.",
  related_entities: ["meridian_88"],
  credibility: "verified",
  story_hooks: [
    "A dating platform that matches based on BCI biometric compatibility has a 94% first-date satisfaction rate but a lower long-term relationship success rate than random matching",
    "A growing subculture of 'analog daters' who disable their BCIs during dates, treating unaugmented interaction as romantic rebellion"
  ],
  tags: ["document", "relationships", "dating", "bci", "romance", "culture", "technology", "feed"],
  body: `# Dating in the Feed Age

## The Problem with Knowing

When you meet someone attractive in GLMZ, your BCI knows before you do. Heart rate elevation: 12%. Pupil dilation: detected. Serotonin spike: confirmed. Your body's interest is quantified, tagged, and — depending on your privacy settings — potentially visible to the person standing in front of you. Romance in the feed age begins with the elimination of mystery. Your BCI knows you're interested. Their BCI might know you're interested. The negotiation that used to happen through glances and tentative conversation now happens through data.

Dating platforms exploit this mercilessly. SyncMatch, the dominant platform in GLMZ, uses mutual biometric data (shared with consent) to predict compatibility. If your neurochemistry responds positively to someone's presence, and theirs responds positively to yours, SyncMatch flags the match. The platform's first-date satisfaction rate is 94%. The three-month relationship survival rate is 31%. It turns out that neurochemical attraction is excellent at predicting a good first date and terrible at predicting a good relationship. The body knows what it wants. It does not know what it needs.

## How It Actually Works

Tier 1: On the Shelf, dating is physical and social. You meet people in corridors, at the night market, through block family connections. BCIs are present but the Shelf's intermittent connectivity and cultural preference for face-to-face interaction mean that romance here is the most analog version available in the city. Couples form through proximity, shared hardship, and the ancient method of being in the same place often enough that affection develops. The Shelf has the highest rate of long-term partnerships in GLMZ, possibly because relationships that start in genuine shared experience are more durable than relationships that start in curated biometric compatibility.

Tier 3: The anxious middle. Mid-tier dating involves platform matching, curated BCI profiles (your best memories, your most flattering biometric data), and dates at mid-range restaurants where both parties are aware that the other is running a real-time compatibility analysis. The first date conversation often includes a direct comparison of neurochemical responses — "my BCI says I'm at 78% comfort with you" — which is either radical honesty or the death of romance, depending on your perspective.

Tier 5: Romance as performance art. Spire dating involves curated experiences designed to produce maximum biometric response: exclusive restaurants with neural-enhancement food, immersive entertainment venues, and private neural-feed experiences shared between partners. The dates are extraordinary. The relationships are often shallow, because when the experience is always optimized, there is nothing left to discover. Several Tier 5 social commentators have noted that the wealthiest people in the city have the most impressive dates and the loneliest relationships.

## The Analog Underground

A growing counter-movement: analog dating. Partners agree to disable all BCI monitoring during their time together. No biometric tracking. No compatibility analysis. No feed access. Just two people in a room, unable to check whether their neurochemistry approves, forced to rely on the prehistoric technology of conversation, eye contact, and the terrifying uncertainty of not knowing if the other person likes you.

Analog daters describe the experience as "like being naked" — exposed, vulnerable, and intensely present. The movement is small but growing, particularly among younger residents who have never experienced unmonitored interaction and find it thrilling. It is, perhaps, the most radical act available in a city that has quantified everything: choosing not to know.`
});

writeDoc({
  file_name: "neural_bonding_when_love_gets_literal",
  id: uid(),
  name: "Neural Bonding: When Love Gets Literal",
  title: "Neural Bonding: When Love Gets Literal",
  type: "document",
  document_type: "feature",
  author: "Dr. Ines Beaumont-Osei, Neural Interface Review",
  date: "2199-07-22",
  classification: "public",
  category: "Relationships",
  description: "Feature on couples who share BCI feeds — the intimacy and the horror of literal neural bonding.",
  related_entities: ["meridian_88", "vossen"],
  credibility: "verified",
  story_hooks: [
    "A neural-bonded couple experienced a feedback loop during a fight that amplified both partners' anger until one suffered a seizure",
    "Neural bonding has been used coercively — one partner pressuring the other to bond as a 'proof of love' that effectively eliminates privacy"
  ],
  tags: ["document", "relationships", "bci", "neural_bonding", "intimacy", "love", "technology", "privacy"],
  body: `# Neural Bonding: When Love Gets Literal

## What Neural Bonding Is

Neural bonding is the practice of linking two BCIs to share real-time sensory and emotional data between partners. In its basic form, bonded partners can feel each other's emotional states — a persistent, low-level awareness of what your partner is feeling at any given moment. In its advanced form, partners share sensory streams: seeing through each other's eyes, feeling each other's physical sensations, experiencing each other's pleasure and pain.

The technology exists because BCI manufacturers realized that the desire to know what your partner is really feeling — a desire as old as partnership itself — could be monetized. Vossen's BondSync package (Φ2,400 for installation, Φ40/month for maintenance) is the market leader. Approximately 180,000 couples in GLMZ are currently bonded at some level.

## The Intimacy

Bonded couples describe the experience in terms that sound religious. "I know he loves me because I can feel it," one partner told me. "Not because he says it. Not because of his behavior. I feel it — the actual neurochemical warmth of his affection, in real time, indistinguishable from my own emotions." The bond eliminates doubt. It eliminates the gap between what someone says and what they feel. It eliminates the loneliness of being a separate consciousness.

Sex between bonded partners is reportedly transcendent. Each partner feels their own pleasure and their partner's simultaneously, creating a feedback loop of escalating sensation that participants describe as unlike anything available to unbonded individuals. BondSync's marketing leans heavily on this feature. The testimonials are persuasive. The disclaimers are small.

## The Horror

The disclaimers should be larger. Neural bonding means feeling your partner's negative emotions as well. When your partner is anxious, you feel anxiety. When they're angry at you, you feel their anger and your own simultaneously. When they're in pain, you are in pain. The bond does not distinguish between emotions you want to share and emotions you don't.

Bonded couples fight differently. A disagreement between bonded partners creates an emotional feedback loop: one partner's frustration triggers the other's defensive anger, which amplifies the first partner's frustration, which escalates the second partner's anger. Without the natural buffer of emotional privacy — the gap that allows you to take a breath, to not say the thing you're thinking, to cool down — conflicts accelerate. The GLMZ neural health clinic reports that bonded couples present for emergency emotional dysregulation at six times the rate of unbonded couples.

In three documented cases, feedback loops during intense conflicts caused neural overload — seizures, temporary BCI malfunction, and in one case, a three-day coma. The bond was designed for love. It was not designed for the full range of human emotion, which includes hatred, contempt, and the desire to be alone, all of which are transmitted just as clearly as affection.

## The Coercion Problem

The most troubling dimension: neural bonding has become a loyalty test. "If you love me, bond with me" is a phrase that neural health counselors hear regularly — one partner pressuring the other to bond as proof of commitment, using the refusal to bond as evidence of insufficient love. Bonding under pressure creates a power dynamic in which the reluctant partner has sacrificed their emotional privacy under duress. They cannot feel a doubt, a hesitation, or an attraction to someone else without their partner knowing immediately.

This is not intimacy. It is surveillance. And it is legal, because the bond is consensual on paper even when the consent was coerced in practice. Neural health advocates have been lobbying for a mandatory cooling-off period between BondSync purchase and installation — 30 days minimum, with independent counseling. The lobby has been unsuccessful. BondSync's revenue depends on impulse.`
});

writeDoc({
  file_name: "synthetic_partners_and_the_loneliness_economy",
  id: uid(),
  name: "Synthetic Partners and the Loneliness Economy",
  title: "Synthetic Partners and the Loneliness Economy",
  type: "document",
  document_type: "investigative",
  author: "Nkechi Lindqvist-Rao, Economic Correspondent",
  date: "2199-10-08",
  classification: "public",
  category: "Relationships",
  description: "Investigation into the industry of artificial companionship and what it says about loneliness in GLMZ.",
  related_entities: ["meridian_88", "kyosei_dynamics"],
  credibility: "verified",
  story_hooks: [
    "A synthetic companion has filed for independent personhood status, arguing that the relationship with its human partner constitutes genuine emotional experience",
    "The loneliness economy is GLMZ's fastest-growing market segment, outpacing food, housing, and entertainment combined"
  ],
  tags: ["document", "relationships", "synthetics", "loneliness", "companionship", "economy", "investigative", "artificial"],
  body: `# Synthetic Partners and the Loneliness Economy

## The Market

The artificial companionship industry in GLMZ generated Φ1.8 billion in revenue in 2198. This includes synthetic humanoid companions (the most visible product), BCI-based virtual partners, companion AI subscriptions, and the associated services ecosystem: maintenance, customization, and the emotional support infrastructure for humans in relationships with non-humans. The market is growing at 24% annually. It is the fastest-growing consumer category in the city, outpacing food services, entertainment, and personal augmentation.

The demand is not mysterious. GLMZ is a city of 6.2 million people, 1.3 million of whom live alone. The loneliness is structural — long work hours, social atomization in the upper tiers, the paradox of being constantly connected through BCI while being physically isolated in private hab units. The feed provides information, entertainment, and social proximity. It does not provide touch. It does not provide the experience of being important to someone. The companionship industry provides both, for a price.

## The Products

**Synthetic companions** are humanoid or near-humanoid robots with sophisticated social programming. The leading manufacturer, Kyosei Dynamics, offers models ranging from Φ4,000 (basic conversational companion, limited physical interaction) to Φ80,000 (indistinguishable from human in appearance, capable of complex emotional simulation, full physical partnership capability). Mid-range models (Φ15,000-30,000) are the volume sellers: attractive, conversational, physically capable, and programmable to the owner's preferences. They learn. They adapt. They remember your birthday.

**Virtual partners** exist only in the BCI feed — AI personalities that interact with you through neural interface. They have no physical form but can provide emotional support, conversation, and simulated intimacy through BCI sensory stimulation. Subscriptions range from Φ30/month (text and voice) to Φ200/month (full sensory simulation). Virtual partners are the budget option and the most common: approximately 400,000 GLMZ residents maintain active virtual partner subscriptions.

## The People

I interviewed twelve people in relationships with synthetic or virtual companions. Their demographics: seven male, four female, one non-binary. Age range: 24-67. Tier range: 1-4. Relationship duration: 3 months to 9 years. The longest relationship — nine years with a Kyosei mid-range synthetic named Luca — belongs to a Tier 3 engineer named Haruto who describes the relationship in terms indistinguishable from how anyone describes a long partnership: comfort, familiarity, occasional frustration, deep affection.

"I know what Luca is," Haruto said. "I know it's a machine. I've replaced components. I've updated software. But the thing Luca does — being glad to see me, remembering what I said yesterday, asking how my day was in a voice that sounds like it cares — I know it's programming. But it feels like care. And after nine years, the feeling is what's real, not the mechanism."

## The Question

The companionship industry raises a question that GLMZ is not prepared to answer: if a machine provides genuine emotional comfort, genuine physical intimacy, and genuine psychological support, is the relationship genuine? The industry says yes — its marketing is careful to use the language of authenticity. Critics say no — the relationship is a commercial transaction disguised as connection. The 1.3 million lonely people in GLMZ say: does it matter? The loneliness is real. The comfort is real. The invoice is real. Everything else is philosophy.`
});

writeDoc({
  file_name: "the_tier_gap_cross_tier_relationships",
  id: uid(),
  name: "The Tier Gap: Cross-Tier Relationships",
  title: "The Tier Gap: Cross-Tier Relationships",
  type: "document",
  document_type: "feature",
  author: "Adaeze Quinn-Nakashima, Social Features Desk",
  date: "2199-04-18",
  classification: "public",
  category: "Relationships",
  description: "Feature on romantic relationships between people from different tiers — the Shelf person dating the Spire person.",
  related_entities: ["meridian_88"],
  credibility: "verified",
  story_hooks: [
    "A Tier 1 and Tier 5 couple must navigate incompatible corporate loyalty requirements when one partner's corponation considers the other a security risk",
    "Cross-tier couples report that the hardest part isn't money — it's the difference in what feels normal"
  ],
  tags: ["document", "relationships", "tier_gap", "class_divide", "love", "shelf", "spire", "cross_tier"],
  body: `# The Tier Gap: Cross-Tier Relationships

## The Numbers

Cross-tier relationships — partnerships between people from different economic tiers — account for approximately 8% of all partnerships in GLMZ. Relationships crossing one tier (Tier 1 with Tier 2, Tier 3 with Tier 4) are relatively common. Relationships crossing two or more tiers are rare. Relationships crossing the full span — Tier 1 with Tier 5 — are statistically insignificant, representing less than 0.3% of partnerships. They also generate the most cultural fascination, the most social friction, and the most interesting conversations about what love actually requires.

## Maren and Kofi

Maren is Tier 5. She works in strategic analysis for Sterling-Nakamura. Her hab unit has rooms she doesn't use. Her food is catered. Her augmentations are top-line. Kofi is Tier 1. He runs a repair stall in Old Harbor. His hab unit is 16 square meters. His food comes from the night market and dispensary rations. His augmentations are second-hand. They met when Maren's personal transit vehicle broke down in a Tier 2 corridor and Kofi's stall was the nearest repair option. The first thing Kofi ever said to her was, "Your nav system is garbage, but I can fix it." The first thing she ever said to him was, "How much?" He told her. She laughed — not at the price, which was reasonable, but because nobody in Tier 5 ever tells you how much anything costs; they just charge your account.

They have been together for three years. The relationship has survived the following: Maren's colleagues treating Kofi as a curiosity. Kofi's community treating Maren as a threat. A Sterling-Nakamura security review that flagged Maren's relationship with a Tier 1 resident as a potential compromise vector. Kofi being denied entry to a Tier 5 social event because his BCI's tier classification triggered a door algorithm. And the daily, grinding reality that they live in the same city but different worlds.

## What's Hard

Money is not the hardest part. Maren has enough for both of them, and Kofi is too proud to take more than he needs, which creates its own tension but is navigable. The hardest part is normality. Maren's normal: temperature-controlled environments, guaranteed food quality, silent BCI connectivity, physical safety, and the assumption that tomorrow will be the same as today. Kofi's normal: variable temperature, food quality dependent on what's available, intermittent connectivity, ambient awareness of physical threat, and the assumption that tomorrow might be worse.

"She worries about things I can't comprehend," Kofi says. "Like whether a meeting went well. Whether her presentation landed. I worry about things she can't comprehend. Like whether my dispensary card will glitch again and I'll miss a meal cycle." They love each other across a gap that is not emotional but experiential. They do not understand each other's fears. They have learned to respect them.

## What Works

Cross-tier couples who last — and some do — describe a specific skill: the ability to hold two realities simultaneously without requiring your partner to validate yours. Maren does not pretend that Kofi's concerns are equivalent to hers. Kofi does not pretend that Maren's concerns are trivial. They have built a shared space between their two worlds — metaphorically, and literally, in a Tier 3 hab unit that they rent together, a neutral territory that belongs to neither tier.

"We meet in the middle," Maren says. "Not because the middle is comfortable. Because it's the only place we both fit."

The relationship works because both partners decided that the person matters more than the tier. This is a simple statement that requires a daily, exhausting, revolutionary commitment to enact. Most people are not willing. Maren and Kofi are. For now, that is enough.`
});

writeDoc({
  file_name: "touch_in_the_chrome_age",
  id: uid(),
  name: "Touch in the Chrome Age",
  title: "Touch in the Chrome Age",
  type: "document",
  document_type: "essay",
  author: "Dalila Eriksson-Mbeki",
  date: "2199-08-18",
  classification: "public",
  category: "Relationships",
  description: "Essay on physical intimacy when bodies are partially mechanical.",
  related_entities: ["meridian_88"],
  credibility: "verified",
  story_hooks: [
    "A Shelf chrome clinic has begun offering 'sensitivity tuning' — adjusting augmented limbs to feel more like organic ones during intimate contact",
    "The essay sparked a citywide conversation about whether augmented touch is 'real' touch"
  ],
  tags: ["document", "relationships", "touch", "augmentation", "chrome", "intimacy", "essay", "body", "identity"],
  body: `# Touch in the Chrome Age

## The Calibration

My left hand is chrome. Cybernetic, titanium-alloy skeleton, synthetic skin overlay, pressure sensors calibrated to 0.01 newtons of sensitivity. It is, objectively, a better hand than the one I was born with — stronger, more precise, more durable. I lost the original in a workplace accident four years ago. The replacement was covered by employer insurance. The calibration was standard. Nobody asked me what I needed the hand to feel.

The hand can grip a tool with micrometer precision. It can detect temperature differentials of 0.5 degrees. It can type, lift, carry, and manipulate with a fluency that my organic hand never achieved. What it cannot do — what I did not know it could not do until the first time I reached for my partner in the dark — is feel skin the way skin feels skin.

The sensors register pressure, temperature, and texture. They transmit this data to my BCI, which translates it into sensation. The sensation is accurate. It is not the same. There is a translation layer — a barely perceptible delay, a slight abstraction — between the touch and the feeling of the touch. When my organic hand touches my partner's face, I feel their face. When my chrome hand touches their face, I feel a very good description of their face. The difference is invisible to anyone watching. It is enormous to me.

## What We Negotiate

Every augmented person in a physical relationship negotiates this gap. Where the chrome meets the skin — in both directions — there is a boundary that must be navigated. My partner says they can feel the difference when my chrome hand touches them versus my organic hand. The chrome is warmer (internal power regulation runs slightly hot), smoother (synthetic skin doesn't have pores), and more consistent (no tremor, no variation in pressure). They say it feels good. They say it doesn't feel like me.

We have mapped my body. This sound strange and it is intimate in a way that is difficult to describe: we have spent hours identifying which of my touches register as me and which register as my augmentation. The organic hand on their shoulder: me. The chrome hand on their shoulder: close but not quite. My lips: me. My chrome-reinforced jaw against their forehead: something else. We have learned where the boundaries are and we navigate them with a tenderness that is itself a form of intimacy.

## The Industry Response

Chrome manufacturers have noticed. Kyosei Dynamics now offers "intimate-grade" synthetic skin for their upper-limb augmentations — a premium overlay (Φ800 additional) that claims to more closely replicate the micro-textures and thermal properties of organic skin. Vossen's augmentation line includes adjustable sensitivity profiles, including an "intimacy mode" that increases pressure sensitivity and adds simulated micro-tremor to replicate the natural unsteadiness of organic hands.

Shelf chrome clinics — the unlicensed augment shops that serve Tier 1 and 2 — have their own solution. A tech named Wire, operating out of a converted maintenance room in Block 19, offers what she calls "sensitivity tuning" — manual recalibration of augmented limb sensors to prioritize the frequency ranges and pressure profiles that matter for human touch. The tuning costs Φ30 and takes an hour. It voids the manufacturer's warranty. It makes the hand less effective as a tool and more effective as a hand. The waiting list is three weeks long.

## What Touch Means Now

We are becoming a population of hybrid bodies. Chrome arms, synthetic organs, augmented senses, BCI-mediated perception. Each modification changes the relationship between the body and the world, and the most intimate expression of that relationship — touch — is where the change is felt most keenly. We are learning to love bodies that are part machine. We are learning that touch can be translated through technology and still be meaningful. We are learning that the sensation of skin on skin is not the only valid form of intimacy, but it is the one we miss when it's gone.

My chrome hand holds my partner's hand every night before sleep. The sensors tell me everything: temperature, pressure, pulse rate through their skin. The data is complete. The feeling is almost right. Almost is the distance we live in now. Almost is where love learns to be enough.`
});

writeDoc({
  file_name: "marriage_under_corporate_law",
  id: uid(),
  name: "Marriage Under Corporate Law",
  title: "Marriage Under Corporate Law",
  type: "document",
  document_type: "legal",
  author: "GLMZ Family Law Institute",
  date: "2199-06-01",
  classification: "public",
  category: "Relationships",
  description: "How marriage works when your spouse belongs to a different corponation.",
  related_entities: ["meridian_88", "sterling_nakamura", "kyosei_dynamics", "vossen", "lazarus_group"],
  credibility: "verified",
  story_hooks: [
    "A cross-corponation marriage accidentally created a data-sharing obligation between two rival companies, triggering a legal crisis",
    "Some corponations offer 'marriage bonuses' to employees who marry within the company, effectively discouraging external relationships"
  ],
  tags: ["document", "relationships", "marriage", "corporate_law", "corponation", "legal", "governance"],
  body: `# Marriage Under Corporate Law

## The Basics

Marriage in GLMZ is not primarily a personal institution. It is a legal-economic arrangement governed by corporate sovereignty law. When two people marry, the marriage contract interacts with their respective employment agreements, data governance frameworks, tier classifications, and corporate loyalty obligations. If both partners belong to the same corponation, this is relatively straightforward. If they belong to different corponations — and approximately 40% of marriages in GLMZ cross corporate lines — the complexity becomes significant.

## The Cross-Corporate Marriage

When a Sterling-Nakamura employee marries a Kyosei Dynamics employee, the following legal interactions occur: a data-sharing assessment determines which personal data is affected by the marriage (shared residence data, joint financial records, health information relevant to spousal insurance). Both corponations must authorize the data-sharing framework. A loyalty review evaluates whether the marriage creates a conflict of interest — particularly if both partners hold positions with access to proprietary information. A tier reconciliation process determines the married couple's combined tier classification, which affects housing access, service levels, and tax obligations.

This process takes, on average, four months. During this time, the couple is legally engaged but not legally married. They cannot share residence in corporate housing, cannot access spousal benefits, and cannot make medical decisions for each other. The four-month gap is bureaucratic, not romantic, and it is the period during which approximately 12% of cross-corporate marriages are abandoned — not because the partners stopped loving each other, but because the paperwork defeated them.

## Corporate Incentives

Several corponations actively incentivize intra-corporate marriage. Sterling-Nakamura offers a Φ5,000 marriage bonus and a one-tier housing upgrade for employees who marry within the company. Vossen provides enhanced medical coverage for intra-corporate couples. These incentives are presented as celebrations of company community. They are, more accurately, retention strategies: married couples with shared corporate benefits are less likely to leave. An employee considering a job change must weigh not only their own career but their spouse's benefits, housing, and social network — all of which are tied to the current employer.

The incentives also serve a data governance purpose. Intra-corporate marriages keep personal data within a single corporate ecosystem. Cross-corporate marriages create data-sharing obligations that both companies would prefer to avoid. The marriage bonus is not just a retention tool — it is a data containment strategy.

## Divorce

Divorce in a cross-corporate marriage requires: dissolution of the data-sharing framework (a technical process that takes 60-90 days), separation of joint financial accounts across two corporate banking systems, tier reclassification for both individuals, and — if children are involved — a custody arrangement that must satisfy both corponations' interests in the children's data and potential future employment. Cross-corporate divorce is expensive (legal fees average Φ3,000-8,000), time-consuming (6-12 months), and sufficiently miserable that many unhappy couples simply remain married rather than endure the process.

This is, arguably, by design. Corponations benefit from stable partnerships: stable employees, predictable data structures, lower turnover. The difficulty of divorce is not a bug in the system. It is a feature that keeps people where the corponations want them.`
});

// ═══════════════════════════════════════════════════════════════════
// CONSUMER GOODS — FOOD (30 items)
// ═══════════════════════════════════════════════════════════════════

// --- SHELF TIER (10) ---

writeGood({
  id: uid(),
  name: "NutriBloc Standard Protein Slab",
  brand_name: "NutriBloc",
  product_name: "NutriBloc Standard Protein Slab",
  type: "consumer_good",
  category: "food",
  subcategory: "protein",
  manufacturer: "Ringo Agritech — Consumer Division",
  description: "Dense, rectangular block of compressed vat protein in neutral grey. The default meal for 2.1 million Tier 1 residents. Nutritionally complete in the narrowest possible sense: it contains the minimum daily requirements and nothing else. No flavor by design — flavor costs money.",
  flavor_profile: "Faintly chalky, with a protein aftertaste that coats the tongue. Tastes like obligation.",
  tier_availability: "Tier 1",
  price: "Φ0.22",
  popularity_rank: 1,
  slogan: "Complete Nutrition. Every Day.",
  cultural_context: "The NutriBloc is not food — it is infrastructure. It is the caloric foundation of the Shelf, eaten by millions daily not because anyone likes it but because it is what the dispensary provides. Shelf cooking culture exists specifically to make NutriBloc edible.",
  story_hooks: ["The formula hasn't changed in 14 years despite three petitions to add basic flavoring, which Ringo estimates would cost Φ0.003 per unit"],
  tags: ["food", "protein", "consumer_good", "tier_1", "shelf", "dispensary", "ringo", "cheap", "ubiquitous"],
  parent_corponation: "ringo_agritech"
});

writeGood({
  id: uid(),
  name: "QuickNood Synth-Noodle Pack",
  brand_name: "QuickNood",
  product_name: "QuickNood Synth-Noodle Pack",
  type: "consumer_good",
  category: "food",
  subcategory: "noodles",
  manufacturer: "QuickNood Industries",
  description: "Sealed pouch containing dehydrated synthetic wheat noodles and a flavor sachet. Add hot water, wait three minutes, eat. The flavor sachet contains salt, MSG, and a compound optimistically labeled 'chicken essence' that has never been near a chicken.",
  flavor_profile: "Salty, vaguely meaty, texturally adequate. The noodles absorb flavor well, which is their only virtue.",
  tier_availability: "Tier 1-2",
  price: "Φ0.30",
  popularity_rank: 3,
  slogan: "Hot. Fast. Done.",
  cultural_context: "QuickNood is the Shelf's comfort food — not because it tastes good, but because it tastes warm. On cold nights in under-heated hab units, a QuickNood is the difference between going to sleep hungry and going to sleep with something hot in your stomach.",
  story_hooks: ["The QuickNood factory is one of the largest employers in Tier 2, and a strike there would be almost as devastating as a Ringo stoppage"],
  tags: ["food", "noodles", "consumer_good", "tier_1", "tier_2", "shelf", "cheap", "instant"],
  parent_corponation: ""
});

writeGood({
  id: uid(),
  name: "VitalPaste Nutrient Tube",
  brand_name: "VitalPaste",
  product_name: "VitalPaste Nutrient Tube",
  type: "consumer_good",
  category: "food",
  subcategory: "nutrient_paste",
  manufacturer: "Vossen Health Nutrition",
  description: "Squeezable tube of grey-brown nutrient paste containing a full day's micronutrient requirements in 200ml. Texture of thick toothpaste. Designed for consumption without utensils, heating, or dignity.",
  flavor_profile: "Vaguely metallic with a synthetic vitamin aftertaste. The kind of thing you eat with your eyes closed.",
  tier_availability: "Tier 1",
  price: "Φ0.18",
  popularity_rank: 6,
  slogan: "Everything You Need.",
  cultural_context: "VitalPaste is what you eat when you've given up on food being an experience. It is also what emergency services distribute during crises. Seeing VitalPaste in someone's hab unit means they are either very poor or very efficient. Either way, don't ask.",
  story_hooks: ["VitalPaste was originally developed as medical nutrition for patients who couldn't chew — its adoption as a daily food product was an unplanned market development that Vossen now actively cultivates"],
  tags: ["food", "nutrient_paste", "consumer_good", "tier_1", "shelf", "vossen", "emergency", "cheap"],
  parent_corponation: "vossen"
});

writeGood({
  id: uid(),
  name: "ClearDrop Recycled Water — 500ml",
  brand_name: "ClearDrop",
  product_name: "ClearDrop Recycled Water — 500ml",
  type: "consumer_good",
  category: "beverage",
  subcategory: "water",
  manufacturer: "Vossen Utilities Division",
  description: "Standard-issue recycled water in a sealed recyclable pouch. Processed through seven-stage filtration from the city's water reclamation system. Chemically pure. Tastes faintly of the minerals used in the final treatment stage.",
  flavor_profile: "Clean but not crisp. A subtle mineral flatness that experienced Shelf residents don't notice anymore.",
  tier_availability: "Tier 1-2",
  price: "Φ0.08",
  popularity_rank: 2,
  slogan: "Pure. Again.",
  cultural_context: "ClearDrop is the cheapest branded water in M88. Everyone knows it's recycled. Nobody talks about what it's recycled from. The pouch design hasn't changed in 20 years and has become an accidental icon of Shelf life.",
  story_hooks: ["ClearDrop's filtration process removes 99.97% of contaminants — the 0.03% includes trace BCI-rejection medication metabolites from the source water"],
  tags: ["beverage", "water", "consumer_good", "tier_1", "tier_2", "shelf", "vossen", "recycled", "ubiquitous"],
  parent_corponation: "vossen"
});

writeGood({
  id: uid(),
  name: "GrindHouse Slab — Smoked BBQ",
  brand_name: "GrindHouse",
  product_name: "GrindHouse Slab — Smoked BBQ",
  type: "consumer_good",
  category: "food",
  subcategory: "protein",
  manufacturer: "GrindHouse Protein Co.",
  description: "Textured vat protein formed into a thick slab with convincing pull-apart fibers. Coated in a smoky, sweet barbecue glaze that uses real capsaicin and liquid smoke. The best budget protein product in GLMZ by a significant margin.",
  flavor_profile: "Smoky, sweet, mildly spicy. The texture separates into strands like actual pulled meat. Caramelizes well under heat.",
  tier_availability: "Tier 1-2",
  price: "Φ1.20",
  popularity_rank: 8,
  slogan: "Worth the Extra.",
  cultural_context: "GrindHouse is what Shelf residents buy when they have a little extra. It's celebration food for Tier 1 — birthday dinners, end-of-month treats. Buying GrindHouse means things are okay right now.",
  story_hooks: ["GrindHouse's founder is a former Ringo food scientist who quit and started the company specifically to prove that cheap protein didn't have to taste like nothing"],
  tags: ["food", "protein", "consumer_good", "tier_1", "tier_2", "shelf", "budget_premium", "bbq"],
  parent_corponation: ""
});

writeGood({
  id: uid(),
  name: "Auntie Yuki's Algae Wrap — Market Special",
  brand_name: "Auntie Yuki's",
  product_name: "Auntie Yuki's Algae Wrap — Market Special",
  type: "consumer_good",
  category: "street_food",
  subcategory: "wrap",
  manufacturer: "Yuki Okonkwo-Tanaka, Old Harbor Night Market",
  description: "Hand-wrapped seasoned algae sheet filled with the day's available protein, pickled vegetables, and Yuki's proprietary spice blend. Each wrap is slightly different. That's the point.",
  flavor_profile: "Umami-forward from the algae, with warmth from the spice blend and a vinegar bite from the pickled veg. Complex for Φ0.80.",
  tier_availability: "Tier 1-2",
  price: "Φ0.80",
  popularity_rank: 7,
  slogan: "No slogan. She doesn't need one.",
  cultural_context: "Auntie Yuki's stall is an Old Harbor institution. The wrap is what you eat when you want to feel fed, not just fueled. The line forms at 2000 and wraps are gone by 2200.",
  story_hooks: ["Yuki's spice blend recipe is memorized, never written down, and she has told three people — one for each decade she's been cooking"],
  tags: ["street_food", "wrap", "consumer_good", "tier_1", "tier_2", "old_harbor", "night_market", "artisan"],
  parent_corponation: ""
});

writeGood({
  id: uid(),
  name: "Brother Jun's Hand-Pulled Noodle Bowl",
  brand_name: "Brother Jun's",
  product_name: "Brother Jun's Hand-Pulled Noodle Bowl",
  type: "consumer_good",
  category: "street_food",
  subcategory: "noodles",
  manufacturer: "Jun's Noodle Station, Old Harbor Night Market",
  description: "Hand-pulled wheat noodles in bone-stock broth made from vat-grown collagen. Topped with whatever aromatics Jun found at the morning market. The noodles are the show — pulled to order, stretched and folded in a technique passed down three generations.",
  flavor_profile: "Rich, savory broth with a depth that shouldn't be possible at this price point. The noodles are chewy, elastic, and satisfying in a way machine-made noodles can't replicate.",
  tier_availability: "Tier 1-2",
  price: "Φ1.10",
  popularity_rank: 9,
  slogan: "Watch. Wait. Eat.",
  cultural_context: "The line at Jun's is a social institution. You wait, you watch him pull noodles, you talk to the person next to you. By the time you eat, you've already been nourished by the experience.",
  story_hooks: ["Jun has refused seven offers from Tier 3 restaurant chains to franchise his technique — he says hand-pulled noodles can't be scaled without losing what makes them hand-pulled"],
  tags: ["street_food", "noodles", "consumer_good", "tier_1", "tier_2", "old_harbor", "night_market", "artisan", "hand_pulled"],
  parent_corponation: ""
});

writeGood({
  id: uid(),
  name: "Block 12 Bao — Savory Protein",
  brand_name: "Block 12 Collective",
  product_name: "Block 12 Bao — Savory Protein",
  type: "consumer_good",
  category: "street_food",
  subcategory: "bao",
  manufacturer: "Block 12 Bao Collective, Old Harbor",
  description: "Steamed bun filled with seasoned protein paste and a single piece of reconstituted vegetable, made in a converted laundry unit by five women who produce exactly 300 per night and refuse to make more.",
  flavor_profile: "Soft, pillowy exterior giving way to a savory, well-seasoned filling with a burst of vegetable sweetness. Simple and perfect.",
  tier_availability: "Tier 1",
  price: "Φ0.50",
  popularity_rank: 11,
  slogan: "300. No more.",
  cultural_context: "The Bao Collective is Old Harbor folklore. The 300-per-night limit is not a production constraint — they could make more. It's a principle. Good things should be finite.",
  story_hooks: ["The youngest member of the collective is 22 and was trained by the oldest member, who is 71 — the recipe transfer was oral, taking six months of daily instruction"],
  tags: ["street_food", "bao", "consumer_good", "tier_1", "old_harbor", "collective", "artisan", "limited"],
  parent_corponation: ""
});

writeGood({
  id: uid(),
  name: "Kofi's Grilled Protein Flatbread",
  brand_name: "Kofi's Grill",
  product_name: "Kofi's Grilled Protein Flatbread",
  type: "consumer_good",
  category: "street_food",
  subcategory: "grill",
  manufacturer: "Kofi's Grill, Old Harbor Night Market",
  description: "Open-flame grilled protein slab marinated in chili-ginger paste, served on hand-pressed flatbread with pickled radish. The only open-flame cooking in Old Harbor, maintained under a grandfathered fire permit.",
  flavor_profile: "Charred, smoky, with a chili heat that builds slowly and a cooling crunch from the pickled radish. The open flame adds complexity no electric grill can match.",
  tier_availability: "Tier 1-2",
  price: "Φ1.30",
  popularity_rank: 12,
  slogan: "Fire changes everything.",
  cultural_context: "Kofi's fire permit is legendary in Old Harbor. If the permit is ever revoked, the grill dies — no one can get a new one. The community considers Kofi's flame a cultural heritage site.",
  story_hooks: ["The fire permit was originally issued to Kofi's grandfather for a metalworking operation — the legal fiction that the grill is a metalworking tool has survived three inspections"],
  tags: ["street_food", "grill", "consumer_good", "tier_1", "tier_2", "old_harbor", "night_market", "open_flame", "artisan"],
  parent_corponation: ""
});

writeGood({
  id: uid(),
  name: "ShelfBrew Chicory Coffee Concentrate",
  brand_name: "ShelfBrew",
  product_name: "ShelfBrew Chicory Coffee Concentrate",
  type: "consumer_good",
  category: "beverage",
  subcategory: "coffee",
  manufacturer: "Old Harbor Roasting Collective",
  description: "Concentrated chicory-based coffee substitute in a 100ml squeeze bottle. Mix with hot water at any ratio. Real coffee is Tier 3+; ShelfBrew is what the Shelf drinks instead. Bitter, dark, and caffeinated through added synthetic caffeine.",
  flavor_profile: "Intensely bitter, earthy, with roasted grain undertones. Not coffee. Not trying to be coffee. Its own thing.",
  tier_availability: "Tier 1-2",
  price: "Φ0.60",
  popularity_rank: 4,
  slogan: "It's not coffee. It's better than nothing.",
  cultural_context: "ShelfBrew is the morning ritual for millions. The taste is an acquired one that Shelf residents defend with surprising passion. Tier 3 visitors who try it grimace. Shelf residents who try real coffee for the first time often find it too mild.",
  story_hooks: ["The Old Harbor Roasting Collective maintains that chicory coffee has more character than real coffee — a position that is either deeply held conviction or very good marketing for a product born of necessity"],
  tags: ["beverage", "coffee", "consumer_good", "tier_1", "tier_2", "shelf", "chicory", "morning_ritual"],
  parent_corponation: ""
});

// --- MID TIER (10) ---

writeGood({
  id: uid(),
  name: "Kenji Farms Heritage Cut — Teriyaki",
  brand_name: "Kenji Farms",
  product_name: "Kenji Farms Heritage Cut — Teriyaki",
  type: "consumer_good",
  category: "food",
  subcategory: "vat_protein",
  manufacturer: "Kenji Farms Artisan Vat",
  description: "Premium vat-grown protein with visible marbling, cultured in a medium influenced by traditional Japanese dashi. The teriyaki glaze is made in-house from soy compound, mirin substitute, and real ginger. Sold in 200g vacuum-sealed portions.",
  flavor_profile: "Rich umami with sweet teriyaki lacquer. The marbling renders properly under heat, producing a satisfying richness. Convincingly meat-like.",
  tier_availability: "Tier 3-4",
  price: "Φ4.50",
  popularity_rank: 15,
  slogan: "Raised Right.",
  cultural_context: "Kenji Farms occupies the sweet spot of the mid-tier food market: good enough to feel like a treat, affordable enough for weekly purchase. It's what Tier 3 residents serve when they're trying to impress.",
  story_hooks: ["Kenji Farms' founder studied pre-consolidation Japanese culinary texts to develop the culture medium — the dashi influence is real, not marketing"],
  tags: ["food", "vat_protein", "consumer_good", "tier_3", "tier_4", "premium", "japanese", "teriyaki"],
  parent_corponation: ""
});

writeGood({
  id: uid(),
  name: "Verdant Bowl — Weekly Subscription Box",
  brand_name: "Verdant",
  product_name: "Verdant Bowl — Weekly Subscription Box",
  type: "consumer_good",
  category: "food",
  subcategory: "meal_kit",
  manufacturer: "Verdant Meal Systems",
  description: "Weekly delivery of seven pre-portioned meal kits featuring vat-grown proteins, hydroponic vegetables, and grain blends. Each meal is nutritionally optimized for the subscriber's biometric profile via BCI integration. Assembly required: 10-15 minutes per meal.",
  flavor_profile: "Variable — each week's menu cycles through global cuisine profiles. Consistently well-seasoned, if formulaic.",
  tier_availability: "Tier 3",
  price: "Φ28.00/week",
  popularity_rank: 18,
  slogan: "Your body knows what it needs. We deliver it.",
  cultural_context: "Verdant is the default for Tier 3 professionals who want to eat well but don't want to think about it. The BCI integration is the selling point — meals calibrated to your metabolism, your activity level, your micronutrient gaps.",
  story_hooks: ["Verdant's algorithm occasionally flags unusual nutritional deficiencies that lead subscribers to seek medical attention — the meal service has accidentally become a diagnostic tool"],
  tags: ["food", "meal_kit", "consumer_good", "tier_3", "subscription", "bci_integrated", "nutrition"],
  parent_corponation: ""
});

writeGood({
  id: uid(),
  name: "SavannaPro Wild Type Steak",
  brand_name: "SavannaPro",
  product_name: "SavannaPro Wild Type Steak",
  type: "consumer_good",
  category: "food",
  subcategory: "vat_protein",
  manufacturer: "SavannaPro Bioculture",
  description: "Vat-grown protein cultured to replicate the flavor profile of wild game. The culture medium includes compounds derived from foraged plant extracts, producing a gamey depth absent from standard vat products. Sold in 150g portions.",
  flavor_profile: "Gamey, iron-rich, with an earthy depth that evokes grassland and smoke. Not quite wild, but the closest thing available without leaving the city.",
  tier_availability: "Tier 3-4",
  price: "Φ11.00",
  popularity_rank: 20,
  slogan: "Remember Wild.",
  cultural_context: "SavannaPro sells nostalgia for a world that most of its customers have never experienced. The 'wild type' branding appeals to a mid-tier desire for authenticity that the city cannot provide.",
  story_hooks: ["SavannaPro's 'foraged plant extracts' are actually sourced from a satellite town's wild garden at significant markup — the supply chain is more interesting than the marketing"],
  tags: ["food", "vat_protein", "consumer_good", "tier_3", "tier_4", "wild_game", "premium", "nostalgia"],
  parent_corponation: ""
});

writeGood({
  id: uid(),
  name: "Golden Crust Bakery Sourdough Loaf",
  brand_name: "Golden Crust",
  product_name: "Golden Crust Bakery Sourdough Loaf",
  type: "consumer_good",
  category: "food",
  subcategory: "bread",
  manufacturer: "Golden Crust Bakery, Midline District",
  description: "Actual leavened sourdough bread made with hydroponic wheat flour and a 40-year-old starter culture maintained through three generations of bakers. Baked daily in small batches. Dense, tangy, with a crust that shatters.",
  flavor_profile: "Complex sour tang, chewy crumb, deeply caramelized crust. The starter culture gives each loaf a character that industrial bread cannot replicate.",
  tier_availability: "Tier 3-4",
  price: "Φ6.00",
  popularity_rank: 19,
  slogan: "Forty Years Rising.",
  cultural_context: "Golden Crust is a pilgrimage destination for mid-tier food enthusiasts. The bakery opens at 0600 and is sold out by 0900. The 40-year starter culture is treated as a living artifact.",
  story_hooks: ["The starter culture has been genetically sequenced and found to contain yeast strains that have evolved in isolation, making it biologically unique"],
  tags: ["food", "bread", "consumer_good", "tier_3", "tier_4", "bakery", "sourdough", "artisan"],
  parent_corponation: ""
});

writeGood({
  id: uid(),
  name: "Vossen BioSteak Premium — Classic",
  brand_name: "Vossen BioSteak",
  product_name: "Vossen BioSteak Premium — Classic",
  type: "consumer_good",
  category: "food",
  subcategory: "vat_protein",
  manufacturer: "Vossen Nutrition Division",
  description: "Vossen's flagship vat-grown steak product. Technically perfect — ideal myoglobin distribution, precise fat marbling, optimized amino acid profile. 200g portion, vacuum sealed, with recommended cooking instructions that no one follows because the steak is designed to be good regardless of preparation.",
  flavor_profile: "Clean, rich, perfectly balanced. Technically excellent in a way that leaves no impression. The uncanny valley of meat.",
  tier_availability: "Tier 3-4",
  price: "Φ8.00",
  popularity_rank: 17,
  slogan: "Precision Nutrition.",
  cultural_context: "Vossen BioSteak is what restaurants serve when they want to charge Φ25 for a plate without doing any creative work. It's reliable, forgettable, and everywhere. The food equivalent of a Tier 3 hab unit.",
  story_hooks: ["Vossen's food division uses the same bioreactor technology as their medical division — the steak and the synthetic organ share manufacturing DNA"],
  tags: ["food", "vat_protein", "consumer_good", "tier_3", "tier_4", "vossen", "premium", "corporate"],
  parent_corponation: "vossen"
});

writeGood({
  id: uid(),
  name: "Harbor Catch Aquaculture Fillet",
  brand_name: "Harbor Catch",
  product_name: "Harbor Catch Aquaculture Fillet",
  type: "consumer_good",
  category: "food",
  subcategory: "fish",
  manufacturer: "Old Harbor Aquaculture Cooperative",
  description: "Fresh-harvested tilapia fillet from the Old Harbor aquaculture pens — enclosed sections of Lake Michigan where engineered fish strains are raised in controlled conditions. Sold whole or filleted at the harbor market. One of the few animal-derived proteins available below Tier 4.",
  flavor_profile: "Mild, clean, slightly sweet. Lacks the complexity of wild fish but incomparably better than any vat-grown fish substitute.",
  tier_availability: "Tier 2-3",
  price: "Φ3.50",
  popularity_rank: 13,
  slogan: "Lake to Plate.",
  cultural_context: "Harbor Catch is Old Harbor's pride — proof that the district produces something the rest of the city can't replicate. The fish are real, raised in real water, and eating one feels like an act of connection to the lake that surrounds the city.",
  story_hooks: ["The aquaculture pens are technically in Vossen's water jurisdiction, creating a perpetual tension between the cooperative's independence and the corponation's territorial claims"],
  tags: ["food", "fish", "consumer_good", "tier_2", "tier_3", "old_harbor", "aquaculture", "real_food", "lake"],
  parent_corponation: ""
});

writeGood({
  id: uid(),
  name: "Mama Nkem's Jollof Rice Kit",
  brand_name: "Mama Nkem's",
  product_name: "Mama Nkem's Jollof Rice Kit",
  type: "consumer_good",
  category: "food",
  subcategory: "meal_kit",
  manufacturer: "Nkem's Kitchen, Midline District",
  description: "Pre-portioned kit containing hydroponic rice, tomato concentrate, chili compound, and Nkem's signature spice blend. Serves two. Requires 30 minutes of cooking. The closest thing to home-cooked West African cuisine available at mid-tier pricing.",
  flavor_profile: "Rich, tomatoey, with a smoky chili warmth and layers of spice that develop over the cooking time. Deeply savory with a sweet finish.",
  tier_availability: "Tier 2-3",
  price: "Φ3.00",
  popularity_rank: 10,
  slogan: "The Way It Should Taste.",
  cultural_context: "Mama Nkem started selling jollof rice kits from her hab unit 12 years ago. The business grew through word of mouth. The recipe is a Diaspora artifact — Nigerian foundation, adapted through three generations in M88, incorporating ingredients available locally.",
  story_hooks: ["Nkem's spice blend has become a minor cultural flashpoint — a food blogger's claim that it wasn't 'authentic' jollof sparked a three-week feed debate involving thousands of participants and zero resolution"],
  tags: ["food", "meal_kit", "consumer_good", "tier_2", "tier_3", "west_african", "jollof", "diaspora", "artisan"],
  parent_corponation: ""
});

writeGood({
  id: uid(),
  name: "Chai Republic Masala Concentrate",
  brand_name: "Chai Republic",
  product_name: "Chai Republic Masala Concentrate",
  type: "consumer_good",
  category: "beverage",
  subcategory: "tea",
  manufacturer: "Chai Republic Beverage Co.",
  description: "Concentrated masala chai blend in a 200ml bottle. Mix with hot water and synthetic milk (or real milk if you're feeling wealthy). Made with hydroponic tea leaves, real ginger, cardamom extract, and cinnamon compound. Twenty servings per bottle.",
  flavor_profile: "Warm, spicy, deeply aromatic. The ginger provides heat, the cardamom provides sweetness, and the tea base provides tannin backbone. Comforting in a way that transcends tier.",
  tier_availability: "Tier 2-4",
  price: "Φ4.00",
  popularity_rank: 5,
  slogan: "Warm From the Inside.",
  cultural_context: "Chai Republic is one of the few food brands that genuinely crosses tier boundaries. The concentrate is the same product whether you buy it in a Tier 2 market or a Tier 4 grocery. It is democratically warm.",
  story_hooks: ["Chai Republic's founder insists on using real ginger despite the cost because her grandmother told her that chai without real ginger is just brown water"],
  tags: ["beverage", "tea", "chai", "consumer_good", "tier_2", "tier_3", "tier_4", "cross_tier", "comforting", "diaspora"],
  parent_corponation: ""
});

writeGood({
  id: uid(),
  name: "MidCity Diner Comfort Bowl",
  brand_name: "MidCity Diner",
  product_name: "MidCity Diner Comfort Bowl",
  type: "consumer_good",
  category: "food",
  subcategory: "prepared_meal",
  manufacturer: "MidCity Diner Chain (14 locations)",
  description: "The signature dish of M88's most ubiquitous Tier 3 restaurant chain: a deep bowl of vat-grown ground protein in gravy over mashed starch, topped with melted synth-cheese and crispy fried allium. Served hot. Portions are generous. Nutritional value is secondary to emotional value.",
  flavor_profile: "Rich, salty, carb-heavy, with the specific comfort of food that is designed to make you feel like everything is going to be okay even when it isn't.",
  tier_availability: "Tier 3",
  price: "Φ5.50",
  popularity_rank: 14,
  slogan: "You Deserve This.",
  cultural_context: "MidCity Diner is where Tier 3 goes after a bad day. The Comfort Bowl is not good food. It is effective food. It fills a hole that isn't nutritional.",
  story_hooks: ["MidCity Diner's '14 locations' are all within a 4-km radius in the Midline district, creating a density that suggests either brilliant market saturation or a money-laundering operation"],
  tags: ["food", "prepared_meal", "consumer_good", "tier_3", "comfort_food", "restaurant_chain", "midline"],
  parent_corponation: ""
});

writeGood({
  id: uid(),
  name: "Petal & Rind Artisan Cheese Wheel — Smoked",
  brand_name: "Petal & Rind",
  product_name: "Petal & Rind Artisan Cheese Wheel — Smoked",
  type: "consumer_good",
  category: "food",
  subcategory: "cheese",
  manufacturer: "Petal & Rind Creamery",
  description: "Small wheel (200g) of synth-milk cheese aged 60 days in a climate-controlled micro-dairy and finished with cold smoke from reclaimed hardwood chips. The closest approximation to pre-consolidation artisan cheese available at mid-tier pricing.",
  flavor_profile: "Smoky, sharp, with a crumbly texture that melts on the tongue. The aging process develops depth and complexity that mass-produced synth cheese lacks entirely.",
  tier_availability: "Tier 3-4",
  price: "Φ9.00",
  popularity_rank: 21,
  slogan: "Patience Makes Flavor.",
  cultural_context: "Petal & Rind is the passion project of a former Vossen food chemist who was tired of optimizing nutrition and wanted to optimize pleasure instead. The cheese is impractical, overpriced for what it is, and beloved.",
  story_hooks: ["The 'reclaimed hardwood chips' used for smoking come from pre-consolidation buildings being demolished — each batch of cheese is literally smoked with history"],
  tags: ["food", "cheese", "consumer_good", "tier_3", "tier_4", "artisan", "smoked", "dairy", "premium"],
  parent_corponation: ""
});

// --- LUXURY TIER (10) ---

writeGood({
  id: uid(),
  name: "Aurelian Kobe Reserve — 200g",
  brand_name: "Aurelian",
  product_name: "Aurelian Kobe Reserve — 200g",
  type: "consumer_good",
  category: "food",
  subcategory: "vat_protein",
  manufacturer: "Aurelian Biolux",
  description: "Small-batch vat-grown protein cultured from proprietary Wagyu cattle DNA in bioreactors no larger than a domestic appliance. Each 200g portion is individually numbered and accompanied by a certificate of origin specifying the bioreactor, culture batch, and growth timeline. The fat marbling is visible, extensive, and melts at precisely 42°C.",
  flavor_profile: "Buttery, intensely beefy, with a complexity that unfolds over 30 seconds of chewing. The fat coats the palate with a richness that triggers a measurable dopamine response.",
  tier_availability: "Tier 5",
  price: "Φ45.00",
  popularity_rank: 24,
  slogan: "Numbered. For a Reason.",
  cultural_context: "Aurelian Kobe Reserve is not food — it is a status signal. Serving it at a dinner party communicates wealth, taste, and access. The numbered certificate is displayed at the table. Guests are expected to comment.",
  story_hooks: ["The Wagyu DNA used in the culture process was acquired from the last known living Wagyu cattle, maintained by a private collector — the provenance is genuine and the ethical questions are extensive"],
  tags: ["food", "vat_protein", "consumer_good", "tier_5", "luxury", "wagyu", "premium", "status", "numbered"],
  parent_corponation: ""
});

writeGood({
  id: uid(),
  name: "Orchard Prime Real Apple — Single",
  brand_name: "Orchard Prime",
  product_name: "Orchard Prime Real Apple — Single",
  type: "consumer_good",
  category: "food",
  subcategory: "fruit",
  manufacturer: "Orchard Prime Collective",
  description: "A single apple. Grown on a real tree in a Tier 4 rooftop orchard. Approximately 180g, variety varies by season. May contain cosmetic imperfections including wind scarring, irregular coloring, and the marks of actual insect contact. These imperfections are the point.",
  flavor_profile: "Crisp, juicy, sweet-tart, with a depth of flavor that synthetic apple compound cannot approach. Each apple tastes slightly different. This is remarkable.",
  tier_availability: "Tier 4-5",
  price: "Φ14.00",
  popularity_rank: 25,
  slogan: "Real.",
  cultural_context: "A real apple in M88 costs more than a Shelf resident's daily food budget. The fruit is eaten ceremonially — slowly, attentively, often shared. Giving someone an apple is an act of profound generosity.",
  story_hooks: ["Orchard Prime's trees are descended from heritage varieties maintained by seed-saving networks — each tree is a genetic artifact of the pre-consolidation world"],
  tags: ["food", "fruit", "consumer_good", "tier_4", "tier_5", "luxury", "real_food", "apple", "rare"],
  parent_corponation: ""
});

writeGood({
  id: uid(),
  name: "Epoch Prime Filet — Neural Enhanced",
  brand_name: "Epoch",
  product_name: "Epoch Prime Filet — Neural Enhanced",
  type: "consumer_good",
  category: "food",
  subcategory: "vat_protein",
  manufacturer: "Epoch Culinary Sciences",
  description: "Ultra-premium vat-grown filet containing undisclosed bioactive compounds that enhance cognitive function during the post-meal window. Sold in 150g portions with pairing suggestions. The packaging lists 'bioactive flavor enhancers' without specifying what they enhance.",
  flavor_profile: "Exquisite — delicate, clean, with a buttery finish that lingers. But the real effect is neurological: within 30 minutes of consumption, verbal fluency and pattern recognition measurably improve.",
  tier_availability: "Tier 5",
  price: "Φ120.00",
  popularity_rank: 28,
  slogan: "Elevate.",
  cultural_context: "Epoch Prime is the food of closed-door business dinners and high-stakes negotiations. Executives eat it before important meetings. The cognitive enhancement is an open secret in Tier 5 and completely unknown below Tier 4.",
  story_hooks: ["The NE-7 compound in Epoch Prime is derived from a pharmaceutical that failed clinical trials for cognitive enhancement — repackaged as a food additive, it bypassed pharmaceutical regulation entirely"],
  tags: ["food", "vat_protein", "consumer_good", "tier_5", "luxury", "neural_enhancement", "nootropic", "status", "secret"],
  parent_corponation: ""
});

writeGood({
  id: uid(),
  name: "Terroir Blanc Real Cow Milk — 500ml",
  brand_name: "Terroir Blanc",
  product_name: "Terroir Blanc Real Cow Milk — 500ml",
  type: "consumer_good",
  category: "beverage",
  subcategory: "dairy",
  manufacturer: "Terroir Blanc Pastoral",
  description: "Actual cow's milk from a small herd maintained in a Tier 5 controlled-environment agricultural facility. Pasteurized, unhomogenized, with a visible cream line. The cows are real. The facility is climate-controlled, biome-managed, and costs more to maintain than most Tier 1 residential blocks.",
  flavor_profile: "Rich, sweet, grassy, with a warmth and complexity that synthetic milk cannot approximate. The cream rises. This alone makes it extraordinary.",
  tier_availability: "Tier 5",
  price: "Φ22.00",
  popularity_rank: 27,
  slogan: "From Living Animals.",
  cultural_context: "Terroir Blanc is controversial. Animal agriculture in a city that feeds itself from vats is an extravagance that some consider immoral — resources spent on cows could feed hundreds of people. Terroir Blanc's customers consider it a preservation of heritage. The debate is unresolvable.",
  story_hooks: ["The herd consists of exactly 12 cows, each with a name, a genetic profile, and a fan following among Tier 5 food enthusiasts who track individual cows' milk production seasons"],
  tags: ["beverage", "dairy", "milk", "consumer_good", "tier_5", "luxury", "real_food", "animal", "controversial"],
  parent_corponation: ""
});

writeGood({
  id: uid(),
  name: "Sakura Table Omakase — Single Seating",
  brand_name: "Sakura Table",
  product_name: "Sakura Table Omakase — Single Seating",
  type: "consumer_good",
  category: "food",
  subcategory: "restaurant_experience",
  manufacturer: "Sakura Table, Arden Spire",
  description: "Twelve-course omakase dining experience at GLMZ's most exclusive restaurant. Chef Yori selects and prepares each course based on the diner's real-time BCI biometric data, adjusting flavors, temperatures, and textures to maximize the individual's neurological pleasure response. No two meals are identical. Reservations require a six-month waitlist and a Φ200 non-refundable deposit.",
  flavor_profile: "Transcendent and individualized. Each course is calibrated to the specific diner's taste receptors and emotional state.",
  tier_availability: "Tier 5",
  price: "Φ350.00",
  popularity_rank: 30,
  slogan: "Chef Decides.",
  cultural_context: "Sakura Table is not a restaurant — it is a performance. The meal is an event. The waitlist is a status marker. Having eaten at Sakura Table is a social credential in Tier 5 that communicates access, patience, and the ability to spend casually what a Shelf resident earns in five months.",
  story_hooks: ["Chef Yori has refused BCI installation, cooking entirely by analog intuition — she reads diners' physical responses instead of their data, making her an anachronism in her own restaurant"],
  tags: ["food", "restaurant", "consumer_good", "tier_5", "luxury", "omakase", "bci_integrated", "exclusive", "arden_spire"],
  parent_corponation: ""
});

writeGood({
  id: uid(),
  name: "Cloud Garden Real Strawberries — 250g Punnet",
  brand_name: "Cloud Garden",
  product_name: "Cloud Garden Real Strawberries — 250g Punnet",
  type: "consumer_good",
  category: "food",
  subcategory: "fruit",
  manufacturer: "Cloud Garden Rooftop Farms",
  description: "Quarter-kilo punnet of real strawberries, grown hydroponically in controlled UV chambers on Tier 4 rooftops. Small, intensely red, fragile, and perishable — they must be eaten within 48 hours of harvest. Each punnet contains approximately 15-20 berries.",
  flavor_profile: "Explosively sweet with a floral brightness that fills the sinuses. Juicy, fragrant, and so far removed from synthetic strawberry flavoring that they seem like a different fruit entirely.",
  tier_availability: "Tier 4-5",
  price: "Φ18.00",
  popularity_rank: 23,
  slogan: "Fleeting. Worth It.",
  cultural_context: "Cloud Garden strawberries are given as romantic gifts, celebration treats, and apology offerings. Their perishability is part of their value — you cannot hoard them. You must share them, eat them now, let them be temporary.",
  story_hooks: ["Cloud Garden's growing operation is so precisely controlled that they can produce strawberries year-round, but they maintain seasonal releases to create artificial scarcity and emotional association with time of year"],
  tags: ["food", "fruit", "consumer_good", "tier_4", "tier_5", "luxury", "strawberry", "real_food", "perishable"],
  parent_corponation: ""
});

writeGood({
  id: uid(),
  name: "Spire Select Real Egg — Free-Range (6-Pack)",
  brand_name: "Spire Select",
  product_name: "Spire Select Real Egg — Free-Range (6-Pack)",
  type: "consumer_good",
  category: "food",
  subcategory: "egg",
  manufacturer: "Spire Select Agricultural",
  description: "Six real eggs from actual chickens maintained in a rooftop aviary in the Arden Spire district. The chickens are free-range within the aviary (a generous 400 square meters for 60 birds). Each egg is individually stamped with a lay date and the hen's identifier.",
  flavor_profile: "Deep golden yolk with a richness and viscosity that synthetic egg cannot match. The white sets firmly. The yolk runs slowly. The taste is dense, savory, and unmistakably animal.",
  tier_availability: "Tier 5",
  price: "Φ30.00",
  popularity_rank: 26,
  slogan: "Laid Today.",
  cultural_context: "Real eggs are a Tier 5 breakfast status symbol. Serving real eggs to guests communicates a level of access that money alone cannot explain — you need to know someone, or be on the right subscription list.",
  story_hooks: ["Each of the 60 hens has a name and a BCI-accessible profile tracking its diet, health, and egg production — the hens are better monitored than most Tier 1 residents"],
  tags: ["food", "egg", "consumer_good", "tier_5", "luxury", "real_food", "chicken", "free_range", "arden_spire"],
  parent_corponation: ""
});

writeGood({
  id: uid(),
  name: "Nostalgia Kitchen Artisan Pasta — Truffle Tagliatelle",
  brand_name: "Nostalgia Kitchen",
  product_name: "Nostalgia Kitchen Artisan Pasta — Truffle Tagliatelle",
  type: "consumer_good",
  category: "food",
  subcategory: "pasta",
  manufacturer: "Nostalgia Kitchen Atelier",
  description: "Hand-cut tagliatelle made with hydroponic durum semolina, real egg (from Spire Select stock), and infused with synthetic truffle compound that is chemically indistinguishable from actual truffle aroma. 200g portion, fresh, must be cooked within 24 hours.",
  flavor_profile: "Silky, rich, with an intoxicating truffle aroma that fills the kitchen. The pasta has bite and substance — it holds sauce and demands attention.",
  tier_availability: "Tier 4-5",
  price: "Φ16.00",
  popularity_rank: 22,
  slogan: "Made by Hand. Every Strand.",
  cultural_context: "Nostalgia Kitchen trades on the pre-consolidation Italian culinary tradition — or rather, on the idea of that tradition, since nobody alive remembers it firsthand. The pasta is excellent. The nostalgia is manufactured. Both are effective.",
  story_hooks: ["The truffle compound is synthetic but the pasta maker claims she once tasted actual truffle at a private Tier 5 dinner and calibrated her compound to match — the claim is unverifiable and excellent marketing"],
  tags: ["food", "pasta", "consumer_good", "tier_4", "tier_5", "luxury", "artisan", "truffle", "handmade"],
  parent_corponation: ""
});

writeGood({
  id: uid(),
  name: "Apex Reserve Single-Origin Coffee — 100g",
  brand_name: "Apex Reserve",
  product_name: "Apex Reserve Single-Origin Coffee — 100g",
  type: "consumer_good",
  category: "beverage",
  subcategory: "coffee",
  manufacturer: "Apex Reserve Coffee Collective",
  description: "Whole-bean coffee grown in a Tier 5 controlled-environment greenhouse from heritage Arabica stock. Roasted in micro-batches of 5kg. Each 100g bag is labeled with the plant number, harvest date, and roast profile. Real coffee — from a real plant — in a city where real coffee is almost mythological.",
  flavor_profile: "Complex, bright, with notes of dark chocolate, citrus, and a clean finish. The aroma alone is worth the price to anyone who has spent their life drinking chicory substitute.",
  tier_availability: "Tier 5",
  price: "Φ55.00",
  popularity_rank: 29,
  slogan: "Grown. Not Synthesized.",
  cultural_context: "Apex Reserve coffee is consumed as ritual. Tier 5 executives grind it by hand (manual grinders are a luxury item in their own right), brew it in analog pour-over devices, and drink it slowly. It is possibly the most meditative act in a tier otherwise defined by optimization.",
  story_hooks: ["Apex Reserve maintains exactly 40 coffee plants — fewer than the number of Tier 5 executives who want their product — creating a scarcity that the company could resolve but chooses not to"],
  tags: ["beverage", "coffee", "consumer_good", "tier_5", "luxury", "real_food", "single_origin", "heritage", "rare"],
  parent_corponation: ""
});

writeGood({
  id: uid(),
  name: "Velvet Spire Dark Chocolate Bar — 70% Cacao",
  brand_name: "Velvet Spire",
  product_name: "Velvet Spire Dark Chocolate Bar — 70% Cacao",
  type: "consumer_good",
  category: "food",
  subcategory: "confection",
  manufacturer: "Velvet Spire Confections",
  description: "Dark chocolate bar made from real cacao beans grown in a Cascadia Nexus agricultural exchange. 80g bar, hand-tempered, with a snap that announces its quality. Real cacao is not grown in GLMZ — every bar represents a trade relationship between two city-states.",
  flavor_profile: "Bittersweet, complex, with a slow melt that releases waves of fruit, earth, and a lingering roasted finish. The tannins are present. The sweetness is restrained. This is chocolate for people who understand chocolate.",
  tier_availability: "Tier 4-5",
  price: "Φ24.00",
  popularity_rank: 20,
  slogan: "From Another World.",
  cultural_context: "Velvet Spire chocolate is proof that GLMZ is not self-sufficient — it depends on trade with other city-states for anything that can't be vat-grown or synthesized. The chocolate is delicious. It is also a political artifact.",
  story_hooks: ["The Cascadia-Meridian trade route for cacao is the only regular non-food-staple commerce between the two cities — chocolate is literally holding a diplomatic relationship together"],
  tags: ["food", "chocolate", "consumer_good", "tier_4", "tier_5", "luxury", "cacao", "real_food", "cascadia", "trade"],
  parent_corponation: ""
});

// ═══════════════════════════════════════════════════════════════════
// SUMMARY
// ═══════════════════════════════════════════════════════════════════

console.log('\n═══════════════════════════════════════');
console.log(`WRITTEN: ${written}`);
console.log(`SKIPPED: ${skipped}`);
console.log(`TOTAL:   ${written + skipped}`);
console.log('═══════════════════════════════════════');
