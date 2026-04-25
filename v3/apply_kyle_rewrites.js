// One-shot: apply six operator-proposed rewrites to the Kyle Part I story.
// Each edit has a verbatim FIND that must match exactly once in the story.html
// field. The script archives the original to engine/data/archives/ before any
// mutation, so the change is reversible.

const fs = require('fs');
const path = require('path');

const STORY_PATH = path.resolve(__dirname, '..', 'engine', 'data', 'stories',
    '019d6143ab61752da68e0bc71595cd6c', 'story.json');
const ARCHIVE_DIR = path.resolve(__dirname, '..', 'engine', 'data', 'archives', 'stories');

const edits = [
    {
        label: '#1 — silhouette and gear at the threshold',
        find:
`He is six feet of nothing. That's the first thing. A swimmer's build stripped past lean into something the body shouldn't sustain. His cheekbones throw shadows in low light. His forearms, emerging from rolled sleeves, are rope and tendon and zero forgiveness — no softness anywhere, no insulation, as if the hardware in his chest has been running a slow auction on his body mass for years and winning. He looks like a man who forgot to eat in 2031 and has been catching up badly ever since. The katana is across his back. The hand cannon is on his right hip, holstered but not secured. He crossed the threshold and stood still and let them look.`,
        replace:
`He is six feet of nothing. That's the first thing. A swimmer's build stripped past lean into something the body shouldn't sustain. His cheekbones throw shadows in low light. His forearms, emerging from rolled sleeves, are rope and tendon and zero forgiveness — no softness anywhere, no insulation, as if the hardware in his skull has been running a slow auction on his body mass for years and winning. Thirty-two thousand electrodes don't come free. The array bills him in calories and he has been paying the invoice in flesh since he was nineteen. He looks like a man who forgot to eat in 2031 and has been catching up badly ever since.

The katana is across his back in a matte-black friction sheath — a 102-centimeter draw, hilt over the right shoulder, the tsukaito wrapping dark with use. The hamon, where it shows above the collar of the saya, is cold blue. Resting charge. The blade is called Silence and the name was not his idea. On his right hip, holstered but not secured, sits the Torii TSS-3 — bird's-head grip, no stock, the silhouette of a large revolver under a coat. Four rounds in the magazine and one in the chamber. Chorus. He named it himself, and he considers every round he has ever fired through it a failure of planning.

He crossed the threshold and stood still and let them look.`,
    },
    {
        label: '#2 — the array reading the room',
        find:
`Kyle's chest warmed. A deep, cellular warmth, like a coal being blown to life. The array read the room in the time it takes a trigger finger to begin its intention. Not the action. The intention. The micro-contraction of a forearm flexor. The specific angle of a barrel rotating in a hand not yet raised. His body processed this information and responded before his mind was done receiving it, and that is the part he cannot explain and has stopped trying to.`,
        replace:
`Kyle's chest warmed — not Silence, not yet. The array. A deep, cellular warmth behind the sternum, like a coal being blown to life, the firmware coming up from idle into the resolution it lived in when it had a reason. Thirty-two thousand electrodes, two hundred and fifty-six threads, processing at a speed his mouth could not keep up with. The room rendered itself. Six bodies. Heat signatures. The micro-contraction of a forearm flexor at the card table. The specific angle of a barrel rotating inside a waistband in a hand not yet raised. The younger one's shoulder telegraphing its intention before the intention had finished forming. Not prediction. *Precedence.* The array read the half-second before the action and put it on the page where Kyle could read it too, and his body responded before his mind was done receiving it, and that is the part he cannot explain and has stopped trying to.

*Ballistic precognition. That's what NeoCortex called it on the intake form. They didn't put it on the discharge papers because there were no discharge papers.*`,
    },
    {
        label: '#3 — Chorus opening the fight',
        find:
`The hand cannon spoke first. The sound in the enclosed concrete space was enormous — not a crack but a concussion, pressure against the eardrums, the chest, the fillings in the back teeth. The younger man's kneecap disappeared and he went down wet and final and the sound he made was not screaming because screaming requires a kind of composure he no longer had.`,
        replace:
`Chorus spoke first. Kyle's right hand found the bird's-head grip and the recoil came up the wrist instead of around it — that was the whole point of the configuration, the reason he'd ordered it from Torii in the first place, the reason he could fire one-handed without losing the line. The sound in the enclosed concrete space was enormous. Not a crack. A concussion. Twelve-gauge in a room with no acoustic mercy, pressure against the eardrums and the chest and the fillings in the back teeth. The younger man's kneecap disappeared at three meters and he went down wet and final and the sound he made was not screaming because screaming requires a kind of composure he no longer had.

*Four rounds left. Each one a failure. Don't fire the second.*`,
    },
    {
        label: '#4 — both chrome arms (passive disruption + first hard parry)',
        find:
`Kyle had already crossed the distance. The first chrome arm came in wide — hydraulic-assisted, absolutely lethal to anything standing where Kyle had been — and the katana came up through the joint space at the shoulder, the piezoelectric layer discharging on contact. The arm dropped. The man's implants shorted in sequence, a cascade failure that started at the shoulder and traveled down his spine like a rumor. He sat down. He didn't get up. Kyle was already behind him.

The second chrome arm he took at the wrist — a severing cut, clean at the joint, sparks and the shriek of torn myomer and the smell of ozone cutting through the copper-blood smell that was already rising. The wrist hit the concrete. It sat there sparking like a question no one wanted to answer. The man stared at the space where his hand had been with an expression that was not pain yet, that was the moment before pain, the moment of pure ontological revision.`,
        replace:
`Kyle had already crossed the distance. Silence cleared the sheath in the time the first chrome arm took to commit to its swing — a draw Seo had drilled into him over four years of mornings, the kind of motion that did not look fast because nothing about it was wasted. Hilt over the shoulder, blade out and live in one continuous gesture, the hamon already shifting from cold blue toward cyan as the array fed the harvesting core a forecast of the impact about to land.

The first chrome arm came in wide — hydraulic-assisted, absolutely lethal to anything standing where Kyle had been — and Silence came up through the joint space at the shoulder, kawagane to chrome, and the passive disruption layer did what it did. No charge required. The contact alone shorted the cyberlimb's motor bus. The arm dropped, dead weight, still attached but no longer the man's. The cascade traveled inboard from the shoulder socket down whatever spinal augmentation he'd paid for, implant by implant, a sequence of small electrical betrayals running through his nervous system like a rumor. He sat down. He didn't get up. Kyle was already behind him.

The second chrome arm he took at the wrist. Hard parry first — the arm came down and Silence caught it on the flat with full structural commitment, and the blade *rang*, a clean high sustained note in the concrete, and Kyle felt the shingane drink. Square-law. Hydraulic-assisted impact, full-force absorbed, the supercapacitor in the tsuka jumping from cyan to white-blue in the half-second the ring decayed. *The blade wants this. The blade has always wanted this.* Then the cut. Severing, clean at the joint, sparks and the shriek of torn myomer and the smell of ozone cutting through the copper-blood smell that was already rising. The wrist hit the concrete. It sat there sparking like a question no one wanted to answer. The man stared at the space where his hand had been with an expression that was not pain yet, that was the moment before pain, the moment of pure ontological revision.`,
    },
    {
        label: '#5 — sub-dermal plating (array tags armor before contact)',
        find:
`The sub-dermal plating stopped the first cut. Kyle knew it would. He used the flat of the blade on the man's jaw instead — a precise strike, calibrated, the sound like a dropped ceramic plate — and the reinforced jaw shattered at the hinge and the man's teeth scattered across the concrete in a small bright arc.`,
        replace:
`The sub-dermal plating stopped the first cut. Kyle knew it would. The array had read the seam at the man's collar before he'd crossed the threshold and tagged the torso as armored, and he had no intention of spending Silence's edge on ceramic. He used the flat instead — a precise strike, calibrated, the sound like a dropped ceramic plate — and the reinforced jaw shattered at the hinge and the man's teeth scattered across the concrete in a small bright arc.`,
    },
    {
        label: '#6 — sixth man + post-fight sheathing/cooldown',
        find:
`The sixth man — the expensive one, the decision-maker — had not run. Kyle put a hole through his left shoulder and left him against the far wall, not dead, not even close to dead, just completely revised.

*Sufficient. Not more. They have to be able to walk out of a hospital eventually. They have to be able to tell people what happened here.*

---

Twenty-two seconds total. Kyle stood in the aftermath and breathed. The loading dock smelled like ozone and copper and hydraulic fluid — a chemical-organic mixture that had no analogue in any experience that didn't involve this specific kind of work. Chrome limbs on the concrete like tools someone had decided were no longer useful. Blood in the drainage channels, moving in slow dark lines toward the drain.

Kyle's hands were steady. They would shake in four minutes. He had learned to use the window.`,
        replace:
`The sixth man — the expensive one, the decision-maker — had not run. He'd drawn. Some kind of compact pistol Kyle didn't bother to identify, because the array had already mapped the draw angle and the wrist rotation and the shot was coming high right and Kyle was no longer there. Chorus came back up in his right hand, the katana still live in his left, and he put a single twelve-gauge slug through the man's left shoulder and left him against the far wall, not dead, not even close to dead, just completely revised. The hamon caught the muzzle flash. White-blue now. A working charge, unspent.

*Two failures of planning. Two rounds fired. Three left.*

*Sufficient. Not more. They have to be able to walk out of a hospital eventually. They have to be able to tell people what happened here.*

---

Twenty-two seconds total. Kyle stood in the aftermath and breathed. The loading dock smelled like ozone and copper and hydraulic fluid — a chemical-organic mixture that had no analogue in any experience that didn't involve this specific kind of work. Chrome limbs on the concrete like tools someone had decided were no longer useful. Blood in the drainage channels, moving in slow dark lines toward the drain.

He sheathed Silence without wiping it. The kawagane shed at the molecular level — a cleaning ritual was for show, for other people's swords. He felt for the edge with three fingers along the flat as the blade went home. No new fractures. The hamon was still bright through the saya's seam, white-blue, the supercapacitor sitting on a working charge it had not been asked to spend. Chorus went back into the holster on his right hip, magazine down two, chamber still hot. Three rounds plus one. He registered the count the way other people registered the time.

Kyle's hands were steady. They would shake in four minutes. He had learned to use the window.`,
    },
];

