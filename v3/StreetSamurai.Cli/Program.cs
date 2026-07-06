using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MindAttic.Authentication;
using MindAttic.Authentication.Web;
using StreetSamurai.Cli;
using StreetSamurai.Core.Data;
using StreetSamurai.Core.Extensions;
using StreetSamurai.Core.Services;
using MindAttic.Vault.Configuration;

// Force UTF-8 console I/O so piped prose (em-dashes, curly quotes, etc.) round-trips
// correctly through `Get-Content <file> | dotnet run -- --beat update --text -`.
// Without this, Windows defaults to OEM 437, which maps E2 80 94 (UTF-8 em dash) to
// the mojibake sequence "ΓÇö" and corrupts every non-ASCII character in stored beats.
Console.InputEncoding  = System.Text.Encoding.UTF8;
Console.OutputEncoding = System.Text.Encoding.UTF8;

// QuestPDF Community license — required call before the first Document.Create.
// This project is the non-commercial indie use case the Community tier exists for.
QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

// Multi-universe: a global `--universe <slug>` flag selects which universe this
// process targets (SS-LAW-15). UniverseContext also honors the SS_UNIVERSE env
// var (per terminal), so two CLIs can write different universes at once. Parsed
// here before the dispatch chain so every CLI block + the web host inherit it.
UniverseBootstrap.RequestedSlug ??= UniverseBootstrap.ParseSlug(args);

// CLI mode: dotnet run --project ... -- --rebuild-graph
// Rebuilds world_graph.json from source data without starting the web server.
if (args.Contains("--rebuild-graph"))
{
    var sp = BuildCoreServices(args);
    // Pin the universe scope BEFORE building so it can't shift mid-rebuild. Resolving the context
    // forces its lazy catalog load + applies the --universe/SS_UNIVERSE/default selection, so every
    // builder in this rebuild sees one stable scope (the non-deterministic node/edge counts came
    // from the scope resolving partway through the multi-builder pass). Defaults to GLMZ.
    var cliUniverse = sp.GetRequiredService<StreetSamurai.Core.Services.IUniverseContext>();
    Console.WriteLine($"[rebuild-graph] Universe scope: {cliUniverse.CurrentSlug} ({cliUniverse.CurrentId})");
    var graph = sp.GetRequiredService<WorldGraphService>();
    Console.WriteLine("[rebuild-graph] Rebuilding world graph from source data...");
    graph.Rebuild();
    Console.WriteLine($"[rebuild-graph] Done: {graph.NodeCount} nodes, {graph.EdgeCount} edges saved to world_graph.json");
    return;
}

// CLI mode: ss --reset-password --email <e> --password <p> [--require-change]
// Operator password reset over the MindAttic.Authentication store, no web server.
if (args.Contains("--reset-password"))
{
    var sp = BuildServicesWithVaultAndAuth(args);
    Environment.ExitCode = await ResetPasswordCli.RunAsync(args, sp);
    return;
}

