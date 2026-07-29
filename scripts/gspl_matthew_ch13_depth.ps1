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
$Ch13NodeId = [guid]"019FA06B-7580-76BD-93D9-2ADDCEE9AF4C"

$maxNoteSortKey = [double](Exec-Scalar "SELECT ISNULL(MAX(bn.SortKey),0) FROM BeatNodes bn WHERE bn.NodeId='019FA01D-FA22-76C6-976C-3EA4F4D54A14' AND bn.IsEnabled=1")
$maxGlossarySortKey = [double](Exec-Scalar "SELECT ISNULL(MAX(bn.SortKey),0) FROM BeatNodes bn WHERE bn.NodeId='019FA026-3758-7A57-AF65-EC1DB4303EF3' AND bn.IsEnabled=1")
$maxChapterSortKey = [double](Exec-Scalar "SELECT ISNULL(MAX(bn.SortKey),0) FROM BeatNodes bn WHERE bn.NodeId='019FA06B-7580-76BD-93D9-2ADDCEE9AF4C' AND bn.IsEnabled=1")
$maxNoteNumber = [int](Exec-Scalar "SELECT ISNULL(MAX(CAST(LEFT(b.Text, CHARINDEX(' ',b.Text)-1) AS INT)),0) FROM BeatNodes bn JOIN Beats b ON bn.BeatId=b.Id WHERE bn.NodeId='019FA01D-FA22-76C6-976C-3EA4F4D54A14' AND bn.IsEnabled=1 AND CHARINDEX(' ', b.Text) > 1 AND LEFT(b.Text, CHARINDEX(' ',b.Text)-1) NOT LIKE '%[^0-9]%'")

Write-Host "Starting: MaxNumber=$script:MaxNumber MaxNoteSortKey=$maxNoteSortKey MaxNoteNumber=$maxNoteNumber MaxGlossarySortKey=$maxGlossarySortKey MaxChapterSortKey=$maxChapterSortKey"

