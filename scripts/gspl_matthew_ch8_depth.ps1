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
$NotesNodeId = [guid]"019FA01D-FA22-76C6-976C-3EA4F4D54A14"
$GlossaryNodeId = [guid]"019FA026-3758-7A57-AF65-EC1DB4303EF3"
$Ch8NodeId = [guid]"019FA066-CCC6-746B-A184-E781F3461446"

$maxNoteSortKey = [double](Exec-Scalar "SELECT ISNULL(MAX(bn.SortKey),0) FROM BeatNodes bn WHERE bn.NodeId='019FA01D-FA22-76C6-976C-3EA4F4D54A14' AND bn.IsEnabled=1")
$maxGlossarySortKey = [double](Exec-Scalar "SELECT ISNULL(MAX(bn.SortKey),0) FROM BeatNodes bn WHERE bn.NodeId='019FA026-3758-7A57-AF65-EC1DB4303EF3' AND bn.IsEnabled=1")
$maxChapterSortKey = [double](Exec-Scalar "SELECT ISNULL(MAX(bn.SortKey),0) FROM BeatNodes bn WHERE bn.NodeId='019FA066-CCC6-746B-A184-E781F3461446' AND bn.IsEnabled=1")
$maxNoteNumber = [int](Exec-Scalar "SELECT ISNULL(MAX(CAST(LEFT(b.Text, CHARINDEX(' ',b.Text)-1) AS INT)),0) FROM BeatNodes bn JOIN Beats b ON bn.BeatId=b.Id WHERE bn.NodeId='019FA01D-FA22-76C6-976C-3EA4F4D54A14' AND bn.IsEnabled=1 AND CHARINDEX(' ', b.Text) > 1 AND LEFT(b.Text, CHARINDEX(' ',b.Text)-1) NOT LIKE '%[^0-9]%'")

Write-Host "Starting: MaxNumber=$script:MaxNumber MaxNoteSortKey=$maxNoteSortKey MaxNoteNumber=$maxNoteNumber MaxGlossarySortKey=$maxGlossarySortKey MaxChapterSortKey=$maxChapterSortKey"

