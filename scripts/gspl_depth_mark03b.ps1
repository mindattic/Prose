$OutputEncoding = [System.Text.UTF8Encoding]::new($false)
[Console]::OutputEncoding = [System.Text.UTF8Encoding]::new($false)
Add-Type -AssemblyName System.Data

$conn = New-Object System.Data.SqlClient.SqlConnection("Server=(localdb)\MSSQLLocalDB;Database=StreetSamurai;Trusted_Connection=True;TrustServerCertificate=True;")
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

$NotesNodeId = "019FA968-1B3B-75DC-84CF-0C7D9C4E783C"
$Ch3 = "019FA967-1DC4-7B12-8948-FC0C423511D4"

$script:num = [int](Exec-Scalar @"
SELECT ISNULL(MAX(CAST(LEFT(b.Text, CHARINDEX(' ',b.Text)-1) AS INT)),0)
FROM BeatNodes bn JOIN Beats b ON b.Id=bn.BeatId
WHERE bn.NodeId='$NotesNodeId' AND bn.IsEnabled=1
  AND CHARINDEX(' ', b.Text) > 1 AND LEFT(b.Text, CHARINDEX(' ',b.Text)-1) NOT LIKE '%[^0-9]%'
"@)
$script:sk = [double](Exec-Scalar "SELECT ISNULL(MAX(bn.SortKey),0) FROM BeatNodes bn WHERE bn.NodeId='$NotesNodeId'")
$script:beatNum = [int](Exec-Scalar "SELECT MAX(Number) FROM Beats")
Write-Host "starting at note $script:num"

function Add-Note([string]$title, [string]$body) {
    $script:num++; $script:sk += 50; $script:beatNum++
    $text = "$script:num $em $title" + "`n`n" + $body.Trim()
    $id = [guid]::NewGuid()
    Exec-NonQuery "INSERT INTO Beats (Id, Text, TextHash, Act, SceneType, Kind, Number, Stale, WasCorrected, IsChapterStart, Version, EntityStale, CreatedAt, UpdatedAt) VALUES (@Id, @Text, @Hash, 0, 'scene', 'prose', @Number, 0, 0, 0, 0, 0, SYSUTCDATETIME(), SYSUTCDATETIME())" @{ Id = $id; Text = $text; Hash = (Sha256Hex $text); Number = $script:beatNum }
    Exec-NonQuery "INSERT INTO BeatNodes (NodeId, BeatId, SortKey, IsEnabled) VALUES (@NodeId, @BeatId, @SortKey, 1)" @{ NodeId = [guid]$NotesNodeId; BeatId = $id; SortKey = $script:sk }
    Write-Host "  note $script:num"
    return $script:num
}
function Fix-Text([string]$find, [string]$replace) {
    $c = $conn.CreateCommand()
    $c.CommandText = "SELECT b.Id, b.Text FROM BeatNodes bn JOIN Beats b ON b.Id=bn.BeatId WHERE bn.NodeId=@N AND bn.IsEnabled=1"
    $c.Parameters.AddWithValue("@N", [guid]$Ch3) | Out-Null
    $r = $c.ExecuteReader(); $rows = @()
    while ($r.Read()) { $rows += [pscustomobject]@{ Id = $r.GetGuid(0); Text = $r.GetString(1) } }
    $r.Close()
    $hit = $false
    foreach ($x in $rows) {
        if ($x.Text.IndexOf($find) -ge 0) {
            $new = $x.Text.Replace($find, $replace)
            Exec-NonQuery "UPDATE Beats SET Text=@T, TextHash=@H, UpdatedAt=SYSUTCDATETIME() WHERE Id=@Id" @{ T = $new; H = (Sha256Hex $new); Id = $x.Id }
            $hit = $true; break
        }
    }
    if ($hit) { Write-Host "  ok: $($find.Substring(0,[Math]::Min(48,$find.Length)))..." }
    else { Write-Host "  !! NOT FOUND: $($find.Substring(0,[Math]::Min(60,$find.Length)))..." }
}

# ---------------- NOTES ----------------
$nCustom = Add-Note "Josephus says the Galileans went through Samaria, not around it" @"
Josephus, Antiquities of the Jews, Book 20, ch. 6, section 1 (= section 118 Niese), and The Life of Flavius Josephus (Vita), section 269, trans. William Whiston. Josephus states the custom directly: "it was the custom of the Galileans, when they came to the holy city at the time of the festivals, to take their journeys through the country of the Samaritans." He adds in the Life that speed required that same road, "by which route Jerusalem may be reached in three days from Galilee." Both passages therefore describe Galilean pilgrims routinely crossing Samaria rather than avoiding it, and put the journey at about three days $em not a long detour east of the Jordan.
"@
$nGinae = Add-Note "Where the reputation for danger actually comes from" @"
Josephus, Antiquities of the Jews, Book 20, ch. 6, sections 1-3 (= sections 118-136 Niese). Immediately after stating that Galileans customarily travelled through Samaria, Josephus narrates why the road had a reputation: at the village of Ginae, on the boundary between Samaria and the great plain, Samaritan villagers attacked a party of Galilean pilgrims and killed a number of them, and when the Galilean leadership asked the Roman procurator Cumanus to punish it, he was paid to let the matter drop. The episode falls in Cumanus's procuratorship, in the decades after Jesus's ministry. It is real evidence that the route could be dangerous, and it is not evidence that the route was avoided $em Josephus reports the custom and the killing in the same breath.
"@
$nEkron = Add-Note "Baal-zebub, the god of Ekron" @"
2 Kings 1:2-3, 6, 16. The Hebrew Bible names the god three times within a single chapter, in the account of the injured King Ahaziah of Israel sending messengers to consult "Baal-zebub, the god of Ekron" about whether he would recover, and of the prophet Elijah intercepting them. Ekron was one of the five principal Philistine cities. The name is the direct ancestor of the New Testament "Beelzebul."
"@
$nRosters = Add-Note "Four lists of the Twelve, and they do not match" @"
Mark 3:16-19, Matthew 10:2-4, Luke 6:14-16, and Acts 1:13, compared directly. Mark and Matthew include a Thaddaeus who appears in neither Lukan list; both Lukan lists include a Judas son of James who appears in neither Mark nor Matthew; and the two names never appear together in the same roster. Mark's "Simon the Cananaean" is rendered by Luke as "Simon called the Zealot." The divergences are limited to naming and order $em all four lists agree on the number twelve and on the great majority of the individuals.
"@
$nFathers = Add-Note "The three fourth-century positions, and the works that argue them" @"
Jerome, De perpetua virginitate beatae Mariae adversus Helvidium (Against Helvidius: On the Perpetual Virginity of Blessed Mary), c. 383; and Epiphanius of Salamis, Panarion, c. 375-378. The three standard readings of the "brothers" of Jesus still carry their fourth-century proponents' names: the Helvidian (children of Joseph and Mary born after Jesus), the Epiphanian (Joseph's children by an earlier marriage, hence older than Jesus), and the Hieronymian (cousins). Helvidius's own treatise does not survive independently and is known through Jerome's reply. The Epiphanian reading remains the standard position in Eastern Orthodoxy; the Hieronymian became standard in the Latin West through Jerome's influence.
"@
$nBauckham = Add-Note "Bauckham's case for the Epiphanian reading" @"
Richard Bauckham, Jude and the Relatives of Jesus in the Early Church (Edinburgh: T. & T. Clark, 1990). Bauckham assembles the evidence for the role Jesus's relatives played in the earliest church, drawing on the New Testament, the Church Fathers, the New Testament apocrypha, rabbinic literature, and Palestinian archaeology, and argues that the Epiphanian position rests on a line of tradition older than and independent of Jerome's fourth-century cousin argument $em so that whatever its final merits, it is not simply a doctrine-driven invention.
"@
$nSolomon = Add-Note "The territorial formula Mark's list may be echoing" @"
1 Kings 4:21 (= 5:1 in the Hebrew numbering), with the recurring merism "from Dan to Beersheba" at Judges 20:1, 1 Samuel 3:20, and 2 Samuel 24:2. 1 Kings describes Solomon's dominion as reaching "from the River" (the Euphrates) to "the border of Egypt." Both are stock scriptural devices for naming Israel's whole extent by citing its outermost points, which is the pattern some scholars hear behind Mark's sixfold list of regions.
"@

