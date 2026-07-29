$ErrorActionPreference = 'Stop'   # fail loudly: a SqlException is otherwise non-terminating
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
$Ch20NodeId = [guid]"019FA96A-C70E-7958-AB73-945FD5159F40"
$maxNoteSortKey = [double](Exec-Scalar "SELECT ISNULL(MAX(bn.SortKey),0) FROM BeatNodes bn WHERE bn.NodeId='019FA96B-18E2-7BB4-BAEB-11ACA24934F4'")
$maxGlossarySortKey = [double](Exec-Scalar "SELECT ISNULL(MAX(bn.SortKey),0) FROM BeatNodes bn WHERE bn.NodeId='019FA96B-29F5-7BB9-99D0-0F787960E530'")
$maxNoteNumber = [int](Exec-Scalar "SELECT MAX(CAST(LEFT(b.Text, CHARINDEX(' ',b.Text)-1) AS INT)) FROM BeatNodes bn JOIN Beats b ON bn.BeatId=b.Id WHERE bn.NodeId='019FA96B-18E2-7BB4-BAEB-11ACA24934F4'")
Write-Host "Starting: MaxNumber=$script:MaxNumber MaxNoteSortKey=$maxNoteSortKey MaxNoteNumber=$maxNoteNumber MaxGlossarySortKey=$maxGlossarySortKey"

$em = [char]8212

# ---- Notes ----
$notes = [ordered]@{
'sanhedrin-composition' = @{ title="Priests, elders, scribes: the coalition behind the challenge"; body="Encyclopaedia Britannica, 'Sanhedrin,' and Mishnah Sanhedrin 1:6 (redacted c. 200 CE, describing earlier practice). The Great Sanhedrin's seats drew from high-priestly families, lay elders (heads of prominent aristocratic households), and scribes trained in Torah law, meeting as Jerusalem's supreme council under Roman oversight $em the three-office coalition matches Luke's framing exactly." }
'isaiah-vineyard-song' = @{ title="A parable built on Isaiah's own vineyard"; body="Isaiah 5:1-7 (the Song of the Vineyard), cross-referenced against Luke 20:9-19. The parable's opening formula $em a man who plants, hedges, and leases out a vineyard before departing $em recalls Isaiah's Song of the Vineyard almost verbatim, a deliberate echo Jesus's original audience would have recognized instantly given the vineyard's established use as a metaphor for Israel." }
'wicked-tenants-allegory-debate' = @{ title="Original parable, or later allegorical elaboration?"; body="Klyne Snodgrass, The Parable of the Wicked Tenants, WUNT 27 (Tubingen: J.C.B. Mohr, 1983); Adolf Julicher, Die Gleichnisreden Jesu (1888/1899). Scholars remain divided on whether the parable's close allegorical mapping onto Jesus's death reflects his own telling or later Christian shaping; Snodgrass and John Meier argue for substantial authenticity, against the older Julicher-influenced view that treated close allegory as proof of post-crucifixion composition." }
'galilee-absentee-estates' = @{ title="A real, documented shift toward absentee landlordism"; body="Sakari Hakkinen, 'Poverty in the First-Century Galilee,' HTS Teologiese Studies/Theological Studies 72, no. 4 (2016). Documents a shift across first-century Judea and Galilee, evidenced in papyri from Nahal Hever and Wadi Murabba'at, toward large landholdings under absentee owners worked by tenant farmers falling into debt under harsh lease terms $em precisely the social friction the parable assumes its listeners will recognize instantly." }
'tribute-penny-inscription' = @{ title="A coin claiming divine sonship"; body="Doug Smith, 'Tiberius: The Tribute Penny,' forumancientcoins.com. The standard 'Tribute Penny' denarius of Tiberius (struck c. 15-37 CE, chiefly at Lugdunum) carries the obverse legend TI CAESAR DIVI AVG F AVGVSTVS ('Tiberius Caesar Augustus, son of the deified Augustus') around Tiberius's laureate portrait." }
'jewish-coin-aniconism' = @{ title="Judean coinage deliberately avoided ruler portraits"; body="Biblical Archaeology Society, ''Render Unto Caesar' and the First Jewish Revolt.' Hasmonean and Herodian mints in Judea avoided human and animal imagery out of deference to Jewish prohibitions on graven images, and Jewish Revolt-era coinage (66-70 CE) deliberately used aniconic motifs in place of any ruler's portrait $em against that backdrop, a denarius bearing a divine-filiation claim was a genuine point of religious friction." }
'levirate-marriage-law' = @{ title="A real, formally legislated obligation"; body="Dvora E. Weisberg, 'The Widow of Our Discontent: Levirate Marriage in the Bible and Ancient Israel,' Journal for the Study of the Old Testament 28, no. 4 (2004): 403-429. Deuteronomy 25:5-10 obligates a surviving brother to marry his deceased, childless brother's widow so the dead man's name will not be blotted out of Israel, with a formal public escape clause (sandal removal and spitting) for a brother who refused." }
'psalm-110-authorship' = @{ title="A royal psalm of disputed authorship"; body="Hans-Joachim Kraus, Psalms 60-150: A Continental Commentary, trans. Hilton C. Oswald (Minneapolis: Augsburg Fortress, 1989), entry on Psalm 110. Classifies the psalm among the kingship/royal psalms tied to ancient Near Eastern enthronement ideology; most historical-critical scholars deny strict Davidic authorship despite the 'of David' superscription, with no firm consensus on the psalm's actual date of origin." }
}

