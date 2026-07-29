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
$Ch24NodeId = [guid]"019FA96B-0852-788F-A080-F71808F0DC08"
$maxNoteSortKey = [double](Exec-Scalar "SELECT ISNULL(MAX(bn.SortKey),0) FROM BeatNodes bn WHERE bn.NodeId='019FA96B-18E2-7BB4-BAEB-11ACA24934F4'")
$maxGlossarySortKey = [double](Exec-Scalar "SELECT ISNULL(MAX(bn.SortKey),0) FROM BeatNodes bn WHERE bn.NodeId='019FA96B-29F5-7BB9-99D0-0F787960E530'")
$maxNoteNumber = [int](Exec-Scalar "SELECT MAX(CAST(LEFT(b.Text, CHARINDEX(' ',b.Text)-1) AS INT)) FROM BeatNodes bn JOIN Beats b ON bn.BeatId=b.Id WHERE bn.NodeId='019FA96B-18E2-7BB4-BAEB-11ACA24934F4'")
Write-Host "Starting: MaxNumber=$script:MaxNumber MaxNoteSortKey=$maxNoteSortKey MaxNoteNumber=$maxNoteNumber MaxGlossarySortKey=$maxGlossarySortKey"

$em = [char]8212

# ---- Notes ----
$notes = [ordered]@{
'josephus-women-testimony' = @{ title="Josephus states the operative social fact directly"; body="Flavius Josephus, Antiquities of the Jews, Book 4, ch. 8, section 15 (Whiston numbering; = section 219 Niese), trans. William Whiston. In a passage cataloguing who may serve as a legal witness, Josephus states plainly, 'let not the testimony of women be admitted, on account of the levity and boldness of their sex' $em a dated, non-Christian primary source contemporary with the Gospels." }
'mishnah-women-witnesses-exceptions' = @{ title="A real but not absolute restriction"; body="Mishnah Shevuot 4:1 and Rosh Hashanah 1:8; Babylonian Talmud, Shevuot 30a and Gittin 46a. The rabbinic sources restrict women's formal legal testimony, reasoning that a woman's sphere is properly the home rather than the court, but the restriction was never absolute $em later halakhic analysis documents recognized exceptions where women's testimony was accepted on matters within their own direct experience." }
'empty-tomb-synoptic-divergence' = @{ title="Four accounts, four different headcounts"; body="Mark 16:1-8; Matthew 28:1-8; Luke 24:1-12; John 20:1-18, compared directly against one another. The four canonical resurrection-discovery accounts differ, checkably, on the number and names of the women present and on the number of angelic figures encountered." }
'embarrassment-criterion-scholarship' = @{ title="A real historiographical tool, and a real dispute over its weight"; body="John P. Meier, A Marginal Jew: Rethinking the Historical Jesus, vol. 1 (New York: Doubleday, 1991), formalizing the 'criterion of embarrassment'; N.T. Wright, The Resurrection of the Son of God (Minneapolis: Fortress Press, 2003); Dale C. Allison Jr., Resurrecting Jesus (London: T&T Clark, 2005). Contrast Bart Ehrman's and John Dominic Crossan's skepticism toward how much weight the criterion can bear; a widely cited poll of historical-Jesus specialists found roughly 53 percent accepting the empty tomb as historical." }
'emmaus-location-debate' = @{ title="Three candidate sites, no settled answer"; body="Eusebius of Caesarea, Onomasticon (4th century); Codex Sinaiticus's reading of '160 stadia' versus the majority manuscript reading of '60 stadia' at Luke 24:13; Adriaan Reland, Palaestina ex Monumentis Veteribus Illustrata (1714). The 60-versus-160-stadia manuscript discrepancy is the direct textual root of the modern site-identification controversy between Emmaus Nicopolis, Abu Ghosh, and el-Qubeibeh, which remains genuinely open among specialists." }
'emmaus-nicopolis-archaeology' = @{ title="Real archaeology, not confirmation of the specific event"; body="L.-H. Vincent and F.-M. Abel, Emmaus, sa basilique et son histoire (Paris: Gabalda, 1932). Reports the 1924-1930 excavation of two basilicas at Imwas/Emmaus-Nicopolis, including a fifth-century Byzantine basilica with mosaic floors and a cruciform baptismal font, and a third-century Roman bathhouse $em confirming continuous habitation but not the specific Gospel event." }
'anti-docetic-fish-eating' = @{ title="The same physicality argument, deployed within a generation"; body="Ignatius of Antioch, Epistle to the Smyrnaeans, ch. 3 (c. 107-110 CE). Quotes a resurrection-appearance tradition, 'lay hold, handle me, and see that I am not an incorporeal spirit,' functionally parallel to Luke 24:39-43 and explicitly deployed against docetic teaching that denied Jesus a real physical body." }
'jewish-bodily-resurrection-vs-greek-soul' = @{ title="Embodied resurrection was the load-bearing Jewish category"; body="N.T. Wright, The Resurrection of the Son of God (Minneapolis: Fortress Press, 2003). Argues mainstream Second Temple Jewish, especially Pharisaic, expectation was embodied resurrection against a Greco-Roman philosophical default that denied bodily resurrection outright, though real overlap and hybridization existed between the two traditions." }
'western-non-interpolations-luke24' = @{ title="A century-long textual-critical debate over the ascension's wording"; body="B. F. Westcott and F. J. A. Hort, The New Testament in the Original Greek (Cambridge/London: Macmillan, 1881). Identified nine short passages, eight in Luke 22-24, that the earliest manuscripts they judged best either omit or shorten; four occur in Luke 24, including 24:51's 'and was carried up into heaven,' missing from the original hand of Codex Sinaiticus before a later corrector added it. The 1961 publication of Papyrus 75 sided with the fuller text, and later translations restored the phrases to the main text." }
}

