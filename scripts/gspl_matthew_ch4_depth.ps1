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
    $cmd = $conn.CreateCommand()
    $cmd.CommandText = $sql
    foreach ($k in $params.Keys) {
        $cmd.Parameters.AddWithValue("@$k", $params[$k]) | Out-Null
    }
    $cmd.ExecuteNonQuery() | Out-Null
}

function Exec-Scalar([string]$sql) {
    $cmd = $conn.CreateCommand()
    $cmd.CommandText = $sql
    return $cmd.ExecuteScalar()
}

function New-BeatRow([string]$text) {
    $script:MaxNumber = $script:MaxNumber + 1
    $id = [guid]::NewGuid()
    $hash = Sha256Hex $text
    $sql = "INSERT INTO Beats (Id, Text, TextHash, Act, SceneType, Kind, Number, Stale, WasCorrected, IsChapterStart, Version, EntityStale, CreatedAt, UpdatedAt) VALUES (@Id, @Text, @Hash, 0, 'scene', 'prose', @Number, 0, 0, 0, 0, 0, SYSUTCDATETIME(), SYSUTCDATETIME())"
    Exec-NonQuery $sql @{ Id = $id; Text = $text; Hash = $hash; Number = $script:MaxNumber }
    return $id
}

function Add-BeatNode([guid]$nodeId, [guid]$beatId, [double]$sortKey) {
    $sql = "INSERT INTO BeatNodes (NodeId, BeatId, SortKey, IsEnabled) VALUES (@NodeId, @BeatId, @SortKey, 1)"
    Exec-NonQuery $sql @{ NodeId = $nodeId; BeatId = $beatId; SortKey = $sortKey }
}

function Seed-Entity([string]$name, [string]$slug, [string]$type, [string]$desc) {
    $exists = Exec-Scalar "SELECT COUNT(*) FROM Entities WHERE UniverseId='0197E9C9-0003-7000-8000-000000000003' AND Slug='$slug'"
    if ($exists -gt 0) { Write-Host "  entity exists, skip: $name"; return }
    $id = [guid]::NewGuid()
    $sql = "INSERT INTO Entities (Id, EntityType, Name, Slug, Status, Description, CreatedAt, ModifiedAt, IsActive, UniverseId) VALUES (@Id, @Type, @Name, @Slug, 'canon', @Desc, SYSUTCDATETIME(), SYSUTCDATETIME(), 1, '0197E9C9-0003-7000-8000-000000000003')"
    Exec-NonQuery $sql @{ Id = $id; Type = $type; Name = $name; Slug = $slug; Desc = $desc }
    Write-Host "  seeded entity: $name"
}

# ---- Live state ----
$script:MaxNumber = [int](Exec-Scalar "SELECT MAX(Number) FROM Beats")
$NotesNodeId = [guid]"019FA01D-FA22-76C6-976C-3EA4F4D54A14"
$GlossaryNodeId = [guid]"019FA026-3758-7A57-AF65-EC1DB4303EF3"
$Ch4NodeId = [guid]"019FA063-93C6-708B-8B3E-90703FF50C84"

# Hardened derivations: filter to IsEnabled=1, and guard MaxNoteNumber against a stray
# non-numeric leading token elsewhere in the shared Notes node.
$maxNoteSortKey = [double](Exec-Scalar "SELECT ISNULL(MAX(bn.SortKey),0) FROM BeatNodes bn WHERE bn.NodeId='019FA01D-FA22-76C6-976C-3EA4F4D54A14' AND bn.IsEnabled=1")
$maxGlossarySortKey = [double](Exec-Scalar "SELECT ISNULL(MAX(bn.SortKey),0) FROM BeatNodes bn WHERE bn.NodeId='019FA026-3758-7A57-AF65-EC1DB4303EF3' AND bn.IsEnabled=1")
$maxNoteNumber = [int](Exec-Scalar "SELECT ISNULL(MAX(CAST(LEFT(b.Text, CHARINDEX(' ',b.Text)-1) AS INT)),0) FROM BeatNodes bn JOIN Beats b ON bn.BeatId=b.Id WHERE bn.NodeId='019FA01D-FA22-76C6-976C-3EA4F4D54A14' AND bn.IsEnabled=1 AND CHARINDEX(' ', b.Text) > 1 AND LEFT(b.Text, CHARINDEX(' ',b.Text)-1) NOT LIKE '%[^0-9]%'")
$maxCh4SortKey = [double](Exec-Scalar "SELECT ISNULL(MAX(bn.SortKey),0) FROM BeatNodes bn WHERE bn.NodeId='019FA063-93C6-708B-8B3E-90703FF50C84' AND bn.IsEnabled=1")