# ---- Chapter beats ----
$beat1 = @"
Luke sets the whole chapter inside a single confrontation in the temple courts: "the chief priests and the scribes with the elders" (20:1) come as one body to challenge Jesus's authority. This is the same three-office coalition this book has already traced as the practical government of Judea under Rome -- priestly aristocracy, propertied lay elders, and professional scribes, meeting together as the Sanhedrin [[NOTE:sanhedrin-composition]]. Jesus answers a question with a question -- where did John's baptism come from? -- and traps the coalition in its own political calculation: endorse John and answer for not following him; deny him and face the crowd that "were all convinced that John was a prophet" (20:6). The exchange turns entirely on the credibility the Baptist has already earned in Luke's narrative.
"@

$beat2 = @"
The parable that follows (20:9-19) borrows its entire scaffolding from a text every listener in the temple would have known by heart. "A man planted a vineyard, leased it to tenants, and went to another country for a long time" (20:9) reworks, almost line for line, the opening of Isaiah's Song of the Vineyard [[NOTE:isaiah-vineyard-song]]. Isaiah's song ends with the vineyard itself turning wild and unproductive, a metaphor for a nation's failure to produce justice; Jesus's version keeps the vineyard fruitful and relocates the failure onto the tenants who withhold the owner's due and escalate from insult to murder. Whether the parable's ending was part of Jesus's original telling or an allegorical elaboration added after the crucifixion is a live question inside historical-Jesus scholarship [[NOTE:wicked-tenants-allegory-debate]]. Separate from that literary question is an economic one: the parable's premise -- a landowner living elsewhere while tenant farmers work his vineyard under a lease that can turn adversarial -- was not narrative invention. Documentary and legal evidence from the Roman period shows a real and accelerating shift across first-century Judea and Galilee toward large estates held by absentee owners and worked by tenants falling into debt under harsh lease terms [[NOTE:galilee-absentee-estates]].
"@

$beat3 = @"
The coalition's next move (20:20-26) is a coin trap, and the coin itself is real and identifiable. The "denarius" (20:24) Jesus asks to see was, by the reign of Tiberius, a well-documented silver issue whose obverse read TI CAESAR DIVI AVG F AVGVSTVS -- "Tiberius Caesar, Augustus, son of the deified Augustus" -- around a laureate portrait of the emperor [[NOTE:tribute-penny-inscription]]. That inscription was not decorative: it asserted, on a coin a person might carry daily, that the previous emperor had become a god and the current one was his divine son -- a genuine point of friction for observant Jews. The friction was concrete enough that Judea's own coinage record shows deliberate avoidance: Hasmonean and Herodian mints, and later the coins struck during the First Jewish Revolt, stuck to plants and vessels rather than risk minting a ruler's face at all [[NOTE:jewish-coin-aniconism]]. Jesus's answer -- "Render to Caesar the things that are Caesar's, and to God the things that are God's" -- lands as a dodge of the coalition's trap, but it is staged over a coin whose imagery made the question of possessing and handling it a live religious irritant.
"@

$beat4 = @"
The Sadducees who step forward next (20:27-40) are already established in this book as the party denying bodily resurrection, in contrast to the Pharisees' affirmation of it -- this chapter adds only the specific device they deploy to make resurrection look absurd. Their hypothetical -- seven brothers, one widow, each dying childless in turn -- is built directly on a real, named legal institution: the levirate marriage law of Deuteronomy 25:5-10 [[NOTE:levirate-marriage-law]]. The Sadducees' scenario, sequentially exhausting seven brothers, is a rhetorical extreme case built to strain the law past absurdity, not a claim that such a sequence ever occurred. Jesus's answer bypasses the Sadducees' own scriptural ground and argues instead from Exodus's burning-bush designation of God as "the God of Abraham, and the God of Isaac, and the God of Jacob" (20:37) -- an argument from within the Sadducees' own canon.
"@

