// generate_restaurants.js — 200 restaurant/food venues across the GLMZ corridor
// Run: node generate_restaurants.js
const fs = require('fs');
const path = require('path');
const crypto = require('crypto');

const PLACES_DIR = path.join(__dirname, '..', 'engine', 'data', 'places');
const existing = new Set(fs.readdirSync(PLACES_DIR).map(f => f.toLowerCase()));

function genId() { return crypto.randomBytes(16).toString('hex'); }

let written = 0, skipped = 0;

function writePlace(place) {
  const id = genId();
  const filename = id + '.json';
  if (existing.has(filename)) { console.log('SKIP (id collision): ' + filename); skipped++; return; }
  const entity = {
    id,
    type: 'place',
    name: place.name,
    aliases: place.aliases || [],
    description: place.description,
    atmosphere: place.atmosphere || { sights: [], sounds: [], smells: [], feel: '', tags: [] },
    connections: place.connections || { adjacent_to: [], exits: [], tags: [] },
    frequented_by: place.frequented_by || [],
    notable_locations: place.notable_locations || [],
    coordinates: place.coordinates,
    tags: place.tags,
    related_entities: place.related_entities || []
  };
  fs.writeFileSync(path.join(PLACES_DIR, filename), JSON.stringify(entity, null, 2), 'utf8');
  existing.add(filename);
  console.log('WROTE: ' + place.name);
  written++;
}

// ═══════════════════════════════════════════════════════════════════════════════
// $ TIER 1 — SHELF CHEAP (50 venues)
// Street vendors, synth-noodle shops, nutrient paste, cart food, soup kitchens
// ═══════════════════════════════════════════════════════════════════════════════

writePlace({
  name: "Bao's Last Cart",
  aliases: ["Bao's", "Last Cart"],
  description: "A street cart welded from salvaged auto parts, parked at the same intersection in Pilsen Slab since 2189. Bao Nguyen-Okafor runs it alone, sixteen hours a day, seven days a week. He sells synth-pork buns for Φ2 each, steamed in a repurposed industrial humidifier that shouldn't work but does. The buns are dense, filling, and taste like almost-pork. They are the best thing you can buy for Φ2 in the southern Shelf, and Bao knows it — he doesn't advertise, doesn't negotiate, doesn't make small talk. You hand him two quanta, he hands you a bun. The line never stops.",
  coordinates: { lat: 41.8560, lng: -87.6560 },
  tags: ["place", "street_vendor", "food", "tier_1"],
  related_entities: ["Bao Nguyen-Okafor"],
  frequented_by: ["Shelf workers on break", "Runners grabbing food between jobs", "Anyone with Φ2 and hunger"]
});

writePlace({
  name: "The Paste Bar",
  aliases: ["Paste", "NutriBar"],
  description: "A nutrient paste dispensary on South Halsted operating out of a converted shipping container. Six flavors, all of them lying about what they taste like. 'Chicken Teriyaki' tastes like salt and regret. 'Strawberry Cream' tastes like pink. But a full tube is Φ1.50 and contains everything a human body needs for twelve hours, which is exactly the point. The Paste Bar is not food — it is fuel, dispensed by a machine that doesn't judge you for needing it. The owner, Kemi Strand-Asante, maintains three of these containers across the southern corridor and considers herself a public health worker, not a restaurateur.",
  coordinates: { lat: 41.8430, lng: -87.6460 },
  tags: ["place", "street_vendor", "food", "tier_1"],
  related_entities: ["Kemi Strand-Asante"],
  frequented_by: ["Shelf residents on tight budgets", "Night shift workers", "People who need calories, not comfort"]
});

writePlace({
  name: "Uncle Jun's Synth-Noodle Window",
  aliases: ["Jun's", "The Window", "Noodle Window"],
  description: "A hole in a wall. Literally — someone knocked a hole in the side of a condemned apartment building in Wicker Park Shelf, installed a counter and a wok burner behind it, and started selling noodles. Jun Park-Mensah is seventy-one, deaf in one ear, and makes synth-noodles in a broth that has no right being as good as it is. The secret, he'll tell anyone who asks, is MSG. Just an enormous amount of MSG. A bowl is Φ3 and comes in one size: large. There are no seats — you stand on the sidewalk and eat, or you take it with you. The line forms at 6 PM and the noodles run out by 9.",
  coordinates: { lat: 41.9100, lng: -87.6770 },
  tags: ["place", "street_vendor", "food", "tier_1"],
  related_entities: ["Jun Park-Mensah"],
  frequented_by: ["Wicker Park Shelf residents", "Noodle devotees", "Late shift workers heading home"]
});

writePlace({
  name: "Solidarity Soup",
  aliases: ["Solidarity", "The Soup"],
  description: "A mutual aid soup kitchen operating from the basement of a former Catholic church in Bridgeport. Volunteers from the Bridgeport Collective cook three hundred liters of soup every day using donated, scavenged, and occasionally stolen ingredients. The soup changes daily because the ingredients change daily. Monday might be potato. Tuesday might be whatever root vegetable someone found. The kitchen has been running continuously for eleven years and has never missed a day. You eat for free. If you can, you leave something — Φ, labor, ingredients. If you can't, you eat for free. Nobody asks.",
  coordinates: { lat: 41.8380, lng: -87.6510 },
  tags: ["place", "restaurant", "food", "tier_1"],
  related_entities: ["Bridgeport Collective"],
  frequented_by: ["Shelf families", "Unhoused corridor residents", "Off-duty mutual aid volunteers", "Anyone hungry"]
});

writePlace({
  name: "Fatima's Pretzel Rack",
  aliases: ["Fatima's", "Pretzel Rack"],
  description: "A pretzel stand on a wheeled cart that Fatima Johansson-Diallo pushes through the Milwaukee Shelf districts on a route she's been walking for eight years. She makes soft pretzels from real flour — actual grain flour, not synth — which is either a luxury or a statement depending on how you read it. A plain pretzel is Φ4. With mustard seed paste, Φ5. She bakes them in a portable oven powered by a salvaged fuel cell, and the smell travels half a block in every direction, which is her only marketing strategy. It works.",
  coordinates: { lat: 43.0230, lng: -87.9120 },
  tags: ["place", "street_vendor", "food", "tier_1"],
  related_entities: ["Fatima Johansson-Diallo"],
  frequented_by: ["Milwaukee Shelf pedestrians", "Children following the smell", "Regulars who know her route"]
});

writePlace({
  name: "Hot Dog Theorem",
  aliases: ["Theorem", "HDT"],
  description: "A hot dog cart parked outside the ruins of the University of Chicago campus. The owner, a former computational mathematics adjunct named Dr. Priya Nakamura-Owusu, was denied tenure in 2191 and started selling hot dogs the next day. She's been here since. The dogs are vat-protein, snapped into natural casings she sources from a guy in the Stockyards, and served on synth-bread with a rotating selection of toppings she calls 'proofs.' Today's proof might be kimchi-sauerkraut fusion. Tomorrow's might be pickled beet relish. She will explain the mathematical reasoning behind each topping if asked. Do not ask unless you have time.",
  coordinates: { lat: 41.7890, lng: -87.5990 },
  tags: ["place", "street_vendor", "food", "tier_1"],
  related_entities: ["Dr. Priya Nakamura-Owusu"],
  frequented_by: ["South Side workers", "Former academics", "Anyone who appreciates hot dogs with footnotes"]
});

writePlace({
  name: "Wrap & Walk",
  aliases: ["W&W", "Wrap Walk"],
  description: "A vat-protein wrap stand in a doorway on North Milwaukee Avenue. Three wraps on the menu: chicken-style, beef-style, and 'whatever this is' — the third option changes daily and is always the best one. Wraps are Φ3 each, made in thirty seconds flat by Tomasz Eriksson-Bello, who has the dead-eyed efficiency of a man who has made approximately four hundred thousand wraps and will make four hundred thousand more. The hot sauce is homemade and genuinely dangerous. He calls it 'Consent Required' because you have to specifically ask for it and acknowledge the consequences.",
  coordinates: { lat: 41.9200, lng: -87.6870 },
  tags: ["place", "street_vendor", "food", "tier_1"],
  related_entities: ["Tomasz Eriksson-Bello"],
  frequented_by: ["Lunch crowd from the nearby workshops", "Spice enthusiasts", "People in a hurry"]
});

writePlace({
  name: "The Synth Ramen Trough",
  aliases: ["The Trough", "Ramen Trough"],
  description: "Calling it a restaurant is generous. It's a counter with eight stools bolted to the floor of what used to be a laundromat in Logan Square. Yuki Hassan-Petersen makes one thing: synth-ramen. One broth, one noodle type, Φ4. You can add a vat-egg for Φ1. The broth is cloudy, fatty, and simmers in a pot that hasn't been fully emptied in six years — each batch builds on the residue of the last, creating a flavor profile that is technically a health code violation and practically the best bowl of cheap ramen in the northern corridor. Yuki does not discuss the pot. The pot is the pot.",
  coordinates: { lat: 41.9240, lng: -87.7010 },
  tags: ["place", "restaurant", "food", "tier_1"],
  related_entities: ["Yuki Hassan-Petersen"],
  frequented_by: ["Logan Square Shelf regulars", "Ramen chasers", "People who don't ask questions about the pot"]
});

writePlace({
  name: "Corner Slice",
  aliases: ["Corner", "The Slice"],
  description: "Pizza by the slice from a window on 47th Street in Back of the Yards. The dough is synth, the cheese is vat-cultured, and the sauce is canned. A slice is Φ2. It is objectively mediocre pizza by any standard, and it sells three hundred slices a day because it is hot, it is cheap, and it is there. Roshani Andersen-Nkomo runs the operation with her two teenage sons, who have opinions about the pizza that their mother does not invite. The oven is a converted industrial heating unit that makes the entire block smell like bread, which is the nicest thing that block has smelled in decades.",
  coordinates: { lat: 41.8080, lng: -87.6570 },
  tags: ["place", "street_vendor", "food", "tier_1"],
  related_entities: ["Roshani Andersen-Nkomo"],
  frequented_by: ["Back of the Yards workers", "School kids", "Anyone within smelling distance"]
});

writePlace({
  name: "Auntie Efe's Pot",
  aliases: ["Auntie Efe's", "The Pot"],
  description: "A soup and stew vendor operating from a converted utility closet in a Milwaukee Shelf residential tower. Efe Lindqvist-Amadi cooks West African-inspired stews using whatever protein is cheapest that week — usually vat-chicken, sometimes synth-goat, once memorably something she refused to identify but everyone agreed was excellent. A bowl with fufu is Φ5, which is expensive by Shelf standards but worth it because Auntie Efe's cooking is the kind that makes you forget where you are for the duration of the meal. She feeds corridor kids for free and considers this non-negotiable.",
  coordinates: { lat: 43.0340, lng: -87.9210 },
  tags: ["place", "restaurant", "food", "tier_1"],
  related_entities: ["Efe Lindqvist-Amadi"],
  frequented_by: ["Milwaukee Shelf families", "Corridor kids", "Homesick diaspora residents"]
});

writePlace({
  name: "Griddle Ghost",
  aliases: ["Ghost Griddle", "GG"],
  description: "A breakfast cart that appears at different locations across Chicago's Shelf every morning, never the same spot twice in a row. The operator, known only as Ghost, makes synth-egg sandwiches on griddle-pressed bread for Φ3. The eggs are surprisingly convincing. The bread is hot and slightly crispy. Ghost does not speak, communicates entirely through a handwritten menu board, and vanishes by 10 AM. Attempts to track Ghost's pattern have become a minor Shelf pastime. Nobody has succeeded.",
  coordinates: { lat: 41.8710, lng: -87.6280 },
  tags: ["place", "street_vendor", "food", "tier_1"],
  related_entities: [],
  frequented_by: ["Early risers", "Shelf workers heading to day labor", "Ghost-trackers"]
});

writePlace({
  name: "Kettle Row",
  aliases: ["The Kettles"],
  description: "A communal soup kitchen on Green Bay's west side, run by a rotating crew of volunteers from the Lakeshore Mutual Aid Network. Three industrial kettles, each big enough to bathe in, produce a continuous supply of soup from dawn to midnight. The recipe is 'whatever we have plus water plus heat plus time.' On good days, it's hearty and filling. On lean days, it's warm water with nutritional supplements dissolved in it. Both versions are free. Kettle Row has fed more people in Green Bay than any restaurant in the city's history, and nobody who works there considers that an achievement — they consider it a failure of every system that made Kettle Row necessary.",
  coordinates: { lat: 44.5080, lng: -88.0200 },
  tags: ["place", "restaurant", "food", "tier_1"],
  related_entities: ["Lakeshore Mutual Aid Network"],
  frequented_by: ["Green Bay's unhoused population", "Day laborers", "Mutual aid volunteers", "Anyone who needs a meal"]
});

writePlace({
  name: "The Calorie Counter",
  aliases: ["Cal Counter", "CC"],
  description: "A nutrient paste dispensary in Racine that takes the concept to its logical extreme: each tube is labeled not with a flavor but with its exact caloric and nutritional content. Tube 2200 has 2200 calories. Tube 1500 has 1500. The paste tastes like chalky vanilla regardless of which tube you pick, and costs Φ0.50 per 500 calories. The owner, Dmitri Svensson-Achebe, is a former nutritional scientist who lost his corporate position and decided the Shelf needed efficient fuel, not pretend food. He is correct and joyless about it.",
  coordinates: { lat: 42.7260, lng: -87.7830 },
  tags: ["place", "street_vendor", "food", "tier_1"],
  related_entities: ["Dmitri Svensson-Achebe"],
  frequented_by: ["Racine Shelf residents", "Long-haul corridor travelers", "People who view food as mathematics"]
});

writePlace({
  name: "Nana Kofi's Rice Box",
  aliases: ["Nana's", "Rice Box"],
  description: "A rice-and-stew window in Humboldt Park Shelf. Nana Kofi Johansson-Mensah makes jollof-style rice with vat-protein in a spice blend she brought from her grandmother's kitchen and has never written down. A box is Φ4 and big enough to split between two people if neither of them is particularly hungry. The rice is real — she buys it at cost from a Circuit-tier supplier who gives her the broken grains that won't sell upmarket. Broken rice cooks better anyway, she says, and she's right.",
  coordinates: { lat: 41.9020, lng: -87.7020 },
  tags: ["place", "street_vendor", "food", "tier_1"],
  related_entities: ["Nana Kofi Johansson-Mensah"],
  frequented_by: ["Humboldt Park residents", "West Side workers", "Rice devotees"]
});

writePlace({
  name: "Smoke Pipe",
  aliases: ["The Pipe"],
  description: "A vat-meat smoker built inside a section of decommissioned sewer pipe in Gary. The pipe is four meters in diameter and ten meters long, laid on its side in a vacant lot, with a fire pit at one end and a service window cut into the other. Marcus Otieno-Lindgren tends the smoker and sells pulled vat-pork sandwiches for Φ4. The smoke flavor is genuine — real wood, real fire, real time — and it transforms the vat-protein into something that almost passes for the real thing. The line starts at 11 AM and the meat is gone by 2 PM. Marcus does not hurry.",
  coordinates: { lat: 41.5930, lng: -87.3460 },
  tags: ["place", "street_vendor", "food", "tier_1"],
  related_entities: ["Marcus Otieno-Lindgren"],
  frequented_by: ["Gary industrial workers", "Smoke chasers from across the southern corridor", "Lunch crowd"]
});

writePlace({
  name: "Congee Alley",
  aliases: ["The Alley"],
  description: "Three congee vendors in a Chinatown alley who have been in silent competition for nine years. Each sells rice porridge with various toppings for Φ3-5. The vendors — Lin Osei-Chang, Mei Andersson-Hu, and Shu Park-Ibrahim — do not speak to each other, do not acknowledge each other's existence, and make incrementally better congee every week in an arms race that benefits everyone who eats here. Regulars have fierce loyalties. Newcomers are encouraged to try all three and choose. Choosing wrong is apparently possible, though nobody agrees on what wrong means.",
  coordinates: { lat: 41.8520, lng: -87.6320 },
  tags: ["place", "street_vendor", "food", "tier_1"],
  related_entities: ["Lin Osei-Chang", "Mei Andersson-Hu", "Shu Park-Ibrahim"],
  frequented_by: ["Chinatown residents", "Congee partisans", "Food tourists from the Circuit"]
});

writePlace({
  name: "The Φ1 Window",
  aliases: ["Dollar Window", "One-Quanta"],
  description: "A window in a wall in Englewood that sells exactly one thing: a vat-protein patty on synth-bread for Φ1. No condiments, no options, no variations. The patty is thin, the bread is soft, and the entire transaction takes four seconds. Whoever operates it is never visible — the food appears on a tray in the window, you leave your quanta in a dish, you take the food. The dish has never been stolen from. This is either remarkable community trust or remarkable community awareness of what happens to people who steal from the Φ1 Window. Both explanations circulate.",
  coordinates: { lat: 41.7780, lng: -87.6440 },
  tags: ["place", "street_vendor", "food", "tier_1"],
  related_entities: [],
  frequented_by: ["Englewood residents", "People with exactly Φ1", "The desperate and the curious"]
});

writePlace({
  name: "Pier Dumplings",
  aliases: ["Pier Dumps", "The Dumpling Pier"],
  description: "A dumpling stand on the Milwaukee lakefront built on the remnants of a collapsed fishing pier. Olga Kimura-Petersen makes dumplings — synth-pork, synth-cabbage, or mixed — and steams them in bamboo baskets over a propane burner. Six dumplings for Φ4. They're good. Not transformative, not life-changing, just good: hot, savory, properly sealed so the juice stays inside, served with a soy-vinegar dip that has actual garlic in it. In the Shelf, 'just good' is a luxury most people can't afford, which makes Olga's pier one of the most popular lunch spots on the Milwaukee waterfront.",
  coordinates: { lat: 43.0370, lng: -87.8930 },
  tags: ["place", "street_vendor", "food", "tier_1"],
  related_entities: ["Olga Kimura-Petersen"],
  frequented_by: ["Milwaukee dock workers", "Lakefront pedestrians", "Dumpling enthusiasts"]
});

writePlace({
  name: "Tamale Marta",
  aliases: ["Marta's", "Marta's Tamales"],
  description: "Marta Svensson-Ramirez sells tamales from a cooler strapped to a hand cart in Waukegan. The tamales are wrapped in real corn husks — she grows the corn herself in a rooftop plot that produces just enough for her operation — and filled with synth-chicken in a red chile paste that she makes from dried peppers traded up from the southern agricultural zones. Six tamales for Φ5. They sell out by noon every day. Marta has been offered Circuit-tier backing to expand her operation three times and has declined each time. 'If I make more,' she says, 'they won't be mine anymore.'",
  coordinates: { lat: 42.3630, lng: -87.8440 },
  tags: ["place", "street_vendor", "food", "tier_1"],
  related_entities: ["Marta Svensson-Ramirez"],
  frequented_by: ["Waukegan Shelf residents", "Early risers who know the schedule", "Tamale devotees"]
});

writePlace({
  name: "CaloBlock",
  aliases: ["The Block"],
  description: "A calorie bar dispensary in Kenosha shaped like a vending machine but operated by a person — Kwesi Lindqvist-Owusu sits inside a booth behind the machine's facade and hand-presses calorie bars from a mixture of oats, synth-protein powder, dried fruit paste, and binding agents. Each bar is Φ1 and provides roughly 600 calories. They taste like compressed ambition. Kwesi makes about 400 bars a day and sells every one. He could automate the process but says the hand-pressing is the only thing that keeps him from losing his mind.",
  coordinates: { lat: 42.5840, lng: -87.8210 },
  tags: ["place", "street_vendor", "food", "tier_1"],
  related_entities: ["Kwesi Lindqvist-Owusu"],
  frequented_by: ["Kenosha commuters", "Corridor travelers", "People who need portable calories"]
});

writePlace({
  name: "Fried Everything",
  aliases: ["FE", "Fry Stand"],
  description: "A deep-fry cart in Austin Shelf, Chicago, that will fry anything you bring it. Literally anything. Bring a vat-protein slab, they'll fry it. Bring a synth-vegetable, they'll fry it. Bring something unidentifiable, they'll fry it and not ask. The base price is Φ2 for frying services; the cart also sells its own pre-battered synth-fish strips for Φ3. Operated by twin brothers Ade and Olu Gustafsson-Adeyemi, who argue constantly about oil temperature and agree on nothing except that everything is better fried.",
  coordinates: { lat: 41.8960, lng: -87.7650 },
  tags: ["place", "street_vendor", "food", "tier_1"],
  related_entities: ["Ade Gustafsson-Adeyemi", "Olu Gustafsson-Adeyemi"],
  frequented_by: ["Austin Shelf residents", "People with items that need frying", "Oil enthusiasts"]
});

writePlace({
  name: "The Broth Pipe",
  aliases: ["Broth Pipe", "BP"],
  description: "A bone broth stand built into the wall of a residential tower in South Shore Shelf. The broth — made from vat-bones simmered for seventy-two hours with ginger, garlic, and star anise — dispenses from an actual pipe protruding from the wall into a cup you bring yourself. No cup, no broth. A fill is Φ2. The broth is hot, rich, and medicinal in the way that very good broth has always been. Whoever makes it operates from inside the building and has never been seen. Residents of the tower claim not to know. The broth appears at 5 AM and the pipe runs dry by noon.",
  coordinates: { lat: 41.7600, lng: -87.5770 },
  tags: ["place", "street_vendor", "food", "tier_1"],
  related_entities: [],
  frequented_by: ["South Shore morning crowd", "People recovering from illness", "BYOC (bring your own cup) regulars"]
});

writePlace({
  name: "Green Bay Community Table",
  aliases: ["Community Table", "The Table"],
  description: "A long table — literally, a twelve-meter table built from reclaimed lumber — set up in the courtyard of a former school in Green Bay's east side. Every evening at 6 PM, volunteers from four different mutual aid groups bring whatever they've cooked and lay it out. Some of it is good. Some of it is survival-grade. All of it is free. You sit, you eat, you talk to whoever sits next to you. The Table has become the de facto community center for Green Bay's Shelf, not because anyone planned it but because feeding people together turns strangers into neighbors.",
  coordinates: { lat: 44.5190, lng: -88.0080 },
  tags: ["place", "restaurant", "food", "tier_1"],
  related_entities: ["Green Bay Mutual Aid Coalition"],
  frequented_by: ["Green Bay Shelf residents", "Families", "Mutual aid volunteers", "Lonely people"]
});

writePlace({
  name: "Grillmother",
  aliases: ["GM"],
  description: "A woman known only as Grillmother operates a charcoal grill on the shoulder of the I-94 corridor between Milwaukee and Chicago, at a rest point used by corridor travelers. She grills vat-meat skewers over actual charcoal — a luxury at this price point — and sells them for Φ3 each. The skewers are seasoned with a spice blend that tastes like suya, which suggests West African culinary heritage, but Grillmother does not confirm this or anything else. She grills. You eat. The smoke is visible from the maglev line, and passengers have been known to wave.",
  coordinates: { lat: 42.4500, lng: -87.8400 },
  tags: ["place", "street_vendor", "food", "tier_1"],
  related_entities: [],
  frequented_by: ["I-94 corridor travelers", "Long-haul transport workers", "Maglev passengers who wish they could stop"]
});

