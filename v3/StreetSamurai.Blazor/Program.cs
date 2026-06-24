using System.Net;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using MindAttic.Authentication;
using MindAttic.Authentication.Web;
using StreetSamurai.Core.Data;
using Serilog;
using Serilog.Events;
using StreetSamurai.Blazor.Cli;
using StreetSamurai.Blazor.Components;
using StreetSamurai.Blazor.Services;
using StreetSamurai.Core.Extensions;
using StreetSamurai.Core.Interfaces;
using StreetSamurai.Core.Models;
using StreetSamurai.Core.Services;
using StreetSamurai.Shared.Services;
using MindAttic.Vault.Configuration;
using MindAttic.Vault.DependencyInjection;

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
    var cliBuilder = WebApplication.CreateBuilder(args);
    cliBuilder.Services.AddStreetSamuraiServices();
    var cliApp = cliBuilder.Build();
    // Pin the universe scope BEFORE building so it can't shift mid-rebuild. Resolving the context
    // forces its lazy catalog load + applies the --universe/SS_UNIVERSE/default selection, so every
    // builder in this rebuild sees one stable scope (the non-deterministic node/edge counts came
    // from the scope resolving partway through the multi-builder pass). Defaults to GLMZ.
    var cliUniverse = cliApp.Services.GetRequiredService<StreetSamurai.Core.Services.IUniverseContext>();
    Console.WriteLine($"[rebuild-graph] Universe scope: {cliUniverse.CurrentSlug} ({cliUniverse.CurrentId})");
    var graph = cliApp.Services.GetRequiredService<WorldGraphService>();
    Console.WriteLine("[rebuild-graph] Rebuilding world graph from source data...");
    graph.Rebuild();
    Console.WriteLine($"[rebuild-graph] Done: {graph.NodeCount} nodes, {graph.EdgeCount} edges saved to world_graph.json");
    return;
}

// CLI mode: ss --reset-password --email <e> --password <p> [--require-change]
// Operator password reset over the MindAttic.Authentication store, no web server.
if (args.Contains("--reset-password"))
{
    var cliBuilder = WebApplication.CreateBuilder(args);
    // Security bucket surfaces the Argon2id pepper from Vault so the hash the
    // reset writes is verifiable by the login flow (same as the live host).
    cliBuilder.Configuration.AddMindAtticVaultFiles(o => o.Buckets = new[]
        { "LLM", "Brokers", "Tokens", "Subtitles", "Notifications", "AudioStore", "Security" });
    cliBuilder.Services.AddStreetSamuraiServices();
    cliBuilder.Services.AddMindAtticAuthentication<StreetSamuraiAuthDbContext>(
        cliBuilder.Configuration,
        o => { o.AppName = "StreetSamurai"; o.IsProduction = !cliBuilder.Environment.IsDevelopment(); });
    var cliApp = cliBuilder.Build();
    Environment.ExitCode = await ResetPasswordCli.RunAsync(args, cliApp.Services);
    return;
}

// CLI mode: dotnet run --project ... -- --write-story <mode> [options]
// Generates a story via the same pipeline as the /stories UI and saves it as a Chapter.
if (args.Contains("--write-story"))
{
    var cliBuilder = WebApplication.CreateBuilder(args);
    cliBuilder.Services.AddStreetSamuraiServices();
    var cliApp = cliBuilder.Build();
    Environment.ExitCode = await StoryWriterCli.RunAsync(args, cliApp.Services);
    return;
}

// CLI mode: dotnet run --project ... -- --refine-story <projectId> [-o notes.json]
// Analyzes a completed story and writes refinement notes (no rewrites).
if (args.Contains("--refine-story"))
{
    var cliBuilder = WebApplication.CreateBuilder(args);
    cliBuilder.Services.AddStreetSamuraiServices();
    var cliApp = cliBuilder.Build();
    Environment.ExitCode = await StoryRefineCli.RunAsync(args, cliApp.Services);
    return;
}

// CLI mode: book operations — list / new / show / chapters / absorb / review / apply / export / delete.
// Run `dotnet run --project StreetSamurai.Blazor -- --book` (no subcommand) to see full usage.
if (args.Contains("--book"))
{
    var cliBuilder = WebApplication.CreateBuilder(args);
    cliBuilder.Services.AddStreetSamuraiServices();
    var cliApp = cliBuilder.Build();
    Environment.ExitCode = await BookCli.RunAsync(args, cliApp.Services);
    return;
}

// CLI mode: unified continuity store — migrate / stats / contradictions / resolve / entity.
// Run `dotnet run --project StreetSamurai.Blazor -- --continuity` (no subcommand) to see full usage.
if (args.Contains("--continuity"))
{
    var cliBuilder = WebApplication.CreateBuilder(args);
    cliBuilder.Services.AddStreetSamuraiServices();
    var cliApp = cliBuilder.Build();
    Environment.ExitCode = ContinuityCli.Run(args, cliApp.Services);
    return;
}

// CLI mode: SQL Server migration — apply EF migrations and import JSON entities.
//   ss --migrate-sql --schema           apply EF migrations
//   ss --migrate-sql --import people    import character JSON files
//   ss --migrate-sql --all              schema + import all supported types
if (args.Contains("--migrate-sql"))
{
    var cliBuilder = WebApplication.CreateBuilder(args);
    cliBuilder.Services.AddStreetSamuraiServices();
    var cliApp = cliBuilder.Build();
    Environment.ExitCode = await MigrateSqlCli.RunAsync(args, cliApp.Services);
    return;
}

// CLI mode: prose → entities + edges. LLM-driven.
//   ss --interpret --text "..."  | --file path.txt
//   add --commit to apply, --auto-create to stub missing entities, --tag <source>
if (args.Contains("--interpret"))
{
    var cliBuilder = WebApplication.CreateBuilder(args);
    cliBuilder.Services.AddStreetSamuraiServices();
    var cliApp = cliBuilder.Build();
    Environment.ExitCode = await InterpretCli.RunAsync(args, cliApp.Services);
    return;
}

// CLI mode: insert a worldbuilding Document directly into canon.
//   ss --add-doc --title "…" --body-file path.md [--category essay] [--tags "a,b,c"] [--filename slug.md]
if (args.Contains("--add-doc"))
{
    var cliBuilder = WebApplication.CreateBuilder(args);
    cliBuilder.Services.AddStreetSamuraiServices();
    var cliApp = cliBuilder.Build();
    Environment.ExitCode = await AddDocCli.RunAsync(args, cliApp.Services);
    return;
}

// CLI mode: insert a Character from a CharacterData JSON file.
//   ss --add-character --file path.json
if (args.Contains("--add-character"))
{
    var cliBuilder = WebApplication.CreateBuilder(args);
    cliBuilder.Services.AddStreetSamuraiServices();
    var cliApp = cliBuilder.Build();
    Environment.ExitCode = await AddCharacterCli.RunAsync(args, cliApp.Services);
    return;
}

// CLI mode: insert OR update a Place/District from a DistrictData JSON file.
// Upsert: include "id" to update, omit to create. Safe service-layer path
// (DistrictRepository.Save) — no hand-SQL, collision-safe slugs.
//   ss --add-place --file path.json [--print]
if (args.Contains("--add-place"))
{
    var cliBuilder = WebApplication.CreateBuilder(args);
    cliBuilder.Services.AddStreetSamuraiServices();
    var cliApp = cliBuilder.Build();
    Environment.ExitCode = await AddPlaceCli.RunAsync(args, cliApp.Services);
    return;
}

// CLI mode: insert a CorpoNation from a CorponationData JSON file.
//   ss --add-corponation --file path.json
if (args.Contains("--add-corponation"))
{
    var cliBuilder = WebApplication.CreateBuilder(args);
    cliBuilder.Services.AddStreetSamuraiServices();
    var cliApp = cliBuilder.Build();
    Environment.ExitCode = await AddCorponationCli.RunAsync(args, cliApp.Services);
    return;
}

// CLI mode: generate a resource-tracked combat sequence via CombatSceneWriter.
//   ss --combat --file scene.json [--out prose.txt]
//   ss --combat --location "Hegewisch" --objective "..." --exchanges 6 --tone Cinematic
if (args.Contains("--combat"))
{
    var cliBuilder = WebApplication.CreateBuilder(args);
    cliBuilder.Services.AddStreetSamuraiServices();
    var cliApp = cliBuilder.Build();
    Environment.ExitCode = await CombatCli.RunAsync(args, cliApp.Services);
    return;
}

// CLI mode: insert a News article from a NewsData JSON file.
//   ss --add-news --file path.json
if (args.Contains("--add-news"))
{
    var cliBuilder = WebApplication.CreateBuilder(args);
    cliBuilder.Services.AddStreetSamuraiServices();
    var cliApp = cliBuilder.Build();
    Environment.ExitCode = await AddNewsCli.RunAsync(args, cliApp.Services);
    return;
}

// CLI mode: per-table schema operations (snapshot + safe column-reorder rebuild).
//   ss --schema snapshot --table NAME [--out path.sql]
//   ss --schema rebuild  --table NAME --order "col1,col2,col3,…"
if (args.Contains("--schema"))
{
    var cliBuilder = WebApplication.CreateBuilder(args);
    cliBuilder.Services.AddStreetSamuraiServices();
    var cliApp = cliBuilder.Build();
    Environment.ExitCode = await SchemaCli.RunAsync(args, cliApp.Services);
    return;
}

// CLI mode: dump the entire StreetSamurai DB to a re-runnable .sql script.
//   ss --sql-export --schema             schema-only DDL
//   ss --sql-export --data               schema + INSERT data
//   ss --sql-export --schema --out path  override output path
if (args.Contains("--sql-export"))
{
    var cliBuilder = WebApplication.CreateBuilder(args);
    cliBuilder.Services.AddStreetSamuraiServices();
    var cliApp = cliBuilder.Build();
    Environment.ExitCode = await SqlExportCli.RunAsync(args, cliApp.Services);
    return;
}

// CLI mode: dossier-driven story repair — walks every chapter, augments character
// records with timeline entries and (optionally) LLM-extracted continuity claims.
//   ss --repair                # cheap timeline-only pass
//   ss --repair --continuity   # also run continuity extraction (LLM-heavy)
if (args.Contains("--repair"))
{
    var cliBuilder = WebApplication.CreateBuilder(args);
    cliBuilder.Services.AddStreetSamuraiServices();
    var cliApp = cliBuilder.Build();
    Environment.ExitCode = await RepairCli.RunAsync(args, cliApp.Services);
    return;
}

// CLI mode: cloud RAG over the canon corpus. Replaces the retired Ollama path.
//   ss --ask "Question" [--k 8] [--type character]
if (args.Contains("--ask"))
{
    var cliBuilder = WebApplication.CreateBuilder(args);
    cliBuilder.Services.AddStreetSamuraiServices();
    var cliApp = cliBuilder.Build();
    Environment.ExitCode = await AskCli.RunAsync(args, cliApp.Services);
    return;
}

// CLI mode: idempotent stub-creator for the seeded "Vultures on the Doorstep"
// future story. Creates the Book + Draft outline only; writes no prose.
//   ss --seed-vultures
if (args.Contains("--seed-vultures"))
{
    var cliBuilder = WebApplication.CreateBuilder(args);
    cliBuilder.Services.AddStreetSamuraiServices();
    var cliApp = cliBuilder.Build();
    Environment.ExitCode = VulturesSeedCli.Run(args, cliApp.Services);
    return;
}

