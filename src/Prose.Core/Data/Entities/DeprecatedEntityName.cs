namespace Prose.Core.Data.Entities;

/// <summary>
/// Registry of deprecated / renamed noun references that must NOT appear in prose.
/// When a named thing (character handle, drone name, job title, place name, etc.)
/// is renamed or retired, the old name is registered here so
/// <see cref="Prose.Core.Services.NounConsistencyService"/> can flag any
/// beats that still use it.
///
/// EntityId is optional: it points to the canonical <see cref="Entity"/> row when
/// one exists. CanonicalName is always required and is the human-readable
/// replacement shown in violation reports.
///
/// Rules are universe-scoped so a GLMZ rename does not pollute Fantasy scans.
/// </summary>
public class DeprecatedEntityName
{
    public long Id { get; set; }

    /// <summary>Universe this rule applies to (SS-LAW-15).</summary>
    public Guid UniverseId { get; set; }

    /// <summary>The old/wrong name to flag in prose (e.g. "VacCell", "Rider", "QCE").</summary>
    public string DeprecatedName { get; set; } = "";

    /// <summary>The correct name to use instead (e.g. "Nit", "Exo").</summary>
    public string CanonicalName { get; set; } = "";

    /// <summary>Optional FK to the canonical Entity row when seeded in the Entities table.</summary>
    public Guid? EntityId { get; set; }

    /// <summary>Human note explaining the rename (e.g. "Renamed SS-A38 when Rider job was retired").</summary>
    public string? Notes { get; set; }

    public DateTime AddedAt { get; set; } = DateTime.UtcNow;

    public Entity? Entity { get; set; }
}
