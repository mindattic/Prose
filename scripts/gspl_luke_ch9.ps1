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
$Ch9NodeId = [guid]"019FA96A-0586-741F-A105-2CB2DEB94ADE"
$maxNoteSortKey = [double](Exec-Scalar "SELECT ISNULL(MAX(bn.SortKey),0) FROM BeatNodes bn WHERE bn.NodeId='019FA96B-18E2-7BB4-BAEB-11ACA24934F4'")
$maxGlossarySortKey = [double](Exec-Scalar "SELECT ISNULL(MAX(bn.SortKey),0) FROM BeatNodes bn WHERE bn.NodeId='019FA96B-29F5-7BB9-99D0-0F787960E530'")
$maxNoteNumber = [int](Exec-Scalar "SELECT MAX(CAST(LEFT(b.Text, CHARINDEX(' ',b.Text)-1) AS INT)) FROM BeatNodes bn JOIN Beats b ON bn.BeatId=b.Id WHERE bn.NodeId='019FA96B-18E2-7BB4-BAEB-11ACA24934F4'")
Write-Host "Starting: MaxNumber=$script:MaxNumber MaxNoteSortKey=$maxNoteSortKey MaxNoteNumber=$maxNoteNumber MaxGlossarySortKey=$maxGlossarySortKey"

$em = [char]8212

