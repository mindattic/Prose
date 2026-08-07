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
$NotesNodeId = [guid]"019FA01D-FA22-76C6-976C-3EA4F4D54A14"
$GlossaryNodeId = [guid]"019FA026-3758-7A57-AF65-EC1DB4303EF3"
$Ch10NodeId = [guid]"019FA068-3D3A-7CE3-BD9C-F0463448908A"

$maxNoteSortKey = [double](Exec-Scalar "SELECT ISNULL(MAX(bn.SortKey),0) FROM BeatNodes bn WHERE bn.NodeId='019FA01D-FA22-76C6-976C-3EA4F4D54A14' AND bn.IsEnabled=1")
$maxGlossarySortKey = [double](Exec-Scalar "SELECT ISNULL(MAX(bn.SortKey),0) FROM BeatNodes bn WHERE bn.NodeId='019FA026-3758-7A57-AF65-EC1DB4303EF3' AND bn.IsEnabled=1")
$maxChapterSortKey = [double](Exec-Scalar "SELECT ISNULL(MAX(bn.SortKey),0) FROM BeatNodes bn WHERE bn.NodeId='019FA068-3D3A-7CE3-BD9C-F0463448908A' AND bn.IsEnabled=1")
$maxNoteNumber = [int](Exec-Scalar "SELECT ISNULL(MAX(CAST(LEFT(b.Text, CHARINDEX(' ',b.Text)-1) AS INT)),0) FROM BeatNodes bn JOIN Beats b ON bn.BeatId=b.Id WHERE bn.NodeId='019FA01D-FA22-76C6-976C-3EA4F4D54A14' AND bn.IsEnabled=1 AND CHARINDEX(' ', b.Text) > 1 AND LEFT(b.Text, CHARINDEX(' ',b.Text)-1) NOT LIKE '%[^0-9]%'")

Write-Host "Starting: MaxNumber=$script:MaxNumber MaxChapterSortKey=$maxChapterSortKey MaxNoteSortKey=$maxNoteSortKey MaxNoteNumber=$maxNoteNumber MaxGlossarySortKey=$maxGlossarySortKey"