# ---- Notes (slug -> title/body) in order ----
$notes = [ordered]@{
'tzaraat-lepra-broader-category' = @{ title='Tzaraat and lepra: a category, not a diagnosis'; body="E. V. Hulse, 'The Nature of Biblical Leprosy and the Use of Alternative Medical Terms in Modern Translations of the Bible,' Palestine Exploration Quarterly 107, no. 2 (1975): 87-105. Hulse's widely cited study argues that the Hebrew tzaraat (translated lepra in the Septuagint and Greek New Testament, then 'leprosy' in English) named a broad category of visible skin, fabric, and even house-wall conditions rather than one specific disease, and that true Hansen's disease (leprosy in the modern clinical sense, caused by Mycobacterium leprae) is almost certainly not what most biblical tzaraat cases describe." }
'leviticus-priestly-diagnostic-procedure' = @{ title="The priest as diagnostician: Leviticus 13-14's own criteria"; body="Jacob Milgrom, Leviticus 1-16: A New Translation with Introduction and Commentary, Anchor Bible vol. 3 (New York: Doubleday, 1991), commentary ad loc. Leviticus 13:1-46, 14:1-32. Milgrom's commentary details the elaborate priestly diagnostic protocol Leviticus 13-14 lays out — color, depth, spreading pattern, and a mandatory seven-day (twice-repeated) waiting period before a priest certifies a case tzaraat or clean — a system built to sort a range of skin, hair, and fabric or wall conditions into a single ritual-purity category, not to diagnose a specific pathogen the way a modern physician would." }
'herod-antipas-independent-forces' = @{ title="Antipas's own army, not a Roman legion"; body="Morten Hørning Jensen, Herod Antipas in Galilee: The Literary and Archaeological Sources on the Reign of Herod Antipas and Its Socio-Economic Impact on Galilee, 2nd rev. ed., Wissenschaftliche Untersuchungen zum Neuen Testament 2/215 (Tübingen: Mohr Siebeck, 2010). Jensen's study of Antipas's reign documents that Galilee remained under Antipas's own client-tetrarch administration, with his own locally raised forces, until his exile in 39 CE; direct Roman provincial rule of Galilee itself did not begin until 44 CE, after Herod Agrippa I's death, meaning a literal Roman legionary centurion stationed in Capernaum this early is a geographic and administrative stretch rather than the straightforward reading English translations invite." }
'josephus-antipas-aretas-war' = @{ title="Josephus's account of Antipas fielding his own generals"; body="Flavius Josephus, Jewish Antiquities, Book 18, sections 109-115 (Loeb Classical Library, trans. Louis H. Feldman, Cambridge, MA: Harvard University Press). Josephus records that when Antipas went to war with the Nabataean king Aretas IV over a border dispute in Perea, both sides sent out their own generals to fight rather than mobilizing Roman legions — direct confirmation that Antipas maintained and deployed a standing force answerable to himself, organized and titled along Roman lines but administratively separate from the Roman army proper." }
'centurion-title-not-exclusively-roman' = @{ title="'Centurion' names a rank, not automatically an army"; body="Adrian Goldsworthy, The Roman Army at War, 100 BC-AD 200, Oxford Classical Monographs (Oxford: Clarendon Press, 1996), chapter on unit organization and command. Goldsworthy's study of Roman military structure describes the centurionate as a company-command rank (roughly one hundred men) that client rulers across the eastern Mediterranean routinely copied wholesale into their own forces, titles included; a Greek hekatontarches ('commander of a hundred') serving Herod Antipas would have carried the same title and rank insignia as a Roman legionary centurion without being one." }
'kloppenborg-q-parallel-centurion' = @{ title="A Q-tradition story, with real differences between Matthew and Luke"; body="John S. Kloppenborg, Q Parallels: Synopsis, Critical Notes, and Concordance (Sonoma, CA: Polebridge Press, 1988), section on Q 7:1-10. Kloppenborg's synoptic reconstruction places the centurion's servant story in the double tradition (material shared by Matthew and Luke but absent from Mark), noting that Matthew has the centurion approach Jesus directly while Luke's parallel version (Luke 7:1-10) has him send Jewish elders and then friends as intermediaries — a real compositional difference between the two Gospels' versions of what is otherwise the same core story, not merely two independent eyewitness reports of the same conversation." }
'meier-gentile-inclusion-theme' = @{ title="'Many will come from east and west': a Gentile-inclusion saying with its own history"; body="John P. Meier, A Marginal Jew: Rethinking the Historical Jesus, Volume Two: Mentor, Message, and Miracles, Anchor Bible Reference Library (New York: Doubleday, 1994), part on Jesus's miracles and their sayings-context. Meier's treatment situates Matthew 8:11-12's saying about 'many' coming 'from east and west' to feast with the patriarchs, while 'the sons of the kingdom' are cast out, within a wider set of Gentile-inclusion sayings attributed to Jesus across independent source strands; the saying's placement here, directly after a Gentile centurion's professed faith, is Matthew's own editorial pairing rather than something the underlying tradition required." }
'beaton-isaiah-healing-not-atonement' = @{ title="A healing citation, not yet an atonement citation"; body="Richard Beaton, Isaiah's Christ in Matthew's Gospel, Society for New Testament Studies Monograph Series 123 (Cambridge: Cambridge University Press, 2002). Beaton's study of Matthew's formula quotations argues that Matthew 8:17's citation of Isaiah 53:4 ('he took our illnesses and bore our diseases') is applied strictly to the physical healings just narrated — fevers, demonic affliction, skin disease — with no reference to vicarious suffering or atonement for sin, a reading substantially narrower than the vicarious-atonement sense the same Suffering Servant passage (especially Isaiah 53:5, 'by his wounds we are healed') later acquires in Christian theology, most famously in 1 Peter 2:24." }
'davies-allison-8-17-commentary' = @{ title="An editorial choice, not an incidental proof-text"; body="W. D. Davies and Dale C. Allison Jr., Matthew 8-18, International Critical Commentary vol. 2 (Edinburgh: T&T Clark, 1991), commentary ad loc. Matthew 8:16-17. Davies and Allison read Matthew's placement of the Isaiah 53:4 citation directly after the healing summary (8:16) as a deliberate editorial framing device unique to Matthew among the Synoptics — Mark and Luke's parallel healing summaries (Mark 1:32-34, Luke 4:40-41) carry no such citation — consistent with Matthew's broader pattern of appending formula quotations to underline that Jesus's actions fulfill specific, named scripture." }
'metzger-gadarenes-textual-support' = @{ title="What the earliest manuscripts of Matthew actually read"; body="Bruce M. Metzger, A Textual Commentary on the Greek New Testament, 2nd ed. (Stuttgart: Deutsche Bibelgesellschaft / United Bible Societies, 1994; repr. Peabody, MA: Hendrickson, 2005), textual notes ad loc. Matthew 8:28. Metzger's textual-critical analysis, prepared for the United Bible Societies' Greek New Testament committee, concludes that 'Gadarenes' has the best early manuscript support in Matthew (against 'Gerasenes,' which has the best support in the parallel accounts at Mark 5:1 and Luke 8:26), while 'Gergesenes' — the reading Origen preferred on geographic grounds — is judged a later, geographically motivated scribal correction rather than the earliest recoverable text in any of the three Gospels." }
'kjv-textus-receptus-gergesenes' = @{ title="Why an older English Bible says something a newer one doesn't"; body="David C. Parker, An Introduction to the New Testament Manuscripts and Their Texts (Cambridge: Cambridge University Press, 2008), chapters on the history of the printed Greek text and the Textus Receptus. Parker traces how the King James Version's Greek base text (the Byzantine-era Textus Receptus, fixed in print by Erasmus and his successors in the sixteenth century) drew on later manuscripts than the earliest papyri and great uncials modern critical editions (Nestle-Aland/UBS) prioritize — which is why the King James Version reads 'Gergesenes' at Matthew 8:28 while modern translations built on the critical text read 'Gadarenes': two different manuscript traditions, not a translation error on either side." }
'freyne-decapolis-gentile-population' = @{ title="The Decapolis: Greek cities in Jewish territory"; body="Sean Freyne, Galilee, Jesus, and the Gospels: Literary Approaches and Historical Investigations (Philadelphia: Fortress Press, 1988), chapter on Galilee's Hellenistic and Gentile neighbors. Freyne documents that the Decapolis cities east and southeast of the Sea of Galilee — of which Gadara and Gerasa were both members — were self-governing Greek poleis with substantially Gentile populations, in contrast to the predominantly Jewish towns ringing the lake itself; a story set in or near this territory involving a herd of swine is consistent with that regional population mix rather than requiring any narrative special pleading." }
'plutarch-jews-abstain-pork' = @{ title="A pagan writer already noticed the same taboo"; body="Plutarch, Table Talk (Quaestiones Convivales), Book 4, Question 5 (Moralia 669E-671B), in Plutarch's Moralia, vol. 8, Loeb Classical Library, trans. Paul A. Clement and Herbert B. Hoffleit (Cambridge, MA: Harvard University Press, 1969). In this dinner-table dialogue Plutarch's characters debate why Jews abstain from pork, treating it as a well-known, distinctive Jewish practice recognizable to a general Greco-Roman readership decades after the Gospels were written — independent, non-Jewish, non-Christian testimony that the pork taboo functioned exactly as the kind of visible cultural marker this chapter's pig herd implies." }
'milgrom-pork-prohibition-social-marker' = @{ title="Why pork specifically was Israel's boundary marker"; body="Jacob Milgrom, Leviticus 1-16: A New Translation with Introduction and Commentary, Anchor Bible vol. 3 (New York: Doubleday, 1991), commentary ad loc. Leviticus 11:7-8. Milgrom's commentary on the pig prohibition notes that ancient Israelite and later Jewish practice treated abstention from pork as one of the most socially visible boundary markers of Jewish identity in the Greco-Roman world, which is exactly why a herd of pigs being pastured openly nearby signals a non-Jewish, or religiously mixed, population to an ancient audience without the text needing to say so directly." }
'hesse-wapnish-pig-ethnic-marker-debate' = @{ title="Pig bones as an ethnic signal: a live archaeological argument"; body="Brian Hesse and Paula Wapnish, 'Can Pig Remains Be Used for Ethnic Diagnosis in the Ancient Near East?' in The Archaeology of Israel: Constructing the Past, Interpreting the Present, ed. Neil Asher Silberman and David B. Small, Journal for the Study of the Old Testament Supplement Series 237 (Sheffield: Sheffield Academic Press, 1997), 238-270. Hesse and Wapnish's influential zooarchaeological study cautions against a simple rule that pig-bone presence equals Gentile occupation and absence equals Jewish occupation, arguing that pig husbandry varied with ecology, economy, and period as well as ethnicity or religious law; the pig herd in this story is consistent with, but not proof by itself of, a specifically Gentile setting, since Leviticus 11:7 forbids pork to Jews without making pig-raising itself a reliable archaeological fingerprint everywhere it appears." }
}

