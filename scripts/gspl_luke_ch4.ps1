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
$NotesNodeId = [guid]"019FA96B-18E2-7BB4-BAEB-11ACA24934F4"
$GlossaryNodeId = [guid]"019FA96B-29F5-7BB9-99D0-0F787960E530"
$Ch4NodeId = [guid]"019FA969-AF97-7AFA-991B-52064F38E463"

$maxNoteSortKey = [double](Exec-Scalar "SELECT ISNULL(MAX(bn.SortKey),0) FROM BeatNodes bn WHERE bn.NodeId='019FA96B-18E2-7BB4-BAEB-11ACA24934F4'")
$maxGlossarySortKey = [double](Exec-Scalar "SELECT ISNULL(MAX(bn.SortKey),0) FROM BeatNodes bn WHERE bn.NodeId='019FA96B-29F5-7BB9-99D0-0F787960E530'")
$maxNoteNumber = [int](Exec-Scalar "SELECT MAX(CAST(LEFT(b.Text, CHARINDEX(' ',b.Text)-1) AS INT)) FROM BeatNodes bn JOIN Beats b ON bn.BeatId=b.Id WHERE bn.NodeId='019FA96B-18E2-7BB4-BAEB-11ACA24934F4'")

Write-Host "Starting: MaxNumber=$script:MaxNumber MaxNoteSortKey=$maxNoteSortKey MaxNoteNumber=$maxNoteNumber MaxGlossarySortKey=$maxGlossarySortKey"

