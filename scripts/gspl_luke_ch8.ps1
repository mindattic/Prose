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
$Ch8NodeId = [guid]"019FA969-F317-737B-98F0-E1CB606D1FF1"
$maxNoteSortKey = [double](Exec-Scalar "SELECT ISNULL(MAX(bn.SortKey),0) FROM BeatNodes bn WHERE bn.NodeId='019FA96B-18E2-7BB4-BAEB-11ACA24934F4'")
$maxGlossarySortKey = [double](Exec-Scalar "SELECT ISNULL(MAX(bn.SortKey),0) FROM BeatNodes bn WHERE bn.NodeId='019FA96B-29F5-7BB9-99D0-0F787960E530'")
$maxNoteNumber = [int](Exec-Scalar "SELECT MAX(CAST(LEFT(b.Text, CHARINDEX(' ',b.Text)-1) AS INT)) FROM BeatNodes bn JOIN Beats b ON bn.BeatId=b.Id WHERE bn.NodeId='019FA96B-18E2-7BB4-BAEB-11ACA24934F4'")
Write-Host "Starting: MaxNumber=$script:MaxNumber MaxNoteSortKey=$maxNoteSortKey MaxNoteNumber=$maxNoteNumber MaxGlossarySortKey=$maxGlossarySortKey"

$em = [char]8212

