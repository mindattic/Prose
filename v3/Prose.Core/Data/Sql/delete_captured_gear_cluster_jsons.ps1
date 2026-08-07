# Delete every engine/data/<gear>/*.json file that is captured in the
# Prose SQL Server database. Driven by the manifest written by
# `ss --repair --prune-json-report`. Per the 2026-05-08 memory rule
# "DB is the only canon store" + the explicit user directive to delete
# (not archive) JSON files already captured in the DB, this is a hard
# delete, not a move.
#
# Decision matrix (per-row):
#   Match  → DB has byte-identical canonical copy → DELETE
#   Drift  → DB has the entity by Id; file is older revision → DELETE
#   NoId   → name-lookup against DB; if entity found → DELETE, else SKIP
#   Missing / NoRepo → SKIP (not safe to delete without DB confirmation)
#
# Idempotent: re-running on a directory that's already drained skips
# missing source files.
#
# After running, re-run `ss --repair --prune-json-report` to confirm zero
# remaining files in the scanned folders.

$ErrorActionPreference = 'Stop'

$ManifestPath = 'D:\Projects\MindAttic\Prose\engine\data\archives\json-prune-2026-05-08\manifest.tsv'
$ConnString   = 'Server=(localdb)\MSSQLLocalDB;Database=Prose;Trusted_Connection=True;TrustServerCertificate=True;'

if (-not (Test-Path $ManifestPath)) { Write-Error "Manifest not found: $ManifestPath"; exit 1 }

$rows = Import-Csv -Path $ManifestPath -Delimiter "`t"
Write-Host "Loaded $($rows.Count) manifest rows."

# For NoId rows we need a name-lookup against the DB. Pre-fetch the
# (folder, name) → bool map so we don't open one connection per row.
function Test-EntityExistsByName {
    param([string]$EntityType, [string]$Name)
    Add-Type -AssemblyName 'System.Data'
    $conn = New-Object System.Data.SqlClient.SqlConnection $ConnString
    $conn.Open()
    try {
        $cmd = $conn.CreateCommand()
        $cmd.CommandText = @'
SET QUOTED_IDENTIFIER ON; SET ANSI_NULLS ON;
SELECT TOP 1 1 FROM Entities
WHERE EntityType = @t AND Name = @n AND IsActive = 1;
'@
        [void]$cmd.Parameters.AddWithValue('@t', $EntityType)
        [void]$cmd.Parameters.AddWithValue('@n', $Name)
        $r = $cmd.ExecuteScalar()
        return $null -ne $r
    } finally { $conn.Close() }
}

# Folder → entity-type label mapping (mirrors EfRepository.RepoNameMap).
$folderToType = @{
    'weaponry'        = 'weapon'
    'equipment'       = 'equipment'
    'cyberware'       = 'cyberware'
    'apparel'         = 'apparel'
    'ammunition'      = 'ammunition'
    'pharmaceuticals' = 'pharmaceutical'
    'genemods'        = 'genemod'
    'materials'       = 'material'
    'transportation'  = 'transportation'
    'consumer_goods'  = 'consumer_good'
}

$deleted   = 0
$skipped   = 0
$noidKept  = 0
$missingFile = 0
$errCount  = 0

foreach ($row in $rows) {
    $src = $row.file
    $status = $row.status
    $folder = $row.folder

    if (-not (Test-Path $src)) { $missingFile++; continue }

    $shouldDelete = $false
    switch ($status) {
        'Match' { $shouldDelete = $true }
        'Drift' { $shouldDelete = $true }
        'NoId'  {
            # Last-chance name lookup. Read just enough JSON to extract Name.
            try {
                $obj  = Get-Content -LiteralPath $src -Raw | ConvertFrom-Json
                $name = $obj.name
                $type = $folderToType[$folder]
                if ($name -and $type -and (Test-EntityExistsByName -EntityType $type -Name $name)) {
                    $shouldDelete = $true
                } else {
                    Write-Host "  [keep] NoId not in DB: $src"
                    $noidKept++
                }
            } catch {
                Write-Host "  [keep] NoId parse failure: $src"
                $noidKept++
            }
        }
        default { $skipped++; continue }
    }

    if ($shouldDelete) {
        try {
            Remove-Item -LiteralPath $src -Force
            $deleted++
        } catch {
            Write-Host "  [error] $src - $($_.Exception.Message)"
            $errCount++
        }
    }
}

Write-Host ''
Write-Host "deleted     : $deleted"
Write-Host "noid kept   : $noidKept"
Write-Host "skipped     : $skipped (Missing / NoRepo)"
Write-Host "already gone: $missingFile"
Write-Host "errors      : $errCount"