# ---- Notes (slug -> title/body) in order ----
$notes = [ordered]@{
'moses-forty-days-typology' = @{ title='Moses and the forty days'; body="Joseph A. Fitzmyer, The Gospel According to Luke I-IX, Anchor Bible vol. 28 (Garden City, NY: Doubleday & Company, 1981), commentary ad loc. Luke 4:1-2. Fitzmyer and the mainstream commentary tradition identify Jesus's forty-day wilderness fast as a deliberate literary echo of Moses's forty days without food on Sinai before receiving the law (Exodus 34:28), reading the number as a typological marker rather than an incidental chronological detail." }
'elijah-forty-days-typology' = @{ title="Elijah's forty days to Horeb"; body="Joseph A. Fitzmyer, The Gospel According to Luke I-IX, Anchor Bible vol. 28 (Garden City, NY: Doubleday & Company, 1981), commentary ad loc. Luke 4:1-13 (Old Testament background discussion). The same commentary tradition reads the forty days as also recalling Elijah's forty-day journey to Horeb/Sinai after fleeing Jezebel (1 Kings 19:8), so that Jesus's wilderness period draws together the two great 'forty-day' figures of the Law (Moses) and the Prophets (Elijah)." }
'deuteronomy-triple-citation' = @{ title="Three replies, one book"; body="Joseph A. Fitzmyer, The Gospel According to Luke I-IX, Anchor Bible vol. 28 (Garden City, NY: Doubleday & Company, 1981), commentary ad loc. Luke 4:4, 4:8, 4:12. All three of Jesus's scriptural rebuttals are drawn from Deuteronomy alone (8:3; 6:13; 6:16), a pattern mainstream commentary treats as evidence of deliberate literary composition -- a scripted duel of citations built to make a theological point about Israel's core confession -- rather than a verbatim transcript of an actual exchange." }
'jebel-quruntul-later-tradition' = @{ title="The Mount of Temptation, a fourth-century identification"; body="Jerome Murphy-O'Connor, The Holy Land: An Oxford Archaeological Guide from Earliest Times to 1700, 5th ed. (Oxford: Oxford University Press, 2008), entry on Jericho. The identification of Jebel Quruntul (Mount of Quarantania) above Jericho as the temptation site is a Christian pilgrimage tradition first solidly attested in the fourth century CE, with a monastery following in the sixth century -- a later geographic attachment to a story that itself names no location." }
'kingdoms-vision-literary-device' = @{ title="A panoramic vision, not a claim about geography"; body="David L. Mathewson, 'The Apocalyptic Vision of Jesus According to the Gospel of Matthew: Reading Matthew 3:16-4:11 Intertextually,' Tyndale Bulletin 62.1 (2011): 89-108. Mathewson situates the temptation narrative's vision of 'all the kingdoms of the world... in a moment of time' within the literary conventions of Second Temple apocalyptic vision writing, where compressed, instantaneous panoramic sight functions as a recognized device for making a cosmic claim rather than describing literal geography; the argument applies equally to Luke's parallel version of the same core tradition." }
'isaiah-58-insertion' = @{ title="A spliced citation: Isaiah 61 plus Isaiah 58"; body="Francois Bovon, Luke 1: A Commentary on the Gospel of Luke 1:1-9:50, Hermeneia series, trans. Christine M. Thomas (Minneapolis: Fortress Press, 2002), commentary ad loc. Luke 4:18-19. Luke's quotation inserts the clause 'to set at liberty those who are oppressed' into the middle of the Isaiah 61:1 citation; that phrase is not found in Isaiah 61 at all but is drawn from the Greek (Septuagint) text of Isaiah 58:6, making the reading a composite, edited citation rather than a straight quotation of a single passage." }
'isaiah-vengeance-clause-omission' = @{ title="Where the reading stops"; body="Joseph A. Fitzmyer, The Gospel According to Luke I-IX, Anchor Bible vol. 28 (Garden City, NY: Doubleday & Company, 1981), commentary ad loc. Luke 4:19. Isaiah 61:2 continues past 'the year of the LORD's favor' into 'the day of vengeance of our God'; Luke's version of Jesus's reading stops before that clause, a stopping point Fitzmyer and other commentators treat as a deliberate editorial choice framing the inaugurated ministry as grace-first rather than judgment-first." }
'synagogue-reading-practice-first-century' = @{ title="How standardized was the synagogue lectionary?"; body="Lee I. Levine, The Ancient Synagogue: The First Thousand Years, 2nd ed. (New Haven: Yale University Press, 2005), chapter on the origins and development of synagogue Torah-reading liturgy. Levine documents that while Sabbath Torah reading is attested early (including in Acts), the fixed annual (Babylonian) and triennial (Palestinian) lectionary cycles later described in rabbinic sources are only clearly documented centuries after the first century; the earliest-period practice may have left selection of the accompanying prophetic reading to the individual reader, exactly as Luke depicts." }
'nazareth-population-ken-dark' = @{ title="How big was Nazareth, really?"; body="Ken Dark, Archaeology of Jesus' Nazareth (Oxford: Oxford University Press, 2023), chapter on Roman-period settlement extent and population estimate; see also Ken Dark, 'The Archaeology of Nazareth in the Early First Century,' Bible and Interpretation (July 2020). Dark's excavation project revises older estimates of a Nazareth population in the low hundreds upward to as many as roughly a thousand residents in the early first century, still a small agricultural and quarrying village dwarfed by nearby Sepphoris." }
'nazareth-synagogue-not-excavated' = @{ title="No first-century synagogue found at Nazareth"; body="Donald D. Binder, Into the Temple Courts: The Place of the Synagogue in the Second Temple Period, SBL Dissertation Series 169 (Atlanta: Society of Biblical Literature, 1999), catalog of Second Temple-period synagogue excavations. Binder's catalog of confirmed pre-70 CE synagogue remains in Judea and Galilee (including Capernaum, Gamla, Masada, Herodium, and Magdala) does not include a synagogue building at Nazareth; no first-century synagogue structure has been excavated there, though the site's limited excavability (built over by the modern city) makes this an absence of evidence rather than confirmed evidence of absence." }
'floating-prophet-saying' = @{ title="A saying that shows up everywhere"; body="John P. Meier, A Marginal Jew: Rethinking the Historical Jesus, Volume One: The Roots of the Problem and the Person (New York: Doubleday, 1991), discussion of the criteria of authenticity, especially multiple attestation. Meier's standard treatment of the criterion of multiple independent attestation uses sayings like 'a prophet has no honor in his own country' -- found independently in Mark 6:4, Matthew 13:57, Luke 4:24, John 4:44, and Gospel of Thomas 31 -- as a strong candidate for an authentically early tradition precisely because it surfaces across sources with different literary relationships to one another." }
'mount-precipice-later-identification' = @{ title="The cliff tradition supplies its own site"; body="Jerome Murphy-O'Connor, The Holy Land: An Oxford Archaeological Guide from Earliest Times to 1700, 5th ed. (Oxford: Oxford University Press, 2008), entry on Nazareth. The Mount Precipice (Mt Kedumim) site south of Nazareth is a Byzantine-period identification of the cliff-throwing scene, its earliest physical trace a sixth-century mosaic floor rather than anything contemporary with the Gospel narrative, which itself names no specific site." }
'capernaum-synagogue-basalt-dispute' = @{ title="A synagogue's basalt foundation, disputed among its own excavators"; body="Jodi Magness, 'The Pottery from the Village of Capernaum and the Chronology of Galilean Synagogues,' Tel Aviv 39 (2012); compare Stanislao Loffreda, Recovering Capharnaum, 2nd ed. (Jerusalem: Franciscan Printing Press / Studium Biblicum Franciscanum, 1993). Excavators Virgilio Corbo and Stanislao Loffreda disagreed on whether basalt walls beneath Capernaum's later white-limestone synagogue belonged to an original first-century synagogue on the site or to a later intermediate phase; Magness's independent ceramic and numismatic analysis argues Galilean-type synagogues generally date later than early excavators proposed, making the 'first-century synagogue at this exact spot' claim a live scholarly dispute rather than settled consensus." }
'capernaum-house-of-peter' = @{ title="A room singled out for veneration, early"; body="James F. Strange and Hershel Shanks, 'Has the House Where Jesus Stayed in Capernaum Been Found?' Biblical Archaeology Review 8, no. 6 (1982). Excavators Virgilio Corbo and Stanislao Loffreda found a modest first-century-BCE house at Capernaum with one room distinctively plastered and painted from as early as the mid-first century CE, plus later graffiti invoking 'Peter'; Strange and Shanks, reporting the find, were explicit that this circumstantial case falls short of proof and may never be provable outright." }
'ancient-illness-possession-framework' = @{ title="Disease versus illness in the ancient Mediterranean world"; body="John J. Pilch, Healing in the New Testament: Insights from Medical and Mediterranean Anthropology (Minneapolis: Fortress Press, 2000). Pilch applies the medical-anthropological distinction between 'disease' (a biomedical construct) and 'illness' (a culturally constructed meaning of sickness) to Gospel healing and exorcism accounts, arguing that ancient Mediterranean audiences understood affliction and healing/restoration in social and cosmic terms not reducible to modern diagnostic categories." }
'josephus-eleazar-exorcist' = @{ title="An eyewitnessed exorcism before Vespasian"; body="Flavius Josephus, Jewish Antiquities, Book 8, sections 42-49 (Loeb Classical Library, trans. H. St. J. Thackeray, Cambridge, MA: Harvard University Press). Josephus, writing as a contemporary eyewitness, describes a Jewish exorcist named Eleazar performing an exorcism using a ring, a root, and invocations attributed to Solomon, in the presence of the Roman commander Vespasian, his sons, and his officers." }
'tacitus-vespasian-healing' = @{ title="Tacitus records a healing at Alexandria"; body="Cornelius Tacitus, Histories, Book 4, chapter 81 (Loeb Classical Library, trans. Clifford H. Moore, Cambridge, MA: Harvard University Press, 1931). Tacitus records, on claimed eyewitness testimony, that while at Alexandria the emperor Vespasian was petitioned by a blind man and a man with a withered hand, both of whom reportedly recovered after Vespasian touched them, per instructions each said they had received from the god Serapis." }
'apollonius-tyana-parallel' = @{ title="A later, thinner parallel"; body="Philostratus, Life of Apollonius of Tyana (early third century CE); background summarized in standard historical-Jesus comparative scholarship. Philostratus's biography, written more than a century after Apollonius's own first-century lifetime, attributes to him healings, exorcisms, and other wonders comparable to those attributed to Jesus, but the century-plus gap between the reported events and their only detailed written source makes the comparison a weaker independent check than the Gospels' own decades-long transmission gap." }
'vermes-galilean-hasidim' = @{ title="A recognized category of Galilean holy man"; body="Geza Vermes, Jesus the Jew: A Historian's Reading of the Gospels (London: Collins, 1973). Vermes situates Jesus within a recognized category of Galilean charismatic holy man (Hasid), comparing him to Honi the Circle-Drawer (active in the first century BCE) and Hanina ben Dosa (a rough contemporary of Jesus), both remembered in rabbinic sources as wonder-workers whose reputed powers the Mishnah says faded after the Second Temple's destruction." }
'luke-4-44-judea-galilee-variant' = @{ title="Judea or Galilee? A manuscript split"; body="Textual apparatus of the Nestle-Aland Novum Testamentum Graece, ad loc. Luke 4:44, as reflected in the ESV/NIV ('synagogues of Judea') versus KJV/NKJV ('synagogues of Galilee') translation split. The earliest major manuscripts (Codex Vaticanus, Codex Sinaiticus) read 'Judea,' while many later manuscripts read 'Galilee' (which fits the immediate narrative context, since Jesus has not yet left Galilee); modern critical editions and translations remain divided on which reading is original." }
}

