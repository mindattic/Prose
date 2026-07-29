$OutputEncoding = [System.Text.UTF8Encoding]::new($false)
[Console]::OutputEncoding = [System.Text.UTF8Encoding]::new($false)
Add-Type -AssemblyName System.Data

$conn = New-Object System.Data.SqlClient.SqlConnection("Server=(localdb)\MSSQLLocalDB;Database=StreetSamurai;Trusted_Connection=True;TrustServerCertificate=True;")
$conn.Open()
$em = [char]8212
$deg = [char]176

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
function Next-Note([string]$notes) {
    return [int](Exec-Scalar @"
SELECT ISNULL(MAX(CAST(LEFT(bt.Text, CHARINDEX(' ',bt.Text)-1) AS INT)),0)
FROM BeatNodes bn JOIN Beats bt ON bt.Id=bn.BeatId
WHERE bn.NodeId='$notes' AND bn.IsEnabled=1
  AND CHARINDEX(' ', bt.Text) > 1 AND LEFT(bt.Text, CHARINDEX(' ',bt.Text)-1) NOT LIKE '%[^0-9]%'
"@) + 1
}
function Add-Note([string]$notes, [int]$num, [string]$title, [string]$body) {
    $sk = [double](Exec-Scalar "SELECT ISNULL(MAX(bn.SortKey),0)+50 FROM BeatNodes bn WHERE bn.NodeId='$notes'")
    $bnum = [int](Exec-Scalar "SELECT MAX(Number) FROM Beats") + 1
    $id = [guid]::NewGuid()
    $text = "$num $em $title" + "`n`n" + $body.Trim()
    Exec-NonQuery "INSERT INTO Beats (Id, Text, TextHash, Act, SceneType, Kind, Number, Stale, WasCorrected, IsChapterStart, Version, EntityStale, CreatedAt, UpdatedAt) VALUES (@Id, @Text, @Hash, 0, 'scene', 'prose', @Number, 0, 0, 0, 0, 0, SYSUTCDATETIME(), SYSUTCDATETIME())" @{ Id = $id; Text = $text; Hash = (Sha256Hex $text); Number = $bnum }
    Exec-NonQuery "INSERT INTO BeatNodes (NodeId, BeatId, SortKey, IsEnabled) VALUES (@N, @B, @S, 1)" @{ N = [guid]$notes; B = $id; S = $sk }
}
function Append-ToEvangelistChapter([string]$code, [string]$extra) {
    $c = $conn.CreateCommand()
    $c.CommandText = @"
SELECT bt.Id, bt.Text FROM Nodes c
JOIN Nodes p ON p.Id=c.ParentNodeId
JOIN BeatNodes bn ON bn.NodeId=c.Id
JOIN Beats bt ON bt.Id=bn.BeatId
WHERE p.NodeCode=@Code AND c.Title='The Evangelist: What Is Known, What Is Believed' AND bn.IsEnabled=1
"@
    $c.Parameters.AddWithValue("@Code", $code) | Out-Null
    $r = $c.ExecuteReader()
    if (-not $r.Read()) { $r.Close(); Write-Host "  $code : chapter not found"; return }
    $id = $r.GetGuid(0); $cur = $r.GetString(1); $r.Close()
    if ($cur.Contains("Scattered")) { Write-Host "  $code : already present, skip"; return }
    $new = $cur.TrimEnd() + "`n`n" + $extra.Trim()
    $new = [regex]::Replace($new, "(?<!`n)`n(?!`n)", ("`n" + "`n"))
    $new = [regex]::Replace($new, "`n{3,}", ("`n" + "`n")).Trim()
    Exec-NonQuery "UPDATE Beats SET Text=@T, TextHash=@H, UpdatedAt=SYSUTCDATETIME() WHERE Id=@Id" @{ T = $new; H = (Sha256Hex $new); Id = $id }
    Write-Host "  $code : relic section appended ($($new.Length) chars total)"
}

# ============================================================ MATTHEW
$N = "019FA01D-FA22-76C6-976C-3EA4F4D54A14"
$a = Next-Note $N; $b = $a+1
Add-Note $N $a "The arm in the Treasure Chapel" @"
Cappella del Tesoro (Treasure Chapel), Salerno Cathedral, Salerno, Campania, Italy. Alongside the remains venerated in the crypt, the cathedral's Treasure Chapel displays a relic venerated as the arm of the evangelist Matthew. The relics as a whole are recorded as having been brought to Salerno in 954 CE. No published scientific examination of either the crypt remains or the arm has been undertaken.
"@
Add-Note $N $b "A second arm, in Rome" @"
San Matteo in Merulana, Rome, and the Basilica di Santa Maria Maggiore, Rome (approximately 41.8976$deg N, 12.4984$deg E). The church of San Matteo in Merulana held a relic venerated as the arm of Matthew; recorded as derelict during the pontificate of Innocent X (1644-1655), it lost the relic to Santa Maria Maggiore, where the claim passed. Since Salerno Cathedral separately displays a relic venerated as Matthew's arm, at least two arm relics attributed to the same evangelist have been venerated simultaneously in Italy. Neither has been scientifically examined, and the duplication is not disputed by anyone $em it is simply not usually mentioned in the same breath.
"@
Append-ToEvangelistChapter "MATTHEW" @"
Scattered: who else claims a piece of him

The crypt is not the whole story, because relics were routinely divided, and Matthew's were. Salerno Cathedral itself displays, separately from the crypt, a relic venerated as his arm, in the Cappella del Tesoro $em the Treasure Chapel [$a].

There is also a second arm. A Roman church, San Matteo in Merulana, held a relic venerated as the arm of Matthew; when the church fell derelict under Innocent X in the mid-seventeenth century, the relic passed to the Basilica of Santa Maria Maggiore (approximately 41.8976$deg N, 12.4984$deg E), where the claim went with it [$b].

So two churches in Italy have venerated an arm of the same evangelist at the same time. Nobody disputes this and nobody hides it; it simply does not tend to get said in one sentence, which is how these things work. Neither arm has ever been examined, so the honest position is not that one is a fake $em it is that we have no way to know whether either is his, and we can be certain that at least one is not.
"@

# ============================================================ MARK
$N = "019FA968-1B3B-75DC-84CF-0C7D9C4E783C"
$a = Next-Note $N; $b = $a+1; $c = $a+2
Add-Note $N $a "The head that never left Egypt" @"
Saint Mark's Coptic Orthodox Cathedral, Mahatet el-Raml, Alexandria, Egypt: approximately 31.1983$deg N, 29.8994$deg E; the historical seat of the Pope of Alexandria. Coptic Orthodox tradition holds that the head of Mark remained in Alexandria when the body was taken in 828, and is preserved in the cathedral named for him there. More broadly, the Coptic Church maintains that Mark's remains never left Egypt at all, and are divided between Alexandria and Cairo $em a position in direct contradiction to the Venetian account that the body was removed to Venice in the ninth century. Neither set of remains has been scientifically examined, so the contradiction cannot presently be resolved by evidence.
"@
Add-Note $N $b "The 1968 return" @"
Saint Mark's Coptic Orthodox Cathedral, Abbassia district, Cairo, Egypt $em the principal Coptic Orthodox cathedral and the seat of the Coptic Pope. In 1968 Pope Paul VI returned a portion of the relics held at Venice to the Coptic Orthodox Church, and they are kept in the Cairo cathedral. (No surveyed coordinate for the Abbassia cathedral is given here; the district is named instead, in keeping with this book's practice of not supplying a precise figure it has not verified.)
"@
Add-Note $N $c "A third claim, on an island in Lake Constance" @"
Reichenau Abbey, Reichenau Island, Lake Constance, Baden-Wurttemberg, Germany: approximately 47.6944$deg N, 9.0625$deg E. Relics of Mark are claimed at Reichenau in addition to those at Venice and in Egypt; the abbey's claim is traditionally dated to the ninth century, in the same period as the Venetian translation. The claim is documented as a claim; the remains have not been examined.
"@
Append-ToEvangelistChapter "MARK" @"
Scattered: who else claims a piece of him

