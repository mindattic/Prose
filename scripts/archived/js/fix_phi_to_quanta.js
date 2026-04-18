/**
 * fix_phi_to_quanta.js
 *
 * Finds and replaces incorrect uses of "Phi" / "phi" (as a currency name)
 * with "Quanta" / "quanta" across all JSON files in engine/data/.
 *
 * The symbol Φ (Unicode U+03A6) is the QUANTA currency symbol and is
 * left untouched. Only the English word "Phi"/"phi" used as a currency
 * name is replaced.
 */

const fs = require("fs");
const path = require("path");

const DATA_DIR = path.resolve(__dirname, "..", "engine", "data");

let filesChecked = 0;
let filesModified = 0;
let totalReplacements = 0;

function getJsonFiles(dir) {
  const results = [];
  for (const entry of fs.readdirSync(dir, { withFileTypes: true })) {
    const fullPath = path.join(dir, entry.name);
    if (entry.isDirectory()) {
      results.push(...getJsonFiles(fullPath));
    } else if (entry.isFile() && entry.name.endsWith(".json")) {
      results.push(fullPath);
    }
  }
  return results;
}

function fixPhiToQuanta(text) {
  let count = 0;

  // Replace \bPhi\b (whole-word, capitalized) with Quanta
  // This covers: "Phi " "Phi," "Phi." " Phi" "the Phi" "in Phi" "of Phi"
  // "anonymous Phi" "local Phi" "currency Phi" "called Phi" "named Phi" etc.
  // But we must NOT touch the Φ character itself.
  const result = text.replace(/\bPhi\b/g, (match, offset, str) => {
    // Check if this is preceded by Φ somehow (shouldn't happen, but guard)
    // Also skip if inside a URL or something weird — but for JSON data this is fine
    count++;
    return "Quanta";
  });

  // Replace \bphi\b (whole-word, lowercase) with Quanta (lowercase context)
  // In dialogue/slang: "two phi", "five phi", "every phi", "your phi"
  const result2 = result.replace(/\bphi\b/g, (match, offset, str) => {
    // Guard: don't replace inside "Φ" (the character itself is not "phi" in text)
    // Guard: skip "phi" if it's part of the literary_rules quanta_symbol definition
    // that explains what phi is NOT — but the regex won't match Φ anyway.
    count++;
    return "quanta";
  });

  return { text: result2, count };
}

// Main
const files = getJsonFiles(DATA_DIR);

for (const filePath of files) {
  filesChecked++;
  const original = fs.readFileSync(filePath, "utf-8");
  const { text: modified, count } = fixPhiToQuanta(original);

  if (count > 0) {
    fs.writeFileSync(filePath, modified, "utf-8");
    filesModified++;
    totalReplacements += count;
    const relPath = path.relative(DATA_DIR, filePath);
    console.log(`  [${count} replacements] ${relPath}`);
  }
}

console.log("");
console.log("=== Summary ===");
console.log(`Files checked:   ${filesChecked}`);
console.log(`Files modified:  ${filesModified}`);
console.log(`Total replacements: ${totalReplacements}`);
