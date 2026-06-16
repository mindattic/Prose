using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using StreetSamurai.Core.Data.Entities;
using StreetSamurai.Core.Models.Canon;
using StreetSamurai.Core;

namespace StreetSamurai.Core.Data;

using ContractEntity = StreetSamurai.Core.Data.Entities.Contract;

/// <summary>
/// Bidirectional mapper between the column/bridge schema (Contract +
/// ContractBonusRow + ContractComplication) and the domain model (ContractData).
/// List fields:
///   .bonuses         → ContractBonusRow      (bridge already exists)
///   .complications   → ContractComplication   (bridge already exists)
///   .required_capabilities → flattened to 10 Capability* scalar columns
///   .tags            → EntityTags (universal layer)
///
/// ADDITIVE CONTRACT: Records.Json is never touched or deleted here.
/// </summary>
public static class ContractMapper
{
    // ─────────────────────────────────────────────────────────────────────
    // READ PATH
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Lightweight list-view projection. Returns Id, Name, Category, Tags only.
    /// </summary>
    public static List<ContractData> LoadAllLite(StreetSamuraiDbContext db)
    {
        var rows = db.Contracts.AsNoTracking()
            .Join(db.Entities.AsNoTracking().Where(e => e.IsActive && e.EntityType == "contract"),
                c => c.Id, e => e.Id,
                (c, e) => new { c.Id, Name = e.Name, c.Category, c.Rating, c.VoteCount })
            .ToList();

        if (rows.Count == 0) return new();

        var ids = rows.Select(r => r.Id).ToHashSet();
        var tagsByEntity = db.EntityTags.AsNoTracking()
            .Where(t => ids.Contains(t.EntityId))
            .Select(t => new { t.EntityId, t.Tag!.Name })
            .ToList()
            .GroupBy(t => t.EntityId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.Name).ToList());

