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
$Ch18NodeId = [guid]"019FA96A-A5E2-7CEA-A466-C3D869572B74"
$maxNoteSortKey = [double](Exec-Scalar "SELECT ISNULL(MAX(bn.SortKey),0) FROM BeatNodes bn WHERE bn.NodeId='019FA96B-18E2-7BB4-BAEB-11ACA24934F4'")
$maxGlossarySortKey = [double](Exec-Scalar "SELECT ISNULL(MAX(bn.SortKey),0) FROM BeatNodes bn WHERE bn.NodeId='019FA96B-29F5-7BB9-99D0-0F787960E530'")
$maxNoteNumber = [int](Exec-Scalar "SELECT MAX(CAST(LEFT(b.Text, CHARINDEX(' ',b.Text)-1) AS INT)) FROM BeatNodes bn JOIN Beats b ON bn.BeatId=b.Id WHERE bn.NodeId='019FA96B-18E2-7BB4-BAEB-11ACA24934F4'")
Write-Host "Starting: MaxNumber=$script:MaxNumber MaxNoteSortKey=$maxNoteSortKey MaxNoteNumber=$maxNoteNumber MaxGlossarySortKey=$maxGlossarySortKey"

$em = [char]8212

# ---- Notes ----
$notes = [ordered]@{
'widow-legal-vulnerability' = @{ title="A widow's documented legal risk"; body="Elisabeth M. Tetlow, 'The Status of Women in Greek, Roman and Jewish Society,' in Women and Ministry in the New Testament (New York: Paulist Press, 1980), 5-29. Jewish legal custom channeled litigation through male kinsmen, town elders, or a formally recognized advocate far more often than allowing a woman to sue independently; a widow without living sons or a kinsman-redeemer occupied a distinct category of legal risk." }
'hylen-widow-independence' = @{ title="Not every widow was helpless"; body="Susan E. Hylen, Women in the New Testament World (Oxford: Oxford University Press, 2018). Argues from Mediterranean legal papyri and inscriptions that the totally dependent widow is a largely false generalization; many widows became legally independent upon a father's or husband's death and could, and did, own property and initiate litigation on their own." }
'josephus-procurator-corruption' = @{ title="Routine bribery among Judea's Roman administrators"; body="Flavius Josephus, Antiquities of the Jews 20.9.5 (procurator Lucceius Albinus); Jewish War 2.14 (procurator Gessius Florus). Josephus describes Albinus releasing imprisoned criminals for payment and Florus extorting the population more openly and violently, naming Florus's misconduct as a proximate cause of the 66 CE revolt." }
'verres-corruption-repetundis' = @{ title="A standing Roman court for exactly this crime"; body="Cicero, In Verrem, delivered 70 BC; background statute: the lex Calpurnia de repetundis (149 BC), establishing Rome's first standing criminal court for prosecuting magistrate extortion and bribery. Cicero's prosecution of Gaius Verres, governor of Sicily, is the best-documented case; the court's two-century-plus existence is itself evidence the problem was systemic." }
'bensira-judges-warning' = @{ title="A Second Temple sage warns against judgeship itself"; body="Ben Sira (Sirach/Ecclesiasticus), composed c. 180-175 BC. Warns the reader against seeking the office of judge specifically because of the risk of being unable to root out injustice or of favoring the powerful $em direct Second Temple Jewish evidence that judicial partiality was a recognized occupational hazard well before the Gospels." }
'amidah-standing-prayer' = @{ title="Standing to pray was ordinary, not Pharisaic affectation"; body="Mishnah, Berakhot 1:3 and 5:1. The Amidah, Second Temple Judaism's core petitionary prayer, took its Hebrew name $em literally 'the Standing' $em from the fact that it was said standing; the Mishnah's one substantive first-century dispute over prayer posture concerns reclining versus standing for the Shema, not the Amidah." }
'roman-child-legal-status' = @{ title="A child's diminished legal standing"; body="Roman legal category of doli incapax as recorded in classical jurisprudence (Gaius, Institutes, 2nd century CE). Roman children remained under a father's near-total legal authority (patria potestas) and were categorically treated by jurists as incapable of independent legal or moral agency below an age threshold $em a broadly Mediterranean assumption about children's diminished status." }
'gundry-volf-children-status' = @{ title="Why the disciples' rebuke made social sense"; body="Judith Gundry-Volf, 'To Such as These Belongs the Reign of God: Jesus and Children,' Theology Today 56, no. 4 (2000): 469-480. Identifies childcare as low-status activity typically assigned to women or slaves in the first-century Mediterranean; the disciples' attempt to turn away the children expresses that status hierarchy, which Jesus deliberately inverts." }
'talmud-elephant-needle' = @{ title="A shared regional idiom of impossibility"; body="Babylonian Talmud, Berakhot 55b. Preserves a structurally identical hyperbolic saying using an elephant rather than a camel passing through a needle's eye, compiled roughly four centuries after the Gospels $em evidence of a shared regional idiom, not a literary source for the Gospel saying." }
'kamelos-kamilos-variant' = @{ title="Camel, not rope: the manuscripts are not close"; body="Bruce M. Metzger, A Textual Commentary on the Greek New Testament, 2nd ed. (Stuttgart: Deutsche Bibelgesellschaft / United Bible Societies, 1994), on Matthew 19:24/Mark 10:25/Luke 18:25. A minority of late Byzantine-tradition manuscripts read kamilos ('rope/cable') rather than kamelos ('camel'), a conjecture from the fifth-century bishop Cyril of Alexandria; the earliest and best manuscripts of all three Synoptic parallels read 'camel.'" }
'needles-eye-gate-myth' = @{ title="A medieval invention, not an attested Jerusalem gate"; body="Agnieszka Zieminska, 'The Origin of the `"Needle's Eye Gate`" Myth: Theophylact or Anselm?,' New Testament Studies 68, no. 3 (2022): 366-378. Traces the earliest attestation of the small-Jerusalem-gate explanation to an eleventh-century gloss attributed to Anselm of Canterbury, transmitted via Thomas Aquinas's Catena Aurea; no gate by this name or description is attested in any earlier, Second Temple, or archaeological source." }
'bartimaeus-naming-difference' = @{ title="Only Mark names him"; body="Direct synoptic comparison, Mark 10:46-52, Luke 18:35-43, Matthew 20:29-34; Joel Marcus, Mark 8-16: A New Translation with Introduction and Commentary (Anchor Yale Bible; New Haven: Yale University Press, 2009), ad loc. Only Mark names the healed beggar (Bartimaeus, son of Timaeus) and places the healing on departure from Jericho; Luke's text supplies no name and places the healing on approach into the city." }
'jericho-herodian-palace-netzer' = @{ title="Herod's winter palace, excavated"; body="Ehud Netzer, Hasmonean and Herodian Palaces at Jericho, Volume I: Stratigraphy and Architecture (Jerusalem: Israel Exploration Society, 2001). Ten excavation seasons beginning in 1973 documented a Hasmonean-through-Herodian palace complex at Tulul Abu el-Alayiq with mosaic floors, frescoes, and a sunken garden, in continuous use by Herod's family until its destruction in the 66-70 CE revolt." }
'pliny-josephus-balsam-jericho' = @{ title="A wealthy regional monopoly economy"; body="Pliny the Elder, Natural History 13.6-9; Flavius Josephus, Antiquities of the Jews 15.4.2 and Jewish War 4.459-475. Two independent, non-Christian classical sources corroborate Jericho's balsam and date crops as a valuable regional monopoly, supporting heavy traveler traffic on the Jerusalem-Jericho road." }
}

# ---- Chapter beats ----
$beat1 = @"
Jesus tells his disciples a parable about persistence in prayer: a widow with a grievance against an adversary appeals repeatedly to a city judge who "neither feared God nor cared what people thought," and gets no relief -- until, worn down by her refusal to stop asking, the judge grants her justice simply to be rid of her (18:1-8). The parable's logic depends on two pieces of realism doing real work underneath the metaphor: that a widow petitioning alone was a genuinely vulnerable position, and that a judge could refuse to rule for reasons having nothing to do with the merits of the case.

On the first point, Jewish legal custom did channel litigation through kinsmen, elders at the gate, or a formally recognized male advocate far more often than not, and a woman without living sons or a levir occupied a documented category of risk [[NOTE:widow-legal-vulnerability]]. But the mainstream historical-critical picture has been revised in a direction popular retelling hasn't caught up to: Susan Hylen's study of the wider Mediterranean evidence shows that many widows, once released from a father's or husband's legal authority, became legally independent property owners who could and did initiate their own suits [[NOTE:hylen-widow-independence]]. The "utterly helpless widow" is a real category some women fell into, not the whole picture.

The judge's venality is the other leg the parable stands on, and here the independent record is unambiguous. A century and a half before Jesus, the Second Temple sage Ben Sira warns a reader off the bench altogether, because judicial impartiality was known to be hard to keep [[NOTE:bensira-judges-warning]]. Josephus's own history of Judea under the last Roman procurators describes prison sentences bought and sold and taxes extorted as routine administrative practice [[NOTE:josephus-procurator-corruption]]. On the Greco-Roman side, Rome had maintained a standing criminal court, the quaestio de repetundis, since 149 BC for the specific purpose of prosecuting extortion by magistrates -- the court's mere existence for over two centuries is itself the strongest evidence that a corrupt judge was not a narrative flourish [[NOTE:verres-corruption-repetundis]].
"@

$beat2 = @"
Two men go up to the Temple to pray: a Pharisee, who thanks God he is not like "robbers, evildoers, adulterers" or the tax collector standing nearby, and recites his own credentials; and the tax collector, who stands at a distance and beats his chest asking only for mercy (18:9-14). Jesus's verdict -- the tax collector, not the Pharisee, goes home justified -- depends for its force on locating the offense correctly. It is not the posture: standing was the default position for the central petitionary prayer of Second Temple Judaism, the Amidah, literally "the Standing" [[NOTE:amidah-standing-prayer]]. Both men in Luke's scene stand; the fault is entirely in the content of what the Pharisee says. His boast that he fasts "twice a week" is not an invented detail -- this book has already traced its source to the Monday-and-Thursday supererogatory Pharisaic fast (Chapter 5); nothing here revises that finding.
"@

$beat3 = @"
People start bringing infants to Jesus for a blessing, and the disciples try to turn them away -- until Jesus rebukes the disciples instead (18:15-17). The historical background makes the disciples' irritation something closer to a coherent, status-conscious judgment -- mistaken, per Jesus, but not baseless. Across the wider Greco-Roman world, a child held a genuinely diminished legal and social standing: under Roman law a child remained under a father's near-total legal authority, and jurists treated children as categorically incapable of independent legal or moral agency below an age threshold [[NOTE:roman-child-legal-status]]. New Testament scholar Judith Gundry-Volf identifies childcare as low-status work typically assigned to women or slaves in this period, and reads the disciples' rebuke as a coherent expression of that status hierarchy -- one Jesus deliberately and pointedly inverts [[NOTE:gundry-volf-children-status]].
"@

$beat4 = @"
A "certain ruler" asks Jesus what he must do to inherit eternal life; Jesus tells him to sell everything and give it to the poor, and the man goes away grieving, being very rich (18:18-23). Jesus then generalizes: "it is easier for a camel to go through the eye of a needle than for a rich person to enter the kingdom of God" (18:24-25) -- a line that has attracted more folk explanation than almost any other single verse in the Gospels, most of it unreliable.

Start with what isn't in dispute at the manuscript level. A small number of very late Greek manuscripts read kamilos ("ship's rope") instead of kamelos ("camel"), a conjecture traceable to the fifth-century bishop Cyril of Alexandria; the earliest and best manuscripts across all three Synoptic parallels read "camel" [[NOTE:kamelos-kamilos-variant]]. Second: the camel-through-a-needle image likely draws on a shared regional idiom for "impossible" -- the Babylonian Talmud preserves comparable imagery using an elephant instead, though that text postdates the Gospels by roughly four centuries [[NOTE:talmud-elephant-needle]]. Third, and this is the one worth being skeptical about hardest: the popular sermon claim that "the eye of the needle" was a real, small pedestrian gate in Jerusalem's walls has no support in any Second Temple text, any archaeological find, or any source earlier than the medieval period. A 2022 study traces the claim's earliest attestation to an eleventh-century gloss attributed to Anselm of Canterbury [[NOTE:needles-eye-gate-myth]]. No wall, gate, or gap in Jerusalem's fortifications by this name is attested in any first-century source -- this is Legendary Accretion in a fairly pure form.
"@

$beat5 = @"
For the third time in Luke's narrative, Jesus tells the Twelve plainly what is coming in Jerusalem -- betrayal, mockery, flogging, death, and resurrection on the third day (18:31-34) -- and, as with the two earlier predictions, the disciples "understood none of these things." This iteration adds no new checkable claim beyond what's already established for the earlier predictions.
"@

$beat6 = @"
As Jesus approaches Jericho, a blind man begging by the road hears the crowd, calls out to "Jesus, Son of David," and is healed, told "your faith has saved you" (18:35-43). The name "Bartimaeus," often attached to this scene, is worth pausing on before going further, because it isn't actually in Luke's text. Mark's parallel account names him Bartimaeus, son of Timaeus, and places the healing as Jesus is leaving Jericho, not entering it; Luke's own Greek names no one and places the encounter on the approach into the city [[NOTE:bartimaeus-naming-difference]]. Any title using "Bartimaeus" for this Lukan scene is a harmonization with Mark, not a report of what Luke wrote.

Why would beggars gather on this particular stretch of road? First-century Jericho was one of the wealthiest small cities in Judea: Herod the Great built a winter palace complex there, excavated across ten seasons beginning in 1973, with mosaic floors, frescoes, and a sunken garden [[NOTE:jericho-herodian-palace-netzer]]. The surrounding oasis supported a lucrative agricultural economy built on balsam resin and dates; Pliny the Elder singles out Jericho's date palms and balsam for extended praise, and Josephus describes the balsam harvest [[NOTE:pliny-josephus-balsam-jericho]]. A city built around a royal winter residence and an export-monopoly crop would have carried heavy traffic of wealthy travelers -- exactly the kind of route economically rational for a beggar to work.
"@

$beats = @($beat1, $beat2, $beat3, $beat4, $beat5, $beat6)

# ---- New glossary entries ----
$glossary = [ordered]@{
'BARTIMAEUS' = "Blind beggar healed by Jesus near Jericho per Mark 10:46-52 (`"Bartimaeus, son of Timaeus`"). Not named in Luke's own account of the parallel healing (18:35-43), where the man is simply `"a certain blind man`"; the common title `"Blind Bartimaeus`" applied to Luke 18 is a harmonization with Mark, not Luke's own wording [[NOTE:bartimaeus-naming-difference]]."
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
    Add-BeatNode $Ch18NodeId $id $sortKey
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
Try-Append "JERICHO" "This chapter adds two new checkable claims: Herod the Great's excavated winter palace complex [[NOTE:jericho-herodian-palace-netzer]] and the city's documented balsam/date agricultural economy [[NOTE:pliny-josephus-balsam-jericho]], offered as economic backdrop for 18:35-43." $slugToNumber
Try-Append "FLAVIUS JOSEPHUS" "This chapter adds citations: Antiquities 20.9.5 and Jewish War 2.14 on procurators Albinus and Florus [[NOTE:josephus-procurator-corruption]]; Antiquities 15.4.2 and Jewish War 4.459-475 on the Jericho balsam economy [[NOTE:pliny-josephus-balsam-jericho]]." $slugToNumber
Try-Append "THE PHARISEES" "This chapter adds: standing was the normal, non-distinctive Temple prayer posture, not a Pharisaic affectation, per Mishnah Berakhot [[NOTE:amidah-standing-prayer]]." $slugToNumber

# ---- Seed new entities ----
# "Bartimaeus" already exists in the entity catalog (slug: bartimaeus) -- not reseeded here.
Seed-Entity "Timaeus" "timaeus-father-of-bartimaeus" "character" "Patronym given for Bartimaeus's father in Mark's account."
Seed-Entity "Gaius Verres" "gaius-verres" "character" "Corrupt Roman governor of Sicily whose prosecution is the best-documented case of Greco-Roman judicial corruption."
Seed-Entity "Ben Sira" "ben-sira" "character" "Second Temple Jewish sage, author of Sirach/Ecclesiasticus; warns against seeking judgeship."

$conn.Close()
Write-Host "DONE Chapter 18."
