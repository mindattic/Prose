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
$Ch14NodeId = [guid]"019FA96A-5DD7-7E47-8245-15A9F5A3CFC4"
$maxNoteSortKey = [double](Exec-Scalar "SELECT ISNULL(MAX(bn.SortKey),0) FROM BeatNodes bn WHERE bn.NodeId='019FA96B-18E2-7BB4-BAEB-11ACA24934F4'")
$maxGlossarySortKey = [double](Exec-Scalar "SELECT ISNULL(MAX(bn.SortKey),0) FROM BeatNodes bn WHERE bn.NodeId='019FA96B-29F5-7BB9-99D0-0F787960E530'")
$maxNoteNumber = [int](Exec-Scalar "SELECT MAX(CAST(LEFT(b.Text, CHARINDEX(' ',b.Text)-1) AS INT)) FROM BeatNodes bn JOIN Beats b ON bn.BeatId=b.Id WHERE bn.NodeId='019FA96B-18E2-7BB4-BAEB-11ACA24934F4'")
Write-Host "Starting: MaxNumber=$script:MaxNumber MaxNoteSortKey=$maxNoteSortKey MaxNoteNumber=$maxNoteNumber MaxGlossarySortKey=$maxGlossarySortKey"

$em = [char]8212

# ---- Notes ----
$notes = [ordered]@{
'luke14-dropsy-hippocratic' = @{ title="Dropsy: a real ancient diagnostic category"; body="Alain Touwaide and Natale Gaspare De Santo, 'Edema in the Corpus Hippocraticum,' American Journal of Nephrology 19, no. 2 (1999): 155-158. Establishes that the Hippocratic Corpus recognizes symptoms of edema under the terms oidema (swelling) and hydrops/hydrops (dropsy), explained through the era's own qualities/humors framework rather than modern cardiac, renal, or hepatic etiology. Confirms 'dropsy' as a genuine ancient Greek diagnostic category, not an invented or purely biblical term." }
'luke14-banquet-seating' = @{ title="Seating rank as a real, contested social marker"; body="Plutarch, Quaestiones Convivales (Table Talk), Book 1, chapter 2; Dennis E. Smith, From Symposium to Eucharist: The Banquet in the Early Christian World (Minneapolis: Fortress Press, 2003). Plutarch treats banquet seating assignment as a genuinely contested matter of etiquette; Smith documents that both Greco-Roman and Jewish banquets used rank-based seating as a real, socially visible marker of status, grounding Luke 14:7-11 in documented period custom." }
'luke14-resurrection-debate' = @{ title="A doctrinal split independently corroborated twice over"; body="Josephus, Antiquities of the Jews 18.14, 18.16; Jewish War 2.163-165; Acts 23:6-8. Josephus reports the Pharisees held the soul immortal and affirmed revival after death, while the Sadducees held souls die with the body; Acts independently records the identical doctrinal split disrupting the Sanhedrin when Paul invokes 'the resurrection of the dead.' Two independent literatures corroborate the same internal Second Temple Jewish theological divide without either depending on the other." }
'luke14-newlywed-exemption' = @{ title="A real legal exemption, borrowed for a smaller excuse"; body="Deuteronomy 24:5. Exempts a newly married man from military conscription and public duty for a full year. The Luke 14 banquet excuse is not a literal citation of this law, but trades on the cultural recognition that marriage carried real, legally sanctioned priority claims over civic obligation for a defined season $em precisely what makes the excuse land as a poor one inside the parable's own logic." }
'luke14-hate-idiom' = @{ title="'Hate' as Semitic comparative idiom, not literal animosity"; body="Stephen Robert Llewelyn and Will Robinson, 'Hyperbole and the Cost of Discipleship: A Case Study of Luke 14:26,' Harvard Theological Review 116, no. 1 (2023): 44-65. Establishes the mainstream critical reading of miseo ('hate') in Luke 14:26 as Semitic comparative hyperbole for lesser relative priority, cross-confirmed by identical usage describing Leah as 'hated' in Genesis 29:30-31 and by Matthew's parallel rendering as 'loveth... more than me' (10:37)." }
'luke14-cost-planning' = @{ title="Real advance cost-and-strength reckoning"; body="Papers in Honour of Janet DeLaine, From Concept to Monument: Time and Costs of Construction in the Ancient World (Oxford: Archaeopress, 2023); Jonathan P. Roth, The Logistics of the Roman Army at War, 264 B.C.-A.D. 235. Establishes that advance reckoning of construction cost and advance strategic assessment of relative troop strength before battle were real, documented planning practices in the Roman-era Mediterranean, grounding the tower-building and war-council images of Luke 14:28-32 in genuine period practice." }
'luke14-salt-savor-chemistry' = @{ title="Impure salt really can go flavorless"; body="Pliny the Elder, Natural History, Book 31, chapters 39 and 41. Confirms first-century Levantine salt, sourced largely from Dead Sea evaporation and inland deposits such as Mount Sodom's halite mass, was typically an impure mineral mixture rather than pure sodium chloride; the genuinely soluble NaCl fraction could leach out through moisture or damp storage, leaving a flavorless mineral residue $em a real, checkable material basis for Luke 14:34-35, distinct from the separate fact that pure NaCl itself cannot lose its saltiness." }
}

