using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MindAttic.Legion;
using StreetSamurai.Core.Data;
using StreetSamurai.Core.Data.Entities;
using StreetSamurai.Core.Interfaces;
using StreetSamurai.Core.Models.Canon;

namespace StreetSamurai.Core.Services;

/// <summary>
/// Legion-persona quality voting for canon entities. Replaces the old LlmVoting
/// (10 GLMZ residents, 1-10 scale) with the same infrastructure as
/// <see cref="StrandReviewService"/>: the full 1000-persona library, 1-100 scale,
/// append-only <see cref="EntityReview"/> rows, cheap ballots + prose upgrades for
/// the most informative voters.
///
/// After each batch: entity.Rating = mean of that batch; entity.VoteCount accumulates.
/// Reviews are queryable by EntityId/EntityType from the SQL DB.
/// </summary>
public class EntityReviewService
{
    private readonly LegionClient legion;
    private readonly VotingConfiguration cfg;
    private readonly IDbContextFactory<StreetSamuraiDbContext> dbFactory;
    private readonly ILogger<EntityReviewService> log;

    private readonly CharacterRepository characters;
    private readonly TechnologyRepository technology;
    private readonly WeaponryRepository weaponry;
    private readonly AmmunitionRepository ammunition;
    private readonly EquipmentRepository equipment;
    private readonly CyberwareRepository cyberware;
    private readonly GenemodRepository genemods;
    private readonly TransportationRepository transportation;
    private readonly AutomatonRepository automata;
    private readonly SubsidiaryRepository subsidiaries;
    private readonly EntertainmentRepository entertainment;
    private readonly ApparelRepository apparel;
    private readonly MaterialRepository materials;
    private readonly PharmaceuticalRepository pharmaceuticals;
    private readonly ConsumerGoodRepository consumerGoods;
    private readonly FactionRepository factions;
    private readonly DistrictRepository districts;
    private readonly ContractRepository contracts;
    private readonly LabSpecimenRepository labSpecimens;
    private readonly PsionicRepository psionics;

    private const int MaxConcurrency = 8;