# ---- Chapter beats (with [[NOTE:slug]] placeholders) ----
$beat1 = @'
Luke says Jesus, freshly baptized and "full of the Holy Spirit," is led by that same Spirit into the wilderness, where he fasts forty days while the devil tests him three times -- turn stone to bread, take dominion over the world's kingdoms in exchange for worship, force God's hand by leaping from the Temple -- and each time Jesus answers with a line from Deuteronomy before the devil departs "until an opportune time" (4:1-13).

The number is the first thing worth pausing on, because it isn't a neutral chronological detail -- "forty days" is Israel's own number, and Luke's audience would have heard it that way immediately. Moses fasted forty days and nights on Sinai before receiving the law (Exodus 34:28), and the mainstream historical-critical reading of this scene treats the parallel as a deliberate compositional choice: Jesus's wilderness ordeal recapitulates the formative discipline of Israel's founding lawgiver rather than simply reporting an incidental span of time [[NOTE:moses-forty-days-typology]]. The same commentary tradition layers in a second echo: Elijah, fleeing after his confrontation with Jezebel's prophets, travels forty days to Horeb -- another name for Sinai -- before his own encounter with God (1 Kings 19:8) [[NOTE:elijah-forty-days-typology]]. Between them, Moses (the law) and Elijah (the prophets) cover the two halves of Israel's scriptural self-understanding, and Jesus's forty days gathers both into one scene at the start of his public work.