// CLI mode: report Character columns that disagree with their latest
// matching EntityStateEvents row. Lights up the static-vs-dynamic recipe
// only for columns that actually drifted.
//   ss --audit-drift           pretty-printed report
//   ss --audit-drift --json    JSON dump
if (args.Contains("--audit-drift"))
{
    var cliBuilder = WebApplication.CreateBuilder(args);
    cliBuilder.Services.AddStreetSamuraiServices();
    var cliApp = cliBuilder.Build();
    Environment.ExitCode = await AuditDriftCli.RunAsync(args, cliApp.Services);
    return;
}

// CLI mode: backfill EntityStateEvents from the dynamic columns currently
// sitting on Characters (Location, LifeStatus, Role, Affiliation, Belongings*,
// Territory*, DailyLife). One-shot, idempotent.
if (args.Contains("--backfill-character-state"))
{
    var cliBuilder = WebApplication.CreateBuilder(args);
    cliBuilder.Services.AddStreetSamuraiServices();
    var cliApp = cliBuilder.Build();
    Environment.ExitCode = await StateBackfillCli.RunAsync(args, cliApp.Services);
    return;
}

// CLI mode: rewrite ethnicity-keyed visual descriptors in image prompts to
// match a character's current genetic_ancestry. Cost-aware via stored hash.
//   ss --image-prompts regen --id <id|slug> [--force]
//   ss --image-prompts regen --all-changed
if (args.Contains("--image-prompts"))
{
    var cliBuilder = WebApplication.CreateBuilder(args);
    cliBuilder.Services.AddStreetSamuraiServices();
    var cliApp = cliBuilder.Build();
    Environment.ExitCode = await ImagePromptsCli.RunAsync(args, cliApp.Services);
    return;
}

// CLI mode: propose a plausible immediate family for one character.
//   ss --family-gen propose --of <id|slug>           dry run
//   ss --family-gen propose --of <id|slug> --commit  write characters + edges + propagate genetics
//   --seed N for reproducible RNG
if (args.Contains("--family-gen"))
{
    var cliBuilder = WebApplication.CreateBuilder(args);
    cliBuilder.Services.AddStreetSamuraiServices();
    var cliApp = cliBuilder.Build();
    Environment.ExitCode = await FamilyGenCli.RunAsync(args, cliApp.Services);
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
    var cliBuilder = WebApplication.CreateBuilder(args);
    cliBuilder.Services.AddStreetSamuraiServices();
    var cliApp = cliBuilder.Build();
    Environment.ExitCode = await GeneticsCli.RunAsync(args, cliApp.Services);
    return;
}

// CLI mode: family ties — hand-seed parent/sibling/spouse links between characters.
//   ss --family parent  --parent <id|slug> --child <id|slug>
//   ss --family sibling --a <id|slug> --b <id|slug>
//   ss --family spouse  --a <id|slug> --b <id|slug>
//   ss --family show    --of <id|slug>
if (args.Contains("--family"))
{
    var cliBuilder = WebApplication.CreateBuilder(args);
    cliBuilder.Services.AddStreetSamuraiServices();
    var cliApp = cliBuilder.Build();
    Environment.ExitCode = await FamilyCli.RunAsync(args, cliApp.Services);
    return;
}

// CLI mode: backfill BeatEntityMentions — index which entity names appear in
// each beat so entity-update staleness propagation works.
//   ss --scan-entity-mentions
if (args.Contains("--scan-entity-mentions"))
{
    var cliBuilder = WebApplication.CreateBuilder(args);
    cliBuilder.Services.AddStreetSamuraiServices();
    var cliApp = cliBuilder.Build();
    Environment.ExitCode = await ScanEntityMentionsCli.RunAsync(cliApp.Services);
    return;
}

// CLI mode: dump canon JSON to the user's Downloads folder.
//   ss --export global                every repo, zipped + timestamped
//   ss --export <repoName>            one repo, zipped (e.g. "people", "weaponry")
//   ss --export <entityId>            one entity, plain .json
if (args.Contains("--export"))
{
    var cliBuilder = WebApplication.CreateBuilder(args);
    cliBuilder.Services.AddStreetSamuraiServices();
    var cliApp = cliBuilder.Build();
    Environment.ExitCode = await ExportCli.RunAsync(args, cliApp.Services);
    return;
}

// CLI mode: rebuild the entity-embedding cache via cloud OpenAI.
//   ss --reembed              drift-skipped corpus pass (only changed entities re-embed)
//   ss --reembed --force      clear the table first, re-embed everything
if (args.Contains("--reembed"))
{
    var cliBuilder = WebApplication.CreateBuilder(args);
    cliBuilder.Services.AddStreetSamuraiServices();
    var cliApp = cliBuilder.Build();
    Environment.ExitCode = await ReembedCli.RunAsync(args, cliApp.Services);
    return;
}

// CLI mode: query the Legion / LLMVoting cloud-LLM panel directly.
//   ss --legion ask "Q" --options "A,B,C"  → forced-choice Quorum decision (JSON on stdout)
//   ss --legion vote "Q" [--context "…"]    → open-ended vote with synthesized narrative
if (args.Contains("--legion"))
{
    var cliBuilder = WebApplication.CreateBuilder(args);
    cliBuilder.Services.AddStreetSamuraiServices();
    var cliApp = cliBuilder.Build();
    Environment.ExitCode = await LegionCli.RunAsync(args, cliApp.Services);
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
    var cliBuilder = WebApplication.CreateBuilder(args);
    cliBuilder.Services.AddStreetSamuraiServices();
    var cliApp = cliBuilder.Build();
    Environment.ExitCode = await SeedCli.RunAsync(args, cliApp.Services);
    return;
}

// CLI mode: (re)generate the strand bible for an existing strand.
//   ss --bible-strand --slug <slug> [--beats N] [--replace-beats]
if (args.Contains("--bible-strand"))
{
    var cliBuilder = WebApplication.CreateBuilder(args);
    cliBuilder.Services.AddStreetSamuraiServices();
    var cliApp = cliBuilder.Build();
    Environment.ExitCode = await StrandBibleCli.RunAsync(args, cliApp.Services);
    return;
}

// CLI mode: generate a new strand (bible-first: plan → planned beats → expand in UI).
// CLI mode: autonomous corpus loop — generate N strands end-to-end and review them.
//   ss --run-corpus --count N [--seed "..."] [--kind episode] [--beats 12] [--ballots 20] [--resume] [--dry-run]
if (args.Contains("--run-corpus"))
{
    var cliBuilder = WebApplication.CreateBuilder(args);
    cliBuilder.Services.AddStreetSamuraiServices();
    var cliApp = cliBuilder.Build();
    Environment.ExitCode = await RunCorpusCli.RunAsync(args, cliApp.Services);
    return;
}

// CLI mode: expand planned beats in a strand to prose (headless ✨ for each beat).
//   ss --edit-beat --slug <slug> (--beat-number N | --insert-after N) --file <path>
if (args.Contains("--edit-beat"))
{
    var cliBuilder = WebApplication.CreateBuilder(args);
    cliBuilder.Services.AddStreetSamuraiServices();
    var cliApp = cliBuilder.Build();
    Environment.ExitCode = await EditBeatCli.RunAsync(args, cliApp.Services);
    return;
}

//   ss --expand-beat (--slug <slug> | --id <guid>) [--beat <beatId>] [--force]
if (args.Contains("--expand-beat"))
{
    var cliBuilder = WebApplication.CreateBuilder(args);
    cliBuilder.Services.AddStreetSamuraiServices();
    var cliApp = cliBuilder.Build();
    Environment.ExitCode = await ExpandBeatCli.RunAsync(args, cliApp.Services);
    return;
}

//   ss --write-strand --seed "..." [--title "..."] [--kind episode] [--beats 12] [--bible-only]
if (args.Contains("--write-strand"))
{
    var cliBuilder = WebApplication.CreateBuilder(args);
    cliBuilder.Services.AddStreetSamuraiServices();
    var cliApp = cliBuilder.Build();
    Environment.ExitCode = await WriteStrandCli.RunAsync(args, cliApp.Services);
    return;
}

// CLI mode: delete the 44 legacy book/chapter Entity+Records blobs whose
// content already lives in the Strands/Beats model. Classifies each as JUNK,
// REDUNDANT, or ORPHAN (converts orphans to Strands before deleting).
//   ss --migrate-legacy-book-chapter
if (args.Contains("--migrate-legacy-book-chapter"))
{
    var cliBuilder = WebApplication.CreateBuilder(args);
    cliBuilder.Services.AddStreetSamuraiServices();
    var cliApp = cliBuilder.Build();
    Environment.ExitCode = await MigrateLegacyBookChapterCli.RunAsync(args, cliApp.Services);
    return;
}

// CLI mode: migrate legacy Books/Chapters/ChapterBeats/Episodes/EpisodeBeats
// data into the unified Beat/Strand schema. Idempotent — safe to re-run.
//   ss --migrate-strands
if (args.Contains("--migrate-strands"))
{
    var cliBuilder = WebApplication.CreateBuilder(args);
    cliBuilder.Services.AddStreetSamuraiServices();
    var cliApp = cliBuilder.Build();
    var svc = cliApp.Services.GetRequiredService<StrandMigrationService>();
    var report = await svc.MigrateAllAsync();
    Console.WriteLine($"[migrate-strands] Books={report.BooksAdded} Chapters={report.ChaptersAdded} Beats={report.BeatsAdded} Episodes={report.EpisodesAdded} Standalone={report.StandaloneBeatsAdded} Junctions={report.JunctionRowsAdded}");
    return;
}

// CLI mode: reconcile audio bytes between local disk and Azure Blob storage.
// Companion to DualWriteAudioStore — repairs drift from offline recordings
// and failed background uploads. Default (no --push/--pull args) is full
// bidirectional repair. See SyncAudioCli class doc for the full arg list.
//   ss --sync-audio [--push] [--pull] [--strand SLUG] [--dry-run] [--verbose]
if (args.Contains("--sync-audio"))
{
    var cliBuilder = WebApplication.CreateBuilder(args);
    // CLI builders don't get the main web host's config wiring (that lives after
    // these early returns), so surface %APPDATA%\MindAttic\<bucket>\providers.json
    // here too — AzureBlobAudioStore reads AudioStore:ConnectionString straight
    // from IConfiguration with no file-store fallback, so without this the sync
    // throws "requires AudioStore:ConnectionString" even though the Vault has it.
    cliBuilder.Configuration.AddMindAtticVaultFiles();
    cliBuilder.Services.AddStreetSamuraiServices();
    var cliApp = cliBuilder.Build();
    Environment.ExitCode = await SyncAudioCli.RunAsync(args, cliApp.Services);
    return;
}

// CLI mode: (re)narrate an EXISTING strand by id (full or prefix) or slug.
// Runs the same NarrateAsync path the Record button uses. Use to re-record a
// strand whose beats failed (e.g. a TTS 400) without regenerating prose.
//   ss --narrate-strand (--id <guid|prefix> | --slug <slug>)
if (args.Contains("--narrate-strand"))
{
    var cliBuilder = WebApplication.CreateBuilder(args);
    cliBuilder.Services.AddStreetSamuraiServices();
    var cliApp = cliBuilder.Build();
    Environment.ExitCode = await NarrateStrandCli.RunAsync(args, cliApp.Services);
    return;
}

// CLI mode: create a fixed, named reviewer panel of N personas, disjoint from
// every existing focus group (no persona on two panels). No LLM calls.
//   ss --make-group --name "Group B" [--size 128]
if (args.Contains("--make-group"))
{
    var cliBuilder = WebApplication.CreateBuilder(args);
    cliBuilder.Services.AddStreetSamuraiServices();
    var cliApp = cliBuilder.Build();
    Environment.ExitCode = await MakeGroupCli.RunAsync(args, cliApp.Services);
    return;
}

