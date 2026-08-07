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

# ---- Live state ----
$script:MaxNumber = [int](Exec-Scalar "SELECT MAX(Number) FROM Beats")
$NotesNodeId = [guid]"019FA96B-18E2-7BB4-BAEB-11ACA24934F4"
$GlossaryNodeId = [guid]"019FA96B-29F5-7BB9-99D0-0F787960E530"
$Ch7NodeId = [guid]"019FA969-E19C-72C6-BB63-117877953132"
$maxNoteSortKey = [double](Exec-Scalar "SELECT ISNULL(MAX(bn.SortKey),0) FROM BeatNodes bn WHERE bn.NodeId='019FA96B-18E2-7BB4-BAEB-11ACA24934F4'")
$maxGlossarySortKey = [double](Exec-Scalar "SELECT ISNULL(MAX(bn.SortKey),0) FROM BeatNodes bn WHERE bn.NodeId='019FA96B-29F5-7BB9-99D0-0F787960E530'")
$maxNoteNumber = [int](Exec-Scalar "SELECT MAX(CAST(LEFT(b.Text, CHARINDEX(' ',b.Text)-1) AS INT)) FROM BeatNodes bn JOIN Beats b ON bn.BeatId=b.Id WHERE bn.NodeId='019FA96B-18E2-7BB4-BAEB-11ACA24934F4'")
Write-Host "Starting: MaxNumber=$script:MaxNumber MaxNoteSortKey=$maxNoteSortKey MaxNoteNumber=$maxNoteNumber MaxGlossarySortKey=$maxGlossarySortKey"

$em = [char]8212

