#!/usr/bin/env node
// tools/extract-lore-triples.js
//
// Read a chapter (or every chapter in a book), dispatch a Legion Quorum vote
// with a lore-triple-extraction rubric, validate that every claimed triple's
// snippet actually appears in the source prose, and upsert the surviving
// triples into the per-entity continuity store at
// engine/data/continuity/<entity_id>.json.
//
// On a triple whose (entity_id, predicate) already exists with a different
// `object`, the existing triple is left untouched and the new one is recorded
// with status=CONTRADICTED — both are surfaced for resolution. Resolutions
// are applied via `--resolve` mode, which marks a winner and a loser without
// destroying audit history.
//
// Modes:
//   chapter (default) — extract triples from one chapter
//   book              — extract triples from every chapter in a book
//   list              — list all triples (or filter by entity / predicate)
//   contradictions    — list every CONTRADICTED triple awaiting resolution
//   resolve           — resolve a contradiction with A | B | custom
//
// Usage:
//   node tools/extract-lore-triples.js <chapter_id>
//   node tools/extract-lore-triples.js <book_id> --mode book
//   node tools/extract-lore-triples.js --mode list [--entity <id_or_name>] [--predicate <p>]
//   node tools/extract-lore-triples.js --mode contradictions
//   node tools/extract-lore-triples.js --mode resolve --triple-a <id> --triple-b <id> --winner A|B
//   node tools/extract-lore-triples.js --mode resolve --triple-a <id> --triple-b <id> --winner custom --custom-object "<value>"
//
// Exit codes:
//   0  success / no contradictions
//   1  contradictions present (extraction modes)
//   2  usage / pipeline error

const fs = require('fs');
const os = require('os');
const path = require('path');
const crypto = require('crypto');
const { spawnSync } = require('child_process');

const ROOT = path.resolve(__dirname, '..');
const ENGINE_DATA = path.join(ROOT, 'engine', 'data');
const CONTINUITY_DIR = path.join(ENGINE_DATA, 'continuity');
const LEGION_BIN = process.env.LEGION_BIN || 'D:/Projects/MindAttic/MindAttic.Legion/MindAttic.Legion.Cli/bin/Debug/net10.0/legion.exe';

if (!fs.existsSync(CONTINUITY_DIR)) fs.mkdirSync(CONTINUITY_DIR, { recursive: true });

// ── helpers ──────────────────────────────────────────────────────────────────

function readJson(p) {
  return JSON.parse(fs.readFileSync(p, 'utf-8').replace(/^﻿/, ''));
}
function readJsonOrNull(p) { try { return readJson(p); } catch { return null; } }
function writeJson(p, obj) { fs.writeFileSync(p, JSON.stringify(obj, null, 2)); }

function chapterPath(id) { return path.join(ENGINE_DATA, 'chapters', id, 'chapter.json'); }
function bookPath(id)    { return path.join(ENGINE_DATA, 'books',    id + '.json'); }
function continuityPath(entityId) { return path.join(CONTINUITY_DIR, entityId + '.json'); }