# ---- Notes (slug -> title/body) in order ----
$notes = [ordered]@{
'apostle-lists-four-independent-sources' = @{ title="Four lists, one disputed name"; body="W. D. Davies and Dale C. Allison Jr., A Critical and Exegetical Commentary on the Gospel According to Saint Matthew, vol. 2: Matthew VIII-XVIII, International Critical Commentary (Edinburgh: T&T Clark, 1991), commentary ad loc. Matthew 10:2-4. Davies and Allison lay out all four New Testament rosters of the Twelve side by side (Matthew 10:2-4, Mark 3:16-19, Luke 6:14-16, Acts 1:13) and note that while the great majority of names and the three-tier grouping structure agree closely across all four, no single fixed list survives that all four sources reproduce identically; the clearest point of disagreement is the tenth name, given as Thaddaeus in Matthew and Mark but as Judas son of James in both Luke and Acts." }
'thaddaeus-lebbaeus-textual-variant' = @{ title="Lebbaeus, a manuscript alternative"; body="Bruce M. Metzger, A Textual Commentary on the Greek New Testament, 2nd ed. (Stuttgart: Deutsche Bibelgesellschaft / United Bible Societies, 1994), commentary ad loc. Matthew 10:3. A number of manuscript witnesses to Matthew's list read Lebbaeus in place of Thaddaeus, and a smaller number conflate the two as Lebbaeus, who was called Thaddaeus; Metzger's textual apparatus treats Thaddaeus as the best-attested original reading, with Lebbaeus judged a later scribal substitution or harmonization rather than a competing authentic tradition." }
'thaddaeus-judas-son-of-james-same-person' = @{ title="Same man, two names, probably"; body="W. D. Davies and Dale C. Allison Jr., A Critical and Exegetical Commentary on the Gospel According to Saint Matthew, vol. 2: Matthew VIII-XVIII, International Critical Commentary (Edinburgh: T&T Clark, 1991), commentary ad loc. Matthew 10:2-4. Davies and Allison note the standard scholarly resolution — that Thaddaeus and Judas son of James name the same apostle under two different designations, the way Simon and Peter name one person — while stressing this is an inference, not a demonstrable fact: nothing internal to any of the four lists ties the two names together explicitly, and no Gospel narrative gives this figure an independent scene or saying under either name that would let the identification be checked." }
'israel-first-mission-matt-10-5-6' = @{ title="An Israel-only mission, stated plainly"; body="John P. Meier, The Vision of Matthew: Christ, Church, and Morality in the First Gospel (New York: Paulist Press, 1979), chapter on Matthew's salvation-history schema. Meier reads the double restriction of Matthew 10:5-6 — explicitly barring the road to the Gentiles and forbidding entry into Samaritan towns, in favor of the lost sheep of the house of Israel — as a genuine, deliberately preserved piece of Matthew's own compositional record: a directive this specific and this restrictive is an unlikely later invention for a Gospel that ends by commanding a mission to all nations, and its survival is best explained as an authentic trace of an earlier, narrower phase of the movement's self-understanding that Matthew chose to keep rather than edit away." }
'great-commission-forward-reference-matt-28-19' = @{ title="A commission that outgrows its own start"; body="Ulrich Luz, Matthew 8-20: A Commentary, Hermeneia series (Minneapolis: Fortress Press, 2001), commentary ad loc. Matthew 10:5-6. Luz situates this restriction within Matthew's broader salvation-historical structure, in which an Israel-first mission (10:5-6, later echoed at 15:24's I was sent only to the lost sheep of the house of Israel) gives way, only after the resurrection, to the universal charge of 28:19's make disciples of all nations. Read this way the tension is not a contradiction requiring harmonization but a compositional seam marking a real historical development, one this book returns to at chapter 28." }
'mishnah-oholot-gentile-land-dust' = @{ title="The Mishnah's own accounting of Gentile dust"; body="W. D. Davies and Dale C. Allison Jr., A Critical and Exegetical Commentary on the Gospel According to Saint Matthew, vol. 2: Matthew VIII-XVIII, International Critical Commentary (Edinburgh: T&T Clark, 1991), commentary ad loc. Matthew 10:14; the underlying halakhic principle is codified at Mishnah Oholot 2:3, which rules that soil originating outside the land of Israel conveys the same category of ritual impurity as a grave or a corpse. Davies and Allison read Jesus's instruction against this specific backdrop: a disciple leaving an unreceptive town performs, against that town, the exact gesture a pious Jew performed against Gentile territory, converting an everyday purity practice into a pointed act of religious judgment." }
'acts-13-51-dust-shaking-attested' = @{ title="Carried out on the road: Acts 13:51"; body="C. K. Barrett, A Critical and Exegetical Commentary on the Acts of the Apostles, vol. 1: Preliminary Introduction and Commentary on Acts I-XIV, International Critical Commentary (Edinburgh: T&T Clark, 1994), commentary ad loc. Acts 13:51. The instruction is not left as rhetoric in the New Testament's own record: expelled from Pisidian Antioch by the city's leading citizens, Paul and Barnabas shook off the dust from their feet against them before moving on to Iconium, the one narrated instance elsewhere in the New Testament of the gesture actually being performed, which Barrett treats as evidence the instruction was received and enacted as a real practice rather than left as a one-off rhetorical flourish confined to this commissioning speech." }
'mark-6-8-9-staff-sandals-divergence' = @{ title="Mark allows what Matthew forbids"; body="Robert A. Guelich, Mark 1-8:26, Word Biblical Commentary vol. 34A (Dallas: Word Books, 1989), commentary ad loc. Mark 6:8-9. Mark's version of the same mission charge diverges from Matthew's at a specific, checkable point: where Matthew's Jesus forbids the Twelve to take sandals or a staff (10:9-10, paralleled at Luke 9:3), Mark's Jesus explicitly permits a staff and allows the wearing of sandals, prohibiting only bread, a bag, and money in the belt. Guelich treats this as a genuine divergence between two independent tellings of a shared core tradition rather than a copying error, since Mark's wording is deliberate and specific rather than vague." }
'acquire-vs-take-verb-distinction' = @{ title="Acquire versus take, a partial linguistic explanation"; body="W. D. Davies and Dale C. Allison Jr., A Critical and Exegetical Commentary on the Gospel According to Saint Matthew, vol. 2: Matthew VIII-XVIII, International Critical Commentary (Edinburgh: T&T Clark, 1991), commentary ad loc. Matthew 10:9-10. Davies and Allison note that Matthew's Greek verb here is ktaomai, to acquire or procure, rather than Mark's airo, to take or carry — a distinction some interpreters use to argue Matthew forbids picking up a new staff while Mark permits keeping one already owned. Davies and Allison judge this harmonization only partly successful: it does not account for Mark's explicit permission of sandals against Matthew's explicit prohibition, leaving a real, unresolved divergence between the two accounts of the same instruction." }
'micah-7-6-direct-citation' = @{ title="A direct line from Micah"; body="W. D. Davies and Dale C. Allison Jr., A Critical and Exegetical Commentary on the Gospel According to Saint Matthew, vol. 2: Matthew VIII-XVIII, International Critical Commentary (Edinburgh: T&T Clark, 1991), commentary ad loc. Matthew 10:34-36. Davies and Allison identify Jesus's warning that his mission will set a man against his father, and a daughter against her mother, and a daughter-in-law against her mother-in-law (10:35) as a close, direct citation of Micah 7:6, itself part of a prophetic lament over a society in which family bonds have broken down; Matthew's Jesus applies the prophet's description of social collapse directly to the household division his own movement will cause." }
'theissen-wandering-charismatics' = @{ title="Wandering charismatics and the households they left"; body="Gerd Theissen, Sociology of Early Palestinian Christianity, trans. John Bowden (Philadelphia: Fortress Press, 1978). Theissen's sociological model of the earliest Jesus movement identifies a real, documentable social type behind sayings like this one: itinerant wandering charismatics who left home, family, trade, and settled security to travel and preach, supported along the way by sympathizers in the villages they passed through. A movement built substantially around that kind of radical itinerancy would predictably produce exactly the sort of household rupture Matthew's Jesus describes, not as hyperbole but as a lived social cost." }
'stark-new-religious-movements-family-conflict' = @{ title="A pattern, not an anomaly: family conflict in new religious movements"; body="Rodney Stark, The Rise of Christianity: A Sociologist Reconsiders History (Princeton, NJ: Princeton University Press, 1996), chapter on conversion and social networks. Stark's broader sociological survey of new religious movements documents family and household conflict over a convert's new allegiance as a recurring, well-attested pattern across very different times and places, not a phenomenon unique to the earliest Jesus movement; this wider comparative record lends real, checkable social plausibility to Matthew 10:34-36's description of village- and family-level rupture as a predictable, rather than merely rhetorical, consequence of the movement's demands." }
}

