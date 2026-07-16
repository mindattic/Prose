using Microsoft.Extensions.DependencyInjection;
using StreetSamurai.Core.Data.Entities;
using StreetSamurai.Core.Services;

namespace StreetSamurai.Cli;

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
    private static readonly (string Type, Guid UniverseId)[] AllDocuments =
    [
        ("WorldBible",    Universe.GlmzId),
        ("WorldMaster",   Universe.GlmzId),
        ("Franchise",     Universe.GlmzId),
        ("UniverseCanon", Universe.FantasyId),
    ];

    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        string? type  = null;
        bool    all   = args.Contains("--all");
        bool    quiet = args.Contains("--quiet");

        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "--type" && i + 1 < args.Length) type = args[++i];
        }

        if (!all && string.IsNullOrWhiteSpace(type))
        {
            Console.Error.WriteLine("[generate-canon-md] --type <type> or --all is required.");
            Console.Error.WriteLine("Usage: ss --generate-canon-md --type <WorldBible|WorldMaster|Franchise|UniverseCanon>");
            Console.Error.WriteLine("       ss --generate-canon-md --all");
            return 2;
        }

        var svc = services.GetRequiredService<CanonDocumentService>();

        var targets = all
            ? AllDocuments
            : AllDocuments.Where(d => d.Type.Equals(type, StringComparison.OrdinalIgnoreCase)).ToArray();

        if (targets.Length == 0)
        {
            Console.Error.WriteLine($"[generate-canon-md] Unknown document type '{type}'. Valid: WorldBible, WorldMaster, Franchise, UniverseCanon.");
            return 1;
        }

        if (!quiet)
            Console.WriteLine($"[generate-canon-md] Generating {targets.Length} document(s)…");

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