# ---- Notes (slug -> title/body) in order ----
$notes = [ordered]@{
'mashal-rabbinic-parable-form' = @{ title="The mashal — a genre Jesus didn't invent"; body="David Stern, Parables in Midrash: Narrative and Exegesis in Rabbinic Literature (Cambridge, MA: Harvard University Press, 1991), esp. chs. 1-2 on the mashal's narrative structure and interpretive function. Stern's landmark study of the rabbinic mashal — the short, realistic narrative paired with a nimshal (application), preserved by the hundreds in the later classical Midrash and the Talmud — describes a genre built around exactly the kind of everyday material Jesus reaches for in this chapter: a sower, a woman baking, a king settling accounts, a merchant, a fisherman, each deployed to illuminate a point about God, Israel, or judgment. Stern's evidence mostly postdates Jesus by a century or more, so the claim is not that he borrowed any specific surviving rabbinic mashal, only that he was working inside a real, indigenous, widely practiced Jewish teaching form rather than improvising an unprecedented rhetorical device." }
'brad-young-jewish-parable-roots' = @{ title="Reading the parables against their Jewish frame"; body="Brad H. Young, Jesus and His Jewish Parables: Rediscovering the Roots of Jesus' Teaching (New York/Mahwah, NJ: Paulist Press, 1989), esp. Part One on the mashal's rabbinic background and Part Two's parable-by-parable comparative readings. Young, writing from within the 'Jewish roots' school of historical Jesus scholarship, argues Jesus's parables read more naturally against the conventions of rabbinic meshalim than against Greco-Roman rhetorical fable — a framing choice with real interpretive consequences, since rabbinic meshalim routinely assign meaning to more than one narrative detail at once (a king figure standing for God, a son for Israel, an unpaid debt for sin) rather than making a single isolated comparison." }
'julicher-single-point-thesis' = @{ title="One point, not an allegory — Jülicher's founding argument"; body="Adolf Jülicher, Die Gleichnisreden Jesu, 2 vols. (Freiburg im Breisgau: J. C. B. Mohr, 1888; enlarged 2nd ed., Tübingen: J. C. B. Mohr, 1899). Jülicher's massive study argued that Jesus's parables were originally simple, realistic comparisons making a single moral or theological point, sharply distinguished from allegory, in which each narrative detail carries its own separate coded meaning; on this view, the elaborate, detail-by-detail readings attached to parables like the sower are not Jesus's own teaching but a later layer, smuggled back in by centuries of allegorizing interpreters. Jülicher's thesis became the dominant paradigm in critical parable scholarship for most of the twentieth century." }
'jeremias-original-vs-allegorized' = @{ title="Jeremias: recovering the parable behind the explanation"; body="Joachim Jeremias, The Parables of Jesus, rev. ed., trans. S. H. Hooke (London: SCM Press, 1963), translated from the German 6th ed., Die Gleichnisse Jesu (Göttingen: Vandenhoeck & Ruprecht, 1962), esp. the discussion of the sower and its point-by-point explanation. Working form-critically rather than from Jülicher's genre rule as such, Jeremias argued that the detailed, seed-equals-this / soil-equals-that explanation attached to the sower parable (13:18-23 and its Markan parallel) reflects the early church's later application of the story to its own missionary situation, not a transcript of Jesus's original telling — reconstructing, wherever possible, a leaner, less allegorized original form of each parable as spoken in Jesus's own historical setting rather than the church's later one." }
'snodgrass-blomberg-pushback' = @{ title="The pushback: allegory Jesus himself may have intended"; body="Klyne R. Snodgrass, Stories with Intent: A Comprehensive Guide to the Parables of Jesus (Grand Rapids, MI: Eerdmans, 2008), esp. the introductory survey of interpretive history; Craig L. Blomberg, Interpreting the Parables (Downers Grove, IL: InterVarsity Press, 1990). Both scholars argue Jülicher's single-point rule overcorrected: ancient audiences, Jewish and Greco-Roman alike, routinely told and heard stories carrying more than one point of contact with reality, and nothing about a first-century setting requires that a multi-referent, quasi-allegorical reading must be a later church invention rather than something Jesus himself built into the telling from the start. Neither scholar claims certainty in the other direction; the case for authenticity of some allegorical framing is cumulative and probabilistic, and the debate remains genuinely open in current scholarship rather than settled in either direction." }
'mustard-seed-not-literal-smallest' = @{ title="Smallest sown, not smallest possible"; body="R. T. France, The Gospel of Matthew, New International Commentary on the New Testament (Grand Rapids, MI: Eerdmans, 2007), commentary ad loc. Matthew 13:31-32. France notes that mustard seed is not literally the smallest seed known in the ancient or modern world — orchid seeds and poppy seeds are both smaller, and both were known in the ancient Mediterranean — but observes that neither was a crop a Galilean farmer actually planted as a field or garden crop. Within that working universe of seeds an ordinary first-century farmer would recognize and handle, black mustard genuinely was about as small as commonly sown seed got, while the mature plant could reach several feet in height, branching enough for birds to nest in its shade; the saying's rhetorical contrast (smallest planted thing, largest garden plant) is accurate to ordinary Galilean agricultural experience even though it was never a scientifically exhaustive claim about the smallest seed in all creation." }
'isaiah-6-hardening-range' = @{ title="Isaiah 6:9-10: judgment, or something else?"; body="Craig A. Evans, To See and Not Perceive: Isaiah 6:9-10 in Early Jewish and Christian Interpretation, Journal for the Study of the Old Testament Supplement Series 64 (Sheffield: JSOT Press, 1989). Evans's monograph-length study traces how early Jewish and Christian interpreters handled Isaiah's commission to 'make the heart of this people fat, and their ears heavy, and shut their eyes' so that they would not see, hear, understand, and turn to be healed. Evans himself reads the passage as judicial hardening — a judgment already decreed, sealing shut a people who had already and freely rejected the message, so that the hardening is consequence rather than arbitrary cause. Other interpreters across the tradition Evans surveys read the darkened hearing and blinded sight less as something actively imposed from outside and more as a description of a spiritual condition already present before the commission is given. Matthew's use of the text to explain Jesus's parabolic teaching (13:13-15) inherits this same, still-debated ambiguity rather than resolving it." }
'matthew-nazareth-abbreviated-mark' = @{ title="A shorter ending, and a missing cliff"; body="W. D. Davies and Dale C. Allison Jr., A Critical and Exegetical Commentary on the Gospel According to Saint Matthew, International Critical Commentary, vol. 2: Matthew VIII-XVIII (Edinburgh: T&T Clark, 1991), commentary ad loc. Matthew 13:53-58. Davies and Allison conclude there is no reason to read Matthew's version of the Nazareth rejection as anything other than a revised and abbreviated retelling of Mark's parallel account (Mark 6:1-6a), trimmed of several of Mark's details, including the notice that Jesus himself was 'amazed' at the town's unbelief; Luke's considerably fuller version of the same underlying tradition (Luke 4:16-30) is judged more likely to preserve an independently transmitted telling rather than a further reworking of Mark. Whatever the precise source relationships, one plain compositional fact is checkable directly from the texts themselves: Matthew's account ends with rejection and Jesus's departure, while only Luke's version of the same scene escalates all the way to the congregation trying to hurl Jesus off a cliff at the edge of town (Luke 4:28-29) — a violent climax Matthew's telling does not include at all." }
}

