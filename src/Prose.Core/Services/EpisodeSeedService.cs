using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Prose.Core.Data;

namespace Prose.Core.Services;

/// <summary>
/// Manufactures connective-membrane episode seeds. Each seed is a one-line
/// situation that names 2-3 canon entities and pulls a story hook from one of
/// them. The DB is the substrate — this service is the connective tissue.
///
/// The seeds are TEMPLATES with placeholder slots ({character}, {place},
/// {faction}, {hook}). At call time we draw random canon entities to fill the
/// slots. Same template + different draw = new story.
/// </summary>
public class EpisodeSeedService
{
    private readonly IDbContextFactory<ProseDbContext> dbFactory;
    private readonly ILogger<EpisodeSeedService> log;
    private readonly Random rng = new();

    public EpisodeSeedService(
        IDbContextFactory<ProseDbContext> dbFactory,
        ILogger<EpisodeSeedService> log)
    {
        this.dbFactory = dbFactory;
        this.log = log;
    }

    /// <summary>Templates. The slot-keys are case-sensitive. {character} and {place}
    /// pull from the canon DB; {hook} pulls from CharacterStoryHooks /
    /// PlaceStoryHooks on the entity already chosen.</summary>
    private static readonly string[] Templates =
    {
        "{character} walks into Mrs. Chen's looking for Kyle. They have a problem only Kyle can solve.",
        "A child rides up to Mrs. Chen's stall with a contract written on a Carrion receipt. The contract names {character}.",
        "Sable routes Kyle a courier job through {place}. The cargo is not what the manifest says.",
        "Kyle owes {character} a favor he does not remember owing.",
        "Hua calls in one Φ of her Φ85,000 debt. The favor she wants concerns {character}.",
        "A Lotus lieutenant Sable has not vetted brings Kyle a sealed envelope. Inside: an address in {place}.",
        "Pixel finds something in Kyle's hardware diagnostic that has {character}'s signature on it.",
        "{character} sends Kyle a message through Otto's parts shop. The message is one word.",
        "Mrs. Chen mentions a name she should not know: {character}.",
        "Kyle takes a job at {place}. {character} is already there, waiting.",
        "A face from Kyle's past — {character} — turns up at the noodle stall asking for the night's special.",
        "Carrion Logistics LLC posts a body-pickup notice that names Kyle. The body is alive. It belongs to {character}.",
        "{character} is being hunted through {place}. Sable thinks Kyle should care. Kyle is not sure he does.",
        "A storm closes the Pulse for the night. Three people Kyle does not want to meet are stuck in {place}.",
        "Kyle finds a folded piece of paper in his coat pocket. He did not put it there. It is from {character}.",
        "An old contract from before Bushido Coda surfaces with {character}'s name on the routing tag. It was never closed.",
        "Pixel asks Kyle for a favor for the first time in four years. The favor involves {character}.",
        "Kyle is finishing a bowl when {character} sits down on the stool next to him and says nothing for a long time.",
        "Someone is impersonating Kyle in {place}. The impersonator is doing a good enough job that {character} is convinced.",
        "{character} dies in the first paragraph. The rest of the night is Kyle figuring out who did it and what he is going to do about it.",
    };

    /// <summary>Draw a seed for an episode. Returns the realized one-line situation
    /// plus the entity ids it referenced, so the generator can pull richer context
    /// downstream.</summary>
    public async Task<DrawnSeed> DrawAsync(CancellationToken ct = default)
    {
        var template = Templates[rng.Next(Templates.Length)];

        await using var db = await dbFactory.CreateDbContextAsync(ct);

        // Pick a non-Kyle character at random for the {character} slot.
        var characterId = await PickRandomEntityIdAsync(db, "character",
            excludeName: "Kyle Ellen Corbin", ct);

        var placeId = template.Contains("{place}")
            ? await PickRandomEntityIdAsync(db, "place", excludeName: null, ct)
            : (Guid?)null;

        var charName  = characterId is { } cId ? await GetNameAsync(db, cId, ct)  : "Sable";
        var placeName = placeId    is { } pId ? await GetNameAsync(db, pId, ct) : "the Gray Zone";

        var realized = template
            .Replace("{character}", charName)
            .Replace("{place}", placeName);

        log.LogInformation("Drew episode seed: {Seed}", realized);

        return new DrawnSeed(
            Realized: realized,
            Template: template,
            CharacterId: characterId,
            PlaceId: placeId);
    }

    private async Task<Guid?> PickRandomEntityIdAsync(
        ProseDbContext db, string entityType, string? excludeName, CancellationToken ct)
    {
        var query = db.Entities
            .AsNoTracking()
            .Where(e => e.EntityType == entityType);
        if (!string.IsNullOrEmpty(excludeName))
            query = query.Where(e => e.Name != excludeName);

        var count = await query.CountAsync(ct);
        if (count == 0) return null;

        var skip = rng.Next(count);
        var picked = await query.OrderBy(e => e.Id).Skip(skip).Take(1).Select(e => e.Id).FirstOrDefaultAsync(ct);
        return picked == Guid.Empty ? null : picked;
    }

    private static async Task<string> GetNameAsync(ProseDbContext db, Guid id, CancellationToken ct)
    {
        var name = await db.Entities.AsNoTracking().Where(e => e.Id == id).Select(e => e.Name).FirstOrDefaultAsync(ct);
        return string.IsNullOrWhiteSpace(name) ? "someone" : name!;
    }
}

/// <summary>The output of seed selection — realized text plus the entity ids that
/// got pulled, so the generator can fetch richer context (story hooks,
/// descriptions, edges) for the prompt.</summary>
public record DrawnSeed(
    string Realized,
    string Template,
    Guid? CharacterId,
    Guid? PlaceId);
