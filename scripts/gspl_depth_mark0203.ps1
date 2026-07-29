$OutputEncoding = [System.Text.UTF8Encoding]::new($false)
[Console]::OutputEncoding = [System.Text.UTF8Encoding]::new($false)
Add-Type -AssemblyName System.Data

$connStr = "Server=(localdb)\MSSQLLocalDB;Database=StreetSamurai;Trusted_Connection=True;TrustServerCertificate=True;"
$conn = New-Object System.Data.SqlClient.SqlConnection($connStr)
$conn.Open()
$em = [char]8212

function Sha256Hex([string]$text) {
    $sha = [System.Security.Cryptography.SHA256]::Create()
    $bytes = [System.Text.Encoding]::UTF8.GetBytes($text.Trim())
    return ([System.BitConverter]::ToString($sha.ComputeHash($bytes)) -replace '-', '').ToLower()
}
function Exec-NonQuery([string]$sql, [hashtable]$params) {
    $cmd = $conn.CreateCommand(); $cmd.CommandText = $sql
    foreach ($k in $params.Keys) { $cmd.Parameters.AddWithValue("@$k", $params[$k]) | Out-Null }
    $cmd.ExecuteNonQuery() | Out-Null
}
function Exec-Scalar([string]$sql) { $cmd = $conn.CreateCommand(); $cmd.CommandText = $sql; return $cmd.ExecuteScalar() }

$NotesNodeId = "019FA968-1B3B-75DC-84CF-0C7D9C4E783C"
$Ch2 = "019FA967-0D77-73B8-A0B4-BA4423DF5219"
$Ch3 = "019FA967-1DC4-7B12-8948-FC0C423511D4"

