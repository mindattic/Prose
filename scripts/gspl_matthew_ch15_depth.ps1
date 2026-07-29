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

# ---- Live state ----
$script:MaxNumber = [int](Exec-Scalar "SELECT MAX(Number) FROM Beats")
$NotesNodeId = [guid]"019FA01D-FA22-76C6-976C-3EA4F4D54A14"
$GlossaryNodeId = [guid]"019FA026-3758-7A57-AF65-EC1DB4303EF3"
$Ch15NodeId = [guid]"019FA06C-F68B-7BC8-96BA-0F00A6216BD7"

$maxNoteSortKey = [double](Exec-Scalar "SELECT ISNULL(MAX(bn.SortKey),0) FROM BeatNodes bn WHERE bn.NodeId='019FA01D-FA22-76C6-976C-3EA4F4D54A14' AND bn.IsEnabled=1")
$maxGlossarySortKey = [double](Exec-Scalar "SELECT ISNULL(MAX(bn.SortKey),0) FROM BeatNodes bn WHERE bn.NodeId='019FA026-3758-7A57-AF65-EC1DB4303EF3' AND bn.IsEnabled=1")
$maxChapterSortKey = [double](Exec-Scalar "SELECT ISNULL(MAX(bn.SortKey),0) FROM BeatNodes bn WHERE bn.NodeId='019FA06C-F68B-7BC8-96BA-0F00A6216BD7' AND bn.IsEnabled=1")
$maxNoteNumber = [int](Exec-Scalar "SELECT ISNULL(MAX(CAST(LEFT(b.Text, CHARINDEX(' ',b.Text)-1) AS INT)),0) FROM BeatNodes bn JOIN Beats b ON bn.BeatId=b.Id WHERE bn.NodeId='019FA01D-FA22-76C6-976C-3EA4F4D54A14' AND bn.IsEnabled=1 AND CHARINDEX(' ', b.Text) > 1 AND LEFT(b.Text, CHARINDEX(' ',b.Text)-1) NOT LIKE '%[^0-9]%'")

Write-Host "Starting: MaxNumber=$script:MaxNumber MaxNoteSortKey=$maxNoteSortKey MaxNoteNumber=$maxNoteNumber MaxGlossarySortKey=$maxGlossarySortKey MaxChapterSortKey=$maxChapterSortKey"