Write-Host "Starting: MaxNumber=$script:MaxNumber MaxNoteSortKey=$maxNoteSortKey MaxNoteNumber=$maxNoteNumber MaxGlossarySortKey=$maxGlossarySortKey MaxCh4SortKey=$maxCh4SortKey"

# ---- Notes (slug -> title/body) in order ----
# This is a DEPTH-PASS: existing chapter 4 prose is untouched. These notes back four new
# supplementary beats appended after the existing narrative, filling checkable-claim
# territory (temptation geography, Deuteronomy citation accuracy, Galilee demographics,
# Capernaum's trade-route profile, and the fishing economy of the first four disciples)
# that the original pass did not cover.
$notes = [ordered]@{
'allison-new-moses-typology-matthew' = @{ title="Matthew's own Moses typology, named and documented"; body="Dale C. Allison Jr., The New Moses: A Matthean Typology (Minneapolis: Fortress Press, 1993), chapters on the wilderness temptation and Sinai parallels. Allison's book-length study, addressed specifically to Matthew rather than to the Synoptic tradition generally, documents in detail the pattern this chapter's original discussion already named in brief: Jesus's forty-day wilderness fast recapitulating Moses's forty days on Sinai (Exodus 34:28) and Israel's own forty years of wilderness wandering, treating it as one strand in a sustained set of Moses parallels running through Matthew's whole Gospel, not an isolated echo confined to this one scene." }
'josephus-temple-pinnacle-height' = @{ title='How high off the ground, in an eyewitness''s own words'; body="Flavius Josephus, Jewish Antiquities, Book 15, section 412 (15.11.5) (Loeb Classical Library, trans. Ralph Marcus, Cambridge, MA: Harvard University Press, 1943). Describing Herod's rebuilt royal portico at the Temple's southeastern corner, overhanging the Kidron Valley, Josephus, who had seen the structure himself before its destruction in 70 CE, writes that anyone looking down from its parapet 'would become dizzy, while his sight could not reach to such an immense depth.' Independent, non-Christian testimony of this kind confirms that the 'pinnacle of the temple' named in Matthew 4:5 answers to a real, extraordinarily tall, identifiable piece of Herodian architecture, whatever did or didn't happen atop it." }
'matthew-apocalyptic-vision-mountain' = @{ title="A vision keyed to Matthew's own mountain, not a claim about eyesight"; body="David L. Mathewson, 'The Apocalyptic Vision of Jesus According to the Gospel of Matthew: Reading Matthew 3:16-4:11 Intertextually,' Tyndale Bulletin 62.1 (2011): 89-108. Mathewson's study is addressed specifically to Matthew's own wording, not Luke's parallel, and situates the devil showing Jesus 'all the kingdoms of the world and their glory' from a 'very high mountain' (4:8) within the conventions of Second Temple apocalyptic vision literature, where a compressed, panoramic, instantaneous sight functions as a recognized literary device for a cosmic claim rather than a report of literal geography, the same literalist-versus-literary-device question this campaign has already raised for Luke's version of the same core tradition, here anchored in Matthew's own high-mountain setting rather than borrowed secondhand from it." }
'jebel-quruntul-matthew-high-mountain' = @{ title='One wilderness tradition, two temptations'; body="Jerome Murphy-O'Connor, The Holy Land: An Oxford Archaeological Guide from Earliest Times to 1700, 5th ed. (Oxford: Oxford University Press, 2008), entry on Jericho. The same fourth-century-attested pilgrimage identification of Jebel Quruntul above Jericho already discussed in this campaign for Luke's parallel temptation scene also supplies the traditional site for Matthew's own 'very high mountain' of 4:8, since both evangelists place the whole encounter in the same unnamed wilderness. Matthew's text, like Luke's, specifies no mountain by name; the identification remains a later devotional attachment rather than a detail either Gospel itself supplies." }
'deuteronomy-citations-matthew-order' = @{ title="Same three verses, Matthew's own order"; body="W.D. Davies and Dale C. Allison Jr., Matthew 1-7, International Critical Commentary, vol. 1 (Edinburgh: T&T Clark, 1988), commentary ad loc. Matthew 4:1-11. As in Luke's parallel version of the scene, all three of Matthew's citations answering the devil are drawn from Deuteronomy alone: 8:3 answers the bread test (4:4), 6:16 answers the temple test (4:7), and 6:13 answers the kingdoms test (4:10), though Matthew places the temple and kingdoms tests in the reverse order from Luke's sequence. Davies and Allison read the pattern, consistent with the mainstream reading already applied to Luke's version, as a deliberately composed contest of citation recapitulating Israel's own wilderness testing across Deuteronomy 6 and 8, not a verbatim transcript of extemporaneous dialogue." }
'josephus-galilee-borders-gentile-territories' = @{ title="Galilee's own borders, in Josephus's accounting"; body="Flavius Josephus, The Jewish War, Book 3, section 35 (3.3.2) (Loeb Classical Library, trans. H. St. J. Thackeray, Cambridge, MA: Harvard University Press, 1927-1928). Josephus, describing Galilee's boundaries firsthand as the region's onetime Jewish military commander, places Phoenicia and the territory of Ptolemais and Carmel to the west, Samaria and Scythopolis to the south, and Hippos, Gadara, Gaulanitis, and Herod Philip's kingdom to the east: Gentile or Gentile-governed territory on three sides of a Jewish region, the real geographic substrate behind Isaiah's older phrase 'Galilee of the Gentiles,' quoted at Matthew 4:15-16." }
'chancey-myth-gentile-galilee' = @{ title='A border phrase, not a population count'; body="Mark A. Chancey, The Myth of a Gentile Galilee, Society for New Testament Studies Monograph Series 118 (Cambridge: Cambridge University Press, 2002). Chancey's synthesis of Josephus's writings with published excavation reports across Galilee's cities and villages finds the region's interior first-century population overwhelmingly Jewish, directly challenging the assumption, common in Bible dictionaries and historical-Jesus scholarship alike, that Galilee held a large or majority-Gentile population in Jesus's own lifetime. Read against Galilee's genuinely mixed border geography, 'Galilee of the Gentiles' names a Jewish territory bordered by Gentile lands on multiple sides, not a demographically mixed or Gentile-majority one within it." }
'capernaum-via-maris-border-toll-town' = @{ title='Why a fishing village needed a customs post'; body="Jonathan L. Reed, Archaeology and the Galilean Jesus: A Re-examination of the Evidence (Harrisburg, PA: Trinity Press International, 2000), chapters on Capernaum. Reed situates Capernaum, entered by Jesus in this chapter (4:13), on the Sea of Galilee's northwest shore directly astride the Via Maris, the region's principal north-south trade route linking Damascus to the Mediterranean coast, and at the border between Herod Antipas's Galilee and Herod Philip's territory across the Jordan's entry into the lake. That same border-and-trade-route position is the reason Matthew's own Gospel later places a toll collector's post at Capernaum (Matthew 9:9), a separate scene from anything discussed in this chapter, and distinct from this campaign's earlier, extensive discussion of the site's disputed synagogue archaeology." }
'hanson-galilean-fishing-licensing-capital' = @{ title='Fishing was a licensed, capitalized trade, not a subsistence scrape'; body="K.C. Hanson, 'The Galilean Fishing Economy and the Jesus Tradition,' Biblical Theology Bulletin 27 (1997): 99-111. Hanson's economic reconstruction models first-century Galilean fishing as a state-regulated trade under Herod Antipas, requiring leased or licensed lake access and real working capital for boats and nets, and, for larger household operations, hired labor beyond the immediate family: a fishing business of real substance, not a subsistence-level scrape, for families able to sustain it." }
'mark-zebedee-hired-servants-synoptic' = @{ title="A detail Matthew's own text leaves out"; body="Mark 1:19-20, compared against Matthew 4:21-22. Mark's parallel account of the same calling scene adds a detail Matthew's own version omits: Zebedee is left in the boat 'with the hired servants' once James and John depart, implying a household able to employ outside labor rather than one scraping by on family hands alone. Matthew's version states only that the brothers 'left the boat and their father'; the hired-servants detail belongs to the synoptic tradition's fuller picture of this same family, not to Matthew's own wording, and is worth flagging precisely because this book's method treats what a Gospel actually says, versus what a parallel account adds, as two different kinds of claim." }
}

