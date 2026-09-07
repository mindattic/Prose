using System.Text.RegularExpressions;

namespace Prose.UnitTests;

/// <summary>
/// RFC 0012 §3.6 — "one door" for beat text, enforced by the build rather than by doctrine.
///
/// <para>Two invariants, checked by scanning the source tree (the same way RFC 0009's
/// <c>BeatWriteReason</c> makes every writer declare itself, this makes every NEW writer fail a
/// test until it is either routed through <c>NodeWorkbenchService</c> or explicitly listed here
/// with a declared reason):</para>
/// <list type="number">
/// <item>Prose is generated in exactly one place: <c>BeatGeneratorService.GenerateBeatAsync</c>
///   is called only by <c>ProseWriterRouter</c>.</item>
/// <item>Beat text is written in exactly one door: any file that assigns <c>.Text =</c> on a
///   <c>Beat</c> row or adds <c>Beats</c> rows is either <c>NodeWorkbenchService</c> or on the
///   short allow-list below — and every allow-listed file must set <c>LastWriteReason</c>.</item>
/// </list>
/// <para>Adding a file to the allow-list is a reviewable one-line diff, which is the point.</para>
/// </summary>
[TestFixture]
public class WriterFunnelTests
{
    private static readonly string[] ScannedProjects = ["Prose.Core", "Prose.Cli", "Prose.Mcp", "Prose.Hub"];

