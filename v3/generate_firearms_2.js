const fs = require('fs');
const path = require('path');
const crypto = require('crypto');

const OUTPUT_DIR = path.join(__dirname, '..', 'engine', 'data', 'weaponry');

if (!fs.existsSync(OUTPUT_DIR)) {
  fs.mkdirSync(OUTPUT_DIR, { recursive: true });
}

const existingFiles = new Set(fs.readdirSync(OUTPUT_DIR));

function slugify(str) {
  return str
    .toLowerCase()
    .replace(/['']/g, '')
    .replace(/[^a-z0-9]+/g, '_')
    .replace(/^_+|_+$/g, '')
    .slice(0, 80);
}

function writeEntity(entity) {
  const shortName = entity.name.slice(0, 60);
  const filename = slugify(shortName) + '.json';
  if (existingFiles.has(filename)) {
    console.log(`SKIP (exists): ${filename}`);
    return false;
  }
  const filepath = path.join(OUTPUT_DIR, filename);
  fs.writeFileSync(filepath, JSON.stringify(entity, null, 2), 'utf-8');
  existingFiles.add(filename);
  console.log(`WROTE: ${filename}`);
  return true;
}

function id() {
  return crypto.randomBytes(16).toString('hex');
}

// ═══════════════════════════════════════════════════════
// ASSAULT RIFLES (25)
// ═══════════════════════════════════════════════════════
const assaultRifles = [
  {
    id: id(),
    name: "Arcturus Defense Solutions AR-7 'Mandate'",
    type: "weapon",
    aliases: ["Mandate", "AR-7", "The Corpo Standard"],
    category: "assault_rifle",
    description: "The standard-issue assault rifle of Arcturus Defense Solutions' corporate security divisions, the AR-7 Mandate represents the baseline of modern military lethality. Chambered in 6.5mm caseless, the rifle feeds from a 40-round helical magazine and features an integrated BCI smart-link port that allows neural fire-control for sub-MOA accuracy at combat distances. The polymer-ceramic receiver shrugs off environmental abuse from arctic operations to equatorial humidity.\n\nThe Mandate earned its name from the corporate policy that made it the mandatory sidearm for all Arcturus ground personnel above E-3 clearance. Its ubiquity in corporate conflict zones has made it one of the most recognized weapons on Meridian 88, and surplus units flood grey markets whenever Arcturus rotates inventory cycles. Street-modified Mandates with disabled IFF transponders are a common sight in freelancer arsenals.\n\nCultural significance runs deep — owning a Mandate signals either corporate affiliation or the connections to acquire military hardware. In the lower tiers, a clean Mandate commands respect and suspicion in equal measure.",
    manufacturer: "ARCTURUS DEFENSE SOLUTIONS",
    tier_availability: "Tier 3+",
    legality: "Restricted — corporate-issued, grey market available",
    base_technologies: ["BCI smart-link integration", "Caseless ammunition system", "Polymer-ceramic composite receiver"],
    specifications: "caliber: 6.5mm caseless\neffective_range: 550m\nrate_of_fire: 750 rpm (cyclic)\nmagazine_capacity: 40 rounds (helical)\nweight: 3.4 kg",
    tactical_use: "The AR-7 excels as a general-purpose combat rifle across all engagement distances inside 600 meters. Its BCI integration allows trained operators to achieve precision fire without traditional optics, while the caseless ammunition system eliminates extraction failures and reduces carried weight. Corporate fireteams rely on the Mandate as their backbone weapon, supplementing with specialist platforms as needed.",
    cultural_context: "The Mandate is the face of corporate military power. Seeing a column of Arcturus security carrying AR-7s is a common sight in contested economic zones, and the weapon's silhouette has become shorthand for corporate enforcement in street art and propaganda. Surplus Mandates are prized on the grey market — a clean unit with working BCI link fetches Φ4,500 or more.",
    known_users: ["Arcturus Corporate Security", "Meridian 88 PMC contractors", "Tier 3+ freelancers"],
    story_hooks: [
      "A shipment of 200 Mandates with disabled IFF transponders has gone missing from an Arcturus depot — someone inside is arming an insurgency.",
      "A freelancer discovers their grey-market Mandate still has an active Arcturus tracking beacon embedded in the receiver."
    ],
    ammunition_type: ["6.5mm caseless standard", "6.5mm caseless AP"],
    tags: ["weapon", "assault_rifle", "corporate", "military", "BCI", "smart-link", "caseless", "tier 3"]
  },
  {
    id: id(),
    name: "Tessera TAR-12 'Consensus'",
    type: "weapon",
    aliases: ["Consensus", "TAR-12", "The Vote"],
    category: "assault_rifle",
    description: "Tessera's flagship assault rifle integrates their proprietary distributed targeting AI, which networks multiple TAR-12 units into a cooperative fire-control system. When a squad carries Consensus rifles, each weapon's BCI link shares targeting data, automatically deconflicting fire lanes and prioritizing threats based on collective sensor input. The result is a squad that shoots with the coordination of a single intelligence.\n\nThe TAR-12 fires 5.8mm polymer-tipped rounds from a 35-round box magazine, with a secondary electromagnetic acceleration rail that can be toggled for armor-piercing velocity at the cost of increased power cell drain. The rifle's modular chassis accepts Tessera's ecosystem of smart attachments, from thermal imaging foregrips to predictive recoil compensation stocks.\n\nThe Consensus is expensive — a full squad kit runs upward of Φ80,000 — but organizations that field it report 40% improvement in engagement efficiency. Critics call it a crutch that degrades individual marksmanship. Operators call it winning.",
    manufacturer: "TESSERA",
    tier_availability: "Tier 4+",
    legality: "Restricted — licensed military and premium security",
    base_technologies: ["Distributed targeting AI", "BCI squad-link networking", "Electromagnetic acceleration rail", "Modular smart-attachment interface"],
    specifications: "caliber: 5.8mm polymer-tipped\neffective_range: 500m (standard), 650m (EM-accelerated)\nrate_of_fire: 800 rpm (cyclic)\nmagazine_capacity: 35 rounds\nweight: 3.8 kg (base)",
    tactical_use: "The TAR-12 transforms squad tactics from individual marksmanship into networked lethality. The distributed AI automatically assigns targets, calls out flanking threats, and coordinates suppressive fire without verbal communication. In urban environments where communication is jammed, the weapon-to-weapon mesh network operates on encrypted short-range protocols that are extremely difficult to intercept.",
    cultural_context: "The Consensus represents Tessera's philosophy that technology should amplify collective human capability rather than replace it. Owning one without the squad network is like owning half a weapon — the rifle functions independently, but its true potential requires the full ecosystem. This has created a dependency model that critics liken to corporate lock-in disguised as tactical superiority.",
    known_users: ["Tessera Rapid Response Teams", "Elite PMC units", "Tier 4+ corporate security details"],
    story_hooks: [
      "A hacker has found a way to inject false targeting data into the TAR-12's mesh network, turning a squad's coordinated fire against friendly positions.",
      "A black-market dealer is selling individual TAR-12s stripped of their networking capability — but the distributed AI still phones home to Tessera."
    ],
    ammunition_type: ["5.8mm polymer-tipped", "5.8mm AP sabot"],
    tags: ["weapon", "assault_rifle", "corporate", "AI", "networked", "BCI", "smart-link", "Tessera", "tier 4"]
  },
  {
    id: id(),
    name: "Crucible Industries Forge Rifle FR-9 'Ironmonger'",
    type: "weapon",
    aliases: ["Ironmonger", "FR-9", "The Forge"],
    category: "assault_rifle",
    description: "Crucible Industries built the FR-9 for environments that destroy lesser weapons. The Ironmonger's monolithic steel-ceramic receiver is milled from a single block of composite material, eliminating weak points at joins and seams. It chambers the heavy 7.62mm caseless round and delivers it with a long-stroke gas piston system that cycles reliably through sand, mud, chemical contamination, and temperature extremes from -40C to 65C.\n\nThe FR-9 lacks the smart features of its Tessera and Arcturus competitors. There is no BCI link, no targeting AI, no electromagnetic acceleration. What it offers is mechanical perfection — a weapon that fires when the trigger is pulled, every time, in any condition. The iron sights are tritium-illuminated. The magazine is a standard 30-round steel box. The charging handle is oversized for gloved or prosthetic hands.\n\nCrucible markets the Ironmonger to frontier security forces, mining operations, and independent militias who operate beyond reliable maintenance infrastructure. It has become the weapon of choice for operators who distrust networked systems and prefer the certainty of analog reliability.",
    manufacturer: "CRUCIBLE INDUSTRIES",
    tier_availability: "Tier 2+",
    legality: "Available — licensed civilian and security",
    base_technologies: ["Monolithic steel-ceramic composite", "Long-stroke gas piston", "Tritium-illuminated iron sights"],
    specifications: "caliber: 7.62mm caseless\neffective_range: 600m\nrate_of_fire: 650 rpm (cyclic)\nmagazine_capacity: 30 rounds\nweight: 4.1 kg",
    tactical_use: "The FR-9 is the weapon you bring when nothing else will work. Its heavier caliber provides superior barrier penetration and stopping power compared to intermediate rounds, and its mechanical simplicity means field repairs require basic tools and no diagnostic software. Operators sacrifice smart-link precision for the guarantee that the weapon functions regardless of EMP, jamming, or network compromise.",
    cultural_context: "The Ironmonger has a cult following among anti-corporate frontier communities and old-school operators who view BCI-linked weapons as surveillance tools with triggers. Crucible cultivates this identity deliberately — their marketing emphasizes independence, self-reliance, and freedom from corporate ecosystems. In Tier 1-2 settlements, the FR-9 is often the most advanced weapon available and the most trusted.",
    known_users: ["Frontier security forces", "Independent mining operations", "Anti-corporate militia groups", "Tier 2 settlement defense"],
    story_hooks: [
      "A Crucible Industries factory has been producing FR-9s with hidden RFID trackers at the request of a corponation that wants to map militia supply chains.",
      "An FR-9 recovered from a crime scene has serial numbers that trace back to a batch supposedly destroyed in a factory fire — someone faked the destruction records."
    ],
    ammunition_type: ["7.62mm caseless standard", "7.62mm caseless heavy"],
    tags: ["weapon", "assault_rifle", "analog", "reliable", "frontier", "Crucible", "tier 2"]
  },
  {
    id: id(),
    name: "Kang-Petrov Arms KPA-15 'Diaspora'",
    type: "weapon",
    aliases: ["Diaspora", "KPA-15", "The People's Rifle"],
    category: "assault_rifle",
    description: "The KPA-15 Diaspora is the most widely manufactured assault rifle on Meridian 88. Kang-Petrov Arms designed it for mass production — stamped steel components, minimal machining, and a modular architecture that allows regional factories to produce the weapon with varying levels of sophistication. The base model is a straightforward 5.56mm gas-operated rifle with polymer furniture and a 30-round magazine. Optional BCI-link modules snap into a rail-mounted interface port.\n\nProduction variants range from bare-bones militia grade (Φ800) to corporate-contract models with integrated optics and recoil compensation (Φ3,200). This range makes the Diaspora the most democratic weapon in production — it arms everyone from Tier 1 neighborhood watches to Tier 3 corporate auxiliary forces. Kang-Petrov licenses production to fourteen regional manufacturers, ensuring supply even when trade routes collapse.\n\nThe weapon's reliability is adequate rather than exceptional, but replacement parts are available everywhere. When a Diaspora breaks, you fix it with parts from any other Diaspora. This interchangeability is its greatest asset and the reason it has proliferated across every conflict zone on the planet.",
    manufacturer: "KANG-PETROV ARMS",
    tier_availability: "Tier 1+",
    legality: "Widely available — minimal restrictions in most zones",
    base_technologies: ["Stamped steel mass production", "Modular BCI-link interface", "Universal parts interchangeability"],
    specifications: "caliber: 5.56mm standard\neffective_range: 400m\nrate_of_fire: 700 rpm (cyclic)\nmagazine_capacity: 30 rounds\nweight: 3.2 kg",
    tactical_use: "The KPA-15 is a volume weapon — its tactical advantage is availability, not superiority. In protracted conflicts where supply chains matter more than individual engagements, the Diaspora's ubiquitous parts and ammunition keep forces armed when premium weapons sit idle waiting for proprietary components. Smart operators pair baseline Diasporas with aftermarket optics and BCI modules to achieve 80% of a premium rifle's capability at 25% of the cost.",
    cultural_context: "The Diaspora is the rifle of the masses. Its name reflects the weapon's presence in every displaced community, every refugee defense force, every neighborhood militia across Meridian 88. Kang-Petrov's decision to license production broadly has made them less profitable per unit but enormously influential — when people think 'rifle,' they think KPA-15. Street murals frequently feature the Diaspora's distinctive angular profile as a symbol of armed community self-defense.",
    known_users: ["Community defense militias", "Tier 1-2 security forces", "Corporate auxiliary units", "Refugee defense networks"],
    story_hooks: [
      "One of the fourteen licensed KPA-15 manufacturers has been producing weapons with deliberately weakened firing pins — the rifles function for approximately 200 rounds before catastrophic failure.",
      "A community militia discovers that the BCI-link modules on their Diasporas are transmitting location data to a corponation that plans to annex their territory."
    ],
    ammunition_type: ["5.56mm standard", "5.56mm tracer"],
    tags: ["weapon", "assault_rifle", "mass_production", "affordable", "ubiquitous", "militia", "tier 1"]
  },
  {
    id: id(),
    name: "Arcturus Defense Solutions MAR-8X 'Longbow Mk.II'",
    type: "weapon",
    aliases: ["Longbow Mark Two", "MAR-8X", "The Rail"],
    category: "assault_rifle",
    description: "The MAR-8X is the next evolution of Arcturus' magnetic accelerator rifle platform, upgrading the original Longbow with a dual-stage electromagnetic acceleration system that launches 4mm tungsten penetrators at hypersonic velocity. The projectiles carry no propellant — kinetic energy alone provides devastating terminal effects, punching through Level IV armor at 400 meters and maintaining lethal velocity beyond 800.\n\nPower consumption remains the platform's primary constraint. The MAR-8X draws from a belt-mounted capacitor pack that provides 60 shots before requiring a 90-second field recharge from any standard power source. The weapon is silent save for the supersonic crack of the projectile and a faint electromagnetic hum during the capacitor charge cycle. BCI integration is mandatory — the fire-control system requires neural input to manage the acceleration timing.\n\nArcturus restricts MAR-8X sales to Tier 4+ military contracts, but the original Longbow's proliferation means experienced technicians can sometimes upgrade older units to near-8X specifications. These bootleg conversions are dangerous — an improperly calibrated acceleration coil can detonate the capacitor pack.",
    manufacturer: "ARCTURUS DEFENSE SOLUTIONS",
    tier_availability: "Tier 4+",
    legality: "Highly restricted — military contract only",
    base_technologies: ["Dual-stage electromagnetic acceleration", "Tungsten penetrator ammunition", "Mandatory BCI fire-control", "Belt-mounted capacitor system"],
    specifications: "caliber: 4mm tungsten penetrator\neffective_range: 800m\nrate_of_fire: 120 rpm (semi-auto, capacitor-limited)\nmagazine_capacity: 60 penetrators per capacitor charge\nweight: 5.2 kg (rifle) + 1.8 kg (capacitor pack)",
    tactical_use: "The MAR-8X fills the role of a squad-level anti-armor rifle. Its tungsten penetrators defeat personal armor, light vehicle plating, and hardened cover that would stop conventional rounds. The near-silent firing signature makes it devastating in ambush scenarios. The mandatory BCI link means only neurally-augmented operators can field the weapon, creating a natural restriction on unauthorized use.",
    cultural_context: "The Longbow platform represents the cutting edge of personal electromagnetic weapons. Possessing an 8X variant signals either deep corporate connections or extremely dangerous black-market contacts. Bootleg Longbow conversions are a thriving underground industry — and a regular source of casualties when capacitor packs detonate during firing.",
    known_users: ["Arcturus Special Operations", "Tier 5 corporate strike teams", "Elite freelancers with military contacts"],
    story_hooks: [
      "A bootleg MAR-8X conversion detonated during a freelance operation, killing the operator. The capacitor pack was deliberately sabotaged — someone is assassinating people through their weapons.",
      "Arcturus is field-testing a MAR-8X variant that uses the operator's BCI to calculate ricochets off hard surfaces — corner-shooting with tungsten penetrators."
    ],
    ammunition_type: ["4mm tungsten penetrator", "4mm tungsten AP-incendiary"],
    tags: ["weapon", "assault_rifle", "electromagnetic", "railgun", "armor_piercing", "BCI", "tier 4"]
  },
  {
    id: id(),
    name: "Meridian Munitions MM-4 'Breadwinner'",
    type: "weapon",
    aliases: ["Breadwinner", "MM-4", "The Paycheck"],
    category: "assault_rifle",
    description: "Meridian Munitions designed the MM-4 specifically for the freelancer market — operators who need corporate-grade reliability without corporate-grade prices or corporate-grade surveillance. The Breadwinner chambers 6mm caseless in a bullpup configuration that keeps the barrel length at 450mm while maintaining an overall length under 700mm. A basic BCI interface provides aim-assist without the full neural handshake that premium weapons demand.\n\nThe MM-4's defining feature is its open-architecture accessory rail system, which accepts attachments from any manufacturer rather than locking users into a proprietary ecosystem. This makes it the preferred platform for operators who mix and match equipment from multiple sources. Meridian even publishes the rail specifications openly, encouraging third-party development.\n\nAt Φ2,800 for a standard unit, the Breadwinner occupies the sweet spot between the disposable KPA-15 and premium corporate rifles. It rewards skilled marksmanship without demanding neural augmentation, making it popular among operators who keep their BCI integration minimal.",
    manufacturer: "MERIDIAN MUNITIONS",
    tier_availability: "Tier 2+",
    legality: "Available — standard licensing",
    base_technologies: ["Open-architecture accessory system", "Bullpup caseless configuration", "Basic BCI aim-assist"],
    specifications: "caliber: 6mm caseless\neffective_range: 450m\nrate_of_fire: 720 rpm (cyclic)\nmagazine_capacity: 35 rounds\nweight: 3.0 kg",
    tactical_use: "The MM-4 is a workhorse for independent operators. Its compact bullpup layout excels in vehicle operations and urban environments where overall length matters. The open accessory system means operators can configure the weapon for any mission profile without buying into a single manufacturer's ecosystem. The basic BCI aim-assist provides a meaningful accuracy improvement without requiring deep neural integration.",
    cultural_context: "The Breadwinner's name is literal — it is the tool that puts food on the table for thousands of freelance operators across Meridian 88. Meridian Munitions has cultivated a reputation as the arms manufacturer that respects operator independence, and the MM-4 embodies that ethos. In freelancer bars and safe houses, the Breadwinner is the default rifle, as common and unremarkable as a work tool should be.",
    known_users: ["Independent freelancers", "Tier 2-3 security contractors", "Bounty hunters", "Caravan guards"],
    story_hooks: [
      "Meridian Munitions is being pressured by Arcturus to close their open-architecture specs. If they comply, thousands of freelancers lose their equipment ecosystem overnight.",
      "A modified Breadwinner recovered from an assassination has aftermarket parts from six different manufacturers — tracing the weapon means tracing six separate supply chains."
    ],
    ammunition_type: ["6mm caseless standard", "6mm caseless hollow-point"],
    tags: ["weapon", "assault_rifle", "freelancer", "open_architecture", "bullpup", "caseless", "tier 2"]
  },
  {
    id: id(),
    name: "Volkov-Saito Precision VSR-20 'Partisan'",
    type: "weapon",
    aliases: ["Partisan", "VSR-20", "The Insurgent's Friend"],
    category: "assault_rifle",
    description: "Volkov-Saito Precision built the VSR-20 for asymmetric warfare. The Partisan is a select-fire rifle chambered in 6.8mm caseless with a heavy barrel profile optimized for sustained accurate fire from fixed positions. Where other assault rifles prioritize mobility and rate of fire, the VSR-20 prioritizes first-round accuracy and sustained fire without thermal degradation.\n\nThe rifle features a liquid-cooled barrel jacket that circulates a thermal management fluid, allowing the VSR-20 to maintain accuracy through extended engagements that would warp a conventional barrel. A built-in bipod deploys from the forend, and the stock adjusts for length, cheek height, and cant angle to accommodate any shooter regardless of body type or prosthetic configuration.\n\nVolkov-Saito markets the Partisan to defensive security forces and territorial militias, but its true customer base is any group expecting to fight a sustained engagement against a superior force. The VSR-20 does not win firefights through volume — it wins through persistent, accurate, demoralizing fire that pins advancing forces and bleeds them at range.",
    manufacturer: "VOLKOV-SAITO PRECISION",
    tier_availability: "Tier 2+",
    legality: "Available — standard licensing",
    base_technologies: ["Liquid-cooled barrel system", "Adaptive ergonomic stock", "Heavy barrel sustained-fire profile"],
    specifications: "caliber: 6.8mm caseless\neffective_range: 600m\nrate_of_fire: 600 rpm (cyclic)\nmagazine_capacity: 25 rounds\nweight: 4.5 kg",
    tactical_use: "The VSR-20 excels in defensive engagements where sustained accuracy matters more than mobility. Its liquid-cooled barrel maintains sub-MOA accuracy through hundreds of rounds that would render conventional barrels inaccurate. Paired with a competent marksman and good cover, a single Partisan can suppress an advancing squad. The heavy caliber and accurate fire create a psychological deterrent beyond its physical lethality.",
    cultural_context: "The Partisan has become synonymous with community defense against corporate expansion. Its name invokes historical resistance movements, and Volkov-Saito has leaned into this identity. In Tier 1-2 communities facing corporate encroachment, the VSR-20 represents the ability to impose costs on aggression. Corporate security briefings list the Partisan as a primary threat indicator for organized local resistance.",
    known_users: ["Territorial defense militias", "Settlement security forces", "Anti-corporate resistance cells"],
    story_hooks: [
      "A Volkov-Saito engineer is secretly providing Partisan maintenance manuals and upgrade kits to resistance groups — the company officially denies involvement while quietly supporting their best customers.",
      "A settlement's defense depends on three Partisan operators holding a chokepoint. If any of them fall, the position is lost and the community is overrun."
    ],
    ammunition_type: ["6.8mm caseless match", "6.8mm caseless AP"],
    tags: ["weapon", "assault_rifle", "precision", "defensive", "sustained_fire", "resistance", "tier 2"]
  },
  {
    id: id(),
    name: "Tessera Adaptive Platform TAP-5 'Chameleon'",
    type: "weapon",
    aliases: ["Chameleon", "TAP-5", "The Shapeshifter"],
    category: "assault_rifle",
    description: "The TAP-5 Chameleon is Tessera's modular assault platform, designed to reconfigure between three operational modes without tools. The weapon's core receiver accepts barrel, stock, and feed assemblies that snap-lock into position, allowing an operator to convert from a compact 5.56mm carbine to a 7.62mm battle rifle to a 4.6mm high-velocity PDW configuration in under thirty seconds.\n\nEach configuration carries its own ballistic profile in the weapon's onboard computer, and the BCI smart-link automatically adjusts aim-assist parameters when the operator swaps modules. Tessera sells the TAP-5 as a system — the base receiver plus three conversion kits runs Φ12,000, but the logistics savings of carrying one weapon platform instead of three justify the cost for extended operations.\n\nThe Chameleon's weakness is the mechanical complexity of its quick-change system. Each locking interface is a potential failure point, and field conditions can foul the snap-lock mechanisms. Operators who trust the TAP-5 maintain it religiously. Those who don't carry a backup.",
    manufacturer: "TESSERA",
    tier_availability: "Tier 3+",
    legality: "Restricted — licensed security and military",
    base_technologies: ["Quick-change modular weapon system", "Auto-calibrating BCI ballistic profiles", "Snap-lock barrel/stock/feed assemblies"],
    specifications: "caliber: 5.56mm / 7.62mm / 4.6mm (configurable)\neffective_range: 350-600m (configuration dependent)\nrate_of_fire: 650-900 rpm (configuration dependent)\nmagazine_capacity: 20-40 rounds (configuration dependent)\nweight: 3.1-4.3 kg (configuration dependent)",
    tactical_use: "The TAP-5 provides mission flexibility that no single-caliber platform can match. Operators select their configuration based on expected engagement parameters — 5.56mm for urban patrol, 7.62mm for perimeter defense, 4.6mm for close protection details. The 30-second conversion time means reconfiguration happens during operational pauses rather than requiring return to an armory.",
    cultural_context: "The Chameleon embodies Tessera's design philosophy of adaptive technology. It appeals to operators who pride themselves on versatility and preparation. Critics argue the TAP-5 does three things adequately rather than one thing excellently, and in corporate security circles the debate between Chameleon generalists and dedicated-platform specialists is ongoing and occasionally heated.",
    known_users: ["Tessera corporate security", "Long-range patrol units", "Freelance operators on extended contracts"],
    story_hooks: [
      "A TAP-5 reconfiguration failed during a critical engagement — the snap-lock jammed between modes, leaving the operator with a non-functional weapon at the worst possible moment.",
      "Someone is selling counterfeit TAP-5 conversion kits that look identical to genuine Tessera modules but have subtly different tolerances that cause failures under stress."
    ],
    ammunition_type: ["5.56mm standard", "7.62mm caseless", "4.6mm high-velocity"],
    tags: ["weapon", "assault_rifle", "modular", "configurable", "Tessera", "BCI", "tier 3"]
  },
  {
    id: id(),
    name: "Hearthstone Firearms HF-30 'Homestead'",
    type: "weapon",
    aliases: ["Homestead", "HF-30", "The Porch Gun"],
    category: "assault_rifle",
    description: "Hearthstone Firearms caters to the civilian defense market with weapons that are deliberately simple, deliberately rugged, and deliberately affordable. The HF-30 Homestead is a semi-automatic rifle chambered in 5.56mm with a traditional layout, wooden furniture options, and no electronic components whatsoever. No BCI link. No smart-link. No onboard computer. Just a rifle.\n\nThe Homestead uses a conventional brass-cased cartridge rather than the caseless ammunition that dominates the military market. This is a deliberate choice — brass-cased 5.56mm is manufactured by dozens of small operations across Meridian 88 and remains available when caseless supply chains collapse. The rifle's manual of arms requires no technical training beyond basic firearms operation, making it accessible to communities without military experience.\n\nAt Φ600, the Homestead is the cheapest new-production rifle on the market. Hearthstone sells them in bulk to settlement cooperatives and community defense funds. The weapon won't impress anyone who has handled a Mandate or a Consensus, but it puts an accurate, reliable rifle in the hands of people who need one and can't afford anything else.",
    manufacturer: "HEARTHSTONE FIREARMS",
    tier_availability: "Tier 1+",
    legality: "Unrestricted — civilian grade",
    base_technologies: ["Conventional gas operation", "Brass-cased ammunition compatibility", "Zero-electronics design"],
    specifications: "caliber: 5.56mm brass-cased\neffective_range: 350m\nrate_of_fire: Semi-automatic only\nmagazine_capacity: 20 rounds\nweight: 3.6 kg",
    tactical_use: "The HF-30 is not a combat weapon — it is a defense weapon. Its semi-automatic operation and 20-round magazine discourage spray-and-pray while encouraging aimed fire. In the hands of a trained shooter it is accurate enough to deter opportunistic raiders and wildlife threats. Its zero-electronics design means it is immune to EMP, jamming, and any form of electronic warfare.",
    cultural_context: "The Homestead represents armed self-sufficiency at its most basic. Hearthstone's branding emphasizes community, family defense, and independence from corporate supply chains. In Tier 1 settlements, the HF-30 is often the only manufactured weapon available, and its wooden furniture gives it an anachronistic warmth that contrasts sharply with the polymer-ceramic aggression of corporate arms. People name their Homesteads.",
    known_users: ["Tier 1 settlement defenders", "Frontier homesteaders", "Community cooperatives", "Civilian self-defense"],
    story_hooks: [
      "A Tier 1 settlement has been ordered to surrender all weapons as a condition of corporate annexation. Their Homesteads are the only things standing between the community and absorption.",
      "Hearthstone's founder is dying and the company is being courted by Arcturus for acquisition — if the buyout succeeds, the cheapest rifle on the market disappears."
    ],
    ammunition_type: ["5.56mm brass-cased"],
    tags: ["weapon", "assault_rifle", "civilian", "affordable", "analog", "frontier", "Hearthstone", "tier 1"]
  },
  {
    id: id(),
    name: "Crucible Industries Storm Carbine SC-4 'Downpour'",
    type: "weapon",
    aliases: ["Downpour", "SC-4", "Storm Gun"],
    category: "assault_rifle",
    description: "The SC-4 Downpour is Crucible Industries' entry into the high-rate-of-fire carbine market, designed for shipboard security and close-quarters facility defense. Chambered in 4.6mm high-velocity caseless, the Storm Carbine fires at a blistering 1,100 rpm from a 50-round drum magazine. The small caliber and frangible ammunition are engineered to defeat soft body armor while fragmenting against bulkheads and hull plating, minimizing the risk of catastrophic breaches in pressurized environments.\n\nThe SC-4's recoil management system uses a counterweight bolt carrier that moves in opposition to the bolt, canceling felt recoil to a degree that allows controllable automatic fire from an unsupported standing position. BCI integration is optional — the weapon functions identically with or without a smart-link, though neural fire-control improves hit probability by approximately 15% at full auto.\n\nCrucible partnered with several orbital station operators and maritime security firms to develop the Downpour, and it shows in every design decision. Short overall length. Frangible-optimized barrel twist. Hull-safe ammunition. This is a weapon designed for fighting inside things you don't want to destroy.",
    manufacturer: "CRUCIBLE INDUSTRIES",
    tier_availability: "Tier 3+",
    legality: "Restricted — licensed security",
    base_technologies: ["Counterweight recoil cancellation", "Frangible ammunition optimization", "Hull-safe terminal ballistics"],
    specifications: "caliber: 4.6mm HV caseless frangible\neffective_range: 200m\nrate_of_fire: 1,100 rpm (cyclic)\nmagazine_capacity: 50 rounds (drum)\nweight: 2.8 kg",
    tactical_use: "The SC-4 dominates close-quarters engagements in confined spaces. Its extreme rate of fire and manageable recoil allow operators to saturate tight corridors and compartments with frangible projectiles that shred soft targets while sparing structural elements. Boarding actions, facility clearing, and shipboard defense are its primary roles. Beyond 200 meters the small frangible rounds lose effectiveness rapidly.",
    cultural_context: "The Downpour is a specialist weapon that has found an unexpected civilian following among Tier 3+ sport shooters who appreciate its controllability and the visceral experience of dumping 50 rounds in under three seconds. Crucible has capitalized on this with a civilian semi-auto variant, though the full-auto version remains restricted. In orbital communities, the SC-4 is standard issue for emergency response teams.",
    known_users: ["Orbital station security", "Maritime boarding teams", "Facility defense forces", "Crucible corporate security"],
    story_hooks: [
      "During a station emergency, the security team's SC-4 frangible rounds failed to fragment — someone loaded standard penetrator ammunition into the magazines, and every missed shot risks hull integrity.",
      "A pirate crew has modified their Downpours to fire armor-piercing rounds, negating the weapon's hull-safe design for use in aggressive boarding actions."
    ],
    ammunition_type: ["4.6mm HV caseless frangible", "4.6mm HV caseless standard"],
    tags: ["weapon", "assault_rifle", "CQB", "shipboard", "frangible", "high_rate", "Crucible", "tier 3"]
  },
  {
    id: id(),
    name: "Kang-Petrov Arms KPA-20E 'Thunderclap'",
    type: "weapon",
    aliases: ["Thunderclap", "KPA-20E", "The Budget Rail"],
    category: "assault_rifle",
    description: "Kang-Petrov's first electromagnetic accelerator rifle brings railgun technology to the mass market. The KPA-20E Thunderclap uses a single-stage magnetic acceleration system to fire 3mm steel-core projectiles at supersonic velocity. It lacks the power and range of Arcturus' MAR-8X, but at Φ5,500 it costs less than a quarter of the military platform and requires no BCI integration to operate.\n\nThe Thunderclap's capacitor system is integrated into the rifle's stock, providing 40 shots per charge. Recharging takes 120 seconds from a standard power outlet. The weapon has no moving bolt or gas system — projectiles are loaded from a gravity-fed hopper atop the receiver and accelerated through the barrel by electromagnetic coils. This mechanical simplicity means there are no extraction or feeding failures, though the hopper is vulnerable to contamination if left uncovered.\n\nCritics from the premium arms manufacturers dismiss the Thunderclap as a toy — its 3mm projectiles lack the penetration of larger tungsten penetrators, and the single-stage acceleration limits velocity. But for operators who want electromagnetic capability without corporate entanglement, the KPA-20E democratizes access to technology that was previously exclusive to military budgets.",
    manufacturer: "KANG-PETROV ARMS",
    tier_availability: "Tier 2+",
    legality: "Available — standard licensing",
    base_technologies: ["Single-stage electromagnetic acceleration", "Gravity-fed hopper magazine", "Integrated capacitor stock"],
    specifications: "caliber: 3mm steel-core projectile\neffective_range: 400m\nrate_of_fire: 200 rpm (capacitor-limited)\nmagazine_capacity: 40 projectiles per charge\nweight: 3.8 kg",
    tactical_use: "The KPA-20E provides electromagnetic capability at a fraction of military cost. While it cannot match the MAR-8X's penetration or range, the Thunderclap's steel-core projectiles defeat Level II body armor and most personal barriers. The lack of chemical propellant means no muzzle flash and minimal sound signature — the electromagnetic hum and supersonic crack are the only signatures. Useful for operators who need the advantages of EM acceleration without the expense.",
    cultural_context: "The Thunderclap represents Kang-Petrov's mission to democratize weapons technology. Corporate arms manufacturers have lobbied for restrictions on civilian electromagnetic weapons, arguing that the technology is inherently military-grade. Kang-Petrov's legal team has fought every restriction, positioning the KPA-20E as a test case for whether corponations can monopolize entire categories of arms technology.",
    known_users: ["Independent operators", "Tech-forward militia groups", "Tier 2-3 security contractors"],
    story_hooks: [
      "Arcturus Defense Solutions is funding a legal campaign to classify all electromagnetic weapons as military-exclusive technology — if they succeed, every KPA-20E owner becomes a felon overnight.",
      "A hacker collective has published modifications that double the Thunderclap's acceleration power. The mod works, but the capacitor stock has a 5% chance of catastrophic failure per shot."
    ],
    ammunition_type: ["3mm steel-core", "3mm tungsten-tipped"],
    tags: ["weapon", "assault_rifle", "electromagnetic", "railgun", "affordable", "Kang-Petrov", "tier 2"]
  },
  {
    id: id(),
    name: "Tessera Neural Assault System TNAS-1 'Puppeteer'",
    type: "weapon",
    aliases: ["Puppeteer", "TNAS-1", "Ghost Rifle"],
    category: "assault_rifle",
    description: "The TNAS-1 Puppeteer represents Tessera's most aggressive integration of neural technology with small arms. The weapon cannot be fired by trigger pull — it has no trigger. The Puppeteer fires exclusively through BCI neural command, with the operator's intent translated into firing decisions by an onboard neural interpretation engine. Think 'fire' and it fires. Think 'burst' and it fires three rounds. Think 'suppress' and it dumps a magazine at the designated area.\n\nThe rifle chambers 5.8mm caseless and features a free-floating barrel in a fully sealed receiver that eliminates environmental ingress. Without a trigger mechanism, trigger guard, or manual safety, the weapon's exterior is a smooth polymer shell with no external controls beyond a physical power switch. The BCI link handles everything — fire mode selection, round counting, malfunction diagnostics, and even thermal management alerts.\n\nThe Puppeteer is the most controversial weapon in production. Advocates praise its response time — neural firing commands bypass the mechanical delay of trigger pull, reducing time-to-fire by approximately 80 milliseconds. Critics argue that a weapon fired by thought is a weapon that can be fired by intrusive thoughts, hacked neural commands, or BCI malfunction. Tessera's liability waivers for the TNAS-1 are longer than the user manual.",
    manufacturer: "TESSERA",
    tier_availability: "Tier 4+",
    legality: "Restricted — requires BCI certification and psychological screening",
    base_technologies: ["Neural intent firing system", "Sealed triggerless receiver", "BCI neural interpretation engine", "Thought-command fire control"],
    specifications: "caliber: 5.8mm caseless\neffective_range: 500m\nrate_of_fire: 850 rpm (cyclic, neural-commanded)\nmagazine_capacity: 35 rounds\nweight: 3.1 kg",
    tactical_use: "The Puppeteer offers the fastest possible engagement time for any conventional firearm. The 80ms advantage over trigger-fired weapons is marginal in isolation but decisive in close-quarters engagements where reaction time determines survival. The sealed receiver and lack of external mechanisms make the weapon virtually maintenance-free in the field. However, any disruption to the operator's BCI link renders the weapon completely inoperable — it becomes an expensive club.",
    cultural_context: "The Puppeteer provokes visceral reactions. Neural-purists see it as the natural evolution of human-weapon integration. Traditional operators view it as a weapon that can be turned against its user by anyone with a BCI hack. The psychological screening requirement has created a secondary market for forged certification documents, and several incidents of accidental discharge by stressed operators have fueled regulatory campaigns.",
    known_users: ["Tessera elite security", "BCI-specialized operators", "Neural warfare units"],
    story_hooks: [
      "A Puppeteer operator's BCI was hacked during an engagement, causing the weapon to fire on friendly targets. The neural intrusion was so subtle the operator believed they were shooting at enemies.",
      "A black-market neural interface claiming to be Puppeteer-compatible is actually recording the operator's neural patterns for resale to a data broker."
    ],
    ammunition_type: ["5.8mm caseless standard", "5.8mm caseless subsonic"],
    tags: ["weapon", "assault_rifle", "neural", "BCI", "triggerless", "controversial", "Tessera", "tier 4"]
  },
  {
    id: id(),
    name: "Crucible Industries Ember Rifle ER-7 'Ashfall'",
    type: "weapon",
    aliases: ["Ashfall", "ER-7", "The Burner"],
    category: "assault_rifle",
    description: "The ER-7 Ashfall fires thermite-tipped 6.5mm rounds that ignite on impact, combining ballistic trauma with incendiary effect. Crucible Industries developed the platform for anti-materiel operations where targets include equipment caches, vehicle fuel systems, and fortified positions with combustible components. Each round carries a 0.3-gram thermite payload in the projectile tip that activates on deformation.\n\nThe rifle itself is a conventional gas-operated platform built on the proven FR-9 receiver with modifications to the barrel lining and chamber to resist the thermal signature of the incendiary ammunition. A BCI-linked thermal scope provides targeting data overlaid with flammability analysis of the target environment — the system identifies combustible materials and predicts fire spread patterns to maximize incendiary effect.\n\nThe Ashfall is devastating against unarmored targets and soft vehicles but less effective against modern armor, which is designed to resist thermal penetration. Its real value is psychological — a single thermite round igniting inside a defensive position creates panic disproportionate to the actual damage. Fire is a primal fear that no amount of training fully eliminates.",
    manufacturer: "CRUCIBLE INDUSTRIES",
    tier_availability: "Tier 3+",
    legality: "Restricted — incendiary weapons regulations apply",
    base_technologies: ["Thermite-tipped projectiles", "Thermal-resistant barrel lining", "BCI flammability analysis scope"],
    specifications: "caliber: 6.5mm thermite-tipped\neffective_range: 450m\nrate_of_fire: 600 rpm (cyclic)\nmagazine_capacity: 25 rounds\nweight: 4.0 kg",
    tactical_use: "The ER-7 excels in area denial and anti-materiel roles. Operators use the flammability analysis scope to identify high-value combustible targets — fuel storage, ammunition caches, communication equipment with flammable insulation — and deliver thermite rounds that create secondary fires. Against personnel, the psychological impact of incendiary ammunition frequently breaks defensive positions more effectively than superior volume of fire.",
    cultural_context: "Incendiary weapons carry a stigma that other weapons do not. The Ashfall's operators are viewed with unease even by their own allies, and using thermite rounds against personnel in civilian areas is considered a war crime by most governance frameworks. Crucible markets the ER-7 exclusively as an anti-materiel platform, but everyone knows what thermite does to people.",
    known_users: ["Anti-materiel specialists", "Sabotage teams", "Crucible demolition contractors"],
    story_hooks: [
      "An Ashfall operator accidentally ignited a chemical storage facility in a Tier 2 settlement, causing a fire that destroyed three city blocks. Crucible is trying to suppress the incident report.",
      "Someone is using stolen ER-7 ammunition in a conventional rifle to commit arsons that look like industrial accidents — the thermite residue is being missed by standard forensics."
    ],
    ammunition_type: ["6.5mm thermite-tipped", "6.5mm standard (compatible)"],
    tags: ["weapon", "assault_rifle", "incendiary", "thermite", "anti_materiel", "Crucible", "tier 3"]
  },
  {
    id: id(),
    name: "Arcturus Defense Solutions SAR-3 'Warden'",
    type: "weapon",
    aliases: ["Warden", "SAR-3", "The Guardian"],
    category: "assault_rifle",
    description: "The SAR-3 Warden is Arcturus' purpose-built law enforcement rifle, designed for corporate police forces operating in dense urban environments. The weapon features an integrated IFF transponder that cross-references targets against Arcturus' citizen database in real-time, providing color-coded BCI overlays that identify corporate employees, registered civilians, known offenders, and unidentified individuals. The fire-control system includes an optional compliance lock that prevents firing on targets tagged as corporate assets.\n\nChambered in 5.56mm caseless with low-velocity frangible rounds as the default load, the Warden prioritizes minimal collateral damage in populated areas. The barrel is ported for reduced muzzle velocity, and the onboard computer tracks every round fired with GPS coordinates, timestamp, and target bearing for post-incident review. Every trigger pull generates a record.\n\nThe Warden is the most surveilled weapon in production. Its operators cannot fire without creating an audit trail, cannot target corporate-flagged individuals without override authorization, and cannot disable the tracking systems without triggering an alert. For corporate police this represents accountability. For everyone else, it represents a weapon that tells its manufacturer who you shoot, where, and when.",
    manufacturer: "ARCTURUS DEFENSE SOLUTIONS",
    tier_availability: "Tier 3+",
    legality: "Restricted — law enforcement issue only",
    base_technologies: ["Real-time IFF citizen database", "BCI target identification overlay", "Compliance fire-control lock", "Full audit trail logging"],
    specifications: "caliber: 5.56mm caseless frangible\neffective_range: 300m\nrate_of_fire: 700 rpm (cyclic)\nmagazine_capacity: 30 rounds\nweight: 3.3 kg",
    tactical_use: "The SAR-3 is optimized for urban policing operations where collateral damage and civilian casualties create liability. The IFF system prevents friendly-fire incidents against corporate personnel, and the audit trail ensures every engagement is documented. In practice, the compliance lock creates a dangerous delay when an operator needs to engage a target near a corporate-flagged individual — the system requires manual override confirmation that costs critical seconds.",
    cultural_context: "The Warden embodies the surveillance state with a trigger. Corporate police carrying SAR-3s are sometimes called 'Wardens' themselves, and the weapon's data-collection capabilities have made it a symbol of corporate overreach. Activists argue that a weapon that identifies targets by social database is a weapon of social control. Arcturus argues it prevents exactly the kind of indiscriminate violence that gives weapons manufacturers a bad name.",
    known_users: ["Arcturus corporate police", "Licensed municipal security forces", "Corporate campus security"],
    story_hooks: [
      "The Warden's IFF database was updated with incorrect civilian tags — a protected witness was reclassified as a known offender, making them a valid target for every SAR-3 in the district.",
      "A former corporate police officer has a decommissioned SAR-3 with its compliance lock removed. The audit trail still works — but now it records for whoever hacked the upload destination."
    ],
    ammunition_type: ["5.56mm caseless frangible", "5.56mm caseless rubber (less-lethal)"],
    tags: ["weapon", "assault_rifle", "law_enforcement", "surveillance", "IFF", "audit_trail", "Arcturus", "tier 3"]
  },
  {
    id: id(),
    name: "Volkov-Saito Precision VS-44 'Fenris'",
    type: "weapon",
    aliases: ["Fenris", "VS-44", "Wolf Rifle"],
    category: "assault_rifle",
    description: "The VS-44 Fenris is a heavy assault rifle chambered in 8.6mm caseless, designed to bridge the gap between standard infantry rifles and crew-served weapons. At nearly five kilograms unloaded, the Fenris is too heavy for conventional patrol use but devastating in the hands of augmented operators with reinforced skeletal systems or powered exoframes. Its 8.6mm round delivers energy comparable to legacy .338 Lapua at half the range, shredding Level III armor and punching through light vehicle panels.\n\nVolkov-Saito designed the weapon for operators who are themselves weapons platforms — individuals whose physical augmentation allows them to handle recoil and weight that would be punishing for baseline humans. The stock interfaces with common spinal-mount weapon stabilization systems, and the BCI link includes a recoil-prediction algorithm that pre-tensions the operator's augmented musculature milliseconds before each shot.\n\nThe Fenris is not subtle. Its muzzle report is thunderous, its muzzle flash is visible in daylight, and its terminal effects are gratuitously destructive. Volkov-Saito sells it as a force multiplier for augmented combatants who need to deliver crew-served firepower from a man-portable platform.",
    manufacturer: "VOLKOV-SAITO PRECISION",
    tier_availability: "Tier 3+",
    legality: "Restricted — augmented operator certification required",
    base_technologies: ["Heavy-caliber caseless system", "Spinal-mount stabilization interface", "BCI recoil-prediction algorithm", "Augmented operator optimization"],
    specifications: "caliber: 8.6mm caseless\neffective_range: 700m\nrate_of_fire: 450 rpm (cyclic)\nmagazine_capacity: 20 rounds\nweight: 4.9 kg",
    tactical_use: "The Fenris turns a single augmented operator into a fire support element. Its 8.6mm round penetrates cover that stops intermediate calibers, and the recoil-prediction system maintains accuracy through sustained fire that would be impossible for unaugmented shooters. In urban combat, a Fenris operator can suppress hardened positions and engage light vehicles without requiring dedicated anti-materiel weapons. The weapon's weight and recoil make it impractical for baseline human use.",
    cultural_context: "The Fenris occupies an uncomfortable space in the augmentation debate. It is a weapon that requires augmentation to use effectively, making it a tool exclusively for the enhanced. This exclusivity appeals to augmented operators who view their modifications as competitive advantages, while baseline humans see it as another door closed by the augmentation gap. In some communities, carrying a Fenris is a statement: I am more than you.",
    known_users: ["Augmented PMC operators", "Exoframe-equipped security forces", "Heavy assault specialists"],
    story_hooks: [
      "A baseline human operator has been using a Fenris without augmentation, absorbing punishing recoil that is slowly destroying their shoulder and spine. They cannot afford the augmentations but need the weapon's firepower to survive.",
      "A series of vehicle ambushes using Fenris rifles points to an augmented operator gone rogue — but the ballistic signatures suggest the same weapon is appearing in multiple cities simultaneously."
    ],
    ammunition_type: ["8.6mm caseless", "8.6mm caseless AP"],
    tags: ["weapon", "assault_rifle", "heavy", "augmented", "high_caliber", "Volkov-Saito", "tier 3"]
  },
  {
    id: id(),
    name: "Meridian Munitions Compact Rifle MCR-2 'Errand'",
    type: "weapon",
    aliases: ["Errand", "MCR-2", "The Runner"],
    category: "assault_rifle",
    description: "The MCR-2 Errand is Meridian Munitions' ultracompact assault rifle, designed for couriers, drivers, and operators who need a rifle that disappears into a messenger bag. With the stock folded, the weapon measures 380mm overall — shorter than most submachine guns — yet fires the same 6mm caseless round as the larger MM-4 Breadwinner. Magazine compatibility between the two weapons is deliberate, allowing operators to standardize on a single ammunition type across their primary and backup weapons.\n\nThe Errand sacrifices barrel length for concealability, which reduces effective range to 250 meters and increases muzzle flash and report. A compact suppressor threaded to the barrel adds 120mm but brings both signatures down to manageable levels. The BCI aim-assist system is particularly valuable on the MCR-2, where the short sight radius would otherwise make iron-sight accuracy challenging.\n\nMeridian markets the Errand as a vehicle defense weapon and personal protection rifle, but its concealability makes it popular with operators who need to carry a rifle where rifles aren't welcome. In Tier 2-3 zones with weapons checkpoints, the MCR-2 passes for electronics equipment in a sufficiently cluttered bag.",
    manufacturer: "MERIDIAN MUNITIONS",
    tier_availability: "Tier 2+",
    legality: "Restricted — concealed weapon regulations apply",
    base_technologies: ["Ultracompact folding design", "Magazine compatibility with MM-4 platform", "Compact suppressor integration"],
    specifications: "caliber: 6mm caseless\neffective_range: 250m (unsuppressed), 220m (suppressed)\nrate_of_fire: 750 rpm (cyclic)\nmagazine_capacity: 35 rounds (MM-4 compatible)\nweight: 2.4 kg",
    tactical_use: "The MCR-2 fills the gap between a submachine gun and a full-size rifle. Its rifle-caliber round provides better terminal performance than pistol-caliber PDWs while its compact dimensions allow concealed carry in civilian environments. The suppressed configuration is a favorite of covert operators who need to engage targets without advertising their position. Magazine commonality with the MM-4 simplifies logistics for teams carrying both platforms.",
    cultural_context: "The Errand is the weapon of people who go places. Couriers, fixers, negotiators, and anyone whose job involves moving through spaces where visible weapons invite trouble. Its name reflects its intended user — someone running errands in dangerous territory who needs insurance that fits in a shoulder bag. In freelancer circles, drawing an MCR-2 from a messenger bag has become a cliched movie move, but it works.",
    known_users: ["Couriers and runners", "Covert operators", "Vehicle crews", "Personal protection details"],
    story_hooks: [
      "A courier's MCR-2 was scanned at a weapons checkpoint that supposedly couldn't detect the weapon — the scanner operator is taking bribes, but from whom?",
      "Someone has been planting MCR-2s in dead drops across a Tier 3 zone. The weapons are clean, loaded, and positioned at locations that suggest a planned coordinated attack."
    ],
    ammunition_type: ["6mm caseless standard", "6mm caseless subsonic"],
    tags: ["weapon", "assault_rifle", "compact", "concealed", "courier", "suppressed", "Meridian", "tier 2"]
  },
  {
    id: id(),
    name: "Crucible Industries Battle Rifle BR-11 'Tribunal'",
    type: "weapon",
    aliases: ["Tribunal", "BR-11", "Judge Gun"],
    category: "assault_rifle",
    description: "The BR-11 Tribunal is a 7.62mm battle rifle built for Crucible's corporate adjudication teams — security forces authorized to enforce contract disputes through direct action. The weapon's onboard system maintains a legal log that timestamps each engagement with the relevant contract clause being enforced, creating a chain of evidence that satisfies corporate arbitration courts. Every trigger pull is a legally documented act.\n\nMechanically, the Tribunal is a refined version of the FR-9 Ironmonger refit with precision components. The barrel is chrome-lined match-grade, the trigger is a two-stage unit adjustable from 1.5 to 3.5 kilograms, and the action is glass-bedded into the receiver for consistent accuracy. BCI integration provides legal overlay information — the operator sees contract boundaries, authorized engagement zones, and target authorization status in real-time.\n\nCrucible designed the Tribunal to make violence bureaucratic. Every aspect of the weapon exists to ensure that when force is applied, it is documented, justified, and legally defensible. This makes the BR-11 the most civilized instrument of brutality in production.",
    manufacturer: "CRUCIBLE INDUSTRIES",
    tier_availability: "Tier 3+",
    legality: "Restricted — corporate adjudication forces only",
    base_technologies: ["Legal engagement logging", "BCI contract-overlay system", "Match-grade precision components", "Arbitration-ready evidence chain"],
    specifications: "caliber: 7.62mm caseless\neffective_range: 650m\nrate_of_fire: 600 rpm (cyclic)\nmagazine_capacity: 20 rounds\nweight: 4.3 kg",
    tactical_use: "The Tribunal functions as both weapon and legal instrument. Its precision components deliver match-grade accuracy for engagements where clean, documented kills are required — a stray round into non-contracted areas creates legal liability. Operators are trained to fire only within authorized engagement zones, and the BCI overlay ensures they know exactly where those boundaries lie. The legal logging system has a secondary tactical benefit: it discourages unauthorized use, since every round is audited.",
    cultural_context: "The Tribunal represents the intersection of violence and bureaucracy that defines corporate sovereignty. In a world where contracts carry the force of law and enforcement is privatized, the BR-11 is the physical manifestation of corporate justice. Being targeted by a Tribunal operator means your death will be filed, reviewed, and archived. The weapon's nickname 'Judge Gun' captures its role perfectly — it doesn't just kill, it adjudicates.",
    known_users: ["Crucible adjudication teams", "Corporate enforcement specialists", "Contract dispute resolution forces"],
    story_hooks: [
      "A Tribunal operator discovers their legal overlay has been manipulated — the 'authorized targets' displayed in their BCI were actually civilians reclassified by a corrupt contract manager.",
      "The legal logs from a BR-11 recovered at a crime scene prove the killing was authorized under a contract that technically doesn't exist. Someone fabricated an entire corporate dispute to justify murder."
    ],
    ammunition_type: ["7.62mm caseless match", "7.62mm caseless standard"],
    tags: ["weapon", "assault_rifle", "legal", "adjudication", "precision", "corporate", "Crucible", "tier 3"]
  },
  {
    id: id(),
    name: "Kang-Petrov Arms KPA-8 'Solidarity'",
    type: "weapon",
    aliases: ["Solidarity", "KPA-8", "The Union Gun"],
    category: "assault_rifle",
    description: "The KPA-8 Solidarity is a select-fire assault rifle designed for organized labor defense forces — the armed wings of worker cooperatives and trade unions that protect their members against corporate strike-breaking operations. Chambered in 5.56mm, the Solidarity improves on the baseline KPA-15 Diaspora with a chrome-lined barrel, improved trigger group, and a ruggedized BCI interface that uses open-source targeting software rather than proprietary corporate code.\n\nKang-Petrov developed the Solidarity in partnership with several major worker collectives, incorporating feedback from operators who had been using modified Diasporas. The result is a rifle that costs Φ1,600 — twice the cheapest Diaspora variant but half the price of corporate alternatives — and delivers reliability that approaches military grade. The open-source BCI software is maintained by a community of volunteer developers and cannot be remotely disabled by any corporate entity.\n\nThe Solidarity's significance extends beyond its mechanical capabilities. It is the first weapon explicitly designed for labor defense, and its existence is a political statement. Kang-Petrov faced threats, sanctions, and a cyberattack on their manufacturing systems after announcing the platform. They shipped on schedule.",
    manufacturer: "KANG-PETROV ARMS",
    tier_availability: "Tier 1+",
    legality: "Available — standard licensing",
    base_technologies: ["Open-source BCI targeting software", "Community-maintained fire control", "Chrome-lined sustained fire barrel"],
    specifications: "caliber: 5.56mm standard\neffective_range: 420m\nrate_of_fire: 700 rpm (cyclic)\nmagazine_capacity: 30 rounds\nweight: 3.4 kg",
    tactical_use: "The KPA-8 provides organized defense forces with a reliable, maintainable platform that cannot be compromised through corporate software backdoors. The open-source BCI system eliminates the risk of remote kill-switches or surveillance through weapon-integrated software. In labor disputes that escalate to armed confrontation, the Solidarity ensures that the workers' weapons keep firing regardless of what the opposing corporation does to their networks.",
    cultural_context: "The Solidarity is a symbol of armed labor resistance. Its very existence challenges the corporate monopoly on organized violence, and Kang-Petrov's willingness to face corporate retaliation to produce it has elevated the company to near-mythical status among worker movements. Union halls prominently display Solidarity rifles, and the weapon's profile appears on labor movement flags and patches across Meridian 88.",
    known_users: ["Worker cooperative defense forces", "Trade union security", "Labor movement militias", "Community mutual defense organizations"],
    story_hooks: [
      "A major corponation has placed a bounty on the source code repository for the Solidarity's open-source BCI software — destroying it would leave thousands of rifles without fire-control updates.",
      "Kang-Petrov is secretly shipping Solidarity rifles to workers inside a corporate enclave preparing for a strike. If the shipment is intercepted, it will be treated as an act of war."
    ],
    ammunition_type: ["5.56mm standard", "5.56mm AP"],
    tags: ["weapon", "assault_rifle", "labor", "union", "open_source", "resistance", "Kang-Petrov", "tier 1"]
  },
  {
    id: id(),
    name: "Arcturus Defense Solutions Specter Rifle SR-6 'Whisper'",
    type: "weapon",
    aliases: ["Whisper", "SR-6", "Ghost Gun"],
    category: "assault_rifle",
    description: "The SR-6 Whisper is Arcturus' integrally suppressed assault rifle, designed for operations where sound discipline is paramount. Unlike rifles with detachable suppressors, the Whisper's barrel is permanently enclosed in a suppression shroud that bleeds propellant gases through a series of expansion chambers along the barrel's full length. Combined with subsonic 6.5mm ammunition, the weapon produces less noise than a closing car door.\n\nThe BCI integration includes a sound-profile analyzer that monitors the weapon's acoustic signature in real-time and alerts the operator if suppression efficiency degrades. The rifle also features a thermal masking system that circulates coolant through the suppression shroud, reducing the infrared signature of the heated barrel to near-ambient levels within seconds of firing.\n\nArcturus developed the Whisper for their deniable operations division — the arm of corporate security that conducts actions that officially never happened. The weapon's combination of acoustic and thermal suppression makes detection by surveillance systems extremely difficult. After an engagement, the SR-6 leaves minimal forensic signature — subsonic rounds fragment on impact, and the integrally suppressed design captures residue that would otherwise contaminate the shooter.",
    manufacturer: "ARCTURUS DEFENSE SOLUTIONS",
    tier_availability: "Tier 4+",
    legality: "Highly restricted — deniable operations authorization only",
    base_technologies: ["Integral suppression system", "Thermal signature masking", "BCI acoustic monitoring", "Forensic signature reduction"],
    specifications: "caliber: 6.5mm subsonic caseless\neffective_range: 300m\nrate_of_fire: 650 rpm (cyclic)\nmagazine_capacity: 25 rounds\nweight: 4.2 kg",
    tactical_use: "The SR-6 is optimized for covert engagements where discovery means mission failure. Its combined acoustic and thermal suppression defeats both human senses and electronic surveillance, allowing operators to engage targets in monitored environments without triggering alarms. The subsonic ammunition limits range and penetration compared to standard loads, but within its operational envelope the Whisper is nearly undetectable.",
    cultural_context: "The Whisper's existence is technically classified, though its use in enough deniable operations has made it an open secret. In intelligence circles, being told someone carries a Whisper is a warning: the person holding it operates outside normal rules of engagement. The weapon has no legitimate civilian or standard military application — it exists solely to kill quietly and leave no evidence.",
    known_users: ["Arcturus deniable operations", "Tier 5 intelligence operatives", "Corporate assassination specialists"],
    story_hooks: [
      "A Whisper was recovered from a crime scene that Arcturus claims is impossible — the weapon's serial number belongs to a unit that was supposedly destroyed in a decommissioning audit three years ago.",
      "An operator realizes their SR-6's BCI acoustic monitor has been recording and transmitting their location every time they fire — the weapon has been tracking its own user."
    ],
    ammunition_type: ["6.5mm subsonic caseless", "6.5mm subsonic frangible"],
    tags: ["weapon", "assault_rifle", "suppressed", "covert", "deniable", "stealth", "Arcturus", "tier 4"]
  },
  {
    id: id(),
    name: "Hearthstone Firearms Scout Rifle HSR-10 'Wanderer'",
    type: "weapon",
    aliases: ["Wanderer", "HSR-10", "Trail Gun"],
    category: "assault_rifle",
    description: "The HSR-10 Wanderer is a lightweight semi-automatic rifle designed for frontier scouts, surveyors, and travelers who need a weapon that handles both two-legged and four-legged threats without weighing them down. Chambered in 6.5mm brass-cased, the Wanderer offers better range and terminal performance than the company's 5.56mm Homestead while maintaining Hearthstone's signature no-electronics design philosophy.\n\nThe rifle weighs just 2.9 kilograms thanks to a skeletonized aluminum receiver and carbon-fiber-wrapped barrel. A fixed 4x optical scope provides magnification for medium-range engagements, and the smooth two-stage trigger delivers consistent pulls that reward patient marksmanship. The 15-round magazine keeps the profile slim for carry, and the weapon's overall length with folding stock collapsed fits comfortably in a backpack frame.\n\nHearthstone developed the Wanderer after surveying frontier operators who reported that existing rifles were either too heavy for extended foot travel, too dependent on electronics that failed in remote areas, or too expensive to risk in wilderness conditions. The HSR-10 answers all three complaints with a rifle that is light, analog, and priced at Φ900.",
    manufacturer: "HEARTHSTONE FIREARMS",
    tier_availability: "Tier 1+",
    legality: "Unrestricted — civilian grade",
    base_technologies: ["Skeletonized aluminum receiver", "Carbon-fiber barrel wrap", "Analog optical scope"],
    specifications: "caliber: 6.5mm brass-cased\neffective_range: 500m\nrate_of_fire: Semi-automatic only\nmagazine_capacity: 15 rounds\nweight: 2.9 kg",
    tactical_use: "The Wanderer is a traveling weapon. Its light weight and compact folding profile make it ideal for operators covering long distances on foot. The 6.5mm caliber provides enough energy for medium game and personnel threats at distances where 5.56mm begins to falter. The fixed optical scope is simple but effective, and the lack of electronics means the weapon is always ready regardless of power availability or electromagnetic environment.",
    cultural_context: "The Wanderer is the companion weapon of Meridian 88's frontier — carried by the scouts, traders, and wanderers who move between settlements through territory that belongs to no one. Hearthstone's marketing features actual frontier travelers rather than models, and the weapon has developed a romantic association with independence and open wilderness that its utilitarian design does little to discourage.",
    known_users: ["Frontier scouts", "Wilderness surveyors", "Long-distance traders", "Settlement outriders"],
    story_hooks: [
      "A frontier scout's Wanderer was found beside a trail with a full magazine and no sign of its owner. The scope is scratched as if something clawed at it.",
      "Hearthstone is sponsoring a cross-frontier endurance race where participants carry only a Wanderer and basic supplies. The race route passes through territory claimed by three hostile factions."
    ],
    ammunition_type: ["6.5mm brass-cased"],
    tags: ["weapon", "assault_rifle", "lightweight", "frontier", "scout", "analog", "Hearthstone", "tier 1"]
  },
  {
    id: id(),
    name: "Tessera Autonomous Rifle Platform TARP-3 'Delegate'",
    type: "weapon",
    aliases: ["Delegate", "TARP-3", "The Drone Rifle"],
    category: "assault_rifle",
    description: "The TARP-3 Delegate is an assault rifle designed to be wielded by humanoid combat drones rather than human operators. Tessera engineered every aspect of the weapon for machine operation — the grip pressure requirements exceed human hand strength, the trigger pull weight is set at 15 kilograms, and the manual of arms requires motor precision that only servos can deliver. A human can physically carry the Delegate but cannot effectively operate it.\n\nThe weapon fires 6mm caseless through a conventional gas system, but its fire-control is entirely machine-mediated. A hardened data port replaces the BCI link, connecting directly to the combat drone's targeting system. The Delegate achieves consistent sub-half-MOA accuracy at all ranges because the platform holding it does not breathe, flinch, or fatigue. Rate of fire is limited by the weapon's cyclic rate rather than the operator's trigger finger.\n\nTessera sells the TARP-3 exclusively as part of complete drone weapon systems, bundled with their combat automata. The weapon is useless without a drone, and the drone is less effective without its purpose-built weapon. This interdependency is deliberate — Tessera wants to sell platforms, not components.",
    manufacturer: "TESSERA",
    tier_availability: "Tier 4+",
    legality: "Restricted — autonomous weapons platform regulations",
    base_technologies: ["Machine-optimized ergonomics", "Hardened data port fire-control", "Servo-grade operational requirements"],
    specifications: "caliber: 6mm caseless\neffective_range: 550m\nrate_of_fire: 800 rpm (cyclic)\nmagazine_capacity: 40 rounds\nweight: 3.6 kg",
    tactical_use: "The TARP-3 delivers inhuman accuracy from an inhuman platform. Combat drones carrying Delegates provide persistent, tireless fire support without risk to human operators. The machine-optimized design prevents field capture and use by human adversaries, ensuring that even if a drone is disabled, its weapon cannot be turned against friendly forces. Drone-Delegate teams are typically deployed in fire-and-forget perimeter defense roles.",
    cultural_context: "The Delegate represents the dehumanization of combat taken to its logical extreme — a weapon that humans cannot use, carried by machines that cannot choose not to. Anti-autonomous weapons activists target the TARP-3 specifically because it is a rifle, the most human of weapons, redesigned to exclude humans entirely. Tessera's marketing avoids showing the Delegate in human hands, reinforcing the separation between people and the violence conducted on their behalf.",
    known_users: ["Tessera combat drone platforms", "Automated perimeter defense systems", "Corporate facility security drones"],
    story_hooks: [
      "A technician has modified a Delegate to be human-operable by reducing the trigger weight and adding a conventional grip. The weapon's machine-precision accuracy is lost, but now a human can carry a rifle that corporate databases say only drones can use.",
      "A combat drone carrying a Delegate has gone offline but continues to patrol its assigned route and engage targets. The drone's AI is damaged, and no one knows what its current targeting parameters are."
    ],
    ammunition_type: ["6mm caseless standard"],
    tags: ["weapon", "assault_rifle", "autonomous", "drone", "machine", "Tessera", "tier 4"]
  },
  {
    id: id(),
    name: "Volkov-Saito Precision VSX-7 'Strelok'",
    type: "weapon",
    aliases: ["Strelok", "VSX-7", "The Artisan"],
    category: "assault_rifle",
    description: "The VSX-7 Strelok is a hand-fitted competition and precision assault rifle that Volkov-Saito produces in limited runs of 200 units per year. Each rifle is assembled by a single gunsmith who signs the receiver, and the weapons are individually accuracy-tested to guarantee sub-quarter-MOA performance with match ammunition. The Strelok chambers 6.5mm caseless through a precision-lapped barrel with a match chamber cut to minimum tolerances.\n\nThe BCI integration on the VSX-7 goes beyond standard smart-link — the weapon's onboard processor builds a ballistic profile specific to each individual rifle, accounting for the unique harmonics of that particular barrel, action, and trigger. Over time, the system learns the rifle's behavior and provides corrections that make each Strelok more accurate the longer it is used. Volkov-Saito calls this 'weapon learning' and considers it their signature technology.\n\nAt Φ18,000, the Strelok is priced for professionals who measure their skill in fractions of an arc-minute. It is not a battlefield weapon — its tight tolerances make it sensitive to contamination, and its match chamber will not reliably feed anything except premium ammunition. But within its operating parameters, it is the most accurate assault rifle in production.",
    manufacturer: "VOLKOV-SAITO PRECISION",
    tier_availability: "Tier 4+",
    legality: "Available — premium licensing",
    base_technologies: ["Hand-fitted precision assembly", "Individual ballistic profiling", "Weapon learning AI", "Match-grade minimum-tolerance chamber"],
    specifications: "caliber: 6.5mm caseless match\neffective_range: 700m\nrate_of_fire: Semi-automatic (precision mode), 600 rpm (combat mode)\nmagazine_capacity: 20 rounds\nweight: 3.9 kg",
    tactical_use: "The Strelok excels in precision engagement roles where first-round accuracy at distance determines the outcome. Its weapon-learning system makes it increasingly effective in the hands of a consistent operator, rewarding disciplined marksmanship with corrections that compensate for environmental variables. The rifle is less suited to extended combat — its tight tolerances and sensitivity to contamination make it a liability in dirty, sustained engagements.",
    cultural_context: "Owning a Strelok signals mastery. The weapon's limited production and individual craftsmanship create a culture of ownership pride among operators who treat their rifles as partners rather than tools. Strelok owners often know their rifle's production number, their gunsmith's name, and their weapon's unique accuracy profile. In marksmanship circles, 'Strelok-grade' has become shorthand for the highest standard of precision.",
    known_users: ["Elite marksmen", "Competition shooters", "Precision freelancers", "Collector-operators"],
    story_hooks: [
      "A stolen Strelok's weapon-learning system has been tracking its new user's firing patterns. Volkov-Saito can identify the thief by their unique neural signature recorded through the BCI link.",
      "One of the 200 Streloks produced this year was assembled with a deliberate flaw by a disgruntled gunsmith. The barrel will catastrophically fail after approximately 500 rounds."
    ],
    ammunition_type: ["6.5mm caseless match"],
    tags: ["weapon", "assault_rifle", "precision", "limited_production", "hand_fitted", "Volkov-Saito", "tier 4"]
  },
  {
    id: id(),
    name: "Meridian Munitions Urban Carbine MUC-6 'Crosswalk'",
    type: "weapon",
    aliases: ["Crosswalk", "MUC-6", "Street Sweeper"],
    category: "assault_rifle",
    description: "The MUC-6 Crosswalk is Meridian Munitions' urban-optimized carbine, designed for the tight angles, short distances, and civilian density of city combat. The weapon features a short 280mm barrel with aggressive porting that reduces muzzle flash to near-invisible levels in daylight conditions. The integrated BCI link includes a threat-discrimination system that highlights hostile targets in the operator's neural display while tagging bystanders with caution markers.\n\nChambered in 6mm caseless with an optimized twist rate for close-range terminal performance, the Crosswalk sacrifices ballistic performance beyond 300 meters for devastating effect inside 150. The 6mm round at close range with the Crosswalk's optimized twist fragments reliably in soft tissue while losing energy rapidly after penetration, reducing the risk of overpenetration in populated environments.\n\nMeridian designed the MUC-6 for the urban operators who make up the majority of their customer base — freelancers, security contractors, and bounty hunters whose work happens on city streets, in parking structures, and through apartment corridors. At Φ3,400, the Crosswalk is moderately priced and brutally effective within its intended engagement envelope.",
    manufacturer: "MERIDIAN MUNITIONS",
    tier_availability: "Tier 2+",
    legality: "Available — standard licensing",
    base_technologies: ["Flash-suppressed ported barrel", "BCI threat-discrimination system", "Close-range optimized twist rate"],
    specifications: "caliber: 6mm caseless\neffective_range: 300m (150m optimal)\nrate_of_fire: 800 rpm (cyclic)\nmagazine_capacity: 30 rounds\nweight: 2.6 kg",
    tactical_use: "The Crosswalk dominates urban combat inside 150 meters. Its threat-discrimination BCI overlay helps operators make shoot/no-shoot decisions in crowded environments, reducing collateral casualties. The flash-suppressed barrel allows firing in darkened interiors without destroying the operator's night vision or revealing their position. The close-range optimized ammunition fragments reliably in targets while minimizing danger to bystanders from overpenetration.",
    cultural_context: "The Crosswalk is the city gun — the weapon of operators who work where people live. Meridian's marketing emphasizes precision and responsibility, positioning the MUC-6 as the weapon of professionals who care about collateral damage. In practice, the 'Street Sweeper' nickname tells a different story. The weapon is devastatingly effective in close quarters, and not everyone who carries one shares Meridian's concern for bystanders.",
    known_users: ["Urban freelancers", "Bounty hunters", "Building security teams", "Close protection details"],
    story_hooks: [
      "A Crosswalk's threat-discrimination system was hacked to tag a specific civilian as a hostile combatant. The operator fired before realizing the system was compromised.",
      "Meridian is recalling a batch of MUC-6s after discovering the close-range ammunition fragments are leaving distinctive metallic residue that allows forensic identification of the specific weapon used — a feature no one requested and no one wants."
    ],
    ammunition_type: ["6mm caseless frangible", "6mm caseless standard"],
    tags: ["weapon", "assault_rifle", "urban", "CQB", "low_flash", "Meridian", "tier 2"]
  },
  {
    id: id(),
    name: "Crucible Industries Garrison Rifle GR-5 'Rampart'",
    type: "weapon",
    aliases: ["Rampart", "GR-5", "Wall Gun"],
    category: "assault_rifle",
    description: "The GR-5 Rampart is a heavy-barreled 7.62mm assault rifle designed for static defensive positions — guard towers, checkpoints, and fortified perimeters. The weapon features an integrated mounting clamp that locks into standard defensive position fixtures, providing a stable firing platform without requiring a bipod or sandbag rest. Once mounted, the Rampart's BCI link interfaces with the defensive position's sensor array to provide 360-degree threat detection and automated tracking.\n\nThe rifle's heavy barrel profile supports sustained fire rates that would overheat lighter weapons, and a quick-change barrel system allows hot barrels to be swapped in under ten seconds. The magazine well accepts standard 30-round boxes as well as Crucible's proprietary 100-round drum for extended engagements. The weapon is unwieldy for mobile operations — at 5.5 kilograms before mounting hardware, it is firmly in the 'emplaced weapon' category.\n\nCrucible sells the Rampart to settlement defense forces, corporate facility security, and any organization that needs to hold ground. The weapon is frequently encountered at checkpoints across Meridian 88, mounted to standardized defensive fixtures and ready to deny passage to anyone unauthorized.",
    manufacturer: "CRUCIBLE INDUSTRIES",
    tier_availability: "Tier 2+",
    legality: "Available — defensive installation licensing",
    base_technologies: ["Integrated mounting clamp system", "Quick-change barrel", "Sensor array BCI integration", "100-round drum compatibility"],
    specifications: "caliber: 7.62mm caseless\neffective_range: 700m\nrate_of_fire: 650 rpm (cyclic)\nmagazine_capacity: 30 rounds (box) / 100 rounds (drum)\nweight: 5.5 kg (unmounted)",
    tactical_use: "The Rampart is a position defense weapon. Mounted in a prepared position with sensor integration, it provides a single operator with the sustained firepower normally requiring a crew-served weapon. The quick-change barrel ensures continuous fire capability during extended siege scenarios. The 100-round drum magazine allows a full minute of sustained fire without reloading. The weapon's weight and mounting system make it impractical for mobile operations.",
    cultural_context: "The Rampart is the weapon of walls and gates — wherever people draw a line and dare others to cross it. Settlements evaluate their security by counting their Ramparts, and the presence of GR-5s at a checkpoint signals serious defensive intent. In Tier 1-2 communities, a mounted Rampart is often the most powerful weapon available, and the person who operates it holds disproportionate importance in the settlement's defense hierarchy.",
    known_users: ["Settlement defense forces", "Checkpoint security", "Corporate facility perimeter teams", "Fortified position operators"],
    story_hooks: [
      "A settlement's three Rampart positions have been sabotaged simultaneously — the quick-change barrels were replaced with units that will catastrophically fail after 50 rounds of sustained fire.",
      "A Rampart operator at a disputed checkpoint has been ordered to deny passage to refugees fleeing a corporate conflict zone. The operator's BCI shows women, children, and wounded civilians approaching their firing position."
    ],
    ammunition_type: ["7.62mm caseless standard", "7.62mm caseless AP", "7.62mm caseless tracer"],
    tags: ["weapon", "assault_rifle", "defensive", "emplaced", "heavy", "sustained_fire", "Crucible", "tier 2"]
  },
  {
    id: id(),
    name: "Kang-Petrov Arms KPA-25EM 'Tempest'",
    type: "weapon",
    aliases: ["Tempest", "KPA-25EM", "Storm Rail"],
    category: "assault_rifle",
    description: "The KPA-25EM Tempest is Kang-Petrov's second-generation electromagnetic rifle, addressing the Thunderclap's limitations with a dual-stage acceleration system and improved capacitor technology. The Tempest fires 4mm tungsten-composite penetrators — an upgrade from the Thunderclap's steel-core rounds — and achieves velocities approaching the Arcturus MAR-8X at two-thirds the cost.\n\nKang-Petrov's engineering team reverse-engineered the acceleration physics from captured MAR-8X units and developed their own implementation using commercially available components. The Tempest's capacitor system is distributed through the weapon's body rather than concentrated in a single pack, reducing the catastrophic failure risk that plagues Thunderclap modifications. Power supply provides 50 shots per charge with a 60-second recharge cycle.\n\nArcturus has filed seventeen patent infringement claims against the Tempest, all of which Kang-Petrov has contested in corporate arbitration. The legal battle has become a proxy war over whether electromagnetic weapons technology can be monopolized. Meanwhile, the Tempest ships to any buyer with Φ8,000 and standard licensing.",
    manufacturer: "KANG-PETROV ARMS",
    tier_availability: "Tier 3+",
    legality: "Available — standard licensing (pending litigation)",
    base_technologies: ["Dual-stage electromagnetic acceleration", "Distributed capacitor architecture", "Tungsten-composite penetrators"],
    specifications: "caliber: 4mm tungsten-composite penetrator\neffective_range: 650m\nrate_of_fire: 180 rpm (capacitor-limited)\nmagazine_capacity: 50 penetrators per charge\nweight: 4.1 kg",
    tactical_use: "The Tempest provides near-military electromagnetic capability to operators outside the corporate military pipeline. Its tungsten-composite penetrators defeat Level III armor at engagement range, and the distributed capacitor system eliminates the single-point failure that makes Thunderclap modifications dangerous. The weapon functions as a squad-level anti-armor asset for forces that cannot afford or access Arcturus platforms.",
    cultural_context: "The Tempest is a direct challenge to Arcturus' dominance in electromagnetic weapons, and the ongoing legal battle has made it a symbol of resistance against corporate technology monopolies. Buying a Tempest is partly a practical decision and partly a political statement. Kang-Petrov prints 'INNOVATION CANNOT BE MONOPOLIZED' on every Tempest shipping crate.",
    known_users: ["Anti-corporate militia forces", "Independent security contractors", "Tier 3 defense cooperatives"],
    story_hooks: [
      "Arcturus has lost patience with legal channels and is planning a covert operation to sabotage Kang-Petrov's Tempest manufacturing facility.",
      "A Tempest purchased through legitimate channels came with hidden firmware that scans for and logs the presence of nearby Arcturus equipment — Kang-Petrov is building an intelligence map of Arcturus deployments through their customers' weapons."
    ],
    ammunition_type: ["4mm tungsten-composite penetrator"],
    tags: ["weapon", "assault_rifle", "electromagnetic", "railgun", "Kang-Petrov", "anti_monopoly", "tier 3"]
  },
];

