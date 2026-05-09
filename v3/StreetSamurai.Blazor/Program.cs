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

// Eager-instantiate background services that subscribe to events at construction.
// Wrapped in try/Log.Fatal so a ctor failure doesn't silently swallow itself —
// without this the host comes up but the service never wires its OnChapterSaved
// subscription, and the symptom is "saves don't trigger findings" with no log line.
try
{
    // ContinuousQualityService subscribes to IChapterRepository.OnChapterSaved for
    // autonomous contradiction/cliché scans against the cloud LLM.
    _ = app.Services.GetRequiredService<StreetSamurai.Core.Services.ContinuousQualityService>();
}
catch (Exception ex) { Log.Fatal(ex, "Eager-instantiate ContinuousQualityService failed — chapter-save quality scan will not run"); }

try
{
    // Eager-instantiate the BeatStateExtractor so its OnChapterSaved subscription
    // is live before any chapter save can happen.
    _ = app.Services.GetRequiredService<StreetSamurai.Core.Services.BeatStateExtractor>();
}
catch (Exception ex) { Log.Fatal(ex, "Eager-instantiate BeatStateExtractor failed — beat state extraction on save will not run"); }

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
