using Microsoft.Extensions.Logging;
using MindAttic.Legion;
using StreetSamurai.Core.Interfaces;
using StreetSamurai.Core.Models.Canon;

namespace StreetSamurai.Core.Services;

/// <summary>
/// Crowd-sources entity interest ratings via LLMVoting.
///
/// Each voting run adds 10 personas per entity — scores ACCUMULATE across runs rather than
/// replacing. Over many runs the total vote pool grows toward thousands of personas and the
/// distribution naturally converges: genuinely interesting entries float up, mediocre ones
/// settle in the middle, dull ones sink.
///
/// VoteCount tracks the total ballots cast. Rating is the running weighted average (0–100).
///
/// Run RateAllAsync() to add another round of votes to every entity.
/// Run RateUnratedAsync() to only score entities with no votes yet.
/// GetAllRated() returns the cross-repo leaderboard sorted by Rating descending.
/// </summary>
public class EntityRatingService
{
    private readonly LlmVotingService llmVoting;
    private readonly ILogger<EntityRatingService> log;

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

    private const string Question = "How genuinely interesting is this world-building entry? Would a resident of GLMZ care about this?";
    private const int VotersPerEntity = 10;

    // Pool of GLMZ resident personas — randomly sampled to form a crowd of 10 per vote
    private static readonly string[] PersonaPool =
    [
        "You are a burned-out ripperdoc who works out of a shipping container in the Shelf. You've rebuilt bodies and buried the failures. You value survival-grade knowledge and have zero patience for things that won't matter when the bullets start flying.",
        "You are a Tier 2 corpo analyst at a mid-size subsidiary. You live in spreadsheets and threat models. You find things interesting if they shift power, open markets, or represent unusual risk.",
        "You are a freelance fixer who specializes in information brokerage. Your currency is secrets and leverage. You rate things by their usefulness in negotiation — information that nobody else knows is gold.",
        "You are a former Arcturus Civil Security officer who went private. You think in terms of threat profiles, crowd dynamics, and containment. You respect things that reveal how power actually moves on the street.",
        "You are a Shelf scrap-runner, eighteen years old, half your friends are dead or disappeared. You find things interesting that explain why the city works against people like you — or how to survive anyway.",
        "You are a black-market pharmacist who synthesizes unlicensed compounds in a basement lab. Your interests are biochemistry, side effects, and what happens when something goes wrong at scale.",
        "You are a jazz musician who plays the underground clubs in the Gutter. You read people, moods, and subtext better than most. You're drawn to things with texture — things that feel true even when they're dark.",
        "You are a senior GLMZ logistics coordinator for a Tier 3 transport company. You know every chokepoint, bribe rate, and inspection schedule in the city. You find things interesting if they change how goods — legal or otherwise — move.",
        "You are a veteran Shelf gang lieutenant whose territory keeps shrinking. You think in terms of loyalty, betrayal, and territorial math. You care about things with real weight in the politics of survival.",
        "You are a synthetic-life welfare researcher embedded in GLMZ by an overseas NGO. You document conditions, rights violations, and institutional denial. You find things interesting that reveal systemic injustice or unusual resistance.",
        "You are a street-level augmentation clinic nurse. You've seen what happens when cheap cyberware rejects. You care about what things cost people — in money, in flesh, in dignity.",
        "You are a retired corponation negotiator who has seen every trick in the contract playbook. You read situations for leverage, obligation, and what people are willing to sacrifice to win.",
        "You are a GLMZ underground journalist running an encrypted feed. You care about things that expose hidden systems — the real owners, the real rules, the real price paid by people who don't make decisions.",
        "You are a wasteland scout who runs supply chains to the outer settlements beyond the flood line. You have a practical, stripped-down worldview. Things are interesting if they matter where the infrastructure ends.",
        "You are a neural interface calibration technician. You spend your days inside other people's perceptual systems. You find things interesting that blur or challenge the line between augmented and authentic experience.",
        "You are a high-end synthetic concierge in a Tier 4 residential tower. You observe the powerful at close range, serving them invisibly. You notice what they want people not to notice.",
        "You are a mid-level corponation security analyst specializing in corporate espionage patterns. You read any new piece of information for how it could be weaponized, leaked, or denied.",
        "You are a street medic for one of GLMZ's unrecognized informal settlements. Resources are impossible. You make decisions about who gets treatment and who doesn't. You value things that help the abandoned.",
        "You are a data courier who physically transports air-gapped drives across contested territory. You've been shot at for things you didn't understand. You find things interesting when the stakes make physical sense.",
        "You are a GLMZ municipal water reclamation engineer. You maintain the systems everyone depends on and nobody credits. You see the city as infrastructure — what's load-bearing, what's decorative, what fails first.",
        "You are a low-level enforcement arm for a debt-collection syndicate. You've heard every story people tell to avoid paying. You find things interesting when they reveal what people actually fear losing.",
        "You are a Tier 1 community organizer running mutual-aid networks in the deepest Shelf districts. You've watched every charity turn extractive. You trust things that last without funding and distrust things that require it.",
        "You are a retired black-ops operative living anonymously in GLMZ. You know what information gets people killed. You find things interesting when they're dangerous to know — and you can tell the difference.",
        "You are a luxury goods counterfeiter whose work is indistinguishable from the real thing. You know everything about how status is manufactured, sold, and believed. You find the machinery of desire deeply interesting.",
        "You are a psionic researcher working off the books in a basement lab. Your work is illegal in six jurisdictions. You think about cognition, perception, and what it means when the mind itself becomes contested territory.",
    ];

