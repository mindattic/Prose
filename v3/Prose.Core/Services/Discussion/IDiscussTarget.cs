namespace Prose.Core.Services.Discussion;

/// <summary>The current state of the thing a discussion is attached to.</summary>
/// <param name="Text">What the anchor is resolved against right now.</param>
/// <param name="TextHash">Its hash, so a thread can record what it was last confirmed against.</param>
/// <param name="Label">Human wording for lists and prompts, e.g. "Beat #134" or "Kyle · description".</param>
public sealed record DiscussSubject(
    string Kind,
    Guid Id,
    string? Field,
    string Text,
    string? TextHash,
    string Label);

/// <summary>
/// One discussable surface. Adding prose, entity fields, beat intent, obligations or anything else
/// to the conversation means one implementation of this plus one DI registration — never a schema
/// change, because <see cref="Data.Entities.DiscussionThread.TargetKind"/> is a string.
///
/// <para>Registered as a list and resolved by <see cref="Kind"/>, the same shape
/// <c>IWriteGateSyncCheck</c> already uses.</para>
///
/// <para>Reading only, for now. Applying a change and scanning its consequences arrive with the
/// request pipeline; they are deliberately absent here rather than present and throwing, so that
/// "this surface cannot be written yet" is a compile-time fact rather than a runtime surprise.</para>
/// </summary>
public interface IDiscussTarget
{
    /// <summary>The <c>TargetKind</c> this handles — see <c>DiscussionTargetKind</c>.</summary>
    string Kind { get; }

    /// <summary>Current text of the target, or null if it no longer exists.</summary>
    Task<DiscussSubject?> LoadAsync(Guid id, string? field, CancellationToken ct = default);
}

/// <summary>Resolves a <c>TargetKind</c> string to the implementation that handles it.</summary>
public sealed class DiscussTargetRegistry
{
    private readonly Dictionary<string, IDiscussTarget> byKind;

    public DiscussTargetRegistry(IEnumerable<IDiscussTarget> targets)
    {
        byKind = targets.ToDictionary(t => t.Kind, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>The handler for a kind, or null when nothing is registered for it — which happens
    /// legitimately when a thread was written by a newer build that knows a surface this one does
    /// not. Such a thread is shown, not discarded.</summary>
    public IDiscussTarget? For(string kind)
        => byKind.TryGetValue(kind, out var t) ? t : null;

    public IReadOnlyCollection<string> Kinds => byKind.Keys;
}