The reply pattern sharpens the same point. All three of Jesus's counters are drawn from a single book -- Deuteronomy 8:3 answers the bread test (4:4), Deuteronomy 6:13 answers the kingdoms test (4:8), and Deuteronomy 6:16 answers the Temple test (4:12) [[NOTE:deuteronomy-triple-citation]]. A person improvising under duress doesn't usually reach for the same scroll three times in a row; the effect reads less like a transcript and more like a composed contest of citation, built to make a theological point about Israel's core confession rather than to record verbatim dialogue word for word.

Luke never names where this took place -- "the wilderness" is as specific as the text gets. The now-famous location, Jebel Quruntul rising above Jericho, enters Christian tradition only in the fourth century, with a monastery following in the sixth [[NOTE:jebel-quruntul-later-tradition]]. That's a textbook case of Legendary Accretion: a devotional and touristic need for a place to stand where a landscape gets retrofitted onto a story that itself never specifies one.

The devil's second offer raises its own "wait, actually" question: he shows Jesus "all the kingdoms of the world... in a moment of time" (4:5). Taken as literal geography, no vantage point on earth permits that view, and unlike Matthew's version of the same scene -- which places it atop "a very high mountain" -- Luke doesn't even give it a mountain to stand on. Scholarship on the passage's genre situates this within the conventions of Second Temple apocalyptic vision literature, where a compressed, instantaneous, panoramic sight functions as a recognized device for making a cosmic claim rather than a travelogue [[NOTE:kingdoms-vision-literary-device]]. What remains genuinely open is whether Luke intends his readers to picture an actual visionary experience, or a purely rhetorical flourish -- the text gives no basis for choosing between those readings.
'@