Venice is not the only claimant, and the rival claims do not merely divide the body $em one of them denies the Venetian story outright.

Coptic Orthodox tradition holds that the head stayed in Alexandria when the merchants took the body, and it is venerated there, in the cathedral named for him near Mahatet el-Raml (approximately 31.1983$deg N, 29.8994$deg E). Stated more strongly, the Coptic Church maintains that Mark's remains never left Egypt at all and are divided between Alexandria and Cairo. That is not a disagreement about which bone went where. It is a flat contradiction of the founding story of Venice's own basilica, held by the church that counts Mark as its founder [$a].

Then there is the 1968 return: Paul VI gave a portion of the Venetian relics back to the Coptic Church, and they rest in the principal Coptic cathedral in the Abbassia district of Cairo. Whatever else is true, some quantity of bone venerated as Mark's has now made the Alexandria-to-Venice journey and part of the return leg, eleven centuries apart [$b].

And a third claimant, rarely mentioned in the same paragraph as the other two: the abbey on Reichenau Island in Lake Constance (approximately 47.6944$deg N, 9.0625$deg E), which claims relics of Mark on a tradition dated to the same ninth century as the Venetian theft [$c].

Three custodians, one of whom says the transfer that produced the second never happened. None of the remains has been tested. This is what the evidentiary situation for a major relic actually looks like when you write all of it down at once, and it is worth noticing that no party is lying: each is faithfully transmitting what it received.
"@

