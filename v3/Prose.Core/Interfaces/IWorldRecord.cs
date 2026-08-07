namespace Prose.Core.Interfaces;

/// <summary>
/// Minimal interface for any persistent world data record that needs a stable file ID.
/// Does NOT imply graph connectivity, ratings, or media generation.
/// Implemented by ambient world data (quotes, vocabulary) that exists alongside
/// but outside the entity graph.
/// </summary>
public interface IWorldRecord
{
    string Id { get; set; }
}
