namespace Prose.Core.Interfaces;

/// <summary>
/// Implemented by repositories that can round-trip a single canonical JSON
/// blob (the on-disk shape that used to live under <c>engine/data/&lt;folder&gt;/*.json</c>)
/// into the SQL Server Prose database. Originally used by JsonPruneService
/// (retired 2026-05-08); the contract survives for user-submitted JSON imports
/// per the project rule "JSON is INPUT to interpret into the DB, not material to
/// save to disk."
///
/// Every <see cref="Prose.Core.Data.EfRepository{T}"/> implements this
/// for free — non-EfRepository repos opt in by hand.
/// </summary>
public interface IJsonImportable
{
    /// <summary>Display-friendly name (matches IExportableRepository.RepoName / folder label).</summary>
    string RepoName { get; }

    /// <summary>
    /// Deserialize the JSON into the repository's domain type and route through
    /// the standard <c>Save()</c> path. Idempotent: if an entity with the same
    /// Id already exists, it's updated rather than inserted twice.
    /// Throws on malformed JSON or missing required fields — callers should
    /// catch and report rather than crash a bulk loop.
    /// </summary>
    void ImportFromJson(string fileJson);

    /// <summary>
    /// Compares the on-disk JSON against the canonical <c>Records.Json</c>
    /// already stored in the DB for the same entity. Returns one of:
    ///   <list type="bullet">
    ///     <item><c>Missing</c> — Id is not in <c>Entities</c></item>
    ///     <item><c>Match</c>   — round-trips identical to the DB blob</item>
    ///     <item><c>Drift</c>   — Id is in <c>Entities</c> but content differs</item>
    ///     <item><c>NoId</c>    — file has no usable Id field</item>
    ///   </list>
    /// Used by the prune verifier — only <c>Match</c> files are safe to
    /// archive without losing canon.
    /// </summary>
    JsonVerifyResult VerifyAgainstDb(string fileJson);
}

/// <summary>Outcome of <see cref="IJsonImportable.VerifyAgainstDb"/>.</summary>
public enum JsonVerifyResult
{
    /// <summary>File has no <c>id</c> field we can resolve to an Entity row.</summary>
    NoId,

    /// <summary>Id present in file but no matching <c>Entities</c> row in DB.</summary>
    Missing,

    /// <summary>Id present in DB and content round-trips identical (after canonical re-serialization).</summary>
    Match,

    /// <summary>Id present in DB but content differs — file may have data not captured in <c>Records.Json</c>.</summary>
    Drift,
}