writePlace({
  name: "The Crumb",
  aliases: ["Crumb"],
  description: "A bakery — using the term loosely — in a converted closet in Uptown Shelf. Ada Johansson-Kimathi bakes flatbread from synth-flour on a salvaged pizza stone heated by an electric element. The bread is Φ1 per piece, and she makes thirty pieces a day, which is all the element can handle before it overheats. The bread is warm, slightly charred, and somehow better than it has any right to be. She also sells a spread made from processed legume paste that she seasons with cumin and lemon juice. Bread plus spread is Φ2 and constitutes the most dignified meal available in Uptown for under Φ5.",
  coordinates: { lat: 41.9660, lng: -87.6540 },
  tags: ["place", "street_vendor", "food", "tier_1"],
  related_entities: ["Ada Johansson-Kimathi"],
  frequented_by: ["Uptown Shelf residents", "Morning bread line regulars"]
});

writePlace({
  name: "Chili Trench",
  aliases: ["The Trench"],
  description: "A chili stand in the basement of a Cicero warehouse, accessible by a set of stairs that descend below street level — hence the name. Hector Lindberg-Osei makes a single pot of chili every day, enough for about eighty bowls, using vat-beef, synth-kidney beans, and a chili powder he buys in bulk from a Circuit-tier spice dealer. A bowl is Φ3 with synth-cornbread. The chili is thick, aggressively spiced, and served at a temperature that suggests Hector is testing your commitment. Regulars bring their own bowls. First-timers get disposable cups and a look of mild pity.",
  coordinates: { lat: 41.8450, lng: -87.7540 },
  tags: ["place", "restaurant", "food", "tier_1"],
  related_entities: ["Hector Lindberg-Osei"],
  frequented_by: ["Cicero warehouse workers", "Chili chasers", "People who own their own bowls"]
});

writePlace({
  name: "Ration Mama",
  aliases: ["RM"],
  description: "A nutrient ration distribution point in Sheboygan that started as emergency disaster relief and never stopped. The disaster was the collapse of Sheboygan's last public food program in 2193. Adama Eriksson-Diallo took the remaining supplies, set up in a parking structure, and has been distributing nutrient rations — a combination of paste, bars, and occasionally actual cooked food — every day since. She calls it Ration Mama because that's what the children call her. The rations are free. Donations are accepted but never requested. Adama considers asking for money to be a failure of the model.",
  coordinates: { lat: 43.7510, lng: -87.7140 },
  tags: ["place", "restaurant", "food", "tier_1"],
  related_entities: ["Adama Eriksson-Diallo"],
  frequented_by: ["Sheboygan Shelf families", "Children", "People who have nowhere else to eat"]
});

writePlace({
  name: "Taco Pipe",
  aliases: ["The Pipe"],
  description: "A taco stand built into the side of a decommissioned water main in Joliet. The pipe is exposed where it surfaces above ground, and someone cut a section open, installed a flat-top griddle inside, and started making tacos. Synth-carne asada on synth-tortillas, Φ2 each. The cook, known only as Pipe, makes them fast and without ceremony. Two hundred tacos a day, every day. The salsa verde is the real draw — made from tomatillos grown in a hydroponic setup inside the pipe itself, which gets enough heat from the griddle to sustain a small growing operation. Pipe does not explain how this works. It works.",
  coordinates: { lat: 41.5250, lng: -88.0830 },
  tags: ["place", "street_vendor", "food", "tier_1"],
  related_entities: [],
  frequented_by: ["Joliet workers", "Corridor commuters", "Salsa verde devotees"]
});

writePlace({
  name: "Porridge House",
  aliases: ["PH"],
  description: "A porridge kitchen in Appleton operating from a former laundromat. Ingrid Okafor-Strand serves oat porridge — real oats, not synth — with a rotating selection of toppings: dried fruit, nut butter paste, honey substitute, or just salt. A bowl is Φ3. The porridge is thick, hot, and unremarkable in every way except that it is made with actual grain by a person who cares whether you eat today. Ingrid opens at 5 AM for the factory shift workers and doesn't close until the pot is empty, which is usually around 2 PM. She has done this for four years and plans to do it until she can't.",
  coordinates: { lat: 44.2620, lng: -88.4150 },
  tags: ["place", "restaurant", "food", "tier_1"],
  related_entities: ["Ingrid Okafor-Strand"],
  frequented_by: ["Appleton factory workers", "Early morning commuters", "People who find comfort in oats"]
});

writePlace({
  name: "Skewer Alley",
  aliases: ["Skewers"],
  description: "A row of three skewer vendors in a Waukesha back alley, each selling variations on grilled vat-meat on sticks. Φ2 per skewer. The vendors — Kofi Petersen-Asante, Yara Lindqvist-Hassan, and Diego Strand-Nakamura — work side by side with a camaraderie that their Congee Alley counterparts in Chinatown would find bewildering. They share charcoal, cover each other's shifts, and argue about marinades with the passion of people who genuinely love what they do, even if what they do is grill cheap protein in an alley for a living.",
  coordinates: { lat: 43.0120, lng: -88.2310 },
  tags: ["place", "street_vendor", "food", "tier_1"],
  related_entities: ["Kofi Petersen-Asante", "Yara Lindqvist-Hassan", "Diego Strand-Nakamura"],
  frequented_by: ["Waukesha residents", "Alley regulars", "People who eat with their hands"]
});

writePlace({
  name: "Dawn Bread",
  aliases: ["Dawn"],
  description: "A bread cart that appears outside the Oshkosh labor exchange every morning at 4:30 AM. By the time workers line up for day assignments at 5 AM, there's fresh flatbread available for Φ1 per piece. The baker, Sahara Gustafsson-Chen, starts work at 2 AM in a kitchen she shares with three other Shelf businesses on a time-rotation basis. Her bread is simple, warm, and serves a single purpose: making sure the people who do the hardest work start the day with something in their stomachs. She considers this a minimum standard of civilization, not charity.",
  coordinates: { lat: 44.0250, lng: -88.5430 },
  tags: ["place", "street_vendor", "food", "tier_1"],
  related_entities: ["Sahara Gustafsson-Chen"],
  frequented_by: ["Oshkosh day laborers", "Early morning workers", "Labor exchange crowd"]
});

writePlace({
  name: "The Vat Shack",
  aliases: ["Vat Shack"],
  description: "A shack — genuinely a shack, built from corrugated metal and optimism — on the outskirts of Fond du Lac, serving vat-protein in three forms: fried, grilled, or boiled. Each is Φ3 with a side of synth-rice. The owner, Emeka Johansson-Silva, left a Circuit-tier job to open this place and refuses to discuss why. The food is basic but prepared with a care that elevates it above its ingredients. Emeka seasons everything by hand, tastes everything before it goes out, and personally apologizes on days when the vat-protein delivery is worse than usual.",
  coordinates: { lat: 43.7750, lng: -88.4470 },
  tags: ["place", "restaurant", "food", "tier_1"],
  related_entities: ["Emeka Johansson-Silva"],
  frequented_by: ["Fond du Lac workers", "Highway travelers", "People who respect the effort"]
});

writePlace({
  name: "Egg Window",
  aliases: ["The Egg"],
  description: "A window in a building in South Chicago that sells synth-egg sandwiches from 5 AM to 8 AM. One sandwich, Φ2, eggs scrambled with salt on toasted synth-bread. That's it. The operation is run by an elderly man named Jiro Lindberg-Mensah who has never explained why he only operates for three hours a day. He makes exactly one hundred sandwiches and stops. Some mornings the line is longer than one hundred people, and the people at the back simply leave, understanding that the Egg Window is a finite resource, like patience or clean water.",
  coordinates: { lat: 41.7390, lng: -87.5540 },
  tags: ["place", "street_vendor", "food", "tier_1"],
  related_entities: ["Jiro Lindberg-Mensah"],
  frequented_by: ["South Chicago early risers", "Factory shift workers", "The first hundred people"]
});

writePlace({
  name: "Vitamin Silo",
  aliases: ["The Silo"],
  description: "A converted grain silo on the outskirts of Manitowoc repurposed as a nutrient supplement dispensary and soup kitchen hybrid. The operator, a collective called Silo Crew, produces fortified soup by adding medical-grade vitamin and mineral supplements to whatever broth base they can source that day. A bowl is free but comes with a lecture about nutritional deficiency from whoever's on ladle duty. The lectures are well-intentioned and interminable. The soup is adequate. The vitamins are the actual product. Silo Crew has reduced malnutrition rates in Manitowoc's Shelf districts by measurable percentages, which they track on a whiteboard inside the silo.",
  coordinates: { lat: 44.0890, lng: -87.6580 },
  tags: ["place", "restaurant", "food", "tier_1"],
  related_entities: ["Silo Crew"],
  frequented_by: ["Manitowoc Shelf residents", "People with nutritional deficiencies", "Anyone willing to endure a lecture for free vitamins"]
});

writePlace({
  name: "The Bench",
  aliases: ["Bench Lunch"],
  description: "Not a restaurant, not a cart, not a stand — just a bench in a Rockford park where someone leaves a cooler full of sandwiches every morning at 7 AM. Vat-protein on synth-bread, individually wrapped, free. The cooler is always clean, the sandwiches are always fresh, and nobody has ever seen who delivers it. Rockford Shelf residents call it The Bench and treat it with a reverence usually reserved for religious sites. Taking more than one sandwich is technically possible and socially unthinkable. The Bench has operated for three years. Nobody knows who funds it.",
  coordinates: { lat: 42.2710, lng: -89.0940 },
  tags: ["place", "street_vendor", "food", "tier_1"],
  related_entities: [],
  frequented_by: ["Rockford Shelf residents", "Morning park visitors", "People who need exactly one sandwich"]
});

writePlace({
  name: "Noodle Bucket",
  aliases: ["The Bucket"],
  description: "A synth-noodle vendor in Hammond, Indiana, operating from a five-gallon bucket. Literally — Chandra Eriksson-Patel cooks noodles in a pot, portions them into repurposed containers, and serves them from a bucket she carries to wherever the crowd is. Factory gate at shift change, transit stop at rush hour, wherever people are hungry and moving. A portion is Φ2. The noodles are good, seasoned with a turmeric-chili oil that stains the container yellow and your fingers for hours afterward. Chandra walks eight kilometers a day carrying the bucket. She says it keeps her healthy.",
  coordinates: { lat: 41.5830, lng: -87.5000 },
  tags: ["place", "street_vendor", "food", "tier_1"],
  related_entities: ["Chandra Eriksson-Patel"],
  frequented_by: ["Hammond factory workers", "Transit commuters", "Turmeric finger people"]
});

writePlace({
  name: "Steam Table",
  aliases: ["The Table"],
  description: "A cafeteria-style operation in a repurposed Elgin auto shop. Four steam trays, each containing a different synth-protein preparation, served over synth-rice or synth-bread. Φ4 for a plate. The food rotates daily and ranges from passable to surprisingly good, depending on what ingredients showed up that morning. Run by a cooperative of five former fast-food workers who pooled their savings, bought the trays secondhand, and decided they could do this better than the chains they used to work for. They were right, but only marginally.",
  coordinates: { lat: 42.0370, lng: -88.2810 },
  tags: ["place", "restaurant", "food", "tier_1"],
  related_entities: [],
  frequented_by: ["Elgin workers", "Families looking for a cheap hot meal", "Former fast-food employees in solidarity"]
});

writePlace({
  name: "Fish Fry Friday (Every Day)",
  aliases: ["Fish Fry", "FFF"],
  description: "A vat-fish fry stand in West Allis, Milwaukee, that started as a Friday tradition and never stopped. Leena Nakamura-Osei runs the fryer seven days a week, selling beer-battered vat-perch with synth-coleslaw and fries for Φ5. The name stays because nobody in West Allis is willing to call it anything else. The batter is the star — Leena uses a recipe from her grandmother's recipe box, adapted for vat-protein, and the crunch is architectural. The coleslaw is incidental. The fries are competent. But the fish — the fish is why people come.",
  coordinates: { lat: 43.0170, lng: -88.0070 },
  tags: ["place", "street_vendor", "food", "tier_1"],
  related_entities: ["Leena Nakamura-Osei"],
  frequented_by: ["West Allis regulars", "Fish fry traditionalists", "Milwaukee comfort food seekers"]
});

writePlace({
  name: "The Ration Station",
  aliases: ["RatStat"],
  description: "A government-subsidized nutrient dispensary in Gary that technically closed in 2195 when the subsidy ended but kept operating because the woman who ran it, Blessing Andersson-Kone, refused to stop. She converted it to a donation-based model, makes do with whatever comes in, and still serves 150 people a day. The rations are basic — paste, bars, occasionally a hot meal — but they come with a dignity that the government version never had. Blessing knows everyone's name, asks about their families, and remembers what they said last time. The food keeps you alive. Blessing keeps you human.",
  coordinates: { lat: 41.6010, lng: -87.3370 },
  tags: ["place", "restaurant", "food", "tier_1"],
  related_entities: ["Blessing Andersson-Kone"],
  frequented_by: ["Gary's poorest residents", "Families", "Former government program recipients"]
});

writePlace({
  name: "Pierogi Corner",
  aliases: ["Pierogi"],
  description: "A pierogi stand on a corner in Slavic Village — which is what people still call the remnants of a Polish-heritage neighborhood in south Milwaukee, even though the demographics shifted three generations ago. Zofia Okafor-Kowalski makes pierogis from synth-dough stuffed with vat-potato and onion, boiled and then pan-fried on a portable griddle. Six for Φ4. They are starchy, oniony, and heavy in the way that food designed to get people through winters is heavy. Zofia is twenty-six and learned the recipe from a neighbor who learned it from a grandmother who probably learned it from another grandmother. The chain of pierogi knowledge stretches back further than anyone tracks.",
  coordinates: { lat: 43.0050, lng: -87.9190 },
  tags: ["place", "street_vendor", "food", "tier_1"],
  related_entities: ["Zofia Okafor-Kowalski"],
  frequented_by: ["Milwaukee south side residents", "Comfort food seekers", "Heritage eaters"]
});

writePlace({
  name: "The Tin Kitchen",
  aliases: ["Tin Kitchen", "TK"],
  description: "A food stall in Aurora built entirely from flattened tin cans, which gives it the appearance of a folk art installation and the structural integrity of a strong opinion. Inside, Kwame Lindgren-Asante makes a rotating menu of one-pot meals: stews, curries, chilis, anything that can be made in a single large vessel and ladled into bowls. Φ4 per bowl. The pot changes daily, and Kwame posts the day's offering on a chalkboard outside. Some days are better than others. All days are honest. The tin walls have been signed and decorated by years of customers, making the stall a kind of inadvertent community mural.",
  coordinates: { lat: 41.7600, lng: -88.3200 },
  tags: ["place", "restaurant", "food", "tier_1"],
  related_entities: ["Kwame Lindgren-Asante"],
  frequented_by: ["Aurora Shelf residents", "One-pot devotees", "Mural contributors"]
});

writePlace({
  name: "Bus Stop Buns",
  aliases: ["Bus Buns"],
  description: "A steamed bun seller who operates exclusively at bus stops along the Route 15 corridor in Racine. Aisha Petersen-Nguyen rides the bus with a portable steamer, sells buns at each stop, and rides to the next one. Synth-pork buns, Φ2 each. She makes about sixty buns per bus trip and does four trips a day. Commuters on the route know her schedule better than the bus schedule, which is unreliable, whereas Aisha is not. The buns are small, dense, and perfectly adequate. Aisha's commitment to the bit is the actual product.",
  coordinates: { lat: 42.7310, lng: -87.7920 },
  tags: ["place", "street_vendor", "food", "tier_1"],
  related_entities: ["Aisha Petersen-Nguyen"],
  frequented_by: ["Route 15 commuters", "Bus riders", "People who admire logistics"]
});

writePlace({
  name: "Calorie Cavern",
  aliases: ["The Cavern"],
  description: "A subterranean food court — two vendor stalls and a communal eating area — in a converted basement in Evanston Shelf. The ceiling is low, the lighting is bad, and the food is cheap. One stall sells synth-noodle soup (Φ3), the other sells vat-protein rice bowls (Φ4). Both are operated by a married couple, Riku and Amara Svensson-Osei, who split the labor without dividing the business. The Cavern seats about twenty people at repurposed school desks, and at peak lunch hour every seat is taken by people eating in the particular focused silence of those who have thirty minutes and no more.",
  coordinates: { lat: 42.0450, lng: -87.6880 },
  tags: ["place", "restaurant", "food", "tier_1"],
  related_entities: ["Riku Svensson-Osei", "Amara Svensson-Osei"],
  frequented_by: ["Evanston Shelf workers", "Lunch crowd", "People who eat efficiently"]
});

writePlace({
  name: "Palm Leaf",
  aliases: ["Palm"],
  description: "A South Indian-inspired street food stall in Rogers Park Shelf serving dosas made from a fermented batter of synth-rice and synth-lentil. The dosas are thin, crispy, and served on actual palm leaf plates that the owner, Devi Lindqvist-Naidu, imports from a hydroponics operation in Indiana. A plain dosa with sambar is Φ3. A masala dosa with vat-potato filling is Φ5. The sambar is the real achievement — a complex, sour-spicy broth that Devi makes fresh each morning and that is better than it has any right to be, given the ingredients available at this price point.",
  coordinates: { lat: 42.0100, lng: -87.6700 },
  tags: ["place", "street_vendor", "food", "tier_1"],
  related_entities: ["Devi Lindqvist-Naidu"],
  frequented_by: ["Rogers Park residents", "South Asian diaspora food seekers", "Dosa enthusiasts"]
});

writePlace({
  name: "Shelf Sausage",
  aliases: ["Sausage Stand"],
  description: "A bratwurst stand in Green Bay that serves vat-pork bratwursts on synth-bread with sauerkraut and mustard. Φ4 each. The operator, Erik Osei-Johansson, is adamant that Green Bay's bratwurst tradition survives regardless of whether the pork is real, and he treats every brat he serves with a seriousness that borders on ceremonial. The sauerkraut is genuine — fermented cabbage, the real thing — which he considers the non-negotiable element. The bread is whatever's cheapest. The brat is vat-grown. But the kraut is real, and on this hill Erik will die.",
  coordinates: { lat: 44.5130, lng: -88.0160 },
  tags: ["place", "street_vendor", "food", "tier_1"],
  related_entities: ["Erik Osei-Johansson"],
  frequented_by: ["Green Bay residents", "Bratwurst traditionalists", "Sauerkraut purists"]
});

writePlace({
  name: "The Chai Line",
  aliases: ["Chai Line"],
  description: "A chai and snack stand in Devon Avenue, Chicago, that sells masala chai for Φ1 and samosas for Φ2. The chai is brewed in a massive pot with real tea leaves, cardamom, ginger, and sweetened condensed synth-milk. The samosas are filled with spiced synth-potato and peas, fried to order. Nasreen Eriksson-Khan runs the stand with her daughter, and together they serve about 200 cups of chai a day. The stand is technically a table with a gas burner on it. The chai is technically the best thing on Devon Avenue. These two facts coexist without contradiction.",
  coordinates: { lat: 41.9980, lng: -87.6730 },
  tags: ["place", "street_vendor", "food", "tier_1"],
  related_entities: ["Nasreen Eriksson-Khan"],
  frequented_by: ["Devon Avenue residents", "Chai addicts", "Morning commuters"]
});

writePlace({
  name: "Belly Full",
  aliases: ["Belly"],
  description: "A community kitchen in Janesville that operates on a simple principle: bring an ingredient, get a meal. Whatever people bring — a can of synth-beans, a packet of rice, a questionable vegetable — goes into the communal pot, and everyone eats from the communal pot. The cook, Blessing Kimura-Asante, has an extraordinary talent for making disparate ingredients cohere into something edible. On good days, when the contributions are generous, the pot produces genuine comfort food. On lean days, it produces warm sustenance. Either way, nobody leaves hungry. Contribution is voluntary. Most people contribute.",
  coordinates: { lat: 42.6830, lng: -89.0200 },
  tags: ["place", "restaurant", "food", "tier_1"],
  related_entities: ["Blessing Kimura-Asante"],
  frequented_by: ["Janesville Shelf residents", "Families", "People who believe in the pot"]
});

// ═══════════════════════════════════════════════════════════════════════════════
// $$ TIER 2-3 — CIRCUIT WORKING CLASS (60 venues)
// Diners, burger joints, taco shops, pubs, bakeries, coffee houses
// ═══════════════════════════════════════════════════════════════════════════════

writePlace({
  name: "Meridian Diner",
  aliases: ["Meridian", "The Diner"],
  description: "A twenty-four-hour diner on South State Street that has been open continuously since 2181 and looks every day of it. The booths are patched with tape, the counter stools spin unevenly, and the coffee is a religion unto itself — Meridian sources actual coffee beans from a Circuit-tier importer and brews them strong enough to dissolve doubt. The menu is seventeen pages long and everything on it costs between Φ8 and Φ15. Breakfast is served all day. The hash browns are legendary, made from real potatoes fried in vat-butter until they shatter. Owned and operated by Essie Petersen-Agyeman, who inherited it from her mother and will pass it to whichever of her three children demonstrates the most commitment to the griddle.",
  coordinates: { lat: 41.8700, lng: -87.6280 },
  tags: ["place", "restaurant", "food", "tier_2"],
  related_entities: ["Essie Petersen-Agyeman"],
  frequented_by: ["Night shift workers", "Runners between jobs", "Anyone who needs coffee at 3 AM", "South State regulars"]
});

writePlace({
  name: "Big Tomas Burger",
  aliases: ["Big Tomas", "BTB"],
  description: "A burger joint in Pilsen that serves vat-beef patties on real brioche buns — the bun is the luxury, not the meat. Tomas Eriksson-Ramirez bakes the buns himself every morning, and the difference between a Big Tomas burger and every other burger in the Circuit is that bun: soft, slightly sweet, golden. The patties are standard vat-grade, but Tomas seasons them with a house blend he won't disclose, and they come off the griddle with a crust that makes you forget the meat isn't real. A single with fries is Φ10. A double is Φ14. There is no triple because Tomas considers it 'architecturally irresponsible.'",
  coordinates: { lat: 41.8560, lng: -87.6630 },
  tags: ["place", "restaurant", "food", "tier_2"],
  related_entities: ["Tomas Eriksson-Ramirez"],
  frequented_by: ["Pilsen Circuit workers", "Burger enthusiasts", "Bun connoisseurs"]
});

writePlace({
  name: "Lucky Jade Noodle House",
  aliases: ["Lucky Jade", "LJ"],
  description: "A noodle house in Chinatown that bridges the gap between Shelf-tier ramen and actual cuisine. The noodles are hand-pulled — owner Wei Lindqvist-Tanaka employs two noodle pullers who work in the window so passersby can watch the dough stretch and fold. The broth is a twenty-four-hour pork-style stock made from vat-bones and aromatics. A bowl of pulled noodles in broth with sliced vat-pork belly is Φ12. The menu has thirty items, ranging from dan dan noodles to cold sesame noodles, each between Φ8 and Φ15. Wei's mother started the shop; Wei expanded it from four seats to twenty-two. The noodle pullers are the marketing department, and they're the best in the corridor.",
  coordinates: { lat: 41.8510, lng: -87.6340 },
  tags: ["place", "restaurant", "food", "tier_2"],
  related_entities: ["Wei Lindqvist-Tanaka"],
  frequented_by: ["Chinatown regulars", "Noodle tourists from the Circuit", "Lunch crowd"]
});