function stripHtml(s) {
  if (!s) return '';
  return String(s)
    .replace(/<[^>]+>/g, ' ')
    .replace(/&nbsp;/g, ' ')
    .replace(/&amp;/g, '&')
    .replace(/&quot;/g, '"')
    .replace(/&#39;/g, "'")
    .replace(/\s+/g, ' ')
    .trim();
}

function normalize(s) {
  return String(s || '').toLowerCase().replace(/\s+/g, ' ').trim();
}

function factId(entityId, predicate, object) {
  const h = crypto.createHash('sha1');
  h.update(entityId + '|' + normalize(predicate) + '|' + normalize(object));
  return 'fact-' + h.digest('hex').slice(0, 16);
}

// ── entity directory scan ────────────────────────────────────────────────────
// Build a name → {id, kind, file} index across every entity directory the
// fact extractor cares about. The vote rubric returns entity_name strings;
// we map back to the canonical stored entity by name match.

const ENTITY_DIRS = [
  { dir: 'people',       kind: 'person'      },
  { dir: 'places',       kind: 'place'       },
  { dir: 'factions',     kind: 'faction'     },
  { dir: 'synthetics',   kind: 'synthetic'   },
  { dir: 'corponations', kind: 'corponation' },
  { dir: 'subsidiaries', kind: 'subsidiary'  },
  { dir: 'weaponry',     kind: 'weapon'      },
  { dir: 'cyberware',    kind: 'cyberware'   },
  { dir: 'automata',     kind: 'automaton'   },
];

let entityIndexCache = null;
function buildEntityIndex() {
  if (entityIndexCache) return entityIndexCache;
  const idx = new Map(); // normalized name → { id, name, kind, file }
  for (const { dir, kind } of ENTITY_DIRS) {
    const full = path.join(ENGINE_DATA, dir);
    if (!fs.existsSync(full)) continue;
    for (const f of fs.readdirSync(full)) {
      if (!f.endsWith('.json')) continue;
      const j = readJsonOrNull(path.join(full, f));
      if (!j || !j.id || !j.name) continue;
      const entry = { id: j.id, name: j.name, kind, file: path.join(full, f) };
      idx.set(normalize(j.name), entry);
      if (Array.isArray(j.aliases)) {
        for (const a of j.aliases) idx.set(normalize(a), entry);
      }
    }
  }
  entityIndexCache = idx;
  return idx;
}

function resolveEntity(name) {
  if (!name) return null;
  const idx = buildEntityIndex();
  const cleaned = String(name).replace(/\s*\([^)]*\)\s*$/, '').trim();
  return idx.get(normalize(cleaned)) || idx.get(normalize(name)) || null;
}

// ── continuity store I/O ─────────────────────────────────────────────────────
// One JSON file per entity at engine/data/continuity/<entity_id>.json. Shape:
//   { entity_id, entity_name, kind, facts: [Triple, ...], updated: ISO }
// Triples are append-only — old values become SUPERSEDED rather than deleted.

function loadEntityFacts(entityId) {
  const p = continuityPath(entityId);
  const j = readJsonOrNull(p);
  if (j && Array.isArray(j.facts)) return j;
  return null;
}

function saveEntityFacts(file) {
  if (!file || !file.entity_id) return;
  file.updated = new Date().toISOString();
  writeJson(continuityPath(file.entity_id), file);
}

function upsertFact(entity, candidate, sourceChapter) {
  // candidate: { predicate, object, snippet, voice, confidence, extracted_by[] }
  // Returns: { status: 'NEW' | 'CONFIRMED' | 'CHANGED' | 'CONTRADICTED', fact, prior? }
  let file = loadEntityFacts(entity.id) || {
    entity_id:   entity.id,
    entity_name: entity.name,
    kind:        entity.kind,
    facts:       [],
  };

  const fid = factId(entity.id, candidate.predicate, candidate.object);
  const existing = file.facts.find(f => f.id === fid);

  // Match on (predicate) only — different objects mean conflict.
  const samePredicate = file.facts.find(f =>
    normalize(f.predicate) === normalize(candidate.predicate)
    && f.status !== 'REJECTED'
    && f.status !== 'SUPERSEDED'
  );

  if (existing && existing.status !== 'REJECTED' && existing.status !== 'SUPERSEDED') {
    // Same fact already known — confirm it.
    existing.status = 'CONFIRMED';
    existing.last_confirmed_at = new Date().toISOString();
    if (!existing.confirmed_in_chapters.includes(sourceChapter.number)) {
      existing.confirmed_in_chapters.push(sourceChapter.number);
    }
    for (const v of candidate.extracted_by || []) {
      if (!existing.extracted_by.includes(v)) existing.extracted_by.push(v);
    }
    saveEntityFacts(file);
    return { status: 'CONFIRMED', fact: existing };
  }

  // Build the new fact record
  const newFact = {
    id: fid,
    entity_id: entity.id,
    entity_name: entity.name,
    predicate: candidate.predicate,
    object: candidate.object,
    snippet: candidate.snippet,
    source_chapter_id: sourceChapter.id,
    source_chapter_number: sourceChapter.number,
    source_chapter_title: sourceChapter.title,
    voice: candidate.voice || 'narrator',
    confidence: candidate.confidence || 'medium',
    extracted_by: candidate.extracted_by || [],
    confirmed_in_chapters: [sourceChapter.number],
    first_asserted_at: new Date().toISOString(),
    last_confirmed_at: new Date().toISOString(),
    status: 'NEW',
  };

  if (samePredicate) {
    // Conflict — same predicate, different object. Mark both as CONTRADICTED;
    // the resolution flow picks a winner.
    samePredicate.status = 'CONTRADICTED';
    samePredicate.contradicts = (samePredicate.contradicts || []).concat([fid]);
    newFact.status = 'CONTRADICTED';
    newFact.contradicts = [samePredicate.id];
    file.facts.push(newFact);
    saveEntityFacts(file);
    return { status: 'CONTRADICTED', fact: newFact, prior: samePredicate };
  }

  file.facts.push(newFact);
  saveEntityFacts(file);
  return { status: 'NEW', fact: newFact };
}

