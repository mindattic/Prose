using System.Text.Json;
using Prose.Core.Services;

namespace Prose.Cli;

/// <summary>
/// RFC 0017 conduit (docs/CONDUIT.md §7) — game texts, <c>prose-text/1</c>. Every verb takes its universe as
/// the first positional argument (no ambient scope), so all are in Program.cs's UniverseAgnosticCommands.
///
///   prose --texts-list &lt;slug&gt; [--kind k] [--status s] [--since v|date] [--key key] [--json]
///   prose --texts-get &lt;slug&gt; &lt;id&gt; [--version n] [--history]
///   prose --texts-pull &lt;slug&gt; --out &lt;dir&gt; [--kind k] [--status s] [--since v|date]
///       Writes each text as &lt;dir&gt;/&lt;id&gt;.v&lt;version&gt;.json (default status: pullable = approved + published).
///   prose --texts-draft &lt;slug&gt; &lt;file.json|dir&gt; [--by name]
///       Drafts one text, a JSON array of texts, or every *.json in a folder. Identical content changes nothing.
///   prose --texts-approve &lt;slug&gt; &lt;id&gt;... --by &lt;name&gt; [--version n]      (the author's call; RFC 0009)
///   prose --texts-publish &lt;slug&gt; --by &lt;name&gt; [id...]                    (one texts.published notice)
///   prose --texts-retire &lt;slug&gt; &lt;id&gt;... --by &lt;name&gt;
///   prose --texts-approve-all &lt;slug&gt; --kind &lt;k&gt; --by &lt;name&gt; [--prefix p] [--about id] [--publish] [--dry-run] [--json]
///       PRS-T918 bulk approval: every pending draft of one kind (optionally keys starting with p and/or texts about
///       an entity). Each draft is re-checked; one that fails is reported and left a draft, the rest are approved
///       (exit 1 when any was refused). --publish also publishes them as one batch (one texts.published notice).
///   prose --texts-publish-all &lt;slug&gt; --kind &lt;k&gt; --by &lt;name&gt; [--prefix p] [--about id] [--dry-run] [--json]
///       PRS-T918 bulk publish: every approved, unpublished text of one kind, as one batch.
///   prose --texts-verify &lt;slug&gt; [id...] [--kind k] [--json]
///       RFC 0018 (PRS-T922): run the game-mode verifiers (canon, continuity, obligations, voice, branches, barks) over
///       the given texts, else every text of the universe, and file their findings. Writes findings only, never a text.
///   prose --texts-findings &lt;slug&gt; [id] [--status new|triaged|applied|dismissed] [--json]
///       RFC 0018: the findings filed on a universe's texts, or on one text (every version).
/// </summary>
public static class ProseTextCli
{
    private static readonly JsonSerializerOptions Pretty = new(ProseTextService.JsonOptsPublic) { WriteIndented = true };

    private static readonly HashSet<string> ValueFlags =
        ["--kind", "--status", "--since", "--key", "--version", "--out", "--by", "--prefix", "--about", "--limit"];

    public static async Task<int> RunListAsync(string[] args, IServiceProvider services)
    {
        var pos = Positionals(args);
        if (pos.Count < 1) return Usage("prose --texts-list <slug> [--kind k] [--status s] [--since v|date] [--key key] [--json]");
        var svc = services.GetRequiredService<ProseTextService>();
        try
        {
            var docs = await svc.ListAsync(pos[0], Flag(args, "--kind"), Flag(args, "--status"), Flag(args, "--since"), Flag(args, "--key"));
            if (args.Contains("--json")) { Console.WriteLine(JsonSerializer.Serialize(docs, Pretty)); return 0; }
            Console.WriteLine($"[texts] {docs.Count} text(s) in '{pos[0]}'");
            foreach (var d in docs)
            {
                var extra = d.DraftVersion != null ? $" (draft v{d.DraftVersion} pending)" : d.LiveVersion != null ? $" (live v{d.LiveVersion})" : "";
                Console.WriteLine($"  {d.Kind,-12} {d.Id,-44} v{d.Version} {d.Status,-9} {d.Key}{extra}");
            }
            return 0;
        }
        catch (InvalidOperationException ex) { return Fail("texts-list", ex.Message); }
    }

