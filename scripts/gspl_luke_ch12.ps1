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
$Ch12NodeId = [guid]"019FA96A-3A6B-7D8C-9960-89F2E212469B"
$maxNoteSortKey = [double](Exec-Scalar "SELECT ISNULL(MAX(bn.SortKey),0) FROM BeatNodes bn WHERE bn.NodeId='019FA96B-18E2-7BB4-BAEB-11ACA24934F4'")
$maxGlossarySortKey = [double](Exec-Scalar "SELECT ISNULL(MAX(bn.SortKey),0) FROM BeatNodes bn WHERE bn.NodeId='019FA96B-29F5-7BB9-99D0-0F787960E530'")
$maxNoteNumber = [int](Exec-Scalar "SELECT MAX(CAST(LEFT(b.Text, CHARINDEX(' ',b.Text)-1) AS INT)) FROM BeatNodes bn JOIN Beats b ON bn.BeatId=b.Id WHERE bn.NodeId='019FA96B-18E2-7BB4-BAEB-11ACA24934F4'")
Write-Host "Starting: MaxNumber=$script:MaxNumber MaxNoteSortKey=$maxNoteSortKey MaxNoteNumber=$maxNoteNumber MaxGlossarySortKey=$maxGlossarySortKey"

$em = [char]8212

