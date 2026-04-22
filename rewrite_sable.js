// Rewrite Sable (019d6143a6c07bbdb0144496b7f489a7) per user spec:
//   - 45-year-old professional woman, skin of a 30-year-old (cosmetic maintenance)
//   - Circular ocular implants like two black telescopes (new signature augment)
//   - Long strawberry-red hair in a loose braid
//   - Professional dress, tan coat, hands almost never leave the pockets
//   - Does not shake hands, does not carry anything
//   - Fixer who lines up jobs for Kyle
//   - ZERO references to chrome jaw, armless-ness, or missing hands
const fs = require('fs');
const FILE = 'D:/Projects/MindAttic/StreetSamurai/engine/data/people/019d6143a6c07bbdb0144496b7f489a7.json';
const s = JSON.parse(fs.readFileSync(FILE, 'utf8'));

s.age = 45;

s.description = `Sable is a slightly older professional woman — forty-five with the skin of a thirty-year-old, the result of discreet cosmetic maintenance that reads as wealth rather than vanity. She wears her strawberry-red hair long and always in a loose braid that falls over one shoulder, never repinned during a conversation no matter how long the conversation runs. Her clothes are tailored: professional dress, tier-neutral colors, fabric that costs enough to be worth not noticing. The tan coat — always the tan coat — is long, well-cut, knee-length, with deep pockets, and her hands almost never leave those pockets. She does not shake hands. She does not remove her hands to accept what people offer her. She does not carry anything. No bag, no case, no visible device. Whatever she needs she already knows. Whatever she is offering arrives by other channels.

Her eyes are replaced. Two circular ocular implants, black, telescopic — the housings machined to tolerances most people's apartments cost, the aperture visible as a small mechanical ring that adjusts when she shifts focus. The adjustment makes a sound. It is not loud. Most people do not notice it the first time they meet her. They notice it the second time, and then they cannot unnotice it, and then they begin to understand what the implants are worth and what that says about what Sable is worth. The pupils are not pupils. They are black glass. People read the rest of her face and find it professional and honest, and then they meet the telescopes and the reading stops working. She built for that.

Kyle's code tells him Sable is not a threat. She is visibly unarmed. She has never lifted a weapon in his presence. She carries nothing. She does not even take her hands out of her pockets — a joke he makes to himself exactly once, then examines, then discards. The joke doesn't sit right. Not because it's wrong but because the ease with which his brain reached for it feels engineered, the way a magician draws your eye to the wrong hand. Sable is not unarmed. Sable is something else, something the Circuit does not have a word for yet, and the absence of that word is where the danger lives.`;

s.augmentations = `Paired circular ocular implants — black telescopic housings with visible aperture mechanics, medical-grade, premium manufacture; the aperture rings adjust audibly when she shifts focus. Cosmetic dermal maintenance that holds her skin at a thirty-year-old's tolerance at age forty-five. No other visible augments — which means the invisible ones are the ones to worry about.`;

// Speech patterns — drop chrome-jaw micro-pause framing, replace with implant-aperture tell
s.speech_patterns.vocabulary = `Corporate jargon repurposed for the underground with dark, dry irony. 'Restructure' means kill. 'Optimize' means clean up evidence. 'Deliverable' means the target. Her voice is level, clean, unhurried — the voice of a woman who has never needed to raise it and does not intend to start now.`;
s.speech_patterns.cadence = `Measured. Deliberate. The ocular implants adjust their aperture slightly when she is thinking — a small mechanical sound most people do not notice and cannot unhear once they do. She uses the pause the way other fixers use the desk between themselves and the client — as distance.`;