# ---- Chapter beats (with [[NOTE:slug]] placeholders) ----
$beat1 = @'
Chapter 13 packs seven parables into a single teaching session — the sower, the weeds among the wheat, the mustard seed, the leaven, the hidden treasure, the pearl of great price, and the dragnet — more concentrated parabolic teaching than anywhere else in the Synoptic tradition. Before touching any one story, the form itself deserves a beat of its own, because "parable" can sound like a technique Jesus invented from nothing: an unprecedented rhetorical trick with no real antecedent in his world. It wasn't. The mashal — a short, realistic or semi-realistic narrative built to illuminate a moral or theological point, typically paired with an explicit application — is a well-attested, indigenous Jewish teaching form, preserved by the hundreds in the later classical Midrash and the Talmud. David Stern's landmark study of the genre catalogues meshalim built around exactly the kind of everyday material Jesus reaches for here — a sower, a woman baking, a king settling accounts, a merchant, a fisherman — deployed to make a point about God, Israel, or judgment [[NOTE:mashal-rabbinic-parable-form]]. Stern's surviving evidence mostly postdates Jesus by a century or more, so the claim isn't that he borrowed any specific rabbinic mashal; it's that he was working inside a real, widely practiced genre, not improvising a form with no cultural footing.

Brad Young's comparative study pushes the same point further, arguing Jesus's parables read more naturally against rabbinic conventions than against Greco-Roman rhetorical fable, and the difference has teeth: rabbinic meshalim routinely assign meaning to more than one detail at once — a king in the story standing for God, a son for Israel, an unpaid debt for sin — rather than making a single isolated comparison [[NOTE:brad-young-jewish-parable-roots]]. Young also notes that many surviving meshalim open with a set introductory formula — "the matter is like a king who..." — distinctly rhyming with the "the kingdom of heaven is like..." framing that opens most of the parables in this very chapter, reinforcing the case that Matthew's parable collection reads as a Jewish teaching form doing exactly what such forms did [[NOTE:brad-young-jewish-parable-roots]]. That matters directly here, because Jesus does exactly this multi-referent move with the sower: explaining it privately to the disciples, he assigns the seed, the birds, the thorns, and the different soils each their own separate meaning (13:18-23), a move that looks less like an anomaly needing explanation and more like ordinary practice within the genre he is already working in. Stern's broader argument about the mashal — that the form itself resists being reduced to one tidy takeaway, since its symbolism was built to be unpacked and argued over — cuts against treating any given parable's "correct" reading as self-evident from the bare story alone [[NOTE:mashal-rabbinic-parable-form]].
'@

