const fs = require('fs');
const path = require('path');

const CHAR_DIR = path.join(__dirname, '..', 'engine', 'data', 'characters');
const WEAPON_DIR = path.join(__dirname, '..', 'engine', 'data', 'weaponry');

// ═══════════════════════════════════════════════
// Load all weapons from the weaponry directory
// ═══════════════════════════════════════════════

const weaponFiles = fs.readdirSync(WEAPON_DIR).filter(f => f.endsWith('.json'));
const allWeapons = [];
for (const f of weaponFiles) {
  try {
    const w = JSON.parse(fs.readFileSync(path.join(WEAPON_DIR, f), 'utf8'));
    allWeapons.push({
      name: w.name,
      category: w.category || 'unknown',
      tier: w.tier_availability || '',
      manufacturer: w.manufacturer || ''
    });
  } catch (e) {
    // skip malformed files
  }
}

// ═══════════════════════════════════════════════
// Categorize weapons by tier suitability
// ═══════════════════════════════════════════════

function tierFromAvailability(tierStr) {
  if (!tierStr) return 3;
  const m = tierStr.match(/Tier\s*(\d)/i);
  return m ? parseInt(m[1]) : 3;
}

// Weapons grouped by accessibility tier
const weaponsByTier = { 1: [], 2: [], 3: [], 4: [], 5: [] };
for (const w of allWeapons) {
  const t = tierFromAvailability(w.tier);
  for (let i = t; i <= 5; i++) {
    weaponsByTier[i].push(w);
  }
}

// Filter by category for easy lookup
function filterByCat(weapons, categories) {
  return weapons.filter(w => categories.includes(w.category));
}

// Specific weapon pools by tier and type
const tier1Pistols = filterByCat(weaponsByTier[1], ['pistol', 'revolver']);
const tier1Shotguns = filterByCat(weaponsByTier[1], ['shotgun']);
const tier1Improvised = filterByCat(weaponsByTier[1], ['improvised']);
const tier1Melee = filterByCat(weaponsByTier[1], ['melee']);
const tier1Smgs = filterByCat(weaponsByTier[1], ['smg', 'SMG']);

const tier2Pistols = filterByCat(weaponsByTier[2], ['pistol', 'revolver']);
const tier2Shotguns = filterByCat(weaponsByTier[2], ['shotgun']);
const tier2Smgs = filterByCat(weaponsByTier[2], ['smg', 'SMG']);

const tier3Pistols = filterByCat(weaponsByTier[3], ['pistol', 'revolver']);
const tier3Rifles = filterByCat(weaponsByTier[3], ['assault_rifle', 'rifle', 'dmr']);
const tier3Smgs = filterByCat(weaponsByTier[3], ['smg', 'SMG']);
const tier3Shotguns = filterByCat(weaponsByTier[3], ['shotgun']);

const tier4Pistols = filterByCat(weaponsByTier[4], ['pistol', 'revolver']);
const tier4Rifles = filterByCat(weaponsByTier[4], ['assault_rifle', 'rifle', 'dmr']);
const tier4Smgs = filterByCat(weaponsByTier[4], ['smg', 'SMG']);
const tier4Sniper = filterByCat(weaponsByTier[4], ['sniper']);

const tier5Pistols = filterByCat(weaponsByTier[5], ['pistol', 'revolver']);
const tier5Rifles = filterByCat(weaponsByTier[5], ['assault_rifle', 'rifle', 'dmr']);

// ═══════════════════════════════════════════════
// Helper functions
// ═══════════════════════════════════════════════

function pick(arr) {
  if (!arr || arr.length === 0) return null;
  return arr[Math.floor(Math.random() * arr.length)];
}

function pickN(arr, n) {
  if (!arr || arr.length === 0) return [];
  const shuffled = [...arr].sort(() => Math.random() - 0.5);
  return shuffled.slice(0, Math.min(n, shuffled.length));
}

