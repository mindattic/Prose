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
    $cmd = $conn.CreateCommand()
    $cmd.CommandText = $sql
    foreach ($k in $params.Keys) { $cmd.Parameters.AddWithValue("@$k", $params[$k]) | Out-Null }
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

function Append-ToExistingBeat([guid]$beatId, [string]$extraParagraph) {
    $cmd = $conn.CreateCommand()
    $cmd.CommandText = "SELECT Text FROM Beats WHERE Id=@Id"
    $cmd.Parameters.AddWithValue("@Id", $beatId) | Out-Null
    $current = $cmd.ExecuteScalar()
    $updated = "$current`n`n$extraParagraph"
    $hash = Sha256Hex $updated
    $u = $conn.CreateCommand()
    $u.CommandText = "UPDATE Beats SET Text=@Text, TextHash=@Hash, UpdatedAt=SYSUTCDATETIME() WHERE Id=@Id"
    $u.Parameters.AddWithValue("@Text", $updated) | Out-Null
    $u.Parameters.AddWithValue("@Hash", $hash) | Out-Null
    $u.Parameters.AddWithValue("@Id", $beatId) | Out-Null
    $u.ExecuteNonQuery() | Out-Null
}

function Find-GlossaryBeatId([string]$headingPrefix) {
    $cmd = $conn.CreateCommand()
    $cmd.CommandText = "SELECT TOP 1 b.Id FROM BeatNodes bn JOIN Beats b ON bn.BeatId=b.Id WHERE bn.NodeId='019FA96B-29F5-7BB9-99D0-0F787960E530' AND b.Text LIKE @pat"
    $cmd.Parameters.AddWithValue("@pat", "$headingPrefix%") | Out-Null
    return $cmd.ExecuteScalar()
}

# ---- Live state ----
$script:MaxNumber = [int](Exec-Scalar "SELECT MAX(Number) FROM Beats")
$NotesNodeId = [guid]"019FA96B-18E2-7BB4-BAEB-11ACA24934F4"
$GlossaryNodeId = [guid]"019FA96B-29F5-7BB9-99D0-0F787960E530"
$Ch5NodeId = [guid]"019FA969-C063-70F5-B56D-CA0D956B34FA"

$maxNoteSortKey = [double](Exec-Scalar "SELECT ISNULL(MAX(bn.SortKey),0) FROM BeatNodes bn WHERE bn.NodeId='019FA96B-18E2-7BB4-BAEB-11ACA24934F4'")
$maxGlossarySortKey = [double](Exec-Scalar "SELECT ISNULL(MAX(bn.SortKey),0) FROM BeatNodes bn WHERE bn.NodeId='019FA96B-29F5-7BB9-99D0-0F787960E530'")
$maxNoteNumber = [int](Exec-Scalar "SELECT MAX(CAST(LEFT(b.Text, CHARINDEX(' ',b.Text)-1) AS INT)) FROM BeatNodes bn JOIN Beats b ON bn.BeatId=b.Id WHERE bn.NodeId='019FA96B-18E2-7BB4-BAEB-11ACA24934F4'")

Write-Host "Starting: MaxNumber=$script:MaxNumber MaxNoteSortKey=$maxNoteSortKey MaxNoteNumber=$maxNoteNumber MaxGlossarySortKey=$maxGlossarySortKey"

$em = [char]8212