# ---- Notes ----
$notes = [ordered]@{
'antipas-no-roman-garrison' = @{ title='No Roman legion was stationed in Galilee'; body="Bible Odyssey, 'Herod Antipas' entry (Society of Biblical Literature, bibleodyssey.org); standard scholarship on Herodian client rule. Antipas ruled Galilee and Perea as tetrarch with his own independent military forces from 4 BCE to 39 CE, and no Roman imperial garrison was stationed in Galilee or Perea in this period; Roman legionary forces for the region were based in Syria and deployed only when needed. This supports reading 'centurion' in Luke 7:1-10 as a term applied to a non-Roman officer rather than evidence of a literal Roman garrison in Antipas's territory." }
'centurion-title-luke-acts' = @{ title="A loanword for Antipas's own officer corps"; body="Alexander Kyrychenko, The Roman Army and the Expansion of the Gospel: The Role of the Centurion in Luke-Acts, Beihefte zur Zeitschrift fur die neutestamentliche Wissenschaft 203 (Berlin/Boston: Walter de Gruyter, 2014). Kyrychenko argues that Luke uses centurions across Luke-Acts as a recurring literary device -- prototypical sympathetic Gentile figures anticipating the Gentile mission -- and that the Capernaum centurion was most plausibly an officer in Herod Antipas's own forces, organized along Roman lines despite not being components of the Roman legionary army itself." }
'capernaum-synagogue-basalt' = @{ title="A basalt foundation beneath the later synagogue"; body="Excavation findings of Franciscan archaeologists Virgilio Corbo and Stanislao Loffreda at Capernaum. A black basalt foundation was found beneath the later Byzantine-period white limestone synagogue at Capernaum; associated first-century pottery beneath the cobblestone floor is used to date this basalt-foundation structure to the first century CE, making it a plausible, though not certain, physical trace of the building described in Luke 7:5." }
'aphrodisias-godfearers' = @{ title="Gentile patrons of synagogue life, independently attested"; body="Joyce Reynolds and Robert F. Tannenbaum, Jews and Godfearers at Aphrodisias: Greek Inscriptions with Commentary, Cambridge Philological Society Supplementary Volume 12 (Cambridge: Cambridge Philological Society, 1987). The inscribed marble pillar found at Aphrodisias in 1976, associated with a Jewish memorial building of the early third century CE, lists 69 Jewish donor names and 54 Greek names identified separately as theosebeis (God-fearers) -- Gentiles with a documented, financially-committed affiliation to the Jewish community, several of whom held civic office. This is the strongest surviving epigraphic proof that Gentile patrons of Jewish institutional life were a real and recognized class, though the inscription itself is roughly two centuries later than this scene." }
'nain-site-id' = @{ title='Nain identified, but never fully excavated'; body="'Nain,' Bible Odyssey (Society of Biblical Literature); site visit accounts summarized from Edward Robinson and Eli Smith's mid-nineteenth-century Palestine survey. Nain is identified with the modern village of Nein on the northwest slope of Jebel Dahi, an identification traceable to Eusebius's and Jerome's fourth-century place-name lists and reaffirmed by Robinson and Smith's on-the-ground survey. Iron Age and Hellenistic-period pottery sherds attest continuous habitation, but the modern village overlying the ancient site has prevented any full modern excavation." }
'jewish-funeral-customs' = @{ title='A documented first-century funeral procession'; body="'Burial Practices in First Century Palestine,' Bible Odyssey (Society of Biblical Literature); Jewish Virtual Library, 'Death and Bereavement in Judaism: Ancient Burial Practices,' drawing on Mishnaic-era sources. Documented first-century Jewish practice called for same-day burial, an open bier carried on the shoulders of rotating bearer teams, and a procession customarily led or flanked by wailing mourning women performing lament; Luke 7:12's description of a bier, a crowd of mourners, and the widow at the front of the procession matches this documented social pattern." }
'josephus-john-imprisonment' = @{ title="Josephus gives a different motive for John's arrest"; body="Flavius Josephus, Antiquities of the Jews 18.5.2 (section 116-119), consulted via the standard William Whiston translation (1737). Josephus states that Herod Antipas had John executed because he feared John's popular influence and persuasive power over the crowds might lead to a rebellion he could not control -- a political-security rationale independent of, and different in emphasis from, the Gospels' account (Mark 6:17-20; Matthew 14:3-5; cf. Luke 3:19-20) that Antipas acted over John's public condemnation of his marriage to Herodias." }
'machaerus-excavation' = @{ title="Machaerus: an excavated, confirmed fortress"; body="Gyozo Voros, excavation reports summarized in 'Machaerus: Beyond the Beheading of John the Baptist,' Biblical Archaeology Society (biblicalarchaeology.org). The Hungarian Academy of Arts, led by architect-archaeologist Gyozo Voros, has excavated the Machaerus fortress site (in modern Jordan) continuously since 2009, uncovering the royal courtyard, columns, baths, cisterns, and the reconstructed banquet/throne hall of the Herodian palace that Josephus names as the site of John the Baptist's imprisonment and execution; the site's identity was confirmed in 1968 after being lost to memory following its destruction by Rome circa 71-72 CE." }
'pharisee-banquet-reclining' = @{ title='How first-century dinner guests reclined'; body="'A Feast for the Senses ... and the Soul,' Biblical Archaeology Society (biblicalarchaeology.org); comparable summaries of first-century Greco-Roman/Jewish triclinium dining practice. Elite first-century Jewish households, following adopted Greco-Roman custom, dined reclining on low couches arranged around a central table, propped on the left elbow with feet extended outward away from the table toward the room's open edge -- a documented physical arrangement that explains how an uninvited woman could approach and touch a guest's feet at Luke 7:37-38 without first passing among the seated diners." }
'myron-vs-nard-terminology' = @{ title="Luke's word is generic, not specifically nard"; body="Philological comparison of Luke 7:37 (myron, ointment/perfumed oil, generic) against John 12:3 (nardou pistikes, genuine/unadulterated nard), per standard Greek-text lexicon tools. Luke's account never specifies nard, so claims about the cost of the ointment in this scene properly belong to John's account, not Luke's." }
'nard-trade-cost' = @{ title="What imported nard actually cost"; body="Summarized from period-sourced overviews of the ancient spice trade and standard reference summaries of Nardostachys jatamansi sourcing. Nard was sourced from a plant native to the Himalayan foothills of Nepal and northern India and reached the Mediterranean via long overland/maritime spice routes, making it a luxury import; John's Gospel separately prices a pound of pure nard at 300 denarii (John 12:5), roughly a year's wage for an unskilled day laborer at period rates." }
'bethany-anointing-synoptic-identity' = @{ title='One anointing story, or three?'; body="R. Reed, 'The Sinful Woman of Luke 7:36-50: An Exploration of Her Actions' (MA by Research thesis, University of Birmingham, 2024, etheses.bham.ac.uk); cross-referenced against standard synoptic-comparison summaries. The field is genuinely divided on whether Luke 7:36-50 is the same incident as Mark 14:3-9/Matthew 26:6-13 (Bethany, shortly before the Passion) or John 12:1-8 (explicitly Mary of Bethany): proponents of a distinct tradition point to the different host, different anointed body part, and different narrative point in the ministry; proponents of partial identity point to shared feet/hair/perfume/table-objection details. No consensus resolves the question either way." }
'gregory-composite-magdalene' = @{ title="How three women became one, in 591 CE"; body="Homily 33 of Pope Gregory I (Gregory the Great), delivered c. 591 CE, discussed in patristic scholarship on the sermon's reception history. Gregory's homily explicitly states, 'She whom Luke calls the sinful woman, whom John calls Mary, we believe to be the Mary from whom seven devils were ejected according to Mark' -- fusing Luke's unnamed sinful woman (7:36-50), John's Mary of Bethany, and Mary Magdalene (Luke 8:2) into one composite figure, a reading that shaped Western Christian devotional tradition for centuries and originated Mary Magdalene's popular, unscriptural reputation as a reformed prostitute." }
}

