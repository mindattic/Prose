namespace StreetSamurai.Core.Interfaces;

/// <summary>
/// Marker interface for all canon entities stored via JsonDirectoryRepository.
/// Provides a stable unique Id (GUID) distinct from the human-readable Name.
/// </summary>
public interface ICanonEntity : IWorldRecord
{
    /// <summary>Interest score 0–100. Populated by LLMVoting; editable manually.</summary>
    double Rating { get; set; }
}