writePlace({
  name: "Cargo Tacos",
  aliases: ["Cargo"],
  description: "A taco shop in a converted shipping container in Wicker Park that's been painted the color of a sunset by someone with strong opinions about orange. Cargo serves tacos on handmade corn tortillas — real masa, pressed and griddled to order — with a selection of vat-meat fillings: al pastor, carnitas, barbacoa, and a mushroom tinga that outsells them all. Tacos are Φ4 each, which is expensive for tacos and cheap for the quality. The horchata is made from real rice milk and cinnamon. The owner, Lucia Nakamura-Vega, considers the tortilla the foundation of civilization and treats it accordingly.",
  coordinates: { lat: 41.9090, lng: -87.6770 },
  tags: ["place", "restaurant", "food", "tier_2"],
  related_entities: ["Lucia Nakamura-Vega"],
  frequented_by: ["Wicker Park Circuit residents", "Taco pilgrims", "Tortilla fundamentalists"]
});

writePlace({
  name: "The Copper Kettle",
  aliases: ["Copper Kettle", "CK"],
  description: "A pub in Milwaukee's Third Ward that serves food good enough to justify the drink prices. The shepherd's pie — vat-lamb in gravy under mashed real potatoes — is Φ14 and is the best version of comfort food available in the northern corridor's Circuit tier. Fish and chips uses vat-cod in a beer batter made with actual beer from the pub's own tap. The owner, Siobhan Osei-Murphy, came from a family of publicans and considers a pub without proper food to be a moral failure. The Copper Kettle has twelve taps, a fireplace that works, and a Wednesday quiz night that has been running for six years.",
  coordinates: { lat: 43.0340, lng: -87.9090 },
  tags: ["place", "pub", "food", "nightlife", "tier_2"],
  related_entities: ["Siobhan Osei-Murphy"],
  frequented_by: ["Third Ward regulars", "Quiz night devotees", "Shepherd's pie enthusiasts"]
});

writePlace({
  name: "Sizzle Pit Pizza",
  aliases: ["Sizzle Pit", "SP"],
  description: "A pizza parlor in Logan Square serving deep-dish pizza with real dough, synth-cheese, and a choice of vat-meat toppings. A small deep-dish is Φ12. A large feeds four people and costs Φ28. The dough is the pride of the operation — fermented for forty-eight hours, which gives it a flavor complexity that synth-dough can't match. The sauce is canned tomatoes, which at Circuit prices means actual tomatoes that were actually canned. Owner Gianni Okafor-Rossi learned deep-dish from a neighbor who learned it from a pizzaiolo who claimed a lineage going back to the original Chicago pizza wars of the twentieth century. Whether this is true matters less than the pizza, which is excellent.",
  coordinates: { lat: 41.9240, lng: -87.6980 },
  tags: ["place", "restaurant", "food", "tier_2"],
  related_entities: ["Gianni Okafor-Rossi"],
  frequented_by: ["Logan Square families", "Deep-dish devotees", "Groups splitting a large pie"]
});

writePlace({
  name: "Curry Circuit",
  aliases: ["CC", "The Curry"],
  description: "A curry house on Devon Avenue serving a menu that spans the Indian subcontinent's diaspora: butter chicken, lamb rogan josh, daal makhani, chana masala — all made with vat-proteins and real spices. The spices are the investment. Owner Arjun Lindqvist-Sharma buys directly from a spice trader who sources from hydroponic operations in the southern agricultural zones, and the difference is immediate: this curry tastes like curry, not like curry-flavored. Entrees range from Φ10 to Φ18. The naan is baked in a tandoor that Arjun built from salvaged brick and operates at temperatures that make the kitchen uninhabitable in summer.",
  coordinates: { lat: 41.9970, lng: -87.6690 },
  tags: ["place", "restaurant", "food", "tier_2"],
  related_entities: ["Arjun Lindqvist-Sharma"],
  frequented_by: ["Devon Avenue residents", "Spice seekers", "Diaspora families looking for familiar flavors"]
});

writePlace({
  name: "Frost Line Ice Cream",
  aliases: ["Frost Line", "FL"],
  description: "An ice cream shop in Evanston that makes its product from vat-cultured cream — actual dairy proteins, lab-grown — frozen with liquid nitrogen for a texture that is smoother than traditional churning can achieve. A scoop is Φ6. The flavors rotate weekly: cardamom rose, black sesame, corn and cayenne, burnt honey. Owner Amara Johansson-Chen is a former food chemist who considers ice cream a serious discipline and will discuss emulsification science with anyone who makes the mistake of asking. The shop seats eight and there's usually a line out the door, which Amara considers a validation of science.",
  coordinates: { lat: 42.0480, lng: -87.6810 },
  tags: ["place", "cafe", "food", "tier_2"],
  related_entities: ["Amara Johansson-Chen"],
  frequented_by: ["Evanston families", "Ice cream tourists", "Anyone curious about this week's flavor"]
});

writePlace({
  name: "The Night Owl Bakery",
  aliases: ["Night Owl", "Owl"],
  description: "A bakery in Ukrainian Village that operates from 8 PM to 6 AM — reverse hours, serving the night shift economy. Bread, rolls, pastries, and a dense chocolate cake that has developed a cult following. Everything is baked with real flour, real sugar, and vat-butter, which puts Night Owl firmly in Circuit territory price-wise: a loaf of bread is Φ8, pastries are Φ4-6. Owner Olesya Okafor-Kovalenko starts baking at 6 PM and the smell hits the street by 7:30 PM, which is the only advertisement she needs. The cake — the cake is Φ15 per slice and worth every quanta.",
  coordinates: { lat: 41.8990, lng: -87.6770 },
  tags: ["place", "cafe", "food", "tier_2"],
  related_entities: ["Olesya Okafor-Kovalenko"],
  frequented_by: ["Night shift workers", "Late-night carb seekers", "Chocolate cake cultists"]
});

writePlace({
  name: "Wings & Wreckage",
  aliases: ["W&R", "Wings"],
  description: "A bar in Bridgeport that serves the best wings in the southern corridor and knows it. The wings are vat-chicken, deep-fried twice for maximum crunch, and sauced in one of eight options ranging from 'Mild' (Φ10/dozen) to 'Structural Failure' (Φ12/dozen, waiver required). The bar itself is a standard Circuit-tier dive — sticky floors, loud music, dartboard with the wrong number of holes in it — but the wings elevate everything. Owner Dayo Eriksson-Adewale was a competitive hot sauce maker before opening the bar, and she considers wings to be a sauce delivery system. She is not wrong.",
  coordinates: { lat: 41.8380, lng: -87.6490 },
  tags: ["place", "bar", "food", "nightlife", "tier_2"],
  related_entities: ["Dayo Eriksson-Adewale"],
  frequented_by: ["Bridgeport regulars", "Wing tourists", "People testing their heat tolerance"]
});

writePlace({
  name: "Lake Effect Coffee",
  aliases: ["Lake Effect", "LE"],
  description: "A coffee house on Brady Street in Milwaukee serving single-origin beans from the last remaining coffee importers in the Great Lakes corridor. A drip coffee is Φ5. An espresso is Φ7. A pour-over is Φ9 and comes with a brief explanation of the bean's origin, roast profile, and brewing parameters, whether you want it or not. The shop is small, warm, and smells like what coffee used to smell like before most people switched to synth-caffeine. Owner Kian Petersen-Abbasi is a former barista competition champion who considers synth-caffeine a personal insult.",
  coordinates: { lat: 43.0520, lng: -87.8920 },
  tags: ["place", "cafe", "food", "tier_2"],
  related_entities: ["Kian Petersen-Abbasi"],
  frequented_by: ["Brady Street regulars", "Coffee purists", "People willing to pay for real beans"]
});

writePlace({
  name: "Holler Ramen",
  aliases: ["Holler"],
  description: "A ramen shop in Bucktown that takes the form seriously: four broths (tonkotsu, miso, shoyu, shio), each simmered for a minimum of eighteen hours, served with fresh noodles made in-house. The pork belly is vat-grown but chashu-prepared — braised for hours in soy and mirin until it melts. A bowl is Φ14-18 depending on toppings. Owner Kenji Lindqvist-Osei trained under a ramen master in a VR recreation of a Tokyo ramen school, which sounds absurd until you taste the broth. The shop seats sixteen and the wait at peak hours is forty-five minutes. Nobody complains because the broth justifies everything.",
  coordinates: { lat: 41.9120, lng: -87.6740 },
  tags: ["place", "restaurant", "food", "tier_2"],
  related_entities: ["Kenji Lindqvist-Osei"],
  frequented_by: ["Bucktown residents", "Ramen obsessives", "People willing to wait"]
});

writePlace({
  name: "Blackstone Pub",
  aliases: ["Blackstone"],
  description: "A proper pub in the old South Loop, serving pints and proper pub food: bangers and mash, fish and chips, steak and kidney pie, all made with vat-proteins but prepared traditionally. A pint of locally brewed ale is Φ7. A plate of bangers and mash is Φ12. The pub is dark, wood-paneled (salvaged, not original), and has the atmosphere of a place that has been doing this for longer than it actually has. Owner Declan Nakamura-Byrne built the atmosphere deliberately — he studied pub design with the intensity of an architect and considers ambiance a structural element.",
  coordinates: { lat: 41.8680, lng: -87.6240 },
  tags: ["place", "pub", "food", "nightlife", "tier_2"],
  related_entities: ["Declan Nakamura-Byrne"],
  frequented_by: ["South Loop workers", "After-work pint crowd", "Pub food traditionalists"]
});

writePlace({
  name: "Sweet Meridian",
  aliases: ["Sweet M"],
  description: "A bakery and coffee shop in Hyde Park that serves pastries, cakes, and sandwiches alongside real-bean coffee. The croissants — made with vat-butter and real flour, laminated by hand — are the signature: Φ6 each and worth it. They shatter when you bite into them and have the kind of buttery interior that reminds you what bakeries used to be. Owner Miriam Gustafsson-Nkrumah is a pastry chef who left a Spire-tier restaurant because she wanted to bake for people who don't already have everything. Her prices are Circuit-tier. Her skills are not.",
  coordinates: { lat: 41.7940, lng: -87.5900 },
  tags: ["place", "cafe", "food", "tier_2"],
  related_entities: ["Miriam Gustafsson-Nkrumah"],
  frequented_by: ["Hyde Park residents", "Pastry devotees", "Students from nearby campuses"]
});

writePlace({
  name: "The Slider Joint",
  aliases: ["Slider Joint", "SJ"],
  description: "A slider bar in Milwaukee's Bay View neighborhood. The sliders are small — three bites each — and come in twelve varieties, from classic vat-beef with onion to a synth-brisket with pickled cabbage that has no business being as good as it is. Three sliders for Φ8. Six for Φ14. The bar serves beer and nothing else, which keeps things simple. Owner Nessa Lindberg-Adebayo considers the slider a perfect food form and will argue this point with the conviction of a philosopher defending first principles.",
  coordinates: { lat: 43.0070, lng: -87.8990 },
  tags: ["place", "bar", "food", "nightlife", "tier_2"],
  related_entities: ["Nessa Lindberg-Adebayo"],
  frequented_by: ["Bay View locals", "Beer-and-slider enthusiasts", "Bar hoppers"]
});

writePlace({
  name: "Stack House Pancakes",
  aliases: ["Stack House"],
  description: "A breakfast spot in Wicker Park specializing in pancakes, waffles, and French toast — all made with real flour and vat-eggs. A short stack is Φ8. A full stack is Φ12. The waffles have a crispness that comes from a proprietary iron that the owner, Blessing Eriksson-Kowalski, designed and had fabricated by a metalworker in the corridor. The maple syrup is synthetic but Blessing adds vanilla and a pinch of salt that makes it taste real enough. Open 6 AM to 2 PM, because Blessing believes breakfast has a natural endpoint and brunch is a lie.",
  coordinates: { lat: 41.9100, lng: -87.6810 },
  tags: ["place", "restaurant", "food", "tier_2"],
  related_entities: ["Blessing Eriksson-Kowalski"],
  frequented_by: ["Wicker Park morning crowd", "Pancake enthusiasts", "People who agree brunch is a lie"]
});

writePlace({
  name: "Onda Taqueria",
  aliases: ["Onda"],
  description: "A sit-down taqueria in Pilsen that feels like the inside of someone's home, because it was — owner Marisol Svensson-Gutierrez converted her living room into a twelve-seat restaurant and hasn't looked back. The menu is short: tacos, burritos, quesadillas, and a pozole that appears on weekends and disappears before noon. Everything uses real masa, real spices, and vat-proteins seasoned with the authority of someone who learned to cook in a family kitchen, not a culinary school. Tacos are Φ5 each. The pozole is Φ15 and serves two.",
  coordinates: { lat: 41.8540, lng: -87.6620 },
  tags: ["place", "restaurant", "food", "tier_2"],
  related_entities: ["Marisol Svensson-Gutierrez"],
  frequented_by: ["Pilsen families", "Pozole seekers on weekends", "People who eat in someone's living room"]
});

writePlace({
  name: "The Draft Board",
  aliases: ["Draft Board", "DB"],
  description: "A beer bar in Green Bay that serves thirty-two taps of Great Lakes corridor microbrews and a food menu built around what pairs well with beer: pretzels with cheese sauce, smoked vat-sausages, loaded fries, and a vat-beef chili that regulars order by the quart. Pints are Φ6-9. Food ranges from Φ6-14. The owner, Pekka Okafor-Virtanen, is a former brewmaster who transitioned to curation — he doesn't make beer anymore, he selects it, and his palate is the bar's competitive advantage. Every tap tells a story that Pekka will share at length.",
  coordinates: { lat: 44.5130, lng: -88.0180 },
  tags: ["place", "bar", "food", "nightlife", "tier_2"],
  related_entities: ["Pekka Okafor-Virtanen"],
  frequented_by: ["Green Bay beer enthusiasts", "Tap tourists", "People who like stories with their pints"]
});

writePlace({
  name: "Momo House",
  aliases: ["Momo"],
  description: "A Tibetan-Nepali dumpling house on Broadway in Uptown serving handmade momos — steamed, fried, or in soup — stuffed with vat-chicken, vat-pork, or a vegetable blend. Eight momos for Φ10. The dipping sauce — a fiery tomato-sesame chutney — is made fresh daily and is the kind of condiment people try to buy by the jar. Owner Tenzin Lindqvist-Gurung makes the dough and filling himself, wrapping each momo by hand with a speed that suggests muscle memory measured in decades. The restaurant seats fourteen and plays Nepali radio at a volume that encourages eating over lingering.",
  coordinates: { lat: 41.9670, lng: -87.6560 },
  tags: ["place", "restaurant", "food", "tier_2"],
  related_entities: ["Tenzin Lindqvist-Gurung"],
  frequented_by: ["Uptown residents", "Dumpling seekers", "Chutney jar hopefuls"]
});

writePlace({
  name: "The Brick Oven",
  aliases: ["Brick Oven"],
  description: "A pizza place in Kenosha with an actual wood-fired brick oven, which is rare enough at this price point to be notable. The oven was built by hand by owner Alessia Okafor-De Luca, who spent a year on the project and sources real wood from corridor salvage operations. The pizza is Neapolitan-style: thin, charred, real dough, synth-mozzarella, canned San Marzano-style tomatoes. A margherita is Φ12. Toppings are Φ2-4 each. The oven runs at 450 degrees Celsius and cooks a pizza in ninety seconds, which Alessia considers the only correct way to make pizza. She will not debate this.",
  coordinates: { lat: 42.5850, lng: -87.8210 },
  tags: ["place", "restaurant", "food", "tier_2"],
  related_entities: ["Alessia Okafor-De Luca"],
  frequented_by: ["Kenosha residents", "Pizza pilgrims", "Wood-fire purists"]
});

writePlace({
  name: "Two Rivers Diner",
  aliases: ["Two Rivers", "TRD"],
  description: "A diner in Two Rivers, Wisconsin, that claims — with some historical justification — to be the birthplace of the ice cream sundae. The diner itself is modern, but the ice cream sundae commitment is genuine: twelve sundae varieties, each using vat-cream ice cream with real toppings. A sundae is Φ8-12. The diner also serves standard diner fare — burgers, fries, breakfast plates — at Φ8-14. Owner Yuki Hassan-Schmidt maintains the sundae legacy with the solemnity of a museum curator. The hot fudge is made from real cocoa and vat-cream, and it is very, very good.",
  coordinates: { lat: 44.1530, lng: -87.5690 },
  tags: ["place", "restaurant", "food", "tier_2"],
  related_entities: ["Yuki Hassan-Schmidt"],
  frequented_by: ["Two Rivers residents", "Ice cream historians", "Diner regulars"]
});

writePlace({
  name: "Pho King Good",
  aliases: ["Pho King", "PKG"],
  description: "A pho restaurant in Argyle Street's Little Saigon that has been operating under this name for seventeen years with zero self-consciousness. The pho is, in fairness, very good: a twelve-hour bone broth made from vat-beef bones, star anise, cinnamon, and charred ginger, served with rice noodles and thin-sliced vat-beef. A large bowl is Φ12. The condiment tray — bean sprouts, basil, lime, hoisin, sriracha — is real produce, which at this tier means someone is growing it intentionally. Owner Linh Petersen-Tran considers the name a conversation starter and the pho a conversation ender.",
  coordinates: { lat: 41.9720, lng: -87.6570 },
  tags: ["place", "restaurant", "food", "tier_2"],
  related_entities: ["Linh Petersen-Tran"],
  frequented_by: ["Argyle Street regulars", "Pho enthusiasts", "People with opinions about the name"]
});

writePlace({
  name: "Backyard BBQ",
  aliases: ["Backyard", "BBQ"],
  description: "A barbecue joint in a literal backyard in South Milwaukee — the owner, Darnell Lindqvist-Washington, built a pit smoker in his yard, started selling plates over the fence, and eventually knocked down the fence entirely. Now it's a twenty-seat outdoor restaurant that operates weather permitting, which in Milwaukee means about seven months a year. Vat-brisket smoked for fourteen hours, pulled vat-pork, and ribs that are the closest thing to real barbecue most Circuit-tier residents will ever taste. A two-meat plate with sides is Φ16. The sides — cornbread, coleslaw, baked beans — are all made from scratch.",
  coordinates: { lat: 42.9110, lng: -87.8600 },
  tags: ["place", "restaurant", "food", "tier_2"],
  related_entities: ["Darnell Lindqvist-Washington"],
  frequented_by: ["South Milwaukee residents", "BBQ pilgrims", "Fair-weather diners"]
});

writePlace({
  name: "Griddle & Grace",
  aliases: ["G&G"],
  description: "A breakfast and brunch spot in Oak Park serving elevated comfort food: eggs Benedict with vat-ham on real English muffins (Φ14), avocado toast with actual avocado (Φ12, when available), and a shakshuka that owner Nadia Eriksson-Hassan learned from her grandmother and refuses to simplify. The coffee is real. The orange juice is synth but honest about it. Griddle & Grace is the kind of place where Circuit-tier families go on weekends to pretend, for an hour, that the world is normal and Sunday brunch is a thing people still do.",
  coordinates: { lat: 41.8850, lng: -87.7910 },
  tags: ["place", "restaurant", "food", "tier_2"],
  related_entities: ["Nadia Eriksson-Hassan"],
  frequented_by: ["Oak Park families", "Weekend brunch crowd", "People performing normalcy"]
});

writePlace({
  name: "The Wok Box",
  aliases: ["Wok Box"],
  description: "A stir-fry restaurant in Schaumburg where you pick your protein, vegetables, and sauce, and the cook woks it in front of you over a burner hot enough to light paper. Vat-chicken, vat-beef, vat-shrimp, or tofu, with real vegetables when available, synth when not. A box is Φ10-14. The wok hei — that smoky, charred flavor from cooking over high heat — is the whole point. Owner Chen Gustafsson-Li is a former industrial welder who applies the same relationship to flame in both careers.",
  coordinates: { lat: 42.0310, lng: -88.0840 },
  tags: ["place", "restaurant", "food", "tier_2"],
  related_entities: ["Chen Gustafsson-Li"],
  frequented_by: ["Schaumburg office workers", "Wok hei chasers", "The customization-minded"]
});

writePlace({
  name: "Dough & Deep",
  aliases: ["D&D"],
  description: "A donut shop in Lakeview that fries donuts to order from scratch dough — not from pre-made rounds, not from synth-dough, but from a yeasted dough that rises in the kitchen and gets cut, fried, and glazed while you watch. A classic glazed is Φ5. Filled donuts are Φ7. The crullers are Φ6 and have a cult following. Owner Patience Osei-Nakamura worked pastry in a Spire-tier hotel before deciding she'd rather make donuts for people who appreciate them than garnishes for people who don't.",
  coordinates: { lat: 41.9420, lng: -87.6530 },
  tags: ["place", "cafe", "food", "tier_2"],
  related_entities: ["Patience Osei-Nakamura"],
  frequented_by: ["Lakeview residents", "Donut enthusiasts", "The cruller cult"]
});

writePlace({
  name: "Smoke & Spoke",
  aliases: ["Smoke Spoke"],
  description: "A barbecue and beer bar in Waukesha built in a former bicycle repair shop — the spokes in the name refer to the bicycle wheels that still decorate the walls. The smoker is out back and runs twenty-four hours. Vat-brisket, pulled vat-pork, smoked vat-chicken, all with a selection of house-made sauces ranging from sweet to punishing. A plate with two sides is Φ14. The beer list is twenty taps deep, curated to pair with smoke. Owner Felix Okafor-Johansson was a competitive barbecue pitmaster and treats every plate like it's going to be judged.",
  coordinates: { lat: 43.0110, lng: -88.2330 },
  tags: ["place", "bar", "food", "nightlife", "tier_2"],
  related_entities: ["Felix Okafor-Johansson"],
  frequented_by: ["Waukesha locals", "BBQ-and-beer seekers", "Former bicycle customers who were confused the first time"]
});

writePlace({
  name: "Amore Pizza",
  aliases: ["Amore"],
  description: "A thin-crust pizza joint in the West Loop that serves what its owner, Rosa Lindberg-Ferraro, calls 'pizza by weight' — you point at a slab, they cut it, they weigh it, you pay by the gram. A generous slice runs Φ6-8. The pizza is Roman-style: rectangular, crispy-bottomed, topped generously. The toppings rotate daily and lean toward creative: vat-prosciutto with fig paste, synth-gorgonzola with honey, potato and rosemary. Rosa considers this an honest system — you pay for what you eat, nothing more — and the regulars agree.",
  coordinates: { lat: 41.8840, lng: -87.6440 },
  tags: ["place", "restaurant", "food", "tier_2"],
  related_entities: ["Rosa Lindberg-Ferraro"],
  frequented_by: ["West Loop lunch crowd", "Pizza-by-weight converts", "Topping adventurers"]
});

writePlace({
  name: "The Velvet Bean",
  aliases: ["Velvet Bean", "VB"],
  description: "A coffee house in Andersonville that doubles as a meeting space, co-working spot, and occasional poetry venue. The coffee is real-bean, the pastries are sourced from Night Owl Bakery, and the atmosphere is deliberately warm — exposed brick, soft lighting, mismatched furniture that invites you to stay. A coffee is Φ5-8. Pastries are Φ4-7. Owner Sage Nakamura-Osei considers the space more important than the product, which is not to say the product isn't good — it is — but the Velvet Bean exists to give people a place to be, and the coffee is the cover charge.",
  coordinates: { lat: 41.9800, lng: -87.6700 },
  tags: ["place", "cafe", "food", "tier_2"],
  related_entities: ["Sage Nakamura-Osei", "The Night Owl Bakery"],
  frequented_by: ["Andersonville residents", "Remote workers", "Poets", "People who need a place to be"]
});