# ---- Notes ----
$notes = [ordered]@{
'sowing-before-plowing-galilee' = @{ title='Sowing before plowing explains the wasted seed'; body="Kenneth E. Bailey, Poet and Peasant and Through Peasant Eyes: A Literary-Cultural Approach to the Parables in Luke, combined ed. (Grand Rapids, MI: Eerdmans, 1983), section on the Parable of the Sower. Based on decades of residence among Middle Eastern peasant farming communities, Bailey argues broadcasting seed by hand before plowing $em not after $em was the customary sequence, meaning seed regularly and unremarkably fell on path, rocky ground, and thorn margins as a normal feature of the method rather than a sign of a careless farmer." }
'chuza-epitropos-title' = @{ title='A real administrative title, not a vague description'; body="Joseph A. Fitzmyer, The Gospel According to Luke I-IX, Anchor Bible vol. 28 (Garden City, NY: Doubleday, 1981), comment on Luke 8:3. Fitzmyer notes epitropos was a recognized Greco-Roman title for a steward or manager entrusted with a household's or estate's financial affairs, consistent with Chuza holding a genuine administrative post in Herod Antipas's court." }
'mary-magdalene-seven-demons-distinct' = @{ title='A clean introduction, no later conflation'; body="Cross-referenced against the standard account of Pope Gregory I's 591 CE homily conflating Mary Magdalene, Mary of Bethany, and the unnamed sinful woman of Luke 7 (already covered in this book). Modern scholarship, and the Catholic Church's own 1969 liturgical calendar revision, treat these as three separate women, with Mary Magdalene's own textual introduction occurring cleanly at Luke 8:2 with no reference to sin or prostitution." }
'galilee-clay-oil-lamps' = @{ title='A local lamp-manufacturing workshop'; body="Ariel David, 'Lost Village of Shikhin and Its Oil Lamp Industry Discovered in Israel,' Haaretz, December 25, 2024, reporting on the University of the Holy Land/Duke University Shikhin Excavation Project. The discovery of an active lamp-manufacturing workshop near Sepphoris, dated to the late first and early second centuries CE, confirms cheap, mass-produced clay oil lamps were locally made and widely available across Galilean villages." }
'josephus-james-brother-jesus' = @{ title="Josephus names Jesus's brother"; body="Flavius Josephus, Jewish Antiquities, Book 20, ch. 9, section 1 (20.200-203), Loeb Classical Library, trans. Louis H. Feldman, vol. 10 (Cambridge, MA: Harvard University Press, 1965). Josephus reports that in 62 CE the high priest Ananus had 'the brother of Jesus, who was called Christ, whose name was James' stoned; the reference is accepted as authentic by most scholars, though a minority view holds the clause 'who was called Christ' may be a later scribal addition." }
'galatians-james-lords-brother' = @{ title="Paul's own testimony"; body="Galatians 1:19 (NRSV); J. Louis Martyn, Galatians, Anchor Bible vol. 33A (New York: Doubleday, 1997), comment on 1:18-19. Paul's first-person account of meeting 'James, the Lord's brother' in Jerusalem, written within roughly two decades of the crucifixion, is treated as independent, near-contemporary attestation that Jesus had a named brother." }
'jerome-helvidius-perpetual-virginity' = @{ title='The confessional counter-reading'; body="Jerome, Adversus Helvidium de Mariae virginitate perpetua, ca. 383 CE, trans. W. H. Fremantle, Nicene and Post-Nicene Fathers, Series 2, vol. 6 (Buffalo, NY: Christian Literature Publishing Co., 1893). Jerome argues against Helvidius's plain-sense reading of the Gospels' 'brothers' as Mary's biological sons, proposing the Greek adelphos could carry the broader Semitic sense of 'kinsman,' identifying these figures as cousins in defense of Mary's perpetual virginity." }
'sea-of-galilee-storm-meteorology' = @{ title="Why the lake's storms arrive so fast"; body="Denis Baly, The Geography of the Bible, rev. ed. (London: Lutterworth Press, 1957; New York: Harper & Row, 1974), chapter on the Jordan Valley and Sea of Galilee. Baly documents that cold air draining from the elevated Golan plateau collides with warm air in the lake's below-sea-level basin, producing sudden, violent squalls independent of any narrative claim." }
'gerasenes-gadarenes-textual-variants' = @{ title='A three-way manuscript split'; body="Bruce M. Metzger, A Textual Commentary on the Greek New Testament, 2nd ed. (Stuttgart: Deutsche Bibelgesellschaft / United Bible Societies, 1994), textual notes on Mark 5:1 and parallels. Metzger documents the split between 'Gerasenes,' 'Gadarenes,' and 'Gergesenes' across the earliest witnesses, including Papyrus 75, Codex Alexandrinus, and Codex Sinaiticus, as a genuine, early textual problem." }
'gerasa-distance-sea-of-galilee' = @{ title='Too far for the pigs to run'; body="Entry on 'Gerasa,' The Anchor Bible Dictionary, ed. David Noel Freedman, vol. 2 (New York: Doubleday, 1992). Gerasa (modern Jerash, Jordan) is located roughly 55 kilometers southeast of the Sea of Galilee, a distance incompatible with the narrative's description of a swine herd running into the lake." }
'gergesa-kursi-site' = @{ title='Kursi: the site that actually fits'; body="Vassilios Tzaferis, 'New Archaeological Finds from Kursi-Gergesa,' Atiqot 79 (Jerusalem: Israel Antiquities Authority, 2014). Tzaferis's excavation reports on the Byzantine monastery at Kursi, on the lake's eastern shore, built to commemorate this exorcism story at the one point on that coastline with a steep bank descending to the water." }
'rock-cut-tombs-galilee-burial' = @{ title='Tombs large enough to shelter a living person'; body="Jodi Magness, 'Ossuaries and the Burials of Jesus and James,' Journal of Biblical Literature 124, no. 1 (2005): 121-154. Magness documents that rock-cut chamber tombs with carved recesses (kokhim) were physically large, multi-chambered structures distinct from simple pit burials $em spaces substantial enough that a living person could shelter within them for an extended period." }
'decapolis-gentile-pig-herding' = @{ title='Pig-herding as a marker of Gentile territory'; body="Entry on 'Decapolis,' The Oxford Encyclopedia of Archaeology in the Near East, ed. Eric M. Meyers, vol. 2 (New York: Oxford University Press, 1997). The Decapolis cities, including Gerasa and Gadara, were predominantly Hellenized and Gentile in population, a demographic context consistent with commercial-scale pig husbandry that carried no ritual complication for the herders, unlike under Jewish law." }
'legion-x-fretensis-boar-emblem' = @{ title="Reading 'Legion' as anti-Roman allegory"; body="Ched Myers, Binding the Strong Man: A Political Reading of Mark's Story of Jesus, 20th anniversary ed. (Maryknoll, NY: Orbis Books, 2008), chapter on the Gerasene demoniac. Myers reads the episode's military vocabulary as deliberate anti-Roman political allegory, noting Legio X Fretensis's reported boar emblem as a plausible resonance with the swine's destruction." }
'mark-gospel-date-jewish-war' = @{ title="Legio X Fretensis postdates Jesus's ministry"; body="Adam Winn, The Purpose of Mark's Gospel: An Early Christian Response to Roman Imperial Propaganda, WUNT 2.245 (Tubingen: Mohr Siebeck, 2008), introduction on dating Mark. Winn surveys the mainstream dating of Mark's composition to shortly before or after 70 CE, placing Legio X Fretensis's most visible regional presence closer to the Gospel's writing than to the narrative's own 30s CE setting." }
'twelve-years-structural-doubling' = @{ title="A deliberate narrative doubling"; body="Elizabeth Struthers Malbon, discussed in 'Jesus' Location at the Periphery: The Woman with the Flow of Blood and Jairus's Daughter,' Working Preacher (Luther Seminary). The paired twelve-year spans $em the girl's age and the woman's chronic illness $em are read as a deliberate narrative doubling contrasting sheltered centrality with marginalized suffering, resolved by the same intervention." }
'leviticus-15-chronic-bleeding' = @{ title='Twelve years of enforced ritual separation'; body="Leviticus 15:25-30 (NRSV); Jacob Milgrom, Leviticus 1-16, Anchor Bible vol. 3 (New York: Doubleday, 1991), comment on the zavah purity category. Milgrom explains any non-menstrual genital blood flow rendered a woman ritually impure for its entire duration, subject to touch-transmission rules, distinct from and lasting well beyond ordinary menstrual impurity." }
'theodotos-inscription-archisynagogos' = @{ title="A real title, confirmed in stone before 70 CE"; body="The Theodotos Inscription, discovered by Raymond Weill at the Ophel excavation site, Jerusalem, December 1913; discussed in John S. Kloppenborg, 'The Theodotos Synagogue Inscription and the Problem of First-Century Synagogue Buildings,' in Ancient Synagogues, ed. Dan Urman and Paul V. M. Flesher (Leiden: Brill, 1995). The inscription, dated before 70 CE, identifies its dedicator as a priest and archisynagogos following his father and grandfather in the same office, confirming this was a real, sometimes hereditary title in Second Temple Judaism." }
}

