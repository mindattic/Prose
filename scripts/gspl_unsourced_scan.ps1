# Finds paragraphs that invoke an EXTERNAL AUTHORITY but carry no note reference.
#
# GSPL.md §1: nothing is asserted without a source. The failure mode this catches is a
# paragraph that says "Josephus confirms..." or "Pliny records..." or "radiocarbon dated..."
# and then cites nothing - the reader has no way to check it, and in a history book an
# uncheckable claim is indistinguishable from an invented one.
#
# Found for real: MATTHEW ch22 asserted that Josephus "confirms the same basic split" about
# the Sadducees with no citation anywhere in the paragraph. (That paragraph was a duplicate of
# a properly-cited one later in the same chapter, and was cut.)
#
# Deliberately NOT flagged:
#   - Paragraphs discussing the Gospel text itself. Quoting or summarising the book under
#     discussion is the subject matter, not an external claim needing support.
#   - Paragraphs in Notes (they ARE the citations) or the Glossary/Gazetteer, whose entries
#     carry their sourcing at entry level.
#   - Paragraphs whose claim is sourced by a note in the immediately adjacent paragraph of the
#     same beat, which is normal prose practice.
#
# Read-only report.

param([string]$Book = 'all')
$ErrorActionPreference = 'Stop'

$OutputEncoding = [System.Text.UTF8Encoding]::new($false)
[Console]::OutputEncoding = [System.Text.UTF8Encoding]::new($false)
. "$PSScriptRoot\gspl_db.ps1"

$conn = Open-SS
$codes = if ($Book -eq 'all') { @('MATTHEW','MARK','LUKE','JOHN') } else { @($Book.ToUpper()) }

# An ATTRIBUTION - a named authority plus a reporting verb - is a claim the reader must be
# able to check. A bare mention of the words "archaeology" or "excavate" is not: the first pass
# flagged "the historian's tools - inscriptions, administrative records, archaeology", "Not
# every pericope carries a checkable archaeological hook", and Mark 2's point about the Greek
# VERB ("they excavate"). 36 hits, almost all noise. Requiring the reporting verb is what
# caught the one real case: "Josephus... confirms the same basic split", uncited.
$attribution = '\b(Josephus|Philo|Pliny|Tacitus|Suetonius|Cassius Dio|Eusebius|Origen|Strabo|' +
               'the Mishnah|the Talmud|the Didache)\b[^.\[\]]{0,120}\b(records?|reports?|says|' +
               'states?|describes?|confirms?|notes|mentions|attests?|writes|preserves?|places|' +
               'gives|lists|calls)\b'
# Reverse order too: "as Josephus reports" vs "reported by Josephus".
$attribution2 = '\b(according to|as reported by|as recorded by|on the testimony of)\b[^.\[\]]{0,40}\b(Josephus|Philo|Pliny|Tacitus|Suetonius|Cassius Dio|Eusebius|Origen|Strabo)\b'

$total = 0; $flagged = New-Object System.Collections.ArrayList
foreach ($code in $codes) {
    $cm = $conn.CreateCommand()
    $cm.CommandText = @"
SELECT c.Title, bn.SortKey, bt.Text
FROM Nodes c JOIN Nodes p ON p.Id=c.ParentNodeId
JOIN BeatNodes bn ON bn.NodeId=c.Id JOIN Beats bt ON bt.Id=bn.BeatId
WHERE p.NodeCode=@C AND bn.IsEnabled=1 AND c.Title LIKE 'Chapter%'
ORDER BY c.SortKey, bn.SortKey
"@
    [void]$cm.Parameters.AddWithValue('@C', $code)
    $rd = $cm.ExecuteReader()
    $beats = New-Object System.Collections.ArrayList
    while ($rd.Read()) { [void]$beats.Add([pscustomobject]@{ Ch=$rd.GetString(0); T=$rd.GetString(2) }) }
    $rd.Close()

    foreach ($b in $beats) {
        $paras = @($b.T -split "`n`n" | Where-Object { $_.Trim().Length -gt 120 })
        for ($i = 0; $i -lt $paras.Count; $i++) {
            $p = $paras[$i]
            $total++
            if ($p -match '\[\d+\]') { continue }              # sourced here
            # sourced by a neighbour in the same beat?
            $prevOk = ($i -gt 0) -and ($paras[$i-1] -match '\[\d+\]')
            $nextOk = ($i -lt $paras.Count - 1) -and ($paras[$i+1] -match '\[\d+\]')
            # "Then and Now" closing movements are uncited BY DESIGN (GSPL.md §3c: the modern
            # half stays qualitative, because an uncited modern statistic breaks §1 exactly as
            # an invented ancient one does). Skip anything at or after that heading.
            $tanAt = $b.T.IndexOf('Then and Now', [System.StringComparison]::Ordinal)
            if ($tanAt -ge 0 -and $b.T.IndexOf($p, [System.StringComparison]::Ordinal) -gt $tanAt) { continue }

            $m1 = [regex]::Match($p, $attribution)
            $m2 = [regex]::Match($p, $attribution2)
            if (-not $m1.Success -and -not $m2.Success) { continue }
            $quote = if ($m1.Success) { $m1.Value } else { $m2.Value }
            [void]$flagged.Add([pscustomobject]@{
                Book=$code; Ch=$b.Ch; Names=(($quote -replace '\s+',' ').Trim()); Hard=$false
                Adjacent=($prevOk -or $nextOk)
                Snippet=(($p.Substring(0, [Math]::Min(150, $p.Length))) -replace "`n", ' ')
            })
        }
    }
}

$strict = @($flagged | Where-Object { -not $_.Adjacent })
Write-Host ("chapter paragraphs examined            : {0}" -f $total)
Write-Host ("invoke an authority with no note here  : {0}" -f $flagged.Count)
Write-Host ("...and no note in an adjacent paragraph: {0}   <-- these are the real risk" -f $strict.Count)
Write-Host ""
foreach ($f in ($strict | Sort-Object Book, Ch)) {
    Write-Host ("  [{0}/{1}]  names: {2}" -f $f.Book, $f.Ch.Substring(0,[Math]::Min(30,$f.Ch.Length)), $(if($f.Names){$f.Names}else{'(hard claim only)'}))
    Write-Host ("      {0}..." -f $f.Snippet.Trim())
}
$conn.Close()