    /// <summary>Files allowed to write Beats.Text / add Beats rows besides NodeWorkbenchService,
    /// each with the reason they exist. Every one must contain "LastWriteReason".</summary>
    private static readonly Dictionary<string, string> BeatWriteAllowList = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Prose.Core/Services/NodeWorkbenchService.cs"]        = "the door itself (UpdateBeatTextAsync, DuplicateNodeAsync, CreateNodeFromBeatsAsync, splits)",
        ["Prose.Cli/Cli/NodeBeatWriter.cs"]                     = "--import-book / --reimport: creates rows from a parsed file, stamps Import",
        ["Prose.Core/Services/DuplicateEntityScanService.cs"]   = "entity-tag GUID rewrite on merge, stamps TagMaintenance (RFC 0009 Phase 3b)",
        ["Prose.Core/Services/DistributedWorkerCoordinator.cs"] = "worker-generated prose into an EMPTY beat, stamps Generation (RFC 0009 Phase 3b)",
        ["Prose.Core/Services/BeatRebuildService.cs"]           = "re-segments existing text into new rows, stamps StructuralSplit",
        ["Prose.Core/Services/NodeMigrationService.cs"]         = "legacy Chapter/Episode model → Beats, stamps Import",
        ["Prose.Core/Services/NodeOutlineService.cs"]           = "seed-spine: empty planned beats from the outline, stamps Plan",
        ["Prose.Cli/Cli/MigrateLegacyBookChapterCli.cs"]        = "legacy Book/Chapter migration, stamps Import",
        ["Prose.Cli/Cli/SanitizeBeatsCli.cs"]                   = "mojibake repair, stamps TagMaintenance",
        ["Prose.Cli/Cli/TagEntitiesCli.cs"]                     = "entity-tag rescan, stamps TagMaintenance",
    };

    [Test]
    public void GenerateBeatAsync_IsCalledOnlyByTheRouter()
    {
        var offenders = new List<string>();
        foreach (var (path, text) in SourceFiles())
        {
            var rel = Rel(path);
            if (rel.EndsWith("BeatGeneratorService.cs") || rel.EndsWith("ProseWriterRouter.cs")) continue;
            if (Regex.IsMatch(text, @"\bGenerateBeatAsync\s*\(") && !IsOnlyInComments(text, "GenerateBeatAsync"))
                offenders.Add(rel);
        }
        Assert.That(offenders, Is.Empty,
            "Prose is generated in one place. These files call GenerateBeatAsync outside ProseWriterRouter:\n  " + string.Join("\n  ", offenders));
    }

    [Test]
    public void BeatText_IsWrittenOnlyThroughTheDoor_OrByADeclaredWriter()
    {
        var offenders = new List<string>();
        var undeclared = new List<string>();
        // A write to the canon Beats table is: a row added to the DbSet, a `new Beat { … }` entity,
        // or `.Text =` on a variable in a file that also queries db.Beats. Other `.Beats.Add(` calls
        // (in-memory lists, the legacy ChapterBeatEntity model, parser DTOs) are not.
        var textAssign = new Regex(@"\b(beat|b|nb|target|current|row|existing)\s*\.\s*Text\s*=[^=]", RegexOptions.IgnoreCase);
        var beatsAdd   = new Regex(@"\bdb\s*\.\s*Beats\s*\.\s*Add(Range)?\s*\(");
        var newBeat    = new Regex(@"\bnew\s+Beat\s*[{(]");

        foreach (var (path, text) in SourceFiles())
        {
            var rel = Rel(path);
            var code = StripComments(text);
            var queriesBeats = Regex.IsMatch(code, @"\bdb\s*\.\s*Beats\b");
            var writes = beatsAdd.IsMatch(code) || newBeat.IsMatch(code) || (textAssign.IsMatch(code) && queriesBeats);
            if (!writes) continue;

            // Legacy/other models that are not the canon Beats table.
            if (rel.EndsWith("ChapterRecordingService.cs") || rel.EndsWith("EpisodeAudioService.cs")) continue;
            // Read-only detached mutation for stripping tags before a comparison — not a write.
            if (rel.EndsWith("BeatVerificationService.cs")) continue;

            if (!BeatWriteAllowList.ContainsKey(rel))
            {
                offenders.Add(rel);
                continue;
            }
            if (!code.Contains("LastWriteReason", StringComparison.Ordinal))
                undeclared.Add(rel);
        }

        Assert.Multiple(() =>
        {
            Assert.That(offenders, Is.Empty,
                "Beat text has one door. These files write Beats.Text / add Beats rows and are not NodeWorkbenchService or on the allow-list:\n  " + string.Join("\n  ", offenders));
            Assert.That(undeclared, Is.Empty,
                "Allow-listed beat writers must stamp LastWriteReason (RFC 0009):\n  " + string.Join("\n  ", undeclared));
        });
    }

    [Test]
    public void AllowList_EntriesStillExist()
    {
        var root = FindRepoRoot();
        var missing = BeatWriteAllowList.Keys.Where(k => !File.Exists(Path.Combine(root, "v3", k.Replace('/', Path.DirectorySeparatorChar)))).ToList();
        Assert.That(missing, Is.Empty, "Stale allow-list entries (file gone — remove the line):\n  " + string.Join("\n  ", missing));
    }

    // ── helpers ───────────────────────────────────────────────────────────────

    private static IEnumerable<(string Path, string Text)> SourceFiles()
    {
        var root = FindRepoRoot();
        foreach (var proj in ScannedProjects)
        {
            var dir = Path.Combine(root, "v3", proj);
            if (!Directory.Exists(dir)) continue;
            foreach (var f in Directory.EnumerateFiles(dir, "*.cs", SearchOption.AllDirectories))
            {
                var n = f.Replace('\\', '/');
                if (n.Contains("/Migrations/") || n.Contains("/obj/") || n.Contains("/bin/")) continue;
                yield return (f, File.ReadAllText(f));
            }
        }
    }

    private static string Rel(string path)
    {
        var root = FindRepoRoot();
        var v3 = Path.Combine(root, "v3");
        return Path.GetRelativePath(v3, path).Replace('\\', '/');
    }

    private static string StripComments(string s)
    {
        s = Regex.Replace(s, @"/\*.*?\*/", "", RegexOptions.Singleline);
        s = Regex.Replace(s, @"//[^\n]*", "");
        return s;
    }

    private static bool IsOnlyInComments(string text, string token)
        => !StripComments(text).Contains(token, StringComparison.Ordinal);

    private static string? repoRoot;
    private static string FindRepoRoot()
    {
        if (repoRoot != null) return repoRoot;
        var dir = AppDomain.CurrentDomain.BaseDirectory;
        while (dir != null)
        {
            if (Directory.Exists(Path.Combine(dir, "v3", "Prose.Core"))) return repoRoot = dir;
            dir = Directory.GetParent(dir)?.FullName;
        }
        // Build artefacts may live outside the repo (Directory.Build.props); fall back to the
        // compile-time source path of this file.
        var fromSource = Directory.GetParent(SourceFilePath())?.Parent?.Parent?.FullName;
        if (fromSource != null && Directory.Exists(Path.Combine(fromSource, "v3", "Prose.Core"))) return repoRoot = fromSource;
        Assert.Fail("Could not locate the repo root (no v3/Prose.Core above the test binary or this source file).");
        return "";
    }

    private static string SourceFilePath([System.Runtime.CompilerServices.CallerFilePath] string path = "") => path;
}
