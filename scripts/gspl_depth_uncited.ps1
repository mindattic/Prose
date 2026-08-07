$ErrorActionPreference = 'Stop'   # fail loudly: a SqlException is otherwise non-terminating
$OutputEncoding = [System.Text.UTF8Encoding]::new($false)
[Console]::OutputEncoding = [System.Text.UTF8Encoding]::new($false)
Add-Type -AssemblyName System.Data

$conn = New-Object System.Data.SqlClient.SqlConnection("Server=(localdb)\MSSQLLocalDB;Database=Prose;Trusted_Connection=True;TrustServerCertificate=True;")
$conn.Open()
$em = [char]8212

function Sha256Hex([string]$t) {
    $s = [System.Security.Cryptography.SHA256]::Create()
    return ([System.BitConverter]::ToString($s.ComputeHash([System.Text.Encoding]::UTF8.GetBytes($t.Trim()))) -replace '-','').ToLower()
}
function Exec-NonQuery([string]$sql, [hashtable]$p) {
    $c = $conn.CreateCommand(); $c.CommandText = $sql
    foreach ($k in $p.Keys) { $c.Parameters.AddWithValue("@$k", $p[$k]) | Out-Null }
    $c.ExecuteNonQuery() | Out-Null
}
function Exec-Scalar([string]$sql) { $c = $conn.CreateCommand(); $c.CommandText = $sql; return $c.ExecuteScalar() }