$beat2 = @'
The sower's private, point-by-point explanation to the disciples (13:18-23) sits at the center of one of the genuinely long-running, unresolved debates in parable scholarship, and it is worth presenting both sides plainly rather than picking a winner. In 1888, Adolf Jülicher published the study that would define the field for most of the following century, arguing that Jesus's parables were originally simple, single-point comparisons — a sower is a sower, the point is one thing, not many — sharply distinguished from allegory, where each detail of a story carries its own separate coded meaning [[NOTE:julicher-single-point-thesis]]. On Jülicher's account, the elaborate allegorical readings attached to parables like the sower (seed equals the word, birds equal the evil one, thorns equal worldly cares, good soil equals a receptive heart) are not Jesus's own teaching but a later layer, read back in by generations of allegorizing interpreters.

Joachim Jeremias took Jülicher's project in a related but distinct direction. Rather than ruling out allegory as a matter of genre, Jeremias worked form-critically to reconstruct each parable's setting, and concluded that the sower's detailed explanation specifically reflects the early church's application of the story to its own missionary situation — a message about different responses to gospel preaching — rather than a transcript of what Jesus originally said in Galilee [[NOTE:jeremias-original-vs-allegorized]]. On this reading, the parable proper is old and likely goes back to Jesus in something close to its original form; its allegorical unpacking is the church's overlay, added once the story had moved from Jesus's own setting into a new one.

That view no longer commands automatic assent. Klyne Snodgrass and Craig Blomberg, among others writing since the 1990s, have argued Jülicher's single-point rule overcorrected: ancient audiences, Jewish and Greco-Roman alike, routinely told and heard stories carrying more than one point of contact with reality, and nothing about a first-century setting requires that a multi-referent reading must be a later invention rather than something Jesus himself built into the telling from the start [[NOTE:snodgrass-blomberg-pushback]]. Both scholars stop short of claiming certainty in the other direction — the case for authenticity is cumulative and probabilistic, not a knockout argument, and it does not settle which specific elements of the sower's explanation, if any, trace to Jesus rather than to Matthew's or Mark's editorial hand. What can be said plainly is that this remains a live, unsettled question in current scholarship, not one where the field has quietly returned to consensus in either direction [[NOTE:julicher-single-point-thesis]] [[NOTE:jeremias-original-vs-allegorized]].
'@

$beat3 = @'
The mustard seed parable's own internal logic depends on a real point of agricultural fact, and it is worth being precise about exactly what that point is and is not. Jesus calls the mustard seed the smallest of all seeds (13:31-32) — and taken as a claim about every seed in existence, that isn't true: orchid seeds and poppy seeds are both smaller, and both were known in the ancient Mediterranean world. The more precise, defensible version of the claim is this: neither orchid nor poppy seed was something a Galilean farmer actually planted as a field or garden crop, and within that working universe of seeds an ordinary first-century farmer would recognize and handle, black mustard genuinely was about as small as commonly sown seed got, while the mature plant could reach several feet in height — tall and branching enough for birds to nest in its shade [[NOTE:mustard-seed-not-literal-smallest]]. The saying's rhetorical force — smallest planted thing, largest garden plant — tracks ordinary Galilean farming experience even though it was never a scientifically exhaustive claim about the smallest seed in all creation, and reading it as one manufactures a problem the text never intended to create.

This is also a case where the single-point-versus-multi-referent debate from the previous beat has a concrete stake. If the mustard seed is a single-point comparison in Jülicher's sense, its whole force is the contrast of small beginning and large result, full stop. A fuller treatment of the parable argues for taking the additional detail of birds nesting in the branches as itself doing interpretive work — a probable echo of Hebrew Bible imagery (Ezekiel 17:23; Daniel 4:12) in which a great tree sheltering birds pictures a kingdom sheltering the nations — which, if right, is exactly the kind of second layer of meaning Jülicher's method was built to rule out [[NOTE:snodgrass-blomberg-pushback]]. Whether that additional resonance is something Matthew's original audience would have caught, or a connection scholars have drawn after the fact, is not something the parable's bare agricultural accuracy can settle either way.
'@