writePlace({
  name: "Belly & Brisket",
  aliases: ["B&B"],
  description: "A sandwich shop in Lincoln Square specializing in smoked vat-meat sandwiches. The brisket is smoked for sixteen hours. The pastrami is cured for a week and smoked for six hours. The pulled pork is cooked low and slow until it surrenders. Each sandwich comes on bread baked that morning, piled high, and served with a pickle that Kwesi Okafor-Rosenberg makes in-house from real cucumbers. Sandwiches are Φ12-16. The portions are aggressive. Owner Kwesi considers a thin sandwich an insult to the bread.",
  coordinates: { lat: 41.9680, lng: -87.6890 },
  tags: ["place", "restaurant", "food", "tier_2"],
  related_entities: ["Kwesi Okafor-Rosenberg"],
  frequented_by: ["Lincoln Square workers", "Sandwich devotees", "People with large appetites"]
});

writePlace({
  name: "The Grateful Griddle",
  aliases: ["Grateful Griddle", "GG"],
  description: "A twenty-four-hour breakfast joint in downtown Milwaukee that serves the late-night crowd with the same quality it serves the morning crowd. Pancakes, eggs, hash browns, omelets — all made from a mix of real and synth ingredients, with the real stuff clearly marked on the menu for those who care. A full breakfast plate is Φ10-14. The coffee is bottomless and costs Φ4, which makes it the cheapest real coffee in downtown Milwaukee. Owner Efia Lindqvist-Petersen sleeps in four-hour shifts and considers eight hours of sleep a myth perpetuated by people who don't run diners.",
  coordinates: { lat: 43.0380, lng: -87.9060 },
  tags: ["place", "restaurant", "food", "tier_2"],
  related_entities: ["Efia Lindqvist-Petersen"],
  frequented_by: ["Milwaukee night owls", "Morning commuters", "Third-shift workers", "Anyone at 3 AM"]
});

writePlace({
  name: "Ember & Iron",
  aliases: ["Ember"],
  description: "A charcoal grill restaurant in Naperville serving burgers, steaks, and grilled vegetables over real charcoal in an open kitchen. The grill is the centerpiece — diners can watch their food cook, hear the sizzle, smell the char. A burger is Φ12. A vat-ribeye is Φ18. The vegetables — grilled corn, peppers, mushrooms — are sourced from a hydroponic cooperative in the western suburbs and are the menu's quiet star. Owner Obioma Svensson-Nakamura believes fire makes everything better and has yet to encounter evidence to the contrary.",
  coordinates: { lat: 41.7710, lng: -88.1480 },
  tags: ["place", "restaurant", "food", "tier_2"],
  related_entities: ["Obioma Svensson-Nakamura"],
  frequented_by: ["Naperville families", "Grill watchers", "Vegetable converts"]
});

writePlace({
  name: "The Rye & Reason",
  aliases: ["Rye & Reason", "R&R"],
  description: "A whiskey bar in Wrigleyville that serves serious cocktails and bar snacks elevated just enough to justify the drink prices. The cocktail list is short and precise — eight drinks, each a variation on whiskey, each Φ10-14. The food: crispy vat-pork belly bites, duck-fat fries (vat-duck fat, but the process is the same), and a grilled cheese made with three synth-cheeses and caramelized onions that regulars order as a main course. Owner Nia Gustafsson-Brennan was a bartender for fifteen years and considers a poorly made Old Fashioned a personal offense.",
  coordinates: { lat: 41.9470, lng: -87.6570 },
  tags: ["place", "bar", "food", "nightlife", "tier_2"],
  related_entities: ["Nia Gustafsson-Brennan"],
  frequented_by: ["Wrigleyville after-work crowd", "Whiskey enthusiasts", "Grilled cheese converts"]
});

writePlace({
  name: "Kimchi Haus",
  aliases: ["KH"],
  description: "A Korean restaurant in Lincoln Park serving bibimbap, bulgogi, japchae, and a kimchi jjigae that has been developing flavor in the same pot for four years. The kimchi is house-fermented — a process that owner Soo-Jin Eriksson-Park considers the restaurant's heartbeat. All proteins are vat-grown but marinated and prepared traditionally. Bibimbap in a hot stone bowl is Φ14. The banchan — small side dishes — come free and include that four-year kimchi, which is complex, funky, and deeply sour in a way that only time produces. You don't ferment for four years for the Circuit. You ferment for four years because you mean it.",
  coordinates: { lat: 41.9240, lng: -87.6510 },
  tags: ["place", "restaurant", "food", "tier_2"],
  related_entities: ["Soo-Jin Eriksson-Park"],
  frequented_by: ["Lincoln Park residents", "Korean food enthusiasts", "Fermentation devotees"]
});

writePlace({
  name: "The Hearthstone",
  aliases: ["Hearthstone"],
  description: "A gastropub in Green Bay's downtown serving elevated pub food alongside a curated beer and cider selection. The hearth is literal — a stone fireplace in the center of the dining room that radiates warmth and serves as the restaurant's anchor. The menu: braised vat-short ribs (Φ16), a mushroom and gruyere tart (Φ14), fish and chips with remoulade (Φ13), and a bread pudding dessert that uses day-old bread from a local bakery. Owner Kaya Lindberg-Virtanen designed the restaurant around the fireplace and considers it the only indispensable element.",
  coordinates: { lat: 44.5140, lng: -88.0100 },
  tags: ["place", "pub", "food", "nightlife", "tier_3"],
  related_entities: ["Kaya Lindberg-Virtanen"],
  frequented_by: ["Green Bay's Circuit professionals", "Date night couples", "People drawn to fire"]
});

writePlace({
  name: "Lakefront Creamery",
  aliases: ["Lakefront", "LC"],
  description: "An ice cream and frozen custard shop on Milwaukee's lakefront that serves frozen custard the old-fashioned Wisconsin way — dense, rich, and made fresh in small batches throughout the day. The custard uses vat-cream and real egg yolks, and the texture is denser and silkier than ice cream. A dish is Φ7. A concrete mixer — custard blended with mix-ins — is Φ9. Owner Nkechi Johansson-Schmidt considers frozen custard a regional art form and refuses to call it ice cream, which she regards as a lesser medium.",
  coordinates: { lat: 43.0500, lng: -87.8880 },
  tags: ["place", "cafe", "food", "tier_2"],
  related_entities: ["Nkechi Johansson-Schmidt"],
  frequented_by: ["Milwaukee lakefront visitors", "Custard purists", "Summer crowd"]
});

writePlace({
  name: "The Bao Stop",
  aliases: ["Bao Stop"],
  description: "A bao bun restaurant in Irving Park serving steamed, baked, and fried bao with fillings that span the diaspora: classic vat-pork belly, Indian-spiced vat-lamb, jerk-seasoned vat-chicken, and a dessert bao filled with red bean paste. Three bao for Φ8. The buns are made from a dough recipe that owner Mei Lindqvist-Owusu spent two years perfecting — pillowy, slightly sweet, stretchy in the way that only properly fermented dough can be. The shop is tiny — six seats at a counter — and the steam from the bao baskets fogs the windows permanently.",
  coordinates: { lat: 41.9530, lng: -87.7260 },
  tags: ["place", "restaurant", "food", "tier_2"],
  related_entities: ["Mei Lindqvist-Owusu"],
  frequented_by: ["Irving Park residents", "Bao enthusiasts", "Diaspora flavor chasers"]
});

writePlace({
  name: "Third Rail Coffee",
  aliases: ["Third Rail"],
  description: "A coffee shop inside a decommissioned CTA station in Ravenswood. The platform is the seating area. The ticket booth is the bar. The old tracks run through the middle of the space, painted over and used as a shelf for potted plants. Coffee is Φ5-9, real beans, expertly prepared. The pastry case is stocked by three local bakers on a rotating schedule. Owner Zara Okafor-Lindgren chose the location because the acoustics of a train station make every conversation feel important, and because the rent was zero — she simply moved in and nobody stopped her.",
  coordinates: { lat: 41.9750, lng: -87.6740 },
  tags: ["place", "cafe", "food", "tier_2"],
  related_entities: ["Zara Okafor-Lindgren"],
  frequented_by: ["Ravenswood residents", "Coffee tourists", "People who like train station acoustics"]
});

writePlace({
  name: "Mama Kofi's Jollof",
  aliases: ["Mama Kofi's"],
  description: "A West African restaurant in Rogers Park serving jollof rice, waakye, kelewele, and grilled tilapia — the tilapia is vat-grown but prepared with the same spice rubs and grilling technique used for real fish. Mama Kofi Svensson-Asante is seventy-two and has been cooking commercially since she was nineteen. A plate of jollof rice with grilled tilapia is Φ14. The jollof is tomato-red, fragrant with onion and scotch bonnet, and cooked in a pot big enough to serve forty people. The debate about whether her jollof is Ghanaian-style or Nigerian-style has been running for twelve years. Mama Kofi says it's her style.",
  coordinates: { lat: 42.0090, lng: -87.6690 },
  tags: ["place", "restaurant", "food", "tier_2"],
  related_entities: ["Mama Kofi Svensson-Asante"],
  frequented_by: ["Rogers Park residents", "West African diaspora families", "Jollof debate participants"]
});

writePlace({
  name: "Milwaukee Pie Company",
  aliases: ["MPC", "Pie Company"],
  description: "A pie shop in Milwaukee's Walker's Point selling sweet and savory pies from a glass case that displays them like jewelry. Savory pies — vat-chicken pot pie, steak and mushroom, spinach and feta — are Φ10-12 for a personal size. Sweet pies — apple (real apples), cherry (synth), key lime (synth but good) — are Φ7 a slice. Owner Isaac Okafor-Lindqvist bakes everything from scratch with real flour and vat-butter, and the crust is the quiet masterpiece: flaky, golden, structural enough to hold its filling but fragile enough to melt on contact.",
  coordinates: { lat: 43.0260, lng: -87.9100 },
  tags: ["place", "cafe", "food", "tier_2"],
  related_entities: ["Isaac Okafor-Lindqvist"],
  frequented_by: ["Walker's Point residents", "Pie enthusiasts", "Anyone who can see the glass case"]
});

writePlace({
  name: "Night Market Noodles",
  aliases: ["Night Market", "NMN"],
  description: "A late-night noodle shop in Chinatown that opens at 9 PM and closes at 4 AM, serving the corridor's nocturnal economy. The menu is Southeast Asian street food: pad thai, char kway teow, mee goreng, and a laksa that is aggressively spicy and built for 2 AM consumption. Dishes are Φ10-14. The wok station runs at full heat all night and the kitchen is louder than the dining room. Owner Rani Gustafsson-Tan spent years cooking at night markets in Singapore VR simulations before opening a real one. The food is better than the simulation, which she considers the point.",
  coordinates: { lat: 41.8530, lng: -87.6310 },
  tags: ["place", "restaurant", "food", "nightlife", "tier_2"],
  related_entities: ["Rani Gustafsson-Tan"],
  frequented_by: ["Night shift workers", "Late-night diners", "The nocturnal economy"]
});

writePlace({
  name: "Honest Burger",
  aliases: ["Honest"],
  description: "A burger bar in downtown Racine serving what it calls 'honest burgers' — no pretense, no gimmicks, just well-made vat-beef patties on well-made buns with well-made condiments. A burger is Φ10. Fries are Φ4. A milkshake — vat-cream, real vanilla — is Φ7. The honesty extends to the menu, which lists the exact origin and grade of every ingredient. Owner Kweku Petersen-Johansson believes transparency is a flavor enhancer and that people eat better when they know what they're eating. The restaurant seats thirty and is full every lunch hour.",
  coordinates: { lat: 42.7270, lng: -87.7850 },
  tags: ["place", "restaurant", "food", "tier_2"],
  related_entities: ["Kweku Petersen-Johansson"],
  frequented_by: ["Racine lunch crowd", "Transparency enthusiasts", "Milkshake seekers"]
});

writePlace({
  name: "Szechuan Thunder",
  aliases: ["Thunder"],
  description: "A Szechuan restaurant in Bridgeport that takes the 'ma la' — numbing and spicy — philosophy to its logical conclusion. The signature dish is a chili oil fish stew (Φ16) served in a bowl the size of a hubcap, swimming in enough dried chilis and Szechuan peppercorns to make your lips go numb for an hour. The mapo tofu (Φ12) uses vat-pork mince and enough doubanjiang to make you reconsider your heat tolerance. Owner Liu Eriksson-Wei is uncompromising about spice levels and considers mild orders a philosophical disappointment. There is no mild option. The lowest setting is 'moderate,' which is not.",
  coordinates: { lat: 41.8410, lng: -87.6520 },
  tags: ["place", "restaurant", "food", "tier_2"],
  related_entities: ["Liu Eriksson-Wei"],
  frequented_by: ["Spice masochists", "Bridgeport residents", "People who enjoy numb lips"]
});

writePlace({
  name: "The Craft Table",
  aliases: ["Craft Table"],
  description: "A craft beer and food hall in Appleton featuring four kitchen stalls and a central bar with twenty taps. The stalls rotate operators monthly — any Circuit-tier cook can apply for a month-long residency — creating a constantly changing food landscape. This month's lineup: a vat-brisket stand, a dumpling window, a crepe station, and a salad bar using hydroponically grown greens. Beer is Φ6-9. Food is Φ8-15 depending on the stall. Owner Linnea Osei-Strand considers the rotation model a way to give cooks a platform without the capital requirements of a full restaurant.",
  coordinates: { lat: 44.2630, lng: -88.4110 },
  tags: ["place", "bar", "food", "nightlife", "tier_2"],
  related_entities: ["Linnea Osei-Strand"],
  frequented_by: ["Appleton residents", "Rotating food stall regulars", "Beer enthusiasts"]
});

writePlace({
  name: "Greta's Wurst",
  aliases: ["Greta's", "Wurst"],
  description: "A German-style sausage house in Sheboygan — the bratwurst capital of Wisconsin, a title the city has maintained through every technological and social upheaval since the twentieth century. Greta Okafor-Schmidt makes bratwurst from vat-pork using traditional casings and a spice blend documented in a family recipe book that has been passed through four generations. A brat on a hard roll with sauerkraut and mustard is Φ8. A double brat is Φ14. The beer list is exclusively German-style lagers brewed in the Great Lakes corridor. Greta considers bratwurst a cultural obligation, not a menu item.",
  coordinates: { lat: 43.7510, lng: -87.7130 },
  tags: ["place", "restaurant", "food", "tier_2"],
  related_entities: ["Greta Okafor-Schmidt"],
  frequented_by: ["Sheboygan residents", "Bratwurst traditionalists", "Beer-and-brat enthusiasts"]
});

writePlace({
  name: "The Empanada Stand",
  aliases: ["Empanada Stand"],
  description: "A takeout window on 18th Street in Pilsen selling empanadas in twelve varieties, each crimped differently so you can identify the filling by shape. Vat-beef, vat-chicken, cheese, spinach, corn, and six rotating seasonal options. Two empanadas for Φ6. The dough is made with real flour and lard — actual animal fat, which at Circuit prices means someone is paying a premium for authenticity. Owner Valentina Lindqvist-Morales considers the crimp an art form and employs three people whose only job is crimping. Each has a signature style.",
  coordinates: { lat: 41.8580, lng: -87.6600 },
  tags: ["place", "street_vendor", "food", "tier_2"],
  related_entities: ["Valentina Lindqvist-Morales"],
  frequented_by: ["Pilsen residents", "Empanada enthusiasts", "Crimp pattern collectors"]
});

writePlace({
  name: "Midnight Gyros",
  aliases: ["Midnight"],
  description: "A gyro shop in Greektown that operates from 8 PM to 5 AM, serving the drunk, the tired, the nocturnal, and anyone else who needs a vat-lamb gyro wrapped in warm pita at an hour when most restaurants are closed. A gyro is Φ8. A plate with fries and salad is Φ12. The tzatziki is made from real yogurt and cucumber. The lamb is vat-grown but seasoned and cooked on a vertical rotisserie that gives it the proper texture — crispy edges, tender interior. Owner Nikolaos Okafor-Papadopoulos sleeps during the day and considers the night shift the only honest shift.",
  coordinates: { lat: 41.8780, lng: -87.6470 },
  tags: ["place", "restaurant", "food", "nightlife", "tier_2"],
  related_entities: ["Nikolaos Okafor-Papadopoulos"],
  frequented_by: ["Greektown night owls", "Post-bar crowd", "Night shift workers"]
});

writePlace({
  name: "The Bean Counter",
  aliases: ["Bean Counter"],
  description: "A coffee and sandwich shop in Oshkosh that combines meticulous coffee preparation with aggressively practical sandwiches. The coffee is real-bean, prepared with the precision of a chemistry lab. The sandwiches are vat-meat, real cheese (when available), and fresh bread from a local bakery. Coffee is Φ5-8. Sandwiches are Φ8-12. Owner Abena Svensson-Fischer counts everything — beans per cup, grams per portion, seconds per brew — and has spreadsheets tracking the optimal preparation of every item on the menu. The food is excellent. The process is exhausting to watch.",
  coordinates: { lat: 44.0250, lng: -88.5430 },
  tags: ["place", "cafe", "food", "tier_2"],
  related_entities: ["Abena Svensson-Fischer"],
  frequented_by: ["Oshkosh office workers", "Precision coffee seekers", "Spreadsheet enthusiasts"]
});

writePlace({
  name: "Harbor Fish Shack",
  aliases: ["Fish Shack", "Harbor"],
  description: "A fried fish restaurant on the Waukegan harbor serving beer-battered vat-perch, vat-walleye, and vat-catfish with fries, coleslaw, and tartar sauce. A fish plate is Φ12. A fish sandwich is Φ8. The shack is literally a shack — corrugated metal walls, picnic table seating, a view of the harbor that makes up for the lack of decor. Owner Blessing Lindqvist-Hansen loves fish, loves frying, and considers the view a free amenity that no restaurant designer could improve upon. On summer evenings, the shack serves until the oil runs out.",
  coordinates: { lat: 42.3630, lng: -87.8280 },
  tags: ["place", "restaurant", "food", "tier_2"],
  related_entities: ["Blessing Lindqvist-Hansen"],
  frequented_by: ["Waukegan harbor visitors", "Fish fry devotees", "Summer evening diners"]
});

writePlace({
  name: "Sumo Bowl",
  aliases: ["Sumo"],
  description: "A rice bowl restaurant in the Loop serving oversized grain bowls topped with teriyaki vat-chicken, marinated vat-beef, or glazed vat-salmon, plus a generous heap of pickled vegetables, edamame, and a drizzle of spicy mayo. A regular bowl is Φ12. A sumo bowl — double protein, double rice — is Φ18. Owner Haruto Lindqvist-Osei designed the menu for the lunch rush: fast, filling, customizable, and portable. The line moves quickly because Haruto runs the kitchen like a production line, and the bowls arrive within three minutes of ordering.",
  coordinates: { lat: 41.8820, lng: -87.6290 },
  tags: ["place", "restaurant", "food", "tier_2"],
  related_entities: ["Haruto Lindqvist-Osei"],
  frequented_by: ["Loop office workers", "Lunch rush crowd", "People who need volume"]
});

writePlace({
  name: "Blue Door Tavern",
  aliases: ["Blue Door"],
  description: "A neighborhood tavern in Milwaukee's Riverwest with a blue door that is the only exterior signage. Inside: fourteen bar stools, eight tables, a jukebox that plays actual records, and a kitchen that serves burgers, cheese curds, and a Friday fish fry that draws people from three neighborhoods. Burgers are Φ10. Cheese curds — vat-cheese, beer-battered, deep-fried — are Φ7 and the best in Riverwest by consensus. Beer is Φ5-7. Owner Adwoa Petersen-Mueller runs the Blue Door with the philosophy that a tavern should feel like someone's living room, except the living room serves beer and has better food.",
  coordinates: { lat: 43.0620, lng: -87.8960 },
  tags: ["place", "pub", "food", "nightlife", "tier_2"],
  related_entities: ["Adwoa Petersen-Mueller"],
  frequented_by: ["Riverwest regulars", "Cheese curd enthusiasts", "Friday fish fry crowd"]
});

writePlace({
  name: "Pita Palace",
  aliases: ["Pita"],
  description: "A Mediterranean restaurant on Kedzie Avenue serving shawarma, falafel, hummus plates, and the best pita bread in the corridor — baked in a tandoor until it puffs like a balloon and served so hot it steams when you tear it. A shawarma plate with rice and salad is Φ12. Falafel wrap is Φ8. The hummus is made from real chickpeas, which at Circuit prices represents a commitment to authenticity that owner Yasmin Lindqvist-Haddad considers non-negotiable. She also makes a garlic sauce so pungent it should come with a social advisory, and it is perfect.",
  coordinates: { lat: 41.9170, lng: -87.7090 },
  tags: ["place", "restaurant", "food", "tier_2"],
  related_entities: ["Yasmin Lindqvist-Haddad"],
  frequented_by: ["Kedzie Avenue residents", "Shawarma seekers", "Garlic sauce devotees"]
});

writePlace({
  name: "Iron Kettle Chili",
  aliases: ["Iron Kettle"],
  description: "A chili restaurant in Joliet that serves nothing but chili in five heat levels and with a variety of toppings. The base chili — vat-beef, beans, tomatoes, and a spice blend that owner Obi Svensson-Nakamura has been perfecting for eleven years — is Φ8 for a bowl. Toppings (cheese, sour cream, onions, jalapenos, cornbread crumble) are Φ1-2 each. The chili is thick enough to stand a spoon in and has a depth of flavor that comes from slow-cooking for eight hours minimum. Obi makes one batch a day and when it's gone, it's gone. This is usually around 3 PM.",
  coordinates: { lat: 41.5260, lng: -88.0820 },
  tags: ["place", "restaurant", "food", "tier_2"],
  related_entities: ["Obi Svensson-Nakamura"],
  frequented_by: ["Joliet workers", "Chili connoisseurs", "People who arrive before 3 PM"]
});

writePlace({
  name: "The Waffle Iron",
  aliases: ["Waffle Iron"],
  description: "A waffle shop in Evanston serving Belgian-style waffles — thick, deep-pocketed, crispy on the outside, soft inside — topped with everything from fresh fruit to fried vat-chicken. A classic waffle with synth-maple syrup is Φ8. A chicken-and-waffle plate is Φ14. The batter uses real flour, vat-eggs, and vat-butter, and is made in small batches to maintain consistency. Owner Ama Eriksson-Laurent trained as a patissier and considers the waffle an underappreciated architectural form — a structure designed to hold toppings, which makes it, in her view, edible infrastructure.",
  coordinates: { lat: 42.0460, lng: -87.6830 },
  tags: ["place", "restaurant", "food", "tier_2"],
  related_entities: ["Ama Eriksson-Laurent"],
  frequented_by: ["Evanston families", "Waffle enthusiasts", "Brunch crowd"]
});

writePlace({
  name: "Noodle Bridge",
  aliases: ["The Bridge"],
  description: "A Vietnamese noodle shop in an Elgin strip mall serving pho, bun bo hue, and banh mi sandwiches. The pho broth is a sixteen-hour stock that fills the strip mall with the smell of star anise and cinnamon. A large pho is Φ12. A banh mi — vat-pork pate, vat-ham, pickled daikon, cilantro, jalapeno on a baguette baked in-house — is Φ8 and is one of the most complete sandwiches in the western corridor. Owner Thanh Okafor-Pham runs the shop with her daughter and considers the banh mi a perfect sandwich that requires no improvement, only consistency.",
  coordinates: { lat: 42.0370, lng: -88.2820 },
  tags: ["place", "restaurant", "food", "tier_2"],
  related_entities: ["Thanh Okafor-Pham"],
  frequented_by: ["Elgin residents", "Pho seekers", "Banh mi devotees"]
});