# ---- Chapter beats (with [[NOTE:slug]] placeholders) ----
$beat1 = @'
The leprosy healing that opens this chapter (8:1-4) is worth a second, closer pass on vocabulary alone, because "leprosy" is doing more translation work than the English word suggests. The Hebrew tzaraat, rendered lepra in the Septuagint and carried into the Greek New Testament as the same word, was never a single diagnosis the way modern medicine uses the term. It named a category — a family of visible skin conditions (and, per Leviticus 13:47-59 and 14:33-53, even fabric and house-wall discolorations) that a priest, not a physician, was trained to recognize and rule on [[NOTE:tzaraat-lepra-broader-category]]. True Hansen's disease, the single specific bacterial illness modern readers picture when they hear "leprosy," is almost certainly not what most tzaraat cases in the Hebrew Bible describe.

Leviticus 13-14's actual diagnostic protocol bears this out. It is a genuinely elaborate system — color, depth beneath the skin's surface, whether hair within the affected patch has turned white, whether the condition is spreading, a mandatory seven-day quarantine repeated as many as twice before a ruling is made — built to sort a range of conditions into a single ritual-purity category rather than to identify a pathogen [[NOTE:leviticus-priestly-diagnostic-procedure]]. That is exactly the process Jesus invokes when he tells the healed man to "go, show yourself to the priest and offer the gift that Moses commanded" (8:4): not incidental local color, but the real next procedural step the law of the period required before the man could rejoin the community. Whatever the underlying condition actually was, the instruction that follows it is legally precise, consistent with a genuinely Jewish setting rather than a later community inventing ritual detail from outside that world.
'@

