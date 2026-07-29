param([switch]$Apply)
$ErrorActionPreference = 'Stop'   # fail loudly: a SqlException is otherwise non-terminating

$OutputEncoding = [System.Text.UTF8Encoding]::new($false)
[Console]::OutputEncoding = [System.Text.UTF8Encoding]::new($false)
Add-Type -AssemblyName System.Data

$connStr = "Server=(localdb)\MSSQLLocalDB;Database=StreetSamurai;Trusted_Connection=True;TrustServerCertificate=True;"
$conn = New-Object System.Data.SqlClient.SqlConnection($connStr)
$conn.Open()

function Sha256Hex([string]$text) {
    $sha = [System.Security.Cryptography.SHA256]::Create()
    $bytes = [System.Text.Encoding]::UTF8.GetBytes($text.Trim())
    return ([System.BitConverter]::ToString($sha.ComputeHash($bytes)) -replace '-', '').ToLower()
}

# Abbreviations that end in a period but do NOT end a sentence.
$abbrev = @('ch','chs','vol','vols','trans','ed','eds','p','pp','no','nos','sec','secs','fig',
            'e.g','i.e','cf','St','Dr','Mr','Mrs','Ms','Prof','Rev','Jr','Sr','vs','ca','c',
            'AD','BC','BCE','CE','A.D','B.C','Gen','Ex','Lev','Deut','Isa','Ps','Matt','Mk','Lk','Jn')

function Is-RealSentenceEnd([string]$text, [int]$dotIdx) {
    # dotIdx points at a '.' followed by ' ' + uppercase.
    # Walk back to collect the token immediately before the period.
    $i = $dotIdx - 1
    # Step back over a trailing note/citation reference - "...side [34]." or "...(12)." -
    # otherwise the token walk hits ']' immediately, collects nothing, and rejects a
    # genuine sentence end. Most sentences in this corpus end with a bracketed note.
    if ($i -ge 0 -and ($text[$i] -eq ']' -or $text[$i] -eq ')')) {
        $open = '['
        if ($text[$i] -eq ')') { $open = '(' }
        $j = $i - 1
        while ($j -ge 0 -and $text[$j] -ne $open) { $j-- }
        if ($j -ge 0) { $i = $j - 1 }
        # "...side [34]." leaves us on the space before '['; skip it so the token
        # walk below actually reaches the word.
        while ($i -ge 0 -and $text[$i] -eq ' ') { $i-- }
    }
    $tok = ''
    while ($i -ge 0 -and $text[$i] -match '[A-Za-z\.]') { $tok = $text[$i] + $tok; $i-- }
    if ($tok.Length -eq 0) { return $false }
    if ($tok.Length -eq 1 -and $tok -match '[A-Z]') { return $false }   # initial, e.g. "B. F. Westcott"
    foreach ($a in $abbrev) { if ($tok -ieq $a) { return $false } }
    return $true
}

function Get-SentenceEnds([string]$text) {
    $ends = New-Object System.Collections.Generic.List[int]
    for ($i = 1; $i -lt $text.Length - 2; $i++) {
        $c = $text[$i]
        if ($c -ne '.' -and $c -ne '?' -and $c -ne '!') { continue }
        # allow a closing quote/bracket to follow the stop
        $j = $i + 1
        while ($j -lt $text.Length -and ($text[$j] -eq '"' -or $text[$j] -eq "'" -or $text[$j] -eq ')' -or $text[$j] -eq ']')) { $j++ }
        if ($j + 1 -ge $text.Length) { continue }
        if ($text[$j] -ne ' ') { continue }
        $next = $text[$j + 1]
        if ($next -cnotmatch '[A-Z"]') { continue }
        if ($c -eq '.' -and -not (Is-RealSentenceEnd $text $i)) { continue }
        $ends.Add($j) | Out-Null   # index of the space separating sentences
    }
    return $ends
}

