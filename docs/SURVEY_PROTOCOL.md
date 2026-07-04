---
id: SS-SURVEY
title: Canon Survey Protocol
layer: L0
updated: 2026-07-04
---

# Canon Survey Protocol {#SS-SURVEY}

> The canonical mechanism for resolving contradictions across docs, DB entities, and story
> prose. When a fact is disputed or inconsistent across sources, the survey is the path to
> a single authoritative answer that propagates to all layers.

---

## Why This Exists

Canon is stored in three places that can drift independently:
1. **Docs** — `docs/BIBLE.md`, `docs/universes/GLMZ.md`, `docs/nodes/<CODE>.md`
2. **DB Entities** — character/faction/place/technology descriptions in SQL
3. **Prose** — beat text in `Beats.BeatText`

No automated tool can make these agree without a human deciding what is true. The survey
is how those decisions are made in bulk, then applied consistently.

---

## Survey File Format

Surveys live at `docs/surveys/<slug>.md`. Each survey is a markdown file the user edits
directly in any editor. Answers are marked with `[x]`.

### Question format

```markdown
### Q-001 · date · Blue Massacre year
> What year did Arcturus replace the city police by force?
> Context: GLMZ_SETTING.md history table said 2065; user corrected to 2096; BIBLE.md
> uses 2096. This date anchors the no-police rule for all GLMZ stories.

- [ ] **A** — 2065 *(source: GLMZ_SETTING.md legacy)*
- [ ] **B** — 2096 *(source: user correction + GLMZ.md)*
- [ ] **C** — Custom: _____________________
```

The user marks one (or more, for multi-select) answers. A blank `_____` line accepts free text.

### Multi-select marker

Add `[multi-select]` after the category tag when multiple answers can coexist:
```markdown
### Q-042 · vocabulary · [multi-select] · Canonical job titles for freelancers
```

---

## Survey Lifecycle

```
1. DISCOVER  — workflow reads all docs + DB, finds contradictions
2. COMPOSE   — survey .md file written to docs/surveys/<slug>.md
3. ANSWER    — user opens file, marks [x] answers, saves
4. APPLY     — `ss --survey apply docs/surveys/<slug>.md`
              OR: `/survey-apply <path>` in Claude Code
5. VERIFY    — codex doctor + affected story logic sweeps
6. ARCHIVE   — survey moved to docs/surveys/archive/<slug>.md
```

---

## Application rules

When applying answers, each question type propagates to specific targets:

| Category | Applies to |
|---|---|
| `date` | BIBLE.md timeline, GLMZ.md, GLMZ_SETTING.md, entity descriptions |
| `name` / `vocabulary` | Docs, entity descriptions, `DeprecatedEntityNames` table |
| `rule` | BIBLE.md (laws), GLMZ.md (prose ground rules) |
| `geography` | GLMZ.md, node bibles that reference the location |
| `technology` | GLMZ.md, entity descriptions (Technology/Cyberware) |
| `social` | GLMZ.md Social Structure section, entity descriptions |
| `character` | Entity description in DB + relevant node bible |
| `faction` | Entity description in DB + GLMZ.md Factions section |
| `biology` | GLMZ.md, entity descriptions, BIBLE.md laws |

After applying, **prose is NOT automatically rewritten**. Beat text is flagged via
`NounConsistencyService` / finding entries for manual review in the next logic sweep.

---

## Running a Survey Cycle

```bash
# 1. Discover contradictions and write survey file
ss --survey discover --output docs/surveys/canon-sync-2026-07.md

# 2. (user answers the file)

# 3. Apply answers
ss --survey apply docs/surveys/canon-sync-2026-07.md --dry-run   # preview
ss --survey apply docs/surveys/canon-sync-2026-07.md             # apply

# 4. Verify
powershell -File tools/codex.ps1 doctor
```

---

## When to run a survey

- After any major world-building session where multiple facts were added/changed
- Before any story beat generation for a new strand
- When a logic sweep uncovers "which version is correct?" questions
- Quarterly — run a fresh discovery scan on all entity descriptions

---

## This file's own status

The protocol is the process. The answers live in the survey files under `docs/surveys/`.
The most recent survey is the source of truth for any fact it adjudicates.
