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
$Ch20NodeId = [guid]"019FA070-1F63-7891-8ABA-40A617CF7273"

$maxNoteSortKey = [double](Exec-Scalar "SELECT ISNULL(MAX(bn.SortKey),0) FROM BeatNodes bn WHERE bn.NodeId='019FA01D-FA22-76C6-976C-3EA4F4D54A14' AND bn.IsEnabled=1")
$maxGlossarySortKey = [double](Exec-Scalar "SELECT ISNULL(MAX(bn.SortKey),0) FROM BeatNodes bn WHERE bn.NodeId='019FA026-3758-7A57-AF65-EC1DB4303EF3' AND bn.IsEnabled=1")
$maxChapterSortKey = [double](Exec-Scalar "SELECT ISNULL(MAX(bn.SortKey),0) FROM BeatNodes bn WHERE bn.NodeId='019FA070-1F63-7891-8ABA-40A617CF7273' AND bn.IsEnabled=1")
$maxNoteNumber = [int](Exec-Scalar "SELECT ISNULL(MAX(CAST(LEFT(b.Text, CHARINDEX(' ',b.Text)-1) AS INT)),0) FROM BeatNodes bn JOIN Beats b ON bn.BeatId=b.Id WHERE bn.NodeId='019FA01D-FA22-76C6-976C-3EA4F4D54A14' AND bn.IsEnabled=1 AND CHARINDEX(' ', b.Text) > 1 AND LEFT(b.Text, CHARINDEX(' ',b.Text)-1) NOT LIKE '%[^0-9]%'")

Write-Host "Starting: MaxNumber=$script:MaxNumber MaxNoteSortKey=$maxNoteSortKey MaxNoteNumber=$maxNoteNumber MaxGlossarySortKey=$maxGlossarySortKey MaxChapterSortKey=$maxChapterSortKey"

