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
$Ch21NodeId = [guid]"019FA96A-D7AB-79F9-B44B-5930429E810C"
$maxNoteSortKey = [double](Exec-Scalar "SELECT ISNULL(MAX(bn.SortKey),0) FROM BeatNodes bn WHERE bn.NodeId='019FA96B-18E2-7BB4-BAEB-11ACA24934F4'")
$maxGlossarySortKey = [double](Exec-Scalar "SELECT ISNULL(MAX(bn.SortKey),0) FROM BeatNodes bn WHERE bn.NodeId='019FA96B-29F5-7BB9-99D0-0F787960E530'")
$maxNoteNumber = [int](Exec-Scalar "SELECT MAX(CAST(LEFT(b.Text, CHARINDEX(' ',b.Text)-1) AS INT)) FROM BeatNodes bn JOIN Beats b ON bn.BeatId=b.Id WHERE bn.NodeId='019FA96B-18E2-7BB4-BAEB-11ACA24934F4'")
Write-Host "Starting: MaxNumber=$script:MaxNumber MaxNoteSortKey=$maxNoteSortKey MaxNoteNumber=$maxNoteNumber MaxGlossarySortKey=$maxGlossarySortKey"

$em = [char]8212

# ---- Notes ----
$notes = [ordered]@{
'lepton-half-prutah-value' = @{ title="The smallest coin in circulation"; body="Ya'akov Meshorer, Ancient Jewish Coinage, Vol. 2: Herod the Great Through Bar Cochba (New York: Amphora Books, 1982). Establishes the lepton as a bronze coin roughly half the weight and value of the prutah, the smallest denomination struck in Judea; types first minted under Alexander Jannaeus (103-76 BCE) remained in circulation, worn down, into the first century CE." }
'mark-quadrans-gloss-luke-omission' = @{ title="A gloss Mark supplies and Luke drops"; body="Frederick William Danker, ed., A Greek-English Lexicon of the New Testament (BDAG), 3rd ed. (Chicago: University of Chicago Press, 2000), s.v. kodrantes. Confirms kodrantes (Latin quadrans) as a Roman copper coin equal to 1/64 of a denarius; Mark 12:42 uses this term to convert the Judean lepta into a value benchmark for its readers, a gloss Luke's parallel (21:2) omits, consistent with Luke assuming a Greek-comfortable readership." }
'shofar-chests-mishnah-shekalim' = @{ title="Thirteen collection chests, independently attested"; body="Mishnah Shekalim 6:5, trans. Herbert Danby (Oxford: Oxford University Press, 1933). Describes thirteen shofar-shaped collection chests in the Temple treasury, each earmarked for a specific category of offering, deliberately narrow at the neck so no one could reach back in and remove a deposited coin." }
'claudius-famine-suetonius-tacitus' = @{ title="Independent Roman corroboration of a real famine"; body="Suetonius, The Twelve Caesars: 'Claudius' 18.2; Tacitus, Annals 12.43. Both Roman writers, independently of each other and of any Christian source, record recurring grain shortages during Claudius's reign (41-54 CE), with Tacitus describing a crisis narrowly averted by favorable weather." }
'josephus-judean-famine-helena' = @{ title="Josephus corroborates the same famine from the Jewish side"; body="Josephus, Jewish Antiquities 20.51-53 and 20.101. Records a severe famine in Judea circa 45-47 CE, relieved substantially by Queen Helena of Adiabene's grain and dried-fig shipments purchased in Alexandria and Cyprus $em the episode most often identified with the famine referenced in Acts 11:28." }
'josephus-circumvallation-wall' = @{ title="Eyewitness testimony to the actual siege"; body="Josephus, The Jewish War 5.499-511. As an eyewitness participant, Josephus describes Titus ordering a full wall of circumvallation around Jerusalem $em thirty-nine stadia long, reinforced by thirteen forts, sealing every avenue of escape or resupply $em built by Roman work-crews in three days, a pace Josephus himself flags as remarkable." }
'luke-post70-dating-scholarship' = @{ title="Plain siege language replacing apocalyptic code"; body="Joseph A. Fitzmyer, The Gospel According to Luke I-IX, Anchor Bible 28 (Garden City, NY: Doubleday, 1981), Introduction. Mainstream historical-critical scholarship generally dates Luke to roughly 80-85 CE, after Mark, and reads Luke's replacement of Mark's cryptic 'abomination of desolation' with plain military language ('surrounded by armies,' 21:20) as evidence of a writer composing with the actual 70 CE siege already in view." }
'robinson-early-dating-counter' = @{ title="A minority counter-argument for earlier composition"; body="John A. T. Robinson, Redating the New Testament (London: SCM Press, 1976). Argues, against the scholarly majority, that the Synoptics' silence about the Temple's fall having actually happened -- recording only the forecast, never a fulfilled-past-tense notice -- suggests composition before 70 CE; a minority position, but the standard counter-argument to the vaticinium ex eventu reading." }
}