# ---- Notes ----
$notes = [ordered]@{
'sparrow-assarion-price' = @{ title='Two sparrows for a fifth bird free'; body="Craig A. Evans and standard numismatic reference works on Roman provincial coinage (summarized via the Southwestern Journal of Theology, 'An Assarion for Your Thoughts'). The assarion was a small bronze Roman coin, roughly one-sixteenth of a denarius by the first century CE, itself close to a day's unskilled wage. Luke 12:6 ('five sparrows for two assaria') and Matthew 10:29 ('two sparrows for one assarion') quote different numbers for the same saying $em Luke's version effectively throws in a fifth bird for the same rate." }
'hair-idiom-family' = @{ title='A standing Hebrew idiom of divine protection'; body="Cross-reference survey: 1 Samuel 14:45; 2 Samuel 14:11; 1 Kings 1:52; Luke 21:18; Acts 27:34. 'Not a hair of the head shall fall' is a recurring Hebrew idiom of complete divine protection reused twice in Luke-Acts; Luke 12:7's 'the hairs of your head are all numbered' intensifies this standing qualitative promise into an explicit claim of enumeration." }
'rabbi-inheritance-arbiter' = @{ title="A real social role, not an eccentric request"; body="Amy-Jill Levine, Short Stories by Jesus: The Enigmatic Parables of a Controversial Rabbi (New York: HarperOne, 2014), treatment of the Rich Fool. Teachers with local reputations for wisdom were routinely asked to settle disputes informally, including inheritance disputes, a real and recognized social role alongside the more formal beit din structure." }
'moses-judge-echo' = @{ title="A deliberate echo of Exodus"; body="Joseph A. Fitzmyer, The Gospel According to Luke X-XXIV, Anchor Bible vol. 28A (Garden City, NY: Doubleday, 1985), on Luke 12:13-14. Fitzmyer notes Jesus's rebuff, 'who made me a judge or arbitrator over you?,' closely reproduces the rebuke given to Moses in Exodus 2:14, 'who made you a ruler and a judge over us?' $em read as a deliberate scriptural echo." }
'galilee-farm-economy' = @{ title='Grain storage as an economic outlier'; body="'A Galilean Farm `"Frozen in Time,`"' Biblical Archaeology Society, reporting a 2,100-year-old Hasmonean-period farmstead near Nahal Abel. The excavated farmstead preserved storage jars rather than dedicated granary architecture, and regional surveys find storage buildings nearly absent from the Galilean archaeological record $em the Rich Fool's plan to build larger barns marks him as an economic outlier, not a typical farmer." }
'sardanapalus-carpe-diem' = @{ title="A Hellenistic tomb-inscription parallel"; body="Athenaeus of Naucratis, Deipnosophistae, quoting the Stoic philosopher Chrysippus, on the legendary tomb inscription of the Assyrian king Sardanapalus. The inscription's closing line, 'eat, drink, be merry, since all else is not worth this,' is a well-known Hellenistic-era parallel to the sentiment voiced by the Rich Fool in Luke 12:19." }
'gilgamesh-ecclesiastes-carpe-diem' = @{ title="An ancient Near Eastern literary commonplace"; body="'Kingship and Carpe Diem, Between Gilgamesh and Qoheleth,' Vetus Testamentum vol. 67, issue 2 (2017), Brill. Siduri's counsel to Gilgamesh in the Old Babylonian tablet tradition ('fill your belly with good things... feast and rejoice') is a genuine ancient Near Eastern literary antecedent to the carpe diem sentiment later voiced in Ecclesiastes 9:7 and Isaiah 22:13, echoed in Luke 12:19 $em evidence of a shared regional commonplace, not direct borrowing." }
'luke-matthew-ravens-textual' = @{ title="Ravens, not generic birds"; body="Synoptic comparison of Luke 12:24 and Matthew 6:26 in Greek, per standard exegetical commentaries. Luke's text specifies korakes (ravens/crows), a narrower term, where Matthew's parallel uses the generic peteina tou ouranou ('birds of the air') $em a genuine, checkable textual variation between the two versions of the same saying." }
'raven-unclean-leviticus' = @{ title='A bird Torah marks as unclean'; body="Leviticus 11:13-20, listing the raven among birds forbidden as food, generally explained by its scavenging habits. Luke's specific choice of 'ravens' over Matthew's generic 'birds' carries added rhetorical weight: God provides even for the bird Jewish law itself sets apart as unclean." }
'wedding-bridegroom-timing' = @{ title="An unpredictable hour by design"; body="Comparative treatment of first-century Jewish wedding customs in relation to New Testament watchfulness parables. The bridegroom's exact hour of return $em following wedding preparations, procession, and celebration, often stretching to midnight or beyond $em was genuinely unpredictable, the real social premise behind the parable's demand for constant readiness." }
'dichotomesei-severity' = @{ title="Hyperbole, not an attested household punishment"; body="Francois Bovon, Luke 2: A Commentary on the Gospel of Luke 9:51-19:27, Hermeneia series (Minneapolis: Fortress Press, 2013), on Luke 12:41-48. Mainstream commentators read dichotomesei ('cut in pieces') as hyperbole for total, severe judgment, not a literal practice; Matthew's parallel (24:51) has the 'cut' servant then weeping and gnashing his teeth, incoherent if meant literally." }
'persian-dismemberment-idiom' = @{ title="The idiom borrows real Persian judicial horror"; body="Bruno Jacobs, 'Torture in the Achaemenid Period,' Encyclopaedia Iranica; the Aramaic idiom in Daniel 2:5 and 3:29 ('made into parts') reflects genuine Old Persian legal phrasing. Independently documented Achaemenid judicial severity (impaling at Bisotun, one attested case of a judge flayed alive) shows the 'cut in pieces' idiom draws on real foreign legal horror, though not an actual practiced punishment in first-century Jewish households." }
'micah-7-6-echo' = @{ title='A precise Old Testament citation'; body="Direct textual comparison, Micah 7:6 and Luke 12:53. The family pairings in Luke 12:53 track Micah's list closely enough that scholars treat the correspondence as intentional quotation, also echoed in Matthew 10:35-36." }
'levant-wind-patterns' = @{ title='Real, geography-specific meteorology'; body="Regional meteorological description of the Judean-Galilean corridor. Rain-bearing weather genuinely arrives on clouds from the Mediterranean to the west, while the south lies open toward the Negev and Arabian deserts, so a south wind reliably brings hot, dry conditions $em an accurate, geography-specific description, not an interchangeable folk saying." }
'roman-debt-imprisonment' = @{ title="Debt enforcement under Roman provincial law"; body="'Lex Poetelia Papiria,' Oxford Classical Dictionary, citing Livy's traditional dating of 326 BCE. The law abolished nexum (formal debt bondage), shifting Roman remedies toward seizure of property $em but practical imprisonment of insolvent debtors by magistrate order persisted into the Roman provincial period, matching the judge-officer-prison sequence in Luke 12:58-59." }
}

# ---- Chapter beats ----
$beat1 = @"
As crowds press in so thickly that people trample one another, Jesus turns first to his disciples with a warning about "the leaven of the Pharisees" -- hypocrisy that hides now but will be shouted from the rooftops later (12:1-3) -- then pivots to a teaching about fear: do not fear those who can kill the body, fear the One with authority beyond it (12:4-5). The transition lands on two small, oddly specific pieces of first-century material culture. "Are not five sparrows sold for two pennies?" (12:6) is an actual price, worth pausing on because Matthew's version of the same saying (10:29) quotes a different number: two sparrows for one assarion, not five for two. Luke's version is the better deal. The assarion behind both sayings was a small bronze Roman coin worth roughly one-sixteenth of a denarius, and a denarius was close to a day's unskilled wage in the period [[NOTE:sparrow-assarion-price]] -- so the sparrow, plucked and skewered in the marketplace bird-stalls, was about as cheap as animal protein got. From there Jesus intensifies a further claim: "the hairs of your head are all numbered." This belongs to a standing Hebrew idiom of divine protection -- "not one hair of his head shall fall to the ground" is said of Jonathan in 1 Samuel 14:45, of a condemned son in 2 Samuel 14:11, and of Solomon's rival Adonijah in 1 Kings 1:52; Luke himself will reuse the formula twice more (21:18; Acts 27:34) [[NOTE:hair-idiom-family]]. Luke 12:7 takes a well-worn qualitative promise and pushes it to a quantitative claim -- the same idiom family, one register more extreme.
"@

$beat2 = @"
A man in the crowd asks Jesus to "tell my brother to divide the inheritance with me" (12:13) -- a request that sounds odd aimed at a traveling preacher, but reflects a genuine social role. Teachers with any reputation for wisdom were routinely asked to arbitrate disputes informally, a function that ran alongside the formal beit din requiring recognized judges [[NOTE:rabbi-inheritance-arbiter]]. Jesus's refusal is sharper than a modern reader catches on a first pass: "who made me a judge or arbitrator over you?" (12:14) reproduces, almost word for word, the rebuke a Hebrew slave gives Moses in Exodus 2:14 [[NOTE:moses-judge-echo]]. Jesus declines the office Moses was once mockingly denied, and tells a parable instead.

The rich man's problem -- a harvest so large his existing storage cannot hold it -- reads as an ordinary farming crisis, but tearing down barns to build bigger ones (12:18) marks him as unusual. A 2,100-year-old Galilean farmstead excavated near Nahal Abel shows the ordinary storage technology of the period: large ceramic jars, not dedicated granary buildings, and broader surveys of the Galilean rural economy find storage buildings and shops nearly absent, consistent with subsistence-level households [[NOTE:galilee-farm-economy]]. The rich man's ambition places him among a genuine landowning minority rather than a typical Galilean farmer. His self-toast, "eat, drink, and be merry" (12:19), belongs to a real and well-documented commonplace: Ecclesiastes 9:7 and Isaiah 22:13 sit inside Israel's own wisdom literature, and a striking non-biblical parallel survives in the Epic of Gilgamesh, where the tavern-keeper Siduri counsels the grieving king to "feast and rejoice" [[NOTE:gilgamesh-ecclesiastes-carpe-diem]]. A later Hellenistic tradition attaches nearly identical words to a tomb inscription for the legendary king Sardanapalus, preserved by Athenaeus quoting Chrysippus [[NOTE:sardanapalus-carpe-diem]]. Scholars treat these as evidence of a genuine cross-cultural commonplace about mortality and pleasure, not proof of direct borrowing.
"@

$beat3 = @"
The teaching against worry that follows (12:22-34) opens with "consider the ravens" (12:24), one of the more precise textual differences between Luke and Matthew's parallel. Matthew 6:26 has Jesus point to "the birds of the air" generically; Luke narrows the image to one specific bird: korakes, ravens [[NOTE:luke-matthew-ravens-textual]]. The choice sharpens the argument, because ravens carried a specific charge in Jewish law -- Leviticus 11:15 lists the raven among the unclean birds, tied to its scavenging habits [[NOTE:raven-unclean-leviticus]]. Luke's version makes a stronger claim than Matthew's: God provides not for "birds" abstractly but for the specific bird the Torah marks as unclean. The lily comparison a few verses later -- "Solomon in all his glory was not arrayed like one of these" (12:27) -- leans on Solomon's proverbial reputation for wealth and splendor, already covered in this book's Solomon glossary entry; this chapter adds no new empirical claim about Solomon, only a new literary use of the existing reputation.
"@

$beat4 = @"
The watchfulness parables (12:35-48) picture servants waiting up for a master's return "from a wedding banquet" (12:36), and the premise depends on a real social fact: the exact hour of a bridegroom's return was genuinely unpredictable, sometimes stretching to midnight or beyond [[NOTE:wedding-bridegroom-timing]]. The parable's punishment for the unfaithful steward -- the master "will cut him in pieces" (12:46) -- reads as shockingly literal in translation, but the mainstream reading treats it as hyperbole for total judgment; Matthew's parallel (24:51) has the "cut" servant then weeping and gnashing his teeth, incoherent if the cutting were literal [[NOTE:dichotomesei-severity]]. The idiom's shock value borrows real horror from a genuine tradition outside Judaism: the Aramaic threat "made into parts" appears twice in Daniel (2:5, 3:29) as an actual Achaemenid Persian penalty, independently documented elsewhere [[NOTE:persian-dismemberment-idiom]].
"@

$beat5 = @"
The declaration that follows -- fire on the earth, and division "father against son... mother-in-law against daughter-in-law" (12:49-53) -- is a close, direct citation of Micah 7:6 [[NOTE:micah-7-6-echo]], one of the more precisely traceable Old Testament quotations anywhere in Luke's Gospel, later echoed again in Matthew 10:35-36.
"@

$beat6 = @"
The chapter's final unit turns to reading signs. "When you see a cloud rising in the west... and when the south wind blows, you say, `'It's going to be hot`'" (12:54-55) describes a real, checkable regional weather pattern: rain-bearing clouds genuinely arrive from the west off the Mediterranean, while the south lies open toward the Negev and Arabian desert, so a south wind reliably brings hot, dry air [[NOTE:levant-wind-patterns]]. The closing advice to settle out of court "on the way," or be dragged before the judge and thrown into prison until the debt is paid (12:58-59), matches a real feature of Roman-period legal practice. Formal debt bondage had been abolished centuries earlier by the Lex Poetelia Papiria, but practical imprisonment of a defaulting debtor by a magistrate's order persisted well into the Roman provincial period, matching the judge-to-officer-to-prison sequence Jesus lays out almost exactly [[NOTE:roman-debt-imprisonment]].
"@

$beats = @($beat1, $beat2, $beat3, $beat4, $beat5, $beat6)

# ---- New glossary entries ----
$glossary = [ordered]@{
'MICAH (PROPHET)' = "Eighth-century BCE Judean prophet, contemporary of Isaiah, author of the book bearing his name. Luke 12:53 directly quotes Micah 7:6's list of family members turned against one another, one of the more precisely traceable Old Testament citations in Luke's Gospel [[NOTE:micah-7-6-echo]]."
'ASSARION (COIN)' = "Small bronze Roman provincial coin, valued at roughly one-sixteenth of a denarius by the first century CE, itself close to a day's unskilled wage. Appears in Luke 12:6 ('five sparrows for two assaria') and its Matthean parallel (10:29, 'two sparrows for one assarion'), quoting different market rates for the same saying [[NOTE:sparrow-assarion-price]]."
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
    Add-BeatNode $Ch12NodeId $id $sortKey
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
Try-Append "SOLOMON" "This chapter adds a new literary use: Luke 12:27 invokes Solomon's proverbial splendor specifically as a clothing/adornment comparison ('Solomon in all his glory was not arrayed like one of these [lilies]')." $slugToNumber
Try-Append "ISAIAH (PROPHET)" "This chapter's Rich Fool beat cites Isaiah 22:13 ('let us eat and drink, for tomorrow we die') as an Old Testament comparative parallel to the `"eat, drink, be merry`" carpe diem commonplace [[NOTE:gilgamesh-ecclesiastes-carpe-diem]]." $slugToNumber

# ---- Seed new entities ----
Seed-Entity "Sardanapalus" "sardanapalus" "character" "Legendary Assyrian king cited via a Hellenistic tomb-inscription tradition as a carpe-diem literary parallel to Luke 12:19."
Seed-Entity "Siduri" "siduri" "character" "Tavern-keeper figure in the Epic of Gilgamesh whose counsel parallels the Rich Fool's `"eat, drink, be merry`" sentiment."
Seed-Entity "Epic of Gilgamesh" "epic-of-gilgamesh" "document" "Mesopotamian epic poem cited for its carpe-diem parallel to Ecclesiastes and Luke 12:19."
Seed-Entity "Lex Poetelia Papiria" "lex-poetelia-papiria" "document" "Roman law (trad. 326 BCE) abolishing nexum debt-bondage, cited as legal backdrop to Luke 12:58-59."
Seed-Entity "Nahal Abel farmstead" "nahal-abel-farmstead" "place" "2,100-year-old Hasmonean-period Galilean farm excavation, cited for realistic period grain-storage archaeology."

$conn.Close()
Write-Host "DONE Chapter 12."
