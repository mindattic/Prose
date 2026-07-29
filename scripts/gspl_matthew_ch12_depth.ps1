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

# —— Live state ——
$script:MaxNumber = [int](Exec-Scalar "SELECT MAX(Number) FROM Beats")
$NotesNodeId = [guid]"019FA01D-FA22-76C6-976C-3EA4F4D54A14"
$GlossaryNodeId = [guid]"019FA026-3758-7A57-AF65-EC1DB4303EF3"
$Ch12NodeId = [guid]"019FA069-844F-7A56-A07C-D5037832F038"

$maxNoteSortKey = [double](Exec-Scalar "SELECT ISNULL(MAX(bn.SortKey),0) FROM BeatNodes bn WHERE bn.NodeId='019FA01D-FA22-76C6-976C-3EA4F4D54A14' AND bn.IsEnabled=1")
$maxGlossarySortKey = [double](Exec-Scalar "SELECT ISNULL(MAX(bn.SortKey),0) FROM BeatNodes bn WHERE bn.NodeId='019FA026-3758-7A57-AF65-EC1DB4303EF3' AND bn.IsEnabled=1")
$maxChapterSortKey = [double](Exec-Scalar "SELECT ISNULL(MAX(bn.SortKey),0) FROM BeatNodes bn WHERE bn.NodeId='019FA069-844F-7A56-A07C-D5037832F038' AND bn.IsEnabled=1")
$maxNoteNumber = [int](Exec-Scalar "SELECT ISNULL(MAX(CAST(LEFT(b.Text, CHARINDEX(' ',b.Text)-1) AS INT)),0) FROM BeatNodes bn JOIN Beats b ON bn.BeatId=b.Id WHERE bn.NodeId='019FA01D-FA22-76C6-976C-3EA4F4D54A14' AND bn.IsEnabled=1 AND CHARINDEX(' ', b.Text) > 1 AND LEFT(b.Text, CHARINDEX(' ',b.Text)-1) NOT LIKE '%[^0-9]%'")

Write-Host "Starting: MaxNumber=$script:MaxNumber MaxNoteSortKey=$maxNoteSortKey MaxNoteNumber=$maxNoteNumber MaxGlossarySortKey=$maxGlossarySortKey MaxChapterSortKey=$maxChapterSortKey"

