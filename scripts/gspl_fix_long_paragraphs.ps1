# Splits individual PARAGRAPHS over $Threshold chars.
#
# Why this exists alongside gspl_fix_paragraph_walls.ps1: that script tests
#   CHARINDEX(CHAR(10)+CHAR(10), Text) = 0
# at the BEAT level, so a beat shaped "heading \n\n 3199-char block" looked
# already-divided and was skipped. The wall was in a paragraph, not the beat.
# This pass walks every line of every enabled beat instead.
#
# Reuses the sentence-boundary + abbreviation guard from gspl_fix_paragraph_walls.ps1.
# Dry-run by default; pass -Apply to write.

param([switch]$Apply, [int]$Threshold = 1800, [int]$ShowBreaks = 0)

$OutputEncoding = [System.Text.UTF8Encoding]::new($false)
[Console]::OutputEncoding = [System.Text.UTF8Encoding]::new($false)
Add-Type -AssemblyName System.Data

$conn = New-Object System.Data.SqlClient.SqlConnection("Server=(localdb)\MSSQLLocalDB;Database=StreetSamurai;Trusted_Connection=True;TrustServerCertificate=True;")
$conn.Open()

function Sha256Hex([string]$text) {
    $sha = [System.Security.Cryptography.SHA256]::Create()
    return ([System.BitConverter]::ToString($sha.ComputeHash([System.Text.Encoding]::UTF8.GetBytes($text.Trim()))) -replace '-', '').ToLower()
}

$abbrev = @('ch','chs','vol','vols','trans','ed','eds','p','pp','no','nos','sec','secs','fig',
            'e.g','i.e','cf','St','Dr','Mr','Mrs','Ms','Prof','Rev','Jr','Sr','vs','ca','c',
            'AD','BC','BCE','CE','A.D','B.C','Gen','Ex','Lev','Deut','Isa','Ps','Matt','Mk','Lk','Jn')

function Is-RealSentenceEnd([string]$text, [int]$dotIdx) {
    $i = $dotIdx - 1
    # Step back over a trailing note/citation reference - "...side [34]." or "...(12)." -
    # otherwise the token walk below hits ']' immediately, collects nothing, and rejects
    # a genuine sentence end. Most sentences in this corpus end with a bracketed note.
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
    if ($tok.Length -eq 1 -and $tok -match '[A-Z]') { return $false }
    foreach ($a in $abbrev) { if ($tok -ieq $a) { return $false } }
    return $true
}

function Get-SentenceEnds([string]$text) {
    $ends = New-Object System.Collections.Generic.List[int]
    for ($i = 1; $i -lt $text.Length - 2; $i++) {
        $c = $text[$i]
        if ($c -ne '.' -and $c -ne '?' -and $c -ne '!') { continue }
        $j = $i + 1
        while ($j -lt $text.Length -and ($text[$j] -eq '"' -or $text[$j] -eq "'" -or $text[$j] -eq ')' -or $text[$j] -eq ']')) { $j++ }
        if ($j + 1 -ge $text.Length) { continue }
        if ($text[$j] -ne ' ') { continue }
        $next = $text[$j + 1]
        if ($next -cnotmatch '[A-Z"]') { continue }
        if ($c -eq '.' -and -not (Is-RealSentenceEnd $text $i)) { continue }
        [void]$ends.Add($j)
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
            $tooClose = $false
            foreach ($c2 in $chosen) { if ([Math]::Abs($e - $c2) -lt 300) { $tooClose = $true } }
            if ($tooClose) { continue }
            $d = [Math]::Abs($e - $want)
            if ($d -lt $bestDist) { $bestDist = $d; $best = $e }
        }
        if ($best -ge 0 -and $bestDist -lt 1200) { [void]$chosen.Add($best) }
    }
    if ($chosen.Count -eq 0) { return $null }
    $sb = New-Object System.Text.StringBuilder
    $prev = 0
    foreach ($idx in ($chosen | Sort-Object)) {
        [void]$sb.Append($text.Substring($prev, $idx - $prev))
        [void]$sb.Append("`n`n")
        $prev = $idx + 1
    }
    [void]$sb.Append($text.Substring($prev))
    return $sb.ToString()
}