// CLI mode: run Legion persona quality voting across canon entity repos.
// Replaces the old LlmVoting (10 GLMZ residents) with the full 1000-persona library,
// 1-100 scale, and append-only EntityReview rows (same process as strand reviews).
//   ss --review-entity [--type <type>] [--ballots N] [--prose N] [--unrated]
if (args.Contains("--review-entity"))
{
    var cliBuilder = WebApplication.CreateBuilder(args);
    cliBuilder.Services.AddStreetSamuraiServices();
    var cliApp = cliBuilder.Build();
    Environment.ExitCode = await ReviewEntityCli.RunAsync(args, cliApp.Services);
    return;
}

// CLI mode: have N Legion personas each read an EXISTING strand and write an
// honest, scored reader review (saved to StrandReviews), then synthesize the
// Amazon-style aggregate summary. Round-robins reviewers across the trusted-4.
//   ss --review-strand (--id <guid|prefix> | --slug <slug>) [--readers N]
if (args.Contains("--review-strand"))
{
    var cliBuilder = WebApplication.CreateBuilder(args);
    cliBuilder.Services.AddStreetSamuraiServices();
    var cliApp = cliBuilder.Build();
    Environment.ExitCode = await ReviewStrandCli.RunAsync(args, cliApp.Services);
    return;
}

// CLI mode: add an author ruling to the prose-lessons memory store.
// Lessons are injected into review ballot prompts so reviewers don't penalise
// beats the author has already ruled are doing their job.
//   ss --lesson-add --scope <scope> --kind <kind> --text "<text>"
//   Scope: global | strand:<slug> | beat:<guid>
//   Kind:  score-vs-function | delight | voice | pacing | continuity | other
if (args.Contains("--lesson-add"))
{
    var cliBuilder = WebApplication.CreateBuilder(args);
    cliBuilder.Services.AddStreetSamuraiServices();
    var cliApp = cliBuilder.Build();
    Environment.ExitCode = await ProseLessonCli.RunAddAsync(args, cliApp.Services);
    return;
}

// CLI mode: list prose lessons (all scopes or filtered).
//   ss --lessons-list [--scope <scope>]
if (args.Contains("--lessons-list"))
{
    var cliBuilder = WebApplication.CreateBuilder(args);
    cliBuilder.Services.AddStreetSamuraiServices();
    var cliApp = cliBuilder.Build();
    Environment.ExitCode = await ProseLessonCli.RunListAsync(args, cliApp.Services);
    return;
}

// CLI mode: register feedback loop — surface top-N beats by EmotionalScore, identify
// which register law each exemplifies, and append as candidate entries to
// docs/registers/<NAME>.md. Closes the story→review→exemplar→register→prose loop.
//   ss --update-register-exemplars (--slug <slug> | --id <guid>) [--top N] [--dry-run]
if (args.Contains("--update-register-exemplars"))
{
    var cliBuilder = WebApplication.CreateBuilder(args);
    cliBuilder.Services.AddStreetSamuraiServices();
    var cliApp = cliBuilder.Build();
    Environment.ExitCode = await UpdateRegisterExemplarsCli.RunAsync(args, cliApp.Services);
    return;
}

// CLI mode: review-driven auto-editor. Weight the latest reviews, target the
// lowest / most-flagged beats (raise the floor), and emit conservative
// before/after rewrite PROPOSALS (JSON) for an approval survey. Nothing is written.
//   ss --edit-strand (--id <guid|prefix> | --slug <slug>) [--top N]
if (args.Contains("--edit-strand"))
{
    var cliBuilder = WebApplication.CreateBuilder(args);
    cliBuilder.Services.AddStreetSamuraiServices();
    var cliApp = cliBuilder.Build();
    Environment.ExitCode = await EditStrandCli.RunAsync(args, cliApp.Services);
    return;
}

// CLI mode: stitch an existing strand's beats into one combined file (WAV →
// MP3), copy it to the publish output dir (Downloads by default), and record
// the publication run + process-event ledger. Headless Publish button.
//   ss --publish-strand (--id <guid|prefix> | --slug <slug>)
if (args.Contains("--publish-strand"))
{
    var cliBuilder = WebApplication.CreateBuilder(args);
    cliBuilder.Services.AddStreetSamuraiServices();
    var cliApp = cliBuilder.Build();
    Environment.ExitCode = await PublishStrandCli.RunAsync(args, cliApp.Services);
    return;
}

// CLI mode: render a strand to a KDP-ready Word .docx in Downloads.
//   ss --publish-docx (--id <guid|prefix> | --slug <slug>) [--author "Name"]
if (args.Contains("--publish-docx"))
{
    var cliBuilder = WebApplication.CreateBuilder(args);
    cliBuilder.Services.AddStreetSamuraiServices();
    var cliApp = cliBuilder.Build();
    Environment.ExitCode = await PublishDocxCli.RunAsync(args, cliApp.Services);
    return;
}

// CLI mode: build an Audible AI-narration hand-off package for a strand.
// Produces a narration-clean manuscript, pronunciation guide, and README.
//   ss --prepare-audible (--slug <slug> | --id <guid|prefix>) [--no-phonetics]
if (args.Contains("--prepare-audible"))
{
    var cliBuilder = WebApplication.CreateBuilder(args);
    cliBuilder.Services.AddStreetSamuraiServices();
    var cliApp = cliBuilder.Build();
    Environment.ExitCode = await PrepareAudibleCli.RunAsync(args, cliApp.Services);
    return;
}

// CLI mode: deterministic timeline-consistency check (RFC 0009 §5).
// Detects dead-character-acting and wound-regression violations. No LLM calls.
//   ss --timeline-check (--slug <slug> | --id <guid>)
if (args.Contains("--timeline-check"))
{
    var cliBuilder = WebApplication.CreateBuilder(args);
    cliBuilder.Services.AddStreetSamuraiServices();
    var cliApp = cliBuilder.Build();
    Environment.ExitCode = await TimelineCheckCli.RunAsync(args, cliApp.Services);
    return;
}

// CLI mode: set the ParentStrandId on an existing strand (move it into a collection).
// X-Ray scene assembly (RFC 0002): print the entity roster + voice context block
// for a beat or raw prose. CLI twin of the MCP tool assemble_scene_context.
//   ss --assemble-scene (--beat <guid> | --text "<prose>") [--budget N]
if (args.Contains("--assemble-scene"))
{
    var cliBuilder = WebApplication.CreateBuilder(args);
    cliBuilder.Services.AddStreetSamuraiServices();
    var cliApp = cliBuilder.Build();
    Environment.ExitCode = await AssembleSceneCli.RunAsync(args, cliApp.Services);
    return;
}

//   ss --reparent-strand (--slug <slug> | --id <id>) (--parent-slug <slug> | --parent-id <id>)
//   ss --reparent-strand --slug <slug> --clear   — detach from parent
if (args.Contains("--reparent-strand"))
{
    var cliBuilder = WebApplication.CreateBuilder(args);
    cliBuilder.Services.AddStreetSamuraiServices();
    var cliApp = cliBuilder.Build();
    Environment.ExitCode = await ReparentStrandCli.RunAsync(args, cliApp.Services);
    return;
}

// CLI mode: render the WHOLE strand as one continuous audiobook (one TTS pass,
// tiered to ElevenLabs limits — one request, else per-chapter, else split) and
// drop the MP3 in Downloads. The headless twin of the "Export Audio" button.
//   ss --record | --export-audio | --export-mp3 | --publish-audiobook
//      (--id <guid|prefix> | --slug <slug>)
if (args.Contains("--publish-audiobook") || args.Contains("--record") || args.Contains("--export-audio") || args.Contains("--export-mp3"))
{
    var cliBuilder = WebApplication.CreateBuilder(args);
    cliBuilder.Services.AddStreetSamuraiServices();
    var cliApp = cliBuilder.Build();
    Environment.ExitCode = await PublishAudiobookCli.RunAsync(args, cliApp.Services);
    return;
}

// CLI mode: codify the GLMZ house voice + world rules from the memory rubric into
// the DB stores the generator reads (literary_rules / tone_bible). De-fragilizes
// the rules so they no longer depend on an .md file being parsed. Idempotent.
//   ss --seed-voice-rules
if (args.Contains("--seed-voice-rules"))
{
    var cliBuilder = WebApplication.CreateBuilder(args);
    cliBuilder.Services.AddStreetSamuraiServices();
    var cliApp = cliBuilder.Build();
    Environment.ExitCode = await SeedVoiceRulesCli.RunAsync(args, cliApp.Services);
    return;
}

// CLI mode: extract a time / elapsed-duration timeline from all beats in a strand.
// Flags clock anchors, infers story-relative timestamps, and surfaces conflicts.
//   ss --timeline (--slug <slug> | --id <id>)
if (args.Contains("--timeline"))
{
    var cliBuilder = WebApplication.CreateBuilder(args);
    cliBuilder.Services.AddStreetSamuraiServices();
    var cliApp = cliBuilder.Build();
    Environment.ExitCode = await TimelineCli.RunAsync(args, cliApp.Services);
    return;
}

// CLI mode: per-entity-type reachability matrix (how much canon is embedded and
// thus pullable into prose). The standing gap-finder.
//   ss --coverage
if (args.Contains("--coverage"))
{
    var cliBuilder = WebApplication.CreateBuilder(args);
    cliBuilder.Services.AddStreetSamuraiServices();
    var cliApp = cliBuilder.Build();
    Environment.ExitCode = await CoverageCli.RunAsync(args, cliApp.Services);
    return;
}

// CLI mode: (re)build the materialized character read-model projection from the
// relational source of truth. Run after a bulk import / relational migration,
// or whenever ReadModelVersion is bumped. Backfills missing/stale rows, prunes
// orphans. The steady-state path self-heals, so this is a one-time / maintenance op.
//   ss --rebuild-readmodel [--archived]
if (args.Contains("--rebuild-readmodel"))
{
    var cliBuilder = WebApplication.CreateBuilder(args);
    cliBuilder.Services.AddStreetSamuraiServices();
    var cliApp = cliBuilder.Build();
    Environment.ExitCode = await RebuildReadModelCli.RunAsync(args, cliApp.Services);
    return;
}

// CLI mode: create a runtime-defined repository (custom entity type).
//   ss --create-repository --name "Artifacts" [--category World] [--icon bi-box] [--description "..."]
if (args.Contains("--create-repository"))
{
    string ArgVal(string flag) { var i = Array.IndexOf(args, flag); return i >= 0 && i + 1 < args.Length ? args[i + 1] : ""; }
    var repoName = ArgVal("--name");
    if (string.IsNullOrWhiteSpace(repoName)) { Console.Error.WriteLine("[create-repository] --name is required."); Environment.ExitCode = 1; return; }
    var cliBuilder = WebApplication.CreateBuilder(args);
    cliBuilder.Services.AddStreetSamuraiServices();
    var cliApp = cliBuilder.Build();
    var svc = cliApp.Services.GetRequiredService<StreetSamurai.Core.Services.RepositoryDefinitionService>();
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
    var cliBuilder = WebApplication.CreateBuilder(args);
    cliBuilder.Services.AddStreetSamuraiServices();
    var cliApp = cliBuilder.Build();
    Environment.ExitCode = await RebuildFactionRelationalCli.RunAsync(args, cliApp.Services);
    return;
}