$beat4 = @'
When the disciples ask privately why Jesus teaches in parables at all, his answer is one of the harder passages in the chapter to sit with honestly: quoting Isaiah 6:9-10, he says the crowds are given parables precisely so that seeing they may not see, and hearing they may not understand (13:13-15) — presented, on Matthew's telling, not as an unfortunate side effect of the teaching method but as its very purpose. That citation carries a long, genuinely difficult interpretive history behind it. Isaiah's original commission tells the prophet to make the heart of the people fat, so that they will not see, hear, understand, and turn to be healed — language that has generated centuries of interpretive disagreement over what kind of act is actually being described. A monograph-length survey of the passage's reception across early Jewish and Christian sources documents a genuine range of readings: some interpreters read it as judicial hardening — a judgment already decreed, sealing shut a people who had already and freely rejected the message, so the hardening is consequence rather than arbitrary cause; others in the same tradition read the darkened hearing and blinded sight less as something actively imposed from outside and more as a description of a spiritual condition already present before the commission is ever given [[NOTE:isaiah-6-hardening-range]]. Matthew inherits this same interpretive fork rather than resolving it: nothing in 13:13-15 forces a reader to choose between "God hardens people who were never going to listen anyway" and "God's judgment on prior refusal takes the specific form of parables that further conceal," and the honest position is that both readings have real, serious scholarly defenders [[NOTE:isaiah-6-hardening-range]].

The chapter closes its parable section with a second, less remarked prophetic citation making a rather different claim about the same teaching method: "I will open my mouth in parables; I will utter what has been hidden since the foundation of the world" (13:35), attributed to fulfillment of Psalm 78:2. Where the Isaiah citation frames parables as concealment for outsiders, this second citation frames them as revelation — uncovering what had been hidden — and the two citations sitting a few verses apart in the same chapter, doing opposite rhetorical work, is worth naming plainly rather than smoothing into one tidy theology of why Jesus taught this way.
'@

$beat5 = @'
The chapter's closing scene returns Jesus to Nazareth, where the hometown crowd's familiarity curdles into contempt: "Is not this the carpenter's son?... where then did this man get all this?" (13:54-57). This project's companion volume on Luke covers the fuller version of the same underlying tradition (Luke 4:16-30) in depth — Nazareth's likely population, the absence of any excavated first-century synagogue at the site, the later Byzantine-period identification of the cliff shown to visitors today — and that ground isn't retraced here. What is worth stating plainly, because it's a real, checkable difference between two Gospels handling the same core material rather than a matter of interpretation, is how much shorter and lower-key Matthew's version is by comparison.

A leading modern commentary on Matthew concludes there is no reason to read 13:53-58 as anything other than a revised, trimmed retelling of Mark's own version of the scene (Mark 6:1-6a) — Matthew's account drops several of Mark's details, including the notice that Jesus himself was "amazed" at the town's unbelief — while Luke's considerably fuller telling of the same tradition is judged more likely to represent an independently transmitted account rather than a further reworking of Mark [[NOTE:matthew-nazareth-abbreviated-mark]]. Whatever the precise relationship between the three versions, one plain compositional fact holds regardless of which source-critical model is right: Matthew's telling ends with the town taking offense and Jesus doing no mighty works there "because of their unbelief" (13:58) — full stop. It is only Luke's version of this same scene that escalates all the way to a mob marching Jesus to the edge of town to throw him off a cliff (Luke 4:28-29). Matthew, working from the same underlying tradition, simply does not include that climax at all [[NOTE:matthew-nazareth-abbreviated-mark]].
'@