# ---- Chapter beats ----
$beat1 = @"
Luke opens this chapter with a detail easy to read past: Jesus is now touring "cities and villages" with a paid, organized traveling operation, and Luke names who bankrolled it (8:1-3). Three women are named -- Mary called Magdalene, "from whom seven demons had gone out"; Joanna, "wife of Chuza, Herod's steward"; and Susanna -- plus "many others" who "provided for them from their substance." This is the clean, first-name introduction of Mary Magdalene in Luke's own text, and it is worth pausing on precisely because of what it is not: nothing here calls her a prostitute, nothing links her to the unnamed sinful woman of the previous chapter's dinner scene, and nothing ties her to Mary of Bethany. That composite figure is a much later interpretive layer -- this book's Chapter 7 material already covers Pope Gregory the Great's sixth-century homily that fused those three separate women into one, a reading the Catholic Church itself has since walked back [[NOTE:mary-magdalene-seven-demons-distinct]].

Joanna's husband gets an actual administrative title in the Greek text: epitropos, "steward" or "manager" (8:3). This was a recognized office in a Herodian-scale household -- the trusted official who ran the day-to-day financial and estate affairs of a ruler's court [[NOTE:chuza-epitropos-title]]. If the title is accurate to Chuza's actual position, then Joanna was not a poor woman following an itinerant preacher out of desperation -- she was the wife of a senior palace official, funding a rival religious movement out of her own resources. Susanna gets no title and no other mention anywhere in the New Testament -- she is named once, here, and disappears from the record entirely.