$beat2 = @'
Back in Galilee, Jesus teaches in synagogues to acclaim, then returns to Nazareth, stands up in the synagogue on the Sabbath, is handed the Isaiah scroll, reads a passage announcing good news to the poor and liberty to captives, sits down and declares it fulfilled that very day -- and the crowd's admiration curdles fast into "Is not this Joseph's son?" When Jesus answers with the proverb that a prophet finds no honor in his own country, and adds that Elijah and Elisha both worked their greatest wonders for foreigners rather than Israelites, the congregation is enraged enough to drive him to the edge of town intending to throw him off a cliff; he escapes and moves on (4:14-30).

Start with the quotation itself, because Luke's version of Isaiah 61 is not a clean lift. Isaiah 61:1-2 in its fuller form moves from "the year of the LORD's favor" straight into "the day of vengeance of our God" -- and Luke's Jesus stops reading exactly at the favor clause, leaving the vengeance clause out entirely [[NOTE:isaiah-vengeance-clause-omission]]. At the same time, Luke's quotation inserts a phrase that isn't in Isaiah 61 at all: "to set at liberty those who are oppressed" comes from the Greek text of Isaiah 58:6, spliced into the middle of the 61:1 quotation [[NOTE:isaiah-58-insertion]]. Whether this composite, edited text reflects Jesus's own selective reading that day or Luke's editorial hand, historical-critical scholars agree it is a deliberately shaped citation, not a passive transcription.

The mechanics of the scene -- standing to read, being handed a scroll, finding a chosen passage, sitting to teach afterward -- are broadly plausible for the period, but "plausible" is different from "verified in detail." Torah reading in the synagogue is attested early, and later rabbinic sources describe both an annual Babylonian reading cycle and a three-year Palestinian one -- but both fixed lectionary cycles are only clearly documented centuries after this scene, and it isn't established that a rigid Sabbath-by-Sabbath schedule pairing specific Torah and Prophets portions existed yet in the 20s-30s CE; it's entirely possible the prophetic reading was, as Luke depicts here, left to the individual reader to select [[NOTE:synagogue-reading-practice-first-century]].

Nazareth itself is worth a beat of its own. Ken Dark's Nazareth Archaeological Project has argued the settlement may have supported a larger population than once assumed -- up to roughly a thousand people, rather than the older estimate of a few hundred -- built on quarrying and agriculture, still dwarfed by the much larger neighboring city of Sepphoris a few miles away [[NOTE:nazareth-population-ken-dark]]. What has not turned up, however, is an actual first-century synagogue building at the site. Ten-plus pre-70 CE synagogues have been excavated elsewhere in Judea and Galilee, and Nazareth is not among them [[NOTE:nazareth-synagogue-not-excavated]]. That's a real, open archaeological gap, not a settled negative: a Jewish village of Nazareth's kind is exactly the sort of place scholars would expect to have had some assembly space, but the text's specific image of Jesus standing up in a recognizable communal building rests on the narrative alone.

The proverb Jesus quotes -- "no prophet is accepted in his own country" -- deserves its own note, because it isn't unique to Luke. Versions of the same saying appear independently in Mark 6:4, Matthew 13:57, John 4:44, and the non-canonical Gospel of Thomas [[NOTE:floating-prophet-saying]]. Wide, independent circulation across sources that don't all derive from one another is exactly the kind of evidence historical-critical scholars weigh heavily when judging a saying's antiquity. What that multiple attestation does not establish is Luke's specific dramatic staging of it: only Luke ties the saying to a violent expulsion and an attempted cliff-throwing at Nazareth.

