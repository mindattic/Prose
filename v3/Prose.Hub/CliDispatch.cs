using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Prose.Core.Data;
using Prose.Core.Data.Entities;
using Prose.Core.Services;

namespace Prose.Hub;

/// <summary>
/// Generic reflection-based invoker for the Prose Hub's CLI-command migration (Phase 2 /
/// Stage C) — the same shape as ToolDispatch.cs, applied to Prose.Cli's `Cli/*.cs` handler
/// classes instead of Prose.Mcp's tool classes.
///
/// Unlike the MCP tools (which needed an Impl/forward split per method), every Cli handler
/// already shares one of two uniform signatures — `public static (async )?Task&lt;int&gt;
/// RunAsync(string[] args, IServiceProvider services)` or `public static int Run(string[]
/// args, IServiceProvider services)` — so no source split is needed at all. Program.cs's
/// dispatch chain already determines WHICH handler class to call; each migrated block just
/// forwards the handler's name instead of running it in-process.
///
/// Referenced classes live in Prose.Mcp... no — Prose.Cli.dll (this project has a
/// ProjectReference to it). Not circular: Prose.Cli never references Prose.Hub at compile
/// time, only calls it over HTTP at runtime (see Prose.Cli's HubCliClient).
/// </summary>
public static class CliDispatch
{
    // Method defaults to null: tries "RunAsync" then "Run" (the ~150-command common case).
    // Pass it explicitly for handlers with a differently-named entry point (e.g.
    // GlossaryCli.RunBookAsync, AutoCorrectUndoCli.RunStatusAsync) - see HubCliClient.ForwardAsync.
    // ExtraParamValue: for the handlers that take a third parameter beyond args/services -
    // either an enum the caller already resolved client-side (PublishManuscriptCli's Format,
    // from which of two mutually-exclusive flags was passed) or a plain string
    // (BeatLensCli's lens - "causality"/"affect"/"interpersonal") - parsed into whichever
    // parameter isn't IServiceProvider/string[].
    // Cwd/Stdin: the Hub runs as its own long-lived process (its own working directory, no
    // real stdin) — a migrated handler that resolves a relative --file path or reads
    // Console.In (the "-" stdin sentinel convention shared by BeatCli/ImportMarkdownCli/
    // ImportNodeCli/ReimportNodeCli/DocContextHookCli) would silently do the wrong thing if
    // just invoked in-process. Threading the CALLER's cwd/stdin through and applying them
    // for the duration of the call (same ConsoleGate-serialized in/restore pattern as
    // Console.Out/Error below) makes every such handler behave exactly as it did running
    // in-process, with zero changes to the handler classes themselves.
    public sealed record InvokeRequest(string HandlerClass, string[] Args, string? Universe, string? Method = null, string? ExtraParamValue = null, string? Cwd = null, string? Stdin = null);
    public sealed record InvokeResponse(int ExitCode, string Output, string Error);

    // Console.Out/Console.Error are process-wide statics, same hazard class as the
    // multi-universe bleed bug this whole migration already fixed once (UniverseScope) - two
    // concurrent CLI invocations redirecting Console.Out at the same time would scramble each
    // other's captured output. Serializing through this gate is the correct fix for a
    // personal-use tool where CLI commands are infrequent and typically longer-running than a
    // graph query anyway - throughput isn't the concern here, correctness is.
    private static readonly SemaphoreSlim ConsoleGate = new(1, 1);

    // Handler-class lookup is cacheable: the Hub's loaded assemblies never change after
    // startup, so re-running AppDomain.CurrentDomain.GetAssemblies().SelectMany(GetTypes())
    // on every single one of the ~150 CLI commands (now the hot path for all Prose command
    // execution, post-migration) was a real, avoidable per-call cost. Keyed by simple name,
    // same lookup shape the uncached version used.
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, Type?> HandlerTypeCache = new();