    public EntityReviewService(
        LegionClient legion,
        VotingConfiguration cfg,
        IDbContextFactory<StreetSamuraiDbContext> dbFactory,
        ILogger<EntityReviewService> log,
        CharacterRepository characters,
        TechnologyRepository technology,
        WeaponryRepository weaponry,
        AmmunitionRepository ammunition,
        EquipmentRepository equipment,
        CyberwareRepository cyberware,
        GenemodRepository genemods,
        TransportationRepository transportation,
        AutomatonRepository automata,
        SubsidiaryRepository subsidiaries,
        EntertainmentRepository entertainment,
        ApparelRepository apparel,
        MaterialRepository materials,
        PharmaceuticalRepository pharmaceuticals,
        ConsumerGoodRepository consumerGoods,
        FactionRepository factions,
        DistrictRepository districts,
        ContractRepository contracts,
        LabSpecimenRepository labSpecimens,
        PsionicRepository psionics)
    {
        this.legion          = legion;
        this.cfg             = cfg;
        this.dbFactory       = dbFactory;
        this.log             = log;
        this.characters      = characters;
        this.technology      = technology;
        this.weaponry        = weaponry;
        this.ammunition      = ammunition;
        this.equipment       = equipment;
        this.cyberware       = cyberware;
        this.genemods        = genemods;
        this.transportation  = transportation;
        this.automata        = automata;
        this.subsidiaries    = subsidiaries;
        this.entertainment   = entertainment;
        this.apparel         = apparel;
        this.materials       = materials;
        this.pharmaceuticals = pharmaceuticals;
        this.consumerGoods   = consumerGoods;
        this.factions        = factions;
        this.districts       = districts;
        this.contracts       = contracts;
        this.labSpecimens    = labSpecimens;
        this.psionics        = psionics;
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>Review all entities (or just those in <paramref name="entityType"/>).
    /// <paramref name="skipRated"/> limits to Rating == 0 entries only.</summary>
    public async Task ReviewAllAsync(
        bool skipRated = false,
        int ballotCount = 30,
        int proseCount = 5,
        string? entityType = null,
        CancellationToken ct = default)
    {
        log.LogInformation("EntityReviewService: ReviewAllAsync (skipRated={Skip}, ballots={B}, prose={P}, type={T})",
            skipRated, ballotCount, proseCount, entityType ?? "all");
        await RunBatches(skipRated, ballotCount, proseCount, entityType, ct);
        log.LogInformation("EntityReviewService: ReviewAllAsync complete");
    }

    /// <summary>Returns all reviewed entities (Rating > 0) across all repos,
    /// sorted by Rating descending — same signature as EntityRatingService.GetAllRated().</summary>
    public IEnumerable<(string Name, string Type, string Route, string Id, double Rating, int VoteCount)> GetAllReviewed()
    {
        var results = new List<(string Name, string Type, string Route, string Id, double Rating, int VoteCount)>();

        void Collect<T>(List<T> entities, Func<T, string> name, string type, string route) where T : ICanonEntity
        {
            foreach (var e in entities.Where(e => e.Rating > 0))
                results.Add((name(e), type, route, e.Id, e.Rating, e.VoteCount));
        }

        Collect(characters.GetAll(),      e => e.Name,      "character",     "/characters");
        Collect(technology.GetAll(),      e => e.Name,      "technology",    "/technology");
        Collect(weaponry.GetAll(),        e => e.Name,      "weapon",        "/weaponry");
        Collect(ammunition.GetAll(),      e => e.Name,      "ammunition",    "/ammunition");
        Collect(equipment.GetAll(),       e => e.Name,      "equipment",     "/equipment");
        Collect(cyberware.GetAll(),       e => e.Name,      "cyberware",     "/cyberware");
        Collect(genemods.GetAll(),        e => e.Name,      "genemod",       "/genemods");
        Collect(transportation.GetAll(),  e => e.Name,      "transportation","/transportation");
        Collect(automata.GetAll(),        e => e.Name,      "automaton",     "/automata");
        Collect(subsidiaries.GetAll(),    e => e.Name,      "subsidiary",    "/subsidiaries");
        Collect(entertainment.GetAll(),   e => e.Name,      "entertainment", "/entertainment");
        Collect(apparel.GetAll(),         e => e.Name,      "apparel",       "/apparel");
        Collect(materials.GetAll(),       e => e.Name,      "material",      "/materials");
        Collect(pharmaceuticals.GetAll(), e => e.Name,      "pharmaceutical","/pharmaceuticals");
        Collect(consumerGoods.GetAll(),   e => e.Name,      "consumer-good", "/goods");
        Collect(factions.GetAll(),        e => e.Name,      "faction",       "/factions");
        Collect(districts.GetAll(),       e => e.Name,      "district",      "/places");
        Collect(contracts.GetAll(),       e => e.Codename,  "contract",      "/contracts");
        Collect(labSpecimens.GetAll(),    e => e.Name,      "lab-specimen",  "/lab-specimens");
        Collect(psionics.GetAll(),        e => e.Name,      "psionic",       "/psionics");

        return results.OrderByDescending(r => r.Rating);
    }

    // ── Private — batch runner ────────────────────────────────────────────────

    private async Task RunBatches(bool skipRated, int ballotCount, int proseCount, string? entityType, CancellationToken ct)
    {
        var all = string.IsNullOrWhiteSpace(entityType);
        if (all || entityType == "character")     await ReviewBatch(characters.GetAll(),     e => (e.Id, e.Name, e.Description),         e => characters.Save(e),     "character",     skipRated, ballotCount, proseCount, ct);
        if (all || entityType == "technology")    await ReviewBatch(technology.GetAll(),     e => (e.Id, e.Name, e.Description),         e => technology.Save(e),     "technology",    skipRated, ballotCount, proseCount, ct);
        if (all || entityType == "weapon")        await ReviewBatch(weaponry.GetAll(),       e => (e.Id, e.Name, e.Description),         e => weaponry.Save(e),       "weapon",        skipRated, ballotCount, proseCount, ct);
        if (all || entityType == "ammunition")    await ReviewBatch(ammunition.GetAll(),     e => (e.Id, e.Name, e.Description),         e => ammunition.Save(e),     "ammunition",    skipRated, ballotCount, proseCount, ct);
        if (all || entityType == "equipment")     await ReviewBatch(equipment.GetAll(),      e => (e.Id, e.Name, e.Description),         e => equipment.Save(e),      "equipment",     skipRated, ballotCount, proseCount, ct);
        if (all || entityType == "cyberware")     await ReviewBatch(cyberware.GetAll(),      e => (e.Id, e.Name, e.Description),         e => cyberware.Save(e),      "cyberware",     skipRated, ballotCount, proseCount, ct);
        if (all || entityType == "genemod")       await ReviewBatch(genemods.GetAll(),       e => (e.Id, e.Name, e.Description),         e => genemods.Save(e),       "genemod",       skipRated, ballotCount, proseCount, ct);
        if (all || entityType == "transportation")await ReviewBatch(transportation.GetAll(), e => (e.Id, e.Name, e.Description),         e => transportation.Save(e), "transportation",skipRated, ballotCount, proseCount, ct);
        if (all || entityType == "automaton")     await ReviewBatch(automata.GetAll(),       e => (e.Id, e.Name, e.Description),         e => automata.Save(e),       "automaton",     skipRated, ballotCount, proseCount, ct);
        if (all || entityType == "subsidiary")    await ReviewBatch(subsidiaries.GetAll(),   e => (e.Id, e.Name, e.Description),         e => subsidiaries.Save(e),   "subsidiary",    skipRated, ballotCount, proseCount, ct);
        if (all || entityType == "entertainment") await ReviewBatch(entertainment.GetAll(),  e => (e.Id, e.Name, e.Description),         e => entertainment.Save(e),  "entertainment", skipRated, ballotCount, proseCount, ct);
        if (all || entityType == "apparel")       await ReviewBatch(apparel.GetAll(),        e => (e.Id, e.Name, e.Description),         e => apparel.Save(e),        "apparel",       skipRated, ballotCount, proseCount, ct);
        if (all || entityType == "material")      await ReviewBatch(materials.GetAll(),      e => (e.Id, e.Name, e.Description),         e => materials.Save(e),      "material",      skipRated, ballotCount, proseCount, ct);
        if (all || entityType == "pharmaceutical")await ReviewBatch(pharmaceuticals.GetAll(),e => (e.Id, e.Name, e.Description),         e => pharmaceuticals.Save(e),"pharmaceutical",skipRated, ballotCount, proseCount, ct);
        if (all || entityType == "consumer-good") await ReviewBatch(consumerGoods.GetAll(),  e => (e.Id, e.Name, e.Description),         e => consumerGoods.Save(e),  "consumer-good", skipRated, ballotCount, proseCount, ct);
        if (all || entityType == "faction")       await ReviewBatch(factions.GetAll(),       e => (e.Id, e.Name, e.Description),         e => factions.Save(e),       "faction",       skipRated, ballotCount, proseCount, ct);
        if (all || entityType == "district")      await ReviewBatch(districts.GetAll(),      e => (e.Id, e.Name, e.Description),         e => districts.Save(e),      "district",      skipRated, ballotCount, proseCount, ct);
        if (all || entityType == "contract")      await ReviewBatch(contracts.GetAll(),      e => (e.Id, e.Codename, e.Description),     e => contracts.Save(e),      "contract",      skipRated, ballotCount, proseCount, ct);
        if (all || entityType == "lab-specimen")  await ReviewBatch(labSpecimens.GetAll(),   e => (e.Id, e.Name, e.PhysicalDescription), e => labSpecimens.Save(e),   "lab-specimen",  skipRated, ballotCount, proseCount, ct);
        if (all || entityType == "psionic")       await ReviewBatch(psionics.GetAll(),       e => (e.Id, e.Name, e.Mechanism),           e => psionics.Save(e),       "psionic",       skipRated, ballotCount, proseCount, ct);
    }

    private async Task ReviewBatch<T>(
        List<T> entities,
        Func<T, (string id, string name, string text)> getContext,
        Action<T> save,
        string entityType,
        bool skipRated,
        int ballotCount,
        int proseCount,
        CancellationToken ct) where T : ICanonEntity
    {
        var targets = skipRated ? entities.Where(e => e.Rating == 0).ToList() : entities;
        if (targets.Count == 0) return;

        log.LogInformation("EntityReview: {Count} {Type} entities ({Ballots} ballots + {Prose} prose each)",
            targets.Count, entityType, ballotCount, proseCount);

        var providers = ReviewProviderIds();
        if (providers.Count == 0) { log.LogWarning("No providers configured — skipping"); return; }

        foreach (var entity in targets)
        {
            if (ct.IsCancellationRequested) break;

            var (entityId, name, rawText) = getContext(entity);
            var text = (rawText ?? "").Length > 3000 ? rawText![..3000] : rawText ?? "";
            var contentHash = ComputeContentHash(name, text);

            // ── Tier 1: cheap ballots ─────────────────────────────────────────
            var personas = SampleEnrichedPersonas(ballotCount);
            var sem = new SemaphoreSlim(MaxConcurrency);
            var bag = new ConcurrentBag<EntityReview>();
            var failed = 0;

            await Task.WhenAll(personas.Select((persona, i) => Task.Run(async () =>
            {
                await sem.WaitAsync(ct);
                try
                {
                    var provider = providers[i % providers.Count];
                    var r = await BallotOnceAsync(entityId, entityType, name, text, contentHash, persona, provider, ct);
                    if (r != null) bag.Add(r);
                    else Interlocked.Increment(ref failed);
                }
                catch (Exception ex) { Interlocked.Increment(ref failed); log.LogWarning(ex, "Ballot failed: {P}", persona.Id); }
                finally { sem.Release(); }
            }, ct)));

            var saved = bag.ToList();
            if (saved.Count == 0)
            {
                log.LogWarning("EntityReview: {Type} '{Name}' — all {N} ballots failed", entityType, name, ballotCount);
                continue;
            }

            // ── Tier 2: prose upgrades on the most informative ballots ────────
            if (proseCount > 0)
            {
                var picks = SelectInformative(saved, Math.Min(proseCount, saved.Count));
                var psem = new SemaphoreSlim(MaxConcurrency);
                await Task.WhenAll(picks.Select(rv => Task.Run(async () =>
                {
                    await psem.WaitAsync(ct);
                    try
                    {
                        var persona = PersonasByIds([rv.PersonaId]).FirstOrDefault();
                        if (persona == null) return;
                        var prose = await ProseOnceAsync(entityType, name, text, persona, rv.ProviderId, ct);
                        if (prose != null) { rv.ReviewText = prose.Value.review; rv.Improvements = prose.Value.improvements; }
                    }
                    catch (Exception ex) { log.LogWarning(ex, "Prose upgrade failed: {P}", rv.PersonaId); }
                    finally { psem.Release(); }
                }, ct)));
            }

            // ── Persist + update entity.Rating ────────────────────────────────
            await using (var db = await dbFactory.CreateDbContextAsync(ct))
            {
                db.EntityReviews.AddRange(saved);
                await db.SaveChangesAsync(ct);
            }

            var mean = saved.Average(r => (double)r.Score);
            entity.Rating    = Math.Round(mean, 1);
            entity.VoteCount = entity.VoteCount + saved.Count;
            save(entity);

            log.LogInformation(
                "EntityReview: {Type} '{Name}' → {Rating} ({New} ballots, {F} failed, {Prose} prose)",
                entityType, name, entity.Rating, saved.Count, failed, proseCount > 0 ? saved.Count(r => !string.IsNullOrEmpty(r.ReviewText)) : 0);
        }
    }

    // ── Single entity ballot ──────────────────────────────────────────────────

    private async Task<EntityReview?> BallotOnceAsync(
        string entityId, string entityType, string name, string text,
        string contentHash, Persona persona, string provider, CancellationToken ct)
    {
        var key = ResolveKey(provider);
        if (string.IsNullOrWhiteSpace(key)) return null;

        var model = cfg.ModelOverrides.TryGetValue(provider, out var m) && !string.IsNullOrWhiteSpace(m)
            ? m : LegionClient.DefaultModels.GetValueOrDefault(provider, "");

        var system = BuildBallotPrompt(persona, entityType, name);
        var raw = await legion.CallAsync(provider, key, model, system, text, maxTokens: 300, temperature: 0.85, ct);

        if (!TryParseBallot(raw, out var score, out var weakness)) return null;

        return new EntityReview
        {
            Id           = Guid.CreateVersion7(),
            EntityId     = entityId,
            EntityType   = entityType,
            EntityName   = name,
            PersonaId    = persona.Id,
            PersonaName  = persona.Name,
            PersonaBlurb = FirstLine(persona.PersonalityMarkdown),
            ProviderId   = provider,
            Model        = string.IsNullOrWhiteSpace(model) ? null : model,
            Score        = Math.Clamp(score, 1, 100),
            ReviewText   = "",
            Improvements = string.IsNullOrWhiteSpace(weakness) ? null : weakness.Trim(),
            ContentHash  = contentHash,
            ReviewedAt   = DateTime.UtcNow,
            CreatedAt    = DateTime.UtcNow,
            UpdatedAt    = DateTime.UtcNow,
        };
    }

    private async Task<(string review, string? improvements)?> ProseOnceAsync(
        string entityType, string name, string text,
        Persona persona, string provider, CancellationToken ct)
    {
        var key = ResolveKey(provider);
        if (string.IsNullOrWhiteSpace(key)) return null;

        var model = cfg.ModelOverrides.TryGetValue(provider, out var m) && !string.IsNullOrWhiteSpace(m)
            ? m : LegionClient.DefaultModels.GetValueOrDefault(provider, "");

        var system = BuildReviewPrompt(persona, entityType, name);
        var raw = await legion.CallAsync(provider, key, model, system, text, maxTokens: 800, temperature: 0.85, ct);

        if (!TryParseReview(raw, out _, out var review, out var improvements)) return null;
        return (review.Trim(), improvements.Count > 0 ? string.Join("\n", improvements) : null);
    }

    // ── Prompt builders ───────────────────────────────────────────────────────

    private string BuildBallotPrompt(Persona persona, string entityType, string name)
    {
        var who = BuildWhoBlock(persona);
        return
$@"{who}

You are rating a worldbuilding entry from GLMZ (Greater Lake Michigan Zone, 2225) — a cyberpunk city ceded to corporate sovereignty.

The entry below describes a {entityType} called ""{name}"".

Read it as the person described above. Rate how compelling, original, and well-crafted this entry is — whether it would stick with you, feel alive, or reveal something true about how this world works. Reserve high scores for entries that genuinely surprise or unsettle you. Mediocre entries are common.

Return ONLY a JSON object, nothing else:
{{""score"": <integer 1-100>, ""weakness"": ""<your single biggest gripe in 8 words or fewer, or 'none'>""}}";
    }

    private string BuildReviewPrompt(Persona persona, string entityType, string name)
    {
        var who = BuildWhoBlock(persona);
        return
$@"{who}

You are reviewing a worldbuilding entry from GLMZ (Greater Lake Michigan Zone, 2225) — a cyberpunk city ceded to corporate sovereignty.

The entry below describes a {entityType} called ""{name}"".

Read it as the person described above. Write 2-3 honest sentences: is it memorable, original, well-crafted? Does it feel alive or generic? Would you want to encounter this {entityType} in a story? Be specific. Not flattering.

Return ONLY a JSON object, nothing else:
{{""score"": <integer 1-100>, ""review"": ""<your 2-3 sentence honest review>"", ""improvements"": [""<concrete fix>""]}}";
    }

    private string BuildWhoBlock(Persona persona)
    {
        var who = string.IsNullOrWhiteSpace(persona.PersonalityMarkdown)
            ? "You are an opinionated reader with strong opinions."
            : persona.PersonalityMarkdown;

        var profile = PersonaLibrary.GetProfile(persona.Id);
        if (profile != null)
            who +=
$@"

YOUR MEASURED PSYCHOMETRIC PROFILE — let it shape what you notice and how you score: {profile.Summary()}.
React as THIS person: high Openness welcomes strange and original; low wants clarity. High Conscientiousness has no patience for loose, vague, underdeveloped entries; lower forgives for energy. Be honest, not generous.";

        who +=
@"

ONE MORE THING: you are a die-hard cyberpunk reader (Neuromancer, Snow Crash, Count Zero, Diamond Age, Hardwired). You know the difference between world-building that feels earned — specific, lived-in, with real texture — and generic setting-filler that performs profundity without containing any. Judge accordingly.";

        return who;
    }

    // ── Parse helpers ─────────────────────────────────────────────────────────

    private static bool TryParseBallot(string? raw, out int score, out string? weakness)
    {
        score = 0; weakness = null;
        if (string.IsNullOrWhiteSpace(raw)) return false;
        var json = ExtractJson(raw);
        if (json == null) return false;
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (!root.TryGetProperty("score", out var s) || !s.TryGetInt32(out score)) return false;
            weakness = root.TryGetProperty("weakness", out var w) ? w.GetString() : null;
            if (weakness is "none" or "None" or "") weakness = null;
            return score > 0;
        }
        catch { return false; }
    }