# —— Notes (slug -> title/body) in order ——
$notes = [ordered]@{
'grain-melachot-reaping-category' = @{ title="Reaping by hand: the specific melachah category behind the grain-field complaint"; body="Mishnah, Shabbat 7:2 (trans. Herbert Danby, The Mishnah, Oxford: Clarendon Press, 1933). The Mishnah's list of thirty-nine primary categories of Sabbath-forbidden labor — already drawn on elsewhere in this campaign for the mat-carrying dispute behind John's Bethesda healing (John 5:8-10) — places reaping (qotzer) third on the list, immediately followed by binding sheaves, threshing, and winnowing; plucking grain heads by hand while walking through a field is the specific act later halakhah classified under this category, which is what gives the Pharisees' objection in Matthew 12:2 its real legal shape rather than leaving it a vague accusation of unspecified 'work.'" }
'keener-halakhic-argument-form' = @{ title="An argument built the way rabbis argued"; body="Craig S. Keener, A Commentary on the Gospel of Matthew (Grand Rapids: Eerdmans, 1999), commentary ad loc. Matthew 12:1-8. Keener reads Jesus's twofold reply — David eating the showbread, the priests working in the Temple — as following a recognizable pattern of early Jewish legal reasoning: arguing from an accepted precedent (David) and an accepted exception (priestly Temple service) to a new case, the same inference-from-precedent structure rabbinic argument would later formalize as qal wahomer ('light and heavy,' arguing from a lesser to a greater case). On this reading Jesus is not rejecting Sabbath law's validity but arguing inside it, using its own logic." }
'davies-allison-showbread-precedent' = @{ title="The showbread case and the priests' exception, cited exactly"; body="W. D. Davies and Dale C. Allison Jr., Matthew 8-18, International Critical Commentary vol. 2 (Edinburgh: T&T Clark, 1991), commentary ad loc. Matthew 12:1-8. Davies and Allison trace the two precedents Jesus cites to their specific texts — David eating consecrated bread reserved for priests when fleeing Saul (1 Samuel 21:1-6) and the priests' own Sabbath Temple labor mandated by the additional Sabbath offering law (Numbers 28:9-10) — and note that both were already recognized, debated cases in Second Temple and early rabbinic legal discussion, not precedents Matthew's Jesus invents for the scene." }
'beelzebul-ugaritic-herrmann-ddd' = @{ title="A prince's title, read backward as an insult"; body="Wolfgang Herrmann, s.v. 'Baal-Zebub,' in Dictionary of Deities and Demons in the Bible, 2nd extensively rev. ed., ed. Karel van der Toorn, Bob Becking, and Pieter W. van der Horst (Leiden: Brill; Grand Rapids: Eerdmans, 1999). Herrmann's entry situates the Hebrew Bible's 'Baal-zebub, god of Ekron' (2 Kings 1:2-3) against Ugaritic texts that use the epithet zbl b'l ('Prince Baal,' or 'Baal the Prince') for the storm-god Baal at Ugarit; the scholarly reconstruction Matthew 12:24's 'Beelzeboul' spelling supports is that Israelite scribes deliberately flattened a rival deity's honorific 'zebul' ('prince,' 'exalted one') into the mocking, near-homophonous 'zebub' ('fly'), so that the demon name the Pharisees fling at Jesus carries a buried piece of Canaanite religious history inside it." }
'jonah-onah-inclusive-reckoning' = @{ title="How three days could mean parts of three days"; body="W. D. Davies and Dale C. Allison Jr., Matthew 8-18, International Critical Commentary vol. 2 (Edinburgh: T&T Clark, 1991), commentary ad loc. Matthew 12:40. Davies and Allison note that ancient Jewish time-reckoning worked in onah units (a day-and-night pairing), under which any part of an onah could count as the whole; a burial late Friday, all of Saturday, and part of Sunday morning would satisfy 'three days' under that convention without requiring three literal 24-hour spans, and this is the harmonization most commonly offered for the gap between Matthew 12:40's wording and the Gospel's own Passion timeline." }
'jonah-three-nights-remaining-difficulty' = @{ title="Where the harmonization still strains"; body="Robert H. Gundry, Matthew: A Commentary on His Handbook for a Mixed Church under Persecution, 2nd ed. (Grand Rapids: Eerdmans, 1994), commentary ad loc. Matthew 12:40. Gundry, reading Matthew's editorial habits candidly across the commentary, treats the specific phrase 'three nights' as the element inclusive day-counting handles least comfortably: the Passion narrative's own sequence supplies at most two nights (Friday-into-Saturday, Saturday-into-Sunday) before the empty tomb is found, so the saying's precise wording sits in tension with the timeline its own Gospel narrates — a genuine difficulty rather than a fully closed question, even for scholars who accept the onah convention for the 'days' portion." }
'blasphemy-augustine-sermon71' = @{ title="A sermon that took the unforgivable sin seriously"; body="Augustine, Sermon 71 ('Sermon 21' in the Nicene and Post-Nicene Fathers renumbering), 'On the Words of the Gospel of Matthew 12:32,' trans. R. G. MacMullen, in Nicene and Post-Nicene Fathers, First Series, vol. 6, ed. Philip Schaff (Buffalo: The Christian Literature Company, 1888). Augustine devotes an entire sermon to Matthew 12:31-32, reasoning that since many who speak carelessly or even sacrilegiously against the Spirit are later forgiven and received into the church, the unforgivable blasphemy cannot be any single spoken sentence; he resolves it as persistent, unrepentant hardness of heart against grace itself — an interpretive move that both defused the verse's most alarming reading and set the terms for a long line of later pastoral anxiety over whether a given thought or doubt might count." }
'blasphemy-france-immediate-context' = @{ title="What the accusation was, in the room, that day"; body="R. T. France, The Gospel of Matthew, New International Commentary on the New Testament (Grand Rapids: Eerdmans, 2007), commentary ad loc. Matthew 12:31-32. France reads the saying in its own narrative setting rather than as a freestanding doctrine: the Pharisees have just attributed Jesus's Spirit-empowered exorcism to Satan (12:24), and the 'unforgivable' blasphemy in context is that specific act — calling the Spirit's plainly good work demonic — rather than any and every doubt, blasphemous outburst, or crisis of faith a later reader might worry qualifies, a narrower original scope than centuries of pastoral anxiety about the verse have often assumed." }
'family-fictive-kinship-guijarro' = @{ title="Cutting against a world built on family obligation"; body="Santiago Guijarro, 'The Family in First-Century Galilee,' in Constructing Early Christian Families: Family as Social Reality and Metaphor, ed. Halvor Moxnes (London: Routledge, 1997). Guijarro documents how thoroughly first-century Galilean identity, economic security, and social obligation ran through kinship networks — family was not a private sentimental unit but the basic structure of belonging and survival. Read against that background, Jesus's reply in 12:46-50 is not a mild rhetorical flourish but a genuinely disruptive redefinition, substituting shared obedience to God's will for blood as the operative bond, in a culture where cutting against family obligation carried real social cost." }
}