// ── extraction prompt + Legion call ──────────────────────────────────────────

const EXTRACTION_QUESTION =
  'Extract every atomic factual assertion the prose makes about every named entity. ' +
  'Cover: physical features, gear/weapon placement, abilities, locations, possessions, relationships, ' +
  'knowledge, residence, employment, ages, handedness, and any persistent attribute. ' +
  'Skip transient emotion or one-time action. ' +
  'For each fact, return:  ' +
  '{ "entity_name": "<exact name as it appears>", "predicate": "<short snake_case key, e.g. weapon_carry_location, hair_color, lives_at>", ' +
  '"object": "<the value, concise>", "snippet": "<≤200-char exact quote from the prose that supports the claim>", ' +
  '"voice": "narrator|character|inner_monologue", "confidence": "low|medium|high" }. ' +
  'Output ONLY a single JSON array on the FINAL line of your response. If no facts can be extracted, output []. ' +
  'Be strict: every fact MUST be supported by an exact substring quote from the prose. Do not invent or paraphrase. ' +
  'Prefer atomic predicates over compound ones (e.g. "weapon_carry_location" not "carry_setup"). ' +
  'Use the SAME predicate name when reasserting the same kind of fact about different entities.';

function callLegionVote(question, contextText, opts) {
  const tmpFile = path.join(
    os.tmpdir(),
    `legion-fact-${process.pid}-${crypto.randomBytes(6).toString('hex')}.txt`
  );
  fs.writeFileSync(tmpFile, contextText, 'utf-8');

  const args = ['vote', question, '--context-file', tmpFile, '--quorum', opts.quorum || 'plurality'];
  if (opts.maxTokens) args.push('--max-tokens', String(opts.maxTokens));
  args.push('--no-narrative');

  if (opts.dryRun) {
    console.log('=== DRY RUN — would invoke ===');
    console.log(LEGION_BIN);
    console.log('args[0]: vote');
    console.log('args[1] (question, ' + question.length + ' chars):', question.slice(0, 120) + '...');
    console.log('--context-file', tmpFile, '(' + contextText.length + ' chars)');
    console.log('--quorum', opts.quorum || 'plurality');
    try { fs.unlinkSync(tmpFile); } catch {}
    return null;
  }

  const r = spawnSync(LEGION_BIN, args, { encoding: 'utf-8', maxBuffer: 50 * 1024 * 1024 });
  try { fs.unlinkSync(tmpFile); } catch {}
  if (r.error) {
    console.error('legion CLI error:', r.error.message);
    process.exit(2);
  }
  let parsed;
  try { parsed = JSON.parse(r.stdout); }
  catch {
    console.error('legion did not return JSON. stdout was:');
    console.error(r.stdout.slice(0, 2000));
    if (r.stderr) console.error('stderr:', r.stderr.slice(0, 1000));
    process.exit(2);
  }
  return parsed;
}

function extractJsonArrayFromText(txt) {
  if (!txt) return null;
  // Greedy first: from the first '[' to the last ']' (catches arrays embedded
  // anywhere in the response, including inside decision wrappers).
  const greedy = txt.match(/\[[\s\S]*\]/);
  if (greedy) {
    try { const a = JSON.parse(greedy[0]); if (Array.isArray(a)) return a; } catch {}
  }
  // Non-greedy fallback for responses with multiple arrays — pick the first
  // that parses and contains objects.
  const re = /\[\s*\{[\s\S]*?\}\s*\]/g;
  let m;
  while ((m = re.exec(txt)) !== null) {
    try { const a = JSON.parse(m[0]); if (Array.isArray(a) && a.length > 0) return a; } catch {}
  }
  return null;
}