$beat2 = @'
The centurion of 8:5-13 is usually read as a straightforwardly Roman figure, and that assumption deserves a second look. Capernaum in the 20s-30s CE sat inside Galilee, which at this date was not a Roman province at all but the client tetrarchy of Herod Antipas — direct Roman rule of Galilee itself did not begin until 44 CE, after Herod Agrippa I's death [[NOTE:herod-antipas-independent-forces]]. Antipas was not without an army of his own: Josephus records that when Antipas went to war with the Nabataean king Aretas IV over a border dispute, both rulers sent out their own generals to fight, evidence that Antipas fielded and commanded a standing force answerable to him rather than to Rome [[NOTE:josephus-antipas-aretas-war]].

That does not make the centurion a fiction — it makes him, more likely, an officer in Antipas's own Galilean forces, organized and ranked along Roman lines (as client-king armies across the eastern Mediterranean routinely were) rather than a literal legionary posted to a Roman garrison. The rank title itself was portable: "centurion" named a command over roughly a hundred men, a structure client rulers borrowed wholesale, titles included, without thereby becoming part of the Roman army proper [[NOTE:centurion-title-not-exclusively-roman]]. None of this touches whether the healing happened. It only means the man's uniform, so to speak, was almost certainly Herodian rather than Roman.

The story itself is shared with Luke (7:1-10) as part of the double tradition — material both Gospels draw on independently of Mark — and the two versions differ in a real, checkable way: Matthew has the centurion approach Jesus in person, while Luke has him send Jewish elders first and then friends, never appearing face to face at all [[NOTE:kloppenborg-q-parallel-centurion]]. Matthew's version closes on the saying that "many will come from east and west" to feast with Abraham, Isaac, and Jacob while "the sons of the kingdom" are shut out (8:11-12) — a Gentile-inclusion saying that belongs to a wider set of similar sayings attributed to Jesus across independent strands of tradition, and whose placement directly after a Gentile officer's own professed faith is Matthew's editorial pairing rather than a detail the underlying story required [[NOTE:meier-gentile-inclusion-theme]].
'@

$beat3 = @'
Matthew's citation of Isaiah 53:4 at 8:17 — "he took our illnesses and bore our diseases" — deserves its own look because of how differently it gets used here compared to its more famous later career. In later Christian theology, the Suffering Servant passage of Isaiah 52:13-53:12 is read overwhelmingly through its next verse, Isaiah 53:5 ("by his wounds we are healed"), applied to Christ's atoning death for sin — most explicitly in 1 Peter 2:24, which quotes that exact verse in that exact sense. Matthew's use of the neighboring verse here is doing something narrower and more literal: applied strictly to the physical healings and exorcisms just narrated in this chapter, with no reference to vicarious suffering or atonement for sin at all [[NOTE:beaton-isaiah-healing-not-atonement]].

