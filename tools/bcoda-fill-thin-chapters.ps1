# bcoda-fill-thin-chapters.ps1
# Inserts workflow output text as expansion beats for BCODA thin chapters.
# Reads from the workflow output JSON, inserts into Beats + BeatNodes tables.

param(
    [string]$OutputFile = "C:\Users\ryand\AppData\Local\Temp\claude\D--Projects-MindAttic-StreetSamurai\d758f1a3-ce3a-42e4-8773-c2129bb39a82\tasks\we6d759s9.output",
    [switch]$DryRun
)

Set-Location "D:\Projects\MindAttic\StreetSamurai"

$raw      = [System.IO.File]::ReadAllText($OutputFile, [System.Text.Encoding]::UTF8)
$parsed   = $raw | ConvertFrom-Json
$chapters = $parsed.result

# Chapters to fill: chapter number -> Node ID
$thinChapters = @{
    2  = "019EE688-C3C9-7FE1-A5E9-10887755B7DB"
    3  = "019EE688-CAA7-79E4-9C8C-0AD065636556"
    4  = "019EDD05-646F-7A5F-BD9E-4E245FFC8837"
    5  = "019EE696-E235-7339-9824-83B5C3C4CBFC"
    7  = "3983BE10-24A4-4B06-895B-AF46E24B64E0"
    9  = "019EE691-53C8-76C0-BAB1-D0453CF75289"
    14 = "019EE70D-437D-7C8E-BE2F-76820066DD81"
}

$inserted = 0
$errors   = 0

foreach ($num in ($thinChapters.Keys | Sort-Object)) {
    $nodeId = $thinChapters[$num]
    $ch     = $chapters | Where-Object { $_.num -eq $num }

    if (-not $ch -or [string]::IsNullOrWhiteSpace($ch.text)) {
        Write-Host "SKIP Ch${num} - no workflow text found"
        continue
    }

    $wordCount = ($ch.text -split '\s+').Count
    Write-Host "Ch${num} '$($ch.title)': $wordCount words -> node $nodeId"

    if ($DryRun) { continue }

    # Get current max sortkey for this node
    $maxQuery = "SELECT ISNULL(MAX(bn.SortKey),0) AS MaxKey FROM BeatNodes bn WHERE bn.NodeId = '$nodeId'"
    $maxKey   = sqlcmd -S "(localdb)\MSSQLLocalDB" -d StreetSamurai -Q $maxQuery -W -h -1 2>&1 |
                    Select-Object -First 1 | ForEach-Object { ($_ -replace '\s','') -as [double] }

    # Get current max Number across all beats
    $maxNumQuery = "SELECT ISNULL(MAX(Number),0) AS MaxNum FROM Beats"
    $maxNum  = sqlcmd -S "(localdb)\MSSQLLocalDB" -d StreetSamurai -Q $maxNumQuery -W -h -1 2>&1 |
                    Select-Object -First 1 | ForEach-Object { ($_ -replace '\s','') -as [int] }

    $newSortKey  = $maxKey + 1000
    $newNumber   = $maxNum + 1
    $newId       = [System.Guid]::NewGuid().ToString().ToUpper()
    $escapedText = $ch.text.Replace("'", "''")

    # Build SQL using single-quoted strings to avoid PS parser issues with [Text]/[Version]
    $nl  = "`r`n"
    $sql  = 'SET QUOTED_IDENTIFIER ON;' + $nl
    $sql += 'SET ANSI_NULLS ON;' + $nl
    $sql += 'DECLARE @beatId UNIQUEIDENTIFIER = ''' + $newId + ''';' + $nl
    $sql += 'INSERT INTO Beats (Id, [Text], Number, Act, SceneType, Stale, WasCorrected, IsChapterStart, Kind, [Version], EntityStale)' + $nl
    $sql += 'VALUES (@beatId, N''' + $escapedText + ''', ' + $newNumber + ', 1, N''scene'', 0, 0, 0, N''prose'', 0, 0);' + $nl
    $sql += 'INSERT INTO BeatNodes (NodeId, BeatId, SortKey)' + $nl
    $sql += 'VALUES (''' + $nodeId + ''', @beatId, ' + $newSortKey + ');' + $nl
    $sql += 'SELECT ''' + $newId + ''' AS InsertedBeatId;'

    $tmpFile = [System.IO.Path]::Combine([System.IO.Path]::GetTempPath(), "bcoda_fill_ch${num}.sql")
    [System.IO.File]::WriteAllText($tmpFile, $sql, [System.Text.Encoding]::UTF8)

    $result    = sqlcmd -S "(localdb)\MSSQLLocalDB" -d StreetSamurai -i $tmpFile -f 65001 2>&1
    $resultStr = ($result | Out-String)

    if ($resultStr -match 'Msg \d+') {
        Write-Host "  SQL ERROR Ch${num}: $resultStr"
        $errors++
    } elseif ($LASTEXITCODE -ne 0) {
        Write-Host "  ERROR Ch${num}: exit=$LASTEXITCODE $resultStr"
        $errors++
    } else {
        Write-Host "  -> Inserted expansion beat for Ch${num} at SortKey $newSortKey"
        $inserted++
    }

    Remove-Item $tmpFile -ErrorAction SilentlyContinue
}

Write-Host ""
Write-Host "Done: $inserted inserted, $errors errors"