# ---- Notes ----
$notes = [ordered]@{
'kinneret-boat-dating' = @{ title='The Kinneret Boat'; body="Shelley Wachsmann, The Sea of Galilee Boat: An Extraordinary 2000 Year Old Discovery, 3rd ed. (College Station: Texas A&M University Press, 2009; orig. New York: Plenum, 1995). The excavation report and popular synthesis of the January 1986 find near Kibbutz Ginosar: a hull roughly 8.2 x 2.3 meters, built with recycled cedar and other woods joined by mortise-and-tenon and pegging, radiocarbon- and pottery-dated to approximately 100 BCE$em 70 CE $em a working-class fishing vessel contemporary with the Gospel setting, now displayed at the Yigal Alon Museum, Kibbutz Ginosar." }
'galilee-fishing-tax-farming' = @{ title='A taxed, licensed lake'; body="K. C. Hanson, 'The Galilean Fishing Economy and the Jesus Tradition,' Biblical Theology Bulletin 27 (1997): 99-111. Documents that Herod Antipas held an effective monopoly over Galilee's inland water resources, that fishing required purchased licenses, that tax brokers bid at auction for the right to collect tolls and lease fishing rights to working fishermen, and that the customs house excavated at Capernaum is physical evidence of how deeply this taxation regime reached into ordinary village fishing life." }
'koinonoi-fishing-partnership' = @{ title="Luke's word for 'partner'"; body="K. C. Hanson, 'The Galilean Fishing Economy and the Jesus Tradition,' Biblical Theology Bulletin 27 (1997): 99-111. Analyzes Luke's use of koinonos (Luke 5:10) as reflecting an organized, shared-risk commercial fishing association rather than casual friendship, consistent with the broader first-century Galilean pattern of family-based fishing cooperatives pooling boats, nets, and hired labor." }
'luke-net-terminology' = @{ title='Seine versus casting net'; body="Richard Chenevix Trench, Synonyms of the New Testament, s.v. diktyon, amphiblestron, sagene (London: Macmillan, various editions from 1854 onward; widely reprinted; LoC catalog record under 'Trench, Richard Chenevix, 1807-1886'). Establishes the philological distinction between diktyon, the general term Luke uses (denoting the larger seine net paid out and hauled from a boat), and amphiblestron, the smaller circular casting net used in the parallel calling accounts in Mark 1:16 and Matthew 4:18 $em a distinction corroborated across standard Koine Greek lexicography." }
'tzaraat-not-hansens' = @{ title="'Leprosy' was not Hansen's disease"; body="David L. Kaplan, 'Biblical Leprosy: An Anachronism Whose Time Has Come,' Journal of the American Academy of Dermatology 28, no. 3 (1993): 507-510. Argues, with a differential-diagnosis review, that biblical tzaraat/lepra was a broad catch-all term for various visible skin conditions (psoriasis, vitiligo, favus, fungal and other dermatoses) and was almost certainly not Hansen's disease (modern leprosy); traces the mistaken equivalence to the Septuagint's Greek rendering of tzaraat as lepra, a term whose meaning shifted toward true Hansen's disease only as that illness spread into the Mediterranean world in later centuries." }
'mishnah-negaim-date' = @{ title='Negaim: a later legal elaboration'; body="Jacob Neusner, trans., The Mishnah: A New Translation (New Haven: Yale University Press, 1988), general introduction and Negaim (Order Tohorot). Establishes the Mishnah's redaction under Rabbi Judah ha-Nasi around 200 CE and describes tractate Negaim's fourteen chapters as a detailed legal elaboration $em priestly examination criteria, timing, isolation procedure $em built directly on the skin-disease law of Leviticus 13-14." }
'qumran-skin-disease-purity' = @{ title='Qumran already had this procedure'; body="Joseph M. Baumgarten, Qumran Cave 4.XIII: The Damascus Document (4Q266-273), Discoveries in the Judaean Desert 18 (Oxford: Clarendon Press, 1996), 4Q266 fragment 6, column i, line 13 (parallel text 4Q272 fragment 1, column ii, line 2). Presents the Qumran community's own Damascus Document rule assigning 'the sons of Aaron' the priestly duty to examine and separate persons with skin disease, using Leviticus-derived diagnostic criteria, in manuscripts dated earlier than the Gospels themselves." }
'capernaum-house-construction' = @{ title='A roof you could open and patch'; body="Virgilio C. Corbo and Stanislao Loffreda's Capernaum excavations (1968-1986), as synthesized in Jonathan L. Reed, Archaeology and the Galilean Jesus: A Re-Examination of the Evidence (Harrisburg, PA: Trinity Press International, 2000), chapter on Capernaum domestic architecture. Documents Capernaum's first-century houses as built of rough, undressed basalt blocks packed with mud, roofed with basalt beams spanning thinner slabs sealed and waterproofed with branches, thatch, and rolled mud, accessed by exterior courtyard stairways to the roof $em a maintainable surface consistent with the roof-opening scene in Luke 5:19." }
'scribes-pharisees-historical' = @{ title='Scribes and Pharisees were not the same thing'; body="Anthony J. Saldarini, Pharisees, Scribes, and Sadducees in Palestinian Society: A Sociological Approach (Grand Rapids: Eerdmans, 2001; orig. Wilmington, DE: Michael Glazier, 1988). A sociological analysis of Josephus's roughly twenty scattered references to the Pharisees as one school of thought among several, arguing modern scholarship should be cautious about assuming the Pharisees were as numerous or uniformly influential as later Christian and rabbinic sources imply, and that scribe in Josephus and the broader documentary record denotes a trained clerical-legal function at multiple levels of government rather than a sectarian identity." }
'blasphemy-forgiveness-prerogative' = @{ title="Forgiveness as God's exclusive prerogative"; body="Isaiah 43:25 (Hebrew Bible, primary text) as the scriptural anchor for the Second Temple Jewish expectation that forgiveness of sin is God's exclusive prerogative; no surviving Second Temple-era text depicts a human intermediary independently declaring sins forgiven outright, distinct from mediating sacrificial atonement $em a documented absence supporting the historical plausibility of the scribal objection in Luke 5:21 as reflecting a genuine period sensitivity rather than later Christian dramatization." }
'tax-collectors-collaboration-not-greed' = @{ title='Collaborator, not just crook'; body="John R. Donahue, 'Tax Collectors and Sinners: An Attempt at Identification,' The Catholic Biblical Quarterly 33 (1971): 39-61. Argues that the Gospels' linked category 'tax collectors and sinners' reflects primarily a social-political judgment $em Jewish toll collectors were viewed as collaborators extracting money from their own people on behalf of Roman or Herodian power $em rather than simple moral condemnation of financial dishonesty, though the tax-farming system's structural incentive toward overcharging compounded the resentment." }
'capernaum-toll-border' = @{ title="Capernaum's internal Herodian border"; body="Jonathan L. Reed, Archaeology and the Galilean Jesus: A Re-Examination of the Evidence (Harrisburg, PA: Trinity Press International, 2000), discussion of Capernaum's regional economic geography. Locates Capernaum near the boundary between Herod Antipas's tetrarchy and the tetrarchy of his brother Philip, situated on a trade route toward Damascus, explaining why a customs/toll station generating internal Herodian border revenue would be sited there." }
'levi-matthew-identification' = @{ title='Is Levi really Matthew?'; body="Richard Bauckham, Jesus and the Eyewitnesses: The Gospels as Eyewitness Testimony (Grand Rapids: Eerdmans, 2006), chapter 5 ('The Twelve'). Challenges the traditional harmonization that Levi (Mark 2:14, Luke 5:27) and the apostle Matthew are the same double-named person, arguing Mark gives no signal the two names refer to one figure and that Matthew's Gospel may have reassigned the calling narrative to the apostle Matthew to strengthen its own claim to eyewitness authorship." }
'pharisees-fasting-days' = @{ title='An independent source confirms the fasting schedule'; body="Didache 8:1, in The Apostolic Fathers, vol. 1, trans. Kirsopp Lake, Loeb Classical Library (Cambridge, MA: Harvard University Press, 1912; widely reprinted). An early Christian community manual instructing readers not to fast 'with the hypocrites,' who fast on the second and fifth days of the week (Monday and Thursday) $em independently corroborating, from outside the Gospels, that this specific twice-weekly fasting schedule was a recognized and named Pharisaic practice in the period." }
'wineskin-material-science' = @{ title='Why an old skin bursts'; body="General principle of alcoholic fermentation chemistry applied to cured-hide vessel construction, as surveyed in Patrick E. McGovern, Ancient Wine: The Search for the Origins of Viniculture, rev. ed. (Princeton, NJ: Princeton University Press, 2019), survey of ancient storage- and transport-vessel technology. Explains that a previously used, already-fully-stretched skin has lost the elasticity to absorb further fermentation gas pressure and will split under a new batch of actively fermenting wine, while an unused skin retains the flexibility to expand safely." }
}