    private static bool TryParseReview(string? raw, out int score, out string review, out List<string> improvements)
    {
        score = 0; review = ""; improvements = new List<string>();
        if (string.IsNullOrWhiteSpace(raw)) return false;
        var json = ExtractJson(raw);
        if (json == null) return false;
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (!root.TryGetProperty("score", out var s) || !s.TryGetInt32(out score)) return false;
            review = root.TryGetProperty("review", out var r) ? r.GetString() ?? "" : "";
            if (root.TryGetProperty("improvements", out var imp) && imp.ValueKind == JsonValueKind.Array)
                foreach (var item in imp.EnumerateArray())
                { var t = item.GetString(); if (!string.IsNullOrWhiteSpace(t)) improvements.Add(t.Trim()); }
            return score > 0;
        }
        catch { return false; }
    }

    private static string? ExtractJson(string raw)
    {
        // Strip code fences if present
        var m = Regex.Match(raw, @"```(?:json)?\s*([\s\S]*?)```");
        if (m.Success) raw = m.Groups[1].Value;
        var start = raw.IndexOf('{');
        var end   = raw.LastIndexOf('}');
        return start >= 0 && end > start ? raw[start..(end + 1)] : null;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static List<EntityReview> SelectInformative(List<EntityReview> all, int k)
    {
        if (k >= all.Count) return all.ToList();
        var ordered = all.OrderBy(r => r.Score).ToList();
        int low  = Math.Max(1, k * 3 / 10);
        int high = Math.Max(1, k * 3 / 10);
        int mid  = Math.Max(0, k - low - high);
        var picked = new List<EntityReview>();
        picked.AddRange(ordered.Take(low));
        picked.AddRange(ordered.Skip(Math.Max(low, ordered.Count - high)).Take(high));
        if (mid > 0)
        {
            int start = Math.Clamp(ordered.Count / 2 - mid / 2, 0, Math.Max(0, ordered.Count - mid));
            picked.AddRange(ordered.Skip(start).Take(mid));
        }
        return picked.DistinctBy(r => r.Id).Take(k).ToList();
    }

    private static List<Persona> SampleEnrichedPersonas(int count)
    {
        var pool = PersonaLibrary.Enriched.ToList();
        var rng  = Random.Shared;
        for (int i = 0; i < Math.Min(count, pool.Count); i++)
        {
            int j = rng.Next(i, pool.Count);
            (pool[i], pool[j]) = (pool[j], pool[i]);
        }
        return pool.Take(count).ToList();
    }

    private static List<Persona> PersonasByIds(IReadOnlyList<string> ids)
    {
        var byId = PersonaLibrary.All.ToDictionary(p => p.Id);
        var list = new List<Persona>(ids.Count);
        foreach (var id in ids)
            if (byId.TryGetValue(id, out var p)) list.Add(p);
        return list;
    }

    private List<string> ReviewProviderIds() => cfg.ActiveProviderIds.ToList();

    private string? ResolveKey(string provider)
    {
        if (cfg.ApiKeys.TryGetValue(provider, out var k) && !string.IsNullOrWhiteSpace(k)) return k;
        return MindAtticCredentialStore.GetKey(provider);
    }

    private static string ComputeContentHash(string name, string text)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(name + "\n" + text));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static string? FirstLine(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;
        var i = s.IndexOfAny(['\r', '\n']);
        return (i < 0 ? s : s[..i]).Trim();
    }
}