# ---- Notes (slug -> title/body) in order ----
$notes = [ordered]@{
'tacitus-denarius-standard-pay' = @{ title="Tacitus and the denarius as a standard day-wage benchmark"; body="Cornelius Tacitus, Annals, Book 1, chapter 17 (Loeb Classical Library, trans. John Jackson, Cambridge, MA: Harvard University Press, 1931). In the mutiny speech Tacitus records for the Pannonian legions in 14 CE, the soldiers cite a denarius a day as the benchmark figure for fair compensation, evidence that Roman-world audiences of this period recognized 'a denarius a day' as a stable, common reference point for ordinary daily pay rather than a figure invented for Matthew's parable. Tacitus's own context is military pay rather than farm labor specifically, so this evidence corroborates rather than directly duplicates the agricultural-wage data below." }
'duncan-jones-egypt-wage-papyri' = @{ title="Payroll papyri: what Roman Egypt actually paid"; body="Richard Duncan-Jones, The Economy of the Roman Empire: Quantitative Studies, 2nd ed. (Cambridge: Cambridge University Press, 1982), appendix tables on wages and prices in Roman Egypt. Duncan-Jones's compilation of dated wage receipts and payroll papyri from Roman Egypt across the first three centuries CE documents unskilled and casual agricultural laborers commonly earning in the range of one to a few silver drachmas (roughly comparable in silver content to a denarius) per day, giving the parable's flat one-denarius day rate a real, independently documented economic floor rather than an invented number chosen for narrative convenience." }
'davies-allison-denarius-day-wage' = @{ title="The commentary consensus: a denarius was the going rate"; body="W.D. Davies and Dale C. Allison Jr., A Critical and Exegetical Commentary on the Gospel According to Saint Matthew, International Critical Commentary, vol. 3: Commentary on Matthew XIX-XXVIII (Edinburgh: T&T Clark, 1997), commentary ad loc. Matthew 20:1-16. Davies and Allison note the denarius was the standard, unremarkable day-wage for unskilled labor across the first-century Roman world, and observe the same figure recurring as a day's pay in the deuterocanonical book of Tobit (Tobit 5:15, in its Greek-drachma form) — a detail situating Matthew's parable inside a familiar, checkable wage economy rather than an invented amount chosen to make the parable's math come out even." }
'bailey-marketplace-hiring-practice' = @{ title="The hiring square: an attested Mediterranean labor practice"; body="Kenneth E. Bailey, Jesus Through Middle Eastern Eyes: Cultural Studies in the Gospels (Downers Grove, IL: InterVarsity Press, 2008), chapter on the parable Bailey titles 'the parable of the compassionate employer' (Matthew 20:1-16). Drawing on decades spent living and teaching in the Middle East, Bailey documents the practice the parable assumes — unemployed day laborers gathering at a recognized public spot each morning hoping to be chosen for a day's paid work — as a genuinely attested regional custom, one Bailey states he personally observed still operating, in essentially the same form, in twentieth-century Middle Eastern towns." }
'luz-mark-sons-direct-request' = @{ title="Who actually asked: comparing Mark and Matthew"; body="Ulrich Luz, Matthew 8-20: A Commentary, Hermeneia series, trans. James E. Crouch (Minneapolis: Fortress Press, 2001), commentary ad loc. Matthew 20:20-23, compared with Mark 10:35-37. Mark's version of this scene has James and John approach Jesus and voice the request in their own words directly; Matthew's parallel adds their mother as the one who kneels and speaks on their behalf, though Jesus's reply in Matthew still addresses the two brothers directly, leaving them as the request's real subject even in Matthew's version. Luz and the broader commentary tradition read Matthew's added intermediary as a plausible redactional softening — deflecting the pushiness of the ask onto the mother rather than onto the two apostles themselves — though this remains an interpretive judgment about authorial intent, not a fact either text states outright." }
'deissmann-lytron-manumission' = @{ title="Lytron: a word borrowed from the slave market"; body="Adolf Deissmann, Light from the Ancient East: The New Testament Illustrated by Recently Discovered Texts of the Graeco-Roman World, trans. Lionel R.M. Strachan (New York: George H. Doran, 1927; repr. Peabody, MA: Hendrickson, 1995), 322ff. Deissmann's foundational study of contemporary Greek papyri and inscriptions documents lytron as an ordinary, well-attested commercial and legal term for the price paid to free a slave or captive, most extensively preserved in roughly a thousand manumission inscriptions from the sanctuary of Apollo at Delphi (spanning the second century BCE to the first century CE) recording real cash transactions in which a slave's freedom was purchased through a fictive sale to the god. Matthew's 'ransom for many' (20:28, lytron anti pollon) draws its rhetorical force from this attested economic practice." }
'roman-law-captive-ransom' = @{ title="Ransoming captives: a documented Roman legal category"; body="The Digest of Justinian, trans. Alan Watson (Philadelphia: University of Pennsylvania Press, 1985), Book 49, Title 15 ('On Postliminium and Those Ransomed from the Enemy'), preserving the opinions of classical Roman jurists including Ulpian and Pomponius. This title documents Roman law's formal recognition of the redemption (redemptio ab hostibus) of Roman citizens captured by an enemy — a captive's family, associates, or the state could pay to ransom them — and addresses what civil rights and property a ransomed person recovered on return under the doctrine of ius postliminii. Real, documented ancient legal infrastructure existed around paying to free a captive person, a separate register of the same economic and social institution behind the private manumission-of-slaves practice." }
'netzer-herodian-jericho-excavation' = @{ title="The excavator's own report: Netzer at Jericho"; body="Ehud Netzer, 'The Winter Palaces of the Judean Kings at Jericho,' Bulletin of the American Schools of Oriental Research 228 (1977): 1-13; see also Ehud Netzer, In the Palaces of the Hasmonean Kings and Herod (Jerusalem: Israel Exploration Society / Yad Ben-Zvi Press, 2001). Netzer's decade of excavation, beginning in 1973, at Tulul Abu el-Alayiq — the Herodian winter palace complex roughly a mile south of the Bronze Age tell of Jericho — documents successive Hasmonean and Herodian building phases, including a large hippodrome-theater and elaborate garden-and-pool architecture, confirming this as a distinct, dateable, and extensively excavated first-century royal complex separate from the older mound." }
'murphy-oconnor-herodian-jericho' = @{ title="Two Jerichos, confirmed on the ground"; body="Jerome Murphy-O'Connor, The Holy Land: An Oxford Archaeological Guide from Earliest Times to 1700, 5th ed. (Oxford: Oxford University Press, 2008), entry on Jericho. Murphy-O'Connor's standard archaeological guide situates the Hasmonean-and-Herodian winter palace complex at Tulul Abu el-Alayiq, in the Wadi Qelt south of the ancient tell, as a physically distinct first-century site from the Bronze Age mound where the Joshua conquest narrative is set. The Gospel scene of Jesus departing Jericho belongs to this Herodian-era city, not the long-abandoned Bronze Age tell." }
}

