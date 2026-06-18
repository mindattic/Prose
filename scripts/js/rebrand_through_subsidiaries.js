const fs = require("fs");
const path = require("path");

const BASE = path.resolve(__dirname, "..", "engine", "data");

// Load subsidiary index
const subsidiaryIndex = JSON.parse(
  fs.readFileSync(path.join(BASE, "CorpoNations", "subsidiary_index.json"), "utf8")
);

// Major CorpoNation names and their matching patterns
const majorCorpos = [
  "Arcturus Defense Solutions",
  "TESSERA",
  "Ringo",
  "Ouroboros Energy",
  "Vantablack Media",
  "Lazarus Pharmaceuticals",
  "Crucible Genomics"
];

// Canonical parent name lookup (partial/case-insensitive match -> canonical)
function matchCorpo(value) {
  if (!value) return null;
  const lower = value.toLowerCase();
  if (lower.includes("arcturus")) return "Arcturus Defense Solutions";
  if (lower.includes("tessera")) return "Tessera CorpoNation";
  if (lower.includes("ringo")) return "Ringo CorpoNation";
  if (lower.includes("ouroboros")) return "Ouroboros Energy";
  if (lower.includes("vantablack")) return "Vantablack Media";
  if (lower.includes("lazarus")) return "Lazarus Pharmaceuticals";
  if (lower.includes("crucible")) return "Crucible Genomics";
  return null;
}

// Build parent -> subsidiaries map
const parentSubs = {};
for (const [subName, info] of Object.entries(subsidiaryIndex)) {
  const parent = info.parent;
  if (!parentSubs[parent]) parentSubs[parent] = [];
  parentSubs[parent].push({ name: subName, lob: info.line_of_business.toLowerCase() });
}

// Category-to-keyword mapping for subsidiary selection
const categoryKeywords = {
  weaponry: ["weapon", "arms", "munition", "armament", "firearms", "gun", "combat", "tactical", "defense", "ordnance", "precision"],
  ammunition: ["munition", "ammo", "ammunition", "arms", "ordnance", "weapon", "armament"],
  cyberware: ["neural", "medical", "prosthe", "cyber", "bci", "augment", "sensory", "cortex", "bioelectric", "implant", "cognitive", "health"],
  equipment: ["equipment", "armor", "protective", "defense", "tactical", "ballistic", "gear", "perimeter", "material"],
  technology: ["tech", "research", "data", "system", "network", "cyber", "digital", "computing", "lab", "applied", "engineering"],
  automata: ["robot", "drone", "autonom", "swarm", "aerial", "machine", "exoskel", "mech", "platform", "engagement"],
  apparel: ["apparel", "cloth", "fashion", "textile", "wear", "consumer", "outdoor", "fitness", "lifestyle"],
  consumer_goods: ["consumer", "food", "beverage", "provision", "pet", "home", "lifestyle", "care", "drink", "brew", "fitness"],
  pharmaceuticals: ["pharma", "medical", "health", "drug", "therapeutic", "treatment", "biotech", "clinical", "recovery"],
  transportation: ["vehicle", "transport", "logistic", "carrier", "courier", "transit", "aero", "marine", "automotive", "fleet"],
  entertainment: ["media", "entertainment", "publish", "game", "record", "music", "film", "studio", "sound", "broadcast"]
};

function scoreSubsidiary(sub, category) {
  const keywords = categoryKeywords[category] || [];
  let score = 0;
  for (const kw of keywords) {
    if (sub.lob.includes(kw)) score++;
  }
  return score;
}

function pickSubsidiary(parentCanonical, category) {
  const subs = parentSubs[parentCanonical];
  if (!subs || subs.length === 0) return null;

  // Score all subsidiaries by relevance
  const scored = subs.map(s => ({ ...s, score: scoreSubsidiary(s, category) }));
  scored.sort((a, b) => b.score - a.score);

  // Pick from top candidates (those with score > 0)
  const relevant = scored.filter(s => s.score > 0);
  if (relevant.length > 0) {
    // Pick randomly from top 3 relevant
    const topN = relevant.slice(0, Math.min(3, relevant.length));
    return topN[Math.floor(Math.random() * topN.length)].name;
  }

  // Fallback: random subsidiary
  return subs[Math.floor(Math.random() * subs.length)].name;
}

// Directories to process
const directories = [
  "weaponry", "cyberware", "equipment", "technology", "automata",
  "apparel", "consumer_goods", "pharmaceuticals", "ammunition",
  "transportation", "entertainment"
];

