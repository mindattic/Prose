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
$Ch5NodeId = [guid]"019FA064-CBA6-7FF6-828B-72094212CB22"
$NotesNodeId = [guid]"019FA01D-FA22-76C6-976C-3EA4F4D54A14"
$GlossaryNodeId = [guid]"019FA026-3758-7A57-AF65-EC1DB4303EF3"

$maxNoteSortKey = [double](Exec-Scalar "SELECT ISNULL(MAX(bn.SortKey),0) FROM BeatNodes bn WHERE bn.NodeId='019FA01D-FA22-76C6-976C-3EA4F4D54A14' AND bn.IsEnabled=1")
$maxGlossarySortKey = [double](Exec-Scalar "SELECT ISNULL(MAX(bn.SortKey),0) FROM BeatNodes bn WHERE bn.NodeId='019FA026-3758-7A57-AF65-EC1DB4303EF3' AND bn.IsEnabled=1")
$maxNoteNumber = [int](Exec-Scalar "SELECT ISNULL(MAX(CAST(LEFT(b.Text, CHARINDEX(' ',b.Text)-1) AS INT)),0) FROM BeatNodes bn JOIN Beats b ON bn.BeatId=b.Id WHERE bn.NodeId='019FA01D-FA22-76C6-976C-3EA4F4D54A14' AND bn.IsEnabled=1 AND CHARINDEX(' ', b.Text) > 1 AND LEFT(b.Text, CHARINDEX(' ',b.Text)-1) NOT LIKE '%[^0-9]%'")
$maxCh5SortKey = [double](Exec-Scalar "SELECT ISNULL(MAX(bn.SortKey),0) FROM BeatNodes bn WHERE bn.NodeId='019FA064-CBA6-7FF6-828B-72094212CB22' AND bn.IsEnabled=1")

Write-Host "Starting: MaxNumber=$script:MaxNumber MaxNoteSortKey=$maxNoteSortKey MaxNoteNumber=$maxNoteNumber MaxGlossarySortKey=$maxGlossarySortKey MaxCh5SortKey=$maxCh5SortKey"

