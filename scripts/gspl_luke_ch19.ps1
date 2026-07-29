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
$Ch19NodeId = [guid]"019FA96A-B6EA-72C1-8FC8-CD6FA1ABA179"
$maxNoteSortKey = [double](Exec-Scalar "SELECT ISNULL(MAX(bn.SortKey),0) FROM BeatNodes bn WHERE bn.NodeId='019FA96B-18E2-7BB4-BAEB-11ACA24934F4'")
$maxGlossarySortKey = [double](Exec-Scalar "SELECT ISNULL(MAX(bn.SortKey),0) FROM BeatNodes bn WHERE bn.NodeId='019FA96B-29F5-7BB9-99D0-0F787960E530'")
$maxNoteNumber = [int](Exec-Scalar "SELECT MAX(CAST(LEFT(b.Text, CHARINDEX(' ',b.Text)-1) AS INT)) FROM BeatNodes bn JOIN Beats b ON bn.BeatId=b.Id WHERE bn.NodeId='019FA96B-18E2-7BB4-BAEB-11ACA24934F4'")
Write-Host "Starting: MaxNumber=$script:MaxNumber MaxNoteSortKey=$maxNoteSortKey MaxNoteNumber=$maxNoteNumber MaxGlossarySortKey=$maxGlossarySortKey"

$em = [char]8212

# ---- Notes ----
$notes = [ordered]@{
'fitzmyer-architelones-hapax' = @{ title="A one-time word in all of Greek literature"; body="Joseph A. Fitzmyer, The Gospel According to Luke X-XXIV (Anchor Bible 28A; Garden City, NY: Doubleday, 1985), commentary at 19:2. Fitzmyer identifies architelones ('chief tax collector') as a term unattested in the Septuagint, in surviving Koine papyri, or anywhere else in Greek literature outside this single occurrence in Luke $em a genuine hapax legomenon reconstructed from its compound parts and the known hierarchical structure of Roman provincial tax-farming." }
'zacchaeus-name-meaning' = @{ title="'Pure,' hung on Jericho's most notorious collaborator"; body="Behind the Name, 'Zacchaeus' and 'Zakkai' entries; standard biblical onomastic reference works. The name Zacchaeus derives from Hebrew Zakkai, from the root zakah ('to be clean, pure') $em a name carried, pointedly, by Jericho's chief tax collector, a figure whose profession made him a byword for corruption." }
'sycamore-fig-vs-sycamine' = @{ title="Two different trees, correctly distinguished by Luke"; body="Lytton John Musselman, Figs, Dates, Laurel, and Myrrh: Plants of the Bible and the Quran (Portland, OR: Timber Press, 2007), entries on sycomore and mulberry. Identifies the tree Zacchaeus climbs (Greek sykomorea, Luke 19:4) as Ficus sycomorus, a fig-mulberry native to the warm Jordan Valley, distinct in genus, range, and growth habit from sykaminos (Luke 17:6), the black mulberry Morus nigra." }
'mina-hundred-drachmas-vs-talent' = @{ title="A mina is not a talent"; body="Standard ancient Greek/Hellenistic metrology, cross-referenced via Attic weight-standard reference sources. A mina equaled 100 drachmas; an Attic talent equaled 60 minas (6,000 drachmas) $em meaning the ten servants of Luke 19:13 each receive roughly 100 days' wages, a materially smaller sum than the talents distributed in Matthew's parallel-but-distinct parable of the talents (Matthew 25:14-30)." }
'josephus-archelaus-rome-journey' = @{ title="A real trip to Rome to secure a throne"; body="Flavius Josephus, Antiquities of the Jews, Book 17, section 219ff. (Whiston 17.9.3). Josephus records that after Herod the Great's death, his son Archelaus traveled to Rome to have Herod's will and his own claim to kingship confirmed personally by Caesar Augustus." }
'josephus-archelaus-embassy' = @{ title="A Jewish delegation opposing him before Augustus"; body="Flavius Josephus, Antiquities of the Jews, Book 17, section 299-303 (Whiston 17.11.1). Josephus records that a delegation of fifty official ambassadors, joined by more than eight thousand Jews already resident in Rome, traveled to oppose Archelaus's confirmation before Augustus, asking instead that Judea be released from Herodian rule; Augustus ultimately confirmed Archelaus only as ethnarch, over a reduced territory." }
'mount-of-olives-descent-route' = @{ title="A route still walked today"; body="Standard topographical/pilgrimage reference sources on Bethphage and the Mount of Olives, corroborated by standard historical-geography descriptions of first-century Jerusalem's eastern approach. The road from Bethany and Bethphage descends the Mount of Olives' southern shoulder, crosses the Kidron Valley, and enters the Old City near the area of the present Golden Gate $em a route still traceable and walked in the annual Palm Sunday procession." }
'stones-cry-out-hyperbole' = @{ title="An echo of Habakkuk, not a claim about geology"; body="Habakkuk 2:11 (Masoretic and standard English translations), cross-referenced against Luke 19:40. Jesus's line about stones crying out most plausibly echoes the image in Habakkuk 2:11 ('the stone will cry out from the wall'), a different application of the same stock hyperbolic image, not a direct fulfillment-citation." }
'tyrian-shekel-purity' = @{ title="Purity over piety: why pagan coinage paid the Temple tax"; body="Standard numismatic scholarship on Second Temple-period coinage, summarized in Biblical Archaeology Society coverage of the Tyrian shekel. Tyrian shekels, though bearing the image of the Tyrian god Melqart and an eagle, were mandated for the half-shekel Temple tax because of their unusually high and consistent silver content, roughly 94-97 percent, well above contemporary Roman provincial coinage." }
'jeremiah-den-of-robbers' = @{ title="A compound quotation of two specific prophets"; body="Jeremiah 7:11 and Isaiah 56:7, Masoretic Text and standard English translations, cross-checked against Luke 19:46 and its Synoptic parallels. Luke 19:46 combines a citation of Isaiah 56:7 with a near-verbatim citation of Jeremiah 7:11, the latter originally Jeremiah's own accusation against Temple corruption roughly six centuries earlier." }
'royal-stoa-robinsons-arch' = @{ title="A commercial infrastructure built into the Temple's scale"; body="Standard Herodian-period archaeological scholarship on the Temple Mount's southwestern corner, summarized in Biblical Archaeology Society coverage of the Royal Stoa and Robinson's Arch. Robinson's Arch, excavated remains of a monumental staircase-arch roughly 15 meters in span, carried pedestrian traffic up to the Royal Stoa, a basilica-scale colonnaded hall that Josephus describes as a hub for commercial and legal business." }
}