    public static async Task<int> RunGetAsync(string[] args, IServiceProvider services)
    {
        var pos = Positionals(args);
        if (pos.Count < 2) return Usage("prose --texts-get <slug> <id> [--version n] [--history]");
        var svc = services.GetRequiredService<ProseTextService>();
        try
        {
            object? found = args.Contains("--history")
                ? await svc.GetRecordAsync(pos[0], pos[1])
                : await svc.GetAsync(pos[0], pos[1], IntFlag(args, "--version"));
            if (found == null) return Fail("texts-get", $"no text '{pos[1]}' in '{pos[0]}'");
            Console.WriteLine(JsonSerializer.Serialize(found, Pretty));
            return 0;
        }
        catch (InvalidOperationException ex) { return Fail("texts-get", ex.Message); }
    }

    public static async Task<int> RunPullAsync(string[] args, IServiceProvider services)
    {
        var pos = Positionals(args);
        var outDir = Flag(args, "--out");
        if (pos.Count < 1 || string.IsNullOrWhiteSpace(outDir))
            return Usage("prose --texts-pull <slug> --out <dir> [--kind k] [--status s] [--since v|date]");
        var svc = services.GetRequiredService<ProseTextService>();
        try
        {
            var docs = await svc.ListAsync(pos[0], Flag(args, "--kind"), Flag(args, "--status"), Flag(args, "--since"));
            Directory.CreateDirectory(outDir);
            foreach (var d in docs)
                await ProseArtifacts.WriteTextAsync(Path.Combine(outDir, $"{d.Id}.v{d.Version}.json"),
                    JsonSerializer.Serialize(d, Pretty) + "\n", ProseArtifacts.Overwrite);
            Console.WriteLine($"[texts-pull] Wrote {docs.Count} text(s) from '{pos[0]}' to {outDir}");
            return 0;
        }
        catch (InvalidOperationException ex) { return Fail("texts-pull", ex.Message); }
    }

    public static async Task<int> RunDraftAsync(string[] args, IServiceProvider services)
    {
        var pos = Positionals(args);
        if (pos.Count < 2) return Usage("prose --texts-draft <slug> <file.json|dir> [--by name]");
        var source = pos[1];
        var files = Directory.Exists(source)
            ? Directory.GetFiles(source, "*.json").OrderBy(f => f, StringComparer.Ordinal).ToArray()
            : File.Exists(source) ? [source] : [];
        if (files.Length == 0) return Fail("texts-draft", $"no JSON at {source}");

        var svc = services.GetRequiredService<ProseTextService>();
        var by = Flag(args, "--by");
        int created = 0, revised = 0, unchanged = 0, failed = 0;
        foreach (var file in files)
        {
            var json = await File.ReadAllTextAsync(file);
            List<string> docs;
            try
            {
                using var parsed = JsonDocument.Parse(json);
                docs = parsed.RootElement.ValueKind == JsonValueKind.Array
                    ? parsed.RootElement.EnumerateArray().Select(e => e.GetRawText()).ToList()
                    : [json];
            }
            catch (JsonException ex) { Console.Error.WriteLine($"[texts-draft] {Path.GetFileName(file)}: {ex.Message}"); failed++; continue; }

            foreach (var doc in docs)
            {
                var r = await svc.DraftAsync(pos[0], doc, by);
                if (!r.Success)
                {
                    failed++;
                    foreach (var e in r.Errors) Console.Error.WriteLine($"[texts-draft] ✘ {Path.GetFileName(file)} {r.Id}: {e}");
                    continue;
                }
                if (r.Unchanged) unchanged++; else if (r.Created) created++; else revised++;
                if (!r.Unchanged) Console.WriteLine($"[texts-draft] {r.Id} v{r.Version} draft{(r.Created ? " (new)" : "")}");
                PrintFindings(r.Findings);
            }
        }
        Console.WriteLine($"[texts-draft] '{pos[0]}': {created} new, {revised} revised, {unchanged} unchanged, {failed} refused");
        return failed == 0 ? 0 : 1;
    }

    public static async Task<int> RunApproveAsync(string[] args, IServiceProvider services)
    {
        var pos = Positionals(args);
        var by = Flag(args, "--by");
        if (pos.Count < 2 || string.IsNullOrWhiteSpace(by))
            return Usage("prose --texts-approve <slug> <id>... --by <name> [--version n]");
        var r = await services.GetRequiredService<ProseTextService>().ApproveAsync(pos[0], pos.Skip(1).ToList(), by, IntFlag(args, "--version"));
        return Report("texts-approve", "approved", r);
    }

