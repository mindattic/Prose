using Prose.Core.Models.Canon;
using Prose.Core.Services;

namespace Prose.Cli;

/// <summary>
/// <c>prose --create-material --name "&lt;name&gt;" [--category …] [--description …] [--properties a,b,c]
/// [--applications a,b,c] [--tier …] [--cost …] [--tags a,b,c] [--aliases a,b,c] [--append-lists]</c>
///
/// <para>Create or update a material record. Materials were the one canon entity type with a
/// fully relational table (<c>Materials</c> + its bridges), a repository that can write them
/// (<c>MaterialRepository.Save</c>), read surfaces on both CLI and MCP (<c>list_materials</c>,
/// <c>get_material</c>) — and <b>no write path at all</b>. Every other gear type has a
/// <c>create_&lt;thing&gt;</c> MCP tool; material had none, so the only way to correct a material
/// record was a rebuild from the legacy JSON blob. This closes that gap.</para>
///
/// <para><b>Omitted scalars are left alone</b>, matching <c>CreateWeapon</c>'s contract rather
/// than <c>create_character</c>'s — an update that blanks the fields you did not mention is the
/// documented way this system has silently destroyed records before. List fields REPLACE by
/// default (pass <c>--append-lists</c> to merge instead), and <c>[]</c> clears one.</para>
///
/// <para>Deliberately a CLI and not an MCP tool: MCP tool schemas are fixed when the MCP server
/// process starts, so a newly-added tool is invisible to any session already running, while a CLI
/// command forwards straight into the Hub and works the moment the Hub is redeployed.</para>
/// </summary>
public static class CreateMaterialCli
{
    public static Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        string? name = null, category = null, description = null, tier = null, cost = null,
                properties = null, applications = null, tags = null, aliases = null,
                developers = null, brand = null, product = null;
        var append = false;

        for (var i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--name":         if (i + 1 < args.Length) name = args[++i]; break;
                case "--category":     if (i + 1 < args.Length) category = args[++i]; break;
                case "--description":  if (i + 1 < args.Length) description = args[++i]; break;
                case "--properties":   if (i + 1 < args.Length) properties = args[++i]; break;
                case "--applications": if (i + 1 < args.Length) applications = args[++i]; break;
                case "--developers":   if (i + 1 < args.Length) developers = args[++i]; break;
                case "--tier":         if (i + 1 < args.Length) tier = args[++i]; break;
                case "--cost":         if (i + 1 < args.Length) cost = args[++i]; break;
                case "--tags":         if (i + 1 < args.Length) tags = args[++i]; break;
                case "--aliases":      if (i + 1 < args.Length) aliases = args[++i]; break;
                case "--brand":        if (i + 1 < args.Length) brand = args[++i]; break;
                case "--product":      if (i + 1 < args.Length) product = args[++i]; break;
                case "--append-lists": append = true; break;
            }
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            Console.Error.WriteLine("[create-material] --name \"<material name>\" is required.");
            return Task.FromResult(1);
        }

        var repo = services.GetRequiredService<MaterialRepository>();

        // Match on the exact name the read surfaces use, so --set-material and get_material
        // always mean the same record.
        var existing = repo.GetAll().FirstOrDefault(m =>
            string.Equals(m.Name, name, StringComparison.OrdinalIgnoreCase));
        var isNew = existing is null;
        var m2 = existing ?? new MaterialData { Name = name };

        if (category    is not null) m2.Category        = category;
        if (description is not null) m2.Description     = description;
        if (tier        is not null) m2.TierAvailability = tier;
        if (cost        is not null) m2.Cost            = cost;
        if (brand       is not null) m2.BrandName       = brand;
        if (product     is not null) m2.ProductName     = product;

        m2.Properties   = Merge(m2.Properties,   properties,   append);
        m2.Applications = Merge(m2.Applications, applications, append);
        m2.Developers   = Merge(m2.Developers,   developers,   append);
        m2.Tags         = Merge(m2.Tags,         tags,         append);
        m2.Aliases      = Merge(m2.Aliases,      aliases,      append);

        repo.Save(m2);

        // Read back rather than trusting the write — a Save that reports nothing and changed
        // nothing is the documented failure mode across this codebase's entity writers.
        var after = repo.GetById(m2.Id)
                    ?? repo.GetAll().FirstOrDefault(x => string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase));
        if (after is null)
        {
            Console.Error.WriteLine($"[create-material] Save reported success but \"{name}\" could not be read back. Nothing was written.");
            return Task.FromResult(1);
        }

        Console.WriteLine($"[create-material] {(isNew ? "Created" : "Updated")} \"{after.Name}\" ({after.Category}) — id {after.Id}");
        Console.WriteLine($"  properties:   {Show(after.Properties)}");
        Console.WriteLine($"  applications: {Show(after.Applications)}");
        Console.WriteLine($"  tags:         {Show(after.Tags)}");
        if (!string.IsNullOrWhiteSpace(after.TierAvailability)) Console.WriteLine($"  tier:         {after.TierAvailability}");
        if (!string.IsNullOrWhiteSpace(after.Cost))             Console.WriteLine($"  cost:         {after.Cost}");
        return Task.FromResult(0);
    }

    /// <summary>
    /// Null input leaves the list untouched; "[]" clears it; otherwise the comma-separated value
    /// replaces the list, or is merged into it when <paramref name="append"/> is set. Commas are
    /// the delimiter throughout this codebase's entity CLIs, so a value containing one has to be
    /// passed as separate items — the same constraint every other list field here carries.
    /// </summary>
    private static List<string> Merge(List<string> current, string? incoming, bool append)
    {
        if (incoming is null) return current;
        if (incoming.Trim() == "[]") return [];
        var parsed = incoming.Split(',').Select(s => s.Trim()).Where(s => s.Length > 0).ToList();
        if (!append) return parsed;
        var merged = new List<string>(current);
        foreach (var p in parsed)
            if (!merged.Contains(p, StringComparer.OrdinalIgnoreCase)) merged.Add(p);
        return merged;
    }

    private static string Show(List<string> xs) => xs.Count == 0 ? "(none)" : string.Join(", ", xs);
}