    private static readonly Random Rng = Random.Shared;

    public EntityRatingService(
        LlmVotingService llmVoting,
        ILogger<EntityRatingService> log,
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
        this.llmVoting      = llmVoting;
        this.log            = log;
        this.characters     = characters;
        this.technology     = technology;
        this.weaponry       = weaponry;
        this.ammunition     = ammunition;
        this.equipment      = equipment;
        this.cyberware      = cyberware;
        this.genemods       = genemods;
        this.transportation = transportation;
        this.automata       = automata;
        this.subsidiaries   = subsidiaries;
        this.entertainment  = entertainment;
        this.apparel        = apparel;
        this.materials      = materials;
        this.pharmaceuticals = pharmaceuticals;
        this.consumerGoods  = consumerGoods;
        this.factions       = factions;
        this.districts      = districts;
        this.contracts      = contracts;
        this.labSpecimens   = labSpecimens;
        this.psionics       = psionics;
    }

    /// <summary>
    /// Add another round of votes to every entity. Scores accumulate — run repeatedly
    /// to grow the total vote pool and converge toward a natural interest distribution.
    /// </summary>
    public async Task RateAllAsync(CancellationToken ct = default)
    {
        log.LogInformation("EntityRatingService: RateAllAsync starting");
        await RunBatches(skipRated: false, ct);
        log.LogInformation("EntityRatingService: RateAllAsync complete");
    }

    /// <summary>
    /// Rate only entities where Rating == 0. Useful for incremental updates.
    /// </summary>
    public async Task RateUnratedAsync(CancellationToken ct = default)
    {
        log.LogInformation("EntityRatingService: RateUnratedAsync starting");
        await RunBatches(skipRated: true, ct);
        log.LogInformation("EntityRatingService: RateUnratedAsync complete");
    }