The parable that follows (8:4-15) has generated a durable "aha" among readers encountering first-century farming for the first time: a farmer who scatters seed on a footpath, rocky ground, and among thorns before it lands on good soil looks careless by modern standards, where plowing precedes planting. Fieldwork among small Middle Eastern peasant farming communities documented a real, still-practiced alternative sequence: broadcasting seed by hand first, then plowing it under afterward, which means seed landing on a hard footpath, thin rocky soil, or a thorn-choked margin was an ordinary, expected feature of the method rather than evidence of an incompetent sower [[NOTE:sowing-before-plowing-galilee]].
"@

$beat2 = @"
The saying about not hiding a lit lamp under a jar but setting it on a stand (8:16) is parabolic rather than a claim about any specific event, so there is no historical claim to test in the usual sense. But the image rests on ordinary material culture archaeology has recovered in quantity: the standard household lamp across first-century Galilee was a small, cheap, wheel-made or mold-made clay vessel, found by the hundreds at excavated sites -- including a lamp-manufacturing workshop unearthed at Shikhin, near Sepphoris, actively producing these lamps for local and regional sale in exactly this period [[NOTE:galilee-clay-oil-lamps]]. A household lamp of this kind gave off a small, precious circle of light in a dark room -- hiding it under a storage jar wasn't a hypothetical inconvenience, it was a vivid, wasteful image anyone tending a real oil lamp at night would recognize instantly.
"@

$beat3 = @"
This short scene -- Jesus's mother and "brothers" arrive but can't get through the crowd, and Jesus responds that his true family is "those who hear the word of God and do it" -- is theologically about redefined kinship, but the passing mention of "brothers" (adelphoi) sits on top of one of the more genuinely interesting cross-checks in the Gospel record, because it isn't limited to the Gospels at all. Flavius Josephus, writing decades later with no stake in Christian theology, records that in 62 CE the high priest Ananus had "the brother of Jesus, who was called Christ, whose name was James" stoned along with others -- a passage most scholars treat as authentic, even though a minority view holds "who was called Christ" may be a later scribal gloss [[NOTE:josephus-james-brother-jesus]]. Independently, Paul -- writing decades before any Gospel was composed -- refers in passing to "James, the Lord's brother" as someone he personally met in Jerusalem (Galatians 1:19) [[NOTE:galatians-james-lords-brother]].

Here is the live divergence, and it runs along a confessional line rather than a fringe-versus-mainstream one. The historical-critical academy generally reads "brothers" as full or half brothers -- biological sons of Mary, or possibly of Joseph by an earlier marriage. Roman Catholic and Eastern Orthodox tradition, committed on doctrinal grounds to Mary's perpetual virginity, read the word differently: the fourth-century church father Jerome argued, against a priest named Helvidius who read "brothers" at face value, that adelphos could carry the broader sense of the Semitic ah, "kinsman," making these sons of a different Mary entirely [[NOTE:jerome-helvidius-perpetual-virginity]]. Both readings work from the same Greek word; they diverge on which extratextual doctrinal commitment gets to arbitrate an otherwise ordinary family-relationship term.
"@

$beat4 = @"
The disciples' boat is caught in a sudden, violent squall on the lake, and the detail that the storm arrives with almost no warning is not a narrative flourish -- it describes a real, well-documented feature of this specific body of water, independent of any miracle claim. The lake sits nearly 700 feet below sea level in a steep basin, ringed by highlands, most consequentially the Golan Heights to the east; cold air draining down off the elevated plateau meets the warm air sitting over the lake's basin, and the resulting temperature differential can turn a calm surface into six-to-ten-foot wind-driven waves within roughly half an hour [[NOTE:sea-of-galilee-storm-meteorology]]. This doesn't touch the miracle claim itself, but it does mean the storm's sudden arrival, and the disciples' terror at how fast it came, sits on solid, checkable ground.
"@