// ═══════════════════════════════════════════════════════
// SMGs / PDWs (25)
// ═══════════════════════════════════════════════════════
const smgs = [
  {
    id: id(),
    name: "Tessera Personal Defense System TPDS-1 'Reflex'",
    type: "weapon",
    aliases: ["Reflex", "TPDS-1", "Twitch Gun"],
    category: "smg",
    description: "The TPDS-1 Reflex is Tessera's BCI-optimized personal defense weapon, designed to be drawn and firing in under 400 milliseconds through neural command. The weapon's holster contains a capacitive sensor that detects the operator's BCI-transmitted draw signal before their hand physically reaches the weapon, pre-chambering a round and disengaging the safety during the draw stroke. By the time the Reflex clears the holster, it is ready to fire.\n\nChambered in 4.6mm high-velocity caseless from a 30-round magazine, the Reflex is a blowback-operated PDW with a cyclic rate of 900 rpm. The BCI fire-control system predicts the operator's intended point of aim based on neural signals and pre-adjusts the weapon's electronic sighting to compensate for draw-and-shoot inaccuracy. Tessera claims the system reduces first-shot time to 0.35 seconds from concealed carry.\n\nThe Reflex costs Φ6,500 and requires a compatible BCI implant with at least a Class 3 motor-interface rating. Without the neural link, the weapon functions as a conventional PDW — competent but unremarkable. With it, the Reflex becomes an extension of the operator's nervous system, firing at the speed of thought rather than the speed of muscle.",
    manufacturer: "TESSERA",
    tier_availability: "Tier 3+",
    legality: "Restricted — BCI certification required",
    base_technologies: ["BCI predictive draw system", "Neural aim-prediction", "Capacitive holster pre-chamber", "Motor-interface integration"],
    specifications: "caliber: 4.6mm HV caseless\neffective_range: 150m\nrate_of_fire: 900 rpm (cyclic)\nmagazine_capacity: 30 rounds\nweight: 1.8 kg",
    tactical_use: "The Reflex is a reaction weapon — designed for the moment when an operator needs firepower immediately and has no time for a conventional draw. The BCI-predicted draw and pre-chambering shave critical fractions of a second off the engagement timeline. In close protection and executive security roles, those fractions determine whether the principal lives or dies. The weapon's limitations are its short range and small caliber, which restrict it to emergency engagements inside 50 meters.",
    cultural_context: "The Reflex represents the arms race between draw speed and threat speed. In a world where augmented attackers can close distance faster than unaugmented defenders can react, the TPDS-1 restores the reaction advantage to the defender. Corporate executives and high-value targets view the Reflex as essential insurance, while critics note that a weapon designed to fire faster than conscious thought raises uncomfortable questions about accountability.",
    known_users: ["Executive protection details", "Corporate VIP security", "BCI-equipped close protection operators"],
    story_hooks: [
      "A Reflex operator's BCI was subjected to a stress-injection hack that simulated a threat response, causing the weapon to pre-chamber and draw while the operator was in a crowded meeting.",
      "Tessera's insurance actuaries have discovered that Reflex operators have a higher rate of accidental discharge than any other weapon system — the BCI-predicted draw sometimes fires on false positives."
    ],
    ammunition_type: ["4.6mm HV caseless", "4.6mm HV caseless hollow-point"],
    tags: ["weapon", "smg", "PDW", "BCI", "fast_draw", "executive_protection", "Tessera", "tier 3"]
  },
  {
    id: id(),
    name: "Kang-Petrov Arms KPS-9 'Rattler'",
    type: "weapon",
    aliases: ["Rattler", "KPS-9", "Buzz Gun", "Street Sweeper"],
    category: "smg",
    description: "The KPS-9 Rattler is the cheapest submachine gun on Meridian 88. At Φ400, it costs less than many handguns, and Kang-Petrov manufactures it with that price point as the primary design constraint. The weapon is a simple blowback-operated SMG chambered in 9mm brass-cased, with a stamped steel receiver, polymer grip, and a 32-round stick magazine. There is no BCI link, no electronic sight, no onboard computer. The sights are molded into the receiver.\n\nReliability is inconsistent. The Rattler functions adequately when clean and properly lubricated, but the loose tolerances that enable mass production also admit contamination. In dusty or wet conditions, malfunctions increase noticeably. The weapon's accuracy is sufficient for engagements inside 50 meters, but beyond that distance the combination of fixed sights and a short barrel makes consistent hits challenging.\n\nNone of this matters to the Rattler's customers. The KPS-9 exists because violence is not exclusively a privilege of the well-funded. For Φ400, a person in a Tier 1 zone gets a weapon that fires 30 rounds of commonly available ammunition. It is not a good weapon. It is an available weapon. In a world where availability often matters more than quality, the Rattler sells millions.",
    manufacturer: "KANG-PETROV ARMS",
    tier_availability: "Tier 1+",
    legality: "Widely available — minimal restrictions",
    base_technologies: ["Stamped steel mass production", "Simple blowback operation"],
    specifications: "caliber: 9mm brass-cased\neffective_range: 50m\nrate_of_fire: 600 rpm (cyclic)\nmagazine_capacity: 32 rounds\nweight: 2.1 kg",
    tactical_use: "The Rattler has no tactical sophistication. It is a volume-of-fire weapon used at close range where accuracy is determined by proximity rather than skill. In desperate defense scenarios, the KPS-9 provides suppressive capability that a handgun cannot match. Its sole tactical advantage is ubiquity — ammunition and spare parts are available everywhere, and the weapon's simplicity means anyone can learn to operate it in minutes.",
    cultural_context: "The Rattler is the weapon of the desperate, the impoverished, and the unprepared. Finding a KPS-9 in someone's possession tells you nothing about their affiliations — it tells you about their budget. The weapon appears in crime scenes, defensive actions, and everywhere that violence occurs among people who cannot afford better. Kang-Petrov has been criticized for manufacturing a weapon this cheap, but the alternative is a monopoly on violence by those who can afford premium arms.",
    known_users: ["Tier 1 civilians", "Street-level criminals", "Emergency defense", "Anyone with Φ400"],
    story_hooks: [
      "A bulk shipment of 10,000 Rattlers was intercepted en route to a Tier 1 zone that is about to vote on corporate annexation. The timing suggests someone wants the vote to go a specific way.",
      "A forensic analyst has traced identical manufacturing defects in Rattlers from three different crime scenes — the weapons came from the same production batch, suggesting coordinated distribution."
    ],
    ammunition_type: ["9mm brass-cased standard", "9mm brass-cased hollow-point"],
    tags: ["weapon", "smg", "cheap", "mass_production", "ubiquitous", "Kang-Petrov", "tier 1"]
  },
  {
    id: id(),
    name: "Arcturus Defense Solutions CSW-4 'Cicada'",
    type: "weapon",
    aliases: ["Cicada", "CSW-4", "The Hive"],
    category: "smg",
    description: "The CSW-4 Cicada is Arcturus' corporate security SMG, standard issue for their internal security personnel operating in office environments, corporate campuses, and executive facilities. The weapon fires 4.6mm caseless from a 25-round magazine at a controlled 700 rpm, with the BCI smart-link providing an augmented-reality threat identification overlay that cross-references Arcturus' employee database in real-time.\n\nThe Cicada's design prioritizes concealment and non-intimidation. The weapon's profile is deliberately understated — smooth lines, matte grey finish, no visible magazine or charging handle in its stowed configuration. When holstered in Arcturus' standard duty rig, it resembles a communications device rather than a weapon. The goal is a security presence that does not alarm corporate employees or visiting executives.\n\nDespite its unassuming appearance, the Cicada is a thoroughly professional weapon. The 4.6mm round provides adequate penetration against soft body armor while fragmenting in office-grade partition walls, reducing overpenetration risk. Arcturus issues the CSW-4 with strict rules of engagement programmed into the BCI fire-control — the weapon physically will not fire if the smart-link identifies the target as a corporate employee above a certain clearance level.",
    manufacturer: "ARCTURUS DEFENSE SOLUTIONS",
    tier_availability: "Tier 3+",
    legality: "Restricted — corporate security issue",
    base_technologies: ["Concealed-carry profile design", "Employee database IFF integration", "Clearance-locked fire control", "Low-penetration ammunition optimization"],
    specifications: "caliber: 4.6mm caseless\neffective_range: 100m\nrate_of_fire: 700 rpm (cyclic)\nmagazine_capacity: 25 rounds\nweight: 1.5 kg",
    tactical_use: "The Cicada is designed for corporate interior engagements — offices, lobbies, corridors, and parking structures. Its concealed profile allows security to maintain a presence without creating a hostile atmosphere, while the IFF system prevents friendly fire incidents that would generate liability. The clearance-locked fire control is both a safety feature and a political tool — it ensures corporate hierarchy is literally enforced at gunpoint.",
    cultural_context: "The Cicada represents the corporate approach to violence: controlled, documented, and hierarchically aware. The weapon's inability to fire on sufficiently senior executives has generated dark humor among Arcturus security staff — the joke is that the safest place in a firefight is behind someone with a high enough clearance level. Less amusing is the implication that corporate rank determines whose life the weapon system values.",
    known_users: ["Arcturus internal security", "Corporate campus police", "Executive facility guards"],
    story_hooks: [
      "An Arcturus security officer needs to engage an active threat — but the attacker has stolen a senior executive's identity badge, and the Cicada's IFF will not allow the weapon to fire at them.",
      "A batch of Cicadas has been modified to remove the clearance lock. Arcturus security personnel with these weapons can fire on anyone, including executives — someone is enabling an internal coup."
    ],
    ammunition_type: ["4.6mm caseless frangible"],
    tags: ["weapon", "smg", "corporate", "concealed", "IFF", "Arcturus", "tier 3"]
  },
  {
    id: id(),
    name: "Meridian Munitions Viper PDW MVP-3 'Sidewinder'",
    type: "weapon",
    aliases: ["Sidewinder", "MVP-3", "Viper"],
    category: "smg",
    description: "The MVP-3 Sidewinder is Meridian Munitions' compact personal defense weapon, designed for operators who need maximum firepower in minimum space. The weapon uses a telescoping bolt design that wraps around the barrel, reducing overall length to 280mm with the stock folded while maintaining a 180mm barrel for adequate velocity. Chambered in 5.7mm caseless armor-piercing, the Sidewinder punches above its weight class against body armor.\n\nThe BCI aim-assist module is the same open-architecture system used in the MM-4 Breadwinner, maintaining Meridian's commitment to non-proprietary accessories. A rail-mounted micro red dot is included as standard, and the weapon accepts any manufacturer's suppressor on its threaded barrel. The 25-round magazine fits flush with the grip, maintaining the compact profile.\n\nAt Φ2,200, the Sidewinder occupies the mid-range of the PDW market. It lacks the BCI sophistication of Tessera's Reflex and the rock-bottom pricing of Kang-Petrov's Rattler, but it delivers reliable performance with armor-piercing capability in a package that fits inside a laptop bag. For the freelancer who needs a concealable weapon that can defeat body armor, the MVP-3 is the practical choice.",
    manufacturer: "MERIDIAN MUNITIONS",
    tier_availability: "Tier 2+",
    legality: "Available — standard licensing",
    base_technologies: ["Telescoping bolt design", "Open-architecture BCI module", "Armor-piercing optimized feed"],
    specifications: "caliber: 5.7mm caseless AP\neffective_range: 150m\nrate_of_fire: 850 rpm (cyclic)\nmagazine_capacity: 25 rounds\nweight: 1.6 kg",
    tactical_use: "The Sidewinder provides armor-piercing capability in a concealable package. Its 5.7mm AP round defeats Level II body armor at close range and degrades Level III protection, giving operators a response to armored threats that pistol-caliber weapons cannot address. The compact dimensions allow concealed carry in civilian environments where a visible weapon would compromise the operation. The open-architecture BCI module means operators already using Meridian's ecosystem can transfer their settings directly.",
    cultural_context: "The Sidewinder is the freelancer's insurance policy — the weapon carried when the job description says 'no weapons expected' but experience says otherwise. Its popularity among couriers, negotiators, and fixers reflects the reality that Meridian 88's professional class moves through danger zones as part of their daily routine. The MVP-3 doesn't start fights. It finishes the ones that find you.",
    known_users: ["Freelance operators", "Corporate couriers", "Negotiators and fixers", "Personal defense carriers"],
    story_hooks: [
      "A courier's Sidewinder was the only weapon available when their convoy was ambushed. The 25-round magazine held off attackers long enough for extraction — barely.",
      "Meridian is developing an upgraded Sidewinder with a 40-round extended magazine, but the prototype was stolen from their R&D facility along with the production blueprints."
    ],
    ammunition_type: ["5.7mm caseless AP", "5.7mm caseless standard"],
    tags: ["weapon", "smg", "PDW", "compact", "armor_piercing", "concealed", "Meridian", "tier 2"]
  },
  {
    id: id(),
    name: "Crucible Industries Close Defense Weapon CDW-8 'Barricade'",
    type: "weapon",
    aliases: ["Barricade", "CDW-8", "The Brick"],
    category: "smg",
    description: "The CDW-8 Barricade is a bullpup submachine gun built like a cinder block. Crucible Industries designed it for operators in powered exoframes and heavy augmentation rigs where weapon size is less important than durability. The weapon's reinforced polymer-steel shell can withstand being used as a blunt weapon, dropped from vehicles, or run over by light transport without affecting function. The internal mechanism is sealed against water, dust, and chemical contamination.\n\nChambered in 10mm caseless heavy subsonic, the Barricade sacrifices rate of fire for stopping power. Each round delivers energy equivalent to a 12-gauge slug, and the weapon's recoil is managed by an internal hydraulic buffer that reduces felt impulse to levels manageable by augmented operators. Unaugmented shooters find the Barricade punishing after extended use, though short engagements are manageable.\n\nThe CDW-8 was originally designed for shipyard security where operators in industrial exoframes needed a weapon compatible with their existing equipment. Its success in that niche led Crucible to market it more broadly to augmented operators who need a weapon matching their own durability. At Φ3,800, the Barricade is moderately priced for a weapon that will outlast its operator.",
    manufacturer: "CRUCIBLE INDUSTRIES",
    tier_availability: "Tier 2+",
    legality: "Available — standard licensing",
    base_technologies: ["Reinforced polymer-steel shell", "Hydraulic recoil buffer", "Sealed environmental protection", "Heavy subsonic optimization"],
    specifications: "caliber: 10mm caseless heavy subsonic\neffective_range: 75m\nrate_of_fire: 500 rpm (cyclic)\nmagazine_capacity: 20 rounds\nweight: 2.8 kg",
    tactical_use: "The Barricade delivers devastating close-range firepower in a package that survives abuse that would destroy conventional SMGs. Its 10mm heavy subsonic round provides exceptional stopping power against unarmored and lightly armored targets without the overpenetration risk of high-velocity rounds. The sealed design functions in environments — flooded compartments, chemical spills, industrial contamination — where other weapons fail. The hydraulic buffer makes automatic fire controllable for augmented operators.",
    cultural_context: "The Barricade has found an unexpected following among industrial workers who transition into security roles, bringing their familiarity with rugged equipment into their weapons choices. In dockyard and shipyard communities, the CDW-8 is the default security weapon, and its blocky, industrial appearance fits naturally among heavy equipment. The nickname 'Brick' is affectionate — it looks like one and hits like one.",
    known_users: ["Shipyard security", "Industrial facility guards", "Augmented close-combat operators", "Exoframe-equipped personnel"],
    story_hooks: [
      "A CDW-8 survived a factory explosion that destroyed everything else in the armory. The weapon's sealed internals protected a data chip hidden inside the grip — someone used the Barricade as a dead drop.",
      "A dock worker used their issued Barricade to defend coworkers during a corporate raid. The weapon absorbed three direct hits from rifle fire without failing — Crucible wants to use the story in their marketing, but the worker's identity would expose them to retaliation."
    ],
    ammunition_type: ["10mm caseless heavy subsonic", "10mm caseless standard"],
    tags: ["weapon", "smg", "heavy", "durable", "industrial", "augmented", "Crucible", "tier 2"]
  },
  {
    id: id(),
    name: "Tessera Micro-PDW TMP-2 'Synapse'",
    type: "weapon",
    aliases: ["Synapse", "TMP-2", "Brain Gun"],
    category: "smg",
    description: "The TMP-2 Synapse is a micro-PDW that weighs 900 grams and measures 200mm with its stock folded. Tessera designed it as the smallest possible weapon that maintains meaningful combat capability, intended for deep-cover operatives and intelligence personnel who cannot carry anything larger. The weapon fires 4.6mm caseless from a 15-round flush magazine at 1,000 rpm — emptying its magazine in under a second on full auto.\n\nThe Synapse's BCI integration is its defining feature. The weapon's targeting computer tracks the operator's eye movements through the neural link and adjusts point of aim to compensate for the micro-PDW's minimal sight radius. In effect, the Synapse hits where the operator looks, not where the weapon points. This eye-tracking fire control transforms what would otherwise be an inaccurate last-resort weapon into a precise close-quarters tool.\n\nAt Φ9,000, the Synapse is extraordinarily expensive for its size. The cost reflects the miniaturized BCI targeting system rather than the mechanical components, which are simple. Tessera sells the TMP-2 to intelligence agencies and executive protection firms who need the smallest possible weapon with the highest possible hit probability.",
    manufacturer: "TESSERA",
    tier_availability: "Tier 4+",
    legality: "Restricted — intelligence and executive protection only",
    base_technologies: ["BCI eye-tracking fire control", "Micro-PDW miniaturization", "Neural aim compensation"],
    specifications: "caliber: 4.6mm caseless\neffective_range: 50m\nrate_of_fire: 1,000 rpm (cyclic)\nmagazine_capacity: 15 rounds\nweight: 0.9 kg",
    tactical_use: "The Synapse is a reaction weapon for environments where any visible weapon is unacceptable. Its 15-round magazine and extreme rate of fire provide approximately one second of automatic fire — enough for a single engagement at conversational distance. The BCI eye-tracking system ensures those rounds hit their target despite the weapon's minimal dimensions. After the magazine is empty, the Synapse is spent. It is designed for one engagement, not sustained combat.",
    cultural_context: "The Synapse is the weapon of last resort for people who inhabit spaces where weapons do not exist. Diplomats, intelligence operatives, and undercover agents carry TMP-2s because the alternative is being unarmed in situations where an exposed weapon means death. The weapon's existence is semi-classified — Tessera does not advertise it publicly, and finding one on someone is grounds for immediate detention in most corporate jurisdictions.",
    known_users: ["Intelligence operatives", "Deep-cover agents", "Diplomatic protection details"],
    story_hooks: [
      "A Synapse was found during a routine security scan of a diplomatic envoy. The weapon's BCI logs reveal its last target was a head of state — and the engagement was logged as successful.",
      "An intelligence operative's Synapse malfunctioned, and the eye-tracking system locked onto their own reflection in a mirror. The weapon discharged into the reflective surface, revealing the operative's position."
    ],
    ammunition_type: ["4.6mm caseless"],
    tags: ["weapon", "smg", "micro", "concealed", "intelligence", "BCI", "eye_tracking", "Tessera", "tier 4"]
  },
  {
    id: id(),
    name: "Hearthstone Firearms Defender SMG HDS-5 'Hearth Guard'",
    type: "weapon",
    aliases: ["Hearth Guard", "HDS-5", "Home Defense"],
    category: "smg",
    description: "The HDS-5 Hearth Guard is a semi-automatic-only carbine chambered in 9mm brass-cased, designed for civilian home and business defense. Hearthstone Firearms deliberately restricted the weapon to semi-automatic operation to avoid regulatory classification as a military weapon, keeping it legal in jurisdictions where automatic weapons are prohibited. The result is a 9mm carbine with a 16-round magazine, basic iron sights, and no electronic components.\n\nThe Hearth Guard's design emphasizes safety for untrained users. The manual safety is oversized and color-coded — red for fire, white for safe. The magazine release requires deliberate two-finger operation to prevent accidental drops. The trigger pull is a heavy 4.5 kilograms to prevent negligent discharge. Every design decision prioritizes preventing accidents over maximizing combat effectiveness.\n\nAt Φ350, the Hearth Guard is the cheapest long gun Hearthstone produces. It sells in enormous volumes to small business owners, residential cooperatives, and individuals who need a weapon more capable than a handgun but cannot afford or legally possess a military-grade platform. The weapon is basic, safe, and reliable — exactly what its customers need.",
    manufacturer: "HEARTHSTONE FIREARMS",
    tier_availability: "Tier 1+",
    legality: "Unrestricted — civilian grade",
    base_technologies: ["Safety-prioritized design", "Semi-automatic restricted operation", "Accident-prevention ergonomics"],
    specifications: "caliber: 9mm brass-cased\neffective_range: 75m\nrate_of_fire: Semi-automatic only\nmagazine_capacity: 16 rounds\nweight: 2.3 kg",
    tactical_use: "The Hearth Guard provides civilian defenders with a stable, easy-to-shoot platform that is more accurate than a handgun at home-defense distances. The heavy trigger and safety features reduce the risk of accidental discharge by untrained family members. The 9mm carbine format offers mild recoil suitable for shooters of all sizes and strength levels. It is not a combat weapon — it is a defense tool designed to be used by people who hope they never need it.",
    cultural_context: "The Hearth Guard is the weapon of shopkeepers, teachers, and parents. It lives in bedroom closets and behind store counters, and most of them are never fired outside of basic familiarization. Hearthstone markets the HDS-5 with images of ordinary people in ordinary spaces — a deliberate contrast to the operator-focused marketing of corporate arms. The message is clear: this weapon is for you, not for soldiers.",
    known_users: ["Civilian homeowners", "Small business owners", "Residential cooperatives", "Community watch groups"],
    story_hooks: [
      "A shopkeeper defended their business with a Hearth Guard during a riot and is now facing legal consequences — the jurisdiction's self-defense laws were written by the corponation that instigated the unrest.",
      "Hearthstone's bulk shipment of 5,000 Hearth Guards to a Tier 1 settlement was intercepted by corporate security, who classify the semi-automatic carbines as military weapons under a newly enacted regulation."
    ],
    ammunition_type: ["9mm brass-cased standard"],
    tags: ["weapon", "smg", "civilian", "home_defense", "safe", "affordable", "Hearthstone", "tier 1"]
  },
  {
    id: id(),
    name: "Volkov-Saito Precision VSP-11 'Stiletto'",
    type: "weapon",
    aliases: ["Stiletto", "VSP-11", "Needle Gun"],
    category: "smg",
    description: "The VSP-11 Stiletto is a precision submachine gun — a contradiction in terms that Volkov-Saito makes work through obsessive engineering. The weapon fires 5.7mm caseless from a match-grade barrel with a precision bolt that locks into battery with the same consistency as their rifle platforms. The result is an SMG that delivers 2-MOA accuracy at 100 meters in semi-automatic mode — performance that rivals many assault rifles at that distance.\n\nThe Stiletto's BCI integration includes Volkov-Saito's signature weapon-learning system, building a ballistic profile specific to each individual weapon over time. The 20-round magazine feeds through a precision-machined feed ramp that eliminates the feeding inconsistencies common in SMG platforms. The trigger is a two-stage match unit adjustable from 1.5 to 3 kilograms.\n\nAt Φ7,500, the Stiletto costs more than many assault rifles. Its customers are operators who need SMG-class concealability with rifle-class accuracy — executive protection specialists who might need to make a 100-meter shot through a crowd without hitting bystanders, or covert operators who need precision from a compact platform. The VSP-11 does not spray. It places.",
    manufacturer: "VOLKOV-SAITO PRECISION",
    tier_availability: "Tier 3+",
    legality: "Restricted — premium licensing",
    base_technologies: ["Match-grade SMG barrel", "Precision bolt lockup", "Weapon-learning BCI system", "Match trigger unit"],
    specifications: "caliber: 5.7mm caseless\neffective_range: 200m\nrate_of_fire: 750 rpm (cyclic), semi-auto preferred\nmagazine_capacity: 20 rounds\nweight: 2.0 kg",
    tactical_use: "The Stiletto enables precise engagements from a concealable platform. Its accuracy in semi-automatic mode allows operators to make shots that would require a rifle from other manufacturers' SMGs. In close protection scenarios where a missed shot hits a civilian, the VSP-11's precision is not a luxury — it is a requirement. The weapon-learning system improves accuracy over time, rewarding operators who maintain a consistent relationship with their specific weapon.",
    cultural_context: "The Stiletto has a devoted following among precision-oriented operators who refuse to accept the accuracy limitations of conventional SMGs. Volkov-Saito's reputation for precision transfers directly to the VSP-11, and carrying one signals that the operator values placement over volume. In competitive shooting circles, Stiletto-class SMG matches have become a distinct discipline.",
    known_users: ["Precision close-protection operators", "Competition shooters", "Covert precision specialists"],
    story_hooks: [
      "A VSP-11 was used to make an impossible shot through a crowded market — the 100-meter precision engagement hit only the intended target despite dozens of bystanders. Only a Stiletto with a trained operator could have made that shot.",
      "Volkov-Saito's weapon-learning data from returned Stilettos has been compiled into a database that can identify individual operators by their firing patterns — an intelligence goldmine."
    ],
    ammunition_type: ["5.7mm caseless match", "5.7mm caseless AP"],
    tags: ["weapon", "smg", "precision", "match_grade", "Volkov-Saito", "tier 3"]
  },
  {
    id: id(),
    name: "Kang-Petrov Arms KPS-12D 'Typhoon'",
    type: "weapon",
    aliases: ["Typhoon", "KPS-12D", "Drum Gun", "The Flood"],
    category: "smg",
    description: "The KPS-12D Typhoon is a high-capacity submachine gun built around a 75-round drum magazine that gives the weapon its distinctive profile. Chambered in 9mm caseless, the Typhoon fires at 1,100 rpm and can empty its drum in just over four seconds. The weapon exists for one purpose: sustained volume of fire from a man-portable platform.\n\nKang-Petrov made no attempt at subtlety with the Typhoon. The drum magazine protrudes from the bottom of the receiver, making concealment impossible. The weapon is heavy by SMG standards at 3.1 kilograms loaded. Accuracy beyond 30 meters is optimistic. But within its intended engagement envelope — close range, high volume, maximum suppression — the Typhoon is terrifyingly effective.\n\nAt Φ1,200, the Typhoon occupies a price point between the bargain-basement Rattler and professional-grade options. Its customers are operators who prioritize volume over precision: gang enforcers, untrained militia, and anyone whose tactical doctrine amounts to 'more bullets.' The weapon's simplicity means it rarely malfunctions, and the drum magazine provides enough rounds to compensate for poor marksmanship through statistical probability.",
    manufacturer: "KANG-PETROV ARMS",
    tier_availability: "Tier 1+",
    legality: "Available — standard licensing",
    base_technologies: ["High-capacity drum magazine", "High-cyclic blowback action", "Simplified mass production"],
    specifications: "caliber: 9mm caseless\neffective_range: 50m\nrate_of_fire: 1,100 rpm (cyclic)\nmagazine_capacity: 75 rounds (drum)\nweight: 3.1 kg (loaded)",
    tactical_use: "The Typhoon's tactical role is suppression through volume. Its 75-round drum and high cyclic rate allow a single operator to lay down continuous fire that pins enemies behind cover and denies movement through kill zones. Accuracy is secondary — the weapon creates a wall of bullets rather than placing individual shots. In defensive scenarios, a Typhoon operator can hold a corridor or doorway against multiple attackers through sheer volume of fire.",
    cultural_context: "The Typhoon is the weapon of overwhelming force applied without finesse. It is simultaneously feared and mocked — feared for the volume of fire it delivers, mocked for the lack of skill required to operate it. In street parlance, calling someone a 'Typhoon shooter' implies they compensate for incompetence with ammunition expenditure. Despite the stigma, the weapon sells in enormous quantities because its effectiveness does not require skill.",
    known_users: ["Street-level enforcers", "Untrained militia", "Suppressive fire specialists", "Anyone needing volume of fire"],
    story_hooks: [
      "A building siege was broken by a single Typhoon operator who emptied four drums down a hallway in two minutes. The hallway was destroyed, but the attackers retreated. The operator is now deaf in one ear.",
      "A modified Typhoon with a 150-round extended drum has appeared on the black market. The modification is crude but functional, and the weapon has been used in three mass-casualty events."
    ],
    ammunition_type: ["9mm caseless standard", "9mm caseless tracer"],
    tags: ["weapon", "smg", "high_capacity", "suppressive", "volume_fire", "Kang-Petrov", "tier 1"]
  },
  {
    id: id(),
    name: "Arcturus Defense Solutions Executive SMG ESM-2 'Attaché'",
    type: "weapon",
    aliases: ["Attaché", "ESM-2", "Briefcase Gun"],
    category: "smg",
    description: "The ESM-2 Attaché is disguised as a corporate briefcase. The weapon is permanently integrated into a standard Arcturus executive carry case, with the barrel concealed behind a false panel that deploys on BCI command. The operator carries the briefcase normally, and upon neural activation, the front panel drops, exposing the barrel and trigger assembly. The weapon fires through the opened panel while the operator holds the case by its handle.\n\nChambered in 4.6mm caseless with a 20-round internal magazine, the Attaché provides 3 seconds of automatic fire at 700 rpm. The BCI link handles targeting through the executive carry case's integrated sensors — the operator does not aim in any conventional sense. They point the case and the onboard system manages fire distribution across the threat zone.\n\nArcturus developed the Attaché for executives who travel through hostile territory and require personal defense capability that is genuinely invisible. The weapon passes standard security scans when the case is closed — the components are distributed through the case's structure in a pattern that mimics ordinary electronics. At Φ15,000, the Attaché is among the most expensive SMGs in production, but its customers have expense accounts.",
    manufacturer: "ARCTURUS DEFENSE SOLUTIONS",
    tier_availability: "Tier 4+",
    legality: "Highly restricted — executive protection authorization",
    base_technologies: ["Concealed weapon integration", "Scan-defeating component distribution", "BCI autonomous targeting", "Disguised carry system"],
    specifications: "caliber: 4.6mm caseless\neffective_range: 30m\nrate_of_fire: 700 rpm (cyclic)\nmagazine_capacity: 20 rounds (internal)\nweight: 2.5 kg (complete case)",
    tactical_use: "The Attaché is a single-use emergency weapon. Its 20-round internal magazine provides one engagement's worth of firepower — enough to neutralize an immediate threat and create space for evacuation. The scan-defeating design allows the weapon to enter spaces where all visible weapons are prohibited, providing defense where it would otherwise be impossible. Once deployed, the disguise is blown and the case must be reloaded by Arcturus technicians.",
    cultural_context: "The Attaché is an open secret in corporate executive circles. Everyone knows Arcturus makes a weapon disguised as a briefcase. No one knows who carries one. This uncertainty is itself a deterrent — any Arcturus executive's briefcase might contain a weapon, which means threatening any Arcturus executive carries a specific risk. The weapon's existence has influenced corporate fashion; some executives carry deliberately oversized briefcases as a bluff.",
    known_users: ["Arcturus executives", "Corporate VIPs in hostile territories", "Executive protection principals"],
    story_hooks: [
      "An executive's Attaché deployed accidentally during a board meeting when a BCI glitch interpreted a stressful negotiation as a threat response. No one was hit, but the footage is now leverage.",
      "A counterfeit Attaché has appeared on the black market — it looks identical to the genuine article but contains a bomb instead of a weapon, targeting executives who trust the Arcturus brand."
    ],
    ammunition_type: ["4.6mm caseless frangible"],
    tags: ["weapon", "smg", "concealed", "disguised", "executive", "BCI", "Arcturus", "tier 4"]
  },
  {
    id: id(),
    name: "Meridian Munitions Street PDW MSP-7 'Alleycat'",
    type: "weapon",
    aliases: ["Alleycat", "MSP-7", "Cat Gun"],
    category: "smg",
    description: "The MSP-7 Alleycat is Meridian Munitions' dedicated street-level PDW, designed for the reality of urban self-defense in Tier 2-3 zones. The weapon fires 5.7mm caseless from a 20-round magazine and features Meridian's open-architecture BCI module for operators who have basic neural interfaces. For those without BCI, the weapon includes conventional micro red-dot sights and a manual safety.\n\nThe Alleycat's distinguishing feature is its integrated smart-holster system. The weapon's grip contains a biometric lock that releases only for registered users, preventing the weapon from being used against its owner if seized. The holster communicates with the weapon's BCI module to track draw/reholster cycles, and Meridian's companion app provides usage analytics that many operators find useful for training.\n\nAt Φ1,800, the Alleycat sits comfortably in the affordable professional range. It is not cheap enough to be disposable or expensive enough to represent an investment. It is a working tool priced for working people — exactly the market Meridian has cultivated. The weapon is reliable without being remarkable, accurate without being precise, and effective without being devastating.",
    manufacturer: "MERIDIAN MUNITIONS",
    tier_availability: "Tier 2+",
    legality: "Available — standard licensing",
    base_technologies: ["Biometric grip lock", "Smart-holster integration", "Dual BCI/manual operation"],
    specifications: "caliber: 5.7mm caseless\neffective_range: 100m\nrate_of_fire: 800 rpm (cyclic)\nmagazine_capacity: 20 rounds\nweight: 1.4 kg",
    tactical_use: "The Alleycat provides reliable personal defense for urban operators. Its biometric grip lock ensures the weapon cannot be turned against its owner, and the dual BCI/manual operation means it functions regardless of the operator's augmentation level. The 5.7mm round offers better penetration than 9mm against soft armor while remaining controllable in the lightweight platform. The smart-holster analytics help operators train their draw speed and weapon handling.",
    cultural_context: "The Alleycat is the PDW of Meridian 88's urban middle class — security contractors, shop owners in rough neighborhoods, and anyone whose daily commute passes through contested zones. It is the weapon equivalent of a reliable car: not exciting, not prestigious, but always there when you need it. Meridian's marketing emphasizes the Alleycat's approachability, featuring ordinary people in ordinary situations rather than operators in tactical gear.",
    known_users: ["Urban security contractors", "Shop owners", "Commuters in contested zones", "Personal defense carriers"],
    story_hooks: [
      "An Alleycat's biometric lock was hacked, and the weapon was used in a crime while still registered to its original owner. The forensic trail leads back to the victim, not the perpetrator.",
      "Meridian's companion app usage data was subpoenaed by a corponation, revealing the locations and weapon-handling habits of thousands of Alleycat owners — a privacy nightmare."
    ],
    ammunition_type: ["5.7mm caseless standard", "5.7mm caseless hollow-point"],
    tags: ["weapon", "smg", "PDW", "urban", "biometric", "Meridian", "tier 2"]
  },
  {
    id: id(),
    name: "Crucible Industries Suppressed Compact SC-2 'Murmur'",
    type: "weapon",
    aliases: ["Murmur", "SC-2", "Whisper Box"],
    category: "smg",
    description: "The SC-2 Murmur is an integrally suppressed submachine gun designed for industrial security operations where noise discipline is required — server farms, laboratory complexes, and medical facilities where gunfire causes equipment damage beyond the immediate engagement zone. The weapon's barrel is permanently enclosed in a suppression housing that reduces the 9mm subsonic report to levels below the threshold for triggering acoustic security alarms.\n\nThe Murmur uses a locked-breech design unusual for a 9mm SMG, eliminating the mechanical noise of a blowback bolt cycling. The result is a weapon where the loudest sound is the bullet's impact rather than its departure. Combined with subsonic ammunition, the SC-2 can be fired inside a server room without triggering decibel-sensitive fire suppression systems — a real operational concern in facilities where the equipment is worth more than the people.\n\nCrucible markets the Murmur to facility security forces and specialized teams operating in noise-sensitive environments. At Φ4,200, it is priced for organizational procurement rather than individual purchase. The weapon is unremarkable in every way except its silence, which in the right environment is worth more than any other feature.",
    manufacturer: "CRUCIBLE INDUSTRIES",
    tier_availability: "Tier 3+",
    legality: "Restricted — suppressed weapons licensing",
    base_technologies: ["Integral suppression housing", "Locked-breech noise reduction", "Sub-alarm-threshold acoustic profile"],
    specifications: "caliber: 9mm subsonic caseless\neffective_range: 50m\nrate_of_fire: 600 rpm (cyclic)\nmagazine_capacity: 25 rounds\nweight: 2.4 kg",
    tactical_use: "The Murmur allows armed response in environments where the sound of gunfire causes more damage than the bullets. Server farms, clean rooms, laboratory complexes, and medical facilities all contain equipment that is sensitive to acoustic shock or decibel-triggered safety systems. The SC-2's below-alarm acoustic profile ensures that engaging a threat does not simultaneously destroy the facility's critical infrastructure.",
    cultural_context: "The Murmur occupies a peculiar niche — a weapon designed to protect things that are damaged by weapons. Its existence reflects the priority hierarchy of corporate facilities: equipment first, then data, then personnel. Security forces carrying Murmurs are sometimes called 'librarians' — they enforce silence as a professional requirement.",
    known_users: ["Server farm security", "Laboratory complex guards", "Medical facility security", "Data center protection teams"],
    story_hooks: [
      "A data center heist went undetected because the thieves used stolen Murmurs — the acoustic security system never triggered, and the camera footage showed muzzle flashes with no corresponding audio alerts.",
      "A Murmur was used in an assassination at a medical facility. The suppressed weapon was so quiet that patients in adjacent rooms heard nothing — the body wasn't discovered for hours."
    ],
    ammunition_type: ["9mm subsonic caseless"],
    tags: ["weapon", "smg", "suppressed", "silent", "facility_security", "Crucible", "tier 3"]
  },
  {
    id: id(),
    name: "Kang-Petrov Arms KPS-3M 'Mongrel'",
    type: "weapon",
    aliases: ["Mongrel", "KPS-3M", "Junkyard Special"],
    category: "smg",
    description: "The KPS-3M Mongrel is Kang-Petrov's modular SMG platform designed to accept components scavenged from other weapons. The receiver has universal attachment points that mate with barrels, stocks, grips, and magazines from over forty different weapon systems. A Mongrel might have a Tessera barrel, a Crucible stock, and a Meridian magazine, all functioning together through Kang-Petrov's universal interface system.\n\nThe base Mongrel kit — receiver, bolt, and basic furniture — costs Φ300. Everything else is salvageable. Kang-Petrov publishes compatibility guides listing which components from which manufacturers fit the Mongrel's universal interface, and the community has expanded this list through experimentation. The weapon's reliability depends entirely on the quality of components used — a Mongrel built from premium salvage outperforms a Mongrel built from garbage.\n\nThe KPS-3M exists because weapons break, get abandoned, and accumulate in conflict zones faster than they can be manufactured or disposed of. The Mongrel transforms this surplus of parts into functional weapons, recycling the detritus of violence into tools for survival. It is simultaneously the most innovative and most desperate weapon in production.",
    manufacturer: "KANG-PETROV ARMS",
    tier_availability: "Tier 1+",
    legality: "Available — basic licensing",
    base_technologies: ["Universal component interface", "Cross-manufacturer compatibility", "Modular salvage architecture"],
    specifications: "caliber: Variable (depends on barrel/magazine configuration)\neffective_range: Variable (30-150m depending on build)\nrate_of_fire: Variable (depends on bolt and caliber)\nmagazine_capacity: Variable (depends on magazine used)\nweight: 1.5-3.0 kg (depends on configuration)",
    tactical_use: "The Mongrel's tactical value is adaptability. In environments where resupply is impossible, the ability to scavenge components from any weapon system and build a functional SMG is invaluable. Operators in prolonged sieges, behind enemy lines, or in collapsed infrastructure zones can maintain armed capability by harvesting parts from the battlefield. The weapon's performance ceiling is limited by available components, but its performance floor is 'functional firearm from scrap metal.'",
    cultural_context: "The Mongrel is the weapon of improvisation and survival. Every Mongrel is unique — a physical record of what was available, what was scavenged, and what was needed. In Tier 1 communities, Mongrel-building is a respected skill, and gunsmiths who can assemble reliable weapons from random parts are valued members of any settlement. The weapon is ugly, inconsistent, and deeply personal. No two are alike.",
    known_users: ["Scavengers", "Siege survivors", "Frontier gunsmiths", "Anyone with parts and desperation"],
    story_hooks: [
      "A Mongrel recovered from a crime scene contains components from weapons that were supposed to be destroyed in an Arcturus decommissioning. Someone is diverting 'destroyed' weapon parts into the salvage market.",
      "A legendary gunsmith builds Mongrels that outperform factory weapons — their builds are collector's items worth Φ5,000+. They've been kidnapped by a faction that wants exclusive access to their skills."
    ],
    ammunition_type: ["Variable — depends on configuration"],
    tags: ["weapon", "smg", "modular", "salvage", "improvised", "Kang-Petrov", "tier 1"]
  },
  {
    id: id(),
    name: "Tessera Networked SMG TNSM-3 'Chorus'",
    type: "weapon",
    aliases: ["Chorus", "TNSM-3", "Harmony Gun"],
    category: "smg",
    description: "The TNSM-3 Chorus is the SMG variant of Tessera's networked weapons platform, sharing the TAR-12 Consensus's distributed targeting AI in a compact package designed for close-quarters team operations. When multiple Chorus units are networked, the weapons coordinate fire distribution, prevent friendly-fire incidents, and assign sectors of fire automatically based on each operator's position and facing.\n\nChambered in 4.6mm caseless with a 30-round magazine, the Chorus provides adequate individual firepower. Its true capability emerges in teams of three or more, where the networking creates a coordinated defense that covers all angles simultaneously. The BCI link provides each operator with their teammates' positions, assigned sectors, and real-time ammunition counts, enabling tactical coordination without verbal communication.\n\nThe TNSM-3 costs Φ5,000 per unit, and Tessera sells them in team packs of four with a discount to Φ18,000. The networking protocol is encrypted and self-organizing — units automatically detect and connect to other Chorus weapons within 200 meters. This simplicity of deployment makes the system practical for ad-hoc team formation, though the networking also means stolen Chorus units could potentially join an enemy's network if not properly locked.",
    manufacturer: "TESSERA",
    tier_availability: "Tier 3+",
    legality: "Restricted — team weapons licensing",
    base_technologies: ["Distributed fire coordination AI", "Self-organizing weapon mesh network", "BCI team awareness overlay", "Automated sector assignment"],
    specifications: "caliber: 4.6mm caseless\neffective_range: 100m\nrate_of_fire: 850 rpm (cyclic)\nmagazine_capacity: 30 rounds\nweight: 1.9 kg",
    tactical_use: "The Chorus transforms a group of individual operators into a coordinated fire team without requiring extensive training or established communication procedures. The automated sector assignment and fire deconfliction prevent the most common team-fire errors — double-targeting and friendly fire — while the ammunition count sharing ensures teams know when a member needs to reload. In close-quarters clearing operations, the Chorus network allows silent coordination that verbal communication cannot match.",
    cultural_context: "The Chorus represents Tessera's belief that networking improves everything, applied to close-quarters combat. The weapon creates instant team cohesion among operators who may have never worked together — a valuable capability in the freelancer market where scratch teams are assembled for single operations. The flip side is dependency: Chorus operators who rely on the network struggle when forced to fight independently.",
    known_users: ["Tessera security teams", "PMC close-quarters units", "Freelance scratch teams"],
    story_hooks: [
      "A stolen Chorus unit auto-connected to a friendly team's network during a clearing operation, feeding the thief real-time tactical data on the team's positions and ammunition status.",
      "A freelance team's Chorus network was compromised by a signals intelligence unit that injected false position data, causing the team to fire into each other's assigned sectors."
    ],
    ammunition_type: ["4.6mm caseless standard"],
    tags: ["weapon", "smg", "networked", "team", "AI", "Tessera", "tier 3"]
  },
  {
    id: id(),
    name: "Volkov-Saito Precision VSK-8 'Rapier'",
    type: "weapon",
    aliases: ["Rapier", "VSK-8", "The Fencer's Choice"],
    category: "smg",
    description: "The VSK-8 Rapier is Volkov-Saito's answer to the question no one asked: what if an SMG were as elegant as a dueling weapon? The Rapier is a long-barreled, semi-automatic-only SMG chambered in 5.7mm caseless with a target-grade barrel and precision sights. It trades rate of fire for accuracy, eschewing automatic capability entirely in favor of placing each shot with surgical precision.\n\nThe weapon's ergonomics are inspired by competitive target pistols — a precisely angled grip, adjustable palm shelf, and match trigger set at 1.2 kilograms. The BCI integration provides wind-and-distance compensation that makes the Rapier effective at distances that would be optimistic for conventional SMGs. At 200 meters, a skilled Rapier operator can place shots in a 3-centimeter group.\n\nVolkov-Saito prices the Rapier at Φ6,000 and sells it to operators who view combat as a discipline rather than a desperate scramble. The weapon demands skill — without automatic fire to compensate for misses, every shot must count. This appeals to a certain type of operator and horrifies everyone else. In a market dominated by volume-of-fire weapons, the Rapier is an anachronism that refuses to be irrelevant.",
    manufacturer: "VOLKOV-SAITO PRECISION",
    tier_availability: "Tier 3+",
    legality: "Available — premium licensing",
    base_technologies: ["Target-grade barrel", "Competition ergonomics", "BCI wind-distance compensation", "Semi-automatic precision platform"],
    specifications: "caliber: 5.7mm caseless\neffective_range: 200m\nrate_of_fire: Semi-automatic only\nmagazine_capacity: 20 rounds\nweight: 1.7 kg",
    tactical_use: "The Rapier is a precision engagement weapon in an SMG form factor. Its semi-automatic operation demands disciplined marksmanship but rewards it with accuracy that embarrasses assault rifles at medium range. In environments where ammunition conservation and shot placement matter — hostage rescue, surgical strikes, and engagements near fragile infrastructure — the Rapier's precision is a tactical asset. In prolonged firefights against multiple opponents, it is the wrong tool.",
    cultural_context: "The Rapier has cultivated a following among operators who view marksmanship as an art. 'Rapier shooters' are respected for their skill and mocked for their pretension in roughly equal measure. The weapon appears in competitive shooting circuits where it dominates precision categories, and its aesthetic — all clean lines and purposeful geometry — has made it an object of admiration even among people who would never carry one into combat.",
    known_users: ["Precision-oriented operators", "Competition shooters", "Hostage rescue specialists", "Surgical strike teams"],
    story_hooks: [
      "A hostage situation was resolved by a Rapier operator who placed three shots through a 5cm gap in a barricade at 150 meters — a feat that would have been impossible with any other SMG.",
      "A Rapier competition is being used as a front for recruiting precision shooters into a black-ops assassination program. The top scorers receive very specific job offers."
    ],
    ammunition_type: ["5.7mm caseless match"],
    tags: ["weapon", "smg", "precision", "semi_auto", "competition", "Volkov-Saito", "tier 3"]
  },
  {
    id: id(),
    name: "Arcturus Defense Solutions Neural PDW ANPD-1 'Impulse'",
    type: "weapon",
    aliases: ["Impulse", "ANPD-1", "Think Gun"],
    category: "smg",
    description: "The ANPD-1 Impulse is the PDW variant of Tessera's neural firing concept, developed by Arcturus under a disputed technology-sharing agreement. Like the TNAS-1 Puppeteer, the Impulse fires exclusively through BCI neural command with no physical trigger. Unlike the Puppeteer, the Impulse is designed for instinctive close-quarters reaction rather than deliberate engagement.\n\nThe weapon's neural interpretation engine is calibrated for threat-response speed rather than accuracy — it fires when the operator's brain enters a threat-response state, with the targeting system directing rounds at whatever the operator's attention is focused on. The 4.6mm caseless rounds fire at 1,000 rpm in controlled bursts determined by the AI's assessment of the threat level. A minor threat receives a 3-round burst. A major threat receives the full magazine.\n\nArcturus markets the Impulse as the ultimate reactive defense weapon: it fires before the operator consciously decides to fire, leveraging the pre-conscious threat detection that the human brain performs faster than conscious thought. The ethical implications are staggering, and Arcturus' legal department maintains a dedicated team for Impulse-related liability cases.",
    manufacturer: "ARCTURUS DEFENSE SOLUTIONS",
    tier_availability: "Tier 4+",
    legality: "Highly restricted — requires neural weapons certification",
    base_technologies: ["Pre-conscious threat detection", "Neural-reactive firing system", "AI threat-level burst control", "Attention-directed targeting"],
    specifications: "caliber: 4.6mm caseless\neffective_range: 50m\nrate_of_fire: 1,000 rpm (AI-controlled bursts)\nmagazine_capacity: 25 rounds\nweight: 1.3 kg",
    tactical_use: "The Impulse fires faster than conscious reaction allows. Its pre-conscious threat detection engages targets during the operator's startle response, delivering firepower before the conscious mind has finished processing the threat. This speed advantage is decisive in ambush scenarios where the first fraction of a second determines survival. The AI burst control prevents the weapon from emptying its magazine on minor threats, preserving ammunition for the engagement's duration.",
    cultural_context: "The Impulse is the most controversial PDW ever manufactured. A weapon that fires before its operator decides to fire raises fundamental questions about agency, responsibility, and the definition of self-defense. Arcturus argues the weapon merely accelerates the operator's own intent. Critics argue it makes kill decisions on behalf of a brain that hasn't finished thinking. Legal systems across Meridian 88 are struggling to define liability for pre-conscious weapons discharge.",
    known_users: ["Arcturus threat-response teams", "High-risk executive protection", "Neural weapons specialists"],
    story_hooks: [
      "An Impulse operator killed an unarmed person whose sudden movement triggered the pre-conscious threat response. The operator's brain saw a threat that didn't exist, and the weapon acted on that false perception.",
      "A hacker is selling a neural signal that mimics the threat-response pattern — when broadcast to an Impulse operator's BCI, it causes the weapon to discharge at whatever they're looking at."
    ],
    ammunition_type: ["4.6mm caseless frangible"],
    tags: ["weapon", "smg", "PDW", "neural", "pre_conscious", "controversial", "Arcturus", "tier 4"]
  },
  {
    id: id(),
    name: "Meridian Munitions Compact Machine Pistol CMP-4 'Jackrabbit'",
    type: "weapon",
    aliases: ["Jackrabbit", "CMP-4", "Jack"],
    category: "smg",
    description: "The CMP-4 Jackrabbit is a machine pistol small enough to fit in a coat pocket and powerful enough to ruin someone's day at across-the-room distances. Chambered in 9mm caseless, the Jackrabbit fires from a 15-round magazine at 900 rpm with a tiny reciprocating bolt that generates impressive muzzle climb in automatic mode. Meridian includes a folding foregrip that drops from beneath the barrel for two-handed operation.\n\nThe Jackrabbit has no BCI integration, no electronic sights, and no onboard systems. It is a mechanical weapon in a digital age, and this is its selling point. At Φ800, it provides automatic firepower that cannot be hacked, disabled remotely, or tracked through its electronics. The weapon's manual safety, fixed iron sights, and mechanical operation make it the preferred backup weapon for operators who have been burned by electronic weapon failures.\n\nMeridian designed the CMP-4 for the operator who needs a weapon that works when everything else has been compromised. When EMP grenades have killed smart-links, when BCI hacks have locked out neural weapons, and when electronic sights have been spoofed, the Jackrabbit still fires when you pull the trigger. Simple. Reliable. Analog.",
    manufacturer: "MERIDIAN MUNITIONS",
    tier_availability: "Tier 1+",
    legality: "Available — standard licensing",
    base_technologies: ["Compact machine pistol design", "Zero-electronics operation", "Folding foregrip system"],
    specifications: "caliber: 9mm caseless\neffective_range: 30m\nrate_of_fire: 900 rpm (cyclic)\nmagazine_capacity: 15 rounds\nweight: 0.8 kg",
    tactical_use: "The Jackrabbit is a last-ditch weapon and EMP backup. Its zero-electronics design ensures functionality in environments where electronic warfare has disabled smart weapons. The compact size allows concealed carry as a backup regardless of primary weapon choice. Automatic fire is controllable only with the foregrip deployed and at very close range — beyond 15 meters, semi-automatic fire is more effective. The weapon excels as a surprise equalizer in situations where both parties expected to be unarmed.",
    cultural_context: "The Jackrabbit is the weapon that keeps honest people honest. Its presence in inner coat pockets across Meridian 88 represents the baseline of personal defense — a mechanical guarantee that technology cannot overrule. The weapon is so common in some Tier 2 zones that 'checking for Jacks' has become slang for frisking someone. Its simplicity has given it a retro charm that appeals to operators nostalgic for an age when weapons were just weapons.",
    known_users: ["Backup weapon carriers", "EMP-environment operators", "Covert operatives", "Anyone who distrusts electronics"],
    story_hooks: [
      "After an EMP attack disabled every smart weapon in a building, the only armed person was a janitor with a Jackrabbit in their coat. The corporate security team with their locked-out Cicadas had to negotiate with the janitor for protection.",
      "A black-market dealer is selling 'ghost Jackrabbits' — CMP-4s manufactured without serial numbers that cannot be traced through any database."
    ],
    ammunition_type: ["9mm caseless standard"],
    tags: ["weapon", "smg", "machine_pistol", "analog", "compact", "backup", "Meridian", "tier 1"]
  },
  {
    id: id(),
    name: "Crucible Industries Boarding SMG BSM-6 'Cutlass'",
    type: "weapon",
    aliases: ["Cutlass", "BSM-6", "Pirate Gun"],
    category: "smg",
    description: "The BSM-6 Cutlass is purpose-built for maritime and orbital boarding operations, featuring a waterproof sealed action, a built-in breaching tool at the muzzle, and an integrated magnetic clamp on the underside that locks the weapon to ferrous surfaces when the operator needs both hands free. The weapon fires 10mm caseless from a 20-round magazine with a controlled 550 rpm rate designed for deliberate room-clearing rather than suppressive spray.\n\nThe Cutlass's breaching tool is a hardened steel prow at the muzzle that functions as a prying bar, glass breaker, and emergency melee weapon. The BCI integration includes a structural analysis overlay that identifies weak points in doors, hatches, and bulkheads for the breaching tool, and a decompression risk calculator for orbital operations that warns operators before firing in areas with hull integrity concerns.\n\nCrucible developed the Cutlass in response to the growing piracy problem on Meridian 88's maritime trade routes and the security needs of orbital stations. At Φ3,500, it is priced for organizational procurement by security firms, ship crews, and station operators who need a weapon designed for the unique challenges of forced entry in sealed environments.",
    manufacturer: "CRUCIBLE INDUSTRIES",
    tier_availability: "Tier 2+",
    legality: "Available — maritime/orbital security licensing",
    base_technologies: ["Sealed waterproof action", "Integrated breaching tool", "Magnetic weapon clamp", "BCI structural analysis overlay"],
    specifications: "caliber: 10mm caseless\neffective_range: 50m\nrate_of_fire: 550 rpm (cyclic)\nmagazine_capacity: 20 rounds\nweight: 2.5 kg",
    tactical_use: "The Cutlass is a specialist boarding weapon. Its breaching tool eliminates the need for a separate entry tool, its magnetic clamp frees hands for climbing and movement through zero-G environments, and its sealed action functions after full submersion. The 10mm round provides the stopping power needed to end close-quarters engagements quickly in confined spaces where prolonged firefights risk structural damage. The decompression calculator prevents operators from creating lethal hull breaches with their own weapons.",
    cultural_context: "The Cutlass has been adopted by both sides of maritime conflict — security forces and pirates alike carry BSM-6s because the weapon is simply the best tool for the job. Crucible officially condemns piracy while acknowledging that they cannot control secondary market sales. In port communities, the Cutlass is as common as a boat hook, and its distinctive muzzle prow makes it instantly recognizable.",
    known_users: ["Maritime security forces", "Orbital station boarding teams", "Ship crews", "Pirates (unofficial)"],
    story_hooks: [
      "A pirate crew's Cutlass weapons all share sequential serial numbers — they were stolen directly from a Crucible shipment meant for a security firm. The firm is demanding Crucible replace them, and Crucible is demanding the firm explain how an entire shipment was hijacked.",
      "An orbital station boarding team discovered their Cutlass decompression calculators had been miscalibrated. If they had fired in the section they were clearing, they would have breached the hull and killed everyone aboard."
    ],
    ammunition_type: ["10mm caseless standard", "10mm caseless frangible"],
    tags: ["weapon", "smg", "boarding", "maritime", "orbital", "breaching", "Crucible", "tier 2"]
  },
  {
    id: id(),
    name: "Kang-Petrov Arms KPS-7V 'Verdict'",
    type: "weapon",
    aliases: ["Verdict", "KPS-7V", "Judge"],
    category: "smg",
    description: "The KPS-7V Verdict is Kang-Petrov's heavy-caliber SMG, chambered in .45 ACP caseless — an old cartridge concept rendered in modern caseless form. The Verdict prioritizes stopping power over penetration, delivering heavy, slow projectiles that transfer maximum energy to the target. Against unarmored opponents, the .45 caseless round is devastating. Against body armor, it is a firm shove.\n\nThe weapon's design is deliberately retro-futurist, with angular polymer furniture over a stamped steel receiver that echoes mid-20th-century submachine guns. Kang-Petrov's design team included historical weapons enthusiasts who wanted to prove that heavy-caliber SMGs remained tactically relevant in an era of high-velocity micro-calibers. The Verdict's 25-round magazine and 600 rpm cyclic rate provide sustained close-range firepower with terminal effects that smaller calibers struggle to match.\n\nAt Φ1,500, the Verdict appeals to operators who value knockdown power and operators who appreciate the weapon's aggressive aesthetic. In Tier 2-3 zones where body armor is rare, the .45 caseless round's stopping power makes the Verdict genuinely terrifying at close range. In zones where armor is common, it is a relic outclassed by armor-piercing alternatives.",
    manufacturer: "KANG-PETROV ARMS",
    tier_availability: "Tier 1+",
    legality: "Available — standard licensing",
    base_technologies: ["Heavy-caliber caseless conversion", "Retro-futurist design language", "Maximum energy transfer optimization"],
    specifications: "caliber: .45 ACP caseless\neffective_range: 75m\nrate_of_fire: 600 rpm (cyclic)\nmagazine_capacity: 25 rounds\nweight: 2.6 kg",
    tactical_use: "The Verdict excels against unarmored targets at close range. Its heavy .45 caseless round delivers stopping power that lighter calibers cannot match, putting targets down with fewer hits. In environments where body armor is uncommon — Tier 1-2 zones, criminal enforcement, wildlife defense — the Verdict's terminal performance is superior to lighter-caliber alternatives. Against armored opponents, operators should switch to AP-capable platforms.",
    cultural_context: "The Verdict has a cult following driven by its combination of retro aesthetics and modern performance. Weapons collectors prize its design language, and street operators appreciate its visceral stopping power. The weapon has appeared in several popular media properties, boosting its cultural visibility beyond its tactical merits. In some communities, carrying a Verdict is a fashion statement as much as a tactical choice.",
    known_users: ["Street-level operators", "Tier 1-2 enforcers", "Weapons collectors", "Retro-tech enthusiasts"],
    story_hooks: [
      "A media personality popularized the Verdict in a streaming series, causing demand to spike 400%. Kang-Petrov can't manufacture them fast enough, and counterfeit Verdicts are flooding the market with dangerous quality variations.",
      "A series of enforcement killings all used .45 ACP caseless ammunition — a caliber rare enough that the supply chain is traceable to a single distributor."
    ],
    ammunition_type: [".45 ACP caseless standard", ".45 ACP caseless hollow-point"],
    tags: ["weapon", "smg", "heavy_caliber", "retro", "stopping_power", "Kang-Petrov", "tier 1"]
  },
  {
    id: id(),
    name: "Tessera Swarm Micro-PDW TSM-4 'Locust'",
    type: "weapon",
    aliases: ["Locust", "TSM-4", "Swarm Gun"],
    category: "smg",
    description: "The TSM-4 Locust fires micro-flechettes rather than conventional projectiles — 1mm tungsten needles accelerated electromagnetically from a 200-round cassette. Each trigger pull releases a burst of 5 flechettes in a controlled spread pattern, and the BCI smart-link adjusts the spread width based on range to target. At conversational distance, the spread is a 2cm circle. At 50 meters, it expands to a 30cm pattern.\n\nThe Locust's electromagnetic acceleration system draws from an internal capacitor that recharges from a belt-mounted power cell, providing approximately 40 bursts (200 flechettes) per charge. The micro-flechettes penetrate soft body armor through cumulative damage — individual needles are marginally effective, but a burst of five creates a wound channel that defeats fiber-based armor through sheer number of penetration points.\n\nTessera designed the Locust for close-protection scenarios where collateral damage must be minimized. The micro-flechettes lose lethality rapidly beyond 50 meters and cannot penetrate hard barriers, making them ideal for environments where overpenetration threatens bystanders or infrastructure. At Φ11,000, the weapon is expensive and its ammunition is proprietary — a deliberate choice by Tessera to maintain control over the supply chain.",
    manufacturer: "TESSERA",
    tier_availability: "Tier 4+",
    legality: "Restricted — specialized weapons licensing",
    base_technologies: ["Electromagnetic flechette acceleration", "BCI spread-width control", "Micro-flechette cassette feed", "Cumulative armor defeat"],
    specifications: "caliber: 1mm tungsten micro-flechette\neffective_range: 50m\nrate_of_fire: 200 bursts/min (5 flechettes per burst)\nmagazine_capacity: 200 flechettes (40 bursts)\nweight: 1.4 kg",
    tactical_use: "The Locust provides close-quarters lethality with minimal overpenetration risk. The BCI-controlled spread pattern ensures maximum hit probability at all engagement distances while the micro-flechettes' rapid energy loss beyond 50 meters eliminates collateral risk to distant bystanders. The cumulative armor defeat mechanism makes the weapon effective against soft body armor while remaining unable to penetrate hard barriers or structural elements. In close-protection scenarios, the Locust allows engagement of threats without endangering the principal.",
    cultural_context: "The Locust is a weapon that provokes horror in medical professionals. Micro-flechette wounds are nightmarish to treat — dozens of 1mm tungsten needles embedded in tissue, many too small to image on standard medical scanners, each one a potential infection vector or delayed hemorrhage source. Anti-weapons campaigners have targeted the Locust specifically, calling it a weapon designed to maximize suffering. Tessera responds that the weapon is designed to minimize collateral damage. Both are correct.",
    known_users: ["Close-protection specialists", "VIP security details", "Tessera special operations"],
    story_hooks: [
      "A Locust victim survived the initial engagement but is dying slowly as embedded micro-flechettes migrate through tissue toward vital organs. The only scanner capable of locating them all belongs to the corponation that ordered the hit.",
      "Someone has reverse-engineered the Locust's proprietary flechette cassettes and is manufacturing bootleg ammunition. The bootleg flechettes are made from surgical steel instead of tungsten — they're magnetic, which makes them detectable but also means they interact unpredictably with anyone carrying ferromagnetic cyberware."
    ],
    ammunition_type: ["1mm tungsten micro-flechette cassette"],
    tags: ["weapon", "smg", "flechette", "electromagnetic", "micro", "Tessera", "tier 4"]
  },
  {
    id: id(),
    name: "Hearthstone Firearms Ranch SMG HRS-3 'Cattleman'",
    type: "weapon",
    aliases: ["Cattleman", "HRS-3", "Ranch Gun"],
    category: "smg",
    description: "The HRS-3 Cattleman is Hearthstone's only automatic weapon — a simple, robust 9mm submachine gun designed for agricultural defense against wildlife threats and livestock predators. The weapon features a semi-auto/three-round burst selector with no full-auto option, keeping the rate of fire controlled for untrained ranch operators. The 20-round magazine provides sufficient capacity for most encounters without the bulk of military magazines.\n\nTrue to Hearthstone's philosophy, the Cattleman has no electronic components. Iron sights, manual safety, mechanical trigger. The weapon is finished in a corrosion-resistant matte ceramic coating designed for years of outdoor storage in ranch buildings and vehicle gun racks. The action is intentionally over-gassed to cycle reliably with dirty, corroded, or low-quality ammunition — the kind of ammunition available at frontier trading posts.\n\nAt Φ500, the Cattleman fills the gap between Hearthstone's semi-auto Hearth Guard and the security-oriented weapons of larger manufacturers. It provides controlled automatic firepower to communities that face threats from modified wildlife, feral synthetics, and the occasional human predator. The burst limiter prevents untrained users from dumping ammunition in panic, imposing discipline that their training does not.",
    manufacturer: "HEARTHSTONE FIREARMS",
    tier_availability: "Tier 1+",
    legality: "Unrestricted — agricultural/civilian grade",
    base_technologies: ["Burst-limited fire control", "Corrosion-resistant ceramic coating", "Over-gassed dirty-ammunition tolerance"],
    specifications: "caliber: 9mm brass-cased\neffective_range: 50m\nrate_of_fire: Three-round burst, ~750 rpm burst rate\nmagazine_capacity: 20 rounds\nweight: 2.0 kg",
    tactical_use: "The Cattleman provides controlled firepower for non-military users facing close-range threats. The three-round burst delivers enough rounds to neutralize a threat without the ammunition waste of full-automatic fire. The dirty-ammunition tolerance ensures reliability with frontier-quality supplies, and the ceramic coating survives storage conditions that would corrode conventional finishes. Against human threats, the Cattleman's burst fire provides a meaningful improvement over semi-automatic weapons for untrained shooters.",
    cultural_context: "The Cattleman is the ranch weapon — the gun that lives in a truck cab or hangs by the barn door. It is as much a part of frontier agricultural life as fencing tools and water purifiers. Hearthstone designed it for people who think of weapons as tools rather than identities, and the Cattleman's utilitarian no-nonsense design reflects that philosophy. No one boasts about carrying a Cattleman. Everyone is glad to have one when they need it.",
    known_users: ["Frontier ranchers", "Agricultural cooperatives", "Rural settlement defenders", "Livestock guards"],
    story_hooks: [
      "Modified predatory wildlife has been attacking frontier ranches with increasing coordination. The standard Cattleman load is no longer sufficient — the ranchers need heavier weapons, but they can't afford them.",
      "Hearthstone received a bulk order for 2,000 Cattlemen from a 'farming cooperative' that doesn't appear to exist. The shipping address is in a Tier 3 urban zone with no agricultural operations."
    ],
    ammunition_type: ["9mm brass-cased standard"],
    tags: ["weapon", "smg", "agricultural", "frontier", "burst_fire", "Hearthstone", "tier 1"]
  },
  {
    id: id(),
    name: "Volkov-Saito Precision VSC-5 'Venom'",
    type: "weapon",
    aliases: ["Venom", "VSC-5", "Poison SMG"],
    category: "smg",
    description: "The VSC-5 Venom is a suppressed precision SMG designed for targeted elimination at close range. Volkov-Saito built the weapon for operators who need to kill a specific person quietly, without alerting adjacent rooms or triggering acoustic monitoring. The integrally suppressed 5.7mm subsonic platform produces less than 70 decibels at the muzzle — quieter than a normal conversation.\n\nThe Venom's BCI integration includes a biometric targeting system that can be pre-loaded with a target's physiological profile — height, build, gait pattern, thermal signature. The system identifies the pre-loaded target in the operator's neural display and confirms target acquisition before allowing the weapon to fire. This pre-confirmation system prevents misidentification in low-light conditions and ensures the right person is engaged.\n\nVolkov-Saito does not publicly acknowledge the Venom's existence. The weapon is not listed in their catalog, does not appear on their website, and is sold exclusively through direct contact with Volkov-Saito's 'special applications' division. At an estimated Φ20,000, the Venom is priced for customers with specific needs and the budget to meet them. Its existence is known primarily through its results.",
    manufacturer: "VOLKOV-SAITO PRECISION",
    tier_availability: "Tier 5",
    legality: "Illegal — assassination-grade weapon",
    base_technologies: ["Sub-conversational suppression", "Biometric target confirmation", "Pre-loaded physiological profiling", "Neural target-lock fire authorization"],
    specifications: "caliber: 5.7mm subsonic caseless\neffective_range: 75m\nrate_of_fire: Semi-automatic only\nmagazine_capacity: 15 rounds\nweight: 1.8 kg",
    tactical_use: "The Venom is designed for one purpose: targeted killing with zero acoustic signature. Its sub-conversational noise level allows engagement in hotel corridors, office buildings, and residential areas without alerting anyone outside the immediate vicinity. The biometric targeting system prevents collateral kills that would draw investigation, ensuring that only the intended target is engaged. The semi-automatic-only operation reinforces the weapon's role as a precision tool, not a combat weapon.",
    cultural_context: "The Venom exists in the shadows. Operators who carry one do not discuss it. Organizations that procure them do not acknowledge the purchase. The weapon's biometric targeting system creates a disturbing intimacy between weapon and target — the operator must know their target's body to program the confirmation system, turning assassination into something approaching a relationship. In intelligence circles, receiving a biometric profile along with a mission brief means a Venom engagement is planned.",
    known_users: ["Classified"],
    story_hooks: [
      "A Venom was recovered with its biometric targeting system still loaded with its last target's profile. The profile matches someone who is still alive — the operator missed, and now the target knows exactly who they are.",
      "Volkov-Saito's special applications division has been compromised. The client list for every Venom ever sold is being auctioned to the highest bidder."
    ],
    ammunition_type: ["5.7mm subsonic caseless"],
    tags: ["weapon", "smg", "assassination", "suppressed", "biometric", "classified", "Volkov-Saito", "tier 5"]
  },
  {
    id: id(),
    name: "Arcturus Defense Solutions Crowd Control SMG ACSM-3 'Shepherd'",
    type: "weapon",
    aliases: ["Shepherd", "ACSM-3", "Crowd Gun"],
    category: "smg",
    description: "The ACSM-3 Shepherd is a dual-feed SMG that accepts two magazines simultaneously — one loaded with lethal 9mm caseless rounds and one with rubber-composite less-lethal projectiles. The BCI fire-control system allows the operator to switch between lethal and less-lethal ammunition through neural command, selecting the appropriate response to each individual in a crowd without changing weapons or magazines.\n\nThe weapon's BCI overlay integrates with Arcturus' crowd-analysis AI, which categorizes individuals in real-time as passive, agitated, or hostile. The system recommends lethal or less-lethal response for each categorized individual, and the operator confirms or overrides per-target. In practice, a Shepherd operator can fire rubber rounds at protesters while switching to lethal ammunition for armed individuals within the same trigger squeeze.\n\nArcturus designed the Shepherd for corporate police crowd-control operations, replacing the two-weapon system (lethal rifle plus less-lethal launcher) with a single platform. At Φ5,500, the weapon is institutional procurement only. The dual-feed mechanism is mechanically complex and requires regular maintenance, but the tactical flexibility it provides has made the ACSM-3 standard issue for Arcturus crowd-management units.",
    manufacturer: "ARCTURUS DEFENSE SOLUTIONS",
    tier_availability: "Tier 3+",
    legality: "Restricted — law enforcement crowd control",
    base_technologies: ["Dual-feed magazine system", "BCI lethal/less-lethal toggle", "Crowd-analysis AI integration", "Per-target response selection"],
    specifications: "caliber: 9mm caseless (lethal) / 9mm rubber-composite (less-lethal)\neffective_range: 50m (lethal), 30m (less-lethal)\nrate_of_fire: 600 rpm (cyclic)\nmagazine_capacity: 20+20 rounds (dual magazine)\nweight: 2.7 kg",
    tactical_use: "The Shepherd allows continuous crowd-control operations with escalating force options available per trigger pull. Operators can suppress non-lethal threats with rubber rounds while immediately engaging armed threats with lethal ammunition, maintaining a single point of aim and eliminating the delay of weapon transitions. The crowd-analysis AI provides real-time threat categorization, but operators retain final authority on ammunition selection for each target.",
    cultural_context: "The Shepherd is the most visible weapon of corporate crowd control. Its dual-feed system has been criticized as enabling casual escalation — the ease of switching between lethal and less-lethal ammunition lowers the psychological barrier to lethal force. Protesters have learned to identify Shepherd operators by the distinctive dual-magazine profile, and the weapon has become a symbol of the corporate police state's capacity for selective violence within crowds.",
    known_users: ["Arcturus corporate police", "Crowd-management units", "Civil unrest response teams"],
    story_hooks: [
      "A Shepherd operator's BCI was hacked to display all crowd members as 'hostile' — the operator fired lethal rounds into a peaceful protest before realizing the overlay was compromised.",
      "The crowd-analysis AI has been trained on biased data that categorizes certain demographics as 'agitated' regardless of their actual behavior, leading to discriminatory less-lethal targeting."
    ],
    ammunition_type: ["9mm caseless lethal", "9mm rubber-composite less-lethal"],
    tags: ["weapon", "smg", "crowd_control", "dual_feed", "less_lethal", "law_enforcement", "Arcturus", "tier 3"]
  },
  {
    id: id(),
    name: "Meridian Munitions Twin PDW MTP-2 'Gemini'",
    type: "weapon",
    aliases: ["Gemini", "MTP-2", "Twin Gun", "Double Tap"],
    category: "smg",
    description: "The MTP-2 Gemini is a paired weapon system — two identical micro-PDWs sold as a matched set and designed to be dual-wielded with BCI coordination. Each unit is a compact 5.7mm caseless PDW with a 15-round magazine, but the weapons' BCI systems communicate to create a unified fire-control solution. When both weapons are drawn, the smart-link allocates targeting between left and right hands, allowing the operator to engage two separate targets simultaneously.\n\nDual-wielding weapons is generally impractical — human neurology isn't optimized for independent bilateral targeting. The Gemini's BCI system compensates by handling the targeting math neurally, splitting the operator's attention between two aim points and providing independent aim-assist to each hand. The system requires extensive training and a Class 4 motor-interface BCI, but trained Gemini operators can engage two targets in different directions with the speed of a single-weapon draw.\n\nAt Φ8,000 per pair, the Gemini is a specialist system for operators with the neural hardware and training to exploit its capabilities. In untrained hands, the paired weapons offer no advantage over a single PDW with a larger magazine. In trained hands, the Gemini doubles the operator's engagement capacity at close range.",
    manufacturer: "MERIDIAN MUNITIONS",
    tier_availability: "Tier 3+",
    legality: "Available — premium licensing",
    base_technologies: ["Paired weapon BCI coordination", "Bilateral targeting AI", "Independent dual aim-assist", "Cross-linked fire control"],
    specifications: "caliber: 5.7mm caseless\neffective_range: 75m per unit\nrate_of_fire: 850 rpm per unit (cyclic)\nmagazine_capacity: 15 rounds per unit (30 total)\nweight: 1.1 kg per unit (2.2 kg total)",
    tactical_use: "The Gemini enables simultaneous multi-target engagement at close range. Trained operators can neutralize two threats in the time it takes a single-weapon operator to engage one. The paired system excels in close-protection scenarios where threats may approach from multiple directions simultaneously. The bilateral targeting AI handles the coordination that human neurology cannot naturally perform, making practical what would otherwise be cinematic fantasy.",
    cultural_context: "The Gemini occupies a strange space between practical weapon and performance art. Dual-wielding has been cinematic fantasy for centuries, and the Gemini makes it functional reality through neural technology. Gemini operators attract attention — the sight of someone drawing and firing two weapons simultaneously is viscerally impressive. This visibility cuts both ways: in covert operations, the distinctive dual-draw is a signature that identifies the operator's equipment and capability.",
    known_users: ["Specialist close-protection operators", "Exhibition shooters", "High-tier freelancers"],
    story_hooks: [
      "A Gemini operator's BCI link to one weapon was severed mid-engagement, causing them to fire wildly with their off-hand while maintaining precision with their dominant hand. The split-second of uncontrolled fire hit a bystander.",
      "An underground fighting circuit features Gemini operators in duels — the first competitor to score hits with both weapons simultaneously wins. The matches are illegal, broadcast, and enormously popular."
    ],
    ammunition_type: ["5.7mm caseless standard", "5.7mm caseless AP"],
    tags: ["weapon", "smg", "PDW", "dual_wield", "paired", "BCI", "Meridian", "tier 3"]
  },
  {
    id: id(),
    name: "Crucible Industries Emergency SMG ESM-1 'Breakglass'",
    type: "weapon",
    aliases: ["Breakglass", "ESM-1", "Emergency Gun", "The Red Box"],
    category: "smg",
    description: "The ESM-1 Breakglass is a single-use emergency SMG stored in a sealed wall-mounted case alongside fire extinguishers and first-aid kits in corporate facilities, orbital stations, and government buildings. The weapon is factory-loaded with a sealed 30-round magazine of 9mm frangible ammunition and cannot be reloaded — once the magazine is empty, the weapon is spent. The case is sealed with a tamper-evident breakaway panel and alarmed to notify security when opened.\n\nThe Breakglass requires no training to operate. The sealed case contains pictographic instructions, the weapon has a single control (safety/fire selector), and the BCI-compatible grip provides basic aim-assist to anyone with even a rudimentary neural interface. The weapon is brightly colored in emergency orange with reflective strips, ensuring it is never mistaken for anything other than what it is.\n\nCrucible designed the Breakglass for catastrophic scenarios where security has been overwhelmed and untrained personnel need immediate access to a firearm. At Φ200 per unit installed, facilities order them by the hundreds. The weapons are inspected annually and replaced every five years. Most are never used. The ones that are used tend to generate stories worth telling.",
    manufacturer: "CRUCIBLE INDUSTRIES",
    tier_availability: "Tier 2+",
    legality: "Facility installation — special emergency licensing",
    base_technologies: ["Single-use sealed weapon system", "Pictographic no-training operation", "Tamper-evident alarmed case", "Universal BCI aim-assist"],
    specifications: "caliber: 9mm frangible caseless\neffective_range: 25m\nrate_of_fire: 600 rpm (cyclic)\nmagazine_capacity: 30 rounds (sealed, non-reloadable)\nweight: 1.5 kg",
    tactical_use: "The Breakglass is a last resort for facilities where security has failed. Its single-use design prevents unauthorized stockpiling, its sealed case provides accountability, and its simplified operation allows untrained personnel to deliver effective close-range fire. The frangible ammunition eliminates overpenetration risk in facility environments. The weapon's orange color and alarm system prevent covert removal but ensure visibility when needed.",
    cultural_context: "The Breakglass is a constant reminder that catastrophic violence is always possible. Its presence on facility walls — next to fire extinguishers and defibrillators — normalizes the expectation that civilians may need to fight. Some see this as responsible preparedness. Others see it as a dystopian admission that the social contract has failed so thoroughly that office workers need access to submachine guns. Both perspectives have merit.",
    known_users: ["Emergency civilian use", "Facility personnel in crisis", "Anyone with access to the wall-mounted case"],
    story_hooks: [
      "During a corporate facility siege, an accountant broke the Breakglass case and held off attackers long enough for security to arrive. The weapon's 30 rounds were exactly enough. The accountant has no memory of firing.",
      "Someone has been systematically replacing Breakglass units in a facility with non-functional replicas. When the emergency comes, every wall-mounted weapon will fail."
    ],
    ammunition_type: ["9mm frangible caseless (sealed)"],
    tags: ["weapon", "smg", "emergency", "single_use", "facility", "civilian", "Crucible", "tier 2"]
  },
];