# ---- Chapter beats ----
$beat1 = @"
At first light on the first day of the week, Mary Magdalene, Joanna, Mary the mother of James, and "the other women with them" carry spices to a tomb they expect to find sealed (24:1, 24:10). They find the stone rolled back and the body gone; two men in dazzling clothing tell them Jesus has risen; they run to tell the eleven, who dismiss the report as an idle tale (24:11).

That the discovery narrative's first witnesses in all four gospels are women is not incidental, and it has become one of the most durable arguments historians make about how this story originated. Flavius Josephus, writing within living memory of the events Luke describes, states the operative social fact directly: "let not the testimony of women be admitted, on account of the levity and boldness of their sex" [[NOTE:josephus-women-testimony]]. The Mishnah preserves a parallel, though less absolute, restriction, with real documented exceptions for matters women alone would have directly witnessed [[NOTE:mishnah-women-witnesses-exceptions]].

This is the historical spine behind what scholars call the "criterion of embarrassment": a detail awkward for a storyteller to invent is, other things equal, more likely to reflect authentic memory than free invention [[NOTE:embarrassment-criterion-scholarship]]. N.T. Wright and Dale Allison both give the women-as-first-witnesses detail real evidentiary weight on exactly these grounds. Bart Ehrman and John Dominic Crossan push back, not merely as reflexive skeptics: reporting a find informally to one's own household is not the same transaction as testifying before a court, so the argument may prove less than its proponents want. What the argument can responsibly establish is something about likely narrative origin, not confirmation of a supernatural event.

The four gospels also disagree, precisely and checkably, about who went to the tomb and what they found. Mark names three women meeting a single young man; Matthew narrows the list to two, with one angel; Luke names three women plus an unspecified group of others, and doubles the messenger count to two men; John narrows the scene to Mary Magdalene alone, who eventually encounters two angels [[NOTE:empty-tomb-synoptic-divergence]]. Historians increasingly read this pattern less as contradiction demanding harmonization than as a fingerprint of independent oral tradition.
"@

