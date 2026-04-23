#!/usr/bin/env node
// Audit lingering old-name references after phase1/2/3/4.
// For each people file, count full-name occurrences of each renamed character.
// Subtract the expected 1 from the owner's own file (their intentional self-alias).
// Anything else is a legitimate cross-reference leak.

const fs = require('fs');
const path = require('path');

const REPO_ROOT = path.resolve(__dirname, '..', '..');
const MAP = JSON.parse(fs.readFileSync(path.join(__dirname, 'rename_map.json'), 'utf8'));

function walk(dir, acc = []) {
  for (const e of fs.readdirSync(dir, { withFileTypes: true })) {
    const full = path.join(dir, e.name);
    if (e.isDirectory()) {
      if (['archives', 'bin', 'obj', 'logs', 'graph', 'exports',
           'node_modules', '.git', 'LLMVoting', '.vs'].includes(e.name)) continue;
      walk(full, acc);
    } else {
      if (!/\.(json|razor|cs|md|txt)$/.test(e.name)) continue;
      if (e.name === 'rename_map.json' || e.name === 'rename_report.csv') continue;
      acc.push(full);
    }
  }
  return acc;
}

function escapeRe(s) { return s.replace(/[.*+?^${}()|[\]\\]/g, '\\$&'); }

// For each old name, use word-boundary regex. For multi-word names this still
// matches whole-phrase; for short single-word names it prevents substring false
// positives like "Arden" matching inside "Arden Makwa-Guerrero".
const rxByOld = new Map();
for (const r of MAP) {
  rxByOld.set(r.oldName, new RegExp(`\\b${escapeRe(r.oldName)}\\b`, 'g'));
}

const files = walk(REPO_ROOT);
let expectedSelfAliasHits = 0;
let crossRefLeaks = 0;
const problems = []; // { file, old, count, why }

for (const f of files) {
  let text;
  try { text = fs.readFileSync(f, 'utf8'); } catch { continue; }

  // If this is a people-canon file, identify its id AND parse aliases + name.
  let ownerId = null;
  let ownerAliases = new Set();
  let ownerName = '';
  const isPeople = f.includes(path.sep + 'people' + path.sep);
  if (isPeople) {
    try {
      const d = JSON.parse(text);
      ownerId = d.id;
      ownerName = d.name || '';
      if (Array.isArray(d.aliases)) ownerAliases = new Set(d.aliases);
    } catch {}
  }

  for (const r of MAP) {
    const rx = rxByOld.get(r.oldName);
    const matches = text.match(rx);
    if (!matches) continue;
    let count = matches.length;

    // If this is the owner's file, subtract one alias entry (they keep their old
    // full name in aliases[] intentionally).
    if (isPeople && ownerId === r.id && ownerAliases.has(r.oldName)) {
      count -= 1;
      expectedSelfAliasHits += 1;
    }

    if (count <= 0) continue;

    // If this file is a DIFFERENT character's people file, check whether the
    // matched old name is one of that character's own aliases (a preserved
    // historical link). Those aren't "leaks" — they're the same kind of
    // intentional breadcrumb.
    if (isPeople && ownerAliases.has(r.oldName)) continue;

    // Also filter substring-within-compound false positives. If the owner has
    // an alias like "Slate Björnsdóttir-Kwon" and we're matching "Slate
    // Björnsdóttir", the \b regex matches at the hyphen. Not a real leak.
    if (isPeople) {
      const prefixMatch = [...ownerAliases].some(a =>
        a !== r.oldName && a.startsWith(r.oldName + '-'));
      if (prefixMatch) continue;
    }

    // Filter out matches that are part of the owner's current `name` field.
    // For a keeper like "Briar Seo-Suwannapoom", the regex \bBriar\b matches
    // their own name; that's not a leak.
    if (isPeople && ownerName) {
      const nameRx = new RegExp(`\\b${escapeRe(r.oldName)}\\b`, 'g');
      const inOwnerName = (ownerName.match(nameRx) || []).length;
      if (inOwnerName > 0) {
        count -= inOwnerName;
        if (count <= 0) continue;
      }
    }

    crossRefLeaks += count;
    problems.push({
      file: path.relative(REPO_ROOT, f).replace(/\\/g, '/'),
      old: r.oldName,
      count,
      why: isPeople ? 'cross-character people-file leak' : 'cross-file leak',
    });
  }
}

console.log(`Files scanned:          ${files.length}`);
console.log(`Expected self-aliases:  ${expectedSelfAliasHits} (one per renamed character's own aliases[])`);
console.log(`Cross-reference leaks:  ${crossRefLeaks} (bugs — stale references to renamed characters)`);
console.log(`Problem files:          ${problems.length}`);

if (problems.length > 0) {
  console.log('\nFirst 20 problems:');
  for (const p of problems.slice(0, 20)) {
    console.log(`  [${p.count}] ${p.old}  in  ${p.file}  — ${p.why}`);
  }

  // Group by file type
  const byDir = {};
  for (const p of problems) {
    const prefix = p.file.split('/').slice(0, 3).join('/');
    byDir[prefix] = (byDir[prefix] || 0) + p.count;
  }
  console.log('\nBy directory:');
  Object.entries(byDir)
    .sort((a, b) => b[1] - a[1])
    .slice(0, 15)
    .forEach(([k, v]) => console.log(`  ${String(v).padStart(6)}  ${k}`));
}
