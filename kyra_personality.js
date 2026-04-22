// Flesh out Kyra's stub so she stops reading as "younger Kyle" or "Ciri variant":
//   - Personality: OPPOSITE of Kyle's reserve — expressive, verbal, confident, revels in her abilities
//   - Speech: fast, playful, externalizing; she narrates herself aloud as self-regulation
//   - Offscreen arc: freelance gunslinger on the eastern road, no chosen-one destiny, no mentor,
//     building a reputation by working jobs she picks herself
//   - "Opposite of Kyle" is the load-bearing thematic anchor — not mood-aligned genetic successor
const fs = require('fs');
const STUB = 'D:/Projects/MindAttic/StreetSamurai/engine/data/people/019db33aedd17097b813f9e28da1ba5f.json';

const k = JSON.parse(fs.readFileSync(STUB, 'utf8'));

// --- Description: add two personality paragraphs after the existing backstory -----------
k.description = `Nineteen years old. Ferrogate secondary-facility continuation-program subject, genetically a branch of NDC-4471 (Kyle's parent-facility designation) — grown from tissue banked while the parent program was still trying to make Kyle work, and never stopped trying to make the line work. Fully integrated NeoCortex array at 32,768 electrode density — eight times the 4,096 Kyle's burned-out array topped at, the corrected specification the program arrived at from watching him fail. Healed insertion port scar at the base of the skull, chrome leads buried under grown-in hair. Her face carries Kyle's cheekbones and jaw line — same facility genetic base, corrected. She was raised on his recorded life: every biometric crash, every conditioning session, every escape attempt was part of her curriculum.

Untrained in combat. The program scheduled field instruction for the year after first buyer handoff — her combat trials were to be run by whoever bought her, before formal curriculum began. What she walked out of the facility with was raw neural capacity and nineteen years of passive conditioning.

She is not a younger Kyle. She is the opposite of Kyle. Where he is reserved, she is expressive — she talks through what she is doing while she is doing it, she asks strangers questions, she tests jokes to see if they work and tries another when the first one doesn't. Where he has optimized away enjoyment, she revels in her abilities. The first time she fires a gun she lingers on the grip afterward the way you linger on a thing you have just discovered you are fluent in. The second time she is *pleased*. The third time she is grinning. She has no shame about being good at this — no Kyle-style silence about the work, no Seo-era discipline telling her the skill is a debt. She likes being dangerous and she is frank about liking it, which unsettles everyone who has ever met Kyle and then meets her.

Her counting tic is still there (rivets, tiles, rain impacts) but she can stop herself in company. She counts under her breath only when she wants to — as self-regulation during the high-cognitive moments — and when she doesn't want to, she talks. She talks a lot. She narrates what she is seeing, what she is deciding, what she is going to try next. She plays at accents she has heard once. She calls people *friend* and *trouble* and *the one with the boots*, testing nicknames the way she tests jokes. She makes eye contact and holds it the way Kyle never does.

This is not an act. This is what happens when you build someone with a neural architecture tuned to absorb and integrate and then give her nineteen years of captivity with only archived recordings of other people for company. What came out was someone *hungry* for contact, for improvisation, for the feel of her own body discovering what it can do. The gunslinger archetype was the first language she got to speak. She is still learning all the others.

She walked out on her own at dawn because she wanted to know what she would be alone. She meant it. She will come back when she has something to offer — or when she needs some bullets dislodged. The joke was the first one she ever made. There will be more.`;