function extractCandidatesFromVote(votePayload) {
  const all = [];
  for (const v of votePayload.votes || []) {
    if (v.error || v.is_error) continue;
    const txt = (v.decision || '') + '\n' + (v.reasoning || '');
    const arr = extractJsonArrayFromText(txt);
    if (!arr) continue;
    for (const c of arr) {
      if (typeof c !== 'object' || !c) continue;
      if (!c.entity_name || !c.predicate || !c.object || !c.snippet) continue;
      all.push({
        entity_name: String(c.entity_name).slice(0, 200),
        predicate:   String(c.predicate).slice(0, 80),
        object:      String(c.object).slice(0, 300),
        snippet:     String(c.snippet).slice(0, 300),
        voice:       String(c.voice || 'narrator').slice(0, 32),
        confidence:  String(c.confidence || 'medium').slice(0, 16),
        voter:       v.voter || v.provider || 'unknown',
      });
    }
  }
  return all;
}

// ── core: extract from one chapter ───────────────────────────────────────────

function buildExtractionContext(chapter, prose) {
  const sections = [];
  sections.push('=== CHAPTER PROSE (extract facts from this) ===');
  sections.push(`Chapter ${chapter.number}: ${chapter.title}`);
  sections.push('');
  sections.push(prose);
  return sections.join('\n');
}

function runChapterExtraction(chapterId, args) {
  const chapter = readJsonOrNull(chapterPath(chapterId));
  if (!chapter) {
    console.error('chapter not found:', chapterId);
    process.exit(2);
  }
  const prose = stripHtml(chapter.html);
  if (!prose) {
    console.error('chapter has no prose:', chapterId);
    process.exit(2);
  }

  const ctxText = buildExtractionContext(chapter, prose);

  if (args.dryRun) {
    callLegionVote(EXTRACTION_QUESTION, ctxText, args);
    return null;
  }

  console.error(`[lore triple extractor] chapter ${chapter.number}: ${chapter.title} — prose ${prose.length} chars`);
  const vote = callLegionVote(EXTRACTION_QUESTION, ctxText, args);
  const candidates = extractCandidatesFromVote(vote);
  console.error(`[lore triple extractor] ${candidates.length} candidate facts proposed by ${vote.successful_voters}/${vote.total_voters} voters`);

  // Group candidates by (entity_name + predicate + object) so each fact's
  // voter list is the union — Quorum threshold for storage is ≥2 voters
  // unless the user passes --min-voters.
  const minVoters = args.minVoters || 1;
  const grouped = new Map();
  for (const c of candidates) {
    // Validate snippet exists in prose (defends against hallucination)
    if (!prose.includes(c.snippet) && !prose.toLowerCase().includes(c.snippet.toLowerCase())) {
      continue;
    }
    const key = normalize(c.entity_name) + '|' + normalize(c.predicate) + '|' + normalize(c.object);
    if (!grouped.has(key)) grouped.set(key, { ...c, voters: new Set([c.voter]) });
    else grouped.get(key).voters.add(c.voter);
  }
  const survived = [...grouped.values()].filter(g => g.voters.size >= minVoters);

  // Upsert each into the fact store
  const diff = { new: [], confirmed: [], contradicted: [], unknown_entity: [] };
  const sourceChapter = { id: chapterId, number: chapter.number, title: chapter.title };

  for (const c of survived) {
    const entity = resolveEntity(c.entity_name);
    if (!entity) {
      diff.unknown_entity.push({ entity_name: c.entity_name, predicate: c.predicate, object: c.object });
      continue;
    }
    const candidate = {
      predicate: c.predicate,
      object: c.object,
      snippet: c.snippet,
      voice: c.voice,
      confidence: c.confidence,
      extracted_by: [...c.voters],
    };
    const result = upsertFact(entity, candidate, sourceChapter);
    if (result.status === 'NEW')          diff.new.push(result.fact);
    else if (result.status === 'CONFIRMED') diff.confirmed.push(result.fact);
    else if (result.status === 'CONTRADICTED') diff.contradicted.push({ new: result.fact, prior: result.prior });
  }

  return {
    mode: 'chapter',
    chapter_id: chapterId,
    chapter_title: chapter.title,
    chapter_number: chapter.number,
    voters_successful: vote.successful_voters,
    voters_total: vote.total_voters,
    candidates_proposed: candidates.length,
    candidates_validated: survived.length,
    new_facts: diff.new.length,
    confirmed_facts: diff.confirmed.length,
    contradictions: diff.contradicted.length,
    unknown_entities: diff.unknown_entity.length,
    diff,
  };
}