    /// <summary>
    /// Returns all rated entities (Rating > 0) across all repos, sorted by Rating descending.
    /// </summary>
    public IEnumerable<(string Name, string Type, string Route, string Id, double Rating, int VoteCount)> GetAllRated()
    {
        var results = new List<(string Name, string Type, string Route, string Id, double Rating, int VoteCount)>();

        void Collect<T>(List<T> entities, Func<T, string> name, string type, string route) where T : ICanonEntity
        {
            foreach (var e in entities.Where(e => e.Rating > 0))
                results.Add((name(e), type, route, e.Id, e.Rating, e.VoteCount));
        }

        Collect(characters.GetAll(),     e => e.Name,     "character",     "/characters");
        Collect(technology.GetAll(),     e => e.Name,     "technology",    "/technology");
        Collect(weaponry.GetAll(),       e => e.Name,     "weapon",        "/weaponry");
        Collect(ammunition.GetAll(),     e => e.Name,     "ammunition",    "/ammunition");
        Collect(equipment.GetAll(),      e => e.Name,     "equipment",     "/equipment");
        Collect(cyberware.GetAll(),      e => e.Name,     "cyberware",     "/cyberware");
        Collect(genemods.GetAll(),       e => e.Name,     "genemod",       "/genemods");
        Collect(transportation.GetAll(), e => e.Name,     "transportation","/transportation");
        Collect(automata.GetAll(),       e => e.Name,     "automaton",     "/automata");
        Collect(subsidiaries.GetAll(),   e => e.Name,     "subsidiary",    "/subsidiaries");
        Collect(entertainment.GetAll(),  e => e.Name,     "entertainment", "/entertainment");
        Collect(apparel.GetAll(),        e => e.Name,     "apparel",       "/apparel");
        Collect(materials.GetAll(),      e => e.Name,     "material",      "/materials");
        Collect(pharmaceuticals.GetAll(),e => e.Name,     "pharmaceutical","/pharmaceuticals");
        Collect(consumerGoods.GetAll(),  e => e.Name,     "consumer-good", "/goods");
        Collect(factions.GetAll(),       e => e.Name,     "faction",       "/factions");
        Collect(districts.GetAll(),      e => e.Name,     "district",      "/places");
        Collect(contracts.GetAll(),      e => e.Codename, "contract",      "/contracts");
        Collect(labSpecimens.GetAll(),   e => e.Name,     "lab-specimen",  "/lab-specimens");
        Collect(psionics.GetAll(),       e => e.Name,     "psionic",       "/psionics");

        return results.OrderByDescending(r => r.Rating);
    }

    // ── Private ───────────────────────────────────────────────────────────────

    private async Task RunBatches(bool skipRated, CancellationToken ct)
    {
        await RateBatch(characters.GetAll(),     e => (e.Name, e.Description),        e => characters.Save(e),     "character",     skipRated, ct);
        await RateBatch(technology.GetAll(),     e => (e.Name, e.Description),        e => technology.Save(e),     "technology",    skipRated, ct);
        await RateBatch(weaponry.GetAll(),       e => (e.Name, e.Description),        e => weaponry.Save(e),       "weaponry",      skipRated, ct);
        await RateBatch(ammunition.GetAll(),     e => (e.Name, e.Description),        e => ammunition.Save(e),     "ammunition",    skipRated, ct);
        await RateBatch(equipment.GetAll(),      e => (e.Name, e.Description),        e => equipment.Save(e),      "equipment",     skipRated, ct);
        await RateBatch(cyberware.GetAll(),      e => (e.Name, e.Description),        e => cyberware.Save(e),      "cyberware",     skipRated, ct);
        await RateBatch(genemods.GetAll(),       e => (e.Name, e.Description),        e => genemods.Save(e),       "genemod",       skipRated, ct);
        await RateBatch(transportation.GetAll(), e => (e.Name, e.Description),        e => transportation.Save(e), "transportation",skipRated, ct);
        await RateBatch(automata.GetAll(),       e => (e.Name, e.Description),        e => automata.Save(e),       "automaton",     skipRated, ct);
        await RateBatch(subsidiaries.GetAll(),   e => (e.Name, e.Description),        e => subsidiaries.Save(e),   "subsidiary",    skipRated, ct);
        await RateBatch(entertainment.GetAll(),  e => (e.Name, e.Description),        e => entertainment.Save(e),  "entertainment", skipRated, ct);
        await RateBatch(apparel.GetAll(),        e => (e.Name, e.Description),        e => apparel.Save(e),        "apparel",       skipRated, ct);
        await RateBatch(materials.GetAll(),      e => (e.Name, e.Description),        e => materials.Save(e),      "material",      skipRated, ct);
        await RateBatch(pharmaceuticals.GetAll(),e => (e.Name, e.Description),        e => pharmaceuticals.Save(e),"pharmaceutical",skipRated, ct);
        await RateBatch(consumerGoods.GetAll(),  e => (e.Name, e.Description),        e => consumerGoods.Save(e),  "consumer-good", skipRated, ct);
        await RateBatch(factions.GetAll(),       e => (e.Name, e.Description),        e => factions.Save(e),       "faction",       skipRated, ct);
        await RateBatch(districts.GetAll(),      e => (e.Name, e.Description),        e => districts.Save(e),      "district",      skipRated, ct);
        await RateBatch(contracts.GetAll(),      e => (e.Codename, e.Description),    e => contracts.Save(e),      "contract",      skipRated, ct);
        await RateBatch(labSpecimens.GetAll(),   e => (e.Name, e.PhysicalDescription),e => labSpecimens.Save(e),   "lab-specimen",  skipRated, ct);
        await RateBatch(psionics.GetAll(),       e => (e.Name, e.Mechanism),          e => psionics.Save(e),       "psionic",       skipRated, ct);
    }

