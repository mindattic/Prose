#!/usr/bin/env node
// tools/check-contradictions.js
//
// Read a chapter, build a canon-context bundle from the entities and prior
// events the chapter touches, dispatch a Legion Quorum vote with a
// contradiction-finding rubric, and emit a JSON report of flagged
// contradictions with citations.
//
// Usage:
//   node tools/check-contradictions.js <chapter_id>
//   node tools/check-contradictions.js <chapter_id> --providers claude,openai
//   node tools/check-contradictions.js <chapter_id> --quorum twothirds
//   node tools/check-contradictions.js <chapter_id> --max-context-chars 80000
//   node tools/check-contradictions.js <chapter_id> --dry-run        # print prompt, do not vote
//
// Exit codes:
//   0  no contradictions flagged
//   1  contradictions flagged
//   2  usage error or pipeline error

const fs = require('fs');
const path = require('path');
const { spawnSync } = require('child_process');

const ROOT = path.resolve(__dirname, '..');
const ENGINE_DATA = path.join(ROOT, 'engine', 'data');
const LEGION_BIN = process.env.LEGION_BIN || 'D:/Projects/MindAttic/MindAttic.Legion/MindAttic.Legion.Cli/bin/Debug/net10.0/legion.exe';

function readJson(p) {
  return JSON.parse(fs.readFileSync(p, 'utf-8').replace(/^﻿/, ''));
}

function readJsonOrNull(p) {
  try { return readJson(p); } catch { return null; }
}

// --- Locators ----------------------------------------------------------------

function chapterPath(id)      { return path.join(ENGINE_DATA, 'chapters', id, 'chapter.json'); }
function bookPath(id)         { return path.join(ENGINE_DATA, 'books',    id + '.json'); }
function peopleDir()          { return path.join(ENGINE_DATA, 'people'); }
function placesDir()          { return path.join(ENGINE_DATA, 'places'); }
function factionsDir()        { return path.join(ENGINE_DATA, 'factions'); }
function syntheticsDir()      { return path.join(ENGINE_DATA, 'synthetics'); }

function findEntityByName(dir, name) {
  if (!fs.existsSync(dir)) return null;
  const target = name.trim();
  for (const f of fs.readdirSync(dir)) {
    if (!f.endsWith('.json')) continue;
    const j = readJsonOrNull(path.join(dir, f));
    if (!j) continue;
    if (j.name === target) return j;
    if (Array.isArray(j.aliases) && j.aliases.includes(target)) return j;
  }
  return null;
}

// --- Canon-context build -----------------------------------------------------

function summarizePerson(p) {
  if (!p) return null;
  const lines = [];
  lines.push(`PERSON: ${p.name}${p.aliases?.length ? '  (aliases: ' + p.aliases.slice(0, 3).join(', ') + ')' : ''}`);
  if (p.age) lines.push(`  age: ${p.age}`);
  if (p.role) lines.push(`  role: ${p.role.slice(0, 200)}`);
  if (p.species) lines.push(`  species: ${p.species}`);
  if (p.augmentations) lines.push(`  augmentations: ${String(p.augmentations).slice(0, 400)}`);
  if (p.affiliation) lines.push(`  affiliation: ${p.affiliation}`);
  if (p.location) lines.push(`  location: ${String(p.location).slice(0, 300)}`);
  if (p.psychology?.secret) lines.push(`  SECRET (private): ${String(p.psychology.secret).slice(0, 400)}`);
  if (p.psychology?.blind_spots?.length) lines.push(`  blind_spots: ${p.psychology.blind_spots.join(' | ').slice(0, 400)}`);
  if (p.behavioral?.decision_rules?.length) lines.push(`  decision_rules: ${p.behavioral.decision_rules.join(' | ').slice(0, 600)}`);
  if (p.speech_patterns?.avoidances?.length) lines.push(`  avoidances: ${p.speech_patterns.avoidances.join(' | ').slice(0, 300)}`);
  if (p.neural_abilities?.length) {
    const abilities = p.neural_abilities.map(a => `${a.name}${a.passive ? ' (passive)' : ''}`).join(', ');
    lines.push(`  neural_abilities: ${abilities}`);
  }
  if (p.timeline?.length) {
    const recent = p.timeline.slice(-3).map(t => `${t.date}: ${t.event}`).join(' | ');
    lines.push(`  recent_timeline: ${recent.slice(0, 600)}`);
  }
  return lines.join('\n');
}

function summarizeBookState(book, currentChapterIndex) {
  if (!book) return '';
  const lines = [];
  lines.push(`BOOK: ${book.title}`);
  if (book.premise) lines.push(`  premise: ${book.premise.slice(0, 400)}`);
  if (book.state_at_end?.character_status) {
    lines.push('  END-OF-BOOK STATUS (canonical):');
    for (const [k, v] of Object.entries(book.state_at_end.character_status)) {
      lines.push(`    - ${k}: ${String(v).slice(0, 300)}`);
    }
  }
  if (book.state_at_end?.open_threads?.length) {
    lines.push('  OPEN THREADS (unresolved canon):');
    book.state_at_end.open_threads.forEach(t => lines.push(`    - ${String(t).slice(0, 300)}`));
  }
  if (book.state_at_end?.canon_changes?.length) {
    lines.push('  CANON CHANGES:');
    book.state_at_end.canon_changes.forEach(c => lines.push(`    - ${String(c).slice(0, 300)}`));
  }
  return lines.join('\n');
}