writePlace({
  name: "Biscuit Box",
  aliases: ["BB"],
  description: "A biscuit sandwich shop in Hyde Park making buttermilk biscuits from scratch — real flour, vat-buttermilk, vat-butter — and filling them with fried vat-chicken, vat-bacon and egg, or honey butter. A biscuit sandwich is Φ8-12. The biscuits are flaky, tall, and have the structural integrity to survive being stuffed without collapsing, which owner Dayo Petersen-Kimathi considers the true test of a biscuit. She makes them in batches of forty and they're gone in an hour. When the biscuits are gone, the shop closes. Some days this is 9 AM. Most days it's 10:30.",
  coordinates: { lat: 41.7960, lng: -87.5910 },
  tags: ["place", "cafe", "food", "tier_2"],
  related_entities: ["Dayo Petersen-Kimathi"],
  frequented_by: ["Hyde Park early risers", "Biscuit devotees", "People racing the clock"]
});

// ═══════════════════════════════════════════════════════════════════════════════
// $$$ TIER 3-4 — LACEWORKS/CORE MID-RANGE (50 venues)
// Sit-down restaurants, fusion, wine bars, sushi, steakhouses
// ═══════════════════════════════════════════════════════════════════════════════

writePlace({
  name: "Diaspora Table",
  aliases: ["Diaspora"],
  description: "A fusion restaurant in the West Loop that takes the Ubiquitous Diaspora literally: every dish on the menu combines culinary traditions from at least two continents. Miso-glazed vat-lamb chops with chimichurri (Φ34). Jerk-spiced vat-duck breast with coconut dal (Φ30). Szechuan peppercorn-crusted vat-tuna with wasabi guacamole (Φ28). Chef-owner Amara Lindberg-Okafor calls her cooking 'post-national cuisine' — food from everywhere, belonging nowhere, tasting like the future. The restaurant seats forty-five, the decor is warm minimalism, and the wine list features Great Lakes corridor vintages that pair with confusion and delight in equal measure.",
  coordinates: { lat: 41.8850, lng: -87.6470 },
  tags: ["place", "restaurant", "food", "tier_3"],
  related_entities: ["Amara Lindberg-Okafor"],
  frequented_by: ["West Loop professionals", "Fusion curious diners", "Food critics", "Date night couples"]
});

writePlace({
  name: "Glass & Grain",
  aliases: ["G&G"],
  description: "A wine and cocktail bar in Bucktown with a food menu that justifies a two-hour stay. The wine list is sixty bottles deep, all Great Lakes corridor or northern agricultural zone vintages. The cocktails are crafted by a bar team that treats mixology as chemistry. The food: charcuterie boards featuring vat-cured meats and real cheeses (Φ24), flatbreads with seasonal toppings (Φ18), and a chocolate tart (Φ14) that pastry chef Nkem Okafor-Strand makes fresh daily. The room is all exposed brick, soft light, and the sound of ice in crystal. Owner Jules Svensson-Achebe designed it to feel like the kind of place adults go when they want to pretend they're not exhausted.",
  coordinates: { lat: 41.9120, lng: -87.6800 },
  tags: ["place", "bar", "food", "nightlife", "tier_3"],
  related_entities: ["Jules Svensson-Achebe", "Nkem Okafor-Strand"],
  frequented_by: ["Bucktown professionals", "Date night couples", "Wine enthusiasts"]
});

writePlace({
  name: "Harborview Sushi",
  aliases: ["Harborview"],
  description: "A sushi restaurant on the Milwaukee lakefront with floor-to-ceiling windows overlooking the harbor. The fish is a mix: colony-caught perch and whitefish from the lake, vat-grown salmon and tuna for the premium cuts. A twelve-piece omakase is Φ45. Individual pieces range from Φ3-8. Chef Kenji Lindqvist-Osei trained in classical Japanese technique and applies it to Great Lakes fish with results that are both faithful and regional — a lake perch nigiri that belongs nowhere else in the world. The rice is real, short-grain, seasoned with rice vinegar he makes himself.",
  coordinates: { lat: 43.0380, lng: -87.8920 },
  tags: ["place", "restaurant", "food", "tier_3"],
  related_entities: ["Kenji Lindqvist-Osei"],
  frequented_by: ["Milwaukee professionals", "Sushi enthusiasts", "Harbor-view seekers"]
});

writePlace({
  name: "Ember Room",
  aliases: ["Ember"],
  description: "A steakhouse in River North serving premium-grade vat-grown steaks that are aged, seasoned, and grilled over hardwood charcoal. A vat-ribeye is Φ38. A vat-filet mignon is Φ42. The steaks are the best synthetic meat available outside the Spire tier — dense, marbled, with a flavor profile that approaches real beef without quite reaching it. Chef-owner Obinna Lindqvist-Hassan considers the gap between vat and real to be 'the last ten percent, which is the hardest ten percent of anything.' The room is dark, the service is precise, and the wine list is deep enough to get lost in.",
  coordinates: { lat: 41.8930, lng: -87.6320 },
  tags: ["place", "restaurant", "food", "tier_3"],
  related_entities: ["Obinna Lindqvist-Hassan"],
  frequented_by: ["River North executives", "Expense account diners", "Steak aficionados"]
});

writePlace({
  name: "The Pearl",
  aliases: ["Pearl"],
  description: "A seafood restaurant in Lincoln Park sourcing fish from both the Great Lakes colony fishers and coastal importers — real ocean fish, flown in, expensive, and transformative. A grilled lake trout is Φ28. Imported ocean scallops are Φ36. The raw bar features colony-caught oysters from a Lake Michigan operation that most people don't know exists. Chef Solange Okafor-Petersen grew up on the lake and considers freshwater fish underrated by a culinary establishment that has always favored the ocean. Her restaurant is a correction of that bias, and it's working.",
  coordinates: { lat: 41.9260, lng: -87.6380 },
  tags: ["place", "restaurant", "food", "tier_3"],
  related_entities: ["Solange Okafor-Petersen"],
  frequented_by: ["Lincoln Park affluent residents", "Seafood enthusiasts", "Lake fish converts"]
});

writePlace({
  name: "Atelier Noodle",
  aliases: ["Atelier"],
  description: "A high-end noodle restaurant in Fulton Market that treats noodles as fine dining. The udon is made from stone-ground flour, hand-cut, and served in a dashi so refined it tastes like the ocean distilled. The ramen uses a forty-eight-hour tonkotsu broth enriched with vat-bone marrow. A bowl is Φ22-28. The restaurant seats thirty, all at a counter facing the open kitchen, where you watch three cooks work with the focused intensity of surgeons. Chef Hiroshi Eriksson-Nakamura left a Spire restaurant to open this because he believed noodles deserved the same respect as a twelve-course tasting menu.",
  coordinates: { lat: 41.8860, lng: -87.6530 },
  tags: ["place", "restaurant", "food", "tier_3"],
  related_entities: ["Hiroshi Eriksson-Nakamura"],
  frequented_by: ["Fulton Market professionals", "Noodle devotees", "Counter-seat diners"]
});

writePlace({
  name: "Terroir",
  aliases: ["Terroir Wine Bar"],
  description: "A wine bar and small-plates restaurant in the Gold Coast serving natural wines from Great Lakes corridor vineyards — wines made without synth-additives, unfiltered, sometimes funky, always interesting. A glass is Φ12-20. A bottle is Φ40-120. The food is designed to pair: burrata with aged balsamic (Φ18, real balsamic, aged twelve years), vat-lamb tartare with preserved lemon (Φ22), and a selection of real cheeses sourced from small-scale dairy operations in Wisconsin. Owner Celeste Lindqvist-Dubois was a sommelier before she was a restaurateur, and she considers wine a conversation and food the punctuation.",
  coordinates: { lat: 41.9020, lng: -87.6280 },
  tags: ["place", "bar", "food", "nightlife", "tier_3"],
  related_entities: ["Celeste Lindqvist-Dubois"],
  frequented_by: ["Gold Coast wine drinkers", "Natural wine enthusiasts", "Cheese plate devotees"]
});

writePlace({
  name: "Cornerstone",
  aliases: ["Cornerstone Restaurant"],
  description: "A New American restaurant in Milwaukee's Fifth Ward that serves a menu built around whatever is freshest that week. The menu changes every Monday. Last week: seared vat-duck breast with cherry reduction (Φ32), pan-roasted lake whitefish with brown butter (Φ28), a root vegetable risotto with real parmesan (Φ24). Chef-owner Kofi Petersen-Lindgren sources aggressively from local producers and considers a static menu a sign of creative death. The restaurant seats thirty-five, the space is industrial-chic (exposed ductwork, concrete floors, warm lighting), and reservations are recommended on weekends.",
  coordinates: { lat: 43.0310, lng: -87.9080 },
  tags: ["place", "restaurant", "food", "tier_3"],
  related_entities: ["Kofi Petersen-Lindgren"],
  frequented_by: ["Milwaukee's professional class", "Foodies", "Regular reservation holders"]
});

writePlace({
  name: "Sakura Garden",
  aliases: ["Sakura"],
  description: "A Japanese restaurant in Lakeview serving sushi, sashimi, and izakaya small plates in a space decorated with actual cherry blossom branches — artificial, but convincing enough to create an atmosphere that transports. The sashimi uses colony-caught lake fish and vat-grown tuna. An eight-piece sashimi plate is Φ30. Izakaya plates — grilled vat-chicken skewers, edamame, gyoza — are Φ10-16 each. Chef Yui Osei-Tanaka runs both the sushi bar and the kitchen, which would be impossible for most people but appears effortless for her.",
  coordinates: { lat: 41.9430, lng: -87.6530 },
  tags: ["place", "restaurant", "food", "tier_3"],
  related_entities: ["Yui Osei-Tanaka"],
  frequented_by: ["Lakeview diners", "Sushi and izakaya enthusiasts", "Date night couples"]
});

writePlace({
  name: "The Foundry Kitchen",
  aliases: ["Foundry"],
  description: "A farm-to-table restaurant in a converted foundry building in Milwaukee's Menomonee Valley. The farm is a hydroponic operation on the building's roof, and the table is forty seats surrounded by the original foundry equipment — massive, rusted, beautiful. Chef Nadia Okafor-Eriksson grows seventy percent of her vegetables on-site, sources the rest from corridor producers, and treats the vat-proteins with a respect that elevates them. A three-course dinner is Φ55. The menu changes seasonally, and each dish comes with a note about where every ingredient originated. This is not pretension — it's accountability.",
  coordinates: { lat: 43.0210, lng: -87.9250 },
  tags: ["place", "restaurant", "food", "tier_3"],
  related_entities: ["Nadia Okafor-Eriksson"],
  frequented_by: ["Milwaukee foodies", "Farm-to-table advocates", "Industrial architecture enthusiasts"]
});

writePlace({
  name: "Violet Hour Cocktail Bar",
  aliases: ["Violet Hour", "VH"],
  description: "A cocktail bar in Wicker Park that has been operating since before the century turned and survived by being too good to close. The cocktails are precise, seasonal, and served in glassware that the bartenders select specifically for each drink. A cocktail is Φ14-18. The food menu is limited to six items, each designed to pair with the drinks: truffle fries (Φ14), smoked vat-salmon crostini (Φ16), a cheese plate that rotates weekly (Φ20). The bar is hidden behind an unmarked door on a side street, which was a novelty in 2050 and is now simply tradition. Owner Ingrid Okafor-Strand considers the unmarked door an honesty — if you know, you know.",
  coordinates: { lat: 41.9100, lng: -87.6790 },
  tags: ["place", "bar", "food", "nightlife", "tier_3"],
  related_entities: ["Ingrid Okafor-Strand"],
  frequented_by: ["Wicker Park's cocktail crowd", "Secret bar enthusiasts", "People who know about the door"]
});

writePlace({
  name: "Golden Elephant",
  aliases: ["Golden Elephant Thai"],
  description: "A Thai restaurant in Ravenswood serving a menu that goes far beyond the usual pad thai and green curry — though both are available and excellent. The specialties are northern Thai dishes: khao soi (Φ22, a coconut curry noodle soup with crispy noodle topping), laab (Φ18, a spicy minced vat-protein salad), and a whole grilled vat-fish with chili-lime sauce (Φ32). Chef-owner Narong Okafor-Svensson sources fresh lemongrass, galangal, and kaffir lime from a Thai herb garden he maintains in a greenhouse behind the restaurant. Real herbs change everything, and this food proves it.",
  coordinates: { lat: 41.9740, lng: -87.6730 },
  tags: ["place", "restaurant", "food", "tier_3"],
  related_entities: ["Narong Okafor-Svensson"],
  frequented_by: ["Ravenswood diners", "Thai food enthusiasts", "Northern Thai devotees"]
});

writePlace({
  name: "Provisions",
  aliases: ["Provisions Restaurant"],
  description: "A market-driven restaurant in Oak Park where the kitchen buys whatever looks best at the morning market and builds the menu from there. By noon, the day's four entrees, three appetizers, and two desserts are posted on a chalkboard. Yesterday: grilled lake bass with fennel and orange (Φ28), braised vat-short ribs with polenta (Φ32), a beet salad with goat cheese and walnut (Φ16). Today: something entirely different. Chef Adaeze Gustafsson-Petersen considers a fixed menu a form of cowardice and enjoys the daily constraint of working with what's available.",
  coordinates: { lat: 41.8850, lng: -87.7890 },
  tags: ["place", "restaurant", "food", "tier_3"],
  related_entities: ["Adaeze Gustafsson-Petersen"],
  frequented_by: ["Oak Park residents", "Market-menu enthusiasts", "Chalkboard readers"]
});

writePlace({
  name: "The Spice Route",
  aliases: ["Spice Route"],
  description: "An Indian fine-dining restaurant in Streeterville that elevates traditional dishes with premium ingredients and modern technique. The butter chicken uses vat-chicken thighs braised in a tomato-cream sauce enriched with real butter and finished with actual kashmiri chili powder (Φ26). The biryani is layered in the traditional dum method, sealed with real dough, and cracked open tableside (Φ30). Chef-owner Kavitha Lindqvist-Reddy trained in Michelin-starred kitchens before opening a restaurant that serves the food she grew up with, prepared with the precision she learned elsewhere.",
  coordinates: { lat: 41.8930, lng: -87.6200 },
  tags: ["place", "restaurant", "food", "tier_3"],
  related_entities: ["Kavitha Lindqvist-Reddy"],
  frequented_by: ["Streeterville professionals", "Indian cuisine enthusiasts", "Tableside biryani seekers"]
});

writePlace({
  name: "Lake & Vine",
  aliases: ["L&V"],
  description: "A wine bar and bistro on Milwaukee's lakefront serving French-inspired food with Great Lakes ingredients. Moules-frites using lake mussels (Φ22). Steak-frites with a vat-hanger steak and real bearnaise (Φ30). A cheese board featuring Wisconsin artisan cheeses (Φ24). The wine list is half corridor vintages, half imported — actual imported wine, from actual vineyards, at prices that reflect the logistics. A glass of imported Burgundy is Φ25. A glass of corridor Pinot Noir is Φ14. Owner Francoise Okafor-Dubois considers both worth it for different reasons.",
  coordinates: { lat: 43.0490, lng: -87.8890 },
  tags: ["place", "bar", "food", "nightlife", "tier_3"],
  related_entities: ["Francoise Okafor-Dubois"],
  frequented_by: ["Milwaukee's wine scene", "French food enthusiasts", "Lakefront date night"]
});

writePlace({
  name: "Kuroshio",
  aliases: ["Kuroshio Sushi"],
  description: "A high-end sushi restaurant in the Gold Coast specializing in omakase — chef's choice, no menu, you eat what Chef Masa Lindberg-Hayashi prepares. A twelve-course omakase is Φ65. A twenty-course experience, for the committed, is Φ110. The fish is a mix of colony-caught lake species, vat-grown ocean fish, and occasionally — for special courses — real ocean fish flown in at costs that only the Spire tier ignores. Masa prepares each piece at a twelve-seat counter with a precision that makes surgery look casual. The restaurant takes twelve guests per seating, two seatings per night. Reservations book three weeks out.",
  coordinates: { lat: 41.9010, lng: -87.6260 },
  tags: ["place", "restaurant", "food", "tier_4"],
  related_entities: ["Masa Lindberg-Hayashi"],
  frequented_by: ["Gold Coast wealthy", "Omakase devotees", "Sushi purists willing to spend"]
});

writePlace({
  name: "Green Bay Chop House",
  aliases: ["GBCH", "Chop House"],
  description: "A steakhouse in Green Bay's downtown core serving premium vat-steaks in an atmosphere of dark wood, white tablecloths, and the quiet confidence of a restaurant that knows what it does well. A vat-prime ribeye is Φ36. A vat-porterhouse for two is Φ70. The sides are generous and traditional: creamed spinach, baked potato, Caesar salad. The wine list is functional rather than adventurous. Owner Manu Okafor-Johansson comes from a family of butchers and considers the steak the only thing that matters — everything else exists in service of the meat.",
  coordinates: { lat: 44.5140, lng: -88.0130 },
  tags: ["place", "restaurant", "food", "tier_3"],
  related_entities: ["Manu Okafor-Johansson"],
  frequented_by: ["Green Bay business dinners", "Steak enthusiasts", "Special occasion diners"]
});

writePlace({
  name: "Piccolo",
  aliases: ["Piccolo Trattoria"],
  description: "An Italian trattoria in Lincoln Park serving handmade pasta — real semolina, real eggs, rolled and cut in-house — with sauces that range from a simple aglio e olio (Φ18) to a rich vat-boar ragu (Φ28). The pasta is the message. Chef-owner Lucia Lindqvist-Bianchi makes every shape by hand: pappardelle, orecchiette, ravioli, tagliatelle. The ravioli, filled with ricotta and lemon zest, in sage brown butter (Φ24), is the dish that people remember and return for. The restaurant is small — twenty-two seats — and the noise level at capacity suggests a room full of people who are enjoying themselves.",
  coordinates: { lat: 41.9220, lng: -87.6470 },
  tags: ["place", "restaurant", "food", "tier_3"],
  related_entities: ["Lucia Lindqvist-Bianchi"],
  frequented_by: ["Lincoln Park pasta devotees", "Italian food enthusiasts", "Ravioli pilgrims"]
});

writePlace({
  name: "Charcoal",
  aliases: ["Charcoal Grill"],
  description: "A Middle Eastern grill in Albany Park serving kebabs, grilled vegetables, and mezze with the authority of a kitchen that has been perfecting these dishes across generations. The lamb kebab (vat-grown, but marinated for twenty-four hours in a yogurt-spice blend) is Φ24. The mixed mezze — hummus, baba ganoush, tabbouleh, pickles, and warm pita — is Φ20 and feeds two. Chef-owner Tariq Lindberg-Hamid built a custom charcoal grill that burns at specific temperatures for each protein, and the smoky char on the kebabs is the restaurant's signature. The space is bright, tiled in blue and white, and smells like the best moment of every day.",
  coordinates: { lat: 41.9680, lng: -87.7230 },
  tags: ["place", "restaurant", "food", "tier_3"],
  related_entities: ["Tariq Lindberg-Hamid"],
  frequented_by: ["Albany Park residents", "Kebab enthusiasts", "Mezze sharers"]
});

writePlace({
  name: "Moonrise Cocktail Club",
  aliases: ["Moonrise"],
  description: "A rooftop cocktail bar and restaurant in the Loop with views of the lake and the skyline. The cocktails are theatrical — smoked, frozen, foamed, set on fire — and cost Φ16-22. The food menu is small but precise: tartare of vat-tuna (Φ22), duck-fat popcorn (Φ10), a wagyu-grade vat-beef slider trio (Φ26). The view is the amenity, but the drinks justify the visit independently. Owner Celestine Eriksson-Okafor designed the space so every seat has a sightline to the lake, which at sunset turns the entire bar golden. Reservations required Friday and Saturday.",
  coordinates: { lat: 41.8800, lng: -87.6260 },
  tags: ["place", "bar", "food", "nightlife", "tier_3"],
  related_entities: ["Celestine Eriksson-Okafor"],
  frequented_by: ["Loop professionals", "Sunset cocktail seekers", "Date night couples"]
});

writePlace({
  name: "Fork & Flame",
  aliases: ["Fork Flame"],
  description: "A wood-fired restaurant in Evanston that cooks everything — protein, vegetables, bread, even dessert — over open wood fire. The vat-chicken roasted over cherry wood (Φ26) has a smokiness that permeates to the bone. The hearth bread (Φ8) is baked in the embers and has a crust like armor. The vegetables — real vegetables from a North Shore hydroponic cooperative — are grilled and served with a simplicity that lets the fire do the talking. Chef-owner Nanna Lindqvist-Osei believes cooking over fire is the oldest and most honest form of food preparation and that every technological advance since has been a compromise.",
  coordinates: { lat: 42.0490, lng: -87.6810 },
  tags: ["place", "restaurant", "food", "tier_3"],
  related_entities: ["Nanna Lindqvist-Osei"],
  frequented_by: ["Evanston fine diners", "Fire cooking enthusiasts", "Hearth bread devotees"]
});

writePlace({
  name: "The Sardine Room",
  aliases: ["Sardine Room"],
  description: "A small-plates restaurant in Milwaukee's East Side that is deliberately, almost aggressively tiny — eighteen seats in a room barely bigger than a bedroom. The intimacy is the point. The menu is twelve small plates, each Φ12-18: burrata with basil oil, charred octopus (vat-grown) with paprika aioli, lamb meatballs (vat) in tomato-saffron broth. Chef-owner Adaeze Svensson-Lindberg believes that small spaces force good food — no room for filler, no space for mediocrity. The wine list is ten bottles, each chosen to pair with the plate directly below it on the menu. Reservations are not just recommended, they're essential.",
  coordinates: { lat: 43.0560, lng: -87.8890 },
  tags: ["place", "restaurant", "food", "tier_3"],
  related_entities: ["Adaeze Svensson-Lindberg"],
  frequented_by: ["Milwaukee's intimate dining crowd", "Small-plates enthusiasts", "Reservation planners"]
});

writePlace({
  name: "Bramble & Bone",
  aliases: ["B&B"],
  description: "A brunch restaurant in Wicker Park serving what it calls 'restorative brunch' — meals designed to rebuild you after whatever the week did. The bone broth Benedict (Φ18) serves eggs on toast with a ladleful of rich vat-bone broth instead of hollandaise. The congee with soft egg and chili crisp (Φ16) is comfort weaponized. The coffee service includes a 'recovery flight' — a cortado, a cold brew, and an espresso, served in order of escalating intensity (Φ14). Chef-owner Kaya Eriksson-Okafor considers brunch a medical intervention and the coffee a prescription.",
  coordinates: { lat: 41.9100, lng: -87.6760 },
  tags: ["place", "restaurant", "food", "tier_3"],
  related_entities: ["Kaya Eriksson-Okafor"],
  frequented_by: ["Wicker Park's recovering weekenders", "Brunch devotees", "Coffee flight enthusiasts"]
});

writePlace({
  name: "Maplewood Tavern",
  aliases: ["Maplewood"],
  description: "A gastropub in Logan Square that pairs a rotating craft beer selection with a food menu that takes bar food seriously. Duck confit poutine (Φ20) with real cheese curds and vat-duck confit. A Wagyu-grade vat-burger with gruyere and truffle aioli (Φ22). Fish tacos with beer-battered lake perch and mango salsa (Φ18). The beer list is thirty taps, half of them exclusive corridor microbrews you can't get elsewhere. Owner Nnamdi Lindqvist-Chen considers the intersection of good beer and good food to be civilization's highest achievement and runs his pub accordingly.",
  coordinates: { lat: 41.9250, lng: -87.7020 },
  tags: ["place", "pub", "food", "nightlife", "tier_3"],
  related_entities: ["Nnamdi Lindqvist-Chen"],
  frequented_by: ["Logan Square's craft beer crowd", "Gastropub enthusiasts", "Duck poutine converts"]
});

