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
/// <see cref="NodeReviewService"/>: the full 1000-persona library, 1-100 scale,
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
    private readonly VotingGate votingGate;

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

    private const int MaxConcurrency      = 8;
    private const int MaxConcurrencyLocal = 20;

    public EntityReviewService(
        LegionClient legion,
        VotingConfiguration cfg,
        IDbContextFactory<StreetSamuraiDbContext> dbFactory,
        ILogger<EntityReviewService> log,
        VotingGate votingGate,
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
        this.votingGate      = votingGate;
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
    /// <paramref name="skipRated"/> limits to entities with no existing reviews.
    /// When <paramref name="localUrl"/> is set, ballots route to that OpenAI-compatible
    /// endpoint (e.g. RunPod vLLM) instead of the configured Legion cloud providers.</summary>
    public async Task ReviewAllAsync(
        bool skipRated = false,
        int ballotCount = 30,
        int proseCount = 5,
        string? entityType = null,
        string? localUrl = null,
        string? localKey = null,
        string? localModel = null,
        CancellationToken ct = default,
        bool allowVotes = false)
    {
        votingGate.EnsureAllowed("review-entity", allowVotes);
        log.LogInformation(
            "EntityReviewService: ReviewAllAsync (skipRated={Skip}, ballots={B}, prose={P}, type={T}, local={L})",
            skipRated, ballotCount, proseCount, entityType ?? "all", localUrl ?? "cloud");
        await RunBatches(skipRated, ballotCount, proseCount, entityType, localUrl, localKey, localModel, ct);
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

    // ── All entity types flow through ReviewGenericBatchAsync ────────────────
    // The typed-repo path (EfRepository<T>.GetAll) reads from the Records JSON
    // table which is empty — all entity data lives in the typed SQL tables.
    // Querying db.Entities directly (Description column) is the correct path
    // for scoring. EntityReviewSummaries is updated after each entity.
    private async Task RunBatches(bool skipRated, int ballotCount, int proseCount, string? entityType,
        string? localUrl, string? localKey, string? localModel, CancellationToken ct)
    {
        var all = string.IsNullOrWhiteSpace(entityType);

        // All non-character entity types — scored via Entities.Description.
        var knownTypes = new[]
        {
            "technology", "weapon", "ammunition", "equipment", "cyberware",
            "genemod", "transportation", "automaton", "subsidiary", "entertainment",
            "apparel", "material", "pharmaceutical", "consumer_good", "faction",
            "place", "contract", "lab_specimen", "psionic", "corponation",
            "document", "motif", "vocabulary", "news", "archetype", "quote",
            "flyover_entity", "synthetic", "schism-entity", "person", "organization",
            "character"
        };

        foreach (var t in knownTypes)
        {
            if (all || entityType == t)
                await ReviewGenericBatchAsync(t, skipRated, ballotCount, proseCount, localUrl, localKey, localModel, ct);
        }

        // Unknown types: if --type was supplied and didn't match anything above, still attempt it.
        if (!all && !knownTypes.Contains(entityType, StringComparer.OrdinalIgnoreCase))
            await ReviewGenericBatchAsync(entityType!, skipRated, ballotCount, proseCount, localUrl, localKey, localModel, ct);
    }

    private async Task ReviewBatch<T>(
        List<T> entities,
        Func<T, (string id, string name, string text)> getContext,
        Action<T> save,
        string entityType,
        bool skipRated,
        int ballotCount,
        int proseCount,
        string? localUrl,
        string? localKey,
        string? localModel,
        CancellationToken ct) where T : ICanonEntity
    {
        var targets = skipRated ? entities.Where(e => e.Rating == 0).ToList() : entities;
        if (targets.Count == 0) return;

        log.LogInformation("EntityReview: {Count} {Type} entities ({Ballots} ballots + {Prose} prose each)",
            targets.Count, entityType, ballotCount, proseCount);

        var useLocal = !string.IsNullOrWhiteSpace(localUrl);
        var providers = useLocal ? ["local"] : ReviewProviderIds();
        if (providers.Count == 0) { log.LogWarning("No providers configured — skipping"); return; }
        var concurrency = useLocal ? MaxConcurrencyLocal : MaxConcurrency;

        foreach (var entity in targets)
        {
            if (ct.IsCancellationRequested) break;

            var (entityId, name, rawText) = getContext(entity);
            var text = (rawText ?? "").Length > 3000 ? rawText![..3000] : rawText ?? "";
            var contentHash = ComputeContentHash(name, text);

            // ── Tier 1: cheap ballots ─────────────────────────────────────────
            var personas = SampleEnrichedPersonas(ballotCount);
            var sem = new SemaphoreSlim(concurrency);
            var bag = new ConcurrentBag<EntityReview>();
            var failed = 0;

            await Task.WhenAll(personas.Select((persona, i) => Task.Run(async () =>
            {
                await sem.WaitAsync(ct);
                try
                {
                    var provider = useLocal ? "local" : providers[i % providers.Count];
                    var r = await BallotOnceAsync(entityId, entityType, name, text, contentHash,
                                persona, provider, localUrl, localKey, localModel, ct);
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
                var psem = new SemaphoreSlim(concurrency);
                await Task.WhenAll(picks.Select(rv => Task.Run(async () =>
                {
                    await psem.WaitAsync(ct);
                    try
                    {
                        var persona = PersonasByIds([rv.PersonaId]).FirstOrDefault();
                        if (persona == null) return;
                        var provider = useLocal ? "local" : rv.ProviderId;
                        var prose = await ProseOnceAsync(entityType, name, text, persona,
                                        provider, localUrl, localKey, localModel, ct);
                        if (prose != null) { rv.ReviewText = prose.Value.review; rv.Improvements = prose.Value.improvements; }
                    }
                    catch (Exception ex) { log.LogWarning(ex, "Prose upgrade failed: {P}", rv.PersonaId); }
                    finally { psem.Release(); }
                }, ct)));
            }

            // ── Persist + update entity.Rating + EntityReviewSummary ─────────
            await using (var db = await dbFactory.CreateDbContextAsync(ct))
            {
                db.EntityReviews.AddRange(saved);
                await db.SaveChangesAsync(ct);
            }
            await UpsertSummaryAsync(entityId, entityType, name, contentHash, ct);

            var mean = saved.Average(r => (double)r.Score);
            entity.Rating    = Math.Round(mean, 1);
            entity.VoteCount = entity.VoteCount + saved.Count;
            save(entity);

            log.LogInformation(
                "EntityReview: {Type} '{Name}' → {Rating} ({New} ballots, {F} failed, {Prose} prose)",
                entityType, name, entity.Rating, saved.Count, failed, proseCount > 0 ? saved.Count(r => !string.IsNullOrEmpty(r.ReviewText)) : 0);
        }
    }

    // ── Generic batch — direct Entities table, no typed repo ─────────────────

    private async Task ReviewGenericBatchAsync(
        string entityType, bool skipRated, int ballotCount, int proseCount,
        string? localUrl, string? localKey, string? localModel, CancellationToken ct)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        HashSet<string>? reviewed = null;
        if (skipRated)
        {
            reviewed = (await db.EntityReviews
                .Where(r => r.EntityType == entityType)
                .Select(r => r.EntityId)
                .Distinct()
                .ToListAsync(ct))
                .Select(s => Guid.TryParse(s, out var g) ? g.ToString("N") : s)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
        }

        var rows = await db.Entities
            .Where(e => e.EntityType == entityType && e.IsActive)
            .Select(e => new { e.Id, e.Name, e.Description, e.UniverseId })
            .ToListAsync(ct);

        if (skipRated && reviewed != null)
            rows = rows.Where(e => !reviewed.Contains(e.Id.ToString())).ToList();

        if (rows.Count == 0) return;

        log.LogInformation("EntityReview(generic): {Count} {Type} entities ({Ballots} ballots each)",
            rows.Count, entityType, ballotCount);

        var useLocal  = !string.IsNullOrWhiteSpace(localUrl);
        var providers = useLocal ? ["local"] : ReviewProviderIds();
        if (providers.Count == 0) { log.LogWarning("No providers configured — skipping {Type}", entityType); return; }
        var concurrency = useLocal ? MaxConcurrencyLocal : MaxConcurrency;

        foreach (var row in rows)
        {
            if (ct.IsCancellationRequested) break;

            var entityId    = row.Id.ToString("N");
            var name        = row.Name ?? "(unnamed)";
            var rawText     = row.Description ?? "";
            var text        = rawText.Length > 3000 ? rawText[..3000] : rawText;
            var contentHash = ComputeContentHash(name, text);

            var personas = SampleEnrichedPersonas(ballotCount);
            var sem      = new SemaphoreSlim(concurrency);
            var bag      = new ConcurrentBag<EntityReview>();
            var failed   = 0;

            await Task.WhenAll(personas.Select((persona, i) => Task.Run(async () =>
            {
                await sem.WaitAsync(ct);
                try
                {
                    var provider = useLocal ? "local" : providers[i % providers.Count];
                    var r = await BallotOnceAsync(entityId, entityType, name, text, contentHash,
                                persona, provider, localUrl, localKey, localModel, ct);
                    if (r != null) bag.Add(r);
                    else Interlocked.Increment(ref failed);
                }
                catch (Exception ex) { Interlocked.Increment(ref failed); log.LogWarning(ex, "Generic ballot failed: {P}", persona.Id); }
                finally { sem.Release(); }
            }, ct)));

            var saved = bag.ToList();
            if (saved.Count == 0) { log.LogWarning("Generic: {Type} '{Name}' — all ballots failed", entityType, name); continue; }

            if (proseCount > 0)
            {
                var picks = SelectInformative(saved, Math.Min(proseCount, saved.Count));
                var psem  = new SemaphoreSlim(concurrency);
                await Task.WhenAll(picks.Select(rv => Task.Run(async () =>
                {
                    await psem.WaitAsync(ct);
                    try
                    {
                        var persona = PersonasByIds([rv.PersonaId]).FirstOrDefault();
                        if (persona == null) return;
                        var provider = useLocal ? "local" : rv.ProviderId;
                        var prose = await ProseOnceAsync(entityType, name, text, persona,
                                        provider, localUrl, localKey, localModel, ct);
                        if (prose != null) { rv.ReviewText = prose.Value.review; rv.Improvements = prose.Value.improvements; }
                    }
                    catch (Exception ex) { log.LogWarning(ex, "Generic prose failed: {P}", rv.PersonaId); }
                    finally { psem.Release(); }
                }, ct)));
            }

            await using var db2 = await dbFactory.CreateDbContextAsync(ct);
            db2.EntityReviews.AddRange(saved);
            await db2.SaveChangesAsync(ct);
            await UpsertSummaryAsync(entityId, entityType, name, contentHash, ct);
            await ExtractAndSaveRelationshipsAsync(row.Id, entityType, name, text, row.UniverseId,
                                                   localUrl, localKey, localModel, ct);

            log.LogInformation("Generic: {Type} '{Name}' → {Mean:F1} ({N} ballots, {F} failed)",
                entityType, name, saved.Average(r => (double)r.Score), saved.Count, failed);
        }
    }

    // ── EntityReviewSummaries upsert ──────────────────────────────────────────

    private async Task UpsertSummaryAsync(string entityId, string entityType, string entityName,
        string contentHash, CancellationToken ct)
    {
        try
        {
            await using var db = await dbFactory.CreateDbContextAsync(ct);
            var allReviews = await db.EntityReviews
                .Where(r => r.EntityId == entityId)
                .ToListAsync(ct);

            if (allReviews.Count == 0) return;

            var avg   = allReviews.Average(r => (double)r.Score);
            var dist  = allReviews.GroupBy(r => r.Score / 10 * 10)
                                  .ToDictionary(g => g.Key, g => g.Count());
            var distJson = System.Text.Json.JsonSerializer.Serialize(dist);

            var existing = await db.EntityReviewSummaries
                .FirstOrDefaultAsync(s => s.EntityId == entityId, ct);

            if (existing == null)
            {
                db.EntityReviewSummaries.Add(new EntityReviewSummary
                {
                    Id              = Guid.CreateVersion7(),
                    EntityId        = entityId,
                    EntityType      = entityType,
                    EntityName      = entityName,
                    ReviewCount     = allReviews.Count,
                    AvgScore        = Math.Round(avg, 2),
                    ScoreDistributionJson = distJson,
                    ContentHash     = contentHash,
                    GeneratedAt     = DateTime.UtcNow,
                });
            }
            else
            {
                existing.ReviewCount          = allReviews.Count;
                existing.AvgScore             = Math.Round(avg, 2);
                existing.ScoreDistributionJson = distJson;
                existing.ContentHash          = contentHash;
                existing.GeneratedAt          = DateTime.UtcNow;
                existing.EntityName           = entityName;
            }
            await db.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "UpsertSummary failed for {EntityId}", entityId);
        }
    }

    // ── Single entity ballot ──────────────────────────────────────────────────

    private async Task<EntityReview?> BallotOnceAsync(
        string entityId, string entityType, string name, string text,
        string contentHash, Persona persona, string provider,
        string? localUrl, string? localKey, string? localModel,
        CancellationToken ct)
    {
        var useLocal = !string.IsNullOrWhiteSpace(localUrl);
        string key, model;
        if (useLocal)
        {
            key   = string.IsNullOrWhiteSpace(localKey)   ? "local" : localKey;
            model = string.IsNullOrWhiteSpace(localModel) ? "qwen2.5-72b-32k" : localModel;
        }
        else
        {
            key = ResolveKey(provider) ?? "";
            if (string.IsNullOrWhiteSpace(key)) return null;
            model = cfg.ModelOverrides.TryGetValue(provider, out var m) && !string.IsNullOrWhiteSpace(m)
                ? m : LegionClient.DefaultModels.GetValueOrDefault(provider, "");
        }

        var system = BuildBallotPrompt(persona, entityType, name);
        var raw = useLocal
            ? await legion.CallAsync("local", key, model, system, text, localUrl!, maxTokens: 400, temperature: 0.85, ct)
            : await legion.CallAsync(provider, key, model, system, text, maxTokens: 400, temperature: 0.85, ct);

        if (!TryParseBallot(raw, out var score, out var weakness, out var contradictions)) return null;

        return new EntityReview
        {
            Id              = Guid.CreateVersion7(),
            EntityId        = entityId,
            EntityType      = entityType,
            EntityName      = name,
            PersonaId       = persona.Id,
            PersonaName     = persona.Name,
            PersonaBlurb    = FirstLine(persona.PersonalityMarkdown),
            ProviderId      = provider,
            Model           = string.IsNullOrWhiteSpace(model) ? null : model,
            Score           = Math.Clamp(score, 1, 100),
            ReviewText      = "",
            Improvements    = string.IsNullOrWhiteSpace(weakness) ? null : weakness.Trim(),
            Contradictions  = contradictions?.Count > 0 ? string.Join("\n", contradictions) : null,
            ContentHash     = contentHash,
            ReviewedAt      = DateTime.UtcNow,
            CreatedAt       = DateTime.UtcNow,
            UpdatedAt       = DateTime.UtcNow,
        };
    }

    private async Task<(string review, string? improvements)?> ProseOnceAsync(
        string entityType, string name, string text,
        Persona persona, string provider,
        string? localUrl, string? localKey, string? localModel,
        CancellationToken ct)
    {
        var useLocal = !string.IsNullOrWhiteSpace(localUrl);
        string key, model;
        if (useLocal)
        {
            key   = string.IsNullOrWhiteSpace(localKey)   ? "local" : localKey;
            model = string.IsNullOrWhiteSpace(localModel) ? "qwen2.5-72b-32k" : localModel;
        }
        else
        {
            key = ResolveKey(provider) ?? "";
            if (string.IsNullOrWhiteSpace(key)) return null;
            model = cfg.ModelOverrides.TryGetValue(provider, out var m) && !string.IsNullOrWhiteSpace(m)
                ? m : LegionClient.DefaultModels.GetValueOrDefault(provider, "");
        }

        var system = BuildReviewPrompt(persona, entityType, name);
        var raw = useLocal
            ? await legion.CallAsync("local", key, model, system, text, localUrl!, maxTokens: 800, temperature: 0.85, ct)
            : await legion.CallAsync(provider, key, model, system, text, maxTokens: 800, temperature: 0.85, ct);

        if (!TryParseReview(raw, out _, out var review, out var improvements)) return null;
        return (review.Trim(), improvements.Count > 0 ? string.Join("\n", improvements) : null);
    }

    // ── Prompt builders ───────────────────────────────────────────────────────

    private string BuildBallotPrompt(Persona persona, string entityType, string name)
    {
        var who = BuildWhoBlock(persona);
        var worldLine = UniverseScope.Current?.UniverseGroundingOr("You are rating a worldbuilding entry from GLMZ (Great Lakes Metropolitan Zone, 2226) — a cyberpunk city ceded to corporate sovereignty.")
            ?? "You are rating a worldbuilding entry from GLMZ (Great Lakes Metropolitan Zone, 2226) — a cyberpunk city ceded to corporate sovereignty.";
        return
$@"{who}

{worldLine}

The entry below describes a {entityType} called ""{name}"".

Read it as the person described above. Rate how compelling, original, and well-crafted this entry is — whether it would stick with you, feel alive, or reveal something true about how this world works. Reserve high scores for entries that genuinely surprise or unsettle you. Mediocre entries are common.

Also flag any internal contradictions or world-canon violations you spot — things that don't add up within the entry itself, or that clash with the 2226 GLMZ setting.

Return ONLY a JSON object, nothing else:
{{""score"": <integer 1-100>, ""weakness"": ""<your single biggest gripe in 8 words or fewer, or 'none'>"", ""contradictions"": [""<contradiction if any, else omit array or leave empty>""]}}";
    }

    private string BuildReviewPrompt(Persona persona, string entityType, string name)
    {
        var who = BuildWhoBlock(persona);
        var worldLine = UniverseScope.Current?.UniverseGroundingOr("You are reviewing a worldbuilding entry from GLMZ (Great Lakes Metropolitan Zone, 2226) — a cyberpunk city ceded to corporate sovereignty.")
            ?? "You are reviewing a worldbuilding entry from GLMZ (Great Lakes Metropolitan Zone, 2226) — a cyberpunk city ceded to corporate sovereignty.";
        return
$@"{who}

{worldLine}

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

" + (UniverseScope.Current?.UniverseGroundingOr(
            "ONE MORE THING: you are a die-hard cyberpunk reader (Neuromancer, Snow Crash, Count Zero, Diamond Age, Hardwired). You know the difference between world-building that feels earned — specific, lived-in, with real texture — and generic setting-filler that performs profundity without containing any. Judge accordingly.")
            ?? "ONE MORE THING: you are a die-hard cyberpunk reader (Neuromancer, Snow Crash, Count Zero, Diamond Age, Hardwired). You know the difference between world-building that feels earned — specific, lived-in, with real texture — and generic setting-filler that performs profundity without containing any. Judge accordingly.");

        return who;
    }

    // ── Parse helpers ─────────────────────────────────────────────────────────

    private static bool TryParseBallot(string? raw, out int score, out string? weakness, out List<string>? contradictions)
    {
        score = 0; weakness = null; contradictions = null;
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
            if (root.TryGetProperty("contradictions", out var carr) && carr.ValueKind == JsonValueKind.Array)
            {
                var list = new List<string>();
                foreach (var item in carr.EnumerateArray())
                { var t = item.GetString(); if (!string.IsNullOrWhiteSpace(t)) list.Add(t.Trim()); }
                if (list.Count > 0) contradictions = list;
            }
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

    // ── Relationship extraction (one call per entity, writes to Edges table) ───

    private async Task ExtractAndSaveRelationshipsAsync(
        Guid entityGuid, string entityType, string entityName, string text, Guid universeId,
        string? localUrl, string? localKey, string? localModel, CancellationToken ct)
    {
        try
        {
            var useLocal = !string.IsNullOrWhiteSpace(localUrl);
            string provider, key, model;
            if (useLocal)
            {
                provider = "local";
                key      = string.IsNullOrWhiteSpace(localKey)   ? "local" : localKey;
                model    = string.IsNullOrWhiteSpace(localModel) ? "qwen2.5-72b-32k" : localModel;
            }
            else
            {
                var providers = ReviewProviderIds();
                if (providers.Count == 0) return;
                provider = providers[0];
                key      = ResolveKey(provider) ?? "";
                if (string.IsNullOrWhiteSpace(key)) return;
                model    = cfg.ModelOverrides.TryGetValue(provider, out var m) ? m
                           : LegionClient.DefaultModels.GetValueOrDefault(provider, "");
            }

            if (string.IsNullOrWhiteSpace(text)) return;

            var system = BuildRelationshipPrompt(entityType, entityName);
            var raw = useLocal
                ? await legion.CallAsync(provider, key, model, system, text, localUrl!, maxTokens: 600, temperature: 0.5, ct)
                : await legion.CallAsync(provider, key, model, system, text, maxTokens: 600, temperature: 0.5, ct);

            var rels = ParseRelationships(raw);
            if (rels.Count == 0) return;

            await using var db = await dbFactory.CreateDbContextAsync(ct);

            var targetNames = rels.Select(r => r.TargetName)
                                  .Where(n => !string.IsNullOrWhiteSpace(n))
                                  .Distinct(StringComparer.OrdinalIgnoreCase)
                                  .ToList();

            var matches = await db.Entities
                .Where(e => e.IsActive && targetNames.Contains(e.Name))
                .Select(e => new { e.Id, e.Name })
                .ToListAsync(ct);

            var nameToId = matches
                .GroupBy(m => m.Name, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First().Id, StringComparer.OrdinalIgnoreCase);

            // Load existing edge pairs to avoid duplicates without per-row round-trips.
            var existingPairs = await db.Edges
                .Where(e => e.SourceId == entityGuid)
                .Select(e => new { e.TargetId, e.RelationType })
                .ToListAsync(ct);
            var existingSet = existingPairs
                .Select(e => $"{e.TargetId}|{e.RelationType}")
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var toAdd = new List<Edge>();
            foreach (var rel in rels)
            {
                if (rel.Confidence < 0.6) continue;
                if (!nameToId.TryGetValue(rel.TargetName, out var targetId)) continue;
                if (targetId == entityGuid) continue;
                var pairKey = $"{targetId}|{rel.RelationType}";
                if (existingSet.Contains(pairKey)) continue;
                existingSet.Add(pairKey);

                toAdd.Add(new Edge
                {
                    SourceId     = entityGuid,
                    TargetId     = targetId,
                    RelationType = rel.RelationType,
                    Description  = rel.Description,
                    Sentiment    = rel.Sentiment,
                    Weight       = rel.Confidence,
                    Source       = "review:entity-scoring",
                    UniverseId   = universeId,
                });
            }

            if (toAdd.Count == 0) return;
            db.Edges.AddRange(toAdd);
            await db.SaveChangesAsync(ct);
            log.LogInformation("Relationships: {Type} '{Name}' → {N} new edges", entityType, entityName, toAdd.Count);
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "Relationship extraction failed for {Type} '{Name}'", entityType, entityName);
        }
    }

    private static string BuildRelationshipPrompt(string entityType, string entityName)
    {
        return
$@"You are a world-graph editor for GLMZ (Great Lakes Metropolitan Zone, 2226), a cyberpunk city. Your task: read the worldbuilding entry below and identify factual, named-entity-to-named-entity relationships.

Rules:
- Only include a relationship when the TARGET is a specific NAMED entity (brand, company, person, place, faction, product). No generic references like ""consumers"" or ""street gangs"".
- relationType must be one of: makes | sold_by | used_by | competes_with | part_of | owned_by | manufactures | employed_by | located_at | mentioned_in | derived_from | contains | replaces | banned_by | endorsed_by | associated_with
- confidence: how certain you are this is a real cross-entity relationship (0.0–1.0). Only include entries with confidence ≥ 0.6.

Return ONLY a JSON object, no prose:
{{""relationships"": [{{""targetName"": ""<exact name>"", ""relationType"": ""<type>"", ""description"": ""<one factual sentence>"", ""sentiment"": ""positive|neutral|negative"", ""confidence"": 0.9}}]}}
If no qualifying relationships exist: {{""relationships"": []}}

Entry type: {entityType}
Entry name: {entityName}";
    }

    private record RelationshipExtract(string TargetName, string RelationType, string Description, string Sentiment, double Confidence);

    private static List<RelationshipExtract> ParseRelationships(string? raw)
    {
        var result = new List<RelationshipExtract>();
        if (string.IsNullOrWhiteSpace(raw)) return result;
        var json = ExtractJson(raw);
        if (json == null) return result;
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("relationships", out var arr) || arr.ValueKind != JsonValueKind.Array)
                return result;
            foreach (var item in arr.EnumerateArray())
            {
                var targetName   = item.TryGetProperty("targetName",   out var n)  ? n.GetString()  ?? "" : "";
                var relationType = item.TryGetProperty("relationType", out var rt) ? rt.GetString() ?? "" : "";
                var description  = item.TryGetProperty("description",  out var d)  ? d.GetString()  ?? "" : "";
                var sentiment    = item.TryGetProperty("sentiment",    out var s)  ? s.GetString()  ?? "neutral" : "neutral";
                var confidence   = item.TryGetProperty("confidence",   out var c)  ? c.GetDouble()  : 0.5;
                if (!string.IsNullOrWhiteSpace(targetName) && !string.IsNullOrWhiteSpace(relationType))
                    result.Add(new RelationshipExtract(targetName, relationType, description, sentiment, confidence));
            }
        }
        catch { }
        return result;
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
