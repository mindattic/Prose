$OutputEncoding = [System.Text.UTF8Encoding]::new($false)
[Console]::OutputEncoding = [System.Text.UTF8Encoding]::new($false)
Add-Type -AssemblyName System.Data

$conn = New-Object System.Data.SqlClient.SqlConnection("Server=(localdb)\MSSQLLocalDB;Database=StreetSamurai;Trusted_Connection=True;TrustServerCertificate=True;")
$conn.Open()
$em = [char]8212
$dg = [char]176
$GSPL = [guid]"0197E9C9-0003-7000-8000-000000000003"

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
function Get-BookProse([string]$code) {
    $c = $conn.CreateCommand()
    $c.CommandText = "SELECT bt.Text FROM Nodes c JOIN Nodes p ON p.Id=c.ParentNodeId JOIN BeatNodes bn ON bn.NodeId=c.Id JOIN Beats bt ON bt.Id=bn.BeatId WHERE p.NodeCode=@Code AND bn.IsEnabled=1"
    $c.Parameters.AddWithValue("@Code", $code) | Out-Null
    $r = $c.ExecuteReader(); $sb = New-Object System.Text.StringBuilder
    while ($r.Read()) { $sb.Append($r.GetString(0)) | Out-Null }
    $r.Close(); return $sb.ToString()
}
function Add-GazChapter([string]$bookId, [string]$slugBase, [double]$sortKey, [string]$body) {
    $t = "A Gazetteer of Places"
    $exists = [int](Exec-Scalar "SELECT COUNT(*) FROM Nodes WHERE ParentNodeId='$bookId' AND Title='$t'")
    if ($exists -gt 0) {
        $bid = Exec-Scalar "SELECT TOP 1 bn.BeatId FROM Nodes n JOIN BeatNodes bn ON bn.NodeId=n.Id WHERE n.ParentNodeId='$bookId' AND n.Title='$t' AND bn.IsEnabled=1"
        $tx = [regex]::Replace($body.Trim(), "`n{3,}", ("`n" + "`n")).Trim()
        Exec-NonQuery "UPDATE Beats SET Text=@T, TextHash=@H, UpdatedAt=SYSUTCDATETIME() WHERE Id=@Id" @{ T = $tx; H = (Sha256Hex $tx); Id = [guid]$bid }
        Write-Host "    chapter REWRITTEN ($($tx.Length) chars)"
        return
    }
    $nodeId = [guid]::NewGuid()
    Exec-NonQuery @"
SET QUOTED_IDENTIFIER ON;
INSERT INTO Nodes (Id, Slug, Title, Kind, Status, SortKey, StartedAt, CharsNarrated, CreatedAt, UpdatedAt,
                   NarratedBeatCount, TotalBeatsToNarrate, IsCanon, Version, UniverseId, NodeType, ParentNodeId)
VALUES (@Id, @Slug, @T, 'chapter', 'draft', @SK, SYSUTCDATETIME(), 0, SYSUTCDATETIME(), SYSUTCDATETIME(),
        0, 0, 0, 0, @Uni, 'chapter', @Parent)
"@ @{ Id = $nodeId; Slug = "$slugBase-gazetteer"; T = $t; SK = $sortKey; Uni = $GSPL; Parent = [guid]$bookId }
    $bnum = [int](Exec-Scalar "SELECT MAX(Number) FROM Beats") + 1
    $beatId = [guid]::NewGuid()
    $text = [regex]::Replace($body.Trim(), "`n{3,}", ("`n" + "`n")).Trim()
    Exec-NonQuery "INSERT INTO Beats (Id, Text, TextHash, Act, SceneType, Kind, Number, Stale, WasCorrected, IsChapterStart, Version, EntityStale, CreatedAt, UpdatedAt) VALUES (@Id, @Text, @Hash, 0, 'scene', 'prose', @Number, 0, 0, 1, 0, 0, SYSUTCDATETIME(), SYSUTCDATETIME())" @{ Id = $beatId; Text = $text; Hash = (Sha256Hex $text); Number = $bnum }
    Exec-NonQuery "INSERT INTO BeatNodes (NodeId, BeatId, SortKey, IsEnabled) VALUES (@N, @B, 100.0, 1)" @{ N = $nodeId; B = $beatId }
    Write-Host "    chapter added ($($text.Length) chars)"
}