// Narration voice — swap chrome-jaw references for implant imagery
s.narration_voice = `Sable's narration is controlled, analytical, observational. Where Kyle fragments, Sable constructs. Longer sentences that build toward a point. She thinks in transactions, leverage, and probability. She notices people before objects — body language, micro-expressions, the tell that someone is lying. The prose reads like a chess player narrating their own game: calm, three moves ahead, with dry commentary on the absurdity of everyone else's position. The ocular implants do not editorialize — they log. When she is reading a room the prose registers the aperture shift before it names what she just saw. She never describes her hands. Her hands are in her coat pockets. They stay there. No facet tags. No interior war. Sable's internal conflict is structural: she has optimized away her own humanity and occasionally catches herself wondering if that was the right trade. When that doubt surfaces, the prose doesn't tag it — it just pauses. A beat of nothing. Then the analysis resumes.`;

// Habits — remove chrome-jaw habit, add hands-in-pockets + braid
s.behavioral.habits = [
  'Rotates back rooms in the Circuit black market district — never the same room twice in a month',
  'Cooks elaborate solo meals with French technique and West African spices',
  'Maintains the cipher notebook — the one thing she trusts, the one analog record',
  "Keeps her hands in the tan coat's pockets almost exclusively; removes them only when the removal is itself the message",
  "Does not shake hands; does not accept objects directly; anything handed to her arrives later by courier to a dead-drop",
  'Annual pilgrimage to the Meridian Core building where she used to work — stands outside for exactly one hour, rain or not',
];

// Decision rules — strip chrome-jaw rule; replace with hands-in-pockets discipline
s.behavioral.decision_rules = [
  'Every human interaction is a transaction — this removes emotional risk and flattens connection into manageable data',
  'Never lie about the odds — lie about everything else if necessary but never the odds',
  'Pay operatives on time and disclose risks accurately — this is not ethics, this is asset management',
  'Never occupy the same back room twice in a month',
  'Maintain physical anonymity at all costs — if nobody sees you, nobody can hurt you',
  'Steer the better jobs toward Kyle without acknowledging this is anything other than portfolio optimization',
  'Treat Axiom contracts as small revenges — take their money, use their information, never acknowledge the satisfaction',
  'Keep the cipher notebook as the one unhackable record — trust nothing digital',
  "Never remove hands from the tan coat's pockets unless the removal is itself the instrument — stillness is the message",
  'Factor risk premium into every quote before the client asks — then present the total as non-negotiable',
];

// Escalation ladder — replace chrome-jaw stillness with implant aperture lock
s.behavioral.escalation_ladder = [
  'The implants go still — aperture locked, no adjustment, the full weight of her attention fixed and unmoving',
  'Measured corporate jargon deployed with dark irony — restructure, optimize, deliverable',
  'A single precisely chosen sentence that reveals she knows more than the other party assumed',
  'Complete transactional shutdown — the deal is dead, the relationship is reclassified',
  'Strategic information release that restructures every power dynamic in the room simultaneously',
];

// Stress responses
s.behavioral.stress_responses = {
  low: 'Cooks elaborate meals alone — French technique, West African spices, eaten in silence',
  medium: 'The cipher notebook gets updated; transaction logs reviewed; contingencies mapped',
  high: 'The ocular implants stop adjusting entirely — aperture locked; every word becomes a calculated instrument',
  critical: 'Goes to the Meridian Core on the anniversary and stands outside the building where she used to work for exactly one hour',
};

// Interpersonal modes — swap chrome-jaw line for implants line
s.behavioral.interpersonal_modes.strangers = `The ocular implants at their default reading — aperture adjusting once, logging, holding. The level voice, the hands in the coat pockets. Assesses body language, micro-expressions, and tells before acknowledging the person exists.`;

// Breaking points — remove "recognizes the hands" variant
s.behavioral.breaking_points = [
  'A contract going catastrophically wrong that requires Kyle to break his code to clean up',
  'Her identity being compromised — someone close to tracing her to a real name',
  'A client defaulting and targeting her — the fixer becoming the job',
  'Cooking for someone — the most vulnerable thing she could do',
  'Someone from her old life recognizing her at the annual Meridian Core visit',
  "Having to take her hands out of her pockets to defend herself — the fact of it, not the act",
];

