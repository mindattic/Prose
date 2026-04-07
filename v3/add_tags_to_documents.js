const fs = require("fs");
const path = require("path");

const documentsDir = path.resolve(__dirname, "../engine/data/documents");

// Known entities and themes to look for in content
const corponations = [
  "arcturus", "tessera", "ringo", "ouroboros", "vantablack", "lazarus", "crucible",
  "axiom", "palladian", "ironclad", "pinnacle", "solace", "kindred", "calyx",
  "veridian", "helix", "stratos", "dominion", "bastion", "ninth_circle",
  "agrisystems", "novatech", "omnicorp"
];

const locations = [
  "shelf", "circuit", "harbor", "spires", "underworld", "meridian",
  "grind", "breach", "lake_michigan", "great_lakes", "alaska",
  "denver", "arctic", "sector"
];

const themes = [
  "war", "crime", "technology", "augment", "augmentation", "bci",
  "synthetic", "elf", "corporate", "medical", "political", "military",
  "surveillance", "privacy", "security", "hacking", "cybersecurity",
  "manufacturing", "fabrication", "nanofabrication", "printing",
  "genetic", "genomic", "biotech", "pharmaceutical", "drug",
  "ai", "artificial_intelligence", "machine_learning", "algorithm",
  "quantum", "neural", "cyberware", "geneware", "substrate",
  "weapon", "weaponry", "ammunition", "armor", "defence",
  "transport", "transit", "vehicle", "drone", "aerial",
  "law", "legal", "regulation", "governance", "justice", "enforcement",
  "economics", "trade", "currency", "quanta", "labor", "employment",
  "religion", "faith", "spiritual", "philosophy",
  "education", "research", "university", "academy",
  "media", "journalism", "propaganda", "broadcast", "news",
  "food", "agriculture", "water", "energy", "power",
  "housing", "infrastructure", "construction", "architecture",
  "espionage", "intelligence", "smuggling", "black_market",
  "runner", "mercenary", "freelance", "contract",
  "supermind", "sentient", "consciousness", "autonomy",
  "pollution", "environment", "climate", "ecology",
  "poverty", "inequality", "class", "tier",
  "immigration", "refugee", "diaspora", "heritage",
  "music", "art", "culture", "entertainment", "sport",
  "communication", "network", "signal", "frequency",
  "corruption", "conspiracy", "cover_up", "classified",
  "death", "violence", "trauma", "addiction", "mental_health"
];

// Map of word patterns to tags
const keywordToTag = {
  "3d print": "3d_printing",
  "nanofab": "nanofabrication",
  "brain.computer": "bci",
  "neural mesh": "bci",
  "neural interface": "bci",
  "brain interface": "bci",
  "gauss": "gauss_weaponry",
  "electromagnetic": "electromagnetic",
  "acoustic": "acoustic",
  "sonar": "acoustic",
  "hydrophone": "acoustic",
  "geopoliti": "geopolitics",
  "sovereign": "sovereignty",
  "permanent fund": "sovereign_wealth",
  "dividend": "economics",
  "corpona": "corporate_sovereignty",
  "big 20": "big_20",
  "tier system": "tier_system",
  "tier 1": "tier_system",
  "tier 2": "tier_system",
  "tier 3": "tier_system",
  "tier 4": "tier_system",
  "black market": "black_market",
  "ninth circle": "ninth_circle",
  "patchwork": "patchwork",
  "spindle": "spindle",
  "fabricator": "synthetic",
  "e\\.l\\.f": "elf",
  "stray e": "elf",
  "supermind": "supermind",
  "permanent resident": "citizenship",
  "citizenship": "citizenship",
  "credential": "identity",
  "identity": "identity",
  "biometric": "biometric",
  "encrypt": "encryption",
  "decrypt": "encryption",
  "firewall": "cybersecurity",
  "intrusion": "cybersecurity",
  "exploit": "cybersecurity",
  "malware": "cybersecurity",
  "implant": "augmentation",
  "prosthe": "augmentation",
  "cybernetic": "cyberware",
  "gene therap": "geneware",
  "gene edit": "geneware",
  "crispr": "geneware",
  "substrate": "substrate",
  "autonomous": "autonomous_systems",
  "robot": "robotics",
  "automat": "automation",
  "sensor": "sensor_technology",
  "camera": "surveillance",
  "monitor": "surveillance",
  "tracking": "surveillance",
  "recogni": "surveillance",
  "facial": "surveillance",
  "prison": "incarceration",
  "incarcer": "incarceration",
  "parole": "justice_system",
  "sentenc": "justice_system",
  "court": "justice_system",
  "tribunal": "justice_system",
  "patent": "intellectual_property",
  "copyright": "intellectual_property",
  "freelanc": "freelance",
  "mercen": "mercenary",
  "bounty": "bounty",
  "assassin": "assassination",
  "smuggl": "smuggling",
  "traffick": "trafficking",
  "undercover": "espionage",
  "classified": "classified",
  "restricted": "restricted",
  "redacted": "redacted",
  "investig": "investigation",
  "autopsy": "forensics",
  "forensic": "forensics",
  "anomal": "anomaly",
  "paranorm": "paranormal",
  "unexplained": "anomaly",
  "haunted": "paranormal",
  "mutat": "mutation",
  "contamin": "contamination",
  "toxic": "contamination",
  "pollut": "pollution",
  "radiat": "radiation",
  "outbreak": "outbreak",
  "epidemic": "medical_emergency",
  "pandemic": "medical_emergency",
  "quarantine": "quarantine",
  "hospital": "medical",
  "clinic": "medical",
  "surgeon": "medical",
  "doctor": "medical",
  "vertiport": "aerial_transit",
  "taxi": "transit",
  "subway": "transit",
  "maglev": "transit",
  "ferry": "transit",
  "dock": "harbor",
  "port": "harbor",
  "warehouse": "logistics",
  "cargo": "logistics",
  "shipping": "logistics",
  "freight": "logistics"
};