# ---- the gazetteer entries: match-key, heading, coordinate line, body ----
$Entries = @(
 @{k='Nazareth'; h='NAZARETH'; c="32.7019$dg N, 35.3033$dg E"; b="Modern Nazareth, Northern District, Israel. The village of the childhood; an unwalled agricultural settlement of modest size in the first century, with rock-cut tombs, cisterns, and agricultural installations excavated within the modern city. Identification secure $em the name has never been lost."}
 @{k='Sepphoris'; h='SEPPHORIS (TZIPPORI)'; c="32.7456$dg N, 35.2786$dg E"; b="About an hour's walk north-west of Nazareth. The administrative capital of Galilee, rebuilt on a substantial scale during Jesus's youth. It is never named in any Gospel, which is itself worth noticing: the largest building project within walking distance of Nazareth goes entirely unmentioned. Identification secure."}
 @{k='Capernaum'; h='CAPERNAUM (KFAR NAHUM)'; c="32.8811$dg N, 35.5750$dg E"; b="North shore of the Sea of Galilee. The base of operations; excavated, with a fourth-or-fifth-century white limestone synagogue standing on the black basalt foundations of an earlier one, and an octagonal Byzantine church built over a first-century house complex venerated from early date as Peter's. A customs post fits the political geography, since the town sat near the boundary of Herod Antipas's territory. Identification secure."}
 @{k='Chorazin'; h='CHORAZIN (KORAZIM)'; c="32.9111$dg N, 35.5628$dg E"; b="Two and a half miles north of Capernaum, in the basalt hills. Named only in the woe pronounced on it. Extensively excavated ruins, including a basalt synagogue, are visible today. Identification secure; the town is, in the ordinary sense, gone."}
 @{k='Bethsaida'; h='BETHSAIDA $em two candidates'; c="et-Tell: 32.9103$dg N, 35.6306$dg E | el-Araj: 32.8933$dg N, 35.6191$dg E"; b="Home of Andrew, Peter, and Philip in John's account. The identification is genuinely unsettled: et-Tell, a mound about a mile and a half from the present shoreline, has been excavated since 1987 and is the long-standing candidate; el-Araj, closer to the lake, has produced first-century remains and a Byzantine church and now has substantial support. A caution for anyone checking a map: there is a second, unrelated et-Tell in the West Bank identified with biblical Ai $em not this site."}
 @{k='Magdala'; h='MAGDALA (MIGDAL)'; c="32.8250$dg N, 35.5156$dg E"; b="West shore of the Sea of Galilee. The place-name behind Mary Magdalene's byname. A first-century synagogue was excavated here from 2009, along with the carved Magdala Stone; the town also had a harbour and a fish-processing industry. Identification secure."}
 @{k='Cana'; h='CANA $em two candidates'; c="Khirbet Qana: 32.8236$dg N, 35.3042$dg E | Kafr Kanna: 32.7500$dg N, 35.3500$dg E"; b="Kafr Kanna has been shown to pilgrims since the Middle Ages and is more accessible; Khirbet Qana is an unoccupied ruin excavated since 1998 which has produced first-century Jewish occupation, a synagogue, ritual baths, Hasmonean coins, and a cave shrine with a shelf built for six stone jars. The traditional site has produced no evidence of Roman-period Jewish settlement. Contested, with the archaeological weight now favouring Khirbet Qana."}
 @{k='Nain'; h='NAIN (NEIN)'; c="32.6306$dg N, 35.3500$dg E"; b="A village on the north slope of the hill of Moreh, south-east of Nazareth, where the widow's son is raised. A small modern village occupies the site. Identification generally accepted."}
 @{k='Gergesa'; h='GERGESA (KURSI)'; c="32.8261$dg N, 35.6504$dg E"; b="East shore of the Sea of Galilee, and the leading candidate for the scene of the pig-herd exorcism, chiefly because it is the only spot on that shore with a steep slope running down to the water. Byzantine remains including a large monastery were excavated here. The place-name in the manuscripts varies between Gerasa, Gadara, and Gergesa, and the identification follows the topography rather than the text."}
 @{k='Mount Tabor'; h='MOUNT TABOR'; c="32.6872$dg N, 35.3903$dg E"; b="A distinctive dome rising from the Jezreel Valley, and the traditional site of the Transfiguration from at least the fourth century. Against it: the summit carried a settlement in the first century, which fits poorly with a mountain apart. Contested; see also Mount Hermon."}
 @{k='Mount Hermon'; h='MOUNT HERMON'; c="33.4161$dg N, 35.8575$dg E"; b="The high massif on the modern Syria-Lebanon border, snow-covered much of the year and by far the highest ground in the region. Favoured by many modern scholars for the Transfiguration on the grounds of proximity to Caesarea Philippi, where the preceding scene is set, and because it is genuinely a high mountain apart. Contested; see also Mount Tabor."}
 @{k='Caesarea Philippi'; h='CAESAREA PHILIPPI (BANIAS / PANEAS)'; c="33.2486$dg N, 35.6944$dg E"; b="At the foot of Mount Hermon, at one of the sources of the Jordan. A Herodian city rebuilt by Philip the tetrarch, long a cult site of the god Pan, whose grotto and rock-cut niches are the visible ruins today. The setting of Peter's confession. Identification secure."}
 @{k='Caesarea Maritima'; h='CAESAREA MARITIMA'; c="32.5000$dg N, 34.8917$dg E"; b="Herod's great artificial harbour on the Mediterranean coast, and the seat of the Roman governor $em meaning Pilate's normal residence was here, not Jerusalem. The inscription naming Pilate as prefect of Judea was found in its theatre in 1961. Identification secure and extensively excavated."}
 @{k='Jordan'; h='THE JORDAN BAPTISM SITE $em two banks, two countries'; c="Qasr el-Yahud (west): 31.8383$dg N, 35.5392$dg E | Al-Maghtas (east): 31.8372$dg N, 35.5503$dg E"; b="The traditional baptism area lies near the river's outflow toward the Dead Sea, and the tradition is split across an international border by the river itself: the western site, Qasr el-Yahud, and the eastern site, Al-Maghtas in Jordan, sit barely a kilometre apart at almost the same latitude. Both hold Byzantine church remains. The general locality is well attested; a precise spot is not recoverable, not least because the river has moved."}
 @{k='Jericho'; h='JERICHO'; c="Tell es-Sultan (ancient): 31.8710$dg N, 35.4443$dg E | modern town: 31.8667$dg N, 35.4500$dg E"; b="On the road up from the Jordan valley to Jerusalem $em the ascent the Good Samaritan's road descends. Worth knowing: there are effectively two Jerichos, the ancient mound and the separate Herodian-era winter-palace town nearby, which is why a Gospel can have Jesus both entering and leaving Jericho in successive verses without contradiction. Identification secure."}
 @{k='Herodium'; h='HERODIUM'; c="31.6658$dg N, 35.2414$dg E"; b="Herod the Great's artificial cone of a fortress-palace south-east of Bethlehem, and $em on the identification published in 2007 $em the site of his own tomb. The king of the nativity narrative has a findable address. Identification of the site secure; the tomb identification is accepted by many and disputed by some."}
 @{k='Machaerus'; h='MACHAERUS'; c="31.5672$dg N, 35.6242$dg E"; b="A Herodian fortress on a peak east of the Dead Sea, in modern Jordan, where Josephus places the imprisonment and execution of John the Baptist. Excavated, including the throne room and courtyard of the kind of court a birthday banquet would require. Identification secure; Josephus supplies the location, the Gospels do not."}
 @{k='Bethlehem'; h='BETHLEHEM'; c="Church of the Nativity: 31.7043$dg N, 35.2076$dg E"; b="Five miles south of Jerusalem. The Church of the Nativity, begun under Constantine in the fourth century, stands over a grotto venerated as the birthplace $em making it one of the oldest continuously used churches on earth. Note what the text does and does not say: no Gospel mentions a stable or a cave, and the word rendered inn (kataluma) means a guest room, not a commercial inn, for which Greek has a different word. A minority scholarly view places the birth at Nazareth instead, and a separate proposal has argued for Bethlehem of Galilee in the north."}
 @{k='Temple'; h='THE TEMPLE MOUNT'; c="31.7781$dg N, 35.2358$dg E"; b="The vast Herodian platform, whose retaining walls are the largest surviving remains of the building. Nothing of the sanctuary itself stands. The courts, porticoes, money-changers' tables, and treasury chests of the Gospel scenes were all on this platform. Identification secure; the platform is the single most certainly located place in the New Testament."}
 @{k='Bethesda'; h='POOL OF BETHESDA'; c="31.7814$dg N, 35.2358$dg E"; b="By the Sheep Gate in the north-east of the Old City, beside the Church of St Anne. Excavation revealed a twin-pool complex whose layout accommodates the five porticoes John describes $em four sides and a dividing colonnade $em after the detail had long been treated as evidence the author did not know Jerusalem. Identification secure."}
 @{k='Siloam'; h='POOL OF SILOAM'; c="31.7690$dg N, 35.2343$dg E"; b="At the southern end of the City of David, fed by the Gihon spring through Hezekiah's tunnel. The Second Temple-period pool, with its stepped stone sides, was uncovered from 2004 during sewer work, some distance from the smaller Byzantine pool previously shown to visitors. Identification secure."}
 @{k='Gethsemane'; h='GETHSEMANE'; c="31.7794$dg N, 35.2402$dg E"; b="Across the Kidron valley at the foot of the Mount of Olives, by the Church of All Nations. The name means something like oil press, a workaday agricultural label rather than a mystical one. A first-century ritual bath was identified nearby in 2020, consistent with an agricultural installation on the site. The general locality is secure; the precise enclosure is traditional."}
 @{k='Mount of Olives'; h='MOUNT OF OLIVES'; c="31.7783$dg N, 35.2439$dg E"; b="The ridge east of the Old City, separated from the Temple Mount by the Kidron valley, carrying the road to Bethany and Jericho. The vantage point for the discourse on the Temple's destruction $em from which the building being discussed was in plain view. Identification secure."}
 @{k='Bethany'; h='BETHANY (AL-EIZARIYA)'; c="31.7700$dg N, 35.2644$dg E"; b="On the eastern slope of the Mount of Olives, about two miles from Jerusalem, on the Jericho road. The village of Martha, Mary, and Lazarus, and the base for the final week. The modern Arabic name preserves Lazarus $em al-Eizariya, the place of Lazarus $em and a tomb venerated as his is shown there. Identification of the village secure; the tomb is traditional."}
 @{k='Golgotha'; h='GOLGOTHA AND THE TOMB $em two candidates'; c="Church of the Holy Sepulchre: 31.7786$dg N, 35.2294$dg E | Garden Tomb: 31.7836$dg N, 35.2247$dg E"; b="The Church of the Holy Sepulchre stands over a disused limestone quarry containing rock-cut tombs of the right period, outside the city wall as it ran at the time, and excavation beneath the church has recovered traces of cultivated ground $em olive and vine $em of roughly the right date. The Garden Tomb, identified in the nineteenth century, is a genuine rock-cut tomb but is generally dated earlier than the first century and has almost no support in scholarship. Contested in popular presentation; not seriously contested in the field."}
 @{k="Jacob's Well"; h="JACOB'S WELL"; c="32.2095$dg N, 35.2853$dg E"; b="At the eastern edge of modern Nablus, near ancient Shechem, in the crypt of an Orthodox church. A deep, genuinely ancient well at the traditional site of the conversation with the Samaritan woman. The identification is old and geographically coherent; the well is real and still holds water."}
 @{k='Gerizim'; h='MOUNT GERIZIM'; c="32.2009$dg N, 35.2733$dg E"; b="Above Nablus. The Samaritan holy mountain and the site of their temple, destroyed by the Hasmonean John Hyrcanus in the second century BCE. This is the mountain meant by this mountain in the Samaritan woman's question about where worship properly belongs. Identification secure; the ruins are excavated."}
 @{k='Emmaus'; h='EMMAUS $em unlocated'; c="no secure fix"; b="The only significant Gospel place in this gazetteer with no coordinate. Manuscripts disagree on the distance from Jerusalem $em sixty stadia in most, one hundred and sixty in Codex Sinaiticus $em and the candidate sites split accordingly: Emmaus Nicopolis has the oldest identification but sits at roughly the longer distance, while Abu Ghosh and el-Qubeibeh match the shorter reading but carry no ancient claim to the name. A real village, genuinely mislaid."}
)