$cmd = $conn.CreateCommand()
$cmd.CommandText = @"
SELECT p.NodeCode, c.Title, bt.Id, bt.Text
FROM Nodes c
JOIN Nodes p ON p.Id = c.ParentNodeId
JOIN BeatNodes bn ON bn.NodeId = c.Id
JOIN Beats bt ON bt.Id = bn.BeatId
WHERE p.NodeCode IN ('MATTHEW','MARK','LUKE','JOHN')
  AND bn.IsEnabled = 1
ORDER BY p.NodeCode, c.SortKey
"@
$rdr = $cmd.ExecuteReader()
$rows = @()
while ($rdr.Read()) {
    $rows += [pscustomobject]@{ Book = $rdr.GetString(0); Chapter = $rdr.GetString(1); Id = $rdr.GetGuid(2); Text = $rdr.GetString(3) }
}
$rdr.Close()

$touched = 0; $paras = 0; $skipped = 0
$perBook = @{}
foreach ($r in $rows) {
    $lines = $r.Text -split "`n"
    $changed = $false
    for ($i = 0; $i -lt $lines.Count; $i++) {
        if ($lines[$i].Length -le $Threshold) { continue }
        $new = Split-Wall $lines[$i]
        if (-not $new) {
            Write-Host ("  SKIP (no safe break): {0} {1} [{2} chars]" -f $r.Book, $r.Chapter, $lines[$i].Length)
            $skipped++
            continue
        }
        $before = $lines[$i].Length
        if ($ShowBreaks -gt 0) {
            $ShowBreaks--
            Write-Host ("  --- break points for {0} {1}:" -f $r.Book, $r.Chapter)
            foreach ($bm in [regex]::Matches($new, "`n`n")) {
                $l = $new.Substring([Math]::Max(0, $bm.Index - 70), [Math]::Min(70, $bm.Index))
                $rr = $new.Substring($bm.Index + 2, [Math]::Min(70, $new.Length - $bm.Index - 2))
                Write-Host ("      ...{0}  //  {1}..." -f $l, $rr)
            }
        }
        $lines[$i] = $new
        $changed = $true; $paras++
        if (-not $perBook.ContainsKey($r.Book)) { $perBook[$r.Book] = 0 }
        $perBook[$r.Book]++
        Write-Host ("  {0,-8} {1,-42} {2} chars -> {3} paragraphs" -f $r.Book, $r.Chapter.Substring(0, [Math]::Min(42, $r.Chapter.Length)), $before, (([regex]::Matches($new, "`n`n")).Count + 1))
    }
    if (-not $changed) { continue }
    $newText = ($lines -join "`n")
    # collapse any run of 3+ newlines the join may have produced
    $newText = [regex]::Replace($newText, "`n{3,}", ("`n" + "`n")).Trim()
    $touched++
    if ($Apply) {
        $c2 = $conn.CreateCommand()
        $c2.CommandText = "UPDATE Beats SET Text=@T, TextHash=@H, UpdatedAt=SYSUTCDATETIME() WHERE Id=@Id"
        [void]$c2.Parameters.AddWithValue("@T", $newText)
        [void]$c2.Parameters.AddWithValue("@H", (Sha256Hex $newText))
        [void]$c2.Parameters.AddWithValue("@Id", $r.Id)
        [void]$c2.ExecuteNonQuery()
    }
}

Write-Host ""
Write-Host ("threshold      : {0} chars" -f $Threshold)
Write-Host ("beats touched  : {0}" -f $touched)
Write-Host ("paragraphs split: {0}" -f $paras)
Write-Host ("skipped        : {0}" -f $skipped)
foreach ($k in ($perBook.Keys | Sort-Object)) { Write-Host ("  {0,-8} {1}" -f $k, $perBook[$k]) }
Write-Host ""
if ($Apply) { Write-Host "APPLIED." } else { Write-Host "DRY RUN - nothing written. Re-run with -Apply." }

$conn.Close()