# ---- Chapter beats ----
$beat1 = @"
Luke sets the scene with unusual specificity: Jesus stands in Simon's boat and teaches the crowd from just off the beach at "the lake of Gennesaret" (5:1) $em Luke's own preferred name for what Mark and Matthew call the Sea of Galilee, and what today is Lake Kinneret. After the teaching, Jesus tells Simon to push into deep water and let down the nets; Simon protests that they'd fished all night and caught nothing, but complies, and the catch is so large the nets begin to tear (5:1-6). He signals to his partners in the other boat $em named a few verses later as James and John, sons of Zebedee (5:10) $em and both boats nearly sink under the weight of fish. Simon falls at Jesus' knees; Jesus tells him not to be afraid, "from now on you will catch men" (5:10); all three leave everything to follow him (5:11).

What can be checked here isn't the miracle itself but the world it's set in, and that world holds up in useful detail. First, the boat. In January 1986, during a severe drought that dropped the lake's water level, two fishermen brothers from Kibbutz Ginosar spotted the outline of a hull in the exposed mud. The vessel $em now called the Kinneret Boat or "Jesus Boat" $em was excavated, conserved, and radiocarbon- and pottery-dated to a range of roughly 100 BCE to 70 CE: squarely the period in question [[NOTE:kinneret-boat-dating]]. It's a working boat, not a status object $em about 8.2 meters long, built with recycled wood and a mix of mortise-and-tenon joinery and simple pegging, consistent with a small-scale fishing operation patching together what it could afford rather than commissioning something new.

