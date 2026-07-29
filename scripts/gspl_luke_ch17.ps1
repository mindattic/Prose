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
$Ch17NodeId = [guid]"019FA96A-9507-7DFD-8E43-53E7682266FD"
$maxNoteSortKey = [double](Exec-Scalar "SELECT ISNULL(MAX(bn.SortKey),0) FROM BeatNodes bn WHERE bn.NodeId='019FA96B-18E2-7BB4-BAEB-11ACA24934F4'")
$maxGlossarySortKey = [double](Exec-Scalar "SELECT ISNULL(MAX(bn.SortKey),0) FROM BeatNodes bn WHERE bn.NodeId='019FA96B-29F5-7BB9-99D0-0F787960E530'")
$maxNoteNumber = [int](Exec-Scalar "SELECT MAX(CAST(LEFT(b.Text, CHARINDEX(' ',b.Text)-1) AS INT)) FROM BeatNodes bn JOIN Beats b ON bn.BeatId=b.Id WHERE bn.NodeId='019FA96B-18E2-7BB4-BAEB-11ACA24934F4'")
Write-Host "Starting: MaxNumber=$script:MaxNumber MaxNoteSortKey=$maxNoteSortKey MaxNoteNumber=$maxNoteNumber MaxGlossarySortKey=$maxGlossarySortKey"

$em = [char]8212

# ---- Notes ----
$notes = [ordered]@{
'millstone-onikos-mill-tech' = @{ title="A commercial millstone, not a kitchen quern"; body="Bauer/Danker/Arndt/Gingrich, A Greek-English Lexicon of the New Testament (BDAG), 3rd ed. (Chicago: University of Chicago Press, 2000), s.v. onikos. The adjective onikos ('of or for a donkey') modifies mylos ('millstone') to specify the large, animal-turned commercial grinding stone rather than the small hand-quern a household would use $em a stone weighing one to several hundred pounds." }
'millstone-drowning-punishment' = @{ title="A real, attested method of execution"; body="Compiled ancient testimony crediting Diodorus Siculus's Bibliotheca Historica, as preserved in McClintock and Strong's Cyclopedia of Biblical, Theological, and Ecclesiastical Literature, s.v. 'Millstone.' Drowning with a fastened millstone was a punishment for sacrilege and parricide among Greeks, Syrians, and Macedonians, echoed in Roman-era Judea as judicial drowning (katapontismos)." }
'sycamine-black-mulberry-vs-sycamore-fig' = @{ title="Two different trees, easily confused"; body="Easton's Bible Dictionary (1897), s.v. 'Sycamine Tree.' The sycamine (Greek sykaminos) of Luke 17:6 is the black mulberry, Morus nigra, botanically and lexically distinct from the sycamore-fig, Ficus sycomorus (Greek sykomorea), that appears in Luke 19:4 $em two different trees sharing a confusable English rendering in most translations." }
'samaria-galilee-border-geography' = @{ title="A real, porous administrative seam"; body="Felix Just, S.J., 'Biblical Geography: Samaria and the Samaritans,' catholic-resources.org. Luke 17:11's 'through the midst of Samaria and Galilee' reflects a genuine border zone a traveler moving toward Jerusalem would cross and recross rather than a single clean line; some geographers place the episode in the Harod Valley corridor." }
'bab-edh-dhra-numeira-sodom-debate' = @{ title="Sodom's location remains genuinely disputed"; body="Walter E. Rast and R. Thomas Schaub, excavation reports on Bab edh-Dhra and Numeira (Early Bronze Age destruction layers, c. 2350-2300 BCE); contrasted with Steven Collins's Tall el-Hammam excavations. Rast and Schaub never claimed to have identified literal Sodom and Gomorrah; the Tall el-Hammam identification remains actively disputed on geographic and chronological grounds. No site has scholarly consensus as biblical Sodom." }
'tall-el-hammam-airburst-retraction' = @{ title="A retracted claim, and a recent one"; body="T. E. Bunch et al., 'A Tunguska sized airburst destroyed Tall el-Hammam,' Scientific Reports 11 (2021); Retraction Note, Scientific Reports (24 April 2025). The 2021 paper proposing a cosmic airburst destroyed Tall el-Hammam around 1650 BCE was formally retracted after independent reviewers concluded the mineralogical and geochemical evidence did not support the claim." }
'noah-flood-gilgamesh-atrahasis-parallel' = @{ title="Atrahasis, the closer Mesopotamian ancestor"; body="Andrew George, trans., The Epic of Gilgamesh (London: Penguin Classics, 2003), Tablet XI; W. G. Lambert and A. R. Millard, Atra-hasis: The Babylonian Story of the Flood (Oxford: Oxford University Press, 1969). Scholarly consensus holds Gilgamesh Tablet XI's flood narrative was adapted from the older Atrahasis epic; real differences persist $em Noah's flood carries an explicit moral cause and runs 40 days, Utnapishtim's is divinely arbitrary and runs six days." }
'griffon-vulture-nesher-carrion-ecology' = @{ title="A real bird with real carrion-gathering behavior"; body="Fred S. Cannon, 'The Biblical Nesher as the Griffon Vulture, Gyps fulvus: Ornithological Character Traits,' Journal for the Study of the Old Testament 48 (2024): 470-93. The Hebrew nesher is best identified as the griffon vulture, an obligate carrion-feeder that gathers socially and in numbers at a kill or corpse, unlike the eagles later translators sometimes substituted." }
}

