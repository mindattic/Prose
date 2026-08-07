$ErrorActionPreference = 'Stop'   # fail loudly: a SqlException is otherwise non-terminating
$OutputEncoding = [System.Text.UTF8Encoding]::new($false)
[Console]::OutputEncoding = [System.Text.UTF8Encoding]::new($false)
Add-Type -AssemblyName System.Data

$connStr = "Server=(localdb)\MSSQLLocalDB;Database=Prose;Trusted_Connection=True;TrustServerCertificate=True;"
$conn = New-Object System.Data.SqlClient.SqlConnection($connStr)
$conn.Open()

function Sha256Hex([string]$text) {
    $sha = [System.Security.Cryptography.SHA256]::Create()
    $bytes = [System.Text.Encoding]::UTF8.GetBytes($text.Trim())
    $hash = $sha.ComputeHash($bytes)
    return ([System.BitConverter]::ToString($hash) -replace '-', '').ToLower()
}
function Exec-NonQuery([string]$sql, [hashtable]$params) {
    $cmd = $conn.CreateCommand(); $cmd.CommandText = $sql
    foreach ($k in $params.Keys) { $cmd.Parameters.AddWithValue("@$k", $params[$k]) | Out-Null }
    $cmd.ExecuteNonQuery() | Out-Null
}
function Exec-Scalar([string]$sql) { $cmd = $conn.CreateCommand(); $cmd.CommandText = $sql; return $cmd.ExecuteScalar() }
function New-BeatRow([string]$text) {
    $script:MaxNumber = $script:MaxNumber + 1
    $id = [guid]::NewGuid(); $hash = Sha256Hex $text
    $sql = "INSERT INTO Beats (Id, Text, TextHash, Act, SceneType, Kind, Number, Stale, WasCorrected, IsChapterStart, Version, EntityStale, CreatedAt, UpdatedAt) VALUES (@Id, @Text, @Hash, 0, 'scene', 'prose', @Number, 0, 0, 0, 0, 0, SYSUTCDATETIME(), SYSUTCDATETIME())"
    Exec-NonQuery $sql @{ Id = $id; Text = $text; Hash = $hash; Number = $script:MaxNumber }
    return $id
}
function Add-BeatNode([guid]$nodeId, [guid]$beatId, [double]$sortKey) {
    Exec-NonQuery "INSERT INTO BeatNodes (NodeId, BeatId, SortKey, IsEnabled) VALUES (@NodeId, @BeatId, @SortKey, 1)" @{ NodeId = $nodeId; BeatId = $beatId; SortKey = $sortKey }
}
function Append-ToExistingBeat([guid]$beatId, [string]$extraParagraph) {
    $cmd = $conn.CreateCommand(); $cmd.CommandText = "SELECT Text FROM Beats WHERE Id=@Id"; $cmd.Parameters.AddWithValue("@Id", $beatId) | Out-Null
    $current = $cmd.ExecuteScalar()
    $updated = "$current`n`n$extraParagraph"
    $hash = Sha256Hex $updated
    Exec-NonQuery "UPDATE Beats SET Text=@Text, TextHash=@Hash, UpdatedAt=SYSUTCDATETIME() WHERE Id=@Id" @{ Text = $updated; Hash = $hash; Id = $beatId }
}
function Find-GlossaryBeatId([string]$headingPrefix) {
    $cmd = $conn.CreateCommand()
    $cmd.CommandText = "SELECT TOP 1 b.Id FROM BeatNodes bn JOIN Beats b ON bn.BeatId=b.Id WHERE bn.NodeId='019FA96B-29F5-7BB9-99D0-0F787960E530' AND b.Text LIKE @pat"
    $cmd.Parameters.AddWithValue("@pat", "$headingPrefix%") | Out-Null
    return $cmd.ExecuteScalar()
}
function Try-Append([string]$heading, [string]$extra, [hashtable]$slugMap) {
    $id = Find-GlossaryBeatId $heading
    if ($id) {
        foreach ($slug in $slugMap.Keys) { $extra = $extra.Replace("[[NOTE:$slug]]", "[$($slugMap[$slug])]") }
        Append-ToExistingBeat $id $extra
        Write-Host "Appended to $heading"
    } else { Write-Host "NOT FOUND: $heading" }
}