# ---- Chapter beats ----
$beat1 = @"
Luke gives us a name, a job title, and a tree, and each of the three turns out to be more interesting than the Sunday-school version lets on (19:1-10). Jesus is passing through Jericho when he encounters "a man named Zacchaeus," described as architelones, "chief tax collector." Wait, actually: this exact Greek title appears nowhere else. Not in the Septuagint, not in any surviving Greek papyrus, not in any other Greek literary text that has come down to us -- a hapax legomenon, a one-time word [[NOTE:fitzmyer-architelones-hapax]]. That does not make it fictional; the Roman tax-farming system in the provinces genuinely was hierarchical, with regional toll districts overseen by a supervisor above the ordinary telones, and Jericho -- sitting on a lucrative balsam and date-palm trade route -- is exactly the kind of customs chokepoint that would warrant a supervisory post. But strictly, we know the office existed by inference from the system, not because the title shows up anywhere else to confirm it.

There is a second layer of irony worth naming: the man's own name, Zacchaeus, comes from the Hebrew Zakkai, meaning "pure" or "innocent" [[NOTE:zacchaeus-name-meaning]] -- a name that would have struck any Jericho local as a small joke, hung as it was on the town's most notorious collaborator with Rome's tax apparatus. Then there is the tree. Being "small in stature," Zacchaeus climbs a sykomorea -- a sycamore-fig -- to see over the crowd, a real, identifiable, and climbable tree common in the warm Jordan Valley floor around Jericho, capable of reaching 15 meters but branching close to the ground. It is worth pausing on this because Luke has already used a different tree word twice, back in chapter 17, when Jesus says faith could uproot a "sycamine" tree -- and readers reasonably conflate the two. They shouldn't. Sykaminos is the black mulberry, a different genus entirely; the Septuagint and Greek botanical usage keep the two trees distinct, and modern botanists working the biblical corpus have made the same distinction [[NOTE:sycamore-fig-vs-sycamine]]. Luke, writing for a mixed audience, uses the correct and different word each time.
"@