function Next-Num([string]$notes) {
    return [int](Exec-Scalar @"
SELECT ISNULL(MAX(CAST(LEFT(b.Text, CHARINDEX(' ',b.Text)-1) AS INT)),0)
FROM BeatNodes bn JOIN Beats b ON b.Id=bn.BeatId
WHERE bn.NodeId='$notes' AND bn.IsEnabled=1
  AND CHARINDEX(' ', b.Text) > 1 AND LEFT(b.Text, CHARINDEX(' ',b.Text)-1) NOT LIKE '%[^0-9]%'
"@)
}
function Add-Note([string]$notes, [int]$num, [string]$title, [string]$body) {
    $sk = [double](Exec-Scalar "SELECT ISNULL(MAX(bn.SortKey),0)+50 FROM BeatNodes bn WHERE bn.NodeId='$notes'")
    $bn = [int](Exec-Scalar "SELECT MAX(Number) FROM Beats") + 1
    $text = "$num $em $title" + "`n`n" + $body.Trim()
    $id = [guid]::NewGuid()
    Exec-NonQuery "INSERT INTO Beats (Id, Text, TextHash, Act, SceneType, Kind, Number, Stale, WasCorrected, IsChapterStart, Version, EntityStale, CreatedAt, UpdatedAt) VALUES (@Id, @Text, @Hash, 0, 'scene', 'prose', @Number, 0, 0, 0, 0, 0, SYSUTCDATETIME(), SYSUTCDATETIME())" @{ Id = $id; Text = $text; Hash = (Sha256Hex $text); Number = $bn }
    Exec-NonQuery "INSERT INTO BeatNodes (NodeId, BeatId, SortKey, IsEnabled) VALUES (@NodeId, @BeatId, @SortKey, 1)" @{ NodeId = [guid]$notes; BeatId = $id; SortKey = $sk }
    Write-Host "  note $num"
}
function Cite([string]$chapterNode, [string]$find, [string]$replace) {
    $c = $conn.CreateCommand()
    $c.CommandText = "SELECT b.Id, b.Text FROM BeatNodes bn JOIN Beats b ON b.Id=bn.BeatId WHERE bn.NodeId=@N AND bn.IsEnabled=1"
    $c.Parameters.AddWithValue("@N", [guid]$chapterNode) | Out-Null
    $r = $c.ExecuteReader(); $rows = @()
    while ($r.Read()) { $rows += [pscustomobject]@{ Id = $r.GetGuid(0); Text = $r.GetString(1) } }
    $r.Close()
    foreach ($x in $rows) {
        if ($x.Text.IndexOf($find) -ge 0) {
            $new = $x.Text.Replace($find, $replace)
            Exec-NonQuery "UPDATE Beats SET Text=@T, TextHash=@H, UpdatedAt=SYSUTCDATETIME() WHERE Id=@Id" @{ T = $new; H = (Sha256Hex $new); Id = $x.Id }
            Write-Host "  cited: $($find.Substring(0,[Math]::Min(46,$find.Length)))..."
            return
        }
    }
    Write-Host "  !! NOT FOUND: $($find.Substring(0,[Math]::Min(60,$find.Length)))..."
}

$MkNotes = "019FA968-1B3B-75DC-84CF-0C7D9C4E783C"
$MtNotes = "019FA01D-FA22-76C6-976C-3EA4F4D54A14"
$Mk2  = "019FA967-0D77-73B8-A0B4-BA4423DF5219"
$Mk12 = "019FA967-B4EF-7F1B-B44A-506365CDE94A"
$Mt1  = "019FA049-5D94-766F-A919-4623FD605028"
$Mt28 = "019FA078-392B-7408-B4A9-4CA5E15931F7"

# ---------- MARK ----------
$k = Next-Num $MkNotes
$nShabbat = $k + 1
Add-Note $MkNotes $nShabbat "The thirty-nine categories, and their date" @"
Mishnah, Shabbat 7:2. The tractate lists the primary categories of labour forbidden on the Sabbath as "forty less one," enumerating them in groups that begin with the agricultural sequence $em sowing, plowing, reaping, binding sheaves, threshing, winnowing, selecting, grinding, sifting, kneading, baking $em before moving to textile, writing, building, and carrying work. Each primary category (av) subdivides into derivative labours (toladot). Reaping is on the list, which is why plucking grain could be argued about at all. The date matters as much as the content: the Mishnah was redacted around 200 CE, roughly two centuries after the scene Mark describes, so it records where the rabbinic definitions had settled rather than what was necessarily agreed in Galilee in the 20s.
"@
$nJudas = $nShabbat + 1
Add-Note $MkNotes $nJudas "Josephus on Judas, Zadok, and the tax that started it" @"
Josephus, Antiquities of the Jews, Book 18, ch. 1, sections 1 and 6 (= sections 4-10 and 23 Niese), trans. William Whiston. Josephus reports that Judas $em whom he calls a Gaulanite, from Gamala $em together with a Pharisee named Zadok, urged revolt at the time of the census under Quirinius, denouncing the taxation as "nothing but an introduction to slavery" and calling on the nation to reassert its freedom. Josephus classes their movement as a "fourth philosophy" alongside the Pharisees, Sadducees, and Essenes: agreeing with the Pharisees in most things but "intensely devoted to freedom," holding God alone as ruler, and prepared to accept any death rather than call a man lord.
"@

# ---------- MATTHEW ----------
$m = Next-Num $MtNotes
$nAfricanus = $m + 1
Add-Note $MtNotes $nAfricanus "The levirate solution, and who first proposed it" @"
Eusebius of Caesarea, Ecclesiastical History, Book 1, ch. 7, quoting at length the letter of Julius Africanus (c. 160-c. 240) to Aristides. Africanus reconciles the two genealogies by levirate marriage: Matthan, of Solomon's line, fathered Jacob; after Matthan's death Melchi, of Nathan's line, fathered Eli by the same woman, making Eli and Jacob brothers by one mother. Eli died childless, so Jacob raised up seed to him, and Joseph was therefore Jacob's son by nature and Eli's by law $em which is how one man can appear with two different fathers. Africanus states that he had the explanation from the surviving relatives of Jesus, who he says preserved the family record by memory after Herod destroyed the public genealogical archives. This is the third-century origin of the levirate solution, not a modern harmonisation.
"@
$nEclipse = $nAfricanus + 1
Add-Note $MtNotes $nEclipse "The one eclipse in all of Josephus" @"
Josephus, Antiquities of the Jews, Book 17, ch. 6, sections 2-4 (= sections 149-167 Niese). Josephus recounts that Herod had two teachers, Matthias and Judas, burned alive for inciting a crowd to tear down the golden eagle from the Temple, and that "on that very night there was an eclipse of the moon." Herod's death and the following Passover come shortly after. This is the only lunar eclipse mentioned anywhere in Josephus's works, which is why it carries so much chronological weight. It is conventionally identified with the eclipse of 13 March 4 BCE, though that identification is contested $em the 4 BCE event was a minor partial eclipse, and the sequence of events Josephus places between the eclipse and Passover is difficult to fit into the available weeks, which is why alternative identifications have been proposed.
"@
$nWomen = $nEclipse + 1
Add-Note $MtNotes $nWomen "Josephus on women as witnesses" @"
Josephus, Antiquities of the Jews, Book 4, ch. 8, section 15 (= section 219 Niese), trans. William Whiston. In a passage setting out who may serve as a legal witness, Josephus writes: "let not the testimony of women be admitted, on account of the levity and boldness of their sex." It is a dated, non-Christian primary source contemporary with the Gospels, and it is the single most direct evidence for the legal disadvantage under which a woman's testimony stood $em which is what gives the resurrection accounts' choice of first witnesses its evidentiary interest. The restriction was real but not absolute; rabbinic sources preserve recognised exceptions on matters within a woman's own direct experience.
"@

# ---------- attach ----------
Write-Host "Attaching citations..."
Cite $Mk2  "at Mishnah Shabbat 7:2, was not compiled until roughly two centuries after this scene" "at Mishnah Shabbat 7:2 [$nShabbat], was not compiled until roughly two centuries after this scene"
Cite $Mk12 "Josephus records that Judas, together with a Pharisee named Zadok, denounced the census as" "Josephus records [$nJudas] that Judas, together with a Pharisee named Zadok, denounced the census as"
Cite $Mt1  "Eusebius's Ecclesiastical History, under which Heli and Jacob were half-brothers by the same mother" "Eusebius's Ecclesiastical History [$nAfricanus], under which Heli and Jacob were half-brothers by the same mother"
Cite $Mt1  "Josephus records it, sometime after a lunar eclipse and before that year's Passover" "Josephus records it, sometime after a lunar eclipse and before that year's Passover [$nEclipse]"
Cite $Mt28 "Josephus states plainly, writing in his own voice, that women's testimony was not to be counted credible" "Josephus states plainly, writing in his own voice [$nWomen], that women's testimony was not to be counted credible"

$conn.Close()
Write-Host "DONE"