function inferTier(char) {
  // Check explicit tier field
  if (char.tier) {
    const m = String(char.tier).match(/(\d)/);
    if (m) return parseInt(m[1]);
  }

  const loc = (char.location || '').toLowerCase();
  const role = (char.role || '').toLowerCase();
  const desc = (char.description || '').toLowerCase();
  const affil = (char.affiliation || '').toLowerCase();

  // Location-based inference
  if (loc.includes('gulch') || loc.includes('bilge')) return 1;
  if (loc.includes('shelf') || loc.includes('tier 1') || loc.includes('tier-1')) return 1;
  if (loc.includes('tier 2') || loc.includes('tier-2')) return 2;
  if (loc.includes('mids') || loc.includes('tier 3') || loc.includes('tier-3')) return 3;
  if (loc.includes('tier 4') || loc.includes('tier-4')) return 4;
  if (loc.includes('crown') || loc.includes('tier 5') || loc.includes('tier-5') || loc.includes('penthouse') || loc.includes('spire')) return 5;

  // Role-based inference
  if (role.includes('executive') || role.includes('ceo') || role.includes('director') || role.includes('board')) return 5;
  if (role.includes('senior') || role.includes('manager') || role.includes('lawyer') || role.includes('surgeon')) return 4;
  if (role.includes('engineer') || role.includes('analyst') || role.includes('detective') || role.includes('technician')) return 3;
  if (role.includes('worker') || role.includes('clerk') || role.includes('driver') || role.includes('courier')) return 2;
  if (role.includes('scavenger') || role.includes('homeless') || role.includes('beggar') || role.includes('squatter')) return 1;

  // Description-based inference
  if (desc.includes('gulch') || desc.includes('gutter') || desc.includes('bottom tier')) return 1;
  if (desc.includes('shelf district') || desc.includes('shelf block')) return 1;
  if (desc.includes('crown district') || desc.includes('penthouse')) return 5;
  if (desc.includes('upper tier') || desc.includes('high-rise')) return 4;

  // Default to tier 2 (most common population band)
  return 2;
}

function inferRole(char) {
  const role = (char.role || '').toLowerCase();
  const desc = (char.description || '').toLowerCase();
  const tags = (char.stats?.tags || []).map(t => t.toLowerCase());
  const allText = role + ' ' + desc + ' ' + tags.join(' ');

  if (/military|soldier|veteran|combat|mercenary|operator|ex-military|marine|infantry/.test(allText)) return 'military';
  if (/security|guard|bodyguard|enforcer|bouncer|protection|tactical/.test(allText)) return 'security';
  if (/police|cop|officer|detective|constable|patrol|law enforcement/.test(allText)) return 'police';
  if (/criminal|gang|smuggler|dealer|thief|con|grifter|racketeer|fixer|runner|hitter|assassin|killer|hitman/.test(allText)) return 'criminal';
  if (/hunter|bounty|tracker|investigator|private/.test(allText)) return 'investigator';
  if (/mechanic|tech|engineer|hacker|rigger/.test(allText)) return 'technical';
  if (/doctor|medic|nurse|surgeon|clinic/.test(allText)) return 'medical';
  if (/child|elderly|retired|artist|musician|teacher|professor|student|librarian|archivist/.test(allText)) return 'civilian_unarmed';
  if (/bartender|chef|cook|vendor|shopkeep|clerk|service|janitor|custodian/.test(allText)) return 'civilian_service';

  return 'civilian';
}

function shouldBeArmed(char) {
  const roleType = inferRole(char);
  const species = (char.species || '').toLowerCase();

  // Some roles are typically unarmed
  if (roleType === 'civilian_unarmed') return false;
  if (roleType === 'medical') return Math.random() < 0.2; // some medics carry

  // Children are never armed
  if (char.age && char.age < 16) return false;

  // Very elderly less likely
  if (char.age && char.age > 80) return Math.random() < 0.15;

  // Combat/security roles always armed
  if (['military', 'security', 'police', 'criminal', 'investigator'].includes(roleType)) return true;

  // Service workers and regular civilians — depends on tier and context
  const tier = inferTier(char);
  if (tier <= 2) return Math.random() < 0.55; // Shelf/Gulch — many people carry something
  if (tier === 3) return Math.random() < 0.35; // Mids — some people carry
  if (tier >= 4) return Math.random() < 0.2; // Upper tiers — fewer carry, security handles it

  return Math.random() < 0.3;
}

