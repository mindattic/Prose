using MindAttic.Legion;
using Microsoft.Extensions.Logging;
using StreetSamurai.Core.Models;

namespace StreetSamurai.Core.Services;

/// <summary>
/// Owns the reusable expert-persona table used for beat-generation panels.
/// Personas are persisted via <see cref="SettingsKvStore"/> as a single JSON
/// document so they survive restarts; on first read, an empty table is seeded
/// from the curated <see cref="ExpertPersonaCatalog"/> starter set.
///
/// <para><b>Selection.</b> <see cref="SelectPertinentAsync"/> runs a small
/// Haiku-class panel (low tier, fast) to pick the top-N personas most
/// pertinent to a scene context. The vote sees every persona's name + lens
/// snippet + tags and picks by id. This is the path
/// <see cref="BeatGeneratorService.SuggestNextBeatsAsync"/> takes per call —
/// it lets the table grow to hundreds of personas without making every
/// generation call use all of them.</para>
/// </summary>
public class ExpertPersonaService
{
    private const string KvKey = "expert_personas";

    private readonly SettingsKvStore kv;
    private readonly LLMVotingService? voting;
    private readonly ILogger<ExpertPersonaService> log;

    public ExpertPersonaService(SettingsKvStore kv, ILogger<ExpertPersonaService> log, LLMVotingService? voting = null)
    {
        this.kv      = kv;
        this.voting  = voting;
        this.log     = log;
    }

    /// <summary>Read every persona, seeding from the catalog when the store is empty.</summary>
    public List<ExpertPersona> ListAll()
    {
        var doc = kv.Get<ExpertPersonaCollection>(KvKey);
        if (doc != null && doc.Personas.Count > 0) return doc.Personas;
        // First-run seed.
        var seeded = new ExpertPersonaCollection
        {
            Personas  = ExpertPersonaCatalog.Starter().ToList(),
            SeededAt  = DateTime.UtcNow,
        };
        kv.Set(KvKey, seeded);
        log.LogInformation("ExpertPersonas: seeded {Count} starter personas", seeded.Personas.Count);
        return seeded.Personas;
    }

    public ExpertPersona? Get(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        return ListAll().FirstOrDefault(p => p.Id == id);
    }

    /// <summary>Add or update a persona; persists immediately.</summary>
    public void Save(ExpertPersona persona)
    {
        var doc = kv.Get<ExpertPersonaCollection>(KvKey) ?? new ExpertPersonaCollection();
        if (doc.Personas.Count == 0)
            doc.Personas = ExpertPersonaCatalog.Starter().ToList();

        var existing = doc.Personas.FirstOrDefault(p => p.Id == persona.Id);
        if (existing == null)
        {
            persona.Created  = DateTime.UtcNow;
            persona.Modified = DateTime.UtcNow;
            doc.Personas.Add(persona);
        }
        else
        {
            existing.Name     = persona.Name;
            existing.Lens     = persona.Lens;
            existing.Tags     = persona.Tags;
            existing.Modified = DateTime.UtcNow;
        }
        kv.Set(KvKey, doc);
    }

    public void Delete(string id)
    {
        var doc = kv.Get<ExpertPersonaCollection>(KvKey);
        if (doc == null) return;
        doc.Personas.RemoveAll(p => p.Id == id);
        kv.Set(KvKey, doc);
    }

