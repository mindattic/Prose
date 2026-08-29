using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;
using Prose.Core.Data.Entities;

namespace Prose.Core.Services;

/// <summary>
/// CRUD for the Prose-wide hard-banned name registry (2026-08-26). The actual enforcement lives
/// in <see cref="Services.WriteGate.BannedNameSyncCheck"/> — this service is just the sanctioned
/// read/write surface for CLI (<c>prose --banned-names</c>) and MCP
/// (<c>add_banned_name</c>/<c>list_banned_names</c>/<c>remove_banned_name</c>).
/// </summary>
public class BannedNameService(IDbContextFactory<ProseDbContext> dbFactory)
{
    public async Task<BannedName> AddAsync(string name, string? notes = null, CancellationToken ct = default)
    {
        var trimmed = (name ?? "").Trim();
        if (trimmed.Length == 0)
            throw new ArgumentException("Banned name cannot be empty or whitespace — an empty entry " +
                "would match \\b\\b against every non-empty value and reject all future writes.", nameof(name));

        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var existing = await db.BannedNames.FirstOrDefaultAsync(
            b => b.Name.ToLower() == trimmed.ToLower(), ct);
        if (existing != null) return existing;

        var row = new BannedName { Name = trimmed, Notes = notes?.Trim(), AddedAt = DateTime.UtcNow };
        db.BannedNames.Add(row);
        await db.SaveChangesAsync(ct);
        return row;
    }

    public async Task<IReadOnlyList<BannedName>> ListAsync(CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        return await db.BannedNames.AsNoTracking().OrderBy(b => b.Name).ToListAsync(ct);
    }

    public async Task<bool> RemoveAsync(long id, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var row = await db.BannedNames.FindAsync([id], ct);
        if (row == null) return false;
        db.BannedNames.Remove(row);
        await db.SaveChangesAsync(ct);
        return true;
    }
}