# ---- Live state ----
$script:MaxNumber = [int](Exec-Scalar "SELECT MAX(Number) FROM Beats")
$NotesNodeId = [guid]"019FA96B-18E2-7BB4-BAEB-11ACA24934F4"
$Ch15NodeId = [guid]"019FA96A-701D-705E-86AB-2A1586EC255D"
$maxNoteSortKey = [double](Exec-Scalar "SELECT ISNULL(MAX(bn.SortKey),0) FROM BeatNodes bn WHERE bn.NodeId='019FA96B-18E2-7BB4-BAEB-11ACA24934F4'")
$maxNoteNumber = [int](Exec-Scalar "SELECT MAX(CAST(LEFT(b.Text, CHARINDEX(' ',b.Text)-1) AS INT)) FROM BeatNodes bn JOIN Beats b ON bn.BeatId=b.Id WHERE bn.NodeId='019FA96B-18E2-7BB4-BAEB-11ACA24934F4'")
Write-Host "Starting: MaxNumber=$script:MaxNumber MaxNoteSortKey=$maxNoteSortKey MaxNoteNumber=$maxNoteNumber"

$em = [char]8212

# ---- Notes ----
$notes = [ordered]@{
'shepherd-flock-economics' = @{ title="A comfortable, not spectacular, flock"; body="Kenneth E. Bailey, Poet and Peasant and Through Peasant Eyes: A Literary-Cultural Approach to the Parables in Luke, combined ed. (Grand Rapids: Eerdmans, 1983), ch. on the Lost Sheep. Bailey argues a hundred-head flock represented a comfortable-but-unspectacular holding for a first-century household, and that flocks of this size were conventionally tended by two or three shepherds together, not one alone $em meaning the parable's solitary search is dramatic compression, not standard practice." }
'drachma-day-wage' = @{ title="A day's income, not incidental change"; body="Nanci DeBloois, 'Coins in the New Testament,' BYU Studies 36, no. 3 (1996-97): 239-251. Notes the drachma and Roman denarius were close but not identical in value, commonly reckoned at roughly four drachmas to three denarii, with the denarius functioning as the standard day's wage for unskilled labor (cf. Matthew 20:2) $em the woman's lost coin in Luke 15:8-9 represented close to a full day's income." }
'lost-coin-headdress-critique' = @{ title="A Bedouin custom, not a first-century one"; body="Paula Gooder, The Parables (London: Canterbury Press, 2021), discussion of Luke 15:8-10; compare Kenneth E. Bailey's original headdress-dowry claim in Poet and Peasant. Gooder points out that coin headdresses are documented specifically as a Bedouin custom, while the woman in Luke's parable is explicitly a settled householder who sweeps a floor $em a mismatch undercutting the popular reading of the coins as headdress dowry." }
'weeden-bailey-critique' = @{ title="A methodological warning about reading the present into the past"; body="Theodore J. Weeden Sr., 'Kenneth Bailey's Theory of Oral Tradition: A Theory Contested by Its Evidence,' Journal for the Study of the Historical Jesus 7 (2009): 3-43. Argues Bailey's broader method of reading twentieth-century Middle Eastern village and Bedouin culture back onto first-century Judea and Galilee treats two millennia of social change as though village life had been preserved unchanged." }
'mishnah-inheritance-transfer' = @{ title="A father could legally distribute his estate early"; body="Mishnah, Bava Batra 8:7, trans. Herbert Danby (Oxford: Oxford University Press, 1933). Permits a father to assign his estate to a son to take effect immediately or after his death, provided the transfer follows the required legal form $em confirming a father distributing property while alive was a recognized legal category, distinct from a son unilaterally demanding his share early." }
'deut-double-portion' = @{ title="The firstborn's legally protected double share"; body="Deuteronomy 21:15-17. Requires a father to grant his firstborn son a double portion of the estate regardless of personal favoritism. This is the legal backdrop against which both the younger son's advance request (15:12) and the older brother's grievance (15:29) would have registered for a first-century audience." }
'kezazah-custom' = @{ title="A later ritual, not confirmed for the first century"; body="Kenneth E. Bailey, The Cross and the Prodigal, rev. ed. (Downers Grove, IL: InterVarsity Press, 2005), for the original kezazah ('cutting-off') claim sourced to the Jerusalem Talmud; the earliest textual attestations of the ceremony come from rabbinic compilations of the third through seventh centuries CE $em centuries after the Gospels' setting, with no first-century source confirming the ritual directly." }
'joseph-robe-ring' = @{ title="A ring as a seal of authority, not jewelry"; body="Genesis 41:42. Pharaoh removes his own signet ring and places it on Joseph's hand, clothing him in fine linen, as the literal ceremonial act of transferring delegated royal authority $em the ring functioning as a seal for certifying documents, not ornamental jewelry. The closest attested Near Eastern textual parallel for reading the father's ring in Luke 15:22 as a marker of restored authority." }
'fattened-calf-hospitality' = @{ title="A well-attested festive-slaughter pattern"; body="Genesis 18:7. Abraham has a servant prepare a choice, tender calf for three unexpected visitors, establishing a festive-slaughter-for-honored-guests pattern attested across the Hebrew Bible independent of any single scholar's field observation." }
}

