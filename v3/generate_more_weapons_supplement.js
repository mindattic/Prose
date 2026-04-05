const fs = require('fs');
const path = require('path');
const outDir = path.join(__dirname, '..', 'engine_data', 'weaponry');

const weapons = [
  {
    name: "Street Custom 'Molotov Standard' Incendiary Bottle",
    type: "weapon", aliases: ["Molotov", "Fire Bottle", "Cocktail", "Street Fire"],
    category: "improvised", manufacturer: "Street Custom",
    description: "The oldest improvised weapon still in active use — a glass bottle filled with flammable liquid with a cloth wick. In 2200, the Molotov has evolved: Tier 1 chemists mix industrial solvents with gelling agents to create sticky napalm variants that burn hotter, longer, and cling to surfaces. The weapon remains effective against vehicles, barricades, and personnel because fire does not care about armor ratings or augmentation levels. It is the great equalizer of asymmetric conflict.",
    specifications: "fuel: Gelled industrial solvent mix\nburn temperature: 800-1200°C depending on formulation\nburn duration: 30-90 seconds\nsplash radius: 2-3 meters\nweight: 0.5-1 kg\ncost: Φ2-10 in materials\nconstruction time: 5 minutes",
    tier_availability: "Tier 1+", legality: "Prohibited — improvised incendiary",
    street_price: "Φ2-10",
    base_technologies: ["Basic combustion chemistry", "Gelling agent formulation"],
    story_hooks: ["A coordinated Molotov attack using a new adhesive gel formulation has set an entire corporate checkpoint ablaze — the gel cannot be extinguished with water and sticks to riot shields.", "A chemist in Tier 1 has developed a Molotov formula that burns with a specific color based on chemical additives — gangs are using color-coded fire as territorial signals visible across districts."]
  },
  {
    name: "Arcturus Defense Solutions Concussive Breach Charge CBC-3 'Knocker'",
    type: "weapon", aliases: ["Knocker", "CBC-3", "Door Buster", "Hard Knock"],
    category: "explosive", manufacturer: "ARCTURUS DEFENSE SOLUTIONS",
    description: "A shaped explosive charge designed specifically for breaching reinforced doors and walls without producing lethal fragmentation on the far side. The CBC-3 focuses its blast energy directionally into a narrow cone that defeats the structural integrity of the target barrier while minimizing overpressure on the breach side. The charge uses a water-tamped explosive that converts blast energy into a hydraulic ram effect, punching through doors and walls with less collateral risk than conventional explosives.",
    specifications: "charge type: Shaped water-tamped directional\nbreach capability: Reinforced steel doors, concrete block walls\nfragmentation: Minimal — water tamping absorbs fragments\noverpressure: Reduced by 70% vs conventional breach charges\nweight: 0.8 kg per charge\nplacement: Magnetic adhesion to target surface\ndetonation: Manual, timed, or remote",
    tier_availability: "Tier 3+", legality: "Licensed — tactical entry teams",
    street_price: "Φ1,500 per charge",
    base_technologies: ["Shaped explosive focusing", "Water-tamped blast direction", "Low-fragmentation breach engineering"],
    story_hooks: ["CBC-3 charges were used to breach a vault but the water tamping failed — the resulting fragmentation killed a hostage on the other side of the door.", "A stolen shipment of CBC-3 charges has given a criminal organization military-grade breaching capability that no commercial door or wall can resist."]
  },
  {
    name: "Vespid Dynamics Neural Feedback Dart NFD-2 'Scream'",
    type: "weapon", aliases: ["Scream", "NFD-2", "Pain Dart", "Feedback Shot"],
    category: "exotic", manufacturer: "VESPID DYNAMICS",
    description: "A pneumatic dart weapon that delivers a neural interface exploit payload on penetration, hijacking the target's pain processing and amplifying all sensory input to maximum intensity. The victim experiences every sensation — light, sound, touch, temperature — as agonizing pain for approximately 2 minutes. The dart carries a micro-transmitter that injects the exploit through the wound channel into any nearby neural interface hardware. Against unaugmented targets, the dart is merely a sharp object. Against augmented targets, it is a precision agony weapon.",
    specifications: "propulsion: Pneumatic, 140 m/s\nrange: 5-30 meters\npayload: Neural interface pain amplification exploit\neffect duration: 2 minutes\neffect: All sensory input registered as maximum pain\nmagazine: 4 darts\nweight: 1.2 kg\nacoustic signature: 32 dB",
    tier_availability: "Tier 4+", legality: "Prohibited — neural warfare weapon",
    street_price: "Φ16,000 launcher, Φ1,200 per dart",
    base_technologies: ["Neural interface exploit delivery", "Pain pathway amplification", "Pneumatic precision dart systems"],
    story_hooks: ["An NFD-2 dart was used during an interrogation — the 2-minute pain cycle broke the subject's resistance permanently, and they now experience phantom pain episodes triggered by any strong sensation.", "Modified NFD-2 darts have appeared that deliver a pleasure amplification exploit instead of pain — the resulting euphoria is so intense that victims become addicted to being shot."]
  },
  {
    name: "Tessera Industries Optical Disruptor OD-4 'Strobe'",
    type: "weapon", aliases: ["Strobe", "OD-4", "Flash Gun", "Eye Killer"],
    category: "energy", manufacturer: "TESSERA INDUSTRIES",
    description: "A handheld directed energy device that emits a rapidly cycling pattern of high-intensity light at frequencies calculated to induce seizure-like neural disruption in human visual processing. The OD-4 projects a beam of strobing light that cycles through specific frequencies known to cause photosensitive responses — disorientation, nausea, loss of motor control, and in approximately 3% of targets, full tonic-clonic seizures. The beam is effective against both augmented and unaugmented targets because it exploits fundamental biological visual processing rather than electronic systems.",
    specifications: "beam type: High-intensity cycling visible light\nfrequency: Calculated photosensitive disruption pattern\neffective range: 5-50 meters directional\neffect: Disorientation, nausea, motor loss, 3% seizure risk\npower: Rechargeable capacitor, 30 seconds continuous\nweight: 0.4 kg\nform factor: Pistol-sized handheld\ncountermeasure: Light-filtering eyewear or closed eyes",
    tier_availability: "Tier 2+", legality: "Licensed — riot control",
    street_price: "Φ3,500",
    base_technologies: ["Photosensitive frequency calculation", "High-intensity directed light projection", "Neural visual disruption patterns"],
    story_hooks: ["An OD-4 was used in a crowded venue and triggered seizures in eleven people — the operator did not know or care about the 3% seizure risk in a crowd of 400.", "A modified OD-4 has been tuned to a frequency that specifically disrupts the visual processing of Axiom-model neural interfaces, causing permanent damage to the optical enhancement firmware."]
  },
  {
    name: "Street Custom 'Mercy' Veterinary Tranquilizer Gun",
    type: "weapon", aliases: ["Mercy", "Tranq Gun", "Animal Control", "Sleeper"],
    category: "improvised", manufacturer: "Street Custom",
    description: "A commercially available veterinary tranquilizer rifle modified for anti-personnel use by loading darts with human-dosed sedatives instead of animal compounds. The weapon fires pneumatic darts capable of delivering a fast-acting sedative that renders a human-sized target unconscious within 30-60 seconds depending on body mass and augmentation level. The tranquilizer rifle is legally available for purchase in most jurisdictions as an animal control tool, making acquisition trivially easy. The sedative compounds are sourced from veterinary supply or synthesized from commercially available precursors.",
    specifications: "propulsion: Compressed CO2, 90 m/s\nrange: 10-50 meters\nsedative: Modified veterinary compound, human-dosed\nonset: 30-60 seconds\neffect duration: 2-4 hours depending on dose\nmagazine: 5 darts\nweight: 3.2 kg\nacoustic signature: 45 dB",
    tier_availability: "Tier 1+", legality: "Legal as veterinary tool — illegal with human-dosed loads",
    street_price: "Φ300 rifle, Φ20-50 per dart",
    base_technologies: ["Pneumatic dart delivery", "Sedative compound dosing", "Modified veterinary equipment"],
    story_hooks: ["A series of abductions in Tier 2 all involve victims who remember nothing between a sharp sting and waking up hours later in an unfamiliar location — the dart wounds are consistent with modified veterinary equipment.", "A street medic has been using Mercy darts as an improvised anesthetic for field surgery in Tier 1 — the dosing is imprecise and dangerous, but the alternative is operating on conscious patients."]
  }
];

function toFileName(name) {
  return name.toLowerCase().replace(/['']/g, '').replace(/[^a-z0-9]+/g, '_').replace(/^_|_$/g, '') + '.json';
}

let written = 0, skipped = 0;
for (const w of weapons) {
  const fname = toFileName(w.name);
  const fpath = path.join(outDir, fname);
  if (fs.existsSync(fpath)) { skipped++; continue; }
  fs.writeFileSync(fpath, JSON.stringify(w, null, 2) + '\n');
  written++;
}
console.log(`Weapons supplement: wrote ${written}, skipped ${skipped}`);
console.log(`Total weapon files now: ${fs.readdirSync(outDir).length}`);