function assignWeapons(char) {
  const tier = inferTier(char);
  const roleType = inferRole(char);

  let carried = [];
  let registered = [];

  switch (roleType) {
    case 'military': {
      // Military gets rifles + sidearms, properly registered
      const rifle = pick(tier >= 3 ? tier4Rifles : tier3Rifles);
      const sidearm = pick(tier >= 3 ? tier4Pistols : tier3Pistols);
      if (rifle) { carried.push(rifle.name); registered.push(rifle.name); }
      if (sidearm) { carried.push(sidearm.name); registered.push(sidearm.name); }
      // Some carry melee as backup
      if (Math.random() < 0.3) {
        const melee = pick(filterByCat(weaponsByTier[Math.min(tier, 3)], ['melee']));
        if (melee) carried.push(melee.name);
      }
      break;
    }

    case 'security': {
      // Security — sidearm + maybe SMG, registered
      const sidearm = pick(tier >= 4 ? tier4Pistols : tier3Pistols);
      if (sidearm) { carried.push(sidearm.name); registered.push(sidearm.name); }
      if (Math.random() < 0.5) {
        const smg = pick(tier >= 4 ? tier4Smgs : tier3Smgs);
        if (smg) { carried.push(smg.name); registered.push(smg.name); }
      }
      break;
    }

    case 'police': {
      // Police — department sidearm + maybe personal weapon
      const deptWeapon = pick(tier3Pistols);
      if (deptWeapon) { carried.push(deptWeapon.name); registered.push(deptWeapon.name); }
      if (Math.random() < 0.3) {
        const personal = pick(tier2Pistols);
        if (personal) { carried.push(personal.name); registered.push(personal.name); }
      }
      break;
    }

    case 'criminal': {
      if (tier <= 2) {
        // Low-tier criminals: cheap/improvised, unregistered
        if (Math.random() < 0.5) {
          const gun = pick(tier1Pistols);
          if (gun) carried.push(gun.name); // NOT registered
        }
        if (Math.random() < 0.4) {
          const imp = pick(tier1Improvised);
          if (imp) carried.push(imp.name);
        }
        if (Math.random() < 0.3) {
          const melee = pick(tier1Melee);
          if (melee) carried.push(melee.name);
        }
        if (carried.length === 0) {
          const fallback = pick(tier1Pistols);
          if (fallback) carried.push(fallback.name);
        }
      } else {
        // Higher-tier criminals: better weapons, still unregistered
        const primary = pick(tier >= 4 ? tier4Pistols : tier3Pistols);
        if (primary) carried.push(primary.name);
        if (Math.random() < 0.4) {
          const secondary = pick(tier >= 4 ? tier4Smgs : tier3Smgs);
          if (secondary) carried.push(secondary.name);
        }
        if (Math.random() < 0.3) {
          const melee = pick(filterByCat(weaponsByTier[tier], ['melee']));
          if (melee) carried.push(melee.name);
        }
        // Criminals don't register
      }
      break;
    }

    case 'investigator': {
      // PIs and bounty hunters — sidearm, maybe registered
      const sidearm = pick(tier >= 3 ? tier3Pistols : tier2Pistols);
      if (sidearm) {
        carried.push(sidearm.name);
        if (Math.random() < 0.6) registered.push(sidearm.name);
      }
      if (Math.random() < 0.3) {
        const backup = pick(tier >= 3 ? tier3Pistols : tier2Pistols);
        if (backup) carried.push(backup.name);
      }
      break;
    }

    case 'technical': {
      // Techs sometimes carry for protection
      if (tier <= 2) {
        const gun = pick(tier1Pistols);
        if (gun) {
          carried.push(gun.name);
          if (Math.random() < 0.3) registered.push(gun.name);
        }
      } else {
        const gun = pick(tier3Pistols);
        if (gun) {
          carried.push(gun.name);
          if (Math.random() < 0.6) registered.push(gun.name);
        }
      }
      break;
    }

    case 'civilian_service': {
      // Bartenders, vendors, etc. — basic protection
      if (tier <= 2) {
        const r = Math.random();
        if (r < 0.4) {
          const gun = pick(tier1Pistols);
          if (gun) carried.push(gun.name);
        } else if (r < 0.6) {
          const shotgun = pick(tier1Shotguns);
          if (shotgun) carried.push(shotgun.name);
        } else {
          const imp = pick(tier1Improvised);
          if (imp) carried.push(imp.name);
        }
        // Low-tier service workers rarely register
        if (Math.random() < 0.15 && carried.length > 0) registered.push(carried[0]);
      } else {
        const gun = pick(tier2Pistols);
        if (gun) {
          carried.push(gun.name);
          if (Math.random() < 0.5) registered.push(gun.name);
        }
      }
      break;
    }

    default: {
      // Generic civilians
      if (tier <= 1) {
        // Shelf/Gulch civilians — cheap pistols, old shotguns, improvised
        const r = Math.random();
        if (r < 0.35) {
          const gun = pick(tier1Pistols);
          if (gun) carried.push(gun.name);
        } else if (r < 0.55) {
          const shotgun = pick(tier1Shotguns);
          if (shotgun) carried.push(shotgun.name);
        } else if (r < 0.75) {
          const imp = pick(tier1Improvised);
          if (imp) carried.push(imp.name);
        } else {
          const melee = pick(tier1Melee);
          if (melee) carried.push(melee.name);
        }
        // Tier 1 almost never registers
        if (Math.random() < 0.05 && carried.length > 0) registered.push(carried[0]);
      } else if (tier === 2) {
        const gun = pick(tier2Pistols);
        if (gun) {
          carried.push(gun.name);
          if (Math.random() < 0.3) registered.push(gun.name);
        }
      } else if (tier === 3) {
        const gun = pick(tier3Pistols);
        if (gun) {
          carried.push(gun.name);
          if (Math.random() < 0.6) registered.push(gun.name);
        }
      } else {
        // Tier 4-5 civilians who carry — high-end, properly registered
        const gun = pick(tier4Pistols);
        if (gun) {
          carried.push(gun.name);
          registered.push(gun.name);
        }
      }
      break;
    }
  }

  return { carried, registered };
}