Second, the business itself. Fishing on the lake wasn't an open commons. Herod Antipas, the tetrarch who governed Galilee under Rome, held what amounted to a monopoly over the exploitation of the lake's resources, and fishing rights were leased and taxed $em fishermen needed licenses, and tax brokers who'd bought the right to collect at auction stood between the water and the men working it. The existence of an actual customs house at Capernaum, on this same shoreline, is physical testimony to how far down into ordinary village life this taxation reached [[NOTE:galilee-fishing-tax-farming]]. Read against that backdrop, Simon's all-night failed shift reads less like bad luck in a vacuum and more like the ordinary risk of a taxed, licensed trade $em and the size of the catch that follows is dramatic precisely because it's set against a real economic floor, not a fairy-tale one.

Third, the partnership. Luke's word for what James and John are to Simon is koinonoi $em "partners" (5:10), a term with real commercial weight in the period, denoting a working association that shared risk, equipment, and profit rather than a loose friendship [[NOTE:koinonoi-fishing-partnership]]. And the net itself: Luke's Greek word is diktyon, a general term that in fishing contexts denotes the seine $em a large net paid out from a boat and hauled back in a wide arc $em as distinct from the amphiblestron, the smaller circular casting net that Mark and Matthew use in their parallel calling scenes [[NOTE:luke-net-terminology]]. Luke's word choice, in other words, fits the scale of the story he's telling.

What's Attested here, then, is the material and economic scaffolding: a first-century Galilean lake fishing economy under tax-farming, boats of exactly this type and date, and a real commercial-partnership structure for men working a boat together. What's Assumed, resting on the text alone, is that this particular catch happened, that these particular men were the ones in the boat, and that the encounter unfolded on this particular day. No outside source names Simon, James, or John independently of the Gospels; their historicity as recruits of a Galilean teacher rests on the internal consistency of the Christian textual tradition itself, corroborated only by the plausibility of the setting, not by an external record of the individuals.
"@

$beat2 = @"
A man "full of leprosy" begs Jesus for healing; Jesus touches him, he's cleansed, and Jesus sends him to show himself to the priest and offer for his cleansing "as Moses commanded" $em an explicit reference to the ritual in Leviticus 14 (5:12-14). Word spreads and crowds gather; Jesus withdraws to pray (5:15-16).

The single most important thing to know here is that "leprosy" in this text is almost certainly not Hansen's disease $em the bacterial illness modern medicine calls leprosy. The underlying terms, Hebrew tzaraat and Greek lepra, functioned as a broad diagnostic catch-all for a wide range of visible skin conditions $em most of which a modern dermatologist would sort into categories like psoriasis, vitiligo, favus, or fungal infection. There's a genuine scholarly consensus on this point, laid out most influentially in a 1993 paper arguing the Hansen's-disease reading was a later retrofit, one that hardened once the Septuagint's Greek translators rendered tzaraat as lepra $em a Greek medical term that in classical usage meant something closer to psoriasis $em and the two conditions were conflated centuries later as actual Hansen's disease spread into the Mediterranean world [[NOTE:tzaraat-not-hansens]]. This is a genuine "wait, actually": the text is not describing what most modern readers picture when they hear the word.