$beat2 = @"
The parable Jesus tells as he approaches Jerusalem is a stewardship story with an unusually harsh frame narrative (19:11-27). First, the money: a nobleman gives ten servants one mina each. A mina was a real, standard unit of Greek and Roman-provincial currency, equal to 100 drachmas, with a drachma roughly equivalent to a day's wage [[NOTE:mina-hundred-drachmas-vs-talent]]. This is worth naming precisely because Matthew tells a similarly shaped story -- the parable of the talents -- that readers often flatten into "the same parable." It is not: a talent was 60 minas, 6,000 drachmas, in the standard Attic weight system. Matthew's stewards are handed sums many times larger than Luke's.

Second, and more strikingly: the frame around the stewardship story is about a nobleman "going to receive a kingdom" who faces a delegation of citizens sent to say "we do not want this man to reign over us." Scholars have long noted this maps with unusual precision onto real events after Herod the Great's death in 4 BC. Herod Archelaus, Herod's son and designated heir, traveled to Rome specifically to have his father's will and his own claim to kingship confirmed by Augustus [[NOTE:josephus-archelaus-rome-journey]]. While he was there, a Jewish delegation -- fifty official ambassadors joined by more than eight thousand Jews already resident in Rome -- traveled to oppose him before Augustus [[NOTE:josephus-archelaus-embassy]]. Augustus ultimately gave Archelaus a reduced title over a reduced territory -- and Archelaus, once in power, was remembered for brutal reprisals against those who had crossed him. Nearly every commentator who works this parable side by side with Josephus reaches the same conclusion: the "harsh man" framing story is not neutral scenery.
"@

$beat3 = @"
The entry into Jerusalem combines a staged detail and a piece of geography that is still walkable today (19:28-40). Jesus sends two disciples ahead to procure a colt from Bethphage and Bethany, villages on the Mount of Olives' eastern slope, and rides it down into the city while a crowd spreads cloaks and shouts praise. The route itself is real and identifiable: the road descends the southern shoulder of the Mount of Olives, crosses the Kidron Valley, and enters the city near what is now the area of the Golden Gate -- a descent that remains a marked, walkable path, retraced annually in the Palm Sunday procession [[NOTE:mount-of-olives-descent-route]]. When Pharisees in the crowd object to the acclamation, Jesus answers that if the disciples fell silent, "the very stones would cry out" -- straightforward rhetorical hyperbole, likely echoing Habakkuk 2:11, not a claim about geology [[NOTE:stones-cry-out-hyperbole]].
"@

$beat4 = @"
Jesus's tears over Jerusalem (19:41-44) are, on their face, an emotional beat rather than a factual claim -- but the content of the lament is a prediction of the city's siege and leveling "so that not one stone will be left on another." This is the same territory this book has already covered in the extended discussion of the Jerusalem lament in chapter 13: whether this material reflects genuine advance warning or later theological hindsight is the live scholarly question there, and it applies here in miniature. Nothing about the underlying historical event is in doubt -- Jerusalem's walls and Temple were destroyed by Titus's forces in 70 AD, an event as archaeologically and historiographically certain as anything in this book gets.
"@

$beat5 = @"
The Temple scene that closes the chapter turns on a piece of numismatics that sounds like a contradiction until you see the mechanism (19:45-48). Every Jewish male of qualifying age owed an annual half-shekel Temple tax, and the money-changers Jesus overturns existed because that tax had to be paid in one specific currency: the Tyrian shekel, minted in Tyre and bearing the head of the Phoenician-Tyrian god Melqart -- exactly the kind of graven pagan imagery the Temple establishment otherwise treated as intolerable. The reason rabbinic authorities nonetheless mandated this coin specifically is metallurgical: the Tyrian shekel held an unusually high and reliably consistent silver content, well above the debased provincial and Roman coinage in ordinary circulation [[NOTE:tyrian-shekel-purity]]. That necessary exchange is what put money-changers' tables inside the Temple precincts in the first place.

