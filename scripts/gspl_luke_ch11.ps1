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
$Ch11NodeId = [guid]"019FA96A-2935-7B65-A953-0250EC861825"
$maxNoteSortKey = [double](Exec-Scalar "SELECT ISNULL(MAX(bn.SortKey),0) FROM BeatNodes bn WHERE bn.NodeId='019FA96B-18E2-7BB4-BAEB-11ACA24934F4'")
$maxGlossarySortKey = [double](Exec-Scalar "SELECT ISNULL(MAX(bn.SortKey),0) FROM BeatNodes bn WHERE bn.NodeId='019FA96B-29F5-7BB9-99D0-0F787960E530'")
$maxNoteNumber = [int](Exec-Scalar "SELECT MAX(CAST(LEFT(b.Text, CHARINDEX(' ',b.Text)-1) AS INT)) FROM BeatNodes bn JOIN Beats b ON bn.BeatId=b.Id WHERE bn.NodeId='019FA96B-18E2-7BB4-BAEB-11ACA24934F4'")
Write-Host "Starting: MaxNumber=$script:MaxNumber MaxNoteSortKey=$maxNoteSortKey MaxNoteNumber=$maxNoteNumber MaxGlossarySortKey=$maxGlossarySortKey"

$em = [char]8212

# ---- Notes ----
$notes = [ordered]@{
'lukan-shorter-prayer-priority' = @{ title="Luke's prayer is the shorter, likely-prior form"; body="John S. Kloppenborg, The Formation of Q: Trajectories in Ancient Wisdom Collections (Philadelphia: Fortress Press, 1987), reconstruction of the Q Lord's Prayer. Kloppenborg's Q-source reconstruction treats Luke's four-petition prayer as preserving the shorter, likely-prior form, with Matthew's seven-petition version read as a liturgically expanded development $em consistent with the broader text-critical convention that expansion of a fixed prayer-text in community use is easier to account for than deliberate compression." }
'jesus-seminar-lords-prayer-vote' = @{ title="The Jesus Seminar's fringe-empiricist vote"; body="Robert W. Funk, Roy W. Hoover, and the Jesus Seminar, The Five Gospels: What Did Jesus Really Say? (New York: Macmillan; Sonoma, CA: Polebridge Press, 1993), ballot results on the Lord's Prayer. The Seminar's published vote recorded only 'Our Father' in the highest-confidence tier, printing the remainder in lower-confidence tiers $em an empiricist-fringe methodology not adopted by the mainstream historical-critical guild, cited here for its position on the spectrum, not as consensus." }
'amidah-kaddish-dating-caution' = @{ title='A shared idiom, not a borrowed text'; body="Ismar Elbogen, Jewish Liturgy: A Comprehensive History, trans. Raymond P. Scheindlin (Philadelphia and New York: Jewish Publication Society/Jewish Theological Seminary, 1993), chapters on the Amidah and Kaddish. Elbogen traces the earliest strata of both prayers to short Second Temple-period benedictory formulas, but places the Amidah's fixed sequence in the Yavneh-era process associated with Rabban Gamaliel II, generally dated to the late first century CE $em after Jesus's lifetime, cautioning against claims of direct textual dependence in either direction." }
'galilean-one-room-house-connect' = @{ title="The parable's premise rests on real floor plans"; body="Jonathan L. Reed, Archaeology and the Galilean Jesus: A Re-Examination of the Evidence (Harrisburg, PA: Trinity Press International, 2000), survey of excavated Galilean village domestic architecture. Reed documents the basalt-walled, single-multipurpose-room house plan typical of first-century Galilean villages, in which an entire family would sleep, eat, and work in one common space $em directly grounding the physical premise of the friend-at-midnight parable." }
'beelzebul-etymology-baalzebub' = @{ title="A one-letter satirical corruption"; body="Karel van der Toorn, Bob Becking, and Pieter W. van der Horst, eds., Dictionary of Deities and Demons in the Bible, 2nd ed. (Leiden: Brill; Grand Rapids, MI: Eerdmans, 1999), s.v. 'Baal-Zebub.' Traces 'Baal-zebub' (2 Kings 1:2-3, 6, 16) to the Ugaritic divine epithet zbl b'l ars, 'Prince, Lord of the Earth,' attested for the storm-god Baal at Ugarit, and reads the Hebrew Bible's zebub ('flies') as a deliberate one-letter satirical distortion of the honorific zebul ('prince, exalted one')." }
'ekron-inscription-context' = @{ title="Ekron confirmed, but not the specific name"; body="Seymour Gitin, Trude Dothan, and Joseph Naveh, 'A Royal Dedicatory Inscription from Ekron,' Israel Exploration Journal 47 (1997). The 1996 Tel Miqne-Ekron excavation recovered a monumental Iron Age II temple complex and a royal dedicatory inscription naming five generations of Ekron rulers, dedicating the structure to the goddess 'Ptgyh' $em confirming Ekron as a real, major Philistine cult city, though the inscription dates roughly two centuries after 2 Kings 1's setting and does not name 'Baal-zebub' specifically." }
'golden-fly-claim-unverified' = @{ title='A popular claim that does not check out'; body="Cross-check against Ashkelon/Philistine-cemetery excavation reporting (Biblical Archaeology Society) to test the popular claim that 'golden fly' cult artifacts have been recovered at Ekron confirming a literal fly-cult. No such Ekron assemblage appears in the archaeological literature reviewed; the well-documented ancient 'golden fly' object is the unrelated Egyptian New Kingdom military valor decoration, apparently conflated with Baal-zebub by simple word association." }
'jonah-historicity-scope-note' = @{ title='A separate Old Testament question, out of scope'; body="Cross-check via standard Hebrew Bible introduction-level scholarship on the Book of Jonah's historicity debate, including the disputed scale of Nineveh and the absence of extrabiblical corroboration for a citywide Ninevite repentance. Cited only to establish this is a live, separate Old Testament question outside this New Testament-focused book's scope." }
'mishnah-demai-tithing' = @{ title='Real, attested tithing scrupulosity'; body="The Mishnah, trans. Herbert Danby (Oxford: Oxford University Press, 1933), tractate Demai. Demai is devoted to produce of doubtful tithe-status purchased from the religiously non-scrupulous, and reflects an institution generally dated to early in the Second Temple period as a marker of Pharisaic ritual rigor $em real, attested corroboration that meticulous tithing of minor garden produce was genuine documented practice, not caricature." }
'tomb-of-prophets-dating' = @{ title="A tomb that postdates the saying it inspired"; body="Gideon Avni and Boaz Zissu, 'The `"Tombs of the Prophets`" on the Mount of Olives: A Re-Examination,' in Viewing Ancient Jewish Art and Archaeology (Leiden: Brill, 2016). The re-examination dates the site's primary burial-catacomb use to the Byzantine period (fourth-to-fifth century CE), well after the Gospels, with the traditional attribution to Haggai, Zechariah, and Malachi resting on later tradition rather than contemporary corroboration." }
'kidron-valley-tombs-second-temple' = @{ title='Genuine Second Temple monumental tombs, unattributed'; body="Amos Kloner and Boaz Zissu, The Necropolis of Jerusalem in the Second Temple Period (Leuven: Peeters, 2007). Documents the Kidron Valley's monumental rock-cut tomb facades, including the Tomb of Benei Hezir (second-century-BCE Hasmonean) and the so-called Tomb of Absalom (redated to the first century CE) $em genuine examples of the honored-dead monumental tomb-building culture Luke 11:47-48 references, without a specific match to any named prophet." }
'zechariah-berachiah-identity-puzzle' = @{ title='Two Zechariahs conflated'; body="W. D. Davies and Dale C. Allison Jr., The Gospel According to Saint Matthew, International Critical Commentary, vol. 3 (Edinburgh: T&T Clark, 1997), commentary on Matthew 23:35. Davies and Allison discuss 'son of Barachiah' in Matthew's parallel as most plausibly a conflation of Zechariah son of Jehoiada (murdered in the Temple court per 2 Chronicles 24:20-22) with the later prophet Zechariah son of Berechiah, for whom no such death is recorded; some manuscripts of Matthew omit the patronymic entirely, and Luke's version drops it altogether." }
}

# ---- Chapter beats ----
$beat1 = @"
The familiar version most readers carry in their heads is the one memorized in childhood -- "Our Father, who art in heaven, hallowed be thy name" -- but that is Matthew's prayer (Matthew 6:9-13), not this one. Luke's version, prompted by a disciple's direct request (11:1), is shorter and starker: no "who art in heaven," no "thy will be done, on earth as it is in heaven," no "deliver us from evil," no closing doxology (11:2-4). Where Matthew's Jesus offers seven petitions, Luke's offers four.

This is one of the most durable data points in New Testament source criticism. Both prayers almost certainly draw on a shared common source, and the text-critical convention has run in one direction for centuries: expansion is easier to explain than compression [[NOTE:lukan-shorter-prayer-priority]]. A scribe or a liturgizing community has an obvious motive to round out a terse prayer for public use; almost no one has a motive to strip lines out of a prayer already in devotional use. On the far empiricist edge of the spectrum, the Jesus Seminar's 1993 published ballot went considerably further, voting only the words "Our Father" as likely to trace to a single historical utterance [[NOTE:jesus-seminar-lords-prayer-vote]] -- a minority-fringe methodology most historical-critical scholars outside the Seminar's own circle do not adopt, but whose underlying point lines up with the mainstream text-critical read.

The temptation, once the divergence is on the table, is to reach for a tidy parallel: Jesus taught something like the Kaddish, or something like the Amidah. That claim needs more care than it usually gets. The Kaddish's earliest strata likely originated as short benedictory formulas circulating in the late Second Temple period. But the Amidah reached its fixed number and sequence of blessings only in a process traditionally associated with Rabban Gamaliel II at Yavneh, generally dated to the last decades of the first century CE -- after the Temple's destruction, and therefore after Jesus's own lifetime [[NOTE:amidah-kaddish-dating-caution]]. The honest position sits between two overclaims: Jesus was not reciting a version of "the Amidah" as later Judaism fixed it, but the resemblance is not coincidental either -- both prayers draw on a common first-century Jewish idiom of praise-then-petition.
"@

$beat2 = @"
The parable's comic premise -- a man roused after bedtime by a neighbor's knock, protesting that "my children are with me in bed" and that getting up would wake the whole household (11:7) -- reads as exaggerated hospitality-shaming until it is set against the one-room domestic reality this book has already established for Galilean village housing. The basalt-walled, single-family dwellings excavated in villages like Capernaum typically centered on one multipurpose room used for eating, working, and sleeping [[NOTE:galilean-one-room-house-connect]]. There was no guest room to retreat to and no way to rouse one sleeper without rousing all of them -- the parable's joke and its social pressure both depend on real, excavated floor plans, not comic invention.
"@

$beat3 = @"
Jesus casts out a demon; some in the crowd accuse him of doing it "by Beelzebul, the prince of demons" (11:15, 18-19) -- and the name itself carries a documented, multi-century history of deliberate mockery. The Hebrew Bible names a specific deity, "Baal-zebub, the god of Ekron," consulted by the injured King Ahaziah of Israel (2 Kings 1:2-3, 6, 16) -- a Philistine god of a real city on Judah's southwestern border. Rendered literally, Baal-zebub means "lord of the flies," and this is very likely not the god's actual cultic title but a satirical distortion of one. Ugaritic religious texts repeatedly give the storm-god Baal the honorific zbl b'l ars, "Prince, Lord of the Earth," using the root zbl, "prince," not zebub, "fly" [[NOTE:beelzebul-etymology-baalzebub]]. The consonantal swap is a one-letter, maximally insulting pun: it takes a genuine Canaanite divine epithet of exaltation and turns it into "lord of dungheap flies."

What does the archaeology at Ekron itself add? Excavations at Tel Miqne uncovered a monumental Iron Age II temple complex and, in 1996, a royal dedicatory inscription naming five generations of Ekron's rulers, dedicating the temple to a goddess, "Ptgyh" -- not to Baal-zebub by name [[NOTE:ekron-inscription-context]]. The find independently confirms Ekron as a real, prominent Philistine city with an active cult center -- but it should not be oversold: the inscription dates roughly two centuries after the narrative setting, and no Ekron inscription names "Baal-zebub" specifically. One further popular claim deserves a caution flag: some popular accounts assert "golden fly" cult objects have been recovered at Ekron confirming a literal fly-cult. That claim does not hold up on inspection [[NOTE:golden-fly-claim-unverified]].
"@

$beat4 = @"
Jesus offers "the sign of Jonah" as the only sign this generation will get, invoking the Ninevites' repentance and the Queen of the South as witnesses against his hearers. The separate question of the Book of Jonah's own historicity -- whether it records an actual eighth-century-BCE mission to a historical Nineveh, or is better read as a didactic parable built around a historical prophet's name -- is a live and long-running debate in Hebrew Bible scholarship, but it belongs to the Old Testament and sits outside this book's New Testament scope [[NOTE:jonah-historicity-scope-note]]. What matters here is only that Luke's Jesus treats the Jonah narrative as a known, shared cultural reference point.
"@

$beat5 = @"
This short unit -- the lamp not hidden under a bushel, the eye as the lamp of the body, the whole body "full of light" if the eye is sound -- is parabolic and ethical throughout, restating and extending the lamp-saying already used elsewhere in the Synoptic tradition. It carries no separately checkable historical, archaeological, or textual claim beyond what is already covered in the Gospels' broader lamp-and-light imagery.
"@

$beat6 = @"
The first woe -- "you Pharisees tithe mint and rue and every herb, but neglect justice and the love of God" (11:42) -- reads to modern ears as caricature, obsessing over kitchen-garden trivia. The rabbinic record corroborates the practice as real and taken seriously. The Mishnah's tractate Demai is devoted entirely to produce of doubtful tithe status, and explicitly extends tithing concern to garden herbs, including exactly the kind of minor domestic produce Luke's Jesus names [[NOTE:mishnah-demai-tithing]]. Luke's woe is not mocking an invented practice; it is arguing, from inside real and attested Pharisaic scrupulosity, that the scrupulosity has displaced the weightier commandments.

The second checkable claim is architectural: "you build the tombs of the prophets" (11:47-48). It is tempting to reach for the site tourists visit today under exactly that name -- the "Tomb of the Prophets" on the Mount of Olives. That specific complex does not support the connection: a 2015-2016 re-examination dates its primary use to the Byzantine period, centuries after Luke's Jesus is depicted speaking [[NOTE:tomb-of-prophets-dating]]. What is genuinely attested for the right period is different but adjacent: the monumental rock-cut tomb facades still visible in Jerusalem's Kidron Valley, including the Tomb of Benei Hezir and the so-called Tomb of Absalom, are exactly the kind of grand funerary monument-building that would make "building tombs for the honored dead" a recognizable, contemporary social practice [[NOTE:kidron-valley-tombs-second-temple]].

The chapter's woes close with one of the New Testament's genuine, long-recognized textual puzzles: the blood "of Zechariah, who perished between the altar and the sanctuary" (11:51). The specific death described -- a priest murdered in the Temple court by royal order -- matches exactly one figure: Zechariah son of Jehoiada, stoned on King Joash's command (2 Chronicles 24:20-22). But Matthew's parallel names him "Zechariah son of Barachiah" (Matthew 23:35) -- and the only figure in the Hebrew Bible called "son of Berechiah" is the very different, much later prophet Zechariah, for whom no Temple-court martyrdom is recorded [[NOTE:zechariah-berachiah-identity-puzzle]]. Luke's version, notably, omits the patronymic altogether -- arguably the more cautious, less error-prone form.
"@

$beats = @($beat1, $beat2, $beat3, $beat4, $beat5, $beat6)

# ---- New glossary entries (unique to ch11) ----
$glossary = [ordered]@{
'BEELZEBUL / BEELZEBUB' = "The name applied to `"the prince of demons`" in the Beelzebul controversy (Luke 11:14-28). The name derives from the Hebrew Bible's `"Baal-zebub, the god of Ekron`" (2 Kings 1:2-3, 6, 16), consulted by King Ahaziah of Israel after a fall. Rendered `"lord of the flies,`" the name is very likely a deliberate satirical corruption of an authentic divine epithet: Ugaritic texts repeatedly call Baal zbl b'l ars, `"Prince, Lord of the Earth`" [[NOTE:beelzebul-etymology-baalzebub]]. No archaeological inscription recovered at Ekron itself names this specific deity by either form of the name [[NOTE:ekron-inscription-context]]."
'EKRON' = "One of the five principal cities of the Philistine pentapolis, identified with the site of Tel Miqne. Named as the home of the god `"Baal-zebub`" consulted by King Ahaziah (2 Kings 1:2). Excavations uncovered a monumental Iron Age II temple complex and, in 1996, a royal dedicatory inscription naming five generations of Ekron rulers and dedicating the temple to the goddess `"Ptgyh`" $em independently confirming Ekron's reality and prominence, though the inscription postdates the 2 Kings narrative by roughly two centuries and does not itself name Baal-zebub [[NOTE:ekron-inscription-context]]."
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
    Add-BeatNode $Ch11NodeId $id $sortKey
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
Try-Append "CAPERNAUM" "This chapter adds the domestic-architecture claim (single-room, shared-courtyard housing, whole family sleeping together) underlying the friend-at-midnight parable [[NOTE:galilean-one-room-house-connect]]." $slugToNumber
Try-Append "THE PHARISEES" "This chapter adds a corroborating claim (Mishnah tractate Demai, tithing scrupulosity over minor garden herbs) that substantiates rather than caricatures the group's documented practice [[NOTE:mishnah-demai-tithing]]." $slugToNumber
Try-Append "MISHNAH" "This chapter cites tractate Demai and its content on doubtful-tithe produce law [[NOTE:mishnah-demai-tithing]]." $slugToNumber

# ---- Seed new entities ----
# "Beelzebul" already exists in the entity catalog (slug: beelzebul) -- not reseeded here.
Seed-Entity "Ekron" "ekron" "place" "Philistine pentapolis city; home of the god Baal-zebub consulted by King Ahaziah in 2 Kings 1."

$conn.Close()
Write-Host "DONE Chapter 11."