# ---------------- CORRECTION + CITATIONS ----------------
Write-Host "Applying correction and citations..."

Fix-Text `
"Josephus records that Galilean pilgrims travelling south for the great festivals customarily avoided the more direct route through Samaria, a region with a long history of hostility toward Galilean travelers, taking the longer road east through Perea instead (Antiquities 20.118; Life 269), meaning even the shortest practical route between Galilee and Jerusalem ran directly through the very ""beyond the Jordan"" territory Mark lists as its own separate point of origin." `
"Josephus is worth quoting accurately here, because the popular version of this has it backwards. He states that ""it was the custom of the Galileans, when they came to the holy city at the time of the festivals, to take their journeys through the country of the Samaritans,"" and puts that route at about three days [$nCustom]. Galilean pilgrims went through Samaria; they did not routinely detour around it. The road's genuine reputation for danger rests on a specific incident Josephus narrates in the very next breath $em Samaritan villagers at Ginae killing a party of Galilean pilgrims, and the Roman procurator Cumanus taking money to ignore it $em which happened in the decades after Jesus's ministry, not before it [$nGinae]. So Mark's ""beyond the Jordan"" is a genuinely separate point of origin rather than a leg of the Jerusalem road."

Fix-Text "(2 Kings 1:2-3, 6, 16)" "(2 Kings 1:2-3, 6, 16) [$nEkron]"
Fix-Text "Matthew 10:2-4, Luke 6:14-16, and Acts 1:13" "Matthew 10:2-4, Luke 6:14-16, and Acts 1:13 [$nRosters]"
Fix-Text "proposed instead that adelphoi here means cousins" "proposed instead that adelphoi here means cousins [$nFathers]"
Fix-Text "Richard Bauckham's later study of Jesus's actual named relatives and their documented role in the earliest church argues" "Richard Bauckham's later study of Jesus's actual named relatives and their documented role in the earliest church [$nBauckham] argues"
Fix-Text "1 Kings 4:21)" "1 Kings 4:21) [$nSolomon]"

$conn.Close()
Write-Host "MARK 3 DONE"