$beat2 = @"
That same day, two disciples walk from Jerusalem to a village called Emmaus, "about sixty stadia" away -- roughly seven miles (24:13). One is named Cleopas (24:18); the other goes unnamed. A stranger falls in with them, expounds the scriptures concerning himself, and is recognized only in the breaking of bread, after which he vanishes (24:28-35).

Where, precisely, is Emmaus? This has not been a settled question for a very long time. Emmaus Nicopolis, roughly nineteen miles from Jerusalem, was identified as Luke's Emmaus at least as early as Eusebius in the fourth century -- and Codex Sinaiticus itself reads "160" rather than "60" at 24:13, a manuscript variant that maps suspiciously well onto Nicopolis's actual distance [[NOTE:emmaus-location-debate]]. Abu Ghosh and el-Qubeibeh, by contrast, sit almost exactly at the seven-mile distance most manuscripts give, but neither carries an ancient identification with Emmaus. Emmaus Nicopolis has, at least, produced real archaeology regardless of whether it is Luke's village: French Dominican excavators dug two basilicas there between 1924 and 1930, uncovering a fifth-century Byzantine basilica built explicitly to commemorate the resurrection-appearance tradition [[NOTE:emmaus-nicopolis-archaeology]] -- confirming a real, continuously inhabited town at the site the earliest identifiable tradition names, not confirmation that a specific first-century encounter happened on its road.
"@

$beat3 = @"
Back in Jerusalem, Jesus stands among the assembled disciples; they are, in the text's own word, terrified, supposing they see a spirit (24:37). He answers the fear with two deliberately physical proofs: "handle me, and see; for a spirit does not have flesh and bones as you see I have" (24:39), then eats a piece of broiled fish in front of them (24:41-43).

Why fish, and why insist on flesh and bones at all? The scene is doing real conceptual work against a live first-century alternative. Mainstream Second Temple Jewish thought, especially Pharisaic expectation, overwhelmingly anticipated resurrection as a restored, embodied life, not the soul's bare survival apart from a body, which sat closer to the Greek philosophical default [[NOTE:jewish-bodily-resurrection-vs-greek-soul]]. The strategy did not stay theoretical: within a generation, the bishop Ignatius of Antioch, writing against teachers who denied Jesus had a real physical body, quotes what is functionally the same tradition almost verbatim, adding that the risen Jesus ate and drank with his followers as proof of real flesh [[NOTE:anti-docetic-fish-eating]].
"@

$beat4 = @"
Luke closes his gospel at Bethany: Jesus lifts his hands, blesses the disciples, and -- in most modern Bibles -- "was carried up into heaven" (24:51); the disciples return to Jerusalem "with great joy" (24:52).

Except that several of those clauses are exactly the ones textual critics have argued over for more than a century. In 1881, B. F. Westcott and F. J. A. Hort identified nine short passages -- all but one clustered in Luke's final three chapters -- that the earliest manuscripts they judged best either omit or shorten, calling these "Western non-interpolations." Luke 24 alone supplies four of the nine: Peter's solo run to the tomb at 24:12 is missing from Bezae and the Old Latin manuscripts; "Peace be with you" at 24:36 is absent from the same witnesses; the demonstration of hands and feet at 24:40 is likewise missing; and "and was carried up into heaven" at 24:51 is missing not only from Bezae and the Old Latins but from the original hand of Codex Sinaiticus itself, before a later corrector added the phrase back in [[NOTE:western-non-interpolations-luke24]].

The scholarly assessment has moved, but the underlying fact hasn't disappeared. Westcott and Hort's theory carried enough weight that the Revised Standard Version relegated these phrases to footnotes, including the ascension clause. The 1961 publication of the third-century Papyrus 75, which sides with the fuller text at these points, undercut the theory's premise, and later translations restored the disputed phrases to the main text. Current mainstream text-critical opinion leans toward keeping "carried up into heaven" as original, but the fact is worth sitting with regardless of which reading one accepts: for decades, working from the best evidence then available, serious textual scholars concluded that Luke's own hand may not have described a visible, upward-motion ascension at all.
"@