# ============================================================ LUKE
$N = "019FA96B-18E2-7BB4-BAEB-11ACA24934F4"
$a = Next-Note $N; $b = $a+1; $c = $a+2
Add-Note $N $a "The skull in Prague" @"
St Vitus Cathedral, Prague Castle, Prague, Czech Republic: approximately 50.0905$deg N, 14.4010$deg E. A skull venerated as Luke's is held at St Vitus, having been removed from the Padua remains by the Emperor Charles IV in 1354 and taken to Prague. This accounts for the absence of the skull from the skeleton in the lead coffin opened at Padua in 1998.
"@
Add-Note $N $b "The skull fits the skeleton" @"
Reported findings of the 1998-2001 examination commissioned by Bishop Antonio Mattiazzo of Padua, which extended to the skull held at Prague as well as the remains at Padua. In addition to the radiocarbon and mitochondrial DNA results, the examination reported that the Prague skull fits the neck vertebra of the Padua skeleton. This is a materially different kind of finding from the others: it is an anatomical articulation test between two relics held 700 kilometres apart and separated since 1354, and a match is not what one would expect from two unrelated sets of remains. It does not identify the individual as Luke $em nothing in the examination can $em but it does indicate that the Prague skull and the Padua skeleton plausibly belonged to one person.
"@
Add-Note $N $c "The rib sent to Thebes" @"
On 17 September 2000 a Catholic delegation led by the Bishop of Padua, accompanied by a monk of the abbey, travelled to Thebes (Thiva) in Boeotia, central Greece, carrying a rib from the Padua skeleton $em the rib nearest the heart $em for the empty sepulchre venerated as Luke's in the Orthodox cathedral there. The transfer answered the 1992 request from the Orthodox Metropolitan of Thebes that had prompted the scientific examination in the first place. (No surveyed coordinate for the Theban cathedral is given here, only the locality, in keeping with this book's practice.)
"@
Append-ToEvangelistChapter "LUKE" @"
Scattered: who else claims a piece of him

Luke's remains are divided across three countries, and unusually, the division can be traced as documented history rather than tradition.

The skull is not at Padua. The Emperor Charles IV removed it in 1354 and took it to Prague, where it is held at St Vitus Cathedral in the castle complex (approximately 50.0905$deg N, 14.4010$deg E) $em which is why the skeleton in the lead coffin was found headless in 1998 [$a].

Then comes the most quietly impressive result of the whole investigation, and it is the reason this relic stands apart from every other in this series. The examination did not stop at Padua; it extended to the skull in Prague. And the Prague skull was found to fit the neck vertebra of the Padua skeleton [$b].

Consider what that is and is not. It is not proof that either belonged to Luke, and the investigators did not claim it was. What it is, is an articulation test between two objects that have been in different countries since 1354, roughly 700 kilometres apart, venerated separately for six centuries by people with no ability to check. They fit. That is a real, physical, falsifiable prediction that could easily have failed and did not. Two relics that tradition says came from one man are at least consistent with having come from one man.

And a third fragment travelled recently enough to have been photographed. On 17 September 2000, the Bishop of Padua carried a rib $em specifically the one nearest the heart $em to Thebes in Boeotia, and placed it in the empty tomb there. That gift was the whole reason the coffin had been opened two years earlier: Thebes had asked for a relic in 1992, and Padua had said yes on condition that the bones be tested first [$c].

So the ledger reads: a skeleton at Padua, a skull at Prague that fits it, and a rib returned to the Greek tomb the skeleton is said to have come from $em with a radiocarbon window and a maternal lineage that both point the right way and prove nothing. Of the four evangelists, Luke is the only one whose relics have been treated as a question rather than a possession, and the result is the most honest answer any of them has.
"@

# ============================================================ JOHN
$N = "019FA96D-7D48-75E0-9BD9-2190171276DC"
$a = Next-Note $N
Add-Note $N $a "The evangelist nobody claims" @"
On the relics of John: the tomb on Ayasuluk Hill at Selcuk was reported empty when opened in antiquity, the sixth-century basilica raised over it was abandoned after earthquakes and the region's conquest, and the location of any remains is unknown. John is accordingly noted as the rare major New Testament figure whose body or relics are claimed by no church and no city $em a category he shares, in the traditional reckoning, with Mary and Joseph. Explanations offered within the tradition range from removal of the remains for safekeeping to bodily assumption; the plain evidentiary situation is that there is nothing to examine and no claimant to examine it.
"@
Append-ToEvangelistChapter "JOHN" @"
Scattered: who else claims a piece of him

Nobody. That is the answer, and it is the most striking fact in this chapter.

The other three evangelists are divided among churches $em Matthew between a crypt and two rival arms, Mark between Venice and Egypt and an island in Lake Constance, Luke between Padua and Prague and Thebes. John has none of it. His tomb at Selcuk was reported empty when it was opened in antiquity; the great basilica over it was abandoned after earthquakes and conquest; and no church anywhere claims to hold his bones. In the traditional reckoning he shares that category with only Mary and Joseph [$a].

It is worth resisting the urge to make this mean something. There are ordinary explanations $em remains removed for safekeeping during a period of raids, or simply lost, which is what usually happens to a grave in an abandoned city. The tradition offers a less ordinary one. This book cannot adjudicate between them, and will not try.

What can be said is narrower and, in its way, more interesting. The medieval market for relics was enormous, and the pressure to produce them was correspondingly intense; almost every apostle acquired a shrine, and several acquired more than one. John, alone among the four, never did. Whatever the reason, the absence held $em across a thousand years in which claiming him would have been lucrative, and nobody did.
"@

$conn.Close()
Write-Host "DONE"
