using Microsoft.Extensions.Logging;
using Prose.Core.Models;
using Prose.Core.Models.Graph;

namespace Prose.Core.Services;

/// <summary>
/// Pre-write contradiction guard. Given a proposed scene (characters, location,
/// AsOf, synopsis), assembles the relevant Dossiers and reports findings the LLM
/// or the writer should resolve before generation. Catches the obvious classes
/// of drift up front: writing a dead character into a present scene, putting a
/// character somewhere they cannot be, or referring to gear they don't carry.
/// Cheap: just dossier reads + string scans, no LLM calls.
/// </summary>
public class WorldStatePrecheckService
{
    private readonly WorldStateService world;
    private readonly WorldGraphService graph;
    private readonly ILogger<WorldStatePrecheckService> log;

    private static readonly HashSet<string> DeadStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        "deceased", "dead", "killed", "destroyed",
    };

    /// <summary>Node kinds we treat as "gear" — anything a character could draw, wear, or carry.</summary>
    private static readonly HashSet<string> GearKinds = new(StringComparer.OrdinalIgnoreCase)
    {
        "weapon", "weaponry", "equipment", "apparel", "cyberware",
        "consumer_good", "ammunition", "transportation", "pharmaceutical",
    };

    private List<(string Name, string Id, string Kind)>? gearIndex;
    private readonly object gearIndexLock = new();

    public WorldStatePrecheckService(WorldStateService world, WorldGraphService graph, ILogger<WorldStatePrecheckService> log)
    {
        this.world = world;
        this.graph = graph;
        this.log = log;
    }

    /// <summary>Drop the cached gear index. Call after canon edits add/rename gear nodes.</summary>
    public void InvalidateGearIndex()
    {
        lock (gearIndexLock) gearIndex = null;
    }

    private List<(string Name, string Id, string Kind)> GetGearIndex()
    {
        lock (gearIndexLock)
        {
            if (gearIndex != null) return gearIndex;
            graph.EnsureLoaded();
            var list = new List<(string, string, string)>();
            foreach (var kind in GearKinds)
            {
                foreach (var node in graph.GetNodesByType(kind))
                {
                    if (string.IsNullOrWhiteSpace(node.Name) || node.Name.Length < 3) continue;
                    list.Add((node.Name, node.Id, kind));
                }
            }
            // Sort longest-name first so substring scans prefer specific gear ("Smith Pattern .40")
            // over generic ("Smith") when both exist in the index.
            list.Sort((a, b) => b.Item1.Length.CompareTo(a.Item1.Length));
            gearIndex = list;
            return gearIndex;
        }
    }

    /// <summary>
    /// Run every check against the proposed scene and return all findings.
    /// Severity ranking: Block > Warn > Info. Blocks should halt generation;
    /// warns should be surfaced to the LLM as constraints; infos are advisory.
    /// </summary>
    public PrecheckReport Check(PrecheckRequest request)
    {
        var findings = new List<PrecheckFinding>();
        var dossiers = new Dictionary<string, Dossier>(StringComparer.OrdinalIgnoreCase);

        foreach (var name in request.Characters.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(name)) continue;
            var dossier = world.GetDossier(name, request.AsOf);
            if (dossier == null)
            {
                findings.Add(new PrecheckFinding(
                    Severity:  PrecheckSeverity.Warn,
                    Code:      "unknown_entity",
                    EntityRef: name,
                    Message:   $"'{name}' could not be resolved in the world graph. The LLM will fabricate facts about them."));
                continue;
            }
            dossiers[dossier.Subject.Name] = dossier;

            CheckCharacterIsAlive(dossier, findings);
            CheckLocationConsistency(dossier, request.Location, findings);
            CheckGearReferences(dossier, request.Synopsis, findings);
        }

        return new PrecheckReport(findings, dossiers);
    }

    // ── Individual checks ────────────────────────────────────────────────────

    private static void CheckCharacterIsAlive(Dossier dossier, List<PrecheckFinding> findings)
    {
        var status = dossier.Now.Status;
        if (string.IsNullOrEmpty(status)) return;
        if (DeadStatuses.Contains(status))
        {
            findings.Add(new PrecheckFinding(
                Severity:  PrecheckSeverity.Block,
                Code:      "character_deceased",
                EntityRef: dossier.Subject.Name,
                Message:   $"{dossier.Subject.Name} has status '{status}' as of {dossier.AsOf.Describe()}. Putting them in a present-tense scene contradicts canon."));
        }
    }

    private static void CheckLocationConsistency(Dossier dossier, string? sceneLocation, List<PrecheckFinding> findings)
    {
        if (string.IsNullOrWhiteSpace(sceneLocation)) return;
        var current = dossier.Now.Location;
        if (string.IsNullOrWhiteSpace(current)) return; // unknown location — don't false-flag

        if (LooseLocationMatch(current, sceneLocation)) return;

        findings.Add(new PrecheckFinding(
            Severity:  PrecheckSeverity.Warn,
            Code:      "location_mismatch",
            EntityRef: dossier.Subject.Name,
            Message:   $"{dossier.Subject.Name} was last placed at '{current}' but the scene is set in '{sceneLocation}'. Either include a transit beat first or update their location."));
    }

    private void CheckGearReferences(Dossier dossier, string? synopsis, List<PrecheckFinding> findings)
    {
        if (string.IsNullOrWhiteSpace(synopsis)) return;

        // Build the "available to this character" gear set: everything in linked entities
        // (carries/wields/wears/owns) plus everything surfaced by the belongings section.
        var available = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var l in dossier.Linked) available.Add(l.Name);
        foreach (var g in dossier.Now.Holding) available.Add(g);
        foreach (var g in dossier.Now.Wearing) available.Add(g);
        foreach (var section in dossier.Subject.Sections)
        {
            if (!section.Title.Equals("belongings", StringComparison.OrdinalIgnoreCase)) continue;
            foreach (var line in section.Lines)
            {
                // belongings lines are "key: value" or "key: a, b, c". Extract names crudely.
                var idx = line.IndexOf(':');
                var rhs = idx >= 0 ? line[(idx + 1)..] : line;
                foreach (var token in rhs.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                    available.Add(token);
            }
        }

        var index = GetGearIndex();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var (name, _, kind) in index)
        {
            if (synopsis.IndexOf(name, StringComparison.OrdinalIgnoreCase) < 0) continue;
            if (available.Contains(name)) continue;
            if (!seen.Add(name)) continue;

            findings.Add(new PrecheckFinding(
                Severity:  PrecheckSeverity.Warn,
                Code:      "gear_not_available",
                EntityRef: dossier.Subject.Name,
                Message:   $"Synopsis references {kind} '{name}' but {dossier.Subject.Name} doesn't carry/own/wear it according to canon. Either add the acquisition beat or pick gear from the dossier."));
        }
    }

    private static bool LooseLocationMatch(string a, string b)
    {
        var na = Normalize(a);
        var nb = Normalize(b);
        if (na == nb) return true;
        // Substring match handles "Archer's Line, Milwaukee and Damen" vs "Archer's Line".
        return na.Contains(nb, StringComparison.Ordinal) || nb.Contains(na, StringComparison.Ordinal);
    }

    private static string Normalize(string s)
        => System.Text.RegularExpressions.Regex.Replace(s.ToLowerInvariant(), @"[^\w\s]", "").Trim();
}

