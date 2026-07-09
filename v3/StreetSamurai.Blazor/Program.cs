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
using StreetSamurai.Blazor.Components;
using StreetSamurai.Blazor.Services;
using StreetSamurai.Core.Extensions;
using StreetSamurai.Core.Interfaces;
using StreetSamurai.Core.Models;
using StreetSamurai.Core.Services;
using StreetSamurai.Shared.Services;
using MindAttic.Media;
using MindAttic.Vault.Configuration;
using MindAttic.Vault.DependencyInjection;
using StreetSamurai.Core.Services;

UniverseBootstrap.RequestedSlug ??= UniverseBootstrap.ParseSlug(args);

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
    .AddAdditionalAssemblies(typeof(StreetSamurai.Shared.Components.Pages.Login).Assembly);

// MindAttic.Authentication HTTP endpoints — /_ma-auth/{login,mfa-challenge,logout,
// change-password,reset/request,reset/confirm}. These OWN sign-in (the Razor components
// only render the antiforgery-protected forms that post here).
app.MapMindAtticAuthEndpoints(group => group.RequireRateLimiting("login"));

// Media file serve endpoint — GET /_media/{uid:guid}
// Local disk files stream inline; Azure Blob URIs redirect to the blob URL.
app.MapMediaEndpoints();

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

// ── Unified node audio endpoints ───────────────────────────────────────
// Per-beat audio served from engine/strands/{slug}/audio/{beatId}.{wav|mp3}.
// File names are Beat.Id ("N" format) so a beat in multiple nodes has one
// rendering — the file path is keyed on the beat, not the node.
app.MapGet("/api/nodes/{nodeId:guid}/beat/{beatId:guid}/audio", async (
    Guid nodeId,
    Guid beatId,
    StreetSamurai.Core.Interfaces.IAudioStore audioStore,
    Microsoft.EntityFrameworkCore.IDbContextFactory<StreetSamurai.Core.Data.StreetSamuraiDbContext> dbFactory) =>
{
    await using var db = await dbFactory.CreateDbContextAsync();
    // Validate that this beat is actually a member of the node in the URL.
    // Without this check, an authenticated user with any beat GUID could pull
    // its audio by inventing any node GUID — the nodeId segment was
    // decorative. The unique (NodeId, BeatId) PK on BeatNodes makes this
    // an index seek, ~free at any scale.
    var isMember = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions
        .AnyAsync(db.BeatNodes.Where(sb => sb.NodeId == nodeId && sb.BeatId == beatId));
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

app.MapGet("/api/nodes/{nodeId:guid}/node.wav", async (
    Guid nodeId,
    StreetSamurai.Core.Interfaces.IAudioStore audioStore,
    Microsoft.EntityFrameworkCore.IDbContextFactory<StreetSamurai.Core.Data.StreetSamuraiDbContext> dbFactory) =>
{
    await using var db = await dbFactory.CreateDbContextAsync();
    var node = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions
        .FirstOrDefaultAsync(
            Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.AsNoTracking(db.Nodes)
                .Where(s => s.Id == nodeId));
    if (node is null) return Results.NotFound();

    // Try the WAV first, then MP3, then legacy episode-era filenames.
    // Both formats are valid outputs from ExportCombinedAsync depending on
    // whether the node's beats were narrated as lossless PCM or MP3.
    string? rel = null, contentType = null, filenameExt = null;
    var candidates = new[]
    {
        ($"{node.Slug}/node.wav",  "audio/wav",  "wav"),
        ($"{node.Slug}/node.mp3",  "audio/mpeg", "mp3"),
        ($"{node.Slug}/episode.wav", "audio/wav",  "wav"),
        ($"{node.Slug}/episode.mp3", "audio/mpeg", "mp3"),
    };
    foreach (var (r, t, e) in candidates)
    {
        if (await audioStore.ExistsAsync(r)) { rel = r; contentType = t; filenameExt = e; break; }
    }
    if (rel == null) return Results.NotFound();

    var fileDownloadName = $"{node.Slug}.{filenameExt}";
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

// ── Distributed worker REST API ───────────────────────────────────────────────
// Auth: X-Worker-Key header must match WorkerSettings:ApiKey in appsettings / env.
// Workers are stateless: they claim work, run the local LLM, and POST results back.
// The coordinator (this process) is the only writer to EntityReviews, NodeReviews, Beats, Edges.

static bool WorkerAuthOk(HttpContext ctx, IConfiguration cfg)
{
    var expected = cfg["WorkerSettings:ApiKey"] ?? "";
    if (string.IsNullOrWhiteSpace(expected)) return false; // key not configured → deny all
    ctx.Request.Headers.TryGetValue("X-Worker-Key", out var provided);
    return provided.ToString() == expected;
}

// GET /api/worker/status — queue counts by work type and status
app.MapGet("/api/worker/status", async (
    StreetSamurai.Core.Services.DistributedWorkerCoordinator coordinator,
    IConfiguration cfg, HttpContext ctx) =>
{
    if (!WorkerAuthOk(ctx, cfg)) return Results.Unauthorized();
    var rows = await coordinator.GetStatusAsync();
    return Results.Ok(rows);
});

// GET /api/worker/claim?workerId=X&workType=entity-review&batch=20
app.MapGet("/api/worker/claim", async (
    string workerId, string workType, int batch,
    StreetSamurai.Core.Services.DistributedWorkerCoordinator coordinator,
    IConfiguration cfg, HttpContext ctx) =>
{
    if (!WorkerAuthOk(ctx, cfg)) return Results.Unauthorized();
    batch = Math.Clamp(batch, 1, 100);
    var items = await coordinator.ClaimBatchAsync(workerId, workType, batch);
    return Results.Ok(items);
});

// POST /api/worker/submit — worker posts completed results
app.MapPost("/api/worker/submit", async (
    StreetSamurai.Core.Services.WorkerResult result,
    StreetSamurai.Core.Services.DistributedWorkerCoordinator coordinator,
    IConfiguration cfg, HttpContext ctx) =>
{
    if (!WorkerAuthOk(ctx, cfg)) return Results.Unauthorized();
    var submitResult = await coordinator.SubmitAsync(result);
    return Results.Ok(submitResult);
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