$ErrorActionPreference = "Stop"

$surveyPath = "D:\Projects\MindAttic\StreetSamurai\engine\data\surname_survey.txt"
$dataRoot   = "D:\Projects\MindAttic\StreetSamurai\engine\data"
$logPath    = "D:\Projects\MindAttic\StreetSamurai\v3\rename_apply.log"
$utf8       = New-Object System.Text.UTF8Encoding $false
$connStr    = "Server=(localdb)\MSSQLLocalDB;Database=StreetSamurai;Trusted_Connection=True;TrustServerCertificate=True;"

function Log([string]$s) {
    $line = "[$((Get-Date).ToString('HH:mm:ss'))] $s"
    Add-Content -LiteralPath $logPath -Value $line -Encoding UTF8
    Write-Host $line
}

# Mirrors WorldGraphService.Slugify: lowercase, non-alphanumeric runs -> underscore, trim
function Slugify([string]$name) {
    $s = $name.Trim().ToLowerInvariant()
    $s = [regex]::Replace($s, '[^a-z0-9]+', '_')
    return $s.Trim('_')
}

# Set log to fresh file
"" | Out-File -LiteralPath $logPath -Encoding UTF8
Log "=== rename apply started ==="

# 1) Parse survey
$proposals = @()
foreach ($line in Get-Content $surveyPath -Encoding UTF8) {
    if ($line -match '^\s*#') { continue }
    if ($line -match '^([^|]+?)\s*\|\s*([0-9a-fA-F-]{36})\s*\|\s*(.+)$') {
        $newLast = $matches[1].Trim()
        $id      = $matches[2].Trim()
        $curName = $matches[3].Trim()
        if ($newLast -ieq 'KEEP') { continue }
        $proposals += [PSCustomObject]@{ Id = $id; OldName = $curName; NewLast = $newLast }
    }
}
Log ("parsed proposals: $($proposals.Count)")

if ($proposals.Count -eq 0) { Log "no proposals -- exiting"; return }

# 2) Pull current FirstName/MiddleName/LastName/Slug from DB so we can compute new full name
$conn = New-Object System.Data.SqlClient.SqlConnection $connStr
$conn.Open()
$plan = New-Object System.Collections.Generic.List[object]
$desiredSlugs = @{}   # newSlug -> count, for collision detection across the batch
$existingSlugByEntityType = @{}   # (entityType:slug) -> entityId, populated lazily

foreach ($p in $proposals) {
    $cmd = $conn.CreateCommand()
    $cmd.CommandText = "SELECT FirstName, MiddleName, LastName, Slug FROM Characters WHERE Id = @id"
    $cmd.Parameters.AddWithValue("@id", [Guid]$p.Id) | Out-Null
    $reader = $cmd.ExecuteReader()
    if (-not $reader.Read()) { $reader.Close(); Log "MISS in DB: $($p.Id)"; continue }
    $first  = if ($reader.IsDBNull(0)) { "" } else { $reader.GetString(0) }
    $middle = if ($reader.IsDBNull(1)) { "" } else { $reader.GetString(1) }
    $oldLast = if ($reader.IsDBNull(2)) { "" } else { $reader.GetString(2) }
    $oldSlug = if ($reader.IsDBNull(3)) { "" } else { $reader.GetString(3) }
    $reader.Close()

    $namePieces = @($first)
    if ($middle) { $namePieces += $middle }
    $namePieces += $p.NewLast
    $newName = ($namePieces -join ' ')
    $newSlug = Slugify $newName

    if (-not $desiredSlugs.ContainsKey($newSlug)) { $desiredSlugs[$newSlug] = 0 }
    $desiredSlugs[$newSlug]++

    $plan.Add([PSCustomObject]@{
        Id       = $p.Id
        OldName  = $p.OldName
        NewName  = $newName
        OldLast  = $oldLast
        NewLast  = $p.NewLast
        OldSlug  = $oldSlug
        NewSlug  = $newSlug
    }) | Out-Null
}

# 3) Slug collision check: against existing OTHER character entities (excluding self) AND within batch
$cmd = $conn.CreateCommand()
$cmd.CommandText = "SELECT Slug, Id FROM Entities WHERE EntityType = 'character' AND IsActive = 1"
$reader = $cmd.ExecuteReader()
$existingCharSlugs = @{}
while ($reader.Read()) { $existingCharSlugs[$reader.GetString(0)] = $reader.GetGuid(1).ToString() }
$reader.Close()

$conflictsResolved = 0
foreach ($r in $plan) {
    $existingHolder = $existingCharSlugs[$r.NewSlug]
    $batchCount     = $desiredSlugs[$r.NewSlug]
    if ($batchCount -gt 1 -or ($existingHolder -and $existingHolder -ine $r.Id)) {
        # disambiguate with -{id-no-dashes}
        $r.NewSlug = "$($r.NewSlug)_$($r.Id.Replace('-',''))"
        $conflictsResolved++
    }
}
Log "rename plan size: $($plan.Count); slug conflicts disambiguated: $conflictsResolved"