$preamble = @"
Every place named in this book that can be located is listed here with coordinates, so that any of it can be typed into a map and looked at. That is not a gimmick. The argument running through this whole series is that these events are set in a real landscape rather than a symbolic one, and the fastest way to feel the difference is to see the hill, measure the walk, and notice that two villages competing for the same name are eleven miles apart.

Four conventions, stated plainly.

Coordinates are decimal degrees on the WGS84 datum, the system a phone or web map uses, so they can be pasted in directly. They locate a site, not a spot: a figure given for a town centre or an archaeological mound is accurate to the settlement, not to a doorway.

Where a coordinate could not be verified, none is given. There are a few of those, and the omission is deliberate $em a plausible-looking figure invented for completeness would be exactly the failure this book exists to avoid.

Where the identification itself is disputed, both or all candidates are listed with their own coordinates, and the state of the argument is summarised rather than resolved. Contested does not mean unknowable; it means the evidence has not settled it yet.

Where a place cannot be located at all, it still gets an entry saying so. An honest blank is information.

One caution before the list. Coordinates carry an air of authority that can outrun what they actually establish. That a tomb sits at a given latitude tells you where a building venerated as a tomb stands today. It does not tell you what happened there. The figures below are a reliable guide to where to stand and a poor guide to what to believe, and this book would rather you had both facts than one.
"@