// CLI mode: materialize relational rows for active characters that are blob-only
// (no Characters row) — the no-data-loss gate before dropping the Character blob. (RFC 0007)
//   ss --backfill-missing-characters
if (args.Contains("--backfill-missing-characters"))
{
    var cliBuilder = WebApplication.CreateBuilder(args);
    cliBuilder.Services.AddStreetSamuraiServices();
    var cliApp = cliBuilder.Build();
    Environment.ExitCode = await BackfillMissingCharactersCli.RunAsync(args, cliApp.Services);
    return;
}

// CLI mode: backfill the Quotes relational schema from Records.Json blobs.
// ADDITIVE — Records.Json is never modified. (RFC 0007)
//   ss --rebuild-quote-relational
if (args.Contains("--rebuild-quote-relational"))
{
    var cliBuilder = WebApplication.CreateBuilder(args);
    cliBuilder.Services.AddStreetSamuraiServices();
    var cliApp = cliBuilder.Build();
    Environment.ExitCode = await RebuildQuoteRelationalCli.RunAsync(args, cliApp.Services);
    return;
}

// CLI mode: backfill the News relational schema from Records.Json blobs.
// ADDITIVE — Records.Json is never modified. (RFC 0007)
//   ss --rebuild-news-relational
if (args.Contains("--rebuild-news-relational"))
{
    var cliBuilder = WebApplication.CreateBuilder(args);
    cliBuilder.Services.AddStreetSamuraiServices();
    var cliApp = cliBuilder.Build();
    Environment.ExitCode = await RebuildNewsRelationalCli.RunAsync(args, cliApp.Services);
    return;
}

// CLI mode: backfill the Contracts relational schema from Records.Json blobs.
// ADDITIVE — Records.Json is never modified. (RFC 0007)
//   ss --rebuild-contract-relational
if (args.Contains("--rebuild-contract-relational"))
{
    var cliBuilder = WebApplication.CreateBuilder(args);
    cliBuilder.Services.AddStreetSamuraiServices();
    var cliApp = cliBuilder.Build();
    Environment.ExitCode = await RebuildContractRelationalCli.RunAsync(args, cliApp.Services);
    return;
}

// CLI mode: backfill the VocabularyEntries relational schema from Records.Json blobs.
// ADDITIVE — Records.Json is never modified. (RFC 0007)
//   ss --rebuild-vocabulary-relational
if (args.Contains("--rebuild-vocabulary-relational"))
{
    var cliBuilder = WebApplication.CreateBuilder(args);
    cliBuilder.Services.AddStreetSamuraiServices();
    var cliApp = cliBuilder.Build();
    Environment.ExitCode = await RebuildVocabularyRelationalCli.RunAsync(args, cliApp.Services);
    return;
}

// CLI mode: backfill the Archetypes relational schema from Records.Json blobs.
// ADDITIVE — Records.Json is never modified. (RFC 0007)
//   ss --rebuild-archetype-relational
if (args.Contains("--rebuild-archetype-relational"))
{
    var cliBuilder = WebApplication.CreateBuilder(args);
    cliBuilder.Services.AddStreetSamuraiServices();
    var cliApp = cliBuilder.Build();
    Environment.ExitCode = await RebuildArchetypeRelationalCli.RunAsync(args, cliApp.Services);
    return;
}

// CLI mode: backfill the Genemods relational schema from Records.Json blobs.
// ADDITIVE — Records.Json is never modified. (RFC 0007)
//   ss --rebuild-genemod-relational
if (args.Contains("--rebuild-genemod-relational"))
{
    var cliBuilder = WebApplication.CreateBuilder(args);
    cliBuilder.Services.AddStreetSamuraiServices();
    var cliApp = cliBuilder.Build();
    Environment.ExitCode = await RebuildGenemodRelationalCli.RunAsync(args, cliApp.Services);
    return;
}

// CLI mode: backfill the Materials relational schema from Records.Json blobs.
// ADDITIVE — Records.Json is never modified. (RFC 0007)
//   ss --rebuild-material-relational
if (args.Contains("--rebuild-material-relational"))
{
    var cliBuilder = WebApplication.CreateBuilder(args);
    cliBuilder.Services.AddStreetSamuraiServices();
    var cliApp = cliBuilder.Build();
    Environment.ExitCode = await RebuildMaterialRelationalCli.RunAsync(args, cliApp.Services);
    return;
}

// CLI mode: backfill the Psionics relational schema from Records.Json blobs.
// ADDITIVE — Records.Json is never modified. (RFC 0007)
//   ss --rebuild-psionic-relational
if (args.Contains("--rebuild-psionic-relational"))
{
    var cliBuilder = WebApplication.CreateBuilder(args);
    cliBuilder.Services.AddStreetSamuraiServices();
    var cliApp = cliBuilder.Build();
    Environment.ExitCode = await RebuildPsionicRelationalCli.RunAsync(args, cliApp.Services);
    return;
}

// CLI mode: backfill the Motifs relational schema from Records.Json blobs.
// ADDITIVE — Records.Json is never modified. (RFC 0007)
//   ss --rebuild-motif-relational
if (args.Contains("--rebuild-motif-relational"))
{
    var cliBuilder = WebApplication.CreateBuilder(args);
    cliBuilder.Services.AddStreetSamuraiServices();
    var cliApp = cliBuilder.Build();
    Environment.ExitCode = await RebuildMotifRelationalCli.RunAsync(args, cliApp.Services);
    return;
}

// CLI mode: backfill the LabSpecimens relational schema from Records.Json blobs.
// ADDITIVE — Records.Json is never modified. (RFC 0007)
//   ss --rebuild-lab-specimen-relational
if (args.Contains("--rebuild-lab-specimen-relational"))
{
    var cliBuilder = WebApplication.CreateBuilder(args);
    cliBuilder.Services.AddStreetSamuraiServices();
    var cliApp = cliBuilder.Build();
    Environment.ExitCode = await RebuildLabSpecimenRelationalCli.RunAsync(args, cliApp.Services);
    return;
}

// CLI mode: backfill the FlyoverEntities relational schema from Records.Json blobs.
// ADDITIVE — Records.Json is never modified. (RFC 0007)
//   ss --rebuild-flyover-entity-relational
if (args.Contains("--rebuild-flyover-entity-relational"))
{
    var cliBuilder = WebApplication.CreateBuilder(args);
    cliBuilder.Services.AddStreetSamuraiServices();
    var cliApp = cliBuilder.Build();
    Environment.ExitCode = await RebuildFlyoverEntityRelationalCli.RunAsync(args, cliApp.Services);
    return;
}

// CLI mode: backfill the Automata relational schema from Records.Json blobs.
// ADDITIVE — Records.Json is never modified. (RFC 0007)
//   ss --rebuild-automaton-relational
if (args.Contains("--rebuild-automaton-relational"))
{
    var cliBuilder = WebApplication.CreateBuilder(args);
    cliBuilder.Services.AddStreetSamuraiServices();
    var cliApp = cliBuilder.Build();
    Environment.ExitCode = await RebuildAutomatonRelationalCli.RunAsync(args, cliApp.Services);
    return;
}

// CLI mode: backfill the Ammunitions relational schema from Records.Json blobs.
// ADDITIVE — Records.Json is never modified. (RFC 0007)
//   ss --rebuild-ammunition-relational
if (args.Contains("--rebuild-ammunition-relational"))
{
    var cliBuilder = WebApplication.CreateBuilder(args);
    cliBuilder.Services.AddStreetSamuraiServices();
    var cliApp = cliBuilder.Build();
    Environment.ExitCode = await RebuildAmmunitionRelationalCli.RunAsync(args, cliApp.Services);
    return;
}

// CLI mode: backfill the Transportations relational schema from Records.Json blobs.
// ADDITIVE — Records.Json is never modified. (RFC 0007)
//   ss --rebuild-transportation-relational
if (args.Contains("--rebuild-transportation-relational"))
{
    var cliBuilder = WebApplication.CreateBuilder(args);
    cliBuilder.Services.AddStreetSamuraiServices();
    var cliApp = cliBuilder.Build();
    Environment.ExitCode = await RebuildTransportationRelationalCli.RunAsync(args, cliApp.Services);
    return;
}

// CLI mode: backfill the Corponations relational schema from Records.Json blobs.
// ADDITIVE — Records.Json is never modified. (RFC 0007)
//   ss --rebuild-corponation-relational
if (args.Contains("--rebuild-corponation-relational"))
{
    var cliBuilder = WebApplication.CreateBuilder(args);
    cliBuilder.Services.AddStreetSamuraiServices();
    var cliApp = cliBuilder.Build();
    Environment.ExitCode = await RebuildCorponationRelationalCli.RunAsync(args, cliApp.Services);
    return;
}

// CLI mode: backfill the EquipmentItems relational schema from Records.Json blobs.
// ADDITIVE — Records.Json is never modified. (RFC 0007)
//   ss --rebuild-equipment-relational
if (args.Contains("--rebuild-equipment-relational"))
{
    var cliBuilder = WebApplication.CreateBuilder(args);
    cliBuilder.Services.AddStreetSamuraiServices();
    var cliApp = cliBuilder.Build();
    Environment.ExitCode = await RebuildEquipmentRelationalCli.RunAsync(args, cliApp.Services);
    return;
}

// CLI mode: backfill the Technologies relational schema from Records.Json blobs.
// ADDITIVE — Records.Json is never modified. (RFC 0007)
//   ss --rebuild-technology-relational
if (args.Contains("--rebuild-technology-relational"))
{
    var cliBuilder = WebApplication.CreateBuilder(args);
    cliBuilder.Services.AddStreetSamuraiServices();
    var cliApp = cliBuilder.Build();
    Environment.ExitCode = await RebuildTechnologyRelationalCli.RunAsync(args, cliApp.Services);
    return;
}

// CLI mode: backfill the Pharmaceuticals relational schema from Records.Json blobs.
// ADDITIVE — Records.Json is never modified. (RFC 0007)
//   ss --rebuild-pharmaceutical-relational
if (args.Contains("--rebuild-pharmaceutical-relational"))
{
    var cliBuilder = WebApplication.CreateBuilder(args);
    cliBuilder.Services.AddStreetSamuraiServices();
    var cliApp = cliBuilder.Build();
    Environment.ExitCode = await RebuildPharmaceuticalRelationalCli.RunAsync(args, cliApp.Services);
    return;
}

// CLI mode: backfill the CyberwareItems relational schema from Records.Json blobs.
// ADDITIVE — Records.Json is never modified. (RFC 0007)
//   ss --rebuild-cyberware-relational
if (args.Contains("--rebuild-cyberware-relational"))
{
    var cliBuilder = WebApplication.CreateBuilder(args);
    cliBuilder.Services.AddStreetSamuraiServices();
    var cliApp = cliBuilder.Build();
    Environment.ExitCode = await RebuildCyberwareRelationalCli.RunAsync(args, cliApp.Services);
    return;
}

// CLI mode: backfill the ConsumerGoods relational schema from Records.Json blobs.
// ADDITIVE — Records.Json is never modified. (RFC 0007)
//   ss --rebuild-consumer-good-relational
if (args.Contains("--rebuild-consumer-good-relational"))
{
    var cliBuilder = WebApplication.CreateBuilder(args);
    cliBuilder.Services.AddStreetSamuraiServices();
    var cliApp = cliBuilder.Build();
    Environment.ExitCode = await RebuildConsumerGoodRelationalCli.RunAsync(args, cliApp.Services);
    return;
}