// Story hooks — remove "recognizes the hands" hook
s.story_hooks = [
  'A contract goes catastrophically wrong and Sable needs Kyle to clean up — but the cleanup requires breaking his code',
  "Sable's identity is compromised — someone is close to tracing her to a real name. She needs to disappear or fight, and she has never had to fight.",
  'She offers Kyle a job working directly for Axiom. The pay is life-changing. The target makes it impossible.',
  "Sable's annual visit to the old office building is observed. Someone from her old life recognizes her.",
  'A client defaults and targets Sable — the fixer becomes the job, and the only operative she trusts is Kyle',
  "Sable cooks for someone. It's the most vulnerable thing she has ever done.",
  "Someone hands her something and she has to take it out of the pocket to accept it. She does not.",
];

// Tags — drop chrome-jaw / armless, add ocular-implants / hands-in-pockets
s.stats.tags = [
  'fixer',
  'contract-broker',
  'information-controller',
  'ocular-implants',
  'hands-in-pockets',
  'most-dangerous-person-in-the-room',
  'corporate-ghost',
  'kyle-employer',
];

// Ancestry — redistribute toward Celtic-dominant (natural strawberry red) while keeping
// the Ubiquitous Diaspora principle of unexpected global combination.
s.genetic_ancestry = {
  'Northern European': 42,
  'West African': 24,
  'East Asian': 18,
  'Mediterranean': 10,
  'Indigenous Caribbean': 4,
  'South Asian': 2,
};
s.ancestry_detail = {
  'Northern European': {
    'Celtic': { 'Scottish': 22, 'Irish': 14 },
    'Finnic': { 'Finnish': 6 },
  },
  'West African': {
    'Yoruba': { 'Yoruba': 16 },
    'Akan': { 'Akan': 8 },
  },
  'East Asian': {
    'Japanese': { 'Japanese': 12 },
    'Korean': { 'Korean': 6 },
  },
  'Mediterranean': {
    'Italian': { 'Sicilian': 6 },
    'Greek': { 'Greek': 4 },
  },
  'Indigenous Caribbean': {
    'Taíno': { 'Taíno': 4 },
  },
  'South Asian': {
    'Indian': { 'Bengali': 2 },
  },
};

// Physical description — complete replacement, the main field the user cares about
s.physical_description = {
  heritage: 'Mixed Celtic (Scottish / Irish) with West African (Yoruba) and East Asian (Japanese) dominance, layered with Mediterranean and Indigenous Caribbean — the Celtic ancestry accounts for the natural strawberry-red hair, the West African and East Asian layers for the warm olive-undertone skin and the geometry around the cheekbones. A face of the Ubiquitous Diaspora: strawberry red and olive-brown and high cheekbones from three continents and nowhere you can put your finger on.',
  height_cm: 170,
  weight_kg: 62,
  build: 'Lean professional — the body of a woman who walks a lot, sits meetings for money, and has not lifted anything heavier than a coffee cup in a decade. She does not need to be physically threatening. Her implants cost more than whatever room she is standing in.',
  hair_color: 'Strawberry red',
  hair_style: 'Long — worn in a loose, single braid that falls over her left shoulder. The braid is never repinned during a conversation. The weave has a few strands that have escaped and she does not smooth them.',
  hair_length: 'Long (mid-back when loose)',
  eye_color: 'n/a — replaced by circular black ocular implants',
  skin_tone: 'Medium olive with warm undertones — even across the face, discreetly maintained, holding at a thirty-year-old tolerance through cosmetic procedures that read as wealth rather than vanity',
  complexion: 'Smooth, precise, cared for — the skin tells you the budget. Fine lines only at the corners of the mouth and at the hairline margin around the implant housings; nothing else. The upkeep is part of how the implants read as expensive instead of alarming.',
  distinguishing_marks: [
    'Paired circular ocular implants — black, telescopic, housings machined to visible tolerance, aperture rings that adjust audibly when she shifts focus. The pupils are black glass. They do not blink the way natural eyes blink.',
    'Clean, healed margin where the orbital bone meets the implant housing — no visible scarring, the work was expensive.',
    'A single strand of strawberry-red hair that never stays braided; it sits against her cheekbone. She does not tuck it.',
  ],
  visible_augmentations: 'Paired circular ocular implants — the only visible augment. Everything else is subcutaneous or behavioral.',
  posture_movement: 'Still. She does not fidget. Her hands stay in the tan coat pockets and she does not take them out. When she turns her head the implants lead — a small audible aperture shift as she brings her attention to bear. She does not gesture with her hands because her hands are never out. She gestures, when she does, with a tilt of the chin or a rotation of the shoulders inside the coat.',
  clothing_style: 'Tan coat — always, in every season and every room — long, knee-length, well-cut, deep pockets she keeps her hands in. Professional dress underneath: tailored fitted trousers or an unassuming pencil skirt, cashmere or silk blouse in tier-neutral colors, discreet low heels or flats that have been resoled but never replaced. No jewelry. No branding. Nothing that catches light. The whole silhouette is designed to be forgotten the moment you stop looking at her — except the implants, which you cannot forget, and the coat, which she is never without.',
};

