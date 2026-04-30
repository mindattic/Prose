namespace StreetSamurai.Core.Interfaces;

/// <summary>
/// Abstracts role-based access checks so Shared components work across Blazor (auth-based)
/// and test contexts without referencing authentication types directly.
/// Roles are hierarchical: an Administrator is also a Contributor. Readonly mode forces
/// every effective role down to Visitor regardless of the underlying claim.
/// </summary>
public interface IWriteAccessProvider
{
    /// <summary>True if the current user has read-only access (unauthenticated, "User" role, or readonly mode).</summary>
    bool IsVisitor { get; }

    /// <summary>True if the current user can create/edit/delete entities (Contributor or Administrator, not in readonly mode).</summary>
    bool IsContributor { get; }

    /// <summary>True if the current user has admin privileges — user management, settings, archive (Administrator only, not in readonly mode).</summary>
    bool IsAdministrator { get; }

    /// <summary>True when the build config forces pure readonly mode (no login, no auth).</summary>
    bool IsReadOnlyMode { get; }

    string CurrentUserName { get; }
    string CurrentUserRole { get; }
}
