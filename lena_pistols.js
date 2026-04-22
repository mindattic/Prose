// Lena Connor: drop the "first handgun wrapped in cloth" keepsake entirely.
// She is not sentimental about weapons. She is a natural with any pistol.
// When one runs dry she picks up a fresh one off whoever is on the ground.
// The pistol is fungible — the *skill* is the signature.
const fs = require('fs');
const STUB = 'D:/Projects/MindAttic/StreetSamurai/engine/data/people/019db33aedd17097b813f9e28da1ba5f.json';
const k = JSON.parse(fs.readFileSync(STUB, 'utf8'));

// --- Rewrite the description's "keeps the first handgun" beat entirely ----------
// Find the personality paragraphs and adjust the "she revels in her abilities" section
// to reflect weapon-fungibility instead of the keepsake ritual.
k.description = `Nineteen years old. Ferrogate secondary-facility continuation-program subject, genetically a branch of NDC-4471 (Kyle's parent-facility designation) — grown from tissue banked while the parent program was still trying to make Kyle work, and never stopped trying to make the line work. Fully integrated NeoCortex array at 32,768 electrode density — eight times the 4,096 Kyle's burned-out array topped at, the corrected specification the program arrived at from watching him fail. Healed insertion port scar at the base of the skull, chrome leads buried under grown-in hair. Her face carries Kyle's cheekbones and jaw line — same facility genetic base, corrected. She was raised on his recorded life: every biometric crash, every conditioning session, every escape attempt was part of her curriculum.

Untrained in combat. The program scheduled field instruction for the year after first buyer handoff — her combat trials were to be run by whoever bought her, before formal curriculum began. What she walked out of the facility with was raw neural capacity and nineteen years of passive conditioning.

She is not a younger Kyle. She is the opposite of Kyle. Where he is reserved, she is expressive — she talks through what she is doing while she is doing it, she asks strangers questions, she tests jokes to see if they work and tries another when the first one doesn't. Where he has optimized away enjoyment, she revels in her abilities. The first time she fires a gun she lingers on the grip afterward the way you linger on a thing you have just discovered you are fluent in. The second time she is *pleased*. The third time she is grinning. She has no shame about being good at this — no Kyle-style silence about the work, no Seo-era discipline telling her the skill is a debt. She likes being dangerous and she is frank about liking it, which unsettles everyone who has ever met Kyle and then meets her.

She is a natural with any pistol. The grip is different on every one — single-action, double-action, recoil mass, trigger pull, safety geometry, magazine capacity, the specific dumb arrangement a manufacturer chose because an accountant liked it — and her hands learn the difference in under half a magazine. She does not carry a favorite. She does not name her weapon. She does not wrap the first one in cloth and field-strip it by moonlight — that is Kyle's kind of ritual, the kind she watched for nineteen years and decided against. When the one in her hand runs dry she drops it and takes the next one off whoever is on the ground, and the new weapon is in her grip inside a second, calibrated inside another, firing correctly by the third. She treats pistols the way a cook treats knives at a line: the best one is the one in her hand right now. The program did not teach her this. The program did not get to teach her anything. The facility built her to be very good at precisely this and she met the opportunity for the first time in the Ferrogate maintenance corridor and has not stopped absorbing the data.

Her counting tic is still there (rivets, tiles, rain impacts) but she can stop herself in company. She counts under her breath only when she wants to — as self-regulation during the high-cognitive moments — and when she doesn't want to, she talks. She talks a lot. She narrates what she is seeing, what she is deciding, what she is going to try next. She plays at accents she has heard once. She calls people *friend* and *trouble* and *the one with the boots*, testing nicknames the way she tests jokes. She makes eye contact and holds it the way Kyle never does.

This is not an act. This is what happens when you build someone with a neural architecture tuned to absorb and integrate and then give her nineteen years of captivity with only archived recordings of other people for company. What came out was someone *hungry* for contact, for improvisation, for the feel of her own body discovering what it can do. The gunslinger archetype was the first language she got to speak. She is still learning all the others.

She walked out on her own at dawn because she wanted to know what she would be alone. She meant it. She will come back when she has something to offer — or when she needs some bullets dislodged. The joke was the first one she ever made. There will be more.`;

// --- New secret (replaces the first-handgun ritual) --------------------------------
k.psychology.secret = `She writes letters to Kyle's dead-drop address. She has not sent one. She drafts them by hand on whatever paper she has — receipts, bar napkins, the margins of a job brief — and she keeps the drafts in a canvas bag she does not let anyone touch. Each letter is a report: what she learned on the job she just finished, what the weapon did, what the client's tell was, how the nickname she tried landed, what she would do differently. Some of them are funny. Some of them are analytical. One of them is about the first time she killed someone alone and what it felt like to do it without Kyle in the room. She has not decided whether she is going to hand him the bag when she comes back or burn it. She has not decided whether the letters are for him or for her. She is not going to decide yet.`;

