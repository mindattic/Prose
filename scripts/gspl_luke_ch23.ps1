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
$Ch23NodeId = [guid]"019FA96A-F7DB-7C55-A3FB-B537AC6B6848"
$maxNoteSortKey = [double](Exec-Scalar "SELECT ISNULL(MAX(bn.SortKey),0) FROM BeatNodes bn WHERE bn.NodeId='019FA96B-18E2-7BB4-BAEB-11ACA24934F4'")
$maxGlossarySortKey = [double](Exec-Scalar "SELECT ISNULL(MAX(bn.SortKey),0) FROM BeatNodes bn WHERE bn.NodeId='019FA96B-29F5-7BB9-99D0-0F787960E530'")
$maxNoteNumber = [int](Exec-Scalar "SELECT MAX(CAST(LEFT(b.Text, CHARINDEX(' ',b.Text)-1) AS INT)) FROM BeatNodes bn JOIN Beats b ON bn.BeatId=b.Id WHERE bn.NodeId='019FA96B-18E2-7BB4-BAEB-11ACA24934F4'")
Write-Host "Starting: MaxNumber=$script:MaxNumber MaxNoteSortKey=$maxNoteSortKey MaxNoteNumber=$maxNoteNumber MaxGlossarySortKey=$maxGlossarySortKey"

$em = [char]8212

# ---- Notes ----
$notes = [ordered]@{
'pilate-charges-maiestas' = @{ title="Kingship as the one charge with real teeth"; body="B. C. McGing, 'Pontius Pilate and the Sources,' Catholic Biblical Quarterly 53 (1991): 416-438. McGing surveys the ancient sources on Pilate's prefecture; a claim to kingship, unlike a purely religious or tax dispute, mapped directly onto Roman maiestas (treason against the emperor) and was the charge with genuine legal weight in Tiberian Rome." }
'herod-trial-luke-exclusive' = @{ title="A scene found only in Luke"; body="Raymond E. Brown, The Death of the Messiah: From Gethsemane to the Grave, vol. 1 (New York: Doubleday, 1994), section 33. Brown catalogs the Herod Antipas hearing as material found exclusively in Luke, with no trace in Mark, Matthew, or John, likely rooted in an early tradition Luke has reshaped for his own literary and theological ends." }
'roman-jurisdiction-governors' = @{ title="Consultation, not a formal jurisdictional handoff"; body="Peter Garnsey, 'The Criminal Jurisdiction of Governors,' Journal of Roman Studies 58 (1968): 51-59. Garnsey's study of provincial governors' wide discretionary criminal jurisdiction provides the legal backdrop against which Luke's Pilate-to-Herod referral must be judged; Acts 25:13-22 (Festus consulting Agrippa II over Paul) supplies the closest attested real-world analogue, and it falls short of a full jurisdictional handoff of the kind Luke describes." }
'barabbas-custom-privilegium-paschale' = @{ title="No independent attestation of a standing amnesty custom"; body="John Curran, 'Pilate, Barabbas, and the Privilegium Paschale: Law and Leverage in Roman Judaea,' Journal for the Study of the New Testament 47.4 (2025): 501-525; Josephus, Antiquities of the Jews 20.215 and 17.233. No Jewish or Roman source independent of the Gospels documents a standing Passover prisoner-release custom exercised by Roman governors; what exists instead is circumstantial evidence that governors could and did release prisoners on other occasions." }
'barabbas-name-etymology' = @{ title="'Son of the father'"; body="Standard lexical derivation of Aramaic bar abba, 'son of the father,' widely attested in New Testament lexicons and onomastic studies of the period; the name also occurs as a surname among rabbis of the era, suggesting it functioned as an ordinary personal name rather than a symbolic invention." }
'cyrene-jewish-diaspora' = @{ title="A real, attested diaspora community"; body="Strabo, as quoted in Josephus, Antiquities of the Jews 14.7.2 (sections 114-118); Acts 2:10, 6:9, 11:20. Strabo describes four classes of the population of Cyrene under Sulla, including Jews as a recognized class; independent classical and biblical attestation together confirm a substantial Cyrenian Jewish community with members present in first-century Jerusalem." }
'patibulum-crossbeam-practice' = @{ title="Only the crossbeam, not a whole cross"; body="Ruben van Wingerden, 'Carrying a patibulum: A Reassessment of Non-Christian Latin Sources,' New Testament Studies 66.3 (2020): 433-453. Confirms that Roman crucifixion practice had the condemned carry only the crossbeam (patibulum) to a fixed, reused upright stake at the execution site, rather than an entire assembled cross." }
'paradise-persian-loanword' = @{ title="A Persian loanword for a walled garden"; body="Markus Bockmuehl and Guy G. Stroumsa, eds., Paradise in Antiquity: Jewish and Christian Views (Cambridge: Cambridge University Press, 2010), ch. 5. Traces 'paradise' from the Old Persian pairidaeza ('walled enclosure/park'), through Xenophon's Greek borrowing and the Septuagint's use of the word for Eden, into first-century Jewish eschatological usage as a heavenly counterpart to the earthly garden." }
'yehohanan-ossuary-heel-nail' = @{ title="The only skeletal evidence of Roman crucifixion"; body="Vassilios Tzaferis's 1968 excavation report, Israel Exploration Journal 20 (1970); Joe Zias and Eliezer Sekeles, 'The Crucified Man from Giv'at ha-Mivtar: A Reappraisal,' Israel Exploration Journal 35.1 (1985): 22-27. The 1968 find remains the only skeletal evidence of Roman crucifixion recovered from antiquity -- an 11.5cm nail bent through a right heel bone -- though the 1985 reappraisal revised several of the original claims." }
'thallus-phlegon-darkness' = @{ title="A thin, thirdhand chain of pagan testimony"; body="N. P. L. Allen, 'Thallus and Phlegon: Solar Eclipse in Jerusalem c. 33 CE?,' Akroterion 63 (2018): 73-93. Allen's analysis traces both writers' testimony through the sole surviving transmission chain -- the third-century Christian chronicler Julius Africanus, himself preserved only in later excerpts -- and shows that in what survives, neither Thallus nor Phlegon explicitly names Jesus or the crucifixion; the connection is made by Africanus, generations after the fact." }
'eclipse-passover-astronomical-impossibility' = @{ title="A solar eclipse is impossible at full moon"; body="Colin J. Humphreys and W. G. Waddington, 'Dating the Crucifixion,' Nature 306 (1983): 743-746. Confirms that a solar eclipse is impossible at Passover, a full-moon festival, and instead calculate a visible lunar eclipse over Jerusalem on the evening of April 3, 33 CE -- a different phenomenon that cannot account for midday darkness as Luke describes it." }
'joseph-arimathea-tomb-burial-custom' = @{ title="A standard mode of burial for the wealthy"; body="Byron R. McCane, Roll Back the Stone: Death and Burial in the World of Jesus (Harrisburg, PA: Trinity Press International, 2003). McCane's archaeological survey confirms rock-cut family tombs, as opposed to simple trench graves, were the standard mode of burial for first-century Jerusalem's wealthier residents." }
}

