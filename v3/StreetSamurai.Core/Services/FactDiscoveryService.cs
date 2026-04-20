using Microsoft.Data.Sqlite;
using StreetSamurai.Core.Interfaces;

namespace StreetSamurai.Core.Services;

/// <summary>
/// Read/write access to facts.db — the fact discovery database.
/// Populated either by the legacy Python pipeline or the in-process FactExtractionService.
/// </summary>
public class FactDiscoveryService
{
    private readonly string dbPath;

    private const string SchemaSql = """
        CREATE TABLE IF NOT EXISTS triples (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            source_file TEXT, source_repo TEXT, entity_name TEXT,
            subject TEXT, predicate TEXT, object TEXT, full_sentence TEXT,
            embedding BLOB, cluster_id INTEGER DEFAULT 0,
            created_at TEXT DEFAULT (datetime('now'))
        );
        CREATE INDEX IF NOT EXISTS idx_triples_subject ON triples(subject);
        CREATE TABLE IF NOT EXISTS clusters (
            cluster_id INTEGER PRIMARY KEY,
            representative_sentence TEXT, triple_count INTEGER, unique_sources INTEGER
        );
        CREATE TABLE IF NOT EXISTS fact_scores (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            cluster_id INTEGER DEFAULT 0,
            subject TEXT, predicate TEXT, consensus_object TEXT,
            confidence REAL, agreeing_sources INTEGER, dissenting_sources INTEGER, total_sources INTEGER
        );
        CREATE TABLE IF NOT EXISTS flagged_triples (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            triple_id INTEGER, source_file TEXT, entity_name TEXT,
            subject TEXT, predicate TEXT,
            incorrect_object TEXT, correct_object TEXT,
            confidence REAL, repaired INTEGER DEFAULT 0
        );
        CREATE TABLE IF NOT EXISTS processing_log (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            phase TEXT, status TEXT, files_processed INTEGER,
            triples_extracted INTEGER, message TEXT,
            timestamp TEXT DEFAULT (datetime('now'))
        );
        """;

    public FactDiscoveryService(IPathProvider paths)
    {
        dbPath = Path.Combine(paths.DataRoot, "v3", "python", "facts.db");
    }

    public bool IsAvailable => File.Exists(dbPath);

    private SqliteConnection Open(bool write = false)
    {
        var mode = write ? "ReadWrite;Cache=Shared" : "ReadOnly";
        var conn = new SqliteConnection($"Data Source={dbPath};Mode={mode}");
        conn.Open();
        return conn;
    }

    // ── Schema / lifecycle ────────────────────────────────────