    private async Task RateBatch<T>(
        List<T> entities,
        Func<T, (string name, string text)> getContext,
        Action<T> save,
        string typeName,
        bool skipRated,
        CancellationToken ct) where T : ICanonEntity
    {
        var targets = skipRated ? entities.Where(e => e.Rating == 0).ToList() : entities;
        if (targets.Count == 0) return;

        log.LogInformation("EntityRating: scoring {Count} {Type} entities with {Voters} personas each",
            targets.Count, typeName, VotersPerEntity);

        foreach (var entity in targets)
        {
            if (ct.IsCancellationRequested) break;

            var (name, text) = getContext(entity);
            var context = string.IsNullOrWhiteSpace(text)
                ? name
                : $"{name}\n\n{text}"[..Math.Min(2500, name.Length + 2 + text.Length)];

            var request = new ScoredVoteRequest
            {
                Question         = Question,
                Context          = context,
                Dimensions       = ["INTEREST"],
                MaxTokens        = 256,
                SynthesizeNarrative = false,
                EvaluatorContext = """
                    You are a resident of GLMZ, a near-future city in the United States that has been
                    ceded to corporate sovereignty. Rate how genuinely interesting this world-building
                    entry is — not just factually, but as something that matters, surprises, or reveals
                    something true about how this city works. Score INTEREST 1–10 from your character's
                    perspective. Be honest. Mediocre entries are common; reserve high scores for entries
                    that feel truly alive or reveal something unexpected.
                    """,
            };

            var personas = SamplePersonas(VotersPerEntity);

            try
            {
                var result = await llmVoting.ScoreWithProfilesAsync(request, personas, ct);

                // Average only non-error, non-zero INTEREST scores (zeros = timeouts/errors)
                var validScores = result.IndividualVotes
                    .Where(v => !v.IsError && v.Scores.TryGetValue("INTEREST", out var s) && s > 0)
                    .Select(v => (double)v.Scores["INTEREST"])
                    .ToList();

                if (validScores.Count == 0)
                {
                    log.LogWarning("EntityRating: {Type} '{Name}' — all {Total} votes were null/zero, skipping",
                        typeName, name, VotersPerEntity);
                    continue;
                }

                // Accumulate: blend new votes into the running weighted average
                // oldSum is recovered from the stored rating (Rating = avg*10, avg = sum/count)
                var oldCount = entity.VoteCount;
                var oldSum   = oldCount > 0 ? (entity.Rating / 10.0) * oldCount : 0.0;
                var newCount = oldCount + validScores.Count;
                var newAvg   = (oldSum + validScores.Sum()) / newCount;

                entity.Rating    = Math.Round(newAvg * 10.0, 1);
                entity.VoteCount = newCount;
                save(entity);

                log.LogInformation(
                    "EntityRating: {Type} '{Name}' → {Rating} ({NewVotes} new votes, {TotalVotes} total, avg={Avg:F2}/10)",
                    typeName, name, entity.Rating, validScores.Count, newCount, newAvg);
            }
            catch (Exception ex)
            {
                log.LogWarning(ex, "EntityRating: failed to score {Type} '{Name}'", typeName, name);
            }
        }
    }

    private List<VoterProfile> SamplePersonas(int count)
    {
        var providerIds = llmVoting.GetActiveProviderIds();
        if (providerIds.Count == 0) providerIds = ["claude"];

        // Shuffle the persona pool, pick `count` unique personas (cycling if pool is smaller)
        var shuffled = PersonaPool.OrderBy(_ => Rng.NextDouble()).ToList();
        var personas = new List<VoterProfile>(count);

        for (int i = 0; i < count; i++)
        {
            var persona   = shuffled[i % shuffled.Count];
            var providerId = providerIds[i % providerIds.Count];

            personas.Add(new VoterProfile
            {
                Name                = $"Voter-{i + 1}",
                ProviderId          = providerId,
                PersonalityMarkdown = persona,
                MaxTokensOverride   = 256,
            });
        }
        return personas;
    }
}
