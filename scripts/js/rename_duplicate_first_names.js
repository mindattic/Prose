#!/usr/bin/env node
// Rename every character whose first name collides with another character,
// so that every character in engine/data/people/ has a unique first name.
//
// Rules (agreed with user 2026-04-22):
//   - Keeper per collision group = highest `rating`, tiebreak = smallest `id` lex.
//   - Renamed character's canonical `name` gets a new first name from scripts/js/name_pool.json.
//   - The old full name is preserved in `aliases[]` so cross-references still resolve.
//   - Forbidden new first names: Sarah, Lee, Bekka, Karen.
//   - Pool names currently used by any canon character are excluded to prevent new collisions.
//
// Phases:
//   phase1 — rename people files; emit rename_map.json
//   phase2 — read rename_map.json; update full-name references in every other JSON under engine/data and ghostwriter
//
// Usage:
//   node scripts/js/rename_duplicate_first_names.js --phase1 [--dry-run]
//   node scripts/js/rename_duplicate_first_names.js --phase2 [--dry-run]

const fs   = require('fs');
const path = require('path');

const REPO_ROOT    = path.resolve(__dirname, '..', '..');
const PEOPLE_DIR   = path.join(REPO_ROOT, 'engine', 'data', 'people');
const POOL_FILE    = path.join(REPO_ROOT, 'engine', 'data', 'name_pool.json');
const MAP_FILE     = path.join(__dirname, 'rename_map.json');
const CSV_FILE     = path.join(__dirname, 'rename_report.csv');

const FORBIDDEN    = new Set(['sarah', 'lee', 'bekka', 'karen']);

// IDs pinned as keepers regardless of rating — characters we've manually curated this session
// and do not want to lose their canonical first name to a higher-rated NPC.
const KEEPER_OVERRIDES = new Set([
  '019d6143a64a75958d2ff079da23cb91', // Kira Hong
]);

function args() {
  const a = process.argv.slice(2);
  return {
    phase1:  a.includes('--phase1'),
    phase2:  a.includes('--phase2'),
    dryRun:  a.includes('--dry-run'),
    sample:  a.includes('--sample'),
  };
}

function listPeople() {
  return fs.readdirSync(PEOPLE_DIR)
    .filter(f => f.endsWith('.json'))
    .map(f => path.join(PEOPLE_DIR, f));
}

function firstNameOf(fullName) {
  return (fullName || '').trim().split(/\s+/)[0] || '';
}

function loadChar(file) {
  try { return { file, data: JSON.parse(fs.readFileSync(file, 'utf8')) }; }
  catch (e) { return null; }
}

function saveChar(file, data) {
  fs.writeFileSync(file, JSON.stringify(data, null, 2) + '\n');
}

// ── Phase 1 ───────────────────────────────────────────────────

function phase1(dryRun) {
  const pool = JSON.parse(fs.readFileSync(POOL_FILE, 'utf8'));
  const poolUnique = [...new Set(pool)];

  const chars = listPeople().map(loadChar).filter(Boolean);
  console.log(`[phase1] loaded ${chars.length} character files`);

  // Current in-use first names (case-insensitive)
  const used = new Set();
  for (const c of chars) used.add(firstNameOf(c.data.name).toLowerCase());

  // Build candidate pool: pool minus used minus forbidden
  const candidates = poolUnique.filter(n => {
    const k = n.toLowerCase();
    return !used.has(k) && !FORBIDDEN.has(k);
  });
  console.log(`[phase1] pool=${poolUnique.length} used=${used.size} forbidden=${FORBIDDEN.size} candidates=${candidates.length}`);

  // Group by first name (case-insensitive) — only duplicates
  const groups = new Map();
  for (const c of chars) {
    const fn = firstNameOf(c.data.name).toLowerCase();
    if (!fn) continue;
    if (!groups.has(fn)) groups.set(fn, []);
    groups.get(fn).push(c);
  }
  const dupGroups = [...groups.entries()].filter(([, list]) => list.length > 1);
  const totalToRename = dupGroups.reduce((s, [, l]) => s + (l.length - 1), 0);
  console.log(`[phase1] ${dupGroups.length} collision groups; ${totalToRename} characters to rename`);

  if (candidates.length < totalToRename) {
    throw new Error(`not enough candidate names: need ${totalToRename}, have ${candidates.length}`);
  }

  // Keeper: override-pinned id wins; then highest rating; tiebreak smallest id lex
  function pickKeeper(list) {
    const pinned = list.find(c => KEEPER_OVERRIDES.has(c.data.id));
    if (pinned) return pinned;
    return [...list].sort((a, b) => {
      const ra = Number(a.data.rating ?? 0);
      const rb = Number(b.data.rating ?? 0);
      if (rb !== ra) return rb - ra;
      return (a.data.id || '').localeCompare(b.data.id || '');
    })[0];
  }

  // Deterministic candidate order (alphabetical) so runs are repeatable
  const nextName = (() => {
    const queue = [...candidates].sort();
    const taken = new Set();
    return () => {
      while (queue.length) {
        const n = queue.shift();
        if (taken.has(n.toLowerCase())) continue;
        taken.add(n.toLowerCase());
        return n;
      }
      return null;
    };
  })();

  const renameMap = [];

  for (const [, list] of dupGroups) {
    const keeper = pickKeeper(list);
    for (const c of list) {
      if (c === keeper) continue;

      const oldName     = c.data.name;
      const oldFirst    = firstNameOf(oldName);
      const surname     = oldName.slice(oldFirst.length).trimStart();
      const newFirst    = nextName();
      if (!newFirst) throw new Error('pool exhausted');
      const newName     = surname ? `${newFirst} ${surname}` : newFirst;

      renameMap.push({
        id:      c.data.id,
        file:    path.relative(REPO_ROOT, c.file).replace(/\\/g, '/'),
        oldName,
        newName,
      });

      if (!dryRun) {
        c.data.name = newName;
        c.data.aliases = Array.isArray(c.data.aliases) ? c.data.aliases : [];
        if (!c.data.aliases.includes(oldName))  c.data.aliases.unshift(oldName);
        if (!c.data.aliases.includes(oldFirst)) c.data.aliases.push(oldFirst);
        saveChar(c.file, c.data);
      }
    }
  }

  if (!dryRun) {
    fs.writeFileSync(MAP_FILE, JSON.stringify(renameMap, null, 2));
    const csv = ['id,file,old_name,new_name', ...renameMap.map(r =>
      [r.id, r.file, JSON.stringify(r.oldName), JSON.stringify(r.newName)].join(',')
    )].join('\n');
    fs.writeFileSync(CSV_FILE, csv);
    console.log(`[phase1] wrote ${renameMap.length} renames`);
    console.log(`[phase1] map: ${path.relative(REPO_ROOT, MAP_FILE)}`);
    console.log(`[phase1] csv: ${path.relative(REPO_ROOT, CSV_FILE)}`);
  } else {
    console.log(`[phase1] DRY RUN — ${renameMap.length} would be renamed`);
    console.log('\nFirst 30 proposed renames:');
    for (const r of renameMap.slice(0, 30))
      console.log(`  ${r.oldName}  →  ${r.newName}`);
  }
}