$beat5 = @"
The counter-question Jesus poses about the Messiah's identity (20:41-44) turns on a single verse, Psalm 110:1, read as David speaking of a "Lord" greater than himself. This is almost entirely an exegetical move rather than an independently checkable historical claim, but the psalm behind it has its own contested history: Psalm 110 is widely classified by critical scholars as a royal enthronement psalm, and while its superscription attributes it "of David," mainstream historical-critical scholarship largely treats that ascription as a traditional liturgical attribution rather than proof of authorship, with no settled consensus on the psalm's actual origin [[NOTE:psalm-110-authorship]]. Luke's Jesus, in the text as written, takes David's authorship as a given; that the psalm's actual origin is scholarly-disputed doesn't undercut the exegetical point Luke is making, but it is worth naming as a gap between the confessional reading and the critical one.
"@

$beat6 = @"
The chapter closes (20:45-47) with a brief public warning against the scribes -- their appetite for long robes, the best seats in synagogues and at banquets, and public honor, paired with the accusation that they "devour widows' houses" while performing long prayers. This sits on top of the scribal profile already built in this book: literate legal professionals whose social status ran ahead of their priestly counterparts in some settings. The "devouring widows' houses" charge is Luke's rhetorical accusation rather than a claim this book can independently verify against an outside record -- no external source catalogs specific instances of scribal exploitation of widows in this period.
"@

$beats = @($beat1, $beat2, $beat3, $beat4, $beat5, $beat6)

# ---- New glossary entries ----
$glossary = [ordered]@{
'SANHEDRIN' = "Jerusalem's seventy-one-member governing council of chief priests, elders, and scribes under Roman-era Judea; the coalition that challenges Jesus's authority in the Temple (20:1) [[NOTE:sanhedrin-composition]]."
'DENARIUS (TRIBUTE PENNY)' = "Roman silver coin bearing Tiberius's portrait and a divine-filiation inscription (TI CAESAR DIVI AVG F AVGVSTVS), at the center of the `"render unto Caesar`" episode (20:20-26) [[NOTE:tribute-penny-inscription]]."
'JEZREEL VALLEY' = "Fertile Galilean plain documented as a site of large Roman-period absentee-owned agricultural estates [[NOTE:galilee-absentee-estates]]."
'LEVIRATE MARRIAGE' = "Deuteronomic legal custom (25:5-10) obligating a surviving brother to marry his deceased, childless brother's widow so the family line and inheritance would continue [[NOTE:levirate-marriage-law]]."
'PSALM 110' = "Royal enthronement psalm quoted by Jesus in the `"whose son is the Christ`" exchange (20:41-44); Davidic authorship disputed in critical scholarship [[NOTE:psalm-110-authorship]]."
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
    Add-BeatNode $Ch20NodeId $id $sortKey
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
Try-Append "ISAIAH" "This chapter adds: Isaiah 5:1-7 (Song of the Vineyard) is the direct literary source echoed almost verbatim by the Parable of the Wicked Tenants' opening (20:9) [[NOTE:isaiah-vineyard-song]]." $slugToNumber
Try-Append "THE SADDUCEES" "This chapter adds: their resurrection challenge (20:27-40) is built specifically on the Deuteronomy 25:5-10 levirate marriage law [[NOTE:levirate-marriage-law]]." $slugToNumber
Try-Append "THE SCRIBES" "This chapter adds: accused in 20:45-47 of seeking public honor and of `"devouring widows' houses`" -- a rhetorical accusation, not an independently attested historical pattern." $slugToNumber
Try-Append "TIBERIUS CAESAR" "This chapter adds: the specific denarius inscription associated with his reign (TI CAESAR DIVI AVG F AVGVSTVS) asserting divine filiation from the deified Augustus [[NOTE:tribute-penny-inscription]]." $slugToNumber
Try-Append "GALILEE" "This chapter adds: documented site of Roman-period absentee-landlord estate agriculture (Jezreel Valley), the economic backdrop assumed by the wicked-tenants parable [[NOTE:galilee-absentee-estates]]." $slugToNumber

# ---- Seed new entities ----
Seed-Entity "Jezreel Valley" "jezreel-valley" "place" "Galilean plain documented for absentee-owned Roman-period estates."
# "Levirate Marriage" already exists in the entity catalog (slug: levirate-marriage) -- not reseeded here.
Seed-Entity "Psalm 110" "psalm-110" "document" "Royal psalm quoted in the `"Son of David`" exchange (Luke 20:41-44)."

$conn.Close()
Write-Host "DONE Chapter 20."