# ---- Chapter beats ----
$beat1 = @"
A Roman-titled officer stationed at Capernaum sends Jewish elders, then his own friends, to ask Jesus to heal a slave "he valued highly" (7:2), who is dying. Jesus never enters the house; the centurion's line -- "just say the word, and my servant will be healed" (7:7) -- becomes the pericope's point, and Jesus calls it faith unmatched "even in Israel" (7:9).

Here's the "wait, actually": the text calls this man a centurion, and popular retellings usually picture a Roman legionary billeted in a conquered province. But Capernaum in the late 20s CE sat inside Herod Antipas's tetrarchy, not under direct Roman rule -- Judea proper was administered by a Roman prefect, while Galilee and Perea were Antipas's own client territory, and Rome kept no legionary garrison there [[NOTE:antipas-no-roman-garrison]]. The mainstream historical-critical reading is that this was almost certainly an officer in Antipas's own security forces, organized "in the Roman manner," with Roman-style ranks, even though the rank-and-file were not Roman citizens or legionaries [[NOTE:centurion-title-luke-acts]]. Luke, writing decades later for a Greco-Roman audience, likely uses the loanword because it was the term his readers understood, not because the man wore Rome's eagle.

The detail that the man "loves our nation and built us our synagogue" (7:5) is a second genuinely checkable claim, and here the independent record is a real corroboration rather than a debunk: excavations beneath the later Byzantine synagogue at Capernaum uncovered a black basalt foundation dated by associated first-century pottery, plausibly the building standing in Jesus's lifetime [[NOTE:capernaum-synagogue-basalt]] -- and separately, Gentile patronage of synagogue construction is independently and unambiguously attested in the epigraphic record, most famously the early-third-century Aphrodisias inscription, which lists dozens of Gentile God-fearers as donors alongside Jewish donors to a synagogue building fund [[NOTE:aphrodisias-godfearers]]. The custom Luke describes -- a sympathetic Gentile patron underwriting a synagogue -- is a documented social pattern of the period, even though the Aphrodisias inscription postdates this scene by roughly two centuries and cannot confirm this specific synagogue or this specific man.
"@

$beat2 = @"
Jesus, entering the town of Nain with a crowd, meets a funeral procession -- "the only son of his mother, and she was a widow" (7:12) -- being carried out for burial, and, moved, stops the bier and raises the young man.