function extractTags(doc) {
  const tags = new Set();

  // Always include category or document_type
  const category = (doc.category || "").toLowerCase().replace(/\s+/g, "_");
  const documentType = (doc.document_type || "").toLowerCase().replace(/\s+/g, "_");
  if (category) tags.add(category);
  if (documentType) tags.add(documentType);

  // Include classification if present
  const classification = (doc.classification || "").toLowerCase();
  if (classification && classification !== "public") {
    tags.add(classification);
  }

  // Build a combined text to search
  const textParts = [
    doc.title || "",
    doc.name || "",
    doc.description || "",
    doc.body || "",
    (doc.headings || []).join(" "),
    (doc.related_entities || []).join(" "),
    (doc.story_hooks || []).join(" ")
  ];
  const fullText = textParts.join(" ").toLowerCase();

  // Check for corponation mentions
  for (const corp of corponations) {
    if (fullText.includes(corp)) {
      tags.add(corp);
    }
  }

  // Check for location mentions
  for (const loc of locations) {
    const pattern = new RegExp(`\\b${loc}\\b`, "i");
    if (pattern.test(fullText)) {
      tags.add(loc);
    }
  }

  // Check for theme keywords
  for (const theme of themes) {
    const searchTerm = theme.replace(/_/g, "[_ ]?");
    const pattern = new RegExp(`\\b${searchTerm}`, "i");
    if (pattern.test(fullText)) {
      tags.add(theme);
    }
  }

  // Check keyword-to-tag mappings
  for (const [keyword, tag] of Object.entries(keywordToTag)) {
    const pattern = new RegExp(keyword, "i");
    if (pattern.test(fullText)) {
      tags.add(tag);
    }
  }

  // Extract words from the title/name for additional context
  const titleText = (doc.title || doc.name || "").toLowerCase();
  const titleWords = titleText
    .replace(/[^a-z0-9\s]/g, " ")
    .split(/\s+/)
    .filter(w => w.length > 3)
    .filter(w => !["the", "and", "for", "from", "with", "that", "this", "into", "when", "what", "where", "which", "about", "over", "under", "between", "through", "during", "before", "after", "above", "below", "making", "anything", "everything", "nothing", "something"].includes(w));

  // Add select title words as tags if they seem meaningful
  for (const word of titleWords) {
    if (tags.size >= 15) break;
    if (word.length >= 5 && !tags.has(word)) {
      // Only add if it somewhat relates to known themes
      for (const theme of [...themes, ...corponations, ...locations]) {
        if (word.includes(theme) || theme.includes(word)) {
          tags.add(word);
          break;
        }
      }
    }
  }

  // Ensure at least 5 tags by adding from the body if needed
  if (tags.size < 5) {
    // Add "meridian" if meridian 88 is mentioned
    if (fullText.includes("meridian 88") || fullText.includes("meridian88")) {
      tags.add("meridian_88");
    }
    // Try to extract more from the file_name
    if (doc.file_name) {
      const parts = doc.file_name.split("_").filter(w => w.length > 3);
      for (const part of parts) {
        if (tags.size >= 5) break;
        if (!["the", "and", "for", "from", "with"].includes(part)) {
          tags.add(part);
        }
      }
    }
  }

  // Cap at 15
  const result = Array.from(tags).slice(0, 15);

  // Ensure at least 5 by padding with generic terms from content
  if (result.length < 5 && fullText.includes("meridian")) result.push("meridian_88");
  if (result.length < 5 && fullText.includes("2200")) result.push("year_2200");
  if (result.length < 5 && fullText.includes("2199")) result.push("year_2199");
  if (result.length < 5) result.push("world_document");

  // Deduplicate final
  return [...new Set(result)].slice(0, 15);
}

function main() {
  const files = fs.readdirSync(documentsDir).filter(f => f.endsWith(".json"));
  let updated = 0;
  let skipped = 0;

  for (const file of files) {
    const filePath = path.join(documentsDir, file);
    let doc;
    try {
      doc = JSON.parse(fs.readFileSync(filePath, "utf8"));
    } catch (err) {
      console.error(`Error reading ${file}: ${err.message}`);
      continue;
    }

    if (Array.isArray(doc.tags)) {
      skipped++;
      continue;
    }

    const tags = extractTags(doc);
    doc.tags = tags;

    fs.writeFileSync(filePath, JSON.stringify(doc, null, 2) + "\n", "utf8");
    updated++;
    console.log(`Tagged: ${file} -> [${tags.join(", ")}]`);
  }

  console.log(`\nDone. Updated: ${updated}, Skipped: ${skipped}, Total: ${files.length}`);
}

main();