/// <summary>Input to the precheck. Anything missing just disables that check class.</summary>
public sealed record PrecheckRequest(
    IReadOnlyList<string> Characters,
    string? Location,
    string? Synopsis,
    AsOfCursor AsOf);

public sealed record PrecheckReport(
    IReadOnlyList<PrecheckFinding> Findings,
    IReadOnlyDictionary<string, Dossier> Dossiers)
{
    public bool HasBlockers => Findings.Any(f => f.Severity == PrecheckSeverity.Block);
    public bool HasWarnings => Findings.Any(f => f.Severity == PrecheckSeverity.Warn);

    /// <summary>Render warnings + blockers as constraints to inject into the LLM prompt.</summary>
    public string ToPromptConstraints()
    {
        var relevant = Findings.Where(f => f.Severity != PrecheckSeverity.Info).ToList();
        if (relevant.Count == 0) return "";
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("CONTINUITY CONSTRAINTS (must not contradict):");
        foreach (var f in relevant)
            sb.AppendLine($"  • [{f.Severity}] {f.EntityRef}: {f.Message}");
        return sb.ToString().TrimEnd();
    }
}

public enum PrecheckSeverity { Info, Warn, Block }

public sealed record PrecheckFinding(
    PrecheckSeverity Severity,
    string Code,
    string EntityRef,
    string Message);