The placement is itself an editorial signature. Mark and Luke both include their own healing-summary scenes at this point in the narrative (Mark 1:32-34, Luke 4:40-41), and neither attaches a scriptural citation to it; only Matthew appends the Isaiah 53:4 quotation, consistent with his broader habit of pinning specific "formula quotations" onto episodes to argue that Jesus's actions fulfill named scripture [[NOTE:davies-allison-8-17-commentary]]. Reading Matthew 8:17 in isolation, without importing the atonement reading Isaiah 53 acquires elsewhere in the New Testament, recovers what looks like the citation's original, narrower purpose in this specific scene.
'@

$beat4 = @'
The place name at the start of the pig-herd story (8:28) is its own small crux, worth a closer pass than the general geography question already on record for this chapter. The earliest and best manuscripts of Matthew read "Gadarenes"; the earliest and best manuscripts of Mark's and Luke's parallel versions of the same story read "Gerasenes" instead — not a spelling variant, but two different, independently attested ancient cities, and modern critical editions of the Greek New Testament weigh the manuscript evidence for each Gospel separately and land on different place names for the same event [[NOTE:metzger-gadarenes-textual-support]].

That split has a visible afterlife in English Bibles today. The King James Version reads "Gergesenes" at Matthew 8:28 — the third variant, the one closest to the lakeshore and the one Origen already favored on geographic grounds — because the King James translators worked from the Textus Receptus, a Greek base text fixed in print in the sixteenth century from later Byzantine-era manuscripts. Modern translations built on the earlier papyri and great uncial manuscripts that critical editions like Nestle-Aland/UBS now prioritize read "Gadarenes" instead [[NOTE:kjv-textus-receptus-gergesenes]]. Neither reading is a translation error; they are two different, honestly reconstructed Greek texts, disagreeing about which words the earliest recoverable manuscript tradition actually preserved.
'@

$beat5 = @'
One demographic detail in the pig-herd scene (8:30-32) is worth pulling out on its own: a herd of swine large enough for the story to need ("a great herd of swine, feeding") is itself a piece of information about who lived in this stretch of territory. Leviticus 11:7-8 forbids pork to Jews outright, so pig husbandry at this scale signals a Gentile, or at least religiously mixed, population rather than a devout Jewish one. The Decapolis cities on the lake's eastern and southeastern side — Gadara and Gerasa both among them — were self-governing Greek poleis with substantially Gentile populations, distinct from the mostly Jewish towns ringing the lake's western and northern shore [[NOTE:freyne-decapolis-gentile-population]]. A pagan writer noticed the same practice independently: Plutarch, writing decades after the Gospels, has his dinner-table interlocutors debate at length why Jews abstain from pork, treating it as a well-known, recognizable feature of Jewish life to a general Greco-Roman readership [[NOTE:plutarch-jews-abstain-pork]] — and commentary on the same Levitical prohibition treats pork abstention as one of the most socially visible boundary markers separating Jewish from Gentile practice in the Greco-Roman world [[NOTE:milgrom-pork-prohibition-social-marker]], the same priestly holiness code that supplies this chapter's leprosy-certification procedure [[NOTE:leviticus-priestly-diagnostic-procedure]].

That said, the archaeological picture is more contested than "pig bones equal Gentiles" makes it sound. A now-standard zooarchaeological study cautions that pig-remains frequency in the ancient Near East tracked ecology and local economy as much as ethnicity or religious law, and shouldn't be read as an automatic ethnic fingerprint at every site where it appears [[NOTE:hesse-wapnish-pig-ethnic-marker-debate]]. The story's pig herd is consistent with, and expected in, a mixed or Gentile-leaning stretch of Decapolis territory — it just isn't, on its own, proof of exactly who lived there.
'@

$beats = @($beat1, $beat2, $beat3, $beat4, $beat5)