writePlace({
  name: "Ivory & Saffron",
  aliases: ["I&S"],
  description: "A Persian restaurant in Ravenswood serving dishes that most diners in the corridor have never encountered. Tahdig — the crispy golden rice from the bottom of the pot — is the signature (Φ18 as a side, and worth every quanta). Ghormeh sabzi — an herb stew with vat-lamb (Φ26). Joojeh kebab — saffron-marinated vat-chicken grilled over charcoal (Φ24). Chef-owner Shirin Okafor-Hosseini uses real saffron, which at current prices makes it one of the most expensive spices in the corridor. The saffron turns rice golden, flavors broth with an earthy sweetness, and justifies its cost by transforming everything it touches.",
  coordinates: { lat: 41.9750, lng: -87.6750 },
  tags: ["place", "restaurant", "food", "tier_3"],
  related_entities: ["Shirin Okafor-Hosseini"],
  frequented_by: ["Ravenswood residents", "Persian cuisine enthusiasts", "Tahdig devotees"]
});

writePlace({
  name: "Café Lune",
  aliases: ["Lune"],
  description: "A French café and patisserie in the Gold Coast that serves croissants, tartines, quiches, and pastries alongside real-bean coffee and a selection of French wines by the glass. A croissant is Φ8 — expensive, but the lamination is textbook perfect and the vat-butter content is high enough to make it shatter. A croque monsieur is Φ16. The pastry case displays macarons, eclairs, and a Paris-Brest that is a work of architectural engineering. Owner and chef Margaux Lindqvist-Osei trained in Paris in a VR culinary program modeled on a Michelin-starred patisserie, and her technique is indistinguishable from the real thing.",
  coordinates: { lat: 41.9000, lng: -87.6270 },
  tags: ["place", "cafe", "food", "tier_3"],
  related_entities: ["Margaux Lindqvist-Osei"],
  frequented_by: ["Gold Coast residents", "Pastry enthusiasts", "People seeking Parisian pretense"]
});

writePlace({
  name: "Okonomi",
  aliases: ["Okonomi"],
  description: "A Japanese okonomiyaki restaurant in Pilsen — the only one in the GLMZ corridor — serving savory pancakes grilled tableside on built-in teppan grills. Each table has its own grill, and diners either cook their own (Φ18 for ingredients and instruction) or have the chef cook for them (Φ24). The pancakes are loaded with cabbage, vat-pork belly, pickled ginger, and finished with okonomiyaki sauce, mayo, and bonito flakes. Chef Aoi Lindqvist-Mensah opened this restaurant because she missed okonomiyaki and decided that if no one else was going to make it in Chicago, she would.",
  coordinates: { lat: 41.8570, lng: -87.6610 },
  tags: ["place", "restaurant", "food", "tier_3"],
  related_entities: ["Aoi Lindqvist-Mensah"],
  frequented_by: ["Pilsen diners", "Japanese food enthusiasts", "Tableside cooking adventurers"]
});

writePlace({
  name: "Cedarwood",
  aliases: ["Cedarwood Grill"],
  description: "A Lebanese restaurant in Skokie serving an extensive mezze menu, wood-fired flatbreads, and grilled meats that are the best in the northern suburbs. The mixed grill — vat-lamb, vat-chicken, and kofta over charcoal, served with garlic sauce, pickled turnip, and fresh flatbread — is Φ32 for two. The fattoush salad uses real vegetables and sumac, creating an acid-bright contrast to the smoky proteins. Owner Hassan Eriksson-Khoury built a custom wood-fired oven for the flatbreads, and the bread alone — puffed, charred, served seconds off the fire — is worth the trip.",
  coordinates: { lat: 42.0330, lng: -87.7330 },
  tags: ["place", "restaurant", "food", "tier_3"],
  related_entities: ["Hassan Eriksson-Khoury"],
  frequented_by: ["Skokie residents", "Lebanese food enthusiasts", "Flatbread pilgrims"]
});

writePlace({
  name: "Ninth Wave",
  aliases: ["Ninth Wave"],
  description: "A seafood restaurant in Green Bay's revitalized waterfront district, serving Great Lakes fish prepared with Nordic-influenced technique. Smoked lake trout with dill cream (Φ24). Pickled herring with rye bread and mustard (Φ18). A whole roasted lake whitefish for two (Φ48). The restaurant's decor evokes Scandinavian minimalism — pale wood, clean lines, candles — and the food is deceptively simple: few ingredients, each treated with respect. Chef-owner Sigrid Okafor-Lindgren grew up fishing on Green Bay and considers the lake's fish the most underused premium ingredient in the corridor.",
  coordinates: { lat: 44.5180, lng: -88.0050 },
  tags: ["place", "restaurant", "food", "tier_3"],
  related_entities: ["Sigrid Okafor-Lindgren"],
  frequented_by: ["Green Bay's dining scene", "Seafood enthusiasts", "Nordic food curious"]
});

writePlace({
  name: "The Copper Fox",
  aliases: ["Copper Fox"],
  description: "A whiskey bar and restaurant in Milwaukee's Third Ward serving New American cuisine alongside a whiskey collection that numbers over three hundred bottles. The food is designed for whiskey pairing: smoked vat-duck breast with bourbon glaze (Φ30), charred corn bisque (Φ16), and a pecan pie with rye whiskey caramel (Φ14). The whiskey flights (Φ20-40) are curated by the bar team and come with tasting notes written by owner Declan Lindqvist-Okafor, who considers whiskey a form of storytelling and each bottle a narrative. The bar is dark amber, the lighting is warm, and the mood is contemplative.",
  coordinates: { lat: 43.0350, lng: -87.9080 },
  tags: ["place", "bar", "food", "nightlife", "tier_3"],
  related_entities: ["Declan Lindqvist-Okafor"],
  frequented_by: ["Third Ward whiskey enthusiasts", "Pairing dinner guests", "Contemplative drinkers"]
});

writePlace({
  name: "Root & Bloom",
  aliases: ["Root Bloom"],
  description: "A vegetable-forward restaurant in Oak Park that proves you don't need protein to make great food — though it's available as an add-on. The cauliflower steak, roasted whole with harissa and tahini (Φ22), is a signature. The mushroom tasting plate — five preparations of five mushroom species, all hydroponically grown — is Φ26. The cocktail list is botanical, featuring drinks infused with herbs from the restaurant's garden. Chef-owner Ife Lindqvist-Strand considers vegetables the most creative medium in the kitchen and treats them with a seriousness usually reserved for fine proteins.",
  coordinates: { lat: 41.8860, lng: -87.7920 },
  tags: ["place", "restaurant", "food", "tier_3"],
  related_entities: ["Ife Lindqvist-Strand"],
  frequented_by: ["Oak Park diners", "Vegetable enthusiasts", "The meat-optional crowd"]
});

writePlace({
  name: "Lighthouse Bistro",
  aliases: ["Lighthouse"],
  description: "A bistro inside a converted lighthouse on the Kenosha harbor. The lighthouse still functions — the light rotates at night, visible from the dining room, casting rhythmic shadows across the tables. The food is lakefront-inspired: pan-seared lake perch (Φ26), clam chowder made with lake clams (Φ16), and a bouillabaisse-style lake fish stew (Φ32). The setting is the draw — eating inside a working lighthouse, watching the beam sweep the lake — but Chef Adaeze Lindqvist-Hansen ensures the food deserves the location. The wine list favors whites and rosés that pair with fish and with the view.",
  coordinates: { lat: 42.5870, lng: -87.8100 },
  tags: ["place", "restaurant", "food", "tier_3"],
  related_entities: ["Adaeze Lindqvist-Hansen"],
  frequented_by: ["Kenosha special occasion diners", "Lighthouse enthusiasts", "Lakefront romantics"]
});

writePlace({
  name: "The Crimson Lantern",
  aliases: ["Crimson Lantern"],
  description: "A dim sum restaurant in Chinatown that operates weekend brunch service with rolling carts — the traditional way, with bamboo steamers stacked on trolleys pushed between tables by staff who call out their offerings. Har gow, siu mai, char siu bao, cheung fun, custard tarts — each Φ6-10 per basket. The quality is a tier above the street-level Chinatown joints: the shrimp in the har gow is a mix of real lake shrimp and vat-grown, and the dumplings are folded with the twenty-plus pleats that signify proper technique. Chef-owner Jimmy Okafor-Lau has been doing dim sum for thirty years and considers the cart service non-negotiable.",
  coordinates: { lat: 41.8520, lng: -87.6330 },
  tags: ["place", "restaurant", "food", "tier_3"],
  related_entities: ["Jimmy Okafor-Lau"],
  frequented_by: ["Chinatown regulars", "Dim sum enthusiasts", "Weekend brunch families"]
});

writePlace({
  name: "Sage & Salt",
  aliases: ["Sage Salt"],
  description: "A Mediterranean-California fusion restaurant in Naperville serving dishes that combine coastal Mediterranean technique with the Great Lakes larder. Grilled halloumi with watermelon and mint (Φ18). Vat-lamb shoulder braised in white wine with olives and preserved lemon (Φ32). A citrus and olive oil cake (Φ12) that tastes like sunshine, which in the corridor qualifies as an exotic ingredient. Chef-owner Amira Lindqvist-Osei designed the menu to taste like the Mediterranean climate that the Great Lakes region will never have but can dream about.",
  coordinates: { lat: 41.7720, lng: -88.1470 },
  tags: ["place", "restaurant", "food", "tier_3"],
  related_entities: ["Amira Lindqvist-Osei"],
  frequented_by: ["Naperville's dining crowd", "Mediterranean food enthusiasts", "Sunshine seekers"]
});

writePlace({
  name: "Ember & Rye",
  aliases: ["Ember Rye"],
  description: "A Scandinavian-inspired restaurant in Waukesha that embraces the region's Nordic culinary heritage — fermentation, preservation, fire, and the patient transformation of simple ingredients into something remarkable. Gravlax with dill and mustard sauce (Φ22). Smoked vat-venison with lingonberry reduction (Φ30). A rye bread that's fermented for three days and baked until the crust is nearly black (Φ8, served with cultured butter). Chef Astrid Okafor-Lindgren considers the long game — fermenting, curing, aging — to be the essence of Nordic cooking and runs her kitchen on timelines measured in days, not hours.",
  coordinates: { lat: 43.0120, lng: -88.2290 },
  tags: ["place", "restaurant", "food", "tier_3"],
  related_entities: ["Astrid Okafor-Lindgren"],
  frequented_by: ["Waukesha diners", "Nordic food enthusiasts", "Fermentation devotees"]
});

writePlace({
  name: "Yonder",
  aliases: ["Yonder Restaurant"],
  description: "A Southern-inspired restaurant in Bronzeville that serves vat-fried chicken, collard greens, mac and cheese, and cornbread at a quality level that transcends its ingredients. The fried chicken (Φ22 for a half bird) is brined for twenty-four hours, dredged in seasoned flour, and fried in a cast-iron skillet that has been seasoned for fifteen years. The cornbread is made with real cornmeal and baked in cast iron. The collards are slow-cooked with vat-ham hock. Chef-owner Darnella Lindqvist-Washington calls this 'memory food' — it tastes like something you remember even if you've never had it before.",
  coordinates: { lat: 41.8230, lng: -87.6170 },
  tags: ["place", "restaurant", "food", "tier_3"],
  related_entities: ["Darnella Lindqvist-Washington"],
  frequented_by: ["Bronzeville residents", "Soul food enthusiasts", "Memory food seekers"]
});

writePlace({
  name: "Aquavit North",
  aliases: ["Aquavit"],
  description: "A Scandinavian fine-dining restaurant in Green Bay specializing in preserved, fermented, and smoked preparations of Great Lakes fish and local produce. The tasting menu (Φ55 for five courses) features courses like cured lake trout with buttermilk and horseradish, smoked whitefish with potato and dill, and a dessert of cloudberry sorbet with aquavit granita. Chef-owner Bjorn Okafor-Nilssen considers preservation techniques a way to capture time itself — each pickle, each cure, each ferment is a conversation with an older season. The dining room is austere, candlelit, and seats twenty-four.",
  coordinates: { lat: 44.5160, lng: -88.0100 },
  tags: ["place", "restaurant", "food", "tier_4"],
  related_entities: ["Bjorn Okafor-Nilssen"],
  frequented_by: ["Green Bay's fine dining crowd", "Nordic cuisine enthusiasts", "Preservation technique admirers"]
});

writePlace({
  name: "The Rind",
  aliases: ["Rind"],
  description: "A cheese-focused restaurant and retail shop in Milwaukee's Third Ward that sources from Wisconsin's remaining artisan cheesemakers — small operations that produce real cheese from real milk, a luxury that most corridor residents have never experienced. A cheese flight (five varieties with accompaniments) is Φ28. A grilled cheese made with three-year aged cheddar on sourdough is Φ18 and is a transformative experience if you've never had real cheese. The fondue (Φ36 for two) uses a blend of Gruyere and Emmental that owner Nkem Petersen-Strand sources from a single dairy in the Driftless Area.",
  coordinates: { lat: 43.0340, lng: -87.9070 },
  tags: ["place", "restaurant", "food", "tier_3"],
  related_entities: ["Nkem Petersen-Strand"],
  frequented_by: ["Third Ward foodies", "Cheese enthusiasts", "People discovering real dairy"]
});

// ═══════════════════════════════════════════════════════════════════════════════
// $$$$ TIER 5 — SPIRE LUXURY (20 venues)
// Fine dining, real ingredients, molecular gastronomy, private clubs
// ═══════════════════════════════════════════════════════════════════════════════

writePlace({
  name: "Elevation",
  aliases: ["Elevation Chicago"],
  description: "The highest restaurant in the GLMZ corridor, occupying the 87th floor of a Spire tower in downtown Chicago. Floor-to-ceiling windows provide views of the lake, the city, and on clear days, the curve of the earth. The food is secondary to the altitude — except it isn't, because Chef Aurore Lindqvist-Okafor runs a kitchen that would be extraordinary at ground level and is transcendent at 300 meters. The twelve-course tasting menu (Φ280) features real ingredients throughout: actual beef, actual seafood, actual vegetables. The wine pairing (Φ180) includes bottles from vineyards that most diners have only heard of. A meal here costs what a Shelf family earns in a month. The diners do not think about this.",
  coordinates: { lat: 41.8870, lng: -87.6270 },
  tags: ["place", "restaurant", "food", "tier_5"],
  related_entities: ["Aurore Lindqvist-Okafor"],
  frequented_by: ["Spire executives", "Corporate entertaining", "People for whom money is atmospheric"]
});

writePlace({
  name: "The Threshold",
  aliases: ["Threshold"],
  description: "A private dining club in the Gold Coast that does not advertise, does not accept walk-ins, and does not have a sign. Membership is Φ5,000 annually, and the annual dinner — twelve courses, real ingredients exclusively, wines older than most members — costs Φ400 per person on top of that. Chef Séraphin Okafor-Blanc operates with a staff of eight and serves a maximum of twenty-four guests per evening. Every ingredient is real: beef from a ranch in Montana that still raises actual cattle, vegetables from a private greenhouse, fish caught that morning from Lake Michigan by a contracted fisher. The Threshold exists to remind the Spire that wealth can purchase the past, and the past tastes better than the future.",
  coordinates: { lat: 41.9030, lng: -87.6260 },
  tags: ["place", "restaurant", "food", "tier_5"],
  related_entities: ["Séraphin Okafor-Blanc"],
  frequented_by: ["Spire elite", "Private club members", "People who can afford nostalgia"]
});

writePlace({
  name: "Neural Palate",
  aliases: ["NP", "Neural"],
  description: "A molecular gastronomy restaurant in Streeterville that enhances dining with optional BCI-integrated neural flavor amplification. Diners with compatible neural interfaces can choose the 'enhanced' menu (Φ350), which pairs each course with a neural signal that amplifies specific taste receptors, making flavors more vivid, textures more pronounced, and the overall experience something that cannot be replicated by food alone. The 'analog' menu (Φ200) is excellent without the enhancement but feels, by comparison, like watching a film in black and white. Chef Nkechi Lindqvist-Osei is both a culinary artist and a neurotechnologist, and she considers the intersection of food and neural engineering the future of dining.",
  coordinates: { lat: 41.8940, lng: -87.6190 },
  tags: ["place", "restaurant", "food", "tier_5"],
  related_entities: ["Nkechi Lindqvist-Osei"],
  frequented_by: ["Spire diners with neural interfaces", "Molecular gastronomy enthusiasts", "BCI early adopters"]
});

writePlace({
  name: "Viticulture",
  aliases: ["Viti"],
  description: "A fine-dining restaurant and wine cellar in River North that maintains one of the largest collections of pre-collapse wines in the Great Lakes corridor — bottles from Bordeaux, Burgundy, Napa, and Barossa that were cellared before global supply chains fractured. The cellar holds approximately 8,000 bottles. A tasting of three pre-collapse wines is Φ150. A bottle of 2045 Chateau Margaux is Φ800. The food is French-classical: foie gras (real, from actual ducks), beef Wellington (real beef), and a cheese course featuring imported French fromage at prices that would make a Circuit family faint. Chef Jean-Luc Osei-Beaumont considers pre-collapse wine a connection to a world that no longer exists, and his restaurant a museum that you can drink.",
  coordinates: { lat: 41.8920, lng: -87.6310 },
  tags: ["place", "restaurant", "food", "tier_5"],
  related_entities: ["Jean-Luc Osei-Beaumont"],
  frequented_by: ["Wine collectors", "Spire's old money", "People who drink history"]
});

writePlace({
  name: "Lake Monarch",
  aliases: ["Monarch"],
  description: "A lakefront fine-dining restaurant in Milwaukee's Spire district, cantilevered over the water so diners feel suspended above the lake. The architecture is as much a draw as the food — the dining room is glass-floored in sections, and at night the lake is illuminated beneath you. Chef Kofi Lindqvist-Beaumont serves a seven-course tasting menu (Φ220) that focuses on the lake: colony-caught whitefish, lake mussels, freshwater crayfish, and a lake trout preparation that changes seasonally. Everything is real. Nothing is vat-grown. The restaurant seats thirty, and the wait for a reservation is measured in weeks.",
  coordinates: { lat: 43.0460, lng: -87.8870 },
  tags: ["place", "restaurant", "food", "tier_5"],
  related_entities: ["Kofi Lindqvist-Beaumont"],
  frequented_by: ["Milwaukee's Spire elite", "Architecture enthusiasts", "Lake fish devotees"]
});

writePlace({
  name: "Obsidian",
  aliases: ["Obsidian Dining"],
  description: "A fine-dining restaurant in Chicago's Spire that specializes in what Chef Amara Okafor-Strand calls 'extinction cuisine' — dishes made from ingredients that are functionally extinct in the wild but preserved through genetic banking and controlled cultivation. Real bluefin tuna. Real Wagyu beef. Real truffles, dug by actual dogs from cultivated truffle orchards in southern Indiana. A five-course menu is Φ300. The ethical implications are debated in the press. The diners do not debate — they eat, because they can, and because the food is a reminder of what the world lost and what wealth can still retrieve.",
  coordinates: { lat: 41.8890, lng: -87.6260 },
  tags: ["place", "restaurant", "food", "tier_5"],
  related_entities: ["Amara Okafor-Strand"],
  frequented_by: ["Spire's wealthiest", "Extinction cuisine curious", "Ethical debate participants who eat anyway"]
});

writePlace({
  name: "The Glass Garden",
  aliases: ["Glass Garden"],
  description: "A restaurant inside a temperature-controlled glass conservatory atop a Spire tower in downtown Chicago. The conservatory contains a working garden — real soil, real sunlight, real plants — and the restaurant serves what grows in it. The menu changes daily based on the harvest. On a summer day, dinner might be heirloom tomato salad, grilled lamb with mint from the garden, and strawberry shortcake with berries picked that morning. On a winter day, it's root vegetables, braised meats, and preserved fruit. The eight-course tasting menu is Φ260. Chef Soleil Lindqvist-Okafor considers the garden the chef and herself merely the translator.",
  coordinates: { lat: 41.8850, lng: -87.6250 },
  tags: ["place", "restaurant", "food", "tier_5"],
  related_entities: ["Soleil Lindqvist-Okafor"],
  frequented_by: ["Spire residents", "Garden-to-table purists", "People who miss sunlight on real soil"]
});

writePlace({
  name: "Mirage",
  aliases: ["Mirage Chicago"],
  description: "A fine-dining restaurant in the Gold Coast that changes its entire concept — menu, decor, service style — every three months. This quarter it's Japanese kaiseki. Last quarter it was modernist Spanish. Next quarter it will be Ethiopian fine dining. Chef collective Mirage Studio, led by Yuki Okafor-Strand, treats the restaurant as a living art installation where food is the medium. The current kaiseki menu is fourteen courses (Φ320) and includes preparations that take three days to complete. The commitment to each concept is total — the staff retrains, the kitchen rebuilds, the space transforms. Mirage is not a restaurant; it is a restaurant that reinvents itself quarterly.",
  coordinates: { lat: 41.9050, lng: -87.6280 },
  tags: ["place", "restaurant", "food", "tier_5"],
  related_entities: ["Yuki Okafor-Strand", "Mirage Studio"],
  frequented_by: ["Spire's cultural elite", "Concept dining enthusiasts", "Repeat visitors who never eat the same meal twice"]
});

writePlace({
  name: "Chef's Bunker",
  aliases: ["The Bunker"],
  description: "A sixteen-seat restaurant in a converted bank vault beneath a Spire building in downtown Chicago. The vault door is the entrance. Inside: concrete walls, a single long table, and Chef Ade Lindqvist-Mensah preparing a twenty-course tasting menu (Φ380) in full view of every diner. There is no menu — you eat what Ade cooks, which is whatever inspired him that day. The ingredients are real, sourced that morning, and the progression from first course to twentieth follows an emotional arc that Ade designs intentionally. He considers each dinner a performance and each plate a line of dialogue. The vault door closes at 8 PM. No one enters after.",
  coordinates: { lat: 41.8810, lng: -87.6290 },
  tags: ["place", "restaurant", "food", "tier_5"],
  related_entities: ["Ade Lindqvist-Mensah"],
  frequented_by: ["Spire's adventurous wealthy", "Tasting menu devotees", "People who surrender control to the chef"]
});

writePlace({
  name: "The Botanical",
  aliases: ["Botanical"],
  description: "A restaurant and cocktail bar in Chicago's Spire district where every dish and drink incorporates botanicals grown in an on-site solarium. The gin and tonic uses gin infused with juniper, lavender, and citrus peel from the solarium. The salad courses use greens harvested minutes before service. The main courses incorporate herb preparations that fresh-from-garden ingredients make extraordinary. An eight-course dinner is Φ240. The cocktail pairing is Φ120. Chef-owner Linnea Okafor-Chen considers freshness not a quality but a philosophy, and the maximum distance an ingredient should travel from soil to plate is twenty meters.",
  coordinates: { lat: 41.8880, lng: -87.6260 },
  tags: ["place", "restaurant", "food", "nightlife", "tier_5"],
  related_entities: ["Linnea Okafor-Chen"],
  frequented_by: ["Spire's plant-forward diners", "Botanical cocktail enthusiasts", "Freshness maximalists"]
});