# ---- Chapter beats ----
$beat1 = @"
Luke opens this chapter with Jesus back at a Pharisee's table on the Sabbath, and the meal itself is doing narrative work before anyone says a word: "when he went into the house of one of the chief Pharisees to eat bread on the sabbath day" (14:1). A man present has "the dropsy" (14:2), Jesus asks the lawyers and Pharisees outright whether it is lawful to heal on the Sabbath, heals him, and answers the ensuing silence with the familiar ox-or-donkey-in-a-pit argument (14:3-6). This is the third and final Sabbath-healing controversy in Luke's Gospel -- after the man with the withered hand (6:6-11) and the bent-over woman (13:10-17) -- and the pattern is now familiar rather than new.

What is worth pausing on is "dropsy" itself, because it names something checkable that isn't just theological color. The term translates the Greek hydropikos, "having hydrops," and hydrops is a genuine, well-attested ancient Greek medical category, not an invented biblical curiosity [[NOTE:luke14-dropsy-hippocratic]]. The Hippocratic Corpus discusses hydrops and the related term oidema (swelling) as recognized clinical presentations, explained within the era's own physiological framework rather than in terms of the heart, kidney, or liver pathology a modern clinician would suspect. "Dropsy" survived into English medical usage as late as the nineteenth century as a catch-all label for generalized edema; modern medicine dissolved it into its actual causes and no longer uses the word as a diagnosis at all. So the Gospel's detail is neither anachronistic nor decorative -- it names a real, ancient, and eventually superseded diagnostic category.
"@

$beat2 = @"
The chief Pharisee's dinner table now becomes the occasion for two further teachings, framed around where people choose to sit -- which is not an invented custom for narrative convenience. Jesus, watching guests angle for "the highest room," warns that presuming to the best seat risks public demotion, and counsels taking the lowest place instead so the host can promote you upward in front of everyone (14:7-11). This is a real, well-documented feature of Greco-Roman dining: Plutarch devotes a section of his Table Talk to exactly this question, arguing that leaving seating to chance is as absurd as serving courses in no particular order, because "the best man should have the best place" [[NOTE:luke14-banquet-seating]]. Jewish banquets of the period operated under a parallel logic of honor-coded seating. Jesus is not inventing an etiquette rule; he is taking a real, high-stakes social practice and inverting its usual reward structure.

The second half of the table-teaching turns to guest lists: invite the poor and the maimed rather than kin who can repay the favor, "for thou shalt be recompensed at the resurrection of the just" (14:12-14). That phrase drops into a real and specific fault line inside Second Temple Judaism, independently checkable from two directions at once. Josephus reports that the Pharisees held "every soul is imperishable" while the Sadducees held that "souls die with the bodies" [[NOTE:luke14-resurrection-debate]]. Acts corroborates this from the inside: Paul before the Sanhedrin invoking "the hope of the resurrection of the dead" splits the assembly along exactly this line (23:6-8). Two independent bodies of literature describe the identical doctrinal split without either citing the other -- about as good as ancient corroboration gets for an internal theological debate.
"@

$beat3 = @"
A guest's comment about "eating bread in the kingdom of God" (14:15) prompts the parable of the great supper: invited guests all beg off -- one has just bought a field, another five yoke of oxen, a third has just married and "therefore I cannot come" (14:16-20). The angry host fills the house with the poor and the maimed instead (14:21-24). The field and oxen excuses read as ordinary agrarian business, but the marriage excuse lands on a specific, textually attested legal provision: Deuteronomy 24:5 exempts a newly married man from military conscription and public duty for a full year [[NOTE:luke14-newlywed-exemption]]. The parable is not literally about a military draft, but invoking marriage to dodge a banquet borrows the moral weight of an exemption scripture reserves for something far graver -- precisely what makes the excuse land as a poor one inside the parable's own logic.
"@

