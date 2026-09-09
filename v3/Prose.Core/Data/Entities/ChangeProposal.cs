namespace Prose.Core.Data.Entities;

/// <summary>
/// A durable, reviewable request to change canon, prose, entities, relationships, or structure.
/// Creating a proposal never mutates its target; a separate human approval grant is required
/// before any future apply operation may act on it.
/// </summary>
public class ChangeProposal
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid UniverseId { get; set; }
    public string Target { get; set; } = "";
    public string OldValue { get; set; } = "";
    public string NewValue { get; set; } = "";
    public string Rationale { get; set; } = "";
    public string VerificationPlan { get; set; } = "";
    public string Status { get; set; } = "proposed";
    public string RequestId { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
