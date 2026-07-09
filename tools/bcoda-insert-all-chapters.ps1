# bcoda-insert-all-chapters.ps1
# Reads the workflow output JSON, inserts each chapter as a ChapterBeat.

param(
    [string]$OutputFile = "C:\Users\ryand\AppData\Local\Temp\claude\D--Projects-MindAttic-StreetSamurai\d758f1a3-ce3a-42e4-8773-c2129bb39a82\tasks\we6d759s9.output",
    [switch]$DryRun
)

Write-Host "Reading workflow output..."
$raw = [System.IO.File]::ReadAllText($OutputFile, [System.Text.Encoding]::UTF8)

$parsed   = $raw | ConvertFrom-Json
$chapters = $parsed.result
Write-Host "Found $($chapters.Count) chapters"

$inserted = 0
$errors = 0

foreach ($ch in $chapters) {
    $num   = $ch.num
    $title = $ch.title
    $id    = $ch.id
    $text  = $ch.text

    if ([string]::IsNullOrWhiteSpace($text)) {
        Write-Host "SKIP Ch${num} '$title' -- empty text"
        continue
    }

    $wordCount = ($text -split '\s+').Count
    Write-Host "Ch${num} '$title': $wordCount words"

    if ($DryRun) { continue }

    $synopsis = (($text -split '\s+') | Select-Object -First 40) -join ' '
    if ($synopsis.Length -gt 390) { $synopsis = $synopsis.Substring(0, 387) + '...' }

    $escapedText     = $text.Replace("'", "''")
    $escapedTitle    = ("Ch${num}: " + $title).Replace("'", "''")
    $escapedSynopsis = $synopsis.Replace("'", "''")

    $insertSql  = "INSERT INTO ChapterBeats (BeatGuid, ChapterId, [Index], Title, Synopsis, [Text], Act, SortKey, WasCorrected, StructureRole, SceneType) VALUES (NEWID(), '" + $id + "', 1, N'" + $escapedTitle + "', N'" + $escapedSynopsis + "', N'" + $escapedText + "', 1, 1000.0, 0, N'', N'scene');"
    $selectSql  = "SELECT SCOPE_IDENTITY() AS InsertedId;"
    $fullSql    = $insertSql + [System.Environment]::NewLine + $selectSql

    $tmpFile = [System.IO.Path]::Combine([System.IO.Path]::GetTempPath(), "bcoda_ch${num}.sql")
    [System.IO.File]::WriteAllText($tmpFile, $fullSql, [System.Text.Encoding]::UTF8)

    $result = sqlcmd -S "(localdb)\MSSQLLocalDB" -d StreetSamurai -i $tmpFile -f 65001 2>&1
    $resultStr = ($result | Out-String)
    if ($resultStr -match 'Msg \d+') {
        Write-Host "  SQL ERROR Ch${num}: $resultStr"
        $errors++
    } elseif ($LASTEXITCODE -ne 0) {
        Write-Host "  ERROR Ch${num}: $resultStr"
        $errors++
    } else {
        Write-Host "  -> Inserted Ch${num}"
        $inserted++
    }
    Remove-Item $tmpFile -ErrorAction SilentlyContinue
}

Write-Host ""
Write-Host "Done: $inserted inserted, $errors errors"