// --- Habits: drop the field-strip ritual, lean into weapon-swapping ----------------
k.behavioral.habits = [
  'Collects idioms from strangers and redeploys them within 24 hours',
  'Nicknames people within thirty seconds of meeting',
  'Walks everywhere — she does not have a vehicle, she likes the walking',
  'Does not wear Kyle\'s style (no tailored coat, no blade). Short dark jacket, hip-carry sidearm of the week, a single brass earring in her left ear that she acquired in a bar game',
  "Does not carry a favorite weapon. Picks up what is nearby, learns it inside a magazine, drops it if a better one arrives. Field-strips whatever is in her hand at the end of a job in a bar in front of the bartender to make small talk.",
  "Drafts letters to Kyle's dead-drop address on whatever paper is to hand. Does not send them. Keeps the drafts.",
];

// --- Decision rules: remove keep-first-handgun, add pistol-fungibility ------------
k.behavioral.decision_rules = [
  'If the thing scares you, run toward it. Fear is information, not a verdict.',
  'Say the nickname out loud within thirty seconds. People like being named.',
  'Do not count out loud in front of people you want to keep.',
  'Never use the facility designation to describe yourself — make them work to figure you out',
  "Test jokes. The ones that land are worth keeping. The ones that don't are worth keeping too, just filed differently.",
  'Take the contract that teaches you something you do not already know, even if it pays less',
  'If a job goes sideways, talk through it — the narration is regulation and occasionally it is a misdirect',
  "The best pistol is the one in your hand right now. The second-best pistol is the one on the ground next to whoever just dropped it.",
  "Do not keep a weapon you feel anything about. The feeling is how they find you.",
];

// --- Belongings: no favored weapon, no cloth-wrapped sidearm ---------------------
k.belongings.primary_weapon = 'Whatever pistol is in her hand — rotates constantly, taken off jobs, traded, discarded when the ammunition market makes a particular caliber stupid. She never buys a weapon from a shop. The one she is carrying today is not the one she was carrying last week and will not be the one she is carrying next week.';
k.belongings.secondary_weapon = 'Whatever she has just picked up off the ground. She works on the assumption that if she needs a second pistol mid-fight, the second pistol is already nearby; somebody she has shot is carrying it.';
k.belongings.signature_gear = [
  'Whatever pistol is current (hip-carry, no holster, coat pocket)',
  'Brass earring (left ear) — the one thing she keeps',
  'Canvas bag of unsent letters',
  'Notebook for idioms and nicknames',
  'Short dark utility jacket with deep inside pockets — sized to accommodate swaps',
];

// --- Story hooks: add the unsent-letters hook, drop the keepsake-gun hook ---------
k.story_hooks = [
  "Lena returns to Kyle's stall with a piece of intel he cannot get any other way — the offer of a trade, her first as an equal",
  "A job goes bad and she needs a bullet dislodged she cannot dislodge herself; she has to call in the favor she promised",
  "Axiom's Tier-3 recovery priority bumps up a tier because quarterly targets shifted; the window narrows",
  "Someone recognizes her as 4471-K in a bar and she has to decide whether to let them live",
  "She discovers the program started a third subject in her line and has to decide what to do about it",
  "She hands Kyle the canvas bag of unsent letters. Or she does not. Either decision changes the next story.",
];

// --- Narrative function: refresh to reflect pistol-fungibility ----------------------
k.narrative_function = `Lena is the corrected output of the program that failed to make Kyle — not her father's daughter, not a protégée, not an heir. She is a *different organism* wearing the same genetic scaffolding. Her narrative function is to be what Kyle is not: expressive where he is reserved, extraverted where he is closed, delighted in her own capacity where he is silent about his, improvisational where he is disciplined. Where Kyle maintains Seo's blade as prayer — one weapon, six years of oil on the strop, the relationship to the blade as moral scaffolding — Lena treats pistols as fungible instruments. She does not have a favorite. She does not wrap the first one in cloth. She picks up what is in reach, learns it in a magazine, and drops it for the next one when the next one comes. The inversion is deliberate and she is aware of it.

When Kyle meets her he does not see a younger version of himself. He sees what he would have been if the program had not broken him — and what he sees is a person who likes being good at this, which is the one thing he has never been able to afford.

She is the story's contradiction of the protector/ward trope. She did not need rescuing. She walked out under her own power, took a contract on her second day out of the facility, and has not stopped moving. Her offscreen life is a solo gunslinger's — freelance jobs in the eastern farmlands and Pulse-hub stations, a reputation being built one clean shot at a time, no mentor, no found family, no chosen-one destiny to live up to. She is not being hunted across dimensions. She is being *tracked* by Axiom at Tier-3 recovery priority, which is corporate for "when we have the budget," which means she has months, maybe years, to become the kind of person they regret trying to reacquire.

When she appears in future stories she appears as herself — not as a Kyle-shaped problem, not as a kid needing rescue, not as a bloodline-burdened heir. She appears as Lena Connor, gunslinger, nineteen, working, occasionally loud, occasionally laughing, whatever pistol in her hand this week, increasingly hard to surprise.`;