# —— Chapter beats (with [[NOTE:slug]] placeholders) ——
$beat1 = @'
Chapter twelve's opening dispute deserves a closer look at its actual legal machinery, because "work" was not an undefined term the Pharisees were free to stretch however they liked. By the time later rabbis codified Sabbath law formally, the Mishnah's Shabbat tractate listed thirty-nine categories of forbidden labor, and reaping sits third on that list — the same fixed list this campaign has already drawn on for a related Sabbath "carrying" dispute elsewhere in the Gospels. Plucking grain heads by hand while walking through a field is the specific act later halakhah filed under that reaping category, which is what gives the Pharisees' objection in 12:2 real legal shape rather than leaving it a vague accusation [[NOTE:grain-melachot-reaping-category]].

What Jesus offers in reply is not a rejection of that legal framework but an argument mounted inside it. Citing David eating the priests-only showbread while fleeing Saul (1 Samuel 21:1-6) and the priests' own Sabbath Temple labor under the additional-offering law (Numbers 28:9-10), he reasons from an accepted precedent and an accepted exception to a new case — a recognizable early Jewish legal pattern, structurally close to what rabbinic argument would later formalize as reasoning from a lesser case to a greater one [[NOTE:keener-halakhic-argument-form]]. Both cited precedents were themselves already-recognized cases in Second Temple and early rabbinic legal memory, not analogies invented on the spot for this scene [[NOTE:davies-allison-showbread-precedent]]. Matthew stages a halakhic dispute conducted by its own period's rules, whatever judgment a reader reaches about whether the exchange happened exactly as narrated.
'@

$beat2 = @'
This chapter's own glossary entry on Satan/Beelzebul already traces the name's descent from the Philistine god Baal-zebub named at 2 Kings 1:2 and the case some scholars make for an original, mocked-and-corrupted "Baal-zebul." The epigraphic grounding for that case is worth adding directly: Ugaritic texts recovered from the ancient city of Ugarit use the epithet zbl b'l — "Prince Baal," or "Baal the Prince" — as a genuine, attested royal-divine title for the storm-god Baal at that site centuries before either Testament was written [[NOTE:beelzebul-ugaritic-herrmann-ddd]]. Matthew's own spelling in 12:24, Beelzeboul, sits closer to that "prince" reading than to "lord of the flies," which is a real piece of internal textual evidence for the corruption theory rather than a modern imposition on it: a title of genuine ancient Near Eastern religious standing survives, flattened into an insult, inside the Pharisees' accusation against Jesus.
'@

$beat3 = @'
The "sign of Jonah" comparison (12:40) makes a specific, checkable numerical claim — "three days and three nights in the heart of the earth" — and it is worth being honest about how awkwardly that claim actually sits against this same Gospel's own Passion timeline. Crucified Friday afternoon and found risen early Sunday morning, Jesus's body occupies parts of three separate calendar days, but not three full 24-hour spans and not three literal nights. The standard harmonizing explanation appeals to the ancient Jewish onah convention, under which any part of a day-night unit could count as the whole unit, so that a late-Friday burial, all of Saturday, and part of Sunday morning satisfy "three days" without requiring anything close to seventy-two hours [[NOTE:jonah-onah-inclusive-reckoning]]. That convention handles the "days" comfortably. It handles "nights" less so: the Passion narrative supplies at most two nights before the tomb is found empty, and commentators willing to look at Matthew's editorial habits directly acknowledge that the specific detail of three nights remains the harder element to reconcile, even for those who accept the onah explanation for everything else [[NOTE:jonah-three-nights-remaining-difficulty]]. This is a genuine, long-discussed textual tension, not a solved non-issue dressed up as one.
'@

$beat4 = @'
The "blasphemy against the Holy Spirit" warning (12:31-32) has carried an unusually heavy interpretive weight across Christian history, and it is worth tracing why. Augustine devoted an entire sermon to these two verses, reasoning that since people who speak carelessly or even sacrilegiously against the Spirit are routinely later forgiven and welcomed into the church, the unforgivable blasphemy could not be any single sentence spoken in anger or ignorance; he resolved it instead as a settled, unrepentant hardness of heart against grace itself [[NOTE:blasphemy-augustine-sermon71]] — a reading that eased one anxiety even as it opened a longer one, since "am I hardened past repentance" proved a harder question to answer than "did I say a specific sentence." Read inside its own scene rather than as a freestanding doctrine, the saying has a narrower original target: the Pharisees have just called Jesus's Spirit-empowered exorcism the work of Satan (12:24), and the unforgivable act in context is that specific move — naming the Spirit's visible good work demonic — not the broader class of doubts, outbursts, or crises of faith later centuries of pastoral anxiety have folded into the same verse [[NOTE:blasphemy-france-immediate-context]].
'@