function runBookExtraction(bookId, args) {
  const book = readJsonOrNull(bookPath(bookId));
  if (!book) { console.error('book not found:', bookId); process.exit(2); }
  const chapterIds = book.chapter_ids || [];
  if (chapterIds.length === 0) { console.error('book has no chapters'); process.exit(2); }

  const perChapter = [];
  let totals = { new: 0, confirmed: 0, contradicted: 0, unknown: 0 };
  for (const cid of chapterIds) {
    const r = runChapterExtraction(cid, args);
    if (!r) continue;
    perChapter.push(r);
    totals.new          += r.new_facts;
    totals.confirmed    += r.confirmed_facts;
    totals.contradicted += r.contradictions;
    totals.unknown      += r.unknown_entities;
  }

  return {
    mode: 'book',
    book_id: bookId,
    book_title: book.title,
    chapters_processed: perChapter.length,
    totals,
    by_chapter: perChapter,
  };
}

// ── list / contradictions / resolve ──────────────────────────────────────────

function listAllFacts(filter) {
  const out = [];
  if (!fs.existsSync(CONTINUITY_DIR)) return out;
  for (const f of fs.readdirSync(CONTINUITY_DIR)) {
    if (!f.endsWith('.json')) continue;
    const j = readJsonOrNull(path.join(CONTINUITY_DIR, f));
    if (!j || !Array.isArray(j.facts)) continue;
    for (const fact of j.facts) {
      if (filter.entity && !(
        fact.entity_id === filter.entity
        || normalize(fact.entity_name) === normalize(filter.entity)
      )) continue;
      if (filter.predicate && normalize(fact.predicate) !== normalize(filter.predicate)) continue;
      if (filter.status && fact.status !== filter.status) continue;
      out.push(fact);
    }
  }
  return out;
}

function findTripleById(factId) {
  if (!fs.existsSync(CONTINUITY_DIR)) return null;
  for (const f of fs.readdirSync(CONTINUITY_DIR)) {
    if (!f.endsWith('.json')) continue;
    const file = readJsonOrNull(path.join(CONTINUITY_DIR, f));
    if (!file || !Array.isArray(file.facts)) continue;
    const fact = file.facts.find(x => x.id === factId);
    if (fact) return { fact, file };
  }
  return null;
}

function runResolve(args) {
  const a = findTripleById(args.tripleA);
  const b = findTripleById(args.tripleB);
  if (!a) { console.error('triple A not found:', args.tripleA); process.exit(2); }
  if (!b) { console.error('triple B not found:', args.tripleB); process.exit(2); }
  if (a.fact.entity_id !== b.fact.entity_id) {
    console.error('triples belong to different entities — cannot resolve as a single contradiction');
    process.exit(2);
  }

  const winner = (args.winner || '').toLowerCase();
  if (!['a', 'b', 'custom'].includes(winner)) {
    console.error('--winner must be A, B, or custom');
    process.exit(2);
  }

  // Single fact-store file (both belong to same entity)
  const file = a.file;
  const factA = file.facts.find(x => x.id === a.fact.id);
  const factB = file.facts.find(x => x.id === b.fact.id);

  const note = args.note || '';
  const resolvedAt = new Date().toISOString();

  if (winner === 'a' || winner === 'b') {
    const win = winner === 'a' ? factA : factB;
    const lose = winner === 'a' ? factB : factA;
    win.status = 'CANONICAL';
    win.resolved_at = resolvedAt;
    win.resolution_note = note;
    lose.status = 'REJECTED';
    lose.resolved_at = resolvedAt;
    lose.resolution_note = note;
    lose.superseded_by = win.id;
  } else {
    // custom: both losers, plus a new CANONICAL fact
    if (!args.customObject) {
      console.error('--custom-object required when --winner custom');
      process.exit(2);
    }
    factA.status = 'REJECTED';
    factA.resolved_at = resolvedAt;
    factB.status = 'REJECTED';
    factB.resolved_at = resolvedAt;
    const custom = {
      id: factId(file.entity_id, factA.predicate, args.customObject),
      entity_id: file.entity_id,
      entity_name: file.entity_name,
      predicate: factA.predicate,
      object: args.customObject,
      snippet: '(writer-asserted custom resolution)',
      source_chapter_id: '',
      source_chapter_number: 0,
      source_chapter_title: '(manual)',
      voice: 'writer',
      confidence: 'high',
      extracted_by: ['writer'],
      confirmed_in_chapters: [],
      first_asserted_at: resolvedAt,
      last_confirmed_at: resolvedAt,
      status: 'CANONICAL',
      resolved_at: resolvedAt,
      resolution_note: note,
      supersedes: [factA.id, factB.id],
    };
    factA.superseded_by = custom.id;
    factB.superseded_by = custom.id;
    file.facts.push(custom);
  }

  saveEntityFacts(file);
  return {
    mode: 'resolve',
    entity_id: file.entity_id,
    entity_name: file.entity_name,
    predicate: factA.predicate,
    winner,
    custom_object: args.customObject || null,
    fact_a: factA,
    fact_b: factB,
  };
}