# ---- Notes (slug -> title/body) in order ----
$notes = [ordered]@{
'netilat-yadayim-pharisaic-innovation' = @{ title="Hand-washing before meals: a Pharisaic extension, not yet universal Torah law"; body="E. P. Sanders, Jewish Law from Jesus to the Mishnah: Five Studies (Philadelphia: Trinity Press International, 1990), essay on Pharisees and common Judaism. Sanders argues that ritual hand-washing before ordinary meals (later codified in rabbinic law as netilat yadayim) was, at this date, a Pharisaic practice and proposal rather than a Torah commandment binding on all first-century Jews; ordinary Jews outside Pharisaic circles are not shown to have observed it as obligatory, and it only becomes a fixed, widely codified requirement in the developed rabbinic legal tradition of later centuries." }
'furstenberg-purity-extension' = @{ title="Extending priestly purity to the ordinary table"; body="Yair Furstenberg, 'Defilement Penetrating the Body: A New Understanding of Contamination in Mark 7.15,' New Testament Studies 54, no. 2 (April 2008): 176-200; see also Yair Furstenberg, Purity and Identity in Ancient Judaism: From the Temple to the Mishnah (Bloomington: Indiana University Press, 2023). Furstenberg situates Pharisaic hand-washing as part of a documented, distinctive project of extending purity categories rooted in priestly, Temple-centered law to the ordinary daily meal table of laypeople, a move he distinguishes from both the plain biblical priestly purity system and from the separatist purity model practiced at Qumran." }
'corban-mishnah-nedarim' = @{ title="Vow law as its own legal specialty: tractate Nedarim"; body="Herbert Danby, trans., The Mishnah (Oxford: Oxford University Press, 1933), tractate Nedarim. The Mishnah devotes an entire tractate, Nedarim, to the detailed regulation of vows, including how a vow is validly made, under what narrow circumstances it can be released or annulled, and how a vow dedicating property or support 'to God' (qorban) removes that property from ordinary use; the sheer legal machinery devoted to the subject confirms that vow law of this general kind was a real, developed halakhic category, not an invented rhetorical target." }
'fitzmyer-qorban-inscription' = @{ title="An inscribed ossuary confirms the word was in real use"; body="Joseph A. Fitzmyer, 'The Aramaic Qorban Inscription from Jebel Hallet et-Turi and Mark 7:11/Matt 15:5,' Journal of Biblical Literature 78, no. 1 (March 1959): 60-65. Fitzmyer publishes and analyzes an Aramaic ossuary inscription from a Jerusalem-area tomb, roughly contemporary with the Gospels, that uses the term qorban to bar anyone from deriving benefit from the ossuary's contents; this is independent epigraphic evidence that qorban-vow language of exactly the kind Jesus criticizes was in genuine documented circulation in first-century Judea, not a strawman invented for the scene." }
'davies-allison-canaanite-naming' = @{ title="An archaizing ethnonym, chosen deliberately"; body="W. D. Davies and Dale C. Allison Jr., A Critical and Exegetical Commentary on the Gospel According to Saint Matthew, International Critical Commentary, vol. 2: Commentary on Matthew VIII-XVIII (Edinburgh: T&T Clark, 1991), commentary ad loc. Matthew 15:22. Davies and Allison note that Matthew's designation of the woman as 'a Canaanite' replaces Mark's contemporary, administratively current label 'a Gentile, a Syrophoenician by birth' (Mark 7:26), even though 'Canaanite' had not been a live first-century ethnic self-designation for close to a thousand years; the commentary treats the substitution as a deliberate Matthean choice reaching back into Israel's own scriptural vocabulary for its ancestral 'other,' rather than a more mundane, period-accurate ethnic-geographic label." }
'josephus-menander-tyre-solomon' = @{ title="A Tyrian court record, quoted centuries later"; body="Flavius Josephus, Against Apion, Book 1, sections 106-127 (Loeb Classical Library, trans. H. St. J. Thackeray, Cambridge, MA: Harvard University Press, 1926). Josephus quotes the Hellenistic historian Menander of Ephesus's Greek translation of Tyrian royal archives, including an account of King Hiram of Tyre's building projects and a riddle-contest exchanged with King Solomon of Jerusalem; the passage is independent, non-biblical confirmation that sustained diplomatic and economic contact between Israel and Tyre reached back to the united monarchy (cf. 1 Kings 5:1-12), centuries before this Gospel scene." }
'tyrian-shekel-temple-tax' = @{ title="A pagan city's coin, required at a Jewish altar"; body="Mishnah, Bekhorot 8:7, in Herbert Danby, trans., The Mishnah (Oxford: Oxford University Press, 1933). Rabbinic sources confirm that the annual Jerusalem Temple half-shekel tax had to be paid specifically in Tyrian currency, prized for its reliable, high silver content, even though the coin bore the pagan image of the god Melqart and was minted in Gentile Tyre; the practical requirement overrode any objection to the coin's imagery, a real, documented case of close economic entanglement running alongside religious and ethnic distance between the two populations." }
'josephus-tyre-sidon-outbreak-66ce' = @{ title="One coastal city turns violent, its neighbor does not"; body="Flavius Josephus, The Jewish War, Book 2, sections 478-479 (Loeb Classical Library, trans. H. St. J. Thackeray, Cambridge, MA: Harvard University Press, 1927). At the outbreak of the Jewish revolt in 66 CE, Josephus reports that the people of Tyre killed or imprisoned large numbers of their resident Jewish population, while neighboring Sidon (along with Antioch and Apamea) is reported to have spared its Jewish residents from violence or bondage; the contrast is a genuine, documented data point on how mixed and locally variable Jewish-Gentile relations on this specific coastline actually were, rather than a single uniform relationship of either amity or hostility." }
'davies-allison-four-thousand-doublet' = @{ title="A doublet, an independent event, or one story told twice?"; body="W. D. Davies and Dale C. Allison Jr., A Critical and Exegetical Commentary on the Gospel According to Saint Matthew, International Critical Commentary, vol. 2: Commentary on Matthew VIII-XVIII (Edinburgh: T&T Clark, 1991), commentary ad loc. Matthew 15:32-39. Davies and Allison describe the feeding of the four thousand as 'most probably a doublet' of the feeding of the five thousand in Matthew 14:13-21/Mark 6:32-44, noting the shifted details (Jesus rather than the disciples takes the initiative, a stated three-day duration, seven loaves and seven baskets rather than five and twelve, four thousand rather than five thousand); the commentary presents the live options fairly — two separate historical incidents, two originally distinct oral traditions describing the same underlying event, or one tradition split into two by the time it reached Mark's written source — without resolving the question, since Matthew and Mark (which also contains both accounts, at 6:32-44 and 8:1-10) are the only two Gospels to preserve a second feeding story at all." }
}

