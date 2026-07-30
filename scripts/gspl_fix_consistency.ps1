# Fixes the three genuine defects found by gspl_consistency_scan.ps1.
#
# 1. CITATION PLACEMENT (29). Corpus convention, by 1,766 instances to 29, is the note marker
#    BEFORE the sentence stop: "...their claimed scale [20]. A three-hour..." The 29 outliers
#    (25 of them in MATTHEW, from the orphan-note attachment pass that appended the marker
#    after the sentence it belonged to) read "...scale. [20] A three-hour..."
#
# 2. MISSING DASHES IN MARK CHAPTER 2 (3). Three places have a double space where punctuation
#    was lost, orphaning a quotation: 'off them  "let not your fasts...week"  recommending'.
#    Both halves of a parenthetical em-dash pair, plus one more, are simply gone.
#
# 3. ERA STYLE (17). The series uses BCE/CE in body prose (79-337 uses per book) but 17 body
#    passages use BC/AD - 16 of them in JOHN, concentrated in its chapters 2-3 and apparatus,
#    which matches the earlier unrecorded session that wrote JOHN ch.1-3.
#    NOT a blind swap: "AD 70" must become "70 CE", not "CE 70".
#    NOT applied inside cited titles - Schurer's "(175 B.C.-A.D. 135)" and Goldsworthy's
#    "100 BC-AD 200" are real book titles and must stay exactly as published. 10 such matches
#    are deliberately skipped by the title guard.
#
# Deliberately NOT changed, verified legitimate: "in In Flaccum" (preposition + Philo's title),
# "Wars of the Jews" (Whiston's translation is titled that), Paneas/Panias/Banias (the books
# explain ancient vs modern names), and the orphan "]" in LUKE's Pilate Stone note, which is
# epigraphic lacuna notation.
#
# Dry-run by default; -Apply to write.

param([switch]$Apply)
$ErrorActionPreference = 'Stop'

$OutputEncoding = [System.Text.UTF8Encoding]::new($false)
[Console]::OutputEncoding = [System.Text.UTF8Encoding]::new($false)
. "$PSScriptRoot\gspl_db.ps1"

$conn = Open-SS
$em = [char]8212

# A BC/AD match sitting inside a bibliographic title must be left alone: those are published
# titles and altering them misquotes the source.
#
# Must look BOTH WAYS. A backward-only guard missed four RINAP volumes, whose titles embed the
# reign dates - "The Royal Inscriptions of Esarhaddon, King of Assyria (680-669 BC), RINAP 4
# (Winona Lake, IN: Eisenbrauns...)" - because the publisher marker follows the match instead
# of preceding it. A dry run showed those four about to be silently rewritten.
$titleGuard = '(trans\.|ed\.\)|Rev\. ed|Oxford|Cambridge|Press|Monographs|Library|Loeb|' +
              'University|Anchor Bible|Doubleday|HarperOne|Brill|Eerdmans|Winona Lake|' +
              'Eisenbrauns|RINAP|Royal Inscriptions|The History of|The Roman Army|' +
              'vol\.|ISBN|\(\d{3,4} B)'
$GUARD_BACK = 150
$GUARD_FWD  = 150

function Fix-EraStyle([string]$t, [ref]$count) {
    # Compound forms first, so "50 BC-AD 50" is not half-converted by a simpler rule.
    $rules = @(
        @{ p = '\b(\d+)\s*BC\s*-\s*AD\s*(\d+)';        r = '$1 BCE-$2 CE' }
        @{ p = '\bAD\s*(\d+)\s*-\s*(\d+)';             r = '$1-$2 CE' }
        @{ p = '\bAD\s*(\d+)\s*/\s*(\d+)';             r = '$1/$2 CE' }
        @{ p = '\bAD\s*(\d+)\s+or\s+(\d+)';            r = '$1 or $2 CE' }
        @{ p = '\bAD\s*(\d+)s\b';                      r = '$1s CE' }
        @{ p = '\bAD\s*(\d+)\b';                       r = '$1 CE' }
        # Reversed order: "70 AD", "(23-79 AD)". Written this way in a few places, and the
        # AD-first rules above cannot see them.
        @{ p = '\b(\d+)\s*-\s*(\d+)\s+AD\b';           r = '$1-$2 CE' }
        @{ p = '\b(\d+)\s+AD\b';                       r = '$1 CE' }
        # Worded form: "the first century BC to the first century AD".
        @{ p = '\bcentury\s+AD\b';                     r = 'century CE' }
        @{ p = '\bcentury\s+BC\b(?!\.?E)';             r = 'century BCE' }
        # (?!\.?E) so "63 B.C.E.-70 C.E." is not matched as a bare "BC".
        @{ p = '\b(\d+(?:\s*/\s*\d+)?)\s*BC\b(?!\.?E)'; r = '$1 BCE' }
    )
    foreach ($rule in $rules) {
        $t = [regex]::Replace($t, $rule.p, {
            param($m)
            $s = [Math]::Max(0, $m.Index - $GUARD_BACK)
            $len = [Math]::Min($GUARD_BACK + $m.Length + $GUARD_FWD, $t.Length - $s)
            $ctx = $t.Substring($s, $len)
            if ($ctx -match $titleGuard) { return $m.Value }   # inside a cited title: leave it
            $count.Value++
            return [regex]::Replace($m.Value, $rule.p, $rule.r)
        })
    }
    return $t
}

