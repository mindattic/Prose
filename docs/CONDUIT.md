# The Prose Conduit: how a game keeps its story in Prose

This is the one standard every MindAttic game uses to keep its story in Prose. Today that is **GridGame**
(universe slug `gridgame`), **SpaceGod** (`spacegod`) and **Unreal Eve** (`eve`, the Unreal Engine successor to
ExperimentEve, which no longer consumes it). The design and its reasons are in
[RFC 0017](rfc/0017-game-data-conduit.md), and game writing as a first-class mode of Prose (verifiers, context, the
Writer view) in [RFC 0018](rfc/0018-game-writing-first-class.md); this page is the working spec. The entity file format is
[RFC 0007](rfc/0007-universe-interchange.md) (`schemas/universe.schema.json`), and the text format is
`prose-text/1` (`schemas/prose-text.schema.json`). For a first connection to the Hub (the API key, the Outbox hook),
see [CONSUMER_ONBOARDING.md](CONSUMER_ONBOARDING.md).

## 1. Ownership

| The game owns | Prose owns |
|---|---|
| Gameplay facts and numbers: stats, abilities, AI, stages, waves, prices, drop tables, balance | Narrative text: every word a player reads as story |
| Entity **ids** and **types**, and the facts the story should know (a boss is a boss, who belongs to which faction) | Canon entities: who a character is, what a place means, the world's rules |
| Its text **keys** (`dialogue:wanderer_ember`, `class:lich`) and what a choice tag means in play | Conversations, crawls, descriptions, quest text, books, barks, glossary |
| UI strings, tutorials, art and audio | |

- Where both touch, as with an entity's name, **the game owns the id and Prose owns the words.**
- A game repo never authors story. Its story files are **generated** by its applier from approved Prose texts:
  they are committed, but never edited by hand. An edit belongs in Prose; the next apply overwrites a hand edit and
  reports it.
- Gameplay never waits on story. A game builds, tests and plays on placeholder text, and arriving story changes text
  only, never mechanics.

## 2. Ids

- **Universe:** one per game, identified by its row id. The slug (`gridgame`, `spacegod`) is the handle every call
  uses; the display name ("GridGame", "SpaceGod") is only for display.
- **Entities:** stable, lowercase, **type-prefixed** slugs that match `^[a-z0-9]+(-[a-z0-9]+)*$`, the form Prose's
  slugifier leaves unchanged: `class-lich`, `faction-pale-concord`, `place-arkgrave`, `pilot-tomas-brennan`. A rename
  changes `name`, never `id`. Two things that slug alike get `-2`, `-3`.
- **Texts:** kind-prefixed slugs of the same form: `conversation-wanderer-ember`, `crawl-prologue`,
  `description-class-lich`. A text's id never changes.
- **Keys:** a text's `key` is the game's own lookup handle (`^[A-Za-z0-9][A-Za-z0-9_.:/-]*$`), unique among a
  universe's live texts. The game's gameplay data refers to text by key only; a missing key fails the game's tests.
- **Reserved:** the entity type `prose-text` is Prose's own. An interchange file that uses it is refused.
- **GUIDs** (PRS-T923): every entity and every text also has a GUID, its Prose database id, fixed for its life (a
  rename or a new version keeps it). Prose returns it as `guid` in every text pull (list and get) and on every entity
  of a universe export, and the kit records a text's `guid` in `applied.jsonl` and keeps it in `pending/`, so a game
  can key its text by GUID end to end, resolved at build time. The slug ids stay the handles every call takes. The
  field is additive: a reader that does not know it ignores it, and an import or a draft ignores one sent to it.

## 3. The folder layout in a game repo

```text
<game repo>/
  Data/Prose/
    prose.config.json          the one config file (section 6); committed
    <slug>.universe.json       the mapper's output (gitignored: regenerated from gameplay data)
    pending/                   pulled texts waiting to be applied, one <id>.v<version>.json each (gitignored)
    applied.jsonl              one line per applied text {id, guid, version, hash, key, kind, appliedAt}; committed
    outbox.jsonl               the drained Outbox journal (gitignored)
    .kit/                      kit scratch: import batches, exports, snapshots (gitignored)
  <game text files>            written only by the game's applier; committed
```

`Data/Prose` is the default. A game may move any of these with the config's paths.

## 4. The round trip

```text
  game data ──mapper──> <slug>.universe.json ──kit push──> Prose universe (entities, edges)
                                                               │
                                     author writes texts in Prose (Writer, generate_scene, canon tools)
                                     agents may draft (MCP draft_text); only the author approves
                                                               │
  game text files <──applier── pending/*.json <──kit pull── approved/published texts (prose-text/1)
        └─────────── applied.jsonl records {id, version, hash} ───────────┘
```