# ---- Chapter beats ----
$beat1 = @"
The chapter opens on a seating arrangement rather than a sermon: "tax collectors and sinners" pressing close enough to listen, and "the Pharisees and the scribes" grumbling at the company Jesus keeps (15:1-2) -- the same two opposed camps this book has already tracked since Galilee. Jesus answers with three stories about searching, and the first is pastoral. "What man of you, having a hundred sheep, if he loses one of them, does not leave the ninety-nine in the wilderness and go after the one that is lost?" (15:4). The number is not a storyteller's round figure -- a hundred head was a real and checkable quantity for the period, substantial but not spectacular. Kenneth Bailey's fieldwork among rural Middle Eastern shepherding communities led him to argue that flocks approaching a hundred animals were conventionally minded by two or three shepherds working in tandem, not one [[NOTE:shepherd-flock-economics]]. That creates a small, honest wrinkle in the parable's own logic: if cooperative herding was the norm for a flock this size, the picture of one man alone abandoning ninety-nine sheep to chase a single animal is already a dramatic compression -- Jesus is telling a story about total, personal search, not filing a field report on standard practice.
"@

$beat2 = @"
The second parable moves indoors. "What woman, having ten silver coins, if she loses one coin, does not light a lamp and sweep the house and seek diligently until she finds it?" (15:8). Luke's Greek names the coin as a drachma, close in value to a Roman denarius -- the standard day's wage named directly in Jesus's own vineyard parable (Matthew 20:2) [[NOTE:drachma-day-wage]]. So the loss is not trivial: something in the range of a full day's income, plausible grounds for sweeping a house by lamplight.

Here the popular tradition needs a harder look than it usually gets. Decades of sermons, and Kenneth Bailey's influential academic treatment, have read the ten coins as sewn onto a headdress a married woman wore as her visible dowry: lose one, and you've damaged the one asset that is legally hers. It's a vivid image, and it is not well anchored in first-century evidence. Bailey's authority for the custom was decades of ethnographic observation of twentieth-century village and Bedouin women -- real fieldwork, but nineteen centuries removed from Luke. Biblical scholar Paula Gooder has made the mismatch explicit: coin headdresses of this kind are documented as a Bedouin custom, while the woman in Luke's parable is unmistakably a householder -- she sweeps a floor, lights a lamp, calls in neighbors -- not a tent-dwelling pastoralist [[NOTE:lost-coin-headdress-critique]]. The deeper methodological problem, that reading twentieth-century village life back onto the first century treats two thousand years as though nothing changed, was argued at length by Theodore Weeden in a sustained scholarly critique of Bailey's method generally [[NOTE:weeden-bailey-critique]]. None of this proves the coins weren't a dowry -- Luke's text is silent on the point. What the record doesn't support is the specific, widely repeated image of a coin headdress as an attested first-century Judean custom.
"@

