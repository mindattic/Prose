using Prose.Core.Services;

namespace Prose.Cli;

/// <summary>
/// <c>prose --universe &lt;subcommand&gt;</c> — universe management from the CLI.
///
/// Subcommands:
///   list    Print all universes with id, slug, name, theme, active flag.
///   current Print the currently active universe for this process.
///   use     --slug &lt;slug&gt; | --id &lt;guid&gt;
///           Switch the active universe (persisted as the default for subsequent CLI calls).
/// </summary>
public static class UniverseCli
{
    /// <summary>
    /// The subcommands <c>prose --universe</c> actually accepts. Program.cs consults this before
    /// claiming dispatch, so that <c>--universe &lt;slug&gt; --some-command</c> — where --universe
    /// is the scoping flag rather than the command — falls through to the real command instead of
    /// being swallowed and answered with usage text.
    /// </summary>
    private static readonly string[] Subcommands = ["list", "current", "use"];

    /// <summary>True when <paramref name="token"/> names a universe subcommand.</summary>
    public static bool IsSubcommand(string? token) =>
        token != null && Subcommands.Contains(token, StringComparer.OrdinalIgnoreCase);

    public static Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        if (args.Length == 0 || args[0].StartsWith("--"))
        {
            PrintUsage();
            return Task.FromResult(1);
        }

        var sub = args[0];
        var rest = args[1..];
        return sub switch
        {
            "list"    => Task.FromResult(ListAsync(services)),
            "current" => Task.FromResult(CurrentAsync(services)),
            "use"     => UseAsync(rest, services),
            _         => Task.FromResult(PrintUsage()),
        };
    }

    private static int ListAsync(IServiceProvider services)
    {
        var ctx = services.GetRequiredService<IUniverseContext>();
        var universes = ctx.ListUniverses();
        if (universes.Count == 0) { Console.WriteLine("[universe list] No universes found."); return 0; }

        Console.WriteLine($"{"Id",-36} {"Slug",-20} {"Name",-30} {"Theme",-20} {"Active"}");
        Console.WriteLine(new string('-', 115));
        foreach (var u in universes.OrderBy(u => u.SortKey))
            Console.WriteLine($"{u.Id,-36} {u.Slug,-20} {u.Name,-30} {(u.Theme ?? ""),20} {(u.IsActive ? "✓" : "")}");

        Console.WriteLine($"\nCurrent: {ctx.CurrentSlug} ({ctx.CurrentId})");
        return 0;
    }

    private static int CurrentAsync(IServiceProvider services)
    {
        var ctx = services.GetRequiredService<IUniverseContext>();
        var u = ctx.CurrentUniverse;
        if (u == null) { Console.WriteLine("[universe current] No universe selected (using global default)."); return 0; }
        Console.WriteLine($"Id:    {u.Id}");
        Console.WriteLine($"Slug:  {u.Slug}");
        Console.WriteLine($"Name:  {u.Name}");
        Console.WriteLine($"Theme: {u.Theme ?? "(none)"}");
        return 0;
    }

    private static async Task<int> UseAsync(string[] args, IServiceProvider services)
    {
        string? slug = null, idStr = null;
        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--slug": if (i + 1 < args.Length) slug = args[++i]; break;
                case "--id":   if (i + 1 < args.Length) idStr = args[++i]; break;
            }
        }
        if (string.IsNullOrWhiteSpace(slug) && string.IsNullOrWhiteSpace(idStr))
        {
            Console.Error.WriteLine("[universe use] One of --slug or --id is required.");
            return 1;
        }

        var ctx = services.GetRequiredService<IUniverseContext>();

        if (!string.IsNullOrWhiteSpace(slug))
        {
            if (!ctx.UseUniverseBySlug(slug))
            {
                Console.Error.WriteLine($"[universe use] Unknown slug '{slug}'. Run 'prose --universe list' to see available universes.");
                return 1;
            }
        }
        else
        {
            if (!Guid.TryParse(idStr, out var g)) { Console.Error.WriteLine("[universe use] --id must be a GUID."); return 1; }
            ctx.UseUniverse(g);
        }

        var current = ctx.CurrentUniverse;
        Console.WriteLine($"[universe use] Active universe: {current?.Slug ?? "unknown"} ({ctx.CurrentId})");
        return await Task.FromResult(0);
    }

    private static int PrintUsage()
    {
        Console.Error.WriteLine("Usage: prose --universe <subcommand> [args]");
        Console.Error.WriteLine("  list");
        Console.Error.WriteLine("  current");
        Console.Error.WriteLine("  use  --slug <slug> | --id <guid>");
        return 1;
    }
}
