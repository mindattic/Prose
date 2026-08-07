using System.ComponentModel.DataAnnotations;

namespace Prose.Core.Data.Entities;

/// <summary>
/// User-managed context override: pins a specific markdown doc into the
/// DocContextStack regardless of LRU tier, or excludes one that would
/// normally be injected.
///
/// Rows are keyed by SessionKey (Environment.UserName by default) and expire
/// after 24 hours, or can be cleared explicitly via <c>ss --context clear</c>.
/// Managed by <see cref="Prose.Core.Services.UserContextService"/>.
/// </summary>
public class ContextOverride
{
    public int      Id             { get; set; }
    [MaxLength(256)]
    public string   SessionKey     { get; set; } = "";
    public Guid?    NodeId         { get; set; }   // null = applies to all nodes in this session
    public string   Action         { get; set; } = "";  // "pin" | "exclude"
    public Guid     MarkdownFileId { get; set; }
    public DateTime CreatedAt      { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt      { get; set; } = DateTime.UtcNow.AddHours(24);
}