# ---- Chapter beats (with [[NOTE:slug]] placeholders) ----
$beat1 = @'
The opening dispute is worth slowing down on before following Jesus's counter-argument, because "the tradition of the elders" the Pharisees and scribes invoke (15:2) names something real and traceable, not a vague catch-all for "Jewish custom." Washing hands before eating an ordinary meal — what later rabbinic law codifies as netilat yadayim — is a genuinely documented Pharisaic practice of this period, but the mainstream historical-critical reading is that it was not yet a Torah commandment binding on Jews generally; it functioned as a Pharisaic proposal and marker of their own observance, one that only hardens into a fixed, universally codified requirement in the developed rabbinic legal tradition of later centuries [[NOTE:netilat-yadayim-pharisaic-innovation]]. That matters for how the scene should be read: this is not Jesus rejecting Torah, or even rejecting Judaism's purity concerns as such, but one first-century Jewish teacher declining to accept one specific school's extension of the law's scope, in a dispute recognizably internal to the Judaism of his day.

The mechanism behind that extension has its own scholarly account. Purity language and purity law in the Hebrew Bible center overwhelmingly on the Temple, its priests, and access to sacrificial worship. What the Pharisaic hand-washing practice does, on the reading argued most fully by Yair Furstenberg, is take purity categories built for that priestly, Temple-centered context and extend them outward to the ordinary daily table of laypeople who have no priestly role at all — a documented, distinctive move, and one Furstenberg distinguishes explicitly from the different, separatist purity model practiced at Qumran, where purity instead marks a boundary of withdrawal from the wider community [[NOTE:furstenberg-purity-extension]]. Jesus's own answer — that it is what comes out of a person, not what unwashed hands introduce, that defiles (15:11) — lands as a substantive position within that same live argument over how far purity law should reach, not as a dismissal of the argument's premises from outside. The underlying distinction is worth restating plainly, because it is easy to blur: a Pharisaic proposal about the ordinary meal table, however seriously some circles held it, is not the same thing as a settled, universally binding Torah requirement in the 20s-30s CE, and the mainstream historical-critical reading of this scene does not treat it as one [[NOTE:netilat-yadayim-pharisaic-innovation]].
'@

$beat2 = @'
Jesus's counter-challenge to the Pharisees and scribes is sharper than a simple accusation of hypocrisy: he names a specific legal mechanism, and it is a real one. The scenario he describes — a son declaring that support he owes his parents is instead "Corban," a gift dedicated to God, and thereby (in his telling) exempted from ever reaching them — depends on vow law being a genuine, technical area of halakhah with its own binding force, not a made-up loophole invented for the sake of the argument. The Mishnah confirms exactly that: an entire tractate, Nedarim, is devoted to the mechanics of how a vow is validly made, under what narrow conditions it can be annulled, and how dedicating something "to God" removes it from ordinary use — serious, developed legal machinery built around exactly this kind of vow [[NOTE:corban-mishnah-nedarim]]. This was never a hypothetical abstraction, either. An inscribed first-century ossuary from the Jerusalem area, published and analyzed by Joseph Fitzmyer, uses the word qorban to forbid anyone from taking benefit from what the tomb contains — independent, physical, non-biblical confirmation that the vow-dedication vocabulary Jesus is arguing against was in genuine documented use around the time of this Gospel, not a strawman built to win a rhetorical point [[NOTE:fitzmyer-qorban-inscription]].

What Jesus is doing with the example, then, is holding up a real, attested legal-technical maneuver — one built, like the hand-washing practice just discussed, on a genuine extension of religious-legal reasoning — and arguing that this particular use of it defeats its own purpose, evading the plain commandment to honor father and mother by wrapping the evasion in a properly executed vow [[NOTE:corban-mishnah-nedarim]]. His citation of Isaiah 29:13 immediately afterward, on lips that honor while the heart stays distant, functions as the general diagnosis for which the Corban maneuver is his specific, concrete case in point. The ossuary inscription's mere existence closes off any suggestion that the whole scenario was dreamed up from nothing for the sake of a clean rhetorical target; whoever first told this story was pointing at a real, exploitable feature of contemporary vow law, not an imaginary one [[NOTE:fitzmyer-qorban-inscription]].
'@

