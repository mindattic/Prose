using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;
using Prose.Core.Models.Canon;
using Prose.Core.Services;

namespace Prose.Mcp;

// ── Archetypes: a write path ─────────────────────────────────────────────────
// ArchetypeRepository.Save existed with nothing exposing it (the gap materials and vocabulary had).
// The street roles are archetypes by the author's ruling 01a0de90.
//   create_archetype
// CLI twin: prose --create-archetype.

[McpServerToolType]
public class ArchetypeTools(ArchetypeRepository archetypes, HubInvoker hub)
{
    [McpServerTool, Description(
        "Create or update an archetype: an occupational or social role in the world (street roles such as " +
        "Stitcher, Channeler, Read, Psyker, Ghost, Street Samurai and Splicer; or a behavioural type). " +
        "An existing name updates that record. Omitted scalar fields are LEFT UNCHANGED. willAlways, " +
        "willNever and unless split on ';' (their items contain commas); tags split on ','. A list given " +
        "replaces the old one; '[]' clears it. Returns the record as read back.")]
    public Task<string> create_archetype(
        [Description("Archetype name. Required.")] string name,
        [Description("Category, e.g. 'street role', 'combat', 'social'.")] string? category = null,
        [Description("What the role is.")] string? description = null,
        [Description("How it shows in behaviour.")] string? behavioralSignature = null,
        [Description("Under stress.")] string? underStress = null,
        [Description("At rest.")] string? atRest = null,
        [Description("';'-separated things it will always do.")] string? willAlways = null,
        [Description("';'-separated things it will never do.")] string? willNever = null,
        [Description("';'-separated exceptions.")] string? unless = null,
        [Description("','-separated tags.")] string? tags = null) =>
        hub.InvokeAsync(nameof(ArchetypeTools), nameof(CreateArchetypeImpl), new
        {
            name, category, description, behavioralSignature, underStress, atRest, willAlways, willNever, unless, tags,
        });

    public string CreateArchetypeImpl(
        string name, string? category = null, string? description = null, string? behavioralSignature = null,
        string? underStress = null, string? atRest = null, string? willAlways = null, string? willNever = null,
        string? unless = null, string? tags = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            return JsonSerializer.Serialize(new { ok = false, error = "name is required" }, CanonTools.JsonOpts);

        var matches = archetypes.GetAll().Where(a => string.Equals(a.Name, name, StringComparison.OrdinalIgnoreCase)).ToList();
        if (matches.Count > 1)
            return JsonSerializer.Serialize(new { ok = false, error = "duplicate_name", ids = matches.Select(m => m.Id) }, CanonTools.JsonOpts);

        var isNew = matches.Count == 0;
        var a = matches.FirstOrDefault() ?? new ArchetypeData();
        a.Name = name;
        if (category is not null) a.Category = category;
        if (description is not null) a.Description = description;
        if (behavioralSignature is not null) a.BehavioralSignature = behavioralSignature;
        if (underStress is not null) a.UnderStress = underStress;
        if (atRest is not null) a.AtRest = atRest;
        a.WillAlways = List(willAlways, ';', a.WillAlways);
        a.WillNever = List(willNever, ';', a.WillNever);
        a.Unless = List(unless, ';', a.Unless);
        a.Tags = List(tags, ',', a.Tags);

        archetypes.Save(a);

        // Read back rather than reporting ok:true on faith.
        var after = archetypes.GetById(a.Id);
        if (after is null)
            return JsonSerializer.Serialize(new { ok = false, error = "write_not_readable" }, CanonTools.JsonOpts);
        return JsonSerializer.Serialize(new { ok = true, created = isNew, record = after }, CanonTools.JsonOpts);
    }

    private static List<string> List(string? incoming, char separator, List<string> current)
    {
        if (incoming is null) return current;
        if (incoming.Trim() == "[]") return [];
        return incoming.Split(separator).Select(s => s.Trim()).Where(s => s.Length > 0).ToList();
    }
}