// --- Offscreen arc: tighten the "pistol-fungibility" and "unsent letters" anchors ---
k.offscreen_arc.signature = 'Pistols are fungible. She does not carry a favorite weapon and does not keep mementos of the jobs she works. What she keeps is the canvas bag of unsent letters to Kyle\'s dead-drop address and the single brass earring she won in a bar game. Nothing else survives a week.';
k.offscreen_arc.weapon_handling = 'Grabs whatever pistol is closest; learns it inside one magazine; drops it for the next one when the next one is better or the caliber runs out; never treats the weapon as meaningful. If she runs dry mid-fight, the refill is already on the ground — she assumes this and has been right every time so far.';
k.offscreen_arc.anti_ciri_rule = "She is NOT a chosen one. Her abilities are explicable (neural architecture the program built), not mystical. She is not being hunted by cosmic forces or trans-dimensional hunters. Her arc is a gunslinger-on-the-road arc, not a prophecy arc. If a story generation drifts into 'special bloodline destiny,' reject it.";
k.offscreen_arc.anti_logan_rule = "She is NOT being protected by Kyle. She and Kyle have parted and will meet again only by her choice, on her terms, with something to offer. Stories with Kyle must not revert to protector/ward.";
k.offscreen_arc.anti_keepsake_rule = "She does NOT carry a sentimental weapon. The first gun was not kept. There is no cloth-wrapped Tier-2 sidearm in a pack. If a story generation drifts into 'she still has the first gun,' reject it — the object of sentiment is the canvas bag of unsent letters, and that is off-limits for inciting incidents.";
k.offscreen_arc.return_conditions = "She comes back when (a) she has something to offer — intel, a favor, a tool Kyle can use, or (b) she needs a bullet dislodged that she cannot dislodge herself. Never for rescue. Never because she is lost.";
k.offscreen_arc.voice_note = "When she returns she is further along than when she left. Story engines should not depict her backsliding into captive-adjacent helplessness. If she is hurt, she is hurt with competence — the injury is the cost of a decision she made, not a vulnerability the plot used against her.";

// --- Physical description tweak: visible augmentations line ---------------------
// Remove any implication she carries a specific weapon as identity
// (keep other physical descriptors — height, hair, face, posture, etc.)

// --- Tags: swap out 'revels-in-ability' emphasis to also include pistol-fungible --
k.tags = [
  'auto-scaffolded',
  'needs-review',
  'continuation-program',
  'kindred',
  'tissue-match',
  '4471-line',
  'gunslinger',
  'pistol-fungible',     // NEW — she does not keep favorites
  'untrained',
  'at-large',
  'extravert',
  'revels-in-ability',
  'anti-protégée',
  'anti-chosen-one',
  'anti-keepsake',        // NEW — no sentimental weapon
  'on-the-road',
];

fs.writeFileSync(STUB, JSON.stringify(k, null, 2));

// Verify — no lingering "first handgun kept in cloth" style phrasing
const raw = fs.readFileSync(STUB, 'utf8');
const forbidden = [
  /wrapped in cloth/i,
  /field-strip.*once a month/i,
  /keeps the first handgun/i,
  /\bTier-2 sidearm.*cloth/i,
  /\bkeep the first handgun\b/i,
];
const hits = forbidden.map(r => [r.source, (raw.match(new RegExp(r.source, r.flags + 'g')) || []).length]).filter(([, n]) => n > 0);
console.log('Forbidden-phrase hits:', hits.length ? hits : 'NONE');
console.log('Name:', k.name);
console.log('Primary weapon:', k.belongings.primary_weapon.substring(0, 90) + '...');
console.log('Secret (first line):', k.psychology.secret.split('.')[0] + '.');
console.log('Tags:', k.tags);