# ---- Chapter beats (with [[NOTE:slug]] placeholders) ----
$beat1 = @'
The parable's central economic detail — one denarius, paid alike to a worker hired at dawn and to one hired an hour before quitting time (20:1-16) — is not an invented number chosen to make the story's arithmetic land. It is the real, well-documented standard daily wage for an unskilled agricultural laborer across the Roman world of this period, attested in more than one independent line of first-century and near-first-century evidence.

Cornelius Tacitus, writing decades after the events this book covers, records the mutinous Pannonian legions of 14 CE citing a denarius a day as the benchmark figure for fair pay [[NOTE:tacitus-denarius-standard-pay]] — a Roman-world audience treating "a denarius a day" as a stable, recognizable reference point for ordinary compensation, not a novelist's round number invented for a parable. Independent, non-literary evidence points the same direction: Richard Duncan-Jones's compilation of dated wage receipts and payroll papyri from Roman Egypt documents unskilled and casual agricultural laborers commonly earning in the same range across the first three centuries CE [[NOTE:duncan-jones-egypt-wage-papyri]], and his broader empire-wide price and wage tables show that same figure holding up as a rough constant well beyond Egypt alone [[NOTE:duncan-jones-egypt-wage-papyri]]. Commentators working this exact passage note the same wage figure recurring as a day's pay in the deuterocanonical book of Tobit, in its Greek-drachma equivalent [[NOTE:davies-allison-denarius-day-wage]].

None of this can confirm the parable happened as narrated. What it does confirm is that its central economic detail was drawn from a real, checkable wage economy rather than invented wholesale for narrative convenience — which is exactly why the early workers' complaint about being paid the same as the latecomers (20:11-12) would have landed as a genuinely relatable economic grievance to Matthew's first audience, not an abstraction about fairness in general.
'@

$beat2 = @'
The parable's opening scene assumes a specific, documented ancient labor practice rather than an invented backdrop: a landowner going out to a public gathering place at dawn, then again at nine, at noon, at three, and at five, each time finding still more men "standing idle" and hoping to be hired (20:1-7). Unemployed day laborers across the Greco-Roman Mediterranean gathered each morning at a recognized public spot — an open hiring market functioning much like the agora or forum did for other kinds of transactions — hoping a landowner or contractor passing through would choose them for a day's paid work.

Kenneth Bailey, who spent much of his working life living and teaching in the Middle East, documents this exact custom as a genuinely attested regional practice, one he notes he personally observed still operating, in essentially the same form the parable describes, in twentieth-century Middle Eastern towns [[NOTE:bailey-marketplace-hiring-practice]]. Bailey's own preferred title for the parable — "the compassionate employer," rather than "the workers in the vineyard" — reflects his reading that the landowner's five separate trips out to find idle men are themselves the point of the story, an employer actively searching out the unemployed rather than simply filling a labor shortage [[NOTE:bailey-marketplace-hiring-practice]]. Davies and Allison's commentary on these same verses situates the hiring-square scene within the identical first-century casual-labor economy that produced the denarius wage figure itself [[NOTE:davies-allison-denarius-day-wage]] — the setting and the pay are two faces of one real economic world, not two separate inventions.

That reading of the landowner's motive is an interpretive judgment, not a textual fact the parable states outright. The hiring-square practice underneath it, though, is real, checkable social history, still visible in some traditional Mediterranean and Middle Eastern labor markets today, not a setting invented for the sake of the story.
'@

$beat3 = @'
The request scene itself (20:20-23) is one of the cleaner synoptic-comparison points in this Gospel, worth naming as a genuine textual divergence rather than smoothing it over. In Matthew, it is the mother of James and John — never named beyond that description, as this book's glossary entry on her already covers — who kneels before Jesus and voices the request that her two sons sit at his right and left hand in his kingdom. Mark's parallel version of the identical scene (10:35-37) has no mother in it at all: James and John approach Jesus themselves and make the request directly, in their own words, with no intermediary.

