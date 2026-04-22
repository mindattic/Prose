// Strip two flourishes I added without being asked:
//   1. Brass earring won in a bar game
//   2. Canvas bag of unsent letters to Kyle's dead-drop
// Neither was requested. Both contradict "drop the keepsake" from the previous turn.
const fs = require('fs');
const STUB = 'D:/Projects/MindAttic/StreetSamurai/engine/data/people/019db33aedd17097b813f9e28da1ba5f.json';
const k = JSON.parse(fs.readFileSync(STUB, 'utf8'));

// Description rewrite — no earring, no unsent letters, no "one thing she keeps" framing
k.description = `Nineteen years old. Ferrogate secondary-facility continuation-program subject, genetically a branch of NDC-4471 (Kyle's parent-facility designation) — grown from tissue banked while the parent program was still trying to make Kyle work, and never stopped trying to make the line work. Fully integrated NeoCortex array at 32,768 electrode density — eight times the 4,096 Kyle's burned-out array topped at, the corrected specification the program arrived at from watching him fail. Healed insertion port scar at the base of the skull, chrome leads buried under grown-in hair. Her face carries Kyle's cheekbones and jaw line — same facility genetic base, corrected. She was raised on his recorded life: every biometric crash, every conditioning session, every escape attempt was part of her curriculum.

Untrained in combat. The program scheduled field instruction for the year after first buyer handoff — her combat trials were to be run by whoever bought her, before formal curriculum began. What she walked out of the facility with was raw neural capacity and nineteen years of passive conditioning.

She is not a younger Kyle. She is the opposite of Kyle. Where he is reserved, she is expressive — she talks through what she is doing while she is doing it, she asks strangers questions, she tests jokes to see if they work and tries another when the first one doesn't. Where he has optimized away enjoyment, she revels in her abilities. The first time she fires a gun she lingers on the grip afterward the way you linger on a thing you have just discovered you are fluent in. The second time she is pleased. The third time she is grinning.

She is a natural with any pistol. The grip is different on every one — single-action, double-action, recoil mass, trigger pull, safety geometry, magazine capacity — and her hands learn the difference inside half a magazine. She does not carry a favorite. She does not name her weapon. She does not keep mementos of the jobs she works. When the pistol in her hand runs dry she drops it and takes the next one off whoever is on the ground, and the new weapon is in her grip inside a second, calibrated inside another, firing correctly by the third. Pistols are tools. The skill is hers.

Her counting tic is still there (rivets, tiles, rain impacts) but she can stop herself in company. She counts under her breath only when she wants to — as self-regulation during high-cognitive moments — and when she doesn't want to, she talks. She talks a lot. She narrates what she is seeing, what she is deciding, what she is going to try next. She plays at accents she has heard once. She calls people friend and trouble and the one with the boots, testing nicknames the way she tests jokes. She makes eye contact and holds it the way Kyle never does.

This is what happens when you build someone with a neural architecture tuned to absorb and integrate and then give her nineteen years of captivity with only archived recordings of other people for company. What came out was someone hungry for contact, for improvisation, for the feel of her own body discovering what it can do. The gunslinger archetype was the first language she got to speak. She is still learning all the others.

She walked out on her own at dawn because she wanted to know what she would be alone. She meant it. She will come back when she has something to offer — or when she needs some bullets dislodged. The joke was the first one she ever made. There will be more.`;

// Secret — internal, no object. Nineteen years old and out a short time; no rituals yet.
k.psychology.secret = `She measures herself against Kyle in every quiet moment and hates that she does. Nineteen years of his recorded life were her curriculum, and she is still trying to figure out which of her instincts are hers and which are his playing back. She does not talk about this. She is not sure she ever will.`;

