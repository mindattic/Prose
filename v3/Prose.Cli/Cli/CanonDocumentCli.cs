using Microsoft.Extensions.DependencyInjection;
using Prose.Core.Services;

namespace Prose.Cli;

/// <summary>
/// <c>ss --generate-canon-md</c> — regenerate canon document .md files from DB.
///
/// Source of truth is <c>CanonDocuments</c> + <c>CanonDocumentSections</c>.
/// The disk files are generated read-only mirrors; never hand-edit them.
///
/// Args:
///   --type &lt;WorldBible|WorldMaster|Franchise|UniverseCanon&gt;   Target one document type.
///   --all                                                        Regenerate all four documents.
///   --quiet                                                      Suppress per-file output.
/// </summary>
public static class CanonDocumentCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        string? type  = null;
        bool    all   = args.Contains("--all");
        bool    quiet = args.Contains("--quiet");

        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "--type" && i + 1 < args.Length) type = args[++i];
        }

        var registry = services.GetRequiredService<CanonDocumentTypeRegistry>();

        if (!all && string.IsNullOrWhiteSpace(type))
        {
            var validTypes = string.Join(", ", await registry.ListActiveTypeNamesAsync());
            Console.Error.WriteLine("[generate-canon-md] --type <type> or --all is required.");
            Console.Error.WriteLine($"Usage: ss --generate-canon-md --type <{validTypes}>");
            Console.Error.WriteLine("       ss --generate-canon-md --all");
            return 2;
        }

        var svc = services.GetRequiredService<CanonDocumentService>();

        // "--all" means every (DocumentType, UniverseId) pair actually migrated into
        // CanonDocuments — reflects what's real, not a compile-time list that drifts from it.
        var migrated = await registry.ListMigratedAsync();
        var targets = all
            ? migrated
            : migrated.Where(d => d.DocumentType.Equals(type, StringComparison.OrdinalIgnoreCase)).ToList();

        if (targets.Count == 0)
        {
            var validTypes = string.Join(", ", await registry.ListActiveTypeNamesAsync());
            Console.Error.WriteLine($"[generate-canon-md] Unknown or unmigrated document type '{type}'. Valid: {validTypes}.");
            return 1;
        }

        if (!quiet)
            Console.WriteLine($"[generate-canon-md] Generating {targets.Count} document(s)…");

        int ok = 0, fail = 0;
        foreach (var (docType, universeId) in targets)
        {
            try
            {
                var result = await svc.GenerateMdAsync(docType, universeId);
                if (!result.Ok)
                {
                    Console.Error.WriteLine($"  ✗ {docType} — {result.ErrorMessage}");
                    fail++;
                }
                else
                {
                    if (!quiet)
                        Console.WriteLine($"  ✓ {docType,-16} {result.SectionCount} sections → {result.FilePath}");
                    ok++;
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"  ✗ {docType} — {ex.Message}");
                fail++;
            }
        }

        if (!quiet)
            Console.WriteLine($"[generate-canon-md] Done: {ok} succeeded, {fail} failed.");

        return fail > 0 ? 1 : 0;
    }
}