        var result = new List<ContractData>(rows.Count);
        foreach (var r in rows)
        {
            tagsByEntity.TryGetValue(r.Id, out var tags);
            result.Add(new ContractData
            {
                Id        = r.Id.ToString("N"),
                Codename  = r.Name ?? "",
                Category  = r.Category ?? "",
                Rating    = r.Rating,
                VoteCount = r.VoteCount,
                Tags      = tags ?? new List<string>(),
            });
        }
        return result;
    }

    /// <summary>Full load of every active Contract row + all bridge rows, projected to ContractData.</summary>
    public static List<ContractData> LoadAll(StreetSamuraiDbContext db, bool includeArchived = false)
    {
        var ids = (includeArchived
            ? db.Entities.AsNoTracking().Where(e => e.EntityType == "contract")
            : db.Entities.AsNoTracking().Where(e => e.EntityType == "contract" && e.IsActive))
            .Select(e => e.Id)
            .ToHashSet();

        if (ids.Count == 0) return new();

        var contracts = BuildIncludeChain(db.Contracts.AsNoTracking())
            .Where(c => ids.Contains(c.Id))
            .ToList();

        var entityById = db.Entities.AsNoTracking()
            .Where(e => ids.Contains(e.Id))
            .ToDictionary(e => e.Id, e => e);

        var tagsByEntity = db.EntityTags.AsNoTracking()
            .Where(t => ids.Contains(t.EntityId))
            .Select(t => new { t.EntityId, t.Tag!.Name })
            .ToList()
            .GroupBy(t => t.EntityId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.Name).ToList());

        var result = new List<ContractData>(contracts.Count);
        foreach (var c in contracts)
        {
            entityById.TryGetValue(c.Id, out var entity);
            tagsByEntity.TryGetValue(c.Id, out var tags);
            result.Add(Materialize(c, entity, tags));
        }
        return result;
    }

    /// <summary>Load a single Contract by id. Returns null when not found.</summary>
    public static ContractData? LoadOne(StreetSamuraiDbContext db, Guid id)
    {
        var c = BuildIncludeChain(db.Contracts.AsNoTracking()).FirstOrDefault(x => x.Id == id);
        if (c == null) return null;
        var entity = db.Entities.AsNoTracking().FirstOrDefault(e => e.Id == id);
        var tags = db.EntityTags.AsNoTracking()
            .Where(t => t.EntityId == id)
            .Select(t => t.Tag!.Name)
            .ToList();
        return Materialize(c, entity, tags);
    }

    private static IQueryable<ContractEntity> BuildIncludeChain(IQueryable<ContractEntity> q)
        => q.AsSplitQuery()
            .Include(c => c.Bonuses)
            .Include(c => c.Complications);

    /// <summary>Build a ContractData from the entity row + bridges.</summary>
    public static ContractData Materialize(ContractEntity c, Entity? entity, List<string>? tags)
    {
        var data = new ContractData
        {
            Id          = c.Id.ToString("N"),
            Type        = "contract",
            Codename    = entity?.Name ?? c.Name,
            Status      = c.ContractStatus,
            Client      = c.Client,
            ClientTier  = c.ClientTier,
            Category    = c.Category,
            Description = c.Description,
            Objective   = c.Objective,
            Location    = c.Location,
            Target      = c.Target,
            Opposition  = c.Opposition,
            Payout      = c.Payout,
            CrewSize    = c.CrewSize,
            Difficulty  = c.Difficulty,
            TimeLimit   = c.TimeLimit,
            Outcome     = c.Outcome,
            Rating      = c.Rating,
            VoteCount   = c.VoteCount,
            MidjourneyPrompt = c.MidjourneyPrompt,
            Dalle3Prompt = c.Dalle3Prompt,
            Tags        = tags ?? new List<string>(),
            RequiredCapabilities = new CrewCapabilities
            {
                Combat       = c.CapabilityCombat,
                Stealth      = c.CapabilityStealth,
                Hacking      = c.CapabilityHacking,
                Social       = c.CapabilitySocial,
                Medical      = c.CapabilityMedical,
                Tech         = c.CapabilityTech,
                Transport    = c.CapabilityTransport,
                Demolitions  = c.CapabilityDemolitions,
                Surveillance = c.CapabilitySurveillance,
                Linguistics  = c.CapabilityLinguistics,
            },
        };

        data.Bonuses = c.Bonuses.OrderBy(x => x.Position).Select(b => new ContractBonus
        {
            Type      = b.BonusType,
            Amount    = b.Amount,
            Condition = b.Condition,
        }).ToList();

        data.Complications = c.Complications
            .OrderBy(x => x.Position)
            .Select(x => x.Description)
            .ToList();

        return data;
    }

    // ─────────────────────────────────────────────────────────────────────
    // WRITE PATH
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Upsert a ContractData into the relational schema. Bridge rows are wiped and
    /// re-inserted on every save. Caller must call db.SaveChanges() after this.
    /// Records.Json is NOT touched — additive-only contract.
    /// </summary>
    public static async Task PersistAsync(StreetSamuraiDbContext db, Guid id, ContractData src, CancellationToken ct = default)
    {
        var contract = await db.Contracts.FirstOrDefaultAsync(c => c.Id == id, ct);
        var isNew = contract == null;

        if (!isNew)
        {
            await db.ContractBonuses.Where(x => x.ContractId == id).ExecuteDeleteAsync(ct);
            await db.ContractComplications.Where(x => x.ContractId == id).ExecuteDeleteAsync(ct);
        }
        else
        {
            contract = new ContractEntity { Id = id };
            db.Contracts.Add(contract);
        }

        FillScalars(contract!, src);
        FillBridges(db, id, src);
    }

    /// <summary>Populate scalar columns on Contract from src (no DB touch).</summary>
    public static void FillScalars(ContractEntity c, ContractData src)
    {
        var caps = src.RequiredCapabilities ?? new CrewCapabilities();

        c.Name                 = src.Codename ?? "";
        c.Codename             = src.Codename ?? "";
        c.ContractStatus       = string.IsNullOrEmpty(src.Status) ? "open" : src.Status;
        c.Client               = src.Client ?? "";
        c.ClientTier           = src.ClientTier ?? "";
        c.Category             = src.Category ?? "";
        c.Description          = src.Description ?? "";
        c.Objective            = src.Objective ?? "";
        c.Location             = src.Location ?? "";
        c.Target               = src.Target ?? "";
        c.Opposition           = src.Opposition ?? "";
        c.Payout               = src.Payout ?? "";
        c.CrewSize             = src.CrewSize ?? "";
        c.Difficulty           = src.Difficulty ?? "";
        c.TimeLimit            = src.TimeLimit ?? "";
        c.Outcome              = src.Outcome ?? "";
        c.Rating               = src.Rating;
        c.VoteCount            = src.VoteCount;
        c.MidjourneyPrompt     = src.MidjourneyPrompt ?? "";
        c.Dalle3Prompt         = src.Dalle3Prompt ?? "";
        c.CapabilityCombat     = caps.Combat;
        c.CapabilityStealth    = caps.Stealth;
        c.CapabilityHacking    = caps.Hacking;
        c.CapabilitySocial     = caps.Social;
        c.CapabilityMedical    = caps.Medical;
        c.CapabilityTech       = caps.Tech;
        c.CapabilityTransport  = caps.Transport;
        c.CapabilityDemolitions = caps.Demolitions;
        c.CapabilitySurveillance = caps.Surveillance;
        c.CapabilityLinguistics  = caps.Linguistics;

        // Resolve client and location FKs when possible (best-effort, no-op if not found).
        // These are set via the DB context at backfill time; skip for speed on direct FillScalars calls.
    }

    /// <summary>Insert all bridge rows (assumes parent bridges have already been wiped).</summary>
    public static void FillBridges(StreetSamuraiDbContext db, Guid id, ContractData src)
    {
        for (int i = 0; i < src.Bonuses.Count; i++)
        {
            var b = src.Bonuses[i];
            db.ContractBonuses.Add(new ContractBonusRow
            {
                ContractId = id,
                Position   = i,
                BonusType  = b.Type ?? "",
                Amount     = b.Amount ?? "",
                Condition  = b.Condition ?? "",
            });
        }

        for (int i = 0; i < src.Complications.Count; i++)
        {
            db.ContractComplications.Add(new ContractComplication
            {
                ContractId  = id,
                Position    = i,
                Description = src.Complications[i] ?? "",
            });
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    // BACKFILL
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Backfill: for every active contract Entity, deserialize its Records.Json
    /// blob → ContractData → persist. Returns the number of contracts written.
    ///
    /// ADDITIVE — Records.Json is never modified or deleted.
    /// Run once via <c>ss --rebuild-contract-relational</c>.
    /// </summary>
    public static async Task<int> RebuildAllAsync(StreetSamuraiDbContext db, CancellationToken ct = default)
    {
        var opts = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
        };

        var contractEntityIds = db.Entities.AsNoTracking()
            .Where(e => e.EntityType == "contract" && e.IsActive)
            .Select(e => e.Id)
            .ToHashSet();

        if (contractEntityIds.Count == 0) return 0;

        var blobRows = db.Records.AsNoTracking()
            .Where(r => contractEntityIds.Contains(r.EntityId))
            .Select(r => new { r.EntityId, r.Json })
            .ToList();

        int written = 0;
        foreach (var row in blobRows)
        {
            ContractData? src;
            try { src = JsonSerializer.Deserialize<ContractData>(row.Json, opts); }
            catch (Exception ex)
            {
                Serilog.Log.Warning(ex, "ContractMapper.RebuildAllAsync: failed to deserialize blob for entity {Id}", row.EntityId);
                continue;
            }
            if (src == null) continue;

            try
            {
                await PersistAsync(db, row.EntityId, src, ct);
                FactionMapper.SyncTagsForEntity(db, row.EntityId, src.Tags);
                await db.SaveChangesAsync(ct);
                written++;
            }
            catch (Exception ex)
            {
                Serilog.Log.Warning(ex, "ContractMapper.RebuildAllAsync: failed to persist contract {Id}", row.EntityId);
                db.ChangeTracker.Clear();
            }
        }
        return written;
    }
}
