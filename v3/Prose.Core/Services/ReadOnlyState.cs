namespace Prose.Core.Services;

/// <summary>
/// Holds the global read-only flag. When true, all write UI is hidden.
/// Set once at startup from configuration (appsettings.ReadOnly.json).
/// </summary>
public class ReadOnlyState
{
    public bool IsReadOnly { get; set; }
}
