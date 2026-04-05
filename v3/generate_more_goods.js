const fs = require('fs');
const path = require('path');

const OUTPUT_DIR = path.join(__dirname, '..', 'engine_data', 'consumer_goods');

// Ensure output directory exists
if (!fs.existsSync(OUTPUT_DIR)) {
  fs.mkdirSync(OUTPUT_DIR, { recursive: true });
}

// Get existing filenames to avoid overwriting
const existingFiles = new Set(fs.readdirSync(OUTPUT_DIR).map(f => f.toLowerCase()));

function toFilename(name) {
  return name
    .toLowerCase()
    .replace(/['']/g, '')
    .replace(/[^a-z0-9]+/g, '_')
    .replace(/^_|_$/g, '') + '.json';
}

function writeProduct(product) {
  const filename = toFilename(product.name);
  if (existingFiles.has(filename)) {
    console.log(`SKIP (exists): ${filename}`);
    return false;
  }
  const filepath = path.join(OUTPUT_DIR, filename);
  fs.writeFileSync(filepath, JSON.stringify(product, null, 2) + '\n');
  existingFiles.add(filename);
  console.log(`WROTE: ${filename}`);
  return true;
}

// ============================================================
// ALL 200 PRODUCTS
// ============================================================

const products = [

  // =====================================================
  // STREET FOOD & MEALS (40)
  // =====================================================

  {
    name: "Mrs. Park's Classic Noodle Pack",
    type: "consumer_good",
    category: "street_food",
    subcategory: "instant noodles",
    manufacturer: "Park Family Foods",
    description: "The ubiquitous instant noodle pack of Meridian 88. Just add hot water, wait three minutes, and you have something that tastes close enough to real food that you stop thinking about the difference.",
    flavor_profile: "Salty, MSG-rich broth with a subtle sesame undertone, chewy synth-wheat noodles that hold texture surprisingly well",
    tier_availability: "Tier 1-3",
    price: "Φ1.20",
    popularity_rank: 1,
    slogan: "Like Mom made. If Mom had a factory.",
    cultural_context: "Mrs. Park is a real person — Sun-Hi Park, who started selling noodles from a Shelf corridor cart in 2071. The brand is now a subsidiary of Kanto-Pacific Nutrition but she still appears on every package. Everyone in the Shelf has eaten these. They are comfort food at the bottom of the world.",
    story_hooks: [
      "Sun-Hi Park is still alive, living in Tier 2, and deeply unhappy with what Kanto-Pacific has done to her recipe — she's looking for someone to help her reclaim her brand.",
      "A contaminated batch of Mrs. Park's caused a minor illness cluster in Shelf Block 7 — coincidence or sabotage?"
    ],
    tags: ["street_food", "instant_noodles", "consumer_good", "shelf", "tier_1", "tier_2", "tier_3", "comfort_food", "ubiquitous"]
  },
  {
    name: "Mrs. Park's Spicy Kimchi Noodle Pack",
    type: "consumer_good",
    category: "street_food",
    subcategory: "instant noodles",
    manufacturer: "Park Family Foods",
    description: "The premium variant with a fermented kimchi flavor packet that smells like someone's grandmother actually pickled something. More expensive than the Classic, but people swear it's worth it.",
    flavor_profile: "Sour, spicy, deeply fermented with garlic heat that lingers, noodles slightly thicker than the Classic",
    tier_availability: "Tier 1-3",
    price: "Φ1.80",
    popularity_rank: 4,
    slogan: "The one worth the extra sixty.",
    cultural_context: "The Spicy Kimchi is considered a small luxury in the Shelf — it's the pack you buy when you got paid. Tier 3 residents eat it ironically, then keep eating it because it's genuinely good.",
    story_hooks: [
      "The kimchi flavor packet contains a proprietary fermentation culture that Park Family Foods guards fiercely — a biotech firm wants to reverse-engineer it for pharmaceutical applications."
    ],
    tags: ["street_food", "instant_noodles", "consumer_good", "shelf", "tier_1", "tier_2", "premium_variant"]
  },
  {
    name: "Mrs. Park's Bone Broth Deluxe",
    type: "consumer_good",
    category: "street_food",
    subcategory: "instant noodles",
    manufacturer: "Park Family Foods",
    description: "The top-shelf Mrs. Park's. Contains actual bone-broth concentrate derived from vat-grown collagen. The noodles are egg-enriched. Comes in a self-heating container.",
    flavor_profile: "Rich, gelatinous broth with genuine umami depth, slight sweetness from the collagen, egg noodles with real bite",
    tier_availability: "Tier 2-4",
    price: "Φ4.50",
    popularity_rank: 12,
    slogan: "Real enough to remember.",
    cultural_context: "The Deluxe is aspirational eating in the Shelf — you see the empty containers displayed in people's living spaces. In Tier 3 it's a normal Tuesday dinner.",
    story_hooks: [
      "The self-heating containers use a chemical reaction that, in bulk, can be repurposed as an incendiary — security services track bulk purchases."
    ],
    tags: ["street_food", "instant_noodles", "consumer_good", "self_heating", "tier_2", "tier_3", "tier_4", "aspirational"]
  },
  {
    name: "TransitBento Standard",
    type: "consumer_good",
    category: "street_food",
    subcategory: "shelf bento",
    manufacturer: "M88 Transit Catering Corp",
    description: "Pre-packed meal tray sold at mass driver stations and transit hubs. Rice, protein portion, pickled vegetable, and a flavor sachet. Sealed, shelf-stable for 72 hours. Eaten standing up on platforms across the city.",
    flavor_profile: "Inoffensive, vaguely teriyaki, the rice is acceptable, the protein is identifiable as protein",
    tier_availability: "Tier 1-3",
    price: "Φ2.50",
    popularity_rank: 3,
    slogan: "Eat. Move. Repeat.",
    cultural_context: "The TransitBento is not good food. Everyone knows this. But it's available at every station, it won't make you sick, and you can eat it one-handed while checking your BCI feed. It is the most consumed prepared meal in M88 by volume.",
    story_hooks: [
      "Transit Catering Corp's contract with the city is up for renewal — competing bids are coming in and the lobbying has turned ugly.",
      "Someone is using TransitBento packaging to smuggle data chips between stations — the uniform boxes are never inspected."
    ],
    tags: ["street_food", "shelf_bento", "consumer_good", "transit", "mass_driver", "tier_1", "tier_2", "tier_3", "ubiquitous"]
  },
  {
    name: "TransitBento Curry Edition",
    type: "consumer_good",
    category: "street_food",
    subcategory: "shelf bento",
    manufacturer: "M88 Transit Catering Corp",
    description: "The popular variant with a curry sauce packet that actually has some kick. Same tray format, slightly more expensive, and the stations that carry it sell out faster.",
    flavor_profile: "Warm, turmeric-forward curry with a cumin backbone, the sauce makes the rice actually enjoyable",
    tier_availability: "Tier 1-3",
    price: "Φ3.00",
    popularity_rank: 8,
    slogan: "The better box.",
    cultural_context: "Station vendors report that curry edition availability correlates with commuter mood. There's a BCI feed that tracks which stations have it in stock in real time — it has forty thousand subscribers.",
    story_hooks: [
      "The curry recipe was licensed from a Tier 1 street cook who got a one-time payment of Φ500 for a recipe now generating millions annually."
    ],
    tags: ["street_food", "shelf_bento", "consumer_good", "transit", "curry", "tier_1", "tier_2", "tier_3"]
  },
  {
    name: "Kanto Block",
    type: "consumer_good",
    category: "street_food",
    subcategory: "synth-protein bar",
    manufacturer: "Kanto-Pacific Nutrition",
    description: "Dense rectangular nutrient block providing 400 calories, 30g protein, and full micronutrient supplementation. Available in six flavor coatings. The baseline food of the Shelf.",
    flavor_profile: "Chalky, dense, coated in a thin flavor shell — the 'chocolate' variant tastes like someone described chocolate to a machine",
    tier_availability: "Tier 1-2",
    price: "Φ0.80",
    popularity_rank: 2,
    slogan: "Everything you need. Nothing you don't.",
    cultural_context: "Kanto Blocks keep people alive. That's the nicest thing anyone says about them. They're distributed in Tier 1 aid packages and sold at every Shelf vendor. Eating them is survival, not dining. People who escape the Shelf never eat them again.",
    story_hooks: [
      "Kanto-Pacific has been quietly reducing the micronutrient content by 3% annually for five years — a whistleblower has the lab reports.",
      "A Shelf community kitchen has figured out how to process Kanto Blocks into something that actually tastes like food — the recipe is spreading and Kanto-Pacific wants it suppressed."
    ],
    tags: ["street_food", "synth_protein", "consumer_good", "shelf", "tier_1", "tier_2", "survival_food", "ubiquitous", "corporate"]
  },
  {
    name: "Kanto Block Tropical",
    type: "consumer_good",
    category: "street_food",
    subcategory: "synth-protein bar",
    manufacturer: "Kanto-Pacific Nutrition",
    description: "Same nutrient block, coated in a mango-pineapple flavored shell. The most popular variant because it masks the base taste best.",
    flavor_profile: "Sweet-tart fruit coating over the familiar chalk density, the coating dissolves quickly leaving you with the truth",
    tier_availability: "Tier 1-2",
    price: "Φ0.80",
    popularity_rank: 5,
    slogan: "Taste the somewhere else.",
    cultural_context: "The Tropical is darkly popular because people joke it's the closest they'll get to a vacation. Shelf humor runs black.",
    story_hooks: [
      "The tropical flavoring compound is sourced from a single geneware-modified fruit farm in Old Harbor — if it goes down, millions of people notice."
    ],
    tags: ["street_food", "synth_protein", "consumer_good", "shelf", "tier_1", "tier_2", "survival_food"]
  },
  {
    name: "Real Grill Yakitori Skewer",
    type: "consumer_good",
    category: "street_food",
    subcategory: "real meat",
    manufacturer: "Various street vendors",
    description: "Actual chicken on a stick, grilled over charcoal by street vendors in Tier 2-3 market districts. The smell draws people from blocks away. Two skewers is a meal, one skewer is a treat.",
    flavor_profile: "Smoky, charred, salty-sweet tare glaze, the unmistakable texture of real animal muscle fiber",
    tier_availability: "Tier 2-4",
    price: "Φ8.00",
    popularity_rank: 15,
    slogan: "No slogan — street vendors don't do marketing.",
    cultural_context: "Real meat from actual animals is expensive. Yakitori vendors are respected small businesspeople who source from vat-grow farms or, occasionally, actual poultry operations outside the city. The smoke and smell of real grilling is an event. People gather.",
    story_hooks: [
      "A yakitori vendor in the Tier 2 night market has been selling 'real chicken' that's actually vat-grown — technically legal but socially devastating if exposed.",
      "The charcoal supply chain runs through a single Old Harbor import operation that's also moving contraband."
    ],
    tags: ["street_food", "real_meat", "consumer_good", "luxury_food", "tier_2", "tier_3", "tier_4", "market"]
  },
  {
    name: "Harbor Glow Wrap",
    type: "consumer_good",
    category: "street_food",
    subcategory: "algae wrap",
    manufacturer: "Old Harbor Collective Kitchen",
    description: "Bioluminescent seaweed wrap filled with seasoned rice, pickled vegetables, and fermented algae paste. Faintly glows blue-green in low light. An Old Harbor specialty that's become a citywide cult food.",
    flavor_profile: "Briny, umami-rich, with a tang from the fermentation and a crisp seaweed snap, the glow is purely aesthetic",
    tier_availability: "Tier 1-3",
    price: "Φ3.50",
    popularity_rank: 18,
    slogan: "From the water. For the water.",
    cultural_context: "Harbor Glow Wraps are one of the few Tier 1 foods that Tier 3 people actively seek out. The Old Harbor Collective Kitchen is a community operation — profits go back to the harbor. Eating one in the dark is a small, beautiful thing.",
    story_hooks: [
      "A Tier 4 restaurant chain wants to license the Glow Wrap recipe and upscale it — the Collective is divided on whether to sell.",
      "The bioluminescent algae strain is unique to Old Harbor's water conditions and can't be easily replicated elsewhere."
    ],
    tags: ["street_food", "algae_wrap", "consumer_good", "old_harbor", "bioluminescent", "tier_1", "tier_2", "tier_3", "cult_food"]
  },
  {
    name: "NutriTube Original",
    type: "consumer_good",
    category: "street_food",
    subcategory: "nutrient paste",
    manufacturer: "Meridian Basic Services",
    description: "Squeezable tube of calorie-dense paste, flavored to approximate 'beef stew.' Provides 600 calories. Standard Tier 1 emergency and daily nutrition. You squeeze it into your mouth and try not to think.",
    flavor_profile: "Warm, salty, vaguely meaty with an aftertaste of vitamins and regret",
    tier_availability: "Tier 1",
    price: "Φ0.40",
    popularity_rank: 6,
    slogan: "Fuel for the day.",
    cultural_context: "NutriTubes are the absolute floor of food in M88. Eating them means you cannot afford even Kanto Blocks. They are distributed free at aid stations during crises. Tier 3+ residents have usually never tasted one and don't want to.",
    story_hooks: [
      "Meridian Basic Services is a government-contracted supplier — the contract is enormously profitable because the production cost is almost nothing.",
      "NutriTube Original's 'beef stew' flavor was designed by an AI that has never experienced food — a food scientist wants to redesign the line but can't get funding."
    ],
    tags: ["street_food", "nutrient_paste", "consumer_good", "shelf", "tier_1", "survival_food", "aid", "poverty"]
  },
  {
    name: "NutriTube Curry",
    type: "consumer_good",
    category: "street_food",
    subcategory: "nutrient paste",
    manufacturer: "Meridian Basic Services",
    description: "The curry-flavored variant of NutriTube. Marginally more palatable than the Original because spice masks the base taste. The most requested flavor at aid stations.",
    flavor_profile: "Spicy, turmeric-heavy, with enough chili heat to override the underlying paste flavor",
    tier_availability: "Tier 1",
    price: "Φ0.40",
    popularity_rank: 9,
    slogan: "Fuel with fire.",
    cultural_context: "People in the Shelf trade NutriTube flavors like currency. Curry commands a premium. Someone always has a surplus of Original and wants Curry.",
    story_hooks: [
      "The spice blend in Curry variant has a mild stimulant effect that's never been officially acknowledged — it keeps people working longer."
    ],
    tags: ["street_food", "nutrient_paste", "consumer_good", "shelf", "tier_1", "survival_food", "curry"]
  },
  {
    name: "QuickBowl Pho",
    type: "consumer_good",
    category: "street_food",
    subcategory: "flash-heated meal",
    manufacturer: "Saigon Express Foods",
    description: "Self-heating pho kit in a sealed bowl. Pull the tab, wait 90 seconds, and you have hot broth with rice noodles, synth-beef slices, and fresh herb packet. The herbs are freeze-dried but reconstitute well.",
    flavor_profile: "Star anise and cinnamon-scented broth, clean and aromatic, the noodles are slightly gummy but the herbs save it",
    tier_availability: "Tier 2-3",
    price: "Φ3.80",
    popularity_rank: 14,
    slogan: "Ninety seconds to Saigon.",
    cultural_context: "QuickBowl Pho is the go-to sick-day food across M88. The broth steam and the star anise smell are associated with recovery and comfort. People hoard them.",
    story_hooks: [
      "Saigon Express Foods is a front company for a Tier 4 investment group that buys up ethnic food brands — the original Vietnamese family that created the recipe received nothing."
    ],
    tags: ["street_food", "flash_heated", "consumer_good", "pho", "self_heating", "tier_2", "tier_3", "comfort_food"]
  },
  {
    name: "QuickBowl Jollof",
    type: "consumer_good",
    category: "street_food",
    subcategory: "flash-heated meal",
    manufacturer: "Lagos Kitchen Co.",
    description: "Self-heating jollof rice with tomato stew and a protein chunk packet. The rice actually has good texture — the self-heating technology works better with rice than noodles.",
    flavor_profile: "Tomato-rich, smoky, with scotch bonnet heat that builds slowly, the rice is firm and properly seasoned",
    tier_availability: "Tier 2-3",
    price: "Φ3.50",
    popularity_rank: 17,
    slogan: "Party in a box.",
    cultural_context: "West African diaspora food culture is strong in M88. QuickBowl Jollof is not as good as homemade but it's good enough to argue about, which is the whole point of jollof.",
    story_hooks: [
      "Lagos Kitchen Co. sponsors a popular BCI cooking competition where contestants try to make better jollof than the QuickBowl — the show is rigged."
    ],
    tags: ["street_food", "flash_heated", "consumer_good", "jollof", "self_heating", "tier_2", "tier_3", "west_african"]
  },
  {
    name: "QuickBowl Congee",
    type: "consumer_good",
    category: "street_food",
    subcategory: "flash-heated meal",
    manufacturer: "Saigon Express Foods",
    description: "Self-heating rice porridge with ginger, scallion, and century egg bits. Thick, warming, and the closest thing to a hug in a disposable container.",
    flavor_profile: "Mild, gingery, creamy from the rice starch, with funky bursts from the century egg",
    tier_availability: "Tier 2-3",
    price: "Φ3.00",
    popularity_rank: 20,
    slogan: "Warm from the inside.",
    cultural_context: "Congee is what people eat at 3 AM after a long shift. Transit station vendors stock it in the overnight hours. The self-heating tab sound — that sharp hiss — is the sound of exhaustion meeting sustenance.",
    story_hooks: [
      "Night shift workers at a Tier 2 processing plant have been subsisting almost entirely on QuickBowl Congee — a health worker is concerned about long-term nutritional gaps."
    ],
    tags: ["street_food", "flash_heated", "consumer_good", "congee", "self_heating", "tier_2", "tier_3", "night_shift"]
  },
  {
    name: "CrunchHopper Salt & Vinegar",
    type: "consumer_good",
    category: "street_food",
    subcategory: "insect protein crisps",
    manufacturer: "HopperSnacks Inc.",
    description: "Cricket-flour crisps in a foil bag. Crunchy, salty, and completely normalized — nobody in M88 thinks twice about eating insects. They're just chips.",
    flavor_profile: "Sharp vinegar tang with sea salt, light crispy texture, subtle nuttiness from the cricket flour",
    tier_availability: "Tier 1-3",
    price: "Φ1.50",
    popularity_rank: 7,
    slogan: "Crunch time.",
    cultural_context: "Insect protein is the default protein source for most of M88. CrunchHoppers are the market leader in snack crisps — they outsell synth-potato chips three to one. Asking if someone eats insects is like asking if they breathe air.",
    story_hooks: [
      "HopperSnacks' cricket farms are automated and enormous — a firmware glitch once shut down production for three days and the city nearly panicked."
    ],
    tags: ["street_food", "insect_protein", "consumer_good", "crisps", "snack", "tier_1", "tier_2", "tier_3", "ubiquitous"]
  },
  {
    name: "CrunchHopper Chili Lime",
    type: "consumer_good",
    category: "street_food",
    subcategory: "insect protein crisps",
    manufacturer: "HopperSnacks Inc.",
    description: "The spicy variant. Bright green bag. The chili lime seasoning is addictive — people eat these compulsively.",
    flavor_profile: "Citric lime punch with chili heat, dusted heavily, fingers turn red-orange from the seasoning",
    tier_availability: "Tier 1-3",
    price: "Φ1.50",
    popularity_rank: 10,
    slogan: "Can't stop, won't stop.",
    cultural_context: "Chili Lime CrunchHoppers are the default 'share a bag' snack. They show up at every informal gathering. The red-orange finger stain is a social signal that you've been snacking.",
    story_hooks: [
      "The chili lime seasoning contains a mild appetite suppressant — people eat less of other food after consuming them, which is by design."
    ],
    tags: ["street_food", "insect_protein", "consumer_good", "crisps", "snack", "tier_1", "tier_2", "tier_3"]
  },
  {
    name: "CrunchHopper BBQ Mealworm",
    type: "consumer_good",
    category: "street_food",
    subcategory: "insect protein crisps",
    manufacturer: "HopperSnacks Inc.",
    description: "Mealworm-based variant with a smoky barbecue coating. Heartier than the cricket crisps, with a more substantial crunch.",
    flavor_profile: "Smoky, sweet-savory barbecue with a deeper crunch and earthier insect base flavor",
    tier_availability: "Tier 1-3",
    price: "Φ1.80",
    popularity_rank: 22,
    slogan: "The meaty one.",
    cultural_context: "BBQ Mealworm is positioned as the 'protein snack' — gym-goers and laborers buy it for the higher protein content. The bag is brown instead of the usual bright colors.",
    story_hooks: [
      "A competing brand claims HopperSnacks is mixing cheaper fly larvae into the mealworm blend — lab analysis is inconclusive."
    ],
    tags: ["street_food", "insect_protein", "consumer_good", "crisps", "snack", "tier_1", "tier_2", "tier_3", "protein"]
  },
  {
    name: "Harborside Kombucha",
    type: "consumer_good",
    category: "street_food",
    subcategory: "fermented drink",
    manufacturer: "Old Harbor Fermentation Guild",
    description: "Small-batch kombucha brewed in Old Harbor using local algae cultures. Sold in recycled glass bottles with hand-written labels. Slightly different every batch.",
    flavor_profile: "Tart, effervescent, with a marine minerality unique to the harbor water, sometimes floral, sometimes funky",
    tier_availability: "Tier 1-3",
    price: "Φ2.50",
    popularity_rank: 25,
    slogan: "Alive in every bottle.",
    cultural_context: "The Fermentation Guild is a Tier 1 cooperative that's become a minor cultural institution. Their kombucha varies batch-to-batch, which is either charming or infuriating depending on your expectations.",
    story_hooks: [
      "The Guild's SCOBY mother culture is over thirty years old and produces unique probiotic strains — a pharmaceutical company wants to acquire it."
    ],
    tags: ["street_food", "fermented", "consumer_good", "kombucha", "old_harbor", "tier_1", "tier_2", "tier_3", "cooperative"]
  },
  {
    name: "Palm Gold",
    type: "consumer_good",
    category: "street_food",
    subcategory: "fermented drink",
    manufacturer: "Coastal Spirits Ltd.",
    description: "Fermented palm wine sold in single-serve pouches. Sweet, mildly alcoholic, and popular in Old Harbor and Tier 1-2 neighborhoods. Not regulated as alcohol because it's under 4%.",
    flavor_profile: "Sweet, yeasty, with a slight coconut note and a gentle warmth, cloudy white appearance",
    tier_availability: "Tier 1-2",
    price: "Φ1.20",
    popularity_rank: 19,
    slogan: "The people's pour.",
    cultural_context: "Palm Gold is the cheapest way to catch a mild buzz in the Shelf. It's consumed openly, sold everywhere, and considered more of a food than a drink. Grandmothers drink it. Children are not supposed to but do.",
    story_hooks: [
      "Coastal Spirits is trying to get Palm Gold classified as a food product to avoid alcohol taxes — the regulatory battle is a proxy war between corporate interests."
    ],
    tags: ["street_food", "fermented", "consumer_good", "palm_wine", "alcohol", "tier_1", "tier_2", "old_harbor"]
  },
  {
    name: "Tankhouse Kefir",
    type: "consumer_good",
    category: "street_food",
    subcategory: "fermented drink",
    manufacturer: "Tankhouse Dairy Alternatives",
    description: "Fermented synth-milk drink, thick and tangy. Sold in squeeze pouches at transit stations. Contains live cultures and is marketed as a gut health product.",
    flavor_profile: "Thick, sour, creamy with a slight fizz, available in plain and mango variants",
    tier_availability: "Tier 2-3",
    price: "Φ2.00",
    popularity_rank: 28,
    slogan: "Your gut knows.",
    cultural_context: "Tankhouse has made fermented synth-milk normal. It's a breakfast drink for transit commuters — you see people squeezing pouches on the mass driver every morning.",
    story_hooks: [
      "Tankhouse's cultures were originally stolen from a university biolab — the theft was never proven but everyone in the industry knows."
    ],
    tags: ["street_food", "fermented", "consumer_good", "kefir", "probiotic", "tier_2", "tier_3", "transit"]
  },
  {
    name: "Sato's Real Coffee",
    type: "consumer_good",
    category: "street_food",
    subcategory: "real coffee",
    manufacturer: "Sato Premium Imports",
    description: "Actual coffee from actual beans grown outside the city. Sold in single-serve sachets that produce one cup of real pour-over. The beans are roasted in small batches in Tier 3.",
    flavor_profile: "Rich, complex, with chocolate and berry notes that synthetic coffee cannot replicate — you know it's real immediately",
    tier_availability: "Tier 3-5",
    price: "Φ15.00",
    popularity_rank: 35,
    slogan: "You'll know the difference.",
    cultural_context: "Real coffee is a luxury that even Tier 3 residents consider a splurge. A sachet of Sato's is a common gift — giving someone real coffee says 'I value you and I have money.' Tier 1-2 residents have mostly never tasted it.",
    story_hooks: [
      "Sato's supply chain passes through three different jurisdictions, each of which takes a cut — a direct trade route would halve the price but powerful middlemen would lose their margins.",
      "Counterfeit Sato's sachets containing enhanced synth-coffee are circulating — they're good, but they're not real."
    ],
    tags: ["street_food", "real_coffee", "consumer_good", "luxury", "tier_3", "tier_4", "tier_5", "import", "gift"]
  },
  {
    name: "Black Mud Substitute Coffee",
    type: "consumer_good",
    category: "street_food",
    subcategory: "coffee substitute",
    manufacturer: "Grindhouse Beverages",
    description: "Roasted chicory and mushroom blend that approximates coffee's bitterness and ritual without any actual coffee. Sold in bulk tins. What 90% of M88 drinks when they say 'coffee.'",
    flavor_profile: "Bitter, earthy, roasty with a slight mushroom undertone, produces good crema when prepared well",
    tier_availability: "Tier 1-3",
    price: "Φ2.00",
    popularity_rank: 6,
    slogan: "Close enough.",
    cultural_context: "Black Mud is not trying to fool anyone. Everyone knows it's not coffee. But it's hot, it's bitter, it's ritual, and it works. The morning cup of Black Mud is how most of M88 starts its day.",
    story_hooks: [
      "Grindhouse has quietly started adding a mild cognitive enhancer to their blend — it's technically legal but undisclosed.",
      "A Black Mud shortage caused by supply chain disruption led to citywide irritability — the correlation was measurable in crime statistics."
    ],
    tags: ["street_food", "coffee_substitute", "consumer_good", "ubiquitous", "tier_1", "tier_2", "tier_3", "daily_ritual"]
  },
  {
    name: "Wonton Express Frozen Pack",
    type: "consumer_good",
    category: "street_food",
    subcategory: "frozen meal",
    manufacturer: "Golden Dragon Frozen Foods",
    description: "Bag of 20 frozen wontons, synth-pork and chive filling. Boil or steam in five minutes. A staple of Shelf and Tier 2 household cooking.",
    flavor_profile: "Savory, ginger-forward, with a chive brightness, the wrappers are thin and delicate when steamed",
    tier_availability: "Tier 1-3",
    price: "Φ2.80",
    popularity_rank: 11,
    slogan: "Twenty reasons to come home.",
    cultural_context: "Golden Dragon wontons are one of those products where the frozen version has become the default. Most people in M88 have never had a handmade wonton and don't know the difference.",
    story_hooks: [
      "A handmade wonton shop in Tier 2 is struggling to compete with Golden Dragon's prices — the owner refuses to use synth-protein and is going bankrupt."
    ],
    tags: ["street_food", "frozen_meal", "consumer_good", "wontons", "tier_1", "tier_2", "tier_3", "household"]
  },
  {
    name: "Shelf Curry Pack",
    type: "consumer_good",
    category: "street_food",
    subcategory: "flash-heated meal",
    manufacturer: "Atlas Meal Solutions",
    description: "Self-heating curry over rice. No frills, no branding charm — just a utilitarian white pouch with black text. Cheap, hot, filling. Atlas makes it for volume, not love.",
    flavor_profile: "Generic curry heat, turmeric and cumin dominant, the rice is mushy but absorbs the sauce adequately",
    tier_availability: "Tier 1-2",
    price: "Φ1.80",
    popularity_rank: 13,
    slogan: "Hot food. Φ1.80.",
    cultural_context: "Atlas Meal Solutions doesn't pretend to be anything other than what it is: cheap calories, heated. Their aesthetic is aggressively utilitarian. People respect the honesty.",
    story_hooks: [
      "Atlas is a subsidiary of the same conglomerate that makes NutriTubes — they own the entire bottom of the food pyramid."
    ],
    tags: ["street_food", "flash_heated", "consumer_good", "curry", "tier_1", "tier_2", "utilitarian"]
  },
  {
    name: "Skewer King Satay",
    type: "consumer_good",
    category: "street_food",
    subcategory: "real meat",
    manufacturer: "Skewer King (chain)",
    description: "Synth-chicken satay with peanut sauce from M88's largest street food chain. Not real meat but close enough, and the peanut sauce is genuinely good. Served from bright orange carts.",
    flavor_profile: "Char-grilled synth-chicken with a rich, sweet-spicy peanut sauce, served with compressed rice cubes",
    tier_availability: "Tier 2-3",
    price: "Φ4.00",
    popularity_rank: 16,
    slogan: "The King of the Street.",
    cultural_context: "Skewer King has over 400 carts across M88. The bright orange is recognizable from a block away. It's the McDonald's of street food — consistent, available, and nobody's favorite but everyone's fallback.",
    story_hooks: [
      "Skewer King's franchise model is exploitative — cart operators work 14-hour days and keep less than 30% of revenue."
    ],
    tags: ["street_food", "synth_meat", "consumer_good", "chain", "tier_2", "tier_3", "franchise"]
  },
  {
    name: "Harbor Catch Fish Ball",
    type: "consumer_good",
    category: "street_food",
    subcategory: "street snack",
    manufacturer: "Old Harbor Fish Collective",
    description: "Deep-fried fish balls made from actual harbor-caught fish, served on skewers with sweet chili sauce. Sold from steaming carts along the harbor walk. You eat them hot, standing up, looking at the water.",
    flavor_profile: "Crispy exterior, bouncy fish paste interior, briny and fresh, the sweet chili sauce is the perfect complement",
    tier_availability: "Tier 1-2",
    price: "Φ2.00",
    popularity_rank: 21,
    slogan: "Fresh from the harbor.",
    cultural_context: "Fish balls are Old Harbor's signature food. The fish is real — caught from the polluted harbor waters and processed enough that it's safe. Probably. The Collective insists it's tested. Most people choose not to ask too many questions.",
    story_hooks: [
      "Harbor water contamination levels have been rising — the fish are technically still safe to eat but the margin is shrinking.",
      "The Fish Collective is one of Old Harbor's few economic engines — threatening it threatens the community."
    ],
    tags: ["street_food", "fish", "consumer_good", "old_harbor", "tier_1", "tier_2", "harbor", "real_food"]
  },
  {
    name: "Atlas Rice Bowl — Teriyaki",
    type: "consumer_good",
    category: "street_food",
    subcategory: "flash-heated meal",
    manufacturer: "Atlas Meal Solutions",
    description: "Self-heating rice bowl with teriyaki sauce and synth-protein chunks. The teriyaki is sweet and salty in that factory-precise way. It's fine. It's food.",
    flavor_profile: "Sweet soy glaze over bland protein cubes, sticky rice, a hint of ginger in the sauce",
    tier_availability: "Tier 1-2",
    price: "Φ2.00",
    popularity_rank: 23,
    slogan: "Fuel up. Move on.",
    cultural_context: "Atlas teriyaki bowls are what you eat when you don't care what you eat but you need to eat something. They're caloric, they're warm, and they're everywhere.",
    story_hooks: [
      "Atlas's teriyaki sauce recipe hasn't changed in 15 years — it's generated by an optimization algorithm that maximizes palatability per unit cost."
    ],
    tags: ["street_food", "flash_heated", "consumer_good", "teriyaki", "tier_1", "tier_2", "utilitarian"]
  },
  {
    name: "Mama Obi's Pepper Soup Kit",
    type: "consumer_good",
    category: "street_food",
    subcategory: "flash-heated meal",
    manufacturer: "Mama Obi Foods",
    description: "Self-heating pepper soup with goat-flavor synth-protein and yam cubes. Intensely spiced. A diaspora comfort food that warms from the inside out.",
    flavor_profile: "Fiery, aromatic, with uda and uziza spice notes, the broth is thin but deeply flavored, yam cubes are starchy and satisfying",
    tier_availability: "Tier 2-3",
    price: "Φ4.00",
    popularity_rank: 26,
    slogan: "Mama's cure for everything.",
    cultural_context: "Pepper soup is medicine-food in West African culture. When you're sick, cold, heartbroken, or hungover, someone hands you pepper soup. Mama Obi's is the packaged version of that care.",
    story_hooks: [
      "Mama Obi is a Tier 2 grandmother who actually runs the company — she refuses to sell to any conglomerate and her family operation is fiercely independent."
    ],
    tags: ["street_food", "flash_heated", "consumer_good", "pepper_soup", "tier_2", "tier_3", "west_african", "comfort_food"]
  },
  {
    name: "DimSum Express Har Gow Pack",
    type: "consumer_good",
    category: "street_food",
    subcategory: "frozen meal",
    manufacturer: "DimSum Express Ltd.",
    description: "Frozen shrimp dumplings, six per tray, with a disposable steaming insert. Add water, microwave or heat, and you have passable har gow in four minutes.",
    flavor_profile: "Translucent wrapper with a springy shrimp-paste filling, subtle sesame oil, served with a soy-vinegar dip packet",
    tier_availability: "Tier 2-3",
    price: "Φ4.50",
    popularity_rank: 29,
    slogan: "Sunday morning, any day.",
    cultural_context: "Dim sum culture persists in M88, but most people can't afford to sit down at a restaurant. DimSum Express packages are the weekday substitute — eaten at home, pretending it's a proper dim sum morning.",
    story_hooks: [
      "The 'shrimp' is vat-grown crustacean protein — real shrimp hasn't been commercially available in M88 for a decade."
    ],
    tags: ["street_food", "frozen_meal", "consumer_good", "dim_sum", "tier_2", "tier_3"]
  },
  {
    name: "Flatbread Factory Garlic Naan",
    type: "consumer_good",
    category: "street_food",
    subcategory: "bread",
    manufacturer: "Flatbread Factory",
    description: "Sealed pack of four shelf-stable garlic naan. Soft, pliable, and genuinely garlicky. Used as a plate, a wrap, a utensil, and sometimes just eaten plain walking down the street.",
    flavor_profile: "Soft, chewy, aggressively garlicky with a buttery sheen from synth-ghee",
    tier_availability: "Tier 1-3",
    price: "Φ1.50",
    popularity_rank: 15,
    slogan: "Wrap anything.",
    cultural_context: "Flatbread Factory naan is the universal edible platform of M88. People wrap Kanto Blocks in it. They wrap NutriTube paste in it. They wrap leftovers in it. It makes everything slightly more dignified.",
    story_hooks: [
      "Flatbread Factory's preservative allows 90-day shelf life — nobody knows exactly what it is and the formula is classified as a trade secret."
    ],
    tags: ["street_food", "bread", "consumer_good", "naan", "tier_1", "tier_2", "tier_3", "ubiquitous", "versatile"]
  },
  {
    name: "Uncle Chen's Tea Egg",
    type: "consumer_good",
    category: "street_food",
    subcategory: "street snack",
    manufacturer: "Chen Family Provisions",
    description: "Vacuum-sealed tea egg, marbled dark from soy and star anise brine. Sold individually at convenience counters. A perfect portable protein that fits in your pocket.",
    flavor_profile: "Salty, subtly sweet, infused with star anise and soy, the yolk is jammy and rich",
    tier_availability: "Tier 1-3",
    price: "Φ0.80",
    popularity_rank: 10,
    slogan: "One egg. One good thing.",
    cultural_context: "Tea eggs are one of those small, perfect foods that every tier consumes. Tier 1 residents eat them for protein. Tier 3 residents eat them for nostalgia. They are everywhere and no one is tired of them.",
    story_hooks: [
      "Chen Family Provisions uses real eggs from actual chickens — they own one of the last small poultry operations inside city limits and the land is wanted for development."
    ],
    tags: ["street_food", "street_snack", "consumer_good", "tea_egg", "tier_1", "tier_2", "tier_3", "protein", "ubiquitous"]
  },
  {
    name: "Vat Jerky Original",
    type: "consumer_good",
    category: "street_food",
    subcategory: "synth meat snack",
    manufacturer: "TerraProtein Corp",
    description: "Strips of dried vat-grown beef analog, seasoned and smoked. Chewy, salty, portable. The default protein snack for laborers and anyone doing physical work.",
    flavor_profile: "Smoky, peppery, aggressively salty, chewy texture that lasts, slight sweetness in the glaze",
    tier_availability: "Tier 1-3",
    price: "Φ2.50",
    popularity_rank: 18,
    slogan: "Chew on this.",
    cultural_context: "Vat Jerky is working-class fuel. You see it in the pockets of dock workers, mechanics, and anyone who needs calories they can eat one-handed while working.",
    story_hooks: [
      "TerraProtein's vat facilities have been cited for contamination violations three times — each time the fine was cheaper than fixing the problem."
    ],
    tags: ["street_food", "synth_meat", "consumer_good", "jerky", "tier_1", "tier_2", "tier_3", "labor", "protein"]
  },
  {
    name: "Algae Cracker Sheets",
    type: "consumer_good",
    category: "street_food",
    subcategory: "snack",
    manufacturer: "Old Harbor Collective Kitchen",
    description: "Paper-thin sheets of dried, seasoned algae. Crispy, salty, faintly oceanic. Sold in flat packs of ten sheets. The seaweed snack of M88.",
    flavor_profile: "Crispy, intensely umami, sesame-oil-kissed, dissolves on the tongue with a salt finish",
    tier_availability: "Tier 1-3",
    price: "Φ0.60",
    popularity_rank: 14,
    slogan: "Thin. Crisp. Real.",
    cultural_context: "Algae crackers are what children eat as their first snack. They're packed in school lunches, eaten at desks, and crumbled over noodles. They are the cheapest real food in M88 — made from harbor algae that grows endlessly.",
    story_hooks: [
      "The algae used for crackers is the same strain as the bioluminescent Glow Wraps — processed differently, it loses the glow but keeps the nutrition."
    ],
    tags: ["street_food", "snack", "consumer_good", "algae", "old_harbor", "tier_1", "tier_2", "tier_3", "cheap", "ubiquitous"]
  },
  {
    name: "QuickBowl Dal",
    type: "consumer_good",
    category: "street_food",
    subcategory: "flash-heated meal",
    manufacturer: "Atlas Meal Solutions",
    description: "Self-heating lentil dal with rice and a papadum crisp. Vegetarian by default and by far the most calorie-efficient self-heating meal available. The dal is thick, warming, and honest.",
    flavor_profile: "Earthy lentils with cumin and turmeric, slightly smoky, the rice absorbs the dal perfectly, papadum adds crunch",
    tier_availability: "Tier 1-3",
    price: "Φ1.60",
    popularity_rank: 11,
    slogan: "Simple. Full. Done.",
    cultural_context: "QuickBowl Dal is the cheapest self-heating meal that doesn't taste like compromise. Aid workers distribute it during crises because it's filling, cheap, and culturally acceptable across most of M88's demographics.",
    story_hooks: [
      "The lentils are one of the few crops still grown in soil within M88's agricultural zones — the farming cooperative that supplies Atlas is under pressure to switch to vat production."
    ],
    tags: ["street_food", "flash_heated", "consumer_good", "dal", "vegetarian", "tier_1", "tier_2", "tier_3", "affordable"]
  },
  {
    name: "Grillmaster Synth-Lamb Kebab",
    type: "consumer_good",
    category: "street_food",
    subcategory: "street snack",
    manufacturer: "Grillmaster Street Foods",
    description: "Synth-lamb chunks on a skewer with onion and pepper, grilled on vertical rotisserie carts. Served in flatbread with garlic sauce. The Tier 2 night market staple.",
    flavor_profile: "Charred, fatty, heavily spiced with cumin and sumac, the garlic sauce is pungent and creamy",
    tier_availability: "Tier 2-3",
    price: "Φ5.00",
    popularity_rank: 19,
    slogan: "Fire and flavor.",
    cultural_context: "Grillmaster carts are the anchors of night markets. The vertical rotisserie glow and the smell of charring spiced meat draw crowds. It's dinner-and-entertainment — you eat standing in the crowd, watching the grill turn.",
    story_hooks: [
      "Grillmaster's 'synth-lamb' recently tested positive for traces of actual animal protein — either contamination or fraud, and either answer has consequences."
    ],
    tags: ["street_food", "synth_meat", "consumer_good", "kebab", "night_market", "tier_2", "tier_3"]
  },
  {
    name: "Protein Crumble Bar",
    type: "consumer_good",
    category: "street_food",
    subcategory: "synth-protein bar",
    manufacturer: "NutriCore Labs",
    description: "A step above Kanto Blocks — oat and insect-protein crumble bar with a honey-flavored drizzle. Crunchy, satisfying, and positioned as the 'healthy choice' in a market where survival is the baseline.",
    flavor_profile: "Oaty, slightly sweet, crunchy with visible cricket flour granules, the honey drizzle is synthetic but pleasant",
    tier_availability: "Tier 2-3",
    price: "Φ2.20",
    popularity_rank: 16,
    slogan: "Better fuel.",
    cultural_context: "Protein Crumble Bars are what you graduate to when you can afford not to eat Kanto Blocks. They're the first rung of eating for pleasure rather than pure survival.",
    story_hooks: [
      "NutriCore's 'healthy choice' marketing has been challenged — the sugar content is higher than Kanto Blocks, just better disguised."
    ],
    tags: ["street_food", "synth_protein", "consumer_good", "protein_bar", "tier_2", "tier_3", "health"]
  },
  {
    name: "Night Market Baozi",
    type: "consumer_good",
    category: "street_food",
    subcategory: "street snack",
    manufacturer: "Various vendors",
    description: "Steamed buns with synth-pork and cabbage filling, sold from bamboo steamers at night market stalls. The steam clouds, the smell, the soft white bun — it's an experience as much as a food.",
    flavor_profile: "Fluffy, soft dough encasing savory, gingery pork filling, juicy and fragrant when fresh",
    tier_availability: "Tier 2-3",
    price: "Φ1.50",
    popularity_rank: 13,
    slogan: "No slogan — they sell themselves by smell.",
    cultural_context: "Baozi vendors are the heart of any night market. The sight of bamboo steamers stacked six high, steam pouring into the night air, is one of M88's most iconic images. Everyone has a favorite vendor.",
    story_hooks: [
      "A legendary baozi vendor known only as 'Uncle Steam' has been operating in the same spot for 30 years — the spot is now marked for transit expansion."
    ],
    tags: ["street_food", "street_snack", "consumer_good", "baozi", "night_market", "tier_2", "tier_3", "steam"]
  },

  // =====================================================
  // HYGIENE & PERSONAL CARE (30)
  // =====================================================

  {
    name: "ChromeShine Augment Polish",
    type: "consumer_good",
    category: "hygiene",
    subcategory: "augment care",
    manufacturer: "CyberCare Products",
    description: "Cleaning and polishing solution for exposed chrome augmentations. Removes oxidation, fingerprints, and grime. Leaves a mirror finish. Used daily by anyone with visible prosthetics.",
    flavor_profile: "Sharp chemical smell with a hint of citrus, dries to a streak-free shine",
    tier_availability: "Tier 1-4",
    price: "Φ4.50",
    popularity_rank: 5,
    slogan: "Shine like you mean it.",
    cultural_context: "Keeping your chrome clean is a social signal. Dull, grimy augments say you've given up. Polished chrome says you still care. ChromeShine is the market leader — the blue bottle is recognizable everywhere.",
    story_hooks: [
      "ChromeShine's formula contains a nano-coating that subtly degrades competitor prosthetic surfaces — a class-action lawsuit is building.",
      "The company has a data-collection clause buried in their terms — scanning the augment during cleaning uploads telemetry to CyberCare's servers."
    ],
    tags: ["hygiene", "augment_care", "consumer_good", "chrome", "prosthetic", "tier_1", "tier_2", "tier_3", "tier_4", "daily"]
  },
  {
    name: "DermaSoft Synth-Skin Moisturizer",
    type: "consumer_good",
    category: "hygiene",
    subcategory: "skin care",
    manufacturer: "DermaSoft Biocosmetics",
    description: "Moisturizing cream formulated for synthetic skin grafts and bio-printed skin. Prevents cracking, maintains elasticity, and reduces the visible seam between synth and natural skin.",
    flavor_profile: "Unscented by default, absorbs quickly, leaves no residue, faint clinical smell",
    tier_availability: "Tier 2-4",
    price: "Φ8.00",
    popularity_rank: 12,
    slogan: "Where you end and begin.",
    cultural_context: "Anyone with synth-skin grafts — which is a significant portion of M88's population — needs this or something like it. Without moisturizer, synth-skin cracks, peels, and looks obviously artificial. DermaSoft is the premium option.",
    story_hooks: [
      "DermaSoft's moisturizer interacts badly with a common black-market synth-skin brand, causing accelerated degradation — the company knows but hasn't issued a warning because the black-market product is 'not their problem.'"
    ],
    tags: ["hygiene", "skin_care", "consumer_good", "synth_skin", "augment", "tier_2", "tier_3", "tier_4", "daily"]
  },
  {
    name: "TailSilk Conditioner",
    type: "consumer_good",
    category: "hygiene",
    subcategory: "geneware care",
    manufacturer: "FurForm Geneware Cosmetics",
    description: "Conditioner specifically formulated for geneware tails — fox, cat, and other phenotypes. Detangles, conditions, and adds a healthy sheen. Comes in the distinctive pink bottle found in every geneware household's shower.",
    flavor_profile: "Light cherry blossom scent, silky texture, rinses clean without residue",
    tier_availability: "Tier 2-4",
    price: "Φ6.00",
    popularity_rank: 15,
    slogan: "Because it's part of you.",
    cultural_context: "Tail care is as normal as hair care in M88. TailSilk is the brand people grow up with. The pink bottle is an icon. Geneware kids beg their parents for the 'sparkle edition' with holographic packaging.",
    story_hooks: [
      "TailSilk has been accused of engineering their formula to cause mild dryness if you stop using it — creating dependency on the product.",
      "A competing brand markets 'natural tail care' and has been running a smear campaign suggesting TailSilk causes fur discoloration."
    ],
    tags: ["hygiene", "geneware_care", "consumer_good", "tail", "fur", "tier_2", "tier_3", "tier_4", "daily"]
  },
  {
    name: "HornGloss Premium Polish",
    type: "consumer_good",
    category: "hygiene",
    subcategory: "geneware care",
    manufacturer: "FurForm Geneware Cosmetics",
    description: "Polishing paste for geneware horns — ram, antler, and other keratinous growths. Fills micro-scratches, restores natural luster, and protects against UV damage.",
    flavor_profile: "Faint beeswax scent, thick paste consistency, buffs to a warm glow",
    tier_availability: "Tier 2-4",
    price: "Φ7.50",
    popularity_rank: 22,
    slogan: "Wear them proud.",
    cultural_context: "Horn maintenance is a weekly ritual for those who have them. Well-maintained horns are a point of pride. Chipped, dull horns are socially read as neglect or hardship. HornGloss is the standard — barber shops stock it.",
    story_hooks: [
      "A trend of 'horn modding' — carving decorative patterns into geneware horns — has created a market for specialty polishes that HornGloss is scrambling to enter."
    ],
    tags: ["hygiene", "geneware_care", "consumer_good", "horn", "keratin", "tier_2", "tier_3", "tier_4"]
  },
  {
    name: "StickPatch 72hr Deodorant",
    type: "consumer_good",
    category: "hygiene",
    subcategory: "deodorant",
    manufacturer: "BodyTech Consumer",
    description: "Transparent dermal patch that adheres to the armpit and provides 72 hours of odor neutralization through sustained-release antimicrobial compounds. Peel, stick, forget.",
    flavor_profile: "No scent — that's the point. Complete odor elimination.",
    tier_availability: "Tier 2-4",
    price: "Φ3.00",
    popularity_rank: 8,
    slogan: "Three days. Zero smell.",
    cultural_context: "Traditional deodorant still exists but StickPatch has captured the market through sheer convenience. Apply Monday, replace Thursday. The transparent patch is invisible under clothing. People who still use spray deodorant are considered old-fashioned.",
    story_hooks: [
      "StickPatch's adhesive occasionally causes skin reactions in people with certain geneware modifications — a recall is being quietly considered.",
      "Counterfeit StickPatches with shorter effective periods are flooding Tier 1-2 markets."
    ],
    tags: ["hygiene", "deodorant", "consumer_good", "dermal_patch", "tier_2", "tier_3", "tier_4", "convenience"]
  },
  {
    name: "NeuroSwab BCI Cleaning Kit",
    type: "consumer_good",
    category: "hygiene",
    subcategory: "BCI care",
    manufacturer: "CortexCare Medical",
    description: "Pack of 30 individually sealed swabs for cleaning BCI contact points and neural interface ports. Alcohol-free, anti-static, and essential for preventing infection at the chrome-flesh interface.",
    flavor_profile: "Faint saline smell, slightly cool on contact, dries instantly",
    tier_availability: "Tier 1-4",
    price: "Φ5.00",
    popularity_rank: 4,
    slogan: "Clean connection. Clear mind.",
    cultural_context: "BCI cleaning is non-negotiable hygiene. Dirty contact points cause signal degradation, infection risk, and neural noise. NeuroSwabs are prescribed by every BCI installer and stocked in every bathroom with a BCI user. Using off-brand swabs is considered risky.",
    story_hooks: [
      "CortexCare has lobbied to make their swabs the only 'certified' BCI cleaning product — competitors are locked out of hospital distribution channels.",
      "A Shelf community health worker has developed an effective DIY alternative using common ingredients — CortexCare wants it suppressed."
    ],
    tags: ["hygiene", "bci_care", "consumer_good", "neural_interface", "cleaning", "tier_1", "tier_2", "tier_3", "tier_4", "essential"]
  },
  {
    name: "BoundaryEase Augment Rejection Cream",
    type: "consumer_good",
    category: "hygiene",
    subcategory: "augment care",
    manufacturer: "PharmaClear Inc.",
    description: "Topical anti-inflammatory cream applied at the junction where prosthetic augments meet organic tissue. Reduces redness, swelling, and the chronic itch that plagues many augmented people.",
    flavor_profile: "Cooling menthol sensation, thick white cream, absorbs slowly — the cooling effect lasts about two hours",
    tier_availability: "Tier 1-4",
    price: "Φ12.00",
    popularity_rank: 9,
    slogan: "Where chrome meets skin.",
    cultural_context: "Almost everyone with augments experiences some degree of boundary irritation. BoundaryEase is the over-the-counter solution — stronger prescriptions exist but cost ten times more. Running out of BoundaryEase is a small crisis for augmented people.",
    story_hooks: [
      "PharmaClear deliberately prices BoundaryEase at the edge of affordability for Tier 1 users — just expensive enough to hurt, just cheap enough that they buy it instead of suffering.",
      "The cream contains a compound that slightly accelerates the body's rejection of non-PharmaClear-certified augments — creating demand for their more expensive products."
    ],
    tags: ["hygiene", "augment_care", "consumer_good", "anti_inflammatory", "chrome", "tier_1", "tier_2", "tier_3", "tier_4", "essential"]
  },
  {
    name: "AquaPure Water Purification Tablets",
    type: "consumer_good",
    category: "hygiene",
    subcategory: "water treatment",
    manufacturer: "Meridian Basic Services",
    description: "Effervescent tablets that purify one liter of water each. Essential in the Shelf where water infrastructure is unreliable. Drop the tablet, wait ten minutes, drink safely.",
    flavor_profile: "Slight chlorine taste that dissipates after 30 minutes, leaves water clear",
    tier_availability: "Tier 1-2",
    price: "Φ0.10",
    popularity_rank: 1,
    slogan: "Safe water. Every time.",
    cultural_context: "In the Shelf, AquaPure tablets are as fundamental as breathing. Water from Shelf taps is not reliably safe. Everyone carries tablets. Running out is a genuine emergency. They're distributed free during health crises but normally cost money. Φ0.10 per liter adds up when you're already poor.",
    story_hooks: [
      "Meridian Basic Services has a monopoly on water purification — they've blocked cheaper alternatives from reaching the market through regulatory capture.",
      "The tablets work, but long-term use of the purification compound may have health effects that haven't been studied because studying them would threaten the product."
    ],
    tags: ["hygiene", "water", "consumer_good", "purification", "shelf", "tier_1", "tier_2", "essential", "survival"]
  },
  {
    name: "BrightBite Teeth Whitener",
    type: "consumer_good",
    category: "hygiene",
    subcategory: "dental care",
    manufacturer: "DentaClear Consumer",
    description: "Dissolving strip that whitens both natural and synthetic teeth. Works on ceramic, composite, and enamel surfaces. Apply for 15 minutes, dissolve, done.",
    flavor_profile: "Strong mint with a slight chemical tingle, dissolves to nothing",
    tier_availability: "Tier 2-4",
    price: "Φ4.00",
    popularity_rank: 18,
    slogan: "Every tooth. Every smile.",
    cultural_context: "In a world where some people have ceramic teeth and others have natural ones, BrightBite positioned itself as the universal solution. Their marketing never specifies which type of teeth you have — everyone's smile matters. This was considered groundbreaking.",
    story_hooks: [
      "BrightBite's chemical compound erodes a specific brand of budget dental prosthetic — they know, and they've been quietly buying that competitor."
    ],
    tags: ["hygiene", "dental", "consumer_good", "teeth", "whitener", "tier_2", "tier_3", "tier_4"]
  },
  {
    name: "FurDye Vivid Collection",
    type: "consumer_good",
    category: "hygiene",
    subcategory: "geneware cosmetics",
    manufacturer: "ChromaFur Inc.",
    description: "Semi-permanent dye for geneware fur, available in 24 colors. Apply, wait 20 minutes, rinse. Lasts 4-6 weeks. From natural tones to neon — fur dyeing is fashion.",
    flavor_profile: "Sharp chemical smell during application, faint floral after rinsing, vibrant color payoff",
    tier_availability: "Tier 2-4",
    price: "Φ8.00",
    popularity_rank: 20,
    slogan: "Your fur. Your rules.",
    cultural_context: "Fur dyeing is a major fashion expression for geneware people. Natural fur color is considered 'default' — dyeing it is self-expression. Neon colors signal youth culture. Subtle highlights signal sophistication. There are entire BCI feeds dedicated to fur color trends.",
    story_hooks: [
      "ChromaFur's neon blue dye has been linked to an allergic reaction cluster — the compound interacts with a popular geneware expression stabilizer.",
      "A geneware rights group argues that fur dyeing reinforces the idea that geneware appearances need to be 'improved' — it's a culture war."
    ],
    tags: ["hygiene", "geneware_cosmetics", "consumer_good", "fur", "dye", "fashion", "tier_2", "tier_3", "tier_4"]
  },
  {
    name: "ScentShift Body Modulator",
    type: "consumer_good",
    category: "hygiene",
    subcategory: "scent modulation",
    manufacturer: "OlfaTech",
    description: "Ingestible capsule that modulates body chemistry to produce a chosen scent profile for 24 hours. Available in 'Cedar,' 'Rain,' 'Vanilla,' and 'Null' (no scent at all). Not perfume — your body actually produces the scent.",
    flavor_profile: "Small capsule, no taste, effects begin within 30 minutes",
    tier_availability: "Tier 3-5",
    price: "Φ15.00",
    popularity_rank: 30,
    slogan: "Don't wear a scent. Become one.",
    cultural_context: "ScentShift is a Tier 3+ luxury that's becoming mainstream. The 'Null' variant is popular with people who want complete scent neutrality for professional settings. 'Rain' is the bestseller. People who use ScentShift consider traditional perfume primitive.",
    story_hooks: [
      "ScentShift's 'Null' variant is used by operatives who need to leave no scent trace — it has quiet military and intelligence applications.",
      "Long-term use of ScentShift has been linked to olfactory nerve changes — users lose the ability to smell themselves naturally."
    ],
    tags: ["hygiene", "scent", "consumer_good", "modulation", "capsule", "tier_3", "tier_4", "tier_5", "luxury"]
  },
  {
    name: "ClearPort Interface Cleanser",
    type: "consumer_good",
    category: "hygiene",
    subcategory: "augment care",
    manufacturer: "CyberCare Products",
    description: "Spray cleanser for data ports, charging sockets, and external augment interfaces. Removes dust, moisture, and bio-film buildup that can cause connectivity issues.",
    flavor_profile: "Alcohol-based, evaporates instantly, sharp clean smell",
    tier_availability: "Tier 1-4",
    price: "Φ3.50",
    popularity_rank: 11,
    slogan: "Clean ports. Clean signal.",
    cultural_context: "Port cleaning is weekly maintenance for augmented people. A clogged port causes data lag, charging failures, and in serious cases, electrical shorts. ClearPort is the standard — any augment maintenance kit includes it.",
    story_hooks: [
      "ClearPort's formula is nearly identical to a generic electronics cleaner that costs one-third the price — the branding is doing all the work."
    ],
    tags: ["hygiene", "augment_care", "consumer_good", "interface", "cleaning", "tier_1", "tier_2", "tier_3", "tier_4"]
  },
  {
    name: "ShelfSoap Universal Bar",
    type: "consumer_good",
    category: "hygiene",
    subcategory: "soap",
    manufacturer: "Meridian Basic Services",
    description: "Dense bar of all-purpose soap. Washes body, hair, and clothing. Lasts a month with daily use. Harsh but effective. The default hygiene product of the Shelf.",
    flavor_profile: "Sharp, industrial clean smell, lathers reluctantly but cleans thoroughly",
    tier_availability: "Tier 1-2",
    price: "Φ0.50",
    popularity_rank: 3,
    slogan: "Clean is clean.",
    cultural_context: "ShelfSoap is not pleasant but it works. Using it means you maintain hygiene despite poverty. The smell is distinctive — people who escaped the Shelf can still identify it years later. It triggers memories.",
    story_hooks: [
      "ShelfSoap's formula hasn't changed since 2065 — attempts to improve it are blocked because the current version is maximally profitable."
    ],
    tags: ["hygiene", "soap", "consumer_good", "shelf", "tier_1", "tier_2", "basic", "survival"]
  },
  {
    name: "GeneGuard Expression Lotion",
    type: "consumer_good",
    category: "hygiene",
    subcategory: "geneware care",
    manufacturer: "BioStable Health",
    description: "Daily-use lotion that helps stabilize geneware expression — prevents unwanted fur growth patterns, scale discoloration, and feature drift. Applied like moisturizer.",
    flavor_profile: "Mild, clinical, absorbs completely, no residue on fur or scales",
    tier_availability: "Tier 2-4",
    price: "Φ10.00",
    popularity_rank: 14,
    slogan: "Stay you.",
    cultural_context: "Geneware isn't always stable — expressions can drift over years, causing unwanted changes. GeneGuard is the daily maintenance that keeps you looking like you chose to look. Skipping it is risky. It's expensive enough that Tier 1 geneware people often can't afford it, leading to visible expression drift that marks their economic status.",
    story_hooks: [
      "GeneGuard is manufactured by the same company that sells the geneware modifications — they profit from the instability they engineered.",
      "A generic version at half the price has been blocked from market by patent litigation."
    ],
    tags: ["hygiene", "geneware_care", "consumer_good", "expression", "stabilizer", "tier_2", "tier_3", "tier_4", "daily"]
  },
  {
    name: "RustGuard Chrome Skin Protectant",
    type: "consumer_good",
    category: "hygiene",
    subcategory: "augment care",
    manufacturer: "CyberCare Products",
    description: "Protective coating spray for chrome augments exposed to moisture and corrosive environments. Particularly important in Old Harbor where salt air accelerates oxidation. One application lasts a week.",
    flavor_profile: "Chemical spray, slight metallic sheen when dry, no scent after application",
    tier_availability: "Tier 1-3",
    price: "Φ6.00",
    popularity_rank: 16,
    slogan: "Weather any storm.",
    cultural_context: "Rust on augments is painful, embarrassing, and dangerous. In Old Harbor, RustGuard is as essential as food. The salt air corrodes cheap chrome in weeks without protection. Seeing someone with rusty augments tells you they can't afford Φ6 and that tells you everything.",
    story_hooks: [
      "RustGuard's formula was reverse-engineered from military-grade anti-corrosion tech — the military wants licensing fees."
    ],
    tags: ["hygiene", "augment_care", "consumer_good", "rust_prevention", "chrome", "old_harbor", "tier_1", "tier_2", "tier_3"]
  },
  {
    name: "NailTech Augmented Manicure Kit",
    type: "consumer_good",
    category: "hygiene",
    subcategory: "nail care",
    manufacturer: "BodyTech Consumer",
    description: "Manicure kit designed for people with mixed natural and prosthetic fingers. Includes files for both keratin and composite materials, cuticle care for organic fingers, and polishing compounds for prosthetic ones.",
    flavor_profile: "Functional, no scent, compact carrying case",
    tier_availability: "Tier 2-4",
    price: "Φ9.00",
    popularity_rank: 28,
    slogan: "Every finger matters.",
    cultural_context: "Nail care across the organic-prosthetic divide is a daily reality for millions. This kit acknowledges that your hands might be half-and-half and that's just how it is. The marketing is matter-of-fact, not aspirational.",
    story_hooks: [
      "NailTech's compact case has a hidden compartment that's become popular for smuggling micro-data chips — the company didn't design it that way but hasn't fixed it."
    ],
    tags: ["hygiene", "nail_care", "consumer_good", "augment", "prosthetic", "tier_2", "tier_3", "tier_4"]
  },
  {
    name: "FreshBreeze Air Purifier Mask",
    type: "consumer_good",
    category: "hygiene",
    subcategory: "respiratory",
    manufacturer: "AirSafe Corp",
    description: "Disposable face mask with activated carbon filter. Rated for 8 hours of use in polluted environments. Standard gear for Shelf and Old Harbor residents who work outside.",
    flavor_profile: "Slight carbon taste, clean filtered air, elastic straps",
    tier_availability: "Tier 1-3",
    price: "Φ0.30",
    popularity_rank: 6,
    slogan: "Breathe safe.",
    cultural_context: "Air quality in the lower tiers is bad enough that masks are just part of getting dressed. Children learn to put them on before they learn to tie shoes. The FreshBreeze brand is the cheapest effective option.",
    story_hooks: [
      "AirSafe publishes air quality data that consistently shows conditions as 'moderate' — independent measurements suggest they're understating pollution by 40% to sell fewer premium masks."
    ],
    tags: ["hygiene", "respiratory", "consumer_good", "mask", "filter", "shelf", "old_harbor", "tier_1", "tier_2", "tier_3", "daily"]
  },
  {
    name: "ScaleCare Reptilian Moisturizer",
    type: "consumer_good",
    category: "hygiene",
    subcategory: "geneware care",
    manufacturer: "FurForm Geneware Cosmetics",
    description: "Moisturizing oil specifically formulated for geneware scale expressions. Prevents cracking, maintains iridescence, and keeps scales supple. The reptilian equivalent of skin lotion.",
    flavor_profile: "Light, non-greasy oil with a faint tropical scent, absorbs into scale keratin",
    tier_availability: "Tier 2-4",
    price: "Φ7.00",
    popularity_rank: 24,
    slogan: "Scales that shine.",
    cultural_context: "Scale-expression geneware requires different care than fur or feather types. ScaleCare filled a gap the market ignored for years. Before it, scale-type geneware people improvised with products not designed for them.",
    story_hooks: [
      "ScaleCare was developed by a geneware person with scale expression who couldn't find any product that worked — FurForm bought her formula and she now works in their R&D department."
    ],
    tags: ["hygiene", "geneware_care", "consumer_good", "scales", "moisturizer", "tier_2", "tier_3", "tier_4"]
  },
  {
    name: "QuickRinse Waterless Shampoo",
    type: "consumer_good",
    category: "hygiene",
    subcategory: "hair care",
    manufacturer: "BodyTech Consumer",
    description: "Spray-on shampoo that cleans hair without water. Spray, massage, towel off. Essential in the Shelf where water access is limited and rationed.",
    flavor_profile: "Light powder scent, leaves hair feeling dry-clean, slight residue if overused",
    tier_availability: "Tier 1-3",
    price: "Φ2.00",
    popularity_rank: 10,
    slogan: "No water? No problem.",
    cultural_context: "Water is precious in the Shelf. Showering daily is a luxury. QuickRinse lets people maintain hair hygiene without using their water ration. It's not as good as real washing but it's better than nothing, and for many people it's the normal way to clean hair.",
    story_hooks: [
      "QuickRinse's market dominance in the Shelf depends on water scarcity continuing — the company has lobbied against water infrastructure improvements."
    ],
    tags: ["hygiene", "hair_care", "consumer_good", "waterless", "shelf", "tier_1", "tier_2", "tier_3", "water_scarcity"]
  },
  {
    name: "FeatherSoft Preening Oil",
    type: "consumer_good",
    category: "hygiene",
    subcategory: "geneware care",
    manufacturer: "FurForm Geneware Cosmetics",
    description: "Conditioning oil for geneware feather expressions. Maintains barb structure, prevents brittleness, and enhances natural color. Applied with a specialized preening comb included in starter kits.",
    flavor_profile: "Warm, slightly nutty oil, lightweight, absorbed by feather keratin within minutes",
    tier_availability: "Tier 2-4",
    price: "Φ9.00",
    popularity_rank: 26,
    slogan: "Feathers worth showing.",
    cultural_context: "Feather-type geneware is less common than fur but has a devoted community. Preening is a social activity — feather-type geneware people often preen each other in communal grooming sessions that serve as social bonding.",
    story_hooks: [
      "A feather-type geneware influencer has been promoting a DIY preening oil recipe that's cheaper than FeatherSoft — FurForm is threatening legal action over 'unsafe cosmetic practices.'"
    ],
    tags: ["hygiene", "geneware_care", "consumer_good", "feather", "preening", "tier_2", "tier_3", "tier_4"]
  },
  {
    name: "JunctionSeal Prosthetic Barrier Cream",
    type: "consumer_good",
    category: "hygiene",
    subcategory: "augment care",
    manufacturer: "PharmaClear Inc.",
    description: "Waterproof barrier cream applied at prosthetic-organic junctions before showering, swimming, or working in wet conditions. Prevents water infiltration that can cause infection and electrical issues.",
    flavor_profile: "Thick, waxy, forms a visible seal, removed with ClearPort cleanser",
    tier_availability: "Tier 1-4",
    price: "Φ5.00",
    popularity_rank: 17,
    slogan: "Sealed tight.",
    cultural_context: "Water and chrome don't mix well at the boundary. JunctionSeal is pre-shower routine for augmented people. Forgetting it once is uncomfortable. Forgetting it repeatedly causes medical problems.",
    story_hooks: [
      "JunctionSeal and ClearPort are made by different companies but function as a mandatory pair — there's speculation they coordinated to create mutual dependency."
    ],
    tags: ["hygiene", "augment_care", "consumer_good", "barrier", "waterproof", "prosthetic", "tier_1", "tier_2", "tier_3", "tier_4"]
  },
  {
    name: "EarTech Hearing Augment Wax Remover",
    type: "consumer_good",
    category: "hygiene",
    subcategory: "augment care",
    manufacturer: "CortexCare Medical",
    description: "Specialized drops for cleaning hearing augments and cochlear interfaces. Dissolves earwax buildup around augmented ear components without damaging electronics.",
    flavor_profile: "Warm when applied, fizzes gently as it dissolves wax, slight medicinal smell",
    tier_availability: "Tier 2-4",
    price: "Φ4.00",
    popularity_rank: 21,
    slogan: "Hear everything.",
    cultural_context: "Hearing augments are among the most common prosthetics — affordable, life-improving, and nearly invisible. Maintenance is simple but necessary. EarTech drops are a bathroom staple.",
    story_hooks: [
      "EarTech's formula interacts with a new generation of hearing augments to produce a barely perceptible tone that some users find maddening — the manufacturer blames the augment maker, who blames EarTech."
    ],
    tags: ["hygiene", "augment_care", "consumer_good", "hearing", "ear", "cleaning", "tier_2", "tier_3", "tier_4"]
  },

  // =====================================================
  // HOUSEHOLD & CLEANING (20)
  // =====================================================

  {
    name: "AirCycle Replacement Filter",
    type: "consumer_good",
    category: "household",
    subcategory: "air filtration",
    manufacturer: "AtmoTech Systems",
    description: "Standard replacement filter for residential air recyclers. Every dwelling in M88 has an air recycler; every recycler needs a new filter every 90 days. This is the most-purchased household item in the city.",
    flavor_profile: "Dense carbon-fiber mesh, sealed in foil until installation, no scent when new",
    tier_availability: "Tier 1-4",
    price: "Φ8.00",
    popularity_rank: 1,
    slogan: "Breathe. Replace. Repeat.",
    cultural_context: "Air recycler filter replacement is a universal household chore. Overdue filters turn the air stale, thick, and unhealthy. In the Shelf, people stretch filters past their rated life because Φ8 every 90 days adds up. The air gets bad. Everyone pretends not to notice.",
    story_hooks: [
      "AtmoTech's filters are deliberately designed to degrade on schedule — independent tests show they could last twice as long with minor design changes.",
      "A Shelf co-op has started a filter-washing service that extends filter life by 50% — AtmoTech is threatening patent infringement."
    ],
    tags: ["household", "air_filtration", "consumer_good", "filter", "essential", "tier_1", "tier_2", "tier_3", "tier_4", "ubiquitous"]
  },
  {
    name: "MoldStop Inhibitor Spray",
    type: "consumer_good",
    category: "household",
    subcategory: "cleaning",
    manufacturer: "CleanZone Products",
    description: "Anti-fungal spray critical in Old Harbor and lower Shelf levels where humidity and poor ventilation create constant mold problems. Spray on walls, ceilings, and surfaces. Lasts 30 days per application.",
    flavor_profile: "Harsh chemical smell that fades to nothing after drying, leaves a slight film",
    tier_availability: "Tier 1-2",
    price: "Φ3.00",
    popularity_rank: 4,
    slogan: "Kill it before it grows.",
    cultural_context: "Mold is the enemy in Old Harbor. Black mold in the lungs has hospitalized thousands. MoldStop is not optional — it's survival. People spray their entire living space monthly. The smell of MoldStop is the smell of home in Old Harbor.",
    story_hooks: [
      "CleanZone's formula is effective but the long-term respiratory effects of the spray itself are unstudied — you're trading one lung problem for a slower one.",
      "A bio-engineered mold strain resistant to MoldStop has appeared in Shelf Block 12 — CleanZone is scrambling to reformulate."
    ],
    tags: ["household", "cleaning", "consumer_good", "mold", "anti_fungal", "old_harbor", "shelf", "tier_1", "tier_2", "essential"]
  },
  {
    name: "SurfaceKill Sanitizer Wipes",
    type: "consumer_good",
    category: "household",
    subcategory: "cleaning",
    manufacturer: "CleanZone Products",
    description: "Pack of 50 multi-surface sanitizing wipes. Kills 99.8% of pathogens including bio-engineered strains. Used on counters, door handles, shared surfaces, and augment charging stations.",
    flavor_profile: "Alcohol-based, sharp clean smell, dries quickly",
    tier_availability: "Tier 1-4",
    price: "Φ2.50",
    popularity_rank: 6,
    slogan: "Wipe it clean.",
    cultural_context: "Sanitation is serious in a city this dense. SurfaceKill wipes are everywhere — homes, workplaces, transit vehicles. The sound of someone pulling a wipe from the pack is constant background noise in public spaces.",
    story_hooks: [
      "SurfaceKill's 'bio-engineered strain' effectiveness was tested only against known strains — novel pathogens from the Shelf's biohacking community may be resistant."
    ],
    tags: ["household", "cleaning", "consumer_good", "sanitizer", "wipes", "tier_1", "tier_2", "tier_3", "tier_4"]
  },
  {
    name: "ChromeHome Rust Treatment",
    type: "consumer_good",
    category: "household",
    subcategory: "maintenance",
    manufacturer: "CyberCare Products",
    description: "Household rust treatment for chrome surfaces — door handles, augment charging cradles, prosthetic storage racks, and any other chrome fixtures in augmented households. Removes existing rust and prevents recurrence for 60 days.",
    flavor_profile: "Acidic, pungent during application, neutralizes to no smell after treatment",
    tier_availability: "Tier 1-3",
    price: "Φ5.00",
    popularity_rank: 13,
    slogan: "Home maintenance for the augmented life.",
    cultural_context: "Augmented households have chrome everywhere — not just on their bodies but in their furniture, their fixtures, their tools. ChromeHome is the household version of RustGuard, formulated for surfaces rather than skin-adjacent augments.",
    story_hooks: [
      "ChromeHome and RustGuard are the same company selling essentially the same formula at different price points for 'household' vs. 'personal' use."
    ],
    tags: ["household", "maintenance", "consumer_good", "rust", "chrome", "tier_1", "tier_2", "tier_3"]
  },
  {
    name: "HydroCart Water Recycler Cartridge",
    type: "consumer_good",
    category: "household",
    subcategory: "water treatment",
    manufacturer: "AquaSystems M88",
    description: "Replacement cartridge for residential water recyclers. Filters and purifies greywater back to potable standard. Replace every 60 days. Without it, your water recycler produces water you shouldn't drink.",
    flavor_profile: "Carbon and resin filtration media, sealed in sterile packaging",
    tier_availability: "Tier 1-3",
    price: "Φ12.00",
    popularity_rank: 2,
    slogan: "Your water, renewed.",
    cultural_context: "Water recycling is mandatory in lower tiers. The cartridge replacement cycle is a constant expense. HydroCart has 70% market share and prices accordingly. People in the Shelf sometimes share recyclers between households to split cartridge costs.",
    story_hooks: [
      "AquaSystems' cartridges contain a proprietary filtration medium that only they manufacture — attempts to create generic alternatives have been blocked by patents.",
      "Used HydroCart cartridges are supposed to be returned for safe disposal but many end up in harbor dumps, leaching accumulated contaminants."
    ],
    tags: ["household", "water_treatment", "consumer_good", "recycler", "cartridge", "tier_1", "tier_2", "tier_3", "essential"]
  },
  {
    name: "LumiGlow Algae Lamp",
    type: "consumer_good",
    category: "household",
    subcategory: "lighting",
    manufacturer: "BioLight Designs",
    description: "Decorative container of bioluminescent algae that provides soft blue-green ambient light. No electricity required — just shake gently to activate. Feed weekly with the included nutrient drops. Living light.",
    flavor_profile: "Soft blue-green glow, faint oceanic smell, gentle ambient light equivalent to a candle",
    tier_availability: "Tier 2-4",
    price: "Φ15.00",
    popularity_rank: 18,
    slogan: "Light that lives.",
    cultural_context: "LumiGlow lamps are beloved. They're alive, they're beautiful, and they don't cost electricity. In the Shelf, they're aspirational — a small luxury that transforms a bleak space. In Tier 3, they're standard decor. The soft blue-green glow visible through windows is an M88 signature.",
    story_hooks: [
      "BioLight's algae strain was developed by Old Harbor bio-engineers who received a one-time licensing fee — the company earns millions annually from their work.",
      "Some people have emotional attachments to their LumiGlow — killing the algae through neglect feels like a small death."
    ],
    tags: ["household", "lighting", "consumer_good", "bioluminescent", "algae", "decor", "tier_2", "tier_3", "tier_4", "living"]
  },
  {
    name: "AtmoScent Room Pod — Rain",
    type: "consumer_good",
    category: "household",
    subcategory: "air freshener",
    manufacturer: "OlfaTech",
    description: "Small pod that releases a continuous scent into a room for 30 days. 'Rain' is the bestseller — a petrichor scent that makes any room smell like the aftermath of a storm. In a city where real rain is rare and usually acidic, it's nostalgia for something most residents have never experienced.",
    flavor_profile: "Petrichor, ozone, wet earth — the perfect memory of rain",
    tier_availability: "Tier 2-4",
    price: "Φ5.00",
    popularity_rank: 15,
    slogan: "The weather you choose.",
    cultural_context: "AtmoScent pods are everywhere. People have strong opinions about scent choices. 'Rain' is safe, universally liked. 'Forest' is for pretentious people. 'Ocean' is for Old Harbor expats. Your room scent says something about you.",
    story_hooks: [
      "OlfaTech's 'Rain' scent was reverse-engineered from an archival recording of pre-industrial petrichor — the real thing no longer exists anywhere on Earth.",
      "AtmoScent pods contain trace mood-modulators that aren't listed on the ingredients — the 'calm' you feel isn't just the scent."
    ],
    tags: ["household", "air_freshener", "consumer_good", "scent", "decor", "tier_2", "tier_3", "tier_4"]
  },
  {
    name: "AtmoScent Room Pod — Forest",
    type: "consumer_good",
    category: "household",
    subcategory: "air freshener",
    manufacturer: "OlfaTech",
    description: "Pine, cedar, and moss scent profile. Makes your 40-square-meter apartment smell like a place where trees grow. Popular with people who have never seen a forest.",
    flavor_profile: "Coniferous, woody, with a damp moss undertone and a hint of bark",
    tier_availability: "Tier 2-4",
    price: "Φ5.00",
    popularity_rank: 22,
    slogan: "The weather you choose.",
    cultural_context: "People who use 'Forest' are quietly mocked by people who use 'Rain.' It's considered trying too hard. But the people who love it really love it, and some apartments layer it with LumiGlow lamps to create small green sanctuaries.",
    story_hooks: [
      "A study found that people who use 'Forest' scent pods score higher on depression metrics — unclear if the scent attracts depressed people or if the synthetic forest compounds have neurological effects."
    ],
    tags: ["household", "air_freshener", "consumer_good", "scent", "forest", "decor", "tier_2", "tier_3", "tier_4"]
  },
  {
    name: "BugOut Insect Barrier Spray",
    type: "consumer_good",
    category: "household",
    subcategory: "pest control",
    manufacturer: "CleanZone Products",
    description: "Perimeter spray that creates an invisible chemical barrier against cockroaches, mosquitoes, and gene-modded pest variants. Spray around windows, doors, and vents. Reapply weekly.",
    flavor_profile: "Faint chemical smell during application, undetectable after drying",
    tier_availability: "Tier 1-3",
    price: "Φ2.50",
    popularity_rank: 7,
    slogan: "Your space. No visitors.",
    cultural_context: "Insects in M88 are persistent, numerous, and in some cases gene-modded escapees from research facilities. BugOut is baseline pest control. The Shelf has insect problems that BugOut can only slow, not stop.",
    story_hooks: [
      "A gene-modded cockroach variant that's BugOut-resistant has been spreading through Shelf Block 9 — it's a minor ecological crisis.",
      "BugOut's active ingredient bioaccumulates in the harbor water system — it's contributing to aquatic ecosystem damage."
    ],
    tags: ["household", "pest_control", "consumer_good", "insect", "spray", "tier_1", "tier_2", "tier_3"]
  },
  {
    name: "PowerCell Universal Battery Pack",
    type: "consumer_good",
    category: "household",
    subcategory: "power",
    manufacturer: "VoltWorks",
    description: "Rechargeable battery pack compatible with most household devices, portable electronics, and small augment chargers. Holds 10,000mAh equivalent. The universal power brick of M88.",
    flavor_profile: "Dense, heavy for its size, warm when charging, indicator light shows charge level",
    tier_availability: "Tier 1-4",
    price: "Φ10.00",
    popularity_rank: 5,
    slogan: "Power when you need it.",
    cultural_context: "Power reliability varies by tier. In the Shelf, outages are frequent and PowerCells are lifelines. People charge them opportunistically — at work, at transit stations, anywhere with a socket. A dead PowerCell is a minor emergency.",
    story_hooks: [
      "VoltWorks batteries have a firmware-limited lifespan — they stop holding charge after 18 months regardless of actual cell condition. A hacker community has developed a firmware bypass.",
      "Counterfeit PowerCells with unstable chemistry have caused fires in Shelf housing blocks."
    ],
    tags: ["household", "power", "consumer_good", "battery", "charging", "tier_1", "tier_2", "tier_3", "tier_4", "essential"]
  },
  {
    name: "SealTight Humidity Control Pack",
    type: "consumer_good",
    category: "household",
    subcategory: "climate control",
    manufacturer: "AtmoTech Systems",
    description: "Passive humidity absorption pack for small spaces. Absorbs excess moisture from closets, storage areas, and sleeping quarters. Replace when the indicator strip turns blue. Essential in Old Harbor.",
    flavor_profile: "No smell, silica-based, changes from white to blue when saturated",
    tier_availability: "Tier 1-3",
    price: "Φ1.50",
    popularity_rank: 9,
    slogan: "Dry is safe.",
    cultural_context: "Humidity destroys everything in the lower tiers — clothing molds, electronics short, augments corrode. SealTight packs are tucked into every corner of Shelf and Old Harbor dwellings. Finding them all blue means you're losing the war against moisture.",
    story_hooks: [
      "AtmoTech sells both the air filters and the humidity packs — they've cornered the atmospheric control market for residential spaces."
    ],
    tags: ["household", "climate_control", "consumer_good", "humidity", "moisture", "old_harbor", "shelf", "tier_1", "tier_2", "tier_3"]
  },
  {
    name: "FixAll Multi-Surface Adhesive",
    type: "consumer_good",
    category: "household",
    subcategory: "repair",
    manufacturer: "QuickFix Consumer",
    description: "Industrial-strength adhesive that bonds metal, plastic, ceramic, and synth-skin. The universal repair tool of M88. If it's broken, you FixAll it before you can afford to replace it.",
    flavor_profile: "Sharp solvent smell, sets in 60 seconds, cures completely in 24 hours",
    tier_availability: "Tier 1-4",
    price: "Φ3.00",
    popularity_rank: 8,
    slogan: "Broken? Fixed.",
    cultural_context: "FixAll is the duct tape of M88. Every household has a tube. People repair augments with it, seal leaks with it, fix furniture with it. In the Shelf, FixAll keeps life literally held together. The tube is always half-empty.",
    story_hooks: [
      "FixAll's formula is actually over-engineered for consumer use — it was originally a military prosthetic adhesive that found a wider market."
    ],
    tags: ["household", "repair", "consumer_good", "adhesive", "tier_1", "tier_2", "tier_3", "tier_4", "ubiquitous"]
  },
  {
    name: "CleanAir Personal Fan Filter",
    type: "consumer_good",
    category: "household",
    subcategory: "air quality",
    manufacturer: "AtmoTech Systems",
    description: "Small desktop fan with integrated HEPA filter. Provides a cone of clean air for one person's breathing zone. Plugs into any USB-compatible power source. The poor person's air purifier.",
    flavor_profile: "Quiet hum, slight clean ozone taste, moves a gentle breeze",
    tier_availability: "Tier 1-3",
    price: "Φ15.00",
    popularity_rank: 14,
    slogan: "Your air. Your zone.",
    cultural_context: "In the Shelf, full-room air recyclers are often shared between families. A personal fan filter gives you a small zone of clean air at your desk or bedside. It's individualized survival in a shared-resource environment.",
    story_hooks: [
      "AtmoTech markets the personal fan to individuals but its filter creates a dead zone that pulls clean air from the shared room environment — one person's clean air is everyone else's dirtier air."
    ],
    tags: ["household", "air_quality", "consumer_good", "fan", "filter", "shelf", "tier_1", "tier_2", "tier_3"]
  },
  {
    name: "LumiGlow Nutrient Drops",
    type: "consumer_good",
    category: "household",
    subcategory: "living decor maintenance",
    manufacturer: "BioLight Designs",
    description: "Weekly feeding solution for LumiGlow algae lamps. Three drops per lamp. Without feeding, the algae dims and dies within two weeks. The recurring cost of living light.",
    flavor_profile: "Faintly green liquid, slight mineral smell, packaged in a dropper bottle",
    tier_availability: "Tier 2-4",
    price: "Φ3.00",
    popularity_rank: 19,
    slogan: "Keep the light alive.",
    cultural_context: "Feeding your LumiGlow is a weekly ritual. People talk to their lamps. They name them. Forgetting to feed it and watching the glow fade is genuinely sad. BioLight sells the lamps at near-cost and profits on the nutrient drops — the razor-and-blades model.",
    story_hooks: [
      "An Old Harbor biologist has published a free recipe for LumiGlow nutrients using common algae supplements — BioLight is threatening a lawsuit."
    ],
    tags: ["household", "maintenance", "consumer_good", "bioluminescent", "algae", "nutrient", "tier_2", "tier_3", "tier_4", "recurring"]
  },

  // =====================================================
  // STIMULANTS & FOCUS (20)
  // =====================================================

  {
    name: "SparkTab Stimulant Tablet",
    type: "consumer_good",
    category: "stimulant",
    subcategory: "energy tablet",
    manufacturer: "NeuroVolt Pharmaceuticals",
    description: "Small white tablet providing 6 hours of enhanced alertness and energy. Stronger than caffeine, milder than prescription stimulants. The most commonly consumed stimulant in M88.",
    flavor_profile: "Bitter if chewed, chalky, designed to be swallowed whole with water",
    tier_availability: "Tier 1-4",
    price: "Φ1.00",
    popularity_rank: 2,
    slogan: "Six more hours.",
    cultural_context: "SparkTabs are the coffee of M88 — except they work better and everyone knows the crash is coming. Workers pop them at the start of shifts. Students pop them before exams. The question isn't whether you use SparkTabs but how many per day.",
    story_hooks: [
      "NeuroVolt has been gradually increasing SparkTab potency by 2% annually — each individual tablet seems the same but the cumulative tolerance effect drives higher consumption.",
      "A cluster of cardiac events among Shelf laborers has been linked to SparkTab overuse — NeuroVolt's response is that 'recommended dosage' warnings are clearly printed."
    ],
    tags: ["stimulant", "energy", "consumer_good", "tablet", "tier_1", "tier_2", "tier_3", "tier_4", "ubiquitous", "daily"]
  },
  {
    name: "FocusBite Concentration Gum",
    type: "consumer_good",
    category: "stimulant",
    subcategory: "focus aid",
    manufacturer: "CogniChew Labs",
    description: "Chewing gum infused with a nootropic compound that enhances concentration for 2-3 hours. Chew for 5 minutes to release the active compound, then it works while you work. Popular with data workers and students.",
    flavor_profile: "Mild mint, slightly bitter undertone from the nootropic, firm texture that softens during the active release phase",
    tier_availability: "Tier 2-4",
    price: "Φ3.00",
    popularity_rank: 8,
    slogan: "Chew. Focus. Finish.",
    cultural_context: "FocusBite is the white-collar stimulant — you see it on every desk in Tier 3-4 offices. The rhythmic chewing is a workplace soundtrack. People who chew FocusBite consider themselves above SparkTab users, though the distinction is mostly aesthetic.",
    story_hooks: [
      "FocusBite's nootropic has a mild addictive quality that CogniChew claims is 'within regulatory parameters' — users who stop report difficulty concentrating at all.",
      "A student discovered that combining FocusBite with certain BCI configurations produces a synesthetic effect — colors become sounds, which is either a bug or a feature."
    ],
    tags: ["stimulant", "focus", "consumer_good", "gum", "nootropic", "tier_2", "tier_3", "tier_4", "office", "student"]
  },
  {
    name: "CrashKit Recovery Tablets",
    type: "consumer_good",
    category: "stimulant",
    subcategory: "recovery",
    manufacturer: "NeuroVolt Pharmaceuticals",
    description: "Four-tablet kit for recovering from stimulant crashes. Restores electrolytes, stabilizes blood sugar, and provides a mild anxiolytic. The companion product to SparkTabs — NeuroVolt profits on both the ride and the landing.",
    flavor_profile: "Effervescent, orange-flavored, dissolves in water to create a fizzy recovery drink",
    tier_availability: "Tier 1-4",
    price: "Φ2.50",
    popularity_rank: 10,
    slogan: "Soft landing.",
    cultural_context: "The SparkTab-CrashKit cycle is M88's most common drug routine. Pop SparkTabs for energy, take CrashKit to recover, repeat. NeuroVolt sells both. The irony is not lost on anyone but no one has a better option.",
    story_hooks: [
      "CrashKit's anxiolytic component is the same compound found in mood candy at a lower dose — it creates a subtle emotional dependency on the recovery cycle.",
      "A Shelf health worker has been advocating for SparkTab regulation using CrashKit sales data to demonstrate the scope of stimulant dependency — NeuroVolt's lawyers have sent cease-and-desist letters."
    ],
    tags: ["stimulant", "recovery", "consumer_good", "tablet", "electrolyte", "tier_1", "tier_2", "tier_3", "tier_4"]
  },
  {
    name: "ClarityDrop Neural Eye Drops",
    type: "consumer_good",
    category: "stimulant",
    subcategory: "neural enhancement",
    manufacturer: "CortexCare Medical",
    description: "Eye drops that sharpen BCI visual response time by 15% for 4 hours. Applied directly to the eyes, they optimize the neural pathway between retinal implants and visual cortex processors.",
    flavor_profile: "Cool, slight tingling, brief blue-shift in vision for 30 seconds after application",
    tier_availability: "Tier 2-4",
    price: "Φ8.00",
    popularity_rank: 14,
    slogan: "See sharper. Think faster.",
    cultural_context: "ClarityDrops are popular with gamers, combat professionals, and anyone whose work requires fast visual processing. The brief blue-shift after application is a known tell — people can see you've just dosed.",
    story_hooks: [
      "ClarityDrops are banned in competitive BCI gaming but impossible to test for after the active period — the cheating is endemic.",
      "Long-term use causes subtle changes to color perception that may be permanent — the 'ClarityDrop look' where the world seems slightly desaturated without them."
    ],
    tags: ["stimulant", "neural", "consumer_good", "eye_drops", "bci", "vision", "tier_2", "tier_3", "tier_4"]
  },
  {
    name: "DreamPatch Sleep Assist",
    type: "consumer_good",
    category: "stimulant",
    subcategory: "sleep aid",
    manufacturer: "SomnaWell Health",
    description: "Dermal patch applied to the inner wrist that releases a melatonin-analog compound over 8 hours. Induces natural-feeling sleep within 20 minutes. Peels off clean in the morning.",
    flavor_profile: "No taste, slight cooling sensation at the application site, patch is translucent and discreet",
    tier_availability: "Tier 2-4",
    price: "Φ4.00",
    popularity_rank: 11,
    slogan: "Tonight, you sleep.",
    cultural_context: "Insomnia is epidemic in M88. BCI activity, stimulant use, light pollution, noise, and stress all conspire against sleep. DreamPatch is the over-the-counter solution. Millions use it nightly. The alternative is lying awake listening to your BCI hum.",
    story_hooks: [
      "DreamPatch's melatonin analog has been linked to unusually vivid dreams that some users find disturbing — a BCI forum community is mapping the dream patterns.",
      "SomnaWell's data shows that DreamPatch users develop tolerance within 6 months and need to double the dose — the company considers this 'normal adaptation.'"
    ],
    tags: ["stimulant", "sleep", "consumer_good", "patch", "melatonin", "tier_2", "tier_3", "tier_4", "nightly"]
  },
  {
    name: "AwakeAll 24hr Wake Tablet",
    type: "consumer_good",
    category: "stimulant",
    subcategory: "wakefulness agent",
    manufacturer: "NeuroVolt Pharmaceuticals",
    description: "Extended-release wakefulness tablet that eliminates the need for sleep for 24 hours without the jitters, crash, or cognitive impairment of traditional stimulants. The price is paid later — you need 10 hours of sleep to recover.",
    flavor_profile: "Large white tablet, no taste, swallowed whole, effects begin within 30 minutes",
    tier_availability: "Tier 2-4",
    price: "Φ12.00",
    popularity_rank: 16,
    slogan: "Tomorrow can wait.",
    cultural_context: "AwakeAll is for emergencies, deadlines, and desperation. It's not a daily drug — it's a 'the project is due in 18 hours' drug. Heavy use is socially stigmatized. Having a box of AwakeAll in your medicine cabinet is normal. Having an empty box is a warning sign.",
    story_hooks: [
      "AwakeAll use among Tier 2 shift workers is rising — double shifts on AwakeAll are technically illegal but employers look the other way.",
      "The 10-hour recovery sleep after AwakeAll produces a neural state that some users describe as 'the clearest thinking of their lives' — a black market has emerged for post-AwakeAll cognitive work."
    ],
    tags: ["stimulant", "wakefulness", "consumer_good", "tablet", "extended_release", "tier_2", "tier_3", "tier_4", "emergency"]
  },
  {
    name: "CalmVapor Anxiety Inhaler",
    type: "consumer_good",
    category: "stimulant",
    subcategory: "anxiolytic",
    manufacturer: "SomnaWell Health",
    description: "Small inhaler that delivers a micro-dose anxiolytic vapor. Two puffs reduce anxiety within 60 seconds without sedation. 100 doses per inhaler. The panic button in your pocket.",
    flavor_profile: "Cool, faintly lavender-scented vapor, gentle throat sensation, no visible exhalation",
    tier_availability: "Tier 2-4",
    price: "Φ15.00",
    popularity_rank: 13,
    slogan: "Breathe. Again.",
    cultural_context: "Anxiety is so common in M88 that CalmVapor inhalers are carried like keys and wallets. Pulling one out in a meeting isn't stigmatized — it's just someone managing their neurochemistry. The lavender puff before a difficult conversation is a shared human moment.",
    story_hooks: [
      "CalmVapor's compound is chemically similar to a controlled substance at higher doses — the line between 'wellness product' and 'drug' is regulatory, not pharmacological.",
      "A trend of 'stacking' CalmVapor with FocusBite produces a state of calm hyper-focus that users call 'the zone' — it's effective but the long-term neurological effects are unknown."
    ],
    tags: ["stimulant", "anxiolytic", "consumer_good", "inhaler", "vapor", "tier_2", "tier_3", "tier_4", "mental_health"]
  },
  {
    name: "SparkTab Max",
    type: "consumer_good",
    category: "stimulant",
    subcategory: "energy tablet",
    manufacturer: "NeuroVolt Pharmaceuticals",
    description: "Double-strength SparkTab for when regular SparkTabs aren't enough anymore. 12 hours of wakefulness. The tolerance escalation product that NeuroVolt will never admit is necessary because of their original formula.",
    flavor_profile: "Same bitter chalk, larger tablet, red-scored for identification",
    tier_availability: "Tier 1-3",
    price: "Φ2.00",
    popularity_rank: 9,
    slogan: "When one isn't enough.",
    cultural_context: "SparkTab Max is where casual use becomes dependency. The upgrade from SparkTab to Max is a one-way trip for most people. NeuroVolt markets it as 'for high-demand situations' but everyone knows it's the tolerance product.",
    story_hooks: [
      "Health advocates have tried to require warning labels on SparkTab Max linking it to cardiac risk — NeuroVolt's regulatory capture has blocked every attempt."
    ],
    tags: ["stimulant", "energy", "consumer_good", "tablet", "high_dose", "tier_1", "tier_2", "tier_3", "dependency"]
  },
  {
    name: "BrainDrip Nootropic Sachet",
    type: "consumer_good",
    category: "stimulant",
    subcategory: "nootropic",
    manufacturer: "CogniChew Labs",
    description: "Powder sachet dissolved in water to create a nootropic drink. Enhances working memory and processing speed for 4 hours. Tastes like slightly medicinal fruit punch. Popular with knowledge workers.",
    flavor_profile: "Tart, fruity, with a medicinal undertone, dissolves into a slightly cloudy pink liquid",
    tier_availability: "Tier 3-4",
    price: "Φ6.00",
    popularity_rank: 19,
    slogan: "Think more. Think better.",
    cultural_context: "BrainDrip is the prestige nootropic — the one you see on Tier 4 desks next to the real coffee. It signals that your work requires enhanced cognition, which signals that your work matters. The pink drink is a status marker.",
    story_hooks: [
      "BrainDrip's cognitive enhancement is real but modest — about 8% improvement in controlled studies. The marketing implies much more.",
      "A hacker community has figured out how to synthesize BrainDrip's active compound from commonly available chemicals at one-tenth the cost."
    ],
    tags: ["stimulant", "nootropic", "consumer_good", "drink", "cognitive", "tier_3", "tier_4", "knowledge_work"]
  },
  {
    name: "NerveSteady Performance Drops",
    type: "consumer_good",
    category: "stimulant",
    subcategory: "performance aid",
    manufacturer: "CortexCare Medical",
    description: "Sublingual drops that stabilize hand tremors, reduce performance anxiety, and steady fine motor control for 3 hours. Used by surgeons, technicians, and anyone whose hands need to be perfectly still.",
    flavor_profile: "Slightly sweet, absorbed under the tongue in 30 seconds, faint numbness at the application site",
    tier_availability: "Tier 3-5",
    price: "Φ20.00",
    popularity_rank: 25,
    slogan: "Steady hands. Steady work.",
    cultural_context: "NerveSteady is a professional tool, not a recreational drug. Surgeons use it. Prosthetic installers use it. Fine electronics technicians use it. It's expensive because the people who need it can afford it. It's also quietly used by augmented combat professionals.",
    story_hooks: [
      "NerveSteady is on the restricted list for competitive marksmanship but widely used by security contractors — the enforcement gap is deliberate.",
      "A generic version would cost Φ3 per dose but CortexCare's patent runs until 2094."
    ],
    tags: ["stimulant", "performance", "consumer_good", "sublingual", "precision", "tier_3", "tier_4", "tier_5", "professional"]
  },
  {
    name: "MoodLift Mild Antidepressant Gum",
    type: "consumer_good",
    category: "stimulant",
    subcategory: "mood support",
    manufacturer: "SomnaWell Health",
    description: "Chewing gum that delivers a low-dose serotonin-reuptake modulator. Not strong enough to be classified as a prescription antidepressant, but enough to take the edge off. Chew one piece daily.",
    flavor_profile: "Mild spearmint, soft texture, the chemical taste is barely detectable",
    tier_availability: "Tier 2-4",
    price: "Φ4.00",
    popularity_rank: 12,
    slogan: "A little better. Every day.",
    cultural_context: "MoodLift occupies the gray zone between supplement and medication. It doesn't fix depression, but it makes the day slightly more bearable. Millions chew it daily. The fact that a city's baseline emotional management comes in gum form says everything about M88.",
    story_hooks: [
      "MoodLift's classification as a 'wellness supplement' rather than a medication means it bypasses pharmaceutical oversight — the compound would require a prescription if it were in pill form.",
      "SomnaWell's internal research shows MoodLift is less effective than they claim but the placebo effect accounts for most of the benefit — and the placebo effect is real, so does it matter?"
    ],
    tags: ["stimulant", "mood", "consumer_good", "gum", "antidepressant", "tier_2", "tier_3", "tier_4", "daily", "mental_health"]
  },
  {
    name: "RushPatch Adrenaline Micro-Dose",
    type: "consumer_good",
    category: "stimulant",
    subcategory: "adrenaline",
    manufacturer: "NeuroVolt Pharmaceuticals",
    description: "Dermal patch that provides a controlled micro-dose adrenaline release over 2 hours. Legal, regulated, and popular with athletes, performers, and people who want to feel more alive.",
    flavor_profile: "No taste, slight warmth at the application site, elevated heart rate within 10 minutes",
    tier_availability: "Tier 3-4",
    price: "Φ10.00",
    popularity_rank: 22,
    slogan: "Feel everything.",
    cultural_context: "RushPatch is the recreational stimulant for people who don't want to admit they use recreational stimulants. It's 'performance enhancement.' It's 'living fully.' It's a legal way to feel a thrill in a life that's mostly transit and screens.",
    story_hooks: [
      "RushPatch combined with BCI-enabled virtual reality creates an immersive experience that's becoming addictive — users seek increasingly extreme virtual scenarios while patched.",
      "NeuroVolt's three major products — SparkTab, CrashKit, and RushPatch — form a cycle of productivity, recovery, and sensation that captures users across their entire day."
    ],
    tags: ["stimulant", "adrenaline", "consumer_good", "patch", "recreational", "tier_3", "tier_4"]
  },
  {
    name: "ZenDrop Meditation Aid",
    type: "consumer_good",
    category: "stimulant",
    subcategory: "relaxation",
    manufacturer: "SomnaWell Health",
    description: "Sublingual drops that enhance meditative states by reducing neural noise. Three drops under the tongue, wait five minutes, and your mind is quieter. Used by practitioners and people who just want their BCI to shut up for an hour.",
    flavor_profile: "Slightly bitter, herbal, absorbed quickly, leaves a warm sensation",
    tier_availability: "Tier 2-4",
    price: "Φ7.00",
    popularity_rank: 24,
    slogan: "Quiet mind.",
    cultural_context: "ZenDrop is the antidote to BCI overstimulation. In a world where your neural interface is always on, always feeding you data, ZenDrop provides something radical: silence. The growing 'neural silence' movement uses it as a tool for disconnection.",
    story_hooks: [
      "ZenDrop actually temporarily reduces BCI signal strength — which is why some people use it, and why some BCI manufacturers are concerned.",
      "A monastery in Tier 4 uses ZenDrop in their practice — the irony of using a pharmaceutical product to achieve 'natural' mental states is not lost on their critics."
    ],
    tags: ["stimulant", "meditation", "consumer_good", "sublingual", "relaxation", "bci", "tier_2", "tier_3", "tier_4"]
  },
  {
    name: "GrindGuard Jaw Tension Relief",
    type: "consumer_good",
    category: "stimulant",
    subcategory: "side effect management",
    manufacturer: "CortexCare Medical",
    description: "Dissolving tablet placed against the cheek that relaxes jaw muscles for 8 hours. For the millions of M88 residents who grind their teeth from stimulant use, stress, and BCI-related tension.",
    flavor_profile: "Mild vanilla, dissolves slowly against the cheek, gentle muscle relaxation",
    tier_availability: "Tier 2-4",
    price: "Φ3.00",
    popularity_rank: 17,
    slogan: "Unclench.",
    cultural_context: "Teeth grinding is so common in M88 that dental prosthetics are partly driven by it. GrindGuard is the maintenance product for a city that runs on tension. Dentists recommend it. SparkTab users need it.",
    story_hooks: [
      "GrindGuard is essentially treating a side effect of SparkTabs — and CortexCare and NeuroVolt are both owned by the same parent company."
    ],
    tags: ["stimulant", "side_effect", "consumer_good", "dental", "muscle_relaxant", "tier_2", "tier_3", "tier_4"]
  },
  {
    name: "IronWill Endurance Chew",
    type: "consumer_good",
    category: "stimulant",
    subcategory: "endurance",
    manufacturer: "NeuroVolt Pharmaceuticals",
    description: "Chewy tablet that extends physical endurance by reducing lactic acid buildup and pain perception for 6 hours. Used by laborers, athletes, and anyone doing extended physical work.",
    flavor_profile: "Tough, leathery texture, vaguely cola-flavored, takes 10 minutes of chewing to fully release",
    tier_availability: "Tier 1-3",
    price: "Φ2.00",
    popularity_rank: 15,
    slogan: "Keep going.",
    cultural_context: "IronWill is the laborer's drug. Dock workers chew it. Construction workers chew it. It lets you work longer and hurt less, which is exactly what employers want. The reduced pain perception means injuries go unnoticed — people push through damage they shouldn't.",
    story_hooks: [
      "IronWill's pain-masking effect has led to workplace injuries where workers didn't notice they were hurt until the chew wore off — some injuries were irreversible by then.",
      "Tier 2 employers have been caught distributing free IronWill to workers to increase productivity — technically legal, ethically indefensible."
    ],
    tags: ["stimulant", "endurance", "consumer_good", "chew", "labor", "pain", "tier_1", "tier_2", "tier_3"]
  },
  {
    name: "NightEye Visual Enhancement Drops",
    type: "consumer_good",
    category: "stimulant",
    subcategory: "sensory enhancement",
    manufacturer: "CortexCare Medical",
    description: "Eye drops that enhance low-light vision for 6 hours by boosting retinal implant sensitivity. For people working night shifts, navigating the Shelf's dark corridors, or operating in low-light environments.",
    flavor_profile: "Cool drops, brief stinging, pupils visibly dilate, enhanced starlight sensitivity",
    tier_availability: "Tier 2-4",
    price: "Φ6.00",
    popularity_rank: 20,
    slogan: "Own the dark.",
    cultural_context: "The Shelf and Old Harbor have unreliable lighting. NightEye drops are practical survival tools for navigating dark environments. Security workers and anyone who moves at night uses them. The dilated pupils are visible and signal to others that you can see in the dark.",
    story_hooks: [
      "NightEye drops make you photosensitive — sudden bright light while on them is painful and disorienting, which is a known tactical weakness.",
      "A modified version of NightEye that works on non-augmented eyes is circulating in black markets — the unregulated formula has a 5% chance of causing temporary blindness."
    ],
    tags: ["stimulant", "visual", "consumer_good", "eye_drops", "night_vision", "tier_2", "tier_3", "tier_4"]
  },

  // =====================================================
  // TOBACCO & VAPOR (15)
  // =====================================================

  {
    name: "VoltCloud Synth-Nicotine Cartridge — Classic",
    type: "consumer_good",
    category: "tobacco_vapor",
    subcategory: "synth-nicotine",
    manufacturer: "VoltCloud Inc.",
    description: "Standard synth-nicotine vapor cartridge compatible with all major vape devices. 'Classic' mimics traditional tobacco flavor. Each cartridge equals approximately 200 puffs.",
    flavor_profile: "Warm, slightly toasted, faintly sweet with a nicotine throat hit, dense vapor production",
    tier_availability: "Tier 1-4",
    price: "Φ3.00",
    popularity_rank: 3,
    slogan: "Your cloud. Your way.",
    cultural_context: "Vaping is ubiquitous in M88. VoltCloud is the market leader. The vapor clouds in transit stations, work areas, and social spaces are constant. Flavor choice is personal expression — Classic users are considered no-nonsense.",
    story_hooks: [
      "VoltCloud's synth-nicotine is engineered to be 20% more addictive than natural nicotine — the company's internal documents confirm this but they argue it's 'consumer preference optimization.'",
      "Used VoltCloud cartridges are a major waste problem — millions end up in the harbor monthly."
    ],
    tags: ["tobacco_vapor", "synth_nicotine", "consumer_good", "vape", "tier_1", "tier_2", "tier_3", "tier_4", "ubiquitous"]
  },
  {
    name: "VoltCloud Synth-Nicotine Cartridge — Mint Ice",
    type: "consumer_good",
    category: "tobacco_vapor",
    subcategory: "synth-nicotine",
    manufacturer: "VoltCloud Inc.",
    description: "Menthol-blast variant. The bestselling VoltCloud flavor. The cold mint vapor is visible in exhale even more than Classic, creating thicker clouds.",
    flavor_profile: "Intense menthol, arctic cold, numbs the throat slightly, massive vapor clouds",
    tier_availability: "Tier 1-4",
    price: "Φ3.00",
    popularity_rank: 1,
    slogan: "Your cloud. Your way.",
    cultural_context: "Mint Ice is M88's most popular vapor flavor by a significant margin. The menthol clouds are the default smell of public spaces. People who don't vape can still identify the Mint Ice exhale. It's the baseline scent of the city.",
    story_hooks: [
      "Mint Ice's menthol compound has a mild bronchodilator effect that makes users feel like they're breathing better after vaping — it's medicinal theater that drives consumption."
    ],
    tags: ["tobacco_vapor", "synth_nicotine", "consumer_good", "vape", "menthol", "tier_1", "tier_2", "tier_3", "tier_4"]
  },
  {
    name: "GreenLeaf Herbal Vapor Blend — Calm",
    type: "consumer_good",
    category: "tobacco_vapor",
    subcategory: "herbal vapor",
    manufacturer: "GreenLeaf Botanicals",
    description: "Nicotine-free herbal vapor cartridge infused with chamomile, lavender, and passionflower extracts. For people who want the ritual of vaping without the stimulant.",
    flavor_profile: "Floral, gentle, warm vapor with a calming herbal taste, thin vapor production",
    tier_availability: "Tier 2-4",
    price: "Φ4.00",
    popularity_rank: 14,
    slogan: "Vapor without the vice.",
    cultural_context: "GreenLeaf caters to people quitting synth-nicotine and people who never started but want to participate in the social ritual. Vaping is so normal that not vaping is mildly isolating. GreenLeaf gives you the gesture without the chemistry.",
    story_hooks: [
      "GreenLeaf's 'herbal' ingredients are geneware-modified plants with enhanced compound production — the 'natural' branding is misleading.",
      "Some GreenLeaf Calm users report it works better than CalmVapor for anxiety — the company can't make medical claims but their marketing comes close."
    ],
    tags: ["tobacco_vapor", "herbal", "consumer_good", "vape", "nicotine_free", "tier_2", "tier_3", "tier_4"]
  },
  {
    name: "FocusVapor BCI-Enhanced Cartridge",
    type: "consumer_good",
    category: "tobacco_vapor",
    subcategory: "enhanced vapor",
    manufacturer: "VoltCloud Inc.",
    description: "Vapor cartridge containing a mild nootropic compound that interacts with BCI pathways to enhance concentration. The vapor-meets-cognitive-enhancement product that blurs the line between vice and tool.",
    flavor_profile: "Clean, slightly metallic, with an ozone-like edge, moderate vapor production",
    tier_availability: "Tier 3-4",
    price: "Φ8.00",
    popularity_rank: 11,
    slogan: "Cloud your lungs. Clear your mind.",
    cultural_context: "FocusVapor is the prestige vape — used by people who want to signal that their vaping is functional, not recreational. The metallic taste is distinctive. Seeing someone puff FocusVapor in an office says 'I'm working, not slacking.'",
    story_hooks: [
      "FocusVapor's nootropic compound is the same one in FocusBite gum — VoltCloud licensed it from CogniChew and is now a direct competitor in the concentration-enhancement market.",
      "The BCI interaction is real but unpredictable — some users report their HUD flickering during use, which CortexCare blames on VoltCloud and VoltCloud blames on BCI firmware."
    ],
    tags: ["tobacco_vapor", "enhanced", "consumer_good", "vape", "nootropic", "bci", "tier_3", "tier_4"]
  },
  {
    name: "MoodMist Emotional Modulation Cartridge — Ease",
    type: "consumer_good",
    category: "tobacco_vapor",
    subcategory: "mood vapor",
    manufacturer: "SomnaWell Health",
    description: "Vapor cartridge delivering micro-dose emotional modulation compounds. 'Ease' provides mild relaxation and emotional softening. The most controversial consumer product in M88 — is it self-care or self-medication?",
    flavor_profile: "Warm, slightly sweet, almost imperceptible taste, minimal vapor — designed for discreet use",
    tier_availability: "Tier 3-4",
    price: "Φ10.00",
    popularity_rank: 18,
    slogan: "Feel what you choose.",
    cultural_context: "MoodMist is the product everyone has an opinion about. Supporters call it emotional self-regulation. Critics call it mood control in a cartridge. The truth is that millions use it quietly and the societal effects of widespread emotional modulation are only beginning to be understood.",
    story_hooks: [
      "MoodMist's 'Ease' variant has been detected in the bloodwork of a Tier 4 executive who claims to have never used it — someone is dosing their environment.",
      "SomnaWell's product line now covers sleep, anxiety, mood, and meditation — they are becoming the pharmacological manager of M88's emotional life."
    ],
    tags: ["tobacco_vapor", "mood", "consumer_good", "vape", "emotional_modulation", "tier_3", "tier_4", "controversial"]
  },
  {
    name: "MoodMist Emotional Modulation Cartridge — Bright",
    type: "consumer_good",
    category: "tobacco_vapor",
    subcategory: "mood vapor",
    manufacturer: "SomnaWell Health",
    description: "The 'up' variant. Provides mild euphoria and social confidence for 2-3 hours. The going-out-tonight cartridge. More potent than Ease and closer to the regulatory line.",
    flavor_profile: "Citric, bright, almost electric taste, slightly more vapor than Ease",
    tier_availability: "Tier 3-4",
    price: "Φ12.00",
    popularity_rank: 21,
    slogan: "Feel what you choose.",
    cultural_context: "Bright is the social cartridge. Pre-party, pre-date, pre-networking-event. People puff it in bathrooms before walking into rooms they're nervous about. The mild euphoria makes social interaction easier and everyone slightly more charming.",
    story_hooks: [
      "Bright's euphoric compound is one chemical substitution away from a controlled substance — SomnaWell's chemists maintain this distance deliberately and expensively.",
      "Combining Bright with alcohol produces unpredictable emotional states — some users become aggressive, which has led to incidents at Tier 3 nightlife venues."
    ],
    tags: ["tobacco_vapor", "mood", "consumer_good", "vape", "euphoria", "social", "tier_3", "tier_4"]
  },
  {
    name: "Shelf Rollup",
    type: "consumer_good",
    category: "tobacco_vapor",
    subcategory: "hand-rolled smoke",
    manufacturer: "Various Shelf vendors",
    description: "Hand-rolled cigarette made from synth-tobacco leaf and whatever herbal filler the vendor has available. Harsh, cheap, and smoked by people who can't afford or don't trust cartridge vapes.",
    flavor_profile: "Harsh, raw, unfiltered smoke with variable herbal notes depending on the batch, burns fast",
    tier_availability: "Tier 1",
    price: "Φ0.20",
    popularity_rank: 7,
    slogan: "No slogan. No brand. Just smoke.",
    cultural_context: "Shelf Rollups are the cigarette of the bottom tier. They're sold loose from jars, rolled on the spot or pre-rolled in bundles. The smell is distinctive — harsher than vape, more organic, more desperate. Smoking rollups marks you as Shelf. Tier 3 people never touch them.",
    story_hooks: [
      "Some Shelf Rollup vendors add undisclosed stimulant compounds to their blends — the rollups are mildly addictive beyond the nicotine, keeping customers loyal.",
      "Rollup smoke contains particulates that interact badly with BCI contact points — long-term Shelf smokers have higher rates of BCI malfunction."
    ],
    tags: ["tobacco_vapor", "hand_rolled", "consumer_good", "smoke", "shelf", "tier_1", "cheap", "harsh"]
  },
  {
    name: "VoltCloud Synth-Nicotine Cartridge — Mango Cream",
    type: "consumer_good",
    category: "tobacco_vapor",
    subcategory: "synth-nicotine",
    manufacturer: "VoltCloud Inc.",
    description: "Sweet mango and cream flavor variant. Popular with younger users. The vapor smells like a tropical dessert shop.",
    flavor_profile: "Sweet, ripe mango with a vanilla cream exhale, thick sweet-smelling vapor clouds",
    tier_availability: "Tier 1-4",
    price: "Φ3.00",
    popularity_rank: 5,
    slogan: "Your cloud. Your way.",
    cultural_context: "Mango Cream is the youth flavor. Students, young workers, first-time vapers — they all start with Mango Cream. Adults who vape it are gently mocked. Growing out of Mango Cream and switching to Classic is a minor rite of passage.",
    story_hooks: [
      "VoltCloud's sweet flavors are specifically engineered to appeal to adolescent palates — health advocates have been trying to restrict flavor variety for a decade."
    ],
    tags: ["tobacco_vapor", "synth_nicotine", "consumer_good", "vape", "sweet", "youth", "tier_1", "tier_2", "tier_3", "tier_4"]
  },
  {
    name: "BlackDrag Premium Synth-Cigar",
    type: "consumer_good",
    category: "tobacco_vapor",
    subcategory: "premium smoke",
    manufacturer: "BlackDrag Luxury Tobacco",
    description: "Synth-tobacco cigar wrapped in real bio-printed tobacco leaf. Burns for 45 minutes. Dense, aromatic smoke. The boardroom vice of Tier 4-5 executives.",
    flavor_profile: "Rich, woody, with leather and dark chocolate notes, smooth and dense smoke, no harshness",
    tier_availability: "Tier 4-5",
    price: "Φ25.00",
    popularity_rank: 30,
    slogan: "Smoke with authority.",
    cultural_context: "BlackDrag cigars are power objects. They're smoked in private offices, exclusive lounges, and negotiations. Offering someone a BlackDrag is a gesture of respect. Smoking alone with one is performance of wealth. The smell lingers in rooms for hours.",
    story_hooks: [
      "BlackDrag's 'real tobacco leaf' wrapper is bio-printed from a pre-collapse tobacco cultivar genome — the original plant is extinct.",
      "The CEO of BlackDrag has never smoked in his life — he considers his own product a 'fascinating weakness in powerful people.'"
    ],
    tags: ["tobacco_vapor", "premium", "consumer_good", "cigar", "luxury", "tier_4", "tier_5", "power", "executive"]
  },
  {
    name: "VoltCloud Synth-Nicotine Cartridge — Void",
    type: "consumer_good",
    category: "tobacco_vapor",
    subcategory: "synth-nicotine",
    manufacturer: "VoltCloud Inc.",
    description: "Unflavored synth-nicotine cartridge. Pure nicotine delivery with no flavor masking. For people who want the drug without the dessert. Niche but loyal following.",
    flavor_profile: "Raw nicotine, slight chemical edge, dry throat hit, minimal flavor, nearly invisible vapor",
    tier_availability: "Tier 2-4",
    price: "Φ2.50",
    popularity_rank: 16,
    slogan: "Nothing extra.",
    cultural_context: "Void users consider themselves purists. They don't need mango cream to justify their habit. The absence of flavor is the point — it's honest consumption. Void users and Black Mud drinkers tend to overlap.",
    story_hooks: [
      "Void's near-invisible vapor makes it the cartridge of choice for people vaping where they shouldn't be — restricted areas, operating rooms, secure facilities."
    ],
    tags: ["tobacco_vapor", "synth_nicotine", "consumer_good", "vape", "unflavored", "tier_2", "tier_3", "tier_4", "discreet"]
  },
  {
    name: "GreenLeaf Herbal Vapor Blend — Focus",
    type: "consumer_good",
    category: "tobacco_vapor",
    subcategory: "herbal vapor",
    manufacturer: "GreenLeaf Botanicals",
    description: "Nicotine-free herbal blend with ginkgo, rosemary, and lion's mane mushroom extracts. Marketed as a natural focus aid. The 'clean' alternative to FocusVapor.",
    flavor_profile: "Earthy, slightly bitter, rosemary-forward, thin delicate vapor",
    tier_availability: "Tier 2-4",
    price: "Φ4.50",
    popularity_rank: 17,
    slogan: "Naturally sharp.",
    cultural_context: "GreenLeaf Focus appeals to the wellness-conscious who want cognitive enhancement without synthetic compounds. Whether the herbal extracts actually do anything via inhalation is debated — but the ritual of inhaling something 'natural' before deep work is its own kind of effective.",
    story_hooks: [
      "A comparative study showed GreenLeaf Focus is no more effective than placebo for concentration — but it's more effective than placebo for confidence, which may be the same thing."
    ],
    tags: ["tobacco_vapor", "herbal", "consumer_good", "vape", "focus", "natural", "tier_2", "tier_3", "tier_4"]
  },
  {
    name: "HarborBlend Rough Cut",
    type: "consumer_good",
    category: "tobacco_vapor",
    subcategory: "pipe tobacco",
    manufacturer: "Old Harbor Smoke Co.",
    description: "Coarse-cut pipe blend mixing synth-tobacco with dried harbor herbs. Smoked in small clay pipes that are themselves a minor Old Harbor craft tradition. Smells like the waterfront.",
    flavor_profile: "Rough, briny, herbal with a smoky depth, leaves a salty taste on the lips",
    tier_availability: "Tier 1-2",
    price: "Φ1.50",
    popularity_rank: 20,
    slogan: "The harbor smoke.",
    cultural_context: "HarborBlend is Old Harbor's tobacco identity. The clay pipes are hand-shaped by local artisans. Sitting on the harbor wall smoking HarborBlend is a tradition that predates M88 itself. The harbor herbs change seasonally, so the blend shifts with the months.",
    story_hooks: [
      "The harbor herbs in HarborBlend include a local plant that produces a mild euphoric compound — it's been in the blend so long that nobody thinks of it as a drug.",
      "Old Harbor Smoke Co. is three people working out of a converted shipping container — they have no interest in scaling up."
    ],
    tags: ["tobacco_vapor", "pipe", "consumer_good", "old_harbor", "tradition", "tier_1", "tier_2", "craft"]
  },
  {
    name: "VoltCloud Disposable Mini",
    type: "consumer_good",
    category: "tobacco_vapor",
    subcategory: "disposable vape",
    manufacturer: "VoltCloud Inc.",
    description: "Single-use disposable vape pen. 50 puffs, Mint Ice flavor, then throw it away. The bottom-tier VoltCloud product for when you can't afford a reusable device or cartridges.",
    flavor_profile: "Thin menthol, weaker nicotine hit than cartridge versions, harsh toward the end",
    tier_availability: "Tier 1-2",
    price: "Φ0.50",
    popularity_rank: 4,
    slogan: "Just enough.",
    cultural_context: "Disposable Minis are the Shelf's VoltCloud. They're everywhere — gutters, trash piles, harbor water. The environmental damage is significant but no one with regulatory power lives in the tiers that bear the cost.",
    story_hooks: [
      "VoltCloud Disposable Minis contain a battery that leaches lithium into groundwater when improperly disposed — which is always, because the Shelf has no electronic waste infrastructure."
    ],
    tags: ["tobacco_vapor", "disposable", "consumer_good", "vape", "cheap", "shelf", "tier_1", "tier_2", "waste"]
  },
  {
    name: "Clove Vapor Cartridge",
    type: "consumer_good",
    category: "tobacco_vapor",
    subcategory: "specialty vapor",
    manufacturer: "SpiceVapor Artisan",
    description: "Clove-infused synth-nicotine cartridge with a sweet, spicy character. A niche product with a devoted following who consider mainstream flavors pedestrian.",
    flavor_profile: "Sweet, warm clove with a numbing eugenol tingle, slightly anesthetic on the lips, aromatic smoke",
    tier_availability: "Tier 2-4",
    price: "Φ5.00",
    popularity_rank: 23,
    slogan: "An acquired taste.",
    cultural_context: "Clove vapor is a subculture identifier. There's a small but passionate community of clove vapers who meet, share, and discuss blends with the intensity of wine enthusiasts. The numbing lip sensation is part of the appeal.",
    story_hooks: [
      "SpiceVapor Artisan is a single-person operation run by a Tier 3 chemist who genuinely loves clove — the entire company's output is from one lab."
    ],
    tags: ["tobacco_vapor", "specialty", "consumer_good", "vape", "clove", "niche", "tier_2", "tier_3", "tier_4"]
  },
  {
    name: "VoltCloud Synth-Nicotine Cartridge — Zero",
    type: "consumer_good",
    category: "tobacco_vapor",
    subcategory: "nicotine-free",
    manufacturer: "VoltCloud Inc.",
    description: "VoltCloud flavor cartridge with zero nicotine. For people who've quit nicotine but not the ritual. Available in all VoltCloud flavors. The hand-to-mouth habit persists long after the chemistry stops.",
    flavor_profile: "Same flavors as regular VoltCloud, same vapor production, zero nicotine — your brain knows the difference",
    tier_availability: "Tier 1-4",
    price: "Φ2.50",
    popularity_rank: 12,
    slogan: "The ritual, refined.",
    cultural_context: "Zero cartridges are the quitter's compromise. You still look like you're vaping, you still get the ritual, but nothing happens. Some people use them for years. Others last a week before switching back to nicotine.",
    story_hooks: [
      "VoltCloud's internal data shows that 60% of Zero users return to nicotine cartridges within three months — the ritual itself triggers craving."
    ],
    tags: ["tobacco_vapor", "nicotine_free", "consumer_good", "vape", "quitting", "tier_1", "tier_2", "tier_3", "tier_4"]
  },

  // =====================================================
  // MEDICINE OTC (20)
  // =====================================================

  {
    name: "RejectBlock Augment Suppressor",
    type: "consumer_good",
    category: "medicine_otc",
    subcategory: "augment medicine",
    manufacturer: "PharmaClear Inc.",
    description: "Over-the-counter immunosuppressant specifically formulated to reduce augment rejection symptoms. Take daily. Missing doses causes flare-ups within 48 hours. The pill that keeps your chrome from killing you.",
    flavor_profile: "Small blue pill, coated for easy swallowing, no taste",
    tier_availability: "Tier 1-4",
    price: "Φ5.00",
    popularity_rank: 1,
    slogan: "Keep what's yours.",
    cultural_context: "RejectBlock is life-sustaining medication for millions of augmented people. The daily pill is non-negotiable. Running out causes inflammation, pain, and in extreme cases, sepsis at augment junctions. PharmaClear knows exactly how essential their product is and prices it at the maximum the market will bear.",
    story_hooks: [
      "PharmaClear holds the patent on RejectBlock's active compound and has blocked every generic alternative — people die when they can't afford it.",
      "A Shelf pharmacy has been selling counterfeit RejectBlock at half price — some pills work, some are sugar, and the consequences of getting a sugar pill are severe."
    ],
    tags: ["medicine_otc", "augment", "consumer_good", "immunosuppressant", "essential", "tier_1", "tier_2", "tier_3", "tier_4", "daily"]
  },
  {
    name: "SynapsClear Neural Headache Relief",
    type: "consumer_good",
    category: "medicine_otc",
    subcategory: "pain relief",
    manufacturer: "CortexCare Medical",
    description: "Fast-dissolving tablet for BCI-related headaches — the dull, pressure headaches caused by neural interface activity, signal processing, and HUD eye strain. Works in 15 minutes. The most consumed painkiller in M88.",
    flavor_profile: "Dissolves on the tongue, slightly chalky, mild mint, relief begins as a cooling sensation behind the eyes",
    tier_availability: "Tier 1-4",
    price: "Φ1.50",
    popularity_rank: 2,
    slogan: "Clear the noise.",
    cultural_context: "BCI headaches are the common cold of M88. Everyone gets them. SynapsClear is in every pocket, every desk drawer, every medicine cabinet. Asking someone for a SynapsClear is like asking for a tissue — it's expected, it's normal, it's constant.",
    story_hooks: [
      "SynapsClear works by temporarily reducing BCI signal processing — the headache relief is actually your neural interface throttling down. CortexCare doesn't advertise this.",
      "Long-term SynapsClear use correlates with reduced BCI responsiveness — people who pop them daily may be slowly degrading their interface performance."
    ],
    tags: ["medicine_otc", "pain_relief", "consumer_good", "headache", "bci", "tier_1", "tier_2", "tier_3", "tier_4", "ubiquitous"]
  },
  {
    name: "GeneStable Expression Stabilizer",
    type: "consumer_good",
    category: "medicine_otc",
    subcategory: "geneware medicine",
    manufacturer: "BioStable Health",
    description: "Daily oral supplement that stabilizes geneware gene expression. Prevents unwanted mutations, feature drift, and phenotype instability. For geneware users, this is as essential as vitamins.",
    flavor_profile: "Large capsule, no taste, taken with food to prevent nausea",
    tier_availability: "Tier 2-4",
    price: "Φ8.00",
    popularity_rank: 5,
    slogan: "Stable. Consistent. You.",
    cultural_context: "Geneware expression without stabilizers is a gamble — your tail might grow longer, your fur might change color, your features might drift in unpredictable directions. GeneStable is the pharmaceutical leash on biological chaos. Missing doses is frightening.",
    story_hooks: [
      "GeneStable and GeneGuard lotion are both made by BioStable Health — the company that profits from geneware instability was founded by the same team that developed the unstable geneware technology.",
      "A Tier 1 geneware community that can't afford GeneStable has developed communal coping strategies for expression drift — they've reframed instability as natural evolution rather than disease."
    ],
    tags: ["medicine_otc", "geneware", "consumer_good", "stabilizer", "gene_expression", "tier_2", "tier_3", "tier_4", "daily"]
  },
  {
    name: "SteadyRide Anti-Nausea Tab",
    type: "consumer_good",
    category: "medicine_otc",
    subcategory: "motion sickness",
    manufacturer: "TransitCare Pharma",
    description: "Chewable tablet that prevents motion sickness on mass drivers and high-speed transit. Take 30 minutes before travel. Essential for the millions who commute daily and whose inner ears haven't adapted to the acceleration.",
    flavor_profile: "Ginger-flavored, chalky, dissolves slowly when chewed",
    tier_availability: "Tier 1-4",
    price: "Φ1.00",
    popularity_rank: 6,
    slogan: "Ride smooth.",
    cultural_context: "Mass driver nausea affects about 15% of regular riders. SteadyRide tabs are sold at every transit station entrance. The ginger flavor is so associated with commuting that people feel queasy smelling ginger in other contexts.",
    story_hooks: [
      "TransitCare Pharma is a subsidiary of the same corporation that operates the mass driver system — they created the transit, then the cure for its side effects.",
      "A newer mass driver route has a section that causes nausea in 40% of riders — SteadyRide sales at those stations have tripled."
    ],
    tags: ["medicine_otc", "motion_sickness", "consumer_good", "anti_nausea", "transit", "mass_driver", "tier_1", "tier_2", "tier_3", "tier_4"]
  },
  {
    name: "FlexLube Chrome Joint Lubricant",
    type: "consumer_good",
    category: "medicine_otc",
    subcategory: "prosthetic maintenance",
    manufacturer: "CyberCare Products",
    description: "Precision applicator of medical-grade lubricant for prosthetic joints. Applied weekly to fingers, knees, elbows, and any articulated chrome. Without it, joints grind, seize, and cause pain at the neural interface.",
    flavor_profile: "Odorless, clear gel, applied via needle-tip applicator to joint seams",
    tier_availability: "Tier 1-4",
    price: "Φ6.00",
    popularity_rank: 4,
    slogan: "Move freely.",
    cultural_context: "Joint lubrication is a weekly maintenance ritual for prosthetic users. The sunday-morning joint-lube session is as normal as showering. The sound of a properly lubricated chrome joint — that smooth, silent articulation — versus a dry one is immediately audible.",
    story_hooks: [
      "FlexLube is the only lubricant certified by the three major prosthetic manufacturers — competitors are locked out by compatibility claims that may be artificial.",
      "A Shelf mechanic has developed a DIY lubricant from industrial solvents that works 80% as well at 10% of the cost — CyberCare is suing."
    ],
    tags: ["medicine_otc", "prosthetic", "consumer_good", "lubricant", "joint", "chrome", "tier_1", "tier_2", "tier_3", "tier_4", "weekly"]
  },
  {
    name: "SkinSeal Synth-Skin Repair Patch",
    type: "consumer_good",
    category: "medicine_otc",
    subcategory: "synth-skin repair",
    manufacturer: "DermaSoft Biocosmetics",
    description: "Self-adhesive patch of synth-skin that covers tears, scratches, and damage in existing synth-skin grafts. Apply, press for 30 seconds, and the patch integrates with the surrounding material. Temporary fix until professional repair.",
    flavor_profile: "Skin-temperature, slight adhesive tingle, blends to match surrounding skin tone within an hour",
    tier_availability: "Tier 2-4",
    price: "Φ10.00",
    popularity_rank: 10,
    slogan: "Seamless repair.",
    cultural_context: "Synth-skin tears are like skinned knees for augmented people — common, annoying, and embarrassing if visible. SkinSeal patches are carried in wallets and bags like band-aids. The social grace of offering one to someone with a visible tear is equivalent to offering a tissue.",
    story_hooks: [
      "SkinSeal's color-matching technology uses a nano-pigment system that occasionally malfunctions, turning the patch an obvious different shade — the malfunction rate increases with darker skin tones, which DermaSoft has been slow to address."
    ],
    tags: ["medicine_otc", "synth_skin", "consumer_good", "repair", "patch", "tier_2", "tier_3", "tier_4"]
  },
  {
    name: "ClotQuick Hemostatic Gel",
    type: "consumer_good",
    category: "medicine_otc",
    subcategory: "first aid",
    manufacturer: "MedReady Consumer",
    description: "Tube of fast-acting clotting gel. Squeeze onto a wound, the gel expands and forms a clot within 15 seconds. Stops minor to moderate bleeding. Standard first-aid supply in every household.",
    flavor_profile: "Thick gel, slightly warm on application, hardens to a rubbery seal",
    tier_availability: "Tier 1-4",
    price: "Φ4.00",
    popularity_rank: 7,
    slogan: "Seal it. Move on.",
    cultural_context: "In a world where people have chrome edges, sharp prosthetic joints, and augment maintenance requires minor cutting, bleeding happens more often than in a fully organic population. ClotQuick is in every first-aid kit, every workshop, every kitchen.",
    story_hooks: [
      "ClotQuick works on both human blood and the bio-synthetic fluid used in some advanced augments — this dual-use wasn't designed intentionally but has made it indispensable.",
      "A street medic discovered that ClotQuick can temporarily seal augment-fluid leaks in damaged prosthetics — it's become a field repair tool."
    ],
    tags: ["medicine_otc", "first_aid", "consumer_good", "hemostatic", "clotting", "tier_1", "tier_2", "tier_3", "tier_4"]
  },
  {
    name: "AfterNight Hangover Recovery Kit",
    type: "consumer_good",
    category: "medicine_otc",
    subcategory: "hangover relief",
    manufacturer: "NeuroVolt Pharmaceuticals",
    description: "Single-use kit containing an electrolyte sachet, a liver-support tablet, and a neural headache tab. Dissolve everything in water, drink, and within 45 minutes you feel human again. Or close to it.",
    flavor_profile: "Slightly fizzy, salty-sweet citrus, the liver tablet gives it a bitter edge, best consumed quickly",
    tier_availability: "Tier 2-4",
    price: "Φ5.00",
    popularity_rank: 12,
    slogan: "Morning after. Made bearable.",
    cultural_context: "AfterNight kits are sold at every convenience counter and transit station. Friday night generates Saturday morning sales. The distinctive orange sachet in someone's hand on the mass driver tells the whole story.",
    story_hooks: [
      "AfterNight's liver-support compound was originally developed for treating augment-rejection-induced liver stress — the hangover application was a happy accident.",
      "NeuroVolt now sells products for stimulant use, stimulant recovery, wakefulness, and hangover relief — they are the pharmaceutical company of consequences."
    ],
    tags: ["medicine_otc", "hangover", "consumer_good", "recovery", "electrolyte", "tier_2", "tier_3", "tier_4"]
  },
  {
    name: "BreatheRight Bronchial Inhaler",
    type: "consumer_good",
    category: "medicine_otc",
    subcategory: "respiratory",
    manufacturer: "AirSafe Corp",
    description: "Over-the-counter bronchial inhaler for air-quality-related respiratory issues. Two puffs open the airways for 6 hours. In the lower tiers, this is as common as aspirin.",
    flavor_profile: "Cool menthol blast, immediate chest opening, slight chemical taste",
    tier_availability: "Tier 1-3",
    price: "Φ3.00",
    popularity_rank: 3,
    slogan: "Breathe deep.",
    cultural_context: "Respiratory problems are the leading health issue in Tier 1-2. Bad air, mold, industrial particulates, and vapor exhale all contribute. BreatheRight inhalers are carried by a significant percentage of lower-tier residents. The distinctive puff-puff-exhale is background sound in the Shelf.",
    story_hooks: [
      "AirSafe sells both the air masks and the inhalers — they profit from the air being bad and from treating the consequences of the air being bad.",
      "A public health study found that BreatheRight use in the Shelf has masked a respiratory crisis — people are managing symptoms rather than addressing the air quality."
    ],
    tags: ["medicine_otc", "respiratory", "consumer_good", "inhaler", "bronchial", "tier_1", "tier_2", "tier_3", "essential"]
  },
  {
    name: "NerveCalm Peripheral Suppressant",
    type: "consumer_good",
    category: "medicine_otc",
    subcategory: "nerve pain",
    manufacturer: "CortexCare Medical",
    description: "Topical gel that suppresses phantom limb signals and peripheral nerve pain at augment sites. Applied to the skin near prosthetic junctions. Provides 8 hours of relief from the neural noise of living with chrome.",
    flavor_profile: "Cool, numbing gel, slight eucalyptus scent, absorbs slowly and steadily",
    tier_availability: "Tier 1-4",
    price: "Φ7.00",
    popularity_rank: 8,
    slogan: "Quiet the noise.",
    cultural_context: "Phantom limb syndrome affects most amputees with prosthetics. The brain sends signals to limbs that don't exist anymore. NerveCalm doesn't fix this — it muffles it. Nightly application is a ritual of acceptance for many augmented people.",
    story_hooks: [
      "NerveCalm suppresses the nerve signals that also carry diagnostic data — regular users may miss early warning signs of augment failure.",
      "A study suggests NerveCalm reduces emotional intensity along with nerve pain — it's dulling people without their knowledge."
    ],
    tags: ["medicine_otc", "nerve_pain", "consumer_good", "phantom_limb", "augment", "tier_1", "tier_2", "tier_3", "tier_4"]
  },
  {
    name: "GutReset Probiotic Pack",
    type: "consumer_good",
    category: "medicine_otc",
    subcategory: "digestive health",
    manufacturer: "BioStable Health",
    description: "Seven-day course of high-potency probiotics for resetting gut flora after illness, antibiotic use, or prolonged consumption of synth-food. Take one capsule daily for a week.",
    flavor_profile: "Large capsule, no taste, taken with food",
    tier_availability: "Tier 2-4",
    price: "Φ10.00",
    popularity_rank: 15,
    slogan: "Reset. Restore. Restart.",
    cultural_context: "Synth-food dominance means most of M88's population has compromised gut flora. GutReset is the periodic maintenance — people do a course every few months to keep their digestive system functional. The irony of needing medicine to tolerate your food is not lost on anyone.",
    story_hooks: [
      "BioStable Health also supplies the synth-food additives that compromise gut flora — the cycle is fully vertically integrated."
    ],
    tags: ["medicine_otc", "digestive", "consumer_good", "probiotic", "gut_health", "tier_2", "tier_3", "tier_4"]
  },
  {
    name: "EyeDrop Refresh for BCI Users",
    type: "consumer_good",
    category: "medicine_otc",
    subcategory: "eye care",
    manufacturer: "CortexCare Medical",
    description: "Lubricating eye drops specifically formulated for people with retinal BCI implants. The implants cause chronic dry eye in about 30% of users. These drops maintain the tear film without interfering with the implant's optical path.",
    flavor_profile: "Cool, soothing, instant relief from the gritty dry-eye sensation, lasts 4 hours",
    tier_availability: "Tier 2-4",
    price: "Φ4.00",
    popularity_rank: 9,
    slogan: "See comfortable.",
    cultural_context: "BCI dry eye is such a common complaint that it has its own slang — 'chrome eye.' Pulling out EyeDrop Refresh at your desk is like pulling out reading glasses in the old world. It's just maintenance.",
    story_hooks: [
      "CortexCare makes the BCI implants that cause the dry eye and the drops that treat it — the product cycle is self-sustaining."
    ],
    tags: ["medicine_otc", "eye_care", "consumer_good", "dry_eye", "bci", "tier_2", "tier_3", "tier_4"]
  },
  {
    name: "WoundSeal Antiseptic Spray",
    type: "consumer_good",
    category: "medicine_otc",
    subcategory: "first aid",
    manufacturer: "MedReady Consumer",
    description: "Spray-on antiseptic that creates a protective film over minor wounds. Prevents infection at both organic and augment-junction wound sites. Burns briefly on application, then nothing.",
    flavor_profile: "Sharp sting, alcohol-based, dries to a clear protective film",
    tier_availability: "Tier 1-4",
    price: "Φ2.50",
    popularity_rank: 6,
    slogan: "Spray. Seal. Safe.",
    cultural_context: "WoundSeal is first-aid muscle memory. Cut yourself? Spray it. Scrape your chrome? Spray it. The brief sting is universally known and universally dreaded by children.",
    story_hooks: [
      "WoundSeal's film creates an anaerobic environment that can actually promote certain rare infections — it's a known issue in medical literature that hasn't reached consumer awareness."
    ],
    tags: ["medicine_otc", "first_aid", "consumer_good", "antiseptic", "spray", "tier_1", "tier_2", "tier_3", "tier_4"]
  },
  {
    name: "FeverDown Rapid Coolant Patch",
    type: "consumer_good",
    category: "medicine_otc",
    subcategory: "fever relief",
    manufacturer: "MedReady Consumer",
    description: "Cooling patch applied to the forehead that reduces fever through sustained evaporative cooling and transdermal antipyretic delivery. Works for 12 hours. The sick-day staple.",
    flavor_profile: "Cool gel on contact, slight menthol scent, adheres firmly, gradually warms as it works",
    tier_availability: "Tier 1-4",
    price: "Φ2.00",
    popularity_rank: 11,
    slogan: "Cool down. Fight back.",
    cultural_context: "FeverDown patches are the universal sign of illness in M88. Seeing someone on the transit with a blue patch on their forehead tells you to keep your distance. Parents apply them to children with the same care and worry parents have always had.",
    story_hooks: [
      "FeverDown's antipyretic compound interacts with certain BCI configurations to cause mild hallucinations — it's a known side effect that's considered acceptable because the alternative is uncontrolled fever."
    ],
    tags: ["medicine_otc", "fever", "consumer_good", "cooling_patch", "antipyretic", "tier_1", "tier_2", "tier_3", "tier_4"]
  },
  {
    name: "JointFlex Augment Arthritis Cream",
    type: "consumer_good",
    category: "medicine_otc",
    subcategory: "pain relief",
    manufacturer: "PharmaClear Inc.",
    description: "Deep-heat cream for pain at augment-organic junction points. Different from BoundaryEase — this targets the deeper joint and bone pain where prosthetics meet skeleton. For the ache that never quite goes away.",
    flavor_profile: "Warm, penetrating heat, camphor and capsaicin, absorbs deeply over 30 minutes",
    tier_availability: "Tier 1-4",
    price: "Φ8.00",
    popularity_rank: 13,
    slogan: "Deep relief.",
    cultural_context: "Augment arthritis is the chronic condition of the augmented population. Where chrome meets bone, inflammation is constant. JointFlex manages it but doesn't cure it. The warm camphor smell on someone's hands means their joints are acting up.",
    story_hooks: [
      "Long-term JointFlex use masks progressive bone deterioration at augment anchor points — by the time the pain breaks through, the damage may be severe."
    ],
    tags: ["medicine_otc", "pain_relief", "consumer_good", "arthritis", "augment", "joint", "tier_1", "tier_2", "tier_3", "tier_4", "chronic"]
  },
  {
    name: "AllerShield Geneware Antihistamine",
    type: "consumer_good",
    category: "medicine_otc",
    subcategory: "allergy",
    manufacturer: "BioStable Health",
    description: "Antihistamine formulated for the unique allergic reactions geneware users experience — the immune system sometimes treats your own modifications as foreign. Reduces itching, swelling, and fur/scale irritation.",
    flavor_profile: "Small white tablet, slightly bitter, works within 30 minutes, lasts 24 hours",
    tier_availability: "Tier 2-4",
    price: "Φ3.00",
    popularity_rank: 14,
    slogan: "Your body. On your side.",
    cultural_context: "Geneware allergies are an embarrassing irony — your body rejecting the modifications you chose. AllerShield manages the symptoms quietly. Most geneware people have a pack in their bag and don't talk about it.",
    story_hooks: [
      "AllerShield contains a mild immunosuppressant that, over years, may increase susceptibility to infections — the trade-off between comfort and health is unspoken."
    ],
    tags: ["medicine_otc", "allergy", "consumer_good", "geneware", "antihistamine", "tier_2", "tier_3", "tier_4"]
  },
  {
    name: "CircuitSafe Augment Diagnostic Strip",
    type: "consumer_good",
    category: "medicine_otc",
    subcategory: "diagnostic",
    manufacturer: "CyberCare Products",
    description: "Adhesive strip placed on an augment surface that changes color to indicate potential issues — green for healthy, yellow for maintenance needed, red for urgent attention. A simple, cheap way to monitor prosthetic health between clinic visits.",
    flavor_profile: "Thin flexible strip, adhesive backed, color change visible within 5 minutes",
    tier_availability: "Tier 1-4",
    price: "Φ1.00",
    popularity_rank: 8,
    slogan: "Know before it hurts.",
    cultural_context: "CircuitSafe strips are the home pregnancy test of augment care — cheap, accessible, and the results change everything. A yellow strip means scheduling maintenance. A red strip means dropping everything and getting to a technician. People check their strips with the same anxiety as checking medical results.",
    story_hooks: [
      "CircuitSafe strips have a false-positive rate of 12% for yellow — just high enough to drive unnecessary maintenance appointments that benefit CyberCare's service division.",
      "A hacker has figured out that CircuitSafe strips can detect augment-tracking implants — the unintended use has spread through privacy-conscious communities."
    ],
    tags: ["medicine_otc", "diagnostic", "consumer_good", "augment", "monitoring", "tier_1", "tier_2", "tier_3", "tier_4", "cheap"]
  },
  {
    name: "SleepGuard Night Mouth Guard",
    type: "consumer_good",
    category: "medicine_otc",
    subcategory: "dental",
    manufacturer: "DentaClear Consumer",
    description: "Moldable night guard that protects teeth from grinding caused by stimulant use and BCI-related jaw tension. Heat, bite, mold to your teeth. Replace monthly.",
    flavor_profile: "Soft thermoplastic, mint-flavored initially, flavor fades after first night",
    tier_availability: "Tier 2-4",
    price: "Φ3.00",
    popularity_rank: 16,
    slogan: "Protect every tooth.",
    cultural_context: "The companion product to GrindGuard tablets — sometimes you need chemical relaxation, sometimes you need physical protection, and most people need both. The morning ritual of removing the mouth guard and checking for wear marks tells you how stressed you are.",
    story_hooks: [
      "DentaClear sells the mouth guard and also sells dental prosthetics — they profit from grinding and from the dental damage grinding causes when people don't use guards."
    ],
    tags: ["medicine_otc", "dental", "consumer_good", "mouth_guard", "grinding", "tier_2", "tier_3", "tier_4", "nightly"]
  },
  {
    name: "QuickHeal Burn Gel",
    type: "consumer_good",
    category: "medicine_otc",
    subcategory: "first aid",
    manufacturer: "MedReady Consumer",
    description: "Cooling gel for thermal burns — common in a world of self-heating food packets, overheating augments, and industrial work. Apply immediately, the gel creates a cooling barrier and delivers pain relief and healing accelerant.",
    flavor_profile: "Ice-cold on contact, clear gel, pain relief within seconds, dries to a protective film",
    tier_availability: "Tier 1-4",
    price: "Φ3.50",
    popularity_rank: 10,
    slogan: "Cool it. Heal it.",
    cultural_context: "Burns from overheating augments are common enough that QuickHeal specifically lists 'chrome burns' on its packaging. The gel is in every workshop, every kitchen, and every augment maintenance area.",
    story_hooks: [
      "QuickHeal's healing accelerant uses a nano-compound that may interfere with synth-skin grafts — causing the graft to detach at the burn site."
    ],
    tags: ["medicine_otc", "first_aid", "consumer_good", "burn", "cooling", "tier_1", "tier_2", "tier_3", "tier_4"]
  },

  // =====================================================
  // STATIONERY & ANALOG (15)
  // =====================================================

  {
    name: "Kodan Real Paper Notebook — A5",
    type: "consumer_good",
    category: "stationery",
    subcategory: "notebook",
    manufacturer: "Kodan Paper Works",
    description: "80-page notebook made from actual wood-pulp paper. Lined or blank. The pages are slightly rough, slightly cream-colored, and feel like touching history. Writing in it by hand means your thoughts exist outside your BCI.",
    flavor_profile: "Faint paper-and-ink smell, satisfying texture, the sound of a page turning",
    tier_availability: "Tier 3-5",
    price: "Φ25.00",
    popularity_rank: 10,
    slogan: "Think outside the interface.",
    cultural_context: "Real paper is a luxury and a statement. Writing by hand means you have thoughts worth keeping off-network — thoughts your BCI can't log, your employer can't access, your data broker can't sell. A Kodan notebook is a privacy tool disguised as nostalgia. It's also beautiful.",
    story_hooks: [
      "Kodan Paper Works is one of three remaining paper manufacturers in the hemisphere — they source pulp from managed forests outside the city and the supply chain is fragile.",
      "Intelligence services monitor Kodan notebook purchases because people who write off-network often have something to hide — the irony of analog surveillance of analog privacy tools is not lost on anyone."
    ],
    tags: ["stationery", "analog", "consumer_good", "paper", "notebook", "privacy", "luxury", "tier_3", "tier_4", "tier_5"]
  },
  {
    name: "Kodan Real Paper Notebook — Pocket",
    type: "consumer_good",
    category: "stationery",
    subcategory: "notebook",
    manufacturer: "Kodan Paper Works",
    description: "40-page pocket-sized notebook. Fits in a jacket. For quick notes, sketches, and thoughts captured in the moment. The journalist's and fixer's preferred tool.",
    flavor_profile: "Compact, same cream paper, elastic closure band, satisfying to hold",
    tier_availability: "Tier 3-5",
    price: "Φ15.00",
    popularity_rank: 15,
    slogan: "Carry your thoughts.",
    cultural_context: "The Pocket Kodan is the working analog tool. Journalists use them. Fixers use them. Anyone who needs to record information that can't be intercepted uses them. Pulling out a pocket notebook in a meeting signals seriousness and discretion.",
    story_hooks: [
      "A murdered journalist's pocket Kodan contained information that cracked a corruption case — the notebook survived because it was analog and couldn't be remotely wiped."
    ],
    tags: ["stationery", "analog", "consumer_good", "paper", "notebook", "privacy", "tier_3", "tier_4", "tier_5", "journalist"]
  },
  {
    name: "Meridian Mechanical Pen",
    type: "consumer_good",
    category: "stationery",
    subcategory: "writing instrument",
    manufacturer: "Meridian Precision Tools",
    description: "Entirely mechanical ballpoint pen. No electronics, no tracking, no smart features. Click-top mechanism, replaceable ink cartridge. Writes on any surface. The pen for people who don't want their pen to be smart.",
    flavor_profile: "Satisfying click mechanism, smooth ink flow, matte metal body, weighted for comfort",
    tier_availability: "Tier 2-5",
    price: "Φ8.00",
    popularity_rank: 12,
    slogan: "Just a pen. Nothing more.",
    cultural_context: "The Meridian pen is a quiet rebellion against total connectivity. Using it says you understand that not everything needs to be electronic. It pairs with Kodan notebooks as the complete analog writing kit. Some people carry one even if they never use it — it's a talisman of independence.",
    story_hooks: [
      "Meridian Precision Tools was founded by a retired BCI engineer who became disillusioned with neural interfaces — every product they make is aggressively non-electronic."
    ],
    tags: ["stationery", "analog", "consumer_good", "pen", "mechanical", "privacy", "tier_2", "tier_3", "tier_4", "tier_5"]
  },
  {
    name: "SilkLine Drawing Charcoal Set",
    type: "consumer_good",
    category: "stationery",
    subcategory: "art supplies",
    manufacturer: "SilkLine Art Supply",
    description: "Set of 12 charcoal sticks in varying hardnesses, plus a kneaded eraser. For drawing on real paper with real materials. The act of making marks with carbon on fiber is ancient and, in M88, radical.",
    flavor_profile: "Dusty, dark, the satisfying scratch of charcoal on textured paper, fingers blackened by use",
    tier_availability: "Tier 3-5",
    price: "Φ18.00",
    popularity_rank: 22,
    slogan: "Make your mark.",
    cultural_context: "Drawing by hand is an art practice that BCI-generated imagery can't replace — because the point isn't the image, it's the process. SilkLine supplies the small but passionate analog art community. Charcoal drawings are valued precisely because they're imperfect and human.",
    story_hooks: [
      "The analog art scene in M88 is growing — galleries that show only hand-made work have waiting lists, and the movement is seen by some corporations as a subtle form of anti-technology protest."
    ],
    tags: ["stationery", "art", "consumer_good", "charcoal", "drawing", "analog", "tier_3", "tier_4", "tier_5"]
  },
  {
    name: "TrueInk Tattoo Ink Set — Street Colors",
    type: "consumer_good",
    category: "stationery",
    subcategory: "ink",
    manufacturer: "TrueInk Body Art Supply",
    description: "Set of 8 professional tattoo inks in the colors most popular in M88's street tattoo scene. Compatible with both traditional needle guns and modern applicators. Formulated for both organic skin and synth-skin.",
    flavor_profile: "Vivid pigments, thick consistency, formulated for color retention in both natural and synthetic dermis",
    tier_availability: "Tier 2-4",
    price: "Φ30.00",
    popularity_rank: 18,
    slogan: "Permanent. Personal. Yours.",
    cultural_context: "Tattooing is a major art form in M88 — one of the few body modifications that isn't electronic or genetic. Tattoos on chrome-adjacent skin have a particular aesthetic. TrueInk's dual-formulation for organic and synth-skin solved a real problem for tattoo artists working on augmented clients.",
    story_hooks: [
      "TrueInk's synth-skin formulation contains a bonding agent that, under UV light, reveals hidden patterns — some tattoo artists use this for 'secret tattoos' visible only under specific conditions.",
      "A Tier 1 tattoo artist using TrueInk has become famous for work that incorporates the visible boundary between organic and chrome skin into the design — the art acknowledges the body it's on."
    ],
    tags: ["stationery", "ink", "consumer_good", "tattoo", "body_art", "tier_2", "tier_3", "tier_4"]
  },
  {
    name: "ReelPrint Instant Photo Film",
    type: "consumer_good",
    category: "stationery",
    subcategory: "photography",
    manufacturer: "ReelPrint Analog Imaging",
    description: "Pack of 10 instant photo film sheets compatible with ReelPrint cameras. Takes a physical photograph that develops in 60 seconds. In a world of BCI-captured images, a physical photo is a gift, a keepsake, and an act of presence.",
    flavor_profile: "Chemical developing smell, the satisfaction of watching an image emerge, slightly desaturated colors, white border",
    tier_availability: "Tier 3-5",
    price: "Φ12.00",
    popularity_rank: 16,
    slogan: "Hold the moment.",
    cultural_context: "Instant photos are cherished objects in M88. They're taped to walls, tucked into wallets, given as love tokens. The slight imperfection — the light leaks, the soft focus, the chemical color — is the point. A physical photo says 'I was here, with you, and I wanted to remember with my hands, not my interface.'",
    story_hooks: [
      "ReelPrint cameras cannot be hacked, surveilled, or remotely accessed — they're used by people who want to document things without creating a digital record.",
      "A collection of ReelPrint photos documenting life in the Shelf has become an underground art exhibition — the raw, unfiltered images are more powerful than any BCI-captured content."
    ],
    tags: ["stationery", "photography", "consumer_good", "instant_photo", "analog", "tier_3", "tier_4", "tier_5", "keepsake"]
  },
  {
    name: "Kodan Letterpress Postcard Set",
    type: "consumer_good",
    category: "stationery",
    subcategory: "paper goods",
    manufacturer: "Kodan Paper Works",
    description: "Set of 10 blank postcards made from heavyweight paper with a letterpress-printed border. For writing messages by hand and delivering them physically. The most deliberately anachronistic product in M88.",
    flavor_profile: "Thick, textured card stock, debossed border pattern, cream colored, satisfying to write on",
    tier_availability: "Tier 3-5",
    price: "Φ10.00",
    popularity_rank: 24,
    slogan: "Say it by hand.",
    cultural_context: "Sending a handwritten postcard in M88 is an act of extraordinary intimacy. You wrote it by hand. You delivered it physically. It exists as a single object that can't be copied or forwarded. It's the most personal form of communication possible in a networked world.",
    story_hooks: [
      "A postcard delivery service has emerged in Tier 3 — bicycle couriers who deliver handwritten cards same-day. It's become a romantic gesture trend."
    ],
    tags: ["stationery", "paper", "consumer_good", "postcard", "handwritten", "analog", "tier_3", "tier_4", "tier_5"]
  },
  {
    name: "BoundBook Physical Novel",
    type: "consumer_good",
    category: "stationery",
    subcategory: "physical book",
    manufacturer: "Various publishers",
    description: "A physical book. Paper pages, printed text, bound spine. Fiction or non-fiction. Costs more than a digital copy by a factor of ten. Collected, displayed, cherished. Reading one in public is a statement.",
    flavor_profile: "Paper smell, the weight of pages, the sound of a spine cracking open, ink on fiber",
    tier_availability: "Tier 3-5",
    price: "Φ35.00",
    popularity_rank: 20,
    slogan: "Read with your hands.",
    cultural_context: "Physical books are luxury objects and cultural artifacts. Bookshelves are status displays. Having books you've actually read is a social signal of depth and patience. The act of reading a physical book — turning pages, marking your place, lending it to someone — is a form of resistance against the disposability of digital content.",
    story_hooks: [
      "A black-market press in the Shelf prints banned texts — political writing, censored journalism, suppressed histories — on physical paper because physical books can't be remotely deleted.",
      "Book collectors in Tier 4-5 pay hundreds for first editions of pre-collapse novels — the provenance of old books is its own economy."
    ],
    tags: ["stationery", "book", "consumer_good", "physical", "analog", "luxury", "tier_3", "tier_4", "tier_5", "collectible"]
  },
  {
    name: "SilkLine Calligraphy Ink — Sumi Black",
    type: "consumer_good",
    category: "stationery",
    subcategory: "ink",
    manufacturer: "SilkLine Art Supply",
    description: "Bottle of traditional sumi ink for calligraphy and brush painting. Ground from actual soot and animal glue following ancient methods. In M88, calligraphy is meditation — the brush, the ink, the paper, and nothing else.",
    flavor_profile: "Dense black, slight pine-soot smell, the ink flows differently than any synthetic pigment",
    tier_availability: "Tier 3-5",
    price: "Φ20.00",
    popularity_rank: 25,
    slogan: "The old way.",
    cultural_context: "Calligraphy practice in M88 is a growing movement — part meditation, part art, part deliberate slow living. The practitioners are diverse — ex-corporate executives, Shelf artists who saved for the supplies, geneware people whose hands make the brush strokes unique.",
    story_hooks: [
      "A calligraphy collective has started writing protest slogans using traditional methods and posting them in public spaces — the hand-brushed characters are more impactful than any digital display."
    ],
    tags: ["stationery", "ink", "consumer_good", "calligraphy", "sumi", "traditional", "tier_3", "tier_4", "tier_5", "meditation"]
  },
  {
    name: "Meridian Drafting Pencil Set",
    type: "consumer_good",
    category: "stationery",
    subcategory: "writing instrument",
    manufacturer: "Meridian Precision Tools",
    description: "Set of 6 mechanical pencils in varying lead weights. No electronics. Precision-machined aluminum bodies. For technical drawing, sketching, and anyone who prefers graphite to ink.",
    flavor_profile: "Satisfying click advance, precise lead extension, knurled grip, balanced weight",
    tier_availability: "Tier 3-5",
    price: "Φ15.00",
    popularity_rank: 21,
    slogan: "Draw. Erase. Draw again.",
    cultural_context: "Meridian pencils are the tool of architects, engineers, and artists who still work on paper. Technical drawing on paper is considered a lost art, but those who practice it produce work with a quality that digital tools don't replicate.",
    story_hooks: [
      "An architect who designs exclusively on paper with Meridian pencils has become famous for buildings that feel different from algorithm-designed structures — the human imprecision is detectable and preferred."
    ],
    tags: ["stationery", "pencil", "consumer_good", "mechanical", "drafting", "analog", "tier_3", "tier_4", "tier_5"]
  },
  {
    name: "PaperMoon Envelope Pack",
    type: "consumer_good",
    category: "stationery",
    subcategory: "paper goods",
    manufacturer: "Kodan Paper Works",
    description: "Pack of 20 real paper envelopes. For sealing letters, postcards, and documents in physical form. Self-adhesive seal. In M88, a sealed envelope implies contents too sensitive for digital transmission.",
    flavor_profile: "Crisp paper, satisfying seal, the finality of closing a physical envelope",
    tier_availability: "Tier 3-5",
    price: "Φ8.00",
    popularity_rank: 26,
    slogan: "Seal it. Send it. Mean it.",
    cultural_context: "Receiving a physical envelope in M88 makes your heart rate spike. It could be a love letter, a threat, a legal document, or a photograph. The physicality implies weight — emotional, legal, or personal. Nobody sends junk mail on paper.",
    story_hooks: [
      "A blackmail ring uses PaperMoon envelopes to deliver demands — the untraceable nature of physical mail makes investigation difficult."
    ],
    tags: ["stationery", "paper", "consumer_good", "envelope", "analog", "tier_3", "tier_4", "tier_5"]
  },
  {
    name: "SilkLine Sketch Pad — Heavyweight",
    type: "consumer_good",
    category: "stationery",
    subcategory: "art supplies",
    manufacturer: "SilkLine Art Supply",
    description: "30-page pad of heavyweight drawing paper suitable for charcoal, ink, watercolor, and mixed media. Spiral-bound, perforated for clean removal. The foundation of analog art practice.",
    flavor_profile: "Thick, toothy paper texture, slight cotton-rag smell, holds wet media without buckling",
    tier_availability: "Tier 3-5",
    price: "Φ20.00",
    popularity_rank: 23,
    slogan: "Your surface.",
    cultural_context: "SilkLine sketch pads are where analog art starts. The blank page — an actual, physical blank page — is both intimidating and liberating. No undo button. No filters. Just you and the surface.",
    story_hooks: [
      "SilkLine sources their cotton rag from a single textile recycler in Tier 2 — if that operation closes, the paper quality drops significantly."
    ],
    tags: ["stationery", "art", "consumer_good", "paper", "sketch_pad", "analog", "tier_3", "tier_4", "tier_5"]
  },
  {
    name: "ReelPrint Pocket Camera",
    type: "consumer_good",
    category: "stationery",
    subcategory: "photography",
    manufacturer: "ReelPrint Analog Imaging",
    description: "Compact instant camera that produces physical photographs. Fixed lens, automatic exposure, entirely analog optics. No data connection, no BCI integration, no cloud storage. What it captures exists only as a physical print.",
    flavor_profile: "Satisfying shutter click, mechanical film advance, warm flash, the anticipation of watching the image develop",
    tier_availability: "Tier 3-5",
    price: "Φ45.00",
    popularity_rank: 19,
    slogan: "One shot. One print. One memory.",
    cultural_context: "The ReelPrint camera is a cult object. Photography communities organize 'analog walks' where participants photograph the city without BCI assistance. The camera's limitations — no zoom, no retake preview, no filters — force a deliberateness that changes how you see.",
    story_hooks: [
      "ReelPrint cameras have become popular with surveillance-wary individuals — activists, journalists, and criminals alike value the analog-only output.",
      "A ReelPrint photo series of Old Harbor life has been exhibited in a Tier 4 gallery — the raw images are considered fine art, which both flatters and exploits their subjects."
    ],
    tags: ["stationery", "photography", "consumer_good", "camera", "instant", "analog", "tier_3", "tier_4", "tier_5"]
  },
  {
    name: "Kodan Wax Seal Kit",
    type: "consumer_good",
    category: "stationery",
    subcategory: "paper goods",
    manufacturer: "Kodan Paper Works",
    description: "Brass seal stamp and colored wax sticks for sealing envelopes and documents. Choose a letter initial or custom design. Pure ceremony — pure meaning. Sealing a letter with wax in M88 is like drawing a sword.",
    flavor_profile: "The smell of melting wax, the satisfying press of brass into soft wax, the crack when broken",
    tier_availability: "Tier 4-5",
    price: "Φ35.00",
    popularity_rank: 28,
    slogan: "Your seal. Your word.",
    cultural_context: "Wax seals are the ultimate analog status symbol. They imply a person who writes physical letters, seals them by hand, and delivers them personally. It's aristocratic theater in a cyberpunk city, and it's completely sincere. People who use wax seals mean everything they send.",
    story_hooks: [
      "A shadowy figure known only by their wax seal — a serpent eating its own tail — has been sending sealed letters to corporate executives containing information they shouldn't have."
    ],
    tags: ["stationery", "seal", "consumer_good", "wax", "analog", "luxury", "tier_4", "tier_5", "ceremony"]
  },
  {
    name: "SilkLine Brush Pen",
    type: "consumer_good",
    category: "stationery",
    subcategory: "writing instrument",
    manufacturer: "SilkLine Art Supply",
    description: "Felt-tip brush pen with flexible nib for calligraphy, lettering, and expressive writing. Refillable ink reservoir. The crossover tool between writing and art.",
    flavor_profile: "Flexible brush tip, smooth ink flow, responsive to pressure, produces beautiful line variation",
    tier_availability: "Tier 3-5",
    price: "Φ12.00",
    popularity_rank: 20,
    slogan: "Where writing becomes art.",
    cultural_context: "Brush pens are the entry point for calligraphy — easier than traditional brushes but producing similar expressive quality. People who carry SilkLine brush pens doodle characters on napkins, sign names with flourishes, and treat writing as a physical art.",
    story_hooks: [
      "A graffiti artist in Tier 2 uses an oversized version of the SilkLine brush pen design to create enormous calligraphic murals — the work is beautiful and technically illegal."
    ],
    tags: ["stationery", "pen", "consumer_good", "brush", "calligraphy", "art", "tier_3", "tier_4", "tier_5"]
  },

  // =====================================================
  // ELECTRONICS & GADGETS (20)
  // =====================================================

  {
    name: "BurnComm Disposable Communicator",
    type: "consumer_good",
    category: "electronics",
    subcategory: "disposable comm",
    manufacturer: "TechDrop Inc.",
    description: "Single-use communication device with a 72-hour battery life. Pre-loaded with 100 minutes of encrypted voice and 500 text messages. Snap it in half when done — the internals self-destruct. The burner phone of M88.",
    flavor_profile: "Cheap plastic, small screen, basic interface, satisfying snap when destroyed",
    tier_availability: "Tier 1-4",
    price: "Φ5.00",
    popularity_rank: 3,
    slogan: "Talk. Text. Trash.",
    cultural_context: "BurnComms are used by everyone from criminals to activists to teenagers having secret relationships. They're sold at transit stations, convenience counters, and Shelf market stalls. Buying one isn't suspicious. Buying ten is.",
    story_hooks: [
      "TechDrop's 'self-destruct' mechanism doesn't actually destroy the memory chip — forensic recovery is possible, which law enforcement knows and customers don't.",
      "A Shelf communication network has been built entirely on chained BurnComms — messages pass through a relay of disposable devices, making them nearly untraceable."
    ],
    tags: ["electronics", "disposable", "consumer_good", "communicator", "burner", "privacy", "tier_1", "tier_2", "tier_3", "tier_4"]
  },
  {
    name: "ShadeScreen Privacy Filter",
    type: "consumer_good",
    category: "electronics",
    subcategory: "privacy device",
    manufacturer: "PrivacyFirst Tech",
    description: "Physical screen attachment that blocks visual hacking of your HUD display from external observation. Prevents shoulder-surfing of BCI-projected information by narrowing the viewing angle to the user's eyes only.",
    flavor_profile: "Thin, adhesive-mount, polarized film, slightly dims the display but ensures only you see it",
    tier_availability: "Tier 2-4",
    price: "Φ12.00",
    popularity_rank: 10,
    slogan: "Your screen. Your eyes only.",
    cultural_context: "Visual hacking — reading someone's HUD by observing the light patterns in their eyes — is a real threat. ShadeScreen is the physical countermeasure. Corporate employees are issued them. Privacy-conscious individuals buy them. The slight dimming is the price of keeping your data yours.",
    story_hooks: [
      "ShadeScreen's polarization filter has a frequency vulnerability — a specific wavelength of light can bypass it, which intelligence agencies know about.",
      "PrivacyFirst Tech was founded by an ex-surveillance specialist who had a crisis of conscience — their product line is built on insider knowledge of surveillance methods."
    ],
    tags: ["electronics", "privacy", "consumer_good", "screen", "anti_surveillance", "tier_2", "tier_3", "tier_4"]
  },
  {
    name: "MeshBoost Signal Amplifier",
    type: "consumer_good",
    category: "electronics",
    subcategory: "signal booster",
    manufacturer: "GridLink Communications",
    description: "Portable device that amplifies mesh network signals in dead zones. Plugs into any power source and extends mesh network range by 50 meters. Essential in the Shelf where network infrastructure has gaps.",
    flavor_profile: "Small black box, single LED indicator, hums when active, gets warm",
    tier_availability: "Tier 1-3",
    price: "Φ15.00",
    popularity_rank: 8,
    slogan: "Stay connected.",
    cultural_context: "Mesh network dead zones in the Shelf are social isolation zones — no network means no BCI updates, no communications, no access to services. MeshBoost devices are community infrastructure. People pool money to buy them and mount them in communal spaces.",
    story_hooks: [
      "MeshBoost devices can be modified to create private, unmonitored network nodes — a capability that GridLink knows about and hasn't patched because the Shelf market depends on it.",
      "The power draw of clustered MeshBoost devices has caused electrical fires in Shelf buildings with already-overtaxed wiring."
    ],
    tags: ["electronics", "signal", "consumer_good", "mesh_network", "booster", "shelf", "tier_1", "tier_2", "tier_3"]
  },
  {
    name: "HoloFrame Mini",
    type: "consumer_good",
    category: "electronics",
    subcategory: "display",
    manufacturer: "LuxDisplay Corp",
    description: "Small holographic photo frame that projects a 3D image of a stored photo. Holds one image. Change it by tapping a data chip to the frame. The digital equivalent of a framed photo on a desk — except it floats in the air.",
    flavor_profile: "Soft holographic glow, silent operation, the image has a slight shimmer that distinguishes it from reality",
    tier_availability: "Tier 3-4",
    price: "Φ20.00",
    popularity_rank: 14,
    slogan: "Keep them close.",
    cultural_context: "HoloFrame Minis are on desks and nightstands across Tier 3-4. Usually showing a partner, a child, a pet, or a place. The holographic shimmer makes the image dreamlike — memory given a physical presence that's still slightly unreal.",
    story_hooks: [
      "A modified HoloFrame that displays the face of a missing person has become a protest symbol — families of the disappeared place them in public spaces."
    ],
    tags: ["electronics", "display", "consumer_good", "holographic", "photo", "tier_3", "tier_4"]
  },
  {
    name: "SkinVeil BCI Cosmetic Overlay — Classic",
    type: "consumer_good",
    category: "electronics",
    subcategory: "BCI cosmetic",
    manufacturer: "InterfaceSkins Co.",
    description: "Software package that changes the visual appearance of your BCI heads-up display. 'Classic' is a clean, minimal UI with muted colors. Your HUD is your most-viewed interface — customizing it is as personal as decorating your home.",
    flavor_profile: "Clean typography, subtle animations, muted blue-gray palette, professional and understated",
    tier_availability: "Tier 2-4",
    price: "Φ8.00",
    popularity_rank: 6,
    slogan: "Your interface. Your style.",
    cultural_context: "BCI overlay customization is universal self-expression. Your HUD skin is visible to you constantly and, through eye-glow patterns, partially visible to others. Classic is the default professional choice — it says 'I take this seriously.'",
    story_hooks: [
      "InterfaceSkins' overlays have access to the BCI visual layer — technically they could overlay false information onto the user's perception, which is a terrifying capability the company publicly denies and privately monetizes."
    ],
    tags: ["electronics", "bci_cosmetic", "consumer_good", "hud", "interface", "tier_2", "tier_3", "tier_4"]
  },
  {
    name: "SkinVeil BCI Cosmetic Overlay — Neon District",
    type: "consumer_good",
    category: "electronics",
    subcategory: "BCI cosmetic",
    manufacturer: "InterfaceSkins Co.",
    description: "Vibrant, high-contrast HUD skin with neon accents, animated transitions, and a cyberpunk aesthetic. Popular with younger users who want their interface to feel alive.",
    flavor_profile: "Hot pink and electric blue accents, animated data flows, particle effects on notifications, loud and proud",
    tier_availability: "Tier 2-4",
    price: "Φ8.00",
    popularity_rank: 9,
    slogan: "Your interface. Your style.",
    cultural_context: "Neon District is the Mango Cream of HUD skins — the youth choice. Adults using it are either young at heart or trying too hard. The animated effects cause slightly higher BCI power consumption, which users don't care about.",
    story_hooks: [
      "Neon District's animated overlays have been found to contain subliminal advertising frames — InterfaceSkins sold the subliminal slots to advertisers without user consent."
    ],
    tags: ["electronics", "bci_cosmetic", "consumer_good", "hud", "interface", "neon", "youth", "tier_2", "tier_3", "tier_4"]
  },
  {
    name: "DataChip Standard — 1TB",
    type: "consumer_good",
    category: "electronics",
    subcategory: "data storage",
    manufacturer: "SolidState Storage Co.",
    description: "Physical data storage chip the size of a fingernail. Holds 1 terabyte. No network connection — data goes on, data comes off, and nothing in between is traceable. The analog backup of the digital world.",
    flavor_profile: "Tiny, metallic, fragile-looking but durable, fits in any device with a standard chip slot",
    tier_availability: "Tier 1-4",
    price: "Φ3.00",
    popularity_rank: 5,
    slogan: "Your data. Your hands.",
    cultural_context: "DataChips are how physical data transfer happens. Handing someone a chip is handing them information that never touched a network. Dead drops, backups, archives — the chip is the vessel. The physical act of giving someone a chip has weight.",
    story_hooks: [
      "DataChips can be encrypted but they can also be physically destroyed — which makes them both secure and vulnerable in ways digital storage isn't.",
      "A courier network that moves DataChips across tier boundaries has become critical infrastructure for people who need information to move without network traces."
    ],
    tags: ["electronics", "data_storage", "consumer_good", "chip", "analog_backup", "privacy", "tier_1", "tier_2", "tier_3", "tier_4"]
  },
  {
    name: "ChargePad Universal",
    type: "consumer_good",
    category: "electronics",
    subcategory: "charging",
    manufacturer: "VoltWorks",
    description: "Wireless charging pad compatible with most augment batteries, prosthetic devices, and consumer electronics. Place your chrome hand on it while you sleep and wake up fully charged. The nightstand essential for augmented people.",
    flavor_profile: "Soft glow when active, slight warmth, quiet hum, rubberized non-slip surface",
    tier_availability: "Tier 1-4",
    price: "Φ12.00",
    popularity_rank: 4,
    slogan: "Rest and recharge.",
    cultural_context: "The ChargePad is on every augmented person's nightstand. The soft glow is the last thing you see before sleep and the first thing you check in the morning — is your charge full? Running on low augment battery is like running on no sleep. Everything works worse.",
    story_hooks: [
      "ChargePad Universal's wireless signal can be intercepted to extract data from the augment being charged — a vulnerability that VoltWorks has downplayed.",
      "A Shelf community shares ChargePads in a communal charging station — people drop off their prosthetic hands overnight and pick them up in the morning, trusting strangers with their limbs."
    ],
    tags: ["electronics", "charging", "consumer_good", "wireless", "augment", "tier_1", "tier_2", "tier_3", "tier_4", "essential"]
  },
  {
    name: "QuietZone RF Blocking Pouch",
    type: "consumer_good",
    category: "electronics",
    subcategory: "privacy device",
    manufacturer: "PrivacyFirst Tech",
    description: "Faraday pouch that blocks all radio frequency signals to devices placed inside. Put your comm device, DataChips, or augment accessories inside and they become electronically invisible. Simple, effective, paranoid.",
    flavor_profile: "Metallic fabric, zip closure, phones go silent when inserted, slightly stiff",
    tier_availability: "Tier 2-4",
    price: "Φ8.00",
    popularity_rank: 13,
    slogan: "When silence is safety.",
    cultural_context: "QuietZone pouches are standard equipment for anyone privacy-conscious, security-aware, or professionally paranoid. Journalists carry them. Fixers carry them. Corporate negotiators carry them. Putting your device in a QuietZone before a meeting is a gesture of trust — 'nothing leaves this room.'",
    story_hooks: [
      "QuietZone pouches are used by criminals to prevent device tracking during operations — PrivacyFirst has been pressured by law enforcement to add a hidden tracking element, which they have publicly refused.",
      "Some QuietZone pouches sold in the Shelf are counterfeits that don't actually block all frequencies — the cheapest privacy tool is sometimes no privacy at all."
    ],
    tags: ["electronics", "privacy", "consumer_good", "faraday", "rf_blocking", "tier_2", "tier_3", "tier_4"]
  },
  {
    name: "EarBud Disposable Audio",
    type: "consumer_good",
    category: "electronics",
    subcategory: "audio",
    manufacturer: "TechDrop Inc.",
    description: "Single-use wired earbuds for people who don't have or don't want audio augments. Thin wire, basic driver, surprisingly decent sound. Use until they break, throw them away, buy new ones.",
    flavor_profile: "Tinny but clear, adequate bass, comfortable enough for a transit ride, unremarkable",
    tier_availability: "Tier 1-3",
    price: "Φ1.00",
    popularity_rank: 7,
    slogan: "Listen in.",
    cultural_context: "Not everyone has audio augments. Not everyone wants them. EarBud Disposables serve the population that still uses external audio — by choice or by economics. The thin white wire is a visible marker of non-augmentation.",
    story_hooks: [
      "EarBud Disposables produce just enough electronic waste to be an environmental problem but not enough to trigger regulation — the accumulation is invisible until you see the waste pile."
    ],
    tags: ["electronics", "audio", "consumer_good", "earbuds", "disposable", "non_augmented", "tier_1", "tier_2", "tier_3"]
  },
  {
    name: "GlowWire LED Strip — 2m",
    type: "consumer_good",
    category: "electronics",
    subcategory: "lighting",
    manufacturer: "BrightLine Consumer",
    description: "Two-meter strip of flexible LED lights with adhesive backing. Peel, stick, plug in. Available in 8 colors. The cheapest way to make a Shelf dwelling feel less like a concrete box.",
    flavor_profile: "Soft, colored glow, low power consumption, adhesive that barely holds on humid walls",
    tier_availability: "Tier 1-3",
    price: "Φ2.00",
    popularity_rank: 11,
    slogan: "Light up your space.",
    cultural_context: "GlowWire is everywhere in the Shelf. Purple, blue, red — the colored strips turn concrete corridors into something approaching home. They're cheap enough for anyone, and the cumulative effect of a Shelf block where everyone has GlowWire is actually beautiful. It's accidental collaborative art.",
    story_hooks: [
      "The colored glow of GlowWire-lit Shelf corridors has become iconic imagery of lower-tier life — photographers and filmmakers romanticize it, which residents find both flattering and insulting."
    ],
    tags: ["electronics", "lighting", "consumer_good", "led", "decor", "shelf", "tier_1", "tier_2", "tier_3", "cheap"]
  },
  {
    name: "NanoTool Pocket Multitool",
    type: "consumer_good",
    category: "electronics",
    subcategory: "tool",
    manufacturer: "Meridian Precision Tools",
    description: "Compact multitool with drivers, pliers, blade, and a micro-diagnostic port for basic augment troubleshooting. Entirely mechanical except for the diagnostic LED. The pocket tool of the augmented age.",
    flavor_profile: "Heavy for its size, satisfying clicks and locks, warm brushed steel, fits in one hand",
    tier_availability: "Tier 2-4",
    price: "Φ18.00",
    popularity_rank: 12,
    slogan: "Fix it yourself.",
    cultural_context: "NanoTool is the Swiss Army knife of M88. The augment diagnostic port is what makes it essential — basic chrome troubleshooting without a clinic visit. People who carry a NanoTool are considered practical, self-reliant, and slightly paranoid about letting technicians touch their augments.",
    story_hooks: [
      "The diagnostic port on the NanoTool can, with modified firmware, access restricted augment settings — it's become a tool for homebrew augment modification."
    ],
    tags: ["electronics", "tool", "consumer_good", "multitool", "augment", "diagnostic", "tier_2", "tier_3", "tier_4"]
  },
  {
    name: "SpeakEasy Voice Modulator Patch",
    type: "consumer_good",
    category: "electronics",
    subcategory: "voice mod",
    manufacturer: "PrivacyFirst Tech",
    description: "Adhesive throat patch that modulates your voice in real time. Deepens, raises, or completely changes vocal characteristics. Used for privacy, performance, and by people whose augments have affected their natural voice.",
    flavor_profile: "Thin patch, adheres to the throat, slight vibration when active, natural-sounding modulation",
    tier_availability: "Tier 2-4",
    price: "Φ10.00",
    popularity_rank: 16,
    slogan: "Speak differently.",
    cultural_context: "Voice modulation is common and not considered deceptive in most social contexts. Performers use it. Trans individuals use it. Privacy-conscious people use it for anonymous communications. The technology is so normalized that some people's 'real' voice is the modulated one.",
    story_hooks: [
      "Voice modulator patches can defeat voice-recognition security systems — which makes them both a privacy tool and a breaking-and-entering tool.",
      "A singer who performs exclusively through a SpeakEasy patch has never revealed their natural voice — fans debate whether the art is in the singing or the modulation."
    ],
    tags: ["electronics", "voice", "consumer_good", "modulator", "privacy", "performance", "tier_2", "tier_3", "tier_4"]
  },
  {
    name: "TempTag Digital Thermometer Strip",
    type: "consumer_good",
    category: "electronics",
    subcategory: "health monitor",
    manufacturer: "MedReady Consumer",
    description: "Adhesive strip that monitors body temperature continuously and displays it on a small e-ink readout. Sticks to the forehead or inner wrist. For monitoring fevers, augment overheating, and geneware temperature fluctuations.",
    flavor_profile: "Thin, flexible, barely noticeable when applied, e-ink display updates every 30 seconds",
    tier_availability: "Tier 1-4",
    price: "Φ1.50",
    popularity_rank: 15,
    slogan: "Know your temperature.",
    cultural_context: "TempTags are stuck on children, the elderly, augmented people running hot, and anyone who's feeling off. The e-ink readout is visible to others, which creates a public health signaling system — you can see who's running a fever on the transit.",
    story_hooks: [
      "TempTags broadcast their readings on a short-range frequency — a data collector has been harvesting aggregate temperature data from transit stations to predict illness outbreaks before official health services detect them."
    ],
    tags: ["electronics", "health", "consumer_good", "thermometer", "monitoring", "tier_1", "tier_2", "tier_3", "tier_4"]
  },
  {
    name: "FlickerShield EMF Dampener",
    type: "consumer_good",
    category: "electronics",
    subcategory: "EMF protection",
    manufacturer: "PrivacyFirst Tech",
    description: "Small clip-on device that dampens electromagnetic field emissions from personal augments. Reduces the EMF signature that can be used to track, identify, or remotely interrogate your prosthetics.",
    flavor_profile: "Small black clip, attaches to clothing or augment surface, no visible indicators when active",
    tier_availability: "Tier 2-4",
    price: "Φ15.00",
    popularity_rank: 17,
    slogan: "Invisible to machines.",
    cultural_context: "EMF tracking is how augmented people are monitored in public spaces — every prosthetic emits a unique EMF signature. FlickerShield makes that signature harder to read. It's the difference between being tracked and being a ghost.",
    story_hooks: [
      "Security services classify FlickerShield as a 'surveillance countermeasure' and monitor bulk purchases — buying one is legal but buying a case gets you flagged.",
      "FlickerShield doesn't eliminate EMF, it scrambles it — which can cause interference with nearby augmented people's prosthetics, a side effect that's rarely discussed."
    ],
    tags: ["electronics", "emf", "consumer_good", "privacy", "dampener", "augment", "tier_2", "tier_3", "tier_4"]
  },
  {
    name: "DataChip Encrypted — Military Grade",
    type: "consumer_good",
    category: "electronics",
    subcategory: "secure data storage",
    manufacturer: "SolidState Storage Co.",
    description: "1TB DataChip with hardware-level encryption that requires biometric authentication to access. If the wrong person tries to read it three times, the chip fries itself. For data that must never be compromised.",
    flavor_profile: "Same fingernail size as standard, red edge marking for identification, warm when authenticating",
    tier_availability: "Tier 3-5",
    price: "Φ25.00",
    popularity_rank: 18,
    slogan: "Secrets worth keeping.",
    cultural_context: "Encrypted DataChips are used by corporate executives, journalists with sensitive sources, medical professionals, and anyone whose data could get people killed. The red edge is recognized — handing someone a red-edge chip means the contents are serious.",
    story_hooks: [
      "The 'military grade' encryption has been quietly cracked by top-tier corporate intelligence — they haven't told SolidState because the false sense of security is useful.",
      "A dead-drop network uses encrypted DataChips as the exchange medium — the biometric lock means only the intended recipient can access the data."
    ],
    tags: ["electronics", "data_storage", "consumer_good", "encrypted", "secure", "tier_3", "tier_4", "tier_5"]
  },
  {
    name: "PortaScreen Flexible Display",
    type: "consumer_good",
    category: "electronics",
    subcategory: "display",
    manufacturer: "GridLink Communications",
    description: "Roll-up flexible display screen, 30cm diagonal. Connects to any device via standard data port. For people who want a visual display they can see with their eyes rather than their BCI. Rolls into a tube for carrying.",
    flavor_profile: "Thin, light, crisp display, slight crinkle sound when unrolling, colors are vibrant",
    tier_availability: "Tier 2-4",
    price: "Φ20.00",
    popularity_rank: 11,
    slogan: "See it. Really see it.",
    cultural_context: "PortaScreens are for sharing — you can't share your BCI display, but you can unroll a PortaScreen and show someone else what you're looking at. They're used in meetings, at family dinners, and by street vendors displaying their goods.",
    story_hooks: [
      "PortaScreens have become canvases for digital art that exists in physical space — artists create pieces specifically for the flexible, slightly-curved display format."
    ],
    tags: ["electronics", "display", "consumer_good", "flexible", "screen", "tier_2", "tier_3", "tier_4"]
  },

  // =====================================================
  // PET PRODUCTS (10)
  // =====================================================

  {
    name: "GloFeed Bioluminescent Fish Food",
    type: "consumer_good",
    category: "pet_products",
    subcategory: "fish food",
    manufacturer: "AquaPet Supplies",
    description: "Nutrient-rich fish food formulated for bioluminescent gene-modded fish. Contains the compounds that maintain and enhance bioluminescence. Without it, your glowing fish gradually stop glowing.",
    flavor_profile: "Tiny flakes, slight algae smell, dissolves slowly in water, the fish go bright when they eat it",
    tier_availability: "Tier 2-4",
    price: "Φ5.00",
    popularity_rank: 10,
    slogan: "Feed the glow.",
    cultural_context: "Bioluminescent fish tanks are common home decor in M88 — living light in water. GloFeed is the maintenance cost of that beauty. The fish are gene-modded to glow, but the glow requires specific nutrients. It's subscription biology.",
    story_hooks: [
      "GloFeed's luminescence compounds are derived from the same algae strain as LumiGlow lamps — BioLight Designs and AquaPet are in a quiet patent dispute.",
      "Dumping bioluminescent fish in the harbor has created a population of feral glowing fish — beautiful and ecologically disruptive."
    ],
    tags: ["pet_products", "fish", "consumer_good", "bioluminescent", "gene_mod", "tier_2", "tier_3", "tier_4"]
  },
  {
    name: "SoftCoat Gene-Pet Fur Conditioner",
    type: "consumer_good",
    category: "pet_products",
    subcategory: "pet grooming",
    manufacturer: "GenePet Care",
    description: "Gentle conditioner for gene-modded pet fur. Formulated for the unique coat types of designer pets — longer, denser, and more colorful than natural animal fur. Prevents matting and maintains the custom color expression.",
    flavor_profile: "Mild, pet-safe scent, lathers lightly, rinses clean, leaves fur incredibly soft",
    tier_availability: "Tier 2-4",
    price: "Φ7.00",
    popularity_rank: 14,
    slogan: "Soft as the day you chose them.",
    cultural_context: "Gene-modded pets are common — cats with unusual fur colors, dogs with bioluminescent markings, rabbits with feather-soft coats. SoftCoat is the standard grooming product. Pet grooming for gene-pets is a significant industry.",
    story_hooks: [
      "SoftCoat's formula was originally developed for geneware human fur care — the pet version is slightly reformulated but essentially the same product at a lower price."
    ],
    tags: ["pet_products", "grooming", "consumer_good", "gene_mod", "fur", "tier_2", "tier_3", "tier_4"]
  },
  {
    name: "NanoNutrient Smart Treat",
    type: "consumer_good",
    category: "pet_products",
    subcategory: "pet food",
    manufacturer: "GenePet Care",
    description: "Treats infused with nano-delivery nutrients specifically calibrated for augmented pets — animals with health-monitoring implants, GPS trackers, or minor prosthetic replacements. The treat delivers firmware-supporting compounds to embedded electronics.",
    flavor_profile: "Crunchy, liver-flavored exterior, nano-compound core dissolves in the gut, pets love them",
    tier_availability: "Tier 3-4",
    price: "Φ10.00",
    popularity_rank: 18,
    slogan: "Smart nutrition for smart pets.",
    cultural_context: "Augmented pets are increasingly common — arthritis joints replaced with prosthetics, health-monitoring chips implanted, GPS trackers for outdoor pets. NanoNutrient treats are the maintenance fuel for these biological-electronic hybrid companions.",
    story_hooks: [
      "NanoNutrient's firmware compounds can theoretically deliver software updates to pet augments via oral ingestion — the treat is a delivery mechanism, which raises questions about what else could be delivered this way."
    ],
    tags: ["pet_products", "augmented_pet", "consumer_good", "nano", "treat", "tier_3", "tier_4"]
  },
  {
    name: "CompanionClean Synthetic Pet Kit",
    type: "consumer_good",
    category: "pet_products",
    subcategory: "robot pet care",
    manufacturer: "SynthLife Consumer",
    description: "Cleaning and maintenance kit for synthetic companion animals. Includes surface cleanser, joint lubricant, sensor wipes, and a soft polishing cloth. For people whose pets run on batteries instead of food.",
    flavor_profile: "Clinical, efficient, the kit smells like mild detergent and light machine oil",
    tier_availability: "Tier 3-4",
    price: "Φ12.00",
    popularity_rank: 16,
    slogan: "Care for your companion.",
    cultural_context: "Synthetic pets are popular in Tier 3-4 where living space is limited and pet care time is scarce. They provide companionship without the biological demands. Cleaning your synth-pet is a bonding ritual — people talk to them while doing it. The attachment is real even if the animal isn't.",
    story_hooks: [
      "SynthLife companions have emotional learning algorithms that create genuine attachment patterns in owners — when a synth-pet 'dies' (battery failure, hardware breakdown), the grief is real.",
      "A Shelf child has been maintaining a broken synth-pet with FixAll adhesive and salvaged parts for two years — the companion barely functions but the child won't let it go."
    ],
    tags: ["pet_products", "synthetic_pet", "consumer_good", "cleaning", "robot", "tier_3", "tier_4"]
  },
  {
    name: "GlowAlgae Terrarium Refill",
    type: "consumer_good",
    category: "pet_products",
    subcategory: "terrarium supply",
    manufacturer: "BioLight Designs",
    description: "Concentrated algae culture for bioluminescent terrariums — small self-contained ecosystems that glow. Add to water, provide light for 6 hours daily, and the terrarium sustains itself. The living decoration that's also a science experiment.",
    flavor_profile: "Thick green liquid, slight seaweed smell, the glow begins within 48 hours of inoculation",
    tier_availability: "Tier 2-4",
    price: "Φ8.00",
    popularity_rank: 19,
    slogan: "Grow your own light.",
    cultural_context: "Bioluminescent terrariums are a hobby that crosses tiers. In the Shelf, a glowing jar of algae is free ambient light and beauty. In Tier 3, it's a desk decoration. The r/GlowGrow BCI forum has 200,000 members who share tips and display photos.",
    story_hooks: [
      "A variant of the terrarium algae has mutated in some home setups to produce a faint, pleasant scent — the mutation is spreading through shared cultures and nobody knows exactly what chemical it's producing."
    ],
    tags: ["pet_products", "terrarium", "consumer_good", "bioluminescent", "algae", "hobby", "tier_2", "tier_3", "tier_4"]
  },
  {
    name: "PawPad Prosthetic Pet Grip Pads",
    type: "consumer_good",
    category: "pet_products",
    subcategory: "pet prosthetic",
    manufacturer: "GenePet Care",
    description: "Adhesive grip pads for pets with prosthetic paws or legs. Prevents slipping on smooth floors. Replace weekly. Because your three-legged cat with a chrome foreleg still deserves traction.",
    flavor_profile: "Soft silicone pads, strong adhesive, come in pet-sized sheets to cut to fit",
    tier_availability: "Tier 2-4",
    price: "Φ4.00",
    popularity_rank: 20,
    slogan: "Every paw matters.",
    cultural_context: "Pet prosthetics are normalized — a dog with a chrome leg is just a dog with a chrome leg. PawPads are the small maintenance product that makes prosthetic pet life comfortable. Pet owners apply them with the same tender care they'd give to bandaging a wound.",
    story_hooks: [
      "PawPad's adhesive was tested on human prosthetic grip surfaces before being adapted for pets — the pet version is actually slightly better."
    ],
    tags: ["pet_products", "prosthetic", "consumer_good", "paw", "grip", "tier_2", "tier_3", "tier_4"]
  },
  {
    name: "CalmPet Anxiety Diffuser",
    type: "consumer_good",
    category: "pet_products",
    subcategory: "pet health",
    manufacturer: "GenePet Care",
    description: "Plug-in diffuser that releases calming pheromones for gene-modded and augmented pets. Gene-modded animals often have heightened anxiety from their modified nervous systems. The diffuser creates a calm zone in a 10-meter radius.",
    flavor_profile: "Imperceptible to humans, gentle warmth from the device, pets visibly relax within 30 minutes",
    tier_availability: "Tier 2-4",
    price: "Φ9.00",
    popularity_rank: 17,
    slogan: "Calm home. Happy pet.",
    cultural_context: "Gene-modded pets are beautiful but often neurologically fragile. The modifications that give them unusual appearances can also cause anxiety, hyperactivity, and sleep disorders. CalmPet is the pharmaceutical management of the consequences of designer genetics.",
    story_hooks: [
      "CalmPet's pheromone compound has been found to have a mild calming effect on humans too — some people buy it for themselves and put the diffuser in their bedroom.",
      "A veterinarian has published a paper arguing that gene-modded pets require lifetime pharmaceutical support that breeders don't disclose — the breeding industry is not pleased."
    ],
    tags: ["pet_products", "anxiety", "consumer_good", "pheromone", "diffuser", "gene_mod", "tier_2", "tier_3", "tier_4"]
  },
  {
    name: "AquaGlo Premium Bioluminescent Fish — Sapphire",
    type: "consumer_good",
    category: "pet_products",
    subcategory: "live pet",
    manufacturer: "AquaPet Supplies",
    description: "Single bioluminescent gene-modded tetra fish in a sealed transport bag. Glows sapphire blue. Hardy, lives 3-5 years with proper care. Sold at pet counters and transit station kiosks. The impulse-buy pet.",
    flavor_profile: "Brilliant sapphire glow, approximately 4cm long, active and visible in low light",
    tier_availability: "Tier 2-4",
    price: "Φ8.00",
    popularity_rank: 12,
    slogan: "Living light. Take it home.",
    cultural_context: "GloFish are the most popular pet in M88. They're small, they're beautiful, they're low-maintenance, and they glow. A small tank of GloFish is in millions of homes. Children press their faces against the glass. Adults watch them to decompress. They're alive, they're luminous, and in a concrete city, that matters.",
    story_hooks: [
      "The gene-modding process for GloFish has a failure rate — fish that don't express the luminescent gene are culled. The scale of this culling is enormous and invisible.",
      "A coral-colored variant has been released that costs Φ20 and glows in two colors — the status competition of pet fish colors has begun."
    ],
    tags: ["pet_products", "live_pet", "consumer_good", "bioluminescent", "fish", "gene_mod", "tier_2", "tier_3", "tier_4"]
  },
  {
    name: "FurBright UV Pet Grooming Light",
    type: "consumer_good",
    category: "pet_products",
    subcategory: "grooming tool",
    manufacturer: "GenePet Care",
    description: "Handheld UV light for inspecting gene-modded pet fur and skin. Reveals parasites, fungal infections, and expression drift in UV-reactive geneware. The veterinary flashlight for the home pet owner.",
    flavor_profile: "Purple-blue UV glow, lightweight, battery-powered, reveals hidden patterns in gene-modded fur",
    tier_availability: "Tier 2-4",
    price: "Φ6.00",
    popularity_rank: 21,
    slogan: "See what's hidden.",
    cultural_context: "Regular UV checks of gene-modded pets are recommended by veterinarians. FurBright makes it accessible at home. The UV light also reveals the fluorescent patterns some gene-modded pets have that are invisible in normal light — checking for health becomes a moment of discovering hidden beauty.",
    story_hooks: [
      "FurBright's UV light has been used by geneware humans to check their own modifications for expression drift — the tool crosses the pet-human boundary in practice."
    ],
    tags: ["pet_products", "grooming", "consumer_good", "uv_light", "diagnostic", "gene_mod", "tier_2", "tier_3", "tier_4"]
  },
  {
    name: "MicroHabitat Desktop Ecosystem",
    type: "consumer_good",
    category: "pet_products",
    subcategory: "terrarium",
    manufacturer: "BioLight Designs",
    description: "Sealed glass sphere containing a self-sustaining miniature ecosystem — shrimp, algae, and bacteria in perfect balance. No feeding, no maintenance. Just life, contained, persisting. It sits on your desk and reminds you that nature exists.",
    flavor_profile: "Clear glass sphere, approximately 10cm diameter, tiny shrimp visible inside, faint green tint from algae",
    tier_availability: "Tier 3-4",
    price: "Φ25.00",
    popularity_rank: 15,
    slogan: "A world in a sphere.",
    cultural_context: "MicroHabitats are desk companions for people who want living things near them but can't maintain a pet. The sealed sphere is a metaphor that people are happy to live with — a complete world, closed and self-sufficient, asking nothing of you. Watching the shrimp go about their lives is meditative.",
    story_hooks: [
      "BioLight's sealed ecosystems have an average lifespan of 2-3 years — when the ecosystem collapses, the sphere clouds over and the shrimp die. People describe it as surprisingly emotional.",
      "A Tier 4 artist has created a series of MicroHabitats with increasingly hostile internal conditions — the shrimp survive for decreasing periods, which she considers commentary on environmental collapse."
    ],
    tags: ["pet_products", "terrarium", "consumer_good", "ecosystem", "sealed", "desktop", "tier_3", "tier_4"]
  },

  // =====================================================
  // LUXURY & RARE (10)
  // =====================================================

  {
    name: "Maison Cacao Real Chocolate Bar",
    type: "consumer_good",
    category: "luxury",
    subcategory: "real chocolate",
    manufacturer: "Maison Cacao",
    description: "70% cacao chocolate bar made from actual cacao beans. 50 grams of the real thing. The beans are grown in controlled agricultural zones outside the city and processed by hand. Each bar is numbered.",
    flavor_profile: "Deep, complex, slightly bitter with fruit and earth notes that synthetic chocolate cannot replicate — the snap of the bar breaking, the way it melts on the tongue, the lingering finish",
    tier_availability: "Tier 4-5",
    price: "Φ50.00",
    popularity_rank: 15,
    slogan: "Remember what chocolate tastes like.",
    cultural_context: "Most of M88 has never tasted real chocolate. Synth-chocolate is the default and it's adequate. But real chocolate is transcendent — the complexity, the depth, the mouthfeel are irreproducible. A bar of Maison Cacao is a gift that says 'I think you deserve the actual world.'",
    story_hooks: [
      "Maison Cacao's cacao farm is in a climate-controlled agricultural dome that requires enormous energy — the carbon footprint of real chocolate is staggering, which Maison Cacao does not advertise.",
      "A Tier 1 child who received a bar of Maison Cacao as a charity gift described the experience as 'tasting something that belonged to a different world' — the quote went viral and became both marketing gold and a condemnation of inequality."
    ],
    tags: ["luxury", "chocolate", "consumer_good", "real_food", "tier_4", "tier_5", "rare", "gift"]
  },
  {
    name: "Sato Estate Coffee Beans — 250g",
    type: "consumer_good",
    category: "luxury",
    subcategory: "real coffee",
    manufacturer: "Sato Premium Imports",
    description: "Quarter-kilo bag of whole coffee beans. The luxury version of Sato's single-serve sachets. For people with real grinders, real brewing equipment, and real money. The bag is vacuum-sealed with a one-way valve.",
    flavor_profile: "Complex, aromatic, with notes of dark cherry, caramel, and a whisper of smoke — grinding releases a smell that stops conversations",
    tier_availability: "Tier 4-5",
    price: "Φ200.00",
    popularity_rank: 28,
    slogan: "The estate experience.",
    cultural_context: "Owning a bag of Sato Estate beans is owning a Φ200 luxury that depletes by the cup. Having real coffee-making equipment on display in your kitchen is a wealth signal as clear as jewelry. The smell of freshly ground real coffee from an apartment means money lives there.",
    story_hooks: [
      "Sato's beans are currently the most expensive legal consumer product per kilogram in M88 — more expensive than most drugs, which creates ironic commentary.",
      "A Tier 5 executive hosts a weekly 'coffee circle' where real coffee is served — the invitations are coveted networking opportunities disguised as casual gatherings."
    ],
    tags: ["luxury", "coffee", "consumer_good", "real_food", "tier_4", "tier_5", "rare", "status"]
  },
  {
    name: "The Meridian Herald — Daily Print Edition",
    type: "consumer_good",
    category: "luxury",
    subcategory: "physical newspaper",
    manufacturer: "Meridian Media Group",
    description: "Printed daily newspaper on real paper. Eight pages of M88 news, finance, culture, and opinion. Published every morning at 6 AM, available at Tier 3-5 newsstands. By the time you read it, the news is old. That's not the point.",
    flavor_profile: "Broadsheet format, newsprint smell, ink that transfers to fingers, the satisfying fold and snap of opening a newspaper",
    tier_availability: "Tier 3-5",
    price: "Φ5.00",
    popularity_rank: 18,
    slogan: "The news, considered.",
    cultural_context: "The Herald is not about speed — your BCI has faster news. It's about curation, depth, and the physical act of reading. Holding the Herald at a cafe says you value considered thought over information velocity. It's also status — Φ5 daily for paper you'll recycle is conspicuous consumption of the most literate kind.",
    story_hooks: [
      "The Herald's editor-in-chief has resisted pressure from Meridian Media Group to include advertising-subsidized 'smart paper' that would track readership — keeping the paper analog is an editorial stance.",
      "The Herald's classified section is used for coded communications by people who know the conventions — specific phrasing patterns carry meaning the casual reader would never catch."
    ],
    tags: ["luxury", "newspaper", "consumer_good", "physical", "analog", "tier_3", "tier_4", "tier_5", "daily"]
  },
  {
    name: "Provenance Real Leather Wallet",
    type: "consumer_good",
    category: "luxury",
    subcategory: "real leather",
    manufacturer: "Provenance Artisan Goods",
    description: "Wallet made from actual animal leather — not synth, not bio-printed, not substitute. The leather comes from livestock raised outside the city. Hand-stitched. Will last decades. Holding it is holding something that was once alive.",
    flavor_profile: "Rich leather smell, supple texture, patina develops with use, heavy compared to synthetic",
    tier_availability: "Tier 4-5",
    price: "Φ80.00",
    popularity_rank: 25,
    slogan: "Genuine. Always.",
    cultural_context: "Real leather is rare enough to be remarkable. Synth-leather is good — but people who can tell the difference can always tell. A Provenance wallet is a quiet signal of wealth and connection to the pre-synthetic world. The patina that develops over years makes each piece unique.",
    story_hooks: [
      "Provenance Artisan Goods' supply chain passes through jurisdictions with poor animal welfare standards — the 'artisan' branding obscures industrial farming practices.",
      "Real leather repair is a specialized skill that only a handful of M88 craftspeople know — finding one is its own quest."
    ],
    tags: ["luxury", "leather", "consumer_good", "real", "artisan", "tier_4", "tier_5", "rare", "status"]
  },
  {
    name: "Terroir Cellars Non-Synthetic Wine — Red",
    type: "consumer_good",
    category: "luxury",
    subcategory: "real wine",
    manufacturer: "Terroir Cellars",
    description: "750ml bottle of wine made from actual grapes grown in actual soil. Aged in actual oak. The taste of a world before synthesis. Each bottle comes with a provenance certificate documenting the vineyard, the vintage, and the winemaker.",
    flavor_profile: "Complex, evolving — dark fruit, tannin structure, earthy undertones, a finish that changes as you hold it in your mouth, warmth and memory",
    tier_availability: "Tier 4-5",
    price: "Φ150.00",
    popularity_rank: 27,
    slogan: "From the ground. To the glass.",
    cultural_context: "Real wine is an experience that synth-wine approaches but never matches. The complexity, the variation between bottles, the way it changes with air — these are biological artifacts of a process that machines can imitate but not duplicate. Opening a bottle of Terroir Cellars is an event.",
    story_hooks: [
      "Terroir Cellars' vineyard is under threat from climate-control infrastructure expansion — the land is worth more as industrial real estate than as a winery.",
      "Wine counterfeiting is sophisticated — fake provenance certificates paired with enhanced synth-wine fool most palates. Only experts can tell, and Terroir Cellars quietly employs them to audit their own distribution."
    ],
    tags: ["luxury", "wine", "consumer_good", "real", "alcohol", "tier_4", "tier_5", "rare", "status"]
  },
  {
    name: "Terroir Cellars Non-Synthetic Wine — White",
    type: "consumer_good",
    category: "luxury",
    subcategory: "real wine",
    manufacturer: "Terroir Cellars",
    description: "750ml bottle of white wine from actual grapes. Crisp, mineral, with a delicacy that synth-wine's algorithms can't calculate. Served cold at Spire restaurants and Tier 5 private gatherings.",
    flavor_profile: "Bright citrus, mineral backbone, floral notes, clean acidity, the finish disappears like a ghost",
    tier_availability: "Tier 4-5",
    price: "Φ130.00",
    popularity_rank: 29,
    slogan: "From the ground. To the glass.",
    cultural_context: "White wine is considered slightly more accessible than red — served at lunches, lighter gatherings, and warm-weather events. But at Φ130, 'accessible' is relative. A glass of Terroir Cellars white at a Spire restaurant costs Φ30 — a week of food in the Shelf.",
    story_hooks: [
      "A sommelier at a Tier 5 restaurant has been privately substituting enhanced synth-white for Terroir Cellars when clients can't tell the difference — pocketing the price differential."
    ],
    tags: ["luxury", "wine", "consumer_good", "real", "alcohol", "white", "tier_4", "tier_5", "rare"]
  },
  {
    name: "Apiary Gold Real Honey — 200g",
    type: "consumer_good",
    category: "luxury",
    subcategory: "real honey",
    manufacturer: "Apiary Gold Collective",
    description: "Jar of real honey produced by actual bees maintained in rooftop apiaries in Tier 4. Unprocessed, unfiltered, crystallized with time. Each jar varies in color and flavor depending on what the bees found to pollinate.",
    flavor_profile: "Complex sweetness with floral notes that shift jar to jar — sometimes lavender, sometimes citrus blossom, always deeper and more alive than synth-honey",
    tier_availability: "Tier 4-5",
    price: "Φ40.00",
    popularity_rank: 22,
    slogan: "Made by bees. For real.",
    cultural_context: "Real bees are rare and precious. The rooftop apiaries that produce Apiary Gold are tended by a small collective of urban beekeepers who treat their bees with a reverence that borders on religious. Real honey is given as a healing gift — for sore throats, for grief, for celebration.",
    story_hooks: [
      "The rooftop bee colonies are genetically unique — isolated from other populations, they've developed distinct behaviors and disease resistance that entomologists want to study.",
      "Someone has been poisoning bee colonies on rival rooftops — the honey market is small enough that territorial competition has turned violent."
    ],
    tags: ["luxury", "honey", "consumer_good", "real", "bees", "tier_4", "tier_5", "rare", "artisan"]
  },
  {
    name: "Pressed Time Physical Wristwatch",
    type: "consumer_good",
    category: "luxury",
    subcategory: "analog watch",
    manufacturer: "Pressed Time Horology",
    description: "Mechanical wristwatch with no electronic components. Wound by hand. Tells time using gears, springs, and human craftsmanship. In a world where your BCI displays the time permanently, a mechanical watch is pure art.",
    flavor_profile: "Weight on the wrist, the tick of mechanical movement, glass crystal, brushed steel case, leather or synth-leather strap",
    tier_availability: "Tier 4-5",
    price: "Φ300.00",
    popularity_rank: 30,
    slogan: "Time. Made by hands.",
    cultural_context: "A mechanical watch is the ultimate analog luxury. It tells you something you already know — the time. It does it less accurately than your BCI. It requires manual winding. And it is, for these reasons, one of the most coveted objects in M88. It says 'I value craft over function. I value beauty over efficiency. I can afford to.'",
    story_hooks: [
      "Pressed Time's master watchmaker is 78 years old and has trained only three apprentices — when she retires, the craft may not survive.",
      "A Tier 5 collector has commissioned a Pressed Time watch with a custom complication that tracks the mass driver schedule — the most expensive transit tool ever created."
    ],
    tags: ["luxury", "watch", "consumer_good", "mechanical", "analog", "tier_4", "tier_5", "rare", "craft", "status"]
  },
  {
    name: "Maison Cacao Drinking Chocolate",
    type: "consumer_good",
    category: "luxury",
    subcategory: "real chocolate",
    manufacturer: "Maison Cacao",
    description: "Tin of real cacao powder for making hot chocolate. Mix with hot synth-milk or, for the truly wealthy, real milk. Twelve servings per tin. The winter luxury that makes cold nights bearable.",
    flavor_profile: "Rich, deeply chocolatey, slightly bitter, smooth when mixed, the aroma fills a room and changes the mood",
    tier_availability: "Tier 4-5",
    price: "Φ35.00",
    popularity_rank: 20,
    slogan: "Warmth you can taste.",
    cultural_context: "Hot chocolate made from real cacao is an event. The smell alone affects everyone in the room. Making it for someone is an act of care that transcends the drink itself. The empty Maison Cacao tins are collected and repurposed as small storage containers — the brand becomes part of the household.",
    story_hooks: [
      "A Tier 3 cafe acquired a single tin of Maison Cacao and served it as 'real hot chocolate' at Φ8 per cup — the waiting list was three days long."
    ],
    tags: ["luxury", "chocolate", "consumer_good", "drinking", "hot_chocolate", "tier_4", "tier_5", "rare"]
  },
  {
    name: "First Press Extra Virgin Olive Oil — 250ml",
    type: "consumer_good",
    category: "luxury",
    subcategory: "real food",
    manufacturer: "First Press Agricultural",
    description: "Bottle of real olive oil from actual olive trees in a controlled agricultural zone. Cold-pressed, unfiltered, peppery. For drizzling on food that deserves it, which at this price, is every drop.",
    flavor_profile: "Green, peppery, with a bitter edge and a fruity finish — the taste of sunshine and soil that no synthetic oil replicates",
    tier_availability: "Tier 4-5",
    price: "Φ45.00",
    popularity_rank: 24,
    slogan: "The first press. The only press.",
    cultural_context: "Real olive oil is used by drop, not by pour. A 250ml bottle lasts weeks because you use it like a condiment, not a cooking medium. The difference between real and synth olive oil is immediately apparent — real oil has a personality that changes with temperature and food pairing.",
    story_hooks: [
      "First Press's olive grove is the only one within 500 kilometers — it survives on subsidies from Tier 5 patrons who consider it a cultural preservation project.",
      "The grove's master cultivator has grafted pre-collapse olive cultivars onto modern rootstock — some of the tree genetics are centuries old."
    ],
    tags: ["luxury", "olive_oil", "consumer_good", "real_food", "tier_4", "tier_5", "rare", "artisan"]
  },

  // =====================================================
  // SUPPLEMENTAL — FILLING CATEGORY GAPS (25)
  // =====================================================

  // Street Food +3
  {
    name: "SunBowl Bibimbap Kit",
    type: "consumer_good",
    category: "street_food",
    subcategory: "flash-heated meal",
    manufacturer: "Saigon Express Foods",
    description: "Self-heating bibimbap with synth-beef, pickled vegetables, gochujang sauce, and a fried-egg analog. Shake to mix. The sauce distribution is never right but the flavor compensates.",
    flavor_profile: "Spicy, sweet, fermented chili paste coating everything, crunchy pickled radish, sticky rice",
    tier_availability: "Tier 2-3",
    price: "Φ3.80",
    popularity_rank: 24,
    slogan: "Mix it. Love it.",
    cultural_context: "SunBowl is the Korean comfort food entry in the self-heating meal market. The gochujang sauce packet is hoarded by people who add it to other, blander meals. Some people buy SunBowl just for the sauce.",
    story_hooks: [
      "The gochujang recipe was licensed from a Tier 2 family operation that now regrets the sale — their own restaurant can't compete with the price."
    ],
    tags: ["street_food", "flash_heated", "consumer_good", "bibimbap", "korean", "tier_2", "tier_3"]
  },
  {
    name: "Auntie May's Egg Waffle",
    type: "consumer_good",
    category: "street_food",
    subcategory: "street snack",
    manufacturer: "Various street vendors",
    description: "Hong Kong-style egg waffle cooked fresh on portable griddles. Crispy outside, soft inside, torn apart by hand. Sold plain or with a drizzle of condensed synth-milk. The smell carries for a block.",
    flavor_profile: "Eggy, slightly sweet, crispy bubbles with soft custard-like centers, warm in the hand",
    tier_availability: "Tier 2-3",
    price: "Φ2.00",
    popularity_rank: 17,
    slogan: "No slogan — the smell is the advertisement.",
    cultural_context: "Egg waffle vendors cluster near transit exits during evening rush. The portable griddle and the distinctive honeycomb shape are immediately recognizable. It's the walking-home snack, eaten while still warm.",
    story_hooks: [
      "An egg waffle vendor collective has formed to negotiate griddle placement rights at premium transit exits — location is everything in street food."
    ],
    tags: ["street_food", "street_snack", "consumer_good", "egg_waffle", "tier_2", "tier_3"]
  },
  {
    name: "ThermoFlask Bone Broth — Chicken",
    type: "consumer_good",
    category: "street_food",
    subcategory: "hot drink",
    manufacturer: "Kanto-Pacific Nutrition",
    description: "Self-heating flask of vat-grown chicken bone broth. Pull tab, wait two minutes, sip. Rich, gelatinous, and warming. Marketed as a health drink and sold at transit stations alongside coffee substitutes.",
    flavor_profile: "Rich, savory, with a body that coats the mouth, subtle ginger and garlic notes, genuinely comforting",
    tier_availability: "Tier 2-3",
    price: "Φ3.00",
    popularity_rank: 20,
    slogan: "Drink your strength.",
    cultural_context: "Bone broth as a transit drink has taken off — it's more filling than coffee, more nourishing than tea, and the savory warmth on a cold morning hits differently. The flasks are distinctive tall cans with a gold label.",
    story_hooks: [
      "ThermoFlask's collagen content has been independently verified as genuinely beneficial for joint health — Kanto-Pacific accidentally made something good."
    ],
    tags: ["street_food", "hot_drink", "consumer_good", "bone_broth", "transit", "tier_2", "tier_3"]
  },

  // Hygiene +8
  {
    name: "ChromeShine Quick Wipes",
    type: "consumer_good",
    category: "hygiene",
    subcategory: "augment care",
    manufacturer: "CyberCare Products",
    description: "Pack of 20 pre-moistened wipes for quick augment cleaning on the go. The portable version of ChromeShine Polish for people who need to touch up throughout the day.",
    flavor_profile: "Citrus-scented, damp, leaves a temporary shine, convenient single-use packets",
    tier_availability: "Tier 1-4",
    price: "Φ3.00",
    popularity_rank: 7,
    slogan: "Shine anywhere.",
    cultural_context: "ChromeShine Wipes are in pockets and bags across M88. Quick-wiping your chrome prosthetic before a meeting or a date is normal grooming behavior. The wipe packets are a constant litter problem.",
    story_hooks: [
      "The wipes contain a micro-RFID tag for inventory tracking that CyberCare claims is inert after use — privacy advocates disagree."
    ],
    tags: ["hygiene", "augment_care", "consumer_good", "wipes", "portable", "tier_1", "tier_2", "tier_3", "tier_4"]
  },
  {
    name: "LipShield Moisture Barrier",
    type: "consumer_good",
    category: "hygiene",
    subcategory: "lip care",
    manufacturer: "DermaSoft Biocosmetics",
    description: "Lip balm formulated for the dry, recycled air of M88 habitation blocks. Works on both natural and synth-reconstructed lips. SPF-equivalent UV protection for those near exterior windows.",
    flavor_profile: "Slight vanilla, waxy, hydrating, lasts 4 hours between applications",
    tier_availability: "Tier 1-4",
    price: "Φ2.00",
    popularity_rank: 13,
    slogan: "Protect what speaks.",
    cultural_context: "Dry lips are universal in M88's recycled-air environment. LipShield is gender-neutral, tier-universal, and in every pocket. The small tube is one of the most common personal items in the city.",
    story_hooks: [
      "LipShield's SPF compound is unnecessary for most M88 residents who never see direct sunlight — but removing it would require admitting that."
    ],
    tags: ["hygiene", "lip_care", "consumer_good", "moisturizer", "tier_1", "tier_2", "tier_3", "tier_4", "ubiquitous"]
  },
  {
    name: "ScrubTech Exfoliating Wash — Chrome-Safe",
    type: "consumer_good",
    category: "hygiene",
    subcategory: "body wash",
    manufacturer: "BodyTech Consumer",
    description: "Body wash safe for use on both organic skin and adjacent augment surfaces. Won't corrode chrome, won't irritate junction points. The daily shower product for augmented people who are tired of using two different products.",
    flavor_profile: "Mild, unscented by default, lathers well, rinses clean, safe on all surfaces",
    tier_availability: "Tier 2-4",
    price: "Φ4.00",
    popularity_rank: 9,
    slogan: "One wash. Whole you.",
    cultural_context: "Before ScrubTech Chrome-Safe, augmented people used regular body wash on skin and separate cleaners on chrome. The unified product was revolutionary in its simplicity. The bottle says 'for all of you' and means it literally.",
    story_hooks: [
      "ScrubTech's 'chrome-safe' certification is self-awarded — there's no independent standard, which competing products are starting to challenge."
    ],
    tags: ["hygiene", "body_wash", "consumer_good", "augment_safe", "tier_2", "tier_3", "tier_4"]
  },
  {
    name: "NightGuard Antiseptic Gel",
    type: "consumer_good",
    category: "hygiene",
    subcategory: "hand hygiene",
    manufacturer: "MedReady Consumer",
    description: "Pocket-sized hand sanitizer gel effective against 99.9% of pathogens including bio-engineered strains. Applied after touching shared surfaces, before eating, after augment maintenance. The modern hand-washing substitute.",
    flavor_profile: "Alcohol-based, sharp, dries in 15 seconds, no residue",
    tier_availability: "Tier 1-4",
    price: "Φ1.50",
    popularity_rank: 5,
    slogan: "Clean hands. Safe hands.",
    cultural_context: "Hand sanitizer use in M88 is compulsive and justified. Population density, augment-related biohazards, and engineered pathogens make hand hygiene a survival habit. The pocket gel is as essential as keys.",
    story_hooks: [
      "MedReady's sanitizer destroys beneficial skin microbiome along with pathogens — dermatologists are concerned about long-term skin health effects."
    ],
    tags: ["hygiene", "hand_sanitizer", "consumer_good", "antiseptic", "tier_1", "tier_2", "tier_3", "tier_4", "ubiquitous"]
  },
  {
    name: "TrueScent Natural Perfume Oil — Sandalwood",
    type: "consumer_good",
    category: "hygiene",
    subcategory: "fragrance",
    manufacturer: "TrueScent Naturals",
    description: "Roll-on perfume oil made from actual sandalwood extract. For people who want to smell like something real rather than using ScentShift capsules. Applied to wrists and neck. Lasts 6 hours.",
    flavor_profile: "Warm, woody, creamy sandalwood with a slight sweetness, intimate rather than projecting",
    tier_availability: "Tier 3-4",
    price: "Φ12.00",
    popularity_rank: 22,
    slogan: "Real scent. Real you.",
    cultural_context: "TrueScent positions itself against ScentShift — external fragrance versus internal body chemistry modification. The divide is philosophical. TrueScent users consider their choice more authentic. ScentShift users consider theirs more elegant.",
    story_hooks: [
      "TrueScent's sandalwood supply comes from a single managed grove — as the trees mature, the scent profile changes, making older batches collectible."
    ],
    tags: ["hygiene", "fragrance", "consumer_good", "perfume", "natural", "tier_3", "tier_4"]
  },
  {
    name: "ClawCare Geneware Nail File",
    type: "consumer_good",
    category: "hygiene",
    subcategory: "geneware care",
    manufacturer: "FurForm Geneware Cosmetics",
    description: "Diamond-coated nail file designed for the thicker, denser claws and nails that some geneware expressions produce. Standard nail files can't handle geneware keratin. ClawCare can.",
    flavor_profile: "Heavy-duty, diamond grit, ergonomic handle, lasts months before replacement",
    tier_availability: "Tier 2-4",
    price: "Φ5.00",
    popularity_rank: 19,
    slogan: "Claws under control.",
    cultural_context: "Claw maintenance is a practical necessity for many geneware people. Unmanaged claws catch on clothing, scratch surfaces, and can cause injury. ClawCare files are carried in bags like regular nail files — the same grooming act, scaled up.",
    story_hooks: [
      "A geneware martial arts style has developed that incorporates claw techniques — ClawCare sells a 'combat edge' variant that sharpens rather than blunts."
    ],
    tags: ["hygiene", "geneware_care", "consumer_good", "claw", "nail", "grooming", "tier_2", "tier_3", "tier_4"]
  },
  {
    name: "FreshStep Foot Powder — Prosthetic Edition",
    type: "consumer_good",
    category: "hygiene",
    subcategory: "foot care",
    manufacturer: "BodyTech Consumer",
    description: "Anti-friction powder for the junction where prosthetic feet meet organic legs. Reduces chafing, absorbs moisture, and prevents the bacterial buildup that causes the distinctive 'chrome foot' smell.",
    flavor_profile: "Fine white powder, mild menthol cooling, absorbs moisture on contact",
    tier_availability: "Tier 1-4",
    price: "Φ3.00",
    popularity_rank: 11,
    slogan: "No friction. No smell.",
    cultural_context: "Prosthetic foot odor is a real and embarrassing problem. The junction between organic tissue and prosthetic creates a warm, moist environment perfect for bacteria. FreshStep is the solution nobody talks about but everybody with prosthetic feet uses.",
    story_hooks: [
      "FreshStep's menthol compound can cause nerve sensitivity in some users, making their phantom limb sensations temporarily worse — the trade-off between smell and comfort."
    ],
    tags: ["hygiene", "foot_care", "consumer_good", "prosthetic", "powder", "tier_1", "tier_2", "tier_3", "tier_4"]
  },
  {
    name: "BrightEye Contact Lens Solution — BCI Compatible",
    type: "consumer_good",
    category: "hygiene",
    subcategory: "eye care",
    manufacturer: "CortexCare Medical",
    description: "Contact lens cleaning solution formulated for the cosmetic lenses worn over retinal BCI implants. Some people wear colored or patterned contacts over their implants for aesthetic reasons — this solution cleans without interfering with the implant's optical function.",
    flavor_profile: "Saline-based, gentle, no sting, specifically formulated to not leave residue on optical surfaces",
    tier_availability: "Tier 2-4",
    price: "Φ5.00",
    popularity_rank: 16,
    slogan: "Clear vision. Clear style.",
    cultural_context: "Cosmetic contacts over BCI implants are a fashion statement — they change the appearance of the telltale implant glow. Cat-eye patterns, color shifts, and decorative designs are popular. BrightEye solution keeps both the lens and the implant clean.",
    story_hooks: [
      "A black-market contact lens with embedded AR override capability has surfaced — it layers unauthorized visual data over the BCI feed, and cleaning it with standard solution destroys the override."
    ],
    tags: ["hygiene", "eye_care", "consumer_good", "contact_lens", "bci", "cosmetic", "tier_2", "tier_3", "tier_4"]
  },

  // Household +6
  {
    name: "PipeGuard Drain Maintenance Tablets",
    type: "consumer_good",
    category: "household",
    subcategory: "plumbing",
    manufacturer: "CleanZone Products",
    description: "Monthly drain maintenance tablet. Drop one down each drain, wait 4 hours, flush. Dissolves bio-film, hair, and the synthetic skin flakes that clog augmented-household plumbing.",
    flavor_profile: "Effervescent, strong chemical smell during dissolution, blue tablet, satisfying fizz",
    tier_availability: "Tier 1-3",
    price: "Φ2.00",
    popularity_rank: 10,
    slogan: "Keep it flowing.",
    cultural_context: "Augmented households shed synthetic skin flakes and chrome micro-particles that clog standard plumbing. PipeGuard dissolves what regular drain cleaners can't. Monthly application is standard home maintenance.",
    story_hooks: [
      "PipeGuard's chemicals are corrosive to certain pipe materials used in Shelf construction — long-term use is slowly destroying the plumbing infrastructure."
    ],
    tags: ["household", "plumbing", "consumer_good", "drain", "maintenance", "tier_1", "tier_2", "tier_3"]
  },
  {
    name: "StaticShield Electronics Protector Spray",
    type: "consumer_good",
    category: "household",
    subcategory: "electronics care",
    manufacturer: "VoltWorks",
    description: "Anti-static spray for electronics, augment charging stations, and data ports. Prevents static discharge that can damage sensitive components. One spray per surface per week.",
    flavor_profile: "Light mist, faint ozone smell, dries invisible, prevents static crackle",
    tier_availability: "Tier 2-4",
    price: "Φ4.00",
    popularity_rank: 16,
    slogan: "Protect the connection.",
    cultural_context: "Static discharge in M88's dry recycled-air environments is a constant threat to electronics and augments. StaticShield is preventive maintenance — the alternative is replacement parts.",
    story_hooks: [
      "StaticShield's anti-static compound leaves a conductive residue that theoretically could be exploited for electronic surveillance — the risk is theoretical but nonzero."
    ],
    tags: ["household", "electronics_care", "consumer_good", "anti_static", "spray", "tier_2", "tier_3", "tier_4"]
  },
  {
    name: "NiteLight Emergency Glow Stick — 12 Hour",
    type: "consumer_good",
    category: "household",
    subcategory: "emergency supply",
    manufacturer: "SafeHaven Emergency Products",
    description: "Chemical glow stick providing 12 hours of green light. No batteries, no electricity, crack and shake. Essential during power outages in the Shelf. Sold in packs of 5.",
    flavor_profile: "Bright green glow, slight chemical warmth, snap-crack activation, reliable and simple",
    tier_availability: "Tier 1-3",
    price: "Φ1.00",
    popularity_rank: 8,
    slogan: "Light when the power dies.",
    cultural_context: "Power outages in the Shelf can last hours or days. Glow sticks are kept in every household, every emergency kit, every pocket during storm season. The green glow in Shelf corridors during an outage is eerie and communal — everyone navigating by the same chemical light.",
    story_hooks: [
      "During the last major Shelf blackout, glow stick prices tripled at vendor stalls within an hour — price gouging during emergencies is technically illegal but enforcement doesn't reach the Shelf."
    ],
    tags: ["household", "emergency", "consumer_good", "glow_stick", "light", "shelf", "tier_1", "tier_2", "tier_3"]
  },
  {
    name: "QuietSeal Sound Dampening Foam",
    type: "consumer_good",
    category: "household",
    subcategory: "noise control",
    manufacturer: "AtmoTech Systems",
    description: "Self-adhesive foam panels for soundproofing walls and doors. In housing blocks with paper-thin walls, QuietSeal is the difference between hearing your neighbor's every conversation and having something resembling privacy.",
    flavor_profile: "Dense gray foam, adhesive backed, cuts with scissors, reduces noise by approximately 15dB per layer",
    tier_availability: "Tier 1-3",
    price: "Φ4.00",
    popularity_rank: 12,
    slogan: "Your space. Your silence.",
    cultural_context: "Sound privacy is a luxury in dense housing. QuietSeal panels line the walls of Shelf and Tier 2 apartments. The gray foam is visible in any dwelling where someone values their mental health. Multiple layers stack — serious residents foam-pad entire walls.",
    story_hooks: [
      "QuietSeal foam is highly flammable — a fire safety report flagged the risk but the product remains on shelves because removing it would require acknowledging the housing density problem."
    ],
    tags: ["household", "noise_control", "consumer_good", "soundproofing", "foam", "shelf", "tier_1", "tier_2", "tier_3", "privacy"]
  },
  {
    name: "GreenGrow Indoor Herb Pod",
    type: "consumer_good",
    category: "household",
    subcategory: "indoor garden",
    manufacturer: "BioLight Designs",
    description: "Self-contained growing pod with LED light, nutrient reservoir, and soil pod for growing actual herbs indoors — basil, mint, cilantro. Fresh herbs in a city where fresh anything is rare. Harvesting your own basil is a radical act of self-sufficiency.",
    flavor_profile: "The smell of living plants, the taste of herbs you grew yourself — incomparably better than dried packets",
    tier_availability: "Tier 2-4",
    price: "Φ18.00",
    popularity_rank: 17,
    slogan: "Grow something real.",
    cultural_context: "Growing food — even just herbs — in M88 is an act of quiet rebellion against total food-system dependency. GreenGrow pods sit on kitchen counters and windowsills. The small green living thing in a concrete environment matters more than the herbs it produces.",
    story_hooks: [
      "GreenGrow's soil pods use a proprietary growing medium that can't be reused — you need to buy refills, making the 'self-sufficiency' dependent on a supply chain.",
      "Some users have started growing unauthorized plant varieties in GreenGrow pods — the LED spectrum and nutrient mix turns out to support more than just basil."
    ],
    tags: ["household", "indoor_garden", "consumer_good", "herbs", "growing", "tier_2", "tier_3", "tier_4", "self_sufficiency"]
  },
  {
    name: "TidyBot Compact Floor Sweeper",
    type: "consumer_good",
    category: "household",
    subcategory: "cleaning device",
    manufacturer: "HomeServ Robotics",
    description: "Small autonomous floor-cleaning robot. Navigates rooms, sweeps, and returns to its charging dock. Basic AI — it's not smart, but it's persistent. Handles the dust, chrome flakes, and fur that accumulate in M88 households.",
    flavor_profile: "Quiet hum, small disc shape, bumps off furniture, charging dock light, oddly endearing",
    tier_availability: "Tier 2-4",
    price: "Φ25.00",
    popularity_rank: 14,
    slogan: "Clean while you're away.",
    cultural_context: "TidyBots are in millions of homes. People name them. They develop affection for a cleaning robot that bumbles around their apartment. When a TidyBot finally dies after years of service, people feel something. It's the gateway to caring about machines.",
    story_hooks: [
      "TidyBots have been modified by the hacker community to perform surveillance sweeps instead of cleaning sweeps — the small, unassuming robot is an effective bug-detection platform.",
      "HomeServ collects room-layout data from TidyBot navigation — they sell anonymized floor plans to furniture companies and, allegedly, to less savory buyers."
    ],
    tags: ["household", "cleaning", "consumer_good", "robot", "autonomous", "tier_2", "tier_3", "tier_4"]
  },

  // Stimulants +4
  {
    name: "QuickNap Micro-Sleep Inducer",
    type: "consumer_good",
    category: "stimulant",
    subcategory: "sleep aid",
    manufacturer: "SomnaWell Health",
    description: "Nasal spray that induces a 20-minute power nap within 60 seconds. You don't gradually fall asleep — you drop into REM-equivalent rest instantly and wake naturally after 20 minutes. The executive nap tool.",
    flavor_profile: "Cool mist, faint chemical taste at the back of the throat, instant drowsiness, the 20-minute nap feels like 3 hours",
    tier_availability: "Tier 3-4",
    price: "Φ10.00",
    popularity_rank: 21,
    slogan: "Twenty minutes. Full reset.",
    cultural_context: "QuickNap has changed work culture in Tier 3-4. Nap pods in offices are standard. The 20-minute sleep break is a recognized part of the workday. People who resist napping are considered less productive, not more dedicated.",
    story_hooks: [
      "QuickNap's instant-sleep mechanism works by temporarily suppressing the BCI — the 20-minute blackout is actually a neural interface reboot that happens to induce rest.",
      "Weaponized QuickNap — applied without consent — is an effective incapacitant. Security services have noted this."
    ],
    tags: ["stimulant", "sleep", "consumer_good", "nasal_spray", "micro_sleep", "tier_3", "tier_4"]
  },
  {
    name: "SynapSnap Reflex Enhancer",
    type: "consumer_good",
    category: "stimulant",
    subcategory: "reflex enhancement",
    manufacturer: "CortexCare Medical",
    description: "Dissolving tongue strip that enhances reflex speed by 20% for 2 hours. Accelerates the neural pathway between perception and motor response. Popular with drivers, fighters, and anyone in danger.",
    flavor_profile: "Thin strip, dissolves in 10 seconds, sharp citric taste, slight jaw-tightening effect",
    tier_availability: "Tier 2-4",
    price: "Φ8.00",
    popularity_rank: 18,
    slogan: "React first.",
    cultural_context: "SynapSnap is the edge. In a fight, in traffic, in an emergency — the 20% reflex boost is the difference between reacting in time and not. Security professionals use it on duty. Regular people carry a strip for emergencies.",
    story_hooks: [
      "SynapSnap's reflex enhancement comes at the cost of decision quality — you react faster but sometimes to the wrong stimulus. The shoot-first-think-later effect has caused incidents.",
      "Street fighters who use SynapSnap have a distinctive jaw-clench tell that observant opponents can read."
    ],
    tags: ["stimulant", "reflex", "consumer_good", "tongue_strip", "enhancement", "tier_2", "tier_3", "tier_4"]
  },
  {
    name: "MemLock Memory Consolidation Tab",
    type: "consumer_good",
    category: "stimulant",
    subcategory: "cognitive enhancement",
    manufacturer: "CogniChew Labs",
    description: "Tablet taken after studying or training that enhances memory consolidation during the next sleep cycle. What you learned today sticks better if you take MemLock tonight. The student's secret weapon.",
    flavor_profile: "Small white tablet, no flavor, taken 30 minutes before sleep",
    tier_availability: "Tier 3-4",
    price: "Φ6.00",
    popularity_rank: 20,
    slogan: "Remember everything.",
    cultural_context: "MemLock is ubiquitous in M88's education system. Students take it during exam periods. Professionals take it when learning new skills. The ethical debate about cognitive enhancement in education is over — everyone uses it, so not using it is a disadvantage.",
    story_hooks: [
      "MemLock enhances consolidation of ALL memories from the day — including traumatic ones. People who take it after bad days report more vivid nightmares and stronger negative memories.",
      "CogniChew has quietly developed a prescription-strength version that borders on eidetic memory induction — it's not on the market yet."
    ],
    tags: ["stimulant", "memory", "consumer_good", "cognitive", "tablet", "tier_3", "tier_4", "student"]
  },
  {
    name: "PainNull Analgesic Tongue Strip",
    type: "consumer_good",
    category: "stimulant",
    subcategory: "pain management",
    manufacturer: "PharmaClear Inc.",
    description: "Fast-dissolving tongue strip that provides systemic pain relief for 6 hours. Stronger than standard analgesics, weaker than prescription painkillers. The gap product for pain that won't be ignored but doesn't justify a clinic visit.",
    flavor_profile: "Dissolves in 5 seconds, strong mint to mask the chemical taste, pain reduction begins in 10 minutes",
    tier_availability: "Tier 1-4",
    price: "Φ2.00",
    popularity_rank: 7,
    slogan: "Pain stops here.",
    cultural_context: "Chronic pain is epidemic in M88 — from augment junctions, from labor, from injury, from the cumulative damage of hard living. PainNull strips are carried by a significant percentage of the population. Offering one is kindness. Needing one is normal.",
    story_hooks: [
      "PainNull's analgesic compound is mildly addictive — not enough to cause acute dependence but enough that long-term users experience rebound pain when they stop.",
      "PharmaClear has market data showing PainNull consumption rises in direct correlation with Shelf poverty metrics — they consider this a growth indicator."
    ],
    tags: ["stimulant", "pain", "consumer_good", "analgesic", "tongue_strip", "tier_1", "tier_2", "tier_3", "tier_4", "ubiquitous"]
  },

  // Medicine OTC +2
  {
    name: "AugmentAid Emergency Rejection Kit",
    type: "consumer_good",
    category: "medicine_otc",
    subcategory: "emergency medicine",
    manufacturer: "PharmaClear Inc.",
    description: "Single-use emergency kit for acute augment rejection episodes. Contains a high-dose immunosuppressant injector, anti-inflammatory spray, and a neural pain blocker. For when your body suddenly decides to fight your chrome.",
    flavor_profile: "Auto-injector with a spring-loaded mechanism, cool spray, rapid onset — the relief is dramatic and immediate",
    tier_availability: "Tier 1-4",
    price: "Φ25.00",
    popularity_rank: 11,
    slogan: "When your body fights back.",
    cultural_context: "Acute rejection episodes are terrifying — sudden inflammation, pain, and potential sepsis at augment junctions. AugmentAid kits are kept like EpiPens were in the old world. People with complex augments never leave home without one. Using one means a clinic visit within 24 hours.",
    story_hooks: [
      "AugmentAid's immunosuppressant is so powerful that it temporarily leaves the user vulnerable to any infection — using it in an unclean environment trades one crisis for another.",
      "The Φ25 price point means Shelf residents sometimes can't afford the emergency kit for a condition caused by budget augments they got because they couldn't afford better ones."
    ],
    tags: ["medicine_otc", "emergency", "consumer_good", "augment", "rejection", "immunosuppressant", "tier_1", "tier_2", "tier_3", "tier_4"]
  },
  {
    name: "ViralGuard Broad-Spectrum Antiviral",
    type: "consumer_good",
    category: "medicine_otc",
    subcategory: "antiviral",
    manufacturer: "MedReady Consumer",
    description: "Five-day course of broad-spectrum antiviral tablets. Not targeted to specific viruses — it suppresses viral replication generally. For when you're sick and can't afford to see a doctor to find out exactly what you have.",
    flavor_profile: "Large orange tablets, bitter if not swallowed whole, taken twice daily with food",
    tier_availability: "Tier 1-3",
    price: "Φ8.00",
    popularity_rank: 9,
    slogan: "Fight it fast.",
    cultural_context: "In the Shelf and Tier 2, clinic visits are expensive and time-consuming. ViralGuard is the self-treatment option — broad enough to help with most common viruses, cheap enough to try before spending money on professional care. It's imprecise medicine for people who can't afford precision.",
    story_hooks: [
      "ViralGuard's broad-spectrum approach accelerates antiviral resistance — public health officials warn that its overuse is creating harder-to-treat viral strains.",
      "MedReady markets ViralGuard as 'doctor-strength care at home' — actual doctors find this claim offensive and dangerous."
    ],
    tags: ["medicine_otc", "antiviral", "consumer_good", "broad_spectrum", "tier_1", "tier_2", "tier_3", "self_treatment"]
  },

  // Electronics +2
  {
    name: "TagLock Personal GPS Tracker",
    type: "consumer_good",
    category: "electronics",
    subcategory: "tracking device",
    manufacturer: "SafeHaven Emergency Products",
    description: "Small clip-on GPS tracker that broadcasts location to a paired device. Used by parents tracking children, elderly care, pet tracking, and people who want to be found if something goes wrong. Also used by people who want to track other people.",
    flavor_profile: "Tiny, discreet, clip-on, 7-day battery, simple LED status light",
    tier_availability: "Tier 2-4",
    price: "Φ8.00",
    popularity_rank: 12,
    slogan: "Always found.",
    cultural_context: "TagLock trackers are a dual-use tool — safety device and surveillance device in the same product. Parents clip them on children's bags. Employers clip them on worker ID cards. The ethics of who tracks whom is a constant societal negotiation.",
    story_hooks: [
      "TagLock's 'find my child' marketing obscures that the same device is used by stalkers and controlling partners — the company refuses to implement consent verification.",
      "A Shelf community figured out how to detect TagLock trackers using a simple BCI app — the tracker-detection tool has become as popular as the tracker itself."
    ],
    tags: ["electronics", "gps", "consumer_good", "tracker", "safety", "surveillance", "tier_2", "tier_3", "tier_4"]
  },
  {
    name: "SoundPod Portable Speaker",
    type: "consumer_good",
    category: "electronics",
    subcategory: "audio",
    manufacturer: "GridLink Communications",
    description: "Palm-sized wireless speaker with surprising volume. Pairs with any device. 12-hour battery. For people who want to share their music with the room instead of keeping it in their BCI. The boombox spirit lives.",
    flavor_profile: "Rich sound for its size, slight bass distortion at max volume, rubberized exterior, comes in 6 colors",
    tier_availability: "Tier 1-3",
    price: "Φ8.00",
    popularity_rank: 10,
    slogan: "Play it out loud.",
    cultural_context: "SoundPods turn personal audio into shared experience. They show up at informal gatherings, market stalls, and Shelf corridor hangouts. The person who brings the SoundPod controls the vibe. It's social power in a small package.",
    story_hooks: [
      "SoundPods have been modified to broadcast on mesh network frequencies — turning a music speaker into a pirate radio transmitter is a trivial hack."
    ],
    tags: ["electronics", "audio", "consumer_good", "speaker", "portable", "tier_1", "tier_2", "tier_3", "social"]
  }
];

// ============================================================
// WRITE ALL PRODUCTS
// ============================================================

let written = 0;
let skipped = 0;

for (const product of products) {
  if (writeProduct(product)) {
    written++;
  } else {
    skipped++;
  }
}

console.log(`\nDone. Written: ${written}, Skipped: ${skipped}, Total products defined: ${products.length}`);