# ---- Chapter beats ----
$beat1 = @"
Jesus turns from the crowd to his disciples with a warning pitched in the register of nightmare: causing one of "these little ones" to stumble would be better answered by having "a millstone" hung around the neck and being thrown into the sea (17:1-2). The Greek is more specific than most English translations let on -- it names not the fist-sized hand-quern a woman turned in her own kitchen, but a mylos onikos, literally a "donkey-millstone," the massive, animal-turned commercial grinding stone used in bakeries and oil presses, weighing anywhere from a hundred to several hundred pounds [[NOTE:millstone-onikos-mill-tech]]. The image is not idle hyperbole. Drowning a condemned person with a heavy stone lashed to the neck was a real, independently attested Mediterranean punishment for the gravest offenses, practiced among Greeks, Syrians, and Macedonians, and echoed in Roman-era Judea as judicial drowning [[NOTE:millstone-drowning-punishment]]. Jesus is naming the actual worst-case penalty his audience would have recognized, then declaring corrupting a child's faith worse than it. The pericope pivots immediately to its opposite pole -- unlimited forgiveness for the one who repents, even seven times in a day (17:3-4).
"@

$beat2 = @"
The apostles, apparently rattled, ask Jesus to "increase" their faith (17:5). His answer is deliberately absurd: faith the size of a mustard seed could command a sycamine tree to uproot itself and plant itself in the sea (17:6). The sycamine is worth pausing on, because it sets up a trap for the unwary reader two chapters ahead. It is most likely the black mulberry, Morus nigra, distinct in Greek as sykaminos from the sycamore-fig, Ficus sycomorus, that Zacchaeus will climb in chapter 19 [[NOTE:sycamine-black-mulberry-vs-sycamore-fig]]. The two trees share an English name in most translations and a root in Greek, but they are not the same species. The unit closes with the parable of the unworthy servants (17:7-10), assuming the ordinary, unremarked machinery of first-century household slavery -- a real institution, though the parable itself is doing ethical rather than documentary work.
"@

$beat3 = @"
Luke resumes the travel narrative: Jesus is "passing through the midst of Samaria and Galilee" on the way to Jerusalem (17:11) -- the same porous, contested border country this book has already mapped when Jesus turned toward Samaria in chapter 9 and told the parable of the Good Samaritan in chapter 10. The phrasing describes a real administrative and cultural seam that travelers moving toward Jerusalem would cross and recross rather than following one clean route [[NOTE:samaria-galilee-border-geography]]. Ten men with skin disease meet him; he tells them to show themselves to the priests, invoking the same Levitical tzaraat-diagnosis and cleansing procedure already established in this book's earlier leprosy material (17:12-14). Only one returns to give thanks, and Luke notes pointedly that he is a Samaritan (17:15-16) -- a detail that only lands with force because this book has already built out who Samaritans were to a first-century Jewish audience, and because a Samaritan healed of tzaraat had no priest of his own line authorized to certify him "clean." Jesus's closing line -- "your faith has made you well" -- needs no further sourcing than what this book has already laid down about Samaritans and purity law.
"@

$beat4 = @"
Asked when the Kingdom of God is coming, Jesus answers that it will not come "with signs to be observed," then pivots to a longer discourse layering two comparisons from the deep past. "As it was in the days of Noah" (17:26-27) reaches back to the same Mesopotamian flood memory this book already touched in chapter 12's Gilgamesh material, though there for a different purpose. The comparative point that's new here is the flood itself: Gilgamesh's Tablet XI is now understood by most scholars to derive its flood narrative from the older Atrahasis epic, and both offer the closest surviving Mesopotamian parallels to Noah's story -- with real differences persisting, since the biblical flood carries an explicit moral cause and Utnapishtim's is divinely arbitrary [[NOTE:noah-flood-gilgamesh-atrahasis-parallel]].