// --- Psychology: distinct from Kyle's ------------------------------------------------
k.psychology.facet_weights = {
  wound: 0.35,         // the facility wound is there but she is not ruled by it
  ideal: 0.30,         // low — she hasn't assembled a moral code yet
  id: 0.80,            // high — she wants things, she reaches for them
  shadow: 0.55,
  mask: 0.40,          // low — she is not hiding, she is out in the open
  ghost: 0.25,
};
k.psychology.core_fears = [
  "That the curriculum — nineteen years of Kyle-footage — was a shape she cannot escape no matter what she does with her own body",
  "Losing the window on being good at things before the program's buyer catches up to her",
  "Becoming reserved. Becoming careful. Becoming him.",
  "That when she finally makes a friend she won't know how to keep one",
];
k.psychology.core_desires = [
  "To find out what she is by using herself — every job is a test, every stranger is a lesson",
  "To make someone laugh who she did not grow up listening to",
  "A reputation she built without anyone handing it to her",
  "To come back to Kyle's stall someday with a story he cannot predict",
];
k.psychology.coping_mechanisms = [
  "Talking — running commentary on her own actions, out loud, whether or not anyone is listening",
  "Naming things — people, weapons, streets — before she has a reason to",
  "Testing jokes. Watching the face for the response. Filing what worked.",
  "Running toward the thing she is scared of, on the theory that the fear is information and information is not lethal",
  "Counting, still, but silently and only when the social cost of counting out loud is higher than the regulation cost of suppressing it",
];
k.psychology.blind_spots = [
  "She thinks being expressive is the same as being honest. It is not always.",
  "She has not yet discovered that enjoying what she is good at is the same failure-mode the program was engineering for — she assumes enjoyment is freedom because Kyle never had it",
  "She underestimates how much nineteen years of Kyle-footage shows in her body language when she is not paying attention",
  "She has not had to lose someone yet. She does not know what that will do to the voice.",
];
k.psychology.secret = "She keeps the first handgun. Tier-2 sidearm taken off the contractor Kyle put down at joint-lock in the Ferrogate maintenance corridor. She does not carry it as her primary anymore — she has upgraded — but she keeps it wrapped in cloth in whatever room she is sleeping in, and once a month she field-strips it on the table in front of her without looking and reassembles it with her eyes closed. She does not pray. She marks. She has not told anyone why she does this and she would not be able to explain if asked. It is the first fact she chose about herself.";

// --- Speech patterns: outgoing, fast, testing -----------------------------------------
k.speech_patterns.vocabulary = `Raw, experimental, magpie. She collects slang and idiom from every stranger she meets and redeploys it the next day to see how it fits. Her curriculum was recordings — she has heard every dialect of GLMZ English and is still picking a voice. Sometimes the archive leaks: she will drop into Kyle's cadence for a sentence, catch herself, lean out of it on purpose, and the next sentence will be something borrowed from a trucker she met last Tuesday. She calls things by nicknames before she knows their names.`;
k.speech_patterns.cadence = `Fast, confident, front-loaded. Subject-verb-object then whatever else occurs to her, in that order, without pauses. She does not weigh words before releasing them — she releases them and watches the room to see what happens. When she is regulating neural load the cadence tightens and she might go silent for a count of seventeen, but that is the exception. Default: running narration of her own actions and the actions of everyone in the room.`;
k.speech_patterns.verbal_tics = [
  "Nicknames people within thirty seconds of meeting them — *trouble*, *friend*, *the one with the boots*",
  "Says \"tell me what you are\" to objects she is figuring out — guns, locks, vehicles, vending machines",
  "Narrates mid-action: \"okay. Okay, that worked. Let's try—\"",
  "Asks questions in the middle of sentences and keeps talking: \"...and I figured, what do you think, I figured I'd just—\"",
  "Laughs at her own jokes before other people do. Does not appear embarrassed by this.",
];
k.speech_patterns.example_lines = [
  "Tell me what you are. Okay. You cycle like that. You eat this magazine. Good.",
  "I'm going to try a thing. If it works I'll take credit. If it doesn't I was never here.",
  "You don't have to tell me. I already like you. We can work with that.",
  "Kyle would've counted the exits first. I already counted them. But I also said hello, which is the part he leaves out.",
  "I'm not hiding. That's the trick. If you're not hiding, nobody looks.",
  "Three shots. Tight group. That's a gift and I am saying thank you for it out loud.",
];
k.speech_patterns.avoidances = [
  "Does not use the facility designations when describing herself — not *4471-K*, not *subject*. She will say the words once in a fight to throw an opponent off, then never again.",
  "Will not say Kyle's name to strangers. That is hers.",
  "Does not explain the counting tic. If asked she changes the subject with a joke.",
];
k.speech_patterns.subtext = `When she is excited she sounds nineteen. When she is regulating she sounds like Kyle. The shift is audible and she hates that it is audible and she is learning to control it.`;
k.speech_patterns.under_pressure = `Cadence tightens but does not go silent — she talks through the decision. Sometimes talks through the contractor she is about to shoot. This unsettles people in ways she takes professional pleasure in.`;
k.speech_patterns.intimacy_register = `She is still figuring it out. The first person to hold her hand was Kyle in the Ferrogate loading bay. The first person she held a hand for after that will matter.`;