$books = @(
 @{code='MATTHEW'; id='019FA049-322F-75EF-AAB7-0C0DE8DBDB85'; notes='019FA01D-FA22-76C6-976C-3EA4F4D54A14'; slug='matthew'; sk=2975.0}
 @{code='MARK';    id='019FA966-2F28-7A30-9662-F0F6F33C4D54'; notes='019FA968-1B3B-75DC-84CF-0C7D9C4E783C'; slug='mark';    sk=1675.0}
 @{code='LUKE';    id='019FA969-3232-772B-998A-BB2D5158F96E'; notes='019FA96B-18E2-7BB4-BAEB-11ACA24934F4'; slug='luke';    sk=2475.0}
 @{code='JOHN';    id='019FA96B-CAD8-7769-BF17-363E3641048E'; notes='019FA96D-7D48-75E0-9BD9-2190171276DC'; slug='john';    sk=2175.0}
)

foreach ($b in $books) {
    Write-Host $b.code
    $pc = $conn.CreateCommand()
    $pc.CommandText = "SELECT bt.Text FROM Nodes c JOIN Nodes p ON p.Id=c.ParentNodeId JOIN BeatNodes bn ON bn.NodeId=c.Id JOIN Beats bt ON bt.Id=bn.BeatId WHERE p.NodeCode='" + $b.code + "' AND bn.IsEnabled=1"
    $pr = $pc.ExecuteReader(); $psb = New-Object System.Text.StringBuilder
    while ($pr.Read()) { [void]$psb.Append($pr.GetString(0)) }
    $pr.Close()
    $prose = $psb.ToString()
    Write-Host ("    proselen=" + $prose.Length)
    $existingNum = Exec-Scalar "SELECT MIN(CAST(LEFT(bt.Text, CHARINDEX(' ',bt.Text)-1) AS INT)) FROM BeatNodes bn JOIN Beats bt ON bt.Id=bn.BeatId WHERE bn.NodeId='$($b.notes)' AND bn.IsEnabled=1 AND bt.Text LIKE '%How the coordinates in the gazetteer%'"
    if ($existingNum -isnot [DBNull] -and $existingNum) {
        $n = [int]$existingNum
        Write-Host "    reusing existing note $n"
        $skipNote = $true
    } else { $n = Next-Note $b.notes; $skipNote = $false }
    if (-not $skipNote) { Add-Note $b.notes $n "How the coordinates in the gazetteer were obtained" @"
Coordinates are given in decimal degrees on the WGS84 datum, compiled from standard published geographic and archaeological gazetteer references for each site and cross-checked between sources where they differ. They are site-level fixes $em a settlement, a mound, a church $em and are not surveyed points for a specific structure or feature unless the entry says so. Where sources disagreed materially, or where no reliable figure could be confirmed, no coordinate is printed and the entry states the omission. Elevation is not given. Readers should be aware that a coordinate locates a modern place bearing an ancient name, or a modern building venerated as an ancient site, and that this is a separate question from whether the identification is correct $em which each entry addresses on its own.
"@ }
    $body = $preamble.Trim() + " [$n]" + "`n`n"

    $count = 0
    foreach ($ent in $Entries) {
        if ($prose.IndexOf($ent.k, [System.StringComparison]::OrdinalIgnoreCase) -lt 0) { continue }
        $body += $ent.h + "`n" + $ent.c + "`n" + $ent.b.Trim() + "`n`n"
        $count++
    }
    Write-Host "    entries: $count"
    Add-GazChapter $b.id $b.slug $b.sk $body
}

$conn.Close()
Write-Host "DONE"