# ---- Chapter beats (with [[NOTE:slug]] placeholders) ----
$beat1 = @'
Chapter ten's naming of the Twelve already flagged that this list does not perfectly match the other three the New Testament preserves. The exact shape of that disagreement is worth pinning down precisely, because it is a real, checkable feature of the earliest tradition, not something later harmonizers simply invented to explain away.

Lay all four rosters side by side — Matthew 10:2-4, Mark 3:16-19, Luke 6:14-16, and Acts 1:13 — and the overwhelming majority of names and their three-tier grouping structure agree closely across all four. The clear point of disagreement sits at the tenth name. Matthew and Mark both call him Thaddaeus; Luke and Acts both call the same slot Judas son of James instead. No single fixed list survives that all four sources reproduce identically — these read as four independent enumerations of the same underlying group, not four copies of one master document [[NOTE:apostle-lists-four-independent-sources]].

The name itself carries a further manuscript wrinkle. A number of witnesses to Matthew's text read Lebbaeus in place of Thaddaeus, and a smaller number conflate the two into Lebbaeus, who was called Thaddaeus; the critical consensus treats Thaddaeus as the best-attested original reading, with Lebbaeus judged a later scribal substitution rather than a genuinely competing name for this figure [[NOTE:thaddaeus-lebbaeus-textual-variant]].

The standard scholarly resolution to the Thaddaeus and Judas-son-of-James split is that both labels name the same individual, the same way Simon and Peter name one person under two designations. That resolution is a reasonable inference, not a provable fact: nothing internal to any of the four lists ties the two names together explicitly, and no Gospel narrative gives this particular apostle an independent scene, saying, or story under either name that would let the identification be checked against anything else [[NOTE:thaddaeus-judas-son-of-james-same-person]]. The honest state of the evidence is real, internal variation within the earliest apostolic tradition about how to name at least one member of the inner circle — consistent with a genuine, independently remembered group of Twelve, but not with a single fixed roster copied cleanly from source to source.
'@

$beat2 = @'
The instruction to go only to the lost sheep of the house of Israel, explicitly barring the road that leads to Gentiles and forbidding entry into any town of the Samaritans (10:5-6), is worth setting directly against this same Gospel's own ending. Matthew 28:19 closes the book with Jesus commanding the exact opposite geographic scope: Go therefore and make disciples of all nations. Within a single work, the mission is first narrowed to Israel alone and then, eighteen chapters later, thrown open to everyone.