    public static async Task<int> RunPublishAsync(string[] args, IServiceProvider services)
    {
        var pos = Positionals(args);
        var by = Flag(args, "--by");
        if (pos.Count < 1 || string.IsNullOrWhiteSpace(by)) return Usage("prose --texts-publish <slug> --by <name> [id...]");
        var r = await services.GetRequiredService<ProseTextService>().PublishAsync(pos[0], by, pos.Skip(1).ToList());
        return Report("texts-publish", "published", r);
    }

    public static async Task<int> RunRetireAsync(string[] args, IServiceProvider services)
    {
        var pos = Positionals(args);
        var by = Flag(args, "--by");
        if (pos.Count < 2 || string.IsNullOrWhiteSpace(by)) return Usage("prose --texts-retire <slug> <id>... --by <name>");
        var r = await services.GetRequiredService<ProseTextService>().RetireAsync(pos[0], pos.Skip(1).ToList(), by);
        return Report("texts-retire", "retired", r);
    }

    public static async Task<int> RunApproveAllAsync(string[] args, IServiceProvider services)
    {
        var pos = Positionals(args);
        var by = Flag(args, "--by");
        var kind = Flag(args, "--kind");
        if (pos.Count < 1 || string.IsNullOrWhiteSpace(by) || string.IsNullOrWhiteSpace(kind))
            return Usage("prose --texts-approve-all <slug> --kind <k> --by <name> [--prefix p] [--about id] [--publish] [--dry-run] [--json]");
        var r = await services.GetRequiredService<ProseTextService>().BulkApproveAsync(pos[0], kind, by,
            Flag(args, "--prefix"), Flag(args, "--about"), args.Contains("--dry-run"), args.Contains("--publish"));
        return ReportBulk("texts-approve-all", pos[0], r, args.Contains("--json"));
    }

    public static async Task<int> RunPublishAllAsync(string[] args, IServiceProvider services)
    {
        var pos = Positionals(args);
        var by = Flag(args, "--by");
        var kind = Flag(args, "--kind");
        if (pos.Count < 1 || string.IsNullOrWhiteSpace(by) || string.IsNullOrWhiteSpace(kind))
            return Usage("prose --texts-publish-all <slug> --kind <k> --by <name> [--prefix p] [--about id] [--dry-run] [--json]");
        var r = await services.GetRequiredService<ProseTextService>().BulkPublishAsync(pos[0], kind, by,
            Flag(args, "--prefix"), Flag(args, "--about"), args.Contains("--dry-run"));
        return ReportBulk("texts-publish-all", pos[0], r, args.Contains("--json"));
    }

    private static int ReportBulk(string verb, string slug, ProseTextService.BulkResult r, bool json)
    {
        if (json) Console.WriteLine(JsonSerializer.Serialize(r, Pretty));
        foreach (var e in r.Errors) Console.Error.WriteLine($"[{verb}] ✘ {e}");
        if (r.Errors.Count > 0) return 1;
        if (!json)
        {
            var would = r.DryRun ? "would be " : "";
            var filter = r.Kind + (r.KeyPrefix != null ? $", key prefix '{r.KeyPrefix}'" : "") + (r.About != null ? $", about '{r.About}'" : "");
            Console.WriteLine($"[{verb}] '{slug}' ({filter}){(r.DryRun ? " — dry run, nothing changed" : "")}");
            foreach (var i in r.Approved) Console.WriteLine($"  {would}approved  {i.Id,-44} v{i.Version} {i.Key}");
            foreach (var i in r.Published) Console.WriteLine($"  {would}published {i.Id,-44} v{i.Version} {i.Key}");
            foreach (var x in r.Refused)
                foreach (var e in x.Errors) Console.Error.WriteLine($"[{verb}] ✘ refused {x.Id} v{x.Version}: {e}");
            Console.WriteLine($"[{verb}] {r.Approved.Count} {would}approved, {r.Published.Count} {would}published, {r.Refused.Count} refused");
        }
        return r.Refused.Count == 0 ? 0 : 1;
    }

