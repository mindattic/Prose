using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.RateLimiting;
using Serilog;
using Serilog.Events;
using StreetSamurai.Blazor.Auth;
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
    .AddMindAtticVaultFiles();

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

// Cookie authentication — hardened for production
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/login";
        options.LogoutPath = "/api/auth/logout";
        options.ExpireTimeSpan = TimeSpan.FromDays(30);
        options.SlidingExpiration = true;
        options.Cookie.HttpOnly = true;                              // No JS access
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;     // HTTPS only
        options.Cookie.SameSite = SameSiteMode.Strict;               // No cross-site requests
        options.Cookie.Name = "__Host-SS-Auth";                       // __Host- prefix: browser enforces Secure+Path=/

        // Validate SecurityStamp on every request — rejects sessions after password/role change
        options.Events.OnValidatePrincipal = async context =>
        {
            var userId = context.Principal?.FindFirstValue("UserId");
            var stamp = context.Principal?.FindFirstValue("SecurityStamp");
            if (userId == null || stamp == null)
            {
                context.RejectPrincipal();
                await context.HttpContext.SignOutAsync();
                return;
            }

            // Dev auto-login pseudo-user — skip DB validation.
            // DevAutoLoginMiddleware only runs in Development; in production only real users exist.
            if (userId == "dev-auto-login") return;

            var userRepo = context.HttpContext.RequestServices.GetRequiredService<UserRepository>();
            var user = userRepo.GetById(userId);
            if (user == null || user.SecurityStamp != stamp)
            {
                context.RejectPrincipal();
                await context.HttpContext.SignOutAsync();
            }
        };
    });
builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();

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

app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

// Dev-only: auto-login as admin (reads DevAuth section from appsettings.Development.json)
if (app.Environment.IsDevelopment() && app.Configuration.GetSection("DevAuth").Exists())
{
    app.UseMiddleware<DevAutoLoginMiddleware>();
}

// Enforce MustChangePassword: redirect users who haven't changed their forced password.
// Without this, a user could navigate directly to any page and bypass the requirement.
app.Use(async (context, next) =>
{
    var path = context.Request.Path.Value ?? "";
    // Skip enforcement for: static files, API endpoints, the change-password page itself, and login
    if (path.StartsWith("/_") || path.StartsWith("/api/") || path == "/change-password" || path == "/login")
    {
        await next();
        return;
    }

    var userId = context.User?.FindFirst("UserId")?.Value;
    if (!string.IsNullOrEmpty(userId))
    {
        var userRepo = context.RequestServices.GetRequiredService<UserRepository>();
        var user = userRepo.GetById(userId);
        if (user?.MustChangePassword == true)
        {
            context.Response.Redirect("/change-password");
            return;
        }
    }

    await next();
});

app.UseAntiforgery();

app.MapStaticAssets();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddAdditionalAssemblies(typeof(StreetSamurai.Shared.Components.Pages.Home).Assembly);

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

// Login endpoint — form POST from Login.razor, with antiforgery + open redirect + rate limiting
app.MapPost("/api/auth/login", async (HttpContext ctx, AuthService auth, IAntiforgery antiforgery) =>
{
    // Validate CSRF token
    try { await antiforgery.ValidateRequestAsync(ctx); }
    catch (AntiforgeryValidationException)
    {
        ctx.Response.StatusCode = 400;
        return;
    }

    var form = await ctx.Request.ReadFormAsync();
    var email = form["email"].ToString();
    var password = form["password"].ToString();
    var returnUrl = form["returnUrl"].ToString();

    // Open redirect protection: only allow local paths
    if (!AuthService.IsLocalUrl(returnUrl)) returnUrl = "/";

    var user = auth.Authenticate(email, password);
    if (user == null)
    {
        ctx.Response.Redirect("/login?error=invalid");
        return;
    }

    var claims = new[]
    {
        new Claim(ClaimTypes.Name, user.DisplayName),
        new Claim(ClaimTypes.Email, user.Email),
        new Claim(ClaimTypes.Role, user.Role),
        new Claim("UserId", user.Id),
        new Claim("SecurityStamp", user.SecurityStamp),
    };
    var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
    var principal = new ClaimsPrincipal(identity);

    await ctx.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

    // Clear the dev-logout cookie so DevAutoLoginMiddleware works again if needed
    if (app.Environment.IsDevelopment())
        ctx.Response.Cookies.Delete("ss-dev-logout");

    // Force password change on first login (seeded admin, or admin-flagged accounts)
    if (user.MustChangePassword)
        ctx.Response.Redirect("/change-password");
    else
        ctx.Response.Redirect(returnUrl);
}).RequireRateLimiting("login");

// Logout endpoint — with antiforgery
app.MapPost("/api/auth/logout", async (HttpContext ctx, IAntiforgery antiforgery) =>
{
    try { await antiforgery.ValidateRequestAsync(ctx); }
    catch (AntiforgeryValidationException)
    {
        ctx.Response.StatusCode = 400;
        return;
    }

    await ctx.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    // In dev mode, set a cookie so DevAutoLoginMiddleware doesn't immediately re-login
    if (app.Environment.IsDevelopment())
        ctx.Response.Cookies.Append("ss-dev-logout", "1", new CookieOptions { Path = "/" });
    ctx.Response.Redirect("/");
});

// Media file endpoint — serves {entityId}.{index}.{ext} files from engine/data/media/
app.MapGet("/api/media/{filename}", (string filename, MediaService media) =>
{
    var path = media.GetPath(filename);
    if (path == null) return Results.NotFound();
    var mime = MediaService.GetMimeType(filename);
    return Results.File(path, mime, enableRangeProcessing: true);
});

// Open redirect protection is now in AuthService.IsLocalUrl() — single source of truth, unit-testable.

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