Mainstream historical-critical scholarship does not read this as a contradiction that needs smoothing over. A restriction this specific — naming Gentiles and Samaritans by name as off-limits — is an unlikely thing for a later editor to invent inside a Gospel that ends by commanding exactly the mission this verse forbids; its survival in the text is better explained as an authentic trace of an earlier phase of the Jesus movement's own self-understanding, one still narrowly focused on Israel, that Matthew preserved rather than edited away even after his own community's mission had expanded [[NOTE:israel-first-mission-matt-10-5-6]].

Read this way, the tension is a compositional seam, not an error: an Israel-first mission, echoed again at 15:24 where Jesus tells a Canaanite woman I was sent only to the lost sheep of the house of Israel, gives way, specifically after the resurrection, to the universal commission of chapter 28. That two-stage shape reflects a real, plausible development in the earliest Jesus movement's own history — Israel-focused during Jesus's own ministry, expanding to a Gentile mission only afterward — rather than the Gospel simply contradicting itself [[NOTE:great-commission-forward-reference-matt-28-19]]. This book will pick the thread back up when it reaches chapter 28.
'@

$beat3 = @'
Chapter ten's account of the mission instructions already noted that a town rejecting the Twelve gets the dust of that place shaken from the messengers' feet as they leave (10:14), and read that gesture as a deliberate echo of a Jewish practice aimed at Gentile territory. The specific legal basis for that practice, and one place elsewhere in the New Testament where the same instruction is shown actually being carried out, are worth adding in precise, checkable form.

The underlying principle is codified in the Mishnah: soil originating outside the land of Israel is treated as conveying the same category of ritual impurity as a grave or a corpse, a rule that made shaking Gentile dust from one's feet before re-entering Jewish territory a recognized act of ritual self-protection [[NOTE:mishnah-oholot-gentile-land-dust]]. Jesus's instruction repurposes that same gesture against a fellow Jewish town, converting an everyday purity practice into a pointed act of religious judgment against people who would not have expected to be treated like Gentile territory.

The instruction is not left as rhetoric confined to this one speech. Later in the New Testament's own record, expelled from Pisidian Antioch by the city's leading citizens, Paul and Barnabas shook off the dust from their feet against them before moving on to Iconium (Acts 13:51) — the one narrated instance elsewhere in the New Testament of the gesture actually being performed, and evidence the instruction was received by the early movement as a real, repeatable practice rather than a one-off flourish invented for this scene alone [[NOTE:acts-13-51-dust-shaking-attested]].
'@

$beat4 = @'
The mission instructions here forbid the Twelve from taking gold, silver, or copper, a bag for the road, a second tunic, sandals, or a staff (10:9-10), a stripped-down itinerancy chapter ten has already compared to the wandering-teacher tradition of the wider Greco-Roman world. One specific, checkable textual detail is worth adding: Mark's version of this same mission charge does not match Matthew's here, and the mismatch is substantial rather than cosmetic.

Where Matthew's Jesus forbids sandals or a staff, Mark's Jesus explicitly permits a staff and allows the wearing of sandals, prohibiting instead only bread, a bag, and money carried in the belt (Mark 6:8-9). This is a genuine divergence between two independent tellings of a shared core tradition, not a copying slip — Mark's wording is specific and deliberate rather than vague enough to paper over the gap [[NOTE:mark-6-8-9-staff-sandals-divergence]].

One partial linguistic explanation is available. Matthew's Greek verb here is ktaomai, to acquire or procure, rather than Mark's airo, to take or carry — a distinction some interpreters use to argue Matthew forbids picking up a new staff while Mark permits keeping one already owned before setting out. That reading closes part of the gap but not all of it: it does not account for Mark's explicit permission of sandals against Matthew's explicit prohibition of them, leaving a real, unresolved divergence between the two Gospels' accounts of the same set of marching orders [[NOTE:acquire-vs-take-verb-distinction]].
'@

$beat5 = @'
Later in the same chapter, Jesus warns the Twelve that his mission will not bring the peace they might expect: Do not think that I have come to bring peace to the earth; I have not come to bring peace, but a sword. For I have come to set a man against his father, and a daughter against her mother, and a daughter-in-law against her mother-in-law (10:34-36). The line is not free composition — it is a close, direct citation of the prophet Micah, who describes a society in social collapse in almost identical terms: the son treats the father with contempt... a man's enemies are the men of his own house (Micah 7:6) [[NOTE:micah-7-6-direct-citation]]. Matthew's Jesus applies the prophet's picture of a broken household order directly to the division his own movement will cause.