// CLI mode: backfill the SyntheticLives relational schema from Records.Json blobs.
// ADDITIVE — Records.Json is never modified. (RFC 0007)
//   ss --rebuild-synthetic-relational
if (args.Contains("--rebuild-synthetic-relational"))
{
    var cliBuilder = WebApplication.CreateBuilder(args);
    cliBuilder.Services.AddStreetSamuraiServices();
    var cliApp = cliBuilder.Build();
    Environment.ExitCode = await RebuildSyntheticRelationalCli.RunAsync(args, cliApp.Services);
    return;
}

// CLI mode: backfill the Places relational schema from Records.Json blobs.
// ADDITIVE — Records.Json is never modified. (RFC 0007)
//   ss --rebuild-place-relational
if (args.Contains("--rebuild-place-relational"))
{
    var cliBuilder = WebApplication.CreateBuilder(args);
    cliBuilder.Services.AddStreetSamuraiServices();
    var cliApp = cliBuilder.Build();
    Environment.ExitCode = await RebuildPlaceRelationalCli.RunAsync(args, cliApp.Services);
    return;
}

// CLI mode: backfill the Documents relational schema from Records.Json blobs.
// ADDITIVE — Records.Json is never modified. (RFC 0007)
//   ss --rebuild-document-relational
if (args.Contains("--rebuild-document-relational"))
{
    var cliBuilder = WebApplication.CreateBuilder(args);
    cliBuilder.Services.AddStreetSamuraiServices();
    var cliApp = cliBuilder.Build();
    Environment.ExitCode = await RebuildDocumentRelationalCli.RunAsync(args, cliApp.Services);
    return;
}

// CLI mode: backfill the EntertainmentItems relational schema from Records.Json blobs.
// ADDITIVE — Records.Json is never modified. (RFC 0007)
//   ss --rebuild-entertainment-relational
if (args.Contains("--rebuild-entertainment-relational"))
{
    var cliBuilder = WebApplication.CreateBuilder(args);
    cliBuilder.Services.AddStreetSamuraiServices();
    var cliApp = cliBuilder.Build();
    Environment.ExitCode = await RebuildEntertainmentRelationalCli.RunAsync(args, cliApp.Services);
    return;
}

// CLI mode: backfill the Weapons relational schema from Records.Json blobs.
// ADDITIVE — Records.Json is never modified. (RFC 0007)
//   ss --rebuild-weapon-relational
if (args.Contains("--rebuild-weapon-relational"))
{
    var cliBuilder = WebApplication.CreateBuilder(args);
    cliBuilder.Services.AddStreetSamuraiServices();
    var cliApp = cliBuilder.Build();
    Environment.ExitCode = await RebuildWeaponRelationalCli.RunAsync(args, cliApp.Services);
    return;
}

// CLI mode: backfill the Apparels relational schema from Records.Json blobs.
// ADDITIVE — Records.Json is never modified. (RFC 0007)
//   ss --rebuild-apparel-relational
if (args.Contains("--rebuild-apparel-relational"))
{
    var cliBuilder = WebApplication.CreateBuilder(args);
    cliBuilder.Services.AddStreetSamuraiServices();
    var cliApp = cliBuilder.Build();
    Environment.ExitCode = await RebuildApparelRelationalCli.RunAsync(args, cliApp.Services);
    return;
}

// CLI mode: backfill the Subsidiaries relational schema from Records.Json blobs.
// ADDITIVE — Records.Json is never modified. (RFC 0007)
//   ss --rebuild-subsidiary-relational
if (args.Contains("--rebuild-subsidiary-relational"))
{
    var cliBuilder = WebApplication.CreateBuilder(args);
    cliBuilder.Services.AddStreetSamuraiServices();
    var cliApp = cliBuilder.Build();
    Environment.ExitCode = await RebuildSubsidiaryRelationalCli.RunAsync(args, cliApp.Services);
    return;
}

// CLI mode: RFC 0007 unified blob-retirement gate — backfill all 29 relational types
// from Records.Json, validate, and delete the blobs in a single pass. (RFC 0007)
//   ss --retire-records-blobs [--rebuild] [--validate] [--apply]
if (args.Contains("--retire-records-blobs"))
{
    var cliBuilder = WebApplication.CreateBuilder(args);
    cliBuilder.Services.AddStreetSamuraiServices();
    var cliApp = cliBuilder.Build();
    Environment.ExitCode = await RetireRecordsBlobsCli.RunAsync(args, cliApp.Services);
    return;
}

// CLI mode: split a monolithic strand into a Collection (parent + chapter
// child strands) at IsChapterStart boundaries. Backs up to markdown first.
//   ss --split-collection (--slug <s> | --id <guid>)
if (args.Contains("--split-collection"))
{
    var cliBuilder = WebApplication.CreateBuilder(args);
    cliBuilder.Services.AddStreetSamuraiServices();
    var cliApp = cliBuilder.Build();
    Environment.ExitCode = await SplitCollectionCli.RunAsync(args, cliApp.Services);
    return;
}

// CLI mode: print the voice context the generator/re-beater receive — the
// verification that the canon-trained voice is wired into prompts.
//   ss --print-voice
if (args.Contains("--print-voice"))
{
    var cliBuilder = WebApplication.CreateBuilder(args);
    cliBuilder.Services.AddStreetSamuraiServices();
    var cliApp = cliBuilder.Build();
    Environment.ExitCode = await PrintVoiceCli.RunAsync(args, cliApp.Services);
    return;
}

// CLI mode: rebuild a strand's beats to the codified beat doctrine via LLM
// re-segmentation (story beats + dialogue/'?' mechanics + gaps). Dry-run by
// default; --apply backs up to markdown then replaces beats if the word-retention
// guard passes. --all targets every doctrine-violating strand.
//   ss --rebeat-strand (--slug <s> | --id <guid> | --all) [--apply]
if (args.Contains("--rebeat-strand"))
{
    var cliBuilder = WebApplication.CreateBuilder(args);
    cliBuilder.Services.AddStreetSamuraiServices();
    var cliApp = cliBuilder.Build();
    Environment.ExitCode = await RebeatStrandCli.RunAsync(args, cliApp.Services);
    return;
}

// CLI mode: sweep a strand's prose against canon (all entity types) and queue
// contradictions as approval-gated findings — the self-correction pass.
//   ss --check-canon (--slug <s> | --id <guid> | --all)
if (args.Contains("--check-canon"))
{
    var cliBuilder = WebApplication.CreateBuilder(args);
    cliBuilder.Services.AddStreetSamuraiServices();
    var cliApp = cliBuilder.Build();
    Environment.ExitCode = await CheckCanonCli.RunAsync(args, cliApp.Services);
    return;
}

// CLI mode: show what the universal canon reach pulls for a query, across ALL
// entity types — verifies the full-interconnect retrieval path.
//   ss --canon-retrieve "<query>" [--k N] [--types t1,t2]
if (args.Contains("--canon-retrieve"))
{
    var cliBuilder = WebApplication.CreateBuilder(args);
    cliBuilder.Services.AddStreetSamuraiServices();
    var cliApp = cliBuilder.Build();
    Environment.ExitCode = await CanonRetrieveCli.RunAsync(args, cliApp.Services);
    return;
}

// CLI mode: author-only Canon trust gate — mark a strand strong enough to draw
// conclusions about its characters/events (the voice-harvest learns from canon).
//   ss --mark-canon (--slug <s> | --id <guid>) [--off]
if (args.Contains("--mark-canon"))
{
    var cliBuilder = WebApplication.CreateBuilder(args);
    cliBuilder.Services.AddStreetSamuraiServices();
    var cliApp = cliBuilder.Build();
    Environment.ExitCode = await MarkCanonCli.RunAsync(args, cliApp.Services);
    return;
}

// CLI mode: distill voice rules from winning (≥80%) strands into the codified
// DB-backed rules the generator reads. Propose-then-approve.
//   ss --harvest-voice (--slug <s> | --id <id> | --all-80 | --pending | --apply <guid> | --reject <guid>) [--force]
if (args.Contains("--harvest-voice"))
{
    var cliBuilder = WebApplication.CreateBuilder(args);
    cliBuilder.Services.AddStreetSamuraiServices();
    var cliApp = cliBuilder.Build();
    Environment.ExitCode = await HarvestVoiceCli.RunAsync(args, cliApp.Services);
    return;
}

// CLI mode: list every strand as a table (or JSON). Headless twin of /strands.
//   ss --list-strands [--status <s>] [--kind <k>] [--search <text>] [--limit <n>] [--json]
if (args.Contains("--list-strands"))
{
    var cliBuilder = WebApplication.CreateBuilder(args);
    cliBuilder.Services.AddStreetSamuraiServices();
    var cliApp = cliBuilder.Build();
    Environment.ExitCode = await ListStrandsCli.RunAsync(args, cliApp.Services);
    return;
}

// CLI mode: render a strand to Markdown or PDF in Downloads.
// Markdown output embeds <!-- beat:N:id7 --> markers for ss --import-md round-trip.
//   ss (--publish-md | --publish-pdf) (--id <guid|prefix> | --slug <slug>) [--author "Name"]
if (args.Contains("--publish-md") || args.Contains("--publish-pdf"))
{
    var cliBuilder = WebApplication.CreateBuilder(args);
    cliBuilder.Services.AddStreetSamuraiServices();
    var cliApp = cliBuilder.Build();
    var format = args.Contains("--publish-md") ? PublishManuscriptCli.Format.Markdown
               : PublishManuscriptCli.Format.Pdf;
    Environment.ExitCode = await PublishManuscriptCli.RunAsync(args, cliApp.Services, format);
    return;
}

// CLI mode: reimport an edited --publish-md Markdown file back into the DB. Each
// <!-- beat:N:id7 --> marker identifies the beat; prose between markers updates Beat.Text.
//   ss --import-md --file path.md [--dry-run]
if (args.Contains("--import-md"))
{
    var cliBuilder = WebApplication.CreateBuilder(args);
    cliBuilder.Services.AddStreetSamuraiServices();
    var cliApp = cliBuilder.Build();
    Environment.ExitCode = await ImportMarkdownCli.RunAsync(args, cliApp.Services);
    return;
}

// CLI mode: bounded copy-edit of a strand — proper paragraph/dialogue spacing, a
// "?" on questions that lack one, and "asks"/"asked" (not "says") on question
// dialogue. Dry-run by default; --apply commits. Beats edited beyond those bounds
// are rejected (word-token guard) and left untouched.
//   ss --reflow-strand (--id <guid|prefix> | --slug <slug>) [--apply]
if (args.Contains("--reflow-strand"))
{
    var cliBuilder = WebApplication.CreateBuilder(args);
    cliBuilder.Services.AddStreetSamuraiServices();
    var cliApp = cliBuilder.Build();
    Environment.ExitCode = await ReflowStrandCli.RunAsync(args, cliApp.Services);
    return;
}

// CLI mode: deep-duplicate a strand (and its sub-strand tree) into a fresh,
// independent copy — every beat cloned to a new row (prose + metadata kept;
// audio/score/stale reset). Editing the copy never touches the original.
//   ss --duplicate-strand (--id <guid|prefix> | --slug <slug>) --title "New Title"
if (args.Contains("--duplicate-strand"))
{
    var cliBuilder = WebApplication.CreateBuilder(args);
    cliBuilder.Services.AddStreetSamuraiServices();
    var cliApp = cliBuilder.Build();
    Environment.ExitCode = await DuplicateStrandCli.RunAsync(args, cliApp.Services);
    return;
}