# ---- Chapter beats ----
$beat1 = @"
Luke sets the scene without ceremony: Jesus, watching the Temple's collection boxes, sees rich donors dropping in their gifts, then a poor widow who contributes two lepta -- "two mites" in the old translations (21:1-4). The coin itself is not incidental color. The lepton was the smallest bronze denomination struck in first-century Judea, roughly half the weight and value of the more common prutah, a holdover type first minted under the Hasmonean king Alexander Jannaeus and still circulating, worn thin, a century later [[NOTE:lepton-half-prutah-value]]. Mark's parallel account of this same episode (12:41-44) glosses the coin's value for readers who might not know Judean currency, adding that two lepta "make a kodrantes" -- a Roman quadrans, the smallest Roman monetary unit [[NOTE:mark-quadrans-gloss-luke-omission]]. Luke, writing for an audience already comfortable with Greek-world coin talk, drops that conversion -- a small but real editorial fingerprint, not a contradiction. The collection mechanism itself is independently attested outside the Gospels: the Mishnah describes thirteen shofar-shaped chests in the Temple treasury, deliberately narrow at the neck so a hand could not be slipped back out once coins went in [[NOTE:shofar-chests-mishnah-shekalim]]. This detail was already established when this same pericope appeared in Mark's account earlier in this book; Luke simply confirms it from a second angle.
"@

$beat2 = @"
Jesus then predicts, to onlookers admiring the Temple's stonework, that "there shall not be left one stone upon another" (21:5-9). This is the same prophecy already traced at length in this book's treatment of Luke 13 and Luke 19 -- the vaticinium ex eventu question, whether the historical Jesus predicted the Temple's fall in general terms or whether the specificity in the text reflects a writer composing after the event. Nothing in verses 5-9 by itself sharpens that argument; the language here stays broad enough to fit either reading. The sharper evidence comes two pericopes later.
"@

$beat3 = @"
Luke's Jesus continues with warnings of persecution and betrayal, and, among the catalogue of coming disasters, "great earthquakes, and in divers places famines and pestilences" (21:10-19). The famine detail is worth pausing on, because one specific famine from this general period is unusually well corroborated outside the New Testament. Acts 11:28 records the prophet Agabus foretelling a great dearth "in the days of Claudius" -- and a famine centered on Judea in the mid-40s CE is independently attested by three separate ancient sources with no theological stake in the story. Suetonius and Tacitus both record repeated grain shortages during Claudius's reign [[NOTE:claudius-famine-suetonius-tacitus]]. Josephus independently describes a severe famine in Judea spanning roughly 45-47 CE, relieved substantially by Queen Helena of Adiabene's grain shipments [[NOTE:josephus-judean-famine-helena]]. That is a genuinely rare convergence: a Roman biographer, a Roman senator-historian, and a Jewish historian, none citing each other or the Gospels, all independently attesting real famine conditions in almost exactly the window Acts assigns to Agabus's prophecy. It does not prove Luke 21:11's generic "famines" clause is a reference to this specific event -- the language is stock apocalyptic catalogue -- but it confirms the kind of catastrophe listed was not invented scenery.
"@

