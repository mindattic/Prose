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

// CLI mode: dotnet run --project ... -- --rebuild-graph
// Rebuilds world_graph.json from source data without starting the web server.
if (args.Contains("--rebuild-graph"))
{
    var cliBuilder = WebApplication.CreateBuilder(args);
    cliBuilder.Services.AddStreetSamuraiServices();
    var cliApp = cliBuilder.Build();
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

// CLI mode: generate a new strand from a user-supplied seed.
//   ss --write-strand --seed "..." [--voice id] [--kind episode] [--title "..."] [--narrate]
if (args.Contains("--write-strand"))
{
    var cliBuilder = WebApplication.CreateBuilder(args);
    cliBuilder.Services.AddStreetSamuraiServices();
    var cliApp = cliBuilder.Build();
    Environment.ExitCode = await WriteStrandCli.RunAsync(args, cliApp.Services);
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

// CLI mode: set the ParentStrandId on an existing strand (move it into a collection).
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
// drop the MP3 in Downloads. The headless twin of the "Publish Audiobook" button.
//   ss --publish-audiobook (--id <guid|prefix> | --slug <slug>)
if (args.Contains("--publish-audiobook"))
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

// CLI mode: render a strand to Markdown / plain text / PDF in Downloads. The
// headless twins of the writer page's Export dropdown items.
//   ss (--publish-md | --publish-txt | --publish-pdf) (--id <guid|prefix> | --slug <slug>) [--author "Name"]
if (args.Contains("--publish-md") || args.Contains("--publish-txt") || args.Contains("--publish-pdf"))
{
    var cliBuilder = WebApplication.CreateBuilder(args);
    cliBuilder.Services.AddStreetSamuraiServices();
    var cliApp = cliBuilder.Build();
    var format = args.Contains("--publish-md") ? PublishManuscriptCli.Format.Markdown
               : args.Contains("--publish-txt") ? PublishManuscriptCli.Format.Text
               : PublishManuscriptCli.Format.Pdf;
    Environment.ExitCode = await PublishManuscriptCli.RunAsync(args, cliApp.Services, format);
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

var app = builder.Build();

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