And the cliff itself: Nazareth does sit on a genuinely hilly ridge, so a mob marching someone to a "brow of the hill" (4:29) isn't topographically strained. But the specific site shown to visitors today -- Mount Precipice, south of town -- is itself a Byzantine-period identification, its earliest physical marker a sixth-century mosaic floor rather than anything contemporary with the Gospel events [[NOTE:mount-precipice-later-identification]]. Luke names no specific cliff; later tradition supplied one, the same way it supplied Jebel Quruntul for the temptation.
'@

$beat3 = @'
Jesus moves to Capernaum, teaches in the synagogue with an authority that impresses the crowd, casts an "unclean demon" out of a man who publicly recognizes him, heals Simon's mother-in-law of a fever in Simon's own house, and by sundown is healing and casting out spirits from a crowd that has gathered at the door before withdrawing to pray and continuing to preach in the region's synagogues (4:31-44).

Capernaum's synagogue is one of the more genuinely contested pieces of Gospel archaeology. The striking white limestone synagogue building tourists see today dates to the fourth or fifth century CE -- that part isn't disputed. What sits underneath it is a set of black basalt walls, and there the agreement stops: excavator Virgilio Corbo argued the basalt belonged to an original first-century synagogue on the same spot, while his own excavation partner Stanislao Loffreda placed it later, and Jodi Magness has argued on ceramic and numismatic grounds that Galilean-type synagogues generally date later than their excavators originally proposed [[NOTE:capernaum-synagogue-basalt-dispute]]. So the honest state of the evidence is a genuine, ongoing disagreement among the archaeologists who dug the site, not a settled first-century floor plan.

The "House of Peter," a few dozen meters away, is a comparatively stronger case, though still short of proof. Beneath a fifth-century octagonal Byzantine church, Franciscan excavators found a modest first-century-BCE dwelling that saw continuous domestic use, with one room -- and only one -- plastered and painted from as early as the mid-first century CE, alongside later graffiti invoking "Peter" [[NOTE:capernaum-house-of-peter]]. That is a real, unusually early trace of a specific room being singled out for veneration well before Constantine made Christianity legal. What it does not do is name its own former occupant; the excavators themselves were careful to say that proof positive was lacking and might never come.

The illness and possession language itself is worth pausing on before reading anything modern into it. Ancient Mediterranean cultures generally did not distinguish between what we'd call biological disease and social/spiritual "illness" the way modern medicine does; a healing or exorcism restored a person to their place in the community as much as it addressed a physical symptom [[NOTE:ancient-illness-possession-framework]].

That framework is worth setting against the wider record, because exorcism and miraculous healing were not narrative devices unique to the Gospels. Josephus, writing as a contemporary and no friend of the Christian movement, describes witnessing a real Jewish exorcist named Eleazar drive a demon from a man's nostrils using a ring, a root, and Solomon's name, in front of the Roman commander Vespasian and his officers [[NOTE:josephus-eleazar-exorcist]]. Tacitus separately records, as claimed eyewitness testimony, that Vespasian himself was petitioned by a blind man and a man with a withered hand at Alexandria and that both were reportedly healed [[NOTE:tacitus-vespasian-healing]]. Later still, the third-century biography of the philosopher Apollonius of Tyana credits him with healings and exorcisms strikingly similar to Jesus's, though that account was written more than a century after his own life [[NOTE:apollonius-tyana-parallel]]. Closer to home, Geza Vermes situated Jesus within a specifically Galilean type of Jewish charismatic holy man alongside Honi the Circle-Drawer and Hanina ben Dosa [[NOTE:vermes-galilean-hasidim]]. None of this can confirm what happened in a specific Capernaum house on a specific Sabbath. What it does establish is that healer-and-exorcist figures operating within a broadly shared conceptual world were an independently attested, recognized social role in this period.