$beat5 = @"
This episode contains one of the New Testament's most genuinely interesting textual puzzles, starting with the place-name itself. The earliest and best Greek manuscripts don't agree on where this happened: some read "Gerasenes," others "Gadarenes," others "Gergesenes" -- three distinct readings surviving across the manuscript tradition [[NOTE:gerasenes-gadarenes-textual-variants]]. The geography forces the issue: Gerasa (modern Jerash) sits roughly 55 kilometers southeast of the Sea of Galilee, nowhere near a lakeshore a herd of pigs could stampede into [[NOTE:gerasa-distance-sea-of-galilee]]. The reading that actually fits the physical description -- a steep bank running down to the water -- points to a much smaller site: Gergesa, identified since the Byzantine period with the ruins at Kursi on the lake's eastern shore, where archaeologists have excavated a large monastery built specifically to commemorate this story [[NOTE:gergesa-kursi-site]]. Most modern textual critics read this as a case where "Gerasa," the largest and most famous city in the region, displaced the correct but obscure "Gergesa" in transmission.

The man's living arrangement -- dwelling among the tombs (8:27) -- matches documented burial practice: families with means cut chamber tombs directly into soft limestone hillsides, and these rock-cut tombs, with carved recesses (kokhim) for bodies, were physically large enough to shelter a living person for an extended stay [[NOTE:rock-cut-tombs-galilee-burial]]. The presence of an entire herd of pigs (8:32-33) is itself a strong marker of where this is happening: pig-herding at commercial scale is a Gentile economic activity, since pork was ritually forbidden under Jewish law, and the Decapolis was exactly this kind of Gentile-majority territory where raising swine carried no religious complication [[NOTE:decapolis-gentile-pig-herding]].

