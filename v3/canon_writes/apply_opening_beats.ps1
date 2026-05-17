# Parses the rogue_ai_beats.md and street_meat_beats.md files (==BEAT N==
# delimited) and inserts each beat as a ChapterBeats row. Idempotent per
# (chapter, title) pair.

$ErrorActionPreference = 'Stop'
$connStr = 'Server=(localdb)\MSSQLLocalDB;Database=StreetSamurai;Trusted_Connection=True;TrustServerCertificate=True;'
$conn = New-Object System.Data.SqlClient.SqlConnection $connStr

$chapters = @(
    @{ ChapterId = [Guid]'6D75764C-C8DA-8E32-FD73-3DD5C43E92E2'; File = 'D:\Projects\MindAttic\StreetSamurai\v3\canon_writes\rogue_ai_beats.md';  Name = 'The Rogue AI' },
    @{ ChapterId = [Guid]'019DB31F-E888-7C97-A049-65978B5CCDB3'; File = 'D:\Projects\MindAttic\StreetSamurai\v3\canon_writes\street_meat_beats.md'; Name = 'Street Meat'  }
)

function Parse-Beats {
    param([string]$Path)
    $raw = Get-Content -Raw -Encoding UTF8 $Path
    # Split on ==BEAT N==
    $blocks = [regex]::Split($raw, '==BEAT \d+==\s*')
    $beats = @()
    foreach ($block in $blocks) {
        $block = $block.Trim()
        if ($block.Length -eq 0) { continue }
        # Each block starts with TITLE: ... newline TEXT: ... rest
        $m = [regex]::Match($block, '^TITLE:\s*(.+?)\r?\nTEXT:\s*\r?\n([\s\S]+)$')
        if (-not $m.Success) {
            Write-Host ("Skip malformed block: {0}" -f $block.Substring(0, [Math]::Min(60, $block.Length)))
            continue
        }
        $beats += [PSCustomObject]@{
            Title = $m.Groups[1].Value.Trim()
            Text  = $m.Groups[2].Value.Trim()
        }
    }
    return ,$beats
}

$conn.Open()

foreach ($ch in $chapters) {
    $beats = Parse-Beats -Path $ch.File
    Write-Host ("{0}: parsed {1} beat(s)" -f $ch.Name, $beats.Count)

    foreach ($b in $beats) {
        # Idempotency: skip if a beat with this title already exists in this chapter
        $check = $conn.CreateCommand()
        $check.CommandText = 'SELECT COUNT(*) FROM ChapterBeats WHERE ChapterId = @c AND Title = @t;'
        [void]$check.Parameters.AddWithValue('@c', $ch.ChapterId)
        [void]$check.Parameters.AddWithValue('@t', $b.Title)
        $existing = [int]$check.ExecuteScalar()
        $check.Dispose()
        if ($existing -gt 0) {
            Write-Host ("  Skipped (already exists): {0}" -f $b.Title)
            continue
        }

        $tx = $conn.BeginTransaction()
        try {
            $beatGuid = [Guid]::NewGuid()
            $cmd = $conn.CreateCommand(); $cmd.Transaction = $tx
            $cmd.CommandText = "INSERT INTO ChapterBeats (BeatGuid, ChapterId, [Index], Title, Synopsis, [Text], Act, StructureRole, SceneType, FacetTag, InWorldDate) VALUES (@guid, @ch, 0, @t, N'', @text, 0, N'', N'scene', N'', NULL);"
            [void]$cmd.Parameters.AddWithValue('@guid', $beatGuid)
            [void]$cmd.Parameters.AddWithValue('@ch',   $ch.ChapterId)
            [void]$cmd.Parameters.AddWithValue('@t',    $b.Title)
            [void]$cmd.Parameters.AddWithValue('@text', $b.Text)
            $cmd.ExecuteNonQuery() | Out-Null; $cmd.Dispose()
            $tx.Commit()
            Write-Host ("  Inserted: {0} ({1} chars)" -f $b.Title, $b.Text.Length)
        } catch {
            $tx.Rollback()
            Write-Host ("  ROLLED BACK on {0}: {1}" -f $b.Title, $_)
            throw
        }
    }
}

$conn.Close()
Write-Host "=== Done ==="