$beat4 = @"
Great crowds now follow Jesus, and he turns to the cost of what following him requires: hating father, mother, and "his own life also," bearing one's cross (14:25-27, 33). The verb translated "hate" (Greek miseo) is textbook Semitic comparative idiom rather than a call to literal animosity -- the mainstream critical-scholarly position, argued at length in a 2023 Harvard Theological Review study [[NOTE:luke14-hate-idiom]]. The same idiom appears in the Hebrew Bible itself: Genesis 29:30-31 calls Leah "hated" only in contrast to Jacob's greater love for Rachel. Matthew's parallel removes any ambiguity, rendering the same teaching as "he that loveth father or mother more than me" (10:37) -- confirming Luke's harsher wording and Matthew's comparative wording transmit the same instruction about relative priority, not two different doctrines about family.

Jesus grounds the demand in two illustrations of ordinary cost-calculation: a man reckoning whether he can afford to finish a tower, and a king assessing whether his forces can meet an approaching army (14:28-32). Both images draw on real, documented ancient planning practice. Major Roman-era construction required advance reckoning of labor and materials, and pre-battle assessment of relative troop strength was a documented and often decisive element of ancient warfare [[NOTE:luke14-cost-planning]]. Neither illustration needs a specific historical tower or battle behind it to be true to its world -- sober cost-counting before large undertakings was the ordinary, rational thing to do in the first-century Mediterranean.
"@

$beat5 = @"
The chapter closes on salt: "if the salt have lost his savour, wherewith shall it be seasoned?... men cast it out" (14:34-35). This is one of the rare cases where the text makes a claim modern chemistry can directly test, and the test complicates the plain reading in an interesting way. Pure sodium chloride is chemically stable -- it cannot "lose its saltiness" through any ordinary process. But the salt actually available in first-century Judea and Galilee was not pure refined table salt; it came largely from Dead Sea evaporates and inland deposits like the halite formations of Mount Sodom, and Pliny the Elder's Natural History attests that ancient salt was routinely a mixed mineral product, contaminated with gypsum and other insoluble residues [[NOTE:luke14-salt-savor-chemistry]]. In such an impure block, the genuinely soluble NaCl could leach out over time through moisture or damp storage, leaving behind a visually similar but flavorless mineral residue that looks like salt and no longer tastes like anything. So the saying describes a real, physically observable failure mode of the specific, impure, regionally sourced salt an ordinary Galilean listener would have handled.
"@

$beats = @($beat1, $beat2, $beat3, $beat4, $beat5)

# ---- New glossary entries ----
$glossary = [ordered]@{
'THE SADDUCEES' = "Second Temple Jewish sect, priestly-aristocratic in composition, that denied bodily resurrection, angels, and spirits, in contrast to the Pharisees. Attested independently by Josephus (Antiquities 18.16; Jewish War 2.164-165) and by Acts 23:8, making the Pharisee-Sadducee resurrection split one of the best-corroborated points of internal Jewish theological diversity in this period [[NOTE:luke14-resurrection-debate]]. Luke 14:14's `"resurrection of the just`" assumes the Pharisaic side of this exact debate."
'DROPSY (HYDROPS)' = "Ancient Greek medical term (hydropikos/hydrops, from hydor, `"water`") for generalized bodily swelling from fluid retention, discussed as a recognized diagnostic category in the Hippocratic Corpus [[NOTE:luke14-dropsy-hippocratic]]. A real ancient clinical label that survived in English medical usage into the nineteenth century before being superseded by modern understanding of edema's specific causes."
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
    Add-BeatNode $Ch14NodeId $id $sortKey
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
Try-Append "THE PHARISEES" "This chapter adds: `"one of the chief Pharisees`" hosts Jesus for a Sabbath meal (14:1), the setting for both the dropsy healing and the banquet-humility teaching; also affirmed bodily resurrection per Josephus (Antiquities 18.14) [[NOTE:luke14-resurrection-debate]]." $slugToNumber
Try-Append "FLAVIUS JOSEPHUS" "This chapter cites his testimony on the Pharisee/Sadducee resurrection debate (Antiquities 18.14, 18.16; Jewish War 2.163-165) [[NOTE:luke14-resurrection-debate]]." $slugToNumber

# ---- Seed new entities ----
# "Hippocratic Corpus" already exists in the entity catalog (slug: hippocratic-corpus) -- not reseeded here.
Seed-Entity "Plutarch" "plutarch" "character" "Greek biographer-essayist whose Table Talk documents real Greco-Roman banquet seating-rank customs."

$conn.Close()
Write-Host "DONE Chapter 14."
