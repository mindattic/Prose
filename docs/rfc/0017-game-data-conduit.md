---
codex: 1
project: Prose
code: SS
layer: rfc
status: accepted
updated: 2026-11-07
---

# RFC 0017: The Game Data Conduit (Prose as the story store for every game) {#SS-RFC-0017-CONDUIT}

**Status:** accepted 2026-11-07 (owner) · **Ticket:** PRS-T917 (Prose), GG-T355 (GridGame), US-T236 (SpaceGod) · **Builds on:**
RFC 0007 (Universe Interchange), RFC 0009 (No Autonomous Prose Writes)

> "Separate the gameplay from the story, and let Prose handle the story of both SpaceGod and GridGame."
> "There needs to be a standardized methodology for cross repo narrative engine (Prose) to do the work and keep the
> entities." "A conduit through which you can write or read conversations, story crawls, descriptions, books, etc;
> text of any sort into the Prose engine, which is built to make this stuff, rather than force it on a generic LLM
> guess." "The GridGame repo is not the place to do the heavy lifting or writing the entire story."
> (owner, 2026-11-07)

---

## 1. Goal

Every MindAttic game keeps two kinds of data, and they live in different places:

- **Gameplay data** (numbers, rules, abilities, stages, AI, balance) lives **in the game repo** and is tested there.
- **Story data** (canon entities, and every piece of text a player reads) lives **in Prose**. The game reads it
  through one standard conduit. The game repo never authors story.

The same conduit, toolkit and file layout serve every game. Today that means **GridGame** and **SpaceGod**; any later
game joins the same way. Each game is one Prose **universe**, identified by its id. The display names are
"GridGame" and "SpaceGod", and the slugs are `gridgame` and `spacegod`.

## 2. Principles

1. **One owner per fact.**
   - The game owns gameplay facts: HP, speed, damage, stage waves, prices, drop tables.
   - Prose owns narrative: who a character is, what they say, what a place means, how a chapter opens.
   - Where both touch, as with an entity's name, the game owns the id and Prose owns the words.
2. **Prose is the only store of story.** Game-side story files are generated copies: never edited by hand, always
   regenerable.
3. **Ids, not strings, cross the boundary.**
   - Gameplay data refers to story by stable keys, such as `class:lich` or `text:crawl/swamp`.
   - A missing key is a test failure in the game, not a silent blank.
4. **Nothing lands without approval** (RFC 0009).
   - Prose drafts text; the author approves it.
   - Only approved text leaves Prose, and the game only applies approved, versioned text.
   - No agent rewrites approved text on its own.