The detail that draws the most speculation is the demons' self-given name: "Legion" (8:30). Some New Testament scholars, including Ched Myers, read Mark's Gospel (Luke's likely source) as saturated with anti-Roman military vocabulary, noting Legio X Fretensis's reported boar emblem as a resonance with the swine's drowning [[NOTE:legion-x-fretensis-boar-emblem]]. But X Fretensis's well-documented presence in this specific region dates to the Jewish War of 66-73 CE and its aftermath -- decades after the narrative's own 30s CE setting, and closer to the actual composition of Mark's Gospel [[NOTE:mark-gospel-date-jewish-war]]. The likeliest honest read: "Legion" is a deliberately loaded, generic term for occupying military menace that would have landed hard on any first-century audience regardless of which specific unit was garrisoned where in a given decade.
"@

$beat6 = @"
Luke interlaces two healing stories here with a numerical detail too precise to be coincidental: Jairus's daughter is twelve years old (8:42), and the woman who touches Jesus's cloak on the way to her has been hemorrhaging for twelve years (8:43). Scholars reading this as deliberate narrative architecture point to the doubled number as the connective tissue: one female figure has lived a full life's span sheltered at the center of a household and is about to die; the other has spent the identical span pushed to the margins by a condition that made her ritually untouchable, and is restored [[NOTE:twelve-years-structural-doubling]].

The woman's condition connects to purity law already covered in this book: Leviticus 15 treats any abnormal, non-menstrual blood flow as a state of ritual impurity lasting as long as the discharge continues, transmissible by touch [[NOTE:leviticus-15-chronic-bleeding]]. Twelve years under that classification meant twelve years of enforced social and ritual separation -- which is exactly why her reaching out to touch Jesus's cloak in a crowd (8:44) is a genuinely transgressive act within that framework, not a minor social awkwardness.

Jairus himself is introduced with a real, independently attested title: archisynagogos, "ruler of the synagogue" (8:41). This is directly attested outside the New Testament by the Theodotos inscription, discovered in Jerusalem in 1913, in which a man identifies himself as a priest and archisynagogos following his father and grandfather in the same role; the inscription is dated before 70 CE, making it a genuine, pre-destruction, epigraphic confirmation that this title functioned exactly as the Gospels describe [[NOTE:theodotos-inscription-archisynagogos]].
"@

$beats = @($beat1, $beat2, $beat3, $beat4, $beat5, $beat6)

# ---- New glossary entries (unique to ch8) ----
$glossary = [ordered]@{
'MARY MAGDALENE' = "First named in Luke's own text at 8:2, as one of the women who traveled with Jesus and funded his ministry, described as a woman `"from whom seven demons had gone out.`" Nothing in this or any other canonical text identifies her as a prostitute or links her to the unnamed sinful woman of Luke 7:36-50 or to Mary of Bethany; that composite reading originates with a sixth-century homily by Pope Gregory the Great and has been widely set aside by modern scholarship [[NOTE:mary-magdalene-seven-demons-distinct]]."
'JOANNA (WIFE OF CHUZA)' = "Named at Luke 8:3 as one of the women who traveled with and financially supported Jesus's ministry. Identified by her marriage to Chuza, Herod Antipas's epitropos [[NOTE:chuza-epitropos-title]]."
'CHUZA' = "Named at Luke 8:3 as epitropos, `"steward`" or `"manager,`" in the court of Herod Antipas, and as the husband of Joanna. No extra-biblical record of a specific individual named Chuza has been identified; the title itself is independently attested as a genuine Greco-Roman household office [[NOTE:chuza-epitropos-title]]."
'SUSANNA (LUKE 8)' = "Named once, at Luke 8:3, among the women who financially supported Jesus's traveling ministry. She receives no further description or mention anywhere else in the New Testament."
'JAMES (BROTHER OF JESUS / JAMES THE JUST)' = "Distinct from the apostles James son of Zebedee and James son of Alphaeus already covered in this book's glossary, this James is identified in Luke 8:19-21 (implicitly, among the unnamed `"brothers`") and independently in Paul's letters and Josephus as Jesus's biological brother. Paul calls him `"the Lord's brother`" (Galatians 1:19) [[NOTE:galatians-james-lords-brother]]; Josephus records his stoning by order of the high priest Ananus in 62 CE [[NOTE:josephus-james-brother-jesus]]."
'JEROME' = "Fourth-century Christian scholar and translator of the Latin Vulgate, author of Adversus Helvidium (ca. 383 CE), a treatise defending Mary's perpetual virginity by arguing the Gospels' `"brothers`" of Jesus were cousins or broader kin [[NOTE:jerome-helvidius-perpetual-virginity]]."
'HELVIDIUS' = "A fourth-century Christian writer whose own treatise survives only through Jerome's rebuttal. Helvidius argued for the plain-sense reading of Jesus's `"brothers`" as Mary's own subsequent children by Joseph [[NOTE:jerome-helvidius-perpetual-virginity]]."
'GERASA' = "A major Hellenized city of the Decapolis, modern Jerash in Jordan, located roughly 55 kilometers southeast of the Sea of Galilee [[NOTE:gerasa-distance-sea-of-galilee]]. Appears in the best manuscripts as the site of the demoniac's healing (8:26), a reading most textual critics regard as a scribal substitution for the smaller, correctly-located Gergesa [[NOTE:gerasenes-gadarenes-textual-variants]]."
'GERGESA (KURSI)' = "A small site on the Sea of Galilee's eastern shore, identified since the Byzantine period as the likely actual location of the Gerasene demoniac story, on the strength of being the only stretch of that shoreline with a steep bank matching the narrative [[NOTE:gergesa-kursi-site]]."
'DECAPOLIS' = "A loose league of roughly ten Hellenized, largely Gentile cities on the eastern and southeastern fringe of Galilee, including Gerasa and Gadara [[NOTE:decapolis-gentile-pig-herding]]. Its Gentile-majority character explains the commercial pig-herding in the Gerasene demoniac episode."
'LEGIO X FRETENSIS' = "A Roman legion whose well-documented regional presence dates to the First Jewish-Roman War (66-73 CE) and its aftermath; its unit emblems reportedly included a boar, connected by some scholars to the demon `"Legion`" and the swine of Luke 8:30-33 [[NOTE:legion-x-fretensis-boar-emblem]]. Its attested presence postdates Jesus's ministry by decades [[NOTE:mark-gospel-date-jewish-war]]."
'JAIRUS' = "Named at Luke 8:41 as an archisynagogos, `"leader of the synagogue,`" whose twelve-year-old daughter Jesus raises. The title is independently and epigraphically attested by the pre-70 CE Theodotos inscription from Jerusalem [[NOTE:theodotos-inscription-archisynagogos]]."
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
    Add-BeatNode $Ch8NodeId $id $sortKey
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
Try-Append "FLAVIUS JOSEPHUS" "This chapter adds: Antiquities 20.9.1 records the 62 CE stoning of `"James, the brother of Jesus who was called Christ,`" independent non-Gospel attestation of a named sibling of Jesus [[NOTE:josephus-james-brother-jesus]]." $slugToNumber
Try-Append "HEROD ANTIPAS" "This chapter adds: maintained a formal household administration headed by an epitropos (Chuza), evidencing a functioning court bureaucracy [[NOTE:chuza-epitropos-title]]." $slugToNumber
Try-Append "MARY (MOTHER OF JESUS)" "This chapter adds: the perpetual-virginity tradition (Jerome vs. Helvidius) directly engages the `"brothers`" language of Luke 8:19-21 [[NOTE:jerome-helvidius-perpetual-virginity]]." $slugToNumber
Try-Append "LAKE OF GENNESARET" "This chapter adds: a specific, independently attested meteorological mechanism (Golan cold-air drainage into the below-sea-level basin) explains the lake's documented sudden storms [[NOTE:sea-of-galilee-storm-meteorology]]." $slugToNumber
Try-Append "SYNAGOGUE (FIRST-CENTURY)" "This chapter adds: the archisynagogos title held by Jairus is epigraphically attested pre-70 CE by the Theodotos inscription [[NOTE:theodotos-inscription-archisynagogos]]." $slugToNumber
Try-Append "SIMON (THE PHARISEE, LUKE 7)" "Cross-reference: Mary Magdalene's clean, distinct naming at Luke 8:2 is the textual anchor against which this entry's discussion of the Gregory the Great conflation links forward [[NOTE:mary-magdalene-seven-demons-distinct]]." $slugToNumber

# ---- Seed new entities ----
Seed-Entity "Joanna (wife of Chuza)" "joanna-wife-of-chuza" "character" "Female disciple/patron of Jesus's ministry, wife of Herod Antipas's household steward (Luke 8:3)."
Seed-Entity "Chuza" "chuza" "character" "Herod Antipas's epitropos (household steward); husband of Joanna."
Seed-Entity "Susanna (Luke 8)" "susanna-luke-8" "character" "Female disciple/patron named once at Luke 8:3."
# "James, brother of Jesus" already exists in the entity catalog (slug: james-brother-of-jesus) -- not reseeded here.
Seed-Entity "Helvidius" "helvidius" "character" "Fourth-century writer who argued Jesus had biological siblings, against Jerome."
Seed-Entity "Gergesa (Kursi)" "gergesa-kursi" "place" "Likely actual site of the Gerasene demoniac healing, on the Sea of Galilee's eastern shore."
Seed-Entity "Decapolis" "decapolis" "faction" "League of Hellenized Gentile cities east/southeast of Galilee."
Seed-Entity "Legio X Fretensis" "legio-x-fretensis" "faction" "Roman legion linked to the `"Legion`" demon name/boar-emblem resonance in Luke 8:30."
# Jairus already exists in the entity catalog (slug: jairus) -- not reseeded here.

$conn.Close()
Write-Host "DONE Chapter 8."