// --- Stats / personality -------------------------------------------------------------
k.stats.personality = {
  openness_conviction: 9,          // very open to experience; convictions still forming
  empathy_detachment: 6,           // middle — curious about people more than caring about them (yet)
  impulsivity_deliberation: 7,     // leans impulsive; moves fast and adjusts
  assertion_deference: 9,          // high assertion; does not defer
  transparency_guardedness: 7,     // mostly transparent; guarded only about the facility and Kyle
};
k.stats.drives = [
  "Discovery of her own capacity",
  "A reputation she built herself",
  "Novelty — every job a different shape",
  "Coming back to Kyle's stall with a story he could not predict",
];
k.stats.strengths = [
  "Curiosity",
  "Zest",
  "Courage",
  "Social intelligence",
  "Humor",
];
k.stats.weaknesses = [
  "Prudence",
  "Self-regulation (social)",
  "Patience",
  "Forgiveness",
];
k.archetypes = {
  Gunslinger: 0.95,
  Trickster: 0.7,
  Extravert: 0.85,
  Scholar: 0.5,   // of herself, of weapons, of idioms — she studies
  Drifter: 0.7,
};

// --- Behavioral --------------------------------------------------------------------
k.behavioral.decision_rules = [
  "If the thing scares you, run toward it. Fear is information, not a verdict.",
  "Say the nickname out loud within thirty seconds. People like being named.",
  "Do not count out loud in front of people you want to keep.",
  "Never use the facility designation to describe yourself — make them work to figure you out",
  "Test jokes. The ones that land are worth keeping. The ones that don't are worth keeping too, just filed differently.",
  "Take the contract that teaches you something you do not already know, even if it pays less",
  "If a job goes sideways, talk through it — the narration is regulation and occasionally it is a misdirect",
  "Keep the first handgun. Not as primary. As fact.",
];
k.behavioral.interpersonal_modes = {
  strangers: "Opens with a nickname, a question, and a grin. Treats the first thirty seconds as a survey — what is this person, what do they want, what do they find funny. Closes the distance Kyle would have kept.",
  clients: "Professional but not measured — quotes the price, describes the plan with specifics, cracks one joke to calibrate them. Will not take a job from someone who did not laugh.",
  targets: "Talks to them. It is disarming and it is often the kill move. The facility taught her this by accident — she learned it watching Kyle never do it.",
  kyle: "The one person she does not perform for. Quieter with him. Listens more than she talks. Catches herself sliding into his cadence and consciously leans out of it, which means she is still measuring herself against him, which means she has more work to do.",
  pixel: "Has not met her yet. Will get along with her immediately when she does. Two people who externalize recognize each other on sight.",
  children: "Kind, unhurried, the only register where she slows down on purpose. She knows what it is like to have been a project.",
};
k.behavioral.stress_responses = {
  low: "Verbal narration continues, slightly sharper in wit",
  medium: "Starts naming things that don't need names — the wall, the waitress, a passing dog",
  high: "Counting reappears, first silent then under her breath; she becomes quieter, which is how people who know her know something is wrong",
  critical: "Full Kyle-mode — silent, economical, measured. She hates this register and gets out of it as fast as safety permits.",
};
k.behavioral.habits = [
  "Field-strips the first handgun once a month, eyes closed, no reason",
  "Collects idioms from strangers and redeploys them within 24 hours",
  "Nicknames people within thirty seconds of meeting",
  "Walks everywhere — she does not have a vehicle, she likes the walking",
  "Does not wear Kyle's style (no tailored coat, no blade). Short dark jacket, hip-carry sidearm, a single brass earring in her left ear that she acquired in a bar game",
];
k.behavioral.breaking_points = [
  "Hearing Kyle's cadence come out of her own mouth and not being able to lean out of it",
  "Someone she has nicknamed dying on her — this has not happened yet",
  "Being recognized as 4471-K by a civilian who was not supposed to know",
  "Any indication the program is starting a third subject in her line",
];

