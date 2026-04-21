// One-off cleanup: detect character Psychology.Secret fields whose
// grammatical subject is a DIFFERENT named person than the character
// owner, and clear them. The trivia service renders secrets as
// "<Owner>'s secret: <text>", which reads nonsensically when <text>
// actually describes someone else (generation-side data bug).
//
// Pass --dry to preview without writing. Default behavior writes
// `psychology.secret = ""` on flagged files.

const fs = require("fs");
const path = require("path");

const dir = path.join(__dirname, "..", "engine", "data", "people");
const DRY = process.argv.includes("--dry");

const norm = s => (s || "").toString().toLowerCase().normalize("NFD").replace(/\p{Diacritic}/gu, "");
const ownerTokens = name => norm(name).split(/[\s\-]+/).filter(x => x.length >= 2);

function stripPreface(s) {
    let prev;
    do {
        prev = s;
        s = s.replace(/^\s*[—–-]*\s*/, "");
        s = s.replace(/^(Three|Two|Four|Five|Six|Seven|Eight|Nine|Ten|Twelve|Fifteen|Twenty|Thirty|Forty|Fifty|A few)\s+(months?|years?|weeks?|days?|decades?)\s+ago,?\s*/i, "");
        s = s.replace(/^(In|During|After|Before|Since|On|At|By|For|Across|Through|Over|Under|Within)\s+(the\s+)?[^,.]+,\s*/i, "");
        s = s.replace(/^(When|While|If|Because|Although|Though)\s+[^,.]+,\s*/i, "");
        s = s.replace(/^On\s+(three|two|four|five|six|several|multiple)\s+separate\s+occasions,?\s*/i, "");
        s = s.replace(/^(Twenty|Thirty|Forty|Fifty|Sixty|Seventy|Eighty|Ninety)-(one|two|three|four|five|six|seven|eight|nine)\s+years?\s+ago,?\s*/i, "");
    } while (s !== prev);
    return s;
}

const verbs = "is|are|was|were|has|have|had|makes?|made|sends?|sent|handed|killed|believes?|fabricated|performed|buried|traded|gave|given|negotiated|reported|found|discovered|started|began|keeps?|kept|owns?|runs?|ran|operated|operates|built|works?|worked|does|did|loves|loved|hates|hated|fears|feared|knows|knew|uses?|used|carries|carried|stole|steals|wrote|writes|sees|saw|tells|told|hides|hid";
const subjectRe = new RegExp("^([A-ZÀ-Ÿ][\\p{L}\\p{M}'\\.\\-]+(?:\\s+[A-ZÀ-Ÿ][\\p{L}\\p{M}'\\.\\-]+)?)\\s+(?:" + verbs + ")\\b", "u");

// Institutional / org / entity tokens that can legitimately appear as the
// grammatical subject of the first sentence without the secret being
// misattributed. When these appear and the owner is referred to via
// pronouns elsewhere, the secret is kept.
const ORG_TOKENS = new Set([
    "ferrogate", "tessera", "arcturus", "helix", "palladian", "vantablack",
    "ringo", "axiom", "footnote", "meridian", "libation", "zheng", "zhengdao",
    "sterling-nakamura", "sterling", "convergence", "coalition", "reclamation"
]);

// Load additional entity names from corponations/ and synthetics/ data so
// that E.L.F./corp/synthetic-named subjects like "Warm Static" or "The Atlas"
// aren't mistaken for misattributed persons.
for (const sub of ["corponations", "synthetics"]) {
    const d = path.join(__dirname, "..", "engine", "data", sub);
    if (!fs.existsSync(d)) continue;
    for (const f of fs.readdirSync(d)) {
        if (!f.endsWith(".json")) continue;
        try {
            const e = JSON.parse(fs.readFileSync(path.join(d, f), "utf8"));
            if (!e.name) continue;
            for (const t of ownerTokens(e.name)) ORG_TOKENS.add(t);
        } catch { /* skip unreadable */ }
    }
}

// Articles, existentials, and demonstratives that start sentences but
// are not person names — exclude these from subject detection.
const NON_NAME_HEADS = new Set(["the", "there", "a", "an", "this", "that", "these", "those"]);