    private static Type? ResolveHandlerType(string handlerClass) =>
        HandlerTypeCache.GetOrAdd(handlerClass, static name =>
            AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => { try { return a.GetTypes(); } catch { return []; } })
                .FirstOrDefault(t => t.Name == name && t.Namespace == "Prose.Cli"));

    public static async Task<IResult> InvokeAsync(InvokeRequest req, IServiceProvider sp)
    {
        var outcome = await ExecuteCoreAsync(req, sp);
        return outcome.ErrorCode != null
            ? Results.NotFound(outcome.ErrorDetail)
            : Results.Ok(outcome.Response);
    }

    // Split out of InvokeAsync so the cost-gate endpoint (CostGateDispatch.cs) can wrap the
    // exact same execution with a before/after TokenLedger snapshot instead of duplicating
    // this whole reflection + console/cwd/stdin/universe dance a second time.
    public sealed record ExecuteOutcome(string? ErrorCode, object? ErrorDetail, InvokeResponse? Response);

    // Command Ledger (Part A of the observability plan, 2026-08-20): every CLI command this
    // dispatcher runs becomes a permanent, best-effort DB row - the durable "what did the Hub
    // actually do" record, so nothing depends on a conversation's memory to reconstruct it.
    // Source="cli" here; CostGateDispatch's calls into ExecuteCoreAsync are tagged "cost-gate"
    // by that caller instead (it wraps this same method, see CostGateDispatch.cs).
    public static async Task<ExecuteOutcome> ExecuteCoreAsync(InvokeRequest req, IServiceProvider sp, string source = "cli")
    {
        var label = req.HandlerClass + (string.IsNullOrWhiteSpace(req.Method) ? "" : $".{req.Method}");
        HubConsoleEcho.LogIn(source, label, string.Join(' ', req.Args));

        var sw = Stopwatch.StartNew();
        var outcome = await ExecuteCoreInnerAsync(req, sp);
        sw.Stop();

        HubConsoleEcho.LogOut(source, label,
            success: outcome.ErrorCode == null && outcome.Response?.ExitCode == 0,
            outputChars: outcome.Response?.Output.Length ?? 0,
            elapsedMs: sw.Elapsed.TotalMilliseconds,
            error: outcome.ErrorCode ?? outcome.Response?.Error);

        await WriteLedgerEntryAsync(req, sp, source, outcome, sw.Elapsed.TotalMilliseconds);
        return outcome;
    }

    private static async Task WriteLedgerEntryAsync(InvokeRequest req, IServiceProvider sp, string source, ExecuteOutcome outcome, double durationMs)
    {
        try
        {
            var dbFactory = sp.GetRequiredService<IDbContextFactory<ProseDbContext>>();
            await using var db = await dbFactory.CreateDbContextAsync();
            var output = outcome.Response?.Output ?? "";
            db.CommandLedgerEntries.Add(new CommandLedgerEntry
            {
                Source = source,
                HandlerClass = req.HandlerClass,
                Method = req.Method,
                ArgsJson = JsonSerializer.Serialize(req.Args),
                Universe = req.Universe,
                ExitCode = outcome.Response?.ExitCode,
                Success = outcome.ErrorCode == null && outcome.Response?.ExitCode == 0,
                DurationMs = durationMs,
                OutputSummary = output.Length > 500 ? output[..500] : output,
                ErrorMessage = outcome.ErrorCode
                    ?? (outcome.Response?.Error is { Length: > 0 } e ? e[..Math.Min(e.Length, 1024)] : null),
            });
            await db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            // Best-effort, same posture as LlmCallHistory - a logging failure must never
            // break the command it's logging.
            Console.Error.WriteLine($"[command-ledger] failed to record entry: {ex.Message}");
        }
    }

    private static async Task<ExecuteOutcome> ExecuteCoreInnerAsync(InvokeRequest req, IServiceProvider sp)
    {
        var type = ResolveHandlerType(req.HandlerClass);
        if (type == null)
            return new ExecuteOutcome("unknown_handler_class", new { error = "unknown_handler_class", req.HandlerClass }, null);

        var candidateNames = string.IsNullOrWhiteSpace(req.Method)
            ? ["RunAsync", "Run"]
            : new[] { req.Method };
        var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(m => candidateNames.Contains(m.Name))
            .ToList();
        if (methods.Count == 0)
            return new ExecuteOutcome("no_run_method", new { error = "no_run_method", req.HandlerClass, req.Method }, null);

        // Handlers vary in parameter shape beyond the ~150-command common case
        // (string[] args, IServiceProvider services): some take the two params in the opposite
        // order, some take only one of the two, one (PublishManuscriptCli) takes a third enum
        // parameter - found live while migrating Stage C rather than assumed. Matching by
        // parameter TYPE (not position/count) covers all of them with one dispatch path instead
        // of a special case per handler.
        var method = methods.FirstOrDefault(m => m.GetParameters().Length <= 3);
        if (method == null)
            return new ExecuteOutcome("unsupported_signature", new { error = "unsupported_signature", req.HandlerClass, req.Method }, null);

        var callParams = method.GetParameters();
        var callArgs = new object?[callParams.Length];
        for (var i = 0; i < callParams.Length; i++)
        {
            var pt = callParams[i].ParameterType;
            // IsAssignableFrom(string[]), not `pt == typeof(string[])`: several handlers
            // (SeedCli, ResetPasswordCli, AuditDenormCli) type their args
            // parameter as IReadOnlyList<string>/IEnumerable<string> rather than the concrete
            // array — a strict type-equality check left those silently bound to null (the
            // final `: null` branch), which for an async handler doesn't throw synchronously
            // from Invoke() but instead surfaces as a plain ArgumentNullException the first
            // time the null args is enumerated, once the returned Task is awaited below. Found
            // live: `prose --seed <name>` failing with "Value cannot be null (Parameter
            // 'source')" via the Hub-forwarding path since the Stage C CLI migration — this
            // silently broke every handler using a non-array args type until now.
            callArgs[i] = pt == typeof(IServiceProvider) ? sp
                : pt.IsAssignableFrom(typeof(string[])) ? req.Args
                : pt.IsEnum && req.ExtraParamValue != null ? Enum.Parse(pt, req.ExtraParamValue, ignoreCase: true)
                : pt == typeof(string) && req.ExtraParamValue != null ? req.ExtraParamValue
                : null;
        }

        Guid? universeId = null;
        if (!string.IsNullOrWhiteSpace(req.Universe))
        {
            var uc = sp.GetRequiredService<IUniverseContext>();
            foreach (var u in uc.ListUniverses())
                if (string.Equals(u.Slug, req.Universe, StringComparison.OrdinalIgnoreCase)) { universeId = u.Id; break; }
            if (universeId == null)
                return new ExecuteOutcome("unknown_universe", new { error = "unknown_universe", req.Universe }, null);
        }

        var universeContext = sp.GetRequiredService<IUniverseContext>();

        await ConsoleGate.WaitAsync();
        var originalOut = Console.Out;
        var originalErr = Console.Error;
        var originalIn = Console.In;
        var originalCwd = Environment.CurrentDirectory;
        var outWriter = new StringWriter();
        var errWriter = new StringWriter();
        int exitCode;
        try
        {
            if (universeId != null) universeContext.SetFlowUniverse(universeId);
            Console.SetOut(outWriter);
            Console.SetError(errWriter);
            if (req.Stdin != null) Console.SetIn(new StringReader(req.Stdin));
            if (!string.IsNullOrWhiteSpace(req.Cwd) && Directory.Exists(req.Cwd)) Environment.CurrentDirectory = req.Cwd;

            var result = method.Invoke(null, callArgs);
            exitCode = result switch
            {
                Task<int> ti => await ti,
                int i => i,
                Task t => await AwaitVoidTask(t),
                _ => 0,
            };
        }
        catch (TargetInvocationException tie)
        {
            errWriter.WriteLine($"[cli-invoke] handler threw: {(tie.InnerException ?? tie).Message}");
            exitCode = 1;
        }
        catch (Exception ex)
        {
            errWriter.WriteLine($"[cli-invoke] invoke failed: {ex.Message}");
            exitCode = 1;
        }
        finally
        {
            Console.SetOut(originalOut);
            Console.SetError(originalErr);
            Console.SetIn(originalIn);
            Environment.CurrentDirectory = originalCwd;
            if (universeId != null) universeContext.SetFlowUniverse(null);
            ConsoleGate.Release();
        }

        return new ExecuteOutcome(null, null, new InvokeResponse(exitCode, outWriter.ToString(), errWriter.ToString()));
    }

    private static async Task<int> AwaitVoidTask(Task t)
    {
        await t;
        return 0;
    }
}