$beats = @($beat1, $beat2, $beat3, $beat4, $beat5)

# ---- Glossary additions (heading -> body) ----
$glossary = [ordered]@{
'MASHAL (RABBINIC PARABLE FORM)' = "The short, realistic or semi-realistic Jewish narrative form, typically paired with an explicit application, preserved by the hundreds in later classical Midrash and the Talmud and cited in this chapter as the attested genre Jesus's parable-teaching most closely resembles [[NOTE:mashal-rabbinic-parable-form]]. Rabbinic meshalim routinely assign meaning to several narrative details at once rather than making one isolated point, a feature scholars argue is directly relevant to reading the sower's own multi-part explanation (13:18-23) [[NOTE:brad-young-jewish-parable-roots]]."
'ALLEGORICAL VS. SINGLE-POINT PARABLE INTERPRETATION (JULICHER/JEREMIAS DEBATE)' = "The long-running, still-unresolved source-critical debate over whether Jesus's parables were originally simple, one-point illustrative comparisons — with detailed, multi-referent allegorical readings like the sower's point-by-point explanation (13:18-23) added later by the church — or whether at least some allegorical framing goes back to Jesus's own original telling. Adolf Jülicher's 1888-1899 study established the single-point position [[NOTE:julicher-single-point-thesis]]; Joachim Jeremias's form-critical work reached a related but distinct conclusion, treating the sower's explanation specifically as a later missionary-situation overlay [[NOTE:jeremias-original-vs-allegorized]]; more recent scholarship, including Klyne Snodgrass and Craig Blomberg, argues the single-point rule overcorrected and that multi-referent readings need not be later inventions [[NOTE:snodgrass-blomberg-pushback]]. No side of this debate has settled the question."
'MUSTARD SEED (BOTANICAL ACCURACY)' = "The plant at the center of the parable of Matthew 13:31-32, whose seed Jesus calls the smallest of all seeds. Not literally true across all of nature — orchid and poppy seeds are both smaller — but accurate to the seeds a first-century Galilean farmer would actually have sown as a field or garden crop, among which black mustard was about as small as it got, while the mature plant could grow large enough for birds to nest in its branches [[NOTE:mustard-seed-not-literal-smallest]]."
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
$sortKey = $maxChapterSortKey
foreach ($beatText in $beats) {
    $resolved = $beatText
    foreach ($slug in $slugToNumber.Keys) {
        $resolved = $resolved.Replace("[[NOTE:$slug]]", "[$($slugToNumber[$slug])]")
    }
    $sortKey += 100
    $id = New-BeatRow $resolved
    Add-BeatNode $Ch13NodeId $id $sortKey
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
Seed-Entity "Adolf Jülicher" "adolf-julicher" "character" "German New Testament scholar (1857-1938), Professor at Marburg; his 1888-1899 study Die Gleichnisreden Jesu argued Jesus's parables were originally single-point comparisons rather than allegories."
Seed-Entity "David Stern" "david-stern" "character" "Scholar of rabbinic literature; author of Parables in Midrash (Harvard University Press, 1991), the standard study of the rabbinic mashal form."
Seed-Entity "Brad H. Young" "brad-h-young" "character" "Scholar of the Jewish roots of Jesus's teaching; author of Jesus and His Jewish Parables (Paulist Press, 1989), reading Jesus's parables against rabbinic mashal conventions."
Seed-Entity "Klyne Snodgrass" "klyne-snodgrass" "character" "New Testament scholar; author of Stories with Intent (Eerdmans, 2008), a comprehensive parables guide arguing against Jülicher's strict single-point interpretive rule."
Seed-Entity "Craig L. Blomberg" "craig-l-blomberg" "character" "New Testament scholar; author of Interpreting the Parables (InterVarsity Press, 1990), defending a limited, controlled allegorical reading of Jesus's parables against Jülicher's single-point thesis."

$conn.Close()
Write-Host "DONE Chapter 13 depth pass."