Jesus's reply in Matthew still addresses the two brothers directly — "You do not know what you are asking. Are you able to drink the cup that I am to drink?" (20:22) — which means even Matthew's own version keeps the sons as the request's real subject, mother or no mother standing in front of them. Ulrich Luz's commentary on the passage reads Matthew's added intermediary as a plausible redactional softening: deflecting the pushiness of the ask onto the mother rather than onto the two apostles themselves [[NOTE:luz-mark-sons-direct-request]]. That reading — a deliberate softening choice rather than a second, independent memory of the same event — remains an interpretive judgment about authorial intent, not a settled fact either Gospel states outright [[NOTE:luz-mark-sons-direct-request]]. What is a settled, checkable fact is that Mark and Matthew tell this scene with a different person doing the actual asking — a genuine divergence between two texts both describing what is meant to be the same historical moment, worth holding honestly rather than harmonizing away.
'@

$beat4 = @'
The chapter's theological center of gravity — "the Son of Man came not to be served but to serve, and to give his life as a ransom for many" (20:28) — draws its entire rhetorical force from a concrete, real-world economic image, worth grounding before any theological interpretation gets layered on top of it. The Greek behind "ransom," lytron, was an ordinary commercial and legal term in the first-century Mediterranean world: the price paid to free a slave or a captive, not a specialized religious vocabulary word coined for this saying.

Adolf Deissmann's foundational study of contemporary Greek papyri and inscriptions documents lytron in exactly this everyday commercial sense, most extensively preserved in roughly a thousand manumission inscriptions from the sanctuary of Apollo at Delphi, spanning the second century BCE to the first century CE, in which a slave's freedom is purchased through a fictive sale to the god and the price paid is recorded as the lytron [[NOTE:deissmann-lytron-manumission]]. The same economic institution existed at a different register of Roman society in the form of formally recognized captive-ransom law: the Digest of Justinian, preserving the opinions of classical Roman jurists, devotes an entire title to the ransoming of Roman citizens captured by an enemy and to what legal status a ransomed person recovered on return [[NOTE:roman-law-captive-ransom]]. Between the two — private manumission payments and formally recognized captive-ransom law — "paying to free a person from bondage" was a documented, everyday economic and legal category across more than one register of the ancient world, not an image invented for this saying [[NOTE:deissmann-lytron-manumission]].

Matthew's "ransom for many" borrows its entire rhetorical force from that attested institution: hearers in this period would have recognized the term from real slave-market and prisoner-ransom transactions before any theological claim about atonement or substitution was read into it. What the saying claims theologically about the meaning of Jesus's death is, like every other theological claim in this book, outside what external evidence can confirm or deny; what is real and checkable is the economic and legal institution the image itself is built out of.
'@

$beat5 = @'
The chapter's final scene, two blind men healed as Jesus departs Jericho (20:29-34), closes near a city already established earlier in this book as real, ancient, and continuously inhabited — see the glossary entry on JERICHO for that site's much older and separately contested role in the Joshua conquest narrative, which is not this scene. Worth adding here is a precise, often-confused archaeological point this book has not yet stated in full: there are two distinct ancient Jericho sites relevant to the Gospels, not one.

The older site, Tell es-Sultan, is the Bronze Age mound already discussed in this book's JERICHO entry, where Kathleen Kenyon's excavations dated the relevant destruction layer to roughly 1550 BCE — the Old Testament, Joshua-narrative Jericho, a different story from anything in this chapter. Separately, and about a mile to the south along the Wadi Qelt, Herod the Great and his successors built an extensive winter palace complex at a site now called Tulul Abu el-Alayiq: pools, gardens, a hippodrome-theater, and successive Hasmonean-then-Herodian building phases, excavated across ten seasons by archaeologist Ehud Netzer beginning in 1973 [[NOTE:netzer-herodian-jericho-excavation]]. This Herodian-period royal complex, not the long-abandoned Bronze Age tell, is the actual first-century site relevant to Jesus departing Jericho in this chapter, its excavation independently documented both in Netzer's own excavation reports [[NOTE:netzer-herodian-jericho-excavation]] and in standard archaeological guides to the region [[NOTE:murphy-oconnor-herodian-jericho]].