writePlace({
  name: "Forge & Table",
  aliases: ["Forge"],
  description: "A fine-dining restaurant in Milwaukee's emerging Spire that cooks exclusively over a custom-built forge — a blacksmith's forge, adapted for culinary use, burning hand-selected hardwoods at temperatures that exceed conventional ovens by hundreds of degrees. A vat-free wagyu steak cooked over forge heat develops a crust that no grill can replicate (Φ85). The whole roasted chicken (Φ55) — an actual chicken, from an actual farm — arrives at the table with a smokiness that saturates the meat to the bone. Chef-owner Kweku Lindberg-Okafor was a blacksmith before he was a chef and considers heat his primary ingredient.",
  coordinates: { lat: 43.0420, lng: -87.9060 },
  tags: ["place", "restaurant", "food", "tier_5"],
  related_entities: ["Kweku Lindberg-Okafor"],
  frequented_by: ["Milwaukee's Spire elite", "Fire cooking enthusiasts", "People who want to see a forge in a dining room"]
});

writePlace({
  name: "Still Life",
  aliases: ["Still Life Dining"],
  description: "A twelve-seat restaurant in Evanston where dinner is a four-hour experience structured like a gallery exhibition. Each course is presented on custom ceramic plates by artists commissioned for the season, and the food is arranged to reference specific paintings, photographs, or sculptures. The current season's menu references Dutch Golden Age still lifes: a cheese and fruit course arranged like a Vermeer, a seafood course inspired by a de Heem, a dessert that evokes a Kalf. The tasting menu is Φ300. Chef-owner Naomi Okafor-Van Dyck considers art history edible and food a canvas.",
  coordinates: { lat: 42.0500, lng: -87.6800 },
  tags: ["place", "restaurant", "food", "tier_5"],
  related_entities: ["Naomi Okafor-Van Dyck"],
  frequented_by: ["Art collectors", "Evanston's cultural elite", "People who eat paintings"]
});

writePlace({
  name: "Frost",
  aliases: ["Frost Chicago"],
  description: "A molecular gastronomy restaurant in Chicago's Spire that specializes in temperature manipulation. The signature dish — a sphere of gazpacho that is frozen solid on the outside and liquid-hot inside (Φ28 as a course, Φ310 for the full tasting) — requires you to crack it like an egg and drink the soup from the frozen shell. Other courses play similar tricks: ice cream that is warm, bread that is frozen, a dessert that changes temperature as you eat it. Chef-owner Dr. Kai Lindqvist-Frost (yes, the name is real) has a PhD in food science and considers traditional cooking temperatures an arbitrary limitation.",
  coordinates: { lat: 41.8860, lng: -87.6250 },
  tags: ["place", "restaurant", "food", "tier_5"],
  related_entities: ["Dr. Kai Lindqvist-Frost"],
  frequented_by: ["Spire's molecular gastronomy crowd", "Food science enthusiasts", "People who want their soup to surprise them"]
});

writePlace({
  name: "Verdant",
  aliases: ["Verdant Restaurant"],
  description: "A plant-based fine-dining restaurant in River North that serves no animal protein whatsoever — not even vat-grown — and still commands Φ220 for its ten-course tasting menu. Every ingredient is real, grown, and sourced from premium producers. The celery root cooked in its own soil (Φ28 a la carte) is a dish that has been written about in every food publication in the corridor. The king oyster mushroom, dry-aged for two weeks and seared like a steak (Φ32), converts meat-eaters with a single bite. Chef-owner Ife Okafor-Lindqvist does not consider this vegan cooking. She considers it cooking, and the label is irrelevant.",
  coordinates: { lat: 41.8930, lng: -87.6330 },
  tags: ["place", "restaurant", "food", "tier_5"],
  related_entities: ["Ife Okafor-Lindqvist"],
  frequented_by: ["River North's affluent diners", "Plant-based cuisine converts", "Critics and food writers"]
});

writePlace({
  name: "Lumiere",
  aliases: ["Lumiere Dining"],
  description: "A private chef's table experience in a Spire penthouse in downtown Milwaukee, operated by invitation only. Chef Ayo Lindqvist-Dubois cooks for eight guests per evening in the penthouse kitchen while they sit at a single table and watch. The menu is not disclosed in advance. The wines are selected from a private cellar. The cost is Φ400 per person, and the invitation comes through channels that are not publicly described. Those who have attended describe a meal that lasts three hours and changes how they think about food. Those who have not attended are not certain it exists. It does. The penthouse is real. The food is extraordinary. The exclusivity is the architecture.",
  coordinates: { lat: 43.0440, lng: -87.9070 },
  tags: ["place", "restaurant", "food", "tier_5"],
  related_entities: ["Ayo Lindqvist-Dubois"],
  frequented_by: ["Milwaukee's Spire inner circle", "Invitation-only diners", "People who know people"]
});

writePlace({
  name: "Atlas",
  aliases: ["Atlas Chicago"],
  description: "A fine-dining restaurant in the Loop that serves a different national cuisine every month, executed at the highest level with real ingredients. January: French. February: Japanese. March: Ethiopian. April: Peruvian. Each month, Chef collective Atlas Corps — led by Kwame Lindqvist-Osei — retrains, rebuilds the menu, and transforms the dining room. A seven-course dinner is Φ250. The commitment to authenticity is extreme: when the cuisine is Japanese, the fish is flown in from Japan. When it's Ethiopian, the berbere is made from spices imported from Ethiopian highlands. Atlas doesn't adapt cuisines — it imports them, whole.",
  coordinates: { lat: 41.8810, lng: -87.6280 },
  tags: ["place", "restaurant", "food", "tier_5"],
  related_entities: ["Kwame Lindqvist-Osei", "Atlas Corps"],
  frequented_by: ["Spire's global cuisine enthusiasts", "Monthly returning diners", "Cultural authenticity seekers"]
});

writePlace({
  name: "Solstice",
  aliases: ["Solstice Dining"],
  description: "A restaurant in Chicago's Spire that serves only two dinners per year — one on the summer solstice, one on the winter solstice — each a twenty-course tasting menu that reflects the season. The summer menu celebrates abundance: fresh fruits, light proteins, bright flavors. The winter menu celebrates preservation: cured meats, fermented vegetables, dark broths. Each dinner is Φ500 per person and seats forty. Reservations open six months in advance and fill in minutes. Chef Nuru Okafor-Lindqvist considers scarcity a flavor enhancer and rarity a service — in a world of constant availability, something that happens twice a year means something.",
  coordinates: { lat: 41.8840, lng: -87.6240 },
  tags: ["place", "restaurant", "food", "tier_5"],
  related_entities: ["Nuru Okafor-Lindqvist"],
  frequented_by: ["Spire's patient elite", "Solstice tradition keepers", "People who plan six months ahead"]
});

writePlace({
  name: "Origin",
  aliases: ["Origin Restaurant"],
  description: "A fine-dining restaurant in Chicago's Gold Coast that serves a tasting menu built entirely around a single ingredient, which changes monthly. October's ingredient: mushrooms — fourteen courses, each featuring a different species, each prepared differently, from raw to fermented to charred to frozen. November's ingredient: corn — also fourteen courses. The tasting menu is Φ260. Chef-owner Adaeze Okafor-Lindqvist believes that depth is more interesting than breadth and that understanding one ingredient completely is more valuable than knowing a thousand ingredients superficially.",
  coordinates: { lat: 41.9040, lng: -87.6280 },
  tags: ["place", "restaurant", "food", "tier_5"],
  related_entities: ["Adaeze Okafor-Lindqvist"],
  frequented_by: ["Spire's adventurous diners", "Single-ingredient devotees", "Monthly returning guests"]
});

writePlace({
  name: "Canopy",
  aliases: ["Canopy Dining"],
  description: "A treetop restaurant in a Spire-tier residential park on Chicago's North Shore, built among actual trees — imported oaks, grown to maturity in accelerated-growth greenhouses and transplanted to create a canopy above the dining space. The tables are on platforms among the branches. The food is served by staff who navigate the platforms with practiced ease. A five-course dinner is Φ200. The menu is seasonal and features real ingredients exclusively, with an emphasis on foraging — Chef Elodie Lindqvist-Okafor employs a full-time forager who scours the corridor's remaining wild spaces for ingredients. Eating in a tree while consuming food gathered from the wild is either the most authentic dining experience in the Spire or its most absurd performance of nature. Possibly both.",
  coordinates: { lat: 42.0710, lng: -87.6870 },
  tags: ["place", "restaurant", "food", "tier_5"],
  related_entities: ["Elodie Lindqvist-Okafor"],
  frequented_by: ["North Shore elite", "Nature-experience diners", "People who enjoy eating in trees"]
});

writePlace({
  name: "The Crypt",
  aliases: ["Crypt Dining"],
  description: "A fine-dining restaurant in a converted crypt beneath a decommissioned church in Milwaukee's East Side. The space retains its original stone arches, iron gates, and the temperature of the earth — cool, constant, and slightly unsettling. The tasting menu (Φ240) leans into the setting: courses are served in vessels that evoke the space — stone bowls, iron plates, glass that catches candlelight. Chef-owner Obinna Okafor-Lindqvist calls the experience 'memento mori dining' — a reminder that life is finite and therefore meals should be extraordinary. The lamb (real) braised in a sealed stone pot is the signature: you crack the seal at the table, and the steam carries the scent of rosemary and mortality.",
  coordinates: { lat: 43.0590, lng: -87.8880 },
  tags: ["place", "restaurant", "food", "tier_5"],
  related_entities: ["Obinna Okafor-Lindqvist"],
  frequented_by: ["Milwaukee's Spire diners", "Gothic dining enthusiasts", "People unafraid of eating in a crypt"]
});

// ═══════════════════════════════════════════════════════════════════════════════
// SPECIAL/WEIRD — 20 venues
// The unusual, the unexpected, the impossible
// ═══════════════════════════════════════════════════════════════════════════════

writePlace({
  name: "Ironbelly Kitchen",
  aliases: ["Ironbelly", "The Belly"],
  description: "A restaurant operating inside the gutted chassis of a decommissioned Class-3 industrial automaton on the outskirts of Gary. The automaton — designation IBK-0441, formerly a mining platform — stands eighteen meters tall and was stripped of its operational systems in 2191. What remains is the armored shell, which someone with more vision than sense converted into a three-level dining space. Level one (the legs) is the bar. Level two (the torso) is the dining room, seating twenty-four. Level three (the head) is the chef's table, seating four, with windows that look out through the automaton's optical housings. Chef Eze Lindqvist-Okafor serves industrial comfort food — heavy, smoky, protein-rich — from a kitchen built in the automaton's former reactor housing. The menu is Φ20-40. Eating inside a dead machine is either poetic or unsettling, and the regulars have stopped distinguishing between the two.",
  coordinates: { lat: 41.5940, lng: -87.3450 },
  tags: ["place", "restaurant", "food", "tier_2"],
  related_entities: ["Eze Lindqvist-Okafor", "IBK-0441"],
  frequented_by: ["Gary locals", "Automaton enthusiasts", "People who find poetry in dead machines"]
});

writePlace({
  name: "Drift",
  aliases: ["Drift Sushi", "The Float"],
  description: "A floating sushi bar on Lake Michigan, operating from a converted barge that anchors at different points along the Milwaukee lakefront depending on the weather and the captain's mood. Access is by water taxi — Φ10 for the ride, non-refundable if you change your mind upon seeing the barge. The sushi is excellent: colony-caught lake fish, prepared by Chef Hana Okafor-Lindqvist on a twelve-seat counter that rocks gently with the waves. An omakase is Φ50. The experience of eating raw fish on open water while the city lights reflect off the lake is either the most romantic dining experience in Milwaukee or a recipe for seasickness. Regulars develop sea legs. First-timers are advised to choose their seats carefully.",
  coordinates: { lat: 43.0400, lng: -87.8800 },
  tags: ["place", "restaurant", "food", "tier_3"],
  related_entities: ["Hana Okafor-Lindqvist"],
  frequented_by: ["Milwaukee adventurous diners", "Sushi enthusiasts with sea legs", "Lake romantics"]
});

writePlace({
  name: "Noir Dining",
  aliases: ["Noir", "The Dark"],
  description: "A blind dining experience in Chicago's West Loop where the dining room is in complete, absolute darkness. Not dim — dark. You cannot see the food, the table, or the person sitting across from you. The servers navigate by spatial memory and infrared. The four-course menu (Φ65) is not disclosed — you eat what is placed in front of you and discover by taste, texture, and smell what it is. Some courses are designed to confuse: a liquid that tastes like solid food, a solid that tastes like a drink, temperatures that contradict textures. Owner Blessing Lindqvist-Mensah designed Noir to strip dining of its visual bias and force diners to experience food the way it was experienced before plates were beautiful. Most diners find it transformative. Some find it terrifying. Both reactions are correct.",
  coordinates: { lat: 41.8830, lng: -87.6490 },
  tags: ["place", "restaurant", "food", "tier_3"],
  related_entities: ["Blessing Lindqvist-Mensah"],
  frequented_by: ["Experience seekers", "Sensory adventurers", "People willing to eat in the dark"]
});

writePlace({
  name: "Whim",
  aliases: ["Whim Restaurant"],
  description: "A restaurant in Logan Square where the menu is entirely at the chef's discretion. There is no posted menu. You sit down, Chef Aurelia Okafor-Strand cooks whatever she wants to cook that day, and you eat it. The price is Φ35 per person, flat rate, for whatever comes out of the kitchen. Some nights it's four courses of refined French cuisine. Some nights it's a single enormous bowl of pasta. One memorable night it was seventeen varieties of toast. Aurelia's cooking is consistently excellent; her choices are consistently unpredictable. Diners who need control should eat elsewhere. Diners who enjoy surprise will find Whim the most exciting restaurant in the corridor.",
  coordinates: { lat: 41.9260, lng: -87.6990 },
  tags: ["place", "restaurant", "food", "tier_3"],
  related_entities: ["Aurelia Okafor-Strand"],
  frequented_by: ["Logan Square adventurers", "Control-relinquishers", "Toast enthusiasts (one memorable night)"]
});

writePlace({
  name: "The Corridor",
  aliases: ["Vending Corridor", "Corridor"],
  description: "A stretch of hallway in a Shelf residential tower in Humboldt Park that contains twenty-three vending machines, each stocked by a different person, each selling a different food item. One machine sells tamales. Another sells dumplings. Another sells calorie bars. Another sells, inexplicably, cupcakes. Over the course of six years, The Corridor evolved from a convenience into a community — residents gather in the hallway to eat, talk, trade, and argue about whose machine is best. There are no seats, so people sit on the floor, on overturned crates, on each other. The hallway smells like twenty-three different cuisines simultaneously. Nobody planned The Corridor. It planned itself.",
  coordinates: { lat: 41.9030, lng: -87.7010 },
  tags: ["place", "street_vendor", "food", "tier_1"],
  related_entities: [],
  frequented_by: ["Humboldt Park Shelf residents", "Vending machine operators", "Corridor socializers"]
});

writePlace({
  name: "The Accidental",
  aliases: ["Accidental", "Accident"],
  description: "A soup kitchen in Pilsen that was never meant to be a restaurant but became one anyway. Mama Grace Lindqvist-Adebayo started making soup for her neighbors during a particularly harsh winter in 2193. The winter ended. The soup didn't. Demand grew. Neighbors started contributing ingredients. Someone built a proper kitchen. Someone else built tables. Mama Grace kept cooking, and the quality of her cooking — West African soups, stews, jollof, egusi — attracted people from outside the Shelf who were willing to pay. Now it operates on a dual system: free for anyone who can't pay, Φ10-15 for anyone who can. The paying customers fund the free meals. It's the best restaurant in Pilsen and also a soup kitchen, and Mama Grace sees no contradiction in this.",
  coordinates: { lat: 41.8550, lng: -87.6580 },
  tags: ["place", "restaurant", "food", "tier_1"],
  related_entities: ["Mama Grace Lindqvist-Adebayo"],
  frequented_by: ["Pilsen Shelf residents (free)", "Circuit-tier food enthusiasts (paying)", "Everyone in between"]
});

writePlace({
  name: "Open Hand",
  aliases: ["Open Hand Kitchen"],
  description: "A pay-what-you-can restaurant in Bridgeport that serves a daily three-course meal at whatever price the diner decides it's worth. The suggested price is Φ12. Some people pay Φ50. Some people pay nothing. The food is consistently good — chef-owner Esperanza Lindqvist-Okafor is a trained professional who left the Circuit tier to cook for everyone, not just those who could afford her. The menu is simple: a soup, a main, a dessert. The soup is always excellent. The main rotates between vat-protein preparations. The dessert is a single cookie that is, every single day, perfect. Open Hand loses money every month and survives on donations from people who ate there once and couldn't forget it.",
  coordinates: { lat: 41.8390, lng: -87.6500 },
  tags: ["place", "restaurant", "food", "tier_2"],
  related_entities: ["Esperanza Lindqvist-Okafor"],
  frequented_by: ["Bridgeport residents of all tiers", "People who pay forward", "Cookie devotees"]
});

writePlace({
  name: "Line 7 Bar",
  aliases: ["Line 7", "Maglev Bar"],
  description: "A bar built inside a decommissioned maglev car that has been placed on a short section of track on Chicago's South Side and runs back and forth — a four-minute journey each direction — while passengers drink. The car seats eighteen at a bar that runs its length, and the bartender makes cocktails while the car moves, which requires a particular talent for pouring in motion. Cocktails are Φ12. The food is limited to what can be prepared in a moving vehicle: charcuterie boards, cheese plates, and a selection of small bites that don't require cooking. The experience of drinking a martini while traveling at low speed through the South Side's industrial landscape is unique in the corridor. Owner Kofi Lindqvist-Petersen considers the motion the secret ingredient.",
  coordinates: { lat: 41.8100, lng: -87.6250 },
  tags: ["place", "bar", "food", "nightlife", "tier_3"],
  related_entities: ["Kofi Lindqvist-Petersen"],
  frequented_by: ["Cocktail adventurers", "Motion enthusiasts", "People who enjoy drinking on trains"]
});

writePlace({
  name: "Sunset Karaoke & Kitchen",
  aliases: ["Sunset KK", "Sunset"],
  description: "A karaoke bar and restaurant in Albany Park that serves Filipino-Korean fusion food alongside private karaoke rooms. The food — sisig fried rice (Φ12), kimchi pancake (Φ10), adobo-marinated vat-chicken wings (Φ14) — is designed for sharing and for eating between songs. The karaoke rooms are Φ25/hour and come with a call button for food and drink orders. The fusion concept comes from the owners, a married couple — Cris Lindqvist-Santos (Filipino heritage) and Min-Ji Okafor-Kim (Korean heritage) — who discovered their culinary traditions blended better than anyone expected.",
  coordinates: { lat: 41.9690, lng: -87.7240 },
  tags: ["place", "restaurant", "food", "nightlife", "tier_2"],
  related_entities: ["Cris Lindqvist-Santos", "Min-Ji Okafor-Kim"],
  frequented_by: ["Albany Park residents", "Karaoke enthusiasts", "Filipino-Korean fusion converts"]
});

writePlace({
  name: "The Greenhouse",
  aliases: ["Greenhouse Restaurant"],
  description: "A restaurant built inside an actual greenhouse in a vacant lot in Garfield Park, where the food grows around you while you eat. The greenhouse produces vegetables year-round, and the menu is whatever is ripe. Some weeks there's an abundance of tomatoes and everything involves tomatoes. Some weeks it's greens. The unpredictability is the concept. A meal is Φ15-20. Chef-owner Adama Okafor-Lindqvist planted the greenhouse five years ago as a food security project and accidentally created a restaurant when she started cooking what grew. The dining tables sit between the grow beds. You can watch your salad's siblings growing while you eat.",
  coordinates: { lat: 41.8800, lng: -87.7180 },
  tags: ["place", "restaurant", "food", "tier_2"],
  related_entities: ["Adama Okafor-Lindqvist"],
  frequented_by: ["Garfield Park residents", "Urban agriculture enthusiasts", "People who want to meet their food's family"]
});

writePlace({
  name: "Frequency",
  aliases: ["Freq"],
  description: "A restaurant and sound bar in Milwaukee's Bay View where the dining experience is synchronized to a live DJ set. Each course is timed to a specific track, and the food is designed to complement the music: a heavy bass drop accompanies the richest course, a ethereal ambient section pairs with the lightest. The five-course dinner with synchronized sound is Φ55. Without the sound sync (bar seating, a la carte), dishes are Φ14-22. Chef Nkem Lindqvist-Okafor and DJ Astra (Astra Osei-Strand) designed the experience together and consider sound a seasoning that most restaurants neglect.",
  coordinates: { lat: 43.0080, lng: -87.9000 },
  tags: ["place", "restaurant", "food", "nightlife", "tier_3"],
  related_entities: ["Nkem Lindqvist-Okafor", "Astra Osei-Strand"],
  frequented_by: ["Bay View's music scene", "Synesthetic diners", "People who eat with their ears"]
});

writePlace({
  name: "The Map Room",
  aliases: ["Map Room"],
  description: "A restaurant and bar in Wicker Park where the walls, tables, and ceiling are covered in maps — historical maps of Chicago, the Great Lakes, the corridor, the world as it was and the world as it is. The food is 'cartographic cuisine' — dishes named after and inspired by the places they represent. The 'Okinawa' is a seaweed-wrapped vat-fish rice bowl (Φ16). The 'Lagos' is a jollof rice plate (Φ14). The 'Buenos Aires' is grilled vat-steak with chimichurri (Φ20). Owner Zara Lindqvist-Okafor was a cartographer before she was a restaurateur and considers every meal a journey.",
  coordinates: { lat: 41.9090, lng: -87.6780 },
  tags: ["place", "restaurant", "food", "nightlife", "tier_2"],
  related_entities: ["Zara Lindqvist-Okafor"],
  frequented_by: ["Wicker Park regulars", "Map enthusiasts", "Geographical eaters"]
});

writePlace({
  name: "Candlewick",
  aliases: ["Candlewick Dining"],
  description: "A restaurant in Green Bay that operates entirely by candlelight — no electric lighting whatsoever. The darkness is not total (unlike Noir Dining) but soft, warm, and deliberately intimate. The food is comfort cuisine elevated: braised vat-short ribs (Φ28), wild mushroom risotto (Φ22), a chocolate fondant for dessert (Φ14). The candlelight changes how food looks — colors soften, edges blur, and the focus shifts from appearance to taste and smell. Owner Nana Okafor-Strand lit the first candle when the building's electrical system failed and never turned the power back on because the food tasted better in the dark.",
  coordinates: { lat: 44.5120, lng: -88.0140 },
  tags: ["place", "restaurant", "food", "tier_3"],
  related_entities: ["Nana Okafor-Strand"],
  frequented_by: ["Green Bay date-night couples", "Candlelight romantics", "People who prefer their food slightly mysterious"]
});

writePlace({
  name: "The Butcher's Wake",
  aliases: ["Butcher's Wake"],
  description: "A restaurant in Back of the Yards — Chicago's historic meatpacking district — that commemorates the neighborhood's slaughterhouse past with a menu built around what replaced it. Every protein is vat-grown, and each dish is named after a historical aspect of the meatpacking industry: 'The Line' is a sausage sampler (Φ16). 'The Block' is a vat-prime cut with roasted marrow (Φ32). 'The Floor' is — against all instinct — the best dish: a slow-braised offal stew that uses vat-grown organ meats nobody else bothers with. Chef-owner Dayo Lindqvist-Ochoa considers the restaurant a wake for real meat and a celebration of what's possible without it.",
  coordinates: { lat: 41.8100, lng: -87.6560 },
  tags: ["place", "restaurant", "food", "tier_3"],
  related_entities: ["Dayo Lindqvist-Ochoa"],
  frequented_by: ["Back of the Yards residents", "Food historians", "Offal enthusiasts"]
});