# ---- Glossary additions (heading -> body) ----
$glossary = [ordered]@{
"TZARAAT / LEPRA (ANCIENT SKIN-DISEASE CATEGORIES)" = "The Hebrew term applied to the skin condition healed in this chapter's opening scene (8:1-4), rendered lepra in the Septuagint and Greek New Testament and 'leprosy' in most English translations. Modern medical and historical scholarship distinguishes tzaraat/lepra as a broad priestly diagnostic category covering a range of skin, hair, and even fabric or wall conditions, from the single, specific modern disease Hansen's disease (caused by Mycobacterium leprae) [[NOTE:tzaraat-lepra-broader-category]]. Leviticus 13-14 lays out the priest's own detailed, non-medical certification procedure for the category [[NOTE:leviticus-priestly-diagnostic-procedure]]. See also REGION OF THE GADARENES for a comparable case where a single Gospel term maps imperfectly onto a modern category."
"CENTURION (ROMAN VS. HERODIAN MILITARY OFFICER)" = "A Greco-Roman military rank, roughly a company commander over about a hundred soldiers, held by the officer whose servant Jesus heals at a distance in Capernaum (8:5-13). English translations render the figure simply as a 'centurion,' inviting the assumption that he served in a literal Roman legion; but Galilee was governed by the client tetrarch Herod Antipas, not directly by Rome, until 44 CE, and Antipas is independently documented fielding his own locally raised forces organized and titled along Roman lines [[NOTE:herod-antipas-independent-forces]] [[NOTE:josephus-antipas-aretas-war]]. The rank title 'centurion' was routinely adopted wholesale by client-king armies across the region, so the office does not by itself prove Roman citizenship or a Roman chain of command [[NOTE:centurion-title-not-exclusively-roman]]."
"SUFFERING SERVANT (ISAIAH 53) — MATTHEW'S HEALING APPLICATION" = "The unnamed servant figure of Isaiah 52:13-53:12, quoted in this chapter at Matthew 8:17 ('he took our illnesses and bore our diseases,' from Isaiah 53:4) as the explanation for Jesus's healing activity just narrated. Matthew's application here is to physical healing alone, with no reference to vicarious suffering for sin [[NOTE:beaton-isaiah-healing-not-atonement]] — a narrower reading than the vicarious-atonement sense the same passage (especially Isaiah 53:5) takes on elsewhere in the New Testament, and the citation's placement is generally read as Matthew's own editorial framing rather than an inherited detail [[NOTE:davies-allison-8-17-commentary]]. See ISAIAH for the prophet and GREAT ISAIAH SCROLL (1QISA^A) for the manuscript tradition."
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
$sortKey = $maxChapterSortKey
foreach ($beatText in $beats) {
    $resolved = $beatText
    foreach ($slug in $slugToNumber.Keys) {
        $resolved = $resolved.Replace("[[NOTE:$slug]]", "[$($slugToNumber[$slug])]")
    }
    $sortKey += 100
    $id = New-BeatRow $resolved
    Add-BeatNode $Ch8NodeId $id $sortKey
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

# ---- Seed new entities (checked by name against gspl_entity_catalog.txt first; not present) ----
Seed-Entity "E. V. Hulse" "e-v-hulse" "character" "Medical historian; author of the landmark 1975 Palestine Exploration Quarterly study distinguishing biblical tzaraat/lepra from modern Hansen's disease."
Seed-Entity "Morten Hørning Jensen" "morten-horning-jensen" "character" "New Testament historian; author of Herod Antipas in Galilee (WUNT 2/215), documenting Antipas's own client-tetrarch forces prior to direct Roman rule of Galilee in 44 CE."
Seed-Entity "Richard Beaton" "richard-beaton" "character" "New Testament scholar; author of Isaiah's Christ in Matthew's Gospel (SNTSMS 123), on Matthew's formula quotations including Isaiah 53:4 at 8:17."
Seed-Entity "W. D. Davies" "w-d-davies" "character" "New Testament scholar; co-author with Dale C. Allison Jr. of the International Critical Commentary on Matthew."
Seed-Entity "David C. Parker" "david-c-parker" "character" "New Testament textual critic; author of An Introduction to the New Testament Manuscripts and Their Texts (Cambridge, 2008)."
Seed-Entity "Sean Freyne" "sean-freyne" "character" "Historian of Galilee; author of Galilee, Jesus, and the Gospels (Fortress Press, 1988), on the Decapolis's Gentile population."
Seed-Entity "Brian Hesse" "brian-hesse" "character" "Zooarchaeologist; co-author of the influential 1997 study on pig remains and ethnic diagnosis in the ancient Near East."
Seed-Entity "Paula Wapnish" "paula-wapnish" "character" "Zooarchaeologist; co-author with Brian Hesse of the influential 1997 study on pig remains and ethnic diagnosis in the ancient Near East."
Seed-Entity "John S. Kloppenborg" "john-s-kloppenborg" "character" "Q-source scholar; author of Q Parallels: Synopsis, Critical Notes, and Concordance (Polebridge Press, 1988)."

$conn.Close()
Write-Host "DONE Matthew Chapter 8 depth pass."