// ═══════════════════════════════════════════════
// Process all character files
// ═══════════════════════════════════════════════

const charFiles = fs.readdirSync(CHAR_DIR).filter(f => f.endsWith('.json'));
let updated = 0;
let skippedExisting = 0;
let skippedUnarmed = 0;
let errors = 0;

for (const f of charFiles) {
  const filePath = path.join(CHAR_DIR, f);
  try {
    const char = JSON.parse(fs.readFileSync(filePath, 'utf8'));

    // Skip if already has weapon fields
    if (char.registered_firearms || char.carried_weapons) {
      skippedExisting++;
      continue;
    }

    // Decide if this character should be armed
    if (!shouldBeArmed(char)) {
      skippedUnarmed++;
      continue;
    }

    // Assign weapons
    const { carried, registered } = assignWeapons(char);

    if (carried.length === 0) {
      skippedUnarmed++;
      continue;
    }

    // Add fields to character
    char.carried_weapons = carried;
    char.registered_firearms = registered;

    // Write back
    fs.writeFileSync(filePath, JSON.stringify(char, null, 2), 'utf8');
    updated++;

    if (updated <= 10) {
      const tier = inferTier(char);
      const roleType = inferRole(char);
      console.log(`  ${char.name} (Tier ${tier}, ${roleType}): carried=[${carried.join(', ')}] registered=[${registered.join(', ')}]`);
    }
  } catch (e) {
    errors++;
    console.error(`ERROR processing ${f}: ${e.message}`);
  }
}

console.log(`\n═══════════════════════════════════════`);
console.log(`Characters processed: ${charFiles.length}`);
console.log(`Updated with weapons: ${updated}`);
console.log(`Skipped (already armed): ${skippedExisting}`);
console.log(`Skipped (unarmed role/age): ${skippedUnarmed}`);
console.log(`Errors: ${errors}`);
console.log(`═══════════════════════════════════════`);