$beat3 = @'
The woman herself is worth a beat of her own, because the label Matthew gives her is doing more work than it looks like at first glance. Mark's version of this same encounter calls her "a Gentile, a Syrophoenician by birth" (Mark 7:26) — a contemporary, administratively current ethnic-geographic label of the kind an actual Roman-era resident of the Phoenician coast might plausibly have used. Matthew instead calls her "a Canaanite woman" (15:22), and by the first century "Canaanite" had not named a living ethnic group for the better part of a thousand years; the commentary tradition treats this substitution as deliberate rather than incidental, with Matthew reaching backward past Mark's mundane contemporary term for an older, scripturally loaded ethnonym that would have called to mind Israel's paradigmatic ancient enemy nations from its own founding scriptures [[NOTE:davies-allison-canaanite-naming]]. That is a real, traceable difference in how the two Gospels choose to name the same woman in the same encounter — and it is evidence about Matthew's own literary and theological purposes in shaping his material, not evidence about what this particular woman was actually called by her own neighbors on the Phoenician coast. Davies and Allison make the same point when working systematically through Matthew's departures from Mark across this whole pericope, treating the naming shift as one clue among several to a broader, identifiable Matthean redactional program rather than an isolated coincidence [[NOTE:davies-allison-canaanite-naming]].
'@

$beat4 = @'
That the encounter happens at all in "the district of Tyre and Sidon" is itself worth grounding, because the relationship between Jews and these Phoenician coastal cities was neither simple hostility nor simple kinship — it was a genuinely long, mixed, and well-documented history. Non-biblical confirmation of that history goes back centuries before this scene: Josephus quotes the Hellenistic historian Menander of Ephesus's translation of Tyre's own royal archives, including an account of King Hiram of Tyre exchanging riddles and building projects with King Solomon of Jerusalem, independent testimony for the sustained diplomatic and trade relationship the Bible itself describes at 1 Kings 5:1-12 [[NOTE:josephus-menander-tyre-solomon]]. That economic entanglement persisted for centuries and cut in a genuinely surprising direction: by the Roman period, the Jerusalem Temple's own half-shekel tax had to be paid specifically in Tyrian coinage, prized for its reliable silver content, even though the coin carried the image of a pagan god and was minted in Gentile territory — practical economic necessity overriding religious preference in a small but telling way [[NOTE:tyrian-shekel-temple-tax]].

None of that erased real distance and, at moments, real hostility. Three decades or so after this scene, at the outbreak of the Jewish revolt in 66 CE, Josephus reports that the population of Tyre turned violently on its resident Jewish community, killing or imprisoning large numbers — while neighboring Sidon, by contrast, spared its own Jewish residents from harm entirely [[NOTE:josephus-tyre-sidon-outbreak-66ce]]. That contrast between two neighboring coastal cities is a useful corrective on its own: this border region was not one uniform relationship of either amity or enmity but a genuinely local, city-by-city, and variable one, which is exactly the kind of complicated, real-world backdrop against which a Jewish teacher's border-crossing encounter with a local woman reads as a notable, noteworthy departure rather than a routine trip. Trade partnership across generations, a shared coin at the Temple altar, and lethal rioting a lifetime later are not contradictions to be resolved into a single tidy verdict; they are simply what a real, long, mixed relationship between two peoples actually looks like on the ground [[NOTE:tyrian-shekel-temple-tax]].
'@

$beat5 = @'
The feeding of four thousand people from seven loaves and a few fish, with seven baskets of fragments left over (15:32-38), is not this Gospel's first version of a mass-feeding story — chapter fourteen already gave a feeding of five thousand from five loaves and two fish, with twelve baskets left over. That two such similar stories sit inside the same Gospel, a chapter and a half apart, without either Jesus, the disciples, or the narrator remarking on the resemblance, is a genuine and openly debated point among Synoptic-source scholars, distinct from the broader question (already covered in this book's discussion of the feeding tradition in John's Gospel) of whether the feeding-miracle tradition generally is multiply attested across independent sources. The specific question here is narrower: are Matthew's two feedings two separate historical incidents, two originally independent oral traditions describing what was actually one underlying event, or a single tradition that had already split into two forms by the time it reached the written source Matthew and Mark both drew on? The standard critical commentary calls the second feeding "most probably a doublet" of the first, pointing to the shifted details — Jesus rather than the disciples takes the initiative this time, a stated three-day duration is added, the loaf and basket counts change, and the crowd size drops from five thousand to four thousand — while presenting all three explanations as live options rather than declaring the matter closed [[NOTE:davies-allison-four-thousand-doublet]]. It is worth noting, too, that Matthew and Mark are the only two Gospels that preserve two feeding accounts at all; Luke and John each give only one, which is itself a data point cutting toward the doublet reading without settling it outright [[NOTE:davies-allison-four-thousand-doublet]]. As with the feeding of the five thousand, this is the same category of claim this book has flagged since chapter eight: not verifiable and not falsifiable from the outside, only traceable in how the sources that report it relate to one another.
'@

