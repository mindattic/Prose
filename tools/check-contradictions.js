#!/usr/bin/env node
// tools/check-contradictions.js
//
// Read a chapter (or every chapter in a book), build a canon-context bundle
// from the entities and events it touches, dispatch a Legion Quorum vote
// with a contradiction-finding rubric, and emit a JSON report of flagged
// contradictions with citations.
//
// Modes:
//   chapter (default) — checks one chapter against its book's state_at_end
//                       and the synopses of PRIOR chapters. Cheap, but only
//                       catches backward contradictions and only at synopsis
//                       resolution.
//   book              — pairwise sweep: every chapter is graded against the
//                       FULL PROSE of every OTHER chapter (forward AND
//                       backward). Catches things like "X dies in chapter 3
//                       but speaks in chapter 5" or "Y is revealed left-handed
//                       in chapter 6 but catches a ball with their right hand
//                       in chapter 2." Expensive — N votes per book.
//
// Usage:
//   node tools/check-contradictions.js <chapter_id>
//   node tools/check-contradictions.js <chapter_id> --quorum twothirds
//   node tools/check-contradictions.js <chapter_id> --max-context-chars 80000
//   node tools/check-contradictions.js <chapter_id> --dry-run        # print prompt, do not vote
//   node tools/check-contradictions.js <book_id> --mode book
//   node tools/check-contradictions.js <book_id> --mode book --synopsis-only
//
// Exit codes:
//   0  no contradictions flagged
//   1  contradictions flagged
//   2  usage error or pipeline error

const fs = require('fs');
const os = require('os');
const path = require('path');
const crypto = require('crypto');
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

function loadEntityFacts(entityId) {
  if (!entityId) return null;
  const continuityFile = path.join(ENGINE_DATA, 'continuity', entityId + '.json');
  return readJsonOrNull(continuityFile);
}

function summarizeEntityFacts(entityId) {
  const file = loadEntityFacts(entityId);
  if (!file || !Array.isArray(file.facts)) return null;
  // Surface only facts the resolution flow has marked canonical or that
  // remain confirmed/new without an unresolved contradiction. Rejected and
  // superseded facts must NOT appear in canon — they are historical record.
  const live = file.facts.filter(f =>
    f.status === 'CANONICAL' || f.status === 'CONFIRMED' || f.status === 'NEW'
  );
  if (live.length === 0) return null;
  const lines = [];
  lines.push('  EXTRACTED FACTS (treat as canon):');
  for (const f of live) {
    const chapters = (f.confirmed_in_chapters || []).join(',');
    lines.push(`    - ${f.predicate} = ${f.object}  [ch ${chapters}; ${f.status}]`);
  }
  return lines.join('\n');
}

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
  const factSection = summarizeEntityFacts(p.id);
  if (factSection) lines.push(factSection);
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

function stripHtml(s) {
  if (!s) return '';
  return String(s).replace(/<[^>]+>/g, ' ').replace(/&nbsp;/g, ' ').replace(/\s+/g, ' ').trim();
}