$beat3 = @"
The third parable is longer and the checkable ground under it is denser. "Father, give me the share of property that is coming to me" (15:12). Under codified Jewish law a father did have latitude to distribute his own estate while alive -- the Mishnah allows a man to assign property to a son to take effect immediately or on his death, so long as the transfer follows proper form [[NOTE:mishnah-inheritance-transfer]] -- so the request wasn't a legal impossibility. But there is a wide gulf between a father electing to distribute assets early and a younger son unilaterally demanding his cut in advance, jumping ahead of an older brother who under Deuteronomy's rule stood to receive a double portion as firstborn [[NOTE:deut-double-portion]]. Interpreters working from Middle Eastern honor-culture norms have read the younger son's request as, in social terms, tantamount to wishing his father already dead. Bailey pushed the claim further, proposing that a son who squandered his inheritance abroad risked a public shaming ritual called the kezazah, "the cutting-off." The trouble is dating: the rabbinic material describing kezazah was compiled centuries after Jesus, and critics have called treating it as first-century practice an overreach [[NOTE:kezazah-custom]]. The honest position: the shame of the request was certainly real by the culture's own logic; the specific ritual is not independently confirmed for the first century.

Then the son takes his money "into a far country" and wastes it, and famine strikes, and he ends up feeding pigs, so hungry he envies the animals' feed (15:13-16). For a Jewish son specifically, tending pigs, an animal Torah names unclean (Leviticus 11:7; Deuteronomy 14:8), was about as complete a picture of degradation as Jesus's audience could be handed in two verses -- this book's own Gerasene demoniac material already established exactly why pigs functioned as that marker.

The turn comes fast: "while he was still a long way off, his father saw him and ran and embraced him and kissed him" (15:20). What follows is solidly grounded in real Near Eastern vocabulary of restoration: "bring quickly the best robe... put a ring on his hand... and bring the fattened calf" (15:22-23). The closest direct biblical parallel for the robe and ring together is Genesis 41:42, where Pharaoh transfers his own signet ring onto Joseph's hand as the literal instrument of investing him with delegated authority -- the ring not jewelry but a seal used to certify documents [[NOTE:joseph-robe-ring]]. The fattened calf belongs to a still older and more widely attested pattern of festive slaughter for honored arrivals, paralleled directly in Genesis 18:7, where Abraham has a choice calf prepared for three unexpected visitors [[NOTE:fattened-calf-hospitality]] -- unlike the coin-headdress or kezazah claims, this practice rests on multiple independent textual attestations rather than one scholar's field observation.

The older son's complaint leans on an assumption the text never states outright but the period's inheritance law makes legible: as firstborn he stood to receive a double portion of whatever estate remained, and had stayed and worked that estate the entire time (15:29). His anger isn't petty by the economics of his own culture -- a fattened calf meant meat for a substantial gathering, a real expenditure against household resources he might reasonably expect to see reflected in his own protected share.
"@

$beats = @($beat1, $beat2, $beat3)

# ---- Insert Notes ----
$slugToNumber = @{}
foreach ($slug in $notes.Keys) {
    $n = $notes[$slug]
    $noteNum = $maxNoteNumber + 1
    $maxNoteNumber = $noteNum
    $text = "$noteNum $em $($n.title)`n`n$($n.body)"
    $id = New-BeatRow $text
    $maxNoteSortKey += 50
    Add-BeatNode $NotesNodeId $id $maxNoteSortKey
    $slugToNumber[$slug] = $noteNum
}
Write-Host "Inserted $($notes.Count) notes, ending at [$maxNoteNumber]"

# ---- Insert chapter beats ----
$sortKey = 0.0
foreach ($beatText in $beats) {
    $resolved = $beatText
    foreach ($slug in $slugToNumber.Keys) { $resolved = $resolved.Replace("[[NOTE:$slug]]", "[$($slugToNumber[$slug])]") }
    $sortKey += 100
    $id = New-BeatRow $resolved
    Add-BeatNode $Ch15NodeId $id $sortKey
}
Write-Host "Inserted $($beats.Count) chapter beats"

# ---- No new glossary entries this chapter (all figures unnamed parable characters) ----
# ---- No glossary flags with new substantive claims beyond what's already covered ----

$conn.Close()
Write-Host "DONE Chapter 15."