    /// <summary>
    /// Pick the top-<paramref name="n"/> personas most relevant to the scene
    /// context. Runs a Haiku-class panel (low tier, fast) where each voter
    /// returns the ids of their top-<paramref name="n"/> personas; the
    /// final selection is the union ranked by vote count, top-N taken.
    ///
    /// Falls back to a tag-overlap heuristic when voting is unavailable
    /// (no LLM keys configured) so the selector still produces a usable
    /// panel offline.
    /// </summary>
    public async Task<List<ExpertPersona>> SelectPertinentAsync(
        string sceneContext, int n = 10, CancellationToken ct = default)
    {
        var all = ListAll();
        if (all.Count == 0) return new List<ExpertPersona>();
        if (all.Count <= n) return all.ToList();
        if (voting is null) return TagHeuristic(all, sceneContext, n);

        // One prompt per persona is too expensive — single prompt asks the voter
        // to return a JSON array of N ids most pertinent to the scene. Using
        // Haiku-class everywhere because the call is high-volume per beat
        // workflow but each individual decision is cheap.
        var ctxBuilder = new System.Text.StringBuilder();
        ctxBuilder.AppendLine("EXPERT PERSONAS:");
        foreach (var p in all)
        {
            var tags = p.Tags.Count > 0 ? $" [tags: {string.Join(", ", p.Tags)}]" : "";
            ctxBuilder.AppendLine($"  {p.Id} — {p.Name}{tags}");
            ctxBuilder.AppendLine($"     {Truncate(p.Lens, 200)}");
        }
        ctxBuilder.AppendLine();
        ctxBuilder.AppendLine("SCENE CONTEXT:");
        ctxBuilder.AppendLine(sceneContext.Length > 4000 ? sceneContext[^4000..] : sceneContext);

        var request = new VoteRequest
        {
            Question =
                $"Which {n} expert personas (by id) would notice the most that's worth surfacing " +
                "in this scene? Pick by relevance to the scene's specific situation, not by general " +
                $"prestige. Output STRICT JSON: an array of exactly {n} string ids, in order from " +
                "most relevant first. No prose outside the JSON.",
            Context = ctxBuilder.ToString(),
            MaxTokens = 512,
            Temperature = 0.4,
            SynthesizeNarrative = false,
        };

        var voters = BuildLowTierVoters();
        VotingResult result;
        try { result = await voting.VoteWithProfilesAsync(request, Quorum.Plurality, voters, ct); }
        catch (Exception ex)
        {
            log.LogWarning(ex, "ExpertPersonas: SelectPertinent voting failed; falling back to tag heuristic");
            return TagHeuristic(all, sceneContext, n);
        }

        var counts = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var v in result.IndividualVotes.Where(v => !v.IsError))
        {
            var payload = !string.IsNullOrWhiteSpace(v.Decision) ? v.Decision : v.Reasoning;
            foreach (var id in ParseIdArray(payload))
                counts[id] = counts.GetValueOrDefault(id, 0) + 1;
        }

        if (counts.Count == 0) return TagHeuristic(all, sceneContext, n);

        return counts
            .OrderByDescending(kv => kv.Value)
            .Select(kv => all.FirstOrDefault(p => p.Id == kv.Key))
            .Where(p => p != null)
            .Cast<ExpertPersona>()
            .Take(n)
            .ToList();
    }

    /// <summary>4-voter Haiku-class panel for the persona-selection vote — cheap and fast.</summary>
    private static List<VoterProfile> BuildLowTierVoters()
    {
        var providers = new[] { "claude", "openai", "gemini", "deepseek" };
        var voters = new List<VoterProfile>(providers.Length);
        foreach (var pid in providers)
        {
            voters.Add(new VoterProfile
            {
                VoterId       = $"selector-{pid}-{Guid.NewGuid().ToString("N")[..8]}",
                Name          = $"Persona Selector ({pid})",
                ProviderId    = pid,
                ModelOverride = pid switch
                {
                    "claude"   => "claude-haiku-4-5-20251001",
                    "openai"   => "gpt-4.1-nano",
                    "gemini"   => "gemini-2.0-flash",
                    "deepseek" => "deepseek-chat",
                    _          => null,
                },
                PersonalityMarkdown = "",
            });
        }
        return voters;
    }

    /// <summary>Tag-overlap fallback when voting is unavailable. Score = tag matches with simple keyword scan.</summary>
    private static List<ExpertPersona> TagHeuristic(IReadOnlyList<ExpertPersona> all, string scene, int n)
    {
        var lower = (scene ?? "").ToLowerInvariant();
        return all
            .Select(p => (Persona: p, Score: p.Tags.Sum(t => lower.Contains(t.ToLowerInvariant()) ? 1 : 0)))
            .OrderByDescending(x => x.Score)
            .ThenBy(x => x.Persona.Name)
            .Take(n)
            .Select(x => x.Persona)
            .ToList();
    }

    private static IEnumerable<string> ParseIdArray(string? payload)
    {
        if (string.IsNullOrWhiteSpace(payload)) yield break;
        var start = payload.IndexOf('[');
        var end   = payload.LastIndexOf(']');
        if (start < 0 || end <= start) yield break;
        var json = payload[start..(end + 1)];
        System.Text.Json.JsonDocument doc;
        try { doc = System.Text.Json.JsonDocument.Parse(json); }
        catch { yield break; }
        using (doc)
        {
            if (doc.RootElement.ValueKind != System.Text.Json.JsonValueKind.Array) yield break;
            foreach (var e in doc.RootElement.EnumerateArray())
            {
                if (e.ValueKind == System.Text.Json.JsonValueKind.String)
                {
                    var id = e.GetString();
                    if (!string.IsNullOrWhiteSpace(id)) yield return id;
                }
            }
        }
    }

    private static string Truncate(string s, int n) => s.Length <= n ? s : s[..n] + "…";
}