Two elements here are genuinely checkable, independent of the miracle claim itself. First, place: Nain is conventionally identified with the modern Arab village of Nein, roughly 9-10 km southeast of Nazareth -- an identification going back to Eusebius and Jerome in the fourth century and confirmed on the ground by nineteenth-century explorers Edward Robinson and Eli Smith. The toponym has survived essentially unbroken, and pottery finds show Iron Age and Hellenistic-period occupation, but the site has never been the subject of a full modern excavation [[NOTE:nain-site-id]]. Second, custom: the funeral itself follows documented Jewish burial practice of the period -- same-day burial was the norm, the body was carried on an open bier by rotating teams of bearers, and mourning women customarily walked ahead of or alongside the bier, wailing and leading lament -- exactly the scene Luke stages [[NOTE:jewish-funeral-customs]]. That specificity is a point in favor of the account reflecting a real social world accurately, whatever one makes of the miracle itself, which this book does not adjudicate.
"@

$beat3 = @"
John, already imprisoned, sends disciples to ask whether Jesus is "the one who is to come" (7:19); Jesus answers by pointing to his healings and preaching, then praises John to the crowd as the greatest of the prophets (7:18-35).

This section contains the strongest independent-record connection in the whole chapter. John's existence, popularity, and death by execution under Herod Antipas is corroborated entirely outside the Gospels by the Jewish historian Flavius Josephus, writing around 93-94 CE -- a source with no stake in Christian claims about John. But the corroboration comes with a genuine, checkable divergence, not a clean confirmation. Josephus states plainly that Antipas had John killed because he feared John's popular influence might spark a rebellion -- a political-security rationale. The Gospels instead foreground a personal/moral motive: John's public condemnation of Antipas's marriage to Herodias, his brother's ex-wife [[NOTE:josephus-john-imprisonment]]. These aren't necessarily mutually exclusive in a ruler's actual head, but they are two independently-derived explanatory frames, and the honest position is that Josephus's political framing and the Gospels' personal framing cannot simply be harmonized into one "real" motive without speculation.

On the imprisonment site: Josephus places John's imprisonment and execution at Machaerus, a Herodian fortress-palace east of the Dead Sea in modern Jordan. Machaerus is a securely identified and substantially excavated site -- Hungarian archaeologist Gyozo Voros has led ongoing excavation and reconstruction there since 2009, uncovering the royal courtyard, baths, cisterns, and the throne room where, by tradition, Antipas's banquet and Salome's dance would have taken place [[NOTE:machaerus-excavation]]. This is a case where "Attested" is the right word for the site and the political fact of the execution, while the specific staged scene of Herodias's daughter dancing for a head on a platter belongs to the Gospels alone; Josephus never mentions a Salome banquet-dance at all.
"@

$beat4 = @"
At a dinner in the house of a Pharisee named Simon (7:40), an unnamed woman "who lived a sinful life" (7:37) enters, weeps at Jesus's feet, wipes them with her hair, kisses them, and anoints them with perfumed oil from an alabaster jar; Jesus defends her against Simon's silent judgment and tells her, "your sins are forgiven" (7:48).

Two backdrop details make the scene physically legible. First, the seating: elite and Pharisaic households of this period, following Greco-Roman convention, dined reclining on low couches, propped on the left elbow, with the diners' feet extended outward toward the room's open perimeter -- exactly the physical arrangement that lets an uninvited woman approach a guest's feet from behind without disturbing the meal [[NOTE:pharisee-banquet-reclining]]. Second, the perfume: Luke's Greek word here is the generic myron, not the more specific nardos of John's later account [[NOTE:myron-vs-nard-terminology]]. Imported nard, when it is the substance in question, came from a plant native to the Himalayan foothills, carried west at a cost that made a single alabaster flask a luxury purchase -- John's Gospel later prices a pound of it at 300 denarii, on the rough order of a year's wage for a day laborer [[NOTE:nard-trade-cost]].

Which raises the chapter's real open question, and a genuinely disputed one rather than a settled either-way: is this the same incident as the anointing at Bethany reported in Mark and Matthew (shortly before the Passion, ointment poured on Jesus's head, in the house of "Simon the leper"), or the anointing in John 12:1-8 (explicitly Mary of Bethany, feet, six days before Passover)? The scholarly field is genuinely split [[NOTE:bethany-anointing-synoptic-identity]]. The most consequential downstream effect of collapsing all these women into one is a real, datable event in reception history: Pope Gregory the Great, in a homily delivered around 591 CE, explicitly fused Luke's unnamed sinful woman, Mary of Bethany, and Mary Magdalene into a single composite figure -- a reading that dominated Western Christian tradition for over a millennium and is the direct source of Mary Magdalene's enduring popular reputation as a reformed prostitute, a reputation the biblical text itself never assigns her [[NOTE:gregory-composite-magdalene]]. This is a textbook case of the heritage-vs-history gap this project exists to name.
"@