// Stats tracking
const stats = {
  totalFiles: 0,
  independent: 0,
  rebrandedPerCorpo: {},
  keptParentPerCorpo: {},
  skippedNoField: 0
};

for (const corpo of Object.keys(parentSubs)) {
  stats.rebrandedPerCorpo[corpo] = 0;
  stats.keptParentPerCorpo[corpo] = 0;
}

// Seed random for reproducibility (optional)
// Using Math.random() for true randomness as requested

for (const dirName of directories) {
  const dirPath = path.join(BASE, dirName);
  if (!fs.existsSync(dirPath)) {
    console.log(`Directory not found: ${dirPath}`);
    continue;
  }

  const files = fs.readdirSync(dirPath).filter(f => f.endsWith(".json"));

  for (const file of files) {
    const filePath = path.join(dirPath, file);
    let data;
    try {
      data = JSON.parse(fs.readFileSync(filePath, "utf8"));
    } catch (e) {
      console.error(`Failed to parse ${filePath}: ${e.message}`);
      continue;
    }

    stats.totalFiles++;

    // Determine which field to check
    // For entertainment: check "distributor" first, then fall through
    let fieldName = "manufacturer";
    let fieldValue = data.manufacturer;

    if (dirName === "entertainment") {
      if (data.distributor) {
        fieldName = "distributor";
        fieldValue = data.distributor;
      } else {
        // Entertainment without distributor field - mark independent, skip
        if (!data.manufacturer) {
          data.parent_CorpoNation = "";
          fs.writeFileSync(filePath, JSON.stringify(data, null, 2) + "\n", "utf8");
          stats.independent++;
          continue;
        }
      }
    }

    if (!fieldValue) {
      // No manufacturer/distributor field at all
      data.parent_CorpoNation = "";
      fs.writeFileSync(filePath, JSON.stringify(data, null, 2) + "\n", "utf8");
      stats.independent++;
      continue;
    }

    const parentMatch = matchCorpo(fieldValue);

    if (!parentMatch) {
      // Independent - no major CorpoNation match
      data.parent_CorpoNation = "";
      fs.writeFileSync(filePath, JSON.stringify(data, null, 2) + "\n", "utf8");
      stats.independent++;
      continue;
    }

    // Matched a major CorpoNation - decide whether to rebrand (60%) or keep parent (40%)
    if (Math.random() < 0.6) {
      // Rebrand to subsidiary
      const subsidiary = pickSubsidiary(parentMatch, dirName);
      if (subsidiary) {
        data[fieldName] = subsidiary;
        data.parent_CorpoNation = parentMatch;
        stats.rebrandedPerCorpo[parentMatch]++;
      } else {
        // No subsidiary found, keep parent
        data.parent_CorpoNation = parentMatch;
        stats.keptParentPerCorpo[parentMatch]++;
      }
    } else {
      // Keep parent branding
      data.parent_CorpoNation = parentMatch;
      stats.keptParentPerCorpo[parentMatch]++;
    }

    fs.writeFileSync(filePath, JSON.stringify(data, null, 2) + "\n", "utf8");
  }
}

// Report
console.log("\n=== REBRAND REPORT ===\n");
console.log(`Total files processed: ${stats.totalFiles}`);
console.log(`Independent (no parent match): ${stats.independent}`);
console.log();

let totalRebranded = 0;
let totalKept = 0;

console.log("Per CorpoNation:");
console.log("-".repeat(70));
for (const corpo of Object.keys(parentSubs)) {
  const rebranded = stats.rebrandedPerCorpo[corpo] || 0;
  const kept = stats.keptParentPerCorpo[corpo] || 0;
  const total = rebranded + kept;
  if (total > 0) {
    const pct = ((rebranded / total) * 100).toFixed(1);
    console.log(`  ${corpo}`);
    console.log(`    Rebranded to subsidiary: ${rebranded}  |  Kept parent: ${kept}  |  Total: ${total}  (${pct}% rebranded)`);
  }
  totalRebranded += rebranded;
  totalKept += kept;
}

console.log("-".repeat(70));
console.log(`\nTotals: ${totalRebranded} rebranded, ${totalKept} kept parent, ${stats.independent} independent`);
console.log(`Grand total: ${totalRebranded + totalKept + stats.independent} (should match ${stats.totalFiles})`);