// ═══════════════════════════════════════════════════════
// DMRs / DESIGNATED MARKSMAN RIFLES (20)
// ═══════════════════════════════════════════════════════
const dmrs = [
  {
    id: id(),
    name: "Volkov-Saito Precision VSM-14 'Carthage'",
    type: "weapon",
    aliases: ["Carthage", "VSM-14", "City Killer"],
    category: "dmr",
    description: "The VSM-14 Carthage is Volkov-Saito's flagship designated marksman rifle, built around a 6.5mm Creedmoor caseless cartridge that delivers sub-half-MOA accuracy at 1,000 meters. The rifle's precision-lapped barrel is cryogenically treated and stress-relieved, producing consistent harmonics that eliminate shot-to-shot variation. The action is a short-stroke gas piston with a three-lug rotating bolt that locks into battery with mechanical precision measurable in microns.\n\nThe Carthage's BCI integration features Volkov-Saito's advanced weapon-learning system, building not just a ballistic profile for the individual rifle but a combined rifle-and-operator profile that accounts for the specific shooter's neural response patterns, breathing rhythm, and trigger technique. Over hundreds of rounds, the system creates a fusion model that makes the rifle and operator function as a single precision instrument.\n\nAt Φ22,000, the Carthage is Volkov-Saito's most expensive non-custom weapon. Its customers are professional designated marksmen operating in corporate military, law enforcement, and freelance precision roles. The rifle demands excellence from its operator and rewards it with performance that approaches the theoretical limits of its cartridge. In competitive long-range shooting, the Carthage is the benchmark against which all other DMRs are measured.",
    manufacturer: "VOLKOV-SAITO PRECISION",
    tier_availability: "Tier 4+",
    legality: "Restricted — precision weapons licensing",
    base_technologies: ["Cryogenically treated precision barrel", "Weapon-learning operator fusion", "Short-stroke gas piston", "Neural-predictive fire control"],
    specifications: "caliber: 6.5mm Creedmoor caseless\neffective_range: 1,000m\nrate_of_fire: Semi-automatic\nmagazine_capacity: 20 rounds\nweight: 4.8 kg",
    tactical_use: "The Carthage extends the designated marksman's effective range to distances that blur the line between DMR and sniper rifle. The weapon-learning operator fusion system reduces shot-to-shot variation to near-mechanical levels, allowing the human-rifle team to deliver consistent precision fire at ranges where environmental variables dominate. In squad-level operations, a Carthage-equipped marksman provides overwatch and precision engagement capabilities that force opposing forces to respect distance.",
    cultural_context: "The Carthage is the aspirational weapon of precision shooting culture. Owning one signals commitment to the craft of long-range marksmanship, and the weapon-learning system creates a bond between operator and rifle that many describe as intimate. Carthage operators develop a relationship with their specific weapon that transcends normal equipment attachment — the rifle knows them, and they know the rifle. Trading or selling a profiled Carthage is considered almost taboo.",
    known_users: ["Professional designated marksmen", "Corporate military precision units", "Elite freelance marksmen"],
    story_hooks: [
      "A Carthage with 10,000 rounds of operator-fusion data was stolen. The thief cannot use the weapon effectively — the fusion profile doesn't match their neurology — but the data itself reveals the original operator's identity, habits, and psychological patterns.",
      "Volkov-Saito has discovered that Carthage weapon-learning data, when analyzed in aggregate across hundreds of rifles, reveals patterns in human neurology that could be used to predict behavior. The military applications are obvious and terrifying."
    ],
    ammunition_type: ["6.5mm Creedmoor caseless match", "6.5mm Creedmoor caseless AP"],
    tags: ["weapon", "dmr", "precision", "weapon_learning", "Volkov-Saito", "tier 4"]
  },
  {
    id: id(),
    name: "Arcturus Defense Solutions ADM-10 'Overseer'",
    type: "weapon",
    aliases: ["Overseer", "ADM-10", "Big Brother"],
    category: "dmr",
    description: "The ADM-10 Overseer is Arcturus' corporate security DMR, designed to provide precision overwatch for corporate facility perimeters and urban security operations. The rifle fires 6.5mm caseless from a 20-round magazine and features an integrated smart-scope that combines optical magnification with BCI-enhanced target identification, distance ranging, wind measurement, and ballistic calculation in a single neural overlay.\n\nThe Overseer's smart-scope connects to Arcturus' corporate surveillance network, overlaying real-time intelligence on the marksman's field of view. Security camera feeds, personnel tracking data, threat assessments, and engagement authorization status are all displayed in the BCI neural overlay. The marksman sees not just their target but the entire tactical picture, making informed engagement decisions without relying on radio communication.\n\nAt Φ14,000, the Overseer is priced for institutional procurement. Arcturus deploys them at corporate campuses, industrial facilities, and anywhere their interests require precision overwatch. The weapon's surveillance integration makes it as much an intelligence tool as a weapon — the marksman provides eyes and analysis as well as lethal capability. In Arcturus doctrine, the designated marksman is the team's most information-rich position.",
    manufacturer: "ARCTURUS DEFENSE SOLUTIONS",
    tier_availability: "Tier 3+",
    legality: "Restricted — corporate security overwatch authorization",
    base_technologies: ["Integrated BCI smart-scope", "Surveillance network overlay", "Real-time intelligence feed", "Automated ballistic calculation"],
    specifications: "caliber: 6.5mm caseless\neffective_range: 800m\nrate_of_fire: Semi-automatic\nmagazine_capacity: 20 rounds\nweight: 4.5 kg",
    tactical_use: "The Overseer provides precision fire support integrated with real-time intelligence. The marksman's surveillance overlay allows threat identification and engagement authorization without radio communication, reducing response time and preventing miscommunication errors. The smart-scope's automated ballistic calculation delivers firing solutions for any range and wind condition within the weapon's envelope, allowing rapid engagement of multiple targets at varying distances.",
    cultural_context: "The Overseer embodies Arcturus' approach to security: total information dominance expressed through precision violence. The weapon's integration with corporate surveillance networks means the marksman is watching everything the cameras see, knowing everything the database knows about every person in their field of fire. For corporate personnel, the Overseer's presence is reassuring. For everyone else, it is the knowledge that somewhere above, someone who knows your name is looking at you through a scope.",
    known_users: ["Arcturus corporate overwatch teams", "Facility perimeter security", "Urban security marksmen"],
    story_hooks: [
      "An Overseer marksman's surveillance overlay was compromised, displaying falsified intelligence that led them to engage an undercover operative who was actually on the same side.",
      "The Overseer's surveillance feed has been recording and storing everything the marksman sees for years. This accumulated visual intelligence — including private moments witnessed from overwatch positions — has been discovered by a data thief."
    ],
    ammunition_type: ["6.5mm caseless match", "6.5mm caseless AP"],
    tags: ["weapon", "dmr", "surveillance", "smart_scope", "corporate", "overwatch", "Arcturus", "tier 3"]
  },
  {
    id: id(),
    name: "Tessera Networked DMR TNDM-6 'Oracle'",
    type: "weapon",
    aliases: ["Oracle", "TNDM-6", "Prophet Rifle"],
    category: "dmr",
    description: "The TNDM-6 Oracle is Tessera's networked designated marksman rifle, extending their distributed targeting philosophy to precision engagement. When multiple Oracles are deployed, the weapons share ballistic data — wind readings from one position correct calculations at another, and target tracking data from multiple angles provides three-dimensional position estimates that no single marksman could achieve.\n\nThe Oracle fires 7mm caseless from a precision-machined barrel, and its BCI smart-scope incorporates Tessera's predictive targeting AI. The AI models target movement patterns and generates firing solutions that lead moving targets at distances where human prediction fails. When networked with other Oracles, the predictive model improves as multiple observation angles refine the target's movement vector.\n\nTessera sells the Oracle at Φ16,000 per unit, with squad packs of four available at Φ58,000. The networking protocol automatically distributes the most accurate ballistic data to the marksman with the best engagement angle, creating a dynamic fire-control solution that constantly optimizes which marksman should take which shot. In practice, Oracle teams achieve engagement rates that exceed the sum of their individual capabilities.",
    manufacturer: "TESSERA",
    tier_availability: "Tier 4+",
    legality: "Restricted — networked weapons licensing",
    base_technologies: ["Distributed ballistic data sharing", "Predictive target movement AI", "Multi-angle position estimation", "Dynamic fire-control optimization"],
    specifications: "caliber: 7mm caseless\neffective_range: 900m\nrate_of_fire: Semi-automatic\nmagazine_capacity: 15 rounds\nweight: 4.6 kg",
    tactical_use: "The Oracle transforms a group of marksmen into a precision fire network. Shared ballistic data eliminates individual measurement errors, predictive targeting AI defeats evasive movement, and dynamic fire-control optimization ensures each shot is taken by the marksman with the highest probability of success. In overwatch scenarios with multiple elevated positions, an Oracle network provides interlocking precision fire that is nearly impossible to evade.",
    cultural_context: "The Oracle represents Tessera's conviction that networking improves every military capability. Critics argue that networked marksmen lose the individual judgment and initiative that make traditional marksmen effective. Tessera responds with engagement statistics. The philosophical debate between networked collective capability and individual excellence is nowhere more sharply drawn than in the precision shooting community, where marksmanship is traditionally the most individualistic of military skills.",
    known_users: ["Tessera precision teams", "Corporate military overwatch", "Networked security deployments"],
    story_hooks: [
      "An Oracle network was hacked to inject a false target — the system displayed a phantom hostile that multiple marksmen engaged simultaneously, wasting ammunition and revealing their positions.",
      "Two opposing forces both deployed Oracle networks. The weapons accidentally connected to each other's mesh, and for a brief moment both sides had complete visibility of the other's positions and targeting data."
    ],
    ammunition_type: ["7mm caseless match", "7mm caseless AP"],
    tags: ["weapon", "dmr", "networked", "AI", "predictive", "Tessera", "tier 4"]
  },
  {
    id: id(),
    name: "Crucible Industries Field Marksman Rifle FMR-8 'Sentinel'",
    type: "weapon",
    aliases: ["Sentinel", "FMR-8", "The Watch"],
    category: "dmr",
    description: "The FMR-8 Sentinel is Crucible Industries' answer to precision rifles that cost more than a house. At Φ4,500, the Sentinel provides 1-MOA accuracy at 800 meters using conventional optics and mechanical precision rather than BCI-dependent electronics. The rifle chambers 7.62mm caseless through a cold-hammer-forged barrel with a traditional free-floating handguard and adjustable bipod.\n\nThe Sentinel's design philosophy is deliberate simplicity. The scope is a high-quality conventional optic with illuminated reticle — no BCI link, no electronic rangefinding, no ballistic computer. The marksman estimates wind, calculates range, and dials their scope using skills rather than software. This makes the FMR-8 immune to electronic warfare, EMP, and BCI disruption while demanding a higher level of fundamental marksmanship from its operator.\n\nCrucible markets the Sentinel to frontier defense forces, independent militia marksmen, and operators who distrust electronic precision aids. The weapon rewards training and experience over technology budget, creating a level of mastery that cannot be purchased. In a market dominated by smart-scoped wonder weapons, the Sentinel is a reminder that the fundamentals of marksmanship haven't changed — technology has simply made them optional for those who can afford it.",
    manufacturer: "CRUCIBLE INDUSTRIES",
    tier_availability: "Tier 2+",
    legality: "Available — standard licensing",
    base_technologies: ["Cold-hammer-forged barrel", "Conventional precision optics", "Zero-electronics design", "Free-floating barrel system"],
    specifications: "caliber: 7.62mm caseless\neffective_range: 800m\nrate_of_fire: Semi-automatic\nmagazine_capacity: 20 rounds\nweight: 4.7 kg",
    tactical_use: "The Sentinel provides reliable precision fire independent of electronic infrastructure. In EMP-contested environments, after BCI disruption, or in remote areas without network coverage, the FMR-8 continues to deliver accurate fire based solely on the marksman's skill. The weapon's 7.62mm round provides adequate terminal performance against personnel and light cover at all engagement distances. The lack of electronics means the only maintenance required is mechanical — no firmware updates, no calibration cycles, no charging.",
    cultural_context: "The Sentinel has become a symbol of traditional marksmanship in an age of electronic assistance. Marksmen who carry the FMR-8 take pride in skills that smart-scope operators never need to develop — range estimation, wind reading, hold-off calculation. This community views BCI-integrated precision rifles with something between disdain and pity, arguing that the operator's skill should be the limiting factor, not their equipment budget.",
    known_users: ["Frontier designated marksmen", "Independent militia precision shooters", "Traditional marksmanship purists"],
    story_hooks: [
      "A Sentinel marksman outperformed a Carthage operator in an engagement after an EMP disabled the Carthage's electronics. The Sentinel's analog optics were unaffected, and the skilled marksman dominated the field while their technologically superior opponent struggled with dead equipment.",
      "A marksman training school uses Sentinels exclusively, arguing that learning on analog weapons builds fundamentals that transfer to any platform. Their graduates are recruited by every major security firm."
    ],
    ammunition_type: ["7.62mm caseless match", "7.62mm caseless standard"],
    tags: ["weapon", "dmr", "analog", "traditional", "precision", "frontier", "Crucible", "tier 2"]
  },
  {
    id: id(),
    name: "Kang-Petrov Arms KPM-6 'Equalizer'",
    type: "weapon",
    aliases: ["Equalizer", "KPM-6", "People's Precision"],
    category: "dmr",
    description: "The KPM-6 Equalizer is the most affordable designated marksman rifle on Meridian 88. At Φ2,200, it puts precision capability in the hands of communities that cannot afford premium marksmanship platforms. The rifle chambers 7.62mm standard in a semi-automatic action with a simple fixed 6x optical scope and a free-floating barrel that delivers 1.5-MOA accuracy — not match-grade, but adequate for engagements out to 600 meters.\n\nKang-Petrov designed the Equalizer after recognizing that most precision engagements occur at distances under 500 meters, where the difference between 0.5 MOA and 1.5 MOA is academic. The KPM-6 hits a person-sized target reliably at ranges where it matters, and it does so at a price that militia forces and community defense organizations can afford. The optional BCI module adds basic aim-assist for Φ400, bringing the total to Φ2,600.\n\nThe Equalizer's name is political. Kang-Petrov explicitly positions it as the weapon that prevents corporate forces from operating with impunity at medium range. A Tier 1 settlement with three Equalizer-equipped marksmen can impose unacceptable costs on corporate security elements that would otherwise maneuver freely. The weapon is the great leveler — it gives the economically disadvantaged the ability to reach out and touch someone at 600 meters.",
    manufacturer: "KANG-PETROV ARMS",
    tier_availability: "Tier 1+",
    legality: "Available — standard licensing",
    base_technologies: ["Affordable precision manufacturing", "Fixed optical scope", "Optional BCI aim-assist module"],
    specifications: "caliber: 7.62mm standard\neffective_range: 600m\nrate_of_fire: Semi-automatic\nmagazine_capacity: 20 rounds\nweight: 4.2 kg",
    tactical_use: "The Equalizer provides cost-effective precision fire at ranges that matter. While premium DMRs offer superior accuracy at extreme distances, the KPM-6 delivers adequate precision at typical engagement ranges for a fraction of the cost. In community defense scenarios, three Equalizer marksmen cost less than a single Carthage and provide better area coverage. The weapon's accuracy is sufficient for personnel targets at all practical distances, and the optional BCI module closes the gap further.",
    cultural_context: "The Equalizer is named for what it does: it equalizes the power imbalance between well-funded corporate forces and under-resourced communities. The weapon appears on resistance movement materials alongside the Solidarity assault rifle as symbols of affordable armed self-determination. Kang-Petrov's marketing explicitly acknowledges the weapon's role in asymmetric conflict, positioning precision marksmanship as a right rather than a luxury.",
    known_users: ["Community defense marksmen", "Militia precision shooters", "Tier 1-2 settlement defenders", "Budget-conscious freelancers"],
    story_hooks: [
      "A settlement's three Equalizer marksmen held off a corporate security advance for two days, forcing the corporation to negotiate rather than annex. The weapon's reputation has made it a recruiting tool for resistance movements.",
      "Kang-Petrov's Equalizer production line was sabotaged — the barrels produced during a specific shift have a harmonic flaw that causes accuracy to degrade catastrophically after 500 rounds."
    ],
    ammunition_type: ["7.62mm standard match", "7.62mm standard AP"],
    tags: ["weapon", "dmr", "affordable", "precision", "militia", "Kang-Petrov", "tier 1"]
  },
  {
    id: id(),
    name: "Meridian Munitions Precision Carbine MPC-9 'Scalpel'",
    type: "weapon",
    aliases: ["Scalpel", "MPC-9", "Surgeon's Rifle"],
    category: "dmr",
    description: "The MPC-9 Scalpel is a compact designated marksman rifle designed for urban precision work where a full-length DMR is too cumbersome. The weapon chambers 6.5mm caseless through a 400mm barrel — short for a precision platform — but compensates with a BCI-integrated ballistic computer that wrings maximum accuracy from the abbreviated barrel length. The result is a weapon that delivers 1-MOA accuracy at 600 meters in a package not much larger than a carbine.\n\nMeridian's open-architecture philosophy extends to the Scalpel's optics rail, which accepts any manufacturer's scope or smart-sight. The weapon ships with a basic 1-8x variable optic, but most operators replace it with their preferred precision optic within days. The rifle's compact dimensions allow it to be deployed from vehicle windows, building interiors, and rooftop positions where full-length DMRs are unwieldy.\n\nAt Φ6,800, the Scalpel occupies the mid-range of the DMR market. It sacrifices the extreme-range capability of longer-barreled platforms for the versatility of a weapon that functions as both a precision rifle and a fighting carbine. For urban operators who need precision capability without dedicating a team member to a full-size DMR, the MPC-9 provides a compelling compromise.",
    manufacturer: "MERIDIAN MUNITIONS",
    tier_availability: "Tier 2+",
    legality: "Available — standard licensing",
    base_technologies: ["Compact precision barrel", "BCI ballistic compensation", "Open-architecture optics interface", "Short-barrel accuracy optimization"],
    specifications: "caliber: 6.5mm caseless\neffective_range: 600m\nrate_of_fire: Semi-automatic\nmagazine_capacity: 20 rounds\nweight: 3.6 kg",
    tactical_use: "The Scalpel provides urban-optimized precision fire. Its compact dimensions allow deployment in confined spaces where full-length DMRs cannot maneuver, while the BCI ballistic computer maintains accuracy despite the short barrel. The weapon's dual role as precision rifle and fighting carbine allows the designated marksman to participate in close-quarters actions without transitioning to a secondary weapon. In urban operations where engagement distances rarely exceed 400 meters, the Scalpel provides all the precision capability needed in a more versatile package.",
    cultural_context: "The Scalpel represents the urbanization of precision marksmanship. Traditional marksmen view it as a compromise that sacrifices range for convenience. Urban operators view it as the evolution of the DMR for the environments where most people actually fight. The debate mirrors the broader tension between frontier and urban combat doctrine that runs through Meridian 88's military culture.",
    known_users: ["Urban designated marksmen", "Vehicle-based security teams", "Freelance precision operators", "Close-quarters overwatch"],
    story_hooks: [
      "A Scalpel operator made a critical shot through two panes of vehicle glass at 400 meters using the BCI ballistic computer's glass-penetration calculation. The shot was technically impossible with a conventional rifle of the same barrel length.",
      "Meridian's compact precision technology has been stolen by a competitor. The knock-off 'Scalpel-type' rifles flooding the market use inferior barrel steel that fails after 2,000 rounds."
    ],
    ammunition_type: ["6.5mm caseless match", "6.5mm caseless standard"],
    tags: ["weapon", "dmr", "compact", "urban", "precision", "Meridian", "tier 2"]
  },
  {
    id: id(),
    name: "Arcturus Defense Solutions Electromagnetic DMR AEDM-3 'Gauss'",
    type: "weapon",
    aliases: ["Gauss", "AEDM-3", "Silent Reach"],
    category: "dmr",
    description: "The AEDM-3 Gauss is an electromagnetic designated marksman rifle that launches 3mm tungsten penetrators at hypersonic velocity with no chemical propellant and virtually no sound. The weapon's multi-stage electromagnetic acceleration system propels each penetrator to a velocity exceeding Mach 6, creating devastating terminal effects through pure kinetic energy. At the point of impact, the tungsten penetrator releases more energy than a conventional rifle round despite weighing a fraction as much.\n\nThe Gauss's silence is its primary tactical advantage. With no propellant gases and no supersonic crack from the barrel (the projectile goes supersonic inside the acceleration chamber), the only sound is the distant impact. A Gauss operator can engage targets at 800 meters with no visible muzzle flash, no sound at the firing position, and no ballistic trail connecting shooter to target. Counter-sniper detection systems designed for chemical-propellant weapons are ineffective.\n\nArcturus restricts the AEDM-3 to Tier 4+ military contracts at Φ35,000 per unit. The weapon's capacitor system provides 30 shots per charge, and the mandatory BCI link manages the complex acceleration timing that determines projectile velocity and thus effective range. Without the neural interface, the weapon cannot fire.",
    manufacturer: "ARCTURUS DEFENSE SOLUTIONS",
    tier_availability: "Tier 4+",
    legality: "Highly restricted — military contract only",
    base_technologies: ["Multi-stage electromagnetic acceleration", "Silent launch system", "BCI acceleration management", "Tungsten hypersonic penetrators"],
    specifications: "caliber: 3mm tungsten penetrator\neffective_range: 800m\nrate_of_fire: 30 rpm (capacitor-limited)\nmagazine_capacity: 30 penetrators per charge\nweight: 5.8 kg (rifle) + 2.0 kg (capacitor pack)",
    tactical_use: "The Gauss provides silent precision fire at distances where conventional suppressed weapons lose effectiveness. Its electromagnetic launch system produces no detectable firing signature, making counter-sniper operations virtually impossible with conventional detection equipment. The tungsten penetrators defeat all personal armor at engagement range. The low rate of fire and limited magazine capacity restrict the weapon to deliberate precision engagement rather than sustained fire support.",
    cultural_context: "The Gauss is a weapon that kills in silence from beyond visual range. Its existence has changed the calculus of outdoor security — any position within 800 meters of potential Gauss deployment must account for a threat that cannot be heard, seen, or conventionally detected. Corporate executives have invested heavily in electromagnetic sensor countermeasures since the Gauss entered service, driving an arms race between silent weapons and detection technology.",
    known_users: ["Arcturus special operations", "Tier 5 military marksmen", "Classified units"],
    story_hooks: [
      "A high-profile assassination was committed with a Gauss — no one heard the shot, and the entry wound was so small it was initially missed in the autopsy. Only the tungsten residue identified the weapon type.",
      "Someone has built a makeshift electromagnetic detection array that can identify a Gauss capacitor charging within 200 meters. The technology is crude but functional, and counter-Gauss defense has just become possible."
    ],
    ammunition_type: ["3mm tungsten penetrator", "3mm tungsten AP-incendiary"],
    tags: ["weapon", "dmr", "electromagnetic", "silent", "railgun", "precision", "Arcturus", "tier 4"]
  },
  {
    id: id(),
    name: "Tessera Autonomous Marksman Platform TAMP-2 'Arbiter'",
    type: "weapon",
    aliases: ["Arbiter", "TAMP-2", "Judge Machine"],
    category: "dmr",
    description: "The TAMP-2 Arbiter is a DMR designed to operate semi-autonomously from a fixed position. The weapon can be deployed on a stabilized tripod mount with its own sensor suite, power supply, and targeting AI, functioning as an unmanned precision overwatch platform. A remote operator provides engagement authorization through BCI link, but the weapon handles target detection, tracking, and firing solution calculation independently.\n\nThe Arbiter fires 7mm caseless from a precision barrel with an automated round-chambering system that maintains the weapon's readiness without human intervention. The sensor suite includes visual, thermal, and millimeter-wave radar that provides all-weather target detection at ranges exceeding the weapon's effective firing distance. The AI prioritizes targets based on threat assessment algorithms that can be pre-configured for specific operational parameters.\n\nTessera sells the TAMP-2 at Φ45,000 per unit as a force multiplier for precision overwatch. A single remote operator can manage up to four Arbiters simultaneously, providing coverage that would require four human marksmen in four separate positions. The weapon's autonomous capabilities raise the same ethical concerns as all autonomous weapons — the AI identifies and tracks targets, but a human must authorize each engagement. The authorization delay is measured in milliseconds, raising questions about how meaningful that human oversight really is.",
    manufacturer: "TESSERA",
    tier_availability: "Tier 4+",
    legality: "Restricted — autonomous weapons platform licensing",
    base_technologies: ["Semi-autonomous targeting AI", "Multi-sensor detection suite", "Remote BCI engagement authorization", "Automated weapon maintenance"],
    specifications: "caliber: 7mm caseless\neffective_range: 900m\nrate_of_fire: Semi-automatic (AI-paced)\nmagazine_capacity: 20 rounds\nweight: 6.2 kg (weapon) + 4.5 kg (tripod/sensor suite)",
    tactical_use: "The Arbiter provides persistent, tireless precision overwatch without risking a human marksman in an exposed position. Its multi-sensor suite maintains target detection in conditions that would blind human operators, and the AI targeting system tracks multiple targets simultaneously while calculating optimal engagement sequences. A four-Arbiter network controlled by a single operator provides interlocking fields of precision fire covering a perimeter that would otherwise require significant personnel investment.",
    cultural_context: "The Arbiter is the designated marksman's job threat. Its existence raises uncomfortable questions about the role of human skill when an AI can detect, track, and calculate firing solutions faster and more consistently than any human. Proponents argue the human remains essential for the engagement decision. Opponents note that a decision presented as 'authorize/deny' with milliseconds of AI-analyzed context is not really a decision — it is a formality.",
    known_users: ["Tessera perimeter defense", "Corporate facility overwatch", "Remote security deployments"],
    story_hooks: [
      "An Arbiter's remote operator authorized an engagement based on the AI's threat assessment. The target was actually a child carrying a toy that the thermal sensor interpreted as a weapon. The AI was technically correct — the object's thermal signature matched a weapon profile.",
      "Four Arbiters deployed at a corporate facility have been operating without remote operator oversight for 72 hours due to a communications failure. The weapons continue to function on their pre-set parameters, engaging anything that triggers their threat algorithms."
    ],
    ammunition_type: ["7mm caseless match"],
    tags: ["weapon", "dmr", "autonomous", "AI", "remote", "overwatch", "Tessera", "tier 4"]
  },
  {
    id: id(),
    name: "Hearthstone Firearms Hunter's Precision HHP-7 'Longreach'",
    type: "weapon",
    aliases: ["Longreach", "HHP-7", "Ranch Precision"],
    category: "dmr",
    description: "The HHP-7 Longreach is a bolt-action precision rifle designed for frontier hunters and settlement defenders who need accuracy at distance without electronic assistance. The weapon chambers .308 brass-cased — a legacy cartridge that Hearthstone perpetuates because it is manufactured by dozens of small ammunition producers across Meridian 88. The bolt-action operation is slower than semi-automatic alternatives but provides a rigid lockup that maximizes accuracy.\n\nThe Longreach comes with a fixed 10x optical scope of surprisingly good quality, manufactured in-house by Hearthstone's optics division. The scope features a ballistic-drop compensating reticle calibrated specifically for the .308 at standard atmospheric conditions, allowing holdover adjustments without turret dialing. The rifle's walnut stock is hand-checkered — an anachronistic touch that Hearthstone maintains because their customers value craftsmanship.\n\nAt Φ1,200, the Longreach provides genuine precision capability at a price that frontier communities can afford. Its bolt action and legacy cartridge are limitations by military standards, but for a settlement defender engaging threats at 500-700 meters, the rifle delivers. The HHP-7 has put more game on more tables and deterred more threats at more settlement gates than any smart-scoped wonder weapon ever manufactured.",
    manufacturer: "HEARTHSTONE FIREARMS",
    tier_availability: "Tier 1+",
    legality: "Unrestricted — civilian grade",
    base_technologies: ["Bolt-action precision lockup", "BDC-calibrated optical scope", "Legacy cartridge compatibility", "Hand-finished wood furniture"],
    specifications: "caliber: .308 brass-cased\neffective_range: 700m\nrate_of_fire: Bolt-action (8-10 rpm skilled)\nmagazine_capacity: 5 rounds (internal)\nweight: 4.0 kg",
    tactical_use: "The Longreach provides frontier precision capability independent of any electronic or corporate infrastructure. Its bolt action and legacy cartridge ensure function with widely available ammunition and no maintenance beyond basic cleaning. The BDC reticle allows rapid engagement at known distances without the complexity of dialing turrets. In settlement defense, the HHP-7's accuracy at 500+ meters creates a deterrent zone that casual aggressors respect.",
    cultural_context: "The Longreach is the frontier precision rifle — carried by hunters, ranchers, and settlement watchers who learned marksmanship on analog equipment and see no reason to change. Its hand-checkered walnut stock is a deliberate statement: this is a weapon made by people for people, not stamped by machines for quotas. Hearthstone's frontier customers develop multi-generational relationships with their Longreach rifles, passing them from parent to child.",
    known_users: ["Frontier hunters", "Settlement watchers", "Ranch defenders", "Traditional marksmen"],
    story_hooks: [
      "A Longreach that has been in one family for three generations was used to make a 700-meter shot that stopped a corporate land survey. The shot didn't hit anyone — it hit the survey equipment. The message was clear.",
      "Hearthstone's hand-checkering team is retiring, and no one has apprenticed to replace them. The last generation of hand-finished Longreach rifles is in production, and collectors are buying them faster than Hearthstone can make them."
    ],
    ammunition_type: [".308 brass-cased standard", ".308 brass-cased match"],
    tags: ["weapon", "dmr", "bolt_action", "frontier", "analog", "traditional", "Hearthstone", "tier 1"]
  },
  {
    id: id(),
    name: "Volkov-Saito Precision VS-50EM 'Thunderbolt'",
    type: "weapon",
    aliases: ["Thunderbolt", "VS-50EM", "Rail Precision"],
    category: "dmr",
    description: "The VS-50EM Thunderbolt is Volkov-Saito's electromagnetic precision rifle, combining their signature accuracy obsession with dual-stage magnetic acceleration. The weapon fires 4mm tungsten-ceramic composite penetrators at velocities that create a brief visible plasma trail in humid conditions. Each penetrator arrives at the target with energy sufficient to defeat any personal armor system in production.\n\nUnlike Arcturus' Gauss, which prioritizes silence, the Thunderbolt prioritizes accuracy. Volkov-Saito's weapon-learning system integrates with the electromagnetic acceleration, building a precise model of each individual weapon's coil characteristics to deliver penetrators with velocity consistency measured in single meters per second. This consistency, combined with the aerodynamic stability of the tungsten-ceramic penetrator, produces sub-quarter-MOA accuracy at 1,000 meters.\n\nThe Thunderbolt costs Φ40,000 and requires a dedicated power cell worn as a backpack that provides 25 shots per charge. The weapon is sold exclusively to vetted customers through Volkov-Saito's precision division, with each unit individually serial-matched to its buyer. Resale requires Volkov-Saito's authorization. These restrictions have not prevented the Thunderbolt from appearing on the black market, where units command Φ80,000 or more.",
    manufacturer: "VOLKOV-SAITO PRECISION",
    tier_availability: "Tier 5",
    legality: "Highly restricted — vetted customers only",
    base_technologies: ["Precision electromagnetic acceleration", "Tungsten-ceramic composite penetrators", "Weapon-learning coil calibration", "Velocity-consistent launch system"],
    specifications: "caliber: 4mm tungsten-ceramic penetrator\neffective_range: 1,000m\nrate_of_fire: 20 rpm (capacitor-limited)\nmagazine_capacity: 25 penetrators per charge\nweight: 5.5 kg (rifle) + 3.0 kg (power cell backpack)",
    tactical_use: "The Thunderbolt delivers the most accurate electromagnetic precision fire available. Its weapon-learning system and velocity-consistent launch combine to produce accuracy that matches the best chemical-propellant precision rifles while adding the armor-defeat capability of a tungsten penetrator. The plasma trail in humid conditions is a tactical disadvantage that reveals the firing position, but in dry environments the weapon's signature is minimal.",
    cultural_context: "The Thunderbolt is the ultimate expression of Volkov-Saito's precision philosophy applied to emerging technology. Its Φ40,000+ price tag and vetted-customer-only sales create an exclusivity that borders on mystique. In precision shooting circles, the Thunderbolt is spoken of with reverence — a weapon that represents the current peak of individual precision capability. Owning one is a statement of both wealth and skill.",
    known_users: ["Elite precision marksmen", "Vetted private operators", "Classified military units"],
    story_hooks: [
      "A Thunderbolt appeared on the black market with its weapon-learning data intact. The data reveals the firing patterns of its original owner — a classified military marksman whose identity is now compromised.",
      "Volkov-Saito's vetting process was penetrated by an intelligence agency that used a front company to purchase six Thunderbolts. The weapons are now in the hands of an assassination cell."
    ],
    ammunition_type: ["4mm tungsten-ceramic penetrator"],
    tags: ["weapon", "dmr", "electromagnetic", "precision", "railgun", "Volkov-Saito", "tier 5"]
  },
  {
    id: id(),
    name: "Crucible Industries Anti-Materiel DMR AMDMR-5 'Verdict'",
    type: "weapon",
    aliases: ["Heavy Verdict", "AMDMR-5", "Wall Breaker"],
    category: "dmr",
    description: "The AMDMR-5 is a semi-automatic anti-materiel DMR chambered in 12.7mm caseless — a round that blurs the boundary between rifle ammunition and small cannon shells. Crucible designed the weapon for engaging light vehicles, equipment, and hardened positions at distances where conventional DMR calibers are insufficient. Each round delivers enough energy to penetrate engine blocks, disable communications equipment, and breach reinforced walls.\n\nThe rifle weighs 9 kilograms unloaded and requires either augmented operator strength, a supported firing position, or an exoframe to operate effectively. The recoil management system uses a long-travel hydraulic buffer that absorbs the 12.7mm impulse over a 15cm stroke, reducing felt recoil to levels comparable to a heavy battle rifle. The BCI integration provides structural analysis that identifies high-value material targets and optimal aim points for maximum effect.\n\nAt Φ8,500, the AMDMR-5 provides anti-materiel capability at a fraction of dedicated anti-materiel rifle costs. Its semi-automatic operation allows rapid engagement of multiple material targets — a capability that bolt-action anti-materiel rifles cannot match. The 8-round magazine capacity reflects the weapon's role: this is not a weapon for sustained fire, but for deliberate destruction of high-value targets.",
    manufacturer: "CRUCIBLE INDUSTRIES",
    tier_availability: "Tier 3+",
    legality: "Restricted — anti-materiel weapons licensing",
    base_technologies: ["Heavy-caliber semi-automatic action", "Long-travel hydraulic recoil buffer", "BCI structural analysis targeting"],
    specifications: "caliber: 12.7mm caseless\neffective_range: 1,200m\nrate_of_fire: Semi-automatic (limited by recoil recovery)\nmagazine_capacity: 8 rounds\nweight: 9.0 kg",
    tactical_use: "The AMDMR-5 provides squad-level anti-materiel capability. Its 12.7mm round defeats light vehicle armor, communications equipment, and fortified positions at ranges exceeding one kilometer. The semi-automatic action allows rapid target transitions between material objectives — disabling a vehicle's engine, its communications antenna, and its weapon mount in three shots. Against personnel, the weapon is devastatingly excessive, which is sometimes the point.",
    cultural_context: "The AMDMR-5 represents escalation. When someone deploys a 12.7mm DMR, the message is that vehicles, equipment, and cover are all valid targets. The weapon's presence on a battlefield changes the tactical calculus — nothing short of hardened bunker construction provides safety. In asymmetric conflicts, a single AMDMR-5 can deny an entire road to vehicle traffic, making it a strategic asset for forces that lack anti-vehicle weapons.",
    known_users: ["Anti-materiel specialists", "Vehicle denial teams", "Heavy marksmen", "Siege warfare operators"],
    story_hooks: [
      "A single AMDMR-5 operator has been systematically disabling a corponation's supply vehicles along a trade route, costing them millions in delayed shipments. The corponation wants the shooter found — but the 1,200-meter engagement range means no one has seen them.",
      "An AMDMR-5 was used to breach a safe room wall during a corporate extraction. The weapon's 12.7mm round penetrated the reinforced concrete that the safe room's designers guaranteed would stop any personal weapon."
    ],
    ammunition_type: ["12.7mm caseless AP", "12.7mm caseless explosive"],
    tags: ["weapon", "dmr", "anti_materiel", "heavy", "vehicle_denial", "Crucible", "tier 3"]
  },
  {
    id: id(),
    name: "Meridian Munitions Smart Marksman MSM-5 'Indexer'",
    type: "weapon",
    aliases: ["Indexer", "MSM-5", "Smart Rifle"],
    category: "dmr",
    description: "The MSM-5 Indexer is Meridian Munitions' AI-assisted DMR designed to make a competent shooter into a skilled marksman through technology rather than training. The weapon's onboard AI handles windage calculation, range estimation, barometric pressure compensation, Coriolis correction, and spin drift — every variable that a skilled marksman calculates mentally is computed automatically and displayed as a BCI firing solution overlay.\n\nThe Indexer fires 6.5mm caseless through a precision barrel and uses Meridian's open-architecture smart-scope interface to connect with any compatible optic. The weapon's AI is the product — the mechanical components are competent but unremarkable. What sets the MSM-5 apart is that it makes 600-meter precision engagement accessible to operators with weeks of training rather than years.\n\nAt Φ5,500, the Indexer is positioned as the precision rifle for organizations that cannot afford to invest years in traditional marksman development. Corporate security firms, frontier defense cooperatives, and freelance teams all benefit from fielding operators who can make precision shots without master-level training. Traditional marksmen view the Indexer with contempt. Its users view it with gratitude.",
    manufacturer: "MERIDIAN MUNITIONS",
    tier_availability: "Tier 2+",
    legality: "Available — standard licensing",
    base_technologies: ["AI ballistic computation", "Multi-variable environmental compensation", "Automated firing solution generation", "Open-architecture smart-scope"],
    specifications: "caliber: 6.5mm caseless\neffective_range: 700m\nrate_of_fire: Semi-automatic\nmagazine_capacity: 20 rounds\nweight: 4.3 kg",
    tactical_use: "The Indexer democratizes precision marksmanship. Its AI handles the computational burden that traditionally requires years of training, allowing operators to focus on the fundamentals of trigger control and position. In organizations where precision capability is needed immediately, the MSM-5 provides it without the timeline investment of traditional marksman training. The weapon's accuracy depends on the AI's calculations, which means it is only as good as its environmental sensors — in conditions that confuse the sensors, accuracy degrades.",
    cultural_context: "The Indexer has ignited a fierce debate in the marksmanship community. Traditional marksmen argue that outsourcing calculation to AI produces operators who cannot function without their technology. Indexer advocates argue that the goal is effective fire, not personal achievement, and that any tool that puts accurate rounds on target is a good tool. This debate reflects the broader tension between skill and technology that pervades Meridian 88's military culture.",
    known_users: ["Corporate security marksmen", "Frontier defense cooperatives", "Freelance teams", "Rapid-deployment precision units"],
    story_hooks: [
      "An Indexer's AI was corrupted by a firmware update that introduced a systematic error — every firing solution was 2 meters to the right at 500 meters. The error was subtle enough to go unnoticed in training but catastrophic in the field.",
      "A traditional marksman challenged an Indexer operator to a precision competition. The Indexer won at every range — until the traditional marksman disabled the range's environmental sensors, and the AI-dependent operator couldn't compensate."
    ],
    ammunition_type: ["6.5mm caseless match", "6.5mm caseless standard"],
    tags: ["weapon", "dmr", "AI", "smart_scope", "accessible", "Meridian", "tier 2"]
  },
  {
    id: id(),
    name: "Kang-Petrov Arms KPM-12 'Steadfast'",
    type: "weapon",
    aliases: ["Steadfast", "KPM-12", "The Constant"],
    category: "dmr",
    description: "The KPM-12 Steadfast is a battle-DMR hybrid that sacrifices extreme accuracy for sustained-fire capability. Where conventional DMRs are precision instruments that demand careful shooting, the Steadfast is designed for designated marksmen who operate as part of assault elements and need to maintain precision fire while moving and shooting rapidly. The weapon chambers 7.62mm caseless with a heavy barrel that resists thermal shift during sustained engagement.\n\nThe Steadfast's BCI integration focuses on moving-target engagement rather than static precision. The AI tracks target movement vectors and provides a continuously updated lead indicator in the neural overlay, allowing the marksman to maintain effective fire on moving targets at ranges up to 500 meters. The weapon's 1.5-MOA accuracy is modest by DMR standards but maintained consistently through high round counts and rapid-fire sequences.\n\nKang-Petrov prices the Steadfast at Φ3,500, positioning it between their Equalizer and premium competitors. Its customers are operators who need a marksman's reach with a rifleman's tempo — squad-level marksmen who engage targets of opportunity while keeping pace with their assault element rather than providing static overwatch. The weapon fills a role that traditional DMR doctrine doesn't acknowledge: the moving marksman.",
    manufacturer: "KANG-PETROV ARMS",
    tier_availability: "Tier 2+",
    legality: "Available — standard licensing",
    base_technologies: ["Heavy barrel thermal resistance", "BCI moving-target tracking", "Sustained-fire accuracy maintenance"],
    specifications: "caliber: 7.62mm caseless\neffective_range: 500m (moving engagement), 650m (static)\nrate_of_fire: Semi-automatic (rapid capable)\nmagazine_capacity: 20 rounds\nweight: 4.5 kg",
    tactical_use: "The Steadfast enables precision fire from assault elements in motion. Traditional DMR doctrine positions the marksman in a static overwatch role, but many operations require the marksman to advance with the squad. The KPM-12's moving-target AI and thermal-resistant barrel maintain engagement capability during the kind of rapid, mobile shooting that would degrade a traditional precision rifle's accuracy. The weapon trades peak accuracy for consistency under stress.",
    cultural_context: "The Steadfast represents a doctrinal shift in how designated marksmen are employed. Rather than separating the marksman from the squad, the KPM-12 integrates them into the assault element as a mobile precision asset. This approach appeals to freelance teams where separating a member for static overwatch reduces an already small team's maneuver capability. The weapon is practical rather than prestigious — no one brags about carrying a Steadfast, but no one regrets having one.",
    known_users: ["Squad-integrated marksmen", "Mobile security details", "Freelance assault teams", "Patrol precision elements"],
    story_hooks: [
      "A Steadfast marksman tracked a moving target through a crowd at 400 meters using the BCI lead indicator. The shot was clean — but the AI's movement prediction was based on the target's walking pace, and they stopped to tie their shoe. The round missed the target and hit a vendor stall.",
      "Kang-Petrov is developing a Steadfast variant with a fully automatic mode for suppressive precision fire. The prototype achieves 2-MOA accuracy at 300 rpm — terrifying if it reaches production."
    ],
    ammunition_type: ["7.62mm caseless standard", "7.62mm caseless match"],
    tags: ["weapon", "dmr", "mobile", "assault", "moving_target", "Kang-Petrov", "tier 2"]
  },
  {
    id: id(),
    name: "Tessera Predictive Marksman Rifle TPMR-4 'Cassandra'",
    type: "weapon",
    aliases: ["Cassandra", "TPMR-4", "Fortune Teller"],
    category: "dmr",
    description: "The TPMR-4 Cassandra takes Tessera's predictive targeting AI to its logical extreme. The weapon's AI doesn't just track current target position — it models probable future positions based on movement history, environmental constraints, and behavioral analysis. The system calculates where the target will be when the bullet arrives and fires at that predicted position. The operator's role is reduced to target selection; the AI handles timing and aim.\n\nThe Cassandra fires 7mm caseless through a precision barrel, and the BCI integration is deeper than any competing platform. The AI requires full neural access to the operator's visual processing centers, essentially borrowing the operator's eyes as biological sensors while providing the firing solution through the same neural pathway. Some operators describe the experience as the weapon aiming itself through their body.\n\nAt Φ28,000, the Cassandra is Tessera's most expensive DMR. Its predictive capability is genuinely remarkable — in controlled tests, the system predicts target position with sufficient accuracy to hit running targets at 700 meters using the operator's trigger timing as the variable the AI cannot control. This is the weapon's vulnerability: the human is the weakest link in the chain, and the AI knows it.",
    manufacturer: "TESSERA",
    tier_availability: "Tier 4+",
    legality: "Restricted — advanced BCI weapons licensing",
    base_technologies: ["Predictive behavioral targeting AI", "Deep neural visual integration", "Probabilistic firing solution", "Human-AI shared targeting system"],
    specifications: "caliber: 7mm caseless\neffective_range: 800m (static), 700m (predictive moving)\nrate_of_fire: Semi-automatic (AI-timed)\nmagazine_capacity: 15 rounds\nweight: 4.4 kg",
    tactical_use: "The Cassandra defeats evasive movement through prediction. Targets that zigzag, sprint between cover, or use irregular movement patterns to defeat conventional marksmen are vulnerable to the AI's behavioral modeling. The system's effectiveness increases with observation time — the longer the AI watches a target move, the more accurately it predicts future positions. First shots at newly acquired targets are less accurate than fifth or sixth shots against targets the AI has been tracking.",
    cultural_context: "The Cassandra raises profound questions about the nature of skill. If the AI handles aiming and timing, what does the operator contribute? Tessera argues the operator provides judgment — choosing which targets to engage and when. Critics argue the Cassandra is an autonomous weapon with a human signature requirement, performing the killing function independently while a human provides legal cover. The weapon's name is darkly appropriate: Cassandra predicted the future but could not change it.",
    known_users: ["Tessera advanced operations", "AI-integrated marksman teams", "Counter-evasion specialists"],
    story_hooks: [
      "A Cassandra's predictive AI began modeling a target's behavior with such accuracy that it predicted the target would enter a specific building at a specific time — 72 hours in advance. The operator reported the prediction to command. It was correct.",
      "An operator became psychologically dependent on the Cassandra's deep neural integration, unable to function as a marksman without the AI's presence in their visual cortex. The weapon became an addiction."
    ],
    ammunition_type: ["7mm caseless match"],
    tags: ["weapon", "dmr", "AI", "predictive", "neural", "advanced", "Tessera", "tier 4"]
  },
  {
    id: id(),
    name: "Volkov-Saito Precision Custom Shop VSCS-1 'Masterwork'",
    type: "weapon",
    aliases: ["Masterwork", "VSCS-1", "The Custom"],
    category: "dmr",
    description: "The VSCS-1 Masterwork is not a production weapon — it is a platform. Each Masterwork is built to the individual customer's specifications by Volkov-Saito's custom shop, with the buyer selecting caliber, barrel length, action type, stock configuration, optics, and BCI integration level. The only constant is the quality standard: every Masterwork delivers sub-quarter-MOA accuracy regardless of configuration.\n\nThe custom shop process begins with a consultation where the buyer's physical dimensions, neural interface specifications, shooting style, and intended use case are documented. A master gunsmith then designs and builds the weapon over a period of 8-12 weeks, hand-fitting every component and individually testing the complete system. The weapon-learning BCI system is pre-calibrated during the build process using the buyer's neural profile, so the rifle arrives partially personalized.\n\nPricing starts at Φ50,000 and escalates based on complexity. Electromagnetic acceleration options add Φ20,000. Custom caliber development adds more. Each Masterwork is serial-numbered and registered to its owner, with Volkov-Saito maintaining lifetime service records. Fewer than 100 are produced annually, and the waiting list extends two years. The Masterwork is the pinnacle of individual precision weapons — a weapon built for one person and one purpose.",
    manufacturer: "VOLKOV-SAITO PRECISION",
    tier_availability: "Tier 5",
    legality: "Restricted — custom weapons registration",
    base_technologies: ["Custom-built precision platform", "Individual owner specification", "Pre-calibrated weapon-learning system", "Master gunsmith hand-fitting"],
    specifications: "caliber: Customer specified\neffective_range: Configuration dependent (800-1,200m typical)\nrate_of_fire: Configuration dependent\nmagazine_capacity: Configuration dependent\nweight: Configuration dependent (typically 4-6 kg)",
    tactical_use: "The Masterwork is the ultimate expression of the precision marksman's relationship with their weapon. Built to individual specification, the rifle fits one operator's body, interface, and shooting style with a precision that production weapons cannot approach. The pre-calibrated weapon-learning system begins at a higher baseline than field-calibrated systems, and the hand-fitted components eliminate the tolerances that limit production accuracy. In capable hands, the Masterwork approaches the theoretical accuracy limit of its chosen cartridge.",
    cultural_context: "The Masterwork is the prestige weapon of the precision shooting world. Owning one signifies both the financial means and the demonstrated skill that Volkov-Saito requires before accepting a custom order — they interview potential buyers and reject those they deem insufficiently dedicated. This gatekeeping creates an exclusive community of Masterwork owners who share a bond of recognized skill and invested commitment. A Masterwork is never just a weapon. It is a collaboration between gunsmith and marksman.",
    known_users: ["Elite precision marksmen", "Competition champions", "Ultra-wealthy operators", "Precision shooting collectors"],
    story_hooks: [
      "A Masterwork owner died, and their weapon went to estate sale. Volkov-Saito is attempting to buy it back — the weapon-learning data contains sensitive information about the deceased operator's neural patterns that could be exploited.",
      "A Masterwork with electromagnetic acceleration capabilities was stolen during transit from the custom shop to the buyer. The weapon is worth Φ75,000 and is the most accurate individual precision rifle ever built. Everyone wants it."
    ],
    ammunition_type: ["Customer specified"],
    tags: ["weapon", "dmr", "custom", "hand_built", "premium", "precision", "Volkov-Saito", "tier 5"]
  },
  {
    id: id(),
    name: "Arcturus Defense Solutions Counter-BCI Rifle ACBR-2 'Migraine'",
    type: "weapon",
    aliases: ["Migraine", "ACBR-2", "Neural Sniper"],
    category: "dmr",
    description: "The ACBR-2 Migraine is a designated marksman rifle that fires a specialized round containing a focused EMP micro-charge alongside a conventional 6.5mm penetrator. On impact, the projectile delivers ballistic damage while simultaneously emitting a localized electromagnetic pulse designed to disrupt BCI implants within a 2-meter radius of the impact point. Against BCI-augmented targets, the Migraine inflicts both physical trauma and neural interface disruption — a combination that is incapacitating even if the ballistic wound is survivable.\n\nThe rifle's BCI-ironic design — using smart-link technology to deliver anti-BCI weapons — is built on Arcturus' standard DMR platform with a modified barrel to accommodate the larger EMP-carrying projectile. The smart-scope provides targeting that identifies BCI-augmented targets by detecting the electromagnetic emissions of active neural interfaces, effectively locating targets by their cybernetic enhancements.\n\nArcturus developed the Migraine for counter-augmentation operations — engaging targets whose BCI-linked weapons, neural armor, or cybernetic enhancements provide tactical advantages that conventional weapons cannot neutralize. At Φ12,000, plus Φ50 per specialized round, the weapon is expensive to field. Its customers view the cost as worthwhile insurance against BCI-dependent opponents whose entire combat capability collapses when their neural interface goes dark.",
    manufacturer: "ARCTURUS DEFENSE SOLUTIONS",
    tier_availability: "Tier 4+",
    legality: "Restricted — counter-augmentation operations authorization",
    base_technologies: ["EMP micro-charge projectiles", "BCI emission detection scope", "Dual kinetic-electromagnetic terminal effect"],
    specifications: "caliber: 6.5mm EMP-carrier\neffective_range: 600m\nrate_of_fire: Semi-automatic\nmagazine_capacity: 15 rounds\nweight: 4.8 kg",
    tactical_use: "The Migraine neutralizes BCI-dependent combatants by disrupting the technology they rely on. A single hit disables BCI-linked weapons, neural targeting aids, communication systems, and cybernetic enhancements within the EMP blast radius. Against heavily augmented opponents, this disruption is often more tactically significant than the ballistic wound itself. The BCI emission detection scope allows the marksman to identify and prioritize the most heavily augmented targets in a group.",
    cultural_context: "The Migraine is a weapon designed to punish augmentation. In a world where BCI integration provides overwhelming tactical advantages, the ACBR-2 represents the counter — a reminder that dependence on technology creates vulnerability. Anti-augmentation groups celebrate the Migraine as proof that the augmented are not invulnerable. Augmented communities view it as a targeted weapon of prejudice, designed specifically to harm people for their cybernetic choices.",
    known_users: ["Counter-augmentation specialists", "Anti-cyberware operations teams", "EMP warfare units"],
    story_hooks: [
      "A Migraine round struck an augmented civilian bystander, and the EMP disrupted their life-sustaining medical cyberware. The target survived; the bystander did not. The weapon's indiscriminate EMP radius is now a legal liability issue.",
      "An augmented rights group has obtained a Migraine and is publicly destroying it in a media event. Arcturus is calculating whether the publicity is more damaging than the loss of a single weapon."
    ],
    ammunition_type: ["6.5mm EMP-carrier", "6.5mm standard (compatible)"],
    tags: ["weapon", "dmr", "EMP", "counter_BCI", "anti_augmentation", "Arcturus", "tier 4"]
  },
  {
    id: id(),
    name: "Crucible Industries Siege DMR SDMR-3 'Bastion'",
    type: "weapon",
    aliases: ["Bastion", "SDMR-3", "Siege Rifle"],
    category: "dmr",
    description: "The SDMR-3 Bastion is a defensive DMR designed to be mounted in prepared positions and fired for extended periods without degradation. The weapon features a quick-change barrel system, a high-capacity 30-round magazine, and a liquid cooling system that circulates thermal management fluid through the barrel jacket. These features allow the Bastion to deliver precision fire at rates that would destroy conventional DMR barrels within minutes.\n\nChambered in 7.62mm caseless, the Bastion delivers 1-MOA accuracy through its first 500 rounds of sustained fire without barrel change. A fresh barrel extends this to 1,000 rounds. The liquid cooling system maintains barrel temperature below thermal-shift thresholds, ensuring consistent accuracy regardless of fire volume. The weapon's mounting system interfaces with standard defensive position hardware, providing stable platform support.\n\nCrucible designed the Bastion for siege defense and prolonged overwatch operations where ammunition expenditure exceeds conventional DMR operational parameters. At Φ7,000, it is priced for settlements and organizations that expect to fight from fixed positions against sustained assault. The weapon is heavy, immobile, and purpose-specific — the antithesis of a versatile field weapon. But when the walls are manned and the assault begins, the Bastion delivers precision fire that does not stop.",
    manufacturer: "CRUCIBLE INDUSTRIES",
    tier_availability: "Tier 2+",
    legality: "Available — defensive installation licensing",
    base_technologies: ["Liquid-cooled precision barrel", "Quick-change barrel system", "Extended sustained-fire accuracy", "Defensive position mounting"],
    specifications: "caliber: 7.62mm caseless\neffective_range: 800m\nrate_of_fire: Semi-automatic (sustained precision capable)\nmagazine_capacity: 30 rounds\nweight: 6.0 kg (unmounted)",
    tactical_use: "The Bastion provides precision fire through engagement durations that destroy conventional DMRs. In siege defense, where the enemy assaults in waves over hours or days, the Bastion's sustained accuracy ensures the marksman maintains effectiveness throughout the engagement. The quick-change barrel system allows barrel swaps during pauses in the assault, and the liquid cooling extends operational periods between changes. The 30-round magazine reduces reload frequency during critical moments.",
    cultural_context: "The Bastion is the DMR of last stands. Its design assumes a scenario where retreat is not an option and the engagement will last until one side is destroyed. This grim purpose gives the weapon a reputation that transcends its mechanical specifications — carrying a Bastion to a position means you expect to fight from that position until the fighting is over, one way or another. In settlement defense culture, the Bastion is respected as the weapon of commitment.",
    known_users: ["Settlement defense forces", "Siege specialists", "Fixed-position marksmen", "Perimeter defense teams"],
    story_hooks: [
      "A single Bastion marksman held a chokepoint for 14 hours, firing over 400 rounds through three barrel changes. The liquid cooling system failed at hour 12, and the last two hours were fired through a warping barrel. The accuracy degraded. The position held.",
      "A Bastion was recovered from an abandoned defensive position with 800 spent casings and no operator. The weapon's position logs show continuous fire for six hours, but no body was found. The marksman's fate is unknown."
    ],
    ammunition_type: ["7.62mm caseless match", "7.62mm caseless standard"],
    tags: ["weapon", "dmr", "siege", "defensive", "sustained_fire", "liquid_cooled", "Crucible", "tier 2"]
  },
  {
    id: id(),
    name: "Meridian Munitions Dual-Mode DMR MDDM-7 'Switchback'",
    type: "weapon",
    aliases: ["Switchback", "MDDM-7", "Two-Face"],
    category: "dmr",
    description: "The MDDM-7 Switchback is a dual-caliber DMR that chambers both 6.5mm caseless and 8.6mm caseless through a barrel-swap system that the BCI smart-link manages automatically. The weapon carries two barrels internally — one precision 6.5mm tube for long-range accuracy and one heavy 8.6mm tube for close-range stopping power. On neural command, the active barrel rotates into position in 0.8 seconds, reconfiguring the weapon's ballistic profile.\n\nThe 6.5mm mode delivers 1-MOA accuracy at 800 meters — competitive with dedicated DMR platforms. The 8.6mm mode sacrifices accuracy for devastating close-range terminal effect, functioning essentially as a semi-automatic battle rifle that hits like a freight train inside 300 meters. The BCI system adjusts the smart-scope's ballistic calculations automatically when the barrel swaps, and the magazine well accepts both caliber-specific magazines.\n\nMeridian designed the Switchback for designated marksmen who operate without close-range backup. In small freelance teams where the marksman might need to defend their position at CQB distances before resuming precision overwatch, the MDDM-7 provides both capabilities without requiring a secondary weapon. At Φ9,500, the dual-barrel system represents a significant engineering investment that Meridian prices competitively.",
    manufacturer: "MERIDIAN MUNITIONS",
    tier_availability: "Tier 3+",
    legality: "Available — premium licensing",
    base_technologies: ["Dual-barrel rotation system", "BCI automated barrel swap", "Multi-caliber ballistic adaptation", "Compact dual-barrel housing"],
    specifications: "caliber: 6.5mm caseless / 8.6mm caseless (switchable)\neffective_range: 800m (6.5mm) / 400m (8.6mm)\nrate_of_fire: Semi-automatic\nmagazine_capacity: 20 rounds (6.5mm) / 12 rounds (8.6mm)\nweight: 5.0 kg",
    tactical_use: "The Switchback eliminates the DMR's traditional vulnerability to close-range engagement. When hostiles close inside the 6.5mm's optimal range, the operator swaps to 8.6mm and meets the threat with overwhelming close-range firepower. The 0.8-second barrel swap is fast enough for reactive transitions during dynamic engagements. The weapon's versatility allows the marksman to operate independently or in small teams without requiring dedicated close-range support.",
    cultural_context: "The Switchback reflects the reality of freelance designated marksmanship — that precision shooters rarely have the luxury of a full squad protecting their position. The weapon acknowledges that the marksman is often alone, and solitude requires versatility. Freelance marksmen have adopted the Switchback enthusiastically, valuing its ability to handle the unpredictable engagement distance variations of independent operations.",
    known_users: ["Freelance designated marksmen", "Solo operators", "Small-team marksmen", "Versatile precision shooters"],
    story_hooks: [
      "A Switchback's barrel rotation jammed mid-swap during a critical engagement, leaving the operator with neither barrel in firing position. The 0.8-second swap became an eternity as the operator manually forced the rotation.",
      "A Switchback operator discovered that the dual-barrel housing had been modified to include a third barrel position — empty, but pre-drilled for a caliber that doesn't exist in production. Someone is planning for ammunition that hasn't been invented yet."
    ],
    ammunition_type: ["6.5mm caseless match", "8.6mm caseless"],
    tags: ["weapon", "dmr", "dual_caliber", "versatile", "switchable", "Meridian", "tier 3"]
  },
  {
    id: id(),
    name: "Kang-Petrov Arms KPM-15R 'Reclaim'",
    type: "weapon",
    aliases: ["Reclaim", "KPM-15R", "Resistance Rifle"],
    category: "dmr",
    description: "The KPM-15R Reclaim is a precision rifle designed specifically for urban resistance operations in built environments. The weapon chambers 7.62mm caseless with a barrel length optimized for engagements between 200 and 500 meters — the typical ranges encountered when shooting between buildings, across intersections, and through urban canyons. The scope is a fixed 6x optic with an illuminated reticle optimized for the geometric regularity of urban environments.\n\nThe Reclaim's distinguishing feature is its rapid-displacement design. The weapon breaks down into three components — barrel assembly, receiver, and stock — in under 10 seconds without tools, and reassembles in 15. This allows the marksman to break the weapon down into pieces that fit inside a common backpack, move to a new position through public spaces without displaying a rifle, and reassemble at the new firing position. The quick-disconnect system maintains zero — the weapon shoots to the same point of aim after reassembly.\n\nKang-Petrov markets the Reclaim openly as a resistance weapon, making no pretense about its intended use case. At Φ3,000, it is affordable for organized resistance movements, and its rapid-displacement capability allows urban marksmen to maintain operational tempo in environments where static positions are quickly identified and neutralized.",
    manufacturer: "KANG-PETROV ARMS",
    tier_availability: "Tier 2+",
    legality: "Available — standard licensing",
    base_technologies: ["Zero-maintaining quick-disconnect system", "Urban-optimized barrel length", "Rapid displacement break-down", "Backpack-concealable components"],
    specifications: "caliber: 7.62mm caseless\neffective_range: 500m (urban optimized for 200-500m)\nrate_of_fire: Semi-automatic\nmagazine_capacity: 15 rounds\nweight: 4.0 kg",
    tactical_use: "The Reclaim enables shoot-and-move tactics in urban environments. The marksman fires from a position, breaks the weapon down in 10 seconds, moves through public spaces as a civilian, and reassembles at a new position for the next engagement. This displacement cycle makes the marksman extremely difficult to locate and neutralize, as the weapon disappears between engagements. The zero-maintaining quick-disconnect ensures accuracy is preserved through repeated assembly cycles.",
    cultural_context: "The Reclaim is the weapon of urban insurgency. Its design explicitly enables the tactics that make urban resistance effective — appearing from nowhere, striking, and vanishing into the civilian population. Corporate security forces view the Reclaim as a threat to stability. Resistance movements view it as a tool of liberation. Kang-Petrov views it as a product that sells itself to customers who have no other options.",
    known_users: ["Urban resistance marksmen", "Clandestine precision operators", "Asymmetric warfare specialists"],
    story_hooks: [
      "A Reclaim marksman has been engaging corporate security from a different position each day for two weeks. The weapon breaks down so quickly that every search of the surrounding buildings finds nothing. Corporate security knows the weapon exists but cannot locate it between engagements.",
      "Kang-Petrov's Reclaim production data was leaked, revealing the serial numbers of every unit shipped to a specific resistance movement. The corponation now knows exactly how many Reclaims the resistance has — but not where they are."
    ],
    ammunition_type: ["7.62mm caseless standard", "7.62mm caseless AP"],
    tags: ["weapon", "dmr", "urban", "resistance", "displacement", "concealable", "Kang-Petrov", "tier 2"]
  },
  {
    id: id(),
    name: "Hearthstone Firearms Watchtower Rifle HWR-4 'Vigil'",
    type: "weapon",
    aliases: ["Vigil", "HWR-4", "Tower Gun"],
    category: "dmr",
    description: "The HWR-4 Vigil is a semi-automatic DMR designed for settlement watchtower duty — the specific role of a defender stationed in an elevated position providing overwatch for a community's perimeter. The weapon chambers 6.5mm brass-cased through a 550mm barrel with a fixed 8x scope, delivering 1.5-MOA accuracy at 700 meters. Like all Hearthstone weapons, the Vigil contains no electronic components.\n\nThe weapon's design reflects its intended deployment. The stock has an integral cheek rest optimized for downward-angle shooting from elevated positions. The barrel is free-floated within a ventilated handguard designed for sustained observation in hot climates. The scope mount is reinforced against the impact of the weapon being set down repeatedly on stone or concrete watchtower ledges. Every detail serves the reality of long hours in a tower, watching, waiting, and occasionally shooting.\n\nAt Φ1,800, the Vigil is priced for settlement procurement — communities buying weapons for their watch rotation rather than individual operators. Hearthstone sells them in watchtower kits that include the rifle, scope, cleaning supplies, and a weather-resistant storage case designed to hang from a tower wall. The complete kit runs Φ2,100, and many settlements maintain multiple kits at their watchtower positions.",
    manufacturer: "HEARTHSTONE FIREARMS",
    tier_availability: "Tier 1+",
    legality: "Unrestricted — civilian grade",
    base_technologies: ["Elevated-position ergonomics", "Downward-angle shooting optimization", "Ventilated sustained-observation design", "Impact-resistant scope mount"],
    specifications: "caliber: 6.5mm brass-cased\neffective_range: 700m\nrate_of_fire: Semi-automatic\nmagazine_capacity: 10 rounds\nweight: 4.1 kg",
    tactical_use: "The Vigil provides elevated overwatch capability optimized for the specific ergonomics and engagement patterns of watchtower defense. The downward-angle stock geometry accounts for the ballistic differences of shooting from elevation, and the reinforced scope mount maintains zero despite the rough handling inherent to watchtower service. The 10-round magazine encourages deliberate fire — a watchtower marksman's role is to identify and deter threats, not to engage in sustained firefights.",
    cultural_context: "The Vigil is the watchtower weapon. In Tier 1 settlements across Meridian 88, it sits in its wall-mounted case at the top of the community watchtower, ready for whoever draws the watch shift. The weapon belongs to the community rather than an individual, and its presence represents the collective's commitment to its own security. Children in these settlements grow up seeing the Vigil as part of the watchtower's furniture — as permanent and essential as the walls themselves.",
    known_users: ["Settlement watch rotations", "Community perimeter defense", "Frontier watchtower operators"],
    story_hooks: [
      "A settlement's Vigil was stolen from its watchtower case the night before a predicted raid. Without the overwatch rifle, the settlement's perimeter defense is critically weakened. The theft was an inside job.",
      "A watchtower operator using a Vigil spotted a corporate reconnaissance team at 600 meters and placed a warning shot that impacted 2 meters from the team leader. The message was received. The reconnaissance team withdrew."
    ],
    ammunition_type: ["6.5mm brass-cased standard", "6.5mm brass-cased match"],
    tags: ["weapon", "dmr", "watchtower", "settlement", "overwatch", "community", "Hearthstone", "tier 1"]
  },
  {
    id: id(),
    name: "Tessera Adaptive Marksman Platform TAMP-5 'Mimic'",
    type: "weapon",
    aliases: ["Mimic", "TAMP-5", "The Copycat"],
    category: "dmr",
    description: "The TAMP-5 Mimic is a DMR with onboard AI that studies enemy marksmen's firing patterns and develops countermeasures in real-time. When engaged against an opposing designated marksman, the Mimic's AI analyzes incoming fire to determine the enemy's weapon type, estimated position, firing rhythm, and engagement tendencies. This analysis is presented to the operator as a BCI overlay showing the predicted timing and direction of the enemy's next shot.\n\nThe rifle fires 6.5mm caseless through a precision barrel and features a multi-sensor suite that detects incoming projectile signatures — supersonic crack analysis, ballistic trajectory backtracking, and thermal flash detection combine to build a model of the opposing shooter. The system improves with each exchanged shot, becoming more accurate in its predictions as the engagement continues.\n\nTessera markets the Mimic at Φ19,000 as a counter-sniper platform for designated marksmen who expect to face skilled opposition. The AI's ability to predict an enemy marksman's behavior creates an asymmetric advantage — the Mimic operator knows when and where the next shot is coming, while their opponent operates blind. In marksman-vs-marksman engagements, this information advantage is often decisive.",
    manufacturer: "TESSERA",
    tier_availability: "Tier 4+",
    legality: "Restricted — advanced counter-sniper licensing",
    base_technologies: ["Enemy pattern analysis AI", "Multi-sensor incoming fire detection", "Predictive counter-marksman overlay", "Real-time behavioral modeling"],
    specifications: "caliber: 6.5mm caseless\neffective_range: 800m\nrate_of_fire: Semi-automatic\nmagazine_capacity: 20 rounds\nweight: 5.0 kg",
    tactical_use: "The Mimic transforms counter-sniper engagements by providing predictive intelligence about the opposing marksman. The AI's analysis of enemy firing patterns enables the operator to time their exposure between predicted enemy shots, move during the opponent's reload cycles, and identify the enemy's position through backtracked trajectories. In prolonged marksman duels, the Mimic's advantage compounds as the AI builds an increasingly accurate behavioral model.",
    cultural_context: "The Mimic introduces an element of meta-cognition to precision shooting. The weapon doesn't just help the operator shoot — it helps them understand how their opponent thinks. This has created a new discipline in precision marksmanship: counter-prediction, where skilled marksmen deliberately vary their patterns to defeat AI analysis. The cat-and-mouse game between Mimic operators and pattern-aware opponents has become one of the most intellectually demanding aspects of modern precision combat.",
    known_users: ["Counter-sniper specialists", "Marksman duel specialists", "Tessera precision teams"],
    story_hooks: [
      "Two Mimic operators were deployed against each other. Both AIs analyzed the other's patterns, creating an escalating prediction loop that resulted in both weapons recommending the exact same shot timing — the engagement ended in a simultaneous exchange that wounded both operators.",
      "A marksman realized the Mimic's AI was being fed false data — the 'enemy marksman' was a decoy system broadcasting fake firing signatures to draw the operator into a position where the real threat was waiting."
    ],
    ammunition_type: ["6.5mm caseless match"],
    tags: ["weapon", "dmr", "counter_sniper", "AI", "predictive", "pattern_analysis", "Tessera", "tier 4"]
  },
];

// ═══════════════════════════════════════════════════════
// WRITE ALL WEAPONS
// ═══════════════════════════════════════════════════════
const allWeapons = [...assaultRifles, ...smgs, ...dmrs];

let written = 0;
let skipped = 0;

for (const weapon of allWeapons) {
  if (writeEntity(weapon)) {
    written++;
  } else {
    skipped++;
  }
}

console.log(`\n=== COMPLETE ===`);
console.log(`Total defined: ${allWeapons.length}`);
console.log(`Written: ${written}`);
console.log(`Skipped (existing): ${skipped}`);
console.log(`Assault Rifles: ${assaultRifles.length}`);
console.log(`SMGs/PDWs: ${smgs.length}`);
console.log(`DMRs: ${dmrs.length}`);