$beats = @($beat1, $beat2, $beat3, $beat4)

# ---- New glossary entries ----
$glossary = [ordered]@{
'CLEOPAS' = "One of the two disciples encountered by the risen Jesus on the road to Emmaus (24:18); the only one of the pair Luke names, and otherwise unattested outside this pericope."
'EMMAUS' = "Village named as the destination of the road encounter in Luke 24:13-35, `"about sixty stadia`" from Jerusalem in most manuscripts. Its precise identity is disputed among three candidate sites, a debate rooted in a genuine manuscript disagreement over the stated distance itself [[NOTE:emmaus-location-debate]]."
'CODEX BEZAE' = "Fifth-century Greek-Latin diglot manuscript, the primary witness to the `"Western`" New Testament text-type; key to the `"Western non-interpolations`" debate over several shorter readings in Luke 24 [[NOTE:western-non-interpolations-luke24]]."
'CODEX SINAITICUS' = "Fourth-century manuscript, one of the two oldest surviving complete Greek New Testaments; its original hand omits `"and was carried up into heaven`" at Luke 24:51, a reading later added by a corrector [[NOTE:western-non-interpolations-luke24]]."
'PAPYRUS 75 (P75)' = "Third-century New Testament manuscript whose 1961 publication sided with the longer text at several disputed Luke 24 readings, undermining the premise of the `"Western non-interpolations`" theory [[NOTE:western-non-interpolations-luke24]]."
'WESTERN NON-INTERPOLATIONS' = "Term coined by B. F. Westcott and F. J. A. Hort (1881) for nine short New Testament passages, eight of them in Luke 22-24, that the earliest manuscripts they judged most reliable omit or shorten relative to the majority text [[NOTE:western-non-interpolations-luke24]]."
'CRITERION OF EMBARRASSMENT' = "Historical-Jesus research tool, formalized by John P. Meier, holding that details awkward or counterproductive for early Christian storytellers to invent are more likely to reflect authentic memory [[NOTE:embarrassment-criterion-scholarship]]."
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
    Add-BeatNode $Ch24NodeId $id $sortKey
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
Try-Append "MARY MAGDALENE" "This chapter adds: named first among the empty-tomb witnesses in all four canonical gospels (24:1-12 and parallels); central figure in the `"argument from embarrassment`" discussion [[NOTE:embarrassment-criterion-scholarship]]." $slugToNumber
Try-Append "JOANNA" "This chapter adds: named in Luke's empty-tomb witness list (24:10) but absent from the parallel lists in Mark and Matthew -- a specific, checkable synoptic-divergence data point [[NOTE:empty-tomb-synoptic-divergence]]." $slugToNumber
Try-Append "FLAVIUS JOSEPHUS" "This chapter adds: Antiquities of the Jews 4.8.15 (=4.219), his explicit statement on the inadmissibility of women's legal testimony, cited as the independent-record backdrop to the empty-tomb `"argument from embarrassment`" (24:1-12) [[NOTE:josephus-women-testimony]]." $slugToNumber

# ---- Seed new entities ----
# "Cleopas", "John P. Meier", and "Dale C. Allison Jr." already exist in the entity catalog -- not reseeded here.
Seed-Entity "Emmaus" "emmaus" "place" "Disputed village site of the post-resurrection road appearance in Luke 24:13-35."
Seed-Entity "Adriaan Reland" "adriaan-reland" "character" "Early 18th-century Dutch scholar who first corrected the Abu Ghosh/Qubeibeh identification of Emmaus in print (1714)."

$conn.Close()
Write-Host "DONE Chapter 24. LUKE COMPLETE."