What Leviticus 13-14 does describe in real, checkable detail is a purity-exclusion bureaucracy $em a priest examines the skin, declares a person clean or unclean, and prescribes isolation, re-examination, and eventually a specific sacrificial re-entry ritual. This is corroborated as ongoing practice, and even elaborated, by two independent bodies of evidence. The first is the Mishnah's tractate Negaim, a detailed fourteen-chapter legal expansion of exactly this diagnostic and exclusion procedure, codified around 200 CE $em later than the Gospels, but demonstrating that the priestly-examination model was still a living, elaborated legal institution within rabbinic Judaism [[NOTE:mishnah-negaim-date]]. The second, older and more striking, is the Dead Sea Scrolls community's own Damascus Document, which assigns "the sons of Aaron" the specific priestly duty of examining and separating those with skin disease, using diagnostic language recognizably drawn from Leviticus $em manuscript copies from Qumran Cave 4 that predate the Gospels themselves [[NOTE:qumran-skin-disease-purity]]. Between the Qumran sect before Jesus and the rabbis two centuries after him, the purity-exclusion system Luke describes sits inside an attested, continuous practice, not an invention.
"@

$beat3 = @"
Jesus is teaching indoors, surrounded by Pharisees and "teachers of the law" (scribes), when men carrying a paralyzed friend on a mat can't get through the crowd $em so they go up, remove roofing material, and lower him down in front of Jesus (5:17-19). Jesus says, "Friend, your sins are forgiven you," which the scribes and Pharisees privately object to as blasphemy $em "Who can forgive sins but God alone?" $em and Jesus responds by healing the man outright, framing the visible healing as proof of his authority to make the invisible claim (5:20-26).

The roof detail is the kind of thing that sounds implausible until you know how Galilean houses were actually built. Excavations at Capernaum by Franciscan archaeologists Virgilio Corbo and Stanislao Loffreda, running from 1968 through the mid-1980s, uncovered exactly the kind of domestic architecture this scene requires: rough, undressed basalt-block walls, topped by roofs made of horizontal beams spanned with thinner slabs, packed and sealed with branches, matted thatch, and mud, then rolled to compact and waterproof the surface. Access to the roof was typically by an exterior stone stairway from the courtyard [[NOTE:capernaum-house-construction]]. A roof like that isn't a fixed structural shell; it's a maintainable surface that a determined person could open up and, afterward, patch.

The scribes and Pharisees in the room aren't literary inventions either $em both are real, independently attested social categories of the period, though their scope is genuinely debated. Josephus, writing as a contemporary, treats the Pharisees as one of several identifiable "schools of thought" within Second Temple Judaism, though he mentions them only about twenty times; modern scholars increasingly caution that the Pharisees may have been a smaller, less universally influential group than later Christian and rabbinic sources imply [[NOTE:scribes-pharisees-historical]]. Scribes, meanwhile, aren't listed by Josephus as a distinct sect at all $em in his account, "scribe" describes a trained clerical-legal function that existed at every level of government and temple bureaucracy, a job description more than a party affiliation.

The theological friction is also real and not a strawman. The expectation that only God forgives sins runs straight back through Second Temple Jewish thought to passages like Isaiah 43:25, and no surviving Second Temple text has a priest, prophet, or human intermediary simply declaring someone's sins forgiven outright the way Jesus does here [[NOTE:blasphemy-forgiveness-prerogative]].
"@

$beat4 = @"
Jesus sees Levi sitting at a tax booth, calls him, and Levi leaves everything to follow; Levi throws a feast attended by "tax collectors and sinners," and Pharisees and their scribes object to the company Jesus keeps, prompting the "physician for the sick, not the healthy" reply (5:27-32).

Tax collectors in this setting were not simply unpopular the way modern tax auditors are unpopular. The system was Roman-derived tax farming: Rome, and in Galilee's case Herod Antipas as the client ruler, sold or leased the right to collect specific tolls and customs to local operators, who then had every financial incentive to collect more than required and pocket the difference. But the deeper source of contempt was collaboration, not greed: a Jewish tax collector working a Roman-derived toll station was, functionally, a local instrument of the occupying power, extracting money from his own countrymen on behalf of a foreign-backed administration [[NOTE:tax-collectors-collaboration-not-greed]]. Regular handling of Gentile coinage and goods layered a ritual-impurity problem on top of the political one. "Collaborator" is closer to the first-century charge than "crook."

