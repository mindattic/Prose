# bcoda-insert-chapter.ps1
# Usage: .\bcoda-insert-chapter.ps1 -ChapterId "<guid>" -ChapterTitle "<title>" -ChapterText "<full prose text>"
# Inserts a single beat (the full chapter prose) into ChapterBeats.
# Uses a temp .sql file to avoid em-dash/encoding issues with inline -Q.

param(
    [Parameter(Mandatory=$true)][string]$ChapterId,
    [Parameter(Mandatory=$true)][string]$ChapterTitle,
    [Parameter(Mandatory=$true)][string]$ChapterText,
    [int]$BeatIndex = 1,
    [float]$SortKey = 1000.0,
    [int]$Act = 1
)

$synopsis = ($ChapterText -split ' ' | Select-Object -First 30) -join ' '
if ($synopsis.Length -gt 400) { $synopsis = $synopsis.Substring(0, 397) + '...' }

# Escape single quotes for SQL
$escapedText = $ChapterText -replace "'", "''"
$escapedTitle = $ChapterTitle -replace "'", "''"
$escapedSynopsis = $synopsis -replace "'", "''"

$sql = @"
INSERT INTO ChapterBeats
    (BeatGuid, ChapterId, [Index], Title, Synopsis, [Text], Act, SortKey, WasCorrected)
VALUES (
    NEWID(),
    '$ChapterId',
    $BeatIndex,
    N'$escapedTitle',
    N'$escapedSynopsis',
    N'$escapedText',
    $Act,
    $SortKey,
    0
);
SELECT SCOPE_IDENTITY() AS InsertedId;
"@

$tmpFile = [System.IO.Path]::GetTempFileName() + ".sql"
[System.IO.File]::WriteAllText($tmpFile, $sql, [System.Text.Encoding]::UTF8)

try {
    $result = sqlcmd -S "(localdb)\MSSQLLocalDB" -d StreetSamurai -i $tmpFile -f 65001
    Write-Host "Inserted chapter beat for: $ChapterTitle"
    Write-Host $result
} finally {
    Remove-Item $tmpFile -ErrorAction SilentlyContinue
}