# ---- New supplementary chapter beats (appended after existing narrative; nothing existing is touched) ----
$beat1 = @'
This chapter's original discussion already named the forty-day fast's echo of Moses's forty days on Sinai and Israel's own forty years in the wilderness, but named it without a source; Dale Allison's book-length study of Matthew's Moses typology substantiates that pattern in detail, specifically for this Gospel rather than for the Synoptic tradition in general, treating it as one strand among several running through the whole book [[NOTE:allison-new-moses-typology-matthew]].

Two further pieces of the temptation's staging are worth a closer, checkable look of their own. The third test sets Jesus "on the pinnacle of the temple" in "the holy city" and dares him to throw himself down (4:5-6) — and unlike the unnamed wilderness of the whole scene, this is a specific, identifiable piece of architecture. Herod's rebuilt Temple included a royal portico at its southeastern corner, overhanging the Kidron Valley, and Josephus — who had seen the structure himself before its destruction in 70 CE — describes looking down from its parapet as enough to make a person "become dizzy, while his sight could not reach to such an immense depth" [[NOTE:josephus-temple-pinnacle-height]]. Whatever did or didn't happen atop it, "the pinnacle of the temple" names a real, extraordinarily tall place a first-century reader could have pictured concretely.

The second test does the opposite. It takes Jesus to "a very high mountain" and shows him "all the kingdoms of the world and their glory" (4:8) — and no mountain, however high, offers a literal line of sight to the whole world. This campaign has already raised the same literalist-versus-literary-device question for Luke's parallel version of this scene; scholarship addressed specifically to Matthew's own wording reaches the same conclusion by a different route, situating the vision within the conventions of Second Temple apocalyptic literature, where a compressed, instantaneous, panoramic sight is a recognized device for making a cosmic claim rather than a travelogue [[NOTE:matthew-apocalyptic-vision-mountain]]. And just as a later Christian tradition supplied a specific mountain for a scene that names none — the fourth-century-attested Jebel Quruntul above Jericho, already discussed in this campaign for Luke's version of the same story — the identical devotional logic applies here: Matthew's own "very high mountain" is no more located by the text itself than Luke's is [[NOTE:jebel-quruntul-matthew-high-mountain]].
'@