$beat4 = @"
Then comes the passage's most specific military language: "when ye shall see Jerusalem compassed with armies, then know that the desolation thereof is nigh" -- armies that will lay the city level, its people falling by the sword or led captive, Jerusalem "trodden down of the Gentiles" (21:20-24). This is worth setting directly against Mark's parallel (13:14), which instead has the cryptic phrase "the abomination of desolation," borrowed from Daniel -- apocalyptic code, not military reportage. Mainstream historical-critical scholarship reads this substitution as a meaningful editorial choice: Luke, writing after Mark, converts an ambiguous apocalyptic cipher into a description that reads like straightforward hindsight [[NOTE:luke-post70-dating-scholarship]]. And the hindsight, if that is what it is, matches the historical record closely. Josephus -- present at the siege -- describes Titus's forces building a complete wall of circumvallation around Jerusalem, thirty-nine stadia long, studded with thirteen forts, completed in just three days [[NOTE:josephus-circumvallation-wall]]. "Compassed with armies" is, point for point, what actually happened at Jerusalem in the spring of 70 CE. The counter-case is not absent from the record, though it remains a minority position: John A.T. Robinson argued that the Synoptics' complete silence about the Temple actually having fallen suggests the Gospels were finished before 70 CE [[NOTE:robinson-early-dating-counter]]. This remains an open question in the strict sense: no manuscript, inscription, or external witness settles definitively whether Luke wrote before or after watching a Roman legion do exactly what his text describes.
"@

$beat5 = @"
The chapter's final apocalyptic material -- signs in sun, moon, and stars, the Son of Man coming in a cloud, framed by the parable of the fig tree and capped with the notoriously debated line "this generation shall not pass away, till all be fulfilled" (21:25-33) -- is cosmic and theological rather than checkable in the archaeological sense. Nothing here names a place, artifact, or dated event this book's method can independently verify; "this generation" has been read literally, figuratively, and generationally-elastically by commentators across two millennia, and that interpretive question belongs to theology, not to the historical record this book restricts itself to.
"@

$beat6 = @"
Luke closes the discourse with a plain ethical warning against being caught unprepared, followed by the narrative note that Jesus spent his days teaching in the Temple and his nights on the Mount of Olives, drawing crowds who came early each morning to hear him (21:34-38). The Mount of Olives as Jesus's overnight base is consistent with the topography already established earlier in this book's account of the triumphal entry.
"@

$beats = @($beat1, $beat2, $beat3, $beat4, $beat5, $beat6)

# ---- New glossary entries ----
$glossary = [ordered]@{
'CLAUDIUS FAMINE' = "A famine affecting Judea and other parts of the Roman world during the reign of Emperor Claudius (c. 45-47 CE), independently attested by Suetonius, Tacitus, and Josephus; relieved in Jerusalem by Queen Helena of Adiabene's grain shipments [[NOTE:claudius-famine-suetonius-tacitus]] [[NOTE:josephus-judean-famine-helena]]. Widely identified as the background event behind Agabus's prophecy in Acts 11:28."
'CIRCUMVALLATION WALL (SIEGE OF JERUSALEM, 70 CE)' = "A 39-stadia siege wall studded with thirteen forts, built by Titus's Roman forces around Jerusalem in three days per Josephus's eyewitness account, sealing the city to starve its defenders [[NOTE:josephus-circumvallation-wall]]. The closest independent, dated military correlate to Luke 21:20's `"compassed with armies`" language."
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
    Add-BeatNode $Ch21NodeId $id $sortKey
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

# ---- Seed new entities ----
Seed-Entity "Helena of Adiabene" "helena-of-adiabene" "character" "Queen whose grain and dried-fig shipments relieved the mid-40s CE Judean famine."
Seed-Entity "Cuspius Fadus" "cuspius-fadus" "character" "Roman procurator of Judea during whose term Josephus dates the onset of the mid-40s CE famine."
Seed-Entity "Tiberius Julius Alexander" "tiberius-julius-alexander" "character" "Roman procurator of Judea whose term overlapped the mid-40s CE famine period."

$conn.Close()
Write-Host "DONE Chapter 21."