$script:maxNoteNumber = [int](Exec-Scalar @"
SELECT ISNULL(MAX(CAST(LEFT(b.Text, CHARINDEX(' ',b.Text)-1) AS INT)),0)
FROM BeatNodes bn JOIN Beats b ON b.Id=bn.BeatId
WHERE bn.NodeId='$NotesNodeId' AND bn.IsEnabled=1
  AND CHARINDEX(' ', b.Text) > 1 AND LEFT(b.Text, CHARINDEX(' ',b.Text)-1) NOT LIKE '%[^0-9]%'
"@)
$script:maxNoteSortKey = [double](Exec-Scalar "SELECT ISNULL(MAX(bn.SortKey),0) FROM BeatNodes bn WHERE bn.NodeId='$NotesNodeId'")
$script:MaxNumber = [int](Exec-Scalar "SELECT MAX(Number) FROM Beats")
Write-Host "starting note number: $script:maxNoteNumber"

function Add-Note([string]$title, [string]$body) {
    $script:maxNoteNumber++; $script:maxNoteSortKey += 50; $script:MaxNumber++
    $text = "$script:maxNoteNumber $em $title" + "`n`n" + $body.Trim()
    $id = [guid]::NewGuid()
    Exec-NonQuery "INSERT INTO Beats (Id, Text, TextHash, Act, SceneType, Kind, Number, Stale, WasCorrected, IsChapterStart, Version, EntityStale, CreatedAt, UpdatedAt) VALUES (@Id, @Text, @Hash, 0, 'scene', 'prose', @Number, 0, 0, 0, 0, 0, SYSUTCDATETIME(), SYSUTCDATETIME())" @{ Id = $id; Text = $text; Hash = (Sha256Hex $text); Number = $script:MaxNumber }
    Exec-NonQuery "INSERT INTO BeatNodes (NodeId, BeatId, SortKey, IsEnabled) VALUES (@NodeId, @BeatId, @SortKey, 1)" @{ NodeId = [guid]$NotesNodeId; BeatId = $id; SortKey = $script:maxNoteSortKey }
    Write-Host "  note $script:maxNoteNumber added"
    return $script:maxNoteNumber
}
function Get-BeatId([string]$nodeId, [string]$prefix) {
    $cmd = $conn.CreateCommand()
    $cmd.CommandText = "SELECT TOP 1 b.Id FROM BeatNodes bn JOIN Beats b ON b.Id=bn.BeatId WHERE bn.NodeId=@N AND bn.IsEnabled=1 AND b.Text LIKE @P ORDER BY bn.SortKey"
    $cmd.Parameters.AddWithValue("@N", [guid]$nodeId) | Out-Null
    $cmd.Parameters.AddWithValue("@P", "$prefix%") | Out-Null
    $r = $cmd.ExecuteScalar(); if (-not $r) { throw "not found: $prefix" }; return [guid]$r
}
function Append-ToBeat([guid]$beatId, [string]$extra) {
    $cmd = $conn.CreateCommand(); $cmd.CommandText = "SELECT Text FROM Beats WHERE Id=@Id"
    $cmd.Parameters.AddWithValue("@Id", $beatId) | Out-Null
    $cur = $cmd.ExecuteScalar()
    $new = $cur.TrimEnd() + "`n`n" + $extra.Trim()
    Exec-NonQuery "UPDATE Beats SET Text=@T, TextHash=@H, UpdatedAt=SYSUTCDATETIME() WHERE Id=@Id" @{ T = $new; H = (Sha256Hex $new); Id = $beatId }
    Write-Host "  appended"
}

# ---------------- NOTES ----------------
$nBooth = Add-Note "Capernaum sat on a tax border" @"
Mark 2:14, read against the political geography of the Herodian tetrarchies. On the death of Herod the Great his kingdom was divided among his sons, and Capernaum lay in Herod Antipas's Galilee close to the frontier with the territory of his half-brother Philip on the far side of the lake and upper Jordan. The town also sat on the through route carrying traffic down from the north. A toll post at Capernaum is therefore exactly what the political map predicts: Levi's booth would have collected indirect duties $em customs and transit tolls on goods crossing into Antipas's jurisdiction $em rather than the direct taxes a Roman official would assess. The distinction matters, because it makes Levi a local contractor working a border, not an agent of Rome.
"@

$nAbiathar = Add-Note "Mark names the wrong priest, and the other two evangelists quietly drop the name" @"
Mark 2:26, against 1 Samuel 21:1-6. Mark has Jesus place David's eating of the consecrated bread "in the time of Abiathar the high priest." 1 Samuel names the priest at Nob as Ahimelech; Abiathar was his son, who escaped when Saul had Ahimelech and the other priests of Nob killed and only afterwards served as high priest under David. Proposed resolutions include a proleptic use of the title (naming him by the office he later held), a looser sense for the Greek preposition epi than "when," and shared priestly duty between father and son. The most telling datum is editorial rather than lexical: the parallel accounts at Matthew 12:3-4 and Luke 6:3-4 reproduce the incident and omit the high priest's name altogether, which is what one would expect if the difficulty was noticed early.
"@

$nFasts = Add-Note "Two fasts a week, neither of them commanded" @"
Luke 18:12 and Didache 8:1, with Leviticus 16:29-31. The Torah imposes one annual fast, the Day of Atonement; the additional weekly fasting referred to in the Gospels was voluntary pious practice, not statutory obligation. Luke's Pharisee says he fasts twice a week, and the Didache $em a Christian manual usually dated to the late first or early second century $em identifies the days and distances its own readers from them: "let not your fasts be with the hypocrites, for they fast on the second and fifth day of the week," directing Christians to the fourth day and Friday instead. The second and fifth days, counted from the Sabbath, are Monday and Thursday. The Didache is thus independent early evidence both for the Monday-Thursday practice and for the speed with which the two groups differentiated themselves by calendar.
"@

$nYoma = Add-Note "Rabbinic law already permitted Sabbath healing when life was at risk" @"
Mishnah, Yoma 8:6. The tractate rules that one seized with a dangerous craving may be fed even forbidden food until he recovers, and preserves Rabbi Matya ben Charash's ruling that medicine may be put into the mouth of a man with a sore throat on the Sabbath "because of possible danger to his life, and whatever threatens to endanger life supersedes the observance of the Sabbath." The principle later known as pikuach nefesh holds even where the danger is merely possible rather than established. This is essential context for the Sabbath-healing controversies in the Gospels: the dispute was never over whether the Sabbath could be broken to save a life, which was conceded, but over conditions like a withered hand or a chronic bend of the spine, where no life was in immediate danger and the exception therefore did not obviously apply.
"@

# ---------------- PROSE ----------------
$b2Levi = Get-BeatId $Ch2 "Jesus calls his next disciple straight out of a customs booth"
Append-ToBeat $b2Levi @"
The booth's location is not incidental, and the political map explains it. When Herod the Great's kingdom was divided among his sons, Capernaum ended up in Herod Antipas's Galilee, close to the boundary with the territory his half-brother Philip held on the other side of the lake and the upper Jordan, and on the route carrying traffic down from the north. A customs post is precisely what those two facts predict [$nBooth]. That places Levi in a specific job: collecting transit and customs duties on goods entering Antipas's jurisdiction, as a local contractor working a frontier $em not, as the shorthand "tax collector" tends to suggest to a modern ear, an official of Rome collecting Rome's taxes. The resentment he attracted was local money going to a local ruler through a neighbour who took his cut at the gate.
"@

$b2Grain = Get-BeatId $Ch2 "Walking through a grain field on the Sabbath"
Append-ToBeat $b2Grain @"
There is also a plain factual problem in the way Jesus's precedent is reported here, and it is worth naming rather than smoothing. Mark sets David's eating of the consecrated bread "in the time of Abiathar the high priest," but 1 Samuel names the priest at Nob as Ahimelech $em Abiathar was his son, who survived Saul's massacre of the priests there and held the high priesthood only later, under David [$nAbiathar]. Defences of the wording exist and are not frivolous: the title may be used proleptically, naming the man by the office he would come to hold, and the Greek preposition behind "in the time of" is genuinely elastic.

What tips the scales toward a simple slip is not lexical but editorial. Matthew and Luke both retell this same argument, and both drop the high priest's name entirely. That is the behaviour of writers working from Mark who noticed something they preferred not to repeat $em and it is a small, useful demonstration that the earliest of the Gospels was being read critically by the other evangelists within a generation of its composition.
"@

$b3Hand = Get-BeatId $Ch3 "Jesus goes back into a synagogue where a man with a withered hand"
Append-ToBeat $b3Hand @"
One piece of legal background is indispensable here, and its absence is what makes these controversies look, to modern readers, like a quarrel between compassion and heartless rule-keeping. Rabbinic law did not forbid saving a life on the Sabbath. The Mishnah rules the opposite outright: medicine may be administered on the Sabbath to a man with a sore throat "because of possible danger to his life, and whatever threatens to endanger life supersedes the observance of the Sabbath," and the exception applies even where the danger is only possible rather than proven [$nYoma].

That is why the cases the Gospels choose are the cases they choose. A withered hand is not a medical emergency, and neither is a spine bent for eighteen years. Both are conditions in which the life-saving exception plainly does not apply, which puts the argument exactly where the dispute was genuinely live $em not over whether the Sabbath yields to urgent need, which was conceded on all sides, but over whether it yields to need that has waited years already and could, without harm, wait one more day. Read that way, the silence Mark reports (3:4) is not obtuseness. The question has been framed so that answering it concedes the point.
"@

$conn.Close()
Write-Host "MARK 2+3 DEPTH DONE"
