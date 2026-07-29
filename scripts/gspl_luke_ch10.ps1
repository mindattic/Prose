$OutputEncoding = [System.Text.UTF8Encoding]::new($false)
[Console]::OutputEncoding = [System.Text.UTF8Encoding]::new($false)
Add-Type -AssemblyName System.Data

$connStr = "Server=(localdb)\MSSQLLocalDB;Database=StreetSamurai;Trusted_Connection=True;TrustServerCertificate=True;"
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
function Seed-Entity([string]$name, [string]$slug, [string]$type, [string]$desc) {
    $exists = Exec-Scalar "SELECT COUNT(*) FROM Entities WHERE UniverseId='0197E9C9-0003-7000-8000-000000000003' AND Slug='$slug'"
    if ($exists -gt 0) { Write-Host "  entity exists, skip: $name"; return }
    $id = [guid]::NewGuid()
    Exec-NonQuery "INSERT INTO Entities (Id, EntityType, Name, Slug, Status, Description, CreatedAt, ModifiedAt, IsActive, UniverseId) VALUES (@Id, @Type, @Name, @Slug, 'canon', @Desc, SYSUTCDATETIME(), SYSUTCDATETIME(), 1, '0197E9C9-0003-7000-8000-000000000003')" @{ Id = $id; Type = $type; Name = $name; Slug = $slug; Desc = $desc }
    Write-Host "  seeded entity: $name"
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
$GlossaryNodeId = [guid]"019FA96B-29F5-7BB9-99D0-0F787960E530"
$Ch10NodeId = [guid]"019FA96A-17B1-74B5-9EF1-2DBF1B6097A2"
$maxNoteSortKey = [double](Exec-Scalar "SELECT ISNULL(MAX(bn.SortKey),0) FROM BeatNodes bn WHERE bn.NodeId='019FA96B-18E2-7BB4-BAEB-11ACA24934F4'")
$maxGlossarySortKey = [double](Exec-Scalar "SELECT ISNULL(MAX(bn.SortKey),0) FROM BeatNodes bn WHERE bn.NodeId='019FA96B-29F5-7BB9-99D0-0F787960E530'")
$maxNoteNumber = [int](Exec-Scalar "SELECT MAX(CAST(LEFT(b.Text, CHARINDEX(' ',b.Text)-1) AS INT)) FROM BeatNodes bn JOIN Beats b ON bn.BeatId=b.Id WHERE bn.NodeId='019FA96B-18E2-7BB4-BAEB-11ACA24934F4'")
Write-Host "Starting: MaxNumber=$script:MaxNumber MaxNoteSortKey=$maxNoteSortKey MaxNoteNumber=$maxNoteNumber MaxGlossarySortKey=$maxGlossarySortKey"

$em = [char]8212

# ---- Notes ----
$notes = [ordered]@{
'luke10-72-manuscript-split' = @{ title="Seventy or seventy-two? The manuscripts disagree"; body="Bruce M. Metzger, A Textual Commentary on the Greek New Testament, 2nd ed. (Stuttgart: Deutsche Bibelgesellschaft / United Bible Societies, 1994), ad loc. Luke 10:1, 17. Metzger canvasses the manuscript split $em 'seventy-two' attested in P75, Codex Vaticanus, and Codex Bezae against 'seventy' in Codex Sinaiticus and Codex Alexandrinus $em and concludes the original number 'cannot be determined with confidence,' while noting the editorial preference for 'seventy-two' as the harder reading." }
'seventy-nations-symbolism' = @{ title='A symbolic number, not a headcount'; body="Numbers 11:16-25 (Moses's seventy elders) and Genesis 10 (the Table of Nations, seventy names in the Hebrew text, seventy-two in the Greek Septuagint); Joseph A. Fitzmyer, The Gospel According to Luke X-XXIV, Anchor Bible 28A (Garden City, NY: Doubleday, 1985), ad loc. 10:1. The number likely functions as a symbol of a mission with a universal, all-nations horizon rather than a verifiable historical headcount." }
'chorazin-archaeology-scale' = @{ title='A modest village behind harsh rhetoric'; body="Excavation reports on Chorazin by Ze'ev Yeivin, Israel Antiquities Authority excavation seasons 1962-1965 and 1980-1986, summarized in Biblical Archaeology Society surveys. Findings: a basalt-built village of roughly twenty-five acres centered on a monumental synagogue dated to the late third or early fourth century CE; no undisputed first-century occupation stratum has been confirmed beneath the later synagogue." }
'bethsaida-location-debate' = @{ title='Which mound is Bethsaida?'; body="Rami Arav et al., excavation reports on et-Tell, against Mordechai Aviam, R. Steven Notley, and Dina Shalem, excavation reports on el-Araj, summarized in the Biblical Archaeology Society feature 'The Great Bethsaida Debate.' Et-Tell proponents identify it on continuity-of-occupation grounds; el-Araj proponents argue et-Tell sits too far from the shoreline and lacks confirmed first-century structures, whereas el-Araj has produced early Roman material including a bathhouse and a coin dated 65-66 CE." }
'jericho-road-elevation-distance' = @{ title="A thousand-meter drop in seventeen miles"; body="Standard geographic survey elevation data for Jerusalem (approximately 754m above sea level) and Jericho (approximately -258m); Flavius Josephus, The Jewish War 4.8, describes the intervening country as 'desert and stony' and gives the distance as roughly one hundred fifty stadia, independently corroborating the terrain's barren character." }
'jericho-road-banditry' = @{ title="A notoriously dangerous road, independently documented"; body="Flavius Josephus, Antiquities of the Jews 14.15.5 and The Jewish War 1.16 (robbers using cave hideouts in the region's ravines); Inn of the Good Samaritan Museum site documentation (opened June 4, 2009, near Ma'ale Adumim). The corridor including Wadi Qelt and the Ascent of Adummim carried a documented reputation for banditry from the Hellenistic-Roman period onward, evidenced by successive fortified way stations rebuilt there across the Roman, Byzantine, Crusader, and Ottoman periods." }
'good-samaritan-inn-tradition' = @{ title='A later commemorative site, not a verified location'; body="Israel Antiquities Authority / Israel Nature and Parks Authority, Inn of the Good Samaritan Museum documentation. The site's continuous use as a way station from the Roman through Ottoman periods, now displaying regional mosaics, establishes it as a later traditional commemorative location on the historically dangerous road, not an archaeologically verified location of any specific incident." }
'samaritan-jewish-hostility' = @{ title='Hostility and practical coexistence side by side'; body="Flavius Josephus, Antiquities of the Jews 20.118 and surrounding narrative (the attack on Galilean pilgrims near Ginea, temple defilement, mutual village-burning), contrasted with Josephus's own statement that Galilean pilgrims customarily traveled through Samaritan territory to Jerusalem festivals. Documented, serious mutual hostility coexisted with ongoing practical travel through Samaritan territory." }
'corpse-impurity-law' = @{ title="The real legal logic behind the priest's and Levite's avoidance"; body="Numbers 19:11-13 (seven-day impurity from corpse contact) and Leviticus 21:1-4 (additional restriction barring ordinary priests from corpse contact except for immediate family). The real legal framework available to a first-century Jewish audience for evaluating the priest's and Levite's avoidance in the parable." }
'levine-good-samaritan-critique' = @{ title="Levine's challenge to the purity-law excuse"; body="Amy-Jill Levine, Short Stories by Jesus: The Enigmatic Parables of a Controversial Rabbi (New York: HarperOne, 2014), chapter 'The Good Samaritan.' Levine argues the purity-law excuse for the priest and Levite is overstated (ordinary Israelites faced no such restriction, and the priest is traveling away from Jerusalem), and that the parable's real shock lay in naming a Samaritan as the story's moral exemplar." }
'sitting-at-feet-idiom' = @{ title='A technical idiom for formal discipleship'; body="Mishnah, Avot 1:4 (attributed to Yose ben Yoezer): 'Let your house be a meeting-place for the sages, and sit amid the dust of their feet.' Cited alongside Acts 22:3 (Paul 'brought up at the feet of Gamaliel') as evidentiary basis for reading 'sitting at the feet' as a recognized idiom for formal rabbinic discipleship, ordinarily occupied by male students." }
'hospitality-duty-custom' = @{ title='Hospitality as a serious social duty'; body="Victor H. Matthews, 'Hospitality and Hostility in Genesis 19 and Judges 19,' Biblical Theology Bulletin 21, no. 1 (1991): 13-21. Documents the ancient Near Eastern and Israelite framework in which hosting a traveler was a binding social and religious obligation, providing the cultural background against which Martha's domestic labor should be read as a genuinely valued duty." }
'luke-bethany-geography-tension' = @{ title="Is this the same Martha and Mary as Bethany?"; body="Discussed across multiple critical treatments of the Synoptic-Johannine relationship, including George Ogg's source-critical proposal that Luke drew on a source describing the same Bethany incident narrated independently in John 11-12. Luke's own narrative geography at 10:38 gives no explicit indication the unnamed village is Bethany; scholars remain divided over whether the traditions describe the same incident, independent traditions, or later harmonization." }
'martha-bethany-schrader' = @{ title="A textual-critical wrinkle: was Martha added later?"; body="Elizabeth Schrader, 'Was Martha of Bethany Added to the Fourth Gospel in the Second Century?,' Harvard Theological Review 110, no. 3 (2017): 360-392. Schrader's examination of early witnesses to John 11-12 identifies textual instability around Martha's name, arguing the Lazarus narrative may originally have featured only Mary and that 'Martha' was introduced at a later stage." }
}

# ---- Chapter beats ----
$beat1 = @"
Luke reports that Jesus, having set his face toward Jerusalem, appointed a body of disciples and sent them ahead of him "two by two into every town and place where he himself was about to go" (10:1), instructed them to travel without purse, bag, or sandals, to heal the sick and proclaim that "the kingdom of God has come near," and to shake the dust from their feet against any town that refused them (10:2-12). On their return the disciples report that "even the demons submit to us in your name" (10:17), prompting Jesus's declaration "I saw Satan fall like lightning from heaven" (10:18).

The headcount itself is genuinely uncertain: at both 10:1 and 10:17, the oldest and best witnesses split down the middle between "seventy" and "seventy-two" [[NOTE:luke10-72-manuscript-split]]. Why would either number matter enough for scribes to have altered it? Almost certainly because seventy (and, in the Greek Septuagint's longer name-list, seventy-two) was already a loaded number in Jewish tradition: it recalls the seventy elders Moses gathered in Numbers 11:16-25, and the Table of Nations of Genesis 10 [[NOTE:seventy-nations-symbolism]]. The mainstream historical-critical reading treats this echo as the real point: whichever number Luke originally wrote, he is signaling a mission with a universal, all-the-nations horizon.

The second turn in this unit is sharper. Jesus pronounces judgment on three Galilee-area towns: "Woe to you, Chorazin! Woe to you, Bethsaida! ... it will be more tolerable for Sodom" (10:13-15). Chorazin is a genuine, excavated site $em a basalt-built Galilean village with a monumental synagogue, first systematically excavated in 1906-1909 and again in the 1960s-1980s [[NOTE:chorazin-archaeology-scale]]. What excavation has actually confirmed is a modest agricultural village $em the surviving synagogue itself dates to the late third or early fourth century, well after Jesus's generation, and archaeologists have had real difficulty confirming an undisputed first-century occupation layer beneath it at all. Bethsaida compounds the problem differently $em its very location is still disputed among working archaeologists, between et-Tell and el-Araj [[NOTE:bethsaida-location-debate]]. Before any question of what happened at Bethsaida can be asked, historians have not yet agreed on which mound is Bethsaida.
"@

$beat2 = @"
A legal expert tests Jesus with the question of eternal life, is directed to the double command of love of God and neighbor, and presses further: "And who is my neighbor?" (10:29). Jesus answers with a story: a man travels "down" from Jerusalem to Jericho, falls among robbers who strip, beat, and leave him "half dead" (10:30), and is passed by in turn by a priest and a Levite, before a Samaritan stops, bandages his wounds, and pays for his continued care (10:31-35).

The geography here is not incidental color $em "went down" is literally accurate. Jerusalem sits roughly 754 meters above sea level; Jericho sits about 258 meters below it $em a drop of roughly a thousand meters over a road of some seventeen to eighteen miles [[NOTE:jericho-road-elevation-distance]]. The road's reputation for danger is likewise independently documented: the gorge of Wadi Qelt and the Ascent of Adummim were notorious for banditry through antiquity, evidenced by successive fortified way stations built and rebuilt along it from the Roman through Ottoman periods [[NOTE:jericho-road-banditry]]. A modern museum on that same site $em the Inn of the Good Samaritan, opened in 2009 $em now marks how firmly later tradition attached itself to this stretch of real road, even though nothing ties any specific incident to that specific spot [[NOTE:good-samaritan-inn-tradition]].

Samaritan-Jewish hostility in this period is also independently attested. Josephus records Samaritans attacking Galilean pilgrims, Samaritans scattering human bones in the Jerusalem sanctuary, and Jews retaliating by burning Samaritan villages $em yet the same source shows Galilean pilgrims using the direct route through Samaria as standing custom, so hostility and practical coexistence ran side by side [[NOTE:samaritan-jewish-hostility]]. Amy-Jill Levine argues that for Jesus's first Jewish audience, naming a Samaritan as the story's hero read less like mild interethnic friction and more like casting a hated enemy as the moral exemplar [[NOTE:levine-good-samaritan-critique]].

The priest's and Levite's avoidance deserves a fair hearing rather than a flat "callous clergy" read. Numbers 19 lays out real legal consequences for corpse contact, and Leviticus 21:1-4 places an additional restriction specifically on ordinary priests [[NOTE:corpse-impurity-law]]. That is a real legal logic available to Luke's audience, not invented modern apologetics $em though Levine and others push back on how far this excuse should be stretched, since ordinary Israelites faced no such prohibition at all and the text never actually states the two men's motive.
"@

$beat3 = @"
Jesus enters an unnamed village where a woman named Martha welcomes him into her home; her sister Mary "sat at the Lord's feet, listening to what he said," while Martha, "distracted by all the preparations," complains that Mary has left her to serve alone; Jesus answers that Mary "has chosen what is better" (10:38-42).

"Sitting at the feet" is a recognized technical idiom in this period for formal discipleship under a named teacher: rabbis customarily sat on a raised seat while their students sat on the floor at their feet, a phrase directly attested in Mishnah Avot 1:4 [[NOTE:sitting-at-feet-idiom]]. Read against that background, Mary's posture is doing real cultural work $em women occupying the formal student's position at a teacher's feet was not the ordinary social default. Martha's labor deserves the same fair reading: hospitality toward a guest was a serious social and religious duty in this period, not a background chore [[NOTE:hospitality-duty-custom]]. The pericope's tension is best read as a conflict between two genuinely valued goods rather than a simple morality tale.

One open question is worth flagging honestly: Luke does not name this village, and nothing in his narrative geography at this point identifies it as Bethany, where John's Gospel places a Martha and Mary in a strikingly similar scene [[NOTE:luke-bethany-geography-tension]]. A further textual-critical wrinkle sharpens rather than settles the puzzle: a 2017 study of the earliest surviving papyrus of John's Gospel found instability around Martha's name, raising the question of whether "Martha" was a later addition to that Johannine story [[NOTE:martha-bethany-schrader]].
"@

$beats = @($beat1, $beat2, $beat3)

# ---- New glossary entries (unique to ch10, excluding BETHSAIDA and SAMARITANS which already exist from ch9) ----
$glossary = [ordered]@{
'CHORAZIN' = "A Galilean village roughly two and a half miles north of the Sea of Galilee, named by Jesus alongside Bethsaida and Capernaum in a formula of judgment against towns that saw his works and did not repent (10:13). Excavated across the twentieth century, the site preserves a basalt-built village of about twenty-five acres centered on a monumental synagogue $em but that synagogue dates to the late third or early fourth century CE, and excavators have not confirmed an undisputed first-century occupation layer beneath it [[NOTE:chorazin-archaeology-scale]]."
'JERICHO' = "An ancient city in the Jordan Valley, at roughly 258 meters below sea level among the lowest permanently inhabited elevations on Earth, and the destination of the traveler in the Good Samaritan parable (10:30). Jerusalem sits roughly a thousand meters higher over a road of some seventeen to eighteen miles [[NOTE:jericho-road-elevation-distance]]."
'WADI QELT / ASCENT OF ADUMMIM' = "The steep desert gorge and adjoining pass the ancient Jerusalem-Jericho road followed, descending nearly a thousand meters over the route's length. Independently documented as bandit-prone across antiquity, evidenced by successive fortified way stations built and rebuilt along it [[NOTE:jericho-road-banditry]]."
'MARTHA (OF BETHANY)' = "A woman who welcomes Jesus into her home in an unnamed village and is later described as `"distracted by all the preparations`" while her sister Mary sits and listens to Jesus (10:38-42). Possibly, though not certainly, identical with the Martha of John 11-12 at Bethany [[NOTE:luke-bethany-geography-tension]] [[NOTE:martha-bethany-schrader]]."
'MARY (OF BETHANY)' = "Martha's sister, who `"sat at the Lord's feet, listening to what he said`" (10:39), a posture identified as the technical idiom for formal rabbinic discipleship in this period [[NOTE:sitting-at-feet-idiom]]. Distinguished from Mary the mother of Jesus and from Mary Magdalene."
'THE SEVENTY(-TWO)' = "The group of disciples Jesus appoints and sends out in pairs ahead of him (10:1), distinct from the twelve apostles named earlier in Luke. The Greek manuscript tradition is evenly split between `"seventy`" and `"seventy-two`" [[NOTE:luke10-72-manuscript-split]], a number likely carrying symbolic weight from the seventy elders of Numbers 11 and the nations of Genesis 10 [[NOTE:seventy-nations-symbolism]]."
}

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
    Add-BeatNode $Ch10NodeId $id $sortKey
}
Write-Host "Inserted $($beats.Count) chapter beats"

# ---- Insert new glossary entries ----
foreach ($heading in $glossary.Keys) {
    $body = $glossary[$heading]
    foreach ($slug in $slugToNumber.Keys) { $body = $body.Replace("[[NOTE:$slug]]", "[$($slugToNumber[$slug])]") }
    $text = "$heading`n`n$body"
    $id = New-BeatRow $text
    $maxGlossarySortKey += 50
    Add-BeatNode $GlossaryNodeId $id $maxGlossarySortKey
}
Write-Host "Inserted $($glossary.Count) glossary entries"

# ---- Append new claims to existing glossary beats ----
Try-Append "BETHSAIDA" "This chapter adds: named alongside Chorazin in the same woe-formula (10:13-15), condemned in the same breath as Sodom [[NOTE:bethsaida-location-debate]]." $slugToNumber
Try-Append "SAMARITANS" "This chapter adds: the Good Samaritan parable's hero is identified only by this group membership (10:33), against a documented backdrop of mutual hostility that coexisted with practical travel through Samaritan territory [[NOTE:samaritan-jewish-hostility]]." $slugToNumber
Try-Append "CAPERNAUM" "This chapter adds: named in the same woe-triad as Chorazin and Bethsaida (10:15), a useful comparative-footprint point once Capernaum's own better-attested first-century archaeology is considered." $slugToNumber
Try-Append "FLAVIUS JOSEPHUS" "This chapter adds two citations: Antiquities 20.118 (the Ginea attack on Galilean pilgrims, documenting Samaritan-Jewish hostility) [[NOTE:samaritan-jewish-hostility]] and Jewish War 4.8 (the Jordan Valley/Jericho terrain description and distance figures) [[NOTE:jericho-road-elevation-distance]]." $slugToNumber

# ---- Seed new entities ----
Seed-Entity "Chorazin" "chorazin" "place" "Galilean village near the Sea of Galilee, condemned by Jesus in a woe-formula alongside Bethsaida and Capernaum (Luke 10:13)."
Seed-Entity "Wadi Qelt / Ascent of Adummim" "wadi-qelt-ascent-of-adummim" "place" "Steep desert gorge and pass carrying the ancient Jerusalem-Jericho road, historically bandit-prone."

$conn.Close()
Write-Host "DONE Chapter 10."