# ---- Chapter beats ----
$beat1 = @"
Before Pilate, the accusers bring three charges in a single breath: that Jesus has been "subverting our nation," forbidding the payment of tribute to Caesar, and calling himself a king (23:1-5). The first two charges dress up a religious dispute in the vocabulary Rome actually prosecuted; it is the third charge, kingship, that a career Roman administrator would have heard as an accusation of treason against the emperor -- maiestas -- the one charge on this list with real teeth under Tiberius [[NOTE:pilate-charges-maiestas]]. Luke's Pilate finds "no fault in this man" (23:4) -- a verdict he will render twice more before the day is over.
"@

$beat2 = @"
Then the story takes a turn that exists nowhere else in the New Testament. Learning Jesus is a Galilean, Pilate sends him to Herod Antipas, tetrarch of Galilee, who happens to be in Jerusalem for the festival (23:6-12). This is worth stating precisely: the entire Herod interrogation -- the tetrarch's curiosity, the mockery, the "gorgeous robe," the return to Pilate -- appears only in Luke. Mark, Matthew, and John have no knowledge of it whatsoever [[NOTE:herod-trial-luke-exclusive]]. Is the underlying maneuver at least plausible as Roman administrative practice? The honest answer is: partially, and imperfectly. Roman governors held wide personal discretion, and provincial administrators are independently attested consulting local client rulers on cases touching their subjects' affairs -- Festus does exactly this with King Agrippa II over Paul a generation later. But that is consultation, not a formal transfer of jurisdiction [[NOTE:roman-jurisdiction-governors]]. Luke's scene goes further, sending Jesus to Herod as if venue itself could shift on nationality -- not documented as impossible, but not documented as routine either. Luke notes that Herod and Pilate had been enemies, apparently over the Galileans Pilate had killed while they sacrificed -- the same incident this book examined in Chapter 13 -- and that this encounter made them friends (23:12).
"@

$beat3 = @"
The crowd's choice is the hardest-edged claim in the chapter. All three Synoptics and John describe a custom whereby the Roman governor released one Passover prisoner at the crowd's request, and on this point the honest verdict has to be as blunt as this book was about Pilate's Galilean massacre in Chapter 13: no Jewish or Roman source outside the Gospels documents any such standing custom, and no Roman governor anywhere else in the empire is known to have held one [[NOTE:barabbas-custom-privilegium-paschale]]. What exists instead is circumstantial: Josephus records Herod Archelaus releasing prisoners at a Passover decades earlier and the later governor Albinus emptying the prisons for a bribe -- evidence that governors could and did release prisoners, not evidence of a fixed annual amnesty. The name of the prisoner released in his place carries its own dark irony worth naming precisely: Barabbas, from the Aramaic bar-abba, literally "son of the father" [[NOTE:barabbas-name-etymology]].
"@