// For book mode: a richer summary that can include full prose, so the voter
// can spot prose-level facts (handedness, who-is-where, who-is-dead) that
// would never appear in a synopsis.
function summarizeChapterForCanon(c, includeProse) {
  const lines = [];
  lines.push(`CHAPTER ${c.number}: ${c.title}`);
  if (c.synopsis) lines.push(`  SYNOPSIS: ${c.synopsis.slice(0, 1500)}`);
  if (includeProse) {
    const prose = stripHtml(c.html);
    if (prose) {
      lines.push('  PROSE:');
      lines.push(prose);
    }
  }
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

// Book-mode canon: every OTHER chapter in the book (forward AND backward),
// optionally with full prose. The framing tells voters that contradiction
// direction does not matter — a fact established later in the book is still
// canon for an earlier chapter being checked.
function buildBookCanonContext(currentChapter, book, characters, otherChapters, includeProse) {
  const sections = [];
  sections.push('=== CANON CONTEXT (ALL OTHER CHAPTERS, FORWARD AND BACKWARD) ===');
  sections.push('Below is the established canon from every OTHER chapter in this book — chapters');
  sections.push('that come BEFORE and chapters that come AFTER the one being checked. The chapter');
  sections.push('order is fixed by chapter number; treat all of it as canon. Direction does not');
  sections.push('matter:');
  sections.push('  - If a character dies in a later chapter, an earlier chapter cannot show them');
  sections.push('    being killed in a way the later chapter contradicts.');
  sections.push('  - If a character dies in an earlier chapter, a later chapter cannot show them');
  sections.push('    speaking, acting, or being addressed as if alive (without a stated mechanism).');
  sections.push('  - If a character is established left-handed in any chapter, they should not be');
  sections.push('    shown using their right hand in another chapter without explanation.');
  sections.push('  - Stated ages, locations, possessions, knowledge, augmentations, and');
  sections.push('    relationships must remain consistent across chapters in canonical sequence.');
  sections.push('');

  if (book) {
    sections.push('--- BOOK ---');
    sections.push(summarizeBookState(book));
    sections.push('');
  }

  if (otherChapters.length > 0) {
    sections.push('--- OTHER CHAPTERS (in canonical order) ---');
    const sorted = [...otherChapters].sort((a, b) => (a.number || 0) - (b.number || 0));
    sorted.forEach(oc => {
      sections.push(summarizeChapterForCanon(oc, includeProse));
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

function loadOtherChapters(book, currentChapterId) {
  if (!book?.chapter_ids) return [];
  const out = [];
  for (const cid of book.chapter_ids) {
    if (cid === currentChapterId) continue;
    const c = readJsonOrNull(chapterPath(cid));
    if (c) out.push(c);
  }
  return out;
}

function loadAllBookChapters(book) {
  if (!book?.chapter_ids) return [];
  const out = [];
  for (const cid of book.chapter_ids) {
    const c = readJsonOrNull(chapterPath(cid));
    if (c) out.push(c);
  }
  return out;
}

// Union of every named character across every chapter in the book — so when
// a chapter mentions someone obliquely (without listing them in `characters`),
// their profile is still in scope for the contradiction check.
function unionCharactersAcrossBook(book) {
  if (!book?.chapter_ids) return [];
  const seenNames = new Set();
  const all = [];
  for (const cid of book.chapter_ids) {
    const c = readJsonOrNull(chapterPath(cid));
    if (!c) continue;
    for (const rawName of c.characters || []) {
      if (!rawName) continue;
      const cleaned = rawName.replace(/\s*\([^)]*\)\s*$/, '').trim();
      if (seenNames.has(cleaned)) continue;
      const p = findEntityByName(peopleDir(), cleaned) || findEntityByName(peopleDir(), rawName);
      if (p) {
        seenNames.add(cleaned);
        all.push(p);
      }
    }
  }
  return all;
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
  'TEMPORAL (an event is referenced in a sequence inconsistent with other chapters — including ones that come after the chapter being checked), ' +
  'CAPABILITY (a character demonstrates an ability they should not have, or fails to use one they should), ' +
  'CANON (a stated fact directly conflicts with an entity record or book state). ' +
  'Output ONLY a single JSON array on the final line of your response, with each item shaped: ' +
  '{ "type": "EPISTEMIC|TEMPORAL|CAPABILITY|CANON", "snippet": "<the text fragment from the draft>", ' +
  '"conflict": "<what canon source it contradicts>", "severity": "low|medium|high", "fix_suggestion": "<concrete one-line fix>" }. ' +
  'If no contradictions are found, output [] on the final line. ' +
  'Be strict: a character knowing or referencing private medical / hardware / classified information must have a stated source for that knowledge in the canon. Reading a public roster file does not grant access to a character\'s NeoCortex medical file or program-internal capabilities.';

function callLegionVote(question, contextText, opts) {
  // Context can be hundreds of KB; passing it as argv blows Windows' ~32k argv
  // limit (ENAMETOOLONG). Write it to a temp file and pass --context-file.
  const tmpFile = path.join(
    os.tmpdir(),
    `legion-ctx-${process.pid}-${crypto.randomBytes(6).toString('hex')}.txt`
  );
  fs.writeFileSync(tmpFile, contextText, 'utf-8');

  const args = ['vote', question, '--context-file', tmpFile, '--quorum', opts.quorum || 'plurality'];
  if (opts.maxTokens) args.push('--max-tokens', String(opts.maxTokens));
  if (opts.noNarrative) args.push('--no-narrative');

  if (opts.dryRun) {
    console.log('=== DRY RUN — would invoke ===');
    console.log(LEGION_BIN);
    console.log('args[0]: vote');
    console.log('args[1] (question, ' + question.length + ' chars):', question.slice(0, 120) + '...');
    console.log('--context-file', tmpFile, '(' + contextText.length + ' chars)');
    console.log('--quorum', opts.quorum || 'plurality');
    console.log('=== context preview (first 2000 chars) ===');
    console.log(contextText.slice(0, 2000));
    try { fs.unlinkSync(tmpFile); } catch {}
    return null;
  }

  const r = spawnSync(LEGION_BIN, args, { encoding: 'utf-8', maxBuffer: 50 * 1024 * 1024 });
  try { fs.unlinkSync(tmpFile); } catch {}
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
    else if (a === '--mode')      args.mode = argv[++i];
    else if (a === '--synopsis-only') args.synopsisOnly = true;
    else if (a === '--dry-run')   args.dryRun = true;
    else if (a === '--no-narrative') args.noNarrative = true;
    else if (a === '-h' || a === '--help') args.help = true;
    else args.positional.push(a);
  }
  return args;
}

function printUsage() {
  console.log('usage:');
  console.log('  node tools/check-contradictions.js <chapter_id> [opts]');
  console.log('  node tools/check-contradictions.js <book_id> --mode book [opts]');
  console.log('');
  console.log('  --mode <m>              chapter (default) | book');
  console.log('                          book = pairwise sweep, every chapter graded against');
  console.log('                                 the FULL PROSE of every other chapter (forward');
  console.log('                                 and backward). Catches death/handedness/etc.');
  console.log('  --synopsis-only         book mode only: feed only synopses, not prose. Cheaper');
  console.log('                                 but misses prose-level facts.');
  console.log('  --providers <list>      (passed through to legion if you wire it)');
  console.log('  --quorum <q>            plurality | simplemajority | twothirds | unanimous');
  console.log('  --max-tokens <N>        max tokens per voter response');
  console.log('  --max-context-chars <N> truncate canon context to this size');
  console.log('                          (default 80000 chapter mode, 400000 book mode prose,');
  console.log('                          120000 book mode --synopsis-only)');
  console.log('  --dry-run               print the assembled prompt, do not invoke legion');
  console.log('  --no-narrative          skip narrative synthesis (faster)');
}

function runChapterCheck(chapterId, args) {
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
    return null;
  }

  const vote = callLegionVote(VOTE_QUESTION, fullContext, args);
  const raw = extractFindings(vote);
  const findings = consolidateFindings(raw);

  return {
    mode: 'chapter',
    chapter_id: chapterId,
    chapter_title: chapter.title,
    chapter_number: chapter.number,
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
}

function runBookSweep(bookId, args) {
  const book = readJsonOrNull(bookPath(bookId));
  if (!book) {
    console.error('book not found:', bookId);
    process.exit(2);
  }
  const chapterIds = book.chapter_ids || [];
  if (chapterIds.length === 0) {
    console.error('book has no chapters:', bookId);
    process.exit(2);
  }

  const includeProse = !args.synopsisOnly;
  const allCharacters = unionCharactersAcrossBook(book);
  const defaultLimit = includeProse ? 400000 : 120000;
  const limit = args.maxContextChars || defaultLimit;

  const perChapter = [];
  let totalRaw = 0;

  for (const cid of chapterIds) {
    const chapter = readJsonOrNull(chapterPath(cid));
    if (!chapter) {
      perChapter.push({ chapter_id: cid, error: 'chapter_not_found' });
      continue;
    }
    const otherChapters = loadOtherChapters(book, cid);
    let canonContext = buildBookCanonContext(chapter, book, allCharacters, otherChapters, includeProse);
    if (canonContext.length > limit) {
      canonContext = canonContext.slice(0, limit) + '\n\n[... truncated for context budget ...]';
    }
    const draftText = chapter.html || '';
    const draftHeader = `\n\n=== DRAFT TEXT (the chapter being checked: chapter ${chapter.number} — ${chapter.title}) ===\n\n`;
    const fullContext = canonContext + draftHeader + draftText;

    if (args.dryRun) {
      console.log(`\n=== DRY RUN — chapter ${chapter.number}: ${chapter.title} ===`);
      callLegionVote(VOTE_QUESTION, fullContext, args);
      continue;
    }

    console.error(`[book sweep] checking chapter ${chapter.number}: ${chapter.title} (canon ${canonContext.length} chars vs ${otherChapters.length} other chapters)`);
    const vote = callLegionVote(VOTE_QUESTION, fullContext, args);
    const raw = extractFindings(vote);
    const findings = consolidateFindings(raw);
    findings.forEach(f => {
      f.in_chapter_id = cid;
      f.in_chapter_title = chapter.title;
      f.in_chapter_number = chapter.number;
    });
    totalRaw += findings.length;

    perChapter.push({
      chapter_id: cid,
      chapter_title: chapter.title,
      chapter_number: chapter.number,
      canon_context_chars: canonContext.length,
      other_chapters_count: otherChapters.length,
      voters: vote.successful_voters,
      total_voters: vote.total_voters,
      findings_count: findings.length,
      findings,
    });
  }

  if (args.dryRun) return null;

  // Cross-chapter consolidation: the same contradiction often shows up from
  // both sides (e.g., chapter 3 says X is dead, chapter 5 has X speaking —
  // both passes will flag a finding referencing the same conflict). Merge
  // them so the user sees one entry with both chapter numbers attached.
  const allFindings = perChapter.flatMap(p => p.findings || []);
  const dedupKey = f => (f.snippet.slice(0, 80) + '|' + f.conflict.slice(0, 80)).toLowerCase();
  const seen = new Map();
  for (const f of allFindings) {
    const k = dedupKey(f);
    if (!seen.has(k)) {
      seen.set(k, { ...f, surfaced_in_chapter_numbers: [f.in_chapter_number] });
    } else {
      const e = seen.get(k);
      if (!e.surfaced_in_chapter_numbers.includes(f.in_chapter_number)) {
        e.surfaced_in_chapter_numbers.push(f.in_chapter_number);
      }
      // Union flagged_by across passes
      if (Array.isArray(f.flagged_by)) {
        for (const v of f.flagged_by) {
          if (!e.flagged_by.includes(v)) e.flagged_by.push(v);
        }
      }
    }
  }
  const consolidated = Array.from(seen.values()).sort((a, b) => {
    const av = a.flagged_by?.length || 0;
    const bv = b.flagged_by?.length || 0;
    if (bv !== av) return bv - av;
    return (b.surfaced_in_chapter_numbers?.length || 0) - (a.surfaced_in_chapter_numbers?.length || 0);
  });

  const report = {
    mode: 'book',
    book_id: bookId,
    book_title: book.title,
    chapters_checked: chapterIds.length,
    include_prose: includeProse,
    canon_context_limit: limit,
    findings_total_raw: totalRaw,
    findings_total_consolidated: consolidated.length,
    by_chapter: perChapter,
    consolidated_findings: consolidated,
  };

  console.log(JSON.stringify(report, null, 2));
  process.exit(consolidated.length > 0 ? 1 : 0);
}

function main() {
  const args = parseArgs(process.argv);
  if (args.help || args.positional.length === 0) {
    printUsage();
    process.exit(args.help ? 0 : 2);
  }
  const id = args.positional[0];
  const mode = (args.mode || 'chapter').toLowerCase();

  if (mode === 'book') {
    runBookSweep(id, args);
    return;
  }
  if (mode !== 'chapter') {
    console.error('unknown --mode:', mode, '(expected: chapter | book)');
    process.exit(2);
  }

  const report = runChapterCheck(id, args);
  if (report) {
    console.log(JSON.stringify(report, null, 2));
    process.exit(report.findings_count > 0 ? 1 : 0);
  }
}

main();