# Save the plan as a manifest in case anything goes wrong
$plan | ConvertTo-Json -Depth 3 | Out-File -LiteralPath "D:\Projects\MindAttic\StreetSamurai\v3\rename_plan.json" -Encoding UTF8

# 4) Apply DB updates inside one transaction
$tx = $conn.BeginTransaction()
try {
    $charsUpdated = 0
    $entUpdated   = 0
    foreach ($r in $plan) {
        # Characters typed columns
        $cmd = $conn.CreateCommand()
        $cmd.Transaction = $tx
        $cmd.CommandText = @"
SET QUOTED_IDENTIFIER ON;
UPDATE Characters
   SET Name = @newName, LastName = @newLast, Slug = @newSlug
 WHERE Id = @id;
"@
        $cmd.Parameters.AddWithValue("@id",      [Guid]$r.Id)        | Out-Null
        $cmd.Parameters.AddWithValue("@newName", $r.NewName)         | Out-Null
        $cmd.Parameters.AddWithValue("@newLast", $r.NewLast)         | Out-Null
        $cmd.Parameters.AddWithValue("@newSlug", $r.NewSlug)         | Out-Null
        $charsUpdated += $cmd.ExecuteNonQuery()

        # Entities canonical row
        $cmd = $conn.CreateCommand()
        $cmd.Transaction = $tx
        $cmd.CommandText = @"
SET QUOTED_IDENTIFIER ON;
UPDATE Entities
   SET Name = @newName, Slug = @newSlug, ModifiedAt = SYSUTCDATETIME()
 WHERE Id = @id;
"@
        $cmd.Parameters.AddWithValue("@id",      [Guid]$r.Id)        | Out-Null
        $cmd.Parameters.AddWithValue("@newName", $r.NewName)         | Out-Null
        $cmd.Parameters.AddWithValue("@newSlug", $r.NewSlug)         | Out-Null
        $entUpdated += $cmd.ExecuteNonQuery()
    }
    Log "DB Characters updated: $charsUpdated; Entities updated: $entUpdated"

    # 5) Records.Json -- ONE big batched UPDATE per character (string replace within JSON)
    $recordsUpdated = 0
    foreach ($r in $plan) {
        if ($r.OldName -ceq $r.NewName) { continue }
        $cmd = $conn.CreateCommand()
        $cmd.Transaction = $tx
        $cmd.CommandText = @"
SET QUOTED_IDENTIFIER ON;
UPDATE Records
   SET Json = REPLACE(Json, @old, @new),
       UpdatedAt = SYSUTCDATETIME()
 WHERE Json LIKE N'%' + @old + N'%';
"@
        $cmd.Parameters.AddWithValue("@old", $r.OldName) | Out-Null
        $cmd.Parameters.AddWithValue("@new", $r.NewName) | Out-Null
        $recordsUpdated += $cmd.ExecuteNonQuery()
    }
    Log "Records.Json blobs touched (sum across all renames): $recordsUpdated"

    $tx.Commit()
    Log "DB transaction committed"
} catch {
    $tx.Rollback()
    Log "DB transaction ROLLED BACK: $_"
    throw
} finally {
    $conn.Close()
}

# 6) On-disk JSON sweep -- one read+write per file with all renames batched
$replacements = @{}
foreach ($r in $plan) { $replacements[$r.OldName] = $r.NewName }
$files = Get-ChildItem -Path $dataRoot -Filter *.json -Recurse | Where-Object {
    $_.FullName -notmatch '\\archives\\' -and
    $_.FullName -notmatch '\\schema-snapshots\\' -and
    $_.FullName -notmatch '\\logs\\'
}
$diskTouched = 0
foreach ($f in $files) {
    $content = [System.IO.File]::ReadAllText($f.FullName, $utf8)
    $original = $content
    foreach ($kv in $replacements.GetEnumerator()) {
        if ($content.Contains($kv.Key)) {
            $content = $content.Replace($kv.Key, $kv.Value)
        }
    }
    if ($content -ne $original) {
        if ($f.IsReadOnly) { $f.IsReadOnly = $false }
        [System.IO.File]::WriteAllText($f.FullName, $content, $utf8)
        $diskTouched++
    }
}
Log "on-disk JSON files touched: $diskTouched (of $($files.Count) scanned)"

# 7) Drop graph cache so it regenerates
$graphCache = "D:\Projects\MindAttic\StreetSamurai\engine\data\graph\world_graph.json"
if (Test-Path $graphCache) { Remove-Item -LiteralPath $graphCache -Force; Log "graph cache removed" }

Log "=== rename apply finished ==="