    public void EnsureSchema()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);
        var conn = new SqliteConnection($"Data Source={dbPath}");
        conn.Open();
        using (conn)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = SchemaSql;
            cmd.ExecuteNonQuery();
        }
    }

    public void ClearExtractionData()
    {
        using var conn = Open(write: true);
        foreach (var sql in new[] {
            "DELETE FROM flagged_triples",
            "DELETE FROM fact_scores",
            "DELETE FROM triples",
            "DELETE FROM clusters"
        })
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            cmd.ExecuteNonQuery();
        }
    }

    // ── Write: triples ────────────────────────────────────────

    public void WriteTriples(IEnumerable<FactTriple> triples)
    {
        using var conn = Open(write: true);
        using var tx = conn.BeginTransaction();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT INTO triples (source_file, source_repo, entity_name, subject, predicate, object, full_sentence)
            VALUES (@sf, @sr, @en, @s, @p, @o, @fs)
            """;
        var pSf = cmd.Parameters.Add("@sf", SqliteType.Text);
        var pSr = cmd.Parameters.Add("@sr", SqliteType.Text);
        var pEn = cmd.Parameters.Add("@en", SqliteType.Text);
        var pS  = cmd.Parameters.Add("@s",  SqliteType.Text);
        var pP  = cmd.Parameters.Add("@p",  SqliteType.Text);
        var pO  = cmd.Parameters.Add("@o",  SqliteType.Text);
        var pFs = cmd.Parameters.Add("@fs", SqliteType.Text);

        foreach (var t in triples)
        {
            pSf.Value = t.SourceFile;
            pSr.Value = t.SourceRepo;
            pEn.Value = t.EntityName;
            pS.Value  = t.Subject;
            pP.Value  = t.Predicate;
            pO.Value  = t.Object;
            pFs.Value = $"{t.Subject} {t.Predicate} {t.Object}";
            cmd.ExecuteNonQuery();
        }
        tx.Commit();
    }

    // ── Write: consensus scoring ──────────────────────────────

    public void BuildConsensus()
    {
        using var conn = Open(write: true);

        // Load all triples into memory for grouping
        var triples = new List<(int Id, string Subject, string Predicate, string Obj, string SourceFile, string EntityName)>();
        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = "SELECT id, subject, predicate, object, source_file, entity_name FROM triples";
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
                triples.Add((reader.GetInt32(0), reader.GetString(1), reader.GetString(2),
                             reader.GetString(3), reader.GetString(4), reader.GetString(5)));
        }

        var groups = triples.GroupBy(t => (t.Subject, t.Predicate));

        using var tx = conn.BeginTransaction();
        foreach (var group in groups)
        {
            var total = group.Count();
            var byObj = group
                .GroupBy(t => t.Obj.Trim().ToLowerInvariant())
                .Select(g => (Obj: g.First().Obj, Count: g.Count(), Rows: g.ToList()))
                .OrderByDescending(x => x.Count)
                .ToList();

            var winner     = byObj[0];
            var confidence = (double)winner.Count / total;

            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = """
                    INSERT INTO fact_scores (cluster_id, subject, predicate, consensus_object, confidence, agreeing_sources, dissenting_sources, total_sources)
                    VALUES (0, @s, @p, @co, @conf, @ag, @ds, @tot)
                    """;
                cmd.Parameters.AddWithValue("@s",    group.Key.Subject);
                cmd.Parameters.AddWithValue("@p",    group.Key.Predicate);
                cmd.Parameters.AddWithValue("@co",   winner.Obj);
                cmd.Parameters.AddWithValue("@conf", confidence);
                cmd.Parameters.AddWithValue("@ag",   winner.Count);
                cmd.Parameters.AddWithValue("@ds",   total - winner.Count);
                cmd.Parameters.AddWithValue("@tot",  total);
                cmd.ExecuteNonQuery();
            }

            // Flag dissenting sources when confidence is meaningful
            if (confidence >= 0.6 && byObj.Count > 1)
            {
                foreach (var loser in byObj.Skip(1))
                {
                    foreach (var row in loser.Rows)
                    {
                        using var cmd = conn.CreateCommand();
                        cmd.CommandText = """
                            INSERT INTO flagged_triples (triple_id, source_file, entity_name, subject, predicate, incorrect_object, correct_object, confidence)
                            VALUES (@tid, @sf, @en, @s, @p, @io, @co, @conf)
                            """;
                        cmd.Parameters.AddWithValue("@tid",  row.Id);
                        cmd.Parameters.AddWithValue("@sf",   row.SourceFile);
                        cmd.Parameters.AddWithValue("@en",   row.EntityName);
                        cmd.Parameters.AddWithValue("@s",    row.Subject);
                        cmd.Parameters.AddWithValue("@p",    row.Predicate);
                        cmd.Parameters.AddWithValue("@io",   row.Obj);
                        cmd.Parameters.AddWithValue("@co",   winner.Obj);
                        cmd.Parameters.AddWithValue("@conf", confidence);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
        }
        tx.Commit();
    }

    // ── Write: dismiss flagged items ──────────────────────────

    public void MarkDismissed(int id)
    {
        using var conn = Open(write: true);
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "UPDATE flagged_triples SET repaired = 1 WHERE id = @id";
        cmd.Parameters.AddWithValue("@id", id);
        cmd.ExecuteNonQuery();
    }

    // ── Read: stats ───────────────────────────────────────────

    public FactStats GetStats()
    {
        if (!IsAvailable) return new FactStats();
        try
        {
            using var conn = Open();
            var stats = new FactStats
            {
                TotalTriples           = QueryInt(conn, "SELECT COUNT(*) FROM triples"),
                SourceFiles            = QueryInt(conn, "SELECT COUNT(DISTINCT source_file) FROM triples"),
                Clusters               = QueryInt(conn, "SELECT COUNT(*) FROM clusters"),
                ConsensussClaims       = QueryInt(conn, "SELECT COUNT(*) FROM fact_scores"),
                FlaggedInconsistencies = QueryInt(conn, "SELECT COUNT(*) FROM flagged_triples WHERE repaired = 0"),
                Repaired               = QueryInt(conn, "SELECT COUNT(*) FROM flagged_triples WHERE repaired = 1")
            };
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT AVG(confidence) FROM fact_scores";
            var avg = cmd.ExecuteScalar();
            stats.AverageConfidence = avg is double d ? d : 0;
            return stats;
        }
        catch { return new FactStats(); }
    }

    // ── Read: query ───────────────────────────────────────────

    public List<ConsensusClaim> QuerySubject(string subject)
    {
        if (!IsAvailable) return [];
        try
        {
            using var conn = Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = """
                SELECT subject, predicate, consensus_object, confidence, agreeing_sources, dissenting_sources, total_sources
                FROM fact_scores WHERE LOWER(subject) LIKE @q ORDER BY confidence DESC LIMIT 50
                """;
            cmd.Parameters.AddWithValue("@q", $"%{subject.ToLowerInvariant()}%");
            var results = new List<ConsensusClaim>();
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
                results.Add(new ConsensusClaim
                {
                    Subject       = reader.GetString(0),
                    Predicate     = reader.GetString(1),
                    Value         = reader.GetString(2),
                    Confidence    = reader.GetDouble(3),
                    AgreeSources  = reader.GetInt32(4),
                    DissentSources = reader.GetInt32(5),
                    TotalSources  = reader.GetInt32(6)
                });
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
            cmd.CommandText = """
                SELECT id, entity_name, subject, predicate, incorrect_object, correct_object, confidence, source_file
                FROM flagged_triples WHERE repaired = 0 AND confidence >= @min
                ORDER BY confidence DESC LIMIT @limit
                """;
            cmd.Parameters.AddWithValue("@min", minConfidence);
            cmd.Parameters.AddWithValue("@limit", limit);
            var results = new List<FlaggedFact>();
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
                results.Add(new FlaggedFact
                {
                    Id             = reader.GetInt32(0),
                    EntityName     = reader.GetString(1),
                    Subject        = reader.GetString(2),
                    Predicate      = reader.GetString(3),
                    IncorrectValue = reader.GetString(4),
                    CorrectValue   = reader.GetString(5),
                    Confidence     = reader.GetDouble(6),
                    SourceFile     = reader.GetString(7)
                });
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
    public int    TotalTriples           { get; set; }
    public int    SourceFiles            { get; set; }
    public int    Clusters               { get; set; }
    public int    ConsensussClaims       { get; set; }
    public int    FlaggedInconsistencies { get; set; }
    public int    Repaired               { get; set; }
    public double AverageConfidence      { get; set; }
}

public record ConsensusClaim
{
    public string Subject       { get; set; } = "";
    public string Predicate     { get; set; } = "";
    public string Value         { get; set; } = "";
    public double Confidence    { get; set; }
    public int    AgreeSources  { get; set; }
    public int    DissentSources { get; set; }
    public int    TotalSources  { get; set; }
}

public record FlaggedFact
{
    public int    Id             { get; set; }
    public string EntityName     { get; set; } = "";
    public string Subject        { get; set; } = "";
    public string Predicate      { get; set; } = "";
    public string IncorrectValue { get; set; } = "";
    public string CorrectValue   { get; set; } = "";
    public double Confidence     { get; set; }
    public string SourceFile     { get; set; } = "";
}
