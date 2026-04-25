// One-shot cleanup: collapse intra-paragraph hard-wraps in all canon JSON.
//
// LLM-generated descriptions were stored verbatim with ~75-char hard wraps,
// which the encyclopedia renderer (EntryViewer.razor) was turning into <br>
// tags. This pass normalizes every string field so:
//   • single \n  → ' '   (was a soft wrap inside a paragraph)
//   • \n\n      → \n\n  (preserved — true paragraph break)
//   • \n\n\n+   → \n\n  (excess blank lines collapsed to one paragraph break)
//
// Run from v3/:  node cleanup_hardwraps.js
// Idempotent — safe to re-run.

const fs = require('fs');
const path = require('path');

const ROOT = path.resolve(__dirname, '..', 'engine', 'data');

let filesScanned = 0;
let filesChanged = 0;
let stringsChanged = 0;
const failures = [];

// Collapse a single string. Lookbehind/lookahead preserve real paragraph
// breaks (\n\n), but a lone \n between non-newline chars becomes a space.
// Trailing/leading whitespace around the replaced \n collapses to one space.
function normalize(s) {
  if (typeof s !== 'string') return s;
  if (s.indexOf('\n') === -1) return s;

  // Step 1: collapse any run of 3+ newlines down to exactly 2.
  let out = s.replace(/\n{3,}/g, '\n\n');

  // Step 2: replace each lone \n (not part of \n\n) with a single space,
  // absorbing any spaces immediately around it.
  out = out.replace(/(?<!\n) *\n *(?!\n)/g, ' ');

  // Step 3: tidy stray double-spaces created by the previous step.
  out = out.replace(/  +/g, ' ');

  return out;
}

// Recursively walk a JSON value, returning the (possibly mutated) value plus
// a count of how many strings were actually changed.
function walk(node, ctx) {
  if (Array.isArray(node)) {
    for (let i = 0; i < node.length; i++) {
      node[i] = walk(node[i], ctx);
    }
    return node;
  }
  if (node && typeof node === 'object') {
    for (const k of Object.keys(node)) {
      node[k] = walk(node[k], ctx);
    }
    return node;
  }
  if (typeof node === 'string') {
    const next = normalize(node);
    if (next !== node) ctx.changed++;
    return next;
  }
  return node;
}

function processFile(filePath) {
  filesScanned++;
  let raw;
  try {
    raw = fs.readFileSync(filePath, 'utf8');
  } catch (e) {
    failures.push({ file: filePath, reason: 'read: ' + e.message });
    return;
  }

  let json;
  try {
    json = JSON.parse(raw);
  } catch (e) {
    failures.push({ file: filePath, reason: 'parse: ' + e.message });
    return;
  }

  const ctx = { changed: 0 };
  walk(json, ctx);
  if (ctx.changed === 0) return;

  // Preserve trailing newline if original had one (consistent with most JSON tooling).
  const hadTrailingNewline = raw.endsWith('\n');
  const out = JSON.stringify(json, null, 2) + (hadTrailingNewline ? '\n' : '');

  try {
    fs.writeFileSync(filePath, out, 'utf8');
    filesChanged++;
    stringsChanged += ctx.changed;
  } catch (e) {
    failures.push({ file: filePath, reason: 'write: ' + e.message });
  }
}

function walkDir(dir) {
  const entries = fs.readdirSync(dir, { withFileTypes: true });
  for (const ent of entries) {
    const full = path.join(dir, ent.name);
    if (ent.isDirectory()) walkDir(full);
    else if (ent.isFile() && ent.name.endsWith('.json')) processFile(full);
  }
}

console.log('Cleaning hard-wraps under', ROOT);
const t0 = Date.now();
walkDir(ROOT);
const dt = ((Date.now() - t0) / 1000).toFixed(1);

console.log(`\nScanned : ${filesScanned} JSON files`);
console.log(`Changed : ${filesChanged} files`);
console.log(`Strings : ${stringsChanged} fields normalized`);
console.log(`Time    : ${dt}s`);

if (failures.length) {
  console.log(`\nFailures (${failures.length}):`);
  for (const f of failures.slice(0, 20)) {
    console.log('  ' + f.file + ' — ' + f.reason);
  }
  if (failures.length > 20) console.log('  ...and ' + (failures.length - 20) + ' more');
}