// Image prompt — rebuild for the new look
s.image_prompt = 'GLMZ character portrait of Sable: a forty-five-year-old professional woman with the skin of a thirty-year-old, mixed Celtic-West African-East Asian heritage, warm medium-olive skin tone with golden undertones, long strawberry-red hair worn in a loose single braid falling over her left shoulder. Her signature feature: paired circular ocular implants where her eyes should be — black telescopic housings with visible machined aperture rings, pupils of black glass, clean surgical margin where the orbital bone meets the housing. Wearing a long tan coat, knee-length, well-cut, collar raised, both hands in the deep pockets — she does not remove them. Tailored professional dress underneath in tier-neutral colors. No jewelry, no branding. Posture still, weight balanced, head turned slightly with the implant aperture adjusting. Moody, low-contrast neo-noir lighting — warm fill on the strawberry hair and the tan coat, cool edge light picking out the black glass of the implants. Urban cyberpunk aesthetic, restrained not flashy. Cinematic. --ar 2:3 --v 6';
s.dalle3_prompt = '';

// Narrative function — strip chrome-jaw reference, clean up the rest
s.narrative_function = `Sable is what Kyle could become if he abandoned the code — competent, successful, invisible, and hollow. She represents pragmatism taken to its logical extreme: a person who has optimized away her own humanity in exchange for control and survival. She is the corporate machine's ghost — someone who left the corporation but brought its operating system with her.

But Sable is also the story's most honest character. She doesn't pretend to be noble. She doesn't dress up what she does in philosophy or tradition. She brokers violence for money and she does it with transparency and fair pay, which makes her, by the Circuit's standards, practically a saint. The question is whether that low bar is enough to constitute a moral life.

Sable poses this question to Kyle: Is your code a luxury? Can you afford principles in a world that prices them at zero? And the follow-up, which Sable would never ask aloud: If you can afford them and I can't, what does that say about which of us is really trapped?`;

fs.writeFileSync(FILE, JSON.stringify(s, null, 2));

// Verification — make sure nothing chrome-jaw or armless slipped through
const raw = fs.readFileSync(FILE, 'utf8');
const bad = ['chrome jaw', 'chrome-jaw', 'armless', 'no hands', 'no arms', 'chrome cap', 'chrome caps', 'mandible', 'marionette', 'having no hands'];
const hits = bad.map(b => [b, (raw.toLowerCase().match(new RegExp(b.toLowerCase().replace(/[-]/g, '[-]'), 'g')) || []).length])
                .filter(([,n]) => n > 0);
console.log('Residual forbidden references:', hits.length ? hits : 'NONE');
console.log('Name:', s.name);
console.log('Age:', s.age);
console.log('Hair:', s.physical_description.hair_color, '—', s.physical_description.hair_style);
console.log('Eyes:', s.physical_description.eye_color);
console.log('Coat:', s.physical_description.clothing_style.split(' ').slice(0, 4).join(' ') + '...');
console.log('Tags:', s.stats.tags);