// CLI mode: dotnet run --project ... -- --write-story <mode> [options]
// Generates a story via the same pipeline as the /stories UI and saves it as a Chapter.
if (args.Contains("--write-story"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await StoryWriterCli.RunAsync(args, sp);
    return;
}

// CLI mode: dotnet run --project ... -- --refine-story <projectId> [-o notes.json]
// Analyzes a completed story and writes refinement notes (no rewrites).
if (args.Contains("--refine-story"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await StoryRefineCli.RunAsync(args, sp);
    return;
}

// CLI mode: book operations — list / new / show / chapters / absorb / review / apply / export / delete.
// Run `dotnet run --project StreetSamurai.Blazor -- --book` (no subcommand) to see full usage.
if (args.Contains("--book"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await BookCli.RunAsync(args, sp);
    return;
}

// CLI mode: unified continuity store — migrate / stats / contradictions / resolve / entity.
// Run `dotnet run --project StreetSamurai.Blazor -- --continuity` (no subcommand) to see full usage.
if (args.Contains("--continuity"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = ContinuityCli.Run(args, sp);
    return;
}

// CLI mode: SQL Server migration — apply EF migrations and import JSON entities.
//   ss --migrate-sql --schema           apply EF migrations
//   ss --migrate-sql --import people    import character JSON files
//   ss --migrate-sql --all              schema + import all supported types
if (args.Contains("--migrate-sql"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await MigrateSqlCli.RunAsync(args, sp);
    return;
}

// CLI mode: prose → entities + edges. LLM-driven.
//   ss --interpret --text "..."  | --file path.txt
//   add --commit to apply, --auto-create to stub missing entities, --tag <source>
if (args.Contains("--interpret"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await InterpretCli.RunAsync(args, sp);
    return;
}

// CLI mode: insert a worldbuilding Document directly into canon.
//   ss --add-doc --title "…" --body-file path.md [--category essay] [--tags "a,b,c"] [--filename slug.md]
if (args.Contains("--add-doc"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await AddDocCli.RunAsync(args, sp);
    return;
}

// CLI mode: insert a Character from a CharacterData JSON file.
//   ss --add-character --file path.json
if (args.Contains("--add-character"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await AddCharacterCli.RunAsync(args, sp);
    return;
}

// CLI mode: insert OR update a Place/District from a DistrictData JSON file.
// Upsert: include "id" to update, omit to create. Safe service-layer path
// (DistrictRepository.Save) — no hand-SQL, collision-safe slugs.
//   ss --add-place --file path.json [--print]
if (args.Contains("--add-place"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await AddPlaceCli.RunAsync(args, sp);
    return;
}

// CLI mode: insert a CorpoNation from a CorponationData JSON file.
//   ss --add-corponation --file path.json
if (args.Contains("--add-corponation"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await AddCorponationCli.RunAsync(args, sp);
    return;
}

// CLI mode: generate a resource-tracked combat sequence via CombatSceneWriter.
//   ss --combat --file scene.json [--out prose.txt]
//   ss --combat --location "Hegewisch" --objective "..." --exchanges 6 --tone Cinematic
if (args.Contains("--combat"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await CombatCli.RunAsync(args, sp);
    return;
}

// CLI mode: insert OR update a Faction from a FactionData JSON file.
// Upsert: include "id" to update, omit to create. Safe service-layer path
// (FactionRepository.Save) — no hand-SQL, collision-safe slugs.
//   ss --add-faction --file path.json
if (args.Contains("--add-faction"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await AddFactionCli.RunAsync(args, sp);
    return;
}

// CLI mode: insert a News article from a NewsData JSON file.
//   ss --add-news --file path.json
if (args.Contains("--add-news"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await AddNewsCli.RunAsync(args, sp);
    return;
}

// CLI mode: per-table schema operations (snapshot + safe column-reorder rebuild).
//   ss --schema snapshot --table NAME [--out path.sql]
//   ss --schema rebuild  --table NAME --order "col1,col2,col3,…"
if (args.Contains("--schema"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await SchemaCli.RunAsync(args, sp);
    return;
}

// CLI mode: dump the entire StreetSamurai DB to a re-runnable .sql script.
//   ss --sql-export --schema             schema-only DDL
//   ss --sql-export --data               schema + INSERT data
//   ss --sql-export --schema --out path  override output path
if (args.Contains("--sql-export"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await SqlExportCli.RunAsync(args, sp);
    return;
}

// CLI mode: dossier-driven story repair — walks every chapter, augments character
// records with timeline entries and (optionally) LLM-extracted continuity claims.
//   ss --repair                # cheap timeline-only pass
//   ss --repair --continuity   # also run continuity extraction (LLM-heavy)
if (args.Contains("--repair"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await RepairCli.RunAsync(args, sp);
    return;
}

// CLI mode: cloud RAG over the canon corpus. Replaces the retired Ollama path.
//   ss --ask "Question" [--k 8] [--type character]
if (args.Contains("--ask"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await AskCli.RunAsync(args, sp);
    return;
}

// CLI mode: idempotent stub-creator for the seeded "Vultures on the Doorstep"
// future story. Creates the Book + Draft outline only; writes no prose.
//   ss --seed-vultures
if (args.Contains("--seed-vultures"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = VulturesSeedCli.Run(args, sp);
    return;
}

// CLI mode: report Character columns that disagree with their latest
// matching EntityStateEvents row. Lights up the static-vs-dynamic recipe
// only for columns that actually drifted.
//   ss --audit-drift           pretty-printed report
//   ss --audit-drift --json    JSON dump
if (args.Contains("--audit-drift"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await AuditDriftCli.RunAsync(args, sp);
    return;
}

// CLI mode: backfill EntityStateEvents from the dynamic columns currently
// sitting on Characters (Location, LifeStatus, Role, Affiliation, Belongings*,
// Territory*, DailyLife). One-shot, idempotent.
if (args.Contains("--backfill-character-state"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await StateBackfillCli.RunAsync(args, sp);
    return;
}

// CLI mode: rewrite ethnicity-keyed visual descriptors in image prompts to
// match a character's current genetic_ancestry. Cost-aware via stored hash.
//   ss --image-prompts regen --id <id|slug> [--force]
//   ss --image-prompts regen --all-changed
if (args.Contains("--image-prompts"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await ImagePromptsCli.RunAsync(args, sp);
    return;
}

// CLI mode: propose a plausible immediate family for one character.
//   ss --family-gen propose --of <id|slug>           dry run
//   ss --family-gen propose --of <id|slug> --commit  write characters + edges + propagate genetics
//   --seed N for reproducible RNG
if (args.Contains("--family-gen"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await FamilyGenCli.RunAsync(args, sp);
    return;
}

// CLI mode: propagate genetic_ancestry from parents to children via the
// family graph (with ±5% recombination noise). Currently a no-op until family
// ties are seeded.
//   ss --genetics propagate                     full graph
//   ss --genetics propagate --id <id|slug>      single character
//   ss --genetics propagate --seed 42           reproducible RNG
if (args.Contains("--genetics"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await GeneticsCli.RunAsync(args, sp);
    return;
}

// CLI mode: family ties — hand-seed parent/sibling/spouse links between characters.
//   ss --family parent  --parent <id|slug> --child <id|slug>
//   ss --family sibling --a <id|slug> --b <id|slug>
//   ss --family spouse  --a <id|slug> --b <id|slug>
//   ss --family show    --of <id|slug>
if (args.Contains("--family"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await FamilyCli.RunAsync(args, sp);
    return;
}

// CLI mode: scan beats for deprecated/renamed noun references.
//   ss --validate-nouns --slug <slug>
if (args.Contains("--validate-nouns"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await ValidateNounsCli.RunAsync(args, sp);
    return;
}

// CLI mode: survey management.
//   ss --list-surveys [--status Open|Completed]
//   ss --get-survey --slug <slug>
if (args.Contains("--list-surveys") || args.Contains("--get-survey"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await SurveyCli.RunAsync(args, sp);
    return;
}

// CLI mode: backfill BeatEntityMentions — index which entity names appear in
// each beat so entity-update staleness propagation works.
//   ss --scan-entity-mentions
if (args.Contains("--scan-entity-mentions"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await ScanEntityMentionsCli.RunAsync(sp);
    return;
}

// CLI mode: dump canon JSON to the user's Downloads folder.
//   ss --export global                every repo, zipped + timestamped
//   ss --export <repoName>            one repo, zipped (e.g. "people", "weaponry")
//   ss --export <entityId>            one entity, plain .json
if (args.Contains("--export"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await ExportCli.RunAsync(args, sp);
    return;
}

// CLI mode: rebuild the entity-embedding cache via cloud OpenAI.
//   ss --reembed              drift-skipped corpus pass (only changed entities re-embed)
//   ss --reembed --force      clear the table first, re-embed everything
if (args.Contains("--reembed"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await ReembedCli.RunAsync(args, sp);
    return;
}

// CLI mode: query the Legion / LLMVoting cloud-LLM panel directly.
//   ss --legion ask "Q" --options "A,B,C"  → forced-choice Quorum decision (JSON on stdout)
//   ss --legion vote "Q" [--context "…"]    → open-ended vote with synthesized narrative
if (args.Contains("--legion"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await LegionCli.RunAsync(args, sp);
    return;
}

// --archive-json retired 2026-05-08 with JsonArchivalService — engine/data/*.json
// no longer exists, so legacy-file verification is moot.

// CLI mode: apply canonical SQL seeds via C# (replaces sqlcmd-by-hand workflow).
//   ss --seed                     list known seeds
//   ss --seed <name>              apply one
//   ss --seed --all [--force]     apply every known seed in order
if (args.Contains("--seed"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await SeedCli.RunAsync(args, sp);
    return;
}

// CLI mode: (re)generate the node bible for an existing node.
//   ss --story-bible --slug <slug> [--beats N] [--replace-beats]
if (args.Contains("--story-bible"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await NodeBibleCli.RunAsync(args, sp);
    return;
}

// CLI mode: generate a new node (bible-first: plan → planned beats → expand in UI).
// CLI mode: autonomous corpus loop — generate N nodes end-to-end and review them.
//   ss --run-corpus --count N [--seed "..."] [--kind episode] [--beats 12] [--ballots 20] [--resume] [--dry-run]
if (args.Contains("--run-corpus"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await RunCorpusCli.RunAsync(args, sp);
    return;
}

// CLI mode: expand planned beats in a node to prose (headless ✨ for each beat).
//   ss --edit-beat --slug <slug> (--beat-number N | --insert-after N) --file <path>
if (args.Contains("--edit-beat"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await EditBeatCli.RunAsync(args, sp);
    return;
}

// CLI mode: create a new empty root node (bible-first; no beats yet).
//   ss --create-story --title "..." [--code SRZR] [--kind story] [--synopsis "..."] [--seed "..."] [--previous <slug|id>] [--parent <slug|id>]
if (args.Contains("--create-story"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await CreateNodeCli.RunAsync(args, sp);
    return;
}

//   ss --expand-beat (--slug <slug> | --id <guid>) [--beat <beatId>] [--force]
if (args.Contains("--expand-beat"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await ExpandBeatCli.RunAsync(args, sp);
    return;
}

//   ss --auto-run (--slug <slug> | --id <guid>) [--effort draft|standard] [--dry-run] [--force]
if (args.Contains("--auto-run"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await AutoRunCli.RunAsync(args, sp);
    return;
}

//   ss --write-node --seed "..." [--title "..."] [--kind episode] [--beats 12] [--bible-only]
if (args.Contains("--write-node"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await WriteNodeCli.RunAsync(args, sp);
    return;
}

// CLI mode: delete the 44 legacy book/chapter Entity+Records blobs whose
// content already lives in the Nodes/Beats model. Classifies each as JUNK,
// REDUNDANT, or ORPHAN (converts orphans to Nodes before deleting).
//   ss --migrate-legacy-book-chapter
if (args.Contains("--migrate-legacy-book-chapter"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await MigrateLegacyBookChapterCli.RunAsync(args, sp);
    return;
}

// CLI mode: migrate legacy Books/Chapters/ChapterBeats/Episodes/EpisodeBeats
// data into the unified Beat/Node schema. Idempotent — safe to re-run.
//   ss --migrate-nodes
if (args.Contains("--migrate-nodes"))
{
    var sp = BuildCoreServices(args);
    var svc = sp.GetRequiredService<NodeMigrationService>();
    var report = await svc.MigrateAllAsync();
    Console.WriteLine($"[migrate-nodes] Books={report.BooksAdded} Chapters={report.ChaptersAdded} Beats={report.BeatsAdded} Episodes={report.EpisodesAdded} Standalone={report.StandaloneBeatsAdded} Junctions={report.JunctionRowsAdded}");
    return;
}

// CLI mode: reconcile audio bytes between local disk and Azure Blob storage.
// Companion to DualWriteAudioStore — repairs drift from offline recordings
// and failed background uploads. Default (no --push/--pull args) is full
// bidirectional repair. See SyncAudioCli class doc for the full arg list.
//   ss --sync-audio [--push] [--pull] [--node SLUG] [--dry-run] [--verbose]
if (args.Contains("--sync-audio"))
{
    // Surface %APPDATA%\MindAttic\<bucket>\providers.json — AzureBlobAudioStore reads
    // AudioStore:ConnectionString straight from IConfiguration with no fallback.
    var sp = BuildServicesWithVault(args);
    Environment.ExitCode = await SyncAudioCli.RunAsync(args, sp);
    return;
}

// CLI mode: (re)narrate an EXISTING node by id (full or prefix) or slug.
// Runs the same NarrateAsync path the Record button uses. Use to re-record a
// node whose beats failed (e.g. a TTS 400) without regenerating prose.
//   ss --narrate-story (--id <guid|prefix> | --slug <slug>)
if (args.Contains("--narrate-story"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await NarrateNodeCli.RunAsync(args, sp);
    return;
}

// CLI mode: create a fixed, named reviewer panel of N personas, disjoint from
// every existing focus group (no persona on two panels). No LLM calls.
//   ss --make-group --name "Group B" [--size 128]
if (args.Contains("--make-group"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await MakeGroupCli.RunAsync(args, sp);
    return;
}

// CLI mode: run Legion persona quality voting across canon entity repos.
// Replaces the old LlmVoting (10 GLMZ residents) with the full 1000-persona library,
// 1-100 scale, and append-only EntityReview rows (same process as node reviews).
//   ss --review-entity [--type <type>] [--ballots N] [--prose N] [--unrated]
if (args.Contains("--review-entity"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await ReviewEntityCli.RunAsync(args, sp);
    return;
}

//   ss --link-weapon-ammo [--local-url URL] [--local-key KEY] [--local-model TAG] [--dry-run]
if (args.Contains("--link-weapon-ammo"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await LinkWeaponAmmoCli.RunAsync(args, sp);
    return;
}

//   ss --populate-queue --entity-review|--story-review|--beat-write|--status [options]
if (args.Contains("--populate-queue"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await StreetSamurai.Cli.PopulateQueueCli.RunAsync(args, sp);
    return;
}

//   ss --worker-mode --queue-url URL --worker-key KEY --worker-id ID --local-url LLM_URL [options]
if (args.Contains("--worker-mode"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await StreetSamurai.Cli.WorkerModeCli.RunAsync(args, sp);
    return;
}

// CLI mode: have N Legion personas each read an EXISTING node and write an
// honest, scored reader review (saved to NodeReviews), then synthesize the
// Amazon-style aggregate summary. Round-robins reviewers across the trusted-4.
//   ss --review-node (--id <guid|prefix> | --slug <slug>) [--readers N]
//   ss --review-story / --run-panel  (legacy aliases)
if (args.Contains("--review-node") || args.Contains("--review-story") || args.Contains("--run-panel"))
{
    var sp = BuildServicesWithVault(args);
    Environment.ExitCode = await ReviewNodeCli.RunAsync(args, sp);
    return;
}

// CLI mode: manage the rented vast.ai review box (key from the MindAttic vault, provider 'vast').
//   ss --gpu <status|stop|start|destroy> [--instance <id>]
if (args.Contains("--gpu"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await VastGpuCli.RunAsync(args, sp);
    return;
}

// CLI mode: manage the rented RunPod review pod (key from the MindAttic vault, provider 'runpod').
//   ss --runpod <status|stop|start|terminate> [--pod <id>]
if (args.Contains("--runpod"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await RunPodGpuCli.RunAsync(args, sp);
    return;
}

// CLI mode: (re)generate the portable per-voter report (JSON + filterable HTM) from
// a node's most recent stored review batch, without re-running the panel.
//   ss --review-report (--slug <slug> | --id <guid> | --code <CODE>) [--provider local|cloud|all]
if (args.Contains("--review-report"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await ReviewReportCli.RunAsync(args, sp);
    return;
}

// CLI mode: add an author ruling to the prose-lessons memory store.
// Lessons are injected into review ballot prompts so reviewers don't penalise
// beats the author has already ruled are doing their job.
//   ss --lesson-add --scope <scope> --kind <kind> --text "<text>"
//   Scope: global | node:<slug> | beat:<guid>
//   Kind:  score-vs-function | delight | voice | pacing | continuity | other
if (args.Contains("--lesson-add"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await ProseLessonCli.RunAddAsync(args, sp);
    return;
}

// CLI mode: list prose lessons (all scopes or filtered).
//   ss --lessons-list [--scope <scope>]
if (args.Contains("--lessons-list"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await ProseLessonCli.RunListAsync(args, sp);
    return;
}

// CLI mode: register feedback loop — surface top-N beats by EmotionalScore, identify
// which register law each exemplifies, and append as candidate entries to
// docs/registers/<NAME>.md. Closes the story→review→exemplar→register→prose loop.
//   ss --update-register-exemplars (--slug <slug> | --id <guid>) [--top N] [--dry-run]
if (args.Contains("--update-register-exemplars"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await UpdateRegisterExemplarsCli.RunAsync(args, sp);
    return;
}

// CLI mode: review-driven auto-editor. Weight the latest reviews, target the
// lowest / most-flagged beats (raise the floor), and emit conservative
// before/after rewrite PROPOSALS (JSON) for an approval survey. Nothing is written.
//   ss --edit-story (--id <guid|prefix> | --slug <slug>) [--top N]
if (args.Contains("--edit-story"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await EditNodeCli.RunAsync(args, sp);
    return;
}

// CLI mode: stitch an existing node's beats into one combined file (WAV →
// MP3), copy it to the publish output dir (Downloads by default), and record
// the publication run + process-event ledger. Headless Publish button.
//   ss --publish-story (--id <guid|prefix> | --slug <slug>)
if (args.Contains("--publish-story"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await PublishNodeCli.RunAsync(args, sp);
    return;
}

// CLI mode: seed Amazon KDP keywords for published nodes.
//   ss --seed-keywords [--slug <slug>]
if (args.Contains("--seed-keywords"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await SeedKeywordsCli.RunAsync(args, sp);
    return;
}

// CLI mode: render a node to a KDP-ready Word .docx in Downloads.
//   ss --publish-docx (--id <guid|prefix> | --slug <slug>) [--author "Name"]
if (args.Contains("--publish-docx"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await PublishDocxCli.RunAsync(args, sp);
    return;
}

// CLI mode: build an Audible AI-narration hand-off package for a node.
// Produces a narration-clean manuscript, pronunciation guide, and README.
//   ss --prepare-audible (--slug <slug> | --id <guid|prefix>) [--no-phonetics]
if (args.Contains("--prepare-audible"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await PrepareAudibleCli.RunAsync(args, sp);
    return;
}

// CLI mode: deterministic timeline-consistency check (RFC 0009 §5).
// Detects dead-character-acting and wound-regression violations. No LLM calls.
//   ss --timeline-check (--slug <slug> | --id <guid>)
if (args.Contains("--timeline-check"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await TimelineCheckCli.RunAsync(args, sp);
    return;
}

// CLI mode: set the ParentNodeId on an existing node (move it into a collection).
// X-Ray scene assembly (RFC 0002): print the entity roster + voice context block
// for a beat or raw prose. CLI twin of the MCP tool assemble_scene_context.
//   ss --assemble-scene (--beat <guid> | --text "<prose>") [--budget N]
if (args.Contains("--assemble-scene"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await AssembleSceneCli.RunAsync(args, sp);
    return;
}

//   ss --reparent-node (--slug <slug> | --id <id>) (--parent-slug <slug> | --parent-id <id>)
//   ss --reparent-node --slug <slug> --clear   — detach from parent
if (args.Contains("--reparent-node"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await ReparentNodeCli.RunAsync(args, sp);
    return;
}


// CLI mode: render the WHOLE node as one continuous audiobook (one TTS pass,
// tiered to ElevenLabs limits — one request, else per-chapter, else split) and
// drop the MP3 in Downloads. The headless twin of the "Export Audio" button.
//   ss --record | --export-audio | --export-mp3 | --publish-audiobook
//      (--id <guid|prefix> | --slug <slug>)
if (args.Contains("--publish-audiobook") || args.Contains("--record") || args.Contains("--export-audio") || args.Contains("--export-mp3"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await PublishAudiobookCli.RunAsync(args, sp);
    return;
}

// CLI mode: codify the GLMZ house voice + world rules from the memory rubric into
// the DB stores the generator reads (literary_rules / tone_bible). De-fragilizes
// the rules so they no longer depend on an .md file being parsed. Idempotent.
//   ss --seed-voice-rules
if (args.Contains("--seed-voice-rules"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await SeedVoiceRulesCli.RunAsync(args, sp);
    return;
}

// CLI mode: extract a time / elapsed-duration timeline from all beats in a node.
// Flags clock anchors, infers story-relative timestamps, and surfaces conflicts.
//   ss --timeline (--slug <slug> | --id <id>)
if (args.Contains("--timeline"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await TimelineCli.RunAsync(args, sp);
    return;
}

// CLI mode: per-entity-type reachability matrix (how much canon is embedded and
// thus pullable into prose). The standing gap-finder.
//   ss --coverage
if (args.Contains("--coverage"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await CoverageCli.RunAsync(args, sp);
    return;
}

// CLI mode: (re)build the materialized character read-model projection from the
// relational source of truth. Run after a bulk import / relational migration,
// or whenever ReadModelVersion is bumped. Backfills missing/stale rows, prunes
// orphans. The steady-state path self-heals, so this is a one-time / maintenance op.
//   ss --rebuild-readmodel [--archived]
if (args.Contains("--rebuild-readmodel"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await RebuildReadModelCli.RunAsync(args, sp);
    return;
}

// CLI mode: create a runtime-defined repository (custom entity type).
//   ss --create-repository --name "Artifacts" [--category World] [--icon bi-box] [--description "..."]
if (args.Contains("--create-repository"))
{
    string ArgVal(string flag) { var i = Array.IndexOf(args, flag); return i >= 0 && i + 1 < args.Length ? args[i + 1] : ""; }
    var repoName = ArgVal("--name");
    if (string.IsNullOrWhiteSpace(repoName)) { Console.Error.WriteLine("[create-repository] --name is required."); Environment.ExitCode = 1; return; }
    var sp = BuildCoreServices(args);
    var svc = sp.GetRequiredService<StreetSamurai.Core.Services.RepositoryDefinitionService>();
    try
    {
        var def = svc.Create(repoName, ArgVal("--category"), ArgVal("--icon"), ArgVal("--description"));
        Console.WriteLine($"[create-repository] Created '{def.Name}' — slug '{def.Slug}', category {def.Category}, route {def.RoutePath}.");
    }
    catch (Exception ex) { Console.Error.WriteLine($"[create-repository] FAILED: {ex.Message}"); Environment.ExitCode = 1; }
    return;
}

// CLI mode: backfill the Factions relational schema from Records.Json blobs.
// Run once after applying add_faction_relationship_tags_20260615.sql.
// ADDITIVE — Records.Json is never modified. (RFC 0007)
//   ss --rebuild-faction-relational
if (args.Contains("--rebuild-faction-relational"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await RebuildFactionRelationalCli.RunAsync(args, sp);
    return;
}

// CLI mode: materialize relational rows for active characters that are blob-only
// (no Characters row) — the no-data-loss gate before dropping the Character blob. (RFC 0007)
//   ss --backfill-missing-characters
if (args.Contains("--backfill-missing-characters"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await BackfillMissingCharactersCli.RunAsync(args, sp);
    return;
}

// CLI mode: backfill the Quotes relational schema from Records.Json blobs.
// ADDITIVE — Records.Json is never modified. (RFC 0007)
//   ss --rebuild-quote-relational
if (args.Contains("--rebuild-quote-relational"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await RebuildQuoteRelationalCli.RunAsync(args, sp);
    return;
}

// CLI mode: backfill the News relational schema from Records.Json blobs.
// ADDITIVE — Records.Json is never modified. (RFC 0007)
//   ss --rebuild-news-relational
if (args.Contains("--rebuild-news-relational"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await RebuildNewsRelationalCli.RunAsync(args, sp);
    return;
}

// CLI mode: backfill the Contracts relational schema from Records.Json blobs.
// ADDITIVE — Records.Json is never modified. (RFC 0007)
//   ss --rebuild-contract-relational
if (args.Contains("--rebuild-contract-relational"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await RebuildContractRelationalCli.RunAsync(args, sp);
    return;
}

// CLI mode: backfill the VocabularyEntries relational schema from Records.Json blobs.
// ADDITIVE — Records.Json is never modified. (RFC 0007)
//   ss --rebuild-vocabulary-relational
if (args.Contains("--rebuild-vocabulary-relational"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await RebuildVocabularyRelationalCli.RunAsync(args, sp);
    return;
}

// CLI mode: backfill the Archetypes relational schema from Records.Json blobs.
// ADDITIVE — Records.Json is never modified. (RFC 0007)
//   ss --rebuild-archetype-relational
if (args.Contains("--rebuild-archetype-relational"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await RebuildArchetypeRelationalCli.RunAsync(args, sp);
    return;
}

// CLI mode: backfill the Genemods relational schema from Records.Json blobs.
// ADDITIVE — Records.Json is never modified. (RFC 0007)
//   ss --rebuild-genemod-relational
if (args.Contains("--rebuild-genemod-relational"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await RebuildGenemodRelationalCli.RunAsync(args, sp);
    return;
}

// CLI mode: backfill the Materials relational schema from Records.Json blobs.
// ADDITIVE — Records.Json is never modified. (RFC 0007)
//   ss --rebuild-material-relational
if (args.Contains("--rebuild-material-relational"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await RebuildMaterialRelationalCli.RunAsync(args, sp);
    return;
}

// CLI mode: backfill the Psionics relational schema from Records.Json blobs.
// ADDITIVE — Records.Json is never modified. (RFC 0007)
//   ss --rebuild-psionic-relational
if (args.Contains("--rebuild-psionic-relational"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await RebuildPsionicRelationalCli.RunAsync(args, sp);
    return;
}

// CLI mode: backfill the Motifs relational schema from Records.Json blobs.
// ADDITIVE — Records.Json is never modified. (RFC 0007)
//   ss --rebuild-motif-relational
if (args.Contains("--rebuild-motif-relational"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await RebuildMotifRelationalCli.RunAsync(args, sp);
    return;
}

// CLI mode: backfill the LabSpecimens relational schema from Records.Json blobs.
// ADDITIVE — Records.Json is never modified. (RFC 0007)
//   ss --rebuild-lab-specimen-relational
if (args.Contains("--rebuild-lab-specimen-relational"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await RebuildLabSpecimenRelationalCli.RunAsync(args, sp);
    return;
}

// CLI mode: backfill the FlyoverEntities relational schema from Records.Json blobs.
// ADDITIVE — Records.Json is never modified. (RFC 0007)
//   ss --rebuild-flyover-entity-relational
if (args.Contains("--rebuild-flyover-entity-relational"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await RebuildFlyoverEntityRelationalCli.RunAsync(args, sp);
    return;
}

// CLI mode: backfill the Automata relational schema from Records.Json blobs.
// ADDITIVE — Records.Json is never modified. (RFC 0007)
//   ss --rebuild-automaton-relational
if (args.Contains("--rebuild-automaton-relational"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await RebuildAutomatonRelationalCli.RunAsync(args, sp);
    return;
}

// CLI mode: backfill the Ammunitions relational schema from Records.Json blobs.
// ADDITIVE — Records.Json is never modified. (RFC 0007)
//   ss --rebuild-ammunition-relational
if (args.Contains("--rebuild-ammunition-relational"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await RebuildAmmunitionRelationalCli.RunAsync(args, sp);
    return;
}

// CLI mode: backfill the Transportations relational schema from Records.Json blobs.
// ADDITIVE — Records.Json is never modified. (RFC 0007)
//   ss --rebuild-transportation-relational
if (args.Contains("--rebuild-transportation-relational"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await RebuildTransportationRelationalCli.RunAsync(args, sp);
    return;
}

// CLI mode: backfill the Corponations relational schema from Records.Json blobs.
// ADDITIVE — Records.Json is never modified. (RFC 0007)
//   ss --rebuild-corponation-relational
if (args.Contains("--rebuild-corponation-relational"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await RebuildCorponationRelationalCli.RunAsync(args, sp);
    return;
}

// CLI mode: backfill the EquipmentItems relational schema from Records.Json blobs.
// ADDITIVE — Records.Json is never modified. (RFC 0007)
//   ss --rebuild-equipment-relational
if (args.Contains("--rebuild-equipment-relational"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await RebuildEquipmentRelationalCli.RunAsync(args, sp);
    return;
}

// CLI mode: backfill the Technologies relational schema from Records.Json blobs.
// ADDITIVE — Records.Json is never modified. (RFC 0007)
//   ss --rebuild-technology-relational
if (args.Contains("--rebuild-technology-relational"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await RebuildTechnologyRelationalCli.RunAsync(args, sp);
    return;
}

// CLI mode: backfill the Pharmaceuticals relational schema from Records.Json blobs.
// ADDITIVE — Records.Json is never modified. (RFC 0007)
//   ss --rebuild-pharmaceutical-relational
if (args.Contains("--rebuild-pharmaceutical-relational"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await RebuildPharmaceuticalRelationalCli.RunAsync(args, sp);
    return;
}

// CLI mode: backfill the CyberwareItems relational schema from Records.Json blobs.
// ADDITIVE — Records.Json is never modified. (RFC 0007)
//   ss --rebuild-cyberware-relational
if (args.Contains("--rebuild-cyberware-relational"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await RebuildCyberwareRelationalCli.RunAsync(args, sp);
    return;
}

// CLI mode: backfill the ConsumerGoods relational schema from Records.Json blobs.
// ADDITIVE — Records.Json is never modified. (RFC 0007)
//   ss --rebuild-consumer-good-relational
if (args.Contains("--rebuild-consumer-good-relational"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await RebuildConsumerGoodRelationalCli.RunAsync(args, sp);
    return;
}

// CLI mode: backfill the SyntheticLives relational schema from Records.Json blobs.
// ADDITIVE — Records.Json is never modified. (RFC 0007)
//   ss --rebuild-synthetic-relational
if (args.Contains("--rebuild-synthetic-relational"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await RebuildSyntheticRelationalCli.RunAsync(args, sp);
    return;
}

// CLI mode: backfill the Places relational schema from Records.Json blobs.
// ADDITIVE — Records.Json is never modified. (RFC 0007)
//   ss --rebuild-place-relational
if (args.Contains("--rebuild-place-relational"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await RebuildPlaceRelationalCli.RunAsync(args, sp);
    return;
}

// CLI mode: backfill the Documents relational schema from Records.Json blobs.
// ADDITIVE — Records.Json is never modified. (RFC 0007)
//   ss --rebuild-document-relational
if (args.Contains("--rebuild-document-relational"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await RebuildDocumentRelationalCli.RunAsync(args, sp);
    return;
}

// CLI mode: backfill the EntertainmentItems relational schema from Records.Json blobs.
// ADDITIVE — Records.Json is never modified. (RFC 0007)
//   ss --rebuild-entertainment-relational
if (args.Contains("--rebuild-entertainment-relational"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await RebuildEntertainmentRelationalCli.RunAsync(args, sp);
    return;
}

// CLI mode: backfill the Weapons relational schema from Records.Json blobs.
// ADDITIVE — Records.Json is never modified. (RFC 0007)
//   ss --rebuild-weapon-relational
if (args.Contains("--rebuild-weapon-relational"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await RebuildWeaponRelationalCli.RunAsync(args, sp);
    return;
}

// CLI mode: backfill the Apparels relational schema from Records.Json blobs.
// ADDITIVE — Records.Json is never modified. (RFC 0007)
//   ss --rebuild-apparel-relational
if (args.Contains("--rebuild-apparel-relational"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await RebuildApparelRelationalCli.RunAsync(args, sp);
    return;
}

// CLI mode: backfill the Subsidiaries relational schema from Records.Json blobs.
// ADDITIVE — Records.Json is never modified. (RFC 0007)
//   ss --rebuild-subsidiary-relational
if (args.Contains("--rebuild-subsidiary-relational"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await RebuildSubsidiaryRelationalCli.RunAsync(args, sp);
    return;
}

// CLI mode: RFC 0007 unified blob-retirement gate — backfill all 29 relational types
// from Records.Json, validate, and delete the blobs in a single pass. (RFC 0007)
//   ss --retire-records-blobs [--rebuild] [--validate] [--apply]
if (args.Contains("--retire-records-blobs"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await RetireRecordsBlobsCli.RunAsync(args, sp);
    return;
}

// CLI mode: split a monolithic node into a Collection (parent + chapter
// child nodes) at IsChapterStart boundaries. Backs up to markdown first.
//   ss --split-collection (--slug <s> | --id <guid>)
if (args.Contains("--split-collection"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await SplitCollectionCli.RunAsync(args, sp);
    return;
}

// CLI mode: print the voice context the generator/re-beater receive — the
// verification that the canon-trained voice is wired into prompts.
//   ss --print-voice
if (args.Contains("--print-voice"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await PrintVoiceCli.RunAsync(args, sp);
    return;
}

// CLI mode: print all beats of a node as continuous prose to stdout.
// No headers, no beat numbers, no metadata — just the prose, beats separated by blank lines.
//   ss --sanitize-beats [--slug <slug> | --all] [--dry-run]
if (args.Contains("--sanitize-beats"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await SanitizeBeatsCli.RunAsync(args, sp);
    return;
}

//   ss --print-story (--id <guid|prefix> | --slug <slug>)
if (args.Contains("--print-story"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await PrintNodeCli.RunAsync(args, sp);
    return;
}

// CLI mode: rebuild a node's beats to the codified beat doctrine via LLM
// re-segmentation (story beats + dialogue/'?' mechanics + gaps). Dry-run by
// default; --apply backs up to markdown then replaces beats if the word-retention
// guard passes. --all targets every doctrine-violating node.
//   ss --rebeat-story (--slug <s> | --id <guid> | --all) [--apply]
if (args.Contains("--rebeat-story"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await RebeatNodeCli.RunAsync(args, sp);
    return;
}

// CLI mode: sweep a node's prose against canon (all entity types) and queue
// contradictions as approval-gated findings — the self-correction pass.
//   ss --check-canon (--slug <s> | --id <guid> | --all)
if (args.Contains("--check-canon"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await CheckCanonCli.RunAsync(args, sp);
    return;
}

// CLI mode: show what the universal canon reach pulls for a query, across ALL
// entity types — verifies the full-interconnect retrieval path.
//   ss --canon-retrieve "<query>" [--k N] [--types t1,t2]
if (args.Contains("--canon-retrieve"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await CanonRetrieveCli.RunAsync(args, sp);
    return;
}

// CLI mode: author-only Canon trust gate — mark a node strong enough to draw
// conclusions about its characters/events (the voice-harvest learns from canon).
//   ss --mark-canon (--slug <s> | --id <guid>) [--off]
if (args.Contains("--mark-canon"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await MarkCanonCli.RunAsync(args, sp);
    return;
}

// CLI mode: distill voice rules from winning (≥80%) nodes into the codified
// DB-backed rules the generator reads. Propose-then-approve.
//   ss --harvest-voice (--slug <s> | --id <id> | --all-80 | --pending | --apply <guid> | --reject <guid>) [--force]
if (args.Contains("--harvest-voice"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await HarvestVoiceCli.RunAsync(args, sp);
    return;
}

// CLI mode: list every node as a table (or JSON). Headless twin of /nodes.
//   ss --list-stories [--status <s>] [--kind <k>] [--search <text>] [--limit <n>] [--json]
if (args.Contains("--list-stories"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await ListNodesCli.RunAsync(args, sp);
    return;
}

//   ss --kdp-status
//   Show KDP publication status: Published / Outdated / WorkInProgress for all tracked nodes.
//   Outdated = published but beats edited since last KDP push.
if (args.Contains("--kdp-status"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await KdpStatusCli.RunAsync(args, sp);
    return;
}

// CLI mode: render a node to Markdown or PDF in Downloads.
// Markdown output embeds <!-- beat:N:id7 --> markers for ss --import-md round-trip.
//   ss (--publish-md | --publish-pdf) (--id <guid|prefix> | --slug <slug>) [--author "Name"]
if (args.Contains("--publish-md") || args.Contains("--publish-pdf"))
{
    var sp = BuildCoreServices(args);
    var format = args.Contains("--publish-md") ? PublishManuscriptCli.Format.Markdown
               : PublishManuscriptCli.Format.Pdf;
    Environment.ExitCode = await PublishManuscriptCli.RunAsync(args, sp, format);
    return;
}

// CLI mode: reimport an edited --publish-md Markdown file back into the DB. Each
// <!-- beat:N:id7 --> marker identifies the beat; prose between markers updates Beat.Text.
//   ss --import-md --file path.md [--dry-run]
if (args.Contains("--import-md"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await ImportMarkdownCli.RunAsync(args, sp);
    return;
}

// CLI mode: bounded copy-edit of a node — proper paragraph/dialogue spacing, a
// "?" on questions that lack one, and "asks"/"asked" (not "says") on question
// dialogue. Dry-run by default; --apply commits. Beats edited beyond those bounds
// are rejected (word-token guard) and left untouched.
//   ss --reflow-story (--id <guid|prefix> | --slug <slug>) [--apply]
if (args.Contains("--reflow-story"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await ReflowNodeCli.RunAsync(args, sp);
    return;
}

// CLI mode: deep-duplicate a node (and its sub-node tree) into a fresh,
// independent copy — every beat cloned to a new row (prose + metadata kept;
// audio/score/stale reset). Editing the copy never touches the original.
//   ss --duplicate-story (--id <guid|prefix> | --slug <slug>) --title "New Title"
if (args.Contains("--duplicate-story"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await DuplicateNodeCli.RunAsync(args, sp);
    return;
}

// CLI mode: import a hand-authored .node file (beat + gap + beat …) into a
// fresh node. The complement to --write-story (LLM-generated): this is for
// drafts written elsewhere (chat exports, transcripts, paper notes typed up).
// See ImportNodeCli class doc for the file format.
//   ss --import-story --file path.node [--title ...] [--kind ...] [--slug ...] [--parent ...] [--dry-run]
if (args.Contains("--import-story"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await ImportNodeCli.RunAsync(args, sp);
    return;
}

// CLI mode: import a local image file (png, jpg, webp) into the Media table.
// Optionally links to a node by --story-code and sets the media type.
//   ss --import-cover --file PATH [--story-code CODE] [--type TYPE] [--notes TEXT] [--dry-run]
if (args.Contains("--import-cover"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await ImportCoverImageCli.RunAsync(args, sp);
    return;
}


// CLI mode: burst oversized beats (e.g. chapter-as-one-beat from old book
// imports) into paragraph-sized pieces. Idempotent — already-small beats
// are skipped on rerun.
//   ss --burst-beats [--min-chars 800] [--node slug] [--kind book] [--dry-run]
if (args.Contains("--burst-beats"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await BurstBeatsCli.RunAsync(args, sp);
    return;
}

// CLI mode: report flat-vs-bridge drift for a denormalised column.
//   ss --audit-denorm Entities.TagsJson
//   ss --audit-denorm Characters.Affiliation
if (args.Contains("--audit-denorm"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await AuditDenormCli.RunAsync(args, sp);
    return;
}

// CLI mode: findings inbox — list / show / apply / dismiss / scan.
if (args.Contains("--findings"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await FindingsCli.RunAsync(args, sp);
    return;
}

// ss --entity-tree (--id <guid> | --slug <slug>) [--depth N] [--rel-types type1,type2] [--as-of date]
if (args.Contains("--entity-tree"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await EntityTreeCli.RunAsync(args, sp);
    return;
}

// ss --prose-check (--slug <nodeSlug> | --id <beatId>) [--all] [--json]
if (args.Contains("--prose-check"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await ProseCheckCli.RunAsync(args, sp);
    return;
}

// ss --prose-health [--slug <nodeSlug>] [--json] [--out <dir>]
// Zero-cost overnight health scan: surface stats + kNN score prediction +
// semantic outlier detection using cached ProseEmbeddings. No API calls.
if (args.Contains("--prose-health"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await ProseHealthCli.RunAsync(args, sp);
    return;
}

// ss --check-fidelity (--slug <nodeSlug> | --id <nodeId>) [--json]
// Detects the Semantic Fidelity Gap — beats scoring high but drifting from the
// story's original meaning (Goodhart's Law in prose). Two checks:
//   Bible alignment: prose vs Seed/Synopsis (north-star drift)
//   Intent alignment: prose vs beat Synopsis (purpose drift)
// Files SEMANTIC-DRIFT findings; also runs automatically after every review.
if (args.Contains("--check-fidelity"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await CheckFidelityCli.RunAsync(args, sp);
    return;
}

// ss --world-state --beat <beatId> [--story-time "date"] [--json]
if (args.Contains("--world-state"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await WorldStateCli.RunAsync(args, sp);
    return;
}

// ss --gear-check --slug <nodeSlug> --character <characterId> [--story-time date]
if (args.Contains("--gear-check"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await GearCheckCli.RunAsync(args, sp);
    return;
}

// ss --score-trend [--batches N] [--universe <slug>]
// Print rolling mean score across N chronological batches of scored nodes.
// Positive Δ confirms the voice-harvest flywheel is spinning forward (SS-US-J6).
// Exit 0 = positive trend, 1 = flat/declining, 2 = not enough data.
if (args.Contains("--score-trend"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await ScoreTrendCli.RunAsync(args, sp);
    return;
}

// ss --write-outline --slug <nodeSlug> [--json] [--skip-audit]
// Generates a beat-by-beat narrative outline (act-grouped, one sentence per beat)
// and runs an adversarial logic audit: plot holes, canon violations, impossible actions,
// causality breaks, prop errors, contradictions. Use --skip-audit for outline only.
// Exit 0 = no issues, 1 = minor/major findings, 2 = critical findings.
if (args.Contains("--write-outline"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await WriteOutlineCli.RunAsync(args, sp);
    return;
}

// ss --diagnose-story --slug <nodeSlug> [--json]
// Pre-flight structural analysis before running the review panel.
// Runs 12 targeted checks (antagonist cost, protagonist behavior change,
// exposition density, etc.) and reports Pass/Warn/Fail with evidence + fixes.
// Exit 0 = ready, 1 = warnings, 2 = blocking failures.
if (args.Contains("--diagnose-story"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await DiagnoseNodeCli.RunAsync(args, sp);
    return;
}

// ss --examine-emotion --slug <nodeSlug> [--effort draft|standard|deep] [--json]
// Emotional Intelligence Examination (SS-A15): 8-dimension 0–4 rubric, per-beat curve,
// character ledger (Want/Need/Wound/Flaw), register-adaptive anchors.
// Exit 0 = none blocking, 1 = advisory issues, 2 = blocking dimensions open.
if (args.Contains("--examine-emotion"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await ExamineEmotionCli.RunAsync(args, sp);
    return;
}

// ss --causality-check / --affect-check / --interpersonal-check --slug <slug> [--json]
// "Behave like people" beat lenses: cause-effect (kill "and then"), emotion→action,
// and verbal+non-verbal interpersonal dynamics (the 90+ relational lever).
if (args.Contains("--causality-check") || args.Contains("--affect-check") || args.Contains("--interpersonal-check"))
{
    var lens = args.Contains("--causality-check") ? "causality"
             : args.Contains("--affect-check") ? "affect" : "interpersonal";
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await BeatLensCli.RunAsync(args, sp, lens);
    return;
}

// ss --list-species — print the species taxonomy (canonical name, label, sentience).
if (args.Contains("--list-species"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = ListSpeciesCli.Run(sp);
    return;
}

// ss --behavior-check --slug <nodeSlug> --character <characterId>
if (args.Contains("--behavior-check"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await BehaviorCheckCli.RunAsync(args, sp);
    return;
}

// ss --weapon-network (--id <weaponId> | --character <characterId> [--as-of date])
if (args.Contains("--weapon-network"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await WeaponNetworkCli.RunAsync(args, sp);
    return;
}

// ss --ambient-palette --character <characterId> [--as-of date]
if (args.Contains("--ambient-palette"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await AmbientPaletteCli.RunAsync(args, sp);
    return;
}

// ss --seed-sensory-hints [--list] [--weapon "Name" --hints "hint1; hint2"] [--force]
if (args.Contains("--seed-sensory-hints"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await SeedSensoryHintsCli.RunAsync(args, sp);
    return;
}

// ss --beat <subcommand> — fine-grained beat manipulation:
//   insert  --node <slug|id> [--after <beatId>] [--text "..."]
//   delete  --id <beatId> [--node <slug|id>]
//   update  --id <beatId> --text "..."  (use '-' for stdin)
//   meta    --id <beatId> [--title "..."] [--kind "..."] [--synopsis "..."] [--tone "..."] ...
//   show    --id <beatId>
//   list    --node <slug|id>
if (args.Contains("--beat"))
{
    var sp = BuildCoreServices(args);
    var beatArgs = args.SkipWhile(a => a != "--beat").Skip(1).ToArray();
    Environment.ExitCode = await BeatCli.RunAsync(beatArgs, sp);
    return;
}

// ss --wound <subcommand> — character wound ledger:
//   list    --character <id|name> [--as-of "date"]
//   log     --character <id|name> --description "..." [--location "chest"] [--severity moderate] ...
//   status  --wound <id> --status active|healed|noted
if (args.Contains("--wound"))
{
    var sp = BuildCoreServices(args);
    var woundArgs = args.SkipWhile(a => a != "--wound").Skip(1).ToArray();
    Environment.ExitCode = await WoundCli.RunAsync(woundArgs, sp);
    return;
}

// ss --universe <subcommand> — universe management:
//   list      Print all universes
//   current   Print the active universe
//   use       --slug <slug> | --id <guid>
if (args.Contains("--universe"))
{
    var sp = BuildCoreServices(args);
    var uniArgs = args.SkipWhile(a => a != "--universe").Skip(1).ToArray();
    Environment.ExitCode = await UniverseCli.RunAsync(uniArgs, sp);
    return;
}

// ss --review-settings [--set <key> <value>] — view or update review voting settings.
// Keys: ballots, prose, panel, readers, max-concurrency, judge-provider, allowed-providers
if (args.Contains("--review-settings"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await ReviewSettingsCli.RunAsync(args, sp);
    return;
}

// ss --get <type> <name-or-id> — targeted entity lookup.
// Types: character | place | weapon | faction | corponation
if (args.Contains("--get"))
{
    var sp = BuildCoreServices(args);
    var getArgs = args.SkipWhile(a => a != "--get").Skip(1).ToArray();
    Environment.ExitCode = await GetEntityCli.RunAsync(getArgs, sp);
    return;
}

// CLI mode: sync project-rule, Codex, and Claude Code memory .md files to DB.
// Upserts by RelativePath; only changed files (hash diff) produce a history row.
//   ss --sync-markdown [--dry-run]
if (args.Contains("--sync-markdown"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await SyncMarkdownCli.RunAsync(args, sp);
    return;
}

// CLI mode: two-way sync between synopsis.txt in each node's publish folder
// and Nodes.Synopsis in the DB. File wins on conflict.
//   ss --sync-synopsis [--slug <slug>] [--dry-run]
if (args.Contains("--sync-synopsis"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await SyncSynopsisCli.RunAsync(args, sp);
    return;
}

// CLI mode: restore .md files from DB back to disk. Supports point-in-time
// recovery from the MarkdownFiles_History temporal table.
//   ss --restore-markdown [--file <relativePath>] [--as-of <datetime-utc>] [--dry-run] [--list]
if (args.Contains("--restore-markdown"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await RestoreMarkdownCli.RunAsync(args, sp);
    return;
}

// CLI mode: keyword recall — call up (print) or create (--to-disk) the select few
// tracked .md files relevant to a topic, straight from the DB.
//   ss --recall <keyword> [--content] [--to-disk] [--as-of <datetime-utc>]
if (args.Contains("--recall"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await RecallMarkdownCli.RunAsync(args, sp);
    return;
}

// CLI mode: Doc Context Stack dry-run — print the rotating cast of .md docs that WOULD
// load for a node + optional scene text (tier, reason, score, budget). Read-only.
//   ss --doc-context --slug <node> [--goal "<text>"] [--budget <tokens>]
if (args.Contains("--doc-context-hook"))
{
    // UserPromptSubmit hook backend — stdout must contain ONLY the hook JSON, so kill logging.
    var sp = BuildCoreServicesNoLogging(args);
    Environment.ExitCode = await DocContextHookCli.RunAsync(args, sp);
    return;
}

// REMOVED 2026-06-24: `--refactor-telemetry` (bulk regenerate-beats-from-synopsis runner).
// It rebuilt finished beats from their one-line goals, discarding hand-crafted prose — proven
// to regress finished nodes (dual-read: surgical 80.8 > baseline 79.7 > regen 76.2). Doc/Entity
// context were validated separately and are KEPT; regen-from-synopsis is not a revision tool and
// is gone. New-beat generation lives in ProseWriterRouter.WriteAsync, untouched.

// CLI mode: dual-read comparative review — the SAME pinned panel grades both versions of a story;
// pairs scores per reader (within-reader delta cancels taste bias) → keep/revert/merge verdict.
//   ss --dual-read --old <slug|id> --new <slug|id> [--panel <name>] [--readers N]
if (args.Contains("--dual-read"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await DualReadCli.RunAsync(args, sp);
    return;
}

if (args.Contains("--doc-context"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await DocContextCli.RunAsync(args, sp);
    return;
}

// ss --workflow-status [--slug <slug> | --all] [--json]
// Per-node or global prose service coverage matrix. Shows which services
// (Pacing, StoryMethodology, PlantPayoff, StoryAudit, Combat) were active
// when beats were written, and surfaces gaps where applicable services weren't used.
if (args.Contains("--workflow-status"))
{
    var sp = BuildCoreServices(args);
    await WorkflowMonitorCli.RunAsync(sp, args);
    return;
}

// ss --backfill-coverage --slug <book-or-chapter-slug>
// Populates BeatServiceLog + BeatModeLog for prose written before ProseWriterRouter
// existed, WITHOUT regenerating any beat. Runs the router's coverage-only path over
// each existing beat so --workflow-status has real logs to report.
if (args.Contains("--backfill-coverage"))
{
    var sp = BuildCoreServices(args);
    await BackfillCoverageCli.RunAsync(sp, args);
    return;
}

// ss --backfill-synopses --slug <s> [--model <id>] [--force]
// ss --backfill-structure-roles --slug <s> [--force]
// Fill missing beat metadata without touching prose. Synopses via LLM (BeatGoal proxy
// for mode detection); StructureRole deterministically by book-global Save-the-Cat arc.
if (args.Contains("--backfill-synopses") || args.Contains("--backfill-structure-roles"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await BackfillBeatMetaCli.RunAsync(sp, args);
    return;
}

// ss --audit-story --slug <book-or-chapter-slug> [--deep] [--model <id>] [--out <path>]
// The "Player Piano" — one repeatable command running the full QA battery (census +
// coverage + plant/prose audits; --deep adds per-chapter examine-emotion + story-audit +
// diagnose + fidelity). --model retargets the deep tier (e.g. Haiku) for the run.
if (args.Contains("--audit-story"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await AuditNodeCli.RunAsync(sp, args);
    return;
}

// ss --story-audit --slug <nodeSlug> [--json]
// Audits a node against 7 commandments — gateway (PreviousNodeId=null) or
// sequel (PreviousNodeId set). Pass/warn/fail per commandment with fix hints.
// Exit 0 = all pass, 1 = advisory warnings, 2 = blocking failures.
if (args.Contains("--story-audit"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await StoryAuditCli.RunAsync(args, sp);
    return;
}

// ss --ml-audit [--slug <nodeSlug>] [--all] [--skip-gripes] [--json]
// Runs the Python ML beat auditor against the trained nightly model.
// Writes ML-PROSE-SCORE findings to the Findings table for weak beats.
// Prerequisites: v3/ml/.venv set up + at least one nightly run completed.
// Exit 0 = clean, 1 = advisory (>=1 Low finding), 2 = blocking (>=1 High finding).
if (args.Contains("--ml-audit"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await MlAuditCli.RunAsync(args, sp);
    return;
}

// ss --export-personas-json [--out <path>]
// Exports all 1024 Legion persona details + OCEAN psychometric profiles to JSON
// for consumption by the Python ML package (v3/ml/artifacts/personas.json).
if (args.Contains("--export-personas-json"))
{
    var outPath = args.SkipWhile(a => a != "--out").Skip(1).FirstOrDefault()
        ?? Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,
            "..", "..", "..", "..", "..", "ml", "artifacts", "personas.json"));
    Directory.CreateDirectory(Path.GetDirectoryName(outPath)!);
    var personas = MindAttic.Legion.PersonaLibrary.AllDetails
        .Select(p =>
        {
            var profile = MindAttic.Legion.PersonaLibrary.Profiles.TryGetValue(p.Id, out var pr) ? pr : null;
            var ocean   = profile?.Ocean;
            return new
            {
                p.Id, p.Archetype, p.Worldview, p.Background, p.Age, p.Quirk,
                Ocean = ocean == null ? null : new
                {
                    ocean.Openness, ocean.Conscientiousness, ocean.Extraversion,
                    ocean.Agreeableness, ocean.Neuroticism,
                },
            };
        });
    var json = System.Text.Json.JsonSerializer.Serialize(
        personas.ToList(),
        new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
    await File.WriteAllTextAsync(outPath, json);
    Console.WriteLine($"Exported {MindAttic.Legion.PersonaLibrary.AllDetails.Count()} personas to {outPath}");
    return;
}

// ss --sanity-scan (--slug <slug|code> | --all) [--json]
// Deterministic prose checks — no LLM. Catches leaked internal node codes,
// undefined all-caps acronyms, encoding corruption, and heft-floor violations.
// Exit 0 = clean, 1 = warnings only, 2 = any blocks.
if (args.Contains("--sanity-scan"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await SanityScanCli.RunAsync(args, sp);
    return;
}

// ss --plant-audit   --slug <node> [--json]   audit plant/payoff pairs
// ss --list-plants   --slug <node> [--json]   list all pairs
// ss --add-plant     --slug <node> --plant "..." --payoff "..." [--cat detail]
if (args.Contains("--plant-audit") || args.Contains("--list-plants") || args.Contains("--add-plant"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await PlantPayoffCli.RunAsync(args, sp);
    return;
}

// CLI mode: Will Storr narrative-science frameworks — sacred flaw, dramatic question,
// scene anatomy, five-act structure. Four subcommands:
//   ss --narrative-science sacred-flaw --character <slug|id> [--scaffold]
//   ss --narrative-science dramatic-question (--slug <s> | --id <beatId>) [--character <slug|id>]
//   ss --narrative-science scene-anatomy (--slug <s> | --id <beatId>)
//   ss --narrative-science five-act --slug <nodeSlug>
//   (add --json to any subcommand for raw JSON output)
if (args.Contains("--narrative-science"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await NarrativeScienceCli.RunAsync(args, sp);
    return;
}

// ss --clone-story (--id <guid> | --slug <slug>) [--title "New Title"] [--story-code SM1] [--draft] [--status ready]
if (args.Contains("--clone-story"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await CloneNodeCli.RunAsync(args, sp);
    return;
}


// ────────────────────────────────────────────────────────────────────────────
// DI bootstrap helpers — replace WebApplication.CreateBuilder in every CLI block.
// Host.CreateDefaultBuilder loads appsettings.json + env vars but starts no
// HTTP server, no Kestrel, no Blazor middleware.
// ────────────────────────────────────────────────────────────────────────────

static IServiceProvider BuildCoreServices(string[] args)
    => Host.CreateDefaultBuilder(args)
        .ConfigureLogging(lb => lb.AddConsole())
        .ConfigureServices((_, svc) => svc.AddStreetSamuraiServices())
        .Build()
        .Services;

static IServiceProvider BuildCoreServicesNoLogging(string[] args)
    => Host.CreateDefaultBuilder(args)
        .ConfigureLogging(lb => lb.ClearProviders())
        .ConfigureServices((_, svc) => svc.AddStreetSamuraiServices())
        .Build()
        .Services;

static IServiceProvider BuildServicesWithVault(string[] args)
    => Host.CreateDefaultBuilder(args)
        .ConfigureAppConfiguration(cfg =>
            cfg.AddMindAtticVaultFiles())
        .ConfigureLogging(lb => lb.AddConsole())
        .ConfigureServices((ctx, svc) =>
        {
            SettingsService.VaultConfiguration = ctx.Configuration;
            svc.AddStreetSamuraiServices();
        })
        .Build()
        .Services;

static IServiceProvider BuildServicesWithVaultAndAuth(string[] args)
{
    var host = Host.CreateDefaultBuilder(args)
        .ConfigureAppConfiguration(cfg =>
            cfg.AddMindAtticVaultFiles(o => o.Buckets = new[]
                { "LLM", "Brokers", "Tokens", "Subtitles", "Notifications", "AudioStore", "Security" }))
        .ConfigureLogging(lb => lb.AddConsole())
        .ConfigureServices((ctx, svc) =>
        {
            SettingsService.VaultConfiguration = ctx.Configuration;
            svc.AddStreetSamuraiServices();
            svc.AddMindAtticAuthentication<StreetSamuraiAuthDbContext>(
                ctx.Configuration,
                o =>
                {
                    o.AppName = "StreetSamurai";
                    o.IsProduction = !string.Equals(
                        ctx.HostingEnvironment.EnvironmentName, "Development",
                        StringComparison.OrdinalIgnoreCase);
                });
        })
        .Build();
    return host.Services;
}