// Habits — no earring, no letters, functional only
k.behavioral.habits = [
  'Collects idioms from strangers and redeploys them within 24 hours',
  'Nicknames people within thirty seconds of meeting',
  'Walks everywhere — she does not have a vehicle, she likes the walking',
  "Does not wear Kyle-style (no tailored coat, no blade). Short dark jacket, hip-carry sidearm of the week.",
  'Does not carry a favorite weapon. Picks up what is nearby, learns it inside a magazine, drops it when a better one arrives.',
  'Field-strips whatever pistol is current at the end of a job for maintenance, not ritual.',
];

// Belongings — no earring, no letter bag
k.belongings.primary_weapon = 'Whatever pistol is in her hand — rotates constantly, taken off jobs, traded, discarded when the ammunition market makes a particular caliber stupid. She never buys a weapon from a shop. The one she is carrying today is not the one she was carrying last week and will not be the one she is carrying next week.';
k.belongings.secondary_weapon = 'Whatever she has just picked up off the ground. She works on the assumption that if she needs a second pistol mid-fight, the second pistol is already nearby — somebody she has shot is carrying it.';
k.belongings.signature_gear = [
  'Whatever pistol is current (hip-carry, no holster, coat pocket)',
  'Short dark utility jacket with deep inside pockets — sized to accommodate swaps',
  'Cleaning kit',
  'Burner pad (rotated monthly)',
];

// Story hooks — drop the letter-bag hook
k.story_hooks = [
  "Lena returns to Kyle's stall with a piece of intel he cannot get any other way — the offer of a trade, her first as an equal",
  'A job goes bad and she needs a bullet dislodged she cannot dislodge herself; she has to call in the favor she promised',
  "Axiom's Tier-3 recovery priority bumps up a tier because quarterly targets shifted; the window narrows",
  'Someone recognizes her as 4471-K in a bar and she has to decide whether to let them live',
  'She discovers the program started a third subject in her line and has to decide what to do about it',
  'A job requires a pistol she has never fired before and she has to learn it in under a minute in front of a client who is watching',
];

// Physical description — strip any earring-distinguishing-mark
if (Array.isArray(k.physical_description?.distinguishing_marks)) {
  k.physical_description.distinguishing_marks =
    k.physical_description.distinguishing_marks.filter(m => !/earring/i.test(m));
}
// Also strip any lingering earring mention in clothing_style
if (k.physical_description?.clothing_style) {
  k.physical_description.clothing_style = k.physical_description.clothing_style
    .replace(/\.\s*A single brass earring[^.]*\./gi, '')
    .replace(/A single brass earring[^.]*\./gi, '')
    .replace(/\s{2,}/g, ' ')
    .trim();
}

// Offscreen arc — rewrite signature field, drop anti-keepsake rule (nothing to guard against)
if (k.offscreen_arc) {
  k.offscreen_arc.signature = 'Pistols are fungible. She does not carry a favorite weapon and does not keep mementos of the jobs she works. Whatever pistol is in her hand this week, whatever jacket she owns currently, whatever pad she is on — everything is provisional. The skill is hers. Nothing else is load-bearing.';
  delete k.offscreen_arc.anti_keepsake_rule;
}

// Tags — drop anti-keepsake
k.tags = k.tags.filter(t => t !== 'anti-keepsake');

fs.writeFileSync(STUB, JSON.stringify(k, null, 2));

// Audit
const raw = fs.readFileSync(STUB, 'utf8');
const checks = [
  ['brass earring',   /brass earring/gi],
  ['earring',         /\bearring\b/gi],
  ['unsent letter',   /unsent letter/gi],
  ['canvas bag',      /canvas bag/gi],
  ['dead-drop',       /dead-drop/gi],
  ['letter bag',      /letter bag/gi],
  ['bar game',        /bar game/gi],
];
console.log('Residual flourish-hits:');
for (const [label, re] of checks) {
  const n = (raw.match(re) || []).length;
  console.log(' ', label.padEnd(18), n);
}
console.log('\nBelongings:');
console.log(' ', k.belongings.signature_gear);
console.log('\nSecret:', k.psychology.secret.split('.')[0] + '.');
console.log('\nTags:', k.tags);