# ---- Notes (slug -> title/body) in order ----
$notes = [ordered]@{
'ashrei-wisdom-blessing-form' = @{ title="Blessed is... the Beatitudes' Hebrew Bible genre"; body="W.D. Davies and Dale C. Allison Jr., A Critical and Exegetical Commentary on the Gospel According to Saint Matthew, International Critical Commentary, vol. 1 (Edinburgh: T&T Clark, 1988), commentary ad loc. Matthew 5:3-12. Davies and Allison trace the Beatitudes' `"Blessed are...`" form directly to the Hebrew Bible's own wisdom-literature ashrei (`"happy/blessed is...`") formula, exemplified by Psalm 1:1 (`"Blessed is the man who walks not in the counsel of the wicked...`") and Psalm 32:1, among many other wisdom and psalmic instances; Matthew is working within an existing, recognized Israelite literary form for commending a way of life, not inventing a new genre from nothing." }
'beatitude-person-shift-redaction' = @{ title="A grammatical seam: where the traditional form ends"; body="W.D. Davies and Dale C. Allison Jr., A Critical and Exegetical Commentary on the Gospel According to Saint Matthew, ICC, vol. 1 (Edinburgh: T&T Clark, 1988), commentary ad loc. Matthew 5:11-12. The first eight Beatitudes (5:3-10) are cast uniformly in third-person plural form (`"Blessed are the... for theirs is/they shall...`"), matching the traditional ashrei pattern; the ninth, on persecution (5:11-12), switches abruptly to direct second-person address (`"Blessed are you when others revile you...`"). Commentators read this grammatical seam as a visible boundary between an inherited traditional core and Matthew's own expanded, community-directed application of it." }
'betz-luke-woes-structure' = @{ title="Four blessings, four woes — and no woes in Matthew at all"; body="Hans Dieter Betz, The Sermon on the Mount: A Commentary on the Sermon on the Mount, Including the Sermon on the Plain (Matthew 5:3-7:27 and Luke 6:20-49), Hermeneia series (Minneapolis: Fortress Press, 1995), commentary ad loc. Luke 6:20-26. Luke's Sermon on the Plain pairs its four blessings (`"Blessed are you poor... you who hunger... you who weep... you when people hate you`") with four matching woes (`"But woe to you who are rich... who are full... who laugh... when all speak well of you`"), a balanced blessing/curse structure Betz situates within conventions of Hellenistic moral diatribe. Matthew's version keeps the blessings — expanded to eight or nine — but drops the woes entirely, a real compositional divergence between the two versions of the same underlying sermon tradition, not a minor stylistic variant." }
'guelich-beatitudes-compositional-debate' = @{ title="How much of the eight goes back to one common list?"; body="Robert A. Guelich, `"The Matthean Beatitudes: 'Entrance Requirements' or Eschatological Blessings?`" Journal of Biblical Literature 95, no. 3 (1976): 415-434. Guelich's classic treatment surveys the ongoing scholarly disagreement over how many of Matthew's eight or nine Beatitudes derive from a shared core tradition with Luke's four (usually reconstructed as part of the hypothetical Q source) versus how many are Matthew's own theological expansion; proposals for the size of the common core have ranged from as few as three or four beatitudes to as many as seven, and the question remains genuinely unresolved rather than settled in either direction." }
'nolland-poor-in-spirit-debate' = @{ title="'Poor in spirit,' plainly 'poor' — one word, two readings"; body="John Nolland, The Gospel of Matthew: A Commentary on the Greek Text, New International Greek Testament Commentary (Grand Rapids: Eerdmans, 2005), commentary ad loc. Matthew 5:3. Nolland lays out the two live scholarly explanations for why Matthew's first Beatitude blesses `"the poor in spirit`" while Luke 6:20 simply blesses `"the poor`": either Matthew has spiritualized an originally economic saying about literal material poverty into an inward disposition of humility before God, or each evangelist independently drew out the emphasis that mattered most to his own community from a genuinely ambiguous or already dual-natured underlying tradition. Nolland treats neither explanation as clearly established over the other." }
'fitzmyer-luke-poor-economic-theme' = @{ title="Luke's poor stay poor — a programmatic theme, not an aside"; body="Joseph A. Fitzmyer, The Gospel According to Luke I-IX, Anchor Bible vol. 28 (Garden City, NY: Doubleday & Company, 1981), commentary ad loc. Luke 6:20. Fitzmyer situates Luke's unadorned `"Blessed are you poor`" within a running Lucan concern for the economically poor and dispossessed threaded through the whole Gospel — Mary's song that God `"has filled the hungry with good things`" (1:53), Jesus's own inaugural reading about `"good news to the poor`" (4:18), and the parable of the rich man and Lazarus (16:19-31) — evidence, on this reading, that Luke's `"poor`" is deliberately literal rather than a truncation of Matthew's fuller, more spiritual phrase." }
'critical-edition-q-poor-reconstruction' = @{ title="What the reconstructed Q text actually says"; body="James M. Robinson, Paul Hoffmann, and John S. Kloppenborg, eds., The Critical Edition of Q: A Synopsis, Including the Gospels of Matthew and Luke, Mark and Thomas, with English, German, and French Translations, Hermeneia supplement (Minneapolis: Fortress Press / Leuven: Peeters, 2000), reconstruction of Q 6:20b. The International Q Project's critical reconstruction of the hypothetical shared source underlying Matthew and Luke's overlapping sayings material reconstructs the base text of the first Beatitude as `"Blessed are you poor,`" without `"in spirit`" — meaning the editorial team's best reconstruction of the common source Matthew and Luke each drew on favors Luke's simpler wording as closer to the shared original, with Matthew's `"in spirit`" read as the later addition." }
'mishnah-avot-fence-around-torah' = @{ title="'Make a fence around the Torah'"; body="Herbert Danby, trans., The Mishnah (Oxford: Oxford University Press, 1933), tractate Avot 1:1. The Mishnah records the maxim of `"the Men of the Great Assembly`": `"Be deliberate in judgement, raise up many disciples, and make a fence around the Torah`" — an explicit, attested rabbinic principle that setting a stricter personal practice around a Torah command, well short of the point of actual violation, protects against ever breaking the command itself. This hedge-building logic is the closest attested first-century-adjacent Jewish parallel to the antitheses' own rhetorical move." }
'davies-setting-sermon-antitheses-hedge' = @{ title="Intensification, not abolition — reading the antitheses as a fence"; body="W.D. Davies, The Setting of the Sermon on the Mount (Cambridge: Cambridge University Press, 1964), chapter on the antitheses and their relationship to contemporary Jewish legal reasoning. Davies argues the six antitheses of 5:21-48 function structurally like the rabbinic fence-building principle applied by Jesus to his own teaching: forbidding anger and contempt builds a hedge around the command against murder, forbidding lust builds a hedge around the command against adultery, forbidding all oath-taking builds a hedge around the command against false oaths. On this reading, the `"but I say to you`" formula intensifies observance of the named commandments rather than replacing or abolishing them." }
'keener-angareia-term' = @{ title="A specific Greek verb for a specific Roman practice"; body="Craig S. Keener, A Commentary on the Gospel of Matthew (Grand Rapids: Eerdmans, 1999), commentary ad loc. Matthew 5:41. Keener identifies the verb translated `"forces you to go`" (angareuei) as a technical term, borrowed into Greek from a Persian loanword for state courier-service, for the attested Roman practice of legally compelling a civilian to carry a soldier's pack or equipment for a fixed distance — one Roman mile, the exact unit the verse names — without payment or the civilian's consent." }
'epictetus-angareia-parallel' = @{ title="A contemporary complaint about the same practice"; body="Epictetus, Discourses, Book 4, chapter 1, section 79 (Loeb Classical Library, trans. W.A. Oldfather, Cambridge, MA: Harvard University Press, 1925). The Stoic philosopher Epictetus, writing within decades of the Gospels, advises a student on how to respond `"if there is a press [angareia] and a soldier lays hold of`" his donkey: let it go rather than resist and be beaten. The passage is independent, non-Christian, first-century attestation that impressment of civilians and their property for exactly this kind of short-distance forced service was a real, recognized, and resented feature of everyday life under Roman military occupation." }
'bdag-angareuo-simon-cyrene-echo' = @{ title="The same rare verb, used twice in one Gospel"; body="Frederick William Danker, ed., A Greek-English Lexicon of the New Testament and Other Early Christian Literature (BDAG), 3rd ed. (Chicago: University of Chicago Press, 2000), entry for angareuo. The lexicon notes that the same uncommon verb Matthew uses for the soldier's demand in 5:41 recurs in only one other place in his Gospel: Matthew 27:32, where Simon of Cyrene is `"compelled`" (angareuousin) to carry Jesus's cross. Matthew's own vocabulary quietly links the Sermon's teaching about excess compliance under compulsion to the Passion narrative's own scene of forced labor, whether or not the echo is deliberate." }
'wink-second-mile-nonviolent-excess' = @{ title="Reading the second mile as leverage, not surrender"; body="Walter Wink, Engaging the Powers: Discernment and Resistance in a World of Domination (Minneapolis: Fortress Press, 1992), chapter on Matthew 5:38-41, `"Jesus' Third Way.`" Wink argues the instruction to go a second mile does not counsel passive submission but a specific, locally legible act of nonviolent excess: exceeding the one-mile legal limit put the Roman soldier himself at risk of disciplinary censure for over-requisitioning labor, so voluntarily walking on past the mile marker turned an act of imposed humiliation into a gesture that momentarily left the soldier, not the civilian, at the mercy of the other's choice." }
'tigay-oath-law-torah-regulates' = @{ title="The Torah regulates oaths — it does not forbid them"; body="Jeffrey H. Tigay, Deuteronomy, JPS Torah Commentary (Philadelphia: Jewish Publication Society, 1996), commentary ad loc. Deuteronomy 23:21-23 (with cross-reference to Numbers 30:2). Tigay's commentary confirms that the Torah's actual oath legislation — both Numbers 30:2 (`"if a man vows a vow to the LORD, or swears an oath to bind himself by a pledge, he shall not break his word`") and Deuteronomy 23:21-23 — assumes vows and oaths are a normal, legitimate practice and regulates how promptly and faithfully they must be fulfilled once made; neither passage forbids oath-taking itself, which makes Jesus's blanket `"do not swear at all`" (5:34) a step beyond, not a restatement of, the Torah's own actual rule." }
'josephus-essenes-oath-avoidance' = @{ title="A group that already refused to swear, on principle"; body="Flavius Josephus, The Jewish War, Book 2, chapter 8, section 6 (section 135 in the standard Niese/Loeb numbering) (Loeb Classical Library, trans. H. St. J. Thackeray, Cambridge, MA: Harvard University Press, 1927). Josephus reports that the Essenes, as a matter of communal principle, avoided swearing oaths in ordinary speech, holding that a man whose plain word could not be trusted without invoking God's name was already condemned regardless — while still requiring solemn oaths of loyalty at initiation into the community. The Essenes stand as a genuine, independently attested first-century Jewish precedent for treating the avoidance of everyday oath-swearing as a matter of principle rather than a uniquely Christian innovation." }
'davies-allison-james-512-parallel' = @{ title="The same teaching, echoed in an early Christian letter"; body="W.D. Davies and Dale C. Allison Jr., A Critical and Exegetical Commentary on the Gospel According to Saint Matthew, ICC, vol. 1 (Edinburgh: T&T Clark, 1988), commentary ad loc. Matthew 5:34-37. Davies and Allison note the letter of James closely echoes this teaching — `"let your yes be yes and your no be no`" (James 5:12) nearly reproduces Matthew 5:37's own wording — while showing enough independent variation in phrasing that most commentators read it as evidence of a widely shared early tradition against oath-swearing circulating in more than one early Christian community, rather than James simply copying Matthew's finished text." }
}

# ---- Chapter beats (with [[NOTE:slug]] placeholders) ----
$beat1 = @'
The Beatitudes' own form is worth naming precisely, because Matthew isn't inventing a genre out of nothing. "Blessed are..." (in Greek, makarios) directly echoes the Hebrew Bible's own wisdom-literature ashrei ("happy is," "blessed is") formula — the same construction that opens the entire Psalter: "Blessed is the man who walks not in the counsel of the wicked" (Psalm 1:1), and recurs throughout the wisdom and psalmic tradition (Psalm 32:1, among many others) [[NOTE:ashrei-wisdom-blessing-form]]. Matthew is working comfortably inside an existing, recognized Israelite literary form for commending a way of life as the truly flourishing one, not improvising a new rhetorical shape for the occasion.

That said, the eight (or nine, depending how the last one is counted) Beatitudes aren't uniform in construction, and the seam is visible on the page. The first eight (5:3-10) hold to the traditional third-person form — "Blessed are the... for theirs is/they shall..." — while the final one, on persecution (5:11-12), switches abruptly into direct second-person address: "Blessed are you when others revile you and persecute you...". Commentators read that grammatical shift as marking a real boundary between an inherited traditional core and Matthew's own expanded, community-facing application layered onto it [[NOTE:beatitude-person-shift-redaction]].

Set against Luke's version, the divergence sharpens further. Luke's Sermon on the Plain pairs four blessings with four matching woes — "Blessed are you poor... woe to you who are rich," "Blessed are you who hunger... woe to you who are full," and so on through weeping/laughing and hatred/approval (Luke 6:20-26) — a balanced blessing-and-curse structure with recognized parallels in Hellenistic moral rhetoric. Matthew keeps the blessings, expands them to eight or nine, and drops the woes entirely [[NOTE:betz-luke-woes-structure]]. That's a genuine compositional difference between the two versions of what both traditions treat as the same founding sermon, not a cosmetic variation in wording. Exactly how much of Matthew's expanded list goes back to a common source shared with Luke — usually reconstructed as part of the hypothetical Q material — versus how much is Matthew's own theological addition remains a live, unresolved question in the scholarship, with proposals for the size of that common core ranging from as few as three or four beatitudes up to as many as seven [[NOTE:guelich-beatitudes-compositional-debate]]. This book won't pretend that question has a settled answer, because it doesn't.
'@

$beat2 = @'
One single-word divergence between the two versions has drawn more sustained scholarly attention than almost any other line in the sermon: Matthew's first Beatitude blesses "the poor in spirit" (5:3), while Luke's parallel blesses simply "the poor" (6:20), full stop. The range of explanations on offer is worth laying out fairly rather than picking a side. One reading holds that Matthew has spiritualized an originally economic saying about literal material poverty into an inward disposition of humility before God; the other holds that each evangelist independently drew out whichever emphasis mattered most to his own community from an underlying tradition that may already have carried some ambiguity, and that neither version is simply a corruption of the other [[NOTE:nolland-poor-in-spirit-debate]].

Luke's plainer wording doesn't stand alone in his Gospel — it sits inside a running Lucan concern for the literally poor and dispossessed that recurs across the whole book: Mary's song that God "has filled the hungry with good things" (1:53), Jesus's own inaugural synagogue reading about "good news to the poor" (4:18), and the parable of the rich man and Lazarus later on (16:19-31). Read against that pattern, Luke's unadorned "the poor" looks like a deliberate, consistent theme rather than an accidental omission of Matthew's fuller phrase [[NOTE:fitzmyer-luke-poor-economic-theme]]. For its part, the most widely used scholarly reconstruction of the hypothetical shared source underlying both Gospels' overlapping material reconstructs the base text of this Beatitude as "Blessed are you poor," without "in spirit" — meaning the editorial judgment behind that reconstruction favors Luke's simpler wording as closer to whatever the two evangelists both started from, with Matthew's addition read as the later development [[NOTE:critical-edition-q-poor-reconstruction]]. That reconstruction is itself a scholarly argument, not a recovered manuscript — Q does not survive as a physical text — so even this data point is a considered judgment rather than direct proof of which version came first.
'@

$beat3 = @'
The six antitheses that follow — "You have heard that it was said... but I say to you" — aren't Jesus improvising a wholly novel form of moral reasoning either. Jewish legal tradition of the following centuries preserves an explicit, named principle for exactly this kind of move: the Mishnah records the maxim attributed to "the Men of the Great Assembly" to "be deliberate in judgement, raise up many disciples, and make a fence around the Torah" (m. Avot 1:1) — setting a stricter personal practice safely inside the boundary of a command, so that the command itself is never actually broken [[NOTE:mishnah-avot-fence-around-torah]].

Read against that principle, the antitheses' structure snaps into focus. Forbidding anger and contempt (5:21-22) builds a hedge around the commandment against murder; forbidding lust (5:27-28) builds a hedge around the commandment against adultery; forbidding all oath-taking (5:33-37) builds a hedge around the commandment against false oaths. On this reading — argued at length in W.D. Davies's classic study of the sermon's setting — "but I say to you" intensifies observance of the named commandments by moving the line of acceptable conduct further back from the violation, rather than replacing or overturning the commandments themselves [[NOTE:davies-setting-sermon-antitheses-hedge]]. This doesn't settle the question this book has already flagged as open elsewhere in this chapter — whether 5:17-20 reflects Jesus's own emphasis or Matthew's community defending itself — but it does supply the closest attested Jewish parallel available for how a first-century teacher could plausibly intensify Torah commands while still insisting, in the same breath, that not one stroke of the law is being set aside.
'@

$beat4 = @'
Two of the six antitheses point to specific, documented features of first-century Roman rule rather than abstract ethical positions, and both are worth tracing in concrete detail.

"If anyone forces you to go one mile, go with him two miles" (5:41) names a real legal mechanism, not a generic metaphor for imposition. The verb translated "forces" (angareuei) is a technical term — borrowed into Greek from a Persian word for the royal courier-relay system — for the attested Roman practice of legally compelling a civilian to carry a soldier's pack or equipment for a fixed distance, without payment or consent [[NOTE:keener-angareia-term]]. The distance itself is exact: one Roman mile, the same unit the verse names, and the same practice shows up independently in a non-Christian source from the same general period — the Stoic philosopher Epictetus advises a student on how to respond "if there is a press and a soldier lays hold of" his donkey: let it go rather than resist and risk a beating [[NOTE:epictetus-angareia-parallel]]. The same rare verb resurfaces exactly once more in Matthew's own Gospel: Simon of Cyrene is "compelled" (angareuousin) to carry Jesus's cross at 27:32, an echo in Matthew's own vocabulary between the Sermon's teaching on excess compliance and the Passion's scene of forced labor [[NOTE:bdag-angareuo-simon-cyrene-echo]]. Read against that background, "go with him two miles" isn't passive resignation — one influential modern reading argues that voluntarily exceeding the one-mile legal limit put the requisitioning soldier himself at risk of censure for over-requisitioning labor, turning an act of imposed humiliation into a moment where the soldier, not the civilian, was left at the mercy of someone else's choice [[NOTE:wink-second-mile-nonviolent-excess]].

The oath-swearing prohibition a few verses earlier (5:33-37) deserves the same precision. "Do not swear at all" sounds like it's overturning Torah law, but the Torah's actual legislation on oaths — Numbers 30:2 and Deuteronomy 23:21-23 — assumes vow-making and oath-taking are normal, legitimate practices and regulates how faithfully and promptly they must be kept once made; neither passage forbids the act of swearing itself [[NOTE:tigay-oath-law-torah-regulates]]. Jesus's own instruction goes further than that existing law, not simply back to it. And it isn't unprecedented, either: Josephus, describing the Essenes as a contemporary eyewitness, records that the group avoided oath-swearing in ordinary speech as a matter of principle, holding that anyone whose plain word needed God's name behind it to be believed was already condemned regardless — while still requiring solemn oaths at initiation into the community itself [[NOTE:josephus-essenes-oath-avoidance]]. And the teaching didn't stay contained to this one text: the New Testament letter of James closely echoes it — "let your yes be yes and your no be no" (James 5:12) nearly reproduces Matthew 5:37's own phrasing, with just enough independent variation that most commentators read it as evidence of a shared early tradition against oath-swearing circulating in more than one early community, rather than James simply copying Matthew's finished text [[NOTE:davies-allison-james-512-parallel]].
'@

$beats = @($beat1, $beat2, $beat3, $beat4)

# ---- Glossary additions (heading -> body) ----
$glossary = [ordered]@{
'BEATITUDE (ASHREI FORM)' = "The Hebrew Bible's own wisdom-literature ashrei (`"happy is,`" `"blessed is`") blessing formula, exemplified by Psalm 1:1 and Psalm 32:1, and the recognized literary genre Matthew's eight (or nine) Beatitudes work within rather than invent (5:3-12). The form's first eight instances share a uniform third-person construction; the final, on persecution, shifts to direct second-person address, a grammatical seam commentators read as marking Matthew's own redactional expansion of an inherited core [[NOTE:ashrei-wisdom-blessing-form]] [[NOTE:beatitude-person-shift-redaction]]."
'ANGAREIA (ROMAN IMPRESSMENT PRACTICE)' = "The attested Roman legal practice, named by a Greek verb (angareuo) borrowed from a Persian term for state courier-relay service, of compelling a civilian to carry a soldier's pack or equipment for a fixed distance — one Roman mile — without payment or consent. Named directly in Matthew 5:41's `"if anyone forces you to go one mile,`" independently attested in the same period by the Stoic philosopher Epictetus, and used again by Matthew himself for Simon of Cyrene's forced carrying of Jesus's cross (27:32) [[NOTE:keener-angareia-term]] [[NOTE:epictetus-angareia-parallel]] [[NOTE:bdag-angareuo-simon-cyrene-echo]]."
'ESSENES' = "A first-century Jewish sect described by the contemporary historian Flavius Josephus, closely associated by most scholars (though not identical by name) with the wilderness community that produced the Dead Sea Scrolls at Qumran. Josephus records that the Essenes avoided oath-swearing in ordinary speech as a matter of communal principle, holding their plain word to be as binding as any oath — a genuine, independently attested first-century Jewish parallel to Jesus's own blanket prohibition on oath-taking in this chapter (5:33-37), while the Essenes still required solemn oaths of initiates joining the community [[NOTE:josephus-essenes-oath-avoidance]]."
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
$sortKey = $maxCh5SortKey
foreach ($beatText in $beats) {
    $resolved = $beatText
    foreach ($slug in $slugToNumber.Keys) {
        $resolved = $resolved.Replace("[[NOTE:$slug]]", "[$($slugToNumber[$slug])]")
    }
    $sortKey += 100
    $id = New-BeatRow $resolved
    Add-BeatNode $Ch5NodeId $id $sortKey
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
Seed-Entity "Beatitude (Ashrei Form)" "beatitude-ashrei-form" "vocabulary" "Hebrew Bible wisdom-literature blessing formula (Psalm 1:1, 32:1) that Matthew's Beatitudes work within as an existing literary genre."
Seed-Entity "Angareia (Roman Impressment Practice)" "angareia-roman-impressment-practice" "vocabulary" "Attested Roman legal practice of compelling a civilian to carry a soldier's pack one Roman mile without pay or consent; named in Matthew 5:41 and 27:32."
Seed-Entity "Essenes" "essenes" "faction" "First-century Jewish sect described by Josephus, closely associated with the Qumran community, notable for avoiding ordinary oath-swearing as a matter of principle."

$conn.Close()
Write-Host "DONE Matthew Chapter 5 depth pass."