5. **One toolkit.**
   - The consumer kit ships from the Prose repo.
   - A game supplies only a config file, a **mapper** (game data → entities) and an **applier** (approved text → the
     game's text files).
6. **Gameplay never waits on story.**
   - A game builds, tests and plays with placeholder text.
   - Story arriving later changes text only, never mechanics.

## 3. What lives where

| Data | Owner | Stored in | Crosses the conduit as |
|---|---|---|---|
| Unit stats, abilities, AI, stages, waves, balance, prices | Game | game JSON (`classes.json`, `abilities.json`, `stages.json`, ...) | the entity's **id and type** only, never the numbers |
| Characters, classes, factions, places/regions, items, creatures | Prose (canon), seeded from the game | Prose entities + edges | Universe Interchange JSON (RFC 0007) |
| World premise, rules of the world, eras | Prose | universe record (`setting`, `logline`, `rules[]`) | interchange |
| Conversations (multi-speaker, choices) | Prose | text kind `conversation` | `prose-text/1` |
| Chapter / region opening crawls | Prose | text kind `crawl` | `prose-text/1` |
| Entity descriptions and lore blurbs | Prose | text kind `description` | `prose-text/1` |
| Quest / bounty text | Prose | text kind `quest` | `prose-text/1` |
| In-game books, letters, logs | Prose | text kind `book` | `prose-text/1` |
| Barks (short combat or ambient lines) | Prose | text kind `bark` | `prose-text/1` |
| Glossary terms | Prose | text kind `glossary` | `prose-text/1` |
| UI strings, tutorials, button labels | Game | game files | not story; stays in the game |
| Art and audio | Game | game assets | entity `details.art` may *reference* an asset path |

## 4. Architecture

```
            ┌──────────────────────────── Prose (D:\Projects\MindAttic\Prose) ────────────────────────────┐
            │ Hub 127.0.0.1:5900 (only process touching SQL) ── CLI ── MCP ── Writer                      │
            │                                                                                              │
            │  universe: gridgame                      universe: spacegod                                 │
            │   entities + edges (canon)                entities + edges (canon)                           │
            │   texts: conversation/crawl/...           texts: ...                                         │
            │   status: draft → approved → published    (same lifecycle)                                   │
            │                                                                                              │
            │  consumer-kit/  (validate · push · pull · diff · leak-check · applied log)                    │
            └───────────────▲──────────────────────────────────────────────────────┬───────────────────────┘
                 push       │ entities (interchange JSON)              pull         │ approved texts (prose-text/1)
                            │                                                        ▼
            ┌───────────────┴────────────── game repo (GridGame / SpaceGod) ─────────────────────────────────┐
            │  Data/Prose/prose.config.json      consumer, universe slug, paths, kinds the game uses           │
            │  Data/Prose/<slug>.universe.json   generated by the game's MAPPER (gitignored)                   │
            │  Data/Prose/pending/               pulled texts awaiting review in the game (optional step)      │
            │  Data/Prose/applied.jsonl          what was applied: id, version, hash, date                     │
            │  <game text files>                 written ONLY by the game's APPLIER (committed, generated)     │
            │  gameplay JSON                     refers to text by key; tests fail on a missing key            │
            └────────────────────────────────────────────────────────────────────────────────────────────────┘
```

## 5. Data model additions in Prose

### 5.1 Entities (already exist)
The Universe Interchange entity `{id, type, name, summary, details{}, relations[{to,kind}], tags[]}` is unchanged.

**Game entity types:**
- `character` (a named person)
- `class` (a playable or enemy unit type)
- `faction`
- `place` (a region or stage area)
- `item`
- `creature`
- `concept` (a lore term)
- `event` (a story beat on the timeline)

**Ids** are stable, lowercase and type-prefixed, owned by the game: `class-lich`, `faction-pale-concord`,
`place-arkgrave`. A rename changes `name`, never `id`.

### 5.2 Texts (new)
A **text** is a unit of player-facing story, authored against entities.

```json
{
  "schema": "prose-text/1",
  "id": "conversation-wanderer-ember",
  "universe": "gridgame",
  "kind": "conversation",
  "key": "dialogue:wanderer_ember",
  "about": ["faction-ember"],
  "speakers": ["$wanderer", "$companion"],
  "version": 3,
  "status": "approved",
  "hash": "sha256:...",
  "payload": { }
}
```

The `payload` shape is fixed per kind and validated by Prose:

| Kind | Payload |
|---|---|
| `conversation` | `nodes[{id, speaker, line, choices[{text, next, tags[]}]}]`, `start`; choice `tags` carry game meaning (e.g. `right-answer`) without game logic |
| `crawl` | `title`, `paragraphs[]` |
| `description` | `short`, `long` (the game decides where each shows) |
| `quest` | `title`, `giver`, `brief`, `complete`, `fail?` |
| `book` | `title`, `author?`, `pages[]` |
| `bark` | `speaker`, `context`, `lines[]` |
| `glossary` | `term`, `definition` |

**Lifecycle** (RFC 0009): `draft → approved → published → retired`.
- Only `approved`/`published` texts are pullable.
- Editing an approved text creates a new `version` as a draft. The previous approved version stays live until the new
  one is approved.
- Every version is kept.

**Placeholders in lines** (`$companion`, `$hero`, `{entity:class-lich}`) are resolved by the game at runtime. Prose
validates that each `{entity:...}` reference exists in the universe.

### 5.3 Access (new)
- **HTTP:**
  - `GET /api/universes/{slug}/texts?kind=&status=&since=`
  - `GET /api/universes/{slug}/texts/{id}`
  - `POST .../texts` (draft)
  - `POST .../texts/{id}/approve`
- **CLI:** `prose --texts-pull <slug> --kind <k> --since <version|date> --out <dir>`, plus `--texts-list`.
- **MCP:** `list_texts`, `get_text`, `draft_text`. MCP has no `approve_text` for agents: approval is the author's
  action in the Writer, or an explicit CLI call the owner runs.
- **Outbox:** one `texts.published` notice per approval batch, not per entity or batch row.

## 6. The round trip

1. **Seed.**
   - The game's **mapper** reads gameplay data and writes `<slug>.universe.json`: every entity's id, type and name,
     plus the gameplay facts the story should know (a boss is a boss, a faction's members), never balance numbers.
   - `kit push` validates it, imports it (prune mode retires entities the game deleted) and runs the round-trip diff
     and leak check.
2. **Author.**
   - The story is written **in Prose**, by the author with Prose's engine (Writer, `generate_scene`, the canon tools),
     against the universe's entities.
   - Agents working in a game repo may *draft* through MCP. They never write story into the game repo.
3. **Approve.** The author approves texts in the Writer.
4. **Pull.** `kit pull` fetches approved texts newer than `applied.jsonl` into `Data/Prose/pending/`.
5. **Apply.**
   - The game's **applier** writes each text into the game's text files by `key`, records `{id, version, hash}` in
     `applied.jsonl`, and the game's tests run.
   - A text whose `key` no gameplay data uses is reported as an orphan, not applied.
   - A gameplay key with no text keeps its placeholder and is reported.
6. **Commit.** The generated text files and `applied.jsonl` are committed in the game repo, under the game's own
   ticket rules.

**Conflicts:** Prose is the authority for text. If a generated game text file was hand-edited, the applier overwrites
it and reports the overwrite, so the edit should have been made in Prose.

## 7. Separating gameplay from story inside a game repo

Each game moves its player-facing text out of gameplay records into generated text files keyed by id. The loader
joins them at runtime, and gameplay records keep only keys.

**GridGame (GG-T355), concretely:**
- **Gameplay stays in:** `classes.json` (minus `Description`/`Lore`), `abilities.json`, `stages.json`,
  `campaign.json` (minus crawl text), `quests.json` (minus text), the drop tables, `factions.json` membership.
- **Text moves to:** `Assets/Gaiden/Resources/Content/Text/*.json`, written only by the applier:
  - `descriptions.json`
  - `crawls.json`
  - `conversations.json`, which replaces the authored part of `dialogues.json`; triggers, conditions and effects stay
    gameplay
  - `quests.json`
  - `barks.json`
  - `glossary.json`
- **The loader:** `ContentLoader` joins text to gameplay by key.
- **Validation:**
  - every key resolves
  - no narrative text field remains in gameplay JSON
  - conversation choice tags the game relies on, such as wanderer `right-answer`, are present
- **First seed:** today's in-repo text (the "Under Morrow" pass, docs/STORY.md) is pushed into Prose as `draft`
  texts, so nothing is lost. The author then replaces it with the new story (the War in Heaven) in Prose.
- **docs/STORY.md** becomes a generated summary (or a link to the universe), not a source.

**SpaceGod (US-T236):**
- Its simulation stays the source of the entities: pilots, planets and ships, via its existing C++ exporter as the
  mapper.
- It replaces its local sync and outbox scripts with the kit.
- It gains the applier it never built: approved texts become `Data/Lore/...` files the game loads.

## 8. The consumer kit (ships in `Prose/consumer-kit/`)

| Command | Does |
|---|---|
| `kit validate` | checks `<slug>.universe.json` and pending texts against the schemas (`universe.schema.json`, `prose-text.schema.json`) |
| `kit push [--prune]` | imports entities in batches; with `--prune`, retires entities absent from the file; round-trip diff and cross-universe leak check |
| `kit pull [--kind k] [--since v]` | approved texts newer than `applied.jsonl` go to `pending/` |
| `kit status` | universe health, counts, pending, orphans, last applied version |
| `kit outbox` | drains `/api/outbox/<consumer>` into the log |

**Config** (`Data/Prose/prose.config.json` in the game repo):

```json
{ "consumer": "gridgame", "universe": "gridgame", "displayName": "GridGame",
  "mapper": "python tools/prose_map.py", "applier": "python tools/prose_apply.py",
  "kinds": ["conversation","crawl","description","quest","bark","glossary"] }
```

**How it's invoked:** the kit runs from the Prose checkout by path (`D:\Projects\MindAttic\Prose\consumer-kit\kit.ps1`
plus a cross-platform Python core), so there is one copy and no vendoring. A game pins the kit version it was tested
with in its config.

**Contracts:**
- The mapper writes interchange JSON to stdout or a path.
- The applier reads `pending/*.json` and writes the game's files.
- Both are game code with their own tests in the game repo.

## 9. Reliability work folded in (PRS-T916)

- Prune/retire on re-import.
- One outbox notice per import.
- Batched transactional import, so one save is not made per entity.
- Graph nodes keyed by entity id, not name, so same-named entities no longer merge.
- Export relations from the real edges.
- A provenance block (`source: consumer@version`) on imported entities.

## 10. Rollout

| Phase | Repo | Ticket | Delivers | Done when |
|---|---|---|---|---|
| 0 | Prose | PRS-T917 | this RFC accepted; `docs/CONDUIT.md` spec + `prose-text.schema.json` | owner accepts the design |
| 1 | Prose | PRS-T917 | text kinds, lifecycle, HTTP/CLI/MCP access, outbox notice | tests green; a text can be drafted, approved and pulled |
| 2 | Prose | PRS-T917 / PRS-T916 | consumer kit + reliability fixes | kit validate/push/pull/status work against a sample universe |
| 3 | GridGame | GG-T355 | mapper, applier, text files split out of gameplay JSON, loader join, validation; seed today's text as drafts | the game plays identically with text from `Content/Text/`; suite green |
| 4 | SpaceGod | US-T236 | kit adoption + applier/loader for `Data/Lore` | an approved text reaches the running game |
| 5 | Prose (authoring) | new ticket | the GridGame story (the War in Heaven) written and approved in Prose | approved crawls, conversations and descriptions applied to GridGame |
| 6 | both games | per repo | retire in-repo story docs, add a test that bans narrative text in gameplay JSON | no story source left outside Prose |

**Order:** 0 → 1 → 2 → (3 ∥ 4) → 5 → 6. GridGame's gameplay work (enemies, regions, balance) continues throughout
with placeholder text.

## 10a. Deviations found against the real code (PRS-T917 build notes)

The working spec is `docs/CONDUIT.md`; where it differs from sections 5 to 8 above, it wins. The differences:

1. **Payload additions.** A conversation node may carry `next` (GridGame's linear talks chain node to node without
   a choice) and a conversation may carry `title`; a glossary payload may carry `aliases[]` (GridGame's glossary has
   them). Everything in section 5.2 is unchanged.
2. **Publish is its own step.** `approve` marks one version approved (pullable). `publish` marks a batch of approved
   texts published and sends the single `texts.published` Outbox notice, so a review session of fifty approvals is
   one notice. Retire is a third owner-only call. None of the three has an MCP tool.
3. **Storage.** A text is an `Entity` of the reserved type `prose-text` whose `Record.Json` holds every version, with
   `about` and `spoken-by` edges to the entities it names. No migration. A universe export never includes texts, an
   interchange file may not use the type, and a replace-import never retires one.
4. **Kit commands.** Besides those in section 8: `map`, `apply` (runs the applier and owns `applied.jsonl`, so an
   applier never writes the log itself), `diff` and `leak`. The applier reports per text on stdout as JSON lines.
5. **Import sessions.** A batched push passes one `session` id on every batch and `final=true` on the last; the prune
   and the one Outbox notice happen at the final batch, over the union of every batch.
6. **Graph nodes per entity (PRS-T916, done after the PRS-T917 build).** `UniverseGraphService` makes one node per
   entity: a name only one entity has keeps its plain name-slug node id (every name lookup is unchanged), and when
   several entities share a name the primary one (live over retired, canon over stub, universe-wide over
   book-scoped, then the oldest) keeps the name slug and each other is `{nameSlug}~{entityId:N}`. Edges rows resolve
   by entity id; `/snapshot` nodes carry `entityId`; a graph cache from before this is rebuilt, not served. Per-edge
   consumer provenance is still not done. The file-level `source` block is kept with the universe and returned on
   export.
7. **Bulk approval (PRS-T918, answers open question 11.1).** The owner said yes to "approve everything of this
   type" on 2026-11-07. Bulk approve and bulk publish act on one kind, optionally narrowed by key prefix and/or an
   about-entity id, with a dry run; author-only (`--by` / HTTP `by`), no MCP tool. Each draft is re-checked as at
   drafting (payload, entity references, key); one that fails is reported and left a draft and the rest are approved,
   so one stale draft never holds up a batch. Retired texts are skipped. Each publish batch is one `texts.published`
   notice. Details in `docs/CONDUIT.md`, "Bulk approval".

## 11. Risks and open questions

1. **Author workload.** Prose drafts and the author approves (RFC 0009). Bulk approval, e.g. "approve all crawls in
   this universe", is needed or the queue stalls. Answered 2026-11-07: yes, built in PRS-T918 (section 10a, item 7).
2. **Game-specific text logic** (wanderer right answers, quest conditions) must stay gameplay. Prose only carries
   tags, and the game interprets them. The tag vocabulary per game lives in the game's config.
3. **Localization** is out of scope now. `prose-text/1` leaves room for a `lang` field later.
4. **Hub availability.** Games never need the Hub to build or run, only to sync. The committed generated text files
   are what ships.
5. **Shared entities across games** are not shared. Each universe is segregated (SS-LAW-15). Copying an entity
   between universes is an explicit export and import.
6. **Placeholder period.** Until Phase 5, GridGame ships the "Under Morrow" draft text. It is clearly a draft, and
   gameplay never depends on its wording.