One small, genuine textual puzzle closes the chapter. Luke says Jesus went on "preaching in the synagogues of Judea" (4:44) -- but the oldest and best manuscripts actually read "Judea" here, while a great many later manuscripts read "Galilee," which fits the immediate narrative context far more comfortably [[NOTE:luke-4-44-judea-galilee-variant]] -- a small reminder that even a single word's wording can rest on a manuscript judgment call.
'@

$beats = @($beat1, $beat2, $beat3)

# ---- Glossary additions (heading -> body) ----
$glossary = [ordered]@{
'THE DEVIL (SATAN)' = "The tempter figure who confronts Jesus in the wilderness across all three temptations (4:1-13), described by Luke as ``the devil'' and departing ``until an opportune time,'' a phrase that signals his later reappearance in the Passion narrative. The scene's literary construction -- built around a scripted exchange of scriptural citations rather than a plausible transcript -- is well attested [[NOTE:deuteronomy-triple-citation]], but nothing about the tempter's nature or reality is a claim external evidence could speak to one way or the other."
'ISAIAH (PROPHET)' = "The eighth-century BCE Judean prophet whose scroll Jesus is handed in the Nazareth synagogue and reads from (4:17-19), quoting a composite of Isaiah 61:1-2 with an inserted clause from Isaiah 58:6 and an omitted clause from 61:2 itself [[NOTE:isaiah-58-insertion]] [[NOTE:isaiah-vengeance-clause-omission]]."
'CAPERNAUM' = "A fishing village on the northwest shore of the Sea of Galilee that becomes Jesus's operational base for much of his Galilean ministry, first entered in this chapter (4:31). Archaeology at the site is unusually rich but also unusually contested: the visible white-limestone synagogue building dates to the fourth or fifth century CE, and whether basalt remains beneath it preserve an actual first-century synagogue is a live, unresolved dispute among the site's own excavators [[NOTE:capernaum-synagogue-basalt-dispute]]. A separate excavated structure nearby, beneath a fifth-century octagonal church, is a strong (though not certain) candidate for the historical ``House of Peter'' referenced in this chapter's healing of Simon's mother-in-law [[NOTE:capernaum-house-of-peter]]."
'SIMON (PETER)' = "A resident of Capernaum whose house Jesus enters and whose mother-in-law he heals of a high fever (4:38-39); this is Simon's first appearance in Luke's narrative, ahead of his formal call to discipleship in the following chapter. He is not yet given the added name ``Peter'' here. The ``House of Peter'' identified by twentieth-century excavation at Capernaum is a later, separately argued archaeological claim about his residence, not something the text itself locates for the reader [[NOTE:capernaum-house-of-peter]]."
'SYNAGOGUE (FIRST-CENTURY)' = "The Jewish communal institution for Sabbath assembly, Torah and Prophets reading, and teaching, featured in both the Nazareth (4:16-30) and Capernaum (4:31-37) scenes. In this period a synagogue could be either a purpose-built structure or simply a designated meeting space; a fixed weekly lectionary pairing specific Torah and Prophets readings is documented only in later rabbinic-era sources [[NOTE:synagogue-reading-practice-first-century]]. Ten-plus purpose-built synagogue buildings dated before 70 CE have been excavated in Judea and Galilee, but none yet at Nazareth [[NOTE:nazareth-synagogue-not-excavated]]."
'JEBEL QURUNTUL (MOUNT OF TEMPTATION)' = "A mountain above Jericho in the West Bank, identified since at least the fourth century CE by Christian pilgrimage tradition as the site of Jesus's wilderness temptation (4:1-13), with a monastery established there in the sixth century. The identification is not made by the biblical text itself, which names no location for the temptation scene [[NOTE:jebel-quruntul-later-tradition]]."
'MOUNT OF PRECIPICE (MT KEDUMIM)' = "A steep hillside south of Nazareth overlooking the Jezreel Valley, identified since the Byzantine period as the cliff to which the Nazareth crowd drove Jesus intending to throw him off (4:29). The earliest physical trace of the tradition is a sixth-century mosaic floor at the summit; Luke's own text specifies only ``the brow of the hill on which their town was built,'' without naming a location [[NOTE:mount-precipice-later-identification]]."
'ELEAZAR (EXORCIST)' = "A Jewish exorcist described by the first-century historian Flavius Josephus as an eyewitnessed contemporary, who performed an exorcism using a ring, a root, and invocations of Solomon's name in the presence of the Roman commander (later emperor) Vespasian and his officers. Cited in this chapter as independent, non-Christian evidence that exorcism was a recognized, practiced social role in first-century Judea [[NOTE:josephus-eleazar-exorcist]]."
'VESPASIAN' = "Roman general and later emperor (r. 69-79 CE), reported by the Roman senator and historian Tacitus to have healed a blind man and a man with a withered hand at Alexandria through touch, acting on instructions the two petitioners said came from the god Serapis. Cited as an independent, non-Jewish, non-Christian parallel for miraculous-healing claims circulating about a public figure within living memory of the events [[NOTE:tacitus-vespasian-healing]]."
'APOLLONIUS OF TYANA' = "A first-century CE Greek philosopher and wandering teacher later credited, in a biography written more than a hundred years after his death, with healings, exorcisms, and other wonders broadly comparable to those attributed to Jesus. The long gap between his life and his only detailed written source makes the comparison weaker evidentially than it might first appear [[NOTE:apollonius-tyana-parallel]]."
'HONI THE CIRCLE-DRAWER' = "A first-century BCE Jewish charismatic holy man remembered in rabbinic tradition chiefly as a successful rainmaker. Cited by historian Geza Vermes as part of a recognized regional category of Galilean-adjacent Jewish wonder-workers that Jesus's reported healing and exorcism activity in this chapter resembles [[NOTE:vermes-galilean-hasidim]]."
'HANINA BEN DOSA' = "A first-century CE Galilean Jewish holy man remembered in rabbinic sources as a healer and ``man of deeds,'' roughly a contemporary of Jesus; the Mishnah states that the class of wonder-workers he represented lost their powers around the time of the Second Temple's destruction [[NOTE:vermes-galilean-hasidim]]."
}