    private static int Report(string verb, string done, ProseTextService.ActionResult r)
    {
        foreach (var e in r.Errors) Console.Error.WriteLine($"[{verb}] ✘ {e}");
        Console.WriteLine($"[{verb}] {r.Ids.Count} text(s) {done}{(r.Ids.Count > 0 ? ": " + string.Join(", ", r.Ids) : "")}");
        PrintFindings(r.Findings);
        return r.Success ? 0 : 1;
    }

    /// <summary>RFC 0018: findings describe; they never block. Printed after the write they were found on.</summary>
    private static void PrintFindings(IReadOnlyCollection<ProseTextVerifier.TextFinding> found)
    {
        foreach (var f in found.OrderBy(f => f.Severity == FindingSeverity.High ? 0 : f.Severity == FindingSeverity.Medium ? 1 : 2))
            Console.WriteLine($"    ! {f.Severity,-6} {f.TextId} v{f.Version} {f.Summary}");
    }

    public static async Task<int> RunVerifyAsync(string[] args, IServiceProvider services)
    {
        var pos = Positionals(args);
        if (pos.Count < 1) return Usage("prose --texts-verify <slug> [id...] [--kind k] [--json]");
        var r = await services.GetRequiredService<ProseTextVerifier>().VerifyAsync(pos[0], pos.Skip(1).ToList(), Flag(args, "--kind"));
        if (args.Contains("--json")) { Console.WriteLine(JsonSerializer.Serialize(r, Pretty)); return r.Success ? 0 : 1; }
        foreach (var e in r.Errors) Console.Error.WriteLine($"[texts-verify] ✘ {e}");
        PrintFindings(r.Findings);
        var bySeverity = string.Join(", ", r.Findings.GroupBy(f => f.Severity).OrderByDescending(g => g.Key).Select(g => $"{g.Count()} {g.Key}"));
        Console.WriteLine($"[texts-verify] '{r.Universe}': {r.TextsChecked} text(s), {r.VersionsChecked} version(s) checked, " +
                          $"{r.Findings.Count} finding(s){(r.Findings.Count > 0 ? $" ({bySeverity})" : "")}. Findings describe; nothing was changed.");
        return r.Success ? 0 : 1;
    }

    public static Task<int> RunFindingsAsync(string[] args, IServiceProvider services)
    {
        var pos = Positionals(args);
        if (pos.Count < 1) return Task.FromResult(Usage("prose --texts-findings <slug> [id] [--status new|triaged|applied|dismissed] [--json]"));
        FindingStatus? status = null;
        if (Flag(args, "--status") is { } st)
        {
            if (!Enum.TryParse<FindingStatus>(st, ignoreCase: true, out var parsed))
                return Task.FromResult(Fail("texts-findings", $"unknown status '{st}' (new, triaged, applied, dismissed)"));
            status = parsed;
        }
        var rows = services.GetRequiredService<ProseTextVerifier>()
            .ListFindingRows(pos[0], pos.Count > 1 ? pos[1] : null, status, IntFlag(args, "--limit") ?? 500);
        if (args.Contains("--json")) { Console.WriteLine(JsonSerializer.Serialize(rows, Pretty)); return Task.FromResult(0); }
        foreach (var f in rows)
            Console.WriteLine($"  #{f.Id,-7} {f.Status,-9} {f.Severity,-6} {f.TextId} v{f.Version?.ToString() ?? "?"} {f.Summary}");
        Console.WriteLine($"[texts-findings] {rows.Count} finding(s)");
        return Task.FromResult(0);
    }

    private static List<string> Positionals(string[] args)
    {
        var list = new List<string>();
        for (var i = 0; i < args.Length; i++)
        {
            if (ValueFlags.Contains(args[i])) { i++; continue; }
            if (args[i].StartsWith("--", StringComparison.Ordinal)) continue;
            list.Add(args[i]);
        }
        return list;
    }

    private static string? Flag(string[] args, string name)
    {
        var idx = Array.IndexOf(args, name);
        return idx >= 0 && idx + 1 < args.Length ? args[idx + 1] : null;
    }

    private static int? IntFlag(string[] args, string name) => int.TryParse(Flag(args, name), out var n) ? n : null;

    private static int Usage(string usage) { Console.Error.WriteLine("Usage: " + usage); return 1; }

    private static int Fail(string verb, string message) { Console.Error.WriteLine($"[{verb}] {message}"); return 1; }
}