$beat2 = @'
The citation pattern behind Jesus's three replies deserves the same scrutiny in Matthew's own wording that this campaign has already given Luke's parallel version of the scene. All three of Matthew's counters, like Luke's, are drawn from a single book: Deuteronomy 8:3 answers the bread test (4:4), Deuteronomy 6:16 answers the temple test (4:7) — the same real, dizzying drop off the Temple's royal portico already described above — and Deuteronomy 6:13 answers the kingdoms test (4:10) [[NOTE:josephus-temple-pinnacle-height]]. All three are verified, findable lines from Deuteronomy's own account of Israel's wilderness testing, not invented or misquoted scripture. Matthew, notably, answers the tests in a different order than Luke: Luke's devil offers the kingdoms before the temple, while Matthew's offers the temple before the kingdoms, so the same three verses land in reverse sequence between the two Gospels [[NOTE:deuteronomy-citations-matthew-order]]. The scholarly point already made about Luke's version applies here without needing to be rederived: a person answering three separate, unplanned challenges under duress doesn't typically reach for the same scroll three times running, and the mainstream historical-critical reading treats the pattern as a composed contest of citation built to recapitulate Israel's own wilderness story, not a stenographic transcript of the exchange.
'@

$beat3 = @'
Matthew's citation of Isaiah 9:1-2 — "the people who sat in darkness have seen a great light," naming Zebulun and Naphtali by name — carries a phrase this chapter's original discussion doesn't unpack on its own terms: "Galilee of the Gentiles" (4:15). Taken at face value, the label invites a reader to picture Galilee as a mixed or Gentile-majority region in Jesus's own lifetime, and that demographic question is genuinely checkable. Josephus, who commanded Galilee's Jewish forces before defecting to Rome and knew the region's geography firsthand, describes it as bordered by Phoenicia and the territory of Ptolemais and Carmel to the west, Samaria and Scythopolis to the south, and Hippos, Gadara, Gaulanitis, and Herod Philip's kingdom to the east — Gentile or Gentile-governed territory pressing in on three sides [[NOTE:josephus-galilee-borders-gentile-territories]]. What that border geography does not establish is a mixed population within Galilee itself. Mark A. Chancey's book-length synthesis of Josephus's own writings with published excavation reports across Galilee's cities and villages finds the interior first-century population overwhelmingly Jewish, directly against the common assumption, found in Bible dictionaries and historical-Jesus scholarship alike, that first-century Galilee held a large or majority-Gentile population [[NOTE:chancey-myth-gentile-galilee]]. "Galilee of the Gentiles," in other words, is best read as a Jewish region hemmed by Gentile neighbors, the same real geography Isaiah's own eighth-century-BCE oracle was describing when Assyria first devastated this territory, not a claim about who actually lived inside its borders in the 20s and 30s CE. Capernaum itself, where Jesus settles two verses earlier (4:13), sits inside this same border territory, a detail worth holding onto for what it says about the town's own position [[NOTE:capernaum-via-maris-border-toll-town]].
'@