"Likewise as it was in the days of Lot" (17:28-29) reaches for a second cautionary parallel. Whether archaeology has located Sodom is a live, unsettled question, not a solved one. Excavators Walter Rast and R. Thomas Schaub documented Early Bronze Age towns at Bab edh-Dhra and Numeira, south of the Dead Sea, with clear destruction layers -- but never claimed to have found the literal cities of Genesis; a rival camp led by Steven Collins argues instead for Tall el-Hammam, north of the Dead Sea [[NOTE:bab-edh-dhra-numeira-sodom-debate]]. The Tall el-Hammam camp's most dramatic claim -- a Tunguska-scale cosmic airburst destroyed the site around 1650 BCE, published in Scientific Reports in 2021 -- was formally retracted by the journal in April 2025 after outside physicists found the underlying evidence did not hold up [[NOTE:tall-el-hammam-airburst-retraction]]. The discourse ends on an image with its own independent grounding: asked where the Son of Man will be found, Jesus answers, "where the corpse is, there the vultures will gather" (17:37). The bird in question is almost certainly the griffon vulture, an obligate carrion-feeder that gathers socially and in numbers at a carcass, exactly the behavior the saying describes [[NOTE:griffon-vulture-nesher-carrion-ecology]].
"@

$beats = @($beat1, $beat2, $beat3, $beat4)

# ---- New glossary entries ----
$glossary = [ordered]@{
'SAMARIA' = "The mixed-population territory north of Judea and south of Galilee, its border with Galilee a real administrative and cultural seam Jesus crosses repeatedly on the road to Jerusalem [[NOTE:samaria-galilee-border-geography]]."
'NOAH' = "Figure from Genesis 6-9 invoked in Luke 17:26-27 as a type for the Kingdom's sudden, unsignaled arrival; connects to this book's existing Gilgamesh/flood-tradition material [[NOTE:noah-flood-gilgamesh-atrahasis-parallel]]."
'LOT' = "Figure from Genesis 19 invoked in Luke 17:28-32 (`"remember Lot's wife`") as a second type for sudden judgment amid ordinary life; tied to the unresolved Sodom-location debate [[NOTE:bab-edh-dhra-numeira-sodom-debate]]."
'SODOM' = "The city destroyed in Genesis 19, named directly in Luke 17:29; its archaeological location remains genuinely disputed among Bab edh-Dhra/Numeira, Tall el-Hammam, and unconfirmed alternatives [[NOTE:bab-edh-dhra-numeira-sodom-debate]] [[NOTE:tall-el-hammam-airburst-retraction]]."
'ATRAHASIS EPIC' = "Old Babylonian flood narrative, the older structural source behind the Gilgamesh flood account in Tablet XI; the closer Mesopotamian ancestor to the Noah flood-story genre [[NOTE:noah-flood-gilgamesh-atrahasis-parallel]]."
'GRIFFON VULTURE (NESHER)' = "Gyps fulvus, the obligate-carrion-feeding raptor most likely meant by the `"vultures`" of Luke 17:37; distinct in feeding ecology from the eagle later translators sometimes substituted [[NOTE:griffon-vulture-nesher-carrion-ecology]]."
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
    Add-BeatNode $Ch17NodeId $id $sortKey
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
Try-Append "EPIC OF GILGAMESH" "This chapter adds a new claim: the flood narrative specifically (Tablet XI), distinct from chapter 12's carpe diem/Siduri usage [[NOTE:noah-flood-gilgamesh-atrahasis-parallel]]." $slugToNumber

# ---- Seed new entities ----
# "Sodom" already exists in the entity catalog (slug: sodom) -- not reseeded here.
Seed-Entity "Atrahasis Epic" "atrahasis-epic" "document" "Old Babylonian flood narrative, source behind Gilgamesh's flood account."
Seed-Entity "Griffon Vulture (Nesher)" "griffon-vulture-nesher" "material" "Carrion-feeding raptor likely meant by `"vultures`" in Luke 17:37."
Seed-Entity "Walter Rast and R. Thomas Schaub" "walter-rast-r-thomas-schaub" "character" "Archaeologists who excavated Bab edh-Dhra and Numeira, EBA sites near the Dead Sea."
Seed-Entity "Steven Collins" "steven-collins-archaeologist" "character" "Archaeologist proposing Tall el-Hammam as the site of biblical Sodom."

$conn.Close()
Write-Host "DONE Chapter 17."