# ---- Insert Notes ----
$slugToNumber = @{}
foreach ($slug in $notes.Keys) {
    $n = $notes[$slug]
    $noteNum = $maxNoteNumber + 1
    $maxNoteNumber = $noteNum
    $text = "$noteNum - $($n.title)`n`n$($n.body)"
    $id = New-BeatRow $text
    $maxNoteSortKey += 50
    Add-BeatNode $NotesNodeId $id $maxNoteSortKey
    $slugToNumber[$slug] = $noteNum
}
Write-Host "Inserted $($notes.Count) notes, ending at [$maxNoteNumber]"

# ---- Insert chapter beats with placeholder replacement ----
$sortKey = 0.0
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

# ---- Seed new entities ----
Seed-Entity "Jebel Quruntul (Mount of Temptation)" "jebel-quruntul-mount-of-temptation" "place" "Traditional (4th-century) pilgrimage site above Jericho identified with Jesus's wilderness temptation."
Seed-Entity "Mount of Precipice (Mt Kedumim)" "mount-of-precipice-mt-kedumim" "place" "Byzantine-era site south of Nazareth identified with the cliff-throwing scene of Luke 4:29."
Seed-Entity "Apollonius of Tyana" "apollonius-of-tyana" "character" "First-century Greek philosopher later credited with healings and exorcisms comparable to Jesus's, per a biography written over a century after his death."
Seed-Entity "Hanina ben Dosa" "hanina-ben-dosa" "character" "First-century CE Galilean Jewish holy man and healer, rough contemporary of Jesus, remembered in rabbinic sources."
Seed-Entity "Synagogue (first-century)" "synagogue-first-century" "vocabulary" "Jewish communal institution for Sabbath assembly, Torah/Prophets reading, and teaching; could be purpose-built or an informal meeting space in this period."

$conn.Close()
Write-Host "DONE Chapter 4."
