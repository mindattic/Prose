# Hugo-mandate stakes-addition revision pass
# 10 surgical text changes across Bushido Coda Chs 1-4
# Idempotent: Replace of absent string = no-op (prints SKIP)

Set-StrictMode -Off
Add-Type -AssemblyName System.Data

$cs = 'Server=(localdb)\MSSQLLocalDB;Database=Prose;Trusted_Connection=True;TrustServerCertificate=True;'
$conn = New-Object System.Data.SqlClient.SqlConnection($cs)
$conn.Open()
$qiCmd = $conn.CreateCommand()
$qiCmd.CommandText = 'SET QUOTED_IDENTIFIER ON'
[void]$qiCmd.ExecuteNonQuery()

function Update-Beat {
    param([string]$BeatId, [string]$Search, [string]$Replace, [string]$Label)
    $r = $conn.CreateCommand()
    $r.CommandText = 'SELECT [Text] FROM Beats WHERE Id = @id'
    [void]$r.Parameters.AddWithValue('@id', $BeatId)
    $txt = [string]$r.ExecuteScalar()
    if (-not $txt) { Write-Host "ERROR ($Label): beat not found"; return }
    # Normalize to CRLF for matching, then try LF as fallback
    $sCRLF = $Search.Replace("`r`n", "`n").Replace("`n", "`r`n")
    $rCRLF = $Replace.Replace("`r`n", "`n").Replace("`n", "`r`n")
    $sLF   = $Search.Replace("`r`n", "`n")
    $rLF   = $Replace.Replace("`r`n", "`n")
    if ($txt.Contains($sCRLF))  { $newTxt = $txt.Replace($sCRLF, $rCRLF) }
    elseif ($txt.Contains($sLF)) { $newTxt = $txt.Replace($sLF,   $rLF) }
    elseif ($txt.Contains($Search)) { $newTxt = $txt.Replace($Search, $Replace) }
    else { Write-Host "SKIP ($Label): search string not found"; return }
    $u = $conn.CreateCommand()
    $u.CommandText = 'UPDATE Beats SET [Text]=@t,[UpdatedAt]=GETUTCDATE(),[Version]=[Version]+1,[Stale]=1 WHERE Id=@id'
    [void]$u.Parameters.AddWithValue('@t', $newTxt)
    [void]$u.Parameters.AddWithValue('@id', $BeatId)
    $n = $u.ExecuteNonQuery()
    Write-Host "OK ($Label): $n row(s) updated"
}

# ─────────────────────────────────────────────────────────────
# CH1: Teeth  (single beat A91EEA4F-0A3C-4C77-8FEE-2D7C8700052F)
# Add ghost-relay sentence after "they always did."
# ─────────────────────────────────────────────────────────────
$s1 = 'It would keep; they always did.'
$r1 = @'
It would keep; they always did.

A relay doesn't route for eleven years after it's been dead six unless somebody's good at killing things on paper and keeping them alive anyway.
'@
Update-Beat -BeatId 'A91EEA4F-0A3C-4C77-8FEE-2D7C8700052F' -Search $s1 -Replace $r1 -Label 'Ch1: relay ghost'

# ─────────────────────────────────────────────────────────────
# CH2: Provenance  (single beat 225BFDE3-970B-4805-879C-5B5FABC99F9C)
# Change 1: array pricing-gap paragraph between Vey line and next ---
# ─────────────────────────────────────────────────────────────
$s2a = @'
a man gets tired."

---
'@
$r2a = @'
a man gets tired."

Kyle took the underlevel stairs slow, the buyers' list in one pocket and the Continuity Office header in the other, and the array did what it always did, costing the morning out in hours and calories and probability bands. It could price a man who got tired, a man who got greedy, a man who ran out of budget. It had no column for the thing that didn't get tired, and he noticed the blank the way you notice a missing key on a ring — not the loss, just the gap where the weight should be — and kept climbing.

---
'@
Update-Beat -BeatId '225BFDE3-970B-4805-879C-5B5FABC99F9C' -Search $s2a -Replace $r2a -Label 'Ch2: array gap'

# Change 2: Rotterdam not-thought sentence before Pixel's coffee
$s2b = @'
from the wrong continent.

Pixel finally handed him the coffee.
'@
$r2b = @'
from the wrong continent.

Someone careful enough to plant it should have been careful enough to hide it better, and he set the thought down on the platform without finishing it and left it there for the next slug.

Pixel finally handed him the coffee.
'@
Update-Beat -BeatId '225BFDE3-970B-4805-879C-5B5FABC99F9C' -Search $s2b -Replace $r2b -Label 'Ch2: Rotterdam not-thought'

