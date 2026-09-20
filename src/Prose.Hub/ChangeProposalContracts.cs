namespace Prose.Hub;

/// <summary>Transport-neutral envelope for the proposal-only operation.</summary>
public sealed record ChangeProposalRequest(
    string ProtocolVersion,
    string Operation,
    string Universe,
    ChangeProposalArguments Arguments,
    string RequestId,
    string? ApprovalGrant = null);

public sealed record ChangeProposalArguments(
    string Target,
    string OldValue,
    string NewValue,
    string Rationale,
    string VerificationPlan);