When Jesus drives them out, his words are a direct, identifiable citation: "It is written, 'My house shall be a house of prayer,'" quoting Isaiah 56:7, "but you have made it a den of robbers" -- a phrase lifted essentially verbatim from Jeremiah 7:11, where the prophet accuses the Temple of his own day of the same corruption six centuries earlier [[NOTE:jeremiah-den-of-robbers]]. Scale matters here too: Herod's Temple complex was one of the largest religious platforms in the ancient Mediterranean world, accessed in its southwestern corner by the monumental staircase-and-arch structure now called Robinson's Arch, leading up to the Royal Stoa, a basilica-scale colonnaded hall that Josephus describes as a hub for exactly the kind of commercial business Jesus is objecting to [[NOTE:royal-stoa-robinsons-arch]]. The "den of robbers" Jesus names was not a card table in a corner but a permanent commercial infrastructure built into one of antiquity's largest religious complexes.
"@

$beats = @($beat1, $beat2, $beat3, $beat4, $beat5)

# ---- New glossary entries ----
$glossary = [ordered]@{
'ZACCHAEUS' = "Jericho's chief tax collector (architelones); climbs a sycamore-fig tree to see Jesus; his name means `"pure`" in Hebrew, an irony against his profession [[NOTE:zacchaeus-name-meaning]] [[NOTE:fitzmyer-architelones-hapax]]."
'HEROD ARCHELAUS' = "Herod the Great's son and heir to Judea; traveled to Rome to have his kingship confirmed by Augustus over Jewish opposition; a historical parallel proposed for the harsh nobleman of the parable of the ten minas [[NOTE:josephus-archelaus-rome-journey]] [[NOTE:josephus-archelaus-embassy]]."
'MINA' = "Greek/Hellenistic currency unit equal to 100 drachmas; distinct from and smaller than the `"talent`" (60 minas) used in Matthew's parallel parable of the talents [[NOTE:mina-hundred-drachmas-vs-talent]]."
'TYRIAN SHEKEL' = "High-purity silver coin minted at Tyre, bearing the pagan god Melqart; mandated for the half-shekel Temple tax despite its imagery because of its silver purity; created the need for Temple money-changers [[NOTE:tyrian-shekel-purity]]."
'ROYAL STOA / ROBINSONS ARCH' = "Monumental colonnaded hall and its connecting staircase-arch on Herod's Temple platform; archaeologically attested; backdrop for the scale of commercial activity Jesus disrupts in the Temple cleansing [[NOTE:royal-stoa-robinsons-arch]]."
'SYCAMORE-FIG TREE (FICUS SYCOMORUS)' = "Low-branching, climbable fig-mulberry native to the Jordan Valley/Jericho region; the tree Zacchaeus climbs; botanically distinct from the `"sycamine`" (black mulberry, Morus nigra) named in Luke 17:6 [[NOTE:sycamore-fig-vs-sycamine]]."
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
    Add-BeatNode $Ch19NodeId $id $sortKey
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
Try-Append "THE TEMPLE" "This chapter adds two new checkable claims: the Tyrian-shekel purity requirement behind Temple money-changing [[NOTE:tyrian-shekel-purity]], and the archaeologically-attested scale of the Royal Stoa/Robinson's Arch complex as backdrop for the cleansing [[NOTE:royal-stoa-robinsons-arch]]." $slugToNumber
Try-Append "JERICHO" "This chapter adds the Zacchaeus/architelones episode and the sycamore-fig botanical detail tied to this location [[NOTE:fitzmyer-architelones-hapax]] [[NOTE:sycamore-fig-vs-sycamine]]." $slugToNumber

# ---- Seed new entities ----
# "Archelaus" already exists in the entity catalog (slug: archelaus) -- not reseeded here.
Seed-Entity "Robinson's Arch" "robinsons-arch" "place" "Monumental staircase-arch connecting Jerusalem's Lower Market to the Royal Stoa."
Seed-Entity "Royal Stoa" "royal-stoa" "place" "Herodian-era colonnaded hall on the Temple platform used for commerce and legal business."

$conn.Close()
Write-Host "DONE Chapter 19."