function Split-Wall([string]$text) {
    $target = 850
    $parts = [Math]::Max(2, [Math]::Round($text.Length / $target))
    $ends = Get-SentenceEnds $text
    if ($ends.Count -lt 1) { return $null }
    $chosen = New-Object System.Collections.Generic.List[int]
    for ($k = 1; $k -lt $parts; $k++) {
        $want = [int]($text.Length * $k / $parts)
        $best = -1; $bestDist = [int]::MaxValue
        foreach ($e in $ends) {
            if ($chosen.Contains($e)) { continue }
            # keep segments from getting tiny
            $tooClose = $false
            foreach ($c2 in $chosen) { if ([Math]::Abs($e - $c2) -lt 300) { $tooClose = $true } }
            if ($tooClose) { continue }
            $d = [Math]::Abs($e - $want)
            if ($d -lt $bestDist) { $bestDist = $d; $best = $e }
        }
        if ($best -ge 0 -and $bestDist -lt 1200) { $chosen.Add($best) | Out-Null }
    }
    if ($chosen.Count -eq 0) { return $null }
    $sorted = $chosen | Sort-Object
    $sb = New-Object System.Text.StringBuilder
    $prev = 0
    foreach ($idx in $sorted) {
        $sb.Append($text.Substring($prev, $idx - $prev)) | Out-Null
        $sb.Append("`n`n") | Out-Null
        $prev = $idx + 1     # skip the space
    }
    $sb.Append($text.Substring($prev)) | Out-Null
    return $sb.ToString()
}

$cmd = $conn.CreateCommand()
$cmd.CommandText = @"
SELECT p.NodeCode, c.Title, bt.Id, bt.Text
FROM Nodes c
JOIN Nodes p ON p.Id=c.ParentNodeId
JOIN BeatNodes bn ON bn.NodeId=c.Id
JOIN Beats bt ON bt.Id=bn.BeatId
WHERE p.NodeCode IN ('MATTHEW','MARK','LUKE','JOHN')
  AND c.Title LIKE 'Chapter%'
  AND bn.IsEnabled=1
  AND LEN(bt.Text) > 1800
  AND CHARINDEX(CHAR(10)+CHAR(10), bt.Text) = 0
ORDER BY p.NodeCode, c.SortKey
"@
$rdr = $cmd.ExecuteReader()
$rows = @()
while ($rdr.Read()) {
    $rows += [pscustomobject]@{ Book = $rdr.GetString(0); Chapter = $rdr.GetString(1); Id = $rdr.GetGuid(2); Text = $rdr.GetString(3) }
}
$rdr.Close()
Write-Host "wall beats found: $($rows.Count)"

$done = 0; $skipped = 0
foreach ($r in $rows) {
    $new = Split-Wall $r.Text
    if (-not $new) { Write-Host "  SKIP (no safe break): $($r.Book) $($r.Chapter)"; $skipped++; continue }
    $breaks = ([regex]::Matches($new, "`n`n")).Count
    if ($Apply) {
        $c2 = $conn.CreateCommand()
        $c2.CommandText = "UPDATE Beats SET Text=@T, TextHash=@H, UpdatedAt=SYSUTCDATETIME() WHERE Id=@Id"
        $c2.Parameters.AddWithValue("@T", $new) | Out-Null
        $c2.Parameters.AddWithValue("@H", (Sha256Hex $new)) | Out-Null
        $c2.Parameters.AddWithValue("@Id", $r.Id) | Out-Null
        $c2.ExecuteNonQuery() | Out-Null
    }
    $done++
    Write-Host ("  {0} {1} | {2} chars -> {3} paragraphs" -f $r.Book, $r.Chapter.Substring(0,[Math]::Min(28,$r.Chapter.Length)), $r.Text.Length, ($breaks+1))
    if (-not $Apply) {
        foreach ($m in [regex]::Matches($new, "`n`n(.{0,70})")) {
            Write-Host ("       break before: " + ($m.Groups[1].Value -replace "`n"," "))
        }
    }
}
$conn.Close()
if ($Apply) { Write-Host "APPLIED to $done beats (skipped $skipped)" } else { Write-Host "DRY RUN: would change $done beats (skipped $skipped). Re-run with -Apply" }