$beat5 = @'
The chapter's closing scene — Jesus's mother and brothers standing outside, and his answer redefining family around "whoever does the will of my Father" (12:46-50) — lands harder against its own period than a modern reader raised on individualist family models might register. First-century Galilean life ran on kinship obligation as its basic structural unit: economic security, social standing, and personal identity were mediated through family ties in ways a modern nuclear-family assumption doesn't fully capture [[NOTE:family-fictive-kinship-guijarro]]. Against that background, Jesus's reply is not a mild rhetorical aside but a real act of social substitution, swapping blood obligation for shared obedience as the operative bond — cutting against a value the culture around him treated as near-absolute. This book has already traced what more can be known about the brothers named at Mark 6:3 — James, Joseph, Simon, and Judas — in the entries covering the Twelve; this scene doesn't add new information about them, but it does show what Matthew thinks family is for, staged in front of the same crowd being taught the rest of the chapter.
'@

$beats = @($beat1, $beat2, $beat3, $beat4, $beat5)

# —— Glossary additions (heading -> body) ——
$glossary = [ordered]@{
'39 CATEGORIES OF SABBATH-FORBIDDEN LABOR (MELACHOT)' = "The Mishnah's Shabbat tractate (7:2) codifies thirty-nine categories (melachot) of Sabbath-forbidden labor, redacted under Rabbi Judah ha-Nasi around 200 CE — roughly a century and a half after the events this chapter narrates, though the underlying legal disputes it reflects are almost certainly older [[NOTE:grain-melachot-reaping-category]]. Reaping, the category behind the grain-plucking complaint at Matthew 12:1-8, sits third on the traditional list. Jesus's reply does not reject the framework of Sabbath-forbidden-labor categories as such; it argues from within it, citing accepted precedent (David's showbread, 1 Samuel 21:1-6) and accepted exception (priestly Temple Sabbath labor, Numbers 28:9-10) in a recognizable early Jewish legal pattern [[NOTE:keener-halakhic-argument-form]] [[NOTE:davies-allison-showbread-precedent]]."
'SIGN OF JONAH (THREE DAYS AND THREE NIGHTS)' = "Jesus's only offered ``sign' to a crowd demanding proof (12:38-42): as Jonah spent three days and three nights in the great fish, so the Son of Man will spend three days and three nights ``in the heart of the earth.' Measured against this same Gospel's own Passion timeline — crucified Friday afternoon, tomb found empty early Sunday morning — the wording claims more than the narrative straightforwardly delivers: parts of three calendar days, but not three literal nights or a full seventy-two hours. The standard harmonization appeals to the ancient Jewish onah convention, in which any part of a day-night unit counts as the whole [[NOTE:jonah-onah-inclusive-reckoning]], though even scholars who accept that convention for the ``days' generally treat the specific ``three nights' as the more strained detail [[NOTE:jonah-three-nights-remaining-difficulty]] — a genuine, still-discussed internal tension rather than a fully resolved question."
'BLASPHEMY AGAINST THE HOLY SPIRIT (UNFORGIVABLE SIN)' = "The warning that speaking against the Holy Spirit ``will not be forgiven, either in this age or in the age to come' (12:31-32), delivered immediately after the Pharisees attribute Jesus's exorcism to Beelzebul (12:24). In its own narrative setting, the plainest referent is that specific accusation — naming the Spirit's visibly good work demonic — rather than any and every doubt or blasphemous outburst a later reader might fear qualifies [[NOTE:blasphemy-france-immediate-context]]. The passage nonetheless generated substantial, long-running religious anxiety across Christian history; Augustine devoted a full sermon to resolving it as persistent, unrepentant hardness of heart rather than any single spoken sentence, a reading that eased one worry while opening a longer one about how a person could know whether they had crossed that line [[NOTE:blasphemy-augustine-sermon71]]."
}

# —— Insert Notes ——
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

# —— Insert chapter beats with placeholder replacement ——
foreach ($beatText in $beats) {
    $resolved = $beatText
    foreach ($slug in $slugToNumber.Keys) {
        $resolved = $resolved.Replace("[[NOTE:$slug]]", "[$($slugToNumber[$slug])]")
    }
    $maxChapterSortKey += 100
    $id = New-BeatRow $resolved
    Add-BeatNode $Ch12NodeId $id $maxChapterSortKey
}
Write-Host "Inserted $($beats.Count) chapter beats"

# —— Insert glossary entries ——
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

# —— Seed new entities ——
Seed-Entity "Mishnah" "mishnah" "vocabulary" "The foundational codification of Jewish oral law, redacted under Rabbi Judah ha-Nasi around 200 CE; its Shabbat tractate (7:2) supplies the classic list of 39 categories of Sabbath-forbidden labor, including the reaping category behind Matthew 12:1-8's grain-plucking dispute."

$conn.Close()
Write-Host "DONE Matthew Chapter 12 depth pass."