$beat4 = @'
This campaign has already examined Capernaum's synagogue and its candidate "House of Peter" in considerable archaeological depth; what's worth adding here is the town's basic first-century profile, since that's what makes it a plausible operating base rather than an arbitrary choice of setting. Capernaum sat on the Sea of Galilee's northwest shore directly astride the Via Maris, the region's principal north-south trade route linking Damascus to the Mediterranean coast, and at the border between Herod Antipas's Galilee and Herod Philip's territory across the Jordan's mouth into the lake [[NOTE:capernaum-via-maris-border-toll-town]]. A fishing and farming village sitting on both a real trade artery and a real political boundary — the same border geography just discussed for Galilee as a whole — had exactly the through-traffic, toll revenue, and cross-border reach a Galilean ministry's operating base would need [[NOTE:josephus-galilee-borders-gentile-territories]], while the town's own population, like Galilee's interior generally, remained predominantly Jewish rather than a Gentile-majority settlement [[NOTE:chancey-myth-gentile-galilee]].

That same economic realism extends to the calling scene itself. Simon, Andrew, James, and John are called away from "casting a net into the sea" and "mending their nets" in their father's boat (4:18-21) — ordinary labor, but not subsistence-level labor. First-century Galilean fishing operated as a state-regulated trade under Herod Antipas, requiring leased or licensed lake access and real working capital for boats and nets, and larger household operations employed hired labor beyond the immediate family [[NOTE:hanson-galilean-fishing-licensing-capital]]. Matthew's own text doesn't mention hired help for Zebedee's household, but Mark's parallel account of this same scene does: it has Zebedee left in the boat "with the hired servants" once his sons depart, a detail worth flagging precisely because it belongs to Mark's wording and not Matthew's [[NOTE:mark-zebedee-hired-servants-synoptic]]. Either way, a family fishing business substantial enough to employ outside labor, or plausibly to have done so per the wider synoptic tradition, is a far more economically grounded starting point for four men who "immediately left" everything than a picture of bare subsistence poverty would be — the choice to follow Jesus reads as a real sacrifice of something, not a flight from nothing.
'@