$cm = $conn.CreateCommand()
$cm.CommandText = @"
SELECT p.NodeCode, c.Title, bt.Id, bt.Text
FROM Nodes c JOIN Nodes p ON p.Id=c.ParentNodeId
JOIN BeatNodes bn ON bn.NodeId=c.Id JOIN Beats bt ON bt.Id=bn.BeatId
WHERE p.NodeCode IN ('MATTHEW','MARK','LUKE','JOHN') AND bn.IsEnabled=1
"@
$rd = $cm.ExecuteReader()
$rows = New-Object System.Collections.ArrayList
while ($rd.Read()) {
    [void]$rows.Add([pscustomobject]@{ C=$rd.GetString(0); Ch=$rd.GetString(1); Id=$rd.GetGuid(2); T=$rd.GetString(3) })
}
$rd.Close()

$totCite = 0; $totDash = 0; $totEra = 0; $touched = 0
foreach ($r in $rows) {
    $t = $r.T
    $orig = $t

    # 1. marker before the stop
    $c1 = 0
    # Lookahead must allow a newline: 8 of the 29 sit at the end of a paragraph, where the
    # marker is followed by "\n\n" rather than a space.
    #
    # Group 1 is [^\s.!?], NOT \S. With \S this matched the final dot of an ELLIPSIS and
    # mangled a quotation: '"Blessed are you poor... [24] woe' became 'poor.. [24]. woe'.
    # Excluding a preceding stop character means an ellipsis can never match, because its last
    # dot is itself preceded by a dot. Caught by re-scanning after the first apply; one beat
    # was damaged and restored from temporal history.
    $t = [regex]::Replace($t, '([^\s.!?])([.!?])[ \t]+(\[\d+\])(?=[ \t\r\n]|$)', { param($m); $script:c1++; "$($m.Groups[1].Value) $($m.Groups[3].Value)$($m.Groups[2].Value)" })

    # 2. MARK ch2 lost dashes
    $c2 = 0
    if ($r.C -eq 'MARK' -and $r.Ch -like 'Chapter 2*') {
        $t = [regex]::Replace($t, '(\S)  (\S)', { param($m); $script:c2++; "$($m.Groups[1].Value) $em $($m.Groups[2].Value)" })
    }

    # 3. era style
    $c3 = 0
    $t = Fix-EraStyle $t ([ref]$c3)

    if ($t -eq $orig) { continue }
    $touched++; $totCite += $c1; $totDash += $c2; $totEra += $c3
    $ch = $r.Ch; if ($ch.Length -gt 26) { $ch = $ch.Substring(0,26) }
    Write-Host ("  {0,-8} {1,-27} cite={2} dash={3} era={4}" -f $r.C, $ch, $c1, $c2, $c3)

    if ($Apply) {
        [void](Invoke-SSNonQuery $conn "UPDATE Beats SET Text=@T, TextHash=@H, UpdatedAt=SYSUTCDATETIME() WHERE Id=@Id" `
            @{ T=$t; H=(Get-SSSha256Hex $t); Id=$r.Id } -Expect 1 -What "$($r.C) $ch")
    }
}

Write-Host ""
Write-Host ("beats touched          : {0}" -f $touched)
Write-Host ("citation placements    : {0}" -f $totCite)
Write-Host ("lost dashes restored   : {0}" -f $totDash)
Write-Host ("era-style corrections  : {0}" -f $totEra)
Write-Host ""
if ($Apply) { Write-Host "APPLIED." } else { Write-Host "DRY RUN - nothing written. Re-run with -Apply." }
$conn.Close()
