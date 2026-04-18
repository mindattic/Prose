const fs = require("fs");
const path = require("path");

const weaponryDir = path.join(__dirname, "..", "engine", "data", "weaponry");

const LETHAL = "lethal";
const LESS_LETHAL = "less_lethal";
const NON_LETHAL = "non_lethal";

const LETHALITY_TAGS = new Set([LETHAL, LESS_LETHAL, NON_LETHAL]);

// Categories that are always lethal
const lethalCategories = new Set([
  "pistol", "revolver", "shotgun", "assault_rifle", "smg", "sniper", "dmr",
  "support", "exotic", "firearm", "rifle", "heavy", "anti-materiel rifle",
  "grenade launcher", "crossbow", "bow", "improvised firearm",
  "vehicle-mounted weapon", "drone-mounted"
]);

// Categories that strongly suggest less-lethal
const lessLethalCategories = new Set([
  "less-lethal", "stun weapon"
]);

// Categories that strongly suggest non-lethal
const nonLethalCategories = new Set([
  "signal device", "personal alarm"
]);

// Keyword patterns for overrides
// Non-lethal keywords: only match on name + category (not description/tags, too many false positives)
const nonLethalNameKeywords = /\b(pepper spray|chemical spray|personal alarm|smoke (device|grenade|bomb)|flash (device|bang)|tracking device|marking device|signal flare|surveillance (device|equipment|drone)|documentation (device|equipment))\b/i;
// Less-lethal keywords checked against name and description
// Keywords checked against name + description
const lessLethalKeywords = /\b(stun|taser|shock|rubber bullet|bean ?bag|net launcher|bola|tear gas|incapacitat|compliance|electroshock|riot control|immobiliz|entangle|binder|adhesive|restraint|crowd[\s-]control)\b/i;
// These are only matched in the weapon name (too ambiguous in descriptions)
const lessLethalNameOnly = /\b(less[\s-]lethal|non[\s-]lethal)\b/i;
const lethalKeywords = /\b(lethal|kill|deadly|armor[\s-]piercing|explosive|fragmentat|incendiary|plasma|railgun|directed energy|anti[\s-]materiel|hollow[\s-]?point)\b/i;

function isLessLethal(name, nameDesc) {
  return lessLethalKeywords.test(nameDesc) || lessLethalNameOnly.test(name);
}

function determineLethality(weapon) {
  const cat = (weapon.category || "").toLowerCase();
  const name = (weapon.name || "").toLowerCase();
  const desc = (weapon.description || "").toLowerCase();
  // Use name + description for keyword checks (not tags — too many false positives)
  const nameDesc = `${name} ${desc}`;

  // Non-lethal categories
  if (nonLethalCategories.has(cat)) {
    if (isLessLethal(name, nameDesc)) return LESS_LETHAL;
    return NON_LETHAL;
  }

  // Less-lethal categories
  if (lessLethalCategories.has(cat)) {
    return LESS_LETHAL;
  }

  // Spray weapon category
  if (cat === "spray weapon") {
    if (isLessLethal(name, nameDesc)) return LESS_LETHAL;
    return NON_LETHAL;
  }

  // Sonic category
  if (cat === "sonic") {
    if (lethalKeywords.test(nameDesc)) return LETHAL;
    return LESS_LETHAL;
  }

  // Chemical category — could go either way
  if (cat === "chemical") {
    if (nonLethalNameKeywords.test(nameDesc)) return NON_LETHAL;
    if (isLessLethal(name, nameDesc)) return LESS_LETHAL;
    return LETHAL;
  }

  // Explicitly lethal categories
  if (lethalCategories.has(cat)) {
    // Check for less-lethal variants (e.g., rubber bullet pistol)
    if (nonLethalNameKeywords.test(name)) return NON_LETHAL;
    if (isLessLethal(name, nameDesc)) return LESS_LETHAL;
    return LETHAL;
  }

  // Melee: default lethal unless stun/shock/compliance
  if (cat === "melee") {
    if (nonLethalNameKeywords.test(name)) return NON_LETHAL;
    if (isLessLethal(name, nameDesc)) return LESS_LETHAL;
    return LETHAL;
  }

  // Concealed weapon
  if (cat === "concealed weapon") {
    if (nonLethalNameKeywords.test(name)) return NON_LETHAL;
    if (isLessLethal(name, nameDesc)) return LESS_LETHAL;
    return LETHAL;
  }

  // Explosive: usually lethal
  if (cat === "explosive") {
    if (nonLethalNameKeywords.test(name)) return NON_LETHAL;
    if (isLessLethal(name, nameDesc)) return LESS_LETHAL;
    return LETHAL;
  }

  // Energy, electromagnetic, cyber, cyber-integrated, drone, improvised, underwater
  // Check keywords, default lethal
  if (nonLethalNameKeywords.test(name)) return NON_LETHAL;
  if (isLessLethal(name, nameDesc)) return LESS_LETHAL;
  return LETHAL;
}

function main() {
  const files = fs.readdirSync(weaponryDir).filter(f => f.endsWith(".json"));
  let counts = { lethal: 0, less_lethal: 0, non_lethal: 0, skipped: 0, errors: 0 };

  for (const file of files) {
    const filePath = path.join(weaponryDir, file);
    try {
      const raw = fs.readFileSync(filePath, "utf8");
      const weapon = JSON.parse(raw);

      if (!Array.isArray(weapon.tags)) {
        weapon.tags = [];
      }

      // Check if already tagged
      const existing = weapon.tags.find(t => LETHALITY_TAGS.has(t));
      if (existing) {
        counts.skipped++;
        continue;
      }

      const lethality = determineLethality(weapon);
      weapon.tags.push(lethality);

      fs.writeFileSync(filePath, JSON.stringify(weapon, null, 2) + "\n", "utf8");
      counts[lethality]++;
    } catch (err) {
      console.error(`Error processing ${file}: ${err.message}`);
      counts.errors++;
    }
  }

  console.log("\n=== Weapon Lethality Tagging Complete ===");
  console.log(`Lethal:      ${counts.lethal}`);
  console.log(`Less Lethal: ${counts.less_lethal}`);
  console.log(`Non Lethal:  ${counts.non_lethal}`);
  console.log(`Skipped:     ${counts.skipped}`);
  console.log(`Errors:      ${counts.errors}`);
  console.log(`Total:       ${files.length}`);
}

main();
