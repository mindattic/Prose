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

$NotesNodeId = "019FA968-1B3B-75DC-84CF-0C7D9C4E783C"   # MARK Notes
$Ch7NodeId   = "019FA967-6138-7D58-A9FA-A44A98DA8B34"   # MARK Chapter 7

# --- hardened next-note-number derivation (docs/GSPL.md 5g3) ---
$maxNoteNumber = [int](Exec-Scalar @"
SELECT ISNULL(MAX(CAST(LEFT(b.Text, CHARINDEX(' ',b.Text)-1) AS INT)),0)
FROM BeatNodes bn JOIN Beats b ON b.Id=bn.BeatId
WHERE bn.NodeId='$NotesNodeId' AND bn.IsEnabled=1
  AND CHARINDEX(' ', b.Text) > 1
  AND LEFT(b.Text, CHARINDEX(' ',b.Text)-1) NOT LIKE '%[^0-9]%'
"@)
$maxNoteSortKey = [double](Exec-Scalar "SELECT ISNULL(MAX(bn.SortKey),0) FROM BeatNodes bn WHERE bn.NodeId='$NotesNodeId'")
$script:MaxNumber = [int](Exec-Scalar "SELECT MAX(Number) FROM Beats")
Write-Host "maxNoteNumber=$maxNoteNumber maxNoteSortKey=$maxNoteSortKey"

function Add-Note([string]$title, [string]$body) {
    $script:maxNoteNumber = $script:maxNoteNumber + 1
    $script:maxNoteSortKey = $script:maxNoteSortKey + 50
    $script:MaxNumber = $script:MaxNumber + 1
    $text = "$script:maxNoteNumber $em $title" + "`n`n" + $body
    $id = [guid]::NewGuid()
    Exec-NonQuery "INSERT INTO Beats (Id, Text, TextHash, Act, SceneType, Kind, Number, Stale, WasCorrected, IsChapterStart, Version, EntityStale, CreatedAt, UpdatedAt) VALUES (@Id, @Text, @Hash, 0, 'scene', 'prose', @Number, 0, 0, 0, 0, 0, SYSUTCDATETIME(), SYSUTCDATETIME())" @{ Id = $id; Text = $text; Hash = (Sha256Hex $text); Number = $script:MaxNumber }
    Exec-NonQuery "INSERT INTO BeatNodes (NodeId, BeatId, SortKey, IsEnabled) VALUES (@NodeId, @BeatId, @SortKey, 1)" @{ NodeId = [guid]$NotesNodeId; BeatId = $id; SortKey = $script:maxNoteSortKey }
    Write-Host "  note $script:maxNoteNumber added"
    return $script:maxNoteNumber
}
$script:maxNoteNumber = $maxNoteNumber
$script:maxNoteSortKey = $maxNoteSortKey

function Get-BeatIdByHeading([string]$headingPrefix) {
    $cmd = $conn.CreateCommand()
    $cmd.CommandText = "SELECT TOP 1 b.Id FROM BeatNodes bn JOIN Beats b ON b.Id=bn.BeatId WHERE bn.NodeId=@N AND bn.IsEnabled=1 AND b.Text LIKE @P"
    $cmd.Parameters.AddWithValue("@N", [guid]$Ch7NodeId) | Out-Null
    $cmd.Parameters.AddWithValue("@P", "$headingPrefix%") | Out-Null
    $r = $cmd.ExecuteScalar()
    if (-not $r) { throw "beat not found: $headingPrefix" }
    return [guid]$r
}
function Append-ToBeat([guid]$beatId, [string]$extra) {
    $cmd = $conn.CreateCommand(); $cmd.CommandText = "SELECT Text FROM Beats WHERE Id=@Id"
    $cmd.Parameters.AddWithValue("@Id", $beatId) | Out-Null
    $current = $cmd.ExecuteScalar()
    $updated = $current.TrimEnd() + "`n`n" + $extra.Trim()
    Exec-NonQuery "UPDATE Beats SET Text=@T, TextHash=@H, UpdatedAt=SYSUTCDATETIME() WHERE Id=@Id" @{ T = $updated; H = (Sha256Hex $updated); Id = $beatId }
}
function Replace-InBeat([guid]$beatId, [string]$find, [string]$replace) {
    $cmd = $conn.CreateCommand(); $cmd.CommandText = "SELECT Text FROM Beats WHERE Id=@Id"
    $cmd.Parameters.AddWithValue("@Id", $beatId) | Out-Null
    $current = $cmd.ExecuteScalar()
    if ($current.IndexOf($find) -lt 0) { Write-Host "  !! find-string absent, skipping replace"; return $false }
    $updated = $current.Replace($find, $replace)
    Exec-NonQuery "UPDATE Beats SET Text=@T, TextHash=@H, WasCorrected=1, UpdatedAt=SYSUTCDATETIME() WHERE Id=@Id" @{ T = $updated; H = (Sha256Hex $updated); Id = $beatId }
    Write-Host "  corrected in-place"
    return $true
}

# ================= NOTES =================
$nTravel = Add-Note "Three days from Galilee, not five or six" @"
Josephus, The Life of Flavius Josephus (Vita), section 269, trans. William Whiston; with Josephus, Antiquities of the Jews, Book 20, ch. 6, section 1 (= section 118 Niese). Josephus states that for rapid travel it was necessary to take the road through Samaria, "by which route Jerusalem may be reached in three days from Galilee," and separately records that "it was the custom of the Galileans, when they came to the holy city at the festivals, to take their journeys through the country of the Samaritans." The standard Galilee-to-Jerusalem journey was therefore roughly three days by the direct Samaritan road; the Jordan valley alternative, taken by travellers who preferred to avoid Samaritan territory, was longer.
"@

$nNedarim = Add-Note "The rabbis argued the same point, and moved the same way" @"
Mishnah, Nedarim 9:1. The tractate preserves a direct dispute over whether a rash vow may be annulled by offering the vower an "opening" of regret grounded in the honour owed to his father and mother: Rabbi Eliezer holds that it may, and the Sages refuse that opening in the general case while agreeing it applies where the vow itself concerned the parents. The significance for Mark 7:9-13 is that the tension Jesus names was argued inside the rabbinic tradition rather than only against it, and that rabbinic law developed machinery $em the annulment procedure, and an explicit refusal to let a vow override the duty to support parents $em aimed at precisely the abuse he describes.
"@

$nGrain = Add-Note "Tyre ate Galilean grain" @"
Acts 12:20; compare 1 Kings 5:11, Ezekiel 27:17, and Ezra 3:7. Acts records that the people of Tyre and Sidon sued for peace with Herod Agrippa I "because their region depended on the king's country for food." The same structural dependence appears centuries earlier: Solomon supplies Hiram of Tyre with wheat and oil (1 Kings 5:11), and Ezekiel's inventory of Tyrian trade has Judah and Israel paying Tyre in wheat, honey, oil, and balm (Ezekiel 27:17). The Phoenician coastal cities held little arable hinterland and imported grain from inland agricultural territory, Galilee included.
"@

$nMogilalos = Add-Note "A rare word used exactly twice" @"
Septuagint, Isaiah 35:6, compared with Mark 7:32. The Greek adjective mogilalos, denoting a speech impediment, occurs only once in the Septuagint $em at Isaiah 35:6, in the promise that the tongue of the mute will sing $em and only once in the New Testament, at Mark 7:32. The rarity of the word in both corpora is the basis for the widely noted argument that Mark's phrasing deliberately echoes the Isaiah passage.
"@

$nSpittle = Add-Note "Fasting spittle was standard first-century medicine" @"
Pliny the Elder, Natural History, Book 28, chs. 7 and 22, trans. W. H. S. Jones (Loeb Classical Library, Cambridge, MA: Harvard University Press). Pliny records the use of "fasting spittle" $em saliva taken before eating $em as an established remedy, prescribing daily application for ophthalmia, recommending it for bloodshot eyes, and reporting its use against boils and leprous spots. He is writing within a generation of Mark, and his testimony establishes that saliva was a recognised therapeutic medium in the wider first-century Mediterranean rather than an eccentric gesture.
"@

$nLadder = Add-Note "The Ladder of Tyre is a real obstacle" @"
Josephus, The Jewish War, Book 2, ch. 10, section 2 (Whiston numbering), which names the Ladder of Tyre (Scala Tyriorum) as a landmark of the coast. Topographical description: the ridge reaches the sea roughly one hundred stadia north of Ptolemais and drops sheer, leaving no passage along its base, so that the ancient road crossed it over the summit by a series of zigzags and cut steps $em the feature from which the name derives. It marked the southern pass into Phoenicia proper.
"@

$nDecapolis = Add-Note "Pliny names the ten cities" @"
Pliny the Elder, Natural History, Book 5, section 74. Pliny's mid-first-century list of the cities of the Decapolis gives Scythopolis, Hippos, Gadara, Raphana, Dion, Pella, Gerasa, Philadelphia, Canatha, and Damascus. All but Scythopolis (Beth-Shean) lie east of the Jordan, which is why a route "through the region of the Decapolis" describes a passage through predominantly Gentile territory.
"@

# ================= PROSE =================
Write-Host "Appending prose..."

# 1. Correct the uncited travel-time claim
$bUnwashed = Get-BeatIdByHeading "### Unwashed Hands"
Replace-InBeat $bUnwashed "a five- or six-day walk from Galilee" "roughly a three-day journey by the direct road through Samaria [$nTravel]" | Out-Null

# 2. Corban - the rabbinic convergence (fairness)
$bCorban = Get-BeatIdByHeading "### The Corban Vow"
Append-ToBeat $bCorban @"
There is a further point that fairness requires, because the passage is often read as Jesus against Judaism when the sources show something more interesting. The rabbinic tradition argued this exact question and largely came down where he did. Mishnah Nedarim preserves a dispute over whether a rash vow can be undone by confronting the person who made it with the honour owed to his father and mother: Rabbi Eliezer holds that it can, and the Sages restrict that route while conceding it where the vow itself concerned the parents [$nNedarim]. The machinery of annulment exists in the tractate precisely because vows that turned out to injure someone were a recognised, recurring problem $em and the principle that a vow must not be allowed to void the duty to support one's parents became the settled position. Read against that background, Mark 7:9-13 is not an outsider's attack on a rule nobody in Judaism doubted. It is a sharp intervention in a live internal argument, on the side that eventually won.
"@

# 3. Syrophoenician woman - the economics of "the children's bread"
$bWoman = Get-BeatIdByHeading "### The Syrophoenician Woman"
Append-ToBeat $bWoman @"
One further piece of context sharpens the exchange in a way most readings miss, and it is economic rather than ethnic. Tyre was a wealthy port with very little farmland behind it, and it fed itself on grain grown inland $em including in Galilee. Acts states the position baldly: the people of Tyre and Sidon sought terms with Herod Agrippa I because their region depended on his territory for food. The arrangement was centuries old, with Solomon supplying Hiram of Tyre in wheat and oil, and Ezekiel's survey of Tyrian commerce listing wheat, honey, oil, and balm moving from Judah and Israel to the city [$nGrain].

Set the dispute over bread inside that trade relationship and it acquires an edge no purely theological reading supplies. A woman from the rich coastal city that habitually bought Galilean grain is asking a Galilean for bread, and is answered with a line about the children's bread not going to the dogs. Whether or not that resonance is deliberate $em nothing in Mark says it is $em a first-century Galilean audience would not have needed it explained. Tyre was, in their economic experience, the place their food went.
"@

# 4. Ephphatha beat - spittle, Isaiah's rare word, and the geography of the detour
$bEph = Get-BeatIdByHeading "### Ephphatha, and a Strange Detour"
Append-ToBeat $bEph @"
Three further details in this scene rest on evidence outside the text. The first is the spit. Modern readers tend to register it as either crude or mystical, and it was neither: saliva was a recognised therapeutic substance in first-century Mediterranean medicine. Pliny the Elder, writing within a generation of Mark, treats "fasting spittle" $em saliva taken before eating $em as an ordinary remedy, prescribing its daily application for ophthalmia, recommending it for bloodshot eyes, and recording its use against boils and skin eruptions [$nSpittle]. Whatever Mark's readers thought was happening here, the medium would have looked like medicine to them.

The second is a single word. Mark describes the man with the Greek adjective mogilalos, which is a genuinely rare term: it appears once in the entire Septuagint, at Isaiah 35:6 $em the promise that the tongue of the mute will sing $em and once in the whole New Testament, here [$nMogilalos]. Whether Mark chose it deliberately cannot be proved from the word alone, but the crowd's closing verdict that "he has done all things well" sits inside a chapter that has just used Isaiah's vocabulary for exactly this healing.

The third bears on the itinerary. The "Ladders of Tyre" is not a vague obstacle: the coastal ridge meets the sea about a hundred stadia north of Ptolemais and falls sheer, leaving no passage at its foot, so that the ancient road climbed over the headland by zigzags and cut steps, and Josephus names the feature as a landmark of that coast [$nLadder]. And the territory the route crosses is definable. Pliny, writing in the same period, lists the ten cities of the Decapolis $em Scythopolis, Hippos, Gadara, Raphana, Dion, Pella, Gerasa, Philadelphia, Canatha, and Damascus $em all but the first lying east of the Jordan [$nDecapolis]. The loop is long, but every leg of it runs through country that can be named.
"@

$conn.Close()
Write-Host "MARK 7 DEPTH DONE"
