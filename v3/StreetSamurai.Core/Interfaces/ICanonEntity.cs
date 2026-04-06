namespace StreetSamurai.Core.Interfaces;

/// <summary>
/// Marker interface for all canon entities stored via JsonDirectoryRepository.
/// Provides a stable unique Id (GUID) distinct from the human-readable Name.
/// </summary>
public interface ICanonEntity
{
    /// <summary>Stable unique identifier — auto-generated, never changes even if Name is edited.</summary>
    string Id { get; set; }
}
