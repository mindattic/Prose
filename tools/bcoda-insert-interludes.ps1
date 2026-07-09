# bcoda-insert-interludes.ps1
# Inserts Pixel/Kyle interlude beats from bcoda-interludes.json into their chapter nodes.
# Places each interlude after the chapter's current last beat (maxSortKey + 1000).

param([switch]$DryRun)

Set-Location "D:\Projects\MindAttic\StreetSamurai"

$jsonPath = "tools\bcoda-interludes.json"
$raw      = [System.IO.File]::ReadAllText($jsonPath, [System.Text.Encoding]::UTF8)
$data     = $raw | ConvertFrom-Json
$list     = $data.interludes

$inserted = 0
$errors   = 0

foreach ($il in $list) {
    $nodeId = $il.chapterId
    $label  = $il.label
    $text   = $il.text

    $wordCount = ($text -split '\s+').Count
    Write-Host "Interlude '$label': $wordCount words -> node $nodeId"

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
    $escapedText = $text.Replace("'", "''")

    $nl  = "`r`n"
    $sql  = 'SET QUOTED_IDENTIFIER ON;' + $nl
    $sql += 'SET ANSI_NULLS ON;' + $nl
    $sql += 'DECLARE @beatId UNIQUEIDENTIFIER = ''' + $newId + ''';' + $nl
    $sql += 'INSERT INTO Beats (Id, [Text], Number, Act, SceneType, Stale, WasCorrected, IsChapterStart, Kind, [Version], EntityStale)' + $nl
    $sql += 'VALUES (@beatId, N''' + $escapedText + ''', ' + $newNumber + ', 1, N''scene'', 0, 0, 0, N''prose'', 0, 0);' + $nl
    $sql += 'INSERT INTO BeatNodes (NodeId, BeatId, SortKey)' + $nl
    $sql += 'VALUES (''' + $nodeId + ''', @beatId, ' + $newSortKey + ');' + $nl
    $sql += 'SELECT ''' + $newId + ''' AS InsertedBeatId;'

    $tmpFile = [System.IO.Path]::Combine([System.IO.Path]::GetTempPath(), "bcoda_interlude.sql")
    [System.IO.File]::WriteAllText($tmpFile, $sql, [System.Text.Encoding]::UTF8)

    $result    = sqlcmd -S "(localdb)\MSSQLLocalDB" -d StreetSamurai -i $tmpFile -f 65001 2>&1
    $resultStr = ($result | Out-String)

    if ($resultStr -match 'Msg \d+') {
        Write-Host "  SQL ERROR '$label': $resultStr"
        $errors++
    } elseif ($LASTEXITCODE -ne 0) {
        Write-Host "  ERROR '$label': exit=$LASTEXITCODE $resultStr"
        $errors++
    } else {
        Write-Host "  -> Inserted '$label' at SortKey $newSortKey"
        $inserted++
    }

    Remove-Item $tmpFile -ErrorAction SilentlyContinue
}

Write-Host ""
Write-Host "Done: $inserted inserted, $errors errors"