The two-Jerichos distinction also bears on a real, small synoptic puzzle this book's discussion of Mark's parallel passage has already covered in detail: where Mark and Luke each report one blind man healed near Jericho, Matthew reports two unnamed men here, consistent with the doubling pattern this book's chapter 9 depth-pass discussion of the two Gadarene demoniacs already identified as one of Matthew's recurring compositional habits [[NOTE:murphy-oconnor-herodian-jericho]]. That does not resolve every discrepancy across the three accounts on its own, but the underlying geography is real and checkable: a first-century traveler on the Jerusalem road could plausibly be described as leaving one Jericho and entering the other in the same journey.
'@

$beats = @($beat1, $beat2, $beat3, $beat4, $beat5)

# ---- Glossary additions (heading -> body) ----
$glossary = [ordered]@{
'DENARIUS (ROMAN DAY-LABORER WAGE)' = "The standard Roman silver coin paid to each vineyard worker in this chapter's opening parable, ``whatever is right'' having already been fixed at one denarius for a full day's labor (20:2, 20:9-10, 20:13). This was the real, well-documented going rate for unskilled and casual agricultural labor across the first-century Roman world, not a figure invented to make the parable's math come out even: Tacitus records Roman soldiers citing a denarius a day as a recognized pay benchmark [[NOTE:tacitus-denarius-standard-pay]], Roman-era Egyptian payroll papyri independently document unskilled laborers earning in the same range [[NOTE:duncan-jones-egypt-wage-papyri]], and the same figure recurs as a day's wage in the book of Tobit [[NOTE:davies-allison-denarius-day-wage]]. See also the tribute-penny denarius of Matthew 22:19-21, a separate scene involving the same coin denomination in a different context."
'DAY-LABORER HIRING MARKET (ROMAN-ERA MARKETPLACE)' = "The public gathering point the vineyard-workers parable assumes without explaining, where unemployed men waited each morning hoping a landowner or contractor would choose them for a day's paid work (20:1-7). This hiring-square practice is attested widely across Greco-Roman sources describing casual and agricultural labor, and Kenneth Bailey documents it as a custom still observable, in essentially the same form, in twentieth-century Middle Eastern towns [[NOTE:bailey-marketplace-hiring-practice]]. The parable's setting is a real, checkable feature of the ancient Mediterranean labor economy, not an invented backdrop for the story."
'RANSOM FOR MANY (LYTRON ANTI POLLON)' = "The Greek phrase (lytron anti pollon) behind Jesus's statement that the Son of Man came ``to give his life as a ransom for many'' (20:28). Lytron was an ordinary commercial and legal term in the ancient Mediterranean world for the price paid to free a slave or captive, attested in roughly a thousand manumission inscriptions from Delphi [[NOTE:deissmann-lytron-manumission]] and in Roman law's own formal treatment of ransoming captives from an enemy [[NOTE:roman-law-captive-ransom]]. The theological interpretation of Jesus's death as such a ``ransom'' sits outside what external evidence can confirm or deny; the concrete economic and legal institution the image draws on is real and independently documented."
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
    Add-BeatNode $Ch20NodeId $id $sortKey
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
Seed-Entity "Herodian Winter Palace at Jericho (Tulul Abu el-Alayiq)" "herodian-winter-palace-jericho" "place" "Hasmonean-and-Herodian winter palace complex south of Tell es-Sultan along the Wadi Qelt, excavated by Ehud Netzer from 1973; the actual first-century Jericho site relevant to Matthew 20:29-34, distinct from the Bronze Age Old Testament tell."
Seed-Entity "Ransom (Lytron Anti Pollon)" "ransom-lytron-anti-pollon" "vocabulary" "Greek economic/legal term for the price paid to free a slave or captive, the concrete background image behind Matthew 20:28's 'ransom for many.'"
Seed-Entity "Day-Laborer Hiring Market (Roman-Era)" "day-laborer-hiring-market-roman-era" "vocabulary" "Attested Greco-Roman practice of unemployed day laborers gathering at a public spot each morning hoping to be hired, the real social setting assumed by the parable in Matthew 20:1-16."

$conn.Close()
Write-Host "DONE Matthew Chapter 20 depth pass."
