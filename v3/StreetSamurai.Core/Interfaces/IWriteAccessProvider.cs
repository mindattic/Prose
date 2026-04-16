namespace StreetSamurai.Core.Interfaces;

/// <summary>
/// Abstracts write-access checks so Shared components work across Blazor (auth-based)
/// and test contexts without referencing authentication types directly.
/// </summary>
public interface IWriteAccessProvider
{
    /// <summary>True if the current user can create/edit/delete entities.</summary>
    bool CanWrite { get; }

    /// <summary>True if the current user has admin privileges (user management, settings).</summary>
    bool CanAdminister { get; }

    /// <summary>True when the build config forces pure readonly mode (no login, no auth).</summary>
    bool IsReadOnlyMode { get; }

    string CurrentUserName { get; }
    string CurrentUserRole { get; }
}
