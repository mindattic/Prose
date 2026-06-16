namespace StreetSamurai.Core.Data.Entities;

/// <summary>
/// A runtime-defined entity type (repository). Instead of a typed SQL table,
/// instances store data in the generic <see cref="Entity"/> spine using
/// <see cref="Slug"/> as the <c>EntityType</c> discriminator.
///
/// Global (not universe-scoped): the definition is shared across all universes.
/// Universe filtering happens at the <see cref="Entity"/> level via <c>UniverseId</c>.
/// An empty repo in a given universe is simply not displayed on the board —
/// the UI only renders repos that have at least one active entity in the
/// current universe.
///
/// Seeded by <c>add_repository_definitions_20260616.sql</c>.
/// </summary>
public class RepositoryDefinition
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    /// <summary>
    /// URL/CLI-safe EntityType discriminator (lowercase, hyphens), e.g. <c>artifact</c>.
    /// Unique. Used as the <c>EntityType</c> string on every <see cref="Entity"/>
    /// that belongs to this repository.
    /// </summary>
    public string Slug { get; set; } = "";

    /// <summary>Human-readable display name, e.g. "Artifacts".</summary>
    public string Name { get; set; } = "";

    /// <summary>
    /// Board category this repo is grouped under on the home grid.
    /// One of: Characters, Organizations, Gear, World, Culture.
    /// </summary>
    public string Category { get; set; } = "World";

    /// <summary>Bootstrap-icon class for the board tile, e.g. "bi-box".</summary>
    public string Icon { get; set; } = "bi-box";

    public string? Description { get; set; }

    /// <summary>Blazor route path, e.g. "/repo/artifact".</summary>
    public string RoutePath { get; set; } = "";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