Capernaum's location makes the specific toll booth Levi sits at especially pointed. The town sat near the boundary between Herod Antipas's territory and the territory of his brother Philip the tetrarch, straddling a trade route running toward Damascus $em meaning goods crossing between the two Herodian jurisdictions, not just international trade, would generate a customs stop right there [[NOTE:capernaum-toll-border]]. A toll booth at Capernaum wasn't a minor backwater post; it sat at an internal Herodian border crossing on a real trade corridor.

One open, genuinely unresolved question belongs here rather than being smoothed over: whether "Levi" is simply another name for the apostle Matthew, as later Christian tradition assumes. The traditional harmonization treats this as ordinary first-century double-naming, on the model of Simon/Peter or Saul/Paul. But that identification has been directly challenged in recent scholarship $em Richard Bauckham argues that Mark's Gospel never actually signals that its "Levi" and its "Matthew" are the same person, and that the author of Matthew's Gospel may have reassigned Mark's calling story to the apostle Matthew specifically to authenticate his own Gospel's connection to an eyewitness [[NOTE:levi-matthew-identification]]. This is a live, named scholarly disagreement, not a settled harmonization.
"@

$beat5 = @"
Some point out that John's disciples and the Pharisees' disciples fast often, while Jesus' disciples eat and drink; Jesus answers that wedding guests don't fast while the bridegroom is present, then gives two short image-sayings $em new cloth patched onto old garments, and new wine poured into old wineskins, both ending in ruin (5:33-39).

The fasting practices named here are real and independently attested, not an invented contrast for the sake of the story. The Pharisees' habit of fasting twice weekly $em Monday and Thursday $em goes beyond anything the Torah itself requires, and it's corroborated from outside the Gospels too: the early Christian manual known as the Didache, likely composed around the turn of the first century, explicitly instructs its readers not to fast "with the hypocrites" and to fast on Wednesday and Friday instead, precisely so as to mark a visible difference from it [[NOTE:pharisees-fasting-days]]. That a separate, independent early Christian text bothers to define itself against this specific fasting schedule is good evidence the practice was a real, known social marker rather than a rhetorical Gospel invention.

The wineskin image checks out materially, too. Wineskins in this period were made from tanned and cured goat or sheep hide, sewn into a sealed bag shape and often pitch-coated on the inside for waterproofing. New wine $em meaning actively fermenting wine $em continues producing carbon dioxide as yeast consumes the grape sugars, and that gas needs somewhere to go; a fresh, still-flexible skin can stretch to absorb the pressure. A skin that has already been through one fermentation cycle has already stretched to its maximum and dried into that shape $em it has lost the elasticity to expand further, so a second batch of actively fermenting wine will simply split it [[NOTE:wineskin-material-science]]. This is straightforward material chemistry, not folk wisdom dressed up as a metaphor.
"@

$beats = @($beat1, $beat2, $beat3, $beat4, $beat5)

