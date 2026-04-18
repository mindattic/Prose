const fs = require("fs");
const path = require("path");

const directories = [
  "D:/Projects/MindAttic/StreetSamurai/engine/data/weaponry",
  "D:/Projects/MindAttic/StreetSamurai/engine/data/cyberware",
  "D:/Projects/MindAttic/StreetSamurai/engine/data/equipment",
  "D:/Projects/MindAttic/StreetSamurai/engine/data/technology",
  "D:/Projects/MindAttic/StreetSamurai/engine/data/automata",
  "D:/Projects/MindAttic/StreetSamurai/engine/data/factions",
  "D:/Projects/MindAttic/StreetSamurai/engine/data/characters",
  "D:/Projects/MindAttic/StreetSamurai/engine/data/consumer_goods",
  "D:/Projects/MindAttic/StreetSamurai/engine/data/pharmaceuticals",
  "D:/Projects/MindAttic/StreetSamurai/engine/data/substrates",
  "D:/Projects/MindAttic/StreetSamurai/engine/data/synthetics",
  "D:/Projects/MindAttic/StreetSamurai/engine/data/geneware",
  "D:/Projects/MindAttic/StreetSamurai/engine/data/transportation",
  "D:/Projects/MindAttic/StreetSamurai/engine/data/ammunition",
  "D:/Projects/MindAttic/StreetSamurai/engine/data/apparel",
  "D:/Projects/MindAttic/StreetSamurai/engine/data/entertainment",
  "D:/Projects/MindAttic/StreetSamurai/engine/data/documents",
  "D:/Projects/MindAttic/StreetSamurai/engine/data/quotes",
  "D:/Projects/MindAttic/StreetSamurai/engine/data/news",
  "D:/Projects/MindAttic/StreetSamurai/engine/data/places",
  "D:/Projects/MindAttic/StreetSamurai/engine/data/archetypes",
  "D:/Projects/MindAttic/StreetSamurai/engine/data/vocabulary",
  "D:/Projects/MindAttic/StreetSamurai/engine/data/contracts",
  "D:/Projects/MindAttic/StreetSamurai/engine/data/corponations",
];

// Tag validation rules: tag -> keywords that justify the tag
// If an entry has this tag but NONE of these keywords appear in name+description+cultural_context,
// the tag is considered incorrectly assigned and removed.
const tagValidationRules = {
  ai: [
    "ai", "a.i.", "artificial intelligence", "machine learning", "autonomous",
    "neural network", "algorithm", "machine intelligence", "deep learning",
    "cognitive engine", "artificial cognition", "synthetic intelligence",
  ],
  war: [
    "war", "warfare", "military", "combat", "conflict", "battle", "soldier",
    "army", "siege", "veteran", "troops", "battalion", "campaign", "militia",
    "wartime", "warzone", "frontline", "conscript",
  ],
  death: [
    "death", "killing", "mortality", "lethal", "fatal", "dead", "die", "dies",
    "died", "corpse", "funeral", "obituary", "executed", "execution", "murder",
    "assassin", "necro",
  ],
  love: [
    "love", "romance", "affection", "relationship", "intimate", "devotion",
    "passion", "lover", "beloved", "romantic", "heartbreak", "amour",
  ],
  rain: [
    "rain", "weather", "storm", "downpour", "drizzle", "precipitation",
    "monsoon", "rainfall", "rainy", "thunderstorm", "shower",
  ],
  bar: [
    "bar", "tavern", "drinking", "pub", "saloon", "lounge", "taproom",
    "cantina", "speakeasy", "nightclub", "bartender", "drink",
  ],
  train: [
    "train", "rail", "transit", "railway", "locomotive", "station",
    "passenger car", "freight car", "monorail", "subway", "metro",
    "railcar", "commuter",
  ],
  fire: [
    "fire", "flame", "burning", "incendiary", "blaze", "inferno", "arson",
    "combustion", "ignite", "pyro", "thermal", "burn", "scorched", "ember",
    "firebomb", "flamethrower", "napalm",
  ],
  secret: [
    "secret", "classified", "hidden", "covert", "clandestine", "concealed",
    "stealth", "undercover", "confidential", "redacted", "black ops",
    "shadow", "espionage", "spy", "intelligence",
  ],
};

function getSearchableText(entry) {
  const parts = [];
  if (entry.name) parts.push(entry.name);
  if (entry.description) parts.push(entry.description);
  if (entry.cultural_context) parts.push(entry.cultural_context);
  if (entry.tactical_use) parts.push(entry.tactical_use);
  if (entry.role) parts.push(entry.role);
  if (entry.content) parts.push(entry.content);
  if (entry.text) parts.push(entry.text);
  if (entry.quote) parts.push(entry.quote);
  return parts.join(" ").toLowerCase();
}

