# Prose SQL Schema

Database name: **Prose**.
Engine: SQL Server (matches the `FractionsOfACenter` / `TaxRateCollector` infra).
Provider: EF Core (`Microsoft.EntityFrameworkCore.SqlServer`).

## Goals

1. **Single source of truth.** Every entity type currently in `engine/data/*.json` lands here.
2. **Bi-temporal.**
   - **System time** — when did the row exist in the DB. Handled by SQL Server `SYSTEM_VERSIONING = ON` history tables. Free audit log.
   - **Story time** — when is the fact true in-world. Two `DATE` columns (23rd-century calendar — `2256-04-15` works directly in `DATE` since SQL Server's range is 0001-01-01 → 9999-12-31).
3. **Extensible.** New entity types add a subtype table; new ad-hoc fields go into `EntityProperty` (JSON bag) without a migration. New cross-cutting facets go into `Taxonomy`.
4. **Indexed for the hot reads.** Slug lookup, type filter, edge traversal, story-time-as-of queries.

---

## Universal layer

### `Entity` — every world object, one row each

| column | type | notes |
|---|---|---|
| `Id` | `UNIQUEIDENTIFIER` | guid7, PK |
| `EntityType` | `NVARCHAR(40)` | `character`, `place`, `faction`, `CorpoNation`, `subsidiary`, `synthetic`, `automaton`, `weapon`, `equipment`, `cyberware`, `apparel`, `ammunition`, `pharmaceutical`, `genemod`, `material`, `transportation`, `consumer_good`, `archetype`, `quote`, `news`, `contract`, `document`, `vocabulary`, `lab_specimen`, `psionic` |
| `Name` | `NVARCHAR(400)` | display name |
| `Slug` | `NVARCHAR(400)` | derived once from name; **UNIQUE** |
| `Status` | `NVARCHAR(40)` | `canon`, `stub`, `archived` |
| `Description` | `NVARCHAR(MAX)` | summary blurb |
| `CreatedAt` | `DATETIME2` | `SYSUTCDATETIME()` default |
| `ModifiedAt` | `DATETIME2` | trigger or app-level update |
| `SysStart`, `SysEnd` | `DATETIME2` | `PERIOD FOR SYSTEM_TIME` (history table: `Entity_History`) |

**Indexes:**
- `UQ_Entity_Slug` on `(Slug)` — name resolution is the hottest read in the codebase.
- `IX_Entity_Type` on `(EntityType, Slug)` — list-by-type is everywhere (the `/characters`, `/places`, … pages).
- `IX_Entity_Name_FT` full-text catalog on `(Name, Description)` for search.

### `EntityProperty` — the flex bag (story-time aware)

Anything that doesn't deserve its own column or that varies per entity-type instance.

| column | type | notes |
|---|---|---|
| `Id` | `BIGINT IDENTITY` | PK |
| `EntityId` | `UNIQUEIDENTIFIER` | FK `Entity.Id`, ON DELETE CASCADE |
| `PropertyKey` | `NVARCHAR(120)` | `role`, `location`, `affiliation`, `ammo_capacity`, anything |
| `Value` | `NVARCHAR(MAX)` | scalar or JSON value (typed via `ValueKind`) |
| `ValueKind` | `NVARCHAR(20)` | `text`, `int`, `float`, `bool`, `json` |
| `StoryValidFrom` | `DATE NULL` | 23rd-century date; null = always-valid before |
| `StoryValidUntil` | `DATE NULL` | null = currently valid |
| `Source` | `NVARCHAR(200)` | `canon`, `chapter:{guid}`, `writer_assertion`, `repair:{run_id}` |
| `SysStart`, `SysEnd` | `DATETIME2` | `PERIOD FOR SYSTEM_TIME` |

**Indexes:**
- `IX_EntityProperty_Entity` on `(EntityId, PropertyKey, StoryValidFrom)` — pull every property for an entity at a story-time, sorted.
- `IX_EntityProperty_Key` on `(PropertyKey, Value)` filtered `WHERE StoryValidUntil IS NULL` — find every entity whose `affiliation` is currently `Arcturus`.

### `Edge` — typed temporal relationships

| column | type | notes |
|---|---|---|
| `Id` | `BIGINT IDENTITY` | PK |
| `SourceId` | `UNIQUEIDENTIFIER` | FK `Entity.Id` |
| `TargetId` | `UNIQUEIDENTIFIER` | FK `Entity.Id` |
| `RelationType` | `NVARCHAR(80)` | `carries`, `wields`, `wears`, `owns`, `partner_of`, `married_to`, `parent_of`, `employer_of`, `member_of`, `affiliated_with`, `located_at`, ... |
| `Description` | `NVARCHAR(1000)` | optional free text |
| `Weight` | `FLOAT` | edge strength (default 1.0) |
| `Sentiment` | `NVARCHAR(20)` | `positive`, `neutral`, `negative` |
| `StoryValidFrom` | `DATE NULL` | |
| `StoryValidUntil` | `DATE NULL` | null = current |
| `InvalidatedAt` | `DATETIME2 NULL` | hard delete in db time (rare; usually use story-time) |
| `Source` | `NVARCHAR(200)` | as above |
| `SysStart`, `SysEnd` | `DATETIME2` | `PERIOD FOR SYSTEM_TIME` |

**Indexes:**
- `IX_Edge_Source` on `(SourceId, RelationType, StoryValidFrom)` — outbound traversal.
- `IX_Edge_Target` on `(TargetId, RelationType, StoryValidFrom)` — inbound traversal.
- `IX_Edge_Active` on `(SourceId, TargetId)` filtered `WHERE StoryValidUntil IS NULL` — the in-game "current" snapshot.

### `Taxonomy` + `EntityTaxonomy` — extensible classification

Cross-cutting facets that don't belong inline (because they may be many-valued and vocabularies grow).

`Taxonomy` rows: `(Id, Domain, Code, Label, ParentId)` where `Domain` is e.g. `species`, `kind_of_being`, `tier`, `archetype`, `district`, `era`. `Code` is the stable handle (`human`, `synthetic_life`, `e_l_f`, `iowan_behemoth`). `Label` is the display text. Hierarchical via `ParentId`.

`EntityTaxonomy`: `(EntityId, TaxonomyId, StoryValidFrom, StoryValidUntil, Confidence)`. Same temporal pattern as edges. Composite PK `(EntityId, TaxonomyId, StoryValidFrom)`.

### `Tag` + `EntityTag` — flat tags

Lighter than taxonomies. One-domain free strings: `surveillance specialist`, `pattern recognition`, `info hub`. Same many-to-many shape but no domain or hierarchy.

---

## Per-subtype tables (strongly typed for fast queries)

Each row's PK is also FK to `Entity.Id` — TPT (table-per-type) inheritance. Every subtype row maps 1:1 to an `Entity` row with the matching `EntityType`. No data duplication; subtype tables hold only the type-specific columns.

### `Character`

| column | type | notes |
|---|---|---|
| `Id` | `UNIQUEIDENTIFIER` | PK + FK |
| `Species` | `NVARCHAR(40)` | `human`, `ai`, `android`, `synthetic`, `cyborg`, `hybrid`, `unknown` |
| `KindOfBeing` | `NVARCHAR(40)` | second taxonomy axis: `human`, `e_l_f`, `iowan_behemoth`, `automaton`, `ai_avatar`, `synthetic` — separate from species so an "android" can also be `e_l_f`-classed |
| `Gender` | `NVARCHAR(40)` | |
| `Pronouns` | `NVARCHAR(40)` | |
| `Age` | `INT` | nullable |
| `Birthdate` | `DATE NULL` | 23rd century |
| `NarrativeFunction` | `NVARCHAR(MAX)` | |
| `NarrationVoice` | `NVARCHAR(MAX)` | |
| `MidjourneyPrompt` | `NVARCHAR(MAX)` | |
| `Dalle3Prompt` | `NVARCHAR(MAX)` | |

> Tags, HomeTurf, TerritoryHomeTurf, and Affiliation were dropped 2026-05-08 as
> denormalized "convenience copies" of bridge tables. Canonical sources are now
> `EntityTags`, `CharacterHomeTurfs`, and `CharacterAffiliations` exclusively.
> See `feedback_no_denorm_convenience_copies` and `project_denorm_cleanup_plan`.

Sub-tables:
- `CharacterCyberware` — cyberware inventory (rows over time).
- `CharacterBelonging` — gear with kind/key/value/since/until.
- `CharacterKnowledge` — topic + summary + learned chapter + source beat.
- `CharacterCondition` — kind + name + severity + since/until.
- `CharacterRelationship` — denormalized relationship row (also surfaced via `Edge`).
- `CharacterPsychology` — 1:1 fears/desires/coping/secret.
- `CharacterBehavioral` — decision rules, escalation, contradictions, breaking points.
- `CharacterSpeech` — vocabulary, cadence, verbal tics, examples.
- `CharacterStats` — physical, mental, social JSON pack (or normalized — see open question).
- `CharacterPhysicalDescription` — height, build, hair, eyes, marks.
- `CharacterAncestry` — region → sub-region → nationality % triples.
- `CharacterNeuralAbility` — name + cost + passive flag.
- `CharacterBioBattery` — capacity + thresholds + recovery (1:1).

### `Place`, `Faction`, `CorpoNation`, `Subsidiary`, `Synthetic`, `Automaton`

Each gets a typed table with the structured fields the JSON model has today. Same pattern as `Character`.

### Gear (`Weapon`, `Equipment`, `Cyberware`, `Apparel`, `Ammunition`, `Pharmaceutical`, `Genemod`, `Material`, `Transportation`, `ConsumerGood`)

| `Manufacturer` | `Tier` | `Legality` | `Sector` | `Category` | type-specific stats columns |

### Story content (`Quote`, `News`, `Contract`, `Document`, `Vocabulary`, `LabSpecimen`, `Psionic`, `Archetype`)

Each gets its dedicated table.

### `Book`, `Series`, `Chapter`, `ChapterBeat`

Books and series move into the DB. Chapters keep their HTML body in a column (it's text, fits SQL). `ChapterBeat` is a child table indexed on `(ChapterId, Index)`.

### `Continuity` (migrate from SQLite)

`ContinuityClaim` (the existing schema), `ClaimContradiction`, `ClaimConfirmation`, `ExtractionRun`. Same shape, hosted by the same DB.

---

## Cross-cutting

### Story-time cursors

The dossier and precheck use `chapter:N` story points today. The DB encodes story-time as `DATE` in the 23rd century. Conversion: each `Chapter` row carries an `InWorldDate` column (the in-world date the chapter takes place on). `chapter:7` resolves to that chapter's `InWorldDate` and AsOf queries become `WHERE StoryValidFrom <= @asOfDate AND (StoryValidUntil IS NULL OR StoryValidUntil > @asOfDate)`.

### Search

- Full-text catalog on `Entity (Name, Description)`.
- Computed columns + filtered indexes for the most-asked properties (e.g. `Affiliation`).
- Semantic-search service is currently retired. Reintroduce via SQL Server 2025 vector indexes when the time comes.

### Audit / temporal recall

Every system-versioned table has a `_History` shadow. Querying `FOR SYSTEM_TIME AS OF '2026-04-15'` gets you the row as the DB knew it on that date. Combined with `StoryValidFrom`/`StoryValidUntil`, you can answer: "What did we know on 2026-04-15 about Kyle's affiliation as of 2256-Q2?"

### 23rd-century dates

Default `Chapter.InWorldDate` writes as `2256-XX-XX`. Helpers convert numeric `chapter:N` into the chapter's date so old code keeps working.

---

## Open questions before I write code

1. **Connection string / instance.** Where does SQL Server live? Need a connection string (or `(localdb)\\MSSQLLocalDB` for dev). Where does the connection string come from in the other projects (`FractionsOfACenter` / `TaxRateCollector`)?
2. **CharacterStats normalization.** Today they're free-form JSON dicts (`physical: { strength: 3, dexterity: 6, ... }`). Two paths:
   - **JSON column** — fast to migrate, slow to query specific stats.
   - **`CharacterStatValue (CharacterId, StatKey, IntValue)`** — fully indexed, slower to migrate.
   Recommendation: JSON column with a computed-column index for the 8 most-queried stats.
3. **Embeddings.** Stay in SQLite or move to SQL Server? My vote: stay in SQLite — vector data isn't where SQL Server shines unless you're on 2025+.
4. **Continuity DB.** Move to the same SQL Server DB or leave separate? My vote: move — having it inside the same DB unlocks joins from claims to entities.

---

## Phased rollout

1. **Foundation** (this session): packages, `ProseDbContext`, schema migration, `JsonImportService` skeleton, `Character` end-to-end with dual-write toggle.
2. **Type-by-type** (subsequent passes): port each entity type, importer + EF repo + cutover.
3. **Service rewires**: `WorldGraphService` reads from EF; `WorldStateService` unchanged.
4. **Decommission**: JSON repos become legacy/export.

Failures stay reversible — JSON files are canonical until the toggle flips. Worst case: roll back the toggle, JSON keeps working.

---

## Concrete next steps after schema OK

- [ ] Add EF Core packages.
- [ ] Create `Data/` namespace with `ProseDbContext` + entity classes (universal layer first, then `Character`).
- [ ] Initial migration with raw SQL for `SYSTEM_VERSIONING` (EF doesn't emit it natively).
- [ ] `JsonImportService.ImportCharactersAsync`.
- [ ] CLI: `ss --migrate-sql` to run the import.
- [ ] `EfCharacterRepository` honoring the existing `CharacterRepository` surface.
- [ ] Settings flag `DataSource = Json | Sql | Dual`. JSON stays primary until you flip.
