using Microsoft.Data.Sqlite;
using StreetSamurai.Core.Interfaces;

namespace StreetSamurai.Core.Services;

/// <summary>
/// Reads results from the Python fact discovery pipeline (facts.db).
/// Provides query access to consensus claims, flagged inconsistencies,
/// and pipeline statistics for the Blazor UI.
///
/// The Python pipeline does the heavy lifting:
/// - SPO extraction via Claude API
/// - Embedding via sentence-transformers
/// - Clustering via HDBSCAN
/// - Consensus scoring
///
/// This service just reads the results. No Python dependency at runtime.
/// </summary>
public class FactDiscoveryService
{
    private readonly string dbPath;

    public FactDiscoveryService(IPathProvider paths)
    {
        dbPath = Path.Combine(paths.DataRoot, "v3", "truth-discovery", "facts.db");
    }

    public bool IsAvailable => File.Exists(dbPath);

    private SqliteConnection Open()
    {
        var conn = new SqliteConnection($"Data Source={dbPath};Mode=ReadOnly");
        conn.Open();
        return conn;
    }

    public FactStats GetStats()
    {
        if (!IsAvailable) return new FactStats();
        try
        {
            using var conn = Open();
            var stats = new FactStats();

            stats.TotalTriples = QueryInt(conn, "SELECT COUNT(*) FROM triples");
            stats.SourceFiles = QueryInt(conn, "SELECT COUNT(DISTINCT source_file) FROM triples");
            stats.Clusters = QueryInt(conn, "SELECT COUNT(*) FROM clusters");
            stats.ConsensussClaims = QueryInt(conn, "SELECT COUNT(*) FROM fact_scores");
            stats.FlaggedInconsistencies = QueryInt(conn, "SELECT COUNT(*) FROM flagged_triples WHERE repaired = 0");
            stats.Repaired = QueryInt(conn, "SELECT COUNT(*) FROM flagged_triples WHERE repaired = 1");

            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT AVG(confidence) FROM fact_scores";
            var avg = cmd.ExecuteScalar();
            stats.AverageConfidence = avg is double d ? d : 0;

            return stats;
        }
        catch { return new FactStats(); }
    }

    public List<ConsensusClaim> QuerySubject(string subject)
    {
        if (!IsAvailable) return [];
        try
        {
            using var conn = Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT subject, predicate, consensus_object, confidence, agreeing_sources, dissenting_sources, total_sources
                FROM fact_scores WHERE LOWER(subject) LIKE @q ORDER BY confidence DESC LIMIT 50";
            cmd.Parameters.AddWithValue("@q", $"%{subject.ToLowerInvariant()}%");

            var results = new List<ConsensusClaim>();
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                results.Add(new ConsensusClaim
                {
                    Subject = reader.GetString(0),
                    Predicate = reader.GetString(1),
                    Value = reader.GetString(2),
                    Confidence = reader.GetDouble(3),
                    AgreeSources = reader.GetInt32(4),
                    DissentSources = reader.GetInt32(5),
                    TotalSources = reader.GetInt32(6)
                });
            }
            return results;
        }
        catch { return []; }
    }

    public List<FlaggedFact> GetFlagged(int limit = 100, double minConfidence = 0)
    {
        if (!IsAvailable) return [];
        try
        {
            using var conn = Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT entity_name, subject, predicate, incorrect_object, correct_object, confidence, source_file
                FROM flagged_triples WHERE repaired = 0 AND confidence >= @min
                ORDER BY confidence DESC LIMIT @limit";
            cmd.Parameters.AddWithValue("@min", minConfidence);
            cmd.Parameters.AddWithValue("@limit", limit);

            var results = new List<FlaggedFact>();
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                results.Add(new FlaggedFact
                {
                    EntityName = reader.GetString(0),
                    Subject = reader.GetString(1),
                    Predicate = reader.GetString(2),
                    IncorrectValue = reader.GetString(3),
                    CorrectValue = reader.GetString(4),
                    Confidence = reader.GetDouble(5),
                    SourceFile = reader.GetString(6)
                });
            }
            return results;
        }
        catch { return []; }
    }

    private static int QueryInt(SqliteConnection conn, string sql)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        return Convert.ToInt32(cmd.ExecuteScalar() ?? 0);
    }
}

public record FactStats
{
    public int TotalTriples { get; set; }
    public int SourceFiles { get; set; }
    public int Clusters { get; set; }
    public int ConsensussClaims { get; set; }
    public int FlaggedInconsistencies { get; set; }
    public int Repaired { get; set; }
    public double AverageConfidence { get; set; }
}

public record ConsensusClaim
{
    public string Subject { get; set; } = "";
    public string Predicate { get; set; } = "";
    public string Value { get; set; } = "";
    public double Confidence { get; set; }
    public int AgreeSources { get; set; }
    public int DissentSources { get; set; }
    public int TotalSources { get; set; }
}

public record FlaggedFact
{
    public string EntityName { get; set; } = "";
    public string Subject { get; set; } = "";
    public string Predicate { get; set; } = "";
    public string IncorrectValue { get; set; } = "";
    public string CorrectValue { get; set; } = "";
    public double Confidence { get; set; }
    public string SourceFile { get; set; } = "";
}
