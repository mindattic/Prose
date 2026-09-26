using Prose.Core.Models.Canon;
using Prose.Core.Services;

namespace Prose.Cli;

/// <summary>
/// <c>prose --create-archetype --name "&lt;name&gt;" [--category …] [--description …]
/// [--behavioral-signature …] [--under-stress …] [--at-rest …] [--will-always a;b] [--will-never a;b]
/// [--unless a;b] [--tags a,b,c]</c>
///
/// <para>Create or update an archetype, the CLI twin of MCP <c>create_archetype</c>. Archetypes had
/// the gap materials and vocabulary had: <c>ArchetypeRepository.Save</c> existed with nothing
/// exposing it. The street roles (Stitcher, Channeler, Read, Psyker, Ghost, Street Samurai,
/// Splicer) are archetypes by the author's ruling 01a0de90.</para>
///
/// <para>Omitted scalars are LEFT UNCHANGED. The three behaviour lists split on ';' because their
/// items are sentences with commas in them; tags split on ','. Any list given replaces the old
/// one, and '[]' clears it. An existing name updates that record.</para>
/// </summary>
public static class CreateArchetypeCli
{
    public static Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        string? Flag(string name) { var i = Array.IndexOf(args, name); return i >= 0 && i + 1 < args.Length ? args[i + 1] : null; }

        var name = Flag("--name");
        if (string.IsNullOrWhiteSpace(name))
        {
            Console.Error.WriteLine("[create-archetype] --name \"<name>\" is required.");
            return Task.FromResult(1);
        }

        var repo = services.GetRequiredService<ArchetypeRepository>();
        var matches = repo.GetAll().Where(a => string.Equals(a.Name, name, StringComparison.OrdinalIgnoreCase)).ToList();
        if (matches.Count > 1)
        {
            Console.Error.WriteLine($"[create-archetype] {matches.Count} archetypes are already named \"{name}\" ({string.Join(", ", matches.Select(m => m.Id))}) — resolve the duplicate first.");
            return Task.FromResult(1);
        }
        var existing = matches.FirstOrDefault();
        var isNew = existing is null;
        var a = existing ?? new ArchetypeData();
        a.Name = name;

        if (Flag("--category") is { } category) a.Category = category;
        if (Flag("--description") is { } description) a.Description = description;
        if (Flag("--behavioral-signature") is { } signature) a.BehavioralSignature = signature;
        if (Flag("--under-stress") is { } stress) a.UnderStress = stress;
        if (Flag("--at-rest") is { } rest) a.AtRest = rest;
        a.WillAlways = List(Flag("--will-always"), ';', a.WillAlways);
        a.WillNever = List(Flag("--will-never"), ';', a.WillNever);
        a.Unless = List(Flag("--unless"), ';', a.Unless);
        a.Tags = List(Flag("--tags"), ',', a.Tags);

        repo.Save(a);

        // Read back by id rather than trusting the write.
        var after = repo.GetById(a.Id);
        if (after is null)
        {
            Console.Error.WriteLine($"[create-archetype] Save reported success but \"{name}\" could not be read back.");
            return Task.FromResult(1);
        }

        Console.WriteLine($"[create-archetype] {(isNew ? "Created" : "Updated")} \"{after.Name}\"" +
                          $"{(string.IsNullOrWhiteSpace(after.Category) ? "" : $" ({after.Category})")} — id {after.Id}");
        if (!string.IsNullOrWhiteSpace(after.Description)) Console.WriteLine($"  {Clip(after.Description, 110)}");
        return Task.FromResult(0);
    }

    internal static List<string> List(string? incoming, char separator, List<string> current)
    {
        if (incoming is null) return current;
        if (incoming.Trim() == "[]") return [];
        return incoming.Split(separator).Select(s => s.Trim()).Where(s => s.Length > 0).ToList();
    }

    private static string Clip(string s, int n) => s.Length <= n ? s : s[..n] + "…";
}