1. **Seed.** The game's mapper writes `<slug>.universe.json` from gameplay data. `kit push` validates it, imports it
   in batches as one import session, retires the entities the game deleted (`--prune`), and proves the round trip
   (export diff and graph leak check).
2. **Author.** The story is written in Prose, against the universe's entities. An agent in a game repo may draft
   through MCP (`draft_text`); it never writes story into the game repo.
3. **Approve.** The author approves texts (`prose --texts-approve`, or `POST .../approve`). MCP has no approve tool
   (RFC 0009). `prose --texts-publish` then marks a batch of approved texts published and sends one
   `texts.published` Outbox notice. To approve everything of one kind at once, the author runs a bulk approval
   (`prose --texts-approve-all <slug> --kind <k> --by <name> [--prefix p] [--about id] [--publish]`; see
   [Bulk approval](#bulk-approval)), with `--dry-run` first to see the batch.
4. **Pull.** `kit pull` fetches approved and published texts whose live version is newer than `applied.jsonl` into
   `pending/`.
5. **Apply.** `kit apply` runs the game's applier over `pending/`. The kit records each applied text in
   `applied.jsonl` and removes it from `pending/`. An orphan (a key no gameplay data uses) is reported, not applied,
   and stays pending.
6. **Commit.** The generated text files and `applied.jsonl` are committed under the game's own ticket rules.

## 5. Texts (`prose-text/1`)

The same document is the text as Prose stores it, as the API returns it, and as `pending/` holds it:

```json
{
  "schema": "prose-text/1",
  "id": "conversation-wanderer-ember",
  "guid": "01a1b974-aa06-7616-b23f-2e353c489a55",
  "universe": "gridgame",
  "kind": "conversation",
  "key": "dialogue:wanderer_ember",
  "about": ["faction-ember"],
  "speakers": ["$hero", "$companion"],
  "version": 3,
  "status": "approved",
  "hash": "sha256:…",
  "approvedBy": "ryan",
  "approvedAt": "2026-11-07T22:00:00Z",
  "context": { "place": "place-arkgrave", "chapter": "act-2", "order": 20, "trigger": "stage:arkgrave-3/victory" },
  "payload": { "start": "meet", "nodes": [ { "id": "meet", "speaker": "$hero", "line": "You fought well." } ] }
}
```

| Kind | Payload | GridGame example | SpaceGod example |
|---|---|---|---|
| `conversation` | `title?`, `start`, `nodes[{id, speaker, line, next?, choices?[{text, next, tags[]}]}]` | recruit and wanderer talks | a hail |
| `crawl` | `title`, `paragraphs[]` | a chapter's opening crawl | a sector intro |
| `description` | `short?`, `long?` (at least one) | a class's description and lore | a planet's blurb |
| `quest` | `title`, `giver?`, `brief`, `complete?`, `fail?` | a quest's summary and turn-in | a contract |
| `book` | `title`, `author?`, `pages[]` | an in-game letter | a ship's log |
| `bark` | `speaker`, `context?`, `lines[]` | a battle cry | a pilot's comms chatter |
| `glossary` | `term`, `aliases?[]`, `definition` | a keyword tooltip | a codex term |

### Context: where and when a text appears

`context` is optional (PRS-T922, RFC 0018 §4). A text without it is valid, "unplaced", and behaves exactly as before;
games and kits that ignore the field are unaffected, and the payload `hash` never covers it.

| Field | Meaning |
|---|---|
| `place` | an entity id of the universe: where the player meets the text. Checked like `about`; becomes an `appears-at` edge |
| `chapter` | the game's own chapter / act / region label (not interpreted by Prose) |
| `order` | a number: the earliest story point the player can see the text, on the game's own monotone scale |
| `trigger` | the game's own trigger handle (not interpreted by Prose) |
| `after` | text ids the player is guaranteed to have seen first (a partial order for a non-linear game) |

Unknown fields in `context` are refused. A change of context alone is a new version. Continuity is judged on placed
texts (an `order` or an `after`): what the player can know at a text is every text placed at or before its order plus
its `after` chain (RFC 0018 §4.2).

### Checks

Prose checks every text deterministically before it is stored:

- the payload has its kind's required fields, with no empty lines;
- every `about` id, every entity speaker and the `context.place` exists in the universe (a `$placeholder` speaker is
  the game's to resolve);
- a conversation's `start`, every `next` and every choice `next` name a node of the text, or a `$token` the game
  resolves (`$outcome`); every node's `speaker` is one of the text's `speakers`;
- every `{entity:<id>}` reference in any line names an entity of the universe;
- the `key` is not used by another live text of the universe;
- a `context`, when present, has only the fields above, in their types.

A text that fails these is refused. Then the **verifiers** run (RFC 0018 §5), and what they find is filed as
findings, never as a refusal:

| Family | Codes | Finds |
|---|---|---|
| `TEXT-CANON` | `retired-entity`, `missing-entity`, `glossary-conflict`, `unknown-giver` | a reference to, or a line naming, an entity retired in canon; two glossary texts defining one term; a quest giver id that names no entity |
| `TEXT-CONTINUITY` | `before-introduction`, `unknown-after`, `after-order`, `place-type` | a placed text naming an entity the player only meets at a later order; a bad `after`; a `place` that is not a place |
| `TEXT-OBLIGATION` | `quest-unresolved`, `undescribed` | a quest with no `complete`; a story text naming entities that have no `description` text |
| `TEXT-VOICE` | `drift` | an entity speaker whose lines read closer to another speaker's established voice (`$placeholders` skipped; thin samples give no signal) |
| `TEXT-BRANCH` | `unreachable`, `no-ending`, `duplicate-choice` | conversation nodes no path reaches; a talk with no way out; one choice offered twice |
| `TEXT-BARK` | `duplicate`, `repeated`, `long` | a line twice in a pool; a line in two pools of one speaker; a line over 140 characters |

- **When:** on every draft (the draft result lists them), on every approval (single and bulk, the result lists them),
  and on demand (`prose --texts-verify <slug> [id...] [--kind k]`, `POST .../texts/verify`, MCP `verify_texts`, the
  Writer's `/texts` view). A draft re-checks its own text; a sweep re-checks every text against the others.
- **Where:** the shared `Findings` table, at `text:{universe}/{id}#v{version}`, category per family, `SuggestedFix`
  always empty, `SourceRuleVersion = prose-text-verify/1`. Each run replaces the version's open `TEXT-` findings;
  a finding the author triaged or dismissed stays so when found again.
- **Never blocking** (RFC 0009): no finding refuses a draft or an approval, demotes an approved text or holds it back
  from a pull, and no verifier writes a text. The author decides.

**Lifecycle** (RFC 0009): `draft → approved → published → retired`.

- Drafting new content adds a new **version** as a draft. Drafting the same content again changes nothing.
- The previous approved or published version stays live (pullable) until the new version is approved.
- Every version is kept. `version` is the version's number, `hash` the SHA-256 of its payload.
- Only approved and published versions leave Prose. Retiring a text takes it out of every pull; the game's applier
  deletes it from the game's files when it sees `status: "retired"` (pull with `--status retired`).

### Bulk approval

PRS-T918 (the owner, 2026-11-07: yes to "approve everything of this type"). Two author-only calls act on every text of
one **kind** in a universe, optionally narrowed by **key prefix** (`--prefix class:`) and/or an **about** entity id
(`--about faction-ember`); both filters match the version being acted on. Like single approval they need the author's
name (`--by`, or `by` in the HTTP body) and have **no MCP tool** (RFC 0009: agents only draft).

- **Bulk approve** takes each matched text's pending draft. Each one gets single approval's checks (it is the newest
  draft and still a draft) and, because the author has not read each one, the draft-time checks again: payload shape
  for its kind, every entity it references still exists and is not archived, and its key is not used by another live
  text. **A draft that fails is refused and left a draft; the rest are approved.** The result lists the refusals with
  their reasons (the CLI exits 1 when any was refused, HTTP still answers 200 with a `refused` list; 400 is only for a
  request-level error such as no `by` or an unknown kind, which changes nothing). `--publish` also publishes every text
  the call approved, as one batch.
- **Bulk publish** marks every matched approved, not yet published live version published, as one batch.
- **Retired texts are skipped** by both: bringing one back stays a single, explicit `--texts-approve`.
- **Dry run** (`--dry-run`, `?dryRun=true`) computes the same lists and refusals and changes nothing: no save, no notice.
- **Notices:** each publish batch (a bulk publish, or a bulk approve with `--publish`) sends exactly one
  `texts.published` Outbox notice listing `{id, kind, key, version, hash}` for every text in it. Approval alone sends
  none.

**In the graph**, a text is an entity of type `prose-text` (its JSON record holds every version) with an `about` edge
to each entity it is about and a `spoken-by` edge to each entity speaker. Texts never appear in a universe export, and
an import never prunes them.

## 6. The consumer kit

One copy, in this repo: `consumer-kit/`. A game invokes it **by path from the Prose checkout**, the way repos invoke
`mindattic-agent-standard/prose-agent.ps1`; nothing is vendored into the game.

```powershell
D:\Projects\MindAttic\Prose\consumer-kit\kit.ps1 <command> [options]     # Windows wrapper
python D:\Projects\MindAttic\Prose\consumer-kit\kit.py <command> [options] # anywhere (Python 3.9+, stdlib only)
```

The kit finds `Data/Prose/prose.config.json` by walking up from the current directory (or `--config <path>`).

| Command | Does |
|---|---|
| `validate` | checks the universe file (schema plus contract: stable ids, no duplicates, no dangling relations, no reserved type) and every `pending/*.json` text |
| `map` | runs the game's mapper to (re)write the universe file |
| `push [--map] [--prune] [--no-check] [--batch-size n]` | validates, imports in batches as one import session (one Outbox notice), with `--prune` retires entities the game no longer has, then exports back, diffs, and runs the leak check |
| `diff [<ours> <theirs>]` | semantic diff of two interchange files; with none, the universe file against a fresh export from the Hub. Entities Prose authored itself (export `origin: "prose"`) and their relations are set aside |
| `leak [<ours> <snapshot> [--export f]]` | every node and edge in a Hub `/snapshot?scope=all` must be one of the file's, compared by entity name (texts and retired entities are ignored; a retired node that is the only node of a live entity's name is kept, since an older Hub merged the two); with no files, live against the Hub. Prose-authored entities named by the export (matched by `entityId` = `guid`) are set aside with every edge that touches them, and so are the export's `proseRelations`; any other node or edge still leaks |
| `pull [--kind k] [--since v] [--status s] [--all]` | approved and published texts newer than `applied.jsonl` into `pending/` |
| `apply [--dry-run]` | runs the applier, records applied texts in `applied.jsonl`, clears them from `pending/` |
| `status` | the universe on the Hub, entity and text counts by kind and status, pending, last applied |
| `outbox [--peek]` | drains `/api/outbox/<consumer>` into `outbox.jsonl` and prints it |

If the Hub is down, `push`, `pull`, `status` and `outbox` start the deployed `C:\Apps\MindAttic\Prose\Hub.exe`
(as Prose's own SessionStart hook does) and wait for it.

**Config** (`Data/Prose/prose.config.json`); every path is relative to the config's folder:

```json
{
  "kit": 1,
  "consumer": "gridgame",
  "universe": "gridgame",
  "displayName": "GridGame",
  "root": "../..",
  "mapper": "python tools/prose_map.py --out Data/Prose/gridgame.universe.json",
  "applier": "python tools/prose_apply.py Data/Prose/pending",
  "kinds": ["conversation", "crawl", "description", "quest", "bark", "glossary"],
  "tags": ["right-answer"],
  "universeFile": "gridgame.universe.json",
  "pending": "pending",
  "applied": "applied.jsonl",
  "batchSize": 400,
  "prune": true
}
```

`root` is the game repo's root (the mapper and applier run there). `kit` is the kit contract version the game was
built against; the kit refuses a config whose `kit` is newer than itself. `tags` is the game's own vocabulary of
choice tags; `validate` warns on a pending text that uses a tag outside it.

### The game's two pieces (contracts)

- **Mapper:** any command. It writes the universe interchange file (`universeFile`) from the game's gameplay data and
  exits 0. Entity ids follow section 2; no balance numbers in `details`. Deterministic: the same game data gives the
  same file.
- **Applier:** any command, run with `pending/` holding `prose-text/1` files. For each text it writes the game's text
  files by `key` and prints one JSON line per text to stdout:
  `{"id": "...", "version": 3, "hash": "sha256:...", "result": "applied" | "orphan" | "skipped", "note": "..."}`.
  It exits non-zero only when it could not run at all. The kit records the `applied` lines and leaves the rest
  pending. The applier also reports, on stderr, each gameplay key that has no text yet (it keeps its placeholder).

Both are game code, tested in the game repo.

## 7. Prose's surface

All HTTP calls go to the Hub at `http://127.0.0.1:5900`. Writes send `X-Prose-Key` (see CONSUMER_ONBOARDING.md);
reads need no key.

| Operation | HTTP | CLI | MCP |
|---|---|---|---|
| Import entities | `POST /api/universes/{slug}/import?mode=merge\|replace&session=<id>&final=true\|false` | `prose --universe-import <file> [--universe s] [--replace]` | `import_universe_file(path, slug?, replace?)` |
| Export entities | — | `prose --universe-export <slug> <path>` | `export_universe_file` |
| List texts | `GET /api/universes/{slug}/texts?kind=&status=&since=&key=` | `prose --texts-list <slug> [--kind k] [--status s]` | `list_texts(universe, kind?, status?)` |
| Get a text | `GET /api/universes/{slug}/texts/{id}?version=` | `prose --texts-get <slug> <id>` | `get_text(universe, id)` |
| Pull texts | (the list call, which returns whole documents) | `prose --texts-pull <slug> --out <dir> [--kind k] [--since v]` | — |
| Draft a text | `POST /api/universes/{slug}/texts` | `prose --texts-draft <slug> <file.json>` | `draft_text(universe, json)` |
| Approve | `POST /api/universes/{slug}/texts/{id}/approve {by, version?}` | `prose --texts-approve <slug> <id>... --by <name>` | none (RFC 0009) |
| Publish | `POST /api/universes/{slug}/texts/publish {by, ids?}` | `prose --texts-publish <slug> --by <name> [ids...]` | none |
| Bulk approve | `POST /api/universes/{slug}/texts/approve[?dryRun=true] {by, kind, keyPrefix?, about?, publish?}` | `prose --texts-approve-all <slug> --kind <k> --by <name> [--prefix p] [--about id] [--publish] [--dry-run] [--json]` | none (RFC 0009) |
| Bulk publish | `POST /api/universes/{slug}/texts/publish[?dryRun=true] {by, kind, keyPrefix?, about?}` | `prose --texts-publish-all <slug> --kind <k> --by <name> [--prefix p] [--about id] [--dry-run] [--json]` | none |
| Retire | `POST /api/universes/{slug}/texts/{id}/retire {by}` | `prose --texts-retire <slug> <id> --by <name>` | none |
| Verify (RFC 0018) | `POST /api/universes/{slug}/texts/verify {ids?, kind?}` | `prose --texts-verify <slug> [id...] [--kind k] [--json]` | `verify_texts(universe, ids?, kind?)` |
| Findings (RFC 0018) | `GET /api/universes/{slug}/texts/findings[?status=]`, `GET .../texts/{id}/findings` | `prose --texts-findings <slug> [id] [--status s] [--json]` | `text_findings(universe, id?, status?)` |
| Writer view (RFC 0018) | `/texts`, `/texts/{slug}[?entity=id]`, `/texts/{slug}/{id}`: browse by kind, key or entity; each text's versions and findings; the approval queue with bulk approve by kind (previewed, the author's name and click) | — | — |
| Outbox | `GET /api/outbox/{consumer}[?peek=true]` | — | — |

`since` is a version number (the live version is greater) or an ISO date (approved or published after it).

**Import modes.** `merge` (the default) upserts and never deletes, as before. `replace` treats the file as the
complete set of the game's entities: an entity a previous import brought in that the file no longer has is retired
(`Status = archived`, kept, never deleted; a later import of the same id revives it), and an imported relation an
entity no longer lists is invalidated. Entities Prose authored itself and texts are never retired by an import.

**Export provenance** (PRS-T923). Every exported entity carries `guid` (its database id) and `origin`: `import` for an
entity an interchange import brought in (it is in the universe's import manifest), `prose` for canon Prose authored
itself (graded authored, or made with Prose's own tools). The kit's round trip compares only the game's entities:
`diff` and `leak` set the `prose` ones aside, with their relations to the game's entities, so a universe the author
has enriched in Prose still round-trips clean. A relation the author adds in Prose between two of the game's own
entities (say, an NPC joined to a faction in the Hub) is not the game's either: it stays out of `relations` and is
listed in the entity's `proseRelations`, which `leak` excuses edge for edge; any other extra edge still leaks. These
fields are ignored on import and never stored.
A batched push passes the same `session` on every batch and `final=true` on the last: the prune runs once, over the
union of every batch, and the Outbox gets one notice for the whole push.

## 8. Adopting the conduit (a new game)

1. Pick the slug and display name. Add `Data/Prose/prose.config.json` (section 6) and gitignore
   `Data/Prose/*.universe.json`, `Data/Prose/pending/`, `Data/Prose/outbox.jsonl`, `Data/Prose/.kit/`.
2. Write the mapper; run `kit.ps1 push --map --prune`. The universe is created on first push.
3. Move every player-facing story string out of gameplay data into generated text files keyed by id, with a loader
   that joins them by key and a test that every key resolves.
4. Seed the story the game already has into Prose as drafts (`prose --texts-draft`, one file or a folder), so nothing
   is lost. The author approves or replaces it in Prose.
5. Write the applier; run `kit.ps1 pull` then `kit.ps1 apply`, run the game's tests, commit.
6. Point the game's agents at Prose for story: drafts go through MCP `draft_text`, never into the game repo.