function countOccurrences(haystack, needle) {
    if (!haystack || !needle) return 0;
    let count = 0, idx = 0;
    while ((idx = haystack.indexOf(needle, idx)) >= 0) {
        count++;
        idx += needle.length;
    }
    return count;
}

const story = JSON.parse(fs.readFileSync(STORY_PATH, 'utf8'));
let html = story.html;
const originalLength = html.length;

// Pre-flight: validate every FIND matches exactly once.
console.log(`Pre-flight check (story.html length: ${originalLength})`);
let allValid = true;
for (const edit of edits) {
    const n = countOccurrences(html, edit.find);
    const status = n === 1 ? 'OK' : (n === 0 ? 'NO MATCH' : `${n} MATCHES`);
    console.log(`  ${edit.label}: ${status}`);
    if (n !== 1) allValid = false;
}

if (!allValid) {
    console.log('\nABORTED — at least one edit did not match exactly once. No file changes made.');
    process.exit(1);
}

// Archive before mutating.
fs.mkdirSync(ARCHIVE_DIR, { recursive: true });
const stamp = new Date().toISOString().replace(/[-:.TZ]/g, '').slice(0, 14);
const archivePath = path.join(ARCHIVE_DIR, `${path.basename(path.dirname(STORY_PATH))}_${stamp}_pre-kyle-rewrites.json`);
fs.copyFileSync(STORY_PATH, archivePath);
console.log(`\nArchived original → ${path.relative(path.resolve(__dirname, '..'), archivePath)}`);

// Apply each rewrite in order.
for (const edit of edits) {
    const idx = html.indexOf(edit.find);
    html = html.slice(0, idx) + edit.replace + html.slice(idx + edit.find.length);
    console.log(`  applied ${edit.label}`);
}

story.html = html;
story.lastModified = new Date().toISOString();
fs.writeFileSync(STORY_PATH, JSON.stringify(story, null, 2), 'utf8');

console.log(`\nDONE. story.html length: ${originalLength} → ${html.length} (Δ${html.length - originalLength >= 0 ? '+' : ''}${html.length - originalLength})`);
console.log('Reload the Write page to see the changes.');