$beats = @($beat1, $beat2, $beat3, $beat4, $beat5)

# ---- Glossary additions (heading -> body) ----
$glossary = [ordered]@{
'NETILAT YADAYIM (RITUAL HAND-WASHING)' = "The later rabbinic name for ritual washing of the hands before eating an ordinary meal, the practice underlying the Pharisees' and scribes' complaint that Jesus's disciples eat with unwashed hands (15:2). At this date the practice is well documented as a Pharisaic extension of priestly-purity thinking to the ordinary table, but mainstream historical-critical scholarship holds it was not yet a universally binding Torah commandment on all first-century Jews — it becomes a fixed, widely codified requirement only in the fuller rabbinic legal tradition of later centuries [[NOTE:netilat-yadayim-pharisaic-innovation]] [[NOTE:furstenberg-purity-extension]]."
'CORBAN (VOW-LAW LOOPHOLE)' = "The Aramaic/Hebrew term for a vow dedicating property or support 'to God,' cited by Jesus as a real, documented halakhic mechanism some used to declare resources unavailable to support their own parents, in technical tension with the commandment to honor father and mother (15:5-6). The Mishnah's tractate Nedarim regulates this exact category of vow law in detail [[NOTE:corban-mishnah-nedarim]], and an inscribed first-century Jerusalem-area ossuary independently confirms the vocabulary was in genuine contemporary use, not an invented rhetorical target [[NOTE:fitzmyer-qorban-inscription]]."
'CANAANITE WOMAN VS. SYROPHOENICIAN WOMAN (MATTHEW VS. MARK NAMING)' = "The unnamed mother who pleads with Jesus on behalf of her demon-afflicted daughter in the district of Tyre and Sidon (15:21-28), called 'a Canaanite woman' by Matthew (15:22) but 'a Gentile, a Syrophoenician by birth' by Mark's version of the same encounter (Mark 7:26). 'Canaanite' had not been a live first-century ethnic self-designation, unlike Mark's contemporary administrative label; commentary treats Matthew's choice as a deliberate reach back into Israel's own scriptural vocabulary for its ancestral 'other,' a traceable editorial difference between the two Gospels rather than a claim about what she was actually called by her own community [[NOTE:davies-allison-canaanite-naming]]."
}

# ---- Insert Notes ----
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

# ---- Insert chapter beats with placeholder replacement ----
foreach ($beatText in $beats) {
    $resolved = $beatText
    foreach ($slug in $slugToNumber.Keys) {
        $resolved = $resolved.Replace("[[NOTE:$slug]]", "[$($slugToNumber[$slug])]")
    }
    $maxChapterSortKey += 100
    $id = New-BeatRow $resolved
    Add-BeatNode $Ch15NodeId $id $maxChapterSortKey
}
Write-Host "Inserted $($beats.Count) chapter beats"

# ---- Insert glossary entries ----
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

# ---- Seed new entities ----
Seed-Entity "Netilat Yadayim (Ritual Hand-Washing)" "netilat-yadayim-ritual-hand-washing" "vocabulary" "Later rabbinic name for ritual hand-washing before eating an ordinary meal; a documented Pharisaic extension of priestly purity practice, not yet universally binding Torah law in the first century."
Seed-Entity "Hiram I of Tyre" "hiram-i-of-tyre" "character" "King of Tyre and contemporary of Solomon of Jerusalem; sustained diplomatic and trade partner of the united Israelite monarchy per 1 Kings 5 and Josephus's quotation of Menander of Ephesus's Tyrian royal records."
Seed-Entity "Menander of Ephesus" "menander-of-ephesus" "character" "Hellenistic historian whose Greek translation of Tyrian royal archives, quoted by Josephus in Against Apion, independently confirms the Solomon-Hiram relationship and other Tyre-Israel contact."

$conn.Close()
Write-Host "DONE Matthew Chapter 15 depth pass."