writePlace({
  name: "The Echo Cellar",
  aliases: ["Echo Cellar", "Echo"],
  description: "A wine bar and small-plates restaurant in a former bomb shelter beneath a Cicero apartment building. The cellar was built in the 2090s during a period of geopolitical anxiety, never used for its intended purpose, and rediscovered in 2195 by owner Ama Lindqvist-Johansson, who cleaned it out and filled it with wine. The acoustics are extraordinary — the concrete walls create an echo that makes whispered conversations audible across the room, which is either charming or a privacy nightmare depending on what you're discussing. The wine list is forty bottles deep. The small plates — cheese, charcuterie, olives, bread — are simple and perfect at Φ10-18.",
  coordinates: { lat: 41.8460, lng: -87.7530 },
  tags: ["place", "bar", "food", "nightlife", "tier_3"],
  related_entities: ["Ama Lindqvist-Johansson"],
  frequented_by: ["Cicero's wine curious", "Atmosphere seekers", "People who don't mind being overheard"]
});

writePlace({
  name: "Noodle Train",
  aliases: ["The Train"],
  description: "A noodle restaurant in a decommissioned CTA train car permanently parked in a Lawndale lot. The car has been gutted and rebuilt as a twelve-seat noodle counter, with the kitchen occupying what was the conductor's area. Chef Yuki Lindqvist-Asante serves ramen, udon, and soba through a window that was once an emergency exit. The noodles are handmade, the broths are serious, and the experience of slurping ramen in a train car that will never move again has a melancholy that the food transcends. A bowl is Φ10-14. The car's original route number — Line 54 — is still visible on the exterior, and regulars call the restaurant by the line number as often as by its name.",
  coordinates: { lat: 41.8690, lng: -87.7200 },
  tags: ["place", "restaurant", "food", "tier_2"],
  related_entities: ["Yuki Lindqvist-Asante"],
  frequented_by: ["Lawndale residents", "Transit nostalgia seekers", "Noodle enthusiasts"]
});

writePlace({
  name: "Moonlight Market",
  aliases: ["Moon Market"],
  description: "A monthly night market that sets up on the lakefront in Kenosha on the full moon, featuring twenty to thirty food vendors selling everything from synth-noodles to premium sushi. The market runs from sunset to 2 AM and has become a corridor-wide event, drawing visitors from Chicago and Milwaukee. Vendor prices range from Φ3 for street food to Φ30 for premium items. The market was started by a collective of Kenosha food vendors who decided that the full moon was the only marketing hook they needed. They were right. On clear nights, the market is lit by the moon and by string lights, and the lakefront becomes the best restaurant in Kenosha — one with fifty kitchens and no walls.",
  coordinates: { lat: 42.5850, lng: -87.8150 },
  tags: ["place", "street_vendor", "food", "nightlife", "tier_2"],
  related_entities: [],
  frequented_by: ["Kenosha residents", "Corridor food tourists", "Full moon enthusiasts"]
});

writePlace({
  name: "The Refectory",
  aliases: ["Refectory"],
  description: "A communal dining hall in a former monastery in Fond du Lac, where fifty strangers sit at a single table and eat the same meal. There is no menu. There is no choice. Whatever the kitchen produces — typically a four-course meal of surprisingly high quality — is what you eat. A meal is Φ18. The dining is communal in the strictest sense: plates are shared, bread is passed, wine is poured for you by the person next to you. Owner-operator Frere Okafor-Beaumont is a former Benedictine monk who left his order but kept its philosophy of community through shared meals. The Refectory is not a restaurant — it's an argument that eating together is more important than eating well, though it achieves both.",
  coordinates: { lat: 43.7740, lng: -88.4450 },
  tags: ["place", "restaurant", "food", "tier_2"],
  related_entities: ["Frere Okafor-Beaumont"],
  frequented_by: ["Fond du Lac residents", "Community seekers", "People willing to eat with strangers"]
});

writePlace({
  name: "Glitch Kitchen",
  aliases: ["Glitch"],
  description: "A restaurant in Milwaukee's Walker's Point where everything is deliberately wrong in carefully controlled ways. The menu is printed backwards. The courses are served in reverse order (dessert first, appetizer last). The plates are the wrong size for the food. The drinks arrive before you order them (the staff predicts based on observation). The food itself is excellent — Φ20-30 per person — and prepared by Chef Nkem Okafor-Eriksson, who considers the deliberate breaking of dining conventions a form of artistic practice. Some diners find it delightful. Others find it infuriating. Nkem considers both reactions successful.",
  coordinates: { lat: 43.0250, lng: -87.9130 },
  tags: ["place", "restaurant", "food", "tier_3"],
  related_entities: ["Nkem Okafor-Eriksson"],
  frequented_by: ["Walker's Point diners", "Chaos enthusiasts", "People who enjoy being wrong-footed"]
});

// ═══════════════════════════════════════════════════════════════════════════════
// Summary
// ═══════════════════════════════════════════════════════════════════════════════

// ═══════════════════════════════════════════════════════════════════════════════
// ADDITIONAL VENUES — Filling to 200
// ═══════════════════════════════════════════════════════════════════════════════

// 3 more $ Tier 1

writePlace({
  name: "Salt Lick Cart",
  aliases: ["Salt Lick"],
  description: "A cart in Hammond that sells nothing but salted vat-jerky strips for Φ1.50 each. The jerky is thin, aggressively salted, and chewy enough to keep your jaw busy for twenty minutes per strip. The operator, Blessing Okafor-Strand, calls it 'the working man's gum' and sells about two hundred strips a day to factory workers who need something to chew during shifts that don't allow meal breaks. The salt content is medically inadvisable. The customer loyalty is absolute.",
  coordinates: { lat: 41.5810, lng: -87.5040 },
  tags: ["place", "street_vendor", "food", "tier_1"],
  related_entities: ["Blessing Okafor-Strand"],
  frequented_by: ["Hammond factory workers", "Jerky enthusiasts", "People who need to chew"]
});

writePlace({
  name: "Five-Grain Alley",
  aliases: ["Grain Alley", "FGA"],
  description: "A porridge and grain bowl stand in an alley behind a West Garfield Park housing block. Operated by a collective of five women who each bring a different grain — oats, rice, millet, barley, and quinoa — and cook them into porridge bowls with whatever toppings are available. A bowl is Φ2. The grains are real, sourced from agricultural zone surplus at near-cost. The collective formed because each woman alone couldn't afford enough grain to cook for her family, but together they could afford enough to cook for fifty. Mathematics as mutual aid.",
  coordinates: { lat: 41.8810, lng: -87.7260 },
  tags: ["place", "street_vendor", "food", "tier_1"],
  related_entities: [],
  frequented_by: ["West Garfield Park families", "Morning commuters", "People who believe in collective grain"]
});

writePlace({
  name: "Sardine Tin",
  aliases: ["Tin"],
  description: "A canned fish vendor in a Green Bay alley who buys expired-label (but safe) tinned fish from freight surplus and sells them for Φ1 each with a slice of synth-bread. The cans come from everywhere — sardines, mackerel, tuna, herring — and you don't choose, you get whatever's on top. Operator Kweku Lindqvist-Johansson considers it a lottery system, and regulars have developed strong opinions about which fish is the 'win.' The bread is an afterthought. The fish is the gamble.",
  coordinates: { lat: 44.5100, lng: -88.0190 },
  tags: ["place", "street_vendor", "food", "tier_1"],
  related_entities: ["Kweku Lindqvist-Johansson"],
  frequented_by: ["Green Bay Shelf residents", "Tinned fish gamblers", "People who don't mind mystery protein"]
});

// 17 more $$$ Tier 3-4

writePlace({
  name: "Lantern & Loom",
  aliases: ["L&L"],
  description: "A tapas and cocktail bar in the West Loop where the decor changes seasonally — the space is redesigned by a different local artist each quarter, and the menu adapts to match the aesthetic. The current installation is fiber art, and the tapas lean toward textured, layered preparations: crispy vat-pork belly with apple slaw (Φ16), layered beet and goat cheese terrine (Φ14). Cocktails are Φ14-18. Owner Zuri Lindqvist-Okafor considers a static restaurant a dead one and treats change as the only constant worth investing in.",
  coordinates: { lat: 41.8840, lng: -87.6460 },
  tags: ["place", "bar", "food", "nightlife", "tier_3"],
  related_entities: ["Zuri Lindqvist-Okafor"],
  frequented_by: ["West Loop art crowd", "Tapas enthusiasts", "Quarterly interior design tourists"]
});

writePlace({
  name: "Hawthorn",
  aliases: ["Hawthorn Restaurant"],
  description: "A New American restaurant in Winnetka serving a prix fixe dinner (Φ65 for four courses) that changes weekly and emphasizes seasonal ingredients from North Shore hydroponic farms. The duck breast with cherry glaze (Φ32 a la carte) is a returning favorite. The butternut squash bisque with brown butter croutons (Φ16) is the kind of soup that makes you reconsider what soup can be. Chef-owner Nia Okafor-Lindqvist trained in a Spire kitchen and brings that precision to a neighborhood restaurant that doesn't require Spire income to enjoy.",
  coordinates: { lat: 42.1080, lng: -87.7360 },
  tags: ["place", "restaurant", "food", "tier_3"],
  related_entities: ["Nia Okafor-Lindqvist"],
  frequented_by: ["North Shore diners", "Prix fixe enthusiasts", "Weekly returnees"]
});

writePlace({
  name: "Salt & Ember",
  aliases: ["S&E"],
  description: "A modern barbecue restaurant in the West Loop that applies fine-dining technique to smoked meats. The vat-brisket is smoked for eighteen hours over post oak, rested for four, then sliced tableside. A brisket plate is Φ28. The smoked vat-short rib, glazed with soy and black garlic, is Φ32. The cocktail menu features smoked spirits — bourbon infused with applewood smoke, mezcal with mesquite. Chef-owner Darnell Okafor-Lindqvist considers barbecue America's greatest culinary contribution and treats it with the reverence of a classical tradition, because it is one.",
  coordinates: { lat: 41.8860, lng: -87.6510 },
  tags: ["place", "restaurant", "food", "tier_3"],
  related_entities: ["Darnell Okafor-Lindqvist"],
  frequented_by: ["West Loop meat enthusiasts", "Smoked cocktail seekers", "Barbecue classicists"]
});

writePlace({
  name: "Azalea",
  aliases: ["Azalea Dining"],
  description: "A Korean fine-dining restaurant in Streeterville serving traditional Korean court cuisine — the elaborate, multi-dish meals once served to Korean royalty — adapted for GLMZ ingredients. A royal table setting (Φ60 per person) includes twelve banchan, a main protein, soup, rice, and dessert. The kimchi is fermented in traditional onggi pots kept in a climate-controlled room. Chef-owner Ji-Yeon Lindqvist-Park considers Korean court cuisine the most sophisticated dining tradition in the world and has spent fifteen years convincing diners she's right.",
  coordinates: { lat: 41.8950, lng: -87.6210 },
  tags: ["place", "restaurant", "food", "tier_4"],
  related_entities: ["Ji-Yeon Lindqvist-Park"],
  frequented_by: ["Streeterville's fine dining crowd", "Korean cuisine enthusiasts", "Court cuisine curious"]
});

writePlace({
  name: "The Apothecary",
  aliases: ["Apothecary"],
  description: "A cocktail bar and restaurant in Milwaukee's East Side designed to look and feel like a nineteenth-century apothecary — the cocktails are served in measuring glasses and beakers, the menu lists drinks as 'prescriptions,' and the ingredients are displayed in labeled jars behind the bar. The food is 'medicinal comfort': bone broth with turmeric and ginger (Φ14), grilled cheese with three-year cheddar and tomato soup (Φ18), and a chicken pot pie (Φ22) that is prescribed for 'general malaise.' Cocktails are Φ14-18. Owner Nkem Svensson-Osei considers comfort food literally therapeutic and runs the restaurant accordingly.",
  coordinates: { lat: 43.0580, lng: -87.8900 },
  tags: ["place", "bar", "food", "nightlife", "tier_3"],
  related_entities: ["Nkem Svensson-Osei"],
  frequented_by: ["Milwaukee's East Side regulars", "Comfort food seekers", "Prescription cocktail enthusiasts"]
});

writePlace({
  name: "Nomad Kitchen",
  aliases: ["Nomad"],
  description: "A pop-up restaurant in Chicago that changes location every two weeks, operating from borrowed kitchens, event spaces, warehouses, and occasionally someone's apartment. Chef Kosi Lindqvist-Okafor announces each location forty-eight hours in advance via encrypted message to a subscriber list. The menu changes with the location — a warehouse dinner might be rustic and smoky (Φ35), an apartment dinner intimate and French (Φ45). The unpredictability is deliberate: Kosi believes restaurants should be events, not addresses, and that food tastes different depending on where you eat it.",
  coordinates: { lat: 41.8950, lng: -87.6530 },
  tags: ["place", "restaurant", "food", "tier_3"],
  related_entities: ["Kosi Lindqvist-Okafor"],
  frequented_by: ["Pop-up chasers", "Encrypted message subscribers", "People who enjoy logistical dining"]
});

writePlace({
  name: "The Bone Room",
  aliases: ["Bone Room"],
  description: "A restaurant in Pilsen that specializes in bone marrow, bone broth, and bone-adjacent preparations. Roasted vat-bone marrow with chimichurri on toast (Φ18). Forty-eight-hour bone broth ramen (Φ22). Bone marrow butter on grilled bread (Φ12). The restaurant's walls display animal bones — real ones, sourced from natural history museum deaccessioning — creating a decor that is either educational or macabre. Chef-owner Obiora Svensson-Lindqvist considers bones the most undervalued ingredient in cooking and has built an entire restaurant to prove it.",
  coordinates: { lat: 41.8570, lng: -87.6640 },
  tags: ["place", "restaurant", "food", "tier_3"],
  related_entities: ["Obiora Svensson-Lindqvist"],
  frequented_by: ["Pilsen foodies", "Bone marrow enthusiasts", "People untroubled by decorative skeletons"]
});

writePlace({
  name: "Meridian Social Club",
  aliases: ["MSC", "Social Club"],
  description: "A members-only restaurant and bar in Hyde Park that operates as a cooperative — membership is Φ50/month, which covers access to a communal kitchen, dining room, and bar stocked by the members themselves. Twice a week, a different member cooks dinner for the group. The quality varies wildly. Some members are trained chefs. Some members are enthusiastic amateurs. One member is legendarily bad but so earnest that nobody has the heart to remove him from the rotation. The cooperative model means the best meals cost nothing beyond the membership, and the worst meals cost nothing beyond your dignity. It's the most egalitarian dining experience in the corridor.",
  coordinates: { lat: 41.7950, lng: -87.5920 },
  tags: ["place", "restaurant", "food", "tier_3"],
  related_entities: [],
  frequented_by: ["Hyde Park intellectuals", "Cooperative members", "People who enjoy culinary Russian roulette"]
});

writePlace({
  name: "Ashwood",
  aliases: ["Ashwood Restaurant"],
  description: "A wood-focused restaurant in Naperville where every dish is prepared using a different wood — ashwood for the fish, cherrywood for the duck, hickory for the pork, applewood for the dessert. The wood selection is not decorative; each wood imparts a distinct smoke and flavor profile that Chef Adaeze Eriksson-Okafor has spent years calibrating. A four-course dinner (Φ55) progresses through four woods, and the flavor journey from ash's clean smoke to apple's sweet finish is deliberate and compelling. The firewood is displayed in the dining room like a wine collection.",
  coordinates: { lat: 41.7730, lng: -88.1490 },
  tags: ["place", "restaurant", "food", "tier_3"],
  related_entities: ["Adaeze Eriksson-Okafor"],
  frequented_by: ["Naperville diners", "Wood-smoke enthusiasts", "Flavor progression seekers"]
});

writePlace({
  name: "Ember Social",
  aliases: ["Ember Social"],
  description: "A communal grilling restaurant in Waukesha where diners share a massive central grill and cook their own food from a selection of prepared ingredients. Vat-steaks, marinated vat-chicken, seasoned vegetables, and sauces are provided; you select what you want, bring it to the grill, and cook it yourself alongside strangers doing the same thing. The experience is Φ30 per person for unlimited selections. Servers circulate with sides and drinks. Owner Obinna Eriksson-Chen designed it as a social experiment: put strangers around a fire with food, and community happens. He was right.",
  coordinates: { lat: 43.0140, lng: -88.2330 },
  tags: ["place", "restaurant", "food", "tier_3"],
  related_entities: ["Obinna Eriksson-Chen"],
  frequented_by: ["Waukesha groups and families", "Communal dining enthusiasts", "People who like cooking their own food"]
});

writePlace({
  name: "Silk Road",
  aliases: ["Silk Road Restaurant"],
  description: "A Central Asian restaurant in Albany Park serving Uzbek, Tajik, and Uyghur dishes that almost nobody else in the corridor makes. Plov (pilaf with vat-lamb and carrots) is Φ20. Manti (large steamed dumplings with vat-beef) are Φ18. Lagman (hand-pulled noodles in a spiced tomato-lamb broth) is Φ16. Chef-owner Alisher Okafor-Nazarov spent three years in VR simulations of Samarkand and Tashkent marketplaces to learn techniques that few living people in the GLMZ practice. The bread — round, stamped, baked in a tandoor — is Φ4 and worth the trip alone.",
  coordinates: { lat: 41.9700, lng: -87.7220 },
  tags: ["place", "restaurant", "food", "tier_3"],
  related_entities: ["Alisher Okafor-Nazarov"],
  frequented_by: ["Albany Park residents", "Central Asian cuisine seekers", "Bread enthusiasts"]
});

writePlace({
  name: "The Roost",
  aliases: ["Roost"],
  description: "A rotisserie restaurant in Ravenswood that does one thing with total commitment: rotisserie vat-chicken. The birds rotate on a wall of spits behind the counter, dripping fat onto potatoes roasting below. A half chicken with roasted potatoes and salad is Φ22. A whole chicken to-go is Φ34. The chicken is brined, herb-rubbed, and roasted until the skin crackles and the meat falls from the bone. Chef-owner Ama Lindqvist-Petersen considers the rotisserie the most honest cooking method and the whole chicken the most democratic meal — it feeds a family, it's the same from any angle, and it doesn't pretend to be anything other than what it is.",
  coordinates: { lat: 41.9740, lng: -87.6720 },
  tags: ["place", "restaurant", "food", "tier_3"],
  related_entities: ["Ama Lindqvist-Petersen"],
  frequented_by: ["Ravenswood families", "Rotisserie enthusiasts", "People buying dinner for four"]
});

writePlace({
  name: "Basalt",
  aliases: ["Basalt Restaurant"],
  description: "A volcanic-themed restaurant in the South Loop that cooks on heated basalt stones brought to the table. Diners place thin-sliced vat-wagyu, vat-seafood, and vegetables on the 400-degree stone and cook them in seconds. A stone-cooking set for two is Φ55. The experience is interactive, theatrical, and occasionally hazardous — the stones are genuinely hot, and first-timers learn quickly not to touch them. Chef-owner Yara Okafor-Lindqvist chose the concept because she wanted diners to have a relationship with heat, not just with food. The stones are imported basalt from a quarry in the Driftless Area and are replaced every hundred uses.",
  coordinates: { lat: 41.8660, lng: -87.6230 },
  tags: ["place", "restaurant", "food", "tier_3"],
  related_entities: ["Yara Okafor-Lindqvist"],
  frequented_by: ["South Loop diners", "Interactive dining enthusiasts", "People who like their food dangerous"]
});

writePlace({
  name: "La Fermata",
  aliases: ["Fermata"],
  description: "An Italian restaurant in Highland Park that is structured like a musical performance — five courses, each timed to a live chamber music quartet that plays between courses. The food is Italian-classical: burrata with heirloom tomato (Φ18), hand-rolled pappardelle with vat-boar ragu (Φ28), veal osso buco (vat, Φ34), tiramisu (Φ14). The five-course dinner with music is Φ80. Chef-owner Elena Lindqvist-Conti considers food and music parallel art forms that peak when performed together. The quartet — the Fermata Ensemble — has been performing at the restaurant since it opened and considers the kitchen their favorite concert hall.",
  coordinates: { lat: 42.1820, lng: -87.8000 },
  tags: ["place", "restaurant", "food", "tier_4"],
  related_entities: ["Elena Lindqvist-Conti", "Fermata Ensemble"],
  frequented_by: ["North Shore cultural dining crowd", "Classical music enthusiasts", "Italian food lovers"]
});

writePlace({
  name: "Stone & Thistle",
  aliases: ["Stone Thistle"],
  description: "A Scottish-inspired restaurant and whisky bar in Andersonville serving dishes from a culinary tradition that most diners in the corridor have never explored. Cullen skink (smoked fish chowder, Φ16). Haggis (vat-offal, traditional preparation, Φ18 — surprisingly popular). Scotch eggs with mustard (Φ12). The whisky list is forty bottles of single malt, each with tasting notes that owner Fiona Okafor-MacLeod writes herself with a poeticism that suggests whisky is not just a drink but a landscape. The dining room has stone walls, tartan upholstery, and a fireplace that burns peat on cold nights. The peat smell is the restaurant's secret weapon.",
  coordinates: { lat: 41.9790, lng: -87.6700 },
  tags: ["place", "pub", "food", "nightlife", "tier_3"],
  related_entities: ["Fiona Okafor-MacLeod"],
  frequented_by: ["Andersonville locals", "Whisky enthusiasts", "Haggis-curious diners"]
});

writePlace({
  name: "Indigo",
  aliases: ["Indigo Restaurant"],
  description: "A pan-African fine-dining restaurant in Bronzeville that serves dishes spanning the continent — Senegalese thieboudienne, Nigerian egusi, Ethiopian doro wot, Mozambican peri-peri chicken, South African bobotie — each prepared with a precision that honors the source and an ambition that elevates it. A five-course dinner is Φ60. Chef-owner Chidinma Lindqvist-Okafor considers African cuisine the most diverse and least understood culinary tradition on earth, and her restaurant exists to correct that. The space is warm, colorful, and decorated with textiles sourced from artisans across the African diaspora.",
  coordinates: { lat: 41.8230, lng: -87.6150 },
  tags: ["place", "restaurant", "food", "tier_4"],
  related_entities: ["Chidinma Lindqvist-Okafor"],
  frequented_by: ["Bronzeville's cultural dining scene", "Pan-African cuisine enthusiasts", "Diaspora diners"]
});

writePlace({
  name: "Undertide",
  aliases: ["Undertide Restaurant"],
  description: "A raw bar and crudo restaurant on the Racine lakefront specializing in raw and minimally cooked preparations of lake fish and vat-grown seafood. Lake perch crudo with yuzu and shiso (Φ18). Vat-tuna tartare with sesame and avocado (Φ22). A raw oyster flight from three different Great Lakes colonies (Φ28). Chef-owner Solange Lindqvist-Nakamura treats raw preparation as the highest form of respect for an ingredient — no heat to hide behind, no sauce to mask mistakes. The restaurant is all glass and pale wood, and the lake is visible from every seat.",
  coordinates: { lat: 42.7280, lng: -87.7790 },
  tags: ["place", "restaurant", "food", "tier_3"],
  related_entities: ["Solange Lindqvist-Nakamura"],
  frequented_by: ["Racine lakefront diners", "Raw fish enthusiasts", "Crudo devotees"]
});

console.log('\n══════════════════════════════════════');
console.log('RESTAURANTS GENERATED');
console.log('Written: ' + written);
console.log('Skipped: ' + skipped);
console.log('══════════════════════════════════════');