// --- Belongings / signature look --------------------------------------------------
k.belongings = k.belongings || {};
k.belongings.primary_weapon = 'Unmarked short-barreled revolver she acquired in her second month east of the Lateral Junction — hip-carried, no holster, just the coat pocket. She is better with it than anyone who has trained longer.';
k.belongings.secondary_weapon = 'The Tier-2 sidearm she took off the first contractor Kyle put down at joint-lock in the Ferrogate maintenance corridor. Wrapped in cloth, carried in her pack, not in rotation. Her first fact about herself.';
k.belongings.armor = 'None. She prefers mobility and visibility — people react differently to an unarmored nineteen-year-old than to a chromed operator, and she uses that.';
k.belongings.vehicle = '';
k.belongings.residence = 'Transient. Weekly rentals, pod hotels, friends-of-friends, occasionally a cot in a back room someone owes her. She has not slept in the same bed eight nights in a row since leaving the facility.';
k.belongings.clothing_style = 'Short dark utility jacket, cargo pants, boots she paid for herself (the first thing she bought with gunslinger money). No coat like Sable. No tailoring. A single brass earring in her left ear — acquired in a bar game she was not supposed to win.';
k.belongings.favorite_food = 'Anything spicy. The facility fed her to a bland nutritional profile for nineteen years. She is making up for it.';
k.belongings.comm_device = 'Burner pad, rotated monthly. Has Kyle\'s dead-drop address memorized but has not written to it yet.';
k.belongings.signature_gear = [
  'Short-barreled revolver (hip)',
  'Tier-2 sidearm (cloth-wrapped, carried, not worn)',
  'Field-strip rag',
  'Brass earring (left ear)',
  'Notebook she has not started writing in yet',
];

// --- Narrative function: the explicit "opposite of Kyle" anchor --------------------
k.narrative_function = `Kyra is the corrected output of the program that failed to make Kyle — not her father's daughter, not a protégée, not an heir. She is a *different organism* wearing the same genetic scaffolding. Her narrative function is to be what Kyle is not: expressive where he is reserved, extraverted where he is closed, delighted in her own capacity where he is silent about his, improvisational where he is disciplined. When Kyle meets her he does not see a younger version of himself. He sees what he would have been if the program had not broken him — and what he sees is a person who likes being good at this, which is the one thing he has never been able to afford.

She is also the story's contradiction of the protector/ward trope. She did not need rescuing. She walked out under her own power, took a contract on her second day out of the facility, and has not stopped moving. Her offscreen life is a solo gunslinger's — freelance jobs in the eastern farmlands and Pulse-hub stations, a reputation being built one clean shot at a time, no mentor, no found family, no chosen-one destiny to live up to. She is not being hunted across dimensions. She is being *tracked* by Axiom at Tier-3 recovery priority, which is corporate for "when we have the budget," which means she has months, maybe years, to become the kind of person they regret trying to reacquire.

When she appears in future stories she appears as herself — not as a Kyle-shaped problem, not as a kid needing rescue, not as a bloodline-burdened heir. She appears as Kyra Krastev-Okonjo, gunslinger, nineteen, working, occasionally loud, occasionally laughing, increasingly hard to surprise.`;