# ─────────────────────────────────────────────────────────────
# CH3: The Regular  (single beat 3D80E5C3-1B1B-4A75-AD9B-06076AD159EB)
# Change 1: waiting gap before "The hold came down at 16:40."
# ─────────────────────────────────────────────────────────────
$s3a = 'The hold came down at 16:40.'
$r3a = @'
He spent the gap back near Chen's block, because paper takes hours to do what a blade does in a second, and somebody had to be there for the hours. Her stall stayed dark — she couldn't cook under the hold, and the hold didn't care that it was lunchtime. A man slowed at her empty griddle, read the laminated notice, and walked the two streets over to Damen Authentic instead; Kyle watched him go and didn't move, because there was nothing in the paper yet to put between that man and that door. That was the part the win column never showed: the hold would lift, and the complaints would vanish, and nobody would ever refund Mrs. Chen the bowls she didn't sell while the right desk got around to her.

The hold came down at 16:40.
'@
Update-Beat -BeatId '3D80E5C3-1B1B-4A75-AD9B-06076AD159EB' -Search $s3a -Replace $r3a -Label 'Ch3: waiting gap'

# Change 2: self-in-paper realization at chapter end
$s3b = 'left it, which was exactly enough.'
$r3b = @'
left it, which was exactly enough.

On the stairs it caught up with him, the way the bill always does after the meal. He'd handed Mercer his whole method to win the room — paper, Vey's name, the buyers' list a man only gets one way — and a Tessaline desk would file every word of it, because filing was the thing they were best at. He'd won the building and signed the guest book on the way out; somewhere now there was a sheet with his name on it, and sheets were always copies. He filed that where it went and climbed the rest of the stairs.
'@
Update-Beat -BeatId '3D80E5C3-1B1B-4A75-AD9B-06076AD159EB' -Search $s3b -Replace $r3b -Label 'Ch3: self-in-paper'

# ─────────────────────────────────────────────────────────────
# CH4: The Carousel — individual beat changes
# ─────────────────────────────────────────────────────────────

# Beat 1 (019EDD05-69C9-7A1E-B593-1BF135D23216): "recently discovered" → "discovering"
Update-Beat `
    -BeatId '019EDD05-69C9-7A1E-B593-1BF135D23216' `
    -Search  'accountant who had recently discovered feelings' `
    -Replace 'accountant discovering feelings' `
    -Label   'Ch4 B1: discovering feelings'

# Beat 6 (019EDD05-69F6-70CB-AE76-3CA0BFC5B16E): remove ambiguity parenthetical
Update-Beat `
    -BeatId '019EDD05-69F6-70CB-AE76-3CA0BFC5B16E' `
    -Search  "draining caps, or possibly told to, no instrument in that room could have said which — turned over once." `
    -Replace 'draining caps — turned over once.' `
    -Label   'Ch4 B6: remove parenthetical'

# Beat 8 (019EDD05-69F6-731E-8665-2ABC00D54BA9): remove glass metaphor sentence
# Note: don't start here-strings with 'from' — PS 5.1 query-expression parser conflict
$s8 = @'
added from.

He put the thought down the way you set down a glass you've noticed is cracked — not thrown out, just set, on the shelf with the other things he hadn't finished deciding about.

His terminal beeped
'@
$r8 = @'
added from.

His terminal beeped
'@
Update-Beat -BeatId '019EDD05-69F6-731E-8665-2ABC00D54BA9' -Search $s8 -Replace $r8 -Label 'Ch4 B8: remove glass metaphor'

# Beat 11 (019EDD05-69F6-7345-B327-E75105F00D1B): Sable stillness before "Three separate"
$s11 = '"Three separate monitoring architectures," she said.'
$r11 = @'
She did not reach for the cold tea, and her hands stayed flat on the table, which they never did.

"Three separate monitoring architectures," she said.
'@
Update-Beat -BeatId '019EDD05-69F6-7345-B327-E75105F00D1B' -Search $s11 -Replace $r11 -Label 'Ch4 B11: Sable stillness'

# Beat 12 (019EDD05-69F7-77B3-A7A0-5646D4C30BA0): geometry implication before "He put the page"
$s12 = 'He put the page in the drawer'
$r12 = @'
He stood there a while with the page, because the tap predated the contract, and a map of children converging on that junction was a map of something that moved through the park the way the children did — something the client could not be, because the client was still on the line when they cut it.

He put the page in the drawer
'@
Update-Beat -BeatId '019EDD05-69F7-77B3-A7A0-5646D4C30BA0' -Search $s12 -Replace $r12 -Label 'Ch4 B12: geometry implication'

$conn.Close()
Write-Host "`nDone."