function summarizePriorChapter(c) {
  const lines = [];
  lines.push(`CHAPTER ${c.number}: ${c.title}`);
  if (c.synopsis) lines.push(`  ${c.synopsis.slice(0, 1200)}`);
  return lines.join('\n');
}

function buildCanonContext(chapter, book, characters, priorChapters) {
  const sections = [];
  sections.push('=== CANON CONTEXT ===');
  sections.push('The following is the established canon as of the chapter being checked.');
  sections.push('');

  if (book) {
    sections.push('--- BOOK ---');
    sections.push(summarizeBookState(book));
    sections.push('');
  }

  if (priorChapters.length > 0) {
    sections.push('--- PRIOR CHAPTERS (in canonical order) ---');
    priorChapters.forEach(pc => {
      sections.push(summarizePriorChapter(pc));
      sections.push('');
    });
  }

  sections.push('--- CHARACTERS IN SCOPE ---');
  characters.forEach(p => {
    const s = summarizePerson(p);
    if (s) {
      sections.push(s);
      sections.push('');
    }
  });

  return sections.join('\n');
}

// --- Prior-chapter retrieval -------------------------------------------------

function loadPriorChapters(book, chapterId) {
  if (!book?.chapter_ids) return [];
  const idx = book.chapter_ids.indexOf(chapterId);
  if (idx <= 0) return [];
  const priors = [];
  for (let i = 0; i < idx; i++) {
    const c = readJsonOrNull(chapterPath(book.chapter_ids[i]));
    if (c) priors.push(c);
  }
  return priors;
}

// --- Character resolution ----------------------------------------------------

function loadCharactersFromList(names) {
  const out = [];
  for (const n of names || []) {
    if (!n) continue;
    const cleaned = n.replace(/\s*\([^)]*\)\s*$/, '').trim();
    const p = findEntityByName(peopleDir(), cleaned) || findEntityByName(peopleDir(), n);
    if (p) out.push(p);
  }
  return out;
}

// --- Vote dispatch -----------------------------------------------------------

const VOTE_QUESTION =
  'Identify every contradiction in the DRAFT TEXT against the established CANON CONTEXT. ' +
  'For each contradiction, classify it as one of: ' +
  'EPISTEMIC (a character is shown to know or reference a fact they have no plausible source for knowing), ' +
  'TEMPORAL (an event is referenced in a sequence inconsistent with prior chapters), ' +
  'CAPABILITY (a character demonstrates an ability they should not have, or fails to use one they should), ' +
  'CANON (a stated fact directly conflicts with an entity record or book state). ' +
  'Output ONLY a single JSON array on the final line of your response, with each item shaped: ' +
  '{ "type": "EPISTEMIC|TEMPORAL|CAPABILITY|CANON", "snippet": "<the text fragment from the draft>", ' +
  '"conflict": "<what canon source it contradicts>", "severity": "low|medium|high", "fix_suggestion": "<concrete one-line fix>" }. ' +
  'If no contradictions are found, output [] on the final line. ' +
  'Be strict: a character knowing or referencing private medical / hardware / classified information must have a stated source for that knowledge in the canon. Reading a public roster file does not grant access to a character\'s NeoCortex medical file or program-internal capabilities.';

function callLegionVote(question, contextText, opts) {
  const args = ['vote', question, '--context', contextText, '--quorum', opts.quorum || 'plurality'];
  if (opts.maxTokens) args.push('--max-tokens', String(opts.maxTokens));
  if (opts.noNarrative) args.push('--no-narrative');

  if (opts.dryRun) {
    console.log('=== DRY RUN — would invoke ===');
    console.log(LEGION_BIN);
    console.log('args[0]: vote');
    console.log('args[1] (question, ' + question.length + ' chars):', question.slice(0, 120) + '...');
    console.log('--context (' + contextText.length + ' chars)');
    console.log('--quorum', opts.quorum || 'plurality');
    console.log('=== context preview (first 2000 chars) ===');
    console.log(contextText.slice(0, 2000));
    return null;
  }

  const r = spawnSync(LEGION_BIN, args, { encoding: 'utf-8', maxBuffer: 50 * 1024 * 1024 });
  if (r.error) {
    console.error('legion CLI error:', r.error.message);
    process.exit(2);
  }
  // legion returns 0 if quorum reached, 1 otherwise. Both produce JSON on stdout.
  let parsed;
  try {
    parsed = JSON.parse(r.stdout);
  } catch {
    console.error('legion did not return JSON. stdout was:');
    console.error(r.stdout.slice(0, 2000));
    if (r.stderr) console.error('stderr:', r.stderr.slice(0, 1000));
    process.exit(2);
  }
  return parsed;
}