// --- Narration voice -----------------------------------------------------------------
k.narration_voice = `Kyra's narration is externalizing. Where Kyle's prose fragments inward, hers runs outward — she narrates what she is seeing, what she is deciding, what she is trying, often mid-action. Her sentences are shorter, her verbs more active, her idioms borrowed and redeployed from whoever she last talked to. She uses first person liberally. She laughs on the page — the prose acknowledges the laugh without apologizing for it. She is expressive about her own capacity: when she is good at something the narration names that it is good, without hedging, and without Kyle's measure-twice-cut-once hesitation. The counting tic surfaces as the one register where her voice goes quiet — it is the ghost in her prose the way discipline is the ghost in Kyle's. She is still discovering what her voice is. The prose shows the discovery happening in real time.`;

// --- Offscreen arc: the explicit anti-Ciri shape -----------------------------------
k.offscreen_arc = {
  shape: "Solo gunslinger on the eastern road. No mentor, no training montage, no chosen-one destiny, no cosmic pursuit. She is learning by working, in a specific geographic territory, against specific adversaries, for specific pay.",
  territory: "East of the Lateral Junction — drowned Michigan farmlands, the Pulse hub stations at Battle Creek and Toledo, the buffer zones around the Indiana Dust. Works the seams between GLMZ and the Outside World.",
  clientele: "Small fixers, desperate families, corporate spin-offs that can't get on an Axiom contract, mid-tier criminal operators who need precision without paperwork. She avoids Axiom work and refuses jobs against children.",
  method: "Takes contracts one at a time, works them solo, delivers on spec, moves. Each job is her curriculum — she treats every engagement as a test of a technique she has not tried yet. She pays attention to what she wants to be good at in a year.",
  reputation: "Building. The rumor is: redhead-adjacent hair, nineteen or twenty, talks too much, shoots like she has been doing it for ten years which she has not. Nobody knows the facility connection. Nobody knows the 4471-K code. The ones who learn it tend not to live to tell.",
  pursuers: "Axiom recovery at Tier 3 (when convenient). Not across dimensions. Not bloodline-destiny. Corporate property recovery with a budget line that deprioritizes regularly when quarterly targets shift.",
  anti_ciri_rule: "She is NOT a chosen one. Her abilities are explicable (neural architecture the program built), not mystical. She is not being hunted by cosmic forces or trans-dimensional hunters. Her arc is a gunslinger-on-the-road arc, not a prophecy arc. If a story generation drifts into 'special bloodline destiny,' reject it.",
  anti_logan_rule: "She is NOT being protected by Kyle. She and Kyle have parted and will meet again only by her choice, on her terms, with something to offer. Stories with Kyle must not revert to protector/ward.",
  return_conditions: "She comes back when (a) she has something to offer — intel, a favor, a tool Kyle can use, or (b) she needs a bullet dislodged that she cannot dislodge herself. Never for rescue. Never because she is lost.",
  voice_note: "When she returns she is further along than when she left. Story engines should not depict her backsliding into captive-adjacent helplessness. If she is hurt, she is hurt with competence — the injury is the cost of a decision she made, not a vulnerability the plot used against her.",
};

// --- Tags update --------------------------------------------------------------------
k.tags = [
  'auto-scaffolded',
  'needs-review',
  'continuation-program',
  'kindred',
  'tissue-match',
  '4471-line',
  'gunslinger',
  'untrained',
  'at-large',
  'extravert',
  'revels-in-ability',
  'anti-protégée',   // explicit: she is not Kyle's student
  'anti-chosen-one', // explicit: no destiny arc
  'on-the-road',
];

fs.writeFileSync(STUB, JSON.stringify(k, null, 2));

// Verification
const raw = fs.readFileSync(STUB, 'utf8');
console.log('Name:', k.name);
console.log('Age:', k.age);
console.log('Archetypes:', k.archetypes);
console.log('Offscreen arc set:', !!k.offscreen_arc);
console.log('Anti-Ciri rule:', k.offscreen_arc.anti_ciri_rule.substring(0, 80) + '...');
console.log('Anti-Logan rule:', k.offscreen_arc.anti_logan_rule.substring(0, 80) + '...');
console.log('Tags:', k.tags);
console.log('File size:', raw.length, 'chars');