$beats = @($beat1, $beat2, $beat3, $beat4)

# ---- New glossary entries (unique to ch7) ----
$glossary = [ordered]@{
'NAIN' = "A small Galilean village, identified with the modern Arab village of Nein on the northwest slope of Jebel Dahi (Hill of Moreh), roughly 9-10 km southeast of Nazareth, and the setting of the widow's son's raising (7:11-17). The identification rests on continuous toponym survival and fourth-century patristic testimony, confirmed on the ground by nineteenth-century surveyors; the site has never been the subject of a full modern excavation [[NOTE:nain-site-id]]."
'MACHAERUS' = "A Herodian fortress-palace east of the Dead Sea (in modern Jordan), independently named by Flavius Josephus as the site of John the Baptist's imprisonment and execution under Herod Antipas. Lost to memory after Rome destroyed it circa 71-72 CE, the site was securely reidentified in 1968 and has been under active excavation and partial reconstruction by Hungarian archaeologist Gyozo Voros since 2009 [[NOTE:machaerus-excavation]]."
'HERODIAS' = "Wife of Herod Antipas (previously married to his brother, per the Gospels' account) and, in the Gospels' telling, the reason John the Baptist publicly condemned Antipas and was imprisoned. This chapter's discussion of 7:18-35 notes the genuine divergence between the Gospels' personal/moral rationale for John's imprisonment and Josephus's political rationale [[NOTE:josephus-john-imprisonment]]."
'SIMON (THE PHARISEE, LUKE 7)' = "The otherwise-unidentified Pharisee host named 'Simon' at Luke 7:40, in whose house the sinful woman anoints Jesus's feet. Nothing external corroborates this individual; he is known only from this narrative, and, per the synoptic-identity debate, is a different figure from 'Simon the leper,' host of the later Bethany anointing in Mark and Matthew [[NOTE:bethany-anointing-synoptic-identity]]."
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
    Add-BeatNode $Ch7NodeId $id $sortKey
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
function Try-Append([string]$heading, [string]$extra) {
    $id = Find-GlossaryBeatId $heading
    if ($id) {
        foreach ($slug in $slugToNumber.Keys) { $extra = $extra.Replace("[[NOTE:$slug]]", "[$($slugToNumber[$slug])]") }
        Append-ToExistingBeat $id $extra
        Write-Host "Appended to $heading"
    } else { Write-Host "NOT FOUND: $heading" }
}

Try-Append "JOHN THE BAPTIST" "This chapter adds: Josephus's stated motive for the execution (fear of rebellion, per Antiquities 18.5.2) diverges from the Gospels' Herodias-based motive, and names Machaerus as the corroborated, excavated site of the imprisonment/execution [[NOTE:josephus-john-imprisonment]] [[NOTE:machaerus-excavation]]."
Try-Append "FLAVIUS JOSEPHUS" "This chapter cites a specific, precisely-located passage (Antiquities of the Jews 18.5.2, section 116-119) and its content regarding John the Baptist's imprisonment motive, distinct from any prior Josephus citation in this book."
Try-Append "HEROD ANTIPAS" "This chapter adds: maintained his own independent, non-Roman-legionary military forces organized along Roman lines, meaning 'centurion' references to his territory (Luke 7:1-10) likely denote an officer in his own service, not a literal Roman soldier [[NOTE:antipas-no-roman-garrison]] [[NOTE:centurion-title-luke-acts]]."
Try-Append "CAPERNAUM" "This chapter adds: a black basalt synagogue foundation, dated by associated first-century pottery, is a plausible physical trace of the synagogue this chapter's centurion is said to have built (7:5) [[NOTE:capernaum-synagogue-basalt]]."

$conn.Close()
Write-Host "DONE Chapter 7."