// CLI mode: import a hand-authored .strand file (beat + gap + beat …) into a
// fresh strand. The complement to --write-strand (LLM-generated): this is for
// drafts written elsewhere (chat exports, transcripts, paper notes typed up).
// See ImportStrandCli class doc for the file format.
//   ss --import-strand --file path.strand [--title ...] [--kind ...] [--slug ...] [--parent ...] [--dry-run]
if (args.Contains("--import-strand"))
{
    var cliBuilder = WebApplication.CreateBuilder(args);
    cliBuilder.Services.AddStreetSamuraiServices();
    var cliApp = cliBuilder.Build();
    Environment.ExitCode = await ImportStrandCli.RunAsync(args, cliApp.Services);
    return;
}

// CLI mode: burst oversized beats (e.g. chapter-as-one-beat from old book
// imports) into paragraph-sized pieces. Idempotent — already-small beats
// are skipped on rerun.
//   ss --burst-beats [--min-chars 800] [--strand slug] [--kind book] [--dry-run]
if (args.Contains("--burst-beats"))
{
    var cliBuilder = WebApplication.CreateBuilder(args);
    cliBuilder.Services.AddStreetSamuraiServices();
    var cliApp = cliBuilder.Build();
    Environment.ExitCode = await BurstBeatsCli.RunAsync(args, cliApp.Services);
    return;
}

// CLI mode: report flat-vs-bridge drift for a denormalised column.
//   ss --audit-denorm Entities.TagsJson
//   ss --audit-denorm Characters.Affiliation
if (args.Contains("--audit-denorm"))
{
    var cliBuilder = WebApplication.CreateBuilder(args);
    cliBuilder.Services.AddStreetSamuraiServices();
    var cliApp = cliBuilder.Build();
    Environment.ExitCode = await AuditDenormCli.RunAsync(args, cliApp.Services);
    return;
}

// CLI mode: findings inbox — list / show / apply / dismiss / scan.
if (args.Contains("--findings"))
{
    var cliBuilder = WebApplication.CreateBuilder(args);
    cliBuilder.Services.AddStreetSamuraiServices();
    var cliApp = cliBuilder.Build();
    Environment.ExitCode = await FindingsCli.RunAsync(args, cliApp.Services);
    return;
}

// ss --entity-tree (--id <guid> | --slug <slug>) [--depth N] [--rel-types type1,type2] [--as-of date]
if (args.Contains("--entity-tree"))
{
    var cliBuilder = WebApplication.CreateBuilder(args);
    cliBuilder.Services.AddStreetSamuraiServices();
    var cliApp = cliBuilder.Build();
    Environment.ExitCode = await EntityTreeCli.RunAsync(args, cliApp.Services);
    return;
}

// ss --prose-check (--slug <strandSlug> | --id <beatId>) [--all] [--json]
if (args.Contains("--prose-check"))
{
    var cliBuilder = WebApplication.CreateBuilder(args);
    cliBuilder.Services.AddStreetSamuraiServices();
    var cliApp = cliBuilder.Build();
    Environment.ExitCode = await ProseCheckCli.RunAsync(args, cliApp.Services);
    return;
}

// ss --check-fidelity (--slug <strandSlug> | --id <strandId>) [--json]
// Detects the Semantic Fidelity Gap — beats scoring high but drifting from the
// story's original meaning (Goodhart's Law in prose). Two checks:
//   Bible alignment: prose vs Seed/Synopsis (north-star drift)
//   Intent alignment: prose vs beat Synopsis (purpose drift)
// Files SEMANTIC-DRIFT findings; also runs automatically after every review.
if (args.Contains("--check-fidelity"))
{
    var cliBuilder = WebApplication.CreateBuilder(args);
    cliBuilder.Services.AddStreetSamuraiServices();
    var cliApp = cliBuilder.Build();
    Environment.ExitCode = await CheckFidelityCli.RunAsync(args, cliApp.Services);
    return;
}

// ss --world-state --beat <beatId> [--story-time "date"] [--json]
if (args.Contains("--world-state"))
{
    var cliBuilder = WebApplication.CreateBuilder(args);
    cliBuilder.Services.AddStreetSamuraiServices();
    var cliApp = cliBuilder.Build();
    Environment.ExitCode = await WorldStateCli.RunAsync(args, cliApp.Services);
    return;
}

// ss --gear-check --slug <strandSlug> --character <characterId> [--story-time date]
if (args.Contains("--gear-check"))
{
    var cliBuilder = WebApplication.CreateBuilder(args);
    cliBuilder.Services.AddStreetSamuraiServices();
    var cliApp = cliBuilder.Build();
    Environment.ExitCode = await GearCheckCli.RunAsync(args, cliApp.Services);
    return;
}

// ss --score-trend [--batches N] [--universe <slug>]
// Print rolling mean score across N chronological batches of scored strands.
// Positive Δ confirms the voice-harvest flywheel is spinning forward (SS-US-J6).
// Exit 0 = positive trend, 1 = flat/declining, 2 = not enough data.
if (args.Contains("--score-trend"))
{
    var cliBuilder = WebApplication.CreateBuilder(args);
    cliBuilder.Services.AddStreetSamuraiServices();
    var cliApp = cliBuilder.Build();
    Environment.ExitCode = await ScoreTrendCli.RunAsync(args, cliApp.Services);
    return;
}

// ss --diagnose-strand --slug <strandSlug> [--json]
// Pre-flight structural analysis before running the review panel.
// Runs 12 targeted checks (antagonist cost, protagonist behavior change,
// exposition density, etc.) and reports Pass/Warn/Fail with evidence + fixes.
// Exit 0 = ready, 1 = warnings, 2 = blocking failures.
if (args.Contains("--diagnose-strand"))
{
    var cliBuilder = WebApplication.CreateBuilder(args);
    cliBuilder.Services.AddStreetSamuraiServices();
    var cliApp = cliBuilder.Build();
    Environment.ExitCode = await DiagnoseStrandCli.RunAsync(args, cliApp.Services);
    return;
}

// ss --examine-emotion --slug <strandSlug> [--effort draft|standard|deep] [--json]
// Emotional Intelligence Examination (SS-A15): 8-dimension 0–4 rubric, per-beat curve,
// character ledger (Want/Need/Wound/Flaw), register-adaptive anchors.
// Exit 0 = none blocking, 1 = advisory issues, 2 = blocking dimensions open.
if (args.Contains("--examine-emotion"))
{
    var cliBuilder = WebApplication.CreateBuilder(args);
    cliBuilder.Services.AddStreetSamuraiServices();
    var cliApp = cliBuilder.Build();
    Environment.ExitCode = await ExamineEmotionCli.RunAsync(args, cliApp.Services);
    return;
}

// ss --list-species — print the species taxonomy (canonical name, label, sentience).
if (args.Contains("--list-species"))
{
    var cliBuilder = WebApplication.CreateBuilder(args);
    cliBuilder.Services.AddStreetSamuraiServices();
    var cliApp = cliBuilder.Build();
    Environment.ExitCode = ListSpeciesCli.Run(cliApp.Services);
    return;
}

// ss --behavior-check --slug <strandSlug> --character <characterId>
if (args.Contains("--behavior-check"))
{
    var cliBuilder = WebApplication.CreateBuilder(args);
    cliBuilder.Services.AddStreetSamuraiServices();
    var cliApp = cliBuilder.Build();
    Environment.ExitCode = await BehaviorCheckCli.RunAsync(args, cliApp.Services);
    return;
}

// ss --weapon-network (--id <weaponId> | --character <characterId> [--as-of date])
if (args.Contains("--weapon-network"))
{
    var cliBuilder = WebApplication.CreateBuilder(args);
    cliBuilder.Services.AddStreetSamuraiServices();
    var cliApp = cliBuilder.Build();
    Environment.ExitCode = await WeaponNetworkCli.RunAsync(args, cliApp.Services);
    return;
}

// ss --ambient-palette --character <characterId> [--as-of date]
if (args.Contains("--ambient-palette"))
{
    var cliBuilder = WebApplication.CreateBuilder(args);
    cliBuilder.Services.AddStreetSamuraiServices();
    var cliApp = cliBuilder.Build();
    Environment.ExitCode = await AmbientPaletteCli.RunAsync(args, cliApp.Services);
    return;
}

// ss --seed-sensory-hints [--list] [--weapon "Name" --hints "hint1; hint2"] [--force]
if (args.Contains("--seed-sensory-hints"))
{
    var cliBuilder = WebApplication.CreateBuilder(args);
    cliBuilder.Services.AddStreetSamuraiServices();
    var cliApp = cliBuilder.Build();
    Environment.ExitCode = await SeedSensoryHintsCli.RunAsync(args, cliApp.Services);
    return;
}

// ss --beat <subcommand> — fine-grained beat manipulation:
//   insert  --strand <slug|id> [--after <beatId>] [--text "..."]
//   delete  --id <beatId> [--strand <slug|id>]
//   update  --id <beatId> --text "..."  (use '-' for stdin)
//   meta    --id <beatId> [--title "..."] [--kind "..."] [--synopsis "..."] [--tone "..."] ...
//   show    --id <beatId>
//   list    --strand <slug|id>
if (args.Contains("--beat"))
{
    var cliBuilder = WebApplication.CreateBuilder(args);
    cliBuilder.Services.AddStreetSamuraiServices();
    var cliApp = cliBuilder.Build();
    var beatArgs = args.SkipWhile(a => a != "--beat").Skip(1).ToArray();
    Environment.ExitCode = await BeatCli.RunAsync(beatArgs, cliApp.Services);
    return;
}

// ss --wound <subcommand> — character wound ledger:
//   list    --character <id|name> [--as-of "date"]
//   log     --character <id|name> --description "..." [--location "chest"] [--severity moderate] ...
//   status  --wound <id> --status active|healed|noted
if (args.Contains("--wound"))
{
    var cliBuilder = WebApplication.CreateBuilder(args);
    cliBuilder.Services.AddStreetSamuraiServices();
    var cliApp = cliBuilder.Build();
    var woundArgs = args.SkipWhile(a => a != "--wound").Skip(1).ToArray();
    Environment.ExitCode = await WoundCli.RunAsync(woundArgs, cliApp.Services);
    return;
}

// ss --universe <subcommand> — universe management:
//   list      Print all universes
//   current   Print the active universe
//   use       --slug <slug> | --id <guid>
if (args.Contains("--universe"))
{
    var cliBuilder = WebApplication.CreateBuilder(args);
    cliBuilder.Services.AddStreetSamuraiServices();
    var cliApp = cliBuilder.Build();
    var uniArgs = args.SkipWhile(a => a != "--universe").Skip(1).ToArray();
    Environment.ExitCode = await UniverseCli.RunAsync(uniArgs, cliApp.Services);
    return;
}

// ss --review-settings [--set <key> <value>] — view or update review voting settings.
// Keys: ballots, prose, panel, readers, max-concurrency, judge-provider, allowed-providers
if (args.Contains("--review-settings"))
{
    var cliBuilder = WebApplication.CreateBuilder(args);
    cliBuilder.Services.AddStreetSamuraiServices();
    var cliApp = cliBuilder.Build();
    Environment.ExitCode = await ReviewSettingsCli.RunAsync(args, cliApp.Services);
    return;
}

// ss --get <type> <name-or-id> — targeted entity lookup.
// Types: character | place | weapon | faction | corponation
if (args.Contains("--get"))
{
    var cliBuilder = WebApplication.CreateBuilder(args);
    cliBuilder.Services.AddStreetSamuraiServices();
    var cliApp = cliBuilder.Build();
    var getArgs = args.SkipWhile(a => a != "--get").Skip(1).ToArray();
    Environment.ExitCode = await GetEntityCli.RunAsync(getArgs, cliApp.Services);
    return;
}