$beat4 = @"
On the road out to the execution ground, Luke names a bystander pressed into carrying the crossbeam: Simon, "of Cyrene" (23:26). Cyrene was a real North African Greek city, home to a substantial and well-attested Jewish community; the geographer Strabo counted Jews as one of four recognized classes of the city's population [[NOTE:cyrene-jewish-diaspora]]. Cyrenian Jews turn up independently in Jerusalem in this same period -- among the Pentecost pilgrims Acts lists, among the Hellenist opponents of Stephen, and among the believers who first preached to Greeks at Antioch -- a real diaspora-demographic pattern, not a detail invented for this one verse. On what Simon actually carries: Roman crucifixion required the condemned to carry only the crossbeam, the patibulum, to a fixed upright post already standing at the execution site, since wood was expensive and reused [[NOTE:patibulum-crossbeam-practice]].
"@

$beat5 = @"
At the execution itself, one of the two criminals crucified alongside Jesus asks to be remembered, and receives an answer that has shaped two thousand years of Christian afterlife imagery: "today you will be with me in paradise" (23:43). The word itself has a traceable, entirely secular origin -- it descends from Old Persian pairidaeza, "a walled-around enclosure," describing the hunting parks of Persian nobility, borrowed into Greek and from there into the Septuagint's translation of Eden [[NOTE:paradise-persian-loanword]]. On the mechanics of the execution itself, this book can point to genuine physical evidence: in 1968, archaeologists excavating a tomb at Giv'at ha-Mivtar recovered the ossuary of a man named Yehohanan, with an iron nail still lodged in his right heel bone [[NOTE:yehohanan-ossuary-heel-nail]]. It remains the only skeletal evidence for Roman crucifixion recovered anywhere from antiquity -- a genuine, singular data point, not a representative sample.
"@

$beat6 = @"
The chapter's most fragile evidentiary claim comes next: an unnatural darkness falls from noon until three (23:44-45), and popular apologetics has long pointed to two non-Christian ancient writers, Thallus and Phlegon of Tralles, as independent pagan corroboration. The honest accounting has to be much more cautious. Neither man's original work survives; both are known only through a chain of secondhand quotation, and in both cases it is the third-century Christian writer Julius Africanus, not Thallus or Phlegon, who makes the connection to Jesus's crucifixion [[NOTE:thallus-phlegon-darkness]]. And there is a hard astronomical ceiling on any solar-eclipse reading regardless of the sources' reliability: Passover falls at the full moon, and a solar eclipse is astronomically impossible at full moon [[NOTE:eclipse-passover-astronomical-impossibility]]. This is a case where the evidence genuinely is as thin as skeptics say.

Finally, the burial. Joseph of Arimathea, "a member of the council" who had not consented to the verdict, asks Pilate for the body and lays it in a new rock-cut tomb (23:50-56) -- the same class of burial this book has already established as standard practice for Jerusalem's wealthier residents in this period [[NOTE:joseph-arimathea-tomb-burial-custom]].
"@

$beats = @($beat1, $beat2, $beat3, $beat4, $beat5, $beat6)

# ---- New glossary entries ----
$glossary = [ordered]@{
'BARABBAS' = "Prisoner released in Jesus's place at the crowd's demand (23:18-25); name derives from Aramaic bar-abba, `"son of the father`" [[NOTE:barabbas-name-etymology]]."
'SIMON OF CYRENE' = "North African Jewish diaspora bystander pressed to carry the crossbeam to the execution site (23:26) [[NOTE:cyrene-jewish-diaspora]] [[NOTE:patibulum-crossbeam-practice]]."
'CYRENE' = "North African Greek city (modern Libya) with an attested Jewish community, one of four recognized population classes per Strabo [[NOTE:cyrene-jewish-diaspora]]."
'PRIVILEGIUM PASCHALE' = "Scholarly term for the disputed Passover prisoner-release custom depicted in all four Gospels; no independent Jewish or Roman source documents such a standing amnesty [[NOTE:barabbas-custom-privilegium-paschale]]."
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
    Add-BeatNode $Ch23NodeId $id $sortKey
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
Try-Append "PONTIUS PILATE" "This chapter adds: the three-charge accusation (23:2) and its mapping onto real Roman legal categories (seditio for the tax/subversion charges, maiestas for the kingship charge) [[NOTE:pilate-charges-maiestas]]." $slugToNumber
Try-Append "HEROD ANTIPAS" "This chapter adds: the Luke-exclusive trial scene (23:6-12), with no parallel in Mark, Matthew, or John, and the Pilate-Herod reconciliation note (23:12) cross-referenced to the Galileans-killed-by-Pilate incident covered in Chapter 13 [[NOTE:herod-trial-luke-exclusive]]." $slugToNumber
Try-Append "THE TEMPLE" "This chapter adds: Joseph of Arimathea's status as `"a member of the council`" who dissented from the verdict (23:50-51), a documented instance of internal Sanhedrin non-unanimity." $slugToNumber

# ---- Seed new entities ----
Seed-Entity "Privilegium Paschale" "privilegium-paschale" "vocabulary" "Scholarly term for the disputed Passover prisoner-release custom."

$conn.Close()
Write-Host "DONE Chapter 23."