$beats = @($beat1, $beat2, $beat3, $beat4)

# ---- New glossary additions (heading -> body); only genuinely new concepts, everything
# else relevant to this chapter already has a heading in the existing 95-entry glossary ----
$glossary = [ordered]@{
'VIA MARIS' = "The region's principal north-south trade route in antiquity, linking Damascus to the Mediterranean coast and passing directly through Capernaum on the Sea of Galilee's northwest shore. Capernaum's position on this route, and on the border between Herod Antipas's Galilee and Herod Philip's territory, is the historical-economic backdrop to Jesus settling there in this chapter (4:13) and to Matthew's own later scene of a toll collector working at the same town (Matthew 9:9) [[NOTE:capernaum-via-maris-border-toll-town]]."
'FIRST-CENTURY GALILEAN FISHING ECONOMY' = "The regulated, capital-intensive trade the first four disciples are drawn from in this chapter (4:18-22): fishing on the Sea of Galilee under Herod Antipas required leased or licensed lake access and real investment in boats and nets, and larger family operations employed hired labor beyond immediate relatives [[NOTE:hanson-galilean-fishing-licensing-capital]]. Mark's parallel account of the same calling scene notes that Zebedee, father of James and John, retained 'hired servants' in his boat after his sons departed, a detail Matthew's own version does not include, but one that situates the family's operation as a business of some substance rather than bare subsistence [[NOTE:mark-zebedee-hired-servants-synoptic]]."
}

# ---- Insert Notes ----
$emdash = [char]0x2014
$slugToNumber = @{}
foreach ($slug in $notes.Keys) {
    $n = $notes[$slug]
    $noteNum = $maxNoteNumber + 1
    $maxNoteNumber = $noteNum
    $text = "$noteNum $emdash $($n.title)`n`n$($n.body)"
    $id = New-BeatRow $text
    $maxNoteSortKey += 50
    Add-BeatNode $NotesNodeId $id $maxNoteSortKey
    $slugToNumber[$slug] = $noteNum
}
Write-Host "Inserted $($notes.Count) notes, ending at [$maxNoteNumber]"

# ---- Insert new chapter beats with placeholder replacement, appended well above existing SortKeys ----
$sortKey = [double]([Math]::Max(900.0, $maxCh4SortKey))
foreach ($beatText in $beats) {
    $resolved = $beatText
    foreach ($slug in $slugToNumber.Keys) {
        $resolved = $resolved.Replace("[[NOTE:$slug]]", "[$($slugToNumber[$slug])]")
    }
    $sortKey += 100
    $id = New-BeatRow $resolved
    Add-BeatNode $Ch4NodeId $id $sortKey
}
Write-Host "Inserted $($beats.Count) chapter beats"

# ---- Insert glossary entries ----
foreach ($heading in $glossary.Keys) {
    $body = $glossary[$heading]
    foreach ($slug in $slugToNumber.Keys) {
        $body = $body.Replace("[[NOTE:$slug]]", "[$($slugToNumber[$slug])]")
    }
    $text = "$heading`n`n$body"
    $id = New-BeatRow $text
    $maxGlossarySortKey += 50
    Add-BeatNode $GlossaryNodeId $id $maxGlossarySortKey
}
Write-Host "Inserted $($glossary.Count) glossary entries"

# ---- Seed new entities (checked against gspl_entity_catalog.txt by name first; Jebel Quruntul,
# Zebedee, Herod Antipas, and Philip the tetrarch already exist and are not reseeded) ----
Seed-Entity "Via Maris" "via-maris" "place" "Ancient north-south trade route linking Damascus to the Mediterranean coast, passing directly through Capernaum on the Sea of Galilee's northwest shore."

$conn.Close()
Write-Host "DONE Matthew Chapter 4 depth pass."
