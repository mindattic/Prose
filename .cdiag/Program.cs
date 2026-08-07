using Microsoft.Data.Sqlite;

var dbPath = @"D:\Projects\MindAttic\Prose\engine\data\continuity.db";
var cs = $"Data Source={dbPath};Mode=ReadOnly";

using var conn = new SqliteConnection(cs);
conn.Open();

void Q(string label, string sql)
{
    Console.WriteLine($"\n── {label} ─────────────────────────────");
    using var c = conn.CreateCommand();
    c.CommandText = sql;
    using var r = c.ExecuteReader();
    var cols = new List<string>();
    for (int i = 0; i < r.FieldCount; i++) cols.Add(r.GetName(i));
    Console.WriteLine(string.Join(" | ", cols));
    int rows = 0;
    while (r.Read())
    {
        var vals = new List<string>();
        for (int i = 0; i < r.FieldCount; i++)
        {
            var v = r.IsDBNull(i) ? "NULL" : r.GetValue(i)?.ToString() ?? "";
            if (v.Length > 80) v = v.Substring(0, 77) + "...";
            vals.Add(v);
        }
        Console.WriteLine(string.Join(" | ", vals));
        rows++;
        if (rows >= 25) { Console.WriteLine("... (truncated)"); break; }
    }
    if (rows == 0) Console.WriteLine("(no rows)");
}

Q("Tables", "SELECT name FROM sqlite_master WHERE type='table' ORDER BY name;");
Q("Status counts", "SELECT status, COUNT(*) FROM claims GROUP BY status ORDER BY status;");
Q("contradictions row count", "SELECT COUNT(*) AS cc_rows FROM claim_contradictions;");
Q("contradictions x end statuses",
  "SELECT a.status AS a_status, b.status AS b_status, COUNT(*) AS n " +
  "FROM claim_contradictions cc " +
  "JOIN claims a ON a.claim_uid = cc.a_uid " +
  "JOIN claims b ON b.claim_uid = cc.b_uid " +
  "GROUP BY a.status, b.status ORDER BY n DESC;");
Q("Sample CONTRADICTED claims",
  "SELECT claim_uid, entity_name, predicate, substr(object,1,60) AS object, source_type FROM claims WHERE status='CONTRADICTED' LIMIT 10;");
Q("CONTRADICTED claims with NO row in claim_contradictions",
  "SELECT COUNT(*) AS orphans FROM claims c " +
  "WHERE c.status='CONTRADICTED' " +
  "  AND NOT EXISTS (SELECT 1 FROM claim_contradictions cc WHERE cc.a_uid=c.claim_uid OR cc.b_uid=c.claim_uid);");
Q("CONTRADICTED grouped by (entity, predicate)",
  "SELECT entity_name, predicate, COUNT(*) AS n, GROUP_CONCAT(substr(object,1,40), ' || ') AS objects " +
  "FROM claims WHERE status='CONTRADICTED' " +
  "GROUP BY entity_id, predicate ORDER BY n DESC, entity_name;");

Q("Singletons (only 1 CONTRADICTED claim in the group — these are MIS-flagged)",
  "SELECT COUNT(*) AS singleton_groups FROM (" +
  "  SELECT entity_id, predicate, COUNT(*) AS n FROM claims WHERE status='CONTRADICTED' " +
  "  GROUP BY entity_id, predicate HAVING n = 1" +
  ");");

Q("Paired but partner is not CONTRADICTED (by partner status)",
  "SELECT partner_status, COUNT(*) AS n FROM (" +
  "  SELECT b.status AS partner_status FROM claims a " +
  "  JOIN claim_contradictions cc ON cc.a_uid = a.claim_uid " +
  "  JOIN claims b ON b.claim_uid = cc.b_uid " +
  "  WHERE a.status='CONTRADICTED' AND b.status<>'CONTRADICTED' " +
  "  UNION ALL " +
  "  SELECT a.status FROM claims b " +
  "  JOIN claim_contradictions cc ON cc.b_uid = b.claim_uid " +
  "  JOIN claims a ON a.claim_uid = cc.a_uid " +
  "  WHERE b.status='CONTRADICTED' AND a.status<>'CONTRADICTED' " +
  ") GROUP BY partner_status ORDER BY n DESC;");