// CLI mode: sync project-rule, Codex, and Claude Code memory .md files to DB.
// Upserts by RelativePath; only changed files (hash diff) produce a history row.
//   ss --sync-markdown [--dry-run]
if (args.Contains("--sync-markdown"))
{
    var cliBuilder = WebApplication.CreateBuilder(args);
    cliBuilder.Services.AddStreetSamuraiServices();
    var cliApp = cliBuilder.Build();
    Environment.ExitCode = await SyncMarkdownCli.RunAsync(args, cliApp.Services);
    return;
}

// CLI mode: restore .md files from DB back to disk. Supports point-in-time
// recovery from the MarkdownFiles_History temporal table.
//   ss --restore-markdown [--file <relativePath>] [--as-of <datetime-utc>] [--dry-run] [--list]
if (args.Contains("--restore-markdown"))
{
    var cliBuilder = WebApplication.CreateBuilder(args);
    cliBuilder.Services.AddStreetSamuraiServices();
    var cliApp = cliBuilder.Build();
    Environment.ExitCode = await RestoreMarkdownCli.RunAsync(args, cliApp.Services);
    return;
}

// ss --story-audit --slug <strandSlug> [--json]
// Audits a strand against 7 commandments — gateway (PreviousStrandId=null) or
// sequel (PreviousStrandId set). Pass/warn/fail per commandment with fix hints.
// Exit 0 = all pass, 1 = advisory warnings, 2 = blocking failures.
if (args.Contains("--story-audit"))
{
    var cliBuilder = WebApplication.CreateBuilder(args);
    cliBuilder.Services.AddStreetSamuraiServices();
    var cliApp = cliBuilder.Build();
    Environment.ExitCode = await StoryAuditCli.RunAsync(args, cliApp.Services);
    return;
}

// ss --plant-audit   --slug <strand> [--json]   audit plant/payoff pairs
// ss --list-plants   --slug <strand> [--json]   list all pairs
// ss --add-plant     --slug <strand> --plant "..." --payoff "..." [--cat detail]
if (args.Contains("--plant-audit") || args.Contains("--list-plants") || args.Contains("--add-plant"))
{
    var cliBuilder = WebApplication.CreateBuilder(args);
    cliBuilder.Services.AddStreetSamuraiServices();
    var cliApp = cliBuilder.Build();
    Environment.ExitCode = await PlantPayoffCli.RunAsync(args, cliApp.Services);
    return;
}

// CLI mode: Will Storr narrative-science frameworks — sacred flaw, dramatic question,
// scene anatomy, five-act structure. Four subcommands:
//   ss --narrative-science sacred-flaw --character <slug|id> [--scaffold]
//   ss --narrative-science dramatic-question (--slug <s> | --id <beatId>) [--character <slug|id>]
//   ss --narrative-science scene-anatomy (--slug <s> | --id <beatId>)
//   ss --narrative-science five-act --slug <strandSlug>
//   (add --json to any subcommand for raw JSON output)
if (args.Contains("--narrative-science"))
{
    var cliBuilder = WebApplication.CreateBuilder(args);
    cliBuilder.Services.AddStreetSamuraiServices();
    var cliApp = cliBuilder.Build();
    Environment.ExitCode = await NarrativeScienceCli.RunAsync(args, cliApp.Services);
    return;
}

var builder = WebApplication.CreateBuilder(args);

// Cloud-native configuration chain. Layered (later sources win):
//   AddJsonFile (already added by WebApplicationBuilder for appsettings.json).
//   AddMindAtticVaultFiles surfaces %APPDATA%\MindAttic\<bucket>\providers.json on dev
//     — the single credential source now that .NET User Secrets is retired.
//   AddEnvironmentVariables (already present) picks up App Service Application
//     Settings + Azure Key Vault references in production.
builder.Configuration
    .AddMindAtticVaultFiles(o => o.Buckets = new[]
    {
        // Default credential buckets PLUS "Security" — the MindAttic.Authentication
        // trust domain (pepper, bootstrap-token, reset-token-key). Without adding it
        // here the auth secrets at %APPDATA%\MindAttic\Security\providers.json would
        // not surface under MindAttic:Vault:Security in dev (env vars cover prod).
        "LLM", "Brokers", "Tokens", "Subtitles", "Notifications", "AudioStore", "Security",
    });

// Hand the host's IConfiguration to SettingsService BEFORE it's constructed so
// the very first ResolveApiKey() call sees Vault values. Static-field injection
// keeps SettingsService's constructor signature unchanged (tests still
// `new SettingsService()` without DI).
SettingsService.VaultConfiguration = builder.Configuration;

// Vault: cloud-native credential resolvers available via DI for any future
// service that wants to read from IConfiguration directly.
builder.Services.AddMindAtticVault(builder.Configuration);

// Configure Serilog — daily rolling log files in engine/logs/
var settings = new SettingsService();
var pathProvider = new FileSystemPathProvider(settings);
var logDir = pathProvider.LogDir;
Directory.CreateDirectory(logDir);

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
    .MinimumLevel.Override("System", LogEventLevel.Warning)
    .WriteTo.Console(outputTemplate: $"{{Timestamp:{settings.TimestampFormat}}} [{{Level:u3}}] {{Message:lj}}{{NewLine}}{{Exception}}")
    .WriteTo.File(
        Path.Combine(logDir, "log-.txt"),
        rollingInterval: RollingInterval.Day,
        outputTemplate: $"{{Timestamp:{settings.TimestampFormat}}} [{{Level:u3}}] {{Message:lj}}{{NewLine}}{{Exception}}",
        retainedFileCountLimit: 90,
        shared: true)
    .CreateLogger();

builder.Host.UseSerilog();

// Razor + interactive server rendering
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// All StreetSamurai services (repos, graph, LLM, TTS, etc.)
builder.Services.AddStreetSamuraiServices();

// ReadOnly mode: set from appsettings.ReadOnly.json when ASPNETCORE_ENVIRONMENT=ReadOnly
var readOnlyState = new ReadOnlyState { IsReadOnly = builder.Configuration.GetValue<bool>("ReadOnly") };
builder.Services.AddSingleton(readOnlyState);

// MindAttic.Authentication — replaces the bespoke cookie/AuthService stack. The
// library internally registers the cookie schemes (__Host-MindAttic.Auth + MfaPending),
// MaPolicies.Admin (role-only here — MFA is off), Data Protection, cascading auth state +
// a revalidating AuthenticationStateProvider, and every auth/user-admin service over
// StreetSamuraiAuthDbContext. Do NOT also call AddAuthentication/AddCookie/AddAuthorization/
// AddCascadingAuthenticationState — that would double-register and clobber the cookie.
builder.Services.AddMindAtticAuthentication<StreetSamuraiAuthDbContext>(
    builder.Configuration,
    o =>
    {
        o.AppName = "StreetSamurai";                          // per-app Data Protection trust boundary
        o.IsProduction = !builder.Environment.IsDevelopment();
        if (o.IsProduction)
        {
            // PROD: persist + protect the Data Protection key ring (the library fail-closes
            // if this isn't supplied in prod). Blob holds the ring; Key Vault wraps it.
            // DataProtection:BlobUri / DataProtection:KeyVaultKeyId come from App Service
            // settings, resolved via the same managed identity used for SQL.
            o.ConfigureDataProtection = dp =>
            {
                var cred = new Azure.Identity.DefaultAzureCredential();
                var blobUri = builder.Configuration["DataProtection:BlobUri"]
                    ?? throw new InvalidOperationException("DataProtection:BlobUri is required in production.");
                var kvKeyId = builder.Configuration["DataProtection:KeyVaultKeyId"]
                    ?? throw new InvalidOperationException("DataProtection:KeyVaultKeyId is required in production.");
                dp.PersistKeysToAzureBlobStorage(new Uri(blobUri), cred)
                  .ProtectKeysWithAzureKeyVault(new Uri(kvKeyId), cred);
            };
        }
        // DEV: the library persists the key ring to %APPDATA%\MindAttic\DataProtection\StreetSamurai.
    });

// Per-IP rate limiting on login endpoint — prevents credential stuffing without DoS'ing legit users
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("login", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            // Partition by IP — each IP gets its own rate limit window
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,                   // 10 attempts per IP per window
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,                     // reject immediately, don't queue
            }));
});

// IWriteAccessProvider — Blazor implementation checks auth claims + ReadOnlyState
builder.Services.AddScoped<IWriteAccessProvider, BlazorWriteAccessProvider>();

// Toast wrapper — shows toast + logs [SS CODE] to browser console
builder.Services.AddScoped<ToastNotifier>();

// Tab bar state — one per browser connection
builder.Services.AddScoped<StreetSamurai.Shared.Services.TabService>();

var app = builder.Build();

// Multi-universe: construct the universe context now so UniverseScope.Current is
// live before the first request or background service queries canon — otherwise
// early reads would run unscoped. Honors a --universe flag / SS_UNIVERSE on the host.
app.Services.GetRequiredService<IUniverseContext>();

// ── Auth startup orchestration ───────────────────────────────────────────
// Strict order: migrate (schema) → import (legacy UserAccount → AuthUser) → seed
// (bootstrap admin, only if NO users exist). MigrateAsync is DEV-ONLY: in prod the
// App Service managed identity cannot run DDL, so the auth EF migration rides the CI
// migrate job (ApplyMigrations) under db_ddladmin; import + seed are DDL-free and safe
// at prod startup over the already-migrated schema.
using (var scope = app.Services.CreateScope())
{
    var sp = scope.ServiceProvider;
    if (app.Environment.IsDevelopment())
    {
        var authDb = sp.GetRequiredService<StreetSamuraiAuthDbContext>();
        await authDb.Database.MigrateAsync();
    }
    var imported = await sp.GetRequiredService<StreetSamurai.Core.Services.AuthUserImportService>().ImportAsync();
    Log.Information("Auth user import: {Count} legacy account(s) migrated.", imported);
    await sp.GetRequiredService<MindAttic.Authentication.Services.AuthBootstrapper>().SeedAdminAsync();
}

// Background-instantiate services that subscribe to events at construction.
// Doing this synchronously on the startup path used to block app.Run() for
// 30-60 s while WorldGraphService.EnsureLoaded ran a full SQL rebuild — the host
// didn't serve a single request until that finished. Fire-and-forget on a
// Task.Run lets the host come up immediately; the subscriptions wire in a few
// seconds later, well before any user can save a chapter.
_ = Task.Run(() =>
{
    try
    {
        _ = app.Services.GetRequiredService<StreetSamurai.Core.Services.ContinuousQualityService>();
    }
    catch (Exception ex) { Log.Fatal(ex, "Background-instantiate ContinuousQualityService failed — chapter-save quality scan will not run"); }

    try
    {
        _ = app.Services.GetRequiredService<StreetSamurai.Core.Services.BeatStateExtractor>();
    }
    catch (Exception ex) { Log.Fatal(ex, "Background-instantiate BeatStateExtractor failed — beat state extraction on save will not run"); }
});

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

// Honor the App Service reverse proxy's forwarded scheme/IP so Request.Scheme is
// https (secure cookie issuance + no redirect loop) and the rate limiter / audit see
// the real client IP. Must run before the auth middleware.
var forwardedHeaders = new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
};
forwardedHeaders.KnownIPNetworks.Clear();
forwardedHeaders.KnownProxies.Clear();
// Trust RFC-1918 private address space so App Service's X-Forwarded-Proto is honored.
forwardedHeaders.KnownIPNetworks.Add(new System.Net.IPNetwork(IPAddress.Parse("10.0.0.0"), 8));
forwardedHeaders.KnownIPNetworks.Add(new System.Net.IPNetwork(IPAddress.Parse("172.16.0.0"), 12));
forwardedHeaders.KnownIPNetworks.Add(new System.Net.IPNetwork(IPAddress.Parse("192.168.0.0"), 16));
app.UseForwardedHeaders(forwardedHeaders);