function findSubject(secret) {
    const m = stripPreface(secret).match(subjectRe);
    if (!m) return null;
    const head = norm(m[1]).split(/[\s\-]+/)[0];
    if (NON_NAME_HEADS.has(head)) return null;
    return m[1];
}

// Owner pronouns → the object/possessive pronouns that, if present in the
// secret, indicate the owner is the real referent.
function ownerBackrefPronouns(pronouns) {
    const p = (pronouns || "").toLowerCase();
    if (p.includes("she") || p.includes("her")) return ["her", "she", "hers", "herself"];
    if (p.includes("he") || p.includes("him")) return ["he", "him", "his", "himself"];
    if (p.includes("they") || p.includes("them")) return ["they", "them", "their", "theirs", "themself", "themselves"];
    return ["he", "him", "his", "she", "her", "they", "them", "their"];
}

function hasBackref(secret, pronouns) {
    const tail = secret.slice(secret.indexOf(" ") + 1).toLowerCase();
    const re = new RegExp("\\b(" + ownerBackrefPronouns(pronouns).join("|") + ")\\b");
    return re.test(tail);
}

const files = fs.readdirSync(dir).filter(f => f.endsWith(".json"));
let total = 0, pronounLed = 0, nameMatch = 0, noSubject = 0;
const flagged = [];

for (const f of files) {
    const p = path.join(dir, f);
    const c = JSON.parse(fs.readFileSync(p, "utf8"));
    const secret = c?.psychology?.secret || "";
    if (secret.length < 20) continue;
    total++;
    if (/^\s*(He|She|They|His|Her|Their|It)\b/i.test(secret)) { pronounLed++; continue; }
    const subject = findSubject(secret);
    if (!subject) { noSubject++; continue; }
    const owner = ownerTokens(c.name);
    const aliases = (c.aliases || []).flatMap(a => ownerTokens(a));
    const subj = norm(subject).split(/[\s\-]+/).filter(x => x.length >= 2);
    if (subj.some(x => owner.includes(x) || aliases.includes(x))) { nameMatch++; continue; }
    // Institutional subject + owner-pronoun backref → likely an SVO where
    // owner is the object (e.g. "Ferrogate has been sending him flags"). Keep.
    if (subj.every(x => ORG_TOKENS.has(x)) && hasBackref(secret, c.pronouns)) {
        nameMatch++;
        continue;
    }
    flagged.push({ file: f, path: p, name: c.name, subject, secret });
}

console.log("total secrets scanned:", total);
console.log("  pronoun-led (kept):", pronounLed);
console.log("  name-led + matches owner (kept):", nameMatch);
console.log("  no clear subject (kept):", noSubject);
console.log("  FLAGGED (subject ≠ owner):", flagged.length);
console.log("---");

const previewCount = process.argv.includes("--all") ? flagged.length : 20;
const preview = flagged.slice(0, previewCount);
for (const x of preview) {
    console.log(`[${x.name}] subj="${x.subject}"`);
    console.log(`   ${x.secret.slice(0, 220).replace(/\n/g, " ")}${x.secret.length > 220 ? "..." : ""}`);
}
if (flagged.length > preview.length) console.log(`... and ${flagged.length - preview.length} more`);

if (DRY) {
    console.log("\n[--dry] no files written");
} else if (flagged.length) {
    let wrote = 0;
    // Surgical in-place replacement: swap only the `"secret": "..."` line's
    // content. Avoids a JSON round-trip that would re-escape unicode and
    // rewrite line endings across the entire file.
    for (const x of flagged) {
        const raw = fs.readFileSync(x.path, "utf8");
        // Match: optional leading whitespace + "secret": " ... " with escape
        // handling (\\ and \"), terminated by an unescaped closing quote.
        const re = /("secret"\s*:\s*)"((?:\\.|[^"\\])*)"/;
        if (!re.test(raw)) { console.warn("  [skip] secret field not found via regex:", x.file); continue; }
        const out = raw.replace(re, '$1""');
        fs.writeFileSync(x.path, out);
        wrote++;
    }
    console.log(`\ncleared secret on ${wrote} file(s)`);
}