function shouldRemoveTag(tag, searchText) {
  const lowerTag = tag.toLowerCase().trim();

  // Empty tags always removed
  if (!lowerTag) return true;

  // Check validation rules
  const keywords = tagValidationRules[lowerTag];
  if (!keywords) return false; // No rule for this tag, keep it

  // Check if any keyword appears in the searchable text
  for (const keyword of keywords) {
    if (searchText.includes(keyword.toLowerCase())) {
      return false; // Found a justifying keyword, keep the tag
    }
  }

  return true; // No keywords found, remove the tag
}

function processDirectory(dirPath) {
  const dirName = path.basename(dirPath);
  let files;
  try {
    files = fs.readdirSync(dirPath).filter((f) => f.endsWith(".json"));
  } catch (err) {
    console.log(`\n--- ${dirName} ---`);
    console.log(`  SKIPPED: Directory not found or not readable`);
    return null;
  }

  let filesProcessed = 0;
  let filesModified = 0;
  let totalTagsRemoved = 0;
  const removedTagCounts = {};
  let finalTagCount = 0;

  for (const file of files) {
    const filePath = path.join(dirPath, file);
    let data;
    try {
      const raw = fs.readFileSync(filePath, "utf8");
      data = JSON.parse(raw);
    } catch (err) {
      continue; // Skip unparseable files
    }

    filesProcessed++;

    if (!Array.isArray(data.tags)) continue;

    const originalTags = [...data.tags];
    const searchText = getSearchableText(data);

    // Step 2: Remove incorrectly assigned tags
    let cleanedTags = data.tags.filter((tag) => {
      if (typeof tag !== "string") return false;
      const remove = shouldRemoveTag(tag, searchText);
      if (remove) {
        const lowerTag = tag.toLowerCase().trim();
        if (lowerTag) {
          removedTagCounts[lowerTag] = (removedTagCounts[lowerTag] || 0) + 1;
        }
      }
      return !remove;
    });

    // Step 4: Normalize to lowercase
    cleanedTags = cleanedTags.map((t) => t.toLowerCase().trim());

    // Step 5: Remove empty strings
    cleanedTags = cleanedTags.filter((t) => t.length > 0);

    // Step 3: Remove duplicates (preserve order)
    const seen = new Set();
    cleanedTags = cleanedTags.filter((t) => {
      if (seen.has(t)) {
        removedTagCounts[t] = (removedTagCounts[t] || 0) + 1;
        totalTagsRemoved++;
        return false;
      }
      seen.add(t);
      return true;
    });

    const tagsRemoved = originalTags.length - cleanedTags.length;
    totalTagsRemoved += tagsRemoved - (originalTags.length - cleanedTags.length - tagsRemoved >= 0 ? 0 : 0);
    // Recalculate properly
    const removedInValidation = originalTags.length - cleanedTags.length;

    finalTagCount += cleanedTags.length;

    if (removedInValidation > 0) {
      data.tags = cleanedTags;
      fs.writeFileSync(filePath, JSON.stringify(data, null, 2) + "\n", "utf8");
      filesModified++;
    }
  }

  // Count total removed from the removedTagCounts map
  const totalFromMap = Object.values(removedTagCounts).reduce((a, b) => a + b, 0);

  // Top 10 most removed
  const topRemoved = Object.entries(removedTagCounts)
    .sort((a, b) => b[1] - a[1])
    .slice(0, 10);

  console.log(`\n--- ${dirName} ---`);
  console.log(`  Files processed: ${filesProcessed}`);
  console.log(`  Files modified:  ${filesModified}`);
  console.log(`  Tags removed:    ${totalFromMap}`);
  console.log(`  Final tag count: ${finalTagCount}`);
  if (topRemoved.length > 0) {
    console.log(`  Top removed tags:`);
    for (const [tag, count] of topRemoved) {
      console.log(`    "${tag}": ${count}`);
    }
  }

  return {
    dir: dirName,
    filesProcessed,
    filesModified,
    tagsRemoved: totalFromMap,
    finalTagCount,
    topRemoved,
  };
}

console.log("=== Tag Cleanup Script ===");
console.log(`Processing ${directories.length} directories...\n`);

let grandTotalFiles = 0;
let grandTotalModified = 0;
let grandTotalRemoved = 0;
let grandFinalTags = 0;

for (const dir of directories) {
  const result = processDirectory(dir);
  if (result) {
    grandTotalFiles += result.filesProcessed;
    grandTotalModified += result.filesModified;
    grandTotalRemoved += result.tagsRemoved;
    grandFinalTags += result.finalTagCount;
  }
}

console.log("\n========== GRAND TOTAL ==========");
console.log(`Total files processed: ${grandTotalFiles}`);
console.log(`Total files modified:  ${grandTotalModified}`);
console.log(`Total tags removed:    ${grandTotalRemoved}`);
console.log(`Total final tags:      ${grandFinalTags}`);