# ---- New glossary entries (unique to ch5) ----
$glossary = [ordered]@{
'JAMES (SON OF ZEBEDEE)' = "One of the two fishing partners of Simon named in this chapter (5:10), called alongside his brother John at the same lakeside episode. Distinguished in later Christian tradition from James the brother of Jesus and from James $em son of Alphaeus. The sons-of-Zebedee fishing operation Luke describes fits the documented pattern of family-based Galilean fishing partnerships that pooled boats, nets, and labor [[NOTE:koinonoi-fishing-partnership]]."
'JOHN (SON OF ZEBEDEE)' = "James's brother and fishing partner, called together with him in this chapter (5:10). Not to be confused with John the Baptist. Later Christian tradition identifies him with the author of a Gospel and Johannine epistles bearing his name, an identification this chapter's material does not itself address."
'ZEBEDEE' = "The father of James and John, named only in passing here as the man whose sons are Simon's fishing partners (5:10). The broader pattern of Galilean fishing households pooling boats, nets, and labor is independently documented [[NOTE:koinonoi-fishing-partnership]]."
'LEVI (TAX COLLECTOR)' = "The toll collector Jesus calls from his booth in this chapter (5:27-32), traditionally identified with the apostle Matthew under the theory that he held two names $em an identification recent scholarship has directly challenged rather than confirmed [[NOTE:levi-matthew-identification]]. His profession situates him within the real, historically attested Herodian tax-farming system operating at Capernaum, a town positioned on an internal border between two tetrarchies [[NOTE:capernaum-toll-border]]."
'THE PHARISEES' = "A pietist Jewish movement of the late Second Temple period, first appearing by name in this book's narrative in this chapter as objectors to Jesus' forgiveness claim (5:21) and his choice of dinner company (5:30). Independently attested by Josephus as one of several identifiable schools of thought, though modern scholarship cautions against assuming they were as large or uniformly influential as later tradition suggests [[NOTE:scribes-pharisees-historical]]. Known independently for practices including twice-weekly fasting, corroborated outside the Gospels by the early Christian Didache [[NOTE:pharisees-fasting-days]]."
'THE SCRIBES' = "A trained clerical-legal professional class appearing in this chapter (5:21, 5:30) paired narratively with the Pharisees, though the two were not necessarily the same people. Josephus does not treat scribes as a distinct sect; the term denotes a documented governmental and legal-clerical function operating at multiple levels of Second Temple Jewish administration [[NOTE:scribes-pharisees-historical]]."
'LAKE OF GENNESARET (SEA OF GALILEE / LAKE KINNERET)' = "The freshwater lake in Galilee where this chapter's opening fishing episode is set (5:1), called by Luke 'the lake of Gennesaret' and elsewhere in the Gospels the Sea of Galilee $em modern Lake Kinneret. The lake's first-century fishing economy is independently documented as taxed and licensed under Herod Antipas [[NOTE:galilee-fishing-tax-farming]], and a genuine first-century fishing vessel $em the Kinneret Boat $em was recovered from its bed in 1986 [[NOTE:kinneret-boat-dating]]."
'TZARAAT / LEPRA (LEPROSY)' = "The Hebrew and Greek terms rendered 'leprosy' in English translations of both Leviticus 13-14 and Luke 5:12-16, denoting a broad category of visible skin conditions rather than the specific bacterial illness modern medicine calls leprosy (Hansen's disease) [[NOTE:tzaraat-not-hansens]]. The associated priestly purity-exclusion procedure is independently corroborated both earlier, at Qumran [[NOTE:qumran-skin-disease-purity]], and later, in the Mishnah [[NOTE:mishnah-negaim-date]]."
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
    Add-BeatNode $Ch5NodeId $id $sortKey
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

# ---- Append new claims to existing ch4 glossary beats ----
$capId = Find-GlossaryBeatId "CAPERNAUM"
if ($capId) {
    $extra = "This chapter adds: excavation revealed basalt-block domestic architecture consistent with the roof-access detail in 5:19 [[NOTE:capernaum-house-construction]], and a customs house consistent with the town's position on an internal Herodian tetrarchy border, astride a trade route toward Damascus [[NOTE:capernaum-toll-border]]."
    foreach ($slug in $slugToNumber.Keys) { $extra = $extra.Replace("[[NOTE:$slug]]", "[$($slugToNumber[$slug])]") }
    Append-ToExistingBeat $capId $extra
    Write-Host "Appended to CAPERNAUM glossary entry"
}
$simonId = Find-GlossaryBeatId "SIMON (PETER)"
if ($simonId) {
    $extra = "This chapter adds: called by Jesus from a working fishing partnership with James and John, sons of Zebedee, on the lake of Gennesaret (5:1-11); Luke's description of him as a working fisherman under a taxed, licensed lake economy is well attested by the material and economic record even though no source outside the Christian tradition independently names him [[NOTE:galilee-fishing-tax-farming]]."
    foreach ($slug in $slugToNumber.Keys) { $extra = $extra.Replace("[[NOTE:$slug]]", "[$($slugToNumber[$slug])]") }
    Append-ToExistingBeat $simonId $extra
    Write-Host "Appended to SIMON (PETER) glossary entry"
}

# ---- Seed new entities ----
Seed-Entity "Kinneret Boat (`"Jesus Boat`")" "kinneret-boat-jesus-boat" "transportation" "First-century fishing vessel recovered from the Sea of Galilee lakebed in 1986, radiocarbon- and pottery-dated to c. 100 BCE-70 CE."

$conn.Close()
Write-Host "DONE Chapter 5."