# ---- Notes ----
$notes = [ordered]@{
'cynic-gear-markers' = @{ title="A Cynic philosopher's defining equipment"; body="Epictetus, Discourses 3.22.9-10, trans. W. A. Oldfather, Loeb Classical Library (Cambridge, MA: Harvard University Press, 1928). In his discourse 'On the Calling of a Cynic,' Epictetus identifies the staff, the wallet or beggar's pouch, and the single rough cloak as the recognizable defining equipment of a genuine Cynic philosopher $em precisely the items the mission instructions of Luke 9:3 forbid the apostles to carry." }
'cynic-minimalism-parallel' = @{ title='A real, bounded parallel with itinerant Cynic preachers'; body="F. Gerald Downing, Cynics and Christian Origins (Edinburgh: T&T Clark, 1992). Downing argues that the itinerant, minimally-provisioned mission style attributed to Jesus and his disciples shows genuine points of contact with popular Cynic itinerant preaching in the eastern Mediterranean, while explicitly stopping short of claiming direct derivation." }
'bethsaida-arav-excavation-reports' = @{ title='Et-Tell: the traditional identification'; body="Rami Arav and Richard A. Freund, eds., Bethsaida: A City by the North Shore of the Sea of Galilee, vol. II (Kirksville, MO: Truman State University Press, 1999). This excavation-report volume documents the Hellenistic-through-Roman-period material recovered at et-Tell that underlies the site's 1994 official identification as Bethsaida by the Israeli Government Naming Committee." }
'bethsaida-site-dispute' = @{ title='A direct challenge to et-Tell'; body="R. Steven Notley, 'Et-Tell Is Not Bethsaida,' Near Eastern Archaeology 70, no. 4 (2007): 220-230. Notley argues that et-Tell's distance from the Sea of Galilee shoreline and the paucity of confirmed first-century material make it an implausible identification for the Gospel-era fishing town of Bethsaida, proposing el-Araj as the stronger candidate." }
'bethsaida-arav-response' = @{ title="Arav's rebuttal"; body="Rami Arav, 'Bethsaida $em A Response to Steven Notley,' Near Eastern Archaeology 74, no. 2 (2011): 92-100. Arav defends et-Tell's identification on the basis of its excavated Hellenistic and early Roman remains, disputing that el-Araj's finds at the time were sufficient to displace the identification." }
'bethsaida-el-araj-inscription' = @{ title="A Byzantine inscription naming Peter"; body="Leah Di Segni, Yaakov Ashkenazi, Mordechai Aviam, and R. Steven Notley, 'The Greek Inscriptions from `"The Church of St. Peter`" at Bethsaida (el-Araj),' Liber Annuus 73 (2023). Publishes and translates a Byzantine-period Greek dedicatory inscription from the el-Araj basilica invoking the intercession of the apostle Peter, read by the excavators as evidence for an early Christian memory identifying el-Araj as Peter's hometown of Bethsaida." }
'loaves-fish-subsistence-diet' = @{ title='Realistic peasant-household provisions'; body="K. C. Hanson and Douglas E. Oakman, Palestine in the Time of Jesus: Social Structures and Social Conflicts, 2nd ed. (Minneapolis: Fortress Press, 2008), chapter on agrarian economy and diet. The authors document barley bread and small dried or salted lake fish as staple, low-cost provisions of the Galilean peasant diet, consistent with the modest quantities in the feeding narrative." }
'groups-of-fifty-crowd-management' = @{ title='A real administrative grid, not an arbitrary number'; body="Geza Vermes, trans., The Complete Dead Sea Scrolls in English, rev. ed. (London: Penguin Classics, 2004), see the Rule of the Congregation (1QSa) and the War Scroll (1QM). Vermes's translation documents the Qumran community's organization of its congregation into units of thousands, hundreds, fifties, and tens $em the same grid found in Exodus 18:21's account of Jethro's advice to Moses, and the background for Luke's detail of seating the crowd in groups of fifty." }
'luke-omits-caesarea-philippi' = @{ title="Luke drops the place-name"; body="Joseph A. Fitzmyer, The Gospel According to Luke I-IX, Anchor Bible vol. 28 (Garden City, NY: Doubleday, 1981), commentary on 9:18-20. Fitzmyer notes that Luke, unlike Matthew (16:13) and Mark (8:27), omits any reference to Caesarea Philippi in his account of Peter's confession, one of several places where Luke trims geographic detail present in his sources." }
'tabor-fortified-settlement' = @{ title='A mountain with a standing garrison'; body="Flavius Josephus, The Jewish War 4.54-61, trans. G. A. Williamson, rev. ed. (London: Penguin Classics, 1981). Josephus describes personally fortifying Mount Tabor as one of nineteen Galilean strongpoints ahead of the First Jewish Revolt, and its eventual fall to a Roman cavalry force under Placidus $em evidence some scholars argue is difficult to reconcile with a private mountaintop retreat decades earlier." }
'transfiguration-mountain-dispute' = @{ title='Tabor or Hermon? An open question'; body="W. D. Davies and Dale C. Allison Jr., The Gospel According to Saint Matthew, International Critical Commentary, vol. 2 (Edinburgh: T&T Clark, 1991), commentary on Matthew 17:1. Davies and Allison survey the traditional Mount Tabor identification against the case for Mount Hermon, noting Hermon's far greater height and its proximity to the just-preceding Caesarea Philippi setting as the strongest arguments for that alternative, while concluding the text itself does not decide the question." }
'samaritan-galilean-pilgrim-conflict' = @{ title='A documented, violent flashpoint'; body="Flavius Josephus, Jewish Antiquities 20.118-136, trans. William Whiston. Josephus records a violent conflict, circa 52 CE, in which Samaritans killed Galilean pilgrims traveling through Samaria toward a Jerusalem festival, the Roman procurator Cumanus was accused of taking bribes to let the killers go unpunished, and armed Galileans retaliated against Samaritan villages before Roman and Jewish authorities intervened." }
}

# ---- Chapter beats ----
$beat1 = @"
Jesus gives the Twelve "power and authority over all demons and to cure diseases" and sends them out to proclaim the kingdom and heal, with instructions that read almost like a packing list of prohibitions: "Take nothing for the journey -- no staff, no bag, no bread, no money, and do not have two tunics" (9:3). They are to accept hospitality where offered and, where rejected, to "shake off the dust from your feet" as testimony against that town (9:5).

Wait, actually -- this list of banned items is not generic asceticism. It reads, almost item for item, as a photographic negative of the standard visual kit of a wandering Cynic philosopher: the staff, the beggar's pouch, and the single rough cloak that a Cynic wore and slept in were the recognized uniform of the type, so recognizable that the Stoic teacher Epictetus could describe a "true Cynic" simply by naming that gear [[NOTE:cynic-gear-markers]]. F. Gerald Downing, whose decades of work argued for a real cultural overlap between itinerant Cynic preachers and the earliest Jesus movement, made exactly this point: whatever the historical relationship, the instruction forbids precisely the items that marked a Cynic as a Cynic [[NOTE:cynic-minimalism-parallel]]. That does not make Jesus a Cynic in disguise -- Cynic minimalism was grounded in philosophical self-sufficiency achieved through personal discipline, while the Lukan instruction grounds the missionaries' vulnerability in dependence on the hospitality of towns and, implicitly, on God. No archaeological record can adjudicate an instruction about what someone did NOT carry, so this claim rests entirely in comparative literary and social-historical analysis: a real, bounded parallel, not a hidden dependency.
"@

$beat2 = @"
Herod Antipas, tetrarch of Galilee and Perea, hears reports of Jesus's activity and is "perplexed" (9:7), turning over the possibilities that John the Baptist -- whom Antipas had beheaded -- has been raised, or that Elijah has appeared. Luke gives this only three verses and does not repeat Mark's more vivid explanation of why Herod is spooked. This is not new evidentiary ground: the Herod Antipas-John the Baptist material was already established in earlier chapters, and 9:7-9 is simply Luke reminding the reader that Antipas is still watching nervously from the wings before Jesus eventually appears before him at the Passion (23:6-12).
"@

$beat3 = @"
The apostles return from their mission, and Jesus withdraws with them to "a town called Bethsaida" (9:10), where a crowd of about five thousand men gathers, is fed from five loaves and two fish, and -- after the disciples seat everyone "in groups of about fifty each" (9:14) -- twelve baskets of leftovers are collected.

Wait, actually -- "a town called Bethsaida" sits on top of one of the liveliest active disputes in Levantine biblical archaeology. For most of the late twentieth century, the Israeli Government Naming Committee's 1994 declaration that et-Tell was ancient Bethsaida stood largely unchallenged, based on excavations led by Rami Arav [[NOTE:bethsaida-arav-excavation-reports]]. But starting in the mid-1990s, a rival excavation at el-Araj, much closer to the modern shore, began turning up Roman-period domestic structures and coins. R. Steven Notley published a direct challenge, arguing the site's distance from the lake and the absence of first-century material contradicted et-Tell's identification [[NOTE:bethsaida-site-dispute]]; Arav answered in the same journal, defending et-Tell's Herodian-period cultic material [[NOTE:bethsaida-arav-response]]. The el-Araj team's case strengthened considerably in 2022, when a Byzantine basilica they call the "Church of the Apostles" produced a Greek inscription invoking the intercession of the apostle Peter [[NOTE:bethsaida-el-araj-inscription]]. As of this writing, both excavation teams continue fieldwork and both continue to argue their site is right -- a live, unresolved question, not a closed one.

The loaves-and-fish quantities check out against what we know of ordinary diet: five barley loaves and two small, dried or salted fish is a realistic single-family provision for a first-century Galilean peasant household, not an exaggerated amount [[NOTE:loaves-fish-subsistence-diet]]. And organizing several thousand people into groups of fifty deliberately echoes the numbered administrative divisions Moses is instructed to impose on Israel at Jethro's urging (Exodus 18:21), the same organizational grid the Qumran community used for both its idealized congregation and its war-camp regulations [[NOTE:groups-of-fifty-crowd-management]].
"@

$beat4 = @"
Jesus, praying alone, asks the disciples who the crowds say he is, and then who they say he is; Peter answers, "The Christ of God" (9:20).

Wait, actually -- the traditional shorthand for this scene, "Peter's confession at Caesarea Philippi," is itself a harmonization. Matthew places the exchange explicitly "in the district of Caesarea Philippi" and Mark locates it in "the villages of Caesarea Philippi" -- Philip the Tetrarch's Roman city near the springs of the Jordan. Luke's version drops the place-name entirely: 9:18-20 gives no location beyond "he was praying alone" [[NOTE:luke-omits-caesarea-philippi]]. This is a plain, checkable textual fact about Luke's redaction, worth naming rather than silently harmonizing away, as popular retellings that default to "Caesarea Philippi" for all three Gospels routinely do.
"@

$beat5 = @"
Jesus predicts his coming suffering, death, and resurrection (9:22), then -- "about eight days" later -- takes Peter, James, and John up "the mountain to pray" (9:28), where his appearance changes, Moses and Elijah appear and speak with him "about his departure," and a voice from a cloud declares him "my Son, my Chosen One" (9:35).

Wait, actually -- Luke, like Matthew and Mark, never names the mountain, and which peak this was is a genuinely open scholarly question. The earliest attested pilgrimage identification is Mount Tabor: Origen in the third century and later Cyril of Jerusalem and Jerome locate the event there, and a Byzantine church was eventually built on its summit. But Tabor has a real archaeological problem for a story about private mountaintop seclusion: the summit carried a fortified settlement across the Hellenistic and Hasmonean periods and again in 66-67 CE, when Josephus himself walled it as a Galilean strongpoint against Rome [[NOTE:tabor-fortified-settlement]]. The modern scholarly case for the alternative points to Mount Hermon, the towering massif just north of Caesarea Philippi -- geographically the next landmark after the place-name Matthew and Mark had just used, and unquestionably higher and more secluded than Tabor [[NOTE:transfiguration-mountain-dispute]]. Neither identification is provable from the text itself, which specifies nothing.
"@

$beat6 = @"
Coming down the mountain, Jesus heals a boy with a convulsive spirit the disciples could not cast out (9:37-43); he again predicts his coming betrayal (9:43-45); the disciples argue over who is greatest (9:46-48); John reports stopping a man from casting out demons "in your name" because "he does not follow with us," and Jesus tells them not to stop him (9:49-50); a Samaritan village refuses to receive Jesus because he is heading toward Jerusalem, and James and John ask to call down fire on it, which Jesus refuses (9:51-56); and the chapter closes with three would-be followers meeting Jesus's hard sayings, including "Foxes have holes, and birds of the air have nests, but the Son of Man has no place to lay his head" (9:58) and "let the dead bury their own dead" (9:59-60).

Wait, actually -- the Samaritan village's refusal is not incidental local color; it sits on top of one of the best-documented ethnic-religious flashpoints of the period. Samaritans and Judean/Galilean Jews shared a scriptural inheritance but disputed the location of legitimate worship and, more practically, the safety of the direct pilgrimage road through Samaria. Josephus records a real, violent escalation on this exact road: around 52 CE, under the Roman procurator Cumanus, Samaritans killed Galilean pilgrims traveling south for a festival; when Cumanus, allegedly after taking a bribe, declined to punish the killers, armed Galileans retaliated against Samaritan villages [[NOTE:samaritan-galilean-pilgrim-conflict]]. That incident postdates Jesus's ministry by roughly two decades and cannot be read directly back into 9:51-56 as its specific cause, but it demonstrates that the antagonism Luke depicts was not invented texture.

The strange exorcist episode (9:49-50) sits inside a recognizable Jewish practice rather than a uniquely Christian anomaly: itinerant Jewish exorcists invoking a powerful name to command spirits were a known type in the period, the same category this book's Eleazar material already established from Josephus. Finally, the itinerancy imagery of 9:57-62 returns to the same comparative ground opened in the mission instructions of 9:1-6: the image of a teacher with no fixed roof is again a point of real, if partial, contact with the Cynic figure, whose signature cloak served as both garment and bedding [[NOTE:cynic-minimalism-parallel]].
"@

$beats = @($beat1, $beat2, $beat3, $beat4, $beat5, $beat6)

# ---- New glossary entries (unique to ch9) ----
$glossary = [ordered]@{
'BETHSAIDA' = "A fishing town on or near the north shore of the Sea of Galilee, named by Luke as the place Jesus withdrew to before the feeding of the five thousand (9:10). Its exact location is one of the genuinely live disputes in Levantine archaeology: et-Tell, an inland mound about 1.5 kilometers from the current shoreline, was officially declared Bethsaida in 1994 on the strength of excavations led by Rami Arav [[NOTE:bethsaida-arav-excavation-reports]]; a rival excavation at el-Araj, much closer to the water, has produced Roman-period domestic structures and, in 2022, a Byzantine-era Greek inscription invoking the apostle Peter's intercession [[NOTE:bethsaida-el-araj-inscription]]. Both teams continue to excavate and both continue to argue for their site [[NOTE:bethsaida-site-dispute]] [[NOTE:bethsaida-arav-response]]."
'MOUNT TABOR' = "A rounded, freestanding hill in the Jezreel Valley in lower Galilee, the earliest-attested pilgrimage identification for the Transfiguration's unnamed `"high mountain,`" recorded by Origen in the third century and by Cyril of Jerusalem and Jerome in the fourth. The identification faces an archaeological objection: the summit carried a fortified garrison across the Hellenistic and Hasmonean periods and again in 66-67 CE [[NOTE:tabor-fortified-settlement]]."
'MOUNT HERMON' = "A roughly 2,814-meter massif at the northern edge of Israelite territory, just north of Caesarea Philippi, proposed by a number of modern scholars as the true site of the Transfiguration on the grounds of its height, seclusion, and geographic proximity to the location just named in the preceding pericope in Matthew and Mark [[NOTE:transfiguration-mountain-dispute]]."
'CAESAREA PHILIPPI' = "A Roman city near the springs that feed the Jordan River, in the tetrarchy of Philip the Tetrarch, named by Matthew and Mark as the setting for Peter's confession. Luke's parallel account (9:18-20) does not name this or any location [[NOTE:luke-omits-caesarea-philippi]]."
'SAMARITANS' = "An ethno-religious community centered on Mount Gerizim in Samaria, sharing scriptural roots with Judean and Galilean Jews but divided from them over the legitimate site of worship and, practically, over safe use of the direct Galilee-to-Jerusalem pilgrimage road through Samaritan territory. Josephus documents a real, violent escalation along that road around 52 CE [[NOTE:samaritan-galilean-pilgrim-conflict]]."
'CUMANUS' = "Roman procurator of Judea, circa 48-52 CE, named by Josephus as the official who allegedly took bribes to avoid punishing Samaritans after they killed Galilean pilgrims, an inaction that triggered further Galilean-Samaritan violence [[NOTE:samaritan-galilean-pilgrim-conflict]]."
'MOSES' = "Lawgiver and central figure of the Exodus tradition, whose death and burial location scripture leaves deliberately unrecorded (Deuteronomy 34:5-6). Appears alongside Elijah at the Transfiguration (9:30). His administrative advice from his father-in-law Jethro (Exodus 18:21) is also the likely background for the `"groups of about fifty`" detail in the feeding of the five thousand [[NOTE:groups-of-fifty-crowd-management]]."
'ELIJAH' = "Prophet of the northern kingdom of Israel who, uniquely among biblical figures, is described as taken up bodily in a whirlwind rather than dying (2 Kings 2:11), feeding a live expectation in Second Temple Judaism of his eschatological return. Appears with Moses at the Transfiguration (9:30)."
'JETHRO' = "Midianite priest and father-in-law of Moses, whose counsel in Exodus 18:21 to organize Israel into units of tens, fifties, hundreds, and thousands is the likely administrative background for Jesus's instruction to seat the crowd `"in groups of about fifty`" (9:14) [[NOTE:groups-of-fifty-crowd-management]]."
'EPICTETUS' = "Stoic-school philosopher (c. 50-135 CE) whose recorded Discourses identify the staff, wallet, and cloak as the defining, recognizable equipment of a Cynic philosopher of the period [[NOTE:cynic-gear-markers]] $em the same items Jesus's mission instructions in Luke 9:3 specifically forbid the apostles to carry."
'QUMRAN COMMUNITY (DEAD SEA SCROLLS)' = "The Second Temple-era Jewish sectarian community whose surviving scrolls describe organizing both their idealized future congregation and their war-camp regulations into administrative units of thousands, hundreds, fifties, and tens [[NOTE:groups-of-fifty-crowd-management]] $em independent material evidence that this numerical organizational grid was live administrative vocabulary in the period."
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
    Add-BeatNode $Ch9NodeId $id $sortKey
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
Try-Append "FLAVIUS JOSEPHUS" "This chapter adds two citations: his account of personally fortifying Mount Tabor ahead of the First Jewish Revolt (Jewish War 4.54-61) [[NOTE:tabor-fortified-settlement]] and his account of the Samaritan-Galilean pilgrim conflict under the procurator Cumanus (Antiquities 20.118-136) [[NOTE:samaritan-galilean-pilgrim-conflict]]." $slugToNumber
Try-Append "ELEAZAR (EXORCIST)" "This chapter's `"strange exorcist`" episode (Luke 9:49-50) reinforces the point that unaffiliated Jewish exorcists invoking a powerful name to command spirits were a recognized type in the period, not a uniquely Christian phenomenon." $slugToNumber
Try-Append "JAMES (SON OF ZEBEDEE)" "New characterization: at 9:54, he and his brother John ask Jesus for authority to call down fire on the Samaritan village that refused them, echoing 2 Kings 1:10 $em evidence of the `"Sons of Thunder`" temperament." $slugToNumber
Try-Append "JOHN (SON OF ZEBEDEE)" "New characterization: at 9:54, he and his brother James ask Jesus for authority to call down fire on the Samaritan village that refused them, echoing 2 Kings 1:10 $em evidence of the `"Sons of Thunder`" temperament." $slugToNumber
Try-Append "SIMON (PETER)" "New confession content: `"The Christ of God`" (9:20) is Peter's own answer to Jesus's question of identity, a milestone in his characterization." $slugToNumber
Try-Append "LAKE OF GENNESARET" "The Bethsaida site dispute (et-Tell vs. el-Araj) turns partly on each site's distance from the current shoreline, relevant background given the Jordan delta's known sedimentation and shoreline shift over two millennia [[NOTE:bethsaida-site-dispute]]." $slugToNumber

# ---- Seed new entities ----
Seed-Entity "Cumanus" "cumanus-procurator" "character" "Roman procurator of Judea, c. 48-52 CE, implicated by Josephus in the Samaritan-Galilean pilgrim conflict."
Seed-Entity "Jethro" "jethro" "character" "Midianite priest and father-in-law of Moses; his administrative advice underlies the crowd organization at the feeding of the five thousand."
Seed-Entity "Epictetus" "epictetus" "character" "Stoic philosopher whose Discourses describe the defining gear of a Cynic philosopher."
Seed-Entity "Qumran Community (Dead Sea Scrolls)" "qumran-community-dead-sea-scrolls" "faction" "Second Temple Jewish sectarian community whose scrolls document a thousands/hundreds/fifties/tens organizational structure."

$conn.Close()
Write-Host "DONE Chapter 9."