app.UseRateLimiter();

// MindAttic.Authentication: UseAuthentication + UseAuthorization + the forced-step
// redirect (MustChangePassword → /account/change-password, claim-driven, no DB hit) +
// a scoped CSP on the auth surface. Replaces the bespoke UseAuthentication/UseAuthorization
// and the hand-rolled MustChangePassword middleware.
app.UseMindAtticAuthentication();

app.UseAntiforgery();

app.MapStaticAssets();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddAdditionalAssemblies(typeof(StreetSamurai.Shared.Components.Pages.Home).Assembly);

// MindAttic.Authentication HTTP endpoints — /_ma-auth/{login,mfa-challenge,logout,
// change-password,reset/request,reset/confirm}. These OWN sign-in (the Razor components
// only render the antiforgery-protected forms that post here).
app.MapMindAtticAuthEndpoints(group => group.RequireRateLimiting("login"));

// Episode audio: serve the per-beat MP3 files the /listen page plays.
// File path is engine/audio/episodes/{episodeId}/{index:D3}.mp3 — bound to the
// EpisodeAudioService's GetAudioRoot() so the two stay in sync.
// Episode artifact endpoints — keyed by Guid (UUIDv7) for stable URLs.
// Each endpoint resolves the slug from the DB (one AsNoTracking query) then
// joins with EpisodeAudioService's path helpers to find the file on disk.
static async Task<string?> ResolveSlugAsync(
    Microsoft.EntityFrameworkCore.IDbContextFactory<StreetSamurai.Core.Data.StreetSamuraiDbContext> dbFactory,
    Guid episodeId)
{
    await using var db = await dbFactory.CreateDbContextAsync();
    var slug = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions
        .FirstOrDefaultAsync(
            Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions
                .AsNoTracking(db.Episodes)
                .Where(e => e.Id == episodeId)
                .Select(e => e.Slug));
    if (string.IsNullOrWhiteSpace(slug)) return null;
    return slug;
}

app.MapGet("/api/episodes/{episodeId:guid}/audio/{index:int}", async (
    Guid episodeId,
    int index,
    StreetSamurai.Core.Services.EpisodeAudioService audio,
    Microsoft.EntityFrameworkCore.IDbContextFactory<StreetSamurai.Core.Data.StreetSamuraiDbContext> dbFactory,
    HttpContext ctx) =>
{
    var slug = await ResolveSlugAsync(dbFactory, episodeId);
    if (slug is null) { ctx.Response.StatusCode = 404; return; }
    var audioDir = System.IO.Path.Combine(audio.GetEpisodeRoot(slug), "audio");
    // Probe lossless first, then MP3 fallback.
    var wavPath = System.IO.Path.Combine(audioDir, $"{index:D3}.wav");
    var mp3Path = System.IO.Path.Combine(audioDir, $"{index:D3}.mp3");
    if (System.IO.File.Exists(wavPath))
    {
        ctx.Response.ContentType = "audio/wav";
        await ctx.Response.SendFileAsync(wavPath);
        return;
    }
    if (System.IO.File.Exists(mp3Path))
    {
        ctx.Response.ContentType = "audio/mpeg";
        await ctx.Response.SendFileAsync(mp3Path);
        return;
    }
    ctx.Response.StatusCode = 404;
}).RequireAuthorization();

app.MapGet("/api/episodes/{episodeId:guid}/script.md", async (
    Guid episodeId,
    StreetSamurai.Core.Services.EpisodeAudioService audio,
    Microsoft.EntityFrameworkCore.IDbContextFactory<StreetSamurai.Core.Data.StreetSamuraiDbContext> dbFactory,
    HttpContext ctx) =>
{
    var slug = await ResolveSlugAsync(dbFactory, episodeId);
    if (slug is null) { ctx.Response.StatusCode = 404; return; }
    var path = System.IO.Path.Combine(audio.GetEpisodeRoot(slug), "script.md");
    if (!System.IO.File.Exists(path)) { ctx.Response.StatusCode = 404; return; }
    ctx.Response.ContentType = "text/markdown; charset=utf-8";
    ctx.Response.Headers.ContentDisposition = $"attachment; filename=\"{slug}.md\"";
    await ctx.Response.SendFileAsync(path);
}).RequireAuthorization();

app.MapGet("/api/episodes/{episodeId:guid}/script.pdf", async (
    Guid episodeId,
    StreetSamurai.Core.Services.EpisodeAudioService audio,
    Microsoft.EntityFrameworkCore.IDbContextFactory<StreetSamurai.Core.Data.StreetSamuraiDbContext> dbFactory,
    HttpContext ctx) =>
{
    var slug = await ResolveSlugAsync(dbFactory, episodeId);
    if (slug is null) { ctx.Response.StatusCode = 404; return; }
    var path = System.IO.Path.Combine(audio.GetEpisodeRoot(slug), "script.pdf");
    if (!System.IO.File.Exists(path)) { ctx.Response.StatusCode = 404; return; }
    ctx.Response.ContentType = "application/pdf";
    ctx.Response.Headers.ContentDisposition = $"attachment; filename=\"{slug}.pdf\"";
    await ctx.Response.SendFileAsync(path);
}).RequireAuthorization();

app.MapGet("/api/episodes/{episodeId:guid}/episode.wav", async (
    Guid episodeId,
    StreetSamurai.Core.Services.EpisodeAudioService audio,
    Microsoft.EntityFrameworkCore.IDbContextFactory<StreetSamurai.Core.Data.StreetSamuraiDbContext> dbFactory,
    HttpContext ctx) =>
{
    var slug = await ResolveSlugAsync(dbFactory, episodeId);
    if (slug is null) { ctx.Response.StatusCode = 404; return; }
    var dir = audio.GetEpisodeRoot(slug);
    // The combined export lands as .wav (lossless tier) or .mp3 (MP3 tier).
    // The URL keeps its historical name but we serve whichever exists.
    var wavPath = System.IO.Path.Combine(dir, "episode.wav");
    var mp3Path = System.IO.Path.Combine(dir, "episode.mp3");
    if (System.IO.File.Exists(wavPath))
    {
        ctx.Response.ContentType = "audio/wav";
        ctx.Response.Headers.ContentDisposition = $"attachment; filename=\"{slug}.wav\"";
        await ctx.Response.SendFileAsync(wavPath);
        return;
    }
    if (System.IO.File.Exists(mp3Path))
    {
        ctx.Response.ContentType = "audio/mpeg";
        ctx.Response.Headers.ContentDisposition = $"attachment; filename=\"{slug}.mp3\"";
        await ctx.Response.SendFileAsync(mp3Path);
        return;
    }
    ctx.Response.StatusCode = 404;
}).RequireAuthorization();

// ── Unified strand audio endpoints ───────────────────────────────────────
// Per-beat audio served from engine/strands/{slug}/audio/{beatId}.{wav|mp3}.
// File names are Beat.Id ("N" format) so a beat in multiple strands has one
// rendering — the file path is keyed on the beat, not the strand.
app.MapGet("/api/strands/{strandId:guid}/beat/{beatId:guid}/audio", async (
    Guid strandId,
    Guid beatId,
    StreetSamurai.Core.Interfaces.IAudioStore audioStore,
    Microsoft.EntityFrameworkCore.IDbContextFactory<StreetSamurai.Core.Data.StreetSamuraiDbContext> dbFactory) =>
{
    await using var db = await dbFactory.CreateDbContextAsync();
    // Validate that this beat is actually a member of the strand in the URL.
    // Without this check, an authenticated user with any beat GUID could pull
    // its audio by inventing any strand GUID — the strandId segment was
    // decorative. The unique (StrandId, BeatId) PK on StrandBeats makes this
    // an index seek, ~free at any scale.
    var isMember = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions
        .AnyAsync(db.StrandBeats.Where(sb => sb.StrandId == strandId && sb.BeatId == beatId));
    if (!isMember) return Results.NotFound();
    var beat = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions
        .FirstOrDefaultAsync(
            Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.AsNoTracking(db.Beats)
                .Where(b => b.Id == beatId));
    if (beat?.AudioPath is null) return Results.NotFound();
    var contentType = beat.AudioPath.EndsWith(".mp3", StringComparison.OrdinalIgnoreCase) ? "audio/mpeg" : "audio/wav";

    // Local backend → Results.File(path) which uses sendfile() and gets
    // range support for free. Blob backend → Results.File(stream) — the
    // BlobClient stream is seekable, so enableRangeProcessing lets the
    // browser scrub long combined-audio without downloading the whole
    // thing. Same Results.File API for both paths.
    var localPath = await audioStore.ResolveLocalPathAsync(beat.AudioPath);
    if (localPath != null)
        return Results.File(localPath, contentType, enableRangeProcessing: true);

    var stream = await audioStore.OpenReadAsync(beat.AudioPath);
    if (stream == null) return Results.NotFound();
    return Results.File(stream, contentType, enableRangeProcessing: true);
}).RequireAuthorization();

app.MapGet("/api/strands/{strandId:guid}/strand.wav", async (
    Guid strandId,
    StreetSamurai.Core.Interfaces.IAudioStore audioStore,
    Microsoft.EntityFrameworkCore.IDbContextFactory<StreetSamurai.Core.Data.StreetSamuraiDbContext> dbFactory) =>
{
    await using var db = await dbFactory.CreateDbContextAsync();
    var strand = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions
        .FirstOrDefaultAsync(
            Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.AsNoTracking(db.Strands)
                .Where(s => s.Id == strandId));
    if (strand is null) return Results.NotFound();

    // Try the WAV first, then MP3, then legacy episode-era filenames.
    // Both formats are valid outputs from ExportCombinedAsync depending on
    // whether the strand's beats were narrated as lossless PCM or MP3.
    string? rel = null, contentType = null, filenameExt = null;
    var candidates = new[]
    {
        ($"{strand.Slug}/strand.wav",  "audio/wav",  "wav"),
        ($"{strand.Slug}/strand.mp3",  "audio/mpeg", "mp3"),
        ($"{strand.Slug}/episode.wav", "audio/wav",  "wav"),
        ($"{strand.Slug}/episode.mp3", "audio/mpeg", "mp3"),
    };
    foreach (var (r, t, e) in candidates)
    {
        if (await audioStore.ExistsAsync(r)) { rel = r; contentType = t; filenameExt = e; break; }
    }
    if (rel == null) return Results.NotFound();

    var fileDownloadName = $"{strand.Slug}.{filenameExt}";
    var localPath = await audioStore.ResolveLocalPathAsync(rel);
    if (localPath != null)
        return Results.File(localPath, contentType!, fileDownloadName, enableRangeProcessing: true);

    var stream = await audioStore.OpenReadAsync(rel);
    if (stream == null) return Results.NotFound();
    return Results.File(stream, contentType!, fileDownloadName, enableRangeProcessing: true);
}).RequireAuthorization();

// Media file endpoint — serves {entityId}.{index}.{ext} files from engine/data/media/
app.MapGet("/api/media/{filename}", (string filename, MediaService media) =>
{
    var path = media.GetPath(filename);
    if (path == null) return Results.NotFound();
    var mime = MediaService.GetMimeType(filename);
    return Results.File(path, mime, enableRangeProcessing: true);
});

Log.Information("StreetSamurai Blazor host started");

try
{
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