// --- Contradiction extraction from vote payload ------------------------------

function extractFindings(votePayload) {
  // Each voter's `decision` should end with a JSON array (per the prompt).
  // We extract that array per voter, then merge.
  const allFindings = [];
  for (const v of votePayload.votes || []) {
    if (v.error) continue;
    const txt = (v.decision || '') + '\n' + (v.reasoning || '');
    const m = txt.match(/\[\s*[\s\S]*?\]\s*$/m);
    if (!m) continue;
    let arr;
    try { arr = JSON.parse(m[0]); } catch { continue; }
    if (!Array.isArray(arr)) continue;
    for (const f of arr) {
      if (typeof f !== 'object' || !f) continue;
      allFindings.push({
        type: f.type || 'UNKNOWN',
        snippet: (f.snippet || '').slice(0, 300),
        conflict: (f.conflict || '').slice(0, 400),
        severity: f.severity || 'medium',
        fix_suggestion: (f.fix_suggestion || '').slice(0, 300),
        flagged_by: v.voter || v.provider || 'unknown',
      });
    }
  }
  return allFindings;
}

function consolidateFindings(findings) {
  // Merge near-duplicate findings (same snippet + same conflict prefix).
  const seen = new Map();
  for (const f of findings) {
    const key = (f.snippet.slice(0, 120) + '|' + f.conflict.slice(0, 120)).toLowerCase();
    if (!seen.has(key)) {
      seen.set(key, { ...f, flagged_by: [f.flagged_by] });
    } else {
      const e = seen.get(key);
      if (!e.flagged_by.includes(f.flagged_by)) e.flagged_by.push(f.flagged_by);
    }
  }
  return Array.from(seen.values()).sort((a, b) => b.flagged_by.length - a.flagged_by.length);
}

// --- Main --------------------------------------------------------------------

function parseArgs(argv) {
  const args = { positional: [] };
  for (let i = 2; i < argv.length; i++) {
    const a = argv[i];
    if (a === '--providers')      args.providers = argv[++i];
    else if (a === '--quorum')    args.quorum = argv[++i];
    else if (a === '--max-tokens') args.maxTokens = parseInt(argv[++i], 10);
    else if (a === '--max-context-chars') args.maxContextChars = parseInt(argv[++i], 10);
    else if (a === '--dry-run')   args.dryRun = true;
    else if (a === '--no-narrative') args.noNarrative = true;
    else if (a === '-h' || a === '--help') args.help = true;
    else args.positional.push(a);
  }
  return args;
}

function printUsage() {
  console.log('usage: node tools/check-contradictions.js <chapter_id> [opts]');
  console.log('  --providers <list>      (passed through to legion if you wire it)');
  console.log('  --quorum <q>            plurality | simplemajority | twothirds | unanimous');
  console.log('  --max-tokens <N>        max tokens per voter response');
  console.log('  --max-context-chars <N> truncate canon context to this size');
  console.log('  --dry-run               print the assembled prompt, do not invoke legion');
  console.log('  --no-narrative          skip narrative synthesis (faster)');
}

function main() {
  const args = parseArgs(process.argv);
  if (args.help || args.positional.length === 0) {
    printUsage();
    process.exit(args.help ? 0 : 2);
  }
  const chapterId = args.positional[0];
  const chapter = readJsonOrNull(chapterPath(chapterId));
  if (!chapter) {
    console.error('chapter not found:', chapterId);
    process.exit(2);
  }

  const book = chapter.book_id ? readJsonOrNull(bookPath(chapter.book_id)) : null;
  const characters = loadCharactersFromList(chapter.characters || []);
  const priorChapters = book ? loadPriorChapters(book, chapterId) : [];

  let canonContext = buildCanonContext(chapter, book, characters, priorChapters);
  const limit = args.maxContextChars || 80000;
  if (canonContext.length > limit) {
    canonContext = canonContext.slice(0, limit) + '\n\n[... truncated for context budget ...]';
  }

  const draftText = chapter.html || '';
  const fullContext = canonContext + '\n\n=== DRAFT TEXT (the chapter being checked) ===\n\n' + draftText;

  if (args.dryRun) {
    callLegionVote(VOTE_QUESTION, fullContext, args);
    return;
  }

  const vote = callLegionVote(VOTE_QUESTION, fullContext, args);
  const raw = extractFindings(vote);
  const findings = consolidateFindings(raw);

  const report = {
    chapter_id: chapterId,
    chapter_title: chapter.title,
    book_id: chapter.book_id,
    characters_in_scope: characters.map(c => c.name),
    prior_chapters_count: priorChapters.length,
    canon_context_chars: canonContext.length,
    voters: vote.successful_voters,
    total_voters: vote.total_voters,
    findings_count: findings.length,
    findings,
    legion_narrative: vote.narrative || '',
  };

  console.log(JSON.stringify(report, null, 2));
  process.exit(findings.length > 0 ? 1 : 0);
}

main();