// ── Phase 2 ───────────────────────────────────────────────────

function walk(dir, acc = []) {
  for (const entry of fs.readdirSync(dir, { withFileTypes: true })) {
    const full = path.join(dir, entry.name);
    if (entry.isDirectory()) {
      // Skip graph cache (regenerated) and archive directories
      if (entry.name === 'graph' || entry.name === 'archives' || entry.name === 'exports' ||
          entry.name === 'logs' || entry.name === 'node_modules' || entry.name === 'bin' ||
          entry.name === 'obj') continue;
      walk(full, acc);
    } else if (entry.name.endsWith('.json')) {
      acc.push(full);
    }
  }
  return acc;
}

function phase2(dryRun) {
  if (!fs.existsSync(MAP_FILE)) {
    throw new Error(`missing ${MAP_FILE} — run --phase1 first`);
  }
  const map = JSON.parse(fs.readFileSync(MAP_FILE, 'utf8'));
  console.log(`[phase2] loaded ${map.length} renames`);

  // Build sorted find/replace pairs — longest oldName first so "Zara Okonkwo"
  // is replaced before any bare "Zara" sweep would match.
  const pairs = map
    .slice()
    .sort((a, b) => b.oldName.length - a.oldName.length);

  const scanDirs = [
    path.join(REPO_ROOT, 'engine', 'data'),
    path.join(REPO_ROOT, 'ghostwriter'),
  ].filter(d => fs.existsSync(d));

  const files = scanDirs.flatMap(d => walk(d));
  // Exclude the people canon files themselves (already updated in phase1)
  const targets = files.filter(f => !f.startsWith(PEOPLE_DIR + path.sep));

  console.log(`[phase2] scanning ${targets.length} files`);

  let touched = 0, replaced = 0;

  for (const f of targets) {
    let text;
    try { text = fs.readFileSync(f, 'utf8'); } catch { continue; }
    let out = text;
    let fileReplaced = 0;

    for (const { oldName, newName } of pairs) {
      // JSON strings — plain text replace is safe because full names are
      // unambiguous tokens. No word-boundary regex (names may contain
      // diacritics, hyphens, apostrophes).
      if (out.includes(oldName)) {
        const before = out;
        // Use split/join for literal global replace without regex escaping.
        out = out.split(oldName).join(newName);
        fileReplaced += (before.length - out.length !== 0) ?
          (before.split(oldName).length - 1) : 0;
      }
    }

    if (out !== text) {
      touched++;
      replaced += fileReplaced;
      if (!dryRun) fs.writeFileSync(f, out);
    }
  }

  console.log(`[phase2] ${touched} files ${dryRun ? 'would be' : 'were'} updated; ${replaced} replacements ${dryRun ? 'proposed' : 'applied'}`);
}

// ── Main ──────────────────────────────────────────────────────

function main() {
  const a = args();
  if (!a.phase1 && !a.phase2) {
    console.error('usage: node rename_duplicate_first_names.js --phase1 [--dry-run] | --phase2 [--dry-run]');
    process.exit(1);
  }
  if (a.phase1) phase1(a.dryRun);
  if (a.phase2) phase2(a.dryRun);
}

main();