Whether that specific warning reflects a real social experience, rather than only a rhetorical flourish, is a question historical-critical scholarship can actually ground. Gerd Theissen's sociological study of the earliest Jesus movement identifies a real, documentable social role behind sayings like this one: itinerant wandering charismatics who left home, trade, family, and settled security to travel and preach, relying on the hospitality of sympathizers along the way. A movement organized substantially around that kind of radical itinerancy would predictably produce exactly the household rupture this saying describes, as a lived social cost rather than hyperbole [[NOTE:theissen-wandering-charismatics]].

That pattern is not unique to the earliest Jesus movement, either. Broader sociological study of new religious movements across very different times and places documents family and household conflict over a convert's new allegiance as a recurring, well-attested feature of how such movements actually spread, not an anomaly invented for this one text [[NOTE:stark-new-religious-movements-family-conflict]]. Taken together, the direct prophetic citation and the wider comparative record give this saying real, checkable grounding in both text and social history, without requiring any claim about its supernatural content one way or the other.
'@

$beats = @($beat1, $beat2, $beat3, $beat4, $beat5)

# ---- Glossary additions (heading -> body) ----
$glossary = [ordered]@{
'THE TWELVE APOSTLES (LIST VARIANCE ACROSS SOURCES)' = "The general term for the inner circle of twelve men Jesus commissions in this chapter (10:1-4), independently listed four times across the New Testament (Matthew 10:2-4, Mark 3:16-19, Luke 6:14-16, Acts 1:13). The four lists agree on the great majority of names and on a consistent three-tier grouping structure, but disagree at the tenth name — Thaddaeus in Matthew and Mark, Judas son of James in Luke and Acts — with a further manuscript variant reading Lebbaeus for Thaddaeus in some copies of Matthew [[NOTE:apostle-lists-four-independent-sources]] [[NOTE:thaddaeus-lebbaeus-textual-variant]]. The standard reading treats the two names as one apostle under two designations, though nothing in the text itself confirms this beyond inference."
'SHAKING OFF DUST (SYMBOLIC ACT)' = "The gesture Jesus instructs the Twelve to perform against any town that refuses to receive them (10:14): shaking the dust of that place from their feet as they leave. The act repurposes a recognized Jewish practice, codified in the Mishnah, of shaking off the dust of Gentile territory before re-entering the land of Israel, since that dust was treated as conveying the same category of ritual impurity as a grave or corpse [[NOTE:mishnah-oholot-gentile-land-dust]]. Applied here against a fellow Jewish town rather than Gentile territory, the gesture becomes a pointed act of religious judgment; it is later shown actually being carried out by Paul and Barnabas at Pisidian Antioch (Acts 13:51) [[NOTE:acts-13-51-dust-shaking-attested]]."
'ISRAEL-FIRST MISSION VS. GREAT COMMISSION (MATTHEW''S INTERNAL DEVELOPMENT)' = "The real, internal compositional tension between this chapter's restriction of the Twelve's mission to the lost sheep of the house of Israel alone, explicitly barring Gentiles and Samaritans (10:5-6), and this same Gospel's closing command to make disciples of all nations (28:19). Mainstream historical-critical scholarship reads the two passages as marking a genuine two-stage development in the early Jesus movement's own self-understanding — an Israel-focused mission during Jesus's ministry, expanding to a Gentile mission only after the resurrection — rather than treating the earlier restriction as an invented detail requiring harmonization away [[NOTE:israel-first-mission-matt-10-5-6]] [[NOTE:great-commission-forward-reference-matt-28-19]]."
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
    Add-BeatNode $Ch10NodeId $id $sortKey
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
Seed-Entity "The Twelve Apostles (List Variance Across Sources)" "twelve-apostles-list-variance" "vocabulary" "The tension between the four independent New Testament apostle lists (Matthew 10:2-4, Mark 3:16-19, Luke 6:14-16, Acts 1:13); agree on most names, disagree on the tenth (Thaddaeus vs. Judas son of James)."
Seed-Entity "Shaking Off Dust (Symbolic Act)" "shaking-off-dust-symbolic-act" "vocabulary" "First-century Jewish practice of shaking Gentile-territory dust from one's feet before re-entering the land of Israel (per Mishnah Oholot 2:3), repurposed in Matthew 10:14 against unreceptive Jewish towns and later enacted at Acts 13:51."
Seed-Entity "Israel-First Mission vs. Great Commission" "israel-first-mission-vs-great-commission" "vocabulary" "The compositional tension within Matthew's own Gospel between the Israel-only mission restriction of 10:5-6 and the universal Great Commission of 28:19, read by mainstream scholarship as a real two-stage development in the early Jesus movement's self-understanding."

$conn.Close()
Write-Host "DONE Chapter 10 depth pass."