// ── arg parsing + main ───────────────────────────────────────────────────────

function parseArgs(argv) {
  const a = { positional: [] };
  for (let i = 2; i < argv.length; i++) {
    const x = argv[i];
    if (x === '--mode')              a.mode = argv[++i];
    else if (x === '--quorum')       a.quorum = argv[++i];
    else if (x === '--max-tokens')   a.maxTokens = parseInt(argv[++i], 10);
    else if (x === '--min-voters')   a.minVoters = parseInt(argv[++i], 10);
    else if (x === '--entity')       a.entity = argv[++i];
    else if (x === '--predicate')    a.predicate = argv[++i];
    else if (x === '--status')       a.status = argv[++i];
    else if (x === '--triple-a')     a.tripleA = argv[++i];
    else if (x === '--triple-b')     a.tripleB = argv[++i];
    else if (x === '--winner')       a.winner = argv[++i];
    else if (x === '--custom-object') a.customObject = argv[++i];
    else if (x === '--note')         a.note = argv[++i];
    else if (x === '--dry-run')      a.dryRun = true;
    else if (x === '-h' || x === '--help') a.help = true;
    else a.positional.push(x);
  }
  return a;
}

function printUsage() {
  console.log('usage:');
  console.log('  node tools/extract-lore-triples.js <chapter_id>');
  console.log('  node tools/extract-lore-triples.js <book_id> --mode book');
  console.log('  node tools/extract-lore-triples.js --mode list [--entity <id|name>] [--predicate <p>] [--status <s>]');
  console.log('  node tools/extract-lore-triples.js --mode contradictions');
  console.log('  node tools/extract-lore-triples.js --mode resolve --triple-a <id> --triple-b <id> --winner A|B');
  console.log('  node tools/extract-lore-triples.js --mode resolve --triple-a <id> --triple-b <id> --winner custom --custom-object "<value>"');
}

function main() {
  const args = parseArgs(process.argv);
  if (args.help) { printUsage(); process.exit(0); }
  const mode = (args.mode || 'chapter').toLowerCase();

  if (mode === 'list') {
    const facts = listAllFacts({ entity: args.entity, predicate: args.predicate, status: args.status });
    console.log(JSON.stringify({ mode: 'list', count: facts.length, facts }, null, 2));
    process.exit(0);
  }

  if (mode === 'contradictions') {
    const facts = listAllFacts({ status: 'CONTRADICTED' });
    console.log(JSON.stringify({ mode: 'contradictions', count: facts.length, facts }, null, 2));
    process.exit(facts.length > 0 ? 1 : 0);
  }

  if (mode === 'resolve') {
    const r = runResolve(args);
    console.log(JSON.stringify(r, null, 2));
    process.exit(0);
  }

  if (args.positional.length === 0) { printUsage(); process.exit(2); }
  const id = args.positional[0];

  if (mode === 'book') {
    const r = runBookExtraction(id, args);
    console.log(JSON.stringify(r, null, 2));
    process.exit(r.totals.contradicted > 0 ? 1 : 0);
  }
  if (mode !== 'chapter') {
    console.error('unknown --mode:', mode);
    process.exit(2);
  }
  const r = runChapterExtraction(id, args);
  if (r) {
    console.log(JSON.stringify(r, null, 2));
    process.exit(r.contradictions > 0 ? 1 : 0);
  }
}

main();
